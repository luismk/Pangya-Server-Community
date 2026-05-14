//Convertion By LuisMK
using System;
using Pangya_GameServer.Models;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;
namespace Pangya_GameServer.Repository
{
    public class CmdItemBuyShopLog : Pangya_DB
    {
        public CmdItemBuyShopLog(bool _waiter = false) : base(_waiter)
        { 
            this.m_uid = 0; 
            this.m_buy_shop = new BuyItem();
        }

        public CmdItemBuyShopLog(uint _buy_uid,
            BuyItem _psi, 
            bool _waiter = false) : base(_waiter)
        {

             this.m_uid = _buy_uid;
            this.m_buy_shop = (_psi);
        }

       
        public uint getUIDBuy()
        {
            return (m_uid);
        }

        public void setUIDBuy(uint _buy_uid)
        {

            m_uid = _buy_uid;
        }

         
        protected override void lineResult(ctx_res _result, uint _index_result)
        {

            // N�o vai usar por que � um INSERT
        }

        protected override Response prepareConsulta()
        {

            if (m_uid == 0)
            {
                throw new exception("[CmdItemBuyShopLog::prepareConsulta][Error] m_uid_buy[value=" + Convert.ToString(m_uid) + "] is invalid", STDA_MAKE_ERROR(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            var r = procedure(m_szConsulta, Convert.ToString(m_uid) + ", " + Convert.ToString(m_buy_shop._typeid) + ", " + Convert.ToString(m_buy_shop.id) + ", " + Convert.ToString(m_buy_shop.time) + ", " + Convert.ToString(m_buy_shop.ItemType) + ", " + Convert.ToString(m_buy_shop.qntd) + ", " + Convert.ToString(m_buy_shop.pang) + ", " + Convert.ToString(m_buy_shop.cookie));

            checkResponse(r, "nao conseguiu inserir log so Buy shop[UID_BUY=" + Convert.ToString(m_uid) + ", ITEM_TYPEID=" + Convert.ToString(m_buy_shop._typeid) + ", ITEM_ID_SELL=" + Convert.ToString(m_buy_shop.id) + ", ITEM_QNTD=" + Convert.ToString(m_buy_shop.qntd) + ", ITEM_PANG=" + Convert.ToString(m_buy_shop.pang) + ", TOTAL_PANG=" + Convert.ToString((ulong)m_buy_shop.qntd * m_buy_shop.pang) + "]");

            return r;
        }
         
        private uint m_uid = new uint(); 
        private BuyItem m_buy_shop = new BuyItem();

        private const string m_szConsulta = "pangya.ProcInsertBuyShopLog";
    }
}