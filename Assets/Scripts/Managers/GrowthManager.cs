using UnityEngine;
using System;

namespace BossRaid.Managers
{
    /// <summary>
    /// ──────────────────────────────────────────────────────────────────
    /// GrowthManager — 방치형 게임의 재화(Gold) 획득 및 소비를 관리하는 매니저
    /// ──────────────────────────────────────────────────────────────────
    /// </summary>
    public class GrowthManager : MonoBehaviour
    {
        public static GrowthManager Instance { get; private set; }

        [Header("재화 정보")]
        [Tooltip("방치형 게임이므로 수치가 기하급수적으로 커질 것을 대비해 double 사용")]
        public double currentGold = 0;

        // 골드량이 변경될 때마다 UI를 업데이트하기 위한 이벤트
        public event Action<double> OnGoldChanged;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                // 씬이 넘어가도 재화가 유지되도록 설정 (선택 사항)
                // DontDestroyOnLoad(gameObject); 
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// 보스가 데미지를 입거나 죽었을 때 호출되어 골드를 획득합니다.
        /// </summary>
        public void AddGold(double amount)
        {
            currentGold += amount;
            OnGoldChanged?.Invoke(currentGold);
        }

        /// <summary>
        /// 스탯 강화 버튼을 눌렀을 때 골드를 소비합니다. (성공 시 true 반환)
        /// </summary>
        public bool SpendGold(double amount)
        {
            if (currentGold >= amount)
            {
                currentGold -= amount;
                OnGoldChanged?.Invoke(currentGold);
                return true; // 지불 성공
            }
            
            // 잔액 부족
            Debug.Log("[GrowthManager] 골드가 부족하여 강화할 수 없습니다.");
            return false; 
        }

        // TODO: 향후 Supabase 연동 시, 게임 시작/종료 시점에 currentGold를 DB에 저장하고 불러오는 로직을 여기에 추가하세요.
    }
}
