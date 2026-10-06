using Pangya_MessengerServer.Flags;
using Pangya_MessengerServer.Handles;
using Pangya_MessengerServer.Manager;
using Pangya_MessengerServer.Models;
using Pangya_MessengerServer.Session;
using PangyaAPI.DataBase;
using PangyaAPI.IFF.Handle.JP;
using PangyaAPI.Network;
using PangyaAPI.Network.Core;
using PangyaAPI.Network.Handle;
using PangyaAPI.Network.Models;
using PangyaAPI.Network.Repository;
using PangyaAPI.Network.Security;
using PangyaAPI.Network.Service;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using PangyaAPI.Utilities.Models;
using System.Diagnostics;

namespace Pangya_MessengerServer.Server
{
    public class MessengerService : AppServer<Player, PacketIDClient>
    {
        private readonly PlayerManager _playerManager; 

        public MessengerService() : base(new PlayerManager(500), new PacketDispatcher<Player, PacketIDClient>(), ServerType.MessengerServer)
        {
            // Fazemos o cast do sessionManager para o seu PlayerManager
            _playerManager = (PlayerManager)SessionsManager;

            LoadConfig();

            RegisterHandlers();
        }

        private void RegisterHandlers()
        {
            // --- Conexão e Autenticação ---
            _dispatcher.Register(PacketIDClient.CLIENT_CONNECT_0x12, new Handle_PLAYER_LOGIN());
            _dispatcher.Register(PacketIDClient.CLIENT_NOTIFY_LOGOUT_0x16, new Handle_PLAYER_LOGOUT());
            _dispatcher.Register(PacketIDClient.CLIENT_REQ_CHECK_NICK_0x17, new Handle_PLAYER_CHECK_NICK());

            // --- Informações de Usuário e Amigos ---
            _dispatcher.Register(PacketIDClient.CLIENT_REQ_USERINFO_0x14, new Handle_FRIEND_GUILD_LIST());
            _dispatcher.Register(PacketIDClient.CLIENT_REQ_USERINFO_OFFLINE_0x13, new Handle_DUMMY());

            // --- Gerenciamento de Lista de Amigos ---
            _dispatcher.Register(PacketIDClient.CLIENT_REQ_REGISTER_FRIEND_0x18, new Handle_PLAYER_ADD_FRIEND());
            _dispatcher.Register(PacketIDClient.CLIENT_REQ_FRIEND_AGREE_0x19, new Handle_PLAYER_CONFIRM_FRIEND());
            _dispatcher.Register(PacketIDClient.CLIENT_REQ_FRIEND_REMOVE_0x1C, new Handle_PLAYER_DELETE_FRIEND());
            _dispatcher.Register(PacketIDClient.CLIENT_REQ_CHANGE_FRIENDALIAS_0x1F, new Handle_PLAYER_ASSING_NICK());

            // --- Privacidade e Bloqueio ---
            _dispatcher.Register(PacketIDClient.CLIENT_REQ_FRIEND_BLOCK_0x1A, new Handle_PLAYER_BLOCK_FRIEND());
            _dispatcher.Register(PacketIDClient.CLIENT_REQ_FRIEND_BLOCK_CANCEL_0x1B, new Handle_PLAYER_UNBLOCK_FRIEND());

            // --- Status e Localização ---
            _dispatcher.Register(PacketIDClient.CLIENT_NOTIFY_UPDATE_MY_STATUS_0x1D, new Handle_PLAYER_STATE());
            _dispatcher.Register(PacketIDClient.CLIENT_REQ_UPDATE_CHANNEL_INFO_0x23, new Handle_UPDATE_CHANNEL_INFO());

            // --- Chat ---
            _dispatcher.Register(PacketIDClient.CLIENT_REQ_CHAT_FRIEND_0x1E, new Handle_PLAYER_CHAT_FRIEND());
            _dispatcher.Register(PacketIDClient.CLIENT_REQ_CHAT_GUILD_0x25, new Handle_PLAYER_CHAT_GUILD());

            // --- Convites e Presentes ---
            _dispatcher.Register(PacketIDClient.CLIENT_NOTIFY_PLAYER_WAS_INVITED_ROOM_0x24, new Handle_PLAYER_INVITE_ROOM());
            _dispatcher.Register(PacketIDClient.CLIENT_NOTIFY_PLAYER_WAS_INVITED_ROOM_GUILD_BATTLE_0x28, new Handle_PLAYER_INVITE_GUILD_BATTLE());
            _dispatcher.Register(PacketIDClient.CLIENT_NOTIFY_PLAYER_GIFT_ITEM_0x29, new Handle_PLAYER_NOTIFY_GIFT_ITEM());

            // --- Sincronização de Guilda (Notificações do Sistema) ---
            _dispatcher.Register(PacketIDClient.CLIENT_NOTIFY_PLAYER_GUILD_JOINED_0x2A, new Handle_DUMMY());
            _dispatcher.Register(PacketIDClient.CLIENT_NOTIFY_PLAYER_GUILD_BANISH_0x2B, new Handle_DUMMY());
            _dispatcher.Register(PacketIDClient.CLIENT_NOTIFY_PLAYER_GUILD_SHIELD_CHANGED_0x2C, new Handle_DUMMY());
            _dispatcher.Register(PacketIDClient.CLIENT_NOTIFY_PLAYER_GUILD_NAME_CHANGED_0x2D, new Handle_DUMMY());
        }

        public override bool CheckCommand(Queue<string> _command)
        {
            Console.ResetColor();

            if (_command.Count == 0)
            {
                _smp.LogManager.Instance.push(new AppMessage("[MessengerServer::CheckCommand][Error] Missing parameter", type_msg.CL_ONLY_CONSOLE));
                return true;
            }

            string s = _command.Dequeue();

            if (!string.IsNullOrEmpty(s) && s == "exit")
            {
                Environment.Exit(-1);
                return true;
            }
            if (s.Equals("status", StringComparison.OrdinalIgnoreCase))
            {
                var process = Process.GetCurrentProcess();
                var memoryUsage = process.PrivateMemorySize64 / 1024 / 1024; // MB  
                _smp.LogManager.Instance.push(new AppMessage($"[{GetType().Name}::CheckCommand][Debug] STATUS[USERS: {Sessions?.Count() ?? 0}, MEMORY: {memoryUsage}, UPTIME: {DateTime.Now - process.StartTime}]", type_msg.CL_FILE_LOG_AND_CONSOLE));
                return true;
            }
            else if (!string.IsNullOrEmpty(s) && s == "reload_files")
            {
                ReloadFiles();
                return true;
            }
            else if (!string.IsNullOrEmpty(s) && s == "Rate")
            {
                string sTipo = _command.Dequeue();
                int tipo = -1;

                if (!string.IsNullOrEmpty(sTipo))
                {
                    switch (sTipo)
                    {
                        case "Pang": tipo = 0; break;
                        case "Experience": tipo = 1; break;
                        case "club": tipo = 2; break;
                        case "Rain": tipo = 3; break;
                        case "Treasure": tipo = 4; break;
                        case "Scratchy": tipo = 5; break;
                        case "pprareitem": tipo = 6; break;
                        case "ppcookieitem": tipo = 7; break;
                        case "memorial": tipo = 8; break;
                        default:
                            _smp.LogManager.Instance.push(new AppMessage($"[MessengerServer::checkCommand][Error] Unknown Command: \"Rate {sTipo}\"", type_msg.CL_ONLY_CONSOLE));
                            break;
                    }
                }
                else
                {
                    _smp.LogManager.Instance.push(new AppMessage($"[MessengerServer::checkCommand][Error] Unknown Command: \"Rate {sTipo}\"", type_msg.CL_ONLY_CONSOLE));
                }

                if (tipo != -1 && tipo >= 0 && tipo <= 8)
                {
                    if (uint.TryParse(_command.Dequeue(), out uint qntd) && qntd > 0)
                    {
                        UpdateRateAndEvent(tipo, qntd);
                    }
                    else
                    {
                        _smp.LogManager.Instance.push(new AppMessage($"[MessengerServer::checkCommand][Error] Unknown value, Command: \"Rate {sTipo}\"", type_msg.CL_ONLY_CONSOLE));
                    }
                }
                return true;
            }
            else if (!string.IsNullOrEmpty(s) && s == "event")
            {
                s = _command.Dequeue();
                uint qntd = 0;

                if (!string.IsNullOrEmpty(s))
                {
                    qntd = uint.Parse(_command.Dequeue());

                    switch (s)
                    {
                        case "grand_zodiac_event":
                            UpdateRateAndEvent(9, qntd);
                            break;
                        case "AngelEvent":
                            UpdateRateAndEvent(10, qntd);
                            break;
                        case "GrandPrixMode":
                            UpdateRateAndEvent(11, qntd);
                            break;
                        case "golden_time":
                            UpdateRateAndEvent(12, qntd);
                            break;
                        case "login_reward":
                            UpdateRateAndEvent(13, qntd);
                            break;
                        case "GMEventBot":
                            UpdateRateAndEvent(14, qntd);
                            break;
                        case "smart_calc":
                            UpdateRateAndEvent(15, qntd);
                            break;
                        default:
                            _smp.LogManager.Instance.push(new AppMessage($"[MessengerServer::checkCommand][Error] Unknown Comamnd: \"Event {s}\"", type_msg.CL_ONLY_CONSOLE));
                            break;
                    }
                }
                return true;
            }
            else if (!string.IsNullOrEmpty(s) && s == "reload_system")
            {
                string sTipo = _command.Dequeue();
                int tipo = -1;

                if (!string.IsNullOrEmpty(sTipo))
                {
                    switch (sTipo)
                    {
                        case "all": tipo = 0; break;
                        case "iff": tipo = 1; break;
                        case "card": tipo = 2; break;
                        case "comet_refill": tipo = 3; break;
                        case "PapelShop": tipo = 4; break;
                        case "box": tipo = 5; break;
                        case "MemorialShop": tipo = 6; break;
                        case "cube_coin": tipo = 7; break;
                        case "treasure_hunter": tipo = 8; break;
                        case "drop": tipo = 9; break;
                        case "attendance_reward": tipo = 10; break;
                        case "map_course": tipo = 11; break;
                        case "approach_mission": tipo = 12; break;
                        case "grand_zodiac_event": tipo = 13; break;
                        case "coin_cube_location": tipo = 14; break;
                        case "golden_time": tipo = 15; break;
                        case "login_reward": tipo = 16; break;
                        case "GMEventBot": tipo = 17; break;
                        case "smart_calc": tipo = 18; break;
                        default:
                            _smp.LogManager.Instance.push(new AppMessage($"[MessengerServer::checkCommand][Error] Unknown Command: \"reload_system {sTipo}\"", type_msg.CL_ONLY_CONSOLE));
                            break;
                    }
                }
                else
                {
                    _smp.LogManager.Instance.push(new AppMessage($"[MessengerServer::checkCommand][Error] Unknown Command: \"reload_system {sTipo}\"", type_msg.CL_ONLY_CONSOLE));
                }

                if (tipo != -1 && tipo >= 0 && tipo <= 18)
                {
                    ReloadGlobalSystem((uint)tipo);
                }
                return true;

            }
            else if (s == "cls" || s == "clear")
            {
                Console.Clear();
                ConsoleEx.Log();
                return true;
            }
            return false;
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
                var packet = new Packet(0x2E);
                packet.WriteByte(0);
                packet.WriteByte(0);
                packet.WriteInt32(player._ParseKey);
                player.Send(packet, true);
                _smp.LogManager.Instance.push(new AppMessage($"[{GetType().Name}::OnClientConnected][Sucess] PLAYER[IP: {player.GetIP()}, OID: {player.ConnectionID}", 0));
            }
            catch (exception ex)
            {
                _smp.LogManager.Instance.push(new AppMessage(
              $"[MessengerServer.OnClientConnected][ErrorSt]: {ex.getFullMessageError()}",
              type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        protected override void OnClientDisconnected(IAppSession session)
        {
            if (session == null)
                throw new exception("[MessengerService::OnClientDisconnected][Error] _session is nullptr.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MESSAGE_SERVER, 60, 0));

            Player p = (Player)session; 

            bool ret = false;

            try
            {
                if (Interlocked.CompareExchange(ref p.UserInfo.m_logout, p.UserInfo.m_logout, 0) == 0)
                {
                    ret = SendUpdatePlayerLogoutToFriends(p);
                }
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[MessengerService::OnClientDisconnecteded][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
            // Log para não mostrar essa mensagem 2x (evita spam se o logout já foi processado)
            if (ret)
                _smp.LogManager.Instance.push(new AppMessage($"[{GetType().Name}::OnClientDisconnected][Warning] PLAYER[ID: {p.UserInfo?.Login} UID: {p.UserInfo?.UID}]", type_msg.CL_FILE_LOG_AND_CONSOLE));
        }

        protected override bool CheckPacket(IAppSession session, Packet packet)
        {
            if (packet == null) return false;

            if ((PacketIDClient)packet.Type == PacketIDClient.CLIENT_CONNECT_0x12 || (PacketIDClient)packet.Type == PacketIDClient.CLIENT_REQ_USERINFO_0x14)
            {
                return true;
            }

            if (!session.Authorized)
            {
                return false;
            }

            switch ((PacketIDClient)packet.Type)
            { 
                case PacketIDClient.CLIENT_REQ_USERINFO_OFFLINE_0x13:
                    
                    
                case PacketIDClient.CLIENT_NOTIFY_LOGOUT_0x16:
                    
                case PacketIDClient.CLIENT_REQ_CHECK_NICK_0x17:
                    
                case PacketIDClient.CLIENT_REQ_REGISTER_FRIEND_0x18:
                    
                case PacketIDClient.CLIENT_REQ_FRIEND_AGREE_0x19:
                    
                case PacketIDClient.CLIENT_REQ_FRIEND_BLOCK_0x1A:
                    
                case PacketIDClient.CLIENT_REQ_FRIEND_BLOCK_CANCEL_0x1B:
                    
                case PacketIDClient.CLIENT_REQ_FRIEND_REMOVE_0x1C:
                    
                case PacketIDClient.CLIENT_NOTIFY_UPDATE_MY_STATUS_0x1D:
                    
                case PacketIDClient.CLIENT_REQ_CHAT_FRIEND_0x1E:
                    
                case PacketIDClient.CLIENT_REQ_CHANGE_FRIENDALIAS_0x1F:
                    
                case PacketIDClient.CLIENT_REQ_UPDATE_CHANNEL_INFO_0x23:
                    
                case PacketIDClient.CLIENT_NOTIFY_PLAYER_WAS_INVITED_ROOM_0x24:
                    
                case PacketIDClient.CLIENT_REQ_CHAT_GUILD_0x25:
                    
                case PacketIDClient.CLIENT_NOTIFY_PLAYER_WAS_INVITED_ROOM_GUILD_BATTLE_0x28:
                    
                case PacketIDClient.CLIENT_NOTIFY_PLAYER_GIFT_ITEM_0x29:
                    
                case PacketIDClient.CLIENT_NOTIFY_PLAYER_GUILD_JOINED_0x2A:
                    
                case PacketIDClient.CLIENT_NOTIFY_PLAYER_GUILD_BANISH_0x2B:
                    
                case PacketIDClient.CLIENT_NOTIFY_PLAYER_GUILD_SHIELD_CHANGED_0x2C:
                    
                case PacketIDClient.CLIENT_NOTIFY_PLAYER_GUILD_NAME_CHANGED_0x2D: 
                    return true;
            }

            return false;
        }

        protected override void OnHeartBeat()
        {
            OnStart();
        }

        protected override void OnStart()
        {
            Console.Title = $"Messenger Service - P: {m_si.CurrentUsers}, Auth: {(m_unit_connect != null && m_unit_connect.isLive()? "ON": "OFF")}";
        }
         
        public override async void LoadConfig()
        {
            base.LoadConfig();

            // Tipo Server
            m_si.Type = 3;


            // Recupera Valores de Rate do server do banco de dados
            var cmd_rci = new CmdRateConfigInfo(m_si.UID);  // Waiter

            if (cmd_rci.getException().getCodeError() != 0 || cmd_rci.isError()/*Deu erro na consulta não tinha o Rate config info para esse gs, pode ser novo*/)
            {

                if (cmd_rci.getException().getCodeError() != 0)
                    _smp.LogManager.Instance.push(new AppMessage("[MessengerService::config_init][ErrorSystem] " + cmd_rci.getException().getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                _smp.LogManager.Instance.push(new AppMessage("[MessengerService::config_init][Error] nao conseguiu recuperar os valores de Rate do server[UID="
                        + (m_si.UID) + "] no banco de dados. Utilizando valores padroes de rates.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                m_si.Rate.Scratchy = 100;
                m_si.Rate.PapelShopRareItem = 100;
                m_si.Rate.PapelShopCookieItem = 100;
                m_si.Rate.Treasure = 100;
                m_si.Rate.MemorialShop = 100;
                m_si.Rate.Rain = 100;
                m_si.Rate.GrandZodiacEventTime = 1; // Ativo por padr�o
                m_si.Rate.GrandPrixEvent = 1;        // Ativo por padr�o
                m_si.Rate.GoldenTimeEvent = 1;       // Ativo por padr�o
                m_si.Rate.LoginRewardEvent = 1;      // Ativo por padr�o
                m_si.Rate.GMEventBot = 1;            // Ativo por padr�o
                m_si.Rate.SmartCalculation = 0;        // Atibo por padr�o

                m_si.Rate.AngelEvent = 0;             // Desativado por padr�o
                m_si.Rate.Pang = 0;
                m_si.Rate.Experience = 0;
                m_si.Rate.ClubMastery = 0;

                // Atualiza no banco de dados
               snmdb.NormalManagerDB.Instance.add(2, new CmdUpdateRateConfigInfo(m_si.UID, m_si.Rate), DBResponse, this);

            }
            else
            {   // Conseguiu recuperar com sucesso os valores do server

                m_si.Rate.Scratchy = cmd_rci.getInfo().Scratchy;
                m_si.Rate.PapelShopRareItem = cmd_rci.getInfo().PapelShopRareItem;
                m_si.Rate.PapelShopCookieItem = cmd_rci.getInfo().PapelShopCookieItem;
                m_si.Rate.Treasure = cmd_rci.getInfo().Treasure;
                m_si.Rate.MemorialShop = cmd_rci.getInfo().MemorialShop;
                m_si.Rate.Rain = cmd_rci.getInfo().Rain;
                m_si.Rate.GrandZodiacEventTime = cmd_rci.getInfo().GrandZodiacEventTime;
                m_si.Rate.GrandPrixEvent = cmd_rci.getInfo().GrandPrixEvent;
                m_si.Rate.GoldenTimeEvent = cmd_rci.getInfo().GoldenTimeEvent;
                m_si.Rate.LoginRewardEvent = cmd_rci.getInfo().LoginRewardEvent;
                m_si.Rate.GMEventBot = cmd_rci.getInfo().GMEventBot;
                m_si.Rate.SmartCalculation = cmd_rci.getInfo().SmartCalculation;

                m_si.Rate.AngelEvent = cmd_rci.getInfo().AngelEvent;
                m_si.Rate.Pang = cmd_rci.getInfo().Pang;
                m_si.Rate.Experience = cmd_rci.getInfo().Experience;
                m_si.Rate.ClubMastery = cmd_rci.getInfo().ClubMastery;
            }
        }

        protected void ReloadFiles()
        {
            base.LoadConfig();
            LoadConfig();

            // Reload All Globals Systems
            ReloadSystem();

            _smp.LogManager.Instance.push(new AppMessage("[MessengerServer::ReloadFiles][Log] Reload System now sucess!", type_msg.CL_FILE_LOG_AND_CONSOLE));

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
                    _smp.LogManager.Instance.push(new AppMessage("[MessengerServer::authCmdDisconnectPlayer][log] Comando do Auth Server, Server[UID=" + (_req_server_uid)
                            + "] pediu para desconectar o PLAYER[UID=" + (s.UserInfo.UID) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                    // Deconecta o Player
                    if (_force == 1) // Força o Disconect do player, sem verificar as regras do Game Server
                        OnClientDisconnected(s);
                    else
                    { 

                            OnClientDisconnected(s);
                    }

                }
                else
                {

                    // Não encontrou o player no server, então desconecta no banco de dados
                    snmdb.NormalManagerDB.Instance.add(5, new CmdRegisterLogon(_player_uid, 1/*Logout*/), DBResponse, this);

                    // Log
                    _smp.LogManager.Instance.push(new AppMessage("[MessengerServer::authCmdDisconnectPlayer][Warning] Comando do Auth Server, Server[UID=" + (_req_server_uid)
                            + "] pediu para desconectar o PLAYER[UID=" + (_player_uid) + "], mas nao encontrou ele no server, entao desconecta ele no banco de dados.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                }

                // UPDATE ON Auth Server
                m_unit_connect.SendConfirmDisconnectPlayer(_req_server_uid, _player_uid);

            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[MessengerServer::authCmdDisconnectPlayer][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void authCmdConfirmDisconnectPlayer(uint _player_uid)
        {
            //base.authCmdConfirmDisconnectPlayer(_player_uid);
        }

        public override void authCmdNewMailArrivedMailBox(uint _player_uid, int _mail_id)
        {
            //base.authCmdNewMailArrivedMailBox(_player_uid, _mail_id);
        }

        public override void authCmdNewRate(uint _tipo, uint _qntd)
        {
            try
            {

               // UpdateRateAndEvent((int)_tipo, _qntd);

            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[MessengerServer::authCmdNewRate][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void authCmdReloadGlobalSystem(uint _tipo)
        {
            try
            {
                //ReloadGlobalSystem(_tipo);
            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[MessengerServer::authCmdReloadGlobalSystem][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
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

                    confirmLoginOnOtherServer(s, _req_server_uid, _aspi);

                }
                else
                    _smp.LogManager.Instance.push(new AppMessage("[MessengerServer::authCmdConfirmSendInfoPlayerOnline][Warning] PLAYER[UID=" + (_aspi.uid)
                            + "] retorno do confirma login com Auth Server do Server[UID=" + (_req_server_uid) + "], mas o palyer nao esta mais conectado.", type_msg.CL_FILE_LOG_AND_CONSOLE));

            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[MessengerServer::authCmdConfirmSendInfoPlayerOnline][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
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
            //base.sendCommandToOtherServerWithAuthServer(_packet, _send_server_uid_or_type);
        }

        public override void sendReplyToOtherServerWithAuthServer(Packet _packet, uint _send_server_uid_or_type)
        {
            //base.sendReplyToOtherServerWithAuthServer(_packet, _send_server_uid_or_type);
        }

        public void confirmLoginOnOtherServer(Player _session, uint _req_server_uid, AuthServerPlayerInfo _aspi)
        {
            // Usar o 'using' garante que o buffer do pacote seja liberado da memória (importante no Linux/Docker)
            using (var p = new Packet())
            {
                try
                {

                    // Validações de Segurança
                    if (_aspi.uid != _session.UserInfo.UID ||
                        _aspi.option != 1 ||
                        _aspi.id != _session.UserInfo.Login ||
                        _aspi.ip != _session.GetIP())
                    {
                        goto send_error;
                    }

                    // --- Bloco de SUCESSO ---

                    // Inicializa lista de amigos
                    _session.UserInfo.m_friend_manager.init(_session.UserInfo);

                    // Estado 4 = Online/Lobby
                    _session.UserInfo.m_state = 4;
                    _session.Authorized = true;

                    _smp.LogManager.Instance.push(new AppMessage($"[MessengerServer] Player[UID={_session.UserInfo.UID}] logou com sucesso!", type_msg.CL_FILE_LOG_AND_CONSOLE));

                    // Resposta de Sucesso (0x2F)
                    p.init_plain(0x2F);
                    p.WriteByte(0); // OK
                    p.WriteUInt32(_session.UserInfo.UID);

                    _session.Send(p); 
                    return; // IMPORTANTE: Sai do método aqui para não executar o erro abaixo!

                send_error:
                    // --- Bloco de ERRO ---
                    p.init_plain(0x2F);
                    p.WriteByte(1); // Error (Geralmente 1 ou 2 dependendo do cliente)
                    _session.Send(p);

                    // Fecha a conexão de forma segura
                    _session.Disconnect();
                }
                catch (Exception e)
                {
                    _smp.LogManager.Instance.push(new AppMessage($"[MessengerServer::confirmLogin] Error: {e.Message}", type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
            }
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

        public void FriendBroadcast(Dictionary<uint, Player> _m_player, Player _s, Packet _p)
        {
            if (_m_player == null || _m_player.Count == 0)
            {
                _m_player = MessengerServer.Instance.FindAllGuildMember(_s.UserInfo.GuildIndex);
            }

            foreach (var el in _m_player)
            {
                if (el.Value != null && el.Value != _s)
                {
                    el.Value.Send(_p);
                }
            }
        } 

        public Dictionary<uint, Player> FindAllGuildMember(uint club_id)
        {
            return _playerManager.FindAllGuildMember(club_id);
        }

        internal Player FindPlayer(uint member_uid)
        {
            return _playerManager.FindPlayer(member_uid, false);
        }

        public void SendUpdatedFriendList(Player target)
        {
            if (target == null || !target.Connected) return;

            var p = new Packet();
            var friend_list = target.UserInfo.m_friend_manager.getAllFriendAndGuildMember();

            // Usando sua constante FRIEND_PAG_LIMIT
            var mp = new ManyPacket((ushort)friend_list.Count, 30);

            if (mp.paginas > 0)
            {
                for (var i = 0; i < mp.paginas; i++, mp.increse())
                {
                    p.init_plain((ushort)0x30);
                    p.WriteUInt16(0x102);   // Sub Packet Id
                    p.WriteBytes(mp.pag.ToArray());

                    // Filtra os amigos da página atual
                    var _begin = friend_list.Skip(mp.index.start).Take(mp.index.end - mp.index.start);

                    foreach (var friend in _begin)
                    {
                        p.WriteBytes(friend.ToArray());

                        // Verifica se o amigo em questão está online no Messenger
                        var s_friend = (Player)FindSessionByUid(friend.uid);
                        FriendInfoEx pFi = null;

                        // Se o amigo está online E não bloqueou o 'target'
                        if (s_friend != null && (pFi = s_friend.UserInfo.m_friend_manager.findFriendInAllFriend(target.UserInfo.UID)) != null && !pFi.state.block.IsTrue())
                        {
                            p.WriteBytes(s_friend.UserInfo.m_cpi.ToArray());
                            p.WriteByte(s_friend.UserInfo.m_state);

                            // Atualiza o bitmask de estado no objeto local antes de enviar
                            friend.state.online = 1;
                            switch (s_friend.UserInfo.m_state)
                            {
                                case 0: friend.state.play = 1; break;
                                case 1: friend.state.AFK = 1; break;
                                case 3: friend.state.busy = 1; break;
                                default: friend.state.online = 1; break;
                            }
                        }
                        else
                        {
                            // Amigo Offline
                            p.WriteInt16(-1);      // Sala
                            p.WriteInt32(-1);      // Tipo Sala
                            p.WriteInt32(-1);      // Server GUID
                            p.WriteSByte(-1);       // Canal ID
                            p.WriteZero(64);   // Nome Canal
                            p.WriteByte(5);        // Ícone OFFLINE
                            friend.state.online = 0;
                        }

                        p.WriteByte(friend.cUnknown_flag);

                        // Lógica de ServerFlag (Master/Sub/Membro/Level)
                        byte flagValue = (friend.flag.ucFlag == 2)
                            ? (byte)(friend.uid == target.UserInfo.UID ? 1 : 0)
                            : friend.level;

                        p.WriteByte(flagValue);
                        p.WriteByte(friend.state.ucState);
                        p.WriteByte(friend.flag.ucFlag);
                    }

                    target.Send(p);
                }
            }
            else
            {
                // Envia página vazia se não tiver amigos/Guild
                p.init_plain((ushort)0x30);
                p.WriteUInt16(0x102);
                p.WriteBytes(mp.pag.ToArray());
                target.Send(p);
            }
        }

        public bool SendUpdatePlayerLogoutToFriends(Player _session)
        {
            bool ret = true;
            var p = new Packet();
            try
            {

                /* Lógica Atômica:
            Tenta mudar m_logout de 0 para 1.
            Se o retorno for 1, significa que outra Thread já passou por aqui.
         */
                if (_session.UserInfo.m_logout == 0)
                {
                    _session.UserInfo.m_logout = 1;
                    return false;
                }

                // Resposta para os amigos do player, que ele deslogou
                p.init_plain(0x30);

                p.WriteUInt16(0x10F); // Sub packet Id

                p.WriteUInt32(_session.UserInfo.UID);

                FriendBroadcast(_playerManager.FindAllFriend(_session.UserInfo.m_friend_manager.getAllFriendAndGuildMember(true/*Not Send To Block Friend*/)), _session, p);

                _smp.LogManager.Instance.push(new AppMessage("[MessengerService::SendUpdatePlayerLogoutToFriends][Log] PLAYER[ID: " + (_session.UserInfo.Login) + ", UID: " + (_session.UserInfo.UID) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[MessengerService::SendUpdatePlayerLogoutToFriends][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Error
                ret = false;
            }

            return ret;
        }

        public Dictionary<uint, Player> FindAllFriend(List<FriendInfoEx> friendInfoExes)
        {
            return _playerManager.FindAllFriend(friendInfoExes);
        }

        private void ReloadSystem()
        {
            // Recarrega IFF_STRUCT
            sIff.Instance.reload(); 
        }

        private void ReloadGlobalSystem(uint _tipo)
        {
            try
            {
                switch (_tipo)
                {
                    case 0:     // Reload All Globals Systems
                        ReloadSystem();
                        break;

                    case 1:     // IFF
                                // Recarrega IFF_STRUCT
                        sIff.Instance.reload();
                        break;
                    case 2:     // Card
                    case 3:     // Comet Refill
                    case 4:     // Papel Shop
                    case 5:     // Box
                    case 6:     // Memorial Shop
                    case 7:     // Cube e Coin
                    case 8:     // Treasure Hunter
                    case 9:     // Drop
                    case 10:    // Attendance Reward
                    case 11:    // Map Course Dados
                    case 12:    // Approach Mission
                    case 13:    // Grand Zodiac Event
                    case 14:    // Coin Cube Location Update System
                    case 15:    // Golden Time System
                    case 16:    // Login Reward System
                    case 17:    // Bot GM Event
                                // N�o tem esses Systemas aqui
                        break;
                    case 18:    // Smart Calculator Lib
                                // Recarrega Smart Calculator Lib
                                // sSmartCalculator.Instance.load();
                        break;

                    default:
                        throw new Exception($"[MessengerServer::reloadGlobalSystem][Error] Tipo[VALUE={_tipo}] desconhecido.");
                }

                // Log
                _smp.LogManager.Instance.push(
                     new AppMessage($"[MessengerServer::reloadGlobalSystem][Error] Recarregou o Sistema[Tipo={_tipo}] com sucesso!", type_msg.CL_FILE_LOG_AND_CONSOLE)
                 );
            }
            catch (Exception e)
            {
                _smp.LogManager.Instance.push(
                     new AppMessage($"[MessengerServer::reloadGlobalSystem][ErrorSystem] {e.Message}", type_msg.CL_FILE_LOG_AND_CONSOLE)
                 );
            }
        }


        // Update Rate e Event of Server

        public void UpdateRateAndEvent(int _tipo, uint _qntd)
        {
            try
            {

                if (_qntd == 0u && _tipo != 9/*Grand Zodiac Event Time*/ && _tipo != 10/*Angel Event*/
                    && _tipo != 11/*Grand Prix Event*/ && _tipo != 12/*Golden Time Event*/ && _tipo != 13/*Login Reward Event*/
                    && _tipo != 14/*Bot GM Event*/ && _tipo != 15/*Smart Calculator*/)
                    throw new exception("[MessengerServer::UpdateRateAndEvent][Error] Rate[TIPO=" + (_tipo) + ", QNTD="
                            + (_qntd) + "], qntd is invalid(zero).", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 120, 0));

                switch (_tipo)
                {
                    case 0: // Pang
                    case 1: // Exp
                    case 2: // Mastery
                    case 3: // Chuva
                    case 4: // Treasure Hunter
                    case 5: // Scratchy
                    case 6: // Papel Shop Rare Item
                    case 7: // Papel Shop Cookie Item
                    case 8: // Memorial shop
                    case 9: // Event Grand Zodiac Time Event [Active/Desactive]
                    case 10: // Event Angel (Reduce 1 quit per game done)
                    case 11: // Grand Prix Event
                    case 12: // Golden Time Event
                    case 13: // Login Reward System Event
                    case 14: // Bot GM Event
                    case 15: // Smart Calculator
                        {
                            m_si.Rate.SmartCalculation = (short)_qntd;

                            // Recarrega o Smart Calculator System se ele foi ativado
                            if (m_si.Rate.SmartCalculation == 1)
                                ReloadGlobalSystem(18/*Smart Calculator*/);

                            break;
                        }
                    default:
                        throw new exception("[MessengerServer::UpdateRateAndEvent][Error] troca Rate[TIPO=" + (_tipo) + ", QNTD="
                                + (_qntd) + "], Type desconhecido.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 120, 0));
                }

                // Update no DB os server do server que foram alterados
                snmdb.NormalManagerDB.Instance.add(2, new CmdUpdateRateConfigInfo(m_si.UID, m_si.Rate), DBResponse, this);

                // Log
                _smp.LogManager.Instance.push(new AppMessage("[MessengerServer::UpdateRateAndEvent][Error] New Rate[Tipo=" + (_tipo) + ", QNTD="
                        + (_qntd) + "] com sucesso!", type_msg.CL_FILE_LOG_AND_CONSOLE));


            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[MessengerServer::UpdateRateAndEvent][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }



        protected override void DBResponse(int _msg_id, Pangya_DB _pangya_db, object _arg)
        {
            if (_arg == null)
            {
                _smp.LogManager.Instance.push(new AppMessage("[MessengerService::DBResponse][WARNING] _arg is nullptr, na msg_id = " + (_msg_id), type_msg.CL_FILE_LOG_AND_CONSOLE));
                return;
            }

            // Por Hora s� sai, depois fa�o outro Type de tratamento se precisar
            if (_pangya_db.getException().getCodeError() != 0)
            {
                _smp.LogManager.Instance.push(new AppMessage("[MessengerService::DBResponse][Error] " + _pangya_db.getException().getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                return;
            }

            switch (_msg_id)
            {
                case 1: // Insert Block IP
                    {
                        var cmd_ibi = (CmdInsertBlockIp)(_pangya_db); 

                        break;
                    }
                case 2: // Update Server Rate Config Info
                    {

                        var cmd_urci = (CmdUpdateRateConfigInfo)(_pangya_db); 

                        break;
                    }
                case 0:
                default:
                    break;
            }
        }

    }

    public class MessengerServer : Singleton<MessengerService>
    { }
}