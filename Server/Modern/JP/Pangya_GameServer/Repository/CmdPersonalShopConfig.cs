//Convertion By LuisMK
using System;
using System.Collections.Generic;
using Pangya_GameServer.Models;
using PangyaAPI.DataBase; 
namespace Pangya_GameServer.Repository
{
    public class CmdPersonalShopConfig : Pangya_DB
    {
        public CmdPersonalShopConfig(bool _waiter)
        {
            m_ctx_ps = new List<ctx_personal_shop>();
        }

        ~CmdPersonalShopConfig()
        {
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {
            checkColumnNumber(5, _result.cols);

            var m_config = new ctx_personal_shop
            {
                index = IFNULL<uint>(_result.data[0]),
                // Name = IFNULL_Long(_result.data[1]), // descomente se necessário
                id = IFNULL<uint>(_result.data[2]),
                price = IFNULL<uint>(_result.data[3])
            };

            if (_result.data[4] != null)
            {
                m_config.reg_date = (DateTime)_translateDate(_result.data[4]);
            }

            m_ctx_ps.Add(m_config);
        }

        protected override Response prepareConsulta()
        {
            var r = consulta(m_szConsulta);

            checkResponse(r, "nao conseguiu pegar o personal ShopRoom config.");

            return r;
        }

        public uint getPrice(uint id)
        {
            for (int i = 0; i < m_ctx_ps.Count; i++)
            {
                if (m_ctx_ps[i].id == id)
                    return m_ctx_ps[i].price;
            } 
            return 1;
        }


        List<ctx_personal_shop> m_ctx_ps;
         

        private const string m_szConsulta = "SELECT [Index], [Name], [ID], [Price],[reg_date] FROM [pangya].[pangya_personal_shop_config]";

    }
}