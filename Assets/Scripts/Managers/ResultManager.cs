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

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this.gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(this.gameObject);
        }

        public async Task ProcessGameResult(bool isWin, float clearTime, List<CombatRecord> playerStats, int stageCleared = 0)
        {
            await Task.Yield();
            DetermineMVP(playerStats);
            Debug.Log($"[Result] Game Over. Win: {isWin}, Time: {clearTime}s");
            foreach (var stat in playerStats)
            {
                Debug.Log($"Player: {stat.nickname}, Damage: {stat.totalDamage}, MVP: {stat.isMvp}");
            }

            if (isWin)
            {
                await UpdateLocalUserProfile(stageCleared);
            }
        }

        private void DetermineMVP(List<CombatRecord> stats)
        {
            if (stats == null || stats.Count == 0) return;
            var mvp = stats.OrderByDescending(s => s.totalDamage).First();
            mvp.isMvp = true;
        }

        private async Task UpdateLocalUserProfile(int stageCleared)
        {
            await Task.Yield();
            try
            {
                if (DatabaseManager.Instance == null || DatabaseManager.Instance.Client == null || DatabaseManager.Instance.Client.Auth.CurrentUser == null)
                {
                    Debug.LogWarning("[ResultManager] DB Sync Skipped: No user is currently logged in (Test Mode?).");
                    return;
                }

                var userId = DatabaseManager.Instance.Client.Auth.CurrentUser.Id;
                var updateData = new Dictionary<string, object>
                {
                    { "total_plays", 1 },
                    { "boss_kills", 1 },
                    { "max_stage_reached", stageCleared }
                };
                Debug.Log("[ResultManager] Syncing clear record to Supabase...");
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
