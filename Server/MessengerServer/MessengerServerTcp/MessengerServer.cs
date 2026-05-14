using Pangya_MessengerServer.Manager;
using Pangya_MessengerServer.Models;
using Pangya_MessengerServer.PacketFunc;
using Pangya_MessengerServer.PangyaEnums;
using Pangya_MessengerServer.Repository;
using Pangya_MessengerServer.Session;
using PangyaAPI.IFF.JP.Extensions;
using PangyaAPI.Network.Models;
using PangyaAPI.Network.PangyaPacket;
using PangyaAPI.Network.PangyaServer;
using PangyaAPI.Network.PangyaUtil;
using PangyaAPI.Network.Repository;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Models;
using PangyaAPI.Utilities.Log;
using snmdb;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace Pangya_MessengerServer.MessengerServerTcp
{
    public class MessengerServer : Server
    {
        public const int FRIEND_LIST_LIMIT = 50;
        public const int FRIEND_PAG_LIMIT = 30;
        static player_manager m_player_manager = new player_manager();
        public MessengerServer() : base(m_player_manager)
        {
            if (m_state == ServerState.Failure)
            {
                _smp.message_pool.getInstance().push(new message("[MessengerServer::message_server][Error] falha ao incializar o message server.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                return;
            }
            try
            {
                config_init();

                // Carrega IFF_STRUCT
                if (!sIff.getInstance().isLoad())
                    sIff.getInstance().initilation();

                // Request Cliente
                init_packets();

                // Initialized complete
                m_state = ServerState.Initialized;
            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[MessengerServer::message_server][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

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
                throw new exception("[MessengerServer::onDisconnect][Error] _session is nullptr.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MESSAGE_SERVER, 60, 0));

            Player p = (Player)_session;

            bool ret = false;

            try
            {
                if(Interlocked.CompareExchange(ref p.m_pi.m_logout, p.m_pi.m_logout, 0) == 0)
                {
                    ret = sendUpdatePlayerLogoutToFriends(p);
                }
            }
            catch (exception e)
            {
                _smp.message_pool.getInstance().push(new message("[MessengerServer::onDisconnected][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
            // Log para não mostrar essa mensagem 2x (evita spam se o logout já foi processado)
            if (ret)
                _smp.message_pool.getInstance().push(new message("[MessengerServer::onDisconnected][Log] PLAYER[ID: " + (p.m_pi.id) + ", UID: " + (p.m_pi.uid) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));
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

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[MessengerServer::onHeartBeat][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            return;
        }

        public override void OnStart()
        {
            Console.Title = $"Messenger Server - P: {m_si.curr_user}";
            m_state = ServerState.Initialized;
        }

        protected override void onAcceptCompleted(PangyaAPI.Network.PangyaSession.Session _session)
        {
            try
            {

                var p = new packet(0x2E);

                p.AddByte(1);
                p.AddByte(1);
                p.AddUInt32(_session.m_key);
                p.makeRaw();
                _session.requestSendBuffer(p.getBuffer(), true);
            }
            catch (exception ex)
            {
                _smp.message_pool.getInstance().push(new message(
              $"[MessengerServer.onAcceptCompleted][ErrorSt]: {ex.getFullMessageError()}",
              type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        /// <summary>
        /// init packet to call !
        /// </summary>
        protected void init_packets()
        {
            packet_func.funcs.addPacketCall(0x12, packet_func.packet012, this);
            packet_func.funcs.addPacketCall(0x13, packet_func.packet013, this);
            packet_func.funcs.addPacketCall(0x14, packet_func.packet014, this);
            packet_func.funcs.addPacketCall(0x16, packet_func.packet016, this);
            packet_func.funcs.addPacketCall(0x17, packet_func.packet017, this);
            packet_func.funcs.addPacketCall(0x18, packet_func.packet018, this);
            packet_func.funcs.addPacketCall(0x19, packet_func.packet019, this);
            packet_func.funcs.addPacketCall(0x1A, packet_func.packet01A, this);
            packet_func.funcs.addPacketCall(0x1B, packet_func.packet01B, this);
            packet_func.funcs.addPacketCall(0x1C, packet_func.packet01C, this);
            packet_func.funcs.addPacketCall(0x1D, packet_func.packet01D, this);
            packet_func.funcs.addPacketCall(0x1E, packet_func.packet01E, this);
            packet_func.funcs.addPacketCall(0x1F, packet_func.packet01F, this);
            packet_func.funcs.addPacketCall(0x23, packet_func.packet023, this);
            packet_func.funcs.addPacketCall(0x24, packet_func.packet024, this);
            packet_func.funcs.addPacketCall(0x25, packet_func.packet025, this);
            packet_func.funcs.addPacketCall(0x28, packet_func.packet028, this);
            packet_func.funcs.addPacketCall(0x29, packet_func.packet029, this);
            packet_func.funcs.addPacketCall(0x2A, packet_func.packet02A, this);
            packet_func.funcs.addPacketCall(0x2B, packet_func.packet02B, this);
            packet_func.funcs.addPacketCall(0x2C, packet_func.packet02C, this);
            packet_func.funcs.addPacketCall(0x2D, packet_func.packet02D, this);

            // Resposta Server
            packet_func.funcs_sv.addPacketCall(0x2E, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x2F, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x30, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x3B, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x3C, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x40, packet_func.packet_svFazNada, this); // Msg Aviso Lobby, cliente tamb�m aceita o Message Server enviar esse Pacote

            // Auth Server
            packet_func.funcs_as.addPacketCall(0x01, packet_func.packet_as001, this);
            packet_func.funcs_as.addPacketCall(0x02, packet_func.packet_as002, this);
            packet_func.funcs_as.addPacketCall(0x03, packet_func.packet_as003, this);
        }


        // Request Login
        public void requestLogin(Player _session, packet _packet)
        {
            var p = new PangyaBinaryWriter();

            try
            {

                uint uid = _packet.ReadUInt32();
                var nickname = _packet.ReadString();



                if (uid == 0)
                    throw new exception("[MessengerServer::requestLogin][Error] player[UID=" + (uid) + ", NICKNAME="
                            + nickname + "] tentou logar com Server, mas o uid eh invalido. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MESSAGE_SERVER, 1, 0x5200101));

                if (nickname.empty())
                    throw new exception("[MessengerServer::requestLogin][Error] player[UID=" + (uid) + ", NICKNAME="
                            + nickname + "] tentou logar com Server, mas o nickname esta vazio. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MESSAGE_SERVER, 2, 0x5200102));

                var cmd_pi = new CmdPlayerInfo(uid);    // Waiter

                snmdb.NormalManagerDB.getInstance().add(0, cmd_pi, null, null);

                if (cmd_pi.getException().getCodeError() != 0)
                    throw cmd_pi.getException();

                _session.m_pi.set_info(cmd_pi.getInfo());

                if (nickname.CompareTo(_session.m_pi.nickname) != 0)
                    throw new exception("[MessengerServer::requestLogin][Error] player[UID=" + (uid) + ", NICKNAME="
                            + nickname + "] tentou logar com Server, mas o nickname do databse[NICKNAME_DB=" + (_session.m_pi.nickname) + "] eh diferente do fornecido pelo cliente. Hacker ou Bug",
                            ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MESSAGE_SERVER, 4, 0x5200104));

                _smp.message_pool.getInstance().push(new message($"[MessengerServer::RequestLogin][Log] PLAYER[ID: {cmd_pi.getInfo().id}, UID: {cmd_pi.getInfo().uid}]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Verifica se o player est� bloqueado
                if (_session.m_pi.block_flag.m_id_state.ull_IDState != 0)
                {

                    if (_session.m_pi.block_flag.m_id_state.L_BLOCK_TEMPORARY && (_session.m_pi.block_flag.m_id_state.block_time == -1 || _session.m_pi.block_flag.m_id_state.block_time > 0))
                    {

                        throw new exception("[MessengerServer::requestLogin][Log] Bloqueado por tempo[Time="
                                + (_session.m_pi.block_flag.m_id_state.block_time == -1 ? ("indeterminado") : ((_session.m_pi.block_flag.m_id_state.block_time / 60)
                                + "min " + (_session.m_pi.block_flag.m_id_state.block_time % 60) + "sec"))
                                + "]. player [UID=" + (_session.m_pi.uid) + ", ID=" + (_session.m_pi.id) + "]", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MESSAGE_SERVER, 1029, 0));

                    }
                    else if (_session.m_pi.block_flag.m_id_state.L_BLOCK_FOREVER)
                    {

                        throw new exception("[MessengerServer::requestLogin][Log] Bloqueado permanente. player [UID=" + (_session.m_pi.uid)
                                + ", ID=" + (_session.m_pi.id) + "]", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MESSAGE_SERVER, 1030, 0));

                    }
                    else if (_session.m_pi.block_flag.m_id_state.L_BLOCK_ALL_IP)
                    {

                        // Bloquea todos os IP que o player logar e da error de que a area dele foi bloqueada

                        // Add o ip do player para a lista de ip banidos
                        snmdb.NormalManagerDB.getInstance().add(1, new CmdInsertBlockIp(_session.m_ip, "255.255.255.255"), SQLDBResponse, this);

                        // Resposta
                        throw new exception("[MessengerServer::requestLogin][Log] Player[UID=" + (_session.m_pi.uid) + ", IP=" + (_session.getIP())
                                + "] Block ALL IP que o player fizer login.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MESSAGE_SERVER, 1031, 0));

                    }
                }

                // Verifica se j� tem outro socket com o mesmo uid conectado
                var s = (Player)HasLoggedWithOuterSocket(_session);

                if (s != null)
                    DisconnectSession(s);

                //// Verifica com o Auth Server se o player est� connectado no server que ele diz e se � o mesmo IP ADDRESS
                if (m_unit_connect.isLive())
                {
                    confirmLoginOnOtherServer(_session, _session.m_pi.server_uid, new AuthServerPlayerInfo { uid = _session.getUID(), id = _session.getID(), ip = _session.getIP(), option = 1});
                } 
                else//entra mizeraaaaaaa
                {
                    DisconnectSession(_session);
                }
            }
            catch (exception e)
            {

                // Resposta
                p.init_plain(0x2F);

                p.WriteByte(1);  // Error;

                packet_func.session_send(p, _session, 1);

                DisconnectSession(_session);

                _smp.message_pool.getInstance().push(new message("[MessengerServer::requestLogin][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void confirmLoginOnOtherServer(Player _session, uint _req_server_uid, AuthServerPlayerInfo _aspi)
        {
            // Usar o 'using' garante que o buffer do pacote seja liberado da memória (importante no Linux/Docker)
            using (var p = new PangyaBinaryWriter())
            {
                try
                {
                    // Validações de Segurança
                    //if (_aspi.uid != _session.m_pi.uid ||
                    //    //_aspi.option != 1 ||
                    //    _aspi.id != _session.m_pi.id ||
                    //    _aspi.ip != _session.getIP())
                    //{
                    //    goto send_error;
                    //}

                    // --- Bloco de SUCESSO ---

                    // Inicializa lista de amigos
                    _session.m_pi.m_friend_manager.init(_session.m_pi);

                    // Estado 4 = Online/Lobby
                    _session.m_pi.m_state = 4;
                    _session.m_is_authorized = true;

                    _smp.message_pool.getInstance().push(new message($"[MessengerServer] Player[UID={_session.m_pi.uid}] logou com sucesso!", type_msg.CL_FILE_LOG_AND_CONSOLE));

                    // Resposta de Sucesso (0x2F)
                    p.init_plain(0x2F);
                    p.WriteByte(0); // OK
                    p.WriteUInt32(_session.m_pi.uid);

                    packet_func.session_send(p, _session, 1);

                    return; // IMPORTANTE: Sai do método aqui para não executar o erro abaixo!

                send_error:
                    // --- Bloco de ERRO ---
                    p.init_plain(0x2F);
                    p.WriteByte(1); // Error (Geralmente 1 ou 2 dependendo do cliente)

                    packet_func.session_send(p, _session, 1);

                    // Fecha a conexão de forma segura
                    if (_session.m_client.Connected)
                    {
                        _session.m_client.Client.Shutdown(System.Net.Sockets.SocketShutdown.Both);
                        _session.m_client.Close();
                    }
                }
                catch (Exception e)
                {
                    _smp.message_pool.getInstance().push(new message($"[MessengerServer::confirmLogin] Error: {e.Message}", type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
            }
        }

        public void requestFriendAndGuildMemberList(Player _session, packet _packet)
        {   //REQUEST_BEGIN("FriendAndGuildMemberList");

            var p = new PangyaBinaryWriter();

            try
            {

                _smp.message_pool.getInstance().push(new message("[FriendList][Log] envia lista de amigos para o player[UID=" + (_session.m_pi.uid) + ", FRIENDS=" + _session.m_pi.m_friend_manager.getAllFriendAndGuildMember().Count + "].", type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Verifica se Session est� varrizada para executar esse a��o, 
                // se ele n�o fez o login com o Server ele n�o pode fazer nada at� que ele fa�a o login
                //CHECK_SESSION_IS_AUTHORIZED("FriendAndGuildMemberList");

                var friend_list = _session.m_pi.m_friend_manager.getAllFriendAndGuildMember();

                var mp = new ManyPacket((ushort)friend_list.Count, FRIEND_PAG_LIMIT);

                // UPDATE ON GAME
                p.init_plain(0x30);

                p.WriteUInt16(0x115);   // Sub packet Id

                p.WriteUInt32(_session.m_pi.uid);
                p.WriteUInt32(_session.m_pi.m_state);

                p.WriteByte(1); // OK

                p.WriteBytes(_session.m_pi.m_cpi.ToArray());

                // Send To Player
                packet_func.session_send(p, _session, 1);

                FriendInfoEx pFi = null;

                // Resposta para Lista de Amigos e Membros da Guild
                if (mp.paginas > 0)
                {
                    for (var i = 0; i < mp.paginas; i++, mp.increse())
                    {
                        p.init_plain(0x30);

                        p.WriteUInt16(0x102);   // Sub packet Id

                        p.WriteBytes(mp.pag.ToArray());

                        var _begin = friend_list.Skip(mp.index.start)/*  // Pula até o índice de início*/.Take(mp.index.end - mp.index.start); // Pega apenas os elementos entre start e end

                        foreach (var fi in _begin)
                        {
                            p.WriteBytes(fi.ToArray());
                            var s = (Player)(m_session_manager.findSessionByUID((fi).uid) == null ? m_session_manager.findSessionByUID((fi).uid) : m_session_manager.FindSessionByNickname((fi).nickname));

                            // Se o Player tem ele na lista de amigos, e ele n�o estiver bloqueado na lista do amigo
                            if (s != null && (pFi = s.m_pi.m_friend_manager.findFriendInAllFriend(_session.m_pi.uid)) != null)
                            {   // Player est� online

                                p.WriteBytes(s.m_pi.m_cpi.ToArray());

                                // State Icon Player
                                p.WriteByte(s.m_pi.m_state);

                                switch (s.m_pi.m_state)
                                {
                                    case 0: // IN GAME
                                        (fi).state.play = 1;
                                        break;
                                    case 1: // AFK
                                        (fi).state.AFK = 1;
                                        break;
                                    case 3: // BUSY
                                        (fi).state.busy = 1;
                                        break;
                                    case 4: // ON
                                    default:
                                        (fi).state.online = 1;
                                        break;
                                }

                                // Online
                                (fi).state.online = 1;

                            }
                            else
                            {   // player n�o est� online
                                p.WriteInt16(-1);       // Sala Numero
                                p.WriteInt32(-1);       // Sala Tipo
                                p.WriteInt32(-1);       // Server GUID
                                p.WriteSByte(-1);        // Canal ID
                                p.WriteZero(64);    // Canal Nome

                                // State Icon Player, OFFLINE not change icon
                                p.WriteByte(5); // OFFLINE

                                // Offline
                                (fi).state.online = 0;
                            }

                            p.WriteByte(fi.cUnknown_flag);

                            // Aqui quando � o player e ele est� guild � 1/*Master*/, 2 sub, e outros membro guild � 0, e quando � friend � o level
                            p.WriteByte((byte)(fi.flag.ucFlag == 2/*S� Guild Member*/ ? (fi.uid == _session.m_pi.uid ? 1/*Master*/ : 0) : fi.level));

                            p.WriteByte(fi.state.ucState);
                            p.WriteByte(fi.flag.ucFlag);
                        }

                        packet_func.session_send(p, _session, 1);
                    }

                }
                else
                {

                    // N�o tem nenhum amigo, manda a p�gina vazia
                    p.init_plain(0x30);

                    p.WriteUInt16(0x102);   // Sub packet Id

                    p.WriteBytes(mp.pag.ToArray());

                    packet_func.session_send(p, _session, 1);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[channel::requestFriendAndGuildMemberList][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x30);

                p.WriteUInt16(0x102);   // Sub packet Id

                p.WriteByte(1); // pagina

                p.WriteUInt32(0);   // 0 Members

                packet_func.session_send(p, _session, 1);
            }

        }

        public void requestUpdateChannelPlayerInfo(Player _session, packet _packet)
        {
            var p = new PangyaBinaryWriter();

            try
            {

                var cpi = new ChannelPlayerInfo().ToRead(_packet);

                _session.m_pi.m_cpi = cpi;

                _smp.message_pool.getInstance().push(new message("[MessengerServer::UpdateChannelPlayerInfo][Log] player[UID= " + (_session.m_pi.uid) + "] Atualizou Channel Info[NAME="
                        + (_session.m_pi.m_cpi.name) + ", ID= " + (_session.m_pi.m_cpi.id) + ", ROOM= " + (_session.m_pi.m_cpi.room.number == ushort.MaxValue ? -1 : _session.m_pi.m_cpi.room.number)
                        + ", ROOM_TYPE= " + (_session.m_pi.m_cpi.room.type == -1 ? GAMETYPE.DEFAULT : _session.m_pi.m_cpi.room.type == 40 ? GAMETYPE.DEFAULT : (GAMETYPE)_session.m_pi.m_cpi.room.type) + ", SERVER_UID= " + (_session.m_pi.m_cpi.server_uid) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                // UPDATE ON GAME
                p.init_plain(0x30);

                p.WriteUInt16(0x115);   // Sub packet Id

                p.WriteUInt32(_session.m_pi.uid);
                p.WriteUInt32(_session.m_pi.m_state);

                p.WriteByte(1); // OK

                p.WriteBytes(_session.m_pi.m_cpi.ToArray());

                // Send To Player
                packet_func.session_send(p, _session, 1);

                // Send To Player Friend(s)
                packet_func.friend_broadcast(m_player_manager.findAllFriend(_session.m_pi.m_friend_manager.getAllFriendAndGuildMember(true/*Not Send To Block Friend*/)), p, _session, 1);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[MessengerServer::requestUpdateChannelPlayerInfo][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x30);

                p.WriteUInt16(0x115);   // Sub packet Id

                p.WriteUInt32(_session.m_pi.uid);
                p.WriteUInt32(_session.m_pi.m_state);

                p.WriteByte(0); // Error(ACHO)

                packet_func.session_send(p, _session, 1);
            }
        }
        public void requestUpdatePlayerState(Player _session, packet _packet)
        {
            //REQUEST_BEGIN("UpdatePlayerState");

            var p = new PangyaBinaryWriter();

            try
            {

                var state = _packet.ReadUInt8();

                // Verifica se Session est� varrizada para executar esse a��o, 
                // se ele n�o fez o login com o Server ele n�o pode fazer nada at� que ele fa�a o login
                //CHECK_SESSION_IS_AUTHORIZED("UpdatePlayerState");

                // S� Atualiza se o state for diferente
                if (_session.m_pi.m_state != state)
                    _session.m_pi.m_state = state;

                // Update ON GAME - To player friend(s)
                p.init_plain(0x30);

                p.WriteUInt16(0x115);   // Sub packet Id

                p.WriteUInt32(_session.m_pi.uid);
                p.WriteUInt32(_session.m_pi.m_state);

                p.WriteByte(1); // OK

                p.WriteBytes(_session.m_pi.m_cpi.ToArray());

                // Send To Player Friend(s)
                packet_func.friend_broadcast(m_player_manager.findAllFriend(_session.m_pi.m_friend_manager.getAllFriendAndGuildMember(true/*Not Send To Block Friend*/)), p, _session, 1);
                switch ((USER_STATUS)state)
                {
                    case USER_STATUS.IS_PLAYING:
                        // Log
                        _smp.message_pool.getInstance().push(new message("[MessengerServer::requestUpdatePlayerState][Log] player[UID=" + (_session.m_pi.uid) + "] PLAYING", type_msg.CL_FILE_LOG_AND_CONSOLE));
                        break;
                    case USER_STATUS.IS_RECONNECT:
                        // Log
                        _smp.message_pool.getInstance().push(new message("[MessengerServer::requestUpdatePlayerState][Log] player[UID=" + (_session.m_pi.uid) + "] SLEEP", type_msg.CL_FILE_LOG_AND_CONSOLE));

                        break;
                    case USER_STATUS.IS_ONLINE:
                        // Log
                        _smp.message_pool.getInstance().push(new message("[MessengerServer::requestUpdatePlayerState][Log] player[UID=" + (_session.m_pi.uid) + "] ONLINE", type_msg.CL_FILE_LOG_AND_CONSOLE));
                        break;
                    case USER_STATUS.IS_IDLE:

                        // Log
                        _smp.message_pool.getInstance().push(new message("[MessengerServer::requestUpdatePlayerState][Log] player[UID=" + (_session.m_pi.uid) + "] BUSY", type_msg.CL_FILE_LOG_AND_CONSOLE));

                        break;
                    default:
                        break;
                }
            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[MessengerServer::requestUpdatePlayerState][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestUpdatePlayerLogout(Player _session, packet _packet)
        {
            try
            {
               sendUpdatePlayerLogoutToFriends(_session); 
            }
            catch (exception e)
            {
                _smp.message_pool.getInstance().push(new message("[MessengerServer::requestUpdatePlayerLogout][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestChatFriend(Player _session, packet _packet)
        {
            //REQUEST_BEGIN("ChatFriend");

            var p = new PangyaBinaryWriter();

            try
            {

                uint uid = _packet.ReadUInt32();
                // Supondo codificação UTF-32 (little-endian)
                string msg = _packet.ReadPStr();


                if (string.IsNullOrEmpty(msg))
                    throw new exception("[MessengerServer::requestChatFriend][Error] PLAYER[UID=" + (_session.m_pi.uid) + "] tentou enviar Message[MESSAGE="
                            + msg + "], vazio. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 1, 1/*UNKNOWN ERROR*/));

                if (!Tools.Sanitize(msg))
                    throw new exception("[MessengerServer::requestChatFriend][Error] PLAYER[UID=" + (_session.m_pi.uid) + "] tentou enviar Message[MESSAGE="
                            + msg + "], tentativa de inject. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 1, 1/*UNKNOWN ERROR*/));


                if (uid == 0)
                    throw new exception("[MessengerServer::requestChatFriend][Error] player[UID=" + (_session.m_pi.uid) + "] tentou enviar Message[MSG="
                            + msg + "] para o Amigo[UID=" + (uid) + "], mas o uid is invalid(zero). Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MESSAGE_SERVER, 1, 0x5200301));

                if (msg.empty())
                    throw new exception("[MessengerServer::requestChatFriend][Error] player[UID=" + (_session.m_pi.uid) + "] tentou enviar Message[MSG="
                            + msg + "] para o Amigo[UID=" + (uid) + "], mas msg is empty. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MESSAGE_SERVER, 2, 0x5200302));

                var pFi = _session.m_pi.m_friend_manager.findFriendInAllFriend(uid);

                if (pFi == null)
                    throw new exception("[MessengerServer::requestChatFriend][Error] player[UID=" + (_session.m_pi.uid) + "] tentou enviar Message[MSG="
                            + msg + "] para o Amigo[UID=" + (uid) + "], mas player nao eh amigo dele. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MESSAGE_SERVER, 3, 0x5200303));

                if (pFi.state.block == 1)
                    throw new exception("[MessengerServer::requestChatFriend][Error] player[UID=" + (_session.m_pi.uid) + "] tentou enviar Message[MSG="
                            + msg + "] para o Amigo[UID=" + (uid) + "], mas o amigo esta bloqueado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MESSAGE_SERVER, 4, 0x5200304));

                var s = (Player)m_player_manager.findSessionByUID(uid);

                if (s == null)
                    throw new exception("[MessengerServer::requestChatFriend][Error] player[UID=" + (_session.m_pi.uid) + "] tentou enviar Message[MSG="
                            + msg + "] para o Amigo[UID=" + (uid) + "], mas o Amigo nao esta online.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MESSAGE_SERVER, 5, 0x5200305));

                pFi = s.m_pi.m_friend_manager.findFriendInAllFriend(_session.m_pi.uid);

                if (pFi == null)
                    throw new exception("[MessengerServer::requestChatFriend][Error] player[UID=" + (_session.m_pi.uid) + "] tentou enviar Message[MSG="
                            + msg + "] para o Amigo[UID=" + (uid) + "], mas o amigo nao tem ele na lista de amigos. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MESSAGE_SERVER, 6, 0x5200306));

                if (pFi.state.block == 1)
                    throw new exception("[MessengerServer::requestChatFriend][Error] player[UID=" + (_session.m_pi.uid) + "] tentou enviar Message[MSG="
                            + msg + "] para o Amigo[UID=" + (uid) + "], mas amigo bloqueou ele. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MESSAGE, 7, 0x5200307));

                // Log Para os GMs
                var gm = m_player_manager.findAllGM();

                if (!gm.empty())
                {

                    var msg_gm = "\\5" + (_session.m_pi.nickname) + ">" + (s.m_pi.nickname) + ": '" + msg + "'";

                    foreach (Player el in gm)
                    {

                        // Nao envia o log de MSN.PM novamente para o GM que enviou ou recebeu MSN.PM
                        if (el.m_pi.uid != _session.m_pi.uid && el.m_pi.uid != s.m_pi.uid)
                        {
                            // Responde no chat do player
                            p.init_plain(0x40);

                            p.WriteByte(0);

                            p.WriteString("\\1[MSN.PM]");   // Nickname

                            p.WriteString(msg_gm);  // Message

                            packet_func.session_send(p, el, 1);
                        }
                    }
                    //await DiscordWebhook.ChatLog("[MessengerServer::ChatFriend][Log] player[UID=" + (_session.m_pi.nickname) + "] enviou Message[MSG="
                    //    + msg + "] para seu Amigo[UID=" + (s.m_pi.nickname) + "]");
                }

                // Log
                _smp.message_pool.getInstance().push(new message("[ChatFriend][Log] player[UID=" + (_session.m_pi.uid) + "] enviou Message[MSG="
                        + msg + "] para seu Amigo[UID=" + (s.m_pi.uid) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Resposta para send chat to friend
                p.init_plain(0x30);

                p.WriteUInt16(0x113);   // Sub packet Id

                p.WriteUInt32(_session.m_pi.uid);           // FROM
                p.WriteString(_session.m_pi.nickname);  // FROM
                p.WriteString(msg);
                p.WriteByte(0); // Chat Friend
                packet_func.session_send(p, s, 1);      // TO

                // ------------------------------- Chat History Discord ------------------------------------
                // Envia a mensagem para o discord chat log se estiver ativado

                // Verifica se o m_chat_discod flag est� ativo para enviar o chat para o discord
                //if (m_si.rate.smart_calculator && m_chat_discord)
                //    sendMessageToDiscordChatHistory(
                //        "[MSN.PM]",                                                                                                     // From
                //        (_session.m_pi.nickname) + ">" + (s.m_pi.nickname) + ": '" + msg + "'"                      // Msg
                //    );

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[MessengerServer::requestChatFriend][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x30);

                p.WriteUInt16(0x113);   // Sub packet Id

                p.WriteInt32(-1);   // Error

                packet_func.session_send(p, _session, 1);
            }
        }

        public void requestChatGuild(Player _session, packet _packet)
        {
            //REQUEST_BEGIN("ChatGuild");

            var p = new PangyaBinaryWriter();

            try
            {

                var msg = _packet.ReadString();

                // Verifica se Session est� varrizada para executar esse a��o, 
                // se ele n�o fez o login com o Server ele n�o pode fazer nada at� que ele fa�a o login
                //CHECK_SESSION_IS_AUTHORIZED("ChatGuild");

                if (_session.m_pi.guild_uid == 0)
                    throw new exception("[MessengerServer::requestChatGuild][Error] player[UID=" + (_session.m_pi.uid) + "] tentou enviar Message[MSG="
                            + msg + "] para o Chat da Guild[UID=" + (_session.m_pi.guild_uid) + "], mas o player nao esta em uma guild. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MESSAGE_SERVER, 1, 0x5200401));

                if (msg.empty())
                    throw new exception("[MessengerServer::requestChatGuild][Error] player[UID=" + (_session.m_pi.uid) + "] tentou enviar Message[MSG="
                            + msg + "] para o Chat da Guild[UID=" + (_session.m_pi.guild_uid) + "], mas a msg is empty. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MESSAGE_SERVER, 2, 0x5200402));

                // Log Para os GMs
                var gm = m_player_manager.findAllGM();

                if (!gm.empty())
                {

                    var guild_name = (_session.m_pi.guild_name);

                    var index = -1;

                    while ((index = guild_name.IndexOf(' ', (index != -1 ? index + 1 : 0))) != -1)
                        guild_name = guild_name.Remove(index, 1).Insert(index, " \\2");

                    var msg_gm = "[\\2" + guild_name + "\\0]\\5>" + (_session.m_pi.nickname) + ": '" + msg + "'";

                    foreach (Player el in gm)
                    {

                        // Nao envia o log de Club Chat novamente para o GM que enviou ou recebeu Club Chat
                        if (el.m_pi.uid != _session.m_pi.uid && el.m_pi.guild_uid != _session.m_pi.guild_uid)
                        {
                            // Responde no chat do player
                            p.init_plain(0x40);

                            p.WriteByte(0);

                            p.WriteString("\\1[CC]");   // Nickname

                            p.WriteString(msg_gm);  // Message

                            packet_func.session_send(p, el, 1);
                        }
                    }
                }

                // Log

                _smp.message_pool.getInstance().push(new message("[ChatGuild][Log] player[UID=" + (_session.m_pi.uid) + "] enviu Message[MSG=" + msg + "] no Chat da Guild[UID="
                        + (_session.m_pi.guild_uid) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Resposta para send chat to Guild
                p.init_plain(0x30);

                p.WriteUInt16(0x113);   // Sub packet Id

                p.WriteUInt32(_session.m_pi.uid);           // FROM
                p.WriteString(_session.m_pi.nickname);  // FROM
                p.WriteString(msg);

                p.WriteByte(1); // Chat Guild

                packet_func.session_send(p, _session, 1);   // SEND TO PLAYER TOO

                // Usa o m_player_manager.findAllGuildMember, que pega todos os players que est�o na mesma guild
                packet_func.friend_broadcast(m_player_manager.findAllGuildMember(_session.m_pi.guild_uid), p, _session, 1); // All GUILD MEMBER
                                                                                                                            //packet_func.friend_broadcast(m_player_manager.findAllFriend(_session.m_pi.m_friend_manager.getAllGuildMember()), p, _session, 1);	// ALL GUILD MEMBER

                // ------------------------------- Chat History Discord ------------------------------------
                // Envia a mensagem para o discord chat log se estiver ativado

                // Verifica se o m_chat_discod flag est� ativo para enviar o chat para o discord
                //if (m_si.rate.smart_calculator && m_chat_discord)
                //    sendMessageToDiscordChatHistory(
                //        "[CC]",                                                                                                             // From
                //        "[" + (_session.m_pi.guild_name) + "]>" + (_session.m_pi.nickname) + ": '" + msg + "'"      // Msg
                //    );

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[MessengerServer::requestChatGuild][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x30);

                p.WriteUInt16(0x113);   // Sub packet Id

                p.WriteInt32(-1);  // Error

                packet_func.session_send(p, _session, 1);
            }
        }

        public void requestCheckNickname(Player _session, packet _packet)
        {
            //REQUEST_BEGIN("CheckNickname");

            var p = new PangyaBinaryWriter();
            var nickname = "";
            try
            {

                nickname = _packet.ReadString();

                // Verifica se Session est� varrizada para executar esse a��o, 
                // se ele n�o fez o login com o Server ele n�o pode fazer nada at� que ele fa�a o login
                //CHECK_SESSION_IS_AUTHORIZED("CheckNickname");

                if (nickname.empty())
                    throw new exception("[MessengerServer::requestCheckNickname][Error] player[UID=" + (_session.m_pi.uid) + "] tentou verificar o Nickname[value="
                            + nickname + "], mas o nickname is empty. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MESSAGE_SERVER, 1, 0x5200501));

                var cmd_vn = new CmdVerifyNick(nickname);    // Waiter

                snmdb.NormalManagerDB.getInstance().add(0, cmd_vn, null, null);

                if (cmd_vn.getException().getCodeError() != 0)
                    throw cmd_vn.getException();

                if (!cmd_vn.getLastCheck())
                    throw new exception("[MessengerServer::requestCheckNickname][Error] player[UID=" + (_session.m_pi.uid) + "] tentou verificar o Nickname[value="
                        + nickname + "], mas o nickname nao existe.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MESSAGE_SERVER, 2, 1));

                // Log
                _smp.message_pool.getInstance().push(new message("[CheckNickname][Log] player[UID=" + (_session.m_pi.uid) + "] pediu para verificar o Nickname[value=" + nickname + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));


                // Resposta para Check Nickname
                p.init_plain(0x30);

                p.WriteUInt16(0x117);   // Sub packet Id

                p.WriteUInt32(0);   // OK

                p.WriteString(nickname);
                p.WriteUInt32(cmd_vn.getUID());

                packet_func.session_send(p, _session, 1);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[MessengerServer::requestCheckNickname][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x30);

                p.WriteUInt16(0x117);   // Sub packet Id

                p.WriteUInt32((ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) == (uint)STDA_ERROR_TYPE.MESSAGE_SERVER) ? ExceptionError.STDA_SYSTEM_ERROR_DECODE(e.getCodeError()) : 0x5200500);

                p.WriteString(nickname);

                packet_func.session_send(p, _session, 1);
            }
        }
        public void requestAssignApelido(Player _session, packet _packet)
        {//REQUEST_BEGIN("AssingApelido");

            var p = new PangyaBinaryWriter();

            try
            {

                uint uid = _packet.ReadUInt32();
                var apelido = _packet.ReadString();

                // Verifica se Session est� varrizada para executar esse a��o, 
                // se ele n�o fez o login com o Server ele n�o pode fazer nada at� que ele fa�a o login
                //CHECK_SESSION_IS_AUTHORIZED("AssingApelido");

                if (uid == 0)
                    throw new exception("[MessengerServer::requestAssingApelido][Error] player[UID=" + (_session.m_pi.uid) + "] tentou da um apelido para o Amigo[UID="
                            + (uid) + ", APELIDO=" + apelido + "], mas o uid is invalid(zero). Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MESSAGE_SERVER, 1, 0x5200901));

                if (apelido.empty())
                    throw new exception("[MessengerServer::requestAssingApelido][Error] player[UID=" + (_session.m_pi.uid) + "] tentou da um apelido para o Amigo[UID="
                            + (uid) + ", APELIDO=" + apelido + "], mas o apelido is empty. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MESSAGE_SERVER, 2, 0x5200902));

                if (apelido.Count() >= 11)
                    throw new exception("[MessengerServer::requestAssingApelido][Error] player[UID=" + (_session.m_pi.uid) + "] tentou da um apelido para o Amigo[UID="
                            + (uid) + ", APELIDO=" + apelido + "], mas o comprimento do apelido[max=11, request=" + (apelido.Count()) + "] eh invalido.",
                            ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MESSAGE_SERVER, 3, 0x5200903));

                var pFi = _session.m_pi.m_friend_manager.findFriend(uid);

                if (pFi == null)
                    throw new exception("[MessengerServer::requestAssingApelido][Error] player[UID=" + (_session.m_pi.uid) + "] tentou da um apelido para o Amigo[UID="
                            + (uid) + ", APELIDO=" + apelido + "], mas ele nao tem esse player como amigo. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MESSAGE_SERVER, 4, 0x5200903));

                // UPDATE ON SERVER 
                pFi.apelido = apelido;

                // UPDATE ON DB
                _session.m_pi.m_friend_manager.requestUpdateFriendInfo(pFi);

                // Log
                _smp.message_pool.getInstance().push(new message("[AssingApelido][Log] player[UID=" + (_session.m_pi.uid) + "] colocou apelido[VALUE="
                        + apelido + "] no Amigo[UID=" + (pFi.uid) + ", NICKNAME=" + (pFi.nickname) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Resposta para assing apelido
                p.init_plain(0x30);

                p.WriteUInt16(0x119);   // Sub packet Id

                p.WriteUInt32(0);   // OK

                p.WriteUInt32(pFi.uid);
                p.WriteString(pFi.apelido);

                packet_func.session_send(p, _session, 1);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[MessengerServer::requestAssingApelido][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x30);

                p.WriteUInt16(0x119);   // Sub packet Id

                p.WriteUInt32((ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) == (uint)STDA_ERROR_TYPE.MESSAGE_SERVER) ? ExceptionError.STDA_SYSTEM_ERROR_DECODE(e.getCodeError()) : 0x5200900);

                packet_func.session_send(p, _session, 1);
            }
        }

        public void requestBlockFriend(Player _session, packet _packet)
        {//REQUEST_BEGIN("BlockFriend");

            var p = new PangyaBinaryWriter();

            try
            {

                uint uid = _packet.ReadUInt32();

                // Verifica se Session est� varrizada para executar esse a��o, 
                // se ele n�o fez o login com o Server ele n�o pode fazer nada at� que ele fa�a o login
                //CHECK_SESSION_IS_AUTHORIZED("BlockFriend");

                if (uid == 0)
                    throw new exception("[MessengerServer::requestBlockFriend][Error] player[UID=" + (_session.m_pi.uid) + "] tentou bloqueiar Amigo[UID="
                            + (uid) + "], mas o uid is invalid(zero). Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MESSAGE_SERVER, 1, 0x5300101));

                var pFi = _session.m_pi.m_friend_manager.findFriend(uid);

                if (pFi == null)
                    throw new exception("[MessengerServer::requestBlockFriend][Error] player[UID=" + (_session.m_pi.uid) + "] tentou bloqueiar Amigo[UID="
                        + (uid) + "], mas o player nao eh amigo dele. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MESSAGE_SERVER, 2, 0x5300102));

                if (pFi.state.block == 1)
                    throw new exception("[MessengerServer::requestBlockFriend][Error] player[UID=" + (_session.m_pi.uid) + "] tentou bloqueiar Amigo[UID="
                            + (uid) + "], mas o amigo ja esta bloqueado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MESSAGE_SERVER, 3, 0x5300103));

                var s = (Player)m_player_manager.findSessionByUID(uid);

                FriendInfoEx pFi2 = null;

                if (s != null)
                {   // Player est� online

                    if ((pFi2 = s.m_pi.m_friend_manager.findFriend(_session.m_pi.uid)) == null)
                        throw new exception("[MessengerServer::requestBlockFriend][Error] player[UID=" + (_session.m_pi.uid) + "] tentou bloqueiar Amigo[UID="
                                + (uid) + "], mas o amigo nao tem ele na lista de amigos. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MESSAGE_SERVER, 4, 0x5300104));

                    // Amigo
                    pFi.state.block = 1;

                    // UPDATE ON DB
                    _session.m_pi.m_friend_manager.requestUpdateFriendInfo(pFi);    // REQUEST

                    // Log
                    _smp.message_pool.getInstance().push(new message("[BlockFriend][Log] player[UID=" + (_session.m_pi.uid) + "] bloqueou o Amigo[UID="
                            + (s.m_pi.uid) + ", NICKNAME=" + (s.m_pi.nickname) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                    // Resposta para o block friend REQUEST
                    p.init_plain(0x30);

                    p.WriteUInt16(0x10C);   // Sub packet Id

                    p.WriteUInt32(0);   // OK

                    p.WriteUInt32(s.m_pi.uid);

                    packet_func.session_send(p, _session, 1);

                    // Resposta para o block friend REQUESTED, Envia que o player deslogou
                    p.init_plain(0x30);

                    p.WriteUInt16(0x10F);   // Sub packet Id

                    p.WriteUInt32(_session.m_pi.uid);

                    packet_func.session_send(p, s, 1);

                }
                else
                {

                    var cmd_pi = new CmdPlayerInfo(uid);    // Waiter

                    snmdb.NormalManagerDB.getInstance().add(0, cmd_pi, null, null);

                    if (cmd_pi.getException().getCodeError() != 0)
                        throw cmd_pi.getException();

                    var pi = cmd_pi.getInfo();

                    if (pi.uid == 0)
                        throw new exception("[MessengerServer::requestBlockFriend][Error] player[UID=" + (_session.m_pi.uid) + "] tentou bloqueiar Amigo[UID="
                                + (uid) + "], mas player nao existe. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MESSAGE_SERVER, 5, 0x5300105));

                    var fm = new FriendManager(pi);

                    fm.init(pi);

                    if (!fm.isInitialized())
                        throw new exception("[MessengerServer::requestBlockFriend][Error] player[UID=" + (_session.m_pi.uid) + "] tentou bloqueiar Amigo[UID="
                                + (uid) + "], nao conseguiu inicializar Friend Manager do amigo. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MESSAGE_SERVER, 6, 0x5300106));

                    if ((pFi2 = fm.findFriend(_session.m_pi.uid)) == null)
                        throw new exception("[MessengerServer::requestBlockFriend][Error] player[UID=" + (_session.m_pi.uid) + "] tentou bloqueiar Amigo[UID="
                                + (uid) + "], mas o amigo nao tem ele na lista de amigos. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MESSAGE_SERVER, 4, 0x5300104));

                    // Amigo
                    pFi.state.block = 1;

                    // UPDATE ON DB
                    _session.m_pi.m_friend_manager.requestUpdateFriendInfo(pFi);    // REQUEST

                    // Log
                    _smp.message_pool.getInstance().push(new message("[BlockFriend][Log] player[UID=" + (_session.m_pi.uid) + "] bloqueou o Amigo[UID="
                            + (pi.uid) + ", NICKNAME=" + (pi.nickname) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                    // Resposta para o block friend REQUEST
                    p.init_plain(0x30);

                    p.WriteUInt16(0x10C);   // Sub packet Id

                    p.WriteUInt32(0);   // OK

                    p.WriteUInt32(pi.uid);

                    packet_func.session_send(p, _session, 1);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[MessengerServer::requestBlockFriend][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x30);

                p.WriteUInt16(0x10C);   // Sub packet Id

                p.WriteUInt32((ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) == (uint)STDA_ERROR_TYPE.MESSAGE_SERVER) ? ExceptionError.STDA_SYSTEM_ERROR_DECODE(e.getCodeError()) : 0x5300100);

                packet_func.session_send(p, _session, 1);
            }
        }
        public void requestUnblockFriend(Player _session, packet _packet)
        {   //REQUEST_BEGIN("UnblockFriend");

            var p = new PangyaBinaryWriter();

            try
            {

                uint uid = _packet.ReadUInt32();

                // Verifica se Session est� varrizada para executar esse a��o, 
                // se ele n�o fez o login com o Server ele n�o pode fazer nada at� que ele fa�a o login
                //CHECK_SESSION_IS_AUTHORIZED("UnblockFriend");

                if (uid == 0)
                    throw new exception("[MessengerServer::requestUnBlockFriend][Error] player[UID=" + (_session.m_pi.uid) + "] tentou desbloquear Amigo[UID="
                            + (uid) + "], mas uid is invalid(zero). Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MESSAGE_SERVER, 1, 0x5300201));

                var pFi = _session.m_pi.m_friend_manager.findFriend(uid);

                if (pFi == null)
                    throw new exception("[MessengerServer::requestUnBlockFriend][Error] player[UID=" + (_session.m_pi.uid) + "] tentou desbloquear Amigo[UID="
                            + (uid) + "], mas o player nao eh amigo dele. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MESSAGE_SERVER, 2, 0x5300202));

                if (!(pFi.state.block == 1))
                    throw new exception("[MessengerServer::requestUnBlockFriend][Error] player[UID=" + (_session.m_pi.uid) + "] tentou desbloquear Amigo[UID="
                            + (uid) + "], mas o amigo ja esta desbloqueado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MESSAGE_SERVER, 3, 0x5300203));

                var s = (Player)m_player_manager.findSessionByUID(uid);

                FriendInfoEx pFi2 = null;

                if (s != null)
                {   // Player est� online

                    if ((pFi2 = s.m_pi.m_friend_manager.findFriend(_session.m_pi.uid)) == null)
                        throw new exception("[MessengerServer::requestUnBlockFriend][Error] player[UID=" + (_session.m_pi.uid) + "] tentou desbloquear Amigo[UID="
                                + (uid) + "], mas o amigo nao tem ele na lista de amigos. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MESSAGE_SERVER, 4, 0x5200204));

                    // Amigo
                    pFi.state.block = 0;

                    // UPDATE ON DB
                    _session.m_pi.m_friend_manager.requestUpdateFriendInfo(pFi);    // REQUEST

                    // Log
                    _smp.message_pool.getInstance().push(new message("[UnBlockFriend][Log] player[UID=" + (_session.m_pi.uid) + "] desbloqueou o Amigo[UID="
                            + (s.m_pi.uid) + ", NICKNAME=" + (s.m_pi.nickname) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                    // Resposta para o unblock friend REQUEST
                    p.init_plain(0x30);

                    p.WriteUInt16(0x10D);   // Sub packet Id

                    p.WriteUInt32(0);   // OK

                    p.WriteUInt32(s.m_pi.uid);

                    packet_func.session_send(p, _session, 1);

                    // Resposta para o unblock friend REQUESTED - Passa Pacote que ele esta online
                    p.init_plain(0x30);

                    p.WriteUInt16(0x115);   // Sub packet Id

                    p.WriteUInt32(_session.m_pi.uid);
                    p.WriteUInt32(_session.m_pi.m_state);

                    p.WriteByte(1); // OK

                    p.WriteBytes(_session.m_pi.m_cpi.ToArray());

                    packet_func.session_send(p, s, 1);

                }
                else
                {

                    var cmd_pi = new CmdPlayerInfo(uid);    // Waiter

                    snmdb.NormalManagerDB.getInstance().add(0, cmd_pi, null, null);

                    if (cmd_pi.getException().getCodeError() != 0)
                        throw cmd_pi.getException();

                    var pi = cmd_pi.getInfo();

                    if (pi.uid == 0)
                        throw new exception("[MessengerServer::requestUnBlockFriend][Error] player[UID=" + (_session.m_pi.uid) + "] tentou desbloquear Amigo[UID="
                                + (uid) + "], mas o player nao existe. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MESSAGE_SERVER, 5, 0x5300205));

                    var fm = new FriendManager(pi);

                    fm.init(pi);

                    if (!fm.isInitialized())
                        throw new exception("[MessengerServer::requestUnBlockFriend][Error] player[UID=" + (_session.m_pi.uid) + "] tentou desbloquear Amigo[UID="
                                + (uid) + "], mas nao conseguiu inicializar Friend Manager do amigo. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MESSAGE_SERVER, 6, 0x5300206));

                    if ((pFi2 = fm.findFriend(_session.m_pi.uid)) == null)
                        throw new exception("[MessengerServer::requestUnBlockFriend][Error] player[UID=" + (_session.m_pi.uid) + "] tentou desbloquear Amigo[UID="
                                + (uid) + "], mas o amigo nao tem ele na lista de amigos. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MESSAGE_SERVER, 4, 0x5300204));

                    // Amigo
                    pFi.state.block = 0;

                    // UPDATE ON DB
                    _session.m_pi.m_friend_manager.requestUpdateFriendInfo(pFi);    // REQUEST

                    // Log
                    _smp.message_pool.getInstance().push(new message("[UnBlockFriend][Log] player[UID=" + (_session.m_pi.uid) + "] desbloqueou o Amigo[UID="
                            + (pi.uid) + ", NICKNAME=" + (pi.nickname) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                    // Resposta para o unblock friend REQUEST
                    p.init_plain(0x30);

                    p.WriteUInt16(0x10D);   // Sub packet Id

                    p.WriteUInt32(0);   // OK

                    p.WriteUInt32(pi.uid);

                    packet_func.session_send(p, _session, 1);
                }
            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[MessengerServer::requestUnblockFriend][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x30);

                p.WriteUInt16(0x10D);   // Sub packet Id

                p.WriteUInt32((ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) == (uint)STDA_ERROR_TYPE.MESSAGE_SERVER) ? ExceptionError.STDA_SYSTEM_ERROR_DECODE(e.getCodeError()) : 0x5300200);

                packet_func.session_send(p, _session, 1);
            }
        }
        public void requestAddFriend(Player _session, packet _packet)
        {

            var p = new PangyaBinaryWriter();

            try
            {

                uint uid = _packet.ReadUInt32();
                var nickname = _packet.ReadString();

                if (uid == 0)
                    throw new exception("[MessengerServer::requestAddFriend][Error] player[UID=" + (_session.m_pi.uid) + "] tentou add Friend[UID="
                            + (uid) + ", NICKNAME=" + nickname + "], mas o uid is invalid(zero). Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MESSAGE_SERVER, 1, 0x5200601));

                if (nickname.empty())
                    throw new exception("[MessengerServer::requestAddFriend][Error] player[UID=" + (_session.m_pi.uid) + "] tentou add Friend[UID="
                            + (uid) + ", NICKNAME=" + nickname + "], mas o nickname is empty. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MESSAGE_SERVER, 2, 0x5200602));

                var pFi = _session.m_pi.m_friend_manager.findFriendInAllFriend(uid);

                if (pFi != null && pFi.flag._friend == 1)
                    throw new exception("[MessengerServer::requestAddFriend][Error] player[UID=" + (_session.m_pi.uid) + "] tentou add Friend[UID="
                        + (uid) + ", NICKNAME=" + nickname + "], mas o player ja eh amigo dele.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MESSAGE_SERVER, 3, 2));

                if (_session.m_pi.m_friend_manager.countFriend() >= FRIEND_LIST_LIMIT)
                    throw new exception("[MessengerServer::requestAddFriend][Error] player[UID=" + (_session.m_pi.uid) + "] tentou add Friend[UID="
                            + (uid) + ", NICKNAME=" + nickname + "], mas ele esta com a lista de amigos cheia[LIMIT=" + (FRIEND_LIST_LIMIT) + "].", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MESSAGE_SERVER, 4, 0x5200603));

                var s = (Player)m_player_manager.findSessionByUID(uid);

                FriendInfoEx fi = new FriendInfoEx(), fi2 = new FriendInfoEx();

                if (s != null)
                {   // Player est� connectado

                    if (string.Compare(nickname, s.m_pi.nickname) != 0)
                        throw new exception("[MessengerServer::requestAddFriend][Error] player[UID=" + (_session.m_pi.uid) + "] tentou add Friend[UID="
                                + (uid) + ", NICKNAME=" + nickname + "], mas o nickname nao bate. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MESSAGE, 7, 0x5200607));

                    if (s.m_pi.m_friend_manager.countFriend() >= FRIEND_LIST_LIMIT)
                        throw new exception("[MessengerServer::requestAddFriend][Error] player[UID=" + (_session.m_pi.uid) + "] tentou add Friend[UID="
                                + (uid) + ", NICKNAME=" + nickname + "], mas o amigo esta com a lista full[LIMIT=" + (FRIEND_LIST_LIMIT) + "]", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MESSAGE_SERVER, 5, 3));

                    // Friend to add
                    fi.uid = s.m_pi.uid;
                    fi.flag.ucFlag = (byte)((pFi == null) ? 1 : pFi.flag.ucFlag | 1);   // Friend

                    fi.apelido = "Friend";
                    fi.nickname = s.m_pi.nickname;
                    fi.state.online = 1;
                    fi.state.request_friend = 1;
                    fi.state.sex = s.m_pi.sex;

                    fi.level = (byte)s.m_pi.level;

                    // Friend that has add
                    fi2.uid = _session.m_pi.uid;
                    fi2.flag.ucFlag = (byte)((pFi == null) ? 1 : pFi.flag.ucFlag | 1);   // Friend
                    fi2.apelido = "Friend";
                    fi2.nickname = _session.m_pi.nickname;


                    fi2.state.online = 1;
                    fi2.state.sex = _session.m_pi.sex;

                    fi2.level = (byte)_session.m_pi.level;

                    // UPDATE ON SERVER AND DB
                    _session.m_pi.m_friend_manager.requestAddFriend(fi);    // Add On Player Request
                    s.m_pi.m_friend_manager.requestAddFriend(fi2);          // Add On Player Requested

                    // Log
                    _smp.message_pool.getInstance().push(new message("[AddFriend][Log] player[UID=" + (_session.m_pi.uid) + "] add Amigo[UID=" + (s.m_pi.uid) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                    // Resposta para o add Friend
                    p.init_plain(0x30);

                    p.WriteUInt16(0x104);   // Sub packet Id

                    p.WriteUInt32(0);   // OK

                    p.WriteBytes(fi.ToArray());

                    p.WriteBytes(s.m_pi.m_cpi.ToArray());

                    // State Icon Player
                    p.WriteByte(s.m_pi.m_state);

                    p.WriteByte(fi.cUnknown_flag);
                    p.WriteByte(fi.level);
                    p.WriteByte(fi.state.ucState);
                    p.WriteByte(fi.flag.ucFlag);

                    packet_func.session_send(p, _session, 1);

                    // Resposta para o player que foi adicionado
                    p.init_plain(0x30);

                    p.WriteUInt16(0x106);   // Sub packet Id

                    p.WriteBytes(fi2.ToArray());
                    p.WriteBytes(_session.m_pi.m_cpi.ToArray());

                    // State Icon Player
                    p.WriteByte(_session.m_pi.m_state);

                    p.WriteByte(fi2.cUnknown_flag);
                    p.WriteByte(fi2.level);
                    p.WriteByte(fi2.state.ucState);
                    p.WriteByte(fi2.flag.ucFlag);

                    packet_func.session_send(p, s, 1);

                }
                else
                {

                    var cmd_pi = new CmdPlayerInfo(uid);    // Waiter

                    snmdb.NormalManagerDB.getInstance().add(0, cmd_pi, null, null);

                    if (cmd_pi.getException().getCodeError() != 0)
                        throw cmd_pi.getException();

                    var pi = cmd_pi.getInfo();

                    if (pi.uid == 0)
                        throw new exception("[MessengerServer::requestAddFriend][Error] player[UID=" + (_session.m_pi.uid) + "] tentou add Friend[UID="
                                + (uid) + ", NICKNAME=" + nickname + "], mas o player nao existe.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MESSAGE_SERVER, 6, 0x5200606));

                    if (string.Compare(nickname, pi.nickname) != 0)
                        throw new exception("[MessengerServer::requestAddFriend][Error] player[UID=" + (_session.m_pi.uid) + "] tentou add Friend[UID="
                           + (uid) + ", NICKNAME=" + nickname + "], mas o nickname nao bate. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MESSAGE, 7, 0x5200607));

                    var fm = new FriendManager(pi);

                    fm.init(pi);

                    if (!fm.isInitialized())
                        throw new exception("[MessengerServer::requestAddFriend][Error] player[UID=" + (_session.m_pi.uid) + "] tentou add Friend[UID="
                                + (uid) + ", NICKNAME=" + nickname + "], mas nao conseguiu inicializar o FriendManager do Amigo.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MESSAGE_SERVER, 8, 0x5200607));

                    if (fm.countFriend() >= FRIEND_LIST_LIMIT)
                        throw new exception("[MessengerServer::requestAddFriend][Error] player[UID=" + (_session.m_pi.uid) + "] tentou add Friend[UID="
                            + (uid) + ", NICKNAME=" + nickname + "], mas o amigo esta com a lista full[LIMIT=" + (FRIEND_LIST_LIMIT) + "]", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MESSAGE_SERVER, 5, 3));

                    // Friend to add
                    fi.uid = pi.uid;
                    fi.flag.ucFlag = (byte)((pFi == null) ? 1 : pFi.flag.ucFlag | 1);   // Friend
                    fi.apelido = "Friend";
                    fi.nickname = pi.nickname;

                    fi.state.online = 1;
                    fi.state.request_friend = 1;
                    fi.state.sex = pi.sex;

                    fi.level = (byte)pi.level;

                    // Friend that has add
                    fi2.uid = _session.m_pi.uid;
                    fi2.flag.ucFlag = (byte)((pFi == null) ? 1 : pFi.flag.ucFlag | 1);   // Friend
                    fi2.apelido = "Friend";
                    fi2.nickname = _session.m_pi.nickname;

                    fi2.state.online = 1;
                    fi2.state.sex = _session.m_pi.sex;

                    fi2.level = (byte)_session.m_pi.level;

                    // UPDATE ON SERVER AND DB
                    _session.m_pi.m_friend_manager.requestAddFriend(fi);    // Add On Player Request
                    fm.requestAddFriend(fi2);                               // Add On Player Requested

                    // Log
                    _smp.message_pool.getInstance().push(new message("[MessengerServer::requestAddFriend][Log] player[UID=" + (_session.m_pi.uid) + "] add Amigo[UID=" + (pi.uid) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                    // Resposta para o add Friend
                    p.init_plain(0x30);

                    p.WriteUInt16(0x104);   // Sub packet Id

                    p.WriteUInt32(0);   // OK

                    p.WriteBytes(fi.ToArray());
                    p.WriteInt16(-1);       // Sala N�mero
                    p.WriteInt32(-1);       // Sala Tipo
                    p.WriteInt32(-1);       // Server GUID
                    p.WriteSByte(-1);        // Canal ID
                    p.WriteZero(64);    // Canal Nome

                    // State Icon Player
                    p.WriteByte(5); // OFFLINE

                    fi.state.online = 0;    // Offline

                    p.WriteByte(fi.cUnknown_flag);
                    p.WriteByte(fi.level);
                    p.WriteByte(fi.state.ucState);
                    p.WriteByte(fi.flag.ucFlag);

                    packet_func.session_send(p, _session, 1);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[MessengerServer::requestAddFriend][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x30);

                p.WriteUInt16(0x104);   // Sub packet Id

                p.WriteUInt32((ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) == (uint)STDA_ERROR_TYPE.MESSAGE_SERVER) ? ExceptionError.STDA_SYSTEM_ERROR_DECODE(e.getCodeError()) : 0x5200600);

                packet_func.session_send(p, _session, 1);
            }
        }

        public void requestConfirmFriend(Player _session, packet _packet)
        {//REQUEST_BEGIN("ConfirmFriend");

            var p = new PangyaBinaryWriter();

            try
            {

                uint uid = _packet.ReadUInt32();

                // Verifica se Session est� varrizada para executar esse a��o, 
                // se ele n�o fez o login com o Server ele n�o pode fazer nada at� que ele fa�a o login
                //CHECK_SESSION_IS_AUTHORIZED("ConfirmFriend");

                if (uid == 0)
                    throw new exception("[MessengerServer::requestConfirmFriend][Error] player[UID=" + (_session.m_pi.uid) + "] tentou aceitar Amigo[UID="
                            + (uid) + "], mas o uid is invalid(zero). Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MESSAGE_SERVER, 1, 0x5200801));

                var pFi = _session.m_pi.m_friend_manager.findFriend(uid);

                if (pFi == null)
                    throw new exception("[MessengerServer::requestConfirmFriend][Error] player[UID=" + (_session.m_pi.uid) + "] tentou aceitar Amigo[UID="
                            + (uid) + "], mas o player nao eh amigo dele. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MESSAGE_SERVER, 2, 0x5200802));

                if (pFi.state.request_friend.IsTrue())
                    throw new exception("[MessengerServer::requestConfirmFriend][Error] player[UID=" + (_session.m_pi.uid) + "] tentou aceitar Amigo[UID="
                            + (uid) + "], mas ele nao pode aceitar um amigo, que ele mesmo enviou pedido de amizade. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MESSAGE_SERVER, 3, 0x5200803));

                if (pFi.state._friend.IsTrue())
                    throw new exception("[MessengerServer::requestConfirmFriend][Error] player[UID=" + (_session.m_pi.uid) + "] tentou aceitar Amigo[UID="
                            + (uid) + "], mas o player ja eh seu amigo. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MESSAGE_SERVER, 4, 0x5200804));

                var s = (Player)m_player_manager.findSessionByUID(uid);

                FriendInfoEx pFi2 = null;

                if (s != null)
                {   // Player est� online

                    if ((pFi2 = s.m_pi.m_friend_manager.findFriend(_session.m_pi.uid)) == null)
                        throw new exception("[MessengerServer::requestConfirmFriend][Error] player[UID=" + (_session.m_pi.uid) + "] tentou aceitar Amigo[UID="
                                + (uid) + "], mas o player nao esta na lista do amigo que ele vai aceitar. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MESSAGE_SERVER, 5, 0x5200804));

                    // Amigo
                    pFi.state._friend = 1;

                    // Amigo
                    pFi2.state.request_friend = 0;
                    pFi2.state._friend = 1;

                    // UPDATE ON SERVER AND DB
                    _session.m_pi.m_friend_manager.requestUpdateFriendInfo(pFi);    // REQUEST
                    s.m_pi.m_friend_manager.requestUpdateFriendInfo(pFi2);      // REQUESTED

                    // Log
                    _smp.message_pool.getInstance().push(new message("[MessengerServer::requestConfirmFriend][Log] player[UID=" + (_session.m_pi.uid) + "] aceitou Amigo[UID="
                            + (s.m_pi.uid) + ", NICKNAME=" + (s.m_pi.nickname) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                    // Resposta para o confirm friend REQUEST
                    p.init_plain(0x30);

                    p.WriteUInt16(0x109);   // Sub packet Id

                    p.WriteUInt32(0);   // OK

                    p.WriteUInt32(s.m_pi.uid);

                    packet_func.session_send(p, _session, 1);

                    // Resposta para o confirm friend REQUESTED
                    p.init_plain(0x30);

                    p.WriteUInt16(0x10A);   // Sub packet Id

                    p.WriteUInt32(0);   // OK

                    p.WriteUInt32(_session.m_pi.uid);

                    packet_func.session_send(p, s, 1);

                }
                else
                {

                    var cmd_pi = new CmdPlayerInfo(uid);    // Waiter

                    snmdb.NormalManagerDB.getInstance().add(0, cmd_pi, null, null);



                    if (cmd_pi.getException().getCodeError() != 0)
                        throw cmd_pi.getException();

                    var pi = cmd_pi.getInfo();

                    if (pi.uid == 0)
                        throw new exception("[MessengerServer::requestConfirmFriend][Error] player[UID=" + (_session.m_pi.uid) + "] tentou aceitar Amigo[UID="
                                + (uid) + "], mas o player nao existe. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MESSAGE_SERVER, 6, 0x5200806));

                    var fm = new FriendManager(pi);

                    fm.init(pi);

                    if (!fm.isInitialized())
                        throw new exception("[MessengerServer::requestConfirmFriend][Error] player[UID=" + (_session.m_pi.uid) + "] tentou aceitar Amigo[UID="
                                + (uid) + "], mas nao conseguiu incializar o Friend Manager do amigo. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MESSAGE_SERVER, 7, 0x5200807));

                    if ((pFi2 = fm.findFriend(_session.m_pi.uid)) == null)
                        throw new exception("[MessengerServer::requestConfirmFriend][Error] player[UID=" + (_session.m_pi.uid) + "] tentou aceitar Amigo[UID="
                                + (uid) + "], mas o player nao esta na lista do amigo que ele vai aceitar. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MESSAGE_SERVER, 5, 0x5200805));

                    // Amigo
                    pFi.state._friend = 1;

                    // Amigo
                    pFi2.state.request_friend = 0;
                    pFi2.state._friend = 1;


                    // UPDATE ON SERVER AND DB
                    _session.m_pi.m_friend_manager.requestUpdateFriendInfo(pFi);    // REQUEST
                    fm.requestUpdateFriendInfo(pFi2);                               // REQUESTED

                    // Log
                    _smp.message_pool.getInstance().push(new message("[MessengerServer::requestConfirmFriend][Log] player[UID=" + (_session.m_pi.uid) + "] aceitou Amigo[UID="
                            + (pi.uid) + ", NICKNAME=" + (pi.nickname) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                    // Resposta para o confirm friend REQUEST
                    p.init_plain(0x30);

                    p.WriteUInt16(0x109);   // Sub packet Id

                    p.WriteUInt32(0);   // OK

                    p.WriteUInt32(pi.uid);

                    packet_func.session_send(p, _session, 1);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[MessengerServer::requestConfirmFriend][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x30);

                p.WriteUInt16(0x109);   // Sub packet Id

                p.WriteUInt32((ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) == (uint)STDA_ERROR_TYPE.MESSAGE_SERVER) ? ExceptionError.STDA_SYSTEM_ERROR_DECODE(e.getCodeError()) : 0x5200800);

                packet_func.session_send(p, _session, 1);
            }
        }


        public void requestDeleteFriend(Player _session, packet _packet)
        { //REQUEST_BEGIN("DeleteFriend");

            var p = new PangyaBinaryWriter();

            try
            {

                uint uid = _packet.ReadUInt32();
                var nickname = _packet.ReadString();

                // Verifica se Session est� varrizada para executar esse a��o, 
                // se ele n�o fez o login com o Server ele n�o pode fazer nada at� que ele fa�a o login
                //CHECK_SESSION_IS_AUTHORIZED("DeleteFriend");

                if (uid == 0)
                    throw new exception("[MessengerServer::requestDeleteFriend][Error] player[UID=" + (_session.m_pi.uid) + "] tentou deletar Amigo[UID="
                            + (uid) + ", NICKNAME=" + nickname + "], mas o uid is invalid(zero). Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MESSAGE_SERVER, 1, 0x5200701));

                if (nickname.empty())
                    throw new exception("[MessengerServer::requestDeleteFriend][Error] player[UID=" + (_session.m_pi.uid) + "] tentou deletar Amigo[UID="
                            + (uid) + ", NICKNAME=" + nickname + "], mas nickname is empty. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MESSAGE_SERVER, 2, 0x5200702));

                var pFi = _session.m_pi.m_friend_manager.findFriend(uid);

                if (pFi == null)
                    throw new exception("[MessengerServer::requestDeleteFriend][Error] player[UID=" + (_session.m_pi.uid) + "] tentou deletar Amigo[UID="
                            + (uid) + ", NICKNAME=" + nickname + "], mas o player nao eh amigo dele. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MESSAGE_SERVER, 3, 0x5200703));

                var s = (Player)m_player_manager.findSessionByUID(uid);

                FriendInfoEx pFi2 = null;

                if (s != null)
                {   // Player est� online

                    if (string.Compare(nickname, s.m_pi.nickname) != 0)
                        throw new exception("[MessengerServer::requestDeleteFriend][Error] player[UID=" + (_session.m_pi.uid) + "] tentou deletar Amigo[UID="
                                + (uid) + ", NICKNAME=" + nickname + "], mas o nickname nao bate. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MESSAGE, 6, 0x5200705));

                    if ((pFi2 = s.m_pi.m_friend_manager.findFriend(_session.m_pi.uid)) == null)
                        throw new exception("[MessengerServer::requestDeleteFriend][Error] player[UID=" + (_session.m_pi.uid) + "] tentou deletar Amigo[UID="
                                + (uid) + ", NICKNAME=" + nickname + "], mas o amigo nao tem ele na lista de amigos. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MESSAGE, 4, 0x5200704));

                    // UPDATE ON SERVER ON DB
                    _session.m_pi.m_friend_manager.requestDeleteFriend(pFi);    // REQUEST
                    s.m_pi.m_friend_manager.requestDeleteFriend(pFi2);      // REQUESTED

                    // Log
                    _smp.message_pool.getInstance().push(new message("[MessengerServer::requestDeleteFriend][Log] player[UID=" + (_session.m_pi.uid) + "] deletou Amigo[UID="
                            + (s.m_pi.uid) + ", NICKNAME=" + nickname + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                    // Respsta para o delete friend TO REQUEST
                    p.init_plain(0x30);

                    p.WriteUInt16(0x10B);   // Sub packet Id

                    p.WriteUInt32(0);   // OK

                    p.WriteUInt32(s.m_pi.uid);

                    packet_func.session_send(p, _session, 1);

                    // Resposta para o delete friend TO REQUESTED
                    p.init_plain(0x30);

                    p.WriteUInt16(0x10B);   // Sub packet Id

                    p.WriteUInt32(0);   // OK

                    p.WriteUInt32(_session.m_pi.uid);

                    packet_func.session_send(p, s, 1);

                }
                else
                {

                    var cmd_pi = new CmdPlayerInfo(uid);    // Waiter

                    snmdb.NormalManagerDB.getInstance().add(0, cmd_pi, null, null);



                    if (cmd_pi.getException().getCodeError() != 0)
                        throw cmd_pi.getException();

                    var pi = cmd_pi.getInfo();

                    if (pi.uid == 0)
                        throw new exception("[MessengerServer::requestDeleteFriend][Error] player[UID=" + (_session.m_pi.uid) + "] tentou deletar Amigo[UID="
                                + (uid) + ", NICKNAME=" + nickname + "], mas o player nao existe. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MESSAGE_SERVER, 5, 0x5200705));

                    if (string.Compare(nickname, pi.nickname) != 0)
                        throw new exception("[MessengerServer::requestDeleteFriend][Error] player[UID=" + (_session.m_pi.uid) + "] tentou deletar Amigo[UID="
                                + (uid) + ", NICKNAME=" + nickname + "], mas o nickname nao bate. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MESSAGE_SERVER, 6, 0x5200706));

                    var fm = new FriendManager(pi);

                    fm.init(pi);

                    if (!fm.isInitialized())
                        throw new exception("[MessengerServer::requestDeleteFriend][Error] player[UID=" + (_session.m_pi.uid) + "] tentou deletar Amigo[UID="
                                + (uid) + ", NICKNAME=" + nickname + "], mas nao conseguiu incializar o Friend Manager do Amigo. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MESSAGE_SERVER, 7, 0x5200707));

                    if ((pFi2 = fm.findFriend(_session.m_pi.uid)) == null)
                        throw new exception("[MessengerServer::requestDeleteFriend][Error] player[UID=" + (_session.m_pi.uid) + "] tentou deletar Amigo[UID="
                                + (uid) + ", NICKNAME=" + nickname + "], mas o amigo nao tem ele na lista de amigos. Hacker ou Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MESSAGE, 8, 0x5200708));

                    // UPDATE ON SERVER ON DB
                    _session.m_pi.m_friend_manager.requestDeleteFriend(pFi);    // REQUEST
                    fm.requestDeleteFriend(pFi2);                               // REQUESTED

                    // Log
                    _smp.message_pool.getInstance().push(new message("[MessengerServer::requestDeleteFriend][Log] player[UID=" + (_session.m_pi.uid) + "] deletou Amigo[UID="
                            + (pi.uid) + ", NICKNAME=" + nickname + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                    // Respsta para o delete friend TO REQUEST
                    p.init_plain(0x30);

                    p.WriteUInt16(0x10B);   // Sub packet Id

                    p.WriteUInt32(0);   // OK

                    p.WriteUInt32(pi.uid);

                    packet_func.session_send(p, _session, 1);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[MessengerServer::requestDeleteFriend][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x30);

                p.WriteUInt16(0x10B);   // Sub packet Id

                p.WriteUInt32((ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) == (uint)STDA_ERROR_TYPE.MESSAGE_SERVER) ? ExceptionError.STDA_SYSTEM_ERROR_DECODE(e.getCodeError()) : 0x5200700);

                packet_func.session_send(p, _session, 1);
            }
        }

        public void requestNotifyPlayerWasInvitedToRoom(Player _session, packet _packet)
        {
            //tenho que pegar do superSS GB
            try
            {
                uint player_invited_uid = _packet.ReadUInt32();

                if (player_invited_uid != _session.m_pi.uid)
                    throw new exception("[MessengerServer::requestNotityPlayerWasInvitedToRoom][Error] Player[UID=" + (_session.m_pi.uid)
                            + "] que foi convidado passou um Player[UID=" + (player_invited_uid)
                            + "] com uid que nao eh o dele. Hacker ou Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MESSAGE_SERVER, 3749, 0));

                // Log
                _smp.message_pool.getInstance().push(new message("[MessengerServer::requestNotityPlayerWasInvitedToRoom][Log] Player[UID=" + (_session.m_pi.uid)
                        + "] foi convidado para um sala no jogo.", type_msg.CL_FILE_LOG_AND_CONSOLE));

            }
            catch (exception e)
            {
                _smp.message_pool.getInstance().push(new message("[MessengerServer::requestNotifyPlayerWasInvitedToRoom][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestInvitePlayerToGuildBattleRoom(Player _session, packet _packet)
        {
            //tenho que pegr do superss gb
            try
            {

                uint server_uid = _packet.ReadUInt32();
                byte channel_id = _packet.ReadUInt8();
                ushort room_numero = _packet.ReadUInt16();

                uint player_invite_uid = _packet.ReadUInt32();
                var player_invite_nickname = _packet.ReadString();

                uint player_invited_uid = _packet.ReadUInt32();

                if (player_invite_uid != _session.m_pi.uid)
                    throw new exception("[MessengerServer::requestInvitPlayerToGuildBattleRoom][Error] Player[UID=" + (_session.m_pi.uid)
                            + "] nao bate com o Player[UID=" + (player_invite_uid) + "] que fez o request para convidar o player[UID="
                            + (player_invited_uid) + "] para a sala de Guild Battle. Hacker ou Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MESSAGE_SERVER, 3750, 0));

                // Log
                _smp.message_pool.getInstance().push(new message("[MessengerServer::requestInvitPlayerToGuildBattleRoom][Log] Player[UID="
                        + (_session.m_pi.uid) + ", NICKNAME=" + player_invite_nickname + "] convidou o Player[UID="
                        + (player_invited_uid) + "] no Server[UID=" + (server_uid) + ", CHANNEL_ID="
                        + (channel_id) + ", ROOM=" + (room_numero) + "] para Guild Battle.", type_msg.CL_FILE_LOG_AND_CONSOLE));

            }
            catch (exception e)
            {
                _smp.message_pool.getInstance().push(new message("[MessengerServer::requestInvitPlayerToGuildBattleRoom][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestAcceptGuildMember(packet _packet)
        {
            PangyaBinaryWriter p = new PangyaBinaryWriter();

            try
            {

                uint club_id = _packet.ReadUInt32();
                uint member_uid = _packet.ReadUInt32();

                if (club_id == 0u)
                    throw new exception("[MessengerServer::requestAcceptGuildMember][Error] club_id is invalid(zero).", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MESSAGE_SERVER, 5401, 0));

                if (member_uid == 0u)
                    throw new exception("[MessengerServer::requestAcceptGuildMember][Error] member_uid is invalid(zero).", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MESSAGE_SERVER, 5401, 0));

                // Find all Club Members
                var v_cm = m_player_manager.findAllGuildMember(club_id);

                if (v_cm.empty())
                    _smp.message_pool.getInstance().push(new message("[MessengerServer::requestAcceptGuildMember][WARNING] Club[ID=" + (club_id)
                            + "] nao tem nenhum membro online para atualizar.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Update All Friend/Guild Member from All Club Members
                foreach (var el in v_cm)
                    if (el.Value != null)
                        el.Value.m_pi.m_friend_manager.init(el.Value.m_pi);

                // Verifica se o player est� online
                var s = m_player_manager.findPlayer(member_uid);

                // Player n�o est� online
                if (s == null || s.m_client == null)
                {

                    var cmd_pi = new CmdPlayerInfo(member_uid); // Waiter

                    NormalManagerDB.getInstance().add(0, cmd_pi, null, null);

                    if (cmd_pi.getException().getCodeError() != 0)
                        throw cmd_pi.getException();

                    var pi = cmd_pi.getInfo();

                    // Envia para cada player sua lista de amigos
                    {
                        foreach (var el in v_cm)
                        {

                            if (el.Value != null)
                            {

                                var friend_list = el.Value.m_pi.m_friend_manager.getAllFriendAndGuildMember();

                                var mp = new ManyPacket((ushort)friend_list.Count, FRIEND_PAG_LIMIT);

                                FriendInfoEx pFi = null;

                                // Resposta para Lista de Amigos e Membros da Guild
                                if (mp.paginas > 0)
                                {

                                    for (var i = 0; i < mp.paginas; i++, mp.increse())
                                    {
                                        p.init_plain(0x30);

                                        p.WriteUInt16(0x102);   // Sub Packet Id

                                        p.WriteBytes(mp.pag.ToArray());

                                        var _begin = friend_list.Skip(mp.index.start)/*  // Pula até o índice de início*/.Take(mp.index.end - mp.index.start); // Pega apenas os elementos entre start e end

                                        foreach (var begin in _begin)
                                        {
                                            p.WriteBytes(begin.ToArray());

                                            s = (Player)m_player_manager.findSessionByUID((begin).uid);

                                            // Se o Player tem ele na lista de amigos, e ele n�o estiver bloqueado na lista do amigo
                                            if (s != null && (pFi = s.m_pi.m_friend_manager.findFriendInAllFriend(el.Value.m_pi.uid)) != null && !pFi.state.block.IsTrue())
                                            {   // Player est� online

                                                p.WriteBytes(s.m_pi.m_cpi.ToArray());

                                                // State Icon Player
                                                p.WriteByte(s.m_pi.m_state);

                                                switch (s.m_pi.m_state)
                                                {
                                                    case 0: // IN GAME
                                                        (begin).state.play = 1;
                                                        break;
                                                    case 1: // AFK
                                                        (begin).state.AFK = 1;
                                                        break;
                                                    case 3: // BUSY
                                                        (begin).state.busy = 1;
                                                        break;
                                                    case 4: // ON
                                                    default:
                                                        (begin).state.online = 1;
                                                        break;
                                                }

                                                // Online
                                                (begin).state.online = 1;

                                            }
                                            else
                                            {   // player n�o est� online
                                                p.WriteInt16(-1);       // Sala Numero
                                                p.WriteInt32(-1);       // Sala Tipo
                                                p.WriteInt32(-1);       // Server GUID
                                                p.WriteByte(-1);        // Canal ID
                                                p.WriteZeroByte(64);    // Canal Nome

                                                // State Icon Player, OFFLINE not change icon
                                                p.WriteByte(5); // OFFLINE

                                                // Offline
                                                (begin).state.online = 0;
                                            }

                                            p.WriteByte((begin).cUnknown_flag);

                                            // Aqui quando � o player e ele est� guild � 1/Master/, 2 sub, e outros membro guild � 0, e quando � friend � o level
                                            p.WriteByte((begin).flag.ucFlag == 2 ? ((begin).uid == el.Value.m_pi.uid ? 1 : 0) : (begin).level);

                                            p.WriteByte((begin).state.ucState);
                                            p.WriteByte((begin).flag.ucFlag);
                                        }

                                        packet_func.session_send(p, el.Value, 1);
                                    }

                                }
                                else
                                {

                                    // N�o tem nenhum amigo, manda a p�gina vazia
                                    p.init_plain(0x30);

                                    p.WriteUInt16(0x102);   // Sub Packet Id

                                    p.WriteBytes(mp.pag.ToArray());

                                    packet_func.session_send(p, el.Value, 1);
                                }
                            }
                        }
                    }

                    // Notifica os player d� Guild que o player foi aceito na Guild
                    p.init_plain(0x3B);

                    p.WriteUInt32(pi.uid);
                    p.WriteUInt32(club_id);
                    p.WriteByte(pi.sex);
                    p.WriteString(pi.id);
                    p.WriteString(pi.nickname);
                    p.WriteUInt16(0x1F);                // No Server Antigo estava 0x1F e 0x125 nas op��es que peguei nos pacotes do pangya USA

                    packet_func.friend_broadcast(v_cm, p, s, 1);

                }
                else
                {   // Player est� online	

                    // Add o player a Guild
                    s.m_pi.guild_uid = club_id;

                    // Update All Friend/Guild Member
                    s.m_pi.m_friend_manager.init(s.m_pi);

                    // Envia para cada player sua lista de amigos
                    {
                        foreach (var el in v_cm)
                        {

                            if (el.Value != null)
                            {

                                var friend_list = el.Value.m_pi.m_friend_manager.getAllFriendAndGuildMember();

                                var mp = new ManyPacket((ushort)friend_list.Count, FRIEND_PAG_LIMIT);

                                FriendInfoEx pFi = null;

                                // Resposta para Lista de Amigos e Membros da Guild
                                if (mp.paginas > 0)
                                {

                                    for (var i = 0; i < mp.paginas; i++, mp.increse())
                                    {
                                        p.init_plain(0x30);

                                        p.WriteUInt16(0x102);   // Sub Packet Id

                                        p.WriteBytes(mp.pag.ToArray());

                                        var _begin = friend_list.Skip(mp.index.start)/*  // Pula até o índice de início*/.Take(mp.index.end - mp.index.start); // Pega apenas os elementos entre start e end

                                        foreach (var begin in _begin)
                                        {
                                            p.WriteBytes(begin.ToArray());

                                            s = (Player)m_player_manager.findSessionByUID((begin).uid);

                                            // Se o Player tem ele na lista de amigos, e ele n�o estiver bloqueado na lista do amigo
                                            if (s != null && (pFi = s.m_pi.m_friend_manager.findFriendInAllFriend(el.Value.m_pi.uid)) != null && !pFi.state.block.IsTrue())
                                            {   // Player est� online

                                                p.WriteBytes(s.m_pi.m_cpi.ToArray());

                                                // State Icon Player
                                                p.WriteByte(s.m_pi.m_state);

                                                switch (s.m_pi.m_state)
                                                {
                                                    case 0: // IN GAME
                                                        (begin).state.play = 1;
                                                        break;
                                                    case 1: // AFK
                                                        (begin).state.AFK = 1;
                                                        break;
                                                    case 3: // BUSY
                                                        (begin).state.busy = 1;
                                                        break;
                                                    case 4: // ON
                                                    default:
                                                        (begin).state.online = 1;
                                                        break;
                                                }

                                                // Online
                                                (begin).state.online = 1;

                                            }
                                            else
                                            {   // player n�o est� online
                                                p.WriteInt16(-1);       // Sala Numero
                                                p.WriteInt32(-1);       // Sala Tipo
                                                p.WriteInt32(-1);       // Server GUID
                                                p.WriteByte(-1);        // Canal ID
                                                p.WriteZeroByte(64);    // Canal Nome

                                                // State Icon Player, OFFLINE not change icon
                                                p.WriteByte(5); // OFFLINE

                                                // Offline
                                                (begin).state.online = 0;
                                            }

                                            p.WriteByte((begin).cUnknown_flag);

                                            // Aqui quando � o player e ele est� guild � 1/Master/, 2 sub, e outros membro guild � 0, e quando � friend � o level
                                            p.WriteByte((begin).flag.ucFlag == 2 ? ((begin).uid == el.Value.m_pi.uid ? 1 : 0) : (begin).level);

                                            p.WriteByte((begin).state.ucState);
                                            p.WriteByte((begin).flag.ucFlag);
                                        }

                                        packet_func.session_send(p, el.Value, 1);
                                    }

                                }
                                else
                                {

                                    // N�o tem nenhum amigo, manda a p�gina vazia
                                    p.init_plain(0x30);

                                    p.WriteUInt16(0x102);   // Sub Packet Id

                                    p.WriteBytes(mp.pag.ToArray());

                                    packet_func.session_send(p, el.Value, 1);
                                }
                            }
                        }
                    }

                    // Notifica os player d� Guild que o player foi aceito na Guild
                    p.init_plain(0x3B);

                    p.WriteUInt32(s.m_pi.uid);
                    p.WriteUInt32(club_id);
                    p.WriteByte(s.m_pi.sex);
                    p.WriteString(s.m_pi.id);
                    p.WriteString(s.m_pi.nickname);
                    p.WriteUInt16(0x1F);                // No Server Antigo estava 0x1F e 0x125 nas op��es que peguei nos pacotes do pangya USA

                    packet_func.friend_broadcast(v_cm, p, s, 1);
                }

                // Log
                _smp.message_pool.getInstance().push(new message("[MessengerServer::requestAcceptGuildMember][Log] Player[UID=" + (member_uid)
                        + "] foi aceito no Club[UID=" + (club_id) + "] com sucesso.", type_msg.CL_FILE_LOG_AND_CONSOLE));

            }
            catch (exception e)
            {
                _smp.message_pool.getInstance().push(new message("[MessengerServer::requestAcceptGuildMember][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestMemberExitedFromGuild(packet _packet)
        {
            PangyaBinaryWriter p = new PangyaBinaryWriter();

            try
            {

                var club_id = _packet.ReadUInt32();
                var member_uid = _packet.ReadUInt32();

                if (club_id == 0u)
                    throw new exception("[MessengerServer::requestMemberExitedFromGuild][Error] club_id is invalid(zero).", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MESSAGE_SERVER, 5401, 0));

                if (member_uid == 0u)
                    throw new exception("[MessengerServer::requestMemberExitedFromGuild][Error] member_uid is invalid(zero).", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MESSAGE_SERVER, 5401, 0));

                // Find all Club Members
                var v_cm = m_player_manager.findAllGuildMember(club_id);

                if (v_cm.empty())
                    _smp.message_pool.getInstance().push(new message("[MessengerServer::requestMemberExitedFromGuild][WARNING] Club[ID=" + (club_id)
                            + "] nao tem nenhum membro online para atualizar.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Update All Friend/Guild Member from All Club Members
                foreach (var el in v_cm)
                    if (el.Value != null)
                        el.Value.m_pi.m_friend_manager.init(el.Value.m_pi);

                // Verifica se o player est� online
                var s = m_player_manager.findPlayer(member_uid);

                // Player n�o est� online
                if (s == null || s.m_client == null)
                {

                    var cmd_pi = new CmdPlayerInfo(member_uid);	// Waiter

                    NormalManagerDB.getInstance().add(0, cmd_pi, null, null);

                    if (cmd_pi.getException().getCodeError() != 0)
                        throw cmd_pi.getException();

                    var pi = cmd_pi.getInfo();

                    // Envia para cada player sua lista de amigos
                    {
                        foreach (var el in v_cm)
                        {

                            if (el.Value != null)
                            {

                                var friend_list = el.Value.m_pi.m_friend_manager.getAllFriendAndGuildMember();

                                var mp = new ManyPacket((ushort)friend_list.Count, FRIEND_PAG_LIMIT);

                                FriendInfoEx pFi = null;

                                // Resposta para Lista de Amigos e Membros da Guild
                                if (mp.paginas > 0)
                                {

                                    for (var i = 0; i < mp.paginas; i++, mp.increse())
                                    {
                                        p.init_plain((ushort)0x30);

                                        p.WriteUInt16(0x102);   // Sub Packet Id

                                        p.WriteBytes(mp.pag.ToArray());

                                        var _begin = friend_list.Skip(mp.index.start)/*  // Pula até o índice de início*/.Take(mp.index.end - mp.index.start); // Pega apenas os elementos entre start e end

                                        foreach (var begin in _begin)
                                        {
                                            p.WriteBytes(begin.ToArray());
                                            s = (Player)m_player_manager.findSessionByUID((begin).uid);

                                            // Se o Player tem ele na lista de amigos, e ele n�o estiver bloqueado na lista do amigo
                                            if (s != null && (pFi = s.m_pi.m_friend_manager.findFriendInAllFriend(el.Value.m_pi.uid)) != null && !pFi.state.block.IsTrue())
                                            {   // Player est� online

                                                p.WriteBytes(s.m_pi.m_cpi.ToArray()); //, sizeof(ChannelPlayerInfo));

                                                // State Icon Player
                                                p.WriteByte(s.m_pi.m_state);

                                                switch (s.m_pi.m_state)
                                                {
                                                    case 0: // IN GAME
                                                        (begin).state.play = 1;
                                                        break;
                                                    case 1: // AFK
                                                        (begin).state.AFK = 1;
                                                        break;
                                                    case 3: // BUSY
                                                        (begin).state.busy = 1;
                                                        break;
                                                    case 4: // ON
                                                    default:
                                                        (begin).state.online = 1;
                                                        break;
                                                }

                                                // Online
                                                (begin).state.online = 1;

                                            }
                                            else
                                            {   // player n�o est� online
                                                p.WriteInt16(-1);     // Sala Numero
                                                p.WriteInt32(-1);     // Sala Tipo
                                                p.WriteInt32(-1);     // Server GUID
                                                p.WriteByte(-1);        // Canal ID
                                                p.WriteZeroByte(64);  // Canal Nome

                                                // State Icon Player, OFFLINE not change icon
                                                p.WriteByte(5); // OFFLINE

                                                // Offline
                                                (begin).state.online = 0;
                                            }

                                            p.WriteByte((begin).cUnknown_flag);

                                            // Aqui quando � o player e ele est� guild � 1/*Master*/, 2 sub, e outros membro guild � 0, e quando � friend � o level
                                            p.WriteByte((begin).flag.ucFlag == 2/*S� Guild Member*/ ? ((begin).uid == el.Value.m_pi.uid ? 1/*Master*/ : 0) : (begin).level);

                                            p.WriteByte((begin).state.ucState);
                                            p.WriteByte((begin).flag.ucFlag);
                                        }

                                        packet_func.session_send(p, el.Value, 1);
                                    }

                                }
                                else
                                {

                                    // N�o tem nenhum amigo, manda a p�gina vazia
                                    p.init_plain((ushort)0x30);

                                    p.WriteUInt16(0x102);   // Sub Packet Id

                                    p.WriteBytes(mp.pag.ToArray());

                                    packet_func.session_send(p, el.Value, 1);
                                }
                            }
                        }
                    }

                    // Notifica os player d� Guild que o player saiu na Guild
                    p.init_plain((ushort)0x3C);

                    p.WriteUInt32(pi.uid);

                    packet_func.friend_broadcast(v_cm, p, s, 1);

                }
                else
                {
                    // Player est� online	

                    // player saiu da Guild
                    s.m_pi.guild_uid = 0;

                    // Update All Friend/Guild Member
                    s.m_pi.m_friend_manager.init(s.m_pi);

                    // Envia para cada player sua lista de amigos
                    {
                        foreach (var el in v_cm)
                        {

                            if (el.Value != null)
                            {

                                var friend_list = el.Value.m_pi.m_friend_manager.getAllFriendAndGuildMember();

                                var mp = new ManyPacket((ushort)friend_list.Count, FRIEND_PAG_LIMIT);

                                FriendInfoEx pFi = null;

                                // Resposta para Lista de Amigos e Membros da Guild
                                if (mp.paginas > 0)
                                {

                                    for (var i = 0; i < mp.paginas; i++, mp.increse())
                                    {
                                        p.init_plain(0x30);

                                        p.WriteUInt16(0x102);   // Sub Packet Id

                                        p.WriteBytes(mp.pag.ToArray());

                                        var _begin = friend_list.Skip(mp.index.start)/*  // Pula até o índice de início*/.Take(mp.index.end - mp.index.start); // Pega apenas os elementos entre start e end

                                        foreach (var begin in _begin)
                                        {
                                            p.WriteBytes(begin.ToArray());


                                            s = (Player)m_player_manager.findSessionByUID((begin).uid);

                                            // Se o Player tem ele na lista de amigos, e ele n�o estiver bloqueado na lista do amigo
                                            if (s != null && (pFi = s.m_pi.m_friend_manager.findFriendInAllFriend(el.Value.m_pi.uid)) != null && !pFi.state.block.IsTrue())
                                            {   // Player est� online

                                                p.WriteBytes(s.m_pi.m_cpi.ToArray()); //, sizeof(ChannelPlayerInfo));

                                                // State Icon Player
                                                p.WriteByte(s.m_pi.m_state);

                                                switch (s.m_pi.m_state)
                                                {
                                                    case 0: // IN GAME
                                                        (begin).state.play = 1;
                                                        break;
                                                    case 1: // AFK
                                                        (begin).state.AFK = 1;
                                                        break;
                                                    case 3: // BUSY
                                                        (begin).state.busy = 1;
                                                        break;
                                                    case 4: // ON
                                                    default:
                                                        (begin).state.online = 1;
                                                        break;
                                                }

                                                // Online
                                                (begin).state.online = 1;

                                            }
                                            else
                                            {   // player n�o est� online
                                                p.WriteInt16(-1);       // Sala Numero
                                                p.WriteInt32(-1);       // Sala Tipo
                                                p.WriteInt32(-1);       // Server GUID
                                                p.WriteByte(-1);        // Canal ID
                                                p.WriteZeroByte(64);    // Canal Nome

                                                // State Icon Player, OFFLINE not change icon
                                                p.WriteByte(5); // OFFLINE

                                                // Offline
                                                (begin).state.online = 0;
                                            }

                                            p.WriteByte((begin).cUnknown_flag);

                                            // Aqui quando � o player e ele est� guild � 1/*Master*/, 2 sub, e outros membro guild � 0, e quando � friend � o level

                                            p.WriteByte((begin).flag.ucFlag == 2/*S� Guild Member*/ ? ((begin).uid == el.Value.m_pi.uid ? 1/*Master*/ : 0) : (begin).level);

                                            p.WriteByte((begin).state.ucState);
                                            p.WriteByte((begin).flag.ucFlag);
                                        }

                                        packet_func.session_send(p, el.Value, 1);
                                    }


                                }
                                else
                                {

                                    // N�o tem nenhum amigo, manda a p�gina vazia
                                    p.init_plain((ushort)0x30);

                                    p.WriteUInt16(0x102);   // Sub Packet Id

                                    p.WriteBytes(mp.pag.ToArray());

                                    packet_func.session_send(p, el.Value, 1);
                                }
                            }
                        }
                    }

                    // Notifica os player d� Guild que o player saiu na Guild
                    p.init_plain((ushort)0x3C);

                    p.WriteUInt32(s.m_pi.uid);

                    packet_func.friend_broadcast(v_cm, p, s, 1);
                }

                // Log
                _smp.message_pool.getInstance().push(new message("[MessengerServer::requestMemberExitedFromGuild][Log] Player[UID=" + (member_uid)
                        + "] saiu do Club[UID=" + (club_id) + "] com sucesso.", type_msg.CL_FILE_LOG_AND_CONSOLE));

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[MessengerServer::requestMemberExitedFromGuild][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestKickGuildMember(packet _packet)
        {
            var p = new PangyaBinaryWriter();

            try
            {

                var club_id = _packet.ReadUInt32();
                var member_uid = _packet.ReadUInt32();

                if (club_id == 0u)
                    throw new exception("[MessengerServer::requestKickGuildMember][Error] club_id is invalid(zero).", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MESSAGE_SERVER, 5401, 0));

                if (member_uid == 0u)
                    throw new exception("[MessengerServer::requestKickGuildMember][Error] member_uid is invalid(zero).", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MESSAGE_SERVER, 5401, 0));

                // Find all Club Members
                var v_cm = m_player_manager.findAllGuildMember(club_id);

                if (v_cm.empty())
                    _smp.message_pool.getInstance().push(new message("[MessengerServer::requestKickGuildMember][WARNING] Club[ID=" + (club_id)
                            + "] nao tem nenhum membro online para atualizar.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Update All Friend/Guild Member from All Club Members
                foreach (var el in v_cm)
                    if (el.Value != null)
                        el.Value.m_pi.m_friend_manager.init(el.Value.m_pi);

                // Verifica se o player est� online
                var s = m_player_manager.findPlayer(member_uid);

                // Player n�o est� online
                if (s == null || s.m_client == null)
                {

                    var cmd_pi = new CmdPlayerInfo(member_uid); // Waiter

                    NormalManagerDB.getInstance().add(0, cmd_pi, null, null);

                    if (cmd_pi.getException().getCodeError() != 0)
                        throw cmd_pi.getException();

                    var pi = cmd_pi.getInfo();


                    // Envia para cada player sua lista de amigos
                    {
                        foreach (var el in v_cm)
                        {

                            if (el.Value != null)
                            {

                                var friend_list = el.Value.m_pi.m_friend_manager.getAllFriendAndGuildMember();

                                var mp = new ManyPacket((ushort)friend_list.Count, FRIEND_PAG_LIMIT);

                                FriendInfoEx pFi = null;

                                // Resposta para Lista de Amigos e Membros da Guild
                                if (mp.paginas > 0)
                                {

                                    for (var i = 0; i < mp.paginas; i++, mp.increse())
                                    {
                                        p.init_plain((ushort)0x30);

                                        p.WriteUInt16(0x102);   // Sub Packet Id

                                        p.WriteBytes(mp.pag.ToArray()); //, sizeof(mp.pag));

                                        var _begin = friend_list.Skip(mp.index.start)/*  // Pula até o índice de início*/.Take(mp.index.end - mp.index.start); // Pega apenas os elementos entre start e end

                                        foreach (var begin in _begin)
                                        {
                                            p.WriteBytes((begin).ToArray()); //, sizeof(FriendInfo));

                                            s = (Player)m_player_manager.findSessionByUID((begin).uid);

                                            // Se o Player tem ele na lista de amigos, e ele n�o estiver bloqueado na lista do amigo
                                            if (s != null && (pFi = s.m_pi.m_friend_manager.findFriendInAllFriend(el.Value.m_pi.uid)) != null && !pFi.state.block.IsTrue())
                                            {   // Player est� online

                                                p.WriteBytes(s.m_pi.m_cpi.ToArray()); //, sizeof(ChannelPlayerInfo));

                                                // State Icon Player
                                                p.WriteByte(s.m_pi.m_state);

                                                switch (s.m_pi.m_state)
                                                {
                                                    case 0: // IN GAME
                                                        (begin).state.play = 1;
                                                        break;
                                                    case 1: // AFK
                                                        (begin).state.AFK = 1;
                                                        break;
                                                    case 3: // BUSY
                                                        (begin).state.busy = 1;
                                                        break;
                                                    case 4: // ON
                                                    default:
                                                        (begin).state.online = 1;
                                                        break;
                                                }

                                                // Online
                                                (begin).state.online = 1;

                                            }
                                            else
                                            {   // player n�o est� online
                                                p.WriteInt16(-1);       // Sala Numero
                                                p.WriteInt32(-1);       // Sala Tipo
                                                p.WriteInt32(-1);       // Server GUID
                                                p.WriteSByte(-1);       // Canal ID
                                                p.WriteZeroByte(64);    // Canal Nome

                                                // State Icon Player, OFFLINE not change icon
                                                p.WriteByte(5); // OFFLINE

                                                // Offline
                                                (begin).state.online = 0;
                                            }

                                            p.WriteByte((begin).cUnknown_flag);

                                            // Aqui quando � o player e ele est� guild � 1/*Master*/, 2 sub, e outros membro guild � 0, e quando � friend � o level
                                            p.WriteByte((begin).flag.ucFlag == 2/*S� Guild Member*/ ? ((begin).uid == el.Value.m_pi.uid ? 1/*Master*/ : 0) : (begin).level);

                                            p.WriteByte((begin).state.ucState);
                                            p.WriteByte((begin).flag.ucFlag);
                                        }

                                        packet_func.session_send(p, el.Value, 1);
                                    }

                                }
                                else
                                {

                                    // N�o tem nenhum amigo, manda a p�gina vazia
                                    p.init_plain((ushort)0x30);

                                    p.WriteUInt16(0x102);   // Sub Packet Id

                                    p.WriteBytes(mp.pag.ToArray()); //, sizeof(mp.pag));

                                    packet_func.session_send(p, el.Value, 1);
                                }
                            }
                        }
                    }

                    // Notifica os player d� Guild que o player saiu na Guild
                    p.init_plain((ushort)0x3C);

                    p.WriteUInt32(pi.uid);

                    packet_func.friend_broadcast(v_cm, p, s/*/*S� para enviar, esse ele n�o usa s� verifica se � o mesmo para n�o enviar para o mesmo Player/*/, 1);

                }
                else
                {   // Player est� online	

                    // player saiu da Guild
                    s.m_pi.guild_uid = 0;

                    // Update All Friend/Guild Member
                    s.m_pi.m_friend_manager.init(s.m_pi);

                    // Envia para cada player sua lista de amigos
                    {
                        foreach (var el in v_cm)
                        {

                            if (el.Value != null)
                            {

                                var friend_list = el.Value.m_pi.m_friend_manager.getAllFriendAndGuildMember();

                                var mp = new ManyPacket((ushort)friend_list.Count, FRIEND_PAG_LIMIT);

                                FriendInfoEx pFi = null;

                                // Resposta para Lista de Amigos e Membros da Guild
                                if (mp.paginas > 0)
                                {

                                    for (var i = 0; i < mp.paginas; i++, mp.increse())
                                    {
                                        p.init_plain((ushort)0x30);

                                        p.WriteUInt16(0x102);   // Sub Packet Id

                                        p.WriteBytes(mp.pag.ToArray()); //, sizeof(mp.pag));

                                        var _begin = friend_list.Skip(mp.index.start)/*  // Pula até o índice de início*/.Take(mp.index.end - mp.index.start); // Pega apenas os elementos entre start e end

                                        foreach (var begin in _begin)
                                        {
                                            p.WriteBytes((begin).ToArray()); //, sizeof(FriendInfo));

                                            s = (Player)m_player_manager.findSessionByUID((begin).uid);

                                            // Se o Player tem ele na lista de amigos, e ele n�o estiver bloqueado na lista do amigo
                                            if (s != null && (pFi = s.m_pi.m_friend_manager.findFriendInAllFriend(el.Value.m_pi.uid)) != null && !pFi.state.block.IsTrue())
                                            {   // Player est� online

                                                p.WriteBytes(s.m_pi.m_cpi.ToArray()); //, sizeof(ChannelPlayerInfo));

                                                // State Icon Player
                                                p.WriteByte(s.m_pi.m_state);

                                                switch (s.m_pi.m_state)
                                                {
                                                    case 0: // IN GAME
                                                        (begin).state.play = 1;
                                                        break;
                                                    case 1: // AFK
                                                        (begin).state.AFK = 1;
                                                        break;
                                                    case 3: // BUSY
                                                        (begin).state.busy = 1;
                                                        break;
                                                    case 4: // ON
                                                    default:
                                                        (begin).state.online = 1;
                                                        break;
                                                }

                                                // Online
                                                (begin).state.online = 1;

                                            }
                                            else
                                            {   // player n�o est� online
                                                p.WriteInt16(-1);       // Sala Numero
                                                p.WriteInt32(-1);       // Sala Tipo
                                                p.WriteInt32(-1);       // Server GUID
                                                p.WriteSByte(-1);       // Canal ID
                                                p.WriteZeroByte(64);    // Canal Nome

                                                // State Icon Player, OFFLINE not change icon
                                                p.WriteByte(5); // OFFLINE

                                                // Offline
                                                (begin).state.online = 0;
                                            }

                                            p.WriteByte((begin).cUnknown_flag);

                                            // Aqui quando � o player e ele est� guild � 1/*Master*/, 2 sub, e outros membro guild � 0, e quando � friend � o level

                                            p.WriteByte((begin).flag.ucFlag == 2/*S� Guild Member*/ ? ((begin).uid == el.Value.m_pi.uid ? 1/*Master*/ : 0) : (begin).level);

                                            p.WriteByte((begin).state.ucState);
                                            p.WriteByte((begin).flag.ucFlag);
                                        }

                                        packet_func.session_send(p, el.Value, 1);
                                    }


                                }
                                else
                                {

                                    // N�o tem nenhum amigo, manda a p�gina vazia
                                    p.init_plain((ushort)0x30);

                                    p.WriteUInt16(0x102);   // Sub Packet Id

                                    p.WriteBytes(mp.pag.ToArray()); //, sizeof(mp.pag));

                                    packet_func.session_send(p, el.Value, 1);
                                }
                            }
                        }
                    }

                    // Notifica os player d� Guild que o player saiu na Guild
                    p.init_plain((ushort)0x3C);

                    p.WriteUInt32(s.m_pi.uid);

                    packet_func.friend_broadcast(v_cm, p, s, 1);
                }

                // Log
                _smp.message_pool.getInstance().push(new message("[MessengerServer::requestKickGuildMember][Log] Player[UID=" + (member_uid)
                        + "] foi chutado do Club[UID=" + (club_id) + "] com sucesso.", type_msg.CL_FILE_LOG_AND_CONSOLE));

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[MessengerServer::requestKickGuildMember][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

        }

        public new void SQLDBResponse(int _msg_id, Pangya_DB _pangya_db, object _arg)
        {
            if (_arg == null)
            {
                _smp.message_pool.getInstance().push(new message("[MessengerServer::SQLDBResponse][WARNING] _arg is nullptr, na msg_id = " + (_msg_id), type_msg.CL_FILE_LOG_AND_CONSOLE));
                return;
            }

            // Por Hora s� sai, depois fa�o outro tipo de tratamento se precisar
            if (_pangya_db.getException().getCodeError() != 0)
            {
                _smp.message_pool.getInstance().push(new message("[MessengerServer::SQLDBResponse][Error] " + _pangya_db.getException().getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                return;
            }

            switch (_msg_id)
            {
                case 1: // Insert Block IP
                    {
                        var cmd_ibi = (CmdInsertBlockIp)(_pangya_db);

                        _smp.message_pool.getInstance().push(new message("[MessengerServer::SQLDBResponse][Log] Inseriu Block IP[IP=" + cmd_ibi.getIP()
         + ", MASK=" + cmd_ibi.getMask() + "] com sucesso.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                        break;
                    }
                case 2: // Update Server Rate Config Info
                    {

                        var cmd_urci = (CmdUpdateRateConfigInfo)(_pangya_db);

                        _smp.message_pool.getInstance().push(new message("[MessengerServer::SQLDBResponse][Log] Atualizou Rate Config Info[SERVER_UID=" + (cmd_urci.getServerUID())
                                + ", " + cmd_urci.GetInfo().ToString() + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                        break;
                    }
                case 0:
                default:
                    break;
            }
        }

        protected bool sendUpdatePlayerLogoutToFriends(Player _session)
        {
            bool ret = true;
            var p = new PangyaBinaryWriter();
            try
            {

                /* Lógica Atômica:
            Tenta mudar m_logout de 0 para 1.
            Se o retorno for 1, significa que outra Thread já passou por aqui.
         */
                if (Interlocked.CompareExchange(ref _session.m_pi.m_logout, 1, 0) == 1)
                {
                    return false;
                }

                // Resposta para os amigos do player, que ele deslogou
                p.init_plain(0x30);

                p.WriteUInt16(0x10F); // Sub packet Id

                p.WriteUInt32(_session.m_pi.uid);

                packet_func.friend_broadcast(m_player_manager.findAllFriend(_session.m_pi.m_friend_manager.getAllFriendAndGuildMember(true/*Not Send To Block Friend*/)), p, _session, 1);

                _smp.message_pool.getInstance().push(new message("[MessengerServer::sendUpdatePlayerLogoutToFriends][Log] PLAYER[ID: " + (_session.m_pi.id) + ", UID: " + (_session.m_pi.uid) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[MessengerServer::sendUpdatePlayerLogoutToFriends][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Error
                ret = false;
            }

            return ret;
        }
        public override void config_init()
        {
            base.config_init();

            // Tipo Server
            m_si.tipo = 3;


            // Recupera Valores de rate do server do banco de dados
            var cmd_rci = new CmdRateConfigInfo(m_si.uid);  // Waiter

            if (cmd_rci.getException().getCodeError() != 0 || cmd_rci.isError()/*Deu erro na consulta não tinha o rate config info para esse gs, pode ser novo*/)
            {

                if (cmd_rci.getException().getCodeError() != 0)
                    _smp.message_pool.getInstance().push(new message("[MessengerServer::config_init][ErrorSystem] " + cmd_rci.getException().getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                _smp.message_pool.getInstance().push(new message("[MessengerServer::config_init][Error] nao conseguiu recuperar os valores de rate do server[UID="
                        + (m_si.uid) + "] no banco de dados. Utilizando valores padroes de rates.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                m_si.rate.scratchy = 100;
                m_si.rate.papel_shop_rare_item = 100;
                m_si.rate.papel_shop_cookie_item = 100;
                m_si.rate.treasure = 100;
                m_si.rate.memorial_shop = 100;
                m_si.rate.chuva = 100;
                m_si.rate.grand_zodiac_event_time = 1; // Ativo por padr�o
                m_si.rate.grand_prix_event = 1;        // Ativo por padr�o
                m_si.rate.golden_time_event = 1;       // Ativo por padr�o
                m_si.rate.login_reward_event = 1;      // Ativo por padr�o
                m_si.rate.bot_gm_event = 1;            // Ativo por padr�o
                m_si.rate.smart_calculator = 0;        // Atibo por padr�o

                m_si.rate.angel_event = 0;             // Desativado por padr�o
                m_si.rate.pang = 0;
                m_si.rate.exp = 0;
                m_si.rate.club_mastery = 0;

                // Atualiza no banco de dados
                snmdb.NormalManagerDB.getInstance().add(2, new CmdUpdateRateConfigInfo(m_si.uid, m_si.rate), SQLDBResponse, this);

            }
            else
            {   // Conseguiu recuperar com sucesso os valores do server

                m_si.rate.scratchy = cmd_rci.getInfo().scratchy;
                m_si.rate.papel_shop_rare_item = cmd_rci.getInfo().papel_shop_rare_item;
                m_si.rate.papel_shop_cookie_item = cmd_rci.getInfo().papel_shop_cookie_item;
                m_si.rate.treasure = cmd_rci.getInfo().treasure;
                m_si.rate.memorial_shop = cmd_rci.getInfo().memorial_shop;
                m_si.rate.chuva = cmd_rci.getInfo().chuva;
                m_si.rate.grand_zodiac_event_time = cmd_rci.getInfo().grand_zodiac_event_time;
                m_si.rate.grand_prix_event = cmd_rci.getInfo().grand_prix_event;
                m_si.rate.golden_time_event = cmd_rci.getInfo().golden_time_event;
                m_si.rate.login_reward_event = cmd_rci.getInfo().login_reward_event;
                m_si.rate.bot_gm_event = cmd_rci.getInfo().bot_gm_event;
                m_si.rate.smart_calculator = cmd_rci.getInfo().smart_calculator;

                m_si.rate.angel_event = cmd_rci.getInfo().angel_event;
                m_si.rate.pang = cmd_rci.getInfo().pang;
                m_si.rate.exp = cmd_rci.getInfo().exp;
                m_si.rate.club_mastery = cmd_rci.getInfo().club_mastery;
            }
        }
        public void reload_files()
        {
            base.config_init();
            config_init();

            // Reload All Globals Systems
            reload_systems();

            _smp.message_pool.getInstance().push(new message("[MessengerServer::reload_files][Log] Reload System now sucess!", type_msg.CL_FILE_LOG_AND_CONSOLE));


        }

        protected virtual void reload_systems()
        {
            // Recarrega IFF_STRUCT
            sIff.getInstance().reload();

            // Recarrega Smart Calculator Lib, s� recarrega se ele estiver ativado
            //if (m_si.rate.smart_calculator == 1)
            // sSmartCalculator.getInstance().load();
        }

        public void reloadGlobalSystem(uint _tipo)
        {
            try
            {
                switch (_tipo)
                {
                    case 0:     // Reload All Globals Systems
                        reload_systems();
                        break;

                    case 1:     // IFF
                                // Recarrega IFF_STRUCT
                        sIff.getInstance().reload();
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
                                // sSmartCalculator.getInstance().load();
                        break;

                    default:
                        throw new Exception($"[MessengerServer::reloadGlobalSystem][Error] Tipo[VALUE={_tipo}] desconhecido.");
                }

                // Log
                _smp.message_pool.getInstance().push(
                     new message($"[MessengerServer::reloadGlobalSystem][Error] Recarregou o Sistema[Tipo={_tipo}] com sucesso!", type_msg.CL_FILE_LOG_AND_CONSOLE)
                 );
            }
            catch (Exception e)
            {
                _smp.message_pool.getInstance().push(
                     new message($"[MessengerServer::reloadGlobalSystem][ErrorSystem] {e.Message}", type_msg.CL_FILE_LOG_AND_CONSOLE)
                 );
            }
        }


        // Update rate e Event of Server

        public void updateRateAndEvent(int _tipo, uint _qntd)
        {
            try
            {

                if (_qntd == 0u && _tipo != 9/*Grand Zodiac Event Time*/ && _tipo != 10/*Angel Event*/
                    && _tipo != 11/*Grand Prix Event*/ && _tipo != 12/*Golden Time Event*/ && _tipo != 13/*Login Reward Event*/
                    && _tipo != 14/*Bot GM Event*/ && _tipo != 15/*Smart Calculator*/)
                    throw new exception("[MessengerServer::updateRateAndEvent][Error] Rate[TIPO=" + (_tipo) + ", QNTD="
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
                            m_si.rate.smart_calculator = (short)_qntd;

                            // Recarrega o Smart Calculator System se ele foi ativado
                            if (m_si.rate.smart_calculator == 1)
                                reloadGlobalSystem(18/*Smart Calculator*/);

                            break;
                        }
                    default:
                        throw new exception("[MessengerServer::updateRateAndEvent][Error] troca Rate[TIPO=" + (_tipo) + ", QNTD="
                                + (_qntd) + "], tipo desconhecido.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 120, 0));
                }

                // Update no DB os server do server que foram alterados
                snmdb.NormalManagerDB.getInstance().add(2, new CmdUpdateRateConfigInfo(m_si.uid, m_si.rate), SQLDBResponse, this);

                // Log
                _smp.message_pool.getInstance().push(new message("[MessengerServer::updateRateAndEvent][Error] New Rate[Tipo=" + (_tipo) + ", QNTD="
                        + (_qntd) + "] com sucesso!", type_msg.CL_FILE_LOG_AND_CONSOLE));


            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[MessengerServer::updateRateAndEvent][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }



        public override void authCmdShutdown(int _time_sec)
        {
            try
            {

                // Shut down com tempo
                if (m_shutdown == null)
                {

                    // Log
                    _smp.message_pool.getInstance().push(new message("[MessengerServer::authCmdShutdown][Error] Auth Server requisitou para o server ser desligado em "
                            + _time_sec + " segundos", type_msg.CL_FILE_LOG_AND_CONSOLE));

                    shutdown_time(_time_sec);

                }
                else
                    _smp.message_pool.getInstance().push(new message("[MessengerServer::authCmdShutdown][Warning] Auth Server requisitou para o server ser delisgado em "
                            + _time_sec + " segundos, mas o server ja esta com o timer de shutdown", type_msg.CL_FILE_LOG_AND_CONSOLE));

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[MessengerServer::authCmdShutdown][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
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
                    throw new exception("[MessengerServer::shutdown_time][Error] nao conseguiu criar o timer", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.RANK_SERVER, 51, 0));
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
                    //_smp.message_pool.getInstance().push(new message("[MessengerServer::authCmdDisconnectPlayer][log] Comando do Auth Server, Server[UID=" + (_req_server_uid)
                    //        + "] pediu para desconectar o PLAYER[UID=" + (s.m_pi.uid) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                    // Deconecta o Player
                    if (_force == 1) // Força o Disconect do player, sem verificar as regras do Game Server
                        DisconnectSession(s);
                    else
                    {

                        // Read Ini File for take Flag Same Id Login

                        int same_id_login = 0;

                        try
                        {
                            same_id_login = m_reader_ini.readInt("OPTION", "SAME_ID_LOGIN", 0);
                        }
                        catch
                        {

                        }

                        // Só desconecta aqui se a type do server de poder logar com o mesmo id estiver desativada
                        if (!(same_id_login == 1))
                            DisconnectSession(s);
                    }

                }
                else
                {

                    // Não encontrou o player no server, então desconecta no banco de dados
                    snmdb.NormalManagerDB.getInstance().add(5, new CmdRegisterLogon(_player_uid, 1/*Logout*/), SQLDBResponse, this);

                    // Log
                    //_smp.message_pool.getInstance().push(new message("[MessengerServer::authCmdDisconnectPlayer][Warning] Comando do Auth Server, Server[UID=" + (_req_server_uid)
                    //        + "] pediu para desconectar o PLAYER[UID=" + (_player_uid) + "], mas nao encontrou ele no server, entao desconecta ele no banco de dados.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                }

                // UPDATE ON Auth Server
                m_unit_connect.sendConfirmDisconnectPlayer(_req_server_uid, _player_uid);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[MessengerServer::authCmdDisconnectPlayer][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void authCmdConfirmDisconnectPlayer(uint _req_server_uid)
        {

            // Message Server n�o usa esse Comando
            return;
        }

        public override void authCmdNewMailArrivedMailBox(uint _player_uid, int _mail_id)
        {
            // Message Server n�o usa esse Comando
            return;
        }

        public override void authCmdNewRate(uint _tipo, uint _qntd)
        {

            try
            {

                updateRateAndEvent((int)_tipo, _qntd);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[MessengerServer::authCmdNewRate][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void authCmdReloadGlobalSystem(uint _tipo)
        {

            try
            {
                reloadGlobalSystem(_tipo);
            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[MessengerServer::authCmdReloadGlobalSystem][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void authCmdConfirmSendInfoPlayerOnline(uint _req_server_uid, AuthServerPlayerInfo _aspi)
        {
            try
            {

                var s = m_player_manager.findPlayer(_aspi.uid);

                if (s != null)
                {

                    confirmLoginOnOtherServer(s, _req_server_uid, _aspi);

                }
                else
                    _smp.message_pool.getInstance().push(new message("[MessengerServer::authCmdConfirmSendInfoPlayerOnline][Warning] PLAYER[UID=" + (_aspi.uid)
                            + "] retorno do confirma login com Auth Server do Server[UID=" + (_req_server_uid) + "], mas o palyer nao esta mais conectado.", type_msg.CL_FILE_LOG_AND_CONSOLE));

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[MessengerServer::authCmdConfirmSendInfoPlayerOnline][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }


        public override bool CheckCommand(Queue<string> _command)
        {
            Console.ResetColor();

            if (_command.Count == 0)
            {
                _smp.message_pool.getInstance().push(new message("[MessengerServer::CheckCommand][Error] Missing parameter", type_msg.CL_ONLY_CONSOLE));
                return true;
            }

            string s = _command.Dequeue();

            if (!string.IsNullOrEmpty(s) && s == "exit")
            {
                Environment.Exit(-1);
                return true;
            }
            else if (!string.IsNullOrEmpty(s) && s == "reload_files")
            {
                reload_files();
                return true;
            }
            else if (!string.IsNullOrEmpty(s) && s == "rate")
            {
                string sTipo = _command.Dequeue();
                int tipo = -1;

                if (!string.IsNullOrEmpty(sTipo))
                {
                    switch (sTipo)
                    {
                        case "pang": tipo = 0; break;
                        case "exp": tipo = 1; break;
                        case "club": tipo = 2; break;
                        case "chuva": tipo = 3; break;
                        case "treasure": tipo = 4; break;
                        case "scratchy": tipo = 5; break;
                        case "pprareitem": tipo = 6; break;
                        case "ppcookieitem": tipo = 7; break;
                        case "memorial": tipo = 8; break;
                        default:
                            _smp.message_pool.getInstance().push(new message($"[MessengerServer::checkCommand][Error] Unknown Command: \"rate {sTipo}\"", type_msg.CL_ONLY_CONSOLE));
                            break;
                    }
                }
                else
                {
                    _smp.message_pool.getInstance().push(new message($"[MessengerServer::checkCommand][Error] Unknown Command: \"rate {sTipo}\"", type_msg.CL_ONLY_CONSOLE));
                }

                if (tipo != -1 && tipo >= 0 && tipo <= 8)
                {
                    if (uint.TryParse(_command.Dequeue(), out uint qntd) && qntd > 0)
                    {
                        updateRateAndEvent(tipo, qntd);
                    }
                    else
                    {
                        _smp.message_pool.getInstance().push(new message($"[MessengerServer::checkCommand][Error] Unknown value, Command: \"rate {sTipo}\"", type_msg.CL_ONLY_CONSOLE));
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
                            updateRateAndEvent(9, qntd);
                            break;
                        case "angel_event":
                            updateRateAndEvent(10, qntd);
                            break;
                        case "grand_prix":
                            updateRateAndEvent(11, qntd);
                            break;
                        case "golden_time":
                            updateRateAndEvent(12, qntd);
                            break;
                        case "login_reward":
                            updateRateAndEvent(13, qntd);
                            break;
                        case "bot_gm_event":
                            updateRateAndEvent(14, qntd);
                            break;
                        case "smart_calc":
                            updateRateAndEvent(15, qntd);
                            break;
                        default:
                            _smp.message_pool.getInstance().push(new message($"[MessengerServer::checkCommand][Error] Unknown Comamnd: \"Event {s}\"", type_msg.CL_ONLY_CONSOLE));
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
                        case "papel_shop": tipo = 4; break;
                        case "box": tipo = 5; break;
                        case "memorial_shop": tipo = 6; break;
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
                        case "bot_gm_event": tipo = 17; break;
                        case "smart_calc": tipo = 18; break;
                        default:
                            _smp.message_pool.getInstance().push(new message($"[MessengerServer::checkCommand][Error] Unknown Command: \"reload_system {sTipo}\"", type_msg.CL_ONLY_CONSOLE));
                            break;
                    }
                }
                else
                {
                    _smp.message_pool.getInstance().push(new message($"[MessengerServer::checkCommand][Error] Unknown Command: \"reload_system {sTipo}\"", type_msg.CL_ONLY_CONSOLE));
                }

                if (tipo != -1 && tipo >= 0 && tipo <= 18)
                {
                    reloadGlobalSystem((uint)tipo);
                }
                return true;

            }
            else if (s == "cls" || s == "clear")
            {
                Console.Clear();
                ConsoleEx.Log();
                return true;
            }
            else
            {
                _smp.message_pool.getInstance().push(new message($"[MessengerServer::CheckCommand][Error] Command No Exist-> {s}", type_msg.CL_ONLY_CONSOLE));
                return false;
            }
        }

    }
}

// Server Static 
namespace sms
{
    public class ms : Singleton<Pangya_MessengerServer.MessengerServerTcp.MessengerServer>
    {
    }
}