using UnityEngine;
using BossRaid.Combat;

namespace BossRaid.Combat.Classes
{
    public class IceMage : CharacterBase
    {
        protected override void Start()
        {
            base.Start();
            role = CharacterRole.RangedDPS;
            maxHealth = 1000f;
            currentHealth = maxHealth;
            characterName = "Ice Mage";
        }

        public override void UseSkill(int skillIndex)
        {
            switch (skillIndex)
            {
                case 0: IceLance(); break;
                case 1: Frostbolt(); break;
                case 2: IceBarrier(); break;
            }
        }

        public override void UseUltimate()
        {
            IceBlock();
        }

        private void IceLance() => Debug.Log("[Ice Mage] Ice Lance! Freeze (Interrupt) & Damage.");
        private void Frostbolt() => Debug.Log("[Ice Mage] Frostbolt! Slow 20%.");
        private void IceBarrier() => Debug.Log("[Ice Mage] Ice Barrier! Absorbing damage for 8s.");
        private void IceBlock() => Debug.Log("[Ice Mage] Ice Block! Invulnerable & Stun self.");
    }
}
