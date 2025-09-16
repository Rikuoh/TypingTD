using UnityEngine;
using TD.Typing;              // ← TickEvent を受ける
using TD.Game;                // ← GameController を呼ぶ
using TD.TypingBridge;        // ← TypingEngineDriver を参照（無ければ外してOK）

namespace TD.UI
{
    /// <summary>
    /// TypingEvents.Tick を監視し、残り時間が 0 になった瞬間に
    /// GameController.EndGame(...) を呼び出す。
    /// 実プレイ時間（elapsed）は UnscaledTime で積算。
    /// </summary>
    public class EndGameOnTimerZero : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private GameController game;            // 空なら自動で探す
        [SerializeField] private TypingEngineDriver driver;      // エンジンの真値が欲しい場合（任意）

        [Header("Time Count")]
        [Tooltip("実プレイ時間の積算に UnscaledDeltaTime を使う（UI停止しても進む）")]
        [SerializeField] private bool useUnscaledTime = true;

        [Header("Debug")]
        [SerializeField] private bool logTransitions = false;

        private bool running;   // Tick が正数になったら true
        private bool ended;     // 一度だけ EndGame を呼ぶ
        private float elapsed;  // 実プレイ秒

        private void Awake()
        {
            if (!game)   game   = FindObjectOfType<GameController>();
            if (!driver) driver = FindObjectOfType<TypingEngineDriver>(); // なくてもOK
            elapsed = 0f;
        }

        private void OnEnable()
        {
            ended = false;
            running = false;
            elapsed = 0f;

            // あなたの TypingEvents が TickEvent を投げる前提
            TypingEvents.Tick += OnTickEvent;

            // もし Tick が float(ms) なら上を外し、下を使ってください：
            // TypingEvents.TickMs += OnTickMs;
        }

        private void OnDisable()
        {
            TypingEvents.Tick -= OnTickEvent;
            // TypingEvents.TickMs -= OnTickMs;
        }

        private void Update()
        {
            if (!running || ended) return;
            elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        }

        // TickEvent 版
        private void OnTickEvent(TickEvent e)
        {
            if (!running && e.timeLeftMs > 0)
            {
                running = true;
                if (logTransitions) Debug.Log("[EndGameOnTimerZero] Run started.");
            }

            if (!ended && e.timeLeftMs <= 0)
            {
                if (logTransitions) Debug.Log("[EndGameOnTimerZero] Time reached zero. Ending…");
                EndNow();
            }
        }

        // float(ms) 版（必要なら使う）
        private void OnTickMs(float ms)
        {
            if (!running && ms > 0) running = true;
            if (!ended && ms <= 0) EndNow();
        }

        private void EndNow()
        {
            ended = true;

            if (!game)
            {
                Debug.LogWarning("[EndGameOnTimerZero] GameController が見つかりません。");
                return;
            }

            // （任意）TypingEngineDriver があれば、終了直前に“エンジンの真値”を Game に渡す
            // ※ GameController に ApplyEngineSummary(TypingState,float) を実装している場合のみ
            //    未実装ならこの if ブロックごと削除/コメントアウトしてください。
            // if (driver && game)
            // {
            //     #if true
            //     game.ApplyEngineSummary(driver.GetFinalState(), driver.ElapsedSeconds);
            //     #endif
            // }

            // 実プレイ経過秒で EndGame（ResultUI が OnGameOver を購読していれば表示される）
            game.EndGame(driver ? driver.ElapsedSeconds : elapsed);
        }
    }
}
