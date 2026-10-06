using PangyaAPI.DataBase;
using PangyaAPI.Network.Config;
using PangyaAPI.Network.Core;
using PangyaAPI.Network.Handle;
using PangyaAPI.Network.Models;
using PangyaAPI.Network.Repository;
using PangyaAPI.Network.Security;
using PangyaAPI.Network.Utils;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using snmdb;
using System.Net;
using System.Net.Sockets;

namespace PangyaAPI.Network.Service.Auth
{
    /// <summary>
    /// Central Authentication Server that manages Game Servers, Login Servers, and Player Authentication
    /// </summary>
    public abstract class UnitServer<T> : UnitAuthCommand, IAppServer where T : class, IAppSession
    {
        private TcpListener _listener;
        private CancellationTokenSource _cts;
        private IAppServerAccept _acceptLoop;
        public ServerType ServerType { get; set; }

        public bool IsRunning { get; private set; }
        protected readonly PacketDispatcher<T, AuthClientDispatcher> _dispatcher;
        public readonly AppSessionManager<T> SessionsManager;  
        private List<ServerInfo> m_server_list; 
        public ServerInfo m_si { get; private set; }
        public int m_Bot_TTL { get; private set; }

        public IReadOnlyCollection<IAppSession> Sessions => SessionsManager.GetAllSessions(); 

        public PangyaSyncTimer m_shutdown { get; private set; }
        /// <summary>
        /// TODO:usar pra derrubar o player se em 5 minutos ele nao logar, ou nao executar nenhum pacote.
        /// </summary>
        public PangyaSyncTimer m_timer_session_mgr { get; private set; }

        public PangyaSyncTimerManager m_timer_mgr { get; private set; } = new PangyaSyncTimerManager();

        private static readonly TimeSpan MonitorTimeout = TimeSpan.FromSeconds(5);
        public ConfigTimeOut Timeouts { get; private set; } = ConfigTimeOut.Default;

        protected UnitServer(
       AppSessionManager<T> sessionManager,
       PacketDispatcher<T, AuthClientDispatcher> dispatcher, ServerType typeServer)
        {
            ServerType = typeServer;
            SessionsManager = sessionManager;
            _dispatcher = dispatcher;
        }

        #region CONFIG

        public virtual void LoadConfig()
        {
            try
            {
                ServerInfo serverInfo = m_si;
                ConfigTimeOut timeouts = Timeouts;
                int timeTickBotLimit = m_Bot_TTL;

                ServerConfig.LoadConfig(ref serverInfo, ref timeouts, ref timeTickBotLimit, ServerType);

                m_si = serverInfo;
                Timeouts = timeouts;
                m_Bot_TTL = timeTickBotLimit;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ServerBase] Config error: {ex.Message}");

                m_Bot_TTL = 1000;
            }
        }

        #endregion

        #region LIFECYCLE

        public async Task StartAsync()
        {
            try
            {
                if (IsRunning)
                    return;

                _cts = new CancellationTokenSource();
                //para producao é modo release....
#if DEBUG
                _listener = new TcpListener(IPAddress.Parse(m_si.ip), m_si.port);
#else
                _listener = new TcpListener(IPAddress.Loopback, m_si.Port);
#endif

                _listener.Start();

                _acceptLoop = new AcceptLoop(_listener, HandleClient);

                _smp.LogManager.Instance.push(new AppMessage(
                    $"[AuthServer] Authentication Server started on {m_si.IpAddress}:{m_si.Port}",
                    type_msg.CL_FILE_LOG_AND_CONSOLE));

                IsRunning = true;
                // Inicia o loop de aceitação de conexões
                _ = _acceptLoop.StartAsync(_cts.Token);
                //test connection
                NormalManagerDB.Instance.Connected();
                // Inicia o monitoramento
                _ = MonitorLoop(_cts.Token);
            }
            catch (Exception ex)
            {
                IsRunning = false;
                _smp.LogManager.Instance.push(new AppMessage(
                    $"[AuthServer] Failed to start: {ex.Message}",
                    type_msg.CL_FILE_LOG_AND_CONSOLE));
                throw;
            }
        }

        public void Stop()
        {
            if (!IsRunning)
                return;

            IsRunning = false;

            try
            {
                _cts?.Cancel();
                _acceptLoop?.Stop();
                _listener?.Stop();
            }
            catch { }

            _smp.LogManager.Instance.push(new AppMessage(
                "[AuthServer] Authentication Server stopped",
                type_msg.CL_FILE_LOG_AND_CONSOLE));
        }

        public void Dispose()
        {
            Stop();
        }

        #endregion

        #region CONNECTION HANDLING

        private async Task HandleClient(TcpClient client)
        {
            var socket = client.Client;
            var ip = ((IPEndPoint)socket.RemoteEndPoint).Address.ToString();
             
            SocketHelper.Configure(socket);

            if (SessionsManager.Add(this, socket) is not T session)
            {
                client.Close();
                return;
            }

            OnClientConnected(session); 

            var parser = CreateParser();
            byte[] buffer = new byte[NetworkConstants.DefaultBufferSize];

            try
            {
                while (IsRunning && session.Connected)
                {
                    // Recebimento Assíncrono
                    int received = await session.ReceiveAsync(buffer);

                    if (received <= 0) break;

                    // Tenta parsear. Se o parser detectar o lixo de 8KB que vimos, ele deve retornar null.
                    var packets = parser.Parse(session._ParseKey, buffer, received);

                    if (packets == null)
                    {
                        // LOG DE SEGURANÇA: Importante para você saber quem banir no Firewall
                        _smp.LogManager.Instance.push(new AppMessage(
                            $"[Security] Protocol Violation (Invalid Size/Header) de {session.GetIP()}. Encerrando conexao.",
                            type_msg.CL_FILE_LOG_AND_CONSOLE)); 
                        break;
                    }

                    foreach (var packet in packets)
                    {
                        // Validação de integridade e autorização
                        if (!CheckPacket(session, packet))
                        {
                            _smp.LogManager.Instance.push(new AppMessage(
                                $"[Security] Packet Malicioso ou Nao Autorizado (ID: {(AuthClientDispatcher)(object)packet.Type}) de {session.GetIP()}. Encerrando.",
                                type_msg.CL_FILE_LOG_AND_CONSOLE)); 
                            return; // Sai do método para parar de processar qualquer lixo restante
                        }

                        _dispatcher.Dispatch(session, (AuthClientDispatcher)packet.Type, packet);
                    }
                }
            }
            catch (Exception ex)
            {
                _smp.LogManager.Instance.push(new AppMessage($"[ServerBase::HandleClient][Error]  Client error: {ex.Message}", type_msg.CL_ONLY_CONSOLE));
            }
            finally
            {
                CloseSession(session);
            }
        }

        public void CloseSession(IAppSession session)
        {
            if (session == null)
                return;

            try
            {
                // 1. Notifica o servidor ANTES de destruir a sessão
                OnClientDisconnected(session);
                 
                //remove by list
                SessionsManager.Remove(session);

                // 4. do Session completed.
                session.Dispose();
            }
            catch (Exception ex)
            {
                _smp.LogManager.Instance.push(new AppMessage($"[ServerBase::DisconnectInternal][Error]  (OID: {session?.ConnectionID}): {ex.Message}", type_msg.CL_ONLY_CONSOLE));
            }
        }

        public void Disconnect(IAppSession session)
        {
            if (session == null)
                return;

            CloseSession(session);
        }

        #endregion

        
         
        #region MONITORING

        private async Task MonitorLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {

                    // 1. Log de rotação diária
                    if (_smp.LogManager.Instance.check_update_day_log())
                    {
                        _smp.LogManager.Instance.push(new AppMessage($"[{GetType().Name}::Monitor][Info] Update File Log.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                    }

                    OnMonitor();
                    // Evento de heartbeat
                    OnHeartBeat();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ServerBase] Monitor error: {ex.Message}");
                }

                await Task.Delay(2000, token);
            }
        }
        #endregion
          

        #region ABSTRACTS

        protected abstract void OnStart();
        protected abstract void OnClientConnected(IAppSession session);
        protected abstract void OnClientDisconnected(IAppSession session);

        protected abstract void OnHeartBeat();

        protected abstract bool CheckPacket(IAppSession session, Packet packet);
        public abstract void DBResponse(int _msg_id, Pangya_DB _pangya_db, object _arg);
        protected abstract bool CheckCommand(Queue<string> _command);

        protected virtual IAuthPacketParser CreateParser()
        {
            return new AppAuthPacketParser();//sera diferente para o auth, o auth -> recebe dados (dados client), auth envia dados(server)
        }

        private async void OnMonitor()
        {
            try
            {
                // 2. Sincronização com o Banco
                m_si.CurrentUsers = SessionsManager.Count;
                // 3. Atualização de listas
                CmdUpdateServerList();
            }
            catch (Exception ex)
            {
                _smp.LogManager.Instance.push(new AppMessage(
                     $"[{GetType().Name}::Monitor][Error] {ex.Message}",
                     type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        private void CmdUpdateServerList()
        {
            DBCommand.RegisterServer(m_si); //atualiza
            this.m_server_list = DBCommand.GetGame();//pegar todos
        }

        public void OnStop()
        {
            Console.WriteLine("[TcpServer] Stopping...");

            // limpar recursos globais
        }


        #endregion


        #region FIND PLAYER
        public List<T> getAllSessions()
        {
            return SessionsManager.GetAllSessions();
        }

        public virtual List<T> FindAllGM()
        {
            return SessionsManager.FindAllGM();
        }

        public virtual T FindSessionByOid(int oid)
        {
            return SessionsManager.FindSessionByOid(oid);
        }

        public virtual T FindSessionByUid(uint uid)
        {
            return SessionsManager.FindSessionByUID(uid);
        }

        public virtual List<T> FindAllSessionByUid(uint uid)
        {
            return SessionsManager.FindAllSessionByUid(uid);
        }

        public virtual IAppSession FindSessionByNickname(string nickname)
        {
            return SessionsManager.FindSessionByNickname(nickname);
        }
        #endregion
    }
}
