using System.Collections.Generic;
using UnityEngine;
using TMPro;
using TD.Typing;       // 既存のイベント
using TD.TypingCore;  // 上で作ったコア

public class TypingController : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text textKana;
    public TMP_Text textRomaji; // 色付き表示

    [Header("Config")]
    public float baseSeconds = 90f;

    private TypingEngineCore engine;

    void Start()
    {
        var words = LoadWords();
        engine = new TypingEngineCore(baseSeconds, words);
        Render();
    }

    void Update()
    {
        // 時間進行
        float dtMs = Time.deltaTime * 1000f;
        engine.Update(dtMs);
        TypingEvents.RaiseTick(engine.State().timeLeftMs);

        // 入力
        foreach (char c in Input.inputString)
        {
            var before = engine.State();
            engine.TypeChar(c.ToString());
            var after = engine.State();

            // 判定の差分からイベントを発火
            if (after.score != before.score)
            {
                TypingEvents.RaiseWordOk(after.score - before.score, after.streak);
            }
            if (after.streak != before.streak && after.streak % 10 == 0 && after.streak>0)
            {
                int tier = Mathf.Min(3, after.streak/10);
                TypingEvents.RaiseStreakTier(tier);
                TypingEvents.RaiseBonusTime(tier); // +1/+2/+3s
            }
            if (after.mistakes != before.mistakes && after.streak==0)
            {
                TypingEvents.RaiseMistake(0);
            }
        }

        Render();
    }

    void Render()
    {
        var s = engine.State();
        if (textKana) textKana.text = s.currentKana;

        if (textRomaji)
        {
            var cur = s.activeRomaji ?? "";
            int k = Mathf.Clamp(s.romajiCursor, 0, cur.Length);
            string hit = cur.Substring(0, k);
            string rest = cur.Substring(k);
            textRomaji.text = $"<color=#00ff00>{hit}</color><color=#888888>{rest}</color>";
        }
    }

    List<Word> LoadWords()
    {
        var ta = Resources.Load<TextAsset>("words_kana");
        if (ta == null) { Debug.LogWarning("words_kana.json not found in Resources"); return new List<Word>{ new Word{ kana="すし", repRomaji="sushi" } }; }
        Debug.Log($"words_kana.json length={ta.text.Length}");
        var list = JsonUtilityWrapper.FromJsonArray<Word>(ta.text);
        Debug.Log($"parsed words count={list.Count}");
        return (list.Count>0)? list : new List<Word>{ new Word{ kana="すし", repRomaji="sushi" } };
    }
    public TD.TypingCore.TypingState StateReadonly() => engine.State();

}

/// JsonUtility は配列直読みが苦手なのでラッパ
static class JsonUtilityWrapper
{
    [System.Serializable] class Wrapper<T> { public List<T> items; }
    public static List<T> FromJsonArray<T>(string json)
    {
        string wrapped = "{\"items\":"+json+"}";
        var w = JsonUtility.FromJson<Wrapper<T>>(wrapped);
        return w?.items ?? new List<T>();
    }
}

