using UnityEngine;

namespace BossRaid.Combat
{
    /// <summary>
    /// 파티 태그(교체) 시스템을 위한 캐릭터 데이터 모델.
    /// GameObject가 SetActive(false) 상태에서도 HP/MP/쿨타임이 메모리에 유지됨.
    /// 쿨타임은 Time.time 타임스탬프 방식으로 오브젝트 비활성화 구간도 정확히 계산.
    /// </summary>
    [System.Serializable]
    public class CharacterStatus
    {
        // ──────────────────────────────────────────────
        // ID & 역할
        // ──────────────────────────────────────────────
        public string characterName;
        public CharacterRole role;

        // ──────────────────────────────────────────────
        // HP / MP
        // ──────────────────────────────────────────────
        public float MaxHP   = 1000f;
        public float CurrentHP;
        public float MaxMP   = 100f;
        public float CurrentMP;

        public bool IsDead => CurrentHP <= 0f;

        // ──────────────────────────────────────────────
        // 스킬 쿨타임 (직업당 정확히 2개 — 기획서 기준)
        //   Skill1: 일반기
        //   Skill2: 생존기 / 궁극기
        // ──────────────────────────────────────────────
        public float Skill1MaxCooldown = 8f;
        public float Skill2MaxCooldown = 20f;

        // Time.time 기준 마지막 사용 타임스탬프
        // SetActive(false) 구간은 Time.time이 계속 흐르므로 쿨타임도 정확히 단축됨
        private float _skill1LastUsed = float.NegativeInfinity;
        private float _skill2LastUsed = float.NegativeInfinity;

        // ──────────────────────────────────────────────
        // 쿨타임 조회
        // ──────────────────────────────────────────────
        public bool IsSkill1Ready(float currentTime) =>
            (currentTime - _skill1LastUsed) >= Skill1MaxCooldown;

        public bool IsSkill2Ready(float currentTime) =>
            (currentTime - _skill2LastUsed) >= Skill2MaxCooldown;

        public float GetSkill1Remaining(float currentTime) =>
            Mathf.Max(0f, Skill1MaxCooldown - (currentTime - _skill1LastUsed));

        public float GetSkill2Remaining(float currentTime) =>
            Mathf.Max(0f, Skill2MaxCooldown - (currentTime - _skill2LastUsed));

        // ──────────────────────────────────────────────
        // 스킬 사용 기록
        // ──────────────────────────────────────────────
        public bool TryUseSkill1(float currentTime)
        {
            if (!IsSkill1Ready(currentTime)) return false;
            _skill1LastUsed = currentTime;
            return true;
        }

        public bool TryUseSkill2(float currentTime)
        {
            if (!IsSkill2Ready(currentTime)) return false;
            _skill2LastUsed = currentTime;
            return true;
        }

        // ──────────────────────────────────────────────
        // 초기화 & 부활
        // ──────────────────────────────────────────────
        public void Initialize()
        {
            CurrentHP = MaxHP;
            CurrentMP = MaxMP;
            _skill1LastUsed = float.NegativeInfinity;
            _skill2LastUsed = float.NegativeInfinity;
        }

        /// <summary>유료 부활 — 체력/마력 100% 복구</summary>
        public void ReviveToFull()
        {
            CurrentHP = MaxHP;
            CurrentMP = MaxMP;
            // 쿨타임은 부활 후에도 유지 (기획서 명시 없음 → 보수적 처리)
        }

        // ──────────────────────────────────────────────
        // 피해 / 회복
        // ──────────────────────────────────────────────
        /// <summary>쉴드 없이 순수 HP 차감 (CharacterBase.TakeDamage와 별개)</summary>
        public void TakeDamage(float amount)
        {
            if (IsDead) return;
            CurrentHP = Mathf.Clamp(CurrentHP - amount, 0f, MaxHP);
        }

        public void Heal(float amount)
        {
            if (IsDead) return;
            CurrentHP = Mathf.Clamp(CurrentHP + amount, 0f, MaxHP);
        }

        public void RestoreMP(float amount)
        {
            CurrentMP = Mathf.Clamp(CurrentMP + amount, 0f, MaxMP);
        }
    }
}
