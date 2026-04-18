using UnityEngine;
using System.Collections;

namespace BossRaid.Combat
{
    [System.Serializable]
    public abstract class SkillBase
    {
        public BossRaid.Data.SkillData skillData;
        public float currentCooldown;

        public string skillName => skillData != null ? skillData.skillName : "Unknown";
        public float cooldownSeconds => skillData != null ? skillData.cooldownSeconds : 0f;
        public float range => skillData != null ? skillData.range : 0f;
        public float damageAmount => skillData != null ? skillData.damageAmount : 0f;
        public float healingAmount => skillData != null ? skillData.healingAmount : 0f;

        public bool IsReady => currentCooldown <= 0;

        public virtual void Tick(float deltaTime)
        {
            if (currentCooldown > 0) currentCooldown -= deltaTime;
        }

        public abstract void Execute(CharacterBase user);
        
        protected void StartCooldown()
        {
            currentCooldown = cooldownSeconds;
        }
    }
}
