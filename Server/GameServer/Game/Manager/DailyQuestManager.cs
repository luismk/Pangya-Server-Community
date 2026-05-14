using System;
using System.Collections.Generic;
using System.Linq;
using Pangya_GameServer.Repository;
using Pangya_GameServer.Game.Manager;
using Pangya_GameServer.Game.System;
using Pangya_GameServer.Models;
using Pangya_GameServer.PacketFunc;

using PangyaAPI.IFF.JP.Extensions;
using PangyaAPI.IFF.JP.Models.Data;
using PangyaAPI.Network.PangyaPacket;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Models;
using PangyaAPI.Utilities.Log;
using static Pangya_GameServer.Models.DefineConstants;
using static Pangya_GameServer.Models.AchievementInfo;

namespace Pangya_GameServer.Game.Manager
{
    public class DailyQuestManager
    {
        public static void requestCheckAndSendDailyQuest(Player _session, packet _packet)
        {

            if (!_session.isConnected() && !_session.getState())
                throw new exception("[DailyQuestManager::" + "CheckAndSandDailyQuest" + "][Error] session not connected.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MGR_DAILY_QUEST,
                       1, 0));

            if (_packet == null)
                throw new exception("[DailyQuestManager::request" + "CheckAndSandDailyQuest" + "][Error] _packet is null", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MGR_DAILY_QUEST,
                         2, 0));

            try
            {
                if (checkCurrentQuestUser(sgs.gs.getInstance().getDailyQuestInfo, _session))
                {
                    // Get Old Quest do player
                    var old_quest = getOldQuestUser(_session);

                    foreach (var el in old_quest)
                        _session.m_pi.mgr_achievement.removeAchievement(el.id);

                    // Add nova quest para o player
                    var v_ai = addQuestUser(sgs.gs.getInstance().getDailyQuestInfo, _session);

                    var p = new PangyaBinaryWriter((ushort)0x216);

                    p.WriteInt32((int)UtilTime.GetSystemTimeAsUnix());

                    // Achievement
                    if (v_ai.Count > 0)
                    {

                        p.WriteInt32(v_ai.Count);

                        foreach (var el in v_ai)
                        {
                            p.WriteByte(2);
                            p.WriteUInt32(el._typeid);//ta vindo id repetido
                            p.WriteInt32(el.id);//tá vindo idex repedito
                            p.WriteUInt32(0); // type
                            p.WriteInt32(0); // Qntd antes
                            p.WriteInt32(1); // Qntd depois
                            p.WriteInt32(1); // add value
                            p.WriteZeroByte(25);
                        }
                    }
                    else
                    {
                        p.WriteUInt32(0u);
                    }

                    packet_func.session_send(p,
                        _session, 1);



                    packet_func.session_send(packet_func.pacote225(_session.m_pi.dqiu,
                        old_quest),
                        _session, 1);

                }
                else
                {
                    var p = new PangyaBinaryWriter((ushort)0x216);

                    p.WriteUInt32((uint)UtilTime.GetSystemTimeAsUnix());
                    p.WriteInt32(0);

                    packet_func.session_send(p,
                        _session, 1);

                    packet_func.session_send(packet_func.pacote225(_session.m_pi.dqiu,
                        new List<RemoveDailyQuestUser>()),
                        _session, 1);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[DailyQuestManager::requestCheckAndSendDailyQuest][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public static void requestLeaveQuest(Player _session, packet _packet)
        {
            {
                if (!_session.isConnected() && !_session.getState())
                {
                    throw new exception("[DailyQuestManager::" + (("LeaveQuest")) + "][Error] session not connected.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MGR_DAILY_QUEST,
                        1, 0));
                }
            }
            ;
            if (_packet == null)
            {
                throw new exception("[DailyQuestManager::request" + "LeaveQuest" + "][Error] _packet is null", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MGR_DAILY_QUEST,
                    2, 0));
            }

            var p = new PangyaBinaryWriter();

            int[] quest_id = null;

            try
            {

                int num_quest = _packet.ReadInt32();

                if (num_quest <= 0u)
                {
                    throw new exception("[DailyQuestManager::requestLeaveQuest][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou desistir da quest, mas o numero de quest para desistir e 0. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MGR_DAILY_QUEST,
                        5010, 0));
                }

                quest_id = _packet.ReadInt32(num_quest);

                var v_quest = leaveQuestUser(_session,
                    quest_id, num_quest);

                Dictionary<int, CounterItemInfo> map_cii = new Dictionary<int, CounterItemInfo>();

                foreach (var el in v_quest)
                {
                    foreach (var kvp in el.map_counter_item)
                    {
                        map_cii[(int)kvp.Key] = kvp.Value;  // ou Add se tiver certeza que não existe a chave
                    }
                }

                p.init_plain(0x216);

                p.WriteInt32((int)UtilTime.GetSystemTimeAsUnix());

                if (map_cii.Count > 0)
                {

                    p.WriteInt32(map_cii.Count);

                    foreach (var el in map_cii)
                    {

                        p.WriteByte(2);
                        p.WriteUInt32(el.Value._typeid);
                        p.WriteInt32(el.Value.id);
                        p.WriteUInt32(0); // type
                        p.WriteInt32(el.Value.value); // Qntd antes
                        p.WriteInt32(0); // Qntd depois
                        p.WriteUInt32((uint)(el.Value.value * -1)); // add quantos, tipo de add tinha 0(antes) + 3(qntd) = 3(depois)
                        p.WriteZeroByte(25);
                    }

                }
                else
                {
                    p.WriteInt32(0);
                }

                packet_func.session_send(p,
                    _session, 1);

                packet_func.session_send(packet_func.pacote228(v_quest),
                    _session, 1);

                if (quest_id == null)
                {
                    quest_id = null;
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[DailyQuestManager::requestLeaveQuest][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                var v_ai = new List<AchievementInfoEx>();

                packet_func.session_send(packet_func.pacote228(v_ai, 1),
                    _session, 0);

                if (quest_id != null)
                {
                    quest_id = null;
                }
            }
        }

        public static void requestAcceptQuest(Player _session, packet _packet)
        {
            {
                if (!_session.isConnected() && !_session.getState())
                {
                    throw new exception("[DailyQuestManager::" + (("AcceptQuest")) + "][Error] session not connected.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MGR_DAILY_QUEST,
                        1, 0));
                }
            }
            ;
            if (_packet == null)
            {
                throw new exception("[DailyQuestManager::request" + "AcceptQuest" + "][Error] _packet is null", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MGR_DAILY_QUEST,
                    2, 0));
            }

            var p = new PangyaBinaryWriter();

            int[] quest_id = null;

            try
            {

                int num_quest = _packet.ReadInt32();

                if (num_quest <= 0u)
                {
                    throw new exception("[DailyQuestManager::requestAcceptQuest][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou aceitar o Daily Quest, mas o numero de quest para aceitar is invalid(zero). Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MGR_DAILY_QUEST,
                        5000, 0));
                }

                quest_id = new int[num_quest]; 

                quest_id = _packet.ReadInt32(num_quest);


                var v_quest = acceptQuestUser(_session,
                    quest_id, num_quest);

                Dictionary<int, CounterItemInfo> map_cii = new Dictionary<int, CounterItemInfo>();

                foreach (var el in v_quest)
                {
                    foreach (var kvp in el.map_counter_item)
                    {
                        map_cii[(int)kvp.Key] = kvp.Value;  // ou Add se tiver certeza que não existe a chave
                    }
                }

                p = new PangyaBinaryWriter((ushort)0x216);

                p.WriteInt32((int)UtilTime.GetSystemTimeAsUnix());

                if (map_cii.Count > 0)
                {
                    p.WriteInt32(map_cii.Count);

                    foreach (var el in map_cii)
                    {
                        p.WriteByte(2);
                        p.WriteUInt32(el.Value._typeid);
                        p.WriteInt32(el.Value.id);
                        p.WriteUInt32(0); // type
                        p.WriteInt32(0); // Qntd antes
                        p.WriteInt32(0); // Qntd depois
                        p.WriteInt32(0); // add quantos, tipo de add tinha 0(antes) + 3(qntd) = 3(depois)
                        p.WriteZeroByte(25);
                    }
                }
                else
                {
                    p.WriteInt32(0);
                }

                packet_func.session_send(p,
                    _session, 1);


                packet_func.session_send(packet_func.pacote226(v_quest),
                    _session, 1);

                if (quest_id != null)
                {
                    quest_id = null;
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[DailyQuestManager::requestAcceptQuest][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                var v_ai = new List<AchievementInfoEx>();

                packet_func.session_send(packet_func.pacote226(v_ai, 1),
                    _session, 1);

                if (quest_id != null)
                {
                    quest_id = null;
                }
            }
        }

        public static void requestTakeRewardQuest(Player _session, packet _packet)
        {
            {
                if (!_session.isConnected() && !_session.getState())
                {
                    throw new exception("[DailyQuestManager::" + (("TakeRewardQuest")) + "][Error] session not connected.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MGR_DAILY_QUEST,
                        1, 0));
                }
            }
            ;
            if (_packet == null)
            {
                throw new exception("[DailyQuestManager::request" + "TakeRewardQuest" + "][Error] _packet is null", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MGR_DAILY_QUEST,
                    2, 0));
            }

            var p = new PangyaBinaryWriter();

            int[] quest_id = null;

            try
            {

                int num_quest = _packet.ReadInt32();

                if (num_quest <= 0u)
                {
                    throw new exception("[DailyQuestManager::requestTakeRewardQuest][Error] numero de quest para pegar recompensa e 0", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MGR_DAILY_QUEST,
                        5005, 0));
                }

                quest_id = _packet.ReadInt32(num_quest);

                var v_quest = leaveQuestUser(_session, quest_id, num_quest);

                QuestItem qi = null;

                List<stItem> v_item = new List<stItem>();
                stItem item = new stItem();

                // UPADATE Achievement ON SERVER, DB and GAME
                AchievementSystem sys_achievement = new AchievementSystem();

                // Add Reward Item do player
                foreach (var el in v_quest)
                {

                    // Item Reward, d� para o player
                    if ((qi = sIff.getInstance().findQuestItem(el._typeid)) != null)
                    {
                        for (var i = 0; i < (qi.reward._typeid.Length); ++i)
                        {

                            if (qi.reward._typeid[i] != 0)
                            {

                                item.clear();

                                item.type = 2;
                                item.id = -1;
                                item._typeid = qi.reward._typeid[i];
                                item.qntd = (int)qi.reward.qntd[i];
                                item.c[0] = (short)item.qntd;
                                item.c[3] = (short)qi.reward.time[i];

                                // Add Item no db e no player
                                var rt = ItemManager.RetAddItem.T_INIT_VALUE;

                                if ((rt = ItemManager.addItem(item,
                                    _session, 0, 0)) < 0)
                                {
                                    throw new exception("[DailyQuestManager::requestTakeRewardQuest][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou pegar a recompensa da Quest[TYPEID=" + Convert.ToString(el._typeid) + ", ID=" + Convert.ToString(el.id) + "], mas nao conseguiu adicionar o Item[TYPEID=" + Convert.ToString(item._typeid) + "]", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MGR_DAILY_QUEST, 1500, 0));
                                }

                                if (rt != ItemManager.RetAddItem.T_SUCCESS_PANG_AND_EXP_AND_CP_POUCH)
                                {
                                    v_item.Add(item);
                                }
                            }
                        }

                        // S� add o contador de clear quest pega recompensa nas quest normais, a box de 10 clear quest n�o add ao contador
                        if (el._typeid != CLEAR_10_DAILY_QUEST_TYPEID)
                        {
                            sys_achievement.incrementCounter(0x6C40009F);
                        }
                    }
                }

                // Add os Counter Item Excluido do player
                foreach (var el in v_quest)
                {

                    if (!el.map_counter_item.empty())
                    {

                        foreach (var el2 in el.map_counter_item)
                        {

                            item.clear();

                            item.type = 2;
                            item.id = el2.Value.id;
                            item._typeid = el2.Value._typeid;
                            item.qntd = (int)(el2.Value.value * -1);
                            item.c[0] = (short)item.qntd;
                            item.stat.qntd_ant = (int)el2.Value.value;
                            item.stat.qntd_dep = item.stat.qntd_ant + item.qntd;
                            v_item.Add(item);
                        }
                    }
                }
 
                // UPDATE ON GAME
                p.init_plain(0x216);

                p.WriteUInt32((uint)UtilTime.GetSystemTimeAsUnix());

                p.WriteInt32(v_item.Count); // Att 2 Item

                foreach (var el in v_item)
                {
                    p.WriteByte(el.type);
                    p.WriteUInt32(el._typeid);
                    p.WriteInt32(el.id); // id do item no banco de dados
                    p.WriteUInt32(el.flag_time); // type
                    p.WriteBytes(el.stat.ToArray());
                    p.WriteInt32((el.c[3] > 0) ? el.c[3] : el.c[0]);
                    p.WriteZeroByte(25);
                }

                packet_func.session_send(p,
                    _session, 1);

                packet_func.session_send(packet_func.pacote227(v_quest),
                    _session, 1);

                // UPADATE Achievement ON SERVER, DB and GAME
                sys_achievement.finish_and_update(_session);

                if (quest_id != null)
                {
                    quest_id = null;
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[DailyQuestManager::requestTakeRewardQuest][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                var v_ai = new List<AchievementInfoEx>();

                var pk = packet_func.pacote227(v_ai,
                      (int)((ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) == (uint)STDA_ERROR_TYPE.CHANNEL) ? ExceptionError.STDA_SYSTEM_ERROR_DECODE(e.getCodeError()) : 500050));

                packet_func.session_send(pk,
                    _session, 1);

                if (quest_id != null)
                {
                    quest_id = null;
                }
            }
        }

        // Auxiliares
        public static bool checkCurrentQuestUser(DailyQuestInfo _dqi, Player _session)
        {

            if (!_session.isConnected() && !_session.getState())
                throw new exception("[DailyQuestManager::" + "checkCurrentQuestUser" + "][Error] session not connected.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MGR_DAILY_QUEST,
                         1, 0));

            var pi = _session.m_pi;

            if (pi.dqiu.current_date != 0)
            {

                UtilTime.TranslateDateLocal(pi.dqiu.current_date, out DateTime st2);// Current Date Daily Quest Player
                if (_dqi.date.Year > st2.Year
                    || _dqi.date.Month > st2.Month
                    || _dqi.date.Day > st2.Day)
                {
                    return true;
                }
            }
            else
                return true;

            return false;
        }

        public static bool checkCurrentQuest(DailyQuestInfo _dqi)
        {
            if (_dqi == null )
            {
                return false;
            }
            DateTime st2 = DateTime.Now;  // Local System Now Date

            return (st2.Year > _dqi.date.Year || st2.Month > _dqi.date.Month || st2.Day > _dqi.date.Day);
        }

        public static List<RemoveDailyQuestUser> getOldQuestUser(Player _session)
        {
            if (!_session.isConnected() && !_session.getState())
            {
                throw new exception("[DailyQuestManager::" + "getOldQuestUser" + "][Error] session not connected.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MGR_DAILY_QUEST,
                    1, 0));
            }

            List<RemoveDailyQuestUser> v_old_quest = new List<RemoveDailyQuestUser>();
            var map_ai = _session.m_pi.mgr_achievement.getAchievementInfo();

            map_ai.ToList().ForEach(el =>
            {
                if (el.Value.status == (byte)ACHIEVEMENT_STATUS.PENDENTING && el.Value._typeid != 0)
                {
                    v_old_quest.Add(new RemoveDailyQuestUser() { id = el.Value.id, _typeid = el.Value._typeid });
                }
            });

            if (v_old_quest.Count == 0)
            { // Se n�o achou no que estava no server procura no banco de dados

                CmdOldDailyQuestInfo cmd_odqi = new CmdOldDailyQuestInfo(_session.m_pi.uid);

                snmdb.NormalManagerDB.getInstance().add(0,
                    cmd_odqi, null, null);

                if (cmd_odqi.getException().getCodeError() == 0)
                {
                    v_old_quest = cmd_odqi.getInfo();
                }
            }

            return v_old_quest;
        }

        public static List<AchievementInfoEx> addQuestUser(DailyQuestInfo _dqi, Player _session)
        {

            {
                if (!_session.isConnected() && !_session.getState())
                {
                    throw new exception("[DailyQuestManager::" + "addQuestUser" + "][Error] session not connected.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MGR_DAILY_QUEST,
                        1, 0));
                }
            }
            ;

            List<AchievementInfoEx> v_ADQU = new List<AchievementInfoEx>();

            QuestItem qi = null;

            for (var i = 0; i < 3; ++i)
            {

                // Add New Quest to player
                _session.m_pi.dqiu._typeid[i] = _dqi._typeid[i];

                // Clear(Limpa) Estruturas, temporarias

                if ((qi = sIff.getInstance().findQuestItem(_dqi._typeid[i])) != null && qi.quest.qntd > 0)
                {

                    var ai = AchievementManager.createAchievement(_session.m_pi.uid,
                        qi, ACHIEVEMENT_STATUS.PENDENTING);

                    _session.m_pi.mgr_achievement.addAchievement(ai);

                    v_ADQU.Add(ai);
                }
            }

            // Seta no banco de dados a data que o player add a nova quest
            _session.m_pi.dqiu.current_date = UtilTime.GetLocalTimeAsUnix();

            snmdb.NormalManagerDB.getInstance().add(0,
                new CmdUpdateDailyQuestUser(_session.m_pi.uid, _session.m_pi.dqiu),
                SQLDBResponse,
                null);

            return v_ADQU;
        }

        public static List<AchievementInfoEx> leaveQuestUser(Player _session,
            int[] _quest_id,
            int _count)
        {

            {
                if (!_session.isConnected() && !_session.getState())
                {
                    throw new exception("[DailyQuestManager::" + "leaveQuestUser" + "][Error] session not connected.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MGR_DAILY_QUEST,
                        1, 0));
                }
            }
            ;

            if (_quest_id == null)
            {
                throw new exception("[DailyQuestManager::leaveQuestUser][Error] _quest_id is null", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MGR_DAILY_QUEST,
                    2, 0));
            }

            if (_count == 0)
            {
                throw new exception("[DailyQuestManager::leaveQuestUser][Error] _count is zero", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MGR_DAILY_QUEST,
                    3, 0));
            }

            List<AchievementInfoEx> v_ai = new List<AchievementInfoEx>();
            Dictionary<uint, AchievementInfoEx>.Enumerator it;

            int id = -1;
            uint _typeid = 0;

            for (var i = 0; i < _count; ++i)
            {
                it = _session.m_pi.mgr_achievement.findAchievementById(_quest_id[i]);
                if (it.MoveNext())
                {


                    v_ai.Add(it.Current.Value);


                    id = it.Current.Value.id;

                    _typeid = it.Current.Value._typeid;

                    // Verifica se � o daily quest 10 Clear, se for Atualizar ela(Resta para os valores iniciais)
                    if (_typeid == CLEAR_10_DAILY_QUEST_TYPEID)
                    { 
                        // Reseta Achievement
                        _session.m_pi.mgr_achievement.resetAchievement(it); 
                    }
                    else
                    {

                        // Delete from achievement
                        _session.m_pi.mgr_achievement.removeAchievement(it);
                    }
                }
            }

            return v_ai;
        }

        public static List<AchievementInfoEx> acceptQuestUser(Player _session,
            int[] _quest_id,
            int _count)
        {

            {
                if (!_session.isConnected() && !_session.getState())
                {
                    throw new exception("[DailyQuestManager::" + "acceptQuestUser" + "][Error] session not connected.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MGR_DAILY_QUEST,
                        1, 0));
                }
            }
            ;

            if (_quest_id == null)
            {
                throw new exception("[DailyQuestManager::acceptQuestUser][Error] _quest_id is null", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MGR_DAILY_QUEST,
                    2, 0));
            }

            if (_count == 0)
            {
                throw new exception("[DailyQuestManager::acceptQuestUser][Error] _count is zero", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MGR_DAILY_QUEST,
                    3, 0));
            }

            List<AchievementInfoEx> v_ai = new List<AchievementInfoEx>();
            Dictionary<uint, AchievementInfoEx>.Enumerator it;
            QuestStuff qs = null;
            CounterItemInfo cii = new CounterItemInfo();

            var map_ai = _session.m_pi.mgr_achievement.getAchievementInfo();

            for (var i = 0; i < _count; ++i)
            {
                it = _session.m_pi.mgr_achievement.findAchievementById(_quest_id[i]);
                if (it.MoveNext())
                {

                    // Add Counter Item
                    foreach (var el in it.Current.Value.v_qsi)
                    {

                        cii.clear();
                        cii.active = 1;

                        if ((qs = sIff.getInstance().findQuestStuff(el._typeid)) != null)
                        {
                            cii._typeid = qs.counter_item._typeid[0];
                        }
                        else
                        {
                            throw new exception("[DailyQuestManager::acceptQuestUser][Error] nao encontrou o quest stuff[typeid=" + Convert.ToString(el._typeid) + "] no IFF QuestStuff, para o player: " + Convert.ToString(_session.m_pi.uid), ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MGR_DAILY_QUEST,
                                6, 0));
                        }

                        if ((el.counter_item_id = addCounterItemUser(_session, cii)) == -1)
                        {
                            throw new exception("[DailyQuestManager::acceptQuestUser][Error] nao conseguiu adicionar o counter item[TYPEID=" + Convert.ToString(cii._typeid) + "] no banco de dados para o player: " + Convert.ToString(_session.m_pi.uid), ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MGR_DAILY_QUEST,
                                7, 0));
                        }

                        // Add Counter To Counter Item Map of Achievement 
                        it.Current.Value.map_counter_item[cii.id] = cii;  // sobrescreve se já existir

                        // Atualiza o counter id da quest no banco de dados
                        snmdb.NormalManagerDB.getInstance().add(0,
                            new CmdUpdateQuestUser(_session.m_pi.uid, el),
                            SQLDBResponse,
                            null);
                    }

                    // Update Achievement, Status Active == 3
                    it.Current.Value.status = 3;

                    snmdb.NormalManagerDB.getInstance().add(0,
                        new CmdUpdateAchievementUser(_session.m_pi.uid, it.Current.Value),
                        SQLDBResponse,
                        null);

                    _session.m_pi.dqiu.accept_date = UtilTime.GetLocalTimeAsUnix();

                    // Update Last Quest Accept Player
                    snmdb.NormalManagerDB.getInstance().add(0,
                        new CmdUpdateDailyQuestUser(_session.m_pi.uid, _session.m_pi.dqiu),
                        SQLDBResponse,
                        null);

                    v_ai.Add(it.Current.Value);
                }
            }

            return v_ai;
        }

        public static int addCounterItemUser(Player _session, CounterItemInfo _cii)
        {

            {
                if (!_session.isConnected() && !_session.getState())
                {
                    throw new exception("[DailyQuestManager::" + "addCounterItemUser" + "][Error] session not connected.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MGR_DAILY_QUEST,
                        1, 0));
                }
            }
            ;

            if (_cii.isValid())
            {
                throw new exception("[DailyQuestManager::addCounterItemUser][Error] _counter_item_typeid is invalid", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MGR_DAILY_QUEST,
                    4, 0));
            }

            // Add Counter Item
            CmdAddCounterItem cmd_aci = new CmdAddCounterItem(_session.m_pi.uid, // waitable
                _cii._typeid, _cii.value);

            snmdb.NormalManagerDB.getInstance().add(0,
                cmd_aci, null, null);

            if (cmd_aci.getException().getCodeError() != 0 || (_cii.id = cmd_aci.getId()) == -1)
            {
                throw new exception("[DailyQuestManager::addCounterItemUser][Error] nao conseguiu adicionar o Counter Item[Typeid=" + Convert.ToString(_cii._typeid) + "] para o player: " + Convert.ToString(_session.m_pi.uid), ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MGR_DAILY_QUEST,
                    5, 0));
            }

            return (int)_cii.id;
        }

        public static void updateDailyQuest(ref DailyQuestInfo _dqi)
        {
            if (_dqi == null)
            {
              _dqi = new DailyQuestInfo();//inicia para testar...
            }

            if (!sIff.getInstance().isLoad())
            {
                sIff.getInstance().initilation();
            }

            var map_qi = sIff.getInstance().getQuestItem(); // Daily Quest

            List<uint> a = new List<uint>();
            List<uint> b = new List<uint>();
            List<uint> c = new List<uint>();

            map_qi.ForEach(el =>
            {
                if (((el.ID & 0x00FFFFFF) >> 16) < 0x40)
                {
                    switch (el.type)
                    {
                        case 1:
                        default:
                            a.Add(el.ID);
                            break;
                        case 2:
                            b.Add(el.ID);
                            break;
                        case 3:
                            c.Add(el.ID);
                            break;
                    }
                }
            });

            for (var i = 0; i < 3u; ++i)
            {

                switch (i + 1)
                {
                    case 1: // F�cil
                    default:
                        _dqi._typeid[i] = a[new Random().Next() % a.Count];
                        break;
                    case 2: // M�dio
                        _dqi._typeid[i] = b[new Random().Next() % b.Count];
                        break;
                    case 3: // Dif�cil
                        _dqi._typeid[i] = c[new Random().Next() % c.Count];
                        break;
                }
            }

            // Update Time Update Daily Quest
            _dqi.date.CreateTime();

            CmdUpdateDailyQuest cmd_udq = new CmdUpdateDailyQuest(_dqi); // Waiter

            snmdb.NormalManagerDB.getInstance().add(0, cmd_udq, SQLDBResponse, null);

            if (cmd_udq.getException().getCodeError() != 0)
            {

                _smp.message_pool.getInstance().push(new message("[DailyQuestManager::updateDailyQuest][ErrorSystem] " + cmd_udq.getException().getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                return; // Error sai da fun��o
            }

            // Update Aqui por que outro sistema conseguiu atualizar primeiro no banco de dados
            if (!cmd_udq.isUpdated())
            {
                _dqi = cmd_udq.getInfo();
            }
        }

        public static void removeQuestUser(Player _session, List<RemoveDailyQuestUser> _v_el)
        {

            {
                if (!_session.isConnected() && !_session.getState())
                {
                    throw new exception("[DailyQuestManager::" + "removeQuestUser" + "][Error] session not connected.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MGR_DAILY_QUEST,
                        1, 0));
                }
            }
            ;

            if (!_v_el.empty())
            {
                snmdb.NormalManagerDB.getInstance().add(0,
                    new CmdDeleteDailyQuest(_session.m_pi.uid, _v_el),
                    SQLDBResponse,
                    null);
            }
        }


        protected static void SQLDBResponse(int _msg_id,
           Pangya_DB _pangya_db,
           object _arg)
        {

            // Por Hora s� sai, depois fa�o outro tipo de tratamento se precisar
            if (_pangya_db.getException().getCodeError() != 0)
            {
                _smp.message_pool.getInstance().push(new message("[DailyQuestManager::SQLDBResponse][Error] " + _pangya_db.getException().getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                return;
            }

            switch (_msg_id)
            {
                case 1: // Update Daily Quest
                    {

                        var cmd_dqi = (CmdUpdateDailyQuest)(_pangya_db);

                        if (cmd_dqi.isUpdated())
                        {
                        }
                        else
                        { 
                            sgs.gs.getInstance().updateDailyQuest(cmd_dqi.getInfo());
                        }

                        break;
                    }
                case 0:
                default:
                    break;
            }
        }
    }
}