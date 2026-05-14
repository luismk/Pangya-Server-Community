using PangyaAPI.Network.Models;
using PangyaAPI.Network.PangyaPacket;
using PangyaAPI.Network.PangyaServer;
using PangyaAPI.Network.PangyaSession;
using PangyaAPI.Network.Repository;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Models;
using PangyaAPI.Utilities.Log;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace PangyaAPI.Network.PangyaUnit
{

    public class unit_auth_server_connect : unit_connect_base
    {
        public unit_auth_server_connect(Server server) : base(server.m_si)
        {
            this.m_owner_server = server;

            if (m_state == STATE.FAILURE)
            {
                _smp.message_pool.getInstance().push(new message("[unit_auth_server_connect::unit_auth_server_connect][Error] na inicializacao unit auth server connect", type_msg.CL_FILE_LOG_AND_CONSOLE));
                return;
            }

            try
            {

                /// ---------- Packets ---------
                init_Packets();
                /// ----------- Pacotes -----------

                // Initialized complete
                m_state = STATE.INITIALIZED;

            }
            catch (exception e)
            {

                m_state = STATE.FAILURE;

                _smp.message_pool.getInstance().push(new message("[unit_auth_server_connect::unit_auth_server_connect][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public virtual void requestFirstPacketKey(UnitPlayer _session, packet _packet)
        {
            if (!_session.getState())
            {
                throw new exception("[unit_auth_server_connect::request" + "FirstPacketKey" + "][Error] player nao esta connectado.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.UNIT_AUTH_SERVER_CONNECT,
                    1, 0));
            }
            if (_packet == null)
            {
                throw new exception("[unit_auth_server_connect::request" + "FirstPacketKey" + "][Error] _packet is null", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.UNIT_AUTH_SERVER_CONNECT,
                    6, 0));
            }

            try
            {

                _session.m_key = (byte)_packet.ReadUInt32();

                var server_guid = _packet.ReadUInt32();

                CmdNewAuthServerKey cmd_nask = new CmdNewAuthServerKey(_session.m_si.uid); // Waiter

                snmdb.NormalManagerDB.getInstance().add(0,
                    cmd_nask, null, null);

                if (cmd_nask.getException().getCodeError() != 0)
                {
                    throw cmd_nask.getException();
                }

                // Resposta para o Auth Server
                var p = new PangyaBinaryWriter((ushort)0x1);

                p.WriteUInt32((uint)_session.m_si.tipo);
                p.WriteUInt32((uint)_session.m_si.uid);
                p.WriteString(_session.m_si.nome);
                p.WriteString(cmd_nask.getInfo());
                p.WriteString(_session.m_si.version_client);
                p.WriteUInt32(m_session.m_si.packet_version);
                packet_func_as.session_send(p,
                    _session, 1);
                
            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[unit_auth_server_connect::requestFirstPacketKey][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        // 1. Mude para Task para melhor controle
        public async Task requestSendKeepLive(UnitPlayer _session)
        {
            if (_session == null) return;

            // 2. Use um CancellationToken se possível para encerrar o loop no desligamento do server
            while (isLive())
            {
                try
                {
                    // 3. Em vez de criar um objeto Writer gigante, 
                    // use um buffer pequeno ou reutilize o writer se sua arquitetura permitir
                    using (var p = new PangyaBinaryWriter(0xFF))
                    {
                        p.WriteInt32(_session.m_si.tipo);
                        p.WriteInt32(_session.m_si.uid);

                        // 4. Enviar o tempo do servidor (Unix Timestamp ou DateTime customizado)
                        p.WriteTime();

                        // 5. Envio direto e assíncrono (se o seu session_send permitir)
                        packet_func_as.session_send(p, _session, 1);
                    }

                    _session.last_activity = DateTime.Now;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[KeepLive] Erro na sessão {_session.m_si.uid}: {ex.Message}");
                    break;
                }

                // 6. Task.Delay é excelente pois não bloqueia a thread
                await Task.Delay(2000);
            }
        }
        public void requestRecvKeepLive(UnitPlayer _session, packet _packet)
        {
#if RELEASE
            _smp.message_pool.getInstance().push(new message("[unit_auth_server_connect::requestRecvKeepLive][Log] ", type_msg.CL_FILE_LOG_AND_CONSOLE));
#endif

            _session.last_activity = DateTime.Now;

        }

        public virtual void requestAskLogin(UnitPlayer _session, packet _packet)
        {
            try
            {

                int oid = _packet.ReadInt32();

                if (oid > -1)
                {
                    _session.m_oid = oid;
                    Task.Run(() => requestSendKeepLive(_session));
                }
                else
                {
                    _smp.message_pool.getInstance().push(new message("[unit_auth_server_connect::requestAskLogin][Log] Nao conseguiu logar com o Auth Server.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                } 
            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[unit_auth_server_connect::requestAskLogin][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public virtual void requestShutdownServer(UnitPlayer _session, packet _packet)
        {
            try
            {

                // Time In tv_sec for Shutdown
                int time = _packet.ReadInt32();

                m_owner_server.authCmdShutdown(time);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[unit_auth_server_connect::requestShutdownServer][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public virtual void requestBroadcastNotice(UnitPlayer _session, packet _packet)
        {
            try
            {

                var notice = _packet.ReadString();

                m_owner_server.authCmdBroadcastNotice(notice);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[unit_auth_server_connect::requestBroadcastNotice][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public virtual void requestBroadcastTicker(UnitPlayer _session, packet _packet)
        {
            try
            {

                var nickname = _packet.ReadString();
                var msg = _packet.ReadString();

                m_owner_server.authCmdBroadcastTicker(nickname, msg);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[unit_auth_server_connect::requestBroadcastTicker][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public virtual void requestBroadcastCubeWinRare(UnitPlayer _session, packet _packet)
        {
            try
            {

                uint option = _packet.ReadUInt32();
                var msg = _packet.ReadString();

                m_owner_server.authCmdBroadcastCubeWinRare(msg, (option));

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[unit_auth_server_connect::requestBroadcastCubeWinRare][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public virtual void requestDisconnectPlayer(UnitPlayer _session, packet _packet)
        {
            try
            {

                uint player_uid = _packet.ReadUInt32();
                uint server_uid = _packet.ReadUInt32();
                byte force = _packet.ReadUInt8(); // Flag que força a disconectar o usuário

                m_owner_server.authCmdDisconnectPlayer((server_uid),
                    (player_uid),
                    force);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[unit_auth_server_connect::requestDisconnectPlayer][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public virtual void requestConfirmDisconnectPlayer(UnitPlayer _session, packet _packet)
        { 
            try
            {

                uint player_uid = _packet.ReadUInt32();

                m_owner_server.authCmdConfirmDisconnectPlayer((player_uid));

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[unit_auth_server_connect::requestConfirmDisconnectPlayer][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public virtual void requestNewMailArrivedMailBox(UnitPlayer _session, packet _packet)
        { 
            try
            {

                uint player_uid = _packet.ReadUInt32();
                int mail_id = _packet.ReadInt32();

                m_owner_server.authCmdNewMailArrivedMailBox((player_uid), (mail_id));

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[unit_auth_server_connect::requestNewMailArrivedMailBox][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public virtual void requestNewRate(UnitPlayer _session, packet _packet)
        { 
            try
            {

                uint tipo = _packet.ReadUInt32();
                uint qntd = _packet.ReadUInt32();

                m_owner_server.authCmdNewRate((tipo), (qntd));

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[unit_auth_server_connect::requestNewRate][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public virtual void requestReloadSystem(UnitPlayer _session, packet _packet)
        { 
            try
            {

                uint sistema = _packet.ReadUInt32();

                m_owner_server.authCmdReloadGlobalSystem((sistema));

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[unit_auth_server_connect::requestReloadSystem][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public virtual void requestInfoPlayerOnline(UnitPlayer _session, packet _packet)
        {
            if (!_session.getState())
            {
                throw new exception("[unit_auth_server_connect::request" + "GetInfoPlayerOnline" + "][Error] player nao esta connectado.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.UNIT_AUTH_SERVER_CONNECT,
                    1, 0));
            }
            if (_packet == null)
            {
                throw new exception("[unit_auth_server_connect::request" + "GetInfoPlayerOnline" + "][Error] _packet is null", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.UNIT_AUTH_SERVER_CONNECT,
                    6, 0));
            }

            try
            {

                uint req_server_uid = _packet.ReadUInt32();
                uint player_uid = _packet.ReadUInt32();

                m_owner_server.authCmdInfoPlayerOnline((req_server_uid), (player_uid));

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[unit_auth_server_connect::requestInfoPlayerOnline][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public virtual void requestConfirmSendInfoPlayerOnline(UnitPlayer _session, packet _packet)
        {
           
            if (_packet == null)
            {
                throw new exception("[unit_auth_server_connect::request" + "ConfirmSendInfoOnline" + "][Error] _packet is null", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.UNIT_AUTH_SERVER_CONNECT,
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

                m_owner_server.authCmdConfirmSendInfoPlayerOnline(req_server_uid, aspi);

            }
            catch (exception e)
            { 
                _smp.message_pool.getInstance().push(new message("[unit_auth_server_connect::requestConfirmSendInfoPlayerOnline][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public virtual void requestSendCommandToOtherServer(UnitPlayer _session, packet _packet)
        { 
            try
            {

                m_owner_server.authCmdSendCommandToOtherServer(_packet);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[unit_auth_server_connect::requestSendCommandToOtherServer][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public virtual void requestSendReplyToOtherServer(UnitPlayer _session, packet _packet)
        { 
            try
            {

                m_owner_server.authCmdSendReplyToOtherServer(_packet);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[unit_auth_server_connect::requestSendReplyToOtherServer][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        // Request _writer
        public virtual void sendConfirmDisconnectPlayer(uint _server_uid, uint _player_uid)
        { 
            try
            {
                var p = new PangyaBinaryWriter((ushort)0x3);

                p.WriteUInt32(_player_uid);
                p.WriteUInt32(_server_uid);

                packet_func_as.session_send(p,
                    m_session, 1);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[unit_auth_server_connect::sendConfirmDisconnectPlayer][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public virtual void sendDisconnectPlayer(uint _server_uid, uint _player_uid)
        { 
            try
            {
                var p = new PangyaBinaryWriter((ushort)0x2);

                p.WriteUInt32(_player_uid);
                p.WriteUInt32(_server_uid);

                packet_func_as.session_send(p,
                    m_session, 1);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[unit_auth_server_connect::sendDisconnectPlayer][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public virtual void sendInfoPlayerOnline(uint _server_uid, AuthServerPlayerInfo _aspi)
        { 
            try
            {
                var p = new PangyaBinaryWriter((ushort)0x5);

                p.WriteUInt32(_server_uid);
                p.WriteInt32(_aspi.option);
                p.WriteUInt32(_aspi.uid);

                if (_aspi.option == 1)
                { 
                    p.WriteString(_aspi.id);
                    p.WriteString(_aspi.ip);
                }

                packet_func_as.session_send(p,
                    m_session, 1);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[unit_auth_server_connect::sendInfoPlayerOnline][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public virtual void getInfoPlayerOnline(uint _server_uid, uint _player_uid)
        { 
            try
            {
                var p = new PangyaBinaryWriter(0x04);

                p.WriteUInt32(_server_uid);
                p.WriteUInt32(_player_uid);

                packet_func_as.session_send(p,
                    m_session, 1);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[unit_auth_server_connect::getInfoPlayerOnline][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }
         

        public virtual void sendCommandToOtherServer(uint _server_uid, packet _packet)
        { 
            try
            {

                // Ler o command ID para verificar se está tudo ok
                _packet.ReadUInt16();

                if (_packet.GetSize < 2)
                {
                    throw new exception("[unit_auth_server_connect::sendCommandToOtherServer][Error] Tentou enviar o comando[ID=" + Convert.ToString(_packet.getTipo()) + "] para o outro server[UID=" + Convert.ToString(_server_uid) + "] com o Auth Server, mas o packet é invalido nao tem nem o id.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.UNIT_AUTH_SERVER_CONNECT,
                        1000, 0));
                }

                ushort command_buff_size = (ushort)(_packet.GetSize - 2u);

                CommandOtherServerHeaderEx cosh = new CommandOtherServerHeaderEx
                {
                    send_server_uid_or_type = _server_uid,
                    command_id = _packet.getTipo()
                };

                // Inicializa comando buffer
                cosh.command.init(command_buff_size);

                if (!cosh.command.is_good())
                {
                    throw new exception("[unit_auth_server_connect::sendCommandToOtherServer][Error] Tentou enviar a reposta[ID=" + Convert.ToString(_packet.getTipo()) + "] para o outro server[UID=" + Convert.ToString(_server_uid) + "] com o Auth Server, mas nao conseguiu allocar memoria para o command buffer. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.UNIT_AUTH_SERVER_CONNECT,
                        1001, 0));
                }

                cosh.command.buff = _packet.ReadBytes(cosh.command.size);

                // Envia o comando para o Auth Server enviar para o outro server
                var p = new PangyaBinaryWriter((ushort)0x06);
                p.WriteBytes(cosh.ToArray()); 
                packet_func_as.session_send(p,
                    m_session, 1);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[unit_auth_server_connect::sendCommandToOtherServer][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public virtual void sendReplyToOtherServer(uint _server_uid, packet _packet)
        { 
            try
            {


                // Ler a Resposta ID para verificar se está tudo ok
                _packet.ReadUInt16();

                if (_packet.GetSize < 2)
                {
                    throw new exception("[unit_auth_server_connect::sendReplyToOtherServer][Error] Tentou enviar a reposta[ID=" + Convert.ToString(_packet.getTipo()) + "] para o outro server[UID=" + Convert.ToString(_server_uid) + "] com o Auth Server, mas o packet é invalido nao tem nem o id.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.UNIT_AUTH_SERVER_CONNECT,
                        1000, 0));
                }

                ushort command_buff_size = (ushort)(_packet.GetSize - 2u);

                CommandOtherServerHeaderEx cosh = new CommandOtherServerHeaderEx();

                cosh.send_server_uid_or_type = _server_uid;
                cosh.command_id = _packet.Id;

                // Inicializa comando buffer
                cosh.command.init(command_buff_size);

                if (!cosh.command.is_good())
                {
                    throw new exception("[unit_auth_server_connect::sendReplyToOtherServer][Error] Tentou enviar a reposta[ID=" + Convert.ToString(_packet.getTipo()) + "] para o outro server[UID=" + Convert.ToString(_server_uid) + "] com o Auth Server, mas nao conseguiu allocar memoria para o command buffer. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.UNIT_AUTH_SERVER_CONNECT,
                        1001, 0));
                }

                cosh.command.buff = _packet.ReadBytes(cosh.command.size);

                // Envia a resposta para o Auth Server enviar para o outro server
                var p = new PangyaBinaryWriter((ushort)0x07); 
                p.WriteBytes(cosh.ToArray());   
                packet_func_as.session_send(p,
                    m_session, 1);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[unit_auth_server_connect::sendReplyToOtherServer][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        } 

        protected override void onHeartBeat()
        {
            try
            {
                // Se não inicializou ou a sessão caiu, o OnMonitor cuida disso.
                if (m_state != STATE.INITIALIZED || !this.isLive())
                    return; 

                // Se chegou aqui, a conexão está saudável
                _retryCount = 0;
            }
            catch (Exception e)
            {
                _smp.message_pool.getInstance().push(
                    new message("[unit_auth_server_connect::onHeartBeat][ErrorSystem] "
                    + e.Message, type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }


        protected override void onConnected()
        { 
            try
            {
            }
            catch (exception e)
            { 
                _smp.message_pool.getInstance().push(new message("[unit_auth_server_connect::onConnected][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        protected override void onDisconnect()
        {

            try
            { 
            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[unit_auth_server_connect::onDisconnect][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        //my codes

        void init_Packets()
        {
            AddPacketHandler(0x00, (uc, s, p) => uc.requestFirstPacketKey(m_session, p));
            AddPacketHandler(0x01, (uc, s, p) => uc.requestAskLogin(m_session, p));
            AddPacketHandler(0x02, (uc, s, p) => uc.requestShutdownServer(m_session, p));
            AddPacketHandler(0x03, (uc, s, p) => uc.requestBroadcastNotice(m_session, p));
            AddPacketHandler(0x04, (uc, s, p) => uc.requestBroadcastTicker(m_session, p));
            AddPacketHandler(0x05, (uc, s, p) => uc.requestBroadcastCubeWinRare(m_session, p));
            AddPacketHandler(0x06, (uc, s, p) => uc.requestDisconnectPlayer(m_session, p));
            AddPacketHandler(0x07, (uc, s, p) => uc.requestConfirmDisconnectPlayer(m_session, p)); 
            AddPacketHandler(0x08, (uc, s, p) => uc.requestNewMailArrivedMailBox(m_session, p));
            AddPacketHandler(0x09, (uc, s, p) => uc.requestNewRate(m_session, p));
            AddPacketHandler(0x0A, (uc, s, p) => uc.requestReloadSystem(m_session, p));
            AddPacketHandler(0x0B, (uc, s, p) => uc.requestInfoPlayerOnline(m_session, p));
            AddPacketHandler(0x0C, (uc, s, p) => uc.requestConfirmSendInfoPlayerOnline(m_session, p));
            AddPacketHandler(0x0D, (uc, s, p) => uc.requestSendCommandToOtherServer(m_session, p));
            AddPacketHandler(0x0E, (uc, s, p) => uc.requestSendReplyToOtherServer(m_session, p));
            AddPacketHandler(0xFE, (uc, s, p) => uc.requestRecvKeepLive(m_session, p));//server -> auth

            funcs_sv.addPacketCall((0x1), (object _arg1, ParamDispatch _arg2) =>
            {
                return 0;
            }, this);

            // Pacote002
            funcs_sv.addPacketCall((0x2), (object _arg1, ParamDispatch _arg2) =>
            {
                return 0;
            }, this);

            // Pacote003
            funcs_sv.addPacketCall((0x3), (object _arg1, ParamDispatch _arg2) =>
            {
                return 0;
            }, this);

            // Pacote004
            funcs_sv.addPacketCall((0x4), (object _arg1, ParamDispatch _arg2) =>
            {
                return 0;
            }, this);

            // Pacote005
            funcs_sv.addPacketCall((0x5), (object _arg1, ParamDispatch _arg2) =>
            {
                return 0;
            }, this);

            // Pacote005
            funcs_sv.addPacketCall((0x6), (object _arg1, ParamDispatch _arg2) =>
            {
                return 0;
            }, this);

            // Pacote005
            funcs_sv.addPacketCall((0x7), (object _arg1, ParamDispatch _arg2) =>
            {
                return 0;
            }, this);

            // Pacote005
            funcs_sv.addPacketCall((0xFF), (object _arg1, ParamDispatch _arg2) =>
            {
                return 0;
            }, this);
        }
        // Método auxiliar reutilizável
        void AddPacketHandler(byte packetId, Action<unit_auth_server_connect, Session, packet> handler)
        {
            funcs.addPacketCall(packetId, (object _arg1, ParamDispatch _arg2) =>
            {
                var pd = new ParamDispatchAS(_arg2);
                var uc = (unit_auth_server_connect)_arg1;

                try
                {
                    handler(uc, pd._session, pd._packet);
                }
                catch (exception e)
                {
                    string hexId = packetId.ToString("X3"); // exemplo: 00A
                    _smp.message_pool.getInstance().push(
                        new message($"[packet_func::packet{hexId}][ErrorSystem] {e.getFullMessageError()}", type_msg.CL_FILE_LOG_AND_CONSOLE)
                    );
                }

                return 0;
            }, this);
        }
        //
        protected IUnitAuthServer m_owner_server;
    }
}
