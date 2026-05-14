using System;
using System.Collections.Generic;
using System.Linq;
using Pangya_GameServer.Repository;
using Pangya_GameServer.Game.Base;
using Pangya_GameServer.Game.System;
using Pangya_GameServer.Models;
using Pangya_GameServer.PacketFunc;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Models;
using PangyaAPI.Utilities.Log;
using PangyaAPI.SQL.Manager;
using snmdb;
namespace Pangya_GameServer.Game.GameModes
{
    public class Versus : VersusBase
    {
        private bool m_versus_state = false;

        public Versus(List<Player> _players, RoomInfoEx _ri, RateValue _rv, bool _channel_rookie) : base(_players, _ri, _rv, _channel_rookie)
        {
            // Atualiza Treasure Hunter System Course

            if (!sTreasureHunterSystem.getInstance().isLoad())
            {
                sTreasureHunterSystem.getInstance().load();
            }


            var course = sTreasureHunterSystem.getInstance().findCourse((m_ri.getMap()));

            if (course == null)
            {
                _smp.message_pool.getInstance().push(new message("[Versus::Versus][Error] tentou pegar o course do Treasure Hunter System, mas o course[COURSE=" + Convert.ToString((ushort)(m_ri.getMap())) + "] nao existe no sistema", 0));
            }
            else
            {
                sTreasureHunterSystem.getInstance().updateCoursePoint(course, -1); // -1 ponto a cada jogo iniciado
            }

            // Aqui tem que inicializar os players info
            initAllPlayerInfo();

            // Last 5 Players Play, tem que salvar no server e no DB, and Achievement
            foreach (var el in m_players)
            {

                // Achievement
                INIT_PLAYER_INFO("Versus",
                    "tentou inicializar o counter item do Versus",
                    el, out PlayerGameInfo pgi);

                initAchievement(el);

                pgi.sys_achieve.incrementCounter(0x6C40001Du);
                // Fim de init Achievement

                foreach (var el2 in m_players)
                {
                    if (el.m_pi.uid != el2.m_pi.uid)
                    {
                        el.m_pi.l5pg.add(el2.m_pi, el.m_pi.mi.sexo);
                    }
                }

                // Update ON DB
                snmdb.NormalManagerDB.getInstance().add(1,
                    new CmdUpdateLastPlayerGame(el.m_pi.uid, el.m_pi.l5pg),
                    Versus.SQLDBResponse, this);
            }

            m_versus_state = init_game();

            if (!m_versus_state)
            {
                _smp.message_pool.getInstance().push(new message("[Versus::Versus][Error] init_game() falhou ao iniciar o jogo.", type_msg.CL_ONLY_CONSOLE_DEBUG));
            }
            else
            {
                _smp.message_pool.getInstance().push(new message("[Versus::Versus][Log] init_game() jogo iniciado.", type_msg.CL_ONLY_CONSOLE_DEBUG));
            }
        }
        ~Versus()
        { Dispose(false); }
        public override void Dispose(bool disposing)
        {
            if (disposing)
            {
                m_versus_state = false;
                // Para o tempo do player Turn
                stopTime();

                // Salva os dados de todos os jogadores
                foreach (var el in m_players)//vai finalizar aqui?
                {
                    finish_game(el);
                }

                deleteAllPlayer();

                LogDestruction();

            }
            //chamar por ultimo
            base.Dispose(true);
        }

        public override void changeHole()
        {
            updateTreasureHunterPoint();

            if (m_players.Count() <= 0 || checkEndGame(m_players.FirstOrDefault()))
                finish_versus(0);
            else if (m_players.Count() > 0)
                // Resposta terminou o hole
                updateFinishHole(); // Terminou
        }

        public override void finishHole()
        {
            foreach (var el in m_players)
            {
                requestFinishHole(el, 0);

                requestUpdateItemUsedGame(el);
            }
        }

        public override bool deletePlayer(Player _session, int _option)
        {

            if (_session == null)
            {
                throw new exception("[Versus::deletePlayer][Error] tentou deletar um player, mas o seu endereco eh null.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS,
                    50, 0));
            }

            bool ret = false;

            try
            {

                // Evitar deadlock com a thread checkVersusTurn - Bloqueia
                m_state_vs.@lock();

                var it = m_players.FirstOrDefault(c => c == _session);

                if (it != null)
                {
                    INIT_PLAYER_INFO("deletePlayer",
                        "tentou sair do jogo",
                        _session, out PlayerGameInfo pgi);

                    if (m_game_init_state == 1)
                    {

                        var p = new PangyaBinaryWriter();

                        // Player Turn Para o tempo dele
                        if (m_player_turn == pgi)
                        {
                            stopTime();
                        }

                        var sessions = getSessions(it);

                        requestFinishItemUsedGame((it)); // Salva itens usados no Tourney

                        requestSaveInfo(it, (_option == 0x800) ? 5 : 1); // Quitou ou tomou DC

                        //pgi->flag = PlayerGameInfo::eFLAG_GAME::QUIT;
                        setGameFlag(pgi, PlayerGameInfo.eFLAG_GAME.QUIT);

                        // Resposta Player saiu do Jogo, tira ele do list de score
                        p.init_plain(0x61);

                        p.WriteInt32(it.m_oid);

                        packet_func.vector_send(p,
                            sessions, 1);

                        // Resposta Player saiu do jogo MSG
                        p.init_plain(0x40);

                        p.WriteByte(2); // Player Saiu Msg

                        p.WriteString(it.m_pi.nickname);

                        p.WriteUInt16(0); // size Msg, n o precisa de msg o pangya j  manda na opt 2

                        packet_func.vector_send(p,
                            sessions, 1);

                        sendUpdateInfoAndMapStatistics(_session, -1);

                        ret = checkNextStepGame(_session);

                    }
                    else if (m_game_init_state == 2 && !pgi.finish_game.IsTrue())
                    {

                        // Acabou
                        requestSaveInfo((it), 0);
                    }

                    // Deleta o player por give up ou time out, ele conta os achievements dele, tem o counter item 0x6C400004u Normal Game Complete
                    // Envia os achievements para ele para ficar igual ao original
                    if (m_game_init_state == 1
                        && pgi.data.bad_condute >= 3
                        && (pgi.data.time_out >= 3 || pgi.data.giveup >= 3))
                    {

                        // Achievements
                        rain_hole_consecutivos_count(_session); // conta os achievement de chuva em holes consecutivas

                        score_consecutivos_count(_session); // conta os achievement de back-to-back(2 ou mais score iguais consecutivos) do player

                        rain_count(_session); // Aqui achievement de rain count

                        pgi.sys_achieve.incrementCounter(0x6C400004u);

                        //Achievement Aqui
                        pgi.sys_achieve.finish_and_update(_session);

                        // Resposta que tem sempre que acaba um jogo, n o sei o que   ainda, esse s  n o tem no HIO Event
                        var p = new PangyaBinaryWriter((ushort)0x244);

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
                    _smp.message_pool.getInstance().push(new message("[Versus::deletePlayer][Warning] player ja foi excluido do game.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                }

                // Evitar deadlock com a thread checkVersusTurn - Libera
                m_state_vs.unlock();

            }
            catch (exception e)
            {
                _smp.message_pool.getInstance().push(new message("[Versus::deletePlayer][Error] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
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

        public void finish_versus(int _option)
        {

            if (m_players.Count > 0 && m_game_init_state == 1)
            {

                foreach (var el in m_players)
                {

                    INIT_PLAYER_INFO("finish_versus",
                        "tentou terminar o versus", el, out PlayerGameInfo pgi);

                    pgi.sys_achieve.incrementCounter(0x6C400004u);

                    requestCalculePang(el);

                    updatePlayerAssist(el);

                    sendFinishMessage(el);
                }


                finish();
            }
        }

        public override void timeIsOver(object _quem)
        {

            // Chama o timeIsOver da classe pai
            base.timeIsOver(_quem);

            if (_quem != null)
            {


                Player pl = (Player)(_quem);

                INIT_PLAYER_INFO("timeIsOver",
                    "tentou acabar o tempo do turno no jogo",
                    pl, out PlayerGameInfo pgi);

                pgi.tempo = 1u;

                if (pgi.bar_space.getState() == 0 && pgi == m_player_turn)
                {

                    pgi.tempo = 0u;

                    if (++pgi.data.time_out >= 3)
                    {
                        // 3 Time outs kika o jogado da sala
                        pgi.data.bad_condute = 3; // Kika Player
                    }
                    if (pgi.data.time_out >= 3)
                    {
                        // Time Out
                        var p = new PangyaBinaryWriter((ushort)0x5C);

                        p.WriteInt32(pgi.oid);

                        packet_func.game_broadcast(this,
                            p, 1);
                        _smp.message_pool.getInstance().push(new message($"[Versus::timeIsOver][Log] PLAYER[UID={pgi.uid}] Time Out", type_msg.CL_FILE_LOG_AND_CONSOLE));
                    }
                }

            }
            else
            {
                _smp.message_pool.getInstance().push(new message("[Versus::timeIsOver][Warning] time is over executed without _quem, _quem is invalid(null). Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override bool init_game()
        {

            try
            {
                base.init_game();

                if (m_players.Count > 0)
                {

                    // variavel que salva a data local do sistema
                    initGameTime();

                    m_game_init_state = 1; // Come ou 
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public void requestFinishExpGame()
        {

            if (m_players.Count > 0)
            {

                Player _session = null;
                float stars = m_course.getStar();
                int exp = 0;
                int hole_seq = 0;

                for (var i = 0; i < m_player_order.Count; ++i)
                {
                    switch (m_ri.qntd_hole)
                    {
                        case 3:
                            exp = 8;
                            break;
                        case 6:
                            exp = 12;
                            break;
                        case 9:
                            exp = 16;
                            break;
                        case 18:
                            exp = 20;
                            break;
                        default:
                            exp = 1;
                            break;
                    }
                    exp = (int)(exp * stars);

                    hole_seq = (int)m_course.findHoleSeq(m_player_order[i].hole);

                    // Ele est  no primeiro hole e n o acertou ele, s  da experi ncia se ele tiver acertado o hole
                    if (hole_seq == 1 && !m_player_order[i].shot_sync.state_shot.display.acerto_hole)
                    {
                        hole_seq = 0;
                    }

                    if ((_session = findSessionByUID(m_player_order[i].uid)) != null)
                    {

                        exp = (int)(1 * m_player_order.Count * (hole_seq > 0 ? hole_seq : 0) * stars);
                        exp = (int)(exp * TRANSF_SERVER_RATE_VALUE(m_player_order[i].used_item.rate.exp) * TRANSF_SERVER_RATE_VALUE(m_rv.exp));
                        exp = (int)((float)exp * (float)(1.0f - (i * 0.1f)));

                        if (m_player_order[i].level < 70)
                        {
                            m_player_order[i].data.exp = exp;
                        }
                    }
                }
            }
        }


        public void finish()
        {

            m_versus_state = false; // Terminou o versus

            m_game_init_state = 2; // Terminou o jogo

            requestCalculeRankPlace();

            requestFinishExpGame();

            requestDrawTreasureHunterItem();

            foreach (var el in m_players)
            {

                INIT_PLAYER_INFO("finish",
                    "tentou finalizar os dados do jogador no jogo",
                    el, out PlayerGameInfo pgi);

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


            // Resposta Treasure Hunter Item Draw
            sendTreasureHunterItemDrawGUI(_session);

            // Resposta terminou game - Drop Itens
            sendDropItem(_session);

            // Resposta terminou game - Placar
            sendPlacar(_session);
        }

        public override bool finish_game(Player _session, int option = 0)
        {

            if (_session.getState()
                && _session.isConnected()
                && m_players.Count > 0)
            {

                INIT_PLAYER_INFO("finish_game",
                    "tentou finalizar o jogo",
                    _session, out PlayerGameInfo pgi);

                // Terminou o hole, finalizar o hole por ele
                if (pgi.shot_sync.state_shot.display.acerto_hole || pgi.data.giveup.IsTrue())
                {

                    requestFinishHole(_session, 0);

                    requestUpdateItemUsedGame(_session);
                }

                pgi.finish_game = 1;

                if (PlayersCompleteGameAndClear() || option == 2)
                {

                    var p = new PangyaBinaryWriter();

                    // Verifica se   o primeiro hole e se nem todos terminaram o hole
                    if (m_course.findHoleSeq(pgi.hole) == 1
                        && !checkAllClearHole()
                        && (pgi.progress.hole <= 0 || pgi.progress.finish_hole[pgi.progress.hole - 1] == 0))
                    {

                        foreach (var el in m_players)
                        {

                            INIT_PLAYER_INFO("finish_game",
                                "tentou finalizar o versus",
                                el, out pgi);

                            if (pgi.flag == PlayerGameInfo.eFLAG_GAME.PLAYING)
                            {

                                requestSaveInfo(el, 2);

                                if (pgi.finish_item_used == 0u)
                                {
                                    requestFinishItemUsedGame(el);
                                }

                                p.init_plain(0x67);

                                packet_func.session_send(p,
                                    el, 1);

                                //pgi->flag = PlayerGameInfo::eFLAG_GAME::END_GAME;
                                setGameFlag(pgi, PlayerGameInfo.eFLAG_GAME.END_GAME);
                            }
                        }

                        m_game_init_state = 2; // Acabou o VS

                        return true;

                    }
                    else
                    {

                        if (m_versus_state) // Deixa o cliente envia o pacote para finalizar o jogo, depois que ele mostrar os placares
                        {
                            finish_versus(1);
                        }
                        else
                        {

                            foreach (var el in m_players)
                            {

                                INIT_PLAYER_INFO("finish_game",
                                    "tentou finalizar o versus",
                                    el, out pgi);

                                if (pgi.flag == PlayerGameInfo.eFLAG_GAME.PLAYING)
                                {

                                    requestSaveRecordCourse(el,
                                        0,
                                        (m_ri.qntd_hole == 18 && m_course.findHoleSeq(pgi.hole) == 18) ? 1 : 0);

                                    requestSaveInfo(el, 0);

                                    // D  Exp para o Caddie E Mascot Tamb m
                                    if (pgi.data.exp > 0)
                                    { // s  add exp se for maior que 0

                                        // Add Exp para o player
                                        el.addExp(pgi.data.exp, false);

                                        // D  Exp para o Caddie Equipado
                                        if (el.m_pi.ei.cad_info != null) // Tem um caddie equipado
                                        {
                                            el.addCaddieExp(pgi.data.exp);
                                        }

                                        // D  Exp para o Mascot Equipado
                                        if (el.m_pi.ei.mascot_info != null)
                                        {
                                            el.addMascotExp(pgi.data.exp);
                                        }
                                    }

                                    sendUpdateInfoAndMapStatistics(el, 0);

                                    // Resposta Treasure Hunter Item
                                    requestSendTreasureHunterItem(el);

                                    // Update Mascot Info ON GAME, se o player estiver com um mascot equipado
                                    if (el.m_pi.ei.mascot_info != null)
                                    {

                                        packet_func.session_send(packet_func.pacote06B(
                                             el.m_pi, 8),
                                            el, 1);
                                    }

                                    // Achievement Aqui
                                    pgi.sys_achieve.finish_and_update(el);

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

                                    p.WriteUInt64(el.m_pi.ui.pang);

                                    p.WriteUInt64(0Ul);

                                    packet_func.session_send(p,
                                        el, 1);

                                    //pgi->flag = PlayerGameInfo::eFLAG_GAME::FINISH;
                                    setGameFlag(pgi, PlayerGameInfo.eFLAG_GAME.FINISH);
                                }
                            }

                            m_game_init_state = 2; // Acabou o VS

                            return true;
                        }
                    }
                }
            }

            return m_players.Count == 0;
        }

        protected new static void SQLDBResponse(int _msg_id,
            Pangya_DB _pangya_db,
            object _arg)
        {

            if (_arg == null)
            {
                return;
            }

            // Por Hora s  sai, depois fa o outro tipo de tratamento se precisar
            if (_pangya_db.getException().getCodeError() != 0)
            {
                _smp.message_pool.getInstance().push(new message("[Versus::SQLDBResponse][Error] " + _pangya_db.getException().getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                return;
            }


            var game = (GameBase)(_arg);

            switch (_msg_id)
            {
                case 1: // Update Last 5 Player Game
                    {
                        var cmd_l5pg = (CmdUpdateLastPlayerGame)(_pangya_db);
                        break;
                    }
                case 0:
                default:
                    break;
            }
        }
    }
}