using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BossRaid.UI
{
    /// <summary>
    /// 개별 스킬 버튼 UI 컴포넌트
    /// 쿨다운 표시, 차단 스킬 글로우, 클릭 이벤트 처리
    /// </summary>
    public class SkillButtonUI : MonoBehaviour
    {
        [Header("References")]
        public Button button;
        public Image iconImage;
        public Image cooldownOverlay;       // 쿨다운 시 어둡게 덮는 오버레이
        public TextMeshProUGUI cooldownText;
        public Image glowEffect;            // 차단 스킬 발광 효과
        public TextMeshProUGUI keyText;     // 단축키 표시 (Q, W, E, R, Space)

        [Header("State")]
        public float maxCooldown = 10f;
        public float currentCooldown = 0f;
        public bool isInterruptSkill = false;   // 차단 스킬 여부

        private int skillIndex;
        private System.Action<int> onSkillUsed;

        public void Initialize(int index, string keyLabel, bool isInterrupt, System.Action<int> callback)
        {
            skillIndex = index;
            isInterruptSkill = isInterrupt;
            onSkillUsed = callback;

            if (keyText != null) {
                keyText.text = keyLabel;
                keyText.color = new Color(1f, 1f, 1f, 0.8f);
                keyText.outlineWidth = 0.3f;
                keyText.outlineColor = Color.black;
            }
            if (glowEffect != null) glowEffect.gameObject.SetActive(false);
            if (cooldownOverlay != null) {
                cooldownOverlay.fillAmount = 0;
                cooldownOverlay.color = new Color(0, 0, 0, 0.75f); // 쿨다운 시 어둡게
            }
            if (cooldownText != null) {
                cooldownText.text = "";
                cooldownText.fontStyle = FontStyles.Bold;
                cooldownText.outlineWidth = 0.25f;
            }

            if (button != null) button.onClick.AddListener(OnClicked);
            
            // [UI Dressing] 버튼 배경 및 테두리 설정 (컴포넌트가 Image인 경우)
            Image btnImg = GetComponent<Image>();
            if (btnImg != null && btnImg.sprite == null) {
                // 기본적으로 어두운 사각형 배경 제공
                btnImg.color = new Color(0.12f, 0.12f, 0.15f, 0.9f);
            }
        }

        private void Update()
        {
            if (currentCooldown > 0)
            {
                currentCooldown -= Time.deltaTime;
                if (currentCooldown <= 0)
                {
                    currentCooldown = 0;
                    if (cooldownText != null) cooldownText.text = "";
                    if (cooldownOverlay != null) cooldownOverlay.fillAmount = 0;
                    if (button != null) button.interactable = true;
                }
                else
                {
                    // 쿨다운 시각적 표현
                    if (cooldownOverlay != null)
                        cooldownOverlay.fillAmount = currentCooldown / (maxCooldown > 0 ? maxCooldown : 1f);
                    
                    if (cooldownText != null)
                        cooldownText.text = Mathf.CeilToInt(currentCooldown).ToString();
                    
                    if (button != null) button.interactable = false;
                }
            }
        }

        private void OnClicked()
        {
            if (currentCooldown > 0) return;
            onSkillUsed?.Invoke(skillIndex);
            StartCooldown(maxCooldown);
        }

        public void StartCooldown(float duration)
        {
            maxCooldown = duration;
            currentCooldown = duration;
            if (button != null) button.interactable = false;
        }

        /// <summary>보스 캐스팅 중일 때 차단 스킬 발광</summary>
        public void SetInterruptGlow(bool active)
        {
            if (!isInterruptSkill || glowEffect == null) return;
            glowEffect.gameObject.SetActive(active);
            
            // [Premium Effect] 발광 시 살짝 깜빡이는 연출 (있다면)
            if (active) {
                float alpha = 0.5f + Mathf.PingPong(Time.time * 3f, 0.5f);
                glowEffect.color = new Color(1f, 0.9f, 0.4f, alpha);
            }
        }

        /// <summary>외부에서 아이콘/이름 설정</summary>
        public void SetSkillInfo(Sprite icon, string skillName, float cd)
        {
            if (iconImage != null) {
                if (icon != null) {
                    iconImage.sprite = icon;
                    iconImage.color = Color.white;
                } else {
                    // 아이콘이 없는 경우 역할에 맞는 색상 지정 (Q: 파랑, W: 초록, E: 노랑 등)
                    SetDefaultSkillColor();
                }
            }
            maxCooldown = cd;
        }

        private void SetDefaultSkillColor()
        {
            if (iconImage == null) return;
            switch(skillIndex) {
                case 0: iconImage.color = new Color(0.2f, 0.5f, 1f, 0.8f); break; // Q - Interrupt
                case 1: iconImage.color = new Color(0.2f, 0.8f, 0.3f, 0.8f); break; // W
                case 2: iconImage.color = new Color(1f, 0.8f, 0.2f, 0.8f); break; // E
                case 3: iconImage.color = new Color(0.8f, 0.2f, 0.8f, 0.8f); break; // R - Ultimate
                case 4: iconImage.color = new Color(0.7f, 0.7f, 0.7f, 0.8f); break; // Space - Dodge
            }
        }
    }
}
