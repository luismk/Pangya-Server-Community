using System;
using PangyaAPI.SQL;

namespace PangyaAPI.Network.Repository
{
    public class CmdAuthKeyLogin : Pangya_DB
    {
        string m_auth_key_login;
        int m_uid;
        public CmdAuthKeyLogin(int _uid)
        {
            m_auth_key_login = "";
            m_uid = _uid;
        }

        public CmdAuthKeyLogin()
        {
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {
            checkColumnNumber(1);
            try
            {
                if (!string.IsNullOrEmpty(_result.data[0].ToString()))
                    m_auth_key_login = (_result.data[0].ToString());

                if (string.IsNullOrEmpty(m_auth_key_login))
                    throw new Exception("[CmdAuthKey::lineResult][Error] retornou nulo na consulta auth key do player: " + (m_uid));

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);

            }
        }

        protected override Response prepareConsulta()
        {
            m_auth_key_login = "";
            var r = procedure("pangya.ProcGeraAuthKeyLogin", m_uid.ToString());

            checkResponse(r, "nao conseguiu pegar a auth key do login server do player: " + (m_uid).ToString());
            return r;
        }


        public string getAuthKey()
        {
            return m_auth_key_login;
        }


        public void setAuthKey(string _auth_key)
        {
            m_auth_key_login = _auth_key;
        }

        public int getUID()
        {
            return m_uid;
        }

        public void setUID(int _uid)
        {
            m_uid = _uid;
        }
    }
}
