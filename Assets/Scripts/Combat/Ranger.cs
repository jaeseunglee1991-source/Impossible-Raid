using UnityEngine;
using BossRaid.Combat;

namespace BossRaid.Combat.Classes
{
    public class Ranger : CharacterBase
    {
        protected override void Start()
        {
            base.Start();
            role = CharacterRole.RangedDPS;
            maxHealth = 1000f;
            currentHealth = maxHealth;
            characterName = "Ranger";
        }

        public override void UseSkill(int skillIndex)
        {
            switch (skillIndex)
            {
                case 0: PiercingArrow(); break;
                case 1: MultiShot(); break;
                case 2: HuntersMark(); break;
            }
        }

        public override void UseUltimate()
        {
            RapidFire();
        }

        private void PiercingArrow() => Debug.Log("[Ranger] Piercing Arrow! Ignore armor & Silence.");
        private void MultiShot() => Debug.Log("[Ranger] Multi-Shot! AOE rain damage.");
        private void HuntersMark() => Debug.Log("[Ranger] Hunter's Mark! Boss takes 10% more damage.");
        private void RapidFire() => Debug.Log("[Ranger] Rapid Fire! Move & Shoot + Attack Speed up.");
    }
}
