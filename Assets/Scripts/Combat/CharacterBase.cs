using UnityEngine;
using System;
using System.Collections.Generic;
using BossRaid.Combat.Boss;

namespace BossRaid.Combat
{
    public enum CharacterRole { Tank, MeleeDPS, RangedDPS, Healer }

    /// <summary>
    /// 모든 캐릭터의 기반 클래스
    /// 자동 평타, 스킬 쿨다운, 버프/디버프, 보호막 시스템 포함
    /// </summary>
    public abstract class CharacterBase : MonoBehaviour
    {
        [Header("Base Stats")]
        public string characterName;
        public CharacterRole role;
        public float maxHealth = 1000f;
        public float currentHealth;

        // [New] 이벤트 기반 최적화 (모바일 CPU 부하 제로 지향)
        public event Action<float, float> OnHealthChanged; // (currentHealth, maxHealth)
        
        [Header("Combat Stats")]
        public float movementSpeed = 5f;
        public float attackRange = 2f;
        public float attackSpeed = 1f;
        public float autoAttackDamage = 30f;

        [Header("Threat (Aggro) State")]
        public float currentThreat = 0f;
        public bool IsDead => currentHealth <= 0;

        [Header("Battle Statistics")]
        public float totalDamageDealt = 0f;
        public float totalHealingDone = 0f;
        public float totalDamageTaken = 0f;
        public float aggroDuration = 0f;

        // 스킬 시스템
        [Header("Skill System")]
        public string[] skillNames = new string[3];
        public float[] skillCooldowns = new float[3];     // 최대 쿨다운
        public float[] skillCurrentCD = new float[3];     // 현재 남은 쿨다운
        public string ultimateName = "";
        public float ultimateCooldown = 60f;
        public float ultimateCurrentCD = 0f;

        // 버프/상태
        public float shieldAmount = 0f;                    // 보호막 잔량
        public float damageReductionMultiplier = 1f;       // 피해 감소 배율 (1=없음, 0.7=30%감소)
        public float attackPowerMultiplier = 1f;           // 공격력 배율
        public bool isInvulnerable = false;                // 무적 여부

        public bool CheckInvulnerable() => isInvulnerable;

        // 자동 평타
        private float autoAttackTimer = 0f;

        // 애니메이션 시스템
        protected Animator animator;
        private Vector3 lastPosition;
        private float currentVelocity;

        // 보스 참조 (자동 평타/스킬 타겟)
        protected BossAI targetBoss;

        protected virtual void Awake()
        {
            currentHealth = maxHealth;
            skillCurrentCD = new float[3];
            lastPosition = transform.position;
            
            // 프리팹 내부의 애니메이터 자동 탐색
            animator = GetComponentInChildren<Animator>();
        }

        // [성장 스탯] 원본 데이터 보존용
        private float _originMaxHealth;
        private float _originAutoAttackDamage;
        private float _originAttackSpeed;

        protected virtual void Start()
        {
            // 하위 클래스(Warrior 등)가 Awake에서 덮어씌운 기초 스탯을 저장
            _originMaxHealth = maxHealth;
            _originAutoAttackDamage = autoAttackDamage;
            _originAttackSpeed = attackSpeed;

            if (BossRaid.Managers.GrowthManager.Instance != null)
            {
                BossRaid.Managers.GrowthManager.Instance.OnGrowthStatsChanged += RecalculateStats;
                RecalculateStats(); // 씬 시작 시 최초 계산 적용
            }
        }

        protected virtual void OnDestroy()
        {
            if (BossRaid.Managers.GrowthManager.Instance != null)
            {
                BossRaid.Managers.GrowthManager.Instance.OnGrowthStatsChanged -= RecalculateStats;
            }
        }

        public void RecalculateStats()
        {
            if (BossRaid.Managers.GrowthManager.Instance == null) return;
            var growth = BossRaid.Managers.GrowthManager.Instance;

            // 최대 체력 갱신 전 현재 비율 유지
            float hpRatio = currentHealth / Mathf.Max(maxHealth, 1f);

            // 1단위 고정 수치 합산 방식 (A, B 경, 해 인플레이션 방지)
            maxHealth = _originMaxHealth + growth.GetBonusHealth();
            autoAttackDamage = _originAutoAttackDamage + growth.GetBonusAttack();
            
            // 공속은 낮을수록 빠른 쿨다운이므로 마이너스 처리. (최소 공속 한계점 0.1초 제한)
            attackSpeed = Mathf.Max(0.1f, _originAttackSpeed - growth.GetBonusAttackSpeedReduction());

            currentHealth = maxHealth * hpRatio;
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            
            Debug.Log($"<color=cyan>[스탯 갱신 완료] {characterName} - HP({maxHealth}), ATK({autoAttackDamage}), ASPD({attackSpeed})</color>");
        }

        protected virtual void Update()
        {
            // 1. 이동 애니메이션 처리
            UpdateMovementAnimation();

            // 쿨다운 틱
            for (int i = 0; i < skillCurrentCD.Length; i++)
            {
                if (skillCurrentCD[i] > 0) skillCurrentCD[i] -= Time.deltaTime;
            }
            if (ultimateCurrentCD > 0) ultimateCurrentCD -= Time.deltaTime;

            // 어그로 시간 체크
            UpdateAggroTimer();

            // 자동 평타
            if (!IsDead) HandleAutoAttack();
        }

        private void UpdateMovementAnimation()
        {
            if (animator == null) return;

            // 2D: XY 평면 이동 속도만 계산 (Z 무시)
            float distXY = Vector2.Distance(
                new Vector2(transform.position.x, transform.position.y),
                new Vector2(lastPosition.x, lastPosition.y));
            currentVelocity = distXY / (Time.deltaTime > 0 ? Time.deltaTime : 0.01f);
            lastPosition = transform.position;

            animator.SetFloat("MovementSpeed", currentVelocity);
        }

        private void UpdateAggroTimer()
        {
            if (IsDead) return;
            // 보스의 타겟인지 확인
            var boss = FindFirstObjectByType<BossAI>();
            if (boss != null && boss.currentTarget == this)
            {
                aggroDuration += Time.deltaTime;
            }
        }

        private void HandleAutoAttack()
        {
            if (targetBoss == null)
            {
                // 보스 자동 감지
                targetBoss = FindFirstObjectByType<BossAI>();
                if (targetBoss == null) return;
            }

            // 2D: XY 거리로 사거리 판정
            float dist = Vector2.Distance(
                new Vector2(transform.position.x, transform.position.y),
                new Vector2(targetBoss.transform.position.x, targetBoss.transform.position.y));
            if (dist <= attackRange)
            {
                autoAttackTimer -= Time.deltaTime;
                if (autoAttackTimer <= 0f)
                {
                    // [Animation] 평타 모션 재생
                    PlayAnimation("Attack");

                    float damage = autoAttackDamage * attackPowerMultiplier;
                    targetBoss.TakeDamage(damage);
                    totalDamageDealt += damage; // 통계 추가
                    AddThreat(damage * 0.5f); // 위협 = 피해의 절반
                    autoAttackTimer = attackSpeed;
                }
            }
        }

        public void PlayAnimation(string triggerName)
        {
            if (animator != null && !IsDead)
            {
                animator.SetTrigger(triggerName);
            }
        }

        public virtual void Heal(float amount)
        {
            if (IsDead) return;
            currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        public void AddThreat(float amount)
        {
            if (IsDead) return;
            currentThreat += amount;
        }

        public void DealDamage(float amount, float threatMultiplier = 0.5f)
        {
            if (targetBoss != null)
            {
                targetBoss.TakeDamage(amount);
                totalDamageDealt += amount;
                AddThreat(amount * threatMultiplier);
            }
        }

        public void HealTarget(CharacterBase target, float amount)
        {
            if (target != null && !target.IsDead)
            {
                target.Heal(amount);
                totalHealingDone += amount;
            }
        }

        public virtual void TakeDamage(float amount)
        {
            if (isInvulnerable) return;

            float originalDamage = amount;

            // 보호막 먼저 소모
            if (shieldAmount > 0)
            {
                if (shieldAmount >= amount) { shieldAmount -= amount; totalDamageTaken += originalDamage; return; }
                amount -= shieldAmount;
                shieldAmount = 0;
            }

            // 피해 감소 적용
            amount *= damageReductionMultiplier;
            currentHealth -= amount;
            totalDamageTaken += originalDamage; // 실제 들어온 피해(보호막 포함) 기록
            
            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            if (currentHealth <= 0) Die();
        }

        protected virtual void Die()
        {
            currentHealth = 0f;
            Debug.Log($"<color=red>[Combat] {characterName} 사망!</color>");

            // 파티 전멸 여부 체크 → BattleManager에 위임
            if (BossRaid.Managers.BattleManager.Instance != null)
                BossRaid.Managers.BattleManager.Instance.CheckGameOver();
        }

        /// <summary>스킬 사용 (쿨다운 체크 포함)</summary>
        public void TryUseSkill(int skillIndex)
        {
            if (IsDead) return;
            if (skillIndex < 0 || skillIndex >= skillCurrentCD.Length) return;
            if (skillCurrentCD[skillIndex] > 0) return;

            // [Animation] 스킬 공용 모션 재생 (AnyRPG 컨트롤러 대응)
            PlayAnimation("Skill");

            UseSkill(skillIndex);
            skillCurrentCD[skillIndex] = skillCooldowns[skillIndex];
        }

        /// <summary>궁극기 사용 (쿨다운 체크 포함)</summary>
        public void TryUseUltimate()
        {
            if (IsDead) return;
            if (ultimateCurrentCD > 0) return;

            // [Animation] 궁극기 전용 모션 재생
            PlayAnimation("Ultimate");

            UseUltimate();
            ultimateCurrentCD = ultimateCooldown;
        }

        public abstract void UseSkill(int skillIndex);
        public abstract void UseUltimate();

        // ===== 유틸리티 =====

        /// <summary>가장 체력이 낮은 아군 찾기 (힐러용)</summary>
        protected CharacterBase FindLowestHPAlly()
        {
            var combat = CombatManager.Instance;
            if (combat == null) return null;

            CharacterBase lowest = null;
            float lowestRatio = float.MaxValue;
            foreach (var p in combat.activePlayers)
            {
                if (p.IsDead) continue;
                float ratio = p.currentHealth / p.maxHealth;
                if (ratio < lowestRatio) { lowestRatio = ratio; lowest = p; }
            }
            return lowest;
        }

        /// <summary>파티 전체 힐</summary>
        protected void HealAllParty(float amount)
        {
            var combat = CombatManager.Instance;
            if (combat == null) return;
            foreach (var p in combat.activePlayers)
            {
                if (!p.IsDead)
                {
                    p.Heal(amount);
                    totalHealingDone += amount; // 통계 추가
                }
            }
        }

        /// <summary>파티 전체 버프</summary>
        protected void ApplyPartyBuff(System.Action<CharacterBase> buff)
        {
            var combat = CombatManager.Instance;
            if (combat == null) return;
            foreach (var p in combat.activePlayers)
            {
                if (!p.IsDead) buff(p);
            }
        }
    }
}
