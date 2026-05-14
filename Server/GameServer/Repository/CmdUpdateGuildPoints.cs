using System;
using Pangya_GameServer.Models;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;
namespace Pangya_GameServer.Repository
{
    public class CmdUpdateGuildPoints : Pangya_DB
    {
        public CmdUpdateGuildPoints()
        {
            this.m_gp = new GuildPoints();
        }

        public CmdUpdateGuildPoints(GuildPoints _gp)
        {
            this.m_gp = (_gp);
        }

        public virtual void Dispose()
        {
        }

        public GuildPoints getInfo()
        {
            // 
            return m_gp;
        }

        public void setInfo(GuildPoints _gp)
        {
            m_gp = _gp;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {

            // N�o usa por que � um UPDATE
            return;
        }

        protected override Response prepareConsulta()
        {

            if (m_gp.uid == 0u)
            {
                throw new exception("[CmdUpdateGuildPoints::prepareConsulta][Error] m_gp.uid is invalid(zero). Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            var r = procedure(m_szConsulta,
                Convert.ToString(m_gp.uid) + ", " + Convert.ToString(m_gp.point) + ", " + Convert.ToString(m_gp.pang) + ", " + Convert.ToString((ushort)m_gp.win));

            checkResponse(r, "nao conseguiu atualizar os Pontos[POINT=" + Convert.ToString(m_gp.point) + ", PANG=" + Convert.ToString(m_gp.pang) + "] da Guild[UID=" + Convert.ToString(m_gp.uid) + ", WIN=" + Convert.ToString((ushort)m_gp.win) + "]");

            return r;
        }


        private GuildPoints m_gp = new GuildPoints();

        private const string m_szConsulta = "pangya.ProcUpdateGuildPoints";
    }
}
