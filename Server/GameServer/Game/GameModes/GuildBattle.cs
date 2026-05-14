using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Pangya_GameServer.Game.Base;
using Pangya_GameServer.Game.Manager;
using Pangya_GameServer.Game.System;
using Pangya_GameServer.Models;
using Pangya_GameServer.PacketFunc;
using PangyaAPI.Network.PangyaPacket;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Models;
using PangyaAPI.Utilities.Log;
namespace Pangya_GameServer.Game.GameModes
{
    public class GuildBattle : TourneyBase
    {

        protected bool m_guild_battle_state;

        protected GuildRoomManager m_guild_manager;
        public GuildBattle(List<Player> _players, RoomInfoEx _ri, RateValue _rv, bool _channel_rookie, GuildRoomManager _guild_manager) : base(_players, _ri, _rv, _channel_rookie)
        {
            m_guild_manager = _guild_manager;

            // Atualiza Treasure Hunter System Course
            if (!sTreasureHunterSystem.getInstance().isLoad())
            {
                sTreasureHunterSystem.getInstance().load();
            }

            var course = sTreasureHunterSystem.getInstance().findCourse((byte)(m_ri.getMap() & 0x7F));

            if (course == null)
            {
                _smp.message_pool.getInstance().push(new message("[GuildBattle::GuildBattle][Error] tentou pegar o course do Treasure Hunter System, mas o course[COURSE=" + Convert.ToString((ushort)((byte)m_ri.getMap() & 0x7F)) + "] nao existe no sistema", type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
            else
            {
                sTreasureHunterSystem.getInstance().updateCoursePoint(course, -1); // -1 ponto a cada jogo iniciado
            }

            // Aqui tem que inicializar os players info
            initAllPlayerInfo();

            foreach (var el in m_players)
            {

                var pgi = INIT_PLAYER_INFO("GuildBattle",
                    "tentou inicializar o counter item do GuildBattle",
                    el);

                initAchievement(el);

                // Counter Item do Team Tournament, para n�o ficar sem fazer esse achievement por que n�o tem team tournament no Fresh UP!
                // Vou colocar para o player fazer esse achievement com o Guild Battle, j� que ele � um tournament de team tamb�m
                pgi.sys_achieve.incrementCounter(0x6C40003Au);

                // Counter Item do Guild Battle
                pgi.sys_achieve.incrementCounter(0x6C40003Bu);
            }

            // inicializa as duplas do Guild Battle
            init_duplas();

            // Inicializa o Guild Battle
            m_guild_battle_state = init_game();
        }

        ~GuildBattle()
        {
            Dispose(false);
        }

        public override void Dispose(bool disposing)
        {
            if (disposedValue) return;

            if (disposing)
            {
                m_guild_battle_state = false;

                if (m_game_init_state != 2)
                {
                    finish();
                }

                while (!PlayersCompleteGameAndClear())
                {
                    Thread.Sleep(500);
                }

                deleteAllPlayer();
                LogDestruction();
            }
            base.Dispose(true);
        }

        public override bool deletePlayer(Player _session, int _option)
        {

            if (_session == null)
            {
                throw new exception("[GuildBattle::deletePlayer][Error] tentou deletar um player, mas o seu endereco é nullptr.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GUILD_BATTLE,
                    50, 0));
            }

            bool ret = false;

            try
            {

                var it = m_players.FirstOrDefault(c => c == _session);

                if (it != null)
                {

                    Dupla dup = null;
                    uint dup_p_index = 0;

                    if (m_game_init_state == 1)
                    {

                        var p = new PangyaBinaryWriter();

                        var pgi = INIT_PLAYER_INFO("deletePlayer",
                            "tentou sair do jogo",
                            _session);

                        var sessions = getSessions();

                        requestFinishItemUsedGame(it); // Salva itens usados no Guild Battle

                        requestSaveInfo(it, (_option == 0x800) ? 5 : 1); // Quitou ou tomou DC

                        setGameFlag(pgi, PlayerGameInfo.eFLAG_GAME.QUIT);

                        // Update dados da dupla
                        dup = m_guild_manager.findDupla(_session);

                        if (dup != null)
                        {

                            dup_p_index = (dup.p[0] == _session) ? 0 : 1u;

                            dup.state[dup_p_index] = Dupla.eSTATE.OUT_GAME;

                            for (var i = 0; i < m_course.findHoleSeq(dup.hole[(int)dup_p_index ^ 1]); ++i)
                            {

                                if (!dup.dados[dup_p_index][i].finish)
                                {
                                    dup.dados[(int)dup_p_index ^ 1][i].score = 2;
                                    dup.dados[(int)dup_p_index ^ 1][i].finish = true;
                                }
                            }

                            // Atualiza dados do GuildRoomManager
                            m_guild_manager.update();

                            // send update player dupla guild
                            sendFinishHoleDupla(_session);

                        }
                        else
                        {
                            _smp.message_pool.getInstance().push(new message("[GuildBattle::deletePlayer][Warning] Player[UID=" + Convert.ToString(_session.m_pi.uid) + "], nao esta em nenhuma dupla no Guild Battle na sala[NUMERO=" + Convert.ToString(m_ri.numero) + "]. Bug.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                        }

                        // Resposta Player saiu do Jogo, tira ele do list de score
                        p.init_plain(0x61);

                        p.WriteInt32(it.m_oid);

                        packet_func.vector_send(p,
                            sessions, 1);

                        // Resposta Player saiu do jogo
                        sendUpdateState(_session, 3);

                        // Att dados de mapas e info do player
                        sendUpdateInfoAndMapStatistics(_session, -1);

                    }
                    else
                    {
                        requestSaveInfo(it, (_option == 0x800) ? 5 : 1);
                    }

                    // Delete Player
                    m_players.Remove(it);

                    if (AllCompleteGameAndClear() || AllTeamQuit())
                    {

                        foreach (var el in m_player_info)
                        {
                            if (el.Key != null
                                && el.Key != _session
                                && el.Value.flag == PlayerGameInfo.eFLAG_GAME.PLAYING
                                && findSessionByUID(el.Value.uid) != null)
                            {
                                finish_guild_battle(el.Key, 0); // Acabou o Guild Battle
                            }
                        }

                        ret = true; // Termina o Guild Battle

                    }
                    else if (dup != null) // Termina o jogo para o outro player da dupla
                    {
                        finish_guild_battle(dup.p[(int)dup_p_index ^ 1], 0);
                    }

                }
                else
                {
                    _smp.message_pool.getInstance().push(new message("[GuildBattle::deletePlayer][Warning] player ja foi excluido do game.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                }

#if _WIN32
        					LeaveCriticalSection(m_cs);
#elif __linux__
        					pthread_mutex_unlock(m_cs);
#endif

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[GuildBattle::deletePlayer][Error] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                 
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

        public override void sendInitialData(Player session)
        {
            var p = new PangyaBinaryWriter();

            try
            {
                // Incrementa e verifica se todos já enviaram
                if (Interlocked.Increment(ref m_sync_send_init_data) == m_players.Count)
                {
                    // Zera variável
                    Interlocked.Exchange(ref m_sync_send_init_data, 0);

                    // Game Data Init
                    p.init_plain(0x76);

                    p.WriteByte(m_ri.tipo_show);
                    p.WriteUInt32(1);
                    p.WriteTime(m_start_time);

                    // Broadcast inicial
                    packet_func.game_broadcast(this, p, 1);

                    // Guild Battle Duplas
                    m_guild_manager.initPacketDuplas(ref p);

                    packet_func.game_broadcast(this, p, 1);

                    // Course
                    foreach (var el in m_players)
                        base.sendInitialData(el);
                }
            }
            catch (Exception e)
            {
                _smp.message_pool.getInstance()
                    .push(new message($"[GuildBattle::sendInitialData][ErrorSystem] {e}",
                                       type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }


        public override void requestInitHole(Player _session, packet _packet)
        {
            //REQUEST_BEGIN("InitHole");

            try
            {

                base.requestInitHole(_session, _packet);

                var pgi = INIT_PLAYER_INFO("requestInitHole",
                    "tentou inicializar o hole",
                    _session);

                var dup = m_guild_manager.findDupla(_session);

                if (dup != null)
                {

                    if (dup.p[0] == _session)
                    {
                        dup.hole[0] = pgi.hole;
                    }
                    else
                    {
                        dup.hole[1] = pgi.hole;
                    }
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[GuildBattle::requestInitHole][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void changeHole(Player _session)
        {

            updateTreasureHunterPoint(_session);

            if (checkEndGame(_session))
            {
                finish_guild_battle(_session, 0);
            }
            else
            {
                // Resposta terminou o hole
                updateFinishHole(_session, 1); // Terminou
            }
        }

        public override void finishHole(Player _session)
        {

            var pgi = INIT_PLAYER_INFO("finishHole",
                "tentu finializar o hole",
                _session);

            if (m_guild_manager.finishHoleDupla(pgi, m_course.findHoleSeq(pgi.hole)))
            {
                sendFinishHoleDupla(_session);
            }

            requestFinishHole(_session, 0);

            requestUpdateItemUsedGame(_session);
        }

        public void finish_guild_battle(Player _session, int _option)
        {

            if (m_players.Count > 0 && m_game_init_state == 1)
            {

                var pgi = INIT_PLAYER_INFO("finish_guild_battle",
                    "tentou terminar o guild battle no jogo",
                    _session);

                if (pgi.flag == PlayerGameInfo.eFLAG_GAME.PLAYING)
                {

                    // Calcula os pangs que o player ganhou
                    requestCalculePang(_session);

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
                        pgi.sys_achieve.incrementCounter(0x6C400004u);

                    }
                    else if (m_game_init_state == 1 && _option == 1)
                    { // Acabou o Tempo

                        var dup = m_guild_manager.findDupla(_session);

                        if (dup != null)
                        {

                            var dup_p_index = (dup.p[0] == _session) ? 0 : 1;
                            var otherIndex = dup_p_index ^ 1; // alterna 0/1

                            dup.state[dup_p_index] = Dupla.eSTATE.OVER_TIME;

                            for (var i = 0; i < m_ri.qntd_hole; ++i)
                            {
                                if (dup.dados[dup_p_index][i].finish && !dup.dados[otherIndex][i].finish)
                                {
                                    dup.dados[dup_p_index][i].score = 2;
                                }
                                else if (!dup.dados[dup_p_index][i].finish && dup.dados[otherIndex][i].finish)
                                {
                                    dup.dados[otherIndex][i].score = 2;
                                }
                            }


                            // Atualiza dados das guilds
                            m_guild_manager.update();

                            // Finish hole Dupla
                            sendFinishHoleDupla(_session);
                        }

                        requestFinishHole(_session, 1); // Acabou o Tempo

                        // Mostra msg que o player terminou o jogo
                        sendFinishMessage(_session);

                        // Resposta terminou o hole
                        updateFinishHole(_session, 0);

                        // Resposta para acabou o tempo do Guild Battle
                        sendTimeIsOver(_session);

                    }

                    setGameFlag(pgi, (_option == 0) ? PlayerGameInfo.eFLAG_GAME.FINISH : PlayerGameInfo.eFLAG_GAME.END_GAME);

                }
                pgi.time_finish = new SYSTEMTIME(DateTime.Now);

                if ((AllCompleteGameAndClear() || AllTeamQuit()) && m_game_init_state == 1)
                {
                    finish(); // Envia os pacotes que termina o jogo Ex: 0xCE, 0x79 e etc
                }
            }
        }

        //// Tempo
        public override void timeIsOver()
        {

            if (m_game_init_state == 1 && m_players.Count > 0)
            {

                Player _session = null;

                foreach (var el in m_player_info)
                {

                    // S� os que n�o acabaram
                    if (el.Value.flag == PlayerGameInfo.eFLAG_GAME.PLAYING && (_session = findSessionByUID(el.Value.uid)) != null)
                    {
                        finish_guild_battle(_session, 1);
                    }
                    else if (el.Value.flag == PlayerGameInfo.eFLAG_GAME.FINISH && (_session = findSessionByUID(el.Value.uid)) != null)
                    {
                        // Resposta para acabou o tempo do Guild Battle
                        sendTimeIsOver(_session);
                    }
                } 
            }
        }

        //// Inicializa Jogo e Finaliza Jogo
        public override bool init_game()
        {

            if (m_players.Count > 0)
            {

                // Cria o timer do Guild Battle
                startTime();

                // variavel que salva a data local do sistema
                initGameTime();

                m_game_init_state = 1; // Come�ou

                m_guild_battle_state = true;
            }

            return true;
        }

        protected virtual void requestFinishExpGame()
        {

            if (m_players.Count > 0)
            {

                Player _session = null;
                float stars = m_course.getStar();
                int exp = 0;
                int hole_seq = 0;

                for (var i = 0; i < m_player_order.Count; ++i)
                {

                    hole_seq = (int)m_course.findHoleSeq(m_player_order[i].hole);

                    // Ele est� no primeiro hole e n�o acertou ele, s� da experi�ncia se ele tiver acertado o hole
                    if (hole_seq == 1 && !m_player_order[i].shot_sync.state_shot.display.acerto_hole)
                    {
                        hole_seq = 0;
                    }

                    if (m_player_order[i].flag == PlayerGameInfo.eFLAG_GAME.FINISH)
                    {

                        if ((_session = findSessionByUID(m_player_order[i].uid)) != null)
                        {

                            exp = (int)(1 * m_player_order.Count * (hole_seq > 0 ? hole_seq : 0) * stars);
                            exp = (int)(exp * TRANSF_SERVER_RATE_VALUE(m_player_order[i].used_item.rate.exp) * TRANSF_SERVER_RATE_VALUE(m_rv.exp));
                            exp = (int)(exp * (1 - (i / m_player_info.Count)));

                            if (m_player_order[i].level < 70)
                            {
                                m_player_order[i].data.exp = exp;
                            }
                        }

                    }
                    else if (m_player_order[i].flag == PlayerGameInfo.eFLAG_GAME.END_GAME)
                    {

                        if ((_session = findSessionByUID(m_player_order[i].uid)) != null)
                        {

                            exp = (int)(1 * m_player_order.Count * (hole_seq > 0 ? hole_seq : 0) * stars);
                            exp = (int)(exp * TRANSF_SERVER_RATE_VALUE(m_player_order[i].used_item.rate.exp) * TRANSF_SERVER_RATE_VALUE(m_rv.exp));
                            exp = (int)(exp * (1 - (i / m_player_info.Count)));

                            if (m_player_order[i].level < 70)
                            {
                                m_player_order[i].data.exp = exp;
                            }
                        }
                    }

                    // Log
                    _smp.message_pool.getInstance().push(new message("[GuildBattle::requestFinishExpGame][Log] PLAYER[UID=" + Convert.ToString(m_player_order[i].uid) + "] ganhou " + Convert.ToString(m_player_order[i].data.exp) + " de experience.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                }
            }
        }

        protected virtual void finish()
        {

            m_game_init_state = 2; // Acabou

            requestCalculeRankPlace();

            m_guild_manager.calcGuildWin();

            requestSaveGuildData(); // Salva os dados da guild

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

        protected virtual void init_duplas()
        {

            m_guild_manager.init_duplas();
        }

        protected virtual bool AllTeamQuit()
        {

            if (m_players.Count == 0)
            {
                return true;
            }

            return m_guild_manager.oneGuildRest();
        }

        protected virtual void requestFinishData(Player _session)
        {

            // Finish Artefact Frozen Flame agora � direto no Finish Item Used Game
            requestFinishItemUsedGame(_session);

            requestSaveDrop(_session);

            rain_hole_consecutivos_count(_session); // conta os achievement de chuva em holes consecutivas

            score_consecutivos_count(_session); // conta os achievement de back-to-back(2 ou mais score iguais consecutivos) do player

            rain_count(_session); // Aqui achievement de rain count

            achievement_top_3_1st(_session); // Se o Player ficou em Top 3 add +1 ao contador de top 3, e se ele ficou em primeiro add +1 ao do primeiro

            // Resposta terminou game - Drop Itens
            sendDropItem(_session);

            // Resposta terminou game - Placar
            sendPlacar(_session);
        }

        protected virtual void requestSaveGuildData()
        {

            // Salva os dados das Guild que jogaram o Guild Battle
            m_guild_manager.saveGuildsData();
        }

        protected virtual void sendFinishHoleDupla(Player _session)
        {

            var dup = m_guild_manager.findDupla(_session);

            if (dup != null)
            {

                var p = new PangyaBinaryWriter((ushort)0xC2);

                p.WriteInt32(_session.m_oid);

                var guild = m_guild_manager.findGuildByTeam(Guild.eTEAM.RED);

                p.WriteUInt16(guild != null ? guild.getPoint() : 0);

                guild = m_guild_manager.findGuildByTeam(Guild.eTEAM.BLUE);

                p.WriteUInt16(guild != null ? guild.getPoint() : 0);

                // P1
                if (dup.p[0] == _session)
                {

                    p.WriteByte((byte)dup.sumScoreP1());
                    p.WriteByte((byte)dup.sumScoreP2());

                }
                else
                { // P2

                    p.WriteByte((byte)dup.sumScoreP2());
                    p.WriteByte((byte)dup.sumScoreP1());
                }

                packet_func.game_broadcast(this,
                    p, 1);
            }
        }

        public override void sendPlacar(Player _session)
        {

            var pgi = INIT_PLAYER_INFO("sendPlacar",
                "tentou enviar o placar do jogo",
                _session);

            var p = new PangyaBinaryWriter((ushort)0x79);

            p.WriteInt32(pgi.data.exp);

            p.WriteUInt32(m_ri.trofel);

            p.WriteByte(0); // Trofel Que o Player Ganhou
            p.WriteByte(m_guild_manager.getGuildWin()); // Team Win, 0 - vermelho, 1 - Azul, 2 nenhum

            var dup = m_guild_manager.findDupla(_session);

            if (dup != null)
            {
                p.WriteUInt32((dup.p[0] == _session) ? dup.pang_win[0] : dup.pang_win[1]);
                p.WriteInt32((dup.p[0] == _session) ? dup.sumScoreP1() : dup.sumScoreP2());
            }
            else // 2 valores de int32 com o valor 0
            {
                p.WriteUInt64(0);
            }

            var guild = m_guild_manager.findGuildByTeam(Guild.eTEAM.RED);

            if (guild != null)
            {
                p.WriteUInt32(guild.getPangWin());
            }
            else
            {
                p.WriteUInt32(0);
            }

            guild = m_guild_manager.findGuildByTeam(Guild.eTEAM.BLUE);

            if (guild != null)
            {
                p.WriteUInt32(guild.getPangWin());
            }
            else
            {
                p.WriteUInt32(0);
            }

            // Medalhas
            for (var i = 0; i < (m_medal.Length); ++i)
            {
                p.WriteBytes(m_medal[i].ToArray());
            }

            // N�o sei se � a geral ou se � s� a do Tourney, (DEIXEI A GERAL) todas as medalhas que ele tem
            p.WriteBytes(_session.m_pi.ui.medal.ToArray());

            packet_func.session_send(p,
                _session, 1);
        }

        public override bool finish_game(Player _session, int option = 0)
        {

            if (_session.getState()
                && _session.isConnected()
                && m_players.Count > 0)
            {

                var p = new PangyaBinaryWriter();

                if (option == 6)
                {

                    if (m_guild_battle_state)
                    {
                        finish_guild_battle(_session, 1); // Termina sem ter acabado de jogar
                    }

                    var pgi = INIT_PLAYER_INFO("finish_game",
                        "tentou terminar o jogo",
                        _session);

                    requestSaveInfo(_session, 0);

                    // D� Exp para o Caddie E Mascot Tamb�m
                    if (pgi.data.exp > 0)
                    { // s� add exp se for maior que 0

                        // Add Exp para o player
                        _session.addExp(pgi.data.exp, false);

                        // D� Exp para o Caddie Equipado
                        if (_session.m_pi.ei.cad_info != null) // Tem um caddie equipado
                        {
                            _session.addCaddieExp(pgi.data.exp);
                        }

                        // D� Exp para o Mascot Equipado
                        if (_session.m_pi.ei.mascot_info != null)
                        {
                            _session.addMascotExp(pgi.data.exp);
                        }
                    }

                    // Update Info Map Statistics
                    sendUpdateInfoAndMapStatistics(_session, 0);

                    // Resposta Treasure Hunter Item
                    requestSendTreasureHunterItem(_session);

                    // Update Mascot Info ON GAME, se o player estiver com um mascot equipado
                    if (_session.m_pi.ei.mascot_info != null)
                    {
                        packet_func.session_send(packet_func.pacote06B(_session.m_pi, 8),
                            _session, 1);
                    }

                    // Achievement Aqui
                    pgi.sys_achieve.finish_and_update(_session);

                    // Resposta que tem sempre que acaba um jogo, n�o sei o que � ainda, esse s� n�o tem no HIO Event
                    p.init_plain(0x244);

                    p.WriteUInt32(0); // OK

                    packet_func.session_send(p,
                        _session, 1);

                    // Esse � novo do JP, tem Tourney, VS, Grand Prix, HIO Event, n�o vi talvez tenha nos outros tamb�m
                    p.init_plain(0x24F);

                    p.WriteUInt32(0); // OK

                    packet_func.session_send(p,
                        _session, 1);

                    // Resposta Update Pang
                    p.init_plain(0xC8);

                    p.WriteUInt64(_session.m_pi.ui.pang);

                    p.WriteUInt64(0);

                    packet_func.session_send(p,
                        _session, 1);

                    // Colocar o finish_game Para 1 quer dizer que ele acabou o camp
                    pgi.finish_game = 1;

                    // Flag do game que terminou
                    m_game_init_state = 2; // ACABOU

                }
            }

            return (PlayersCompleteGameAndClear() && m_guild_battle_state);
        }

    }
}