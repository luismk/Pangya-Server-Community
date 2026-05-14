using PangyaAPI.SQL;


namespace PangyaAPI.Network.Repository
{
    public class CmdAddItemBase : Pangya_DB
    {
        public uint m_uid = 0;
        public byte m_purchase;
        public byte m_gift_flag;
        public CmdAddItemBase(uint _uid, byte _purchase, byte _gift_flag)
        {
            m_purchase = _purchase;
            m_gift_flag = _gift_flag;
            m_uid = _uid;
        }

        public uint getUID()
        {
            return m_uid;
        }

        public void setUID(uint _uid)
        {
            m_uid = _uid;
        }

        public byte getPurchase()
        {
            return m_purchase;
        }

        public void setPurchase(byte _purchase)
        {
            m_purchase = _purchase;
        }

        public byte getGiftFlag()
        {
            return m_gift_flag;
        }

        public void setGiftFlag(byte _gift_flag)
        {
            m_gift_flag = _gift_flag;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {
        }

        protected override Response prepareConsulta()
        {
            return null;
        }
    }
}
