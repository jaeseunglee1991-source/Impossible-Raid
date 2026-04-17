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

    private static T GetOrAddComponent<T>(GameObject target) where T : MonoBehaviour
    {
        T comp = target.GetComponent<T>();
        if (comp == null)
        {
            comp = target.AddComponent<T>();
        }
        return comp;
    }
}
