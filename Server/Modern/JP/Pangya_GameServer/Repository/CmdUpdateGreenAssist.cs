using System;
using PangyaAPI.DataBase;
using PangyaAPI.Utilities;

namespace Pangya_GameServer.Repository
{
    public class CmdUpdateGreenAssist : Pangya_DB
    {
        public CmdUpdateGreenAssist(uint _uid, bool isCheck)
        {
            this.m_uid = _uid;
            m_check = isCheck;  
        }

        public bool getCheck() => m_check;

        protected override void lineResult(ctx_res _result, uint _index_result)
        { 
        }

        protected override Response prepareConsulta()
        {

            if (m_uid == 0)
            {
                throw new exception("[CmdUpdateGreenAssist::prepareConsulta][Error] m_uid is invalid(zero)", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            var r = _update(m_szConsulta + Convert.ToInt32(m_check) + " where UID =" + m_uid);

            checkResponse(r, "nao conseguiu atualizar o Aviso[assist=" + (m_check ? "ON" : "OFF") + "] do Normal[UID=" + Convert.ToString(m_uid) + "]");

            return r;
        }

        private uint m_uid = new uint();
        private bool m_check;
        private string m_szConsulta = "update pangya.pangya_assistente set [assist] = ";
    }
}