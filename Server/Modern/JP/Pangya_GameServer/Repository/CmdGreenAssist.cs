using System;
using PangyaAPI.DataBase;
using PangyaAPI.Utilities;

namespace Pangya_GameServer.Repository
{
    public class CmdGreenAssist : Pangya_DB
    {
        public CmdGreenAssist(uint _uid)
        {
            this.m_uid = _uid;  
        }

        public bool getCheck() => m_check;

        protected override void lineResult(ctx_res _result, uint _index_result)
        {
            m_check = _result.GetBoolean(0);
        }

        protected override Response prepareConsulta()
        {

            if (m_uid == 0)
            {
                throw new exception("[CmdGreenAssist::prepareConsulta][Error] m_uid is invalid(zero)", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }
             
            var r = consulta(m_szConsulta + m_uid);

            checkResponse(r, "nao conseguiu encontrar o Aviso[assist=" + (m_check ? "ON" : "OFF") + "] do Normal[UID=" + Convert.ToString(m_uid) + "]");

            return r;
        }

        private uint m_uid = new uint(); 
        private bool m_check; 
        private string m_szConsulta = "SELECT assist FROM pangya.pangya_assistente where UID =";
    }
}