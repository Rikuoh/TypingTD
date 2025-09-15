using UnityEngine;
using TD.Typing;

namespace TD.Game
{
    // ★ 独自デリゲート型（System.Actionを使わない）
    public delegate void ScoreUpdatedHandler(int score, int okCount);

    public class GameController : MonoBehaviour
    {
        public int Score   { get; private set; }
        public int OkCount { get; private set; }

        // ★ そのデリゲート型でイベント宣言
        public event ScoreUpdatedHandler OnScoreUpdated;

        private void OnEnable()
        {
            TypingEvents.WordOk += OnWordOk;
        }

        private void OnDisable()
        {
            TypingEvents.WordOk -= OnWordOk;
        }

        private void OnWordOk(WordOkEvent e)
        {
            Score   += e.scoreDelta;
            OkCount += 1;

            // ★ 安全に通知（古いC#でも通る書き方）
            var handler = OnScoreUpdated;
            if (handler != null) handler(Score, OkCount);
        }
    }
}
