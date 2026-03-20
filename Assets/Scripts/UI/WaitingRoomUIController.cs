using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using BossRaid.Managers;
using BossRaid.Models;

namespace BossRaid.UI
{
    public class WaitingRoomUIController : MonoBehaviour
    {
        [Header("Player Slots")]
        public GameObject[] playerSlots; // 4개 슬롯 UI
        public TMP_Text[] nicknameTexts;
        public TMP_Text[] jobTexts;
        public GameObject[] readyBadges;

        [Header("Job Selection")]
        public Button[] jobButtons; // 10개 직업 버튼
        public Button randomJobButton;

        [Header("Room Actions")]
        public Button readyButton;
        public Button startButton;
        public TMP_Text roomInfoText;

        private void Start()
        {
            readyButton.onClick.AddListener(OnReadyClicked);
            startButton.onClick.AddListener(OnStartClicked);
            randomJobButton.onClick.AddListener(OnRandomClicked);

            for (int i = 0; i < jobButtons.Length; i++)
            {
                int index = i;
                jobButtons[i].onClick.AddListener(() => OnJobSelected(index));
            }
        }

        public void UpdateUI(List<RoomMember> participants)
        {
            // 슬롯 초기화
            foreach (var slot in playerSlots) slot.SetActive(false);

            for (int i = 0; i < participants.Count && i < 4; i++)
            {
                playerSlots[i].SetActive(true);
                nicknameTexts[i].text = participants[i].nickname + (participants[i].isHost ? " (Host)" : "");
                jobTexts[i].text = participants[i].job;
                readyBadges[i].SetActive(participants[i].isReady);
            }

            // 시작 버튼 활성화 여부 (방장이면서 전원 레디)
            // bool allReady = participants.All(p => p.isHost || p.isReady);
            // startButton.interactable = allReady && isLocalUserHost;
        }

        private string[] jobs = new string[] { "Warrior", "Rogue", "Paladin", "DeathKnight", "Ranger", "FireMage", "IceMage", "Warlock", "Priest", "Druid" };

        private async void OnJobSelected(int index)
        {
            string jobName = index < jobs.Length ? jobs[index] : "Warrior";
            await WaitingRoomManager.Instance.SelectJob(jobName);
        }

        private async void OnRandomClicked()
        {
            await System.Threading.Tasks.Task.Yield();
            // 랜덤 직업 선택 로직
        }

        private async void OnReadyClicked()
        {
            await WaitingRoomManager.Instance.ToggleReady();
        }

        private async void OnStartClicked()
        {
            await System.Threading.Tasks.Task.Yield();
            // 게임 시작 로직 (씬 전환 및 보스 소환)
        }
    }
}
