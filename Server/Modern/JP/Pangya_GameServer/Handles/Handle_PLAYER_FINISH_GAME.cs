using Pangya_GameServer.Flags;
using Pangya_GameServer.Session;
using PangyaAPI.Network;
using PangyaAPI.Network.Core;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
namespace Pangya_GameServer.Handles
{
    public class Handle_PLAYER_FINISH_GAME : HandleBase<Player, Packet_EXAMPLE>
    {
        public override async Task Handle()
        {
            try
            {
                var r = Player.GetGameRoom() ?? throw new exception("[Error] Normal [UID=" + Player.UserInfo.UID + "] tentou finalizar o jogo na sala[NUMERO=" + (Player.UserInfo.Member.RoomID) + "], mas ele nao esta em nenhum sala. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        1, 0x5902201));

                if (r.RequestFinishGame(Player, Packet))
                {
                    Player.GetRoom().FinishGame();
                    if (r.GetTipo() != RoomTypeFlags.PRACTICE && r.GetTipo() != RoomTypeFlags.GRAND_ZODIAC_PRACTICE)
                    {
                        Player.GetChannel()?.SendListUpdateRooms(r.GetRoomInfo());
                    }
                }
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[Handle_PLAYER_FINISH_GAME][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }
    }
}