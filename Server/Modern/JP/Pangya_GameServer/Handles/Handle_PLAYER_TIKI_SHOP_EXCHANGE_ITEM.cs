using Pangya_GameServer.Feature;
using Pangya_GameServer.Manager;
using Pangya_GameServer.Models;
using Pangya_GameServer.Server;
using Pangya_GameServer.Session;
using PangyaAPI.IFF.Handle.JP;
using PangyaAPI.Network;
using PangyaAPI.Network.Core;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using static Pangya_GameServer.Models.DefineConstants;
namespace Pangya_GameServer.Handles
{
    public class Handle_PLAYER_TIKI_SHOP_EXCHANGE_ITEM : HandleBase<Player, Packet_EXAMPLE>
    {
        public override async Task Handle()
        {
            Packet p = new Packet();
            try
            {
                ulong pang = 0Ul;
                uint milage = 0;
                uint tiki_pts = 0;
                uint bonus = 0;
                uint bonus_prob = 0;
                uint[] bonus_minmax = new uint[2];
                string s_item = "";
                List<stItem> v_item = new List<stItem>();
                stItem item = new stItem();
                AchievementSystem sys_achieve = new AchievementSystem();

                item.type = 2;
                item.id = -1;
                item._typeid = MILAGE_POINT_TYPEID;
                item.qntd = 0;
                item.STDA_C_ITEM_QNTD = 0;

                var pWi = Player.Inventory.FindWarehouseItemByTypeid(MILAGE_POINT_TYPEID);

                if (pWi != null && pWi.id != int.MaxValue)
                {
                    item.id = pWi.id;
                    item.qntd = (item.STDA_C_ITEM_QNTD = pWi.STDA_C_ITEM_QNTD);
                }

                uint count = Packet.ReadUInt32();

                if (count == 0 || count > 5)
                    throw new exception($"[[Lobby.Room::RequestTikiShopExchangeItem][CHEAT] UID={Player.UserInfo.UID} count={count}", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 450, 5200451));

                if (Packet.Size < count * 8)
                    throw new exception($"[[Lobby.Room::RequestTikiShopExchangeItem][CHEAT] pacote truncado UID={Player.UserInfo.UID}", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 450, 5200452));

                var r = Player.GetRoom();

                for (var i = 0; i < count; ++i)
                {
                    var tsei = new TikiShopExchangeItem().ToRead(Packet);
                    var _item = ItemManager.exchangeTikiShop(Player, tsei._typeid, tsei.id, tsei.qntd);

                    if (_item.Count == 0)
                    {
                        throw new exception("[Lobby.Room::RequestTikiShopExchangeItem][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou trocar item[TYPEID=" + (tsei._typeid) + ", ID=" + (tsei.id) + ", QNTD=" + (tsei.qntd) + "] no Tiki's Shop, mas nao conseguiu inicializar o item. Hacker ou Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 900, 0x52000901));
                    }

                    if (r != null && r.CheckPersonalShopItem(Player, tsei.id))
                    {
                        throw new exception("[Lobby.Room::RequestTikiShopExchangeItem][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou trocar item[TYPEID=" + (tsei._typeid) + ", ID=" + (tsei.id) + ", QNTD=" + (tsei.qntd) + "] no Tiki's Shop, mas o item esta sendo vendido no Personal ShopRoom dele. Hacker ou Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 1010, 0x5201010));
                    }

                    var @base = sIff.Instance.findCommomItem(tsei._typeid);

                    if (@base == null || @base.ID == 0)
                    {
                        throw new exception("[Lobby.Room::RequestTikiShopExchangeItem][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou trocar item[TYPEID=" + (tsei._typeid) + ", ID=" + (tsei.id) + "] no Tiki's Shop, mas o item nao existe no IFF_STRUCT do Server. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 901, 0x5200902));
                    }

                    if (@base.ID != 0 && !@base.tiki.IsActived())
                    {
                        throw new exception("[Lobby.Room::RequestTikiShopExchangeItem][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou trocar item[TYPEID=" + (tsei._typeid) + ", ID=" + (tsei.id) + "] no Tiki's Shop, mas o item nao é valido para ser trocado. Hacker ou Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 904, 0x5200905));
                    }

                    pang += @base.tiki.Tiki_Pang;
                    milage += @base.tiki.Mileage_Pts * tsei.qntd;
                    bonus_minmax[0] += (uint)@base.tiki.Bonus[0];
                    bonus_minmax[1] += (uint)@base.tiki.Bonus[1];
                    bonus_prob = @base.tiki.Bonus_Prob;
                    v_item.AddRange(_item);

                    switch (@base.tiki.Type_TikiShop)
                    {
                        case 1: sys_achieve.incrementCounter(0x6C4000BEu); break;
                        case 2: sys_achieve.incrementCounter(0x6C4000BFu); break;
                        case 3: sys_achieve.incrementCounter(0x6C4000C0u); break;
                    }

                    var s_ids = "";
                    for (int ii = 0; ii < v_item.Count; ++ii)
                        s_ids += (ii == 0 ? "" : ", ") + v_item[ii].id;

                    s_item += (i == 0 ? "" : ", ") + "[TYPEID=" + tsei._typeid + ", ID(s)={" + s_ids + "}, QNTD=" + tsei.qntd + ", TIPO(Normal, CP, Rare)=" + @base.tiki.Type_TikiShop + "]";
                }

                Random rnd = new Random();
                uint index = (uint)rnd.Next() % (bonus_prob * 3 + 1);

                if (index < bonus_prob)
                {
                    bonus = (uint)rnd.Next((int)bonus_minmax[0], (int)bonus_minmax[1]);
                    sys_achieve.incrementCounter(0x6C4000C1u);
                }

                if (ItemManager.removeItem(v_item, Player) <= 0)
                {
                    throw new exception("[Lobby.Room::RequestTikiShopExchangeItem][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou trocar item(ns)(" + s_item + "), mas nao conseguiu deletar ele(s).", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 902, 0x5200903));
                }

                if ((milage + item.qntd + bonus) > 1000)
                    tiki_pts = (milage + (uint)item.qntd + bonus) / 1000;

                item.STDA_C_ITEM_QNTD = (short)((int)((milage + item.qntd + bonus) % 1000) - (int)item.qntd);
                item.qntd = Math.Abs(item.STDA_C_ITEM_QNTD);

                if (item.STDA_C_ITEM_QNTD != 0)
                {
                    var rt = ItemManager.addItem(item, Player, 0, 0);
                    if (rt < 0) throw new exception("[Lobby.Room::RequestTikiShopExchangeItem][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou adicionar item[TYPEID=" + (item._typeid) + ", ID=" + (item.id) + ", QNTD=" + (item.STDA_C_ITEM_QNTD) + "], mas nao conseguiu.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 903, 0x5200904));
                    if (rt != RetAddItem.SUCCESS_PANG_AND_EXP_AND_CP_POUCH) v_item.Add(item);
                }

                if (tiki_pts > 0)
                {
                    item = new stItem();
                    item.type = 2;
                    item.id = -1;
                    item._typeid = TIKI_POINT_TYPEID;
                    item.qntd = (int)tiki_pts;
                    item.STDA_C_ITEM_QNTD = (short)tiki_pts;

                    pWi = Player.Inventory.FindWarehouseItemByTypeid(TIKI_POINT_TYPEID);
                    if (pWi != null) item.id = pWi.id;

                    var rt = ItemManager.addItem(item, Player, 0, 0);
                    if (rt < 0) throw new exception("[Lobby.Room::RequestTikiShopExchangeItem][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou adicionar item[TYPEID=" + (item._typeid) + ", ID=" + (item.id) + ", QNTD=" + (item.STDA_C_ITEM_QNTD) + "], mas nao conseguiu.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 903, 0x5200904));

                    if (item.id == -1) _smp.LogManager.Instance.push(new AppMessage("[TikiShopExchangeItem][Bug] Normal [UID=" + Player.UserInfo.UID + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));
                    if (rt != RetAddItem.SUCCESS_PANG_AND_EXP_AND_CP_POUCH) v_item.Add(item);

                    sys_achieve.incrementCounter(0x6C4000C2u, (int)tiki_pts);
                }

                Player.UserInfo.consomePang(pang);

                p.init_plain(0xC8);
                p.WriteUInt64(Player.UserInfo.Statistics.pang);
                p.WriteUInt64(pang);
                Player.Send(p);

                p.init_plain(0x216);
                p.WriteUInt32((uint)UtilTime.GetSystemTimeAsUnix());
                p.WriteUInt32((uint)v_item.Count);
                foreach (var el in v_item)
                {
                    p.WriteByte(el.type);
                    p.WriteUInt32(el._typeid);
                    p.WriteInt32(el.id);
                    p.WriteUInt32(el.flag_time);
                    p.WriteInt32(el.stat.qntd_ant);
                    p.WriteInt32(el.stat.qntd_dep);
                    p.WriteInt32((el.STDA_C_ITEM_TIME > 0) ? el.STDA_C_ITEM_TIME : el.STDA_C_ITEM_QNTD);
                    p.WriteZero(25);
                }
                Player.Send(p);

                p.init_plain(0x274);
                p.WriteUInt32(0);
                p.WriteUInt32(milage);
                p.WriteUInt32(bonus);
                Player.Send(p);

                sys_achieve.finish_and_update(Player);
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[Lobby.Room::RequestTikiShopExchangeItem][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                p.init_plain(0x274);
                p.WriteUInt32((ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) == STDA_ERROR_TYPE.CHANNEL) ? ExceptionError.STDA_SYSTEM_ERROR_DECODE(e.getCodeError()) : 0x5200900);
                Player.Send(p);
            }
        }
    }
}