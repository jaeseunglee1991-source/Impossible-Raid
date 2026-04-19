using UnityEngine;
using System.Collections.Generic;

namespace BossRaid.Combat
{
    /// <summary>
    /// ──────────────────────────────────────────────────────────────────
    /// SkillVFXDatabase — 스킬 이름 → VFX 프리팹 매핑 데이터베이스
    ///
    /// ■ ScriptableObject로 에디터에서 등록/관리
    /// ■ CharacterBase.DealDamageTo() 또는 스킬 실행 시 자동으로 VFX 재생
    /// ■ 프리팹이 없는 스킬은 무시 (개발 중 안전)
    ///
    /// [사용법]
    ///   1. Project > Create > BossRaid > Skill VFX Database 로 생성
    ///   2. 스킬명과 프리팹을 매핑
    ///   3. CombatManager 또는 BattleManager에 등록
    /// ──────────────────────────────────────────────────────────────────
    /// </summary>
    [CreateAssetMenu(fileName = "SkillVFXDatabase", menuName = "BossRaid/Skill VFX Database")]
    public class SkillVFXDatabase : ScriptableObject
    {
        [System.Serializable]
        public class SkillVFXEntry
        {
            [Tooltip("CharacterBase.SkillDefinition.skillName과 동일해야 함")]
            public string skillName;

            [Tooltip("스킬 시전 시 재생할 VFX 프리팹")]
            public GameObject vfxPrefab;

            [Tooltip("VFX 재생 위치: 시전자(Self) / 타겟(Target) / 월드(World)")]
            public VFXSpawnPoint spawnPoint = VFXSpawnPoint.Target;

            [Tooltip("VFX 지속 시간 (자동 반환)")]
            public float duration = 2f;

            [Tooltip("VFX 크기 배율")]
            public float scale = 1f;

            [Tooltip("VFX 오프셋 (스폰 위치 기준)")]
            public Vector3 offset = Vector3.zero;
        }

        public enum VFXSpawnPoint
        {
            Self,       // 시전자 위치
            Target,     // 타겟(보스) 위치
            World       // 지정된 월드 좌표 (장판 등)
        }

        public List<SkillVFXEntry> entries = new List<SkillVFXEntry>();

        // 빠른 검색용 딕셔너리 (런타임 캐싱)
        private Dictionary<string, SkillVFXEntry> _lookup;

        /// <summary>스킬 이름으로 VFX 데이터를 조회합니다.</summary>
        public SkillVFXEntry GetEntry(string skillName)
        {
            if (_lookup == null) BuildLookup();
            
            SkillVFXEntry entry;
            _lookup.TryGetValue(skillName, out entry);
            return entry;
        }

        /// <summary>스킬 시전 시 VFX를 자동으로 풀에서 꺼내 재생합니다.</summary>
        public void PlaySkillVFX(string skillName, Transform caster, Transform target)
        {
            var entry = GetEntry(skillName);
            if (entry == null || entry.vfxPrefab == null) return;

            if (SkillVFXPool.Instance == null)
            {
                Debug.LogWarning("[SkillVFXDatabase] SkillVFXPool이 씬에 없습니다.");
                return;
            }

            Vector3 spawnPos;
            Quaternion spawnRot = Quaternion.identity;

            switch (entry.spawnPoint)
            {
                case VFXSpawnPoint.Self:
                    spawnPos = caster != null ? caster.position + entry.offset : Vector3.zero;
                    spawnRot = caster != null ? caster.rotation : Quaternion.identity;
                    break;
                case VFXSpawnPoint.Target:
                    spawnPos = target != null ? target.position + entry.offset : Vector3.zero;
                    break;
                default: // World
                    spawnPos = entry.offset;
                    break;
            }

            var fx = SkillVFXPool.Instance.Get(
                skillName, entry.vfxPrefab, spawnPos, spawnRot, entry.duration);

            if (fx != null && Mathf.Abs(entry.scale - 1f) > 0.01f)
                fx.transform.localScale = Vector3.one * entry.scale;
        }

        private void BuildLookup()
        {
            _lookup = new Dictionary<string, SkillVFXEntry>();
            foreach (var e in entries)
            {
                if (!string.IsNullOrEmpty(e.skillName) && !_lookup.ContainsKey(e.skillName))
                    _lookup[e.skillName] = e;
            }
        }

        private void OnEnable()
        {
            // ScriptableObject가 로드될 때 캐시 갱신
            _lookup = null;
        }
    }
}
