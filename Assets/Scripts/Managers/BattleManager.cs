using UnityEngine;
using System.Collections.Generic;

public enum GameState { IdleFarming, BossChallenge } // StageManager에 있다면 삭제 가능

public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance { get; private set; }

    [Header("상태 관리 (테스트용)")]
    public GameState currentState = GameState.IdleFarming; // 실제로는 StageManager.Instance.CurrentState 사용 권장

    [Header("캐릭터 프리팹")]
    public GameObject warriorPrefab;
    public GameObject roguePrefab;
    public GameObject magePrefab;
    public GameObject healerPrefab;

    [Header("방치형 모드 4인 스폰 위치")]
    public Transform[] idleSpawnPoints; // Inspector에서 4개의 위치 할당 필요

    [Header("레이드 모드 스폰 위치")]
    public Transform raidSpawnPoint;

    [Header("보스 프리팹")]
    public GameObject idleBossPrefab;
    public GameObject raidBossPrefab;
    public Transform bossSpawnPoint;

    private List<GameObject> activeCharacters = new List<GameObject>();
    private GameObject currentBoss;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        InitializeBattle();
    }

    public void InitializeBattle()
    {
        ClearBattlefield();

        if (currentState == GameState.IdleFarming)
        {
            SetupIdleFarmingMode();
        }
        else if (currentState == GameState.BossChallenge)
        {
            SetupBossRaidMode();
        }
    }

    private void SetupIdleFarmingMode()
    {
        Debug.Log("방치형 전투 모드 셋업 시작 (4인 동시 타격)");

        // 1. 태그 UI 숨기기 (UIManager.Instance.ToggleTagUI(false) 등 호출)

        // 2. 4명의 캐릭터 스폰 및 리스트 등록
        SpawnCharacter(warriorPrefab, idleSpawnPoints[0].position, true);
        SpawnCharacter(roguePrefab, idleSpawnPoints[1].position, true);
        SpawnCharacter(magePrefab, idleSpawnPoints[2].position, true);
        SpawnCharacter(healerPrefab, idleSpawnPoints[3].position, true);

        // 3. 방치형 보스 스폰 (1% 재화 루프 적용)
        currentBoss = Instantiate(idleBossPrefab, bossSpawnPoint.position, bossSpawnPoint.rotation);
    }

    private void SetupBossRaidMode()
    {
        Debug.Log("보스 레이드 모드 셋업 시작 (태그 시스템)");

        // 1. 태그 UI 보이기

        // 2. 레이드용 스폰 (기존 로직: 메인 캐릭터 1명 스폰 후 태그 대기)
        SpawnCharacter(warriorPrefab, raidSpawnPoint.position, false);

        // 3. 레이드 보스 스폰 (패턴이 있는 보스)
        currentBoss = Instantiate(raidBossPrefab, bossSpawnPoint.position, bossSpawnPoint.rotation);
    }

    private void SpawnCharacter(GameObject prefab, Vector3 position, bool isIdleMode)
    {
        if (prefab == null) return;

        GameObject charObj = Instantiate(prefab, position, Quaternion.identity);
        activeCharacters.Add(charObj);

        TagCharacterController controller = charObj.GetComponent<TagCharacterController>();
        if (controller != null)
        {
            // 방치형 모드라면 AI를 켜고, 레이드 모드라면 수동 조작으로 설정
            controller.EnableIdleAIMode(isIdleMode);
        }
    }

    private void ClearBattlefield()
    {
        foreach (var chr in activeCharacters)
        {
            if (chr != null) Destroy(chr);
        }
        activeCharacters.Clear();

        if (currentBoss != null) Destroy(currentBoss);
    }
}
