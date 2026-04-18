using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BossRaid.Combat.Boss;

namespace BossRaid.UI
{
    /// <summary>
    /// 중앙 상단 보스 정보 프레임 UI
    /// 보스 HP, 스태거 게이지, 캐스팅 바, 페이즈 표시
    /// </summary>
    public class BossFrameUI : MonoBehaviour
    {
        [Header("Boss Info")]
        public TextMeshProUGUI bossNameText;       // "보스이름 (1단계 / 8단계)"
        public TextMeshProUGUI bossHpPercentText;  // "24%"

        [Header("Boss HP Bar")]
        public Slider bossHpBar;
        public Image bossHpFill;
        
        [Header("Stagger Gauge")]
        public Slider staggerBar;
        public Image staggerFill;
        public TextMeshProUGUI staggerText;        // "경직 게이지"

        [Header("Casting Bar")]
        public GameObject castingBarRoot;
        public Slider castingBar;
        public Image castingFill;
        public TextMeshProUGUI castingNameText;    // "마나 폭발" 등 패턴명
        public TextMeshProUGUI castingTimeText;    // "2.3s"

        [Header("Boss Status")]
        public TextMeshProUGUI bossStatusText;     // "Neutral | Provoked | Enraged"

        [Header("AnyRPG UnitFrame Support (Optional)")]
        public Slider anyRpgHealthSlider;
        public Slider anyRpgResourceSlider; // Use for Stagger
        public TextMeshProUGUI anyRpgLevelText; // Use for Phase
        public Image anyRpgIcon;

        [Header("State")]
        public BossAI linkedBoss;

        // 캐스팅 상태
        private bool isCasting = false;
        private float castDuration = 0f;
        private float castElapsed = 0f;
        private string castName = "";

        // 스태거 상태
        private float maxStagger = 1000f;
        private float currentStagger = 0f;


        private void Awake()
        {
            // [UI 미니멀리즘] 화면을 너무 크게 차지하는 보스 프레임을 숨깁니다.
            // 대신 보스 캐릭터 머리 위의 월드 스페이스 UI가 역할을 대신합니다.
            CanvasGroup cg = GetComponent<CanvasGroup>();
            if (cg == null) cg = gameObject.AddComponent<CanvasGroup>();
            cg.alpha = 0f; // 비주얼은 숨기되, 데이터 브릿지 역할은 수행
            cg.interactable = false;
            cg.blocksRaycasts = false;
        }

        public void Initialize(BossAI boss)
        {
            linkedBoss = boss;
            if (bossNameText != null) bossNameText.text = boss.bossName;
            if (castingBarRoot != null) castingBarRoot.SetActive(false);
            
            // 월드 스페이스 HP바 동적 생성 준비 (있다면 호출)
            SetupWorldHealthBar(boss);
        }

        private void SetupWorldHealthBar(BossAI boss)
        {
            // 실제 월드 스페이스 바 생성 로직은 전투 시작 시 
            // InGameHUDController에서 프리팹으로 생성할 것을 권장합니다.
            Debug.Log($"[{boss.bossName}] 월드 스페이스 HP바 준비 완료.");
        }

        private void Update()
        {
            if (linkedBoss == null) return;

            // 데이터는 계속 업데이트하되, 비주얼은 숨겨진 상태
            UpdateBossHP();
            UpdateStagger();
            UpdateCasting();
        }

        private void UpdateBossHP()
        {
            float ratio = linkedBoss.maxHealth > 0 
                ? linkedBoss.currentHealth / linkedBoss.maxHealth 
                : 0f;

            if (bossHpBar != null) bossHpBar.value = ratio;
            if (bossHpPercentText != null) 
                bossHpPercentText.text = $"{Mathf.Max(0, ratio * 100f):F0}%";

            if (bossHpFill != null)
            {
                bossHpFill.color = Color.Lerp(
                    new Color(0.6f, 0f, 0f), 
                    new Color(0.9f, 0.15f, 0f), 
                    ratio);
            }
        }

        private void UpdateStagger()
        {
            float staggerRatio = maxStagger > 0 ? currentStagger / maxStagger : 0f;
            if (staggerBar != null) staggerBar.value = staggerRatio;
            if (staggerText != null) 
                staggerText.text = $"경직: {(int)currentStagger}/{(int)maxStagger}";
            
            if (staggerFill != null)
                staggerFill.color = Color.Lerp(
                    new Color(0.2f, 0.4f, 0.8f), 
                    new Color(1f, 0.8f, 0.2f), 
                    staggerRatio);
        }

        private void UpdateCasting()
        {
            if (!isCasting)
            {
                if (castingBarRoot != null && castingBarRoot.activeSelf) 
                    castingBarRoot.SetActive(false);
                return;
            }

            castElapsed += Time.deltaTime;
            float ratio = castDuration > 0 ? castElapsed / castDuration : 0f;

            if (castingBarRoot != null && !castingBarRoot.activeSelf) 
                castingBarRoot.SetActive(true);
            if (castingBar != null) castingBar.value = ratio;
            if (castingTimeText != null) 
                castingTimeText.text = $"{Mathf.Max(0, castDuration - castElapsed):F1}s";

            if (castElapsed >= castDuration)
            {
                isCasting = false;
                if (castingBarRoot != null) castingBarRoot.SetActive(false);
            }
        }

        // =====  외부 호출 API  =====

        /// <summary>보스 캐스팅 시작 (BossAI에서 호출)</summary>
        public void StartCasting(string patternName, float duration)
        {
            castName = patternName;
            castDuration = duration;
            castElapsed = 0;
            isCasting = true;
            if (castingNameText != null) castingNameText.text = patternName;
            
            // 캐스팅 바 색상 (차단 가능 = 주황, 차단 불가 = 보라색)
            if (castingFill != null)
                castingFill.color = new Color(1f, 0.6f, 0f);
        }

        /// <summary>캐스팅 차단됨</summary>
        public void InterruptCasting()
        {
            isCasting = false;
            if (castingBarRoot != null) castingBarRoot.SetActive(false);
        }

        /// <summary>스태거 데미지 적용</summary>
        public void AddStagger(float amount)
        {
            currentStagger = Mathf.Min(currentStagger + amount, maxStagger);
            if (currentStagger >= maxStagger)
            {
                Debug.Log("[BossFrame] 보스 경직 발생!");
                currentStagger = 0; // 리셋
            }
        }

        /// <summary>보스 페이즈 업데이트</summary>
        public void UpdatePhase(int currentPhase, int maxPhase)
        {
            string phaseStr = $"{currentPhase}";
            if (anyRpgLevelText != null) anyRpgLevelText.text = phaseStr;

            if (bossNameText != null && linkedBoss != null)
                bossNameText.text = $"{linkedBoss.bossName} ({currentPhase}단계 / {maxPhase}단계)";
        }

        /// <summary>보스 상태 텍스트 업데이트</summary>
        public void UpdateBossStatus(string statusStr)
        {
            if (bossStatusText != null)
                bossStatusText.text = statusStr;
        }

        /// <summary>캐스팅 중인지 여부</summary>
        public bool IsCasting => isCasting;
    }
}
