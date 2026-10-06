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
    public class Handle_PLAYER_PLAYER_REPORT_CHAT_GAME : HandleBase<Player, Packet_EXAMPLE>
    {
        public override async Task Handle()
        {
            try
            {
                var r = Player.GetGameRoom() ?? throw new exception("[Error] Normal [UID=" + Player.UserInfo.UID + "] nao esta em nenhum sala[NUMERO=" + (Player.UserInfo.Member.RoomID) + "] para reportar o char da sala. Hacker ou Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        0x580200, 0));
                r.RequestPlayerReportChatGame(Player, Packet);
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[Handle_PLAYER_PLAYER_REPORT_CHAT_GAME][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if (ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) != STDA_ERROR_TYPE.ROOM)
                {
                    throw;
                }
            }
        }
    }
}