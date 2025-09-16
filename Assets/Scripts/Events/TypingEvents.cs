using System;

namespace TD.Typing
{
    public struct WordOkEvent { public int scoreDelta; public int streak; public WordOkEvent(int scoreDelta,int streak){ this.scoreDelta=scoreDelta; this.streak=streak; } }
    public struct StreakTierEvent { public int tier; public StreakTierEvent(int tier){this.tier=tier;} }
    public struct BonusTimeEvent { public int seconds; public BonusTimeEvent(int seconds){this.seconds=seconds;} }
    public struct MistakeEvent { public int streak; public MistakeEvent(int streak){this.streak=streak;} }
    public struct TickEvent { public float timeLeftMs; public TickEvent(float timeLeftMs){this.timeLeftMs=timeLeftMs;} }

    public static class TypingEvents
    {
        public static event Action<WordOkEvent> WordOk;
        public static event Action<StreakTierEvent> StreakTierUp;
        public static event Action<BonusTimeEvent> BonusTime;
        public static event Action<MistakeEvent> Mistake;
        public static event Action<TickEvent> Tick;

        public static void RaiseWordOk(int scoreDelta, int streak) => WordOk?.Invoke(new WordOkEvent { scoreDelta = scoreDelta, streak =streak });
        public static void RaiseStreakTier(int tier) => StreakTierUp?.Invoke(new StreakTierEvent(tier));
        public static void RaiseBonusTime(int seconds) => BonusTime?.Invoke(new BonusTimeEvent(seconds));
        public static void RaiseMistake(int streak) => Mistake?.Invoke(new MistakeEvent(streak));
        public static void RaiseTick(float timeLeftMs) => Tick?.Invoke(new TickEvent { timeLeftMs = timeLeftMs });
    }
}