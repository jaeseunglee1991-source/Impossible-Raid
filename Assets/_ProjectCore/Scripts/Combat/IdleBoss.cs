using UnityEngine;
using System.Collections;
using BossRaid.Managers;

namespace BossRaid.Combat.Boss
{
    public class IdleBoss : MonoBehaviour, IBossPatternHandler
    {
        public string GetBossName() => "일반 몬스터";
        public bool IsCasting() => isCasting;
        public float maxHP = 3550f; // 목표 처치 시간 10초 (파티 DPS 355 기준)
        public float currentHP;
        private bool isDead = false;

        public int bossLevel = 1; 
        public int goldPerOnePercent = 10; 
        private float nextRewardHpThreshold;

        [Header("Boss Patterns")]
        public float autoAttackDamage = 30f;
        public float autoAttackInterval = 2.0f;
        private Coroutine _patternCoroutine;
        private int _lastPatternIndex = -1;

        [Header("Casting State")]
        public bool isCasting = false;
        public string currentCastName = "";
        public float currentCastProgress = 0f;
        public bool canBeInterrupted = true;

        private Animator animator;
        private SPUM_Prefabs spumPrefab;
        private Collider bossCollider;

        private void Awake()
        {
            animator = GetComponentInChildren<Animator>();
            spumPrefab = GetComponentInChildren<SPUM_Prefabs>();
            bossCollider = GetComponent<Collider>();
            if (bossCollider == null) bossCollider = GetComponentInChildren<Collider>();
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

            // 패턴 루프 시작
            if (_patternCoroutine != null) StopCoroutine(_patternCoroutine);
            _patternCoroutine = StartCoroutine(BossPatternLoop());
            StartCoroutine(AutoAttackLoop());
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
            if (_patternCoroutine != null) StopCoroutine(_patternCoroutine);
            yield return new WaitForSeconds(1.0f);
            InitializeBoss();
        }

        // ═══════════════════════════════════════════════════════════
        //  패턴 시스템 (Belthazar 패턴과 동일한 로직)
        // ═══════════════════════════════════════════════════════════

        private IEnumerator AutoAttackLoop()
        {
            while (!isDead)
            {
                if (isCasting) { yield return null; continue; }

                CharacterBase target = GetRandomTarget();
                if (target != null)
                {
                    if (spumPrefab != null) spumPrefab.PlayAnimation(PlayerState.ATTACK, 0);
                    else if (animator != null) animator.SetTrigger("AttackTrigger");
                    
                    target.TakeDamage(autoAttackDamage);
                }
                else
                {
                    yield return new WaitForSeconds(0.5f);
                    continue;
                }
                yield return new WaitForSeconds(autoAttackInterval);
            }
        }

        private IEnumerator BossPatternLoop()
        {
            yield return new WaitForSeconds(2f); // 스폰 후 여유시간

            while (!isDead)
            {
                int patternIndex;
                do { patternIndex = Random.Range(0, 5); } 
                while (patternIndex == _lastPatternIndex);
                _lastPatternIndex = patternIndex;

                yield return StartCoroutine(ExecutePattern(patternIndex));
                yield return new WaitForSeconds(4f); // 패턴 간 간격
            }
        }

        private IEnumerator ExecutePattern(int index)
        {
            switch (index)
            {
                case 0: yield return StartCoroutine(PatternMagmaburst()); break;
                case 1: yield return StartCoroutine(PatternHellfire()); break;
                case 2: yield return StartCoroutine(PatternMoltenfuse()); break;
                case 3: yield return StartCoroutine(PatternFlameburst()); break;
                case 4: yield return StartCoroutine(PatternFlameturbine()); break;
            }
        }

        // --- 패턴 0: Magmaburst ---
        private IEnumerator PatternMagmaburst()
        {
            yield return StartCoroutine(CastPattern("Magmaburst", 1.5f, true));
            if (!isCasting) // 시전 완료
            {
                for (int i = 0; i < 3; i++)
                {
                    CharacterBase target = GetRandomTarget();
                    if (target != null)
                    {
                        yield return new WaitForSeconds(0.5f);
                        target.TakeDamage(40f);
                        Debug.Log($"<color=red>[패턴] Magmaburst 탄환 적중! {target.characterName}</color>");
                    }
                }
            }
        }

        // --- 패턴 1: Hellfire ---
        private IEnumerator PatternHellfire()
        {
            yield return StartCoroutine(CastPattern("Hellfire", 4.0f, true));
            if (!isCasting)
            {
                CharacterBase target = GetRandomTarget();
                if (target != null)
                {
                    for (int i = 0; i < 5; i++)
                    {
                        target.TakeDamage(30f);
                        yield return new WaitForSeconds(1f);
                    }
                }
            }
        }

        // --- 패턴 2: Moltenfuse ---
        private IEnumerator PatternMoltenfuse()
        {
            CharacterBase target = GetRandomTarget();
            if (target == null) yield break;
            Debug.Log($"<color=yellow>[패턴] Moltenfuse 마킹! -> {target.characterName}</color>");
            yield return new WaitForSeconds(3f);
            if (target != null) target.TakeDamage(80f);
        }

        // --- 패턴 3: Flameburst ---
        private IEnumerator PatternFlameburst()
        {
            if (animator != null) animator.SetTrigger("AttackTrigger");
            yield return new WaitForSeconds(0.5f);
            foreach(var p in FindObjectsByType<CharacterBase>(FindObjectsSortMode.None))
            {
                p.TakeDamage(30f);
            }
            Debug.Log("<color=red>[패턴] Flameburst 방사!</color>");
        }

        // --- 패턴 4: Flameturbine ---
        private IEnumerator PatternFlameturbine()
        {
            Debug.Log("<color=red>[패턴] Flameturbine 회전 화염!</color>");
            for (int i = 0; i < 5; i++)
            {
                foreach(var p in FindObjectsByType<CharacterBase>(FindObjectsSortMode.None))
                {
                    if (Random.value < 0.3f) p.TakeDamage(20f);
                }
                yield return new WaitForSeconds(1f);
            }
        }

        // 캐스팅 시스템
        private IEnumerator CastPattern(string patternName, float duration, bool interruptable)
        {
            isCasting = true;
            currentCastName = patternName;
            canBeInterrupted = interruptable;
            float elapsed = 0f;
            while (elapsed < duration && isCasting)
            {
                elapsed += Time.deltaTime;
                currentCastProgress = elapsed / duration;
                yield return null;
            }
            isCasting = false;
        }

        public void Interrupt()
        {
            if (isCasting && canBeInterrupted)
            {
                isCasting = false;
                Debug.Log("<color=cyan>[차단] 몬스터의 패턴을 차단했습니다!</color>");
            }
        }

        private CharacterBase GetRandomTarget()
        {
            var players = FindObjectsByType<CharacterBase>(FindObjectsSortMode.None);
            if (players.Length == 0) return null;
            return players[Random.Range(0, players.Length)];
        }
    }
}
