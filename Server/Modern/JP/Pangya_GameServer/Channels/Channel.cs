using Pangya_GameServer.Manager;
using Pangya_GameServer.Models;
using Pangya_GameServer.Session;
using PangyaAPI.Network.Models;
using PangyaAPI.Utilities;
namespace Pangya_GameServer.Channels
{
    public partial class Channel
    {
        #region FIELDS 
        private enum ESTADO : byte { UNITIALIZED, INITIALIZED }
        public enum LEAVE_ROOM_STATE : int { DO_NOTHING = -1, SEND_UPDATE_CLIENT = 0, ROOM_DESTROYED }

        public ChannelInfo m_ci { get; set; }
        public RoomManager m_rm { get; set; }

        // Propriedade para acessar a nova classe
        public Lobby Lobby { get; set; }

        private object m_cs = new object();
        private ServerProperty Type { get; set; }
        private int State { get; set; } = 0; 
        public List<Player> Sessions { get; set; }
        private Dictionary<Player, PlayerLobbyInfo> Players_Info { get; set; }
        public List<InviteChannelInfo> sInvites { get; set; }
        private object m_cs_invite = new object();
        private PangyaSyncTimer TimeInvite { get; set; }
        #endregion
   
        #region CONSTRUTOR 
        public Channel(ChannelInfo _ci, ServerProperty _type)
        {
            m_ci = _ci;
            m_rm = new RoomManager(); 
            Lobby = new Lobby(this); 
            Type = _type;
            State = 1;
            Sessions = new(_ci.max_user);
            Players_Info = new(_ci.max_user);
            sInvites = new();
        }
        #endregion 
    }
}