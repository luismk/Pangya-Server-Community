using System;
using System.Threading.Tasks;
using Pangya_GameServer.Session;
using PangyaAPI.Network;
using PangyaAPI.Network.Core;
using PangyaAPI.Utilities; 
using PangyaAPI.Utilities.Log;
using static Pangya_GameServer.Models.DefineConstants;
using Pangya_GameServer.Server;
namespace Pangya_GameServer.Handles
{
    public class Handle_PLAYER_TRADE_OPEN_EDIT_SHOP : HandleBase<Player, Packet_EXAMPLE>
    {
        public override async Task Handle()
        {
            try
            {
                var r = Player.GetRoom();

                if (r == null)
                {
                    _smp.LogManager.Instance.push(new AppMessage("[Lobby.Room::RequestOpenEditSaleShop][Error][WARNIG] Normal [UID=" + Player.UserInfo.UID + "] Channel[ID=" + Player.GetChannel().getId() + "] tentou abrir ou editar um/o personal ShopRoom para ele, mas nao esta em nenhum sala[RoomID=" + (Player.UserInfo.Member.RoomID) + "]. Hacker ou Bug [Tem que enviar a resposta para o cliente, por que ainda nao esta enviando]", type_msg.CL_FILE_LOG_AND_CONSOLE));
                    return;
                }

                if (!Player.getState())
                {
                    throw new exception("[Room::RequestOpenEditSaleShop] [Error] Player nao esta connectado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                        12, 0));
                }
                if (Packet == null)
                {
                    throw new exception("[Room::RequestOpenEditSaleShop] [Error] Packet is null", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                        12, 0));
                }

                var p = new Packet();
                if (r._tradeShop.RequestChatRoomOpenShopToEdit(Player, p))
                {
                    r.SendBroadCast(p);
                }
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[Lobby.Room::RequestOpenEditSaleShop][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                if (ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) != STDA_ERROR_TYPE.ROOM) throw;
            }
        }
    }
}