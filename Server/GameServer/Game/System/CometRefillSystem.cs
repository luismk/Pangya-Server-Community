using System;
using System.Collections.Generic;
using Pangya_GameServer.Repository;
using Pangya_GameServer.Models;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;

namespace Pangya_GameServer.Game.System
{
    public class CometRefillSystem : IDisposable
    {

        private static Dictionary<uint, ctx_comet_refill> m_comet_refill = new Dictionary<uint, ctx_comet_refill>();
        private static bool m_load = false;

        private static readonly object m_cs = new object();

        public CometRefillSystem()
        {
            m_load = false;
            initialize();
        }

        public void Dispose()
        {
            clear();
        }

        public void load()
        {
            if (isLoad())
                clear();
            initialize();
        }

        public bool isLoad()
        {
            lock (m_cs)
            {
                return m_load && m_comet_refill.Count > 0;
            }
        }

        public ctx_comet_refill findCometRefill(uint _typeid)
        {
            if (!isLoad())
                throw new exception("[CometRefillSystem::findCometRefill][Error] nao esta carregado...", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.COMET_REFILL_SYSTEM, 3, 0));

            lock (m_cs)
            {
                foreach (var kv in m_comet_refill)
                {
                    if (kv.Value._typeid == _typeid)
                        return kv.Value;
                }
            }

            throw new exception("[CometRefillSystem::findCometRefill][Error] Tipo não encontrado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.COMET_REFILL_SYSTEM, 3, 1));
        }

        public uint drawsCometRefill(ctx_comet_refill _ctx_cr)
        {
            if (_ctx_cr._typeid == 0)
                throw new exception("[CometRefillSystem][Error] ctx_comet_refill TYPEID é zero", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.COMET_REFILL_SYSTEM, 1, 0));

            if (!_ctx_cr.qntd_range.isValid())
                throw new exception("[CometRefillSystem][Error] Range inválido", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.COMET_REFILL_SYSTEM, 2, 0));

            return (uint)(_ctx_cr.qntd_range.min + Convert.ToInt64((new Random().Next() % (uint)(_ctx_cr.qntd_range.max - _ctx_cr.qntd_range.min + 1))));
        }

        protected void initialize()
        {
            try
            {
                CmdCometRefillInfo cmd_cri = new CmdCometRefillInfo();
                snmdb.NormalManagerDB.getInstance().add(0, cmd_cri, null, null);

                if (cmd_cri.getException().getCodeError() != 0)
                    throw cmd_cri.getException();

                m_comet_refill = new Dictionary<uint, ctx_comet_refill>(cmd_cri.getInfo());
                m_load = true;

                if (m_comet_refill.Count == 0)
                    _smp.message_pool.getInstance().push(new message("[CometRefillSystem::initialize][Warning] Not Loaded!", type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
            catch
            {
                throw;
            }
        }

        protected void clear()
        {
            lock (m_cs)
            {
                m_comet_refill.Clear();
                m_load = false;
            }
        }
    }

    public class sCometRefillSystem : Singleton<CometRefillSystem>
    {
    }
}
