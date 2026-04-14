using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BossRaid.Combat;

namespace BossRaid.UI
{
    /// <summary>
    /// 좌측 상단 파티원 개별 프레임 UI
    /// 직업 아이콘 + 이름 + HP 바 + 디버프/상태 표시
    /// </summary>
    public class PartyMemberFrame : MonoBehaviour
    {
        [Header("UI References")]
        public Image roleIcon;            // 직업 아이콘 (색상으로 역할 구분)
        public TextMeshProUGUI nameText;  // 플레이어 닉네임
        public TextMeshProUGUI roleText;  // [New] 직업명 (전사, 힐러 등)
        public Slider hpBar;              // HP 바
        public Image hpFill;              // HP 바 Fill 이미지 (색상 제어용)
        public TextMeshProUGUI hpText;    // HP 숫자 텍스트
        public Image deadOverlay;         // 사망 시 어둡게 덮는 오버레이

        [Header("AnyRPG Visual Assets")]
        public Image frameBackground;     // 배경 이미지 컴포넌트
        public Image frameBorder;         // 아이콘 테두리 컴포넌트
        public Sprite hpFillSprite;       // AnyRPG SquareSolid
        public Sprite iconBorderSprite;   // AnyRPG IconGoldFrame
        public Sprite frameBackgroundSprite; // AnyRPG PaneBGDark512

        [Header("State")]
        public CharacterBase linkedCharacter;
        public bool isLocalPlayer = false;
        
        // [Mobile Optimization] 메모리 효율을 위한 아이콘 캐싱 (안드로이드 리소스 관리)
        private static System.Collections.Generic.Dictionary<string, Sprite> iconCache = new System.Collections.Generic.Dictionary<string, Sprite>();

        private void Awake()
        {
            OrganizeLayoutHierarchy();
        }

        private void OrganizeLayoutHierarchy()
        {
            // 인스펙터 레퍼런스가 비어있을 경우에만 이름으로 자동 검색 (계층 구조 파괴 금지)
            if (hpBar == null) hpBar = GetComponentInChildren<Slider>(true);
            if (hpText == null) hpText = FindComponent<TextMeshProUGUI>(new[] { "HPText", "HP_ValueText", "Value" });
            if (nameText == null) nameText = FindComponent<TextMeshProUGUI>(new[] { "NameText", "PlayerName", "Name" });
            if (roleIcon == null) roleIcon = FindComponent<Image>(new[] { "RoleIcon", "Icon", "ClassIcon" });
            if (hpFill == null && hpBar != null && hpBar.fillRect != null) hpFill = hpBar.fillRect.GetComponent<Image>();

            if (frameBackground == null) frameBackground = GetComponent<Image>();

            // 강제로 숨겨진 텍스트 복구 (선택적)
            if (nameText != null) nameText.gameObject.SetActive(true);
            if (hpText != null) hpText.gameObject.SetActive(true);
            
            // 텍스트가 바닥에 깔리는 것을 방지하기 위해 계층 순서만 렌더링 최상위로 올림
            if (hpText != null) hpText.transform.SetAsLastSibling();
            if (nameText != null) nameText.transform.SetAsLastSibling();
        }

        private TextMeshProUGUI CreateDynamicText(string name)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(this.transform, false);
            var text = go.GetComponent<TextMeshProUGUI>();
            text.raycastTarget = false;
            return text;
        }

        private T FindComponent<T>(string[] names) where T : Component
        {
            T[] all = GetComponentsInChildren<T>(true);
            foreach (var comp in all)
            {
                foreach (var n in names)
                {
                    if (comp.name.IndexOf(n, System.StringComparison.OrdinalIgnoreCase) >= 0)
                        return comp;
                }
            }
            return null;
        }

        public void Initialize(CharacterBase character, bool isLocal)
        {
            linkedCharacter = character;
            isLocalPlayer = isLocal;

            if (nameText != null) {
                string nameColor = isLocal ? "#FFFF72" : "#FFFFFF"; // 로컬은 황금색, 타인은 흰색
                string jobName = GetKoreanJobName(character.GetType().Name);
                // 유저명 옆에 직업이 바로 붙도록 Rich Text 조합
                nameText.text = $"<color={nameColor}>{character.characterName}</color> <color=#B0B0B0><size=80%>[{jobName}]</size></color>";
            }

            ApplyAnyRPGVisuals();

            // 아이콘 로드
            if (roleIcon != null)
            {
                string className = character.GetType().Name;
                Sprite classSprite = null;
                if (!iconCache.TryGetValue(className, out classSprite))
                {
                    classSprite = LoadSpriteFromResources($"UI/ClassIcons/{className}");
                    if (classSprite != null) iconCache[className] = classSprite;
                }
                
                if (classSprite != null) {
                    roleIcon.sprite = classSprite;
                    roleIcon.color = Color.white;
                    roleIcon.preserveAspect = true;
                } else {
                    ApplyRoleColor(character);
                }
            }

            character.OnHealthChanged += UpdateHPUI;
            UpdateHPUI(character.currentHealth, character.maxHealth);
        }

        private string GetKoreanJobName(string className)
        {
            // WaitingRoomController의 jobNames 배열과 동일하게 맞춤
            switch(className) {
                case "Warrior":     return "전사";
                case "Rogue":       return "도적";
                case "Paladin":     return "성기사";
                case "DeathKnight": return "죽음의 기사";
                case "Ranger":      return "레인저";
                case "FireMage":    return "화염 마법사";
                case "IceMage":     return "냉기 마법사";
                case "Warlock":     return "흑마법사";
                case "Priest":      return "사제";
                case "Druid":       return "드루이드";
                default:            return "모험가";
            }
        }

        private void OnDestroy()
        {
            if (linkedCharacter != null) linkedCharacter.OnHealthChanged -= UpdateHPUI;
        }

        private void UpdateHPUI(float current, float max)
        {
            if (linkedCharacter == null) return;
            float hpRatio = current / (max > 0 ? max : 1f);
            if (hpBar != null) hpBar.value = hpRatio;
            if (hpText != null) hpText.text = $"{(int)current} / {(int)max}";

            if (hpFill != null) {
                if (hpRatio > 0.65f) 
                    hpFill.color = Color.Lerp(new Color(1f, 0.85f, 0.1f), new Color(0.2f, 0.9f, 0.3f), (hpRatio - 0.65f) * 2.8f); 
                else if (hpRatio > 0.3f)
                    hpFill.color = Color.Lerp(new Color(1f, 0.5f, 0.1f), new Color(1f, 0.85f, 0.1f), (hpRatio - 0.3f) / 0.35f); 
                else
                    hpFill.color = Color.Lerp(new Color(0.7f, 0.05f, 0f), new Color(1f, 0.5f, 0.1f), hpRatio / 0.3f); 
            }
            if (deadOverlay != null) deadOverlay.gameObject.SetActive(linkedCharacter.IsDead);
        }

        private Sprite LoadSpriteFromResources(string path)
        {
            Sprite s = Resources.Load<Sprite>(path);
            if (s != null) return s;
            Texture2D tex = Resources.Load<Texture2D>(path);
            if (tex != null) return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            return null;
        }

        private void ApplyRoleColor(CharacterBase character)
        {
            if (roleIcon == null) return;
            switch (character.role) {
                case CharacterRole.Tank: roleIcon.color = new Color(0.1f, 0.6f, 1f, 1f); break; 
                case CharacterRole.Healer: roleIcon.color = new Color(0.1f, 1f, 0.4f, 1f); break;
                case CharacterRole.MeleeDPS: roleIcon.color = new Color(1f, 0.2f, 0.2f, 1f); break; 
                case CharacterRole.RangedDPS: roleIcon.color = new Color(1f, 0.8f, 0.1f, 1f); break;
                default: roleIcon.color = Color.white; break;
            }
        }

        private void ApplyAnyRPGVisuals()
        {
            if (frameBackground != null && frameBackgroundSprite != null) {
                frameBackground.sprite = frameBackgroundSprite;
                frameBackground.type = Image.Type.Sliced;
                frameBackground.color = new Color(0.1f, 0.1f, 0.12f, 0.8f);
            }
            if (frameBorder != null && iconBorderSprite != null) {
                frameBorder.sprite = iconBorderSprite;
                frameBorder.color = Color.white;
            }
            if (hpFill != null && hpFillSprite != null) hpFill.sprite = hpFillSprite;
        }

        private void Update() { }
    }
}
