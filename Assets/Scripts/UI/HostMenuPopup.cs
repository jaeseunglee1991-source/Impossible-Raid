using UnityEngine;
using UnityEngine.UI;
using BossRaid.Managers;
using BossRaid.Models;
using TMPro;

namespace BossRaid.UI
{
    public class HostMenuPopup : MonoBehaviour
    {
        public static HostMenuPopup Instance { get; private set; }
        public GameObject root;
        public TextMeshProUGUI titleText;
        public Button kickButton;
        public Button warnButton;
        public Button banButton;
        public Button closeButton;

        private RoomMember targetMember;

        private void Awake()
        {
            Instance = this;
            if (root != null) root.SetActive(false);
            
            if (kickButton != null) kickButton.onClick.AddListener(OnKickClicked);
            if (warnButton != null) warnButton.onClick.AddListener(OnWarnClicked);
            if (banButton != null) banButton.onClick.AddListener(OnBanClicked);
            if (closeButton != null) closeButton.onClick.AddListener(Close);
        }

        public void Show(RoomMember member)
        {
            targetMember = member;
            titleText.text = $"{member.nickname} 관리";
            if (root != null) root.SetActive(true);
        }

        private async void OnKickClicked()
        {
            await WaitingRoomManager.Instance.KickPlayer(targetMember.id);
            Close();
        }

        private async void OnWarnClicked()
        {
            await WaitingRoomManager.Instance.WarnPlayer(targetMember.id);
            Close();
        }

        private async void OnBanClicked()
        {
            await WaitingRoomManager.Instance.BanPlayer(targetMember.id);
            Close();
        }

        public void Close()
        {
            if (root != null) root.SetActive(false);
        }
    }
}
