using Discord;
using Discord.WebSocket;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PangyaAPI.Discord.Services
{
    public class DiscordService
    {
        private DiscordSocketClient _client;
        private ulong _devChannelId;
        private string _token;

        public DiscordService()
        { }


        public void SetIdChannel(ulong devChannelId)
        {
            _devChannelId = devChannelId;
        }

        public void SetToken(string token)
        {
            _token = token;
        }

        public void StartBlocking()
        {
            _client = new DiscordSocketClient();
            _client.Log += (msg) => { Console.WriteLine(msg); return Task.CompletedTask; };

            _client.MessageReceived += async (msg) =>
            {
                if (msg.Author.IsBot) return;
                // Pode expor eventos para o GameServer assinar
            };

            _client.LoginAsync(TokenType.Bot, _token).GetAwaiter().GetResult();
            _client.StartAsync().GetAwaiter().GetResult();

            while (true)
                Thread.Sleep(1000);
        }

        public void StartBlocking(ulong devChannelId, string token)
        {
            SetIdChannel(devChannelId);
            SetToken(token);

            _client = new DiscordSocketClient();
            _client.Log += (msg) => { Console.WriteLine(msg); return Task.CompletedTask; };

            _client.MessageReceived += async (msg) =>
            {
                if (msg.Author.IsBot) return;
                // Pode expor eventos para o GameServer assinar
            };

            _client.LoginAsync(TokenType.Bot, _token).GetAwaiter().GetResult();
            _client.StartAsync().GetAwaiter().GetResult();

            while (true)
                Thread.Sleep(1000);
        }

        public void SendMessage(ulong userId, string message)
        {
            var user = _client.GetUser(userId);
            if (user != null)
                user.SendMessageAsync(message).GetAwaiter().GetResult();
        }

        public void SendChannel(string message)
        {
            var channel = _client.GetChannel(_devChannelId) as IMessageChannel;
            if (channel != null)
                channel.SendMessageAsync(message).GetAwaiter().GetResult();
        }
    }
}