using System;
using PangyaAPI.SQL;

namespace PangyaAPI.Network.Repository
{
    public class CmdNewAuthServerKey : Pangya_DB
    {
        readonly int m_server_uid = -1;
        string m_key;
        public CmdNewAuthServerKey(int _uid)
        {
            m_key = "";
            m_server_uid = _uid;
        }

        public CmdNewAuthServerKey()
        {
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {
            checkColumnNumber(1);
            try
            {
                m_key = _result.data[0].ToString();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        protected override Response prepareConsulta()
        {
            if (m_server_uid == 0 || m_server_uid == -1)
                throw new Exception("[CmdNewAuthServerKey::prepareConsulta][Error] m_server_uid is invalid(zero).");


            var r = procedure("pangya.ProcGetNewAuthServerKey", m_server_uid.ToString());

            checkResponse(r, "Server[UID=" + (m_server_uid) + "] nao conseguiu gerar uma nova key para o Auth Server");
            return r;
        }


        public string getInfo()
        {
            return m_key;
        }
    }
}
