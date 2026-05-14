using System;
using Pangya_GameServer.Models;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;
namespace Pangya_GameServer.Repository
{
    public class CmdInsertGrandPrixClear : Pangya_DB
    {
        public CmdInsertGrandPrixClear(bool _waiter = false) : base(_waiter)
        {
            this.m_uid = 0;
            this.m_gpc = new GrandPrixClear();
        }

        public CmdInsertGrandPrixClear(uint _uid,
            GrandPrixClear _gpc,
            bool _waiter = false) : base(_waiter)
        {
            this.m_uid = _uid;
            this.m_gpc = _gpc;
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

            // N�o usa por que � um INSERT
            return;
        }

        protected override Response prepareConsulta()
        {

            if (m_uid == 0u)
            {
                throw new exception("[CmdInsertGrandPrixClear::prepareConsulta][Error] m_uid is invalid(zero)", STDA_MAKE_ERROR(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            if (m_gpc._typeid == 0u)
            {
                throw new exception("[CmdInsertGrandPrixClear::prepareConsulta][Error] Grand Prix Clear is invalid typeid is zero", STDA_MAKE_ERROR(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            var r = consulta(m_szConsulta[0] + Convert.ToString(m_uid) + ", " + Convert.ToString(m_gpc._typeid) + ", " + Convert.ToString(m_gpc.position) + m_szConsulta[1]);

            checkResponse(r, "nao conseguiu inserir o GrandPrixClear[TYPEID=" + Convert.ToString(m_gpc._typeid) + ", POSITION=" + Convert.ToString(m_gpc.position) + "] do Player[UID=" + Convert.ToString(m_uid) + "]");

            return r;
        }
        private uint m_uid = new uint();
        private GrandPrixClear m_gpc = new GrandPrixClear();

        private string[] m_szConsulta = { "INSERT INTO pangya.pangya_grandprix_clear(UID, TYPEID, FLAG) VALUES(", ")" };
    }
}
