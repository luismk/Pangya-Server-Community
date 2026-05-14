using System;
using PangyaAPI.SQL;

namespace PangyaAPI.Network.Repository
{
    public class CmdAuthKeyGame : Pangya_DB
    {
        string m_auth_key_game;
        uint m_uid;
        uint m_server_uid;
        public CmdAuthKeyGame(uint _uid, uint _server_uid)
        {
            m_auth_key_game = "";
            m_uid = _uid;
            m_server_uid = _server_uid;
        }


        protected override void lineResult(ctx_res _result, uint _index_result)
        {
            checkColumnNumber(1);
            try
            {
                if (!string.IsNullOrEmpty(_result.data[0].ToString()))
                    m_auth_key_game = (_result.data[0].ToString());

                if (string.IsNullOrEmpty(m_auth_key_game))
                    throw new Exception("[CmdAuthKey::lineResult][Error] retornou nulo na consulta auth key do player: " + (m_uid));

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);

            }
        }

        protected override Response prepareConsulta()
        {
            m_auth_key_game = "";
            var r = procedure("pangya.ProcGeraAuthKeyGame", m_uid.ToString() + "," + m_server_uid.ToString());

            checkResponse(r, "nao conseguiu pegar a auth key do game server do player: " + (m_uid).ToString());
            return r;
        }


        public string getAuthKey()
        {
            return m_auth_key_game;
        }


        public void setAuthKey(string _auth_key)
        {
            m_auth_key_game = _auth_key;
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
