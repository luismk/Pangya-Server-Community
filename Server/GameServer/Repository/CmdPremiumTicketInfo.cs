using System;
using Pangya_GameServer.Models;
using PangyaAPI.SQL;

namespace Pangya_GameServer.Repository
{
    public class CmdPremiumTicketInfo : Pangya_DB
    {
        public CmdPremiumTicketInfo()
        {
            this.m_uid = 0;
            this.m_pt = new PremiumTicket();
        }

        public CmdPremiumTicketInfo(uint _uid)
        {
            this.m_uid = _uid;
            this.m_pt = new PremiumTicket();
        }

        public PremiumTicket getInfo()
        {
            return m_pt;
        }

        public void setInfo(PremiumTicket _pt)
        {
            m_pt = _pt;
        }

        public uint getUID()
        {
            return m_uid;
        }

        public void setUID(uint _uid)
        {
            m_uid = _uid;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {

            checkColumnNumber(4);

            m_pt.id = IFNULL<int>(_result.data[0]);
            m_pt._typeid = IFNULL(_result.data[1]);
            m_pt.unix_end_date = IFNULL<int>(_result.data[2]);
            m_pt.unix_sec_date = IFNULL<int>(_result.data[3]);
        }

        protected override Response prepareConsulta()
        {

            var r = procedure(m_szConsulta,
                Convert.ToString(m_uid));

            checkResponse(r, "nao conseguiu pegar premium ticket info do player: " + Convert.ToString(m_uid));

            return r;
        }

        private uint m_uid = 0;
        private PremiumTicket m_pt = new PremiumTicket();

        private const string m_szConsulta = "pangya.ProcGetPremiumTicket";
    }
}