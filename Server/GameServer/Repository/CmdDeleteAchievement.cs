using System;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;
namespace Pangya_GameServer.Repository
{
    public class CmdDeleteAchievement : Pangya_DB
    {
        public CmdDeleteAchievement()
        {
            this.m_uid = 0;
            this.m_id = -1;
        }

        public CmdDeleteAchievement(uint _uid,
            int _id)
        {
            this.m_uid = _uid;
            this.m_id = _id;
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

        public int getId()
        {
            return (m_id);
        }

        public void setId(int _id)
        {
            m_id = _id;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {

            // N�o usa por que � um DELETE
            return;
        }

        protected override Response prepareConsulta()
        {

            if (m_uid == 0u)
            {
                throw new exception("[CmdDeleteAchievement::prepareConsulta][Error] m_uid is invalid(zero).", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            if (m_id <= 0)
            {
                throw new exception("[CmdDeleteAchievement::prepareConsulta][Error] m_id[VALUE=" + Convert.ToString(m_id) + "] is invalid.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 1));
            }

            var r = consulta(m_szConsulta[0] + Convert.ToString(m_uid) + m_szConsulta[1] + Convert.ToString(m_id));

            checkResponse(r, "nao conseguiu deletar o Achievement[ID=" + Convert.ToString(m_id) + "] do PLAYER[UID=" + Convert.ToString(m_uid) + "]");

            return r;
        }

        private uint m_uid = new uint();
        private int m_id = new int();

        private string[] m_szConsulta = { "DELETE FROM pangya.pangya_achievement WHERE UID = ", " AND ID_ACHIEVEMENT = " };

    }
}