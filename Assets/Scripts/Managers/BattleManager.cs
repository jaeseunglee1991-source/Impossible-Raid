using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BossRaid.Combat;
using BossRaid.Combat.Boss; // 기존 레이드 보스 참조용
using BossRaid.UI;

namespace BossRaid.Managers
{
    /// <summary>
    /// ──────────────────────────────────────────────────────────────────
    /// BattleManager  —  파티 태그(Tag) 시스템 + 게임오버 판정 + 부활 중계
    /// [신규] 방치형 모드(IdleFarming) 시 4인 동시 스폰 및 자동 사냥 모드 분기
    /// ──────────────────────────────────────────────────────────────────
    /// </summary>
    public class BattleManager : MonoBehaviour
    {
        public static BattleManager Instance { get; private set; }

        [Header("State Settings")]
        [Tooltip("StageManager의 전역 상태를 참조하여 모드를 결정합니다.")]
        public bool IsIdleFarmingMode => StageManager.Instance != null && StageManager.Instance.CurrentState == GameState.IdleFarming;

        [Header("Character Prefabs")]
        public GameObject warriorPrefab;
        public GameObject roguePrefab;
        public GameObject magePrefab;
        public GameObject healerPrefab;

        [Header("Spawn Points - Idle Mode (방치형 4인 진형)")]
        public Transform[] idleSpawnPoints; // 인스펙터에서 4개 위치 할당 필요

        [Header("Spawn Points - Raid Mode (기존 태그)")]
        public Transform raidSpawnPoint;

        [Header("Boss Settings")]
        public GameObject idleBossPrefab; // 1% 재화 자판기 보스 프리팹
        public GameObject raidBossPrefab; // 패턴이 있는 기존 레이드 보스 프리팹
        public Transform bossSpawnPoint;

        // 전투에 참여 중인 캐릭터와 보스 추적
        public List<TagCharacterController> ActiveCharacters { get; private set; } = new List<TagCharacterController>();
        public GameObject currentBoss { get; private set; }

        private InGameHUDController inGameHUD;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            inGameHUD = InGameHUDController.Instance;
            
            // 게임 시작 시 전투 초기화 (또는 다른 매니저에서 호출)
            InitializeBattle();
        }

        /// <summary>
        /// 전투를 초기화하고 현재 모드에 따라 스폰 방식을 분기합니다.
        /// </summary>
        public void InitializeBattle()
        {
            ClearBattlefield();

            if (IsIdleFarmingMode)
            {
                SetupIdleFarmingMode();
            }
            else
            {
                SetupBossRaidMode();
            }
        }

        /// <summary>
        /// [신규] 방치형 무한 자동사냥 셋업 (4인 동시 타격)
        /// </summary>
        private void SetupIdleFarmingMode()
        {
            Debug.Log("[BattleManager] 방치형 모드 셋업 (4인 동시 사냥 AI 가동)");

            // 1. 태그 UI 숨기기 (HUD 컨트롤러에 관련 함수가 있다면 연결)
            if (inGameHUD != null)
            {
                // inGameHUD.ToggleTagUI(false); 
            }

            // 2. 4인 파티 스폰 (진형 위치)
            if (idleSpawnPoints != null && idleSpawnPoints.Length >= 4)
            {
                SpawnCharacter(warriorPrefab, idleSpawnPoints[0].position, true);
                SpawnCharacter(roguePrefab,   idleSpawnPoints[1].position, true);
                SpawnCharacter(magePrefab,    idleSpawnPoints[2].position, true);
                SpawnCharacter(healerPrefab,  idleSpawnPoints[3].position, true);
            }
            else
            {
                Debug.LogError("[BattleManager] idleSpawnPoints 배열에 4개의 Transform을 인스펙터에서 할당해야 합니다!");
            }

            // 3. 방치형 샌드백 보스 스폰
            if (idleBossPrefab != null && bossSpawnPoint != null)
            {
                currentBoss = Instantiate(idleBossPrefab, bossSpawnPoint.position, bossSpawnPoint.rotation);
            }

            // 모든 캐릭터 전투 개시
            StartAllCombat();
        }

        /// <summary>
        /// 기존 보스 레이드 셋업 (태그 시스템 기반)
        /// </summary>
        private void SetupBossRaidMode()
        {
            Debug.Log("[BattleManager] 보스 레이드 모드 셋업 (기존 태그 시스템)");

            // 1. 태그 UI 활성화
            if (inGameHUD != null)
            {
                // inGameHUD.ToggleTagUI(true);
            }

            // 2. 레이드용 캐릭터 스폰 (기본적으로 첫 캐릭터 스폰 및 수동 조작)
            if (raidSpawnPoint != null)
            {
                SpawnCharacter(warriorPrefab, raidSpawnPoint.position, false);
                
                // TODO: 파트너(대기 캐릭터) 스폰 로직이 있다면 여기에 추가
            }

            // 3. 패턴을 가진 기존 레이드 보스 스폰
            if (raidBossPrefab != null && bossSpawnPoint != null)
            {
                currentBoss = Instantiate(raidBossPrefab, bossSpawnPoint.position, bossSpawnPoint.rotation);
            }

            StartAllCombat();
        }

        /// <summary>
        /// 캐릭터 프리팹을 스폰하고 상태를 초기화합니다.
        /// </summary>
        private void SpawnCharacter(GameObject prefab, Vector3 position, bool isIdleMode)
        {
            if (prefab == null) return;

            GameObject charObj = Instantiate(prefab, position, Quaternion.identity);
            TagCharacterController controller = charObj.GetComponent<TagCharacterController>();
            
            if (controller != null)
            {
                // 상태 분기의 핵심: isIdleMode가 true면 TagCharacterController 내부에서 수동 조작이 막히고 AI가 켜짐
                controller.EnableIdleAIMode(isIdleMode);
                ActiveCharacters.Add(controller);
            }
        }

        private void StartAllCombat()
        {
            foreach (var controller in ActiveCharacters)
            {
                controller.StartCombat();
            }
        }

        /// <summary>
        /// 전투 재시작이나 씬 전환 전 기존 캐릭터/보스 삭제
        /// </summary>
        public void ClearBattlefield()
        {
            foreach (var controller in ActiveCharacters)
            {
                if (controller != null && controller.gameObject != null)
                {
                    Destroy(controller.gameObject);
                }
            }
            ActiveCharacters.Clear();

            if (currentBoss != null)
            {
                Destroy(currentBoss);
            }
        }

        // ====================================================================================
        // [아래는 기존에 작성해두셨던 태그(Tag), 전멸, 부활 관련 로직들을 배치할 공간입니다]
        // ====================================================================================

        /// <summary>
        /// 태그 시스템: 현재 조작 캐릭터를 교체합니다.
        /// </summary>
        public void SwapCharacters(TagCharacterController outChar, TagCharacterController inChar)
        {
            if (IsIdleFarmingMode) return; // 방치형 모드에서는 태그 불가능

            if (outChar == null || inChar == null) return;
            if (inChar.characterBase != null && inChar.characterBase.IsDead) return; // 죽은 캐릭터로 교체 불가

            // 1. 기존 캐릭터: 수동 조작 해제 → 파트너 AI로 전환
            outChar.isPlayerControlled = false;
            outChar.EnableIdleAIMode(false);

            // 2. 신규 캐릭터: 수동 조작 권한 획득
            inChar.isPlayerControlled = true;
            inChar.EnableIdleAIMode(false);

            // 3. HUD 업데이트 (로컬 플레이어 변경)
            if (InGameHUDController.Instance != null && inChar.characterBase != null)
            {
                InGameHUDController.Instance.localPlayer = inChar.characterBase;
            }

            Debug.Log($"[BattleManager] 태그! {outChar.characterBase?.characterName} → {inChar.characterBase?.characterName}");
        }

        /// <summary>
        /// 파티 전멸 판정. 모두 사망하면 StageManager에 실패를 통보합니다.
        /// </summary>
        public void CheckGameOver()
        {
            if (IsIdleFarmingMode) return;

            bool isAllDead = ActiveCharacters.All(c => c.characterBase != null && c.characterBase.IsDead);
            if (isAllDead)
            {
                Debug.Log("<color=red>[BattleManager] 파티 전멸! 레이드 실패.</color>");
                
                // StageManager에 실패 통보 → 방치형 파밍으로 롤백
                if (StageManager.Instance != null)
                {
                    StageManager.Instance.OnBossFailed();
                }
            }
        }

        /// <summary>
        /// 죽은 파티원 전원을 부활시키고 전투를 재개합니다.
        /// </summary>
        public void ReviveParty()
        {
            Debug.Log("<color=cyan>[BattleManager] 파티 부활 처리</color>");
            foreach (var controller in ActiveCharacters)
            {
                if (controller.characterBase != null && controller.characterBase.IsDead)
                {
                    controller.characterBase.Revive(0.5f); // 50% 체력으로 부활
                }
            }
            
            // 부활 후 전투 재개
            StartAllCombat();
        }
    }
}
