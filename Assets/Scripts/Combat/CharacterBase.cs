using UnityEngine;
using System.Collections;
using BossRaid.Combat.Boss;
using BossRaid.Managers;
using BossRaid.UI;

namespace BossRaid.Combat
{
    public enum CharacterRole
    {
        Tank,
        Healer,
        MeleeDPS,
        RangedDPS
    }

    /// <summary>
    /// ──────────────────────────────────────────────────────────────────
    /// CharacterBase — 모든 직업 클래스(Warrior, Mage 등)의 베이스 클래스
    /// ──────────────────────────────────────────────────────────────────
    /// </summary>
    public class CharacterBase : MonoBehaviour
    {
        // ═══════════════════════════════════════════════════════════
        //  기본 정보
        // ═══════════════════════════════════════════════════════════

        [Header("캐릭터 정보")]
        public string characterName = "플레이어";
        public CharacterRole role;

        // ═══════════════════════════════════════════════════════════
        //  스킬 및 공격 (Subclasses 연동용)
        // ═══════════════════════════════════════════════════════════

        [Header("전투 속성")]
        public float autoAttackDamage = 10f;
        public float attackSpeed = 1.0f;
        public float attackRange = 2.0f;
        public float baseAttackCooldown = 1.0f;

        [Header("스킬")]
        public string[] skillNames = new string[3];
        public float[] skillCooldowns = new float[3];
        public string ultimateName;
        public float ultimateCooldown;

        public BossRaid.Combat.Boss.BossAI targetBoss;
        public float shieldAmount = 0f;
        public float damageReductionMultiplier = 1.0f;
        public float attackPowerMultiplier = 1.0f;
        public float movementSpeed = 5f;

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
            protected set
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
        public bool isInvulnerable
        {
            get => _isInvulnerable;
            set => _isInvulnerable = value;
        }

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

        /// <summary>현재 공격력 (StatUpgrade 성장치 반영)</summary>
        public float baseAttackPower => attackPowerUpgrade.CurrentStat;

        // ═══════════════════════════════════════════════════════════
        //  내부 레퍼런스
        // ═══════════════════════════════════════════════════════════

        private Animator _animator;

        // ═══════════════════════════════════════════════════════════
        //  초기화
        // ═══════════════════════════════════════════════════════════

        protected virtual void Awake()
        {
            _animator = GetComponentInChildren<Animator>();
        }

        protected virtual void Start()
        {
            InitializeHP();
        }

        protected virtual void Update()
        {
            if (_isCurrentTarget && !IsDead)
                aggroDuration += Time.deltaTime;
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
            return incomingDamage * damageReductionMultiplier;
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
        /// CombatManager.ReviveAllPlayers() 또는 Revive सर्विस에서 호출합니다.
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
        /// </summary>
        public void DealDamageTo(BossAI boss, float damage)
        {
            if (IsDead || boss == null) return;

            boss.TakeDamage(damage);
            totalDamageDealt += damage;
            AddThreat(damage);
        }

        /// <summary>
        /// 스킬 사용 (등반 레이드 모드).
        /// 직업 클래스에서 override하여 실제 스킬 이펙트를 구현하세요.
        /// </summary>
        public virtual void UseSkill(int idx)
        {
            if (IsDead) return;
            PlayAnimation("Attack");
            Debug.Log($"[{characterName}] 스킬 {idx}번 (베이스 — 직업 클래스에서 override 필요)");
        }

        public virtual void UseUltimate()
        {
            if (IsDead) return;
            PlayAnimation("Attack");
            Debug.Log($"[{characterName}] 궁극기 사용 (베이스 — 직업 클래스에서 override 필요)");
        }

        public void TryUseSkill(int index)
        {
            UseSkill(index);
        }

        public void TryUseUltimate()
        {
            UseUltimate();
        }

        // ═══════════════════════════════════════════════════════════
        //  유틸리티 (파티 연동)
        // ═══════════════════════════════════════════════════════════

        /// <summary>パーティ 전체에게 회복을 적용합니다. Priest/Druid 궁극기용</summary>
        public void HealAllParty(float amount)
        {
            ApplyPartyBuff(ally => ally.Heal(amount, this));
        }

        /// <summary>파티 중 현재 체력 비율이 가장 낮은 아군을 찾습니다.</summary>
        public CharacterBase FindLowestHPAlly()
        {
            if (CombatManager.Instance == null) return this;
            
            CharacterBase lowest = this;
            float lowestRatio = currentHealth / maxHealth;

            foreach (var ally in CombatManager.Instance.activePlayers)
            {
                if (ally != null && !ally.IsDead)
                {
                    float ratio = ally.currentHealth / ally.maxHealth;
                    if (ratio < lowestRatio)
                    {
                        lowestRatio = ratio;
                        lowest = ally;
                    }
                }
            }
            return lowest;
        }

        /// <summary>파티원 전체에게 특정 효과(델리게이트)를 적용합니다.</summary>
        public void ApplyPartyBuff(System.Action<CharacterBase> action)
        {
            if (CombatManager.Instance == null)
            {
                action?.Invoke(this);
                return;
            }

            foreach (var ally in CombatManager.Instance.activePlayers)
            {
                if (ally != null && !ally.IsDead)
                {
                    action?.Invoke(ally);
                }
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  애니메이션
        // ═══════════════════════════════════════════════════════════

        public void PlayAnimation(string triggerName)
        {
            if (_animator == null) return;
            if (IsDead && triggerName != "Die") return;

            _animator.ResetTrigger("Idle");
            _animator.ResetTrigger("Move");
            _animator.ResetTrigger("Attack");
            _animator.ResetTrigger("Hit");
            _animator.ResetTrigger("Die");

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

        public void OnClickUpgradeAttackPower()
        {
            attackPowerUpgrade.TryUpgrade();
        }
    }
}
