using System;
using Pangya_GameServer.Models;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;
namespace Pangya_GameServer.Repository
{
    public class CmdUpdateGrandPrixClear : Pangya_DB
    {
        public CmdUpdateGrandPrixClear()
        {
            this.m_uid = 0;
            this.m_gpc = new GrandPrixClear();
        }

        public CmdUpdateGrandPrixClear(uint _uid,
            GrandPrixClear _gpc)
        {
            this.m_uid = _uid;
            this.m_gpc = (_gpc);
        }

        public uint getUID()
        {
            return (m_uid);
        }

        public void setUID(uint _uid)
        {
            m_uid = _uid;

        }

        public GrandPrixClear getInfo()
        {
            return m_gpc;
        }

        public void setInfo(GrandPrixClear _gpc)
        {
            m_gpc = _gpc;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {

            // N�o usa por que � um UPDATE
            return;
        }

        protected override Response prepareConsulta()
        {

            if (m_uid == 0u)
            {
                throw new exception("[CmdUpdateGrandPrixClear::prepareConsulta][Error] m_uid is invalid(zero)", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            if (m_gpc._typeid == 0u)
            {
                throw new exception("[CmdUpdateGrandPrixClear::prepareConsulta][Error] Grand Prix Clear is invalid typeid is zero", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            var r = consulta(m_szConsulta[0] + Convert.ToString(m_gpc.position) + m_szConsulta[1] + Convert.ToString(m_uid) + m_szConsulta[2] + Convert.ToString(m_gpc._typeid));

            checkResponse(r, "nao conseguiu atualizar o Grand Prix Clear[TYPEID=" + Convert.ToString(m_gpc._typeid) + ", POSITION=" + Convert.ToString(m_gpc.position) + "] do PLAYER[UID=" + Convert.ToString(m_uid) + "]");

            return r;
        }

        private uint m_uid = new uint();
        private GrandPrixClear m_gpc = new GrandPrixClear();

        private string[] m_szConsulta = { "UPDATE pangya.pangya_grandprix_clear SET flag = ", " WHERE UID = ", " AND typeid = " };
    }
}
