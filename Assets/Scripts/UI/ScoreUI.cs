using UnityEngine;
using TMPro;
using TD.Game;

namespace TD.UI
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class ScoreUI_TMP : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI text;
        [SerializeField] private GameController game;  // ← 未設定でも自動で探す

        private bool subscribed;

        private void Awake()
        {
            if (!text) text = GetComponent<TextMeshProUGUI>();
            if (text) text.text = "SCORE 0";
        }

        private void OnEnable()
        {
            ResolveGameController();       // ← ここで自動結線
            Subscribe();
            // 初期表示
            if (game && text) text.text = $"SCORE {game.Score}";
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void ResolveGameController()
        {
            if (game != null) return;

            // 1) 近場（親方向）から
            game = GetComponentInParent<GameController>();
            if (game) return;

#if UNITY_2022_3_OR_NEWER
            // 2) シーン全体から（最初に見つかったもの）
            game = FindFirstObjectByType<GameController>(FindObjectsInactive.Exclude);
#else
            game = FindObjectOfType<GameController>();
#endif
            if (!game)
            {
                // 3) 名前で拾う（シーン内に "GameController" がある前提なら）
                var go = GameObject.Find("GameController");
                if (go) game = go.GetComponent<GameController>();
            }

            if (!game)
            {
                Debug.LogWarning("[ScoreUI_TMP] GameController が見つかりません。シーンに配置されているか、名称/namespaceをご確認ください。");
            }
        }

        private void Subscribe()
        {
            if (subscribed || game == null) return;
            game.OnScoreUpdated += HandleScoreUpdated;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed || game == null) return;
            game.OnScoreUpdated -= HandleScoreUpdated;
            subscribed = false;
        }

        private void HandleScoreUpdated(int score, int okCount)
        {
            if (!text) return;
            text.text = $"SCORE {score}";
        }
    }
}
