using UnityEngine;
using System.Collections;
using BossRaid.Combat.Boss;

namespace BossRaid.Combat.Classes
{
    public class Paladin : CharacterBase
    {
        protected override void Awake()
        {
            base.Awake();
            role = CharacterRole.Tank;
            maxHealth = 2500f; currentHealth = maxHealth;
            characterName = "팔라딘";
            attackRange = 3f; autoAttackDamage = 40f; attackSpeed = 1.5f;
            skillNames = new string[] { "정의의 방패", "성스러운 빛", "헌신적 오라" };
            skillCooldowns = new float[] { 10f, 8f, 20f };
            ultimateName = "천상의 보호막"; ultimateCooldown = 60f;
        }

        public override void UseSkill(int idx)
        {
            switch (idx)
            {
                case 0: // 정의의 방패 (차단) - 피해 + 1초 기절 + 어그로
                    if (targetBoss != null) { targetBoss.TakeDamage(200f); targetBoss.Interrupt(); AddThreat(500f); }
                    Debug.Log("<color=blue>[팔라딘] 정의의 방패! 차단 + 어그로 대폭 획득</color>");
                    break;
                case 1: // 성스러운 빛 - 자신+주변 1명 힐
                    Heal(400f);
                    var lowestAlly = FindLowestHPAlly();
                    if (lowestAlly != null && lowestAlly != this) lowestAlly.Heal(400f);
                    Debug.Log("<color=green>[팔라딘] 성스러운 빛! 자신+아군 회복</color>");
                    break;
                case 2: // 헌신적 오라 - 6초간 파티 피해 30% 감소
                    StartCoroutine(DevotionAura());
                    Debug.Log("<color=cyan>[팔라딘] 헌신적 오라! 6초간 파티 피해 30% 감소</color>");
                    break;
            }
        }

        public override void UseUltimate()
        {
            StartCoroutine(DivineShield());
            Debug.Log("<color=yellow>[팔라딘] 천상의 보호막! 5초간 무적!</color>");
        }

        private IEnumerator DevotionAura()
        {
            ApplyPartyBuff(p => p.damageReductionMultiplier = 0.7f);
            yield return new WaitForSeconds(6f);
            ApplyPartyBuff(p => p.damageReductionMultiplier = 1f);
        }

        private IEnumerator DivineShield()
        {
            isInvulnerable = true;
            yield return new WaitForSeconds(5f);
            isInvulnerable = false;
        }
    }
}
