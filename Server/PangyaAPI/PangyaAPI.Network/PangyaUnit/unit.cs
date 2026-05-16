using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using PangyaAPI.Network.Repository;
using PangyaAPI.Network.Cryptor;
using PangyaAPI.Network.Models;
using PangyaAPI.Network.PangyaPacket;
using PangyaAPI.Network.PangyaServer;
using PangyaAPI.Network.PangyaSession;
using PangyaAPI.Network.PangyaUtil;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
namespace PangyaAPI.Network.PangyaUnit
{
    /// <summary>
    /// Server Auth ;)
    /// </summary>
    public abstract class unit : pangya_packet_handle
    {
        #region Fields


        public ServerState m_state;
        //DECRYPT FIELDS 
        private List<string> v_mac_ban_list;
        private List<IPBan> v_ip_ban_list;
        public SessionManager m_session_manager;
        public ServerInfoEx m_si = new ServerInfoEx();
        private int m_Bot_TTL; // Anti-bot Time-to-live
        //private bool m_chatDiscord;
        public bool _isRunning;
        public IniHandle m_reader_ini { get; set; }
        public ServerInfoEx getInfo() => m_si;
        public TcpListener _server;
        public List<ServerInfo> m_server_list { get; set; }
        #endregion

        #region Abstract Methods
        public abstract void OnStart();
        /// <summary>
        /// call methods
        /// </summary>
        public abstract void OnHeartBeat();
        /// <summary>
        /// check packet, packet is real
        /// </summary>
        /// <param name="session">client</param>
        /// <param name="_packet">packet read</param>
        /// <param name="opt">0 = server, 1 = client</param>
        /// <returns></returns>
        public abstract bool CheckPacket(Session session, packet _packet, int opt = 0);
        /// <summary>
        /// disconnect players !
        /// </summary>
        /// <param name="_session"></param>
        public abstract void onDisconnected(Session _session);

        /// <summary>
        /// Send Key
        /// </summary>
        /// <param name="_session"></param>
        protected abstract void onAcceptCompleted(Session _session);

        #endregion

        #region Constructor
        public unit(SessionManager manager)
        {
            try
            {
                ConsoleEx.Log();

                m_session_manager = manager;

                m_state = ServerState.Uninitialized;
                 
            }
            catch (exception e)
            {
                _smp.message_pool.getInstance().push(new message("[unit::construtor][Error] " + e.getFullMessageError(), type_msg.CL_ONLY_CONSOLE));
            }
        }


        #endregion

        #region Private Methods    

        public virtual void config_init()
        {
            try
            {
                m_reader_ini = new IniHandle("server.ini");
                m_si = new ServerInfoEx
                {
                    version = m_reader_ini.ReadString("SERVERINFO", "VERSION"),
                    version_client = m_reader_ini.ReadString("SERVERINFO", "CLIENTVERSION"),
                    nome = m_reader_ini.ReadString("SERVERINFO", "NAME", "Pangya Server Csharp"),
                    uid = m_reader_ini.ReadInt32("SERVERINFO", "GUID"),
                    port = m_reader_ini.ReadInt32("SERVERINFO", "PORT"),
                    ip = m_reader_ini.ReadString("SERVERINFO", "IP"),
                    max_user = m_reader_ini.ReadInt32("SERVERINFO", "MAXUSER"),
                    propriedade = new uProperty(m_reader_ini.ReadUInt32("SERVERINFO", "PROPERTY")),
                    rate = new RateConfigInfo(),
                    event_flag = new uEventFlag(),
                    flag = new uFlag(0)
                };
            }
            catch (exception e)
            {
                _smp.message_pool.getInstance().push(new message("[unit::config_init][Error] " + e.getFullMessageError(), type_msg.CL_ONLY_CONSOLE));
            }

            try
            {
                m_Bot_TTL = m_reader_ini.ReadInt32("OPTION", "ANTIBOTTTL", 1000);
                m_si.packet_version = m_reader_ini.ReadUInt32("SERVERINFO", "PACKETVERSION");
            }
            catch (exception e)
            {
                _smp.message_pool.getInstance().push(new message("[unit::config_init][Error] " + e.getFullMessageError(), type_msg.CL_ONLY_CONSOLE));
                m_Bot_TTL = 1000; // Usa o valor padrão do anti bot TTL
            }
        }

        /// <summary>
        /// Aguarda Conexões
        /// </summary> 
        private void OnClientAccepted(IAsyncResult ar)
        {
            TcpClient newClient = null;

            if (!_isRunning) return;

            if (_isRunning)
                _server.BeginAcceptTcpClient(OnClientAccepted, null);


            try
            {
                newClient = _server.EndAcceptTcpClient(ar);
                  
                // Cria thread/Task para processar o cliente
                _ = accept_completed(newClient);
            }
            catch (ObjectDisposedException)
            {
                // listener foi parado
                return;
            }
            catch (Exception e)
            {
                _smp.message_pool.getInstance().push(
                    new message($"[{GetType().Name}::OnClientAccepted][ErrorSystem] {e.Message}", type_msg.CL_FILE_LOG_AND_CONSOLE));
                newClient?.Close();
            }
        }

        /// <summary>
        /// Manuseia Comunicação do Cliente
        /// </summary>
        private async Task accept_completed(object obj)
        {
            TcpClient client = (TcpClient)obj;

            var _session = m_session_manager.AddSession(
                this,
                client,
                client.Client.RemoteEndPoint as IPEndPoint,
                (byte)(new Random().Next() % 16)
            );

            _smp.message_pool.getInstance().push(
                new message(
                    $"[{GetType().Name}] New Player [IP: {_session.getIP()}, Key: {_session.m_key}]",
                    type_msg.CL_FILE_LOG_AND_CONSOLE
                )
            );

            onAcceptCompleted(_session);

            _session.last_activity = DateTime.Now;

            try
            {
                while (client.Connected)
                {
                    bool result = recv_client_new(_session).Result;

                    if (result == false)
                    {
                        break; // desconecta
                    }

                    _session.last_activity = DateTime.Now;
                }
            }
            catch (IOException ioEx)
            {
                _smp.message_pool.getInstance().push(
                    new message($"[{GetType().Name}][IOError] {ioEx.Message}", type_msg.CL_FILE_LOG_AND_CONSOLE)
                );
            }
            catch (Exception ex)
            {
                _smp.message_pool.getInstance().push(
                    new message($"[{GetType().Name}][ErrorSystem] {ex}", type_msg.CL_FILE_LOG_AND_CONSOLE)
                );
            }

            DisconnectSession(_session);
        }
         
        protected async Task OnMonitor()
        {
            _smp.message_pool.getInstance().push(new message($"[{GetType().Name}::onMonitor][Info] monitor iniciado com sucesso!", type_msg.CL_ONLY_FILE_LOG));

            while (_isRunning)
            {
                try
                {
                    // Verifica e atualiza os arquivos de log caso o dia tenha mudado
                    if (_smp.message_pool.getInstance().check_update_day_log())
                    {
                        _smp.message_pool.getInstance().push(new message($"[{GetType().Name}::Monitor::UpdateLogFiles][Info] Atualizou os arquivos de Log porque trocou de dia.", type_msg.CL_ONLY_CONSOLE));
                    }

                    try
                    {
                        // Atualiza o número de sessões conectadas
                        m_si.curr_user = (int)m_session_manager.NumSessionConnected();
                        snmdb.NormalManagerDB.getInstance().add(0, new CmdRegisterServer(m_si), SQLDBResponse, this);
                    }
                    catch (exception e) // Exceção específica da aplicação
                    {
                        _smp.message_pool.getInstance().push(new message(
                             $"[{GetType().Name}::Monitor][ErrorSystem] {e.GetType().Name}: {e.getFullMessageError()}\nStack Trace: {e.getStackTrace()}",
                             type_msg.CL_FILE_LOG_AND_CONSOLE));
                    }
                    // pega a lista de servidores online
                    snmdb.NormalManagerDB.getInstance().add(1, new CmdServerList(TYPE_SERVER.GAME), SQLDBResponse, this);
                    // Evento de heartbeat
                    OnHeartBeat();
                    // On Start
                    OnStart();
                }
                catch (exception e) // Exceção específica da aplicação
                {
                    _smp.message_pool.getInstance().push(new message(
                         $"[{GetType().Name}::Monitor][ErrorSystem] {e.GetType().Name}: {e.getFullMessageError()}\nStack Trace: {e.getStackTrace()}",
                         type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
                catch (Exception ex) // Exceções gerais do .NET
                {
                    _smp.message_pool.getInstance().push(new message(
                         $"[{GetType().Name}::Monitor][ErrorSystem] {ex.GetType().Name}: {ex.Message}\nStack Trace: {ex.StackTrace}",
                         type_msg.CL_FILE_LOG_AND_CONSOLE));
                }

                await Task.Delay(2000);

                foreach (var s in m_session_manager.GetAllOnline())
                {
                    if ((DateTime.Now - s.last_activity).TotalSeconds > 30)
                    {
                        _smp.message_pool.getInstance().push(new message(
                        $"[{GetType().Name}::Monitor][TIMEOUT] Server caiu: {s.getNickname()}",
                        type_msg.CL_FILE_LOG_AND_CONSOLE));

                        DisconnectSession(s);
                    }
                }
            }
        }
          
            public override void dispach_packet_sv_same_thread(Session session, packet _packet)
            {
                if (session == null || session.isConnected() == false || _packet == null)
                {
                    return;//nao esta mais conectado!
                }

                func_arr.func_arr_ex func = null;

                try
                {
                    // Obtém a função correspondente ao tipo de pacote
                    func = packet_func_base.funcs_sv.getPacketCall(_packet.getTipo());
                }
                catch (exception e)
                {
                    _smp.message_pool.getInstance().push(new message($"[Server.DispatchpacketSameThread][ErrorSystem] {e.Message}, {e.getStackTrace()}", type_msg.CL_FILE_LOG_AND_CONSOLE));
                    // Desconecta a sessão
                    DisconnectSession(session);
                }

                try
                {
                // Atualiza o tick do cliente
                session.m_tick = Environment.TickCount;
                session.last_activity = DateTime.Now;

                var pd = new ParamDispatch
                    {
                        _session = session,
                        _packet = _packet
                    };

                    if (CheckPacket(session, _packet))
                    {
                        try
                        {
                            if (func != null && func.ExecCmd(pd) != 0)
                            {
                                //_smp.message_pool.getInstance().push(new message($"[Server.DispatchpacketSameThread][Error][MY] Ao tratar o pacote. ID: {_packet.getTipo()}(0x{_packet.getTipo():X})," + pd._packet.Log(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                                //DisconnectSession(session);
                            }
                        }

                        catch (exception e)
                        {
                            _smp.message_pool.getInstance().push(new message($"[Server.DispatchpacketSameThread][Error][MY] {e.getFullMessageError()}", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            DisconnectSession(session);
                        }
                    }
                }
                catch (exception e)
                {
                    _smp.message_pool.getInstance().push(new message($"[Server.DispatchpacketSameThread][Error][MY] {e.Message}", type_msg.CL_FILE_LOG_AND_CONSOLE));

                    DisconnectSession(session);
                }
            }

        protected override void dispach_packet_same_thread(Session session, packet _packet)
        {
            if (session == null || session.isConnected() == false || _packet == null)
            {
                return;//nao esta mais conectado!
            }

            func_arr.func_arr_ex func = null;

            try
            {
                // Obtém a função correspondente ao tipo de pacote
                func = packet_func_base.funcs.getPacketCall(_packet.getTipo());
            }
            catch (exception e)
            {
                _smp.message_pool.getInstance().push(new message($"[Server.DispatchpacketSameThread][ErrorSystem] {e.Message}, {e.getStackTrace()}", type_msg.CL_FILE_LOG_AND_CONSOLE));
                // Desconecta a sessão
                DisconnectSession(session);
            }

            try
            {
                // Atualiza o tick do cliente
                session.m_tick = Environment.TickCount;
                session.last_activity = DateTime.Now;

                var pd = new ParamDispatch
                {
                    _session = session,
                    _packet = _packet
                };

                if (CheckPacket(session, _packet, 1))
                {
                    try
                    {
                        if (func != null && func.ExecCmd(pd) != 0)
                        {
                            // _smp.message_pool.getInstance().push(new message($"[Server.DispatchpacketSameThread][Error][MY] Ao tratar o pacote. ID: {_packet.getTipo()}(0x{_packet.getTipo():X})," + pd._packet.Log(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                            DisconnectSession(session);
                        }
                    }

                    catch (exception e)
                    {
                        _smp.message_pool.getInstance().push(new message($"[Server.DispatchpacketSameThread][Error][MY] {e.getFullMessageError()}", type_msg.CL_FILE_LOG_AND_CONSOLE));

                        DisconnectSession(session);
                    }
                }
            }
            catch (exception e)
            {
                _smp.message_pool.getInstance().push(new message($"[Server.DispatchpacketSameThread][Error][MY] {e.Message}", type_msg.CL_FILE_LOG_AND_CONSOLE));

                DisconnectSession(session);
            }
        }

        #endregion

        #region Public Methods

        public void Start()
        {
            try
            {
                _server = new TcpListener(IPAddress.Any, m_si.port);
                m_state = ServerState.Initialized;

                if (m_state != ServerState.Failure)
                {

                    try
                    {
                        _server.Start(m_si.max_user);

                        _smp.message_pool.getInstance().push(new message("[unit::Start][Log] Running in Port: " + m_si.port, type_msg.CL_FILE_LOG_AND_CONSOLE));

                        _isRunning = true;

                        // inicia accept
                        _server.BeginAcceptTcpClient(OnClientAccepted, null);

                        // inicia monitor
                        _ = OnMonitor();
                         
                    }
                    catch (exception e)
                    {
                        _smp.message_pool.getInstance().push(new message(e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                    }

                }
                else
                {
                    _smp.message_pool.getInstance().push(new message("[unit::start][Error] Server Inicializado com falha, fechando o server.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
            }
            catch (exception e)
            {
                _smp.message_pool.getInstance().push(new message(e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void Stop()
        {
            _isRunning = false;  
            m_state = ServerState.Failure;
            Console.WriteLine("Server is stopping...");
        }


        public virtual Session HasLoggedWithOuterSocket(Session _session)
        {
            var s = m_session_manager.FindAllSessionByUid(_session.getUID());
            foreach (var el in s)
            {
                if (el.m_oid != _session.m_oid && el.isConnected())
                    return el;
            }

            return null;
        }

        protected virtual void init_option_accepted_socket(in Socket _accepted)
        {
            bool tcp_nodelay = true;

            // ---------- DESEMPENHO COM OS SOCKOPT -----------  
            // COM NO_TCPDELAY                 AVG(MEDIA) 0.552
            // COM SO_SNDBUF 0                AVG(MEDIA) 0.560
            // COM SO_RCVBUF 0                AVG(MEDIA) 0.570
            // COM NO_TCPDELAY e SO_SNDBUF 0  AVG(MEDIA) 0.569
            // COM NO_TCPDELAY e SO_RCVBUF 0  AVG(MEDIA) 0.566
            // SEM NENHUM SOCKOPT             AVG(MEDIA) 0.569
            // Não tem muita diferença, vou deixar só o NO_TCPDELAY mesmo

            try
            {
                // Ativa TCP_NODELAY (desabilita Nagle)
                _accepted.NoDelay = tcp_nodelay;
            }
            catch (SocketException ex)
            {
                throw new Exception("[unit::init_option_accepted_socket][Error] não conseguiu desabilitar tcp delay (nagle algorithm).", ex);
            }

            try
            {
                // KEEPALIVE: habilita + configura tempo
                byte[] keepAlive = new byte[12];
                BitConverter.GetBytes((uint)1).CopyTo(keepAlive, 0);     // onoff
                BitConverter.GetBytes((uint)20000).CopyTo(keepAlive, 4); // keepalivetime (20s)
                BitConverter.GetBytes((uint)2000).CopyTo(keepAlive, 8);  // keepaliveinterval (2s)

                _accepted.IOControl(IOControlCode.KeepAliveValues, keepAlive, null);
                 
            }
            catch (SocketException ex)
            {
                throw new Exception("[unit::init_option_accepted_socket][Error] não conseguiu setar o socket option KEEPALIVE.", ex);
            }
        }

        public bool haveBanList(string _ip_address, string _mac_address, bool _check_mac = true)
        {
            if (_check_mac)
            {
                // Verifica primeiro se o MAC Address foi bloqueado

                // Cliente não enviou um MAC Address válido, bloquea essa conexão que é hacker que mudou o ProjectG
                if (string.IsNullOrEmpty(_mac_address))
                    return true;    // Cliente não enviou um MAC Address válido, bloquea essa conexão que é hacker que mudou o ProjectG

                foreach (var el in v_mac_ban_list)
                {
                    if (!string.IsNullOrEmpty(el) && string.Compare(el, _mac_address, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        return true;
                    }
                }
            }
            // IP Address inválido, bloquea essa conexão que é Hacker ou Bug
            if (string.IsNullOrEmpty(_ip_address))
            {
                return true;
            }
            uint ip = 0;
            if (IPAddress.TryParse(_ip_address, out IPAddress ipAddress))
            {
                byte[] ipBytes = ipAddress.GetAddressBytes();
                ip = BitConverter.ToUInt32(ipBytes, 0);
                ip = (uint)IPAddress.NetworkToHostOrder((int)ip);
            }
            foreach (IPBan el in v_ip_ban_list)
            {
                if (el.type == IPBan._TYPE.IP_BLOCK_NORMAL)
                {
                    if ((ip & el.mask) == (el.ip & el.mask))
                    {
                        return true;
                    }
                }
                else if (el.type == IPBan._TYPE.IP_BLOCK_RANGE)
                {
                    if (el.ip <= ip && ip <= el.mask)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public void Shutdown(int timeSec)
        {
            Console.WriteLine("Shutting down server...");
            Stop();
        }

        public virtual uint GetUID()
        {
            return (uint)m_si.uid;
        }


        public virtual List<Session> FindAllGM()
        {
            return m_session_manager.findAllGM();
        }

        public virtual Session FindSessionByOid(uint oid)
        {
            return m_session_manager.FindSessionByOid(oid);
        }

        public virtual Session FindSessionByUid(uint uid)
        {
            return m_session_manager.findSessionByUID(uid);
        }

        public virtual List<Session> FindAllSessionByUid(uint uid)
        {
            return m_session_manager.FindAllSessionByUid(uid);
        }

        public virtual Session FindSessionByNickname(string nickname)
        {
            return m_session_manager.FindSessionByNickname(nickname);
        }

        public override bool DisconnectSession(Session _session)
        {
            if (_session == null)
            {
                Console.WriteLine("[unit::DisconnectSession][Warning] Tentativa de desconectar uma sessão nula.");
                return false;
            }

            _smp.message_pool.getInstance().push(new message($"[unit::DisconnectSession][Log] PLAYER[IP: {_session.getIP()}, Key: {_session.m_key}, Time: {DateTime.Now}]", type_msg.CL_FILE_LOG_AND_CONSOLE));

            // Notifica que a desconexão ocorreu       
            onDisconnected(_session);

            bool result;
            try
            {
                // Remove a sessão do gerenciador        
                result = m_session_manager.DeleteSession(_session);
            }
            catch (Exception ex)
            {
                result = false;
                Console.WriteLine($"[unit::DisconnectSession][Error] Erro ao deletar sessão: {ex.Message}");
            }
            return result;
        }


        public virtual void SQLDBResponse(int _msg_id, Pangya_DB _pangya_db, object _arg)
        {
            if (_arg == null)
            {
                _smp.message_pool.getInstance().push(new message("[Server.SQLDBResponse][Warning] _arg is null, na msg_id = " + _msg_id, type_msg.CL_FILE_LOG_AND_CONSOLE));
                return;
            }
            switch (_msg_id)
            {
                case 1:
                    {
                        m_server_list = ((CmdServerList)_pangya_db).getServerList();
                    }
                    break;
                default:
                    break;
            }
        }


        public virtual void RunCommand(string[] comando)
        {

        }

        public int getBotTTL() => m_Bot_TTL;
        #endregion  
    }
}
