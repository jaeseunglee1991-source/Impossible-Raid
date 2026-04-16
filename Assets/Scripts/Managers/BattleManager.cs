using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BossRaid.Combat;
using BossRaid.UI;

namespace BossRaid.Managers
{
    /// <summary>
    /// ──────────────────────────────────────────────────────────────────
    /// BattleManager  —  파티 태그(Tag) 시스템 + 게임오버 판정 + 부활 중계
    /// ──────────────────────────────────────────────────────────────────
    /// ■ 구조
    ///   파티1 (멤버 2명) / 파티2 (멤버 2명)
    ///   한 파티씩 '활성화(태그-인)' 되고 나머지는 SetActive(false) 상태
    ///   단, CharacterStatus 데이터(HP/MP/쿨타임)는 메모리에 유지됨
    ///
    /// ■ 게임오버 조건
    ///   현재 전투 중인 활성 파티 2명이 모두 사망 → 즉시 보스전 실패
    ///
    /// ■ HUD 연동
    ///   우측 하단: 파티 전환 버튼 → SwitchParty()
    ///            현재 파티 내 캐릭터 교체 버튼 → SwitchActiveCharacter(index)
    /// ──────────────────────────────────────────────────────────────────
    /// </summary>
    public class BattleManager : MonoBehaviour
    {
        public static BattleManager Instance { get; private set; }

        // ──────────────────────────────────────────────
        // 파티 구성 (Inspector 에서 할당)
        // ──────────────────────────────────────────────
        [Header("Party 1 (직업 2명)")]
        public List<TagCharacterController> Party1 = new List<TagCharacterController>();

        [Header("Party 2 (직업 2명)")]
        public List<TagCharacterController> Party2 = new List<TagCharacterController>();

        // ──────────────────────────────────────────────
        // 상태
        // ──────────────────────────────────────────────
        [Header("Current State (Read-Only)")]
        [SerializeField] private int _activePartyIndex = 1;  // 1 or 2
        [SerializeField] private int _activeCharacterIndex = 0;

        public int ActivePartyIndex  => _activePartyIndex;
        public bool IsGameOver       { get; private set; } = false;

        /// <summary>현재 플레이어가 직접 조작 중인 캐릭터</summary>
        public TagCharacterController ActivePlayerCharacter { get; private set; }

        // ReviveService에서 사용하는 공개 파티 접근자
        public List<TagCharacterController> ActiveParty  => _activePartyIndex == 1 ? Party1 : Party2;
        public List<TagCharacterController> InactiveParty => _activePartyIndex == 1 ? Party2 : Party1;

        // ──────────────────────────────────────────────
        // Unity 생명주기
        // ──────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            // 1파티 선 활성화, 2파티 대기
            ActivateParty(1, forceFirstCharacter: true);
        }

        // ──────────────────────────────────────────────
        // 파티 전환 (HUD 파티 교체 버튼 → 호출)
        // ──────────────────────────────────────────────
        /// <summary>
        /// 철권 태그 방식 파티 교체.
        /// 대기 파티의 Status(HP/MP/쿨타임) 는 교체 전 그대로 보존됨.
        /// </summary>
        public void SwitchParty()
        {
            if (IsGameOver) return;

            int target = (_activePartyIndex == 1) ? 2 : 1;

            // 교체 대상 파티가 전멸 상태라도 교체는 허용 — 교체 후 게임오버 체크
            ActivateParty(target, forceFirstCharacter: true);
        }

        // ──────────────────────────────────────────────
        // 현재 파티 내 조작 캐릭터 전환 (HUD 캐릭터 교체 버튼 → 호출)
        // ──────────────────────────────────────────────
        /// <summary>현재 활성 파티 내에서 index 번 캐릭터에게 조작권 부여</summary>
        public void SwitchActiveCharacter(int index)
        {
            if (IsGameOver) return;
            if (index < 0 || index >= ActiveParty.Count) return;
            var target = ActiveParty[index];
            if (target.Status.IsDead) return;

            _activeCharacterIndex = index;
            AssignPlayerControl(ActiveParty, index);
        }

        // ──────────────────────────────────────────────
        // 게임오버 체크 (CharacterBase.Die() → 호출)
        // ──────────────────────────────────────────────
        /// <summary>
        /// 활성 파티 2명 전부 사망 여부 확인.
        /// 전멸 확인 시 ReviveService에 부활 팝업 권한 넘김 or 직접 게임오버 처리.
        /// </summary>
        public void CheckGameOver()
        {
            if (IsGameOver) return;

            bool isPartyWiped = ActiveParty.All(c => c.Status.IsDead);
            if (!isPartyWiped) return;

            Debug.Log($"<color=red>[BattleManager] ‼ 파티 전멸! 보스전 실패 판정</color>");

            // ReviveService가 존재하고 아직 부활권이 남아 있으면 팝업 표시
            if (ReviveService.Instance != null && !ReviveService.Instance.HasUsedRevive)
            {
                Debug.Log("[BattleManager] 부활 가능 → ReviveService에 팝업 요청");
                ReviveService.Instance.ShowRevivePopup();
            }
            else
            {
                TriggerGameOver();
            }
        }

        /// <summary>실제 게임오버 확정 (ReviveService → 또는 부활 없이 직접 호출)</summary>
        public void TriggerGameOver()
        {
            if (IsGameOver) return;
            IsGameOver = true;

            Debug.Log("<color=red>[BattleManager] 💀 GAME OVER</color>");

            // CombatManager를 통한 전투 종료 처리
            if (CombatManager.Instance != null)
                CombatManager.Instance.EndBattle(false);

            // TODO: 패배 연출 UI 활성화
            // InGameHUDController.Instance?.ShowGameOverUI();
        }

        // ──────────────────────────────────────────────
        // 부활 처리 (ReviveService → 호출)
        // ──────────────────────────────────────────────
        /// <summary>부활 승인 후 현재 파티 사망 캐릭터들 즉시 100% 부활</summary>
        public void ExecuteRevive()
        {
            IsGameOver = false;

            foreach (var ctrl in ActiveParty)
            {
                if (ctrl.Status.IsDead)
                {
                    ctrl.Status.ReviveToFull();

                    // CharacterBase hp도 동기화
                    var cb = ctrl.GetComponent<CharacterBase>();
                    if (cb != null)
                    {
                        cb.currentHealth = cb.maxHealth;
                    }

                    ctrl.gameObject.SetActive(true);
                    Debug.Log($"<color=cyan>[BattleManager] {ctrl.Status.characterName} 부활 완료!</color>");
                }
            }

            // 조작 캐릭터 재지정 (첫 번째 생존자)
            var alive = ActiveParty.FirstOrDefault(c => !c.Status.IsDead);
            if (alive != null)
                AssignPlayerControl(ActiveParty, ActiveParty.IndexOf(alive));
        }

        // ──────────────────────────────────────────────
        // 정보 조회 (TagCharacterController.HealerAI 등에서 사용)
        // ──────────────────────────────────────────────
        /// <summary>모든 파티원(1파티 + 2파티)의 CharacterBase 목록 반환</summary>
        public List<CharacterBase> GetAllPartyMembers()
        {
            var result = new List<CharacterBase>();
            foreach (var c in Party1)
            {
                var cb = c.GetComponent<CharacterBase>();
                if (cb != null) result.Add(cb);
            }
            foreach (var c in Party2)
            {
                var cb = c.GetComponent<CharacterBase>();
                if (cb != null) result.Add(cb);
            }
            return result;
        }

        // ──────────────────────────────────────────────
        // 내부 유틸리티
        // ──────────────────────────────────────────────
        private void ActivateParty(int partyIndex, bool forceFirstCharacter)
        {
            _activePartyIndex = partyIndex;

            var active   = partyIndex == 1 ? Party1 : Party2;
            var inactive = partyIndex == 1 ? Party2 : Party1;

            // 1. 대기 파티 비활성화 (Status는 건드리지 않음)
            foreach (var ctrl in inactive)
            {
                ctrl.SetPlayerControl(false);
                ctrl.gameObject.SetActive(false);
            }

            // 2. 활성 파티 활성화 (생존 캐릭터만)
            int firstAliveIndex = -1;
            for (int i = 0; i < active.Count; i++)
            {
                var ctrl = active[i];
                if (!ctrl.Status.IsDead)
                {
                    ctrl.gameObject.SetActive(true);
                    // RefreshCooldownUI는 OnEnable에서 자동 호출됨
                    if (firstAliveIndex < 0) firstAliveIndex = i;
                }
            }

            // 3. 조작권 부여
            if (forceFirstCharacter && firstAliveIndex >= 0)
            {
                _activeCharacterIndex = firstAliveIndex;
                AssignPlayerControl(active, firstAliveIndex);
            }

            // 4. 파티 전멸 여부 즉시 체크
            CheckGameOver();

            Debug.Log($"[BattleManager] 파티{partyIndex} 태그-인 완료. 조작 캐릭터 Index={_activeCharacterIndex}");
        }

        /// <summary>partyList 중 controlledIndex 만 isPlayerControlled=true, 나머지는 false</summary>
        private void AssignPlayerControl(List<TagCharacterController> partyList, int controlledIndex)
        {
            for (int i = 0; i < partyList.Count; i++)
            {
                bool shouldControl = (i == controlledIndex) && !partyList[i].Status.IsDead;
                partyList[i].SetPlayerControl(shouldControl);
                if (shouldControl)
                    ActivePlayerCharacter = partyList[i];
            }
        }
    }
}
