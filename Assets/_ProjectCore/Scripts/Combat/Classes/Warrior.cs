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
            threatMultiplier = 1.5f; // 탱커 어그로 배율

            RegisterSkills(
                new SkillDefinition("회전 베기",   5f, 0,
                    desc: "주변 적 전체에 공격력 2.5배 광역 피해"),
                new SkillDefinition("전장의 함성", 8f, 1,
                    desc: "자신에게 어그로 대폭 집중, 3초간 피해 경감"),
                new SkillDefinition("방패 강타",   6f, 2, interrupt: true,
                    desc: "보스 캐스팅 차단 + 200 피해 + 어그로 획득")
            );
            RegisterUltimate(new SkillDefinition("불굴의 도발", 15f, 0, ultimate: true,
                desc: "5초간 피해 50% 감소 + 최대 체력 20% 보호막"));
        }

        public override void UseSkill(int idx)
        {
            switch (idx)
            {
                case 0: // 회전 베기
                    float aoe = autoAttackDamage * 2.5f;
                    if (targetBoss != null) { DealDamageTo(targetBoss, aoe); AddThreat(aoe * 2f); }
                    Debug.Log($"<color=orange>[전사] 회전 베기! {aoe} 광역 피해!</color>");
                    break;

                case 1: // 전장의 함성
                    AddThreat(3000f);
                    StartCoroutine(DefensiveCry(3f));
                    Debug.Log("<color=yellow>[전사] 전장의 함성! 어그로 집중 + 피해 경감!</color>");
                    break;

                case 2: // 방패 강타 (차단)
                    if (targetBoss != null) { DealDamageTo(targetBoss, 200f); targetBoss.Interrupt(); AddThreat(500f); }
                    Debug.Log("<color=blue>[전사] 방패 강타! 차단 성공!</color>");
                    break;
            }
        }

        public override void UseUltimate()
        {
            shieldAmount += maxHealth * 0.2f;
            StartCoroutine(DamageReductionBuff(5f));
            if (targetBoss != null) { targetBoss.Interrupt(); AddThreat(5000f); }
            Debug.Log("<color=yellow>[전사] 불굴의 도발! 5초간 피해 50% 감소!</color>");
        }

        private IEnumerator DamageReductionBuff(float duration)
        {
            damageReductionMultiplier = 0.5f;
            yield return new WaitForSeconds(duration);
            damageReductionMultiplier = 1.0f;
        }

        private IEnumerator DefensiveCry(float duration)
        {
            damageReductionMultiplier = 0.7f;
            yield return new WaitForSeconds(duration);
            damageReductionMultiplier = 1.0f;
        }

        protected override float CalculateDamageReduction(float incoming)
            => incoming * damageReductionMultiplier;
    }
}
