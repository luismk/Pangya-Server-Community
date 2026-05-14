using Pangya_LoginServer.Models;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;
using System;

namespace Pangya_LoginServer.Repository
{
    public class CmdCheckConfirmAccount : Pangya_DB
    { 
        public CmdCheckConfirmAccount(string _uid)
        {
            this.m_id = _uid;
            this.m_check = false;
        }
         
        public string getUID()
        {
            return (m_id);
        }

        public void setUID(string _uid)
        {
            m_id = _uid;
        }

        public bool getLastCheck()
        {
            return m_check;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {

            checkColumnNumber(1, _result.cols);

            m_check = IFNULL<bool>(_result.data[0]);
        }

        protected override Response prepareConsulta()
        { 
            m_check = false;

            var r = consulta(m_szConsulta + makeText(m_id));

            checkResponse(r, "nao conseguiu email da conta: " + m_id);

            return r;
        }

        private string m_id;
        private bool m_check;

        private const string m_szConsulta = "SELECT finish_reg FROM pangya.contas_beta WHERE LoginID = ";
    }
}
