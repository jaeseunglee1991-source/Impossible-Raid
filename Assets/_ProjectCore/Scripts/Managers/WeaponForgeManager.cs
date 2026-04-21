using System;
using UnityEngine;
using BossRaid.Gacha;
 
namespace BossRaid.Managers
{
    /// <summary>
    /// 가챠 무기 강화 전담 매니저.
    /// InventoryManager로부터 호출받아 연쇄 강화를 연산하고,
    /// 모든 단계가 끝난 뒤 UI 갱신 이벤트를 단 1회 발생시킨다.
    /// </summary>
    public class WeaponForgeManager : MonoBehaviour
    {
        // ═══════════════════════════════════════════════════════════
        //  싱글턴
        // ═══════════════════════════════════════════════════════════
 
        private static WeaponForgeManager _instance;
        public  static WeaponForgeManager Instance => _instance;
 
        // ═══════════════════════════════════════════════════════════
        //  생명주기
        // ═══════════════════════════════════════════════════════════
 
        private void Awake()
        {
            if (_instance != null && _instance != this) { Destroy(gameObject); return; }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
 
        // ═══════════════════════════════════════════════════════════
        //  핵심 강화 로직
        // ═══════════════════════════════════════════════════════════
 
        /// <summary>
        /// 강화 가능 조건을 만족하는 동안 연쇄 강화를 수행한다.
        ///
        /// 공식: Mathf.Max(1, currentLevel) 개의 중복 수량 소모 → 레벨 +1, 공격력 +0.5
        /// 연쇄 강화가 끝난 뒤 onEnhanced 콜백을 단 1회 호출 (UI 최적화).
        /// </summary>
        /// <param name="item">강화 대상 인벤토리 무기 항목</param>
        /// <param name="onEnhanced">연쇄 강화 완료 후 UI 갱신 콜백 (강화가 없었으면 호출 안 함)</param>
        public void TryEnhanceWeapon(InventoryWeapon item, Action<InventoryWeapon> onEnhanced = null)
        {
            if (item?.weaponData == null) return;
 
            bool didEnhance = false;
            int  required   = item.weaponData.GetRequiredDuplicates(item.enhanceLevel);
 
            // 조건을 만족하는 동안 연쇄 강화 (한 번에 10개·100개 뽑을 때 처리)
            while (item.duplicateAmount >= required)
            {
                item.duplicateAmount -= required;
                item.enhanceLevel    += 1;
                item.currentAttack   += WeaponData.EnhanceStatBonus;
                didEnhance            = true;
 
                required = item.weaponData.GetRequiredDuplicates(item.enhanceLevel);
            }
 
            // 연쇄 강화가 한 번이라도 발생했을 때만 콜백·로그 실행
            if (didEnhance)
            {
                onEnhanced?.Invoke(item);
                Debug.Log($"<color=green>[WeaponForge] {item.weaponData.weaponName} → " +
                          $"+{item.enhanceLevel} | 공격력 {item.currentAttack:F1} | " +
                          $"남은 중복 {item.duplicateAmount}개</color>");
            }
        }
 
        // ═══════════════════════════════════════════════════════════
        //  에디터 테스트 유틸
        // ═══════════════════════════════════════════════════════════
 
#if UNITY_EDITOR
        [ContextMenu("Test: 강화 시뮬레이션 출력")]
        private void TestSimulate()
        {
            Debug.Log("=== 강화 요구량 시뮬레이션 ===");
            int total = 0;
            for (int lv = 0; lv < 10; lv++)
            {
                int need = Mathf.Max(1, lv);
                total += need;
                Debug.Log($"  {lv}강 → {lv + 1}강: {need}개 필요 (누적 {total}개)");
            }
        }
#endif
    }
}
