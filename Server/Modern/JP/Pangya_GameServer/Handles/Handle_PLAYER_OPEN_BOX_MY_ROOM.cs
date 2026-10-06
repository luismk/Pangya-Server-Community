using Pangya_GameServer.Feature;
using Pangya_GameServer.Flags;
using Pangya_GameServer.Manager;
using Pangya_GameServer.Models;
using Pangya_GameServer.Repository;
using Pangya_GameServer.Session;
using PangyaAPI.IFF.Flags;
using PangyaAPI.IFF.Handle.JP;
using PangyaAPI.IFF.Regions.JP.Models.IFF;
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
    public class Handle_PLAYER_OPEN_BOX_MY_ROOM : HandleBase<Player, Packet_EXAMPLE>
    {
        public override async Task Handle()
        {
            Packet p = new Packet();

            try
            {
                if (!sBoxSystem.Instance.isLoad())
                {
                    sBoxSystem.Instance.load();
                }

                uint box_typeid = Packet.ReadUInt32();

                if (box_typeid == 0)
                {
                    throw new exception("[Lobby::RequestOpenBoxMyRoom][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou abrir Box[TYPEID=" + (box_typeid) + "], mas o typeid é invalido(zero). Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        1, 0x6300201));
                }

                var pWi = Player.Inventory.FindWarehouseItemByTypeid(box_typeid);

                if (pWi == null)
                {
                    throw new exception("[Lobby::RequestOpenBoxMyRoom][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou abrir Box[TYPEID=" + (box_typeid) + "], mas ele nao tem essa Box. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        2, 0x6300202));
                }

                if (pWi.STDA_C_ITEM_QNTD < 1)
                {
                    throw new exception("[Lobby::RequestOpenBoxMyRoom][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou abrir Box[TYPEID=" + (pWi._typeid) + ", ID=" + (pWi.id) + "], mas ele nao tem quantidade suficiente da Box[value=" + (pWi.STDA_C_ITEM_QNTD) + ", Request=1]", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        3, 0x6300203));
                }

                if (sIff.Instance.getItemGroupIdentify(pWi._typeid) != IFF_GROUP.ITEM)
                {
                    throw new exception("[Lobby::RequestOpenBoxMyRoom][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou abrir Box[TYPEID=" + (pWi._typeid) + ", ID=" + (pWi.id) + "], mas nao é uma Box valida. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        4, 0x6300204));
                }

                var item_iff = sIff.Instance.findItem(pWi._typeid);

                if (item_iff == null)
                {
                    throw new exception("[Lobby::RequestOpenBoxMyRoom][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou abrir Box[TYPEID=" + (pWi._typeid) + ", ID=" + (pWi.id) + "], mas nao tem essa Box no IFF_STRUCT do Server. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        5, 0x6300205));
                }

                var box = sBoxSystem.Instance.findBox(pWi._typeid);

                if (box == null)
                {
                    throw new exception("[Lobby::RequestOpenBoxMyRoom][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou abrir Box[TYPEID=" + (pWi._typeid) + ", ID=" + (pWi.id) + "], mas nao tem essa Box no Box System do Server. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        6, 0x6300206));
                }

                List<stItem> v_item = new List<stItem>();
                stItem item = new stItem();
                stItem stBox = new stItem();

                ctx_box_item ctx_bi = null;

                // ----------- Sortea ---------------
                ctx_bi = sBoxSystem.Instance.drawBox(Player, box);

                if (ctx_bi == null)
                {
                    throw new exception("[Lobby::RequestOpenBoxMail][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou abrir Box[TYPEID=" + (pWi._typeid) + ", ID=" + (pWi.id) + "], mas nao conseguiu sortear um Box Item. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        9, 0x6300209));
                }

                // Init Item Ganho
                BuyItem bi = new BuyItem();
                Mascot mascot = null;

                item = new stItem();

                bi.id = -1;
                bi._typeid = ctx_bi._typeid;

                // Check se é Mascot, para colocar por dia o tempo que é a quantidade
                if (sIff.Instance.getItemGroupIdentify(ctx_bi._typeid) == IFF_GROUP.MASCOT
                    && (mascot = sIff.Instance.findMascot(ctx_bi._typeid)) != null
                    && mascot.Shop.flag_shop.time_shop.dia > 0
                    && mascot.Shop.flag_shop.time_shop.active)
                {
                    bi.qntd = 1;
                    bi.time = (short)ctx_bi.qntd;
                }
                else
                {
                    bi.qntd = (uint)ctx_bi.qntd;
                }

                ItemManager.initItemFromBuyItem(Player.UserInfo,
                    item, bi, false, 0, 0, 1);

                if (item._typeid == 0)
                {
                    throw new exception("[Lobby::RequestOpenBoxMail][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou abrir Box[TYPEID=" + (pWi._typeid) + ", ID=" + (pWi.id) + "], mas nao conseguiu inicializar o Item[TYPEID=" + (bi._typeid) + "]", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        11, 0x6300211));
                }

                // Verifica se já possui o item, o caddie item verifica se tem o caddie para depois verificar se tem o caddie item
                if ((sIff.Instance.IsCanOverlapped(item._typeid) && sIff.Instance.getItemGroupIdentify(item._typeid) != IFF_GROUP.CAD_ITEM) || !Player.Inventory.ownerItem(item._typeid))
                {
                    if (ItemManager.isSetItem(item._typeid))
                    {
                        var v_stItem = ItemManager.GetItemOfSetItem(Player,
                            item._typeid, false, 1);

                        if (v_stItem.Any())
                        {
                            // Já verificou lá em cima se tem os item so set, então não precisa mais verificar aqui
                            // Só add eles ao List de venda
                            // Verifica se pode ter mais de 1 item e se não ver se não tem o item
                            foreach (var el in v_stItem)
                            {
                                if ((sIff.Instance.IsCanOverlapped(el._typeid) && sIff.Instance.getItemGroupIdentify(el._typeid) != IFF_GROUP.CAD_ITEM) || !Player.Inventory.ownerItem(el._typeid))
                                {
                                    v_item.Add(new stItem(el));
                                }
                            }
                        }
                        else
                        {
                            throw new exception("[Lobby::RequestOpenBoxMail][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou abrir Box[TYPEID=" + (pWi._typeid) + ", ID=" + (pWi.id) + "], mas SetItem que ele ganhou da box, nao tem Item[TYPEID=" + (bi._typeid) + "]. Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                12, 0x6300212));
                        }
                    }
                    else
                    {
                        v_item.Add(new stItem(item));
                    }

                }
                else if (sIff.Instance.getItemGroupIdentify(item._typeid) == IFF_GROUP.CAD_ITEM)
                {
                    throw new exception("[Lobby::RequestOpenBoxMail][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou abrir Box[TYPEID=" + (pWi._typeid) + ", ID=" + (pWi.id) + "], mas o CaddieItem que ele ganhou, nao tem o caddie, Item[TYPEID=" + (bi._typeid) + "]. Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        13, 0x6300213));
                }
                else
                {
                    throw new exception("[Lobby::RequestOpenBoxMail][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou abrir Box[TYPEID=" + (pWi._typeid) + ", ID=" + (pWi.id) + "], mas ele ja tem o Item[TYPEID=" + (bi._typeid) + "]. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        14, 0x6300214));
                }

                // UPDATE ON SERVER AND DB

                // Delete Box
                stBox.clear();

                stBox.type = 2;
                stBox.id = pWi.id;
                stBox._typeid = box._typeid;
                stBox.qntd = 1;
                stBox.STDA_C_ITEM_QNTD = (short)(stBox.qntd * -1);

                if (ItemManager.removeItem(stBox, Player) <= 0)
                {
                    throw new exception("[Lobby::RequestOpenBoxMail][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou abrir Box[TYPEID=" + (pWi._typeid) + ", ID=" + (pWi.id) + "], mas nao conseguiu deletar Box. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        10, 0x6300210));
                }

                string str = "";

                // Coloca Item ganho no My Room do Player
                var rai = ItemManager.addItem(v_item,
                    Player, 0, 0);

                if (rai.fails.Count > 0 && rai.type != RetAddItem.SUCCESS_PANG_AND_EXP_AND_CP_POUCH)
                {

                    for (var i = 0; i < v_item.Count; ++i)
                    {
                        if (i == 0)
                        {
                            str += "[TYPEID=" + (v_item[i]._typeid) + ", ID=" + (v_item[i].id) + ", QNTD=" + ((v_item[i].qntd > 0xFFu) ? v_item[i].qntd : v_item[i].STDA_C_ITEM_QNTD) + (v_item[i].STDA_C_ITEM_TIME > 0 ? ", TEMPO=" + (v_item[i].STDA_C_ITEM_TIME) : "") + "]";
                        }
                        else
                        {
                            str += ", [TYPEID=" + (v_item[i]._typeid) + ", ID=" + (v_item[i].id) + ", QNTD=" + ((v_item[i].qntd > 0xFFu) ? v_item[i].qntd : v_item[i].STDA_C_ITEM_QNTD) + (v_item[i].STDA_C_ITEM_TIME > 0 ? ", TEMPO=" + (v_item[i].STDA_C_ITEM_TIME) : "") + "]";
                        }
                    }

                    throw new exception("[Lobby::RequestOpenBoxMail][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou abrir Box[TYPEID=" + (pWi._typeid) + ", ID=" + (pWi.id) + "], mas ele nao conseguiu adicionar os item(ns){" + str + "}. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        15, 0x6300215));
                }
                else
                {
                    // Init Item Add Log
                    for (var i = 0; i < v_item.Count; ++i)
                    {
                        if (i == 0)
                        {
                            str += "[TYPEID=" + (v_item[i]._typeid) + ", ID=" + (v_item[i].id) + ", QNTD=" + ((v_item[i].qntd > 0xFFu) ? v_item[i].qntd : v_item[i].STDA_C_ITEM_QNTD) + (v_item[i].STDA_C_ITEM_TIME > 0 ? ", TEMPO=" + (v_item[i].STDA_C_ITEM_TIME) : "") + "]";
                        }
                        else
                        {
                            str += ", [TYPEID=" + (v_item[i]._typeid) + ", ID=" + (v_item[i].id) + ", QNTD=" + ((v_item[i].qntd > 0xFFu) ? v_item[i].qntd : v_item[i].STDA_C_ITEM_QNTD) + (v_item[i].STDA_C_ITEM_TIME > 0 ? ", TEMPO=" + (v_item[i].STDA_C_ITEM_TIME) : "") + "]";
                        }
                    }
                }

                // DB Register Rare Win Log
                if (ctx_bi != null && ctx_bi.raridade > 0)
                {
                    NormalManagerDB.Instance.add(22,
                         new CmdInsertBoxRareWinLog(Player.UserInfo.UID,
                             box._typeid, ctx_bi),
                        null, null);
                }

                // UPDATE ON GAME

                // atualiza moedas e item(ns) em jogo
                foreach (var el in v_item)
                {
                    p.init_plain(0xAA);

                    p.WriteUInt16(1); // count;

                    p.WriteUInt32(el._typeid);
                    p.WriteInt32(el.id);
                    p.WriteInt16(el.c[3]);
                    p.WriteByte(el.flag_time);
                    p.WriteUInt16((ushort)el.stat.qntd_dep);
                    if (el.date != null && el.date.active.IsTrue())
                        p.WriteTime(el.date.date.sysDate[1].ConvertTime());
                    else
                        p.WriteZero(16);
                    p.WriteString(el.ucc.IDX, 9);

                    p.WriteUInt64(Player.UserInfo.Statistics.pang);
                    p.WriteUInt64(Player.UserInfo.Cookie);

                    Player.Send(p);
                }

                // Resposta do Abrir Box My Room
                p.init_plain(0x129);

                p.WriteByte(0); // OK

                p.WriteUInt32(box._typeid);
                p.WriteInt32(stBox.stat.qntd_dep);

                p.WriteUInt32((uint)v_item.Count); // Count

                foreach (var el in v_item)
                {
                    p.WriteUInt32(el._typeid);
                    p.WriteInt32(el.id);
                    p.WriteInt32((el.STDA_C_ITEM_TIME > 0) ? el.STDA_C_ITEM_TIME : el.STDA_C_ITEM_QNTD);
                    p.WriteZero(8); // Não sei o que é esses 8 Bytes ainda
                }

                Player.Send(p);

            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[Lobby::RequestOpenBoxMyRoom][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x129);

                p.WriteByte(1); // Error

                p.WriteZero(12); // Box Typeid, Box Qntd e count de itens

                Player.Send(p);
            }
        } 
    }
}