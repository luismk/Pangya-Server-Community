using Pangya_RankingServer.Models;
using Pangya_RankingServer.Session; 
namespace Pangya_RankingServer.Manager
{
    public class PlayerManager : AppSessionManager<Player>
    {
        public PlayerManager(int maxUsers) : base(maxUsers)
        {
        }

        public Player FindByUID(uint uid)
        {
            return GetAllSessions()
                .OfType<Player>()
                .FirstOrDefault(p => p.UserInfo?.UID == uid);
        }

        public Player FindByNickname(string nickname)
        {
            return GetAllSessions()
                .OfType<Player>()
                .FirstOrDefault(p => p.UserInfo?.Login == nickname);
        }

        public bool IsAlreadyLoggedIn(uint uid)
        {
            return GetAllSessions()
                .OfType<Player>()
                .Any(p => p.Connected && p.UserInfo?.UID == uid);
        }

        // Método para o sistema de Kick (Derrubar conexão duplicada)
        public void KickByUID(uint uid)
        {
            var p = FindByUID(uid);
            if (p != null && p.Connected)
            {
                p.Disconnect();
            }
        } 
        public Player FindPlayer(uint uid, bool oid = false)
        {
            Player p = null;
            foreach (var el in this._sessions.Values)
            {
                if (el.Connected && ((!oid) ? el.GetUID() : (uint)el.ConnectionID) == uid)
                {
                    p = (Player)el;
                    break;
                }
            }

            return p;
        }
         
    }
}