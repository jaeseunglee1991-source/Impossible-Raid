#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using System.IO;
using UnityEditor.Events;
using UnityEngine.UI;
using BossRaid.Managers;

namespace BossRaid.Editor
{
    public static class LoginSceneBuilder
    {
        [MenuItem("Tools/Modern Login/Build Scene (Google & Guest)")]
        public static void BuildModernLoginScene()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("경고", "플레이 모드 중에는 씬을 생성할 수 없습니다. 플레이를 멈추고 다시 실행해주세요.", "확인");
                return;
            }
            string loginScenePath = "Assets/Scenes/LoginScene.unity";
            string lobbyScenePath = "Assets/Scenes/LobbyScene.unity";
            string waitingRoomScenePath = "Assets/Scenes/WaitingRoomScene.unity";

            if (!Directory.Exists("Assets/Scenes"))
                Directory.CreateDirectory("Assets/Scenes");

            // 1. LobbyScene 생성
            BuildModernLobbyScene();

            // 2. LoginScene 생성
            var loginScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            loginScene.name = "LoginScene";

            var cameraGO = new GameObject("Main Camera");
            var cam = cameraGO.AddComponent<Camera>();
            cam.tag = "MainCamera";
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.09f, 0.12f, 1f); 
            cameraGO.transform.position = new Vector3(0, 0, -10);

            // EventSystem (Input System 호환성 처리)
            CreateEventSystem();

            var canvasGO = new GameObject("Canvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var cs = canvasGO.AddComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1920, 1080);
            canvasGO.AddComponent<GraphicRaycaster>();

            var mainBoxColor = new Color(0.12f, 0.14f, 0.18f, 0.95f);
            var loginPanel = CreatePanel(canvasGO.transform, "LoginPanel", mainBoxColor, Vector2.zero, new Vector2(600, 500));
            CreateText(loginPanel.transform, "Impossible Raid", 64, new Vector2(0, 150), new Color(0.2f, 0.6f, 1f));
            
            var loadingStatusText = CreateText(loginPanel.transform, "Google 로그인 시도 중...", 24, new Vector2(0, -180), Color.gray);
            loadingStatusText.gameObject.SetActive(false);

            var googleBtnGO = CreateButton(loginPanel.transform, "GoogleLoginBtn", "Google 계정으로 로그인", new Color(0.26f, 0.52f, 0.96f), new Vector2(0, 20), new Vector2(450, 80));
            var guestBtnGO  = CreateButton(loginPanel.transform, "GuestLoginBtn", "게스트로 시작하기", new Color(0.3f, 0.35f, 0.4f), new Vector2(0, -80), new Vector2(450, 60));

            var popupPanel = CreatePanel(canvasGO.transform, "PopupPanel", new Color(0.05f, 0.06f, 0.08f, 0.98f), Vector2.zero, new Vector2(450, 300));
            var pCanvas = popupPanel.AddComponent<Canvas>();
            pCanvas.overrideSorting = true;
            pCanvas.sortingOrder = 999;
            popupPanel.AddComponent<GraphicRaycaster>();
            popupPanel.SetActive(false);

            var popupTextGO = new GameObject("PopupText");
            popupTextGO.transform.SetParent(popupPanel.transform, false);
            var popupText = popupTextGO.AddComponent<TextMeshProUGUI>();
            popupText.text = "Popup Message";
            popupText.fontSize = 24;
            popupText.alignment = TextAlignmentOptions.Center;
            popupText.rectTransform.sizeDelta = new Vector2(400, 150);
            popupText.rectTransform.anchoredPosition = new Vector2(0, 30);
            var popupBtnGO = CreateButton(popupPanel.transform, "PopupCloseButton", "확인", new Color(0.16f, 0.47f, 0.88f), new Vector2(0, -80), new Vector2(160, 45));

            EnsureManagers(loginScene);

            var controllerGO = new GameObject("LoginUIController");
            var ctrl = controllerGO.AddComponent<BossRaid.UI.LoginUIController>();
            ctrl.loginPanel = loginPanel;
            ctrl.googleLoginButton = googleBtnGO.GetComponent<Button>();
            ctrl.guestLoginButton = guestBtnGO.GetComponent<Button>();
            ctrl.loadingStatusText = loadingStatusText;
            ctrl.popupPanel = popupPanel;
            ctrl.popupText = popupText;
            ctrl.popupCloseButton = popupBtnGO.GetComponent<Button>();
            ctrl.lobbySceneName = "LobbyScene";

            UnityEventTools.AddPersistentListener(ctrl.googleLoginButton.onClick, ctrl.OnGoogleLoginClicked);
            UnityEventTools.AddPersistentListener(ctrl.guestLoginButton.onClick, ctrl.OnGuestLoginClicked);
            UnityEventTools.AddPersistentListener(ctrl.popupCloseButton.onClick, ctrl.ClosePopup);

            ApplyPretendardFont();

            EditorSceneManager.SaveScene(loginScene, loginScenePath);
            AssetDatabase.Refresh();
            SetBuildSettings(loginScenePath, lobbyScenePath, waitingRoomScenePath);

            EditorUtility.DisplayDialog("완료!", "모든 씬과 빌드 세팅이 복구되었습니다.", "확인");
        }


        // 구식 로비 생성기 삭제됨

        [MenuItem("Tools/Modern Login/Build All Scenes")]
        public static void BuildAllScenes()
        {
            BuildModernLoginScene();
            BuildModernLobbyScene();
            BuildWaitingRoomScene();
        }

        [MenuItem("Tools/Modern Login/Build Lobby Scene")]
        public static void BuildModernLobbyScene()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("경고", "플레이 모드 중에는 씬을 생성할 수 없습니다. 플레이를 멈추고 다시 실행해주세요.", "확인");
                return;
            }
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            scene.name = "LobbyScene";

            // ─────────────────────────────────────────────
            //  카메라 & 기본 설정
            // ─────────────────────────────────────────────
            var cam = GameObject.FindAnyObjectByType<Camera>();
            if (cam != null) cam.backgroundColor = new Color(0.05f, 0.05f, 0.07f);

            // ─────────────────────────────────────────────
            //  UI Canvas
            // ─────────────────────────────────────────────
            var canvasGO = new GameObject("Canvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGO.AddComponent<GraphicRaycaster>();

            // 기존 EventSystem이 있다면 제거 후 새로 생성 (설정 클린업)
            CreateEventSystem();
            Debug.Log("[SceneBuilder] EventSystem Re-created at Root.");

            // ─────────────────────────────────────────────
            //  WC3 스타일 레이아웃 (색상 테마)
            // ─────────────────────────────────────────────
            Color stoneBg = new Color(0.1f, 0.11f, 0.14f);
            Color metalBorder = new Color(0.25f, 0.28f, 0.35f);
            Color chatBg = new Color(0.02f, 0.02f, 0.03f, 0.85f);

            // 1. 전체 배경
            var bgGO = CreatePanel(canvasGO.transform, "MainBackground", stoneBg, Vector2.zero, Vector2.zero);
            SetRect(bgGO.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var bgImg = bgGO.GetComponent<Image>();
            if (bgImg != null) bgImg.raycastTarget = false; // 배경이 하위 요소의 클릭을 막지 않도록 설정



            // ─────────────────────────────────────────────
            // [사분면 레이아웃 설정 - Meticulous Refinement]
            // ─────────────────────────────────────────────
            Color outerBorder = new Color(0.25f, 0.28f, 0.35f); // 외부 테두리 (Slate)
            Color innerShadow = new Color(0.05f, 0.06f, 0.08f); // 내부 그림자
            Color panelBody   = new Color(0.1f, 0.11f, 0.14f);  // 메인 바디
            Color accentGold  = new Color(1f, 0.8f, 0.2f, 0.8f); // 강조색 (Gold)

            // 1. 중앙 상단: 방 목록 영역 (Top-Left - Large)
            var roomListBorderGO = CreatePanel(bgGO.transform, "RoomListFrame", outerBorder, Vector2.zero, Vector2.zero);
            SetRect(roomListBorderGO.GetComponent<RectTransform>(), new Vector2(0.02f, 0.45f), new Vector2(0.73f, 0.98f), Vector2.zero, Vector2.zero);
            var roomListInnerGO = CreatePanel(roomListBorderGO.transform, "Inner", panelBody, Vector2.zero, Vector2.zero);
            SetRect(roomListInnerGO.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, new Vector2(3, 3), new Vector2(-3, -3));

            var roomHeaderGO = CreatePanel(roomListInnerGO.transform, "Header", innerShadow, Vector2.zero, Vector2.zero);
            SetRect(roomHeaderGO.GetComponent<RectTransform>(), new Vector2(0, 0.85f), Vector2.one, Vector2.zero, Vector2.zero);
            CreateText(roomHeaderGO.transform, "방 목록 (Room List)", 14, Vector2.zero, accentGold);

            var roomScrollGO = CreateScrollView(roomListInnerGO.transform, "RoomScroll");
            var roomContent = roomScrollGO.transform.Find("Viewport/Content");
            SetRect(roomScrollGO.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, new Vector2(5, 5), new Vector2(-5, -55));
            var refreshBtnGO = CreateButton(roomListInnerGO.transform, "RefreshBtn", "방 목록 새로고침", new Color(0.15f, 0.25f, 0.45f, 0.7f), new Vector2(0, 70), new Vector2(300, 60));
            // 히트박스를 버튼 이미지 크기와 일치시킵니다 (CreateButton 내부에서 처리됨)
            // 리프레시 버튼을 하단 중앙에 배치 (anchors 조정)
            var refreshRT = refreshBtnGO.GetComponent<RectTransform>();
            refreshRT.anchorMin = new Vector2(0.5f, 0); refreshRT.anchorMax = new Vector2(0.5f, 0);
            refreshRT.anchoredPosition = new Vector2(0, 30);
            refreshBtnGO.GetComponentInChildren<TextMeshProUGUI>().fontSize = 18;

            // 2. 우측 상단: 게임 만들기 버튼 영역 (Top-Right)
            var createGameTriggerBorderGO = CreatePanel(bgGO.transform, "CreateGameTriggerFrame", outerBorder, Vector2.zero, Vector2.zero);
            SetRect(createGameTriggerBorderGO.GetComponent<RectTransform>(), new Vector2(0.75f, 0.72f), new Vector2(0.98f, 0.88f), Vector2.zero, Vector2.zero);
            var createGameTriggerInnerGO = CreatePanel(createGameTriggerBorderGO.transform, "Inner", panelBody, Vector2.zero, Vector2.zero);
            SetRect(createGameTriggerInnerGO.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, new Vector2(3, 3), new Vector2(-3, -3));

            var createGameHeaderGO = CreatePanel(createGameTriggerInnerGO.transform, "Header", innerShadow, Vector2.zero, Vector2.zero);
            SetRect(createGameHeaderGO.GetComponent<RectTransform>(), new Vector2(0, 0.75f), Vector2.one, Vector2.zero, Vector2.zero);
            CreateText(createGameHeaderGO.transform, "게임 지휘소", 14, Vector2.zero, accentGold);

            var createGameBtn = CreateButton(createGameTriggerInnerGO.transform, "OpenCreateRoomBtn", "CREATE NEW RAID\n(게임 만들기)", new Color(0.15f, 0.35f, 0.2f, 0.5f), Vector2.zero, Vector2.zero);
            var btnRT = createGameBtn.GetComponent<RectTransform>();
            btnRT.anchorMin = Vector2.zero; btnRT.anchorMax = Vector2.one;
            btnRT.offsetMin = Vector2.zero; btnRT.offsetMax = Vector2.zero;
            createGameBtn.GetComponentInChildren<TextMeshProUGUI>().fontSize = 20;
            // Hover 효과 등을 위해 투명도를 조절하거나 배경색을 더 진하게 할 수 있습니다.

            // 3. 중앙/하단 좌측: 대규모 채팅창 (Center-Left)
            var chatBorderGO = CreatePanel(bgGO.transform, "ChatFrame", outerBorder, Vector2.zero, Vector2.zero);
            SetRect(chatBorderGO.GetComponent<RectTransform>(), new Vector2(0.02f, 0.15f), new Vector2(0.73f, 0.43f), Vector2.zero, Vector2.zero);
            var chatInnerGO = CreatePanel(chatBorderGO.transform, "Inner", chatBg, Vector2.zero, Vector2.zero);
            SetRect(chatInnerGO.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, new Vector2(3, 3), new Vector2(-3, -3));

            var chatHeaderGO = CreatePanel(chatInnerGO.transform, "Header", innerShadow, Vector2.zero, Vector2.zero);
            SetRect(chatHeaderGO.GetComponent<RectTransform>(), new Vector2(0, 0.85f), Vector2.one, Vector2.zero, Vector2.zero);
            CreateText(chatHeaderGO.transform, "채팅 (Chat)", 14, Vector2.zero, new Color(0.2f, 0.8f, 0.2f));

            var chatScrollGO = CreateScrollView(chatInnerGO.transform, "ChatScroll");
            var chatContent = chatScrollGO.transform.Find("Viewport/Content");
            SetRect(chatScrollGO.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, new Vector2(5, 35), new Vector2(-5, -5));

            // 채팅 입력 - 하단에 독립적으로 배치
            var chatInputBorderGO = CreatePanel(bgGO.transform, "ChatInputFrame", outerBorder, Vector2.zero, Vector2.zero);
            SetRect(chatInputBorderGO.GetComponent<RectTransform>(), new Vector2(0.02f, 0.05f), new Vector2(0.73f, 0.13f), Vector2.zero, Vector2.zero);
            var chatInputInnerGO = CreatePanel(chatInputBorderGO.transform, "Inner", new Color(0.05f, 0.05f, 0.07f, 1f), Vector2.zero, Vector2.zero);
            SetRect(chatInputInnerGO.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, new Vector2(3, 3), new Vector2(-3, -3));
            
            // 전송 버튼 추가 (우측 끝)
            var sendBtnGO = CreateButton(chatInputInnerGO.transform, "SendBtn", "SEND", new Color(0.15f, 0.25f, 0.45f), new Vector2(-60, 0), new Vector2(100, 40));
            var sendBtnRT = sendBtnGO.GetComponent<RectTransform>();
            sendBtnRT.anchorMin = new Vector2(1, 0.5f); sendBtnRT.anchorMax = new Vector2(1, 0.5f);
            sendBtnRT.anchoredPosition = new Vector2(-60, 0);
            sendBtnGO.GetComponentInChildren<TextMeshProUGUI>().fontSize = 16;

            // 채팅 입력 - 버튼 자리를 제외한 나머지 영역
            var chatInputGO = new GameObject("ChatInputArea", typeof(RectTransform));
            chatInputGO.transform.SetParent(chatInputInnerGO.transform, false);
            var ciRT = chatInputGO.GetComponent<RectTransform>();
            ciRT.anchorMin = Vector2.zero; ciRT.anchorMax = new Vector2(1, 1);
            ciRT.offsetMin = new Vector2(5, 5); ciRT.offsetMax = new Vector2(-125, -5); // 버튼 자리 확보

            var chatInputField = CreateInputField(chatInputGO.transform, "ChatInput", "Broadcast message (Enter)...");
            SetRect(chatInputField.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            // 4. 우측: 플레이어 리스트 (Right - Tall)
            var playerBorderGO = CreatePanel(bgGO.transform, "PlayerFrame", outerBorder, Vector2.zero, Vector2.zero);
            SetRect(playerBorderGO.GetComponent<RectTransform>(), new Vector2(0.75f, 0.05f), new Vector2(0.98f, 0.70f), Vector2.zero, Vector2.zero);
            var playerInnerGO = CreatePanel(playerBorderGO.transform, "Inner", new Color(0.08f, 0.09f, 0.12f), Vector2.zero, Vector2.zero);
            SetRect(playerInnerGO.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, new Vector2(3, 3), new Vector2(-3, -3));

            var playerHeaderGO = CreatePanel(playerInnerGO.transform, "Header", innerShadow, Vector2.zero, Vector2.zero);
            SetRect(playerHeaderGO.GetComponent<RectTransform>(), new Vector2(0, 0.94f), Vector2.one, Vector2.zero, Vector2.zero);
            CreateText(playerHeaderGO.transform, "접속자 (Users)", 14, Vector2.zero, Color.cyan);

            var playerScrollGO = CreateScrollView(playerInnerGO.transform, "PlayerScroll");
            var playerContent = playerScrollGO.transform.Find("Viewport/Content");
            SetRect(playerScrollGO.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, new Vector2(5, 5), new Vector2(-5, -45));

            // ─────────────────────────────────────────────
            //  [SC/WC3 스타일 게임 생성 화면 생성]
            // ─────────────────────────────────────────────
            var createRoomPanel = CreatePanel(canvasGO.transform, "CreateRoomPanel", new Color(0, 0, 0, 0.9f), Vector2.zero, Vector2.zero);
            SetRect(createRoomPanel.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            createRoomPanel.SetActive(false);

            // 배경 장식
            var createFrame = CreatePanel(createRoomPanel.transform, "Frame", stoneBg, Vector2.zero, new Vector2(1200, 800));
            CreateText(createFrame.transform, "CREATE MULTIPLAYER MISSION", 32, new Vector2(0, 350), accentGold);

            // 스테이지 선택부
            var toggles = new Toggle[8];
            for (int i = 0; i < 8; i++)
            {
                var tGO = new GameObject($"StageToggle_{i+1}", typeof(RectTransform), typeof(Toggle));
                tGO.transform.SetParent(createFrame.transform, false);
                var rt = tGO.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(-450, 240 - (i * 60));
                rt.sizeDelta = new Vector2(250, 50);
                var tImg = CreatePanel(tGO.transform, "Background", new Color(0.2f, 0.2f, 0.2f), Vector2.zero, Vector2.zero);
                SetRect(tImg.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                var tCheck = CreatePanel(tImg.transform, "Checkmark", accentGold, Vector2.zero, Vector2.zero);
                SetRect(tCheck.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, new Vector2(5, 5), new Vector2(-5, -5));
                var tLabel = CreateText(tGO.transform, $"Stage {i+1}", 20, new Vector2(100, 0), Color.white);
                tLabel.alignment = TextAlignmentOptions.Left;
                var toggle = tGO.GetComponent<Toggle>();
                toggle.targetGraphic = tImg.GetComponent<Image>();
                toggle.graphic = tCheck.GetComponent<Image>();
                toggle.isOn = (i == 0);
                toggles[i] = toggle;
            }

            // 맵 정보부
            var mapInfoBody = CreatePanel(createFrame.transform, "MapInfo", innerShadow, new Vector2(100, 50), new Vector2(500, 450));
            var mapTitle = CreateText(mapInfoBody.transform, "Stage Name", 26, new Vector2(0, 180), accentGold);
            var mapDesc = CreateText(mapInfoBody.transform, "Description...", 20, new Vector2(0, -20), Color.white);
            mapDesc.rectTransform.sizeDelta = new Vector2(450, 300);
            mapDesc.alignment = TextAlignmentOptions.TopLeft;

            // 방 설정부
            var roomTitleInp = CreateInputField(createFrame.transform, "RoomTitleInp", "Mission Title...");
            roomTitleInp.GetComponent<RectTransform>().anchoredPosition = new Vector2(450, 40);
            roomTitleInp.GetComponent<RectTransform>().sizeDelta = new Vector2(300, 50);
            var roomPassInp = CreateInputField(createFrame.transform, "RoomPassInp", "Password (Optional)");
            roomPassInp.GetComponent<RectTransform>().anchoredPosition = new Vector2(450, -30);
            roomPassInp.GetComponent<RectTransform>().sizeDelta = new Vector2(300, 50);

            // 버튼
            var startBtn = CreateButton(createFrame.transform, "StartMissionBtn", "INITIALIZE OPERATION", new Color(0.15f, 0.35f, 0.2f), new Vector2(450, -200), new Vector2(300, 60));
            var cancelBtn = CreateButton(createFrame.transform, "CancelBtn", "ABORT", new Color(0.35f, 0.15f, 0.15f), new Vector2(450, -280), new Vector2(300, 50));

            // CreateGameUIController 추가 및 연결
            var createCtrl = createRoomPanel.AddComponent<BossRaid.UI.CreateGameUIController>();
            createCtrl.roomTitleInput = roomTitleInp;
            createCtrl.passwordInput = roomPassInp;
            createCtrl.createButton = startBtn.GetComponent<Button>();
            createCtrl.cancelButton = cancelBtn.GetComponent<Button>();
            createCtrl.stageToggles = toggles;
            createCtrl.mapNameText = mapTitle;
            createCtrl.mapDescText = mapDesc;
            createCtrl.selectionInfoText = CreateText(createFrame.transform, "Stages Selected: 1/8", 16, new Vector2(-450, -250), Color.gray);

            // ─────────────────────────────────────────────
            //  LobbyUIController & Mapping
            // ─────────────────────────────────────────────
            var controllerGO = new GameObject("LobbyUIController");
            var controller = controllerGO.AddComponent<BossRaid.UI.LobbyUIController>();

            // 프리팹 로드
            controller.roomItemPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/RoomItem.prefab");

            // 미니 프로필 (상단 우측 여백에 배치)
            var profGO = new GameObject("MiniProfile", typeof(RectTransform));
            profGO.transform.SetParent(bgGO.transform, false);
            SetRect(profGO.GetComponent<RectTransform>(), new Vector2(0.75f, 0.92f), Vector2.one, Vector2.zero, new Vector2(-20, -10));
            controller.nicknameText = CreateText(profGO.transform, "---", 18, new Vector2(-60, 15), accentGold);
            controller.statsText = CreateText(profGO.transform, "---", 14, new Vector2(-60, -15), Color.gray);
            controller.nicknameText.alignment = TextAlignmentOptions.Right;
            controller.statsText.alignment = TextAlignmentOptions.Right;

            controller.roomListContainer = roomContent; 
            controller.refreshButton = refreshBtnGO.GetComponent<Button>();
            controller.roomTitleInput = roomTitleInp; 
            controller.createRoomPanel = createRoomPanel; 
            controller.activeOpsPanelButton = createGameBtn.GetComponent<Button>();
            controller.activeOpsTitleText = createGameHeaderGO.GetComponentInChildren<TextMeshProUGUI>();

            controller.chatContent = chatContent;
            controller.chatInput = chatInputField;
            controller.sendButton = sendBtnGO.GetComponent<Button>();
            controller.playerListContent = playerContent;

            // ─────────────────────────────────────────────
            //  Managers (필수)
            // ─────────────────────────────────────────────
            EnsureManagers(scene);



            // ─────────────────────────────────────────────
            //  폰트 & 마무리
            // ─────────────────────────────────────────────
            var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Pretendard-Regular SDF.asset");
            if (fontAsset != null)
            {
                foreach (var t in Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    t.font = fontAsset;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, "Assets/Scenes/LobbyScene.unity");
            Debug.Log("[LoginSceneBuilder] WC3 Style Lobby Scene built successfully!");
        }

        private static void EnsureManagers(UnityEngine.SceneManagement.Scene scene)
        {
            GameObject managersGO = GameObject.Find("Managers");
            if (managersGO == null)
            {
                managersGO = new GameObject("Managers");
                Debug.Log("[SceneBuilder] Created new 'Managers' object.");
            }

            // 빌드 중인 씬으로 이동
            UnityEditor.SceneManagement.EditorSceneManager.MoveGameObjectToScene(managersGO, scene);

            if (managersGO.GetComponent<DatabaseManager>() == null) managersGO.AddComponent<DatabaseManager>();
            if (managersGO.GetComponent<AuthManager>() == null) managersGO.AddComponent<AuthManager>();
            if (managersGO.GetComponent<LobbyManager>() == null) managersGO.AddComponent<LobbyManager>();
            if (managersGO.GetComponent<WaitingRoomManager>() == null) managersGO.AddComponent<WaitingRoomManager>();
            if (managersGO.GetComponent<ResultManager>() == null) managersGO.AddComponent<ResultManager>();
        }

        private static TMP_InputField CreateInputField(Transform parent, string name, string placeholder, bool isPassword = false)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            SetRect(go.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, new Vector2(10, 5), new Vector2(-10, -5));
            go.GetComponent<Image>().color = new Color(0, 0, 0, 0.3f);
            
            var input = go.AddComponent<TMP_InputField>();
            
            // TMP_InputField는 Viewport(TextArea)와 TextComponent가 올바르게 연결되어야 합니다.
            var textAreaGO = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
            textAreaGO.transform.SetParent(go.transform, false);
            var textAreaRT = textAreaGO.GetComponent<RectTransform>();
            textAreaRT.anchorMin = Vector2.zero; textAreaRT.anchorMax = Vector2.one;
            textAreaRT.offsetMin = new Vector2(10, 5); textAreaRT.offsetMax = new Vector2(-10, -5);

            var textGO = new GameObject("Text", typeof(RectTransform));
            textGO.transform.SetParent(textAreaGO.transform, false);
            var text = textGO.AddComponent<TextMeshProUGUI>();
            text.fontSize = 20; text.color = Color.white;
            text.alignment = TextAlignmentOptions.Left; // 좌측 정렬
            text.verticalAlignment = VerticalAlignmentOptions.Middle; // 중앙 정렬
            text.textWrappingMode = TextWrappingModes.NoWrap; // 한 줄 입력 권장
            text.rectTransform.anchorMin = Vector2.zero; text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.sizeDelta = Vector2.zero;
            
            var placeholderGO = new GameObject("Placeholder", typeof(RectTransform));
            placeholderGO.transform.SetParent(textAreaGO.transform, false);
            var pText = placeholderGO.AddComponent<TextMeshProUGUI>();
            pText.text = placeholder; pText.fontSize = 20; pText.color = Color.gray; pText.fontStyle = FontStyles.Italic;
            pText.alignment = TextAlignmentOptions.Left;
            pText.verticalAlignment = VerticalAlignmentOptions.Middle;
            pText.rectTransform.anchorMin = Vector2.zero; pText.rectTransform.anchorMax = Vector2.one;
            pText.rectTransform.sizeDelta = Vector2.zero;

            input.textViewport = textAreaRT;
            input.textComponent = text;
            input.placeholder = pText;
            if (isPassword) input.contentType = TMP_InputField.ContentType.Password;
            
            return input;
        }

        private static GameObject CreateScrollView(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(ScrollRect), typeof(Image));
            go.transform.SetParent(parent, false);
            SetRect(go.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, new Vector2(10, 10), new Vector2(-10, -10));
            go.GetComponent<Image>().color = new Color(0, 0, 0, 0.1f);

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            viewport.transform.SetParent(go.transform, false);
            SetRect(viewport.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1); contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.sizeDelta = new Vector2(0, 0);

            var vlg = content.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(10, 10, 10, 10);
            vlg.spacing = 5;
            vlg.childControlHeight = false; vlg.childControlWidth = true;
            vlg.childForceExpandHeight = false; vlg.childForceExpandWidth = false;

            content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = go.GetComponent<ScrollRect>();
            scroll.content = contentRect;
            scroll.viewport = viewport.GetComponent<RectTransform>();
            scroll.horizontal = false;
            
            return go;
        }



        private static void SetRect(RectTransform rect, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static GameObject CreatePanel(Transform parent, string name, Color color, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            if (rt == null) rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            var img = go.AddComponent<Image>();
            img.color = color; img.raycastTarget = true;
            return go;
        }

        private static TMP_Text CreateText(Transform parent, string content, int size, Vector2 pos, Color color)
        {
            var go = new GameObject("Text_" + content);
            go.transform.SetParent(parent, false);
            var txt = go.AddComponent<TextMeshProUGUI>();
            txt.text = content; txt.fontSize = size; txt.color = color;
            txt.alignment = TextAlignmentOptions.Center;
            var rt = txt.GetComponent<RectTransform>();
            if (rt != null) { rt.anchoredPosition = pos; rt.sizeDelta = new Vector2(800, 100); }
            return txt;
        }

        private static GameObject CreateButton(Transform parent, string name, string label, Color color, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = color; img.raycastTarget = true;
            
            var rt = go.GetComponent<RectTransform>();
            if (rt == null) rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;

            var txtGO = new GameObject("Label");
            txtGO.transform.SetParent(go.transform, false);
            var txt = txtGO.AddComponent<TextMeshProUGUI>();
            txt.text = label; txt.color = Color.white; txt.fontSize = 24;
            txt.alignment = TextAlignmentOptions.Center;
            txt.raycastTarget = false;
            
            var txtRT = txt.GetComponent<RectTransform>();
            if (txtRT != null)
            {
                txtRT.anchorMin = Vector2.zero; txtRT.anchorMax = Vector2.one;
                txtRT.offsetMin = Vector2.zero; txtRT.offsetMax = Vector2.zero;
            }

            return go;
        }

        private static void SetBuildSettings(string loginPath, string lobbyPath, string waitingPath)
        {
            var scenes = new EditorBuildSettingsScene[] {
                new EditorBuildSettingsScene(loginPath, true),
                new EditorBuildSettingsScene(lobbyPath, true),
                new EditorBuildSettingsScene(waitingPath, true)
            };
            EditorBuildSettings.scenes = scenes;
        }

        [MenuItem("Tools/Modern Login/Build Waiting Room Scene")]
        public static void BuildWaitingRoomScene()
        {
            if (EditorApplication.isPlaying) return;
            string path = "Assets/Scenes/WaitingRoomScene.unity";
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            scene.name = "WaitingRoomScene";

            var canvasGO = new GameObject("Canvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var cs = canvasGO.AddComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1920, 1080);
            canvasGO.AddComponent<GraphicRaycaster>();

            CreateEventSystem();

            // 배경
            var bgGO = CreatePanel(canvasGO.transform, "Background", new Color(0.08f, 0.09f, 0.12f, 1f), Vector2.zero, Vector2.zero);
            SetRect(bgGO.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            CreateText(canvasGO.transform, "WAITING ROOM (대기실)", 48, new Vector2(0, 300), new Color(1, 0.8f, 0));
            CreateText(canvasGO.transform, "Mission starting soon...", 24, new Vector2(0, 200), Color.white);

            // 간단하게 돌아가기 버튼만 일단 배치
            var backBtnGO = CreateButton(canvasGO.transform, "BackToLobbyBtn", "LEAVE ROOM (나가기)", new Color(0.4f, 0.1f, 0.1f, 0.8f), new Vector2(0, -350), new Vector2(300, 60));
            
            // 리스너 연결 (에디터 전용)
            var backBtn = backBtnGO.GetComponent<Button>();
            UnityEventTools.AddPersistentListener(backBtn.onClick, OnLeaveRoomClicked);

            EnsureManagers(scene);
            ApplyPretendardFont();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, path);
            AssetDatabase.Refresh();
            Debug.Log("[SceneBuilder] Waiting Room Scene built successfully!");
        }

        private static void OnLeaveRoomClicked()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("LobbyScene");
        }

        private static void ApplyPretendardFont()
        {
            var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Pretendard-Regular SDF.asset");
            if (fontAsset != null)
            {
                foreach (var t in Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    t.font = fontAsset;
            }
        }
        private static void CreateEventSystem()
        {
            var existingES = Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>();
            if (existingES != null) Object.DestroyImmediate(existingES.gameObject);

            var eventSystemGO = new GameObject("EventSystem");
            eventSystemGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
            
            var inputModuleType = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (inputModuleType != null) 
            {
                eventSystemGO.AddComponent(inputModuleType);
            }
            else 
            {
                eventSystemGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }
        }
    }
}
#endif
