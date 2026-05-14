using PangyaAPI.SQL;
using PangyaAPI.Utilities;
using System;
namespace Pangya_AuthServer.Repository
{
    public class CmdShutdownInfo : Pangya_DB
    {
        public CmdShutdownInfo(bool _waiter = false) : base(_waiter)
        {
            this.m_id = 0;
            this.m_time_sec = 0;
        }

        public CmdShutdownInfo(uint _id, bool _waiter = false) : base(_waiter)
        {
            this.m_id = _id;
            this.m_time_sec = 0;
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

        public int getInfo()
        {
            return (m_time_sec);
        }

        protected override void lineResult(ctx_res _result, uint _result_index)
        {

            checkColumnNumber(1, (uint)_result.cols);

            m_time_sec = IFNULL<int>(_result.data[0]);

        }

        protected override Response prepareConsulta()
        {

            if (m_id == 0)
            {
                throw new exception("[CmdShutdownInfo::prepareConsulta][Error] m_id is invalid(zero).", STDA_MAKE_ERROR(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            var r = procedure(
                m_szConsulta,
                Convert.ToString(m_id));

            checkResponse(r, "nao conseguiu pegar o shutdown server Info[COMMAND_ID=" + Convert.ToString(m_id) + "]");

            return r;
        }

        private uint m_id = 0;
        private int m_time_sec = 0;

        private string m_szConsulta = "pangya.ProcGetShutdownServer";
    }
}
