using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

namespace BossRaid.Editor
{
    /// <summary>
    /// 방치형 + 보스레이드 혼합형 (가로형 1920x1080) UI 뼈대를 자동 생성하는 툴
    /// </summary>
    public class InGameUIBuilder : EditorWindow
    {
        [MenuItem("Tools/3. Build InGame UI (Landscape 가로형 뼈대 생성)")]
        public static void BuildLandscapeUI()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("플레이 모드 중에는 생성할 수 없습니다.");
                return;
            }

            Debug.Log("<color=cyan>1920x1080 가로형 방치형+레이드 UI 뼈대를 구축합니다...</color>");

            // 1. 기존 UI 캔버스가 있다면 우회 또는 덮어쓰기 위해 최상단에 새로 만듦
            GameObject canvasGO = new GameObject("RaidIdle_Canvas");
            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            
            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f; // 가로형 게임 최적화 (Height 타겟팅)

            canvasGO.AddComponent<GraphicRaycaster>();

            // 이벤트 시스템 부착
            if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject esGO = new GameObject("EventSystem");
                esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            // =========================================================================
            //  UI 영역 배치 (가로형 모바일 최적화)
            // =========================================================================

            // 1. 상단 (Top) - 스테이지 정보 및 재화 상태
            var topPanel = CreatePanel(canvasGO.transform, "TopStatusPanel", new Color(0.1f, 0.1f, 0.12f, 0.8f));
            SetRect(topPanel, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -90), new Vector2(0, 0));
            
            CreateText(topPanel.transform, "Stage Info", "현재 스테이지: 1 (잡몹 0/50)", 24, new Vector2(250, -45), TextAlignmentOptions.Left, Color.white);
            CreateText(topPanel.transform, "Gold Status", "💰 보유 골드: 0", 28, new Vector2(-250, -45), TextAlignmentOptions.Right, Color.yellow);
            
            // 중앙 상단 - 보스 도전 버튼 (평소엔 게이지, 다 차면 보스 도전 활성화)
            var bossChallengeBtn = CreateButton(topPanel.transform, "BossChallengeBtn", "보스 도전 (비활성)", new Color(0.8f, 0.2f, 0.2f, 0.6f));
            SetRect(bossChallengeBtn, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-120, -35), new Vector2(120, 35));

            // 2. 좌측 상단 (Top-Left) - 태그 파티 체력 프레임 (2명)
            var partyFramePanel = CreatePanel(canvasGO.transform, "PartyFramePanel", new Color(0, 0, 0, 0f));
            SetRect(partyFramePanel, new Vector2(0, 1), new Vector2(0, 1), new Vector2(20, -300), new Vector2(350, -110));

            var char1 = CreatePanel(partyFramePanel.transform, "Char1_Frame", new Color(0.1f, 0.3f, 0.1f, 0.8f));
            SetRect(char1, new Vector2(0, 0.5f), new Vector2(1, 1), new Vector2(0, 5), new Vector2(0, 0));
            CreateText(char1.transform, "Name", "파티원 1", 20, Vector2.zero, TextAlignmentOptions.Center, Color.white);

            var char2 = CreatePanel(partyFramePanel.transform, "Char2_Frame", new Color(0.2f, 0.2f, 0.4f, 0.8f));
            SetRect(char2, new Vector2(0, 0), new Vector2(1, 0.5f), new Vector2(0, 0), new Vector2(0, -5));
            CreateText(char2.transform, "Name", "파티원 2", 20, Vector2.zero, TextAlignmentOptions.Center, Color.white);

            // 3. 우측 하단 (Bottom-Right) - 수동 조작 컨트롤 (보스 전용)
            var combatControlPanel = CreatePanel(canvasGO.transform, "CombatControlPanel", new Color(0, 0, 0, 0f));
            SetRect(combatControlPanel, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-500, 20), new Vector2(-20, 250));

            var ultimateBtn = CreateButton(combatControlPanel.transform, "UltimateBtn", "생존 / 궁극기\n(수동)", new Color(0.3f, 0.1f, 0.6f, 0.9f));
            SetRect(ultimateBtn, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-180, 20), new Vector2(-20, 180));

            var tagBtn = CreateButton(combatControlPanel.transform, "TagBtn", "파티 태그\n(교체)", new Color(0.1f, 0.5f, 0.6f, 0.9f));
            SetRect(tagBtn, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-360, 20), new Vector2(-200, 120));

            // 4. 좌측 하단 (Bottom-Left) - 방치형 성장(업그레이드) 패널
            var growthPanel = CreatePanel(canvasGO.transform, "GrowthPanel", new Color(0.05f, 0.05f, 0.08f, 0.95f));
            SetRect(growthPanel, new Vector2(0, 0), new Vector2(0.4f, 0.4f), new Vector2(20, 20), new Vector2(0, 0)); // 좌측 하단 40% 영역 차지

            CreateText(growthPanel.transform, "Title", "- 캐릭 성장 (방치형) -", 24, new Vector2(0, -30), TextAlignmentOptions.Center, Color.cyan, true);

            // 공격력 업그레이드
            var atkUpGB = CreatePanel(growthPanel.transform, "AtkUpgrade", new Color(0.15f, 0.15f, 0.2f, 1f));
            SetRect(atkUpGB, new Vector2(0.05f, 0.6f), new Vector2(0.95f, 0.85f), Vector2.zero, Vector2.zero);
            CreateText(atkUpGB.transform, "Label", "공격력 증가", 20, new Vector2(80, 0), TextAlignmentOptions.Left, Color.white);
            CreateText(atkUpGB.transform, "Cost", "비용: 15G", 20, new Vector2(-150, 0), TextAlignmentOptions.Right, Color.yellow);
            var atkBtn = CreateButton(atkUpGB.transform, "BtnUP", "레벨업", new Color(0.2f, 0.6f, 0.2f, 1f));
            SetRect(atkBtn, new Vector2(1, 0), new Vector2(1, 1), new Vector2(-120, 5), new Vector2(-5, -5));

            // 체력 업그레이드
            var hpUpGB = CreatePanel(growthPanel.transform, "HpUpgrade", new Color(0.15f, 0.15f, 0.2f, 1f));
            SetRect(hpUpGB, new Vector2(0.05f, 0.3f), new Vector2(0.95f, 0.55f), Vector2.zero, Vector2.zero);
            CreateText(hpUpGB.transform, "Label", "최대 체력 증가", 20, new Vector2(80, 0), TextAlignmentOptions.Left, Color.white);
            CreateText(hpUpGB.transform, "Cost", "비용: 15G", 20, new Vector2(-150, 0), TextAlignmentOptions.Right, Color.yellow);
            var hpBtn = CreateButton(hpUpGB.transform, "BtnUP", "레벨업", new Color(0.2f, 0.6f, 0.2f, 1f));
            SetRect(hpBtn, new Vector2(1, 0), new Vector2(1, 1), new Vector2(-120, 5), new Vector2(-5, -5));

            Debug.Log("<color=green>✅ [3단계 완료] 가로형 방치형 뼈대 UI가 씬에 완벽히 생성되었습니다!</color>");
        }

        // =========================================================================
        //  UI 헬퍼 함수들
        // =========================================================================
        private static GameObject CreatePanel(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            return go;
        }

        private static void SetRect(GameObject go, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
        }

        private static TMP_Text CreateText(Transform parent, string name, string text, int size, Vector2 position, TextAlignmentOptions align, Color color, bool topAnchor = false)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var txt = go.AddComponent<TextMeshProUGUI>();
            txt.text = text;
            txt.fontSize = size;
            txt.color = color;
            txt.alignment = align;
            txt.fontStyle = FontStyles.Bold;
            
            var rt = go.GetComponent<RectTransform>();
            if (topAnchor) {
                rt.anchorMin = new Vector2(0.5f, 1);
                rt.anchorMax = new Vector2(0.5f, 1);
            } else {
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
            }
            rt.sizeDelta = new Vector2(400, 50);
            rt.anchoredPosition = position;
            return txt;
        }

        private static GameObject CreateButton(Transform parent, string name, string label, Color color)
        {
            var go = CreatePanel(parent, name, color);
            var btn = go.AddComponent<Button>(); // 버튼 추가

            // 버튼 자식에 텍스트 달기
            var txtGo = new GameObject("Label", typeof(RectTransform));
            txtGo.transform.SetParent(go.transform, false);
            SetRect(txtGo, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var txt = txtGo.AddComponent<TextMeshProUGUI>();
            txt.text = label;
            txt.color = Color.white;
            txt.fontSize = 24;
            txt.fontStyle = FontStyles.Bold;
            txt.alignment = TextAlignmentOptions.Center;

            return go;
        }
    }
}
