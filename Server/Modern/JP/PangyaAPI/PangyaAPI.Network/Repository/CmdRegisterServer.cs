using PangyaAPI.Network.Models;
using PangyaAPI.DataBase;
namespace PangyaAPI.Network.Repository
{
    public class CmdRegisterServer : Pangya_DB
    {
        ServerInfo m_si;
        public CmdRegisterServer(ServerInfo _si)
        {
            m_si = _si;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {

        }

        protected override Response prepareConsulta()
        {
            var str = (m_si.UID) + ", " + makeText(m_si.Name) + ", " + makeText(m_si.IpAddress)
                + ", " + (m_si.Port) + ", " + (m_si.Type) + ", " + (m_si.MaxUsers)
                + ", " + (m_si.CurrentUsers) + ", " + (m_si.Rate.Pang) + ", " + makeText(m_si.BuildVersion)
                + ", " + makeText(m_si.ClientVersion) + ", " + (m_si.Property.Value) + ", " + (m_si.AngelicWingsCount)
                + ", " + (m_si.EventFlag.Value) + ", " + (m_si.Rate.Experience) + ", " + (m_si.ServerIcon)
                + ", " + (m_si.Rate.Scratchy) + ", " + (m_si.Rate.ClubMastery) + ", " + (m_si.Rate.Treasure)
                + ", " + (m_si.Rate.PapelShopRareItem) + ", " + (m_si.Rate.PapelShopCookieItem) + ", " + (m_si.Rate.Rain);

            var r = procedure("pangya.ProcRegServer_New", str); 
            checkResponse(r, "nao conseguiu registrar o server[GUID=" + (m_si.UID) + ", PORT=" + (m_si.Port) + ", NOME=" + (m_si.Name) + "] no banco de dados");
            return r;
        }

        public ServerInfo getServerList()
        {
            return this.m_si;
        }


        public void setInfo(ServerInfo _si)
        {
            m_si = _si;
        }
    }
}
