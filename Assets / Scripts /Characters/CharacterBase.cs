using UnityEngine;

public class CharacterBase : MonoBehaviour
{
    // 일반 float 대신 SafeFloat 적용 완료
    public SafeFloat baseAttackPower = 0f;

    // 초기 스탯 세팅 예시
    public void GenerateInitialStats()
    {
        // 3번의 랜덤 능력치가 독립적으로 시행된 후 합산되어 암호화 저장됨
        float roll1 = Random.Range(1.0f, 5.0f);
        float roll2 = Random.Range(1.0f, 5.0f);
        float roll3 = Random.Range(1.0f, 5.0f);

        baseAttackPower = roll1 + roll2 + roll3;
    }

    public void ApplyDamageBuff(float multiplier)
    {
        // 기존과 똑같이 곱하기(*) 연산 사용 가능
        baseAttackPower *= multiplier;
    }
}
