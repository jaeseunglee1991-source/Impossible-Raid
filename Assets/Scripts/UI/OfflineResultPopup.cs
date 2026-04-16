using UnityEngine;
using TMPro; // TextMeshPro 지원
using System;
using BossRaid.Utils; // CurrencyFormatter 사용

namespace BossRaid.UI
{
    /// <summary>
    /// ──────────────────────────────────────────────────────────────────
    /// OfflineResultPopup — 오프라인 복귀 시 획득한 재화를 보여주는 UI 팝업
    /// ──────────────────────────────────────────────────────────────────
    /// </summary>
    public class OfflineResultPopup : MonoBehaviour
    {
        [Header("UI 연결 요소")]
        [Tooltip("팝업 창의 전체 배경/부모 패널 오브젝트")]
        public GameObject popupPanel;               
        
        [Tooltip("경과 시간을 표시할 텍스트 (예: '1시간 20분')")]
        public TextMeshProUGUI timeText;       
        
        [Tooltip("획득한 골드를 표시할 텍스트 (예: '+ 1.50M Gold')")]
        public TextMeshProUGUI rewardAmountText; 

        private void Awake()
        {
            // 게임 시작 시 팝업이 켜져있으면 자동으로 끕니다.
            if (popupPanel != null)
            {
                popupPanel.SetActive(false);
            }
        }

        /// <summary>
        /// OfflineRewardManager에서 계산을 마치고 이 함수를 호출하여 팝업을 띄웁니다.
        /// </summary>
        /// <param name="elapsed">오프라인 경과 시간</param>
        /// <param name="reward">획득한 총 골드량</param>
        public void Show(TimeSpan elapsed, double reward)
        {
            if (popupPanel == null) return;

            // 시간 문자열 포맷팅
            string timeStr = "";
            if (elapsed.Days > 0) timeStr += $"{elapsed.Days}일 ";
            if (elapsed.Hours > 0) timeStr += $"{elapsed.Hours}시간 ";
            timeStr += $"{elapsed.Minutes}분";

            // UI 텍스트 적용 (색상 태그 포함)
            if (timeText != null)
            {
                timeText.text = $"<color=#FFD700>{timeStr}</color> 동안 파티가 열심히 사냥하여";
            }

            if (rewardAmountText != null)
            {
                // ToCurrencyString()은 이전에 추가한 CurrencyFormatter.cs의 확장 메서드입니다.
                rewardAmountText.text = $"+ {reward.ToCurrencyString()} Gold";
            }

            // 팝업 활성화
            popupPanel.SetActive(true);
        }

        /// <summary>
        /// 팝업의 [확인] 버튼 클릭 시 연결할 함수입니다.
        /// </summary>
        public void OnClickClosePopup()
        {
            if (popupPanel != null)
            {
                popupPanel.SetActive(false);
            }
        }
    }
}
