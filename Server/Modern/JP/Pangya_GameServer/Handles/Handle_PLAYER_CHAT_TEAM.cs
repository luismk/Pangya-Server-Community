using Pangya_GameServer.Flags;
using Pangya_GameServer.Models;
using Pangya_GameServer.Server;
using Pangya_GameServer.Session;
using PangyaAPI.Network;
using PangyaAPI.Network.Core;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using System;
using System.Threading.Tasks;

namespace Pangya_GameServer.Handles
{
    public class Handle_PLAYER_CHAT_TEAM : HandleBase<Player, Packet_EXAMPLE>
    {
        public override async Task Handle()
        { 

            if (Packet == null)
            {
                throw new exception("[Error] Packet is null", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }

            var p = new Packet();

            try
            {
                var r = Player.GetRoom();

                if (r == null)
                {
                    throw new exception("[Error] Normal [UID=" + Player.UserInfo.UID + "] tentou mandar AppMessage no chat do Team na sala[NUMERO=" + (Player.UserInfo.Member.RoomID) + "], mas ele nao esta em nenhuma sala. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        1, 0x5900201));
                }

                var msg = Packet.ReadString();

                // Verifica a mensagem com palavras proibida e manda para o log e bloquea o chat dele
                _smp.LogManager.Instance.push(new AppMessage("[Handle_PLAYER_CHAT_TEAM][Info] Normal[UID=" + Player.UserInfo.UID + ", MESSAGE=" + msg + "]", type_msg.CL_ONLY_FILE_LOG));

                if (string.IsNullOrEmpty(msg))
                {
                    throw new exception("[ERROR] Normal[UID=" + Player.UserInfo.UID + "] tentou mandar messsage[MSG=" + msg + "] no chat do Team na sala[NUMERO=" + r.GetRoomId() + "], mas a msg esta vazia. Hacker ou Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                        2000, 0));
                }

                if (r.GetTipo() !=  RoomTypeFlags.MATCH && r.GetTipo() != RoomTypeFlags.GUILD_BATTLE)
                {
                    throw new exception("[ERROR] Normal[UID=" + Player.UserInfo.UID + "] tentou mandar messsage[MSG=" + msg + "] no chat do Team na sala[NUMERO=" + r.GetRoomId() + "], mas a sala nao é MATCH ou GUILD_BATTLE. Hacker ou Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                        2001, 0));
                }

                if (r.TeamCount() == 0)
                {
                    throw new exception("[ERROR] Normal[UID=" + Player.UserInfo.UID + "] tentou mandar messsage[MSG=" + msg + "] no chat do Team na sala[NUMERO=" + r.GetRoomId() + "], mas a sala nao tem nenhum Team. Hacker ou Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                        2002, 0));
                }

                var pri = r.GetPlayerInfo(Player);

                if (pri == null)
                {
                    throw new exception("Error] Normal[UID=" + Player.UserInfo.UID + "] tentou mandar messsage[MSG=" + msg + "] no chat do Team na sala[NUMERO=" + r.GetRoomId() + "], mas a sala nao tem o info dele. Hacker ou Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                        2003, 0));
                }

                var team = r.GetTeamInfo(pri.State.Team);

                if (team.findPlayerByUID(Player.UserInfo.UID) == null)
                {
                    throw new exception("[ERROR] Normal[UID=" + Player.UserInfo.UID + "] tentou mandar messsage[MSG=" + msg + "] no chat do Team na sala[NUMERO=" + r.GetRoomId() + "], mas ele nao esta no Team que a type de Team dele diz. Hacker ou Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                        2004, 0));
                }

                // LOG GM
                // Envia para todo os GM do server essa AppMessage
                var c = Player.GetChannel();

                if (c != null)
                {

                    var gm = GameServer.Instance.FindAllGM();

                    if (gm.Any())
                    {

                        string msg_gm = "\\5" + (Player.UserInfo.NickName) + ": '" + msg + "'";
                        string from = "\\1[Channel=" + (c.getInfo().name) + ", \\1ROOM=" + Convert.ToString(Player.UserInfo.Member.RoomID) + "][Team" + (!(pri.State.Team == 1) ? "R" : "B") + "]";

                        var index = from.IndexOf(' ');

                        if (index != -1)
                        {
                            from = from.Remove(index, 1).Insert(index, " \\1");
                        }

                        foreach (Player el in gm)
                        {
                            if (((el.m_gi.channel > 0 && el.UserInfo.Channel == c.getInfo().id) || el.m_gi.whisper > 0 || el.m_gi.isOpenPlayerWhisper(Player.UserInfo.UID)) && (el.UserInfo.Channel != Player.UserInfo.Channel || el.UserInfo.Member.RoomID != Player.UserInfo.Member.RoomID || team.findPlayerByUID(el.UserInfo.UID) == null))
                            {

                                // Responde no chat do Player
                                p.init_plain(0x40);

                                p.WriteByte(0);

                                p.WriteString(from); // Nickname

                                p.WriteString(msg_gm); // Message
                                Player.Send(p);
                            }
                        }
                    }
                }
                else 
                {
                    _smp.LogManager.Instance.push(new AppMessage("[Handle_PLAYER_CHAT_TEAM][Warning] Log GM nao encontrou o Channel[ID=" + Convert.ToString((ushort)Player.UserInfo.Channel) + "] no server. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                }

                // Manda AppMessage para o Team da sala
                p.init_plain(0xB0);

                p.WriteString(Player.UserInfo.NickName);
                p.WriteString(msg);

                foreach (var el in team.getPlayers())
                {
                    Player.Send(p);
                }
            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[Handle_PLAYER_CHAT_TEAM][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }
    }
}