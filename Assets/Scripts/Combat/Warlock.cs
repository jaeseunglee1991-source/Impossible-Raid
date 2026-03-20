using UnityEngine;
using BossRaid.Combat;

namespace BossRaid.Combat.Classes
{
    public class Warlock : CharacterBase
    {
        protected override void Start()
        {
            base.Start();
            role = CharacterRole.RangedDPS;
            maxHealth = 1000f;
            currentHealth = maxHealth;
            characterName = "Warlock";
        }

        public override void UseSkill(int skillIndex)
        {
            switch (skillIndex)
            {
                case 0: ShadowBolt(); break;
                case 1: Corruption(); break;
                case 2: DrainLife(); break;
            }
        }

        public override void UseUltimate()
        {
            Infernal();
        }

        private void ShadowBolt() => Debug.Log("[Warlock] Shadow Bolt! Fear & Interrupt.");
        private void Corruption() => Debug.Log("[Warlock] Corruption! 12s DOT damage.");
        private void DrainLife() => Debug.Log("[Warlock] Drain Life! Channel & Heal self.");
        private void Infernal() => Debug.Log("[Warlock] Infernal! Summon Golem & Stun boss.");
    }
}
