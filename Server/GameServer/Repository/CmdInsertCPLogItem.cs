using System;
using Pangya_GameServer.Models;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;

namespace Pangya_GameServer.Repository
{
    public class CmdInsertCPLogItem : Pangya_DB
    {
        public CmdInsertCPLogItem(bool _waiter = false) : base(_waiter)
        {
            this.m_uid = 0;
            this.m_log_id = -1L;
            this.m_item_id = -1L;
            this.m_item = new CPLog.stItem(0);
        }

        public CmdInsertCPLogItem(uint _uid,
            long _log_id,
            CPLog.stItem _item,
            bool _waiter = false) : base(_waiter)
        {
            this.m_uid = _uid;
            this.m_log_id = _log_id;
            this.m_item = (_item);
            this.m_item_id = -1L;
        }

        public uint getUID()
        {
            return (m_uid);
        }

        public void setUID(uint _uid)
        {
            m_uid = _uid;
        }

        public long getLogId()
        {
            return (m_log_id);
        }

        public void setLogId(long _log_id)
        {
            m_log_id = _log_id;
        }

        public CPLog.stItem getItem()
        {
            return (m_item);
        }

        public void setItem(CPLog.stItem _item)
        {
            m_item = _item;
        }

        public long getItemId()
        {
            return (m_item_id);
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {

            checkColumnNumber(1, (uint)_result.cols);

            m_item_id = IFNULL(_result.data[0]);
        }

        protected override Response prepareConsulta()
        {

            if (m_log_id <= 0L)
            {
                throw new exception("[CmdInsertCPLogItem::prepareConsulta][Error] m_log_id[VALUE=" + Convert.ToString(m_log_id) + "] is invalid", STDA_MAKE_ERROR(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            m_item_id = -1L;

            var r = procedure(m_szConsulta,
                Convert.ToString(m_log_id) + ", " + Convert.ToString(m_item._typeid) + ", " + Convert.ToString(m_item.qntd) + ", " + Convert.ToString(m_item.price));

            checkResponse(r, "nao conseguiu inserir CPLogItem[LOD_ID=" + Convert.ToString(m_log_id) + ", ITEM_TYPEID=" + Convert.ToString(m_item._typeid) + ", ITEM_QNTD=" + Convert.ToString(m_item.qntd) + ", ITEM_PRICE=" + Convert.ToString(m_item.price) + "] do PLAYER[UID=" + Convert.ToString(m_uid) + "]");

            return r;
        }


        private uint m_uid = new uint(); // Para fins de log, ele não utiliza na proc do DB
        private long m_log_id = new long();
        private long m_item_id = new long();
        private CPLog.stItem m_item = new CPLog.stItem();

        private const string m_szConsulta = "pangya.ProcInsertCPLogItem";
    }
}