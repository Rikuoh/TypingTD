using UnityEngine;
using TMPro;
using TD.Typing;

public class TypingVFXController : MonoBehaviour
{
    [Header("Refs (assign in Inspector)")]
    public RectTransform bonusBarRoot;      // ボーナスバー親
    public CanvasGroup screenFlash;         // 全画面白フラッシュ
    public TMP_Text floatingTextPrefab;     // +10 ポップ
    public TMP_Text timePopupPrefab;        // +1s ポップ
    public ParticleSystem hitSparkPrefab;   // 火花プレハブ（任意）
    public Transform[] muzzlePoints;        // 銃口Transform（Towerにある）
    public Canvas uiCanvas;                 // ← VFXにCanvasをドラッグして渡す
    Vector3 barOrigScale;
    public TMP_Text tierPopupPrefab;        // ← FX_TierPopup をドラッグ
    float _lastTierAt = -999f;
    void OnEnable()
    {
        TypingEvents.WordOk += OnWordOk;
        TypingEvents.StreakTierUp += OnTier;
        TypingEvents.BonusTime += OnBonus;
        TypingEvents.Mistake += OnMistake;
        if (bonusBarRoot) barOrigScale = bonusBarRoot.localScale;
    }
    void OnDisable()
    {
        TypingEvents.WordOk -= OnWordOk;
        TypingEvents.StreakTierUp -= OnTier;
        TypingEvents.BonusTime -= OnBonus;
        TypingEvents.Mistake -= OnMistake;
    }

    void OnWordOk(WordOkEvent e)
    {
        // Flash(0.22f);
        SpawnFloating("+10", new Vector2(Screen.width - 220f, Screen.height - 120f));
        SparkAll();
    }
    void OnTier(StreakTierEvent e)
    {
        // 中央に「TIER UP!」
        _lastTierAt = Time.unscaledTime;
        SpawnTierPopup($"TIER {e.tier}!");
        // ボーナスバーを脈動
        PulseBar(1.15f, 0.12f);
        // 軽いカメラ揺れ
        CameraShake(0.10f, 0.12f);
    }

    void OnBonus(BonusTimeEvent e)
    {
        // 直近にTIERが来ていたら少し遅らせる
        if (Time.unscaledTime - _lastTierAt < 0.35f)
        {
            StartCoroutine(CoDelayTimePopup(e.seconds, 0.25f));
        }
        else
        {
            SpawnTimePopup($"+{e.seconds}s");
        }
    }
    System.Collections.IEnumerator CoDelayTimePopup(int secs, float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        SpawnTimePopup($"+{secs}s");
    }
    void OnMistake(MistakeEvent e)
    {
        // FlashColor(new Color(1f, 0.2f, 0.2f, 0.25f), 0.15f);
        PulseBar(0.95f, 0.08f);
    }

    // --- 演出用の処理 ---
    void Flash(float alpha) => FlashColor(new Color(1, 1, 1, alpha), 0.15f);
    void FlashColor(Color c, float fade)
    {
        if (!screenFlash) return;
        StopAllCoroutines();
        StartCoroutine(CoFlash(c, fade));
    }
    System.Collections.IEnumerator CoFlash(Color c, float fade)
    {
        screenFlash.alpha = c.a;
        var img = screenFlash.GetComponent<UnityEngine.UI.Image>();
        if (img) img.color = c;
        float t = 0;
        while (t < fade) { t += Time.unscaledDeltaTime; screenFlash.alpha = Mathf.Lerp(c.a, 0f, t / fade); yield return null; }
        screenFlash.alpha = 0f;
    }

    void PulseBar(float scale, float durUp)
    {
        if (!bonusBarRoot) return;
        StopCoroutine("CoPulse"); StartCoroutine("CoPulse", (scale, durUp));
    }
    System.Collections.IEnumerator CoPulse(System.ValueTuple<float, float> p)
    {
        float target = p.Item1, up = p.Item2, down = 0.18f;
        Vector3 s0 = barOrigScale, s1 = barOrigScale * target;
        float t = 0; while (t < up) { t += Time.unscaledDeltaTime; bonusBarRoot.localScale = Vector3.Lerp(s0, s1, t / up); yield return null; }
        t = 0; while (t < down) { t += Time.unscaledDeltaTime; bonusBarRoot.localScale = Vector3.Lerp(s1, s0, t / down); yield return null; }
        bonusBarRoot.localScale = s0;
    }

    void SpawnFloating(string text, Vector2 screenPos)
    {
        if (!floatingTextPrefab || !uiCanvas) return;
        var ui = Instantiate(floatingTextPrefab, uiCanvas.transform);
        ui.text = text;
        var rt = ui.rectTransform;
        // 右上アンカー・右上ピボットに固定
        rt.anchorMin = rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(1, 1);
        // 右上からの相対オフセット（-x, -y）
        rt.anchoredPosition = new Vector2(-220f, -120f);
        // 改行防止（ついでに）
        ui.enableWordWrapping = false;
        Destroy(ui.gameObject, 0.8f);
    }

    void SpawnTimePopup(string text)
    {
        if (!timePopupPrefab || !uiCanvas) return;
        var ui = Instantiate(timePopupPrefab, uiCanvas.transform);
        ui.text = text;
        var rt = ui.rectTransform;
        // 画面中央アンカー・中央ピボット
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.45f); // ← 中央より下
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        // 改行防止
        ui.enableWordWrapping = false;
        Destroy(ui.gameObject, 1.0f);
        ui.rectTransform.SetAsLastSibling();

    }

    void SparkAll()
    {
        if (hitSparkPrefab == null || muzzlePoints == null) return;
        foreach (var m in muzzlePoints) if (m)
            {
                var fx = Instantiate(hitSparkPrefab, m.position, Quaternion.identity);
                fx.Play();
                Destroy(fx.gameObject, 1f);
            }
    }


    void CameraShake(float amplitude, float duration)
    {
        if (Camera.main == null) return;
        StartCoroutine(CoShake(Camera.main.transform, amplitude, duration));
    }
    System.Collections.IEnumerator CoShake(Transform cam, float amp, float dur)
    {
        Vector3 basePos = cam.localPosition; float t = 0;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            cam.localPosition = basePos + (Vector3)(Random.insideUnitCircle * amp);
            yield return null;
        }
        cam.localPosition = basePos;
    }

    void SpawnTierPopup(string text = "TIER UP!")
    {
        if (!tierPopupPrefab || !uiCanvas) return;
        var ui = Instantiate(tierPopupPrefab, uiCanvas.transform);
        var rt = ui.rectTransform;

        // 中央に出す（少し上目だと見やすい）
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.58f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;

        // 最初は少し大きめ → 元サイズへ
        rt.localScale = Vector3.one * 1.35f;

        // 折り返し無効を念のため
        ui.enableWordWrapping = false;

        // スケールダウン＆フェードアウト
        StartCoroutine(CoTierAnim(ui));
        ui.rectTransform.SetAsLastSibling();

    }
    System.Collections.IEnumerator CoTierAnim(TMP_Text ui)
    {
        var rt = ui.rectTransform;
        // 1) ぎゅっと縮む（0.18s）
        float t = 0, d1 = 0.18f;
        while (t < d1)
        {
            t += Time.unscaledDeltaTime; float k = t / d1;
            rt.localScale = Vector3.Lerp(Vector3.one * 1.35f, Vector3.one, k);
            yield return null;
        }
        // 2) 上に少し浮かせながらフェード（0.5s）
        CanvasGroup cg = ui.gameObject.AddComponent<CanvasGroup>();
        Vector3 p0 = rt.anchoredPosition, p1 = p0 + Vector3.up * 20f;
        float t2 = 0, d2 = 0.5f;
        while (t2 < d2)
        {
            t2 += Time.unscaledDeltaTime; float k = t2 / d2;
            rt.anchoredPosition = Vector3.Lerp(p0, p1, k);
            cg.alpha = 1f - k;
            yield return null;
        }
        Destroy(ui.gameObject);
    }
}