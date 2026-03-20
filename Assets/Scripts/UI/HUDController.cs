using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BossRaid.Combat;

namespace BossRaid.UI
{
    public class HUDController : MonoBehaviour
    {
        [Header("Player Status")]
        public Slider healthBar;
        public TMP_Text nicknameText;
        
        [Header("Skill Cooldowns")]
        public Image[] skillIcons; // 3개 스킬 + 1개 궁극기 + 1개 회피기
        public Image[] cooldownOverlays;
        public TMP_Text[] cooldownTexts;

        [Header("Boss Status")]
        public Slider bossHealthBar;
        public TMP_Text bossNameText;

        [Header("Game Info")]
        public TMP_Text timerText;

        public void Initialize(CharacterBase player, string bossName)
        {
            nicknameText.text = player.characterName;
            bossNameText.text = bossName;
        }

        public void UpdateHUD(CharacterBase player, float currentBossHealth, float maxBossHealth, float remainingTime)
        {
            healthBar.value = player.currentHealth / player.maxHealth;
            bossHealthBar.value = currentBossHealth / maxBossHealth;
            
            timerText.text = $"{(int)remainingTime / 60:D2}:{(int)remainingTime % 60:D2}";

            // 쿨다운 업데이트 로직 (간소화)
            // for (int i = 0; i < player.skills.Count; i++) { ... }
        }
    }
}
