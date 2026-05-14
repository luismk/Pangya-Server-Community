using Pangya_GameServer.Game;
using Pangya_GameServer.Game.Manager;
using Pangya_GameServer.Game.System;
using Pangya_GameServer.Models;
using Pangya_GameServer.Models.golden_time_type;
using Pangya_GameServer.PacketFunc;
using Pangya_GameServer.PangyaEnums;
using Pangya_GameServer.Repository;
using PangyaAPI.IFF.JP.Extensions;
using PangyaAPI.Network.Models;
using PangyaAPI.Network.PangyaPacket;
using PangyaAPI.Network.PangyaServer;
using PangyaAPI.Network.PangyaSession;
using PangyaAPI.Network.PangyaUtil;
using PangyaAPI.Network.Repository;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Models;
using PangyaAPI.Utilities.Log;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using static Pangya_GameServer.Models.DefineConstants;
//using PangyaAPI.Discord;
namespace Pangya_GameServer.GameServiceTcp
{
    public class GameService : Server
    {
        public int m_access_flag { get; private set; }
        public int m_create_user_flag { get; private set; }
        public int m_same_id_login_flag { get; private set; }
        DailyQuestInfo m_dqi;
        protected List<Channel> v_channel;
        public BroadcastManager m_ticker = new BroadcastManager(30/*30 segundos para o ticker*/);
        public BroadcastManager m_notice = new BroadcastManager(60/*60 segundos 1 minuto para o notice*/);
        static PlayerManager m_player_manager = new PlayerManager();
        LoginManager m_login_manager;
        private bool m_active_room_log;

        public GameService() : base(m_player_manager)
        {
            // Inicializa config do Game Server
            config_init();
            // init Request Client packets
            init_Packets();
            //init create/load channels
            init_load_channels();
            // Inicializa os sistemas Globais
            init_systems();
            // Initialized complete
            m_state = ServerState.Initialized;

            //deixa todo mundo offline...
            snmdb.NormalManagerDB.getInstance().add(0, new CmdPlayerLogout((uint)m_si.uid), null, null);
        }


        public override void config_init()
        {
            base.config_init();

            // Server Tipo
            m_si.tipo = 1;

            m_si.img_no = m_reader_ini.ReadInt16("SERVERINFO", "ICONINDEX");
            m_si.rate.exp = (short)m_reader_ini.readInt("SERVERINFO", "EXPRATE");
            m_si.rate.scratchy = (short)m_reader_ini.readInt("SERVERINFO", "SCRATCHY_RATE");
            m_si.rate.pang = (short)m_reader_ini.readInt("SERVERINFO", "PANGRATE");
            m_si.rate.club_mastery = (short)m_reader_ini.readInt("SERVERINFO", "CLUBMASTERYRATE");
            m_si.rate.papel_shop_rare_item = (short)m_reader_ini.readInt("SERVERINFO", "PAPEL_rate_RATE");
            m_si.rate.papel_shop_cookie_item = (short)m_reader_ini.readInt("SERVERINFO", "PAPEL_COOKIE_ITEM_RATE");
            m_si.rate.treasure = (short)m_reader_ini.readInt("SERVERINFO", "TREASURE_RATE");
            m_si.rate.memorial_shop = (short)m_reader_ini.readInt("SERVERINFO", "MEMORIAL_RATE");
            m_si.rate.chuva = (short)m_reader_ini.readInt("SERVERINFO", "CHUVA_RATE");
            m_si.rate.grand_zodiac_event_time = (short)(m_reader_ini.readInt("SERVERINFO", "GZ_EVENT") >= 1 ? 1 : 0);// Ativo por padrão
            m_si.rate.grand_prix_event = (short)(m_reader_ini.readInt("SERVERINFO", "GP_EVENT") >= 1 ? 1 : 0);// Ativo por padrão
            m_si.rate.golden_time_event = ((short)(m_reader_ini.readInt("SERVERINFO", "GOLDEN_TIME_EVENT") >= 1 ? 1 : 0));// Ativo por padrão
            m_si.rate.login_reward_event = ((short)(m_reader_ini.readInt("SERVERINFO", "LOGIN_REWARD") >= 1 ? 1 : 0));// Ativo por padrão
            m_si.rate.bot_gm_event = ((short)(m_reader_ini.readInt("SERVERINFO", "BOT_GM_EVENT") >= 1 ? 1 : 0));// Ativo por padrão
            m_si.rate.smart_calculator = (/*m_reader_ini.readInt("SERVERINFO", "SMART_CALC") >= 1 ? true :*/ 0);// Atibo por padrão
            m_si.rate.angel_event = ((short)(m_reader_ini.readInt("SERVERINFO", "ANGEL_EVENT") >= 1 ? 1 : 0));// Atibo por padrão
            m_active_room_log = (m_reader_ini.readInt("LOG", "ACTIVE_ROOM_LOG") >= 1 ? true : false);// Atibo por padrão

            try
            {
                m_si.flag.ullFlag = m_reader_ini.ReadUInt64("SERVERINFO", "FLAG");

                m_active_room_log = (m_reader_ini.readInt("LOG", "ACTIVE_ROOM_LOG") >= 1 ? true : false);// Atibo por padrão

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[GameService::config_init][ErrorSystem] Config.FLAG" + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }


            // Recupera Valores de rate do gs do banco de dados
            var cmd_rci = new CmdRateConfigInfo(m_si.uid);  // Waiter

            snmdb.NormalManagerDB.getInstance().add(0, cmd_rci, SQLDBResponse, this);


            if (cmd_rci.getInfo() != null)
            {

                if (cmd_rci.getException().getCodeError() != 0)
                    _smp.message_pool.getInstance().push(new message("[GameService::config_init][ErrorSystem] " + cmd_rci.getException().getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));


                setAngelEvent(m_si.rate.angel_event);
                setRatePang(m_si.rate.pang);
                setRateExp(m_si.rate.exp);
                setRateClubMastery(m_si.rate.club_mastery);

                snmdb.NormalManagerDB.getInstance().add(8, new CmdUpdateRateConfigInfo(m_si.uid, m_si.rate), SQLDBResponse, this);
            }
            else
            {   // Conseguiu recuperar com sucesso os valores do gs

                setAngelEvent(m_si.rate.angel_event);
                setRatePang(m_si.rate.pang);
                setRateExp(m_si.rate.exp);
                setRateClubMastery(m_si.rate.club_mastery);
            }
            m_si.app_rate = 100;    // Esse aqui nunca usei, deixei por que no DB do s4 tinha só cópiei
        }

        public bool getAccessFlag()
        {
            return m_access_flag == 1;
        }

        public bool getCreateUserFlag()
        {
            return m_create_user_flag == 1;
        }

        public bool canSameIDLogin()
        {
            return m_same_id_login_flag == 1;
        }

        // Set Event Server
        private void setAngelEvent(short _angel_event)
        {
            // Evento para reduzir o quit rate, diminui 1 quit a cada jogo concluído
            m_si.event_flag.angel_wing = _angel_event > 0;
            // Update rate Pang
            m_si.rate.angel_event = _angel_event; //precisa fazer isso, pois pode querer desativar
        }

        private void setRatePang(short _pang)
        {
            // Update Flag Event
            m_si.event_flag.pang_x_plus = (_pang >= 200) ? true : false;

            // Update rate Pang
            m_si.rate.pang = _pang;
        }

        private void setRateExp(short _exp)
        {// Reseta flag antes de atualizar ela 
            m_si.event_flag.exp_x2 = m_si.event_flag.exp_x_plus = false;

            // Update Flag Event
            if (_exp > 200)
                m_si.event_flag.exp_x_plus = true;
            else if (_exp == 200)
                m_si.event_flag.exp_x2 = true;
            else
                m_si.event_flag.exp_x2 = m_si.event_flag.exp_x_plus = false;

            // Update rate Experiência
            m_si.rate.exp = _exp;
        }

        private void setRateClubMastery(short _club_mastery)
        {
            // Update Flag Event
            m_si.event_flag.club_mastery_x_plus = (_club_mastery >= 200) ? true : false;

            // Update rate Club Mastery
            m_si.rate.club_mastery = _club_mastery;
        }

        public override void OnHeartBeat()
        {
            try
            {
                // Server ainda não está totalmente iniciado
                if (!this._isRunning)
                    return;

                // Check Invite Time Channels
                foreach (var el in v_channel)
                    el.startInviteTime();

                // Begin Check System Singleton Static
                // Carrega IFF_STRUCT
                if (!sIff.getInstance().isLoad())
                    sIff.getInstance().initilation();

                //// Map Dados Estáticos
                if (!MapSystem.getInstance().isLoad())
                    MapSystem.getInstance().load();

                // Carrega Card System
                if (!sCardSystem.getInstance().isLoad())
                    sCardSystem.getInstance().load();

                //// Carrega Comet Refill System
                if (!sCometRefillSystem.getInstance().isLoad())
                    sCometRefillSystem.getInstance().load();

                // Carrega Papel Shop System
                if (!sPapelShopSystem.getInstance().isLoad())
                    sPapelShopSystem.getInstance().load();

                //// Carrega Box System
                if (!sBoxSystem.getInstance().isLoad())
                    sBoxSystem.getInstance().load();

                //// Carrega Memorial System
                if (!sMemorialSystem.getInstance().isLoad())
                    sMemorialSystem.getInstance().load();

                //// Carrega Cube Coin System(SobreCarga)
                if (!sCubeCoinSystem.getInstance().isLoad())
                    sCubeCoinSystem.getInstance().load();

                //// Treasure Hunter System
                if (!sTreasureHunterSystem.getInstance().isLoad())
                    sTreasureHunterSystem.getInstance().load();

                //// Drop System
                if (!sDropSystem.getInstance().isLoad())
                    sDropSystem.getInstance().load();

                // Attendance Reward System
                if (!sAttendanceRewardSystem.getInstance().isLoad())
                    sAttendanceRewardSystem.getInstance().load();

                //// Approach Mission
                if (!sApproachMissionSystem.getInstance().isLoad())
                    sApproachMissionSystem.getInstance().load();

                //// Grand Zodiac Event
                if (!sGrandZodiacEvent.getInstance().isLoad())
                    sGrandZodiacEvent.getInstance().load();

                //// Coin Cube Location System
                if (!sCoinCubeLocationUpdateSystem.getInstance().isLoad())
                    sCoinCubeLocationUpdateSystem.getInstance().load();

                //// Golden Time System
                if (!sGoldenTimeSystem.getInstance().isLoad())
                    sGoldenTimeSystem.getInstance().load();

                //// Login Reward System
                if (!sLoginRewardSystem.getInstance().isLoad())
                    sLoginRewardSystem.getInstance().load();

                //// Carrega Smart Calculator Lib, Só inicializa se ele estiver ativado
                //if (m_si.rate.smart_calculator && !sSmartCalculator.getInstance().hasStopped() && !sSmartCalculator.getInstance().isLoad())
                //    sSmartCalculator.getInstance().load();

                //// End Check System Singleton Static

                //// check Grand Zodiac Event Time
                if (m_si.rate.grand_zodiac_event_time == 1 && sGrandZodiacEvent.getInstance().checkTimeToMakeRoom())
                    makeGrandZodiacEventRoom();

                ////// check Bot GM Event Time
                if (m_si.rate.bot_gm_event == 1 && sBotGMEvent.getInstance().checkTimeToMakeRoom())
                    makeBotGMEventRoom();


                //// check Golden Time Round Update
                if (m_si.rate.golden_time_event == 1 && sGoldenTimeSystem.getInstance().checkRound())
                    makeListOfPlayersToGoldenTime();

                //// update Login Reward
                if (m_si.rate.login_reward_event == 1)
                    sLoginRewardSystem.getInstance().updateLoginReward();

                //// Check Daily Quest
                if (DailyQuestManager.checkCurrentQuest(m_dqi) && m_dqi != null)
                    DailyQuestManager.updateDailyQuest(ref m_dqi);  // Atualiza daily quest

                //// Check Update Dia do Papel Shop System
                if (sPapelShopSystem.getInstance().isLoad())
                    sPapelShopSystem.getInstance().updateDia();
                 
                if (sTreasureHunterSystem.getInstance().checkUpdateTimePointCourse())
                    foreach (var el in v_channel)
                        packet_func.channel_broadcast(el, packet_func.pacote131(), 1);
                // End Check Treasure Hunter

                // Check Notice (GM or Cube Win Rare)
                BroadcastManager.RetNoticeCtx rt = m_notice.peek();

                PangyaBinaryWriter p = new PangyaBinaryWriter();

                if (rt.ret == BroadcastManager.RET_TYPE.OK)
                {
                    if (rt.nc.type == BroadcastManager.TYPE.GM_NOTICE)
                    {    // GM Notice

                        p.init_plain(0x42);

                        p.WriteString(rt.nc.notice);

                    }
                    else if (rt.nc.type == BroadcastManager.TYPE.CUBE_WIN_RARE)
                    {   // Cube Win Rare Notice

                        p.init_plain(0x1D3);

                        p.WriteUInt32(1);             // Count


                        p.WriteUInt32(rt.nc.option);
                        p.WriteString(rt.nc.notice);

                    }

                    // Broadcast to All Channels
                    foreach (var el in v_channel)
                        packet_func.channel_broadcast(el, p, 1);
                }

                //// Check Ticker
                rt = m_ticker.peek();

                if (rt.ret == BroadcastManager.RET_TYPE.OK && rt.nc.type == BroadcastManager.TYPE.TICKER)
                {   // Ticker Msg

                    p.init_plain(0xC9);
                    p.WriteString(rt.nc.nickname);
                    p.WriteString(rt.nc.notice);

                    // Broadcast to All Channels
                    foreach (var el in v_channel)
                        packet_func.channel_broadcast(el, p, 1);
                }

                m_player_manager.checkPlayersItens();
            }
            catch (exception e)
            {
                _smp.message_pool.getInstance().push(new message("[GameService::onHeartBeat][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public new void SQLDBResponse(int _msg_id, Pangya_DB _pangya_db, object _arg)
        {

            if (_arg == null)
            {
                _smp.message_pool.getInstance().push(new message("[GameService::SQLDBResponse][Error] _arg is null na msg_id = " + (_msg_id), type_msg.CL_FILE_LOG_AND_CONSOLE));
                return;
            }

            // Por Hora só sai, depois faço outro tipo de tratamento se precisar
            if (_pangya_db.getException().getCodeError() != 0)
                throw new exception("[GameService::SQLDBResponse][Error] " + _pangya_db.getException().getFullMessageError());

            var gs = (GameService)(_arg);

            switch (_msg_id)
            {
                case 1: // DailyQuest Info
                    {
                        var m_dqi = ((CmdDailyQuestInfo)_pangya_db).getInfo();  // cmd_dqi.getInfo();

                        // Atualiza daily quest
                        if (DailyQuestManager.checkCurrentQuest(m_dqi))
                        {
                            if (m_dqi != null)
                                DailyQuestManager.updateDailyQuest(ref m_dqi);

                            Thread.Sleep(100);  // Espera 100 milli segundo

                            snmdb.NormalManagerDB.getInstance().add(1, new CmdDailyQuestInfo(), SQLDBResponse, _arg);

                        }
                        break;
                    }
                case 2: // Atualiza DailyQuest Info do server
                    {
                        // Atualiza daily quest
                        getDailyQuestInfo = ((CmdDailyQuestInfo)_pangya_db).getInfo(); // cmd_dqi.getInfo();
                        break;
                    }
                case 3: // Atualiza Chat Macro User
                    {
                        break;
                    }
                case 4: // Insert Msg Off
                    {
                        break;
                    }
                case 5: // Register Player Logon ON DB, 0 Login, 1 Logout
                    {
                        // Não usa por que é um UPDATE
                        break;
                    }
                case 6: // Insert Ticker no DB
                    {
                        break;
                    }
                case 7: // Register Logon do player no Server
                    {
                        // Não usa por que é um update
                        break;
                    }
                case 8: // Update Server Rate Config Info
                    {
                        break;
                    }
                case 9:     // Insert Block IP
                    {
                        break;
                    }
                case 10:    // Insert Block MAC
                    {
                        break;
                    }
                case 0:
                default:
                    break;
            }
        }

        public void destroyRoom(byte _channel_owner, short _number)
        {
            try
            {

                var c = findChannel(_channel_owner);

                if (c != null)
                    c.destroyRoom(_number);

            }
            catch (exception e)
            {
                _smp.message_pool.getInstance().push(new message("[GameService::destroyRoom][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void sendServerListAndChannelListToSession(Player _session)
        {
            packet_func.session_send(packet_func.pacote09F(m_server_list, v_channel), _session);
        }

        public void sendDateTimeToSession(Player _session)
        {
            using (var p = new PangyaBinaryWriter((ushort)0xBA))
            {
                p.WriteTime();
                packet_func.session_send(p, _session);
            }
        }

        public void sendRankServer(Player _session)
        {

            try
            {

                if (_session.m_pi.block_flag.m_flag.rank_server)
                    throw new exception("[GameService::sendRankServer][Error] PLAYER[UID=" + (_session.m_pi.uid)
                            + "] esta bloqueado o Rank Server, ele nao pode acessar o rank server.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 7010, 0));

                var cmd_sl = new CmdServerList(TYPE_SERVER.RANK);   // Waiter

                snmdb.NormalManagerDB.getInstance().add(0, cmd_sl, null, null);

                if (cmd_sl.getException().getCodeError() != 0)
                    throw cmd_sl.getException();

                var sl = cmd_sl.getServerList();

                if (sl.Count == 0)
                    throw new exception("[GameService::sendRankServer][Warning] PLAYER[UID=" + (_session.m_pi.uid)
                            + "] requisitou o Rank Server, mas nao tem nenhum Rank Server online no DB.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 7011, 0));

                using (var p = new PangyaBinaryWriter(0xA2))
                {
                    p.WritePStr(sl[0].ip);
                    p.WriteInt32(sl[0].port);
                    packet_func.session_send(p, _session);
                }


            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[GameService::sendRankServer][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                using (var p = new PangyaBinaryWriter(0xA2))
                {
                    // Erro manda tudo 0
                    p.WriteUInt16(0);  // String IP
                    p.WriteUInt32(0);  // Port
                    packet_func.session_send(p, _session);
                }
            }
        }

        public Channel findChannel(byte _channel)
        {
            if (_channel == 255)
                return null;

            for (var i = 0; i < v_channel.Count; ++i)
                if (v_channel[i].getId() == _channel)
                    return v_channel[i];

            return null;
        }

        public Channel findChannel(Channel _channel)
        {
            if (_channel == null)
                return null;

            for (var i = 0; i < v_channel.Count; ++i)
                if (v_channel[i].getId() == _channel.getId())
                    return v_channel[i];

            return null;
        }

        public Player findPlayer(uint _uid, bool _oid = false)
        {
            return m_player_manager.findPlayer(_uid, _oid);
        }

        public void blockOID(int _oid) { m_player_manager.blockOID(_oid); }

        public void unblockOID(int _oid) { m_player_manager.unblockOID(_oid); }

        public DailyQuestInfo getDailyQuestInfo { get => m_dqi; set => m_dqi = value; }


        // Update Daily Quest Info
        public void updateDailyQuest(DailyQuestInfo _dqi)
        {
            if (_dqi != null)
                m_dqi = _dqi;
        }

        // send Update Room Info, find room nos canais e atualiza o info
        public void sendUpdateRoomInfo(Room _r, int _option)
        {
            try
            {

                if (_r != null)
                {

                    var c = findChannel(_r.getChannelOwenerId());

                    if (c != null)
                        c.sendUpdateRoomInfo(_r.getInfo(), _option);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[GameService::sendUpdateRoomInfo][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
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
                    _smp.message_pool.getInstance().push(new message("[RankingServer::authCmdShutdown][Error] Auth Server requisitou para o server ser desligado em "
                            + _time_sec + " segundos", type_msg.CL_FILE_LOG_AND_CONSOLE));

                    shutdown_time(_time_sec);

                }
                else
                    _smp.message_pool.getInstance().push(new message("[RankingServer::authCmdShutdown][Warning] Auth Server requisitou para o server ser delisgado em "
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

            try
            {
                m_notice.push_back(0, _notice, BroadcastManager.TYPE.GM_NOTICE);
            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[GameService::authCmdBroadcastNotice][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void authCmdBroadcastTicker(string _nickname, string _msg)
        {

            try
            {
                m_ticker.push_back(0, _nickname, _msg, BroadcastManager.TYPE.TICKER);
            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[GameService::authCmdBroadcastTicker][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void authCmdBroadcastCubeWinRare(string _msg, uint _option)
        {

            try
            {
                m_notice.push_back(0, _msg, _option, BroadcastManager.TYPE.CUBE_WIN_RARE);
            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[GameService::authCmdBroadcastCubeWinRare][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void authCmdDisconnectPlayer(uint _req_server_uid, uint _player_uid, byte _force)
        {

            try
            {

                var s = findPlayer(_player_uid);

                if (s != null)
                {
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

                        // Só desconecta aqui se a flag do server de poder logar com o mesmo id estiver desativada
                        if (!(same_id_login == 1))
                            DisconnectSession(s);
                    }

                }
                else
                {

                    // Não encontrou o player no server, então desconecta no banco de dados
                    snmdb.NormalManagerDB.getInstance().add(5, new CmdRegisterLogon(_player_uid, 1/*Logout*/), SQLDBResponse, this);

                    // Log
                    //_smp.message_pool.getInstance().push(new message("[GameService::authCmdDisconnectPlayer][Warning] Comando do Auth Server, Server[UID=" + (_req_server_uid)
                    //        + "] pediu para desconectar o PLAYER[UID=" + (_player_uid) + "], mas nao encontrou ele no server, entao desconecta ele no banco de dados.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                }

                // UPDATE ON Auth Server
                m_unit_connect.sendConfirmDisconnectPlayer(_req_server_uid, _player_uid);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[GameService::authCmdDisconnectPlayer][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void authCmdConfirmDisconnectPlayer(uint _player_uid)
        {
            // Game Server não usa esse Comando
            return;
        }

        public override void authCmdNewMailArrivedMailBox(uint _player_uid, int _mail_id)
        {

            try
            {

                var s = findPlayer(_player_uid);

                if (s == null)
                {
                    return;
                }
                if (_player_uid <= 0)
                {
                    return;
                }

                s.m_pi.m_mail_box.addNewEmailArrived(_player_uid, _mail_id);

                var v_mi = s.m_pi.m_mail_box.getAllUnreadEmail();

                if (v_mi.empty())
                    throw new exception("[GameService::authCmdNewMailArrivedMailBox][Error] Auth Server Comando New Mail[ID=" + (_mail_id)
                            + "] Arrived no Mailbox do PLAYER[UID=" + (_player_uid) + "], mas nao tem nenhum email nao lido no Mailbox dele.",
                           ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 131, 0));

                // UPDATE ON GAME
                var p = new PangyaBinaryWriter(0x210);

                p.WriteUInt32(0);   // OK

                p.WriteInt32(v_mi.Count);   // Count

                foreach (var el in v_mi)
                    p.WriteBytes(el.ToArray());

                packet_func.session_send(p, s, 1);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[GameService::authCmdNewMailArrivedMailBox][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void authCmdNewRate(uint _tipo, uint _qntd)
        {

            try
            {

                updateRateAndEvent((int)_tipo, _qntd);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[GameService::authCmdNewRate][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
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

                _smp.message_pool.getInstance().push(new message("[GameService::authCmdReloadGlobalSystem][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void authCmdConfirmSendInfoPlayerOnline(uint _req_server_uid, AuthServerPlayerInfo _aspi)
        { 
            try
            {

                var s = findPlayer(_aspi.uid);

                if (s != null)
                {
                    _aspi.id = s.m_pi.id;
                    _aspi.ip = s.getIP();
                    _aspi.option = 1; 
                    if (m_unit_connect.isLive())
                    {
                        _smp.message_pool.getInstance().push(new message($"[GameService] Player[UID={_aspi.uid}] confirmado no Server[UID={_req_server_uid}]", type_msg.CL_ONLY_CONSOLE));
                        m_unit_connect.sendInfoPlayerOnline((uint)m_si.uid, _aspi);
                    }
                }
                else
                    _smp.message_pool.getInstance().push(new message("[GameService::authCmdConfirmSendInfoPlayerOnline][Warning] PLAYER[UID=" + (_aspi.uid)
                            + "] retorno do confirma login com Auth Server do Server[UID=" + (_req_server_uid) + "], mas o palyer nao esta mais conectado.", type_msg.CL_FILE_LOG_AND_CONSOLE));

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[GameService::authCmdConfirmSendInfoPlayerOnline][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override bool CheckCommand(Queue<string> _command)
        {
            Console.ResetColor();

            if (_command.Count == 0)
            {
                _smp.message_pool.getInstance().push(new message("[GameService::CheckCommand][Error] Missing parameter", type_msg.CL_FILE_LOG_AND_CONSOLE));
                return true;
            }

            string s = _command.Dequeue();

            if (!string.IsNullOrEmpty(s) && s == "exit")
            {
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
                            _smp.message_pool.getInstance().push(new message($"[GameService::checkCommand][Error] Unknown Command: \"rate {sTipo}\"", type_msg.CL_FILE_LOG_AND_CONSOLE));
                            break;
                    }
                }
                else
                {
                    _smp.message_pool.getInstance().push(new message($"[GameService::checkCommand][Error] Unknown Command: \"rate {sTipo}\"", type_msg.CL_FILE_LOG_AND_CONSOLE));
                }

                if (tipo != -1 && tipo >= 0 && tipo <= 8)
                {
                    if (uint.TryParse(_command.Dequeue(), out uint qntd) && qntd > 0)
                    {
                        updateRateAndEvent(tipo, qntd);
                    }
                    else
                    {
                        _smp.message_pool.getInstance().push(new message($"[GameService::checkCommand][Error] Unknown value, Command: \"rate {sTipo}\"", type_msg.CL_FILE_LOG_AND_CONSOLE));
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
                            _smp.message_pool.getInstance().push(new message($"[GameService::checkCommand][Error] Unknown Comamnd: \"Event {s}\"", type_msg.CL_FILE_LOG_AND_CONSOLE));
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
                            _smp.message_pool.getInstance().push(new message($"[GameService::checkCommand][Error] Unknown Command: \"reload_system {sTipo}\"", type_msg.CL_FILE_LOG_AND_CONSOLE));
                            break;
                    }
                }
                else
                {
                    _smp.message_pool.getInstance().push(new message($"[GameService::checkCommand][Error] Unknown Command: \"reload_system {sTipo}\"", type_msg.CL_FILE_LOG_AND_CONSOLE));
                }

                if (tipo != -1 && tipo >= 0 && tipo <= 18)
                {
                    reloadGlobalSystem((uint)tipo);
                }
                return true;

            }
            else if (s == "notice")     // !@ Teste: Envia aviso para todos os canais
            {
                // Verifica se existe algum texto após o comando
                if (_command.Count == 0)
                {
                    // Opcional: enviar mensagem de erro para quem digitou o comando
                    return false;
                }

                var p = new PangyaBinaryWriter();

                // Une o restante da fila em uma única string (caso a mensagem tenha espaços)
                string mensagemCompleta = string.Join(" ", _command.ToArray());

                p.init_plain(0x42); // 0x42 costuma ser o ID de Notice/Mensagem do Sistema
                p.WriteString(mensagemCompleta);

                // Envia para todos os canais ativos
                foreach (var channel in v_channel)
                {
                    packet_func.channel_broadcast(channel, p, 1);
                }

                return true;
            }
            else if (s == "upt_coin_cube_location")		// !@ Teste
            {
                sCoinCubeLocationUpdateSystem.getInstance().forceUpdate();
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
                _smp.message_pool.getInstance().push(new message($"[GameService::CheckCommand][Error] Command No Exist-> {s}", type_msg.CL_FILE_LOG_AND_CONSOLE));
                return false;
            }
        }

        public void reload_files()
        {
            base.config_init();
            config_init();

            // Reload All Globals Systems
            reload_systems();

            _smp.message_pool.getInstance().push(new message("[game_server::reload_files][Log] Reload System now sucess!", type_msg.CL_FILE_LOG_AND_CONSOLE));

            // UPDATE ON GAME
            var p = new PangyaBinaryWriter(0xF9);

            p.WriteBytes(m_si.ToArray());

            foreach (var el in v_channel)
                packet_func.channel_broadcast(el, p, 1);
        }

        public void init_systems()
        {
            m_login_manager = new LoginManager();

            // SINCRONAR por que se não alguem pode pegar lixo de memória se ele ainda nao estiver inicializado
            var cmd_dqi = new CmdDailyQuestInfo();

            snmdb.NormalManagerDB.getInstance().add(1, cmd_dqi, SQLDBResponse, this);

            if (cmd_dqi.getException().getCodeError() != 0)
                throw new exception("[GameService::game_server][Error] nao conseguiu pegar o Daily Quest Info[Exption: "
                    + cmd_dqi.getException().getFullMessageError() + "]", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 277, 0));

            // Initialize Daily Quest of Server
            m_dqi = cmd_dqi.getInfo();

            // Carrega IFF_STRUCT
            if (!sIff.getInstance().isLoad())
                sIff.getInstance().initilation();


            //// Carrega Map Dados Estáticos
            if (!MapSystem.getInstance().isLoad())
                MapSystem.getInstance().load();

            //// Carrega Card System
            if (!sCardSystem.getInstance().isLoad())
                sCardSystem.getInstance().load();

            //// Carrega Comet Refill System
            if (!sCometRefillSystem.getInstance().isLoad())
                sCometRefillSystem.getInstance().load();

            // Carrega Papel Shop System
            if (!sPapelShopSystem.getInstance().isLoad())
                sPapelShopSystem.getInstance().load();

            //// Carrega Box System
            if (!sBoxSystem.getInstance().isLoad())
                sBoxSystem.getInstance().load();

            //// Carrega Memorial System
            if (!sMemorialSystem.getInstance().isLoad())
                sMemorialSystem.getInstance().load();

            //// Carrega Cube Coin System
            if (!sCubeCoinSystem.getInstance().isLoad())
                sCubeCoinSystem.getInstance().load();

            //// Carrega Treasure Hunter System
            if (!sTreasureHunterSystem.getInstance().isLoad())
                sTreasureHunterSystem.getInstance().load();

            //// Carrega Drop System
            if (!sDropSystem.getInstance().isLoad())
                sDropSystem.getInstance().load();

            // Carrega Attendance Reward System
            if (!sAttendanceRewardSystem.getInstance().isLoad())
                sAttendanceRewardSystem.getInstance().load();

            //// Carrega Approach Mission System
            if (!sApproachMissionSystem.getInstance().isLoad())
                sApproachMissionSystem.getInstance().load();

            //// Carrega Grand Zodiac Event System
            if (!sGrandZodiacEvent.getInstance().isLoad())
                sGrandZodiacEvent.getInstance().load();

            //// Carrega Coin Cube Location Update Syatem
            if (!sCoinCubeLocationUpdateSystem.getInstance().isLoad())
                sCoinCubeLocationUpdateSystem.getInstance().load();

            //// Carrega Golden Time System
            if (!sGoldenTimeSystem.getInstance().isLoad())
                sGoldenTimeSystem.getInstance().load();

            //// Carrega Login Reward System
            if (!sLoginRewardSystem.getInstance().isLoad())
                sLoginRewardSystem.getInstance().load();

            //// Carrega Bot GM Event
            if (!sBotGMEvent.getInstance().isLoad())
                sBotGMEvent.getInstance().load(); 

            //// Coloca aqui para ele não dá erro na hora de destruir o Room Grand Prix static instance
            RoomGrandPrix.initFirstInstance();

            //// Coloca aqui para ele não dá erro na hora de destruir o Room Grand Zodiac Event static instance
            RoomGrandZodiacEvent.initFirstInstance();

            //// Coloca aqui para ele não dá erro na hora de destruir o Room Bot GM Event static instance
            RoomBotGMEvent.initFirstInstance();
        }

        private void init_Packets()
        {
            packet_func.funcs.addPacketCall(0x02, packet_func.packet002, this);
            packet_func.funcs.addPacketCall(0x03, packet_func.packet003, this);
            packet_func.funcs.addPacketCall(0x04, packet_func.packet004, this);
            packet_func.funcs.addPacketCall(0x06, packet_func.packet006, this);
            packet_func.funcs.addPacketCall(0x07, packet_func.packet007, this);
            packet_func.funcs.addPacketCall(0x08, packet_func.packet008, this);
            packet_func.funcs.addPacketCall(0x09, packet_func.packet009, this);
            packet_func.funcs.addPacketCall(0x0A, packet_func.packet00A, this);
            packet_func.funcs.addPacketCall(0x0B, packet_func.packet00B, this);
            packet_func.funcs.addPacketCall(0x0C, packet_func.packet00C, this);
            packet_func.funcs.addPacketCall(0x0D, packet_func.packet00D, this);
            packet_func.funcs.addPacketCall(0x0E, packet_func.packet00E, this);
            packet_func.funcs.addPacketCall(0x0F, packet_func.packet00F, this);
            packet_func.funcs.addPacketCall(0x10, packet_func.packet010, this);
            packet_func.funcs.addPacketCall(0x11, packet_func.packet011, this);
            packet_func.funcs.addPacketCall(0x12, packet_func.packet012, this);
            packet_func.funcs.addPacketCall(0x13, packet_func.packet013, this);
            packet_func.funcs.addPacketCall(0x14, packet_func.packet014, this);
            packet_func.funcs.addPacketCall(0x15, packet_func.packet015, this);
            packet_func.funcs.addPacketCall(0x16, packet_func.packet016, this);
            packet_func.funcs.addPacketCall(0x17, packet_func.packet017, this);
            packet_func.funcs.addPacketCall(0x18, packet_func.packet018, this);
            packet_func.funcs.addPacketCall(0x19, packet_func.packet019, this);
            packet_func.funcs.addPacketCall(0x1A, packet_func.packet01A, this);
            packet_func.funcs.addPacketCall(0x1B, packet_func.packet01B, this);
            packet_func.funcs.addPacketCall(0x1C, packet_func.packet01C, this);
            packet_func.funcs.addPacketCall(0x1D, packet_func.packet01D, this);
            packet_func.funcs.addPacketCall(0x1F, packet_func.packet01F, this);
            packet_func.funcs.addPacketCall(0x20, packet_func.packet020, this);
            packet_func.funcs.addPacketCall(0x22, packet_func.packet022, this);
            packet_func.funcs.addPacketCall(0x26, packet_func.packet026, this);
            packet_func.funcs.addPacketCall(0x29, packet_func.packet029, this);
            packet_func.funcs.addPacketCall(0x2A, packet_func.packet02A, this);
            packet_func.funcs.addPacketCall(0x2D, packet_func.packet02D, this);
            packet_func.funcs.addPacketCall(0x2F, packet_func.packet02F, this);
            packet_func.funcs.addPacketCall(0x30, packet_func.packet030, this);
            packet_func.funcs.addPacketCall(0x31, packet_func.packet031, this);
            packet_func.funcs.addPacketCall(0x32, packet_func.packet032, this);
            packet_func.funcs.addPacketCall(0x33, packet_func.packet033, this);
            packet_func.funcs.addPacketCall(0x34, packet_func.packet034, this);
            packet_func.funcs.addPacketCall(0x35, packet_func.packet035, this);
            packet_func.funcs.addPacketCall(0x36, packet_func.packet036, this);
            packet_func.funcs.addPacketCall(0x37, packet_func.packet037, this);
            packet_func.funcs.addPacketCall(0x39, packet_func.packet039, this);
            packet_func.funcs.addPacketCall(0x3A, packet_func.packet03A, this);
            packet_func.funcs.addPacketCall(0x3C, packet_func.packet03C, this);
            packet_func.funcs.addPacketCall(0x3D, packet_func.packet03D, this);
            packet_func.funcs.addPacketCall(0x3E, packet_func.packet03E, this);
            packet_func.funcs.addPacketCall(0x41, packet_func.packet041, this);
            packet_func.funcs.addPacketCall(0x42, packet_func.packet042, this);
            packet_func.funcs.addPacketCall(0x43, packet_func.packet043, this);
            packet_func.funcs.addPacketCall(0x47, packet_func.packet047, this);
            packet_func.funcs.addPacketCall(0x48, packet_func.packet048, this);
            packet_func.funcs.addPacketCall(0x4A, packet_func.packet04A, this);
            packet_func.funcs.addPacketCall(0x4B, packet_func.packet04B, this);
            packet_func.funcs.addPacketCall(0x4F, packet_func.packet04F, this);
            packet_func.funcs.addPacketCall(0x54, packet_func.packet054, this);
            packet_func.funcs.addPacketCall(0x55, packet_func.packet055, this);
            packet_func.funcs.addPacketCall(0x57, packet_func.packet057, this);
            packet_func.funcs.addPacketCall(0x5C, packet_func.packet05C, this);
            packet_func.funcs.addPacketCall(0x60, packet_func.packet060, this);
            packet_func.funcs.addPacketCall(0x61, packet_func.packet061, this);
            packet_func.funcs.addPacketCall(0x63, packet_func.packet063, this);
            packet_func.funcs.addPacketCall(0x64, packet_func.packet064, this);
            packet_func.funcs.addPacketCall(0x65, packet_func.packet065, this);
            packet_func.funcs.addPacketCall(0x66, packet_func.packet066, this);
            packet_func.funcs.addPacketCall(0x67, packet_func.packet067, this);
            packet_func.funcs.addPacketCall(0x69, packet_func.packet069, this);
            packet_func.funcs.addPacketCall(0x6B, packet_func.packet06B, this);
            packet_func.funcs.addPacketCall(0x73, packet_func.packet073, this);
            packet_func.funcs.addPacketCall(0x74, packet_func.packet074, this);
            packet_func.funcs.addPacketCall(0x75, packet_func.packet075, this);
            packet_func.funcs.addPacketCall(0x76, packet_func.packet076, this);
            packet_func.funcs.addPacketCall(0x77, packet_func.packet077, this);
            packet_func.funcs.addPacketCall(0x78, packet_func.packet078, this);
            packet_func.funcs.addPacketCall(0x79, packet_func.packet079, this);
            packet_func.funcs.addPacketCall(0x7A, packet_func.packet07A, this);
            packet_func.funcs.addPacketCall(0x7B, packet_func.packet07B, this);
            packet_func.funcs.addPacketCall(0x7C, packet_func.packet07C, this);
            packet_func.funcs.addPacketCall(0x7D, packet_func.packet07D, this);
            packet_func.funcs.addPacketCall(0x81, packet_func.packet081, this);
            packet_func.funcs.addPacketCall(0x82, packet_func.packet082, this);
            packet_func.funcs.addPacketCall(0x83, packet_func.packet083, this);
            packet_func.funcs.addPacketCall(0x88, packet_func.packet088, this);
            packet_func.funcs.addPacketCall(0x8B, packet_func.packet08B, this);
            packet_func.funcs.addPacketCall(0x8F, packet_func.packet08F, this);
            packet_func.funcs.addPacketCall(0x98, packet_func.packet098, this);
            packet_func.funcs.addPacketCall(0x9C, packet_func.packet09C, this);
            packet_func.funcs.addPacketCall(0x9D, packet_func.packet09D, this);
            packet_func.funcs.addPacketCall(0x9E, packet_func.packet09E, this);
            packet_func.funcs.addPacketCall(0xA1, packet_func.packet0A1, this);
            packet_func.funcs.addPacketCall(0xA2, packet_func.packet0A2, this);
            packet_func.funcs.addPacketCall(0xAA, packet_func.packet0AA, this);
            packet_func.funcs.addPacketCall(0xAB, packet_func.packet0AB, this);
            packet_func.funcs.addPacketCall(0xAE, packet_func.packet0AE, this);
            packet_func.funcs.addPacketCall(0xB2, packet_func.packet0B2, this);
            // Recebi esse pacote quando troquei de server, e no outro eu tinha jogado um Match feito bastante Achievement
            // e pegado daily quest, desistido do resto e aceito a do dia e aberto alguns card packs, ai troquei de server e recebi esse pacote
            //2018-11-17 20:43:07.307 Tipo : 180(0xB4), desconhecido ou nao implementado.func_arr::getPacketCall()     Error Code : 335609856
            //2018-11-17 20:43:07.307 size packet : 5
            //0000 B4 00 01 00 00 -- -- -- -- -- -- -- -- -- -- --    ................
            packet_func.funcs.addPacketCall(0xB4, packet_func.packet0B4, this);
            packet_func.funcs.addPacketCall(0xB5, packet_func.packet0B5, this);
            packet_func.funcs.addPacketCall(0xB7, packet_func.packet0B7, this);
            packet_func.funcs.addPacketCall(0xB9, packet_func.packet0B9, this);
            packet_func.funcs.addPacketCall(0xBA, packet_func.packet0BA, this);
            packet_func.funcs.addPacketCall(0xBD, packet_func.packet0BD, this);
            packet_func.funcs.addPacketCall(0xC1, packet_func.packet0C1, this);
            packet_func.funcs.addPacketCall(0xC9, packet_func.packet0C9, this);
            packet_func.funcs.addPacketCall(0xCA, packet_func.packet0CA, this);
            packet_func.funcs.addPacketCall(0xCB, packet_func.packet0CB, this);
            packet_func.funcs.addPacketCall(0xCC, packet_func.packet0CC, this);
            packet_func.funcs.addPacketCall(0xCD, packet_func.packet0CD, this);
            packet_func.funcs.addPacketCall(0xCE, packet_func.packet0CE, this);
            packet_func.funcs.addPacketCall(0xCF, packet_func.packet0CF, this);
            packet_func.funcs.addPacketCall(0xD0, packet_func.packet0D0, this);
            packet_func.funcs.addPacketCall(0xD1, packet_func.packet0D1, this);
            packet_func.funcs.addPacketCall(0xD2, packet_func.packet0D2, this);
            packet_func.funcs.addPacketCall(0xD3, packet_func.packet0D3, this);
            packet_func.funcs.addPacketCall(0xD4, packet_func.packet0D4, this);
            packet_func.funcs.addPacketCall(0xD5, packet_func.packet0D5, this);
            packet_func.funcs.addPacketCall(0xD8, packet_func.packet0D8, this);
            packet_func.funcs.addPacketCall(0xDE, packet_func.packet0DE, this);
            packet_func.funcs.addPacketCall(0xE5, packet_func.packet0E5, this);
            packet_func.funcs.addPacketCall(0xE6, packet_func.packet0E6, this);
            packet_func.funcs.addPacketCall(0xE7, packet_func.packet0E7, this);
            packet_func.funcs.addPacketCall(0xEB, packet_func.packet0EB, this);
            packet_func.funcs.addPacketCall(0xEC, packet_func.packet0EC, this);
            packet_func.funcs.addPacketCall(0xEF, packet_func.packet0EF, this);
            packet_func.funcs.addPacketCall(0xF4, packet_func.packet0F4, this);
            packet_func.funcs.addPacketCall(0xFB, packet_func.packet0FB, this);
            packet_func.funcs.addPacketCall(0xFE, packet_func.packet0FE, this);
            packet_func.funcs.addPacketCall(0x119, packet_func.packet119, this);
            packet_func.funcs.addPacketCall(0x126, packet_func.packet126, this);
            packet_func.funcs.addPacketCall(0x127, packet_func.packet127, this);
            packet_func.funcs.addPacketCall(0x128, packet_func.packet128, this);
            packet_func.funcs.addPacketCall(0x129, packet_func.packet129, this);
            packet_func.funcs.addPacketCall(0x12C, packet_func.packet12C, this);
            packet_func.funcs.addPacketCall(0x12D, packet_func.packet12D, this);
            packet_func.funcs.addPacketCall(0x12E, packet_func.packet12E, this);
            packet_func.funcs.addPacketCall(0x12F, packet_func.packet12F, this);
            packet_func.funcs.addPacketCall(0x130, packet_func.packet130, this);
            packet_func.funcs.addPacketCall(0x131, packet_func.packet131, this);
            packet_func.funcs.addPacketCall(0x137, packet_func.packet137, this);
            packet_func.funcs.addPacketCall(0x138, packet_func.packet138, this);
            packet_func.funcs.addPacketCall(0x140, packet_func.packet140, this);
            packet_func.funcs.addPacketCall(0x141, packet_func.packet141, this);
            packet_func.funcs.addPacketCall(0x143, packet_func.packet143, this);
            packet_func.funcs.addPacketCall(0x144, packet_func.packet144, this);
            packet_func.funcs.addPacketCall(0x145, packet_func.packet145, this);
            packet_func.funcs.addPacketCall(0x146, packet_func.packet146, this);
            packet_func.funcs.addPacketCall(0x147, packet_func.packet147, this);
            packet_func.funcs.addPacketCall(0x14B, packet_func.packet14B, this);
            packet_func.funcs.addPacketCall(0x151, packet_func.packet151, this);
            packet_func.funcs.addPacketCall(0x152, packet_func.packet152, this);
            packet_func.funcs.addPacketCall(0x153, packet_func.packet153, this);
            packet_func.funcs.addPacketCall(0x154, packet_func.packet154, this);
            packet_func.funcs.addPacketCall(0x155, packet_func.packet155, this);
            packet_func.funcs.addPacketCall(0x156, packet_func.packet156, this);
            packet_func.funcs.addPacketCall(0x157, packet_func.packet157, this);
            packet_func.funcs.addPacketCall(0x158, packet_func.packet158, this);
            packet_func.funcs.addPacketCall(0x15C, packet_func.packet15C, this);
            packet_func.funcs.addPacketCall(0x15D, packet_func.packet15D, this);
            packet_func.funcs.addPacketCall(0x164, packet_func.packet164, this);
            packet_func.funcs.addPacketCall(0x165, packet_func.packet165, this);
            packet_func.funcs.addPacketCall(0x166, packet_func.packet166, this);
            packet_func.funcs.addPacketCall(0x167, packet_func.packet167, this);
            packet_func.funcs.addPacketCall(0x168, packet_func.packet168, this);
            packet_func.funcs.addPacketCall(0x169, packet_func.packet169, this);
            packet_func.funcs.addPacketCall(0x16B, packet_func.packet16B, this);
            packet_func.funcs.addPacketCall(0x16C, packet_func.packet16C, this);
            packet_func.funcs.addPacketCall(0x16D, packet_func.packet16D, this);
            packet_func.funcs.addPacketCall(0x16E, packet_func.packet16E, this);
            packet_func.funcs.addPacketCall(0x16F, packet_func.packet16F, this);
            packet_func.funcs.addPacketCall(0x171, packet_func.packet171, this);
            packet_func.funcs.addPacketCall(0x172, packet_func.packet172, this);
            packet_func.funcs.addPacketCall(0x176, packet_func.packet176, this);
            packet_func.funcs.addPacketCall(0x177, packet_func.packet177, this);
            packet_func.funcs.addPacketCall(0x179, packet_func.packet179, this);
            packet_func.funcs.addPacketCall(0x17A, packet_func.packet17A, this);
            packet_func.funcs.addPacketCall(0x17F, packet_func.packet17F, this);
            packet_func.funcs.addPacketCall(0x180, packet_func.packet180, this);
            packet_func.funcs.addPacketCall(0x181, packet_func.packet181, this);
            packet_func.funcs.addPacketCall(0x184, packet_func.packet184, this);
            packet_func.funcs.addPacketCall(0x185, packet_func.packet185, this);
            packet_func.funcs.addPacketCall(0x186, packet_func.packet186, this);
            packet_func.funcs.addPacketCall(0x187, packet_func.packet187, this);
            packet_func.funcs.addPacketCall(0x188, packet_func.packet188, this);
            packet_func.funcs.addPacketCall(0x189, packet_func.packet189, this);
            packet_func.funcs.addPacketCall(0x18A, packet_func.packet18A, this);
            packet_func.funcs.addPacketCall(0x18B, packet_func.packet18B, this);
            packet_func.funcs.addPacketCall(0x18C, packet_func.packet18C, this);
            packet_func.funcs.addPacketCall(0x18D, packet_func.packet18D, this);
            packet_func.funcs.addPacketCall(0x192, packet_func.packet192, this);
            packet_func.funcs.addPacketCall(0x196, packet_func.packet196, this);
            packet_func.funcs.addPacketCall(0x197, packet_func.packet197, this);
            packet_func.funcs.addPacketCall(0x198, packet_func.packet198, this);
            packet_func.funcs.addPacketCall(0x199, packet_func.packet199, this);

            packet_func.funcs_sv.addPacketCall(0x3F, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x40, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x42, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x44, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x45, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x46, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x47, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x48, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x49, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x4A, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x4B, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x4C, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x4D, packet_func.packet_sv4D, this);
            packet_func.funcs_sv.addPacketCall(0x4E, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x50, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x52, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x53, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x55, packet_func.packet_sv055, this);
            packet_func.funcs_sv.addPacketCall(0x56, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x58, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x59, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x5A, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x5B, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x5C, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x5D, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x60, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x61, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x63, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x64, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x65, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x66, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x67, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x68, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x6A, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x6B, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x6C, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x6D, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x6E, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x70, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x71, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x72, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x73, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x76, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x77, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x78, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x79, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x7C, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x7D, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x7E, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x83, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x84, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x86, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x89, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x8A, packet_func.packet_sv055, this);   // Esse pede o pacote 0x1B de tacada de novo do player que está com lag
            packet_func.funcs_sv.addPacketCall(0x8B, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x8C, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x8D, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x8E, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x90, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x91, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x92, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x93, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x94, packet_func.packet_svFazNada, this);   // Resposta player report chat game
            packet_func.funcs_sv.addPacketCall(0x95, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x96, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x97, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x9A, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x9E, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x9F, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0xA1, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0xA2, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0xA3, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0xA4, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0xA5, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0xA7, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0xAA, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0xAC, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0xB0, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0xB2, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0xB4, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0xB9, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0xBA, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0xBF, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0xC2, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0xC4, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0xC5, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0xC7, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0xC8, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0xC9, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0xCA, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0xCC, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0xCE, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0xD4, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0xD7, packet_func.packet_svFazNada, this);   // Request GameGuard Auth
            packet_func.funcs_sv.addPacketCall(0xE1, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0xE2, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0xE3, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0xE4, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0xE5, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0xE6, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0xE7, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0xE8, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0xE9, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0xEA, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0xEB, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0xEC, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0xED, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0xF1, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0xF5, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0xF6, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0xF8, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0xF9, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0xFA, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0xFB, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0xFC, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x101, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x102, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x10B, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x10E, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x10F, packet_func.packet_svFazNada, this);  // Dialog Level Up!
            packet_func.funcs_sv.addPacketCall(0x113, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x115, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x11A, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x11B, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x11C, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x11F, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x129, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x12A, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x12B, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x12D, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x12E, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x12F, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x130, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x131, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x132, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x133, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x134, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x135, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x136, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x137, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x138, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x139, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x13F, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x144, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x14E, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x14F, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x150, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x151, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x153, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x154, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x156, packet_func.packet_svRequestInfo, this);
            packet_func.funcs_sv.addPacketCall(0x157, packet_func.packet_svRequestInfo, this);
            packet_func.funcs_sv.addPacketCall(0x158, packet_func.packet_svRequestInfo, this);
            packet_func.funcs_sv.addPacketCall(0x159, packet_func.packet_svRequestInfo, this);
            packet_func.funcs_sv.addPacketCall(0x15A, packet_func.packet_svRequestInfo, this);
            packet_func.funcs_sv.addPacketCall(0x15B, packet_func.packet_svRequestInfo, this);
            packet_func.funcs_sv.addPacketCall(0x15C, packet_func.packet_svRequestInfo, this);
            packet_func.funcs_sv.addPacketCall(0x15D, packet_func.packet_svRequestInfo, this);
            packet_func.funcs_sv.addPacketCall(0x15E, packet_func.packet_svRequestInfo, this);
            packet_func.funcs_sv.addPacketCall(0x160, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x168, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x169, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x16A, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x16C, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x16D, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x16E, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x16F, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x170, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x171, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x172, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x173, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x174, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x176, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x181, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x18D, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x18F, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x190, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x196, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x197, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x198, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x199, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x19D, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x1A9, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x1AD, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x1B1, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x1D3, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x1D4, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x1D9, packet_func.packet_svFazNada, this);  // Update ON GAME. Level And Exp
            packet_func.funcs_sv.addPacketCall(0x1E7, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x1E8, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x1E9, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x1EA, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x1EC, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x1EE, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x1EF, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x1F0, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x1F1, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x1F2, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x1F3, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x1F4, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x1F5, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x1F7, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x1F8, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x1F9, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x1FA, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x200, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x201, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x203, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x20E, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x210, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x211, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x212, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x213, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x214, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x215, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x216, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x21B, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x21D, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x21E, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x220, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x225, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x226, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x227, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x228, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x229, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x22A, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x22B, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x22C, packet_func.packet_svFazNada/*packet_sv22D*/, this);
            packet_func.funcs_sv.addPacketCall(0x22D, packet_func.packet_svFazNada/*packet_sv22D*/, this);
            packet_func.funcs_sv.addPacketCall(0x22E, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x22F, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x230, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x231, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x236, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x237, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x23D, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x23E, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x23F, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x240, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x241, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x242, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x243, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x244, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x245, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x246, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x247, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x248, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x249, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x24C, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x24F, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x24E, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x250, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x251, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x253, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x254, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x255, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x256, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x257, packet_func.packet_svRequestInfo, this);
            packet_func.funcs_sv.addPacketCall(0x258, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x259, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x25A, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x25C, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x25D, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x264, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x265, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x266, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x26A, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x26B, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x26D, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x26E, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x26F, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x270, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x271, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x272, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x273, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x274, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x27E, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x27F, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x280, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x281, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x07F, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x00F, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x26C, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x122, packet_func.packet_svFazNada, this);
            packet_func.funcs_sv.addPacketCall(0x0D, packet_func.packet_svFazNada, this);

            // Auth Server Comandos
            packet_func.funcs_as.addPacketCall(0x1, packet_func.packet_as001, this);
        }

        public void init_load_channels()
        {
            v_channel = new List<Channel>();
            try
            {
                int num_channel = m_reader_ini.readInt("CHANNELINFO", "NUM_CHANNEL");

                for (byte i = 0; i < num_channel; ++i)
                {
                    ChannelInfo ci = new ChannelInfo();
                    try
                    {
                        ci.id = i;
                        ci.name = m_reader_ini.ReadString("CHANNEL" + (i + 1), "NAME");
                        ci.max_user = m_reader_ini.ReadInt16("CHANNEL" + (i + 1), "MAXUSER");
                        ci.min_level_allow = m_reader_ini.ReadUInt32("CHANNEL" + (i + 1), "LOWLEVEL");
                        ci.max_level_allow = m_reader_ini.ReadUInt32("CHANNEL" + (i + 1), "MAXLEVEL");
                        ci.type.ulFlag = m_reader_ini.ReadUInt32("CHANNEL" + (i + 1), "FLAG");
                    }
                    catch (Exception e)
                    {
                        _smp.message_pool.getInstance().push(new message("[GameService::init_load_channels][ErrorSystem] " + e.Message, type_msg.CL_FILE_LOG_AND_CONSOLE));
                    }

                    v_channel.Add(new Channel(ci, m_si.propriedade));
                }
            }
            catch (Exception e)
            {
                _smp.message_pool.getInstance().push(new message("[GameService::init_load_channels][ErrorSystem] " + e.Message, type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void reload_systems()
        {

            // Recarrega IFF_STRUCT
            sIff.getInstance().initilation();

            // Recarrega Card System
            sCardSystem.getInstance().load();

            // Recarrega Comet Refill System
            sCometRefillSystem.getInstance().load();

            // Recarrega Papel Shop System
            sPapelShopSystem.getInstance().load();

            // Recarrega Box System
            sBoxSystem.getInstance().load();

            // Recarrega Memorial System
            sMemorialSystem.getInstance().load();

            // Recarrega Cube Coin System
            sCubeCoinSystem.getInstance().load();

            // Recarrega Treasure Hunter System
            sTreasureHunterSystem.getInstance().load();

            // Recarrega Drop System
            sDropSystem.getInstance().load();

            // Recarrega Attendance Reward System
            sAttendanceRewardSystem.getInstance().load();

            // Recarrega Map Dados Estáticos
            MapSystem.getInstance().load();

            //// Recarrega Approach Mission System
            sApproachMissionSystem.getInstance().load();

            //// Recarrega Grand Zodiac Event System
            sGrandZodiacEvent.getInstance().load();

            // Recarrega Coin Cube Location Update Syatem
            sCoinCubeLocationUpdateSystem.getInstance().load();

            // Recarrega Golden Time System
            sGoldenTimeSystem.getInstance().load();

            // Recarrega Login Reward System
            sLoginRewardSystem.getInstance().load();

            // Recarrega Bot GM Event
            sBotGMEvent.getInstance().load(); 
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
                        sIff.getInstance().reload();
                        break;

                    case 2:     // Card
                                // Recarrega Card System
                        sCardSystem.getInstance().load();
                        break;

                    case 3:     // Comet Refill
                                // Recarrega Comet Refill System
                        sCometRefillSystem.getInstance().load();
                        break;

                    case 4:     // Papel Shop
                                // Recarrega Papel Shop System
                        sPapelShopSystem.getInstance().load();
                        break;

                    case 5:     // Box
                                // Recarrega Box System
                        sBoxSystem.getInstance().load();
                        break;

                    case 6:     // Memorial Shop
                                // Recarrega Memorial System
                        sMemorialSystem.getInstance().load();
                        break;

                    case 7:     // Cube e Coin
                                // Recarrega Cube Coin System
                        sCubeCoinSystem.getInstance().load();
                        break;

                    case 8:     // Treasure Hunter
                                // Recarrega Treasure Hunter System
                        sTreasureHunterSystem.getInstance().load();
                        break;

                    case 9:     // Drop
                                // Recarrega Drop System
                        sDropSystem.getInstance().load();
                        break;

                    case 10:    // Attendance Reward
                                // Recarrega Attendance Reward System
                        sAttendanceRewardSystem.getInstance().load();
                        break;

                    case 11:    // Map Course Dados
                                // Recarrega Map Dados Estáticos
                        MapSystem.getInstance().load();
                        break;

                    case 12:    // Approach Mission
                                // Recarrega Approach Mission
                        sApproachMissionSystem.getInstance().load();
                        break;

                    case 13:    // Grand Zodiac Event
                                // Recarrega Grand Zodiac Event
                        sGrandZodiacEvent.getInstance().load();
                        break;

                    case 14:    // Coin Cube Location Update System
                                // Recarrega Coin Cube Location Update System
                        sCoinCubeLocationUpdateSystem.getInstance().load();
                        break;

                    case 15:    // Golden Time System
                                // Recarrega Golden Time System
                        sGoldenTimeSystem.getInstance().load();
                        break;

                    case 16:    // Login Reward System
                                // Recarrega Login Reward System
                        sLoginRewardSystem.getInstance().load();
                        break;

                    case 17:    // Bot GM Event
                                // Recarrega Bot GM Event
                        sBotGMEvent.getInstance().load();
                        break;

                    case 18:    // Smart Calculator Lib
                                // Recarrega Smart Calculator Lib
                                // sSmartCalculator.getInstance().load();
                        break;

                    default:
                        throw new Exception($"[GameService::reloadGlobalSystem][Error] Tipo[VALUE={_tipo}] desconhecido.");
                }

                // Log
                _smp.message_pool.getInstance().push(
                     new message($"[GameService::reloadGlobalSystem][Log] Recarregou o Sistema[Tipo={_tipo}] com sucesso!", type_msg.CL_FILE_LOG_AND_CONSOLE)
                 );
            }
            catch (Exception e)
            {
                _smp.message_pool.getInstance().push(
                     new message($"[GameService::reloadGlobalSystem][ErrorSystem] {e.Message}", type_msg.CL_FILE_LOG_AND_CONSOLE)
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
                    throw new exception("[GameService::updateRateAndEvent][Error] Rate[TIPO=" + (_tipo) + ", QNTD="
                            + (_qntd) + "], qntd is invalid(zero).", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 120, 0));

                switch (_tipo)
                {
                    case 0: // Pang
                        setRatePang((short)_qntd);
                        break;
                    case 1: // Exp
                        setRateExp((short)_qntd);
                        break;
                    case 2: // Mastery
                        setRateClubMastery((short)_qntd);
                        break;
                    case 3: // Chuva
                        m_si.rate.chuva = (short)_qntd;
                        break;
                    case 4: // Treasure Hunter
                        m_si.rate.treasure = (short)_qntd;
                        break;
                    case 5: // Scratchy
                        m_si.rate.scratchy = (short)_qntd;
                        break;
                    case 6: // Papel Shop Rare Item
                        m_si.rate.papel_shop_rare_item = (short)_qntd;
                        break;
                    case 7: // Papel Shop Cookie Item
                        m_si.rate.papel_shop_cookie_item = (short)_qntd;
                        break;
                    case 8: // Memorial shop
                        m_si.rate.memorial_shop = (short)_qntd;
                        break;
                    case 9: // Event Grand Zodiac Time Event [Active/Desactive]
                        {
                            m_si.rate.grand_zodiac_event_time = (short)_qntd;

                            // Recarrega o Grand Zodiac Event se ele foi ativado
                            if (m_si.rate.grand_zodiac_event_time == 1)
                                reloadGlobalSystem(13/*Grand Zodiac Event*/);

                            break;
                        }
                    case 10: // Event Angel (Reduce 1 quit per game done)
                        setAngelEvent((short)_qntd);
                        break;
                    case 11: // Grand Prix Event
                        m_si.rate.grand_prix_event = (short)_qntd;
                        break;
                    case 12: // Golden Time Event
                        {
                            m_si.rate.golden_time_event = (short)_qntd;

                            // Recarrega o Golden Time Event se ele foi ativado
                            if (m_si.rate.golden_time_event == 1)
                                reloadGlobalSystem(15/*Golden Time Event*/);

                            break;
                        }
                    case 13: // Login Reward System Event
                        {
                            m_si.rate.login_reward_event = (short)_qntd;

                            // Recarrega o Login Reward Event se ele foi ativado
                            if (m_si.rate.login_reward_event == 1)
                                reloadGlobalSystem(16/*Login Reward Event*/);

                            break;
                        }
                    case 14: // Bot GM Event
                        {
                            m_si.rate.bot_gm_event = (short)_qntd;

                            // Recarrega o Bot GM Event se ele foi ativado
                            if (m_si.rate.bot_gm_event == 1)
                                reloadGlobalSystem(17/*Bot GM Event*/);

                            break;
                        }
                    case 15: // Smart Calculator
                        {
                            m_si.rate.smart_calculator = (short)_qntd;

                            // Recarrega o Smart Calculator System se ele foi ativado
                            if (m_si.rate.smart_calculator == 1)
                                reloadGlobalSystem(18/*Smart Calculator*/);

                            break;
                        }
                    default:
                        throw new exception("[GameService::updateRateAndEvent][Error] troca Rate[TIPO=" + (_tipo) + ", QNTD="
                                + (_qntd) + "], tipo desconhecido.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 120, 0));
                }

                // Update no DB os server do server que foram alterados
                snmdb.NormalManagerDB.getInstance().add(8, new CmdUpdateRateConfigInfo(m_si.uid, m_si.rate), SQLDBResponse, this);

                // Log
                _smp.message_pool.getInstance().push(new message("[GameService::updateRateAndEvent][Error] New Rate[Tipo=" + (_tipo) + ", QNTD="
                        + (_qntd) + "] com sucesso!", type_msg.CL_FILE_LOG_AND_CONSOLE));

                // UPDATE ON GAME
                var p = new PangyaBinaryWriter(0xF9);

                p.WriteBytes(m_si.ToArray());

                foreach (var el in v_channel)
                    packet_func.channel_broadcast(el, p, 1);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[GameService::updateRateAndEvent][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        // Make Grand Zodiac Event Room
        public void makeGrandZodiacEventRoom()
        {
            try
            {
                var rt_list = sGrandZodiacEvent.getInstance().getInterval();
                if (rt_list == null || rt_list.Count == 0) return;

                bool broadcastDone = false; // Trava para garantir envio único do pacote

                foreach (var rt in rt_list)
                {
                    // 1. Bloco de Mensagem/Broadcast (Executa apenas 1 vez)
                    if (rt.m_sended_message == false && !broadcastDone)
                    {
                        rt.m_sended_message = true;
                        broadcastDone = true; // Impede que outros intervalos enviem o pacote novamente

                        // Cálculo de duração
                        var duration_event_interval = rt.getDiffInterval() / (1000 * 60);
                        if (duration_event_interval % 10 == 9) duration_event_interval++;

                        // Construção do Pacote
                        string msg = $"<PARAMS><RunningTime>{duration_event_interval}</RunningTime><BroadCastReservedNoticesIdx>531</BroadCastReservedNoticesIdx></PARAMS>";

                        var p = new PangyaBinaryWriter(0x1D3);
                        p.WriteUInt32(2); // Count replay
                        for (var i = 0; i < 2; ++i)
                        {
                            p.WriteUInt32((uint)eBROADCAST_TYPES.BT_GRAND_ZODIAC_EVENT_START_TIME);
                            p.WriteString(msg);
                        }

                        // Envio para os canais
                        foreach (var el in v_channel)
                        {
                            packet_func.channel_broadcast(el, p, 1);
                        }

                        // Marca no Singleton que a mensagem global foi processada
                        sGrandZodiacEvent.getInstance().setSendedMessage();
                    }

                    // 2. Criação das Salas (Isso deve rodar para cada intervalo válido)
                    foreach (var el in v_channel)
                    {
                        if (!el.hasGrandZodiacRoomAtivo(RoomInfo.ROOM_INFO_TYPE.GRAND_ZODIAC_INT))
                        {
                            // Aqui você chama a criação passando o tipo INT
                            el.makeGrandZodiacEventRoom(rt);
                        }

                        // Verifica e cria a Advanced
                        if (!el.hasGrandZodiacRoomAtivo(RoomInfo.ROOM_INFO_TYPE.GRAND_ZODIAC_ADV))
                        {
                            // Aqui você chama a criação passando o tipo ADV
                            el.makeGrandZodiacEventRoom(rt);
                        } 

                    }
                }
            }
            catch (Exception e)
            {
                _smp.message_pool.getInstance().push(new message(
                    $"[GameService::makeGrandZodiacEventRoom][ErrorSystem] {e.Message}",
                    type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }


        // Make List of Players to Golden Time Event
        public void makeListOfPlayersToGoldenTime()
        {
            var p = new PangyaBinaryWriter();
            try
            {

                // (Primeira mensagem) - Mensagem que o round do Golden Time começou
                // ou Mensagem que o round do Golden Time vai ser calculado nesse momento

                // tem que ser primeiro por que ele atualiza o round se não tiver round atual
                bool is_first_message = sGoldenTimeSystem.getInstance().checkFirstMessage();

                // Pega o round atual
                var current_round = sGoldenTimeSystem.getInstance().getCurrentRound();

                if (current_round == null)
                {

                    // Log
                    _smp.message_pool.getInstance().push(new message("[GameService::makeListOfPlayersToGoldenTime][Error] current_round(nullptr) is invalid", type_msg.CL_FILE_LOG_AND_CONSOLE));

                    // Check Next Round Golden Time Event, por que o current_round é inválido
                    sGoldenTimeSystem.getInstance().checkNextRound();

                    return;
                }
                // Se for a primeira mensagem e estiver entre 5 minutos antes do round começar
                //nao vi como era no original(acrisio), fiz desta forma, com mais sentido
                if (is_first_message && DateTime.Now >= current_round.time.ConvertTime().AddMinutes(-5) && DateTime.Now < current_round.time.ConvertTime())
                {

                    // Send msg
                    p.init_plain(0x1D3);

                    p.WriteUInt32(1); // Count

                    p.WriteUInt32(eBROADCAST_TYPES.BT_GOLDEN_TIME_START_OF_DAY);
                    var str = "<PARAMS><EVENTSTARTTIME>" + UtilTime.FormatDate(current_round.time) + "</EVENTSTARTTIME><TYPEID>" + current_round.item._typeid + "</TYPEID><QTY>" + Convert.ToString(current_round.item.qntd_time > 0 ? current_round.item.qntd_time : current_round.item.qntd) + "</QTY></PARAMS>";
                    p.WriteString(str);

                    foreach (var el in v_channel)
                    {
                        packet_func.channel_broadcast(el,
                            p, 1);
                    }

                    return; // Sai, essa msg é enviada assim que muda de dia
                }
                else
                {

                    // Send msg
                    p.init_plain(0x1D3);

                    p.WriteUInt32(1); // Count

                    p.WriteUInt32(eBROADCAST_TYPES.BT_GOLDEN_TIME_START_ROUND);
                    p.WriteString("<PARAMS><EVENTSTARTTIME>" + UtilTime.FormatDate(current_round.time) + "</EVENTSTARTTIME><TYPEID>" + Convert.ToString(current_round.item._typeid) + "</TYPEID><QTY>" + Convert.ToString(current_round.item.qntd_time > 0 ? current_round.item.qntd_time : current_round.item.qntd) + "</QTY></PARAMS>");

                    foreach (var el in v_channel)
                    {
                        packet_func.channel_broadcast(el,
                            p, 1);
                    }
                }

                List<stPlayerReward> players = new List<stPlayerReward>();

                foreach (var c in v_channel)
                {

                    var v_p = c.getAllEligibleToGoldenTime();

                    if (v_p.empty())
                    {
                        continue;
                    }

                    players.AddRange(v_p);
                }

                if (players.Count == 0)
                {

                    // Log
                    _smp.message_pool.getInstance().push(new message("[GameService::makeListOfPlayersToGoldenTime][Error] Nenhum player ganhou no Golden Time Event. Round(" + UtilTime.FormatDate(current_round.time) + ")", type_msg.CL_FILE_LOG_AND_CONSOLE));

                    // Nenhum player ganhou esse Golden Time
                    p.init_plain(0x1D3);

                    p.WriteUInt32(1); // Count

                    p.WriteUInt32(eBROADCAST_TYPES.BT_GOLDEN_TIME_ROUND_NOT_HAVE_WINNERS);
                    p.WriteString("<PARAMS><EVENTSTARTTIME>" + UtilTime.FormatDate(current_round.time) + "</EVENTSTARTTIME></PARAMS>");

                    foreach (var el in v_channel)
                    {
                        packet_func.channel_broadcast(el,
                            p, 1);
                    }

                }
                else
                {

                    // Lambda[print XML all Player Nickname] 
                    Func<List<stPlayerReward>, string> printXMLAllPlayerNickname = _winners =>
                    {
                        string ret = "";
                        foreach (var el in _winners)
                        {
                            var gp = findPlayer(el.uid);
                            if (gp != null)
                            {
                                ret += "<NICKNAME>" + (gp.getNickname()) + "</NICKNAME>";
                            }
                        }

                        return ret;
                    };


                    // Lambda[print all Player UID]
                    Func<List<stPlayerReward>, string> printAllPlayerUID = _winners =>
                    {
                        string ret = "";
                        bool not_first = false;

                        foreach (var el in _winners)
                        {
                            ret += (not_first ? ", " : "") + Convert.ToString(el.uid);

                            if (!not_first)
                                not_first = true;
                        }

                        return ret;
                    };



                    // Mensagem que o round do Golden Time está bombando, tem mais da metada da capacidade de players do servidor participando do Evento
                    if (players.Count > m_si.max_user)
                    {

                        // Send msg
                        p.init_plain(0x1D3);

                        p.WriteUInt32(1); // Count

                        p.WriteUInt32(eBROADCAST_TYPES.BT_GOLDEN_TIME_ROUND_MORE_PEOPLE);
                        p.WriteString("<PARAMS><EVENTSTARTTIME>" + UtilTime.FormatDate(current_round.time) + "</EVENTSTARTTIME></PARAMS>");

                        foreach (var el in v_channel)
                        {
                            packet_func.channel_broadcast(el,
                                p, 1);
                        }
                    }

                    // Calcula o(s) player(s) que ganharam o prêmio do Golden Time Event
                    var reward = sGoldenTimeSystem.getInstance().calculeRoundReward(players);

                    // Log
                    _smp.message_pool.getInstance().push(new message("[GameService::makeListOfPlayersToGoldenTime][Error] Winners Of Round[" + UtilTime.FormatDate(current_round.time) + "] ITEM[TYPEID=" + Convert.ToString(current_round.item._typeid) + ", QNTD=" + Convert.ToString(current_round.item.qntd) + ", QNTD_TIME=" + Convert.ToString(current_round.item.qntd_time) + "] Player(s)[" + printAllPlayerUID(reward.players) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                    // Send Msg que o(s) players(s) ganharam, e coloca os itens no mail dele(s)
                    p.init_plain(0x1D3);

                    p.WriteUInt32(1); // Count

                    p.WriteUInt32(eBROADCAST_TYPES.BT_GOLDEN_TIME_ROUND_REWARD_PLAYER);
                    p.WriteString("<PARAMS><EVENTSTARTTIME>" + UtilTime.FormatDate(current_round.time) + "</EVENTSTARTTIME><TYPEID>" + Convert.ToString(current_round.item._typeid) + "</TYPEID><QTY>" + Convert.ToString(current_round.item.qntd_time > 0 ? current_round.item.qntd_time : current_round.item.qntd) + "</QTY>" + printXMLAllPlayerNickname(reward.players) + "</PARAMS>");

                    foreach (var el in v_channel)
                    {
                        packet_func.channel_broadcast(el,
                            p, 1);
                    }

                    // Send FireWorks na cabeça do(s) player(s) que ganharam e estão na lobby
                    foreach (var el in v_channel)
                    {
                        el.sendFireWorksWinnerGoldenTime(reward.players);
                    }

                    // Insere o item no mail do(s) player(s)
                    sGoldenTimeSystem.getInstance().sendRewardToMailOfPlayers(reward);
                }

                // Check Next Round Golden Time Event
                var next_round = sGoldenTimeSystem.getInstance().checkNextRound();

                SYSTEMTIME current = new SYSTEMTIME(DateTime.Now);

                if (next_round == null)
                {

                    // Acabou o Golden Time Event, e não tem outro marcado
                    // Envia a msg que acabou de vez
                    p.init_plain(0x1D3);

                    p.WriteUInt32(1); // Count

                    p.WriteUInt32(eBROADCAST_TYPES.BT_GOLDEN_TIME_FINISH);
                    p.WriteString("<PARAMS><BroadCastReservedNoticesIdx>53</BroadCastReservedNoticesIdx></PARAMS>");

                    foreach (var el in v_channel)
                    {
                        packet_func.channel_broadcast(el,
                            p, 1);
                    }

                }
                else if (UtilTime.IsSameDay(next_round.time, current))
                {

                    // Ainda é o Mesmo Golden Time Event (mesmo dia), round diferente
                    // Envia msg que acabou esse round e passa o horário e o item(Reward) do próximo
                    p.init_plain(0x1D3);

                    p.WriteUInt32(1); // Count

                    p.WriteUInt32(eBROADCAST_TYPES.BT_GOLDEN_TIME_FINISH_ROUND);
                    p.WriteString("<PARAMS><EVENTSTARTTIME>" + UtilTime.FormatDate(current) + "</EVENTSTARTTIME><EVENTNEXTTIME>" + UtilTime.FormatDate(next_round.time) + "</EVENTNEXTTIME><TYPEID>" + Convert.ToString(next_round.item._typeid) + "</TYPEID><QTY>" + Convert.ToString(next_round.item.qntd_time > 0 ? next_round.item.qntd_time : next_round.item.qntd) + "</QTY></PARAMS>");

                    foreach (var el in v_channel)
                    {
                        packet_func.channel_broadcast(el,
                            p, 1);
                    }

                }
                else
                {

                    // Outro dia é outro Golden Time Event
                    // Envia msg que acabou o Golden Time Event de hoje e passa a data do próximo Golden Time Event (agendado)
                    p.init_plain(0x1D3);

                    p.WriteUInt32(1); // Count

                    p.WriteUInt32(eBROADCAST_TYPES.BT_GOLDEN_TIME_FINISH_OF_DAY);
                    p.WriteString("<PARAMS><EVENTSTARTTIME>" + UtilTime.FormatDate(current) + "</EVENTSTARTTIME><EVENTNEXTTIME>" + UtilTime.FormatDate(next_round.time) + "</EVENTNEXTTIME></PARAMS>");

                    foreach (var el in v_channel)
                    {
                        packet_func.channel_broadcast(el,
                            p, 1);
                    }
                }

            }
            catch (exception e)
            {
                _smp.message_pool.getInstance().push(new message("[GameService::makeListOfPlayersToGoldenTime][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }


        // Make Bot GM Event Room
        public void makeBotGMEventRoom()
        {
            Func<uint, string> getItemName = (_typeid) =>
            {
                var @base = sIff.getInstance().findCommomItem(_typeid);
                return @base != null ? @base.Name : "";
            };

            try
            {
                var rt = sBotGMEvent.getInstance().getInterval();

                if (rt == null)
                {
                    return;
                }

                // Verifica se já enviou a mensagem, se não envia ela
                if (rt != null && !rt.m_sended_message)
                {
                    // Marca que a mensagem já foi envia, que vou enviar ela agora
                    sBotGMEvent.getInstance().setSendedMessage();

                    rt.m_sended_message = true;


                    int duration_event_interval = 2;
                    List<stReward> reward = sBotGMEvent.getInstance().calculeReward();

                    string reward_str = string.Join(", ", reward.Select(r => r.ToString()));
                    string premios = string.Join(", ", reward.Select(r =>
                    {
                        string quantidade = (r.qntd_time > 0)
                            ? r.qntd_time + "day"
                            : r.qntd.ToString();
                        return getItemName(r._typeid) + "(" + quantidade + ")";
                    }));

                    string channelName = "Canal (Livre 1)";
                    var canal = findChannel(rt.m_channel_id);
                    if (canal != null)
                        channelName = canal.getInfo().name;

                    string msg = MESSAGE_BOT_GM_EVENT_START_PART1 + channelName +
                                 MESSAGE_BOT_GM_EVENT_START_PART2 + duration_event_interval +
                                 MESSAGE_BOT_GM_EVENT_START_PART3 + premios;

                    // Broadcast para todos os canais
                    var p = new PangyaBinaryWriter(0x1D3);
                    p.WriteUInt32(2u);

                    for (uint i = 0; i < 2; ++i)
                    {
                        p.WriteUInt32(eBROADCAST_TYPES.BT_MESSAGE_PLAIN);
                        p.WriteString(msg);
                    }

                    foreach (var ch in v_channel)
                        packet_func.channel_broadcast(ch, p, 1);

                    // Cria a sala
                    if (canal != null)
                    {
                        canal.makeBotGMEventRoom(rt, reward);
                    }
                }
            }
            catch (exception e)
            {
                _smp.message_pool.getInstance().push(new message(
                    "[GameService::makeBotGMEventRoom][ErrorSystem] " + e.getFullMessageError(),
                    type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }


        protected override void onAcceptCompleted(Session _session)
        {
            try
            {

                packet p = new packet(0x3F);
                p.AddByte(1); // OPTION 1
                p.AddByte(1); // OPTION 2
                p.AddByte(_session.m_key);	// Key
                p.AddString(_session.m_ip);

                _session.requestSendBuffer(p.makeRaw(), true);

                _smp.message_pool.getInstance().push(
                    new message($"[GameService::onAcceptCompleted][Sucess] [IP: {_session.getIP()}, Key: {_session.m_key}]",
                                type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
            catch (exception ex)
            {
                _smp.message_pool.getInstance().push(new message(
              $"[GameService::onAcceptCompleted][ErrorSt]: {ex.getFullMessageError()}",
              type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override bool CheckPacket(Session _session, packet packet, int opt = 0)
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
                        // _smp.message_pool.getInstance().push(new message($"[{GetType().Name}::CheckPacket][Debug] PLAYER[UID: " + (uid == 0 ? player.m_ip : uid.ToString()) + ", PID: " + (PacketIDClient)packetId + "]", type_msg.CL_ONLY_CONSOLE));
                        return true;
                    }
                    else// nao tem no PacketIDClient
                    {
                        _smp.message_pool.getInstance().push(new message($"[{GetType().Name}::CheckPacket][Info]: PLAYER[UID: {player.m_pi.uid}, CGPID: 0x{packet.Id:X}]", type_msg.CL_FILE_LOG_AND_CONSOLE));
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


        public override void onDisconnected(Session _session)
        {
            if (_session == null)
                return;

            var _player = (Player)_session;

            _smp.message_pool.getInstance().push(new message("[GameService::onDisconnected][Warning] PLAYER[ID: " + _player.m_pi.id + "  UID: " + _player.m_pi.uid + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

            snmdb.NormalManagerDB.getInstance().add(5, new CmdRegisterLogon(_player.m_pi.uid, 1/*Logout*/), SQLDBResponse, this);

            /// Novo
            var _channel = findChannel(_player.m_pi.channel);

            try
            {
                if (_channel != null)
                    _channel.leaveChannel(_player);
            }
            catch (exception e)
            {
                _smp.message_pool.getInstance().push(new message("[GameService::onDisconnect][Error] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        //chama alguma coisa aqui!
        public override void OnStart()
        {
            Console.Title = $"Game Server ({m_si.nome}) - P: {m_si.curr_user}";
        }

        public Channel enterChannel(Player _session, byte _channel)
        {
            Channel enter = null, last = null;
            try
            {

                if ((enter = findChannel(_channel)) == null)
                {
                    packet_func.session_send(packet_func.pacote04E(3), _session);
                    return null;
                }


                if (enter.getId() == _session.m_pi.channel)
                {
                    packet_func.session_send(packet_func.pacote04E(1), _session);
                    return enter;   // Ele já está nesse canal
                }

                if (enter.isFull())
                {
                    // Não conseguiu entrar no canal por que ele está cheio, deixa o enter como null
                    enter = null;
                    packet_func.session_send(packet_func.pacote04E(2), _session);
                }
                if (!enter.checkEnterChannel(_session))
                {
                    enter = null;
                    packet_func.session_send(packet_func.pacote04E(2), _session);
                }
                else
                {
                    try
                    {
                        // Verifica se pode entrar no canal  
                        // Sai do canal antigo se ele estiver em outro canal
                        if (_session.m_pi.channel != DEFAULT_CHANNEL && (last = findChannel(_session.m_pi.channel)) != null)
                            last.leaveChannel(_session);

                        _session.m_channel = enter;
                        // Entra no canal
                        enter.enterChannel(_session);
                    }

                    catch (exception e)
                    {
                        _smp.message_pool.getInstance().push(new message("[GameService::enterChannel][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                        packet_func.session_send(packet_func.pacote04E(3), _session);
                    }
                }
            }
            catch (exception e)
            {
                _smp.message_pool.getInstance().push(new message("[GameService::enterChannel][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                packet_func.session_send(packet_func.pacote04E(3), _session);
            }

            return enter;
        }

        public void requestChangeChatMacroUser(Player _session, packet _packet)
        {
            try
            {
                // 1. Validação de Integridade do Pacote 
                if (_packet.GetSize != 578)
                {
                    throw new exception($"[GameService::requestChangeChatMacroUser][Error] PLAYER[UID={_session.m_pi.uid}] Bug Packet Size ({_packet.GetSize}). Hacker ou Bug",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 1, 1));
                }

                chat_macro_user cmu = new chat_macro_user();
                bool is_inject = false;

                for (int i = 0; i < 9; i++)
                {
                    var macro = _packet.ReadPStr(64);

                    // Se o macro for nulo, algo está muito errado na leitura ou no pacote
                    if (macro == null)
                        throw new exception($"[GameService::requestChangeChatMacroUser][Error] PLAYER[UID={_session.m_pi.uid}] Macro {i} is null.",
                            ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 1, 1));

                    // Validação de Injection
                    if (!Tools.Sanitize(macro))
                    {
                        is_inject = true;
                        macro = "Pangya!"; // Resetamos para um valor seguro
                    }

                    cmu.setMacro(i, macro);
                }

                // 3. Update na Memória
                _session.m_pi.cmu = cmu;

                // 4. Update no Banco de Dados
                snmdb.NormalManagerDB.getInstance().add(3, new CmdUpdateChatMacroUser(_session.m_pi.uid, _session.m_pi.cmu), SQLDBResponse, this);

                // 2. Se detectou tentativa de injection, barramos ANTES de salvar qualquer coisa
                if (is_inject)
                {
                    DisconnectSession(_session);
                }
            }
            catch (exception e)
            {
                _smp.message_pool.getInstance().push(new message("[GameService::requestChangeChatMacroUser][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestChangeServer(Player _session, packet _packet)
        {

            try
            {

                var server_uid = _packet.ReadUInt32();

                var it = m_server_list.FirstOrDefault(c => c.uid == server_uid);

                if (it == null)
                    throw new exception("[GameService::requestChangeServer][Error] PLAYER[UID=" + (_session.m_pi.uid)
                            + "] tentou trocar de server para o Server[UID=" + (server_uid)
                            + "], mas ele nao esta no server list mais.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 0x7500001, 1));

                if (_session.m_pi.lobby != DEFAULT_CHANNEL && _session.m_pi.lobby == 176u/*Grand Prix*/
                    && !it.propriedade.grand_prix/*Não é Grand Prix o Server*/)
                    throw new exception("[GameService::requestChangeServer][Error] PLAYER[UID=" + (_session.m_pi.uid)
                            + "] tentou trocar de server para o Server[UID=" + (server_uid)
                            + "], mas o player esta na lobby grand prix e o server que ele quer entrar nao e' grand prix.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 0x7500002, 2));

                var cmd_akg = new CmdAuthKeyGame(_session.m_pi.uid, server_uid);    // waitable

                snmdb.NormalManagerDB.getInstance().add(0, cmd_akg, null, null);

                if (cmd_akg.getException().getCodeError() != 0)
                    throw cmd_akg.getException();

                var auth_key_game = cmd_akg.getAuthKey();

                var cmd_uakl = new CmdUpdateAuthKeyLogin(_session.m_pi.uid, 1); // waitable

                snmdb.NormalManagerDB.getInstance().add(0, cmd_uakl, null, null);

                if (cmd_uakl.getException().getCodeError() != 0)
                    throw cmd_uakl.getException();

                packet_func.session_send(packet_func.pacote1D4(auth_key_game), _session);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[requestChangeServer][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Envia server lista novamente para o player ele foi proibido de entrar no server ou não conseguiu por algum motivo ou erro
                sendServerListAndChannelListToSession(_session);
            }
        }

        public void requestChangeWhisperState(Player _session, packet _packet)
        {
            try
            {

                var whisper = _packet.ReadByte();

                // Verifica se session está autorizada para executar esse ação, 
                // se ele não fez o login com o Server ele não pode fazer nada até que ele faça o login
                //CHECK_SESSION_IS_AUTHORIZED("ChangeWisperState");

                if (whisper > 1)
                    throw new exception("[GameService::requestChangeWhisperState][Error] PLAYER[UID=" + (_session.m_pi.uid) + "] tentou alterar o estado do Whisper[state="
                            + ((ushort)whisper) + "], mas ele mandou um valor invalido. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 1, 0x5300101));

                _session.m_pi.mi.state_flag.whisper = (_session.m_pi.whisper = whisper);

                // Log
                _smp.message_pool.getInstance().push(new message("[Whisper::ChangeState][Info] PLAYER[UID=" + (_session.m_pi.uid) + "] trocou o Whisper State para : " + (whisper.IsTrue() ? ("ON") : ("OFF")), type_msg.CL_FILE_LOG_AND_CONSOLE));


            }
            catch (exception e)
            {
                _smp.message_pool.getInstance().push(new message("[GameService::requestChangeWhisperState][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestChat(Player _session, packet _packet)
        {
            try
            {
                string nickname = _packet.ReadPStr();
                string msg = _packet.ReadPStr();

                if (string.IsNullOrEmpty(nickname))
                    throw new exception("[GameService::requestChat][Error] PLAYER[UID=" + (_session.m_pi.uid) + "] tentou enviar ticker[MESSAGE="
                            + nickname + "], vazio. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 1, 1/*UNKNOWN ERROR*/));

                if (!Tools.Sanitize(nickname))
                    throw new exception("[GameService::requestChat][Error] PLAYER[UID=" + (_session.m_pi.uid) + "] tentou enviar ticker[MESSAGE="
                            + nickname + "], tentativa de inject. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 1, 1/*UNKNOWN ERROR*/));

                if (string.IsNullOrEmpty(msg))
                    throw new exception("[GameService::requestChat][Error] PLAYER[UID=" + (_session.m_pi.uid) + "] tentou enviar ticker[MESSAGE="
                            + msg + "], vazio. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 1, 1/*UNKNOWN ERROR*/));


                //        // 2. Lista de palavras proibidas
                //        string[] palavrasProibidas = {
                //    "fdp", "filho da puta", "desgraçado", "desgraça", "merda", "bosta",
                //    "caralho", "porra", "puta", "puto", "arrombado", "corno", "viado",
                //    "bichona", "bicha", "trouxa", "otário", "retardado", "mongoloide",
                //    "imbecil", "idiota", "babaca", "piranha", "vagabunda", "cuzão",
                //    "cuzao", "pau no cu", "pau no seu cu", "seu cu", "vai se foder",
                //    "vai tomar no cu", "corna", "prostituta", "lixo"
                //};

                //        // 1. Checa se jogador está atualmente bloqueado
                //        if (_session.ChatPenalty.IsStillBlocked())
                //        {
                //            if (palavrasProibidas.Any(p => msg.IndexOf(p, StringComparison.OrdinalIgnoreCase) >= 0))
                //            {
                //                // Bloqueia o jogador por 30 segundos
                //                _session.ChatPenalty.RegisterOffense(_session.m_pi.uid, "Palavra ofensiva");

                //                // Log da tentativa
                //                _smp.message_pool.getInstance().push(
                //                    new message($"[GameService::requestChat][BLOCKED][Info]: PLAYER[UID: {_session.m_pi.uid}, MSG: {msg}]",
                //                    type_msg.CL_FILE_LOG_AND_CONSOLE));

                //                // Informa o jogador
                //                using (var packet = new PangyaBinaryWriter(0x40))
                //                {
                //                    packet.WriteByte(eChatMsg.CHAT_BLOCKED);
                //                    packet.Write(_session.ChatPenalty.BlockExpireTick);
                //                    packet_func.session_send(packet, _session);
                //                }
                //                return;
                //            }
                //            else
                //            {
                //                using (var packet = new PangyaBinaryWriter(0x122))
                //                {
                //                    packet.WriteByte(8);
                //                    packet.Write(_session.ChatPenalty.BlockExpireTick);
                //                    packet_func.session_send(packet, _session);
                //                }
                //            }
                //            return;
                //        }

                //        // 3. Verifica se mensagem contém palavras proibidas
                //        if (palavrasProibidas.Any(p => msg.IndexOf(p, StringComparison.OrdinalIgnoreCase) >= 0))
                //        {
                //            // Bloqueia o jogador por 30 segundos
                //            _session.ChatPenalty.RegisterOffense(_session.m_pi.uid, "Palavra ofensiva");

                //            // Log da tentativa
                //            _smp.message_pool.getInstance().push(
                //                new message($"[GameService::requestChat][BLOCKED][Info]: PLAYER[UID: {_session.m_pi.uid}, MSG: {msg}]",
                //                type_msg.CL_FILE_LOG_AND_CONSOLE));

                //            // Informa o jogador
                //            using (var packet = new PangyaBinaryWriter(0x122))
                //            {
                //                packet.WriteByte(8);
                //                packet.WritePStr("Você foi bloqueado por linguagem inapropriada.");
                //                packet.Write(_session.ChatPenalty.BlockExpireTick);
                //                packet_func.session_send(packet, _session);
                //            }
                //            return;
                //        }

                // 4. Envia mensagem para GMs
                var c = findChannel(_session.m_pi.channel);
                if (c != null)
                {
                    var gmList = FindAllGM();

                    if (gmList.Any())
                    {
                        string msg_gm = "\\5" + _session.m_pi.nickname + ": '" + msg + "'";
                        string from = "\\1[Channel=" + c.getInfo().name + ", \\1ROOM=" + _session.m_pi.mi.sala_numero + "]";

                        int index = from.IndexOf(' ');
                        if (index != -1)
                            from = from.Substring(0, index) + " \\1" + from.Substring(index + 1);

                        foreach (Player el in gmList)
                        {
                            if (((el.m_gi.channel > 0 && el.m_pi.channel == c.getInfo().id) || el.m_gi.whisper.IsTrue() || el.m_gi.isOpenPlayerWhisper(_session.m_pi.uid))
                                && (el.m_pi.channel != _session.m_pi.channel || el.m_pi.mi.sala_numero != _session.m_pi.mi.sala_numero))
                            {
                                packet_func.session_send(packet_func.pacote040(from, msg_gm, 0), el);
                            }
                        }
                    }

                    // 5. Executa comandos e envia a mensagem para sala ou lobby
                    var comando = new Queue<string>(msg.Split(' '));

                    if (_session.m_pi.mi.sala_numero != ushort.MaxValue)
                    {
                        c.requestSendMsgChatRoom(_session, msg);
                        c.CommandByChat(_session, comando);
                    }
                    else
                    {
                        var flag = _session.m_pi.m_cap.game_master ? eChatMsg.CHAT_GM : 0;
                        packet_func.channel_broadcast(c, packet_func.pacote040(_session.m_pi.nickname, msg, flag), 1);
                        c.CommandByChat(_session, comando);
                    }
                }
            }
            catch (exception e)
            {
                _smp.message_pool.getInstance().push(new message("[GameService::requestChat][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestCheckGameGuardAuthAnswer(Player _session, packet _packet)
        {
        }

        public void requestCommandNoticeGM(Player _session, packet _packet)
        {
            try
            {

                if (!(_session.m_pi.m_cap.game_master/* & 4*/))
                    throw new exception("[GameService::requestCommandNoticeGM][Error] PLAYER[UID=" + (_session.m_pi.uid)
                            + "] nao eh GM mas tentou executar comando GM. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 1, 0x5700100));

                string notice = _packet.ReadString();

                if (notice.empty())
                    throw new exception("[GameService::requestCommandNoticeGM][Error] PLAYER[UID=" + (_session.m_pi.uid)
                            + "] tentou executar o comando de notice, mas a notice is empty. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 8, 0x5700100));

                // Log
                _smp.message_pool.getInstance().push(new message("[GameService::requestCommandNoticeGM][Info] PLAYER[UID=" + (_session.m_pi.uid) + "] enviou notice[NOTICE="
                        + notice + "] para todos do game server.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                using (var p = new PangyaBinaryWriter(0x40))
                {
                    p.WriteByte(7); // Notice

                    p.WritePStr(_session.m_pi.nickname);
                    p.WritePStr(notice);
                    foreach (var c in v_channel)
                        packet_func.channel_broadcast(c, p.GetBytes);
                }
            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[GameService::requestCommandNoticeGM][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                using (var p = new PangyaBinaryWriter(0x40))
                {
                    p.WriteByte(7); // Notice

                    p.WritePStr(_session.m_pi.nickname);
                    p.WritePStr("Nao conseguiu executar o comando.");
                    packet_func.session_send(p, _session);

                }
            }

        }

        public void requestCommonCmdGM(Player _session, packet _packet)
        {
            try
            {
                _session.requestCommonCmdGM(_packet);
            }
            catch (exception e)
            {
                _smp.message_pool.getInstance().push(new message("[GameService::requestCommonCmdGM][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestLogin(Player session, packet pkt)
        {
            PangyaBinaryWriter p = null;

            try
            {
                // --- Variáveis / estruturas ---
                uint packetVersion = 0;
                var kol = new KeysOfLogin();
                string clientVersion = string.Empty;
                string macAddress = string.Empty; // TODO: preencher se você medir MAC do client

                // --- Ler packet ---
                ReadLoginPacket(session, pkt, out uint ntreevUID, out ushort command,
                                out kol.keys[0], out clientVersion, out bool hasClientVersion,
                                out packetVersion, out macAddress, out bool hasMac, out kol.keys[1], out bool hasAuthKeyGame);

                bool hasAuthKeyLogin = !string.IsNullOrEmpty(kol.keys[0]);

                // --- Validações básicas do pacote e do cliente ---
                if (!ValidateLoginPacket(session, hasClientVersion, clientVersion, packetVersion, hasAuthKeyLogin, kol.keys[0], hasMac, macAddress, hasAuthKeyGame, kol.keys[1]))
                {
                    _smp.message_pool.getInstance().push(new message("[GameService::requestLogin][Warning] PLAYER[UID=" + (session.m_pi.uid) + $", UserID= {session.m_pi.id}, AuthKey[1]= {kol.keys[0]},  AuthKey[2]= {kol.keys[1]}, NtreevUID= {ntreevUID}, CVersion= {clientVersion}]", type_msg.CL_FILE_LOG_AND_CONSOLE));
                    session.m_is_authorized = false;
                    SendLoginAck(session, eLoginAck.ACK_INVALID_VERSION);
                    return;
                }

                _smp.message_pool.getInstance().push(new message("[GameService::requestLogin][Info] " + pkt.Log(), type_msg.CL_ONLY_FILE_LOG));

                // --- Ban checks (IP / MAC) ---
                if (this.haveBanList(session.getIP(), macAddress))
                    throw new exception($"PLAYER[UID={session.m_pi.uid}, IP={session.getIP()}, MAC={macAddress}] blocked by banlist.");

                // --- sanity: id non-empty ---
                if (string.IsNullOrEmpty(session.m_pi.id))
                {
                    _smp.message_pool.getInstance().push(new message(
                        $"[GameService::requestLogin][Warning] PLAYER[UID={session.m_pi.uid}, IP={session.getIP()}] invalid id: {session.m_pi.id}",
                        type_msg.CL_FILE_LOG_AND_CONSOLE));

                    session.m_is_authorized = false;
                    SendLoginAck(session, eLoginAck.ACK_INVALID_VERSION);
                    return;
                }

                // --- Retrieve player info from DB ---
                var cmdPi = new CmdPlayerInfo(session.m_pi.uid); // waiter
                snmdb.NormalManagerDB.getInstance().add(0, cmdPi, null, null);
                if (cmdPi.getException().getCodeError() != 0) throw cmdPi.getException();

                session.m_pi.set_info(cmdPi.getInfo());

                if (session.m_pi.uid <= 0)
                {
                    _smp.message_pool.getInstance().push(new message($"[GameService::requestLogin][Warning] PLAYER[UID={session.m_pi.uid}] not found in DB", type_msg.CL_FILE_LOG_AND_CONSOLE));
                    session.m_is_authorized = false;
                    SendLoginAck(session, eLoginAck.ACK_INVALID_ID);
                    return;
                }

                // --- Anti-hack: verify client-supplied ID matches DB ID ---
                if (!string.Equals(cmdPi.getInfo().id, session.m_pi.id, StringComparison.Ordinal))
                {
                    _smp.message_pool.getInstance().push(new message(
                        $"[GameService::requestLogin][Warning] PLAYER[UID={session.m_pi.uid}] client ID mismatch: client={session.m_pi.id}, db={cmdPi.getInfo().id}",
                        type_msg.CL_FILE_LOG_AND_CONSOLE));

                    session.m_is_authorized = false;
                    SendLoginAck(session, eLoginAck.ACK_INVALID_ID);
                    return;
                }

                // --- Account block checks (temporary / forever / all-ip) ---
                CheckAccountBlock(session);

                // --- Packet version validation (after decrypt) ---
                Version_Decrypt(ref packetVersion);
                var serverPacketVersion = this.getInfo().packet_version;
                if (!this.canSameIDLogin() && packetVersion != serverPacketVersion)
                {
                    _smp.message_pool.getInstance().push(new message(
                        $"[GameService::requestLogin][Warning] PLAYER[UID={session.m_pi.uid}]. Client Packet Version not match. Server: {serverPacketVersion} != Client: {packetVersion}",
                        type_msg.CL_FILE_LOG_AND_CONSOLE));

                    session.m_is_authorized = false;
                    SendLoginAck(session, eLoginAck.ACK_INVALID_VERSION);
                    return;
                }

                // --- AuthKey (login) check ---
                var cmdAkli = new CmdAuthKeyLoginInfo((int)session.m_pi.uid);
                snmdb.NormalManagerDB.getInstance().add(0, cmdAkli, null, null);
                if (cmdAkli.getException().getCodeError() != 0) throw cmdAkli.getException();

                // NOTE: security: previously code used bitwise & and inverted booleans -> fixed
                if (!this.canSameIDLogin() && (!string.Equals(kol.keys[0], cmdAkli.getInfo().key, StringComparison.Ordinal) || cmdAkli.getInfo().valid == 0))
                {
                    _smp.message_pool.getInstance().push(new message($"[GameService::requestLogin][Warning] PLAYER[UID={session.m_pi.uid}]. LKey invalid or reused.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                    session.m_is_authorized = false;
                    SendLoginAck(session, eLoginAck.ACK_SECURITY_KEY);
                    return;
                }

                // --- AuthKey (game) check ---
                var cmdAkgi = new CmdAuthKeyGameInfo(session.m_pi.uid, (int)this.getUID());
                snmdb.NormalManagerDB.getInstance().add(0, cmdAkgi, null, null);
                if (cmdAkgi.getException().getCodeError() != 0) throw cmdAkgi.getException();

                if (!this.canSameIDLogin() && (!string.Equals(kol.keys[1], cmdAkgi.getInfo().key, StringComparison.Ordinal) || cmdAkgi.getInfo().valid == 0))
                {
                    _smp.message_pool.getInstance().push(new message($"[GameService::requestLogin][Warning] PLAYER[UID={session.m_pi.uid}]. GKey invalid or reused.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                    session.m_is_authorized = false;
                    SendLoginAck(session, eLoginAck.ACK_SECURITY_KEY);
                    return;
                }

                // --- Client version checks (region/season/high/low) ---
                var cvServer = ClientVersion.MakeVersion(this.getClientVersionSideServer());
                var cvClient = ClientVersion.MakeVersion(clientVersion);
                EvaluateClientVersion(session, cvServer, cvClient);

                // --- Member Info ---
                var cmdMi = new CmdMemberInfo(session.m_pi.uid);
                snmdb.NormalManagerDB.getInstance().add(0, cmdMi, null, null);
                if (cmdMi.getException().getCodeError() != 0) throw cmdMi.getException();
                session.setMemberInfo(cmdMi.getInfo());

                // --- GM handling ---
                session.m_pi.mi.oid = -1;///session.m_pi.mi.oid = session.m_oid;
                session.m_pi.mi.state_flag.visible = 1;
                session.m_pi.mi.state_flag.whisper = session.m_pi.whisper;
                session.m_pi.mi.state_flag.channel = (byte)(session.m_pi.whisper == 0 ? 1 : 0);
                if (session.m_pi.m_cap.game_master)
                {
                    session.m_gi.setGMUID(session.m_pi.uid);
                    session.m_pi.mi.state_flag.visible = session.m_gi.visible;
                    session.m_pi.mi.state_flag.whisper = session.m_gi.whisper;
                    session.m_pi.mi.state_flag.channel = session.m_gi.channel;

                }

                // --- GS property checks (rookie, mantle) ---
                if (this.m_si.propriedade.only_rookie && session.m_pi.level >= 6)
                    throw new exception($"PLAYER[UID={session.m_pi.uid}, LEVEL={session.m_pi.level}] not allowed (rookie-only GS).");

                if (this.m_si.propriedade.mantle && !(session.m_pi.m_cap.mantle || session.m_pi.m_cap.game_master))
                    throw new exception($"PLAYER[UID={session.m_pi.uid}] lacks mantle capability.");

                // --- Overlap: if another session with same UID exists ---
                var alreadyLogged = this.HasLoggedWithOuterSocket(session);
                if (alreadyLogged != null)
                {
                    _smp.message_pool.getInstance().push(new message(
                        $"[GameService::requestLogin][Error] existing session for UID={session.m_pi.uid}, disconnecting existing.",
                        type_msg.CL_FILE_LOG_AND_CONSOLE));

                    if (!this.DisconnectSession(alreadyLogged)) { }
                    //throw new exception($"Failed to disconnect existing session UID={alreadyLogged.getUID()}");
                }

                // --- Merge block flags and authorize session ---
                session.m_pi.block_flag.m_flag.ullFlag |= this.m_si.flag.ullFlag;
                session.m_is_authorized = true;

                // --- DB registration: player logged into GS ---
                snmdb.NormalManagerDB.getInstance().add(5, new CmdRegisterLogon(session.m_pi.uid, 0), this.SQLDBResponse, this);
                snmdb.NormalManagerDB.getInstance().add(7, new CmdRegisterLogonServer(session.m_pi.uid, this.m_si.uid), this.SQLDBResponse, this);

                _smp.message_pool.getInstance().push(new message($"[GameService::requestLogin][Sucess] PLAYER[OID={session.m_oid}, UID={session.m_pi.uid}, NICK={session.m_pi.nickname}].", type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Papel shop init
                sPapelShopSystem.getInstance().init_player_papel_shop_info(session);

                // Create login manager task to load all data
                m_login_manager.createTask(session);

                // Anti-bot timestamp
                session.m_tick_bot = Environment.TickCount;

                // Success: send ACK AUTO_RECONNECT
                packet_func.session_send(packet_func.pacote044(this.m_si, eLoginAck.ACK_AUTO_RECONNECT, session.m_pi), session, 0);
            }
            catch (exception ex)
            {
                _smp.message_pool.getInstance().push(new message($"[GameService::requestLogin][Error] {ex.getFullMessageError()}", type_msg.CL_FILE_LOG_AND_CONSOLE));
                session.m_is_authorized = false;

                // Generic error response
                p = new PangyaBinaryWriter(0x44);
                p.WriteUInt32(300);
                packet_func.session_send(p, session);

                // Disconnect session to be safe
                this.DisconnectSession(session);
            }
        }

        /* ----------------------
           Helper methods used above
           ---------------------- */

        private void ReadLoginPacket(Player session, packet pkt,
            out uint outNtreevUID, out ushort outCommand,
            out string outLKey, out string outClientVersion, out bool outHasClientVersion,
            out uint outPacketVersion, out string outMacAddress, out bool outHasMAC, out string outGKey, out bool outHasGKey)
        {
            outLKey = string.Empty;
            outClientVersion = string.Empty;
            outGKey = string.Empty;
            outMacAddress = string.Empty;

            session.m_pi.id = pkt.ReadString();
            session.m_pi.uid = pkt.ReadUInt32();
            outNtreevUID = pkt.ReadUInt32();
            outCommand = pkt.ReadUInt16();

            outLKey = pkt.ReadString();
            outHasClientVersion = pkt.ReadPStr(out outClientVersion) ? true : false;

            bool okPacketVersion = pkt.ReadUInt32(out outPacketVersion);
            outPacketVersion = okPacketVersion ? outPacketVersion : 0;
            outMacAddress = pkt.ReadString();
            outGKey = pkt.ReadString();
            outHasMAC = !string.IsNullOrEmpty(outMacAddress);
            outHasGKey = !string.IsNullOrEmpty(outGKey);
            session.m_MacAdress = outMacAddress;

        }

        private bool ValidateLoginPacket(Player session,
            bool hasClientVersion, string cversion, uint packetVersion,
            bool hasAuthKeyLogin, string lkey, bool hasMacAddress, string mac,
            bool hasAuthKeyGame, string gkey)
        {
            // checks: patch present, uid present, auth keys exist, id length sanity
            if (packetVersion == 0)
            {
                SendLoginAck(session, eLoginAck.ACK_INVALID_VERSION);
                return false;
            }

            if (session.m_pi.uid == 0)
            {
                SendLoginAck(session, eLoginAck.ACK_LOGIN_FAIL);
                return false;
            }

            if (!hasClientVersion || string.IsNullOrEmpty(cversion))
            {
                SendLoginAck(session, eLoginAck.ACK_INVALID_VERSION);
                return false;
            }

            if (!hasAuthKeyLogin || string.IsNullOrEmpty(lkey))
            {
                SendLoginAck(session, eLoginAck.ACK_SECURITY_KEY);
                return false;
            }

            if (!hasMacAddress || string.IsNullOrEmpty(mac))
            {
                SendLoginAck(session, eLoginAck.ACK_BLOCKED_IP_ADDR);
                return false;
            }

            if (!hasAuthKeyGame || string.IsNullOrEmpty(gkey))
            {
                SendLoginAck(session, eLoginAck.ACK_INVALID_VERSION);
                return false;
            }
            if (string.IsNullOrEmpty(session.m_pi.id) || session.m_pi.id.Length >= 0x40)
            {
                SendLoginAck(session, eLoginAck.ACK_INVALID_ID);
                return false;
            }
            return true;
        }

        private void SendLoginAck(Player session, eLoginAck ack)
        {
            using (var p = new PangyaBinaryWriter(0x44))
            {
                p.WriteUInt32((byte)ack);
                packet_func.session_send(p, session);
            }

            DisconnectSession(session);
        }

        private void CheckAccountBlock(Player _session)
        {
            // Verifica aqui se a conta do player está bloqueada
            if (_session.m_pi.block_flag.m_id_state.ull_IDState != 0)
            {

                if (_session.m_pi.block_flag.m_id_state.L_BLOCK_TEMPORARY && (_session.m_pi.block_flag.m_id_state.block_time == -1 || _session.m_pi.block_flag.m_id_state.block_time > 0))
                {

                    throw new exception("[GameService::requestLogin][Error] Bloqueado por tempo[Time="
                            + (_session.m_pi.block_flag.m_id_state.block_time == -1 ? ("indeterminado") : ((_session.m_pi.block_flag.m_id_state.block_time / 60)
                            + "min " + (_session.m_pi.block_flag.m_id_state.block_time % 60) + "sec"))
                            + "]. player [UID=" + (_session.m_pi.uid) + ", ID=" + (_session.m_pi.id) + "]");

                }
                else if (_session.m_pi.block_flag.m_id_state.L_BLOCK_FOREVER)
                {

                    throw new exception("[GameService::requestLogin][Error] Bloqueado permanente. player [UID=" + (_session.m_pi.uid)
                            + ", ID=" + (_session.m_pi.id) + "]");
                }

                else if (_session.m_pi.block_flag.m_id_state.L_BLOCK_ALL_IP)
                {

                    // Bloquea todos os IP que o player logar e da error de que a area dele foi bloqueada

                    // Add o ip do player para a lista de ip banidos
                    snmdb.NormalManagerDB.getInstance().add(9, new CmdInsertBlockIp(_session.getIP(), "255.255.255.255"), this.SQLDBResponse, this);

                    // Resposta
                    throw new exception("[GameService::requestLogin][Error] PLAYER[UID=" + (_session.m_pi.uid) + ", IP=" + (_session.getIP())
                            + "] Block ALL IP que o player fizer login.");
                }
                else if (_session.m_pi.block_flag.m_id_state.L_BLOCK_MAC_ADDRESS)
                {

                    // Bloquea o MAC Address que o player logar e da error de que a area dele foi bloqueada

                    // Add o MAC Address do player para a lista de MAC Address banidos
                    snmdb.NormalManagerDB.getInstance().add(10, new CmdInsertBlockMac(_session.m_MacAdress), this.SQLDBResponse, this);

                    // Resposta
                    throw new exception("[GameService::requestLogin][Error] PLAYER[UID=" + (_session.m_pi.uid)
                            + ", IP=" + (_session.getIP()) + ", MAC=" + _session.m_MacAdress + "] Block MAC Address que o player fizer login.");

                }
            }
        }

        private void EvaluateClientVersion(Player session, ClientVersion serverVer, ClientVersion clientVer)
        {
            if (clientVer.flag == ClientVersion.COMPLETE_VERSION &&
                string.Equals(clientVer.region, serverVer.region) &&
                string.Equals(clientVer.season, serverVer.season))
            {
                if (clientVer.high != serverVer.high || clientVer.low < serverVer.low)
                    session.m_pi.block_flag.m_flag.all_game = true;
            }
            else
            {
                if (clientVer.high != serverVer.high || clientVer.low < serverVer.low)
                    session.m_pi.block_flag.m_flag.all_game = true;
            }
        }

        public void requestEnterChannel(Player _session, packet _packet)
        {
            try
            {
                _packet.ReadByte(out byte channel);
                // Enter Channel
                var c = enterChannel(_session, channel);
                if (c != null)
                {
                    if (!sAttendanceRewardSystem.getInstance().isLoad())
                        sAttendanceRewardSystem.getInstance().load();

                    var p = new PangyaBinaryWriter();
                    var m_ari = _session.m_pi.ari;
                    if (m_ari.login == 2 || m_ari.login == 3) //sem o now
                    {
                        if (_session.m_pi.ari.counter == 0)
                            _session.m_pi.ari.counter = 1;
                        else
                            _session.m_pi.ari.counter = _session.m_pi.ari.counter + 1;

                        var reward_item = sAttendanceRewardSystem.getInstance().drawReward(1);

                        if (reward_item == null)
                            throw new exception("[GameService::requestEnterChannel][Error] nao conseguiu sortear um item para o player. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ATTENDANCE_REWARD_SYSTEM, 7, 0));

                        _session.m_pi.ari.now._typeid = reward_item._typeid;
                        _session.m_pi.ari.now.qntd = reward_item.qntd;
                        if (sIff.getInstance().IsExist(_session.m_pi.ari.now._typeid) == false)
                        {
                            //gera o proximo se não existir dados la na db
                            reward_item = sAttendanceRewardSystem.getInstance().drawReward((byte)(((_session.m_pi.ari.counter + 1) % 10 == 0) ? 2/*Tipo 2 Papel Box*/ : 1)/*Item Normal*/);

                            if (reward_item == null)
                                throw new exception("[GameService::requestEnterChannel][Error] nao conseguiu sortear um item para o player. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ATTENDANCE_REWARD_SYSTEM, 7, 0));

                            _session.m_pi.ari.now._typeid = reward_item._typeid;
                            _session.m_pi.ari.now.qntd = reward_item.qntd;
                        }
                        else
                        {
                            if (sIff.getInstance().IsExist(_session.m_pi.ari.after._typeid) == false)
                            {   //gera o proximo se não existir dados la na db
                                reward_item = sAttendanceRewardSystem.getInstance().drawReward((byte)(((_session.m_pi.ari.counter + 1) % 10 == 0) ? 2/*Tipo 2 Papel Box*/ : 1)/*Item Normal*/);

                                if (reward_item == null)
                                    throw new exception("[GameService::requestEnterChannel][Error] nao conseguiu sortear um item para o player. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ATTENDANCE_REWARD_SYSTEM, 7, 0));

                                _session.m_pi.ari.after._typeid = reward_item._typeid;
                                _session.m_pi.ari.after.qntd = reward_item.qntd;
                            }
                        }

                        _session.m_pi.ari.last_login.CreateTime();
                        // Zera as Horas deixa s� a date
                        _session.m_pi.ari.last_login.MilliSecond = _session.m_pi.ari.last_login.Second = _session.m_pi.ari.last_login.Minute = _session.m_pi.ari.last_login.Hour = 0;

                        stItem item = new stItem();
                        item.type = 2;
                        item.id = -1;
                        item._typeid = _session.m_pi.ari.now._typeid;
                        item.qntd = (int)_session.m_pi.ari.now.qntd;
                        item.STDA_C_ITEM_QNTD = (short)item.qntd;

                        var msg = "Your Attendance rewards have arrived!";

                        MailBoxManager.sendMessageWithItem(0, _session.m_pi.uid, msg, item);

                        _session.m_pi.ari.login = 0;

                        packet_func.session_send(packet_func.pacote248(_session.m_pi.ari), _session, 0);
                        _session.m_pi.ari.counter = 0;//vai pro zero de novo	  

                        // D� 3 Grand Prix Ticket, por que � a primeira vez que o player loga no dia
                        sAttendanceRewardSystem.getInstance().sendGrandPrixTicket(_session);
                        // D� 5 Key of fortune, por que � a primeira vez que o player loga no dia
                        sAttendanceRewardSystem.getInstance().sendFortuneKey(_session);
                        // D� 5 Key of fortune, por que � a primeira vez que o player loga no dia
                        sAttendanceRewardSystem.getInstance().sendBotTicket(_session);
                    }
                    else
                    {
                        if (sAttendanceRewardSystem.getInstance().passedOneDay(_session))
                        {
                            // Reward
                            stItem item = new stItem();

                            // Passou 1 dia depois que o player logou no pangya	  	
                            _session.m_pi.ari.login = 0;
                            _session.m_pi.ari.now = _session.m_pi.ari.after;
                            // Troca o item after para now
                            if (_session.m_pi.ari.now._typeid == 0 || sIff.getInstance().IsExist(_session.m_pi.ari.now._typeid) == false)
                            {
                            tryhard:
                                //gera o proximo se não existir dados la na db
                                var reward_item = sAttendanceRewardSystem.getInstance().drawReward((byte)(((_session.m_pi.ari.counter + 1) % 10 == 0) ? 2/*Tipo 2 Papel Box*/ : 1)/*Item Normal*/);

                                if (reward_item == null)
                                    throw new exception("[GameService::requestEnterChannel][Error] nao conseguiu sortear um item para o player. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ATTENDANCE_REWARD_SYSTEM, 7, 0));

                                _session.m_pi.ari.now._typeid = reward_item._typeid;
                                _session.m_pi.ari.now.qntd = reward_item.qntd;
                                if (_session.m_pi.ari.now._typeid == 0)
                                    goto tryhard;
                            }
                            else
                            {
                                if (_session.m_pi.ari.after._typeid == 0 || sIff.getInstance().IsExist(_session.m_pi.ari.after._typeid) == false)
                                {
                                tryhard:
                                    //gera o proximo se não existir dados la na db
                                    var reward_item = sAttendanceRewardSystem.getInstance().drawReward((byte)(((_session.m_pi.ari.counter + 1) % 10 == 0) ? 2/*Tipo 2 Papel Box*/ : 1)/*Item Normal*/);

                                    if (reward_item == null)
                                        throw new exception("[GameService::requestEnterChannel][Error] nao conseguiu sortear um item para o player. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ATTENDANCE_REWARD_SYSTEM, 7, 0));

                                    _session.m_pi.ari.after._typeid = reward_item._typeid;
                                    _session.m_pi.ari.after.qntd = reward_item.qntd;
                                    if (_session.m_pi.ari.after._typeid == 0)
                                        goto tryhard;
                                }
                            }
                            // Limpa o After
                            _session.m_pi.ari.after.clear();

                            item.type = 2;
                            item.id = -1;
                            item._typeid = _session.m_pi.ari.now._typeid;
                            item.qntd = (int)_session.m_pi.ari.now.qntd;
                            item.STDA_C_ITEM_QNTD = (short)item.qntd;

                            var msg = "Your Attendance rewards have arrived!";

                            MailBoxManager.sendMessageWithItem(0, _session.m_pi.uid, msg, item);

                            _session.m_pi.ari.counter = _session.m_pi.ari.counter + 1;
                            _session.m_pi.ari.login = 0;
                            packet_func.session_send(packet_func.pacote248(_session.m_pi.ari), _session, 0);


                            // D� 3 Grand Prix Ticket, por que � a primeira vez que o player loga no dia
                            sAttendanceRewardSystem.getInstance().sendGrandPrixTicket(_session);
                            // D� 5 Key of fortune, por que � a primeira vez que o player loga no dia
                            sAttendanceRewardSystem.getInstance().sendFortuneKey(_session);
                            // D� 5 Key of fortune, por que � a primeira vez que o player loga no dia
                            sAttendanceRewardSystem.getInstance().sendBotTicket(_session);
                        }
                    }
                }
               
            }
            catch (exception e)
            {
                _smp.message_pool.getInstance().push(new message("[GameService::requestEnterChannel][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestEnterOtherChannelAndLobby(Player _session, packet _packet)
        {
            try
            {

                // Lobby anterior que o player estava
                var lobby = _session.m_pi.lobby;

                var c = enterChannel(_session, _packet.ReadByte());

                if (c != null)
                    c.enterLobby(_session, lobby);
                else
                    DisconnectSession(_session);
            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[GameService::requestEnterOtherChannelAndLobby][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

            }
        }

        public void requestExceptionClientMessage(Player _session, packet _packet)
        {
            byte tipo = _packet.ReadByte();

            var exception_msg = _packet.ReadPStr();
            if (tipo == 1)
            {
                //cheat?
            }
            _smp.message_pool.getInstance().push(new message("[GameService::requestExceptionClientMessage][Error] PLAYER[UID=" + (_session.m_pi.uid) + ", EXTIPO="
                    + ((ushort)tipo) + ", MSG=" + exception_msg + "]", type_msg.CL_ONLY_CONSOLE));
            //
            DisconnectSession(_session);//send desconection
        }

        public void requestNotifyNotDisplayPrivateMessageNow(Player _session, packet _packet)
        {
            try
            {
                string nickname = _packet.ReadPStr();

                if (nickname.empty())
                    throw new exception("[GameService::requestNotifyNotDisplayPrivateMessageNow][Error] PLAYER[UID=" + (_session.m_pi.uid)
                            + "] nao pode ver mensagem agora, mas o nickname de quem enviou a mensagem para ele eh invalido(empty). Hacker ou Bug.",
                            ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 0x750050, 0));

                if (string.IsNullOrEmpty(nickname))
                    throw new exception("[GameService::requestNotifyNotDisplayPrivateMessageNow][Error] PLAYER[UID=" + (_session.m_pi.uid) + "] tentou enviar ticker[MESSAGE="
                            + nickname + "], vazio. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 1, 1/*UNKNOWN ERROR*/));

                if (!Tools.Sanitize(nickname))
                    throw new exception("[GameService::requestNotifyNotDisplayPrivateMessageNow][Error] PLAYER[UID=" + (_session.m_pi.uid) + "] tentou enviar ticker[MESSAGE="
                            + nickname + "], tentativa de inject. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 1, 1/*UNKNOWN ERROR*/));

                // Procura o player pelo nickname, para ver se ele está online
                var s = (Player)FindSessionByNickname(nickname);
                if (s != null && s.isConnected())
                {
                    // Log
                    _smp.message_pool.getInstance().push(new message("[GameService::requestNotifyNotDisplayPrivateMessageNow][Log] PLAYER[UID=" + (_session.m_pi.uid)
                            + "] recebeu mensagem do PLAYER[UID=" + (s.m_pi.uid) + ", NICKNAME=" + nickname + "], mas ele nao pode ver a mensagem agora.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                    packet_func.session_send(packet_func.pacote040(nickname, "", eChatMsg.CHAT_REFUSE_WHISPER), s);

                }
            }
            catch (exception e)
            {
                _smp.message_pool.getInstance().push(new message("[GameService::requestNotifyNotDisplayPrivateMessageNow][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestPlayerInfo(Player _session, packet _packet)
        {
            try
            {
                uint uid = _packet.ReadUInt32();
                byte season = _packet.ReadByte();

                Player s = null;
                PlayerInfo pi = null;
                CharacterInfo ci = new CharacterInfo();

                if (uid == _session.m_pi.uid)
                {
                    pi = _session.m_pi;
                }
                else if ((s = findPlayer(uid)) != null)
                {
                    pi = s.m_pi;
                }
                else
                {

                    var cmd_mi = new CmdMemberInfo(uid);

                    snmdb.NormalManagerDB.getInstance().add(0, cmd_mi, null, null);

                    if (cmd_mi.getException().getCodeError() != 0)
                        throw cmd_mi.getException();

                    MemberInfoEx mi = cmd_mi.getInfo();

                    // Verifica se não é o mesmo UID, pessoas diferentes
                    // Quem quer ver a info não é GM aí verifica se o player é GM
                    if (uid != _session.m_pi.uid && !_session.m_pi.m_cap.game_master && mi.capability.game_master/* & 4/*(GM)*/)
                    {
                        packet_func.session_send(packet_func.pacote089(uid, season, 3), _session); // No permission to see info of GM
                    }
                    else
                    {

                        List<MapStatisticsEx> v_ms_n, v_msa_n, v_ms_na, v_msa_na, v_ms_g, v_msa_g;

                        var cmd_ci = new CmdCharacterInfo(uid, CmdCharacterInfo.TYPE.ONE, -1);

                        snmdb.NormalManagerDB.getInstance().add(0, cmd_ci, null, null);

                        if (cmd_ci.getException().getCodeError() != 0)
                            throw cmd_ci.getException();

                        ci = cmd_ci.getInfo();

                        var cmd_ue = new CmdUserEquip(uid);

                        snmdb.NormalManagerDB.getInstance().add(0, cmd_ue, null, null);

                        if (cmd_ue.getException().getCodeError() != 0)
                            throw cmd_ue.getException();

                        UserEquip ue = cmd_ue.getInfo();

                        var cmd_ui = new CmdUserInfo(uid);

                        snmdb.NormalManagerDB.getInstance().add(0, cmd_ui, null, null);

                        if (cmd_ui.getException().getCodeError() != 0)
                            throw cmd_ui.getException();

                        UserInfoEx ui = cmd_ui.getInfo();

                        var cmd_gi = new CmdGuildInfo(uid, 0);

                        snmdb.NormalManagerDB.getInstance().add(0, cmd_gi, null, null);

                        if (cmd_gi.getException().getCodeError() != 0)
                            throw cmd_gi.getException();

                        var gi = cmd_gi.getInfo();

                        var cmd_ms = new CmdMapStatistics(uid, (CmdMapStatistics.TYPE_SEASON)(season), CmdMapStatistics.TYPE.NORMAL, CmdMapStatistics.TYPE_MODO.M_NORMAL);

                        snmdb.NormalManagerDB.getInstance().add(0, cmd_ms, null, null);

                        if (cmd_ms.getException().getCodeError() != 0)
                            throw cmd_ms.getException();

                        v_ms_n = cmd_ms.getMapStatistics();

                        cmd_ms.setType(CmdMapStatistics.TYPE.ASSIST);

                        snmdb.NormalManagerDB.getInstance().add(0, cmd_ms, null, null);

                        if (cmd_ms.getException().getCodeError() != 0)
                            throw cmd_ms.getException();

                        v_msa_n = cmd_ms.getMapStatistics();

                        cmd_ms.setType(CmdMapStatistics.TYPE.NORMAL);
                        cmd_ms.setModo(CmdMapStatistics.TYPE_MODO.M_NATURAL);

                        snmdb.NormalManagerDB.getInstance().add(0, cmd_ms, null, null);

                        if (cmd_ms.getException().getCodeError() != 0)
                            throw cmd_ms.getException();

                        v_ms_na = cmd_ms.getMapStatistics();

                        cmd_ms.setType(CmdMapStatistics.TYPE.ASSIST);

                        snmdb.NormalManagerDB.getInstance().add(0, cmd_ms, null, null);

                        if (cmd_ms.getException().getCodeError() != 0)
                            throw cmd_ms.getException();

                        v_msa_na = cmd_ms.getMapStatistics();

                        cmd_ms.setType(CmdMapStatistics.TYPE.NORMAL);
                        cmd_ms.setModo(CmdMapStatistics.TYPE_MODO.M_GRAND_PRIX);

                        snmdb.NormalManagerDB.getInstance().add(0, cmd_ms, null, null);

                        if (cmd_ms.getException().getCodeError() != 0)
                            throw cmd_ms.getException();

                        v_ms_g = cmd_ms.getMapStatistics();

                        cmd_ms.setType(CmdMapStatistics.TYPE.ASSIST);

                        snmdb.NormalManagerDB.getInstance().add(0, cmd_ms, null, null);

                        if (cmd_ms.getException().getCodeError() != 0)
                            throw cmd_ms.getException();

                        v_msa_g = cmd_ms.getMapStatistics();

                        var cmd_tei = new CmdTrophySpecial(uid, (CmdTrophySpecial.TYPE_SEASON)(season), CmdTrophySpecial.TYPE.NORMAL);

                        snmdb.NormalManagerDB.getInstance().add(0, cmd_tei, null, null);

                        if (cmd_tei.getException().getCodeError() != 0)
                            throw cmd_tei.getException();

                        List<TrofelEspecialInfo> v_tei = cmd_tei.getInfo();

                        var cmd_ti = new CmdTrofelInfo(uid, (CmdTrofelInfo.TYPE_SEASON)(season));

                        snmdb.NormalManagerDB.getInstance().add(0, cmd_ti, null, null);

                        if (cmd_ti.getException().getCodeError() != 0)
                            throw cmd_ti.getException();

                        TrofelInfo ti = cmd_ti.getInfo();

                        cmd_tei.setType(CmdTrophySpecial.TYPE.GRAND_PRIX);

                        snmdb.NormalManagerDB.getInstance().add(0, cmd_tei, null, null);

                        if (cmd_tei.getException().getCodeError() != 0)
                            throw cmd_tei.getException();

                        List<TrofelEspecialInfo> v_tegi = cmd_tei.getInfo();

                        packet_func.session_send(packet_func.pacote157(mi, season), _session);

                        packet_func.session_send(packet_func.pacote15E(uid, ci), _session);

                        packet_func.session_send(packet_func.pacote156(uid, ue, season), _session);

                        packet_func.session_send(packet_func.pacote158(uid, ui, season), _session);

                        packet_func.session_send(packet_func.pacote15D(uid, gi), _session);

                        packet_func.session_send(packet_func.pacote15C(uid, v_ms_na, v_msa_na, Convert.ToByte((season != 0) ? 0x33 : 0x0A)), _session);

                        packet_func.session_send(packet_func.pacote15C(uid, v_ms_g, v_msa_g, Convert.ToByte((season != 0) ? 0x34 : 0x0B)), _session);

                        packet_func.session_send(packet_func.pacote15B(uid, season), _session);

                        packet_func.session_send(packet_func.pacote15A(uid, v_tei, season), _session);

                        packet_func.session_send(packet_func.pacote159(uid, ti, season), _session);

                        packet_func.session_send(packet_func.pacote15C(uid, v_ms_n.ToList(), v_msa_n.ToList(), season), _session);

                        packet_func.session_send(packet_func.pacote257(uid, v_tegi, season), _session);

                        packet_func.session_send(packet_func.pacote089(uid, season), _session);
                    }

                    return;
                }

                // Verifica se não é o mesmo UID, pessoas diferentes
                // Quem quer ver a info não é GM aí verifica se o player é GM
                if (uid != _session.m_pi.uid && !_session.m_pi.m_cap.game_master && pi.m_cap.game_master/* & 4/*(GM)*/)
                {
                    packet_func.session_send(packet_func.pacote089(uid, season, 3), _session);
                }
                else
                {

                    var pCi = pi.findCharacterById(pi.ue.character_id);

                    if (pCi != null)
                        ci = pCi;

                    List<MapStatisticsEx> v_ms_n = new List<MapStatisticsEx>(), v_msa_n = new List<MapStatisticsEx>(), v_ms_na = new List<MapStatisticsEx>(), v_msa_na = new List<MapStatisticsEx>(), v_ms_g = new List<MapStatisticsEx>(), v_msa_g = new List<MapStatisticsEx>();

                    for (byte i = 0; i < MS_NUM_MAPS; ++i)
                        if (pi.a_ms_normal[i].best_score != 127)
                            v_ms_n.Add(pi.a_ms_normal[i]);

                    for (byte i = 0; i < MS_NUM_MAPS; ++i)
                        if (pi.a_msa_normal[i].best_score != 127)
                            v_msa_n.Add(pi.a_msa_normal[i]);

                    for (byte i = 0; i < MS_NUM_MAPS; ++i)
                        if (pi.a_ms_natural[i].best_score != 127)
                            v_ms_na.Add(pi.a_ms_natural[i]);

                    for (byte i = 0; i < MS_NUM_MAPS; ++i)
                        if (pi.a_msa_natural[i].best_score != 127)
                            v_msa_na.Add(pi.a_msa_natural[i]);

                    for (byte i = 0; i < MS_NUM_MAPS; ++i)
                        if (pi.a_ms_grand_prix[i].best_score != 127)
                            v_ms_g.Add(pi.a_ms_grand_prix[i]);

                    for (byte i = 0; i < MS_NUM_MAPS; ++i)
                        if (pi.a_msa_grand_prix[i].best_score != 127)
                            v_msa_g.Add(pi.a_msa_grand_prix[i]);

                    packet_func.session_send(packet_func.pacote157(pi.mi, season), _session);

                    packet_func.session_send(packet_func.pacote15E(pi.uid, ci), _session);

                    packet_func.session_send(packet_func.pacote156(pi.uid, pi.ue, season), _session);

                    packet_func.session_send(packet_func.pacote158(pi.uid, pi.ui, season), _session);

                    packet_func.session_send(packet_func.pacote15D(pi.uid, pi.gi), _session);

                    packet_func.session_send(packet_func.pacote15C(pi.uid, v_ms_na, v_msa_na, (byte)((season != 0) ? 0x33 : 0x0A)), _session);

                    packet_func.session_send(packet_func.pacote15C(pi.uid, v_ms_g, v_msa_g, (byte)((season != 0) ? 0x34 : 0x0B)), _session);

                    packet_func.session_send(packet_func.pacote15B(uid, season), _session);

                    packet_func.session_send(packet_func.pacote15A(pi.uid, (season != 0) ? pi.v_tsi_current_season : pi.v_tsi_rest_season, season), _session);

                    packet_func.session_send(packet_func.pacote159(pi.uid, (season != 0) ? pi.ti_current_season : pi.ti_rest_season, season), _session);

                    packet_func.session_send(packet_func.pacote15C(pi.uid, v_ms_n, v_msa_n, season), _session);

                    packet_func.session_send(packet_func.pacote257(pi.uid, (season != 0) ? pi.v_tgp_current_season : pi.v_tgp_rest_season, season), _session);

                    packet_func.session_send(packet_func.pacote089(uid, season), _session);
                }
            }
            catch (Exception e)
            {
                _smp.message_pool.getInstance().push(new message($"[GameService::RequestPlayerInfo][ErrorSystem] {e.Message}", type_msg.CL_FILE_LOG_AND_CONSOLE));
                packet_func.session_send(packet_func.pacote089(0), _session);
            }
        }

        public void requestPrivateMessage(Player _session, packet _packet)
        {
            PangyaBinaryWriter p = new PangyaBinaryWriter();
            Player s = null;
            string nickname = "";

            try
            {

                // Verifica se session está autorizada para executar esse ação, 
                // se ele não fez o login com o Server ele não pode fazer nada até que ele faça o login
                //    CHECK_SESSION_IS_AUTHORIZED("PrivateMessage");

                nickname = _packet.ReadPStr();
                string msg = _packet.ReadPStr();

                if (string.IsNullOrEmpty(nickname))
                    throw new exception("[GameService::requestPrivateMessage][Error] PLAYER[UID=" + (_session.m_pi.uid) + "] tentou enviar ticker[MESSAGE="
                            + nickname + "], vazio. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 1, 1/*UNKNOWN ERROR*/));

                if (!Tools.Sanitize(nickname))
                    throw new exception("[GameService::requestPrivateMessage][Error] PLAYER[UID=" + (_session.m_pi.uid) + "] tentou enviar ticker[MESSAGE="
                            + nickname + "], tentativa de inject. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 1, 1/*UNKNOWN ERROR*/));

                if (string.IsNullOrEmpty(msg))
                    throw new exception("[GameService::requestPrivateMessage][Error] PLAYER[UID=" + (_session.m_pi.uid) + "] tentou enviar ticker[MESSAGE="
                            + msg + "], vazio. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 1, 1/*UNKNOWN ERROR*/));

                if (!Tools.Sanitize(msg))
                    throw new exception("[GameService::requestPrivateMessage][Error] PLAYER[UID=" + (_session.m_pi.uid) + "] tentou enviar ticker[MESSAGE="
                            + msg + "], tentativa de inject. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 1, 1/*UNKNOWN ERROR*/));


                // Verifica se o player tem os itens necessários(PREMIUM USER OR GM) para usar essa função
                if (nickname.Contains("#SC") || nickname.Contains("#CS"))
                {

                    // Só sai do Private message se for comando do Smart Calculator, se não faz as outras verificações para enviar o PM
                    //if (m_si.rate.smart_calculator && checkSmartCalculatorCmd(_session, msg, (nickname.compare("#SC") == 0 ? eTYPE_CALCULATOR_CMD::SMART_CALCULATOR : eTYPE_CALCULATOR_CMD::CALCULATOR_STADIUM)))
                    //    return;
                }

                s = (Player)FindSessionByNickname(nickname);

                if (s == null)
                {
                    p.init_plain(0x40);

                    p.WriteByte(6);
                    if (s != null && s.m_pi != null)
                        p.WritePStr(s.m_pi.nickname);
                    else
                        p.WritePStr(nickname);  // Player não está online usa o nickname que ele forneceu

                    packet_func.session_send(p, _session);
                }

                // Whisper Block
                if (!(s.m_pi.whisper == 1))
                {
                    p.init_plain(0x40);

                    p.WriteByte(6);
                    if (s != null && s.m_pi != null)
                        p.WritePStr(s.m_pi.nickname);
                    else
                        p.WritePStr(nickname);  // Player não está online usa o nickname que ele forneceu

                    packet_func.session_send(p, _session);
                }

                if ((s.m_pi.lobby == 255/*não está na lobby*/ && s.m_pi.mi.sala_numero == ushort.MaxValue/*e não está em nenhum sala*/) || s.m_pi.place != 2)
                {
                    p.init_plain(0x40);

                    p.WriteByte(6);
                    if (s != null && s.m_pi != null)
                        p.WritePStr(s.m_pi.nickname);
                    else
                        p.WritePStr(nickname);  // Player não está online usa o nickname que ele forneceu

                    packet_func.session_send(p, _session);
                }

                // Arqui procura por palavras inapropriadas na message

                // Envia para todo os GM do serve   r essa message
                var gm = FindAllGM();

                if (!gm.Any())
                {

                    var msg_gm = "\\5" + (_session.m_pi.nickname) + ">" + (s.m_pi.nickname) + ": '" + msg + "'";

                    foreach (Player el in gm)
                    {
                        if ((el.m_gi.whisper.IsTrue() || el.m_gi.isOpenPlayerWhisper(_session.m_pi.uid) || el.m_gi.isOpenPlayerWhisper(s.m_pi.uid))
                            && /*Nao envia o log de PM novamente para o GM que enviou ou recebeu PM*/(el.m_pi.uid != _session.m_pi.uid && el.m_pi.uid != s.m_pi.uid))
                        {
                            // Responde no chat do player
                            p.init_plain(0x40);

                            p.WriteByte(0);

                            p.WritePStr("\\1[PM]"); // Nickname

                            p.WritePStr(msg_gm);    // Message
                            packet_func.session_send(p, el);
                        }
                    }

                }

                // Log
                _smp.message_pool.getInstance().push(new message("[PrivateMessage][LOG] PLAYER[UID=" + (_session.m_pi.uid) + "] enviou a Message[" + msg + "] para o PLAYER[UID=" + (s.m_pi.uid) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Resposta para o que enviou a private message
                p.init_plain(0x84);

                p.WriteByte(0); // FROM

                p.WritePStr(s.m_pi.nickname);   // Nickname TO
                p.WritePStr(msg);
                packet_func.session_send(p, _session);

                // Resposta para o player que vai receber a private message
                p.init_plain(0x84);

                p.WriteByte(1); // TO

                p.WritePStr(_session.m_pi.nickname);    // Nickname FROM
                p.WritePStr(msg);
                packet_func.session_send(p, s);

                // Envia a mensagem para o Chat History do discord se ele estiver ativo

                // Verifica se o m_chat_discod flag está ativo para enviar o chat para o discord
                //     if (m_si.rate.smart_calculator && m_chat_discord)
                //sendMessageToDiscordChatHistory(
                //	"[PM]",                                                                                                             // From
                //             (_session.m_pi.nickname) + ">" + (s.m_pi.nickname) + ": '" + msg + "'"						// Msg
                //);

            }
            catch (exception e)
            {
                _smp.message_pool.getInstance().push(new message("[GameService::requestPrivateMessage][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x40);

                p.WriteByte((ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) == (uint)STDA_ERROR_TYPE.GAME_SERVER) ? (byte)ExceptionError.STDA_SYSTEM_ERROR_DECODE(e.getCodeError()) : 5);
                if (s != null && s.isConnected())
                    p.WritePStr(s.m_pi.nickname);
                else
                    p.WritePStr(nickname);  // Player não está online usa o nickname que ele forneceu
                packet_func.session_send(p, _session);
            }
        }

        public void requestQueueTicker(Player _session, packet _packet)
        {
            //////REQUEST_BEGIN("QueueTicker");

            PangyaBinaryWriter p = new PangyaBinaryWriter();

            try
            {

                if (_session.m_pi.block_flag.m_flag.ticker)
                    throw new exception("[GameService::requestQueueTicker][Error] PLAYER[UID=" + (_session.m_pi.uid)
                            + "] tentou abrir a fila do Ticker, mas o ticker esta bloqueado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 10, 1/*UNKNOWN ERROR*/));

                var count = m_ticker.getSize();

                var time_left_milisecond = count * 30000;

                // Send Count Ticker and time left for send ticker
                p.init_plain(0xCA);

                p.WriteUInt16((ushort)count);
                p.WriteUInt32(time_left_milisecond);
                packet_func.session_send(p, _session);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[GameService::requestQueueTicker][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                // estou usando pacote de troca nickname, por que n�o sei qual o pangya manda, quando da erro no mandar ticker, nunca peguei esse erro
                p.init_plain(0x50);

                p.WriteUInt32((ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) == (uint)STDA_ERROR_TYPE.GAME_SERVER) ? ExceptionError.STDA_SYSTEM_ERROR_DECODE(e.getCodeError()) : 1/*UNKNOWN ERROR*/);

                packet_func.session_send(p, _session);
            }
        }


        public void requestSendTicker(Player _session, packet _packet)
        {
            var p = new PangyaBinaryWriter();

            try
            {


                if (_session.m_pi.block_flag.m_flag.ticker)
                    throw new exception("[GameService::requestSendTicker][Error] PLAYER[UID=" + (_session.m_pi.uid)
                            + "] tentou abrir a fila do Ticker, mas o ticker esta bloqueado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 10, 1/*UNKNOWN ERROR*/));

                var msg = _packet.ReadString();//fazer um translation aqui

                if (msg.empty())
                    throw new exception("[GameService::requestSendTicker][Error] PLAYER[UID=" + (_session.m_pi.uid) + "] tentou enviar ticker[MESSAGE="
                            + msg + "], mas msg is empty(vazia). Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 1, 1/*UNKNOWN ERROR*/));


                if (!Tools.Sanitize(msg))
                    throw new exception("[GameService::requestSendTicker][Error] PLAYER[UID=" + (_session.m_pi.uid) + "] tentou enviar ticker[MESSAGE="
                            + msg + "], tentativa de inject. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 1, 1/*UNKNOWN ERROR*/));

                try
                {

                    // Log de Gastos de CP(Cookie Point)
                    CPLog cp_log = new CPLog();

                    cp_log.setType(CPLog.TYPE.TICKER);

                    cp_log.setCookie(1);
                    // fim do inicializa log de gastos de CP

                    _session.m_pi.consomeCookie(1);

                    // Add o Ticker para lista de ticker do server
                    m_ticker.push_back((int)DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 3/*Segundos*/, _session.m_pi.nickname, msg, BroadcastManager.TYPE.TICKER);

                    // Add o Ticker para Commando DB para o Auth Server mandar para os outros serveres
                    snmdb.NormalManagerDB.getInstance().add(6, new CmdInsertTicker(_session.m_pi.uid, (uint)m_si.uid, msg), SQLDBResponse, this);

                    // Salva CP Log
                    _session.saveCPLog(cp_log);

                    // Log
                    _smp.message_pool.getInstance().push(new message("[GameService::requestSendTicker][Sucess] PLAYER[UID=" + (_session.m_pi.uid) + ", NICKNAME="
                            + (_session.m_pi.nickname) + "] enviou Ticker[MESSAGE=" + msg + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                    // UPDATE ON GAME
                    p.init_plain(0x96);

                    p.WriteUInt64(_session.m_pi.cookie);

                    packet_func.session_send(p, _session, 1);

                }
                catch (exception e)
                {

                    if (ExceptionError.STDA_ERROR_CHECK_SOURCE_AND_ERROR_TYPE(e.getCodeError(), STDA_ERROR_TYPE.PLAYER_INFO, 20))
                    {

                        throw new exception("[GameService::requestSendTicker][Error] PLAYER[UID=" + (_session.m_pi.uid) + "] tentou enviar ticker[MESSAGE="
                                + msg + "], mas ele nao tem cookies suficiente[HAVE=" + (_session.m_pi.cookie) + ", REQ=1]", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 1, 4/*NÃO TEM COOKIES SUFICIENTE*/));

                    }
                    else if (ExceptionError.STDA_ERROR_CHECK_SOURCE_AND_ERROR_TYPE(e.getCodeError(), STDA_ERROR_TYPE.PLAYER_INFO, 200/*Tem alterações no Cookie do player no DB*/))
                    {

                        throw new exception("[GameService::requestSendTicker][Error] PLAYER[UID=" + (_session.m_pi.uid) + "] tentou enviar ticker[MESSAGE="
                                + msg + ", mas tem alteracoes no Cookie dele no Banco de dados.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 1, 4/*Tem alterações no Cookie do player no DB*/));

                    }
                    else
                    {

                        // Devolve os Cookies gasto do player, por que não conseguiu enviar o ticker dele
                        _session.m_pi.addCookie(1);

                        // Relança a exception para da uma resposta para o cliente
                        throw;
                    }
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[GameService::requestSendTicker][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                // estou usando pacote de troca nickname, por que não sei qual o pangya manda, quando da erro no mandar ticker, nunca peguei esse erro
                p.init_plain(0x50);

                p.WriteUInt32((ExceptionError.STDA_ERROR_DECODE(e.getCodeError()) == (uint)STDA_ERROR_TYPE.GAME_SERVER) ? ExceptionError.STDA_ERROR_DECODE(e.getCodeError()) : 1/*UNKNOWN ERROR*/);

                packet_func.session_send(p, _session, 1);
            }
        }

        public void requestTranslateSubPacket(Player _session, packet _packet)
        {
            var p = new PangyaBinaryWriter();
            TranslationSubPacket sub_packet_id = TranslationSubPacket.Friend_List;
            try
            {
                sub_packet_id = (TranslationSubPacket)_packet.ReadUInt16();
                switch (sub_packet_id)
                {
                    case TranslationSubPacket.Msg_OFF:
                        {

                            uint uid = _packet.ReadUInt32();
                            var msg = _packet.ReadString();
                            var opt = _packet.ReadByte();

                            if (uid == 0)
                                throw new exception("[GameService::requestTranslateSubPacket][ID=" + (sub_packet_id) + "][Error] PLAYER[UID=" + (_session.m_pi.uid) + "] tentou mandar Message Off["
                                        + msg + "] para o PLAYER[UID=" + (uid) + "], mas m_uid is invalid(zero). Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 1, 0x5700101));

                            if (msg.empty())
                                throw new exception("[GameService::requestTranslateSubPacket][ID=" + (sub_packet_id) + "][Error] PLAYER[UID=" + (_session.m_pi.uid) + "] tentou mandar Message Off["
                                        + msg + "] para o PLAYER[UID=" + (uid) + "], mas msg is empty. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 2, 0x5700102));

                            if (msg.Length > 256)
                                throw new exception("[GameService::requestTranslateSubPacket][ID=" + (sub_packet_id) + "][Error] PLAYER[UID=" + (_session.m_pi.uid) + "] tentou mandar Message Off["
                                        + msg + "] para o PLAYER[UID=" + (uid) + "], mas o tamanho[SIZE=" + (msg.Length) + "] da message eh maior que o limite suportado. Hacker ou Bug",
                                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 3, 0x5700103));

                            if (opt != 0)
                                throw new exception("[GameService::requestTranslateSubPacket][ID=" + (sub_packet_id) + "][Error] PLAYER[UID=" + (_session.m_pi.uid) + "] tentou mandar Message Off["
                                        + msg + "] para o PLAYER[UID=" + (uid) + "], mas opt[VALUE=" + (opt) + " eh diferente de 0. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 4, 0x5700104));

                            if (_session.m_pi.ui.pang < 10)
                                throw new exception("[GameService::requestTranslateSubPacket][ID=" + (sub_packet_id) + "][Error] PLAYER[UID=" + (_session.m_pi.uid) + "] tentou mandar Message Off["
                                        + msg + "] para o PLAYER[UID=" + (uid) + "], mas ele nao tem pang(s) suficiente[have=" + (_session.m_pi.ui.pang) + ", request=10]. Hacker ou Bug",
                                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 5, 0x5700105));

                            // UPDATE ON SERVER
                            _session.m_pi.consomePang(10);

                            // UPDATE ON DB
                            snmdb.NormalManagerDB.getInstance().add(4, new CmdInsertMsgOff(_session.m_pi.uid, uid, msg), SQLDBResponse, this);

                            // Log
                            _smp.message_pool.getInstance().push(new message("[GameService::requestTranslateSubPacket::MessageOff][Error] PLAYER[UID=" + (_session.m_pi.uid) + "] mandou Message Off["
                                    + msg + "] para o PLAYER[UID=" + (uid) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            // UPDATE ON GAME

                            // Resposta para Msg Off
                            p.init_plain(0x95);

                            p.WriteUInt16(sub_packet_id);   // Sub Packet Id

                            p.WriteUInt32(0);   // OK

                            p.WriteUInt64(_session.m_pi.ui.pang);

                            packet_func.session_send(p, _session, 1);

                        }
                        break;
                    case TranslationSubPacket.Friend_List:
                        throw new exception("[GameService::requestTranslateSubPacket][ID=" + (sub_packet_id) + "][Error] PLAYER[UID="
                    + (_session.m_pi.uid) + "] pediu a lista de amigos, mas ainda nao foi implementado essa funcionalidade.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 5, 0x5700105));
                    default:
                        break;
                }
            }
            catch (exception e)
            {
                _smp.message_pool.getInstance().push(new message("[GameService::requestTranslateSubPacket][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x95);

                p.WriteUInt16(sub_packet_id);   // Sub Packet Id

                p.WriteUInt32((ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) == STDA_ERROR_TYPE.GAME_SERVER) ? ExceptionError.STDA_SYSTEM_ERROR_DECODE_TYPE(e.getCodeError()) : 0x5700100);

                packet_func.session_send(p, _session, 1);

                throw;
            }
        }

        public void requestUCCWebKey(Player _session, packet _packet)
        {
            PangyaBinaryWriter p = new PangyaBinaryWriter();

            try
            {
                ctx_UCCWebKey ctx_uwk = new ctx_UCCWebKey();
                // opt (byte)
                ctx_uwk.opt = _packet.ReadByte();
                // uid (uint)
                ctx_uwk.uid = _packet.ReadUInt32();
                // seq (byte)
                ctx_uwk.seq = _packet.ReadByte();
                // item_id (int)
                ctx_uwk.item_id = _packet.ReadInt32();

                if (ctx_uwk.uid == 0)
                    throw new exception("[GameService::requestUCCWebKey][Error] PLAYER[UID=" + (_session.m_pi.uid) + "] tentou gerar chave web de UCC[ITEM_ID="
                            + (ctx_uwk.item_id) + "] do PLAYER[UID=" + (ctx_uwk.uid) + "], mas o uid do player eh invalid. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 1, 0x5100101));

                if (ctx_uwk.item_id <= 0)
                    throw new exception("[GameService::requestUCCWebKey][Error] PLAYER[UID=" + (_session.m_pi.uid) + "] tentou gerar chave web de UCC[ITEM_ID="
                            + (ctx_uwk.item_id) + "] do PLAYER[UID=" + (ctx_uwk.uid) + "], mas o item_id is invalid. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 2, 0x5100102));

                Player s = (Player)m_session_manager.findSessionByUID(ctx_uwk.uid);

                // ----------- PRECISA TERMINAR ELE AINDA, SÓ FUNCIONA PARA O DONO DA UCC ---------------------------
                // Player não está nesse server, se nao tiver, procura no banco de dados
                // [Já fiz] Por Hora envio error, por que não sei se os player que vão ver ucc de outro player envia esse pacote
                if (s == null)
                    throw new exception("[GameService::requestUCCWebKey][Error] PLAYER[UID=" + (_session.m_pi.uid) + "] tentou gerar chave web de UCC[ITEM_ID="
                            + (ctx_uwk.item_id) + "] do PLAYER[UID=" + (ctx_uwk.uid) + "], mas o player nao esta nesse server. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 3, 0x5100103));

                var pWi = s.m_pi.findWarehouseItemById(ctx_uwk.item_id);

                if (pWi == null)
                    throw new exception("[GameService::requestUCCWebKey][Error] PLAYER[UID=" + (_session.m_pi.uid) + "] tentou gerar chave web de UCC[ITEM_ID="
                            + (ctx_uwk.item_id) + "] do PLAYER[UID=" + (ctx_uwk.uid) + "], mas o ele nao tem a UCC. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 4, 0x5100104));

                // Gera Web Key UCC
                var cmd_guwk = new CmdGeraUCCWebKey(_session.m_pi.uid, pWi.id, true); // Waiter

                snmdb.NormalManagerDB.getInstance().add(0, cmd_guwk, null, null);

                if (cmd_guwk.getException().getCodeError() != 0)
                    throw cmd_guwk.getException();

                string key = cmd_guwk.getKey();

                // Log
                _smp.message_pool.getInstance().push(new message("[UCC::SelfDesignSystem::GeraWebKey][Log] PLAYER[UID=" + (_session.m_pi.uid) + "] gerou Web Key[KEY=" + key + "] da UCC[TYPEID="
                        + (pWi._typeid) + ", ID=" + (pWi.id) + "] do PLAYER[UID=" + (ctx_uwk.uid) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Resposta para Gera UCC Web Key
                p.init_plain(0x153);

                p.WriteByte(0); // OK
                p.WriteByte(1); // OK

                p.WriteInt32(pWi.id);
                p.WriteString(key);
                p.WriteByte(ctx_uwk.seq);

                packet_func.session_send(p, _session, 1);
            }
            catch (exception e)
            {
                _smp.message_pool.getInstance().push(new message("[GameService::requestUCCWebKey][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x153);

                p.WriteByte(1); // Error Acho
                p.WriteByte(1); // ACHO
                p.WriteUInt32((ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) == STDA_ERROR_TYPE.GAME_SERVER) ? ExceptionError.STDA_SYSTEM_ERROR_DECODE_TYPE(e.getCodeError()) : 0x5100100);

                packet_func.session_send(p, _session, 1);
            }
        }


        public void requestUCCSystem(Player _session, packet _packet)
        {
            _session.HandleUCC(_packet);
        }

        public void sendChannelListToSession(Player _session)
        {
            try
            {
                packet_func.session_send(packet_func.pacote04D(v_channel), _session);
            }
            catch (exception e)
            {
                _smp.message_pool.getInstance().push(new message("[GameService::sendChannelListToSession][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public PangyaSyncTimer MakeTime(uint milliseconds, Action job, bool autoRepeted = false)
        {
            if (job == null)
                throw new ArgumentException("[GameService::MakeTime] job is invalid");


            var timer = m_timer_mgr.CreateTimer(milliseconds, job, autoRepeted);
            if (timer == null)
                throw new Exception("[GameService::MakeTime] não conseguiu criar o timer");

            return timer;
        }

        public PangyaSyncTimer MakeTime(uint milliseconds, Action job, List<long> tableInterval, PangyaSyncTimer.TIMER_TYPE tipo = PangyaSyncTimer.TIMER_TYPE.PERIODIC)
        {
            if (job == null)
                throw new ArgumentException("[GameService::MakeTime] job is invalid");

            var timer = m_timer_mgr.CreateTimer(milliseconds, job, tableInterval, tipo);
            if (timer == null)
                throw new Exception("[GameService::MakeTime] não conseguiu criar o timer");

            return timer;
        }

        public PangyaSyncTimer MakeTime(uint milliseconds, List<long> tableInterval, Action job, PangyaSyncTimer.TIMER_TYPE tipo = PangyaSyncTimer.TIMER_TYPE.PERIODIC)
        {
            if (job == null)
                throw new ArgumentException("[GameService::MakeTime] job is invalid");

            var timer = m_timer_mgr.CreateTimer(milliseconds, job, tableInterval, tipo);
            if (timer == null)
                throw new Exception("[GameService::MakeTime] não conseguiu criar o timer");

            return timer;
        }

        public void unMakeTime(PangyaSyncTimer timer)
        {
            if (timer == null)
            {
                throw new exception("[GameService::unMakeTime][Error] Tentou deletar o timer, mas o argumento é null",
                    ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 52, 0));
            }

            m_timer_mgr.DeleteTimer(timer);
        }

        public string getClientVersionSideServer()
        {
            return m_si.version_client;
        }

        public bool getActiveRoomLog()
        {
            return m_active_room_log;
        }


        private void Version_Decrypt(ref uint packet_version)
        {
            string PacketVerKey = "{782AE110-2EEF-4c61-B030-A53F17634F7D}";

            byte[] tmpPVer = BitConverter.GetBytes(packet_version);
            int index = 0;

            for (int i = 0; i < PacketVerKey.Length; i++)
            {
                tmpPVer[index] ^= (byte)PacketVerKey[i];
                index = (index == 3) ? 0 : index + 1;
            }

            packet_version = BitConverter.ToUInt32(tmpPVer, 0);
        }
    }
}

namespace sgs
{
    public class gs : Singleton<Pangya_GameServer.GameServiceTcp.GameService>
    {
    }
}