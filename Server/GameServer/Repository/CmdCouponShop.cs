using PangyaAPI.SQL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pangya_GameServer.Repository
{
    public class CmdCouponShop : Pangya_DB
    { 
        public CmdCouponShop(uint _uid,
            int item_id, bool _waiter = false) : base(_waiter)
        {
            this.m_uid = _uid;
            this.m_item_id = item_id;
            this.m_valor = 0;
        }
        
        protected override void lineResult(ctx_res _result, uint _index_result)
        {

            checkColumnNumber(1, _result.cols);
            m_valor = IFNULL(_result.data[0]);
        }
        
        protected override Response prepareConsulta()
        {

            var r = procedure(m_szConsulta, Convert.ToString(m_uid) + ", " + Convert.ToString(m_item_id));

            checkResponse(r, "nao conseguiu pegar o(s) coupon(s) shop do player: " + Convert.ToString(m_uid));

            return r;
        }

        public uint getCouponShop()
        {
            return m_valor;
        }

        private uint m_uid;
        private int m_item_id;
        private uint m_valor;
        private string m_szConsulta = "pangya.VerificaCouponDesconto"; 
    }
}
