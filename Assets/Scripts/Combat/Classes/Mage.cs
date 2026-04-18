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

            RegisterSkills(
                new SkillDefinition("메테오 낙하",   6f, 0,
                    desc: "화면 전체 공격력 3.5배 광역 피해"),
                new SkillDefinition("마법 화살",     2f, 1,
                    desc: "단일 타겟 공격력 1.5배 즉발"),
                new SkillDefinition("마법 폭발",    10f, 2,
                    desc: "보스 주변 광역 공격력 2배 + 넉백")
            );
            RegisterUltimate(new SkillDefinition("타임 프리즈", 25f, 0, ultimate: true,
                desc: "3초간 보스 행동 완전 정지 + 공격력 8배 추가 피해"));
        }

        public override void UseSkill(int idx)
        {
            switch (idx)
            {
                case 0: // 메테오 낙하
                    float metDmg = autoAttackDamage * 3.5f;
                    if (targetBoss != null) DealDamageTo(targetBoss, metDmg);
                    Debug.Log($"<color=red>[마법사] 메테오! {metDmg:F0} 광역 피해</color>");
                    break;

                case 1: // 마법 화살
                    float arrDmg = autoAttackDamage * 1.5f;
                    if (targetBoss != null) DealDamageTo(targetBoss, arrDmg);
                    Debug.Log($"[마법사] 마법 화살 {arrDmg:F0}");
                    break;

                case 2: // 마법 폭발
                    float expDmg = autoAttackDamage * 2f;
                    if (targetBoss != null) DealDamageTo(targetBoss, expDmg);
                    Debug.Log($"<color=orange>[마법사] 마법 폭발! {expDmg:F0}</color>");
                    break;
            }
        }

        public override void UseUltimate()
        {
            if (targetBoss != null) StartCoroutine(TimeFreezeRoutine());
            Debug.Log("<color=cyan>[마법사] 타임 프리즈! 3초 정지!</color>");
        }

        private IEnumerator TimeFreezeRoutine()
        {
            var boss = targetBoss as Boss.BossAI;
            if (boss != null) boss.isStaggered = true;
            DealDamageTo(targetBoss, autoAttackDamage * 8f);
            yield return new WaitForSeconds(3.0f);
            if (boss != null) boss.isStaggered = false;
        }
    }
}
