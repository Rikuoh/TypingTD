// WaveSpawner.cs
using UnityEngine;
using System.Collections;

namespace TD.TDCore
{
    [System.Serializable]
    public class Wave
    {
        public Enemy enemyPrefab;
        public int   count      = 5;
        public float interval   = 0.6f;
        public float startDelay = 0f;
    }

    /// <summary>
    /// Path の最初のセグメント方向を基準に、道幅方向へランダムにオフセットして出現させます。
    /// 出現直後に、敵側に ForceInitialLateral(float) があれば SendMessage で渡します（無ければ無視）。
    /// </summary>
    public class WaveSpawner : MonoBehaviour
    {
        [Header("Route")]
        public WaypointPath path;          // null可
        public Transform    spawnPoint;    // nullなら path の 0 番

        [Header("Waves")]
        public Wave[] waves;

        [Header("Spread")]
        [Tooltip("道幅いっぱいに出す幅（中心 ± 幅/2）。EnemyMover 側の pathWidth と同じにするのが目安")]
        [SerializeField] float initialSpreadWidth = 2.5f;

        [Header("Debug")]
        [SerializeField] bool verboseLogging = false;

        void Start()
        {
            StartCoroutine(RunWaves());
        }

        IEnumerator RunWaves()
        {
            if (waves == null || waves.Length == 0) yield break;

            foreach (var w in waves)
            {
                if (!w?.enemyPrefab) continue;

                if (w.startDelay > 0f) yield return new WaitForSeconds(w.startDelay);

                for (int i = 0; i < Mathf.Max(0, w.count); i++)
                {
                    // --- 基準位置と進行方向 ---
                    Vector2 basePos;
                    if (spawnPoint) basePos = spawnPoint.position;
                    else if (path != null && path.Count > 0) basePos = path.GetPoint(0);
                    else basePos = transform.position;

                    Vector2 dir = Vector2.right; // フォールバック
                    if (path != null)
                    {
                        if (path.Count >= 2)
                            dir = ((Vector2)path.GetPoint(1) - (Vector2)path.GetPoint(0)).normalized;
                        else if (path.Count == 1 && spawnPoint)
                            dir = ((Vector2)path.GetPoint(0) - (Vector2)spawnPoint.position).normalized;
                    }

                    // 2D での「道の右」ベクトル
                    Vector2 right = new Vector2(dir.y, -dir.x);

                    // --- 横ばら撒き ---
                    float half = initialSpreadWidth * 0.5f;
                    float off  = Random.Range(-half, half);      // 中心 ± 幅/2
                    Vector2 spawnPos = basePos + right * off;    // 実スポーン位置

                    // --- 生成 ---
                    var enemy = Instantiate(w.enemyPrefab, spawnPos, Quaternion.identity);
                    enemy.path = path; // あなたの Enemy クラスが持っている想定

                    var mover = enemy.GetComponent<EnemyMover2D>() ?? enemy.GetComponent<EnemyMover2D>();
                    if (mover){
                        mover.SendMessage("SetPathWidth", initialSpreadWidth, SendMessageOptions.DontRequireReceiver);
                        mover.SendMessage("ForceInitialLateral", off, SendMessageOptions.DontRequireReceiver);
                    }


                    // 敵Mover側に初期横オフセットを通知（任意：実装があれば受け取って反映）
                    enemy.gameObject.SendMessage("ForceInitialLateral", off, SendMessageOptions.DontRequireReceiver);

                    if (verboseLogging)
                        Debug.Log($"[WaveSpawner] spawn pos={spawnPos} off={off:F2} dir={dir} i={i+1}/{w.count}");

                    yield return new WaitForSeconds(Mathf.Max(0.01f, w.interval));
                }
            }
        }

#if UNITY_EDITOR
        // Scene上で幅のガイドを表示
        void OnDrawGizmosSelected()
        {
            // 基準位置と方向の推定（エディタ表示用）
            Vector3 basePos;
            if (spawnPoint) basePos = spawnPoint.position;
            else if (path != null && path.Count > 0) basePos = path.GetPoint(0);
            else basePos = transform.position;

            Vector2 dir = Vector2.right;
            if (path != null && path.Count >= 2)
                dir = ((Vector2)path.GetPoint(1) - (Vector2)path.GetPoint(0)).normalized;

            Vector2 right = new Vector2(dir.y, -dir.x);
            Vector3 a = basePos + (Vector3)( right * ( initialSpreadWidth * 0.5f));
            Vector3 b = basePos + (Vector3)(-right * ( initialSpreadWidth * 0.5f));

            Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.9f);
            Gizmos.DrawLine(a, b);
            Gizmos.DrawSphere(a, 0.05f);
            Gizmos.DrawSphere(b, 0.05f);
        }
#endif
    }
}
