// Assets/Scripts/TD/Enemy.cs
using UnityEngine;
using System.Collections.Generic;

namespace TD.TDCore
{
    public class Enemy : MonoBehaviour
    {
        // === Tower から参照される生存リスト ===
        public static readonly List<Enemy> Alive = new List<Enemy>();

        [Header("Stats")]
        public float maxHp = 10f;
        public float moveSpeed = 2.6f;      // 基本速度
        public WaypointPath path;           // 経路 0..N-1 へ

        [Header("Path Spread")]
        [Tooltip("道幅（中心線から左右±半分）。SpawnerのinitialSpreadWidthと同じにする")]
        public float pathWidth = 2.5f;
        [Tooltip("スポーン時の左右ばら撒きを“段”で与えるならON（3～5段推奨）")]
        public bool useLaneSpawn = true;
        [Range(2, 8)] public int laneCount = 4;
        [Range(0f, 0.5f)] public float laneJitter = 0.08f; // 段から少し揺らす

        [Header("Jam / Crowd（詰まり感）")]
        public LayerMask enemyMask;         // Enemies レイヤーを入れると近傍検知が安く正確
        public float frontCheckDist = 0.8f; // 前方ブロック検知距離
        public float frontCheckRadius = 0.22f;
        public float jamRadius = 0.7f;      // 周囲密度を見る半径
        public int   jamMaxNeighbors = 6;   // これ以上いたら最遅
        [Range(0.05f, 1f)]
        public float jamMinSpeedFactor = 0.22f; // 最も詰まった時の速度倍率（下げるほど渋滞）

        [Header("Separation（近接分離）")]
        public float separationRadius = 0.48f;
        public float separationForce  = 1.2f;

        [Header("FX")]
        public ParticleSystem deathExplosionPrefab;

        [Header("Corner Tuning")]
        [Tooltip("コーナーの手前で次セグメント方向にブレンドする範囲（0〜0.45）")]
        [Range(0f, 0.45f)] public float cornerBlend = 0.25f; // 例: 0.25 = 区間の最後25%でブレンド

        // --- 内部 ---
        float _hp;
        int   _wpIndex;
        bool  _dead;
        float _lateral;                        // 横オフセット（中心線から±）
        Vector2 _prevPos; bool _initedPrev;
        readonly Collider2D[] _buf = new Collider2D[12];

        // ===== ライフサイクル =====
        void OnEnable()
        {
            if (!Alive.Contains(this)) Alive.Add(this);
            _hp = maxHp;
            _wpIndex = 0;

            // スポーン時の横オフセット決定（ForceInitialLateralで上書き可能）
            float half = Mathf.Max(0.01f, pathWidth) * 0.5f;
            if (useLaneSpawn && laneCount >= 2)
            {
                int lane = Random.Range(0, laneCount);         // 0..laneCount-1
                float t = (laneCount == 1) ? 0.5f : (lane / (float)(laneCount - 1)); // 0..1
                _lateral = Mathf.Lerp(-half, half, t);
                _lateral += Random.Range(-laneJitter, laneJitter) * half;
            }
            else
            {
                _lateral = Random.Range(-half, half);
            }
        }

        void OnDisable() { Alive.Remove(this); }

        void Update()
        {
            if (_dead || path == null || path.Count == 0) return;

            // 最終点に十分近ければ終了
            if (_wpIndex >= path.Count - 1)
            {
                Vector3 last = path.GetPoint(path.Count - 1);
                if ((last - transform.position).sqrMagnitude <= 0.01f) { Destroy(gameObject); return; }
            }

            // === 現在セグメント ===
            int next = Mathf.Min(_wpIndex + 1, path.Count - 1);
            Vector2 A = path.GetPoint(_wpIndex);
            Vector2 B = path.GetPoint(next);
            Vector2 AB = B - A;

            // 射影（tRaw=生の値, t=0..1にクランプ）
            float denom = AB.sqrMagnitude + 1e-6f;
            float tRaw = Vector2.Dot((Vector2)transform.position - A, AB) / denom;
            float t    = Mathf.Clamp01(tRaw);

            // 1) セグメント切替：tRaw>=1 で即次へ（距離判定ナシ）
            if (tRaw >= 1f && _wpIndex < path.Count - 1)
            {
                _wpIndex++;
                return; // 次フレームに新セグメントで再計算
            }

            // セグメントの基準方向
            Vector2 segDir = (denom < 1e-6f) ? Vector2.right : AB.normalized;

            // 2) 角の手前で次セグメント方向にブレンド（道からはみ出さないため）
            Vector2 dirForOffset = segDir;
            if (cornerBlend > 0f && _wpIndex < path.Count - 2)
            {
                float edge0 = 1f - cornerBlend;                 // ブレンド開始t
                if (t > edge0)
                {
                    Vector2 C     = path.GetPoint(_wpIndex + 2);
                    Vector2 nextDir = (C - B).sqrMagnitude < 1e-6f ? segDir : (C - B).normalized;
                    float s = Mathf.InverseLerp(edge0, 1f, t);  // 0→1
                    dirForOffset = Vector2.Lerp(segDir, nextDir, s).normalized;
                }
            }

            // 最近点（中心線）＋横オフセット
            Vector2 center = A + AB * t;
            Vector2 right  = new Vector2(dirForOffset.y, -dirForOffset.x); // ブレンドした向きの“右”
            Vector2 desiredPos = center + right * _lateral;

            // === 渋滞ファクタ ===
            float speedFactorFront = 1f;
            var hit = Physics2D.CircleCast(transform.position, frontCheckRadius, segDir, frontCheckDist, enemyMask);
            if (hit && hit.rigidbody && hit.rigidbody.gameObject != gameObject) speedFactorFront = 0.4f;

            int n = Physics2D.OverlapCircleNonAlloc(transform.position, jamRadius, _buf, enemyMask);
            int neighbors = 0;
            for (int i = 0; i < n; i++)
            {
                var c = _buf[i];
                if (!c) continue;
                var rb = c.attachedRigidbody;
                if (rb == null || rb.gameObject == gameObject) continue;
                neighbors++;
            }
            float densT = Mathf.Clamp01((neighbors - 1) / Mathf.Max(1f, (float)(jamMaxNeighbors - 1)));
            float speedFactorJam = Mathf.Lerp(1f, jamMinSpeedFactor, densT);
            float speedFactor = Mathf.Min(speedFactorFront, speedFactorJam);

            // 分離（軽い反発）
            Vector2 sep = ComputeSeparation(transform.position);

            // 前進
            Vector2 toDesired = (Vector2)desiredPos - (Vector2)transform.position;
            Vector2 stepDir = (toDesired.sqrMagnitude < 1e-6f) ? dirForOffset : toDesired.normalized;
            float step = moveSpeed * speedFactor * Time.deltaTime;
            transform.position = (Vector2)transform.position + stepDir * step + sep * Time.deltaTime;
        }

        // ===== API（Spawnerや他から上書き可能に）=====
        public void SetPathWidth(float w) => pathWidth = Mathf.Max(0.01f, w);
        public void ForceInitialLateral(float lateral)
        {
            float half = Mathf.Max(0.01f, pathWidth) * 0.5f;
            _lateral = Mathf.Clamp(lateral, -half, half);
        }

        // ===== 近接分離 =====
        Vector2 ComputeSeparation(Vector2 pos)
        {
            int n = Physics2D.OverlapCircleNonAlloc(pos, separationRadius, _buf, enemyMask);
            Vector2 force = Vector2.zero; int count = 0;
            for (int i = 0; i < n; i++)
            {
                var c = _buf[i];
                if (!c) continue;
                var rb = c.attachedRigidbody;
                if (rb == null || rb.gameObject == gameObject) continue;

                Vector2 toMe = pos - (Vector2)rb.position;
                float d = toMe.magnitude + 1e-3f;
                force += toMe / d;
                count++;
            }
            if (count > 0) force = force.normalized * separationForce;
            return force;
        }

        // ===== ダメージ =====
        public void TakeDamage(float damage)
        {
            if (_dead) return;
            _hp -= damage;
            if (_hp <= 0f) Die();
        }

        void Die()
        {
            if (_dead) return;
            _dead = true;

            if (deathExplosionPrefab)
            {
                var fx = Instantiate(deathExplosionPrefab, transform.position, Quaternion.identity);
                fx.Play();
                Destroy(fx.gameObject, 1f);
            }
            Destroy(gameObject);
        }

#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, .8f, 0f, .7f);
            Gizmos.DrawWireSphere(transform.position, jamRadius);
        }
#endif
    }
}
