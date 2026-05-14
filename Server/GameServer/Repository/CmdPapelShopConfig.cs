using Pangya_GameServer.Models;
using PangyaAPI.SQL;
namespace Pangya_GameServer.Repository
{
    public class CmdPapelShopConfig : Pangya_DB
    {
        public CmdPapelShopConfig()
        {
            this.m_ctx_ps = new ctx_papel_shop();
        }


        public ctx_papel_shop getInfo()
        {
            return m_ctx_ps;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {

            checkColumnNumber(5);

            m_ctx_ps.numero = IFNULL<uint>(_result.data[0]);
            m_ctx_ps.price_normal = IFNULL<ulong>(_result.data[1]);
            m_ctx_ps.price_big = IFNULL<ulong>(_result.data[2]);
            m_ctx_ps.limitted_per_day = (byte)IFNULL<uint>(_result.data[3]);

            if (_result.IsNotNull(4))
            {
                m_ctx_ps.update_date.CreateTime(_translateDate(_result.data[4]));
            }
        }

        protected override Response prepareConsulta()
        {

            var r = consulta(m_szConsulta);

            checkResponse(r, "nao conseguiu pegar o papel shop config.");

            return r;
        }

        private ctx_papel_shop m_ctx_ps = new ctx_papel_shop();

        private const string m_szConsulta = "SELECT Numero, Price_Normal, Price_Big, Limitted_YN, Update_Date FROM pangya.pangya_papel_shop_config";
    }
}
