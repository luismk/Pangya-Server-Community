using System;
using Pangya_GameServer.Models;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;

namespace Pangya_GameServer.Repository
{
    public class CmdInsertTicketReportData : Pangya_DB
    {
        public CmdInsertTicketReportData(bool _waiter = false) : base(_waiter)
        {
            this.m_id = -1;
            this.m_trd = new TicketReportInfo.stTicketReportDados();
        }

        public CmdInsertTicketReportData(int _id,
            TicketReportInfo.stTicketReportDados _trd,
            bool _waiter = false) : base(_waiter)
        {
            this.m_id = _id;
            this.m_trd = _trd;
        }

        public int getId()
        {
            return m_id;
        }

        public void setId(int _id)
        {
            m_id = _id;
        }

        public TicketReportInfo.stTicketReportDados getInfo()
        {
            return m_trd;
        }

        public void setInfo(TicketReportInfo.stTicketReportDados _trd)
        {
            m_trd = _trd;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {

            // N�o usa por que � um UPDATE
            return;
        }

        protected override Response prepareConsulta()
        {

            if (m_id < 0)
            {
                throw new exception("[CmdInsertTicketReportData::prepareConsulta][Error] m_id is invalid[VALUE=" + Convert.ToString(m_id) + "]", STDA_MAKE_ERROR(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            string finish_date = "null";

            if (m_trd.finish_time.IsEmpty == false)
            {
                finish_date = makeText(_formatDate(m_trd.finish_time.ConvertTime()));
            }

            var r = procedure(m_szConsulta,
                Convert.ToString(m_id) + ", " + Convert.ToString(m_trd.uid) + ", " + Convert.ToString(m_trd.score) + ", " + Convert.ToString((ushort)m_trd.medal.ucMedal) + ", " + Convert.ToString((ushort)m_trd.trofel) + ", " + Convert.ToString(m_trd.pang) + ", " + Convert.ToString(m_trd.bonus_pang) + ", " + Convert.ToString(m_trd.exp) + ", " + Convert.ToString(m_trd.mascot_typeid) + ", " + Convert.ToString((ushort)m_trd.flag_item_pang) + ", " + Convert.ToString((ushort)m_trd.premium) + ", " + Convert.ToString(m_trd.state) + ", " + finish_date);

            checkResponse(r, "nao conseguiu inserir Ticket Report[ID=" + Convert.ToString(m_id) + "] Dados[UID=" + Convert.ToString(m_trd.uid) + ", SCORE=" + Convert.ToString(m_trd.score) + ", MEDAL=" + Convert.ToString((ushort)m_trd.medal.ucMedal) + ", TROFEL=" + Convert.ToString((ushort)m_trd.trofel) + ", PANG=" + Convert.ToString(m_trd.pang) + ", BONUS_PANG=" + Convert.ToString(m_trd.bonus_pang) + ", EXP=" + Convert.ToString(m_trd.exp) + ", MASCOT=" + Convert.ToString(m_trd.mascot_typeid) + ", BOOST_PANG=" + Convert.ToString((ushort)m_trd.flag_item_pang) + ", PREMIUM=" + Convert.ToString((ushort)m_trd.premium) + ", STATE=" + Convert.ToString(m_trd.state) + ", FINISH_DATE=" + finish_date + "]");

            return r;
        }

        private int m_id = new int();
        private TicketReportInfo.stTicketReportDados m_trd = new TicketReportInfo.stTicketReportDados();

        private const string m_szConsulta = "pangya.ProcInsertTicketReportDados";
    }
}