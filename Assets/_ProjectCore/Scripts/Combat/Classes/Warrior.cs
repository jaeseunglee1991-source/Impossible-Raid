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
            maxHealth     = 2000f;
            autoAttackDamage = 50f;
            attackSpeed   = 1.0f;
            attackRange   = 3f;
            threatMultiplier = 3.0f; // 탱커: 피해의 300% 어그로

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
            float prevRed = ally.damageReductionMultiplier;
            ally.damageReductionMultiplier = 0.8f; // 20% 감소
            yield return new WaitForSeconds(duration);
            ally.damageReductionMultiplier = prevRed;
        }

        private IEnumerator ShieldWallDuration(float duration)
        {
            float prevRed = damageReductionMultiplier;
            damageReductionMultiplier = 0.1f; // 90% 감소
            yield return new WaitForSeconds(duration);
            damageReductionMultiplier = prevRed;
        }

        protected override float CalculateDamageReduction(float incoming)
            => incoming * damageReductionMultiplier;
    }
}
