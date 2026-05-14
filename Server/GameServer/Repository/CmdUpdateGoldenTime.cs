using System;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;


namespace Pangya_GameServer.Repository
{

    public class CmdUpdateGoldenTime : Pangya_DB
    {

        public CmdUpdateGoldenTime()
        {
            this.m_id = 0;
            this.m_is_end = false;
        }

        public CmdUpdateGoldenTime(uint _id,
            bool _is_end)
        {

            this.m_id = _id;
            this.m_is_end = _is_end;
        }

        public virtual void Dispose()
        {
        }

        public uint getId()
        {
            return (m_id);
        }

        public void setId(uint _id)
        {
            m_id = _id;
        }

        public bool getIsEnd()
        {
            return m_is_end;
        }

        public void setIsEnd(bool _is_end)
        {
            m_is_end = _is_end;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {

            // N�o usa por que � um UPDATE
            return;
        }

        protected override Response prepareConsulta()
        {

            if (m_id == 0u)
            {
                throw new exception("[CmdUpdateGoldenTime::prepareConsulta][Error] m_id is invalid(" + Convert.ToString(m_id) + ")", STDA_MAKE_ERROR(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            var r = consulta(m_szConsulta[0] + Convert.ToString(m_is_end ? 1 : 0) + m_szConsulta[1] + Convert.ToString(m_id));

            checkResponse(r, "nao conseguiu atualizar o Golden Time[ID=" + Convert.ToString(m_id) + ", IS_END=" + (m_is_end ? "TRUE" : "FALSE") + "]");

            return r;
        }

        private uint m_id = new uint();
        private bool m_is_end;

        private string[] m_szConsulta = { "UPDATE pangya.pangya_golden_time_info SET is_end = ", " WHERE index = " };
    }
}
