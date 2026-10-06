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
    public class Handle_PLAYER_LEAVE_CHIP_IN_PRACTICE : HandleBase<Player, Packet_EXAMPLE>
    {
        public override async Task Handle()
        {
            try
            {
                var r = Player.GetGameRoom() ?? throw new exception("[Error] Normal [UID=" + Player.UserInfo.UID + "]  tentou sair do Chip-in Practice na sala[NUMERO=" + (Player.UserInfo.Member.RoomID) + "], mas ele nao esta em nenhum sala. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        1, 0x6207701));


                if (r.GetTipo() != RoomTypeFlags.GRAND_ZODIAC_PRACTICE)
                {
                    throw new exception("[Error] Normal[UID=" + Player.UserInfo.UID + "] tentou sair do Chip-in Practice na sala[NUMERO=" + r.GetRoomId() + "], mas TIPO=" + Convert.ToString((ushort)r.GetTipo()) + " de jogo da sala nao é Chip-in Practice", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                        2, 0x6701002));
                }

                // Acabou o tempo /*Sai do Chip-in Practice*/
                if (r.FinishGame(Player, 2))
                {
                    Player.GetRoom().FinishGame();
                } 
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[Handle_PLAYER_LEAVE_CHIP_IN_PRACTICE][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }
    }
}