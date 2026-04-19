namespace BossRaid.Combat
{
    /// <summary>
    /// 캐릭터의 애니메이션 처리를 추상화하는 인터페이스입니다.
    /// SPUM, 일반 애니메이터, 혹은 다른 에셋으로 교체하더라도
    /// 이 인터페이스만 구현하면 CharacterBase가 정상적으로 동작합니다.
    /// </summary>
    public interface ICharacterAnimationHandler
    {
        void PlayAnimation(string stateName, int index = 0);
        void SetMoveSpeed(float speed); // 필요 시 이동 속도에 따른 애니메이션 배율 조절
    }

    public enum CharacterAnimState
    {
        Idle,
        Move,
        Attack,
        Hit,
        Die,
        Skill
    }
}
