using UnityEngine;
using System.Collections;
using BossRaid.Managers;

namespace BossRaid.Combat.Boss
{
    public class IdleBoss : MonoBehaviour
    {
        public float maxHP = 1000f;
        public float currentHP;
        private bool isDead = false;

        public int bossLevel = 1; // 서버로 보낼 현재 보스의 레벨
        public int goldPerOnePercent = 10; 
        private float nextRewardHpThreshold;

        private Animator animator;
        private Collider bossCollider;

        private void Awake()
        {
            animator = GetComponentInChildren<Animator>();
            bossCollider = GetComponent<Collider>();
        }

        private void Start() => InitializeBoss();

        public void InitializeBoss()
        {
            currentHP = maxHP;
            isDead = false;
            if (bossCollider != null) bossCollider.enabled = true;
            nextRewardHpThreshold = 0.99f; 

            if (animator != null)
            {
                animator.Rebind();
                animator.SetTrigger("Idle");
            }
        }

        public void TakeDamage(float damage)
        {
            if (isDead) return;

            currentHP -= damage;
            if (currentHP < 0) currentHP = 0;

            float currentHpPercent = currentHP / maxHP;

            // 1% 깎일 때마다 타격감(UI)을 위해 가짜 보상만 지급 (서버 통신 안함)
            while (currentHpPercent <= nextRewardHpThreshold && nextRewardHpThreshold >= 0f)
            {
                if (GrowthManager.Instance != null)
                {
                    GrowthManager.Instance.AddFakeGold(goldPerOnePercent);
                }
                nextRewardHpThreshold -= 0.01f;
            }

            if (currentHP <= 0 && !isDead)
            {
                Die();
            }
        }

        private async void Die()
        {
            isDead = true;
            if (bossCollider != null) bossCollider.enabled = false;
            if (animator != null) animator.SetTrigger("Die");

            // [보안 핵심] 보스가 죽었을 때 딱 1번 서버와 통신하여 모든 보상을 한 번에 정산
            if (GrowthManager.Instance != null)
            {
                await GrowthManager.Instance.ClaimBossRewardFromServer(bossLevel);
            }

            StartCoroutine(RespawnRoutine());
        }

        private IEnumerator RespawnRoutine()
        {
            yield return new WaitForSeconds(1.0f);
            InitializeBoss();
        }
    }
}
