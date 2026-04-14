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
        public RoomData currentRoomData;
        
        public event Action OnRoomStateChanged;

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
                _ = RefreshRoomState();
            });
            
            await roomChannel.Subscribe();
            await RefreshRoomState();
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
                    currentRoomData = response.Models[0];
                    if (!string.IsNullOrEmpty(currentRoomData.participants))
                    {
                        participants = JsonConvert.DeserializeObject<List<RoomMember>>(currentRoomData.participants);
                        Debug.Log($"[WaitingRoom] Room state updated. Members: {participants.Count}");
                        OnRoomStateChanged?.Invoke();
                    }

                    // 만약 내가 강퇴당했거나 방이 사라졌다면?
                    var myId = DatabaseManager.Instance.Client.Auth.CurrentUser.Id;
                    if (!participants.Any(p => p.id == myId))
                    {
                        Debug.LogWarning("[WaitingRoom] You are no longer in this room.");
                        // Handle leave/kick UI
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WaitingRoom] Refresh state failed: {ex.Message}");
            }
        }

        private async Task SyncParticipants()
        {
            try
            {
                var json = JsonConvert.SerializeObject(participants);
                await DatabaseManager.Instance.Client.From<RoomData>()
                    .Where(x => x.id == currentRoomId)
                    .Set(x => x.participants, json)
                    .Update();
                Debug.Log("[WaitingRoom] Participants synced successfully.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WaitingRoom] Sync participants failed: {ex.Message}");
            }
        }

        public async Task<bool> SelectJob(string jobName)
        {
            var myId = DatabaseManager.Instance.Client.Auth.CurrentUser.Id;
            var me = participants.FirstOrDefault(p => p.id == myId);
            if (me == null) return false;

            if (jobName != "Random")
            {
                // 나를 제외한 다른 사람들이 이 직업을 몇 명 선택했는지 검사
                int count = participants.Count(p => p.job == jobName && p.id != myId);
                if (count >= 2)
                {
                    Debug.LogWarning("해당 직업은 이미 2명이 선택했습니다.");
                    return false;
                }
            }

            me.job = jobName;
            OnRoomStateChanged?.Invoke(); // 로컬에서도 즉시 반영
            await SyncParticipants();
            return true;
        }

        public async Task ToggleReady()
        {
            var myId = DatabaseManager.Instance.Client.Auth.CurrentUser.Id;
            var me = participants.FirstOrDefault(p => p.id == myId);
            if (me == null || me.isHost) return;

            me.isReady = !me.isReady;
            OnRoomStateChanged?.Invoke(); // 로컬에서도 즉시 반영
            await SyncParticipants();
        }

        // --- Host Actions ---

        public async Task KickPlayer(string targetUserId)
        {
            if (!IsHost()) return;
            participants.RemoveAll(p => p.id == targetUserId);
            await SyncParticipants();
        }

        public async Task WarnPlayer(string targetUserId)
        {
            if (!IsHost()) return;
            var target = participants.FirstOrDefault(p => p.id == targetUserId);
            if (target != null)
            {
                target.warningCount++;
                if (target.warningCount >= 3)
                {
                    Debug.Log($"[WaitingRoom] Player {target.nickname} auto-kicked due to 3 warnings.");
                    participants.Remove(target);
                }
                await SyncParticipants();
            }
        }

        public async Task BanPlayer(string targetUserId)
        {
            if (!IsHost()) return;
            
            // participants에서 제거
            participants.RemoveAll(p => p.id == targetUserId);

            // banned_user_ids 업데이트
            var bannedList = string.IsNullOrEmpty(currentRoomData.banned_user_ids) 
                ? new List<string>() 
                : JsonConvert.DeserializeObject<List<string>>(currentRoomData.banned_user_ids);
            
            if (!bannedList.Contains(targetUserId))
            {
                bannedList.Add(targetUserId);
                var bannedJson = JsonConvert.SerializeObject(bannedList);
                
                await DatabaseManager.Instance.Client.From<RoomData>()
                    .Where(x => x.id == currentRoomId)
                    .Set(x => x.banned_user_ids, bannedJson)
                    .Set(x => x.participants, JsonConvert.SerializeObject(participants))
                    .Update();
            }
        }

        public bool IsHost()
        {
            if (DatabaseManager.Instance == null || DatabaseManager.Instance.Client == null || DatabaseManager.Instance.Client.Auth.CurrentUser == null)
            {
                // 테스트 모드나 비로그인 상태일 경우: 참가자 리스트의 첫 번째 플레이어 설정값을 따름
                if (participants != null && participants.Count > 0) return participants[0].isHost;
                return false;
            }
            var myId = DatabaseManager.Instance.Client.Auth.CurrentUser.Id;
            return participants.Any(p => p.id == myId && p.isHost);
        }

        public bool CanStartGame()
        {
            if (!IsHost()) return false;
            // 방장 제외 모든 플레이어가 Ready 상태여야 함
            return participants.Where(p => !p.isHost).All(p => p.isReady);
        }

        public async Task StartGame()
        {
            Debug.Log("[WaitingRoom] StartGame request received.");
            
            if (!IsHost())
            {
                Debug.LogWarning("[WaitingRoom] StartGame failed: You are not the Host.");
                return;
            }

            if (!CanStartGame())
            {
                Debug.LogWarning("[WaitingRoom] StartGame failed: Not all players are READY.");
                // 참가자 상태 로그 출력
                foreach(var p in participants)
                    Debug.Log($" - Player: {p.nickname}, Host: {p.isHost}, Ready: {p.isReady}");
                return;
            }

            Debug.Log("[WaitingRoom] All checks passed. Deterministically assigning jobs and starting...");

            // 1. 랜덤 직업 결정
            AssignRandomJobs();

            // 2. 방 상태를 'playing'으로 변경
            try
            {
                Debug.Log($"[WaitingRoom] Attempting to update DB status to 'playing' for room: {currentRoomId}");
                var response = await DatabaseManager.Instance.Client.From<RoomData>()
                    .Where(x => x.id == currentRoomId)
                    .Set(x => x.status, "playing")
                    .Set(x => x.participants, JsonConvert.SerializeObject(participants))
                    .Update();

                Debug.Log("[WaitingRoom] Database status update request sent. Checking response...");
                
                // 만약 Realtime 응답이 늦을 경우를 대비해 호스트는 즉시 로컬 상태 갱신 시도
                if (currentRoomData != null) currentRoomData.status = "playing";
                OnRoomStateChanged?.Invoke(); 
                
                Debug.Log("[WaitingRoom] Local status updated to 'playing'. Scene transition should trigger.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WaitingRoom] StartGame DB Update Failed: {ex.Message}\n{ex.StackTrace}");
            }
        }

        // --- Rematch Logic (Rule 4-1-2) ---
        public async Task RequestRematch()
        {
            if (!IsHost()) return;
            
            try
            {
                Debug.Log($"[WaitingRoom] Rule 4-1-1: Host requesting rematch for room: {currentRoomId}");
                
                // 전원 Ready 해제 (상태 초기화)
                foreach (var p in participants) p.isReady = false;
                
                await DatabaseManager.Instance.Client.From<RoomData>()
                    .Where(x => x.id == currentRoomId)
                    .Set(x => x.status, "rematch")
                    .Set(x => x.participants, JsonConvert.SerializeObject(participants))
                    .Update();

                // 로컬 즉시 반영
                if (currentRoomData != null) currentRoomData.status = "rematch";
                OnRoomStateChanged?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WaitingRoom] RequestRematch Failed: {ex.Message}");
            }
        }

        public async Task AcceptRematch()
        {
            var myId = DatabaseManager.Instance.Client.Auth.CurrentUser.Id;
            var me = participants.FirstOrDefault(p => p.id == myId);
            if (me == null) return;

            Debug.Log($"[WaitingRoom] AcceptRematch: Player {me.nickname} accepted.");
            me.isReady = true;
            await SyncParticipants();

            // 모든 플레이어가 수락했는지 체크 (방장 본인 포함)
            bool everyoneReady = participants.All(p => p.isReady || p.isHost);
            Debug.Log($"[WaitingRoom] Ready Check: {everyoneReady} (Everyone accepted?)");

            if (IsHost() && everyoneReady)
            {
                Debug.Log("[WaitingRoom] Rule 4-1-2: Everyone accepted. Returning to Waiting Room.");
                await ReturnToWaitingRoom();
            }
        }

        public async Task DeclineRematch()
        {
            Debug.Log("[WaitingRoom] Rule 4-1-2: Rematch declined. Disbanding party to Lobby.");
            try
            {
                await DatabaseManager.Instance.Client.From<RoomData>()
                    .Where(x => x.id == currentRoomId)
                    .Set(x => x.status, "lobby")
                    .Update();

                // 로컬 즉시 반영
                if (currentRoomData != null) currentRoomData.status = "lobby";
                OnRoomStateChanged?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WaitingRoom] DeclineRematch Failed: {ex.Message}");
            }
        }

        private async Task ReturnToWaitingRoom()
        {
            try
            {
                Debug.Log("[WaitingRoom] Updating status to 'waiting'...");
                await DatabaseManager.Instance.Client.From<RoomData>()
                    .Where(x => x.id == currentRoomId)
                    .Set(x => x.status, "waiting")
                    .Update();

                // 로컬 즉시 반영 (씬 전환 트리거)
                if (currentRoomData != null) currentRoomData.status = "waiting";
                OnRoomStateChanged?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WaitingRoom] ReturnToWaitingRoom Failed: {ex.Message}");
            }
        }

        private void AssignRandomJobs()
        {
            string[] availableJobs = { "Warrior", "Rogue", "Paladin", "DeathKnight", "Ranger", "FireMage", "IceMage", "Warlock", "Priest", "Druid" };
            var randomSelects = participants.Where(p => p.job == "Random").ToList();
            
            foreach (var p in randomSelects)
            {
                // 현재 직업별 점유수 계산
                var counts = participants.GroupBy(x => x.job)
                    .ToDictionary(g => g.Key, g => g.Count());

                // 사용 가능한 (2명 미만인) 직업 리스트 추출
                var validJobs = availableJobs.Where(j => !counts.ContainsKey(j) || counts[j] < 2).ToList();
                
                if (validJobs.Count > 0)
                {
                    p.job = validJobs[UnityEngine.Random.Range(0, validJobs.Count)];
                }
                else
                {
                    // 모든 직업이 꽉 찬 경우는 발생하지 않아야 함 (5인 기준)
                    p.job = availableJobs[0];
                }
            }
        }
    }
}
