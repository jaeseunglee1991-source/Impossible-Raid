using UnityEngine;
using TMPro;

namespace BossRaid.UI
{
    /// <summary>
    /// 개별 데미지 텍스트 팝업 (위로 날아가는 애니메이션)
    /// </summary>
    public class FloatingDamageText : MonoBehaviour
    {
        private TextMeshProUGUI _textMesh;
        private float _lifetime = 1.0f;
        private float _timer = 0f;
        private Vector3 _startPos;
        private float _moveSpeed = 1.5f;

        public void Initialize(string text, Color color, float lifetime = 1.0f)
        {
            if (_textMesh == null) _textMesh = GetComponent<TextMeshProUGUI>();

            _textMesh.text = text;
            _textMesh.color = color;
            _lifetime = lifetime;
            _timer = 0f;
            _startPos = transform.position;

            // 약간의 무작위로 위치 틀어주기 (겹침 방지)
            float randomX = Random.Range(-0.5f, 0.5f);
            transform.position += new Vector3(randomX, 0, 0);
        }

        private void Update()
        {
            _timer += Time.deltaTime;

            // 위로 떠오르는 애니메이션
            transform.position += Vector3.up * _moveSpeed * Time.deltaTime;

            // 점점 투명해짐
            float alpha = Mathf.Clamp01(1f - (_timer / _lifetime));
            _textMesh.color = new Color(_textMesh.color.r, _textMesh.color.g, _textMesh.color.b, alpha);

            if (_timer >= _lifetime)
            {
                Destroy(gameObject); // 최적화 시 ObjectPool Release로 변경 가능
            }
        }
    }
}
