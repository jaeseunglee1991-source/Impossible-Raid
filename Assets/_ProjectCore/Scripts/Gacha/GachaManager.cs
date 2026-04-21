using System;
using System.Collections.Generic;
using UnityEngine;
using BossRaid.Managers;
 
namespace BossRaid.Gacha
{
    /// <summary>
    /// 순수 가챠 전담 매니저.
    /// BM 요소(천장·마일리지) 없음. 골드 차감 → 무기 추첨 → InventoryManager 전달만 수행.
    /// </summary>
    public class GachaManager : MonoBehaviour
    {
        // ═══════════════════════════════════════════════════════════
        //  싱글턴
        // ═══════════════════════════════════════════════════════════
 
        private static GachaManager _instance;
        public  static GachaManager Instance => _instance;
 
        // ═══════════════════════════════════════════════════════════
        //  인스펙터 설정
        // ═══════════════════════════════════════════════════════════
 
        [Serializable]
        public class GachaEntry
        {
            public WeaponData weaponData;
            [Range(0.1f, 100f)]
            public float weight = 10f;
        }
 
        [Header("가챠 풀 (WeaponData SO를 드래그하여 등록)")]
        [SerializeField] private List<GachaEntry> _pool = new List<GachaEntry>();
 
        [Header("비용 (인게임 골드)")]
        [SerializeField] private double _cost1   = 100;
        [SerializeField] private double _cost10  = 900;    // 10% 할인
        [SerializeField] private double _cost100 = 8_000;  // 20% 할인
 
        // ═══════════════════════════════════════════════════════════
        //  이벤트 (UI 구독용)
        // ═══════════════════════════════════════════════════════════
 
        /// <summary>뽑기 완료 후 결과 목록 전달 (연출 UI용).</summary>
        public event Action<List<WeaponData>> OnPullCompleted;
 
        // ═══════════════════════════════════════════════════════════
        //  내부 상태 — GC 최소화용 버퍼 재사용
        // ═══════════════════════════════════════════════════════════
 
        private float _totalWeight;
        private readonly List<WeaponData> _resultBuffer = new List<WeaponData>(100);
 
        // ═══════════════════════════════════════════════════════════
        //  생명주기
        // ═══════════════════════════════════════════════════════════
 
        private void Awake()
        {
            if (_instance != null && _instance != this) { Destroy(gameObject); return; }
            _instance = this;
            DontDestroyOnLoad(gameObject);
            CacheWeight();
        }
 
        // ═══════════════════════════════════════════════════════════
        //  공개 API
        // ═══════════════════════════════════════════════════════════
 
        public void Pull1()   => Execute(1,   _cost1);
        public void Pull10()  => Execute(10,  _cost10);
        public void Pull100() => Execute(100, _cost100);
 
        // ═══════════════════════════════════════════════════════════
        //  내부 로직
        // ═══════════════════════════════════════════════════════════
 
        private void Execute(int count, double cost)
        {
            if (_pool.Count == 0)
            {
                Debug.LogWarning("[GachaManager] 가챠 풀이 비어있습니다. 인스펙터에서 WeaponData를 등록하세요.");
                return;
            }
 
            if (GrowthManager.Instance == null || !GrowthManager.Instance.SpendGold(cost))
            {
                Debug.Log($"[GachaManager] 골드 부족. 필요: {cost:F0}G");
                return;
            }
 
            _resultBuffer.Clear();
            for (int i = 0; i < count; i++)
                _resultBuffer.Add(RollOne());
 
            // 결과를 인벤토리에 일괄 전달 → 내부에서 중복 체크·강화 연산 수행
            InventoryManager.Instance.AddWeaponsFromGacha(_resultBuffer);
 
            OnPullCompleted?.Invoke(_resultBuffer);
        }
 
        private WeaponData RollOne()
        {
            float rand  = UnityEngine.Random.Range(0f, _totalWeight);
            float cumul = 0f;
            foreach (var entry in _pool)
            {
                cumul += entry.weight;
                if (rand < cumul) return entry.weaponData;
            }
            return _pool[_pool.Count - 1].weaponData;
        }
 
        private void CacheWeight()
        {
            _totalWeight = 0f;
            foreach (var e in _pool) _totalWeight += e.weight;
        }
 
#if UNITY_EDITOR
        // 인스펙터에서 풀 수정 시 가중치 재계산
        private void OnValidate() => CacheWeight();
#endif
    }
}
