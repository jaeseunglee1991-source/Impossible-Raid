using UnityEngine;
using System.Collections;

namespace BossRaid.Combat.Classes
{
    public class Rogue : CharacterBase
    {
        protected override void Awake()
        {
            base.Awake();
            role          = CharacterRole.MeleeDPS;
            characterName = "도적";
            
            // 초기 스탯 설정 (리니지 로우 스탯 버전)
            maxHpUpgrade.baseStat = 110f;
            initialAttackDamage = 10f;
            initialAttackSpeed = 1.2f;   // 도적: 빠른 편이지만 육안 확인 가능
            initialAttackRange = 2.5f;
            initialDefense = 2f;
            threatMultiplier = 0.5f;

            RegisterSkills(
                new SkillDefinition("암살",           5f, 0,
                    desc: "보스 뒤로 이동하여 공격력 4배 피해"),
                new SkillDefinition("독침 투척",       3f, 1,
                    desc: "단일 1.5배 피해 + 3초간 받는 피해 10% 증가"),
                new SkillDefinition("은신",           10f, 2,
                    desc: "3초간 타겟팅 제외 + 다음 공격력 2배")
            );
            RegisterUltimate(new SkillDefinition("그림자 춤", 25f, 0, ultimate: true,
                desc: "보스 2초 기절 + 총 공격력 10배 폭발적 연타"));
        }

        public override void UseSkill(int idx)
        {
            if (targetBoss == null) return;

            // [패시브] 보스 시전 중에는 기본 데미지 2배 (DealDamageTo 내부에서 처리하거나 여기서 보정)
            float passiveMult = targetBoss.IsCasting() ? 2.0f : 1.0f;

            switch (idx)
            {
                case 0: // 암살
                    float assassinDmg = autoAttackDamage * 4.0f * passiveMult;
                    // 보스 뒤로 위치 이동 (심플하게 연출)
                    transform.position = (targetBoss as MonoBehaviour).transform.position - (targetBoss as MonoBehaviour).transform.forward * 1.5f;
                    DealDamageTo(targetBoss, assassinDmg);
                    Debug.Log($"<color=purple>[도적] 암살! {assassinDmg} 피해 (시전중 보너스: {passiveMult}x)</color>");
                    break;

                case 1: // 독침 투척
                    DealDamageTo(targetBoss, autoAttackDamage * 1.5f * passiveMult);
                    // 방어력 감소 디버프 (기능 확장을 위해 로그만 남김)
                    Debug.Log($"<color=green>[도적] 독침 투척!</color>");
                    break;

                case 2: // 은신
                    StartCoroutine(StealthRoutine(3f));
                    Debug.Log("<color=gray>[도적] 은신 발동!</color>");
                    break;
            }
        }

        public override void UseUltimate()
        {
            if (targetBoss == null) return;

            targetBoss.Interrupt(); // 차단 겸 기절
            var bossAI = targetBoss as Boss.BossAI;
            if (bossAI != null) StartCoroutine(StunBoss(bossAI, 2f));
            
            float ultDmg = autoAttackDamage * 10.0f;
            DealDamageTo(targetBoss, ultDmg);
            Debug.Log("<color=red>[도적] 궁극기: 그림자 춤! 보스 무력화 및 폭딜</color>");
        }

        private IEnumerator StealthRoutine(float duration)
        {
            float prevThreat = threatMultiplier;
            threatMultiplier = 0f; // 타겟팅 완전 해제
            yield return new WaitForSeconds(duration);
            threatMultiplier = prevThreat;
        }

        private IEnumerator StunBoss(Boss.BossAI boss, float duration)
        {
            boss.isStaggered = true;
            yield return new WaitForSeconds(duration);
            boss.isStaggered = false;
        }
    }
}
