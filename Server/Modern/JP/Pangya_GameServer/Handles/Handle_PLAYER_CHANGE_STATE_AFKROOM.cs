using System;
using System.Threading.Tasks;
using Pangya_GameServer.Feature;
using Pangya_GameServer.Flags;
using Pangya_GameServer.Manager;
using Pangya_GameServer.Models;
using Pangya_GameServer.PacketFunc;
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
namespace Pangya_GameServer.Handles
{
    public class Handle_PLAYER_CHANGE_STATE_AFKROOM : HandleBase<Player, Packet_EXAMPLE>
    {
        public override async Task Handle()
        {
            var m_ci = Player.GetChannel();
            try
            {
                byte state = Packet.ReadByte();

                var r = Player.GetRoom();

                if (r == null)
                {
                    throw new exception("[Lobby.Room::RequestChangePlayerStateAFKRoom][Error] sala[NUMERO=" + (Player.UserInfo.Member.RoomID) + "] nao existe.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        10, 0));
                }

                PlayerRoomInfo pri = r.GetPlayerInfo(Player);

                PlayerLobbyInfo pci = m_ci.GetPlayerInfo(Player);

                if (pri == null)
                {
                    throw new exception("[Lobby.Room::RequestChangePlayerStateAFKRoom][Error] nao tem o info do Player na sala[NUMERO=" + (Player.UserInfo.Member.RoomID) + "].", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        11, 0));
                }

                if (pci == null)
                {
                    throw new exception("[Lobby.Room::RequestChangePlayerStateAFKRoom][Error] nao tem o info do Player no canal.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        12, 0));
                }

                pci.State.Sleep = pri.State.Sleep = state;

                Packet p = new Packet(0x8E);
                p.WriteInt32(Player.ConnectionID);
                p.WriteByte(state);

                r.SendBroadCast(p);

                m_ci.Lobby.SendBroadCast(Handle_PACKET_RESPONSE.MakePlayerLobby(new List<PlayerLobbyInfo>() { (pci == null) ? new PlayerLobbyInfo() : pci }, 3));
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[requestChangePlayerStateAFKRoom][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

        await Task.CompletedTask;
        }
    }
}