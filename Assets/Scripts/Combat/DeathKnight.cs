using UnityEngine;
using System.Collections;
using BossRaid.Combat.Boss;

namespace BossRaid.Combat.Classes
{
    public class DeathKnight : CharacterBase
    {
        private int boneShieldCharges = 0;

        protected override void Awake()
        {
            base.Awake();
            role = CharacterRole.Tank;
            maxHealth = 2000f; currentHealth = maxHealth;
            characterName = "죽음의 기사";
            attackRange = 3f; autoAttackDamage = 50f; attackSpeed = 1.5f;
            skillNames = new string[] { "죽음의 손아귀", "죽음의 일격", "뼈 보호막" };
            skillCooldowns = new float[] { 12f, 6f, 15f };
            ultimateName = "사자의 군대"; ultimateCooldown = 75f;
        }

        public override void UseSkill(int idx)
        {
            switch (idx)
            {
                case 0: // 죽음의 손아귀 (차단) - 마법 피해 + 끌어당기기 + 차단 + 어그로
                    if (targetBoss != null) { targetBoss.TakeDamage(180f); targetBoss.Interrupt(); AddThreat(400f); }
                    Debug.Log("<color=blue>[죽음의 기사] 죽음의 손아귀! 차단 + 어그로 획득</color>");
                    break;
                case 1: // 죽음의 일격 - 피해 + 50% 흡혈
                    float dmg = 300f * attackPowerMultiplier;
                    if (targetBoss != null) { targetBoss.TakeDamage(dmg); AddThreat(dmg * 0.3f); }
                    Heal(dmg * 0.5f);
                    Debug.Log("<color=red>[죽음의 기사] 죽음의 일격! 흡혈 회복</color>");
                    break;
                case 2: // 뼈 보호막 - 3회 공격 방어
                    boneShieldCharges = 3;
                    shieldAmount = 600f;
                    Debug.Log("<color=cyan>[죽음의 기사] 뼈 보호막! 3회 방어</color>");
                    break;
            }
        }

        public override void UseUltimate()
        {
            StartCoroutine(ArmyOfTheDead());
            Debug.Log("<color=yellow>[죽음의 기사] 사자의 군대! 구울 소환!</color>");
        }

        private IEnumerator ArmyOfTheDead()
        {
            // 10초간 추가 DPS + 어그로 분산
            float elapsed = 0f;
            while (elapsed < 10f)
            {
                if (targetBoss != null) targetBoss.TakeDamage(30f); // 구울 3마리 DPS
                elapsed += 1f;
                yield return new WaitForSeconds(1f);
            }
        }
    }
}
