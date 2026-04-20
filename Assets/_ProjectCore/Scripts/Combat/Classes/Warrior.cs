using UnityEngine;
using System.Collections;
using BossRaid.Combat.Boss;
using BossRaid.Managers;

namespace BossRaid.Combat.Classes
{
    public class Warrior : CharacterBase
    {
        protected override void Awake()
        {
            base.Awake();
            role          = CharacterRole.Tank;
            characterName = "전사";
            
            // 초기 스탯 설정 (리니지 로우 스탯 버전)
            maxHpUpgrade.baseStat = 180f; // 기사: 높은 체력
            initialAttackDamage = 8f;
            initialAttackSpeed = 1.4f;    // 묵직한 공격
            initialAttackRange = 3f;
            initialDefense = 5f;          // 기본 방어력 높음
            threatMultiplier = 3.0f;

            RegisterSkills(
                new SkillDefinition("도발의 일격",   4f, 0,
                    desc: "강하게 내리쳐 공격력 2배 피해 + 어그로 대량 획득"),
                new SkillDefinition("전술적 고함",   8f, 1,
                    desc: "3초간 파티 전체 피해 20% 감소"),
                new SkillDefinition("방패 강타",     6f, 2, interrupt: true,
                    desc: "보스 캐스팅 차단 + 넉백")
            );
            RegisterUltimate(new SkillDefinition("방벽 전개", 20f, 0, ultimate: true,
                desc: "5초간 무적에 가까운 방어(90% 감소) + 광역 도발"));
        }

        public override void UseSkill(int idx)
        {
            if (targetBoss == null) return;

            switch (idx)
            {
                case 0: // 도발의 일격
                    float tauntDmg = autoAttackDamage * 2.0f;
                    DealDamageTo(targetBoss, tauntDmg);
                    AddThreat(tauntDmg * 5f); // 추가 어그로 배율
                    Debug.Log($"<color=orange>[전사] 도발의 일격! 어그로 폭증</color>");
                    break;

                case 1: // 전술적 고함
                    ApplyPartyBuff(ally => {
                        // 간단한 버프 로직 (실제로는 버프 관리 클래스가 필요하지만 여기선 즉시 처리)
                        StartCoroutine(AllyProtection(ally, 3f));
                    });
                    Debug.Log("<color=yellow>[전사] 전술적 고함! 파티 방어력 증강</color>");
                    break;

                case 2: // 방패 강타 (차단)
                    DealDamageTo(targetBoss, 100f);
                    targetBoss.Interrupt();
                    Debug.Log("<color=blue>[전사] 방패 강타! 차단 성공!</color>");
                    break;
            }
        }

        public override void UseUltimate()
        {
            if (targetBoss == null) return;

            StartCoroutine(ShieldWallDuration(5f));
            AddThreat(10000f);
            targetBoss.Interrupt();
            Debug.Log("<color=red>[전사] 궁극기: 방벽 전개! 5초간 철벽 방어</color>");
        }

        private IEnumerator AllyProtection(CharacterBase ally, float duration)
        {
            // 방어력 +5 증가 (고정치)
            var mod = new StatModifier(5f, StatModType.Flat, this);
            ally.AddStatModifier(StatType.Defense, mod);
            yield return new WaitForSeconds(duration);
            ally.RemoveStatModifier(StatType.Defense, mod);
        }

        private IEnumerator ShieldWallDuration(float duration)
        {
            // 방어력 +20 증가 (철벽)
            var mod = new StatModifier(20f, StatModType.Flat, this);
            AddStatModifier(StatType.Defense, mod);
            yield return new WaitForSeconds(duration);
            RemoveStatModifier(StatType.Defense, mod);
        }

        protected override float CalculateDamageReduction(float incoming)
        {
            // 베이스 로직(CharacterBase) 사용: (1 - damageReduction) 곱함
            return base.CalculateDamageReduction(incoming);
        }
    }
}
