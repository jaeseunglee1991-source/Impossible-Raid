using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using BossRaid.Combat.Boss;
using BossRaid.UI;
using BossRaid.Managers;

namespace BossRaid.Combat
{
    // ─────────────────────────────────────────
    //  직업 역할 열거형 (BossAI 어그로 타겟팅 연동)
    // ─────────────────────────────────────────
    public enum CharacterRole { Tank, MeleeDPS, RangedDPS, Healer }

    /// <summary>
    /// ──────────────────────────────────────────────────────────────────
    /// CharacterBase — 모든 직업 클래스(Warrior, Mage 등)의 베이스 클래스
    ///
    /// ■ 체력 / 사망 / 부활 / 무적
    /// ■ 어그로(위협치) / 쉴드 / 피해 경감
    /// ■ 스킬 슬롯 시스템 (3슬롯 + 궁극기)
    ///   - allSkills     : 직업 클래스 Awake()에서 등록하는 전체 스킬 목록
    ///   - equippedSlots : 각 슬롯에 장착된 allSkills 인덱스 (SaveManager 저장)
    ///   - TryUseSkill() : 슬롯 인덱스로 쿨타임 체크 후 UseSkill() 호출
    ///   - UseSkill()    : 직업 클래스에서 switch(idx)로 실제 로직 구현
    /// ■ 방치형 자동전투 AI 연동
    /// ■ 전투 통계 (ResultManager 연동)
    /// ──────────────────────────────────────────────────────────────────
    /// </summary>
    public class CharacterBase : MonoBehaviour
    {
        // ═══════════════════════════════════════════════════════════
        //  기본 정보 / 직업 속성
        // ═══════════════════════════════════════════════════════════

        [Header("캐릭터 정보")]
        public string        characterName = "플레이어";
        public CharacterRole role          = CharacterRole.MeleeDPS;

        // ─────────────────────────────────────────
        //  전투 스탯 (직업 클래스 Awake에서 초기값 설정)
        // ─────────────────────────────────────────

        [Header("전투 스탯")]
        public float autoAttackDamage        = 50f;
        public float attackSpeed             = 1.0f;   // 공격 간격(초). 낮을수록 빠름
        public float attackRange             = 3f;
        public float movementSpeed           = 5f;
        public float attackPowerMultiplier   = 1.0f;   // 스킬 계수 배율
        public float damageReductionMultiplier = 1.0f; // 피해 감소 배율 (0.7 = 30% 경감)
        public float shieldAmount            = 0f;     // 현재 흡수 가능한 쉴드량

        // ═══════════════════════════════════════════════════════════
        //  체력 (HP)
        // ═══════════════════════════════════════════════════════════

        [Header("체력")]
        public float maxHealth = 1000f;

        [Tooltip("현재 체력 — 인스펙터에서 실시간 확인 가능")]
        [SerializeField] private float _currentHealth;

        /// <summary>현재 체력. 변경 시 OnHealthChanged 이벤트와 HUD를 자동 갱신합니다.</summary>
        public float currentHealth
        {
            get => _currentHealth;
            set
            {
                _currentHealth = Mathf.Clamp(value, 0f, maxHealth);
                OnHealthChanged?.Invoke(_currentHealth, maxHealth);
                UpdateHPBar();
            }
        }

        /// <summary>체력이 변경될 때 발행됩니다. (currentHP, maxHP)</summary>
        public event System.Action<float, float> OnHealthChanged;

        /// <summary>사망 상태. true이면 피해 / 스킬 / 이동 모두 차단됩니다.</summary>
        public bool IsDead { get; private set; } = false;

        // ═══════════════════════════════════════════════════════════
        //  무적 (Invulnerable)
        // ═══════════════════════════════════════════════════════════

        [Header("무적")]
        [SerializeField] private bool _isInvulnerable = false;
        private Coroutine _invulnerableCoroutine;

        /// <summary>무적 여부. BossAI 패턴 판정에서 호출됩니다.</summary>
        public bool CheckInvulnerable() => _isInvulnerable;

        // ═══════════════════════════════════════════════════════════
        //  어그로 (Threat / Aggro)
        // ═══════════════════════════════════════════════════════════

        [Header("어그로")]
        [Tooltip("보스 타겟 선정에 사용하는 위협치. BossAI.GetHighestAggroTarget() 연동")]
        public float currentThreat = 0f;

        [Tooltip("피해 1당 생성되는 어그로 배율 (탱커는 1.5~2.0 권장)")]
        public float threatMultiplier = 1.0f;

        [Tooltip("어그로를 유지한 누적 시간(초). ResultManager 통계용")]
        public float aggroDuration = 0f;

        private bool _isCurrentTarget = false;

        // ═══════════════════════════════════════════════════════════
        //  전투 통계 (ResultManager 연동)
        // ═══════════════════════════════════════════════════════════

        [Header("전투 통계 (읽기 전용)")]
        public float totalDamageDealt  = 0f;
        public float totalHealingDone  = 0f;
        public float totalDamageTaken  = 0f;

        // ═══════════════════════════════════════════════════════════
        //  스킬 슬롯 시스템
        // ═══════════════════════════════════════════════════════════

        // ─────────────────────────────────────────
        //  스킬 목록 (직업 클래스 Awake에서 채움)
        // ─────────────────────────────────────────

        /// <summary>
        /// 이 캐릭터가 쓸 수 있는 전체 스킬 목록.
        /// 직업 클래스 Awake() 에서 RegisterSkills()로 채웁니다.
        /// </summary>
        public List<SkillDefinition> allSkills { get; private set; } = new List<SkillDefinition>();

        /// <summary>궁극기 정의 (별도 슬롯)</summary>
        public SkillDefinition ultimateSkill { get; private set; }

        // ─────────────────────────────────────────
        //  장착 슬롯 (SaveManager 저장 대상)
        // ─────────────────────────────────────────

        public const int SKILL_SLOT_COUNT = 3;

        /// <summary>
        /// 슬롯[0..2] 에 장착된 allSkills 인덱스.
        /// -1 이면 빈 슬롯.
        /// </summary>
        public int[] equippedSlots { get; private set; } = { 0, 1, 2 };

        // ─────────────────────────────────────────
        //  쿨타임 타이머 (런타임 전용 — 저장 안 함)
        // ─────────────────────────────────────────

        private float[] _slotCooldownTimers  = new float[SKILL_SLOT_COUNT];
        private float   _ultimateCooldownTimer = 0f;

        // ─────────────────────────────────────────
        //  HUD 연동용 프로퍼티 (InGameHUDController가 읽음)
        // ─────────────────────────────────────────

        /// <summary>현재 장착된 슬롯의 스킬 이름 배열 (HUD 버튼 라벨)</summary>
        public string[] skillNames
        {
            get
            {
                var names = new string[SKILL_SLOT_COUNT];
                for (int i = 0; i < SKILL_SLOT_COUNT; i++)
                    names[i] = GetEquippedSkill(i)?.skillName ?? "—";
                return names;
            }
        }

        /// <summary>현재 장착된 슬롯의 쿨타임 배열 (HUD 버튼 초기화)</summary>
        public float[] skillCooldowns
        {
            get
            {
                var cds = new float[SKILL_SLOT_COUNT];
                for (int i = 0; i < SKILL_SLOT_COUNT; i++)
                    cds[i] = GetEquippedSkill(i)?.cooldown ?? 0f;
                return cds;
            }
        }

        public string ultimateName     => ultimateSkill?.skillName   ?? "궁극기";
        public float  ultimateCooldown => ultimateSkill?.cooldown     ?? 60f;

        // ─────────────────────────────────────────
        //  현재 전투 타겟 (직업 클래스 UseSkill에서 사용)
        // ─────────────────────────────────────────

        /// <summary>자동전투 AI 또는 TagCharacterController가 설정합니다.</summary>
        public Boss.IBossPatternHandler targetBoss { get; set; }

        // ─────────────────────────────────────────
        //  스킬 등록 API (직업 클래스 Awake에서 호출)
        // ─────────────────────────────────────────

        /// <summary>
        /// 직업 클래스 Awake()에서 이 메서드로 스킬을 등록합니다.
        ///
        /// 사용 예시 (FireMage.Awake):
        ///   RegisterSkills(
        ///       new SkillDefinition("화염 작렬", 12f, 0, interrupt: true),
        ///       new SkillDefinition("화염구",     4f, 1),
        ///       new SkillDefinition("불기둥",    10f, 2)
        ///   );
        ///   RegisterUltimate(new SkillDefinition("발화", 60f, 0, ultimate: true));
        /// </summary>
        protected void RegisterSkills(params SkillDefinition[] skills)
        {
            allSkills.Clear();
            allSkills.AddRange(skills);

            // 기본 장착: 앞 3개를 순서대로 슬롯에 배치
            for (int i = 0; i < SKILL_SLOT_COUNT; i++)
                equippedSlots[i] = (i < allSkills.Count) ? i : -1;
        }

        protected void RegisterUltimate(SkillDefinition ultimate)
        {
            ultimateSkill = ultimate;
        }

        // ─────────────────────────────────────────
        //  장착 / 해제 API (SkillEquipManager에서 호출)
        // ─────────────────────────────────────────

        /// <summary>
        /// slotIndex(0~2) 슬롯에 allSkills[skillIndex]를 장착합니다.
        /// 이미 다른 슬롯에 같은 스킬이 있으면 자동으로 스왑합니다.
        /// </summary>
        public bool EquipSkill(int slotIndex, int skillIndex)
        {
            if (slotIndex < 0 || slotIndex >= SKILL_SLOT_COUNT) return false;
            if (skillIndex < 0 || skillIndex >= allSkills.Count) return false;

            // 동일 스킬이 다른 슬롯에 있으면 스왑
            for (int i = 0; i < SKILL_SLOT_COUNT; i++)
            {
                if (i != slotIndex && equippedSlots[i] == skillIndex)
                {
                    equippedSlots[i] = equippedSlots[slotIndex]; // 기존 슬롯에 이전 스킬
                    break;
                }
            }

            equippedSlots[slotIndex] = skillIndex;
            SaveManager.Instance?.MarkDirty();

            Debug.Log($"[{characterName}] 슬롯 {slotIndex} ← {allSkills[skillIndex].skillName} 장착");
            return true;
        }

        /// <summary>slotIndex 슬롯을 비웁니다.</summary>
        public void UnequipSkill(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= SKILL_SLOT_COUNT) return;
            equippedSlots[slotIndex] = -1;
            SaveManager.Instance?.MarkDirty();
        }

        /// <summary>slotIndex에 장착된 SkillDefinition을 반환합니다. 빈 슬롯이면 null.</summary>
        public SkillDefinition GetEquippedSkill(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= SKILL_SLOT_COUNT) return null;
            int skillIdx = equippedSlots[slotIndex];
            if (skillIdx < 0 || skillIdx >= allSkills.Count) return null;
            return allSkills[skillIdx];
        }

        // ─────────────────────────────────────────
        //  스킬 실행 (쿨타임 체크 포함)
        // ─────────────────────────────────────────

        /// <summary>
        /// HUD 버튼 클릭 / 키 입력 / 자동전투 AI에서 호출합니다.
        ///
        ///   slotIndex 0~2 : 일반 스킬 슬롯
        ///   slotIndex 3   : 궁극기
        ///
        /// 쿨타임 중이면 false를 반환하고 아무것도 하지 않습니다.
        /// </summary>
        public bool TryUseSkill(int slotIndex)
        {
            if (IsDead) return false;

            // ── 궁극기 ──
            if (slotIndex == 3)
            {
                if (_ultimateCooldownTimer > 0f) return false;
                if (ultimateSkill == null) return false;

                UseUltimate();
                _ultimateCooldownTimer = ultimateSkill.cooldown;
                PlayAnimation("Attack");
                return true;
            }

            // ── 일반 슬롯 ──
            if (slotIndex < 0 || slotIndex >= SKILL_SLOT_COUNT) return false;
            if (_slotCooldownTimers[slotIndex] > 0f) return false;

            SkillDefinition skill = GetEquippedSkill(slotIndex);
            if (skill == null) return false;

            UseSkill(skill.skillIndex);
            _slotCooldownTimers[slotIndex] = skill.cooldown;
            PlayAnimation("Attack");
            return true;
        }

        /// <summary>
        /// 자동전투 AI가 쿨다운이 완료된 첫 번째 스킬을 자동으로 사용합니다.
        /// TagCharacterController.ExecuteIdleAutoBattle()에서 평타 전에 호출하세요.
        /// </summary>
        /// <returns>스킬을 사용했으면 true (평타 건너뜀)</returns>
        public bool TryAutoUseSkill()
        {
            for (int i = 0; i < SKILL_SLOT_COUNT; i++)
            {
                if (_slotCooldownTimers[i] <= 0f && equippedSlots[i] >= 0)
                    return TryUseSkill(i);
            }
            return false;
        }

        // ─────────────────────────────────────────
        //  스킬 쿨타임 틱 (Update에서 호출)
        // ─────────────────────────────────────────

        private void TickSkillCooldowns()
        {
            float dt = Time.deltaTime;
            for (int i = 0; i < SKILL_SLOT_COUNT; i++)
            {
                if (_slotCooldownTimers[i] > 0f)
                    _slotCooldownTimers[i] = Mathf.Max(0f, _slotCooldownTimers[i] - dt);
            }
            if (_ultimateCooldownTimer > 0f)
                _ultimateCooldownTimer = Mathf.Max(0f, _ultimateCooldownTimer - dt);
        }

        /// <summary>슬롯의 남은 쿨타임(초). SkillButtonUI 갱신용.</summary>
        public float GetSlotCooldownRemaining(int slotIndex)
        {
            if (slotIndex == 3) return _ultimateCooldownTimer;
            if (slotIndex < 0 || slotIndex >= SKILL_SLOT_COUNT) return 0f;
            return _slotCooldownTimers[slotIndex];
        }

        // ─────────────────────────────────────────
        //  직업 클래스 override 진입점
        // ─────────────────────────────────────────

        /// <summary>
        /// 실제 스킬 로직. 직업 클래스에서 switch(skillIndex)로 구현합니다.
        /// TryUseSkill()이 쿨타임 체크 후 이 메서드를 호출합니다.
        /// </summary>
        public virtual void UseSkill(int skillIndex)
        {
            Debug.Log($"[{characterName}] UseSkill({skillIndex}) — 직업 클래스에서 override 필요");
        }

        /// <summary>
        /// 궁극기 로직. 직업 클래스에서 override합니다.
        /// </summary>
        public virtual void UseUltimate()
        {
            Debug.Log($"[{characterName}] UseUltimate() — 직업 클래스에서 override 필요");
        }

        // ─────────────────────────────────────────
        //  파티 유틸 (힐러/서포터 직업 클래스용)
        // ─────────────────────────────────────────

        /// <summary>파티 중 HP 비율이 가장 낮은 생존 캐릭터를 반환합니다.</summary>
        public CharacterBase FindLowestHPAlly()
        {
            if (CombatManager.Instance == null) return null;

            CharacterBase lowest = null;
            float lowestRatio = float.MaxValue;

            foreach (var p in CombatManager.Instance.activePlayers)
            {
                if (p == null || p.IsDead) continue;
                float ratio = p.currentHealth / p.maxHealth;
                if (ratio < lowestRatio) { lowestRatio = ratio; lowest = p; }
            }
            return lowest;
        }

        /// <summary>파티 전체를 회복시킵니다.</summary>
        public void HealAllParty(float amount)
        {
            if (CombatManager.Instance == null) return;
            foreach (var p in CombatManager.Instance.activePlayers)
            {
                if (p != null && !p.IsDead) p.Heal(amount, this);
            }
        }

        /// <summary>파티 전체에 버프 액션을 적용합니다. (팔라딘 오라, 사제 궁극기 등)</summary>
        public void ApplyPartyBuff(Action<CharacterBase> buffAction)
        {
            if (CombatManager.Instance == null) return;
            foreach (var p in CombatManager.Instance.activePlayers)
            {
                if (p != null && !p.IsDead) buffAction?.Invoke(p);
            }
        }

        // ─────────────────────────────────────────
        //  SaveManager 연동 (장착 슬롯 복원)
        // ─────────────────────────────────────────

        /// <summary>SaveManager.ApplySaveData()에서 저장된 슬롯 배열을 복원합니다.</summary>
        public void RestoreEquippedSlots(int[] savedSlots)
        {
            if (savedSlots == null || savedSlots.Length != SKILL_SLOT_COUNT) return;

            for (int i = 0; i < SKILL_SLOT_COUNT; i++)
            {
                int idx = savedSlots[i];
                equippedSlots[i] = (idx >= 0 && idx < allSkills.Count) ? idx : -1;
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  성장 시스템
        // ═══════════════════════════════════════════════════════════

        [Header("성장 시스템 (인스펙터에서 밸런스 조절 가능)")]
        public StatUpgrade attackPowerUpgrade = new StatUpgrade()
        {
            statName             = "기본 공격력",
            baseCost             = 10,
            costMultiplier       = 1.15,
            baseStat             = 15,
            statIncreasePerLevel = 3
        };

        [Header("전투 속성")]
        public float baseAttackCooldown = 1.0f;

        /// <summary>현재 공격력 (StatUpgrade 성장치 반영)</summary>
        public float baseAttackPower => attackPowerUpgrade.CurrentStat;

        // ═══════════════════════════════════════════════════════════
        //  내부 레퍼런스
        // ═══════════════════════════════════════════════════════════

        private Animator     _animator;
        private SPUM_Prefabs _spumPrefab;
        private Rigidbody    _rb;

        // ═══════════════════════════════════════════════════════════
        //  초기화
        // ═══════════════════════════════════════════════════════════

        protected virtual void Awake()
        {
            _animator = GetComponentInChildren<Animator>();
            _spumPrefab = GetComponentInChildren<SPUM_Prefabs>();
            _rb = GetComponent<Rigidbody>();
        }

        protected virtual void Start()
        {
            InitializeHP();
        }

        protected virtual void Update()
        {
            if (_isCurrentTarget && !IsDead)
                aggroDuration += Time.deltaTime;

            TickSkillCooldowns();
        }

        /// <summary>전투 시작 또는 씬 로드 시 호출. HP를 최대치로 초기화합니다.</summary>
        public void InitializeHP()
        {
            IsDead        = false;
            currentHealth = maxHealth;
        }

        // ═══════════════════════════════════════════════════════════
        //  피해 처리
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// 캐릭터가 피해를 입을 때 호출됩니다.
        ///   1. 사망 / 무적이면 즉시 무시
        ///   2. 방어 경감 계산 (직업 클래스에서 override 가능)
        ///   3. HP 차감 → OnHealthChanged 이벤트 → HUD 갱신
        ///   4. 전투 통계 누적
        ///   5. HP 0 이하 → Die() 호출
        /// </summary>
        public virtual void TakeDamage(float damage)
        {
            if (IsDead || _isInvulnerable) return;
            if (damage <= 0f) return;

            float actual = CalculateDamageReduction(damage);

            currentHealth    -= actual;
            totalDamageTaken += actual;

            PlayAnimation("Hit");

            Debug.Log($"[{characterName}] 피해 -{actual:F0}  " +
                      $"HP: {currentHealth:F0}/{maxHealth:F0}");

            if (currentHealth <= 0f)
                Die();
        }

        /// <summary>
        /// 방어 경감 공식. 직업 클래스에서 override하여 탱커 경감 등을 구현하세요.
        /// 기본값: 경감 없이 원시값 그대로 반환
        /// </summary>
        protected virtual float CalculateDamageReduction(float incomingDamage)
        {
            return incomingDamage;
        }

        // ═══════════════════════════════════════════════════════════
        //  사망 처리
        // ═══════════════════════════════════════════════════════════

        private void Die()
        {
            if (IsDead) return;

            IsDead          = false; // 플래그 먼저 해제하여 PlayAnimation 통과
            IsDead          = true;  // 바로 사망 확정
            _isInvulnerable = false;

            PlayAnimation("Die");

            Debug.Log($"<color=red>[{characterName}] 사망!</color>");

            // BattleManager에 사망 통보 → 전멸 판정
            if (BattleManager.Instance != null)
                BattleManager.Instance.CheckGameOver();
        }

        // ═══════════════════════════════════════════════════════════
        //  부활
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// CombatManager.ReviveAllPlayers() 또는 ReviveService에서 호출합니다.
        /// </summary>
        /// <param name="hpRatio">최대 HP 대비 부활 체력 비율 (기본 50%)</param>
        public void Revive(float hpRatio = 0.5f)
        {
            if (!IsDead) return;

            IsDead        = false;
            currentHealth = maxHealth * Mathf.Clamp01(hpRatio);

            PlayAnimation("Idle");
            SetInvulnerable(0.8f); // 부활 직후 0.8초 무적 (연속 즉사 방지)

            Debug.Log($"<color=cyan>[{characterName}] 부활! HP: {currentHealth:F0}/{maxHealth:F0}</color>");
        }

        // ═══════════════════════════════════════════════════════════
        //  회복 (Heal)
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// 힐러 스킬 또는 자가회복에서 호출합니다.
        /// </summary>
        /// <param name="amount">회복량</param>
        /// <param name="healer">치유를 수행한 캐릭터 (통계 귀속용, null 허용)</param>
        public void Heal(float amount, CharacterBase healer = null)
        {
            if (IsDead || amount <= 0f) return;

            float before  = currentHealth;
            currentHealth += amount;
            float actual  = currentHealth - before; // 오버힐 제외

            if (healer != null)
                healer.totalHealingDone += actual;

            Debug.Log($"[{characterName}] 회복 +{actual:F0}  HP: {currentHealth:F0}/{maxHealth:F0}");
        }

        // ═══════════════════════════════════════════════════════════
        //  무적
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// 지정 시간 동안 무적을 부여합니다. 이미 무적 중이면 남은 시간을 교체합니다.
        /// </summary>
        public void SetInvulnerable(float duration)
        {
            if (_invulnerableCoroutine != null)
                StopCoroutine(_invulnerableCoroutine);

            _invulnerableCoroutine = StartCoroutine(InvulnerableRoutine(duration));
        }

        private IEnumerator InvulnerableRoutine(float duration)
        {
            _isInvulnerable = true;
            yield return new WaitForSeconds(duration);
            _isInvulnerable        = false;
            _invulnerableCoroutine = null;
        }

        // ═══════════════════════════════════════════════════════════
        //  어그로
        // ═══════════════════════════════════════════════════════════

        /// <summary>피해 / 힐 이후 호출하여 어그로를 누적합니다.</summary>
        public void AddThreat(float amount)
        {
            currentThreat += amount * threatMultiplier;
        }

        /// <summary>보스가 이 캐릭터를 타겟으로 삼을 때 호출합니다.</summary>
        public void SetAsTarget(bool isTarget)
        {
            _isCurrentTarget = isTarget;
        }

        // ═══════════════════════════════════════════════════════════
        //  공격 (어그로 + 통계 연동)
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// 보스에게 데미지를 주고 어그로와 딜 통계를 동시에 누적합니다.
        /// TagCharacterController.ExecuteIdleAutoBattle()에서 이 메서드를 사용하세요.
        ///
        /// 기존 코드:  currentBoss.TakeDamage(characterBase.baseAttackPower);
        /// 교체 후:    characterBase.DealDamageTo(currentBoss, characterBase.baseAttackPower);
        /// </summary>
        public void DealDamageTo(Boss.IBossPatternHandler boss, float damage)
        {
            if (IsDead || boss == null) return;

            boss.TakeDamage(damage);
            totalDamageDealt += damage;
            AddThreat(damage);
        }

        /// <summary>
        /// TagCharacterController (레이드 모드) 호환용.
        /// targetBoss를 설정하고 슬롯 0번 스킬을 실행합니다.
        /// </summary>
        public void UseSkill(int skillIndex, BossAI boss)
        {
            targetBoss = boss;
            TryUseSkill(skillIndex); // skillIndex를 슬롯 인덱스로 해석
        }

        // ═══════════════════════════════════════════════════════════
        //  애니메이션
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// SPUM 및 일반 Animator를 지원합니다.
        /// </summary>
        public void PlayAnimation(string triggerName, int index = 0)
        {
            if (_animator == null) return;
            if (IsDead && triggerName != "Die") return;

            // SPUM 컴포넌트가 있는 경우
            if (_spumPrefab != null)
            {
                PlayerState state = PlayerState.IDLE;
                switch (triggerName.ToUpper())
                {
                    case "IDLE":   state = PlayerState.IDLE; break;
                    case "MOVE":   state = PlayerState.MOVE; break;
                    case "ATTACK": state = PlayerState.ATTACK; break;
                    case "HIT":    state = PlayerState.DAMAGED; break;
                    case "DIE":    state = PlayerState.DEATH; break;
                    default:       state = PlayerState.OTHER; break;
                }
                _spumPrefab.PlayAnimation(state, index);
                return;
            }

            // 일반 애니메이터 방식 보강
            _animator.ResetTrigger("Idle");
            _animator.ResetTrigger("Move");
            _animator.ResetTrigger("Attack");
            _animator.ResetTrigger("Hit");
            _animator.ResetTrigger("Die");

            // SPUM 파라미터 직접 호환성 (Bool 필드)
            if (triggerName == "Move") _animator.SetBool("1_Move", true);
            else if (triggerName == "Idle") _animator.SetBool("1_Move", false);

            _animator.SetTrigger(triggerName);
        }

        // ═══════════════════════════════════════════════════════════
        //  HUD 연동 (HP 바)
        // ═══════════════════════════════════════════════════════════

        private void UpdateHPBar()
        {
            if (InGameHUDController.Instance == null) return;
            if (CombatManager.Instance != null && CombatManager.Instance.localPlayer == this)
                InGameHUDController.Instance.UpdateLocalPlayerHP(_currentHealth, maxHealth);
        }

        // ═══════════════════════════════════════════════════════════
        //  강화 (UI 버튼 연결용)
        // ═══════════════════════════════════════════════════════════

        public void OnClickUpgradeAttackPower()
        {
            attackPowerUpgrade.TryUpgrade();
        }

        // ═══════════════════════════════════════════════════════════
        //  에디터 테스트 유틸 (ContextMenu)
        // ═══════════════════════════════════════════════════════════

        [ContextMenu("Test Take Damage (200)")]
        private void TestTakeDamage() => TakeDamage(200f);

        [ContextMenu("Test Heal (300)")]
        private void TestHeal() => Heal(300f);

        [ContextMenu("Test Kill")]
        private void TestDie() => TakeDamage(maxHealth * 10f);

        [ContextMenu("Test Revive")]
        private void TestRevive() => Revive();

        [ContextMenu("Test Invulnerable (3s)")]
        private void TestInvul() => SetInvulnerable(3f);
    }
}
