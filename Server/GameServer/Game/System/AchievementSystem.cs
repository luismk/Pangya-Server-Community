using Pangya_GameServer.Game.Manager;
using Pangya_GameServer.Models;
using Pangya_GameServer.PacketFunc;
using Pangya_GameServer.Repository;
using PangyaAPI.IFF.JP.Extensions;
using PangyaAPI.IFF.JP.Models.Data;
using PangyaAPI.IFF.JP.Models.Flags;
using PangyaAPI.Network.PangyaSession;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Models;
using PangyaAPI.Utilities.Log;
using System;
using System.Collections.Generic;
using System.Linq;
using static Pangya_GameServer.Models.AchievementInfo;
namespace Pangya_GameServer.Game.System
{
    public class AchievementSystem
    {

        private Dictionary<uint, Counter> v_counters = new Dictionary<uint, Counter>();
        protected List<QuestClear> v_quest_clear = new List<QuestClear>();
        protected Dictionary<int, CounterItemCtx> map_cii_change = new Dictionary<int, CounterItemCtx>();

        protected List<Reward> v_reward = new List<Reward>();
        public class Counter
        {
            public void clear()
            {
            }
            public Counter()
            { }
            public Counter(uint _id, int count)
            {
                _typeid = _id;
                value = count;
            }
            public uint _typeid = new uint();
            public int value = 0;
        }

        public class CounterItemCtx : CounterItemInfo
        {
            public CounterItemCtx()
            {
                clear();
            }
            public CounterItemCtx(CounterItemInfo _cii,
                int _last_value,
                int _increase_value) : base(_cii)
            {
                this.last_value = _last_value;
                this.increase_value = _increase_value;
            }
            public int last_value = 0;
            public int increase_value = 0;

            public override void clear()
            { 
                this.last_value = 0;
                this.increase_value = 0;
                base.clear(); 
            }
        }

        // Guarda o typeid do achievement e o typeid da ques concluída do player
        public class QuestClear
        {

            public uint achievement_typeid = new uint();
            public uint quest_typeid = new uint();

            public QuestClear(uint typeid1, uint typeid2)
            {
                achievement_typeid = typeid1;
                quest_typeid = typeid2;
            }
            public QuestClear() { }
        }

        public class Reward
        {
            public uint _typeid = new uint();
            public int qntd = new int();
            public int time = new int();

            public Reward(uint id, int qty, int times)
            {
                _typeid = id;
                qntd = qty;
                time = times;
            }
            public Reward()
            { }
        }

        public enum eSTATE : byte
        {
            NOT_MATCH = 0,
            INCREMENT_COUNTER = 1,
            CLEAR = 2
        }

        public AchievementSystem()
        {
            this.v_counters = new Dictionary<uint, Counter>();
            this.v_quest_clear = new List<QuestClear>();
            this.map_cii_change = new Dictionary<int, CounterItemCtx>();
            this.v_reward = new List<Reward>();

            if (!sIff.getInstance().isLoad())
            {
                sIff.getInstance().initilation();
            }

            // Tempor�rio CounterItemInfo
            var counterInfos = sIff.getInstance().getCounterItem();
            foreach (var el in counterInfos)
            {
                var c = new Counter
                {
                    value = 0,
                    _typeid = el.ID
                };

                v_counters[el.ID] = c;
            }
        }

        ~AchievementSystem()
        {
            clear();
        }

        public void clear()
        {

            if (v_counters.Count > 0)
            {
                v_counters.Clear();

            }

            if (v_quest_clear.Count > 0)
            {
                v_quest_clear.Clear();
            }

            if (map_cii_change.Count > 0)
            {
                map_cii_change.Clear();
            }

            if (v_reward.empty())
            {
                v_reward.Clear();
            }
        }

        public void incrementCounter(uint _typeid)
        {
            // Em C#, preferimos verificar argumentos com ArgumentException
            if (_typeid == 0)
            {
                throw new ArgumentException("[SysAchievement::IncrementCounter][Error] typeId is invalid (zero)");
            }
            // Assumindo que v_c seja uma List<CounterItem>
            // O foreach é o padrão ouro no C# para essa operação
            foreach (var el in v_counters.Values)
            {
                if (el._typeid == _typeid)
                {
                    el.value++;
                }
            }
        }

        public void incrementCounter(uint typeId, int _value)
        {
            // Em C#, preferimos verificar argumentos com ArgumentException
            if (typeId == 0)
            {
                throw new ArgumentException("[SysAchievement::IncrementCounter][Error] typeId is invalid (zero)");
            }

            if (_value == 0)
            {
                throw new ArgumentException($"[SysAchievement::IncrementCounter][Error] CounterItem[typeid={typeId}] value is zero");
            }

            foreach (var el in v_counters.Values)
            {
                if (el._typeid == typeId)
                {
                    el.value += _value;
                }
            }
        }
        public void decrementCounter(uint _typeid)
        {

            if (_typeid == 0)
            {
                throw new exception("[AchievementSystem::decrementCounter][Error] _typeid is invalid", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.SYS_ACHIEVEMENT,
                    2, 0));
            }
            foreach (var el in v_counters.Values)
            {
                if (el._typeid == _typeid)
                {
                    el.value--;
                }
            }
        }

        public void CHECK_CHANGE_COUNTER_AND_INCREMENT_AND_SAVE(CounterItemInfo pcii, Counter _c)
        {
            // Verifica se já processamos esse counter nesta iteração
            if (!map_cii_change.ContainsKey(pcii.id))
            {
                pcii.value += _c.value;
            }

            // Criamos o contexto de mudança 
            int diff = _c.value;
            int oldValue = (pcii.value - _c.value);

            map_cii_change[pcii.id] = new CounterItemCtx(pcii, oldValue, diff);
        }


        public void finish_and_update(Player _session)
        {
            if (!_session.isConnected())
            {
                throw new exception("[AchievementSystem::" + "finish_and_update" + "][Error] _session don't connected", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.SYS_ACHIEVEMENT,
                    1, 0));
            }

            var p = new PangyaBinaryWriter();

            try
            {

                /// INICIO DO NOVO MODO
                var v_counter = getCounterChanged();
                var v_ai = getAchievementChanged(_session, v_counter);

                // envia a altera��o nos contadores e no achievements
                if (v_ai.Count != 0)
                { 
                    if (map_cii_change.Count != 0)
                    {
                        p.init_plain(0x216);

                        p.WriteInt32((int)UtilTime.GetSystemTimeAsUnix());

                        p.WriteUInt32((uint)map_cii_change.Count);

                        foreach (var el in map_cii_change)
                        {

                            // Atualiza o Counter no banco de dados
                            snmdb.NormalManagerDB.getInstance().add(3, new CmdUpdateCounterItem(_session.m_pi.uid, el.Value),  SQLDBResponse, this);

                            p.WriteByte(2); // Type
                            p.WriteUInt32(el.Value._typeid);
                            p.WriteInt32(el.Value.id);
                            p.WriteInt32(0); // Flag
                            p.WriteInt32(el.Value.last_value); // Qtnd Antes
                            p.WriteInt32(el.Value.value); // Qtnd Depois
                            p.WriteInt32(el.Value.increase_value); // Qtnd que add
                            p.WriteZeroByte(25); // 25 bytes que n�o usa com esse tipo de item
                        }

                        packet_func.session_send(p, _session, 1);
                    }

                    if (v_reward.Count != 0)
                    {

                        List<stItem> v_item = new List<stItem>();
                        stItem item = new stItem();

                        foreach (var el in v_reward)
                        {

                            item = new stItem
                            {
                                type = 2,
                                _typeid = el._typeid,
                                qntd = (int)el.qntd
                            };
                            item.STDA_C_ITEM_QNTD = (short)item.qntd;
                            item.c[2] = (short)el.time;

                            var rt = ItemManager.RetAddItem.T_INIT_VALUE;

                            if ((rt = ItemManager.addItem(item,
                                _session, 0, 0)) < 0)
                            {
                                throw new exception("[AchievementSystem::finish_and_update][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou adicionar o item[TYPEID=" + Convert.ToString(item._typeid) + "], mas nao conseguiu adicionar o item. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.SYS_ACHIEVEMENT,
                                    50, 0));
                            }

                            if (rt != ItemManager.RetAddItem.T_SUCCESS_PANG_AND_EXP_AND_CP_POUCH)
                            {
                                v_item.Add(new stItem(item));
                            }
                        }

                        p.init_plain(0x216);

                        p.WriteInt32((int)UtilTime.GetSystemTimeAsUnix());

                        p.WriteUInt32((uint)v_item.Count);

                        foreach (var el in v_item)
                        {
                            p.WriteByte(el.type); // Type
                            p.WriteUInt32(el._typeid);
                            p.WriteInt32(el.id);
                            p.WriteInt32(el.flag); // Flag
                            p.WriteBytes(el.stat.ToArray());
                            p.WriteInt32((el.STDA_C_ITEM_TIME > 0) ? el.STDA_C_ITEM_TIME : el.STDA_C_ITEM_QNTD); // Qtnd que add
                            p.WriteZeroByte(25); // 25 bytes que n�o usa com esse tipo de item
                        }

                        packet_func.session_send(p, _session, 1);
                    }

                    // Aqui tem que manda o pacote22e(quando uma quest(daily ou achievement) foi concluida) e 220(Att Achievement)
                    p.init_plain(0x22E);

                    p.WriteUInt32((uint)v_quest_clear.Count);

                    foreach (var el in v_quest_clear)
                    {
                        p.WriteUInt32(el.achievement_typeid);
                        p.WriteUInt32(el.quest_typeid);
                    }

                    packet_func.session_send(p,
                        _session, 1);

                    CounterItemInfo cii = null;

                    p.init_plain(0x220);

                    p.WriteInt32(0); // SUCCESS

                    p.WriteUInt32((uint)v_ai.Count);

                    foreach (var el in v_ai)
                    {
                        p.WriteByte(el.active);
                        p.WriteUInt32(el._typeid);
                        p.WriteInt32(el.id);
                        p.WriteInt32(el.status);
                        p.WriteUInt32((uint)el.v_qsi.Count);

                        foreach (var el2 in el.v_qsi)
                        {
                            p.WriteUInt32(el2._typeid);

                            if (el2.counter_item_id > 0 && (cii = el.findCounterItemById(el2.counter_item_id)) != null)
                            {
                                p.WriteUInt32(cii._typeid);
                                p.WriteUInt32((uint)cii.id);
                            }
                            else // n�o tem o counter item id e nem o typeid
                            {
                                p.WriteZeroByte(8);
                            }

                            p.WriteUInt32(el2.clear_date_unix);
                        }
                    }

                    packet_func.session_send(p,
                        _session, 1);
                }

            }
            catch (exception e)
            {

                //packet_func::session_send(packet_func::pacote227(&_session, std::vector< AchievementInfoEx >(), 1), &_session, 1);

                // N�o relan�a mais por que a resposta de quest o cliente j� teve, aqui � achievement, que deu erro ele nao precisa saber n�o
                // Mas o server precisa do log para ajeitar depois
                _smp.message_pool.getInstance().push(new message("[AchievementSystem::finish_and_update][Error] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        /**
        * O segundo argumento força a pegar todos countadores, mesmos das quests concluída
        */
        public static List<CounterItemInfo> getCounterItemInfo(List<AchievementInfoEx> _v_ai, bool _all_force)
        {

            List<CounterItemInfo> v_countersii = new List<CounterItemInfo>();

            _v_ai.ForEach((el) =>
            {
                SortedDictionary<int, CounterItemInfo> map_cii = new SortedDictionary<int, CounterItemInfo>();
                CounterItemInfo cii = null;
                el.v_qsi.ForEach(el2 =>
                {
                    if ((_all_force || el2.clear_date_unix == 0) && (cii = el.findCounterItemById(el2.counter_item_id)) != null)
                    {
                        map_cii[(int)cii.id] = cii;
                    }
                });
                foreach (var el3 in map_cii)
                {
                    v_countersii.Add(el3.Value);
                }
            });

            return v_countersii;
        }

        // get Typeid do Counter Item do Character TypeId
        public static uint getCharacterCounterTypeId(uint _character_typeid)
        {

            uint counter_typeid = 0u;

            switch (_character_typeid)
            {
                case 0x4000000: // Nuri
                    counter_typeid = 0x6C40000Fu;
                    break;
                case 0x4000001: // Hana
                    counter_typeid = 0x6C400010u;
                    break;
                case 0x4000002: // Azer
                    counter_typeid = 0x6C400011u;
                    break;
                case 0x4000003: // Cecilia
                    counter_typeid = 0x6C400012u;
                    break;
                case 0x4000004: // Max
                    counter_typeid = 0x6C400013u;
                    break;
                case 0x4000005: // Kooh
                    counter_typeid = 0x6C400014u;
                    break;
                case 0x4000006: // Arin
                    counter_typeid = 0x6C400015u;
                    break;
                case 0x4000007: // Kaz
                    counter_typeid = 0x6C400016u;
                    break;
                case 0x4000008: // Lucia
                    counter_typeid = 0x6C400017u;
                    break;
                case 0x4000009: // Nell
                    counter_typeid = 0x6C400018u;
                    break;
                case 0x400000A: // Spika
                    counter_typeid = 0x6C400040u;
                    break;
                case 0x400000B: // Nuri R
                    counter_typeid = 0x6C4000B9u;
                    break;
                case 0x400000C: // Hana R
                    counter_typeid = 0x6C4000BAu;
                    break;
                case 0x400000E: // Cecilia R
                    counter_typeid = 0x6C4000C4u;
                    break;
            }

            return (counter_typeid);
        }

        // get Typeid do Counter Item do Caddie TypeId
        public static uint getCaddieCounterTypeId(uint _caddie_typeid)
        {

            uint counter_typeid = 0u;

            switch (_caddie_typeid)
            {
                case 0x1C000000: // Ancient Papel
                    counter_typeid = 0x6C400019u;
                    break;
                case 0x1C000001: // Ancient Pippin
                    counter_typeid = 0x6C40001Cu;
                    break;
                case 0x1C000002: // Ancient Titan Boo
                    counter_typeid = 0x6C40001Bu;
                    break;
                case 0x1C000003: // Ancient Dolfini
                    counter_typeid = 0x6C400042u;
                    break;
                case 0x1C000004: // Ancient Lolo
                    counter_typeid = 0x6C400043u;
                    break;
                case 0x1C000005: // Ancient Quma
                    counter_typeid = 0x6C400041u;
                    break;
                case 0x1C000006: // Ancient  Tiki
                    counter_typeid = 0x6C400044u;
                    break;
                case 0x1C000007: // Ancient Cadie
                    counter_typeid = 0x6C40001Au;
                    break;
                case 0x1C000010: // Pippin
                    counter_typeid = 0x6C40001Cu;
                    break;
                case 0x1C000011: // Tittan Boo
                    counter_typeid = 0x6C40001Bu;
                    break;
                case 0x1C000012: // Dolfini
                    counter_typeid = 0x6C400042u;
                    break;
                case 0x1C000013: // Lolo
                    counter_typeid = 0x6C400043u;
                    break;
                case 0x1C000014: // Quma
                    counter_typeid = 0x6C400041u;
                    break;
                case 0x1C000015: // Tiki
                    counter_typeid = 0x6C400044u;
                    break;
                case 0x1C000016: // Cadie
                    counter_typeid = 0x6C40001Au;
                    break;
                case 0x1C00001E: // Golden Papel 18k
                    counter_typeid = 0x6C400045u;
                    break;
            }

            return (counter_typeid);
        }

        // get Typeid do Counter Item do Mascot TypeId
        public static uint getMascotCounterTypeId(uint _mascot_typeid)
        {

            uint counter_typeid = 0u;

            switch (_mascot_typeid)
            {
                case 0x40000000: // Lemmy
                    counter_typeid = 0x6C400049u;
                    break;
                case 0x40000001: // Puff
                    counter_typeid = 0x6C400048u;
                    break;
                case 0x40000002: // Cocoa
                    counter_typeid = 0x6C400047u;
                    break;
                case 0x40000003: // Billy
                    counter_typeid = 0x6C400046u;
                    break;
            }

            return (counter_typeid);
        }

        // get Typeid do Counter Item do Course TypeId
        public static uint getCourseCounterTypeId(uint _course_typeid)
        {

            uint counter_typeid = 0u;

            switch (_course_typeid)
            {
                case 0: // Blue Lagoon
                    counter_typeid = 0x6C400020u;
                    break;
                case 1: // Blue Water
                    counter_typeid = 0x6C400021u;
                    break;
                case 2: // Sepia Wind
                    counter_typeid = 0x6C400022u;
                    break;
                case 3: // Wind Hill
                    counter_typeid = 0x6C400023u;
                    break;
                case 4: // Wiz Wiz
                    counter_typeid = 0x6C400024u;
                    break;
                case 5: // West Wiz
                    counter_typeid = 0x6C400025u;
                    break;
                case 6: // Blue Moon
                    counter_typeid = 0x6C400026u;
                    break;
                case 7: // Silvia Cannon
                    counter_typeid = 0x6C400027u;
                    break;
                case 8: // Ice Cannon
                    counter_typeid = 0x6C400028u;
                    break;
                case 9: // White Wiz
                    counter_typeid = 0x6C400029u;
                    break;
                case 10: // Shining Sand
                    counter_typeid = 0x6C40002Au;
                    break;
                case 11: // Pink Wind
                    counter_typeid = 0x6C40002Bu;
                    break;
                case 13: // Deep Inferno
                    counter_typeid = 0x6C40002Cu;
                    break;
                case 14: // Ice Spa
                    counter_typeid = 0x6C40002Du;
                    break;
                case 15: // Lost Seaway
                    counter_typeid = 0x6C40002Eu;
                    break;
                case 16: // Eastern Vally
                    counter_typeid = 0x6C40002Fu;
                    break;
                case 17: // SSC
                    counter_typeid = 0x6C40006Du;
                    break;
                case 18: // Ice Inferno
                    counter_typeid = 0x6C400031u;
                    break;
                case 19: // Wiz City
                    counter_typeid = 0x6C400030u;
                    break;
                case 20: // Abbot Mine
                    counter_typeid = 0x6C4000A1u;
                    break;
                case 21: // Mystic Ruins
                    counter_typeid = 0x6C4000C5u;
                    break;
            }

            return (counter_typeid);
        }

        // get Typeid do Counter Item do Número de Holes do Jogo
        public static uint getQntdHoleCounterTypeId(uint _qntd_hole)
        {

            uint counter_typeid = 0u;

            switch (_qntd_hole)
            {
                case 3:
                    counter_typeid = 0x6C400069u;
                    break;
                case 6:
                    counter_typeid = 0x6C40006Au;
                    break;
                case 9:
                    counter_typeid = 0x6C40006Bu;
                    break;
                case 18:
                    counter_typeid = 0x6C40006Cu;
                    break;
            }

            return (counter_typeid);
        }

        // get Typeid do Counter Item do Score do Hole
        public static uint getScoreCounterTypeId(int _tacada_num, int _par_hole)
        {

            uint counter_typeid = 0u;

            if (_tacada_num == 1) // HIO
            {
                counter_typeid = 0x6C400006u;
            }
            else
            {
                switch ((int)(_tacada_num - _par_hole))
                {
                    case (int)-3: // Alba
                        counter_typeid = 0x6C400007u;
                        break;
                    case (int)-2: // Eagle
                        counter_typeid = 0x6C400008u;
                        break;
                    case (int)-1: // Birdie
                        counter_typeid = 0x6C400009u;
                        break;
                    case 0: // Par
                        counter_typeid = 0x6C40000Au;
                        break;
                }
            }

            return (counter_typeid);
        }

        // get Typeid do Counter Item do Active Item TypeId
        public static uint getActiveItemCounterTypeId(uint _active_item_typeid)
        {

            uint counter_typeid = 0u;

            switch (_active_item_typeid)
            {
                case 0x18000000: // Spin Mastery
                    counter_typeid = 0x6C400072u;
                    break;
                case 0x18000001: // Curve Mastery
                    counter_typeid = 0x6C400096u;
                    break;
                case 0x18000004: // Strength Boost
                    counter_typeid = 0x6C400070u;
                    break;
                case 0x18000005: // Miracle Sign
                    counter_typeid = 0x6C400074u;
                    break;
                case 0x18000006: // Silent Wind
                    counter_typeid = 0x6C400073u;
                    break;
                case 0x18000009: // Power Calipers
                    counter_typeid = 0x6C4000BDu;
                    break;
                case 0x18000010: // Duostar PS
                    counter_typeid = 0x6C400094u;
                    break;
                case 0x18000011: // Duostar SS
                    counter_typeid = 0x6C400093u;
                    break;
                case 0x18000012: // Duostar LS
                    counter_typeid = 0x6C400092u;
                    break;
                case 0x18000025: // Power Milk
                    counter_typeid = 0x6C400071u;
                    break;
                case 0x18000027: // Power Potion
                    counter_typeid = 0x6C400091u;
                    break;
                case 0x18000028: // Safety
                    counter_typeid = 0x6C400095u;
                    break;
                case 0x1800002C: // Silent Wind Nerve Stabilizer
                    counter_typeid = 0x6C40008Eu;
                    break;
                case 0x1800002D: // Safety Silent Wind
                    counter_typeid = 0x6C40008Fu;
                    break;
                case 0x1800002F: // Wind Strength Boost
                    counter_typeid = 0x6C400090u;
                    break;
            }

            return (counter_typeid);
        }

        // get Typeid do Counter Item do Passive Item TypeId
        public static uint getPassiveItemCounterTypeId(uint _passive_item_typeid)
        {

            uint counter_typeid = 0u;

            switch (_passive_item_typeid)
            {
                case 0x1A000011: // Time Booster
                    counter_typeid = 0x6C400050u;
                    break;
                case 0x1A000040: // Auto Calipers
                    counter_typeid = 0x6C400076u;
                    break;
                case 0x1A000136: // Fairy's Tears
                    counter_typeid = 0x6C400097u;
                    break;
            }

            return (counter_typeid);
        }

        // get score num 0 HIO, 1 Alba, 2 Eagle, 3 Birdie, 4 Par, 5 Bogey, 6 Double Bogey
        public static int getScoreNum(int _tacada_num, int _par_hole)
        {

            int score_num = 0; // HIO

            if (_tacada_num != 1)
            {
                switch ((int)(_tacada_num - _par_hole))
                {
                    case (int)-3: // Alba
                        score_num = 1;
                        break;
                    case (int)-2: // Eagle
                        score_num = 2;
                        break;
                    case (int)-1: // Birdie
                        score_num = 3;
                        break;
                    case 0: // Par
                        score_num = 4;
                        break;
                    case 1: // Bogey
                        score_num = 5;
                        break;
                    case 2: // Double Bogey
                        score_num = 6;
                        break;
                    default: // esse � um score que n�o conta
                        score_num = -1;
                        break;
                }
            }

            return (score_num);
        }

        protected List<Counter> getCounterChanged()
        {
            return v_counters.Values.Where(el => el.value != 0).ToList(); 
        }


        protected List<AchievementInfoEx> getAchievementChanged(Player _session, List<Counter> _v_counters)
        {

            if (!_session.isConnected())
            {
                throw new exception("[AchievementSystem::" + "getAcheievementChanged" + "][Error] _session don't connected", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.SYS_ACHIEVEMENT,
                    1, 0));
            }

            List<AchievementInfoEx> v_ai = new List<AchievementInfoEx>();

            CounterItemInfo cii = null;

            _v_counters.ForEach((element) =>
            {
                foreach (var el in _session.m_pi.mgr_achievement.getAchievementInfo())
                {
                    if (el.Value.status == (uint)ACHIEVEMENT_STATUS.ACTIVED && (cii = el.Value.findCounterItemByTypeId(element._typeid)) != null)
                    {
                        if (checkAchievement(_session, el.Value, element, _v_counters))
                        {
                            v_ai.Add(el.Value);
                        }
                    }
                }
            });

            // Verifica se teve Daily Quest Clear, para poder somar o seu Counter Item
            Counter count = new Counter(0x6C400039u/*Clear Daily Quest*/, 0);

            foreach (var el in v_quest_clear)
            {
                // Verifica se � Quest Conclu�da
                if (sIff.getInstance().getItemGroupIdentify(el.achievement_typeid) == PangyaAPI.IFF.JP.Models.Flags.IFF_GROUP.QUEST_ITEM)
                {
                    count.value++;
                }
            }

            if (count.value > 0)
            {

                // Add o Count do Clear Daily Quest, ao vector de contadores modificados
                _v_counters.Add(count);


                var achievementInfo = _session.m_pi.mgr_achievement.getAchievementInfo();

                var it =  achievementInfo.FirstOrDefault(el =>
        el.Value.status == (uint)ACHIEVEMENT_STATUS.ACTIVED &&
        el.Value.findCounterItemByTypeId(count._typeid) != null
    );

                if (it.Value != null)
                {

                    if (checkAchievement(_session, it.Value, count, _v_counters))
                    {
                        v_ai.Add(it.Value);
                    }
                }
            }
            // Fim de check Clear Daily Quest

            return v_ai;
        }

        protected byte checkQuestClear(Player _session, QuestStuffInfo _qsi, CounterItemInfo _cii, List<Counter> _v_c)
        {
            // C# usa propriedades ou métodos explicitamente
            if (!_session.isConnected())
            {
                throw new Exception("[AchievementSystem::checkQuestClear][Error] _session not connected");
            }

            if (_qsi.id <= 0 || _qsi._typeid == 0)
            {
                _smp.message_pool.getInstance().push(new message("[AchievementSystem::checkQuestClear][Error] QuestStuffinfo _qsi is invalid", type_msg.CL_FILE_LOG_AND_CONSOLE));
                return (byte)eSTATE.NOT_MATCH;
            }

            if (_v_c == null || _v_c.Count == 0)
                return (byte)eSTATE.NOT_MATCH;

            byte ret = (byte)eSTATE.NOT_MATCH;

            var qs = sIff.getInstance().findQuestStuff(_qsi._typeid);
            if (qs != null)
            {
                for (int i = 0; i < qs.counter_item._typeid.Length; ++i)
                {
                    uint currentTargetId = qs.counter_item._typeid[i];
                    if (currentTargetId == 0) continue;

                    // Busca o contador correspondente na lista
                    var counter = _v_c.Find(x => x._typeid == currentTargetId);
                    if (counter == null)
                        return (byte)eSTATE.NOT_MATCH;

                    int targetQntd = qs.counter_item.qntd[i];

                    if (_cii._typeid == currentTargetId)
                    {
                        // Calcula o valor total (Tentativa)
                        // Se não está no mapa de mudanças, soma o valor atual do contador
                        int currentVal = !map_cii_change.ContainsKey(_cii.id) ? counter.value : 0;
                        int totalValue = _cii.value + currentVal;

                        // Lógica CheckBetter: Se objetivo < 0 (decrescente/pior), senão (crescente/melhor)
                        bool isBetter = (targetQntd < 0) ? (targetQntd >= totalValue) : (targetQntd <= totalValue);

                        if (isBetter)
                        {
                            ret |= (byte)eSTATE.CLEAR;
                        }
                        else if (targetQntd == 1)
                        {
                            return (byte)eSTATE.NOT_MATCH; // Não cumulativa
                        }
                        else
                        {
                            ret |= (byte)eSTATE.INCREMENT_COUNTER;
                        }
                    }
                    else
                    {
                        // Verifica contadores secundários
                        bool isSecondaryBetter = (targetQntd < 0) ? (targetQntd >= counter.value) : (targetQntd <= counter.value);
                        if (!isSecondaryBetter)
                            return (byte)eSTATE.NOT_MATCH;
                    }
                }
            }
            else
            {
                _smp.message_pool.getInstance().push(new message($"[AchievementSystem::checkQuestClear][Error] Quest stuff [TYPEID={_qsi._typeid}] nao encontrado.", type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            return ret;
        }

        protected bool checkAchievement(Player _session, AchievementInfoEx _ai, Counter _c, List<Counter> _v_counters)
        {
            CounterItemInfo pcii = null;
            CounterItemInfo pcii_new = null;
            byte ret = (byte)eSTATE.NOT_MATCH;
            bool necessary_update = false;

            foreach (var el in _ai.v_qsi)
            {
                // Validação inicial
                if (el.clear_date_unix <= 0 && el.counter_item_id != 0 && (pcii = _ai.findCounterItemById(el.counter_item_id)) != null)
                {
                    ret = checkQuestClear(_session, el, pcii, _v_counters);

                    if (ret == (byte)eSTATE.CLEAR)
                    {
                        necessary_update = true;

                        // Macro convertida para método
                        CHECK_CHANGE_COUNTER_AND_INCREMENT_AND_SAVE(pcii, _c);

                        var questBase = _ai.getQuestBase();

                        if (questBase != null)
                        {
                            // Se usa o counter da quest base, desvincula criando um novo
                            if (el._typeid != questBase._typeid && el.counter_item_id == questBase.counter_item_id)
                            {
                                var cii = new CounterItemInfo
                                {
                                    active = 1,
                                    _typeid = pcii._typeid,
                                    value = pcii.value
                                };

                                // Assume-se que este método faz o insert e retorna o ID
                                cii.id = UpdateACounterItemInfo(_session, pcii._typeid, cii.value);

                                if (cii.id != -1)
                                {
                                    el.counter_item_id = cii.id;

                                    // Adiciona ao mapa. Use TryAdd para evitar erro de chave duplicada
                                    if (!_ai.map_counter_item.TryAdd(cii.id, cii))
                                    {
                                        throw new exception($"[Error] Falha ao inserir ID {cii.id} no mapa.",
                                            ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.SYS_ACHIEVEMENT, 13, 0));
                                    }

                                    pcii_new = _ai.map_counter_item[cii.id];

                                    // Registra a mudança
                                    map_cii_change[pcii_new.id] = new CounterItemCtx(pcii_new, (int)(pcii_new.value - pcii.value), (int)pcii.value);
                                }
                            }
                        }

                        // --- Processamento de Recompensas ---
                        var qs = sIff.getInstance().findQuestStuff(el._typeid);
                        if (qs == null)
                        {
                            throw new exception($"[Error] IFF Quest {el._typeid} nao encontrado.",
                                ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.SYS_ACHIEVEMENT, 14, 0));
                        }

                        for (int i = 0; i < qs.reward_item._typeid.Length; ++i)
                        {
                            uint rId = qs.reward_item._typeid[i];
                            if (rId != 0)
                            {
                                if (rId == 0x6C000001) // Pontos de Achievement
                                    _session.m_pi.mgr_achievement.incrementPoint(qs.reward_item.qntd[i]);
                                else
                                    v_reward.Add(new Reward(rId, (int)qs.reward_item.qntd[i], (int)qs.reward_item.time[i]));
                            }
                        }

                        // Finalização da Quest
                        el.clear_date_unix = (uint)UtilTime.GetLocalTimeAsUnix();
                        snmdb.NormalManagerDB.getInstance().add(1, new CmdUpdateQuestUser(_session.m_pi.uid, el), SQLDBResponse, this);

                        v_quest_clear.Add(new QuestClear(_ai._typeid, el._typeid));

                        // Verifica se concluiu o Achievement inteiro
                        if (_ai.checkAllQuestClear())
                        {
                            _ai.status = (int)ACHIEVEMENT_STATUS.CONCLUEDED;
                            snmdb.NormalManagerDB.getInstance().add(2, new CmdUpdateAchievementUser(_session.m_pi.uid, _ai), SQLDBResponse, this);
                        }
                    }
                    // CORREÇÃO AQUI: (ret & flag) != 0
                    else if ((ret & (byte)eSTATE.INCREMENT_COUNTER) != 0 &&
                            (sIff.getInstance().getItemGroupIdentify(_ai._typeid) == IFF_GROUP.QUEST_ITEM || _ai.quest_base_typeid != 0))
                    {
                        CHECK_CHANGE_COUNTER_AND_INCREMENT_AND_SAVE(pcii, _c);
                        necessary_update = true;
                    }
                }
            }

            // CORREÇÃO AQUI: Count > 0 em vez de !empty()
            return necessary_update && map_cii_change.Count > 0;
        }
        private int UpdateACounterItemInfo(Player _session, uint _typeid, int _value)
        {

            CmdAddCounterItem cmd_aci = new CmdAddCounterItem(_session.m_pi.uid, _typeid, _value);

            snmdb.NormalManagerDB.getInstance().add(0, cmd_aci, null, null);

            if (cmd_aci.getException().getCodeError() != 0)
            {
                throw new exception("[AchievementSystem::finish_and_update][Error]", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.SYS_ACHIEVEMENT,
                    12, 0));
            }


            return cmd_aci.getId();
        }

        private static void SQLDBResponse(int _msg_id,
                Pangya_DB _pangya_db,
                object _arg)
        {

            if (_arg == null)
            {
                return;
            }

            // Por Hora s� sai, depois fa�o outro tipo de tratamento se precisar
            if (_pangya_db.getException().getCodeError() != 0)
            {
                _smp.message_pool.getInstance().push(new message("[AchievementSystem::SQLDBResponse][Error] " + _pangya_db.getException().getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                return;
            }
             
            switch (_msg_id)
            {  
                default: // 25 � update item equipado slot
                    break;
            }

        }
    }
}