using Pangya_GameServer.Game.Manager;
using Pangya_GameServer.Models;
using Pangya_GameServer.PacketFunc;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Models;
using PangyaAPI.Utilities.Log;
using System;
using System.Collections.Generic;
using System.Linq;
using static Pangya_GameServer.Models.DefineConstants;
namespace Pangya_GameServer.Game.System
{
    public class PremiumSystem
    {

        public PremiumSystem() { }

        // Simulação do macro CHECK_SESSION_BEGIN
        private void CHECK_SESSION_BEGIN(Player _session, string method)
        {
            if (!_session.getState())
                throw new Exception($"[PremiumSystem{method}][Error] player nao esta connectado.");
        }

        public void checkEndTimeTicket(Player _session)
        {
            CHECK_SESSION_BEGIN(_session, "checkEndTimeTicket");
            try
            {
                if (isPremiumTicket(_session.m_pi.pt._typeid) && _session.m_pi.pt.id != 0 && _session.m_pi.pt.unix_sec_date <= 0)
                {
                    WarehouseItemEx ticket = null;
                    // Procura o item no map/dictionary do player
                    var it = _session.m_pi.mp_wi.Values.FirstOrDefault(x => x._typeid == _session.m_pi.pt._typeid);

                    if (it == null)
                    {
                        ticket = ItemManager._ownerItem(_session.m_pi.uid, _session.m_pi.pt._typeid);
                        if (ticket.id <= 0)
                        {
                            _smp.message_pool.getInstance().push(new message("[PremiumSystem::checkEndTimeTicket][Error] player[UID=" + _session.m_pi.uid + "] nao tem o item Ticket Premium. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                            return;
                        }
                        _session.m_pi.mp_wi.Add(ticket.id, ticket);
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
                        throw new Exception("[PremiumSystem::checkEndTimeTicket][Error] player[UID=" + _session.m_pi.uid + "] tentou excluir ticket premium.");

                    _smp.message_pool.getInstance().push(new message("[PremiumSystem::checkEndTimeTicket][Log] Player[UID=" + _session.m_pi.uid + "].\tExcluiu ticket premium do player.", type_msg.CL_ONLY_FILE_LOG));


                    packet_func.session_send(packet_func.pacote26D(_session.m_pi.pt.unix_end_date), _session, 0);

                    _session.m_pi.pt.clear();
                }
            }
            catch (Exception e)
            {
                _smp.message_pool.getInstance().push(new message("[PremiumSystem::checkEndTimeTicket][ErrorSystem] " + e.Message, type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void addPremiumUser(Player _session, WarehouseItemEx _ticket, uint _time)
        {
            CHECK_SESSION_BEGIN(_session, "addPremiumUser");
            try
            {
                _session.m_pi.pt.id = _ticket.id;
                _session.m_pi.pt._typeid = _ticket._typeid;
                _session.m_pi.pt.unix_end_date = (int)_ticket.end_date_unix_local;
                _session.m_pi.pt.unix_sec_date = (int)(_ticket.end_date_unix_local - UtilTime.GetLocalTimeAsUnix());

                _smp.message_pool.getInstance().push(new message("[PremiumSystem::addPremiumUser][Log][UID=" + _session.m_pi.uid + "] eh um Premium User por (" + _time + ") Dias", type_msg.CL_FILE_LOG_AND_CONSOLE));

                List<stItem> add_itens = new List<stItem>();
                _session.m_pi.m_cap.premium_user = true;

                var new_ball = addPremiumBall(_session);
                if (new_ball._typeid != 0) add_itens.Add(new_ball);

                if (isPremium2(_session.m_pi.pt._typeid))
                {
                    addPremiumClubSet(_session, _time);
                    var new_mascot = addPremiumMascot(_session, _time);
                    if (new_mascot._typeid != 0) add_itens.Add(new_mascot);

                    packet_func.session_send(_session.m_pi.mp_wi.Build(), _session, 1);
                }

                var p = new PangyaBinaryWriter(0x9A);
                p.WriteInt32(_session.m_pi.m_cap.ulCapability);
                packet_func.session_send(p, _session, 1);

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
                        p.WriteZeroByte(25);
                    }
                    packet_func.session_send(p, _session, 1);
                }
            }
            catch (Exception e)
            {
                _smp.message_pool.getInstance().push(new message("[PremiumSystem::addPremiumUser][ErrorSystem] " + e.Message, type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void removePremiumUser(Player _session)
        {
            CHECK_SESSION_BEGIN(_session, "removePremiumUser");
            try
            { 
                removePremiumBall(_session);
                _session.m_pi.m_cap.premium_user = false;

                // Tira capacidade de premium user do player
                _session.m_pi.m_cap.premium_user = false;


                packet_func.session_send(packet_func.pacote09A(_session.m_pi.m_cap.ulCapability),
                    _session, 1);

                // UPDATE ON GAME - Mostra a mensagem que acabou o tempo do ticket premium

                packet_func.session_send(packet_func.pacote26D(_session.m_pi.pt.unix_end_date),
                    _session, 0);

                _session.m_pi.pt.clear();

                _smp.message_pool.getInstance().push(new message("[PremiumSystem::removePremiumUser][Log] player[UID=" + _session.m_pi.uid + "] removeu o Premium User...", type_msg.CL_FILE_LOG_AND_CONSOLE));

                using (var p = new PangyaBinaryWriter(0x40))   // Msg to Chat of player
                {
                    p.WriteByte(7);  // Notice

                    p.WritePStr(_session.m_pi.nickname);
                    p.WritePStr("voce nao e mais premium.");

                    packet_func.session_send(p, _session);
                }
            }
            catch (Exception e)
            {
                _smp.message_pool.getInstance().push(new message("[PremiumSystem::removePremiumUser][ErrorSystem] " + e.Message, type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public stItem addPremiumBall(Player _session)
        {
            CHECK_SESSION_BEGIN(_session, "addPremiumBall");
            stItem item = new stItem();
            try
            {
                uint ball = getPremiumBallByTicket(_session.m_pi.pt._typeid);
                WarehouseItemEx new_wi = new WarehouseItemEx();
                new_wi.id = -1;
                new_wi._typeid = ball;
                new_wi.STDA_C_ITEM_QNTD = 1;
                new_wi.type = 0x6A;
                new_wi.clubset_workshop.level = -1;

                _session.m_pi.mp_wi.Add(new_wi.id, new_wi);
                _session.m_pi.ue.ball_typeid = ball;
                _session.m_pi.ei.comet = new_wi;

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
                _smp.message_pool.getInstance().push(new message("[PremiumSystem::addPremiumBall][ErrorSystem] " + e.Message, type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
            return item;
        }

        public stItem addPremiumClubSet(Player _session, uint _time)
        {
            CHECK_SESSION_BEGIN(_session, "addPremiumClubSet");
            stItem item = new stItem();
            try
            {
                uint clubset = getPremiumClubSetByTicket(_session.m_pi.pt._typeid);
                if (_session.m_pi.findWarehouseItemByTypeid(clubset) != null) return item;

                BuyItem bi = new BuyItem();
                bi.id = -1;
                bi._typeid = clubset;
                bi.qntd = 1;
                bi.time = (short)_time;

                ItemManager.initItemFromBuyItem(_session.m_pi, item, bi, false, 0, 0, 1);
                if (item._typeid == 0u) throw new Exception("Erro inicializar ClubSet");

                if (ItemManager.addItem(item, _session, 0, 0) < 0) throw new Exception("Erro adicionar ClubSet");

                var new_wi = _session.m_pi.findWarehouseItemById(item.id);
                new_wi.STDA_C_ITEM_TIME = 0;

                if (isPremium2(_session.m_pi.pt._typeid))
                {
                    clubset = PREMIUM_3_CLUBSET_TYPEID;
                    bi.id = -1;
                    bi._typeid = clubset;
                    ItemManager.initItemFromBuyItem(_session.m_pi, item, bi, false, 0, 0, 1);
                    ItemManager.addItem(item, _session, 0, 0);
                    new_wi = _session.m_pi.findWarehouseItemById(item.id);
                    new_wi.STDA_C_ITEM_TIME = 0;
                }
            }
            catch (Exception e)
            {
                _smp.message_pool.getInstance().push(new message("[PremiumSystem::addPremiumClubSet][ErrorSystem] " + e.Message, type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
            return item;
        }

        public stItem addPremiumMascot(Player _session, uint _time)
        {
            CHECK_SESSION_BEGIN(_session, "addPremiumMascot");
            stItem item = new stItem();
            try
            {
                uint mascot = getPremiumMascotByTicket(_session.m_pi.pt._typeid);
                if (_session.m_pi.findWarehouseItemByTypeid(mascot) != null) return item;

                BuyItem bi = new BuyItem();
                bi.id = -1; bi._typeid = mascot; bi.qntd = 1; bi.time = (short)_time;

                ItemManager.initItemFromBuyItem(_session.m_pi, item, bi, false, 0, 0, 1);
                ItemManager.addItem(item, _session, 0, 0);
            }
            catch (Exception e)
            {
                _smp.message_pool.getInstance().push(new message("[PremiumSystem::addPremiumMascot][ErrorSystem] " + e.Message, type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
            return item;
        }

        public void addPremiumBox(Player _session)
        {
            CHECK_SESSION_BEGIN(_session, "addPremiumBox");
            try
            {
                uint _typeid = getPremiumBoxByTicket(_session.m_pi.pt._typeid);
                uint _qntd = getBoxQntdByTicket(_session.m_pi.pt._typeid);
                if (_qntd > 0)
                {
                    stItem item = new stItem();
                    BuyItem bi = new BuyItem();
                    bi.id = -1; bi._typeid = _typeid; bi.qntd = (uint)_qntd;

                    ItemManager.initItemFromBuyItem(_session.m_pi, item, bi, false, 0, 0, 1);
                    MailBoxManager.sendMessageWithItem(0, _session.m_pi.uid, "Premium System - Gift Box", item);
                }
            }
            catch (Exception e) { _smp.message_pool.getInstance().push(new message("[PremiumSystem::addPremiumBox][ErrorSystem] " + e.Message, type_msg.CL_FILE_LOG_AND_CONSOLE)); }
        }

        public stItem addPremiumTitle(Player _session, uint _time)
        {
            CHECK_SESSION_BEGIN(_session, "addPremiumTitle");
            stItem item = new stItem();
            try
            {
                uint ball = getPremiumTitleByTicket(_session.m_pi.pt._typeid);
                WarehouseItemEx new_wi = new WarehouseItemEx();
                new_wi.id = -1; new_wi._typeid = ball; new_wi.STDA_C_ITEM_QNTD = 1; new_wi.type = 0x6A;

                _session.m_pi.mp_wi.Add(new_wi.id, new_wi);
                item.type = 2; item.id = new_wi.id; item._typeid = new_wi._typeid; item.flag_time = (byte)new_wi.type;
                item.qntd = 1; item.STDA_C_ITEM_QNTD = (short)item.qntd;
            }
            catch (Exception e) { _smp.message_pool.getInstance().push(new message("[PremiumSystem::addPremiumTitle][ErrorSystem] " + e.Message, type_msg.CL_FILE_LOG_AND_CONSOLE)); }
            return item;
        }

        public void removePremiumBall(Player _session)
        {
            CHECK_SESSION_BEGIN(_session, "removePremiumBall");
            try
            {
                uint ball = getPremiumBallByTicket(_session.m_pi.pt._typeid);
                var pair = _session.m_pi.mp_wi.FirstOrDefault(x => x.Value._typeid == ball);
                if (pair.Value != null)
                {
                    stItem item = new stItem();
                    item.type = 2; item.id = pair.Value.id; item._typeid = pair.Value._typeid;
                    item.qntd = 1; item.STDA_C_ITEM_QNTD = -1; item.flag_time = 0x6A;

                    _session.m_pi.mp_wi.Remove(pair.Key);

                    var p = new PangyaBinaryWriter(0x216);
                    p.WriteUInt32((uint)UtilTime.GetSystemTimeAsUnix());
                    p.WriteUInt32(1);
                    p.WriteByte(item.type); p.WriteUInt32(item._typeid); p.WriteInt32(item.id);
                    p.WriteUInt32(item.flag_time); p.WriteBytes(item.stat.ToArray());
                    p.WriteUInt32(item.STDA_C_ITEM_QNTD < 0 ? 1u : (uint)item.STDA_C_ITEM_QNTD);
                    p.WriteZeroByte(25);
                    packet_func.session_send(p, _session, 1);
                }
            }
            catch (Exception e) { _smp.message_pool.getInstance().push(new message("[PremiumSystem::removePremiumBall][ErrorSystem] " + e.Message, type_msg.CL_FILE_LOG_AND_CONSOLE)); }
        }

        public void removePremiumTitle(Player _session)
        {
            CHECK_SESSION_BEGIN(_session, "removePremiumTitle");
            try
            {
                uint ball = getPremiumTitleByTicket(_session.m_pi.pt._typeid);
                var pair = _session.m_pi.mp_wi.FirstOrDefault(x => x.Value._typeid == ball);
                if (pair.Value != null)
                {
                    stItem item = new stItem();
                    item.type = 2; item.id = pair.Value.id; item._typeid = pair.Value._typeid;
                    item.qntd = 1; item.STDA_C_ITEM_QNTD = 1; item.flag_time = 0x6A;

                    _session.m_pi.mp_wi.Remove(pair.Key);

                    var p = new PangyaBinaryWriter(0x216);
                    p.WriteUInt32((uint)UtilTime.GetSystemTimeAsUnix());
                    p.WriteUInt32(1);
                    p.WriteByte(item.type); p.WriteUInt32(item._typeid); p.WriteInt32(item.id);
                    p.WriteUInt32(item.flag_time); p.WriteBytes(item.stat.ToArray());
                    p.WriteUInt32(1u); // valor simplificado do logic do original
                    p.WriteZeroByte(25);
                    packet_func.session_send(p, _session, 1);
                }
            }
            catch (Exception e) { _smp.message_pool.getInstance().push(new message("[PremiumSystem::removePremiumTitle][ErrorSystem] " + e.Message, type_msg.CL_FILE_LOG_AND_CONSOLE)); }
        }

        public void updatePremiumUser(Player _session)
        {
            CHECK_SESSION_BEGIN(_session, "updatePremiumUser");
            try
            {
                List<stItem> add_itens = new List<stItem>();
                _session.m_pi.m_cap.premium_user = true;
                var new_ball = addPremiumBall(_session);
                if (new_ball._typeid != 0u) add_itens.Add(new_ball);

                packet_func.session_send(packet_func.pacote09A(_session.m_pi.m_cap.ulCapability), _session);

                if (add_itens.Count > 0)
                {
                    var p = new PangyaBinaryWriter();

                    p.init_plain(0x216);
                    p.WriteUInt32((uint)UtilTime.GetSystemTimeAsUnix());
                    p.WriteUInt32((uint)add_itens.Count);
                    foreach (var el in add_itens)
                    {
                        p.WriteByte(el.type); p.WriteUInt32(el._typeid); p.WriteUInt32((uint)el.id);
                        p.WriteUInt32(el.flag_time); p.WriteBytes(el.stat.ToArray());
                        p.WriteInt32((el.flag_time == 0) ? (int)el.STDA_C_ITEM_QNTD : el.STDA_C_ITEM_TIME);
                        p.WriteZeroByte(25);
                    }
                    packet_func.session_send(p, _session, 1);
                }
            }
            catch (Exception e) { _smp.message_pool.getInstance().push(new message("[PremiumSystem::updatePremiumUser][ErrorSystem] " + e.Message, type_msg.CL_FILE_LOG_AND_CONSOLE)); }
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