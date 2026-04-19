using UnityEngine;
using System.Collections.Generic;

namespace BossRaid.Combat
{
    /// <summary>
    /// ──────────────────────────────────────────────────────────────────
    /// SkillVFXPool — 모바일 최적화된 스킬 이펙트 오브젝트 풀링 시스템
    ///
    /// ■ Instantiate/Destroy 대신 Get/Release로 GC 스파이크 완전 제거
    /// ■ 카테고리(스킬명)별 풀을 자동 생성 — 새 이펙트 추가 시 코드 수정 불필요
    /// ■ ParticleSystem 안전 리셋 포함 (모바일 파티클 버그 방지)
    /// ■ 자동 반환: 일정 시간 후 풀로 자동 복귀
    ///
    /// [사용법]
    ///   var fx = SkillVFXPool.Instance.Get("메테오_폭발", prefab, position, rotation);
    ///   // 자동 반환 또는 수동: SkillVFXPool.Instance.Release("메테오_폭발", fx);
    /// ──────────────────────────────────────────────────────────────────
    /// </summary>
    public class SkillVFXPool : MonoBehaviour
    {
        public static SkillVFXPool Instance { get; private set; }

        [Header("풀 설정")]
        [Tooltip("카테고리당 초기 생성 수")]
        public int defaultPrewarmCount = 3;

        [Tooltip("카테고리당 최대 보유 수 (초과 시 Destroy)")]
        public int maxPoolSizePerCategory = 20;

        [Tooltip("이펙트 자동 반환 시간 (초)")]
        public float defaultAutoReturnTime = 3f;

        // 카테고리명 → 풀 큐
        private readonly Dictionary<string, Queue<GameObject>> _pools
            = new Dictionary<string, Queue<GameObject>>();

        // 활성 오브젝트 → 카테고리명 (반환 시 어디로 보낼지)
        private readonly Dictionary<GameObject, string> _activeObjects
            = new Dictionary<GameObject, string>();

        private Transform _poolRoot;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _poolRoot = new GameObject("_VFXPoolRoot").transform;
            _poolRoot.SetParent(transform);
            _poolRoot.gameObject.SetActive(false);
        }

        // ═══════════════════════════════════════════════════════════
        //  Public API
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// 풀에서 이펙트를 꺼내 배치합니다.
        /// 풀이 비어있으면 prefab으로부터 새로 생성합니다.
        /// </summary>
        /// <param name="category">이펙트 카테고리명 (예: "메테오_폭발")</param>
        /// <param name="prefab">없을 때 새로 만들 프리팹 (null이면 빈 오브젝트)</param>
        /// <param name="position">생성 위치</param>
        /// <param name="rotation">생성 회전</param>
        /// <param name="autoReturnTime">자동 반환 시간 (0이면 수동 반환)</param>
        public GameObject Get(string category, GameObject prefab, Vector3 position,
                              Quaternion rotation, float autoReturnTime = -1f)
        {
            if (autoReturnTime < 0f) autoReturnTime = defaultAutoReturnTime;

            EnsurePool(category);

            GameObject obj;
            if (_pools[category].Count > 0)
            {
                obj = _pools[category].Dequeue();
                obj.transform.SetParent(null);
                obj.transform.position = position;
                obj.transform.rotation = rotation;
                obj.SetActive(true);
            }
            else
            {
                // 풀이 비었으면 새로 생성
                if (prefab != null)
                    obj = Instantiate(prefab, position, rotation);
                else
                {
                    obj = new GameObject($"VFX_{category}");
                    obj.transform.position = position;
                    obj.transform.rotation = rotation;
                }
            }

            // 파티클 시스템 리셋 및 재생
            ResetAndPlayParticles(obj);

            _activeObjects[obj] = category;

            if (autoReturnTime > 0f)
                StartCoroutine(AutoReturnCoroutine(category, obj, autoReturnTime));

            return obj;
        }

        /// <summary>이펙트를 풀로 반환합니다.</summary>
        public void Release(string category, GameObject obj)
        {
            if (obj == null) return;

            _activeObjects.Remove(obj);

            EnsurePool(category);

            // 풀 최대치 초과 시 파괴
            if (_pools[category].Count >= maxPoolSizePerCategory)
            {
                Destroy(obj);
                return;
            }

            StopAndClearParticles(obj);
            obj.SetActive(false);
            obj.transform.SetParent(_poolRoot);
            _pools[category].Enqueue(obj);
        }

        /// <summary>특정 카테고리의 풀을 프리팹으로 미리 채웁니다 (로딩 화면에서 호출).</summary>
        public void Prewarm(string category, GameObject prefab, int count = -1)
        {
            if (count < 0) count = defaultPrewarmCount;
            EnsurePool(category);

            for (int i = 0; i < count; i++)
            {
                GameObject obj = prefab != null
                    ? Instantiate(prefab, _poolRoot)
                    : new GameObject($"VFX_{category}_{i}");

                obj.SetActive(false);
                obj.transform.SetParent(_poolRoot);
                _pools[category].Enqueue(obj);
            }
        }

        /// <summary>전투 종료 시 모든 활성 이펙트를 즉시 회수합니다.</summary>
        public void ReleaseAll()
        {
            var toRelease = new List<KeyValuePair<GameObject, string>>(_activeObjects);
            foreach (var kv in toRelease)
            {
                if (kv.Key != null) Release(kv.Value, kv.Key);
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  내부 유틸
        // ═══════════════════════════════════════════════════════════

        private void EnsurePool(string category)
        {
            if (!_pools.ContainsKey(category))
                _pools[category] = new Queue<GameObject>();
        }

        private void ResetAndPlayParticles(GameObject obj)
        {
            var particles = obj.GetComponentsInChildren<ParticleSystem>(true);
            foreach (var ps in particles)
            {
                ps.Clear(true);
                ps.Play(true);
            }
        }

        private void StopAndClearParticles(GameObject obj)
        {
            var particles = obj.GetComponentsInChildren<ParticleSystem>(true);
            foreach (var ps in particles)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ps.Clear(true);
            }
        }

        private System.Collections.IEnumerator AutoReturnCoroutine(
            string category, GameObject obj, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (obj != null && obj.activeInHierarchy)
                Release(category, obj);
        }
    }
}
