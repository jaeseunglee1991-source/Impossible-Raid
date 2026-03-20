using System;
using Supabase.Postgrest.Models;
using Supabase.Postgrest.Attributes;

namespace BossRaid.Models
{
    [Table("profiles")]
    public class UserRecord : BaseModel
    {
        [PrimaryKey("id", false)]
        [Column("id")]
        public string id { get; set; }

        [Column("nickname")]
        public string nickname { get; set; }

        [Column("total_plays")]
        public int total_plays { get; set; }

        [Column("boss_kills")]
        public int boss_kills { get; set; }

        [Column("max_stage_reached")]
        public int max_stage_reached { get; set; }

        public UserRecord() { }
    }
}
