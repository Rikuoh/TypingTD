using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyMover2D : MonoBehaviour
{
    [Header("Path")]
    public List<Transform> waypoints;
    public float arriveDist = 0.2f;

    [Header("Movement")]
    public float moveSpeed = 2.6f;          // 平常速度
    public float steerLerp = 12f;           // 進行方向追従
    public float pathWidth = 2.5f;          // 道幅（中心線から左右±幅/2）
    public float lateralChangeInterval = 2f;// 車線変更っぽい横ズレ更新間隔

    [Header("JAM / Crowd")]
    public LayerMask enemyMask;             // ← Enemies レイヤーを含める
    public float frontCheckDist = 0.8f;     // 前方ブロック検知距離
    public float frontCheckRadius = 0.22f;  // 前方チェックの太さ（道幅に合わせ調整）
    public float jamRadius = 0.6f;          // 周囲密度を見る半径
    public int   jamMaxNeighbors = 6;       // これ以上いたら最遅
    [Range(0.05f, 1f)]
    public float jamMinSpeedFactor = 0.28f; // 最高に詰まった時の速度倍率（0.28=約1/3）
    public float extraDragAtJam = 2.0f;     // 詰まり時に一時的に増やす Drag

    [Header("Separation (軽い反発)")]
    public float separationRadius = 0.48f;
    public float separationForce  = 1.6f;

    [Header("Knockback")]
    public float maxKnockbackSpeed = 6f;
    public float extraDragWhenPushed = 3f;

    Rigidbody2D rb;
    int wpIndex;
    float lateralTarget, lateralCurrent, lateralTimer;
    float baseDrag;
    readonly Collider2D[] _buf = new Collider2D[12]; // NonAlloc 用

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        baseDrag = rb.drag;
        rb.gravityScale = 0f; // 念のため
        lateralTarget  = Random.Range(-pathWidth * 0.5f, pathWidth * 0.5f);
        lateralCurrent = lateralTarget;
    }

    void Update()
    {
        if (waypoints == null || waypoints.Count == 0) return;

        // 現在セグメントの中心線・方向
        Vector2 pos = rb.position;
        Vector2 center = GetSegmentTarget(pos, out Vector2 segDir);
        Vector2 right  = new Vector2(segDir.y, -segDir.x);

        // 横ずれ目標をたまに更新（車線変更っぽさ）
        lateralTimer += Time.deltaTime;
        if (lateralTimer >= lateralChangeInterval) {
            lateralTimer = 0f;
            lateralTarget = Random.Range(-pathWidth * 0.5f, pathWidth * 0.5f);
        }
        lateralCurrent = Mathf.Lerp(lateralCurrent, lateralTarget, Time.deltaTime * 2.5f);

        // 目標位置＆向き
        Vector2 desiredPos = center + right * lateralCurrent;
        Vector2 toDesired  = desiredPos - pos;
        Vector2 desiredDir = (toDesired.sqrMagnitude < 0.0001f) ? segDir : toDesired.normalized;

        // === 渋滞ロジック ===

        // 1) 前方が詰まっていれば減速（CircleCast で正面を見る）
        float speedFactorFront = 1f;
        var hit = Physics2D.CircleCast(pos, frontCheckRadius, segDir, frontCheckDist, enemyMask);
        if (hit && hit.rigidbody != rb) {
            speedFactorFront = 0.4f; // 正面すぐに誰か → 強めに落とす
        }

        // 2) 周囲密度でさらに減速（近傍の数で補正）
        int n = Physics2D.OverlapCircleNonAlloc(pos, jamRadius, _buf, enemyMask);
        int neighbors = 0;
        for (int i = 0; i < n; i++) {
            var c = _buf[i];
            if (!c || c.attachedRigidbody == rb) continue;
            neighbors++;
        }
        // 0..jamMaxNeighbors を 1..jamMinSpeedFactor にマップ
        float t = Mathf.Clamp01((neighbors - 1) / Mathf.Max(1f, (float)(jamMaxNeighbors - 1)));
        float speedFactorJam = Mathf.Lerp(1f, jamMinSpeedFactor, t);

        // 3) 近接分離（軽い反発で”密集の震え”防止）
        Vector2 sep = ComputeSeparation(pos);

        // 4) 目標速度（最も遅い係数を適用）
        float speedFactor = Mathf.Min(speedFactorFront, speedFactorJam);
        Vector2 velTarget = desiredDir * (moveSpeed * speedFactor) + sep;

        // Drag を密度に応じて増やす（押し合いの粘り）
        float dragTarget = baseDrag + extraDragAtJam * t;
        rb.drag = Mathf.Lerp(rb.drag, dragTarget, Time.deltaTime * 6f);

        // 実速度へ
        rb.velocity = Vector2.Lerp(rb.velocity, velTarget, Time.deltaTime * 6f);

        // Waypoint到達
        if (Vector2.Distance(pos, waypoints[wpIndex].position) <= arriveDist) {
            if (wpIndex < waypoints.Count - 1) wpIndex++;
            else OnReachGoal();
        }
    }
    
    // 0や負数を入れられても詰まないように
    void OnValidate(){
        pathWidth = Mathf.Max(0.01f, pathWidth);
    }

    // WaveSpawner から幅も同期できるように（任意）
    public void SetPathWidth(float w){
        pathWidth = Mathf.Max(0.01f, w);
    }


    // 出現直後の横オフセットを強制セット（中心線からの±値）
    public void ForceInitialLateral(float lateral)
    {
        float half = pathWidth * 0.5f;
        lateralTarget = Mathf.Clamp(lateral, -half, half);
        lateralCurrent = lateralTarget; // スムーズ待ちなしで即反映
    }

    // 被弾ノックバック
    public void ApplyKnockback(Vector2 sourcePos, float force)
    {
        Vector2 dir = ((Vector2)rb.position - sourcePos).normalized;
        Vector2 impulse = dir * force;

        rb.AddForce(impulse, ForceMode2D.Impulse);
        rb.AddForce(new Vector2(Random.Range(-0.5f, 0.5f), Random.Range(-0.1f, 0.1f)) * force * 0.25f, ForceMode2D.Impulse);

        if (rb.velocity.magnitude > maxKnockbackSpeed)
            rb.velocity = rb.velocity.normalized * maxKnockbackSpeed;

        StopAllCoroutines();
        StartCoroutine(KnockbackDragRoutine());
    }

    System.Collections.IEnumerator KnockbackDragRoutine()
    {
        float d0 = rb.drag;
        rb.drag = d0 + extraDragWhenPushed;
        yield return new WaitForSeconds(0.25f);
        rb.drag = d0;
    }

    // ===== Helpers =====
    Vector2 GetSegmentTarget(Vector2 pos, out Vector2 segDir)
    {
        Transform a = waypoints[wpIndex];
        Transform b = (wpIndex < waypoints.Count - 1) ? waypoints[wpIndex + 1] : waypoints[wpIndex];
        Vector2 A = a.position; Vector2 B = b.position;
        Vector2 AB = B - A;
        segDir = AB.sqrMagnitude < 1e-6f ? Vector2.right : AB.normalized;
        float t = Vector2.Dot(pos - A, AB) / (AB.sqrMagnitude + 1e-6f);
        t = Mathf.Clamp01(t);
        return A + AB * t;
    }

    Vector2 ComputeSeparation(Vector2 pos)
    {
        int n = Physics2D.OverlapCircleNonAlloc(pos, separationRadius, _buf, enemyMask);
        Vector2 force = Vector2.zero; int count = 0;
        for (int i = 0; i < n; i++) {
            var c = _buf[i];
            if (!c || c.attachedRigidbody == rb) continue;
            Vector2 toMe = pos - (Vector2)c.attachedRigidbody.position;
            float d = toMe.magnitude + 1e-3f;
            force += toMe / d;
            count++;
        }
        if (count > 0) force = force.normalized * separationForce;
        return force;
    }

    void OnReachGoal()
    {
        Destroy(gameObject); // ゴール処理はプロジェクト仕様に合わせて
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (Application.isPlaying) {
            Gizmos.color = new Color(1f, .8f, 0f, .7f);
            Gizmos.DrawWireSphere(transform.position, jamRadius);
        }
    }
#endif
}
