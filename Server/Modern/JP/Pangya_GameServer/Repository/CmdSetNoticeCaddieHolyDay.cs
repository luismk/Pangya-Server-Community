using System;
using PangyaAPI.DataBase;
using PangyaAPI.Utilities;

namespace Pangya_GameServer.Repository
{
    public class CmdSetNoticeCaddieHolyDay : Pangya_DB
    { 
        public CmdSetNoticeCaddieHolyDay(uint _uid, int _id, short _check)
        {
            this.m_uid = _uid;
            this.m_id = _id;
            this.m_check = _check;
        }
         
        protected override void lineResult(ctx_res _result, uint _index_result)
        {

            // N�o usa por que � um UPDATE
            return;
        }

        protected override Response prepareConsulta()
        {

            if (m_uid == 0)
            {
                throw new exception("[CmdSetNoticeCaddieHolyDay::prepareConsulta][Error] m_uid is invalid(zero)", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            if (m_id <= 0)
            {
                throw new exception("[CmdSetNoticeCaddieHolyDay::prepareConsulta][Error] m_id[value=" + Convert.ToString(m_id) + "] is invalid", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            var r = _update(m_szConsulta[0] + Convert.ToString(m_check) + m_szConsulta[1] + Convert.ToString(m_uid) + m_szConsulta[2] + Convert.ToString(m_id));

            checkResponse(r, "nao conseguiu atualizar o Aviso[check=" + (m_check != 0 ? "ON" : "OFF") + "] de ferias do Caddie[ID=" + Convert.ToString(m_id) + "] do Normal[UID=" + Convert.ToString(m_uid) + "]");

            return r;
        }

        private uint m_uid = new uint();
        private int m_id = new int();
        private short m_check;

        private string[] m_szConsulta = { "UPDATE pangya.pangya_caddie_information SET CheckEnd = ", " WHERE UID = ", " AND item_id = " };
    }
}