using UnityEngine;
using System;
using System.Threading.Tasks;
using BossRaid.Combat;
using BossRaid.Utils; // CurrencyFormatter 사용을 위해 필요
using BossRaid.UI;    // OfflineResultPopup 사용을 위해 필요

namespace BossRaid.Managers
{
    /// <summary>
    /// ──────────────────────────────────────────────────────────────────
    /// OfflineRewardManager — 서버 시간을 연동한 오프라인 방치 보상 매니저
    /// ──────────────────────────────────────────────────────────────────
    /// </summary>
    public class OfflineRewardManager : MonoBehaviour
    {
        public static OfflineRewardManager Instance { get; private set; }

        [Header("보상 밸런스 설정")]
        [Tooltip("온라인 대비 오프라인 사냥 효율 (0.5 = 50%)")]
        public float offlineEfficiency = 0.5f;
        [Tooltip("오프라인 보상이 누적되는 최대 시간 (시간 단위)")]
        public int maxOfflineHours = 24;

        [Header("UI 연결")]
        [Tooltip("오프라인 보상 결과를 띄워줄 팝업 UI 스크립트를 연결하세요.")]
        public OfflineResultPopup rewardPopup;

        private const string LastPlayTimeKey = "LastPlayTime";

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

        private async void Start()
        {
            // 게임 시작 시 비동기로 오프라인 보상을 계산하고 지급합니다.
            await CalculateOfflineReward();
        }

        public async Task CalculateOfflineReward()
        {
            if (!PlayerPrefs.HasKey(LastPlayTimeKey)) return;

            // 1. Supabase 서버 시간 가져오기 (시간 조작 방지)
            DateTime now = await GetSupabaseServerTime();
            
            // 2. 저장된 마지막 접속 시간 불러오기
            string lastTimeStr = PlayerPrefs.GetString(LastPlayTimeKey);
            DateTime lastDateTime;
            if (!DateTime.TryParse(lastTimeStr, out lastDateTime)) return;
            
            TimeSpan timeSpan = now - lastDateTime;
            double elapsedSeconds = timeSpan.TotalSeconds;

            // 3. 제한 시간 적용 (최대 24시간, 최소 1분)
            double maxSeconds = maxOfflineHours * 3600;
            if (elapsedSeconds > maxSeconds) elapsedSeconds = maxSeconds;
            if (elapsedSeconds < 60) return; // 1분 미만은 보상 없음

            // 4. 보상 계산 (초당 수익 * 경과 시간 * 효율)
            double currentGPS = GetCurrentGoldPerSecond();
            double rewardAmount = elapsedSeconds * currentGPS * offlineEfficiency;

            if (rewardAmount > 0)
            {
                // 재화 매니저를 통해 실제 골드 지급
                if (GrowthManager.Instance != null)
                {
                    GrowthManager.Instance.AddGold(rewardAmount);
                }
                
                // 결과 팝업 UI 띄우기
                if (rewardPopup != null)
                {
                    rewardPopup.Show(timeSpan, rewardAmount);
                }
                else
                {
                    Debug.Log($"[오프라인 보상] {timeSpan.Hours}시간 {timeSpan.Minutes}분 경과 / 획득 재화: {rewardAmount.ToCurrencyString()} Gold");
                }
            }
        }

        /// <summary>
        /// Supabase RPC를 호출하여 서버의 현재 시간을 가져옵니다.
        /// 실패 시 기기 시간을 반환합니다.
        /// </summary>
        private async Task<DateTime> GetSupabaseServerTime()
        {
            try
            {
                // SQL Editor에서 생성한 get_server_time 함수를 호출합니다.
                // 주의: Supabase 초기화 방식에 따라 Supabase.Client.Instance 등을 사용하세요.
                var response = await Supabase.Client.Instance.Rpc("get_server_time", null);
                
                if (response != null && !string.IsNullOrEmpty(response.Content))
                {
                    // 따옴표 제거 후 파싱
                    string timeString = response.Content.Replace("\"", "");
                    return DateTime.Parse(timeString);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[OfflineRewardManager] 서버 시간 연동 실패. 기기 시간을 대체 사용합니다: {e.Message}");
            }
            
            // 오류 발생 시 기기 시간 반환 (안전망)
            return DateTime.Now;
        }

        /// <summary>
        /// 현재 파티 스펙 기준 초당 골드 획득량(GPS)을 계산합니다.
        /// </summary>
        private double GetCurrentGoldPerSecond()
        {
            float totalPartyDPS = 0;
            
            // 씬에 있는 모든 플레이어 캐릭터의 초당 데미지(DPS) 합산
            var characters = FindObjectsOfType<CharacterBase>();
            foreach (var c in characters)
            {
                if (c.baseAttackCooldown > 0)
                {
                    totalPartyDPS += c.baseAttackPower / c.baseAttackCooldown;
                }
            }
            
            // 보스 스펙 및 보상 (실제 밸런스에 맞춰 수정 가능)
            double goldPerFullBoss = 2000; // 보스 1마리당 얻는 총 골드 (1% 100번 + 처치 보너스 100번)
            float bossMaxHP = 1000f;       // 보스 체력

            if (bossMaxHP <= 0) return 0;

            // GPS 계산 공식: (파티 DPS / 보스 체력) * 보스 1마리당 총 골드
            double gps = (totalPartyDPS / bossMaxHP) * goldPerFullBoss;
            
            return gps;
        }

        // 앱이 백그라운드로 가거나 종료될 때 시간 저장
        private async void OnApplicationPause(bool pause)
        {
            if (pause)
            {
                await SaveCurrentTimeToServer();
            }
        }

        private async void OnApplicationQuit()
        {
            await SaveCurrentTimeToServer();
        }

        private async Task SaveCurrentTimeToServer()
        {
            DateTime now = await GetSupabaseServerTime();
            PlayerPrefs.SetString(LastPlayTimeKey, now.ToString());
            PlayerPrefs.Save();
            Debug.Log($"[OfflineRewardManager] 접속 종료 시간 저장 완료: {now}");
        }
    }
}
