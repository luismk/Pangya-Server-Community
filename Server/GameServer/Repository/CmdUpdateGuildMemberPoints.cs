using System;
using Pangya_GameServer.Models;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;

namespace Pangya_GameServer.Repository
{
    public class CmdUpdateGuildMemberPoints : Pangya_DB
    {
        public CmdUpdateGuildMemberPoints()
        {
            this.m_gmp = new GuildMemberPoints();
        }

        public CmdUpdateGuildMemberPoints(GuildMemberPoints _gmp)
        {
            this.m_gmp = (_gmp);
        }

        public GuildMemberPoints getInfo()
        {
            return m_gmp;
        }

        public void setInfo(GuildMemberPoints _gmp)
        {
            m_gmp = _gmp;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {

            // N�o usa por que � um UPDATE
            return;
        }

        protected override Response prepareConsulta()
        {

            if (m_gmp.guild_uid == 0u)
            {
                throw new exception("[CmdUpdateGuildMemberPoints::prepareConsulta][Error] m_gmp.guild_uid is invalid(zero). Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            if (m_gmp.member_uid == 0u)
            {
                throw new exception("[CmdUpdateGuildMemberPoints::prepareConsulta][Error] m_gmp.member_uid is invalid(zero). Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            var r = procedure(m_szConsulta,
                Convert.ToString(m_gmp.guild_uid) + ", " + Convert.ToString(m_gmp.member_uid) + ", " + Convert.ToString(m_gmp.point) + ", " + Convert.ToString(m_gmp.pang));

            checkResponse(r, "nao conseguiu atualizar o Guild[UID=" + Convert.ToString(m_gmp.guild_uid) + "] POINTS[POINT=" + Convert.ToString(m_gmp.point) + ", PANG=" + Convert.ToString(m_gmp.pang) + "] do PLAYER[UID=" + Convert.ToString(m_gmp.member_uid) + "]");

            return r;
        }

        private GuildMemberPoints m_gmp = new GuildMemberPoints();

        private const string m_szConsulta = "pangya.ProcUpdateGuildMemberPoints";
    }
}
