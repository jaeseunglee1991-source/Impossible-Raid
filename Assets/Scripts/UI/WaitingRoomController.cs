using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using BossRaid.Managers;
using BossRaid.Models;
using TMPro;
using System.Linq;

namespace BossRaid.UI
{
    public class WaitingRoomController : MonoBehaviour
    {
        [Header("Player List (Left)")]
        public Transform playerListContainer;
        public GameObject playerSlotPrefab;

        [Header("Character Preview (Right)")]
        public Image characterPreviewImage;
        public TextMeshProUGUI selectedJobText;

        [Header("Job Selection Grid")]
        public Transform jobGridContainer;
        public GameObject jobIconPrefab;
        private List<Image> jobIconImages = new List<Image>();

        [Header("Game Transition")]
        public GameObject loadingPanel;

        [Header("Extra Features")]
        public TextMeshProUGUI roomInfoText;
        public TextMeshProUGUI warningText;
        public GameObject tooltipPanel;
        public TextMeshProUGUI tooltipText;

        [Header("Buttons")]
        public Button readyButton;
        public Button startButton;

        private string[] jobs = new string[] { "Random", "Warrior", "Rogue", "Paladin", "DeathKnight", "Ranger", "FireMage", "IceMage", "Warlock", "Priest", "Druid" };
        private string[] jobNames = new string[] { "무작위", "전사", "도적", "성기사", "죽음의 기사", "레인저", "화염 마법사", "냉기 마법사", "흑마법사", "사제", "드루이드" };

        private void Start()
        {
            if (WaitingRoomManager.Instance != null)
            {
                WaitingRoomManager.Instance.OnRoomStateChanged += RefreshUI;
            }

            readyButton.onClick.AddListener(OnReadyClicked);
            startButton.onClick.AddListener(OnStartClicked);
            
            // Job Grid 초기화
            if (jobGridContainer != null && jobIconPrefab != null)
            {
                foreach(Transform child in jobGridContainer) Destroy(child.gameObject);
                jobIconImages.Clear();

                for(int i = 0; i < jobs.Length; i++)
                {
                    int index = i;
                    var iconGO = Instantiate(jobIconPrefab, jobGridContainer);
                    var img = iconGO.GetComponent<Image>();
                    jobIconImages.Add(img);

                    var txt = iconGO.GetComponentInChildren<TextMeshProUGUI>();
                    if (txt != null) txt.text = jobNames[i];

                    var btn = iconGO.GetComponent<Button>();
                    btn.onClick.AddListener(() => OnJobChanged(index));

                    // 툴팁 연동 (Hover -> Show/Hide)
                    var trigger = iconGO.AddComponent<UnityEngine.EventSystems.EventTrigger>();
                    var enterEntry = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter };
                    enterEntry.callback.AddListener((data) => ShowTooltip(jobNames[index]));
                    trigger.triggers.Add(enterEntry);

                    var exitEntry = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit };
                    exitEntry.callback.AddListener((data) => HideTooltip());
                    trigger.triggers.Add(exitEntry);
                }
            }

            if (tooltipPanel != null) tooltipPanel.SetActive(false);

            // 데이터 수신 전이라도 즉시 UI 한 번 초기화 (이름 바로 뜨게 하기 위해)
            Invoke(nameof(RefreshUI), 0.1f);
            RefreshUI();
        }

        private void OnDestroy()
        {
            if (WaitingRoomManager.Instance != null)
            {
                WaitingRoomManager.Instance.OnRoomStateChanged -= RefreshUI;
            }
        }

        private void RefreshUI()
        {
            var mgr = WaitingRoomManager.Instance;
            if (mgr == null || mgr.currentRoomData == null) return;

            // 게임 시작 (playing 상태) 처리
            if (mgr.currentRoomData.status == "playing")
            {
                if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "InGameScene") return;

                Debug.Log("[WaitingRoomUI] 'playing' status detected! LOADING InGameScene NOW.");
                if (loadingPanel != null) loadingPanel.SetActive(true);
                
                UnityEngine.SceneManagement.SceneManager.LoadScene("InGameScene"); 
                return;
            }

            // 1. 방 기본 정보 업데이트 (난이도/스테이지 텍스트)
            if (roomInfoText != null)
            {
                string diff = "일반 난이도"; // 나중에 RoomData에 난이도가 추가되면 연동
                string stage = mgr.currentRoomData.stages != null && mgr.currentRoomData.stages.Count > 0 ? mgr.currentRoomData.stages[0].ToString() : "?";
                roomInfoText.text = $"[STAGE {stage} - {diff}] {mgr.currentRoomData.title}";
            }

            // 2. 플레이어 리스트 업데이트 (4인 고정 슬롯 방식)
            if (mgr.participants == null || mgr.participants.Count == 0)
            {
                // 아직 데이터가 안 왔으면 잠시 후 재시도 (게스트 로그인 대비)
                Invoke(nameof(RefreshUI), 0.5f);
                return;
            }

            int tankCount = 0;
            int healerCount = 0;
            
            var playerSlots = playerListContainer.GetComponentsInChildren<RectTransform>(true)
                                .Where(rt => rt.name.StartsWith("PlayerSlot_"))
                                .Select(rt => rt.gameObject)
                                .OrderBy(go => go.name)
                                .ToList();

            for (int i = 0; i < playerSlots.Count; i++)
            {
                var slot = playerSlots[i];
                if (i < mgr.participants.Count)
                {
                    var p = mgr.participants[i];
                    slot.SetActive(true);
                    UpdateSlotUI(slot, p);

                    if (p.job == "Warrior" || p.job == "Paladin" || p.job == "DeathKnight") tankCount++;
                    if (p.job == "Priest" || p.job == "Druid") healerCount++;
                }
                else
                {
                    // 비어있는 슬롯 처리
                    var nickText = slot.transform.Find("Layout/Nickname")?.GetComponent<TextMeshProUGUI>();
                    var jobText = slot.transform.Find("Layout/Job")?.GetComponent<TextMeshProUGUI>();
                    var statusText = slot.transform.Find("Layout/Status")?.GetComponent<TextMeshProUGUI>();
                    if (nickText != null) nickText.text = "<color=#444444>공석</color>";
                    if (jobText != null) jobText.text = "";
                    if (statusText != null) statusText.text = "";
                }
            }

            // 파티 조합 경고 시스템
            if (warningText != null)
            {
                if (tankCount == 0 || healerCount == 0)
                {
                    warningText.text = "<color=red>⚠️ 경고: 탱커 또는 힐러가 부족합니다.</color>";
                    warningText.gameObject.SetActive(true);
                }
                else
                {
                    warningText.gameObject.SetActive(false);
                }
            }

            // 2. 버튼 상태 업데이트
            bool isHost = mgr.IsHost();
            readyButton.gameObject.SetActive(!isHost);
            
            var myId = DatabaseManager.Instance.Client.Auth.CurrentUser.Id;
            var me = mgr.participants.FirstOrDefault(p => p.id == myId);
            if (me != null && !isHost)
            {
                var readyText = readyButton.GetComponentInChildren<TextMeshProUGUI>();
                if (readyText != null) readyText.text = me.isReady ? "CANCEL READY" : "READY";
            }

            startButton.gameObject.SetActive(isHost);
            startButton.interactable = mgr.CanStartGame();

            // 3. 내 캐릭터 프리뷰 업데이트
            UpdateMyPreview();
        }

        private void UpdateSlotUI(GameObject slot, RoomMember member)
        {
            // 인덱스 대신 이름으로 찾아서 정확하게 맵핑 (에러 방지)
            var nickText = slot.transform.Find("Layout/Nickname")?.GetComponent<TextMeshProUGUI>();
            var jobText = slot.transform.Find("Layout/Job")?.GetComponent<TextMeshProUGUI>();
            var statusText = slot.transform.Find("Layout/Status")?.GetComponent<TextMeshProUGUI>();
            var pingText = slot.transform.Find("Layout/Ping")?.GetComponent<TextMeshProUGUI>();

            if (nickText != null) nickText.text = member.nickname;
            
            // 직업명을 한국어로 변환하여 출력
            string displayJob = "[무작위]";
            int jobIdx = System.Array.IndexOf(jobs, member.job);
            if (jobIdx >= 0 && jobIdx < jobNames.Length)
            {
                displayJob = $"[{jobNames[jobIdx]}]";
            }
            if (jobText != null) jobText.text = displayJob;
            
            string status = member.isHost ? "<color=yellow>[방장]</color>" : (member.isReady ? "<color=green>준비 완료</color>" : "대기중");
            if (member.warningCount > 0) status += $" <color=red>({member.warningCount}회 경고)</color>";
            if (statusText != null) statusText.text = status;

            if (pingText != null)
            {
                int randomPing = UnityEngine.Random.Range(12, 45);
                pingText.text = $"Ping: <color=#32CD32>{randomPing}ms</color>";
            }

            var btn = slot.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => OnPlayerClicked(member));
            }
        }

        private void UpdateMyPreview()
        {
            var mgr = WaitingRoomManager.Instance;
            if (mgr == null || mgr.currentRoomData == null) return;
            
            var myId = DatabaseManager.Instance.Client.Auth.CurrentUser.Id;
            var me = mgr.participants.FirstOrDefault(p => p.id == myId);
            
            if (me != null)
            {
                int idx = System.Array.IndexOf(jobs, me.job);
                // 현재 선택된 직업의 아이콘 하이라이트
                for (int i = 0; i < jobIconImages.Count; i++)
                {
                    if (i == idx) jobIconImages[i].color = new Color(1f, 0.8f, 0.2f, 1f); // 금색 하이라이트
                    else jobIconImages[i].color = new Color(0.15f, 0.15f, 0.15f, 1f); // 기본 어두운색
                }

                if (characterPreviewImage != null)
                {
                    var sprite = Resources.Load<Sprite>($"Previews/{me.job}");
                    characterPreviewImage.sprite = sprite;
                    characterPreviewImage.enabled = (sprite != null); 
                    characterPreviewImage.color = sprite != null ? Color.white : new Color(1, 1, 1, 0);
                }
            }
        }

        private void OnPlayerClicked(RoomMember member)
        {
            var myId = DatabaseManager.Instance.Client.Auth.CurrentUser.Id;
            if (WaitingRoomManager.Instance.IsHost() && member.id != myId)
            {
                // 호스트 메뉴 팝업 (Kick/Warn/Ban)
                HostMenuPopup.Instance.Show(member);
            }
            else
            {
                // 일반 플레이어 정보/전적 팝업 호출
                PlayerHistoryPopup.Instance.Show(member.id, member.nickname);
            }
        }

        private async void OnReadyClicked() { await WaitingRoomManager.Instance.ToggleReady(); }
        private async void OnStartClicked() 
        { 
            Debug.Log("[WaitingRoomUI] '공격 시작' (Start) Button Clicked!");
            await WaitingRoomManager.Instance.StartGame(); 
        }
        private async void OnJobChanged(int index) 
        { 
            // 시각적 피드백 즉시 제공 (서버 응답 전에도 노란 박스 이동)
            for (int i = 0; i < jobIconImages.Count; i++)
            {
                if (i == index) jobIconImages[i].color = new Color(1f, 0.8f, 0.2f, 1f); // 금색 강조
                else jobIconImages[i].color = new Color(0.15f, 0.15f, 0.15f, 1f); 
            }

            bool success = await WaitingRoomManager.Instance.SelectJob(jobs[index]); 
            if (!success)
            {
                Debug.LogWarning("[UI] 직업 선택 실패. 이전 상태로 되돌립니다.");
                UpdateMyPreview(); 
            }
        }
        // Tooltip Methods
        public void ShowTooltip(string jobName)
        {
            if (tooltipPanel == null || tooltipText == null) return;
            string cleanJobName = jobName.Replace(" ▼", "").Trim();
            string desc = "직업 스킬 정보가 존재하지 않습니다.";
            switch(cleanJobName)
            {
                case "무작위": desc = "게임 시작 시 겹치지 않는 무작위 직업으로 선택됩니다."; break;
                case "전사": desc = "[탱커]\n1. 방패 밀치기 (도발)\n2. 방어 태세 (피해 감소)"; break;
                case "성기사": desc = "[탱커/서포터]\n1. 신성한 방패 (도발)\n2. 치유의 빛 (힐)"; break;
                case "죽음의 기사": desc = "[탱커/딜러]\n1. 죽음의 손아귀 (도발)\n2. 피의 일격 (흡혈)"; break;
                case "사제": desc = "[힐러]\n1. 치유의 물결\n2. 보호막"; break;
                case "드루이드": desc = "[힐러/서포터]\n1. 재생 (도트 힐)\n2. 정신 자극"; break;
                case "도적": desc = "[근접 딜러]\n1. 기습 (강력한 피해)\n2. 회피"; break;
                case "레인저": desc = "[원거리 딜러]\n1. 연발 사격\n2. 덫 놓기"; break;
                case "화염 마법사": desc = "[원거리 딜러/강한 광역]\n1. 불덩이 작렬\n2. 화염 폭풍"; break;
                case "냉기 마법사": desc = "[원거리 딜러/CC]\n1. 얼음 화살 (둔화)\n2. 눈보라"; break;
                case "흑마법사": desc = "[원거리 딜러/도트]\n1. 부패\n2. 생명력 흡수"; break;
            }
            tooltipText.text = desc;
            tooltipPanel.SetActive(true);
        }

        public void HideTooltip()
        {
            if (tooltipPanel != null) tooltipPanel.SetActive(false);
        }
    }
}
