using System;
using System.Threading.Tasks;
using Pangya_GameServer.Flags;
using Pangya_GameServer.Server;
using Pangya_GameServer.Session;
using PangyaAPI.Network;
using PangyaAPI.Network.Core;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
namespace Pangya_GameServer.Handles
{
    public class Handle_PLAYER_USE_TICKET_REPORT : HandleBase<Player, Packet_EXAMPLE>
    {
        public override async Task Handle()
        {
            if (!Player.getState())
            {
                throw new exception("[Error] Player nao esta connectado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }
            if (Packet == null)
            {
                throw new exception("[Error] Packet is null", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }

            try
            {
                var gameRoom = Player.GetGameRoom() ?? throw new exception("[Error] Normal[UID=" + Player.UserInfo.UID + "] tentou usar Ticket Report no Tourney no jogo na sala[NUMERO=" + Player.GetRoom()?.GetRoomId() + "], mas a sala nao tem nenhum jogo inicializado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                        1, 0x6301001));

                gameRoom.RequestUseTicketReport(Player, Packet);
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[Handle_PLAYER_USE_TICKET_REPORT][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }
    }
}