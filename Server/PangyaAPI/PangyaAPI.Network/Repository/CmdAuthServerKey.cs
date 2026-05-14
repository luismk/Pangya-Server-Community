using System;
using PangyaAPI.Network.Models;
using PangyaAPI.SQL;

namespace PangyaAPI.Network.Repository
{
    public class CmdAuthServerKey : Pangya_DB
    {
        int m_server_uid = -1;
        AuthServerKey m_ask;
        public CmdAuthServerKey(int _uid)
        {
            m_ask = new AuthServerKey();
            m_server_uid = _uid;
        }

        public CmdAuthServerKey()
        {
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {
            checkColumnNumber(3);
            try
            {
                m_ask.server_uid = int.Parse(_result.data[0].ToString());

                if (is_valid_c_string(_result.data[1].ToString()))
                    m_ask.key = _result.data[1].ToString();

                m_ask.valid = byte.Parse(_result.data[2].ToString());

                if (m_ask.server_uid != m_server_uid)
                    throw new Exception("[CmdAuthServerKey::lineResult][Error] m_ask.server_uid = " + (m_ask.server_uid).ToString()
                        + " not match with m_server_uid = " + (m_server_uid).ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        protected override Response prepareConsulta()
        {
            if (m_server_uid == 0 || m_server_uid == -1)
                throw new Exception("[CmdAuthServerKey::prepareConsulta][Error] m_server_uid is invalid(zero).");


            var r = consulta("SELECT server_uid, [key], VALID FROM pangya.pangya_auth_key WHERE server_uid =" + m_server_uid);

            checkResponse(r, "nao conseguiu pegar o Auth Server Key do Server[UID=" + (m_server_uid) + "]");
            return r;
        }


        public int getServerUID()
        {
            return m_server_uid;
        }

        public void setServerUID(int _server_uid)
        {
            m_server_uid = _server_uid;
        }

        public AuthServerKey getInfo()
        {
            return m_ask;
        }
    }
}
