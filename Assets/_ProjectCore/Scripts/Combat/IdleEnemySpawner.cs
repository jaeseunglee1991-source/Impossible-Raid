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
            _currentEnemy.name = $"Monster_{StageManager.Instance?.MobsKilledInStage ?? 0}";

            // 아군 프리팹 재활용 시 불필요 스크립트 제거
            var tagController = _currentEnemy.GetComponent<TagCharacterController>();
            if (tagController != null) Destroy(tagController);

            // 몬스터 AI 부착
            if (_currentEnemy.GetComponent<IdleBoss>() == null)
            {
                var boss = _currentEnemy.AddComponent<IdleBoss>();
                boss.maxHP = 2000f;
                boss.bossLevel = StageManager.Instance?.CurrentStageLevel ?? 1;
                boss.isSpawnedMinion = true; // 스포너가 생성한 잡몹임을 명시
            }
            else
            {
                var boss = _currentEnemy.GetComponent<IdleBoss>();
                boss.isSpawnedMinion = true;
            }

            // 애니메이션 브릿지
            if (_currentEnemy.GetComponent<SPUMAnimationBridge>() == null)
                _currentEnemy.AddComponent<SPUMAnimationBridge>();

            Debug.Log($"[Spawner] 몬스터 리스폰: {_currentEnemy.name}");
        }
    }
}
