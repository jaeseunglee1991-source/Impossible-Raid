using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using BossRaid.Managers;
using BossRaid.Models;

namespace BossRaid.Combat
{
    public class CombatManager : MonoBehaviour
    {
        public static CombatManager Instance { get; private set; }
        
        [Header("Battle Settings")]
        public float gameDuration = 180f; // 3분
        public Transform bossSpawnPoint;
        public Transform[] playerSpawnPoints;

        [Header("Current State")]
        public float remainingTime;
        public bool isGameActive = false;
        
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this.gameObject); return; }
            Instance = this;
        }

        public void StartBattle(List<RoomMember> participants)
        {
            isGameActive = true;
            remainingTime = gameDuration;
            
            SpawnBoss();
            SpawnPlayers(participants);
            
            StartCoroutine(UpdateTimer());
        }

        private void SpawnBoss()
        {
            // Arclight 보스 생성 로직
            Debug.Log("[Combat] Boss Arclight has spawned.");
        }

        private void SpawnPlayers(List<RoomMember> participants)
        {
            for (int i = 0; i < participants.Count; i++)
            {
                // participants[i].job 에 맞는 프리팹 생성
                Debug.Log($"[Combat] Player {participants[i].nickname} spawned as {participants[i].job}.");
            }
        }

        private IEnumerator UpdateTimer()
        {
            while (remainingTime > 0 && isGameActive)
            {
                remainingTime -= Time.deltaTime;
                yield return null;
            }

            if (isGameActive) EndBattle(false); // 시간 초과 시 패배
        }

        public void EndBattle(bool isWin)
        {
            isGameActive = false;
            Debug.Log($"[Combat] Battle Ended. Win: {isWin}");
            
            // ResultManager를 통한 결과 처리 호출 예정
        }
    }
}
