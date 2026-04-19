using UnityEngine;
using UnityEditor;
using BossRaid.Combat;
using BossRaid.Combat.Classes;

namespace BossRaid.Editor
{
    public class CharacterPrefabFixer : EditorWindow
    {
        [MenuItem("Impossible Raid/Fix All Character Prefabs")]
        public static void FixPrefabs()
        {
            Debug.Log("=== Character Prefab Fixer Started ===");
            
            // 모든 프리팹 애셋을 찾습니다.
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });
            int fixedCount = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                // 이름에 SPUM이 포함되어 있거나 이미 SPUM_Prefabs가 있는 경우 대상포함
                if (prefab.name.Contains("SPUM") || prefab.GetComponentInChildren<SPUM_Prefabs>() != null)
                {
                    bool isModified = false;

                    // 1. SPUMAnimationBridge 추가
                    if (prefab.GetComponent<SPUMAnimationBridge>() == null)
                    {
                        prefab.AddComponent<SPUMAnimationBridge>();
                        isModified = true;
                    }

                    // 2. TagCharacterController 추가
                    if (prefab.GetComponent<TagCharacterController>() == null)
                    {
                        prefab.AddComponent<TagCharacterController>();
                        isModified = true;
                    }

                    // 3. 직업 클래스 추가 (이름 기반 추측)
                    if (prefab.GetComponent<CharacterBase>() == null)
                    {
                        if (prefab.name.Contains("Warrior")) prefab.AddComponent<Warrior>();
                        else if (prefab.name.Contains("Mage")) prefab.AddComponent<Mage>();
                        else if (prefab.name.Contains("Rogue")) prefab.AddComponent<Rogue>();
                        else if (prefab.name.Contains("Healer")) prefab.AddComponent<Healer>();
                        
                        isModified = true;
                    }

                    if (isModified)
                    {
                        EditorUtility.SetDirty(prefab);
                        fixedCount++;
                        Debug.Log($"[Fixer] Fixed Prefab: {prefab.name}");
                    }
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"=== Character Prefab Fixer Finished. Total fixed: {fixedCount} ===");
            
            EditorUtility.DisplayDialog("Prefab Fixer", $"Successfully fixed {fixedCount} character prefabs!", "OK");
        }
    }
}
