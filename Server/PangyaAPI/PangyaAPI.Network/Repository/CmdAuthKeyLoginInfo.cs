using System;
using PangyaAPI.SQL;
using AuthKeyLoginInfo = PangyaAPI.Network.Models.AuthKeyInfo;
namespace PangyaAPI.Network.Repository
{
    public class CmdAuthKeyLoginInfo : Pangya_DB
    {
        int m_uid = -1;
        AuthKeyLoginInfo m_akli;
        public CmdAuthKeyLoginInfo(int _uid)
        {
            m_akli = new AuthKeyLoginInfo();
            m_uid = _uid;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {
            checkColumnNumber(2);
            try
            {
                if (!string.IsNullOrEmpty(_result.data[0].ToString()))
                    m_akli.key = _result.data[0].ToString();

                m_akli.valid = byte.Parse(_result.data[1].ToString());

                if (m_akli.key[0] == '\0')
                    throw new Exception("[CmdAuthKeyLoginInfo::lineResult][Error] a consulta retornou uma auth key login invalid");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        protected override Response prepareConsulta()
        {
            if (m_uid == 0 || m_uid == -1)
                throw new Exception("[CmdAuthKeyLoginInfo::prepareConsulta][Error] m_uid is invalid(zero).");


            var r = procedure("pangya.ProcGetAuthKeyLogin", m_uid.ToString());

            checkResponse(r, "nao conseguiu pegar o Auth Server Key do Server[UID=" + (m_uid) + "]");
            return r;
        }


        public int getUID()
        {
            return m_uid;
        }

        public void setUID(int _server_uid)
        {
            m_uid = _server_uid;
        }

        public AuthKeyLoginInfo getInfo()
        {
            return m_akli;
        }

        public void setInfo(AuthKeyLoginInfo _akli)
        {
            m_akli = _akli;
        }
    }
}
