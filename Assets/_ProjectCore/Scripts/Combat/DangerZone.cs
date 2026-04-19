using UnityEngine;
using System.Collections.Generic;

namespace BossRaid.Combat
{
    /// <summary>
    /// ──────────────────────────────────────────────────────────────────
    /// DangerZone — 보스가 생성하는 위험 구역 데이터
    ///
    /// ■ WoW/FF14 스타일: 보스가 패턴 시전 시 "장판"을 월드에 등록
    /// ■ AI(TagCharacterController)는 매 프레임 이 목록을 읽고 회피 판정
    /// ■ 시각적 표현(Ground Indicator)과 로직(피해 판정)을 분리하여
    ///   향후 셰이더 교체/이펙트 확장이 자유로움
    ///
    /// [사용 흐름]
    ///   BossAI 패턴 코루틴 → DangerZoneManager.Register(zone)
    ///     → AI가 IsInsideDanger() 체크 → 안전 방향으로 이동
    ///     → 지속 시간 만료 → DangerZoneManager.Unregister(zone)
    /// ──────────────────────────────────────────────────────────────────
    /// </summary>
    public enum DangerShape
    {
        Circle,     // 원형 장판 (중심 + 반지름)
        Cone,       // 부채꼴 (중심 + 방향 + 각도 + 거리)
        Donut,      // 도넛 (내원~외원 사이만 위험)
        Line        // 직선 범위 (시작~끝 + 폭)
    }

    [System.Serializable]
    public class DangerZone
    {
        public string id;              // 고유 식별자 (디버그용)
        public DangerShape shape;
        public Vector3 center;         // 월드 좌표 기준 중심
        public float radius;           // Circle/Donut 외원 반경
        public float innerRadius;      // Donut 내원 반경 (Circle이면 0)
        public Vector3 direction;      // Cone/Line 방향
        public float angle;            // Cone 부채꼴 반각 (도)
        public float width;            // Line 폭
        public float remainingTime;    // 남은 지속 시간 (초)
        public float warningTime;      // 텔레그래프(경고) 시간 (피해 전)
        public float damage;           // 피해 발생 시 데미지
        public bool  hasDealtDamage;   // 이미 데미지를 줬는지 (1회성 폭발용)
        public bool  isDot;            // 지속 피해 여부

        /// <summary>월드 좌표 pos가 이 위험 구역 내부인지 판정</summary>
        public bool IsInside(Vector3 pos)
        {
            Vector2 p = new Vector2(pos.x, pos.y);
            Vector2 c = new Vector2(center.x, center.y);

            switch (shape)
            {
                case DangerShape.Circle:
                    return Vector2.Distance(p, c) <= radius;

                case DangerShape.Donut:
                    float dist = Vector2.Distance(p, c);
                    return dist >= innerRadius && dist <= radius;

                case DangerShape.Cone:
                    float distCone = Vector2.Distance(p, c);
                    if (distCone > radius) return false;
                    Vector2 dir2D = new Vector2(direction.x, direction.y).normalized;
                    Vector2 toPos = (p - c).normalized;
                    float angleBetween = Vector2.Angle(dir2D, toPos);
                    return angleBetween <= angle;

                case DangerShape.Line:
                    // 선분(center → center+direction*radius) 기준 수직 거리
                    Vector2 lineDir = new Vector2(direction.x, direction.y).normalized;
                    Vector2 toP = p - c;
                    float projection = Vector2.Dot(toP, lineDir);
                    if (projection < 0 || projection > radius) return false;
                    float perpDist = Mathf.Abs(toP.x * (-lineDir.y) + toP.y * lineDir.x);
                    return perpDist <= width * 0.5f;

                default:
                    return false;
            }
        }

        /// <summary>주어진 위치에서 이 위험 구역 밖으로 탈출하기 위한 최적 방향</summary>
        public Vector3 GetEscapeDirection(Vector3 pos)
        {
            Vector2 p = new Vector2(pos.x, pos.y);
            Vector2 c = new Vector2(center.x, center.y);
            Vector2 away = (p - c);

            switch (shape)
            {
                case DangerShape.Circle:
                    // 중심에서 바깥으로 도망
                    if (away.sqrMagnitude < 0.01f) away = Random.insideUnitCircle.normalized;
                    return new Vector3(away.normalized.x, away.normalized.y, 0f);

                case DangerShape.Donut:
                    // 도넛: 내원 안쪽(안전)이나 외원 바깥(안전). 더 가까운 쪽으로
                    float d = away.magnitude;
                    float toInner = d - innerRadius;
                    float toOuter = radius - d;
                    if (toInner < toOuter)
                        return new Vector3(-away.normalized.x, -away.normalized.y, 0f); // 안쪽으로
                    else
                        return new Vector3(away.normalized.x, away.normalized.y, 0f); // 바깥으로

                case DangerShape.Cone:
                    // 부채꼴의 수직 방향으로 빠지기
                    Vector2 coneDir = new Vector2(direction.x, direction.y).normalized;
                    Vector2 perp = new Vector2(-coneDir.y, coneDir.x); // 수직
                    float side = Vector2.Dot(p - c, perp);
                    return new Vector3(perp.x * Mathf.Sign(side), perp.y * Mathf.Sign(side), 0f).normalized;

                case DangerShape.Line:
                    // 직선의 수직 방향으로 빠지기
                    Vector2 lineDir2 = new Vector2(direction.x, direction.y).normalized;
                    Vector2 linePerp = new Vector2(-lineDir2.y, lineDir2.x);
                    float lineSide = Vector2.Dot(p - c, linePerp);
                    return new Vector3(linePerp.x * Mathf.Sign(lineSide), linePerp.y * Mathf.Sign(lineSide), 0f).normalized;

                default:
                    return (pos - center).normalized;
            }
        }
    }

    /// <summary>
    /// 전역 위험 구역 레지스트리. BossAI가 등록하고, AI가 읽습니다.
    /// </summary>
    public static class DangerZoneManager
    {
        private static readonly List<DangerZone> _activeZones = new List<DangerZone>();

        public static IReadOnlyList<DangerZone> ActiveZones => _activeZones;

        public static void Register(DangerZone zone)
        {
            _activeZones.Add(zone);
            Debug.Log($"<color=red>[DangerZone] 등록: {zone.id} ({zone.shape}, R={zone.radius})</color>");
        }

        public static void Unregister(DangerZone zone)
        {
            _activeZones.Remove(zone);
        }

        /// <summary>매 프레임 BossAI.Update 또는 별도 매니저에서 호출</summary>
        public static void Tick(float deltaTime)
        {
            for (int i = _activeZones.Count - 1; i >= 0; i--)
            {
                _activeZones[i].remainingTime -= deltaTime;
                if (_activeZones[i].remainingTime <= 0f)
                {
                    Debug.Log($"[DangerZone] 만료: {_activeZones[i].id}");
                    _activeZones.RemoveAt(i);
                }
            }
        }

        /// <summary>모든 위험 구역을 초기화 (전투 종료 시)</summary>
        public static void ClearAll()
        {
            _activeZones.Clear();
        }

        /// <summary>주어진 위치가 어떤 위험 구역 안에 있는지 판정</summary>
        public static bool IsInAnyDanger(Vector3 pos, out DangerZone nearestZone)
        {
            nearestZone = null;
            float closestDist = float.MaxValue;

            foreach (var zone in _activeZones)
            {
                if (zone.IsInside(pos))
                {
                    float dist = Vector3.Distance(pos, zone.center);
                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        nearestZone = zone;
                    }
                }
            }
            return nearestZone != null;
        }
    }
}
