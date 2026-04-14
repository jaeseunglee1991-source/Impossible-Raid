using UnityEngine;
using System.Collections;

namespace BossRaid.Combat.Classes
{
    public class Healer : CharacterBase
    {
        protected override void Awake()
        {
            base.Awake();
            role = CharacterRole.Healer;
            characterName = "힐러";
            
            // 기초 스탯
            maxHealth = 900f; 
            currentHealth = maxHealth;
            autoAttackDamage = 25f; // 평타 약함
            attackSpeed = 1.2f;     
            attackRange = 5.0f;     
            
            skillNames = new string[] { "치유의 빛 (단일 힐)", "", "" };
            skillCooldowns = new float[] { 3f, 0f, 0f }; 
            
            ultimateName = "신의 가호 (파티 풀피 및 무적 2초)";
            ultimateCooldown = 30f; 
        }

        public override void UseSkill(int idx)
        {
            if (idx == 0) // 치유의 빛: 가장 체력이 낮은 아군 치유
            {
                float healAmount = autoAttackDamage * 4.0f; // 공격력 비례 힐
                
                CharacterBase target = FindLowestHPAlly();
                if (target != null)
                {
                    target.Heal(healAmount);
                    Debug.Log($"<color=green>[힐러] 치유의 빛! {target.characterName}의 체력을 {healAmount} 회복!</color>");
                }
                
                // 보스에게 약간의 스마이트 타격 가능
                if (targetBoss != null) targetBoss.TakeDamage(autoAttackDamage);
            }
        }

        public override void UseUltimate()
        {
            Debug.Log($"<color=yellow>[힐러] 신의 가호! 파티 전체 체력 100% 회복 및 2초간 무적!</color>");
            
            // 레이드 기믹: 파티 전체 퍼펙트 케어
            StartCoroutine(DivineSanctuaryRoutine());
        }

        private IEnumerator DivineSanctuaryRoutine()
        {
            // 모든 아군 체력 즉시 회복 및 무적 버프 부여
            ApplyPartyBuff(ally => 
            {
                ally.Heal(ally.maxHealth); // 풀피 회복
                ally.isInvulnerable = true; 
            });
            
            yield return new WaitForSeconds(2.0f); // 2초 유지
            
            // 무적 버프 해제
            ApplyPartyBuff(ally => 
            {
                ally.isInvulnerable = false;
            });
        }
    }
}
