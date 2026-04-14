using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BossRaid.Combat.Boss;

namespace BossRaid.UI
{
    /// <summary>
    /// 보스 전용 월드 스페이스 UI (완전 코드 생성)
    /// - HP 바: 보스 머리 위 표시
    /// - 스킬 게이지(시전바): 보스 발 밑(바닥) 표시 및 크기 확대
    /// </summary>
    public class BossWorldHealthBar : MonoBehaviour
    {
        // ─── 런타임 생성 참조 ───
        private Slider _hpSlider;
        private Image _hpFillImage;
        private TextMeshProUGUI _hpText;
        private TextMeshProUGUI _nameText;

        private Slider _energySlider;
        private Image _energyFillImage;
        private TextMeshProUGUI _energyText;

        private GameObject _castRoot;
        private Slider _castSlider;
        private TextMeshProUGUI _castNameText;

        private BossAI _boss;
        private Transform _bossTransform;
        private Camera _mainCam;

        // ─── 설정값 ───
        private const float CANVAS_SCALE = 0.01f;   // 월드 스페이스 UI 크기 배율
        private const float HEAD_Y_OFFSET = 1.2f;   // 2D: 캐릭터 위(Y축) HP바 높이
        private const float FEET_Y_OFFSET = -320f;  // HP바 기준 발 밑 로컬 위치

        public void Setup(BossAI boss)
        {
            _boss = boss;
            _bossTransform = boss.transform;
            _mainCam = Camera.main;

            // UI 전체를 코드로 생성
            CreateWorldCanvas();
            CreateHPBar();
            CreateEnergyBar();
            CreateCastBar();

            if (_nameText != null) _nameText.text = boss.bossName;

            boss.OnHealthChanged += RefreshHP;
            boss.OnEnergyChanged += RefreshEnergy;
            RefreshHP(boss.currentHealth, boss.maxHealth);
            RefreshEnergy(boss.currentEnergy, boss.maxEnergy);
        }

        private void CreateWorldCanvas()
        {
            Canvas canvas = GetComponent<Canvas>();
            if (canvas == null) canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = GetComponent<CanvasScaler>();
            if (scaler == null) scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 10f;

            RectTransform rt = GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(400, 400); // 상하폭을 넓게 잡아 HP와 스킬바 수용
            rt.localScale = Vector3.one * CANVAS_SCALE;
        }

        private void CreateHPBar()
        {
            GameObject hpRoot = MakeChild("HPBarRoot", transform);
            hpRoot.transform.localPosition = Vector3.zero;

            // 배경 (은은한 글로우 + 어두운 바탕)
            Image bg = MakeImageChild(hpRoot.transform, "BG", 280, 32);
            bg.color = new Color(0.05f, 0.05f, 0.08f, 0.9f);

            // [Dressing] 테두리 효과 (살짝 밝은 외곽선)
            Image border = MakeImageChild(hpRoot.transform, "Border", 284, 36);
            border.color = new Color(0.15f, 0.15f, 0.2f, 1f);
            border.transform.SetAsFirstSibling();

            // HP 슬라이더 (크기 확대)
            _hpSlider = MakeSlider(hpRoot.transform, "HPSlider", 260, 22, Vector2.zero);
            _hpFillImage = _hpSlider.fillRect.GetComponent<Image>();
            _hpFillImage.color = new Color(0.85f, 0.15f, 0.1f);

            // HP 텍스트
            _hpText = MakeTMPChild(hpRoot.transform, "HPText", 250, 22, Vector2.zero);
            _hpText.alignment = TextAlignmentOptions.Center;
            _hpText.fontSize = 15;
            _hpText.color = Color.white;
            _hpText.fontStyle = FontStyles.Bold;
            _hpText.outlineWidth = 0.25f;
            _hpText.outlineColor = Color.black;

            // 보스 이름 (황금색 강조)
            _nameText = MakeTMPChild(hpRoot.transform, "BossName", 350, 35, new Vector2(0, 38));
            _nameText.alignment = TextAlignmentOptions.Center;
            _nameText.fontSize = 18;
            _nameText.color = new Color(1f, 0.85f, 0.3f);
            _nameText.fontStyle = FontStyles.Bold;
            _nameText.outlineWidth = 0.35f;
            _nameText.outlineColor = Color.black;
        }

        private void CreateEnergyBar()
        {
            GameObject energyRoot = MakeChild("EnergyBarRoot", transform);
            energyRoot.transform.localPosition = new Vector3(0, -32, 0); // HP 바 약간 아래에 배치

            // 백그라운드
            Image energyBg = MakeImageChild(energyRoot.transform, "BG", 240, 12);
            energyBg.color = new Color(0.05f, 0.05f, 0.08f, 0.9f);

            // 테두리
            Image energyBorder = MakeImageChild(energyRoot.transform, "Border", 244, 16);
            energyBorder.color = new Color(0.1f, 0.1f, 0.15f, 0.8f); 
            energyBorder.transform.SetAsFirstSibling();

            // 에너지 슬라이더
            _energySlider = MakeSlider(energyRoot.transform, "EnergySlider", 240, 12, Vector2.zero);
            _energyFillImage = _energySlider.fillRect.GetComponent<Image>();
            _energyFillImage.color = new Color(0.1f, 0.6f, 1.0f); // 파란색 에너지
            
            // 에너지 텍스트 (%)
            _energyText = MakeTMPChild(energyRoot.transform, "EnergyText", 100, 15, Vector2.zero);
            _energyText.alignment = TextAlignmentOptions.Center;
            _energyText.fontSize = 11;
            _energyText.color = Color.white;
            _energyText.fontStyle = FontStyles.Bold;
        }

        private void CreateCastBar()
        {
            _castRoot = MakeChild("CastBarRoot", transform);
            
            // [위치 변경] 보스 발 밑 위치로 오프셋 설정
            _castRoot.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, FEET_Y_OFFSET); 
            _castRoot.SetActive(false);

            // [크기 변경] 체력바와 동일한 위드(260)로 설정
            _castSlider = MakeSlider(_castRoot.transform, "CastSlider", 260, 18, Vector2.zero);
            Image castFill = _castSlider.fillRect.GetComponent<Image>();
            castFill.color = new Color(1f, 0.88f, 0.2f); // 따뜻한 노란색 게이지
            
            // 바탕 무색 (테두리만 살짝 주어 게이지 영역 표시)
            Image castBorder = MakeImageChild(_castRoot.transform, "Border", 268, 24);
            castBorder.color = new Color(0.1f, 0.1f, 0.15f, 0.8f); 
            castBorder.transform.SetAsFirstSibling();

            // 스킬 이름 텍스트 (더 크게 강조)
            _castNameText = MakeTMPChild(_castRoot.transform, "SkillName", 300, 30, new Vector2(0, 26));
            _castNameText.alignment = TextAlignmentOptions.Center;
            _castNameText.fontSize = 16;
            _castNameText.color = new Color(1f, 0.95f, 0.45f);
            _castNameText.fontStyle = FontStyles.Bold;
            _castNameText.outlineWidth = 0.3f;
            _castNameText.outlineColor = Color.black;
        }

        private void RefreshHP(float cur, float max)
        {
            float ratio = max > 0f ? cur / max : 0f;
            if (_hpSlider != null) _hpSlider.value = ratio;
            if (_hpText != null) _hpText.text = $"{(int)cur} / {(int)max}";

            if (_hpFillImage != null)
            {
                // [Quality Gradient] 보스의 체력 상태에 따른 긴장감 있는 색상 변화
                // 빨간색(낮음) -> 오렌지(중간) -> 에메랄드(높음)
                if (ratio > 0.5f)
                    _hpFillImage.color = Color.Lerp(new Color(0.9f, 0.5f, 0.1f), new Color(0.1f, 0.9f, 0.3f), (ratio - 0.5f) * 2f);
                else
                    _hpFillImage.color = Color.Lerp(new Color(0.7f, 0f, 0.05f), new Color(0.9f, 0.5f, 0.1f), ratio * 2f);
            }
        }

        private void RefreshEnergy(float cur, float max)
        {
            float ratio = max > 0f ? cur / max : 0f;
            if (_energySlider != null) _energySlider.value = ratio;
            if (_energyText != null) _energyText.text = $"{(int)Mathf.Floor(ratio*100f)}%";

            if (_energyFillImage != null)
            {
                // 에너지가 가득 차면 색상이 보라색(마젠타)으로 변화
                if (ratio >= 0.99f)
                    _energyFillImage.color = new Color(1f, 0.2f, 0.8f);
                else
                    _energyFillImage.color = new Color(0.1f, 0.6f, 1f);
            }
        }

        private void Update()
        {
            if (_boss == null || _bossTransform == null) { Destroy(gameObject); return; }

            // 1. 2D: 캐릭터 위쪽(Y) 오프셋으로 UI 배치
            Vector3 targetPos = _bossTransform.position + Vector3.up * HEAD_Y_OFFSET;
            targetPos.z = -1f; // 2D 카메라 앞에 렌더링 (캐릭터 Z=0보다 앞)
            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * 15f);

            // 2. 2D 환경: 빌보드(LookRotation) 불필요 — 카메라가 고정 정방향이므로 회전 제거

            // 3. 캐스팅 게이지 제어
            if (_boss.isCasting)
            {
                if (!_castRoot.activeSelf) _castRoot.SetActive(true);
                _castSlider.value = _boss.currentCastProgress;
                _castNameText.text = _boss.currentCastName;
            }
            else
            {
                if (_castRoot.activeSelf) _castRoot.SetActive(false);
            }

            if (_boss.currentHealth <= 0f) gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_boss != null) 
            {
                _boss.OnHealthChanged -= RefreshHP;
                _boss.OnEnergyChanged -= RefreshEnergy;
            }
        }

        // ─── 유틸리티 ───
        private GameObject MakeChild(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private Image MakeImageChild(Transform parent, string name, float w, float h)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(w, h);
            var img = go.GetComponent<Image>();
            img.raycastTarget = false;
            return img;
        }

        private Slider MakeSlider(Transform parent, string name, float w, float h, Vector2 pos)
        {
            var root = new GameObject(name, typeof(RectTransform));
            root.transform.SetParent(parent, false);
            var rootRt = root.GetComponent<RectTransform>();
            rootRt.sizeDelta = new Vector2(w, h);
            rootRt.anchoredPosition = pos;

            var fillArea = new GameObject("FillArea", typeof(RectTransform));
            fillArea.transform.SetParent(root.transform, false);
            var faRt = fillArea.GetComponent<RectTransform>();
            faRt.anchorMin = Vector2.zero; faRt.anchorMax = Vector2.one;
            faRt.offsetMin = Vector2.zero; faRt.offsetMax = Vector2.zero;

            var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillGo.transform.SetParent(fillArea.transform, false);
            var fillRt = fillGo.GetComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero; fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = Vector2.zero; fillRt.offsetMax = Vector2.zero;
            
            var fillImg = fillGo.GetComponent<Image>();
            fillImg.raycastTarget = false;

            var slider = root.AddComponent<Slider>();
            slider.fillRect = fillRt;
            slider.targetGraphic = fillImg;
            slider.interactable = false;
            return slider;
        }

        private TextMeshProUGUI MakeTMPChild(Transform parent, string name, float w, float h, Vector2 pos)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = pos;
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.raycastTarget = false;
            return tmp;
        }
    }
}
