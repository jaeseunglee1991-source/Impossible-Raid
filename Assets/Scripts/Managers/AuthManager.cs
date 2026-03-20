using UnityEngine;
using Supabase;
using System.Threading.Tasks;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Collections.Generic;
using BossRaid.Models;
using Supabase.Postgrest;
using Supabase.Gotrue;

namespace BossRaid.Managers
{
    public class AuthManager : MonoBehaviour
    {
        public static AuthManager Instance { get; private set; }
        public UserRecord LocalUser { get; private set; }

        /// <summary>가장 최근 인증 실패 메시지 (UI 팝업에서 사용)</summary>
        public string LastError { get; private set; } = string.Empty;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this.gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(this.gameObject);
        }

        public async Task<bool> SignUp(string email, string password, string nickname)
        {
            // DatabaseManager 초기화 체크
            if (DatabaseManager.Instance == null || DatabaseManager.Instance.Client == null)
            {
                LastError = "데이터베이스 연결이 초기화되지 않았습니다.\n씬에 DatabaseManager가 있는지 확인해 주세요.";
                Debug.LogError("[AuthManager] DatabaseManager is not initialized.");
                return false;
            }

            try
            {
                string hashedPassword = HashPassword(password);
                var session = await DatabaseManager.Instance.Client.Auth.SignUp(email, hashedPassword);
                
                if (session != null && session.User != null)
                {
                    var profile = new UserRecord
                    {
                        id = session.User.Id,
                        nickname = nickname,
                        total_plays = 0,
                        boss_kills = 0,
                        max_stage_reached = 0
                    };

                    await DatabaseManager.Instance.Client.From<UserRecord>().Insert(profile);
                    Debug.Log($"[AuthManager] Profile created for {nickname}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                // 이메일 중복 여부 파싱
                string msg = ex.Message.ToLower();
                if (msg.Contains("already registered") || msg.Contains("duplicate") || msg.Contains("unique"))
                    LastError = "이미 사용 중인 이메일입니다.";
                else if (msg.Contains("email_address_invalid") || msg.Contains("invalid email"))
                    LastError = "유효하지 않은 이메일 형식입니다.";
                else
                    LastError = "회원가입 실패.\n잠시 후 다시 시도해 주세요.";

                Debug.LogError($"[AuthManager] Sign up failed: {ex.Message}");
            }
            return false;
        }

        public async Task<bool> SignIn(string email, string password)
        {
            LastError = string.Empty;

            // DatabaseManager 초기화 체크
            if (DatabaseManager.Instance == null || DatabaseManager.Instance.Client == null)
            {
                LastError = "데이터베이스 연결이 초기화되지 않았습니다.\n씬에 DatabaseManager가 있는지 확인해 주세요.";
                Debug.LogError("[AuthManager] DatabaseManager is not initialized.");
                return false;
            }

            if (IsLockedOut(email))
            {
                var remaining = lockoutTimes[email] - DateTime.Now;
                LastError = $"로그인 시도가 너무 많습니다.\n{(int)remaining.TotalSeconds}초 후 다시 시도해 주세요.";
                Debug.LogWarning($"[AuthManager] Account locked: {email}");
                return false;
            }

            try
            {
                string hashedPassword = HashPassword(password);
                var session = await DatabaseManager.Instance.Client.Auth.SignIn(email, hashedPassword);
                
                if (session != null && session.User != null)
                {
                    var response = await DatabaseManager.Instance.Client.From<UserRecord>()
                        .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, session.User.Id)
                        .Get();

                    if (response.Models.Count > 0)
                    {
                        LocalUser = response.Models[0];
                        Debug.Log($"[AuthManager] Welcome back! {LocalUser.nickname}");
                    }
                    
                    loginFailures[email] = 0;
                    return true;
                }
            }
            catch (Exception ex)
            {
                // 상세 오류 파싱
                string msg = ex.Message.ToLower();
                if (msg.Contains("timeout") || msg.Contains("network") || msg.Contains("reach"))
                    LastError = "서버 연결 시간이 초과되었습니다.\n인터넷 연결 상태를 확인해 주세요.";
                else
                    LastError = "로그인 실패. ID 혹은 비밀번호를 확인해 주세요.";
                    
                HandleLoginFailure(email, ex.Message);
            }
            return false;
        }

        public async Task<bool> SignInAsGuest()
        {
            LastError = string.Empty;
            if (DatabaseManager.Instance == null || DatabaseManager.Instance.Client == null)
            {
                LastError = "데이터베이스 연결이 초기화되지 않았습니다.";
                return false;
            }

            try
            {
                var session = await DatabaseManager.Instance.Client.Auth.SignInAnonymously();
                if (session != null && session.User != null)
                {
                    return await EnsureUserProfile(session.User.Id, "Guest Player");
                }
            }
            catch (Exception ex)
            {
                string msg = ex.Message.ToLower();
                if (msg.Contains("anonymous_provider_disabled"))
                    LastError = "Supabase 설정에서 'Anonymous Sign-ins'를 활성화해야 합니다.\n(Authentication -> Providers -> Anonymous)";
                else
                    LastError = "게스트 로그인 실패. 다시 시도해 주세요.";
                
                Debug.LogError($"[AuthManager] Guest login failed: {ex.Message}");
            }
            return false;
        }

        public async Task<bool> SignInWithGoogle()
        {
            LastError = string.Empty;
            if (DatabaseManager.Instance == null || DatabaseManager.Instance.Client == null)
            {
                LastError = "데이터베이스 연결이 초기화되지 않았습니다.";
                return false;
            }

            try
            {
                // Supabase.Gotrue.Provider.Google (혹은 라이브러리 버전에 따라 다른 위치)
                // 이 방식은 웹 브라우저를 엽니다. Native SDK 연동 시에는 별도 처리가 필요합니다.
                await DatabaseManager.Instance.Client.Auth.SignIn(Supabase.Gotrue.Constants.Provider.Google);
                
                // OAuth의 경우 리다이렉트 처리 후 세션이 유효해지면 호출됩니다.
                // 여기서는 리턴 true를 하지만, 실제 데이터 로드는 온보딩/리다이렉트 처리부에서 수행해야 할 수도 있습니다.
                return true; 
            }
            catch (Exception ex)
            {
                LastError = "구글 로그인 시도 중 오류가 발생했습니다.";
                Debug.LogError($"[AuthManager] Google login failed: {ex.Message}");
            }
            return false;
        }

        private async Task<bool> EnsureUserProfile(string userId, string defaultNickname)
        {
            try
            {
                var response = await DatabaseManager.Instance.Client.From<UserRecord>()
                    .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, userId)
                    .Get();

                if (response.Models.Count > 0)
                {
                    LocalUser = response.Models[0];
                    Debug.Log($"[AuthManager] Welcome back! {LocalUser.nickname}");
                    return true;
                }
                else
                {
                    // 프로필이 없으면 생성
                    var newProfile = new UserRecord
                    {
                        id = userId,
                        nickname = $"{defaultNickname}#{userId.Substring(0, 4)}",
                        total_plays = 0,
                        boss_kills = 0,
                        max_stage_reached = 0
                    };

                    await DatabaseManager.Instance.Client.From<UserRecord>().Insert(newProfile);
                    LocalUser = newProfile;
                    Debug.Log($"[AuthManager] New profile created: {newProfile.nickname}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                LastError = "프로필 정보 동기화 실패. 다시 로그인해 보세요.";
                Debug.LogError($"[AuthManager] Profile sync failed: {ex.Message}");
                return false;
            }
        }

        [Header("Security Settings")]
        public int maxLoginFailures = 5;
        public int lockoutDurationMinutes = 3;
        private Dictionary<string, int> loginFailures = new Dictionary<string, int>();
        private Dictionary<string, DateTime> lockoutTimes = new Dictionary<string, DateTime>();

        private bool IsLockedOut(string email)
        {
            if (lockoutTimes.ContainsKey(email))
            {
                if (DateTime.Now < lockoutTimes[email]) return true;
                else lockoutTimes.Remove(email);
            }
            return false;
        }

        private void HandleLoginFailure(string email, string errorMessage)
        {
            if (!loginFailures.ContainsKey(email)) loginFailures[email] = 0;
            loginFailures[email]++;
            if (loginFailures[email] >= maxLoginFailures)
                lockoutTimes[email] = DateTime.Now.AddMinutes(lockoutDurationMinutes);
            Debug.LogError($"[AuthManager] Login failed: {errorMessage}");
        }

        private string HashPassword(string password)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++) builder.Append(bytes[i].ToString("x2"));
                return builder.ToString();
            }
        }
    }
}
