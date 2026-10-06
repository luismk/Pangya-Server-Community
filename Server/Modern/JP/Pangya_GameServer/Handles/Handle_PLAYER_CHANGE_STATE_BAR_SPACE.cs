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
    public class Handle_PLAYER_CHANGE_STATE_BAR_SPACE : HandleBase<Player, Packet_EXAMPLE>
    {
        public override async Task Handle()
        {
            try
            {
                var r = Player.GetGameRoom();

                if (r == null)
                {
                    throw new exception("[Lobby.Room::RequestChangeStateBarSpace][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou trocar StateRoom da barra de espaco no jogo na sala[NUMERO=" + (Player.UserInfo.Member.RoomID) + "], nas ele nao esta em nenhuma sala. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        1, 0x5900701));
                }

               r.RequestChangeStateBarSpace(Player, Packet);
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[Lobby.Room::RequestChangeStateBarSpace][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }
    }
}