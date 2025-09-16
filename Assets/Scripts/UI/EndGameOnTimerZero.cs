using UnityEngine;
using TD.Typing;   // TickEvent を受ける
using TD.Game;     // GameController を呼ぶ

namespace TD.UI
{
    public class EndGameOnTimerZero : MonoBehaviour
    {
        [SerializeField] private GameController game;
        [SerializeField] private bool useUnscaledTime = true;
        [SerializeField] private KeyCode forceKey = KeyCode.F9; // テスト用：F9で即リザルト

        private bool running; // Tickで>0になったらtrue
        private bool ended;   // 一度だけ EndGame を呼ぶ
        private float elapsed;

        private void Awake()
        {
            if (!game) game = FindObjectOfType<GameController>();
        }

        private void OnEnable()
        {
            running = false; ended = false; elapsed = 0f;
            TypingEvents.Tick += OnTick;   // ★ TickEvent を受ける
        }

        private void OnDisable()
        {
            TypingEvents.Tick -= OnTick;
        }

        private void Update()
        {
            if (running && !ended)
                elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

            // テスト用強制終了
            if (!ended && Input.GetKeyDown(forceKey))
                EndNow();
        }

        private void OnTick(TickEvent e)
        {
            if (!running && e.timeLeftMs > 0f) running = true;
            if (!ended && e.timeLeftMs <= 0f) EndNow();
        }

        private void EndNow()
        {
            ended = true;
            if (game) game.EndGame(elapsed); // ← GameController に EndGame が必要
        }
    }
}
