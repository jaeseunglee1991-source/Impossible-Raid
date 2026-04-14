using UnityEngine;
using System.Collections;
using BossRaid.Combat.Boss;

namespace BossRaid.Combat.Classes
{
    public class IceMage : CharacterBase
    {
        protected override void Awake()
        {
            base.Awake();
            role = CharacterRole.RangedDPS;
            maxHealth = 1000f; currentHealth = maxHealth;
            characterName = "냉기 마법사";
            attackRange = 10f; autoAttackDamage = 35f; attackSpeed = 1.3f;
            skillNames = new string[] { "얼음창", "얼음 화살", "얼음 보호막" };
            skillCooldowns = new float[] { 15f, 4f, 18f };
            ultimateName = "얼음 덩어리"; ultimateCooldown = 60f;
        }

        public override void UseSkill(int idx)
        {
            switch (idx)
            {
                case 0: // 얼음창 (차단) - 관통 피해 + 1초 빙결
                    if (targetBoss != null) { targetBoss.TakeDamage(280f * attackPowerMultiplier); targetBoss.Interrupt(); AddThreat(60f); }
                    Debug.Log("<color=cyan>[냉기 마법사] 얼음창! 차단!</color>");
                    break;
                case 1: // 얼음 화살 - 피해 + 둔화
                    if (targetBoss != null) { targetBoss.TakeDamage(130f * attackPowerMultiplier); AddThreat(30f); }
                    Debug.Log("[냉기 마법사] 얼음 화살! 둔화 적용");
                    break;
                case 2: // 얼음 보호막 - 8초 피해 흡수
                    shieldAmount = 500f;
                    Debug.Log("<color=cyan>[냉기 마법사] 얼음 보호막! 500 피해 흡수</color>");
                    break;
            }
        }

        public override void UseUltimate()
        {
            StartCoroutine(IceBlock());
            Debug.Log("<color=yellow>[냉기 마법사] 얼음 덩어리! 5초 무적!</color>");
        }

        private IEnumerator IceBlock()
        {
            isInvulnerable = true;
            movementSpeed = 0f;
            yield return new WaitForSeconds(5f);
            isInvulnerable = false;
            movementSpeed = 5f;
        }
    }
}
