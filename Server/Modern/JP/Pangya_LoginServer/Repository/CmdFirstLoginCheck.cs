using Pangya_LoginServer.Models;
using PangyaAPI.DataBase;
using PangyaAPI.Utilities;
using System;

namespace Pangya_LoginServer.Repository
{
    public class CmdFirstLoginCheck : Pangya_DB
    {
        public CmdFirstLoginCheck(uint _uid)
        {
            this.m_uid = _uid;
            this.m_check = false;
        } 
 
        public bool getLastCheck()
        {
            return m_check;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {

            checkColumnNumber(1, (uint)_result.cols);

            m_check = IFNULL<bool>(_result.data[0]);
        }

        protected override Response prepareConsulta()
        {

            m_check = false;

            var r = consulta(m_szConsulta + Convert.ToString(m_uid));

            checkResponse(r, "nao conseguiu verificar o first login do player: " + Convert.ToString(m_uid));

            return r;
        }

        private uint m_uid = new uint();
        private bool m_check;

        private const string m_szConsulta = "SELECT FIRST_LOGIN FROM pangya.account WHERE UID = ";
    }
}
