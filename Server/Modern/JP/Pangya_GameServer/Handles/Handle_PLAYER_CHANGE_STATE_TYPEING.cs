using System;
using System.Threading.Tasks;
using PangyaAPI.Utilities.Log;
using Pangya_GameServer.Session;
using PangyaAPI.Network;
using PangyaAPI.Network.Core;
using PangyaAPI.Utilities;
using Pangya_GameServer.Server;

namespace Pangya_GameServer.Handles
{
    public class Handle_PLAYER_CHANGE_STATE_TYPEING : HandleBase<Player, Packet_EXAMPLE>
    {
        public override async Task Handle()
        { 
            try
            {
                var r = Player.GetGameRoom();

                if (r == null)
                {
                    throw new exception("[Error] Normal [UID=" + Player.UserInfo.UID + "] tentou mudar estado de escrevendo icon no jogo na sala[NUMERO=" + (Player.UserInfo.Member.RoomID) + "], mas ele nao esta em nenhuma sala. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        1, 0x5901101));
                }

               r.RequestChangeStateTypeing(Player, Packet);
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[Handle_PLAYER_CHANGE_STATE_TYPEING][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }
    }
}