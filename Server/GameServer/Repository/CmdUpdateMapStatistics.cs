using System;
using Pangya_GameServer.Models;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;

namespace Pangya_GameServer.Repository
{
    public class CmdUpdateMapStatistics : Pangya_DB
    {
        public CmdUpdateMapStatistics(uint _uid,
            MapStatisticsEx _ms,
            byte _assist)
        {

            this.m_uid = _uid;
            //this.
            this.m_assist = _assist;
            this.m_ms = new MapStatisticsEx(_ms);
        }

        public uint getUID()
        {
            return (m_uid);
        }

        public void setUID(uint _uid)
        {

            m_uid = _uid;

        }

        public byte getAssist()
        {
            return m_assist;
        }

        public void setAssist(byte _assist)
        {
            m_assist = _assist;
        }

        public MapStatisticsEx getInfo()
        {
            return new MapStatisticsEx(m_ms);
        }

        public void setInfo(MapStatisticsEx _ms)
        {

            m_ms = _ms;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {

            // N�o usa por que � um UPDATE
            return;
        }

        protected override Response prepareConsulta()
        {

            if (m_uid == 0)
            {
                throw new exception("[CmdUpdateMapStatistics::prepareConsulta][Error] m_uid is invalid(zero)", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            var r = procedure(m_szConsulta,
                Convert.ToString(m_uid) + ", " + Convert.ToString((ushort)m_ms.tipo) + ", " + Convert.ToString((ushort)(m_ms.course & 0x7F)) + ", " + Convert.ToString(m_ms.tacada) + ", " + Convert.ToString(m_ms.putt) + ", " + Convert.ToString(m_ms.hole) + ", " + Convert.ToString(m_ms.fairway) + ", " + Convert.ToString(m_ms.hole_in) + ", " + Convert.ToString(m_ms.putt_in) + ", " + Convert.ToString(m_ms.total_score) + ", " + Convert.ToString((short)m_ms.best_score) + ", " + Convert.ToString(m_ms.best_pang) + ", " + Convert.ToString(m_ms.character_typeid) + ", " + Convert.ToString((ushort)m_ms.event_score) + ", " + Convert.ToString((ushort)m_assist));

            checkResponse(r, "nao conseguiu atualizar o record(MapStatistics) dados[COURSE=" + Convert.ToString((ushort)(m_ms.course & 0x7F)) + ", TIPO=" + Convert.ToString((ushort)m_ms.tipo) + ", ASSIST=" + Convert.ToString((ushort)m_assist) + "] do PLAYER[UID=" + Convert.ToString(m_uid) + "]");

            return r;
        }


        private uint m_uid = new uint();
        private byte m_assist;
        private MapStatisticsEx m_ms = new MapStatisticsEx();

        private const string m_szConsulta = "pangya.ProcUpdateMapStatistics";
    }
}
