using System;
using Pangya_GameServer.Models;
using PangyaAPI.SQL;

namespace Pangya_GameServer.Repository
{
    public class CmdTutorialInfo : Pangya_DB
    {
        private uint m_uid;
        TutorialInfo m_ti;
        public CmdTutorialInfo(uint _uid)
        {
            m_uid = (_uid);
            m_ti = new TutorialInfo();
        }
        protected override void lineResult(ctx_res _result, uint _index_result)
        {
            checkColumnNumber(3);
            try
            {
                m_ti.rookie = Convert.ToUInt32(_result.data[0]);
                m_ti.beginner = Convert.ToUInt32(_result.data[1]);
                m_ti.advancer = Convert.ToUInt32(_result.data[2]);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);

            }
        }

        protected override Response prepareConsulta()
        {
            var r = procedure("pangya.GetTutorial", m_uid.ToString());
            checkResponse(r, "nao conseguiu pegar o member info do player: " + (m_uid));
            return r;
        }


        public TutorialInfo getInfo()
        {
            return m_ti;
        }

        public uint getUID()
        {
            return m_uid;
        }

        public void setUID(uint _uid)
        {
            m_uid = _uid;
        }
    }
}