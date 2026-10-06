using Pangya_MessengerServer.Models;
using Pangya_MessengerServer.Session; 
namespace Pangya_MessengerServer.Manager
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
            foreach (var el in this._sessions.Values)
            {
                if (el.Connected && ((!oid) ? el.GetUID() : (uint)el.ConnectionID) == uid)
                {
                   return el; 
                }
            }

            return null;
        } 

        public Dictionary<uint, Player> FindAllFriend(List<FriendInfoEx> friends)
        {
            var friendMap = new Dictionary<uint, Player>();

            foreach (var player in this._sessions.Values)
            {  
                if (player != null && !friendMap.ContainsKey(player.UserInfo.UID))
                {
                    friendMap[player.UserInfo.UID] = player;
                }
            }

            return friendMap;
        }

        public Dictionary<uint, Player> FindAllGuildMember(uint guildUid)
        {
            var guildMap = new Dictionary<uint, Player>();

            foreach (var player in this._sessions.Values)
            { 
                if (player != null && player.UserInfo.GuildIndex > 0 && player.UserInfo.GuildIndex == guildUid)
                {
                    if (!guildMap.ContainsKey(player.UserInfo.UID))
                    {
                        guildMap[player.UserInfo.UID] = player;
                    }
                }
            }

            return guildMap;
        }
    }
}