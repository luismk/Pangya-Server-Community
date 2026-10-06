using Pangya_RankingServer.Flags;
using Pangya_RankingServer.Handles;
using Pangya_RankingServer.Manager;
using Pangya_RankingServer.Models;
using Pangya_RankingServer.Repository;
using Pangya_RankingServer.Session;
using PangyaAPI.DataBase;
using PangyaAPI.IFF.Handle.JP;
using PangyaAPI.Network;
using PangyaAPI.Network.Core;
using PangyaAPI.Network.Handle;
using PangyaAPI.Network.Models;
using PangyaAPI.Network.Security;
using PangyaAPI.Network.Service;
using PangyaAPI.Network.Session;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using PangyaAPI.Utilities.Models;
using System.Security.Cryptography;

namespace Pangya_RankingServer.Server
{
    public class RankingService : AppServer<Player, PacketIDClient>
    {
        RankRefreshTime m_refresh_time = new();

        int m_sync_update_time_refresh = 0;

        private readonly PlayerManager _playerManager;
        public RankingService() : base(new PlayerManager(500), new PacketDispatcher<Player, PacketIDClient>(), ServerType.RankServer)
        {
            // Fazemos o cast do sessionManager para o seu PlayerManager
            _playerManager = (PlayerManager)SessionsManager;

            LoadConfig();

            RegisterHandlers(); 

            // Carrega IFF_STRUCT
            if (!sIff.Instance.isLoad())
                sIff.Instance.Init(); 
        }

        private async void RegisterHandlers()
        {
            // --- Conexão e Autenticação ---
            //0x01 para o Processo de Login/Identificação
            _dispatcher.Register(PacketIDClient.CLIENT_REQUEST_LOGIN, new Handle_PLAYER_LOGIN());

            _dispatcher.Register(PacketIDClient.CLIENT_REQUEST_PLAYER_INFO, new Handle_REQUEST_PLAYER_INFO());
            // 0x02 para a Busca (Nickname ou Posição)
            _dispatcher.Register(PacketIDClient.CLIENT_REQ_SEARCH_PLAYER_IN_RANKING, new Handle_SEARCH_PLAYER_IN_RANK());
             

        }

        public override bool CheckCommand(Queue<string> _command)
        {
            if (_command.Count == 0)
            {
                _smp.LogManager.Instance.push(new AppMessage("[RankingService::CheckCommand][Error] Missing parameter", type_msg.CL_ONLY_CONSOLE));
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
            }


            else if (!string.IsNullOrEmpty(s) && s == "reload_system")
            {
                string sTipo = _command.Dequeue();
                if (!string.IsNullOrEmpty(sTipo))
                {
                    switch (sTipo)
                    {
                        case "iff":
                            sIff.Instance.reload();
                            return true;
                        default:
                            _smp.LogManager.Instance.push(new AppMessage($"[RankingServer::checkCommand][Error] Unknown Command: \"reload_system {sTipo}\"", type_msg.CL_ONLY_CONSOLE));
                            return false;
                    }
                }
                else
                {
                    _smp.LogManager.Instance.push(new AppMessage($"[RankingServer::checkCommand][Error] Unknown Command: \"reload_system {sTipo}\"", type_msg.CL_ONLY_CONSOLE));
                    return false;
                }
            }
            else if (s.Equals("help", StringComparison.OrdinalIgnoreCase))
            {
                var msg = _smp.LogManager.Instance;
                msg.push(new AppMessage("======= COMMAND LIST =======", type_msg.CL_ONLY_CONSOLE));
                msg.push(new AppMessage("status                      - Mostra uso de memória, usuários e uptime.", type_msg.CL_ONLY_CONSOLE));
                msg.push(new AppMessage("clear / cls                 - Limpa o console.", type_msg.CL_ONLY_CONSOLE));
                msg.push(new AppMessage("reload_files                - Recarrega arquivos básicos do servidor.", type_msg.CL_ONLY_CONSOLE));
                 msg.push(new AppMessage("---------------------------------------------------", type_msg.CL_ONLY_CONSOLE));
                msg.push(new AppMessage("reload_system [Type]        - Tipos: iff.", type_msg.CL_ONLY_CONSOLE));
                msg.push(new AppMessage("===================================================", type_msg.CL_ONLY_CONSOLE));
            }
            return false;
        }

        protected override void OnClientConnected(IAppSession session)
        {
            if (session is not Player player)
            {
                Console.WriteLine($"[Erro] A sessão conectada não é do Type Player! Tipo real: {session.GetType().Name}");
                return;
            }

            try
            {
                var packet = new Packet(0x1388);
                packet.WriteInt32(player._ParseKey);
                packet.WriteByte(5);
                packet.WriteString(UtilTime.formatDateLocal(0));
                player.Send(packet, true);
                _smp.LogManager.Instance.push(new AppMessage($"[{GetType().Name}::OnClientConnected][Sucess] PLAYER[IP: {player.GetIP()}, OID: {player.ConnectionID}", 0));
            }
            catch (exception ex)
            {
                _smp.LogManager.Instance.push(new AppMessage(
              $"[RankingService.OnClientConnected][ErrorSt]: {ex.getFullMessageError()}",
              type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        protected override void OnClientDisconnected(IAppSession session)
        {
            if (session == null)
                throw new exception("[RankingService::OnClientDisconnected][Error] _session is nullptr.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MESSAGE_SERVER, 60, 0));

            Player p = (Player)session;
            try
            {
                _smp.LogManager.Instance.push(new AppMessage($"[{GetType().Name}::OnClientDisconnected][Warning] PLAYER[ID: {p.UserInfo?.Login} UID: {p.UserInfo?.UID}]", type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[RankingService::OnClientDisconnecteded][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

        }

        protected override bool CheckPacket(IAppSession session, Packet packet)
        {
            if (packet == null) return false;

            var Type = (PacketIDClient)packet.Type;
            if (Type == PacketIDClient.CLIENT_REQUEST_LOGIN || Type == PacketIDClient.CLIENT_REQUEST_PLAYER_INFO)
                return true;

            if (!session.Authorized)
                return false;

            switch (Type)
            { 
                case PacketIDClient.CLIENT_REQ_SEARCH_PLAYER_IN_RANKING:
                case PacketIDClient.CLIENT_UNKNOWN3:
                case PacketIDClient.CLIENT_UNKNOWN4:
                case PacketIDClient.CLIENT_UNKNOWN5:
                    return true; 
            }

            return false;
        }

        protected override void OnHeartBeat()
        {
            try
            {
                // Server ainda n�o est� totalmente iniciado
                if (!IsRunning)
                    return;

                if (!sRankRegistryManager.Instance.isLoad())
                    sRankRegistryManager.Instance.load(); // Carrega os registros do Rank

                if (IsRunning && m_sync_update_time_refresh == 0 && m_refresh_time.isOutDated())
                {

                    // Trava update check, para n�o ficar enviando varias requisi��o para atualizar os registro para o banco de dados
                    m_sync_update_time_refresh = 1;

                    // Envia a requisi��o para o banco de dados
                    snmdb.NormalManagerDB.Instance.add(1,
                     new CmdUpdateRankRegistry(),
                     DBResponse,
                     this);
                }
                OnStart();
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[RankingService::onHeartBeat][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        protected override void OnStart()
        {
            Console.Title = $"Ranking Service - P: {m_si.CurrentUsers}, Auth: {(m_unit_connect != null && m_unit_connect.isLive()? "ON": "OFF")}";
        }

        public override async void LoadConfig()
        {
            base.LoadConfig();
            // Server Tipo
            m_si.Type = 4/*Auth Server*/;
            // Carrega a configura��o do Rank
            CmdRankConfigInfo cmd_rci = new CmdRankConfigInfo(); // Waiter

            snmdb.NormalManagerDB.Instance.add(0, cmd_rci);

            if (cmd_rci.getException().getCodeError() != 0)
            {
                throw cmd_rci.getException();
            }

            m_refresh_time = cmd_rci.getInfo();
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

                    sRankRegistryManager.Instance.load();

                    // Cria arquivo de log, com todos os registros
                    sRankRegistryManager.Instance.makeLog();

                    // Libera o HearBeat para verificar de novo quando tempo vai acabar
                    m_sync_update_time_refresh = 0;
                }
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[RankService::updateTimeRefresh][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        protected void ReloadFiles()
        {
            LoadConfig();

            sIff.Instance.reload();
        }

        public override void authCmdShutdown(int _time_sec)
        {
            //base.authCmdShutdown(_time_sec);
        }

        public override void authCmdBroadcastNotice(string _notice)
        {
            //base.authCmdBroadcastNotice(_notice);
        }

        public override void authCmdBroadcastTicker(string _nickname, string _msg)
        {
            //base.authCmdBroadcastTicker(_nickname, _msg);
        }

        public override void authCmdBroadcastCubeWinRare(string _msg, uint _option)
        {
            //base.authCmdBroadcastCubeWinRare(_msg, _option);
        }

        public override void authCmdDisconnectPlayer(uint _req_server_uid, uint _player_uid, byte _force)
        {
            try
            {

                var s = _playerManager.FindPlayer(_player_uid);

                if (s != null)
                {

                    // Log
                    _smp.LogManager.Instance.push(new AppMessage("[RankingServer::authCmdDisconnectPlayer][log] Comando do Auth Server, Server[UID: " + (_req_server_uid)
                            + "] pediu para desconectar o Player[UID: " + (s.UserInfo.UID) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                    // Deconecta o Player
                    OnClientDisconnected(s);

                    // UPDATE ON Auth Server
                    m_unit_connect.SendConfirmDisconnectPlayer(_req_server_uid, _player_uid);

                }
                else
                    _smp.LogManager.Instance.push(new AppMessage("[RankingServer::authCmdDisconnectPlayer][WARNING] Comando do Auth Server, Server[UID: " + (_req_server_uid)
                            + "] pediu para desconectar o Player[UID: " + (_player_uid) + "], mas nao encontrou ele no server.", type_msg.CL_FILE_LOG_AND_CONSOLE));

            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[RankingServer::authCmdDisconnectPlayer][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void authCmdConfirmDisconnectPlayer(uint _player_uid)
        {
            //base.authCmdConfirmDisconnectPlayer(_player_uid);
        }

        public override void authCmdNewMailArrivedMailBox(uint _player_uid, int _mail_id)
        {
            //base.authCmdNewMailArrivedMailBox(_player_uid, _mail_id);
        }

        public override void authCmdNewRate(uint _tipo, uint _qntd)
        {
            //base.authCmdNewRate(_tipo, _qntd);
        }

        public override void authCmdReloadGlobalSystem(uint _tipo)
        {
            //base.authCmdReloadGlobalSystem(_tipo);
        }

        public override void authCmdInfoPlayerOnline(uint _req_server_uid, uint _player_uid)
        {
            //base.authCmdInfoPlayerOnline(_req_server_uid, _player_uid);
        }

        public override void authCmdConfirmSendInfoPlayerOnline(uint _req_server_uid, AuthServerPlayerInfo _aspi)
        {
            try
            {

                var s = _playerManager.FindPlayer(_aspi.uid);

                if (s != null)
                { 
                    ConfirmLoginOnOtherServer(s, _req_server_uid, _aspi); 
                }
                else
                    _smp.LogManager.Instance.push(new AppMessage("[RankingServer::authCmdConfirmSendInfoPlayerOnline][WARNING] Player[UID: " + (_aspi.uid)
                            + "] retorno do confirma login com Auth Server do Server[UID: " + (_req_server_uid) + "], mas o palyer nao esta mais conectado.", type_msg.CL_FILE_LOG_AND_CONSOLE));

            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[RankingServer::authCmdConfirmSendInfoPlayerOnline][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void authCmdSendCommandToOtherServer(Packet _packet)
        {
            //base.authCmdSendCommandToOtherServer(_packet);
        }

        public override void authCmdSendReplyToOtherServer(Packet _packet)
        {
            //base.authCmdSendReplyToOtherServer(_packet);
        }

        public override void sendCommandToOtherServerWithAuthServer(Packet _packet, uint _send_server_uid_or_type)
        {
            //base.sendCommandToOtherServerWithAuthServer(_packet, _send_server_uid_or_type);
        }

        public override void sendReplyToOtherServerWithAuthServer(Packet _packet, uint _send_server_uid_or_type)
        {
            //base.sendReplyToOtherServerWithAuthServer(_packet, _send_server_uid_or_type);
        }

        public override List<Player> FindAllGM()
        {
            return base.FindAllGM();
        }
         
        public override List<Player> FindAllSessionByUid(uint uid)
        {
            return _playerManager.FindAllSessionByUid(uid);
        }

        public override IAppSession FindSessionByNickname(string nickname)
        {
            return base.FindSessionByNickname(nickname);
        }

        public Player FindPlayer(uint member_uid)
        {
            return _playerManager.FindPlayer(member_uid, false);
        }

        public void ConfirmLoginOnOtherServer(Player _session, uint _req_server_uid, AuthServerPlayerInfo _aspi)
        {

            var p = new Packet();

            try
            {

                if (_aspi.uid != _session.UserInfo.UID)
                {
                    throw new exception("[RankService::confirmLoginOnOtherServer][Error] Player[UID=" + Convert.ToString(_session.UserInfo.UID) + ", REQ_UID=" + Convert.ToString(_aspi.uid) + ", REQ_SERVER=" + Convert.ToString(_req_server_uid) + "] request Info player, mas nao eh o mesmo UID que foi retornado do request com o Auth Server. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.RANK_SERVER,
                        1, 0x5200201));
                }

                if (_aspi.option != 1)
                {
                    throw new exception("[RankService::confirmLoginOnOtherServer][Error] Player[UID=" + Convert.ToString(_session.UserInfo.UID) + ", REQ_UID=" + Convert.ToString(_aspi.uid) + ", REQ_SERVER=" + Convert.ToString(_req_server_uid) + "] request Info player, mas nao esta online no outro server.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.RANK_SERVER,
                        2, 0x5200202));
                }

                if (_aspi.id.CompareTo(_session.UserInfo.Login) != 0)
                {
                    throw new exception("[RankService::confirmLoginOnOtherServer][Error] Player[UID=" + Convert.ToString(_session.UserInfo.UID) + ", REQ_UID=" + Convert.ToString(_aspi.uid) + ", REQ_SERVER=" + Convert.ToString(_req_server_uid) + "] request Info player, mas nao eh o mesmo ID[ID=" + _session.UserInfo.Login + ", REQ_ID=" + _aspi.id + "] que foi retornado do request com o Auth Server.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.RANK_SERVER,
                        3, 0x5200203));
                }

                // Confirm Login com sucesso, Atualiza o cliente
                _session.UserInfo.m_state = 4;

                // Authorized a ficar online no server por tempo indeterminado
                _session.Authorized = true;

                // Resposta para o Pedido de Login
                SendFirstPage(_session, 0); 

                _smp.LogManager.Instance.push(new AppMessage($"[ConfirmLoginOnOtherServer][Log] PLAYER[UID: {_session.UserInfo.UID}, NICK: {_session.UserInfo.NickName}] SUCESS.", type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
            catch (exception e)
            {
                // Resposta
                SendFirstPage(_session, 1);

                _smp.LogManager.Instance.push(new AppMessage("[RankService::confirmLoginOnOtherServer][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        private void SendFirstPage(Player _session, int _option)
        {
            var p = new Packet(0x1389);

            if (_option != 0)
            {
                p.WriteByte((byte)_option);

                p.WriteZero(14);
            }
            else
            {

                p.WriteByte(_option);

                p.WriteByte(_session.UserInfo.m_sd.rank_menu);
                p.WriteByte(_session.UserInfo.m_sd.rank_menu_item);
                p.WriteByte(_session.UserInfo.m_sd.term_s5_type);
                p.WriteByte(_session.UserInfo.m_sd.class_type);

                sRankRegistryManager.Instance.pageToPacket(p, _session.UserInfo.m_sd);

                if (_session.UserInfo.m_sd.active > 0)
                {
                    sRankRegistryManager.Instance.playerPositionToPacket(p,
                        _session, _session.UserInfo.m_sd);
                }
                else
                {
                    p.WriteByte(Player_Pos_Rank_Type.PPRT_NOT_TOP_RANK);
                }
            }

            _session.Send(p);
        }


        protected override void DBResponse(int _msg_id, Pangya_DB _pangya_db, object _arg)
        {
            if (_arg == null)
            {
                _smp.LogManager.Instance.push(new AppMessage("[RankService::SQLDBResponse][WARNING] _arg is nullptr, na msg_id = " + Convert.ToString(_msg_id), type_msg.CL_FILE_LOG_AND_CONSOLE));
                return;
            }

            // Por Hora s� sai, depois fa�o outro Type de tratamento se precisar
            if (_pangya_db.getException().getCodeError() != 0)
            {
                _smp.LogManager.Instance.push(new AppMessage("[RankService::SQLDBResponse][Error] " + _pangya_db.getException().getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                return;
            }

            switch (_msg_id)
            {
                case 1: // Update Rank Registros
                    {
                        var cmd_urr = (CmdUpdateRankRegistry)(_pangya_db);

                        if (cmd_urr.getException().getCodeError() != 0)
                        {

                            // Exception print no console
                            _smp.LogManager.Instance.push(new AppMessage("[RankService::SQLDBResponse][Error] " + cmd_urr.getException().getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                            // Liberar o verificador no HearBeat
                            updateTimeRefresh(0u, DateTime.Now);

                        }
                        else
                        {
                            updateTimeRefresh(cmd_urr.getRetState(), cmd_urr.getDate());
                        }

                        break;
                    }
                case 0:
                default:
                    break;
            }
        }
    }

    public class RankingServer : Singleton<RankingService>
    { }
}