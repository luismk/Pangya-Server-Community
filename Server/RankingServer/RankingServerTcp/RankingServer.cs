using System;
using PangyaAPI.Network.PangyaPacket;
using PangyaAPI.Network.PangyaServer;
using PangyaAPI.Utilities;
using Pangya_RankingServer.Session;
using PangyaAPI.Utilities.Log;
using PangyaAPI.IFF.JP.Extensions;
using Pangya_RankingServer.PacketFunc;
using PangyaAPI.Utilities.Models;
using PangyaAPI.Network.Repository;
using Pangya_RankingServer.PangyaEnums;
using System.Threading;
using PangyaAPI.Network.Models;
using Pangya_RankingServer.Repository;
using Pangya_RankingServer.Models;
using System.Diagnostics;
using System.Collections.Generic;
using Pangya_RankingServer.UTIL;
using PangyaAPI.SQL;
namespace Pangya_RankingServer.RankingServerTcp
{
    public class RankingServer : Server
    {
        static player_manager m_player_manager = new player_manager();
        RankRefreshTime m_refresh_time;

        int m_sync_update_time_refresh = 0;

        public enum SEARCH_OPTION : byte
        {
            SO_NICKNAME,
            SO_POSITION
        }
        public RankingServer() : base(m_player_manager)
        {
            if (m_state == ServerState.Failure)
            {
                _smp.message_pool.getInstance().push(new message("[RankingServer::RankingServer][Error] falha ao incializar o message server.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                return;
            }

            try
            {
               
                config_init();
                 
                // Request Cliente
                init_Packets();

                init_systems();
                 
                // Initialized complete
                m_state = ServerState.Initialized;
            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[RankingServer::RankingServer][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

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
                        if (packetId != 244)
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
                throw new exception("[RankingServer::onDisconnect][Error] _session is nullptr.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MESSAGE_SERVER, 60, 0));

            Player p = (Player)_session;

            _smp.message_pool.getInstance().push(new message("[RankingServer::onDisconnected][Log] PLAYER[ID: " + (p.m_pi.id) + ", UID: " + (p.m_pi.uid) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));
        }

        public void init_systems()
        {
            try
            { 
                // Carrega IFF_STRUCT
                if (!sIff.getInstance().isLoad())
                    sIff.getInstance().initilation(); 

                // Trava update check, para n�o ficar enviando varias requisi��o para atualizar os registro para o banco de dados
                m_sync_update_time_refresh = 1;

                // Envia a requisi��o para o banco de dados
                snmdb.NormalManagerDB.getInstance().add(1,
                new CmdUpdateRankRegistry(),
                this.SQLDBResponse,
                this);

                if (!sRankRegistryManager.getInstance().isLoad())
                    sRankRegistryManager.getInstance().load(); // Carrega os registros do Rank
            }
            catch (exception e)
            {
                _smp.message_pool.getInstance().push(new message("[rank_server::onHeartBeat][Error] -> " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

            }
        }

        public override void OnHeartBeat()
        {  
            try
            { 
                // Server ainda n�o est� totalmente iniciado
                if (m_state != ServerState.Initialized)
                    return;

                if (!sRankRegistryManager.getInstance().isLoad())
                    sRankRegistryManager.getInstance().load(); // Carrega os registros do Rank

                if (m_state == ServerState.Initialized  && m_sync_update_time_refresh == 0  && m_refresh_time.isOutDated())
                {

                    // Trava update check, para n�o ficar enviando varias requisi��o para atualizar os registro para o banco de dados
                   m_sync_update_time_refresh = 1;

                    // Envia a requisi��o para o banco de dados
                    snmdb.NormalManagerDB.getInstance().add(1,
                    new CmdUpdateRankRegistry(),
                    this.SQLDBResponse,
                    this);
                }
                
            }
            catch (exception e)
            { 
                _smp.message_pool.getInstance().push(new message("[RankingServer::onHeartBeat][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            } 
        }

        public override void OnStart()
        {
            Console.Title = $"Ranking Server - P: {m_si.curr_user}";
            m_state = ServerState.Initialized;
        }

        protected override void onAcceptCompleted(PangyaAPI.Network.PangyaSession.Session _session)
        {
            try
            {
                var _packet = new packet(0x1388);
                // Se mandar -1 no valor da chave o cliente n�o encripta o pacote antes de enviar 
                _packet.AddInt32(_session.m_key); // key
                _packet.AddByte(5);  // Type Rank Server
                _packet.AddString(UtilTime.formatDateLocal(0));


                _packet.makeRaw();
                var mb = _packet.getBuffer();

                _session.requestSendBuffer(mb, true);
            }
            catch (Exception ex)
            {
                _smp.message_pool.getInstance().push(new message(
              $"[Pangya_RankingServer.onAcceptCompleted][ErrorSt] {ex.Message}\nStack Trace: {ex.StackTrace}",
              type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        /// <summary>
        /// init packet to call !
        /// </summary>
        protected void init_Packets()
        {

            // Request Cliente
            packet_func.funcs.addPacketCall(0x00, packet_func.packet000, this);
            packet_func.funcs.addPacketCall(0x01, packet_func.packet001, this);
            packet_func.funcs.addPacketCall(0x02, packet_func.packet002, this);
            packet_func.funcs.addPacketCall(0x03, packet_func.packet003, this);
            packet_func.funcs.addPacketCall(0x04, packet_func.packet004, this);
            packet_func.funcs.addPacketCall(0x05, packet_func.packet005, this);


            // Resposta Server
            packet_func.funcs_sv.addPacketCall(0x1388, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x1389, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x138A, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x138B, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x138C, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x138D, packet_func.packet_svFazNada, this);

            // Auth Server
            packet_func.funcs_as.addPacketCall(0x01, packet_func.packet_as001, this);
        }


        public override void config_init()
        {
            base.config_init();

            // Server Tipo
            m_si.tipo = 4/*Auth Server*/;
            // Carrega a configura��o do Rank
            CmdRankConfigInfo cmd_rci = new CmdRankConfigInfo(true); // Waiter

            snmdb.NormalManagerDB.getInstance().add(0,
                cmd_rci, null, null);

            if (cmd_rci.getException().getCodeError() != 0)
            {
                throw cmd_rci.getException();
            }

            m_refresh_time = cmd_rci.getInfo();
        }
        protected virtual void ReloadFiles()
        {
            config_init();

            sIff.getInstance().reload();
        }

        public void confirmLoginOnOtherServer(Player _session,
        uint _req_server_uid,
        AuthServerPlayerInfo _aspi)
        {

            var p = new PangyaBinaryWriter();

            try
            {

                if (_aspi.uid != _session.m_pi.uid)
                {
                    throw new exception("[rank_server::confirmLoginOnOtherServer][Error] Player[UID=" + Convert.ToString(_session.m_pi.uid) + ", REQ_UID=" + Convert.ToString(_aspi.uid) + ", REQ_SERVER=" + Convert.ToString(_req_server_uid) + "] request Info player, mas nao eh o mesmo UID que foi retornado do request com o Auth Server. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.RANK_SERVER,
                        1, 0x5200201));
                }

                if (_aspi.option != 1)
                {
                    throw new exception("[rank_server::confirmLoginOnOtherServer][Error] Player[UID=" + Convert.ToString(_session.m_pi.uid) + ", REQ_UID=" + Convert.ToString(_aspi.uid) + ", REQ_SERVER=" + Convert.ToString(_req_server_uid) + "] request Info player, mas nao esta online no outro server.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.RANK_SERVER,
                        2, 0x5200202));
                }

                if (_aspi.id.CompareTo(_session.m_pi.id) != 0)
                {
                    throw new exception("[rank_server::confirmLoginOnOtherServer][Error] Player[UID=" + Convert.ToString(_session.m_pi.uid) + ", REQ_UID=" + Convert.ToString(_aspi.uid) + ", REQ_SERVER=" + Convert.ToString(_req_server_uid) + "] request Info player, mas nao eh o mesmo ID[ID=" + _session.m_pi.id + ", REQ_ID=" + _aspi.id + "] que foi retornado do request com o Auth Server.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.RANK_SERVER,
                        3, 0x5200203));
                }

                // Confirm Login com sucesso, Atualiza o cliente
                _session.m_pi.m_state = 4;

                // Authorized a ficar online no server por tempo indeterminado
                _session.m_is_authorized = true;

                // Resposta para o Pedido de Login
                sendFirstPage(_session, 0);

            }
            catch (exception e)
            {
                // Resposta
                sendFirstPage(_session, 1);

                _smp.message_pool.getInstance().push(new message("[rank_server::confirmLoginOnOtherServer][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void sendFirstPage(Player _session, int _option)
        {
            //CHECK_SESSION_BEGIN("sendFirstPage");

            var p = new PangyaBinaryWriter((ushort)0x1389);

            if (_option != 0)
            {
                p.WriteByte((byte)_option);

                p.WriteZeroByte(14);
            }
            else
            {

                p.WriteByte(_option);

                p.WriteByte(_session.m_pi.m_sd.rank_menu);
                p.WriteByte(_session.m_pi.m_sd.rank_menu_item);

                // Op��es descontinuadas no Fresh UP!, por�m ele ainda mant�m nos packet
                p.WriteByte(_session.m_pi.m_sd.term_s5_type);

                // Op��es descontinuadas no Fresh UP!, por�m ele ainda mant�m nos packet
                p.WriteByte(_session.m_pi.m_sd.class_type);

                sRankRegistryManager.getInstance().pageToPacket(p, _session.m_pi.m_sd);

                // Resposta da requisi��o do player
                // 0 - player est� no rank entre os player colocados. Ex (Top 100)
                // 1 - player n�o est� no rank
                // 2 - player est� no rank, por�m ele n�o est� no top. Ex (Top 100)

                if (_session.m_pi.m_sd.active > 0)
                {
                    sRankRegistryManager.getInstance().playerPositionToPacket(p,
                        _session, _session.m_pi.m_sd);
                }
                else
                {
                    p.WriteByte(ePLAYER_POSITION_RANK_TYPE.PPRT_NOT_TOP_RANK);
                }
            }

            packet_func.session_send(p,
                _session, 1);
        }

        public void requestPlayerInfo(Player _session, packet _packet)
        {
            //REQUEST_BEGIN("PlayerInfo");

            try
            {

                // Verifica se session est� autorizada para executar esse a��o, 
                // se ele n�o fez o login com o Server ele n�o pode fazer nada at� que ele fa�a o login
                ////CHECK_SESSION_IS_AUTHORIZED("PlayerInfo");
                // Request Player Info n�o usa por que ele manda junto com o de logar/pesquisar

                uint uid = _packet.ReadUInt32();
                string id = _packet.ReadString();
                byte active = _packet.ReadByte();

                sRankRegistryManager.getInstance().sendPlayerFullInfo(_session, uid);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[rank_server::requestPlayerInfo][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }




        public void updateTimeRefresh(uint _ret, DateTime _date)
        {

            try
            {
                if (_ret == 0)
                {
                   m_sync_update_time_refresh = 0;
                }
                else if (_ret == 1)
                {

                    // Atualiza tempo e recarregar o registro do Rank novamente
                    m_refresh_time.setLastRefreshDate(_date);

                    sRankRegistryManager.getInstance().load();

                    // Cria arquivo de log, com todos os registros
                    sRankRegistryManager.getInstance().makeLog();
 
                    // Libera o HearBeat para verificar de novo quando tempo vai acabar
                    m_sync_update_time_refresh = 0;
                }
            }
            catch (exception e)
            {
                _smp.message_pool.getInstance().push(new message("[rank_server::updateTimeRefresh][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }


        public void requestSearchPlayerInRank(Player _session, packet _packet)
        { 
            var p = new PangyaBinaryWriter();

            try
            {

                // Verifica se session est� autorizada para executar esse a��o, 
                // se ele n�o fez o login com o Server ele n�o pode fazer nada at� que ele fa�a o login
                //CHECK_SESSION_IS_AUTHORIZED("SearchPlayerInRank");

                byte option = _packet.ReadByte();

                if (option == (byte)SEARCH_OPTION.SO_NICKNAME)
                {

                    string nickname = _packet.ReadString();

                    if (nickname.Length == 0)
                    {
                        throw new exception("[rank_server::requestSearchPlayerInRank][Error] Player[UID=" + Convert.ToString(_session.m_pi.uid) + "] pediu para pesquisar um player no rank, mas o nickname eh invalid(empty). Hacker ou Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.RANK_SERVER,
                            331, 0));
                    }

                    search_dados sd = new search_dados(0u);

                    sd.ToRead(_packet);

                    sRankRegistryManager.getInstance().searchPlayerByNicknameAndSendPage(_session,
                        nickname, sd);

                }
                else if (option == (byte)SEARCH_OPTION.SO_POSITION)
                {

                    uint position = _packet.ReadUInt32();

                    if ((int)position < 0)
                    {
                        throw new exception("[rank_server::requestSearchPlayerInRank][Error] Player[UID=" + Convert.ToString(_session.m_pi.uid) + "] pediu para pesquisar um player no rank, as position no rank value eh invalid(" + Convert.ToString(position) + "). Hacker ou Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.RANK_SERVER,
                            332, 0));
                    }

                    search_dados sd = new search_dados(0u);

                    sd.ToRead(_packet);

                    sRankRegistryManager.getInstance().searchPlayerByRankAndSendPage(_session,
                        position, sd);

                }
                else
                {
                    throw new exception("[rank_server::requestSearchPlayerInRank][Error] Player[UID=" + Convert.ToString(_session.m_pi.uid) + "] pediu para pesquisar um player no rank, mas a option(" + Convert.ToString((ushort)option) + ") que ele passou eh invalid. Hacker ou Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.RANK_SERVER,
                        330, 0));
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[rank_server::requestSearchPlayerInRank][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Error
                p.init_plain((ushort)0x138C);

                p.WriteByte(1); // Error

                packet_func.session_send(p,
                    _session, 1);
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
                    _smp.message_pool.getInstance().push(new message("[RankingServer::authCmdShutdown][Log] Auth Server requisitou para o server ser desligado em "
                            + _time_sec + " segundos", type_msg.CL_FILE_LOG_AND_CONSOLE));

                    shutdown_time(_time_sec);

                }
                else
                    _smp.message_pool.getInstance().push(new message("[RankingServer::authCmdShutdown][WARNING] Auth Server requisitou para o server ser delisgado em "
                            + _time_sec + " segundos, mas o server ja esta com o timer de shutdown", type_msg.CL_FILE_LOG_AND_CONSOLE));

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[RankingServer::authCmdShutdown][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
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
                    throw new exception("[RankingServer::shutdown_time][Error] nao conseguiu criar o timer", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.RANK_SERVER, 51, 0));
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
                    _smp.message_pool.getInstance().push(new message("[RankingServer::authCmdDisconnectPlayer][log] Comando do Auth Server, Server[UID: " + (_req_server_uid)
                            + "] pediu para desconectar o Player[UID: " + (s.m_pi.uid) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                    // Deconecta o Player
                    DisconnectSession(s);

                    // UPDATE ON Auth Server
                    m_unit_connect.sendConfirmDisconnectPlayer(_req_server_uid, _player_uid);

                }
                else
                    _smp.message_pool.getInstance().push(new message("[RankingServer::authCmdDisconnectPlayer][WARNING] Comando do Auth Server, Server[UID: " + (_req_server_uid)
                            + "] pediu para desconectar o Player[UID: " + (_player_uid) + "], mas nao encontrou ele no server.", type_msg.CL_FILE_LOG_AND_CONSOLE));

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[RankingServer::authCmdDisconnectPlayer][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void authCmdConfirmDisconnectPlayer(uint _player_uid)
        {
            // Rank Server n�o usa esse Comando

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

                    confirmLoginOnOtherServer(s, _req_server_uid, _aspi);

                }
                else
                    _smp.message_pool.getInstance().push(new message("[RankingServer::authCmdConfirmSendInfoPlayerOnline][WARNING] Player[UID: " + (_aspi.uid)
                            + "] retorno do confirma login com Auth Server do Server[UID: " + (_req_server_uid) + "], mas o palyer nao esta mais conectado.", type_msg.CL_FILE_LOG_AND_CONSOLE));

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[RankingServer::authCmdConfirmSendInfoPlayerOnline][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
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


        public override bool CheckCommand(Queue<string> _command)
        {
            if (_command.Count == 0)
            {
                _smp.message_pool.getInstance().push(new message("[game_server::CheckCommand][Error] Missing parameter", type_msg.CL_ONLY_CONSOLE));
                return true;
            }

            string command = _command.Dequeue();

            if (command.Equals("exit", StringComparison.OrdinalIgnoreCase))
            {
                return true; // Sai
            }
            else if (command.Equals("reload_files", StringComparison.OrdinalIgnoreCase))
            {
                ReloadFiles();
                _smp.message_pool.getInstance().push(new message("Login Server files have been reloaded.", type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
            else if (command.Equals("reload_socket_config", StringComparison.OrdinalIgnoreCase))
            {
                //if (m_accept_sock != null)
                //    m_accept_sock.ReloadConfigFile();
                //else
                //    _smp.message_pool.getInstance().push(new message("[RankingServer::CheckCommand][WARNING] m_accept_sock is invalid.", type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
            else
            {
                _smp.message_pool.getInstance().push(new message($"Unknown Command: {command}", type_msg.CL_ONLY_CONSOLE));
            }

            return false;
        }


        public void requestLogin(Player _session, packet _packet)
        {
            //REQUEST_BEGIN("Login");

            var p = new PangyaBinaryWriter();

            try
            {

                uint uid = _packet.ReadUInt32();
                string id = _packet.ReadString();

                _session.m_pi.m_sd.ToRead(_packet);

                if (uid == 0)
                {
                    throw new exception("[rank_server::requestLogin][Error] player[UID=" + Convert.ToString(uid) + ", ID=" + id + "] tentou logar com Server, mas o uid eh invalido. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.RANK_SERVER,
                        1, 0x5200101));
                }

                if (id.Length == 0)
                {
                    throw new exception("[rank_server::requestLogin][Error] player[UID=" + Convert.ToString(uid) + ", ID=" + id + "] tentou logar com Server, mas o id esta vazio. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.RANK_SERVER,
                        2, 0x5200102));
                }

                // Verifica se o IP/MAC Address est� banido
                if (haveBanList(_session.getIP(),
                    "", false))
                {
                    throw new exception("[rank_server::requestLogin][Error] Player[UID=" + Convert.ToString(uid) + ", ID=" + id + ", IP=" + _session.getIP() + "] tentou logar com o Server, mas ele esta com IP banido.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.RANK_SERVER,
                        5, 0x5200105));
                }

                CmdPlayerInfo cmd_pi = new CmdPlayerInfo(uid, true); // Waiter

                snmdb.NormalManagerDB.getInstance().add(0,
                    cmd_pi, null, null);

                if (cmd_pi.getException().getCodeError() != 0)
                {
                    throw cmd_pi.getException();
                }

                _session.m_pi.set_info(cmd_pi.getInfo());

                if (string.CompareOrdinal(id, _session.m_pi.id) != 0)
                {
                    throw new exception("[rank_server::requestLogin][Error] player[UID=" + Convert.ToString(uid) + ", ID=" + id + "] tentou logar com Server, mas o id do databse[ID_DB=" + (_session.m_pi.id) + "] eh diferente do fornecido pelo cliente. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.RANK_SERVER,
                        4, 0x5200104));
                }

                // Verifica se o player est� bloqueado
                if (_session.m_pi.block_flag.m_id_state.ull_IDState != 0)
                {

                    if (_session.m_pi.block_flag.m_id_state.L_BLOCK_TEMPORARY && (_session.m_pi.block_flag.m_id_state.block_time == -1 || _session.m_pi.block_flag.m_id_state.block_time > 0))
                    {

                        throw new exception("[rank_server::requestLogin][Log] Bloqueado por tempo[Time=" + (_session.m_pi.block_flag.m_id_state.block_time == -1 ? "indeterminado" : (Convert.ToString(_session.m_pi.block_flag.m_id_state.block_time / 60) + "min " + Convert.ToString(_session.m_pi.block_flag.m_id_state.block_time % 60) + "sec")) + "]. player [UID=" + Convert.ToString(_session.m_pi.uid) + ", ID=" + (_session.m_pi.id) + "]", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.RANK_SERVER,
                            1029, 0));

                    }
                    else if (_session.m_pi.block_flag.m_id_state.L_BLOCK_FOREVER)
                    {

                        throw new exception("[rank_server::requestLogin][Log] Bloqueado permanente. player [UID=" + Convert.ToString(_session.m_pi.uid) + ", ID=" + (_session.m_pi.id) + "]", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.RANK_SERVER,
                            1030, 0));

                    }
                    else if (_session.m_pi.block_flag.m_id_state.L_BLOCK_ALL_IP)
                    {

                        // Bloquea todos os IP que o player logar e da error de que a area dele foi bloqueada

                        // Add o ip do player para a lista de ip banidos
                        snmdb.NormalManagerDB.getInstance().add(1,
                            new CmdInsertBlockIp(_session.m_ip, "255.255.255.255"),
                            SQLDBResponse,
                            this);

                        // Resposta
                        throw new exception("[rank_server::requestLogin][Log] Player[UID=" + Convert.ToString(_session.m_pi.uid) + ", IP=" + (_session.m_ip) + "] Block ALL IP que o player fizer login.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.RANK_SERVER,
                            1031, 0));

                    }
                    else if (_session.m_pi.block_flag.m_id_state.L_BLOCK_MAC_ADDRESS)
                    {

                        // Bloquea o MAC Address que o player logar e da error de que a area dele foi bloqueada

                        // Add o MAC Address do player para a lista de MAC Address banidos
                        //snmdb::NormalManagerDB::getInstance().add(2, new CmdInsertBlockMAC(mac_address), rank_server::SQLDBResponse, this);

                        // Resposta
                        throw new exception("[rank_server::requestLogin][Log] Player[UID=" + Convert.ToString(_session.m_pi.uid) + ", IP=" + (_session.m_ip) + ", MAC=UNKNON] (RANK nao recebe o MAC Address do cliente) Block MAC Address que o player fizer login.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.RANK_SERVER,
                            1032, 0));

                    }
                    else if (_session.m_pi.block_flag.m_flag.rank_server)
                    {

                        // Player est� bloqueado no Rank Server, ele n�o pode logar no rank server

                        // Resposta
                        throw new exception("[rank_server::requestLogin][Log][WARNING] Player[UID=" + Convert.ToString(_session.m_pi.uid) + ", IP=" + (_session.m_ip) + "] foi bloqueado o acesso ao Rank Server pelo ADMIN no block_flag.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.RANK_SERVER,
                            1033, 0));
                    }
                }

                // Verifica se j� tem outro socket com o mesmo uid conectado
                var s = HasLoggedWithOuterSocket(_session);

                if (s != null)
                {

                    _smp.message_pool.getInstance().push(new message("[rank_server::requestLogin][Log] Player[UID=" + Convert.ToString(uid) + ", OID=" + Convert.ToString(_session.m_oid) + ", IP=" + _session.getIP() + "] que esta logando agora, ja tem uma outra session com o mesmo UID logado, desloga o outro Player[UID=" + Convert.ToString(s.getUID()) + ", OID=" + Convert.ToString(s.m_oid) + ", IP=" + s.getIP() + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                    if (!DisconnectSession(s))
                    {
                        throw new exception("[rank_server::requestLogin][Error] Nao conseguiu disconnectar o player[UID=" + Convert.ToString(s.getUID()) + "OID=" + Convert.ToString(s.m_oid) + ", IP=" + s.getIP() + "], ele pode esta com o bug do oid bloqueado, ou Session::UsaCtx bloqueado.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.RANK_SERVER,
                            3, 0x5200103));
                    }
                }

                // S� verifica com o game server se ele n�o estiver autorizado(Logado)
                if (!_session.m_is_authorized)
                {

                    // Verifica com o Auth Server se o player est� connectado no server que ele diz e se � o mesmo IP ADDRESS
                    if (m_unit_connect.isLive())
                    {

                        m_unit_connect.getInfoPlayerOnline(_session.m_pi.server_uid, _session.m_pi.uid);

                    }
                    else
                    {
                        throw new exception("[rank_server::requestLogin][Error] Player[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou logar, mas nao conseguiu verificar com o Auth Server se ele estava online no Server[UID=" + Convert.ToString(_session.m_pi.server_uid) + "]. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.RANK_SERVER,
                            6, 0x5200106));
                    }

                }
                else
                {

                    // Resposta para o Pedido de Login
                    sendFirstPage(_session, 0);
                }

            }
            catch (exception e)
            {

                // Resposta
                sendFirstPage(_session, 1);

                _smp.message_pool.getInstance().push(new message("[message_server::requestLogin][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public new void SQLDBResponse(int _msg_id, Pangya_DB _pangya_db, object _arg)
        {
            if (_arg == null)
            {
                _smp.message_pool.getInstance().push(new message("[rank_server::SQLDBResponse][WARNING] _arg is nullptr, na msg_id = " + Convert.ToString(_msg_id), type_msg.CL_FILE_LOG_AND_CONSOLE));
                return;
            }

            // Por Hora s� sai, depois fa�o outro tipo de tratamento se precisar
            if (_pangya_db.getException().getCodeError() != 0)
            {
                _smp.message_pool.getInstance().push(new message("[rank_server::SQLDBResponse][Error] " + _pangya_db.getException().getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                return;
            }

            var _rank_server = (RankingServer)(_arg);

            switch (_msg_id)
            {
                case 1: // Update Rank Registros
                    {
                        var cmd_urr = (CmdUpdateRankRegistry)(_pangya_db);

                        if (cmd_urr.getException().getCodeError() != 0)
                        {

                            // Exception print no console
                            _smp.message_pool.getInstance().push(new message("[rank_server::SQLDBResponse][Error] " + cmd_urr.getException().getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                            // Liberar o verificador no HearBeat
                            _rank_server.updateTimeRefresh(0u, DateTime.Now);

                        }
                        else
                        {
                            _rank_server.updateTimeRefresh(cmd_urr.getRetState(), cmd_urr.getDate());
                        }

                        break;
                    }
                case 0:
                default:
                    break;
            }
        }
    }
}

// Server Static 
namespace srs
{
    public class rs : Singleton<Pangya_RankingServer.RankingServerTcp.RankingServer>
    {
    }
}