using PangyaAPI.Network.Models;
using PangyaAPI.SQL;
namespace PangyaAPI.Network.Repository
{
    public class CmdRegisterServer : Pangya_DB
    {
        ServerInfoEx m_si;
        public CmdRegisterServer(ServerInfoEx _si)
        {
            m_si = _si;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {

        }

        protected override Response prepareConsulta()
        {
            var str = (m_si.uid) + ", " + makeText(m_si.nome) + ", " + makeText(m_si.ip)
                + ", " + (m_si.port) + ", " + (m_si.tipo) + ", " + (m_si.max_user)
                + ", " + (m_si.curr_user) + ", " + (m_si.rate.pang) + ", " + makeText(m_si.version)
                + ", " + makeText(m_si.version_client) + ", " + (m_si.propriedade.ulProperty) + ", " + (m_si.angelic_wings_num)
                + ", " + (m_si.event_flag.usEventFlag) + ", " + (m_si.rate.exp) + ", " + (m_si.img_no)
                + ", " + (m_si.rate.scratchy) + ", " + (m_si.rate.club_mastery) + ", " + (m_si.rate.treasure)
                + ", " + (m_si.rate.papel_shop_rare_item) + ", " + (m_si.rate.papel_shop_cookie_item) + ", " + (m_si.rate.chuva);
            var r = procedure("pangya.ProcRegServer_New",str );

           

            checkResponse(r, "nao conseguiu registrar o server[GUID=" + (m_si.uid) + ", PORT=" + (m_si.port) + ", NOME=" + (m_si.nome) + "] no banco de dados");
            return r;
        }

        public ServerInfoEx getServerList()
        {
            return this.m_si;
        }


        public void setInfo(ServerInfoEx _si)
        {
            m_si = _si;
        }
    }
}
