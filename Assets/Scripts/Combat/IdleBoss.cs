using UnityEngine;
using System.Collections;

// 프로젝트의 GrowthManager가 있는 네임스페이스를 연결해 줍니다.
// using BossRaid.Managers; 

namespace BossRaid.Combat.Boss
{
    /// <summary>
    /// ──────────────────────────────────────────────────────────────────
    /// IdleBoss — 방치형(Idle Farming) 모드 전용 샌드백 보스
    /// 1% 체력 감소마다 즉각 재화를 지급하며, 처치 시 100% 보너스 후 1초 뒤 부활
    /// ──────────────────────────────────────────────────────────────────
    /// </summary>
    public class IdleBoss : MonoBehaviour
    {
        [Header("보스 스펙 (방치형 동일 스펙 무한반복)")]
        public float maxHP = 1000f;
        public float currentHP;
        private bool isDead = false;

        [Header("재화 보상 설정")]
        public int goldPerOnePercent = 10;   // 1% 깎일 때마다 지급할 재화(골드)량
        private float nextRewardHpThreshold; // 다음 보상을 지급할 체력 비율 (0.99, 0.98 ...)

        // 컴포넌트 참조
        private Animator animator;
        private Collider bossCollider;

        private void Awake()
        {
            // SPUM 캐릭터 구조상 자식 오브젝트에 Animator가 존재함
            animator = GetComponentInChildren<Animator>();
            bossCollider = GetComponent<Collider>();
        }

        private void Start()
        {
            InitializeBoss();
        }

        /// <summary>
        /// 보스의 상태를 초기화하고 처음/부활 상태로 되돌립니다.
        /// </summary>
        public void InitializeBoss()
        {
            currentHP = maxHP;
            isDead = false;
            
            if (bossCollider != null) bossCollider.enabled = true;

            // 100%에서 시작하므로, 첫 보상은 99%(0.99)에 도달할 때 지급
            nextRewardHpThreshold = 0.99f; 

            if (animator != null)
            {
                // SPUM 애니메이션 초기화 및 대기 상태 돌입
                animator.Rebind(); 
                animator.SetTrigger("Idle"); 
            }
        }

        /// <summary>
        /// 플레이어(4인 AI) 캐릭터들이 타격할 때 호출되는 데미지 함수
        /// </summary>
        public void TakeDamage(float damage)
        {
            if (isDead) return;

            currentHP -= damage;
            if (currentHP < 0) currentHP = 0;

            float currentHpPercent = currentHP / maxHP;

            // 1. 체력이 1% 단위 임계치 이하로 떨어졌는지 반복 확인 후 재화 지급
            while (currentHpPercent <= nextRewardHpThreshold && nextRewardHpThreshold >= 0f)
            {
                GiveReward(goldPerOnePercent);
                
                // 다음 보상 목표치를 1% 더 낮게 갱신
                nextRewardHpThreshold -= 0.01f; 
            }

            // 2. 생존 및 피격 애니메이션 처리
            if (currentHP > 0)
            {
                if (animator != null)
                {
                    // SPUM 피격 애니메이션 (경직이 너무 길면 방해되므로 프로젝트에 맞게 주석 해제)
                    // animator.SetTrigger("Damaged"); 
                }
            }
            // 3. 사망 처리
            else 
            {
                Die();
            }
        }

        /// <summary>
        /// 재화 지급 처리 (GrowthManager 연동)
        /// </summary>
        private void GiveReward(int amount)
        {
            // TODO: 주석을 풀고 실제 프로젝트의 GrowthManager 재화 추가 함수로 연결하세요.
            // if (GrowthManager.Instance != null)
            // {
            //     GrowthManager.Instance.AddGold(amount); 
            // }
            
            // 테스트용 디버그 (나중에 지우셔도 됩니다)
            Debug.Log($"[IdleBoss] 체력 1% 깎임! 재화 획득: +{amount} Gold");
        }

        /// <summary>
        /// 보스 사망 시 보너스 지급 및 부활 코루틴 실행
        /// </summary>
        private void Die()
        {
            isDead = true;
            
            // 시체가 타격되지 않도록 콜라이더 비활성화
            if (bossCollider != null) bossCollider.enabled = false;

            // SPUM 사망 애니메이션 재생
            if (animator != null)
            {
                animator.SetTrigger("Die"); 
            }

            // 처치 보너스: 1% 보상의 100배 (총 100% 추가 재화)
            int killBonus = goldPerOnePercent * 100;
            GiveReward(killBonus);
            Debug.Log($"[IdleBoss] 처치 성공! 보너스 재화 획득: +{killBonus} Gold (총 200%)");

            // 1초 후 제자리 즉시 부활
            StartCoroutine(RespawnRoutine());
        }

        private IEnumerator RespawnRoutine()
        {
            // 죽음 모션을 유지하며 1초 대기
            yield return new WaitForSeconds(1.0f);

            // 파괴(Destroy)하지 않고 동일한 오브젝트의 스탯만 초기화하여 재활용 (메모리 최적화)
            InitializeBoss();
            Debug.Log("[IdleBoss] 1초 경과. 방치형 보스 동일 스펙으로 리스폰 완료!");
        }
    }
}
