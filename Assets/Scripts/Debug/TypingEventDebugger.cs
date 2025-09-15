using UnityEngine;
using TD.Typing;

namespace TD.Debugging
{
    public class TypingEventDebugger : MonoBehaviour
    {
        [Header("Auto Simulate")]
        public bool auto = false;
        public float okInterval = 0.6f; // 秒
        public int scorePerWord = 10;

        private float _t;
        private int _streak;

        private void Update()
        {
            if (auto)
            {
                _t += Time.deltaTime;
                if (_t >= okInterval)
                {
                    _t = 0f;
                    SimWordOk();
                }
            }
            else
            {
                if (Input.GetKeyDown(KeyCode.O)) SimWordOk();
                if (Input.GetKeyDown(KeyCode.M)) SimMistake();
            }
        }

        private void SimWordOk()
        {
            _streak++;
            TypingEvents.RaiseWordOk(scorePerWord, _streak);
            // 10/20/30でTier到達＆時間加算（+1/+2/+3）
            if (_streak == 10) { TypingEvents.RaiseStreakTier(1); TypingEvents.RaiseBonusTime(1); }
            else if (_streak == 20) { TypingEvents.RaiseStreakTier(2); TypingEvents.RaiseBonusTime(2); }
            else if (_streak == 30) { TypingEvents.RaiseStreakTier(3); TypingEvents.RaiseBonusTime(3); }
        }

        private void SimMistake()
        {
            _streak = 0;
            TypingEvents.RaiseMistake(0);
        }
    }
}