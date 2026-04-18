using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using BossRaid.Managers;
using BossRaid.Combat;

namespace BossRaid.Editor
{
    /// <summary>
    /// 기존의 지저분한 InGameScene을 폐기하고 아무것도 없는 빈 상태에서
    /// 방치형 + 보스레이드 구조를 한 번에 완벽히 구축하는 툴입니다.
    /// </summary>
    public class CleanInGameSceneBuilder : EditorWindow
    {
        [MenuItem("Tools/⭐ Build Clean InGame Scene (완전 초기화 후 새로 생성)")]
        public static void BuildCleanScene()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("경고", "플레이 모드 중에는 씬을 생성할 수 없습니다.", "확인");
                return;
            }

            bool confirm = EditorUtility.DisplayDialog("씬 초기화 경고", 
                "현재 열려있는 작업 내용이 모두 날아가고, 빈 바닥부터 완벽히 세팅된 새로운 [InGameScene]을 생성합니다.\n계속하시겠습니까?", 
                "네, 새로 만듭니다", "취소");
                
            if (!confirm) return;

            string scenePath = "Assets/Scenes/InGameScene.unity";

            // 1. 완전 빈 씬 생성
            var newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            newScene.name = "InGameScene";

            // 2. 기본 조명 및 카메라 설정
            var cameraGO = new GameObject("Main Camera");
            var cam = cameraGO.AddComponent<Camera>();
            cam.tag = "MainCamera";
            cam.orthographic = true;
            cam.orthographicSize = 6.0f; // 2D 씬에 적합한 사이즈
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.1f, 0.1f, 0.12f); // 약간 어두운 방치형 백그라운드 느낌
            cameraGO.transform.position = new Vector3(0, 0, -10);
            
            var lightGO = new GameObject("Directional Light");
            var light = lightGO.AddComponent<Light>();
            light.type = LightType.Directional;
            lightGO.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            // 3. 기존에 만든 Setup 메서드 순차적으로 자동 실행
            // [Step 1] 매니저 및 존(Zone) 뼈대
            IdleRaidSceneSetup.SetupScene();
            
            // [Step 2] 배경화면 설정
            IdleRaidSceneSetup.SetupBackground();
            
            // [Step 3] 스폰 포인트 설정 및 자동 연결
            IdleRaidSceneSetup.SetupSpawnPoints();
            
            // [Step 4] 유저 지정 프리팹 자동 연결
            IdleRaidSceneSetup.AssignPrefabs();

            // [Step 5] 4개 직업 캐릭터 생성 및 파티 세팅
            IdleRaidSceneSetup.CreateCharactersAndAssign();

            // [Step 6] 가로형 방치형 HUD 캔버스 구축
            InGameUIBuilder.BuildLandscapeUI();

            // 4. 유니티 Build Settings 에 씬 등록 (없으면 추가)
            AddSceneToBuildSettings(scenePath);

            // 5. 씬 강제 저장
            EditorSceneManager.SaveScene(newScene, scenePath);

            Debug.Log("<color=magenta>⭐⭐⭐ [초기화 완료] 이전의 더러운 정보가 모두 날아가고 완벽하게 규격화된 InGameScene이 재탄생했습니다! ⭐⭐⭐</color>");
            EditorUtility.DisplayDialog("완료", "깨끗한 InGameScene 통합 빌드가 완료되었습니다.\nLoginScene으로 돌아가서 게임 시작 버튼을 눌러보세요!", "확인");
        }

        private static void AddSceneToBuildSettings(string scenePath)
        {
            var original = EditorBuildSettings.scenes;
            bool found = false;
            foreach (var scene in original)
            {
                if (scene.path == scenePath)
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                var newScenes = new EditorBuildSettingsScene[original.Length + 1];
                System.Array.Copy(original, newScenes, original.Length);
                newScenes[newScenes.Length - 1] = new EditorBuildSettingsScene(scenePath, true);
                EditorBuildSettings.scenes = newScenes;
            }
        }
    }
}
