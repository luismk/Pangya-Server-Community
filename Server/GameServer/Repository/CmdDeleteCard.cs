//Convertion By LuisMK
using System;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;

namespace Pangya_GameServer.Repository
{
    public class CmdDeleteCard : Pangya_DB
    {
        public CmdDeleteCard(bool _waiter = false) : base(_waiter)
        {
            this.m_uid = 0;
            this.m_id = -1;
        }

        public CmdDeleteCard(uint _uid,
            int _id,
            bool _waiter = false) : base(_waiter)
        {
            this.m_uid = _uid;
            this.m_id = _id;
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

        public int getID()
        {
            return (m_id);
        }

        public void setID(int _id)
        {
            m_id = _id;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {

            // N�o usa por que � um UPDATE
            return;
        }

        protected override Response prepareConsulta()
        {

            if (m_id <= 0)
            {
                throw new exception("[CmdDeleteCard::prepareConsulta][Error] Card id[value=" + Convert.ToString(m_id) + "] is invalid", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            var r = _update(m_szConsulta[0] + m_szConsulta[1] + Convert.ToString(m_uid) + m_szConsulta[2] + Convert.ToString(m_id));

            checkResponse(r, "nao conseguiu deletar Card[ID=" + Convert.ToString(m_id) + "] do PLAYER[UID=" + Convert.ToString(m_uid) + "]");

            return r;
        }

        private uint m_uid = new uint();
        private int m_id = new int();

        private string[] m_szConsulta = { "UPDATE pangya.pangya_card SET QNTD = 0, USE_YN = 0 ", " WHERE UID = ", " AND card_itemid = " };
    }
}