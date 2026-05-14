using System;
using PangyaAPI.Network.Models;
using PangyaAPI.SQL;
namespace PangyaAPI.Network.Repository
{
    public class CmdAuthKeyGameInfo : Pangya_DB
    {
        uint m_uid = 0;
        int m_server_uid = -1;
        AuthKeyGameInfo m_akgi;
        public CmdAuthKeyGameInfo(uint _uid, int _server_uid)
        {
            m_akgi = new AuthKeyGameInfo();
            m_uid = _uid;
            m_server_uid = _server_uid;
        }

        public CmdAuthKeyGameInfo()
        {
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {
            checkColumnNumber(3);
            try
            {
                if (!string.IsNullOrEmpty(_result.data[0].ToString()))
                    m_akgi.key = _result.data[0].ToString();

                m_akgi.server_uid = int.Parse(_result.data[1].ToString());
                m_akgi.valid = byte.Parse(_result.data[2].ToString());
                if (m_akgi.key[0] == '\0')
                    throw new Exception("[CmdAuthKeyGameInfo::lineResult][Error] a consulta retornou uma auth key login invalid");
                if (m_akgi.server_uid != m_server_uid)
                    throw new Exception("[CmdAuthKeyGameInfo::lineResult][Error] o server uid retornado na consulta nao é igual ao requisitado. server uid req: "
                            + (m_server_uid).ToString() + " != " + (m_akgi.server_uid).ToString());

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        protected override Response prepareConsulta()
        {

            if (m_uid == 0 || m_uid == uint.MaxValue)
                throw new Exception("[CmdAuthKeyGameInfo::prepareConsulta][Error] m_uid is invalid(zero).");

            var r = procedure("pangya.ProcGetAuthKeyGame", m_uid.ToString() + "," + m_server_uid.ToString());

            checkResponse(r, "nao conseguiu pegar o auth key game do player: " + (m_uid) + ", do server uid: " + (m_server_uid));
            return r;
        }


        public uint getUID()
        {
            return m_uid;
        }

        public void setUID(uint _server_uid)
        {
            m_uid = _server_uid;
        }

        public AuthKeyGameInfo getInfo()
        {
            return m_akgi;
        }

        public void setInfo(AuthKeyGameInfo _akli)
        {
            m_akgi = _akli;
        }
    }
}
