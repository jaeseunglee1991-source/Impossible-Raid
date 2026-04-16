using UnityEngine;

public class GrowthManager : MonoBehaviour
{
    // 일반 double 대신 SafeDouble 적용 완료
    public SafeDouble currentGold = 150.0;

    public void AddGold(double amount)
    {
        // 덧셈 기호(+)를 그대로 사용해도 내부적으로 자동 암호화/복호화가 진행됩니다.
        currentGold += amount;
        Debug.Log("현재 골드: " + currentGold);
    }

    // Supabase로 데이터 전송 시 예시
    public void SaveToSupabase()
    {
        double goldToSave = (double)currentGold;
        // DB 전송 로직 실행...
    }
}
