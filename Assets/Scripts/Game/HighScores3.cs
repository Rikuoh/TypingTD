using System.Collections.Generic;
using UnityEngine;

namespace TD.Game
{
    /// <summary>上位3スコア管理（名前なし版）</summary>
    public static class HighScores3
    {
        const string K1 = "TD.HS1";
        const string K2 = "TD.HS2";
        const string K3 = "TD.HS3";

        public static int[] GetTop3()
        {
            return new int[]
            {
                PlayerPrefs.GetInt(K1, 0),
                PlayerPrefs.GetInt(K2, 0),
                PlayerPrefs.GetInt(K3, 0),
            };
        }

        /// <summary>スコアを登録して今回の順位(1~)を返す</summary>
        public static int Submit(int score)
        {
            var list = new List<int>(GetTop3());
            list.Add(score);
            list.Sort((a, b) => b.CompareTo(a)); // 降順

            int rank = list.FindIndex(s => s == score) + 1; // 同点は先頭順位

            // 上位3つだけ保存
            PlayerPrefs.SetInt(K1, list[0]);
            PlayerPrefs.SetInt(K2, list.Count > 1 ? list[1] : 0);
            PlayerPrefs.SetInt(K3, list.Count > 2 ? list[2] : 0);
            PlayerPrefs.Save();

            return rank;
        }

        /// <summary>（任意）リセットしたい時用</summary>
        public static void Clear()
        {
            PlayerPrefs.DeleteKey(K1);
            PlayerPrefs.DeleteKey(K2);
            PlayerPrefs.DeleteKey(K3);
            PlayerPrefs.Save();
        }
    }
}
