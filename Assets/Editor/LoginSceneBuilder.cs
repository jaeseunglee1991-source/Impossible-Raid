#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using System.IO;
using UnityEditor.Events;
using UnityEngine.UI;
using BossRaid.Managers;

namespace BossRaid.Editor
{
    public static class LoginSceneBuilder
    {
        [MenuItem("Tools/Modern Login/Build Scene (Google & Guest)")]
        public static void BuildModernLoginScene()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("경고", "플레이 모드 중에는 씬을 생성할 수 없습니다. 플레이를 멈추고 다시 실행해주세요.", "확인");
                return;
            }
            string loginScenePath = "Assets/Scenes/LoginScene.unity";

            if (!Directory.Exists("Assets/Scenes"))
                Directory.CreateDirectory("Assets/Scenes");

            string imagePath = "Assets/Scripts/UI/loginscene.png";
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(imagePath);
            if (texture == null)
            {
                EditorUtility.DisplayDialog("오류", $"[{imagePath}] 경로에 이미지가 없습니다!", "확인");
                return;
            }

            var importer = AssetImporter.GetAtPath(imagePath) as TextureImporter;
            if (importer != null && importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                EditorUtility.SetDirty(importer);
                importer.SaveAndReimport();
            }

            var loginScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            loginScene.name = "LoginScene";

            var cameraGO = new GameObject("Main Camera");
            var cam = cameraGO.AddComponent<Camera>();
            cam.tag = "MainCamera";
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cameraGO.transform.position = new Vector3(0, 0, -10);

            CreateEventSystem();

            var canvasGO = new GameObject("Canvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var cs = canvasGO.AddComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1920, 1080);
            cs.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            canvasGO.AddComponent<GraphicRaycaster>();

            var bgGO = new GameObject("BackgroundImage");
            bgGO.transform.SetParent(canvasGO.transform, false);
            var bgImg = bgGO.AddComponent<Image>();
            
            Sprite bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>(imagePath);
            bgImg.sprite = bgSprite;
            bgImg.preserveAspect = false;

            var bgRT = bgGO.GetComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
            bgRT.offsetMin = Vector2.zero; bgRT.offsetMax = Vector2.zero;

            var aspectFitter = bgGO.AddComponent<UnityEngine.UI.AspectRatioFitter>();
            aspectFitter.aspectMode = UnityEngine.UI.AspectRatioFitter.AspectMode.EnvelopeParent;
            if (bgSprite != null)
                aspectFitter.aspectRatio = bgSprite.rect.width / bgSprite.rect.height;
            else
                aspectFitter.aspectRatio = 1920f / 1080f;

            var googleBtnGO = CreateInvisibleHitboxButton(bgGO.transform, "GoogleLoginBtn", new Vector2(0.34f, 0.49f), new Vector2(0.66f, 0.605f));
            var guestBtnGO  = CreateInvisibleHitboxButton(bgGO.transform, "GuestLoginBtn", new Vector2(0.36f, 0.345f), new Vector2(0.64f, 0.44f));

            var loadingStatusText = CreateText(canvasGO.transform, "로그인 시도 중...", 20, new Vector2(0, -250), Color.gray);
            loadingStatusText.gameObject.SetActive(false);

            var popupPanel = CreatePanel(canvasGO.transform, "PopupPanel", new Color(0.05f, 0.06f, 0.08f, 0.98f), Vector2.zero, new Vector2(450, 300));
            var pCanvas = popupPanel.AddComponent<Canvas>();
            pCanvas.overrideSorting = true;
            pCanvas.sortingOrder = 999;
            popupPanel.AddComponent<GraphicRaycaster>();
            popupPanel.SetActive(false);

            var popupTextGO = new GameObject("PopupText");
            popupTextGO.transform.SetParent(popupPanel.transform, false);
            var popupText = popupTextGO.AddComponent<TextMeshProUGUI>();
            popupText.text = "Popup Message";
            popupText.fontSize = 24;
            popupText.alignment = TextAlignmentOptions.Center;
            popupText.rectTransform.sizeDelta = new Vector2(400, 150);
            popupText.rectTransform.anchoredPosition = new Vector2(0, 30);
            var popupBtnGO = CreateButton(popupPanel.transform, "PopupCloseButton", "확인", new Color(0.16f, 0.47f, 0.88f), new Vector2(0, -80), new Vector2(160, 45));

            EnsureManagers(loginScene);

            var controllerGO = new GameObject("LoginUIController");
            var ctrl = controllerGO.AddComponent<BossRaid.UI.LoginUIController>();
            ctrl.loginPanel = bgGO; 
            ctrl.googleLoginButton = googleBtnGO.GetComponent<Button>();
            ctrl.guestLoginButton = guestBtnGO.GetComponent<Button>();
            ctrl.loadingStatusText = loadingStatusText;
            ctrl.popupPanel = popupPanel;
            ctrl.popupText = popupText;
            ctrl.popupCloseButton = popupBtnGO.GetComponent<Button>();

            UnityEventTools.AddPersistentListener(ctrl.googleLoginButton.onClick, ctrl.OnGoogleLoginClicked);
            UnityEventTools.AddPersistentListener(ctrl.guestLoginButton.onClick, ctrl.OnGuestLoginClicked);
            UnityEventTools.AddPersistentListener(ctrl.popupCloseButton.onClick, ctrl.ClosePopup);

            ApplyPretendardFont();

            EditorSceneManager.SaveScene(loginScene, loginScenePath);
            AssetDatabase.Refresh();
            SetBuildSettings(loginScenePath);

            EditorUtility.DisplayDialog("완료!", "Login Scene 빌드가 완료되었습니다.", "확인");
        }

        private static GameObject CreateInvisibleHitboxButton(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var img = go.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0f); 
            img.raycastTarget = true;

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            
            var cb = btn.colors;
            cb.normalColor = new Color(0, 0, 0, 0f); 
            cb.highlightedColor = new Color(0, 0, 0, 0.15f); 
            cb.pressedColor = new Color(0, 0, 0, 0.4f); 
            cb.selectedColor = new Color(0, 0, 0, 0f);
            btn.colors = cb;
            
            return go;
        }

        private static void EnsureManagers(UnityEngine.SceneManagement.Scene scene)
        {
            GameObject managersGO = GameObject.Find("Managers");
            if (managersGO == null)
            {
                managersGO = new GameObject("Managers");
            }
            UnityEditor.SceneManagement.EditorSceneManager.MoveGameObjectToScene(managersGO, scene);

            if (managersGO.GetComponent<DatabaseManager>() == null) managersGO.AddComponent<DatabaseManager>();
            if (managersGO.GetComponent<AuthManager>() == null) managersGO.AddComponent<AuthManager>();
        }

        private static void SetBuildSettings(string loginPath)
        {
            var scenes = new EditorBuildSettingsScene[] {
                new EditorBuildSettingsScene(loginPath, true)
            };
            EditorBuildSettings.scenes = scenes;
        }

        private static void ApplyPretendardFont()
        {
            var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Pretendard-Regular SDF.asset");
            if (fontAsset != null)
            {
                foreach (var t in Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    t.font = fontAsset;
            }
        }

        private static void CreateEventSystem()
        {
            var existingES = Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>();
            if (existingES != null) Object.DestroyImmediate(existingES.gameObject);

            var eventSystemGO = new GameObject("EventSystem");
            eventSystemGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        private static GameObject CreatePanel(Transform parent, string name, Color color, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            var img = go.AddComponent<Image>();
            img.color = color; img.raycastTarget = true;
            return go;
        }

        private static TextMeshProUGUI CreateText(Transform parent, string content, int size, Vector2 pos, Color color)
        {
            var go = new GameObject("Text_" + content);
            go.transform.SetParent(parent, false);
            var txt = go.AddComponent<TextMeshProUGUI>();
            txt.text = content; txt.fontSize = size; txt.color = color;
            txt.alignment = TextAlignmentOptions.Center;
            var rt = txt.GetComponent<RectTransform>();
            rt.anchoredPosition = pos; 
            rt.sizeDelta = new Vector2(250, 50);
            return txt;
        }

        private static GameObject CreateButton(Transform parent, string name, string label, Color color, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = color; img.raycastTarget = true;
            
            var rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;

            var txtGO = new GameObject("Label");
            txtGO.transform.SetParent(go.transform, false);
            var txt = txtGO.AddComponent<TextMeshProUGUI>();
            txt.text = label; txt.color = Color.white; txt.fontSize = 24;
            txt.alignment = TextAlignmentOptions.Center;
            txt.raycastTarget = false;
            
            var txtRT = txt.GetComponent<RectTransform>();
            txtRT.anchorMin = Vector2.zero; txtRT.anchorMax = Vector2.one;
            txtRT.offsetMin = Vector2.zero; txtRT.offsetMax = Vector2.zero;

            return go;
        }
    }
}
#endif
