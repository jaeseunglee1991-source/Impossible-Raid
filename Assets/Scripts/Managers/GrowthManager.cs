using UnityEngine;
using System;

namespace BossRaid.Managers
{
    /// <summary>
    /// 방치형 게임의 핵심인 '재화 소모를 통한 스탯 영구 성장' 매니저입니다.
    /// 유저 요청 사항: "순식간에 경, 해 로 단위가 뻥튀기 되지 않고 1단위 수치 변화로 묵직한 성장을 원함"
    /// 
    /// - 따라서 레벨당 상승폭을 선형(Linear) 구조로 잡았으며,
    /// - 비용 역시 기하급수적 폭발이 아닌 완만한 곡선으로 설정했습니다.
    /// </summary>
    public class GrowthManager : MonoBehaviour
    {
        public static GrowthManager Instance { get; private set; }

        // ──────────────────────────────────────────────
        // 재화 (방치형 필드에서 잡몹을 잡아 획득)
        // ──────────────────────────────────────────────
        public int Gold { get; private set; } = 0;

        // ──────────────────────────────────────────────
        // 업그레이드 레벨 (계정 공통 성장 버프)
        // ──────────────────────────────────────────────
        public int AttackLevel { get; private set; } = 0;
        public int HealthLevel { get; private set; } = 0;
        public int AttackSpeedLevel { get; private set; } = 0;

        // ──────────────────────────────────────────────
        // 1단위 성장 기반 데이터 (기초 설계)
        // ──────────────────────────────────────────────
        // 1레벨 오를 때마다 순수하게 1~10씩 오릅니다.
        private const float ATTACK_INC_PER_LEVEL = 1.5f;   // 레벨당 공격력 1.5 증가
        private const float HEALTH_INC_PER_LEVEL = 10.0f;  // 레벨당 체력 10 증가
        private const float ASPD_INC_PER_LEVEL = 0.005f;   // 레벨당 공속(공격 속도) 0.005 빨라짐

        // ──────────────────────────────────────────────
        // 이벤트
        // ──────────────────────────────────────────────
        /// <summary>스탯이 업그레이드될 때마다 캐릭터들에게 델리게이트 발송</summary>
        public event Action OnGrowthStatsChanged;
        public event Action<int> OnGoldChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            
            // TODO: 실제 게임에서는 여기서 PlayerPrefs나 Supabase에서 레벨별 데이터를 로컬에 불러업니다.
            LoadData();
        }

        // ──────────────────────────────────────────────
        // 재화 획득
        // ──────────────────────────────────────────────
        public void AddGold(int amount)
        {
            Gold += amount;
            OnGoldChanged?.Invoke(Gold);
        }

        // ──────────────────────────────────────────────
        // 비용 계산 (완만한 선형/계단식 곡선)
        // ──────────────────────────────────────────────
        public int GetAttackUpgradeCost() => 10 + (AttackLevel * 5);     // 10, 15, 20...
        public int GetHealthUpgradeCost() => 10 + (HealthLevel * 5);
        public int GetAttackSpeedUpgradeCost() => 50 + (AttackSpeedLevel * 10);

        // ──────────────────────────────────────────────
        // 업그레이드 트리거
        // ──────────────────────────────────────────────
        public bool TryUpgradeAttack()
        {
            int cost = GetAttackUpgradeCost();
            if (Gold >= cost)
            {
                Gold -= cost;
                AttackLevel++;
                OnGoldChanged?.Invoke(Gold);
                OnGrowthStatsChanged?.Invoke();
                return true;
            }
            return false;
        }

        public bool TryUpgradeHealth()
        {
            int cost = GetHealthUpgradeCost();
            if (Gold >= cost)
            {
                Gold -= cost;
                HealthLevel++;
                OnGoldChanged?.Invoke(Gold);
                OnGrowthStatsChanged?.Invoke();
                return true;
            }
            return false;
        }

        // ──────────────────────────────────────────────
        // 합산 보너스 조회 기능 (캐릭터들이 이 값을 가져다 씀)
        // ──────────────────────────────────────────────
        public float GetBonusAttack() => AttackLevel * ATTACK_INC_PER_LEVEL;
        public float GetBonusHealth() => HealthLevel * HEALTH_INC_PER_LEVEL;
        
        /// <summary>attackSpeed는 수치가 낮을수록 빨리 때리는 쿨다운형식이기 때문에 빼줍니다.</summary>
        public float GetBonusAttackSpeedReduction() => AttackSpeedLevel * ASPD_INC_PER_LEVEL;


        // ──────────────────────────────────────────────
        // 저장 로드 (더미)
        // ──────────────────────────────────────────────
        private void LoadData()
        {
            Gold = PlayerPrefs.GetInt("Save_Gold", 100);
            AttackLevel = PlayerPrefs.GetInt("Save_AtkLvl", 0);
            HealthLevel = PlayerPrefs.GetInt("Save_HpLvl", 0);
        }

        // 앱 종료/포커스 잃을 때 저장
        private void OnApplicationQuit()
        {
            PlayerPrefs.SetInt("Save_Gold", Gold);
            PlayerPrefs.SetInt("Save_AtkLvl", AttackLevel);
            PlayerPrefs.SetInt("Save_HpLvl", HealthLevel);
            PlayerPrefs.Save();
        }
    }
}
