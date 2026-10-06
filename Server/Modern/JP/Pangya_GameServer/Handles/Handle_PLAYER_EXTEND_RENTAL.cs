using Pangya_GameServer.Feature;
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
using System.Threading.Tasks;

namespace Pangya_GameServer.Handles
{
    public class Handle_PLAYER_EXTEND_RENTAL : HandleBase<Player, Packet_EXAMPLE>
    {
        public override async Task Handle()
        {
            Packet p = new Packet();

            try
            {
                int item_id = Packet.ReadInt32();

                if (item_id <= 0)
                {
                    throw new exception("[Lobby::RequestExtendRental][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou extend rental, mas o item[ID=" + (item_id) + "] is invalid. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        350, 5200351));
                }

                var pWi = Player.Inventory.FindWarehouseItemById(item_id);

                if (pWi == null)
                {
                    throw new exception("[Lobby::RequestExtendRental][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou extend rental, mas o Player nao tem o item[ID=" + (item_id) + "]. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        351, 5200352));
                }

                if (sIff.Instance.getItemGroupIdentify(pWi._typeid) != IFF_GROUP.PART)
                {
                    throw new exception("[Lobby::RequestExtendRental][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou extend rental, mas o item[TYPEID=" + (pWi._typeid) + ", ID=" + (pWi.id) + "] nao é um Part. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        352, 5200353));
                }

                var part = sIff.Instance.findPart(pWi._typeid);

                if (part == null)
                {
                    throw new exception("[Lobby::RequestExtendRental][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou extender um rental Item[TYPEID=" + (pWi._typeid) + ", ID=" + (pWi.id) + "] que nao esta no IFF_STRUCT do server. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        353, 5200354));
                }

                if (part.valor_rental <= 0)
                {
                    throw new exception("[Lobby::RequestExtendRental][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou extender um rental Item[TYPEID=" + (pWi._typeid) + ", ID=" + (pWi.id) + "] que nao é um rental no IFF_STRUCT do server. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        354, 5200355));
                }

                pWi.end_date_unix_local = (uint)UtilTime.GetLocalTimeAsUnix() + (7 * 24 * 3600);

                // Convert to UTC to send to client
                pWi.end_date = UtilTime.UnixTimeConvert((long)pWi.end_date_unix_local);

                var end_date = UtilTime.FormatDateLocal(pWi.end_date_unix_local);

                // Cmd Extend Rental + 7 dias no DB
                NormalManagerDB.Instance.add(5, new CmdExtendRental(Player.UserInfo.UID, pWi.id, end_date), null, null);

                // Tira os pangs do valor de renovar o Rental Item
                Player.UserInfo.consomePang(part.valor_rental);

                // Verifica se o Parts já tem um item update do parts
                var v_it = Player.Inventory.FindUpdateItemById(pWi.id);

                if (v_it.Any())
                {
                    foreach (var el in v_it)
                    {
                        if (el.Value.type == UpdateItem.UI_TYPE.WAREHOUSE)
                        {
                            // Tira esse Update Item do map
                            Player.Inventory.UpdateItems.Remove(el.Key);
                        }
                    }
                }

                // Log
                _smp.LogManager.Instance.push(new AppMessage("[Rental::Extend][Sucess] Normal [UID=" + Player.UserInfo.UID + "] extendeu o Rental Item[TYPEID=" + (pWi._typeid) + ", ID=" + (pWi.id) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Att Pang no Jogo
                p.init_plain(0xC8);

                p.WriteUInt64(Player.UserInfo.Statistics.pang);
                p.WriteUInt64(part.valor_rental);

                Player.Send(p);

                // Att Rental Item no Jogo
                p.init_plain(0x18F);

                p.WriteByte(0); // OK

                p.WriteUInt32(pWi._typeid);
                p.WriteInt32(pWi.id);

                Player.Send(p);
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[Lobby::RequestExtendRental][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x18F);

                p.WriteByte(1); // Error

                Player.Send(p);
            }
        }
    }
}