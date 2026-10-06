using Pangya_GameServer.Feature;
using Pangya_GameServer.Flags;
using Pangya_GameServer.Manager;
using Pangya_GameServer.Models;
using Pangya_GameServer.PacketFunc;
using Pangya_GameServer.Repository;
using Pangya_GameServer.Server;
using Pangya_GameServer.Session;
using PangyaAPI.IFF.Flags;
using PangyaAPI.IFF.Handle.JP;
using PangyaAPI.IFF.Regions.JP.Models.Generic;
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
    public class Handle_PLAYER_SEND_MAIL : HandleBase<Player, Packet_EXAMPLE>
    {
        public override async Task Handle()
        {
            Packet p = new Packet();
            var m_ci = Player.GetChannel();
            try
            {
                uint from_uid = Packet.ReadUInt32();
                uint to_uid = Packet.ReadUInt32();
                string to_nick = Packet.ReadString();
                ushort unknown_opt = Packet.ReadUInt16();
                string to_msg = Packet.ReadString();
                ulong pang_price = Packet.ReadUInt64();
                byte count_item = Packet.ReadByte();

                if (string.IsNullOrEmpty(to_nick))
                    throw new exception("[Handle_PLAYER_SEND_MAIL][Error] Normal[UID=" + (Player.UserInfo.UID) + ", ID: " + Player.UserInfo.Login + " ] tentou contra o server[MESSAGE="
                            + to_nick + "], vazio. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 1, 1/*UNKNOWN ERROR*/));

                if (!Tools.Sanitize(to_nick))
                    throw new exception("[Handle_PLAYER_SEND_MAIL][Error] Normal[UID=" + (Player.UserInfo.UID) + ", ID: " + Player.UserInfo.Login + " ] tentou contra o server[MESSAGE="
                            + to_nick + "], tentativa de inject. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 1, 1/*UNKNOWN ERROR*/));

                if (string.IsNullOrEmpty(to_msg))
                    throw new exception("[Handle_PLAYER_SEND_MAIL][Error] Normal[UID=" + (Player.UserInfo.UID) + ", ID: " + Player.UserInfo.Login + " ] tentou contra o server[MESSAGE="
                            + to_msg + "], vazio. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 1, 1/*UNKNOWN ERROR*/));

                if (!Tools.Sanitize(to_msg))
                    throw new exception("[Handle_PLAYER_SEND_MAIL][Error] Normal[UID=" + (Player.UserInfo.UID) + ", ID: " + Player.UserInfo.Login + " ] tentou contra o server[MESSAGE="
                            + to_msg + "], tentativa de inject. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 1, 1/*UNKNOWN ERROR*/));

                if (count_item > 0)
                {
                    if (count_item > 4)
                    {
                        throw new exception("[Handle_PLAYER_SEND_MAIL][Error] Normal [UID=" + (Player.UserInfo.UID) + ", ID: " + Player.UserInfo.Login + " ] tentou enviar um RoomID[value=" + (count_item) + "] de itens é maior que o permitido. Bug ou Hacker", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MAIL_BOX_MANAGER,
                            150, 5100081));
                    }

                    if (pang_price != (ulong)(count_item * 500))
                    {
                        throw new exception("[Handle_PLAYER_SEND_MAIL][Error] Normal [UID=" + (Player.UserInfo.UID) + ", ID: " + Player.UserInfo.Login + " ] tentou usar Pang price[value_client=" + (count_item) + ", value_srv=" + (count_item * 500) + "] send AppMessage is wrong. Bug ou Hacker", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MAIL_BOX_MANAGER,
                            153, 5100084));
                    }

                    EmailInfo.ItemGift[] aItem = Tools.InitializeWithDefaultInstances<EmailInfo.ItemGift>(count_item);
                    List<stItem> v_item = new List<stItem>();
                    stItem item = new stItem();

                    for (int i = 0; i < count_item; i++)
                    {
                        aItem[i] = new EmailInfo.ItemGift().ToRead(Packet);
                    }

                    IFFCommon pBase = null;

                    var r = Player.GetRoom();

                    for (var i = 0; i < count_item; ++i)
                    {
                        var group = sIff.Instance.getItemGroupIdentify(aItem[i]._typeid);

                        if (group != IFF_GROUP.BALL
                            && group != IFF_GROUP.CLUBSET
                            && group != IFF_GROUP.ITEM
                            && group != IFF_GROUP.PART)
                        {
                            throw new exception("[Handle_PLAYER_SEND_MAIL][Error] Normal [UID=" + (Player.UserInfo.UID) + ", ID: " + Player.UserInfo.Login + " ] tentou enviar um item[TYPEID=" + (aItem[i]._typeid) + ", ID=" + (aItem[i].id) + "] para o Normal [UID=" + (to_uid) + "], mas esse item nao pode ser enviado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MAIL_BOX_MANAGER,
                                154, 5100085));
                        }

                        pBase = sIff.Instance.findCommomItem(aItem[i]._typeid);

                        if (pBase == null)
                        {
                            throw new exception("[Handle_PLAYER_SEND_MAIL][Error] Normal [UID=" + (Player.UserInfo.UID) + ", ID: " + Player.UserInfo.Login + " ] tentou enviar um item[TYPEID=" + (aItem[i]._typeid) + ", ID=" + (aItem[i].id) + "] para o Normal [UID=" + (to_uid) + "], mas esse item nao tem no STRUCT IFF do server. Bug ou Hacker", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MAIL_BOX_MANAGER,
                                151, 5100082));
                        }

                        if (!pBase.Shop.flag_shop.can_send_mail_and_personal_shop)
                        {
                            throw new exception("[Handle_PLAYER_SEND_MAIL][Error] Normal [UID=" + (Player.UserInfo.UID) + ", ID: " + Player.UserInfo.Login + " ] tentou enviar um item[TYPEID=" + (aItem[i]._typeid) + ", ID=" + (aItem[i].id) + "] para o Normal [UID=" + (to_uid) + "], mas esse item nao é permitido ser enviado por mail. Bug ou Hacker", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MAIL_BOX_MANAGER,
                                152, 5100083));
                        }

                        if (!sIff.Instance.IsCanOverlapped(pBase.ID) && ItemManager.ownerItem(to_uid, pBase.ID))
                        {
                            throw new exception("[Handle_PLAYER_SEND_MAIL][Error][Sucess] Normal [UID=" + (Player.UserInfo.UID) + ", ID: " + Player.UserInfo.Login + " ] tentou enviar um item[TYPEID=" + (pBase.ID) + ", ID=" + (aItem[i].id) + "] que o outro Normal [UID=" + (to_uid) + "] ja tem esse item.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MAIL_BOX_MANAGER,
                                156, 5100087));
                        }

                        item = new stItem();

                        var pWi = Player.Inventory.FindWarehouseItemByTypeid(aItem[i]._typeid);

                        if (pWi == null)
                        {
                            throw new exception("[Handle_PLAYER_SEND_MAIL][Error] Normal [UID=" + (Player.UserInfo.UID) + ", ID: " + Player.UserInfo.Login + " ] tentou enviar um item[TYPEID=" + (aItem[i]._typeid) + ", ID=" + (aItem[i].id) + "] para o Normal [UID=" + (to_uid) + "], mas ele nao tem esse item. Bug ou Hacker", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MAIL_BOX_MANAGER,
                                157, 5100088));
                        }

                        if (r != null && r.CheckPersonalShopItem(Player, (int)aItem[i].id))
                        {
                            throw new exception("[Handle_PLAYER_SEND_MAIL][Error] Normal [UID=" + (Player.UserInfo.UID) + ", ID: " + Player.UserInfo.Login + " ] tentou enviar o item[TYPEID=" + (aItem[i]._typeid) + ", ID=" + (aItem[i].id) + "] para o Normal [UID=" + (to_uid) + "], mas o item esta sendo vendido no Personal ShopRoom dele. Hacker ou Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                1010, 0x5201010));
                        }

                        if (group == IFF_GROUP.ITEM)
                        {
                            if (aItem[i].qntd > 99)
                            {
                                throw new exception("[Handle_PLAYER_SEND_MAIL][Error] Normal [UID=" + (Player.UserInfo.UID) + ", ID: " + Player.UserInfo.Login + " ] tentou enviar um item[TYPEID=" + (aItem[i]._typeid) + ", ID=" + (aItem[i].id) + "] para o Normal [UID=" + (to_uid) + "], mas a quantidade[value=" + (aItem[i].qntd) + "] maior que 99. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MAIL_BOX_MANAGER,
                                    155, 5100086));
                            }

                            if (pWi.STDA_C_ITEM_QNTD < aItem[i].qntd)
                            {
                                throw new exception("[Handle_PLAYER_SEND_MAIL][Error] Normal [UID=" + (Player.UserInfo.UID) + ", ID: " + Player.UserInfo.Login + " ] tentou enviar um item[TYPEID=" + (aItem[i]._typeid) + ", ID=" + (aItem[i].id) + "] para o Normal [UID=" + (to_uid) + "], mas ele nao tem quantidade[value=" + (pWi.STDA_C_ITEM_QNTD) + ", req=" + (aItem[i].qntd) + "] suficiente. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MAIL_BOX_MANAGER,
                                    158, 5100089));
                            }
                        }

                        item.id = (int)aItem[i].id;
                        item._typeid = aItem[i]._typeid;
                        item.flag_time = aItem[i].flag_time;
                        item.STDA_C_ITEM_QNTD = (short)(item.qntd = (int)aItem[i].qntd);
                        item.STDA_C_ITEM_TIME = (short)(short)aItem[i].tempo_qntd;
                        item.ucc.IDX = aItem[i].ucc_img_mark;
                        item.type = 2;

                        v_item.Add(new stItem(item));
                    }

                    if (ItemManager.giveItem(v_item, Player, 1) <= 0)
                    {
                        throw new exception("[Handle_PLAYER_SEND_MAIL][Error] Normal [UID=" + (Player.UserInfo.UID) + ", ID: " + Player.UserInfo.Login + " ] nao conseguiu presentear o Normal [UID=" + (to_uid) + "]", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MAIL_BOX_MANAGER,
                            159, 5100090));
                    }

                    Player.Send(Handle_PACKET_RESPONSE.pacote216(v_item));

                    var msg_id = MailManager.SendMailWithItem(from_uid,
                        to_uid, to_msg, aItem,
                        count_item);

                    Player.UserInfo.consomePang(pang_price);

                    string log_itens = "";
                    foreach (var el in v_item)
                    {
                        if (log_itens.empty())
                        {
                            log_itens += "";
                        }
                        log_itens += "[TYPEID=" + (el._typeid) + ", ID=" + (el.id) + ", FLAG_TIME=" + ((ushort)el.flag_time) + ", QNTD=" + ((el.STDA_C_ITEM_TIME > 0 ? el.STDA_C_ITEM_TIME : el.STDA_C_ITEM_QNTD)) + ", QNTD_DEPOIS=" + (el.stat.qntd_dep) + "]";
                    }

                    _smp.LogManager.Instance.push(new AppMessage("[Handle_PLAYER_SEND_MAIL][Sucess] Normal [UID=" + (Player.UserInfo.UID) + ", ID: " + Player.UserInfo.Login + " ] enviou presente para o Normal [UID=" + (to_uid) + "] MailBox[Email_ID=" + (msg_id) + ", Message=" + to_msg + "] item(ns)[QNTD=" + (v_item.Count) + "] Item(ns){" + log_itens + "}", type_msg.CL_ONLY_FILE_LOG));

                    p.init_plain(0xC8);
                    p.WriteUInt64(Player.UserInfo.Statistics.pang);
                    p.WriteUInt64(pang_price);
                    Player.Send(p);

                    p.init_plain(0x213);
                    p.WriteUInt32(0);
                    Player.Send(p);
                }
                else
                {
                    if (pang_price != 100)
                    {
                        throw new exception("[Handle_PLAYER_SEND_MAIL][Error] Normal [UID=" + (Player.UserInfo.UID) + ", ID: " + Player.UserInfo.Login + " ] tentou usar Pang price[value_client=" + (count_item) + ", value_srv=" + (100) + "] send AppMessage is wrong. Bug ou Hacker", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MAIL_BOX_MANAGER,
                            153, 5100084));
                    }

                    var msg_id = MailManager.SendMail(from_uid,
                        to_uid, to_msg);

                    Player.UserInfo.consomePang(pang_price);

                    p.init_plain(0xC8);
                    p.WriteUInt64(Player.UserInfo.Statistics.pang);
                    p.WriteUInt64(pang_price);
                    Player.Send(p);

                    p.init_plain(0x213);
                    p.WriteUInt32(0);
                    Player.Send(p);
                }
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[Handle_PLAYER_SEND_MAIL][ErrorSystem] Normal[UID= " + Player.UserInfo.UID + ", ID: " + Player.UserInfo.Login + " ] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x213);
                p.WriteUInt32((ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) == STDA_ERROR_TYPE.CHANNEL) ? ExceptionError.STDA_SYSTEM_ERROR_DECODE(e.getCodeError()) : 0x5500300);
                Player.Send(p);
            }
        }
    }
}