using UnityEngine;
using System;
using BossRaid.Managers;

namespace BossRaid.Combat
{
    /// <summary>
    /// ──────────────────────────────────────────────────────────────────
    /// StatUpgrade — 개별 스탯의 레벨, 요구 골드량, 능력치 증가를 계산하는 클래스
    /// ──────────────────────────────────────────────────────────────────
    /// </summary>
    [System.Serializable] // 유니티 인스펙터 창에서 수치를 직접 수정할 수 있도록 허용
    public class StatUpgrade
    {
        [Header("성장 기본 정보")]
        public string statName = "공격력 강화";
        public int currentLevel = 1;
        public Action OnUpgrade; // 강화 성공 시 캐릭터에게 알림

        [Header("비용 밸런싱")]
        public double baseCost = 10f;         // 1->2 레벨업 시 필요한 기본 골드
        public double costMultiplier = 1.15f; // 레벨업 할 때마다 비싸지는 비율 (1.15 = 15% 증가)

        [Header("스탯 밸런싱")]
        public double baseStat = 10f;              // 1레벨일 때의 기본 공격력
        public double statIncreasePerLevel = 2.5f; // 1레벨 오를 때마다 추가되는 공격력

        /// <summary>
        /// 현재 레벨에 따른 최종 능력치를 계산하여 반환합니다.
        /// 공식: 기본스탯 + ((현재레벨 - 1) * 렙당증가량)
        /// </summary>
        public float CurrentStat 
        {
            get { return (float)(baseStat + (currentLevel - 1) * statIncreasePerLevel); }
        }

        /// <summary>
        /// 다음 레벨로 가기 위해 필요한 골드를 계산하여 반환합니다.
        /// 공식: 기본비용 * (증가비율 ^ (현재레벨 - 1))
        /// </summary>
        public double NextUpgradeCost 
        {
            get { return baseCost * Math.Pow(costMultiplier, currentLevel - 1); }
        }

        /// <summary>
        /// 골드를 소비하여 실제 레벨업을 시도합니다. (UI 버튼 클릭 시 호출됨)
        /// </summary>
        public bool TryUpgrade()
        {
            double cost = NextUpgradeCost;
            
            // GrowthManager(재화 매니저)에서 골드를 성공적으로 차감했다면
            if (GrowthManager.Instance != null && GrowthManager.Instance.SpendGold(cost))
            {
                currentLevel++;
                OnUpgrade?.Invoke(); // 스탯 동기화 유도
                Debug.Log($"[{statName}] 레벨업 성공! Lvl.{currentLevel} / 최종 스탯: {CurrentStat} / 다음 필요 골드: {NextUpgradeCost:F0}");
                
                // 강화 레벨 변경 → 저장 예약 (2초 후 일괄 저장)
                SaveManager.Instance?.MarkDirty();
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
