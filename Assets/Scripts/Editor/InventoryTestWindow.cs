using UnityEngine;
using UnityEditor;
using BossRaid.Managers;
using BossRaid.Equipment;

namespace BossRaid.Editor
{
    public class InventoryTestWindow : EditorWindow
    {
        [MenuItem("Impossible Raid/Inventory Tester")]
        public static void ShowWindow()
        {
            GetWindow<InventoryTestWindow>("Inventory Tester");
        }

        private void OnGUI()
        {
            GUILayout.Label("Inventory System Tester", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("테스트를 하려면 [Play] 버튼을 눌러 게임을 실행해주세요.", MessageType.Warning);
                return;
            }

            if (GUILayout.Button("전설 등급 무기 추가 (Dragon Staff)", GUILayout.Height(30)))
            {
                var item = EquipmentData.Generate("dragon_staff", EquipSlot.Weapon, EquipRarity.Legendary, 10);
                InventoryManager.Instance.AddItem(item);
                Debug.Log("<color=cyan>[Tester] 전설 무기를 추가했습니다.</color>");
            }

            if (GUILayout.Button("보스 드랍 랜덤 생성 (스테이지 10)", GUILayout.Height(30)))
            {
                InventoryManager.Instance.DropFromBoss(10);
            }

            EditorGUILayout.Space();
            if (GUILayout.Button("인벤토리 목록 로그 출력", GUILayout.Height(20)))
            {
                var items = InventoryManager.Instance.GetAllItems();
                Debug.Log($"현재 인벤토리 아이템 개수: {items.Count}");
                foreach(var item in items)
                {
                    Debug.Log($"- {item.FullName} ({item.rarity})");
                }
            }
        }
    }
}
