using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using BossRaid.Combat;
using BossRaid.Equipment;
using BossRaid.Gacha;
 
namespace BossRaid.Managers
{
    /// ──────────────────────────────────────────────────────────────────
    /// InventoryManager — 장비 인벤토리 + 장착 + 강화 + 스탯 적용 통합 관리
    ///
    /// ■ 인벤토리  : 최대 60칸. 인스턴스 ID로 아이템 조회.
    /// ■ 장착 슬롯 : 캐릭터별 Weapon / Armor / Accessory 3슬롯
    /// ■ 스탯 적용 : CharacterBase의 maxHealth, autoAttackDamage 등 직접 수정
    ///              장착 변경 시 이전 장비 보너스를 빼고 새 보너스를 더함
    /// ■ 강화      : GrowthManager.SpendGold()로 비용 차감 후 enhanceLevel +1
    /// ■ 드랍      : StageManager.OnMobKilled() / OnBossDefeated()에서 TryDrop() 호출
    /// ■ 저장      : SaveManager → PlayerSaveData.inventoryItems / equippedGear
    ///
    /// [씬 배치]
    ///   빈 GameObject에 이 컴포넌트 부착. DontDestroyOnLoad로 씬 전환 유지.
    /// ──────────────────────────────────────────────────────────────────
    /// </summary>
    public class InventoryManager : MonoBehaviour
    {
        private static InventoryManager _instance;
        public static InventoryManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    // 씬에서 먼저 찾아봅니다.
                    _instance = FindFirstObjectByType<InventoryManager>();
 
                    // 씬에 없다면 새로 생성합니다 (자동 연동)
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("@InventoryManager");
                        _instance = go.AddComponent<InventoryManager>();
                        Debug.Log("<color=yellow>[InventoryManager] 자동으로 생성되었습니다.</color>");
                    }
                }
                return _instance;
            }
        }
 
        // ═══════════════════════════════════════════════════════════
        //  상수
        // ═══════════════════════════════════════════════════════════
 
        public const int MAX_INVENTORY_SIZE = 60;
        public const int GEAR_SLOTS = 3; // Weapon, Armor, Accessory. 로직 내부용
 
        // ═══════════════════════════════════════════════════════════
        //  인벤토리 데이터
        // ═══════════════════════════════════════════════════════════
 
        // instanceId → EquipmentData
        private readonly Dictionary<string, EquipmentData> _items
            = new Dictionary<string, EquipmentData>();
 
        /// <summary>현재 보유 아이템 수.</summary>
        public int Count => _items.Count;
        public bool IsFull => _items.Count >= MAX_INVENTORY_SIZE;
 
        // ─────────────────────────────────────────
        //  장착 슬롯 : [characterName][slotIndex] = instanceId (null/빈 문자열 = 미장착)
        //  배열 대신 List 사용 (세이브 데이터 호환성 대응)
        // ─────────────────────────────────────────
 
        private readonly Dictionary<string, List<string>> _equippedGear
            = new Dictionary<string, List<string>>();
 
        // [신규] 장착 중인 아이템의 모디파이어 추적 (인스턴스ID -> 모디파이어 리스트)
        // 캐릭터당 여러 슬롯 모디파이어를 관리하기 위해 복합 키 사용: characterName_instanceId
        private readonly Dictionary<string, List<StatModifier>> _activeModifiers 
            = new Dictionary<string, List<StatModifier>>();
 
        // ─────────────────────────────────────────
        //  이벤트 (UI 구독용)
        // ─────────────────────────────────────────
 
        /// <summary>새 장비 획득 시 (item)</summary>
        public event Action<EquipmentData> OnItemAdded;
 
        /// <summary>장착 변경 시 (characterName, slotIndex, newItem or null)</summary>
        public event Action<string, int, EquipmentData> OnGearChanged;
 
        /// <summary>강화 완료 시 (item)</summary>
        /// <summary>강화 완료 시 (item)</summary>
        public event Action<EquipmentData> OnItemEnhanced;
 
        // ─────────────────────────────────────────
        //  [가챠] 무기 인벤토리 (종류별 1개 항목, 중복 수량으로 강화)
        // ─────────────────────────────────────────
 
        private readonly List<InventoryWeapon> _weaponInventory = new List<InventoryWeapon>();
 
        /// <summary>가챠 무기 강화 완료 시 발생. 연쇄 강화 후 단 1회 호출됨.</summary>
        public event Action<InventoryWeapon> OnWeaponEnhanced;
 
        // ═══════════════════════════════════════════════════════════
        //  생명주기
        // ═══════════════════════════════════════════════════════════
        }
 
        // ═══════════════════════════════════════════════════════════
        //  인벤토리 CRUD
        // ═══════════════════════════════════════════════════════════
 
        /// <summary>
        /// 인벤토리에 장비를 추가합니다.
        /// 인벤토리가 꽉 찼으면 false를 반환합니다.
        /// </summary>
        public bool AddItem(EquipmentData item)
        {
            if (item == null || IsFull) return false;
            if (_items.ContainsKey(item.instanceId)) return false;
 
            _items[item.instanceId] = item;
            OnItemAdded?.Invoke(item);
            SaveManager.Instance?.MarkDirty();
 
            Debug.Log($"<color=cyan>[인벤토리] {item.FullName} 획득! " +
                      $"({Count}/{MAX_INVENTORY_SIZE})</color>");
            return true;
        }
 
        /// <summary>instanceId로 아이템을 조회합니다.</summary>
        public EquipmentData GetItem(string instanceId)
        {
            if (string.IsNullOrEmpty(instanceId)) return null;
            _items.TryGetValue(instanceId, out var item);
            return item;
        }
 
        /// <summary>전체 아이템 목록을 반환합니다. (UI 리스트 갱신용)</summary>
        public List<EquipmentData> GetAllItems()
            => new List<EquipmentData>(_items.Values);
 
        /// <summary>슬롯 필터 아이템 목록.</summary>
        public List<EquipmentData> GetItemsBySlot(EquipSlot slot)
            => _items.Values.Where(x => x.slot == slot).ToList();
 
        /// <summary>아이템을 인벤토리에서 제거합니다. 장착 중이면 먼저 해제합니다.</summary>
        public void RemoveItem(string instanceId)
        {
            if (!_items.ContainsKey(instanceId)) return;
 
            // 장착 중인 캐릭터 찾아 해제
            foreach (var kv in _equippedGear)
            {
                for (int i = 0; i < kv.Value.Count; i++)
                {
                    if (kv.Value[i] == instanceId)
                    {
                        UnequipGear(kv.Key, i);
                        break;
                    }
                }
            }
 
            _items.Remove(instanceId);
            SaveManager.Instance?.MarkDirty();
        }
 
        // ═══════════════════════════════════════════════════════════
        //  장착 / 해제
        // ═══════════════════════════════════════════════════════════
 
        /// <summary>
        /// characterName 캐릭터의 slotIndex(0=무기, 1=방어구, 2=장신구)에
        /// instanceId 장비를 장착합니다.
        ///
        /// - 슬롯 타입 불일치 시 false 반환
        /// - 기존 장착 장비 자동 해제 (스탯 환원)
        /// - 새 장비 스탯 적용
        /// </summary>
        public bool EquipGear(string characterName, string instanceId)
        {
            if (!_items.TryGetValue(instanceId, out var item)) return false;
 
            CharacterBase character = FindCharacter(characterName);
            if (character == null) return false;
 
            int slotIndex = (int)item.slot;
            EnsureGearSlots(characterName);
 
            // 기존 장착 해제
            string prevId = _equippedGear[characterName][slotIndex];
            if (!string.IsNullOrEmpty(prevId))
                RemoveGearModifiers(character, prevId);
 
            // 새 장비 장착
            _equippedGear[characterName][slotIndex] = instanceId;
            ApplyGearModifiers(character, item);
            OnGearChanged?.Invoke(characterName, slotIndex, item);
            SaveManager.Instance?.MarkDirty();
 
            Debug.Log($"<color=yellow>[장착] {characterName} ← {item.FullName}" +
                      $" [{item.slot}]</color>");
            return true;
        }
 
        /// <summary>캐릭터의 슬롯 장비를 해제합니다.</summary>
        public void UnequipGear(string characterName, int slotIndex)
        {
            if (!_equippedGear.TryGetValue(characterName, out var slots)) return;
            if (slotIndex >= slots.Count) return; // 안전 검사
 
            string prevId = slots[slotIndex];
            if (string.IsNullOrEmpty(prevId)) return;
 
            CharacterBase character = FindCharacter(characterName);
            if (character != null)
                RemoveGearModifiers(character, prevId);
 
            slots[slotIndex] = string.Empty; // 빈 문자열로 초기화
            OnGearChanged?.Invoke(characterName, slotIndex, null);
            SaveManager.Instance?.MarkDirty();
        }
 
        /// <summary>characterName 캐릭터의 slotIndex에 장착된 장비를 반환합니다.</summary>
        public EquipmentData GetEquippedGear(string characterName, int slotIndex)
        {
            if (!_equippedGear.TryGetValue(characterName, out var slots)) return null;
            if (slotIndex >= slots.Count) return null; // 안전 검사
            
            string id = slots[slotIndex];
            return !string.IsNullOrEmpty(id) ? GetItem(id) : null;
        }
 
        // ═══════════════════════════════════════════════════════════
        //  강화 (Enhance)
        // ═══════════════════════════════════════════════════════════
 
        /// <summary>
        /// 장비를 강화합니다. (+0 → +1 → ... → +10)
        ///
        /// 1. 골드 차감 (GrowthManager.SpendGold)
        /// 2. enhanceLevel + 1
        /// 3. 장착 중인 경우 스탯 차이(delta)를 CharacterBase에 즉시 적용
        /// 4. SaveManager.MarkDirty()
        /// </summary>
        public bool Enhance(string instanceId)
        {
            if (!_items.TryGetValue(instanceId, out var item)) return false;
            if (item.enhanceLevel >= EquipmentData.MAX_ENHANCE)
            {
                Debug.Log($"[강화] {item.FullName}은 이미 최대 강화입니다.");
                return false;
            }
 
            double cost = item.NextEnhanceCost;
            if (GrowthManager.Instance == null || !GrowthManager.Instance.SpendGold(cost))
            {
                Debug.Log($"[강화] 골드 부족. 필요: {cost:F0}");
                return false;
            }
 
            // 강화 시 모디파이어 갱신을 위해 재장착 로직 수행
            foreach (var kv in _equippedGear)
            {
                for (int i = 0; i < kv.Value.Count; i++)
                {
                    if (kv.Value[i] == instanceId)
                    {
                        CharacterBase ch = FindCharacter(kv.Key);
                        if (ch != null)
                        {
                            RemoveGearModifiers(ch, instanceId);
                            ApplyGearModifiers(ch, item);
                        }
                    }
                }
            }
 
            OnItemEnhanced?.Invoke(item);
            SaveManager.Instance?.MarkDirty();
 
            Debug.Log($"<color=green>[강화] {item.FullName} 강화 성공! " +
                      $"(비용: {cost:F0}G)</color>");
            return true;
        }
 
        // ═══════════════════════════════════════════════════════════
        //  드랍 처리
        // ═══════════════════════════════════════════════════════════
 
        /// <summary>
        /// 몹 처치 시 확률 드랍.
        /// StageManager.OnMobKilled()에서 호출합니다.
        ///
        /// 사용 예:
        ///   InventoryManager.Instance?.TryDrop(CurrentStageLevel);
        /// </summary>
        public void TryDrop(int stageLevel)
        {
            if (IsFull) return;
            if (UnityEngine.Random.value > DropTable.MOB_DROP_CHANCE) return;
 
            var item = DropTable.Roll(stageLevel, isBossDrop: false);
            AddItem(item);
        }
 
        /// <summary>
        /// 보스 처치 시 확정 드랍 (최소 Rare 보장).
        /// StageManager.OnBossDefeated()에서 호출합니다.
        /// </summary>
        public void DropFromBoss(int stageLevel)
        {
            if (IsFull) { Debug.Log("[드랍] 인벤토리가 꽉 찼습니다!"); return; }
 
            var item = DropTable.Roll(stageLevel, isBossDrop: true);
            AddItem(item);
 
            Debug.Log($"<color=orange>[보스 드랍] {item.FullName} 획득!</color>");
        }
 
        // ═══════════════════════════════════════════════════════════
        //  스탯 적용 내부 로직
        // ═══════════════════════════════════════════════════════════
 
        private void ApplyGearModifiers(CharacterBase ch, EquipmentData item)
        {
            string key = $"{ch.characterName}_{item.instanceId}";
            var modifiers = new List<StatModifier>();
            var s = item.FinalStat;
 
            // 1. 공격력 (Flat)
            if (s.bonusAttack > 0)
                modifiers.Add(new StatModifier(s.bonusAttack, StatModType.Flat, item));
            
            // 2. HP (Flat + PercentAdd)
            if (s.bonusHpFlat > 0)
                modifiers.Add(new StatModifier(s.bonusHpFlat, StatModType.Flat, item));
            if (s.bonusHpPercent > 0)
                modifiers.Add(new StatModifier(s.bonusHpPercent, StatModType.PercentAdd, item));
 
            // 3. 공격 속도 (PercentAdd) - 0.05 = 5% 증가
            if (s.bonusAttackSpeed > 0)
                modifiers.Add(new StatModifier(s.bonusAttackSpeed, StatModType.PercentAdd, item));
 
            // 4. 피해 경감 (PercentAdd)
            if (s.bonusDmgReduction > 0)
                modifiers.Add(new StatModifier(s.bonusDmgReduction, StatModType.PercentAdd, item));
 
            // 실제 캐릭터에게 적용
            foreach(var mod in modifiers)
            {
                if (mod.Type == StatModType.Flat || mod.Type == StatModType.PercentAdd)
                {
                    // StatBonus 필드 이름에 따라 매핑
                    if (mod.Value == s.bonusAttack) ch.AddStatModifier(CharacterBase.StatType.AttackDamage, mod);
                    else if (mod.Value == s.bonusHpFlat || mod.Value == s.bonusHpPercent) ch.AddStatModifier(CharacterBase.StatType.MaxHP, mod);
                    else if (mod.Value == s.bonusAttackSpeed) ch.AddStatModifier(CharacterBase.StatType.AttackSpeed, mod);
                    else if (mod.Value == s.bonusDmgReduction) ch.AddStatModifier(CharacterBase.StatType.DamageReduction, mod);
                }
            }
 
            _activeModifiers[key] = modifiers;
        }
 
        private void RemoveGearModifiers(CharacterBase ch, string instanceId)
        {
            string key = $"{ch.characterName}_{instanceId}";
            if (!_activeModifiers.TryGetValue(key, out var modifiers)) return;
 
            foreach (var mod in modifiers)
            {
                // Source객체 매칭으로 일괄 제거 (더 안전함)
                ch.RemoveAllModifiersFromSource(mod.Source);
            }
 
            _activeModifiers.Remove(key);
        }
 
        // ═══════════════════════════════════════════════════════════
        //  SaveManager 연동 (직렬화 / 복원)
        // ═══════════════════════════════════════════════════════════
 
        /// <summary>SaveManager.CollectSaveData()에서 호출합니다.</summary>
        public void CollectToSaveData(PlayerSaveData data)
        {
            // 인벤토리 아이템 직렬화
            data.inventoryItems.Clear();
            foreach (var item in _items.Values)
                data.inventoryItems.Add(new InventoryItemEntry(item));
 
            // 장착 슬롯 직렬화
            data.gearSlots.Clear();
            foreach (var kv in _equippedGear)
            {
                data.gearSlots.Add(new GearSlotEntry
                {
                    characterName = kv.Key,
                    slotIds       = new List<string>(kv.Value)
                });
            }
        }
 
        /// <summary>SaveManager.ApplySaveData()에서 호출합니다.</summary>
        public void RestoreFromSaveData(PlayerSaveData data)
        {
            _items.Clear();
            _equippedGear.Clear();
 
            // 아이템 복원
            foreach (var entry in data.inventoryItems)
            {
                var item = entry.ToEquipmentData();
                if (item != null) _items[item.instanceId] = item;
            }
 
            // 장착 슬롯 복원 (CharacterBase가 씬에 있을 때 스탯도 재적용)
            foreach (var entry in data.gearSlots)
            {
                EnsureGearSlots(entry.characterName);
                
                // 기존의 GEAR_SLOTS 크기에 맞춰 채워넣어 이전 세이브파일 구조 유지
                for (int i = 0; i < entry.slotIds.Count && i < GEAR_SLOTS; i++)
                {
                    _equippedGear[entry.characterName][i] = entry.slotIds[i];
                }
 
                CharacterBase ch = FindCharacter(entry.characterName);
                if (ch == null) continue;
 
                for (int i = 0; i < _equippedGear[entry.characterName].Count; i++)
                {
                    string id = _equippedGear[entry.characterName][i];
                    if (!string.IsNullOrEmpty(id) && _items.TryGetValue(id, out var item))
                        ApplyStatBonus(ch, item.FinalStat);
                }
            }
 
            Debug.Log($"<color=cyan>[인벤토리] {_items.Count}개 아이템 복원 완료</color>");
        }
 
        // ═══════════════════════════════════════════════════════════
        //  내부 유틸
        // ═══════════════════════════════════════════════════════════
 
        private void EnsureGearSlots(string characterName)
        {
            if (!_equippedGear.ContainsKey(characterName))
            {
                var emptySlots = new List<string>();
                for (int i = 0; i < GEAR_SLOTS; i++) emptySlots.Add(string.Empty);
                _equippedGear[characterName] = emptySlots;
            }
        }
 
        private CharacterBase FindCharacter(string characterName)
        {
            if (CombatManager.Instance != null)
            {
                foreach (var p in CombatManager.Instance.activePlayers)
                    if (p != null && p.characterName == characterName) return p;
            }
            // 전투 외 씬 (로비 등)에서는 씬 전체 탐색
            foreach (var ch in UnityEngine.Object.FindObjectsByType<CharacterBase>(FindObjectsSortMode.None))
                if (ch.characterName == characterName) return ch;
            return null;
        }
 
        // ═══════════════════════════════════════════════════════════
        //  에디터 테스트 유틸
        // ═══════════════════════════════════════════════════════════
 
        [ContextMenu("Test: 전설 무기 추가")]
        private void TestAddLegendaryWeapon()
        {
            var item = EquipmentData.Generate("dragon_staff", EquipSlot.Weapon,
                                              EquipRarity.Legendary, stageLevel: 10);
            AddItem(item);
        }
        //  에디터 테스트 유틸
        // ═══════════════════════════════════════════════════════════
 
        // ═══════════════════════════════════════════════════════════
        //  가챠 무기 인벤토리 (InventoryWeapon 기반)
        // ═══════════════════════════════════════════════════════════
 
        /// <summary>
        /// GachaManager에서 호출. 뽑힌 무기 목록을 일괄 처리한다.
        /// 종류별 중복 수량을 합산한 뒤 WeaponForgeManager에 강화 체크를 위임한다.
        /// </summary>
        public void AddWeaponsFromGacha(List<WeaponData> results)
        {
            if (results == null || results.Count == 0) return;
 
            foreach (var data in results)
            {
                if (data == null) continue;
                InventoryWeapon entry = FindWeaponEntry(data.weaponId);
 
                if (entry == null)
                {
                    entry = new InventoryWeapon(data);
                    _weaponInventory.Add(entry);
                    // 최초 획득도 duplicateAmount 1로 시작해 강화 체크
                    entry.duplicateAmount += 1;
                }
                else
                {
                    entry.duplicateAmount += 1;
                }
 
                // 수량 추가 즉시 강화 가능 여부 체크 (연쇄 강화 포함)
                WeaponForgeManager.Instance?.TryEnhanceWeapon(entry, OnWeaponEnhanced);
            }
        }
 
        /// <summary>가챠 무기 인벤토리 전체 목록 반환 (UI 갱신용).</summary>
        public List<InventoryWeapon> GetWeaponInventory()
            => new List<InventoryWeapon>(_weaponInventory);
 
        private InventoryWeapon FindWeaponEntry(int weaponId)
        {
            for (int i = 0; i < _weaponInventory.Count; i++)
                if (_weaponInventory[i].weaponData.weaponId == weaponId)
                    return _weaponInventory[i];
            return null;
        }
 
        // ═══════════════════════════════════════════════════════════
        //  에디터 테스트 유틸
        // ═══════════════════════════════════════════════════════════
 
        [ContextMenu("Test: 전설 무기 추가")]
        private void TestAddLegendaryWeapon()
        {
                Debug.Log($"  {item.FullName} ({item.rarity}) — {item.StatSummary}");
        }
    }
 
    // ═══════════════════════════════════════════════════════════
    //  InventoryWeapon — 가챠 무기 인벤토리 항목
    //  (종류별로 1개만 존재하며, 중복 획득은 duplicateAmount로 축적된다)
    // ═══════════════════════════════════════════════════════════
 
    [Serializable]
    public class InventoryWeapon
    {
        public WeaponData weaponData;
        public int   enhanceLevel;
        public int   duplicateAmount;
        public float currentAttack;
 
        public InventoryWeapon(WeaponData data)
        {
            weaponData     = data;
            enhanceLevel   = 0;
            duplicateAmount = 0;
            currentAttack  = data.baseAttack;
        }
 
        public string DisplayName
            => weaponData != null ? $"{weaponData.weaponName} +{enhanceLevel}" : "알 수 없는 무기";
    }
}
