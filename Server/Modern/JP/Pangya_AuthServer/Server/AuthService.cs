using Pangya_AuthServer.Handles;
using Pangya_AuthServer.Manager;
using Pangya_AuthServer.Repository;
using Pangya_AuthServer.Session;
using PangyaAPI.DataBase;
using PangyaAPI.Network;
using PangyaAPI.Network.Core;
using PangyaAPI.Network.Handle;
using PangyaAPI.Network.Models;
using PangyaAPI.Network.Repository;
using PangyaAPI.Network.Security;
using PangyaAPI.Network.Service.Auth;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;

namespace Pangya_AuthServer.Server
{
    /// <summary>
    /// Central Authentication Server that manages Game Servers, Login Servers, and Player Authentication
    /// </summary>
    public class AuthService : UnitServer<Player>
    {
        #region Fields
        private DateTime GuildRankTime;
        private PlayerManager _playerManager; 
        public Handle_TRANSLATE_CMD _TranslateCmd;
        #endregion

        #region Constructor
        public AuthService() : base(new PlayerManager(500), new PacketDispatcher<Player, AuthClientDispatcher>(), ServerType.AuthServer)
        {
            _playerManager = (PlayerManager)SessionsManager;
            LoadConfig();
            _TranslateCmd = new Handle_TRANSLATE_CMD();
            //inicia os registros dos packet 
            RegisterHandlers(); 
        }
        #endregion

        #region Initialization
        public override void LoadConfig()
        {
            base.LoadConfig(); 
            m_si.Type = 5;//auth server
        }

        private void RegisterHandlers()
        { 
            _dispatcher.Register(AuthClientDispatcher.AUTHENTIC_PLAYER, new Handle_PLAYER_LOGIN());
            _dispatcher.Register(AuthClientDispatcher.REQUEST_DISCONNECT_PLAYER, new Handle_REQUEST_DISCONNECT_PLAYER());
            _dispatcher.Register(AuthClientDispatcher.CONFIRM_DISCONNECT_PLAYER, new Handle_CONFIRM_DISCONNECT_PLAYER());
            _dispatcher.Register(AuthClientDispatcher.REQUEST_INFO_PLAYER, new Handle_REQUEST_INFO_PLAYER());
            _dispatcher.Register(AuthClientDispatcher.CONFIRM_SEND_INFO_PLAYER, new Handle_CONFIRM_SEND_INFO_PLAYER());
            _dispatcher.Register(AuthClientDispatcher.SEND_COMMAND_TO_OTHER_SERVER, new Handle_SEND_COMMAND_TO_OTHER_SERVER());
            _dispatcher.Register(AuthClientDispatcher.SEND_REPLY_TO_OTHER_SERVER, new Handle_SEND_REPLY_TO_OTHER_SERVER());
            _dispatcher.Register(AuthClientDispatcher.SERVER_HEART, new Handle_HEARTBEAT());
        }
        #endregion

        #region Protected Overrides - Core Methods

        protected override bool CheckCommand(Queue<string> _command)
        {
            return false;
        }

        protected override bool CheckPacket(IAppSession session, Packet packet)
        {
            return true;
        }

        public override void DBResponse(int _msg_id, Pangya_DB _pangya_db, object _arg)
        {
            if (_arg == null)
            {
                _smp.LogManager.Instance.push(new AppMessage(
                    "[AuthServer::DBResponse][WARNING] _arg is nullptr, na msg_id = " + Convert.ToString(_msg_id),
                    type_msg.CL_FILE_LOG_AND_CONSOLE));
                return;
            }

            if (_pangya_db.getException().getCodeError() != 0)
            {
                _smp.LogManager.Instance.push(new AppMessage(
                    "[AuthServer::DBResponse][Error] " + _pangya_db.getException().getFullMessageError(),
                    type_msg.CL_FILE_LOG_AND_CONSOLE));
                return;
            }

            var _server = (AuthService)(_arg);

            switch (_msg_id)
            {
                case 1: // Update Command
                    var cmd_uc = (CmdUpdateCommand)(_pangya_db);
                    break;
                case 2: // Update Auth Server Key
                    var cmd_uask = (CmdUpdateAuthServerKey)(_pangya_db);
                    break;
                case 3: // Update Guild Ranking
                    break;
                case 0:
                default:
                    break;
            }
        }

        #endregion

        #region Connection Management

        protected override void OnClientConnected(IAppSession session)
        {
            if (session is not Player player)
            {
                Console.WriteLine($"[Erro] A sessão conectada não é do Type Player! Tipo real: {session.GetType().Name}");
                return;
            }

            try
            {
                var packet = new Packet(0x00);
                packet.WriteInt32(player._ParseKey);
                packet.WriteInt32(this.m_si.UID);
                player.SendAuth(packet, true);
                _smp.LogManager.Instance.push(new AppMessage($"[AuthService::OnClientConnected] PLAYER[IP: {player.GetIP()} ID: {player.ConnectionID}]", type_msg.CL_ONLY_CONSOLE));
            }
            catch (exception ex)
            {
                _smp.LogManager.Instance.push(new AppMessage(
                    $"[AuthService.OnClientConnected][ErrorSt]: {ex.getFullMessageError()}",
                    type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        protected override void OnClientDisconnected(IAppSession session)
        {
            if (session == null)
                throw new exception(
                    "[AuthService::OnClientDisconnected][Error] _session is nullptr.",
                    ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MESSAGE_SERVER, 60, 0));

            Player p = (Player)session;
            _smp.LogManager.Instance.push(new AppMessage(
                $"[AuthService::OnClientDisconnecteded][Warning] PLAYER[IP: {p._IpAddress} ID: {p.ConnectionID}]",
                type_msg.CL_FILE_LOG_AND_CONSOLE));
        }

        #endregion

        #region Heartbeat and Maintenance

        protected override void OnHeartBeat()
        {
            var local = DateTime.Now;

            try
            {
                _playerManager = (PlayerManager)SessionsManager;
                OnStart();
                
                if(_playerManager.Count > 0)
                _TranslateCmd.Handle();

                // Guild Ranking Update
                if (GuildRankTime.Year == 0)
                {
                    CmdGuildRankingUpdateTime cmd_grut = new CmdGuildRankingUpdateTime();
                    snmdb.NormalManagerDB.Instance.add(0, cmd_grut);

                    if (cmd_grut.getException().getCodeError() != 0)
                    {
                        throw cmd_grut.getException();
                    }

                    GuildRankTime = cmd_grut.getTime();
                }

                // Check if it's a new day and update Guild Ranking
                if (GuildRankTime.Year < local.Year
                    || GuildRankTime.Month < local.Month
                    || GuildRankTime.Day < local.Day)
                {
                    snmdb.NormalManagerDB.Instance.add(3,
                        new CmdUpdateGuildRanking(),
                        DBResponse,
                        this);

                    GuildRankTime = DateTime.Now;
                }
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage(
                    "[AuthServer::onHeartBeat][ErrorSystem] " + e.getFullMessageError(),
                    type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        #endregion
        protected override void OnStart()
        {
            var sessions = SessionsManager.GetAllSessions();

            // 1. Definimos a ordem e os nomes que DEVEM aparecer
            var serverTemplate = new Dictionary<uint, string>
    {
        { 0, "LS" },
        { 4, "RS" },
        { 3, "MS" },
        { 1, "GS" }, 
    };

            // 2. Contamos as sessões atuais
            var counts = sessions
                .GroupBy(s => s.GetCapability())
                .ToDictionary(g => g.Key, g => g.Count());

            // 3. Montamos a string baseada no nosso template fixo
            var summary = serverTemplate.Select(t =>
            {
                int count = counts.ContainsKey((uint)t.Key) ? counts[t.Key] : 0;
                return $"{t.Value}: {count}";
            });

            string appsConnected = string.Join(", ", summary);
            Console.Title = $"Auth Service: {appsConnected}";
        }

        public Player FindSessionByUID(uint _uid)
        {
            return _playerManager.FindSessionByUID(_uid);
        }

        public Player FindPlayer(uint _uid, bool _oid = false)
        {
            return _playerManager.FindPlayer(_uid, _oid); 
        }

        public List<Player> FindPlayersByType(uint uid)
        {
            return _playerManager.FindPlayerByType(uid);
        }

        public List<Player> FindPlayerByTypeExcludeUID(uint _type, uint _uid)
        {
            return _playerManager.FindPlayerByTypeExcludeUID(_type, _uid);
        }

    }
    public class AuthServer : Singleton<AuthService>{ }
}

