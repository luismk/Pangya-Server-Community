//Convertion By LuisMK
using System;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;

namespace Pangya_GameServer.Repository
{
    internal class CmdTransferPart : Pangya_DB
    {
        public CmdTransferPart(bool _waiter = false) : base(_waiter)
        {
            this.m_uid_sell = 0;
            this.m_uid_buy = 0;
            this.m_item_id = 0;
            this.m_type_iff = 0;
        }

        public CmdTransferPart(uint _uid_sell,
            uint _uid_buy,
            int _item_id,
            byte _type_iff,
            bool _waiter = false) : base(_waiter)
        {
            this.m_uid_sell = _uid_sell;
            this.m_uid_buy = _uid_buy;
            this.m_item_id = _item_id;
            this.m_type_iff = _type_iff;
        }

        public uint getUIDSell()
        {
            return (m_uid_sell);
        }

        public void setUIDSell(uint _uid_sell)
        {
            m_uid_sell = _uid_sell;
        }

        public uint getUIDBuy()
        {
            return (m_uid_buy);
        }

        public void setUIDBuy(uint _uid_buy)
        {
            m_uid_buy = _uid_buy;
        }

        public int getItemID()
        {
            return (m_item_id);
        }

        public void setItemID(int _item_id)
        {
            m_item_id = _item_id;
        }

        public byte getTypeIFF()
        {
            return m_type_iff;
        }

        public void setTypeIFF(byte _type_iff)
        {
            m_type_iff = _type_iff;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {

            // N�o usa aqui por que � um UPDATE
            return;
        }

        protected override Response prepareConsulta()
        {

            if (m_uid_sell == 0 || m_uid_buy == 0)
            {
                throw new exception("[CmdTransferPart::prepareConsulta][Error] player_s[UID=" + Convert.ToString(m_uid_sell) + "] or player_r[UID=" + Convert.ToString(m_uid_buy) + "] is invalid", STDA_MAKE_ERROR(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            if (m_item_id <= 0)
            {
                throw new exception("[CmdTransferPart::prepareConsulta][Error] item[ID=" + Convert.ToString(m_item_id) + "] is invalid", STDA_MAKE_ERROR(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            var r = procedure(m_szConsulta,
                Convert.ToString(m_uid_sell) + ", " + Convert.ToString(m_uid_buy) + ", " + Convert.ToString(m_item_id) + ", " + Convert.ToString(m_type_iff));

            checkResponse(r, "nao conseguiu transferir o item[ID=" + Convert.ToString(m_item_id) + "] do PLAYER[UID=" + Convert.ToString(m_uid_sell) + "] para o PLAYER[UID=" + Convert.ToString(m_uid_buy) + "]");

            return r;
        }


        private uint m_uid_sell = new uint();
        private uint m_uid_buy = new uint();
        private int m_item_id = new int();
        private byte m_type_iff;

        private const string m_szConsulta = "pangya.ProcTransferPart";
    }
}