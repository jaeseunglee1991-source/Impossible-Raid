using UnityEngine;
using System;

namespace BossRaid.Combat
{
    public enum CharacterRole { Tank, MeleeDPS, RangedDPS, Healer }

    public abstract class CharacterBase : MonoBehaviour
    {
        [Header("Base Stats")]
        public string characterName;
        public CharacterRole role;
        public float maxHealth = 1000f;
        public float currentHealth;
        
        [Header("Combat Stats")]
        public float movementSpeed = 5f;
        public float attackRange = 2f;
        public float attackSpeed = 1f; // Seconds per attack

        [Header("Threat (Aggro) State")]
        public float currentThreat = 0f;
        public bool IsDead => currentHealth <= 0;

        protected virtual void Start()
        {
            currentHealth = maxHealth;
        }

        public virtual void Heal(float amount)
        {
            if (IsDead) return;
            currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        }

        public void AddThreat(float amount)
        {
            if (IsDead) return;
            currentThreat += amount;
        }

        public virtual void TakeDamage(float amount)
        {
            currentHealth -= amount;
            if (currentHealth <= 0) Die();
        }

        protected virtual void Die()
        {
            Debug.Log($"{characterName} has died.");
        }

        public abstract void UseSkill(int skillIndex);
        public abstract void UseUltimate();
    }
}
