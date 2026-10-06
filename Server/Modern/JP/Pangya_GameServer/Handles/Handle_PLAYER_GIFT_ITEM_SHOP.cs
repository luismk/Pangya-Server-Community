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

namespace Pangya_GameServer.Handles
{
    public class Handle_PLAYER_GIFT_ITEM_SHOP : HandleBase<Player, Packet_EXAMPLE>
    {
        public override async Task Handle()
        {
            Packet p = new Packet();

            try
            {
                // Dados Log gasto de CP
                CPLog cp_log = new CPLog();

                cp_log.setType(CPLog.TYPE.GIFT_SHOP);

                BuyItem bi = new BuyItem();

                ushort option = Packet.ReadUInt16();
                uint uid_to_send = Packet.ReadUInt32();
                string msg = Packet.ReadString();
                byte opt2 = Packet.ReadByte();
                ushort qntd = Packet.ReadUInt16();

                ulong pang = 0Ul;
                ulong cookie = 0Ul;

                if (Player.UserInfo.BlockFlag.Flag.GiftShop || Player.UserInfo.BlockFlag.Flag.BuyShopAndGift)
                {
                    throw new exception("[Lobby::RequestGiftItemShop][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou presentear Normal [UID=" + (uid_to_send) + "], mas ele nao pode. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        2, 0x790001));
                }

                // Verifica o Level do Player e bloquea se não tiver Level Beginner E
                if (Player.UserInfo.Member.GameLevel < (ushort)enLEVEL.BEGINNER_E)
                {
                    throw new exception("[Lobby::RequestGiftItemShop][Error] Normal [UID=" + Player.UserInfo.UID + ", LEVEL=" + (Player.UserInfo.Member.GameLevel) + "] tentou presentear o Normal [UID=" + (uid_to_send) + "], mas o Level dele é menor que Beginner E.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        3500, 1));
                }

                if (qntd > 0)
                {
                    stItem item = new stItem();
                    List<stItem> v_item = new List<stItem>();

                    for (var i = 0; i < qntd; ++i)
                    {
                        bi = new BuyItem().ToRead(Packet);

                        // Verifica se o item pode ser presenteado
                        if (sIff.Instance.IsGiftItem(bi._typeid))
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

                            ItemManager.initItemFromBuyItem(Player.UserInfo,
                                item, bi, true, option, 1);

                            if (item._typeid == 0)
                            {
                                _smp.LogManager.Instance.push(new AppMessage("[Lobby::RequestGiftItemShop][Error] ao inicializar item from buyItem, item typeid: " + (bi._typeid) + " bug. para o Normal [UID=" + Player.UserInfo.UID + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                                p.init_plain(0x6A);
                                p.WriteUInt32(1);
                                p.WriteUInt64(Player.UserInfo.Statistics.pang);
                                p.WriteUInt64(Player.UserInfo.Cookie);

                                Player.Send(p);
                                return;
                            }

                            if (item.is_cash.IsTrue() ? (item.desconto != 0 ? bi.cookie != (item.desconto * item.qntd) : bi.cookie != (item.price * item.qntd)) : (item.desconto != 0 ? bi.pang != (item.desconto * item.qntd) : bi.pang != (item.price * item.qntd)))
                            {
                                _smp.LogManager.Instance.push(new AppMessage("[Lobby::RequestGiftItemShop][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou presentear para o Normal [UID=" + (uid_to_send) + "] um item com preco[server=" + ((item.desconto != 0 ? (item.desconto * item.qntd) : (item.price * item.qntd))) + ", cliente=" + ((item.is_cash.IsTrue() ? bi.cookie : bi.pang)) + "] diferente, item typeid: " + (bi._typeid) + ". Hacker ou bug.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                                p.init_plain(0x6A);
                                p.WriteUInt32(2);
                                p.WriteUInt64(Player.UserInfo.Statistics.pang);
                                p.WriteUInt64(Player.UserInfo.Cookie);

                                Player.Send(p);
                                return;
                            }

                            if (!ItemManager.isTimeItem(item.date) || ItemManager.betweenTimeSystem(ref item.date))
                            {
                                // para ele verificar se o Player tem o caddie antes de enviar o part do caddie
                                if ((sIff.Instance.IsCanOverlapped(item._typeid) && sIff.Instance.getItemGroupIdentify(item._typeid) != IFF_GROUP.CAD_ITEM) || !ItemManager.ownerItem(uid_to_send, item._typeid))
                                {
                                    if (ItemManager.isSetItem(item._typeid))
                                    {
                                        // CP Log, Set Item
                                        if (item.is_cash.IsTrue() && bi.cookie > 0)
                                        {
                                            cp_log.putItem(item._typeid,
                                                (item.STDA_C_ITEM_TIME > 0 ? item.STDA_C_ITEM_TIME : item.STDA_C_ITEM_QNTD),
                                                bi.cookie);
                                        }

                                        var v_stItem = ItemManager.GetItemOfSetItem(Player,
                                            item._typeid, true, 1);

                                        // No gift ele envia o set para o Player, e não os itens que contém dentro do set
                                        if (v_stItem.Any())
                                        {
                                            v_item.Add(new stItem(item));
                                        }
                                        else
                                        {
                                            _smp.LogManager.Instance.push(new AppMessage("[Lobby::RequestGiftItemShop][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou presentear para o Normal [UID=" + (uid_to_send) + "] um set item que nao tem item, item typeid: " + (bi._typeid) + ". Hacker ou bug.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                                            p.init_plain(0x6A);
                                            p.WriteUInt32(3);
                                            p.WriteUInt64(Player.UserInfo.Statistics.pang);
                                            p.WriteUInt64(Player.UserInfo.Cookie);

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
                                            cp_log.putItem(item._typeid,
                                                (item.STDA_C_ITEM_TIME > 0 ? item.STDA_C_ITEM_TIME : item.STDA_C_ITEM_QNTD),
                                                bi.cookie);
                                        }
                                    }
                                }
                                else if (sIff.Instance.getItemGroupIdentify(item._typeid) == IFF_GROUP.CAD_ITEM)
                                {
                                    _smp.LogManager.Instance.push(new AppMessage("[Lobby::RequestGiftItemShop][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou presentear um CaddieItem que o Normal [UID=" + (uid_to_send) + "] nao tem o caddie, item typeid: " + (bi._typeid), type_msg.CL_FILE_LOG_AND_CONSOLE));

                                    p.init_plain(0x6A);
                                    p.WriteUInt32(11);
                                    p.WriteUInt64(Player.UserInfo.Statistics.pang);
                                    p.WriteUInt64(Player.UserInfo.Cookie);

                                    Player.Send(p);
                                    return;
                                }
                                else
                                {
                                    _smp.LogManager.Instance.push(new AppMessage("[Lobby::RequestGiftItemShop][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou presentear um item que o Normal [UID=" + (uid_to_send) + "] ja tem, item typeid: " + (bi._typeid), type_msg.CL_FILE_LOG_AND_CONSOLE));

                                    p.init_plain(0x6A);
                                    p.WriteUInt32(4);
                                    p.WriteUInt64(Player.UserInfo.Statistics.pang);
                                    p.WriteUInt64(Player.UserInfo.Cookie);

                                    Player.Send(p);
                                    return;
                                }
                            }
                            else
                            {
                                _smp.LogManager.Instance.push(new AppMessage("[Lobby::RequestGiftItemShop][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou presentear para o Normal [UID=" + (uid_to_send) + "] um item que nao esta na data para esta disponivel no ShopRoom, item typeid: " + (bi._typeid) + ". Hacker ou bug.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                                p.init_plain(0x6A);
                                p.WriteUInt32(5);
                                p.WriteUInt64(Player.UserInfo.Statistics.pang);
                                p.WriteUInt64(Player.UserInfo.Cookie);

                                Player.Send(p);
                                return;
                            }
                        }
                        else
                        {
                            _smp.LogManager.Instance.push(new AppMessage("[Lobby::RequestGiftItemShop][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou presentear para o Normal [UID=" + (uid_to_send) + "] um item que nao pode ser comprado[indisponivel no ShopRoom], item typeid: " + (bi._typeid) + ". Hacker ou bug.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            p.init_plain(0x6A);
                            p.WriteUInt32(6);
                            p.WriteUInt64(Player.UserInfo.Statistics.pang);
                            p.WriteUInt64(Player.UserInfo.Cookie);

                            Player.Send(p);
                            return;
                        }
                    }

                    if (Player.UserInfo.Cookie < cookie || Player.UserInfo.Statistics.pang < pang)
                    {
                        _smp.LogManager.Instance.push(new AppMessage("[Lobby::RequestGiftItemShop][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou presentear para o Normal [UID=" + (uid_to_send) + "] um item, mas nao tem moedas(Pang ou Cookie) suficiente, item typeid: " + (bi._typeid) + ". Hacker ou bug.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                        p.init_plain(0x6A);
                        p.WriteUInt32(7);
                        p.WriteUInt64(Player.UserInfo.Statistics.pang);
                        p.WriteUInt64(Player.UserInfo.Cookie);

                        Player.Send(p);
                        return;
                    }

                    try
                    {
                        // Consome o cookie e Pang, Antes de adicionar os itens
                        Player.UserInfo.consomeMoeda(pang, cookie);
                    }
                    catch (exception e)
                    {
                        _smp.LogManager.Instance.push(new AppMessage("[Lobby::RequestGiftItemShop][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                        if (ExceptionError.STDA_ERROR_CHECK_SOURCE_AND_ERROR_TYPE(e.getCodeError(),
                            STDA_ERROR_TYPE.PLAYER_INFO,
                            200))
                        {
                            p.init_plain(0x6A);
                            p.WriteUInt32(2); // Tem alterações no Cookie do Player no DB
                            p.WriteUInt64(Player.UserInfo.Statistics.pang);
                            p.WriteUInt64(Player.UserInfo.Cookie);

                            Player.Send(p);
                            return;
                        }
                        else
                        {
                            throw;
                        }
                    }

                    int mail_id = 0;

                    try
                    {
                        if ((mail_id = MailManager.SendMailWithItem(Player.UserInfo.UID,
                            uid_to_send, msg, v_item)) <= 0)
                        {
                            throw new exception("[Lobby::RequestGiftItemShop][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou presentear um Normal [UID=" + (uid_to_send) + "] com o Item[TYPEID=" + (bi._typeid) + "], mas nao conseguiu colocar o item no mail box do Player. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                1, 0x5800101));
                        }
                    }
                    catch (exception e)
                    {
                        _smp.LogManager.Instance.push(new AppMessage("[Lobby::RequestGiftItemShop][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                        _smp.LogManager.Instance.push(new AppMessage("[Lobby::RequestGiftItemShop][Error] ao add os itens que o Normal [UID=" + Player.UserInfo.UID + "] presenteou para o Normal [UID=" + (uid_to_send) + "]. Hacker ou bug.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                        _smp.LogManager.Instance.push(new AppMessage("[Lobby::RequestGiftItemShop][Error] devolve as moedas gasta deu erro no add itens no db para o Player.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                        // Devolve as moedas gasta para o Player
                        Player.UserInfo.addMoeda(pang, cookie);

                        p.init_plain(0x6A);
                        p.WriteUInt32(8);
                        p.WriteUInt64(Player.UserInfo.Statistics.pang);
                        p.WriteUInt64(Player.UserInfo.Cookie);

                        Player.Send(p);
                        return;
                    }

                    // Log
                    var log_itens = new StringBuilder();

                    foreach (var el in v_item)
                    {
                        if (log_itens.Length > 0)
                            log_itens.Append("; ");

                        log_itens.Append($"[TYPEID={el._typeid}, ID={el.id}, FLAG_TIME={el.flag_time}, " +
                                         $"QNTD={(el.STDA_C_ITEM_TIME > 0 ? el.STDA_C_ITEM_TIME : el.STDA_C_ITEM_QNTD)}, " +
                                         $"QNTD_DEPOIS={el.stat.qntd_dep}]");
                    }

                    var log_msg = $"[Lobby::RequestGiftItemShop][Sucess] Normal [UID=" + Player.UserInfo.UID + "] MailBox[MAIL_ID=" + (mail_id) + "] mandou " + (v_item.Count) + " presente(s), Moedas(CP=" + (cookie) + ", PANG=" + (pang) + "), do Shop para o Normal [UID=" + (uid_to_send) + "]. Item(ns) { " + log_itens + " }";

                    _smp.LogManager.Instance.push(new AppMessage(log_msg, type_msg.CL_ONLY_FILE_LOG));

                    if (pang > 0)
                    {
                        p.init_plain(0xC8);
                        p.WriteUInt64(Player.UserInfo.Statistics.pang);
                        p.WriteUInt64(pang);
                        Player.Send(p);
                    }

                    if (cookie > 0)
                    {
                        // Log de Gastos de CP
                        cp_log.setMailId(mail_id);
                        Player.saveCPLog(cp_log);

                        p.init_plain(0x96);
                        p.WriteUInt64(Player.UserInfo.Cookie);
                        Player.Send(p);
                    }

                    p.init_plain(0x6A);
                    p.WriteUInt32(0);
                    p.WriteUInt64(Player.UserInfo.Statistics.pang);
                    p.WriteUInt64(Player.UserInfo.Cookie);

                    Player.Send(p);

                    NormalManagerDB.Instance.add(0, new CmdItemBuyShopLog(Player.UserInfo.UID, bi), null, this);
                }
                else
                {
                    _smp.LogManager.Instance.push(new AppMessage("[Lobby::RequestGiftItemShop][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou presentear para o Normal [UID=" + (uid_to_send) + "] um item, mas nao enviou nenhum item no Request. Hacker ou bug.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                    p.init_plain(0x6A);
                    p.WriteUInt32(9);
                    p.WriteUInt64(Player.UserInfo.Statistics.pang);
                    p.WriteUInt64(Player.UserInfo.Cookie);

                    Player.Send(p);
                }
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[Lobby::RequestGiftItemShop][Error] Normal [UID=" + Player.UserInfo.UID + "] error desconhecido: " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x6A);
                p.WriteUInt32(ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) == STDA_ERROR_TYPE.CHANNEL ? ExceptionError.STDA_SYSTEM_ERROR_DECODE(e.getCodeError()) : 10);
                p.WriteUInt64(Player.UserInfo.Statistics.pang);
                p.WriteUInt64(Player.UserInfo.Cookie);

                Player.Send(p);
            }
        } 
    }
}