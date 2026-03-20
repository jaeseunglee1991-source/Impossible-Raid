using System;
using System.Collections.Generic;
using Supabase.Postgrest.Models;
using Supabase.Postgrest.Attributes;

namespace BossRaid.Models
{
    [Table("rooms")]
    public class RoomData : BaseModel
    {
        [PrimaryKey("id", false)]
        public string id { get; set; }

        [Column("title")]
        public string title { get; set; }

        [Column("password")]
        public string password { get; set; }

        [Column("stages")]
        public List<int> stages { get; set; }

        [Column("status")]
        public string status { get; set; }

        [Column("creator_id")]
        public string creator_id { get; set; }

        [Column("participants")]
        public string participants { get; set; }

        [Column("created_at")]
        public DateTime created_at { get; set; }
    }
}
