using UnityEngine;
using TD.Typing;

public class TypingDummyInput : MonoBehaviour
{
    private int streak = 0;

    void Update()
    {
        // キー入力をチェック
        foreach (char c in Input.inputString)
        {
            if (char.IsLetter(c)) // アルファベットだけを対象
            {
                streak++;
                TypingEvents.RaiseWordOk(10, streak);

                // 10/20/30 で TIER UP & ボーナス
                if (streak == 10) { TypingEvents.RaiseStreakTier(1); TypingEvents.RaiseBonusTime(1); }
                else if (streak == 20) { TypingEvents.RaiseStreakTier(2); TypingEvents.RaiseBonusTime(2); }
                else if (streak == 30) { TypingEvents.RaiseStreakTier(3); TypingEvents.RaiseBonusTime(3); }
            }
            else if (c == ' ') // スペースキーでミス扱い
            {
                streak = 0;
                TypingEvents.RaiseMistake(0);
            }
        }
    }
}
