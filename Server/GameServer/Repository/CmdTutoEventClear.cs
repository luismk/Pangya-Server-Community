using System;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;

namespace Pangya_GameServer.Repository
{
    public class CmdTutoEventClear : Pangya_DB
    {
        public static uint T_ROOKIE = 0;
        public static uint T_BEGINNER = 1;
        public static uint T_ADVANCER = 2;
        public CmdTutoEventClear(bool _waiter = false) : base(_waiter)
        {
            this.m_uid = 0;
            this.m_type = T_ROOKIE;
        }

        public CmdTutoEventClear(uint _uid,
            uint _type,
            bool _waiter = false) : base(_waiter)
        {
            this.m_uid = _uid;
            this.m_type = (_type);
        }

        public uint getUID()
        {
            return (m_uid);
        }

        public void setUID(uint _uid)
        {
            m_uid = _uid;
        }

        public uint getType()
        {
            return m_type;
        }

        public void setType(uint _type)
        {
            m_type = _type;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {

            // N�o usa por que � um UDPATE
            return;
        }

        protected override Response prepareConsulta()
        {

            if (m_uid == 0)
            {
                throw new exception("[CmdTutoEventClear::prepareConsulta][Error] m_uid is invalid(zero)", STDA_MAKE_ERROR(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            var r = _update(m_szConsulta[0] + (m_type == T_ROOKIE ? m_szConsulta[1] : (m_type == T_BEGINNER ? m_szConsulta[2] : m_szConsulta[3])) + m_szConsulta[4] + Convert.ToString(m_uid));

            checkResponse(r, "nao conseguiu atualizar Tutorial Evento[Type=" + Convert.ToString(m_type) + "] do PLAYER[UID=" + Convert.ToString(m_uid) + "]");

            return r;
        }

        private uint m_uid = 0;
        private uint m_type;

        private string[] m_szConsulta = { "UPDATE pangya.account SET ", "Event1 = 1", "Event2 = 1", "Event3 = 1", " WHERE UID =" };
    }
}