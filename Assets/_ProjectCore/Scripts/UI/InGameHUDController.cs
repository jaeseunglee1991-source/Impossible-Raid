using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BossRaid.Combat;
using BossRaid.Combat.Boss;
using System.Collections.Generic;

namespace BossRaid.UI
{
    /// <summary>
    /// 인게임 메인 HUD 컨트롤러
    /// 파티 프레임, 보스 프레임, 스킬 버튼, 가상 조이스틱, 시스템 UI를 통합 관리
    /// </summary>
    public class InGameHUDController : MonoBehaviour
    {
        public static InGameHUDController Instance { get; private set; }

        [Header("Party Frame (좌측 상단)")]
        public Transform partyFrameContainer;
        public GameObject partyMemberFramePrefab;
        private List<PartyMemberFrame> partyFrames = new List<PartyMemberFrame>();

        [Header("Boss Frame (중앙 상단)")]
        public BossFrameUI bossFrame;

        [Header("Skill Buttons (우측 하단)")]
        public SkillButtonUI skill1Button;   // Q - 차단 스킬
        public SkillButtonUI skill2Button;   // W
        public SkillButtonUI skill3Button;   // E
        public SkillButtonUI ultimateButton; // R - 궁극기
        public SkillButtonUI dodgeButton;    // Space - 회피

        [Header("Virtual Joystick (좌측 하단)")]
        public VirtualJoystick joystick;

        [Header("System UI (우측 상단)")]
        public TextMeshProUGUI timerText;
        public Button settingsButton;
        public Button scoreButton;
        public Button giveUpButton;   
        public Button inventoryButton; 
        public TextMeshProUGUI lifeText;

        [Header("Inventory")]
        public InventoryUIController inventoryUI;

        [Header("Settings Panel")]
        public GameObject settingsPanel;
        public Button settingsCloseButton;      
        public Button settingsExitBattleButton; 

        [Header("Give Up Popup")]
        public GameObject giveUpPopupPanel;
        public Button giveUpConfirmButton;
        public Button giveUpCancelButton;

        [Header("Player HP (로컬 플레이어)")]
        public Image playerHpFill;
        public TextMeshProUGUI playerHpText;

        [Header("Game State")]
        public CharacterBase localPlayer;
        public BossAI currentBoss;

        [Header("Network & Loading UI (신규 추가)")]
        [Tooltip("서버 통신 중 화면 터치를 막는 반투명 패널 (UI Canvas 아래에 생성 후 연결하세요)")]
        public GameObject loadingBlockPanel; 
        [Tooltip("오류나 알림 발생 시 화면 중앙에 뜨는 텍스트 (UI Canvas 아래에 생성 후 연결하세요)")]
        public TextMeshProUGUI systemMessageText; 

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            // 성장(업그레이드) UI 컨트롤러 자동 할당
            if (FindAnyObjectByType<GrowthUIController>() == null)
            {
                GameObject growthPanel = GameObject.Find("GrowthPanel");
                if (growthPanel != null)
                {
                    growthPanel.AddComponent<GrowthUIController>();
                }
                else
                {
                    gameObject.AddComponent<GrowthUIController>();
                }
            }

            if (settingsPanel != null)
            {
                if (settingsCloseButton == null) settingsCloseButton = settingsPanel.transform.Find("CloseSettings")?.GetComponent<Button>();
                if (settingsExitBattleButton == null) settingsExitBattleButton = settingsPanel.transform.Find("GiveUpButton")?.GetComponent<Button>();
            }

            if (settingsButton != null) settingsButton.onClick.AddListener(ToggleSettings);
            if (settingsCloseButton != null) settingsCloseButton.onClick.AddListener(ToggleSettings);
            if (settingsExitBattleButton != null) settingsExitBattleButton.onClick.AddListener(ConfirmGiveUp);
            if (settingsPanel != null) settingsPanel.SetActive(false);

            if (giveUpButton != null) giveUpButton.onClick.AddListener(ShowGiveUpPopup);
            if (giveUpConfirmButton != null) giveUpConfirmButton.onClick.AddListener(ConfirmGiveUp);
            if (giveUpCancelButton != null) giveUpCancelButton.onClick.AddListener(HideGiveUpPopup);
            if (giveUpPopupPanel != null) giveUpPopupPanel.SetActive(false);

            if (inventoryButton != null) inventoryButton.onClick.AddListener(ToggleInventory);

            ApplyHUDContainerStyling();
            
            // 시작 시 로딩 패널과 메시지 초기화
            if (loadingBlockPanel != null) loadingBlockPanel.SetActive(false);
            if (systemMessageText != null) systemMessageText.gameObject.SetActive(false);
        }

        private void ApplyHUDContainerStyling()
        {
            if (partyFrameContainer != null) {
                var bg = partyFrameContainer.GetComponent<Image>();
                if (bg != null) bg.color = new Color(0.1f, 0.1f, 0.15f, 0.45f); 
            }
            
            if (timerText != null) {
                var timerBg = timerText.transform.parent.GetComponent<Image>();
                if (timerBg != null) timerBg.color = new Color(0.1f, 0.1f, 0.12f, 0.7f);
            }
        }

        private void Update()
        {
            UpdateTimer();
            UpdateKeyboardSkills();
            CheckInterruptGlow();
        }

        // ===== [신규] 네트워크 및 알림 UI 제어 =====

        /// <summary>서버 통신 중 UI 터치 차단 패널 제어</summary>
        public void ToggleLoadingPanel(bool isShow)
        {
            if (loadingBlockPanel != null)
            {
                loadingBlockPanel.SetActive(isShow);
            }
            else if (isShow)
            {
                Debug.LogWarning("[HUD] 서버 통신 중... (loadingBlockPanel이 인스펙터에 연결되지 않았습니다)");
            }
        }

        /// <summary>화면 중앙에 시스템 메시지 표시 후 자동 페이드아웃</summary>
        public void ShowSystemMessage(string message, float duration = 2.5f)
        {
            if (systemMessageText != null)
            {
                systemMessageText.text = message;
                systemMessageText.gameObject.SetActive(true);
                systemMessageText.canvasRenderer.SetAlpha(1f);
                systemMessageText.CrossFadeAlpha(0f, duration, false);
            }
            else
            {
                Debug.LogWarning($"[SystemMessage] {message}");
            }
        }

        // ===== 초기화 =====
        public void InitializeHUD(List<CharacterBase> players, BossAI boss, CharacterBase myPlayer)
        {
            localPlayer = myPlayer;
            currentBoss = boss;
            if (bossFrame != null) bossFrame.Initialize(boss);
            InitializePartyFrames(players, myPlayer);
            InitializeSkillButtons();
        }

        private void InitializePartyFrames(List<CharacterBase> players, CharacterBase myPlayer)
        {
            foreach (var frame in partyFrames)
            {
                if (frame != null) Destroy(frame.gameObject);
            }
            partyFrames.Clear();

            foreach (var player in players)
            {
                if (partyFrameContainer != null && partyMemberFramePrefab != null)
                {
                    var frameGO = Instantiate(partyMemberFramePrefab, partyFrameContainer);
                    var frame = frameGO.GetComponent<PartyMemberFrame>();
                    if (frame != null)
                    {
                        bool isLocal = (player == myPlayer);
                        frame.Initialize(player, isLocal);
                        partyFrames.Add(frame);
                    }
                }
            }
        public void RefreshAllPartyFrames()
        {
            foreach (var frame in partyFrames)
            {
                if (frame != null) frame.UpdateHPUI(frame.linkedCharacter.currentHealth, frame.linkedCharacter.maxHealth);
            }
            
            // 스킬 버튼 정보도 현재 조력 캐릭터에 맞춰 갱신
            InitializeSkillButtons();
        }

        public void InitializeSkillButtons()
        {
            if (localPlayer != null)
            {
                if (skill1Button != null) 
                { 
                    skill1Button.Initialize(0, "Q", true,  OnSkillUsed); 
                    skill1Button.SetSkillInfo(null, localPlayer.skillNames[0], localPlayer.skillCooldowns[0]); 
                }
                if (skill2Button != null) 
                { 
                    skill2Button.Initialize(1, "W", false, OnSkillUsed); 
                    skill2Button.SetSkillInfo(null, localPlayer.skillNames[1], localPlayer.skillCooldowns[1]); 
                }
                if (skill3Button != null) 
                { 
                    skill3Button.Initialize(2, "E", false, OnSkillUsed); 
                    skill3Button.SetSkillInfo(null, localPlayer.skillNames[2], localPlayer.skillCooldowns[2]); 
                }
                if (ultimateButton != null) 
                { 
                    ultimateButton.Initialize(3, "R", false, OnUltimateUsed); 
                    ultimateButton.SetSkillInfo(null, localPlayer.ultimateName, localPlayer.ultimateCooldown); 
                }
            }
            if (dodgeButton != null) 
            { 
                dodgeButton.Initialize(4, "Space", false, OnDodgeUsed); 
                dodgeButton.SetSkillInfo(null, "Dash", 5f); 
            }
        }

        // ===== 업데이트 =====
        private void UpdateTimer()
        {
            if (timerText == null) return;
            var combat = CombatManager.Instance;
            if (combat != null && combat.isGameActive)
            {
                int min = (int)combat.remainingTime / 60;
                int sec = (int)combat.remainingTime % 60;
                timerText.text = $"{min:D2}:{sec:D2}";
                
                if (combat.remainingTime <= 30f)
                    timerText.color = Color.Lerp(Color.red, Color.white, Mathf.PingPong(Time.time * 2f, 1f));
                else
                    timerText.color = Color.white;
            }
        }

        private void UpdateKeyboardSkills()
        {
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.qKey.wasPressedThisFrame && skill1Button != null) skill1Button.GetComponent<Button>().onClick.Invoke();
            if (keyboard.wKey.wasPressedThisFrame && skill2Button != null) skill2Button.GetComponent<Button>().onClick.Invoke();
            if (keyboard.eKey.wasPressedThisFrame && skill3Button != null) skill3Button.GetComponent<Button>().onClick.Invoke();
            if (keyboard.rKey.wasPressedThisFrame && ultimateButton != null) ultimateButton.GetComponent<Button>().onClick.Invoke();
            if (keyboard.spaceKey.wasPressedThisFrame && dodgeButton != null) dodgeButton.GetComponent<Button>().onClick.Invoke();

            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                if (inventoryUI != null && inventoryUI.inventoryPanel != null && inventoryUI.inventoryPanel.activeSelf)
                    inventoryUI.CloseInventory();
                else if (giveUpPopupPanel != null && giveUpPopupPanel.activeSelf)
                    HideGiveUpPopup();
                else
                    ToggleSettings();
            }
        }

        private void CheckInterruptGlow()
        {
            bool bossCasting = bossFrame != null && bossFrame.IsCasting;
            if (skill1Button != null) skill1Button.SetInterruptGlow(bossCasting);
        }

        // ===== 이벤트 핸들러 =====
        private void OnSkillUsed(int index)
        {
            if (localPlayer != null)
            {
                localPlayer.TryUseSkill(index);
            }
        }

        private void OnUltimateUsed(int index)
        {
            if (localPlayer != null)
            {
                // 인스펙터 index에 상관없이 궁극기는 3번(또는 별도 메서드) 호출
                localPlayer.TryUseSkill(3);
            }
        }

        private void OnDodgeUsed(int index)
        {
            var pc = localPlayer?.GetComponent<Combat.Player.PlayerController>();
            if (pc != null)
            {
                pc.Dash();
            }
        }

        private void ToggleSettings()
        {
            if (settingsPanel != null) settingsPanel.SetActive(!settingsPanel.activeSelf);
        }

        private void ShowGiveUpPopup()
        {
            if (giveUpPopupPanel != null) giveUpPopupPanel.SetActive(true);
        }

        private void ToggleInventory()
        {
            if (inventoryUI != null)
            {
                if (inventoryUI.inventoryPanel != null && inventoryUI.inventoryPanel.activeSelf)
                {
                    inventoryUI.CloseInventory();
                }
                else
                {
                    string charName = localPlayer != null ? localPlayer.characterName : "";
                    inventoryUI.OpenInventory(charName);
                }
            }
        }

        private void HideGiveUpPopup()
        {
            if (giveUpPopupPanel != null) giveUpPopupPanel.SetActive(false);
        }

        private void ConfirmGiveUp()
        {
            if (giveUpPopupPanel != null) giveUpPopupPanel.SetActive(false);
            if (CombatManager.Instance != null && CombatManager.Instance.isGameActive)
            {
                CombatManager.Instance.EndBattle(false);
            }
        }

        // ===== 외부 API =====
        public void UpdateLife(int currentLives, int maxLives)
        {
            if (lifeText != null) lifeText.text = $"LIFE: {currentLives}/{maxLives}";
        }

        public void UpdateLocalPlayerHP(float currentHealth, float maxHealth)
        {
            if (playerHpFill != null) playerHpFill.fillAmount = currentHealth / maxHealth;
            if (playerHpText != null) playerHpText.text = $"{currentHealth:F0} / {maxHealth:F0}";
        }

        public void NotifyBossCasting(string patternName, float duration)
        {
            if (bossFrame != null) bossFrame.StartCasting(patternName, duration);
        }

        public void NotifyBossInterrupt()
        {
            if (bossFrame != null) bossFrame.InterruptCasting();
        }
    }
}
