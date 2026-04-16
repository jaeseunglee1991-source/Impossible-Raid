using UnityEngine;
using BossRaid.Combat.Boss;

namespace BossRaid.Combat
{
    /// <summary>
    /// ──────────────────────────────────────────────────────────────────
    /// CharacterBase — 캐릭터의 애니메이션, 상태, 스탯 성장을 관리하는 베이스 클래스
    /// ──────────────────────────────────────────────────────────────────
    /// </summary>
    public class CharacterBase : MonoBehaviour
    {
        [Header("성장 시스템 (인스펙터에서 밸런스 조절 가능)")]
        public StatUpgrade attackPowerUpgrade = new StatUpgrade() 
        { 
            statName = "기본 공격력", 
            baseCost = 10, 
            costMultiplier = 1.15, 
            baseStat = 15, 
            statIncreasePerLevel = 3 
        };

        [Header("전투 속성")]
        public float baseAttackCooldown = 1.0f;
        public bool IsDead = false;

        // TagCharacterController에서 데미지를 줄 때 가져가는 '최종 공격력' 프로퍼티
        // attackPowerUpgrade 클래스에서 계산된 성장치가 실시간으로 반영됩니다.
        public float baseAttackPower 
        {
            get { return attackPowerUpgrade.CurrentStat; }
        }

        private Animator animator;

        private void Awake()
        {
            // 캐릭터 프리팹 내부의 Animator(SPUM 등)를 가져옵니다.
            animator = GetComponentInChildren<Animator>();
        }

        /// <summary>
        /// 상태에 맞는 애니메이션을 재생합니다. (TagCharacterController 연동용)
        /// </summary>
        public void PlayAnimation(string triggerName)
        {
            if (animator == null || IsDead) return;

            // 중복 실행을 막거나 트리거를 초기화하는 등의 세부 로직
            animator.ResetTrigger("Idle");
            animator.ResetTrigger("Move");
            animator.ResetTrigger("Attack");

            animator.SetTrigger(triggerName);
        }

        /// <summary>
        /// (등반 모드 레이드용) 스킬 사용 함수
        /// </summary>
        public void UseSkill(int skillIndex, BossAI targetBoss)
        {
            if (IsDead) return;
            
            Debug.Log($"[CharacterBase] {gameObject.name}가 스킬 {skillIndex}번을 사용했습니다!");
            PlayAnimation("Attack"); // 임시로 공격 모션 재생
            
            // TODO: 실제 스킬 데이터(이펙트 생성, 보스 패턴 캔슬 등) 로직을 추후 여기에 구현
        }

        /// <summary>
        /// 캐릭터가 피해를 입었을 때의 로직
        /// </summary>
        public void TakeDamage(float damage)
        {
            if (IsDead) return;
            
            // TODO: 체력 감소 및 사망 로직 구현
        }

        /// <summary>
        /// UI의 '공격력 강화' 버튼에 OnClick 이벤트로 연결해둘 함수
        /// </summary>
        public void OnClickUpgradeAttackPower()
        {
            attackPowerUpgrade.TryUpgrade();
        }
    }
}
