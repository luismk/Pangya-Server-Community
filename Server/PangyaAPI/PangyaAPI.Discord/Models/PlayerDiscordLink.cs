using System;

namespace PangyaAPI.Discord.Models
{
    public class PlayerDiscordLink
    {
        public ulong DiscordUserId { get; set; }
        public int GameUserId { get; set; }
        public DateTime LinkedAt { get; set; }
    }
}