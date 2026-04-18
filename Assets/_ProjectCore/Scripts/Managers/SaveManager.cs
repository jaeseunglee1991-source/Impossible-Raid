using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using BossRaid.Combat;

namespace BossRaid.Managers
{
    /// <summary>
    /// ──────────────────────────────────────────────────────────────────
    /// SaveManager — 방치형 게임 데이터 저장 시스템
    ///
    /// ■ 저장 방식: JSON → AES 암호화(SecurePlayerPrefs) → PlayerPrefs
    /// ■ 자동 저장: 강화/골드 변경 즉시 + 주기 저장(60초) + 앱 종료/일시정지
    /// ■ 더티 플래그: 변경이 있을 때만 실제 디스크 기록 (Write 절약)
    /// ■ 버전 관리: CURRENT_SAVE_VERSION 상수로 마이그레이션 제어
    ///
    /// [사용 방법]
    ///   1. 씬에 빈 GameObject를 만들고 이 컴포넌트를 추가합니다.
    ///   2. 강화 버튼 클릭 후 StatUpgrade.TryUpgrade()에서 아래 호출:
    ///        SaveManager.Instance.MarkDirty();
    ///   3. GrowthManager.AddGold() 내부에서 아래 호출:
    ///        SaveManager.Instance.MarkDirty();
    ///   4. 씬 시작 시 SaveManager.Instance.Load()가 자동 실행됩니다.
    /// ──────────────────────────────────────────────────────────────────
    /// </summary>
    public class SaveManager : MonoBehaviour
    {
        // ═══════════════════════════════════════════════════════════
        //  상수
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// 세이브 구조가 바뀔 때마다 이 값을 올리세요.
        /// 구버전 세이브를 불러오면 자동으로 초기화 처리됩니다.
        /// </summary>
        public const int CURRENT_SAVE_VERSION = 1;

        /// <summary>PlayerPrefs에 저장될 최상위 키</summary>
        private const string SAVE_KEY = "ImpossibleRaid_SaveData_v1";

        /// <summary>주기 자동저장 간격 (초)</summary>
        private const float AUTO_SAVE_INTERVAL = 60f;

        // ═══════════════════════════════════════════════════════════
        //  싱글턴
        // ═══════════════════════════════════════════════════════════

        public static SaveManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject); // 씬 전환 시에도 유지
        }

        // ═══════════════════════════════════════════════════════════
        //  더티 플래그 (불필요한 Write 차단) & 메모리 데이터
        // ═══════════════════════════════════════════════════════════

        private bool   _isDirty       = false;
        private float  _dirtyTimer    = 0f;
        private PlayerSaveData _memoryData = new PlayerSaveData();

        /// <summary>
        /// 저장이 필요한 상태 변경이 생겼을 때 호출합니다.
        /// 실제 디스크 Write는 2초 딜레이 후 일괄 실행됩니다.
        /// (강화 버튼 연타 시 Write가 폭발하지 않도록 방어)
        /// </summary>
        public void MarkDirty()
        {
            _isDirty    = true;
            _dirtyTimer = 0f; // 딜레이 리셋
        }

        // ═══════════════════════════════════════════════════════════
        //  생명주기
        // ═══════════════════════════════════════════════════════════

        private void Start()
        {
            Load();
            StartCoroutine(AutoSaveRoutine());
        }

        private void Update()
        {
            if (!_isDirty) return;

            _dirtyTimer += Time.deltaTime;
            if (_dirtyTimer >= 2f) // 마지막 변경 후 2초 뒤 저장
            {
                Save();
            }
        }

        private void OnApplicationPause(bool isPaused)
        {
            if (isPaused) SaveImmediate();
        }

        private void OnApplicationQuit()
        {
            SaveImmediate();
        }

        // ═══════════════════════════════════════════════════════════
        //  주기 자동저장
        // ═══════════════════════════════════════════════════════════

        private IEnumerator AutoSaveRoutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(AUTO_SAVE_INTERVAL);
                if (_isDirty) Save();
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  저장 (Save)
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// 더티 플래그 확인 후 저장합니다. 변경이 없으면 Write를 건너뜁니다.
        /// </summary>
        public void Save()
        {
            if (!_isDirty) return;
            SaveImmediate();
        }

        /// <summary>
        /// 더티 여부와 관계없이 즉시 저장합니다.
        /// OnApplicationQuit / OnApplicationPause에서 사용합니다.
        /// </summary>
        public void SaveImmediate()
        {
            try
            {
                PlayerSaveData data = CollectSaveData();
                string json         = JsonUtility.ToJson(data);
                SecurePlayerPrefs.SetString(SAVE_KEY, json);

                _isDirty    = false;
                _dirtyTimer = 0f;

                Debug.Log($"<color=green>[SaveManager] 저장 완료 " +
                          $"(골드: {data.gold:F0}, 스테이지: {data.stageLevel}, " +
                          $"강화 항목: {data.upgrades.Count}개)</color>");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] 저장 실패: {e.Message}");
            }
        }

        // ─────────────────────────────────────────
        //  저장할 데이터 수집
        // ─────────────────────────────────────────

        private PlayerSaveData CollectSaveData()
        {
            if (_memoryData == null) _memoryData = new PlayerSaveData();
            
            _memoryData.savedAtUtc = DateTime.UtcNow.ToString("o");

            // 1. 골드
            if (GrowthManager.Instance != null)
                _memoryData.gold = GrowthManager.Instance.displayGold;

            // 2. 스테이지
            if (StageManager.Instance != null)
            {
                _memoryData.stageLevel = StageManager.Instance.CurrentStageLevel;
                _memoryData.mobsKilled = StageManager.Instance.MobsKilledInStage;
            }

            // 3. 강화 레벨 — 씬에 있는 캐릭터들로부터 수집 시 덮어쓰기 (없는 애들은 기존 기록 유지)
            CollectUpgrades(_memoryData);

            // 4. 장비 인벤토리
            InventoryManager.Instance?.CollectToSaveData(_memoryData);

            return _memoryData;
        }

        /// <summary>
        /// 씬에 있는 CharacterBase 컴포넌트들을 순회하여 StatUpgrade 레벨을 수집합니다.
        /// 키 형식: "{characterName}_{statName}"  예) "전사_기본 공격력"
        /// </summary>
        private void CollectUpgrades(PlayerSaveData data)
        {
            var characters = UnityEngine.Object.FindObjectsByType<CharacterBase>(FindObjectsSortMode.None);
            foreach (var character in characters)
            {
                string keyPrefix = character.characterName;

                // CharacterBase.attackPowerUpgrade
                SaveUpgradeEntry(data, keyPrefix, character.attackPowerUpgrade);
                data.SetEquippedSlots(character.characterName, character.equippedSlots);

                // 직업 클래스에 추가 StatUpgrade가 있다면 여기에 추가
                // 예: if (character is Warrior w) SaveUpgradeEntry(data, keyPrefix, w.defenseUpgrade);
            }
        }

        private void SaveUpgradeEntry(PlayerSaveData data, string prefix, StatUpgrade upgrade)
        {
            if (upgrade == null) return;
            string key = $"{prefix}_{upgrade.statName}";
            data.SetUpgradeLevel(key, upgrade.currentLevel);
        }

        // ═══════════════════════════════════════════════════════════
        //  불러오기 (Load)
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Start()에서 자동 호출됩니다. 저장 데이터가 없으면 조용히 기본값으로 시작합니다.
        /// 버전이 다르면 구버전 세이브를 무시하고 초기화합니다.
        /// </summary>
        public void Load()
        {
            string json = SecurePlayerPrefs.GetString(SAVE_KEY, "");

            if (string.IsNullOrEmpty(json))
            {
                Debug.Log("[SaveManager] 저장 데이터 없음 — 새 게임으로 시작합니다.");
                return;
            }

            // 변조 감지: SecurePlayerPrefs 내부에서 복호화 실패 시 빈 문자열 반환됨
            try
            {
                PlayerSaveData data = JsonUtility.FromJson<PlayerSaveData>(json);

                if (data == null)
                {
                    Debug.LogWarning("[SaveManager] 세이브 파싱 실패 — 초기화합니다.");
                    DeleteSave();
                    return;
                }

                // 버전 불일치 → 초기화
                if (data.saveVersion != CURRENT_SAVE_VERSION)
                {
                    Debug.LogWarning($"[SaveManager] 세이브 버전 불일치 " +
                                     $"(파일: {data.saveVersion}, 현재: {CURRENT_SAVE_VERSION}) " +
                                     $"— 초기화합니다.");
                    DeleteSave();
                    return;
                }

                _memoryData = data; // 메모리에 유지 
                ApplySaveData(_memoryData);

                Debug.Log($"<color=cyan>[SaveManager] 불러오기 완료 " +
                          $"(골드: {data.gold:F0}, 스테이지: {data.stageLevel})</color>");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] 불러오기 실패: {e.Message}");
                DeleteSave();
            }
        }

        // ─────────────────────────────────────────
        //  불러온 데이터를 각 매니저에 적용
        // ─────────────────────────────────────────

        private void ApplySaveData(PlayerSaveData data)
        {
            // 1. 골드
            if (GrowthManager.Instance != null)
            {
                GrowthManager.Instance.SetGoldFromSave(data.gold);
            }

            // 2. 스테이지
            if (StageManager.Instance != null)
            {
                StageManager.Instance.CurrentStageLevel = data.stageLevel;
                StageManager.Instance.MobsKilledInStage = data.mobsKilled;
            }

            // 3. 강화 레벨
            ApplyUpgrades(data);

            // 4. 장비 인벤토리
            InventoryManager.Instance?.RestoreFromSaveData(data);
        }

        private void ApplyUpgrades(PlayerSaveData data)
        {
            var characters = UnityEngine.Object.FindObjectsByType<CharacterBase>(FindObjectsSortMode.None);
            foreach (var character in characters)
            {
                string keyPrefix = character.characterName;
                RestoreUpgrade(data, keyPrefix, character.attackPowerUpgrade);

                int[] savedSlots = data.GetEquippedSlots(character.characterName);
                if (savedSlots != null) character.RestoreEquippedSlots(savedSlots);

                // 직업 추가 StatUpgrade가 있다면 여기서도 동일하게 복원
            }
        }

        private void RestoreUpgrade(PlayerSaveData data, string prefix, StatUpgrade upgrade)
        {
            if (upgrade == null) return;
            string key = $"{prefix}_{upgrade.statName}";
            upgrade.currentLevel = data.GetUpgradeLevel(key);
        }

        public int GetSavedUpgradeLevel(string key)
        {
            if (_memoryData == null) return 1;
            return _memoryData.GetUpgradeLevel(key);
        }

        // ═══════════════════════════════════════════════════════════
        //  세이브 삭제 (초기화용)
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// 개발 테스트 또는 '게임 초기화' 버튼에서 호출합니다.
        /// </summary>
        public void DeleteSave()
        {
            PlayerPrefs.DeleteKey(SAVE_KEY);
            PlayerPrefs.Save();
            Debug.Log("[SaveManager] 세이브 데이터 삭제 완료.");
        }

        // ═══════════════════════════════════════════════════════════
        //  에디터 테스트 유틸
        // ═══════════════════════════════════════════════════════════

        [ContextMenu("강제 저장")]
        private void TestSave() => SaveImmediate();

        [ContextMenu("강제 불러오기")]
        private void TestLoad() => Load();

        [ContextMenu("세이브 삭제")]
        private void TestDelete() => DeleteSave();
    }
}
