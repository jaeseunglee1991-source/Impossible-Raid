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
            nextRewardHpThreshold = 0.9f; // 90%부터 10%씩 차감하며 지급

            PlayBossAnimation("Idle");

            // 패턴 루프 시작
            if (_patternCoroutine != null) StopCoroutine(_patternCoroutine);
            _patternCoroutine = StartCoroutine(BossPatternLoop());
            StartCoroutine(AutoAttackLoop());

            // 화면 상단 보스 UI 강제 연동
            if (InGameHUDController.Instance != null)
            {
                InGameHUDController.Instance.ToggleBossFrame(true);
                InGameHUDController.Instance.UpdateBossHealth(currentHP, maxHP, GetBossName());
            }
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

            // 보스 UI 실시간 갱신
            if (InGameHUDController.Instance != null)
                InGameHUDController.Instance.UpdateBossHealth(currentHP, maxHP, GetBossName());

            float currentHpPercent = currentHP / maxHP;

            // 10% 깎일 때마다 큼직한 보상 지급 (보스 사냥의 손맛 연출)
            while (currentHpPercent <= nextRewardHpThreshold && nextRewardHpThreshold > 0f)
            {
                if (GrowthManager.Instance != null && StageManager.Instance != null)
                {
                    int stageLvl = StageManager.Instance.CurrentStageLevel;
                    
                    // 잡몹이면 잡몹 골드, 보스면 보스 골드 기준
                    double totalReward = isSpawnedMinion ? 
                        GrowthManager.Instance.CalculateMobGold(stageLvl) : 
                        GrowthManager.Instance.CalculateBossGold(stageLvl);

                    // 10% 분량 지급
                    GrowthManager.Instance.AddGold(totalReward * 0.1);
                    Debug.Log($"<color=yellow>[보상] 몬스터 체력 {Mathf.RoundToInt(nextRewardHpThreshold * 100 + 10)}% 돌파! 금화 10% 획득.</color>");
                }
                nextRewardHpThreshold -= 0.1f;
            }

            if (currentHP <= 0 && !isDead)
            {
                // 보스 사망 시 UI 정리
                if (InGameHUDController.Instance != null && isSpawnedMinion)
                    InGameHUDController.Instance.ToggleBossFrame(false);

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
                // ─── 잡몹 사망: 마지막 10% 정산 및 마무리 ───
                int stageLvl = StageManager.Instance != null ? StageManager.Instance.CurrentStageLevel : bossLevel;
                double totalMobGold = GrowthManager.Instance != null ? GrowthManager.Instance.CalculateMobGold(stageLvl) : 10.0;

                // 마지막 10% 지급 (이미 90%는 틱으로 지급됨)
                if (GrowthManager.Instance != null)
                    GrowthManager.Instance.AddGold(totalMobGold * 0.1);

                if (StageManager.Instance != null)
                    StageManager.Instance.OnMobKilled(0); // 이미 골드는 위에서 줬으므로 0 전달

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
                yield return new WaitForSeconds(2.5f); // 패턴 간 간격 단축 (더 보스답게)
            }
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
