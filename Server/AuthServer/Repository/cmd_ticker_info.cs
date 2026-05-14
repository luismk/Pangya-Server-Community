using System;
using System.Text;
using Pangya_AuthServer.Models;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;
namespace Pangya_AuthServer.Repository
{
    public class CmdTickerInfo : Pangya_DB
    {
        public CmdTickerInfo(bool _waiter = false) : base(_waiter)
        {
            this.m_id = 0;
            this.m_ti = new TickerInfo(0);
        }

        public CmdTickerInfo(uint _id, bool _waiter = false) : base(_waiter)
        {
            this.m_id = _id;
            this.m_ti = new TickerInfo(0);
        }

        public virtual void Dispose()
        {
        }

        public uint getId()
        {
            return (m_id);
        }

        public void setId(uint _id)
        {
            m_id = _id;
        }

        public TickerInfo getInfo()
        {
            return m_ti;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {
            checkColumnNumber(2);
            if (is_valid_c_string(_result.data[0]))
            {
                m_ti.msg = (_result.GetString(0));
            }

            if (is_valid_c_string(_result.data[1]))
            {
                m_ti.nick = (_result.GetString(1));
            }
        }

        protected override Response prepareConsulta()
        {

            if (m_id == 0 || m_id <= 0)
            {
                throw new exception("[CmdTickerInfo::prepareConsulta][Error] m_id is invalid(zero).", STDA_MAKE_ERROR(PangyaAPI.Utilities.STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            m_ti.clear();

            var r = procedure(
                m_szConsulta,
                Convert.ToString(m_id));

            checkResponse(r, "nao conseguiu pegar Ticker Info do Server[COMMAND_ID=" + Convert.ToString(m_id) + "]");

            return r;
        }



        private uint m_id = 0;
        private TickerInfo m_ti = new TickerInfo();

        private string m_szConsulta = "pangya.ProcGetTicker";
    }
}
