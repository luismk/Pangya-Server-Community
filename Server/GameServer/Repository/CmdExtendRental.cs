using System;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;

namespace Pangya_GameServer.Repository
{
    public class CmdExtendRental : Pangya_DB
    {
        public CmdExtendRental()
        {
            this.m_uid = 0;
            this.m_item_id = 0;
            this.m_date = "";
        }
        public CmdExtendRental(uint _uid,
            int _item_id, string _date
            )
        {
            this.m_uid = _uid;
            this.m_item_id = _item_id;
            this.m_date = _date;
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

        public string getDate()
        {
            return m_date;
        }

        public void setDate(string _date)
        {
            m_date = _date;
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
                throw new exception("[CmdExtendRental::prepareConsulta][Error] m_uid is invalid(zero)", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            if (m_item_id <= 0)
            {
                throw new exception("[CmdExtendRental::prepareConsulta][Error] m_item_id[value=" + Convert.ToString(m_item_id) + "] is invalid", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            if (m_date.Length == 0)
            {
                throw new exception("[CmdExtendRental::prepareConsulta][Error] m_date is empty", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            var r = procedure(m_szConsulta,
                Convert.ToString(m_uid) + ", " + Convert.ToString(m_item_id) + ", " + (m_date));

            checkResponse(r, "nao conseguiu extender o Part Rental[ID=" + Convert.ToString(m_item_id) + "] do PLAYER[UID=" + Convert.ToString(m_uid) + "]");

            return r;
        }

        private uint m_uid = new uint();
        private int m_item_id = new int();
        private string m_date = "";

        private const string m_szConsulta = "pangya.ProcExtendRental";
    }
}