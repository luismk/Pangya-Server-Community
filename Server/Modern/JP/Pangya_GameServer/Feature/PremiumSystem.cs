using Pangya_GameServer.Engine;
using Pangya_GameServer.Feature;
using Pangya_GameServer.Flags;
using Pangya_GameServer.Manager;
using Pangya_GameServer.Models;
using Pangya_GameServer.PacketFunc;
using Pangya_GameServer.Repository;
using Pangya_GameServer.Session;
using PangyaAPI.DataBase;
using PangyaAPI.IFF.Flags;
using PangyaAPI.IFF.Handle.JP;
using PangyaAPI.Network;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using static Pangya_GameServer.Models.DefineConstants;
namespace Pangya_GameServer.Feature
{
    public class PremiumSystem
    {

        public PremiumSystem() { }

        // Simulação do macro CHECK_SESSION_BEGIN
        private void CHECK_SESSION_BEGIN(Player _session, string method)
        {
            if (!_session.Connected)
                throw new Exception($"[PremiumSystem{method}][Error] player nao esta connectado.");
        }

        public void CheckEndTimeTicket(Player _session)
        {
            CHECK_SESSION_BEGIN(_session, "CheckEndTimeTicket");
            try
            {
                if (isPremiumTicket(_session.Inventory.PremiumTicket._typeid) && _session.Inventory.PremiumTicket.id != 0 && _session.Inventory.PremiumTicket.unix_sec_date <= 0)
                {
                    WarehouseItemEx ticket = null;
                    // Procura o item no map/dictionary do player
                    var it = _session.Inventory.WarehouseItems.Values.FirstOrDefault(x => x._typeid == _session.Inventory.PremiumTicket._typeid);

                    if (it == null)
                    {
                        ticket = ItemManager._ownerItem(_session.Inventory.uid, _session.Inventory.PremiumTicket._typeid);
                        if (ticket.id <= 0)
                        {
                            _smp.LogManager.Instance.push(new AppMessage("[PremiumSystem::CheckEndTimeTicket][Error] player[UID=" + _session.Inventory.uid + "] nao tem o item Ticket Premium. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                            return;
                        }
                        _session.Inventory.WarehouseItems.Add(ticket.id, ticket);
                    }
                    else
                    {
                        ticket = it;
                    }

                    stItem item = new stItem();
                    item.type = 2;
                    item.id = ticket.id;
                    item._typeid = ticket._typeid;
                    item.qntd = (int)ticket.STDA_C_ITEM_QNTD;
                    item.STDA_C_ITEM_QNTD = (short)(item.qntd * -1);

                    if (ItemManager.removeItem(item, _session) <= 0)
                        throw new Exception("[PremiumSystem::CheckEndTimeTicket][Error] player[UID=" + _session.Inventory.uid + "] tentou excluir ticket premium.");

                    _smp.LogManager.Instance.push(new AppMessage("[PremiumSystem::CheckEndTimeTicket][Log] Player[UID=" + _session.Inventory.uid + "].\tExcluiu ticket premium do player.", type_msg.CL_ONLY_FILE_LOG));


                    _session.Send(Handle_PACKET_RESPONSE.pacote26D(_session.Inventory.PremiumTicket.unix_end_date));

                    _session.Inventory.PremiumTicket.clear();
                }
            }
            catch (Exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[PremiumSystem::CheckEndTimeTicket][ErrorSystem] " + e.Message, type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void addPremiumUser(Player _session, WarehouseItemEx _ticket, uint _time)
        {
            CHECK_SESSION_BEGIN(_session, "addPremiumUser");
            try
            {
                _session.Inventory.PremiumTicket.id = _ticket.id;
                _session.Inventory.PremiumTicket._typeid = _ticket._typeid;
                _session.Inventory.PremiumTicket.unix_end_date = (int)_ticket.end_date_unix_local;
                _session.Inventory.PremiumTicket.unix_sec_date = (int)(_ticket.end_date_unix_local - UtilTime.GetLocalTimeAsUnix());

                _smp.LogManager.Instance.push(new AppMessage("[PremiumSystem::addPremiumUser][Log][UID=" + _session.Inventory.uid + "] eh um Premium User por (" + _time + ") Dias", type_msg.CL_FILE_LOG_AND_CONSOLE));

                List<stItem> add_itens = new List<stItem>();
                _session.UserInfo.UserCapabilities.UserPremium = true;

                var new_ball = addPremiumBall(_session);
                if (new_ball._typeid != 0) add_itens.Add(new_ball);

                if (isPremium2(_session.Inventory.PremiumTicket._typeid))
                {
                    addPremiumClubSet(_session, _time);
                    var new_mascot = addPremiumMascot(_session, _time);
                    if (new_mascot._typeid != 0) add_itens.Add(new_mascot);

                    _session.Send(_session.Inventory.WarehouseItems.Build());
                }

                var p = new Packet(0x9A);
                p.WriteInt32(_session.UserInfo.UserCapabilities.Value);
                _session.Send(p);

                if (add_itens.Count > 0)
                {
                    p.init_plain(0x216);
                    p.WriteUInt32((uint)UtilTime.GetSystemTimeAsUnix());
                    p.WriteUInt32((uint)add_itens.Count);

                    foreach (var el in add_itens)
                    {
                        p.WriteByte(el.type);
                        p.WriteUInt32(el._typeid);
                        p.WriteUInt32((uint)el.id);
                        p.WriteUInt32(el.flag_time);
                        p.WriteBytes(el.stat.ToArray()); // Assume helper ToArray() para struct stat
                        p.WriteInt32((el.flag_time == 0) ? el.STDA_C_ITEM_QNTD : el.STDA_C_ITEM_TIME);
                        p.WriteZero(25);
                    }
                    _session.Send(p);
                }
            }
            catch (Exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[PremiumSystem::addPremiumUser][ErrorSystem] " + e.Message, type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void removePremiumUser(Player _session)
        {
            CHECK_SESSION_BEGIN(_session, "removePremiumUser");
            try
            { 
                removePremiumBall(_session);
                _session.UserInfo.UserCapabilities.UserPremium = false;

                _session.Send(Handle_PACKET_RESPONSE.pacote09A(_session.UserInfo.UserCapabilities.Value));

                // UPDATE ON GAME - Mostra a mensagem que acabou o tempo do ticket premium

                _session.Send(Handle_PACKET_RESPONSE.pacote26D(_session.Inventory.PremiumTicket.unix_end_date));

                _session.Inventory.PremiumTicket.clear();

                _smp.LogManager.Instance.push(new AppMessage("[PremiumSystem::removePremiumUser][Log] player[UID=" + _session.Inventory.uid + "] removeu o Premium User...", type_msg.CL_FILE_LOG_AND_CONSOLE));

                using (var p = new Packet(0x40))   // Msg to Chat of player
                {
                    p.WriteByte(7);  // Notice

                    p.WriteString(_session.UserInfo.NickName);
                    p.WriteString("voce nao e mais premium.");

                    _session.Send(p);

                }
            }
            catch (Exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[PremiumSystem::removePremiumUser][ErrorSystem] " + e.Message, type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public stItem addPremiumBall(Player _session)
        {
            CHECK_SESSION_BEGIN(_session, "addPremiumBall");
            stItem item = new stItem();
            try
            {
                uint ball = getPremiumBallByTicket(_session.Inventory.PremiumTicket._typeid);
                WarehouseItemEx new_wi = new WarehouseItemEx();
                new_wi.id = -1;
                new_wi._typeid = ball;
                new_wi.STDA_C_ITEM_QNTD = 1;
                new_wi.type = 0x6A;
                new_wi.clubset_workshop.level = -1;

                _session.Inventory.WarehouseItems.Add(new_wi.id, new_wi);
                _session.Inventory.UserEquipment.ball_typeid = ball;
                _session.Inventory.UserEquippedItem.Ball_WI = new_wi;

                item.type = 2;
                item.id = new_wi.id;
                item._typeid = new_wi._typeid;
                item.flag_time = (byte)new_wi.type;
                item.stat.qntd_ant = 0;
                item.stat.qntd_dep = 1;
                item.qntd = 1;
                item.STDA_C_ITEM_QNTD = (short)item.qntd;
            }
            catch (Exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[PremiumSystem::addPremiumBall][ErrorSystem] " + e.Message, type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
            return item;
        }

        public stItem addPremiumClubSet(Player _session, uint _time)
        {
            CHECK_SESSION_BEGIN(_session, "addPremiumClubSet");
            stItem item = new stItem();
            try
            {
                uint clubset = getPremiumClubSetByTicket(_session.Inventory.PremiumTicket._typeid);
                if (_session.Inventory.FindWarehouseItemByTypeid(clubset) != null) return item;

                BuyItem bi = new BuyItem();
                bi.id = -1;
                bi._typeid = clubset;
                bi.qntd = 1;
                bi.time = (short)_time;

                ItemManager.initItemFromBuyItem(_session.UserInfo, item, bi, false, 0, 0, 1);
                if (item._typeid == 0u) throw new Exception("Erro inicializar ClubSet");

                if (ItemManager.addItem(item, _session, 0, 0) < 0) throw new Exception("Erro adicionar ClubSet");

                var new_wi = _session.Inventory.FindWarehouseItemById(item.id);
                new_wi.STDA_C_ITEM_TIME = 0;

                if (isPremium2(_session.Inventory.PremiumTicket._typeid))
                {
                    clubset = PREMIUM_3_CLUBSET_TYPEID;
                    bi.id = -1;
                    bi._typeid = clubset;
                    ItemManager.initItemFromBuyItem(_session.UserInfo, item, bi, false, 0, 0, 1);
                    ItemManager.addItem(item, _session, 0, 0);
                    new_wi = _session.Inventory.FindWarehouseItemById(item.id);
                    new_wi.STDA_C_ITEM_TIME = 0;
                }
            }
            catch (Exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[PremiumSystem::addPremiumClubSet][ErrorSystem] " + e.Message, type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
            return item;
        }

        public stItem addPremiumMascot(Player _session, uint _time)
        {
            CHECK_SESSION_BEGIN(_session, "addPremiumMascot");
            stItem item = new stItem();
            try
            {
                uint mascot = getPremiumMascotByTicket(_session.Inventory.PremiumTicket._typeid);
                if (_session.Inventory.FindWarehouseItemByTypeid(mascot) != null) return item;

                BuyItem bi = new BuyItem();
                bi.id = -1; bi._typeid = mascot; bi.qntd = 1; bi.time = (short)_time;

                ItemManager.initItemFromBuyItem(_session.UserInfo, item, bi, false, 0, 0, 1);
                ItemManager.addItem(item, _session, 0, 0);
            }
            catch (Exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[PremiumSystem::addPremiumMascot][ErrorSystem] " + e.Message, type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
            return item;
        }

        public void addPremiumBox(Player _session)
        {
            CHECK_SESSION_BEGIN(_session, "addPremiumBox");
            try
            {
                uint _typeid = getPremiumBoxByTicket(_session.Inventory.PremiumTicket._typeid);
                uint _qntd = getBoxQntdByTicket(_session.Inventory.PremiumTicket._typeid);
                if (_qntd > 0)
                {
                    stItem item = new stItem();
                    BuyItem bi = new BuyItem();
                    bi.id = -1; bi._typeid = _typeid; bi.qntd = (uint)_qntd;

                    ItemManager.initItemFromBuyItem(_session.UserInfo, item, bi, false, 0, 0, 1);
                   MailManager.SendMessageWithItem(0, _session.Inventory.uid, "Premium System - Gift Box", item);
                }
            }
            catch (Exception e) { _smp.LogManager.Instance.push(new AppMessage("[PremiumSystem::addPremiumBox][ErrorSystem] " + e.Message, type_msg.CL_FILE_LOG_AND_CONSOLE)); }
        }

        public stItem addPremiumTitle(Player _session, uint _time)
        {
            CHECK_SESSION_BEGIN(_session, "addPremiumTitle");
            stItem item = new stItem();
            try
            {
                uint ball = getPremiumTitleByTicket(_session.Inventory.PremiumTicket._typeid);
                WarehouseItemEx new_wi = new WarehouseItemEx();
                new_wi.id = -1; new_wi._typeid = ball; new_wi.STDA_C_ITEM_QNTD = 1; new_wi.type = 0x6A;

                _session.Inventory.WarehouseItems.Add(new_wi.id, new_wi);
                item.type = 2; item.id = new_wi.id; item._typeid = new_wi._typeid; item.flag_time = (byte)new_wi.type;
                item.qntd = 1; item.STDA_C_ITEM_QNTD = (short)item.qntd;
            }
            catch (Exception e) { _smp.LogManager.Instance.push(new AppMessage("[PremiumSystem::addPremiumTitle][ErrorSystem] " + e.Message, type_msg.CL_FILE_LOG_AND_CONSOLE)); }
            return item;
        }

        public void removePremiumBall(Player _session)
        {
            CHECK_SESSION_BEGIN(_session, "removePremiumBall");
            try
            {
                uint ball = getPremiumBallByTicket(_session.Inventory.PremiumTicket._typeid);
                var pair = _session.Inventory.WarehouseItems.FirstOrDefault(x => x.Value._typeid == ball);
                if (pair.Value != null)
                {
                    stItem item = new stItem();
                    item.type = 2; item.id = pair.Value.id; item._typeid = pair.Value._typeid;
                    item.qntd = 1; item.STDA_C_ITEM_QNTD = -1; item.flag_time = 0x6A;

                    _session.Inventory.WarehouseItems.Remove(pair.Key);

                    var p = new Packet(0x216);
                    p.WriteUInt32((uint)UtilTime.GetSystemTimeAsUnix());
                    p.WriteUInt32(1);
                    p.WriteByte(item.type); p.WriteUInt32(item._typeid); p.WriteInt32(item.id);
                    p.WriteUInt32(item.flag_time); p.WriteBytes(item.stat.ToArray());
                    p.WriteUInt32(item.STDA_C_ITEM_QNTD < 0 ? 1u : (uint)item.STDA_C_ITEM_QNTD);
                    p.WriteZero(25);
                    _session.Send(p);
                }
            }
            catch (Exception e) { _smp.LogManager.Instance.push(new AppMessage("[PremiumSystem::removePremiumBall][ErrorSystem] " + e.Message, type_msg.CL_FILE_LOG_AND_CONSOLE)); }
        }

        public void removePremiumTitle(Player _session)
        {
            CHECK_SESSION_BEGIN(_session, "removePremiumTitle");
            try
            {
                uint ball = getPremiumTitleByTicket(_session.Inventory.PremiumTicket._typeid);
                var pair = _session.Inventory.WarehouseItems.FirstOrDefault(x => x.Value._typeid == ball);
                if (pair.Value != null)
                {
                    stItem item = new stItem();
                    item.type = 2; item.id = pair.Value.id; item._typeid = pair.Value._typeid;
                    item.qntd = 1; item.STDA_C_ITEM_QNTD = 1; item.flag_time = 0x6A;

                    _session.Inventory.WarehouseItems.Remove(pair.Key);

                    var p = new Packet(0x216);
                    p.WriteUInt32((uint)UtilTime.GetSystemTimeAsUnix());
                    p.WriteUInt32(1);
                    p.WriteByte(item.type); p.WriteUInt32(item._typeid); p.WriteInt32(item.id);
                    p.WriteUInt32(item.flag_time); p.WriteBytes(item.stat.ToArray());
                    p.WriteUInt32(1u); // valor simplificado do logic do original
                    p.WriteZero(25);
                    _session.Send(p);
                }
            }
            catch (Exception e) { _smp.LogManager.Instance.push(new AppMessage("[PremiumSystem::removePremiumTitle][ErrorSystem] " + e.Message, type_msg.CL_FILE_LOG_AND_CONSOLE)); }
        }

        public void updatePremiumUser(Player _session)
        {
            CHECK_SESSION_BEGIN(_session, "updatePremiumUser");
            try
            {
                List<stItem> add_itens = new List<stItem>();
                _session.UserInfo.UserCapabilities.UserPremium = true;
                var new_ball = addPremiumBall(_session);
                if (new_ball._typeid != 0u) add_itens.Add(new_ball);

                _session.Send(Handle_PACKET_RESPONSE.pacote09A(_session.UserInfo.UserCapabilities.Value));

                if (add_itens.Count > 0)
                {
                    var p = new Packet();

                    p.init_plain(0x216);
                    p.WriteUInt32((uint)UtilTime.GetSystemTimeAsUnix());
                    p.WriteUInt32((uint)add_itens.Count);
                    foreach (var el in add_itens)
                    {
                        p.WriteByte(el.type); p.WriteUInt32(el._typeid); p.WriteUInt32((uint)el.id);
                        p.WriteUInt32(el.flag_time); p.WriteBytes(el.stat.ToArray());
                        p.WriteInt32((el.flag_time == 0) ? (int)el.STDA_C_ITEM_QNTD : el.STDA_C_ITEM_TIME);
                        p.WriteZero(25);
                    }
                    _session.Send(p);
                }
            }
            catch (Exception e) { _smp.LogManager.Instance.push(new AppMessage("[PremiumSystem::updatePremiumUser][ErrorSystem] " + e.Message, type_msg.CL_FILE_LOG_AND_CONSOLE)); }
        }

        // --- Getters e Verificadores ---
        public uint getPremiumBallByTicket(uint _typeid)
        {
            if (_typeid == PREMIUM_TICKET_TYPEID) return PREMIUM_BALL_TYPEID;
            //if (_typeid == PREMIUM_2_TICKET_TYPEID) return PREMIUM_2_BALL_TYPEID;
            return 0;
        }

        public uint getPremiumClubSetByTicket(uint _typeid)
        {
            if (_typeid == PREMIUM_TICKET_TYPEID) return PREMIUM_CLUBSET_TYPEID;
            //if (_typeid == PREMIUM_2_TICKET_TYPEID) return PREMIUM_2_CLUBSET_TYPEID;
            return 0;
        }

        public uint getPremiumMascotByTicket(uint _typeid)
        {
            if (_typeid == PREMIUM_TICKET_TYPEID) return PREMIUM_MASCOT_TYPEID;
            return 0;
        }

        public uint getPremiumTitleByTicket(uint _typeid)
        {
            if (_typeid == PREMIUM_TICKET_TYPEID) return PREMIUM_TITLE_TYPEID;
            return 0;
        }

        public uint getPremiumBoxByTicket(uint _typeid)
        {
            if (_typeid == PREMIUM_TICKET_TYPEID) return PREMIUM_BOX_TYPEID;
            return 0;
        }

        public uint getExpPangRateByTicket(uint _typeid)
        {
            if (_typeid == PREMIUM_TICKET_TYPEID) return 0;
            //if (_typeid == PREMIUM_2_TICKET_TYPEID) return 12;
            return 0;
        }

        public uint getBoxQntdByTicket(uint _typeid)
        {
            if (_typeid == PREMIUM_TICKET_TYPEID) return 4;
            //if (_typeid == PREMIUM_2_TICKET_TYPEID) return 8;
            return 0;
        }

        public bool isPremiumTicket(uint _typeid) => _typeid == PREMIUM_TICKET_TYPEID;
        public bool isPremiumBall(uint _typeid) => _typeid == PREMIUM_BALL_TYPEID || _typeid == PREMIUM_2_BALL_TYPEID;
        public bool isPremium1(uint _typeid) => _typeid == PREMIUM_TICKET_TYPEID;
        public bool isPremium2(uint _typeid) => _typeid == PREMIUM_2_TICKET_TYPEID;
        public bool isPremium(uint _typeid) => isPremium1(_typeid) || isPremium2(_typeid);
    }

    public class sPremiumSystem : Singleton<PremiumSystem>
    {
    }
}