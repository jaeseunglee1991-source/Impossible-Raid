using System;
using UnityEngine;

namespace BossRaid.Equipment
{
    // ═══════════════════════════════════════════════════════════
    //  열거형 정의
    // ═══════════════════════════════════════════════════════════

    /// <summary>장비 슬롯 종류. CharacterBase의 equippedGear[] 인덱스와 1:1 대응.</summary>
    public enum EquipSlot
    {
        Weapon    = 0,   // 공격력 중심
        Armor     = 1,   // HP / 방어 중심
        Accessory = 2,   // 속도 / 특수 효과
    }

    /// <summary>
    /// 희귀도. 색상 코딩: 일반=회색, 희귀=파랑, 영웅=보라, 전설=주황
    /// 스탯 배율: 1.0 / 1.3 / 1.7 / 2.5
    /// </summary>
    public enum EquipRarity { Common, Rare, Epic, Legendary }

    // ═══════════════════════════════════════════════════════════
    //  스탯 보너스 구조체
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// 장비 하나가 캐릭터에 부여하는 스탯 보너스.
    /// CharacterBase 필드와 1:1 매핑:
    ///   bonusAttack       → autoAttackDamage (가산)
    ///   bonusHpFlat       → maxHealth        (가산)
    ///   bonusHpPercent    → maxHealth        (비율, 0.1 = +10%)
    ///   bonusDmgReduction → damageReductionMultiplier 감소량 (0.05 = 5% 경감)
    ///   bonusAttackSpeed  → attackSpeed 감소량 (음수일수록 빠름, 0.05 = 5% 빠름)
    ///   bonusCritChance   → 크리티컬 확률 (추후 시스템 확장 예정)
    /// </summary>
    [Serializable]
    public struct StatBonus
    {
        public float bonusAttack;
        public float bonusHpFlat;
        public float bonusHpPercent;
        public float bonusDmgReduction;   // 0.05 = 받는 피해 5% 감소
        public float bonusAttackSpeed;    // 0.05 = 공격 속도 5% 증가 (쿨타임 감소)
        public float bonusCritChance;

        public static StatBonus Zero => new StatBonus();

        /// <summary>두 보너스를 합산합니다. (여러 장비 합산용)</summary>
        public static StatBonus operator +(StatBonus a, StatBonus b) => new StatBonus
        {
            bonusAttack       = a.bonusAttack       + b.bonusAttack,
            bonusHpFlat       = a.bonusHpFlat       + b.bonusHpFlat,
            bonusHpPercent    = a.bonusHpPercent    + b.bonusHpPercent,
            bonusDmgReduction = a.bonusDmgReduction + b.bonusDmgReduction,
            bonusAttackSpeed  = a.bonusAttackSpeed  + b.bonusAttackSpeed,
            bonusCritChance   = a.bonusCritChance   + b.bonusCritChance,
        };
    }

    // ═══════════════════════════════════════════════════════════
    //  장비 아이템 데이터
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// ──────────────────────────────────────────────────────────────────
    /// EquipmentData — 장비 아이템 한 개의 완전한 정보
    ///
    /// ■ 기본 스탯은 slot + rarity + baseItemId로 결정됩니다.
    /// ■ enhanceLevel(+0~+10)에 따라 스탯이 선형 증가합니다.
    /// ■ instanceId는 인벤토리 내 고유 식별자 (UUID)입니다.
    ///
    /// [생성 방법]
    ///   var sword = EquipmentData.Generate("iron_sword", EquipSlot.Weapon, EquipRarity.Rare);
    /// ──────────────────────────────────────────────────────────────────
    /// </summary>
    [Serializable]
    public class EquipmentData
    {
        // ─────────────────────────────────────────
        //  식별 정보
        // ─────────────────────────────────────────

        /// <summary>인벤토리 내 고유 UUID. 장착/해제/강화 시 이 값으로 조회합니다.</summary>
        public string instanceId  = "";

        /// <summary>아이템 종류 코드. 동일 코드끼리 합산 불가. (예: "iron_sword", "dragon_mail")</summary>
        public string baseItemId  = "";

        public string displayName = "장비";
        public EquipSlot   slot    = EquipSlot.Weapon;
        public EquipRarity rarity  = EquipRarity.Common;

        // ─────────────────────────────────────────
        //  스탯
        // ─────────────────────────────────────────

        /// <summary>+0 기준 기본 스탯 보너스.</summary>
        public StatBonus baseStat;

        /// <summary>강화 레벨 (+0 ~ +10).</summary>
        public int enhanceLevel = 0;

        public const int MAX_ENHANCE = 10;

        /// <summary>강화 포함 최종 스탯. CharacterBase에 실제로 적용되는 값.</summary>
        public StatBonus FinalStat
        {
            get
            {
                float mult = 1f + enhanceLevel * EnhanceMultiplierPerLevel;
                return new StatBonus
                {
                    bonusAttack       = baseStat.bonusAttack       * mult,
                    bonusHpFlat       = baseStat.bonusHpFlat       * mult,
                    bonusHpPercent    = baseStat.bonusHpPercent    * mult,
                    bonusDmgReduction = baseStat.bonusDmgReduction * mult,
                    bonusAttackSpeed  = baseStat.bonusAttackSpeed  * mult,
                    bonusCritChance   = baseStat.bonusCritChance   * mult,
                };
            }
        }

        /// <summary>강화 1레벨당 스탯 증가 비율 (10% 증가).</summary>
        public const float EnhanceMultiplierPerLevel = 0.10f;

        // ─────────────────────────────────────────
        //  강화 비용
        // ─────────────────────────────────────────

        /// <summary>다음 강화에 필요한 골드. 희귀도 × 지수 증가.</summary>
        public double NextEnhanceCost
        {
            get
            {
                if (enhanceLevel >= MAX_ENHANCE) return double.MaxValue;
                double rarityBase = rarity switch
                {
                    EquipRarity.Common    => 100,
                    EquipRarity.Rare      => 300,
                    EquipRarity.Epic      => 800,
                    EquipRarity.Legendary => 2000,
                    _                     => 100
                };
                return rarityBase * Math.Pow(1.8, enhanceLevel);
            }
        }

        // ─────────────────────────────────────────
        //  희귀도별 배율
        // ─────────────────────────────────────────

        public float RarityMultiplier => rarity switch
        {
            EquipRarity.Common    => 1.0f,
            EquipRarity.Rare      => 1.3f,
            EquipRarity.Epic      => 1.7f,
            EquipRarity.Legendary => 2.5f,
            _                     => 1.0f
        };

        // ─────────────────────────────────────────
        //  팩토리 — 장비 생성
        // ─────────────────────────────────────────

        /// <summary>
        /// 장비 인스턴스를 생성합니다. 기본 스탯은 슬롯 + 희귀도 + 스테이지 레벨로 결정됩니다.
        /// InventoryManager.GenerateDrop()에서 호출합니다.
        /// </summary>
        public static EquipmentData Generate(string baseId, EquipSlot slot,
                                             EquipRarity rarity, int stageLevel = 1)
        {
            var item = new EquipmentData
            {
                instanceId  = Guid.NewGuid().ToString(),
                baseItemId  = baseId,
                displayName = BuildDisplayName(baseId, rarity),
                slot        = slot,
                rarity      = rarity,
                enhanceLevel = 0,
            };

            // 스테이지 레벨 스케일링 (10레벨마다 10% 증가)
            float stageMult = 1f + stageLevel * 0.1f;

            item.baseStat = BuildBaseStat(slot, rarity, stageMult);
            return item;
        }

        private static StatBonus BuildBaseStat(EquipSlot slot, EquipRarity rarity, float stageMult)
        {
            float rarityMult = rarity switch
            {
                EquipRarity.Common    => 1.0f,
                EquipRarity.Rare      => 1.3f,
                EquipRarity.Epic      => 1.7f,
                EquipRarity.Legendary => 2.5f,
                _                     => 1.0f
            };
            float m = stageMult * rarityMult;

            return slot switch
            {
                EquipSlot.Weapon    => new StatBonus { bonusAttack = 20f * m, bonusAttackSpeed = 0.02f * m },
                EquipSlot.Armor     => new StatBonus { bonusHpFlat = 200f * m, bonusDmgReduction = 0.03f * m },
                EquipSlot.Accessory => new StatBonus { bonusAttack = 8f * m, bonusHpPercent = 0.05f * m, bonusCritChance = 0.03f * m },
                _                   => StatBonus.Zero
            };
        }

        private static string BuildDisplayName(string baseId, EquipRarity rarity)
        {
            string prefix = rarity switch
            {
                EquipRarity.Rare      => "[희귀] ",
                EquipRarity.Epic      => "[영웅] ",
                EquipRarity.Legendary => "[전설] ",
                _                     => ""
            };
            return prefix + baseId switch
            {
                "iron_sword"    => "무쇠 검",
                "fire_sword"    => "화염 검",
                "dark_blade"    => "어둠의 단검",
                "staff"         => "마법 지팡이",
                "dragon_staff"  => "용의 지팡이",
                "bow"           => "장궁",
                "iron_mail"     => "무쇠 갑옷",
                "dragon_mail"   => "용린 갑옷",
                "leather_vest"  => "가죽 조끼",
                "robe"          => "마법사 로브",
                "ring"          => "반지",
                "amulet"        => "목걸이",
                "bracelet"      => "팔찌",
                "boss_fragment" => "보스 파편",
                _               => baseId
            };
        }

        // ─────────────────────────────────────────
        //  UI 표시용
        // ─────────────────────────────────────────

        public string EnhanceSuffix => enhanceLevel > 0 ? $" +{enhanceLevel}" : "";
        public string FullName      => displayName + EnhanceSuffix;

        public string RarityColorHex => rarity switch
        {
            EquipRarity.Common    => "#AAAAAA",
            EquipRarity.Rare      => "#4488FF",
            EquipRarity.Epic      => "#AA44FF",
            EquipRarity.Legendary => "#FF8800",
            _                     => "#FFFFFF"
        };

        public string StatSummary
        {
            get
            {
                var s = FinalStat;
                var sb = new System.Text.StringBuilder();
                if (s.bonusAttack      > 0)  sb.AppendLine($"공격력 +{s.bonusAttack:F0}");
                if (s.bonusHpFlat      > 0)  sb.AppendLine($"HP +{s.bonusHpFlat:F0}");
                if (s.bonusHpPercent   > 0)  sb.AppendLine($"HP +{s.bonusHpPercent * 100f:F0}%");
                if (s.bonusDmgReduction> 0)  sb.AppendLine($"피해 감소 {s.bonusDmgReduction * 100f:F0}%");
                if (s.bonusAttackSpeed > 0)  sb.AppendLine($"공격 속도 +{s.bonusAttackSpeed * 100f:F0}%");
                if (s.bonusCritChance  > 0)  sb.AppendLine($"크리티컬 {s.bonusCritChance * 100f:F0}%");
                return sb.ToString().TrimEnd();
            }
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  드랍 테이블 (스테이지 레벨별 드랍 설정)
    // ═══════════════════════════════════════════════════════════

    /// <summary>StageManager.OnMobKilled() / OnBossDefeated()에서 참조합니다.</summary>
    public static class DropTable
    {
        /// <summary>몹 처치 시 장비 드랍 확률 (0~1). 방치형 기준 1% 기본.</summary>
        public const float MOB_DROP_CHANCE  = 0.01f;

        /// <summary>보스 처치 시 장비 드랍 확률 (100% — 보스는 항상 드랍).</summary>
        public const float BOSS_DROP_CHANCE = 1.00f;

        // 스테이지별 희귀도 가중치 테이블
        // [스테이지 구간] : { Common, Rare, Epic, Legendary }
        private static readonly (int maxStage, float[] weights)[] _rarityTable =
        {
            (10,  new float[] { 70f, 25f,  5f,  0f }),
            (25,  new float[] { 55f, 32f, 12f,  1f }),
            (50,  new float[] { 40f, 35f, 20f,  5f }),
            (100, new float[] { 25f, 35f, 28f, 12f }),
            (int.MaxValue, new float[] { 10f, 30f, 35f, 25f }),
        };

        // 슬롯별 드랍 아이템 풀
        private static readonly string[][] _itemPools =
        {
            // Weapon
            new[] { "iron_sword", "fire_sword", "dark_blade", "staff", "dragon_staff", "bow" },
            // Armor
            new[] { "iron_mail", "dragon_mail", "leather_vest", "robe" },
            // Accessory
            new[] { "ring", "amulet", "bracelet", "boss_fragment" },
        };

        public static EquipmentData Roll(int stageLevel, bool isBossDrop = false)
        {
            EquipRarity rarity = RollRarity(stageLevel, isBossDrop);
            EquipSlot   slot   = (EquipSlot)UnityEngine.Random.Range(0, 3);
            string[]    pool   = _itemPools[(int)slot];
            string      baseId = pool[UnityEngine.Random.Range(0, pool.Length)];

            return EquipmentData.Generate(baseId, slot, rarity, stageLevel);
        }

        private static EquipRarity RollRarity(int stageLevel, bool isBossGuaranteed)
        {
            float[] weights = null;
            foreach (var row in _rarityTable)
            {
                if (stageLevel <= row.maxStage) { weights = row.weights; break; }
            }

            // 보스 드랍 → 최소 희귀(Rare) 보장
            float rand = UnityEngine.Random.Range(0f, 100f);
            if (isBossGuaranteed) rand = Mathf.Min(rand, weights[0] - 0.01f); // Common 구간 제외

            float cumulative = 0f;
            for (int i = 0; i < weights.Length; i++)
            {
                cumulative += weights[i];
                if (rand < cumulative) return (EquipRarity)i;
            }
            return EquipRarity.Common;
        }
    }
}
