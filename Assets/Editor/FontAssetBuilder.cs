#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using TMPro;
using System.IO;

namespace BossRaid.Editor
{
    public static class FontAssetBuilder
    {
        private const string FONT_SOURCE_PATH = "Assets/Fonts/Pretendard-Regular.otf";
        private const string FONT_ASSET_PATH  = "Assets/Fonts/Pretendard-Regular SDF.asset";
        private const string TMP_SETTINGS_PATH = "Assets/TextMesh Pro/Resources/TMP Settings.asset";

        [MenuItem("Tools/Setup Korean Font")]
        public static void SetupKoreanFont()
        {
            // 1. 소스 폰트 확인
            if (!File.Exists(FONT_SOURCE_PATH))
            {
                EditorUtility.DisplayDialog("폰트 없음", 
                    "Assets/Fonts/Pretendard-Regular.otf 파일이 없습니다.\n\n" +
                    "walkthrough.md의 링크에서 다운로드하여 해당 경로에 넣어주세요.", "확인");
                return;
            }

            // 2. TMP Font Asset 확인 (사용자가 수동으로 SDF를 만들었는지)
            TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_ASSET_PATH);
            
            if (fontAsset == null)
            {
                EditorUtility.DisplayDialog("Font Asset 필요", 
                    "폰트 파일은 찾았으나, TextMeshPro Font Asset(SDF)이 아직 생성되지 않았습니다.\n\n" +
                    "방법:\n1. Window > TextMeshPro > Font Asset Creator 열기\n" +
                    "2. Source Font File에 Pretendard-Regular 선택\n" +
                    "3. Character Set을 'Korean'으로 설정 (또는 상용 한글 2350자 입력)\n" +
                    "4. Generate Atlas -> Save 클릭하여 " + FONT_ASSET_PATH + " 로 저장\n\n" +
                    "완료 후 다시 이 메뉴를 실행해 주세요.", "확인");
                return;
            }

            // 3. TMP Settings 업데이트
            TMP_Settings settings = AssetDatabase.LoadAssetAtPath<TMP_Settings>(TMP_SETTINGS_PATH);
            if (settings != null)
            {
                // SerializedObject를 사용하여 강제 업데이트
                SerializedObject so = new SerializedObject(settings);
                so.FindProperty("m_defaultFontAsset").objectReferenceValue = fontAsset;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
                Debug.Log("[FontAssetBuilder] TMP Default Font Asset set to Pretendard.");
            }

            // 4. 현재 씬의 모든 TMP 오브젝트 업데이트
            var allTMP = Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var tmp in allTMP)
            {
                Undo.RecordObject(tmp, "Update Font");
                tmp.font = fontAsset;
                EditorUtility.SetDirty(tmp);
            }

            EditorUtility.DisplayDialog("완료", 
                "프로젝트 전체에 한글 폰트 적용이 완료되었습니다!\n" +
                "이제 모든 텍스트가 깨짐 없이 출력됩니다.", "확인");
        }
    }
}
#endif
