using UnityEngine;
using System.Collections.Generic;

namespace BossRaid.Combat
{
    /// <summary>
    /// ──────────────────────────────────────────────────────────────────
    /// DangerZoneVisualizer — DangerZoneManager에 등록된 위험 구역을
    /// 실시간으로 바닥에 시각적 표시(Ground Indicator)로 렌더링합니다.
    ///
    /// ■ 프리팹 없이 동작: SpriteRenderer + 코드 생성 텍스처로 장판 표시
    /// ■ 경고(텔레그래프) 단계: 반투명 → 피해 직전: 불투명 빨강 깜빡임
    /// ■ 프로덕션 전환: prefab 필드에 실제 이펙트를 넣으면 자동 교체
    ///
    /// [씬 배치] BossAI와 같은 씬에 빈 오브젝트로 배치하거나,
    ///           BattleManager.InitializeBattle()에서 자동 생성
    /// ──────────────────────────────────────────────────────────────────
    /// </summary>
    public class DangerZoneVisualizer : MonoBehaviour
    {
        public static DangerZoneVisualizer Instance { get; private set; }

        [Header("시각 설정")]
        [Tooltip("장판 기본 색상 (텔레그래프 단계)")]
        public Color warningColor = new Color(1f, 0.3f, 0f, 0.25f);
        
        [Tooltip("피해 직전 색상 (위험 강조)")]
        public Color dangerColor = new Color(1f, 0f, 0f, 0.6f);

        [Tooltip("원형 장판 프리팹 (없으면 자동 생성)")]
        public GameObject circleIndicatorPrefab;

        // 활성 표시기: DangerZone.id → 표시 오브젝트
        private readonly Dictionary<string, GameObject> _indicators 
            = new Dictionary<string, GameObject>();

        // 빌트인 원형 텍스처 (프리팹 없을 때 폴백)
        private Texture2D _circleTexture;
        private Sprite _circleSprite;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            
            GenerateCircleTexture();
        }

        private void Update()
        {
            var zones = DangerZoneManager.ActiveZones;

            // 새로운 위험 구역에 대해 인디케이터 생성
            foreach (var zone in zones)
            {
                if (!_indicators.ContainsKey(zone.id))
                {
                    CreateIndicator(zone);
                }
            }

            // 기존 인디케이터 업데이트 / 만료된 것 제거
            var toRemove = new List<string>();
            foreach (var kv in _indicators)
            {
                // 해당 zone이 아직 살아있는지 확인
                DangerZone matchedZone = null;
                foreach (var z in zones)
                {
                    if (z.id == kv.Key) { matchedZone = z; break; }
                }

                if (matchedZone == null)
                {
                    // 만료됨 — 인디케이터 제거
                    if (kv.Value != null) Destroy(kv.Value);
                    toRemove.Add(kv.Key);
                }
                else
                {
                    // 업데이트: 위치, 크기, 색상 애니메이션
                    UpdateIndicator(kv.Value, matchedZone);
                }
            }

            foreach (var key in toRemove)
                _indicators.Remove(key);
        }

        // ═══════════════════════════════════════════════════════════
        //  인디케이터 생성
        // ═══════════════════════════════════════════════════════════

        private void CreateIndicator(DangerZone zone)
        {
            GameObject indicator;

            if (circleIndicatorPrefab != null && zone.shape == DangerShape.Circle)
            {
                indicator = Instantiate(circleIndicatorPrefab);
            }
            else
            {
                // 코드 기반 폴백 인디케이터 (프리팹 없이도 동작)
                indicator = new GameObject($"DangerIndicator_{zone.id}");
                var sr = indicator.AddComponent<SpriteRenderer>();
                sr.sprite = _circleSprite;
                sr.color = warningColor;
                sr.sortingOrder = -1; // 캐릭터 아래에 그리기
            }

            // 위치 및 크기 설정
            indicator.transform.position = new Vector3(zone.center.x, zone.center.y, 0.1f);
            
            float diameter = zone.radius * 2f;
            if (zone.shape == DangerShape.Donut)
                diameter = zone.radius * 2f; // 외원 기준

            // 스프라이트 기본 크기가 1x1이므로 radius로 스케일링
            indicator.transform.localScale = new Vector3(diameter, diameter, 1f);

            _indicators[zone.id] = indicator;
        }

        // ═══════════════════════════════════════════════════════════
        //  인디케이터 업데이트 (색상 애니메이션)
        // ═══════════════════════════════════════════════════════════

        private void UpdateIndicator(GameObject indicator, DangerZone zone)
        {
            if (indicator == null) return;

            // 위치 추적 (보스가 이동하는 패턴용)
            indicator.transform.position = new Vector3(zone.center.x, zone.center.y, 0.1f);

            var sr = indicator.GetComponent<SpriteRenderer>();
            if (sr == null) return;

            // 경고→위험 색상 전환 (남은 시간에 따라)
            float totalTime = zone.remainingTime + zone.warningTime;
            float dangerRatio = 1f - Mathf.Clamp01(zone.remainingTime / Mathf.Max(totalTime, 0.1f));
            
            Color baseColor = Color.Lerp(warningColor, dangerColor, dangerRatio);

            // 피해 직전 깜빡임 효과 (남은 시간 1초 이내)
            if (zone.remainingTime < 1f)
            {
                float blink = Mathf.PingPong(Time.time * 8f, 1f); // 빠른 깜빡임
                baseColor.a = Mathf.Lerp(0.3f, 0.8f, blink);
            }

            sr.color = baseColor;
        }

        // ═══════════════════════════════════════════════════════════
        //  코드 기반 원형 텍스처 생성 (프리팹 불필요)
        // ═══════════════════════════════════════════════════════════

        private void GenerateCircleTexture()
        {
            int size = 64; // 저해상도로 모바일 최적화
            _circleTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            _circleTexture.filterMode = FilterMode.Bilinear;

            float center = size * 0.5f;
            float radiusSq = center * center;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center + 0.5f;
                    float dy = y - center + 0.5f;
                    float distSq = dx * dx + dy * dy;

                    if (distSq <= radiusSq)
                    {
                        // 가장자리 페이드 (안티앨리어싱)
                        float edge = 1f - Mathf.Clamp01((Mathf.Sqrt(distSq) - center + 2f) / 4f);
                        _circleTexture.SetPixel(x, y, new Color(1f, 1f, 1f, edge));
                    }
                    else
                    {
                        _circleTexture.SetPixel(x, y, Color.clear);
                    }
                }
            }

            _circleTexture.Apply();

            _circleSprite = Sprite.Create(
                _circleTexture,
                new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f),
                size // pixelsPerUnit = size → 원 크기 = 1 월드 유닛
            );
        }

        private void OnDestroy()
        {
            // 메모리 해제
            foreach (var kv in _indicators)
            {
                if (kv.Value != null) Destroy(kv.Value);
            }
            _indicators.Clear();

            if (_circleTexture != null) Destroy(_circleTexture);
        }
    }
}
