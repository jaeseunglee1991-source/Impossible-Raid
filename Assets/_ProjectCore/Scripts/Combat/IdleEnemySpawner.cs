using UnityEngine;
using System.Collections;
using BossRaid.Combat.Boss;
using BossRaid.Managers;

namespace BossRaid.Combat
{
    /// <summary>
    /// 방치형 파밍 모드에서 몬스터를 1마리씩 생성하는 스포너.
    /// 몬스터 사망 → 1초 딜레이 → 다음 몬스터 리스폰.
    /// </summary>
    public class IdleEnemySpawner : MonoBehaviour
    {
        public GameObject[] enemyPrefabs;
        public Transform[] spawnPoints;

        private GameObject _currentEnemy;
        private bool _waitingToSpawn = false;

        private void Start()
        {
            SpawnEnemy();
        }

        private void Update()
        {
            if (StageManager.Instance != null && StageManager.Instance.CurrentState != GameState.IdleFarming)
                return;

            // 현재 몬스터가 죽었거나 사라졌으면 리스폰 대기 시작
            if (_currentEnemy == null && !_waitingToSpawn)
            {
                _waitingToSpawn = true;
                StartCoroutine(RespawnAfterDelay(1f));
            }
        }

        private IEnumerator RespawnAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            SpawnEnemy();
            _waitingToSpawn = false;
        }

        private void SpawnEnemy()
        {
            if (enemyPrefabs == null || enemyPrefabs.Length == 0) return;
            if (spawnPoints == null || spawnPoints.Length == 0) return;

            GameObject prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
            Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];

            _currentEnemy = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);
            _currentEnemy.name = $"BossMob_{StageManager.Instance?.MobsKilledInStage ?? 0}";
            _currentEnemy.transform.localScale = Vector3.one * 1.5f; // 보스처럼 거대하게 연출

            // 아군 프리팹 재활용 시 불필요 스크립트 제거
            var tagController = _currentEnemy.GetComponent<TagCharacterController>();
            if (tagController != null) Destroy(tagController);

            // 몬스터 AI 부착 및 능력치 설정 (스테이지 레벨 비례)
            int stageLvl = StageManager.Instance != null ? StageManager.Instance.CurrentStageLevel : 1;
            
            if (_currentEnemy.GetComponent<IdleBoss>() == null)
            {
                var boss = _currentEnemy.AddComponent<IdleBoss>();
                // ── [보스급 잡몹] 초고체력 및 강력한 데미지 공식 ──
                boss.maxHP = 500f + (stageLvl * 500f);      // 1스테이지 1000, 10스테이지 5500
                boss.autoAttackDamage = 10f + (stageLvl * 5f); // 1스테이지 15, 10스테이지 60
                boss.autoAttackInterval = 1.2f;            // 공격도 더 매섭게 (1.2초)
                boss.bossLevel = stageLvl;
                boss.isSpawnedMinion = true;
            }
            else
            {
                var boss = _currentEnemy.GetComponent<IdleBoss>();
                boss.maxHP = 500f + (stageLvl * 500f);
                boss.autoAttackDamage = 10f + (stageLvl * 5f);
                boss.autoAttackInterval = 1.2f;
                boss.isSpawnedMinion = true;
            }

            // 애니메이션 브릿지
            if (_currentEnemy.GetComponent<SPUMAnimationBridge>() == null)
                _currentEnemy.AddComponent<SPUMAnimationBridge>();

            Debug.Log($"[Spawner] 몬스터 리스폰: {_currentEnemy.name}");
        }
    }
}
