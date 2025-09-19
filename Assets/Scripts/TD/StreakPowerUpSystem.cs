using UnityEngine;

namespace TD.Game
{
    /// <summary>
    /// 「残りが初期の1/3を“上から下へ横切ったあと”に、ノーミス requiredStreak で全タワー強化」
    /// - 初期時間は SetInitialDuration(baseSeconds) で外部同期。未同期でも、最初の SetRemainingSeconds で自動同期。
    /// - 連続判定は“しきい値を下回ってから”のみカウント開始。
    /// </summary>
    public class StreakPowerUpSystem : MonoBehaviour
    {
        [Header("Time condition")]
        [Tooltip("初期の制限時間（秒）。TypingController の baseSeconds を Start で SetInitialDuration してください。")]
        [SerializeField] private float initialDuration = 90f;

        [Tooltip("残り時間がこの割合(0~1)を下回ってからカウント開始。1/3→0.3333")]
        [SerializeField, Range(0.05f, 0.95f)] private float thresholdFraction = 1f / 3f;

        [Tooltip("初回の SetRemainingSeconds で、initialDuration を自動同期する（保険）")]
        [SerializeField] private bool autoSyncInitialOnFirstTime = true;

        [Header("Typing condition")]
        [Tooltip("この回数だけ連続で正解すると発動")]
        [SerializeField] private int requiredStreak = 10;

        [Header("Power-up")]
        [Tooltip("全タワーを何秒間パワーアップするか")]
        [SerializeField] private float powerUpDuration = 8f;

        [Tooltip("連続発動の最短間隔（秒）")]
        [SerializeField] private float triggerCooldown = 5f;

        [Tooltip("条件を満たすたびに何度でも発動させるか（OFFなら一度きり）")]
        [SerializeField] private bool repeatable = true;

        // ---- ランタイム状態 ----
        private float _remaining = 0f;
        private float _prevRemaining = float.PositiveInfinity;
        private float _thresholdValue = 30f; // initialDuration * thresholdFraction
        private bool  _thresholdCrossed = false; // 一度下回ったら戻さない
        private bool  _durationSynced = false;   // SetInitialDuration 済み or 自動同期 済み
        private int   _streak = 0;
        private float _lastTriggerTime = -999f;
        private bool  _firedOnce = false;

        /// <summary>ゲーム開始時などに初期持ち時間（秒）を明示同期</summary>
        public void SetInitialDuration(float seconds)
        {
            initialDuration = Mathf.Max(1f, seconds);
            _thresholdValue = initialDuration * thresholdFraction;
            _durationSynced = true;
            // Debug.Log($"[PowerUp] Initial synced: init={initialDuration}, threshold={_thresholdValue}");
        }

        /// <summary>新ラウンド用の完全リセット（必要時のみ）</summary>
        public void ResetForNewRound(float newInitialDuration)
        {
            initialDuration   = Mathf.Max(1f, newInitialDuration);
            _thresholdValue   = initialDuration * thresholdFraction;
            _remaining        = initialDuration;
            _prevRemaining    = float.PositiveInfinity;
            _thresholdCrossed = false;
            _streak           = 0;
            // _firedOnce = false; _lastTriggerTime = -999f; // 複数ラウンドでも一度きりにしたいならコメントアウト解除
            _durationSynced   = true;
        }

        /// <summary>
        /// 残り時間（秒）を毎フレーム更新。
        /// 「直前はしきい値より上、今回でしきい値以下になった」瞬間のみ _thresholdCrossed を true にする。
        /// </summary>
        public void SetRemainingSeconds(float seconds)
        {
            _remaining = Mathf.Max(0f, seconds);

            // 初回保険：Startで同期漏れしても、最初の値で初期時間を合わせる
            if (!_durationSynced && autoSyncInitialOnFirstTime && _remaining > 0f)
            {
                SetInitialDuration(_remaining);
            }

            // “上から下へ横切った”判定（=厳密なクロッシング）
            if (!_thresholdCrossed && _prevRemaining > _thresholdValue && _remaining <= _thresholdValue)
            {
                _thresholdCrossed = true;
                _streak = 0;
                // Debug.Log($"[PowerUp] Threshold crossed now: {_prevRemaining:F2}s -> {_remaining:F2}s (th={_thresholdValue:F2})");
            }

            _prevRemaining = _remaining;
        }

        /// <summary>正解が出た瞬間に呼ぶ（しきい値を下回ってからのみカウント）</summary>
        public void OnTypeCorrect()
        {
            if (!_thresholdCrossed) return;
            _streak++;
            // Debug.Log($"[PowerUp] Streak = {_streak}/{requiredStreak}");
            if (_streak >= requiredStreak)
            {
                TryTrigger();
                _streak = 0; // 再び requiredStreak 連で再発動
            }
        }

        /// <summary>ミスが出た瞬間に呼ぶ（連続をリセット）</summary>
        public void OnTypeMiss()
        {
            _streak = 0;
        }

        private void TryTrigger()
        {
            if (!repeatable && _firedOnce)
            {
                // Debug.Log("[PowerUp] blocked: repeatable==false and already fired");
                return;
            }
            float elapsed = Time.time - _lastTriggerTime;
            if (elapsed < triggerCooldown)
            {
                // Debug.Log($"[PowerUp] blocked by cooldown: {elapsed:F2}s < {triggerCooldown}s");
                return;
            }

            var towers = FindObjectsOfType<TD.TDCore.Tower>(); // アクティブな塔だけ
            // Debug.Log($"[PowerUp] >>> TRIGGER! towers={towers.Length}, duration={powerUpDuration}s");

            foreach (var t in towers)
            {
                if (!t) continue;
                // Debug.Log($"[PowerUp] -> tower {t.name} PowerUpFor({powerUpDuration})");
                t.PowerUpFor(powerUpDuration);
            }

            _lastTriggerTime = Time.time;
            _firedOnce = true;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            requiredStreak  = Mathf.Max(1, requiredStreak);
            powerUpDuration = Mathf.Max(0.1f, powerUpDuration);
            triggerCooldown = Mathf.Max(0f, triggerCooldown);
            initialDuration = Mathf.Max(1f, initialDuration);
            _thresholdValue = initialDuration * thresholdFraction;
        }
#endif
    }
}
