using UnityEngine;
using UnityEngine.UI;
using BossRaid.Managers;
using BossRaid.Models;
using TMPro;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace BossRaid.UI
{
    public class PlayerHistoryPopup : MonoBehaviour
    {
        public static PlayerHistoryPopup Instance { get; private set; }
        public GameObject root;
        public TextMeshProUGUI titleText;
        public Transform historyContainer;
        public GameObject recordPrefab;
        public Button closeButton;

        private void Awake()
        {
            Instance = this;
            if (root != null) root.SetActive(false);
            if (closeButton != null) closeButton.onClick.AddListener(Close);
        }

        public async void Show(string userId, string nickname)
        {
            titleText.text = $"{nickname}의 전적 기록";
            if (root != null) root.SetActive(true);

            foreach (Transform child in historyContainer) Destroy(child.gameObject);

            // Supabase에서 유저의 과거 전적 조회 (rooms 테이블에서 participants에 해당 유저가 포함된 'finished' 상태의 방들)
            var response = await DatabaseManager.Instance.Client.From<RoomData>()
                .Filter("status", Supabase.Postgrest.Constants.Operator.Equals, "finished")
                .Get();

            if (response.Models != null)
            {
                foreach (var room in response.Models)
                {
                    if (string.IsNullOrEmpty(room.participants)) continue;
                    var members = JsonConvert.DeserializeObject<List<RoomMember>>(room.participants);
                    if (members.Exists(m => m.id == userId))
                    {
                        var go = Instantiate(recordPrefab, historyContainer);
                        var txt = go.GetComponentInChildren<TextMeshProUGUI>();
                        txt.text = $"{room.title} (Finish: {room.created_at:yyyy-MM-dd})";
                    }
                }
            }
        }

        public void Close()
        {
            if (root != null) root.SetActive(false);
        }
    }
}
