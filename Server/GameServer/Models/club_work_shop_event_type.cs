namespace Pangya_GameServer.Models
{
    /// <summary>
    /// ref: 
    /// </summary>
    public class club_work_shop_event_type
    {
        public int totalHoles { get; set; }
        public int holesPerPhase { get; set; }
        public int barraMax { get; set; }
        public int barraAtual { get; set; }
        public int totalHolesMax { get; set; }

        public club_work_shop_event_type()
        {
            totalHoles = 15;
            holesPerPhase = 9;
            barraMax = 10;
            totalHolesMax = 300; // ou algum outro valor total esperado
        }

        public void Calc()
        {
            barraAtual = (totalHoles * barraMax) / totalHolesMax;
            if (barraAtual > barraMax)
                barraAtual = barraMax;
        }
    }

}
