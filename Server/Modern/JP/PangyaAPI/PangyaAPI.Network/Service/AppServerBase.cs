using PangyaAPI.DataBase;
using PangyaAPI.Network;
using PangyaAPI.Network.Config;
using PangyaAPI.Network.Core;
using PangyaAPI.Network.Flags;
using PangyaAPI.Network.Handle;
using PangyaAPI.Network.Models;
using PangyaAPI.Network.Repository;
using PangyaAPI.Network.Security;
using PangyaAPI.Network.Service;
using PangyaAPI.Network.Service.Auth;
using PangyaAPI.Network.Session;
using PangyaAPI.Network.Utils;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using System.Net;
using System.Net.Sockets;
public abstract class AppServerBase<T, TId> : UnitAuthCommand, IAppServer where T : class, IAppSession where TId : struct, Enum 
{
    #region FIELDS
    public ServerType ServerType { get; set; }

    private TcpListener _listener;
    private CancellationTokenSource _cts;
    private IAppServerAccept _acceptLoop;

    protected readonly AppSessionManager<T> SessionsManager;
    protected readonly PacketDispatcher<T, TId> _dispatcher;
    public bool IsRunning { get; private set; }

    public IReadOnlyCollection<IAppSession> Sessions => SessionsManager.GetAllSessions();

    public ServerInfo m_si { get; private set; }
    public int m_Bot_TTL { get; private set; }
    public PangyaSyncTimer m_shutdown { get; private set; }
    /// <summary>
    /// TODO:usar pra derrubar o player se em 5 minutos ele nao logar, ou nao executar nenhum pacote.
    /// </summary>
    public PangyaSyncTimer m_timer_session_mgr { get; private set; }

    public PangyaSyncTimerManager m_timer_mgr { get; private set; } = new PangyaSyncTimerManager();

    private static readonly TimeSpan MonitorTimeout = TimeSpan.FromSeconds(5);
    public ConfigTimeOut Timeouts { get; private set; } = ConfigTimeOut.Default;

    #endregion

    #region CONSTRUTOR
    protected AppServerBase(AppSessionManager<T> sessionManager, PacketDispatcher<T, TId> dispatcher, ServerType typeServer)
    {
        ServerType = typeServer;
        SessionsManager = sessionManager;
        _dispatcher = dispatcher;
    }

    #endregion

    #region START / STOP

    public async Task StartAsync()
    {
        try
        {
            if (IsRunning)
                return;

            _cts = new CancellationTokenSource();

            _listener = new TcpListener(IPAddress.Parse(m_si.IpAddress), m_si.Port);
            _listener.Start();
            // Passa m_si.MaxUsers como limite de conexões simultâneas
            _acceptLoop = new AcceptLoop(_listener, HandleClient, m_si.MaxUsers > 0 ? m_si.MaxUsers : 500);

            _smp.LogManager.Instance.push(new AppMessage($"[{GetType().Name}::Start][Sucess] Running[Port: {m_si.Port}, MaxUsers: {m_si.MaxUsers}, Timeouts: {Timeouts}]",
                type_msg.CL_ONLY_CONSOLE));

            IsRunning = true;

            _ = _acceptLoop.StartAsync(_cts.Token);
            _ = OnMonitorAndHeartBeat(_cts.Token);
            _ = TimeoutMonitorLoop(_cts.Token);   // Timeout monitor (a cada CheckIntervalSeconds)

            // Initialize and start Auth Server connection if configured
            await StartAuthConnectionAsync();
        }
        catch (Exception)
        {
            throw;
        }
    }


    public uint getUID()
    {
        return (uint)m_si.UID;
    }

    public void Stop()
    {
        if (!IsRunning)
            return;

        IsRunning = false;

        try
        {
            _cts.Cancel();
            _acceptLoop?.Stop();
            _listener.Stop();
        }
        catch { }

        OnStop();
    }

    public void Dispose()
    {
        Stop();
    }

    #endregion

    #region CLIENT FLOW

    // 1. Adicione 'async' na assinatura
    // Certifique-se de que o método use T
    private async Task HandleClient(TcpClient client)
    {
        var socket = client.Client;
        var ip = ((IPEndPoint)socket.RemoteEndPoint).Address.ToString();

        if (SessionsManager.IsFull())
        {
            _smp.LogManager.Instance.push(new AppMessage(
                      $"[HandleClient] Protocol Limited by MaxUsers.",
                      type_msg.CL_FILE_LOG_AND_CONSOLE));
            client.Close();
            return;
        }

        SocketHelper.Configure(socket);

        if (SessionsManager.Add(this, socket) is not T session)
        { 
            client.Close();
            return;
        }

        OnClientConnected(session);
        var parser = CreateParser();
        // Evita pressão desnecessária no GC durante alta frequência de pacotes
        byte[] buffer = new byte[NetworkConstants.DefaultBufferSize];

        // ── TIMEOUT 1: HANDSHAKE ────────────────────────────── 
        using var handshakeCts = new CancellationTokenSource(Timeouts.HandshakeTimeSpan);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            _cts.Token, handshakeCts.Token);

        // Passa o CTS para a sessão para que o handler do CS 0x01 possa cancelá-lo
        // após o handshake ser concluído com sucesso.
        if (session is AppSession baseSession)
            baseSession.SetHandshakeCts(handshakeCts);

        try
        {
            while (IsRunning && session.Connected)
            {
                int received;

                try
                {
                    // Usa o ShopToken ligado ao handshake enquanto não for feito,
                    // e o ShopToken principal após o handshake
                    var activeToken = handshakeCts.IsCancellationRequested
                        ? _cts.Token
                        : linkedCts.Token;

                    received = await session.ReceiveAsync(buffer);
                }
                catch (OperationCanceledException) when (handshakeCts.IsCancellationRequested && !_cts.IsCancellationRequested)
                {
                    // Handshake timeout — cliente conectou mas não enviou nada
                    _smp.LogManager.Instance.push(new AppMessage(
                        $"[Timeout::Handshake] {session.GetIP()} (OID {session.ConnectionID}) " +
                        $"não enviou handshake em {Timeouts.HandshakeSeconds}s. Kickando.",
                        type_msg.CL_FILE_LOG_AND_CONSOLE));

                    session.SetReason(CloseReason.HandshakeTimeOut);
                    break;
                }

                if (received <= 0) break;

                // Atualiza o idle timer a cada pacote recebido (Timeout 2)
                if (session is AppSession sess) sess.UpdateLastPacket();

                var packets = parser.Parse(session._ParseKey, buffer, received);

                if (packets == null && !(session.GetCapability() == 4 || session.GetCapability() == 128))
                {
                    session.SetReason(CloseReason.PacketProtocolError);
                    _smp.LogManager.Instance.push(new AppMessage(
                        $"[Security] Protocol Violation de {session.GetIP()}. Encerrando.",
                        type_msg.CL_FILE_LOG_AND_CONSOLE));
                    break;
                }

                foreach (var packet in packets)
                {
                    if (!CheckPacket(session, packet) && !(session.GetCapability() == 4 || session.GetCapability() == 128))
                    {
                        _smp.LogManager.Instance.push(new AppMessage(
                            $"[Security] Packet nao autorizado (ID: {(TId)(object)packet.Type}) de {session.GetIP()}.",
                            type_msg.CL_FILE_LOG_AND_CONSOLE));
                        session.SetReason(CloseReason.PacketProtocolNoAuthorized);
                        return;
                    }

                    _dispatcher.Dispatch(session, (TId)(object)packet.Type, packet);
                }
            }
        }
        catch (Exception ex)
        {
            _smp.LogManager.Instance.push(new AppMessage(
                $"[ServerBase::HandleClient][Error] {ex.Message}",
                type_msg.CL_ONLY_CONSOLE));
        }
        finally
        {
            CloseSession(session);
        }
    }

    public void CloseSession(IAppSession session)
    {
        // (Session.Clear() seta ConnectionID para -1)
        // Isso evita duplo-dispose em caso de race condition
        if (session == null || session.ConnectionID == -1)
            return;

        try
        { 
            OnClientDisconnected(session);
             
            //remove by list
            SessionsManager.Remove(session);
             
            session.Dispose();//limpa tudo.
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

    #region MONITOR

    private async Task OnMonitorAndHeartBeat(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                // Executa OnMonitor() com timeout de 5 segundos 
                using var timeoutCts = new CancellationTokenSource(MonitorTimeout);

                var monitorTask = Task.Run(OnMonitor, timeoutCts.Token);

                try
                {
                    await monitorTask.WaitAsync(MonitorTimeout, token);
                }
                catch (TimeoutException)
                {
                    _smp.LogManager.Instance.push(new AppMessage(
                        $"[{GetType().Name}::MonitorLoop][Warn] OnMonitor() demorou mais de {MonitorTimeout.TotalSeconds}s — possível lentidão no DB.",
                        type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
                 
                OnHeartBeat();
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ServerBase] Monitor error: {ex.Message}");
            }

            await Task.Delay(2000, token);
        }
    }

    private async Task TimeoutMonitorLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(Timeouts.CheckInterval, token);

                // Snapshot da lista para não iterar sobre coleção viva
                var sessions = SessionsManager.GetAllSessions().ToList();

                foreach (var session in sessions)
                {
                    if (session is not AppSession s) continue;
                    if (!s.Connected) continue;
                     
                    if (s.IsHandshakeExpired(Timeouts.HandshakeSeconds))
                    {
                        _smp.LogManager.Instance.push(new AppMessage(
                            $"[Timeout::Handshake] Monitor kickou {s.GetIP()} " +
                            $"(OID {s.ConnectionID}) — sem handshake em {s.SecondsConnectedWithoutHandshake:F0}s.",
                            type_msg.CL_FILE_LOG_AND_CONSOLE));

                        session.SetReason(CloseReason.HandshakeTimeOut); 
                        continue;
                    }

                    // Verifica Idle Timeout
                    if (s.IsIdle(Timeouts.IdleSeconds))
                    {
                        _smp.LogManager.Instance.push(new AppMessage(
                            $"[Timeout::Idle] Kickando {s.GetNickname()} ({s.GetIP()}) " +
                            $"(OID {s.ConnectionID} UID {s.GetUID()}) — " +
                            $"sem pacote há {s.SecondsSinceLastPacket:F0}s.",
                            type_msg.CL_FILE_LOG_AND_CONSOLE));

                        session.SetReason(CloseReason.IdleTimeOut);
                    }
                }
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                Console.WriteLine($"[TimeoutMonitor] {ex.Message}");
            }
        }
    }

    #endregion

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
            Timeouts = ConfigTimeOut.Default;
            m_Bot_TTL = 1000;
        }
    }

    #endregion

    #region ABSTRACTS

    protected abstract void OnStart();
    protected abstract void OnStop(); 
    protected abstract void OnClientConnected(IAppSession session);
    protected abstract void OnClientDisconnected(IAppSession session); 
    protected abstract void OnHeartBeat();
    protected abstract void OnMonitor();
    protected abstract bool CheckPacket(IAppSession session, Packet packet);
    protected abstract void DBResponse(int _msg_id, Pangya_DB _pangya_db, object _arg);
    public abstract bool CheckCommand(Queue<string> _command);
    protected virtual IPacketParser CreateParser()
    {
        return new AppPacketParser();
    }

    #endregion

    public int getBotTTL() => m_Bot_TTL;

    #region Modern Auth System

    public UnitAuthClient<T, TId> m_unit_connect;

    public virtual void InitializeAuthConnection()
    {
        if (m_unit_connect == null)
        {
            m_unit_connect = new UnitAuthClient<T, TId>(this);
        }
    }

    public virtual async Task StartAuthConnectionAsync()
    {
        InitializeAuthConnection();
        _ = m_unit_connect.ConnectAndRun();
        await Task.CompletedTask;
    }

    #endregion

    #region IUnitAuthServer Implementation

    //sao do unit
    public override void authCmdInfoPlayerOnline(uint _req_server_uid, uint _player_uid)
    {
        try
        {

            var s = SessionsManager.FindSessionByUID(_player_uid);

            if (s != null)
            {
                var aspi = new AuthServerPlayerInfo(s.GetUID(), s.GetID(), s.GetIP());

                // UPDATE ON Auth Server
                m_unit_connect.SendInfoPlayerOnline(_req_server_uid, aspi);

            }
            else
            {
                // UPDATE ON Auth Server
                m_unit_connect.SendInfoPlayerOnline(_req_server_uid, new AuthServerPlayerInfo(_player_uid));
            }

        }
        catch (exception e)
        {

            // UPDATE ON Auth Server - Error reply
            m_unit_connect.SendInfoPlayerOnline(_req_server_uid, new AuthServerPlayerInfo(_player_uid));

            _smp.LogManager.Instance.push(new AppMessage($"[{GetType().Name}::authCmdInfoPlayerOnline][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
        }
    }

    public override void authCmdSendCommandToOtherServer(Packet _packet)
    {

        try
        { 
            uint req_server_uid = _packet.ReadUInt32();
            var command_id = _packet.ReadInt16();

            try
            {
                _smp.LogManager.Instance.push(new AppMessage($"[{GetType().Name}::authCmdSendCommandToOtherServer][ErrorSystem] falta fazer ", type_msg.CL_FILE_LOG_AND_CONSOLE));

            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage($"[{GetType().Name}::authCmdSendCommandToOtherServer][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

            }

        }
        catch (exception e)
        {

            _smp.LogManager.Instance.push(new AppMessage($"[{GetType().Name}::authCmdSendCommandToOtherServer][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
        }
    }

    public override void authCmdSendReplyToOtherServer(Packet _packet)
    {
        try
        { 
            uint req_server_uid = _packet.ReadUInt32();
            var command_id = _packet.ReadInt16();

            try
            {

                _smp.LogManager.Instance.push(new AppMessage($"[{GetType().Name}::authCmdSendReplyToOtherServer][ErrorSystem] falta fazer ", type_msg.CL_FILE_LOG_AND_CONSOLE));

            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage($"[{GetType().Name}::authCmdSendCommandToOtherServer][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

        }
        catch (exception e)
        {

            _smp.LogManager.Instance.push(new AppMessage($"[{GetType().Name}::authCmdSendCommandToOtherServer][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
        }
    }

    public override void sendCommandToOtherServerWithAuthServer(Packet _packet, uint _send_server_uid_or_type)
    {
        try
        {

            // Envia o comando para o outro server com o Auth Server
            m_unit_connect.RequestCommandToOtherServer(_send_server_uid_or_type, new Packet(_packet.GetBytes));

        }
        catch (exception e)
        {

            _smp.LogManager.Instance.push(new AppMessage($"[{GetType().Name}::sendCommandToOtherServerWithAuthServer][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
        }
    }

    public override void sendReplyToOtherServerWithAuthServer(Packet _packet, uint _send_server_uid_or_type)
    {
        try
        {

            // Envia a resposta para o outro server com o Auth Server
            m_unit_connect.SendReplyToOtherServer(_send_server_uid_or_type, new Packet(_packet.GetBytes));

        }
        catch (exception e)
        {

            _smp.LogManager.Instance.push(new AppMessage($"[{GetType().Name}::sendReplyToOtherServerWithAuthServer][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
        }
    }

    #endregion
}