using PangyaAPI.Utilities;

namespace PangyaAPI.Network.PangyaServer
{
    public class ClientType
    {
        private eClientType m_type;

        public enum eClientType
        {
            US,
            TH,
            JP
        }
        public ClientType()
        {
            m_type = eClientType.JP;
        }

        public eClientType getType()
        {
            return m_type;
        }
    }
    public class sClientType : Singleton<ClientType>
    { }
}
