using UnityEngine;
using TD.Typing;

namespace TD.Game
{
    public class GameTimer : MonoBehaviour
    {
        [Tooltip("セッション長(秒)")]
        public float sessionSeconds = 90f;
        [Tooltip("Tick通知間隔(秒)")]
        public float tickInterval = 0.1f;

        public bool IsRunning { get; private set; } = true;
        public float TimeLeftMs => _timeLeftMs;

        private float _timeLeftMs;
        private float _tickAccum;

        private void Awake()
        {
            _timeLeftMs = sessionSeconds * 1000f;
        }

        private void OnEnable()
        {
            TypingEvents.BonusTime += OnBonusTime;
        }
        private void OnDisable()
        {
            TypingEvents.BonusTime -= OnBonusTime;
        }

        private void Update()
        {
            if (!IsRunning) return;
            float dtMs = Time.deltaTime * 1000f;
            _timeLeftMs -= dtMs;
            _tickAccum += Time.deltaTime;

            if (_tickAccum >= tickInterval)
            {
                _tickAccum = 0f;
                TypingEvents.RaiseTick(Mathf.Max(_timeLeftMs, 0f));
            }

            if (_timeLeftMs <= 0f)
            {
                _timeLeftMs = 0f;
                IsRunning = false;
                // ここで終了演出や集計呼び出しを行う
                enabled = false;
            }
        }

        private void OnBonusTime(BonusTimeEvent e)
        {
            if (!IsRunning) return;
            _timeLeftMs += e.seconds * 1000f;
        }

        public void ResetAndStart()
        {
            _timeLeftMs = sessionSeconds * 1000f;
            IsRunning = true;
            enabled = true;
        }
    }
}