using System;
using PangyaAPI.SQL;

namespace Pangya_GameServer.Repository
{
    public class CmdItemLeftFromEmail : Pangya_DB
    {
        public CmdItemLeftFromEmail()
        {
            this.m_email_id = 0;
        }

        public CmdItemLeftFromEmail(int _email_id)
        {
            this.m_email_id = _email_id;
        }

        public int getEmailID()
        {
            return m_email_id;
        }

        public void setEmailID(int _email_id)
        {
            m_email_id = _email_id;
        }
        protected override void lineResult(ctx_res _result, uint _index_result)
        {
            return;
        }

        protected override Response prepareConsulta()
        {

            var r = _update(m_szConsulta + Convert.ToString(m_email_id));

            checkResponse(r, "nao conseguiu deletar o(s) item(ns) do email[ID=" + Convert.ToString(m_email_id) + "]");

            return r;
        }

        private int m_email_id = 0;

        private const string m_szConsulta = "UPDATE pangya.pangya_item_mail SET valid = 0 WHERE Msg_ID = ";
    }
}