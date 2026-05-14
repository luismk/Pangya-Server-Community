using PangyaAPI.SQL;

namespace PangyaAPI.Network.Repository
{
    public class CmdRegisterLogon : Pangya_DB
    {
        uint m_uid = 0;
        int m_option = 0;

        public CmdRegisterLogon(uint _uid, int _option)
        {
            m_uid = _uid;
            m_option = _option;
        } 

        protected override void lineResult(ctx_res _result, uint _index_result)
        {
            // Não usa por que é um UPDATE
            return;
        }

        protected override Response prepareConsulta()
        {          
            var r = procedure("pangya.ProcRegisterLogon", m_uid.ToString() + ", " + m_option.ToString());

            checkResponse(r, "nao conseguiu registrar o logon do player: " + (m_uid) + ", na option: " + (m_option));
            return r;
        }

        public int getOption()
        {
            return m_option;
        }


        public uint getUID()
        {
            return m_uid;
        }
    }
}
