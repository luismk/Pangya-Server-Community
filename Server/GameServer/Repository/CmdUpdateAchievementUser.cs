using System;
using Pangya_GameServer.Models;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;

namespace Pangya_GameServer.Repository
{
    public class CmdUpdateAchievementUser : Pangya_DB
    {
        public CmdUpdateAchievementUser(uint _uid, AchievementInfoEx _ai)
        {
            this.m_uid = _uid;
            this.m_ai = _ai;
        }
         
        public uint getUID()
        {
            return (m_uid);
        }

        public void setUID(uint _uid)
        {
            m_uid = _uid;

        }

        public AchievementInfoEx getInfo()
        {
            return m_ai;
        }

        public void setInfo(AchievementInfoEx _ai)
        {
            m_ai = _ai;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {
            // N�o usa por que � um UPDATE
            return;
        }

        protected override Response prepareConsulta()
        {

            if (m_ai.id <= 0 || m_ai._typeid == 0)
            {
                throw new exception("[CmdUpdateAchievementUser::prepareConsulta][Error] AchievementInfoEx m_ai is invalid", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            var r = consulta(m_szConsulta[0] + Convert.ToString(m_ai.status) + m_szConsulta[1] + Convert.ToString(m_uid) + m_szConsulta[2] + Convert.ToString(m_ai.id));

            checkResponse(r, "nao conseguiu atualizar achievement[ID=" + Convert.ToString(m_ai.id) + "] do player: " + Convert.ToString(m_uid));

            return r;
        }

        private uint m_uid = new uint();
        private AchievementInfoEx m_ai = new AchievementInfoEx();

        private string[] m_szConsulta = { "UPDATE pangya.pangya_achievement SET status = ", " WHERE UID = ", " AND ID_ACHIEVEMENT = " };
    }
}
