using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

namespace BossRaid.UI
{
    /// <summary>
    /// 상업용 방치형 게임에서 스킬 사용, 보스 방어 등 세부 전투 기록을 텍스트로 올려주는 스크롤 로그입니다.
    /// 메모리 최적화를 위해 오브젝트 풀링 방식으로 텍스트 객체를 재사용합니다.
    /// </summary>
    public class CombatLogUI : MonoBehaviour
    {
        public static CombatLogUI Instance { get; private set; }

        [Header("Settings")]
        [Tooltip("화면에 표시할 최대 로그 개수")]
        public int maxLogCount = 30;

        [Header("References")]
        public Transform logContainer;       // 로그 텍스트가 쌓일 부모 (ScrollRect의 Content)
        public GameObject logTextPrefab;     // TextMeshProUGUI가 붙은 프리팹

        private Queue<TextMeshProUGUI> _logPool = new Queue<TextMeshProUGUI>();
        private ScrollRect _scrollRect;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            _scrollRect = GetComponentInChildren<ScrollRect>();
        }

        private void Start()
        {
            // 전투 로그 첫 줄
            AddLog("<color=green>전투가 시작되었습니다!</color>");
        }

        /// <summary>
        /// 새로운 전투 로그를 추가합니다. (예: "전사가 1,500 데미지를 입혔습니다.")
        /// </summary>
        public void AddLog(string message)
        {
            if (logContainer == null || logTextPrefab == null) return;

            TextMeshProUGUI txtObj;

            // 1. 최대 개수를 넘어서면 풀링 (가장 오래된 텍스트 재사용)
            if (logContainer.childCount >= maxLogCount && _logPool.Count == 0)
            {
                Transform oldestLog = logContainer.GetChild(0);
                txtObj = oldestLog.GetComponent<TextMeshProUGUI>();
                oldestLog.SetAsLastSibling(); // 맨 아래로 옯김
            }
            else
            {
                // 2. 풀에 남은 게 있으면 거기서 꺼내고, 아니면 새로 생성
                if (_logPool.Count > 0)
                {
                    txtObj = _logPool.Dequeue();
                    txtObj.gameObject.SetActive(true);
                    txtObj.transform.SetAsLastSibling();
                }
                else
                {
                    GameObject go = Instantiate(logTextPrefab, logContainer);
                    txtObj = go.GetComponent<TextMeshProUGUI>();
                }
            }

            // 텍스트 세팅
            txtObj.text = $"[{System.DateTime.Now:HH:mm:ss}] {message}";

            // 스크롤 맨 아래로 내리기 시도 (타이밍 이슈 방지용 LayoutRebuilder 권장)
            Canvas.ForceUpdateCanvases();
            if (_scrollRect != null)
            {
                _scrollRect.verticalNormalizedPosition = 0f;
            }
        }
    }
}
