using Pangya_GameServer.Models;
using Pangya_GameServer.PacketFunc;
using Pangya_GameServer.Repository;
using PangyaAPI.IFF.JP.Extensions;
using PangyaAPI.IFF.JP.Models.Data;
using PangyaAPI.IFF.JP.Models.Flags;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Models;
using PangyaAPI.Utilities.Log;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using static Pangya_GameServer.Models.AchievementInfo;
using static Pangya_GameServer.Models.DefineConstants;
namespace Pangya_GameServer.Game.Manager
{
    public class AchievementManager
    {

        protected Dictionary<uint, AchievementInfoEx> map_ai = new Dictionary<uint, AchievementInfoEx>();

        protected uint m_uid = new uint(); // Owner(dono)

        protected uint m_pontos = new uint(); // Todos os pontos do achievement

        protected bool m_state; 
        public AchievementManager()
        {
            this.m_uid = 0;
            this.map_ai = new Dictionary<uint, AchievementInfoEx>();
            this.m_state = false;
            this.m_pontos = 0; 
        }

        public void clear()
        {

            if (map_ai.Any())
            {
                map_ai.Clear();
            }

            m_state = false;
            m_pontos = 0;
        }

        public void initAchievement(uint _uid, bool _create = false)
        {

            if (_uid == 0u)
            {
                throw new exception("[AchievementManager::initAchievement][Error] _uid is invalid", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MGR_ACHIEVEMENT,
                    2000, 0));
            }

            m_uid = _uid;

            initAchievement(_create);
        }

        public void initAchievement(uint _uid, Dictionary<uint, List<AchievementInfoEx>> _mp_achievement)
        {

            if (_uid == 0u)
            {
                throw new exception("[AchievementManager::initAchievement][Error] _uid is invalid", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MGR_ACHIEVEMENT,
                    2000, 1));
            }

            m_uid = _uid;

            foreach (var values in _mp_achievement.Values)
            {
                foreach (var el in values)
                    addAchievement(el); // Add Achievement
            }

            m_state = true;
        }

        // Gets
        public List<CounterItemInfo> getCounterItemInfo()
        {
            if (!m_state)
            {
                throw new exception("[AchievementManager::getCounterItemInfo][Error] Manager Achievement state is invalid, please call method initAchievement first.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MGR_ACHIEVEMENT,
                    1000, 0));
            }

            List<CounterItemInfo> v_cii = new List<CounterItemInfo>();

            map_ai.ToList().ForEach(el =>
            {
                el.Value.map_counter_item.ToList().ForEach(el2 =>
                 {
                     v_cii.Add(el2.Value);
                 });
            });

            return v_cii;
        }

        public Dictionary<uint, AchievementInfoEx> getAchievementInfo()
        {
            if (!m_state)
            {
                throw new exception("[AchievementManager::getAchievementInfo][Error] Manager Achievement state is invalid, please call method initAchievement first.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MGR_ACHIEVEMENT,
                    1000, 0));
            }

            return map_ai;
        }

        public uint getPontos()
        {
            if (!m_state)
                throw new exception("[AchievementManager::getPontos][Error] Manager Achievement state is invalid, please call method initAchievement first.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MGR_ACHIEVEMENT,
                        1000, 0));

            return m_pontos;
        }

        // Sets

        // Reset Achievement para os valores iniciais
        public void resetAchievement(int _id)
        {
            {
                if (!m_state)
                {
                    throw new exception("[AchievementManager::resetAchievement][Error] Manager Achievement state is invalid, please call method initAchievement first.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MGR_ACHIEVEMENT,
                        1000, 0));
                }
            }
            ;

            if (_id <= 0)
            {
                throw new exception("[AchievementManager::resetAchievement][Error] invalid achievement[ID=" + Convert.ToString(_id) + "]", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MGR_ACHIEVEMENT,
                    1600, 0));
            }

            resetAchievement(findAchievementById(_id));
        }

        public void resetAchievement(Dictionary<uint, AchievementInfoEx>.Enumerator _it)
        {
            {
                if (!m_state)
                {
                    throw new exception("[AchievementManager::resetAchievement][Error] Manager Achievement state is invalid, please call method initAchievement first.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MGR_ACHIEVEMENT,
                        1000, 0));
                }
            }
            ;

            if (_it.Current.Value == null)
            {
                throw new exception("[AchievementManager::resetAchievement][Error] Enumerator achievement invalid", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MGR_ACHIEVEMENT,
                    1601, 0));
            }


            // Zera o(s) Counter Item(ns)
            if (_it.Current.Value.map_counter_item.Any())
            {

                foreach (var el in _it.Current.Value.map_counter_item)
                {

                    el.Value.value = 0;

                    snmdb.NormalManagerDB.getInstance().add(4,  new CmdUpdateCounterItem(m_uid, el.Value),
                        SQLDBResponse,
                        this);
                }
            }

            // Zera o clear date unix da(s) Quest(s)
            if (!_it.Current.Value.v_qsi.empty())
            {

                foreach (var el in _it.Current.Value.v_qsi)
                {

                    el.clear_date_unix = 0;

                    snmdb.NormalManagerDB.getInstance().add(5,
                        new CmdUpdateQuestUser(m_uid, el),
                        SQLDBResponse,
                        this);
                }
            }

            // Atualiza o Achievement
            _it.Current.Value.status = (int)ACHIEVEMENT_STATUS.ACTIVED;

            snmdb.NormalManagerDB.getInstance().add(6,
                new CmdUpdateAchievementUser(m_uid, _it.Current.Value),
                SQLDBResponse,
                this);

            // Log
            _smp.message_pool.getInstance().push(new message("[AchievementManager::resetAchievement][Log] PLAYER[UID=" + Convert.ToString(m_uid) + "] Resetou Achievement[TYPEID=" + Convert.ToString(_it.Current.Value._typeid) + ", ID=" + Convert.ToString(_it.Current.Value.id) + "] para os valores iniciais com sucesso", type_msg.CL_FILE_LOG_AND_CONSOLE));
        }

        // Remove Achievement
        public void removeAchievement(int _id)
        {
            if (!m_state)
                throw new exception("[AchievementManager::removeAchievement][Error] Manager Achievement state is invalid, please call method initAchievement first.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MGR_ACHIEVEMENT,
                       1000, 0));

            if (_id <= 0)
                throw new exception("[AchievementManager::removeAchievement][Error] invalid achievement[ID=" + Convert.ToString(_id) + "]", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MGR_ACHIEVEMENT,
                        10, 0));

            var it = map_ai
                    .Where(kv => kv.Value.id == _id)
                    .ToDictionary(kv => kv.Key, kv => kv.Value);
            if (it.Count > 0)
                removeAchievement(it.GetEnumerator());
        }

        public void removeAchievement(Dictionary<uint, AchievementInfoEx>.Enumerator _it)
        {
            if (!m_state)
            {
                throw new exception("[AchievementManager::removeAchievement][Error] Manager Achievement state is invalid, please call method initAchievement first.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MGR_ACHIEVEMENT,
                    1000, 0));
            }

            if (_it.Current.Key <= 0)
                _it.MoveNext();

            if (_it.Current.Key <= 0)
            {
                throw new exception("[AchievementManager::removeAchievement][Error] Enumerator achievement invalid", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MGR_ACHIEVEMENT,
                    12, 0));
            }

            // Delete Counter Item
            if ((_it.Current.Value != null) && !_it.Current.Value.map_counter_item.empty())
            {
                snmdb.NormalManagerDB.getInstance().add(1,
                    new CmdDeleteCounterItem(m_uid, _it.Current.Value.map_counter_item),
                    SQLDBResponse,
                    this); ;
            }

            // Delete Quest
            if ((_it.Current.Value != null) && !_it.Current.Value.v_qsi.empty())
            {
                snmdb.NormalManagerDB.getInstance().add(2,
                    new CmdDeleteQuest(m_uid, _it.Current.Value.v_qsi),
                    SQLDBResponse,
                    this);
            }
            if ((_it.Current.Value != null))
            {

                // Delete Achievement
                snmdb.NormalManagerDB.getInstance().add(3,
                    new CmdDeleteAchievement(m_uid, (int)_it.Current.Value.id),
                    SQLDBResponse,
                    this);

                var id = _it.Current.Value.id;

                map_ai.Remove(_it.Current.Key);

            }
        }

        // Add Achievement

        // Method Not Necessary CHECK_STATE because it is part of initialization of class
        public AchievementInfoEx addAchievement(AchievementInfoEx _ai)
        {
            var it_new = map_ai.insert(_ai._typeid, _ai);

            if (it_new.Key == 0)
                throw new exception("[AchievementManager::addAchievement][Error] nao conseguiu inserir o achievement no multimap",
                    ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MGR_ACHIEVEMENT, 11, 0));

            // Processa pontos ou rewards
            QuestStuff qsi = null;

            if (sIff.getInstance().getItemGroupIdentify(_ai._typeid) == IFF_GROUP.ACHIEVEMENT
                && (_ai.status == (byte)AchievementInfo.ACHIEVEMENT_STATUS.ACTIVED
                    || _ai.status == (byte)AchievementInfo.ACHIEVEMENT_STATUS.CONCLUEDED))
            {
                foreach (var el in _ai.v_qsi)
                {
                    if (el.clear_date_unix != 0 && (qsi = sIff.getInstance().findQuestStuff(el._typeid)) != null)
                    {
                        for (var i = 0; i < qsi.reward_item._typeid.Length; ++i)
                        {
                            if (qsi.reward_item._typeid[i] != 0 && qsi.reward_item._typeid[i] == 0x6C000001)
                                m_pontos += qsi.reward_item._typeid[i];
                        }
                    }
                }
            }

            // Retorna diretamente o item adicionado
            return _ai;
        }


        // Sender
        public void sendAchievementGuiToPlayer(Player _session)
        {
            if (!m_state)
            {
                throw new exception("[AchievementManager::sendAchievementGuiToPlayer][Error] Manager Achievement state is invalid, please call method initAchievement first.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MGR_ACHIEVEMENT,
                    1000, 0));
            }
            if (!(_session).isConnected() || !(_session).getState())
            {
                throw new exception("[AchievementManager::sendAchievementGuiToPlayer][Error] session is invalid", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MGR_ACHIEVEMENT,
                    1, 0));
            }
             
            // Tem que passar o 22D com os dados, e 22C para mostrar o GUI 
            packet_func.session_send(Build(map_ai.Values.ToList(), 20, 2), _session);

            packet_func.session_send(packet_func.pacote22C(), _session); // SUCCESS 
        }

        public void sendAchievementToPlayer(Player _session)
        {
            if (!m_state)
            {
                throw new exception("[AchievementManager::sendAchievementToPlayer][Error] Manager Achievement state is invalid, please call method initAchievement first.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MGR_ACHIEVEMENT,
                    1000, 0));
            }
            if (!(_session).isConnected() || !(_session).getState())
            {
                throw new exception("[AchievementManager::sendAchievementToPlayer][Error] session is invalid", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MGR_ACHIEVEMENT,
                    1, 0));
            }

            packet_func.session_send(Build(map_ai.Values.ToList(), 20, 0), _session);

        }

        public void sendCounterItemToPlayer(Player _session)
        {
            if (!m_state)
            {
                throw new exception("[AchievementManager::sendCounterItemToPlayer][Error] Manager Achievement state is invalid, please call method initAchievement first.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MGR_ACHIEVEMENT,
                    1000, 0));
            }
            if (!(_session).isConnected() || !(_session).getState())
            {
                throw new exception("[AchievementManager::sendCounterItemToPlayer][Error] session is invalid", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MGR_ACHIEVEMENT,
                    1, 0));
            }

            var v_element = getCounterItemInfo();

            packet_func.session_send(Build(v_element, 20, 1), _session);
        } // FIM MAKE_SPLIT_PACKET --- // Fim do primeiro FOR, então estou usando o end desse MACRO por que ele colocar no VECTOR no final

        private PangyaBinaryWriter Build<T>(List<T> list, byte tipo = 0)
        {
            try
            {
                if (tipo == 0)
                    return PacketFunc.packet_func.pacote21E(list as List<AchievementInfoEx>);
                else if (tipo == 1)
                    return PacketFunc.packet_func.pacote21D(list as List<CounterItemInfo>);
                else if (tipo == 2)
                    return PacketFunc.packet_func.pacote22D(list as List<AchievementInfoEx>);
                else
                    return PacketFunc.packet_func.pacote21D(list as List<CounterItemInfo>);
            }
            catch
            {
                return new PangyaBinaryWriter();
            }
        }

        /// <summary>
        /// Criar o packet de cada pacote, com limite de envio, recomendado, é 20
        /// </summary>
        /// <typeparam name="T">t seria uma conversor, com lista de elementos por exemplo</typeparam>
        /// <param name="counters">list por exemplo</param>
        /// <param name="itensPerPacket">total que sera enviado por lista, no maximo é 20</param>
        /// <param name="tipo">0=pacote021E, 1=pacote021D, 2 = pacote022D, outro esta desconhecido@@</param>
        /// 

        private List<PangyaBinaryWriter> Build<T>(List<T> counters, int itensPerPacket, byte tipo)
        {
            var responses = new List<PangyaBinaryWriter>();
            if (counters.Count * 196 < (1000 - 100))//envio normal
            {
                responses.Add(Build(counters, tipo));
            }
            else
            {
                var splitList = counters.ToList().Split(itensPerPacket); //ChunkBy(this.ToList(), totalBySplit);

                //Percorre lista e adiciona ao resultado
                splitList.ForEach(lista => responses.Add(Build(lista, tipo)));
            }
            return responses;
        }

        // Increasers
        public void incrementPoint(uint _increase = 1u)
        {
            if (!m_state)
            {
                throw new exception("[AchievementManager::incrementPoint][Error] Manager Achievement state is invalid, please call method initAchievement first.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MGR_ACHIEVEMENT,
                    1000, 0));
            }

            m_pontos += _increase;
        }

        // Auxíliar
        public CounterItemInfo findCounterItemById(int _id)
        {
            {
                if (!m_state)
                {
                    throw new exception("[AchievementManager::findCounterItemById][Error] Manager Achievement state is invalid, please call method initAchievement first.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MGR_ACHIEVEMENT,
                        1000, 0));
                }
            }
            ;

            if (_id <= 0)
            {
                throw new exception("[AchievementManager::findCounterItemById][Error] _id is invalid", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MGR_ACHIEVEMENT,
                    13, 0));
            }

            CounterItemInfo cii = null;

            foreach (var el in map_ai)
            {
                if ((cii = el.Value.findCounterItemById(_id)) != null)
                {
                    return cii;
                }
            }

            return cii;
        }

        public CounterItemInfo findCounterItemByTypeid(uint _typeid)
        {
            {
                if (!m_state)
                {
                    throw new exception("[AchievementManager::findCounterItemByTypeid][Error] Manager Achievement state is invalid, please call method initAchievement first.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MGR_ACHIEVEMENT,
                        1000, 0));
                }
            }
            ;

            if (_typeid == 0)
            {
                throw new exception("[AchievementManager::findCounterItemByTypeid][Error] _typeid is invalid", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MGR_ACHIEVEMENT,
                    13, 0));
            }

            CounterItemInfo cii = null;

            foreach (var el in map_ai)
            {
                if ((cii = el.Value.findCounterItemByTypeId(_typeid)) != null)
                {
                    return cii;
                }
            }

            return cii;
        }

        public QuestStuffInfo findQuestStuffById(int _id)
        {
            {
                if (!m_state)
                {
                    throw new exception("[AchievementManager::findQuestStuffById][Error] Manager Achievement state is invalid, please call method initAchievement first.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MGR_ACHIEVEMENT,
                        1000, 0));
                }
            }
            ;

            if (_id <= 0)
            {
                throw new exception("[AchievementManager::findQuestStuffById][Error] _id is invalid", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MGR_ACHIEVEMENT,
                    13, 0));
            }

            QuestStuffInfo qsi = null;

            foreach (var el in map_ai)
            {
                if ((qsi = el.Value.findQuestStuffById(_id)) != null)
                {
                    return qsi;
                }
            }

            return qsi;
        }

        public QuestStuffInfo findQuestStuffByTypeId(uint _typeid)
        {
            {
                if (!m_state)
                {
                    throw new exception("[AchievementManager::findQuestStuffByTypeid][Error] Manager Achievement state is invalid, please call method initAchievement first.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MGR_ACHIEVEMENT,
                        1000, 0));
                }
            }
            ;

            if (_typeid == 0)
            {
                throw new exception("[AchievementManager::findQuestStuffByTypeId][Error] _typeid is invalid", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MGR_ACHIEVEMENT,
                    13, 0));
            }

            QuestStuffInfo qsi = null;

            foreach (var el in map_ai)
            {
                if ((qsi = el.Value.findQuestStuffByTypeId(_typeid)) != null)
                {
                    return qsi;
                }
            }

            return qsi;
        }

        public Dictionary<uint, AchievementInfoEx>.Enumerator findAchievementById(int _id)
        {
            if (!m_state)
            {
                throw new exception("[AchievementManager::removeAchievement][Error] Manager Achievement state is invalid, please call method initAchievement first.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MGR_ACHIEVEMENT,
                    1000, 0));
            }

            if (_id <= 0)
            {
                throw new exception("[AchievementManager::findAchievementById][Error] _id is invalid", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MGR_ACHIEVEMENT,
                    13, 0));
            }
            var filtered = map_ai
        .Where(kv => kv.Value.id == _id)
        .ToDictionary(kv => kv.Key, kv => kv.Value);

            return filtered.GetEnumerator();
        }

        // Statics methods

        // Static Method
        public static AchievementInfoEx createAchievement(uint _uid,
            Achievement _achievement,
            ACHIEVEMENT_STATUS _status)
        {

            if (_achievement.ID <= 0)
            {
                throw new exception("[AchievementManager::createAchievement(IFF::Achievement)][Error] achievement invalid", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MGR_ACHIEVEMENT,
                    1, 0));
            }

            QuestStuffInfo qsi = new QuestStuffInfo();
            CounterItemInfo cii = new CounterItemInfo
            {
                active = 1
            };
            AchievementInfoEx ai = new AchievementInfoEx
            {
                active = 1,
                status = (int)_status,
                _typeid = _achievement.ID,
                quest_base_typeid = _achievement.TypeID_Quest_Index
            };

            CmdCreateQuest cmd_cq = new CmdCreateQuest(_uid, true);
            CmdCreateAchievement cmd_ca = new CmdCreateAchievement(_uid, true);

            QuestStuff qs = null;

            if (_achievement.Quest_TypeID[0] > 0)
            {
                //CmdCreateQuest cmd_cq(pi->uid, *qi, 3/*1 pendente, 2 excluida, 3 ativa, 4 concluida*/);
                var name = (_achievement.Name);

                cmd_ca.setAchievement(_achievement.ID,
                    name, (uint)_status);

                snmdb.NormalManagerDB.getInstance().add(0,
                    cmd_ca, null, null);

                var i = 0;
                if (cmd_ca.getException().getCodeError() == 0 && (ai.id = cmd_ca.getID()) != -1)
                {


                    cmd_cq.setAchievementID(((uint)ai.id));

                    do
                    {
                        if (_achievement.Quest_TypeID[i] != 0 && (qs = sIff.getInstance().findQuestStuff(_achievement.Quest_TypeID[i])) != null)
                        {

                            qsi.clear();

                            qsi._typeid = _achievement.Quest_TypeID[i];
                            cii._typeid = qs.counter_item._typeid[0];

                            cmd_cq.setQuest(qs, _achievement.TypeID_Quest_Index == 0 || _achievement.TypeID_Quest_Index == _achievement.Quest_TypeID[i]);

                            snmdb.NormalManagerDB.getInstance().add(0,
                                cmd_cq, null, null);

                            qsi.id = (int)cmd_cq.getID();
                            cii.id = (int)cmd_cq.getCounterItemID();

                            if (cmd_cq.getException().getCodeError() != 0
                                || qsi.id == -1
                                || (cmd_cq.getIncludeCounter() && cii.id == 0))
                            {
                                throw new exception("[AchievementManager::createAchievement(IFF::Achievement)][Error] nao conseguiu criar quest para o player: " + Convert.ToString(_uid), ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MGR_ACHIEVEMENT,
                                    3, 0));
                            }

                            // Add o Counter Item Id para o Quest Stuff Info, se a quest tem o counter item
                            if (cmd_cq.getIncludeCounter())
                            {
                                qsi.counter_item_id = cii.id;
                            }

                            cii.value = 0;

                            ai.v_qsi.Add(qsi);

                            if (cii.id > 0)
                            {
                                ai.map_counter_item[cii.id] = cii;
                            }
                        }
                    } while (++i < (_achievement.Quest_TypeID.Length));

                    // Atualiza os counter item id nas quest stuff se o achievement esta com o quest base
                    var it = ai.getQuestBase();

                    if (it != null)
                    {

                        foreach (var el in ai.v_qsi)
                        { 
                            // Update
                            if (el.counter_item_id == 0)
                            {
                                el.counter_item_id = it.counter_item_id;
                            }
                        }
                    }

                }
                else
                {
                    throw new exception("[AchievementManager::createAchievement(IFF::Achievement)][Error] nao conseguiu criar achievement para o player: " + Convert.ToString(_uid), ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MGR_ACHIEVEMENT,
                        2, 0));
                }
            }
            else
            {
                throw new exception("[AchievementManager::createAchievement(IFF::Achievement)][Error] not have quest on achievement, achievement invalid", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MGR_ACHIEVEMENT,
                    1, 1));
            }

            return ai;
        }


        // Static Method
        public static AchievementInfoEx createAchievement(uint _uid, QuestItem _qi, ACHIEVEMENT_STATUS _status)
        {

            if (_qi.ID == 0)
            {
                throw new exception("[AchievementManager::createAchievement(IFF::QuestItem)][Error] QuestItem invalid", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MGR_ACHIEVEMENT,
                    1, 0));
            }

            QuestStuffInfo qsi = new QuestStuffInfo();
            CounterItemInfo cii = new CounterItemInfo
            {
                active = 1
            };
            AchievementInfoEx ai = new AchievementInfoEx
            {
                active = 1,
                status = (int)_status,
                _typeid = _qi.ID
            };

            CmdCreateQuest cmd_cq = new CmdCreateQuest(_uid, true); // Waitable
            CmdCreateAchievement cmd_ca = new CmdCreateAchievement(_uid); // Waitable

            QuestStuff qs = null;

            if (_qi.quest.qntd > 0 || _qi.quest._typeid[0] > 0)
            {
                //CmdCreateQuest cmd_cq(pi->uid, *qi, 3/*1 pendente, 2 excluida, 3 ativa, 4 concluida*/);
                var name = (_qi.Name);

                cmd_ca.setAchievement(_qi.ID,
                    name, (uint)_status);

                snmdb.NormalManagerDB.getInstance().add(0,
                    cmd_ca, null, null);

                if (cmd_ca.getException().getCodeError() == 0 && (ai.id = cmd_ca.getID()) != -1)
                {

                    var i = 0;

                    cmd_cq.setAchievementID((uint)(ai.id));

                    do
                    {
                        if (_qi.quest._typeid[i] != 0 && (qs = sIff.getInstance().findQuestStuff(_qi.quest._typeid[i])) != null)
                        {

                            qsi.clear();

                            qsi._typeid = _qi.quest._typeid[i];
                            cii._typeid = qs.counter_item._typeid[0];

                            cmd_cq.setQuest(qs, (_status != ACHIEVEMENT_STATUS.PENDENTING) ? true : false);

                            snmdb.NormalManagerDB.getInstance().add(0,
                                cmd_cq, null, null);

                            qsi.id = (int)cmd_cq.getID();
                            cii.id = (int)cmd_cq.getCounterItemID();

                            if (cmd_cq.getException().getCodeError() != 0
                                || qsi.id == -1
                                || (cmd_cq.getIncludeCounter() && cii.id == 0))
                            {
                                throw new exception("[AchievementManager::createAchievement(IFF::QuestItem)][Error] nao conseguiu criar quest para o player: " + Convert.ToString(_uid), ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MGR_ACHIEVEMENT,
                                    3, 0));
                            }

                            // Add o Counter Item Id para o Quest Stuff Info, se a quest tem o counter item
                            if (cmd_cq.getIncludeCounter())
                            {

                                qsi.counter_item_id = cii.id;
                            }

                            cii.value = 0;

                            ai.v_qsi.Add(qsi);

                            if (cii.id > 0)
                            {

                                ai.map_counter_item[cii.id] = cii;
                            }
                        }
                    } while (++i < _qi.quest.qntd);

                    // Atualiza os counter item id nas quest stuff se o achievement esta com o quest base
                    var it = ai.getQuestBase();


                    if (it != null)
                    {

                        foreach (var el in ai.v_qsi)
                        { 
                            // Update
                            if (el.counter_item_id == 0)
                            {
                                el.counter_item_id = it.counter_item_id;
                            }
                        }
                    }

                }
                else
                {
                    throw new exception("[AchievementManager::createAchievement(IFF::QuestItem][Error] nao conseguiu criar achievement para o player: " + Convert.ToString(_uid), ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MGR_ACHIEVEMENT,
                        2, 0));
                }
            }
            else
            {
                throw new exception("[AchievementManager::createAchievement(IFF::QuestItem)][WARING] not have counter item on QuestItem. invalid QuestItem", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MGR_ACHIEVEMENT,
                    1, 1));
            }
            return ai;
        }


        protected void initAchievement(bool create)
        {
            var swTotal = Stopwatch.StartNew();

            try
            {
                if (!create)
                {
                    var swCheck = Stopwatch.StartNew();
                    bool hasAchievement = HasAchievementInDB();
                    swCheck.Stop(); 

                    if (!hasAchievement)
                        CreateAllAchievements();
                    else
                        LoadAchievementsFromDB();
                }
                else
                {
                    CreateAllAchievements();
                }

                m_state = true;
            }
            catch
            {
                m_state = false;
                throw;
            }
            finally
            {
                swTotal.Stop(); 
            }
        }

        private bool HasAchievementInDB()
        {
            var cmd = new CmdCheckAchievement(m_uid);
            snmdb.NormalManagerDB.getInstance().add(0, cmd, null, null);

            if (cmd.getException().getCodeError() != 0)
                throw cmd.getException();

            return cmd.getLastState();
        }

        private void CreateAllAchievements()
        {
            var iff = sIff.getInstance();
            var list = new List<AchievementInfoEx>(128);

            // Daily Quest 10 dias
            var qi = iff.findQuestItem(CLEAR_10_DAILY_QUEST_TYPEID);
            if (qi != null)
            {
                list.Add(AchievementManager.createAchievement(
                    m_uid, qi, ACHIEVEMENT_STATUS.ACTIVED));
            }

            // Todos os achievements
            foreach (var it in iff.getAchievement())
            {
                list.Add(AchievementManager.createAchievement(
                    m_uid, it, ACHIEVEMENT_STATUS.ACTIVED));
            }

            addAchievementRange(list); 
        }

        private void LoadAchievementsFromDB()
        {
            var cmd = new CmdAchievementInfo(m_uid);
            snmdb.NormalManagerDB.getInstance().add(0, cmd, null, null);

            if (cmd.getException().getCodeError() != 0)
                throw cmd.getException();

            foreach (var values in cmd.GetInfo().Values)
                foreach (var ach in values)
                    addAchievement(ach);
        }

        private void addAchievementRange(IEnumerable<AchievementInfoEx> list)
        {
            foreach (var ai in list)
                addAchievement(ai);
        }


        protected static void SQLDBResponse(int _msg_id,
            Pangya_DB _pangya_db,
            object _arg)
        {

            if (_arg == null)
            {
                _smp.message_pool.getInstance().push(new message("[AchievementManager::SQLDBResponse][Error] _arg is null", type_msg.CL_FILE_LOG_AND_CONSOLE));
                return;
            }

            // Por Hora só sai, depois faço outro tipo de tratamento se precisar
            if (_pangya_db.getException().getCodeError() != 0)
            {
                _smp.message_pool.getInstance().push(new message("[AchievementManager::SQLDBResponse][Error] " + _pangya_db.getException().getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                return;
            }

            var mgr_achievement = (AchievementManager)(_arg);

            switch (_msg_id)
            {
                case 1: // Delete Counter Item
                    {
                        var cmd_dci = (CmdDeleteCounterItem)(_pangya_db);

                        break;
                    }
                case 2: // Delete Quest
                    {
                        var cmd_dq = (CmdDeleteQuest)(_pangya_db);

                        break;
                    }
                case 3: // Delete Achievement
                    {
                        var cmd_da = (CmdDeleteAchievement)(_pangya_db);
                        break;
                    }
                case 4: // Update Counter Item
                    {
                        var cmd_uci = (CmdUpdateCounterItem)(_pangya_db);
                        break;
                    }
                case 5: // Update Quest User
                    {
                        var cmd_uqu = (CmdUpdateQuestUser)(_pangya_db);
                        break;
                    }
                case 6: // Update Achievement User
                    {
                        var cmd_uau = (CmdUpdateAchievementUser)(_pangya_db);
                        break;
                    }
                case 0:
                default:
                    break;
            }
        }
    }
}
