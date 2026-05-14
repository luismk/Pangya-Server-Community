
using System;
using Pangya_GameServer.Models;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;

namespace Pangya_GameServer.Repository
{
    public class CmdUpdateDailyQuestUser : Pangya_DB
    {
        public CmdUpdateDailyQuestUser()
        {
            this.m_uid = 0;
            this.m_dqiu = new DailyQuestInfoUser(0);
        }

        public CmdUpdateDailyQuestUser(uint _uid,
            DailyQuestInfoUser _dqiu)
        {
            this.m_uid = _uid;
            this.m_dqiu = _dqiu;
        }

        public virtual void Dispose()
        {
        }

        public uint getUID()
        {
            return (m_uid);
        }

        public void setUID(uint _uid)
        {
            m_uid = _uid;
        }

        public DailyQuestInfoUser getInfo()
        {
            return m_dqiu;
        }

        public void setInfo(DailyQuestInfoUser _dqiu)
        {
            m_dqiu = _dqiu;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {

            // N�o usa por que � um UPDATE
            return;
        }

        protected override Response prepareConsulta()
        {

            if (m_uid == 0u)
            {
                throw new exception("[CmdUpdateDailyQuestUser][Error] m_uid is invalid(zero).", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            string accept_dt = "NULL";
            string today_dt = "NULL";

            if (m_dqiu.accept_date != 0)
                accept_dt = "'" + DateTimeOffset.FromUnixTimeSeconds(m_dqiu.accept_date)
                                               .ToString("yyyy-MM-dd HH:mm:ss.fffffff") + "'";

            if (m_dqiu.current_date != 0)
                today_dt = "'" + DateTimeOffset.FromUnixTimeSeconds(m_dqiu.current_date)
                                              .ToString("yyyy-MM-dd HH:mm:ss.fffffff") + "'";


            var r = _update(m_szConsulta
                + "last_quest_accept = " + accept_dt
                + ", today_quest = " + today_dt
                + " WHERE UID = " + m_uid);

            checkResponse(r, "nao conseguiu Atualizar o DailyQuest[ACCEPT_DT=" + accept_dt + ", TODAY_DT=" + today_dt + "] do PLAYER[UID=" + Convert.ToString(m_uid) + "]");

            return r;
        }


        private uint m_uid = new uint();
        private DailyQuestInfoUser m_dqiu = new DailyQuestInfoUser();

        private const string m_szConsulta = "UPDATE pangya.pangya_daily_quest_player SET ";
    }
}
