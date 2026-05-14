using Pangya_MessengerServer.Models; 
using PangyaAPI.Network.PangyaPacket;
namespace Pangya_MessengerServer.Session
{
    public class Player : PangyaAPI.Network.PangyaSession.Session
    {
        public PlayerInfo m_pi { get; set; }
 
        public Player()
        {
            m_pi = new PlayerInfo();
        }           

        public override string getNickname()
        {
            return m_pi.nickname;
        }

        public override uint getUID()
        {
            return m_pi.uid;
        }

        public override string getID()
        {
            return m_pi.id;
        }

        public override uint getCapability() { return m_pi.m_cap; } 

        public override byte getStateLogged()
        {
            return 1;
        }

        public override bool clear()
        {
            lock (this)
            {
                bool ret;
                if (ret = base.clear())
                {
                    // Player Info
                    m_pi.clear(); 
                }
                return ret;
            }
        }
    }
}
