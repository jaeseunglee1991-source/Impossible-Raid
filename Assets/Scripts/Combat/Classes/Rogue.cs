using UnityEngine;
using System.Collections;

namespace BossRaid.Combat.Classes
{
    public class Rogue : CharacterBase
    {
        private bool _isNextAttackBoosted = false;

        protected override void Awake()
        {
            base.Awake();
            role          = CharacterRole.MeleeDPS;
            characterName = "도적";
            maxHealth     = 1200f;
            autoAttackDamage = 40f;
            attackSpeed   = 0.5f;
            attackRange   = 2.5f;

            RegisterSkills(
                new SkillDefinition("수리검 난사",   4f, 0,
                    desc: "공격력 1.5배 투사체. 다음 공격 강화 중이면 2배"),
                new SkillDefinition("독 발라치기",   8f, 1,
                    desc: "보스에게 5초간 DoT (초당 공격력 50%)"),
                new SkillDefinition("그림자 도약",   6f, 2,
                    desc: "보스 뒤로 순간이동 후 공격력 3배 기습")
            );
            RegisterUltimate(new SkillDefinition("그림자 회피", 12f, 0, ultimate: true,
                desc: "1초 무적 + 회피 후 다음 공격력 2배"));
        }

        public override void UseSkill(int idx)
        {
            switch (idx)
            {
                case 0: // 수리검 난사
                    float dmg = autoAttackDamage * 1.5f;
                    if (_isNextAttackBoosted) { dmg *= 2f; _isNextAttackBoosted = false; }
                    if (targetBoss != null) DealDamageTo(targetBoss, dmg);
                    Debug.Log($"<color=purple>[도적] 수리검 난사! {dmg:F0} 피해</color>");
                    break;

                case 1: // 독 발라치기
                    if (targetBoss != null) StartCoroutine(PoisonDot());
                    Debug.Log("<color=green>[도적] 독 발라치기! 5초 DoT 시작</color>");
                    break;

                case 2: // 그림자 도약
                    float backstab = autoAttackDamage * 3f;
                    if (targetBoss != null) DealDamageTo(targetBoss, backstab);
                    Debug.Log($"<color=purple>[도적] 그림자 도약! 기습 {backstab:F0} 피해</color>");
                    break;
            }
        }

        public override void UseUltimate()
        {
            StartCoroutine(DodgeRoutine());
            Debug.Log("<color=gray>[도적] 그림자 회피! 1초 무적 + 다음 공격 2배!</color>");
        }

        private IEnumerator PoisonDot()
        {
            float tickDmg = autoAttackDamage * 0.5f;
            for (int i = 0; i < 5; i++)
            {
                yield return new WaitForSeconds(1f);
                if (targetBoss != null) DealDamageTo(targetBoss, tickDmg);
            }
        }

        private IEnumerator DodgeRoutine()
        {
            SetInvulnerable(1.0f);
            yield return new WaitForSeconds(1.0f);
            _isNextAttackBoosted = true;
            if (targetBoss != null)
            {
                float counter = autoAttackDamage * 5f;
                DealDamageTo(targetBoss, counter);
                Debug.Log($"<color=purple>[도적] 카운터 암살! {counter:F0} 피해</color>");
            }
        }
    }
}
