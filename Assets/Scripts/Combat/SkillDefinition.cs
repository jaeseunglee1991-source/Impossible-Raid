using UnityEngine;

namespace BossRaid.Combat
{
    /// <summary>
    /// ──────────────────────────────────────────────────────────────────
    /// SkillDefinition — 하나의 스킬에 대한 메타데이터
    ///
    /// 직업 클래스 Awake()에서 allSkills 리스트로 등록합니다.
    /// SkillEquipManager가 이 데이터를 읽어 UI를 구성합니다.
    ///
    /// [설계 원칙]
    ///   - 스킬 실행 로직은 직업 클래스의 UseSkill(int idx) / UseUltimate()에 유지
    ///   - SkillDefinition은 "무엇이 있는지" 만 기술 (이름, 쿨타임, 설명)
    ///   - 실행은 characterBase.TryUseSkill(slotIndex)가 내부적으로
    ///     allSkills[equippedSlots[slotIndex]].UseSkillLogic() 호출
    /// ──────────────────────────────────────────────────────────────────
    /// </summary>
    [System.Serializable]
    public class SkillDefinition
    {
        [Header("기본 정보")]
        public string skillName   = "스킬";
        public string description = "";

        [Header("전투 설정")]
        public float  cooldown      = 8f;
        public bool   canInterrupt  = false;   // true이면 보스 캐스팅 차단 가능 (HUD 글로우)
        public bool   isUltimate    = false;   // true이면 궁극기 슬롯에 배치

        [Header("UI")]
        [Tooltip("없으면 SkillButtonUI가 슬롯 색상으로 대체합니다.")]
        public Sprite icon = null;

        // ─────────────────────────────────────────
        //  스킬 인덱스 (직업 클래스 UseSkill / UseUltimate 연결 키)
        // ─────────────────────────────────────────

        /// <summary>
        /// 직업 클래스의 UseSkill(int idx) 파라미터에 넘길 값.
        /// isUltimate == true면 이 값은 무시되고 UseUltimate()가 호출됩니다.
        /// </summary>
        public int skillIndex = 0;

        // ─────────────────────────────────────────
        //  편의 생성자
        // ─────────────────────────────────────────

        public SkillDefinition() { }

        public SkillDefinition(string name, float cd, int idx,
                               bool interrupt = false, bool ultimate = false,
                               string desc = "")
        {
            skillName    = name;
            cooldown     = cd;
            skillIndex   = idx;
            canInterrupt = interrupt;
            isUltimate   = ultimate;
            description  = desc;
        }
    }
}
