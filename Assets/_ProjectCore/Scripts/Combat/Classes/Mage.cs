using UnityEngine;
using System.Collections;

namespace BossRaid.Combat.Classes
{
    public class Mage : CharacterBase
    {
        protected override void Awake()
        {
            base.Awake();
            role          = CharacterRole.RangedDPS;
            characterName = "마법사";
            maxHealth     = 800f;
            autoAttackDamage = 60f;
            attackSpeed   = 1.5f;
            attackRange   = 6.0f;
            threatMultiplier = 1.0f; // 법사: 피해의 100% 어그로 (표준)

            RegisterSkills(
                new SkillDefinition("메테오 낙하",   8f, 0,
                    desc: "화면 전체 공격력 5배 광역 피해 (파밍 핵심)"),
                new SkillDefinition("마력 화살",     3f, 1,
                    desc: "단일 2배 피해. 사용 시 10초간 스킬 쿨타임 10% 감소 (중첩)"),
                new SkillDefinition("얼음 송곳",     6f, 2,
                    desc: "보스 이동속도 50% 감소 + 피해 2.5배")
            );
            RegisterUltimate(new SkillDefinition("시간 왜곡", 30f, 0, ultimate: true,
                desc: "10초간 파티 전체 공격 속도 및 스킬 쿨타임 속도 2배"));
        }

        private int _overloadStacks = 0;

        public override void UseSkill(int idx)
        {
            if (targetBoss == null) return;

            // [패시브] 스킬 사용 시 공격속도 증가 (로직상 복잡하므로 간단히 표현)
            _overloadStacks = Mathf.Min(_overloadStacks + 1, 5);
            attackSpeed = 1.5f * (1f - (_overloadStacks * 0.05f)); 

            switch (idx)
            {
                case 0: // 메테오 낙하
                    float metDmg = autoAttackDamage * 5.0f;
                    DealDamageTo(targetBoss, metDmg);
                    Debug.Log($"<color=red>[마법사] 메테오! {metDmg:F0} 광역 피해</color>");
                    break;

                case 1: // 마력 화살
                    DealDamageTo(targetBoss, autoAttackDamage * 2.0f);
                    Debug.Log($"[마법사] 마력 화살 (마력 폭주 {(_overloadStacks*5)}% 가속)");
                    break;

                case 2: // 얼음 송곳
                    DealDamageTo(targetBoss, autoAttackDamage * 2.5f);
                    Debug.Log($"<color=cyan>[마법사] 얼음 송곳! 보스 감속</color>");
                    break;
            }
        }

        public override void UseUltimate()
        {
            ApplyPartyBuff(ally => StartCoroutine(TimeWarpBuff(ally, 10f)));
            Debug.Log("<color=cyan>[마법사] 궁극기: 시간 왜곡! 파티 전체 2배속 기동</color>");
        }

        private IEnumerator TimeWarpBuff(CharacterBase ally, float duration)
        {
            float prevAS = ally.attackSpeed;
            ally.attackSpeed *= 0.5f; // 2배 빠름
            yield return new WaitForSeconds(duration);
            ally.attackSpeed = prevAS;
        }

    }
}
