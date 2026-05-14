using System;
using Pangya_GameServer.Models;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;
namespace Pangya_GameServer.Repository
{
    public class CmdUpdateTutorial : Pangya_DB
    {
        public CmdUpdateTutorial()
        {
            this.m_uid = 0;
            this.m_ti = new TutorialInfo();
        }

        public CmdUpdateTutorial(uint _uid,
            TutorialInfo _ti

            )
        {
            this.m_uid = _uid;
            this.m_ti = (_ti);
        }


        public uint getUID()
        {
            return (m_uid);
        }

        public void setUID(uint _uid)
        {
            m_uid = _uid;

        }

        public TutorialInfo getInfo()
        {
            return m_ti;
        }

        public void setInfo(TutorialInfo _ti)
        {
            m_ti = _ti;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {

            // N�o usa por que � um UPDATE
            return;
        }

        protected override Response prepareConsulta()
        {

            if (m_uid == 0)
            {
                throw new exception("[CmdUpdateTutorial::prepareConsulta][Error] m_uid is invalid(zero)", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            var r = _update(m_szConsulta[0] + Convert.ToString(m_ti.rookie) + m_szConsulta[1] + Convert.ToString(m_ti.beginner) + m_szConsulta[2] + Convert.ToString(m_ti.advancer) + m_szConsulta[3] + Convert.ToString(m_uid));

            checkResponse(r, "nao conseguiu Atualizar o Tutorial[Rookie=" + Convert.ToString(m_ti.rookie) + ", Beginner=" + Convert.ToString(m_ti.beginner) + ", Advancer=" + Convert.ToString(m_ti.advancer) + "] do PLAYER[UID=" + Convert.ToString(m_uid) + "]");

            return r;
        }
        private uint m_uid = new uint();
        private TutorialInfo m_ti = new TutorialInfo();

        private string[] m_szConsulta = { "UPDATE pangya.tutorial SET Rookie = ", ", Beginner = ", ", Advancer = ", " WHERE UID = " };
    }
}
