using UnityEngine;
using BossRaid.Combat;

namespace BossRaid.Combat.Classes
{
    public class FireMage : CharacterBase
    {
        protected override void Start()
        {
            base.Start();
            role = CharacterRole.RangedDPS;
            maxHealth = 1000f;
            currentHealth = maxHealth;
            characterName = "Fire Mage";
        }

        public override void UseSkill(int skillIndex)
        {
            switch (skillIndex)
            {
                case 0: FireBlast(); break;
                case 1: Fireball(); break;
                case 2: FlameStrike(); break;
            }
        }

        public override void UseUltimate()
        {
            Combustion();
        }

        private void FireBlast() => Debug.Log("[Fire Mage] Fire Blast! Instant damage & Knockback.");
        private void Fireball() => Debug.Log("[Fire Mage] Fireball! Burn dot stack.");
        private void FlameStrike() => Debug.Log("[Fire Mage] Flame Strike! 1s delayed AOE explosion.");
        private void Combustion() => Debug.Log("[Fire Mage] Combustion! Explode all burn stacks.");
    }
}
