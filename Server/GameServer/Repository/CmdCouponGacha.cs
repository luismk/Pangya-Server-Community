using System;
using Pangya_GameServer.Models;
using PangyaAPI.SQL;

namespace Pangya_GameServer.Repository
{
    public class CmdCouponGacha : Pangya_DB
    {
        readonly uint m_uid = uint.MaxValue;
        CouponGacha m_cg = new CouponGacha();

        public CmdCouponGacha(uint _uid)
        {
            m_uid = _uid;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {
            checkColumnNumber(1);
            try
            {

                if (_index_result == 0)
                    m_cg.normal_ticket = Convert.ToInt32(_result.data[0]);
                else if (_index_result == 1)
                    m_cg.partial_ticket = Convert.ToInt32(_result.data[0]);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);

            }
        }

     
	 protected override Response prepareConsulta()
{ 
    string query1 = $"{m_szConsulta[0]}{m_uid}";
    string query2 = $"{m_szConsulta[1]}{m_uid}";

    string fullQuery = $"{query1}; {query2}";

    // 4. Execute and Validate
    var r = consulta(fullQuery);

    checkResponse(r, $"Não foi possível buscar os cupons gacha do player: {m_uid}");

    return r;
}
        private string[] m_szConsulta = { "SELECT c0 FROM pangya.pangya_item_warehouse WHERE typeid = 436207744 AND uid = ", "SELECT c0 FROM pangya.pangya_item_warehouse WHERE typeid = 436207747 AND uid = " };



        public CouponGacha getCouponGacha()
        {
            return m_cg;
        }

        public void getCouponGacha(CouponGacha cg)
        {
            m_cg = cg;
        }
		
    }
}
