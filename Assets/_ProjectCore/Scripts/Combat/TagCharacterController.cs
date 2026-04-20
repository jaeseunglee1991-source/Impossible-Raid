using UnityEngine;
using System.Collections;
using BossRaid.Combat.Boss;
using BossRaid.UI;
using BossRaid.Managers;

namespace BossRaid.Combat
{
    /// <summary>
    /// 파티 태그(Tag) 시스템의 View/Controller.
    /// 방치형 모드(IdleAIMode)가 켜지면 4인 동시 자동 전투 수행.
    /// </summary>
    public class TagCharacterController : MonoBehaviour
    {
        [Header("References")]
        public CharacterBase characterBase;
        private Boss.IBossPatternHandler currentBoss;
        private InGameHUDController inGameHUD;
        private VirtualJoystick joystick;

        [Header("State")]
        public bool isPlayerControlled = false;
        public bool isCombatActive = false;
        
        // [신규 추가] 방치형 AI 모드 상태 변수
        public bool isIdleAIMode = false;

        [Header("Movement (Player)")]
        public float moveSpeed = 3f;
        private Vector3 movementInput;
        private CharacterController cc;

        [Header("AI Behavior (Partner / Idle)")]
        public float followDistance = 3f;
        public float attackRange = 2f;
        private float lastAIAttackTime;
        private string lastAnim;

        private void Awake()
        {
            characterBase = GetComponent<CharacterBase>();
            cc = GetComponent<CharacterController>();
            if (cc == null)
            {
                cc = gameObject.AddComponent<CharacterController>();
                // 2D SPUM 캐릭터에 맞춘 기본 캐릭터 컨트롤러 크기 설정
                cc.center = new Vector3(0, 0.5f, 0);
                cc.radius = 0.3f;
                cc.height = 1.0f;
            }
        }

        private void Start()
        {
            inGameHUD = InGameHUDController.Instance;
            joystick = FindFirstObjectByType<VirtualJoystick>();
            ManualInit(); // 초기 참조 연결
            FindBoss();
        }

        /// <summary>
        /// 컴포넌트가 런타임에 추가되었을 때 수동으로 호출하여 참조를 즉시 연결합니다.
        /// </summary>
        public void ManualInit()
        {
            if (characterBase == null) characterBase = GetComponent<CharacterBase>();
            if (cc == null) cc = GetComponent<CharacterController>();
            if (cc == null)
            {
                cc = gameObject.AddComponent<CharacterController>();
                cc.center = new Vector3(0, 0.5f, 0);
                cc.radius = 0.3f;
                cc.height = 1.0f;
            }
        }

        private void OnMouseDown()
        {
            if (BattleManager.Instance != null && !characterBase.IsDead)
            {
                BattleManager.Instance.SetControlledCharacter(this);
            }
        }

        public void StartCombat()
        {
            isCombatActive = true;
            Debug.Log($"[TagCharacterController] {characterBase?.characterName} 전투 시작 (isCombatActive = true)");
            FindBoss();
        }

        public void StopCombat()
        {
            isCombatActive = false;
            movementInput = Vector3.zero;
        }

        // [신규 추가] 방치형 모드를 켜고 끄는 메서드
        public void EnableIdleAIMode(bool enable)
        {
            isIdleAIMode = enable;
            isPlayerControlled = !enable; // 방치형일 땐 수동 조작 불가
        }

        private void FindBoss()
        {
            // 1. 레이드 보스(BossAI) 먼저 탐색
            var raidBoss = FindFirstObjectByType<BossAI>();
            if (raidBoss != null)
            {
                currentBoss = raidBoss;
                return;
            }

            // 2. 일반 몬스터(IdleBoss) 탐색
            var idleBoss = FindFirstObjectByType<IdleBoss>();
            if (idleBoss != null)
            {
                currentBoss = idleBoss;
                return;
            }

            // 3. [보완] 스테이지의 일반 적(Minion) 탐색 (이름 기반 또는 CharacterBase가 없는 Target 탐색 등)
            // 현재는 IdleBoss가 잡몹 역할도 병행하므로 IdleBoss를 우선적으로 찾습니다.
            // 만약 캐릭터들이 여전히 가만히 있다면, 몬스터에 IdleBoss 스크립트가 붙어 있는지 확인이 필요합니다.
        }

        private void Update()
        {
            if (characterBase == null || characterBase.IsDead || !isCombatActive) return;

            // [신규 추가] 방치형 모드일 경우 무조건 자동 사냥 로직만 수행
            if (isIdleAIMode)
            {
                ExecuteIdleAutoBattle();
                return; // 아래의 수동 조작/파트너 AI 로직 무시
            }

            // 기존 로직: 플레이어 조작 vs 태그 파트너 대기
            if (isPlayerControlled)
            {
                HandleMovement();
                HandleCombatInput();
            }
            else
            {
                PartnerAIBehaviour();
            }
        }

        private void HandleMovement()
        {
            Vector3 moveDir = Vector3.zero;

            // 1. 조이스틱 입력 체크 (우선순위 1)
            if (joystick != null && joystick.InputDirection.sqrMagnitude > 0.01f)
            {
                moveDir = new Vector3(joystick.InputDirection.x, 0f, joystick.InputDirection.y);
            }
            // 2. 키보드 입력 체크 (우선순위 2 - 테스트용)
            else
            {
                float h = Input.GetAxisRaw("Horizontal");
                float v = Input.GetAxisRaw("Vertical");
                moveDir = new Vector3(h, 0f, v);
            }

            movementInput = moveDir.normalized;

            if (movementInput.sqrMagnitude > 0.01f)
            {
                cc.Move(movementInput * moveSpeed * Time.deltaTime);

                Quaternion targetRotation = Quaternion.LookRotation(movementInput);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 15f);

                characterBase.PlayAnimation("Move");
            }
            else
            {
                characterBase.PlayAnimation("Idle");
            }
        }

        private void HandleCombatInput()
        {
            // 스킬 1 사용 (예: Q 키)
            if (Input.GetKeyDown(KeyCode.Q))
            {
                characterBase.targetBoss = currentBoss;
                characterBase.UseSkill(0);
            }
            // 스킬 2 사용 (예: E 키)
            else if (Input.GetKeyDown(KeyCode.E))
            {
                characterBase.targetBoss = currentBoss;
                characterBase.UseSkill(1);
            }
        }

        /// <summary>
        /// 태그 시스템에서 대기 중인 파트너의 행동 로직
        /// </summary>
        private void PartnerAIBehaviour()
        {
            var bossMonobehaviour = (currentBoss as MonoBehaviour);
            if (bossMonobehaviour == null) { FindBoss(); return; }

            float distanceToBoss = Vector3.Distance(transform.position, bossMonobehaviour.transform.position);
            var bossCollider = bossMonobehaviour.GetComponent<Collider>();
            if (bossCollider != null) distanceToBoss -= bossCollider.bounds.extents.magnitude * 0.4f;

            float actualMoveSpeed = characterBase.movementSpeed > 0 ? characterBase.movementSpeed : moveSpeed;

            // 보스 주변을 맴돌며 대기 (공격은 하지 않음)
            if (distanceToBoss > followDistance)
            {
                Vector3 direction = (bossMonobehaviour.transform.position - transform.position).normalized;
                direction.y = 0;
                cc.Move(direction * (actualMoveSpeed * 0.7f) * Time.deltaTime);
                
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
                
                if (lastAnim != "Move")
                {
                    characterBase.PlayAnimation("Move");
                    lastAnim = "Move";
                }
            }
            else
            {
                // 보스를 바라보며 대기
                Vector3 lookDir = bossMonobehaviour.transform.position - transform.position;
                lookDir.y = 0;
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * 5f);
                
                if (lastAnim != "Idle")
                {
                    characterBase.PlayAnimation("Idle");
                    lastAnim = "Idle";
                }
            }
        }

        /// <summary>
        /// [신규 추가] 방치형 모드에서 4인이 동시에 보스를 자동 타격하는 AI 로직
        /// </summary>
        private void ExecuteIdleAutoBattle()
        {
            var bossMonobehaviour = currentBoss as MonoBehaviour;
            if (bossMonobehaviour == null)
            {
                // 보스를 못 찾았다면 다시 시도
                if (Time.frameCount % 60 == 0) Debug.Log($"[AI] {characterBase.characterName} 보스 탐색 중...");
                FindBoss();
                return;
            }

            // 거리 계산
            float distanceToBoss = Vector3.Distance(transform.position, bossMonobehaviour.transform.position);
            var bossCollider = bossMonobehaviour.GetComponent<Collider>();
            if (bossCollider != null)
            {
                distanceToBoss -= bossCollider.bounds.extents.magnitude * 0.5f;
            }

            // 스탯 동기화: 컨트롤러 변수가 아니라 CharacterBase(SO 데이터)를 우선적으로 사용
            float actualAttackRange = characterBase.attackRange > 0 ? characterBase.attackRange : attackRange;
            float actualMoveSpeed   = characterBase.movementSpeed > 0 ? characterBase.movementSpeed : moveSpeed;

            // ═══════════════════════════════════════════════════════
            //  위험 구역(DangerZone) 회피 AI
            //  - 탱커는 장판을 무시하고 어그로 유지 (FF14 탱커 철학)
            //  - 나머지 직업은 즉시 안전 지대로 이동
            // ═══════════════════════════════════════════════════════
            if (characterBase.role != CharacterRole.Tank)
            {
                DangerZone nearestDanger;
                if (DangerZoneManager.IsInAnyDanger(transform.position, out nearestDanger))
                {
                    // 탈출 방향 계산
                    Vector3 escapeDir = nearestDanger.GetEscapeDirection(transform.position);
                    escapeDir.y = 0;

                    // 회피 속도는 기본 이속의 1.5배 (긴급 회피)
                    float escapeSpeed = actualMoveSpeed * 1.5f;
                    cc.Move(escapeDir * escapeSpeed * Time.deltaTime);

                    // 보스를 바라보면서 뒤로 빠지기
                    Vector3 lookAtBoss = (bossMonobehaviour.transform.position - transform.position);
                    lookAtBoss.y = 0;
                    if (lookAtBoss.sqrMagnitude > 0.01f)
                        transform.rotation = Quaternion.Slerp(transform.rotation,
                            Quaternion.LookRotation(lookAtBoss.normalized), Time.deltaTime * 12f);

                    if (lastAnim != "Move")
                    {
                        characterBase.PlayAnimation("Move");
                        lastAnim = "Move";
                    }
                    return; // 회피 중에는 공격하지 않음
                }
            }

            // ═══════════════════════════════════════════════════════
            //  역할별 타겟 위치 결정 (포지셔닝)
            // ═══════════════════════════════════════════════════════
            Vector3 targetPos = bossMonobehaviour.transform.position;
            Vector3 bossForward = bossMonobehaviour.transform.forward;
            Vector3 bossRight = bossMonobehaviour.transform.right;

            switch (characterBase.role)
            {
                case CharacterRole.Tank:
                    targetPos += bossForward * 2.5f;
                    break;
                case CharacterRole.MeleeDPS:
                    targetPos += (bossRight + (-bossForward)).normalized * 2.5f;
                    break;
                case CharacterRole.RangedDPS:
                case CharacterRole.Healer:
                    targetPos += (-bossForward) * 7.0f;
                    break;
            }

            // 목표 지점 자체가 위험 구역이면 오프셋을 줘서 안전한 곳으로 보정
            DangerZone posCheck;
            if (characterBase.role != CharacterRole.Tank &&
                DangerZoneManager.IsInAnyDanger(targetPos, out posCheck))
            {
                targetPos += posCheck.GetEscapeDirection(targetPos) * (posCheck.radius + 1f);
            }

            float distanceToTarget = Vector3.Distance(transform.position, targetPos);

            // 타겟 포지션으로 이동
            if (distanceToTarget > 0.5f)
            {
                Vector3 direction = (targetPos - transform.position).normalized;
                direction.y = 0;
                
                cc.Move(direction * actualMoveSpeed * Time.deltaTime);
                
                Vector3 lookAtBoss2 = (bossMonobehaviour.transform.position - transform.position).normalized;
                lookAtBoss2.y = 0;
                if (lookAtBoss2.sqrMagnitude > 0.01f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(lookAtBoss2);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
                }
                
                if (lastAnim != "Move")
                {
                    characterBase.PlayAnimation("Move");
                    lastAnim = "Move";
                }
            }
                // 사거리 내에 들어오면 자동 공격 수행
                else
                {
                    var bossObj = bossMonobehaviour.gameObject;

                    // 보스 바라보기
                    Vector3 lookDir = bossObj.transform.position - transform.position;
                    lookDir.y = 0;
                    if (lookDir.sqrMagnitude > 0.01f)
                        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * 15f);

                    // 스킬 자동 시전 체크
                    characterBase.targetBoss = currentBoss;
                    bool skillCasted = characterBase.TryAutoUseSkill();
                    
                    if (skillCasted) 
                    {
                        lastAIAttackTime = Time.time;
                        lastAnim = "Attack";
                    }

                    // 평타 쿨타임이 찼다면 평타 시전
                    if (!skillCasted && Time.time >= lastAIAttackTime + characterBase.attackSpeed)
                    {
                        characterBase.PlayAnimation("Attack"); 
                        lastAnim = "Attack";
                        
                        characterBase.DealDamageTo(currentBoss, characterBase.autoAttackDamage);
                        lastAIAttackTime = Time.time;
                    }
                    else if (!skillCasted && lastAnim != "Idle" && lastAnim != "Attack") 
                    {
                        characterBase.PlayAnimation("Idle");
                        lastAnim = "Idle";
                    }
                    
                    if (lastAnim == "Attack" && Time.time > lastAIAttackTime + 0.5f)
                    {
                        lastAnim = "";
                    }
                }
            }
    }
}
