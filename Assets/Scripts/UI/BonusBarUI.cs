using UnityEngine;
using UnityEngine.UI;
using TD.Typing;

namespace TD.UI
{
    public class BonusBarUI : MonoBehaviour
    {
        public Slider streakSlider; // MaxValue=30
        public RectTransform arrow; // ▲のRectTransform（スライダー上を水平移動）
        public RectTransform sliderFillArea; // スライダーの可動域Rect（位置算出用）

        private int _streak;

        private void Awake()
        {
            if (streakSlider != null) streakSlider.maxValue = 30f;
        }
        private void OnEnable()
        {
            TypingEvents.WordOk += OnWordOk;
            TypingEvents.Mistake += OnMistake;
        }
        private void OnDisable()
        {
            TypingEvents.WordOk -= OnWordOk;
            TypingEvents.Mistake -= OnMistake;
        }
        private void OnWordOk(WordOkEvent e)
        {
            _streak = e.streak;
            UpdateUI();
        }
        private void OnMistake(MistakeEvent e)
        {
            _streak = e.streak; // リセット(0)想定
            UpdateUI();
        }
        private void UpdateUI()
        {
            if (streakSlider != null) streakSlider.value = Mathf.Clamp(_streak, 0, 30);
            if (arrow != null && sliderFillArea != null)
            {
                float t = Mathf.Clamp01(_streak / 30f);
                var area = sliderFillArea.rect;
                var localPos = arrow.localPosition;
                localPos.x = Mathf.Lerp(area.xMin, area.xMax, t);
                arrow.localPosition = localPos;
            }
        }
    }
}