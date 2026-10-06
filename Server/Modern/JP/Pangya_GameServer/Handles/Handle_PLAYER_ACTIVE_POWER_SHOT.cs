using Pangya_GameServer.Feature;
using Pangya_GameServer.Manager;
using Pangya_GameServer.Models;
using Pangya_GameServer.Repository;
using Pangya_GameServer.Server;
using Pangya_GameServer.Session;
using PangyaAPI.Network;
using PangyaAPI.Network.Core;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using snmdb;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Pangya_GameServer.Handles
{
    public class Handle_PLAYER_ACTIVE_POWER_SHOT : HandleBase<Player, Packet_EXAMPLE>
    {
        public override async Task Handle()
        {

            try
            {
                var r = Player.GetGameRoom() ?? throw new exception("[Handle_PLAYER_ACTIVE_POWER_SHOT][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou ativar power shot, mas a sala [NUMERO=" + Player.UserInfo.Member.RoomID + "] não foi encontrada. Hacker ou Bug",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 1, 0x5900801));
                
                r.RequestActivePowerShot(Player, Packet);
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[Handle_PLAYER_ACTIVE_POWER_SHOT][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            await Task.CompletedTask;
        }
    }
}