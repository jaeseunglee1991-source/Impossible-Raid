using UnityEngine;
using System.Collections;

namespace BossRaid.Combat.Classes
{
    public class Rogue : CharacterBase
    {
        private bool _isNextAttackBoosted = false;

        protected override void Awake()
        {
            base.Awake();
            role = CharacterRole.MeleeDPS;
            characterName = "도적";
            
            // 기초 스탯 (공속이 빠름)
            maxHealth = 1200f; 
            currentHealth = maxHealth;
            autoAttackDamage = 40f; 
            attackSpeed = 0.5f; // 방치형 특화 엄청 빠른 공속
            attackRange = 2.5f;
            
            skillNames = new string[] { "수리검 난사", "", "" };
            skillCooldowns = new float[] { 4f, 0f, 0f }; 
            
            ultimateName = "그림자 회피 (무적 폭딜)";
            ultimateCooldown = 12f; 
        }

        public override void UseSkill(int idx)
        {
            if (idx == 0) // 일반기: 수리검 난사
            {
                float damage = autoAttackDamage * 1.5f; 
                if (_isNextAttackBoosted) { damage *= 2f; _isNextAttackBoosted = false; }

                Debug.Log($"<color=purple>[도적] 수리검 난사! 투투툭! ({damage} 피해)</color>");
                
                if (targetBoss != null) 
                { 
                    targetBoss.TakeDamage(damage); 
                }
                // TODO: 전방의 다수 몹에게 타격 추가
            }
        }

        public override void UseUltimate()
        {
            Debug.Log($"<color=gray>[도적] 그림자 회피! 1초간 완벽한 무적 상태 돌입. 다음 공격력 2배 증가!</color>");
            
            // 레이드 기믹: 일시적 완벽한 무적 (보스 즉사기 회피용)
            StartCoroutine(DodgeRoutine());
        }

        private IEnumerator DodgeRoutine()
        {
            isInvulnerable = true;
            yield return new WaitForSeconds(1.0f); // 1초 무적 닷지
            isInvulnerable = false;
            
            // 회피 성공 시 다음 스킬 데미지 증가 (버프)
            _isNextAttackBoosted = true;
            
            if (targetBoss != null)
            {
                // 보스에게 회피 직후 카운터 단일 데미지
                float counterDamage = autoAttackDamage * 5f;
                targetBoss.TakeDamage(counterDamage);
                Debug.Log($"<color=purple>[도적] 무적 타임 후 암살 찌르기! {counterDamage} 피해!</color>");
            }
        }
    }
}
