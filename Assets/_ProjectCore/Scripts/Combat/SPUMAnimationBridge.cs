using UnityEngine;

namespace BossRaid.Combat
{
    /// <summary>
    /// SPUM 에셋과 우리 게임 로직(CharacterBase) 사이를 연결하는 브릿지 클래스입니다.
    /// 나중에 SPUM을 사용하지 않게 되면 이 컴포넌트만 제거하면 됩니다.
    /// </summary>
    public class SPUMAnimationBridge : MonoBehaviour, ICharacterAnimationHandler
    {
        private SPUM_Prefabs _spumPrefab;
        private Animator _animator;

        private void Awake()
        {
            _spumPrefab = GetComponentInChildren<SPUM_Prefabs>();
            _animator = GetComponentInChildren<Animator>();
        }

        private void Start()
        {
            if (_spumPrefab != null)
            {
                // SPUM 내부 애니메이션 데이터 로드 (에셋 수동 세팅 누락 대비)
                _spumPrefab.PopulateAnimationLists();
                
                // SPUM 내부 딕셔너리 및 오버라이드 컨트롤러 강제 초기화 (KeyNotFoundException 방지)
                _spumPrefab.OverrideControllerInit();

                Debug.Log($"[SPUMBridge] {gameObject.name} 초기화 완료.");
            }
        }

        public void PlayAnimation(string stateName, int index = 0)
        {
            if (_spumPrefab == null) return;

            PlayerState state = PlayerState.IDLE;
            string upperName = stateName.ToUpper();

            switch (upperName)
            {
                case "IDLE":   state = PlayerState.IDLE; break;
                case "MOVE":   state = PlayerState.MOVE; break;
                case "ATTACK": state = PlayerState.ATTACK; break;
                case "HIT":    state = PlayerState.DAMAGED; break;
                case "DIE":    state = PlayerState.DEATH; break;
                default:       state = PlayerState.OTHER; break;
            }

            // [수정] SPUM 내부의 복잡한 트리거 대신 공식 Enum 메서드만 호출
            _spumPrefab.PlayAnimation(state, index);
            
            // 이동 상태일 때만 파라미터 제어 (SetTrigger는 SPUM 내부 Dictionary 에러 위험이 있어 제거)
            if (_animator != null)
            {
                if (upperName == "MOVE") _animator.SetBool("1_Move", true);
                else if (upperName == "IDLE") _animator.SetBool("1_Move", false);
            }
        }

        public void SetMoveSpeed(float speed)
        {
            // 필요 시 SPUM 애니메이션 재생 속도 조절 로직 추가
        }
    }
}
