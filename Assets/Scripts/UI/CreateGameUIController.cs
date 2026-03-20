using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using BossRaid.Managers;
using BossRaid.Models;

namespace BossRaid.UI
{
    public class CreateGameUIController : MonoBehaviour
    {
        [Header("Room Settings")]
        public TMP_InputField roomTitleInput;
        public TMP_InputField passwordInput;
        public Button createButton;
        public Button cancelButton;

        [Header("Stage Selection (1-8)")]
        public Toggle[] stageToggles; // 인스펙터에서 8개 할당
        public TMP_Text selectionInfoText;

        [Header("Map Info Display")]
        public TMP_Text mapNameText;
        public TMP_Text mapDescText;
        public Image mapPreviewImage;

        private List<MapData> _allMaps;
        private int _currentPreviewIndex = 0;

        private void Start()
        {
            InitializeMaps();
            SetupEvents();
            UpdateUI();
        }

        private void InitializeMaps()
        {
            _allMaps = new List<MapData>
            {
                new MapData(1, "심판의 숲", "근접 공격형 보스 '숲의 감시자'가 등장합니다.", "자연속성"),
                new MapData(2, "망령의 묘지", "언데드 마법사 보스가 소환수를 부립니다.", "암흑속성"),
                new MapData(3, "불타는 폐허", "강력한 화염 광역 공격을 퍼붓는 화염 정령입니다.", "화염속성"),
                new MapData(4, "얼어붙은 요새", "이동 속도를 늦추는 냉기 장벽을 생성합니다.", "냉기속성"),
                new MapData(5, "폭풍의 절벽", "빠른 연사 속도로 전기를 방출합니다.", "전기속성"),
                new MapData(6, "공허의 틈새", "플레이어의 스킬을 봉쇄하는 중력 제어 보스입니다.", "혼돈속성"),
                new MapData(7, "고대의 유적", "방어력이 매우 높으며 주기적으로 보호막을 생성합니다.", "물리교란"),
                new MapData(8, "말세의 성역", "모든 속성을 다루는 최종 보스 '데스 로드'가 등장합니다.", "전속성")
            };
        }

        private void SetupEvents()
        {
            createButton.onClick.AddListener(OnCreateClicked);
            cancelButton.onClick.AddListener(() => gameObject.SetActive(false));

            for (int i = 0; i < stageToggles.Length; i++)
            {
                int index = i;
                stageToggles[i].onValueChanged.AddListener((isOn) => OnStageToggleChanged(index, isOn));
                
                // 마우스 오버 시 미리보기 갱신 효과를 원하신다면 EventTrigger 등을 추가할 수 있습니다.
                // 여기서는 토글 클릭 시 해당 스테이지 정보를 보여주는 것으로 간주합니다.
            }
        }

        private void OnStageToggleChanged(int index, bool isOn)
        {
            if (isOn)
            {
                _currentPreviewIndex = index;
                UpdateMapInfo(_allMaps[index]);
            }
            UpdateSelectionInfo();
        }

        private void UpdateMapInfo(MapData map)
        {
            mapNameText.text = $"[STAGE {map.stageIndex}] {map.mapName}";
            mapDescText.text = $"<color=#FFD700>속성: {map.attributeInfo}</color>\n\n{map.description}";
            // mapPreviewImage.sprite = map.previewSprite; 
        }

        private void UpdateSelectionInfo()
        {
            var selectedCount = stageToggles.Count(t => t.isOn);
            selectionInfoText.text = $"선택된 단계: {selectedCount} / 8";
            
            // 최소 1개는 선택되어야 함
            createButton.interactable = selectedCount > 0;
            
            if (selectedCount == 0)
            {
                selectionInfoText.text += " <color=red>(최소 1개 선택 필요)</color>";
            }
        }

        private void UpdateUI()
        {
            // 초기 미리보기는 1단계
            UpdateMapInfo(_allMaps[0]);
            UpdateSelectionInfo();
        }

        private async void OnCreateClicked()
        {
            List<int> selectedStages = new List<int>();
            for (int i = 0; i < stageToggles.Length; i++)
            {
                if (stageToggles[i].isOn)
                {
                    selectedStages.Add(_allMaps[i].stageIndex);
                }
            }

            // 낮은 단계부터 순차적으로 정렬 (유저의 편의성)
            selectedStages.Sort();

            string title = roomTitleInput.text;
            if (string.IsNullOrEmpty(title))
            {
                string creatorName = (AuthManager.Instance != null && AuthManager.Instance.LocalUser != null) 
                    ? AuthManager.Instance.LocalUser.nickname 
                    : "Player";
                title = $"{creatorName}'s Raid";
            }

            string password = passwordInput.text;

            createButton.interactable = false;
            try
            {
                if (LobbyManager.Instance == null)
                {
                    Debug.LogError("[CreateGame] LobbyManager.Instance is null!");
                    return;
                }
                var createdRoom = await LobbyManager.Instance.CreateRoom(title, password, selectedStages.ToArray());
                if (createdRoom != null)
                {
                    Debug.Log($"[CreateGame] Room created successfully: {createdRoom.id}");
                    
                    // 대기방 매니저에 정보 전달 및 씬 전환
                    if (WaitingRoomManager.Instance != null)
                    {
                        await WaitingRoomManager.Instance.JoinRoom(createdRoom.id.ToString());
                    }
                    
                    UnityEngine.SceneManagement.SceneManager.LoadScene("WaitingRoomScene");
                    gameObject.SetActive(false);
                }
            }
            finally
            {
                createButton.interactable = true;
            }
        }
    }
}
