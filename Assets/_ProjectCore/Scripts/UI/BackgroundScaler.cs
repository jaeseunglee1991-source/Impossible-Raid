using UnityEngine;

namespace BossRaid.UI
{
    /// <summary>
    /// SpriteRenderer가 붙은 배경 오브젝트에 부착하면,
    /// 어떤 기기 해상도/비율이든 카메라 화면을 꽉 채우도록 자동 스케일링합니다.
    /// - Orthographic 카메라 전용
    /// - 가로 모드(Landscape) 최적화
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class BackgroundScaler : MonoBehaviour
    {
        [Tooltip("배경이 화면보다 살짝 크게 나오도록 하는 여유 배율 (1.05 = 5% 여유)")]
        public float overscale = 1.05f;

        private SpriteRenderer _sr;
        private Camera _cam;
        private int _lastScreenWidth;
        private int _lastScreenHeight;

        private void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            _cam = Camera.main;
        }

        private void Start()
        {
            FitToScreen();
        }

        private void Update()
        {
            // 해상도가 바뀌었을 때(에디터에서 Game 뷰 크기 변경 시 등) 재계산
            if (Screen.width != _lastScreenWidth || Screen.height != _lastScreenHeight)
            {
                FitToScreen();
            }
        }

        /// <summary>
        /// 카메라의 월드 크기를 계산하고, 스프라이트가 그 크기를 완전히 덮도록 스케일을 조정합니다.
        /// "Cover" 방식: 가로/세로 중 더 큰 비율을 기준으로 확대하여 빈 공간이 없게 합니다.
        /// </summary>
        public void FitToScreen()
        {
            if (_sr == null || _sr.sprite == null || _cam == null) return;

            _lastScreenWidth = Screen.width;
            _lastScreenHeight = Screen.height;

            // 카메라가 보여주는 월드 영역 크기
            float camHeight = _cam.orthographicSize * 2f;
            float camWidth = camHeight * _cam.aspect;

            // 스프라이트 원본 월드 크기 (스케일 1일 때)
            Sprite sprite = _sr.sprite;
            float spriteWorldWidth = sprite.bounds.size.x;
            float spriteWorldHeight = sprite.bounds.size.y;

            // Cover 방식: 가로/세로 비율 중 큰 쪽에 맞춰야 빈 공간이 안 생김
            float scaleX = camWidth / spriteWorldWidth;
            float scaleY = camHeight / spriteWorldHeight;
            float scale = Mathf.Max(scaleX, scaleY) * overscale;

            transform.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
