using UnityEngine;
using TMPro;
using TD.Typing;

namespace TD.UI
{
    /// <summary>
    /// 残り時間を「残り◯◯秒」で表示（小数なし）。
    /// TypingEvents.Tick(e.timeLeftMs) を購読して更新します。
    /// </summary>
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class TimerUI_RemainingJP : MonoBehaviour
    {
        [Header("表示オプション")]
        [Tooltip("2桁ゼロ埋め（例: 残り09秒）")]
        [SerializeField] private bool zeroPad2 = false;

        [Tooltip("小数の丸め方（OFF=切り捨て / ON=切り上げ）")]
        [SerializeField] private bool ceilInsteadOfFloor = false;

        [Tooltip("0未満は0として表示")]
        [SerializeField] private bool clampToZero = true;

        [Tooltip("表示の前後テキスト")]
        [SerializeField] private string prefix = "残り";
        [SerializeField] private string suffix = "秒";

        private TextMeshProUGUI text;
        private int lastShownSec = int.MinValue;

        private void Awake()
        {
            text = GetComponent<TextMeshProUGUI>();
            if (text != null) text.text = $"{prefix}0{suffix}";
        }

        private void OnEnable()
        {
            TypingEvents.Tick += OnTick;
        }

        private void OnDisable()
        {
            TypingEvents.Tick -= OnTick;
        }

        private void OnTick(TickEvent e)
        {
            // ミリ秒 → 秒（小数なし）
            int ms = (int)e.timeLeftMs;
            float secF = ms * 0.001f;

            int sec = ceilInsteadOfFloor
                ? Mathf.CeilToInt(secF)
                : Mathf.FloorToInt(secF);

            if (clampToZero && sec < 0) sec = 0;

            // 同じ秒なら書き換えない（無駄更新防止＆チラツキ対策）
            if (sec == lastShownSec) return;
            lastShownSec = sec;

            string num = zeroPad2 ? sec.ToString("00") : sec.ToString();
            text.text = $"{prefix}{num}{suffix}";
        }
    }
}
