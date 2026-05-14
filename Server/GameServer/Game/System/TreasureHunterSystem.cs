using System;
using System.Collections.Generic;
using System.Threading;
using Pangya_GameServer.Repository;
using Pangya_GameServer.Models;
using Pangya_GameServer.UTIL;
using PangyaAPI.IFF.JP.Extensions;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using static Pangya_GameServer.Models.DefineConstants;
namespace Pangya_GameServer.Game.System
{
    public class TreasureHunterSystem
    {
        public TreasureHunterSystem()
        {
            this.m_thItem = new List<TreasureHunterItem>();
            this.m_load = false;
            this.m_time = DateTime.Now; 
        }

        /*static */
        public void load()
        {

            if (isLoad())
            {
                clear();
            }

            initialize();
        }

        /*static */
        public bool isLoad()
        {

            bool isLoad = false;

            //Monitor.Exit(m_cs);


            isLoad = (m_load && m_thItem.Count > 0);

            //Monitor.Exit(m_cs);


            return isLoad;
        }

        /*static */
        public List<TreasureHunterInfo> getAllCoursePoint()
        {
            var list = new List<TreasureHunterInfo>();
            foreach (var item in m_thi)
            {
                list.Add(item);
            }
            return list;
        }

        /*static */
        public TreasureHunterInfo findCourse(byte _course)
        {

            // Prote��o contra o Random Map, que usa o negatico do 'char'
            if ((_course & 0x7F) >= MS_NUM_MAPS)
            {
                throw new exception("[TreasureHunterSystem::findCourse][Error] _course is invalid[VALUE=" + Convert.ToString((ushort)_course & 0x7F) + "]", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TREASURE_HUNTER_SYSTEM,
                    2, 0));
            }

            TreasureHunterInfo thi = null;

            //Monitor.Exit(m_cs);


            // Prote��o contra o Random Map, que usa o negatico do 'char'
            thi = m_thi[_course & 0x7F];

            //Monitor.Exit(m_cs);


            return thi;
        }

        /*static */
        public uint calcPointNormal(int _tacada, int _par_hole)
        {

            if (_tacada == 1) // Hole In One(HIO)
            {
                return 100;
            }

            uint point = 0;

            switch ((_tacada - _par_hole))
            {
                case -3: // Albatross
                    point = 100;
                    break;
                case -2: // Eagle
                    point = 50;
                    break;
                case -1: // Birdie
                    point = 30;
                    break;
                case 0: // Par
                    point = 15;
                    break;
                case 1: // Bogey
                    point = 10;
                    break;
                case 2: // Double Bogey
                    point = 7; break;
                case 3: // Triple Bogey
                    point = 4; break;
                case 4: // +4
                    point = 1; break;
            }

            return point;
        }

        /*static */
        public uint calcPointSSC(int _tacada, int _par_hole)
        {

            if (_tacada == 1) // Hole In One(HIO)
            {
                return 30;
            }

            uint point = 0;

            switch ((_tacada - _par_hole))
            {
                case -3: // Albatross
                    point = 1;
                    break;
                case -2: // Eagle
                    point = 4;
                    break;
                case -1: // Birdie
                    point = 7;
                    break;
                case 0: // Par
                    point = 10;
                    break;
                case 1: // Bogey
                    point = 15;
                    break;
                case 2: // Double Bogey
                    point = 30;
                    break;
                case 3: // Triple Bogey
                    point = 50;
                    break;
                case 4: // +4
                    point = 100;
                    break;
            }

            return point;
        }

        Random rand = new Random();
        int GetBoxCount(uint point)
        {
            if (point <= 100) return rand.Next(1, 3);       // 1-2
            if (point <= 200) return rand.Next(2, 5);       // 2-4
            if (point <= 300) return rand.Next(3, 6);       // 3-5
            if (point <= 400) return rand.Next(3, 8);       // 3-7
            if (point <= 500) return rand.Next(4, 9);       // 4-8
            if (point <= 600) return rand.Next(4, 11);      // 4-10
            if (point <= 700) return rand.Next(5, 12);      // 5-11
            if (point <= 800) return rand.Next(5, 14);      // 5-13
            if (point <= 900) return rand.Next(6, 15);      // 6-14
            if (point <= 999) return rand.Next(6, 19);      // 6-18
            return rand.Next(12, 25);                        // 12-24 para 1000+
        }

        /*static */
        public List<TreasureHunterItem> drawItem(uint _point, byte _course)
        {

            List<TreasureHunterItem> v_item = new List<TreasureHunterItem>();

            float rate_course = getCourseRate(_course);

            float rate = _point * sgs.gs.getInstance().getInfo().rate.treasure * rate_course / 100.0f;

            int box = GetBoxCount(_point);


            Lottery lottery = new Lottery();

            foreach (var el in m_thItem)
                lottery.Push(el.probabilidade, _value: el);

            TreasureHunterItem thi = new TreasureHunterItem();

            TreasureHunterItem pThi = null;

            Lottery.LotteryCtx ctx = null;


            for (var i = 0; i < box;)
            {
                // Sorteia item
                if ((ctx = lottery.spinRoleta()) == null)
                {
                    _smp.message_pool.getInstance().push(new message("[TreasureHunterSystem::drawItem][Warning] nao conseguiu sortear o item. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                    continue;
                }

                pThi = (TreasureHunterItem)ctx.Value;

                // Verifica se o item existe no IFF_STRUCT do server, para n�o da erro mais tarde
                if (sIff.getInstance().findCommomItem(pThi._typeid) == null)
                {
                    _smp.message_pool.getInstance().push(new message("[TreasureHunterSystem::drawItem][Warning] nao conseguiu encontrar o Item[TYPEID=" + Convert.ToString(pThi._typeid) + "] no IFF_STRUCT do server. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                    continue;
                }

                // Sorteia quantidade
                if (pThi.qntd > 1 && pThi._typeid != PANG_POUCH_TYPEID)
                    pThi.qntd = (uint)(1 + (rand.Next() % pThi.qntd));

                v_item.Add(pThi);

                // Incrementa o index
                i++;
            }

            return v_item;
        }

        /*static */
        public List<TreasureHunterItem> drawApproachBox(uint _num_box, byte _course)
        {

            List<TreasureHunterItem> v_item = new List<TreasureHunterItem>();
            TreasureHunterItem thi = new TreasureHunterItem();
            TreasureHunterItem pThi = null;

            if (_num_box == 0)
            {
                return v_item;
            }



            float rate_course = getCourseRate(_course);

            uint box = (uint)(_num_box * sgs.gs.getInstance().getInfo().rate.treasure * rate_course / 100.0f);

            // _num box � maior que zero, box n�o pode ser 0, tem que ser pelo menos 1
            if (box == 0u)
            {
                box = 1u;
            }

            Lottery lottery = new Lottery();

            foreach (var el in m_thItem)
            {
                lottery.Push((el.probabilidade), el);
            }

            Lottery.LotteryCtx ctx = null;

            for (var i = 0; i < box;)
            {

                // Sorteia item
                if ((ctx = lottery.spinRoleta()) == null)
                {
                    _smp.message_pool.getInstance().push(new message("[TreasureHunterSystem::drawApproachBox][Warning] nao conseguiu sortear o item. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                    continue;
                }

                pThi = (TreasureHunterItem)ctx.Value;

                // Verifica se o item existe no IFF_STRUCT do server, para n�o da erro mais tarde
                if (sIff.getInstance().findCommomItem(pThi._typeid) == null)
                {
                    _smp.message_pool.getInstance().push(new message("[TreasureHunterSystem::drawApprochBox][Warning] nao conseguiu encontrar o Item[TYPEID=" + Convert.ToString(pThi._typeid) + "] no IFF_STRUCT do server. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                    continue;
                }

                // Sorteia quantidade
                if (pThi.qntd > 1 && pThi._typeid != PANG_POUCH_TYPEID)
                {
                    pThi.qntd = (uint)(1 + (new Random().Next() % pThi.qntd));
                }

                v_item.Add(pThi);

                // Incrementa o index
                i++;
            }
            return v_item;
        }

        // Check time update Point Course
        /*static */
        public bool checkUpdateTimePointCourse()
        {
            if ((DateTime.Now - m_time).TotalMinutes >= TREASURE_HUNTER_TIME_UPDATE /*TREASURE_HUNTER_TIME_UPDATE -> 30 * 60*/)
            {
                for (var i = 0; i < MS_NUM_MAPS; i++)
                {
                    if (m_thi[i].point < TREASURE_HUNTER_LIMIT_POINT_COURSE)
                    {
                        updateCoursePoint(m_thi[i], TREASURE_HUNTER_INCREASE_POINT);
                    }
                }

                _smp.message_pool.getInstance().push(new message("[TreasureHunterSystem][Log] Atualizou Pontos dos course.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                m_time = DateTime.Now;

                return true;
            }
            return false;
        }

        /*static */
        public void updateCoursePoint(TreasureHunterInfo _thi, int _point)
        {
            if (_point < 0) // Decrease
            {
                _thi.point = ((_thi.point + _point < 0) ? 0 : _thi.point + _point);
            }
            else // Increase
            {
                _thi.point = ((_thi.point + _point > TREASURE_HUNTER_LIMIT_POINT_COURSE) ? TREASURE_HUNTER_LIMIT_POINT_COURSE : _thi.point + _point);
            }

            snmdb.NormalManagerDB.getInstance().add(1,
                new CmdUpdateTreasureHunterCoursePoint(_thi),
                SQLDBResponse,
                null);
        }

        /*static */
        protected void initialize()
        {
            CmdTreasureHunterInfo cmd_thi = new CmdTreasureHunterInfo(); // Waiter

            snmdb.NormalManagerDB.getInstance().add(0,
                cmd_thi, null, null);

            if (cmd_thi.getException().getCodeError() != 0)
            {
                throw cmd_thi.getException();
            }
             
            var v_thi = cmd_thi.getInfo();

            foreach (TreasureHunterInfo i in v_thi)
            {
                m_thi[i.course] = i;
            }

            // Item Treasure Hunter
            CmdTreasureHunterItem cmd_thItem = new CmdTreasureHunterItem(); // Waiter

            snmdb.NormalManagerDB.getInstance().add(0,
                cmd_thItem, null, null);

            if (cmd_thItem.getException().getCodeError() != 0)
            {
                throw cmd_thItem.getException();
            } 

            m_thItem = new List<TreasureHunterItem>(cmd_thItem.getInfo());

            // Init Time
            m_time = DateTime.Now;
            if (m_thItem.Count == 0)
                _smp.message_pool.getInstance().push(new message("[TreasureHunterSystem::initialize][Warning] Not Loaded!", type_msg.CL_FILE_LOG_AND_CONSOLE));

            // Carregado com sucesso
            m_load = true;
        }

        /*static */
        protected void clear()
        {

            //Monitor.Exit(m_cs);
            // Limpa a lista de itens do Treasure Hunter System
            // n�o faz o shrink_to_fit por que pode preencher ela novamente
            if (m_thItem.Count > 0)
            {
                m_thItem.Clear();
            }

            m_load = false;

            //Monitor.Exit(m_cs);

        }

        /*static */
        protected float getCourseRate(byte _course)
        {

            float rate_course = 0.0f;
            // Prote��o contra o Random Map, que usa o negatico do 'char'
            var course = findCourse((byte)(_course & 0x7F));

            var point = (course != null ? course.point : 0);

            if (point > 0)
            {
                rate_course = Convert.ToSingle(point / TREASURE_HUNTER_LIMIT_POINT_COURSE); // 1000.f;
            }
            return rate_course;
        }

        protected static void SQLDBResponse(int _msg_id,
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
                _smp.message_pool.getInstance().push(new message("[TreasureHunterSystem::SQLDBResponse][Error] " + _pangya_db.getException().getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                return;
            }

            var _channel = Tools.reinterpret_cast<TreasureHunterSystem>(_arg);

            switch (_msg_id)
            {
                case 1: // Update Treasure Hunter Course Point
                    {
                        var cmd_uthcp = Tools.reinterpret_cast<CmdUpdateTreasureHunterCoursePoint>(_pangya_db);
                        break;
                    }
                case 0:
                default:
                    break;
            }

        }

        /*static */
        private TreasureHunterInfo[] m_thi = Tools.InitializeWithDefaultInstances<TreasureHunterInfo>(MS_NUM_MAPS);
        /*static */
        private List<TreasureHunterItem> m_thItem = new List<TreasureHunterItem>(); // Treasure Hunter Item

        /*static */
        private DateTime m_time = new DateTime();

        /*static */
        private bool m_load;

        private object m_cs = new object();
    }
    public class sTreasureHunterSystem : Singleton<TreasureHunterSystem>
    { }

}
