using UnityEngine;
using BossRaid.Combat;

namespace BossRaid.Combat.Classes
{
    public class Rogue : CharacterBase
    {
        protected override void Start()
        {
            base.Start();
            role = CharacterRole.MeleeDPS;
            maxHealth = 1200f;
            currentHealth = maxHealth;
            characterName = "Rogue";
        }

        public override void UseSkill(int skillIndex)
        {
            switch (skillIndex)
            {
                case 0: KickInterrupt(); break;
                case 1: DeadlyPoison(); break;
                case 2: Shadowstep(); break;
            }
        }

        public override void UseUltimate()
        {
            ShadowDance();
        }

        private void KickInterrupt() => Debug.Log("[Rogue] Kick! Damage & Interrupt.");
        private void DeadlyPoison() => Debug.Log("[Rogue] Deadly Poison! 8s DOT damage.");
        private void Shadowstep() => Debug.Log("[Rogue] Shadowstep! Teleport to boss back.");
        private void ShadowDance() => Debug.Log("[Rogue] Shadow Dance! 2x Attack speed & Untargetable.");
    }
}
