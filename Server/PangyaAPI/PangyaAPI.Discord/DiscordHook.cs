using PangyaAPI.Discord.Services;
using PangyaAPI.Utilities;
using System.Threading;

namespace PangyaAPI.Discord
{
    public class DiscordHook : DiscordService
    {
        public void SendUserMessage(ulong gameUserId, string giftName)
        {
            // GiftService interno usa _discord
            SendMessage(gameUserId, giftName);
        }


        public void init(string token, ulong devChannelId)
        {
            SetIdChannel(devChannelId);
            SetToken(token);

            // Rodando o bot em thread interna
            var thread = new Thread(() =>
            {
                this.StartBlocking();
            })
            {
                IsBackground = true
            };
            thread.Start();
        }
        
public void SendDevAlert(string message)
        {
            SendChannel(message);
        }
    }

    public class sDiscordDev : Singleton<DiscordHook>
    { }
    public class sDiscordGame : Singleton<DiscordHook>
    { }
    public class sDiscordException : Singleton<DiscordHook>
    { }
}
