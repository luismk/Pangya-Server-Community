using System;
using Pangya_GameServer.Models;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;

namespace Pangya_GameServer.Repository
{
    public class CmdUpdateLoginRewardPlayer : Pangya_DB
    {

        public CmdUpdateLoginRewardPlayer(stPlayerState _ps)
        {
            this.m_ps = (_ps);
        }

        public CmdUpdateLoginRewardPlayer()
        {
            this.m_ps = new stPlayerState(0u);
        }

        public virtual void Dispose()
        {
        }

        public stPlayerState getPlayerState()
        {
            return m_ps;
        }

        public void setPlayerState(stPlayerState _ps)
        {
            m_ps = _ps;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {

            // N�o usa por que � um UPDATE
            return;
        }

        protected override Response prepareConsulta()
        {

            if (m_ps.id == 0Ul)
            {
                throw new exception("[CmdUpdateLoginRewardPlayer::prepareConsulta][Error] m_ps.id is invalid(" + Convert.ToString(m_ps.id) + ")", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            if (m_ps.uid == 0u)
            {
                throw new exception("[CmdUpdateLoginRewardPlayer::prepareConsulta][Error] m_ps.uid is invalid(" + Convert.ToString(m_ps.uid) + ")", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            var r = procedure(m_szConsulta,
                Convert.ToString(m_ps.id) + ", " + Convert.ToString(m_ps.uid) + ", " + Convert.ToString(m_ps.count_days) + ", " + Convert.ToString(m_ps.count_seq) + ", " + (m_ps.is_clear ? "1" : "0") + ", " + makeText(_formatDate(m_ps.update_date.ConvertTime())));

            checkResponse(r, "nao conseguiu atualizar o PLAYER[" + m_ps.toString() + "]");

            return r;
        }

        private stPlayerState m_ps = new stPlayerState();

        private const string m_szConsulta = "pangya.procUpdateLoginRewardPlayer";
    }
}