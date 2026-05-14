using Pangya_LoginServer.DataBase;
using Pangya_LoginServer.Models;
using Pangya_LoginServer.PacketFunc;
using Pangya_LoginServer.PangyaEnums;
using Pangya_LoginServer.Session;
using PangyaAPI.IFF.JP.Extensions;
using PangyaAPI.Network.Models;
using PangyaAPI.Network.PangyaPacket;
using PangyaAPI.Network.PangyaServer;
using PangyaAPI.Network.PangyaSession;
using PangyaAPI.Network.PangyaUtil;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Models;
using PangyaAPI.Utilities.Log;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Pangya_LoginServer.LoginServerTcp
{
    public class LoginServer : Server
    {
#if DEBUG

        bool MOD_TEST = true;
#else
        bool MOD_TEST = false;
#endif
        private static readonly Regex InvalidIdRegex =
   new Regex(@".*[\^$&,\\?`´~\|""@#¨'%*!\\].*", RegexOptions.Compiled);
        bool m_access_flag;
        bool m_create_user_flag;
        bool m_same_id_login_flag;
        static player_manager m_player_manager = new player_manager();

        public bool IsUnderMaintenance { get; private set; }

        public LoginServer() : base(m_player_manager)
        {
            if (m_state == ServerState.Failure)
            {
                _smp.message_pool.getInstance().push(new message("[LoginServer::LoginServer][Error] falha ao incializar o message server.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                return;
            }

            try
            {
                config_init();

                // Carrega IFF_STRUCT
                if (!sIff.getInstance().isLoad())
                    sIff.getInstance().initilation();

                // Request Cliente
                init_Packets();

                // Initialized complete
                m_state = ServerState.Initialized;
            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[LoginServer::LoginServer][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                m_state = ServerState.Failure;
            }
        }

        public override bool CheckPacket(PangyaAPI.Network.PangyaSession.Session _session, packet packet, int opt = 0)
        {
            var player = (Player)_session;
            var packetId = packet.Id;
            var uid = player.m_pi.uid;


            switch (opt)
            {
                case 1:
                    // Verifica se o valor de packetId é válido no enum PacketIDClient
                    if (Enum.IsDefined(typeof(PacketIDClient), (PacketIDClient)packetId))
                    {
                        _smp.message_pool.getInstance().push(new message($"[{GetType().Name}::CheckPacket][Debug] PLAYER[UID: " + (uid == 0 ? player.m_ip : uid.ToString()) + ", PID: " + (PacketIDClient)packetId + "]", type_msg.CL_ONLY_CONSOLE));
                        return true;
                    }
                    else// nao tem no PacketIDClient
                    {
                        _smp.message_pool.getInstance().push(new message($"[{GetType().Name}::CheckPacket][Info]: PLAYER[UID: {player.m_pi.uid}, CGPID: 0x{packet.Id:X}]", type_msg.CL_ONLY_CONSOLE));
                        return true;
                    }
                default:
                    // Verifica se o valor de packetId é válido no enum PacketIDServer
                    if (Enum.IsDefined(typeof(PacketIDServer), (PacketIDServer)packetId))
                    {
                        Debug.WriteLine($"[{GetType().Name}::CheckPacket][Info]: PLAYER[UID: {player.m_pi.uid}, SGPID: {(PacketIDServer)packetId}]", ConsoleColor.Cyan);
                        return true;
                    }
                    else// nao tem no PacketIDServer
                    {
                        Debug.WriteLine($"[{GetType().Name}::CheckPacket][Info]: PLAYER[UID: {player.m_pi.uid}, SGPID: 0x{packet.Id:X}]");
                        return true;
                    }
            }
        }

        public override void onDisconnected(PangyaAPI.Network.PangyaSession.Session _session)
        {
            if (_session == null)
                throw new exception("[LoginServer::onDisconnect][Error] _session is nullptr.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MESSAGE_SERVER, 60, 0));

            Player p = (Player)_session;

            _smp.message_pool.getInstance().push(new message("[LoginServer::onDisconnected][Log] PLAYER[ID: " + (p.m_pi.id) + ", UID: " + (p.m_pi.uid) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));
        }

        public override void OnHeartBeat()
        {
            // Aqui depois tenho que colocar uma verifica��o que eu queira fazer no server
            // Esse fun��o � chamada na thread monitor

            try
            {

                // Server ainda n�o est� totalmente iniciado
                if (m_state != ServerState.Initialized)
                    return;

                // Begin Check System Singleton Static

                // Carrega Smart Calculator Lib, S� inicializa se ele estiver ativado
                //if (m_si.rate.smart_calculator && !sSmartCalculator::getInstance().hasStopped() && !sSmartCalculator::getInstance().isLoad())
                //    sSmartCalculator::getInstance().Load();

                // End Check System Singleton Static

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[LoginServer::onHeartBeat][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            return;
        }

        public override void OnStart()
        {
            Console.Title = $"Login Server - P: {m_si.curr_user}";
            m_state = ServerState.Initialized;
        }

        protected override void onAcceptCompleted(PangyaAPI.Network.PangyaSession.Session _session)
        {
            try
            {
                var _packet = new packet(0x0);    // Tipo Packet Login Server initial packet no compress e no crypt

                _packet.AddInt32(_session.m_key); // key
                _packet.AddInt32(m_si.uid);                 // Server UID

                _packet.makeRaw();
                var mb = _packet.getBuffer();
                _session.requestSendBuffer(mb, true);
            }
            catch (Exception ex)
            {
                _smp.message_pool.getInstance().push(new message(
              $"[LoginServer.onAcceptCompleted][ErrorSt] {ex.Message}\nStack Trace: {ex.StackTrace}",
              type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        /// <summary>
        /// init packet to call !
        /// </summary>
        protected void init_Packets()
        {
            packet_func_ls.funcs.addPacketCall(0x01, packet_func_ls.packet001, this);
            packet_func_ls.funcs.addPacketCall(0x03, packet_func_ls.packet003, this);
            packet_func_ls.funcs.addPacketCall(0x04, packet_func_ls.packet004, this);
            packet_func_ls.funcs.addPacketCall(0x06, packet_func_ls.packet006, this);
            packet_func_ls.funcs.addPacketCall(0x07, packet_func_ls.packet007, this);
            packet_func_ls.funcs.addPacketCall(0x08, packet_func_ls.packet008, this);
            packet_func_ls.funcs.addPacketCall(0x0B, packet_func_ls.packet00B, this);

            packet_func_ls.funcs_sv.addPacketCall(0x00, packet_func_ls.packet_svFazNada, this);
            packet_func_ls.funcs_sv.addPacketCall(0x01, packet_func_ls.packet_svFazNada, this);
            packet_func_ls.funcs_sv.addPacketCall(0x02, packet_func_ls.packet_svFazNada, this);
            packet_func_ls.funcs_sv.addPacketCall(0x03, packet_func_ls.packet_sv003, this);
            packet_func_ls.funcs_sv.addPacketCall(0x06, packet_func_ls.packet_sv006, this);
            packet_func_ls.funcs_sv.addPacketCall(0x09, packet_func_ls.packet_svFazNada, this);
            packet_func_ls.funcs_sv.addPacketCall(0x0E, packet_func_ls.packet_svFazNada, this);
            packet_func_ls.funcs_sv.addPacketCall(0x0F, packet_func_ls.packet_svFazNada, this);
            packet_func_ls.funcs_sv.addPacketCall(0x10, packet_func_ls.packet_svFazNada, this);
            packet_func_ls.funcs_sv.addPacketCall(0x11, packet_func_ls.packet_svFazNada, this);

            // Auth Server
            packet_func_ls.funcs_as.addPacketCall(0x01, packet_func_ls.packet_as001, this);

            // Initialized complete

        }


        public override void config_init()
        {
            base.config_init();
            // Server Tipo
            m_si.tipo = 0/*Login Server*/;

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

        protected void ReloadFiles()
        {
            config_init();

            sIff.getInstance().reload();
        }

        public override void authCmdShutdown(int _time_sec)
        {
            try
            {

                // Shut down com tempo
                if (m_shutdown == null)
                {

                    // Log
                    _smp.message_pool.getInstance().push(new message("[LoginServer::authCmdShutdown][Log] Auth Server requisitou para o server ser desligado em "
                            + _time_sec + " segundos", type_msg.CL_FILE_LOG_AND_CONSOLE));

                    shutdown_time(_time_sec);

                }
                else
                    _smp.message_pool.getInstance().push(new message("[LoginServer::authCmdShutdown][WARNING] Auth Server requisitou para o server ser delisgado em "
                            + _time_sec + " segundos, mas o server ja esta com o timer de shutdown", type_msg.CL_FILE_LOG_AND_CONSOLE));

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[LoginServer::authCmdShutdown][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }


        public override void shutdown_time(int _time_sec)
        {

            if (_time_sec <= 0) // Desliga o Server Imediatemente
                base.shutdown();
            else
            {
                // Se o Shutdown Timer estiver criado descria e cria um novo
                if (m_shutdown != null)
                {

                    // Para o Tempo se ele não estiver parado
                    if (m_shutdown.getState() != PangyaSyncTimer.TIMER_STATE.STOPPED)
                        m_shutdown.Stop();

                    m_timer_mgr.DeleteTimer(m_shutdown);
                }

                if ((m_shutdown = m_timer_mgr.CreateTimer((uint)(_time_sec * 1000), () => base.end_time_shutdown(this, 0))) == null)
                    throw new exception("[LoginServer::shutdown_time][Error] nao conseguiu criar o timer", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.LOGIN_SERVER, 51, 0));
            }
        }

        public override void authCmdBroadcastNotice(string _notice)
        {
            // Message Server n�o usa esse Comando
            return;
        }

        public override void authCmdBroadcastTicker(string _nickname, string _msg)
        {
            // Message Server n�o usa esse Comando
            return;
        }

        public override void authCmdBroadcastCubeWinRare(string _msg, uint _option)
        {
            // Message Server n�o usa esse Comando
            return;
        }

        public override void authCmdDisconnectPlayer(uint _req_server_uid, uint _player_uid, byte _force)
        {
            try
            {

                var s = m_player_manager.findPlayer(_player_uid);

                if (s != null)
                {

                    // Log
                    _smp.message_pool.getInstance().push(new message("[LoginServer::authCmdDisconnectPlayer][log] Comando do Auth Server, Server[UID: " + (_req_server_uid)
                            + "] pediu para desconectar o Player[UID: " + (s.m_pi.uid) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                    // Deconecta o Player
                    DisconnectSession(s);

                    // UPDATE ON Auth Server
                    m_unit_connect.sendConfirmDisconnectPlayer(_req_server_uid, _player_uid);

                }
                else
                    _smp.message_pool.getInstance().push(new message("[LoginServer::authCmdDisconnectPlayer][WARNING] Comando do Auth Server, Server[UID: " + (_req_server_uid)
                            + "] pediu para desconectar o Player[UID: " + (_player_uid) + "], mas nao encontrou ele no server.", type_msg.CL_FILE_LOG_AND_CONSOLE));

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[LoginServer::authCmdDisconnectPlayer][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void authCmdConfirmDisconnectPlayer(uint _player_uid)
        {
            try
            {

                var s = m_player_manager.findPlayer(_player_uid);

                if (s != null)
                {

                    // Loga com sucesso
                    packet_func_ls.succes_login(this, s);
                }
                else
                {
                    packet_func_ls.succes_login(this, s);
                }
            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[LoginServer::authCmdConfirmDisconnectPlayer][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void authCmdNewMailArrivedMailBox(uint _player_uid, int _mail_id)
        {
            // Message Server n�o usa esse Comando
            return;
        }

        public override void authCmdNewRate(uint _tipo, uint _qntd)
        {
            // Message Server n�o usa esse Comando
            return;
        }

        public override void authCmdReloadGlobalSystem(uint _tipo)
        {
            // Message Server n�o usa esse Comando
            return;
        }

        public override void authCmdInfoPlayerOnline(uint _req_server_uid, uint _player_uid)
        {
            // Message Server n�o usa esse Comando
            return;
        }

        public override void authCmdConfirmSendInfoPlayerOnline(uint _req_server_uid, AuthServerPlayerInfo _aspi)
        {

            try
            {

                var s = m_player_manager.findPlayer(_aspi.uid);

                if (s != null)
                {

                    //confirmLoginOnOtherServer(s, _req_server_uid, _aspi);

                }
                else
                    _smp.message_pool.getInstance().push(new message("[LoginServer::authCmdConfirmSendInfoPlayerOnline][WARNING] Player[UID: " + (_aspi.uid)
                            + "] retorno do confirma login com Auth Server do Server[UID: " + (_req_server_uid) + "], mas o palyer nao esta mais conectado.", type_msg.CL_FILE_LOG_AND_CONSOLE));

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[LoginServer::authCmdConfirmSendInfoPlayerOnline][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void authCmdSendCommandToOtherServer(packet _packet)
        {

        }

        public override void authCmdSendReplyToOtherServer(packet _packet)
        {

        }

        public override void sendCommandToOtherServerWithAuthServer(PangyaBinaryWriter _packet, uint _send_server_uid_or_type)
        {

        }

        public override void sendReplyToOtherServerWithAuthServer(PangyaBinaryWriter _packet, uint _send_server_uid_or_type)
        {

        }

        public bool getAccessFlag() => m_access_flag;
        public bool getCreateUserFlag() => m_create_user_flag;
        public bool canSameIDLogin() => m_same_id_login_flag;

        public override bool CheckCommand(Queue<string> _command)
        {
            if (_command.Count == 0)
            {
                _smp.message_pool.getInstance().push(new message("[LoginServer::CheckCommand][Error] Missing parameter", type_msg.CL_ONLY_CONSOLE));
                return true;
            }

            string s = _command.Dequeue();

            if (s.Equals("exit", StringComparison.OrdinalIgnoreCase))
            {
                Environment.Exit(-1);
                return true; // Sai
            }
            else if (s == "cls" || s == "clear")
            {
                Console.Clear();
                ConsoleEx.Log();
                return true;
            }
            else if (s.Equals("reload_files", StringComparison.OrdinalIgnoreCase))
            {
                ReloadFiles();
                _smp.message_pool.getInstance().push(new message("Login Server files have been reloaded.", type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
            else if (s.Equals("open", StringComparison.OrdinalIgnoreCase))
            {
                if (_command.Count > 1)
                {
                    string subCommand = _command.Dequeue();
                    if (subCommand.Equals("server", StringComparison.OrdinalIgnoreCase))
                    {
                        setIsUnderMaintenance(true);//faço o servidor parar de rodar ou simplesmente não ira mais receber conexao!
                        _smp.message_pool.getInstance().push(new message("Server Accept players ~~~.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                    }
                    else if (subCommand.Equals("gm", StringComparison.OrdinalIgnoreCase))
                    {
                        m_access_flag = true;
                        _smp.message_pool.getInstance().push(new message("Now only GM and registered IPs can login.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                    }
                    else if (subCommand.Equals("all", StringComparison.OrdinalIgnoreCase) && _command.Count > 2 && _command.Dequeue().Equals("user", StringComparison.OrdinalIgnoreCase))
                    {
                        m_access_flag = false;
                        _smp.message_pool.getInstance().push(new message("Now all users can login.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                    }
                    else
                    {
                        _smp.message_pool.getInstance().push(new message($"Unknown Command: \"open {subCommand}\"", type_msg.CL_ONLY_CONSOLE));
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
                        _smp.message_pool.getInstance().push(new message("Server close players ~~~.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                    }
                    else
                    {
                        _smp.message_pool.getInstance().push(new message($"Unknown Command: \"open {subCommand}\"", type_msg.CL_ONLY_CONSOLE));
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
                            sIff.getInstance().reload();
                            return true;
                        default:
                            _smp.message_pool.getInstance().push(new message($"[GameServer::checkCommand][Error] Unknown Command: \"reload_system {sTipo}\"", type_msg.CL_ONLY_CONSOLE));
                            return false;
                    }
                }
                else
                {
                    _smp.message_pool.getInstance().push(new message($"[GameServer::checkCommand][Error] Unknown Command: \"reload_system {sTipo}\"", type_msg.CL_ONLY_CONSOLE));
                    return false;
                }
            }
            else if (s.Equals("create_user", StringComparison.OrdinalIgnoreCase))
            {
                if (_command.Count > 1)
                {
                    string subCommand = _command.Dequeue();

                    if (subCommand.Equals("on", StringComparison.OrdinalIgnoreCase))
                    {
                        m_create_user_flag = true;
                        _smp.message_pool.getInstance().push(new message("Create User ON", type_msg.CL_FILE_LOG_AND_CONSOLE));
                    }
                    else if (subCommand.Equals("off", StringComparison.OrdinalIgnoreCase))
                    {
                        m_create_user_flag = false;
                        _smp.message_pool.getInstance().push(new message("Create User OFF", type_msg.CL_FILE_LOG_AND_CONSOLE));
                    }
                    else
                    {
                        _smp.message_pool.getInstance().push(new message($"Unknown Command: \"create_user {subCommand}\"", type_msg.CL_ONLY_CONSOLE));
                    }
                }
            }

            else
            {
                _smp.message_pool.getInstance().push(new message($"Unknown Command: {s}", type_msg.CL_ONLY_CONSOLE));
            }

            return false;
        }

        public void setIsUnderMaintenance(bool value)
        {
            IsUnderMaintenance = value;
        }

        public void requestLogin(Player _session, packet _packet)
        {

            /// Pacote01 Option 0x0F(15) é manutenção
            var p = new PangyaBinaryWriter();
            var login_type = 1;// login =1 jp, us  = 2;
            try
            {
                var test = _packet.Log();
                // Ler dados do packet de login 
                var result = new LoginData(_packet);

                //  Verify Id is valid
                if (result.id.Length < 2 || InvalidIdRegex.IsMatch(result.id))
                    throw new exception($"[LoginServer::RequestLogin][Error] ID({result.id}), PASS({result.password}) invalid, less then 2 characters or invalid character include in id.", (uint)STDA_ERROR_TYPE.LOGIN_SERVER);

                //  Verify Pass is valid
                if (result.password.Length < 4)
                    throw new exception($"[LoginServer::RequestLogin][Error] ID({result.id}), PASS({result.password}) invalid, less then 2 characters or invalid character include in pass.", (uint)STDA_ERROR_TYPE.LOGIN_SERVER);

                // Password to MD5
                var pass_md5 = Tools.MD5Hash(result.password);
                if (IsUnderMaintenance)
                {
                    packet_func_ls.session_send(packet_func_ls.pacote001(_session, 15), _session, 1); // Erro pass
                    _session.m_is_authorized = false;
                    return;
                }
                try
                {
                    login_type = result.password.Length < 32 ? 1 : 2;
                    if (login_type == 2)
                        throw new exception($"[LoginServer::RequestLogin][Error] ID({result.id}), PASS({result.password}) invalid, less then 2 characters or invalid character include in pass.", (uint)STDA_ERROR_TYPE.LOGIN_SERVER);

                    pass_md5 = Tools.MD5Hash(result.password);

                }
                catch (exception e)
                {

                    _smp.message_pool.getInstance().push(new message("[LoginServer::RequestLogin][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                    // Relança
                    throw;
                }

                if (string.IsNullOrEmpty(result.id))
                    throw new exception("[LoginServer::RequestLogin][Error] PLAYER[UID=" + (_session.m_pi.uid) + "] tentou contra o server[MESSAGE="
                            + result.id + "], vazio. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 1, 1/*UNKNOWN ERROR*/));

                if (!Tools.Sanitize(result.id))
                    throw new exception("[LoginServer::RequestLogin][Error] PLAYER[UID=" + (_session.m_pi.uid) + "] tentou contra o server[MESSAGE="
                            + result.id + "], tentativa de inject. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 1, 1/*UNKNOWN ERROR*/));

                if (string.IsNullOrEmpty(result.mac_address))
                    throw new exception("[LoginServer::RequestLogin][Error] PLAYER[UID=" + (_session.m_pi.uid) + "] tentou contra o server[MESSAGE="
                            + result.mac_address + "], vazio. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 1, 1/*UNKNOWN ERROR*/));

                if (!Tools.Sanitize(result.mac_address))
                    throw new exception("[LoginServer::RequestLogin][Error] PLAYER[UID=" + (_session.m_pi.uid) + "] tentou contra o server[MESSAGE="
                            + result.mac_address + "], tentativa de inject. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 1, 1/*UNKNOWN ERROR*/));


                if (string.IsNullOrEmpty(result.password))
                    throw new exception("[LoginServer::RequestLogin][Error] PLAYER[UID=" + (_session.m_pi.uid) + "] tentou contra o server[MESSAGE="
                            + result.password + "], vazio. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 1, 1/*UNKNOWN ERROR*/));

                if (!Tools.Sanitize(result.password))
                    throw new exception("[LoginServer::RequestLogin][Error] PLAYER[UID=" + (_session.m_pi.uid) + "] tentou contra o server[MESSAGE="
                            + result.password + "], tentativa de inject. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 1, 1/*UNKNOWN ERROR*/));


                if (!haveBanList(_session.m_ip, result.mac_address, login_type == 1))
                {   // Verifica se está na list de ips banidos
                    int _uid;
                    if ((_uid = CommandDB.VerifyID(result.id)) <= 0)
                    {
                        packet_func_ls.session_send(packet_func_ls.pacote001(_session, 6/*ID é 2, 6 é o ID ou pw errado*/), _session, 1);
                        _session.m_pi.id = result.id;
                        _session.m_client.Shutdown(System.Net.Sockets.SocketShutdown.Both);
                    }

                    // Verifica se o ID existe
                    if (MOD_TEST && _uid > 0 && !CommandDB.AccountConfirm(result.id))//verifica antes
                    {
                        packet_func_ls.session_send(packet_func_ls.pacote001(_session, 0x07, 0, "Confirm you accout in Email"), _session, 0);

                        _smp.message_pool.getInstance().push(new message($"[LoginServer::RequestLogin][Log] PLAYER[ID: {result.id}, BETA ACCOUNT: FALSE]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                        _session.m_client.Shutdown(System.Net.Sockets.SocketShutdown.Both);
                        return;
                    }
                     
                    if (_uid > 0)
                    {

                        var _verifyPass = CommandDB.VerifyPass((uint)_uid, pass_md5); // PASSWORD     

                        if (_verifyPass)
                        {   // Verifica se a senha bate com a do banco de dados

                            var cmd_pi = CommandDB.GetPlayerInfo((uint)_uid);

                            _session.m_pi.set_info(cmd_pi);

                            var pi = _session.m_pi;

                            var cmd_lc = CommandDB.IsLogonCheck(pi.uid);
                            var cmd_flc = CommandDB.IsFirstLogin(pi.uid);
                            var cmd_fsc = CommandDB.IsFirstSet(pi.uid);

                            // Verifica se tem o mesmo player logado com outro socket
                            var player_logado = HasLoggedWithOuterSocket(_session);

                            if (!canSameIDLogin() && player_logado != null)
                            {   // Verifica se ja nao esta logado

                                packet_func_ls.session_send(packet_func_ls.pacote001(_session, 0xE2, 5100107), _session, 0);

                                _session.m_pi.id = result.id;
                                _session.m_client.Shutdown(System.Net.Sockets.SocketShutdown.Both);
                            }
                            else if (pi.m_state == 1)
                            {   // Verifica se já pediu para logar

                                packet_func_ls.session_send(packet_func_ls.pacote001(_session, 0xE2, 500010), _session, 0); // Já esta logado, ja enviei o pacote de logar

                                if (pi.m_state++ >= 3)  // Ataque, derruba a conexão maliciosa
                                    _smp.message_pool.getInstance().push("[LoginServer::RequestLogin][Log] Player ja esta logado, o pacote de logar ja foi enviado, player[UID: "
                                            + (pi.uid) + ", ID: " + (pi.id) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE);
                                _session.m_pi.id = result.id;
                                _session.m_client.Shutdown(System.Net.Sockets.SocketShutdown.Both);
                            }
                            else
                            {

                                var cmd_vi = CommandDB.VerifyIP(pi.uid, _session.m_ip);

                                if (!Convert.ToBoolean(pi.m_cap & 4) && getAccessFlag() && !cmd_vi)
                                {
                                    // Verifica se tem permição para acessar 
                                    packet_func_ls.session_send(packet_func_ls.pacote001(_session, 0xE2, 500015), _session, 0); // Já esta logado, ja enviei o pacote de logar
                                    _session.m_pi.id = result.id;
                                    _session.m_client.Shutdown(System.Net.Sockets.SocketShutdown.Both);
                                }
                                else if (pi.block_flag.m_id_state.ull_IDState != 0)
                                {   // Verifica se está bloqueado

                                    if (pi.block_flag.m_id_state.L_BLOCK_TEMPORARY && (pi.block_flag.m_id_state.block_time == -1 || pi.block_flag.m_id_state.block_time > 0))
                                    {

                                        var tempo = pi.block_flag.m_id_state.block_time / 60 / 60/*Hora*/; // Hora

                                        p.init_plain(0x01);

                                        p.WriteByte(7);
                                        p.WriteInt32(pi.block_flag.m_id_state.block_time == -1 || tempo == 0 ? 1/*Menos de uma hora*/ : tempo);   // Block Por Tempo

                                        // Aqui pode ter uma  com mensagem que o pangya exibe
                                        //p.WriteString("ola");

                                        packet_func_ls.session_send(p, _session, 0);

                                        _smp.message_pool.getInstance().push("[LoginServer::RequestLogin][Log] Bloqueado por tempo[Time: "
                                                + (pi.block_flag.m_id_state.block_time == -1 ? ("indeterminado") : ((pi.block_flag.m_id_state.block_time / 60)
                                                + "min " + (pi.block_flag.m_id_state.block_time % 60) + "sec"))
                                                + "]. player [UID: " + (pi.uid) + ", ID: " + (pi.id) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE);
                                        _session.m_pi.id = result.id;
                                        _session.m_client.Shutdown(System.Net.Sockets.SocketShutdown.Both);
                                    }
                                    else if (pi.block_flag.m_id_state.L_BLOCK_FOREVER)
                                    {

                                        p.init_plain((ushort)0x01);

                                        p.WriteByte(0x0c);       // Acho que seja block permanente, que fala de email
                                                                 //p.WriteInt32(500012);	// Block Permanente

                                        packet_func_ls.session_send(p, _session, 0);

                                        _smp.message_pool.getInstance().push("[LoginServer::RequestLogin][Log] Bloqueado permanente. player [UID: " + (pi.uid)
                                                + ", ID: " + (pi.id) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE);
                                        _session.m_pi.id = result.id;
                                        _session.m_client.Shutdown(System.Net.Sockets.SocketShutdown.Both);
                                    }
                                    else if (pi.block_flag.m_id_state.L_BLOCK_ALL_IP)
                                    {

                                        // Bloquea todos os IP que o player logar e da error de que a area dele foi bloqueada

                                        // Add o ip do player para a lista de ip banidos 
                                        CommandDB.InsertBlockIP(_session.m_ip, "255.255.255.255");

                                        // Resposta
                                        p.init_plain((ushort)0x01);

                                        p.WriteByte(16);
                                        p.WriteInt32(500012);     // Ban por Região;

                                        packet_func_ls.session_send(p, _session, 0);
                                        _smp.message_pool.getInstance().push("[LoginServer::RequestLogin][Log] Player[UID: " + (_session.m_pi.uid)
                                                + ", IP: " + (_session.m_ip) + "] Block ALL IP que o player fizer login.", type_msg.CL_FILE_LOG_AND_CONSOLE);
                                        _session.m_pi.id = result.id;
                                        _session.m_client.Shutdown(System.Net.Sockets.SocketShutdown.Both);
                                    }
                                    else if (pi.block_flag.m_id_state.L_BLOCK_MAC_ADDRESS)
                                    {

                                        // Bloquea o MAC Address que o player logar e da error de que a area dele foi bloqueada

                                        // Add o MAC Address do player para a lista de MAC Address banidos
                                        CommandDB.InsertBlockMAC(result.mac_address);

                                        // Resposta
                                        p.init_plain((ushort)0x01);

                                        p.WriteByte(16);
                                        p.WriteInt32(500012);     // Ban por Região;

                                        packet_func_ls.session_send(p, _session, 0);

                                        _smp.message_pool.getInstance().push("[LoginServer::RequestLogin][Log] Player[UID: " + (_session.m_pi.uid)
                                                + ", IP: " + (_session.m_ip) + ", MAC: " + result.mac_address + "] Block MAC Address que o player fizer login.", type_msg.CL_FILE_LOG_AND_CONSOLE);
                                        _session.m_pi.id = result.id;
                                        _session.m_client.Shutdown(System.Net.Sockets.SocketShutdown.Both);
                                    }
                                    else if (!cmd_flc)
                                    {   // Verifica se fez o primeiro login

                                        // Authorized a ficar online no server por tempo indeterminado
                                        _session.m_is_authorized = true;

                                        FIRST_LOGIN(_session);
                                    }
                                    else if (!cmd_fsc)
                                    {   // Verifica se fez o primeiro set do character

                                        // Authorized a ficar online no server por tempo indeterminado
                                        _session.m_is_authorized = true;

                                        FIRST_SET(_session);
                                    }
                                    else if (cmd_lc.getLastCheck)
                                    {   // Verifica se já esta logado no game server

                                        // Pega o Server UID para usar depois no packet004, para derrubar do server
                                        _session.m_pi.m_server_uid = (uint)cmd_lc.getServerUID;

                                        // Já está varrizado a ficar online, o login server só vai derrubar o outro que está online no game server
                                        // Authorized a ficar online no server por tempo indeterminado
                                        _session.m_is_authorized = true;

                                        p.init_plain((ushort)0x01);
                                        p.WriteByte(4);

                                        packet_func_ls.session_send(p, _session, 0);
                                    }
                                    else if (Convert.ToBoolean(pi.m_cap & 4))
                                    {   // Acesso permtido

                                        // Authorized a ficar online no server por tempo indeterminado
                                        _session.m_is_authorized = true;

                                        packet_func_ls.SUCCESS_LOGIN("RequestLogin", this, _session);
                                    }
                                    else
                                    {

                                        // Authorized a ficar online no server por tempo indeterminado
                                        _session.m_is_authorized = true;

                                        packet_func_ls.SUCCESS_LOGIN("RequestLogin", this, _session);
                                    }

                                }
                                else if (!cmd_flc)
                                {   // Verifica se fez o primeiro login

                                    // Authorized a ficar online no server por tempo indeterminado
                                    _session.m_is_authorized = true;

                                    FIRST_LOGIN(_session);
                                }
                                else if (!cmd_fsc)
                                {   // Verifica se fez o primeiro set do character

                                    // Authorized a ficar online no server por tempo indeterminado
                                    _session.m_is_authorized = true;

                                    FIRST_SET(_session);
                                }
                                else if (cmd_lc.getLastCheck)
                                {   // Verifica se já esta logado no game server

                                    // Pega o Server UID para usar depois no packet004, para derrubar do server
                                    _session.m_pi.m_server_uid = (uint)cmd_lc.getServerUID;

                                    // Já está varrizado a ficar online, o login server só vai derrubar o outro que está online no game server
                                    // Authorized a ficar online no server por tempo indeterminado
                                    _session.m_is_authorized = true;

                                    p.init_plain((ushort)0x01);
                                    p.WriteByte(4);

                                    packet_func_ls.session_send(p, _session, 0);
                                }
                                else if (Convert.ToBoolean(pi.m_cap & 4))
                                {   // Acesso permtido

                                    // Authorized a ficar online no server por tempo indeterminado
                                    _session.m_is_authorized = true;

                                    packet_func_ls.SUCCESS_LOGIN("RequestLogin", this, _session);
                                }
                                else
                                {

                                    // Authorized a ficar online no server por tempo indeterminado
                                    _session.m_is_authorized = true;

                                    packet_func_ls.SUCCESS_LOGIN("RequestLogin", this, _session);
                                }
                            }
                        }
                        else
                        {
                            packet_func_ls.session_send(packet_func_ls.pacote001(_session, 6/* ID ou PW errado*/), _session, 1); // Erro pass 
                            _session.m_pi.id = result.id;
                            _session.m_client.Shutdown(System.Net.Sockets.SocketShutdown.Both);
                        }
                    } 
                    else if (!getAccessFlag() && getCreateUserFlag())
                    {

                        //// Authorized a ficar online no server por tempo indeterminado
                        _session.m_is_authorized = true;

                        var ip = _session.m_ip;

                        _uid = (int)CommandDB.CreateUser(result.id, pass_md5, ip, getUID());

                        _session.m_pi.uid = (uint)_uid;

                        var pi = _session.m_pi;

                        var _player_info = CommandDB.GetPlayerInfo(pi.uid);

                        pi.set_info(_player_info);

                        FIRST_LOGIN(_session);

                    } 
                }
                else
                {   // Ban IP/MAC por região

                    p.init_plain((ushort)0x01);

                    p.WriteByte(16);

                    packet_func_ls.session_send(p, _session, 0);
                    _smp.message_pool.getInstance().push("[LoginServer::RequestLogin][Log] Block por Regiao o IP/MAC: " + (_session.m_ip) + "/" + result.mac_address, type_msg.CL_FILE_LOG_AND_CONSOLE);
                    _session.m_pi.id = result.id;

                    _session.m_client.Shutdown(System.Net.Sockets.SocketShutdown.Both);
                }
            }
            catch (exception e)
            {
                _smp.message_pool.getInstance().push("[LoginServer::RequestLogin][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE);
                if (e.getCodeError() == (uint)STDA_ERROR_TYPE.LOGIN_SERVER)
                {

                    // Invalid ID 
                    packet_func_ls.session_send(packet_func_ls.pacote001(_session, 2/*Invlid ID*/), _session, 1);

                }
                else
                {

                    // Unknown Error (System Fail)
                    p.init_plain((ushort)0x01);

                    p.WriteByte(0xE2);

                    packet_func_ls.session_send(p, _session, 0);
                }
                _session.m_client.Shutdown(System.Net.Sockets.SocketShutdown.Both);
            }
        }

        public void requestDownPlayerOnGameServer(Player _session)
        {

            try
            {

                // Verifica se session está autorizada para executar esse ação, 
                // se ele não fez o login com o Server ele não pode fazer nada até que ele faça o login
                //CHECK_SESSION_IS_AUTHORIZED("DownPlayerOnLoginServer");

                // Derruba o player que está logado no game server
                // Se o Auth Server Estiver ligado manda por ele, se não tira pelo banco de dados mesmo
                if (m_unit_connect.isLive())
                {

                    // [Auth Server] . Game Server UID = _session.m_pi.m_server_uid;
                    m_unit_connect.sendDisconnectPlayer(_session.m_pi.m_server_uid, _session.m_pi.uid);

                }
                else
                {

                    // Auth Server não está online, resolver por aqui mesmo
                    CommandDB.RegisterLogon(_session.m_pi.uid, 0);
                    // Loga com sucesso
                    packet_func_ls.SUCCESS_LOGIN("LoginServer", this, _session);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push("[LoginServer::requestDownPlayerOnGame][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE);

                // Fail Login

                packet_func_ls.session_send(packet_func_ls.pacote00E(_session, "", 12, (e.getCodeError() == (uint)STDA_ERROR_TYPE.LOGIN_SERVER ? (uint)e.getCodeError() : 500053)), _session, 1);
            }
        }

        public void requestTryReLogin(Player _session, packet _packet)
        {
            try
            {

                string id = _packet.ReadString();
                _packet.ReadInt32(out int server_uid);
                string auth_key_login = _packet.ReadString();

                var _uid = CommandDB.VerifyID(id); // ID

                if (_uid <= 0) // Verifica se o ID existe
                {
                    packet_func_ls.session_send(packet_func_ls.pacote00E(_session, "", 12, 500052), _session, 1);
                    return;
                }
                var _player_info = CommandDB.GetPlayerInfo((uint)_uid);


                _session.m_pi.set_info(_player_info);

                if (id.CompareTo(_session.m_pi.id) != 0)
                {
                    packet_func_ls.session_send(packet_func_ls.pacote00E(_session, "", 12, 500052), _session, 1);
                    return;
                }

                var akli = CommandDB.GetAuthKeyLogin(_session.m_pi.uid);

                if (auth_key_login.CompareTo(akli) != 0)
                {
                    packet_func_ls.session_send(packet_func_ls.pacote00E(_session, "", 12, 500052), _session, 1);
                    return;
                }

                // Verifica se ele pode logar de novo, verifica as flag do login server
                if (haveBanList(_session.m_ip, "", false/*Não verifica o MAC Address*/))    // Verifica se está na list de ips banidos
                {
                    packet_func_ls.session_send(packet_func_ls.pacote00E(_session, "", 12, 500052), _session, 1);
                    return;
                }

                if (!Convert.ToBoolean(_session.m_pi.m_cap & 4) && getAccessFlag() && !CommandDB.VerifyIP(_session.m_pi.uid, _session.m_ip))
                {   // Verifica se tem permição para acessar

                    throw new exception("[LoginServer::requestReLogin][Log] acesso restrito para o player [UID: " + (_session.m_pi.uid)
                            + ", ID: " + (_session.m_pi.id) + "]", (uint)STDA_ERROR_TYPE.LOGIN_SERVER);

                }
                else if (_session.m_pi.block_flag.m_id_state.ull_IDState != 0)
                {   // Verifica se está bloqueado

                    if (_session.m_pi.block_flag.m_id_state.L_BLOCK_TEMPORARY && (_session.m_pi.block_flag.m_id_state.block_time == -1 || _session.m_pi.block_flag.m_id_state.block_time > 0))
                    {

                        throw new exception("[LoginServer::requestReLogin][Log] Bloqueado por tempo[Time: "
                                + (_session.m_pi.block_flag.m_id_state.block_time == -1 ? ("indeterminado") : ((_session.m_pi.block_flag.m_id_state.block_time / 60)
                                + "min " + (_session.m_pi.block_flag.m_id_state.block_time % 60) + "sec"))
                                + "]. player [UID: " + (_session.m_pi.uid) + ", ID: " + (_session.m_pi.id) + "]", (uint)STDA_ERROR_TYPE.LOGIN_SERVER);

                    }
                    else if (_session.m_pi.block_flag.m_id_state.L_BLOCK_FOREVER)
                    {

                        throw new exception("[LoginServer::requestReLogin][Log] Bloqueado permanente. player [UID: " + (_session.m_pi.uid)
                                + ", ID: " + (_session.m_pi.id) + "]", (uint)STDA_ERROR_TYPE.LOGIN_SERVER);

                    }
                    else if (_session.m_pi.block_flag.m_id_state.L_BLOCK_ALL_IP)
                    {

                        // Bloquea todos os IP que o player logar e da error de que a area dele foi bloqueada

                        // Add o ip do player para a lista de ip banidos
                        CommandDB.InsertBlockIP(_session.m_ip, "255.255.255.255");

                        // Resposta
                        throw new exception("[LoginServer::requestReLogin][Log] Player[UID: " + (_session.m_pi.uid)
                                + ", IP: " + (_session.m_ip) + "] Block ALL IP que o player fizer login.", (uint)STDA_ERROR_TYPE.LOGIN_SERVER);

                    }
                    else if (_session.m_pi.block_flag.m_id_state.L_BLOCK_MAC_ADDRESS)
                    {

                        // Bloquea o MAC Address que o player logar e da error de que a area dele foi bloqueada
                        // CommandDB.InsertBlockMAC(mac);

                        // Aqui só da error por que não tem como bloquear o MAC Address por que o cliente não fornece o MAC Address nesse pacote
                        throw new exception("[LoginServer::requestReLogin][Log] Player[UID: " + (_session.m_pi.uid)
                                + ", IP: " + (_session.m_ip) + ", MAC=UNKNOWN] (Esse pacote o cliente nao fornece o MAC Address) Block MAC Address que o player fizer login.",
                               (uint)STDA_ERROR_TYPE.LOGIN_SERVER);

                    }

                }

                // Authorized a ficar online no server por tempo indeterminado
                _session.m_is_authorized = true;

                packet_func_ls.succes_login(this, _session, 1/*só passa auth Key Login, Server List, Msn Server List*/);

            }
            catch (exception e)
            { 
                // Erro do sistema 
                packet_func_ls.session_send(packet_func_ls.pacote00E(_session, "", 12, 500052), _session, 1);


                _smp.message_pool.getInstance().push("[LoginServer::requestReLogin][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE);
            }
        }

        protected void FIRST_SET(Player _session)
        {
            _session.m_pi.m_state = 3;
            packet_func_ls.session_send(packet_func_ls.pacote00F(_session, 1), _session, 1);
            packet_func_ls.session_send(packet_func_ls.pacote001(_session, 0xD9), _session, 1);
        }

        protected void FIRST_LOGIN(Player _session)
        {
            _session.m_pi.m_state = 2;
            packet_func_ls.session_send(packet_func_ls.pacote00F(_session, 1), _session, 1);
            packet_func_ls.session_send(packet_func_ls.pacote001(_session, 0xD8), _session, 1);
        }
    }
}

// Server Static 
namespace sls
{
    public class ls : Singleton<Pangya_LoginServer.LoginServerTcp.LoginServer>
    {
    }
}