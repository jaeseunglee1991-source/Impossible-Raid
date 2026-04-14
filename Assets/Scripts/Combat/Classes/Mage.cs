using UnityEngine;
using System.Collections;

namespace BossRaid.Combat.Classes
{
    public class Mage : CharacterBase
    {
        protected override void Awake()
        {
            base.Awake();
            role = CharacterRole.RangedDPS;
            characterName = "마법사";
            
            // 기초 스탯
            maxHealth = 800f; 
            currentHealth = maxHealth;
            autoAttackDamage = 60f; 
            attackSpeed = 1.5f; // 가장 느림. 하지만 한방이 쎔
            attackRange = 6.0f; // 최대 사거리
            
            skillNames = new string[] { "메테오 낙하", "", "" };
            skillCooldowns = new float[] { 6f, 0f, 0f }; 
            
            ultimateName = "타임 프리즈 (시간 정지)";
            ultimateCooldown = 25f; 
        }

        public override void UseSkill(int idx)
        {
            if (idx == 0) // 일반기: 메테오 낙하 (맵 전체 광역 청소용)
            {
                float damage = autoAttackDamage * 3.5f; 

                Debug.Log($"<color=red>[마법사] 메테오 낙하! 화면 전체에 {damage} 화염 피해!</color>");
                
                // 보스에게 적용
                if (targetBoss != null) 
                { 
                    targetBoss.TakeDamage(damage); 
                }
                
                // TODO: StageManager 화면 안의 모든 몹에게 광역 피해를 주는 로직 연동
            }
        }

        public override void UseUltimate()
        {
            Debug.Log($"<color=cyan>[마법사] 시간 정지! 3초간 보스의 모든 행동을 멈춥니다!</color>");
            
            // 레이드 기믹: 타임 프리즈 
            if (targetBoss != null)
            {
                // 보스의 코루틴 및 애니메이션을 일시 정지 (예시 로직)
                StartCoroutine(TimeFreezeRoutine());
            }
        }

        private IEnumerator TimeFreezeRoutine()
        {
            if (targetBoss != null)
            {
                // 약간의 꼼수: 보스를 스턴 상태로 만듭니다
                targetBoss.isStaggered = true; 
                // 메테오 후기 데미지
                targetBoss.TakeDamage(autoAttackDamage * 8f);
                
                yield return new WaitForSeconds(3.0f);
                
                if (targetBoss != null) targetBoss.isStaggered = false;
            }
        }
    }
}
