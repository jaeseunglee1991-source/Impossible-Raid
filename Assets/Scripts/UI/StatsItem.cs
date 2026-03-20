using UnityEngine;
using TMPro;
using BossRaid.Models;

namespace BossRaid.UI
{
    public class StatsItem : MonoBehaviour
    {
        public TMP_Text nicknameText;
        public TMP_Text roleText;
        public TMP_Text damageText;
        public TMP_Text healingText;
        public TMP_Text tankingText;
        public GameObject mvpBadge;

        public void SetData(CombatRecord record)
        {
            nicknameText.text = record.nickname;
            roleText.text = record.role;
            damageText.text = record.totalDamage.ToString("N0");
            healingText.text = record.totalHealing.ToString("N0");
            tankingText.text = record.totalDamageTaken.ToString("N0");
            mvpBadge.SetActive(record.isMvp);
        }
    }
}
