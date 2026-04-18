using UnityEngine;
using BossRaid.Combat;

namespace BossRaid.Data
{
    [CreateAssetMenu(fileName = "NewCharacterData", menuName = "BossRaid/Data/Character Data")]
    public class CharacterData : ScriptableObject
    {
        [Header("기본 정보")]
        public string characterName = "New Character";
        public CharacterRole role = CharacterRole.MeleeDPS;
        
        [Header("전투 속성")]
        public float maxHealth = 1000f;
        public float autoAttackDamage = 10f;
        public float attackSpeed = 1.0f;
        public float attackRange = 2.0f;
        public float baseAttackCooldown = 1.0f;
        public float movementSpeed = 5f;
    }
}
