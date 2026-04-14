using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BossRaid.Models;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.Linq;
using BossRaid.Managers;

namespace BossRaid.UI
{
    public class ResultUIController : MonoBehaviour
    {
        [Header("Basic Result")]
        public TMP_Text resultTitle;
        public TMP_Text clearTimeText;
        public GameObject winEffect;
        public GameObject loseEffect;

        [Header("MVP Info")]
        public TMP_Text mvpNicknameText;
        public Image mvpRoleIcon;

        [Header("Statistics")]
        public Transform statsContainer;
        public GameObject statsItemPrefab;

        [Header("Stats Bar Scale")]
        public float dpsMaxVal = 1f;
        public float hpsMaxVal = 1f;
        public float tankMaxVal = 1f;

        [Header("Buttons")]
        public Button lobbyButton;
        public Button restartButton; // 호스트 전용: 동일 멤버로 다시하기
        public Button waitingRoomButton;

        [Header("Rematch Popup")]
        public GameObject rematchRequestPopup;
        public Button btnRematchAccept;
        public Button btnRematchDecline;
        public TMP_Text rematchTimer;

        private float currentRematchTime = 10f;
        private bool isRematchTimerActive = false;

        private void Start()
        {
            // 커서 즉시 해제 (가장 먼저 수행하여 UI 조작 가능하게 함)
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            if (ResultManager.Instance != null && ResultManager.Instance.lastStats != null)
            {
                ShowResult(
                    ResultManager.Instance.lastIsWin, 
                    ResultManager.Instance.lastClearTime, 
                    ResultManager.Instance.lastStats
                );
            }

            // 모든 버튼 리스너 초기화 후 재등록 (중복 등록 방지)
            lobbyButton.onClick.RemoveAllListeners();
            lobbyButton.onClick.AddListener(() => SceneManager.LoadScene("LobbyScene"));

            waitingRoomButton.onClick.RemoveAllListeners();
            waitingRoomButton.onClick.AddListener(() => SceneManager.LoadScene("WaitingRoomScene"));

            // 호스트 여부 체크 (방장만 재시작 버튼 노출)
            bool isHost = WaitingRoomManager.Instance != null && WaitingRoomManager.Instance.IsHost();
            if (restartButton != null)
            {
                restartButton.gameObject.SetActive(isHost);
                restartButton.onClick.RemoveAllListeners();
                restartButton.onClick.AddListener(OnRestartRequested);
            }

            if (rematchRequestPopup != null) rematchRequestPopup.SetActive(false);
            if (btnRematchAccept != null) 
            {
                btnRematchAccept.onClick.RemoveAllListeners();
                btnRematchAccept.onClick.AddListener(OnRematchAccepted);
            }
            if (btnRematchDecline != null) 
            {
                btnRematchDecline.onClick.RemoveAllListeners();
                btnRematchDecline.onClick.AddListener(OnRematchDeclined);
            }
        }

        private void Update()
        {
            if (isRematchTimerActive)
            {
                currentRematchTime -= Time.deltaTime;
                if (rematchTimer != null) 
                    rematchTimer.text = $"남은 시간: {Mathf.CeilToInt(currentRematchTime)}초";
                
                if (currentRematchTime <= 0)
                {
                    OnRematchDeclined(); // 시간 초과 시 거부/파티 해산
                }
            }
        }

        private void OnEnable()
        {
            if (WaitingRoomManager.Instance != null)
                WaitingRoomManager.Instance.OnRoomStateChanged += HandleRoomStateChanged;
        }

        private void OnDisable()
        {
            if (WaitingRoomManager.Instance != null)
                WaitingRoomManager.Instance.OnRoomStateChanged -= HandleRoomStateChanged;
        }

        private void HandleRoomStateChanged()
        {
            var wm = WaitingRoomManager.Instance;
            if (wm == null || wm.currentRoomData == null) return;

            Debug.Log($"[ResultSync] Received Room Status: {wm.currentRoomData.status}");

            // 1. 재경기 요청 발생 시
            if (wm.currentRoomData.status == "rematch")
            {
                if (rematchRequestPopup != null && !rematchRequestPopup.activeSelf)
                {
                    rematchRequestPopup.SetActive(true);
                    currentRematchTime = 10f;
                    isRematchTimerActive = true;
                }
            }
            // 2. 전원 수락 완료 (waiting으로 복귀됨)
            else if (wm.currentRoomData.status == "waiting")
            {
                SceneManager.LoadScene("WaitingRoomScene");
            }
            // 3. 한 명이라도 거절/시간초과 (lobby로 강제 이동됨)
            else if (wm.currentRoomData.status == "lobby")
            {
                SceneManager.LoadScene("LobbyScene");
            }
        }

        public void ShowResult(bool isWin, float time, List<CombatRecord> stats)
        {
            if (stats == null || stats.Count == 0) return;

            resultTitle.text = isWin ? "VICTORY" : "DEFEAT";
            resultTitle.color = isWin ? new Color(1f, 0.8f, 0f) : Color.red;
            clearTimeText.text = $"Total Play Time: {time:F2}s";
            
            if (winEffect != null) winEffect.SetActive(isWin);
            if (loseEffect != null) loseEffect.SetActive(!isWin);

            // 최대치 계산 (바 그래프 스케일용)
            dpsMaxVal = stats.Max(s => s.totalDamage) + 1f;
            hpsMaxVal = stats.Max(s => s.totalHealing) + 1f;
            tankMaxVal = stats.Max(s => s.totalDamageTaken) + 1f;

            // MVP 연출 (글로우/크게 표시 등)
            var mvp = stats.Find(s => s.isMvp);
            if (mvp != null)
            {
                mvpNicknameText.text = $"[ {mvp.nickname} ]";
                mvpNicknameText.color = GetRoleColor(mvp.role);
                
                if (mvpRoleIcon != null)
                {
                    mvpRoleIcon.color = GetRoleColor(mvp.role);
                }
                
                // MVP 타이틀 텍스트 애니메이션 효과 (스케일 업 등은 에디터에서)
                resultTitle.text = isWin ? "레이드 성공! (VICTORY)" : "레이드 실패 (DEFEAT)";
            }

            // 통계 아이템 순차적 생성 (애니메이션 효과)
            foreach (Transform child in statsContainer) Destroy(child.gameObject);
            StartCoroutine(SpawnStatsItemsSequentially(stats));
        }

        private IEnumerator SpawnStatsItemsSequentially(List<CombatRecord> stats)
        {
            foreach (var s in stats)
            {
                var go = Instantiate(statsItemPrefab, statsContainer);
                var item = go.GetComponent<StatsItem>();
                if (item != null)
                {
                    float max = IsTank(s.role) ? (stats.Max(x => x.totalDamageTaken) + 1f) : 
                                (IsHealer(s.role) ? (stats.Max(x => x.totalHealing) + 1f) : (stats.Max(x => x.totalDamage) + 1f));
                    item.SetData(s, max);
                }
                yield return new WaitForSeconds(0.2f);
            }
        }

        private bool IsTank(string job) => job == "Warrior" || job == "Paladin" || job == "DeathKnight";
        private bool IsHealer(string job) => job == "Priest" || job == "Druid";

        private Color GetRoleColor(string job)
        {
            if (IsTank(job)) return new Color(0.3f, 0.5f, 1f);      // 탱커 Blue
            if (IsHealer(job)) return new Color(0.3f, 1f, 0.3f);    // 힐러 Green
            return new Color(1f, 0.3f, 0.3f);                        // 딜러 Red
        }

        private async void OnRestartRequested()
        {
            Debug.Log("[Result] Host requested rematch.");
            if (WaitingRoomManager.Instance != null)
            {
                await WaitingRoomManager.Instance.RequestRematch();
            }
        }

        private async void OnRematchAccepted() 
        { 
            isRematchTimerActive = false;
            if (rematchRequestPopup != null) rematchRequestPopup.SetActive(false);
            
            if (WaitingRoomManager.Instance != null)
            {
                await WaitingRoomManager.Instance.AcceptRematch();
            }
        }

        private async void OnRematchDeclined() 
        { 
            isRematchTimerActive = false;
            if (rematchRequestPopup != null) rematchRequestPopup.SetActive(false);

            if (WaitingRoomManager.Instance != null)
            {
                await WaitingRoomManager.Instance.DeclineRematch();
            }
        }
    }
}
