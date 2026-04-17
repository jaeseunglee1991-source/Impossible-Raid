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
        private BossAI currentBoss;
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

        private void Awake()
        {
            characterBase = GetComponent<CharacterBase>();
            cc = GetComponent<CharacterController>();
        }

        private void Start()
        {
            inGameHUD = FindObjectOfType<InGameHUDController>();
            FindBoss();
        }

        public void StartCombat()
        {
            isCombatActive = true;
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
                currentBoss = FindObjectOfType<BossAI>();
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
            if (currentBoss == null) return;

            // 보스 주변을 맴돌며 대기 (공격은 하지 않음)
            float distanceToBoss = Vector3.Distance(transform.position, currentBoss.transform.position);

            if (distanceToBoss > followDistance)
            {
                Vector3 direction = (currentBoss.transform.position - transform.position).normalized;
                direction.y = 0;
                cc.Move(direction * (moveSpeed * 0.7f) * Time.deltaTime);
                
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
                
                characterBase.PlayAnimation("Move");
            }
            else
            {
                // 보스를 바라보며 대기
                Vector3 lookDir = currentBoss.transform.position - transform.position;
                lookDir.y = 0;
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * 5f);
                
                characterBase.PlayAnimation("Idle");
            }
        }

        /// <summary>
        /// [신규 추가] 방치형 모드에서 4인이 동시에 보스를 자동 타격하는 AI 로직
        /// </summary>
        private void ExecuteIdleAutoBattle()
        {
            if (currentBoss == null)
            {
                FindBoss();
                return;
            }

            float distanceToBoss = Vector3.Distance(transform.position, currentBoss.transform.position);

            // 1. 보스가 사거리보다 멀면 다가감
            if (distanceToBoss > attackRange)
            {
                Vector3 direction = (currentBoss.transform.position - transform.position).normalized;
                direction.y = 0;
                
                // CharacterController를 이용한 이동
                cc.Move(direction * moveSpeed * Time.deltaTime);
                
                // 보스 방향으로 회전
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
                
                characterBase.PlayAnimation("Move");
            }
            // 2. 사거리 내에 들어오면 자동 공격 수행
            else
            {
                // 보스 바라보기
                Vector3 lookDir = currentBoss.transform.position - transform.position;
                lookDir.y = 0;
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * 15f);

                // 쿨타임 체크 (기본 평타 및 스킬 자동 캐스팅)
                if (Time.time >= lastAIAttackTime + characterBase.baseAttackCooldown)
                {
                    // TODO: 나중에 장착된 2개의 스킬(EquippedSkills)의 쿨타임을 먼저 체크하고 
                    // 사용 가능한 스킬이 있으면 스킬 시전, 없으면 평타(기본 공격) 시전하는 로직으로 발전시킬 수 있습니다.
                    
                    characterBase.PlayAnimation("Attack"); // SPUM 등의 공격 애니메이션 호출
                    
                    // 보스에게 데미지 적용
                    characterBase.DealDamageTo(currentBoss, characterBase.baseAttackPower);
                    
                    lastAIAttackTime = Time.time;
                }
                else
                {
                    characterBase.PlayAnimation("Idle");
                }
            }
        }
    }
}
