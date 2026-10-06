using System;
using System.Threading.Tasks;
using Pangya_GameServer.Flags;
using Pangya_GameServer.Models;
using Pangya_GameServer.Server;
using Pangya_GameServer.Session;
using PangyaAPI.Network;
using PangyaAPI.Network.Core;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
namespace Pangya_GameServer.Handles
{
    public class Handle_PLAYER_UN_OR_PAUSE_GAME : HandleBase<Player, Packet_EXAMPLE>
    {
        public override async Task Handle()
        {
           
            try
            {
                var r = Player.GetGameRoom() ?? throw new exception("[Error] Normal [UID=" + Player.UserInfo.UID + "] tentou pausar ou despausar o jogo na sala[NUMERO=" + (Player.UserInfo.Member.RoomID) + "], mas ele nao esta em nenhuma sala. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        1, 0x554001));

                r.RequestUnOrPause(Player, Packet);
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[Handle_PLAYER_UN_OR_PAUSE_GAME][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }
    }
}