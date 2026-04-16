using UnityEngine;
using System;
using BossRaid.Managers;
using BossRaid.Utils; // 이전에 드린 CurrencyFormatter 사용을 위해 필요

namespace BossRaid.Managers
{
    public class OfflineRewardManager : MonoBehaviour
    {
        public static OfflineRewardManager Instance { get; private set; }

        [Header("설정")]
        public float offlineEfficiency = 0.5f; // 온라인 대비 50% 수집
        public int maxOfflineHours = 24;      // 최대 24시간 저장

        private const string LastPlayTimeKey = "LastPlayTime";

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            // 게임 시작 시 오프라인 보상 체크
            CalculateOfflineReward();
        }

        /// <summary>
        /// 오프라인 보상을 계산하고 지급합니다.
        /// </summary>
        public void CalculateOfflineReward()
        {
            if (!PlayerPrefs.HasKey(LastPlayTimeKey)) return;

            // 1. 시간 차이 계산
            string lastTimeStr = PlayerPrefs.GetString(LastPlayTimeKey);
            DateTime lastDateTime = DateTime.Parse(lastTimeStr);
            TimeSpan timeSpan = DateTime.Now - lastDateTime;

            double elapsedSeconds = timeSpan.TotalSeconds;

            // 2. 최대 24시간(86,400초) 제한 적용
            double maxSeconds = maxOfflineHours * 3600;
            if (elapsedSeconds > maxSeconds)
            {
                elapsedSeconds = maxSeconds;
            }

            if (elapsedSeconds < 10) return; // 10초 미만은 계산 제외

            // 3. 초당 수익(GPS) 가져오기
            double currentGPS = GetCurrentGoldPerSecond();

            // 4. 보상 계산: 경과시간(초) * GPS * 효율(0.5)
            double rewardAmount = elapsedSeconds * currentGPS * offlineEfficiency;

            if (rewardAmount > 0)
            {
                GrowthManager.Instance.AddGold(rewardAmount);
                
                // TODO: UI 팝업을 띄워 유저에게 알림을 줄 수 있습니다.
                Debug.Log($"[오프라인 보상] {timeSpan.Hours}시간 {timeSpan.Minutes}분 만에 복귀! " +
                          $"{rewardAmount.ToCurrencyString()} Gold를 획득했습니다. (효율 50% 적용)");
            }
        }

        /// <summary>
        /// 현재 파티의 스탯을 기반으로 이론적인 초당 수익(GPS)을 계산합니다.
        /// </summary>
        private double GetCurrentGoldPerSecond()
        {
            // 방치형 보스의 정보를 참조 (프리팹이나 씬의 보스 정보)
            // 1% 데미지당 골드 / (보스 체력 / 파티 전체 초당 데미지)
            
            // 예시 로직: (실제 프로젝트의 보스 MaxHP와 캐릭터들의 DPS를 연동해야 합니다)
            // 여기서는 간단하게 계산을 위해 임시 값을 사용하거나, BattleManager에서 계산된 값을 가져옵니다.
            
            float totalPartyDPS = 0;
            var characters = FindObjectsOfType<CharacterBase>();
            foreach (var c in characters)
            {
                // 초당 데미지 = 공격력 / 공격 쿨타임
                totalPartyDPS += c.baseAttackPower / c.baseAttackCooldown;
            }

            // 보스가 한 마리 잡힐 때 주는 총 골드 (1%당 골드 * 200틱)
            // 이전 코드 기준: 1%당 골드 100번 + 처치 보너스 100번 = 총 200번분
            double goldPerFullBoss = 2000; // 예시값 (goldPerOnePercent * 200)
            float bossMaxHP = 1000;         // 예시값

            // GPS = (파티 초당 데미지 / 보스 체력) * 보스 1마리당 총 골드
            double gps = (totalPartyDPS / bossMaxHP) * goldPerFullBoss;
            
            return gps;
        }

        // 게임 종료 및 백그라운드 전환 시 시간 저장
        private void OnApplicationQuit()
        {
            SaveCurrentTime();
        }

        private void OnApplicationPause(bool pause)
        {
            if (pause) SaveCurrentTime();
            else CalculateOfflineReward(); // 복귀 시 다시 계산
        }

        private void SaveCurrentTime()
        {
            PlayerPrefs.SetString(LastPlayTimeKey, DateTime.Now.ToString());
            PlayerPrefs.Save();
        }
    }
}
