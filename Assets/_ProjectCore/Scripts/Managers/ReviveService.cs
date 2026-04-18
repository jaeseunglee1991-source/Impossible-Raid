using UnityEngine;
using System;
using System.Threading.Tasks;
using BossRaid.Combat;

namespace BossRaid.Managers
{
    /// <summary>
    /// ──────────────────────────────────────────────────────────────────
    /// ReviveService  —  1회 한정 유료 부활 트랜잭션 (Supabase 연동)
    /// ──────────────────────────────────────────────────────────────────
    /// ■ 보안 원칙
    ///   클라이언트는 Supabase PostgreSQL RPC를 호출하고,
    ///   서버 응답(트랜잭션 성공)을 받은 뒤에만 부활 연출 및 데이터 갱신 실행.
    ///   클라이언트 단독 재화 조작 불가.
    ///
    /// ■ 부활 공유
    ///   1파티 + 2파티가 보스전 1회당 부활 횟수(1회)를 공유함.
    ///   HasUsedRevive 플래그로 중복 방지.
    /// ──────────────────────────────────────────────────────────────────
    /// </summary>
    public class ReviveService : MonoBehaviour
    {
        public static ReviveService Instance { get; private set; }

        // ──────────────────────────────────────────────
        // 상태
        // ──────────────────────────────────────────────
        /// <summary>보스전 1회당 1회 공유 부활 사용 여부 (1파티+2파티 통합)</summary>
        public bool HasUsedRevive { get; private set; } = false;

        /// <summary>현재 트랜잭션 진행 중 여부 (중복 요청 방지)</summary>
        public bool IsProcessing  { get; private set; } = false;

        // ──────────────────────────────────────────────
        // 설정 (Inspector)
        // ──────────────────────────────────────────────
        [Header("Supabase RPC Settings")]
        [Tooltip("Supabase 프로젝트 URL (예: https://xxxx.supabase.co)")]
        public string supabaseUrl = "https://YOUR_PROJECT.supabase.co";

        [Tooltip("Supabase anon public key")]
        public string supabaseAnonKey = "YOUR_ANON_KEY";

        [Tooltip("PostgreSQL RPC 함수명 (재화 차감 트랜잭션)")]
        public string rpcFunctionName = "deduct_revive_currency";

        [Header("Revive Cost")]
        [Tooltip("부활 소모 유료 재화량")]
        public int reviveCost = 50;

        // ──────────────────────────────────────────────
        // Unity 생명주기
        // ──────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            FixInputSystemError();
        }

        private void FixInputSystemError()
        {
            var eventSystem = FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
            if (eventSystem != null)
            {
                var oldModule = eventSystem.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                if (oldModule != null)
                {
                    DestroyImmediate(oldModule);
                    eventSystem.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                    Debug.Log("<color=green>[Fix] Replaced StandaloneInputModule with InputSystemUIInputModule to resolve Input System conflict.</color>");
                }
            }
        }

        // ──────────────────────────────────────────────
        // 전투 리셋 (새 보스전 시작 시 CombatManager → 호출)
        // ──────────────────────────────────────────────
        public void ResetReviveState()
        {
            HasUsedRevive = false;
            IsProcessing  = false;
            Debug.Log("[ReviveService] 부활 횟수 초기화 완료.");
        }

        // ──────────────────────────────────────────────
        // 부활 팝업 표시 (BattleManager → 호출)
        // ──────────────────────────────────────────────
        /// <summary>파티 전멸 감지 시 BattleManager가 호출 — 부활 확인 팝업 띄우기</summary>
        public void ShowRevivePopup()
        {
            if (HasUsedRevive)
            {
                Debug.LogWarning("[ReviveService] 이미 부활권을 사용했습니다.");
                BattleManager.Instance.CheckGameOver();
                return;
            }

            Debug.Log($"[ReviveService] 부활 팝업 표시 (비용: {reviveCost} 유료재화)");

            // TODO: 실제 UI 팝업 활성화
            // InGameHUDController.Instance?.ShowReviveConfirmPopup(reviveCost, OnReviveConfirmed, OnReviveDeclined);

            // ── 테스트용: 팝업 없이 즉시 부활 확인 처리 ──
#if UNITY_EDITOR
            Debug.Log("[ReviveService] [에디터 테스트] 부활 자동 확인");
            OnReviveConfirmed();
#endif
        }

        // ──────────────────────────────────────────────
        // 부활 확인 / 거절 (UI 버튼 → 연결)
        // ──────────────────────────────────────────────
        /// <summary>플레이어가 '부활' 버튼을 눌렀을 때 HUD에서 호출</summary>
        public async void OnReviveConfirmed()
        {
            if (HasUsedRevive || IsProcessing) return;
            IsProcessing = true;

            Debug.Log("[ReviveService] 🔄 Supabase 트랜잭션 요청 중...");
            // TODO: 로딩 스피너 UI 활성화
            // InGameHUDController.Instance?.ShowLoadingSpinner(true);

            bool success = await ExecuteReviveTransactionAsync();

            // TODO: 로딩 스피너 UI 비활성화
            // InGameHUDController.Instance?.ShowLoadingSpinner(false);
            IsProcessing = false;

            if (success)
            {
                HasUsedRevive = true;
                ApplyReviveEffect();
            }
            else
            {
                Debug.LogWarning("[ReviveService] ❌ 트랜잭션 실패 (재화 부족 또는 통신 오류)");
                // TODO: 실패 메시지 UI (상점 이동 버튼 등)
                // InGameHUDController.Instance?.ShowReviveFailPopup();
                BattleManager.Instance.CheckGameOver();
            }
        }

        /// <summary>플레이어가 '포기' 버튼을 눌렀을 때 HUD에서 호출</summary>
        public void OnReviveDeclined()
        {
            Debug.Log("[ReviveService] 플레이어가 부활을 거절했습니다. 게임오버 처리.");
            BattleManager.Instance.CheckGameOver();
        }

        // ──────────────────────────────────────────────
        // Supabase 트랜잭션 (async/await — 非 메인스레드 안전)
        // ──────────────────────────────────────────────
        /// <summary>
        /// Supabase PostgreSQL RPC 호출:
        ///   서버에서 "유저 보유 재화 검증 → reviveCost 차감 → 이력 로그 INSERT" 를
        ///   하나의 DB 트랜잭션으로 처리하고 결과를 반환받음.
        ///   클라이언트는 성공 응답(HTTP 200 + 성공 플래그)을 받아야만 부활 연출 실행.
        /// </summary>
        private async Task<bool> ExecuteReviveTransactionAsync()
        {
            try
            {
                if (DatabaseManager.Instance == null || DatabaseManager.Instance.SupabaseClient == null)
                {
                    Debug.LogError("[ReviveService] DatabaseManager is not initialized.");
                    return false;
                }

                if (AuthManager.Instance == null || AuthManager.Instance.LocalUser == null)
                {
                    Debug.LogError("[ReviveService] User is not logged in.");
                    return false;
                }

                var payload = new System.Collections.Generic.Dictionary<string, object>
                {
                    { "p_user_id", AuthManager.Instance.LocalUser.id },
                    { "p_cost", reviveCost }
                };

                var response = await DatabaseManager.Instance.SupabaseClient.Rpc(rpcFunctionName, payload);

                if (response != null && !string.IsNullOrEmpty(response.Content))
                {
                    var result = JsonUtility.FromJson<ReviveRpcResult>(response.Content);
                    return result.success;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ReviveService] 트랜잭션 예외: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> CallReviveRpcViaWebRequest()
        {
            // UnityWebRequest는 메인 스레드에서만 생성 가능하므로 Task로 래핑
            var tcs = new TaskCompletionSource<bool>();

            // Unity 메인 스레드에서 코루틴 실행을 위해 MonoBehaviour 활용
            StartCoroutine(ReviveWebRequestCoroutine(tcs));

            return await tcs.Task;
        }

        private System.Collections.IEnumerator ReviveWebRequestCoroutine(TaskCompletionSource<bool> tcs)
        {
            string endpoint = $"{supabaseUrl}/rest/v1/rpc/{rpcFunctionName}";

            // 요청 바디: user_id와 cost를 JSON으로 전송
            string userId = ""; // TODO: AuthManager.Instance.CurrentUserId
            string jsonBody = $"{{\"user_id\":\"{userId}\", \"cost_amount\":{reviveCost}}}";
            byte[] bodyRaw  = System.Text.Encoding.UTF8.GetBytes(jsonBody);

            var request = new UnityEngine.Networking.UnityWebRequest(endpoint, "POST");
            request.uploadHandler   = new UnityEngine.Networking.UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type",  "application/json");
            request.SetRequestHeader("apikey",        supabaseAnonKey);
            request.SetRequestHeader("Authorization", $"Bearer {supabaseAnonKey}");

            yield return request.SendWebRequest();

            if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                // 서버 응답 파싱: {"success": true, "new_balance": 950}
                string responseText = request.downloadHandler.text;
                Debug.Log($"[ReviveService] 서버 응답: {responseText}");

                var result = JsonUtility.FromJson<ReviveRpcResult>(responseText);
                tcs.SetResult(result.success);
            }
            else
            {
                Debug.LogError($"[ReviveService] HTTP 오류: {request.error} ({request.responseCode})");
                tcs.SetResult(false);
            }

            request.Dispose();
        }

        // ──────────────────────────────────────────────
        // 부활 연출 적용 (트랜잭션 성공 시에만 실행)
        // ──────────────────────────────────────────────
        private void ApplyReviveEffect()
        {
            if (BattleManager.Instance == null) return;

            // BattleManager에 위임: 활성 파티 사망 캐릭터들 HP/MP 100% 복구
            if(BossRaid.Combat.CombatManager.Instance != null) BossRaid.Combat.CombatManager.Instance.ReviveAllPlayers();

            Debug.Log("<color=cyan>[ReviveService] ✅ 부활 완료! 전투 속행.</color>");

            // TODO: 부활 이펙트/연출 재생
            // EffectManager.Instance?.PlayReviveEffect();
        }

        // ──────────────────────────────────────────────
        // 직렬화 모델 (서버 RPC 응답)
        // ──────────────────────────────────────────────
        [Serializable]
        private class ReviveRpcResult
        {
            /// <summary>서버에서 트랜잭션 성공 여부</summary>
            public bool success;
            /// <summary>차감 후 남은 유료 재화 잔량</summary>
            public int new_balance;
        }

        // ──────────────────────────────────────────────
        // (참고용) Supabase에 생성할 PostgreSQL RPC 뼈대
        // ──────────────────────────────────────────────
        /*
        CREATE OR REPLACE FUNCTION deduct_revive_currency(
            p_user_id   UUID,
            p_cost      INT
        )
        RETURNS JSON
        LANGUAGE plpgsql
        SECURITY DEFINER  -- 서버 권한으로 실행 (클라이언트 직접 조작 불가)
        AS $$
        DECLARE
            v_balance   INT;
            v_new_bal   INT;
        BEGIN
            -- 1. 잔액 확인 (FOR UPDATE로 동시 요청 잠금)
            SELECT premium_currency INTO v_balance
            FROM user_profiles
            WHERE id = p_user_id
            FOR UPDATE;

            IF v_balance IS NULL THEN
                RETURN json_build_object('success', false, 'reason', 'user_not_found');
            END IF;

            IF v_balance < p_cost THEN
                RETURN json_build_object('success', false, 'reason', 'insufficient_currency');
            END IF;

            -- 2. 재화 차감
            v_new_bal := v_balance - p_cost;
            UPDATE user_profiles
            SET premium_currency = v_new_bal
            WHERE id = p_user_id;

            -- 3. 이력 로그
            INSERT INTO currency_logs (user_id, change_amount, reason, created_at)
            VALUES (p_user_id, -p_cost, 'boss_revive', NOW());

            -- 4. 성공 반환
            RETURN json_build_object(
                'success',     true,
                'new_balance', v_new_bal
            );
        END;
        $$;
        */
    }
}
