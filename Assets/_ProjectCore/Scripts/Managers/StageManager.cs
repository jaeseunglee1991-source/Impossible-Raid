using UnityEngine;
using System.Collections;
using BossRaid.Combat;
using BossRaid.UI;

namespace BossRaid.Managers
{
    public enum GameState
    {
        IdleFarming,        // 방치형 사냥 (잡몹 웨이브)
        BossChallenge,      // 보스 레이드 태그 모드
        Transitioning       // 상태 전환 중 (암전 등 연출)
    }

    /// <summary>
    /// 방치형 필드 스폰과 보스(Belthazar 등) 콘텐츠의 분리 및 전환을 담당하는 스테이지 매니저 뼈대
    /// </summary>
    public class StageManager : MonoBehaviour
    {
        public static StageManager Instance { get; private set; }

        public GameState CurrentState { get; private set; } = GameState.IdleFarming;
        
        [Header("Idle Farm Settings")]
        public int CurrentStageLevel = 1;
        public int MobsKilledInStage = 0;
        public int MobsRequiredForBoss = 50; // 잡몹 50마리 잡으면 보스 도전 가능 버저 활성화

        [Header("References")]
        [Tooltip("방치형 사냥용 일반 몹 스포너 (추후 구현)")]
        public GameObject IdleMobSpawner;

        [Tooltip("수동 보스 레이드 시스템 (BattleManager, BossAI 등 활성화)")]
        public GameObject BossRaidSystem;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            // 게임 시작: 무조건 방치형 사냥 모드로 진입
            EnterIdleFarmingState();
        }

        // ──────────────────────────────────────────────
        // 1. 방치형 파밍 모드 진입
        // ──────────────────────────────────────────────
        public void EnterIdleFarmingState()
        {
            CurrentState = GameState.IdleFarming;

            // 보스 레이드 시스템 비활성화
            if (BossRaidSystem != null) BossRaidSystem.SetActive(false);

            // 방치형 잡몹 스포너 활성화 및 자동 컴포넌트 추가
            if (IdleMobSpawner != null)
            {
                IdleMobSpawner.SetActive(true);
                if (IdleMobSpawner.GetComponent<IdleEnemySpawner>() == null)
                {
                    var spawner = IdleMobSpawner.AddComponent<IdleEnemySpawner>();
                    // 기본 소환 포인트가 없다면 스포너 자신의 위치를 사용
                    spawner.spawnPoints = new[] { IdleMobSpawner.transform };
                    
                    // 프리팹은 BattleManager에서 쓰던 것들을 빌려오거나 수동 할당 필요
                    if (BattleManager.Instance != null)
                    {
                        spawner.enemyPrefabs = new[] { BattleManager.Instance.warriorPrefab }; // 임시로 우리 캐릭터 프리팹을 적군으로 소환해서 테스트
                    }
                }
            }
            
            UpdateStageUI();
            Debug.Log($"<color=green>[StageManager] 제 {CurrentStageLevel} 스테이지 - 방치형 파밍 시작 (필요 잡몹: {MobsRequiredForBoss})</color>");
        }

        /// <summary>
        /// 상단 HUD의 스테이지 및 잡몹 진행도를 갱신합니다.
        /// </summary>
        public void UpdateStageUI()
        {
            if (InGameHUDController.Instance != null && InGameHUDController.Instance.timerText != null)
            {
                // [Premium] 타이머 대신 스테이지 정보를 출력 (방치형 모드)
                string status = $"스테이지 {CurrentStageLevel} - 잡몹 <color=yellow>{MobsKilledInStage}</color>/{MobsRequiredForBoss}";
                
                // 50마리 다 채웠을 때 강조
                if (MobsKilledInStage >= MobsRequiredForBoss)
                    status = $"스테이지 {CurrentStageLevel} - <color=#FFD700>보스 도전 가능!</color>";

                InGameHUDController.Instance.timerText.text = status;
            }
        }

        // ──────────────────────────────────────────────
        // 2. 잡몹 처리 카운팅 (MobBase.Die() 등에서 호출)
        // ──────────────────────────────────────────────
        public void OnMobKilled(int goldReward)
        {
            if (CurrentState != GameState.IdleFarming) return;

            MobsKilledInStage++;
            UpdateStageUI(); // UI 즉시 갱신 (이 위의 코드는 절대 실패하지 않음)

            // 골드는 IdleBoss.Die()에서 이미 AddGold()로 처리됨 → 이중 지급 방지

            // 장비 드랍 시도 (1% 확률) - 안전하게 감쌈
            try
            {
                InventoryManager.Instance?.TryDrop(CurrentStageLevel);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[StageManager] 드랍 처리 중 오류 (무시): {e.Message}");
            }

            if (MobsKilledInStage >= MobsRequiredForBoss)
            {
                // [버섯커 키우기식] 잡몹을 다 잡으면 보스방에 들어갈지 묻는 UI "도전" 버튼 활성화
                ShowBossChallengeUI();
            }
        }

        private void ShowBossChallengeUI()
        {
            // HUD의 보스 도전 버튼 활성화
            if (InGameHUDController.Instance != null)
            {
                // 보스 도전 버튼이 있다면 활성화 (버튼 참조는 HUD에서 관리)
                var bossBtn = InGameHUDController.Instance.giveUpButton; // 임시로 기존 버튼 재활용, 추후 전용 버튼으로 교체
                if (bossBtn != null)
                {
                    bossBtn.gameObject.SetActive(true);
                    bossBtn.onClick.RemoveAllListeners();
                    bossBtn.onClick.AddListener(RequestBossChallenge);
                }
            }

            Debug.Log("<color=yellow>[StageManager] 보스 도전 조건 달성! '보스 도전' 버튼 활성화</color>");
        }

        // ──────────────────────────────────────────────
        // 3. 보스 도전 모드 돌입 (UI 버튼 클릭)
        // ──────────────────────────────────────────────
        public void RequestBossChallenge()
        {
            if (CurrentState != GameState.IdleFarming) return;
            if (MobsKilledInStage < MobsRequiredForBoss) return;

            StartCoroutine(TransitionToBossRaid());
        }

        private IEnumerator TransitionToBossRaid()
        {
            CurrentState = GameState.Transitioning;

            Debug.Log("<color=red>[StageManager] 보스 레이드 전환 중... 화면 암전!</color>");

            // 방치형 스포너 끄기 및 남은 잡몹 파괴 처리
            if (IdleMobSpawner != null) IdleMobSpawner.SetActive(false);

            yield return new WaitForSeconds(1.5f); // 1.5초 로딩/연출

            CurrentState = GameState.BossChallenge;

            // 레이드 시스템 활성화
            if (BossRaidSystem != null) BossRaidSystem.SetActive(true);

            // BattleManager에서 전투 재초기화 (보스 레이드 모드로 전환)
            if (BattleManager.Instance != null)
            {
                BattleManager.Instance.InitializeBattle();
            }

            Debug.Log($"<color=magenta>[StageManager] ⚔ {CurrentStageLevel} 스테이지 레이드 보스 전 진입! ⚔</color>");
        }

        // ──────────────────────────────────────────────
        // 4. 보스 클리어 및 롤백 (실패 시)
        // ──────────────────────────────────────────────
        /// <summary>보스 클리어 성공 시 호출</summary>
        public async void OnBossDefeated()
        {
            Debug.Log($"<color=cyan>[StageManager] 보스 처치! 서버 동기화를 대기합니다...</color>");

            // 서버 응답이 실패하면 돈 복사 방지를 위해 스테이지를 넘기지 않음
            bool isSuccess = true;
            if (GrowthManager.Instance != null)
            {
                isSuccess = await GrowthManager.Instance.ClaimBossRewardFromServer(CurrentStageLevel);
            }

            if (!isSuccess)
            {
                Debug.LogError("<color=red>[StageManager] 보스 보상 획득 실패 (네트워크 오류). 다음 스테이지 진입이 취소됩니다.</color>");
                OnBossFailed();
                return;
            }

            Debug.Log($"<color=cyan>[StageManager] 서버 동기화 완료! 다음 스테이지로 넘어갑니다.</color>");

            // 보스 확정 드랍 (최소 Rare 보장)
            InventoryManager.Instance?.DropFromBoss(CurrentStageLevel);

            CurrentStageLevel++;
            MobsKilledInStage = 0;
            UpdateStageUI();

            // 보상 및 다음 스테이지 파밍 전환
            StartCoroutine(ReturnToFarmState(true));
        }

        /// <summary>플레이어 파티 전멸 시 (BattleManager 확인/부활 거절 시) 호출</summary>
        public void OnBossFailed()
        {
            Debug.Log($"<color=gray>[StageManager] 보스전 패배... 현재 위치에서 잡몹 무한 파밍으로 롤백합니다.</color>");
            
            // 게이지/카운트를 깎거나, 그냥 놔둔 채 몹만 다시 띄우는 등 게임 기획에 맞춤
            MobsKilledInStage = 0; 
            
            StartCoroutine(ReturnToFarmState(false));
        }

        private IEnumerator ReturnToFarmState(bool isWin)
        {
            CurrentState = GameState.Transitioning;
            yield return new WaitForSeconds(2.0f); // 결과창/연출 시간

            // 다시 파밍 상태로 진입
            EnterIdleFarmingState();
        }
    }
}
