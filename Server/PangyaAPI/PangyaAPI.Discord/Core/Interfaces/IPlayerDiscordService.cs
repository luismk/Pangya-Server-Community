using PangyaAPI.Discord.Models;
using System.Threading;
using System.Threading.Tasks;

namespace PangyaAPI.Discord.Core.Interfaces
{
    public interface IPlayerDiscordService
    {
        Thread LinkAccountAsync(int gameUserId, ulong discordUserId);
        PlayerDiscordLink GetLinkAsync(int gameUserId);
    }
}