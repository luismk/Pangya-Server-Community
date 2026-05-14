using Pangya_GameServer.Game.Base;
using Pangya_GameServer.Game.Manager;
using Pangya_GameServer.Game.System;
using Pangya_GameServer.Models;
using Pangya_GameServer.PacketFunc;
using Pangya_GameServer.UTIL;
using PangyaAPI.IFF.JP.Extensions;
using PangyaAPI.IFF.JP.Models.Data;
using PangyaAPI.Network.PangyaPacket;
using PangyaAPI.Network.PangyaSession;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Models;
using PangyaAPI.Utilities.Log;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using static Pangya_GameServer.Models.DefineConstants;
using static PangyaAPI.Utilities.Tools;
using static PangyaAPI.Utilities.UtilTime;
using PangyaAPI.IFF.JP.Models.Flags;
namespace Pangya_GameServer.Game.GameModes
{
    /// <summary>
    /// falta organizar e fazer, só é uma copia do GZ
    /// </summary>
    public class GrandPrix : TourneyBase, IDisposable
    {

        public const float TIME_BOOSTER_VELOCIDADE = 3.0f;
        GrandPrixData m_gp;
        List<GrandPrixRankReward> m_gp_reward = new List<GrandPrixRankReward>();
        List<Bot> m_bot = new List<Bot>();

        List<RankPlayerDisplayChracter> m_rank_player_display_char = new List<RankPlayerDisplayChracter>();

        TimerManager m_timer_manager = new TimerManager();
        TimerManager m_timer_manager_rule = new TimerManager();
        public Random rnd = new Random();
        LockManager m_lock_manager = new LockManager();
        bool m_grand_prix_state;
        void ONCE_PER_SHOT(
    string method,
    string msg,
    string flagFieldName,
    Player _session,
    out PlayerGameInfo pgi,
    Action action)
        {
            INIT_PLAYER_INFO(method, msg, _session, out pgi);

            m_lock_manager.@lock(_session);

            try
            {
                var flagField = pgi.GetType().GetField(flagFieldName);
                if (flagField == null)
                {
                    throw new Exception($"[GrandPrix::{method}][Error] Campo '{flagFieldName}' não encontrado no PlayerGameInfo.");
                }

                var flagObj = flagField.GetValue(pgi);

                if (Convert.ToByte(flagObj) > 0)
                {
                    _smp.message_pool.getInstance().push(new message(
                        $"[GrandPrix::{method}][Error] PLAYER[UID={_session.m_pi.uid}] já enviou esse pacote, ignorando.",
                        type_msg.CL_FILE_LOG_AND_CONSOLE));

                    action?.Invoke(); // Só chama se não for null
                    return;
                }

                // Marca como executado (usa o tipo original pra evitar problemas de compatibilidade)
                if (flagObj is byte)
                    flagField.SetValue(pgi, (byte)1);
                else if (flagObj is uint)
                    flagField.SetValue(pgi, 1u);
            }
            finally
            {
                m_lock_manager.unlock(_session);
            }
        }

        public GrandPrix(List<Player> _players,   RoomInfoEx _ri, RateValue _rv, bool _channel_rookie, GrandPrixData _gp) : base(_players, _ri, _rv, _channel_rookie)
        {
            this.m_gp = _gp;
            m_gp_reward = new List<GrandPrixRankReward>();
            m_bot = new List<Bot>();
            m_grand_prix_state = false;
            m_timer_manager = new TimerManager();
            m_timer_manager_rule = new TimerManager();
            m_lock_manager = new LockManager();

            // Treasure Hunter System. diminui o Course Jogado
            if (!sTreasureHunterSystem.getInstance().isLoad())
            {
                sTreasureHunterSystem.getInstance().load();
            }

            var course = sTreasureHunterSystem.getInstance().findCourse((byte)(m_ri.getMap() & 0x7F));

            if (course == null)
            {
                _smp.message_pool.getInstance().push(new message("[GrandPrix::GrandPrix][Error] tentou pegar o course do Treasure Hunter System, mas o course[COURSE=" + ((ushort)(m_ri.getMap() & 0x7F)) + "] nao existe no sistema", type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
            else
            {
                sTreasureHunterSystem.getInstance().updateCoursePoint(course, -1); // -1 ponto a cada jogo iniciado
            }

            // Aqui tem que inicializar os players info
            initAllPlayerInfo();

            // Load Grand Prix Rank Reward from iff
            m_gp_reward = sIff.getInstance().findGrandPrixRankReward(m_gp.TypeID_Link);

            if (m_gp == null) 
            {
                _smp.message_pool.getInstance().push(new message("[GrandPrix::Error] m_gp está NULL", type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            // Log para verificar o carregamento
            if (m_gp_reward != null && m_gp_reward.Count > 0)
            {
                m_gp_reward.Sort((a, b) => a.Rank.CompareTo(b.Rank));
            }
            else
            {
                _smp.message_pool.getInstance().push(new message("[GrandPrix::Error] Falha crítica: m_gp_reward retornou NULL ou nao tem premios para o GP TypeID: " + m_gp.TypeID_Link, type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            // Init Bots
            init_bots();

            // Class Grand Prix Counter Item Typeid
            uint class_gp_counter_typeid = 0;

            if (sIff.getInstance().isGrandPrixEvent(m_gp.ID))
            {

                class_gp_counter_typeid = 0x6C4000AEu;

            }
            else
            {

                switch ((GrandPrixData.GP_ABA)sIff.getInstance().getGrandPrixAbaType(m_gp.ID))
                {
                    case GrandPrixData.GP_ABA.ROOKIE:
                        class_gp_counter_typeid = 0x6C4000AAu;
                        break;
                    case GrandPrixData.GP_ABA.BEGINNER:
                        class_gp_counter_typeid = 0x6C4000ABu;
                        break;
                    case GrandPrixData.GP_ABA.JUNIOR:
                        class_gp_counter_typeid = 0x6C4000ACu;
                        break;
                    case GrandPrixData.GP_ABA.EVENT:
                        class_gp_counter_typeid = 0x6C4000ADu;
                        break;
                }

            }

            // Initialize achievement of players
            foreach (var el in m_players)
            {

                var pgi = INIT_PLAYER_INFO("GrandPrix",
                    "tentou inicializar o counter item do Grand Prix",
                    el);

                initAchievement(el);

                pgi.sys_achieve.incrementCounter(0x6C4000A9u);

                if (class_gp_counter_typeid > 0)
                {
                    pgi.sys_achieve.incrementCounter(class_gp_counter_typeid);
                }
            }

            // Consome os Tickets dos player que v o jogar o Grand Prix
            consomeTicket();

            // inicializa o jogo
            m_state = init_game();
        }

        ~GrandPrix()
        {
            Dispose(false);
        }

        public override void Dispose(bool disposing)
        {
            if (disposedValue) return;

            if (disposing)
            {
                m_grand_prix_state = false;

                if (m_game_init_state != 2)
                {
                    finish();
                }

                while (!PlayersCompleteGameAndClear())
                {
                    Thread.Sleep(500);
                }

                deleteAllPlayer();

                if (!m_bot.empty())
                {
                    m_bot.Clear();
                }

                // Clear timers
                clear_timers();
                LogDestruction();
            }
            base.Dispose(true);

        }

        public override void sendInitialData(Player _session)
        {

            var p = new PangyaBinaryWriter();

            try
            {
                if (Interlocked.Increment(ref m_sync_send_init_data) == m_players.Count())
                {
                    Interlocked.Exchange(ref m_sync_send_init_data, 0);

                    // Game Data Init
                    p.init_plain(0x76);

                    p.WriteByte(m_ri.tipo_show);
                    p.WriteUInt32(1);

                    p.WriteTime(m_start_time);

                    packet_func.game_broadcast(this,
                        p, 1);

                    // Aqui   os bots do GP
                    p.init_plain(0x256);

                    p.WriteUInt32(0); // OK [Option Error]

                    p.WriteUInt16((ushort)m_bot.Count());

                    foreach (var _bot in m_bot)
                    {
                        p.WriteUInt32(_bot.id);
                        p.WriteByte((byte)_bot.hole.Count());
                        foreach (var _hole in _bot.hole)
                            p.WriteBytes(_hole.ToArray());
                    }

                    packet_func.game_broadcast(this,
                        p, 1);

                    // Course
                    // Send Individual Packet to all players in game
                    foreach (var el in m_players)
                    {
                        base.sendInitialData(el);
                    }
                }
            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[GrandPrix::sendInitialData][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }


        public override bool deletePlayer(Player _session, int _option)
        {

            if (_session == null)
            {
                throw new exception("[GrandPrix::deletePlayer][Error] tentou deletar um player, mas o seu endereco eh null.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY,
                    50, 0));
            }

            if (getPlayerInfo((_session)) == null)
                return true;

            bool ret = false;

            try
            {
                var it = m_players.FirstOrDefault(c => c == _session);

                if (it != null)
                {
                    byte opt = 3; // Saiu Quitou

                    var pgi = INIT_PLAYER_INFO("deletePlayer",
                        "tentou sair do jogo",
                        _session);

                    // Para o tempo do hole do player
                    stopTime(_session);
                    stopTimeRule(_session);

                    var p = new PangyaBinaryWriter();

                    if (m_game_init_state == 1)
                    {

                        var sessions = getSessions(it);

                        requestFinishItemUsedGame((it)); // Salva itens usados no Tourney

                        // Rookie Grand Prix n o altera o info do player s  achievement
                        if (!(sIff.getInstance().getGrandPrixAbaType(m_gp.ID) == GrandPrixData.GP_ABA.ROOKIE && sIff.getInstance().isGrandPrixNormal(m_gp.ID)))
                        {
                            requestSaveInfo((it), (_option == 0x800) ? 5 /*N o conta quit*/ : 1); // Quitou ou tomou DC
                        }

                        //pgi.PCBangMascot = PlayerGameInfo::eFLAG_GAME::QUIT;
                        setGameFlag(pgi, PlayerGameInfo.eFLAG_GAME.QUIT);

                        // Resposta Player saiu do Jogo, tira ele do list de score
                        p.init_plain(0x61);

                        p.WriteInt32(it.m_oid);

                        packet_func.vector_send(p,
                            sessions, 1);

                        // Resposta Player saiu do jogo
                        sendUpdateState(_session, opt);

                        if (AllCompleteGameAndClear())
                        {
                            ret = true; // Termina o Tourney
                        }

                        sendUpdateInfoAndMapStatistics(_session, -1);

                    }
                    else if (m_game_init_state == 2 && !(pgi.finish_game == 1))
                    {

                        // Acabou

                        // Rookie Grand Prix n o altera o info do player s  achievement
                        if (!(sIff.getInstance().getGrandPrixAbaType(m_gp.ID) == GrandPrixData.GP_ABA.ROOKIE && sIff.getInstance().isGrandPrixNormal(m_gp.ID)))
                        {
                            requestSaveInfo((it), 0);
                        }
                    }

                    // Deleta o player por give up ou time out, ele conta os achievements dele, tem o counter item 0x6C400004u Normal Game Complete
                    // Envia os achievements para ele para ficar igual ao original
                    if (m_game_init_state == 1


                        && pgi.data.bad_condute >= 3
                        && (pgi.data.time_out > 0 || pgi.data.giveup > 0))
                    {

                        // Achievements
                        rain_hole_consecutivos_count(_session); // conta os achievement de chuva em holes consecutivas

                        score_consecutivos_count(_session); // conta os achievement de back-to-back(2 ou mais score iguais consecutivos) do player

                        rain_count(_session); // Aqui achievement de rain count

                        pgi.sys_achieve.incrementCounter(0x6C400004u /*Normal game complete*/);

                        // Achievement Aqui
                        pgi.sys_achieve.finish_and_update(_session);

                        // Resposta que tem sempre que acaba um jogo, n o sei o que   ainda, esse s  n o tem no HIO Event
                        p = new PangyaBinaryWriter((ushort)0x244);

                        p.WriteUInt32(0); // OK

                        packet_func.session_send(p,
                            _session, 1);

                        // Esse   novo do JP, tem Tourney, VS, Grand Prix, HIO Event, n o vi talvez tenha nos outros tamb m
                        p.init_plain(0x24F);

                        p.WriteUInt32(0); // OK

                        packet_func.session_send(p,
                            _session, 1);
                    }

                    // Delete Player
                    m_players.Remove(it);
                }
                else
                {
                    _smp.message_pool.getInstance().push(new message("[GrandPrix::deletePlayer][Warning] player ja foi excluido do base.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                }

                // Aqui se n o for true tem que ver se todos terminaram o hole e enviar o pacote255
                if (!ret && checkAllClearHoleAndClear())
                {
                    sendAllToNextHole();
                }
            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[GrandPrix::deletePlayer][Error] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Aqui se n o for true tem que ver se todos terminaram o hole e enviar o pacote255
                if (!ret && checkAllClearHoleAndClear())
                {
                    sendAllToNextHole();
                }
            }

            return ret;
        }

        public void deleteAllPlayer()
        {
            // Percorre de trás para frente
            for (int i = m_players.Count - 1; i >= 0; i--)
            {
                var player = m_players[i];
                if (player != null)
                {
                    var pgi = getPlayerInfo(player);
                    if (pgi != null)
                    {
                        deletePlayer(player, 0);
                    }
                }
            }
        }

        public override void requestFinishCharIntro(Player _session, packet _packet)
        {
            ////REQUEST_BEGIN("FinishCharIntro");

            var p = new PangyaBinaryWriter();

            try
            {

                // Chama a base para ela fazer a parte dela
                base.requestFinishCharIntro(_session, _packet);

                var pgi = INIT_PLAYER_INFO("requestFinishCharIntro",
                    "tentou finalizar o character intro do player",
                    _session);

                m_lock_manager.@lock(_session);

                // Aqui zera a PCBangMascot finish hole2 do player
                pgi.finish_hole2 = 0;
                pgi.finish_hole3 = 0;

                m_lock_manager.unlock(_session);

                // Aqui come a o tempo do hole do player
                if (m_gp.TimeHole > 0)
                {
                    startTime(_session);
                }

            }
            catch (exception e)
            {

                m_lock_manager.unlock(_session);

                _smp.message_pool.getInstance().push(new message("[GrandPrix::requestFinishCharIntro][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

        }
        public override void requestActiveBooster(Player _session, packet _packet)
        {
            ////REQUEST_BEGIN("ActiveBooster");

            var p = new PangyaBinaryWriter();

            try
            {


                // 3.f Velociade 2x Padr o do Time Booster, o player est  gastando o dele ou ele   premium user
                // 2.f < 3.f Velocidade do Booster 1.5x do Grand Prix que ele d  de gra a. Porem no Pangya JP n o tem esse Booster no Grand Prix
                float velocidade = _packet.ReadFloat();

                var pgi = INIT_PLAYER_INFO("requestActiveBooster",
                    "tentou ativar Time Booster no jogo",
                    _session);

                // Booster Normal
                if (velocidade >= TIME_BOOSTER_VELOCIDADE)
                {

                    if (_session.m_pi.m_cap.premium_user)
                    { // (n o  )!PREMIUM USER

                        var pWi = _session.m_pi.findWarehouseItemByTypeid(TIME_BOOSTER_TYPEID);

                        if (pWi == null)
                        {
                            throw new exception("[GrandPrix::requestActiveBooster][Error] PLAYER[UID=" + (_session.m_pi.uid) + "] tentou ativar time booster, mas ele nao tem o item passive. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GRAND_PRIX,
                                11, 0));
                        }

                        if (pWi.STDA_C_ITEM_QNTD <= 0)
                        {
                            throw new exception("[GrandPrix::requestActiveBooster][Error] PLAYER[UID=" + (_session.m_pi.uid) + "] tentou ativar time booster, mas ele nao tem quantidade suficiente[VALUE=" + (pWi.STDA_C_ITEM_QNTD) + ", REQUEST=1] do item de time booster.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY_BASE,
                                12, 0));
                        }

                        var it = pgi.used_item.v_passive.find(pWi._typeid);

                        if (it.Key <= 0)
                        {
                            throw new exception("[GrandPrix::requestActiveBooster][Error] PLAYER[UID = " + (_session.m_pi.uid) + "] tentou ativar time booster, mas ele nao tem ele no item passive usados do server. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GRAND_PRIX,
                                13, 0));
                        }

                        if ((short)it.Value.count >= pWi.STDA_C_ITEM_QNTD)
                        {
                            throw new exception("[GrandPrix::requestActiveBooster][Error] PLAYER[UID=" + (_session.m_pi.uid) + "] tentou ativar time booster, mas ele ja usou todos os time booster. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GRAND_PRIX,
                                14, 0));
                        }

                        // Add +1 ao item passive usado
                        it.Value.count++;

                    }
                    else
                    { // Soma +1 no contador de counter item do booster do player e passive item

                        pgi.sys_achieve.incrementCounter(0x6C400075u /*Passive Item*/);

                        pgi.sys_achieve.incrementCounter(0x6C400050u);
                    }

                }

                // Resposta para Active Booster
                p.init_plain(0xC7);

                p.WriteFloat(velocidade);
                p.WriteInt32(_session.m_oid);

                packet_func.session_send(p,
                    _session, 1);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[GrandPrix::requestActiveBooster][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }
        public override void requestStartTurnTime(Player _session, packet _packet)
        {
            ////REQUEST_BEGIN("StartTurnTime");

            try
            {

                var pgi = INIT_PLAYER_INFO("requestStartTurnTime",
                    "tentou comecar o tempo de rule do player",
                    _session);

                m_lock_manager.@lock(_session);

                // Limpa a PCBangMascot init shot
                pgi.init_shot = 0;

                m_lock_manager.unlock(_session);

                // Come a o tempo do Rule do Grand Prix
                if (m_gp.rule > 0)
                {
                    startTimeRule(_session);
                }

            }
            catch (exception e)
            {

                m_lock_manager.unlock(_session);

                _smp.message_pool.getInstance().push(new message("[GrandPrix::requestStartTurnTime][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void changeHole(Player _session)
        {

            updateTreasureHunterPoint(_session);

            if (checkEndGame(_session))
            {
                finish_grand_prix(_session, 0);
            }
            else
            {

                // Resposta terminou o hole
                updateFinishHole(_session, 1); // Terminou

                // Troquei o clear hole e giveup pelo a PCBangMascot finish hole. Agora est  OK
                if (checkAllClearHole())
                {

                    clearAllClearHole();

                    // Change Hole All Finish Hole
                    sendAllToNextHole();
                }
            }
        }
        public override void finishHole(Player _session)
        {

            try
            {

                ONCE_PER_SHOT("finishHole",
                    "tentou finalizar o hole",
                    "finish_hole3", _session, out PlayerGameInfo pgi, () => { return; });

                m_lock_manager.@lock(_session);

                // Para o tempo do player
                stopTime(_session);
                stopTimeRule(_session);

                // Se o player estiver feito give up ou dado time out, n o soma as penalidade que ele j  fez o score max mo
                if (pgi.data.time_out == 0u && pgi.data.giveup == 0u)
                {
                    // Adiciona as penalidade para as tacadas do player
                    pgi.data.tacada_num += (int)pgi.data.penalidade;
                }

                // finaliza os dados do hole Game::requestfinishHole
                requestFinishHole(_session, 0);

                // update itens usados no jogo
                requestUpdateItemUsedGame(_session);

                // Limpa flags das tacadas
                pgi.init_shot = 0;
                pgi.sync_shot_flag = 0;
                pgi.finish_shot = 0;

                // Libera
                m_lock_manager.unlock(_session);

                // Aqui j  tem uma sincroniza  o de todos players do game, 
                // se eu colocar para o locker do player liberar depois dele pode d  deadlock
                // Player terminou o hole agora pode trocar de hole
                setClearHole(pgi);

            }
            catch (exception e)
            {

                // Libera
                m_lock_manager.unlock(_session);

                _smp.message_pool.getInstance().push(new message("[GrandPrix::finishHole][ErrrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

        }
        public override void requestInitShot(Player _session, packet _packet)
        {

            try
            {


                ONCE_PER_SHOT("requestInitShot",
                    "tentou iniciar tacada no jogo",
                    "init_shot", _session, out PlayerGameInfo pgi, () => { return; });

                // Para(Stop) o tempo rule dele que acabou de tacar
                stopTimeRule(_session);

                // Chama o fun  o da classe pai
                base.requestInitShot(_session, _packet);

                // Verifica se as tr s tacadas foram recebidas e para para o proximo turno outro troca o hole
                //changeTurn(_session);

            }
            catch (exception e)
            {

                m_lock_manager.unlock(_session);

                _smp.message_pool.getInstance().push(new message("[GrandPrix::requestInitShot][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void requestSyncShot(Player _session, packet _packet)

        {

            try
            {
                ONCE_PER_SHOT("requestInitShot",
                   "tentou iniciar tacada no jogo",
                   "sync_shot_flag", _session, out PlayerGameInfo pgi, () => { return; });

                base.requestSyncShot(_session, _packet);

                // Verifica se as tr s tacadas foram recebidas e para para o proximo turno outro troca o hole
                changeTurn(_session);

            }
            catch (exception e)
            {

                m_lock_manager.unlock(_session);

                _smp.message_pool.getInstance().push(new message("[GrandPrix::requestSyncShot][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void finish_grand_prix(Player _session, int _option)
        {

            if (m_players.Count > 0 && m_game_init_state == 1)
            {

                var pgi = INIT_PLAYER_INFO("finish_grand_prix",
                    "tentou terminar o grand prix no jogo",
                    _session);

                if (pgi.flag == PlayerGameInfo.eFLAG_GAME.PLAYING)
                {

                    // Calcula os pangs que o player ganhou
                    requestCalculePang(_session);

                    // Rookie Grand Prix s  da 1/3 dos pangs ganhos
                    if (sIff.getInstance().getGrandPrixAbaType(m_gp.ID) == GrandPrixData.GP_ABA.ROOKIE && sIff.getInstance().isGrandPrixNormal(m_gp.ID))
                    {
                        pgi.data.pang = (ulong)(pgi.data.pang * (1.0f / 3.0f));
                        pgi.data.bonus_pang = (ulong)(pgi.data.bonus_pang * (1.0f / 3.0f));
                    }

                    // Atualizar os pang do player se ele estiver com assist ligado, e for maior que beginner E
                    updatePlayerAssist(_session);

                    if (m_game_init_state == 1 && _option == 0)
                    {

                        // Mostra msg que o player terminou o jogo
                        sendFinishMessage(_session);

                        // Resposta terminou o hole
                        updateFinishHole(_session, 1);

                        // Resposta Terminou o Jogo, ou Saiu
                        sendUpdateState(_session, 2);

                        // Achievement Counter
                        pgi.sys_achieve.incrementCounter(0x6C400004u /*Normal game complete*/);

                    }
                    else if (m_game_init_state == 1 && _option == 1)
                    { // Acabou o Tempo

                        requestFinishHole(_session, 1); // Acabou o Tempo

                        // Mostra msg que o player terminou o jogo
                        sendFinishMessage(_session);

                        // Resposta terminou o hole
                        updateFinishHole(_session, 0);

                        // Resposta para acabou o tempo do Tourney
                        sendTimeIsOver(_session);
                    }
                }

                setGameFlag(pgi, (_option == 0) ? PlayerGameInfo.eFLAG_GAME.FINISH : PlayerGameInfo.eFLAG_GAME.END_GAME);

                pgi.time_finish.CreateTime();

                if (AllCompleteGameAndClear() && m_game_init_state == 1)
                {
                    finish(); // Envia os pacotes que termina o jogo Ex: 0xCE, 0x79 e etc
                }
            }
        }

        public void startTime(object _quem)
        {

            try
            {

                if (_quem != null)
                {
                    Player p = (Player)(_quem);

                    // Para Tempo se j  estiver 1 timer
                    var timer = m_timer_manager.findTimer(p);

                    // N o tem um timer criado ainda, cria um para ele
                    if (timer == null || timer.m_timer == null)
                    {
                        if (timer == null && (timer = m_timer_manager.insertTimer(p, sgs.gs.getInstance().MakeTime((uint)(m_gp.TimeHole * 1000), null, () => end_time(this, _quem), PangyaSyncTimer.TIMER_TYPE.NORMAL))) == null)
                        {
                            throw new exception("[GrandPrix::startTime][Error] PLAYER[UID=" + Convert.ToString(p.m_pi.uid) + "] nao conseguiu criar um timer_ctx para poder criar um timer para o player. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GRAND_PRIX,
                                1050, 0));
                        }
                    }
                    else
                    {

                        // J  tem um timer, reseta ele e inicia novamente
                        if (timer.m_timer != null)
                        {
                            if (timer.m_timer.getState() != PangyaSyncTimer.TIMER_STATE.STOP || timer.m_timer.getState() != PangyaSyncTimer.TIMER_STATE.FINISH)
                                timer.m_timer.Stop();

                            // inicia ele novamente, melhor desta forma
                            timer.m_timer = sgs.gs.getInstance().MakeTime((uint)(m_gp.TimeHole * 1000), null, () => end_time(this, _quem), PangyaSyncTimer.TIMER_TYPE.NORMAL);
                        }
                    }

                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[GrandPrix::startTime][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }
        public bool stopTime(object _quem)
        {

            bool ret = true;

            try
            {

                if (_quem != null && m_gp.TimeHole > 0)
                {


                    Player p = (Player)(_quem);
if(p != null)
{
	
                    var timer = m_timer_manager.findTimer(p);

                    if (timer.m_timer.getState() != PangyaSyncTimer.TIMER_STATE.STOP || timer.m_timer.getState() != PangyaSyncTimer.TIMER_STATE.FINISH)
                    {
                        timer.m_timer.Stop();
                    }
}
                }

            }
            catch (exception e)
            {

                ret = false;

                _smp.message_pool.getInstance().push(new message("[GrandPrix::stopTimer][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            return ret;
        }

        public void timeIsOver(object _quem)
        {

            try
            {

                if (_quem != null)
                {


                    var s = (Player)(_quem);

                    try
                    {

                        // Locker Player
                        m_lock_manager.@lock(s);

                        var timer = m_timer_manager.findTimer(s);

                        if (timer != null && timer.m_timer != null)
                        {

                            // Para o tempo se ele n o estiver parado
                            if (timer.m_timer.getState() != PangyaSyncTimer.TIMER_STATE.STOP || timer.m_timer.getState() != PangyaSyncTimer.TIMER_STATE.FINISH)
                            {
                                timer.m_timer.Stop();
                            }

                            // Atualiza os dados do player que ele fez give up por que o tempo do hole  dele acabou
                            var pgi = INIT_PLAYER_INFO("timeIsOver",
                                "acabou o tempo do hole do player",
                                s);

                            // Player ainda n o terminou o hole
                            if (pgi.finish_hole2 == 0u && pgi.finish_hole3 == 0u)
                            {

                                var hole = m_course.findHole(pgi.hole);

                                if (hole == null)
                                {
                                    throw new exception("[GrandPrix::timeIsOver][Error] PLAYER[UID=" + Convert.ToString(s.m_pi.uid) + "] tentou pegar hole[NUMERO=" + Convert.ToString((ushort)pgi.hole) + "] no jogo, mas o numero do hole is invalid. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GRAND_PRIX,
                                        1020, 0));
                                }

                                pgi.data.tacada_num = hole.getPar().total_shot; // Give up

                                // Fez time out
                                pgi.data.time_out = 1;

                                // Envia para o player que o tempo do hole acabou
                                var p = new PangyaBinaryWriter((ushort)0x259);

                                p.WriteUInt32(0); // OK

                                packet_func.session_send(p,
                                    s, 1);
                            }
                        }

                        // Libera
                        m_lock_manager.unlock(s);

                    }
                    catch
                    {
                        //ignorar:  UNREFERENCED_PARAMETER(e);

                        // Libera
                        m_lock_manager.unlock(s);

                        // Relan a para o outro try..catch exibir a mensagem no log
                        throw;
                    }

                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[GrandPrix::timeIsOver][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void requestCalculeRankPlace()
        {

            if (!m_player_order.empty())
            {
                m_player_order.Clear();
            }

            foreach (var el in m_player_info)
            {
                if (el.Value.flag != PlayerGameInfo.eFLAG_GAME.QUIT) // menos os que quitaram
                {
                    m_player_order.Add(el.Value);
                }
            }

            // Add os Bots
            foreach (var el in m_bot)
            {
                m_player_order.Add(el.pi);
            }
            m_player_order.Sort(sort_player_rank);
        }

        public override bool init_game()
        {

            if (m_players.Count > 0)
            {

                // variavel que salva a data local do sistema
                initGameTime();

                m_game_init_state = 1; // Come ou

                m_grand_prix_state = true;
            }

            return true;
        }
        public override int checkEndShotOfHole(Player _session)
        {

            // Agora verifica o se ele acabou o hole e essas coisas
            var pgi = INIT_PLAYER_INFO("checkEndShotOfHole",
                "tentou verificar a ultima tacada do hole no jogo",
                _session);

            if (pgi.shot_sync.state_shot.display.acerto_hole || pgi.data.giveup > 0)
            {

                if (pgi.data.bad_condute >= 3)
                { // Kika player deu 3 give up

                    // !!@@@
                    // Tira o player da sala
                    return 2;
                }

                // Verifica se o player terminou jogo, fez o ultimo hole
                if (m_course.findHoleSeq(pgi.hole) == m_ri.qntd_hole)
                {

                    // Resposta para o player que terminou o ultimo hole do Game
                    var p = new PangyaBinaryWriter((ushort)0x199);

                    packet_func.session_send(p,
                        _session, 1);

                    // Fez o Ultimo Hole, Calcula Clear Bonus para o player
                    if (pgi.shot_sync.state_shot.display.clear_bonus)
                    {

                        if (!MapSystem.getInstance().isLoad())
                        {
                            MapSystem.getInstance().load();
                        }

                        var map = MapSystem.getInstance().getMap((byte)(m_ri.getMap() & 0x7F));

                        if (map == null)
                        {
                            _smp.message_pool.getInstance().push(new message("[GrandPrix::checkEndShotOfHole][Error][Warning] tentou pegar o Map dados estaticos do course[COURSE=" + Convert.ToString((ushort)((byte)(m_ri.getMap() & 0x7F))) + "], mas nao conseguiu encontra na classe do Server.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                        }
                        else
                        {
                            pgi.data.bonus_pang += MapSystem.getInstance().calculeClear30s(map, m_ri.qntd_hole);
                        }
                    }
                }

                finishHole(_session);

                changeHole(_session);

            }
            else
            {
                clearAllShotPacket(_session);
            }

            return 0;
        }
        public override void requestTranslateSyncShotData(Player _session, ShotSyncData _ssd)
        {
            //CHECK_SESSION_BEGIN("requestTranslateSyncShotData");

            try
            {

                // !@ Teste
                //Sleep(5000);

                var s = findSessionByOID(_ssd.oid);

                if (s == null)
                {
                    throw new exception("[GrandPrix::requestTranslateSyncShotData][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou sincronizar tacada do PLAYER[OID=" + Convert.ToString(_ssd.oid) + "], mas o player nao existe nessa jogo. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GRAND_PRIX,
                        200, 0));
                }

                // Bloquea o player o tempo
                m_lock_manager.@lock(_session);

                // Update Sync Shot Player
                if (_session.m_pi.uid == s.m_pi.uid)
                {

                    var pgi = INIT_PLAYER_INFO("requestTranslateSyncShotData",
                        "tentou sincronizar a tacada no jogo",
                        _session);

                    pgi.shot_sync = _ssd;

                    // Last Location Player
                    var last_location = pgi.location;

                    // Update Location Player
                    pgi.location.x = _ssd.location.x;
                    pgi.location.z = _ssd.location.z;

                    // Update Pang and Bonus Pang
                    pgi.data.pang = _ssd.pang;
                    pgi.data.bonus_pang = _ssd.bonus_pang;

                    // J  s  na fun  o que come a o tempo do player do turno
                    pgi.data.tacada_num++;

                    if (_ssd.state == ShotSyncData.SHOT_STATE.OUT_OF_BOUNDS || _ssd.state == ShotSyncData.SHOT_STATE.UNPLAYABLE_AREA)
                    {
                        pgi.data.tacada_num++;
                    }

                    // Verifica se o Grand Prix tem regras especiais e se a regra   de n o poder fazer uma tacada especial
                    // Se sim a penalidade   +1 na tacada do player
                    if ((eRULE)m_gp.rule == eRULE.SPECIAL_SHOT && _ssd.state_shot.display.special_shot)
                    {
                        pgi.data.penalidade++;
                    }

                    // Hole find
                    var hole = m_course.findHole(pgi.hole);

                    if (hole == null)
                    {
                        throw new exception("[GrandPrix::requestTranslateSyncShotData][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou sincronizar tacada no hole[NUMERO=" + Convert.ToString((ushort)pgi.hole) + "], mas o numero do hole is invalid. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GRAND_PRIX,
                            12, 0));
                    }

                    // Conta j  a pr xima tacada, no give up
                    if (!_ssd.state_shot.display.acerto_hole && hole.getPar().total_shot <= (pgi.data.tacada_num + 1))
                    {

                        // +1 que   give up, s  add se n o passou o n mero de tacadas
                        if (pgi.data.tacada_num < hole.getPar().total_shot)
                        {
                            pgi.data.tacada_num++;
                        }

                        pgi.data.giveup = 1;

                        // Soma +1 no Bad Condute
                        pgi.data.bad_condute++;
                    }

                    // Acabou o hole para o tempo do hole do player
                    if (_ssd.state_shot.display.acerto_hole || pgi.data.giveup > 0)
                    {

                        // seta PCBangMascot finish hole2 do player
                        pgi.finish_hole2 = 1;

                        // Para o tempo do player
                        stopTime(_session);
                        stopTimeRule(_session);
                    }

                    // aqui os achievement de power shot int32_t putt beam impact e etc
                    update_sync_shot_achievement(_session, last_location);
                }

                // Libera
                m_lock_manager.unlock(_session);

            }
            catch (exception e)
            {

                // Libera
                m_lock_manager.unlock(_session);

                _smp.message_pool.getInstance().push(new message("[GrandPrix::requestTranslateSyncShotData][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void init_bots()
        {

            // Achievement Bosts
            uint bots_counter_typeid = 0;

            if (m_players.Count == 30)
            {
                bots_counter_typeid = 0x6C4000B8u; // No AI, All player
            }
            else if (m_players.Count == 1)
            {
                bots_counter_typeid = 0x6C4000B7u; // All AI, 1 player
            }

            if (bots_counter_typeid > 0u)
            {

                foreach (var el in m_players)
                {

                    var pgi = INIT_PLAYER_INFO("GrandPrix",
                        "tentou inicializar o counter item do Grand Prix Bots",
                        el);

                    pgi.sys_achieve.incrementCounter(bots_counter_typeid);
                }
            }

            // Inicializa os bots
            if (m_players.Count < 30)
            {

                // Média de score (Avg. Score) dos players da sala
                float mediaScoreAllPlayerRoom = m_players
                    .Where(p => p != null)
                    .Select(p => p.m_pi.ui.getMediaScore())
                    .DefaultIfEmpty(0.0f)
                    .Average();

                // Lambda que atualiza o bot score com base na média da sala
                Func<GrandPrixData, float, GrandPrixData.BOT> lambdaBotScoreByFactorAvgScoreRoom = (gp, roomAvgScore) =>
                {
                    var _bot = new GrandPrixData.BOT();
                    byte qntdHole = (byte)(gp.course_info.Qntd_hole == 0 ? 18 : gp.course_info.Qntd_hole);
                    float mediaBot = ((gp.bot.ScoreBotMin + gp.bot.ScoreBotMed + gp.bot.ScoreBotMax) / 3.0f) * 1.7f;
                    float mediaScorePorHole = (((18.0f / qntdHole) * mediaBot + 72) - roomAvgScore + 180.0f) / 180.0f;

                    int bySign(int scores)
                    {
                        if (scores == 0)
                            return mediaScorePorHole <= 0.8f ? 1 : (mediaScorePorHole >= 1.4f ? -1 : 0);

                        if (scores < 0)
                            return (int)Math.Round(scores * mediaScorePorHole, MidpointRounding.AwayFromZero);

                        return (int)Math.Round(scores / (mediaScorePorHole == 0.0f ? 0.001f : mediaScorePorHole), MidpointRounding.AwayFromZero);
                    }

                    _bot.ScoreBotMin = bySign(gp.bot.ScoreBotMin);
                    _bot.ScoreBotMed = bySign(gp.bot.ScoreBotMed);
                    _bot.ScoreBotMax = bySign(gp.bot.ScoreBotMax);

                    return _bot;
                };

                var bot_score = lambdaBotScoreByFactorAvgScoreRoom(m_gp, mediaScoreAllPlayerRoom);

                var qntd = 30u - m_players.Count;

                var gp_ai = sIff.getInstance().getGrandPrixAIOptionalData();

                Lottery lottery = new Lottery();

                foreach (var el in gp_ai)
                {

                    if (el.Active == 1 && el.Class == m_gp._class)
                    {
                        lottery.Push(1000u, el.ID);
                    }

                }

                // Verifica se tem a quantidade necessária de bots para sortear
                if (Convert.ToInt64(lottery.getLimitProbilidade() / 1000u) < qntd)
                {

                    var rest_qntd = Convert.ToUInt64(qntd - (long)(lottery.getLimitProbilidade() / 1000));

                    foreach (var el in gp_ai)
                    {

                        if (el.Active == 1 && el.Class == m_gp._class)
                        {
                            lottery.Push(1000u, el.ID);

                            if (--rest_qntd == 0)
                            {
                                break;
                            }
                        }
                    }
                }

                // Sortea os Bots e configura eles
                Lottery.LotteryCtx lc = null;
                Lottery lottery_score = new Lottery();

                PlayerGameInfo tmp_pi = new PlayerGameInfo();

                HoleManager hole = null;

                Bot bot = new Bot();

                int score = 0;
                int min_shot = 0;
                int diff_min_shot = 0;
                int diff_max_shot = 0;
                ulong pang = 0;
                ulong bonus_pang = 0;

                // Media do bot se ele fizer par em todos os holes
                float media_all_par_hole = m_course.getMediaAllParHolesBySeq(m_ri.qntd_hole);

                Func<HoleManager, Bot.eTYPE_SCORE, bool, uint> lambdaWindFactor = (m_hole, type, sameType) =>
                {
                    uint factor = 1;
                    int wind = m_hole.getWind().wind;
                    int weather = m_hole.getWeather(); // 2 = chuva ou neve

                    // Vento leve e pontuação máxima
                    if (wind >= 0 && wind < 3 && type == Bot.eTYPE_SCORE.MAX_SCORE)
                    {
                        factor = 2;
                    }
                    // Vento médio e pontuação média
                    else if (wind >= 3 && wind < 6 && type == Bot.eTYPE_SCORE.MED_SCORE)
                    {
                        factor = 4;
                    }
                    // Vento forte e pontuação mínima
                    else if (wind >= 6 && wind < 8 && type == Bot.eTYPE_SCORE.MIN_SCORE)
                    {
                        factor = 6;
                    }
                    // Vento muito forte e pontuação mínima
                    else if (wind >= 8 && type == Bot.eTYPE_SCORE.MIN_SCORE)
                    {
                        factor = 7;
                    }

                    // Se tiver chuva ou neve (weather == 2)
                    if (weather == 2 && (type == Bot.eTYPE_SCORE.MED_SCORE || type == Bot.eTYPE_SCORE.MIN_SCORE))
                    {
                        factor += 2;
                    }

                    if (sameType)
                    {
                        factor += 2;
                    }

                    return factor;
                };

                for (var i = 0; i < qntd; ++i)
                {

                    if ((lc = lottery.spinRoleta(true)) != null)
                    {
                        bot.id = Convert.ToUInt32(lc.Value);// talvez aqui esteja errado, por causa do lottery

                        bot.type_score = (rnd.Next() % 5 == 0 ? Bot.eTYPE_SCORE.MAX_SCORE : (rnd.Next() % 3 == 0 ? Bot.eTYPE_SCORE.MED_SCORE : Bot.eTYPE_SCORE.MIN_SCORE));

                        bot.max_record = (bot.type_score == Bot.eTYPE_SCORE.MAX_SCORE ? bot_score.ScoreBotMax + (int)(rnd.Next() % 3) : (bot.type_score == Bot.eTYPE_SCORE.MED_SCORE ? bot_score.ScoreBotMed + (int)(rnd.Next(0, 6) - 3) : bot_score.ScoreBotMin + (int)(rnd.Next(0, 5) - 3)));

                        bot.qntd_hole = m_ri.qntd_hole;

                        for (var j = 0; j < m_ri.qntd_hole; ++j)
                        {
                            hole = m_course.findHoleBySeq((ushort)(j + 1));
                            if (hole != null)
                            {

                                // Score
                                bot.med_shot_per_hole = (int)Math.Round(((bot.qntd_hole - j + 1) * media_all_par_hole + (bot.max_record - bot.record)) / (float)(bot.qntd_hole - j + 1));

                                min_shot = (hole.getPar().par + ((m_ri.special_flag_mod.short_game) ? -2 /*Short Game*/ : hole.getPar().range_score[0]));

                                if (min_shot >= bot.med_shot_per_hole) // Limite de menor score do hole
                                {
                                    score = min_shot - hole.getPar().par;
                                }
                                else if (bot.med_shot_per_hole >= hole.getPar().total_shot) // Limite de maior score do hole
                                {
                                    score = hole.getPar().total_shot - hole.getPar().par;
                                }
                                else
                                {

                                    lottery_score.Clear();

                                    // Margem que tem para fazer um score melhor
                                    diff_min_shot = (bot.med_shot_per_hole - min_shot);

                                    // Margem que tem para fazer um score pior
                                    diff_max_shot = (hole.getPar().total_shot - bot.med_shot_per_hole);

                                    if (bot.med_shot_per_hole < hole.getPar().par)
                                    {

                                        // min shot, max score
                                        lottery_score.Push((uint)(1000u * diff_max_shot * lambdaWindFactor(hole,
                                            Bot.eTYPE_SCORE.MAX_SCORE,
                                            bot.type_score == Bot.eTYPE_SCORE.MAX_SCORE)), Bot.eTYPE_SCORE.MAX_SCORE);
                                    }

                                    // med
                                    lottery_score.Push((uint)(1000u * bot.med_shot_per_hole * lambdaWindFactor(hole,
                                        Bot.eTYPE_SCORE.MED_SCORE,
                                        bot.type_score == Bot.eTYPE_SCORE.MED_SCORE)), Bot.eTYPE_SCORE.MED_SCORE);

                                    // max shot, min score
                                    lottery_score.Push((uint)(1000u * diff_min_shot * lambdaWindFactor(hole,
                                        Bot.eTYPE_SCORE.MIN_SCORE,
                                        bot.type_score == Bot.eTYPE_SCORE.MIN_SCORE)), Bot.eTYPE_SCORE.MIN_SCORE);

                                    if ((lc = lottery_score.spinRoleta(true)) == null)
                                    {

                                        _smp.message_pool.getInstance().push(new message("[GrandPrix::init_bots][Warning] nao conseguiu rodar a roleta para o score do bot, usando o med_shot_per_hole.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                                        score = bot.med_shot_per_hole - hole.getPar().par;

                                    }
                                    else
                                    {

                                        if ((Bot.eTYPE_SCORE)lc.Value == Bot.eTYPE_SCORE.MAX_SCORE)
                                        {
                                            score = (bot.med_shot_per_hole - (int)rnd.Next(0, diff_min_shot)) - hole.getPar().par;
                                        }
                                        else if ((Bot.eTYPE_SCORE)lc.Value == Bot.eTYPE_SCORE.MED_SCORE)
                                        {
                                            score = bot.med_shot_per_hole - hole.getPar().par;
                                        }
                                        else
                                        {
                                            score = (bot.med_shot_per_hole + (int)rnd.Next(0, diff_max_shot)) - hole.getPar().par;
                                        }
                                    }
                                }

                                // Pang e Bonus pang
                                pang = (ulong)(rnd.Next() % (351 * (hole.getWeather() == 2 ? 2 : 1)));
                                bonus_pang = (ulong)rnd.Next() % 200Ul;

                                // Insere o Hole (i) do Bot
                                bot.hole.Add(new Bot.Hole((byte)(m_ri.getMap() & 0x7F),
                                    hole.getNumero(), score, pang,
                                    bonus_pang));

                                // Incrementa no total
                                bot.record += score;
                                bot.pang_total += pang;
                                bot.bonus_pang_total += bonus_pang;

                            } // If course->findHole

                            else
                            {
                                _smp.message_pool.getInstance().push(new message($"[GrandPrix::init_bots][ERROR] Hole não encontrado para seq {(j + 1)}", type_msg.CL_FILE_LOG_AND_CONSOLE));
                                break;
                            }
                        } // For Hole Bot

                        if (bot.qntd_hole != (byte)bot.hole.Count)
                        {
                            _smp.message_pool.getInstance().push(new message("[GrandPrix::init_bots][WARNIG] Bot[ID=" + Convert.ToString(bot.id) + ", HOLE_QNTD_INIT=" + Convert.ToString(bot.hole.Count) + ", HOLE_QNTD_GP=" + Convert.ToString((ushort)bot.qntd_hole) + "] qntd de holes inicializado esta diferente da quantidade de holes da sala Grand Prix. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                        }

                        tmp_pi = new PlayerGameInfo();

                        tmp_pi.flag = PlayerGameInfo.eFLAG_GAME.BOT;
                        tmp_pi.data.score = bot.record;
                        tmp_pi.data.pang = bot.pang_total;
                        tmp_pi.data.bonus_pang = bot.bonus_pang_total;

                        bot.pi = tmp_pi;

                        // Add bot ao vector
                        m_bot.Add(bot);
                        // Clear Bot para new data
                        bot = new Bot();

                    } // If lottery.spinRoleta

                } // For Rest of Bot

                m_bot.Sort((bot1, bot2) =>
                {
                    if (bot1.record == bot2.record)
                        return bot2.pang_total.CompareTo(bot1.pang_total); // decrescente
                    return bot1.record.CompareTo(bot2.record); // crescente
                });

            } // If players.size < 30
        }

        public void consomeTicket()
        {

            foreach (var el in m_players)
            {

                try
                {

                    // Tira o ticket Grand Prix do player
                    var pWi = el.m_pi.findWarehouseItemByTypeid(m_gp.ticket._typeid) ?? throw new exception("[GrandPrix::consomeTicket][Error] PLAYER[UID=" + Convert.ToString(el.m_pi.uid) + "] tentou comecar o jogo na sala[NUMERO=" + Convert.ToString(m_ri.numero) + ", MASTER=" + Convert.ToString(m_ri.master) + "], mas o player nao tem o Ticket[TYPEID=" + Convert.ToString(m_gp.ticket._typeid) + "] para jogar o Grand Prix. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GRAND_PRIX,
                            9, 0x5900203));

                    if (pWi.STDA_C_ITEM_QNTD < (short)m_gp.ticket.qntd)
                    {
                        throw new exception("[GrandPrix::consomeTicket][Error] PLAYER[UID=" + Convert.ToString(el.m_pi.uid) + "] tentou comecar o jogo na sala[NUMERO=" + Convert.ToString(m_ri.numero) + ", MASTER=" + Convert.ToString(m_ri.master) + "], mas o player nao tem a quantidade de Ticket[TYPEID=" + Convert.ToString(m_gp.ticket._typeid) + ", REQ_QNTD=" + Convert.ToString(m_gp.ticket.qntd) + ", HAVE_QNTD=" + Convert.ToString(pWi.STDA_C_ITEM_QNTD) + "] para jogar o Grand Prix. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GRAND_PRIX,
                            10, 0x5900203));
                    }

                    stItem item = new stItem
                    {
                        type = 2,
                        id = pWi.id,
                        _typeid = pWi._typeid,
                        qntd = (int)m_gp.ticket.qntd
                    };
                    item.STDA_C_ITEM_QNTD = (item.qntd * -1);

                    // Update On Server And Database
                    if (ItemManager.removeItem(item, el) <= 0)
                    {
                        throw new exception("[GrandPrix::consomeTicket][Error] PLAYER[UID=" + Convert.ToString(el.m_pi.uid) + "] tentou comecar o jogo na sala[NUMERO=" + Convert.ToString(m_ri.numero) + ", MASTER=" + Convert.ToString(m_ri.master) + "], mas nao conseguiu excluir o Ticket[TYPEID=" + Convert.ToString(item._typeid) + "] do player. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GRAND_PRIX,
                            11, 0x5900203));
                    }


                    // Update Grand Prix Ticket do player no jogo
                    var p = new PangyaBinaryWriter((ushort)0x216);

                    p.WriteUInt32((uint)GetSystemTimeAsUnix());
                    p.WriteUInt32(1); // Count

                    p.WriteByte(item.type);
                    p.WriteUInt32(item._typeid);
                    p.WriteInt32(item.id);
                    p.WriteUInt32(item.flag_time);
                    p.WriteBytes(item.stat.ToArray());
                    p.WriteInt32((item.STDA_C_ITEM_TIME > 0) ? item.STDA_C_ITEM_TIME : item.STDA_C_ITEM_QNTD);
                    p.WriteZeroByte(25);

                    packet_func.session_send(p,
                        el, 1);

                }
                catch (exception e)
                {

                    _smp.message_pool.getInstance().push(new message("[GrandPrix::consomeTicket][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                    if (ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) != STDA_ERROR_TYPE.GRAND_PRIX)
                    {
                        throw;
                    }
                }

            }
        }

        public void requestFinishExpGame()
        {

            if (m_players.Count > 0)
            {
                float stars = m_course.getStar();
                int exp = 0;



                // Exp padr�o de hole do Grand Prix
                switch (m_ri.qntd_hole)
                {
                    case 3:
                        exp = 2;
                        break;
                    case 6:
                        exp = 4;
                        break;
                    case 9:
                        exp = 5;
                        break;
                    case 18:
                        exp = 7;
                        break;
                    default:
                        exp = 1;
                        break;
                }

                stars = (stars < 1.1f) ? 1.1f : stars;

                stars = ((stars - 1.1f) * 0.125f) + 1.0f;

                exp = (int)(exp * stars);
                // Grand Prix Rookie d  um pouco menos de experi ncia
                if (sIff.getInstance().getGrandPrixAbaType(m_gp.ID) == GrandPrixData.GP_ABA.ROOKIE && sIff.getInstance().isGrandPrixNormal(m_gp.ID))
                {
                    exp = (int)(exp * 0.12f);
                }

                for (var i = 0; i < m_player_order.Count; ++i)
                {
                    Player _session = default;
                    if (m_player_order[i].flag != PlayerGameInfo.eFLAG_GAME.BOT
                        && m_player_order[i].flag == PlayerGameInfo.eFLAG_GAME.FINISH
                        && (_session = findSessionByUID(m_player_order[i].uid)) != null)
                    {

                        // Rate do player e do server
                        exp = (int)(exp * TRANSF_SERVER_RATE_VALUE(m_player_order[i].used_item.rate.exp) * TRANSF_SERVER_RATE_VALUE(m_rv.exp));

                        // Exp que o player ganhou
                        if (m_player_order[i].level < 70 /*Ultimo level n o ganha exp*/)
                        {
                            m_player_order[i].data.exp = exp;
                        }
                    }
                }
            }
        }

        public void requestMakeRankPlayerDisplayCharacter()
        {

            if (m_player_order.Count <= 0)
            {
                requestCalculeRankPlace();
            }

            RankPlayerDisplayChracter rpdc = new RankPlayerDisplayChracter();

            Player p = null;

            // Top 3
            for (var i = 0; i < m_player_order.Count && i < 3u; ++i)
            {

                if (m_player_order[i].flag != PlayerGameInfo.eFLAG_GAME.BOT)
                {

                    if ((p = findSessionByUID(m_player_order[i].uid)) != null)
                    {

                        rpdc = new RankPlayerDisplayChracter();

                        rpdc.uid = p.m_pi.uid;
                        rpdc.rank = (uint)(i + 1);

                        if (p.m_pi.ei.char_info != null)//no meu antigo, esta null no packet, agora ta preenchendo
                        {

                            rpdc.default_hair = p.m_pi.ei.char_info.default_hair;
                            rpdc.default_shirts = p.m_pi.ei.char_info.default_shirts;
                            Array.Copy(
                                p.m_pi.ei.char_info.parts_typeid, // origem
                                rpdc.parts_typeid,                // destino
                                rpdc.parts_typeid.Length          // tamanho (em elementos, não bytes)
                            );

                            Array.Copy(
                                p.m_pi.ei.char_info.auxparts,
                                rpdc.auxparts,
                                rpdc.auxparts.Length
                            );

                            Array.Copy(
                                p.m_pi.ei.char_info.parts_id,
                                rpdc.parts_id,
                                rpdc.parts_id.Length
                            );

                        }

                        // Add para o vector
                        m_rank_player_display_char.Add(rpdc);
                    }
                }
            }

            m_rank_player_display_char.Sort((a, b) => a.rank.CompareTo(b.rank));

        }

        public void finish()
        {

            m_game_init_state = 2; // Acabou

            // J  est  com os bots incluso, fiz um overide dessa fun  o
            requestCalculeRankPlace();

            requestMakeRankPlayerDisplayCharacter();

            requestFinishExpGame();

            foreach (var el in m_players)
            {

                var pgi = INIT_PLAYER_INFO("finish",
                    "tentou finalizar os dados do jogador no jogo",
                    el);

                if (pgi.flag != PlayerGameInfo.eFLAG_GAME.QUIT)
                {
                    requestFinishData(el);
                }
            }
        }

        public void requestFinishData(Player _session)
        {

            // Finish Artefact Frozen Flame agora   direto no Finish Item Used Game
            requestFinishItemUsedGame(_session);

            requestSaveDrop(_session);

            rain_hole_consecutivos_count(_session); // conta os achievement de chuva em holes consecutivas

            score_consecutivos_count(_session); // conta os achievement de back-to-back(2 ou mais score iguais consecutivos) do player

            rain_count(_session); // Aqui achievement de rain count

            achievement_top_3_1st(_session); // Se o Player ficou em Top 3 add +1 ao contador de top 3, e se ele ficou em primeiro add +1 ao do primeiro

            var pgi = INIT_PLAYER_INFO("requestFinishData",
                "tentou finalizar dados do jogo",
                _session);

            // Resposta terminou game - Drop Itens
            sendDropItem(_session);

            // Resposta terminou game - Placar
            sendPlacar(_session);

            // Aqui   os 3 player do podio, mas s  player bot n o vai n o
            sendRankPlayerDisplayCharacter(_session);

            // Trof u que o player ganhou
            sendTrofel(_session);

            // Envia os pr mios que o player ganhou no Grand Prix
            sendRewardRankAndGrandPrix(_session);

            // Verifica se o player concluiu esse Grand Prix em um posi  o melhor, 
            // se sim atualiza no server, db e no jogo
            requestSaveGrandPrixClear(_session);
        }

        public void requestSaveGrandPrixClear(Player _session)
        {

            try
            {

                if (m_player_order.Count <= 0)
                {
                    requestCalculeRankPlace();
                }

                var it = m_player_order.FindIndex(_el => _el.uid == _session.m_pi.uid);
                var position = it != -1 ? it + 1 : 0; // Se não encontrar, posição 0 (equivalente a não encontrado)

                // Atualiza Grand Prix Clear do player
                if (_session.m_pi.updateGrandPrixClear(m_gp.TypeID_Link, (int)position))
                {

                    // Update On Game
                    var p = new PangyaBinaryWriter((ushort)0x25A);

                    p.WriteUInt32(0); // OK;

                    p.WriteUInt32(m_gp.TypeID_Link);
                    p.WriteUInt32((uint)position);

                    packet_func.session_send(p,
                        _session, 1);
                }

            }
            catch (exception e)
            {
                _smp.message_pool.getInstance().push(new message("[requestSaveGrandPrixClear][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void sendTrofel(Player _session)
        {

            uint all_player = getCountPlayersGame();

            if (m_player_order.Count <= 0)
            {
                requestCalculeRankPlace();
            }
            if (m_player_order.Count != (all_player + m_bot.Count))
            {

                _smp.message_pool.getInstance().push(new message("[GrandPrix::sendTrofel][Error] VALUES[ORDER=" + Convert.ToString(m_player_order.Count) + ", INFO=" + Convert.ToString(m_player_info.Count) + ", BOT=" + Convert.ToString(m_bot.Count) + "] nao conseguiu gerar os trofeus por que o vector de player rank order nao bate com o dos players no jogo", type_msg.CL_FILE_LOG_AND_CONSOLE));

                return;
            }

            // D  os Trof us para os 3 primeiros Player, bots est o exclu dos
            if (m_gp_reward.Count > 0)
            {

                GrandPrixRankReward gprr = new GrandPrixRankReward();
                stItem item = new stItem();

                var p = new PangyaBinaryWriter();

                var it = m_player_order.FirstOrDefault(_el => _el.uid == _session.m_pi.uid);

                if (it != null)
                {

                    try
                    { 
                        int index = m_player_order.FindIndex(el => el.uid == _session.m_pi.uid);
                        if (index >= 0 && index < m_gp_reward.Count && m_gp_reward.Count > 0)//verifica se tem premios
                        {
                            gprr = m_gp_reward[index];

                            // Inicializa o Trof u
                            item.type = 2;
                            item.id = -1;
                            item._typeid = gprr.Trophy;
                            item.qntd = 1;
                            item.STDA_C_ITEM_QNTD = (short)item.qntd;

                            // Update on Server and Database
                            if (ItemManager.addItem(item, _session, 0, 0) >= ItemManager.RetAddItem.T_SUCCESS)
                            {
                                // Update Trof u on Game
                                p.init_plain(0x25C);

                                p.WriteUInt32(0); // OK;

                                p.WriteUInt32(gprr.Trophy);

                                packet_func.session_send(p,
                                    _session, 1);

                                // Update Item on Game (Trof u)
                                p.init_plain(0x216);

                                p.WriteUInt32((uint)GetSystemTimeAsUnix());
                                p.WriteUInt32(1u); // Count

                                p.WriteByte(item.type);
                                p.WriteUInt32(item._typeid);
                                p.WriteInt32(item.id);
                                p.WriteUInt32(item.flag_time);
                                p.WriteBytes(item.stat.ToArray());
                                p.WriteInt32((item.STDA_C_ITEM_TIME > 0) ? item.STDA_C_ITEM_TIME : item.STDA_C_ITEM_QNTD);
                                p.WriteZeroByte(25);

                                packet_func.session_send(p,
                                    _session, 1);

                            }
                        }
                        else
                        { 
                            _smp.message_pool.getInstance().push(new message("[GrandPrix::sendTrofel][Log] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] nao tem trofeu nessa room.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                        }
                    }
                    catch (IndexOutOfRangeException e)
                    {

                        _smp.message_pool.getInstance().push(new message("[GrandPrix::sendTrofel][IndexOutOfRangeException] " + e.StackTrace, type_msg.CL_FILE_LOG_AND_CONSOLE));
                    }
                    catch (exception e)
                    {

                        _smp.message_pool.getInstance().push(new message("[GrandPrix::sendTrofel][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                    }

                }
                else
                {
                    _smp.message_pool.getInstance().push(new message("[GrandPrix::sendTrofel][Warning] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] nao esta no vector de player order.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
            }
        }

        public void sendRankPlayerDisplayCharacter(Player _session)
        {

            // 3 Players do P dio
            var p = new PangyaBinaryWriter((ushort)0x258);

            p.WriteUInt32(0u); // OK

            p.WriteByte((byte)m_rank_player_display_char.Count); // count

            foreach (var el in m_rank_player_display_char)
            {
                p.WriteBytes(el.ToArray());
            }

            packet_func.session_send(p,
                _session, 1);
        }

        public void sendRewardRankAndGrandPrix(Player _session)
        {
            // 1. Garante que o rank foi calculado
            if (m_player_order.Count <= 0)
            {
                requestCalculeRankPlace();
            }

            // 2. Encontra a info do player no ranking do jogo atual
            PlayerGameInfo it = m_player_order.Find(_el => _el.uid == _session.m_pi.uid);

            if (it != null && m_gp != null && m_gp.ID != 0)
            {
                List<stItem> v_item = new List<stItem>();

                // --- PARTE A: RECOMPENSA GERAL DO GRAND PRIX (PARTICIPAÇÃO) ---
                for (int i = 0; i < 5; i++)
                {
                    if (m_gp.reward._typeid[i] == 0) continue;

                    // Instancia um novo objeto stItem para não sobrescrever referências
                    stItem itemGP = new stItem
                    {
                        type = 2,
                        id = -1,
                        _typeid = m_gp.reward._typeid[i]
                    };

                    // Configuração de Tempo ou Quantidade
                    if (m_gp.reward.time[i] > 0)
                    {
                        itemGP.qntd = 1;
                        itemGP.STDA_C_ITEM_QNTD = 1;
                        itemGP.STDA_C_ITEM_TIME = (short)m_gp.reward.time[i];
                        itemGP.flag_time = 4; // Dias
                        itemGP.flag = 0x40;   // Flag de tempo
                    }
                    else
                    {
                        itemGP.qntd = (int)m_gp.reward.qntd[i];
                        itemGP.STDA_C_ITEM_QNTD = (short)itemGP.qntd;
                    }

                    // Adiciona se puder acumular ou se o player não possuir
                    if ((sIff.getInstance().IsCanOverlapped(itemGP._typeid) && sIff.getInstance().getItemGroupIdentify(itemGP._typeid) != IFF_GROUP.CAD_ITEM) || !_session.m_pi.ownerItem(itemGP._typeid))
                    {
                        if (ItemManager.isSetItem(itemGP._typeid))
                        {
                            var v_stItem = ItemManager.getItemOfSetItem(_session, itemGP._typeid, false, 1);
                            foreach (var el in v_stItem) v_item.Add(new stItem(el));
                        }
                        else
                        {
                            v_item.Add(itemGP);
                        }
                    }
                }

                // --- PARTE B: RECOMPENSA DE RANK (POSIÇÃO) ---
                int index = m_player_order.FindIndex(el => el.uid == _session.m_pi.uid);

                // CORREÇÃO: index >= 0 permite que o 1º lugar (índice 0) receba o prêmio
                if (index >= 0 && index < m_gp_reward.Count && m_gp_reward.Count > 0)//verifica se tem premios
                {
                    GrandPrixRankReward gprr = m_gp_reward[index];

                    if (gprr != null && gprr.ID != 0)
                    {
                        for (int i = 0; i < 5; i++)
                        {
                            if (gprr.reward._typeid[i] == 0) continue;

                            stItem itemRank = new stItem
                            {
                                type = 2,
                                id = -1,
                                _typeid = gprr.reward._typeid[i]
                            };

                            if (gprr.reward.time[i] > 0)
                            {
                                itemRank.qntd = 1;
                                itemRank.STDA_C_ITEM_QNTD = 1;
                                itemRank.STDA_C_ITEM_TIME = (short)gprr.reward.time[i];
                                itemRank.flag_time = 4;
                                itemRank.flag = 0x40;
                            }
                            else
                            {
                                itemRank.qntd = (int)gprr.reward.qntd[i];
                                itemRank.STDA_C_ITEM_QNTD = (short)itemRank.qntd;
                            }

                            // Verifica posse do item de rank
                            if ((sIff.getInstance().IsCanOverlapped(itemRank._typeid) && sIff.getInstance().getItemGroupIdentify(itemRank._typeid) != IFF_GROUP.CAD_ITEM) || !_session.m_pi.ownerItem(itemRank._typeid))
                            {
                                if (ItemManager.isSetItem(itemRank._typeid))
                                {
                                    var v_stItemRank = ItemManager.getItemOfSetItem(_session, itemRank._typeid, false, 1);
                                    foreach (var el in v_stItemRank) v_item.Add(new stItem(el));
                                }
                                else
                                {
                                    v_item.Add(itemRank);
                                }
                            }
                        }
                    }
                }

                // --- PARTE C: PROCESSAMENTO E ENVIO ---
                if (v_item.Count > 0)
                { 
                    // 1. Adiciona no Banco de Dados e Memória (O addItem que corrigimos o loop de remoção)
                    var rai = ItemManager.addItem(v_item, _session.getUID(), 0, 0);

                    if (rai.fails.Count > 0 && rai.type != ItemManager.RetAddItem.T_SUCCESS_PANG_AND_EXP_AND_CP_POUCH)
                    {
                        _smp.message_pool.getInstance().push(new message($"[GP:Reward][WARNING] Falha ao adicionar {rai.fails.Count} itens para UID: {_session.getUID()}", type_msg.CL_FILE_LOG_AND_CONSOLE));
                    }

                    // 2. Monta e envia o pacote 0x216 (Update Item no Cliente)
                    var p = new PangyaBinaryWriter((ushort)0x216);
                    p.WriteUInt32((uint)GetSystemTimeAsUnix());
                    p.WriteUInt32((uint)v_item.Count);

                    foreach (var el in v_item)
                    {
                        p.WriteByte(el.type);
                        p.WriteUInt32(el._typeid);
                        p.WriteInt32(el.id); // Importante: O addItem deve ter atualizado esse id com o do DB
                        p.WriteUInt32(el.flag_time);
                        p.WriteBytes(el.stat.ToArray());
                        // Se for tempo, envia o tempo, senão a quantidade
                        p.WriteInt32((el.STDA_C_ITEM_TIME > 0) ? el.STDA_C_ITEM_TIME : el.STDA_C_ITEM_QNTD);
                        p.WriteZeroByte(25);
                    }

                    packet_func.session_send(p, _session, 1);
                }
            }
            else
            {
                _smp.message_pool.getInstance().push(new message($"[GrandPrix::sendReward] Erro: Player UID {_session.m_pi.uid} ou GP m_gp inválidos.", type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void sendAllToNextHole()
        {

            // Change Hole All Finish Hole
            var p = new PangyaBinaryWriter((ushort)0x255);

            packet_func.game_broadcast(this,
                p, 1);
        }

        public int changeTurn(Player _session)
        {

            try
            {

                if (checkAllShotPacket(_session))
                {

                    var pgi = INIT_PLAYER_INFO("changeTurn",
                        "tentou trocar o turno do player",
                        _session);

                    // Agora verifica o se ele acabou o hole e essas coisas
                    if (pgi.shot_sync.state_shot.display.acerto_hole
                        || pgi.data.giveup > 0
                        || pgi.data.time_out > 0)
                    {

                        if (pgi.data.bad_condute >= 3)
                        { // Kika player deu 3 give up

                            // !!@@@
                            // Tira o player da sala
                            return 2;
                        }

                        // Verifica se o player terminou jogo, fez o ultimo hole
                        if (m_course.findHoleSeq(pgi.hole) == m_ri.qntd_hole)
                        {

                            // Resposta para o player que terminou o ultimo hole do Game
                            var p = new PangyaBinaryWriter((ushort)0x199);

                            packet_func.session_send(p,
                                _session, 1);

                            // Fez o Ultimo Hole, Calcula Clear Bonus para o player
                            if (pgi.shot_sync.state_shot.display.clear_bonus)
                            {

                                if (!MapSystem.getInstance().isLoad())
                                {
                                    MapSystem.getInstance().load();
                                }

                                var map = MapSystem.getInstance().getMap((byte)(m_ri.getMap() & 0x7F));

                                if (map == null)
                                {
                                    _smp.message_pool.getInstance().push(new message("[TourneyBase::checkEndShotOfHole][Error][Warning] tentou pegar o Map dados estaticos do course[COURSE=" + Convert.ToString((ushort)((byte)(m_ri.getMap() & 0x7F))) + "], mas nao conseguiu encontra na classe do Server.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                                }
                                else
                                {
                                    pgi.data.bonus_pang += MapSystem.getInstance().calculeClear30s(map, m_ri.qntd_hole);
                                }
                            }
                        }

                        finishHole(_session);

                        changeHole(_session);

                    }
                    else
                    {
                        clearAllShotPacket(_session);
                    }

                } // Wait ele ainda n o terminou de enviar todos os pacotes da tacada

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[GrandPrix::changeTurn][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            return 0;
        }
        public bool checkAllShotPacket(Player _session)
        {

            bool ret = false;

            try
            {

                var pgi = INIT_PLAYER_INFO("checkAllShotPacket",
                    "tentou verificar as PCBangMascot de sincronizacao de tacada do player",
                    _session);

                m_lock_manager.@lock(_session);

                ret = ((pgi.init_shot > 0 && pgi.sync_shot_flag > 0 || pgi.data.time_out > 0) && pgi.finish_shot > 0);

                m_lock_manager.unlock(_session);

            }
            catch (exception e)
            {

                m_lock_manager.unlock(_session);

                _smp.message_pool.getInstance().push(new message("[GrandPrix::checkAllShotPacket][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            return ret;
        }

        public void clearAllShotPacket(Player _session)
        {

            try
            {

                var pgi = INIT_PLAYER_INFO("clearAllShotPacket",
                    "tentou limpar as PCBangMascot de sincronizacao de tacada do player",
                    _session);

                // Limpa as veriaveis da tacada
                m_lock_manager.@lock(_session);

                pgi.init_shot = 0;
                pgi.sync_shot_flag = 0;
                pgi.finish_shot = 0;

                m_lock_manager.unlock(_session);

            }
            catch (exception e)
            {

                m_lock_manager.unlock(_session);

                _smp.message_pool.getInstance().push(new message("[GrandPrix::clearAllShotPacket][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }
        public bool checkAllClearHole()
        {

            uint count = 0;

            // Check
            m_players.ForEach(_el =>
            {
                try
                {
                    var pgi = INIT_PLAYER_INFO("checkAllClearHole",
                        "tentou verificar se todos os player terminaram o hole no jogo",
                        _el);
                    if (pgi.finish_hole > 0)
                    {
                        count++;
                    }
                }
                catch (exception e)
                {
                    _smp.message_pool.getInstance().push(new message("[GrandPrix::checkAllClearHole][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
            });
            return (count == m_players.Count);
        }

        public void setClearHole(PlayerGameInfo _pgi)
        {

            if (_pgi == null)
            {

                _smp.message_pool.getInstance().push(new message("[GrandPrix::setClearHole][Error] PlayerGameInfo* _pgi is invalid(nullptr).", type_msg.CL_FILE_LOG_AND_CONSOLE));

                return;
            }
            // Set
            _pgi.finish_hole = 1;
        }

        public void clearAllClearHole()
        {
            clear_all_clear_hole();
        }
        public bool checkAllClearHoleAndClear()
        {

            uint count = 0;
            bool ret = false;
            // Check
            m_players.ForEach(_el =>
            {
                try
                {
                    var pgi = INIT_PLAYER_INFO("checkAllClearHoleAndClear",
                        "tentou verificar se todos os player terminaram o hole no jogo",
                        _el);
                    if (pgi.finish_hole > 0)
                    {
                        count++;
                    }
                }
                catch (exception e)
                {
                    _smp.message_pool.getInstance().push(new message("[GrandPrix::checkAllClearHoleAndClear][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
            });

            ret = (count == m_players.Count);

            // Clear
            if (ret)
            {
                clear_all_clear_hole();
            }

            return ret;
        }

        public void clear_all_clear_hole()
        {

            m_players.ForEach(_el =>
            {
                try
                {
                    var pgi = INIT_PLAYER_INFO("clear_all_clear_hole",
                        " tentou limpar all clear hole no jogo",
                        _el);
                    pgi.finish_hole = 0;
                    pgi.shot_sync.state_shot.display.acerto_hole = false;
                    pgi.data.giveup = 0;
                }
                catch (exception e)
                {
                    _smp.message_pool.getInstance().push(new message("[GrandPrix::clear_all_hole][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
            });
        }

        public void clear_timers()
        {

            try
            {

                // Begin Timer Hole
                m_timer_manager.@lock();

                foreach (var el in m_timer_manager.getTimers())
                {
                    if (el.m_timer != null)
                        sgs.gs.getInstance().unMakeTime(el.m_timer);
                }

                m_timer_manager.unlock();
                // End Time Hole

                // Begin Timer Rule
                m_timer_manager_rule.@lock();

                foreach (var el in m_timer_manager_rule.getTimers())
                {
                    if (el.m_timer != null)
                        sgs.gs.getInstance().unMakeTime(el.m_timer);
                }

                m_timer_manager_rule.unlock();
                // End Time Rule

                // Limpa os timers
                m_timer_manager = new TimerManager();
                m_timer_manager_rule = new TimerManager();

            }
            catch (exception e)
            {

                m_timer_manager.unlock();
                m_timer_manager_rule.unlock();

                _smp.message_pool.getInstance().push(new message("[GrandPrix::clear_timers][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }
        public override void requestCalculeShotSpinningCube(Player _session, ShotSyncData _ssd)
        {
            //CHECK_SESSION_BEGIN("requestCalculeShotSpinningCube");

            try
            {

                // S  calcula se n o for short game e n o for grand prix rookie
                if (!(m_ri.special_flag_mod.short_game) && !(sIff.getInstance().getGrandPrixAbaType(m_gp.ID) == GrandPrixData.GP_ABA.ROOKIE && sIff.getInstance().isGrandPrixNormal(m_gp.ID)))
                {
                    calcule_shot_to_spinning_cube(_session, _ssd);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[GrandPrix::requestCalculeShotSpinningCube][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }
        public override void requestCalculeShotCoin(Player _session, ShotSyncData _ssd)
        {
            //CHECK_SESSION_BEGIN("requestCalculeShotCoin");

            try
            {

                // S  calcula se n o for short game e n o for grand prix rookier
                if (!(m_ri.special_flag_mod.short_game) && !(sIff.getInstance().getGrandPrixAbaType(m_gp.ID) == GrandPrixData.GP_ABA.ROOKIE && sIff.getInstance().isGrandPrixNormal(m_gp.ID)))
                {
                    calcule_shot_to_coin(_session, _ssd);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[GrandPrix::requestCalculeShotCoin][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }
        public override bool finish_game(Player _session, int option)
        {

            if (_session.getState()
                && _session.isConnected()
                && m_players.Count > 0)
            {

                var p = new PangyaBinaryWriter();

                if (option == 6 /*packet06 pacote que termina o game*/)
                {

                    if (m_grand_prix_state)
                    {
                        finish_grand_prix(_session, 1); // Termina sem ter acabado de jogar
                    }

                    var pgi = INIT_PLAYER_INFO("finish_game",
                        "tentou terminar o jogo",
                        _session);

                    // Rookie Grand Prix n o altera o info do player s  achievement
                    if (!(sIff.getInstance().getGrandPrixAbaType(m_gp.ID) == GrandPrixData.GP_ABA.ROOKIE && sIff.getInstance().isGrandPrixNormal(m_gp.ID)))
                    {

                        // Salve o record se o camp acabou e o player n o terminou todos os holes tbm tem que salvar o record [OK][Feito]
                        requestSaveRecordCourse(_session,
                            52 /*Grand Prix*/,
                            (m_ri.qntd_hole == 18 && (m_course.findHoleSeq(pgi.hole) == 18 || pgi.flag == PlayerGameInfo.eFLAG_GAME.END_GAME)) ? 1 : 0);

                        requestSaveInfo(_session, 0);
                    }

                    // D  Exp para o Caddie E Mascot Tamb m
                    if (pgi.data.exp > 0)
                    { // s  add exp se for maior que 0

                        // Add Exp para o player
                        _session.addExp(pgi.data.exp, false /*N o precisa do pacote para trocar de level*/);

                        // D  Exp para o Caddie Equipado
                        if (_session.m_pi.ei.cad_info != null) // Tem um caddie equipado
                        {
                            _session.addCaddieExp(pgi.data.exp);
                        }

                        // D  Exp para o Mascot Equipado
                        if (_session.m_pi.ei.mascot_info != null)
                        {
                            _session.addMascotExp(pgi.data.exp);
                        }
                    }

                    // Update Info Map Statistics
                    sendUpdateInfoAndMapStatistics(_session, 0);

                    // Update Mascot Info ON GAME, se o player estiver com um mascot equipado
                    if (_session.m_pi.ei.mascot_info != null)
                    {
                        packet_func.session_send(packet_func.pacote06B(_session.m_pi, 8),
                            _session, 1);
                    }

                    // Achievement Aqui
                    pgi.sys_achieve.finish_and_update(_session);

                    // Resposta que tem sempre que acaba um jogo, n o sei o que   ainda, esse s  n o tem no HIO Event
                    p.init_plain(0x244);

                    p.WriteUInt32(0); // OK

                    packet_func.session_send(p,
                        _session, 1);

                    // Esse   novo do JP, tem Tourney, VS, Grand Prix, HIO Event, n o vi talvez tenha nos outros tamb m
                    p.init_plain(0x24F);

                    p.WriteUInt32(0); // OK

                    packet_func.session_send(p,
                        _session, 1);

                    // Resposta Update Pang
                    p.init_plain(0xC8);

                    p.WriteUInt64(_session.m_pi.ui.pang);

                    p.WriteUInt64(0Ul);

                    packet_func.session_send(p,
                        _session, 1);

                    // Colocar o finish_game Para 1 quer dizer que ele acabou o camp
                    pgi.finish_game = 1;

                    // Flag do game que terminou
                    m_game_init_state = 2; // ACABOU

                }
            }

            return (PlayersCompleteGameAndClear() && m_grand_prix_state);
        }

        public override int end_time(object _arg1, object _arg2)
        {


            var game = (GrandPrix)(_arg1);

            try
            {

                // Tempo hole acabou
                game.timeIsOver(_arg2);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[GrandPrix::end_time][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            return 0;
        }
        public int end_time_rule(object _arg1, object _arg2)
        {
            var game = reinterpret_cast<GrandPrix>(_arg1);

            try
            {
                // Tempo rule acabou
                game.timeRuleIsOver(_arg2);
            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[GrandPrix::end_time_rule][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            return 0;
        }

        public void startTimeRule(object _quem)
        {

            try
            {

                if (_quem != null
                    && m_gp.rule > 0
                    && ((eRULE)m_gp.rule == eRULE.TIME_10_SEC || (eRULE)m_gp.rule == eRULE.TIME_15_SEC))
                {

                    uint time_milli = ((eRULE)m_gp.rule == eRULE.TIME_10_SEC ? 10u : ((eRULE)m_gp.rule == eRULE.TIME_15_SEC ? 15u : 0u));


                    Player p = (Player)(_quem);

                    // Para Tempo se j  estiver 1 timer
                    var timer = m_timer_manager_rule.findTimer(p);

                    // N o tem um timer criado ainda, cria um para ele
                    if (timer == null || timer.m_timer == null)
                    {


                        // Cria o timer rule 
                        if (timer == null && (timer = m_timer_manager_rule.insertTimer(p, sgs.gs.getInstance().MakeTime(time_milli * 1000 /*milliseconds*/, null, () => end_time_rule(this, _quem), PangyaSyncTimer.TIMER_TYPE.NORMAL))) == null)
                        {
                            throw new exception("[GrandPrix::startTimeRule][Error] PLAYER[UID=" + Convert.ToString(p.m_pi.uid) + "] nao conseguiu criar um timer_ctx para poder criar um timer rule para o player. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GRAND_PRIX,
                                1050, 0));
                        }
                    }
                    else
                    {

                        // J  tem um timer, reseta ele e inicia novamente
                        if (timer.m_timer != null)
                        {

                            if (timer.m_timer.getState() != PangyaSyncTimer.TIMER_STATE.STOP)
                            {
                                timer.m_timer.Stop();
                            }

                            // inicia ele novamente
                            timer.m_timer = sgs.gs.getInstance().MakeTime(time_milli * 1000 /*milliseconds*/, null, () => end_time_rule(this, _quem), PangyaSyncTimer.TIMER_TYPE.NORMAL);
                        }
                    }

                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[GrandPrix::startTimeRule][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }
        public bool stopTimeRule(object _quem)
        {


            bool ret = true;

            try
            {

                if (_quem != null
                    && m_gp.rule > 0
                    && ((eRULE)m_gp.rule == eRULE.TIME_10_SEC || (eRULE)m_gp.rule == eRULE.TIME_15_SEC))
                {


                    var p = (Player)(_quem);

                    var timer = m_timer_manager_rule.findTimer(p);

                    if (timer != null
                        && timer.m_timer != null
                        && timer.m_timer.getState() != PangyaSyncTimer.TIMER_STATE.STOP)
                    {

                        timer.m_timer.Stop();

                        uint time_milli = ((eRULE)m_gp.rule == eRULE.TIME_10_SEC ? 10u : ((eRULE)m_gp.rule == eRULE.TIME_15_SEC ? 15u : 0u));

                    }
                }

            }
            catch (exception e)
            {

                ret = false;

                _smp.message_pool.getInstance().push(new message("[GrandPrix::stopTimeRule][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            return ret;
        }

        public void timeRuleIsOver(object _quem)
        {

            try
            {

                if (_quem != null
                    && m_gp.rule > 0
                    && ((eRULE)m_gp.rule == eRULE.TIME_10_SEC || (eRULE)m_gp.rule == eRULE.TIME_15_SEC))
                {


                    var s = (Player)(_quem);

                    try
                    {

                        m_lock_manager.@lock(s);

                        var timer = m_timer_manager_rule.findTimer(s);

                        if (timer != null && timer.m_timer != null)
                        {

                            // Para o tempo se ele n o estiver parado
                            if (timer.m_timer.getState() != PangyaSyncTimer.TIMER_STATE.STOP)
                            {

                                timer.m_timer.Stop();
                            }
                            // Atualiza os dados do player que o tempo de Rule acabou
                            var pgi = INIT_PLAYER_INFO("timeRuleIsOver",
                            "acabou o tempo do hole do player",
                            s);

                            // Penalidade por que ele n o tacou antes de acabar o tempo Rule
                            if (m_game_init_state == 1 && pgi.init_shot == 0u)
                            {
                                pgi.data.penalidade++;
                            }

                            uint time_milli = ((eRULE)m_gp.rule == eRULE.TIME_10_SEC ? 10u : ((eRULE)m_gp.rule == eRULE.TIME_15_SEC ? 15u : 0u));
                        }

                        m_lock_manager.unlock(s);

                    }
                    catch (exception)
                    {
                        //ignorar:  UNREFERENCED_PARAMETER(e);

                        m_lock_manager.unlock(s);

                        // Relan a para o outro try..catch para mandar a msg no log
                        throw;
                    }

                }

            }
            catch (exception e)
            {
                _smp.message_pool.getInstance().push(new message("[GrandPrix::timeRuleIsOver][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void timeIsOver()
        {
            return;
        }
    }
}