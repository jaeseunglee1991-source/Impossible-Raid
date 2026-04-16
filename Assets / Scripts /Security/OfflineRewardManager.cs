using System;
using UnityEngine;
using System.Threading.Tasks;

public class OfflineRewardManager : MonoBehaviour
{
    private const string LAST_PLAY_TIME_KEY = "Encrypted_LastPlayTime";

    async void Start()
    {
        // 게임 시작 시 즉시 오프라인 보상 계산 시도
        await CalculateOfflineReward();
    }

    private async Task CalculateOfflineReward()
    {
        // 1. Supabase에서 '진짜 서버 시간'을 가져옵니다. (기기 시간 조작 무시)
        DateTime currentServerTime = await GetServerTimeAsync();

        // 2. 로컬에서 암호화된 '마지막 접속 시간'을 가져옵니다. (없으면 현재 시간으로 세팅)
        DateTime lastPlayTime = SecurePlayerPrefs.GetDateTime(LAST_PLAY_TIME_KEY, currentServerTime);

        // 3. 시간 차이 계산
        TimeSpan offlineDuration = currentServerTime - lastPlayTime;
        double offlineSeconds = offlineDuration.TotalSeconds;

        if (offlineSeconds > 60) // 1분 이상 오프라인이었을 때만 보상 지급
        {
            // 예: 1초당 10골드 지급, 최대 24시간(86400초)까지만 보상
            double rewardSeconds = Math.Min(offlineSeconds, 86400); 
            double earnedGold = rewardSeconds * 10.0;

            Debug.Log($"오프라인 시간: {rewardSeconds}초, 획득 골드: {earnedGold}");
            
            // 앞서 만든 GrowthManager의 SafeDouble을 이용해 안전하게 골드 추가
            // GetComponent<GrowthManager>().AddGold(earnedGold);
        }

        // 4. 보상 계산이 끝난 후, 현재 서버 시간을 다시 로컬에 암호화하여 저장
        SecurePlayerPrefs.SetDateTime(LAST_PLAY_TIME_KEY, currentServerTime);
    }

    // 게임 종료 또는 백그라운드 진입 시 마지막 시간 저장
    private async void OnApplicationPause(bool isPaused)
    {
        if (isPaused)
        {
            DateTime currentServerTime = await GetServerTimeAsync();
            SecurePlayerPrefs.SetDateTime(LAST_PLAY_TIME_KEY, currentServerTime);
        }
    }

    // Supabase RPC 호출 로직
    private async Task<DateTime> GetServerTimeAsync()
    {
        try
        {
            // Supabase 매니저 인스턴스를 통해 아까 만든 RPC 호출
            var response = await SupabaseManager.Instance.client.Rpc("get_server_time", null);
            
            if (DateTime.TryParse(response.Content, out DateTime serverTime))
            {
                return serverTime;
            }
        }
        catch (Exception e)
        {
            Debug.LogError("서버 시간을 가져오는데 실패했습니다: " + e.Message);
        }

        // 만약 인터넷이 끊겨서 서버 시간을 못 가져오면? 
        // -> 보상을 주지 않거나 로컬 시간을 임시로 쓰되 패널티를 주는 정책이 필요합니다.
        // 현재는 예비용으로 기기 시간을 반환합니다.
        return DateTime.UtcNow; 
    }
}
