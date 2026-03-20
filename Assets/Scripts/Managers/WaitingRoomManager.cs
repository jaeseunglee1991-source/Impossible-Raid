using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using BossRaid.Models;
using BossRaid.Managers;
using Supabase.Realtime;
using Newtonsoft.Json;
using System;
using System.Linq;
using Supabase.Postgrest; // 변경된 네임스페이스
using Supabase.Realtime.PostgresChanges;

namespace BossRaid.Managers
{
    public class WaitingRoomManager : MonoBehaviour
    {
        public static WaitingRoomManager Instance { get; private set; }
        public string currentRoomId;
        public List<RoomMember> participants = new List<RoomMember>();
        
        private RealtimeChannel roomChannel;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this.gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(this.gameObject);
        }

        public async Task JoinRoom(string roomId)
        {
            currentRoomId = roomId;
            roomChannel = DatabaseManager.Instance.Client.Realtime.Channel($"room-{roomId}");
            
            roomChannel.AddPostgresChangeHandler(PostgresChangesOptions.ListenType.All, (s, c) => {
                RefreshRoomState();
            });
            
            await roomChannel.Subscribe();
        }

        public async Task RefreshRoomState()
        {
            try
            {
                var response = await DatabaseManager.Instance.Client.From<RoomData>()
                    .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, currentRoomId)
                    .Get();
                
                if (response.Models.Count > 0)
                {
                    var room = response.Models[0];
                    if (!string.IsNullOrEmpty(room.participants))
                    {
                        participants = JsonConvert.DeserializeObject<List<RoomMember>>(room.participants);
                        Debug.Log($"[WaitingRoom] Room state updated. Members: {participants.Count}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WaitingRoom] Refresh state failed: {ex.Message}");
            }
        }

        public async Task SelectJob(string jobName)
        {
            await Task.Yield();
            int count = participants.Count(p => p.job == jobName);
            if (count >= 2)
            {
                Debug.LogWarning("해당 직업은 이미 2명이 선택했습니다.");
                return;
            }
            Debug.Log($"[WaitingRoom] Selected Job: {jobName}");
        }

        public async Task ToggleReady() { await Task.Yield(); }
        public async Task KickPlayer(string targetUserId) { await Task.Yield(); }
    }
}
