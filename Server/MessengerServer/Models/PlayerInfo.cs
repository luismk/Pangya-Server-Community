using Pangya_MessengerServer.Manager;
using PangyaAPI.Network.Models;
using System;

namespace Pangya_MessengerServer.Models
{
    public class PlayerInfo : player_info
    {
        public PlayerInfo() : base(0)
        {
            m_logout = 0;
            base.clear();
            m_cpi.clear();
            m_friend_manager.clear();
        }
                        
        public override void clear()
        {
            m_logout = 0;
            base.clear();  
            m_cpi.clear(); 
            m_friend_manager.clear();
        }
                    

        public byte m_state;
        public int m_logout; // Verifica se j� mandou pacote de deslogar 
        public ChannelPlayerInfo m_cpi = new ChannelPlayerInfo();

        public FriendManager m_friend_manager = new FriendManager();
    }
}
