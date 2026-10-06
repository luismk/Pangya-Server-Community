using System;
using System.Threading.Tasks;
using Pangya_GameServer.Flags;
using Pangya_GameServer.Roms.GameBase.Modes;
using Pangya_GameServer.Server;
using Pangya_GameServer.Session;
using PangyaAPI.Network;
using PangyaAPI.Network.Core;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
namespace Pangya_GameServer.Handles
{
    public class Handle_PLAYER_LEAVE_PRACTICE : HandleBase<Player, Packet_EXAMPLE>
    {
        public override async Task Handle()
        { 
            try
            {
                var game = Player.GetGameRoom();

                if (game == null)
                {
                    throw new exception("[Error] Normal [UID=" + Player.UserInfo.uid + "]  tentou sair do practice na sala[NUMERO=" + (Player.UserInfo.Member.RoomID) + "], mas ele nao esta em nenhuma sala. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        1, 0x6202001));
                }

                if (game.GetTipo() != RoomTypeFlags.PRACTICE)
                {
                    throw new exception("[Error] Normal [UID=" + Player.UserInfo.uid + "]  tentou sair do practice na sala[NUMERO=" + (Player.UserInfo.Member.RoomID) + ", TIPO=" + (game.GetTipo()) + "], mas a sala nao é um Type de sala do practice. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        2, 0x6202002));
                }

                if (game is not TourneyBase)
                {
                    throw new exception("[Error] Normal [UID=" + Player.UserInfo.uid + "]  tentou sair do practice na sala[NUMERO=" + (Player.UserInfo.Member.RoomID) + ", TIPO=" + (game.GetTipo()) + "], mas a sala nao é um Type de sala do practice. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        2, 0x6202002));
                }

                // Acabou o tempo /*Sai do Practice*/
                ((TourneyBase)game).GameTimeIsOver(); 
                
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[Handle_PLAYER_LEAVE_PRACTICE][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }
    }
}