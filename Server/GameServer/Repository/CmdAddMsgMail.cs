using System;
using System.Data;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;

namespace Pangya_GameServer.Repository
{
    public class CmdAddMsgMail : Pangya_DB
    {
        public CmdAddMsgMail(uint _uid_from,
                uint _uid_to, string _msg)
        {
            this.m_uid_from = _uid_from;
            this.m_uid_to = _uid_to;
            this.m_msg = _msg;
            this.m_mail_id = -1;
        }

        public uint getUIDFrom()
        {
            return (m_uid_from);
        }

        public void setUIDFrom(uint _uid_from)
        {
            m_uid_from = _uid_from;
        }

        public uint getUIDTo()
        {
            return (m_uid_to);
        }

        public void setUIDTo(uint _uid_to)
        {
            m_uid_to = _uid_to;
        }

        public string getMsg()
        {
            return m_msg;
        }

        public void setMsg(string _msg)
        {
            m_msg = _msg;
        }

        public int getMailID()
        {
            return (m_mail_id);
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {
            checkColumnNumber(1);

            m_mail_id = IFNULL<int>(_result.data[0]);
        }

        protected override Response prepareConsulta()
        {

            if (m_uid_to == 0 || m_msg.Length == 0)
            {
                throw new exception("[CmdAddMsgMail::prepareConsulta][Error] uid[value=" + Convert.ToString(m_uid_to) + "] to send is invalid or msg is emtpy", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            m_mail_id = -1;

            var r = procedure(m_szConsulta, m_uid_from + ", " + m_uid_to + ", " + makeText(m_msg));



            checkResponse(r, "PLAYER[UID=" + Convert.ToString(m_uid_from) + "] nao conseguiu adicionar msg[value=" + m_msg + "] no mail do PLAYER[UID=" + Convert.ToString(m_uid_to) + "]");

            return r;
        }

        private uint m_uid_from = 0;
        private uint m_uid_to = 0;
        private string m_msg = "";
        private int m_mail_id = new int();

        private const string m_szConsulta = "pangya.ProcColocaMsgNoGiftTable";
    }
}