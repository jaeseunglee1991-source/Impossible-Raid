using UnityEngine;
using BossRaid.Combat;

namespace BossRaid.Combat.Classes
{
    public class DeathKnight : CharacterBase
    {
        protected override void Start()
        {
            base.Start();
            role = CharacterRole.Tank;
            maxHealth = 2000f; // 공격형 탱커 스펙
            currentHealth = maxHealth;
            characterName = "Death Knight";
        }

        public override void UseSkill(int skillIndex)
        {
            switch (skillIndex)
            {
                case 0: DeathGrip(); break;
                case 1: DeathStrike(); break;
                case 2: BoneShield(); break;
            }
        }

        public override void UseUltimate()
        {
            ArmyOfTheDead();
        }

        private void DeathGrip() => Debug.Log("[Death Knight] Death Grip! Pulling boss & Interrupted.");
        private void DeathStrike() => Debug.Log("[Death Knight] Death Strike! Damage & Lifesteal 50%.");
        private void BoneShield() => Debug.Log("[Death Knight] Bone Shield! Blocking 3 attacks.");
        private void ArmyOfTheDead() => Debug.Log("[Death Knight] Army of the Dead! Summoning 3 Ghouls.");
    }
}
