using System;
using Supabase.Postgrest.Models;
using Supabase.Postgrest.Attributes;

namespace BossRaid.Models
{
    /// <summary>
    /// 실시간 채팅 메시지를 위한 데이터 모델입니다.
    /// </summary>
    public class ChatMessage
    {
        public string SenderId { get; set; }
        public string SenderNickname { get; set; }
        public string Content { get; set; }
        public DateTime Timestamp { get; set; }

        public ChatMessage() { }
        
        public ChatMessage(string senderId, string nickname, string content)
        {
            SenderId = senderId;
            SenderNickname = nickname;
            Content = content;
            Timestamp = DateTime.Now;
        }
    }
}
