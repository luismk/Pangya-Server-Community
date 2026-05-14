using System;
using System.Linq;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;

namespace Pangya_GameServer.Repository
{
    public class CmdDeleteEmail : Pangya_DB
    {
        public CmdDeleteEmail()
        {
            this.m_uid = 0;
            this.m_email_id = null;
            this.m_count = 0;
        }

        public CmdDeleteEmail(uint _uid,
            uint[] _email_id,
            uint _count)
        {
            this.m_uid = _uid;
            this.m_email_id = null;
            this.m_count = 0;

            if (_email_id == null || _count == 0u)
            {
                return;
            }

            // Alloc memory
            m_email_id = _email_id;
            m_count = _count;
        }

        public uint getUID()
        {
            return m_uid;
        }

        public void setUID(uint _uid)
        {
            m_uid = _uid;
        }

        public uint[] getEmailID()
        {
            return m_email_id;
        }

        public void setEmailID(uint[] _email_id, uint _count)
        {

            if (_email_id == null || _count == 0u)
            {
                m_email_id = null;
                m_count = 0;
                return;
            }

            if (m_email_id == null || _count > m_count)
            {
                m_email_id = new uint[_count];
            }

            Array.Copy(_email_id, m_email_id, _count);
            m_count = _count;
        }

        public uint getCount()
        {
            return m_count;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {
            // UPDATE n�o usa o result
            // mas caso algum dia eu queira usar o result, depois de deletar um email eu mexo aqui
            return;
        }

        protected override Response prepareConsulta()
        {

            if (m_count > 0u && m_email_id != null)
            {
                string ids = string.Join(", ", m_email_id.Take((int)m_count));


                m_szConsulta = new string[] { "UPDATE pangya.pangya_gift_table SET valid = 0 WHERE uid = " + Convert.ToString(m_uid) + " AND Msg_ID IN(" + ids + ")" };
                var r = _update(m_szConsulta[0]);

                checkResponse(r, "nao conseguiu deletar o email(s) do player: " + Convert.ToString(m_uid));

                return r;

            }
            else
            {
                throw new exception("[CmdDeleteEmail][Error] nao pode deletar Email(s) sem id(s)");
            }
        }


        private uint m_uid = 0;
        private uint[] m_email_id;
        private uint m_count = 0;

        private string[] m_szConsulta = { "UPDATE pangya.pangya_gift_table SET valid = 0 WHERE uid = ", " AND Msg_ID IN(", ")" };
    }
}