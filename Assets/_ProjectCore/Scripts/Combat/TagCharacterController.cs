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

        [Header("State")]
        public bool isPlayerControlled = false;
        public bool isCombatActive = false;
        
        // [신규 추가] 방치형 AI 모드 상태 변수
        public bool isIdleAIMode = false;

        [Header("Movement (Player)")]
        public float moveSpeed = 5f;
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
        }

        private void Start()
        {
            inGameHUD = InGameHUDController.Instance;
            FindBoss();
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
            if (currentBoss == null)
            {
                // 1. 레이드 보스(BossAI) 먼저 탐색
                var raidBoss = FindFirstObjectByType<BossAI>();
                if (raidBoss != null)
                {
                    currentBoss = raidBoss;
                    return;
                }

                // 2. 없으면 일반 몬스터(IdleBoss) 탐색
                var idleBoss = FindFirstObjectByType<IdleBoss>();
                if (idleBoss != null)
                {
                    currentBoss = idleBoss;
                }
            }
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
            // 키보드 방향키 또는 WASD 입력
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");

            movementInput = new Vector3(h, 0f, v).normalized;

            if (movementInput.magnitude > 0.1f)
            {
                // 이동 처리
                cc.Move(movementInput * moveSpeed * Time.deltaTime);

                // 캐릭터 회전
                Quaternion targetRotation = Quaternion.LookRotation(movementInput);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);

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

            // 1. 보스가 사거리보다 멀면 다가감
            if (distanceToBoss > actualAttackRange)
            {
                var bossObj = (currentBoss as MonoBehaviour)?.gameObject;
                if (bossObj == null) return;

                Vector3 direction = (bossObj.transform.position - transform.position).normalized;
                direction.y = 0;
                
                // CharacterController를 이용한 이동
                cc.Move(direction * actualMoveSpeed * Time.deltaTime);
                
                // 보스 방향으로 회전
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
                
                if (lastAnim != "Move")
                {
                    characterBase.PlayAnimation("Move");
                    lastAnim = "Move";
                }
            }
                // 2. 사거리 내에 들어오면 자동 공격 수행
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
                        Debug.Log($"[AI] {characterBase.characterName} 스킬 사용!");
                    }

                    // 스킬을 안 썼고, 평타 쿨타임(attackSpeed)이 찼다면 평타 시전
                    if (!skillCasted && Time.time >= lastAIAttackTime + characterBase.attackSpeed)
                    {
                        characterBase.PlayAnimation("Attack"); 
                        lastAnim = "Attack";
                        
                        characterBase.DealDamageTo(currentBoss, characterBase.autoAttackDamage);
                        lastAIAttackTime = Time.time;
                        Debug.Log($"[AI] {characterBase.characterName} 평타 공격! (공격력: {characterBase.autoAttackDamage})");
                    }
                    else if (!skillCasted && lastAnim != "Idle" && lastAnim != "Attack") 
                    {
                        characterBase.PlayAnimation("Idle");
                        lastAnim = "Idle";
                    }
                    
                    // 공격 애니메이션이 끝났을 것으로 예상되는 시점에 상태 리셋 (애니메이터 상태 전이 보조)
                    if (lastAnim == "Attack" && Time.time > lastAIAttackTime + 0.5f)
                    {
                        lastAnim = ""; // 다음 프레임에 Idle이나 다른 애니메이션을 잡을 수 있도록 초기화
                    }
                }
            }
    }
}
