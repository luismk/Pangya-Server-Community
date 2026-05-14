using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pangya_GameServer.Models
{
    public class world_tour_config
    {
        public int Id { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool Active { get; set; } 
        public bool SendNotice { get; set; } // Se já enviou o aviso de início do evento(uma unica vez)
    }
}
