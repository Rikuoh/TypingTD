using UnityEngine;
using TD.TypingCore;  // ← 添付のコア
using TD.Typing;      // ← 既存の TypingEvents を使う前提（RaiseWordOk, Tick 等）
using System.Collections.Generic;

namespace TD.TypingBridge
{
    /// <summary>
    /// キー入力を TypingEngineCore に流し、コアの状態を TypingEvents にブリッジ。
    /// - 90秒の残り時間管理・連続ボーナス加算はコアが実施
    /// - 毎フレーム Tick 通知、語を打ち切ったら WordOk(scoreDelta, streak) を通知
    /// </summary>
    public class TypingEngineDriver : MonoBehaviour
    {
        [Header("Game Rule")]
        [SerializeField] float baseSeconds = 90f;

        [Header("Words (仮)")]
        public List<Word> words = new(); // 空なら "すし" を自動使用（コア側実装）

        TypingEngineCore _core;
        int _scoreAtWordStart;
        string _kanaAtWordStart;
        float _elapsed; // 実プレイ秒（WPM用）

        void Awake()
        {
            _core = new TypingEngineCore(baseSeconds, words);
            _scoreAtWordStart = 0;
            _kanaAtWordStart = _core.S.currentKana;
            _elapsed = 0f;
        }

        void Update()
        {
            // 入力（最小構成：古い InputSystem）。必要に応じて新InputSystemに差し替え可。
            var s = Input.inputString;
            if (!string.IsNullOrEmpty(s))
            {
                for (int i = 0; i < s.Length; i++)
                {
                    char ch = s[i];
                    // コアへ1文字
                    string beforeKana = _core.S.currentKana;
                    int beforeScore = _core.S.score;

                    _core.TypeChar(ch.ToString());

                    // 語打ち切り判定： currentKana が変わったら 1語完了
                    if (_core.S.currentKana != beforeKana)
                    {
                        int delta = _core.S.score - _scoreAtWordStart;
                        // 既存の RaiseWordOk がある想定（メソッド名はプロジェクトの実装に合わせて）
                        TypingEvents.RaiseWordOk(delta, _core.S.streak);
                        _scoreAtWordStart = _core.S.score;
                        _kanaAtWordStart = _core.S.currentKana;
                    }
                }
            }

            // 時間を進める（ms 単位）
            float dtMs = Time.unscaledDeltaTime * 1000f;
            _core.Update(dtMs);
            _elapsed += Time.unscaledDeltaTime;

            // 毎フレーム Tick（既存の RaiseTick 相当がある前提）
            TypingEvents.RaiseTick(_core.S.timeLeftMs);

            // コアが finished になったら終端（保険。通常は Tick 監視側で0検知）
            if (_core.S.finished && _core.S.timeLeftMs <= 0)
            {
                // ここでは何もしない。EndGameOnTimerZero が拾ってくれる
            }
        }

        // 結果用にコアの最終状態を渡すアクセサ
        public TypingState GetFinalState() => _core.S;

        // 実プレイ経過秒（WPM計算はコアでも持ってるが、UIにも出すため）
        public float ElapsedSeconds => _elapsed;
    }
}
