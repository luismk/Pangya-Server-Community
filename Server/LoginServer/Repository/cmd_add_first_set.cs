using Pangya_LoginServer.Models;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;
using System;

namespace Pangya_LoginServer.Repository
{
    public class CmdAddFirstSet : Pangya_DB
    {
        public CmdAddFirstSet()
        {
            this.m_uid = 0;
        }

        public CmdAddFirstSet(uint _uid)
        {
            this.m_uid = _uid;
        }

        public void Dispose()
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

        protected override void lineResult(ctx_res _result, uint _index_result)
        {

            // N�o usa por que � INSERT e UPDATE
            return;
        }

        protected override Response prepareConsulta()
        {

            var r = procedure(m_szConsulta,
                Convert.ToString(m_uid));

            checkResponse(r, "nao conseguiu add first set do player: " + Convert.ToString(m_uid));

            return r;
        }

        private uint m_uid = new uint();

        private const string m_szConsulta = "pangya.ProcFirstSet";
    }
}
