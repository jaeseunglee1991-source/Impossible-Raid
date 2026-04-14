using UnityEngine;
using System.Collections;
using BossRaid.Combat.Boss;

namespace BossRaid.Combat.Classes
{
    public class FireMage : CharacterBase
    {
        private int burnStacks = 0;

        protected override void Awake()
        {
            base.Awake();
            role = CharacterRole.RangedDPS;
            maxHealth = 1000f; currentHealth = maxHealth;
            characterName = "화염 마법사";
            attackRange = 10f; autoAttackDamage = 35f; attackSpeed = 1.2f;
            skillNames = new string[] { "화염 작렬", "화염구", "불기둥" };
            skillCooldowns = new float[] { 12f, 4f, 10f };
            ultimateName = "발화"; ultimateCooldown = 60f;
        }

        public override void UseSkill(int idx)
        {
            switch (idx)
            {
                case 0: // 화염 작렬 (차단) - 폭발 피해 + 넉백 + 차단
                    if (targetBoss != null) { targetBoss.TakeDamage(320f * attackPowerMultiplier); targetBoss.Interrupt(); AddThreat(80f); }
                    Debug.Log("<color=orange>[화염 마법사] 화염 작렬! 차단!</color>");
                    break;
                case 1: // 화염구 - 피해 + 화상 스택
                    if (targetBoss != null) { targetBoss.TakeDamage(150f * attackPowerMultiplier); burnStacks++; AddThreat(40f); }
                    Debug.Log($"[화염 마법사] 화염구! 화상 스택: {burnStacks}");
                    break;
                case 2: // 불기둥 - 1초 후 폭발 장판
                    StartCoroutine(FlameStrike());
                    Debug.Log("[화염 마법사] 불기둥! 1초 후 폭발!");
                    break;
            }
        }

        public override void UseUltimate()
        {
            // 화상 스택 전부 폭발
            float damage = 100f + (burnStacks * 150f);
            if (targetBoss != null) { targetBoss.TakeDamage(damage); AddThreat(damage * 0.5f); }
            Debug.Log($"<color=yellow>[화염 마법사] 발화! {burnStacks}스택 폭발! {damage} 피해!</color>");
            burnStacks = 0;
        }

        private IEnumerator FlameStrike()
        {
            yield return new WaitForSeconds(1f);
            if (targetBoss != null) { targetBoss.TakeDamage(400f * attackPowerMultiplier); AddThreat(100f); }
        }
    }
}
