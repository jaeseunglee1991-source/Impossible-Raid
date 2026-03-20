using System;

namespace BossRaid.Models
{
    [Serializable]
    public class CombatRecord
    {
        public string userId;
        public string nickname;
        public string role;
        public float totalDamage;
        public float totalHealing;
        public float totalDamageTaken;
        public bool isMvp;

        public CombatRecord(string id, string nick, string r)
        {
            userId = id;
            nickname = nick;
            role = r;
            totalDamage = 0;
            totalHealing = 0;
            totalDamageTaken = 0;
            isMvp = false;
        }
    }
}
