using Pangya_LoginServer.Models;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;
using System;

namespace Pangya_LoginServer.Repository
{
    public class CmdFirstSetCheck : Pangya_DB
    {
        public CmdFirstSetCheck()
        {
            this.m_uid = 0;
            this.m_check = false;
        }

        public CmdFirstSetCheck(uint _uid)
        {
            this.m_uid = _uid;
            this.m_check = false;
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

        public bool getLastCheck()
        {
            return m_check;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {

            checkColumnNumber(1, (uint)_result.cols);

            m_check = (IFNULL(_result.data[0]) == 1 ? true : false);
        }

        protected override Response prepareConsulta()
        {

            m_check = false;

            var r = consulta(m_szConsulta + Convert.ToString(m_uid));

            checkResponse(r, "nao conseguiu verificar o first set do player: " + Convert.ToString(m_uid));

            return r;
        }
        private uint m_uid = new uint();
        private bool m_check;

        private const string m_szConsulta = "SELECT FIRST_SET FROM pangya.account WHERE uid = ";
    }
}
