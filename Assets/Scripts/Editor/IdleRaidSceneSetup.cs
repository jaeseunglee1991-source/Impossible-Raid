using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using BossRaid.Managers;
using BossRaid.Combat;
using BossRaid.UI;

/// <summary>
/// 에디터 상단 메뉴에서 한 번의 클릭으로 방치형+레이드 씬의 필수 오브젝트와 
/// 스크립트를 싹 다 자동으로 배포하고 연결해주는 자동화 툴입니다.
/// </summary>
public class IdleRaidSceneSetup : EditorWindow
{
    [MenuItem("Tools/1. Setup Idle Boss Raid Scene (방치형 씬 필수 매니저 셋업)")]
    public static void SetupScene()
    {
        // 안전 장치: 로그인 씬 등에서 실수로 실행하는 것 방지
        string activeSceneName = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().name;
        if (activeSceneName != "InGameScene")
        {
            EditorUtility.DisplayDialog("실행 오류", 
                $"현재 씬({activeSceneName})은 인게임 셋업 대상이 아닙니다.\n'InGameScene'을 열고 다시 실행해주세요.", "확인");
            return;
        }

        Debug.Log("<color=cyan>방치형 + 보스레이드 통합 씬 자동 구성을 시작합니다...</color>");

        GameObject coreManagers = GameObject.Find("CoreManagers");
        if (coreManagers == null)
        {
            coreManagers = new GameObject("CoreManagers");
            coreManagers.transform.SetAsFirstSibling();
        }

        var stageManager = GetOrAddComponent<StageManager>(coreManagers);
        GetOrAddComponent<GrowthManager>(coreManagers);
        GetOrAddComponent<BattleManager>(coreManagers);
        GetOrAddComponent<ReviveService>(coreManagers);

        GameObject idleSystem = GameObject.Find("IdleFarmingSystem_Zone");
        if (idleSystem == null) idleSystem = new GameObject("IdleFarmingSystem_Zone");

        GameObject raidSystem = GameObject.Find("BossRaidSystem_Zone");
        if (raidSystem == null) raidSystem = new GameObject("BossRaidSystem_Zone");

        stageManager.IdleMobSpawner = idleSystem;
        stageManager.BossRaidSystem = raidSystem;
        EditorUtility.SetDirty(stageManager);

        // 배경화면, 스폰 포인트, 프리팹, Canvas Scaler 자동 셋업 포함
        SetupBackground();
        SetupSpawnPoints();
        AssignPrefabs();
        SetupCanvasScalers();
        SetupSafeArea();

        Debug.Log("<color=green>✅ [1단계 완료] 씬 내 필수 매니저 프레임워크와 스크립트 연결이 완료되었습니다.</color>");
    }

    [MenuItem("Tools/2. Create 4 Classes & Assign Party (4개 직업 오브젝트 생성 및 부착)")]
    public static void CreateCharactersAndAssign()
    {
        Debug.Log("<color=cyan>4가지 직업 캐릭터를 생성하고 스크립트를 부착합니다...</color>");

        GameObject raidSystem = GameObject.Find("BossRaidSystem_Zone");
        if (raidSystem == null) raidSystem = new GameObject("BossRaidSystem_Zone");

        // 각각의 캐릭터 오브젝트 생성 및 스크립트 부착
        var warriorObj = CreateCharacter("Player_Warrior", typeof(BossRaid.Combat.Classes.Warrior), raidSystem.transform);
        var rogueObj = CreateCharacter("Player_Rogue", typeof(BossRaid.Combat.Classes.Rogue), raidSystem.transform);
        var mageObj = CreateCharacter("Player_Mage", typeof(BossRaid.Combat.Classes.Mage), raidSystem.transform);
        var healerObj = CreateCharacter("Player_Healer", typeof(BossRaid.Combat.Classes.Healer), raidSystem.transform);

        // 배틀 매니저에 파티 배치
        var battleManager = Object.FindFirstObjectByType<BattleManager>();
        if (battleManager != null)
        {
            battleManager.ActiveCharacters.Clear();

            // 파티에 캐릭터 할당
            var w = warriorObj.GetComponent<TagCharacterController>();
            var h = healerObj.GetComponent<TagCharacterController>();
            var r = rogueObj.GetComponent<TagCharacterController>();
            var m = mageObj.GetComponent<TagCharacterController>();

            if (w != null) battleManager.ActiveCharacters.Add(w);
            if (h != null) battleManager.ActiveCharacters.Add(h);
            if (r != null) battleManager.ActiveCharacters.Add(r);
            if (m != null) battleManager.ActiveCharacters.Add(m);

            EditorUtility.SetDirty(battleManager);
        }
        else
        {
            Debug.LogWarning("BattleManager를 찾을 수 없습니다. Tools/1번 메뉴를 먼저 실행해주세요.");
        }

        Debug.Log("<color=green>✅ [2단계 완료] 오브젝트 생성 -> 직업 스크립트 부착 -> Party 배열에 자동 할당까지 모두 완료되었습니다!</color>");
        Debug.Log("Hierarchy 창에 만들어진 Player 캐릭터들을 클릭해 인스펙터를 확인해보세요.");
    }

    [MenuItem("Tools/3. Setup Background (인게임 배경화면 설정)")]
    public static void SetupBackground()
    {
        string spritePath = "Assets/Resources/Sprites/Backgrounds/Ingame_Back.png";
        
        // 1. 이미지 임포트 설정 강제 (Sprite로 변경 및 Single 모드 설정)
        TextureImporter importer = AssetImporter.GetAtPath(spritePath) as TextureImporter;
        if (importer != null)
        {
            bool needsReimport = false;
            
            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                needsReimport = true;
            }
            
            if (importer.spriteImportMode != SpriteImportMode.Single)
            {
                importer.spriteImportMode = SpriteImportMode.Single;
                needsReimport = true;
            }

            if (needsReimport)
            {
                importer.SaveAndReimport();
                AssetDatabase.Refresh();
            }
        }

        GameObject bgObj = GameObject.Find("InGame_Background");
        if (bgObj == null)
        {
            bgObj = new GameObject("InGame_Background");
            bgObj.transform.position = new Vector3(0, 0, 0); // 스프라이트와 캐릭터들을 모두 0에 두고 SortingOrder로 관리
        }
        
        // 어떤 존(Zone)에도 속하지 않게 최상위로 이동
        bgObj.transform.SetParent(null); 

        var sr = GetOrAddComponent<SpriteRenderer>(bgObj);
        
        // Resources.Load는 확장자 없이 경로만 사용
        Sprite bgSprite = Resources.Load<Sprite>("Sprites/Backgrounds/Ingame_Back");
        
        if (bgSprite != null)
        {
            sr.sprite = bgSprite;
            sr.sortingOrder = -100; // 최하단 레이어
            
            // BackgroundScaler 컴포넌트 부착 — 런타임에 카메라 크기에 맞춰 자동 스케일링
            var bgScaler = GetOrAddComponent<BackgroundScaler>(bgObj);
            bgScaler.overscale = 1.05f;
            EditorUtility.SetDirty(bgScaler);
            
            // 에디터에서 미리보기용으로 임시 스케일 계산 (런타임에는 BackgroundScaler가 대체)
            Camera mainCam = Camera.main;
            if (mainCam != null && mainCam.orthographic)
            {
                float camH = mainCam.orthographicSize * 2f;
                float camW = camH * mainCam.aspect;
                float sprW = bgSprite.bounds.size.x;
                float sprH = bgSprite.bounds.size.y;
                float scale = Mathf.Max(camW / sprW, camH / sprH) * 1.05f;
                bgObj.transform.localScale = new Vector3(scale, scale, 1f);
            }
            else
            {
                bgObj.transform.localScale = new Vector3(1f, 1f, 1f);
            }
            
            Debug.Log("<color=green>✅ [배경화면] Ingame_Back 스프라이트 + BackgroundScaler 자동 스케일링이 적용되었습니다.</color>");
        }
        else
        {
            Debug.LogWarning("배경화면 스프라이트를 찾을 수 없습니다: Resources/Sprites/Backgrounds/Ingame_Back\n(파일이 실제 존재하는지, 그리고 Sprite로 설정되어 있는지 확인해주세요.)");
        }
    }

    [MenuItem("Tools/4. Setup Spawn Points (스폰 포인트 자동 연결)")]
    public static void SetupSpawnPoints()
    {
        var battleManager = Object.FindFirstObjectByType<BattleManager>();
        if (battleManager == null) return;

        // 1. 방치형 스폰 포인트 (Formation_Idle 하위)
        GameObject formationIdle = GameObject.Find("Formation_Idle");
        if (formationIdle == null) formationIdle = new GameObject("Formation_Idle");

        string[] pointNames = { "Point_Warrior", "Point_Rogue", "Point_Mage", "Point_Healer" };
        battleManager.idleSpawnPoints = new Transform[4];

        for (int i = 0; i < pointNames.Length; i++)
        {
            GameObject p = GameObject.Find(pointNames[i]);
            if (p == null)
            {
                p = new GameObject(pointNames[i]);
                p.transform.SetParent(formationIdle.transform);
                // 기본 십자 진형 배치
                Vector3 pos = Vector3.zero;
                if (i == 0) pos = new Vector3(0, -1.5f, 0); // 전사 (앞)
                if (i == 1) pos = new Vector3(-1.5f, 0, 0); // 도적 (좌)
                if (i == 2) pos = new Vector3(1.5f, 0, 0);  // 법사 (우)
                if (i == 3) pos = new Vector3(0, 1.5f, 0);  // 힐러 (뒤)
                p.transform.localPosition = pos;
            }
            battleManager.idleSpawnPoints[i] = p.transform;
        }

        // 2. 레이드 스폰 포인트 및 보스 포인트
        GameObject raidPoint = GameObject.Find("Point_Raid_Entry");
        if (raidPoint == null) raidPoint = new GameObject("Point_Raid_Entry");
        battleManager.raidSpawnPoint = raidPoint.transform;

        GameObject bossPoint = GameObject.Find("Point_Boss_Spawn");
        if (bossPoint == null) bossPoint = new GameObject("Point_Boss_Spawn");
        battleManager.bossSpawnPoint = bossPoint.transform;

        EditorUtility.SetDirty(battleManager);
        Debug.Log("<color=green>✅ [스폰 포인트] BattleManager에 모든 포인트가 자동 연결되었습니다.</color>");
    }

    [MenuItem("Tools/5. Assign Character Prefabs (프리팹 자동 연결)")]
    public static void AssignPrefabs()
    {
        var battleManager = Object.FindFirstObjectByType<BattleManager>();
        if (battleManager == null) return;

        // 유저가 설정한 특정 SPUM 프리팹 경로들 (Resources.Load용)
        battleManager.warriorPrefab = Resources.Load<GameObject>("Addons/BasicPack/2_Prefab/Elf/SPUM_20240911222346858");
        battleManager.roguePrefab   = Resources.Load<GameObject>("Addons/BasicPack/2_Prefab/Skelton/SPUM_20240911222823174");
        battleManager.magePrefab    = Resources.Load<GameObject>("Addons/BasicPack/2_Prefab/Elf/SPUM_20240911222451694");
        battleManager.healerPrefab  = Resources.Load<GameObject>("Addons/BasicPack/2_Prefab/Devil/SPUM_20240911215640476");

        // 보스 프리팹들도 자동 연결 (기존에 있다면)
        if (battleManager.idleBossPrefab == null)
            battleManager.idleBossPrefab = Resources.Load<GameObject>("Addons/BasicPack/2_Prefab/Devil/SPUM_20240911215637772");

        EditorUtility.SetDirty(battleManager);
        Debug.Log("<color=green>✅ [프리팹] 유저가 지정한 SPUM 캐릭터 프리팹들이 성공적으로 연결되었습니다.</color>");
    }

    private static GameObject CreateCharacter(string name, System.Type jobType, Transform parent)
    {
        GameObject obj = GameObject.Find(name);
        if (obj == null)
        {
            obj = new GameObject(name);
            obj.transform.SetParent(parent);
            obj.transform.position = new Vector3(Random.Range(-2f, 2f), Random.Range(-2f, 2f), 0f);
        }

        // 1. 직업 스크립트 (Warrior, Rogue 등) 부착 (CharacterBase를 상속받은 필수 컴포넌트)
        if (obj.GetComponent(jobType) == null) 
            obj.AddComponent(jobType);
        
        // 2. 자동/수동 제어 및 AI 구동 뼈대 (TagCharacterController) 부착
        if (obj.GetComponent<TagCharacterController>() == null)
            obj.AddComponent<TagCharacterController>();

        return obj;
    }

    private static T GetOrAddComponent<T>(GameObject target) where T : Component
    {
        T comp = target.GetComponent<T>();
        if (comp == null)
        {
            comp = target.AddComponent<T>();
        }
        return comp;
    }

    /// <summary>
    /// 씬 내 모든 Canvas의 CanvasScaler를 안드로이드 가로모드(1920x1080)에 최적화된 설정으로 일괄 수정합니다.
    /// - Scale Mode: Scale With Screen Size
    /// - Reference Resolution: 1920 x 1080
    /// - Screen Match Mode: Match Width Or Height
    /// - Match: 0.5 (가로/세로 균형) — 가로형 게임에서 다양한 비율에 안정적
    /// </summary>
    [MenuItem("Tools/6. Fix All Canvas Scalers (모든 캔버스 스케일러 일괄 수정 - 안드로이드 가로모드)")]
    public static void SetupCanvasScalers()
    {
        Canvas[] allCanvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        
        if (allCanvases.Length == 0)
        {
            Debug.LogWarning("씬에 Canvas가 하나도 없습니다. UI를 먼저 생성해주세요.");
            return;
        }

        int fixedCount = 0;
        foreach (Canvas canvas in allCanvases)
        {
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = canvas.gameObject.AddComponent<CanvasScaler>();
            }

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f; // 가로형 게임: 0.5로 가로/세로 균형 잡기 (다양한 비율 대응)
            scaler.referencePixelsPerUnit = 100f;

            EditorUtility.SetDirty(scaler);
            fixedCount++;
            Debug.Log($"  ✔ Canvas '{canvas.gameObject.name}' → ScaleWithScreenSize (1920x1080, Match=0.5)");
        }

        Debug.Log($"<color=green>✅ [Canvas Scaler] 총 {fixedCount}개의 Canvas Scaler가 안드로이드 가로모드(1920x1080)에 맞게 수정되었습니다.</color>");
    }

    /// <summary>
    /// 씬 내 모든 Canvas에 SafeArea 래퍼 오브젝트를 생성하여
    /// 노치/카메라 구멍/하단 네비게이션 바를 피해 UI를 배치합니다.
    /// </summary>
    [MenuItem("Tools/7. Setup Safe Area (노치 및 하단 바 대응 - 모든 Canvas)")]
    public static void SetupSafeArea()
    {
        Canvas[] allCanvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        
        if (allCanvases.Length == 0)
        {
            Debug.LogWarning("씬에 Canvas가 하나도 없습니다. UI를 먼저 생성해주세요.");
            return;
        }

        int setupCount = 0;
        foreach (Canvas canvas in allCanvases)
        {
            // 이미 SafeArea가 있는지 확인
            Transform existing = canvas.transform.Find("SafeArea");
            if (existing != null)
            {
                // 컴포넌트만 확인해서 붙여주기
                if (existing.GetComponent<SafeAreaAdapter>() == null)
                    existing.gameObject.AddComponent<SafeAreaAdapter>();
                EditorUtility.SetDirty(existing.gameObject);
                setupCount++;
                continue;
            }

            // SafeArea 래퍼 오브젝트 생성
            GameObject safeAreaObj = new GameObject("SafeArea");
            safeAreaObj.transform.SetParent(canvas.transform, false);

            RectTransform safeRect = safeAreaObj.AddComponent<RectTransform>();
            safeRect.anchorMin = Vector2.zero;
            safeRect.anchorMax = Vector2.one;
            safeRect.offsetMin = Vector2.zero;
            safeRect.offsetMax = Vector2.zero;

            safeAreaObj.AddComponent<SafeAreaAdapter>();

            // 기존 Canvas 자식들을 SafeArea 하위로 이동
            // (새로 만들어진 SafeArea 자체는 제외)
            int childCount = canvas.transform.childCount;
            for (int i = childCount - 1; i >= 0; i--)
            {
                Transform child = canvas.transform.GetChild(i);
                if (child != safeAreaObj.transform)
                {
                    child.SetParent(safeRect, true);
                }
            }

            // SafeArea를 Canvas의 첫 번째 자식으로 배치
            safeAreaObj.transform.SetAsFirstSibling();

            EditorUtility.SetDirty(canvas.gameObject);
            setupCount++;
            Debug.Log($"  ✔ Canvas '{canvas.gameObject.name}' → SafeArea 래퍼 생성 완료 (기존 UI 자동 이동)");
        }

        Debug.Log($"<color=green>✅ [Safe Area] 총 {setupCount}개 Canvas에 SafeAreaAdapter가 적용되었습니다. 노치/하단바 자동 회피!</color>");
    }
}
