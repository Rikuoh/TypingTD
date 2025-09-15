using UnityEngine;
using TMPro;
using TD.Typing;

[DisallowMultipleComponent]
[RequireComponent(typeof(TextMeshProUGUI))]
public class TimerUI_TMP : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI text;

    private void Awake()
    {
        if (!text) text = GetComponent<TextMeshProUGUI>();

        // フォント保険：未設定なら LiberationSans SDF を試す
        if (text && !text.font)
        {
            var fallback = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF"); // TMP既定
            if (fallback) text.font = fallback;
        }
    }

    private void OnEnable()  { TypingEvents.Tick += OnTick; }
    private void OnDisable() { TypingEvents.Tick -= OnTick; }

    private void OnTick(TickEvent e)
    {
        if (!text) return;

        int ms  = Mathf.Max(0, (int)e.timeLeftMs);
        int sec = ms / 1000;
        int cc  = (ms % 1000) / 10; // 1/100

        text.text = $"{sec:00}:{cc:00}";
        text.ForceMeshUpdate(); // 通常不要。描画遅延がある時だけ有効化
    }
}
