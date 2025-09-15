using UnityEngine;
using TD.Typing;
using System.Collections.Generic;

namespace TD.TDCore
{
    public class TowerManager : MonoBehaviour
    {
        [Header("Build Spots & Prefabs")]
        public Transform[] buildSlots;
        public Tower towerPrefab;

        [Header("Milestones (OK語数しきい値)")]
        public int[] addTowerAt = new int[] { 3, 10, 17 };
        public int[] upgradeAt = new int[] { 6, 13, 20 };

        private int _okCount;
        private int _placedCount;
        private readonly HashSet<int> _appliedAdd = new();
        private readonly HashSet<int> _appliedUpg = new();

        private void OnEnable()
        {
            TypingEvents.WordOk += OnWordOk;
        }
        private void OnDisable()
        {
            TypingEvents.WordOk -= OnWordOk;
        }

        private void OnWordOk(WordOkEvent e)
        {
            _okCount++;
            // Add
            for (int i = 0; i < addTowerAt.Length; i++)
            {
                if (_okCount >= addTowerAt[i] && !_appliedAdd.Contains(addTowerAt[i]))
                {
                    _appliedAdd.Add(addTowerAt[i]);
                    PlaceNextTower();
                }
            }
            // Upgrade (既存タワー全体を1レベルUP)
            for (int i = 0; i < upgradeAt.Length; i++)
            {
                if (_okCount >= upgradeAt[i] && !_appliedUpg.Contains(upgradeAt[i]))
                {
                    _appliedUpg.Add(upgradeAt[i]);
                    UpgradeAll();
                }
            }
        }

        private void PlaceNextTower()
        {
            if (towerPrefab == null || buildSlots == null || _placedCount >= buildSlots.Length) return;
            var slot = buildSlots[_placedCount];
            var tower = Instantiate(towerPrefab, slot.position, Quaternion.identity);
            _placedCount++;
        }

        private void UpgradeAll()
        {
            foreach (var t in FindObjectsOfType<Tower>())
            {
                t.Upgrade();
            }
        }
    }
}