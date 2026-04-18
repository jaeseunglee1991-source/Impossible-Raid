using UnityEngine;
using System.Collections;
using BossRaid.Combat.Boss;

namespace BossRaid.Combat.Classes
{
    public class Warlock : CharacterBase
    {
        protected override void Awake()
        {
            base.Awake();
            role = CharacterRole.RangedDPS;
            maxHealth = 1000f; currentHealth = maxHealth;
            characterName = "흑마법사";
            attackRange = 10f; autoAttackDamage = 30f; attackSpeed = 1.4f;
            RegisterSkills(
                new SkillDefinition("어둠의 화살", 14f, 0, interrupt: true, desc: "암흑 피해 + 차단"),
                new SkillDefinition("부패", 6f, 1, desc: "12초 지속 피해"),
                new SkillDefinition("생명력 흡수", 10f, 2, desc: "채널링 피해 + 자힐")
            );
            RegisterUltimate(new SkillDefinition("지옥불 정령", 80f, 0, ultimate: true, desc: "운석 낙하 + 정령 소환"));
        }

        public override void UseSkill(int idx)
        {
            switch (idx)
            {
                case 0: // 어둠의 화살 (차단) - 암흑 피해 + 공포 + 차단
                    if (targetBoss != null) { targetBoss.TakeDamage(250f * attackPowerMultiplier); targetBoss.Interrupt(); AddThreat(50f); }
                    Debug.Log("<color=purple>[흑마법사] 어둠의 화살! 차단!</color>");
                    break;
                case 1: // 부패 - 12초 DOT
                    if (targetBoss != null) StartCoroutine(Corruption());
                    Debug.Log("<color=green>[흑마법사] 부패! 12초 지속 피해</color>");
                    break;
                case 2: // 생명력 흡수 - 채널링 피해 + 자힐
                    StartCoroutine(DrainLife());
                    Debug.Log("<color=red>[흑마법사] 생명력 흡수!</color>");
                    break;
            }
        }

        public override void UseUltimate()
        {
            StartCoroutine(InfernalSummon());
            Debug.Log("<color=yellow>[흑마법사] 지옥불 정령! 운석 낙하 + 정령 소환!</color>");
        }

        private IEnumerator Corruption()
        {
            for (int i = 0; i < 12; i++)
            {
                if (targetBoss != null) { targetBoss.TakeDamage(35f * attackPowerMultiplier); AddThreat(8f); }
                yield return new WaitForSeconds(1f);
            }
        }

        private IEnumerator DrainLife()
        {
            for (int i = 0; i < 5; i++)
            {
                float dmg = 50f * attackPowerMultiplier;
                if (targetBoss != null) { targetBoss.TakeDamage(dmg); AddThreat(10f); }
                Heal(dmg * 0.8f);
                yield return new WaitForSeconds(0.5f);
            }
        }

        private IEnumerator InfernalSummon()
        {
            // 운석 충돌 (초기 피해)
            if (targetBoss != null) targetBoss.TakeDamage(400f);
            // 15초간 정령 DPS
            float elapsed = 0f;
            while (elapsed < 15f)
            {
                if (targetBoss != null) targetBoss.TakeDamage(25f);
                elapsed += 1f;
                yield return new WaitForSeconds(1f);
            }
        }
    }
}
