using System;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;

namespace Pangya_GameServer.Repository
{
    internal class CmdDeleteRental : Pangya_DB
    {
        public CmdDeleteRental()
        {
            this.m_uid = 0;
            this.m_item_id = 0;
        }

        public CmdDeleteRental(uint _uid,
            int _item_id)
        {
            this.m_uid = _uid;
            this.m_item_id = _item_id;
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

        public int getItemID()
        {
            return (m_item_id);
        }

        public void setItemID(int _item_id)
        {
            m_item_id = _item_id;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {

            // N�o usa por que � um UPDATE
        }

        protected override Response prepareConsulta()
        {

            if (m_uid == 0)
            {
                throw new exception("[CmdDeleteRental::prepareConsulta][Error] m_uid is invalied(zero)", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            if (m_item_id <= 0)
            {
                throw new exception("[CmdDeleteRental::prepareConsulta][Error] item_id[value=" + Convert.ToString(m_item_id) + "] is invalid", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            var r = _update(m_szConsulta[0] + Convert.ToString(m_uid) + m_szConsulta[1] + Convert.ToString(m_item_id));

            checkResponse(r, "nao conseguiu deletar o Rental Item[ID=" + Convert.ToString(m_item_id) + "] do PLAYER[UID=" + Convert.ToString(m_uid) + "]");

            return r;
        }

        // get Class name

        private uint m_uid = new uint();
        private int m_item_id = new int();

        private string[] m_szConsulta = { "UPDATE pangya.pangya_item_warehouse SET valid = 0 WHERE UID = ", " AND item_id = " };

    }
}