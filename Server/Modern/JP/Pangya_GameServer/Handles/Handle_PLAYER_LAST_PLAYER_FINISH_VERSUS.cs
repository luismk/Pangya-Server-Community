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
    public class Handle_PLAYER_LAST_PLAYER_FINISH_VERSUS : HandleBase<Player, Packet_EXAMPLE>
    {
        public override async Task Handle()
        { 
            try
            {
                var r = Player.GetGameRoom() ?? throw new exception("[Error] Normal [UID=" + Player.UserInfo.UID + "] tentou finalizar o Versus na sala[NUMERO=" + (Player.UserInfo.Member.RoomID) + "], mas ele nao esta em nenhuma sala. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        1, 0x555001));

                if (r is StrokeBase)
                {
                    if (r.GetSessions().Count > 0)
                    {
                        var lastPlayer = r.GetSessions()?.FirstOrDefault();
                        if (r.FinishGame(lastPlayer, 2))
                        {
                            Player.GetRoom()?.FinishGame();
                        }

                    }
                    else
                    {
                        Player.GetRoom()?.FinishGame();
                    }

                    Player.GetChannel()?.SendListUpdateRooms(Player.GetRoom().GetInfo());

                }
                else
                {
                    _smp.LogManager.Instance.push(new AppMessage("[Handle_PLAYER_LAST_PLAYER_FINISH_VERSUS][ErrorSystem] O jogo da sala[NUMERO=" + (Player.UserInfo.Member.RoomID) + "] nao e do Type Versus. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[Handle_PLAYER_LAST_PLAYER_FINISH_VERSUS][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }
    }
}