using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BossRaid.Models;
using BossRaid.Managers;
using System;

namespace BossRaid.Managers
{
    public class ResultManager : MonoBehaviour
    {
        public static ResultManager Instance { get; private set; }
        
        // 결과 저장용 (UI 연동)
        public bool lastIsWin;
        public float lastClearTime;
        public List<CombatRecord> lastStats;
        public int lastStageCleared;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this.gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(this.gameObject);
        }

        public async Task ProcessGameResult(bool isWin, float clearTime, List<CombatRecord> playerStats, int stageCleared = 0)
        {
            lastIsWin = isWin;
            lastClearTime = clearTime;
            lastStats = playerStats;
            lastStageCleared = stageCleared;

            DetermineMVP(playerStats);
            
            Debug.Log($"[Result] Game Over. Win: {isWin}, Time: {clearTime}s");
            
            // 전적 정보 유무에 따른 보스 처치 기록 등 업데이트
            await UpdateLocalUserProfile(isWin, stageCleared);

            // 결과 씬으로 전환 전 연출 대기 (2초)
            await Task.Delay(2000);
            UnityEngine.SceneManagement.SceneManager.LoadScene("ResultScene");
        }

        private void DetermineMVP(List<CombatRecord> stats)
        {
            if (stats == null || stats.Count == 0) return;
            
            // MVP 기준 (실제 데이터에 영향을 미치지 않아야 함)
            var scoredStats = stats.Select(s => new {
                Record = s,
                Score = IsTank(s.role) ? (s.totalDamageTaken * 0.5f + s.aggroDuration * 10f) :
                        IsHealer(s.role) ? (s.totalHealing * 1.5f) : 
                        s.totalDamage // DPS
            }).OrderByDescending(x => x.Score).ToList();

            foreach (var s in stats) s.isMvp = false;
            scoredStats.First().Record.isMvp = true;
        }

        private bool IsTank(string job) => job == "Warrior" || job == "Paladin" || job == "DeathKnight";
        private bool IsHealer(string job) => job == "Priest" || job == "Druid";

        private async Task UpdateLocalUserProfile(bool isWin, int stageCleared)
        {
            try
            {
                if (DatabaseManager.Instance == null || DatabaseManager.Instance.Client == null || DatabaseManager.Instance.Client.Auth.CurrentUser == null)
                {
                    Debug.LogWarning("[ResultManager] DB Sync Skipped: No user is currently logged in.");
                    return;
                }

                // 실제 Supabase 업데이트 로직 (간소화)
                var userId = DatabaseManager.Instance.Client.Auth.CurrentUser.Id;
                Debug.Log($"[ResultManager] DB Update: isWin={isWin}, stage={stageCleared}");
                
                // 실제 연동 로직은 프로젝트 스펙에 맞춰 구현 (Skip here for clarity)
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ResultManager] DB Sync Failed: {ex.Message}");
            }
        }

        [ContextMenu("Test Game Result (Win)")]
        public async void TestResultWin()
        {
            await ProcessGameResult(true, 120f, new List<CombatRecord>
            {
                new CombatRecord("test", "Tester", "Warrior") { totalDamage = 10000, isMvp = true }
            }, 1);
        }
    }
}
