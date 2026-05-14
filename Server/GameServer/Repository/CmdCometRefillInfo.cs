using System.Collections.Generic;
using System.Linq;
using Pangya_GameServer.Models;
using PangyaAPI.SQL;

namespace Pangya_GameServer.Repository
{
    public class CmdCometRefillInfo : Pangya_DB
    {
        public CmdCometRefillInfo()
        {
            this.m_ctx_cr = new Dictionary<uint, ctx_comet_refill>();
        }

        public virtual void Dispose()
        {
        }

        public Dictionary<uint, ctx_comet_refill> getInfo()
        {
            return new Dictionary<uint, ctx_comet_refill>(m_ctx_cr);
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {

            checkColumnNumber(3);

            ctx_comet_refill ctx_cr = new ctx_comet_refill
            {
                _typeid = IFNULL(_result.data[0])
            };
            ctx_cr.qntd_range.min = (ushort)IFNULL(_result.data[1]);
            ctx_cr.qntd_range.max = (ushort)IFNULL(_result.data[2]);

            var it = m_ctx_cr.Any(c => c.Key == ctx_cr._typeid);

            if (!it) // N�o tem no map, add ao map
            {
                m_ctx_cr.Add(ctx_cr._typeid, ctx_cr);
            }
        }

        protected override Response prepareConsulta()
        {

            var r = procedure(m_szConsulta, "");

            checkResponse(r, "nao conseguiu pegar o comet refill info");

            return r;
        }

        private Dictionary<uint, ctx_comet_refill> m_ctx_cr = new Dictionary<uint, ctx_comet_refill>();

        private const string m_szConsulta = "pangya.ProcGetCometRefillInfo";
    }
}