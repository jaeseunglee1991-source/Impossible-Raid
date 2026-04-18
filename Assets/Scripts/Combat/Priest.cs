using UnityEngine;
using System.Collections;
using BossRaid.Combat.Boss;

namespace BossRaid.Combat.Classes
{
    public class Priest : CharacterBase
    {
        protected override void Awake()
        {
            base.Awake();
            role = CharacterRole.Healer;
            maxHealth = 1000f; currentHealth = maxHealth;
            characterName = "사제";
            attackRange = 10f; autoAttackDamage = 20f; attackSpeed = 1.5f;
            RegisterSkills(
                new SkillDefinition("신성한 일격", 6f, 0, desc: "보스 피해 + 50% 스마트 힐"),
                new SkillDefinition("순간 치유", 3f, 1, desc: "가장 체력 낮은 아군 대폭 회복"),
                new SkillDefinition("보호막", 10f, 2, desc: "아군 1명 400 피해 흡수 보호막")
            );
            RegisterUltimate(new SkillDefinition("천상의 찬가", 70f, 0, ultimate: true, desc: "파티 전체 대폭 지속 회복"));
        }

        public override void UseSkill(int idx)
        {
            switch (idx)
            {
                case 0: // 신성한 일격 - 보스 피해 + 50% 스마트 힐
                    float dmg = 180f * attackPowerMultiplier;
                    if (targetBoss != null) { targetBoss.TakeDamage(dmg); AddThreat(20f); }
                    var lowest = FindLowestHPAlly();
                    if (lowest != null) lowest.Heal(dmg * 0.5f);
                    Debug.Log("<color=yellow>[사제] 신성한 일격! 피해+스마트 힐</color>");
                    break;
                case 1: // 순간 치유 - 가장 체력 낮은 아군 대폭 회복
                    var target = FindLowestHPAlly();
                    if (target != null) target.Heal(600f);
                    Debug.Log("<color=green>[사제] 순간 치유! 대폭 회복</color>");
                    break;
                case 2: // 신의 권능: 보호막 - 아군 1명 보호막
                    var shieldTarget = FindLowestHPAlly();
                    if (shieldTarget != null) shieldTarget.shieldAmount += 400f;
                    Debug.Log("<color=cyan>[사제] 보호막! 400 피해 흡수</color>");
                    break;
            }
        }

        public override void UseUltimate()
        {
            StartCoroutine(DivineHymn());
            Debug.Log("<color=yellow>[사제] 천상의 찬가! 파티 전체 대폭 회복!</color>");
        }

        private IEnumerator DivineHymn()
        {
            // 4초간 파티 전체 지속 회복
            for (int i = 0; i < 8; i++)
            {
                HealAllParty(150f);
                yield return new WaitForSeconds(0.5f);
            }
        }
    }
}
