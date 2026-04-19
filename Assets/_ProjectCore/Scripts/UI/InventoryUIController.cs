using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using BossRaid.Managers;
using BossRaid.Equipment;
using System.Linq;

namespace BossRaid.UI
{
    public class InventoryUIController : MonoBehaviour
    {
        public static InventoryUIController Instance { get; private set; }

        [Header("Main Panel")]
        public GameObject inventoryPanel;
        public Button closeButton;

        [Header("List View")]
        public Transform itemContentContainer;
        public GameObject itemSlotPrefab;
        
        [Header("Filters")]
        public Button weaponFilterBtn;
        public Button armorFilterBtn;
        public Button accessoryFilterBtn;
        public Button allFilterBtn;

        [Header("Details View")]
        public GameObject detailsPanel;
        public TextMeshProUGUI itemNameText;
        public TextMeshProUGUI itemRarityText;
        public TextMeshProUGUI itemStatsText;
        public TextMeshProUGUI equipStatusText;
        public Button equipButton;
        public Button enhanceButton;
        public TextMeshProUGUI enhanceCostText;

        private EquipmentData _selectedItem;
        private EquipSlot? _currentFilter = null;
        private string _targetCharacterName;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            if (inventoryPanel != null) inventoryPanel.SetActive(false);

            if (closeButton != null) closeButton.onClick.AddListener(CloseInventory);
            if (allFilterBtn != null) allFilterBtn.onClick.AddListener(() => SetFilter(null));
            if (weaponFilterBtn != null) weaponFilterBtn.onClick.AddListener(() => SetFilter(EquipSlot.Weapon));
            if (armorFilterBtn != null) armorFilterBtn.onClick.AddListener(() => SetFilter(EquipSlot.Armor));
            if (accessoryFilterBtn != null) accessoryFilterBtn.onClick.AddListener(() => SetFilter(EquipSlot.Accessory));

            if (equipButton != null) equipButton.onClick.AddListener(OnEquipClicked);
            if (enhanceButton != null) enhanceButton.onClick.AddListener(OnEnhanceClicked);

            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.OnItemAdded += (item) => RefreshInventory();
                InventoryManager.Instance.OnGearChanged += (ch, slot, item) => RefreshInventory();
                InventoryManager.Instance.OnItemEnhanced += (item) => RefreshDetails();
            }
        }

        public void OpenInventory(string characterName)
        {
            _targetCharacterName = characterName;
            if (inventoryPanel != null) inventoryPanel.SetActive(true);
            _selectedItem = null;
            if (detailsPanel != null) detailsPanel.SetActive(false);
            
            RefreshInventory();
        }

        public void CloseInventory()
        {
            if (inventoryPanel != null) inventoryPanel.SetActive(false);
        }

        private void SetFilter(EquipSlot? slot)
        {
            _currentFilter = slot;
            RefreshInventory();
        }

        private void RefreshInventory()
        {
            if (itemContentContainer == null || itemSlotPrefab == null || InventoryManager.Instance == null) return;

            // Clear existing
            foreach (Transform child in itemContentContainer)
            {
                Destroy(child.gameObject);
            }

            var items = InventoryManager.Instance.GetAllItems();
            if (_currentFilter.HasValue)
            {
                items = items.Where(x => x.slot == _currentFilter.Value).ToList();
            }

            foreach (var item in items)
            {
                var go = Instantiate(itemSlotPrefab, itemContentContainer);
                
                // Set name
                var nameText = go.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
                if (nameText != null) 
                {
                    nameText.text = item.FullName;
                    ColorUtility.TryParseHtmlString(item.RarityColorHex, out Color c);
                    nameText.color = c;
                }

                // Add click listener
                var btn = go.GetComponent<Button>();
                if (btn == null) btn = go.AddComponent<Button>();
                
                btn.onClick.AddListener(() => SelectItem(item));

                // Indicate if equipped
                var equipped = IsItemEquipped(item.instanceId);
                var eqText = go.transform.Find("EquippedText")?.GetComponent<TextMeshProUGUI>();
                if (eqText != null)
                {
                    eqText.text = equipped ? "[장착중]" : "";
                    eqText.color = Color.green;
                }
            }
        }

        private bool IsItemEquipped(string instanceId)
        {
            if (InventoryManager.Instance == null || string.IsNullOrEmpty(_targetCharacterName)) return false;
            for (int i = 0; i < InventoryManager.GEAR_SLOTS; i++)
            {
                var eq = InventoryManager.Instance.GetEquippedGear(_targetCharacterName, i);
                if (eq != null && eq.instanceId == instanceId) return true;
            }
            return false;
        }

        private void SelectItem(EquipmentData item)
        {
            _selectedItem = item;
            if (detailsPanel != null) detailsPanel.SetActive(true);
            RefreshDetails();
        }

        private void RefreshDetails()
        {
            if (_selectedItem == null) return;

            if (itemNameText != null)
            {
                itemNameText.text = _selectedItem.FullName;
                ColorUtility.TryParseHtmlString(_selectedItem.RarityColorHex, out Color c);
                itemNameText.color = c;
            }

            if (itemRarityText != null)
            {
                itemRarityText.text = $"{_selectedItem.rarity} {_selectedItem.slot}";
            }

            if (itemStatsText != null)
            {
                itemStatsText.text = _selectedItem.StatSummary;
            }

            bool isEquipped = IsItemEquipped(_selectedItem.instanceId);
            if (equipStatusText != null)
            {
                equipStatusText.text = isEquipped ? "현재 장착 중" : "";
                equipStatusText.color = isEquipped ? Color.green : Color.white;
            }

            if (equipButton != null)
            {
                var tmp = equipButton.GetComponentInChildren<TextMeshProUGUI>();
                if (tmp != null) tmp.text = isEquipped ? "장착 해제" : "장착하기";
            }

            if (enhanceCostText != null)
            {
                if (_selectedItem.enhanceLevel >= EquipmentData.MAX_ENHANCE)
                {
                    enhanceCostText.text = "최대 강화";
                }
                else
                {
                    enhanceCostText.text = $"비용: {_selectedItem.NextEnhanceCost:F0} G";
                }
            }
        }

        private void OnEquipClicked()
        {
            if (_selectedItem == null || InventoryManager.Instance == null || string.IsNullOrEmpty(_targetCharacterName)) return;

            bool isEquipped = IsItemEquipped(_selectedItem.instanceId);
            if (isEquipped)
            {
                // 장착 해제
                InventoryManager.Instance.UnequipGear(_targetCharacterName, (int)_selectedItem.slot);
            }
            else
            {
                // 장착
                InventoryManager.Instance.EquipGear(_targetCharacterName, _selectedItem.instanceId);
            }
            
            RefreshInventory();
            RefreshDetails();
        }

        private void OnEnhanceClicked()
        {
            if (_selectedItem == null || InventoryManager.Instance == null) return;

            bool success = InventoryManager.Instance.Enhance(_selectedItem.instanceId);
            if (success)
            {
                RefreshInventory();
                RefreshDetails();
            }
            else
            {
                Debug.Log("[InventoryUI] 강화 실패 (골드 부족 등)");
            }
        }
    }
}
