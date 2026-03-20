using UnityEngine;

namespace BossRaid.Combat.Classes
{
    public class Warrior : CharacterBase
    {
        protected override void Start()
        {
            base.Start();
            role = CharacterRole.MeleeDPS;
            maxHealth = 1200f;
            currentHealth = maxHealth;
            characterName = "Warrior";
        }

        public override void UseSkill(int skillIndex)
        {
            switch (skillIndex)
            {
                case 0: ChargeBash(); break;
                case 1: Whirlwind(); break;
                case 2: BattleShout(); break;
            }
        }

        public override void UseUltimate()
        {
            Execute();
        }

        private void ChargeBash() => Debug.Log("[Warrior] Charge Bash! Dashing & Interrupting.");
        private void Whirlwind() => Debug.Log("[Warrior] Whirlwind! AOE physical damage.");
        private void BattleShout() => Debug.Log("[Warrior] Battle Shout! Increasing party attack power.");
        private void Execute() => Debug.Log("[Warrior] Execute! Massive damage to low health boss.");
    }
}
