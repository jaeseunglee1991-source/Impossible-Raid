using UnityEngine;
using System.Collections;

namespace BossRaid.Combat
{
    [System.Serializable]
    public abstract class SkillBase
    {
        public string skillName;
        public float cooldownSeconds;
        public float currentCooldown;
        public float range;
        public float damageAmount;
        public float healingAmount;

        public bool IsReady => currentCooldown <= 0;

        public virtual void Tick(float deltaTime)
        {
            if (currentCooldown > 0) currentCooldown -= deltaTime;
        }

        public abstract void Execute(CharacterBase user, CharacterBase target);
        
        protected void StartCooldown()
        {
            currentCooldown = cooldownSeconds;
        }
    }
}
