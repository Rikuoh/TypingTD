// WaveSpawner.cs （Logをまとめてガード）
using UnityEngine;
using System.Collections;

namespace TD.TDCore
{
    [System.Serializable]
    public class Wave
    {
        public Enemy enemyPrefab;
        public int count = 5;
        public float interval = 0.8f;
        public float startDelay = 0f;
    }

    public class WaveSpawner : MonoBehaviour
    {
        public WaypointPath path;
        public Transform spawnPoint; // nullなら pathの最初の点
        public Wave[] waves;

        [SerializeField] bool verboseLogging = false; // InspectorでONにした時だけ出す

        void OnEnable()  { Log("[WaveSpawner] OnEnable"); }
        void Start()     { Log("[WaveSpawner] Start()"); StartCoroutine(RunWaves()); }

        IEnumerator RunWaves()
        {
            Log($"[WaveSpawner] RunWaves start, wavesLen={(waves!=null?waves.Length:-1)}");
            if (waves == null || waves.Length == 0) { Warn("[WaveSpawner] waves is null or empty"); yield break; }

            foreach (var w in waves)
            {
                if (w == null) { Warn("[WaveSpawner] wave element is null"); continue; }
                if (w.enemyPrefab == null) { Warn("[WaveSpawner] enemyPrefab is null"); continue; }

                if (w.startDelay > 0f) yield return new WaitForSeconds(w.startDelay);

                for (int i = 0; i < Mathf.Max(0, w.count); i++)
                {
                    var pos = (spawnPoint != null) ? spawnPoint.position
                        : (path != null && path.Count > 0 ? path.GetPoint(0) : transform.position);

                    var e = Instantiate(w.enemyPrefab, pos, Quaternion.identity);
                    e.path = path;

                    Log($"[WaveSpawner] Spawn pos={pos}, pathAssignedCount={(path!=null?path.Count:-1)}, i={i+1}/{w.count}");
                    yield return new WaitForSeconds(Mathf.Max(0.01f, w.interval));
                }
            }
        }

        // --- dev-only logging helpers ---
        void Log(string msg)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (verboseLogging) Debug.Log(msg);
#endif
        }
        void Warn(string msg)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            Debug.LogWarning(msg);
#endif
        }
    }
}
