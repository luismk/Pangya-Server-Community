using System;
using System.Threading.Tasks;
using Pangya_GameServer.Feature;
using Pangya_GameServer.Flags;
using Pangya_GameServer.Manager;
using Pangya_GameServer.Models;
using Pangya_GameServer.Repository;
using Pangya_GameServer.Server;
using Pangya_GameServer.Session;
using PangyaAPI.IFF.Handle.JP;
using PangyaAPI.IFF.Regions.JP.Models;
using PangyaAPI.Network;
using PangyaAPI.Network.Core;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using snmdb;
using System;
using System.Threading.Tasks;
namespace Pangya_GameServer.Handles
{
    public class Handle_PLAYER_INVITE : HandleBase<Player, Packet_EXAMPLE>
    {
        public override async Task Handle()
        {
            Packet p = new Packet();
            var m_ci = Player.GetChannel();
            try
            {
                string nickname = Packet.ReadString();
                uint uid = Packet.ReadUInt32();

                var s = GameServer.Instance.FindSessionByNickname(nickname);

                if (s == null || s.UserInfo.UID != uid)
                {
                    throw new exception("[Lobby.Room::RequestInvite][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou convidar o Normal [UID=" + (uid) + ", NICKNAME=" + nickname + "] para Sala[NUMERO=" + (Player.UserInfo.Member.RoomID) + "], mas o Player nao esta nesse canal. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        3000, 23));
                }

                if (s.UserInfo.Member.RoomID != -1)
                {
                    throw new exception("[Lobby.Room::RequestInvite][Warning] Normal [UID=" + Player.UserInfo.UID + "] tentou convidar o Normal [UID=" + (uid) + ", NICKNAME=" + nickname + "] para Sala[NUMERO=" + (Player.UserInfo.Member.RoomID) + "], mas o Player ja esta em outra sala.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        3002, 23));
                }

                if (s.UserInfo.Place != 0)
                {
                    throw new exception("[Lobby.Room::RequestInvite][Warning] Normal [UID=" + Player.UserInfo.UID + "] tentou convidar o Normal [UID=" + (uid) + ", NICKNAME=" + nickname + "] para Sala[NUMERO=" + (Player.UserInfo.Member.RoomID) + "], mas o Player nao pode ser Invite no momento.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        3002, 23));
                }

                var r = Player.GetRoom();

                if (r == null)
                {
                    throw new exception("[Lobby.Room::RequestInvite][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou convidar o Normal [UID=" + (uid) + ", NICKNAME=" + nickname + "] para Sala[NUMERO=" + (Player.UserInfo.Member.RoomID) + "], mas ele nao esta em nenhuma sala para poder convidar. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        3001, 23));
                }

                var ici = r.AddInvited(Player.UserInfo.UID, s);

                m_ci.AddInviteTimeRequest(ici);
                m_ci.Lobby.SendUpdateRoomInfo(r.GetInfo(), 3);

                // Resposta Invite Player
                p.init_plain(0x12F);
                p.WriteUInt16(0); // Ok
                p.WriteUInt32(GameServer.Instance.getUID());
                p.WriteByte(m_ci.getId());
                p.WriteInt16(r.GetRoomId());
                p.WriteUInt32(Player.UserInfo.UID);
                p.WriteString(Player.UserInfo.NickName);
                p.WriteUInt32(s.UserInfo.UID);

                Player.Send(p);

                // Envia o Invite para o Player
                p.init_plain(0x83);
                p.WriteUInt16(0); // OK
                p.WriteUInt32(GameServer.Instance.getUID());
                p.WriteByte(m_ci.getId());
                p.WriteInt16(r.GetRoomId());
                p.WriteUInt32(Player.UserInfo.UID);
                p.WriteString(Player.UserInfo.NickName);
                p.WriteUInt32(s.UserInfo.UID);

                s.Send(p);
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[Lobby.Room::RequestInvite][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Resposta Error
                p.init_plain(0x12F);
                p.WriteUInt16((ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) == STDA_ERROR_TYPE.CHANNEL) ? (ushort)ExceptionError.STDA_SYSTEM_ERROR_DECODE(e.getCodeError()) : (ushort)23);
                Player.Send(p);
            }

        await Task.CompletedTask;
        }
    }
}