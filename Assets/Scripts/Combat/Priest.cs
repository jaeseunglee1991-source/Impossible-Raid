using UnityEngine;

namespace BossRaid.Combat.Classes
{
    public class Priest : CharacterBase
    {
        protected override void Start()
        {
            base.Start();
            role = CharacterRole.Healer;
            maxHealth = 1000f;
            currentHealth = maxHealth;
            characterName = "Priest";
        }

        public override void UseSkill(int skillIndex)
        {
            switch (skillIndex)
            {
                case 0: HolySmite(); break;
                case 1: FlashHeal(); break;
                case 2: PowerWordShield(); break;
            }
        }

        public override void UseUltimate()
        {
            DivineHymn();
        }

        private void HolySmite() => Debug.Log("[Priest] Holy Smite! Damage & Smart Heal.");
        private void FlashHeal() => Debug.Log("[Priest] Flash Heal! Rapid single target heal.");
        private void PowerWordShield() => Debug.Log("[Priest] Power Word: Shield! Absorbing damage.");
        private void DivineHymn() => Debug.Log("[Priest] Divine Hymn! Massive party-wide healing.");
    }
}
