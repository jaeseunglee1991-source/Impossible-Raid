using UnityEngine;
using System.Collections.Generic;
using BossRaid.Managers;
using BossRaid.Models;
using BossRaid.Combat;

namespace BossRaid.Testing
{
    public class CombatTestHelper : MonoBehaviour
    {
        [Header("Test Settings")]
        public bool simulateWin = true;
        public float testClearTime = 120.5f;

        [Header("Click to Trigger")]
        public bool triggerTest = false;
        public bool triggerBossTest = false;

        private void OnValidate()
        {
            if (triggerTest)
            {
                triggerTest = false;
                if (!Application.isPlaying)
                {
                    Debug.LogWarning("[Test] Please enter Play Mode (▶️) before triggering the test!");
                    return;
                }
                TestCombatResult();
            }

            if (triggerBossTest)
            {
                triggerBossTest = false;
                if (!Application.isPlaying)
                {
                    Debug.LogWarning("[Test] Please enter Play Mode (▶️) before triggering the Boss test!");
                    return;
                }
                TestImpossibleBoss();
            }
        }

        [ContextMenu("Simulate Combat End")]
        public async void TestCombatResult()
        {
            Debug.Log("[Test] Simulating Combat End...");
            
            // 1. 가상의 유저 데이터 생성
            List<CombatRecord> testStats = new List<CombatRecord>
            {
                new CombatRecord("user1", "WarriorUser", "Warrior") { totalDamage = 15000, totalDamageTaken = 500 },
                new CombatRecord("user2", "PaladinUser", "Paladin") { totalDamage = 3000, totalDamageTaken = 8000 },
                new CombatRecord("user3", "PriestUser", "Priest") { totalDamage = 1000, totalHealing = 12000 }
            };

            // 2. 결과 처리 프로세스 실행
            if (ResultManager.Instance != null)
            {
                await ResultManager.Instance.ProcessGameResult(simulateWin, testClearTime, testStats, 1);
                Debug.Log("[Test] ProcessGameResult called successfully.");
            }
            else
            {
                Debug.LogError("[Test] ResultManager instance not found!");
            }
        }

        [Header("Impossible Boss Simulation")]
        public BossRaid.Combat.Boss.BossAI testBoss;

        [ContextMenu("Simulate Impossible Boss (Start)")]
        public void TestImpossibleBoss()
        {
            if (!Application.isPlaying) 
            { 
                Debug.LogWarning("[Test] Play Mode (▶️) required for Simulation!"); 
                return; 
            }

            GameObject bossObj = new GameObject("TestBossArclight");
            BossRaid.Combat.Boss.BossAI boss = bossObj.AddComponent<BossRaid.Combat.Boss.BossAI>();
            boss.bossName = "Arclight";
            boss.maxHealth = 20000f;
            boss.autoAttackDamage = 300f;
            testBoss = boss;

            List<CharacterBase> players = new List<CharacterBase>();
            
            MockPlayer tank = new GameObject("Paladin").AddComponent<MockPlayer>();
            tank.Init("Paladin (Tank)", CharacterRole.Tank, 3000f);
            
            MockPlayer dps = new GameObject("FireMage").AddComponent<MockPlayer>();
            dps.Init("FireMage (DPS)", CharacterRole.RangedDPS, 1000f);

            MockPlayer healer = new GameObject("Priest").AddComponent<MockPlayer>();
            healer.Init("Priest (Healer)", CharacterRole.Healer, 1200f);

            players.Add(tank); players.Add(dps); players.Add(healer);
            
            Debug.Log("<color=green>[Combat] ====== 워크래프트3 스타일 어그로/전투 시뮬레이션 시작 ======\n</color>");
            boss.InitializeBattle(players);

            StartCoroutine(PlayerCombatLoop(boss, tank, dps, healer));
        }

        private System.Collections.IEnumerator PlayerCombatLoop(BossRaid.Combat.Boss.BossAI boss, MockPlayer tank, MockPlayer dps, MockPlayer healer)
        {
            int tick = 0;
            while (boss.currentHealth > 0)
            {
                if (tank.IsDead && dps.IsDead && healer.IsDead) yield break;

                if (!tank.IsDead)
                {
                    boss.TakeDamage(100f);
                    tank.AddThreat(800f); // 탱커는 데미지는 낮지만 위협 수준(어그로) 생성량이 매우 높음
                    Debug.Log($"[Tank] {tank.characterName} 가 '방패 밀치기' 시전! 보스에게 100 데미지. (어그로 +800)");
                }

                if (!dps.IsDead)
                {
                    boss.TakeDamage(500f);
                    dps.AddThreat(500f); // 딜러는 데미지 1당 어그로 1로 가정
                    Debug.Log($"[DPS] {dps.characterName} 가 '화염구' 시전! 보스에게 500 데미지. (어그로 +500)");
                }

                if (!healer.IsDead && !tank.IsDead)
                {
                    if (tank.currentHealth <= tank.maxHealth - 500f)
                    {
                        tank.Heal(800f);
                        healer.AddThreat(400f); // 힐러는 분산 어그로 (낮음)
                        Debug.Log($"<color=cyan>[Healer] {healer.characterName} 가 '치유의 빛'을 탱커에게 시전! (+800 HP). (어그로 +400)</color>");
                    }
                }

                if (tick == 3)
                {
                    Debug.Log($"<color=magenta>[Critical Event] {dps.characterName} 의 강력한 크리티컬 히트!! 엄청난 딜량 발생 (어그로 대폭 상승 +1500!)</color>");
                    boss.TakeDamage(1500f);
                    dps.AddThreat(1500f);
                }

                if (tick == 4 && !tank.IsDead)
                {
                    Debug.Log($"<color=yellow>[Tank Action] {tank.characterName} 가 긴급하게 '빛의 도발'을 시전합니다! 보스의 시선이 다시 탱커에게 돌아옵니다. (어그로 +3000)</color>");
                    tank.AddThreat(3000f);
                }

                // 위협 수준(어그로) 테이블 출력
                string threatTable = $"<color=white>[Threat Table] Tick {tick} | ";
                threatTable += $"{tank.characterName}: {tank.currentThreat} | ";
                threatTable += $"{dps.characterName}: {dps.currentThreat} | ";
                threatTable += $"{healer.characterName}: {healer.currentThreat}</color>";
                Debug.Log(threatTable);

                tick++;
                yield return new WaitForSeconds(1.5f);
            }
        }
    }

    public class MockPlayer : CharacterBase
    {
        public void Init(string name, CharacterRole charRole, float hp)
        {
            characterName = name;
            role = charRole;
            maxHealth = hp;
            currentHealth = hp;
            currentThreat = 0f;
        }

        protected override void Start() { } // Prevent base start from resetting HP
        public override void UseSkill(int idx) {}
        public override void UseUltimate() {}
    }
}
