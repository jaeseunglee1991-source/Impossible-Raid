using UnityEngine;

namespace BossRaid.Combat.Boss
{
    /// <summary>
    /// 보스 및 일반 몬스터의 공통 인터페이스.
    /// 플레이어 캐릭터들이 대상의 종류에 상관없이 데미지를 입히거나 패턴을 차단할 수 있게 합니다.
    /// </summary>
    public interface IBossPatternHandler
    {
        string GetBossName();
        void TakeDamage(float amount);
        void Interrupt();
        bool IsCasting();
    }
}
