using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using BossRaid.Combat;
using BossRaid.Managers;
using TMPro;

namespace BossRaid.UI
{
    /// <summary>
    /// 상업용 방치형 게임의 필수 요소인 '실시간 딜미터기 (DPS Meter)' 스크립트입니다.
    /// 파티원들의 누적 데미지와 DPS를 실시간으로 계산하여 막대그래프로 보여줍니다.
    /// </summary>
    public class DPSMeterUI : MonoBehaviour
    {
        [System.Serializable]
        public class DPSBar
        {
            public CharacterBase character;
            public Image fillBar;         // 데미지 비례 게이지 (0~1)
            public TextMeshProUGUI nameText;
            public TextMeshProUGUI dpsText; // 예: "1,250K (초당 50K)"
        }

        [Header("Settings")]
        public float updateInterval = 0.5f; // 0.5초마다 갱신 (최적화)
        private float _timer = 0f;
        private float _battleStartTime = 0f;

        [Header("UI References")]
        [Tooltip("파티원 수(4명)에 맞춰 막대그래프 UI를 연결해주세요.")]
        public List<DPSBar> dpsBars = new List<DPSBar>();
        
        [Tooltip("총 파티 누적 데미지 텍스트")]
        public TextMeshProUGUI totalPartyDamageText;

        private void OnEnable()
        {
            _battleStartTime = Time.time;
            _timer = 0f;
        }

        private void Update()
        {
            if (BattleManager.Instance == null || BattleManager.Instance.ActiveCharacters.Count == 0) return;

            _timer += Time.deltaTime;
            if (_timer >= updateInterval)
            {
                UpdateMeter();
                _timer = 0f;
            }
        }

        private void UpdateMeter()
        {
            float currentBattleDuration = Time.time - _battleStartTime;
            if (currentBattleDuration <= 0f) currentBattleDuration = 1f;

            // 1. 현재 파티원 중 가장 데미지를 많이 넣은 값 (1등) 찾기 (그래프 비율 계산용)
            float maxDamage = 0f;
            float totalPartyDamage = 0f;

            var activeChars = BattleManager.Instance.ActiveCharacters
                .Where(c => c.characterBase != null)
                .Select(c => c.characterBase)
                .OrderByDescending(c => c.totalDamageDealt) // 데미지 1등부터 순서대로 정렬
                .ToList();

            foreach (var c in activeChars)
            {
                if (c.totalDamageDealt > maxDamage) maxDamage = c.totalDamageDealt;
                totalPartyDamage += c.totalDamageDealt;
            }

            // 전체 파티 데미지 표기
            if (totalPartyDamageText != null)
            {
                totalPartyDamageText.text = $"파티 총 피해량: {FormatNumber(totalPartyDamage)}";
            }

            // 2. 최대 4명의 UI 바 업데이트
            for (int i = 0; i < dpsBars.Count; i++)
            {
                if (i < activeChars.Count)
                {
                    dpsBars[i].character = activeChars[i];
                    CharacterBase c = activeChars[i];

                    // 이름
                    if (dpsBars[i].nameText != null)
                        dpsBars[i].nameText.text = c.characterName;

                    // 상태 및 수치
                    float dps = c.totalDamageDealt / currentBattleDuration;
                    if (dpsBars[i].dpsText != null)
                    {
                        dpsBars[i].dpsText.text = $"{FormatNumber(c.totalDamageDealt)} <size=80%>(DPS: {FormatNumber(dps)})</size>";
                    }

                    // 게이지 바 채우기 (1등을 기준으로 상대적 비율)
                    if (dpsBars[i].fillBar != null)
                    {
                        float fillAmount = maxDamage > 0 ? (c.totalDamageDealt / maxDamage) : 0;
                        // 부드러운 애니메이션 효과
                        dpsBars[i].fillBar.fillAmount = Mathf.Lerp(dpsBars[i].fillBar.fillAmount, fillAmount, 0.2f);
                        
                        // 1등은 금색, 나머지는 일반 색상 처리 등 상업적 폴리싱
                        dpsBars[i].fillBar.color = (i == 0) ? new Color(1f, 0.84f, 0f) : new Color(0.8f, 0.2f, 0.2f);
                    }
                }
                else
                {
                    // 활성화된 캐릭터가 부족하면 바 숨기기
                    if (dpsBars[i].fillBar != null) dpsBars[i].fillBar.fillAmount = 0;
                    if (dpsBars[i].nameText != null) dpsBars[i].nameText.text = "";
                    if (dpsBars[i].dpsText != null) dpsBars[i].dpsText.text = "";
                }
            }
        }

        /// <summary>
        /// K, M, B 등 방치형 게임 특유의 단위 포맷팅 (1,234K 등)
        /// </summary>
        private string FormatNumber(float number)
        {
            if (number >= 1000000000) return (number / 1000000000D).ToString("0.##") + "B";
            if (number >= 1000000)    return (number / 1000000D).ToString("0.##") + "M";
            if (number >= 1000)       return (number / 1000D).ToString("0.##") + "K";
            
            return number.ToString("0");
        }
    }
}
