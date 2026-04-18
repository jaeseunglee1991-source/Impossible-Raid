using UnityEngine;

namespace BossRaid.Data
{
    [CreateAssetMenu(fileName = "NewSkillData", menuName = "BossRaid/Data/Skill Data")]
    public class SkillData : ScriptableObject
    {
        [Header("스킬 기본 정보")]
        public string skillName = "New Skill";
        [TextArea] public string description = "스킬 설명입니다.";
        
        [Header("전투 수치")]
        public float cooldownSeconds = 5f;
        public float range = 2f;
        public float damageAmount = 50f;
        public float healingAmount = 0f;
    }
}
