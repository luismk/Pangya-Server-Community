using System;
using Pangya_GameServer.Models;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;

namespace Pangya_GameServer.Repository
{
    public class CmdAddLoginRewardPlayer : Pangya_DB
    {

        public CmdAddLoginRewardPlayer(ulong _id,
            stPlayerState _ps)
        {
            this.m_id = _id;
            this.m_ps = (_ps);
        }

        public CmdAddLoginRewardPlayer()
        {
            this.m_id = 0Ul;
            this.m_ps = new stPlayerState(0u);
        }

        public virtual void Dispose()
        {
        }

        public ulong getId()
        {
            return (m_id);
        }

        public void setId(ulong _id)
        {
            m_id = _id;
        }

        public stPlayerState getPlayerState()
        {
            return m_ps;
        }

        public void setPlayerState(stPlayerState _ps)
        {
            m_ps = _ps;
        }

        public bool isGood()
        {
            return m_ps.id != 0Ul;
        }
        protected override void lineResult(ctx_res _result, uint _index_result)
        {

            checkColumnNumber(1);
            m_ps.id = (ulong)IFNULL(_result.data[0]);

            if (m_ps.id == 0Ul)
            {
                throw new exception("[CmdAddLoginRewardPlayer::lineResult][Error] nao conseguiu adicionar player no Login Reward[ID=" + Convert.ToString(m_id) + "] por que m_ps.id retornado is invalid(" + Convert.ToString(m_ps.id) + ").", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                3, 0));
            }
        }

        protected override Response prepareConsulta()
        {

            if (m_id == 0u)
            {
                throw new exception("[CmdAddLoginRewardPlayer::prepareConsulta][Error] m_id is invalid(" + Convert.ToString(m_id) + ")", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            if (m_ps.uid == 0u)
            {
                throw new exception("[CmdAddLoginRewardPlayer::prepareConsulta][Error] m_ps.uid is invalid(" + Convert.ToString(m_ps.uid) + ")", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            m_ps.id = 0Ul;

            var r = procedure(m_szConsulta,
                Convert.ToString(m_id) + ", " + Convert.ToString(m_ps.uid) + ", " + Convert.ToString(m_ps.count_days) + ", " + Convert.ToString(m_ps.count_seq) + ", " + (m_ps.is_clear ? "1" : "0") + ", " + makeText(_formatDate(m_ps.update_date.ConvertTime())));

            checkResponse(r, "nao conseguiu adicionar o PLAYER[" + m_ps.toString() + "] do Login Reward[ID=" + Convert.ToString(m_id) + "]");

            return r;
        }


        private ulong m_id = new ulong();
        private stPlayerState m_ps = new stPlayerState();

        private const string m_szConsulta = "pangya.ProcInsertLoginRewardPlayer";
    }
}