using PangyaAPI.DataBase;
using PangyaAPI.Utilities;
using System;

namespace Pangya_GameServer.Repository
{
    public class CmdInsertMsgOff : Pangya_DB
    { 
        public CmdInsertMsgOff(uint _uid,
            uint _to_uid, string _msg,
            bool _waiter = false)
        {
            this.m_uid = _uid;
            this.m_to_uid = _to_uid;
            this.m_msg = _msg;
        }

        public uint getUID()
        {
            return (m_uid);
        }

        public void setUID(uint _uid)
        {
            m_uid = _uid;
        }

        public uint getToUID()
        {
            return (m_to_uid);
        }

        public void setToUID(uint _to_uid)
        {
            m_to_uid = _to_uid;
        }

        public string getMessage()
        {
            return m_msg;
        }

        public void setMessage(string _msg)
        {
            m_msg = _msg;
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
                Convert.ToString(m_uid) + ", " + Convert.ToString(m_to_uid) + ", " + m_msg);

            checkResponse(r, "nao conseguiu inserir Message Off[" + m_msg + "] do Normal[UID=" + Convert.ToString(m_uid) + "] para o Normal[UID=" + Convert.ToString(m_to_uid) + "]");

            return r;
        }

        private uint m_uid = new uint();
        private uint m_to_uid = new uint();
        private string m_msg = "";

        private const string m_szConsulta = "pangya.ProcAddMsgOff";
    }
}