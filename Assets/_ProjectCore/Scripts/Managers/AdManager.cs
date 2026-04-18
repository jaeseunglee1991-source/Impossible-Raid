using UnityEngine;
using UnityEngine.Advertisements;
using System;

namespace BossRaid.Managers
{
    /// <summary>
    /// ──────────────────────────────────────────────────────────────────
    /// AdManager — Unity Ads(보상형 광고) 초기화 및 재생 매니저
    /// ──────────────────────────────────────────────────────────────────
    /// </summary>
    public class AdManager : MonoBehaviour, IUnityAdsInitializationListener, IUnityAdsLoadListener, IUnityAdsShowListener
    {
        public static AdManager Instance { get; private set; }

        [Header("광고 설정")]
        [Tooltip("Unity Dashboard에서 발급받은 Android Game ID를 입력하세요.")]
        public string androidGameId = "1234567"; // 테스트용 ID. 실제 출시 시 변경 필요
        [Tooltip("테스트 모드 활성화 여부 (출시할 때는 false로 변경)")]
        public bool testMode = true;

        private string rewardedAdUnitId = "Rewarded_Android";
        private Action onRewardCallback; // 광고 시청 완료 후 실행될 함수를 담아두는 델리게이트

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                InitializeAds();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void InitializeAds()
        {
            if (!Advertisement.isInitialized && Advertisement.isSupported)
            {
                Advertisement.Initialize(androidGameId, testMode, this);
            }
        }

        // 초기화 성공 시 자동 로드
        public void OnInitializationComplete()
        {
            Debug.Log("[AdManager] Unity Ads 초기화 성공.");
            LoadAd();
        }

        public void OnInitializationFailed(UnityAdsInitializationError error, string message)
        {
            Debug.LogError($"[AdManager] 초기화 실패: {error} - {message}");
        }

        // 광고 로드 (미리 메모리에 올려둠)
        public void LoadAd()
        {
            Advertisement.Load(rewardedAdUnitId, this);
        }

        public void OnUnityAdsAdLoaded(string adUnitId)
        {
            Debug.Log($"[AdManager] 광고 로드 완료: {adUnitId}");
        }

        public void OnUnityAdsFailedToLoad(string adUnitId, UnityAdsLoadError error, string message)
        {
            Debug.LogError($"[AdManager] 광고 로드 실패: {error} - {message}");
        }

        /// <summary>
        /// 다른 스크립트(UI 버튼 등)에서 이 함수를 호출하여 광고를 봅니다.
        /// </summary>
        /// <param name="onSuccess">광고 시청 완료 후 지급할 보상 로직</param>
        public void ShowRewardedAd(Action onSuccess)
        {
            onRewardCallback = onSuccess;

            if (Advertisement.isInitialized)
            {
                Advertisement.Show(rewardedAdUnitId, this);
            }
            else
            {
                Debug.LogWarning("[AdManager] 광고가 아직 초기화되지 않았습니다.");
                // 에러 방지를 위해 실패 시 기본 보상 지급 처리 (선택사항)
                // onRewardCallback?.Invoke(); 
            }
        }

        // --- 광고 시청 결과 콜백 ---
        public void OnUnityAdsShowComplete(string adUnitId, UnityAdsShowCompletionState showCompletionState)
        {
            if (adUnitId.Equals(rewardedAdUnitId) && showCompletionState == UnityAdsShowCompletionState.COMPLETED)
            {
                Debug.Log("[AdManager] 유저가 보상형 광고를 끝까지 시청했습니다. 보상을 지급합니다.");
                
                // 기다리고 있던 보상 지급 로직 실행
                onRewardCallback?.Invoke();
                
                // 다음 광고를 위해 다시 로드
                LoadAd(); 
            }
            else if (showCompletionState == UnityAdsShowCompletionState.SKIPPED)
            {
                Debug.Log("[AdManager] 유저가 광고를 스킵했습니다. 보상 미지급.");
                LoadAd();
            }
        }

        public void OnUnityAdsShowFailure(string adUnitId, UnityAdsShowError error, string message)
        {
            Debug.LogError($"[AdManager] 광고 재생 중 에러 발생: {error} - {message}");
            LoadAd();
        }

        public void OnUnityAdsShowStart(string adUnitId) { }
        public void OnUnityAdsShowClick(string adUnitId) { }
    }
}
