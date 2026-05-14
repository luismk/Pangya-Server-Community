using System;
using System.Data;
using Pangya_GameServer.Models;
using PangyaAPI.SQL;
namespace Pangya_GameServer.Repository
{
    public class CmdUpdateDailyQuest : Pangya_DB
    {
        public CmdUpdateDailyQuest(DailyQuestInfo _dqi)
        {
            this.m_dqi = _dqi;
            this.m_updated = false;
        }

        public DailyQuestInfo getInfo()
        {
            return m_dqi;
        }

        public void setInfo(DailyQuestInfo _dqi)
        {

            m_dqi = _dqi;
        }

        public bool isUpdated()
        {
            return m_updated;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {

            checkColumnNumber(5);

            // Update ON DB
            m_updated = IFNULL<bool>(_result.data[0]);
            if (!m_updated)
            { // Não atualizou, pega os valores atualizados do banco de dados

                for (var i = 0; i < 3u; ++i)
                {
                    m_dqi._typeid[i] = IFNULL<uint>(_result.data[1u + i]); 
                }

                if (_result.data[4] != null)
                    m_dqi.date.CreateTime(_translateDate(_result.data[4]));
            }

            return;
        }

        protected override Response prepareConsulta()
        {
            m_updated = false;

            string reg_date = "NULL";

            if (!m_dqi.date.IsEmpty)
            reg_date = makeText(_formatDate(m_dqi.date.ConvertTime()));

            if (m_dqi._typeid.Length < 3)
                throw new InvalidOperationException("m_dqi._typeid deve conter pelo menos 3 elementos."); 

            var r = procedure(m_szConsulta, (m_dqi._typeid[0]) + ", " + (m_dqi._typeid[1])
            + ", " + (m_dqi._typeid[2]) + ", " + reg_date
    );

            checkResponse(r, "Não conseguiu atualizar o sistema de Daily Quest [" + m_dqi + "] no banco de dados.");

            return r;
        }

        private DailyQuestInfo m_dqi = new DailyQuestInfo();
        private bool m_updated; // true atualizou no DB, false, outro j� atualizou pega o valor do DB

        private const string m_szConsulta = "pangya.ProcUpdateDailyQuest";
    }
}
