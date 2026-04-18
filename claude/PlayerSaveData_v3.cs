using System;
using System.Collections.Generic;

namespace BossRaid.Managers
{
    /// <summary>
    /// ──────────────────────────────────────────────────────────────────
    /// PlayerSaveData — 저장/불러오기 데이터 구조
    ///
    /// ■ JsonUtility로 직렬화 → AES 암호화(SecurePlayerPrefs) → PlayerPrefs 저장
    /// ■ 저장 항목: 골드, 강화 레벨(캐릭터별), 스테이지, 버전 정보
    /// ■ 세이브 버전 관리로 업데이트 시 마이그레이션 지원
    /// ──────────────────────────────────────────────────────────────────
    /// </summary>
    [Serializable]
    public class PlayerSaveData
    {
        // ─────────────────────────────────────────
        //  메타 (버전 관리)
        // ─────────────────────────────────────────

        /// <summary>
        /// 세이브 파일 버전. 업데이트로 구조가 바뀔 때 이 값을 올리면
        /// SaveManager가 자동으로 마이그레이션/초기화 처리합니다.
        /// </summary>
        public int saveVersion = SaveManager.CURRENT_SAVE_VERSION;

        /// <summary>마지막 저장 시각 (UTC ISO 8601). 무결성 감사용.</summary>
        public string savedAtUtc = "";

        // ─────────────────────────────────────────
        //  재화
        // ─────────────────────────────────────────

        public double gold = 0.0;

        // ─────────────────────────────────────────
        //  스테이지
        // ─────────────────────────────────────────

        public int stageLevel    = 1;
        public int mobsKilled    = 0;

        // ─────────────────────────────────────────
        //  강화 레벨 (캐릭터별 StatUpgrade)
        //
        //  키 규칙: "{CharacterRole}_{statName}"
        //  예시: "Tank_기본 공격력", "Healer_치유량"
        //
        //  Dictionary는 JsonUtility가 직렬화하지 못하므로
        //  Key/Value 쌍 리스트로 저장합니다.
        // ─────────────────────────────────────────

        public List<UpgradeSaveEntry> upgrades = new List<UpgradeSaveEntry>();

        // ─────────────────────────────────────────
        //  스킬 장착 슬롯
        //
        //  키 규칙: "{characterName}_slot{N}"
        //  값: allSkills 인덱스 (int). -1 = 빈 슬롯
        // ─────────────────────────────────────────

        public List<EquipSaveEntry> equippedSkills = new List<EquipSaveEntry>();

        // ─────────────────────────────────────────
        //  장비 인벤토리
        // ─────────────────────────────────────────

        public List<InventoryItemEntry> inventoryItems = new List<InventoryItemEntry>();
        public List<GearSlotEntry>      gearSlots      = new List<GearSlotEntry>();

        // ─────────────────────────────────────────
        //  헬퍼 — 강화 레벨 읽기/쓰기
        // ─────────────────────────────────────────

        public int GetUpgradeLevel(string key)
        {
            foreach (var entry in upgrades)
                if (entry.key == key) return entry.level;
            return 1; // 저장 기록 없으면 기본값 1
        }

        public void SetUpgradeLevel(string key, int level)
        {
            foreach (var entry in upgrades)
            {
                if (entry.key == key) { entry.level = level; return; }
            }
            upgrades.Add(new UpgradeSaveEntry { key = key, level = level });
        }

        // ─────────────────────────────────────────
        //  헬퍼 — 스킬 장착 슬롯 읽기/쓰기
        //  키: "{characterName}"  값: int[3] (슬롯 0~2 인덱스)
        // ─────────────────────────────────────────

        public int[] GetEquippedSlots(string characterName)
        {
            foreach (var e in equippedSkills)
                if (e.characterName == characterName) return e.slots;
            return null; // 저장 기록 없음 → 직업 클래스 기본값 사용
        }

        public void SetEquippedSlots(string characterName, int[] slots)
        {
            foreach (var e in equippedSkills)
            {
                if (e.characterName == characterName) { e.slots = (int[])slots.Clone(); return; }
            }
            equippedSkills.Add(new EquipSaveEntry
            {
                characterName = characterName,
                slots         = (int[])slots.Clone()
            });
        }
    }

    [Serializable]
    public class UpgradeSaveEntry
    {
        public string key;
        public int    level;
    }

    [Serializable]
    public class EquipSaveEntry
    {
        public string characterName;
        public int[]  slots = new int[Combat.CharacterBase.SKILL_SLOT_COUNT];
    }

    // ─────────────────────────────────────────
    //  장비 인벤토리 직렬화
    //  JsonUtility는 Dictionary를 지원하지 않으므로
    //  EquipmentData를 평탄화한 Entry 클래스로 저장합니다.
    // ─────────────────────────────────────────

    [Serializable]
    public class InventoryItemEntry
    {
        public string instanceId;
        public string baseItemId;
        public string displayName;
        public int    slot;        // EquipSlot enum → int
        public int    rarity;      // EquipRarity enum → int
        public int    enhanceLevel;

        // StatBonus 평탄화
        public float bonusAttack;
        public float bonusHpFlat;
        public float bonusHpPercent;
        public float bonusDmgReduction;
        public float bonusAttackSpeed;
        public float bonusCritChance;

        public InventoryItemEntry() { }

        public InventoryItemEntry(Equipment.EquipmentData item)
        {
            instanceId   = item.instanceId;
            baseItemId   = item.baseItemId;
            displayName  = item.displayName;
            slot         = (int)item.slot;
            rarity       = (int)item.rarity;
            enhanceLevel = item.enhanceLevel;

            var s = item.baseStat;
            bonusAttack       = s.bonusAttack;
            bonusHpFlat       = s.bonusHpFlat;
            bonusHpPercent    = s.bonusHpPercent;
            bonusDmgReduction = s.bonusDmgReduction;
            bonusAttackSpeed  = s.bonusAttackSpeed;
            bonusCritChance   = s.bonusCritChance;
        }

        public Equipment.EquipmentData ToEquipmentData()
        {
            if (string.IsNullOrEmpty(instanceId)) return null;
            return new Equipment.EquipmentData
            {
                instanceId   = instanceId,
                baseItemId   = baseItemId,
                displayName  = displayName,
                slot         = (Equipment.EquipSlot)slot,
                rarity       = (Equipment.EquipRarity)rarity,
                enhanceLevel = enhanceLevel,
                baseStat     = new Equipment.StatBonus
                {
                    bonusAttack       = bonusAttack,
                    bonusHpFlat       = bonusHpFlat,
                    bonusHpPercent    = bonusHpPercent,
                    bonusDmgReduction = bonusDmgReduction,
                    bonusAttackSpeed  = bonusAttackSpeed,
                    bonusCritChance   = bonusCritChance,
                }
            };
        }
    }

    [Serializable]
    public class GearSlotEntry
    {
        public string   characterName;
        public string[] slotIds = new string[Managers.InventoryManager.GEAR_SLOTS];
    }
}
