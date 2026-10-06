using System;
using System.Threading.Tasks;
using Pangya_GameServer.Engine;
using Pangya_GameServer.Feature;
using Pangya_GameServer.Flags;
using Pangya_GameServer.Manager;
using Pangya_GameServer.Models;
using Pangya_GameServer.Repository;
using Pangya_GameServer.Server;
using Pangya_GameServer.Session;
using PangyaAPI.IFF.Flags;
using PangyaAPI.IFF.Handle.JP;
using PangyaAPI.IFF.Regions.JP.Models;
using PangyaAPI.IFF.Regions.JP.Models.IFF;
using PangyaAPI.Network;
using PangyaAPI.Network.Core;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using snmdb;
namespace Pangya_GameServer.Handles
{
    public class Handle_PLAYER_CADIE_CAULDRON_EXCHANGE : HandleBase<Player, Packet_EXAMPLE>
    {
        public override async Task Handle()
        {
            Packet p = new Packet();
            var m_ci = Player.GetChannel();
            try
            {
                if (Player.UserInfo.BlockFlag.Flag.CadieRecycle)
                    throw new exception($"[[Lobby.Room::RequestCadieCauldronExchange][BLOCK] UID={Player.UserInfo.UID}", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 8, 0x790001));

                ushort seq = Packet.ReadUInt16();
                uint clientRequested = Packet.ReadUInt32();
                byte count = Packet.ReadByte();

                if (count == 0 || count > 4)
                    throw new exception($"[[Lobby.Room::RequestCadieCauldronExchange][CHEAT] UID={Player.UserInfo.UID} count={count}", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 450, 5200451));

                if (Packet.Size < count * 8)
                    throw new exception($"[[Lobby.Room::RequestCadieCauldronExchange][CHEAT] pacote truncado UID={Player.UserInfo.UID}", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 450, 5200452));

                CadieExchangeItem[] cei = new CadieExchangeItem[count];
                for (int i = 0; i < count; i++)
                    cei[i] = new CadieExchangeItem().ToRead(Packet);

                var cmb = sIff.Instance.findCadieMagicBox((uint)(seq + 1));
                if (cmb == null || cmb.seq != seq + 1 || !cmb.active.IsTrue())
                    throw new exception($"[[Lobby.Room::RequestCadieCauldronExchange][CHEAT] Seq inválida UID={Player.UserInfo.UID}", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 451, 5200452));

                if (Player.UserInfo.Member.GameLevel < cmb.level)
                    throw new exception($"[[Lobby.Room::RequestCadieCauldronExchange][LEVEL] UID={Player.UserInfo.UID}", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 454, 5200455));

                for (int i = 0; i < count; i++)
                {
                    if (cmb.item_trade.ID[i] != 0 && cmb.item_trade.ID[i] != cei[i]._typeid)
                        throw new exception($"[[Lobby.Room::RequestCadieCauldronExchange][CHEAT] item mismatch UID={Player.UserInfo.UID}", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 453, 5200454));

                    cei[i].QtyPerExchange = cmb.item_trade.Qty[i];
                }

                if (ItemManager.isTimeItem(new stItem.stDate.stDateSys(cmb.date.Start, cmb.date.End)) && !ItemManager.betweenTimeSystem(new stItem.stDate.stDateSys(cmb.date.Start, cmb.date.End)))
                    throw new exception("[Lobby.Room::RequestCadieCauldronExchange][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou trocar item no CadieCauldron, mas o item[Seq=" + (seq + 1) + "] nao esta mais na data[temporario]. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 455, 5200456));

                if (cmb.Box_Random_ID == 0 && !sIff.Instance.IsCanOverlapped(cmb.item_receive.ID) && Player.Inventory.ownerItem(cmb.item_receive.ID))
                    throw new exception("[Lobby.Room::RequestCadieCauldronExchange][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou trocar item[Seq=" + (seq + 1) + ", TYPEID_RCV=" + (cmb.item_receive.ID) + "] no Cauldron que ele ja possui e nao pode ter duplicata", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 458, 5200459));

                uint safeExchangeCount = clientRequested;
                for (int i = 0; i < count; i++)
                {
                    uint itemLimit = ItemManager.CalculateSafeExchangeCount(Player, cei[i], safeExchangeCount, 100);
                    if (itemLimit < safeExchangeCount) safeExchangeCount = itemLimit;
                }

                if (safeExchangeCount == 0)
                    throw new exception($"[[Lobby.Room::RequestCadieCauldronExchange][CHEAT] safeExchangeCount=0 UID={Player.UserInfo.UID}", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 902, 0xDEAD0003));

                var r = Player.GetRoom();

                for (int i = 0; i < count; i++)
                {
                    ulong totalQty = (ulong)cmb.item_trade.Qty[i] * safeExchangeCount;
                    if (totalQty > uint.MaxValue)
                        throw new exception($"[[Lobby.Room::RequestCadieCauldronExchange][OVERFLOW] UID={Player.UserInfo.UID}", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 904, 0xDEAD0005));

                    if (ItemManager.exchangeCadieMagicBox(Player, cei[i]._typeid, cei[i].id, (uint)totalQty) <= 0)
                        throw new exception($"[[Lobby.Room::RequestCadieCauldronExchange][Error][CT] troca inválida UID={Player.UserInfo.UID}", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 457, 5200458));

                    if (r != null && r.CheckPersonalShopItem(Player, cei[i].id))
                        throw new exception($"[[Lobby.Room::RequestCadieCauldronExchange][Error] UID={Player.UserInfo.UID}", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 1010, 0x5201010));
                }

                List<stItem> v_remove = new List<stItem>();
                List<stItem> v_item = new List<stItem>();
                stItem item = new stItem();
                BuyItem bi = new BuyItem();
                AchievementSystem sys_achieve = new AchievementSystem();

                for (var i = 0; i < count; ++i)
                {
                    item = new stItem();
                    item.type = 2; item.id = cei[i].id; item._typeid = cei[i]._typeid; item.qntd = (int)(cmb.item_trade.Qty[i] * safeExchangeCount); item.STDA_C_ITEM_QNTD = (short)(item.qntd * -1);
                    v_remove.Add(new stItem(item));
                }

                if (ItemManager.removeItem(v_remove, Player) <= 0)
                    throw new exception("[Lobby.Room::RequestCadieCauldronExchange][Error] problemas ao remover(s) item(ns) do Normal [UID=" + Player.UserInfo.UID + "]", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 461, 5200462));

                if (cmb.Box_Random_ID > 0)
                {
                    var cmbr_iff = sIff.Instance.findCadieMagicBoxRandom(cmb.Box_Random_ID);
                    if (!cmbr_iff.Any()) throw new exception("[Lobby.Room::RequestCadieCauldronExchange][Error] Normal [UID=" + Player.UserInfo.UID + "] CadieMagicBoxRandom[ID=" + (cmb.Box_Random_ID) + "] empty", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 456, 5200457));

                    LotterySystem lottery = new LotterySystem();
                    foreach (var el in cmbr_iff) lottery.Add(el.Value.item_random.Rate, el);
                    var lc = lottery.SpinRoleta();
                    if (lc == null) throw new exception("[Lobby.Room::RequestCadieCauldronExchange][Error] Normal [UID=" + Player.UserInfo.UID + "] nao conseguiu sortear um item do caddie magic box random[ID=" + (cmb.Box_Random_ID) + "]", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 461, 5200462));
                    var cmbr = (CadieMagicBoxRandom)lc.Value;
                    if (cmbr == null) throw new exception("[Lobby.Room::RequestCadieCauldronExchange][Error] Normal [UID=" + Player.UserInfo.UID + "] valor retornado do sorteio is invalid(null)", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 462, 5200463));
                    var item_random = sIff.Instance.findCommomItem(cmbr.item_random.ID);
                    if (item_random == null) throw new exception("[Lobby.Room::RequestCadieCauldronExchange][Error] Normal [UID=" + Player.UserInfo.UID + "] o item random[TYPEID=" + (cmbr.item_random.ID) + "] que esta no IFF_STRUCT do server nao existe no IFF do server. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 463, 5200464));

                    bi.id = -1; bi._typeid = cmbr.item_random.ID; bi.qntd = cmbr.item_random.Qty;
                    bi.time = (short)(item_random.Shop.flag_shop.time_shop.active && item_random.Shop.flag_shop.time_shop.dia > 0 ? item_random.Shop.flag_shop.time_shop.dia : 0);
                }
                else
                {
                    bi.id = -1; bi._typeid = cmb.item_receive.ID; bi.qntd = cmb.item_receive.Qty * safeExchangeCount;
                }

                item = new stItem();
                ItemManager.initItemFromBuyItem(Player.UserInfo, item, bi, false, 0, 0, 1);

                if (ItemManager.isSetItem(item._typeid))
                {
                    var v_stItem = ItemManager.GetItemOfSetItem(Player, item._typeid, false, 1);
                    if (v_stItem.Any()) foreach (var el in v_stItem) if ((sIff.Instance.IsCanOverlapped(el._typeid) && sIff.Instance.getItemGroupIdentify(el._typeid) != IFF_GROUP.CAD_ITEM) || ! Player.Inventory.ownerItem(el._typeid)) v_item.Add(new stItem(el));
                    else throw new exception("[Lobby.Room::RequestCadieCauldronExchange][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou trocar um set item que nao tem item, item typeid: " + (bi._typeid), ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 461, 0x5200062));
                }
                else v_item.Add(new stItem(item));

                if (v_item.Count == 0) throw new exception("[Lobby.Room::RequestCadieCauldronExchange][Error] problemas ao inicializar o item[TYPEID=" + (bi._typeid) + "] para o Normal [UID=" + Player.UserInfo.UID + "]", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 459, 5200460));

                var rai = ItemManager.addItem(v_item, Player, 0, 0);
                if (rai.fails.Count > 0 && rai.type != RetAddItem.SUCCESS_PANG_AND_EXP_AND_CP_POUCH) throw new exception("[Lobby.Room::RequestCadieCauldronExchange][Error] problemas ao adicionar o item[TYPEID=" + (bi._typeid) + "] para o Normal [UID=" + Player.UserInfo.UID + "] ", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 460, 5200461));

                if (item._typeid == 0x1A000083) Player.Inventory.CouponGacha.partial_ticket += item.STDA_C_ITEM_QNTD;
                if (v_item.Count > 0) item = v_item[0];
                if (cmb.Box_Random_ID <= 0 && item._typeid != cmb.item_receive.ID) item._typeid = cmb.item_receive.ID;
                if (rai.type != RetAddItem.SUCCESS_PANG_AND_EXP_AND_CP_POUCH) v_remove.AddRange(v_item);

                p.init_plain(0x216);
                p.WriteUInt32((uint)UtilTime.GetSystemTimeAsUnix());
                p.WriteInt32(v_remove.Count);
                foreach (var el in v_remove)
                {
                    p.WriteByte(el.type); p.WriteUInt32(el._typeid); p.WriteInt32(el.id); p.WriteInt32(el.flag_time); p.WriteBytes(el.stat.ToArray()); p.WriteInt32(el.STDA_C_ITEM_TIME > 0 ? el.STDA_C_ITEM_TIME : el.STDA_C_ITEM_QNTD); p.WriteZero(25);
                }
                Player.Send(p);

                p.init_plain(0x22F);
                p.WriteUInt32(0); p.WriteUInt32(seq); p.WriteUInt32(1);
                p.WriteUInt32(item._typeid); p.WriteInt32(item.id); p.WriteInt32(item.STDA_C_ITEM_QNTD); p.WriteInt32(item.stat.qntd_dep); p.WriteUInt32(item.flag_time);
                Player.Send(p);

                sys_achieve.incrementCounter(0x6C400082u);
                sys_achieve.finish_and_update(Player);
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[Lobby.Room::RequestCadieCauldronExchange][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                p.init_plain(0x22F);
                p.WriteUInt32((ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) == STDA_ERROR_TYPE.CHANNEL) ? ExceptionError.STDA_SYSTEM_ERROR_DECODE(e.getCodeError()) : 5200450);
                Player.Send(p);
            }
        } 
    }
}