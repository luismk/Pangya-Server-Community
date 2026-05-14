using System;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;

namespace Pangya_GameServer.Repository
{
    public class CmdInsertTicketReport : Pangya_DB
    {
        public CmdInsertTicketReport(bool _waiter = false) : base(_waiter)
        {
            this.m_id = -1;
            this.m_trofel = 0;
            this.m_type = 0;
        }

        public CmdInsertTicketReport(uint _trofel,
            byte _type,
            bool _waiter = false) : base(_waiter)
        {
            this.m_id = -1;
            this.m_trofel = _trofel;
            this.m_type = _type;
        }

        public uint getTrofel()
        {
            return (m_trofel);
        }

        public void setTrofel(uint _trofel)
        {
            m_trofel = _trofel;
        }

        public byte getType()
        {
            return m_type;
        }

        public void setType(byte _type)
        {
            m_type = _type;
        }

        public int getId()
        {
            return (m_id);
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {

            checkColumnNumber(1);

            m_id = IFNULL<int>(_result.data[0]);

            if (m_id == -1)
            {
                throw new exception("[CmdInsertTicketReport::lineResult][Error] nao conseguiu inserir um Ticket Report[TROFEL=" + Convert.ToString(m_trofel) + ", TYPE=" + Convert.ToString((ushort)m_type) + "] no banco de dados, ele retornou um id == -1", STDA_MAKE_ERROR(STDA_ERROR_TYPE.PANGYA_DB,
                    3, 0));
            }
        }

        protected override Response prepareConsulta()
        {

            m_id = -1;

            var r = procedure(m_szConsulta,
                Convert.ToString(m_trofel) + ", " + Convert.ToString((ushort)m_type));

            checkResponse(r, "nao conseguiu inserir um Ticket Report[TROFEL=" + Convert.ToString(m_trofel) + ", TYPE=" + Convert.ToString((ushort)m_type) + "] no banco de dados");

            return r;
        }

        private int m_id = -1;
        private uint m_trofel = 0;
        private byte m_type;

        private const string m_szConsulta = "pangya.ProcInsertNewTicketReport";
    }
}