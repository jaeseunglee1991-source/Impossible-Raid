using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BossRaid.Models;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace BossRaid.UI
{
    public class ResultUIController : MonoBehaviour
    {
        [Header("Status")]
        public TMP_Text resultTitle; // WIN / LOSS
        public TMP_Text clearTimeText;
        public GameObject winEffect;
        public GameObject loseEffect;

        [Header("MVP Section")]
        public TMP_Text mvpNicknameText;
        public Image mvpRoleIcon;

        [Header("Statistics")]
        public Transform statsContainer;
        public GameObject statsItemPrefab;

        [Header("Buttons")]
        public Button lobbyButton;

        private void Start()
        {
            lobbyButton.onClick.AddListener(() => SceneManager.LoadScene("LobbyScene"));
        }

        public void ShowResult(bool isWin, float time, List<CombatRecord> stats)
        {
            resultTitle.text = isWin ? "MISSION CLEAR" : "MISSION FAILED";
            clearTimeText.text = $"Clear Time: {time:F1}s";
            
            winEffect.SetActive(isWin);
            loseEffect.SetActive(!isWin);

            // MVP 찾기
            var mvp = stats.Find(s => s.isMvp);
            if (mvp != null)
            {
                mvpNicknameText.text = $"MVP: {mvp.nickname}";
                // mvpRoleIcon.sprite = GetRoleSprite(mvp.role);
            }

            // 통계 아이템 생성
            foreach (var s in stats)
            {
                var go = Instantiate(statsItemPrefab, statsContainer);
                // go.GetComponent<StatsItem>().SetData(s);
            }
        }
    }
}
