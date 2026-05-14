using Pangya_LoginServer.Models;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;
using System;

namespace Pangya_LoginServer.Repository
{
    public class CmdAddFirstLogin : Pangya_DB
    {
        public CmdAddFirstLogin()
        {
            this.m_uid = 0;
            this.m_flag = 0;
        }

        public CmdAddFirstLogin(uint _uid, byte _flag)
        {
            this.m_uid = _uid;
            this.m_flag = _flag;
        }

        public void Dispose()
        {
        }

        public uint getUID()
        {
            return m_uid;
        }

        public void setUID(uint _uid)
        {
            m_uid = _uid;
        }

        public byte getFLag()
        {
            return m_flag;
        }

        public void setFlag(byte _flag)
        {
            m_flag = _flag;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {

            // N�o usa por que � um UPDATE
        }

        protected override Response prepareConsulta()
        {

            var r = _update(m_szConsulta[0] + Convert.ToString((ushort)m_flag) + m_szConsulta[1] + Convert.ToString(m_uid));

            checkResponse(r, "nao conseguiu atualizar o first login do player: " + Convert.ToString(m_uid));

            return r;
        }


        private uint m_uid = new uint();
        private byte m_flag;

        private string[] m_szConsulta = { "UPDATE pangya.account SET FIRST_LOGIN = ", " WHERE UID = " };
    }
}
