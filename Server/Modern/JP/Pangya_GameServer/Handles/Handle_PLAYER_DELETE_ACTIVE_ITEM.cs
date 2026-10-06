using Pangya_GameServer.Feature;
using Pangya_GameServer.Flags;
using Pangya_GameServer.Manager;
using Pangya_GameServer.Models;
using Pangya_GameServer.Repository;
using Pangya_GameServer.Session;
using PangyaAPI.IFF.Flags;
using PangyaAPI.IFF.Handle.JP;
using PangyaAPI.Network;
using PangyaAPI.Network.Core;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using snmdb;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Pangya_GameServer.Models.DefineConstants;

namespace Pangya_GameServer.Handles
{
    public class Handle_PLAYER_DELETE_ACTIVE_ITEM : HandleBase<Player, Packet_EXAMPLE>
    {
        public override async Task Handle()
        {
            Packet p = new Packet();

            try
            {
                uint _typeid = Packet.ReadUInt32();
                uint qntd = Packet.ReadUInt32();

                if (sIff.Instance.getItemGroupIdentify(_typeid) != IFF_GROUP.ITEM)
                {
                    throw new exception("[Lobby::RequestDeleteActiveItem][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou excluir um item[TYPEID=" + (_typeid) + "] que nao pode ser excluido. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        703, 0x5200704));
                }

                var iff_item = sIff.Instance.findItem(_typeid);

                if (iff_item == null)
                {
                    throw new exception("[Lobby::RequestDeleteActiveItem][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou excluir um item[TYPEID=" + (_typeid) + "] que nao pode ser excluido, por que ele nao tem no IFF_STRUCT do Server. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        704, 0x5200705));
                }

                if (sIff.Instance.IsItemEquipable(_typeid) && iff_item.Shop.flag_shop.IsCash)
                {
                    throw new exception("[Lobby::RequestDeleteActiveItem][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou excluir um item[TYPEID=" + (_typeid) + "] que nao pode ser excluido, por que ele é um item equipavel de cash(cookie). Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        705, 0x5200706));
                }

                if (!sIff.Instance.IsItemEquipable(_typeid) && !(iff_item.Shop.flag_shop.IsGift && iff_item.Stats.getSlot[0] > 0))
                {
                    throw new exception("[Lobby::RequestDeleteActiveItem][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou excluir um item[TYPEID=" + (_typeid) + "] que nao pode ser excluido, por que ele é um passive item que nao tem a condicao(giftable) e a quantidade no C[0] para deletar esse item. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        706, 0x5200707));
                }

                var pWi = Player.Inventory.FindWarehouseItemByTypeid(_typeid);

                if (pWi == null)
                {
                    throw new exception("[Lobby::RequestDeleteActiveItem][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou excluir item[TYPEID=" + (_typeid) + "] que ele nao possui. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        700, 0x5200701));
                }

                if (pWi.STDA_C_ITEM_QNTD < (short)qntd)
                {
                    throw new exception("[Lobby::RequestDeleteActiveItem][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou excluir item[TYPEID=" + (_typeid) + "] mas ele nao tem quantidade suficiente[have_qntd=" + (pWi.STDA_C_ITEM_QNTD) + ", req_qntd=" + (qntd) + "]", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        701, 0x5200702));
                }

                stItem item = new();

                item.type = 2;
                item.id = pWi.id;
                item._typeid = pWi._typeid;
                item.qntd = (int)qntd;
                item.STDA_C_ITEM_QNTD = (short)((ushort)qntd * -1);

                // Atualiza ON Server AND Banco de dados
                if (ItemManager.removeItem(item, Player) <= 0)
                {
                    throw new exception("[Lobby::RequestDeleteActiveItem][Error] Normal [UID=" + Player.UserInfo.UID + "] nao conseguiu excluir item[TYPEID=" + (_typeid) + "]", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        702, 0x5200703));
                }

                _smp.LogManager.Instance.push(new AppMessage("[DeleteActiveItem][Sucess] Normal [UID=" + Player.UserInfo.UID + "] excluiu/(Atualizou qntd) item[TYPEID=" + (pWi._typeid) + ", QNTD=" + (qntd) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Atualiza ON Jogo
                p.init_plain(0xC5);

                p.WriteByte(1); // OK

                p.WriteUInt32(pWi._typeid);
                p.WriteUInt32(qntd);
                p.WriteInt32(pWi.id);

                Player.Send(p);

            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[Lobby::RequestDeleteActiveItem][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0xC5);

                p.WriteSByte(-1); // Error

                Player.Send(p);
            }
        }
    }
}