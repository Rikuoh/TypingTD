using UnityEngine;
using TD.Typing;  // WordOkEvent / TypingEvents

namespace TD.Game
{
    // 既存：逐次表示用（使わないなら未購読でOK）
    public delegate void ScoreUpdatedHandler(int score, int okCount);
    

    // ★ リザルト画面へ渡す集計データ
    [System.Serializable]
    public class GameStats
    {
        public int Score;
        public int HighScore;
        public int OkCount;
        public int MissCount;        // 使っていなければ 0 のままでOK
        public int EnemiesDefeated;  // 同上
        public int CorrectChars;     // 同上（正打鍵）
        public int TypedChars;       // 同上（総打鍵）
        public float ElapsedSeconds;

        public int   WPMRounded;
        public float AccuracyPercent;
        
        public int   Rank;          // 今回の順位（1=1位）
        public int[] Top3Scores;    // 上位3スコア

        public void ComputeDerived()
        {
            // 文字数が無ければ OK×5 を近似
            int correct = (CorrectChars > 0) ? CorrectChars : OkCount;
            int typed = (TypedChars > 0) ? TypedChars : (OkCount + MissCount);

            float minutes = Mathf.Max(0.001f, ElapsedSeconds / 60f);
            WPMRounded = Mathf.RoundToInt((correct / 5f) / minutes);
            AccuracyPercent = (typed > 0) ? (100f * correct / typed) : 100f;
        }
    }

    // ★ リザルト通知イベント
    public delegate void GameOverHandler(GameStats stats);

    public class GameController : MonoBehaviour
    {
        // 集計（Unity側は“通知を合算するだけ”）
        public int Score { get; private set; }
        public int OkCount { get; private set; }
        public int MissCount { get; private set; }
        public int EnemiesDefeated { get; private set; }
        public int CorrectChars { get; private set; }
        public int TypedChars { get; private set; }

        // イベント
        public event ScoreUpdatedHandler OnScoreUpdated; // 任意（ゲーム中は使わないなら未購読でOK）
        public event GameOverHandler OnGameOver;         // ★ リザルトUIが購読する

        private const string HighScoreKey = "TD.HighScore";

        private void OnEnable()
        {
            TypingEvents.WordOk += OnWordOk;
            // 必要なら他のイベントも購読して加算：
            TypingEvents.Mistake += OnMistake;
            // TypingEvents.CharDelta += OnCharDelta;
            // EnemyEvents.EnemyKilled += OnEnemyKilled;
        }

        private void OnDisable()
        {
            TypingEvents.WordOk -= OnWordOk;
            TypingEvents.Mistake -= OnMistake;
            // TypingEvents.CharDelta -= OnCharDelta;
            // EnemyEvents.EnemyKilled -= OnEnemyKilled;
        }
        
        private void OnMistake(MistakeEvent e)
        {
            MissCount += 1;
            TypedChars += 1;            // 任意：総打鍵も積むなら
            // OnScoreUpdated は不要なら呼ばなくてOK
        }

        private void OnWordOk(WordOkEvent e)
        {
            // 通知された増分をそのまま加算（再計算しない）
            Score += e.scoreDelta;
            OkCount += 1;

            // ★ 追加（正打鍵として1文字前進）
            CorrectChars += 1;
            TypedChars   += 1;

            var h = OnScoreUpdated;
            if (h != null) h(Score, OkCount);
        }

        // 任意：別イベントから積みたい時の口（今は未使用なら呼ばれない）
        public void RegisterMiss(int typedDelta = 1)
        {
            MissCount++;
            TypedChars += Mathf.Max(1, typedDelta);
        }
        public void RegisterEnemyKilled(int n = 1) => EnemiesDefeated += Mathf.Max(1, n);
        public void RegisterChars(int correctDelta, int typedDelta)
        {
            CorrectChars += Mathf.Max(0, correctDelta);
            TypedChars   += Mathf.Max(0, typedDelta);
        }

        // ★ タイマー0になったら呼ぶ本体（EndGameOnTimerZero から呼び出し）
        public void EndGame(float elapsedSeconds)
        {
            var stats = new GameStats
            {
                Score           = this.Score,
                OkCount         = this.OkCount,
                MissCount       = this.MissCount,
                EnemiesDefeated = this.EnemiesDefeated,
                CorrectChars    = (this.CorrectChars > 0) ? this.CorrectChars : this.OkCount,                   // ★
                TypedChars      = (this.TypedChars   > 0) ? this.TypedChars   : (this.OkCount + this.MissCount),// ★
                ElapsedSeconds  = Mathf.Max(0f, elapsedSeconds)
            };

            stats.ComputeDerived();

            // ★ Top3 登録＆反映
            stats.Rank       = HighScores3.Submit(stats.Score);
            stats.Top3Scores = HighScores3.GetTop3();
            stats.HighScore  = (stats.Top3Scores != null && stats.Top3Scores.Length > 0)
                                ? stats.Top3Scores[0] : stats.Score;

            // ハイスコア保存
            int prev = PlayerPrefs.GetInt(HighScoreKey, 0);
            int now  = Mathf.Max(prev, stats.Score);
            if (now != prev) { PlayerPrefs.SetInt(HighScoreKey, now); PlayerPrefs.Save(); }
            stats.HighScore = now;

            // リザルト通知
            var handler = OnGameOver; if (handler != null) handler(stats);
        }
    }
}
