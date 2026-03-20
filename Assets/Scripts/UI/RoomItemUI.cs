using UnityEngine;
using TMPro;
using UnityEngine.UI;
using BossRaid.Models;

namespace BossRaid.UI
{
    public class RoomItemUI : MonoBehaviour
    {
        public TMP_Text roomNameText;
        public TMP_Text hostText;
        public TMP_Text playerCountText;
        public Button joinButton;

        private RoomData _room;
        private System.Action<RoomData> _onJoin;

        public void SetRoom(RoomData room, System.Action<RoomData> onJoin)
        {
            _room = room;
            _onJoin = onJoin;

            roomNameText.text = string.IsNullOrEmpty(room.title) ? "Unknown Raid" : room.title;
            
            string displayId = "Guest";
            if (!string.IsNullOrEmpty(room.creator_id))
            {
                displayId = room.creator_id.Length >= 5 ? room.creator_id.Substring(0, 5) : room.creator_id;
            }
            hostText.text = $"Host: {displayId}...";
            
            if (playerCountText != null) playerCountText.text = room.status ?? "waiting";
            
            joinButton.onClick.RemoveAllListeners();
            joinButton.onClick.AddListener(() => _onJoin?.Invoke(_room));
        }
    }
}
