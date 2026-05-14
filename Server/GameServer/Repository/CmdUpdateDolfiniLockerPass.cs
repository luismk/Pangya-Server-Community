using System;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;

namespace Pangya_GameServer.Repository
{
    public class CmdUpdateDolfiniLockerPass : Pangya_DB
    {

        public CmdUpdateDolfiniLockerPass(uint _uid,
            string _pass)
        {
            this.m_uid = _uid;
            this.m_pass = _pass;
        }

        public virtual void Dispose()
        {
        }

        public uint getUID()
        {
            return (m_uid);
        }

        public void setUID(uint _uid)
        {
            m_uid = _uid;

        }

        public string getPass()
        {
            return m_pass;
        }

        public void setPass(string _pass)
        {
            m_pass = _pass;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {

            // n�o usa por que � um UDPATE
            return;
        }

        protected override Response prepareConsulta()
        {

            if (m_pass.Length == 0)
            {
                throw new exception("[CmdUpdateDolfiniLockerPass::prepareConsulta][Error] pass is empty", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            if (m_pass.Length > 7)
            {
                throw new exception("[CmdUpdateDolfiniLockerPass::prepareConsulta][Error] pass is hight of permited", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            var r = procedure(m_szConsulta,
                Convert.ToString(m_uid) + ", " + (m_pass));

            checkResponse(r, "nao conseguiu atualizar a senha[value=" + m_pass + "] do dolfini locker do PLAYER[UID=" + Convert.ToString(m_uid) + "]");

            return r;
        }

        private uint m_uid = new uint();
        private string m_pass = "";

        private const string m_szConsulta = "pangya.ProcChangeDolfiniLockerPass";
    }
}
