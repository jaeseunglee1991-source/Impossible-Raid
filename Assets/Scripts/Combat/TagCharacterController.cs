using UnityEngine;
using System.Collections;
using BossRaid.Combat.Boss;
using BossRaid.UI;
using BossRaid.Managers;

namespace BossRaid.Combat
{
    /// <summary>
    /// 파티 태그(Tag) 시스템의 View/Controller.
    /// - isPlayerControlled == true  → 플레이어 입력(수동 2스킬) 처리
    /// - isPlayerControlled == false → 직업별 방치형(Auto) AI 루프 처리
    ///
    /// CharacterBase를 요구(RequireComponent)하며, Status 데이터를
    /// CharacterStatus에 위임하여 오브젝트가 꺼져도 상태가 유지됨.
    ///
    /// 네이밍 충돌 방지: Unity 기본 CharacterController와 구별하기 위해
    /// TagCharacterController 로 명명.
    /// </summary>
    [RequireComponent(typeof(CharacterBase))]
    public class TagCharacterController : MonoBehaviour
    {
        // ──────────────────────────────────────────────
        // 공개 상태
        // ──────────────────────────────────────────────
        [Header("Tag System")]
        [Tooltip("true=수동 조작 / false=AI 자동 방치")]
        public bool isPlayerControlled = false;

        /// <summary>SetActive(false) 이후에도 유지되는 데이터 모델</summary>
        [HideInInspector]
        public CharacterStatus Status = new CharacterStatus();

        // ──────────────────────────────────────────────
        // 내부 참조
        // ──────────────────────────────────────────────
        private CharacterBase _charBase;
        private BossAI        _boss;
        private Coroutine     _aiCoroutine;
        private SpriteRenderer _spriteRenderer;

        // ──────────────────────────────────────────────
        // AI 상태 머신
        // ──────────────────────────────────────────────
        private enum AIState { Idle, ChasingBoss, UsingSkill1, UsingSkill2, Repositioning }
        private AIState _aiState = AIState.Idle;

        // AI 루프 틱 간격 (모바일 CPU 최적화)
        private const float AI_TICK_INTERVAL = 0.25f;

        // ──────────────────────────────────────────────
        // Unity 생명주기
        // ──────────────────────────────────────────────
        private void Awake()
        {
            _charBase = GetComponent<CharacterBase>();
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            // CharacterBase 스탯을 CharacterStatus에 동기화
            SyncStatusFromBase();
        }

        private void Start()
        {
            // 보스 참조 확보
            _boss = FindFirstObjectByType<BossAI>();
        }

        private void OnEnable()
        {
            // 태그 인(Tag-In): 오브젝트 활성화 시 쿨타임 UI 즉각 갱신
            RefreshCooldownUI();

            if (!isPlayerControlled)
                StartAILoop();
        }

        private void OnDisable()
        {
            // 태그 아웃(Tag-Out): AI 정지 (Status는 메모리에 살아있음)
            StopAILoop();
        }

        private void Update()
        {
            if (Status.IsDead) return;
            if (!isPlayerControlled) return; // AI는 코루틴으로 처리

            HandlePlayerInput();
        }

        // ──────────────────────────────────────────────
        // 조작 전환 API (BattleManager에서 호출)
        // ──────────────────────────────────────────────
        public void SetPlayerControl(bool controlled)
        {
            if (isPlayerControlled == controlled) return;
            isPlayerControlled = controlled;

            if (controlled)
            {
                StopAILoop();
                RefreshCooldownUI();
            }
            else
            {
                StartAILoop();
            }
        }

        // ──────────────────────────────────────────────
        // 수동 입력 처리 (isPlayerControlled == true)
        // ──────────────────────────────────────────────
        private void HandlePlayerInput()
        {
            // HUD 스킬 버튼 또는 키보드(테스트 기준):
            //   Q  → 일반기(Skill1)
            //   W  → 생존기/궁극기(Skill2)

            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.qKey.wasPressedThisFrame)
                RequestSkill1();

            if (keyboard.wKey.wasPressedThisFrame)
                RequestSkill2();
        }

        // ──────────────────────────────────────────────
        // 스킬 실행 (HUD 버튼에서도 직접 호출 가능)
        // ──────────────────────────────────────────────
        public void RequestSkill1()
        {
            if (Status.IsDead) return;
            if (!Status.TryUseSkill1(Time.time)) return;

            _charBase.TryUseSkill(0); // CharacterBase의 실제 스킬 로직 실행
            RefreshCooldownUI();
            Debug.Log($"[Tag] {Status.characterName} Skill1 사용");
        }

        public void RequestSkill2()
        {
            if (Status.IsDead) return;
            if (!Status.TryUseSkill2(Time.time)) return;

            _charBase.TryUseUltimate(); // 생존기/궁극기 = CharacterBase의 Ultimate
            RefreshCooldownUI();
            Debug.Log($"[Tag] {Status.characterName} Skill2(생존/궁극) 사용");
        }

        // ──────────────────────────────────────────────
        // 방치형 AI 루프 (isPlayerControlled == false)
        // ──────────────────────────────────────────────
        private void StartAILoop()
        {
            if (_aiCoroutine != null) StopCoroutine(_aiCoroutine);
            _aiCoroutine = StartCoroutine(AILoop());
        }

        private void StopAILoop()
        {
            if (_aiCoroutine != null)
            {
                StopCoroutine(_aiCoroutine);
                _aiCoroutine = null;
            }
        }

        private IEnumerator AILoop()
        {
            while (!Status.IsDead)
            {
                if (_boss == null)
                    _boss = FindFirstObjectByType<BossAI>();

                if (_boss != null && _boss.currentHealth > 0)
                    RunAITick();

                yield return new WaitForSeconds(AI_TICK_INTERVAL);
            }
        }

        private void RunAITick()
        {
            switch (Status.role)
            {
                case CharacterRole.Tank:
                    RunTankAI();
                    break;
                case CharacterRole.MeleeDPS:
                    RunMeleeDpsAI();
                    break;
                case CharacterRole.RangedDPS:
                    RunRangedDpsAI();
                    break;
                case CharacterRole.Healer:
                    RunHealerAI();
                    break;
            }
        }

        // ── Tank AI: 보스에게 최우선 접근, 어그로 획득, 생존기(Skill2) 위기 시 발동 ──
        private void RunTankAI()
        {
            MoveTowardsBoss(stopRange: 1.5f);

            // 생존기: 체력 40% 이하 → Skill2(방어/생존) 즉시 사용
            float hpRatio = Status.CurrentHP / Status.MaxHP;
            if (hpRatio < 0.4f && Status.TryUseSkill2(Time.time))
            {
                _charBase.TryUseUltimate();
                Debug.Log($"[AI:Tank] {Status.characterName} 생존기 발동! HP:{hpRatio:P0}");
                return;
            }

            // 일반기: 쿨 完 → 도발/어그로 스킬
            if (Status.TryUseSkill1(Time.time))
                _charBase.TryUseSkill(0);
        }

        // ── Melee DPS AI : 보스 배후 포지셔닝, 광역기 회피 최우선 ──
        private void RunMeleeDpsAI()
        {
            PositionBehindBoss(behindRange: 1.8f);

            // 광역기 회피: 보스가 캐스팅 중이고 Skill2(회피기) 준비 완료 시 즉시 사용
            if (_boss.isCasting && Status.TryUseSkill2(Time.time))
            {
                _charBase.TryUseUltimate();
                Debug.Log($"[AI:Rogue] {Status.characterName} 회피기 발동! (보스 광역기 감지)");
                return;
            }

            // 일반 딜사이클
            if (Status.TryUseSkill1(Time.time))
                _charBase.TryUseSkill(0);
        }

        // ── Ranged DPS AI: 전사와 최대 거리 유지, 쫄몹 최우선 점사 ──
        private void RunRangedDpsAI()
        {
            KeepMaxRange(optimalRange: 6f);

            // TODO: 쫄몹(Adds) 감지 및 점사 로직 (Add 스폰 이벤트 구독)
            // 현재는 보스 단일 딜사이클
            if (Status.TryUseSkill1(Time.time))
                _charBase.TryUseSkill(0);

            if (Status.TryUseSkill2(Time.time))
                _charBase.TryUseUltimate();
        }

        // ── Healer AI: 최저 체력 아군 치유, 파티 안정화 ──
        private void RunHealerAI()
        {
            // 파티 전체 HP 확인
            var battle = BattleManager.Instance;
            if (battle == null) return;

            CharacterBase lowestAlly  = null;
            float lowestHpRatio = float.MaxValue;

            foreach (var member in battle.GetAllPartyMembers())
            {
                if (member.IsDead) continue;
                float ratio = member.currentHealth / member.maxHealth;
                if (ratio < lowestHpRatio) { lowestHpRatio = ratio; lowestAlly = member; }
            }

            // 단일 힐: 최저 체력 아군
            if (lowestAlly != null && lowestHpRatio < 0.6f && Status.TryUseSkill1(Time.time))
                _charBase.TryUseSkill(0);

            // 광역 힐: 파티 평균 HP 50% 이하
            if (AveragePartyHpRatio(battle) < 0.5f && Status.TryUseSkill2(Time.time))
                _charBase.TryUseUltimate();
        }

        // ──────────────────────────────────────────────
        // 이동 유틸리티 (2D XY 평면)
        // ──────────────────────────────────────────────
        private void MoveTowardsBoss(float stopRange)
        {
            if (_boss == null) return;
            Vector2 dir = ((Vector2)_boss.transform.position - (Vector2)transform.position).normalized;
            float dist  = Vector2.Distance(transform.position, _boss.transform.position);
            if (dist > stopRange)
                transform.position += new Vector3(dir.x, dir.y, 0f)
                                      * _charBase.movementSpeed * AI_TICK_INTERVAL;
            FlipSprite(dir.x);
        }

        private void PositionBehindBoss(float behindRange)
        {
            if (_boss == null) return;
            // 보스 기준 반대편( opposite direction of currentTarget → player )
            Vector2 toBoss = ((Vector2)_boss.transform.position - (Vector2)transform.position).normalized;
            Vector2 behindPos = (Vector2)_boss.transform.position - toBoss * behindRange;
            Vector2 toTarget = (behindPos - (Vector2)transform.position).normalized;
            float dist = Vector2.Distance(transform.position, behindPos);
            if (dist > 0.5f)
                transform.position += new Vector3(toTarget.x, toTarget.y, 0f)
                                      * _charBase.movementSpeed * AI_TICK_INTERVAL;
            FlipSprite(toTarget.x);
        }

        private void KeepMaxRange(float optimalRange)
        {
            if (_boss == null) return;
            float dist = Vector2.Distance(transform.position, _boss.transform.position);
            Vector2 dir = ((Vector2)transform.position - (Vector2)_boss.transform.position).normalized;
            // 너무 가까우면 뒤로, 너무 멀면 앞으로
            if (dist < optimalRange - 1f)
                transform.position += new Vector3(dir.x, dir.y, 0f)
                                      * _charBase.movementSpeed * AI_TICK_INTERVAL;
            else if (dist > optimalRange + 1f)
                transform.position += new Vector3(-dir.x, -dir.y, 0f)
                                      * _charBase.movementSpeed * AI_TICK_INTERVAL;
            FlipSprite(-dir.x);
        }

        private void FlipSprite(float directionX)
        {
            if (_spriteRenderer != null)
                _spriteRenderer.flipX = (directionX < 0f);
        }

        // ──────────────────────────────────────────────
        // 헬퍼
        // ──────────────────────────────────────────────
        private float AveragePartyHpRatio(BattleManager battle)
        {
            var members = battle.GetAllPartyMembers();
            if (members == null || members.Count == 0) return 1f;
            float sum = 0f;
            int aliveCount = 0;
            foreach (var m in members)
            {
                if (m.IsDead) continue;
                sum += m.currentHealth / m.maxHealth;
                aliveCount++;
            }
            return aliveCount == 0 ? 1f : sum / aliveCount;
        }

        /// <summary>태그 인 시 HUD의 스킬 쿨타임 UI를 현재 Status 기준으로 갱신</summary>
        public void RefreshCooldownUI()
        {
            // TODO: InGameHUDController.Instance?.RefreshSkillCooldown(this);
        }

        /// <summary>CharacterBase의 스탯을 CharacterStatus로 초기 동기화</summary>
        private void SyncStatusFromBase()
        {
            if (_charBase == null) return;
            Status.characterName = _charBase.characterName;
            Status.role = _charBase.role;
            Status.MaxHP = _charBase.maxHealth;
            Status.CurrentHP = _charBase.currentHealth;

            // 스킬 쿨타임 동기화 (CharacterBase의 skillCooldowns[0], [2] → Skill1, Skill2)
            if (_charBase.skillCooldowns != null && _charBase.skillCooldowns.Length >= 1)
                Status.Skill1MaxCooldown = _charBase.skillCooldowns[0];
            if (_charBase.ultimateCooldown > 0f)
                Status.Skill2MaxCooldown = _charBase.ultimateCooldown;
        }
    }
}
