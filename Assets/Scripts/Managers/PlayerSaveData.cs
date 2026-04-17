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
    }

    [Serializable]
    public class UpgradeSaveEntry
    {
        public string key;
        public int    level;
    }
}
