namespace Pangya_LoginServer.Models
{
    public class PlayerInfo : player_info
    {
        public PlayerInfo() : base(0)
        {
             clear();
        }
                        
        public new void clear()
        {

            base.clear(); 

            m_state = 0;
            m_place = 0;
            m_server_uid = 0;
        }
        public byte m_state;
        public byte m_place;
        public uint m_server_uid = new uint(); // Server UID em que eles está conectado

    }
}
