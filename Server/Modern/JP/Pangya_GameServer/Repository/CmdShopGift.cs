using System;
using System.Collections.Generic;
using Pangya_GameServer.Models;
using PangyaAPI.DataBase;

namespace Pangya_GameServer.Repository
{
    public class CmdShopGift : Pangya_DB
    {
        List<ShopGift> m_shop_gift;

        public CmdShopGift()
        {
            m_shop_gift = new List<ShopGift>();
        }
        protected override void lineResult(ctx_res _result, uint _index_result)
        {
            checkColumnNumber(9);
            try
            {
                var m_config = new ShopGift();

                m_config.gift_id = IFNULL<int>(_result.data[0]);
                if (is_valid_c_string(_result.data[1]))
                    m_config.item_title = _result.GetString(1);

                if (is_valid_c_string(_result.data[2]))
                    m_config.item_name = _result.GetString(2);

                m_config.item_typeid = IFNULL<int>(_result.data[3]);
                m_config.item_qntd = IFNULL<int>(_result.data[4]);
                m_config.item_qntd_item = IFNULL<int>(_result.data[5]);
                m_config.item_period = IFNULL<int>(_result.data[6]);//depois ver algo melhor!
                m_config.required_price = IFNULL<ulong>(_result.data[7]);
                if (_result.data[8] != DBNull.Value)
                    m_config.end_date.CreateTime(_translateDate(_result.data[8]));

                m_shop_gift.Add(m_config);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);

            }
        }

        protected override Response prepareConsulta()
        {
            var str = makeEscapeKeyword("gift_id") + ", " + makeEscapeKeyword("gift_name") + ", " + makeEscapeKeyword("item_name") + ", " + makeEscapeKeyword("item_typeid") + ", " + makeEscapeKeyword("item_qntd") + ", " + makeEscapeKeyword("item_qntd_time") + ", " + makeEscapeKeyword("item_period") + ", " + makeEscapeKeyword("required_price") + ", " + makeEscapeKeyword("end_date") + "FROM" + makeEscapeKeyword("pangya") + "." + makeEscapeKeyword("pangya_shop_gift");
            var r = consulta("SELECT " + str);
            checkResponse(r, "nao conseguiu pegar as info gift ShopRoom");
            return r;
        }


        public List<ShopGift> getInfo()
        {
            return m_shop_gift;
        }

    }
}