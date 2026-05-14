using System;
using PangyaAPI.SQL;
namespace Pangya_GameServer.Repository
{
    public class CmdGeraWebKey : Pangya_DB
    {
        public CmdGeraWebKey(uint _uid)
        {
            this.m_uid = _uid;
            this.m_web_key = "";
        }


        public string getKey()
        {
            return m_web_key;
        }

        public uint getUID()
        {
            return m_uid;
        }

        public void setUID(uint _uid)
        {
            m_uid = _uid;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {
            checkColumnNumber(1);

            if (is_valid_c_string(_result.data[0]))
            {
                m_web_key = IFNULL<string>(_result.data[0]);
            }
        }

        protected override Response prepareConsulta()
        {

            m_web_key = "";

            var r = procedure(m_szConsulta,
                Convert.ToString(m_uid));

            checkResponse(r, "nao conseguiu pegar weblink key do player: " + Convert.ToString(m_uid));

            return r;
        }

        private uint m_uid = new uint();
        private string m_web_key = "";

        private const string m_szConsulta = "pangya.ProcGeraWeblinkKey";
    }
}
