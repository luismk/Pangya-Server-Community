//Convertion By LuisMK
using System;
using Pangya_GameServer.Models;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;

namespace Pangya_GameServer.Repository
{
    public class CmdUpdatePremiumTicketTime : Pangya_DB
    {
        public CmdUpdatePremiumTicketTime(uint _uid,
            WarehouseItemEx _wi)
        {

            this.m_uid = _uid;
            //this.
            this.m_wi = (_wi);
        }

        public uint getUID()
        {
            return (m_uid);
        }

        public void setUID(uint _uid)
        {
            m_uid = _uid;

        }

        public WarehouseItemEx getPremiumTicket()
        {
            return m_wi;
        }

        public void setPremiumTicket(WarehouseItemEx _wi)
        {
            m_wi = _wi;
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
                throw new exception("[CmdUpdatePremiumTicketTime::prepareConsulta][Error] m_uid is invalid(zero).", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            if (m_wi.id <= 0)
            {
                throw new exception("[CmdUpdatePremiumTicketTime::prepareConsulta][Error] m_wi.id is invalid[VALUE=" + Convert.ToString(m_wi.id) + "].", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            var r = procedure(m_szConsulta,
                Convert.ToString(m_uid) + ", " + Convert.ToString(m_wi.id) + ", " + Convert.ToString(m_wi.c[3]) + ", " + Convert.ToString(m_wi.c[0]) + ", " + Convert.ToString(m_wi.c[1]) + ", " + Convert.ToString(m_wi.c[2]) + ", " + Convert.ToString(m_wi.c[3]) + ", " + Convert.ToString(m_wi.c[4]));

            checkResponse(r, "nao conseguiu atualizar Premium Ticket Time[ID=" + Convert.ToString(m_wi.id) + ", TEMPO=" + Convert.ToString(m_wi.c[3]) + ", C0=" + Convert.ToString(m_wi.c[0]) + ", C1=" + Convert.ToString(m_wi.c[1]) + ", C2=" + Convert.ToString(m_wi.c[2]) + ", C3=" + Convert.ToString(m_wi.c[3]) + ", C4=" + Convert.ToString(m_wi.c[4]) + "] do PLAYER[UID=" + Convert.ToString(m_uid) + "]");

            return r;
        }


        private uint m_uid = new uint();
        private WarehouseItemEx m_wi = new WarehouseItemEx();

        private const string m_szConsulta = "pangya.ProcUpdatePremiumTicketTime";
    }
}
