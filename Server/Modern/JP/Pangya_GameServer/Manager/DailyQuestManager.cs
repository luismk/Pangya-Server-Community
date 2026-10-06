using Pangya_GameServer.Feature;
using Pangya_GameServer.Flags;
using Pangya_GameServer.Models;
using Pangya_GameServer.PacketFunc;
using Pangya_GameServer.Repository;
using Pangya_GameServer.Server;
using Pangya_GameServer.Session;
using PangyaAPI.DataBase;
using PangyaAPI.IFF.Handle.JP;
using PangyaAPI.IFF.Regions.JP.Models.IFF;
using PangyaAPI.Network;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using static Pangya_GameServer.Models.DefineConstants;
namespace Pangya_GameServer.Manager
{
    public class DailyQuestManager
    { 
        // Auxiliares
        public static bool CheckCurrentQuestUser(DailyQuestInfo _dqi, Player _session)
        {

            if (!_session.getState())
                throw new exception("[DailyQuestManager::" + "CheckCurrentQuestUser" + "][Error] session not connected.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MGR_DAILY_QUEST,
                         1, 0));

            var pi = _session.UserInfo;

            if (pi.DailyQuests.current_date != 0)
            {

                UtilTime.TranslateDateLocal(pi.DailyQuests.current_date, out DateTime st2);// Current Date Daily Quest Player
                if (_dqi.date.Year > st2.Year || _dqi.date.Month > st2.Month || _dqi.date.Day > st2.Day)
                {
                    return true;
                }
            }
            else
                return true;

            return false;
        }

        public static bool CheckCurrentQuest(DailyQuestInfo _dqi)
        {
            if (_dqi == null)
            {
                return false;
            }
            DateTime st2 = DateTime.Now;  // Local System Now Date

            return (st2.Year > _dqi.date.Year || st2.Month > _dqi.date.Month || st2.Day > _dqi.date.Day);
        }

        public static List<RemoveDailyQuestUser> GetOldQuestUser(Player _session)
        {
            if (!_session.getState())
            {
                throw new exception("[DailyQuestManager::" + "getOldQuestUser" + "][Error] session not connected.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MGR_DAILY_QUEST,
                    1, 0));
            }

            List<RemoveDailyQuestUser> v_old_quest = new List<RemoveDailyQuestUser>();
            var map_ai = _session.UserInfo.Achievements.getAchievementInfo();

            map_ai.ToList().ForEach(el =>
            {
                if (el.Value.status == (byte)ACHIEVEMENT_STATUS.PENDENTING && el.Value._typeid != 0)
                {
                    v_old_quest.Add(new RemoveDailyQuestUser() { id = el.Value.id, _typeid = el.Value._typeid });
                }
            });

            if (v_old_quest.Count == 0)
            { // Se n�o achou no que estava no server procura no banco de dados

                CmdOldDailyQuestInfo cmd_odqi = new CmdOldDailyQuestInfo(_session.UserInfo.UID);

                snmdb.NormalManagerDB.Instance.add(0,
                    cmd_odqi, null, null);

                if (cmd_odqi.getException().getCodeError() == 0)
                {
                    v_old_quest = cmd_odqi.getInfo();
                }
            }

            return v_old_quest;
        }

        public static List<AchievementInfoEx> NewQuestUser(DailyQuestInfo _dqi, Player _session)
        { 
            List<AchievementInfoEx> v_ADQU = new List<AchievementInfoEx>();

            QuestItem qi = null;

            for (var i = 0; i < 3; ++i)
            { 
                // Add New Quest to player
                _session.UserInfo.DailyQuests._typeid[i] = _dqi._typeid[i];

                // Clear(Limpa) Estruturas, temporarias
                if ((qi = sIff.Instance.findQuestItem(_dqi._typeid[i])) != null && qi.quest.qntd > 0)
                {

                    var ai = AchievementManager.createAchievement(_session.UserInfo.UID,
                        qi, ACHIEVEMENT_STATUS.PENDENTING);

                    _session.UserInfo.Achievements.addAchievement(ai);

                    v_ADQU.Add(ai);
                }
            }

            // Seta no banco de dados a data que o player add a nova quest
            _session.UserInfo.DailyQuests.current_date = UtilTime.GetLocalTimeAsUnix();

            if (CommandDB.LoadDailyQuestCheck(_session.UserInfo.UID))
            {
                // Update Last Quest Accept Player
                snmdb.NormalManagerDB.Instance.add(0, new CmdUpdateDailyQuestUser(_session.UserInfo.UID, _session.UserInfo.DailyQuests));
            }  
            return v_ADQU;
        }

        public static List<AchievementInfoEx> LeaveQuestUser(Player _session,
            int[] _quest_id,
            int _count)
        { 
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
                it = _session.UserInfo.Achievements.findAchievementById(_quest_id[i]);
                if (it.MoveNext())
                {


                    v_ai.Add(it.Current.Value);


                    id = it.Current.Value.id;

                    _typeid = it.Current.Value._typeid;

                    // Verifica se � o daily quest 10 Clear, se for Atualizar ela(Resta para os valores iniciais)
                    if (_typeid == CLEAR_10_DAILY_QUEST_TYPEID)
                    {
                        // Reseta Achievement
                        _session.UserInfo.Achievements.resetAchievement(it);
                    }
                    else
                    {

                        // Delete from achievement
                        _session.UserInfo.Achievements.removeAchievement(it);
                    }
                }
            }

            return v_ai;
        }

        public static List<AchievementInfoEx> AcceptQuestUser(Player _session,
            int[] _quest_id,
            int _count)
        { 

            if (_quest_id == null)
            {
                throw new exception("[DailyQuestManager::AcceptQuestUser][Error] _quest_id is null", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MGR_DAILY_QUEST,
                    2, 0));
            }

            if (_count == 0)
            {
                throw new exception("[DailyQuestManager::AcceptQuestUser][Error] _count is zero", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MGR_DAILY_QUEST,
                    3, 0));
            }

            List<AchievementInfoEx> v_ai = new List<AchievementInfoEx>();
            Dictionary<uint, AchievementInfoEx>.Enumerator it;
            QuestStuff qs = null;
            CounterItemInfo cii = new CounterItemInfo();

            var map_ai = _session.UserInfo.Achievements.getAchievementInfo();

            for (var i = 0; i < _count; ++i)
            {
                it = _session.UserInfo.Achievements.findAchievementById(_quest_id[i]);
                if (it.MoveNext())
                {

                    // Add Counter Item
                    foreach (var el in it.Current.Value.v_qsi)
                    {

                        cii.clear();
                        cii.active = 1;

                        if ((qs = sIff.Instance.findQuestStuff(el._typeid)) != null)
                        {
                            cii._typeid = qs.counter_item._typeid[0];
                        }
                        else
                        {
                            throw new exception("[DailyQuestManager::AcceptQuestUser][Error] nao encontrou o quest stuff[typeid=" + Convert.ToString(el._typeid) + "] no IFF QuestStuff, para o player: " + Convert.ToString(_session.UserInfo.UID), ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MGR_DAILY_QUEST,
                                6, 0));
                        }

                        if ((el.counter_item_id = addCounterItemUser(_session, cii)) == -1)
                        {
                            throw new exception("[DailyQuestManager::AcceptQuestUser][Error] nao conseguiu adicionar o counter item[TYPEID=" + Convert.ToString(cii._typeid) + "] no banco de dados para o player: " + Convert.ToString(_session.UserInfo.UID), ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MGR_DAILY_QUEST,
                                7, 0));
                        }

                        // Add Counter To Counter Item Map of Achievement 
                        it.Current.Value.map_counter_item[cii.id] = cii;  // sobrescreve se já existir

                        // Atualiza o counter Login da quest no banco de dados
                        snmdb.NormalManagerDB.Instance.add(0,
                            new CmdUpdateQuestUser(_session.UserInfo.UID, el),
                            SQLDBResponse,
                            null);
                    }

                    // Update Achievement, Status Active == 3
                    it.Current.Value.status = 3;

                    snmdb.NormalManagerDB.Instance.add(0,
                        new CmdUpdateAchievementUser(_session.UserInfo.UID, it.Current.Value),
                        SQLDBResponse,
                        null);

                    _session.UserInfo.DailyQuests.accept_date = UtilTime.GetLocalTimeAsUnix();
                     
                    if (CommandDB.LoadDailyQuestCheck(_session.UserInfo.UID))
                    {
                        // Update Last Quest Accept Player
                        snmdb.NormalManagerDB.Instance.add(0,
                            new CmdUpdateDailyQuestUser(_session.UserInfo.UID, _session.UserInfo.DailyQuests),
                            SQLDBResponse,
                            null);
                    } 

                    v_ai.Add(it.Current.Value);
                }
            }

            return v_ai;
        }

        public static int addCounterItemUser(Player _session, CounterItemInfo _cii)
        { 

            if (_cii.isValid())
            {
                throw new exception("[DailyQuestManager::addCounterItemUser][Error] _counter_item_typeid is invalid", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MGR_DAILY_QUEST,
                    4, 0));
            }

            // Add Counter Item
            CmdAddCounterItem cmd_aci = new CmdAddCounterItem(_session.UserInfo.UID, // waitable
                _cii._typeid, _cii.value);

            snmdb.NormalManagerDB.Instance.add(0,
                cmd_aci, null, null);

            if (cmd_aci.getException().getCodeError() != 0 || (_cii.id = cmd_aci.getId()) == -1)
            {
                throw new exception("[DailyQuestManager::addCounterItemUser][Error] nao conseguiu adicionar o Counter Item[Typeid=" + Convert.ToString(_cii._typeid) + "] para o player: " + Convert.ToString(_session.UserInfo.UID), ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MGR_DAILY_QUEST,
                    5, 0));
            }

            return (int)_cii.id;
        }

        public static void UpdateDailyQuest(ref DailyQuestInfo _dqi)
        {
            if (_dqi == null)
            {
                _dqi = new DailyQuestInfo();
                return;
            }

            if (!sIff.Instance.isLoad())
            {
                sIff.Instance.Init();
            }

            var map_qi = sIff.Instance.getQuestItem(); // Daily Quest

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
                        _dqi._typeid[i] = a[Random.Shared.Next() % a.Count];
                        break;
                    case 2: // M�dio
                        _dqi._typeid[i] = b[Random.Shared.Next() % b.Count];
                        break;
                    case 3: // Dif�cil
                        _dqi._typeid[i] = c[Random.Shared.Next() % c.Count];
                        break;
                }
            }

            // Update Time Update Daily Quest
            _dqi.date.CreateTime();

            CmdUpdateDailyQuest cmd_udq = new CmdUpdateDailyQuest(_dqi); // Waiter

            snmdb.NormalManagerDB.Instance.add(0,
                cmd_udq, SQLDBResponse, null);

            if (cmd_udq.getException().getCodeError() != 0)
            {

                _smp.LogManager.Instance.push(new AppMessage("[DailyQuestManager::updateDailyQuest][ErrorSystem] " + cmd_udq.getException().getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                return; // Error sai da fun��o
            }

            // Update Aqui por que outro sistema conseguiu atualizar primeiro no banco de dados
            if (!cmd_udq.isUpdated())
            {
                _dqi = cmd_udq.getInfo();
            }
        }
         
        protected static void SQLDBResponse(int _msg_id,
           Pangya_DB _pangya_db,
           object _arg)
        {

            // Por Hora s� sai, depois fa�o outro Type de tratamento se precisar
            if (_pangya_db.getException().getCodeError() != 0)
            {
                _smp.LogManager.Instance.push(new AppMessage("[DailyQuestManager::SQLDBResponse][Error] " + _pangya_db.getException().getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
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

                            // N�o conseguiu atualizar primeiro que outro sistema, ent�o pega a atualiza��o do outro sistema
                            //if (sgs::gs != null)
                            GameServer.Instance.UpdateDailyQuest(cmd_dqi.getInfo());
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