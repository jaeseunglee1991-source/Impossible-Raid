using UnityEngine;
using System.Collections;

namespace BossRaid.Combat.Classes
{
    public class Healer : CharacterBase
    {
        protected override void Awake()
        {
            base.Awake();
            role          = CharacterRole.Healer;
            characterName = "힐러";
            maxHealth     = 900f;
            autoAttackDamage = 25f;
            attackSpeed   = 1.2f;
            attackRange   = 5.0f;
            threatMultiplier = 0.2f; // 힐러: 치유/피해의 20% 어그로 (매우 낮음)

            RegisterSkills(
                new SkillDefinition("치유의 빛",   4f, 0,
                    desc: "가장 체력이 낮은 아군을 공격력 5배만큼 즉시 회복"),
                new SkillDefinition("평온의 기도",   8f, 1,
                    desc: "5초간 파티 전체에 초당 공격력 50% 지속 회복"),
                new SkillDefinition("보호의 성역",   15f, 2,
                    desc: "아군 1명에게 3초간 받는 피해 50% 감소 및 상태이상 제거")
            );
            RegisterUltimate(new SkillDefinition("여신의 가호", 35f, 0, ultimate: true,
                desc: "파티 전체 체력 100% 회복 + 3초간 무적 (최후의 보루)"));
                
            // [패시브] 평온의 오라: 주기적으로 미량 회복
            StartCoroutine(SerenityAuraPassive());
        }

        public override void UseSkill(int idx)
        {
            switch (idx)
            {
                case 0: // 치유의 빛
                    CharacterBase target = FindLowestHPAlly();
                    if (target != null)
                    {
                        float healAmt = autoAttackDamage * 5.0f;
                        target.Heal(healAmt, this);
                        Debug.Log($"<color=green>[힐러] 치유의 빛! {target.characterName} +{healAmt:F0}</color>");
                    }
                    if (targetBoss != null) DealDamageTo(targetBoss, autoAttackDamage);
                    break;

                case 1: // 평온의 기도
                    StartCoroutine(PrayerOfSerenity());
                    Debug.Log("<color=green>[힐러] 평온의 기도! 파티 도트 힐 시작</color>");
                    break;

                case 2: // 보호의 성역
                    CharacterBase targetAlly = FindLowestHPAlly();
                    if (targetAlly != null)
                    {
                        StartCoroutine(SanctuaryEffect(targetAlly, 3f));
                        Debug.Log($"<color=white>[힐러] 보호의 성역! {targetAlly.characterName} 피해 격감</color>");
                    }
                    break;
            }
        }

        public override void UseUltimate()
        {
            ApplyPartyBuff(ally =>
            {
                ally.Heal(ally.maxHealth, this);
                ally.SetInvulnerable(3f);
            });
            Debug.Log("<color=yellow>[힐러] 궁극기: 여신의 가호! 파티 전체 풀힐 + 3초 무적!</color>");
        }

        private IEnumerator SerenityAuraPassive()
        {
            while (true)
            {
                yield return new WaitForSeconds(2f);
                if (!IsDead) ApplyPartyBuff(ally => ally.Heal(autoAttackDamage * 0.2f, this));
            }
        }

        private IEnumerator PrayerOfSerenity()
        {
            for (int i = 0; i < 5; i++)
            {
                ApplyPartyBuff(ally => ally.Heal(autoAttackDamage * 0.5f, this));
                yield return new WaitForSeconds(1f);
            }
        }

        private IEnumerator SanctuaryEffect(CharacterBase ally, float duration)
        {
            float prevRed = ally.damageReductionMultiplier;
            ally.damageReductionMultiplier = 0.5f; // 50% 감소
            yield return new WaitForSeconds(duration);
            ally.damageReductionMultiplier = prevRed;
        }
    }
}
