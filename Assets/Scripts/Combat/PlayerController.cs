using UnityEngine;
using System.Collections;
using BossRaid.Combat;
using BossRaid.UI;

namespace BossRaid.Combat.Player
{
    /// <summary>
    /// 2D 쿼터뷰 플레이어 조작 컨트롤러
    /// - 이동: XY 축 기반 (Vector2)
    /// - 방향 전환: SpriteRenderer.flipX (3D transform.forward 완전 제거)
    /// - 키보드 + 가상 조이스틱 이중 지원
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        public CharacterBase characterInfo;
        public float dashDistance = 5f;
        public float dashCooldown = 5f;
        public float dashDuration = 0.5f;

        private float _nextDashTime = 0f;
        private Vector2 _moveInput;              // 2D: Vector2 (XY)

        private SpriteRenderer _spriteRenderer;  // 2D 방향 반전용
        private Animator _animator;

        private void Awake()
        {
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            _animator = GetComponentInChildren<Animator>();
        }

        private void Update()
        {
            HandleMovement();
        }

        private void HandleMovement()
        {
            float horizontal = 0f;
            float vertical   = 0f;

            // 1. 키보드 입력
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.upArrowKey.isPressed)    vertical   += 1f;
                if (keyboard.downArrowKey.isPressed)  vertical   -= 1f;
                if (keyboard.leftArrowKey.isPressed)  horizontal -= 1f;
                if (keyboard.rightArrowKey.isPressed) horizontal += 1f;
            }

            _moveInput = new Vector2(horizontal, vertical).normalized;

            // 2. 가상 조이스틱 입력 (우선순위)
            if (InGameHUDController.Instance != null && InGameHUDController.Instance.joystick != null)
            {
                var joyDir = InGameHUDController.Instance.joystick.InputDirection;
                if (joyDir.magnitude > 0.1f)
                    _moveInput = joyDir.normalized;
            }

            if (_moveInput.magnitude >= 0.1f)
            {
                // ── 2D XY 이동 (Z 고정) ──
                transform.position += new Vector3(_moveInput.x, _moveInput.y, 0f)
                                      * characterInfo.movementSpeed * Time.deltaTime;

                // ── 좌우 스프라이트 반전 (3D rotation 대체) ──
                if (_spriteRenderer != null)
                    _spriteRenderer.flipX = (_moveInput.x < 0f);

                // ── 애니메이터 파라미터 전달 ──
                if (_animator != null)
                {
                    _animator.SetFloat("MoveX", _moveInput.x);
                    _animator.SetFloat("MoveY", _moveInput.y);
                    _animator.SetFloat("MovementSpeed", _moveInput.magnitude);
                }
            }
            else
            {
                if (_animator != null) _animator.SetFloat("MovementSpeed", 0f);
            }
        }

        /// <summary>HUD Dodge 버튼 또는 Space 키에서 호출</summary>
        public void Dash()
        {
            if (Time.time < _nextDashTime) return;
            StartCoroutine(PerformDash());
            _nextDashTime = Time.time + dashCooldown;
        }

        private IEnumerator PerformDash()
        {
            if (characterInfo != null) characterInfo.SetInvulnerable(0.5f);
            Debug.Log("[Combat] Dashing! Invulnerable for 0.5s.");

            // 2D 대시: XY 방향 사용
            Vector2 dashDir = _moveInput.magnitude > 0.1f ? _moveInput : Vector2.right;
            float startTime = Time.time;

            while (Time.time < startTime + dashDuration)
            {
                transform.position += new Vector3(dashDir.x, dashDir.y, 0f)
                                      * (dashDistance / dashDuration) * Time.deltaTime;
                yield return null;
            }
        }

        public bool IsInvulnerable() => characterInfo != null && characterInfo.CheckInvulnerable();
    }
}
