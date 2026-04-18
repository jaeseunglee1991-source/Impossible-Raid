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

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        /// <summary>
        /// [가짜 보상] 보스가 1% 까일 때마다 호출되어 타격감(UI)만 올려줍니다.
        /// 서버 통신을 하지 않으므로 랙이 전혀 없습니다.
        /// </summary>
        public void AddFakeGold(double amount)
        {
            displayGold += amount;
            OnGoldChanged?.Invoke(displayGold);
        }

        /// <summary>
        /// [오프라인 보상 / 방치형 파밍 킬 보상] 골드를 더하고 저장을 예약합니다.
        /// OfflineRewardManager.CalculateOfflineReward() 및
        /// StageManager.OnMobKilled()에서 호출합니다.
        /// </summary>
        public void AddGold(double amount)
        {
            displayGold += amount;
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
        /// </summary>
        public async Task ClaimBossRewardFromServer(int bossLevel)
        {
            try
            {
                var parameters = new Dictionary<string, object>
                {
                    { "boss_level", bossLevel }
                };

                // 1. 서버에 보스 처치 사실을 알림
                var response = await DatabaseManager.Instance.SupabaseClient.Rpc("claim_boss_reward", parameters);

                // 2. 서버가 계산을 마치고 돌려준 '진짜 골드량'을 받아옴
                if (response != null && !string.IsNullOrEmpty(response.Content))
                {
                    string cleanResponse = response.Content.Replace("\"", "");
                    double realGold = double.Parse(cleanResponse);

                    // 3. 치트키로 조작된 가짜 골드를 서버의 진짜 골드로 덮어씌워 강제 교정 (Anti-Cheat)
                    displayGold = realGold;
                    OnGoldChanged?.Invoke(displayGold);

                    Debug.Log($"[서버 동기화 완료] 보스 처치 보상 수령. 현재 진짜 재화: {displayGold} Gold");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[보안/네트워크 알림] 서버와 동기화 실패: {e.Message}");
                // 실패 시 보상을 다시 요청하거나, 게임을 잠시 멈추는 등의 처리를 할 수 있습니다.
            }
        }

        public bool SpendGold(double amount)
        {
            if (displayGold >= amount)
            {
                // TODO: 실제 강화(소비) 시에도 서버 RPC를 호출하여 차감하는 것이 안전합니다.
                displayGold -= amount;
                OnGoldChanged?.Invoke(displayGold);
                return true;
            }
            return false; 
        }
    }
}
