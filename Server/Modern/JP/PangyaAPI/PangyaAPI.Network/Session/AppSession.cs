using PangyaAPI.Network.Core;
using PangyaAPI.Network.Flags;
using PangyaAPI.Network.Service;
using PangyaAPI.Network.Utils;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading.Channels;
namespace PangyaAPI.Network.Session
{
    public class AppSession : IAppSession, IDisposable
    {
        public string _IpAddress { get; private set; } = "0.0.0.0";
        public bool _MarkedIp { get; private set; } = false;
        public int ConnectionID { get; private set; } = -1;
        public int _ParseKey { get; private set; } = -1;
        public IPEndPoint _RemoteIP { get; private set; }
        public bool Connected => Volatile.Read(ref _disconnectFlag) == 0;
        public bool Authorized { get; set; }
        public int Tick { get; set; }
        public int TicketBot { get; set; }
        public int TimeStart { get; set; }
        public IAppServer Server { get; set; }
         
        private readonly DateTime _connectedAt = DateTime.UtcNow;
        private volatile bool _handshakeDone = false;
        private CancellationTokenSource? _handshakeCts; 
        public DateTime LastPacketTime { get; private set; } = DateTime.UtcNow;
        public CloseReason Reason { get; private set; } = CloseReason.Normal;
        public bool ConnectionTimeOut =>
            Reason == CloseReason.HandshakeTimeOut ||
            Reason == CloseReason.IdleTimeOut ||
            Reason == CloseReason.PacketProtocolError;

        public double SecondsConnectedWithoutHandshake =>
            _handshakeDone ? 0 : (DateTime.UtcNow - _connectedAt).TotalSeconds;

        public double SecondsSinceLastPacket =>
            (DateTime.UtcNow - LastPacketTime).TotalSeconds;

        // ── Rede ─────────────────────────────────────────────────
        public readonly Socket _socket;
        private readonly Channel<byte[]> _sendChannel = Channel.CreateBounded<byte[]>(
            new BoundedChannelOptions(1024)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleWriter = false,
                SingleReader = true
            });

        private int _disconnectFlag = 0;
        private int _disposed = 0; 
        public AppSession(IAppServer server, Socket socket, int id)
        {
            Server = server;
            if (socket != null)
            {
                _socket = socket;
                _RemoteIP = (IPEndPoint)socket.RemoteEndPoint;
                SocketHelper.Configure(_socket);
            }
            ConnectionID = id;
            Authorized = false;
            GenerateParseKey();
            _ = SendLoopAsync();
        }


        public void MarkHandshakeDone()
        {
            _handshakeDone = true;
            LastPacketTime = DateTime.UtcNow;
        }

        public void UpdateLastPacket()
        {
            LastPacketTime = DateTime.UtcNow;
        }

        public bool IsHandshakeExpired(int timeoutSeconds)
        {
            if (_handshakeDone) return false;
            return (DateTime.UtcNow - _connectedAt).TotalSeconds > timeoutSeconds;
        }

        public bool IsIdle(int idleSeconds)
        {
            if (!_handshakeDone) return false;
            return (DateTime.UtcNow - LastPacketTime).TotalSeconds > idleSeconds;
        }

        public void SetHandshakeCts(CancellationTokenSource cts)
        {
            _handshakeCts = cts;
        }

        public void CancelHandshakeTimeout()
        {
            try { _handshakeCts?.Cancel(); }
            catch { }
        }

        public void ResetHandShake()
        {
            MarkHandshakeDone();
            CancelHandshakeTimeout();
        }

        public void Disconnect()
        {
            if (Interlocked.CompareExchange(ref _disconnectFlag, 1, 0) != 0)
                return;

            _sendChannel.Writer.TryComplete();

            try
            {
                if (_socket?.Connected == true)
                    _socket.Shutdown(SocketShutdown.Both);
            }
            catch { }

            SocketHelper.SafeClose(_socket);
            Clear();
        }

        public virtual bool Clear()
        {
            try
            {
                Authorized = false;
                ConnectionID = -1;
                _IpAddress = "0.0.0.0";
                _MarkedIp = false;
                _ParseKey = 0;
                return true;
            }
            catch (Exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage(
                    $"[AppSession::Clear][Error] {e.Message}",
                    type_msg.CL_FILE_LOG_AND_CONSOLE));
                return false;
            }
        }

        public void SetReason(CloseReason reason)
        {
            Reason = reason;
            Server.CloseSession(this);
        }


        public virtual void SendAuth(Packet buffer, bool IsRaw = false)
        {
            if (!Connected || buffer == null || buffer.GetBytes == null || buffer.GetBytes.Length == 0)
                return;

            EnqueueOrDrop(IsRaw ? buffer.MakePacketComplete(-1) : buffer.MakePacket(_ParseKey));
        }

        public virtual void Send(Packet buffer, bool IsRaw = false, int debug = 0)
        {
            if (!Connected || buffer == null || buffer.GetBytes == null || buffer.GetBytes.Length == 0)
                return;

            EnqueueOrDrop(IsRaw ? buffer.MakePacketComplete(-1) : buffer.MakePacketComplete(_ParseKey));
        }

        public virtual void Send(List<Packet> list_buffer)
        {
            // BUG 1 CORRIGIDO: era "if (Connected ||"
            if (!Connected || list_buffer == null || list_buffer.Count == 0)
                return;

            foreach (var buffer in list_buffer)
            {
                if (buffer?.GetBytes == null) continue;
                EnqueueOrDrop(buffer.MakePacketComplete(_ParseKey));
            }
        }

        private void EnqueueOrDrop(byte[] payload)
        {
            if (!_sendChannel.Writer.TryWrite(payload))
            {
                _smp.LogManager.Instance.push(new AppMessage(
                    $"[AppSession::Send][Warn] Canal cheio para {GetIP()} (OID {ConnectionID}). Pacote descartado.",
                    type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        private async Task SendLoopAsync()
        {
            try
            {
                await foreach (var data in _sendChannel.Reader.ReadAllAsync())
                {
                    if (!Connected) break;

                    try
                    {
                        if (_socket?.Connected != true) break;
                        await _socket.SendAsync(data, SocketFlags.None);
                    }
                    catch (ObjectDisposedException ex)
                    {
                        _smp.LogManager.Instance.push(new AppMessage(
                            $"[AppSession::SendLoop][SocketDispose] {ex.Message}",
                            type_msg.CL_FILE_LOG_AND_CONSOLE));
                        break;
                    }
                    catch (SocketException ex)
                    {
                        _smp.LogManager.Instance.push(new AppMessage(
                            $"[AppSession::SendLoop][SocketError] {ex.Message}",
                            type_msg.CL_FILE_LOG_AND_CONSOLE));
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                _smp.LogManager.Instance.push(new AppMessage(
                    $"[AppSession::SendLoop][Exception] {ex.Message}",
                    type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
            finally
            {
                if (Connected)
                    Server.CloseSession(this);
            }
        }

        public async Task<int> ReceiveAsync(byte[] buffer, CancellationToken token)
        {
            if (!Connected) return 0;
            try
            {
                return await _socket.ReceiveAsync(buffer, SocketFlags.None, token);
            }
            catch (OperationCanceledException)
            {
                throw; // repropaga para o AppServerBase tratar como timeout
            }
            catch { return 0; }
        }

        public async Task<int> ReceiveAsync(byte[] buffer)
        {
            if (!Connected) return 0;
            try
            {
                return await _socket.ReceiveAsync(buffer, SocketFlags.None);
            }
            catch (ObjectDisposedException)
            {
                throw; // repropaga para o AppServerBase tratar como timeout
            }
            catch { return 0; }
        }

        public virtual byte GetStateLogged() { return 0; }
        public virtual uint GetUID() { return 0; }
        public virtual uint GetCapability() { return 0; }
        public virtual string GetNickname() { return ""; }
        public virtual string GetID() { return ""; }

        public string GetIP()
        {
            if (!_MarkedIp || (_RemoteIP.Port != 0 && _IpAddress == "0.0.0.0"))
                MakeIp();
            return _IpAddress;
        }

        public void MakeIp()
        {
            if (!_MarkedIp || (_RemoteIP.Port != 0 && _IpAddress == "0.0.0.0"))
            {
                try
                {
                    _IpAddress = _RemoteIP.Address.ToString();
                    _MarkedIp = true;
                }
                catch
                {
                    throw new exception(
                        "Erro ao converter IP. AppSession::MakeIp()",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.UNIT, 1, 0));
                }
            }
        }

        public void GenerateParseKey()
        {
            _ParseKey = Random.Shared.Next(0, 16); // 0..15
        }


        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
            if (disposing) Disconnect();
        }

        ~AppSession() => Dispose(false);
    }
}