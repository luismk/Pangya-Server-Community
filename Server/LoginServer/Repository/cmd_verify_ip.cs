using Pangya_LoginServer.Models;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;
using System;

namespace Pangya_LoginServer.Repository
{
    public class CmdVerifyIP : Pangya_DB
    {
        public CmdVerifyIP()
        {
            this.m_uid = 0;
            this.m_ip = "";
        }

        public CmdVerifyIP(uint _uid,
            string _ip)
        {
            this.m_uid = _uid;
            this.m_ip = _ip;
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

        public string getIP()
        {
            return m_ip;
        }

        public void setIP(string _ip)
        {
            m_ip = _ip;
        }

        public bool getLastVerify()
        {
            return m_last_verify;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {

            checkColumnNumber(1, (uint)_result.cols);

            uint uid_req = IFNULL(_result.data[0]);

            if (uid_req != m_uid)
            {
                throw new exception("[CmdVerifyIP::lineResult][Error] o uid recuperado para verificar o ip access do player e diferente. UID_req: " + Convert.ToString(m_uid) + " != " + Convert.ToString(uid_req), ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                    3, 0));
            }

            m_last_verify = true;
        }

        protected override Response prepareConsulta()
        {

            m_last_verify = false;

            var r = procedure(m_szConsulta,
                Convert.ToString(m_uid) + ", " + makeText(m_ip));

            checkResponse(r, "nao conseguiu verificar o ip de accesso do player: " + Convert.ToString(m_uid));

            return r;
        }
        private uint m_uid = new uint();
        private string m_ip = "";
        private bool m_last_verify;

        private const string m_szConsulta = "pangya.ProcVerifyIP";
    }
}

