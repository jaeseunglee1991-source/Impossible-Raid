using System;
using Supabase.Postgrest.Models;
using Supabase.Postgrest.Attributes;

namespace BossRaid.Models
{
    [Table("rooms")]
    public class RoomRecord : BaseModel
    {
        [PrimaryKey("id", false)]
        public int id { get; set; }

        [Column("created_at")]
        public DateTime created_at { get; set; }

        [Column("room_name")]
        public string room_name { get; set; }

        [Column("host_id")]
        public string host_id { get; set; }

        [Column("host_nickname")]
        public string host_nickname { get; set; }

        [Column("player_count")]
        public int player_count { get; set; }

        [Column("max_players")]
        public int max_players { get; set; }

        [Column("status")]
        public string status { get; set; } // "Waiting", "Playing"

        public RoomRecord() { }
    }
}
