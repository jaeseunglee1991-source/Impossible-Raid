using UnityEngine;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BossRaid.Managers
{
    public class GrowthManager : MonoBehaviour
    {
        public static GrowthManager Instance { get; private set; }

        [Header("재화 정보")]
        [Tooltip("유니티 화면에 보여주기 위한 표시용 골드 (서버 동기화 시 덮어씌워짐)")]
        public double displayGold = 0;

        public event Action<double> OnGoldChanged;

        // ─── 배치 저장 시스템 ───────────────────────────────────────────
        // 잡몹 골드는 로컬에서 모아뒀다가 주기적으로 한 번만 서버에 씀
        private double _pendingGoldToSync = 0;
        private float _lastSyncTime = 0;
        private const float SyncInterval = 60f; // 1분마다 한 번 서버 동기화

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Update()
        {
            // 주기적 배치 저장 (잡몹 골드 누적분을 서버에 전송)
            if (Time.time - _lastSyncTime >= SyncInterval && _pendingGoldToSync > 0)
            {
                _lastSyncTime = Time.time;
                _ = FlushPendingGoldToServer();
            }
        }

        private async Task FlushPendingGoldToServer()
        {
            if (DatabaseManager.Instance == null || DatabaseManager.Instance.SupabaseClient == null)
                return;

            double toSync = _pendingGoldToSync;
            _pendingGoldToSync = 0;

            try
            {
                var parameters = new Dictionary<string, object> { { "gold_amount", toSync } };
                await DatabaseManager.Instance.SupabaseClient.Rpc("add_gold", parameters);
                Debug.Log($"[GrowthManager] 배치 골드 동기화 완료: +{toSync} Gold");
            }
            catch (Exception e)
            {
                // 실패 시 롤백 (다음 사이클에 재시도)
                _pendingGoldToSync += toSync;
                Debug.LogWarning($"[GrowthManager] 배치 동기화 실패, 다음 주기에 재시도: {e.Message}");
            }
        }

        // ─── 경제 밸런스 공식 ───────────────────────────────────────────
        /// <summary>스테이지 레벨에 따른 잡몹 1마리당 골드 지급량 보정 공식 (1.15배수)</summary>
        public double CalculateMobGold(int stageLevel)
        {
            double baseGold = 10f;
            double multiplier = 1.15f;
            return Math.Floor(baseGold * Math.Pow(multiplier, stageLevel));
        }

        /// <summary>보스는 잡몹 50마리 분량의 골드를 한방에 지급</summary>
        public double CalculateBossGold(int stageLevel)
        {
            return CalculateMobGold(stageLevel) * 50.0;
        }

        // ─── Public API ─────────────────────────────────────────────────
        /// <summary>
        /// [잡몹 킬 보상] 서버 호출 없이 로컬에만 즉시 반영.
        /// 배치 저장 시스템이 주기적으로 서버에 씀.
        /// </summary>
        public void AddGold(double amount)
        {
            displayGold += amount;
            _pendingGoldToSync += amount; // 나중에 서버에 쓸 누적값에 추가
            OnGoldChanged?.Invoke(displayGold);
            SaveManager.Instance?.MarkDirty();
        }

        /// <summary>
        /// [SaveManager 전용] 세이브 파일에서 복원할 때 호출합니다.
        /// MarkDirty를 발생시키지 않아 불필요한 재저장을 방지합니다.
        /// </summary>
        public void SetGoldFromSave(double savedGold)
        {
            displayGold = savedGold;
            OnGoldChanged?.Invoke(displayGold);
        }

        /// <summary>
        /// [진짜 보상 확정] 보스가 죽었을 때 딱 1번 호출됩니다.
        /// 응답 실패 시 절대 스테이지를 넘기지 않음 (돈 복사 방지).
        /// </summary>
        public async Task<bool> ClaimBossRewardFromServer(int bossLevel)
        {
            // 오프라인/게스트 모드: 로컬 처리만 허용
            if (DatabaseManager.Instance == null || DatabaseManager.Instance.SupabaseClient == null)
            {
                Debug.Log("[GrowthManager] 오프라인 모드: 보스 보상을 로컬에서만 처리합니다.");
                double offlineReward = CalculateBossGold(bossLevel);
                displayGold += offlineReward;
                OnGoldChanged?.Invoke(displayGold);
                return true;
            }

            try
            {
                // 배치 대기 중인 잡몹 골드도 보스 처치 시 함께 묶어서 정산
                _pendingGoldToSync = 0;

                var parameters = new Dictionary<string, object> { { "boss_level", bossLevel } };
                var response = await DatabaseManager.Instance.SupabaseClient.Rpc("claim_boss_reward", parameters);

                if (response != null && !string.IsNullOrEmpty(response.Content))
                {
                    string clean = response.Content.Replace("\"", "");
                    double realGold = double.Parse(clean);
                    displayGold = realGold; // 서버 기준 값으로 강제 교정 (Anti-Cheat)
                    OnGoldChanged?.Invoke(displayGold);
                    Debug.Log($"[GrowthManager] 보스 보상 서버 정산 완료: {displayGold} Gold");
                    return true;
                }
                return false;
            }
            catch (Exception e)
            {
                Debug.LogError($"[GrowthManager] 보스 보상 정산 실패: {e.Message}");
                return false; // 실패 시 false 반환
            }
        }

        public bool SpendGold(double amount)
        {
            if (displayGold >= amount)
            {
                displayGold -= amount;
                OnGoldChanged?.Invoke(displayGold);
                // 지출 시에도 즉시 저장
                SaveManager.Instance?.MarkDirty();
                return true;
            }
            return false; 
        }
    }
}
