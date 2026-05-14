using System;
using System.Collections.Generic;
using System.Linq;
using Pangya_GameServer.Repository;
using Pangya_GameServer.Game;
using Pangya_GameServer.Game.Base;
using Pangya_GameServer.Game.Manager;
using Pangya_GameServer.Game.System;
using Pangya_GameServer.Models;
using Pangya_GameServer.PacketFunc;
using PangyaAPI.IFF.JP.Extensions;
using PangyaAPI.Network.Repository;
using PangyaAPI.Network.Models;
using PangyaAPI.Network.PangyaPacket;
using PangyaAPI.Network.PangyaSession;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Models;
using PangyaAPI.Utilities.Log;
using static Pangya_GameServer.Models.DefineConstants;
namespace Pangya_GameServer
{
    public class Player : Session
    {
        public ChatPenaltyManager ChatPenalty { get; } = new ChatPenaltyManager();
        //trocar por Inventory depois
        public PlayerInfo m_pi { get; set; }// info do jogador....
        public GMInfo m_gi { get; set; }// info de GM se for GM
        public GameBase m_pGame { get; set; }//tipo de jogo que o jogador esta....  
        public Room m_room { get; set; } //sala onde esta o jogador....
        public Channel m_channel { get; set; } //onde esta o jogador....  
        public string m_MacAdress { get; set; }
        public Player()
        {
            m_pi = new PlayerInfo();
            m_gi = new GMInfo();
        }

        public override string getNickname()
        {
            return m_pi.nickname;
        }

        public override uint getUID()
        {
            return m_pi.uid;
        }

        public override string getID()
        {
            return m_pi.id;
        }

        public override uint getCapability() { return (uint)m_pi.m_cap.ulCapability; }

        public override byte getStateLogged()
        {
            return m_pi.m_state_logged;
        }

        public override bool clear()
        {
            lock (this)
            {
                bool ret;
                if (ret = base.clear())
                {
                    // Player Info
                    m_pi.clear();

                    // Game Master Info
                    m_gi.clear();
                }
                return ret;
            }
        }

        public void addExp(int _exp, bool _upt_on_game = false)
        {

            if (_exp == 0 || _exp < 0)
            {
                throw new exception("[player::addExp][Error] _exp is invalid(zero)", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PLAYER,
                    1, 0));
            }

            try
            {

                // UPDATE ON GAME
                var p = new PangyaBinaryWriter();

                int ret = -1;

                if ((ret = m_pi.addExp(_exp)) >= 0)
                {

                    if (ret > 0)
                    { // Player Upou de level 
                        List<stItem> v_item = new List<stItem>();
                        stItem item = new stItem();
                        BuyItem bi = new BuyItem();

                        var level_prize = sIff.getInstance().getLevelUpPrizeItem();

                        for (var i = (m_pi.mi.level - ret + 1); i <= m_pi.mi.level; ++i)
                        {

                            // Zera o vector de item que vai ser enviado por level UP! para o mail box do player
                            v_item.Clear();

                            var it = level_prize.FirstOrDefault(c => c.Level == i);

                            if (it == null)
                            {
                                throw new exception("[player::addExp][ErrorSystem] PLAYER[UID=" + Convert.ToString(m_pi.uid) + "] addExp, mas nao encontrou o level up prize[level=" + Convert.ToString(i) + "] no IFF_STRUCT do server", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PLAYER,
                                    2, 0));
                            }

                            for (var ii = 0; ii < (it.reward.TypeID.Length); ++ii)
                            {

                                if (it.reward.TypeID[ii] != 0)
                                {
                                    bi = new BuyItem();
                                    item.clear();

                                    bi.id = -1;
                                    bi._typeid = it.reward.TypeID[ii];
                                    bi.qntd = it.reward.Quantity[ii];
                                    bi.time = (short)it.reward.Time[ii];

                                    ItemManager.initItemFromBuyItem(m_pi,
                                        item, bi, false, 0, 0,
                                        1);

                                    if (item._typeid == 0)
                                    {
                                        throw new exception("[player::addExp][ErrorSystem] PLAYER[UID=" + Convert.ToString(m_pi.uid) + "] addExp, mas nao conseguiu inicializar o item[TYPEID=" + Convert.ToString(bi._typeid) + "]", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PLAYER,
                                            3, 0));
                                    }

                                    v_item.Add(item);
                                }
                            }

                            var msg = "Level UP! Prize.";

                            // Envia Prêmio de Level UP! para o Mail Box do player
                            MailBoxManager.sendMessageWithItem(0,
                                m_pi.uid, msg, v_item);
                        }

                        // Mostra msg que o player Upou de level
                        p.init_plain(0x10F);

                        p.WriteUInt32(0); // OK

                        p.WriteByte((byte)ret); // Qntd de level(s) que ele upou
                        p.WriteByte((byte)m_pi.mi.level); // Novo level que o player ficou

                        packet_func.session_send(p,
                            this, 1);
                    }
                }

                // Att Level e Exp do player IN GAME
                if (_upt_on_game)
                { // Só att se for pegando do mail ou ticket report esses negocio, por que jogando vs/tourney, nao precisa desse pacote
                    p.init_plain(0x1D9);

                    p.WriteUInt32(m_pi.mi.level);
                    p.WriteInt32(m_pi.ui.exp);

                    packet_func.session_send(p,
                        this, 1);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[player::addExp][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if (ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) != STDA_ERROR_TYPE.PLAYER_INFO)
                {
                    throw;
                }
            }
        }


        public void addCaddieExp(int _exp)
        {

            if (_exp == 0 || _exp < 0)
            {
                throw new exception("[player::addCaddieExp][Error] PLAYER[UID=" + Convert.ToString(m_pi.uid) + "] tentou adicionar mais exp[VALUE=" + Convert.ToString(_exp) + "] ao caddie equipado, mas exp is invalid(zero).", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PLAYER,
                    300, 0));
            }

            if (m_pi.ei.cad_info == null)
            {
                throw new exception("[player::addCaddieExp][Error] PLAYER[UID=" + Convert.ToString(m_pi.uid) + "] tentou adicionar mais exp[VALUE=" + Convert.ToString(_exp) + "] ao caddie equipado, mas ele nao esta com nenhum caddie equipado.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PLAYER,
                    301, 0));
            }


            // Só add Exp se não estiver no ultimo level do Caddie
            if (m_pi.ei.cad_info.level < LIMIT_LEVEL_CADDIE)
            {

                m_pi.ei.cad_info.exp += _exp;

                int exp_level = 0;

                while (m_pi.ei.cad_info.level < LIMIT_LEVEL_CADDIE && m_pi.ei.cad_info.exp >= (exp_level = (int)(520 + (160 * (m_pi.ei.cad_info.level)))))
                {

                    // Upou 1 Level
                    m_pi.ei.cad_info.level++;

                    m_pi.ei.cad_info.exp -= exp_level;
                }

                // UPDATE ON DB
                snmdb.NormalManagerDB.getInstance().add(1,
                    new CmdUpdateCaddieInfo(m_pi.uid, m_pi.ei.cad_info),
                    SQLDBResponse, this);

                // LOG
            }
        }

        public void addMascotExp(int _exp)
        {

            if (_exp == 0 || _exp < 0)
            {
                throw new exception("[player::addMascotExp][Error] PLAYER[UID=" + Convert.ToString(m_pi.uid) + "] tentou adicionar mais exp[VALUE=" + Convert.ToString(_exp) + "] ao mascot equipado, mas exp is invalid(zero).", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PLAYER,
                    400, 0));
            }

            if (m_pi.ei.mascot_info == null)
            {
                throw new exception("[player::addMascotExp][Error] PLAYER[UID=" + Convert.ToString(m_pi.uid) + "] tentou adicionar mais exp[VALUE=" + Convert.ToString(_exp) + "] ao mascot equipado, mas ele nao esta com nenhum mascot equipado.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PLAYER,
                    401, 0));
            }

            if (m_pi.ei.mascot_info._typeid == 0)
                return;

            // Progressão aritmética de segunda ordem

            // Só add Exp se não estiver no ultimo level do Mascot
            if (m_pi.ei.mascot_info.level < LIMIT_LEVEL_MASCOT)
            {

                m_pi.ei.mascot_info.exp += _exp;

                int exp_level = 0;

                while (m_pi.ei.mascot_info.level < LIMIT_LEVEL_MASCOT && m_pi.ei.mascot_info.exp >= (exp_level = (int)(50 + ((20 + (20 + ((m_pi.ei.mascot_info.level) - 1) * 10)) * (m_pi.ei.mascot_info.level) / 2))))
                {

                    // Upou 1 Level
                    m_pi.ei.mascot_info.level++;

                    m_pi.ei.mascot_info.exp -= exp_level;
                }

                // UPDATE ON DB
                snmdb.NormalManagerDB.getInstance().add(2,
                    new CmdUpdateMascotInfo(m_pi.uid, m_pi.ei.mascot_info),
                    SQLDBResponse, this);

            }
        }
        // Add Exp Estático
        public static void addExp(uint _uid, int _exp)
        {

            if (_exp == 0 || _exp < 0)
            {
                throw new exception("[player::addExp][Error] _exp is invalid(zero)", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PLAYER,
                    1, 0));
            }

            PlayerInfo pi = null;
            packet p = null;

            try
            {

                int ret = -1;

                CmdPlayerInfo cmd_pi = new CmdPlayerInfo(_uid); // Waiter

                snmdb.NormalManagerDB.getInstance().add(0, cmd_pi, null, null);


                if (cmd_pi.getException().getCodeError() != 0)
                {
                    throw cmd_pi.getException();
                }

                pi = new PlayerInfo();

                pi.set_info(cmd_pi.getInfo());

                if ((ret = pi.addExp(_exp)) >= 0)
                {

                    if (ret > 0)
                    { // Player Upou de level

                        List<stItem> v_item = new List<stItem>();
                        stItem item = new stItem();
                        BuyItem bi = new BuyItem();

                        var level_prize = sIff.getInstance().getLevelUpPrizeItem();

                        for (var i = (pi.mi.level - ret + 1); i <= pi.mi.level; ++i)
                        {

                            // Zera o vector de item que vai ser enviado por level UP! para o mail box do player
                            v_item.Clear();

                            var it = level_prize.FirstOrDefault(c => c.Level == i);

                            if (it == null)
                            {
                                throw new exception("[player::addExp][ErrorSystem] PLAYER[UID=" + Convert.ToString(pi.uid) + "] addExp, mas nao encontrou o level up prize[level=" + i + "] no IFF_STRUCT do server", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PLAYER,
                                    2, 0));
                            }

                            for (var ii = 0; ii < (it.reward.TypeID.Length); ++ii)
                            {

                                if (it.reward.TypeID[ii] != 0)
                                {
                                    bi = new BuyItem();
                                    item.clear();

                                    bi.id = -1;
                                    bi._typeid = it.reward.TypeID[ii];
                                    bi.qntd = it.reward.Quantity[ii];
                                    bi.time = (short)it.reward.Time[ii];

                                    ItemManager.initItemFromBuyItem(pi,
                                        item, bi, false, 0, 0,
                                        1/**/);

                                    if (item._typeid == 0)
                                    {
                                        throw new exception("[player::addExp][ErrorSystem] PLAYER[UID=" + Convert.ToString(pi.uid) + "] addExp, mas nao conseguiu inicializar o item[TYPEID=" + Convert.ToString(bi._typeid) + "]", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PLAYER,
                                            3, 0));
                                    }

                                    v_item.Add(item);
                                }
                            }

                            var msg = "Level UP! Prize.";

                            // Envia Prêmio de Level UP! para o Mail Box do player
                            MailBoxManager.sendMessageWithItem(0,
                                pi.uid, msg, v_item);
                        }
                    }
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[player::addExp][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Clean
                if (pi != null)
                {

                    pi = null;

                    pi = null;
                }

                if (p != null)
                {

                    p = null;

                    p = null;
                }

                if (ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) != STDA_ERROR_TYPE.PLAYER_INFO)
                {
                    throw;
                }
            }

            // Clean
            if (pi != null)
            {

                pi = null;

                pi = null;
            }

            if (p != null)
            {

                p = null;

                p = null;
            }
        }

        public void addPang(ulong _pang)
        {

            m_pi.addPang(_pang);

            // UPDATE ON GAME
            var p = new PangyaBinaryWriter((ushort)0xC8);

            p.WriteUInt64(m_pi.ui.pang);
            p.WriteUInt64(_pang);

            packet_func.session_send(p,
                this, 1);
        }

        public void consomePang(ulong _pang)
        {

            m_pi.consomePang(_pang);

            // UPDATE ON GAME
            var p = new PangyaBinaryWriter((ushort)0xC8);

            p.WriteUInt64(m_pi.ui.pang);
            p.WriteUInt64(_pang);

            packet_func.session_send(p,
                this, 1);
        }

        public void saveCPLog(CPLog _cp_log)
        {
            ulong cp = _cp_log.getCookie();

            try
            {
                if (cp > 0)
                {
                    long log_id = -1;

                    var cmd_icpl = new CmdInsertCPLog(m_pi.uid, _cp_log); // Waiter

                    snmdb.NormalManagerDB.getInstance().add(0, cmd_icpl, null, null);

                    if (cmd_icpl.getException().getCodeError() != 0)
                        throw cmd_icpl.getException();

                    if ((log_id = cmd_icpl.getId()) <= 0)
                        throw new exception($"[player::saveCPLog][Error] PLAYER[UID={m_pi.uid}] nao conseguiu salvar o CPLog[{_cp_log.toString()}] do player. Bug",
                             ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PLAYER, 1300, 0));

                    if ((_cp_log.getType() == CPLog.TYPE.BUY_SHOP || _cp_log.getType() == CPLog.TYPE.GIFT_SHOP)
                            && _cp_log.getItemCount() > 0)
                    {

                        // Tem item(ns), salva o log do(s) item(ns)
                        foreach (var el in _cp_log.getItens())
                        {
                            snmdb.NormalManagerDB.getInstance().add(3, new CmdInsertCPLogItem(m_pi.uid, log_id, el), SQLDBResponse, this);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                _smp.message_pool.getInstance().push(new message($"[player::saveCPLog][ErrorSystem] {e.Message}", type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public static void saveCPLog(uint _uid, CPLog _cp_log)
        {
            if (_uid == 0)
                throw new exception($"[player::saveCPLog(static)][Error] _uid is invalid({_uid})",
                    ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PLAYER, 1301, 0));

            ulong cp = _cp_log.getCookie();

            try
            {
                if (cp > 0)
                {
                    long log_id = -1;

                    CmdInsertCPLog cmd_icpl = new CmdInsertCPLog(_uid, _cp_log, true); // Waiter

                    snmdb.NormalManagerDB.getInstance().add(0, cmd_icpl, null, null);

                    if (cmd_icpl.getException().getCodeError() != 0)
                        throw cmd_icpl.getException();

                    if ((log_id = cmd_icpl.getId()) <= 0)
                        throw new exception($"[player::saveCPLog(static)][Error] PLAYER[UID={_uid}] nao conseguiu salvar o CPLog[{_cp_log.toString()}] do player. Bug",
                            ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PLAYER, 1300, 0));

                    if ((_cp_log.getType() == CPLog.TYPE.BUY_SHOP || _cp_log.getType() == CPLog.TYPE.GIFT_SHOP)
                            && _cp_log.getItemCount() > 0)
                    {

                        // Tem item(ns), salva o log do(s) item(ns)
                        foreach (var el in _cp_log.getItens())
                        {
                            snmdb.NormalManagerDB.getInstance().add(3, new CmdInsertCPLogItem(_uid, log_id, el), SQLDBResponse, null);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                _smp.message_pool.getInstance().push(new message($"[player::saveCPLog(static)][ErrorSystem] {e.Message}", type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void addCookie(ulong _cookie)
        {

            m_pi.addCookie(_cookie);

            // UPDATE ON GAME
            var p = new PangyaBinaryWriter((ushort)0x96);

            p.WriteUInt64(m_pi.cookie);

            packet_func.session_send(p,
                this, 1);
        }

        public void consomeCookie(ulong _cookie)
        {

            m_pi.consomeCookie(_cookie);

            // UPDATE ON GAME
            var p = new PangyaBinaryWriter((ushort)0x96);

            p.WriteUInt64(m_pi.cookie);

            packet_func.session_send(p,
                this, 1);
        }

        public void addMoeda(ulong _pang, ulong _cookie)
        {

            addPang(_pang);
            addCookie(_cookie);
        }

        public void consomeMoeda(ulong _pang, ulong _cookie)
        {

            consomePang(_pang);
            consomeCookie(_cookie);
        }

        public bool checkCharacterEquipedAuxPart(CharacterInfo ci)
        {

            bool upt_on_db = false;

            // Check AuxPart Equiped
            for (var i = 0; i < 5; ++i)
            {

                if (ci.auxparts[i] != 0)
                {

                    // Esse AuxPartNumber é o 0x0 anel que consome(só mão direita), 0x1 mão direita, 0x21 mão esquerda
                    if (sIff.getInstance().getItemGroupIdentify(ci.auxparts[i]) == PangyaAPI.IFF.JP.Models.Flags.IFF_GROUP.AUX_PART)
                    {

                        var aux = sIff.getInstance().findAuxPart(ci.auxparts[i]);

                        var pAux = m_pi.findWarehouseItemByTypeid(aux.ID);

                        if (aux != null && aux.Active && pAux != null)
                        {

                            if (aux.Level.GoodLevel((byte)m_pi.level))
                            {

                                if (aux.slot[0] == 0 || pAux.c[0] > 0)
                                {
                                }
                                else
                                { 
                                    // Desequipa
                                    ci.auxparts[i] = 0; 
                                    upt_on_db = true;
                                }

                            }
                            else
                            {

                                // Desequipa
                                ci.auxparts[i] = 0; 
                                upt_on_db = true;
                            }

                        }
                        else
                        {

                            // Desequipa
                            ci.auxparts[i] = 0; 
                            upt_on_db = true;
                        } 
                    }
                    else
                    { 
                        // Desequipa
                        ci.auxparts[i] = 0; 
                        upt_on_db = true;
                    }

                }
                else
                {
                }
            }
            m_pi.ei.char_info = ci;
            return upt_on_db;
        }

        public bool checkCharacterEquipedCutin(CharacterInfo ci)
        {

            bool upt_on_db = false;


            for (var i = 0; i < (ci.cut_in.Length); ++i)
            {

                if (ci.cut_in[i] != 0)
                {

                    var pCutin = m_pi.findWarehouseItemById((int)ci.cut_in[i]);

                    if (pCutin == null)
                    {

                        // Zera (Desequipa)
                        ci.cut_in[i] = 0;

                        upt_on_db = true;

                        _smp.message_pool.getInstance().push(new message("[player::checkCharacterEquipedCutin][Error] PLAYER[UID=" + Convert.ToString(m_pi.uid) + "] Character[TYPEID=" + Convert.ToString(ci._typeid) + ", ID=" + Convert.ToString(ci.id) + "] Not Have Cutin[ID=" + Convert.ToString(ci.cut_in[i]) + ", SLOT=" + Convert.ToString(i) + "], but it is equiped. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                    }
                    else
                    {

                        var cutin = sIff.getInstance().findSkin(pCutin._typeid);

                        if (cutin != null && !cutin.Level.GoodLevel((byte)m_pi.level))
                        {

                            // Zera (Desequipa)
                            ci.cut_in[i] = 0;

                            upt_on_db = true;

                            // Não tem o level necessário para equipar esse Cutin
                            _smp.message_pool.getInstance().push(new message("[player::checkCharacterEquipedCutin][Error] PLAYER[UID=" + Convert.ToString(m_pi.uid) + "] Character[TYPEID=" + Convert.ToString(ci._typeid) + ", ID=" + Convert.ToString(ci.id) + "] Cutin[TYPEID=" + Convert.ToString(pCutin._typeid) + " ID=" + Convert.ToString(pCutin.id) + ", SLOT=" + Convert.ToString(i) + "]  PLAYER[Lv=" + Convert.ToString(m_pi.level) + "] nao tem o level[is_max=" + Convert.ToString(cutin.Level.is_max) + ", Lv=" + Convert.ToString((ushort)cutin.Level.level) + "] para equipar esse item. Hacker ou bug..", type_msg.CL_FILE_LOG_AND_CONSOLE));

                        }
                        else if (cutin == null)
                        {

                            // Zera (Desequipa)
                            ci.cut_in[i] = 0;

                            upt_on_db = true;

                            // Não tem esse Cutin no IFF_STRUCT do server desequipa ele
                            _smp.message_pool.getInstance().push(new message("[player::checkCharacterEquipedCutin][Error] PLAYER[UID=" + Convert.ToString(m_pi.uid) + "] Character[TYPEID=" + Convert.ToString(ci._typeid) + ", ID=" + Convert.ToString(ci.id) + "] Not Have Cutin[TYPEID=" + Convert.ToString(pCutin._typeid) + " ID=" + Convert.ToString(pCutin.id) + ", SLOT=" + Convert.ToString(i) + "] in IFF_STRUCT of server, but it is equiped. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                        }
                    }
                }
            }

            return upt_on_db;
        }

        public void checkCharacterAllItemEquiped(CharacterInfo ci)
        {

            var ret = checkCharacterEquipedPart(ci);

            ret |= checkCharacterEquipedAuxPart(ci);

            ret |= checkCharacterEquipedCutin(ci);

            // Atualiza os parts equipados do player no banco de dados, que tinha parts errados
            if (ret)
            {
                snmdb.NormalManagerDB.getInstance().add(5,
                    new CmdUpdateCharacterAllPartEquiped(m_pi.uid, ci),
                    SQLDBResponse, this);
            } 
        }

        public bool checkCharacterEquipedPart(CharacterInfo ci)
        {

            uint def_part = 0;

            // Angel Part of character 3% quit rate para equipar o Normal angel wings
            var angel_wings_typeid = Global.angel_wings.FirstOrDefault(el => sIff.getInstance().getItemCharIdentify(el) == (ci._typeid & 0x000000FF));

            int angel_wings_part_num = (Global.angel_wings.Any(el => el == angel_wings_typeid) ? -1 : (int)sIff.getInstance().getItemCharPartNumber(angel_wings_typeid));

            bool upt_on_db = false;

            // Checks Parts Equiped
            for (var i = 0u; i < 24; ++i)
            {

                if (ci.parts_typeid[i] != 0)
                {

                    if (sIff.getInstance().getItemGroupIdentify(ci.parts_typeid[i]) == PangyaAPI.IFF.JP.Models.Flags.IFF_GROUP.PART && (sIff.getInstance().getItemCharPartNumber(ci.parts_typeid[i]) == i || (ci.parts_typeid[i] & 0x08000400/*def part*/) == 0x8000400))
                    {

                        var part = sIff.getInstance().findPart(ci.parts_typeid[i]);

                        if (part != null && part.Active)
                        {

                            if (ci.parts_id[i] == 0)
                            {

                                def_part = ((sIff.getInstance().getItemCharPartNumber(ci.parts_typeid[i]) | (uint)(ci._typeid << 5)) << 13) | 0x8000400;

                                if ((ci.parts_typeid[i] & def_part) == def_part)
                                {
                                }
                                else
                                {

                                    // Deseequipa o Part do character e coloca os Parts Default do Character no lugar
                                    ci.unequipPart(part);

                                    upt_on_db = true;
                                }
                            }
                            else
                            {

                                var parts = m_pi.findWarehouseItemById((int)ci.parts_id[i]);

                                if (parts != null/* != _session.m_pi.v_wi.end()*/)
                                {

                                    var slot = part.position_mask.getSlot((int)i);

                                    if (slot == false)
                                    {

                                        // Deseequipa o Part do character e coloca os Parts Default do Character no lugar
                                        ci.unequipPart(part);

                                        upt_on_db = true;
                                    }
                                    else if (slot)
                                    {

                                        if (part.Level.GoodLevel((byte)m_pi.level))
                                        {

                                            if (angel_wings_part_num == -1 || parts._typeid != angel_wings_typeid || m_pi.ui.getQuitRate() < 3.0f)
                                            {
                                            }
                                            else
                                            {

                                                // Deseequipa o Part do character e coloca os Parts Default do Character no lugar
                                                ci.unequipPart(part);

                                                upt_on_db = true;
                                            }
                                        }
                                        else
                                        {

                                            // Deseequipa o Part do character e coloca os Parts Default do Character no lugar
                                            ci.unequipPart(part);

                                            upt_on_db = true;
                                        }
                                    }
                                }
                                else
                                {

                                    // Deseequipa o Part do character e coloca os Parts Default do Character no lugar
                                    ci.unequipPart(part);

                                    upt_on_db = true;
                                }
                            }
                        }
                        else
                        {

                            // Deseequipa o Part do character e coloca os Parts Default do Character no lugar
                            if (part != null)
                                ci.unequipPart(part);
                            else
                            {
                                part = sIff.getInstance().findPart(_typeid: (def_part = ((i | (uint)(ci._typeid << 5)) << 13) | 0x8000400));
                                ci.parts_typeid[i] = (part != null) ? def_part : 0;
                                ci.parts_id[i] = 0;
                            }

                            upt_on_db = true;
                        }
                    }
                    else
                    {

                        var part = sIff.getInstance().findPart(ci.parts_typeid[i]);

                        // Deseequipa o Part do character e coloca os Parts Default do Character no lugar
                        if (part != null)
                            ci.unequipPart(part);
                        else
                        {

                            part = sIff.getInstance().findPart((def_part = ((i | (uint)(ci._typeid << 5)) << 13) | 0x8000400));
                            ci.parts_typeid[i] = (part != null) ? def_part : 0;
                            ci.parts_id[i] = 0;
                        }

                        upt_on_db = true;
                    }
                }
            }
            m_pi.ei.char_info = ci;
            return upt_on_db;
        }
          
        public bool checkSkinEquiped(UserEquip _ue)
        {

            bool upt_on_db = false;
            uint tmp_typeid = 0;
            uint tmp_id = 0;

            for (var i = 0; i < (_ue.skin_typeid.Length); ++i)
            {

                if (_ue.skin_typeid[i] != 0)
                {

                    var pSkin = m_pi.findWarehouseItemByTypeid(_ue.skin_typeid[i]);

                    if (pSkin == null)
                    {

                        // Guarda para usar no Log
                        tmp_typeid = _ue.skin_typeid[i];
                        tmp_id = _ue.skin_id[i];

                        // Zera (Desequipa)
                        _ue.skin_id[i] = 0;
                        _ue.skin_typeid[i] = 0;

                        upt_on_db = true;

                        _smp.message_pool.getInstance().push(new message("[player::checkSkinEquiped][Error] PLAYER[UID=" + Convert.ToString(m_pi.uid) + "] Not Have Skin[TYPEID=" + Convert.ToString(tmp_typeid) + ", ID=" + Convert.ToString(tmp_id) + ", SLOT=" + Convert.ToString(i) + "], but it is equiped. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                    }
                    else
                    {

                        var skin = sIff.getInstance().findSkin(pSkin._typeid);

                        // Aqui tem que verificar as condições dos Title, uns só com 3% de quit rate, % de acerto de pangya e etc

                        if (skin != null && !skin.Level.GoodLevel((byte)m_pi.level))
                        {

                            // Zera (Desequipa)
                            _ue.skin_id[i] = 0;
                            _ue.skin_typeid[i] = 0;

                            upt_on_db = true;

                            // Não tem o level necessário para equipar esse Skin
                            _smp.message_pool.getInstance().push(new message("[player::checkSkinEquiped][Error] PLAYER[UID=" + Convert.ToString(m_pi.uid) + "] Skin[TYPEID=" + Convert.ToString(pSkin._typeid) + " ID=" + Convert.ToString(pSkin.id) + ", SLOT=" + Convert.ToString(i) + "]  PLAYER[Lv=" + Convert.ToString(m_pi.level) + "] nao tem o level[is_max=" + Convert.ToString(skin.Level.is_max) + ", Lv=" + Convert.ToString((ushort)skin.Level.level) + "] para equipar esse item. Hacker ou bug.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                        }
                        else if (skin != null && sIff.getInstance().IsTitle(pSkin._typeid))
                        {

                            // Verifica se o title tem condição e atualiza se tiver
                            uint title_num = sIff.getInstance().getItemTitleNum(pSkin._typeid);

                            var check_title = m_pi.getTitleCallBack((int)title_num);

                            // check_title == null, title não tem condição
                            if (check_title != null && check_title.exec() == 0)
                            {

                                // Zera (Desequipa)
                                _ue.skin_id[i] = 0;
                                _ue.skin_typeid[i] = 0;

                                upt_on_db = true;

                                // Não passa na condição do title, desequipa ele
                                _smp.message_pool.getInstance().push(new message("[player::checkSkinEquiped][Error] PLAYER[UID=" + Convert.ToString(m_pi.uid) + "] Skin[TYPEID=" + Convert.ToString(pSkin._typeid) + " ID=" + Convert.ToString(pSkin.id) + ", SLOT=" + Convert.ToString(i) + "] nao passou na condition TITLE[NUM=" + Convert.ToString(title_num) + "], para equipar esse item. Hacker ou bug.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                            }

                        }
                        else if (skin == null)
                        {

                            // Zera (Desequipa)
                            _ue.skin_id[i] = 0;
                            _ue.skin_typeid[i] = 0;

                            upt_on_db = true;

                            // Não tem o Skin no IFF_STRUCT do server desequipa ele
                            _smp.message_pool.getInstance().push(new message("[player::checkSkinEquiped][Error] PLAYER[UID=" + Convert.ToString(m_pi.uid) + "] Not Have Skin[TYPEID=" + Convert.ToString(pSkin._typeid) + ", ID=" + Convert.ToString(pSkin.id) + ", SLOT=" + Convert.ToString(i) + "] in IFF_STRUCT of server, but it is equiped. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                        }
                    }
                }
            }

            return upt_on_db;
        }

        public bool checkPosterEquiped(UserEquip _ue)
        {

            bool upt_on_db = false;
            int tmp_typeid = 0;

            for (var i = 0; i < (_ue.poster.Length); ++i)
            {

                if (_ue.poster[i] != 0)
                {

                    var pPoster = m_pi.findMyRoomItemByTypeid(_ue.poster[i]);

                    if (pPoster == null)
                    {

                        // Guarda para enviar no log
                        tmp_typeid = (int)_ue.poster[i];

                        // Zera (Desequipa)
                        _ue.poster[i] = 0;

                        upt_on_db = true;

                        _smp.message_pool.getInstance().push(new message("[player::checkPosterEquiped][Error] PLAYER[UID=" + Convert.ToString(m_pi.uid) + "] Not Have Poster[TYPEID=" + Convert.ToString(tmp_typeid) + ", SLOT=" + Convert.ToString(i) + "], but it is equiped. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                    }
                    else
                    {

                        var poster = sIff.getInstance().findFurniture(pPoster._typeid);

                        if (poster != null && !poster.Level.GoodLevel((byte)m_pi.level))
                        {

                            // Zera (Desequipa)
                            _ue.poster[i] = 0;

                            upt_on_db = true;

                            // Não tem o level necessário para equipar esse Poster
                            _smp.message_pool.getInstance().push(new message("[player::checkPosterEquiped][Error] PLAYER[UID=" + Convert.ToString(m_pi.uid) + "] Poster[TYPEID=" + Convert.ToString(pPoster._typeid) + " ID=" + Convert.ToString(pPoster.id) + ", SLOT=" + Convert.ToString(i) + "]  PLAYER[Lv=" + Convert.ToString(m_pi.level) + "] nao tem o level[is_max=" + Convert.ToString(poster.Level.is_max) + ", Lv=" + Convert.ToString((ushort)poster.Level.level) + "] para equipar esse item. Hacker ou bug.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                        }
                        else if (poster == null)
                        {

                            // Zera (Desequipa)
                            _ue.poster[i] = 0;

                            upt_on_db = true;

                            // Não tem esse Poster no IFF_STRUCT do Server desequipa ele
                            _smp.message_pool.getInstance().push(new message("[player::checkPosterEquiped][Error] PLAYER[UID=" + Convert.ToString(m_pi.uid) + "] Not Have Poster[TYPEID=" + Convert.ToString(pPoster._typeid) + ", ID=" + Convert.ToString(pPoster.id) + ", SLOT=" + Convert.ToString(i) + "] in IFF_STRUCT of server, but it is equiped. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                        }
                    }
                }
            }

            return upt_on_db;
        }

        public bool checkCharacterEquiped(UserEquip _ue)
        {

            bool upt_on_db = false;
            int tmp_id = 0;

            if (_ue.character_id != 0)
            {

                if (m_pi.findCharacterById(_ue.character_id) == null)
                {

                    // Guarda para usar no Log
                    tmp_id = (int)_ue.character_id;

                    try
                    {

                        // Equipa Character Padrão
                        equipDefaultCharacter(_ue);

                    }
                    catch (exception e)
                    {

                        _smp.message_pool.getInstance().push(new message("[player::checkCharacterEquiped][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                    }

                    upt_on_db = true;

                    _smp.message_pool.getInstance().push(new message("[player::checkCharacterEquiped][Error] PLAYER[UID=" + Convert.ToString(m_pi.uid) + "] Not Have Character[ID=" + Convert.ToString(tmp_id) + "], but it is equiped. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                } //else Character não tem tempo ou level para equipar, então não precisa verificar o character se o player tiver ele

            }
            else
            {

                try
                {

                    // Equipa Character Padrão
                    equipDefaultCharacter(_ue);

                }
                catch (exception e)
                {

                    _smp.message_pool.getInstance().push(new message("[player::checkCharacterEquiped][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                }

                upt_on_db = true;

                // Não tem nenhum character normal ou padrão equipado, equipado o character padrão
                _smp.message_pool.getInstance().push(new message("[player::checkCharacterEquiped][Error][Warning] PLAYER[UID=" + Convert.ToString(m_pi.uid) + "] nao tem um character equipado. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            return upt_on_db;
        }

        public bool checkCaddieEquiped(UserEquip _ue)
        {

            bool upt_on_db = false;
            int tmp_id = 0;

            if (_ue.caddie_id != 0)
            {

                var pCaddie = m_pi.findCaddieById(_ue.caddie_id);
                if (pCaddie == null)
                {

                    // Guarda para usar no Log
                    tmp_id = (int)_ue.caddie_id;

                    // Zera (Desequipa)
                    _ue.caddie_id = 0;
                    m_pi.ei.cad_info = null;

                    upt_on_db = true;

                    _smp.message_pool.getInstance().push(new message("[player::checkCaddieEquiped][Error] PLAYER[UID=" + Convert.ToString(m_pi.uid) + "] Not Have Caddie[ID=" + Convert.ToString(tmp_id) + "], but it is equiped. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                }
                else
                {

                    var caddie = sIff.getInstance().findCaddie(pCaddie._typeid);

                    if (caddie != null && !caddie.Level.GoodLevel((byte)m_pi.level))
                    {

                        // Zera (Desequipa)
                        _ue.caddie_id = 0;
                        m_pi.ei.cad_info = null;

                        upt_on_db = true;

                        // Não tem o level necessário para equipar esse caddie
                        _smp.message_pool.getInstance().push(new message("[player::checkCaddieEquiped][Error] PLAYER[UID=" + Convert.ToString(m_pi.uid) + "] Caddie[TYPEID=" + Convert.ToString(pCaddie._typeid) + " ID=" + Convert.ToString(pCaddie.id) + " PLAYER[Lv=" + Convert.ToString(m_pi.level) + "] nao tem o level[is_max=" + Convert.ToString(caddie.Level.is_max) + ", Lv=" + Convert.ToString((ushort)caddie.Level.level) + "] para equipar esse item. Hacker ou bug.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                    }
                    else if (caddie != null && pCaddie.rent_flag == 2 && (DateTime.Now >= pCaddie.end_date.ConvertTime())) // tempo acabou
                    {
                        // Desequipa
                        _ue.caddie_id = 0;
                        m_pi.ei.cad_info = null;

                        upt_on_db = true;

                        // Log do caddie expirado
                        _smp.message_pool.getInstance().push(new message(
                            $"[player::checkCaddieEquiped][Error] PLAYER[UID={m_pi.uid}] Caddie[TYPEID={pCaddie._typeid} ID={pCaddie.id}, END_DATE={pCaddie.end_date.ConvertTime()}] expirou e não pode ser equipado. Hacker ou bug.",
                            type_msg.CL_FILE_LOG_AND_CONSOLE));
                    }
                    else if (caddie == null)
                    {

                        // Zera (Desequipa)
                        _ue.caddie_id = 0;
                        m_pi.ei.cad_info = null;

                        upt_on_db = true;

                        // Não tem esse caddie no IFF_STRUCT do server, desequipa ele
                        _smp.message_pool.getInstance().push(new message("[player::checkCaddieEquiped][Error] PLAYER[UID=" + Convert.ToString(m_pi.uid) + "] Not Have Caddie[TYPEID=" + Convert.ToString(pCaddie._typeid) + ", ID=" + Convert.ToString(pCaddie.id) + "] in IFF_STRUCT of server, but it is equiped. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                    }
                }
            }

            return upt_on_db;
        }


        public bool checkMascotEquiped(UserEquip _ue)
        {

            bool upt_on_db = false;
            int tmp_id = 0;
            if (_ue.mascot_id != 0)
            {

                var pMascot = m_pi.findMascotById(_ue.mascot_id);
                if (pMascot == null)
                {

                    // Guarda para usar no Log
                    tmp_id = _ue.mascot_id;

                    // Zera (Desequipa)
                    _ue.mascot_id = 0;
                    m_pi.ei.mascot_info = null;

                    upt_on_db = true;

                    _smp.message_pool.getInstance().push(new message("[player::checkMascotEquiped][Error] PLAYER[UID=" + Convert.ToString(m_pi.uid) + "] Not Have Mascot[ID=" + Convert.ToString(tmp_id) + "], but it is equiped. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                }
                else
                {

                    var mascot = sIff.getInstance().findMascot(pMascot._typeid);

                    if (mascot != null && !mascot.Level.GoodLevel((byte)m_pi.level))
                    {

                        // Zera (Desequipa)
                        _ue.mascot_id = 0;
                        m_pi.ei.mascot_info = null;

                        upt_on_db = true;

                        // Não tem o level necessário para equipar esse mascot, desequipa
                        _smp.message_pool.getInstance().push(new message("[player::checkMascotEquiped][Error] PLAYER[UID=" + Convert.ToString(m_pi.uid) + "] Mascot[TYPEID=" + Convert.ToString(pMascot._typeid) + " ID=" + Convert.ToString(pMascot.id) + " PLAYER[Lv=" + Convert.ToString(m_pi.level) + "] nao tem o level[is_max=" + Convert.ToString(mascot.Level.is_max) + ", Lv=" + Convert.ToString((ushort)mascot.Level.level) + "] para equipar esse item. Hacker ou bug.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                    }
                    else if (mascot != null && pMascot.tipo == 1 && (DateTime.Now >= pMascot.data.ConvertTime()))
                    {

                        // Zera (Desequipa)
                        _ue.mascot_id = 0;
                        m_pi.ei.mascot_info = null;

                        upt_on_db = true;

                        // O tempo do mascot acabou, desequipa
                        _smp.message_pool.getInstance().push(new message("[player::checkMascotEquiped][Error] PLAYER[UID=" + Convert.ToString(m_pi.uid) + "] Mascot[TYPEID=" + Convert.ToString(pMascot._typeid) + " ID=" + Convert.ToString(pMascot.id) + ", END_DATE=" + (pMascot.data.ConvertTime()) + "] acabou o tempo do mascot, nao pode equipar esse mascot. Hacker ou bug.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                    }
                    else if (mascot == null)
                    {

                        // Zera (Desequipa)
                        _ue.mascot_id = 0;
                        m_pi.ei.mascot_info = null;

                        upt_on_db = true;

                        // Não tem esse mascot no IFF_STRUCT do server, desequipa
                        _smp.message_pool.getInstance().push(new message("[player::checkMascotEquiped][Error] PLAYER[UID=" + Convert.ToString(m_pi.uid) + "] Not Have Mascot[TYPEID=" + Convert.ToString(pMascot._typeid) + ", ID=" + Convert.ToString(pMascot.id) + "] in IFF_STRCT of server, but it is equiped. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                    }
                }
            }

            return upt_on_db;
        }


        public bool checkClubSetEquiped(UserEquip _ue)
        {

            bool upt_on_db = false;
            int tmp_id = 0;

            if (_ue.clubset_id != 0)
            {

                var pClubSet = m_pi.findWarehouseItemById(_ue.clubset_id);

                if (pClubSet == null)
                {

                    // Guarda para usar no Log
                    tmp_id = (int)_ue.clubset_id;

                    try
                    {

                        // Equipa ClubSet Padrão
                        equipDefaultClubSet(_ue);

                    }
                    catch (exception e)
                    {

                        _smp.message_pool.getInstance().push(new message("[player::checkClubSetEquiped][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                    }

                    upt_on_db = true;

                    _smp.message_pool.getInstance().push(new message("[player::checkClubSetEquiped][Error] PLAYER[UID=" + Convert.ToString(m_pi.uid) + "] Not Have ClubSet[ID=" + Convert.ToString(tmp_id) + "], but it is equiped. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                }
                else
                {

                    var clubset = sIff.getInstance().findClubSet(pClubSet._typeid);

                    if (clubset != null && !clubset.Level.GoodLevel((byte)m_pi.level))
                    {

                        try
                        {

                            // Equipa ClubSet Padrão
                            equipDefaultClubSet(_ue);

                        }
                        catch (exception e)
                        {

                            _smp.message_pool.getInstance().push(new message("[player::checkClubSetEquiped][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                        }

                        upt_on_db = true;

                        // Não tem o level necessário para equipar esse clubset, equipa o clubset padrão
                        _smp.message_pool.getInstance().push(new message("[player::checkClubSetEquiped][Error] PLAYER[UID=" + Convert.ToString(m_pi.uid) + "] ClubSet[TYPEID=" + Convert.ToString(pClubSet._typeid) + " ID=" + Convert.ToString(pClubSet.id) + " PLAYER[Lv=" + Convert.ToString(m_pi.level) + "] nao tem o level[is_max=" + Convert.ToString(clubset.Level.is_max) + ", Lv=" + Convert.ToString((ushort)clubset.Level.level) + "] para equipar esse item. Hacker ou bug.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                    }
                    else if (clubset == null)
                    {


                        try
                        {

                            // Equipa ClubSet Padrão
                            equipDefaultClubSet(_ue);

                        }
                        catch (exception e)
                        {

                            _smp.message_pool.getInstance().push(new message("[player::checkClubSetEquiped][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                        }

                        upt_on_db = true;

                        // Não tem esse clubset no IFF_STRUCT do server, equipa clubset padrão
                        _smp.message_pool.getInstance().push(new message("[player::checkClubSetEquiped][Error] PLAYER[UID=" + Convert.ToString(m_pi.uid) + "] Not Have ClubSet[TYPEID=" + Convert.ToString(pClubSet._typeid) + ", ID=" + Convert.ToString(pClubSet.id) + "] in IFF_STRUCT of server, but it is equiped. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                    }
                }

            }
            else
            {

                try
                {

                    // Equipa ClubSet Padrão
                    equipDefaultClubSet(_ue);

                }
                catch (exception e)
                {

                    _smp.message_pool.getInstance().push(new message("[player::checkClubSetEquiped][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                }

                upt_on_db = true;

                // Não está com um clubset normal ou padrão equipado, equipa o clubset padrão
                _smp.message_pool.getInstance().push(new message("[player::checkClubSetEquiped][Error][Warning] PLAYER[UID=" + Convert.ToString(m_pi.uid) + "] nao esta com um ClubSet equipado. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            return upt_on_db;
        }

        public bool checkBallEquiped(UserEquip _ue)
        {

            bool upt_on_db = false;
            uint tmp_typeid = 0;

            if (_ue.ball_typeid != 0)
            {

                var pBall = m_pi.findWarehouseItemByTypeid(_ue.ball_typeid);

                if (pBall == null)
                {

                    // Guarda para usar no Log
                    tmp_typeid = _ue.ball_typeid;

                    try
                    {

                        // Equipa Ball padrão
                        equipDefaultBall(_ue);

                    }
                    catch (exception e)
                    {

                        _smp.message_pool.getInstance().push(new message("[player::checkBallEquiped][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                    }

                    upt_on_db = true;

                    _smp.message_pool.getInstance().push(new message("[player::checkBallEquiped][Error] PLAYER[UID=" + Convert.ToString(m_pi.uid) + "] Not Have Ball[TYPEID=" + Convert.ToString(tmp_typeid) + "], but it is equiped. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                }
                else
                {

                    var ball = sIff.getInstance().findBall(pBall._typeid);

                    if (ball != null && !ball.Level.GoodLevel((byte)m_pi.level))
                    {

                        try
                        {

                            // Equipa Ball padrão
                            equipDefaultBall(_ue);

                        }
                        catch (exception e)
                        {

                            _smp.message_pool.getInstance().push(new message("[player::checkBallEquiped][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                        }

                        upt_on_db = true;

                        // Não tem o level necessário para equipar a bola
                        _smp.message_pool.getInstance().push(new message("[player::checkBallEquiped][Error] PLAYER[UID=" + Convert.ToString(m_pi.uid) + "] Ball[TYPEID=" + Convert.ToString(pBall._typeid) + " ID=" + Convert.ToString(pBall.id) + " PLAYER[Lv=" + Convert.ToString(m_pi.level) + "] nao tem o level[is_max=" + Convert.ToString(ball.Level.is_max) + ", Lv=" + Convert.ToString((ushort)ball.Level.level) + "] para equipar esse item. Hacker ou bug.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                    }
                    else if (ball == null)
                    {

                        try
                        {

                            // Equipa Ball padrão
                            equipDefaultBall(_ue);

                        }
                        catch (exception e)
                        {

                            _smp.message_pool.getInstance().push(new message("[player::checkBallEquiped][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                        }

                        upt_on_db = true;

                        // Não tem essa bola no IFF_STRUCT do server, equipa a bola padrão
                        _smp.message_pool.getInstance().push(new message("[player::checkBallEquiped][Error] PLAYER[UID=" + Convert.ToString(m_pi.uid) + "] Not Have Ball[TYPEID=" + Convert.ToString(pBall._typeid) + ", ID=" + Convert.ToString(pBall.id) + "] in IFF_STRUCT of server, but it is equiped. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                    }
                }

            }
            else
            {

                try
                {

                    // Equipa Ball padrão
                    equipDefaultBall(_ue);

                }
                catch (exception e)
                {

                    _smp.message_pool.getInstance().push(new message("[player::checkBallEquiped][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                }

                upt_on_db = true;

                // Não está com nenhuma bola normal ou padrão equipada, equipa a bola padrão
                _smp.message_pool.getInstance().push(new message("[player::checkBallEquiped][Error][Warning] PLAYER[UID=" + Convert.ToString(m_pi.uid) + "] nao esta com uma Ball equipada. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            return upt_on_db;
        }


        public bool checkItemEquiped(UserEquip _ue)
        {
            bool upt_on_db = false;
            uint tmp_typeid = 0;
            WarehouseItemEx pWi = null;

            Dictionary<uint, uint> mp_count_same_item = new Dictionary<uint, uint>();

            for (int i = 0; i < _ue.item_slot.Length; ++i)
            {
                if (_ue.item_slot[i] != 0)
                {
                    if (!sIff.getInstance().ItemEquipavel(_ue.item_slot[i]))
                    {
                        tmp_typeid = _ue.item_slot[i];

                        _ue.item_slot[i] = 0;

                        upt_on_db = true;

                        _smp.message_pool.getInstance().push(new message("[player::checkItemEquiped][Error] PLAYER[UID=" + m_pi.uid +
                            "] Not Equipable Item[TYPEID=" + tmp_typeid + ", SLOT=" + i + "], but it is equiped. Hacker ou Bug",
                            type_msg.CL_FILE_LOG_AND_CONSOLE));
                    }
                    else if ((pWi = m_pi.findWarehouseItemByTypeid(_ue.item_slot[i])) == null)
                    {
                        tmp_typeid = _ue.item_slot[i];

                        _ue.item_slot[i] = 0;

                        upt_on_db = true;

                        _smp.message_pool.getInstance().push(new message("[player::checkItemEquiped][Error] PLAYER[UID=" + m_pi.uid +
                            "] Not Have Item[TYPEID=" + tmp_typeid + ", SLOT=" + i + "], but it is equiped. Hacker ou Bug",
                            type_msg.CL_FILE_LOG_AND_CONSOLE));
                    }
                    else
                    {
                        if (mp_count_same_item.TryGetValue(pWi._typeid, out uint count))
                        {
                            if (active_item_cant_have_2_inventory.Contains(pWi._typeid))
                            {
                                tmp_typeid = _ue.item_slot[i];

                                _ue.item_slot[i] = 0;

                                upt_on_db = true;

                                _smp.message_pool.getInstance().push(new message("[player::checkItemEquiped][Error] PLAYER[UID=" + m_pi.uid +
                                    "] Nao pode equipar 2 Ex:[Corta com (Toma ou Safety)] Item[TYPEID=" + pWi._typeid +
                                    ", ID=" + pWi.id + "] no inventory. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                            }
                            else if (pWi.STDA_C_ITEM_QNTD < (int)(count + 1))
                            {
                                tmp_typeid = _ue.item_slot[i];

                                _ue.item_slot[i] = 0;

                                upt_on_db = true;

                                _smp.message_pool.getInstance().push(new message("[player::checkItemEquiped][Error] PLAYER[UID=" + m_pi.uid +
                                    "] Nao tem quantidade do Item[TYPEID=" + pWi._typeid + ", ID=" + pWi.id +
                                    "] para equipar ele. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                            }
                            else
                            {
                                mp_count_same_item[pWi._typeid] = count + 1;
                            }
                        }
                        else
                        {
                            if (pWi.STDA_C_ITEM_QNTD < 1)
                            {
                                tmp_typeid = _ue.item_slot[i];

                                _ue.item_slot[i] = 0;

                                upt_on_db = true;

                                _smp.message_pool.getInstance().push(new message("[player::checkItemEquiped][Error] PLAYER[UID=" + m_pi.uid +
                                    "] Nao tem quantidade do Item[TYPEID=" + pWi._typeid + ", ID=" + pWi.id +
                                    "] para equipar ele. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                            }
                            else
                            {
                                mp_count_same_item.Add(pWi._typeid, 1);
                            }
                        }
                    }
                }
            }

            return upt_on_db;
        }


        public void checkAllItemEquiped(UserEquip _ue)
        {

            if (checkSkinEquiped(_ue))
            {
                snmdb.NormalManagerDB.getInstance().add(0,
                    new CmdUpdateSkinEquiped(m_pi.uid, _ue),
                    SQLDBResponse, this);
            }

            if (checkPosterEquiped(_ue))
            {
                snmdb.NormalManagerDB.getInstance().add(0,
                    new CmdUpdatePosterEquiped(m_pi.uid, _ue),
                    SQLDBResponse, this);
            }

            if (checkCharacterEquiped(_ue))
            {
                snmdb.NormalManagerDB.getInstance().add(0,
                    new CmdUpdateCharacterEquiped(m_pi.uid, (int)_ue.character_id),
                    SQLDBResponse, this);
            }

            if (checkCaddieEquiped(_ue))
            {
                snmdb.NormalManagerDB.getInstance().add(0,
                    new CmdUpdateCaddieEquiped(m_pi.uid, (int)_ue.caddie_id),
                    SQLDBResponse, this);
            }

            if (checkMascotEquiped(_ue))
            {
                snmdb.NormalManagerDB.getInstance().add(0,
                    new CmdUpdateMascotEquiped(m_pi.uid, (int)_ue.mascot_id),
                    SQLDBResponse, this);
            }

            if (checkItemEquiped(_ue))
            {
                snmdb.NormalManagerDB.getInstance().add(0,
                    new CmdUpdateItemSlot(m_pi.uid, _ue.item_slot),
                    SQLDBResponse, this);
            }

            if (checkClubSetEquiped(_ue))
            {
                snmdb.NormalManagerDB.getInstance().add(0,
                    new CmdUpdateClubsetEquiped(m_pi.uid, (int)_ue.clubset_id),
                    SQLDBResponse, this);
            }

            if (checkBallEquiped(_ue))
            {
                snmdb.NormalManagerDB.getInstance().add(0,
                    new CmdUpdateBallEquiped(m_pi.uid, _ue.ball_typeid),
                    SQLDBResponse, this);
            }
        }
        public void equipDefaultCharacter(UserEquip _ue)
        {

            // Valor padrão caso o adicionar Character de error
            var tmp_id = _ue.character_id;

            _ue.character_id = 0;
            m_pi.ei.char_info = null;

            if (m_pi.mp_ce.Count() > 0)
            {

                _smp.message_pool.getInstance().push(new message("[player::equipDefaultCharacter][Log][Warning] PLAYER[UID=" + Convert.ToString(m_pi.uid) + "] tentou verificar o Character[ID=" + Convert.ToString(tmp_id) + "] para comecar o jogo, colocando o primeiro character do player. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                m_pi.ei.char_info = m_pi.mp_ce.begin().Value;
                _ue.character_id = m_pi.ei.char_info.id;

            }
            else
            {

                _smp.message_pool.getInstance().push(new message("[player::equipDefaultCharacter][Log][Warning] PLAYER[UID=" + Convert.ToString(m_pi.uid) + "] tentou verificar o Character[ID=" + Convert.ToString(tmp_id) + "] para comecar o jogo, ele nao tem nenhum character. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                BuyItem bi = new BuyItem();
                stItem item = new stItem();
                int item_id = 0;

                bi.id = -1;
                bi._typeid = (uint)(sIff.getInstance().CHARACTER << 26); // Nuri
                bi.qntd = 1;

                ItemManager.initItemFromBuyItem(m_pi,
                    item, bi, false, 0, 0,
                    1);

                if (item._typeid != 0)
                {

                    // Add Item já atualiza o Character equipado
                    item_id = (int)ItemManager.addItem(item,
                        this, 2, 0);
                    if (item_id == (int)ItemManager.RetAddItem.T_ERROR)
                    {
                        throw new exception("[player::equipDefaultCharacter][Log][Warning] PLAYER[UID=" + Convert.ToString(m_pi.uid) + "] nao conseguiu adicionar o Character[TYPEID=" + Convert.ToString(item._typeid) + "] padrao para ele. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PLAYER,
                            2500, 2));
                    }

                }
                else
                {
                    throw new exception("[player::equipDefaultCharacter][Log][Warning][Error] PLAYER[UID=" + Convert.ToString(m_pi.uid) + "] nao conseguiu inicializar o Character[TYPEID=" + Convert.ToString(bi._typeid) + "] padrao para ele. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PLAYER,
                        2500, 1));
                }
            }
        }

        public void equipDefaultClubSet(UserEquip _ue)
        {

            // Guarda para usar no Log
            var tmp_id = _ue.clubset_id;

            // Valor padrão caso de erro no add ClubSet Padrão
            _ue.clubset_id = 0;
            m_pi.ei.clubset = null;
            m_pi.ei.csi = new ClubSetInfo();

            _smp.message_pool.getInstance().push(new message("[player::equipDefaultClubSet][Error] PLAYER[UID=" + Convert.ToString(m_pi.uid) + "] tentou verificar o Clubset[ID=" + Convert.ToString(tmp_id) + "] equipado, mas ClubSet Not exists on IFF structure. Equipa o ClubSet padrao. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

            // Coloca o ClubSet CV1 no lugar do ClubSet que acabou o tempo
            var pWi = m_pi.findWarehouseItemByTypeid(AIR_KNIGHT_SET);

            if (pWi != null)
            {

                _smp.message_pool.getInstance().push(new message("[player::equipDefaultClubSet][Log][Warning] PLAYER[UID=" + Convert.ToString(m_pi.uid) + "] tentou verificar o ClubSet[ID=" + Convert.ToString(tmp_id) + "], mas acabou o tempo do ClubSet[ID=" + Convert.ToString(tmp_id) + @"], colocando o ClubSet Padrao""CV1"" do player. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Esse C do WarehouseItem, que pega do DB, não é o ja updado inicial da taqueira é o que fica tabela enchant, 
                // que no original fica no warehouse msm, eu só confundi quando fiz
                m_pi.ei.csi.setValues(pWi.id, pWi._typeid, pWi.c);

                var cs = sIff.getInstance().findClubSet(pWi._typeid);

                if (cs != null)
                {
                    for (var j = 0; j < (m_pi.ei.csi.enchant_c.Length); ++j)
                    {
                        m_pi.ei.csi.enchant_c[j] = (short)(cs.SlotStats.getSlot[j] + pWi.clubset_workshop.c[j]);
                    }
                }

                m_pi.ei.clubset = pWi;
                _ue.clubset_id = pWi.id;

            }
            else
            {

                _smp.message_pool.getInstance().push(new message("[player::equipDefaultClubSet][Log][Warning] PLAYER[UID=" + Convert.ToString(m_pi.uid) + "] tentou verificar o ClubSet[ID=" + Convert.ToString(tmp_id) + "], mas acabou o tempo do ClubSet[ID=" + Convert.ToString(tmp_id) + @"], ele nao tem o ClubSet Padrao""CV1"". Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                BuyItem bi = new BuyItem();
                stItem item = new stItem();
                int item_id = 0;

                bi.id = -1;
                bi._typeid = AIR_KNIGHT_SET;
                bi.qntd = 1;

                ItemManager.initItemFromBuyItem(m_pi,
                   item, bi, false, 0, 0,
                    1);

                if (item._typeid != 0)
                {
                    // Add Item já atualiza o Character equipado
                    item_id = (int)ItemManager.addItem(item,
                        this, 2, 0);
                    if (item_id != (int)ItemManager.RetAddItem.T_ERROR)
                    {

                        // Equipa o ClubSet CV1
                        pWi = m_pi.findWarehouseItemById((int)item_id);

                        if (pWi != null)
                        {

                            // Esse C do WarehouseItem, que pega do DB, não é o ja updado inicial da taqueira é o que fica tabela enchant, 
                            // que no original fica no warehouse msm, eu só confundi quando fiz
                            m_pi.ei.csi.setValues(pWi.id, pWi._typeid, pWi.c);

                            var cs = sIff.getInstance().findClubSet(pWi._typeid);

                            if (cs != null)
                            {
                                for (var j = 0; j < (m_pi.ei.csi.enchant_c.Length); ++j)
                                {
                                    m_pi.ei.csi.enchant_c[j] = (short)(cs.SlotStats.getSlot[j] + pWi.clubset_workshop.c[j]);
                                }
                            }

                            m_pi.ei.clubset = pWi;
                            m_pi.ue.clubset_id = pWi.id;

                            // Update ON DB
                            snmdb.NormalManagerDB.getInstance().add(0,
                                new CmdUpdateClubsetEquiped(m_pi.uid, item_id),
                                SQLDBResponse, this);

                        }
                        else
                        {
                            throw new exception("[player::equipDefaultClubSet][Log][Warning] PLAYER[UID=" + Convert.ToString(m_pi.uid) + @"] nao conseguiu achar o ClubSet""CV1""[ID=" + Convert.ToString(item.id) + "] padrao que acabou de adicionar para ele. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PLAYER,
                                2501, 3));
                        }

                    }
                    else
                    {
                        throw new exception("[player::equipDefaultClubSet][Log][Warning] PLAYER[UID=" + Convert.ToString(m_pi.uid) + "] nao conseguiu adicionar o ClubSet[TYPEID=" + Convert.ToString(item._typeid) + "] padrao para ele. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PLAYER,
                            2501, 2));
                    }

                }
                else
                {
                    throw new exception("[player::equipDefaultClubSet][Log][Warning] PLAYER[UID=" + Convert.ToString(m_pi.uid) + "] nao conseguiu inicializar o ClubSet[TYPEID=" + Convert.ToString(bi._typeid) + "] padrao para ele. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PLAYER,
                        2501, 1));
                }
            }
        }
        public void equipDefaultBall(UserEquip _ue)
        {

            // Verifica se o player é um premium user
            try
            {

                if (m_pi.m_cap.premium_user)
                {

                    // Equipa a Ball Premium User Padrão
                    equipDefaultBallPremiumUser(_ue);

                    return; // Equipou a bola premium user com sucesso
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[player::equipDefaultBall][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            // Guarda para usar no Log
            var tmp_typeid = _ue.ball_typeid;

            // Valor padrão caso de erro no adicionar a Ball padrão
            _ue.ball_typeid = DEFAULT_COMET_TYPEID;
            m_pi.ei.comet = null;

            var pWi = m_pi.findWarehouseItemByTypeid(DEFAULT_COMET_TYPEID);

            if (pWi != null)
            {

                _smp.message_pool.getInstance().push(new message("[player::equipDefaultBall][Log][Warning] PLAYER[UID=" + Convert.ToString(m_pi.uid) + "] tentou verificar a Ball[TYPEID=" + Convert.ToString(tmp_typeid) + "] para comecar o jogo, colocando a Ball Padrao do player. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                m_pi.ei.comet = pWi;
                _ue.ball_typeid = pWi._typeid;

            }
            else
            {

                _smp.message_pool.getInstance().push(new message("[player::equipDefaultBall][Log][Warning] PLAYER[UID=" + Convert.ToString(m_pi.uid) + "] tentou trocar a Ball[TYPEID=" + Convert.ToString(tmp_typeid) + "] para comecar o jogo, ele nao tem a Ball Padrao. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                BuyItem bi = new BuyItem();
                stItem item = new stItem();
                int item_id = 0;

                bi.id = -1;
                bi._typeid = DEFAULT_COMET_TYPEID;
                bi.qntd = 1;

                ItemManager.initItemFromBuyItem(m_pi,
                    item, bi, false, 0);

                if (item._typeid != 0)
                {

                    // Add Item já atualiza o Character equipado
                    item_id = (int)ItemManager.addItem(item,
                        this, 2, 0);
                    if (item_id != (int)ItemManager.RetAddItem.T_ERROR)
                    {

                        // Equipa a Ball padrao
                        pWi = m_pi.findWarehouseItemById((int)item_id);

                        if (pWi != null)
                        {

                            m_pi.ei.comet = pWi;
                            m_pi.ue.ball_typeid = pWi._typeid;

                            // Update ON DB
                            snmdb.NormalManagerDB.getInstance().add(0,
                                new CmdUpdateBallEquiped(m_pi.uid, (uint)item_id),
                                SQLDBResponse, this);

                        }
                        else
                        {
                            throw new exception("[player::equipDefaultBall][Log][Warning] PLAYER[UID=" + Convert.ToString(m_pi.uid) + "] nao conseguiu achar a Ball[ID=" + Convert.ToString(item.id) + "] padrao que acabou de adicionar para ele. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PLAYER,
                                2502, 3));
                        }

                    }
                    else
                    {
                        throw new exception("[player::equipDefaultBall][Log][Warning] PLAYER[UID=" + Convert.ToString(m_pi.uid) + "] nao conseguiu adicionar a Ball[TYPEID=" + Convert.ToString(item._typeid) + "] padrao para ele. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PLAYER,
                            2502, 2));
                    }

                }
                else
                {
                    throw new exception("[player::equipDefaultBall][Log][Warning] PLAYER[UID=" + Convert.ToString(m_pi.uid) + "] nao conseguiu inicializar a Ball[TYPEID=" + Convert.ToString(bi._typeid) + "] padrao para ele. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PLAYER,
                        2502, 1));
                }

            }
        }
        public void equipDefaultBallPremiumUser(UserEquip _ue)
        {

            // Guarda para usar no Log
            var tmp_typeid = _ue.ball_typeid;

            // Valor padrão caso de erro no adicionar a Ball Premium User padrão
            _ue.ball_typeid = sPremiumSystem.getInstance().getPremiumBallByTicket(m_pi.pt._typeid);
            m_pi.ei.comet = null;

            var pWi = m_pi.findWarehouseItemByTypeid(_ue.ball_typeid);

            if (pWi != null)
            {

                _smp.message_pool.getInstance().push(new message("[player::equipDefaultBallPremiumUser][Log][Warning] PLAYER[UID=" + Convert.ToString(m_pi.uid) + "] tentou verificar a Ball[TYPEID=" + Convert.ToString(tmp_typeid) + "] para comecar o jogo, colocando a Ball Premium User Padrao do player. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                m_pi.ei.comet = pWi;
                _ue.ball_typeid = pWi._typeid;

            }
            else
            {

                _smp.message_pool.getInstance().push(new message("[player::equipDefaultBallPremiumUser][Log][Warning] PLAYER[UID=" + Convert.ToString(m_pi.uid) + "] tentou trocar a Ball[TYPEID=" + Convert.ToString(tmp_typeid) + "] para comecar o jogo, ele nao tem a Ball Premium User Padrao. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Add Premium Ball
                stItem item = sPremiumSystem.getInstance().addPremiumBall(this);

                if (item._typeid != 0u)
                {
                    // Update ON DB
                    snmdb.NormalManagerDB.getInstance().add(0,
                        new CmdUpdateBallEquiped(m_pi.uid, item._typeid),
                        SQLDBResponse, this);
                }
                else
                {
                    _smp.message_pool.getInstance().push(new message("[player::equipDefaultBallPremiumUser][ERROR][Warning] PLAYER[UID=" + Convert.ToString(m_pi.uid) + "] tentou trocar a Ball[TYPEID=" + Convert.ToString(tmp_typeid) + "] para comecar o jogo, mas nao conseguiu adicionar a ball premium. Hacker ou Bug.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
            }
        }
        public List<CharacterInfo> isAuxPartEquiped(uint _typeid)
        {

            List<CharacterInfo> v_ci = new List<CharacterInfo>();

            m_pi.mp_ce.ToList().ForEach(_el =>
           {
               if (_el.Value.isAuxPartEquiped(_typeid))
               {
                   v_ci.Add(_el.Value);
               }
           });

            return new List<CharacterInfo>(v_ci);
        }

        public CharacterInfo isPartEquiped(uint _typeid)
        {
            var it = m_pi.mp_ce.FirstOrDefault(_el =>
            {
                return _el.Value.isPartEquiped(_typeid);
            });

            return it.Value;
        }

        public void setMemberInfo(MemberInfoEx memberInfoEx)
        {
            m_pi.mi = memberInfoEx;
            m_pi.m_cap = memberInfoEx.capability;
            m_pi.mi.oid = m_oid;
            //m_pi.mi.state_flag.visible = true;
            //m_pi.mi.state_flag.whisper = m_pi.whisper;
            //m_pi.mi.state_flag.channel = !m_pi.whisper.IsTrue();//passar true?
        }

        public static void SQLDBResponse(int _msg_id,
            Pangya_DB _pangya_db,
            object _arg)
        {
            if (_arg == null)
            {
                return;
            }

            // Por Hora só sai, depois faço outro tipo de tratamento se precisar
            if (_pangya_db.getException().getCodeError() != 0)
            {
                _smp.message_pool.getInstance().push(new message("[player:SQLDBResponse][Error] " + _pangya_db.getException().getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                return;
            }

            // isso aqui depois pode mudar para o Item_manager, que vou tirar de ser uma classe static e usar ela como objeto(instancia)
            var _session = Tools.reinterpret_cast<Player>(_arg);

            switch (_msg_id)
            {
                case 1: // Update Caddie Info
                    {
                        break;
                    }
                case 2: // Update Mascot Info
                    {
                        break;
                    }
                case 3: // Insert CPLog Item
                    {
                        var cmd_icpli = Tools.reinterpret_cast<CmdInsertCPLogItem>(_pangya_db);

                        break;
                    }
                case 0:
                default:
                    break;
            }
        }
    }
}
