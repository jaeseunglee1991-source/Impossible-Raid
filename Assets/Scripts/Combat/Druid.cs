using UnityEngine;
using BossRaid.Combat;

namespace BossRaid.Combat.Classes
{
    public class Druid : CharacterBase
    {
        protected override void Start()
        {
            base.Start();
            role = CharacterRole.Healer;
            maxHealth = 1000f;
            currentHealth = maxHealth;
            characterName = "Druid";
        }

        public override void UseSkill(int skillIndex)
        {
            switch (skillIndex)
            {
                case 0: Moonfire(); break;
                case 1: Rejuvenation(); break;
                case 2: StampedingRoar(); break;
            }
        }

        public override void UseUltimate()
        {
            Tranquility();
        }

        private void Moonfire() => Debug.Log("[Druid] Moonfire! Magic damage & 10s armor reduction.");
        private void Rejuvenation() => Debug.Log("[Druid] Rejuvenation! 8s HOT heal.");
        private void StampedingRoar() => Debug.Log("[Druid] Stampeding Roar! 6s speed boost for party.");
        private void Tranquility() => Debug.Log("[Druid] Tranquility! Large AOE mobile healing.");
    }
}
