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

namespace BossRaid.Managers
{
    public class LobbyManager : MonoBehaviour
    {
        public static LobbyManager Instance { get; private set; }
        private RealtimeChannel lobbyChannel;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this.gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(this.gameObject);
        }

        public async Task<RoomData> CreateRoom(string title, string password, int[] stages)
        {
            try
            {
                var myId = DatabaseManager.Instance.Client.Auth.CurrentUser.Id;
                var myNickname = AuthManager.Instance.LocalUser.nickname;
                var initialParticipants = new List<RoomMember> { new RoomMember(myId, myNickname, true) };

                var newRoom = new RoomData
                {
                    title = title,
                    password = password,
                    stages = new List<int>(stages),
                    status = "waiting",
                    creator_id = myId,
                    participants = JsonConvert.SerializeObject(initialParticipants)
                };

                var response = await DatabaseManager.Instance.Client.From<RoomData>().Insert(newRoom);
                if (response.Models.Count > 0)
                {
                    Debug.Log($"[LobbyManager] Room '{title}' created with ID: {response.Models[0].id}");
                    return response.Models[0];
                }
                return null;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LobbyManager] Create room failed: {ex.Message}");
                return null;
            }
        }

        public async Task<List<RoomData>> GetRoomList()
        {
            try
            {
                if (DatabaseManager.Instance == null || DatabaseManager.Instance.Client == null)
                {
                    Debug.LogWarning("[LobbyManager] Client not initialized yet.");
                    return new List<RoomData>();
                }

                var query = DatabaseManager.Instance.Client.From<RoomData>();
                if (query == null) return new List<RoomData>();

                Debug.Log("[LobbyManager] Starting GetRoomList query...");
                var response = await query.Filter("status", Supabase.Postgrest.Constants.Operator.Equals, "waiting").Get();
                
                if (response == null) {
                    Debug.LogError("[LobbyManager] Response is null!");
                    return new List<RoomData>();
                }
                
                Debug.Log($"[LobbyManager] Query finished. Success: {response.ResponseMessage.IsSuccessStatusCode}");
                
                if (response.Models == null) {
                    Debug.LogWarning("[LobbyManager] response.Models is null. Returning empty list.");
                    return new List<RoomData>();
                }

                Debug.Log($"[LobbyManager] Found {response.Models.Count} rooms. Creating list copy...");
                var resultList = new List<RoomData>();
                foreach (var m in response.Models)
                {
                    if (m != null) resultList.Add(m);
                }
                
                Debug.Log($"[LobbyManager] Returning {resultList.Count} rooms.");
                return resultList;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LobbyManager] Get room list failed at GetRoomList: {ex.Message}\n{ex.StackTrace}");
                return new List<RoomData>();
            }
        }

        public async Task JoinLobbyChat(Action<string, string> onMessageReceived)
        {
            lobbyChannel = DatabaseManager.Instance.Client.Realtime.Channel("lobby-chat");
            
            // lobbyChannel.OnBroadcast("message", (msg) => { ... });

            await lobbyChannel.Subscribe();
        }

        public async Task SendChatMessage(string user, string text)
        {
            await Task.Yield();
            if (lobbyChannel == null) return;
            var payload = new Dictionary<string, object> { { "user", user }, { "text", text } };
            // await lobbyChannel.Send("message", payload);
        }

        public async Task<bool> JoinRoom(RoomData room)
        {
            try
            {
                var myId = DatabaseManager.Instance.Client.Auth.CurrentUser.Id;
                var myNickname = AuthManager.Instance.LocalUser.nickname;

                // 1. 차단 여부 확인
                if (!string.IsNullOrEmpty(room.banned_user_ids))
                {
                    var bannedList = JsonConvert.DeserializeObject<List<string>>(room.banned_user_ids);
                    if (bannedList.Contains(myId))
                    {
                        Debug.LogError("[LobbyManager] You are banned from this room.");
                        return false;
                    }
                }

                // 2. 참가자 목록 업데이트
                var participants = string.IsNullOrEmpty(room.participants)
                    ? new List<RoomMember>()
                    : JsonConvert.DeserializeObject<List<RoomMember>>(room.participants);

                if (participants.Any(p => p.id == myId)) return true; // 이미 참여 중

                if (participants.Count >= (room.max_participants > 0 ? room.max_participants : 5))
                {
                    Debug.LogError("[LobbyManager] Room is full.");
                    return false;
                }

                participants.Add(new RoomMember(myId, myNickname, false));
                var json = JsonConvert.SerializeObject(participants);

                await DatabaseManager.Instance.Client.From<RoomData>()
                    .Where(x => x.id == room.id)
                    .Set(x => x.participants, json)
                    .Update();

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LobbyManager] Join room failed: {ex.Message}");
                return false;
            }
        }
    }
}
