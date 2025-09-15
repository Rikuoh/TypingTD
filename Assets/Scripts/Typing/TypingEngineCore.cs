using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TD.TypingCore
{
    [Serializable] public class Word { public string kana; public string repRomaji; }

    public struct TypingState
    {
        public float timeLeftMs;
        public int score;
        public int streak;
        public bool finished;

        public string currentKana;
        public string repRomaji;     // 代表表記（UIの下行・参考）
        public string activeRomaji;  // 実際に採用している候補
        public int romajiCursor;     // どこまで打てたか

        public int typedCount, correctCount, mistakes;
        public float wpm, accuracy;
    }

    public class RomajiMatcher
    {
        private List<string> _cands = new();
        private int _idx = 0;

        static readonly (string kana, string[] rom)[] TABLE = new (string, string[])[]
        {
            // 拗音
            ("きゃ", new[]{"kya"}),("きゅ", new[]{"kyu"}),("きょ", new[]{"kyo"}),
            ("しゃ", new[]{"sha","sya"}),("しゅ", new[]{"shu","syu"}),("しょ", new[]{"sho","syo"}),
            ("ちゃ", new[]{"cha","tya","cya"}),("ちゅ", new[]{"chu","tyu","cyu"}),("ちょ", new[]{"cho","tyo","cyo"}),
            ("じゃ", new[]{"ja","zya","jya"}),("じゅ", new[]{"ju","zyu","jyu"}),("じょ", new[]{"jo","zyo","jyo"}),
            // 例外
            ("し", new[]{"shi","si"}),("ち", new[]{"chi","ti"}),("つ", new[]{"tsu","tu"}),
            ("じ", new[]{"ji","zi"}),("ふ", new[]{"fu","hu"}),
            // 清音など（主要部）
            ("き",new[]{"ki"}),("ぎ",new[]{"gi"}),("く",new[]{"ku"}),("ぐ",new[]{"gu"}),
            ("け",new[]{"ke"}),("げ",new[]{"ge"}),("か",new[]{"ka"}),("が",new[]{"ga"}),
            ("こ",new[]{"ko"}),("ご",new[]{"go"}),
            ("さ",new[]{"sa"}),("ざ",new[]{"za"}),("す",new[]{"su"}),("ず",new[]{"zu"}),
            ("せ",new[]{"se"}),("ぜ",new[]{"ze"}),("そ",new[]{"so"}),("ぞ",new[]{"zo"}),
            ("た",new[]{"ta"}),("だ",new[]{"da"}),("て",new[]{"te"}),("で",new[]{"de"}),
            ("と",new[]{"to"}),("ど",new[]{"do"}),
            ("な",new[]{"na"}),("に",new[]{"ni"}),("ぬ",new[]{"nu"}),("ね",new[]{"ne"}),("の",new[]{"no"}),
            ("は",new[]{"ha"}),("ひ",new[]{"hi"}),("へ",new[]{"he"}),("ほ",new[]{"ho"}),
            ("ば",new[]{"ba"}),("び",new[]{"bi"}),("ぶ",new[]{"bu"}),("べ",new[]{"be"}),("ぼ",new[]{"bo"}),
            ("ぱ",new[]{"pa"}),("ぴ",new[]{"pi"}),("ぷ",new[]{"pu"}),("ぺ",new[]{"pe"}),("ぽ",new[]{"po"}),
            ("ま",new[]{"ma"}),("み",new[]{"mi"}),("む",new[]{"mu"}),("め",new[]{"me"}),("も",new[]{"mo"}),
            ("や",new[]{"ya"}),("ゆ",new[]{"yu"}),("よ",new[]{"yo"}),
            ("ら",new[]{"ra"}),("り",new[]{"ri"}),("る",new[]{"ru"}),("れ",new[]{"re"}),("ろ",new[]{"ro"}),
            ("わ",new[]{"wa"}),("を",new[]{"wo","o"}),
            ("あ",new[]{"a"}),("い",new[]{"i"}),("う",new[]{"u"}),("え",new[]{"e"}),("お",new[]{"o"}),
        };

        static bool IsConsonant(char ch) => "bcdfghjklmnpqrstvwxyz".IndexOf(char.ToLower(ch))>=0;

        static string[] RomajiOf(string kana)
        {
            foreach (var t in TABLE) if (t.kana==kana) return t.rom;
            return new[]{kana};
        }

        static List<string> Tokenize(string s)
        {
            var YOON = new HashSet<string>{"きゃ","きゅ","きょ","しゃ","しゅ","しょ","ちゃ","ちゅ","ちょ","じゃ","じゅ","じょ"};
            var toks = new List<string>();
            for(int i=0;i<s.Length;)
            {
                if (i+1<s.Length)
                {
                    var two = s.Substring(i,2);
                    if (YOON.Contains(two)) { toks.Add(two); i+=2; continue; }
                }
                toks.Add(s[i].ToString()); i++;
            }
            return toks;
        }

        public RomajiMatcher(string kana)
        {
            _cands = GenerateCandidates(kana);
        }

        public int Cursor => _idx;
        public string Top => _cands.Count>0 ? _cands[0] : "";

        public (bool accepted, bool advanced) TypeChar(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return (false,false);
            var ch = raw.Normalize().ToLower()[0];

            // 前進一致
            var next = _cands.Where(c => _idx < c.Length && c[_idx]==ch).ToList();
            if (next.Count>0){ _cands = next; _idx++; return (true,true); }

            // 余分な n を吸収（直前がn かつ 次が子音（母音/ y / n 以外））
            if (ch=='n' && _idx>0)
            {
                bool prevWasN = _cands.Any(c => _idx-1 < c.Length && c[_idx-1]=='n');
                if (prevWasN)
                {
                    bool expectingConsonant = _cands.Any(c => _idx < c.Length && IsConsonant(c[_idx]) && "aiueoyn".IndexOf(c[_idx])<0);
                    if (expectingConsonant)
                    {
                        _cands = _cands.Where(c => !(_idx < c.Length && c[_idx]=='n')).ToList();
                        return (true,false);
                    }
                }
            }
            return (false,false);
        }

        public bool IsCompleted() => _cands.Any(c => _idx >= c.Length);

        static List<string> GenerateCandidates(string kana)
        {
            var toks = Tokenize(kana);
            var cands = new List<string>{""};

            for(int i=0;i<toks.Count;i++)
            {
                var t = toks[i];

                // っ
                if (t=="っ")
                {
                    var next = (i+1<toks.Count)? RomajiOf(toks[i+1]) : new[]{""};
                    var heads = next.Select(r=>r[0]).Where(h=>IsConsonant(h)).ToList();
                    if (heads.Count==0) continue;
                    var nx = new List<string>();
                    foreach(var c in cands) foreach(var h in heads) nx.Add(c + h);
                    cands = nx; continue;
                }
                // ー
                if (t=="ー")
                {
                    var nx = new List<string>();
                    foreach(var c in cands) nx.Add(c + "-");
                    cands = nx; continue;
                }
                // ん
                if (t=="ん")
                {
                    string[] next = (i+1<toks.Count)? RomajiOf(toks[i+1]) : Array.Empty<string>();
                    var heads = next.Select(r=>r[0]).Select(char.ToLower).ToList();
                    bool strict = heads.Any(h => "aiueoyn".IndexOf(h)>=0); // 母音/や行/な行/n
                    var opts = strict ? new[]{"nn","n'"} : new[]{"nn","n"};
                    var nx = new List<string>();
                    foreach(var c in cands) foreach(var r in opts) nx.Add(c + r);
                    cands = nx; continue;
                }
                // 通常
                {
                    var roms = RomajiOf(t);
                    var nx = new List<string>();
                    foreach(var c in cands) foreach(var r in roms) nx.Add(c + r);
                    cands = nx;
                }
            }
            return cands.Distinct().ToList();
        }
    }

    public class TypingEngineCore
    {
        public TypingState S;
        private List<Word> _words = new();
        private int _idx = 0;
        private float _elapsedMs = 0f;
        private RomajiMatcher _matcher;

        public TypingEngineCore(float baseSeconds, List<Word> words)
        {
            _words = (words!=null && words.Count>0)? words : new List<Word>{ new Word{ kana="すし", repRomaji="sushi"} };
            var first = _words[0];
            _matcher = new RomajiMatcher(first.kana);
            S = new TypingState{
                timeLeftMs = baseSeconds*1000f,
                score=0, streak=0, finished=false,
                currentKana = first.kana, repRomaji = first.repRomaji,
                activeRomaji = _matcher.Top, romajiCursor = 0,
                typedCount=0, correctCount=0, mistakes=0, wpm=0, accuracy=1
            };
        }

        public void Update(float dtMs)
        {
            if (S.finished) return;
            _elapsedMs += dtMs;
            S.timeLeftMs = Mathf.Max(0, S.timeLeftMs - dtMs);
            if (S.timeLeftMs==0) S.finished = true;
            RecomputeMetrics();
        }

        public void TypeChar(string raw)
        {
            if (S.finished || S.timeLeftMs<=0) return;
            if (string.IsNullOrEmpty(raw)) return;
            char ch = char.ToLower(raw[0]);
            if (ch < 32 || ch > 126) return; // 制御系除外

            S.typedCount++;

            var r = _matcher.TypeChar(raw);
            S.activeRomaji = _matcher.Top;

            if (r.accepted)
            {
                if (r.advanced)
                {
                    S.correctCount++;
                    S.romajiCursor = _matcher.Cursor;
                    S.score += 10;
                }
                if (_matcher.IsCompleted())
                {
                    S.streak++;

                    // 10/20/30到達で+1/+2/+3秒
                    if (S.streak%10==0)
                    {
                        int mult = Mathf.Min(3, S.streak/10);
                        S.timeLeftMs += mult * 1000f;
                        // ここではVFX等は呼ばず、外側のControllerがイベント発火
                    }
                    NextWord();
                }
            }
            else
            {
                S.mistakes++;
                S.streak = 0; // ミスでリセット
            }
            RecomputeMetrics();
        }

        public TypingState State() => S;

        void NextWord()
        {
            _idx = (_idx + 1) % _words.Count;
            var w = _words[_idx];
            _matcher = new RomajiMatcher(w.kana);
            S.currentKana = w.kana;
            S.repRomaji = w.repRomaji;
            S.activeRomaji = _matcher.Top;
            S.romajiCursor = 0;
        }

        void RecomputeMetrics()
        {
            float minutes = Mathf.Max(1e-6f, _elapsedMs/60000f);
            S.wpm = (S.correctCount/5f)/minutes;
            S.accuracy = S.typedCount>0 ? (float)S.correctCount/S.typedCount : 1f;
        }
    }
}
