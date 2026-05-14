using Pangya_MessengerServer.Models;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;
using System;

namespace Pangya_MessengerServer.Repository
{
    public class CmdAddFriend : Pangya_DB
    {
        public CmdAddFriend()
        {
            this.m_uid = 0u;
            this.m_fi = new FriendInfoEx(0);
        }

        public CmdAddFriend(uint _uid,
            FriendInfoEx _fi)
        {
            this.m_uid = _uid;
            this.m_fi = _fi;
        }


        public uint getUID()
        {
            return (m_uid);
        }

        public void setUID(uint _uid)
        {
            m_uid = _uid;
        }

        public FriendInfoEx getInfo()
        {
            return m_fi;
        }

        public void setInfo(FriendInfoEx _fi)
        {
            m_fi = _fi;
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
                throw new exception("[CmdAddFriend::prepareConsulta][Error] m_uid is invalid(zero)", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            if (m_fi.uid == 0)
            {
                throw new exception("[CmdAddFriend::prepareConsulta][Error] m_fi.uid is invalid(zero)", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            // Zero Bit Online And Sex Of State Flag
            m_fi.state.online = 0;
            m_fi.state.sex = 0;

            var r = procedure(
                m_szConsulta,
                Convert.ToString(m_uid) + ", " + Convert.ToString(m_fi.uid) + ", " + makeText(m_fi.apelido) + ", " + Convert.ToString(m_fi.lUnknown) + ", " + Convert.ToString(m_fi.lUnknown2) + ", " + Convert.ToString(m_fi.lUnknown3) + ", " + Convert.ToString(m_fi.lUnknown4) + ", " + Convert.ToString(m_fi.lUnknown5) + ", " + Convert.ToString(m_fi.lUnknown6) + ", " + Convert.ToString((short)m_fi.cUnknown_flag) + ", " + Convert.ToString((ushort)m_fi.state.ucState));

            checkResponse(r, "nao conseguiu adicionar o Friend[UID=" + Convert.ToString(m_fi.uid) + ", APELIDO=" + (m_fi.apelido) + ", NICK=" + (m_fi.nickname) + ", STATE=" + Convert.ToString((ushort)m_fi.state.ucState) + "] para o player[UID=" + Convert.ToString(m_uid) + "]");

            return r;
        }

        private uint m_uid = new uint();
        private FriendInfoEx m_fi = new FriendInfoEx();

        private const string m_szConsulta = "pangya.ProcAddFriend";
    }
}
