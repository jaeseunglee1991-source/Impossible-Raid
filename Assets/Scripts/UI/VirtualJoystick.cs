using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace BossRaid.UI
{
    /// <summary>
    /// 가상 조이스틱 (터치/마우스 지원)
    /// 터치 시 해당 위치에 생성되는 동적 조이스틱
    /// </summary>
    public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [Header("Joystick Elements")]
        public RectTransform joystickArea;        // 전체 터치 영역
        public RectTransform outerCircle;         // 외부 원
        public RectTransform innerHandle;         // 내부 핸들
        
        [Header("Settings")]
        public float handleRange = 60f;           // 핸들 이동 최대 거리
        public float deadZone = 0.1f;             // 데드존

        /// <summary>현재 입력 방향 (정규화됨)</summary>
        public Vector2 InputDirection { get; private set; }

        private Canvas parentCanvas;
        private Camera uiCamera;
        private bool isActive = false;

        private void Start()
        {
            parentCanvas = GetComponentInParent<Canvas>();
            if (parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
                uiCamera = parentCanvas.worldCamera;
            
            // 초기 상태: 조이스틱 숨기기
            SetJoystickVisible(false);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            // 터치한 위치에 조이스틱 생성
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                joystickArea, eventData.position, uiCamera, out localPoint);

            outerCircle.anchoredPosition = localPoint;
            innerHandle.anchoredPosition = Vector2.zero;
            
            SetJoystickVisible(true);
            isActive = true;
            
            OnDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!isActive) return;

            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                outerCircle, eventData.position, uiCamera, out localPoint);

            // 핸들 범위 제한
            Vector2 clampedInput = Vector2.ClampMagnitude(localPoint, handleRange);
            innerHandle.anchoredPosition = clampedInput;

            // 정규화된 입력 방향 계산
            Vector2 normalized = clampedInput / handleRange;
            InputDirection = normalized.magnitude > deadZone ? normalized : Vector2.zero;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            isActive = false;
            InputDirection = Vector2.zero;
            innerHandle.anchoredPosition = Vector2.zero;
            SetJoystickVisible(false);
        }

        private void SetJoystickVisible(bool visible)
        {
            if (outerCircle != null)
            {
                var cg = outerCircle.GetComponent<CanvasGroup>();
                if (cg != null) cg.alpha = visible ? 1f : 0f;
                else outerCircle.gameObject.SetActive(visible);
            }
        }
    }
}
