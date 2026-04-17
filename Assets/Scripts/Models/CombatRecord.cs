using System;
using Supabase.Postgrest.Models;
using Supabase.Postgrest.Attributes;

namespace BossRaid.Models
{
    [Serializable]
    [Table("combat_records")]
    public class CombatRecord : BaseModel
    {
        [Column("user_id")]
        public string userId { get; set; }
        
        [Column("nickname")]
        public string nickname { get; set; }
        
        [Column("role")]
        public string role { get; set; }
        
        [Column("total_damage")]
        public float totalDamage { get; set; }
        
        [Column("total_healing")]
        public float totalHealing { get; set; }
        
        [Column("total_damage_taken")]
        public float totalDamageTaken { get; set; }
        
        [Column("aggro_duration")]
        public float aggroDuration { get; set; }
        
        [Column("is_mvp")]
        public bool isMvp { get; set; }

        public CombatRecord() { }

        public CombatRecord(string id, string nick, string r)
        {
            userId = id;
            nickname = nick;
            role = r;
            totalDamage = 0;
            totalHealing = 0;
            totalDamageTaken = 0;
            aggroDuration = 0;
            isMvp = false;
        }
    }
}
