using System;
using Pangya_GameServer.Models;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;

namespace Pangya_GameServer.Repository
{
    public class CmdUpdateEmail : Pangya_DB
    {
        public CmdUpdateEmail(uint _uid,
            EmailInfoEx _ei)
        {
            this.m_uid = _uid;
            this.m_ei = _ei;
        }

        public CmdUpdateEmail()
        {
            this.m_uid = 0;
            this.m_ei = new EmailInfoEx();
        }

        public uint getUID()
        {
            return m_uid;
        }

        public void setUID(uint _uid)
        {
            m_uid = _uid;
        }

        public EmailInfoEx getEmail()
        {
            return m_ei;
        }

        public void setEmail(EmailInfoEx _ei)
        {
            m_ei = _ei;
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
                throw new exception("[CmdUpdateEmail::prepareConsulta][Error] m_uid is invalid(0)");
            }

            if (m_ei.id <= 0)
            {
                throw new exception("[CmdUpdateEmail::prepareConsulta][Error] Email[ID=" + Convert.ToString(m_ei.id) + "] is invalid.");
            }

            var r = consulta(m_szConsulta[0] + Convert.ToString((ushort)m_ei.lida_yn) + m_szConsulta[1] + Convert.ToString(m_ei.visit_count) + m_szConsulta[2] + Convert.ToString(m_uid) + m_szConsulta[3] + Convert.ToString(m_ei.id));

            checkResponse(r, "nao conseguiu atualizar o Email[ID=" + Convert.ToString(m_ei.id) + ", LIDA_YN=" + Convert.ToString((ushort)m_ei.lida_yn) + ", VISIT_COUNT=" + Convert.ToString(m_ei.visit_count) + "] do PLAYER[UID=" + Convert.ToString(m_uid) + "]");

            return r;
        }

        private EmailInfoEx m_ei = new EmailInfoEx();
        private uint m_uid = 0;

        private string[] m_szConsulta = { "UPDATE pangya.pangya_gift_table SET Lida_YN = ", ", Contador_Vista = ", " WHERE UID = ", " AND Msg_ID = " };
    }
}