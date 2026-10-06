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
    public class Handle_PLAYER_EXIT_LOBBY : HandleBase<Player, Packet_EXAMPLE>
    {
        public override async Task Handle()
        {
            var _channel = Player.GetChannel();
            try
            {

                if (_channel != null)
                    _channel.Lobby.LeaveMultiPlayer(Player);
                else
                {
                    //faz alguma coisa...
                }
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[Handle_PLAYER_EXIT_LOBBY][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

        await Task.CompletedTask;
        }
    }
}
