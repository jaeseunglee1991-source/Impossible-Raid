using UnityEngine;
using System;
using System.Threading.Tasks;
using BossRaid.Combat;
using BossRaid.Utils; // CurrencyFormatter용
using BossRaid.UI;    // OfflineResultPopup용

namespace BossRaid.Managers
{
    public class OfflineRewardManager : MonoBehaviour
    {
        // 암호화하여 저장할 키 이름
        private const string LAST_PLAY_TIME_KEY = "Encrypted_LastPlayTime";

        [Header("Settings")]
        [Tooltip("1초당 획득하는 골드량")]
        public double goldPerSecond = 10.0;
        
        [Tooltip("오프라인 보상을 받기 위한 최소 오프라인 시간(초) - 예: 60초")]
        public double minimumOfflineSeconds = 60.0;
        
        [Tooltip("보상이 누적되는 최대 시간(초) - 예: 24시간 = 86400초")]
        public double maxOfflineSeconds = 86400.0;

        [Header("References")]
        public GrowthManager growthManager;
        public OfflineResultPopup resultPopup; // 기존에 만들어두신 결과 팝업창

        private async void Start()
        {
            // 매니저 연결 안 되어 있으면 찾기
            if (growthManager == null) growthManager = FindObjectOfType<GrowthManager>();
            if (resultPopup == null) resultPopup = FindObjectOfType<OfflineResultPopup>();

            // 게임 시작 시 오프라인 보상 계산 시도
            await CalculateOfflineReward();
        }

        private async Task CalculateOfflineReward()
        {
            Debug.Log("[OfflineReward] 오프라인 보상 계산 중...");

            // 1. Supabase에서 '진짜 서버 시간' 가져오기 (기기 시간 조작 방어)
            DateTime currentServerTime = await GetServerTimeAsync();

            // 2. 로컬에서 AES 암호화된 '마지막 접속 시간' 가져오기
            // (기록이 없으면 현재 시간 반환 -> 보상 없음)
            DateTime lastPlayTime = SecurePlayerPrefs.GetDateTime(LAST_PLAY_TIME_KEY, currentServerTime);

            // 3. 시간 차이 계산
            TimeSpan offlineDuration = currentServerTime - lastPlayTime;
            double offlineSeconds = offlineDuration.TotalSeconds;

            // 4. 최소 오프라인 시간 조건을 만족하는지 확인
            if (offlineSeconds > minimumOfflineSeconds) 
            {
                // 최대 누적 시간 상한선 적용
                double rewardSeconds = Math.Min(offlineSeconds, maxOfflineSeconds); 
                double earnedGold = rewardSeconds * goldPerSecond;

                Debug.Log($"[OfflineReward] {rewardSeconds:F0}초 동안 미접속. 획득 골드: {earnedGold}");
                
                // 골드 지급 (기존 SafeDouble 변수에 추가됨)
                if (growthManager != null)
                {
                    growthManager.AddGold(earnedGold);
                }

                // 💡 기존 프로젝트에 있던 UI 팝업 띄우기 로직 연동
                if (resultPopup != null)
                {
                    // 예: "12시간 30분 동안 자리를 비우셨습니다! 150,000 골드를 획득했습니다."
                    resultPopup.ShowPopup(rewardSeconds, earnedGold); 
                }
            }
            else
            {
                Debug.Log("[OfflineReward] 보상을 받기 위한 최소 오프라인 시간이 지나지 않았습니다.");
            }

            // 5. 보상 처리가 끝난 후, 현재 서버 시간을 다시 로컬에 암호화하여 덮어씌움
            SecurePlayerPrefs.SetDateTime(LAST_PLAY_TIME_KEY, currentServerTime);
        }

        // 앱이 일시정지(백그라운드)되거나 다시 켜질 때
        private async void OnApplicationPause(bool isPaused)
        {
            if (isPaused) 
            {
                await SaveCurrentTime();
            }
            else
            {
                // 백그라운드에서 다시 돌아왔을 때 보상 재계산 로직이 필요하다면 여기에 추가
            }
        }

        // 앱이 완전히 종료될 때
        private async void OnApplicationQuit()
        {
            await SaveCurrentTime();
        }

        // 서버 시간을 불러와 안전하게 저장하는 공통 함수
        private async Task SaveCurrentTime()
        {
            DateTime currentServerTime = await GetServerTimeAsync();
            SecurePlayerPrefs.SetDateTime(LAST_PLAY_TIME_KEY, currentServerTime);
            Debug.Log("[OfflineReward] 게임 종료/일시정지: 현재 서버 시간을 안전하게 저장했습니다.");
        }

        // Supabase RPC 호출 로직 (서버 시간 가져오기)
        private async Task<DateTime> GetServerTimeAsync()
        {
            try
            {
                // 🚨 SQL에서 "_v2"를 붙였다면 "get_server_time_v2"로 변경
                var response = await SupabaseManager.Instance.client.Rpc("get_server_time_v2", null);
                
                if (DateTime.TryParse(response.Content, out DateTime serverTime))
                {
                    return serverTime;
                }
            }
            catch (Exception e)
            {
                Debug.LogError("[OfflineReward] 서버 시간을 가져오는데 실패했습니다: " + e.Message);
            }

            // 인터넷 연결 끊김 등의 이슈로 통신에 실패할 경우, 예비용으로 로컬 UTC 시간 반환
            return DateTime.UtcNow; 
        }
    }
}
