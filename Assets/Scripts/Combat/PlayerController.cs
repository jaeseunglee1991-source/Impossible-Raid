using UnityEngine;
using System.Collections;
using BossRaid.Combat;

namespace BossRaid.Combat.Player
{
    public class PlayerController : MonoBehaviour
    {
        public CharacterBase characterInfo;
        public float dashDistance = 5f;
        public float dashCooldown = 5f;
        public float dashDuration = 0.5f;
        
        private float nextDashTime = 0f;
        private bool isInvulnerable = false;
        private Vector3 moveInput;

        private void Update()
        {
            HandleMovement();
            HandleAutoAttack();
            HandleSkills();

            if (Input.GetKeyDown(KeyCode.Space)) Dash();
        }

        private void HandleMovement()
        {
            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");
            moveInput = new Vector3(horizontal, 0f, vertical).normalized;

            if (moveInput.magnitude >= 0.1f)
            {
                transform.position += moveInput * characterInfo.movementSpeed * Time.deltaTime;
                transform.forward = moveInput; // 이동 방향 바라보기
            }
        }

        private void HandleAutoAttack()
        {
            // 가장 가까운 적(보스) 감지 후 사거리 내면 자동 공격
            // characterInfo.AutoAttackTarget(FindNearestBoss());
        }

        private void HandleSkills()
        {
            if (Input.GetKeyDown(KeyCode.Q)) characterInfo.UseSkill(0);
            if (Input.GetKeyDown(KeyCode.W)) characterInfo.UseSkill(1);
            if (Input.GetKeyDown(KeyCode.E)) characterInfo.UseSkill(2);
            if (Input.GetKeyDown(KeyCode.R)) characterInfo.UseUltimate();
        }

        private void Dash()
        {
            if (Time.time < nextDashTime) return;

            StartCoroutine(PerformDash());
            nextDashTime = Time.time + dashCooldown;
        }

        private IEnumerator PerformDash()
        {
            isInvulnerable = true;
            Debug.Log("[Combat] Dashing! Invulnerable for 0.5s.");
            
            Vector3 dashDir = moveInput == Vector3.zero ? transform.forward : moveInput;
            float startTime = Time.time;
            
            while (Time.time < startTime + dashDuration)
            {
                transform.position += dashDir * (dashDistance / dashDuration) * Time.deltaTime;
                yield return null;
            }

            isInvulnerable = false;
        }

        public bool CheckInvulnerable() => isInvulnerable;
    }
}
