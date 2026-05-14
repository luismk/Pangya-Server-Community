using System;
using Pangya_GameServer.Models;
using PangyaAPI.SQL;

namespace Pangya_GameServer.Repository
{
    public class CmdUpdateTreasureHunterCoursePoint : Pangya_DB
    {
        public CmdUpdateTreasureHunterCoursePoint()
        {
            this.m_thi = new TreasureHunterInfo();
        }

        public CmdUpdateTreasureHunterCoursePoint(TreasureHunterInfo _thi)
        {
            this.m_thi = (_thi);
        }

        public virtual void Dispose()
        {
        }

        public TreasureHunterInfo getInfo()
        {
            return m_thi;
        }

        public void setInfo(TreasureHunterInfo _thi)
        {
            m_thi = _thi;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {

            // N�o usa por que � um UPDATE
            return;
        }

        protected override Response prepareConsulta()
        {

            var r = consulta(m_szConsulta[0] + Convert.ToString(m_thi.point) + m_szConsulta[1] + Convert.ToString((ushort)(m_thi.course & 0x7F)));

            checkResponse(r, "nao conseguiu atulizar o Treasure Hunter Info[COURSE=" + Convert.ToString((ushort)(m_thi.course & 0x7F)) + ", POINT=" + Convert.ToString(m_thi.point) + "]");

            return r;
        }


        private TreasureHunterInfo m_thi = new TreasureHunterInfo();

        private string[] m_szConsulta = { "UPDATE pangya.pangya_course_reward_treasure SET PANGREWARD = ", " WHERE COURSE = " };
    }
}
