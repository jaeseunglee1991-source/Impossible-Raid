using UnityEngine;
using System;
using System.Threading.Tasks;
using BossRaid.Combat;
using BossRaid.Utils; // CurrencyFormatter용 (필요시)
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

        // 💡 통신 딜레이/강제 종료 방지용 오프셋 변수
        private TimeSpan timeOffset;

        private async void Start()
        {
            // 매니저 및 UI 컴포넌트 연결 안 되어 있으면 우선 싱글턴으로 찾기, 팝업은 인스펙터 할당 필수
            if (growthManager == null) growthManager = GrowthManager.Instance;
            
            if (resultPopup == null) 
            {
                Debug.LogWarning("[OfflineRewardManager] OfflineResultPopup이 인스펙터에 연결되지 않았습니다. 결과 팝업이 뜨지 않습니다.");
            }

            // 게임 시작 시 오프라인 보상 계산 시도
            await CalculateOfflineReward();
        }

        private async Task CalculateOfflineReward()
        {
            Debug.Log("[OfflineReward] 오프라인 보상 계산 중...");

            // 1. Supabase에서 '진짜 서버 시간' 가져오기 (기기 시간 조작 방어)
            DateTime currentServerTime = await GetServerTimeAsync();

            // 💡 핵심 최적화: 기기 시간과 서버 시간의 오프셋(격차)을 미리 계산해 둡니다.
            // 이를 통해 앱 종료 시 통신 없이도 정확한 서버 시간을 유추할 수 있습니다.
            timeOffset = currentServerTime - DateTime.UtcNow;

            // 2. 로컬에서 AES 암호화된 '마지막 접속 시간' 가져오기
            // (기록이 없으면 현재 시간 반환 -> 첫 접속이므로 보상 없음)
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
                
                // 골드 지급 (앞서 만든 SafeDouble 변수에 안전하게 추가됨)
                if (growthManager != null)
                {
                    growthManager.AddGold(earnedGold);
                }

                // 기존 프로젝트에 있던 UI 팝업 띄우기 로직 연동
                if (resultPopup != null)
                {
                    resultPopup.Show(System.TimeSpan.FromSeconds(rewardSeconds), earnedGold); 
                }
            }
            else
            {
                Debug.Log("[OfflineReward] 보상을 받기 위한 최소 오프라인 시간이 지나지 않았습니다.");
            }

            // 5. 보상 처리가 끝난 후, 계산된 현재 시간을 다시 로컬에 암호화하여 덮어씌움
            SaveEstimatedServerTime();
        }

        // 스마트폰에서 홈 버튼을 눌러 앱이 백그라운드로 내려가거나 다시 켜질 때
        private void OnApplicationPause(bool isPaused)
        {
            if (isPaused) 
            {
                // 앱이 내려갈 때 비동기 통신 없이 즉시 저장! (저장 증발 버그 완벽 차단)
                SaveEstimatedServerTime();
            }
        }

        // PC에서 창을 닫거나 안드로이드에서 앱을 강제 종료(Kill) 할 때
        private void OnApplicationQuit()
        {
            // 통신 없이 즉시 저장!
            SaveEstimatedServerTime();
        }

        // 💡 오프셋을 활용하여 서버 통신 없이 예상 서버 시간을 안전하고 빠르게 저장하는 함수
        private void SaveEstimatedServerTime()
        {
            DateTime estimatedServerTime = DateTime.UtcNow + timeOffset;
            SecurePlayerPrefs.SetDateTime(LAST_PLAY_TIME_KEY, estimatedServerTime);
            Debug.Log("[OfflineReward] 게임 종료/일시정지: 예상 서버 시간을 안전하게 저장했습니다.");
        }

        // Supabase RPC 호출 로직 (서버 시간 가져오기)
        private async Task<DateTime> GetServerTimeAsync()
        {
            try
            {
                // 🚨 SQL에서 기존 함수를 DROP하고 새로 만들었다면 "get_server_time" 그대로 사용
                // "_v2"를 붙여서 생성하셨다면 "get_server_time_v2"로 변경하세요.
                var response = await DatabaseManager.Instance.SupabaseClient.Rpc("get_server_time", null);
                
                // 🪲 버그 수정: Supabase JSON 반환값의 쌍따옴표(") 제거
                string cleanTimeStr = response.Content.Trim('"'); 
                
                if (DateTime.TryParse(cleanTimeStr, out DateTime serverTime))
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
