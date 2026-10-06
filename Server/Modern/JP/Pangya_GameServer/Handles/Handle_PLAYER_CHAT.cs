using Pangya_GameServer.Flags;
using Pangya_GameServer.Manager;
using Pangya_GameServer.Models;
using Pangya_GameServer.PacketFunc;
using Pangya_GameServer.Server;
using Pangya_GameServer.Session;
using PangyaAPI.IFF.Flags;
using PangyaAPI.IFF.Handle.JP;
using PangyaAPI.Network;
using PangyaAPI.Network.Core;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Pangya_GameServer.Handles
{
    public class Handle_PLAYER_CHAT : HandleBase<Player, Packet_EXAMPLE>
    {
        public override async Task Handle()
        {
            var m_ci = Player.GetChannel();
            try
            {
                string nickname = Packet.ReadPStr();
                string msg = Packet.ReadPStr();

                if (string.IsNullOrEmpty(nickname))
                    throw new exception(" Normal[UID=" + (Player.UserInfo.UID) + "] tentou enviar msg[MESSAGE="
                            + nickname + "], vazio. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 1, 1/*UNKNOWN ERROR*/));

                if (!Tools.Sanitize(nickname))
                    throw new exception(" Normal[UID=" + (Player.UserInfo.UID) + "] tentou enviar msg[MESSAGE="
                            + nickname + "], tentativa de inject. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 1, 1/*UNKNOWN ERROR*/));

                if (string.IsNullOrEmpty(msg))
                    throw new exception(" Normal[UID=" + (Player.UserInfo.UID) + "] tentou enviar msg[MESSAGE="
                            + msg + "], vazio. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 1, 1/*UNKNOWN ERROR*/));

                if (m_ci != null)
                {
                    var gmList = GameServer.Instance.FindAllGM();

                    if (gmList.Any())
                    {
                        string msg_gm = "\\5" + Player.UserInfo.NickName + ": '" + msg + "'";
                        string from = "\\1[Channel=" + m_ci.getName() + ", \\1ROOM=" + Player.UserInfo.Member.RoomID + "]";

                        int index = from.IndexOf(' ');
                        if (index != -1)
                            from = from.Substring(0, index) + " \\1" + from.Substring(index + 1);

                        foreach (Player el in gmList)
                        {
                            if (((el.m_gi.channel > 0 && el.UserInfo.Channel == m_ci.getId()) || el.m_gi.whisper.IsTrue() || el.m_gi.isOpenPlayerWhisper(Player.UserInfo.UID))
                                && (el.UserInfo.Channel != Player.UserInfo.Channel || el.UserInfo.Member.RoomID != Player.UserInfo.Member.RoomID))
                            {
                                el.Send(Handle_PACKET_RESPONSE.pacote040(from, msg_gm, 0));
                            }
                        }
                    }

                    // 5. Executa comandos e envia a mensagem para sala ou lobby
                    var comando = new Queue<string>(msg.Split(' '));

                    if (Player.UserInfo.Member.RoomID != -1)
                    {
                        var r = Player.GetRoom();

                        r?.SendBroadCast(Handle_PACKET_RESPONSE.pacote040(Player.UserInfo.NickName, msg, ((Player.UserInfo.UserCapabilities.IsGameMaster) ? eChatMsg.CHAT_GM : 0)));
                    }
                    else
                    {
                        var flag = Player.UserInfo.UserCapabilities.IsGameMaster ? eChatMsg.CHAT_GM : 0;
                        m_ci.SendBroadcast(Handle_PACKET_RESPONSE.pacote040(Player.UserInfo.NickName, msg, flag));
                    }
                    if (Player.UserInfo.UserCapabilities.IsGameMaster)
                        m_ci.CommandByChat(Player, comando);
                }
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[Handle_PLAYER_CHAT][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            await Task.CompletedTask;
        }
    }
}
