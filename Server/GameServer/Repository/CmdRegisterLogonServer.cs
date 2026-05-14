using PangyaAPI.SQL;

namespace Pangya_GameServer.Repository
{
    public class CmdRegisterLogonServer : Pangya_DB
    {
        uint m_uid = 0;
        int m_server_uid = 0;

        public CmdRegisterLogonServer(uint _uid, int _server_uid)
        {
            m_uid = _uid;
            m_server_uid = _server_uid;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {
            // Não usa por que é um UPDATE
            return;
        }

        protected override Response prepareConsulta()
        {
            var r = procedure("pangya.ProcRegisterLogonServer", m_uid.ToString() + ", " + m_server_uid);

            checkResponse(r, "nao conseguiu registrar o logon do player: " + (m_uid) + ", na option: " + (m_server_uid));
            return r;
        }

        public int getOption()
        {
            return m_server_uid;
        }


        public uint getUID()
        {
            return m_uid;
        }
    }
}
