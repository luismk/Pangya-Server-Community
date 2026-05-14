using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
 
namespace Pangya_AuthServer.Models
{
    public class PlayerInfo : player_info
    {
        public PlayerInfo()
        {
            clear();
        }

        public virtual void Dispose()
        {
            clear();
        }

        public override void clear()
        {

            base.clear();

            m_state = 0;
            m_place = 0;
            m_server_uid = 0u;
        }

        public byte m_state { get; set; }
        public byte m_place { get; set; }
        public uint m_server_uid { get; set; } = new uint(); // Server UID em que eles est� conectado
    }
}
