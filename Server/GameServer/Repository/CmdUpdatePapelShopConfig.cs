using System;
using Pangya_GameServer.Models;
using PangyaAPI.SQL;
namespace Pangya_GameServer.Repository
{
    public class CmdUpdatePapelShopConfig : Pangya_DB
    {
        public CmdUpdatePapelShopConfig(ctx_papel_shop _ps)
        {
            this.m_ps = (_ps);
            this.m_updated = false;
        }

        public ctx_papel_shop getInfo()
        {
            return m_ps;
        }

        public void setInfo(ctx_papel_shop _ps)
        {
            m_ps = _ps;
        }

        public bool isUpdated()
        {
            return m_updated;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {

            checkColumnNumber(6);

            // Update ON DB
            m_updated = IFNULL(_result.data[0]) == 1 ? true : false;

            if (!m_updated)
            { // Não atualizou, pega os valores atualizados do banco de dados

                m_ps.numero = IFNULL(_result.data[1]);
                m_ps.price_normal = IFNULL(_result.data[2]);
                m_ps.price_big = IFNULL(_result.data[3]);
                m_ps.limitted_per_day = (byte)IFNULL(_result.data[4]);
                if (_result.IsNotNull(5))

                    m_ps.update_date.CreateTime(_translateDate(_result.data[5]));
            }

            return;
        }

        protected override Response prepareConsulta()
        {

            m_updated = false;

            string upt_dt = "null";

            if (!m_ps.update_date.IsEmpty)
                upt_dt = makeText(_formatDate(m_ps.update_date.ConvertTime()));

            var r = procedure(m_szConsulta,
                Convert.ToString(m_ps.numero) + ", " + Convert.ToString(m_ps.price_normal) + ", " + Convert.ToString(m_ps.price_big) + ", " + Convert.ToString((ushort)m_ps.limitted_per_day) + ", " + upt_dt);

            checkResponse(r, "nao conseguiu atualizar o Papel Shop Config[" + m_ps.toString() + "]");

            return r;
        }

        private ctx_papel_shop m_ps = new ctx_papel_shop();
        private bool m_updated;

        private const string m_szConsulta = "pangya.ProcUpdatePapelShopConfig";
    }
}
