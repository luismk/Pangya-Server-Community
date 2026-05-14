namespace Pangya_RankingServer.Models
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
        }
        public byte m_state; 
        // Dados que usa para consultar o rank
        public search_dados_ex m_sd { get; set; } = new search_dados_ex(); // Search dados
    }
}
