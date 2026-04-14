using UnityEngine;
using TMPro;
using BossRaid.Models;

namespace BossRaid.UI
{
    public class StatsItem : MonoBehaviour
    {
        public TMP_Text nicknameText;
        public TMP_Text roleText;
        public TMP_Text valueText; // 메인 수치
        public TMP_Text subValueText; // 보조 수치 (탱커용 어그로 등)
        public UnityEngine.UI.Image roleIcon;
        public UnityEngine.UI.Image fillBar;
        public GameObject mvpBadge;

        public void SetData(CombatRecord record, float maxValue)
        {
            nicknameText.text = record.nickname;
            roleText.text = GetKoreanRole(record.role);
            
            // 역할별 색상 설정
            Color roleColor = GetRoleColor(record.role);
            if (roleIcon != null) roleIcon.color = roleColor;
            if (fillBar != null) fillBar.color = new Color(roleColor.r, roleColor.g, roleColor.b, 0.8f);

            float mainVal = 0;
            string label = "";

            if (IsTank(record.role)) 
            { 
                mainVal = record.totalDamageTaken; 
                label = "피해"; 
                if (subValueText != null) subValueText.text = $"[어그로 {record.aggroDuration:F0}s]"; 
            }
            else if (IsHealer(record.role)) 
            { 
                mainVal = record.totalHealing; 
                label = "힐"; 
                if (subValueText != null) subValueText.text = ""; 
            }
            else 
            { 
                mainVal = record.totalDamage; 
                label = "딜"; 
                if (subValueText != null) subValueText.text = ""; 
            }

            valueText.text = $"<b>{label}</b> {mainVal:N0}";
            fillBar.fillAmount = maxValue > 0 ? mainVal / maxValue : 0;
            mvpBadge.SetActive(record.isMvp);
        }

        private Color GetRoleColor(string job)
        {
            if (IsTank(job)) return new Color(0.3f, 0.5f, 1f);      // 탱커 Blue
            if (IsHealer(job)) return new Color(0.3f, 1f, 0.3f);    // 힐러 Green
            return new Color(1f, 0.3f, 0.3f);                        // 딜러 Red
        }

        private bool IsTank(string job) => job == "Warrior" || job == "Paladin" || job == "DeathKnight";
        private bool IsHealer(string job) => job == "Priest" || job == "Druid";
        
        private string GetKoreanRole(string job)
        {
            switch(job)
            {
                case "Warrior": return "전사";
                case "Paladin": return "성기사";
                case "Rogue": return "도적";
                case "Ranger": return "레인저";
                case "FireMage": return "화염법사";
                case "IceMage": return "냉기법사";
                case "Warlock": return "흑마법사";
                case "Priest": return "사제";
                case "Druid": return "드루이드";
                case "DeathKnight": return "죽기";
                default: return job;
            }
        }
    }
}
