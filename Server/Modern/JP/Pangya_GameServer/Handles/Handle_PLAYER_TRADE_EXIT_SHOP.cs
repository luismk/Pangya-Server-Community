using Pangya_GameServer.Session;
using PangyaAPI.Network;
using PangyaAPI.Network.Core;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
namespace Pangya_GameServer.Handles
{
    public class Handle_PLAYER_TRADE_EXIT_SHOP : HandleBase<Player, Packet_EXAMPLE>
    {
        public override async Task Handle()
        {
           
            var m_ci =  Player.GetChannel();

            try
            {
                var r = Player.GetRoom();

                if (r != null)
                {
                   r._tradeShop.RequestChatRoomCloseViewShop(Player, Packet.ReadUInt32());
                }
                else
                {
                    // não aqui mas no else tem que retornar erro para o cliente, que ele esta tentando Fechar um Personal Shop, mas ele nao esta em nenhum sala
                    // Isso é Hacker ou Bug
                    _smp.LogManager.Instance.push(new AppMessage("[Handle_PLAYER_TRADE_EXIT_SHOP][Error][WARNIG] Normal [UID=" + Player.UserInfo.UID + "] tentou fechar o personal ShopRoom de outro Player. mas nao esta em nenhum sala[RoomID=" + (Player.UserInfo.Member.RoomID) + "]. Hacker ou Bug [Tem que enviar a resposta para o cliente, por que ainda nao esta enviando]", type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[Handle_PLAYER_TRADE_EXIT_SHOP][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if (ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) != STDA_ERROR_TYPE.ROOM)
                {
                    throw;
                }
            }
        }
    }
}