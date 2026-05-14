using System.Collections.Generic;
using Pangya_GameServer.Models;
using PangyaAPI.SQL;
namespace Pangya_GameServer.Repository
{
    public class CmdPapelShopItem : Pangya_DB
    {
        public CmdPapelShopItem()
        {
            this.m_ctx_psi = new List<ctx_papel_shop_item>();
        }

        public List<ctx_papel_shop_item> getInfo()
        {
            return m_ctx_psi;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {

            checkColumnNumber(5);

            ctx_papel_shop_item ctx_psi = new ctx_papel_shop_item
            {
                _typeid = IFNULL<uint>(_result.data[0]),
                probabilidade = IFNULL<uint>(_result.data[1]),
                numero = IFNULL<int>(_result.data[2]),
                tipo = (PAPEL_SHOP_TYPE)IFNULL<uint>(_result.data[3]),
                active = IFNULL<byte>(_result.data[4])
            };

            m_ctx_psi.Add(ctx_psi);
        }

        protected override Response prepareConsulta()
        {

            var r = consulta(m_szConsulta);

            checkResponse(r, "nao conseguiu pegar os papel shop itens");

            return r;
        }

        private List<ctx_papel_shop_item> m_ctx_psi = new List<ctx_papel_shop_item>();

        private const string m_szConsulta = "SELECT typeid, probabilidade, numero, tipo, active FROM pangya.pangya_papel_shop_item";
    }
}
