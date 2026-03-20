using UnityEngine;
using BossRaid.Combat;
using System.Collections.Generic;

namespace BossRaid.Combat.Classes
{
    public class Paladin : CharacterBase
    {
        [Header("Paladin Skills")]
        public float shieldBashCooldown = 10f;
        public float holyLightCooldown = 8f;
        public float devotionAuraCooldown = 20f;
        public float bubbleCooldown = 60f;

        protected override void Start()
        {
            base.Start();
            role = CharacterRole.Tank;
            maxHealth = 2500f; // 초기 기획 스펙
            currentHealth = maxHealth;
            characterName = "Paladin";
        }

        private void Update()
        {
            // 스킬 쿨다운 티킹 소망 (나중에 추상화 가능)
        }

        public override void UseSkill(int skillIndex)
        {
            switch (skillIndex)
            {
                case 0: ShieldBash(); break;
                case 1: HolyLight(); break;
                case 2: DevotionAura(); break;
            }
        }

        public override void UseUltimate()
        {
            DivineShield();
        }

        private void ShieldBash()
        {
            Debug.Log("[Paladin] Shield Bash! Interrupting & Gaining Aggro.");
            // 타겟에게 데미지 + 어그로 + 차단 처리
        }

        private void HolyLight()
        {
            Debug.Log("[Paladin] Holy Light! Healing self or ally.");
            // 체력 즉시 회복
        }

        private void DevotionAura()
        {
            Debug.Log("[Paladin] Devotion Aura! Reducing party damage by 30%.");
            // 파티 버프 적용 (6초)
        }

        private void DivineShield()
        {
            Debug.Log("[Paladin] Divine Shield! Invulnerable for 5 seconds.");
            // 무적 상태 부여
        }
    }
}
