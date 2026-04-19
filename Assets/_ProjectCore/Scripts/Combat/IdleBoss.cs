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

        [Header("Spawner")]
        [HideInInspector] public bool isSpawnedMinion = false; // 스포너가 생성한 잡몹 여부

        private ICharacterAnimationHandler animHandler;
        private Animator animator;
        private Collider bossCollider;

        private void Awake()
        {
            animHandler = GetComponent<ICharacterAnimationHandler>();
            animator = GetComponentInChildren<Animator>();
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

            PlayBossAnimation("Idle");

            // 패턴 루프 시작
            if (_patternCoroutine != null) StopCoroutine(_patternCoroutine);
            _patternCoroutine = StartCoroutine(BossPatternLoop());
            StartCoroutine(AutoAttackLoop());
        }

        // 공통 애니메이션 실행 헬퍼
        private void PlayBossAnimation(string stateName, int index = 0)
        {
            if (animHandler != null)
            {
                animHandler.PlayAnimation(stateName, index);
                return;
            }

            if (animator != null)
            {
                animator.SetTrigger(stateName);
            }
        }

        public void TakeDamage(float damage)
        {
            if (isDead) return;

            currentHP -= damage;
            if (currentHP < 0) currentHP = 0;

            float currentHpPercent = currentHP / maxHP;

            // 1% 깎일 때마다 타격감(UI)을 위해 보상 지급
            while (currentHpPercent <= nextRewardHpThreshold && nextRewardHpThreshold >= 0f)
            {
                if (GrowthManager.Instance != null && StageManager.Instance != null)
                {
                    // 공식 기반의 보스 보상에서 1% 분량만 쪼개서 지급
                    double bossGold = GrowthManager.Instance.CalculateBossGold(StageManager.Instance.CurrentStageLevel);
                    GrowthManager.Instance.AddGold(bossGold * 0.01);
                }
                nextRewardHpThreshold -= 0.01f;
            }

            if (currentHP <= 0 && !isDead)
            {
                Die();
            }
        }

        private void Die()
        {
            isDead = true;
            if (bossCollider != null) bossCollider.enabled = false;
            PlayBossAnimation("Die");

            if (isSpawnedMinion)
            {
                // ─── 잡몹 사망: 클라이언트 로컬만 처리, 서버 호출 절대 금지 ───
                int stageLvl = StageManager.Instance != null ? StageManager.Instance.CurrentStageLevel : bossLevel;
                double calculatedMobGold = 10.0; // 기본값
                
                if (GrowthManager.Instance != null)
                    calculatedMobGold = GrowthManager.Instance.CalculateMobGold(stageLvl);

                if (StageManager.Instance != null)
                    StageManager.Instance.OnMobKilled((int)calculatedMobGold);

                // GrowthManager에 로컬 골드 적립
                if (GrowthManager.Instance != null)
                    GrowthManager.Instance.AddGold(calculatedMobGold);

                Destroy(gameObject, 1.5f);
            }
            else
            {
                // ─── 진짜 보스 사망: 서버 정산은 StageManager.OnBossDefeated()에서 처리 ───
                // 서버 응답을 받고 나서 스테이지를 넘기는 것이 중요 (돈 복사 방지)
                if (StageManager.Instance != null)
                    StageManager.Instance.OnBossDefeated();
            }
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
                    PlayBossAnimation("Attack");
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
            PlayBossAnimation("Attack");
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
