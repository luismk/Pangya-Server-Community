using Pangya_GameServer.Feature;
using Pangya_GameServer.Flags;
using Pangya_GameServer.Manager;
using Pangya_GameServer.Models;
using Pangya_GameServer.Repository;
using Pangya_GameServer.Server;
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
    public class Handle_PLAYER_USE_CARD_SPECIAL : HandleBase<Player, Packet_EXAMPLE>
    {
        public override async Task Handle()
        {
            Packet p = new Packet();

            var m_ci = Player.GetChannel();
            try
            {
                var r = Player.GetRoom();

                uint card_typeid = Packet.ReadUInt32();

                AchievementSystem sys_achieve = new();

                stItem item = new();
                CardEquipInfoEx cei = new();

                if (card_typeid == 0)
                {
                    throw new exception("[Handle_PLAYER_USE_CARD_SPECIAL][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou usar card Special[TYPEID=" + (card_typeid) + "], mas o typeid é invalid.(zero). Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        350, 0x5500351));
                }

                var pCi = Player.Inventory.FindCardByTypeid(card_typeid);

                if (pCi == null)
                {
                    throw new exception("[Handle_PLAYER_USE_CARD_SPECIAL][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou usar card Special[TYPEID=" + (card_typeid) + "], mas ele nao tem o card. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        351, 0x5500352));
                }

                if (pCi.qntd < 1)
                {
                    throw new exception("[Handle_PLAYER_USE_CARD_SPECIAL][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou usar card Special[TYPEID=" + (card_typeid) + "], nao tem quantidade suficiante[value=" + (pCi.qntd) + ", request=1] de card. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        357, 0x5500358));
                }

                var card = sIff.Instance.findCard(pCi._typeid);

                if (card == null || card.ID != pCi._typeid)
                {
                    throw new exception("[Handle_PLAYER_USE_CARD_SPECIAL][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou usar card Special[TYPEID=" + (card_typeid) + "], mas o card nao existe no IFF_STRUCT do Server. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        352, 0x5500353));
                }

                if (sIff.Instance.getItemSubGroupIdentify22(card.ID) != (uint)CARD_SUB_TYPE.T_SPECIAL)
                {
                    throw new exception("[Handle_PLAYER_USE_CARD_SPECIAL][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou usar card Special[TYPEID=" + (card_typeid) + "], tentou usar um card que nao é espacial. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        353, 0x5500354));
                }

                 
                if (r != null && r.CheckPersonalShopItem(Player, pCi.id))
                {
                    throw new exception("[Handle_PLAYER_USE_CARD_SPECIAL][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou usar card Special[TYPEID=" + (pCi._typeid) + ", ID=" + (pCi.id) + "], mas o card esta sendo vendido no Personal ShopRoom dele. Hacker ou Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        1010, 0x5201010));
                }

                item = new stItem();
                item.type = 2;
                item.id = pCi.id;
                item._typeid = pCi._typeid;
                item.qntd = 1;
                item.STDA_C_ITEM_QNTD = (short)(item.qntd * -1);

                cei.index = -1;
                cei.id = (uint)pCi.id;
                cei._typeid = pCi._typeid;
                cei.efeito = card.Effect;
                cei.efeito_qntd = card.EffectValue;
                cei.parts_typeid = 0;
                cei.parts_id = 0;
                cei.use_yn = 1;
                cei.tipo = sIff.Instance.getItemSubGroupIdentify22(pCi._typeid);
                cei.slot = 0;

                switch (card.Effect)
                {
                    case 1: // Exp Value
                        {
                            if ((int)card.EffectValue <= 0)
                            {
                                throw new exception("[Handle_PLAYER_USE_CARD_SPECIAL][ErrorSystem] Normal [UID=" + Player.UserInfo.UID + "] tentou usar card Special[TYPEID=" + (pCi._typeid) + ", ID=" + (pCi.id) + "], mas a quantidade do efeito[TYPE=" + (card.Effect) + ", QNTD=" + (card.EffectValue) + "] é invalida. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                    356, 0x5500357));
                            }

                            if (ItemManager.removeItem(item, Player) <= 0)
                            {
                                throw new exception("[Handle_PLAYER_USE_CARD_SPECIAL][ErrorSystem] Normal [UID=" + Player.UserInfo.UID + "] tentou usar card Special[TYPEID=" + (pCi._typeid) + ", ID=" + (pCi.id) + "], mas nao conseguiu deletar o card. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                    355, 0x5500356));
                            }

                            Player.addExp(card.EffectValue);
                            break;
                        }
                    case 4: // Pang Value
                        {
                            if (card.EffectValue <= 0)
                            {
                                throw new exception("[Handle_PLAYER_USE_CARD_SPECIAL][ErrorSystem] Normal [UID=" + Player.UserInfo.UID + "] tentou usar card Special[TYPEID=" + (pCi._typeid) + ", ID=" + (pCi.id) + "], mas a quantidade do efeito[TYPE=" + (card.Effect) + ", QNTD=" + (card.EffectValue) + "] é invalida. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                    356, 0x5500357));
                            }

                            if (ItemManager.removeItem(item, Player) <= 0)
                            {
                                throw new exception("[Handle_PLAYER_USE_CARD_SPECIAL][ErrorSystem] Normal [UID=" + Player.UserInfo.UID + "] tentou usar card Special[TYPEID=" + (pCi._typeid) + ", ID=" + (pCi.id) + "], mas nao conseguiu deletar o card. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                    355, 0x5500356));
                            }

                            Player.addPang(card.EffectValue);
                            break;
                        }
                    case 17: // Pang Value Sorteio
                        {
                            if (card.EffectValue <= 0)
                            {
                                throw new exception("[Handle_PLAYER_USE_CARD_SPECIAL][ErrorSystem] Normal [UID=" + Player.UserInfo.UID + "] tentou usar card Special[TYPEID=" + (pCi._typeid) + ", ID=" + (pCi.id) + "], mas a quantidade do efeito[TYPE=" + (card.Effect) + ", QNTD=" + (card.EffectValue) + "] é invalida. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                    356, 0x5500357));
                            }

                            if (ItemManager.removeItem(item, Player) <= 0)
                            {
                                throw new exception("[Handle_PLAYER_USE_CARD_SPECIAL][ErrorSystem] Normal [UID=" + Player.UserInfo.UID + "] tentou usar card Special[TYPEID=" + (pCi._typeid) + ", ID=" + (pCi.id) + "], mas nao conseguiu deletar o card. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                    355, 0x5500356));
                            }

                            ulong pang = (ulong)(100 + ((Random.Shared.Next() % Convert.ToInt32(card.EffectValue - 100)) + 1));

                            Player.addPang(pang);
                            break;
                        }
                    // Use Card Special Effect get in Game or End Game, AND PER TIME
                    case 2: // Pang %
                    case 3: // Exp %
                    case 5: // PWR Stat
                    case 6: // CTRL Stat
                    case 7: // ACCURY Stat
                    case 8: // SPIN Stat
                    case 9: // CURVE Stat
                    case 10: // Stat Power Gague
                    case 11: // Item Slot +1
                    case 12: // Impact zone Increase
                    case 13: // Sepia Wind %
                    case 14: // Wind Hill %
                    case 15: // Pink Wind %
                    case 16: // Blue Moon %
                    case 18: // Treasure Hunter %
                    case 19: // Chuva %
                    case 20: // Blue Lagoon %
                    case 21: // Blue Water %
                    case 22: // Shinning Send %
                    case 23: // Deep Inferno %
                    case 24: // Silvia Cannon %
                    case 25: // Eastern Valley %
                    case 26: // Lost Seaway %
                    case 27: // Increase Yard(s) On Power Normal, Not Power Shot
                    case 28: // Increase Power Gague for Pangya shot
                    case 29: // Ice Inferno %
                    case 30: // Wiz City %
                    case 31: // Se chover, persistir no próximo hole a Rain
                    case 32: // Efeito de Flor do esquecimento(Mullegen Rose) infinito por tempo(alguns minutos)
                    case 33: // Uknown
                    case 34: // ClubSet Mastery %
                        {
                            if (ItemManager.removeItem(item, Player) <= 0)
                            {
                                throw new exception("[Handle_PLAYER_USE_CARD_SPECIAL][ErrorSystem] Normal [UID=" + Player.UserInfo.UID + "] tentou usar card Special[TYPEID=" + (pCi._typeid) + ", ID=" + (pCi.id) + "], mas nao conseguiu deletar o card. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                    355, 0x5500356));
                            }

                            var pCei = Player.Inventory.FindCardEquipedByTypeid(cei._typeid,
                                0, 0,
                                (int)sIff.Instance.getItemSubGroupIdentify22(cei._typeid),
                                card.Effect);

                            if (pCei != null)
                            {
                                if (pCei._typeid != cei._typeid)
                                {
                                    pCei.id = cei.id;
                                    pCei._typeid = cei._typeid;
                                    pCei.efeito = card.Effect;
                                    pCei.efeito_qntd = card.EffectValue;
                                    pCei.tipo = sIff.Instance.getItemSubGroupIdentify22(cei._typeid);
                                    pCei.use_date = new SystemTime(DateTime.Now);
                                    pCei.end_date = UtilTime.UnixToSystemTime(UtilTime.SystemTimeToUnix(pCei.use_date) + (card.EffectTime * 60));
                                }
                                else
                                {
                                    // É o mesmo só aumenta o tempo
                                    var new_end_date = (UtilTime.GetLocalTimeAsUnix() > UtilTime.SystemTimeToUnix(pCei.end_date)) ? UtilTime.GetLocalTimeAsUnix() : UtilTime.SystemTimeToUnix(pCei.end_date);

                                    pCei.end_date = UtilTime.UnixToSystemTime(new_end_date + (card.EffectTime * 60));
                                }

                                NormalManagerDB.Instance.add(17, new CmdUpdateCardSpecialTime(Player.UserInfo.UID, pCei));

                                cei = pCei;
                            }
                            else
                            {
                                cei.use_date = new SystemTime(DateTime.Now);
                                cei.end_date = (UtilTime.UnixToSystemTime(UtilTime.SystemTimeToUnix(cei.use_date) + (card.EffectTime * 60)));

                                CmdEquipCard cmd_ec = new CmdEquipCard(Player.UserInfo.UID, cei, card.EffectTime);

                                NormalManagerDB.Instance.add(10,
                                    cmd_ec, null, null);

                                if (cmd_ec.getException().getCodeError() != 0)
                                {
                                    throw cmd_ec.getException();
                                }

                                cei = cmd_ec.getInfo();
                                Player.Inventory.CardEquipment.Add(cei);
                                pCei = cei;
                            }
                            break;
                        }
                    default:
                        throw new exception("[Handle_PLAYER_USE_CARD_SPECIAL][ErrorSystem] Normal [UID=" + Player.UserInfo.UID + "] tentou usar card Special[TYPEID=" + (cei._typeid) + ", ID=" + (cei.id) + "], mas card efeito[TYPE=" + (card.Effect) + ", QNTD=" + (card.EffectValue) + ", TEMPO=" + (card.EffectTime) + "min] no IFF_STRUCT do Server é desconhecido. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                            354, 0x5500355));
                }

                p.init_plain(0x160);
                p.WriteUInt32(0); // OK  
                p.WriteUInt32(cei.id);
                p.WriteUInt32(cei._typeid);
                p.WriteUInt32(cei.parts_typeid);
                p.WriteUInt32(cei.parts_id);
                p.WriteUInt32(cei.slot);
                p.WriteUInt32(1);       // Acho que seja o State date, como estava no meu antigo
                p.WriteTime(cei.use_date);
                p.WriteTime(cei.end_date);
                p.WriteUInt16(0);		// Não sei o que é ainda
                Player.Send(p);

                sys_achieve.incrementCounter(0x6C40009E);
                sys_achieve.finish_and_update(Player);
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[Handle_PLAYER_USE_CARD_SPECIAL][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x160);
                p.WriteUInt32((ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) == STDA_ERROR_TYPE.CHANNEL) ? ExceptionError.STDA_SYSTEM_ERROR_DECODE(e.getCodeError()) : 0x5500350);
                Player.Send(p);
            }
        }
    }
}