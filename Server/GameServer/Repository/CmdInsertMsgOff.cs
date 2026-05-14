using PangyaAPI.SQL;
using PangyaAPI.Utilities;
using System;

namespace Pangya_GameServer.Repository
{
    public class CmdInsertMsgOff : Pangya_DB
    { 
        public CmdInsertMsgOff(uint _uid,
            uint _to_uid, string _msg,
            bool _waiter = false) : base(_waiter)
        {
            this.m_uid = _uid;
            this.m_to_uid = _to_uid;
            this.m_msg = _msg;
        } 

        protected override void lineResult(ctx_res _result, uint _index_result)
        {

            // N�o usa por que � um INSERT
            return;
        }

        protected override Response prepareConsulta()
        {

            if (m_uid == 0)
            {
                throw new exception("[CmdInsertMsgOff::prepareConsulta][Error] m_uid is invalid(zero)", STDA_MAKE_ERROR(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            if (m_to_uid == 0)
            {
                throw new exception("[CmdInsertMsgOff::prepareConsulta][Error] m_to_uid is invalid(zero)", STDA_MAKE_ERROR(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            if (m_msg.Length == 0)
            {
                throw new exception("[CmdInsertMsgOff::prepareConsulta][Error] m_msg is empty", STDA_MAKE_ERROR(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            if (m_msg.Length > 256)
            {
                throw new exception("[CmdInsertMsgOff::prepareConsulta][Error] m_msg size is great of limit supported", STDA_MAKE_ERROR(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            var r = procedure(m_szConsulta,
                Convert.ToString(m_uid) + ", " + Convert.ToString(m_to_uid) + ", " + makeText(m_msg));

            checkResponse(r, "nao conseguiu inserir Message Off[" + m_msg + "] do PLAYER[UID=" + Convert.ToString(m_uid) + "] para o PLAYER[UID=" + Convert.ToString(m_to_uid) + "]");

            return r;
        }

        private uint m_uid = new uint();
        private uint m_to_uid = new uint();
        private string m_msg = "";

        private const string m_szConsulta = "pangya.ProcAddMsgOff";
    }
}