using UnityEngine;
using UnityEditor;
using BossRaid.Managers;
using BossRaid.Combat;

/// <summary>
/// 에디터 상단 메뉴에서 한 번의 클릭으로 방치형+레이드 씬의 필수 오브젝트와 
/// 스크립트를 싹 다 자동으로 배포하고 연결해주는 자동화 툴입니다.
/// </summary>
public class IdleRaidSceneSetup : EditorWindow
{
    [MenuItem("Tools/1. Setup Idle Boss Raid Scene (방치형 씬 필수 매니저 셋업)")]
    public static void SetupScene()
    {
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

        // 배경화면, 스폰 포인트, 프리팹 자동 셋업 포함
        SetupBackground();
        SetupSpawnPoints();
        AssignPrefabs();

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
            
            // 배경 스케일을 원본 크기(1:1)로 설정 (필요시 에디터에서 직접 조절 권장)
            bgObj.transform.localScale = new Vector3(1f, 1f, 1f);  
            
            Debug.Log("<color=green>✅ [배경화면] Ingame_Back 스프라이트가 적용되었습니다. (임포트 설정 자동 수정 완료)</color>");
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
}
