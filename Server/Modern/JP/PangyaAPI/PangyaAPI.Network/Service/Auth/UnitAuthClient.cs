using PangyaAPI.Network;
using PangyaAPI.Network.Config;
using PangyaAPI.Network.Core;
using PangyaAPI.Network.Models;
using PangyaAPI.Network.Repository;
using PangyaAPI.Network.Service.Auth;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using System.Diagnostics;
using System.Net.Sockets;
using System.Numerics;
using System.Threading.Channels;

public class UnitAuthClient<T, TId> : UnitAuthCommand
where T : class, IAppSession
where TId : struct, Enum
{
    private readonly Channel<byte[]> _sendQueue = Channel.CreateUnbounded<byte[]>();
    private readonly AppServerBase<T, TId> _owner;
    private CancellationTokenSource? _cts;
    private int _parseKey = -1;
    private uint _server_guid;
    private int ConnectionId;
    private DateTime _lastActivityTime;
    private bool IsRunning;
    public UnitAuthClient(AppServerBase<T, TId> owner)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _lastActivityTime = DateTime.UtcNow;
    }

    public async Task ConnectAndRun()
    {
        using IniHandle m_reader_ini = ServerConfig.GetLoadConfigIni(_owner.ServerType);
        var ip = m_reader_ini.ReadString("AUTHSERVER", "IP");
        var port = m_reader_ini.readInt("AUTHSERVER", "PORT");

        _cts = new CancellationTokenSource();

        while (!_cts.IsCancellationRequested)
        {
            try
            {
                _parseKey = -1;
                _server_guid = 0;
                IsRunning = false;
                // Drenar o canal se houver lixo de conexões mortas
                while (_sendQueue.Reader.TryRead(out _)) { /* limpa a fila */ }

                using var tcp = new TcpClient();
                await tcp.ConnectAsync(ip, port, _cts.Token);

                using var connectionCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token);
                var stream = tcp.GetStream();

#if DEBUG
                _smp.message_pool.getInstance.push(new message("[UnitAuth] Connected to Auth!", type_msg.CL_ONLY_CONSOLE));
#endif
                IsRunning = true;
                var sendTask = FillSendAsync(stream, connectionCts.Token);
                var readTask = ReadLoopAsync(stream, connectionCts.Token);

                var completedTask = await Task.WhenAny(sendTask, readTask);

                connectionCts.Cancel();
            }
            catch (Exception ex)
            {
                await Task.Delay(5000, _cts.Token);
            }
        }
    }

    // --- ENVIO DE PACOTES ---
    // Este método é chamado pela unit.cs para mandar dados ao Auth
    public void Send(Packet p)
    {
        _sendQueue.Writer.TryWrite(p.MakePacket(_parseKey));//manda encriptacao
    }

    private async Task FillSendAsync(NetworkStream stream, CancellationToken ct)
    {
        await foreach (var data in _sendQueue.Reader.ReadAllAsync(ct))
        {
            await stream.WriteAsync(data, ct);
            await stream.FlushAsync(ct);
        }
    }

    // --- RECEBIMENTO E DISPATCHER (O que faltava) ---
    private async Task ReadLoopAsync(NetworkStream stream, CancellationToken ct)
    {
        byte[] buffer = new byte[NetworkConstants.DefaultBufferSize];
        while (!ct.IsCancellationRequested)
        {
            // Recebimento Assíncrono
            int received = await stream.ReadAsync(buffer);

            if (received <= 0) break;
            // Tenta parsear. Se o parser detectar o lixo de 8KB que vimos, ele deve retornar null.
            var packets = new AppAuthPacketParser().Parse(_parseKey, buffer, received);

            for (int i = 0; i < packets.Count; i++)
            {
                var packet = packets[i];
                // Processa o pacote sem travar a leitura do próximo
                _ = Task.Run(() => HandlePacket((AuthDispatcher)packet.Type, packet), ct);

            }
        }
    }

    private void HandlePacket(AuthDispatcher type, Packet packet)
    {
        if (type == AuthDispatcher.FIRST_PACKET_KEY)
        {
            _parseKey = packet.ReadInt32();
            _server_guid = packet.ReadUInt32();
        }

#if DEBUG
            _smp.message_pool.getInstance.push(new message($"[UnitAuthClient::HandlePacket][Debug] SEND[UID: " + _server_guid + ", PID: " + type + "]", type_msg.CL_ONLY_CONSOLE));
#else
#endif

        switch (type)
        {
            case AuthDispatcher.FIRST_PACKET_KEY:
                RequestFirstPacketKey(packet);
                break;

            case AuthDispatcher.ASK_LOGIN_RESULT:
                RequestAskLogin(packet);
                break;

            case AuthDispatcher.SHUTDOWN_SERVER:
                RequestShutdownServer(packet);
                break;

            case AuthDispatcher.BROADCAST_NOTICE:
                RequestBroadcastNotice(packet);
                break;

            case AuthDispatcher.BROADCAST_TICKER:
                RequestBroadcastTicker(packet);
                break;

            case AuthDispatcher.BROADCAST_CUBE_WIN:
                RequestBroadcastCubeWinRare(packet);
                break;

            case AuthDispatcher.DISCONNECT_PLAYER:
                RequestDisconnectPlayer(packet);
                break;

            case AuthDispatcher.CONFIRM_DISCONNECT:
                RequestConfirmDisconnectPlayer(packet);
                break;

            case AuthDispatcher.NEW_MAIL_ARRIVED:
                RequestNewMailArrivedMailBox(packet);
                break;

            case AuthDispatcher.NEW_RATE:
                RequestNewRate(packet);
                break;

            case AuthDispatcher.RELOAD_SYSTEM:
                RequestReloadSystem(packet);
                break;

            case AuthDispatcher.INFO_PLAYER_ONLINE:
                RequestInfoPlayerOnline(packet);
                break;

            case AuthDispatcher.CONFIRM_PLAYER_INFO:
                RequestConfirmSendInfoPlayerOnline(packet);
                break;

            case AuthDispatcher.CMD_FROM_OTHER_SERVER:
                RequestSendCommandToOtherServer(packet);
                break;

            case AuthDispatcher.REPLY_FROM_OTHER_SERVER:
                RequestSendReplyToOtherServer(packet);
                break;

            case AuthDispatcher.RECV_KEEP_ALIVE:
                RequestRecvKeepLive(packet);
                break;

            default:
                // Log de pacote desconhecido para debug
                Console.WriteLine($"[Auth] Pacote desconhecido: 0x{((ushort)type):X2}");
                break;
        }
    }

    public void RequestFirstPacketKey(Packet _packet)
    {
        if (_packet == null)
        {
            throw new exception("[UnitAuthClient::RequestFirstPacketKey" + "][Error] _packet is null", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.UNIT_AUTH_SERVER_CONNECT,
                6, 0));
        }

        try
        {
            CmdNewAuthServerKey cmd_nask = new CmdNewAuthServerKey(_owner.m_si.UID); // Waiter

            snmdb.NormalManagerDB.Instance.add(0,
                cmd_nask, null, null);

            if (cmd_nask.getException().getCodeError() != 0)
            {
                throw cmd_nask.getException();
            }

            // Resposta para o Auth Server
            var p = new Packet(0x1);
            p.WriteUInt32((uint)_owner.m_si.Type);
            p.WriteUInt32((uint)_owner.m_si.UID);
            p.WriteString(_owner.m_si.Name);
            p.WriteString(cmd_nask.getInfo());
            p.WriteString(_owner.m_si.ClientVersion);
            p.WriteUInt32(_owner.m_si.VersionPacket);
            Send(p);
        }
        catch (exception e)
        {

            _smp.LogManager.Instance.push(new AppMessage("[UnitAuthClient::RequestFirstPacketKey][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
        }
    }

    // 1. Mude para Task para melhor controle
    public async Task RequestSendKeepLive()
    {
        if (_owner == null) return;

        // 2. Use um CancellationToken se possível para encerrar o loop no desligamento do server
        while (!_cts.IsCancellationRequested)
        {
            try
            {
                // 3. Em vez de criar um objeto Writer gigante, 
                // use um buffer pequeno ou reutilize o writer se sua arquitetura permitir
                using (var p = new Packet(0xFF))
                {
                    p.WriteInt32(_owner.m_si.Type);
                    p.WriteInt32(_owner.m_si.UID);

                    // 4. Enviar o tempo do servidor (Unix Timestamp ou DateTime customizado)
                    p.WriteTime();

                    // 5. Envio direto e assíncrono (se o seu session_Send permitir)
                    Send(p);
                }

                _lastActivityTime = DateTime.Now;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[KeepLive] Erro na sessão {_owner.m_si.UID}: {ex.Message}");
                break;
            }

            // 6. Task.Delay é excelente pois não bloqueia a thread
            await Task.Delay(2000);
        }
    }

    public void RequestRecvKeepLive(Packet _packet)
    {
        _lastActivityTime = DateTime.Now;
    }

    public void RequestAskLogin(Packet _packet)
    {
        try
        {

            int oid = _packet.ReadInt32();

            if (oid > -1)
            {
                ConnectionId = oid;
                Task.Run(() => RequestSendKeepLive());
            }
            else
            {
                _smp.LogManager.Instance.push(new AppMessage("[UnitAuthClient::RequestAskLogin][Log] Nao conseguiu logar com o Auth Server.", type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }
        catch (exception e)
        {

            _smp.LogManager.Instance.push(new AppMessage("[UnitAuthClient::RequestAskLogin][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
        }
    }

    public void RequestShutdownServer(Packet _packet)
    {
        try
        {

            // Time In tv_sec for Shutdown
            int time = _packet.ReadInt32();

            _owner.authCmdShutdown(time);

        }
        catch (exception e)
        {

            _smp.LogManager.Instance.push(new AppMessage("[UnitAuthClient::RequestShutdownServer][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
        }
    }

    public void RequestBroadcastNotice(Packet _packet)
    {
        try
        {

            var notice = _packet.ReadString();

            _owner.authCmdBroadcastNotice(notice);

        }
        catch (exception e)
        {

            _smp.LogManager.Instance.push(new AppMessage("[UnitAuthClient::RequestBroadcastNotice][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
        }
    }

    public void RequestBroadcastTicker(Packet _packet)
    {
        try
        {

            var nickname = _packet.ReadString();
            var msg = _packet.ReadString();

            _owner.authCmdBroadcastTicker(nickname, msg);

        }
        catch (exception e)
        {

            _smp.LogManager.Instance.push(new AppMessage("[UnitAuthClient::RequestBroadcastTicker][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
        }
    }

    public void RequestBroadcastCubeWinRare(Packet _packet)
    {
        try
        {

            uint option = _packet.ReadUInt32();
            var msg = _packet.ReadString();

            _owner.authCmdBroadcastCubeWinRare(msg, (option));

        }
        catch (exception e)
        {

            _smp.LogManager.Instance.push(new AppMessage("[UnitAuthClient::RequestBroadcastCubeWinRare][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
        }
    }

    public void RequestDisconnectPlayer(Packet _packet)
    {
        try
        {

            uint player_uid = _packet.ReadUInt32();
            uint server_uid = _packet.ReadUInt32();
            byte force = _packet.ReadByte(); // ServerFlag que força a disconectar o usuário

            _owner.authCmdDisconnectPlayer((server_uid),
                (player_uid),
                force);

        }
        catch (exception e)
        {

            _smp.LogManager.Instance.push(new AppMessage("[UnitAuthClient::RequestDisconnectPlayer][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
        }
    }

    public void RequestConfirmDisconnectPlayer(Packet _packet)
    {
        try
        {

            uint player_uid = _packet.ReadUInt32();

            _owner.authCmdConfirmDisconnectPlayer((player_uid));

        }
        catch (exception e)
        {

            _smp.LogManager.Instance.push(new AppMessage("[UnitAuthClient::RequestConfirmDisconnectPlayer][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
        }
    }

    public void RequestNewMailArrivedMailBox(Packet _packet)
    {
        try
        {

            uint player_uid = _packet.ReadUInt32();
            int mail_id = _packet.ReadInt32();

            _owner.authCmdNewMailArrivedMailBox((player_uid), (mail_id));

        }
        catch (exception e)
        {

            _smp.LogManager.Instance.push(new AppMessage("[UnitAuthClient::RequestNewMailArrivedMailBox][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
        }
    }

    public void RequestNewRate(Packet _packet)
    {
        try
        {

            uint tipo = _packet.ReadUInt32();
            uint qntd = _packet.ReadUInt32();

            _owner.authCmdNewRate((tipo), (qntd));

        }
        catch (exception e)
        {

            _smp.LogManager.Instance.push(new AppMessage("[UnitAuthClient::RequestNewRate][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
        }
    }

    public void RequestReloadSystem(Packet _packet)
    {
        try
        {

            uint sistema = _packet.ReadUInt32();

            _owner.authCmdReloadGlobalSystem((sistema));

        }
        catch (exception e)
        {

            _smp.LogManager.Instance.push(new AppMessage("[UnitAuthClient::RequestReloadSystem][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
        }
    }

    public void RequestInfoPlayerOnline(Packet _packet)
    {
        if (_packet == null)
        {
            throw new exception("[UnitAuthClient::RequestGetInfoPlayerOnline" + "][Error] _packet is null", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.UNIT_AUTH_SERVER_CONNECT,
                6, 0));
        }

        try
        {

            uint req_server_uid = _packet.ReadUInt32();
            uint player_uid = _packet.ReadUInt32();

            _owner.authCmdInfoPlayerOnline((req_server_uid), (player_uid));

        }
        catch (exception e)
        {

            _smp.LogManager.Instance.push(new AppMessage("[UnitAuthClient::RequestInfoPlayerOnline][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
        }
    }

    public void RequestConfirmSendInfoPlayerOnline(Packet _packet)
    {

        if (_packet == null)
        {
            throw new exception("[UnitAuthClient::RequestConfirmSendInfoOnline" + "][Error] _packet is null", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.UNIT_AUTH_SERVER_CONNECT,
                6, 0));
        }

        try
        {

            AuthServerPlayerInfo aspi = new AuthServerPlayerInfo();

            uint req_server_uid = _packet.ReadUInt32();

            aspi.option = _packet.ReadInt32();
            aspi.uid = _packet.ReadUInt32();

            if (aspi.option == 1)
            {
                aspi.id = _packet.ReadString();
                aspi.ip = _packet.ReadString();
            }

            _owner.authCmdConfirmSendInfoPlayerOnline(req_server_uid, aspi);

        }
        catch (exception e)
        {
            _smp.LogManager.Instance.push(new AppMessage("[UnitAuthClient::RequestConfirmSendInfoPlayerOnline][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
        }
    }

    public void RequestSendCommandToOtherServer(Packet _packet)
    {
        try
        {

            _owner.authCmdSendCommandToOtherServer(_packet);

        }
        catch (exception e)
        {

            _smp.LogManager.Instance.push(new AppMessage("[UnitAuthClient::RequestSendCommandToOtherServer][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
        }
    }

    public void RequestSendReplyToOtherServer(Packet _packet)
    {
        try
        {

            _owner.authCmdSendReplyToOtherServer(_packet);

        }
        catch (exception e)
        {

            _smp.LogManager.Instance.push(new AppMessage("[UnitAuthClient::RequestSendReplyToOtherServer][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
        }
    }

    // Request _writer
    public void SendConfirmDisconnectPlayer(uint _server_uid, uint _player_uid)
    {
        try
        {
            var p = new Packet(0x3);

            p.WriteUInt32(_player_uid);
            p.WriteUInt32(_server_uid);

            Send(p);
        }
        catch (exception e)
        {

            _smp.LogManager.Instance.push(new AppMessage("[UnitAuthClient::SendConfirmDisconnectPlayer][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
        }
    }

    public void SendDisconnectPlayer(uint _server_uid, uint _player_uid)
    {
        try
        {
            var p = new Packet(0x2);

            p.WriteUInt32(_player_uid);
            p.WriteUInt32(_server_uid);

            Send(p);
        }
        catch (exception e)
        {

            _smp.LogManager.Instance.push(new AppMessage("[UnitAuthClient::SendDisconnectPlayer][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
        }
    }

    public void SendInfoPlayerOnline(uint _server_uid, AuthServerPlayerInfo _aspi)
    {
        try
        {
            var p = new Packet(0x5);

            p.WriteUInt32(_server_uid);
            p.WriteInt32(_aspi.option);
            p.WriteUInt32(_aspi.uid);

            if (_aspi.option == 1)
            {
                p.WriteString(_aspi.id);
                p.WriteString(_aspi.ip);
            }

            Send(p);
        }
        catch (exception e)
        {

            _smp.LogManager.Instance.push(new AppMessage("[UnitAuthClient::SendInfoPlayerOnline][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
        }
    }

    public void getInfoPlayerOnline(uint _server_uid, uint _player_uid)
    {
        try
        {
            var p = new Packet(0x04);

            p.WriteUInt32(_server_uid);
            p.WriteUInt32(_player_uid);

            Send(p);
        }
        catch (exception e)
        {

            _smp.LogManager.Instance.push(new AppMessage("[UnitAuthClient::getInfoPlayerOnline][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
        }
    }


    public void RequestCommandToOtherServer(uint _server_uid, Packet _packet)
    {
        try
        {

            // Ler o command ID para verificar se está tudo ok
            _packet.ReadUInt16();

            if (_packet.Size < 2)
            {
                throw new exception("[UnitAuthClient::SendCommandToOtherServer][Error] Tentou enviar o comando[ID=" + Convert.ToString(_packet.Type) + "] para o outro server[UID=" + Convert.ToString(_server_uid) + "] com o Auth Server, mas o packet é invalido nao tem nem o Login.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.UNIT_AUTH_SERVER_CONNECT,
                    1000, 0));
            }

            ushort command_buff_size = (ushort)(_packet.Size - 2u);

            CommandOtherServerHeaderEx cosh = new CommandOtherServerHeaderEx
            {
                send_server_uid_or_type = _server_uid,
                command_id = (short)_packet.Type
            };

            // Inicializa comando buffer
            cosh.command.init(command_buff_size);

            if (!cosh.command.is_good())
            {
                throw new exception("[UnitAuthClient::SendCommandToOtherServer][Error] Tentou enviar a reposta[ID=" + Convert.ToString(_packet.Type) + "] para o outro server[UID=" + Convert.ToString(_server_uid) + "] com o Auth Server, mas nao conseguiu allocar memoria para o command buffer. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.UNIT_AUTH_SERVER_CONNECT,
                    1001, 0));
            }

            cosh.command.buff = _packet.ReadBytes(cosh.command.size);

            // Envia o comando para o Auth Server enviar para o outro server
            var p = new Packet(0x06);
            p.WriteBytes(cosh.ToArray());
            Send(p);
        }
        catch (exception e)
        {

            _smp.LogManager.Instance.push(new AppMessage("[UnitAuthClient::SendCommandToOtherServer][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
        }
    }

    public void SendReplyToOtherServer(uint _server_uid, Packet _packet)
    {
        try
        {


            // Ler a Resposta ID para verificar se está tudo ok
            _packet.ReadUInt16();

            if (_packet.Size < 2)
            {
                throw new exception("[UnitAuthClient::SendReplyToOtherServer][Error] Tentou enviar a reposta[ID=" + Convert.ToString(_packet.Type) + "] para o outro server[UID=" + Convert.ToString(_server_uid) + "] com o Auth Server, mas o packet é invalido nao tem nem o Login.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.UNIT_AUTH_SERVER_CONNECT,
                    1000, 0));
            }

            ushort command_buff_size = (ushort)(_packet.Size - 2u);

            CommandOtherServerHeaderEx cosh = new CommandOtherServerHeaderEx();

            cosh.send_server_uid_or_type = _server_uid;
            cosh.command_id = (short)_packet.Type;

            // Inicializa comando buffer
            cosh.command.init(command_buff_size);

            if (!cosh.command.is_good())
            {
                throw new exception("[UnitAuthClient::SendReplyToOtherServer][Error] Tentou enviar a reposta[ID=" + Convert.ToString(_packet.Type) + "] para o outro server[UID=" + Convert.ToString(_server_uid) + "] com o Auth Server, mas nao conseguiu allocar memoria para o command buffer. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.UNIT_AUTH_SERVER_CONNECT,
                    1001, 0));
            }

            cosh.command.buff = _packet.ReadBytes(cosh.command.size);

            // Envia a resposta para o Auth Server enviar para o outro server
            var p = new Packet(0x07);
            p.WriteBytes(cosh.ToArray());
            Send(p);
        }
        catch (exception e)
        {

            _smp.LogManager.Instance.push(new AppMessage("[UnitAuthClient::SendReplyToOtherServer][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
        }
    }

    public void Dispose() => _cts?.Cancel();

    public bool isLive()
    {
        return IsRunning;
    }
}