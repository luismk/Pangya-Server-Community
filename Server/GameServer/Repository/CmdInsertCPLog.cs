using System;
using Pangya_GameServer.Models;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;

namespace Pangya_GameServer.Repository
{
    public class CmdInsertCPLog : Pangya_DB
    {
        public CmdInsertCPLog(bool _waiter = false) : base(_waiter)
        {
            this.m_uid = 0;
            this.m_id = -1;
            this.m_cp_log = new CPLog(0);
        }

        public CmdInsertCPLog(uint _uid,
            CPLog _cp_log,
            bool _waiter = false) : base(_waiter)
        {
            this.m_uid = _uid;
            this.m_cp_log = (_cp_log);
            this.m_id = -1L;
        }

        public uint getUID()
        {
            return (m_uid);
        }

        public void setUID(uint _uid)
        {
            m_uid = _uid;
        }

        public CPLog getLog()
        {
            return m_cp_log;
        }

        public void setLog(CPLog _cp_log)
        {
            m_cp_log = _cp_log;
        }

        public long getId()
        {
            return (m_id);
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {

            checkColumnNumber(1);

            m_id = IFNULL(_result.data[0]);
        }

        protected override Response prepareConsulta()
        {

            if (m_uid == 0u)
            {
                throw new exception("[CmdInsertCPLog::prepareConsulta][Error] m_uid is invalid.", STDA_MAKE_ERROR(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            m_id = -1L;

            var r = procedure(m_szConsulta,
                Convert.ToString(m_uid) + ", " + Convert.ToString((ushort)m_cp_log.getType()) + ", " + Convert.ToString(m_cp_log.getMailId()) + ", " + Convert.ToString(m_cp_log.getCookie()) + ", " + Convert.ToString(m_cp_log.getItemCount()));

            checkResponse(r, "nao conseguiu inserir o CPLog[" + m_cp_log.toString() + "] do PLAYER[UID=" + Convert.ToString(m_uid) + "]");

            return r;
        }


        private uint m_uid = new uint();
        private long m_id = new long();
        private CPLog m_cp_log = new CPLog();

        private const string m_szConsulta = "pangya.ProcInsertCPLog";
    }
}