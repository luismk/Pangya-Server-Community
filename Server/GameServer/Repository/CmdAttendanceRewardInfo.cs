using System;
using Pangya_GameServer.Models;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;

namespace Pangya_GameServer.Repository
{
    public class CmdAttendanceRewardInfo : Pangya_DB
    {
        public CmdAttendanceRewardInfo()
        {
            this.m_uid = 0;
            this.m_ari = new AttendanceRewardInfoEx();
        }

        public CmdAttendanceRewardInfo(uint _uid)
        {
            this.m_uid = _uid;
            this.m_ari = new AttendanceRewardInfoEx();
        }


        public uint getUID()
        {
            return m_uid;
        }

        public void setUID(uint _uid)
        {
            m_uid = _uid;
        }

        public AttendanceRewardInfoEx getInfo()
        {
            return m_ari;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {

            checkColumnNumber(6);

            try
            {
                m_ari.counter = IFNULL(_result.data[0]);
                m_ari.now._typeid = IFNULL(_result.data[1]);
                m_ari.now.qntd = IFNULL(_result.data[2]);
                m_ari.after._typeid = IFNULL(_result.data[3]);
                m_ari.after.qntd = IFNULL(_result.data[4]);

                if (!(_result.data[5] is DBNull))
                    m_ari.last_login.CreateTime(_translateDate(_result.data[5]));

                if (m_ari.counter == 0)
                {
                    if (m_ari.after._typeid == 0 && m_ari.after.qntd == 0)
                    {
                        m_ari.login = 3;
                    }
                    else if ((m_ari.now._typeid == 0 && m_ari.now.qntd == 0))
                    {
                        m_ari.login = 2;
                    }
                    else if ((m_ari.after._typeid == 0 && m_ari.after.qntd == 0) && (m_ari.now._typeid == 0 && m_ari.now.qntd == 0)) { m_ari.login = 2; }
                }
            }
            catch (exception e)
            {

                throw e;
            }
        }

        protected override Response prepareConsulta()
        {
            var r = procedure(m_szConsulta,
                Convert.ToString(m_uid));

            checkResponse(r, "nao conseguiu pegar attendance reward info do player: " + Convert.ToString(m_uid));

            return r;
        }

        private uint m_uid;
        private AttendanceRewardInfoEx m_ari = new AttendanceRewardInfoEx();

        private const string m_szConsulta = "pangya.ProcGetAttendanceReward";

        public partial class ProcGetAttendanceReward
        {
            public uint counter { get; set; }
            public uint item_typeid_now { get; set; }
            public uint item_qntd_now { get; set; }
            public uint item_typeid_after { get; set; }
            public uint item_qntd_after { get; set; }
            public Nullable<System.DateTime> last_login { get; set; }
        }
    }
}