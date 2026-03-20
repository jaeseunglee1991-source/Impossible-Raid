using UnityEngine;

namespace BossRaid.Combat.Camera
{
    public class CameraFollow : MonoBehaviour
    {
        public Transform target;
        public Vector3 offset = new Vector3(0, 15, -10); // 기본 쿼터뷰 각도
        public float smoothSpeed = 0.125f;

        private void LateUpdate()
        {
            if (target == null) return;

            Vector3 desiredPosition = target.position + offset;
            Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
            transform.position = smoothedPosition;

            transform.LookAt(target); // 타겟(플레이어)을 항상 정중앙에 응시
        }
    }
}
