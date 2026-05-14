using System;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;

namespace Pangya_GameServer.Repository
{
    public class CmdSetNoticeCaddieHolyDay : Pangya_DB
    {
        public CmdSetNoticeCaddieHolyDay()
        {
            this.m_uid = 0;
            this.m_id = -1;
            this.m_check = 0;
        }

        public CmdSetNoticeCaddieHolyDay(uint _uid,
            int _id, ushort _check)
        {
            this.m_uid = _uid;
            this.m_id = _id;
            this.m_check = _check;
        }
        public uint getUID()
        {
            return (m_uid);
        }

        public void setUID(uint _uid)
        {
            m_uid = _uid;
        }

        public int getId()
        {
            return (m_id);
        }

        public void setId(int _id)
        {
            m_id = _id;
        }

        public ushort getCheck()
        {
            return m_check;
        }

        public void setCheck(ushort _check)
        {
            m_check = _check;
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

            checkResponse(r, "nao conseguiu atualizar o Aviso[check=" + (m_check != 0 ? "ON" : "OFF") + "] de ferias do Caddie[ID=" + Convert.ToString(m_id) + "] do PLAYER[UID=" + Convert.ToString(m_uid) + "]");

            return r;
        }

        private uint m_uid = new uint();
        private int m_id = new int();
        private ushort m_check;

        private string[] m_szConsulta = { "UPDATE pangya.pangya_caddie_information SET CheckEnd = ", " WHERE UID = ", " AND item_id = " };
    }
}