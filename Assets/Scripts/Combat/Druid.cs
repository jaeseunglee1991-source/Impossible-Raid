using UnityEngine;
using System.Collections;
using BossRaid.Combat.Boss;

namespace BossRaid.Combat.Classes
{
    public class Druid : CharacterBase
    {
        protected override void Awake()
        {
            base.Awake();
            role = CharacterRole.Healer;
            maxHealth = 1000f; currentHealth = maxHealth;
            characterName = "드루이드";
            attackRange = 10f; autoAttackDamage = 25f; attackSpeed = 1.5f;
            skillNames = new string[] { "달빛 섬광", "재생", "쇄도의 포효" };
            skillCooldowns = new float[] { 8f, 4f, 25f };
            ultimateName = "평온"; ultimateCooldown = 60f;
        }

        public override void UseSkill(int idx)
        {
            switch (idx)
            {
                case 0: // 달빛 섬광 - 보스 피해 + 10초 방어력 15% 감소
                    if (targetBoss != null) { targetBoss.TakeDamage(200f * attackPowerMultiplier); AddThreat(30f); }
                    StartCoroutine(MoonfireDebuff());
                    Debug.Log("<color=purple>[드루이드] 달빛 섬광! 방어력 15% 감소</color>");
                    break;
                case 1: // 재생 - 8초 HoT
                    var target = FindLowestHPAlly();
                    if (target != null) StartCoroutine(Rejuvenation(target));
                    Debug.Log("<color=green>[드루이드] 재생! 8초 지속 회복</color>");
                    break;
                case 2: // 쇄도의 포효 - 6초 이동속도 30% 증가
                    StartCoroutine(StampedingRoar());
                    Debug.Log("<color=cyan>[드루이드] 쇄도의 포효! 6초 이동속도 30% 증가</color>");
                    break;
            }
        }

        public override void UseUltimate()
        {
            StartCoroutine(Tranquility());
            Debug.Log("<color=yellow>[드루이드] 평온! 파티 전체 지속 회복!</color>");
        }

        private IEnumerator MoonfireDebuff()
        {
            // 파티 공격력 증가로 간접 구현 (방어력 감소 효과)
            ApplyPartyBuff(p => p.attackPowerMultiplier *= 1.15f);
            yield return new WaitForSeconds(10f);
            ApplyPartyBuff(p => p.attackPowerMultiplier = 1f);
        }

        private IEnumerator Rejuvenation(CharacterBase target)
        {
            for (int i = 0; i < 8; i++)
            {
                if (target != null && !target.IsDead) target.Heal(80f);
                yield return new WaitForSeconds(1f);
            }
        }

        private IEnumerator StampedingRoar()
        {
            ApplyPartyBuff(p => p.movementSpeed *= 1.3f);
            yield return new WaitForSeconds(6f);
            ApplyPartyBuff(p => p.movementSpeed = 5f);
        }

        private IEnumerator Tranquility()
        {
            for (int i = 0; i < 10; i++)
            {
                HealAllParty(120f);
                yield return new WaitForSeconds(0.5f);
            }
        }
    }
}
