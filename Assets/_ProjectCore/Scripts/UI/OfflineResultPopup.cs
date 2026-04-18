using UnityEngine;
using TMPro;
using System;
using BossRaid.Utils;
using BossRaid.Managers; // GrowthManager 및 AdManager 참조용

namespace BossRaid.UI
{
    /// <summary>
    /// ──────────────────────────────────────────────────────────────────
    /// OfflineResultPopup — 오프라인 보상 팝업 (+ 광고 보고 2배 받기 연동)
    /// ──────────────────────────────────────────────────────────────────
    /// </summary>
    public class OfflineResultPopup : MonoBehaviour
    {
        [Header("UI 연결 요소")]
        public GameObject popupPanel;               
        public TextMeshProUGUI timeText;       
        public TextMeshProUGUI rewardAmountText; 
        
        [Header("버튼 연결")]
        public GameObject doubleRewardButton; // 광고 시청 후 버튼을 숨기기 위해 참조

        private double baseRewardAmount; // 저장해둘 기본 보상금액

        private void Awake()
        {
            if (popupPanel != null) popupPanel.SetActive(false);
        }

        public void Show(TimeSpan elapsed, double reward)
        {
            if (popupPanel == null) return;

            // 보상 금액 임시 저장 (광고 2배 지급 시 사용)
            baseRewardAmount = reward;

            string timeStr = "";
            if (elapsed.Days > 0) timeStr += $"{elapsed.Days}일 ";
            if (elapsed.Hours > 0) timeStr += $"{elapsed.Hours}시간 ";
            timeStr += $"{elapsed.Minutes}분";

            if (timeText != null) timeText.text = $"<color=#FFD700>{timeStr}</color> 동안 파티가 열심히 사냥하여";
            if (rewardAmountText != null) rewardAmountText.text = $"+ {reward.ToCurrencyString()} Gold";

            // 팝업 열릴 때 광고 버튼 활성화
            if (doubleRewardButton != null) doubleRewardButton.SetActive(true);

            popupPanel.SetActive(true);
        }

        /// <summary>
        /// (일반) '확인' 버튼 클릭 시 - 추가 보상 없이 창 닫기
        /// </summary>
        public void OnClickClosePopup()
        {
            if (popupPanel != null) popupPanel.SetActive(false);
        }

        /// <summary>
        /// (광고) '광고 보고 2배 획득' 버튼 클릭 시
        /// </summary>
        public void OnClickAdDoubleReward()
        {
            // AdManager에 광고 재생을 요청하고, 다 봤을 때 실행할 람다 함수(Action)를 넘겨줍니다.
            AdManager.Instance.ShowRewardedAd(() => 
            {
                // 보상 지급 (처음에 받은 만큼 한 번 더 지급 = 총 2배)
                GrowthManager.Instance.AddGold(baseRewardAmount);
                
                // UI 텍스트 업데이트 (시각적 피드백)
                if (rewardAmountText != null) 
                {
                    double totalReward = baseRewardAmount * 2;
                    rewardAmountText.text = $"<color=#00FF00>+ {totalReward.ToCurrencyString()} Gold (x2)</color>";
                }

                // 버튼 숨기기 (광고는 1회만 볼 수 있도록)
                if (doubleRewardButton != null) doubleRewardButton.SetActive(false);
            });
        }
    }
}
