using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using BossRaid.UI;

namespace BossRaid.Editor
{
    public class InventoryUISetupTool : EditorWindow
    {
        [MenuItem("BossRaid/Auto Setup/Build Inventory UI")]
        public static void BuildInventoryUI()
        {
            var canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("[InventorySetup] 씬(Scene)에 Canvas가 없습니다. Canvas가 있는 인게임 씬에서 실행해주세요.");
                return;
            }

            var hud = Object.FindFirstObjectByType<InGameHUDController>();
            if (hud == null)
            {
                Debug.LogWarning("[InventorySetup] InGameHUDController를 찾을 수 없지만 UI 생성은 계속 진행합니다.");
            }

            // 1. 인벤토리 메인 패널 생성
            GameObject inventoryRoot = new GameObject("Inventory_UI", typeof(RectTransform));
            inventoryRoot.transform.SetParent(canvas.transform, false);
            var rootRt = inventoryRoot.GetComponent<RectTransform>();
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.sizeDelta = Vector2.zero;

            GameObject bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(inventoryRoot.transform, false);
            StretchRect(bg.GetComponent<RectTransform>());
            bg.GetComponent<Image>().color = new Color(0.05f, 0.05f, 0.08f, 0.9f); // 모던 다크 테마

            // 2. 인벤토리 창 (가운데 중앙)
            GameObject window = new GameObject("InventoryWindow", typeof(RectTransform), typeof(Image));
            window.transform.SetParent(inventoryRoot.transform, false);
            var winRt = window.GetComponent<RectTransform>();
            winRt.anchorMin = new Vector2(0.5f, 0.5f);
            winRt.anchorMax = new Vector2(0.5f, 0.5f);
            winRt.sizeDelta = new Vector2(1000, 600); // 가로 1000, 세로 600의 널찍한 레이아웃
            window.GetComponent<Image>().color = new Color(0.12f, 0.12f, 0.15f, 1f);

            // 타이틀 바
            GameObject titleBar = new GameObject("TitleBar", typeof(RectTransform), typeof(Image));
            titleBar.transform.SetParent(window.transform, false);
            var titleRt = titleBar.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0, 1);
            titleRt.anchorMax = new Vector2(1, 1);
            titleRt.pivot = new Vector2(0.5f, 1);
            titleRt.anchoredPosition = Vector2.zero;
            titleRt.sizeDelta = new Vector2(0, 60);
            titleBar.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.1f, 1f);

            var titleTxt = CreateText(titleBar.transform, "TitleText", "INVENTORY & EQUIPMENT", 24, TextAlignmentOptions.Center, Color.white);
            StretchRect(titleTxt.rectTransform);

            var closeBtnGO = CreateButton(titleBar.transform, "CloseButton", "X", new Vector2(50, 50));
            var closeRt = closeBtnGO.GetComponent<RectTransform>();
            closeRt.anchorMin = new Vector2(1, 0.5f);
            closeRt.anchorMax = new Vector2(1, 0.5f);
            closeRt.anchoredPosition = new Vector2(-30, 0);

            // 3. 레이아웃 분할 (좌측: 아이템 리스트 / 우측: 아이템 디테일)
            GameObject leftPanel = new GameObject("ListPanel", typeof(RectTransform));
            leftPanel.transform.SetParent(window.transform, false);
            var leftRt = leftPanel.GetComponent<RectTransform>();
            leftRt.anchorMin = new Vector2(0, 0);
            leftRt.anchorMax = new Vector2(0.6f, 1); // 60% 차지
            leftRt.offsetMin = new Vector2(20, 20);
            leftRt.offsetMax = new Vector2(-10, -70); // 위 타이틀바 여백

            GameObject rightPanel = new GameObject("DetailsPanel", typeof(RectTransform), typeof(Image));
            rightPanel.transform.SetParent(window.transform, false);
            var rightRt = rightPanel.GetComponent<RectTransform>();
            rightRt.anchorMin = new Vector2(0.6f, 0);
            rightRt.anchorMax = new Vector2(1, 1); // 40% 차지
            rightRt.offsetMin = new Vector2(10, 20);
            rightRt.offsetMax = new Vector2(-20, -70);
            rightPanel.GetComponent<Image>().color = new Color(0.18f, 0.18f, 0.22f, 1f);

            // ============================================
            // 4. 필터 구성 (ListPanel 상단)
            // ============================================
            GameObject filterGroup = new GameObject("FilterGroup", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            filterGroup.transform.SetParent(leftPanel.transform, false);
            var fgRt = filterGroup.GetComponent<RectTransform>();
            fgRt.anchorMin = new Vector2(0, 1);
            fgRt.anchorMax = new Vector2(1, 1);
            fgRt.pivot = new Vector2(0.5f, 1);
            fgRt.anchoredPosition = Vector2.zero;
            fgRt.sizeDelta = new Vector2(0, 40);

            var hlg = filterGroup.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 10; hlg.childControlWidth = true; hlg.childForceExpandWidth = true;

            var btnAll = CreateButton(filterGroup.transform, "Btn_All", "ALL").GetComponent<Button>();
            var btnWep = CreateButton(filterGroup.transform, "Btn_Weapon", "WEAPON").GetComponent<Button>();
            var btnArm = CreateButton(filterGroup.transform, "Btn_Armor", "ARMOR").GetComponent<Button>();
            var btnAcc = CreateButton(filterGroup.transform, "Btn_Accessory", "ACCESSORY").GetComponent<Button>();

            // ============================================
            // 5. 스크롤 뷰 구현 (리스트 패널 메인)
            // ============================================
            GameObject scrollObj = new GameObject("Scroll View", typeof(RectTransform), typeof(ScrollRect));
            scrollObj.transform.SetParent(leftPanel.transform, false);
            var scrollRt = scrollObj.GetComponent<RectTransform>();
            scrollRt.anchorMin = new Vector2(0, 0);
            scrollRt.anchorMax = new Vector2(1, 1);
            scrollRt.offsetMin = new Vector2(0, 0);
            scrollRt.offsetMax = new Vector2(0, -50); // 필터 여백

            GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(scrollObj.transform, false);
            StretchRect(viewport.GetComponent<RectTransform>());
            viewport.GetComponent<Image>().color = new Color(1, 1, 1, 0.05f); // 어두운 백그라운드 보여주기
            viewport.GetComponent<Mask>().showMaskGraphic = false;

            GameObject content = new GameObject("Content", typeof(RectTransform), typeof(GridLayoutGroup));
            content.transform.SetParent(viewport.transform, false);
            var contentRt = content.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0, 1);
            contentRt.anchorMax = new Vector2(1, 1);
            contentRt.pivot = new Vector2(0.5f, 1);
            contentRt.sizeDelta = new Vector2(0, 500); // 스크롤용 높이

            var grid = content.GetComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(120, 120);
            grid.spacing = new Vector2(15, 15);
            grid.padding = new RectOffset(15, 15, 15, 15);

            var sr = scrollObj.GetComponent<ScrollRect>();
            sr.content = contentRt;
            sr.viewport = viewport.GetComponent<RectTransform>();
            sr.horizontal = false;

            // ============================================
            // 6. 아이템 슬롯 프리팹 (코드 레벨에서 임시 생성 후 숨김)
            // ============================================
            GameObject itemSlotPrefabObj = new GameObject("ItemSlot_Prefab", typeof(RectTransform), typeof(Image), typeof(Button));
            itemSlotPrefabObj.transform.SetParent(inventoryRoot.transform, false); // 루트 바깥에 저장용
            itemSlotPrefabObj.SetActive(false);
            itemSlotPrefabObj.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.25f, 1f);

            var nameText = CreateText(itemSlotPrefabObj.transform, "NameText", "Item Name", 14, TextAlignmentOptions.Center, Color.white);
            nameText.rectTransform.anchorMin = new Vector2(0, 0);
            nameText.rectTransform.anchorMax = new Vector2(1, 0);
            nameText.rectTransform.pivot = new Vector2(0.5f, 0);
            nameText.rectTransform.sizeDelta = new Vector2(0, 30);
            nameText.rectTransform.anchoredPosition = Vector2.zero;

            var eqText = CreateText(itemSlotPrefabObj.transform, "EquippedText", "", 14, TextAlignmentOptions.Right, Color.green);
            eqText.rectTransform.anchorMin = new Vector2(0, 1);
            eqText.rectTransform.anchorMax = new Vector2(1, 1);
            eqText.rectTransform.pivot = new Vector2(0.5f, 1);
            eqText.rectTransform.anchoredPosition = new Vector2(-5, -5);

            // ============================================
            // 7. 디테일 뷰 (우측 패널) 구성
            // ============================================
            var dNameTxt = CreateText(rightPanel.transform, "ItemName", "Select An Item", 26, TextAlignmentOptions.Center, Color.white);
            dNameTxt.rectTransform.anchorMin = new Vector2(0, 1); dNameTxt.rectTransform.anchorMax = new Vector2(1, 1);
            dNameTxt.rectTransform.pivot = new Vector2(0.5f, 1); dNameTxt.rectTransform.anchoredPosition = new Vector2(0, -30);

            var dRarityTxt = CreateText(rightPanel.transform, "ItemRarity", "Rarity | Slot", 16, TextAlignmentOptions.Center, Color.gray);
            dRarityTxt.rectTransform.anchorMin = new Vector2(0, 1); dRarityTxt.rectTransform.anchorMax = new Vector2(1, 1);
            dRarityTxt.rectTransform.pivot = new Vector2(0.5f, 1); dRarityTxt.rectTransform.anchoredPosition = new Vector2(0, -70);

            var dEquipTxt = CreateText(rightPanel.transform, "EquippedStatus", "", 18, TextAlignmentOptions.Center, Color.green);
            dEquipTxt.rectTransform.anchorMin = new Vector2(0, 1); dEquipTxt.rectTransform.anchorMax = new Vector2(1, 1);
            dEquipTxt.rectTransform.pivot = new Vector2(0.5f, 1); dEquipTxt.rectTransform.anchoredPosition = new Vector2(0, -100);

            var dStatsBg = new GameObject("StatsBg", typeof(RectTransform), typeof(Image));
            dStatsBg.transform.SetParent(rightPanel.transform, false);
            var sBgRt = dStatsBg.GetComponent<RectTransform>();
            sBgRt.anchorMin = new Vector2(0, 0.4f); sBgRt.anchorMax = new Vector2(1, 0.8f);
            sBgRt.offsetMin = new Vector2(20, 0); sBgRt.offsetMax = new Vector2(-20, 0);
            dStatsBg.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.13f, 1f);

            var dStatsTxt = CreateText(dStatsBg.transform, "StatsDetails", "+20 Attack\n+10% HP", 18, TextAlignmentOptions.TopLeft, Color.white);
            StretchRect(dStatsTxt.rectTransform, 10);

            var equipBtn = CreateButton(rightPanel.transform, "EquipButton", "EQUIP", new Vector2(200, 50)).GetComponent<Button>();
            equipBtn.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 0);
            equipBtn.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0);
            equipBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 150);

            var enhanceBtn = CreateButton(rightPanel.transform, "EnhanceButton", "ENHANCE", new Vector2(200, 50)).GetComponent<Button>();
            enhanceBtn.GetComponent<Image>().color = new Color(0.2f, 0.5f, 0.3f);
            enhanceBtn.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 0);
            enhanceBtn.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0);
            enhanceBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 80);

            var enhanceCostTxt = CreateText(rightPanel.transform, "CostText", "Cost: 100G", 16, TextAlignmentOptions.Center, Color.yellow);
            enhanceCostTxt.rectTransform.anchorMin = new Vector2(0.5f, 0); enhanceCostTxt.rectTransform.anchorMax = new Vector2(0.5f, 0);
            enhanceCostTxt.rectTransform.anchoredPosition = new Vector2(0, 45);

            // ============================================
            // 8. Inventory UI Controller 스크립트 부착 및 연동
            // ============================================
            var controller = inventoryRoot.AddComponent<InventoryUIController>();
            // window가 아니라 inventoryRoot 자체를 껐다 켜야 배경(어두운 오버레이)도 함께 사라집니다.
            controller.inventoryPanel = inventoryRoot;
            controller.closeButton = closeBtnGO.GetComponent<Button>();
            controller.itemContentContainer = contentRt;
            controller.itemSlotPrefab = itemSlotPrefabObj;

            controller.weaponFilterBtn = btnWep;
            controller.armorFilterBtn = btnArm;
            controller.accessoryFilterBtn = btnAcc;
            controller.allFilterBtn = btnAll;

            controller.detailsPanel = rightPanel;
            controller.itemNameText = dNameTxt;
            controller.itemRarityText = dRarityTxt;
            controller.itemStatsText = dStatsTxt;
            controller.equipStatusText = dEquipTxt;
            controller.equipButton = equipBtn;
            controller.enhanceButton = enhanceBtn;
            controller.enhanceCostText = enhanceCostTxt;

            // ============================================
            // 9. HUD 버튼 자동 연동
            // ============================================
            if (hud != null)
            {
                hud.inventoryUI = controller;
                
                // 아직 Inventory 버튼이 HUD에 시각적으로 없으므로 동적으로 생성해줍시다
                if (hud.inventoryButton == null)
                {
                    var sysGroup = hud.settingsButton != null ? hud.settingsButton.transform.parent : canvas.transform;
                    var invHudBtn = CreateButton(sysGroup, "HUD_InventoryButton", "BAG", new Vector2(60, 60));
                    hud.inventoryButton = invHudBtn.GetComponent<Button>();
                    
                    if (hud.settingsButton != null)
                    {
                        var rt = invHudBtn.GetComponent<RectTransform>();
                        rt.anchorMin = hud.settingsButton.GetComponent<RectTransform>().anchorMin;
                        rt.anchorMax = hud.settingsButton.GetComponent<RectTransform>().anchorMax;
                        rt.anchoredPosition = hud.settingsButton.GetComponent<RectTransform>().anchoredPosition + new Vector2(-70, 0);
                    }
                    else
                    {
                        // 셋팅 버튼이 없다면 우측 상단에 강제로 예쁘게 배치
                        var rt = invHudBtn.GetComponent<RectTransform>();
                        rt.anchorMin = new Vector2(1, 1);
                        rt.anchorMax = new Vector2(1, 1);
                        rt.pivot = new Vector2(1, 1);
                        rt.anchoredPosition = new Vector2(-20, -20);
                    }

                    // Start()에서 ToggleInventory 코드로 자동 연동되므로 이벤트 등록 생략
                }
                
                EditorUtility.SetDirty(hud);
                Debug.Log("<color=green>[InventorySetup] InGameHUDController 자동 연동 완료!</color>");
            }

            // 초기 상태는 꺼둡니다 (에디터에서도 안 보이게)
            inventoryRoot.SetActive(false);
            EditorUtility.SetDirty(inventoryRoot);

            Debug.Log("<color=cyan>[InventorySetup] 성공적으로 모던 인벤토리 UI 시스템을 자동 구축했습니다.</color>");
            Selection.activeGameObject = inventoryRoot;
        }

        private static void StretchRect(RectTransform rt, float padding = 0)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            rt.offsetMin = new Vector2(padding, padding);
            rt.offsetMax = new Vector2(-padding, -padding);
        }

        private static GameObject CreateButton(Transform parent, string name, string text, Vector2? size = null)
        {
            GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(parent, false);
            btnObj.GetComponent<Image>().color = new Color(0.3f, 0.3f, 0.35f, 1f); // 버튼 기본색
            
            if (size.HasValue)
            {
                btnObj.GetComponent<RectTransform>().sizeDelta = size.Value;
            }

            var txtObj = CreateText(btnObj.transform, "Text", text, 20, TextAlignmentOptions.Center, Color.white);
            StretchRect(txtObj.rectTransform);
            
            return btnObj;
        }

        private static TextMeshProUGUI CreateText(Transform parent, string name, string text, float size, TextAlignmentOptions align, Color color)
        {
            GameObject txtObj = new GameObject(name, typeof(RectTransform));
            txtObj.transform.SetParent(parent, false);
            var tmp = txtObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.alignment = align;
            tmp.color = color;
            return tmp;
        }
    }
}
