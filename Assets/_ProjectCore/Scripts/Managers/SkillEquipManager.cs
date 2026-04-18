using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using BossRaid.Combat;

namespace BossRaid.Managers
{
    /// <summary>
    /// ──────────────────────────────────────────────────────────────────
    /// SkillEquipManager — 스킬 장착 UI 및 저장 연동
    ///
    /// [씬 구성]
    ///   equipPanel          : 전체 장착 창 (전투 전 로비/대기실 화면)
    ///   characterTabs[]     : 캐릭터 전환 탭 버튼 (전사, 마법사, 도적, 힐러)
    ///   slotButtons[]       : 슬롯 0~2 버튼 (현재 장착된 스킬 표시)
    ///   availableSkillItems : 선택 가능한 스킬 목록 ScrollView 안에 동적 생성
    ///
    /// [흐름]
    ///   1. 캐릭터 탭 선택 → 해당 캐릭터의 allSkills 목록 표시
    ///   2. 슬롯 버튼 클릭 → 슬롯 선택(하이라이트)
    ///   3. 스킬 목록 아이템 클릭 → EquipSkill() 호출 → SaveManager.MarkDirty()
    ///   4. 창 닫기 → 자동 저장
    /// ──────────────────────────────────────────────────────────────────
    /// </summary>
    public class SkillEquipManager : MonoBehaviour
    {
        public static SkillEquipManager Instance { get; private set; }

        // ═══════════════════════════════════════════════════════════
        //  인스펙터 연결
        // ═══════════════════════════════════════════════════════════

        [Header("패널")]
        public GameObject equipPanel;

        [Header("캐릭터 탭 버튼 (CombatManager.activePlayers 순서와 일치)")]
        public Button[] characterTabButtons;
        public Image[]  characterTabHighlights; // 선택된 탭 강조

        [Header("슬롯 버튼 (0~2 + 궁극기)")]
        public Button[]          slotButtons;       // 슬롯 0, 1, 2
        public TextMeshProUGUI[] slotNameTexts;     // 슬롯에 표시될 스킬 이름
        public Image[]           slotHighlights;    // 선택된 슬롯 강조 테두리
        public Button            ultimateSlotButton;
        public TextMeshProUGUI   ultimateNameText;

        [Header("스킬 목록 (ScrollView Content)")]
        public Transform  skillListContent;   // ScrollView > Viewport > Content
        public GameObject skillItemPrefab;    // 스킬 목록 아이템 프리팹

        [Header("스킬 상세 설명")]
        public TextMeshProUGUI detailNameText;
        public TextMeshProUGUI detailDescText;
        public TextMeshProUGUI detailCooldownText;

        // ═══════════════════════════════════════════════════════════
        //  런타임 상태
        // ═══════════════════════════════════════════════════════════

        private CharacterBase _selectedCharacter;
        private int           _selectedSlot     = -1;   // -1: 미선택, 3: 궁극기
        private List<GameObject> _skillItemPool  = new List<GameObject>();

        // ═══════════════════════════════════════════════════════════
        //  생명주기
        // ═══════════════════════════════════════════════════════════

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            SetupSlotButtons();
            SetupCharacterTabs();
            if (equipPanel != null) equipPanel.SetActive(false);
        }

        // ═══════════════════════════════════════════════════════════
        //  패널 열기 / 닫기
        // ═══════════════════════════════════════════════════════════

        public void OpenPanel()
        {
            if (equipPanel == null) return;
            equipPanel.SetActive(true);

            // 첫 번째 캐릭터(로컬 플레이어) 탭 자동 선택
            var firstChar = GetCharacterByTabIndex(0);
            if (firstChar != null) SelectCharacter(firstChar, 0);
        }

        public void ClosePanel()
        {
            if (equipPanel == null) return;
            equipPanel.SetActive(false);
            _selectedSlot = -1;
            RefreshSlotHighlights();
        }

        // ═══════════════════════════════════════════════════════════
        //  캐릭터 탭 선택
        // ═══════════════════════════════════════════════════════════

        private void SetupCharacterTabs()
        {
            if (characterTabButtons == null) return;
            for (int i = 0; i < characterTabButtons.Length; i++)
            {
                int idx = i;
                characterTabButtons[i]?.onClick.AddListener(() =>
                {
                    var ch = GetCharacterByTabIndex(idx);
                    if (ch != null) SelectCharacter(ch, idx);
                });
            }
        }

        private void SelectCharacter(CharacterBase character, int tabIndex)
        {
            _selectedCharacter = character;
            _selectedSlot      = -1;

            // 탭 하이라이트
            if (characterTabHighlights != null)
            {
                for (int i = 0; i < characterTabHighlights.Length; i++)
                    if (characterTabHighlights[i] != null)
                        characterTabHighlights[i].gameObject.SetActive(i == tabIndex);
            }

            RefreshSlotDisplay();
            RefreshSkillList();
            ClearDetail();
        }

        private CharacterBase GetCharacterByTabIndex(int idx)
        {
            if (CombatManager.Instance == null) return null;
            var players = CombatManager.Instance.activePlayers;
            if (idx < 0 || idx >= players.Count) return null;
            return players[idx];
        }

        // ═══════════════════════════════════════════════════════════
        //  슬롯 버튼 설정
        // ═══════════════════════════════════════════════════════════

        private void SetupSlotButtons()
        {
            if (slotButtons != null)
            {
                for (int i = 0; i < slotButtons.Length; i++)
                {
                    int slotIdx = i;
                    slotButtons[i]?.onClick.AddListener(() => SelectSlot(slotIdx));
                }
            }
            ultimateSlotButton?.onClick.AddListener(() => SelectSlot(3));
        }

        private void SelectSlot(int slotIndex)
        {
            _selectedSlot = (_selectedSlot == slotIndex) ? -1 : slotIndex; // 토글
            RefreshSlotHighlights();

            // 슬롯 선택 시 현재 장착 스킬 상세 표시
            if (_selectedCharacter != null && _selectedSlot >= 0)
            {
                SkillDefinition skill = (_selectedSlot == 3)
                    ? _selectedCharacter.ultimateSkill
                    : _selectedCharacter.GetEquippedSkill(_selectedSlot);
                ShowDetail(skill);
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  슬롯 UI 갱신
        // ═══════════════════════════════════════════════════════════

        private void RefreshSlotDisplay()
        {
            if (_selectedCharacter == null) return;

            if (slotNameTexts != null)
            {
                for (int i = 0; i < CharacterBase.SKILL_SLOT_COUNT && i < slotNameTexts.Length; i++)
                {
                    var skill = _selectedCharacter.GetEquippedSkill(i);
                    if (slotNameTexts[i] != null)
                        slotNameTexts[i].text = skill?.skillName ?? "— 비어있음 —";
                }
            }

            if (ultimateNameText != null)
                ultimateNameText.text = _selectedCharacter.ultimateSkill?.skillName ?? "궁극기 없음";

            RefreshSlotHighlights();
        }

        private void RefreshSlotHighlights()
        {
            if (slotHighlights == null) return;
            for (int i = 0; i < slotHighlights.Length; i++)
            {
                if (slotHighlights[i] != null)
                    slotHighlights[i].gameObject.SetActive(i == _selectedSlot);
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  스킬 목록 갱신
        // ═══════════════════════════════════════════════════════════

        private void RefreshSkillList()
        {
            if (skillListContent == null || skillItemPrefab == null || _selectedCharacter == null)
                return;

            // 기존 아이템 정리
            foreach (var go in _skillItemPool) Destroy(go);
            _skillItemPool.Clear();

            // 스킬 아이템 동적 생성
            foreach (var skill in _selectedCharacter.allSkills)
            {
                SkillDefinition capturedSkill = skill;
                GameObject item = Instantiate(skillItemPrefab, skillListContent);
                _skillItemPool.Add(item);

                // 이름 표시
                var nameText = item.GetComponentInChildren<TextMeshProUGUI>();
                if (nameText != null) nameText.text = skill.skillName;

                // 차단 스킬 표시 (아이콘 or 색상)
                var icon = item.transform.Find("InterruptIcon");
                if (icon != null) icon.gameObject.SetActive(skill.canInterrupt);

                // 클릭 이벤트
                var btn = item.GetComponent<Button>();
                btn?.onClick.AddListener(() => OnSkillItemClicked(capturedSkill));

                // 마우스 오버 → 상세 표시 (EventTrigger 없이 간단 구현)
                var hoverTrigger = item.AddComponent<SkillItemHover>();
                hoverTrigger.Init(capturedSkill, ShowDetail);
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  스킬 클릭 → 장착
        // ═══════════════════════════════════════════════════════════

        private void OnSkillItemClicked(SkillDefinition skill)
        {
            if (_selectedCharacter == null) return;

            ShowDetail(skill);

            // 슬롯이 선택되어 있지 않으면 첫 번째 빈 슬롯에 자동 배치
            if (_selectedSlot < 0)
            {
                for (int i = 0; i < CharacterBase.SKILL_SLOT_COUNT; i++)
                {
                    if (_selectedCharacter.equippedSlots[i] < 0)
                    {
                        _selectedSlot = i;
                        break;
                    }
                }
                if (_selectedSlot < 0) _selectedSlot = 0; // 꽉 차있으면 0번 교체
            }

            // 궁극기는 슬롯 교체 불가 (별도 고정 슬롯)
            if (_selectedSlot == 3) return;

            int skillIndex = _selectedCharacter.allSkills.IndexOf(skill);
            if (skillIndex < 0) return;

            _selectedCharacter.EquipSkill(_selectedSlot, skillIndex);

            RefreshSlotDisplay();
            RefreshSkillList();
        }

        // ═══════════════════════════════════════════════════════════
        //  스킬 상세 설명
        // ═══════════════════════════════════════════════════════════

        private void ShowDetail(SkillDefinition skill)
        {
            if (skill == null) { ClearDetail(); return; }
            if (detailNameText    != null) detailNameText.text    = skill.skillName;
            if (detailDescText    != null) detailDescText.text    = skill.description;
            if (detailCooldownText!= null) detailCooldownText.text = $"쿨타임 {skill.cooldown:F0}초";
        }

        private void ClearDetail()
        {
            if (detailNameText    != null) detailNameText.text    = "";
            if (detailDescText    != null) detailDescText.text    = "";
            if (detailCooldownText!= null) detailCooldownText.text = "";
        }

        // ═══════════════════════════════════════════════════════════
        //  에디터 테스트 유틸
        // ═══════════════════════════════════════════════════════════

        [ContextMenu("패널 열기")]
        private void TestOpen() => OpenPanel();

        [ContextMenu("패널 닫기")]
        private void TestClose() => ClosePanel();
    }

    // ─────────────────────────────────────────────────────────────
    //  보조 컴포넌트: 스킬 목록 아이템 호버 처리
    // ─────────────────────────────────────────────────────────────

    /// <summary>스킬 목록 아이템에 자동 부착. 마우스 오버 시 상세 패널 업데이트.</summary>
    public class SkillItemHover : MonoBehaviour,
        UnityEngine.EventSystems.IPointerEnterHandler,
        UnityEngine.EventSystems.IPointerExitHandler
    {
        private SkillDefinition               _skill;
        private System.Action<SkillDefinition> _onHover;

        public void Init(SkillDefinition skill, System.Action<SkillDefinition> onHover)
        {
            _skill   = skill;
            _onHover = onHover;
        }

        public void OnPointerEnter(UnityEngine.EventSystems.PointerEventData e) => _onHover?.Invoke(_skill);
        public void OnPointerExit (UnityEngine.EventSystems.PointerEventData e) { }
    }
}
