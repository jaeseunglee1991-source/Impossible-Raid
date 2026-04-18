using UnityEngine;
using System.Collections;
using BossRaid.Combat.Boss;

namespace BossRaid.Combat.Classes
{
    public class Ranger : CharacterBase
    {
        protected override void Awake()
        {
            base.Awake();
            role = CharacterRole.RangedDPS;
            maxHealth = 1000f; currentHealth = maxHealth;
            characterName = "레인저";
            attackRange = 12f; autoAttackDamage = 45f; attackSpeed = 1f;
            RegisterSkills(
                new SkillDefinition("관통의 화살", 12f, 0, interrupt: true, desc: "방어력 무시 + 차단"),
                new SkillDefinition("일제 사격", 8f, 1, desc: "광역 피해"),
                new SkillDefinition("사냥꾼의 징표", 20f, 2, desc: "10초 피해 10% 증가 디버프")
            );
            RegisterUltimate(new SkillDefinition("속사", 50f, 0, ultimate: true, desc: "5초간 초고속 평타"));
        }

        public override void UseSkill(int idx)
        {
            switch (idx)
            {
                case 0: // 관통의 화살 (차단) - 방어력 무시 + 차단
                    if (targetBoss != null) { targetBoss.TakeDamage(280f); targetBoss.Interrupt(); AddThreat(40f); }
                    Debug.Log("<color=orange>[레인저] 관통의 화살! 차단!</color>");
                    break;
                case 1: // 일제 사격 - 광역 피해
                    if (targetBoss != null) { targetBoss.TakeDamage(250f * attackPowerMultiplier); AddThreat(60f); }
                    Debug.Log("[레인저] 일제 사격! 광역 피해");
                    break;
                case 2: // 사냥꾼의 징표 - 10초 피해 10% 증가 디버프
                    if (targetBoss != null) StartCoroutine(HuntersMark());
                    Debug.Log("<color=cyan>[레인저] 사냥꾼의 징표! 10초 피해 10% 증가</color>");
                    break;
            }
        }

        public override void UseUltimate()
        {
            StartCoroutine(RapidFire());
            Debug.Log("<color=yellow>[레인저] 속사! 5초간 초고속 평타!</color>");
        }

        private IEnumerator HuntersMark()
        {
            // 보스 받는 피해 증가 (간접적 구현: 파티 공격력 증가)
            ApplyPartyBuff(p => p.attackPowerMultiplier *= 1.1f);
            yield return new WaitForSeconds(10f);
            ApplyPartyBuff(p => p.attackPowerMultiplier = 1f);
        }

        private IEnumerator RapidFire()
        {
            float orig = attackSpeed;
            attackSpeed = 0.3f; // 매우 빠른 공격
            yield return new WaitForSeconds(5f);
            attackSpeed = orig;
        }
    }
}
