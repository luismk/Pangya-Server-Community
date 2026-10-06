using Pangya_GameServer.Feature;
using Pangya_GameServer.Manager;
using Pangya_GameServer.Models;
using Pangya_GameServer.PacketFunc;
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

namespace Pangya_GameServer.Handles
{
    public class Handle_PLAYER_BUY_ITEM_SHOP : HandleBase<Player, Packet_EXAMPLE>
    {
        public override async Task Handle()
        {
            var p = new Packet();
            try
            {
                if (Player.UserInfo.BlockFlag.Flag.BuyShopAndGift)
                {
                    throw new exception("[Lobby::RequestBuyItemShop][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou comprar item no ShopRoom, mas ele nao pode. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 1, 0x790001));
                }

                // Log Gastos de CP
                CPLog cp_log = new CPLog();
                cp_log.setType(CPLog.TYPE.BUY_SHOP);

                BuyItem bi = new BuyItem();
                byte option = Packet.ReadByte();
                ushort qntd = Packet.ReadUInt16();

                // Coupon
                stItem coupon = new stItem();
                string coupon_msg = "";
                ulong pang = 0;
                ulong cookie = 0;

                if (qntd > 0)
                {
                    stItem item = new stItem();
                    List<stItem> v_item = new List<stItem>();

                    for (var i = 0; i < qntd; ++i)
                    {
                        bi = new BuyItem().ToRead(Packet);

                        // Verifica se o item pode ser comprado
                        if (sIff.Instance.IsBuyItem(bi._typeid) && !sIff.Instance.IsOnlyGift(bi._typeid))
                        {
                            // Inicializa o item que o Player vai comprar
                            if (bi.pang > 0)
                            {
                                pang += bi.pang;
                            }

                            if (bi.cookie > 0)
                            {
                                cookie += bi.cookie;
                            }

                            item = new stItem();

                            ItemManager.initItemFromBuyItem(Player.UserInfo, item, bi, true, option);

                            if (item._typeid == 0)
                            {
                                _smp.LogManager.Instance.push(new AppMessage("[Lobby::RequestBuyItemShop][Error] ao inicializar item from buyItem, item typeid: " + bi._typeid + " bug. para o Normal [UID=" + Player.UserInfo.UID + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                                p = new Packet((ushort)0x68);
                                p.WriteUInt32(1);
                                Player.Send(p);
                                return;
                            }

                            if (item.is_cash == 1 ? (option != 1 && item.desconto != 0 ? bi.cookie != (item.desconto * item.qntd) : bi.cookie != (item.price * item.qntd)) :
                              (option != 1 && item.desconto != 0 ? bi.pang != (item.desconto * item.qntd) : bi.pang != (item.price * item.qntd)))
                            {
                                _smp.LogManager.Instance.push(new AppMessage("[Lobby::RequestBuyItemShop][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou comprar um item com preco[server=" + (item.desconto != 0 ? (item.desconto * item.qntd) : (item.price * item.qntd)) + ", cliente=" + (item.is_cash.IsTrue() ? bi.cookie : bi.pang) + "] diferente, item typeid: " + bi._typeid + ". Hacker ou bug.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                                p.init_plain(0x68);
                                p.WriteUInt32(2);
                                Player.Send(p);
                                return;
                            }

                            if (!ItemManager.isTimeItem(item.date) || ItemManager.betweenTimeSystem(ref item.date))
                            {
                                // Verifica se já possui o item
                                if ((sIff.Instance.IsCanOverlapped(item._typeid) && sIff.Instance.getItemGroupIdentify(item._typeid) != IFF_GROUP.CAD_ITEM) || !Player.Inventory.ownerItem(item._typeid))
                                {
                                    if (ItemManager.isSetItem(item._typeid))
                                    {
                                        var v_stItem = ItemManager.GetItemOfSetItem(Player, item._typeid, true, 1);

                                        // CP Log, Set Item
                                        if (item.is_cash.IsTrue() && bi.cookie > 0)
                                        {
                                            cp_log.putItem(item._typeid, (item.STDA_C_ITEM_TIME > 0 ? item.STDA_C_ITEM_TIME : item.STDA_C_ITEM_QNTD), bi.cookie);
                                        }

                                        if (v_stItem.Count > 0)
                                        {
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
                                            _smp.LogManager.Instance.push(new AppMessage("[Lobby::RequestBuyItemShop][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou comprar um set item que nao tem item, item typeid: " + bi._typeid + ". Hacker ou bug.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                                            p.init_plain(0x68);
                                            p.WriteUInt32(3);
                                            Player.Send(p);
                                            return;
                                        }
                                    }
                                    else
                                    {
                                        v_item.Add(new stItem(item));

                                        // CP Log, Item
                                        if (item.is_cash.IsTrue() && bi.cookie > 0)
                                        {
                                            cp_log.putItem(item._typeid, (item.STDA_C_ITEM_TIME > 0 ? item.STDA_C_ITEM_TIME : item.STDA_C_ITEM_QNTD), bi.cookie);
                                        }
                                    }
                                }
                                else if (sIff.Instance.getItemGroupIdentify(item._typeid) == IFF_GROUP.CAD_ITEM)
                                {
                                    _smp.LogManager.Instance.push(new AppMessage("[Lobby::RequestBuyItemShop][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou comprar um CaddieItem que ele nao tem o caddie, item typeid: " + bi._typeid + ". Hacker ou bug.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                                    p.init_plain(0x68);
                                    p.WriteUInt32(11);
                                    Player.Send(p);
                                    return;
                                }
                                else
                                {
                                    _smp.LogManager.Instance.push(new AppMessage("[Lobby::RequestBuyItemShop][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou comprar um item que ele ja tem, item typeid: " + bi._typeid + ". Hacker ou bug.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                                    p.init_plain(0x68);
                                    p.WriteUInt32(4);
                                    Player.Send(p);
                                    return;
                                }
                            }
                            else
                            {
                                _smp.LogManager.Instance.push(new AppMessage("[Lobby::RequestBuyItemShop][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou comprar um item que nao pode comprar, nao esta na data, item typeid: " + bi._typeid + ". Hacker ou bug.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                                p.init_plain(0x68);
                                p.WriteUInt32(5);
                                Player.Send(p);
                                return;
                            }
                        }
                        else
                        {
                            _smp.LogManager.Instance.push(new AppMessage("[Lobby::RequestBuyItemShop][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou comprar um item que nao pode ser comprado, item typeid: " + bi._typeid + ". Hacker ou bug.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                            p = new Packet((ushort)0x68);
                            p.WriteUInt32(6);
                            Player.Send(p);
                            return;
                        }
                    }

                    // Coupon Id
                    coupon.id = Packet.ReadInt32();

                    if (coupon.id != 0)
                    {
                        var wi_coupon = Player.Inventory.FindWarehouseItemById(coupon.id);
                        if (wi_coupon == null)
                        {
                            _smp.LogManager.Instance.push(new AppMessage("[Lobby::RequestBuyItemShop][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou comprar um item com coupon de descontou, mas ele nao tem o coupon[ID=" + coupon.id + "], item typeid: " + bi._typeid + ". Hacker ou bug.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                            p.init_plain(0x68);
                            p.WriteUInt32(6);
                            Player.Send(p);
                            return;
                        }

                        coupon.type = 2;
                        coupon._typeid = wi_coupon._typeid;
                        coupon.qntd = 1;
                        coupon.STDA_C_ITEM_QNTD = (short)(coupon.qntd * -1);

                        ulong old_price = cookie;
                        string type_desconto = "5%";

                        var cmd_guai = new CmdCouponShop(Player.UserInfo.UID, coupon.id, true);
                        NormalManagerDB.Instance.add(0, cmd_guai, null, null);

                        if (cmd_guai.getException().getCodeError() != 0)
                            throw cmd_guai.getException();

                        var desconto = cmd_guai.getCouponShop();

                        if (desconto > 0)
                        {
                            if ((long)(cookie - (ulong)desconto) < 0)
                                cookie = 0;
                            else
                                cookie -= (ulong)desconto;

                            type_desconto = desconto + "% CP";
                        }
                        else
                        {
                            cookie = (ulong)(cookie * 0.95f);
                        }

                        coupon_msg = " e usou Coupon[TYPEID=" + wi_coupon._typeid + ", ID=" + wi_coupon.id + ", DESCONTO=" + type_desconto + ", TOTAL_CP=" + old_price + ", TOTAL_CP_COM_DESCONTO=" + cookie + "]";
                    }

                    if (Player.UserInfo.Cookie < cookie || Player.UserInfo.Statistics.pang < pang)
                    {
                        _smp.LogManager.Instance.push(new AppMessage("[Lobby::RequestBuyItemShop][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou comprar um item, mas nao tem moedas(Pang ou Cookie) suficiente, item typeid: " + bi._typeid + ". Hacker ou bug.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                        p.init_plain(0x68);
                        p.WriteUInt32(7);
                        Player.Send(p);
                        return;
                    }

                    try
                    {
                        Player.UserInfo.consomeMoeda(pang, cookie);
                    }
                    catch (exception e)
                    {
                        _smp.LogManager.Instance.push(new AppMessage("[Lobby::RequestBuyItemShop][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                        if (ExceptionError.STDA_ERROR_CHECK_SOURCE_AND_ERROR_TYPE(e.getCodeError(), STDA_ERROR_TYPE.PLAYER_INFO, 200))
                        {
                            p.init_plain(0x68);
                            p.WriteUInt32(2);
                            Player.Send(p);
                            return;
                        }
                        else
                        {
                            throw;
                        }
                    }

                    if (coupon.id != 0 && coupon._typeid != 0u)
                    {
                        if (ItemManager.removeItem(coupon, Player) <= 0)
                        {
                            _smp.LogManager.Instance.push(new AppMessage("[Lobby::RequestBuyItemShop][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou comprar um item com coupon de descontou, mas nao conseguiu remove o coupon[TYPEID=" + coupon._typeid + ", ID=" + coupon.id + "], item typeid: " + bi._typeid + ". Hacker ou bug.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                            _smp.LogManager.Instance.push(new AppMessage("[Lobby::RequestBuyItemShop][Warning] devolve as moedas gasta deu erro no add itens no db para o Player.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                            Player.UserInfo.addMoeda(pang, cookie);
                            p.init_plain(0x68);
                            p.WriteUInt32(8);
                            Player.Send(p);
                            return;
                        }

                        p.init_plain(0x216);
                        p.WriteUInt32((uint)UtilTime.GetSystemTimeAsUnix());
                        p.WriteUInt32(1u);
                        p.WriteByte(coupon.type);
                        p.WriteUInt32(coupon._typeid);
                        p.WriteInt32(coupon.id);
                        p.WriteInt32(coupon.flag_time);
                        p.WriteInt32(coupon.stat.qntd_ant);
                        p.WriteInt32(coupon.stat.qntd_dep);
                        p.WriteInt32((coupon.STDA_C_ITEM_TIME > 0 ? coupon.STDA_C_ITEM_TIME : coupon.STDA_C_ITEM_QNTD));
                        p.WriteZero(25);
                        Player.Send(p);
                    }

                    var rai = ItemManager.addItem(v_item, Player, 0, 1);

                    if (rai.fails.Count > 0 && rai.type != RetAddItem.SUCCESS_PANG_AND_EXP_AND_CP_POUCH)
                    {
                        string str = "";
                        for (var i = 0; i < rai.fails.Count; ++i)
                        {
                            if (i == 0)
                                str += "[TYPEID=" + rai.fails[i]._typeid + ", ID=" + rai.fails[i].id + ", QNTD=" + ((rai.fails[i].qntd > 0xFFu) ? rai.fails[i].qntd : rai.fails[i].STDA_C_ITEM_QNTD) + (rai.fails[i].STDA_C_ITEM_TIME > 0 ? ", TEMPO=" + rai.fails[i].STDA_C_ITEM_TIME : "") + "]";
                            else
                                str += ", [TYPEID=" + rai.fails[i]._typeid + ", ID=" + rai.fails[i].id + ", QNTD=" + ((rai.fails[i].qntd > 0xFFu) ? rai.fails[i].qntd : rai.fails[i].STDA_C_ITEM_QNTD) + (rai.fails[i].STDA_C_ITEM_TIME > 0 ? ", TEMPO=" + rai.fails[i].STDA_C_ITEM_TIME : "") + "]";
                        }

                        _smp.LogManager.Instance.push(new AppMessage("[Lobby::RequestBuyItemShop][Error] Itens que falhou ao add os itens que o Normal [UID=" + Player.UserInfo.UID + "] comprou item(ns){" + str + "}. Hacker ou bug.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                        _smp.LogManager.Instance.push(new AppMessage("[Lobby::RequestBuyItemShop][Warning] devolve as moedas gasta deu erro no add itens no db para o Player.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                        Player.UserInfo.addMoeda(pang, cookie);
                        p.init_plain(0x68);
                        p.WriteUInt32(8);
                        Player.Send(p);
                        return;
                    }

                    if (pang > 0)
                    {
                        p.init_plain(0xC8);
                        p.WriteUInt64(Player.UserInfo.Statistics.pang);
                        p.WriteUInt64(pang);
                        Player.Send(p);
                    }

                    if (cookie > 0)
                    {
                        Player.saveCPLog(cp_log);
                        p.init_plain(0x96);
                        p.WriteUInt64(Player.UserInfo.Cookie);
                        Player.Send(p);
                    }

                    Player.Send(Handle_PACKET_RESPONSE.pacote0AA(Player, v_item));

                    p.init_plain(0x68);
                    p.WriteUInt32(0);
                    p.WriteUInt64(Player.UserInfo.Statistics.pang);
                    p.WriteUInt64(Player.UserInfo.Cookie);
                    Player.Send(p);

                    NormalManagerDB.Instance.add(0, new CmdItemBuyShopLog(Player.UserInfo.UID, bi), null, null);
                }
                else
                {
                    _smp.LogManager.Instance.push(new AppMessage("[Lobby::RequestBuyItemShop][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou comprar um item, mas nao enviou nenhum item no Request. Hacker ou bug.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                    p.init_plain(0x68);
                    p.WriteUInt32(9);
                    Player.Send(p);
                }
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[Lobby::RequestBuyItemShop][Error] Normal [UID=" + Player.UserInfo.UID + "] error desconhecido: " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                p.init_plain(0x68);
                p.WriteUInt32(10);
                Player.Send(p);
            }
        }
    }
}