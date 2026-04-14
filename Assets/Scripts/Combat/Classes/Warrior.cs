using UnityEngine;
using System.Collections;
using BossRaid.Combat.Boss;
using BossRaid.Managers;

namespace BossRaid.Combat.Classes
{
    public class Warrior : CharacterBase
    {
        protected override void Awake()
        {
            base.Awake();
            role = CharacterRole.Tank;
            characterName = "전사";
            
            // 기초 스탯 (GrowthManager에서 계속 더해짐)
            maxHealth = 2000f; 
            currentHealth = maxHealth;
            autoAttackDamage = 50f; 
            attackSpeed = 1.0f; 
            attackRange = 3f;
            
            skillNames = new string[] { "회전 베기 (광역)", "전장의 함성 (탱킹/어그로)", "" };
            skillCooldowns = new float[] { 5f, 0f, 0f }; 
            
            ultimateName = "불굴의 도발 (궁극기 생존)";
            ultimateCooldown = 15f; 
        }

        // Skill 1: 방치형 파밍에 특화된 회전 베기 (광역기)
        public override void UseSkill(int idx)
        {
            if (idx == 0)
            {
                float damage = autoAttackDamage * 2.5f; // 평타 2.5배 계수
                Debug.Log($"<color=orange>[전사] 회전 베기! 주변 적에게 {damage} 광역 피해!</color>");
                
                // 보스에게 적용
                if (targetBoss != null) 
                { 
                    targetBoss.TakeDamage(damage); 
                    AddThreat(damage * 2f); // 탱커 레이드 기믹: 도발 수치 2배
                }

                // TODO: 방치형 몹(StageManager.IdleMobSpawner) 주변 광역 타격 로직 추가
            }
        }

        // Skill 2 (Ultimate): 보스 레이드 태그용 필살기 (생존 및 폭딜)
        public override void UseUltimate()
        {
            Debug.Log($"<color=yellow>[전사] 불굴의 도발! 5초간 받는 피해 50% 감소 및 자신의 최대 체력의 20% 보호막 생성!</color>");
            
            // 레이드 탱커형 궁극기 기믹
            shieldAmount += maxHealth * 0.2f; // 체력 비례 쉴드
            StartCoroutine(DamageReductionBuff(5f));
            
            if (targetBoss != null)
            {
                // 차단(Interrupt) 기믹 추가 (가능한 경우)
                targetBoss.Interrupt();
                AddThreat(5000f); // 보스 어그로 즉시 획득
            }
        }

        private IEnumerator DamageReductionBuff(float duration)
        {
            damageReductionMultiplier = 0.5f; // 50% 피해 감소
            yield return new WaitForSeconds(duration);
            damageReductionMultiplier = 1.0f; // 원래대로 복구
        }
    }
}
