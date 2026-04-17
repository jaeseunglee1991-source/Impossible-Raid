using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using BossRaid.Combat;

namespace BossRaid.Combat.Boss
{
    /// <summary>
    /// Belthazar, Lord of Flame — 보스 AI
    /// 원작 Impossible Bosses WC3 위키 기반 충실 구현
    /// 
    /// ■ 에너지 시스템: 12.5/초 재생 → 100 도달 시 궁극기 "Devouring Flames"
    /// ■ 5가지 일반 기술 무작위 루프: Magmaburst, Hellfire, Moltenfuse, Flameburst, Flameturbine
    /// ■ 궁극기 "Devouring Flames": 중앙 텔레포트 → 용암 장판 → 화염 구체 → 호 강타 → 연쇄 폭발 (14초)
    /// ■ 페이즈: 50% 이하 → 격노(에너지 재생 2배), 20% 이하 → 3페이즈(지속 DoT + DPS 체크)
    /// ■ 카운터(차단): Magmaburst(1.5초), Hellfire(4.5초) 시전 중 차단 가능
    /// </summary>
    public class BossAI : MonoBehaviour
    {
        [Header("Boss Stats")]
        public string bossName = "Belthazar, Lord of Flame";
        public float maxHealth = 50000f;
        public float currentHealth;
        public float autoAttackDamage = 180f;
        public float autoAttackInterval = 2.0f;

        // [Optimization] 보스용 헬스 이벤트 (UI 연동 전용)
        public event System.Action<float, float> OnHealthChanged;

        [Header("Energy System (Belthazar)")]
        public float maxEnergy = 100f;
        public float currentEnergy = 0f;
        public float baseEnergyRegenRate = 12.5f;  // 원작 기준 12.5/초
        public float currentEnergyRegenRate = 12.5f;
        public bool isUltimateActive = false;
        public event System.Action<float, float> OnEnergyChanged;

        [Header("Combat State")]
        public bool isPhaseTwo = false;
        public bool isPhaseThree = false;
        public bool isEnraged = false;
        public float currentShield = 0f;
        public bool hasDpsCheckShield = false;
        public List<CharacterBase> activePlayers = new List<CharacterBase>();
        public CharacterBase currentTarget;

        [Header("Casting")]
        public bool isCasting = false;
        public string currentCastName = "";
        public float currentCastDuration = 0f;
        public float currentCastProgress = 0f;
        public bool canBeInterrupted = true;

        [Header("Stagger")]
        public float maxStagger = 1000f;
        public float currentStagger = 0f;
        public bool isStaggered = false;
        public float staggerDuration = 3f;

        [Header("UI (Distribution Optimized)")]
        public UI.BossWorldHealthBar worldHealthBar;
        private Animator _animator;
        private Transform _visualPart;
        private Vector3 _startLocalPos;
        private Vector3 _arenaCenter;       // 궁극기용 아레나 중앙 좌표
        private const string WORLD_HP_BAR_PATH = "UI/BossWorldHPBar";
        
        private Coroutine _patternCoroutine;
        private int _lastPatternIndex = -1;  // 같은 패턴 연속 방지

        // ═══════════════════════════════════════════════════════════
        //  초기화
        // ═══════════════════════════════════════════════════════════

        public void InitializeBattle(List<CharacterBase> players)
        {
            activePlayers = players;
            currentHealth = maxHealth;
            currentStagger = 0f;
            isStaggered = false;
            isPhaseTwo = false;
            isPhaseThree = false;
            isEnraged = false;
            
            currentEnergy = 0f;
            currentEnergyRegenRate = baseEnergyRegenRate;
            isUltimateActive = false;
            _lastPatternIndex = -1;
            _arenaCenter = new Vector3(transform.position.x, transform.position.y, 0f); // 2D: Z=0 고정

            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            OnEnergyChanged?.Invoke(currentEnergy, maxEnergy);

            SetupWorldUI();

            _patternCoroutine = StartCoroutine(BossPatternLoop());
            StartCoroutine(EnergyRegenLoop());
            StartCoroutine(AutoAttackLoop());
        }

        private void SetupWorldUI()
        {
            if (worldHealthBar == null) worldHealthBar = GetComponentInChildren<UI.BossWorldHealthBar>();
            
            if (worldHealthBar == null)
            {
                GameObject hpBarObj = new GameObject($"{bossName}_WorldHPBar");
                hpBarObj.transform.SetParent(null);
                hpBarObj.transform.position = transform.position + new Vector3(0f, 1.2f, -1f); // 2D: Y위, Z카메라앞
                worldHealthBar = hpBarObj.AddComponent<UI.BossWorldHealthBar>();
                Debug.Log($"<color=green>[{bossName}] 월드 HP바 동적 생성 완료!</color>");
            }

            worldHealthBar.Setup(this);

            _animator = GetComponentInChildren<Animator>();
            if (_animator != null)
            {
                _animator.SetBool("InCombat", true);
                _visualPart = _animator.transform;
                _startLocalPos = _visualPart.localPosition;
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  어그로 & 타겟팅 (원작: Taunt > 최고 DPS/힐러)
        // ═══════════════════════════════════════════════════════════

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

        private CharacterBase GetRandomAliveTarget()
        {
            var alive = activePlayers.FindAll(p => !p.IsDead);
            if (alive.Count == 0) return null;
            return alive[Random.Range(0, alive.Count)];
        }

        /// <summary>원작: 보스 200범위 밖에 있는 영웅 타겟 (Magmaburst용)</summary>
        private CharacterBase GetTargetOutsideRange(float range)
        {
            var candidates = activePlayers.FindAll(p => 
                !p.IsDead && Vector2.Distance(new Vector2(transform.position.x, transform.position.y), new Vector2(p.transform.position.x, p.transform.position.y)) > range);
            if (candidates.Count == 0) return GetRandomAliveTarget(); // 모두 근접이면 랜덤
            return candidates[Random.Range(0, candidates.Count)];
        }

        // ═══════════════════════════════════════════════════════════
        //  평타 루프
        // ═══════════════════════════════════════════════════════════

        private IEnumerator AutoAttackLoop()
        {
            while (currentHealth > 0)
            {
                if ((CombatManager.Instance != null && !CombatManager.Instance.isGameActive) 
                    || isStaggered || isCasting || isUltimateActive) 
                { 
                    yield return null; 
                    continue; 
                }

                currentTarget = GetHighestAggroTarget();
                if (currentTarget == null)
                {
                    Debug.Log($"<color=red>[{bossName}] 전멸! 생존한 플레이어가 없습니다.</color>");
                    if (CombatManager.Instance != null && !CombatManager.Instance.IsPartyWiping) 
                        CombatManager.Instance.OnPartyWipe();
                        
                    yield return new WaitForSeconds(2.0f);
                    continue;
                }

                if (_animator != null) _animator.SetTrigger("AttackTrigger");
                currentTarget.TakeDamage(autoAttackDamage);

                Debug.Log($"<color=orange>[어그로 타격] {bossName} → {currentTarget.characterName} " + 
                          $"(-{autoAttackDamage} HP, 남은: {currentTarget.currentHealth})</color>");

                yield return new WaitForSeconds(autoAttackInterval);
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  에너지 시스템 (원작: 12.5/초, 격노 시 2배)
        // ═══════════════════════════════════════════════════════════

        private IEnumerator EnergyRegenLoop()
        {
            while (currentHealth > 0)
            {
                if ((CombatManager.Instance != null && !CombatManager.Instance.isGameActive) 
                    || isStaggered || isUltimateActive) 
                { 
                    yield return null; 
                    continue; 
                }

                if (currentEnergy < maxEnergy)
                {
                    currentEnergy += currentEnergyRegenRate * Time.deltaTime;
                    if (currentEnergy >= maxEnergy)
                    {
                        currentEnergy = maxEnergy;
                        TriggerUltimate();
                    }
                    OnEnergyChanged?.Invoke(currentEnergy, maxEnergy);
                }

                yield return null;
            }
        }

        private void TriggerUltimate()
        {
            if (isUltimateActive) return;
            isUltimateActive = true;
            Debug.Log($"<color=magenta>[{bossName}] 에너지 100%! 궁극기 준비!</color>");

            if (_patternCoroutine != null) StopCoroutine(_patternCoroutine);
            _patternCoroutine = StartCoroutine(UltimateDevouringFlames());
        }

        // ═══════════════════════════════════════════════════════════
        //  궁극기: Devouring Flames (원작 충실 구현)
        //  
        //  원작 시퀀스:
        //  1) 3초 딜레이 (무적 전환) 
        //  2) 아레나 중앙으로 텔레포트
        //  3) 중앙 용암 장판 생성 (초당 1000 피해)
        //  4) 화염 구체(Fire Orbs) 스폰 (대기)
        //  5) 가장 많은 영웅이 있는 방향으로 호 강타 (1500 피해 + 화상 750/5초)
        //  6) 불꽃 구체 연쇄 폭발 (구체 접촉 시 800 피해)
        //  7) 14초 후 종료, 에너지 0 리셋
        // ═══════════════════════════════════════════════════════════

        private IEnumerator UltimateDevouringFlames()
        {
            Debug.Log($"<color=magenta>[{bossName}] ═══ 궁극기: Devouring Flames 준비! (3초 대기, 무적) ═══</color>");
            
            if (_animator != null) _animator.SetBool("Casting", true);
            
            // Phase 0: 3초 딜레이 (원작: 무적 상태 진입)
            yield return new WaitForSeconds(3f);

            // Phase 1: 아레나 중앙으로 텔레포트
            Debug.Log($"<color=magenta>[{bossName}] 아레나 중앙으로 텔레포트!</color>");
            transform.position = new Vector3(0f, 0f, 0f);

            // Phase 2: 중앙 용암 장판 - 14초간 진행
            Debug.Log($"<color=magenta>[{bossName}] ★ Devouring Flames 발동! ★</color>");
            Debug.Log($"<color=red>[System] 🔥 중앙에 거대한 용암 장판 생성! 접근 시 초당 1000 피해!</color>");

            float totalDuration = 14f;
            float elapsed = 0f;
            float lavaDotTimer = 0f;        // 용암 장판 틱
            float orbSpawnDelay = 2f;       // 2초 후 구체 스폰
            float arcSmashDelay = 5f;       // 5초 후 호 강타
            float chainDetonateDelay = 8f;  // 8초 후 연쇄 폭발
            bool orbsSpawned = false;
            bool arcSmashed = false;
            bool chainDetonated = false;
            float lavaRange = 4f;           // 중앙 용암 장판 반경

            while (elapsed < totalDuration && currentHealth > 0)
            {
                elapsed += Time.deltaTime;
                lavaDotTimer += Time.deltaTime;

                // ── 중앙 용암 장판 DoT (초당 1000 피해, 근접한 영웅만) ──
                if (lavaDotTimer >= 1f)
                {
                    lavaDotTimer = 0f;
                    foreach (var p in activePlayers)
                    {
                        if (!p.IsDead && !p.CheckInvulnerable() 
                            && Vector2.Distance(new Vector2(p.transform.position.x, p.transform.position.y), new Vector2(_arenaCenter.x, _arenaCenter.y)) < lavaRange)
                        {
                            p.TakeDamage(1000f);
                            Debug.Log($"<color=red>[용암 장판] {p.characterName} 이(가) 용암에 닿아 1000 피해!</color>");
                        }
                    }
                }

                // ── 2초: 화염 구체 스폰 ──
                if (!orbsSpawned && elapsed >= orbSpawnDelay)
                {
                    orbsSpawned = true;
                    Debug.Log($"<color=yellow>[{bossName}] 화염 구체(Fire Orbs) 아레나 주변에 소환!</color>");
                }

                // ── 5초: 호 강타 (Arc Smash) - 1500 피해 + 화상 ──
                if (!arcSmashed && elapsed >= arcSmashDelay)
                {
                    arcSmashed = true;
                    Debug.Log($"<color=red>[{bossName}] ◆ 호 강타(Arc Smash)! 전방 부채꼴 1500 피해 + 화상!</color>");
                    
                    foreach (var p in activePlayers)
                    {
                        if (!p.IsDead && !p.CheckInvulnerable())
                        {
                            // 2D 방향 판정: XY 평면에서 보스→플레이어 방향 벡터 사용
                    Vector2 dirToPlayer = new Vector2(
                        p.transform.position.x - transform.position.x,
                        p.transform.position.y - transform.position.y).normalized;
                    // 보스의 "앞" 방향: +X축(오른쪽)을 기준으로 90도 부채꼴
                    Vector2 bossForward2D = new Vector2(1f, 0f);
                    float angle = Vector2.Angle(bossForward2D, dirToPlayer);
                    
                    if (angle < 90f) // 전방 180도 부채꼴
                            {
                                p.TakeDamage(1500f);
                                Debug.Log($"<color=red>[호 강타] {p.characterName} 적중! (-1500)</color>");
                                
                                // 화상 DoT: 750 피해 / 5초 (150/초 × 5틱)
                                StartCoroutine(ApplyBurnDot(p, 150f, 5));
                            }
                        }
                    }
                }

                // ── 8초: 연쇄 폭발 (Chain Detonation) - 구체 폭발 800 피해 ──
                if (!chainDetonated && elapsed >= chainDetonateDelay)
                {
                    chainDetonated = true;
                    Debug.Log($"<color=red>[{bossName}] ◆ 화염 구체 연쇄 폭발! 전장 전체 800 피해!</color>");
                    
                    foreach (var p in activePlayers)
                    {
                        if (!p.IsDead && !p.CheckInvulnerable())
                        {
                            p.TakeDamage(800f);
                        }
                    }
                }

                yield return null;
            }

            // ── 종료 ──
            if (_animator != null) _animator.SetBool("Casting", false);

            Debug.Log($"<color=cyan>[{bossName}] ═══ Devouring Flames 종료. 에너지 리셋! ═══</color>");
            currentEnergy = 0f;
            isUltimateActive = false;
            OnEnergyChanged?.Invoke(currentEnergy, maxEnergy);

            _patternCoroutine = StartCoroutine(BossPatternLoop());
        }

        /// <summary>화상 DoT 적용 (원작: 750 dmg / 5sec = 150/sec × 5 ticks)</summary>
        private IEnumerator ApplyBurnDot(CharacterBase target, float dmgPerTick, int ticks)
        {
            for (int i = 0; i < ticks; i++)
            {
                yield return new WaitForSeconds(1f);
                if (target != null && !target.IsDead && !target.CheckInvulnerable())
                {
                    target.TakeDamage(dmgPerTick);
                    Debug.Log($"<color=orange>[화상] {target.characterName} -${dmgPerTick} (잔여 {ticks - i - 1}틱)</color>");
                }
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  패턴 루프 (원작 5가지 기술 무작위 + 연속 방지)
        //
        //  원작 벨타자르 일반 기술 목록:
        //  0) Magmaburst    — 시전 1.5초(차단 가능), 보스 200범위 밖 대상, 용암 3발 낙하 750×3
        //  1) Hellfire       — 시전 4.5초(차단 가능), 화염 빔 추적, DPS 800/초 × 10초 + 용암 잔류
        //  2) Moltenfuse     — 대상 마킹 3초 후 폭발 1000 + 넉업 + 주변 미사일 600
        //  3) Flameburst     — 보스 중심 사방 화염 미사일 방사 600×다수
        //  4) Flameturbine   — 내/외 원형 화염구 5초 회전 750×접촉
        // ═══════════════════════════════════════════════════════════

        private IEnumerator BossPatternLoop()
        {
            Debug.Log($"<color=green>[{bossName}] 패턴 루프 시작! (5가지 기술 무작위)</color>");
            
            while (currentHealth > 0)
            {
                if ((CombatManager.Instance != null && !CombatManager.Instance.isGameActive) 
                    || isStaggered || isUltimateActive) 
                { 
                    yield return null; 
                    continue; 
                }

                // 같은 패턴 연속 방지
                int patternIndex;
                do { patternIndex = Random.Range(0, 5); } 
                while (patternIndex == _lastPatternIndex);
                _lastPatternIndex = patternIndex;

                yield return StartCoroutine(ExecutePattern(patternIndex));
                
                // 페이즈 체크
                float healthPct = currentHealth / maxHealth;
                if (!isPhaseTwo && healthPct <= 0.5f)   EnterPhaseTwo();
                if (!isPhaseThree && healthPct <= 0.2f) EnterPhaseThree();

                // 패턴 간 대기 (원작 기준 약 3~5초 간격)
                float cooldown = isPhaseTwo ? 3f : 5f;  // 격노 시 패턴 속도 증가
                yield return new WaitForSeconds(cooldown);
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

        // ─────────────────────────────────────────
        //  [0] Magmaburst — 용암 탄환 3연발
        //  원작: 시전 1.5초(차단 가능), 보스 200범위 밖 대상
        //        용암 3발 낙하, 각 750 피해, 착탄 후 바닥 용암 3초 잔류
        // ─────────────────────────────────────────
        private IEnumerator PatternMagmaburst()
        {
            Debug.Log($"<color=yellow>[{bossName}] ▶ Magmaburst 시전 시작! (1.5초 — 차단 가능)</color>");
            yield return StartCoroutine(CastPattern("Magmaburst", 1.5f, true));
            
            // 차단 성공 시 CastPattern 내부에서 isCasting = false 처리됨
            // 차단 실패(시전 완료) 시에만 아래 로직 실행
            if (!isCasting) // 차단되지 않고 시전 완료됨 (CastPattern에서 발동 후 false)
            {
                Debug.Log($"<color=red>[{bossName}] Magmaburst 발동! 용암 3발 낙하!</color>");
                for (int i = 0; i < 3; i++)
                {
                    CharacterBase target = GetTargetOutsideRange(3f);
                    if (target != null && !target.CheckInvulnerable() && !target.IsDead)
                    {
                        yield return new WaitForSeconds(1f); // 착탄까지 1초
                        target.TakeDamage(750f);
                        Debug.Log($"<color=red>[Magmaburst #{i+1}] {target.characterName} 용암 직격! (-750)</color>");
                    }
                    else yield return new WaitForSeconds(1f);
                }
            }
        }

        // ─────────────────────────────────────────
        //  [1] Hellfire — 추적 화염 빔 
        //  원작: 시전 4.5초(차단 가능), 화염 빔이 대상을 추적
        //        초당 800 피해, 총 10초간 지속, 바닥에 용암 잔류
        // ─────────────────────────────────────────
        private IEnumerator PatternHellfire()
        {
            Debug.Log($"<color=yellow>[{bossName}] ▶ Hellfire 시전 시작! (4.5초 — 차단 가능)</color>");
            yield return StartCoroutine(CastPattern("Hellfire", 4.5f, true));
            
            if (!isCasting) // 시전 완료
            {
                CharacterBase target = GetHighestAggroTarget();
                if (target == null) target = GetRandomAliveTarget();
                
                if (target != null)
                {
                    Debug.Log($"<color=red>[{bossName}] Hellfire 발동! {target.characterName}을(를) 화염 빔이 추적!</color>");
                    
                    // 10초간 초당 800 피해 추적 빔
                    float hellfireDuration = 10f;
                    float hellfireElapsed = 0f;
                    float tickTimer = 0f;
                    
                    while (hellfireElapsed < hellfireDuration && currentHealth > 0 && !isStaggered)
                    {
                        hellfireElapsed += Time.deltaTime;
                        tickTimer += Time.deltaTime;
                        
                        if (tickTimer >= 1f)
                        {
                            tickTimer = 0f;
                            if (target != null && !target.IsDead && !target.CheckInvulnerable())
                            {
                                target.TakeDamage(800f);
                                Debug.Log($"<color=red>[Hellfire 빔] {target.characterName} -800 ({(int)hellfireElapsed}/{(int)hellfireDuration}초)</color>");
                            }
                            // 빔 대상이 죽으면 다른 대상으로 전환
                            if (target == null || target.IsDead)
                            {
                                target = GetHighestAggroTarget();
                                if (target != null) 
                                    Debug.Log($"<color=orange>[Hellfire] 대상 전환 → {target.characterName}</color>");
                            }
                        }
                        yield return null;
                    }
                    Debug.Log($"<color=cyan>[{bossName}] Hellfire 종료. 바닥에 용암 잔류!</color>");
                }
            }
        }

        // ─────────────────────────────────────────
        //  [2] Moltenfuse — 용융 폭발 (마킹 → 폭발 → 파편)
        //  원작: 대상에 후광 마킹, 3초 후 폭발 1000 피해 + 1초 넉업
        //        주변에 화염 미사일 발사 (각 600 피해)
        //        Taunt 무시, 최고 DPS/힐러 우선
        // ─────────────────────────────────────────
        private IEnumerator PatternMoltenfuse()
        {
            // 원작: Taunt 무시, 최고 딜러/힐러 우선 → GetHighestAggroTarget()으로 대체
            CharacterBase fuseTarget = GetHighestAggroTarget();
            if (fuseTarget == null) fuseTarget = GetRandomAliveTarget();
            if (fuseTarget == null) yield break;

            Debug.Log($"<color=yellow>[{bossName}] ▶ Moltenfuse! {fuseTarget.characterName}에 후광 마킹! (3초 후 폭발)</color>");
            
            // 3초 카운트다운
            yield return new WaitForSeconds(3f);
            
            if (fuseTarget != null && !fuseTarget.IsDead)
            {
                if (!fuseTarget.CheckInvulnerable())
                {
                    // 본체 폭발: 1000 피해
                    fuseTarget.TakeDamage(1000f);
                    Debug.Log($"<color=red>[Moltenfuse 폭발] {fuseTarget.characterName} -1000! (넉업 1초)</color>");
                }

                // 주변 파편 미사일: 반경 6m 이내 다른 영웅에게 각 600 피해
                foreach (var p in activePlayers)
                {
                    if (p != fuseTarget && !p.IsDead && !p.CheckInvulnerable()
                        && Vector2.Distance(new Vector2(fuseTarget.transform.position.x, fuseTarget.transform.position.y), new Vector2(p.transform.position.x, p.transform.position.y)) < 6f)
                    {
                        p.TakeDamage(600f);
                        Debug.Log($"<color=orange>[Moltenfuse 파편] {p.characterName} -600 (산개 실패!)</color>");
                    }
                }
            }
        }

        // ─────────────────────────────────────────
        //  [3] Flameburst — 화염 미사일 방사
        //  원작: 보스 중심으로 사방에 화염 미사일 방사
        //        각 미사일 600 피해, 근접 범위에서 특히 위험
        // ─────────────────────────────────────────
        private IEnumerator PatternFlameburst()
        {
            Debug.Log($"<color=red>[{bossName}] ▶ Flameburst! 사방으로 화염 미사일 방사!</color>");
            
            if (_animator != null) _animator.SetTrigger("AttackTrigger");
            yield return new WaitForSeconds(0.3f); // 짧은 시전 모션
            
            // 원작: 근접 범위일수록 위험, 다수의 미사일
            foreach (var p in activePlayers)
            {
                if (!p.IsDead && !p.CheckInvulnerable())
                {
                    float dist = Vector2.Distance(new Vector2(transform.position.x, transform.position.y), new Vector2(p.transform.position.x, p.transform.position.y));
                    
                    if (dist < 4f)
                    {
                        // 근접: 미사일 2발 적중 확률
                        p.TakeDamage(600f);
                        if (Random.value < 0.5f) p.TakeDamage(600f); // 50% 확률로 추가 적중
                        Debug.Log($"<color=red>[Flameburst 근접] {p.characterName} 다중 피탄! (근접 위험!)</color>");
                    }
                    else if (dist < 10f)
                    {
                        // 중거리: 1발 적중
                        if (Random.value < 0.6f)
                        {
                            p.TakeDamage(600f);
                            Debug.Log($"<color=orange>[Flameburst] {p.characterName} -600</color>");
                        }
                    }
                    // 원거리: 회피 가능 (무피해)
                }
            }
        }

        // ─────────────────────────────────────────
        //  [4] Flameturbine — 회전 화염구 (내/외 원형)
        //  원작: 내원(Inner) + 외원(Outer) 각 3개 화염구
        //        5초간 회전하며 관통 피해, 각 750 피해
        //        Hard+ 난이도: 기울어진 축으로 회전
        // ─────────────────────────────────────────
        private IEnumerator PatternFlameturbine()
        {
            Debug.Log($"<color=red>[{bossName}] ▶ Flameturbine! 내/외 원형 화염구 소환! (5초 회전)</color>");
            
            if (_animator != null) _animator.SetBool("Casting", true);
            
            float turbineDuration = 5f;
            float turbineElapsed = 0f;
            float tickTimer = 0f;
            float tickRate = 0.5f; // 0.5초마다 판정
            
            while (turbineElapsed < turbineDuration && currentHealth > 0 && !isStaggered)
            {
                turbineElapsed += Time.deltaTime;
                tickTimer += Time.deltaTime;
                
                if (tickTimer >= tickRate)
                {
                    tickTimer = 0f;
                    
                    foreach (var p in activePlayers)
                    {
                        if (!p.IsDead && !p.CheckInvulnerable())
                        {
                            float dist = Vector2.Distance(new Vector2(transform.position.x, transform.position.y), new Vector2(p.transform.position.x, p.transform.position.y));
                            
                            // 내원(2~4m) 또는 외원(6~8m) 범위에 있으면 피격
                            bool inInnerRing = (dist >= 2f && dist <= 4f);
                            bool inOuterRing = (dist >= 6f && dist <= 8f);
                            
                            if (inInnerRing || inOuterRing)
                            {
                                // 회전 판정: 시간에 따라 화염구 위치 변화 (120도 간격 3개)
                                float angle = Mathf.Repeat(turbineElapsed * 120f, 360f); // 초당 120도 회전
                            // 2D 각도: Atan2(y, x) — XY 평면 기준
                                float playerAngle = Mathf.Atan2(
                                    p.transform.position.y - transform.position.y,
                                    p.transform.position.x - transform.position.x) * Mathf.Rad2Deg;
                                
                                // 3개 화염구(0°, 120°, 240°) 중 하나와 근접하면 피격
                                for (int orb = 0; orb < 3; orb++)
                                {
                                    float orbAngle = Mathf.Repeat(angle + orb * 120f, 360f);
                                    float diff = Mathf.Abs(Mathf.DeltaAngle(playerAngle, orbAngle));
                                    if (diff < 30f) // ±30도 판정 범위
                                    {
                                        p.TakeDamage(750f);
                                        string ring = inInnerRing ? "내원" : "외원";
                                        Debug.Log($"<color=red>[Flameturbine {ring}] {p.characterName} -750!</color>");
                                        break; // 같은 틱에 중복 피격 방지
                                    }
                                }
                            }
                        }
                    }
                }
                yield return null;
            }
            
            if (_animator != null) _animator.SetBool("Casting", false);
            Debug.Log($"<color=cyan>[{bossName}] Flameturbine 종료.</color>");
        }

        // ═══════════════════════════════════════════════════════════
        //  캐스팅 시스템 (시전 바 + 차단 판정)
        // ═══════════════════════════════════════════════════════════

        /// <summary>캐스팅 패턴 실행 — 차단 가능한 시전 기술</summary>
        private IEnumerator CastPattern(string patternName, float duration, bool interruptable)
        {
            isCasting = true;
            currentCastName = patternName;
            currentCastDuration = duration;
            canBeInterrupted = interruptable;
            currentCastProgress = 0f;

            if (_animator != null) _animator.SetBool("Casting", true);
            if (_visualPart != null) StartCoroutine(FloatVisual(duration));

            if (CombatManager.Instance != null)
                CombatManager.Instance.NotifyBossCasting(patternName, duration);

            Debug.Log($"<color=yellow>[{bossName}] 시전 중: {patternName} ({duration}초) — {(interruptable ? "차단 가능!" : "차단 불가!")}</color>");

            float elapsed = 0f;
            while (elapsed < duration && isCasting)
            {
                elapsed += Time.deltaTime;
                currentCastProgress = elapsed / duration;
                yield return null;
            }
            currentCastProgress = 0f;

            // 시전 완료(차단 안 됨) — 각 패턴의 호출 측에서 후속 처리
            // isCasting은 false로 전환하여 "시전 완료" 상태 표시
            isCasting = false;
            if (_animator != null) _animator.SetBool("Casting", false);
        }

        /// <summary>시전 중 하늘로 띄우는 시각 연출</summary>
        private IEnumerator FloatVisual(float duration)
        {
            float elapsed = 0f;
            float jumpHeight = 3.5f;
            float jumpTime = 0.5f;
            
            while (elapsed < jumpTime && isCasting)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / jumpTime;
                float h = Mathf.Sin(t * Mathf.PI * 0.5f) * jumpHeight;
                _visualPart.localPosition = _startLocalPos + Vector3.up * h;
                yield return null;
            }

            while (elapsed < duration - 0.4f && isCasting)
            {
                elapsed += Time.deltaTime;
                float bob = Mathf.Sin(elapsed * 4f) * 0.2f;
                _visualPart.localPosition = _startLocalPos + Vector3.up * (jumpHeight + bob);
                yield return null;
            }

            float landTime = 0.3f;
            float landElapsed = 0f;
            Vector3 midPos = _visualPart.localPosition;
            while (landElapsed < landTime)
            {
                landElapsed += Time.deltaTime;
                float t = landElapsed / landTime;
                _visualPart.localPosition = Vector3.Lerp(midPos, _startLocalPos, t * t);
                yield return null;
            }
            
            _visualPart.localPosition = _startLocalPos;
            Debug.Log("<color=red>[Combat] 💥 벨타자르 강습 착지!</color>");
        }

        // ═══════════════════════════════════════════════════════════
        //  차단 (Interrupt / Counter)
        // ═══════════════════════════════════════════════════════════

        /// <summary>캐스팅 차단 (외부 호출 — 플레이어 F키 Counter)</summary>
        public void Interrupt()
        {
            if (!isCasting || !canBeInterrupted) return;
            
            isCasting = false;
            
            Debug.Log($"<color=cyan>[{bossName}] ★ 차단 성공! ★ 시간 보너스 +10초!</color>");
            if (CombatManager.Instance != null)
            {
                CombatManager.Instance.NotifyInterrupt();
                CombatManager.Instance.AddTimeBonus(10f);
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  페이즈 전환
        // ═══════════════════════════════════════════════════════════

        private void EnterPhaseTwo()
        {
            isPhaseTwo = true;
            Debug.Log($"<color=red>[{bossName}] ═══ HP 50% 돌파! 격노! (에너지 재생 2배, 패턴 간격 단축) ═══</color>");
            
            currentEnergyRegenRate = baseEnergyRegenRate * 2f;

            if (CombatManager.Instance != null)
                CombatManager.Instance.AdvancePhase();

            // 2페이즈 지속 화염 DoT 시작
            StartCoroutine(PhaseAuraDot(15f, false));
        }

        private void EnterPhaseThree()
        {
            isPhaseThree = true;
            Debug.Log($"<color=red>[{bossName}] ═══ HP 20% 돌파! 3페이즈! (DoT 강화 + DPS 체크) ═══</color>");
            Debug.Log($"<color=magenta>[{bossName}] 거대한 보호막 생성! 15초 내에 파괴하세요!</color>");
            
            if (CombatManager.Instance != null)
                CombatManager.Instance.AdvancePhase();

            StartCoroutine(PhaseAuraDot(30f, true)); // 3페이즈: 초당 30 도트
            StartCoroutine(DpsCheckRoutine());
        }

        /// <summary>페이즈 지속 화염 오라 DoT</summary>
        private IEnumerator PhaseAuraDot(float dps, bool isPhaseThreeDot)
        {
            while (currentHealth > 0)
            {
                // 3페이즈 DoT는 isPhaseThree가 true일 때만
                // 2페이즈 DoT는 isPhaseThree가 false인 동안만
                if (!isPhaseThreeDot && isPhaseThree) yield break;
                
                foreach (var p in activePlayers)
                {
                    if (!p.IsDead) p.TakeDamage(dps);
                }
                yield return new WaitForSeconds(1f);
            }
        }

        private IEnumerator DpsCheckRoutine()
        {
            currentShield = maxHealth * 0.05f; 
            hasDpsCheckShield = true;
            float timer = 15f;

            while (timer > 0 && hasDpsCheckShield && currentHealth > 0)
            {
                timer -= Time.deltaTime;
                yield return null;
            }

            if (hasDpsCheckShield && currentHealth > 0)
            {
                Debug.Log($"<color=red>[{bossName}] DPS 체크 실패! 전멸기 발동!</color>");
                foreach (var p in activePlayers) if (!p.IsDead) p.TakeDamage(999999f);
                hasDpsCheckShield = false;
            }
            else if (!hasDpsCheckShield && currentHealth > 0 && timer > 0)
            {
                Debug.Log($"<color=cyan>[{bossName}] 보호막 파괴! DPS 체크 성공!</color>");
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  스태거 시스템
        // ═══════════════════════════════════════════════════════════

        public void AddStagger(float amount)
        {
            currentStagger += amount;
            
            if (UI.InGameHUDController.Instance != null && UI.InGameHUDController.Instance.bossFrame != null)
                UI.InGameHUDController.Instance.bossFrame.AddStagger(amount);

            if (currentStagger >= maxStagger)
            {
                StartCoroutine(StaggerCoroutine());
            }
        }

        private IEnumerator StaggerCoroutine()
        {
            isStaggered = true;
            currentStagger = 0f;
            Debug.Log($"<color=blue>[{bossName}] 경직! {staggerDuration}초간 행동 불능!</color>");
            
            yield return new WaitForSeconds(staggerDuration);
            
            isStaggered = false;
            Debug.Log($"[{bossName}] 경직 해제.");
        }


        // ═══════════════════════════════════════════════════════════
        //  피해 처리
        // ═══════════════════════════════════════════════════════════

        public void TakeDamage(float amount)
        {
            if (currentHealth <= 0) return;

            // 궁극기 중 무적 (원작 기준)
            if (isUltimateActive) return;

            // DPS 체크 쉴드 우선 타격
            if (hasDpsCheckShield)
            {
                currentShield -= amount;
                if (currentShield <= 0) hasDpsCheckShield = false;
                return;
            }

            currentHealth -= amount;
            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            if (currentHealth <= 0)
            {
                currentHealth = 0;
                if (_animator != null) _animator.SetBool("IsDead", true);

                Debug.Log($"<color=green>[{bossName}] has been defeated! 🎆</color>");

                if (CombatManager.Instance != null)
                    CombatManager.Instance.EndBattle(true);
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  광폭화 (Enrage) — 타임 오버 시 CombatManager에서 호출
        // ═══════════════════════════════════════════════════════════

        public void TriggerEnrage()
        {
            if (isEnraged || currentHealth <= 0) return;
            isEnraged = true;
            
            Debug.Log($"<color=red>[{bossName}] 타임 오버! 광폭화! (공격력 500%, 공속 4배!)</color>");
            
            autoAttackDamage *= 5f;
            autoAttackInterval = 0.5f;
            currentEnergyRegenRate = baseEnergyRegenRate * 4f; // 에너지 재생도 대폭 강화

            StartCoroutine(HardEnrageRoutine());
        }

        private IEnumerator HardEnrageRoutine()
        {
            Debug.Log($"<color=yellow>경고: 30초 뒤 확정 전멸기 발동!</color>");
            yield return new WaitForSeconds(30f);

            if (currentHealth > 0)
            {
                Debug.Log($"<color=red>[{bossName}] 하드 인레이지! 피할 수 없는 종말!</color>");
                foreach (var p in activePlayers) if (!p.IsDead) p.TakeDamage(999999f);
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  에디터 테스트 유틸
        // ═══════════════════════════════════════════════════════════

        [ContextMenu("Test Take Damage")]
        public void TestDamage() => TakeDamage(1000f);

        [ContextMenu("Force Phase 2")]
        public void TestPhase2() => EnterPhaseTwo();

        [ContextMenu("Force Phase 3")]
        public void TestPhase3() => EnterPhaseThree();

        [ContextMenu("Test Interrupt")]
        public void TestInterrupt() => Interrupt();

        [ContextMenu("Force Ultimate")]
        public void TestUltimate() => TriggerUltimate();
    }
}
