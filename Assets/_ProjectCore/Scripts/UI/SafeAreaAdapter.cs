using UnityEngine;

namespace BossRaid.UI
{
    /// <summary>
    /// Canvas 내 최상위 패널(RectTransform)에 부착하면,
    /// 노치/카메라 구멍/하단 네비게이션 바를 피해 UI를 안전 영역(Safe Area) 안으로 자동 조정합니다.
    /// 
    /// 사용법: Canvas 바로 아래에 "SafeArea" 빈 오브젝트를 만들고 이 스크립트를 부착,
    ///         모든 UI 요소를 그 SafeArea 하위에 배치하면 됩니다.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaAdapter : MonoBehaviour
    {
        private RectTransform _rect;
        private Rect _lastSafeArea;
        private Vector2Int _lastScreenSize;
        private ScreenOrientation _lastOrientation;

        private void Awake()
        {
            _rect = GetComponent<RectTransform>();
            ApplySafeArea();
        }

        private void Update()
        {
            // 회전이나 해상도 변경 시 재계산
            if (Screen.safeArea != _lastSafeArea ||
                Screen.width != _lastScreenSize.x ||
                Screen.height != _lastScreenSize.y ||
                Screen.orientation != _lastOrientation)
            {
                ApplySafeArea();
            }
        }

        /// <summary>
        /// Screen.safeArea를 기준으로 RectTransform의 앵커를 조정합니다.
        /// 노치가 있는 쪽은 UI가 밀려들어오고, 없는 쪽은 그대로 유지됩니다.
        /// </summary>
        public void ApplySafeArea()
        {
            Rect safeArea = Screen.safeArea;
            _lastSafeArea = safeArea;
            _lastScreenSize = new Vector2Int(Screen.width, Screen.height);
            _lastOrientation = Screen.orientation;

            if (Screen.width <= 0 || Screen.height <= 0) return;

            // Safe Area를 앵커 비율(0~1)로 변환
            Vector2 anchorMin = safeArea.position;
            Vector2 anchorMax = safeArea.position + safeArea.size;

            anchorMin.x /= Screen.width;
            anchorMin.y /= Screen.height;
            anchorMax.x /= Screen.width;
            anchorMax.y /= Screen.height;

            _rect.anchorMin = anchorMin;
            _rect.anchorMax = anchorMax;
            _rect.offsetMin = Vector2.zero;
            _rect.offsetMax = Vector2.zero;

#if UNITY_EDITOR
            Debug.Log($"[SafeArea] 적용됨: {safeArea} → anchorMin({anchorMin.x:F3},{anchorMin.y:F3}), anchorMax({anchorMax.x:F3},{anchorMax.y:F3})");
#endif
        }
    }
}
