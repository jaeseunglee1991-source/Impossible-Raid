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
    /// 방치형 필드 스폰과 보스(Belthazar 등) 콘텐츠의 분리 및 전환을 담당하는 스테이지 매니저
    /// </summary>
    public class StageManager : MonoBehaviour
    {
        public static StageManager Instance { get; private set; }

        public GameState CurrentState { get; private set; } = GameState.IdleFarming;
        
        [Header("Idle Farm Settings")]
        public int CurrentStageLevel = 1;
        public int MobsKilledInStage = 0;
        public int MobsRequiredForBoss = 50;

        [Header("References")]
        public GameObject IdleMobSpawner;
        public GameObject BossRaidSystem;

        // [신규] 서버 동기화 중복 호출 방지 플래그
        private bool _isProcessingReward = false; 

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            EnterIdleFarmingState();
        }

        // ──────────────────────────────────────────────
        // 1. 방치형 파밍 모드 진입
        // ──────────────────────────────────────────────
        public void EnterIdleFarmingState()
        {
            CurrentState = GameState.IdleFarming;

            if (BossRaidSystem != null) BossRaidSystem.SetActive(false);

            if (IdleMobSpawner != null)
            {
                IdleMobSpawner.SetActive(true);
                if (IdleMobSpawner.GetComponent<IdleEnemySpawner>() == null)
                {
                    var spawner = IdleMobSpawner.AddComponent<IdleEnemySpawner>();
                    spawner.spawnPoints = new[] { IdleMobSpawner.transform };
                    
                    if (BattleManager.Instance != null)
                    {
                        spawner.enemyPrefabs = new[] { BattleManager.Instance.warriorPrefab };
                    }
                }
            }
            
            UpdateStageUI();
            Debug.Log($"<color=green>[StageManager] 제 {CurrentStageLevel} 스테이지 - 방치형 파밍 시작</color>");
        }

        public void UpdateStageUI()
        {
            if (InGameHUDController.Instance != null && InGameHUDController.Instance.timerText != null)
            {
                string status = $"스테이지 {CurrentStageLevel} - 잡몹 <color=yellow>{MobsKilledInStage}</color>/{MobsRequiredForBoss}";
                
                if (MobsKilledInStage >= MobsRequiredForBoss)
                    status = $"스테이지 {CurrentStageLevel} - <color=#FFD700>보스 도전 가능!</color>";

                InGameHUDController.Instance.timerText.text = status;
            }
        }

        // ──────────────────────────────────────────────
        // 2. 잡몹 처리 카운팅
        // ──────────────────────────────────────────────
        public void OnMobKilled(int goldReward)
        {
            if (CurrentState != GameState.IdleFarming) return;

            MobsKilledInStage++;
            UpdateStageUI(); 

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
                ShowBossChallengeUI();
            }
        }

        private void ShowBossChallengeUI()
        {
            if (InGameHUDController.Instance != null)
            {
                var bossBtn = InGameHUDController.Instance.giveUpButton; 
                if (bossBtn != null)
                {
                    bossBtn.gameObject.SetActive(true);
                    bossBtn.onClick.RemoveAllListeners();
                    bossBtn.onClick.AddListener(RequestBossChallenge);
                }
            }
            Debug.Log("<color=yellow>[StageManager] 보스 도전 조건 달성!</color>");
        }

        // ──────────────────────────────────────────────
        // 3. 보스 도전 모드 돌입
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
            if (IdleMobSpawner != null) IdleMobSpawner.SetActive(false);

            yield return new WaitForSeconds(1.5f); 

            CurrentState = GameState.BossChallenge;
            if (BossRaidSystem != null) BossRaidSystem.SetActive(true);

            if (BattleManager.Instance != null)
            {
                BattleManager.Instance.InitializeBattle();
            }
        }

        // ──────────────────────────────────────────────
        // 4. 보스 클리어 및 롤백 (실패 시) - 성공 팝업 제거 버전
        // ──────────────────────────────────────────────
        public async void OnBossDefeated()
        {
            // 1. 중복 실행 차단 (다단히트로 여러번 호출되는 것 방지)
            if (_isProcessingReward)
            {
                return;
            }
            _isProcessingReward = true;

            Debug.Log($"<color=cyan>[StageManager] 보스 처치! 서버 동기화를 대기합니다...</color>");

            // 2. 화면 터치 차단 패널 ON
            if (InGameHUDController.Instance != null)
                InGameHUDController.Instance.ToggleLoadingPanel(true);

            bool isSuccess = false;
            
            // 3. 서버 호출 대기
            if (GrowthManager.Instance != null)
            {
                isSuccess = await GrowthManager.Instance.ClaimBossRewardFromServer(CurrentStageLevel);
            }

            // 4. 화면 터치 차단 패널 OFF
            if (InGameHUDController.Instance != null)
                InGameHUDController.Instance.ToggleLoadingPanel(false);

            // 5. 서버 통신 실패 시 롤백 처리
            if (!isSuccess)
            {
                Debug.LogError("<color=red>[StageManager] 서버 응답 실패. 보상을 받지 못했습니다.</color>");
                
                if (InGameHUDController.Instance != null)
                    InGameHUDController.Instance.ShowSystemMessage("<color=red>서버 통신 실패. 진행도가 롤백됩니다.</color>");

                OnBossFailed(); // 실패 처리
                _isProcessingReward = false;
                return; // 다음 스테이지 진입 차단!
            }

            // 6. 성공 시 다음 스테이지 이동
            Debug.Log($"<color=cyan>[StageManager] 동기화 완료! 다음 스테이지 이동.</color>");

            // 💡 [수정됨] 유저 흐름을 방해하지 않기 위해 성공 시 중앙 팝업 메시지를 띄우지 않습니다.
            // if (InGameHUDController.Instance != null)
            //     InGameHUDController.Instance.ShowSystemMessage("<color=#00FF00>보상 획득 완료! 다음 스테이지로 이동합니다.</color>");

            InventoryManager.Instance?.DropFromBoss(CurrentStageLevel);

            CurrentStageLevel++;
            MobsKilledInStage = 0;
            UpdateStageUI();

            _isProcessingReward = false;
            StartCoroutine(ReturnToFarmState(true));
        }

        public void OnBossFailed()
        {
            Debug.Log($"<color=gray>[StageManager] 패배/오류로 파밍 모드 롤백.</color>");
            MobsKilledInStage = 0; 
            UpdateStageUI();
            StartCoroutine(ReturnToFarmState(false));
        }

        private IEnumerator ReturnToFarmState(bool isWin)
        {
            CurrentState = GameState.Transitioning;
            yield return new WaitForSeconds(2.0f); 
            EnterIdleFarmingState();
        }
    }
}
