using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using BossRaid.Combat;
using BossRaid.Managers;

namespace BossRaid.UI
{
    /// <summary>
    /// 방치형 성장 UI(공격력, 체력 업그레이드)와 스탯 표시를 담당하는 컨트롤러
    /// </summary>
    public class GrowthUIController : MonoBehaviour
    {
        [Header("업그레이드 UI 연결 (직접 드래그 안 하면 이름으로 자동 할당)")]
        public Button atkUpgradeBtn;
        public TextMeshProUGUI atkCostText;
        public TextMeshProUGUI atkLabelText;

        public Button hpUpgradeBtn;
        public TextMeshProUGUI hpCostText;
        public TextMeshProUGUI hpLabelText;

        [Header("스테이터스 UI")]
        public TextMeshProUGUI statusText;

        private void Start()
        {
            AutoFindUIElements();

            if (atkUpgradeBtn != null) atkUpgradeBtn.onClick.AddListener(OnAtkUpgradeClicked);
            if (hpUpgradeBtn != null) hpUpgradeBtn.onClick.AddListener(OnHpUpgradeClicked);

            if (GrowthManager.Instance != null)
            {
                GrowthManager.Instance.OnGoldChanged += delegate { UpdateUI(); };
            }

            // 초기 지연 업데이트 (모든 씬 로드 및 매니저 초기화 후)
            Invoke("UpdateUI", 0.5f);
        }

        private void AutoFindUIElements()
        {
            // Atk UI
            if (atkUpgradeBtn == null)
            {
                Transform atkUp = GameObject.Find("AtkUpgrade")?.transform;
                if (atkUp)
                {
                    atkUpgradeBtn = atkUp.GetComponentInChildren<Button>();
                    atkLabelText = atkUp.Find("Label")?.GetComponent<TextMeshProUGUI>();
                    atkCostText = atkUp.Find("Cost")?.GetComponent<TextMeshProUGUI>();
                }
            }

            // HP UI
            if (hpUpgradeBtn == null)
            {
                Transform hpUp = GameObject.Find("HpUpgrade")?.transform;
                if (hpUp)
                {
                    hpUpgradeBtn = hpUp.GetComponentInChildren<Button>();
                    hpLabelText = hpUp.Find("Label")?.GetComponent<TextMeshProUGUI>();
                    hpCostText = hpUp.Find("Cost")?.GetComponent<TextMeshProUGUI>();
                }
            }

            // Status Text - 만약 없으면 GrowthPanel 밑에 하나 생성합니다.
            if (statusText == null)
            {
                Transform growthPanel = GameObject.Find("GrowthPanel")?.transform;
                if (growthPanel != null)
                {
                    Transform t = growthPanel.Find("StatusText");
                    if (t != null) statusText = t.GetComponent<TextMeshProUGUI>();
                    else
                    {
                        GameObject go = new GameObject("StatusText", typeof(RectTransform));
                        go.transform.SetParent(growthPanel, false);
                        statusText = go.AddComponent<TextMeshProUGUI>();
                        statusText.fontSize = 18;
                        statusText.color = Color.white;
                        statusText.alignment = TextAlignmentOptions.TopLeft;
                        
                        RectTransform rt = go.GetComponent<RectTransform>();
                        rt.anchorMin = new Vector2(0, 0);
                        rt.anchorMax = new Vector2(1, 0);
                        rt.pivot = new Vector2(0, 0);
                        rt.anchoredPosition = new Vector2(20, -100);
                        rt.sizeDelta = new Vector2(-40, 90);
                    }
                }
            }
        }

        private void OnAtkUpgradeClicked()
        {
            CharacterBase firstChar = GetFirstActiveCharacter();
            if (firstChar == null) return;

            // 골드 부족 등을 TryUpgrade에서 알아서 처리함
            if (firstChar.attackPowerUpgrade.TryUpgrade())
            {
                // 파티 멤버 전체에게 동일 레벨 동기화
                SyncUpgradeLevelToAll("Atk", firstChar.attackPowerUpgrade.currentLevel);
                UpdateUI();
            }
        }

        private void OnHpUpgradeClicked()
        {
            CharacterBase firstChar = GetFirstActiveCharacter();
            if (firstChar == null) return;

            if (firstChar.maxHpUpgrade.TryUpgrade())
            {
                // 파티 멤버 전체에게 동일 레벨 동기화
                SyncUpgradeLevelToAll("HP", firstChar.maxHpUpgrade.currentLevel);
                UpdateUI();
                
                // 증가된 최대 체력만큼 모든 캐릭터 체력 회복
                foreach (var character in FindObjectsByType<CharacterBase>(FindObjectsSortMode.None))
                {
                    if (character != null && !character.IsDead)
                    {
                        character.currentHealth += (float)character.maxHpUpgrade.statIncreasePerLevel;
                    }
                }
            }
        }

        private void SyncUpgradeLevelToAll(string type, int newLevel)
        {
            var characters = FindObjectsByType<CharacterBase>(FindObjectsSortMode.None);
            foreach (var character in characters)
            {
                if (type == "Atk") character.attackPowerUpgrade.currentLevel = newLevel;
                else if (type == "HP") character.maxHpUpgrade.currentLevel = newLevel;
            }
            SaveManager.Instance?.MarkDirty();
        }

        private CharacterBase GetFirstActiveCharacter()
        {
            var chars = FindObjectsByType<CharacterBase>(FindObjectsSortMode.None);
            if (chars.Length > 0) return chars[0];
            return null;
        }

        public void UpdateUI()
        {
            CharacterBase c = GetFirstActiveCharacter();
            if (c == null) return;

            double gold = GrowthManager.Instance != null ? GrowthManager.Instance.displayGold : 0;

            if (atkLabelText != null && atkCostText != null)
            {
                atkLabelText.text = $"공격력 증가 [Lv.{c.attackPowerUpgrade.currentLevel}]";
                double cost = c.attackPowerUpgrade.NextUpgradeCost;
                atkCostText.text = $"비용: {cost:F0}G";
                atkCostText.color = (gold >= cost) ? Color.yellow : Color.red;
            }

            if (hpLabelText != null && hpCostText != null)
            {
                hpLabelText.text = $"최대 체력 증가 [Lv.{c.maxHpUpgrade.currentLevel}]";
                double cost = c.maxHpUpgrade.NextUpgradeCost;
                hpCostText.text = $"비용: {cost:F0}G";
                hpCostText.color = (gold >= cost) ? Color.yellow : Color.red;
            }

            // 캐릭터 스테이터스 (전투력) 출력
            if (statusText != null)
            {
                float atk = c.baseAttackPower;
                float hp = c.maxHealth;
                float dps = atk / c.attackSpeed;
                
                statusText.text = $"[파티 대표 스펙]\n• 기본 공격력: {atk:F0}\n• 최대 체력: {hp:F0}\n• 기본 초당 피해량(DPS): {dps:F1}";
            }
        }
    }
}
