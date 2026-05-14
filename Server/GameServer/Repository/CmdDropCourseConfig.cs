using Pangya_GameServer.Game.System;
using PangyaAPI.SQL;

namespace Pangya_GameServer.Repository
{
    public class CmdDropCourseConfig : Pangya_DB
    {
        public CmdDropCourseConfig()
        {
            this.m_config = new DropSystem.stConfig();
        }

        public virtual void Dispose()
        {
        }

        public DropSystem.stConfig getConfig()
        {
            return m_config;
        }
        protected override void lineResult(ctx_res _result, uint _index_result)
        {
            checkColumnNumber(3, (uint)_result.cols);

            m_config.rate_mana_artefact = IFNULL(_result.data[0]);
            m_config.rate_grand_prix_ticket = IFNULL(_result.data[1]);
            m_config.rate_SSC_ticket = IFNULL(_result.data[2]);
        }

        protected override Response prepareConsulta()
        {

            m_config.clear();

            var r = procedure(m_szConsulta, "");

            checkResponse(r, "nao consiguiu pegar o Drop Course Config");

            return r;
        }


        private DropSystem.stConfig m_config = new DropSystem.stConfig();

        private const string m_szConsulta = "pangya.ProcGetDropCourseConfig";
    }
}