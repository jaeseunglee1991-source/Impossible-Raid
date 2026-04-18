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
        public Button giveUpButton;   // 새 버튼 추가
        public TextMeshProUGUI lifeText;

        [Header("Settings Panel")]
        public GameObject settingsPanel;
        public Button settingsCloseButton;      // 설정창 우측 상단 X 버튼
        public Button settingsExitBattleButton; // 설정창 하단 '전투 종료' 버튼

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

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            // 수동 할당이 안된 경우 대비하여 이름으로 자동 찾기 로직 추가
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

            ApplyHUDContainerStyling();
        }

        private void ApplyHUDContainerStyling()
        {
            // 파티 프레임 컨테이너 배경 설정 (AnyRPG 스타일)
            if (partyFrameContainer != null) {
                var bg = partyFrameContainer.GetComponent<Image>();
                if (bg != null) {
                    bg.color = new Color(0.1f, 0.1f, 0.15f, 0.45f); // 은은한 어두운 배경
                }
            }
            
            // 타이머 등 시스템 UI 배경
            if (timerText != null) {
                var timerBg = timerText.transform.parent.GetComponent<Image>();
                if (timerBg != null) {
                    timerBg.color = new Color(0.1f, 0.1f, 0.12f, 0.7f);
                }
            }
        }

        private void Update()
        {
            UpdateTimer();
            UpdateKeyboardSkills();
            CheckInterruptGlow();
        }

        // ===== 초기화 =====

        /// <summary>전투 시작 시 HUD 초기화</summary>
        public void InitializeHUD(List<CharacterBase> players, BossAI boss, CharacterBase myPlayer)
        {
            localPlayer = myPlayer;
            currentBoss = boss;

            // 보스 프레임 초기화
            if (bossFrame != null) bossFrame.Initialize(boss);

            // 파티 프레임 생성
            InitializePartyFrames(players, myPlayer);

            // 스킬 버튼 초기화
            InitializeSkillButtons();
        }

        private void InitializePartyFrames(List<CharacterBase> players, CharacterBase myPlayer)
        {
            // 기존 프레임 정리
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
        }

        private void InitializeSkillButtons()
        {
            if (localPlayer != null)
            {
                // [Premium] 실제 아이콘이 없을 경우 기본 색상이라도 입혀서 출력
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
                
                // 시간 부족 경고 (30초 이하: 빨간색 깜빡임)
                if (combat.remainingTime <= 30f)
                    timerText.color = Color.Lerp(Color.red, Color.white, Mathf.PingPong(Time.time * 2f, 1f));
                else
                    timerText.color = Color.white;
            }
        }

        private void UpdateKeyboardSkills()
        {
            // 새로운 입력 시스템 (Input System Package) 지원
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.qKey.wasPressedThisFrame && skill1Button != null) skill1Button.GetComponent<Button>().onClick.Invoke();
            if (keyboard.wKey.wasPressedThisFrame && skill2Button != null) skill2Button.GetComponent<Button>().onClick.Invoke();
            if (keyboard.eKey.wasPressedThisFrame && skill3Button != null) skill3Button.GetComponent<Button>().onClick.Invoke();
            if (keyboard.rKey.wasPressedThisFrame && ultimateButton != null) ultimateButton.GetComponent<Button>().onClick.Invoke();
            if (keyboard.spaceKey.wasPressedThisFrame && dodgeButton != null) dodgeButton.GetComponent<Button>().onClick.Invoke();

            // ESC키 동작: 열려있는 팝업을 닫거나 설정창 열기
            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                if (giveUpPopupPanel != null && giveUpPopupPanel.activeSelf)
                    HideGiveUpPopup();
                else
                    ToggleSettings();
            }
        }

        private void CheckInterruptGlow()
        {
            // 보스 캐스팅 중일 때 차단 스킬 글로우 활성화
            bool bossCasting = bossFrame != null && bossFrame.IsCasting;
            if (skill1Button != null) skill1Button.SetInterruptGlow(bossCasting);
        }

        // ===== 이벤트 핸들러 =====

        private void OnSkillUsed(int index)
        {
            if (localPlayer != null)
            {
                localPlayer.TryUseSkill(index);
                Debug.Log($"[HUD] Skill {index} used.");
            }
        }

        private void OnUltimateUsed(int index)
        {
            if (localPlayer != null)
            {
                localPlayer.TryUseSkill(3);
                Debug.Log("[HUD] Ultimate used!");
            }
        }

        private void OnDodgeUsed(int index)
        {
            // PlayerController의 Dash 호출
            var pc = localPlayer?.GetComponent<Combat.Player.PlayerController>();
            if (pc != null)
            {
                pc.Dash();
                Debug.Log("[HUD] Dodge used!");
            }
        }

        private void ToggleSettings()
        {
            if (settingsPanel != null)
            {
                settingsPanel.SetActive(!settingsPanel.activeSelf);
            }
        }

        private void ShowGiveUpPopup()
        {
            if (giveUpPopupPanel != null) giveUpPopupPanel.SetActive(true);
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

        /// <summary>라이프 텍스트 업데이트</summary>
        public void UpdateLife(int currentLives, int maxLives)
        {
            if (lifeText != null)
                lifeText.text = $"LIFE: {currentLives}/{maxLives}";
        }

        public void UpdateLocalPlayerHP(float currentHealth, float maxHealth)
        {
            if (playerHpFill != null)
                playerHpFill.fillAmount = currentHealth / maxHealth;
            if (playerHpText != null)
                playerHpText.text = $"{currentHealth:F0} / {maxHealth:F0}";
        }

        /// <summary>보스 캐스팅 시작 (외부에서 호출)</summary>
        public void NotifyBossCasting(string patternName, float duration)
        {
            if (bossFrame != null) bossFrame.StartCasting(patternName, duration);
        }

        /// <summary>보스 캐스팅 차단 (외부에서 호출)</summary>
        public void NotifyBossInterrupt()
        {
            if (bossFrame != null) bossFrame.InterruptCasting();
        }
    }
}
