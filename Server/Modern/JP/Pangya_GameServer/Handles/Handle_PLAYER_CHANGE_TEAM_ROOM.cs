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
    public class Handle_PLAYER_CHANGE_TEAM_ROOM : HandleBase<Player, Packet_EXAMPLE>
    {
        public override async Task Handle()
        { 
            Packet p = new();

            try
            {

                var r = (Player.GetRoom()) ?? throw new exception("[Lobby.Room::RequestChangePlayerTeamRoom][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou trocar de Team(time) na sala[NUMERO=" + (Player.UserInfo.Member.RoomID) + "], mas a sala nao existe. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        10, 0));

                byte team = Packet.ReadByte();

                PlayerRoomInfo pPri = r.GetPlayerInfo(Player);

                if (pPri == null)
                {
                    throw new exception("[Room::RequestChangeTeam] [Error] Normal[UID=" + Player.UserInfo.UID + "] tentou trocar o Team(time) na sala[NUMERO=" + r.GetRoomId() + "], mas a sala nao tem o info do Player. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                        1505, 0));
                }

                if (r.TeamCount() < 2)
                {
                    throw new exception("[Room::RequestChangeTeam] [Error] Normal[UID=" + Player.UserInfo.UID + "] tentou trocar o Team(time) na sala[NUMERO=" + r.GetRoomId() + "], mas a sala nao tem teans(times) suficiente. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                        1506, 0));
                }

                // Sai do outro Team(time) se ele estiver
                try
                {

                    r.DeletePlayerTeam(Player, 3);

                }
                catch (exception e)
                {

                    _smp.LogManager.Instance.push(new AppMessage("[Room::RequestChangeTeam][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                }

                // Add o Player ao (Team)time
                r.AddPlayerTeam(Player, team);

                pPri.State.Team = team;

                r.UpdatePlayerInfo(Player);


                p = new Packet((ushort)0x7D);

                p.WriteInt32(Player.ConnectionID);

                p.WriteByte(team);

                r.SendBroadCast(p);
            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[Room::RequestChangeTeam][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }
    }
}