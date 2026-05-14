using System;
using Pangya_GameServer.Models;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;
namespace Pangya_GameServer.Repository
{
    public class CmdUpdateAttendanceReward : Pangya_DB
    {

        public CmdUpdateAttendanceReward(uint _uid,
            AttendanceRewardInfoEx _ari)
        {
            this.m_uid = _uid;
            this.m_ari = (_ari);
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

        public AttendanceRewardInfoEx getInfo()
        {
            return m_ari;
        }

        public void setInfo(AttendanceRewardInfoEx _ari)
        {
            m_ari = _ari;
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
                throw new exception("[CmdUpdateAttendanceReward::prepareConsulta][Error] m_uid is invalid(zero).", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            string last_login = "null";

            if (m_ari.last_login.Year != 0 && m_ari.last_login.Month != 0 && m_ari.last_login.Day != 0)
                last_login = makeText(_formatDate(m_ari.last_login.ConvertTime()));

            var r = procedure(m_szConsulta,
                    Convert.ToString(m_uid) + ", " + Convert.ToString(m_ari.counter) + ", " + Convert.ToString(m_ari.now._typeid) + ", " + Convert.ToString(m_ari.now.qntd) + ", " + Convert.ToString(m_ari.after._typeid) + ", " + Convert.ToString(m_ari.after.qntd) + ", " + last_login);

            checkResponse(r, "nao conseguiu Atualizar o Attendance Reward[COUNTER=" + Convert.ToString(m_ari.counter) + ", NOW_TYPEID=" + Convert.ToString(m_ari.now._typeid) + ", NOW_QNTD=" + Convert.ToString(m_ari.now.qntd) + ", AFTER_TYPEID=" + Convert.ToString(m_ari.after._typeid) + ", AFTER_QNTD=" + Convert.ToString(m_ari.after.qntd) + ", LAST_LOGIN=" + last_login + "] do PLAYER[UID=" + Convert.ToString(m_uid) + "]");

            return r;
        }
        private uint m_uid = new uint();
        private AttendanceRewardInfoEx m_ari = new AttendanceRewardInfoEx();

        private const string m_szConsulta = "pangya.ProcUpdateAttendanceReward";
    }
}
