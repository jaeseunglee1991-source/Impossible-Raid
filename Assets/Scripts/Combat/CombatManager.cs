using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using BossRaid.Managers;
using BossRaid.Models;
using BossRaid.UI;
using BossRaid.Combat.Boss;
using BossRaid.Combat.Classes;
using BossRaid.Combat.Player;
using BossRaid.Combat.Camera;

namespace BossRaid.Combat
{
    /// <summary>
    /// 전투 관리자 - 전투 흐름 제어 및 HUD 연동
    /// 인게임 씬 로드 시 자동으로 전투를 시작
    /// </summary>
    public class CombatManager : MonoBehaviour
    {
        public static CombatManager Instance { get; private set; }
        
        [Header("Battle Settings")]
        public float gameDuration = 180f; // 3분
        public int maxLives = 10;

        [Header("Current State")]
        public float remainingTime;
        public int currentLives;
        public bool isGameActive = false;
        public int currentPhase = 1;
        public int maxPhase = 8;
        public bool IsPartyWiping { get; private set; } = false;

        [Header("References")]
        public BossAI currentBoss;
        public List<CharacterBase> activePlayers = new List<CharacterBase>();
        public CharacterBase localPlayer;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this.gameObject); return; }
            Instance = this;

            // 안드로이드 가로 모드 고정 (모바일 환경이므로 커서 숨김/잠금 불필요)
            Screen.orientation = ScreenOrientation.LandscapeLeft;
        }

        private void Start()
        {
            // 인게임 씬 로드 시 자동 전투 시작
            StartCoroutine(AutoStartBattle());
        }

        /// <summary>씬 로드 후 1프레임 대기 뒤 자동 전투 시작</summary>
        private IEnumerator AutoStartBattle()
        {
            yield return null; // HUD 등 다른 컴포넌트 초기화 대기

            // WaitingRoomManager에서 참가자 정보 가져오기
            List<RoomMember> participants = null;
            if (WaitingRoomManager.Instance != null && WaitingRoomManager.Instance.participants != null
                && WaitingRoomManager.Instance.participants.Count > 0)
            {
                participants = WaitingRoomManager.Instance.participants;
                Debug.Log($"[Combat] 대기방에서 {participants.Count}명의 참가자 정보를 가져왔습니다.");
            }
            else
            {
                // 테스트용: 대기방 정보 없을 시 기본 참가자 생성
                Debug.LogWarning("[Combat] WaitingRoomManager 데이터가 없습니다. 테스트 모드로 시작합니다.");
                var testMember = new RoomMember("test_1", "테스트전사", true);
                testMember.job = "Warrior";
                participants = new List<RoomMember> { testMember };
            }

            StartBattle(participants);
        }

        public void StartBattle(List<RoomMember> participants)
        {
            isGameActive = true;
            remainingTime = gameDuration;
            currentLives = maxLives;
            currentPhase = 1;
            
            SpawnBoss();
            SpawnPlayers(participants);

            // 보스에게 플레이어 리스트 전달
            if (currentBoss != null)
            {
                currentBoss.InitializeBattle(activePlayers);
            }
            
            // HUD 초기화
            if (InGameHUDController.Instance != null && currentBoss != null && localPlayer != null)
            {
                InGameHUDController.Instance.InitializeHUD(activePlayers, currentBoss, localPlayer);
                InGameHUDController.Instance.UpdateLife(currentLives, maxLives);
                Debug.Log("[Combat] HUD 초기화 완료.");
            }

            // 카메라 추적 설정
            SetupCamera();
            
            StartCoroutine(UpdateTimer());
            Debug.Log("[Combat] ⚔️ 전투 시작!");
        }

        private void SpawnBoss()
        {
            // ── 2D: 보스는 XY 평면 중앙(Z=0) 배치 ──
            GameObject bossGO = new GameObject("Boss_Belthazar");
            bossGO.transform.position = new Vector3(0f, 0f, 0f);

            // [2D] 보스 스프라이트 로드 시도
            Sprite bossSprite = Resources.Load<Sprite>("Boss/Sprites/boss_belthazar");
            if (bossSprite != null)
            {
                GameObject visual = new GameObject("BossVisual");
                visual.transform.SetParent(bossGO.transform, false);
                var sr = visual.AddComponent<SpriteRenderer>();
                sr.sprite = bossSprite;
                sr.sortingLayerName = "Characters";
                sr.sortingOrder = 1;

                // 애니메이터 컨트롤러 연결 (2D용)
                RuntimeAnimatorController controller =
                    Resources.Load<RuntimeAnimatorController>("Boss/Animators/BossAnimatorController");
                if (controller != null)
                {
                    var anim = visual.AddComponent<Animator>();
                    anim.runtimeAnimatorController = controller;
                    Debug.Log("<color=cyan>[Combat] 2D 보스 애니메이터 연결 성공!</color>");
                }

                // 2D 충돌체
                var col = bossGO.AddComponent<CircleCollider2D>();
                col.radius = 1.2f;
                col.offset = Vector2.zero;
            }
            else
            {
                // 폴백: 색상 원형 스프라이트
                Debug.LogWarning("[Combat] 보스 스프라이트 없음 — Fallback 사용");
                GameObject visual = new GameObject("BossVisual");
                visual.transform.SetParent(bossGO.transform, false);
                var sr = visual.AddComponent<SpriteRenderer>();
                sr.sprite = Resources.Load<Sprite>("Sprites/circle") ?? CreateFallbackSprite(new Color(0.8f, 0.2f, 0f));
                sr.transform.localScale = Vector3.one * 2.5f;
                sr.sortingLayerName = "Characters";

                var col = bossGO.AddComponent<CircleCollider2D>();
                col.radius = 1.2f;
            }

            // BossAI 컴포넌트 추가
            var bossAI = bossGO.AddComponent<BossAI>();
            bossAI.bossName = "Belthazar, Lord of Flame";
            bossAI.maxHealth = 50000f;
            bossAI.currentHealth = 50000f;
            bossAI.autoAttackDamage = 180f;

            currentBoss = bossAI;
            Debug.Log("[Combat] 🔥 Boss Belthazar 2D 스폰 완료!");
        }

        /// <summary>스프라이트 에셋 없을 때 단색 원형 Fallback 생성</summary>
        private Sprite CreateFallbackSprite(Color color)
        {
            var tex = new Texture2D(64, 64);
            var pixels = new Color[64 * 64];
            Vector2 center = new Vector2(32, 32);
            for (int i = 0; i < pixels.Length; i++)
            {
                int x = i % 64; int y = i / 64;
                pixels[i] = Vector2.Distance(new Vector2(x, y), center) < 30f ? color : Color.clear;
            }
            tex.SetPixels(pixels); tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 64);
        }

        private void SpawnPlayers(List<RoomMember> participants)
        {
            activePlayers.Clear();

            // ── 2D: 보스 아래쪽(Y 음수) 반원형 배치 ──
            float spawnRadius = 4f;
            float angleStep   = participants.Count > 1 ? 120f / (participants.Count - 1) : 0f;
            float startAngle  = -60f; // -60 ~ +60도 호 범위

            for (int i = 0; i < participants.Count; i++)
            {
                var member = participants[i];

                // 2D 스폰 위치 (XY 평면, Z=0)
                float angle = startAngle + angleStep * i;
                float rad   = angle * Mathf.Deg2Rad;
                Vector3 spawnPos = new Vector3(
                    Mathf.Sin(rad) * spawnRadius,    // X
                    -Mathf.Cos(rad) * spawnRadius,   // Y (보스 아래쪽)
                    0f                               // 2D: Z 고정
                );

                // 플레이어 GameObject 생성
                GameObject playerGO = new GameObject($"Player_{member.nickname}");
                playerGO.transform.position = spawnPos;

                // 직업별 클래스 컴포넌트 추가
                CharacterBase charBase = AddJobComponent(playerGO, member.job);
                charBase.characterName = member.nickname;

                // [2D] 직업별 스프라이트 로드
                string spritePath = $"Characters/Sprites/{member.job}_idle";
                Sprite charSprite = Resources.Load<Sprite>(spritePath);

                GameObject visual = new GameObject("Visual");
                visual.transform.SetParent(playerGO.transform, false);
                var sr = visual.AddComponent<SpriteRenderer>();
                sr.sprite = charSprite ?? CreateFallbackSprite(Color.cyan);
                sr.sortingLayerName = "Characters";
                sr.sortingOrder = 0;

                // 2D 애니메이터 (있을 경우)
                RuntimeAnimatorController controller =
                    Resources.Load<RuntimeAnimatorController>($"Characters/Animators/{member.job}Controller");
                if (controller != null)
                {
                    var anim = visual.AddComponent<Animator>();
                    anim.runtimeAnimatorController = controller;
                }

                // 2D 캡슐 충돌체
                var col = playerGO.AddComponent<CapsuleCollider2D>();
                col.size   = new Vector2(0.6f, 1.0f);
                col.offset = new Vector2(0f, 0f);

                // 첫 번째 플레이어(호스트) = 로컬 플레이어
                if (i == 0)
                {
                    var pc = playerGO.AddComponent<PlayerController>();
                    pc.characterInfo = charBase;
                    localPlayer = charBase;
                }

                activePlayers.Add(charBase);
                Debug.Log($"[Combat] 🛡️ {member.nickname} ({member.job}) 2D 스프라이트 스폰 완료!");
            }
        }

        /// <summary>직업 문자열에 맞는 CharacterBase 파생 클래스 추가</summary>
        private CharacterBase AddJobComponent(GameObject go, string job)
        {
            switch (job)
            {
                case "Warrior":     return go.AddComponent<Warrior>();
                case "Paladin":     return go.AddComponent<Paladin>();
                case "Rogue":       return go.AddComponent<Rogue>();
                case "DeathKnight": return go.AddComponent<DeathKnight>();
                case "Ranger":      return go.AddComponent<Ranger>();
                case "FireMage":    return go.AddComponent<FireMage>();
                case "IceMage":     return go.AddComponent<IceMage>();
                case "Warlock":     return go.AddComponent<Warlock>();
                case "Priest":      return go.AddComponent<Priest>();
                case "Druid":       return go.AddComponent<Druid>();
                default:
                    Debug.LogWarning($"[Combat] 알 수 없는 직업: {job}, Warrior로 대체합니다.");
                    return go.AddComponent<Warrior>();
            }
        }

        private void SetupCamera()
        {
            // ── 2D 직교 카메라 설정 ──
            var mainCam = UnityEngine.Camera.main;
            if (mainCam != null)
            {
                // 카메라 직교 모드 강제 설정
                mainCam.orthographic = true;
                mainCam.orthographicSize = 6f;

                if (localPlayer != null)
                {
                    var follow = mainCam.GetComponent<CameraFollow>();
                    if (follow == null) follow = mainCam.gameObject.AddComponent<CameraFollow>();
                    follow.target = localPlayer.transform;
                    follow.offset = new Vector3(0f, 0f, -10f); // 2D: Z만 음수 고정
                    follow.smoothSpeed = 8f;
                    follow.orthographicSize = 6f;
                }
                Debug.Log("[Combat] 📷 2D 직교 카메라 추적 설정 완료.");
            }
        }

        private IEnumerator UpdateTimer()
        {
            bool hasEnraged = false;
            while (remainingTime > 0 && isGameActive)
            {
                remainingTime -= Time.deltaTime;
                yield return null;
            }

            // A. 광폭화: 시간 종료 시 패배가 아닌 보스 Enrage 상태 진입
            if (isGameActive && !hasEnraged) 
            {
                hasEnraged = true;
                if (currentBoss != null)
                {
                    currentBoss.TriggerEnrage();
                }
            }
        }

        public void AddTimeBonus(float seconds)
        {
            if (!isGameActive || currentBoss == null || currentBoss.isEnraged) return;
            remainingTime += seconds;
            Debug.Log($"<color=green>[Combat] ⏳ 타임 보너스 +{seconds}초 추가! 남은 시간: {remainingTime:F1}초</color>");
        }

        /// <summary>파티 전멸 시 라이프 차감</summary>
        public void OnPartyWipe()
        {
            if (IsPartyWiping) return;
            IsPartyWiping = true;

            currentLives--;
            if (InGameHUDController.Instance != null)
                InGameHUDController.Instance.UpdateLife(currentLives, maxLives);

            if (currentLives <= 0)
            {
                EndBattle(false);
            }
            else
            {
                Debug.Log($"[Combat] 💀 파티 전멸! 남은 라이프: {currentLives}/{maxLives}");
                StartCoroutine(ReviveRoutine());
            }
        }

        private IEnumerator ReviveRoutine()
        {
            // 전멸 부활 대기시간 (3초)
            yield return new WaitForSeconds(3.0f);
            ReviveAllPlayers();
            IsPartyWiping = false;
        }

        public void ReviveAllPlayers()
        {
            foreach (var player in activePlayers)
            {
                if (player.IsDead)
                {
                    player.Revive(0.5f); // 50% 체력 부활
                    Debug.Log($"[Combat] {player.characterName} 부활! (HP: {player.currentHealth})");
                }
            }
        }

        /// <summary>보스 페이즈 전환</summary>
        public void AdvancePhase()
        {
            currentPhase++;
            if (InGameHUDController.Instance != null && InGameHUDController.Instance.bossFrame != null)
                InGameHUDController.Instance.bossFrame.UpdatePhase(currentPhase, maxPhase);
            
            Debug.Log($"[Combat] Phase advanced to {currentPhase}/{maxPhase}");
        }

        /// <summary>보스 캐스팅 발동 시 HUD 통보</summary>
        public void NotifyBossCasting(string patternName, float duration)
        {
            if (InGameHUDController.Instance != null)
                InGameHUDController.Instance.NotifyBossCasting(patternName, duration);
        }

        /// <summary>캐스팅 차단 성공 시 HUD 통보</summary>
        public void NotifyInterrupt()
        {
            if (InGameHUDController.Instance != null)
                InGameHUDController.Instance.NotifyBossInterrupt();
        }

        public void EndBattle(bool isWin)
        {
            isGameActive = false;
            Debug.Log($"[Combat] 전투 종료. 승리: {isWin}");
            
            if (!isWin)
            {
                Debug.Log("<color=red>[System] 💀 게임 오버! 우울한 BGM 및 화면 암전 파티클 효과 재생!</color>");
                // 실제 코드 예: Instantiate(Resources.Load("Effects/DefeatScreen"), Vector3.zero, Quaternion.identity);
            }

            // ResultManager를 통한 결과 처리 호출
            if (ResultManager.Instance != null)
            {
                float clearTime = gameDuration - remainingTime;
                var stats = new List<CombatRecord>();
                foreach (var p in activePlayers)
                {
                    // 직업명은 클래스명에서 가져옴 (Warrior, Ranger 등)
                    string jobName = p.GetType().Name;
                    var record = new CombatRecord(p.characterName, p.characterName, jobName)
                    {
                        totalDamage = p.totalDamageDealt,
                        totalHealing = p.totalHealingDone,
                        totalDamageTaken = p.totalDamageTaken,
                        aggroDuration = p.aggroDuration
                    };
                    stats.Add(record);
                }
                
                // 결과 처리 및 씬 전환 주도
                _ = ResultManager.Instance.ProcessGameResult(isWin, clearTime, stats, isWin ? currentPhase : 0);
            }
        }
    }
}
