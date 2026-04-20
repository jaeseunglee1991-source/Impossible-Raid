using System.Threading.Tasks;
using UnityEngine;
using BossRaid.Equipment;

namespace BossRaid.Managers
{
    /// <summary>
    /// 보상 지급을 담당하는 가상 서버 서비스 layer
    /// 실제 배포 환경에서는 이 로직이 서버(Python/Node.js/Go 등)에 위치해야 하며, 
    /// 클라이언트는 결과값만 받아와야 핵(Hack)을 방지할 수 있습니다.
    /// </summary>
    public static class RewardService
    {
        /// <summary>
        /// 보스 처치 보상을 서버에 요청합니다. (확률 계산 서버 수행 시뮬레이션)
        /// </summary>
        public static async Task<EquipmentData> RequestBossReward(int stageLevel)
        {
            // 1. 네트워크 지연 발생 (시뮬레이션)
            await Task.Delay(500); 

            // 2. [서버 로직] 확률 기반 아이템 생성
            // 실제 서비스에서는 여기서 DB를 조회하고 검증을 수행합니다.
            EquipmentData reward = DropTable.Roll(stageLevel, isBossDrop: true);

            Debug.Log($"<color=lime>[Server] 보상 생성 완료: {reward.FullName} (Stage {stageLevel})</color>");
            return reward;
        }

        /// <summary>
        /// 잡몹 처치 시 드랍 여부를 서버에 확인합니다.
        /// </summary>
        public static async Task<EquipmentData> RequestMobDrop(int stageLevel)
        {
            // 방치형 사냥은 빈번하므로 지연을 짧게 설정하거나 배칭 처리하는 것이 일반적입니다.
            if (Random.value > DropTable.MOB_DROP_CHANCE) return null;

            await Task.Delay(100);
            return DropTable.Roll(stageLevel, isBossDrop: false);
        }
    }
}
