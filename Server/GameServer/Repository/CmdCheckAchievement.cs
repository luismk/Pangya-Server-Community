using System;
using PangyaAPI.SQL;

namespace Pangya_GameServer.Repository
{
    public class CmdCheckAchievement : Pangya_DB
    {
        private uint m_uid = new uint();
        private bool m_check = false;

        private const string m_szConsulta = "pangya.ProcCheckAchievement";

        public CmdCheckAchievement(uint uid)  : base(false)
        {
            this.m_uid = uid;
        }

        public uint getUID()
        {
            return m_uid;
        }

        public void setUID(uint _uid)
        {
            m_uid = _uid;
        }

        public bool getLastState()
        {
            return m_check;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {

            checkColumnNumber(1);

            m_check = _result.GetInt32(0) > 0;
        }

        protected override Response prepareConsulta()
        {

            m_check = false;

            var r = procedure(m_szConsulta, m_uid.ToString());

            checkResponse(r, "nao conseguiu verificar o achievement do player: " + Convert.ToString(m_uid));

            return r;
        }
    }
}