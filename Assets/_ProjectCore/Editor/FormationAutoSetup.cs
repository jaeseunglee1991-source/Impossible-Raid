using UnityEngine;
using UnityEditor;
using BossRaid.Managers;
using System.Collections.Generic;

public class FormationAutoSetup : EditorWindow
{
    [MenuItem("Tools/Impossible Raid/Clean and Setup Spawn Points")]
    public static void Setup()
    {
        // 1. BattleManager 찾기
        BattleManager bm = FindObjectOfType<BattleManager>();
        if (bm == null)
        {
            EditorUtility.DisplayDialog("Error", "BattleManager를 씬에서 찾을 수 없습니다!", "OK");
            return;
        }

        // 2. 기존 SpwanPoint 또는 Formation 부모 오브젝트 찾기
        GameObject spawnParent = GameObject.Find("Formation_Idle");
        if (spawnParent == null) spawnParent = GameObject.Find("SpwanPoint");

        // 만약 부모가 없다면 생성
        if (spawnParent == null)
        {
            spawnParent = new GameObject("Formation_Idle");
        }
        else
        {
            spawnParent.name = "Formation_Idle"; // 이름 통일
        }

        // 3. 부모 좌표 초기화 (중요: 상용 표준)
        spawnParent.transform.position = Vector3.zero;
        spawnParent.transform.localScale = Vector3.one;

        // 4. 기존 자식들(가짜 캐릭터 등) 청소
        List<GameObject> children = new List<GameObject>();
        foreach (Transform child in spawnParent.transform) children.Add(child.gameObject);
        children.ForEach(child => DestroyImmediate(child));

        // 5. 황금 진형 좌표 설정 (다이아몬드 진형)
        // 씬 중앙(0,0) 근처로 설정하되, 각자 2유닛 정도 벌림
        Vector3[] offsetPositions = new Vector3[]
        {
            new Vector3(0, 1.5f, 0),   // Mage (뒤쪽 중앙)
            new Vector3(-2, 0, 0),     // Rogue (왼쪽)
            new Vector3(2, 0, 0),      // Healer (오른쪽)
            new Vector3(0, -1.5f, 0)   // Warrior (앞쪽 중앙)
        };

        string[] pointNames = { "Point_Mage", "Point_Rogue", "Point_Healer", "Point_Warrior" };
        Transform[] newPoints = new Transform[4];

        for (int i = 0; i < 4; i++)
        {
            GameObject p = new GameObject(pointNames[i]);
            p.transform.SetParent(spawnParent.transform);
            p.transform.localPosition = offsetPositions[i];
            newPoints[i] = p.transform;
            
            // 기즈모 선택이 잘 되도록 아이콘 설정 (유니티 빌트인 아이콘)
            var icon = EditorGUIUtility.IconContent("sv_label_0").image as Texture2D;
            EditorGUIUtility.SetIconForObject(p, icon);
        }

        // 6. BattleManager에 자동 할당
        Undo.RecordObject(bm, "Setup Spawn Points");
        bm.idleSpawnPoints = newPoints;

        // 7. 결과 보고
        EditorUtility.SetDirty(bm);
        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog("성공", "진형 세팅이 완료되었습니다!\n1. 가짜 캐릭터 제거 완료\n2. 4개 스폰포인트 생성 완료\n3. BattleManager 자동 연결 완료", "확인");
    }
}
