using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Pangya_GameServer.Game.Base;
using Pangya_GameServer.Game.System;
using Pangya_GameServer.Models;
using Pangya_GameServer.PacketFunc;
using PangyaAPI.Network.PangyaPacket;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Models;
using PangyaAPI.Utilities.Log;
using static Pangya_GameServer.Models.DefineConstants;
namespace Pangya_GameServer.Game.GameModes
{
    public class Match : VersusBase
    {
        private bool m_match_state;
        byte m_team_win;       // Team que ganhou o Match ou 2 para draw(empate)

        Team m_team_turn;

        TreasureHunterVersusInfo m_thi_blue;        // Treasure Hunter Item point do team Azul, n�o gera itens mas soma os pontos
        List<TeamOrderTurnCtx> v_team_order_turn;
        // Teans(times)
        List<Team> m_teans;
        public class TeamOrderTurnCtx
        {
            public Team team;
            public HoleManager hole;

            public TeamOrderTurnCtx(uint _ul = 0)
            {
                team = null;
                hole = null;
            }

            public TeamOrderTurnCtx(Team team, HoleManager hole)
            {
                this.team = team;
                this.hole = hole;
            }

            public void Clear()
            {
                team = null;
                hole = null;
            }
        }

        private Team INIT_TEAM_INFO(string _msg, Player __session, [CallerMemberName] string _method = "")
        {
            // Equivalente ao var team = var team = INIT_TEAM_INFO
            Team team = null;

            var it = m_teans.Find(t => t.findPlayerByUID(__session.m_pi.uid) != null);

            if (it != null)
            {
                team = it;
            }
            else
            {
                throw new exception($"[Match::{_method}][Error] PLAYER[UID={__session.m_pi.uid}] tentou encontrar o time dele, mas no jogo não tem o time dele. Bug",
                    ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MATCH, 100, 0));
            }
            return team;
        }
        public Match(List<Player> _players, RoomInfoEx _ri, RateValue _rv, bool _channel_rookie, List<Team> _teans) : base(_players, _ri, _rv, _channel_rookie)
        {
            this.m_team_win = 0;
            this.m_match_state = false;
            this.m_teans = _teans;
            this.m_team_turn = null;
            this.m_thi_blue = new TreasureHunterVersusInfo();
            v_team_order_turn = new List<TeamOrderTurnCtx>();

            if (!sTreasureHunterSystem.getInstance().isLoad())
            {
                sTreasureHunterSystem.getInstance().load();
            }

            var course = sTreasureHunterSystem.getInstance().findCourse((byte)(m_ri.getMap() & 0x7F));

            if (course == null)
            {
                _smp.message_pool.getInstance().push(new message("[Match::Match][Error] tentou pegar o course do Treasure Hunter System, mas o course[COURSE=" + Convert.ToString((ushort)(m_ri.getMap() & 0x7F)) + "] nao existe no sistema", type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
            else
            {
                sTreasureHunterSystem.getInstance().updateCoursePoint(course, -1); // -1 ponto a cada jogo iniciado
            }

            // Aqui tem que inicializar os players info
            initAllPlayerInfo();

            // Inicializar as posi  es dos jogadores do team que n  sala ele pode ser diferente, a ordem que entrou no team
            // Tem que sempre ficar na ordem da sala
            init_team_player_position();

            foreach (var el in m_players)
            {

                var pgi = INIT_PLAYER_INFO("Match",
                     "tentou inicializar o counter item do Match",
                     el);

                initAchievement(el);

                pgi.sys_achieve.incrementCounter(0x6C40001Eu);
            }

            m_match_state = init_game();

        }

        ~Match()
        {
            Dispose(false);
        }

        public override void Dispose(bool disposing)
        {
            if (disposedValue) return;

            if (disposing)
            { 
                // Para o tempo do Player Turn
                stopTime();

                // Salva os dados de todos os jogadores
                foreach (var el in m_players)
                {
                    finish_game(el);
                }

                clear_teans();

                m_team_win = 0;

                deleteAllPlayer();
                LogDestruction();

            }
            base.Dispose(true);
        }

        public override bool deletePlayer(Player _session, int _option)
        {

            if (_session == null)
            {
                throw new exception("[Match::deletePlayer][Error] tentou deletar um Player, mas o seu endereco é nullptr.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MATCH,
                    50, 0));
            }

            bool ret = false;

            try
            {

                // Evitar deadlock com a thread checkVersusTurn - Bloqueia
                m_state_vs.@lock();

                //(m_cs);
                //

                var it = m_players.FirstOrDefault(c => c == _session);

                if (it != null)
                {
                    var pgi = INIT_PLAYER_INFO("deletePlayer",
                        "tentou sair do jogo",
                        _session);

                    if (m_game_init_state == 1)
                    {

                        var team = INIT_TEAM_INFO("deletePlayer", _session);

                        PangyaBinaryWriter p = new PangyaBinaryWriter();

                        // Player Turn Para o tempo dele
                        if (m_player_turn == pgi)
                        {
                            stopTime();
                        }

                        var sessions = getSessions(it);

                        requestFinishItemUsedGame((it)); // Salva itens usados no Tourney

                        requestSaveInfo((it), (_option == 0x800) ? 5 : 1); // Quitou ou tomou DC

                        //pgi.type = PlayerGameInfo::eFLAG_GAME::QUIT;
                        setGameFlag(pgi, PlayerGameInfo.eFLAG_GAME.QUIT);

                        // Player Quitou, o time desiste
                        team.setQuit(1);

                        // Resposta Player saiu do Jogo, tira ele do list de score
                        p.init_plain(0x61);

                        p.WriteInt32((it).m_oid);

                        packet_func.vector_send(p,
                            sessions, 1);

                        // Resposta Player saiu do jogo MSG
                        p.init_plain(0x40);

                        p.WriteByte(2); // Player Saiu Msg

                        p.WriteString((it).m_pi.nickname);

                        p.WriteUInt16(0); // size Msg, n o precisa de msg o pangya j  manda na opt 2

                        packet_func.vector_send(p,
                            sessions, 1);

                        // Salva Achievement do Player

                        sendUpdateInfoAndMapStatistics(_session, -1);

                        ret = checkNextStepGame(_session);

                        if (!ret && m_players.Count > 0)
                        {
                            ret = true;
                        }

                    }
                    else if (m_game_init_state == 2 && !(pgi.finish_game > 0))
                    {

                        // Acabou
                        requestSaveInfo((it), 0);

                        // Salva Achievement tbm
                    }

                    // Deleta o Player por give up ou time out, ele conta os achievements dele, tem o counter item 0x6C400004u Normal Game Complete
                    // Envia os achievements para ele para ficar igual ao original
                    if (m_game_init_state == 1
                        && pgi.data.bad_condute >= 3
                        && (pgi.data.time_out >= 3 || pgi.data.giveup >= 3))
                    {

                        // Achievements
                        rain_hole_consecutivos_count(_session); // conta os achievement de chuva em holes consecutivas

                        score_consecutivos_count(_session); // conta os achievement de back-to-back(2 ou mais score iguais consecutivos) do Player

                        rain_count(_session); // Aqui achievement de rain count

                        pgi.sys_achieve.incrementCounter(0x6C400004u);

                        // Achievement Aqui
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
                    _smp.message_pool.getInstance().push(new message("[Match::deletePlayer][Warning] Player ja foi excluido do game.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                }




                // Evitar deadlock com a thread checkVersusTurn - Libera
                m_state_vs.unlock();

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Match::deletePlayer][Error] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Libera Critical Section



                // Evitar deadlock com a thread checkVersusTurn - Libera
                m_state_vs.unlock();
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

        public override void requestInitHole(Player _session, packet _packet)
        {
            ////REQUEST_BEGIN("InitHole");

            PangyaBinaryWriter p = new PangyaBinaryWriter(); 
            try
            {

                #region Read Packet
                stInitHole ctx_hole = new stInitHole().ToRead(_packet); 
                #endregion

                var hole = m_course.findHole(ctx_hole.numero);

                hole.init(ctx_hole.tee, ctx_hole.pin);

                var pgi = INIT_PLAYER_INFO("requestInitHole",
                    "tentou inicializar o hole[NUMERO = " + Convert.ToString(hole.getNumero()) + "] no jogo",
                    _session);

                var team = INIT_TEAM_INFO("requestInitHole", _session);

                // Update Location Player in Hole
                pgi.location.x = ctx_hole.tee.x;
                pgi.location.z = ctx_hole.tee.z;

                // Update Team Location
                team.setLocation(pgi.location);

                // N mero do hole atual, que o Player est  jogando
                pgi.hole = ctx_hole.numero;

                // Flag que marca se o Player j  inicializou o primeiro hole do jogo
                if (!pgi.init_first_hole)
                {
                    pgi.init_first_hole = true;
                }

                // Update Team Hole
                team.setHole(ctx_hole.numero);

                // Gera degree para o Player ou pega o degree sem gerar que   do modo do hole repeat
                team.setDegree(m_ri.modo == 4 ? hole.getWind().degree.getDegree() : hole.getWind().degree.getShuffleDegree());

                //pgi.degree = team.getDegree();

                _smp.message_pool.getInstance().push(new message("[Match::requestInitHole][Log] Player[UID=" + Convert.ToString(pgi.uid) + "] Vento[Graus=" + Convert.ToString(pgi.degree) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Match::requestInitHole][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

            }
        }

        public override void requestMoveBall(Player _session, packet _packet)
        {
            ////REQUEST_BEGIN("MoveBall");

            PangyaBinaryWriter p = new PangyaBinaryWriter();

            try
            {

                float x = _packet.ReadFloat();
                float y = _packet.ReadFloat();
                float z = _packet.ReadFloat();

                var pgi = INIT_PLAYER_INFO("requestMoveBall",
                    "tentou recolocar a bola no jogo",
                    _session);

                var team = INIT_TEAM_INFO("requestMoveBall", _session);

                pgi.location.x = x;
                pgi.location.y = y;
                pgi.location.z = z;

                // Update Team Location
                team.setLocation(pgi.location);

                team.decrementPlayerStartHole();

                // para o tempo do da tacada do Player, que ele vai recolocar e come a um novo tempo depois
                stopTime();

                // Resposta para Move Ball
                p.init_plain(0x60);

                p.WriteFloat(pgi.location.x);
                p.WriteFloat(pgi.location.y);
                p.WriteFloat(pgi.location.z);

                packet_func.game_broadcast(this,
                    p, 1);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Match::requestMoveBall][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void changeHole()
        {

            updateTreasureHunterPoint();

            if (m_player_turn == null)
            {
                throw new exception("[Match::changeHole][Error] PlayerGameInfo m_player_turn is invalid(nullptr). Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MATCH,
                    100, 0));
            }

            var hole_seq = m_course.findHoleSeq(m_player_turn.hole);

            int state = 0;
            int hole_diff = m_ri.qntd_hole - hole_seq;

            if ((int)(m_teans[0].getPoint() - m_teans[1].getPoint()) > hole_diff)
            {
                state = 1;
            }
            else if ((int)(m_teans[1].getPoint() - m_teans[0].getPoint()) > hole_diff)
            {
                state = 1;
            }

            if (state != 0
                || m_players.Count <= 0
                || checkEndGame(m_players.begin()))
            {

                // Resposta para o Player que terminou o ultimo hole do Game
                var p = new PangyaBinaryWriter((ushort)0x199);

                packet_func.game_broadcast(this,
                    p, 1);

                // Fez o Ultimo Hole, Calcula Clear Bonus para o Player
                if (!MapSystem.getInstance().isLoad())
                {
                    MapSystem.getInstance().load();
                }

                var map = MapSystem.getInstance().getMap((byte)(m_ri.getMap() & 0x7F));

                var clear_bonus = 0;

                // !!@@@ aqui pode ser que adicionar o clear bonus pros 2 mesmo que o outro n o fez o ultimo hole,
                // j  estou add, s  vou deixar coment rio para causo de erro ou mude de ideia a frente
                if (map == null)
                {
                    _smp.message_pool.getInstance().push(new message("[Match::changeHole][Error][Warning] tentou pegar o Map dados estaticos do course[COURSE=" + Convert.ToString((ushort)(m_ri.getMap() & 0x7F)) + "], mas nao conseguiu encontra na classe do Server.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
                else
                {
                    foreach (var team in m_teans)
                    {
                        team.incrementBonusPang((ulong)(clear_bonus = (int)MapSystem.getInstance().calculeClearMatch(map, hole_seq)));

                        _smp.message_pool.getInstance().push(new message("[Match::changeHole][Log] player_turn[UID=" + Convert.ToString(m_player_turn.uid) + "] do Team[ID=" + Convert.ToString(team.getId()) + "] fez o ultimo hole do Match e ganhou " + Convert.ToString(clear_bonus) + " Clear Bonus", type_msg.CL_FILE_LOG_AND_CONSOLE));
                        team.incrementBonusPang(MapSystem.getInstance().calculeClearMatch(map, m_ri.qntd_hole));
                    }
                }

                // Finish Match
                finish_match(0);

            }
            else if (m_players.Count > 0)
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

        void finish_match(int _option)

        {

            if (m_players.Count > 0 && m_game_init_state == 1)
            {

                // All All Pang and Bonus Pang All players of teans
                requestUpdateTeamPang();

                foreach (var el in m_players)
                {

                    var pgi = INIT_PLAYER_INFO("finish_match",
                        "tentou finalizar o Match", el);

                    pgi.sys_achieve.incrementCounter(0x6C400004u);

                    requestCalculePang(el);

                    updatePlayerAssist(el);

                    sendFinishMessage(el);
                }

                finish();
            }
        }

        public override void requestTeamFinishHole(Player _session, packet _packet)

        {
            ////REQUEST_BEGIN("TeamFinishHole");

            try
            {

                // 9 putt, 10 Chip-in
                var state_finish = _packet.ReadUInt16();

                var team = INIT_TEAM_INFO("requestTeamFinishHole", _session);

                team.setStateFinish(state_finish);

                _smp.message_pool.getInstance().push(new message("[Match::requestTeamFinishHole][Log] Player[UID=" + Convert.ToString(_session.m_pi.uid) + "] do Team[ID=" + Convert.ToString(team.getId()) + ", STATE=" + Convert.ToString(team.getStateFinish()) + "] terminou o hole.", type_msg.CL_FILE_LOG_AND_CONSOLE));

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Match::requestTeamFinishHole][Error] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void startTime(object _quem)

        {

            try
            {
                // Aqui tem tem ser come a o tempo do Player do turno e soma +1 ao n mero de tacadas dele
                var pgi = INIT_PLAYER_INFO("startTime",
                    "tentou comecar o tempo do Player turno no jogo",
                    (Player)_quem);

                var team = INIT_TEAM_INFO("startTime", (Player)_quem);

                // Soma +1 na tacada do Player
                pgi.data.tacada_num++;

                team.incrementTacadaNum();

                team.incrementPlayerStartHole();

                // Para Tempo se j  estiver 1 timer
                if (m_timer != null)
                    stopTime();

                m_timer = sgs.gs.getInstance().MakeTime(m_ri.time_vs, () => end_time(this, _quem), new List<long>(), PangyaSyncTimer.TIMER_TYPE.NORMAL);
            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Match::startTime][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void timeIsOver(object _quem)
        {

            // Chama o timeIsOver da classe pai
            base.timeIsOver(_quem);

            if (_quem != null)
            {


                Player p = (Player)(_quem);

                var pgi = INIT_PLAYER_INFO("timeIsOver",
                    "tentou acabar o tempo do turno no jogo",
                    p);

                var team = INIT_TEAM_INFO("timeIsOver", p);

                // set timeout do team
                team.setTimeout(1);

                pgi.tempo = 1;

                if (pgi.bar_space.getState() == 0 && pgi == m_player_turn)
                {

                    pgi.tempo = 0;

                    if (++pgi.data.time_out >= 3)
                    {
                        // 3 Time outs kika o jogado da sala
                        pgi.data.bad_condute = 3; // Kika Player
                    }

                    // Time Out
                    var pk = new PangyaBinaryWriter((ushort)0x5C);

                    pk.WriteInt32(pgi.oid);

                    packet_func.game_broadcast(this,
                        pk, 1);
                }

            }
            else
            {
                _smp.message_pool.getInstance().push(new message("[Match::timeIsOver][Warning] time is over executed without _quem, _quem is invalid(nullptr). Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override bool init_game()

        {

            var lixo = base.init_game();

            if (m_players.Count > 0)
            {

                // variavel que salva a data local do sistema
                initGameTime();

                m_game_init_state = 1; // Come ou

                m_match_state = true;
            }

            return true;
        }

        public override void requestTranslateSyncShotData(Player _session, ShotSyncData _ssd)

        {
            //CHECK_SESSION_BEGIN("requestTransateSyncShotData");

            try
            {

                var s = findSessionByOID(_ssd.oid);

                if (s == null)
                {
                    throw new exception("[Match::requestTranslateSyncShotData][Error] Player[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou sincronizar tacada do Player[OID=" + Convert.ToString(_ssd.oid) + "], mas o Player nao existe nessa jogo. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                        200, 0));
                }

                // Update Sync Shot Player
                if (_session.m_pi.uid == s.m_pi.uid)
                {

                    var pgi = INIT_PLAYER_INFO("requestTranslateSyncShotData",
                        "tentou sincronizar a tacada no jogo",
                        _session);

                    var team = INIT_TEAM_INFO("requestTranslateSyncShotData", _session);

                    pgi.shot_sync = _ssd;

                    // Last Location Team(Player)
                    var last_location = team.getLocation();

                    // Update Location Player
                    pgi.location.x = _ssd.location.x;
                    pgi.location.z = _ssd.location.z;

                    // Update Team Location
                    team.setLocation((Location)_ssd.location);

                    // Update Pang and Bonus Pang
                    pgi.data.pang = _ssd.pang;
                    pgi.data.bonus_pang = _ssd.bonus_pang;

                    // Update Pang and Bonus Pang Team
                    team.setPang(_ssd.pang);
                    team.setBonusPang(_ssd.bonus_pang);

                    // J  s  na fun  o que come a o tempo do Player do turno
                    //pgi.data.tacada_num++;

                    if (_ssd.state == ShotSyncData.SHOT_STATE.OUT_OF_BOUNDS || _ssd.state == ShotSyncData.SHOT_STATE.UNPLAYABLE_AREA)
                    {
                        pgi.data.tacada_num++;

                        // Update Tacada Num Team
                        team.incrementTacadaNum();
                    }

                    var hole = m_course.findHole(pgi.hole);

                    if (hole == null)
                    {
                        throw new exception("[Match::requestTranslateSyncShotData][Error] Player[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou sincronizar tacada no hole[NUMERO=" + Convert.ToString((ushort)pgi.hole) + "], mas o numero do hole is invalid. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                            12, 0));
                    }

                    // Update Team Acerto Hole
                    team.setAcertoHole(Convert.ToByte(_ssd.state_shot.display.acerto_hole));

                    // Conta j  a pr xima tacada, no give up
                    if (!_ssd.state_shot.display.acerto_hole && hole.getPar().total_shot <= (pgi.data.tacada_num + 1))
                    {

                        // +1 que   giveup, s  add se n o passou o n mero de tacadas
                        if (pgi.data.tacada_num < hole.getPar().total_shot)
                        {
                            pgi.data.tacada_num++;

                            // Update Tacada Num Team
                            team.incrementTacadaNum();
                        }

                        pgi.data.giveup = 1;

                        // Update Give Up Team
                        team.setGiveUp(1);

                        // Soma +1 no Bad Condute
                        pgi.data.bad_condute++;

                        // Update Bad Condute Team
                        team.incrementBadCondute();
                    }

                    // aqui os achievement de power shot int32_t putt beam impact e etc
                    update_sync_shot_achievement(_session, last_location);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Match::requestTranslateSyncShotData][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void requestTranslateFinishHoleData(Player _session, UserInfoEx _ui)

        {
            //CHECK_SESSION_BEGIN("requestTranslateFinishHole");

            try
            {

                var pgi = INIT_PLAYER_INFO("requestTranslateFinishHoleData",
                    "tentou finalizar hole dados no jogo",
                    _session);

                pgi.ui = _ui;

                // S  terminou o hole sem acerta se os 2 team n o acertou o hole
                if (!pgi.shot_sync.state_shot.display.acerto_hole
                    && !(m_teans[0].getAcertoHole())
                    && !(m_teans[1].getAcertoHole()))
                { // Terminou o Hole sem acerta ele, Give Up

                    var team = INIT_TEAM_INFO("requestTranslateFinishHoleData", _session);

                    var hole = m_course.findHole(pgi.hole);

                    if (hole == null)
                    {
                        throw new exception("[Match::requestFinishHoleData][Error] Player[UID=" + Convert.ToString(pgi.uid) + "] tentou finalizar os dados do hole no jogo, mas o hole[NUMERO=" + Convert.ToString(pgi.hole) + "] nao existe no course. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS,
                            400, 0));
                    }

                    // +1 que   giveup, s  add se n o passou o n mero de tacadas
                    if (pgi.data.tacada_num < hole.getPar().total_shot)
                    {
                        pgi.data.tacada_num++;

                        team.setTacadaNum(pgi.data.tacada_num);
                    }

                    // Ainda n o colocara o give up, o outro pacote, coloca nesse(muito dif cil, n o colocar s  se estiver com bug)
                    if (!(pgi.data.giveup > 0))
                    {
                        pgi.data.giveup = 1;

                        team.setGiveUp(1);
                    }
                }

                // Aqui Salva os dados do Pgi, os best Chipin, Long putt e best drive(max dist ncia)
                // N o sei se precisa de salvar aqui, j  que estou salvando no pgi User Info
                pgi.progress.best_chipin = _ui.best_chip_in;
                pgi.progress.best_long_puttin = _ui.best_long_putt;
                pgi.progress.best_drive = _ui.best_drive;

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Match::requestTranslateFinishHoleData][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        protected override bool checkEndGame(Player _session)

        {

            var pgi = INIT_PLAYER_INFO("checkEndGame",
                "tentou verificar se é o final do jogo",
                _session);

            return (m_course.findHoleSeq(pgi.hole) == m_ri.qntd_hole || ((m_players.Count % 2) == 1));
        }

        public override bool checkAllClearHole()

        {

            uint count = 0;
            bool ret = false;

            foreach (var _el in m_teans)
            {
                if (_el.getAcertoHole()
                    || _el.getGiveUp()
                    || _el.isQuit() > 0)
                {
                    count++;
                }
            }

            ret = (count == m_teans.Count);

            return ret;
        }


        public override void clearAllClearHole()
        {
            clear_all_clear_hole();
        }

        public override void clear_all_clear_hole()

        {

            m_teans.ForEach(_el =>
            {
                _el.setAcertoHole(0);
                _el.setGiveUp(0);
            });
        }

        void clear_teans()

        {
            if (!m_teans.empty())
            {
                m_teans.Clear();
            }
        }

        public override void updateTreasureHunterPoint()

        {


            if (!sTreasureHunterSystem.getInstance().isLoad()) sTreasureHunterSystem.getInstance().load();

            // Calcule Treasure Pontos - s  do team Red(vermelho)

            // Red team
            if (m_teans[0].getAcertoHole())
            {
                // S  se ele acertou o hole ele add mais treasure hunter point
                var hole = m_course.findHole(m_teans[0].getHole());

                if (hole == null)
                {
                    throw new exception("[VersusBase::updateTreasureHunterPoint][Error] tentou atualizar os pontos do Treasure Hunter no hole[NUMERO=" + Convert.ToString((ushort)m_teans[0].getHole()) + "], mas o hole nao existe. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY_BASE,
                        30, 0));
                }

                // 2x o valor ganho por que s  ganha item do treasure hunter point do time RED(vermelho)

                m_thi.treasure_point += (sTreasureHunterSystem.getInstance().calcPointNormal(m_teans[0].getTacadaNum(), hole.getPar().par) + m_thi.getPoint(m_teans[0].getTacadaNum(), (byte)hole.getPar().par)) * 2;
            }

            // Blue team
            if (m_teans[1].getAcertoHole())
            {
                // S  se ele acertou o hole ele add mais treasure hunter point
                var hole = m_course.findHole(m_teans[1].getHole());

                if (hole == null)
                {
                    throw new exception("[VersusBase::updateTreasureHunterPoint][Error] tentou atualizar os pontos do Treasure Hunter no hole[NUMERO=" + Convert.ToString((ushort)m_teans[1].getHole()) + "], mas o hole nao existe. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY_BASE,
                        30, 0));
                }


                m_thi_blue.treasure_point += (sTreasureHunterSystem.getInstance().calcPointNormal(m_teans[1].getTacadaNum(), hole.getPar().par) + m_thi_blue.getPoint(m_teans[1].getTacadaNum(), (byte)hole.getPar().par)) * 2;
            }

            // no Match passa 2x o pacote132 treasure Hunter point, varia um pouco a valor
            // team Vermelho
            // Team Azul e Vermelho

            // Mostra score board
            var p = new PangyaBinaryWriter((ushort)0x132);

            p.WriteUInt32(m_thi.treasure_point);

            // No Modo Match passa outro valor tbm

            packet_func.game_broadcast(this,
                p, 1);

            // Mostra score board dos 2 teans
            p.init_plain(0x132);

            // Red Team
            p.WriteUInt32(m_thi.treasure_point);

            // Blue Team
            p.WriteUInt32(m_thi_blue.treasure_point);

            packet_func.game_broadcast(this,
                p, 1);
        }

        public override bool checkNextStepGame(Player _session)

        {

            var ret = false;

            try
            {

                var pgi = INIT_PLAYER_INFO("checkNextStepGame",
                    "tentou verificar o proximo passo do jogo",
                    _session);

                if (m_players.Count > 0)
                {

                    if (m_player_turn == null)
                    {

                        // Player Turn ainda n o foi decidido, termina o jogo
                        m_state_vs.setStateWithLock(STATE_VERSUS.WAIT_END_GAME);

                        ret = true; // Termina o Game

                    }
                    else if (m_player_turn == pgi)
                    {

                        m_state_vs.setStateWithLock(STATE_VERSUS.WAIT_END_GAME);

                        ret = true; // Termina o Game

                    }
                    else if (!checkPlayerTurnExistOnGame())
                    {

                        // Player Turn n o est  mais no jogo, termina o jogo
                        m_state_vs.setStateWithLock(STATE_VERSUS.WAIT_END_GAME);

                        ret = true; // Termina o Game

                    }
                    else
                    {
                        m_flag_next_step_game = 2; // Termina o game
                    }
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Match::checkNextStepGame][ErroSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            return (ret);
        }

        public override void requestSaveInfo(Player _session, int _option) 
        {
            //CHECK_SESSION_BEGIN("SaveInfo");

            try
            {

                var pgi = INIT_PLAYER_INFO("requestSaveInfo",
                    "tentou salvar o info do Player no Match",
                    _session);

                var team = INIT_TEAM_INFO("requestSaveInfo", _session);

                if (_option == 0)
                {
                    pgi.ui.team_game = 1;
                    pgi.ui.team_win = (m_team_win == team.getId()) ? 1 : 0;
                }
                else
                {
                    pgi.ui.team_game = 0;
                    pgi.ui.team_win = 0;
                }

                var hole_seq = m_course.findHoleSeq(pgi.hole);

                if (hole_seq == ushort.MaxValue)
                {
                    _smp.message_pool.getInstance().push(new message("[Match::requestSaveInfo][Error] Player[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou pegar sequencia do hole[NUMERO=" + Convert.ToString(pgi.hole) + "] no course, mas nao encontrou o hole no course. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
                else
                {
                    pgi.ui.team_hole = hole_seq;
                }

                base.requestSaveInfo(_session, _option);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Match::requestSaveInfo][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        void requestFinishExpGame()

        {

            if (m_players.Count > 0)
            {

                Player _session = null;
                float stars = m_course.getStar();
                float temp = 0.0f;
                int exp = 0;
                int hole_seq = 0;

                foreach (var team in m_teans)
                {

                    temp = 1.0f;

                    if (m_team_win != 2 && team.getId() != m_team_win)
                    {
                        temp -= 0.4f;
                    }

                    foreach (var el in team.getPlayers())
                    {

                        var pgi = INIT_PLAYER_INFO("requestFinishExpGame",
                            "tentou finalizar experiencia do jogo",
                            el);

                        hole_seq = (int)m_course.findHoleSeq(pgi.hole);

                        // Ele est  no primeiro hole e n o acertou ele, s  da experi ncia se ele tiver acertado o hole
                        if (hole_seq == 1 && !(team.getAcertoHole()))
                        {
                            hole_seq = 0;
                        }

                        if ((_session = findSessionByUID(pgi.uid)) != null)
                        {

                            exp = (int)(1 * m_players.Count * (hole_seq > 0 ? hole_seq : 0) * stars);
                            exp = (int)(exp * TRANSF_SERVER_RATE_VALUE(pgi.used_item.rate.exp) * TRANSF_SERVER_RATE_VALUE(m_rv.exp));
                            exp = (int)((float)exp * temp);

                            if (pgi.level < 70)
                            {
                                pgi.data.exp = (int)exp;
                            }
                        }

                        _smp.message_pool.getInstance().push(new message("[Match::requestFinishExpGame][Log] Player[UID=" + Convert.ToString(pgi.uid) + "] ganhou " + Convert.ToString(pgi.data.exp) + " de experience.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                    }
                }
            }
        }

        void requestCalculeTeamWin()

        {

            m_team_win = 0; // Red

            if (m_teans[0].getPoint() == m_teans[1].getPoint())
            {

                if (m_teans[0].getPang() == m_teans[1].getPang())
                {
                    m_team_win = 2; // Empate(Draw)
                }
                else if (m_teans[1].getPang() > m_teans[0].getPang())
                {
                    m_team_win = 1; // Blue
                }

            }
            else if (m_teans[1].getPoint() > m_teans[0].getPoint())
            {
                m_team_win = 1; // Blue
            }
        }

        void requestUpdateTeamPang()

        {

            foreach (var team in m_teans)
            {

                foreach (var el in team.getPlayers())
                {

                    var pgi = INIT_PLAYER_INFO("requestUpdateTeamPang",
                        "tentou atualizar pangs do Player[UID=" + Convert.ToString(el.m_pi.uid) + "] no Match",
                        el);

                    // Update Pang Player To Team Pang and Bonus Pang
                    pgi.data.pang = team.getPang();
                    pgi.data.bonus_pang = team.getBonusPang();
                }
            }
        }

        void finish()

        {

            m_match_state = false; // Terminou o versus

            m_game_init_state = 2; // Terminou o jogo

            requestCalculeRankPlace();

            // Calcula o team(time) que ganhou o Match
            requestCalculeTeamWin();

            requestFinishExpGame();

            requestDrawTreasureHunterItem();

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

        void requestFinishTeamHole()

        {

            finishHole();

            if (m_player_turn == null)
            {
                throw new exception("[Match::requestFinishTeamHole][Error] m_player_turn is invalid(nullptr). Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MATCH,
                    100, 0));
            }

            var hole = m_course.findHole(m_player_turn.hole);

            if (hole == null)
            {
                throw new exception("[Match::requestFinishTeamHole][Error] Player[UID=" + Convert.ToString(m_player_turn.uid) + "] tentou finalizar hole[NUMERO=" + Convert.ToString((ushort)m_player_turn.hole) + "] no jogo, mas o numero do hole is invalid. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MATCH,
                    20, 0));
            }

            // Win Last And Point Team
            m_teans[0].setLastWin(0);
            m_teans[1].setLastWin(0);

            if (m_teans[0].getAcertoHole()
                && m_teans[0].getStateFinish() > 0
                && m_teans[1].getStateFinish() == 0
                && m_teans[0].getTacadaNum() < (m_teans[1].getTacadaNum() + 1))
            { // Win
                m_teans[0].setLastWin(1);
                m_teans[0].incrementPoint();
            }
            else if (m_teans[1].getAcertoHole() && m_teans[1].getStateFinish() > 0 && m_teans[0].getStateFinish() == 0 && m_teans[1].getTacadaNum() < (m_teans[0].getTacadaNum() + 1))
            { // Win
                m_teans[1].setLastWin(1);
                m_teans[1].incrementPoint();
            }

            m_teans[0].setStateFinish(0);
            m_teans[1].setStateFinish(0);

            foreach (var el in m_teans)
            {

                el.setStateFinish(0);

                el.incrementTotalTacadaNum(el.getTacadaNum());

                el.setScore((int)(el.getTacadaNum() - hole.getPar().par));

                // !!!@@ N o sei por que estou zerando aqui, no antigo, mas vamos testar
                // Deu certo, mas vou deixar o coment rio para duvidas futuras
                el.setPlayerStartHole(0);
                //el.setTacadaNum(0);
            }
        }

        void requestFinishData(Player _session)

        {

            // Finish Artefact Frozen Flame agora   direto no Finish Item Used Game
            requestFinishItemUsedGame(_session);

            requestSaveDrop(_session);

            rain_hole_consecutivos_count(_session); // conta os achievement de chuva em holes consecutivas

            score_consecutivos_count(_session); // conta os achievement de back-to-back(2 ou mais score iguais consecutivos) do Player

            rain_count(_session); // Aqui achievement de rain count

            //var pgi = INIT_PLAYER_INFO("requestFinishData", "tentou finalizar dados do jogo", &_session);

            // Resposta Treasure Hunter Item Draw
            sendTreasureHunterItemDrawGUI(_session);

            // Resposta terminou game - Drop Itens
            sendDropItem(_session);

            // Resposta terminou game - Placar
            sendPlacar(_session);
        }

        public override void sendPlacar(Player _session)

        {

            var p = new PangyaBinaryWriter((ushort)0x91);

            p.WriteByte((byte)m_players.Count);

            foreach (var el in m_players)
            {

                var pgi = INIT_PLAYER_INFO("sendPlacar",
                    "tentou enviar o placar do jogo",
                    el);

                var team = INIT_TEAM_INFO("sendPlacar", el);

                p.WriteInt32(el.m_oid);
                p.WriteByte((byte)getRankPlace(el));
                p.WriteByte(0x7F);
                p.WriteByte((byte)pgi.data.total_tacada_num);

                p.WriteUInt16((ushort)pgi.data.exp);
                p.WriteUInt64(team.getPang());
                p.WriteUInt64(team.getBonusPang());

                // Valor que usa no Pang Battle, valor de pang que ganhou ou perdeu
                // Como aqui   vs Base deixa o valor 0
                p.WriteUInt64(0L);
            }

            p.WriteByte((byte)m_teans[0].getPoint());
            p.WriteByte((byte)m_teans[1].getPoint());
            p.WriteByte(m_team_win);

            packet_func.session_send(p,
                _session, 1);
        }

        protected override void sendFinishMessage(Player _session)

        {

            var pgi = INIT_PLAYER_INFO("sendFinishMessage",
                "tentou enviar message no chat que o Player terminou o jogo",
                _session);

            var team = INIT_TEAM_INFO("sendFinishMessage", _session);

            var p = new PangyaBinaryWriter((ushort)0x40);

            p.WriteByte(16); // Msg que terminou o game

            p.WriteString(_session.m_pi.nickname);
            p.WriteUInt16(0); // Size Msg

            p.WriteUInt32(team.getPoint());
            p.WriteUInt64(team.getPang());
            p.WriteByte(pgi.assist_flag);

            packet_func.game_broadcast(this,
                p, 1);
        }

        public override void sendReplyFinishLoadHole()

        {

            try
            {

                PlayerGameInfo pgi = requestCalculePlayerTurn();

                var hole = m_course.findHole(pgi.hole);

                if (hole == null)
                {
                    throw new exception("[Match::requestFinishLoadHole][Error] Player[UID=" + Convert.ToString(pgi.uid) + "] tentou finalizar carregamento do hole[NUMERO=" + Convert.ToString(pgi.hole) + "], mas nao conseguiu encontrar o hole no course. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                        201, 0));
                }

                var team = INIT_TEAM_INFO("requestFinishLoadHole", findSessionByPlayerGameInfo(pgi));

                // Resposta de tempo do hole
                var p = new PangyaBinaryWriter((ushort)0x9E);

                p.WriteUInt16(hole.getWeather());
                p.WriteByte(0); // Option do tempo, sempre peguei zero aqui dos pacotes que vi

                packet_func.game_broadcast(this,
                    p, 1);

                var wind_flag = initCardWindPlayer(m_player_turn, hole.getWind().wind);

                // Resposta do vento do hole
                p.init_plain(0x5B);

                p.WriteByte(hole.getWind().wind + wind_flag);
                p.WriteByte((wind_flag < 0) ? 1 : 0); // Flag de card de vento, aqui   a qnd diminui o vento, 1 Vento azul
                p.WriteUInt16(team.getDegree());
                p.WriteByte(1); // Flag do vento, 1 Reseta o Vento, 0 soma o vento que nem o comando gm \wind do pangya original

                packet_func.game_broadcast(this,
                    p, 1);

                // Resposta passa o oid do Player que vai come a o Hole
                p.init_plain(0x53);

                if (m_player_turn == null)
                {
                    _smp.message_pool.getInstance().push(new message("[Match::requestFinishLoadHole][Error] player_turn is invalid(nullptr)", type_msg.CL_FILE_LOG_AND_CONSOLE));

                    p.WriteUInt32(0);
                }
                else
                {
                    p.WriteInt32(m_player_turn.oid);
                }

                packet_func.game_broadcast(this,
                    p, 1);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Match::sendReplyFinishLoadHole][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void sendReplyFinishCharIntro()

        {

            PangyaBinaryWriter p = new PangyaBinaryWriter();

            try
            {

                foreach (var el in m_teans)
                {

                    el.setTacadaNum(0);

                    el.setGiveUp(0);
                }

                // Resposta para Finish Char Intro
                p.init_plain(0x90);

                packet_func.game_broadcast(this,
                    p, 1);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Match::sendReplyFinishCharIntro][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

        }

        public override int checkEndShotOfHole(Player _session)

        {
            //CHECK_SESSION_BEGIN("checkEndShotOfHole");

            try
            {

                // Agora verifica o se ele acabou o hole e essas coisas
                var pgi = INIT_PLAYER_INFO("checkEndShotOfHole",
                    "tentou verificar a ultima tacada do hole no jogo",
                    _session);

                //pgi.finish_shot = 1;

                if (pgi.data.bad_condute >= 3)
                {
                    return 2;
                }
                else
                {
                    setFinishShot(pgi);
                }


            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Match::checkEndShotOfHole][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            return 0;
        }

        public override void changeTurn()

        {

            try
            {

                if (m_player_turn == null)
                {
                    throw new exception("[Match::changeTurn][Error] PlayerGameInfo m_player_turn is invalid(nullptr). Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MATCH,
                        100, 0));
                }

                var team = INIT_TEAM_INFO("changeTurn", findSessionByPlayerGameInfo(m_player_turn));

                // Para o tempo do Player do turno
                stopTime();

                var hole_seq = m_course.findHoleSeq(m_player_turn.hole);

                int state = 0;
                int hole_diff = m_ri.qntd_hole - hole_seq;

                if (checkAllClearHole())
                {
                    state = 1;
                }
                else if ((m_players.Count % 2) == 1)
                {
                    state = 1;
                }
                else if (m_teans[0].getAcertoHole() && !m_teans[1].getTimeout() && (m_teans[0].getTacadaNum() < (m_teans[1].getTacadaNum() + 1)))
                {
                    state = 1;
                }
                else if (m_teans[1].getAcertoHole() && !m_teans[0].getTimeout() && (m_teans[1].getTacadaNum() < (m_teans[0].getTacadaNum() + 1)))
                {
                    state = 1;
                }
                else if (m_teans[0].getAcertoHole() && !m_teans[1].getTimeout() && (m_teans[0].getTacadaNum() == (m_teans[1].getTacadaNum() + 1)) && (int)(m_teans[0].getPoint() - m_teans[1].getPoint()) > hole_diff)
                {
                    state = 1;
                }
                else if (m_teans[1].getAcertoHole() && !m_teans[0].getTimeout() && (m_teans[1].getTacadaNum() == (m_teans[0].getTacadaNum() + 1)) && (int)(m_teans[1].getPoint() - m_teans[0].getPoint()) > hole_diff)
                {
                    state = 1;
                }

                // Limpa dados que usa para cada tacada
                clearDataEndShot(m_player_turn);

                // Verifica se todos fizeram o hole, ou o outro team venceu por que chipo antes com menos tacada
                if (state != 0)
                {

                    clear_all_flag_sync();

                    // clear teans timeout type
                    foreach (var el in m_teans)
                    {
                        el.setTimeout(0);
                    }

                    //finishHole();
                    requestFinishTeamHole();

                    changeHole();

                    // Clear Acerto hole e Give Up
                    // Aqui zera mesmo se j  zero, por que n o sei se foi por all clear ou outras regras
                    clearAllClearHole();

                }
                else
                { // Troca o Turno

                    clear_all_flag_sync();

                    // clear teans timeout type
                    foreach (var el in m_teans)
                    {
                        el.setTimeout(0);
                    }

                    // Recalcula Turno
                    requestCalculePlayerTurn();

                    if (m_player_turn == null)
                    {
                        throw new exception("[Match::changeTurn][Error] PlayerGameInfo m_player_turn is invalid(nullptr). Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MATCH,
                            100, 1));
                    }

                    var hole = m_course.findHole(m_player_turn.hole);

                    if (hole == null)
                    {
                        throw new exception("[Match::changeTurn][Error] Player[UID=" + Convert.ToString(m_player_turn.uid) + "] tentou encontrar o hole[NUMERO=" + Convert.ToString(m_player_turn.hole) + "] do course no jogo, mas nao foi encontrado. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MATCH,
                            101, 0));
                    }

                    team = INIT_TEAM_INFO("changeTurn", findSessionByPlayerGameInfo(m_player_turn));

                    var wind_flag = initCardWindPlayer(m_player_turn, hole.getWind().wind);

                    // Resposta do vento do hole
                    var p = new PangyaBinaryWriter((ushort)0x5B);

                    p.WriteByte(hole.getWind().wind + wind_flag);
                    p.WriteByte((wind_flag < 0) ? 1 : 0); // Flag de card de vento, aqui   a qnd diminui o vento, 1 Vento azul
                    p.WriteUInt16(team.getDegree());
                    p.WriteByte(1); // Flag do vento, 1 Reseta o Vento, 0 soma o vento que nem o comando gm \wind do pangya original

                    packet_func.game_broadcast(this,
                        p, 1);

                    // Resposta passa o oid do Player que vai come a o Hole
                    p.init_plain(0x63);

                    if (m_player_turn == null)
                    {
                        _smp.message_pool.getInstance().push(new message("[Match::changeTurn][Error] player_turn is invalid(nullptr)", type_msg.CL_FILE_LOG_AND_CONSOLE));

                        p.WriteUInt32(0);
                    }
                    else
                    {
                        p.WriteInt32(m_player_turn.oid);
                    }

                    // !!@@@
                    // Aqui tem 2 bytes a+, int16 de um valor, que acho que acontece de ver em quando, ou s  no pang battle isso estava escrito no meu outro
                    // acho que possa ser do pang battle, certaza, aqui acabei de ver na classe pang battle do antigo

                    packet_func.game_broadcast(this,
                        p, 1);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Match::changeTurn][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void CCGChangeWind(Player _gm,
                byte _wind, ushort _degree)

        {

            try
            {

                if (m_player_turn == null)
                {
                    throw new exception("[Match::CCGChangeWind][Error] Player[UID=" + Convert.ToString(_gm.m_pi.uid) + "] tentou executar o comando de troca de vento no versus na sala[NUMERO=" + Convert.ToString(m_ri.numero) + "], mas o player_turn do versus é invalido. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MATCH,
                        1, 0x5700100));
                }

                var hole = m_course.findHole(m_player_turn.hole);

                if (hole == null)
                {
                    throw new exception("[Match::CCGChangeWind][Error] Player[UID=" + Convert.ToString(_gm.m_pi.uid) + "] tentou executar o comando de troca de vento no versus na sala[NUMERO=" + Convert.ToString(m_ri.numero) + "], mas o nao encontrou o hole[VALUE=" + Convert.ToString((short)m_player_turn.hole) + "] no course. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MATCH,
                        2, 0x5700100));
                }

                var wind = hole.getWind();

                // Change Wind of Hole
                wind.wind = _wind;

                hole.setWind(wind);

                // Change Degree of team
                m_team_turn.setDegree((ushort)(_degree % LIMIT_DEGREE));

                // Log
                _smp.message_pool.getInstance().push(new message("[Match::CCGChangeWind][Log] [GM] Player[UID=" + Convert.ToString(_gm.m_pi.uid) + "] trocou o vento e graus da sala[NUMERO=" + Convert.ToString(m_ri.numero) + ", VENTO=" + Convert.ToString((ushort)_wind + 1) + ", GRAUS=" + Convert.ToString(_degree) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                var wind_flag = initCardWindPlayer(m_player_turn, hole.getWind().wind);

                // UPDATE ON GAME
                var p = new PangyaBinaryWriter((ushort)0x5B);

                p.WriteByte(hole.getWind().wind + wind_flag); // Wind
                p.WriteByte((wind_flag < 0) ? 1 : 0); // Card Wind Flag, minus wind type
                p.WriteUInt16(m_team_turn.getDegree()); // Degree
                p.WriteByte(1); // Flag 1 = Reset Degree, 0 = Plus Degree

                packet_func.game_broadcast(this,
                    p, 1);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Match::CCGChangeWind][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                throw;
            }

        }

        public override PlayerGameInfo requestCalculePlayerTurn()

        {

            var team = requestCalculeTeamTurn();

            var pgi = INIT_PLAYER_INFO("requestCalculePlayerTurn",
                "tentou calcular o Player turno no Match",
                team.requestCalculePlayerTurn(m_course.findHoleSeq(team.getHole())));

            m_player_turn = pgi;

            return m_player_turn;
        }

        Team requestCalculeTeamTurn()

        {

            if (m_player_info.Any())
            {

                var hole = m_course.findHole(m_player_info.First().Value.hole);

                if (hole == null)
                {
                    _smp.message_pool.getInstance().push(new message("[Match::requestCalculeTeamTurn][Error] Player[UID=" + Convert.ToString(m_player_info.begin().Value.uid) + "] o hole[NUMERO=" + Convert.ToString(m_player_info.begin().Value.hole) + "] nao foi encontrado no course. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                    m_player_turn = null;

                    return null;
                }
                v_team_order_turn.Clear();

                foreach (var el in m_teans)
                {
                    v_team_order_turn.Add(new TeamOrderTurnCtx(el, hole));
                }

                if (v_team_order_turn.Count == 0)
                {
                    _smp.message_pool.getInstance().push(new message("[Match::requestCalculeTeamTurn][Error] nao tem players, para calcular o turno. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                    m_player_turn = null;

                    return null;
                }

                v_team_order_turn.Sort(sort_team_turn);

                m_team_turn = v_team_order_turn[0].team;//prmeiro
            }

            return m_team_turn;
        }

        void init_team_player_position()

        {

            bool red_flag = false;
            bool blue_flag = false;

            foreach (var el in m_players)
            {

                var team = INIT_TEAM_INFO("init_team_player_position", el);

                if (team.getId() == 0 && red_flag == false)
                {

                    team.sort_player(el.m_pi.uid);

                    red_flag = true;

                }
                else if (team.getId() == 1 && !blue_flag)
                {

                    team.sort_player(el.m_pi.uid);

                    blue_flag = true;
                }
            }

        }

        public override bool finish_game(Player _session, int option = 0)

        {

            if (_session.getState()
                && _session.isConnected()
                && m_players.Count > 0)
            {

                var pgi = INIT_PLAYER_INFO("finish_game",
                    "tentou finalizar o jogo",
                    _session);

                // Terminou o hole, finalizar o hole por ele
                if (pgi.shot_sync.state_shot.display.acerto_hole || pgi.data.giveup > 0)
                {

                    requestFinishHole(_session, 0);

                    requestUpdateItemUsedGame(_session);
                }

                pgi.finish_game = 1;

                if (PlayersCompleteGameAndClear() || option == 2)
                {

                    PangyaBinaryWriter p = new PangyaBinaryWriter();

                    // Verifica se   o primeiro hole e se nem todos terminaram o hole
                    if (m_course.findHoleSeq(pgi.hole) == 1 && !checkAllClearHole())
                    {

                        foreach (var el in m_players)
                        {

                            pgi = INIT_PLAYER_INFO("finish_game",
                               "tentou finalizar o versus",
                               el);

                            if (pgi.flag == PlayerGameInfo.eFLAG_GAME.PLAYING)
                            {

                                requestSaveInfo(el, 2);

                                if (pgi.finish_item_used == 0)
                                {
                                    requestFinishItemUsedGame(el);
                                }

                                p.init_plain(0x67);

                                packet_func.session_send(p,
                                    el, 1);

                                //pgi.type = PlayerGameInfo::eFLAG_GAME::END_GAME;
                                setGameFlag(pgi, PlayerGameInfo.eFLAG_GAME.END_GAME);
                            }
                        }

                        m_game_init_state = 2; // Acabou o Match

                        return true;

                    }
                    else
                    {

                        if (m_match_state) // Deixa o cliente envia o pacote para finalizar o jogo, depois que ele mostrar os placares
                        {
                            finish_match(1);
                        }
                        else
                        {

                            foreach (var el in m_players)
                            {

                                pgi = INIT_PLAYER_INFO("finish_game",
                                   "tentou finalizar o versus",
                                   el);

                                if (pgi.flag == PlayerGameInfo.eFLAG_GAME.PLAYING)
                                {

                                    requestSaveInfo(el, 0);

                                    // D  Exp para o Caddie E Mascot Tamb m
                                    if (pgi.data.exp > 0)
                                    { // s  add exp se for maior que 0

                                        // Add Exp para o Player
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

                                    // Update Mascot Info ON GAME, se o Player estiver com um mascot equipado
                                    if (el.m_pi.ei.mascot_info != null)
                                    {
                                        var pck = packet_func.pacote06B(el.m_pi, 8);

                                        packet_func.session_send(pck,
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

                                    p.WriteUInt64(0L);

                                    packet_func.session_send(p,
                                        el, 1);

                                    //pgi.type = PlayerGameInfo::eFLAG_GAME::FINISH;
                                    setGameFlag(pgi, PlayerGameInfo.eFLAG_GAME.FINISH);
                                }
                            }

                            m_game_init_state = 2; // Acabou o Match

                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public int sort_team_turn(TeamOrderTurnCtx t1, TeamOrderTurnCtx t2)
        {
            _smp.message_pool.getInstance().push(new message("[Match::sort_team_turn][Log] Foi Chamado com sucesso!", type_msg.CL_FILE_LOG_AND_CONSOLE));

            var diff1 = t1.hole.getPinLocation().diffXZ(t1.team.getLocation());
            var diff2 = t2.hole.getPinLocation().diffXZ(t2.team.getLocation());

            // Ambos deram give up ou acertaram
            if ((t1.team.getAcertoHole() || t1.team.getGiveUp()) &&
                (t2.team.getAcertoHole() || t2.team.getGiveUp()))
                return 0; // iguais

            // T1 ainda jogando, T2 terminou
            if (!t1.team.getAcertoHole() && (t2.team.getAcertoHole() || t2.team.getGiveUp()))
                return 1;

            // T1 não jogou, T2 já jogou
            if (t1.team.getTacadaNum() == 0 && t2.team.getTacadaNum() > 0)
                return 1;

            // Mais distante do buraco
            if (diff1 > diff2 && !t1.team.getAcertoHole() && !t1.team.getGiveUp())
                return 1;

            // Mesmo diff, menos tacadas
            if (diff1 == diff2 && t1.team.getTacadaNum() < t2.team.getTacadaNum() && !t1.team.getAcertoHole() && !t1.team.getGiveUp())
                return 1;

            // Mesmo diff e tacadas, mais vitórias
            if (diff1 == diff2 && t1.team.getTacadaNum() == t2.team.getTacadaNum() && t1.team.getLastWin() > t2.team.getLastWin() && !t1.team.getAcertoHole() && !t1.team.getGiveUp())
                return 1;

            // Mesmo diff, tacadas, vitórias, mais pontos
            if (diff1 == diff2 && t1.team.getTacadaNum() == t2.team.getTacadaNum() && t1.team.getLastWin() == t2.team.getLastWin() && t1.team.getPoint() > t2.team.getPoint() && !t1.team.getAcertoHole() && !t1.team.getGiveUp())
                return 1;

            // Mesmo tudo acima, mais pang
            if (diff1 == diff2 && t1.team.getTacadaNum() == t2.team.getTacadaNum() && t1.team.getLastWin() == t2.team.getLastWin() && t1.team.getPoint() == t2.team.getPoint() && t1.team.getPang() > t2.team.getPang() && !t1.team.getAcertoHole() && !t1.team.getGiveUp())
                return 1;

            return 0;
        }

        public new void SQLDBResponse(int _msg_id,
                Pangya_DB _pangya_db,
                object _arg)

        {

            if (_arg == null)
            {
                _smp.message_pool.getInstance().push(new message("[Match::SQLDBResponse][Warning] _arg is nullptr com msg_id = " + Convert.ToString(_msg_id), type_msg.CL_FILE_LOG_AND_CONSOLE));
                return;
            }

            // Por Hora s  sai, depois fa o outro tipo de tratamento se precisar
            if (_pangya_db.getException().getCodeError() != 0)
            {
                _smp.message_pool.getInstance().push(new message("[Match::SQLDBResponse][Error] " + _pangya_db.getException().getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                return;
            }


            var game = (GameBase)(_arg);

            switch (_msg_id)
            {
                case 0:
                default:
                    break;
            }
        }
    }
}