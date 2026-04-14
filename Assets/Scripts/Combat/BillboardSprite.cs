using UnityEngine;

namespace BossRaid.Combat
{
    /// <summary>
    /// 2D 환경에서는 빌보드 불필요 — 비활성/무동작 유지
    /// 3D→2D 전환으로 인해 카메라가 정면 고정이므로 회전 로직 제거
    /// </summary>
    public class BillboardSprite : MonoBehaviour
    {
        // 2D 쿼터뷰에서는 카메라가 정면(Z=-10)으로 고정되어 있으므로
        // 별도의 빌보드(얼굴 향하기) 로직이 필요하지 않습니다.
        // 이 컴포넌트는 기존 참조 호환성을 위해 유지합니다.
    }
}
