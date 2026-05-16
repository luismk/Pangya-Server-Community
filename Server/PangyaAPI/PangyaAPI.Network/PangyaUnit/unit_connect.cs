using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using PangyaAPI.Network.Cryptor;
using PangyaAPI.Network.Models;
using PangyaAPI.Network.PangyaPacket;
using PangyaAPI.Network.PangyaSession;
using PangyaAPI.Utilities; 
using PangyaAPI.Utilities.Log;
using PangyaAPI.Utilities.Models;

namespace PangyaAPI.Network.PangyaUnit
{
    public class ParamDispatchAS
    {
        public UnitPlayer _session;
        public packet _packet;

        public ParamDispatchAS(ParamDispatch arg2)
        {
            _session = (UnitPlayer)arg2._session;
            _packet = arg2._packet;
        }
        public ParamDispatchAS()
        {
        }
    }

    public class UnitPlayer : Session
    {
        public struct player_info
        {
            public string nickname;
            public string id;
            public uint uid;
            public uint tipo;
            public byte m_state;
        }
        public override byte getStateLogged() => m_pi.m_state;
        public override uint getUID() => m_pi.uid;
        public override uint getCapability() => m_pi.tipo;
        public override string getNickname() => m_pi.nickname;
        public override string getID() => m_pi.id;
        public ServerInfoEx m_si;
        public player_info m_pi;

        public UnitPlayer(pangya_packet_handle _Packet_Handle, ServerInfoEx serverInfo)
        {
            m_si = serverInfo;
            this._Packet_Handle_Base = _Packet_Handle;
            m_pi = new player_info();
        }


        public void Connect(string ip, int port)
        {
            m_pi = new player_info();
            m_client = new TcpClient(ip, port);
            m_addr = m_client.Client.RemoteEndPoint as IPEndPoint;
            setState(true);
            setConnected(true);
        }

        public override bool clear()
        {
            m_si = null;
            this._Packet_Handle_Base = null;
            m_pi = new player_info();
            return base.clear();
        }

        public override void requestSendBuffer(byte[] _buff, bool _raw = false)
        {

            if (_buff == null)
            {
                throw new exception("Error _buff is null. Session::requestSendBuffer()", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.SESSION,
                    3, 0));
            }
            int _size = _buff.Length;
            if (_size <= 0)
            {
                throw new exception("Error _size is less or equal the zero. Session::requestSendBuffer()", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.SESSION,
                    4, 0));
            }
            try
            {
                if (isConnectedToSend())
                {

                    var payloadData = _raw ? _buff : Cipher.EncryptClient(_buff, m_key, 0);

                    if (!m_client.Send(payloadData, payloadData.Length))
                    {
                        @lock();
                        setConnectedToSend(false);
                        unlock();

                        try
                        {
                            _Packet_Handle_Base.DisconnectSession(this);
                        }
                        catch (exception e)
                        {
                            _smp.message_pool.getInstance().push(new message("[threadpool::send_new][Error] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                        }
                    }
                    else
                    {
                        //new mode
                        _Packet_Handle_Base.dispach_packet_sv_same_thread(this, _raw ? new packet(_buff) : new packet(_buff));
                    }
                }
                else
                {
                    //m_buff_s.releaseWrite();
                    return;
                }
            }
            finally
            {
                // m_buff_s.unlock();
            }
        }

    }

    public abstract class unit_connect_base : pangya_packet_handle
    {
        public unit_connect_base(ServerInfoEx _si)
        {
            funcs = new func_arr();
            funcs_sv = new func_arr();
            m_unit_ctx = new stUnitCtx();
            // Inicializar Config do arquivo ini \\
            m_reader_ini = new IniHandle("Server.ini");
            config_init();
            // ------------------------------------
            m_session = new UnitPlayer(this, _si);
        }

        protected void config_init()
        {
            m_unit_ctx.ip = m_reader_ini.ReadString("AUTHSERVER", "IP");
            m_unit_ctx.port = m_reader_ini.readInt("AUTHSERVER", "PORT");

            // Carregou com sucesso
            m_unit_ctx.state = true;
        }
        public enum STATE : byte { UNINITIALIZED, GOOD, GOOD_WITH_WARNING, INITIALIZED, FAILURE }
        public enum ThreadType { WORKER_IO, WORKER_IO_SEND, WORKER_IO_RECV, WORKER_LOGICAL, WORKER_SEND, TT_CONSOLE, TT_ACCEPT, TT_ACCEPTEX, TT_ACCEPTEX_IO, TT_RECV, TT_SEND, TT_JOB, TT_DB_NORMAL, TT_MONITOR, TT_SEND_MSG_TO_LOBBY }
        public enum OperationType { SEND_RAW_REQUEST, SEND_RAW_COMPLETED, RECV_REQUEST, RECV_COMPLETED, SEND_REQUEST, SEND_COMPLETED, DISPACH_PACKET_SERVER, DISPACH_PACKET_CLIENT, ACCEPT_COMPLETED }

        public struct stUnitCtx
        {
            public bool state;
            public string ip;
            public int port;

            public void Clear()
            {
                ip = string.Empty;
                port = 0;
                state = false;
            }
        }

        public virtual bool isLive()
        {
            return m_session.m_client != null && m_session.m_client.Connected;
        }

        protected abstract void onHeartBeat();
        protected abstract void onConnected();
        protected abstract void onDisconnect();

        public bool ConnectAndAssoc()
        {
            if (!m_unit_ctx.state)
            {
                throw new Exception("[UnitConnectBase::ConnectAndAssoc][Error] A configuração do unit_connect não foi carregada com sucesso.");
            }

            try
            {
                // Conecta ao IP e Porta fornecidos
                m_session.Connect(m_unit_ctx.ip, m_unit_ctx.port);
                //handle player and packets 
                Task.Run(() => accept_completed());

            }
            catch
            {
                return false;
                //throw new Exception("[UnitConnectBase::ConnectAndAssoc][Error] Falha ao conectar.", ex);
            }

            // On Connected
            onConnected();


            Thread thread = new Thread(() =>
            {
                onMonitor();
            });
            thread.IsBackground = true;
            thread.Start(); // Inicia a thread de verificação
            return true;
        }


        private void onMonitor()
        {
            _smp.message_pool.getInstance().push(new message("[unit_connect::onMonitor][Log] monitor iniciado com sucesso!", type_msg.CL_ONLY_FILE_LOG));

            while (true)
            {
                try
                {

                    // Evento de heartbeat
                    if (isLive())
                        onHeartBeat();

                }
                catch (exception e) // Exceção específica da aplicação
                {
                    _smp.message_pool.getInstance().push(new message(
                        $"[unit.Monitor][ErrorSystem] {e.GetType().Name}: {e.getFullMessageError()}\nStack Trace: {e.getStackTrace()}",
                        type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
                catch (Exception ex) // Exceções gerais do .NET
                {
                    _smp.message_pool.getInstance().push(new message(
                        $"[unit.Monitor][ErrorSystem] {ex.GetType().Name}: {ex.Message}\nStack Trace: {ex.StackTrace}",
                        type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
                Thread.Sleep(5000);
            }
        }

        private void accept_completed()
        {
            //send key
            bool raw = true;
            while (m_session.isConnected())
            {
                try
                {
                    if (!m_session.isConnected())
                    {
                        DisconnectSession(m_session);
                        break;
                    }

                    if (recv_client_new(m_session, raw).Result)
                    {
                        // Processa o pacote recebido
                        raw = false;//ja leu packet ket
                    }
                    else
                    {
                        DisconnectSession(m_session);
                        break;
                    }
                }
                catch (IOException ioEx)
                {
                    _smp.message_pool.getInstance().push(new message("[unit_connect::accept_completed][IOError] " + ioEx.Message, type_msg.CL_FILE_LOG_AND_CONSOLE));
                    DisconnectSession(m_session);
                    break;
                }
                catch (exception ex)
                {
                    _smp.message_pool.getInstance().push(new message("[unit_connect::accept_completed][ErrorSystem] " + ex.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                    DisconnectSession(m_session);
                    break;
                }
            }
        }

        protected override void dispach_packet_same_thread(Session _session, packet _packet)
        {
            if (_session == null || _session.isConnected() == false || _packet == null)
            {
                return;//nao esta mais conectado!
            }

            func_arr.func_arr_ex func = null;

            try
            {
                // Obtém a função correspondente ao tipo de pacote
                func = funcs.getPacketCall(_packet.getTipo());
            }
            catch (exception e)
            {
                _smp.message_pool.getInstance().push(new message($"[Server.DispatchpacketSameThread][ErrorSystem] {e.Message}, {e.getStackTrace()}", type_msg.CL_FILE_LOG_AND_CONSOLE));
                // Desconecta a sessão
                DisconnectSession(_session);
            }

            try
            {
                // Atualiza o tick do cliente
                _session.m_tick = Environment.TickCount;

                var pd = new ParamDispatch
                {
                    _session = _session,
                    _packet = _packet
                };
                try
                {
                    if (func != null && func.ExecCmd(pd) != 0)
                    {
                        // _smp.message_pool.getInstance().push(new message($"[Server.DispatchpacketSameThread][Error][MY] Ao tratar o pacote. ID: {_packet.getTipo()}(0x{_packet.getTipo():X})," + pd._packet.Log(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                        DisconnectSession(_session);
                    }
                }

                catch (exception e)
                {
                    _smp.message_pool.getInstance().push(new message($"[Server.DispatchpacketSameThread][Error][MY] {e.getFullMessageError()}", type_msg.CL_FILE_LOG_AND_CONSOLE));

                    DisconnectSession(_session);
                }
            }
            catch (exception e)
            {
                _smp.message_pool.getInstance().push(new message($"[Server.DispatchpacketSameThread][Error][MY] {e.Message}", type_msg.CL_FILE_LOG_AND_CONSOLE));

                DisconnectSession(_session);
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
                func = funcs_sv.getPacketCall(_packet.getTipo());
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

                var pd = new ParamDispatch
                {
                    _session = session,
                    _packet = _packet
                };
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
            catch (exception e)
            {
                _smp.message_pool.getInstance().push(new message($"[Server.DispatchpacketSameThread][Error][MY] {e.Message}", type_msg.CL_FILE_LOG_AND_CONSOLE));

                DisconnectSession(session);
            }
        }


        public override bool DisconnectSession(Session _session)
        {
            return _session.clear();//
        }

        private DateTime _lastReconnectAttempt = DateTime.MinValue;
        private int _retryCount = 0;

        private System.Threading.Timer _reconnectTimer;

        public void start()
        {
            _eventTryConnect.Set();

            _reconnectTimer = new System.Threading.Timer(_ =>
            {
                if (m_session.m_client.Connected)
                {
                    _reconnectTimer?.Dispose(); // já conectou, para o timer
                    return;
                }

                int delaySeconds = Math.Min(30, (int)Math.Pow(2, _retryCount));

                if ((DateTime.Now - _lastReconnectAttempt).TotalSeconds >= delaySeconds)
                {
                    try
                    {
                        ConnectAndAssoc();
                    }
                    catch (Exception ex)
                    {
                        _retryCount++;
                        _smp.message_pool.getInstance().push(
                            new message("[unit_auth_server_connect::start][ReconnectError] " + ex.Message,
                            type_msg.CL_FILE_LOG_AND_CONSOLE));
                    }

                    _lastReconnectAttempt = DateTime.Now;
                }

            }, null, 0, 1000); // roda a cada 1s
        }

        public func_arr funcs;
        public func_arr funcs_sv;
        public UnitPlayer m_session;
        public STATE m_state;
        public stUnitCtx m_unit_ctx;
        public IniHandle m_reader_ini;
        private AutoResetEvent _eventTryConnect = new AutoResetEvent(false);

        public class packet_func_as
        {
            public static void session_send(PangyaBinaryWriter p, UnitPlayer s, byte _debug)
            {
                s.requestSendBuffer(p.GetBytes);
            }
            public static void session_send(List<PangyaBinaryWriter> v_p, UnitPlayer s, byte _debug)
            {
                foreach (var writer in v_p)
                {
                    s.requestSendBuffer(writer.GetBytes);
                }

            }
        }

    }
}
