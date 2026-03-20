using UnityEngine;

namespace BossRaid.Testing
{
    /// <summary>
    /// 로그인 화면 기능을 Unity Editor의 Inspector에서 테스트하기 위한 헬퍼.
    /// LoginUIController가 있는 게임오브젝트에 붙인 뒤,
    /// Play Mode에서 Inspector > Context Menu를 통해 각 테스트를 실행하세요.
    /// </summary>
    [RequireComponent(typeof(BossRaid.UI.LoginUIController))]
    public class LoginTestValidator : MonoBehaviour
    {
        [Header("Test Credentials")]
        [Tooltip("테스트할 이메일 주소 (실제 Supabase 계정 또는 새로 가입할 이메일)")]
        public string testEmail = "test@example.com";
        [Tooltip("테스트할 비밀번호 (6자 이상)")]
        public string testPassword = "password123";
        [Tooltip("테스트 닉네임")]
        public string testNickname = "TestUser";

        private BossRaid.UI.LoginUIController _controller;

        private void Awake()
        {
            _controller = GetComponent<BossRaid.UI.LoginUIController>();
        }

        // ─────────────────────────────────────────
        // Inspector Context Menu 테스트 (New Flow)
        // ─────────────────────────────────────────

        [ContextMenu("Test/01 - 게스트 로그인 시도")]
        private void Test_GuestLogin()
        {
            Log("게스트 로그인 테스트 시작...");
            if (_controller.guestLoginButton != null)
                _controller.guestLoginButton.onClick.Invoke();
            else
                LogError("Guest Login Button이 연결되지 않았습니다.");
        }

        [ContextMenu("Test/02 - 구글 로그인 시도")]
        private void Test_GoogleLogin()
        {
            Log("구글 로그인 테스트 시작...");
            if (_controller.googleLoginButton != null)
                _controller.googleLoginButton.onClick.Invoke();
            else
                LogError("Google Login Button이 연결되지 않았습니다.");
        }

        [ContextMenu("Test/03 - 팝업 수동 표시")]
        private void Test_ManualPopup()
        {
            _controller.ShowPopup("자동 설정 및 개편이 완료되었습니다!\n이제 에러 없이 작동합니다.", autoClose: true);
        }

        private void Log(string msg) => Debug.Log($"<color=cyan>[LoginTestValidator]</color> {msg}");
        private void LogError(string msg) => Debug.LogError($"<color=red>[LoginTestValidator]</color> {msg}");
    }
}
