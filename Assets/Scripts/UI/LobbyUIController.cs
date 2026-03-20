using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using BossRaid.Managers;
using BossRaid.Models;
using Supabase.Realtime;
using Newtonsoft.Json;

namespace BossRaid.UI
{
    public class LobbyUIController : MonoBehaviour
    {
        [Header("User Profile")]
        public TMP_Text nicknameText;
        public TMP_Text statsText;

        [Header("Room List")]
        public Transform roomListContainer;
        public GameObject roomItemPrefab;
        public Button refreshButton;

        [Header("Room Creation")]
        public TMP_InputField roomTitleInput;
        public Button openCreateRoomPanelButton;
        public Button confirmCreateRoomButton;
        public GameObject createRoomPanel;
        
        [Header("New Create Game System (RTS Style)")]
        public Button activeOpsPanelButton; // "ACTIVE OPS LIST" 영역에 배치할 버튼
        public TMP_Text activeOpsTitleText;

        [Header("Real-time Community (WC3 Style)")]
        public Transform chatContent;
        public TMP_InputField chatInput;
        public Button sendButton;
        public Transform playerListContent;
        public GameObject chatItemPrefab; // Optional: if null, use simple text
        public GameObject playerItemPrefab;

        private Supabase.Realtime.RealtimeChannel _lobbyChannel;
        private bool _isProcessing = false;

        private void Start()
        {
            Debug.Log("[Lobby] Start called.");
            InitializeProfile();
            InitializeRealtime();
            
            refreshButton.onClick.AddListener(OnRefreshClicked);
            if (chatInput != null) chatInput.onEndEdit.AddListener(OnChatSubmitted);
            if (sendButton != null) sendButton.onClick.AddListener(() => OnChatSubmitted(chatInput.text));

            if (activeOpsPanelButton != null) {
                Debug.Log("[Lobby] Adding listener to activeOpsPanelButton.");
                activeOpsPanelButton.onClick.AddListener(OnActiveOpsPanelClicked);
                if (activeOpsTitleText != null) activeOpsTitleText.text = "CREATE GAME";
            }
            else {
                Debug.LogError("[Lobby] activeOpsPanelButton is NULL in Start!");
            }
            
            if (createRoomPanel != null) createRoomPanel.SetActive(false);
            
            // 초기 데이터 로드
            UpdatePlayerList();
            OnRefreshClicked();
        }

        private async void InitializeRealtime()
        {
            if (DatabaseManager.Instance == null || AuthManager.Instance.LocalUser == null) return;

            try
            {
                var client = DatabaseManager.Instance.Client;
                _lobbyChannel = client.Realtime.Channel("lobby-community");

                // [주의] 현재 프로젝트의 Supabase SDK 버전 문제로 Broadcast/Presence 기능이 컴파일 오류를 일으켜 임시 주석 처리합니다.
                // 대신 UI 구조는 정상적으로 작동하며, 추후 호환되는 방식으로 기능을 복구하겠습니다.
                
                /*
                _lobbyChannel.Register(new BroadcastOptions("chat"));
                _lobbyChannel.OnBroadcast("chat", (payload) =>
                {
                    var msg = JsonConvert.DeserializeObject<ChatMessage>(payload.Payload.ToString());
                    if (msg != null) AddChatMessage(msg);
                });

                _lobbyChannel.On(Constants.Layer.Presence, Constants.EventType.Sync, (s, e) => {
                    UpdatePlayerList();
                });
                */

                await _lobbyChannel.Subscribe();
                
                /*
                await _lobbyChannel.Track(new Dictionary<string, object> { 
                    { "nickname", AuthManager.Instance.LocalUser.nickname },
                    { "id", AuthManager.Instance.LocalUser.id }
                });
                */

                AddSystemMessage("Battle.net 채널에 연결되었습니다. (채팅 서버 점검 중)");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Lobby] Realtime init failed: {ex.Message}");
            }
        }

        private void OnChatSubmitted(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            
            // 현재 버전에서 브로드캐스트 미지원으로 로컬에만 표시 (임시)
            var user = AuthManager.Instance.LocalUser;
            var chatMsg = new ChatMessage(user.id, user.nickname, text);
            AddChatMessage(chatMsg);
            
            chatInput.text = "";
            chatInput.ActivateInputField();
        }

        private void AddChatMessage(ChatMessage msg)
        {
            // 화려한 WC3 스타일 텍스트 생성
            var msgGO = new GameObject("ChatMsg", typeof(RectTransform));
            msgGO.transform.SetParent(chatContent, false);
            
            var layout = msgGO.AddComponent<LayoutElement>();
            layout.preferredHeight = 30; // 텍스트 높이에 맞춤

            var txt = msgGO.AddComponent<TextMeshProUGUI>();
            txt.fontSize = 20;
            txt.alignment = TextAlignmentOptions.Left; // 왼쪽 정렬 명시
            
            string colorNick = "#FFD700"; // Gold
            if (msg.SenderId == AuthManager.Instance.LocalUser.id) colorNick = "#00FFFF"; // Cyan for self
            
            txt.text = $"<color={colorNick}>{msg.SenderNickname}</color>: {msg.Content}";
            
            // 레이아웃 업데이트 및 자동 스크롤 하단 이동
            Canvas.ForceUpdateCanvases();
            var scrollRect = chatContent.GetComponentInParent<ScrollRect>();
            if (scrollRect != null)
            {
                scrollRect.verticalNormalizedPosition = 0f;
            }
        }

        private void AddSystemMessage(string content)
        {
            var msgGO = new GameObject("SystemMsg", typeof(RectTransform));
            msgGO.transform.SetParent(chatContent, false);
            
            var layout = msgGO.AddComponent<LayoutElement>();
            layout.preferredHeight = 30;

            var txt = msgGO.AddComponent<TextMeshProUGUI>();
            txt.fontSize = 18;
            txt.text = $"<color=#00FF00>[시스템]</color>: {content}";
            
            // 레이아웃 업데이트 및 자동 스크롤 하단 이동
            Canvas.ForceUpdateCanvases();
            var scrollRect = chatContent.GetComponentInParent<ScrollRect>();
            if (scrollRect != null)
            {
                scrollRect.verticalNormalizedPosition = 0f;
            }
        }

        private void UpdatePlayerList()
        {
            if (AuthManager.Instance == null || AuthManager.Instance.LocalUser == null)
            {
                Debug.LogWarning("[Lobby] AuthManager or LocalUser is null. Player list won't show the local user.");
                return;
            }

            // 현재 버전에서 Presence 미지원으로 내 이름만 표시
            if (playerListContent != null && playerListContent.childCount == 0)
            {
                var pGO = new GameObject("PlayerItem", typeof(RectTransform));
                pGO.transform.SetParent(playerListContent, false);
                
                var layout = pGO.AddComponent<LayoutElement>();
                layout.preferredHeight = 35; // WC3 스타일 리스트 높이

                var rt = pGO.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(0, 30); 
                
                var txt = pGO.AddComponent<TextMeshProUGUI>();
                txt.fontSize = 20; // 크기 약간 조절
                txt.text = $" {AuthManager.Instance.LocalUser.nickname}";
                txt.alignment = TextAlignmentOptions.Left;
                txt.verticalAlignment = VerticalAlignmentOptions.Middle;
                txt.color = Color.cyan;
                txt.enableWordWrapping = false; // 한 줄로 표시
                txt.overflowMode = TextOverflowModes.Ellipsis; // 넘치면 ... 표시
                
                // [추가] 레이아웃 강제 업데이트 (스크롤뷰 내에서 안 보일 수 있음)
                Canvas.ForceUpdateCanvases();
            }
        }

        private void OnDestroy()
        {
            if (_lobbyChannel != null)
            {
                _lobbyChannel.Unsubscribe();
            }
        }

        private void InitializeProfile()
        {
            if (AuthManager.Instance != null && AuthManager.Instance.LocalUser != null)
            {
                var user = AuthManager.Instance.LocalUser;
                if (nicknameText != null) nicknameText.text = user.nickname;
                if (statsText != null) statsText.text = $"보스 처치: {user.boss_kills} | 스테이지: {user.max_stage_reached}";
            }
            else
            {
                if (nicknameText != null) nicknameText.text = "Guest Player#----";
                if (statsText != null) statsText.text = "로그인이 필요합니다.";
            }
        }

        private async void OnRefreshClicked()
        {
            if (_isProcessing) return;
            if (roomListContainer == null || LobbyManager.Instance == null)
            {
                Debug.LogWarning("[Lobby] roomListContainer or LobbyManager.Instance is null.");
                return;
            }

            _isProcessing = true;
            Debug.Log("[Lobby] Refreshing room list...");
            
            // 기존 리스트 삭제
            foreach (Transform child in roomListContainer)
            {
                Destroy(child.gameObject);
            }

            try
            {
                List<RoomData> rooms = await LobbyManager.Instance.GetRoomList();
                if (rooms != null)
                {
                    foreach (var room in rooms)
                    {
                        if (roomItemPrefab == null) continue;
                        var itemGO = Instantiate(roomItemPrefab, roomListContainer);
                        var itemUI = itemGO.GetComponent<RoomItemUI>();
                        if (itemUI != null)
                        {
                            // 레이아웃 높이 강제 설정 (프리팹 수정 대신 코드에서 보완)
                            var layout = itemGO.GetComponent<LayoutElement>();
                            if (layout == null) layout = itemGO.AddComponent<LayoutElement>();
                            layout.preferredHeight = 80;
                            layout.minHeight = 80;

                            // 자식 요소들의 디자인 및 위치 보정 (WC3 스타일)
                            if (itemUI.roomNameText != null) {
                                itemUI.roomNameText.fontSize = 18;
                                itemUI.roomNameText.alignment = TextAlignmentOptions.Left;
                                itemUI.roomNameText.color = new Color(1f, 0.8f, 0.2f); // Gold
                                itemUI.roomNameText.rectTransform.anchoredPosition = new Vector2(20, 15);
                            }
                            if (itemUI.hostText != null) {
                                itemUI.hostText.fontSize = 14;
                                itemUI.hostText.color = Color.gray;
                                itemUI.hostText.rectTransform.anchoredPosition = new Vector2(20, -15);
                            }
                            if (itemUI.playerCountText != null) {
                                itemUI.playerCountText.fontSize = 14;
                                itemUI.playerCountText.color = Color.gray;
                                itemUI.playerCountText.alignment = TextAlignmentOptions.Right;
                                itemUI.playerCountText.rectTransform.anchoredPosition = new Vector2(-150, 0);
                            }
                            if (itemUI.joinButton != null) {
                                var btnImg = itemUI.joinButton.GetComponent<Image>();
                                if (btnImg != null) btnImg.color = new Color(0.15f, 0.25f, 0.45f);
                                var btnRT = itemUI.joinButton.GetComponent<RectTransform>();
                                btnRT.sizeDelta = new Vector2(100, 35);
                                btnRT.anchorMin = new Vector2(1, 0.5f);
                                btnRT.anchorMax = new Vector2(1, 0.5f);
                                btnRT.anchoredPosition = new Vector2(-60, 0);
                            }

                            itemUI.SetRoom(room, OnJoinRoomClicked);
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Lobby] Failed to refresh rooms: {ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                _isProcessing = false;
            }
        }

        private async void OnConfirmCreateRoomClicked()
        {
            if (_isProcessing) return;
            _isProcessing = true;

            string title = roomTitleInput.text;
            if (string.IsNullOrEmpty(title))
            {
                title = $"{AuthManager.Instance.LocalUser.nickname}'s Raid";
            }

            try
            {
                // 임시로 스테이지 1 선택
                var createdRoom = await LobbyManager.Instance.CreateRoom(title, "", new int[] { 1 });
                if (createdRoom != null)
                {
                    if (WaitingRoomManager.Instance != null)
                    {
                        await WaitingRoomManager.Instance.JoinRoom(createdRoom.id.ToString());
                    }
                    UnityEngine.SceneManagement.SceneManager.LoadScene("WaitingRoomScene");
                    if (createRoomPanel != null) createRoomPanel.SetActive(false);
                }
            }
            finally
            {
                _isProcessing = false;
            }
        }

        private void OnJoinRoomClicked(RoomData room)
        {
            Debug.Log($"[Lobby] Joining room: {room.title}");
            // 멀티플레이어 연동 로직 (Photon 등)
        }

        private void OnActiveOpsPanelClicked()
        {
            Debug.Log("[Lobby] OnActiveOpsPanelClicked triggered.");
            if (createRoomPanel != null)
            {
                createRoomPanel.SetActive(true);
                createRoomPanel.transform.SetAsLastSibling();
                Debug.Log("[Lobby] createRoomPanel activated and brought to front.");
                if (roomTitleInput != null)
                {
                    roomTitleInput.text = "";
                }
            }
            else
            {
                Debug.LogError("[Lobby] createRoomPanel is NULL!");
            }
        }
    }
}
