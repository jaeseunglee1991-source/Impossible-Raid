using System;

namespace BossRaid.Models
{
    [Serializable]
    public class RoomMember
    {
        public string id;
        public string nickname;
        public string job; // Paladin, Warrior, etc.
        public bool isReady;
        public bool isHost;

        public RoomMember(string id, string nickname, bool isHost = false)
        {
            this.id = id;
            this.nickname = nickname;
            this.isHost = isHost;
            this.job = "None";
            this.isReady = false;
        }
    }
}
