using System;
using Pangya_RankingServer.Models;
using Pangya_RankingServer.UTIL;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;

namespace Pangya_RankingServer.Repository
{
    public class CmdUpdateRankRegistry : Pangya_DB
    {
        public CmdUpdateRankRegistry(bool _waiter = false) : base(_waiter)
        {
            this.m_ret_state = 0u;
        }

        public uint getRetState()
        {
            return m_ret_state;
        }

        public DateTime getDate()
        {
            return m_date;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {

            if (_result.cols == 1)
            {
                m_ret_state = IFNULL<uint>(_result.data[0]);
            }
            else if (_result.cols == 2)
            {
                m_ret_state = IFNULL<uint>(_result.data[0]);

                if (_result.data[1] != null)
                {

                    var _date = (DateTime)_translateDate(_result.data[1]);
                    m_date = new DateTime(_date.Year, _date.Month, _date.Day, _date.TimeOfDay.Hours, _date.TimeOfDay.Minutes, _date.TimeOfDay.Seconds, _date.TimeOfDay.Milliseconds);
                }

            }
            else
            {
                checkColumnNumber(1, _result.cols); // S� para enviar a exception, por que a consulta retornou n�mero de colunas inv�lidas
            }
        }

        protected override Response prepareConsulta()
        {

            m_ret_state = 0u;

            var r = procedure(
                m_szConsulta, "");

            checkResponse(r, "Nao conseguiu atualizar os registro do Rank no banco de dados.");

            return r;
        }

        private uint m_ret_state = new uint();
        private DateTime m_date;

        private string m_szConsulta = "pangya.GeraRankAll";
    }
}
