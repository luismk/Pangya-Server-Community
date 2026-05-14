
using System;
using System.Runtime.InteropServices.WindowsRuntime;
using PangyaAPI.SQL;

namespace PangyaAPI.Network.Repository
{
    public class CmdVerifyPass : Pangya_DB
    {
        uint m_uid = 0;
        string m_pass = "";
        bool m_lastVerify = false;

        public CmdVerifyPass(uint _uid, string pass)
        {
            m_pass = pass;
            m_uid = _uid;
            m_lastVerify = false;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {
            checkColumnNumber(1);
            try
            {
                var uid_req = int.Parse(_result.data[0].ToString());
                m_lastVerify = uid_req == m_uid;

                if (!m_lastVerify)
                    throw new Exception("[CmdVerifyPass::lineResult][Error] UID do player info nao e igual ao requisitado. UID Req: " + (uid_req) + " != " + m_uid);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);

            }
        }

        protected override Response prepareConsulta()
        {

            m_lastVerify = false;

            var r = procedure("pangya.ProcVerifyPass", $"{m_uid}, {this.makeText(m_pass)}");

            checkResponse(r, "nao conseguiu pegar a uid do player pela senha: " + m_pass);

            return r;

        }
        public bool getLastVerify() => m_lastVerify;

    }
}
