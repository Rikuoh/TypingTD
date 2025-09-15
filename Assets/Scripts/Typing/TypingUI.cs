// using UnityEngine;
// using TMPro;

// public class TypingUI : MonoBehaviour
// {
//     public TMP_Text kanaText;
//     public TMP_Text romajiText;

//     private string currentKana = "すし";
//     private string currentRomaji = "sushi";
//     private int cursor = 0;

//     void Update()
//     {
//         // 入力チェック
//         foreach (char c in Input.inputString)
//         {
//             if (cursor < currentRomaji.Length && char.ToLower(c) == currentRomaji[cursor])
//             {
//                 cursor++;
//             }
//         }

//         // お題表示
//         kanaText.text = currentKana;

//         // 入力済み部分を緑、残りを灰色で表示
//         string hit = currentRomaji.Substring(0, cursor);
//         string rest = currentRomaji.Substring(cursor);
//         romajiText.text = $"<color=#00ff00>{hit}</color><color=#888888>{rest}</color>";
//     }
// }
