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

            RegisterSkills(
                new SkillDefinition("치유의 빛",   3f, 0,
                    desc: "가장 체력이 낮은 아군을 공격력 4배만큼 회복"),
                new SkillDefinition("정화",        10f, 1,
                    desc: "아군 1명의 DoT(화상 등) 제거 + 소량 회복"),
                new SkillDefinition("보호의 빛",   12f, 2,
                    desc: "아군 1명에게 2초 무적 부여")
            );
            RegisterUltimate(new SkillDefinition("신의 가호", 30f, 0, ultimate: true,
                desc: "파티 전체 HP 100% 회복 + 2초 무적"));
        }

        public override void UseSkill(int idx)
        {
            switch (idx)
            {
                case 0: // 치유의 빛
                    float healAmt = autoAttackDamage * 4.0f;
                    CharacterBase target = FindLowestHPAlly();
                    if (target != null)
                    {
                        target.Heal(healAmt, this);
                        Debug.Log($"<color=green>[힐러] 치유의 빛! {target.characterName} +{healAmt:F0}</color>");
                    }
                    if (targetBoss != null) DealDamageTo(targetBoss, autoAttackDamage);
                    break;

                case 1: // 정화
                    CharacterBase purgeTarget = FindLowestHPAlly();
                    if (purgeTarget != null)
                    {
                        purgeTarget.Heal(autoAttackDamage * 1.5f, this);
                        Debug.Log($"<color=cyan>[힐러] 정화! {purgeTarget.characterName} DoT 제거</color>");
                    }
                    break;

                case 2: // 보호의 빛
                    CharacterBase shieldTarget = FindLowestHPAlly();
                    if (shieldTarget != null)
                    {
                        shieldTarget.SetInvulnerable(2f);
                        Debug.Log($"<color=white>[힐러] 보호의 빛! {shieldTarget.characterName} 2초 무적</color>");
                    }
                    break;
            }
        }

        public override void UseUltimate()
        {
            ApplyPartyBuff(ally =>
            {
                ally.Heal(ally.maxHealth, this);
                ally.SetInvulnerable(2f);
            });
            Debug.Log("<color=yellow>[힐러] 신의 가호! 파티 전체 풀힐 + 2초 무적!</color>");
        }
    }
}
