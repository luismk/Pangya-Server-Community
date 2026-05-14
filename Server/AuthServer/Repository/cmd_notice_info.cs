using PangyaAPI.SQL;
using PangyaAPI.Utilities;
using System;
using System.Text;
namespace Pangya_AuthServer.Repository
{
    public class CmdNoticeInfo : Pangya_DB
    {
        public CmdNoticeInfo(bool _waiter = false) : base(_waiter)
        {
            this.m_id = 0;
            this.m_message = "";
        }

        public CmdNoticeInfo(uint _id, bool _waiter = false) : base(_waiter)
        {
            this.m_id = _id;
            this.m_message = "";
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

        public string getInfo()
        {
            return m_message;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {

            checkColumnNumber(1, (uint)_result.cols);

            if (is_valid_c_string(_result.data[0]))
            {
                var m_message_bytes = Encoding.GetEncoding("Shift_JIS").GetBytes(_result.GetString(0));
                m_message = Encoding.GetEncoding("Shift_JIS").GetString(m_message_bytes);
            }
        }

        protected override Response prepareConsulta()
        {

            if (m_id == 0)
            {
                throw new exception("[CmdNoticeInfo::prepareConsulta][Error] m_id is invalid(zero).", STDA_MAKE_ERROR(STDA_ERROR_TYPE.PANGYA_DB,

                    4, 0));
            }

            m_message = "";

            var r = procedure(
                    m_szConsulta,
                    Convert.ToString(m_id));

            checkResponse(r, "nao conseguiu pegar a Notice do server Info[COMMAND_ID=" + Convert.ToString(m_id) + "]");

            return r;
        }


        private uint m_id = 0;
        private string m_message = "";

        private string m_szConsulta = "pangya.ProcGetNotice";
    }
}
