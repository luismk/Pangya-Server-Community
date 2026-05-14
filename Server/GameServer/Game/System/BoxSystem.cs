using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Pangya_GameServer.Repository;
using Pangya_GameServer.Models;
using Pangya_GameServer.UTIL;
using PangyaAPI.IFF.JP.Extensions;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;

namespace Pangya_GameServer.Game.System
{
    public class BoxSystem
    {
        public BoxSystem()
        {
            this.m_load = false;
            this.m_box = new Dictionary<uint, ctx_box>();

            // Inicializa
            initialize();
        }

        ~BoxSystem()
        {
            clear();
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
            return m_load && !m_box.empty();
        }

        /*static*/
        public ctx_box findBox(uint _typeid)
        {

            if (!isLoad())
            {
                throw new exception("[BoxSystem::findBox][Error] Box System not loadded, please call load method first.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.BOX_SYSTEM,
                    2, 0));
            }

            var it = m_box.FirstOrDefault(el =>
            {
                return el.Value._typeid == _typeid;
            });

            if (it.Key != 0)
            {
                return it.Value;
            }
            return null;
        }

        /*static*/
        public ctx_box_item drawBox(Player _session, ctx_box _ctx_b)
        { 
		if (!_session.getState()
                        || !_session.isConnected()
                        || _session.isQuit())
                    {
                        throw new exception("[BoxSystem::" + (("drawBox")) + "][Error] session is not connected", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.BOX_SYSTEM,
                            1, 0));
                    }
                if (!isLoad())
                {
                    throw new exception("[BoxSystem::" + "drawBox" + "][Error] Box System not loadded, please call load method first.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.BOX_SYSTEM,
                        2, 0));
                }
                if (_ctx_b._typeid == 0)
                {
                    throw new exception("[BoxSystem::" + "drawBox" + "][Error] box _typeid is invalid(zero)", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.BOX_SYSTEM,
                        3, 0));
                }
                if (_ctx_b.item.Count == 0)
                {
                    throw new exception("[BoxSystem::" + "drawBox" + "][Error] box is empty.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.BOX_SYSTEM,
                        4, 0));
                }

            ctx_box_item bi = null;




            Lottery lottery = new Lottery();

            foreach (var el in _ctx_b.item)
            {

                if (el.active == 1)
                {

                    // Verifica qual o tipo da box, se for 100% raro, ent�o s� coloca os raros se j� pegou todos os raros colocar os Lucky reward
                    if (_ctx_b.tipo != BOX_TYPE.ALL_RARE_OR_LUCKY_REWARD || el.raridade != (BOX_TYPE_RARETY)BOX_TYPE_RARETY.R_NORMAL)
                    {

                        // S� pode add os itens que o player n�o tem ou pode ter duplicada
                        if (!(!(el.duplicar == 1) && (!sIff.getInstance().IsCanOverlapped(el._typeid) || sIff.getInstance().getItemGroupIdentify(el._typeid) == PangyaAPI.IFF.JP.Models.Flags.IFF_GROUP.CAD_ITEM) && _session.m_pi.ownerItem(el._typeid)))
                        {
                            lottery.Push(el.probabilidade, el);
                        }

                    }

                }
            }

            // Player j� tem todos os raros da o Lucky reward para ele
            if (_ctx_b.tipo == BOX_TYPE.ALL_RARE_OR_LUCKY_REWARD && lottery.getCountItem() == 0)
            {

                foreach (var el in _ctx_b.item)
                {

                    // Add na roleta os itens normais para o player ganhar que ele j� ganhou todos os raros
                    if (el.active == 1 && el.raridade == (BOX_TYPE_RARETY)BOX_TYPE_RARETY.R_NORMAL)
                    {

                        // S� pode add os itens que o player n�o tem ou pode ter duplicada
                        if (!(!el.duplicar.IsTrue() && (!sIff.getInstance().IsCanOverlapped(el._typeid) || sIff.getInstance().getItemGroupIdentify(el._typeid) == PangyaAPI.IFF.JP.Models.Flags.IFF_GROUP.CAD_ITEM) && _session.m_pi.ownerItem(el._typeid)))
                        {
                            lottery.Push(el.probabilidade, el);
                        }

                    }
                }
            }

            // Verifica se o tem algum item para o player ganhar raro ou Lucky reward
            if (lottery.getCountItem() == 0)
            {
                throw new exception("[BoxSystem::drawBox][Error] player ja tem todos os itens e a Box[Typeid=" + Convert.ToString(_ctx_b._typeid) + ", ID=" + Convert.ToString(_ctx_b.id) + "] nao tem o Lucky reward ou ele nao pode ter duplicata.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.BOX_SYSTEM,
                    8, 0));
            }

            Lottery.LotteryCtx lc = null;
            uint count = 1; // 1 Item

            do
            {

                {
                    if (!_session.getState()
                        || !_session.isConnected()
                        || _session.isQuit())
                    {
                        throw new exception("[BoxSystem::" + "drawBox" + "][Error] session is not connected", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.BOX_SYSTEM,
                            1, 0));
                    }
                };

                lc = lottery.spinRoleta(); // Remove o item sorteado para n�o sortear ele novamente

                if (lc == null)
                {
                    throw new exception("[BoxSystem::drawBox][Error] nao conseguiu sortear um item, erro na hora de rodar a roleta", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.BOX_SYSTEM,
                        6, 0));
                }

                // Tempor�rio Box Item
                bi = (ctx_box_item)lc.Value;

                // Decrementa o count, que 1 item voi sorteado
                --count;

            } while (count > 0);

            return bi;
        }

        /*static*/
        protected void initialize()
        {

            // Carrega as box do banco de dados
            CmdBoxInfo cmd_bi = new CmdBoxInfo(); // Waiter

            snmdb.NormalManagerDB.getInstance().add(0,
                cmd_bi, null,
                null);

            if (cmd_bi.getException().getCodeError() != 0)
            {
                throw cmd_bi.getException();
            }

            m_box = cmd_bi.getInfo();

            if (m_box.Count == 0)
                _smp.message_pool.getInstance().push(new message("[BoxSystem::initialize][Warning] Not Loaded!", type_msg.CL_FILE_LOG_AND_CONSOLE));


            // Carregou com sucesso
            m_load = true;
        }

        /*static*/
        protected void clear()
        {

            //Monitor.Exit(m_cs);

            if (!m_box.empty())
            {
                m_box.Clear();
            }

            m_load = false;

            //Monitor.Exit(m_cs);
        }

        /*static*/
        private Dictionary<uint, ctx_box> m_box; // Todas as box do server que tem no DB

        /*static*/
        private bool m_load;

        private object m_cs = new object();
    }

    public class sBoxSystem : Singleton<BoxSystem>
    { }
}
