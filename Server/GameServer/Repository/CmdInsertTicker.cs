using System;
using System.Data;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;

namespace Pangya_GameServer.Repository
{
    public class CmdInsertTicker : Pangya_DB
    {
        public CmdInsertTicker(bool _waiter = false) : base(_waiter)
        {
            this.m_uid = 0;
            this.m_server_uid = 0;
            this.m_msg = "";
        }

        public CmdInsertTicker(uint _uid,
            uint _server_uid,
            string _msg,
            bool _waiter = false) : base(_waiter)
        {
            this.m_uid = _uid;
            this.m_server_uid = _server_uid;
            this.m_msg = _msg;
        }

        public uint getUID()
        {
            return (m_uid);
        }

        public void setUID(uint _uid)
        {
            m_uid = _uid;
        }

        public uint getServerUID()
        {
            return (m_server_uid);
        }

        public void setServerUID(uint _server_uid)
        {
            m_server_uid = _server_uid;
        }

        public string getMessage()
        {
            return m_msg;
        }

        public void setMessage(string _msg)
        {
            m_msg = _msg;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {

            // N�o usa por que � um UPDATE
            return;
        }

        protected override Response prepareConsulta()
        {

            if (m_uid == 0u)
            {
                throw new exception("[CmdInsertTicker::prepareConsulta][Error] m_uid is invalid(zero).", STDA_MAKE_ERROR(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            if (m_server_uid == 0u)
            {
                throw new exception("[CmdInsertTicker::prepareConsulta][Error] m_server_uid is invalid(zero).", STDA_MAKE_ERROR(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            var r = procedure(m_szConsulta, m_uid + ", " +
    m_server_uid + ", " +
    makeText(m_msg)
);
            checkResponse(r, "nao conseguiu adicionar um Ticker[MESSAGE=" + m_msg + "] para o PLAYER[UID=" + Convert.ToString(m_uid) + "] no Server[UID=" + Convert.ToString(m_server_uid) + "]");

            return r;
        }

        private uint m_uid = new uint();
        private uint m_server_uid = new uint();
        private string m_msg = "";

        private const string m_szConsulta = "pangya.ProcRegisterTicker";
    }
}