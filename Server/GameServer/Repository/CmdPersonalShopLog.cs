//Convertion By LuisMK
using System;
using Pangya_GameServer.Models;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;
namespace Pangya_GameServer.Repository
{
    public class CmdPersonalShopLog : Pangya_DB
    {
        public CmdPersonalShopLog(bool _waiter = false) : base(_waiter)
        {
            this.m_uid_sell = 0;
            this.m_uid_buy = 0;
            this.m_item_id_buy = 0;
            this.m_psi = new PersonalShopItem();
        }

        public CmdPersonalShopLog(uint _sell_uid,
            uint _buy_uid,
            PersonalShopItem _psi,
            int _item_id_buy,
            bool _waiter = false) : base(_waiter)
        {

            this.m_uid_sell = _sell_uid;
            this.m_uid_buy = _buy_uid;
            this.m_item_id_buy = _item_id_buy;
            this.m_psi = (_psi);
        }

        public uint getUIDSell()
        {
            return (m_uid_sell);
        }

        public void setUIDSell(uint _sell_uid)
        {
            m_uid_sell = _sell_uid;
        }

        public uint getUIDBuy()
        {
            return (m_uid_buy);
        }

        public void setUIDBuy(uint _buy_uid)
        {

            m_uid_buy = _buy_uid;
        }

        public int getItemIDBuy()
        {
            return (m_item_id_buy);
        }

        public void setItemIDBuy(int _item_id_buy)
        {
            m_item_id_buy = _item_id_buy;
        }

        public PersonalShopItem getItemSell()
        {
            return m_psi;
        }

        public void setItemSell(PersonalShopItem _psi)
        {
            m_psi = _psi;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {

            // N�o vai usar por que � um INSERT
        }

        protected override Response prepareConsulta()
        {

            if (m_uid_sell == 0 || m_uid_buy == 0)
            {
                throw new exception("[CmdPersonalShopLog::prepareConsulta][Error] m_uid_sell[value=" + Convert.ToString(m_uid_sell) + "] or m_uid_buy[value=" + Convert.ToString(m_uid_buy) + "] is invalid", STDA_MAKE_ERROR(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            var r = procedure(m_szConsulta,
                Convert.ToString(m_uid_sell) + ", " + Convert.ToString(m_uid_buy) + ", " + Convert.ToString(m_psi.item._typeid) + ", " + Convert.ToString(m_psi.item.id) + ", " + Convert.ToString(m_item_id_buy) + ", " + Convert.ToString(m_psi.item.qntd) + ", " + Convert.ToString(m_psi.item.pang) + ", " + Convert.ToString((ulong)m_psi.item.qntd * m_psi.item.pang));

            checkResponse(r, "nao conseguiu inserir log so personal shop[UID_SELL=" + Convert.ToString(m_uid_sell) + ", UID_BUY=" + Convert.ToString(m_uid_buy) + ", ITEM_TYPEID=" + Convert.ToString(m_psi.item._typeid) + ", ITEM_ID_SELL=" + Convert.ToString(m_psi.item.id) + ", ITEM_ID_BUY=" + Convert.ToString(m_item_id_buy) + ", ITEM_QNTD=" + Convert.ToString(m_psi.item.qntd) + ", ITEM_PANG=" + Convert.ToString(m_psi.item.pang) + ", TOTAL_PANG=" + Convert.ToString((ulong)m_psi.item.qntd * m_psi.item.pang) + "]");

            return r;
        }

        private uint m_uid_sell = new uint();
        private uint m_uid_buy = new uint();
        private int m_item_id_buy = new int();
        private PersonalShopItem m_psi = new PersonalShopItem();

        private const string m_szConsulta = "pangya.ProcInsertPersonalShopLog";
    }
}