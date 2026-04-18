using UnityEngine;
using System.Collections;
using BossRaid.Combat.Boss;

namespace BossRaid.Combat.Classes
{
    public class FireMage : CharacterBase
    {
        private int burnStacks = 0;

        protected override void Awake()
        {
            base.Awake();
            role          = CharacterRole.RangedDPS;
            characterName = "화염 마법사";
            maxHealth     = 1000f;
            autoAttackDamage = 35f;
            attackSpeed   = 1.2f;
            attackRange   = 10f;

            // ── 스킬 슬롯 등록 ──
            RegisterSkills(
                new SkillDefinition("화염 작렬", 12f, 0, interrupt: true,
                    desc: "보스 캐스팅을 차단하고 320 폭발 피해를 줍니다."),
                new SkillDefinition("화염구",     4f, 1,
                    desc: "150 피해 + 화상 스택 1 누적"),
                new SkillDefinition("불기둥",    10f, 2,
                    desc: "1초 후 지점에 400 폭발 장판")
            );
            RegisterUltimate(new SkillDefinition("발화", 60f, 0, ultimate: true,
                desc: "누적된 화상 스택을 전부 폭발시킵니다. (스택당 +150 피해)"));
        }

        public override void UseSkill(int idx)
        {
            switch (idx)
            {
                case 0: // 화염 작렬 (차단)
                    if (targetBoss != null)
                    {
                        targetBoss.TakeDamage(320f * attackPowerMultiplier);
                        targetBoss.Interrupt();
                        AddThreat(80f);
                    }
                    Debug.Log("<color=orange>[화염 마법사] 화염 작렬! 차단!</color>");
                    break;

                case 1: // 화염구
                    if (targetBoss != null)
                    {
                        targetBoss.TakeDamage(150f * attackPowerMultiplier);
                        burnStacks++;
                        AddThreat(40f);
                    }
                    Debug.Log($"[화염 마법사] 화염구! 화상 스택: {burnStacks}");
                    break;

                case 2: // 불기둥
                    StartCoroutine(FlameStrike());
                    Debug.Log("[화염 마법사] 불기둥! 1초 후 폭발!");
                    break;
            }
        }

        public override void UseUltimate()
        {
            float damage = 100f + (burnStacks * 150f);
            if (targetBoss != null) { targetBoss.TakeDamage(damage); AddThreat(damage * 0.5f); }
            Debug.Log($"<color=yellow>[화염 마법사] 발화! {burnStacks}스택 폭발! {damage} 피해!</color>");
            burnStacks = 0;
        }

        private IEnumerator FlameStrike()
        {
            yield return new WaitForSeconds(1f);
            if (targetBoss != null) { targetBoss.TakeDamage(400f * attackPowerMultiplier); AddThreat(100f); }
        }
    }
}

