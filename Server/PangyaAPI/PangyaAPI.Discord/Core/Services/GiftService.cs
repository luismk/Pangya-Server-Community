using PangyaAPI.Discord.Core.Interfaces;
using PangyaAPI.Discord.Services;

namespace PangyaAPI.Discord.Core.Services
{
    public class GiftService
    {
        private readonly IPlayerDiscordService _discordLink;
        private readonly DiscordService _discordBot;

        public GiftService(IPlayerDiscordService discordLink, DiscordService discordBot)
        {
            _discordLink = discordLink;
            _discordBot = discordBot;
        }

        public void GiveGiftAsync(int gameUserId, string giftName)
        {
            var link = _discordLink.GetLinkAsync(gameUserId);
            if (link == null) return;

            _discordBot.SendMessage(link.DiscordUserId, $"🎁 Você recebeu: {giftName}");
        }
    }
}