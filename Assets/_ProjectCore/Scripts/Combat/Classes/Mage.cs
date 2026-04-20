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
            
            // 초기 스탯 설정 (리니지 로우 스탯 버전)
            maxHpUpgrade.baseStat = 80f;
            initialAttackDamage = 14f;
            initialAttackSpeed = 1.8f;   // 긴 영창 시간 반영
            initialAttackRange = 6.0f;
            initialDefense = 1f;
            threatMultiplier = 1.0f;

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

        private List<StatModifier> _overloadModifiers = new List<StatModifier>();

        public override void UseSkill(int idx)
        {
            if (targetBoss == null) return;

            // [패시브] 마력 폭주 스택 (최대 5스택, 각 5% 가속)
            if (_overloadModifiers.Count < 5)
            {
                var mod = new StatModifier(0.05f, StatModType.PercentAdd, this);
                _overloadModifiers.Add(mod);
                AddStatModifier(StatType.AttackSpeed, mod);
                StartCoroutine(RemoveOverloadStack(mod, 10f));
            }

            switch (idx)
            {
                case 0: // 메테오 낙하
                    float metDmg = autoAttackDamage * 5.0f;
                    DealDamageTo(targetBoss, metDmg);
                    Debug.Log($"<color=red>[마법사] 메테오! {metDmg:F0} 광역 피해</color>");
                    break;

                case 1: // 마력 화살
                    DealDamageTo(targetBoss, autoAttackDamage * 2.0f);
                    Debug.Log($"[마법사] 마력 화살 (마력 폭주 스택: {_overloadModifiers.Count})");
                    break;

                case 2: // 얼음 송곳
                    DealDamageTo(targetBoss, autoAttackDamage * 2.5f);
                    Debug.Log($"<color=cyan>[마법사] 얼음 송곳! 보스 감속</color>");
                    break;
            }
        }

        private IEnumerator RemoveOverloadStack(StatModifier mod, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (_overloadModifiers.Remove(mod))
            {
                RemoveStatModifier(StatType.AttackSpeed, mod);
            }
        }

        public override void UseUltimate()
        {
            ApplyPartyBuff(ally => StartCoroutine(TimeWarpBuff(ally, 10f)));
            Debug.Log("<color=cyan>[마법사] 궁극기: 시간 왜곡! 파티 전체 2배속 기동</color>");
        }

        private IEnumerator TimeWarpBuff(CharacterBase ally, float duration)
        {
            // 공격속도 30% 증가 (가속 로직)
            var mod = new StatModifier(0.3f, StatModType.PercentAdd, this);
            ally.AddStatModifier(StatType.AttackSpeed, mod);
            yield return new WaitForSeconds(duration);
            ally.RemoveStatModifier(StatType.AttackSpeed, mod);
        }

    }
}
