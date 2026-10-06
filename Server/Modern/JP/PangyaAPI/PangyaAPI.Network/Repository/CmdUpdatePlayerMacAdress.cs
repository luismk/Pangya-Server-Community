using PangyaAPI.DataBase;
namespace PangyaAPI.Network.Repository
{
    public class CmdUpdatePlayerMacAdress : Pangya_DB
    {
        string m_ask;
        uint m_uid;
        public CmdUpdatePlayerMacAdress(uint _uid, string _ask)
        {
            m_uid = _uid;
            m_ask = _ask;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {

        }

        protected override Response prepareConsulta()
        {
            if (m_uid == 0u)
                throw new Exception("[CmdUpdatePlayerMacAdress::prepareConsulta][Error] string m_ask.ServerIndex is invalid(zero).");

            var r = consulta($"update pangya.account set MacAddress = {makeText(m_ask)} where UID = {m_uid}");

            checkResponse(r, "nao conseguiu atualizar MacAddress[PLAYER=" + m_uid + "]");
            return r;
        }
    }
}
