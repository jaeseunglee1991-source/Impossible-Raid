using UnityEngine;

namespace BossRaid.Combat.Camera
{
    /// <summary>
    /// 2D 쿼터뷰 카메라 추적기
    /// - Orthographic 카메라로 강제 전환
    /// - XY 평면만 추적, Z축 고정 (-10)
    /// - 3D LookAt 제거
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        public Transform target;
        
        [Tooltip("타겟으로부터의 오프셋 (2D: X·Y만 사용, Z는 카메라 깊이)")]
        public Vector3 offset = new Vector3(0f, 2f, -10f);
        
        [Tooltip("카메라 스무딩 속도 (낮을수록 부드러움)")]
        public float smoothSpeed = 8f;

        [Tooltip("직교 카메라 크기 (픽셀 단위 뷰 범위 절반)")]
        public float orthographicSize = 6f;

        private UnityEngine.Camera _cam;

        private void Awake()
        {
            _cam = GetComponent<UnityEngine.Camera>();
            if (_cam != null)
            {
                _cam.orthographic = true;
                _cam.orthographicSize = orthographicSize;
                // 2D 환경에서는 카메라 회전 고정 (XY 평면 정면 응시)
                transform.rotation = Quaternion.identity;
            }
        }

        private void LateUpdate()
        {
            if (target == null) return;

            // XY 평면만 추적, Z는 offset 값(시야 깊이)으로 고정
            float targetX = target.position.x + offset.x;
            float targetY = target.position.y + offset.y;

            Vector3 desiredPosition = new Vector3(targetX, targetY, offset.z);
            transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

            // ※ 2D에서는 LookAt 사용 금지 — 카메라 회전 유지
        }
    }
}
