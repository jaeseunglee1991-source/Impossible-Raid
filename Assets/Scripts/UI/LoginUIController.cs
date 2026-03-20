using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using BossRaid.Managers;
using System.Collections;

namespace BossRaid.UI
{
    /// <summary>
    /// 로그인 화면의 모든 UI 상호작용을 관리합니다.
    /// - 로그인 / 회원가입 패널 전환
    /// - 클라이언트 측 유효성 검사
    /// - 로딩 상태 표시
    /// - 팝업 메시지 표시
    /// - 로그인 성공 시 로비 씬으로 전환
    /// </summary>
    public class LoginUIController : MonoBehaviour
    {
        // ─────────────────────────────────────────────
        //  로그인 패널 (새 디자인)
        // ─────────────────────────────────────────────
        [Header("Modern Login Panel")]
        public GameObject loginPanel;
        public Button googleLoginButton;
        public Button guestLoginButton;
        public TMP_Text loadingStatusText; // "로그인 중..." 등을 표시할 별도 텍스트 (선택)

        // (기존 필드는 하위 호환성을 위해 남겨두거나 주석 처리할 수 있습니다. 
        // 여기서는 깔끔하게 새 버튼 중심으로 전환합니다.)

        // ─────────────────────────────────────────────
        //  팝업
        // ─────────────────────────────────────────────
        [Header("Popup")]
        public GameObject popupPanel;
        public TMP_Text popupText;
        public Button popupCloseButton;

        // ─────────────────────────────────────────────
        //  씬 전환
        // ─────────────────────────────────────────────
        [Header("Scene Settings")]
        public string lobbySceneName = "LobbyScene";

        // ─────────────────────────────────────────────
        //  상태 관리
        // ─────────────────────────────────────────────
        private bool _isProcessing = false;

        // ─────────────────────────────────────────────
        //  초기화
        // ─────────────────────────────────────────────
        private void Start()
        {
            // 리스너 중복 방지를 위해 초기화 시 명시적으로 정리
            if (popupCloseButton != null)
            {
                popupCloseButton.onClick.RemoveAllListeners();
                popupCloseButton.onClick.AddListener(ClosePopup);
            }

            // [중요] 빌더에서 등록한 Persistent Listener만 사용하도록 런타임 중복 등록 제거
            if (loginPanel != null) loginPanel.SetActive(true);
            
            // 시작 시 모든 팝업 닫기
            if (popupPanel != null) popupPanel.SetActive(false);
        }

        // ─────────────────────────────────────────────
        //  구글 로그인
        // ─────────────────────────────────────────────
        public async void OnGoogleLoginClicked()
        {
            if (_isProcessing) return;
            _isProcessing = true;

            SetLoadingState(true, "Google 로그인 시도 중...");
            bool success = await AuthManager.Instance.SignInWithGoogle();
            
            _isProcessing = false;
            
            if (!success)
            {
                SetLoadingState(false);
                ShowPopup(AuthManager.Instance.LastError ?? "Google 로그인 실패");
            }
        }

        // ─────────────────────────────────────────────
        //  게스트 로그인
        // ─────────────────────────────────────────────
        public async void OnGuestLoginClicked()
        {
            if (_isProcessing) 
            {
                Debug.Log("[LoginUI] Already processing, ignoring click.");
                return;
            }
            _isProcessing = true;

            SetLoadingState(true, "게스트 로그인 중...");
            try
            {
                bool success = await AuthManager.Instance.SignInAsGuest();
                SetLoadingState(false);

                if (success)
                {
                    ShowPopup("게스트 로그인 성공!\n데이터가 기기에 저장됩니다.", autoClose: true, onClose: GoToLobby);
                }
                else
                {
                    ShowPopup(AuthManager.Instance.LastError ?? "게스트 로그인 실패");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[LoginUIController] Guest Login Error: {ex.Message}");
                SetLoadingState(false);
                ShowPopup("시스템 오류가 발생했습니다.\n서버 상태를 확인해 주세요.");
            }
            finally
            {
                _isProcessing = false;
            }
        }

        private void SetLoadingState(bool isLoading, string status = "")
        {
            if (googleLoginButton != null) googleLoginButton.interactable = !isLoading;
            if (guestLoginButton != null) guestLoginButton.interactable = !isLoading;
            
            if (loadingStatusText != null)
            {
                loadingStatusText.gameObject.SetActive(isLoading);
                loadingStatusText.text = status;
            }
        }

        // ─────────────────────────────────────────────
        //  팝업 관리
        // ─────────────────────────────────────────────
        private System.Action _onPopupClose;

        public void ShowPopup(string message, bool autoClose = false, System.Action onClose = null)
        {
            // 팝업이 이미 떠 있더라도 텍스트를 교체하고 최상단으로 유지
            if (popupText != null) popupText.text = message;
            if (popupPanel != null) popupPanel.SetActive(true);
            
            _onPopupClose = onClose;

            if (autoClose)
            {
                StopAllCoroutines();
                StartCoroutine(AutoClosePopup(2.0f));
            }
        }

        private IEnumerator AutoClosePopup(float delay)
        {
            yield return new WaitForSeconds(delay);
            ClosePopup();
        }

        public void ClosePopup()
        {
            Debug.Log("[LoginUIController] ClosePopup Called.");
            if (popupPanel != null) popupPanel.SetActive(false);
            
            // 팝업이 닫힐 때 로딩 상태도 함께 해제 (안전 장치)
            SetLoadingState(false);
            _isProcessing = false;

            var callback = _onPopupClose;
            _onPopupClose = null; 
            callback?.Invoke();
        }

        // ─────────────────────────────────────────────
        //  씬 전환
        // ─────────────────────────────────────────────
        public void GoToLobby()
        {
            Debug.Log($"[LoginUIController] Loading scene: {lobbySceneName}");
            SceneManager.LoadScene(lobbySceneName);
        }
    }
}
