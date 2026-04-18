using UnityEngine;
using System.Collections;
using BossRaid.Combat;

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

            // 방치형 잡몹 스포너 활성화
            if (IdleMobSpawner != null) IdleMobSpawner.SetActive(true);

            // TODO: 플레이어 파티의 수동 조작 UI (Q,W) 비활성화 & 전원 AI 루프 위임
            // BattleManager의 태그 기능도 잠금 처리
            
            Debug.Log($"<color=green>[StageManager] 제 {CurrentStageLevel} 스테이지 - 방치형 파밍 시작 (필요 잡몹: {MobsRequiredForBoss})</color>");
        }

        // ──────────────────────────────────────────────
        // 2. 잡몹 처리 카운팅 (MobBase.Die() 등에서 호출)
        // ──────────────────────────────────────────────
        public void OnMobKilled(int goldReward)
        {
            if (CurrentState != GameState.IdleFarming) return;

            MobsKilledInStage++;
            GrowthManager.Instance.AddGold(goldReward);

            // 장비 드랍 시도 (DropTable.MOB_DROP_CHANCE 확률)
            InventoryManager.Instance?.TryDrop(CurrentStageLevel);

            if (MobsKilledInStage >= MobsRequiredForBoss)
            {
                ShowBossChallengeUI();
            }
        }

        private void ShowBossChallengeUI()
        {
            // TODO: 이 시점에 HUD에서 우측 상단 "BOSS 도전" 버튼을 흔들거리게 만듭니다.
            Debug.Log("[StageManager] 보스 도전 조건 달성! HUD '도전' 버튼 활성화");
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

            // TODO: 화면 암전, 경고 사이렌 등 연출
            Debug.Log("<color=red>[StageManager] 보스 레이드 전환 중... 화면 암전!</color>");

            // 방치형 스포너 끄기 및 남은 잡몹 파괴 처리
            if (IdleMobSpawner != null) IdleMobSpawner.SetActive(false);

            yield return new WaitForSeconds(1.5f); // 1.5초 로딩/연출

            CurrentState = GameState.BossChallenge;

            // 레이드 시스템 활성화
            if (BossRaidSystem != null) BossRaidSystem.SetActive(true);

            // 태그 시스템 재개
            // TODO: 태그용 UI 보이기, 첫 번째 캐릭터 수동 조작 권한 획득 등
            if (BattleManager.Instance != null)
            {
                // 여기서 배틀매니저 초기화 및 캐릭터 타겟팅 재설정
                // 보스 오브젝트 실체화 등
            }

            Debug.Log($"<color=magenta>[StageManager] ⚔ {CurrentStageLevel} 스테이지 레이드 보스 전 진입! ⚔</color>");
        }

        // ──────────────────────────────────────────────
        // 4. 보스 클리어 및 롤백 (실패 시)
        // ──────────────────────────────────────────────
        public void OnBossDefeated()
        {
            Debug.Log($"<color=cyan>[StageManager] 보스 처치 완료! 다음 스테이지로 넘어갑니다.</color>");

            // 보스 확정 드랍 (최소 Rare 보장)
            InventoryManager.Instance?.DropFromBoss(CurrentStageLevel);

            CurrentStageLevel++;
            MobsKilledInStage = 0;
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
