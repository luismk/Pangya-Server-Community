using Pangya_MessengerServer.Server;
using Pangya_MessengerServer.Session;
using PangyaAPI.Network;
using PangyaAPI.Network.Core;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using PangyaAPI.Utilities.Models;

namespace Pangya_MessengerServer.Handles
{
    public class Handle_PLAYER_CHAT_GUILD : HandleBase<Player, Packet_EXAMPLE>//<Packet_PLAYER_CHAT_GUILD, MPlayer>
    {
        public override async Task Handle()
        {
            var p = new Packet();

            try
            {
                var msg = Packet.ReadString();

                if (Player.UserInfo.GuildIndex == 0)
                    throw new exception("[MessengerService::requestChatGuild][Error] player[UID=" + (Player.UserInfo.UID) + "] tentou enviar Message[MSG="
                            + msg + "] para o Chat da Guild[UID=" + (Player.UserInfo.GuildIndex) + "], mas o player nao esta em uma Guild. Hacker ou Bug",
                            ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MESSAGE_SERVER, 1, 0x5200401));

                if (string.IsNullOrEmpty(msg)) 
                    throw new exception("[MessengerService::requestChatGuild][Error] player[UID=" + (Player.UserInfo.UID) + "] tentou enviar Message[MSG="
                            + msg + "] para o Chat da Guild[UID=" + (Player.UserInfo.GuildIndex) + "], mas a msg is empty. Hacker ou Bug",
                            ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MESSAGE_SERVER, 2, 0x5200402));

                var gm = MessengerServer.Instance.FindAllGM();

                if (gm.Count > 0)
                {
                    var guild_name = (Player.UserInfo.GuildName);
                    var index = -1;

                    while ((index = guild_name.IndexOf(' ', (index != -1 ? index + 1 : 0))) != -1)
                        guild_name = guild_name.Remove(index, 1).Insert(index, " \\2");

                    var msg_gm = "[\\2" + guild_name + "\\0]\\5>" + (Player.UserInfo.NickName) + ": '" + msg + "'";

                    foreach (Player el in gm)
                    {
                        if (el.UserInfo.UID != Player.UserInfo.UID && el.UserInfo.GuildIndex != Player.UserInfo.GuildIndex)
                        {
                            p.init_plain(0x40);
                            p.Write((byte)0);
                            p.WriteString("CHAT");   // Nickname/Tag
                            p.WriteString(msg_gm);      // Message
                            el.Send(p);
                        }
                    }
                }

                // Log Interno original
                _smp.LogManager.Instance.push(new AppMessage("[ChatGuild][Log] player[UID=" + (Player.UserInfo.UID) + "] enviu Message[MSG=" + msg + "] no Chat da Guild[UID="
                        + (Player.UserInfo.GuildIndex) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Resposta para send chat to Guild (Protocolo 0x30 / 0x113)
                p.init_plain(0x30);
                p.Write((ushort)0x113); // Sub packet Id
                p.Write(Player.UserInfo.UID);
                p.WriteString(Player.UserInfo.NickName);
                p.WriteString(msg);
                p.Write((byte)1); // Identificador de Chat Guild

                // Envia para o próprio player conforme original
                Player.Send(p);

                // Broadcast para todos os membros da Guild online
                //passo null, para iniciar por lá...
                MessengerServer.Instance.FriendBroadcast(null, Player, p); 
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[MessengerService::requestChatGuild][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x30);
                p.Write((ushort)0x113);
                p.Write((int)-1); // Código de Erro
                Player.Send(p);
            }
        }
    }
}
