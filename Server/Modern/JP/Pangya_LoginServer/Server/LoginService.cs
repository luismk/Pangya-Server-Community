using Pangya_LoginServer.Handles;
using Pangya_LoginServer.Manager;
using Pangya_LoginServer.PangyaEnums;
using Pangya_LoginServer.Session;
using PangyaAPI.DataBase;
using PangyaAPI.IFF.Handle.JP;
using PangyaAPI.Network;
using PangyaAPI.Network.Config;
using PangyaAPI.Network.Core;
using PangyaAPI.Network.Handle;
using PangyaAPI.Network.Models;
using PangyaAPI.Network.Service;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using System.Diagnostics;
namespace Pangya_LoginServer.Server
{
    public class LoginService : AppServer<Player, PacketIDClient>
    {
        bool m_access_flag;
        bool m_create_user_flag;
        bool m_same_id_login_flag;

        private PlayerManager _playerManager;
        public bool IsUnderMaintenance { get; private set; }

        public LoginService() : base(new PlayerManager(500), new PacketDispatcher<Player, PacketIDClient>(), ServerType.LoginServer)
        { 
            _playerManager = (PlayerManager)SessionsManager;

            LoadConfig();

            RegisterHandlers();

            if (!sIff.Instance.isLoad())
                sIff.Instance.Init();
        }

        private void RegisterHandlers()
        {
            // [0x01] Autenticação Inicial (User, Pass, MacAddress, Opt Code)
            _dispatcher.Register(PacketIDClient.CLIENT_REQ_LOGIN, new Handle_PLAYER_LOGIN());

            // [0x03] Seleção de Servidor (Entrar no Lobby de Login)
            _dispatcher.Register(PacketIDClient.CLIENT_REQ_ENTER_SERVER, new Handle_PLAYER_ENTER_SERVER());

            // [0x04] Solicitação de Kick (Derrubar conexão anterior)
            _dispatcher.Register(PacketIDClient.CLIENT_REQ_KICK, new Handle_KICK_PLAYER());

            // [0x06] Definição de Nickname (Set Nick)
            _dispatcher.Register(PacketIDClient.CLIENT_REQ_SET_NICK, new Handle_PLAYER_SET_NICKNAME());

            // [0x07] Verificação de Nickname (Check Nick)
            _dispatcher.Register(PacketIDClient.CLIENT_REQ_CONFIRM_SET_NICK, new Handle_PLAYER_CONFIRM_NICKNAME());

            // [0x08] Seleção do Primeiro Personagem
            _dispatcher.Register(PacketIDClient.CLIENT_REQ_SET_CHARACTER, new Handle_PLAYER_SET_FIRST_CHARACTER());

            // [0x0B] Reconexão (Volta do Game Server para o Login)
            _dispatcher.Register(PacketIDClient.CLIENT_REQ_RECONNECT, new Handle_PLAYER_RECONNECT());
        }

        public override bool CheckCommand(Queue<string> _command)
        {
            Console.ResetColor();

            if (_command.Count == 0)
            {
                _smp.LogManager.Instance.push(new AppMessage($"[{GetType().Name}::CheckCommand][Error] Missing parameter", type_msg.CL_ONLY_CONSOLE));
                return true;
            }

            string s = _command.Dequeue();

            if (s.Equals("status", StringComparison.OrdinalIgnoreCase))
            {
                var process = Process.GetCurrentProcess();
                var memoryUsage = process.PrivateMemorySize64 / 1024 / 1024; // MB  
                _smp.LogManager.Instance.push(new AppMessage($"[{GetType().Name}::CheckCommand][Debug] STATUS[USERS: {Sessions?.Count() ?? 0}, MEMORY: {memoryUsage}, UPTIME: {DateTime.Now - process.StartTime}]", type_msg.CL_FILE_LOG_AND_CONSOLE));

            }
            else if (s.Equals("reload_files", StringComparison.OrdinalIgnoreCase))
            {
                ReloadFiles();
                _smp.LogManager.Instance.push(new AppMessage("Login Server files have been reloaded.", type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
            else if (s.Equals("open", StringComparison.OrdinalIgnoreCase))
            {
                if (_command.Count > 1)
                {
                    string subCommand = _command.Dequeue();
                    if (subCommand.Equals("server", StringComparison.OrdinalIgnoreCase))
                    {
                        setIsUnderMaintenance(true);//faço o servidor parar de rodar ou simplesmente não ira mais receber conexao!
                        _smp.LogManager.Instance.push(new AppMessage("Server Accept players ~~~.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                    }
                    else if (subCommand.Equals("gm", StringComparison.OrdinalIgnoreCase))
                    {
                        m_access_flag = true;
                        _smp.LogManager.Instance.push(new AppMessage("Now only GM and registered IPs can login.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                    }
                    else if (subCommand.Equals("all", StringComparison.OrdinalIgnoreCase) && _command.Count > 2 && _command.Dequeue().Equals("user", StringComparison.OrdinalIgnoreCase))
                    {
                        m_access_flag = false;
                        _smp.LogManager.Instance.push(new AppMessage("Now all users can login.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                    }
                    else
                    {
                        _smp.LogManager.Instance.push(new AppMessage($"Unknown Command: \"open {subCommand}\"", type_msg.CL_ONLY_CONSOLE));
                    }
                }
            }
            else if (s.Equals("stop", StringComparison.OrdinalIgnoreCase))
            {
                if (_command.Count > 1)
                {
                    string subCommand = _command.Dequeue();
                    if (subCommand.Equals("server", StringComparison.OrdinalIgnoreCase))
                    {
                        setIsUnderMaintenance(false);//faço o servidor parar de rodar ou simplesmente não ira mais receber conexao!
                        _smp.LogManager.Instance.push(new AppMessage("Server close players ~~~.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                    }
                    else
                    {
                        _smp.LogManager.Instance.push(new AppMessage($"Unknown Command: \"open {subCommand}\"", type_msg.CL_ONLY_CONSOLE));
                    }
                }
            }
            else if (!string.IsNullOrEmpty(s) && s == "reload_system")
            {
                string sTipo = _command.Dequeue(); 
                if (!string.IsNullOrEmpty(sTipo))
                {
                    switch (sTipo)
                    {
                        case "iff":
                            sIff.Instance.reload();
                            return true;
                        default:
                            _smp.LogManager.Instance.push(new AppMessage($"[{GetType().Name}::checkCommand][Error] Unknown Command: \"reload_system {sTipo}\"", type_msg.CL_FILE_LOG_AND_CONSOLE));
                            break;
                    }
                }
                else
                {
                    _smp.LogManager.Instance.push(new AppMessage($"[{GetType().Name}::checkCommand][Error] Unknown Command: \"reload_system {sTipo}\"", type_msg.CL_FILE_LOG_AND_CONSOLE));
                } 
                return true;
            } 
            else if (s == "cls" || s == "clear")
            {
                Console.Clear();
                ConsoleEx.Log();
                return true;
            }
            else if (s.Equals("help", StringComparison.OrdinalIgnoreCase) || s == "?")
            {
                var msg = _smp.LogManager.Instance;
                msg.push(new AppMessage("======= LOGIN SERVICE - COMMAND LIST =======", type_msg.CL_ONLY_CONSOLE));
                msg.push(new AppMessage("status                        - Status de memória, uptime e sessões de login.", type_msg.CL_ONLY_CONSOLE));
                msg.push(new AppMessage("clear / cls                   - Limpa o console.", type_msg.CL_ONLY_CONSOLE));
                msg.push(new AppMessage("reload_files                  - Recarrega arquivos de configuração do Login.", type_msg.CL_ONLY_CONSOLE));
                msg.push(new AppMessage("---------------------------------------------------", type_msg.CL_ONLY_CONSOLE));
                msg.push(new AppMessage("open server                   - Abre o servidor para conexões.", type_msg.CL_ONLY_CONSOLE));
                msg.push(new AppMessage("stop server                   - Coloca o servidor em manutenção.", type_msg.CL_ONLY_CONSOLE));
                msg.push(new AppMessage("open gm                       - Restringe acesso apenas para GMs/IPs registrados.", type_msg.CL_ONLY_CONSOLE));
                msg.push(new AppMessage("open all user                 - Libera o acesso para todos os jogadores.", type_msg.CL_ONLY_CONSOLE));
                msg.push(new AppMessage("---------------------------------------------------", type_msg.CL_ONLY_CONSOLE));
                msg.push(new AppMessage("create_user [on/off]          - Ativa ou desativa a criação de novas contas.", type_msg.CL_ONLY_CONSOLE));
                msg.push(new AppMessage("reload_system iff             - Recarrega as tabelas IFF no Login Service.", type_msg.CL_ONLY_CONSOLE));
                msg.push(new AppMessage("===================================================", type_msg.CL_ONLY_CONSOLE));

                return true;
            }
            else if (s.Equals("create_user", StringComparison.OrdinalIgnoreCase))
            {
                if (_command.Count > 1)
                {
                    string subCommand = _command.Dequeue();

                    if (subCommand.Equals("on", StringComparison.OrdinalIgnoreCase))
                    {
                        m_create_user_flag = true;
                        _smp.LogManager.Instance.push(new AppMessage("Create User ON", type_msg.CL_FILE_LOG_AND_CONSOLE));
                    }
                    else if (subCommand.Equals("off", StringComparison.OrdinalIgnoreCase))
                    {
                        m_create_user_flag = false;
                        _smp.LogManager.Instance.push(new AppMessage("Create User OFF", type_msg.CL_FILE_LOG_AND_CONSOLE));
                    }
                    else
                    {
                        _smp.LogManager.Instance.push(new AppMessage($"Unknown Command: \"create_user {subCommand}\"", type_msg.CL_ONLY_CONSOLE));
                    }
                }
            }
            else
            {
                _smp.LogManager.Instance.push(new AppMessage($"[{GetType().Name}::CheckCommand][Error] Command No Exist-> {s}", type_msg.CL_FILE_LOG_AND_CONSOLE));
                return false;
            }

            return false;
        }

        public void setIsUnderMaintenance(bool value)
        {
            IsUnderMaintenance = value;
        }

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
                packet.WriteInt32(m_si.UID);
                player.Send(packet, true);
                _smp.LogManager.Instance.push(new AppMessage($"[LoginService::OnClientConnected][Sucess] PLAYER[IP: {player.GetIP()}, OID: {player.ConnectionID}", 0));
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        protected override void OnClientDisconnected(IAppSession session)
        {
            if (session == null)
                throw new exception("[LoginService::onDisconnect][Error] _session is nullptr.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MESSAGE_SERVER, 60, 0));

            Player p = (Player)session;

            _smp.LogManager.Instance.push(new AppMessage($"[{GetType().Name}::OnClientDisconnected][Warning] PLAYER[ID: {p.UserInfo?.Login} UID: {p.UserInfo?.UID}]", type_msg.CL_FILE_LOG_AND_CONSOLE));
        }

        protected override bool CheckPacket(IAppSession session, Packet packet)
        {
            // Basic integrity check
            if (packet == null)
                return false;

            var type = (PacketIDClient)packet.Type;

            // 1. Connection-Level packets are always allowed
            if (type == PacketIDClient.CLIENT_REQ_LOGIN || type == PacketIDClient.CLIENT_REQ_RECONNECT)
            {
                return true;
            }

            // 2. Authorization Guard
            // If the session isn't authorized, drop any packet that isn't Connect/Reconnect
            if (!session.Authorized)
            {
                return false;
            }

            // 3. Authorized Packet Whitelist
            switch (type)
            { 
                case PacketIDClient.CLIENT_REQ_ENTER_SERVER:
                    break; 
                case PacketIDClient.CLIENT_REQ_SET_NICK:
                    break;
                case PacketIDClient.CLIENT_REQ_CONFIRM_SET_NICK:
                    break;
                case PacketIDClient.CLIENT_REQ_SET_CHARACTER:
                    break; 
            }
            return true;
        }

        protected override void OnHeartBeat()
        { 
            OnStart();
        }

        protected override void OnStart()
        {
            Console.Title = $"Login Service - P: {m_si.CurrentUsers}, Auth: {(m_unit_connect != null && m_unit_connect.isLive()? "ON": "OFF")}";
        }


        public override void LoadConfig()
        {
            base.LoadConfig();
            // Server Tipo
            m_si.Type = 0/*Login Server*/; 
            using (var m_reader_ini = ServerConfig.GetLoadConfigIni(ServerType))
            {
                m_access_flag = m_reader_ini.readInt("OPTION", "ACCESSFLAG") == 1;
                m_create_user_flag = m_reader_ini.readInt("OPTION", "CREATEUSER") == 1;

                try
                {
                    m_same_id_login_flag = m_reader_ini.readInt("OPTION", "SAME_ID_LOGIN") == 1;
                }
                catch
                {
                    // Não precisa printar mensagem por que essa opção é de desenvolvimento
                }
            }
        }

        protected void ReloadFiles()
        {
            LoadConfig();

            sIff.Instance.reload();
        }

        public override void authCmdShutdown(int _time_sec)
        {
            //base.authCmdShutdown(_time_sec);
        }

        public override void authCmdBroadcastNotice(string _notice)
        {
            //base.authCmdBroadcastNotice(_notice);
        }

        public override void authCmdBroadcastTicker(string _nickname, string _msg)
        {
            //base.authCmdBroadcastTicker(_nickname, _msg);
        }

        public override void authCmdBroadcastCubeWinRare(string _msg, uint _option)
        {
            //base.authCmdBroadcastCubeWinRare(_msg, _option);
        }

        public override void authCmdDisconnectPlayer(uint _req_server_uid, uint _player_uid, byte _force)
        {
            try
            {

                var s = _playerManager.FindPlayer(_player_uid);

                if (s != null)
                {

                    // Log
                    _smp.LogManager.Instance.push(new AppMessage("[LoginServer::authCmdDisconnectPlayer][log] Comando do Auth Server, Server[UID: " + (_req_server_uid)
                            + "] pediu para desconectar o Player[UID: " + (s.UserInfo.UID) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                    // Deconecta o Player
                    OnClientDisconnected(s);

                    // UPDATE ON Auth Server
                    m_unit_connect.SendConfirmDisconnectPlayer(_req_server_uid, _player_uid);

                }
                else
                    _smp.LogManager.Instance.push(new AppMessage("[LoginServer::authCmdDisconnectPlayer][WARNING] Comando do Auth Server, Server[UID: " + (_req_server_uid)
                            + "] pediu para desconectar o Player[UID: " + (_player_uid) + "], mas nao encontrou ele no server.", type_msg.CL_FILE_LOG_AND_CONSOLE));

            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[LoginServer::authCmdDisconnectPlayer][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void authCmdConfirmDisconnectPlayer(uint _player_uid)
        {

            try
            {

                var s = _playerManager.FindPlayer(_player_uid);

                if (s != null)
                {

                    // Loga com sucesso
                    Task.Run(()=> Handle_PLAYER_LOGIN.SUCCESS_LOGIN(s, 0));
                }
                else
                {

                    Task.Run(() => Handle_PLAYER_LOGIN.SUCCESS_LOGIN(s, 0));
                }
            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[LoginServer::authCmdConfirmDisconnectPlayer][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void authCmdNewMailArrivedMailBox(uint _player_uid, int _mail_id)
        {
            //base.authCmdNewMailArrivedMailBox(_player_uid, _mail_id);
        }

        public override void authCmdNewRate(uint _tipo, uint _qntd)
        {
            //base.authCmdNewRate(_tipo, _qntd);
        }

        public override void authCmdReloadGlobalSystem(uint _tipo)
        {
            //base.authCmdReloadGlobalSystem(_tipo);
        }

        public override void authCmdInfoPlayerOnline(uint _req_server_uid, uint _player_uid)
        {
            //base.authCmdInfoPlayerOnline(_req_server_uid, _player_uid);
        }

        public override void authCmdConfirmSendInfoPlayerOnline(uint _req_server_uid, AuthServerPlayerInfo _aspi)
        {
            try
            {

                var s = _playerManager.FindPlayer(_aspi.uid);

                if (s != null)
                {

                    //confirmLoginOnOtherServer(s, _req_server_uid, _aspi);

                }
                else
                    _smp.LogManager.Instance.push(new AppMessage("[LoginServer::authCmdConfirmSendInfoPlayerOnline][WARNING] Player[UID: " + (_aspi.uid)
                            + "] retorno do confirma login com Auth Server do Server[UID: " + (_req_server_uid) + "], mas o palyer nao esta mais conectado.", type_msg.CL_FILE_LOG_AND_CONSOLE));

            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[LoginServer::authCmdConfirmSendInfoPlayerOnline][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void authCmdSendCommandToOtherServer(Packet _packet)
        {
            //base.authCmdSendCommandToOtherServer(_packet);
        }

        public override void authCmdSendReplyToOtherServer(Packet _packet)
        {
            //base.authCmdSendReplyToOtherServer(_packet);
        }

        public override void sendCommandToOtherServerWithAuthServer(Packet _packet, uint _send_server_uid_or_type)
        {
           // base.sendCommandToOtherServerWithAuthServer(_packet, _send_server_uid_or_type);
        }

        public override void sendReplyToOtherServerWithAuthServer(Packet _packet, uint _send_server_uid_or_type)
        {
           // base.sendReplyToOtherServerWithAuthServer(_packet, _send_server_uid_or_type);
        }

        protected override void DBResponse(int _msg_id, Pangya_DB _pangya_db, object _arg)
        {
             
        }

        public override List<Player> FindAllGM()
        {
            return _playerManager.FindAllGM();
        }

        public override Player FindSessionByOid(int oid)
        {
            return _playerManager.FindSessionByOid(oid);
        }

        public override Player FindSessionByUid(uint uid)
        {
            return _playerManager.FindSessionByUID(uid);
        }

        public override List<Player> FindAllSessionByUid(uint uid)
        {
            return _playerManager.FindAllSessionByUid(uid);
        }

        public override Player FindSessionByNickname(string nickname)
        {
            return _playerManager.FindSessionByNickname(nickname);
        }

        public bool getAccessFlag() => m_access_flag;
        public bool getCreateUserFlag() => m_create_user_flag;
        public bool canSameIDLogin() => m_same_id_login_flag;

    }

    public class LoginServer : Singleton<LoginService>
    { }
}