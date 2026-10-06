using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Pangya_GameServer.Repository;
using Pangya_GameServer.Models;
using Pangya_GameServer.Engine;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using Pangya_GameServer.Feature;
using PangyaAPI.IFF.Handle.JP;
using PangyaAPI.DataBase;
using static Pangya_GameServer.Models.DefineConstants;
using PangyaAPI.IFF.Regions.JP.Models.IFF;
using Pangya_GameServer.Session;
using snmdb;
namespace Pangya_GameServer.Feature
{
    public class DropSystem
    {
        #region Classes
        public class stDropCourse
        {
            public stDropCourse(uint _ul = 0u)
            {
                clear();
            }
            public partial class stDropItem
            {
                public enum eTIPO : byte
                {
                    ALL_PROBABILITY, // Todos holes pode dropar, tem chance
                    SEQUENCE_DROP, // Dropa em uma quantidade fixe de 2 em 2 holes 1 em 1 hole, pode ser de 1 a 18
                    LAST_HOLE_PROBABILITY // Ultimo hole tem a chance de dropar
                }
                public enum ePROB_TIPO : byte
                {
                    _3HOLES_ALL, // 3 Holes ou todos os holes, para outros tipos de drop
                    _6HOLES_SEQUENCE, // 6 Holes ou a sequência que o item pode dropar
                    _9HOLES, // 9 Holes
                    _18HOLES // 18 Holes
                }
                public void clear()
                {
                }
                public uint _typeid = new uint();
                public byte tipo;
                public uint qntd = new uint();
                public uint[] probabilidade = new uint[4]; // 3H_ALL, 6H, 9H, 18H, probabilidade
                public byte active = 1;
            }

            public void clear()
            { 
                course = 0;

                if (v_item.Any())
                {
                    v_item.Clear();
                }
            }
            public byte course;
            public List<stDropItem> v_item = new List<stDropItem>();
        }

        public class stCourseInfo
        {
            public void clear()
            {
            }
            public byte course;
            public byte hole; // Número do hole em relação do CourseIndex
            public byte seq_hole; // Sequência do hole de 1 a 18
            public byte qntd_hole; // Quantidade de holes do jogo
            public uint artefact = new uint();
            public byte char_motion = 1;
            public byte angel_wings = 2; // 1 2 3 YES, 0 NO
            public uint rate_drop = new uint();
        }

        public class stConfig
        {
            public void clear()
            {
            }
            public uint rate_mana_artefact = new uint();
            public uint rate_grand_prix_ticket = new uint();
            public uint rate_SSC_ticket = new uint();
        }
        #endregion

        public DropSystem()
        {
            this.m_load = false;
            this.m_config = new stConfig();
            this.Course = new Dictionary<byte, stDropCourse>();
        }

        /*static*/
        public void load()
        {

            if (isLoad())
            {
                clear();
            }

            initialize();
        }

        /*static*/
        public bool isLoad()
        {

            bool isLoad = false;

            //Monitor.Exit(m_cs);

            isLoad = m_load;

            //Monitor.Exit(m_cs);
            return isLoad;
        }

        /*static*/
        public DropItem drawArtefactPang(stCourseInfo _ci, uint _num_players)
        {

            DropItem di = new DropItem();


            uint pang = 0;

            switch (_ci.artefact)
            {
                case ART_WICKED_BROOMSTICK:
                    pang = 1;
                    break;
                case ART_TEORITE_ORE:
                    pang = 2;
                    break;
                case ART_REDNOSE_WIZBERRY:
                    pang = 3;
                    break;
                case ART_MAGANI_FLOWER:
                    pang = 6;
                    break;
                case ART_ROGER_K_STEERING_WHEEL:
                    {

                        pang = (uint)(_num_players < 25 ? (Random.Shared.Next() % 6) + 1 : (Random.Shared.Next() % 1002) + 1);

                        break;
                    } // END CASE ART_ROGER_K_STEERING_WHEEL
            } // END SWITCH

            if (pang != 0)
            {

                di._typeid = PANG_POUCH_TYPEID;
                di.course = (byte)(_ci.course & 0x7F);
                di.numero_hole = _ci.hole;

                di.qntd = (short)pang;
                di.type = DropItem.eTYPE.QNTD_MULTIPLE_500;

            }
            return di;
        }

        /*static*/
        public List<DropItem> drawCourse(stDropCourse _dc, stCourseInfo _ci)
        {
            CHECK_DROP("drawCourse", _dc);

            List<DropItem> v_item = new List<DropItem>();

            uint qntd = 1;

            if (_ci.char_motion == 1)
            {
                qntd *= 2;
            }

            if (_ci.artefact == ART_RAINBOW_MAGIC_HAT)
            {
                qntd++;
            }

            LotterySystem lottery = new LotterySystem();
            LotterySystem.LotteryCtx ctx = null;

            stDropCourse.stDropItem pDi = null;
            DropItem di = new DropItem();

            float rate = 1.0f;

            if (_ci.rate_drop > 100)
            {
                rate *= _ci.rate_drop / 100.0f;
            }

            if (_ci.angel_wings == 1)
            {
                rate *= 1.2f; // 20%
            }

            foreach (var el in _dc.v_item)
            {

                if ((el.tipo != (byte)stDropCourse.stDropItem.eTIPO.LAST_HOLE_PROBABILITY || _ci.seq_hole == _ci.qntd_hole) && (el.tipo != (byte)stDropCourse.stDropItem.eTIPO.SEQUENCE_DROP || (_ci.seq_hole % el.probabilidade[(int)stDropCourse.stDropItem.ePROB_TIPO._6HOLES_SEQUENCE]) == 0))
                {

                    lottery = new LotterySystem();

                    switch ((stDropCourse.stDropItem.eTIPO)el.tipo)
                    {
                        case stDropCourse.stDropItem.eTIPO.ALL_PROBABILITY:
                        case stDropCourse.stDropItem.eTIPO.SEQUENCE_DROP:
                        default:
                            // Aqui � item por item que sorteia, cada item tem sua chance no CourseIndex
                            lottery.Add(el.probabilidade[(int)stDropCourse.stDropItem.ePROB_TIPO._3HOLES_ALL], el);
                            break;
                        case stDropCourse.stDropItem.eTIPO.LAST_HOLE_PROBABILITY:
                            {
                                // Holes do jogo
                                if (_ci.qntd_hole == 3)
                                {
                                    lottery.Add(el.probabilidade[(int)stDropCourse.stDropItem.ePROB_TIPO._3HOLES_ALL], el);
                                }
                                else if (_ci.qntd_hole == 6)
                                {
                                    lottery.Add(el.probabilidade[(int)stDropCourse.stDropItem.ePROB_TIPO._6HOLES_SEQUENCE], el);
                                }
                                else if (_ci.qntd_hole == 9)
                                {
                                    lottery.Add(el.probabilidade[(int)stDropCourse.stDropItem.ePROB_TIPO._9HOLES], el);
                                }
                                else // 18 holes
                                {
                                    lottery.Add(el.probabilidade[(int)stDropCourse.stDropItem.ePROB_TIPO._18HOLES], el);
                                }

                                break;
                            }
                    }

                    // S� coloca outro pra sortear se a probabilidade for menor que 1000(100%)
                    if ((lottery.getLimitProbilidade() * rate) < 1000u)
                    {
                        lottery.Add((uint)(1000 - (lottery.getLimitProbilidade() * rate)), 0u);
                    }

                    // Sorteia
                    ctx = lottery.SpinRoleta();

                    if (ctx == null)
                    {
                        throw new exception("[DropSystem::drawCourse][Error] nao conseguiu sortear um drop.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.DROP_SYSTEM,
                            7, 0));
                    }

                    if (ctx.Value != null && ctx.Value is stDropCourse.stDropItem)
                    {

                        pDi = ctx.Value as stDropCourse.stDropItem;

                        di = new DropItem();

                        di._typeid = pDi._typeid;
                        di.qntd = (short)pDi.qntd;

                        di.course = (byte)(_ci.course & 0x7F);
                        di.numero_hole = _ci.hole;

                        di.type = DropItem.eTYPE.NORMAL_QNTD;

                        for (var i = 0; i < qntd; ++i)
                        {
                            v_item.Add(di);
                        }
                    }

                }
            }
            return v_item;
        }

        /*static*/
        public DropItem drawManaArtefact(stCourseInfo _ci)
        {

            DropItem di = new DropItem();

            var item = sIff.Instance.getItem();

            LotterySystem lottery = new LotterySystem();

            item.ForEach(_el =>
            {
                if (_el.ItemType == 4)
                {
                    lottery.Add(200, _el);
                }
            });

            var limit = lottery.getLimitProbilidade();

            if (limit <= 0)
            {
                throw new exception("[DropSystem::drawManaArtefact][Error] nao achou nenhum mana artefact item no IFF_STRUCT. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.DROP_SYSTEM,
                    8, 0));
            }

            float rate = (float)((m_config.rate_mana_artefact > 0) ? m_config.rate_mana_artefact / 100.0f : 1.0f);

            // Drop Item Rate of player and Angel Wing
            if (_ci.rate_drop > 100)
            {
                rate *= _ci.rate_drop / 100.0f;
            }

            if (_ci.angel_wings == 1)
            {
                rate *= 1.2f; // 20%
            }
            // End Drop Item Rate of Player and Angel Wing

            limit = (ulong)(limit / rate);

            lottery.Add((uint)limit, 0);

            var ctx = lottery.SpinRoleta();

            if (ctx == null)
            {
                throw new exception("[DropSystem::drawManaArtefact][Error] nao conseguiu sortear um drop.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.DROP_SYSTEM,
                    7, 0));
            }

            if (ctx.Value != null && ctx.Value is Item)
            {
                var pDi = ctx.Value as Item;

                di._typeid = pDi.ID;
                di.qntd = 1;

                di.course = (byte)(_ci.course & 0x7F);
                di.numero_hole = _ci.hole;

                di.type = DropItem.eTYPE.NORMAL_QNTD;
            }
            return di;
        }

        /*static*/
        public DropItem drawGrandPrixTicket(stCourseInfo _ci, Player _session)
        {
            DropItem di = new DropItem();

            short qntd = 0;

            var pWi = _session.Inventory.FindWarehouseItemByTypeid(GRAND_PRIX_TICKET);

            if (pWi == null || (qntd = pWi.c[0]) < LIMIT_GRAND_PRIX_TICKET)
            {

                if (_ci.qntd_hole == 18 || _ci.qntd_hole == 9)
                { // 100%

                    di._typeid = GRAND_PRIX_TICKET;
                    di.course = (byte)(_ci.course & 0x7F);
                    di.numero_hole = _ci.hole;
                    di.type = DropItem.eTYPE.NORMAL_QNTD;

                    di.qntd = (short)((qntd == 49 || _ci.qntd_hole == 9) ? 1 : 2);

                }
                else
                { // 50%

                    LotterySystem lottery = new LotterySystem();

                    lottery.Add(200, GRAND_PRIX_TICKET);
                    lottery.Add(400, 0);

                    var ctx = lottery.SpinRoleta();

                    if (ctx == null)
                    {
                        throw new exception("[DropSystem::drawGrandPrixTicket][Error] nao conseguiu sortear um drop.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.DROP_SYSTEM,
                            7, 0));
                    }

                    if (ctx.Value != null)
                    {

                        di._typeid = Convert.ToUInt32(ctx.Value);
                        di.qntd = 1;

                        di.course = (byte)(_ci.course & 0x7F);
                        di.numero_hole = _ci.hole;

                        di.type = DropItem.eTYPE.NORMAL_QNTD;
                    }
                }
            }

            return di;
        }

        /*static*/
        public List<DropItem> drawSSCTicket(stCourseInfo _ci)
        {

            List<DropItem> v_item = new List<DropItem>();
            uint qntd = 1;




            if (_ci.char_motion == 1)
            {
                qntd *= 2;
            }

            if (_ci.artefact == ART_RAINBOW_MAGIC_HAT)
            {
                qntd++;
            }

            LotterySystem lottery = new LotterySystem();

            lottery.Add(200, SSC_TICKET);

            var limit = lottery.getLimitProbilidade();

            float rate = (float)((m_config.rate_SSC_ticket > 0) ? m_config.rate_SSC_ticket / 100.0f : 1.0f);

            // Drop Item Rate of player and Angel Wing
            if (_ci.rate_drop > 100)
            {
                rate *= _ci.rate_drop / 100.0f;
            }

            if (_ci.angel_wings == 1)
            {
                rate *= 1.2f; // 20%
            }
            // End Drop Item Rate of Player and Angel Wing

            limit = (ulong)(limit / rate);

            lottery.Add((uint)limit, 0);
            lottery.Add((uint)limit, 0);

            var ctx = lottery.SpinRoleta();

            if (ctx == null)
            {
                throw new exception("[DropSystem::drawSSCTicker][Error] nao conseguiu sortear um drop.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.DROP_SYSTEM,
                    7, 0));
            }

            if (ctx.Value != null)
            {

                DropItem di = new DropItem();

                di._typeid = Convert.ToUInt32(ctx.Value);
                di.qntd = 1;

                di.course = (byte)(_ci.course & 0x7F);
                di.numero_hole = _ci.hole;

                di.type = DropItem.eTYPE.NORMAL_QNTD;

                for (var i = 0; i < qntd; ++i)
                {
                    v_item.Add(di);
                }
            }

            return v_item;
        }

        /*static*/
        public stDropCourse findCourse(byte _course)
        {  
            return Course.FirstOrDefault(c => c.Key == (byte)(_course & 0x7F)).Value;
        }

        /*static*/
        private void initialize()
        {

            CmdDropCourseConfig cmd_dcc = new CmdDropCourseConfig(); // Waiter

            NormalManagerDB.Instance.add(0,
                  cmd_dcc, null,
                  null);

            if (cmd_dcc.getException().getCodeError() != 0)
            {
                throw cmd_dcc.getException();
            }

            m_config = cmd_dcc.getConfig();

            CmdDropCourseInfo cmd_dci = new CmdDropCourseInfo(); // Waiter

            NormalManagerDB.Instance.add(0,
                  cmd_dci, null,
                  null);

            if (cmd_dci.getException().getCodeError() != 0)
            {
                throw cmd_dci.getException();
            }

            Course = cmd_dci.getInfo();

            //#ifdef DEBUG
            if (Course.Count == 0)
                _smp.LogManager.Instance.push(new AppMessage("[DropSystem::initialize][Warning] Not Loaded!", type_msg.CL_FILE_LOG_AND_CONSOLE));

            // Carregado com sucesso
            m_load = true;
        }

        /*static*/
        protected void clear()
        {

            //Monitor.Exit(m_cs);

            if (Course.Count > 0)
            {
                Course.Clear();
            }

            m_load = false;

            //Monitor.Exit(m_cs);
        }

        private void CHECK_DROP(string _method, stDropCourse _dc)
        {
            if (!isLoad())
                throw new exception("[DropSystem::" + ((_method)) + "][Error] Drop System not loadded, please call load method first.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.DROP_SYSTEM, 2, 0));
            if (_dc.course == 0x7F/*Random*/)
                throw new exception("[DropSystem::" + ((_method)) + "][Error] CourseIndex is invalid(0x7F) Random value", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.DROP_SYSTEM, 3, 0));

            if (!_dc.v_item.Any())
                throw new exception("[DropSystem::" + ((_method)) + "][Error] drop item vector is empty.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.DROP_SYSTEM, 4, 0));
        }

        /*static*/
        private Dictionary<byte, stDropCourse> Course = new Dictionary<byte, stDropCourse>();

        /*static*/
        private stConfig m_config = new stConfig();

        /*static*/
        private bool m_load;
        private object m_cs = new object();
    }

    // Implementação do padrão Singleton
    public class sDropSystem : Singleton<DropSystem>
    {
    }
}
