using Pangya_GameServer.Feature;
using Pangya_GameServer.Manager;
using Pangya_GameServer.Models;
using Pangya_GameServer.Repository;
using Pangya_GameServer.Session;
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
    public class Handle_PLAYER_PAY_CADDIE_HOLY_DAY : HandleBase<Player, Packet_EXAMPLE>
    {
        public override async Task Handle()
        {
            Packet p = new();

            try
            {
                int caddie_id = Packet.ReadInt32();

                if (caddie_id <= 0)
                {
                    throw new exception("[Lobby::RequestPayCaddieHolyDay][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou pagar as ferias do Caddie[ID=" + (caddie_id) + "], mas o caddie_id é invalido. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        1, 0x6100101));
                }

                var pCi = Player.Inventory.FindCaddieById(caddie_id);

                if (pCi == null)
                {
                    throw new exception("[Lobby::RequestPayCaddieHolyDay][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou pagar as ferias do Caddie[ID=" + (caddie_id) + "], mas o ele nao possui esse Caddie. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        2, 0x6100102));
                }

                var caddie = sIff.Instance.findCaddie(pCi._typeid);

                if (caddie == null)
                {
                    throw new exception("[Lobby::RequestPayCaddieHolyDay][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou pagar as ferias do Caddie[TYPEID=" + (pCi._typeid) + ", ID=" + (pCi.id) + "], mas nao tem esse caddie no IFF_STRUCT do Server. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        3, 0x6100103));
                }

                if ((!caddie.Shop.flag_shop.IsCash && caddie.valor_mensal <= 0) || pCi.rent_flag != 2)
                {
                    throw new exception("[Lobby::RequestPayCaddieHolyDay][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou pagar as ferias do Caddie[TYPEID=" + (pCi._typeid) + ", ID=" + (pCi.id) + "], mas nao é um caddie valido para pagar as verias. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        4, 0x6100104));
                }

                if (caddie.valor_mensal > (long)Player.UserInfo.Statistics.pang)
                {
                    throw new exception("[Lobby::RequestPayCaddieHolyDay][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou pagar as ferias do Caddie[TYPEID=" + (pCi._typeid) + ", ID=" + (pCi.id) + "], mas o ele nao tem pangs suficiente[value=" + (Player.UserInfo.Statistics.pang) + ", Request=" + (caddie.valor_mensal) + "] para pagar as ferias do caddie. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        5, 0x6100105));
                }

                // UPDATE ON SERVER

                // Date
                var end_date_unix = UtilTime.GetSystemTimeAsUnix() + (30 * 24 * 3600); // TO STRING DATE

                // Convert para System Time novamente
                pCi.end_date = (UtilTime.UnixToSystemTime(end_date_unix));

                // Update Caddie End Date Unix
                pCi.updateEndDate();
                 
               Player.UserInfo.consomePang(caddie.valor_mensal);

                // UPDATE ON DB
                NormalManagerDB.Instance.add(20, new CmdPayCaddieHolyDay(Player.UserInfo.UID, pCi.id, UtilTime.FormatDate(pCi.end_date)), null, null);

                // Verifica se o Caddie já tem um item update
                var v_it = Player.Inventory.FindUpdateItemById(pCi.id);

                if (v_it != null && v_it.Count > 0)
                {
                    foreach (var el in v_it.ToList())
                    {
                        if (el.Value.type == UpdateItem.UI_TYPE.CADDIE)
                        {
                            // Tira esse Update Item do map
                            Player.Inventory.UpdateItems.Remove(el.Key);
                        }
                    }
                }

                // Log
                _smp.LogManager.Instance.push(new AppMessage("[PayCaddieHolyDay][Sucess] Normal [UID=" + Player.UserInfo.UID + "] pagou as ferias do Caddie[TYPEID=" + (pCi._typeid) + ", ID=" + (pCi.id) + ", PRICE=" + (caddie.valor_mensal) + "] ate " + UtilTime.FormatDate(pCi.end_date), type_msg.CL_FILE_LOG_AND_CONSOLE));

                // UPDATE ON GAME
                p.init_plain(0x93);

                p.WriteByte(2); // OK
                p.WriteInt32(pCi.id);
                p.WriteUInt64(Player.UserInfo.Statistics.pang);

                Player.Send(p);

            }
            catch (exception e)
            {
               _smp. LogManager.Instance.push(new AppMessage("[Lobby::RequestPayCaddieHolyDay][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x93);
                p.WriteByte(1); // Error

                Player.Send(p);
            }
        }
    }
}