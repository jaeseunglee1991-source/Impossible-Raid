using UnityEngine;
using System.Collections;
using BossRaid.Combat;

namespace BossRaid.Combat.Boss
{
    public class BossAI : MonoBehaviour
    {
        [Header("Boss Stats")]
        public string bossName = "Arclight";
        public float maxHealth = 50000f;
        public float currentHealth;
        public float autoAttackDamage = 180f;

        [Header("Combat State")]
        private bool isPhaseTwo = false;
        public System.Collections.Generic.List<CharacterBase> activePlayers = new System.Collections.Generic.List<CharacterBase>();
        public CharacterBase currentTarget;

        public void InitializeBattle(System.Collections.Generic.List<CharacterBase> players)
        {
            activePlayers = players;
            currentHealth = maxHealth;
            StartCoroutine(BossPatternLoop());
            StartCoroutine(AutoAttackLoop());
        }

        private CharacterBase GetHighestAggroTarget()
        {
            CharacterBase highest = null;
            float maxThreat = -1f;
            foreach (var p in activePlayers)
            {
                if (!p.IsDead && p.currentThreat > maxThreat)
                {
                    highest = p; maxThreat = p.currentThreat;
                }
            }
            return highest;
        }

        private IEnumerator AutoAttackLoop()
        {
            while (currentHealth > 0)
            {
                currentTarget = GetHighestAggroTarget();
                if (currentTarget == null)
                {
                    Debug.Log($"<color=red>[{bossName}] 전멸! 생존한 플레이어가 없습니다. 라이프 1 감소.</color>");
                    yield break;
                }

                currentTarget.TakeDamage(autoAttackDamage);
                Debug.Log($"<color=orange>[어그로 타격] {bossName} 가 {currentTarget.characterName} 을(를) 공격! (-{autoAttackDamage} HP) " + 
                          $"(남은 HP: {currentTarget.currentHealth}, 현재 어그로: {currentTarget.currentThreat})</color>");

                yield return new WaitForSeconds(2.0f); // 2초마다 평타
            }
        }

        private IEnumerator BossPatternLoop()
        {
            while (currentHealth > 0)
            {
                Debug.Log($"[{bossName}] 0-10s: Slam Pattern");
                yield return new WaitForSeconds(10f);
                Debug.Log($"[{bossName}] 10-15s: Jump Smash Pattern");
                yield return new WaitForSeconds(5f);
                Debug.Log($"[{bossName}] 20-24s: Mana Explosion! Interrupt REQUIRED.");
                yield return new WaitForSeconds(9f);
                
                if (!isPhaseTwo && (currentHealth / maxHealth) <= 0.4f)
                {
                    EnterPhaseTwo();
                }
            }
        }

        private void EnterPhaseTwo()
        {
            isPhaseTwo = true;
            Debug.Log($"[{bossName}] Phase 2: Overload! Constant AOE Damage.");
        }

        public void TakeDamage(float amount)
        {
            currentHealth -= amount;
            if (currentHealth <= 0) Debug.Log($"{bossName} has been defeated!");
        }

        [ContextMenu("Test Take Damage")]
        public void TestDamage() => TakeDamage(1000f);

        [ContextMenu("Force Phase 2")]
        public void TestPhase2() => EnterPhaseTwo();
    }
}
