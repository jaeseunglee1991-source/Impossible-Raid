using UnityEngine;
 
namespace BossRaid.Gacha
{
    /// <summary>
    /// 가챠 무기 한 종류의 정의 데이터. ScriptableObject로 인스펙터에서 등록.
    /// 강화는 인스턴스가 아닌 '종류 단위'로 관리되므로 BaseAttack만 보관하고
    /// 현재 공격력(currentAttack)은 InventoryWeapon이 런타임에 추적한다.
    /// </summary>
    [CreateAssetMenu(fileName = "New WeaponData", menuName = "BossRaid/Gacha/WeaponData")]
    public class WeaponData : ScriptableObject
    {
        [Header("기본 정보")]
        public int    weaponId;
        public string weaponName = "무기";
 
        [Header("스탯")]
        public float baseAttack = 10f;
 
        // 강화 1레벨당 공격력 증가량 (로우 스탯 밸런스 고정값)
        public const float EnhanceStatBonus = 0.5f;
 
        /// <summary>
        /// n강 → (n+1)강에 필요한 중복 무기 수.
        /// 0강→1강 = 1개 / n강→(n+1)강 = n개
        /// </summary>
        public int GetRequiredDuplicates(int currentLevel)
            => Mathf.Max(1, currentLevel);
    }
}
