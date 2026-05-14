using System;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;

namespace Pangya_GameServer.Repository
{
    public class CmdPayCaddieHolyDay : Pangya_DB
    {
        public CmdPayCaddieHolyDay()
        {
            this.m_uid = 0;
            this.m_id = -1;
            this.m_end_dt = "";
        }

        public CmdPayCaddieHolyDay(uint _uid,
            int _id, string _end_dt)
        {
            this.m_uid = _uid;
            this.m_id = _id;
            this.m_end_dt = _end_dt;
        }

        public virtual void Dispose()
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

        public int getId()
        {
            return (m_id);
        }

        public void setId(int _id)
        {
            m_id = _id;
        }

        public string getEndDate()
        {
            return m_end_dt;
        }

        public void setEndDate(string _end_dt)
        {
            m_end_dt = _end_dt;
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
                throw new exception("[CmdPayCaddieHolyDay::prepareConsulta][Error] m_uid is invalid(zero)", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            if (m_id <= 0)
            {
                throw new exception("[CmdPayCaddieHolyDay::prepareConsulta][Error] m_id[value=" + Convert.ToString(m_id) + "] is invalid", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            if (m_end_dt.Length == 0)
            {
                throw new exception("[CmdPayCaddieHolyDay::prepareConsulta][Error] m_end_dt_unix is invalid(empty)", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            var r = procedure(m_szConsulta,
                Convert.ToString(m_uid) + ", " + Convert.ToString(m_id) + ", " + m_end_dt);

            checkResponse(r, "nao conseguiu atualizar a end date[exntend days of caddie][date=" + m_end_dt + "] do caddie[ID=" + Convert.ToString(m_id) + "] do PLAYER[UID=" + Convert.ToString(m_uid) + "]");

            return r;

        }

        private uint m_uid = new uint();
        private int m_id = new int();
        private string m_end_dt = "";

        private const string m_szConsulta = "pangya.ProcUpdateCaddieHolyDay";
    }
}