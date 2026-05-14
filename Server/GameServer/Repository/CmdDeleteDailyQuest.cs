using System;
using System.Collections.Generic;
using Pangya_GameServer.Models;
using PangyaAPI.SQL;

namespace Pangya_GameServer.Repository
{
    public class CmdDeleteDailyQuest : Pangya_DB
    {
        public CmdDeleteDailyQuest(bool _waiter = false) : base(_waiter)
        {
            this.m_uid = 0;
            this.v_rdqu = new List<RemoveDailyQuestUser>();
        }

        public CmdDeleteDailyQuest(uint _uid,
            List<RemoveDailyQuestUser> _rdqu,
            bool _waiter = false) : base(_waiter)
        {
            this.m_uid = _uid;
            this.v_rdqu = new List<RemoveDailyQuestUser>(_rdqu);
        }


        public uint getUID()
        {
            return (m_uid);
        }

        public void setUID(uint _uid)
        {
            m_uid = _uid;
        }

        public List<RemoveDailyQuestUser> getDeleteDailyQuest()
        {
            return new List<RemoveDailyQuestUser>(v_rdqu);
        }

        public void setDeleteDailyQuest(List<RemoveDailyQuestUser> _rdqu)
        {
            v_rdqu = new List<RemoveDailyQuestUser>(_rdqu);
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {

            // N�o usa por que � um delete esse aqui
            return;
        }

        protected override Response prepareConsulta()
        {

            string ids = "";

            for (var i = 0; i < v_rdqu.Count; ++i)
            {
                if (i == 0)
                {
                    ids = Convert.ToString(v_rdqu[i].id);
                }
                else
                {
                    ids += ", " + Convert.ToString(v_rdqu[i].id);
                }
            }

            var r = consulta(m_szConsulta[0] + Convert.ToString(m_uid) + m_szConsulta[1] + ids + m_szConsulta[2] + Convert.ToString(m_uid) + m_szConsulta[3] + ids + m_szConsulta[4]);

            checkResponse(r, "nao conseguiu deletar a daily[ID(s)=" + ids + "] quest do player: " + Convert.ToString(m_uid));

            return r;
        }

        private uint m_uid = new uint();
        private List<RemoveDailyQuestUser> v_rdqu = new List<RemoveDailyQuestUser>();

        private string[] m_szConsulta = { "DELETE FROM pangya.pangya_achievement WHERE uid = ", " AND ID_ACHIEVEMENT IN(", ");DELETE FROM pangya.pangya_quest WHERE uid = ", " AND achievement_id IN(", ")" };

    }
}