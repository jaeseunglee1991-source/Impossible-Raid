using System;

namespace BossRaid.Models
{
    [Serializable]
    public class RoomMember
    {
        public string id;
        public string nickname;
        public string job; // Paladin, Warrior, etc. ("Random" by default)
        public bool isReady;
        public bool isHost;
        public int warningCount;

        public RoomMember(string id, string nickname, bool isHost = false)
        {
            this.id = id;
            this.nickname = nickname;
            this.isHost = isHost;
            this.job = "Random";
            this.isReady = false;
            this.warningCount = 0;
        }
    }
}
