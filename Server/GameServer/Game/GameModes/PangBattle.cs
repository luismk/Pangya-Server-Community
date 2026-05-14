using System;
using System.Collections.Generic;
using System.Linq;
using Pangya_GameServer.Game.Base;
using Pangya_GameServer.Game.System;
using Pangya_GameServer.Models;
using Pangya_GameServer.PacketFunc;

using Pangya_GameServer.UTIL;
using PangyaAPI.Network.PangyaPacket;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Models;
using PangyaAPI.Utilities.Log;
using static Pangya_GameServer.Models.DefineConstants;
namespace Pangya_GameServer.Game.GameModes
{
    public class PangBattle : VersusBase
    {
        private bool m_pang_battle_state;
        PangBattleData m_pbd;

        List<PlayerGameInfo> m_player_order_pb;			// Lista de player do rank do jogo do Pang Battle

        public PangBattle(List<Player> _players, RoomInfoEx _ri, RateValue _rv, bool _channel_rookie) : base(_players, _ri, _rv, _channel_rookie)
        {


            this.m_pang_battle_state = false;
            this.m_pbd = new PangBattleData();
            m_player_order_pb = new List<PlayerGameInfo>();

            if (!sTreasureHunterSystem.getInstance().isLoad())
            {
                sTreasureHunterSystem.getInstance().load();
            }

            var course = sTreasureHunterSystem.getInstance().findCourse((byte)(m_ri.getMap() & 0x7F));

            if (course == null)
            {
                _smp.message_pool.getInstance().push(new message("[PangBattle::PangBattle][Error] tentou pegar o course do Treasure Hunter System, mas o course[COURSE=" + Convert.ToString((ushort)(m_ri.getMap() & 0x7F)) + "] nao existe no sistema", type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
            else
            {
                sTreasureHunterSystem.getInstance().updateCoursePoint(course, -1); // -1 ponto a cada jogo iniciado
            }

            // Aqui tem que inicializar os players info
            initAllPlayerInfo();

            // Initialize the Achievement of player
            foreach (var el in m_players)
            {

                // Achievement
                var pgi = INIT_PLAYER_INFO("PangBattle",
                    "tentou inicializar o counter item do Versus",
                    el);

                initAchievement(el);
            }

            init_pang_battle_data();

            m_pang_battle_state = init_game();
        }

        ~PangBattle()
        {
            Dispose(false);
        }

        public override void Dispose(bool disposing)
        {
            if (disposedValue) return;

            if (disposing)
            {
                m_pang_battle_state = false;
                // Para o tempo do player Turn
                stopTime();

                // Salva os dados de todos os jogadores
                foreach (var el in m_players)
                {
                    finish_game(el);
                }

                deleteAllPlayer();

                if

        (m_player_order_pb.Any())
                {
                    m_player_order_pb.Clear();
                }

                LogDestruction();

            }
            base.Dispose(true);
        }

        public override bool deletePlayer(Player _session, int _option)
        {

            if (_session == null)
            {
                throw new exception("[PangBattle::deletePlayer][Error] tentou deletar um player, mas o seu endereco é nullptr.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS,
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
                    var pgi = INIT_PLAYER_INFO("deletePlayer",
                        "tentou sair do jogo",
                        _session);

                    if (m_game_init_state == 1 /*Come ou */)
                    {

                        var p = new PangyaBinaryWriter();

                        // Player Turn Para o tempo dele
                        if (m_player_turn == pgi)
                        {
                            stopTime();
                        }

                        var sessions = getSessions(it);

                        requestFinishItemUsedGame((it)); // Salva itens usados no Tourney

                        // Aqui atualizar os pangs do Pang Battle que o player saiu
                        if (m_pbd.m_hole > 0 && m_pbd.m_hole <= (short)m_pbd.v_player_win.Count)
                        {

                            pgi.data.pang_pang_battle -= (m_pbd.v_player_win[m_pbd.m_hole - 1].pang * m_pbd.v_player_win[m_pbd.m_hole - 1].vezes);

                            if (pgi.data.pang_pang_battle > 0 && m_players.Count > 1)
                            {

                                long div = (long)(pgi.data.pang_pang_battle / (m_players.Count - 1));

                                // Zera ele n o perde nenhum pang por que ele estava ganhando
                                pgi.data.pang_pang_battle = 0;

                                foreach (var ell in m_players)
                                {

                                    if (ell != null && ell.m_pi.uid != pgi.uid)
                                    {

                                        try
                                        {

                                            var _pgi = getPlayerInfo(ell);

                                            if (_pgi == null)
                                            {
                                                throw new exception("[PangBattle::deletePlayer][Error] PLAYER[UID=" + Convert.ToString(ell.m_pi.uid) + "] tentou pegar o player para add os pangs do pang battle que o player saiu, mas o game nao tem o info dele guardado. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANG_BATTLE,
                                                    1, 4));
                                            }

                                            _pgi.data.pang_pang_battle += div;
                                            _pgi.data.pang_battle_run_hole = 1;

                                        }
                                        catch (exception e)
                                        {

                                            _smp.message_pool.getInstance().push(new message("[PangBattle::deletePlayer][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                                        }
                                    }
                                }
                            }

                            // Extra value
                            m_pbd.v_player_win[m_pbd.m_hole - 1].pang_extra += (m_pbd.v_player_win[m_pbd.m_hole - 1].pang * m_pbd.v_player_win[m_pbd.m_hole - 1].vezes);
                        }

                        // Player saiu do Pang Battle tira 1
                        pgi.data.pang_battle_run_hole = -1;

                        // Log
                        _smp.message_pool.getInstance().push(new message("[PangBattle::deletePlayer][Log] Player[UID=" + Convert.ToString(it.m_pi.uid) + "] Correu do Pang Battle, perdeu " + Convert.ToString(pgi.data.pang_pang_battle) + " Pang(s).", type_msg.CL_FILE_LOG_AND_CONSOLE));

                        // Salva info do player
                        requestSaveInfo((it), (_option == 0x800) ? 5 /*N o conta quit */ : 1); // Quitou ou tomou DC

                        setGameFlag(pgi, PlayerGameInfo.eFLAG_GAME.QUIT);

                        // Resposta Player saiu do Jogo, tira ele do list de score
                        p.init_plain(0x61);

                        p.WriteInt32(it.m_oid);

                        packet_func.vector_send(p,
                            sessions, 1);

                        // Resposta Player saiu do jogo MSG
                        p.init_plain(0x40);

                        p.WriteByte(8); // Player Saiu Msg (8 = Pang Battle, 2 = Versus e Match)

                        p.WriteString(it.m_pi.nickname);

                        p.WriteUInt16(0); // size Msg, n o precisa de msg o pangya j  manda na opt 2

                        packet_func.vector_send(p,
                            sessions, 1);

                        sendUpdateInfoAndMapStatistics(_session, -1);

                        ret = checkNextStepGame(_session);

                    }
                    else if (m_game_init_state == 2 && !(pgi.finish_game > 0))
                    {

                        // Acabou
                        requestSaveInfo((it), 0);
                    }

                    // Deleta o player por give up ou time out, ele conta os achievements dele, tem o counter item 0x6C400004u Normal Game Complete
                    // Envia os achievements para ele para ficar igual ao original
                    if (m_game_init_state == 1 /*Come ou */

                        && pgi.data.bad_condute >= 3
                        && (pgi.data.time_out >= 3 || pgi.data.giveup >= 3))
                    {

                        // Achievements
                        rain_hole_consecutivos_count(_session); // conta os achievement de chuva em holes consecutivas

                        score_consecutivos_count(_session); // conta os achievement de back-to-back(2 ou mais score iguais consecutivos) do player

                        rain_count(_session); // Aqui achievement de rain count

                        pgi.sys_achieve.incrementCounter(0x6C400004u /*Normal game complete */);

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
                    _smp.message_pool.getInstance().push(new message("[PangBattle::deletePlayer][Warning] player ja foi excluido do game.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                } 
                // Evitar deadlock com a thread checkVersusTurn - Libera
                m_state_vs.unlock();

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[PangBattle::deletePlayer][Error] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                 
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
            //REQUEST_BEGIN("InitHole");

            try
            {

                // Chama a base para inicializar o hole do player
                base.requestInitHole(_session, _packet);

                var pgi = INIT_PLAYER_INFO("requestInitHole",
                    "tentou inicializar o hole",
                    _session);

                // Aqui atualiza a sequ ncia do hole do Pang Battle dados
                var seq_hole = m_course.findHoleSeq(pgi.hole);

                if (m_pbd.m_hole == -1 || m_pbd.m_count_finish_hole < m_pbd.v_player_win.Count)
                {
                    m_pbd.m_hole = (short)seq_hole;
                }
                else
                {

                    m_pbd.m_hole_extra = (short)seq_hole;

                    m_pbd.m_hole_extra_flag = true; // Entrou no hole extra
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[PangBattle::requestInitHole][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }
        public override void changeHole()
        {

            if (m_players.Count <= 0 || checkEndGame(m_players.begin()))
            {
                finish_pang_battle(0);
            }
            else if (m_players.Count > 0)
            {
                // Resposta terminou o hole
                updateFinishHole(); // Terminou
            }
        }
        public override void finishHole()
        {

            // Mais um hole finalizado, soma os hole extra tamb m se tiver
            m_pbd.m_count_finish_hole++;

#if _WIN32
		
#elif __linux__
		
#endif

            foreach (var el in m_players)
            {

                requestFinishHole(el, 0);

                requestUpdateItemUsedGame(el);
            }

#if _WIN32
		
#elif __linux__
		
#endif
        }
        public override void requestInitShot(Player _session, packet _packet)
        {
            //REQUEST_BEGIN("InitShot");

            var p = new PangyaBinaryWriter();

            try
            {

                var pgi = INIT_PLAYER_INFO("requestInitShot",
                    "tentou iniciar tacada no jogo",
                    _session);


                if (pgi.init_shot == 1u)
                {

                    _smp.message_pool.getInstance().push(new message("[PangBattle::requestInitShot][Log] Player[UID=" + Convert.ToString(_session.m_pi.uid) + "] o server ja recebeu o pacote12 Init Shot. ignora esse.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                    return;

                }
                else
                {
                    pgi.init_shot = 1;
                }

                // Stop time turn
                pgi.bar_space.setState(0); // Volta para 1 depois que taca, era esse meu coment rio no antigo

                // para o tempo da tacada ele acabou de tacar
                stopTime();

                pgi.tempo = 0; // Reseta o tempo
                               // end

                ShotDataEx sd = new ShotDataEx();

                #region Read Shot Sync Data
                sd.option = _packet.ReadUInt16();

                if (sd.option == 1)
                {
                    sd.power_shot.option = _packet.ReadByte();
                    sd.power_shot.decrease_power_shot = _packet.ReadInt32();
                    sd.power_shot.increase_power_shot = _packet.ReadInt32();
                }

                //READ SHOTDataBase primeiro, primeiro se lê a classe base, e depois a classe que herda.

                sd.bar_point[0] = _packet.ReadSingle();
                sd.bar_point[1] = _packet.ReadSingle();

                sd.ball_effect[0] = _packet.ReadSingle();
                sd.ball_effect[1] = _packet.ReadSingle();

                sd.acerto_pangya_flag = _packet.ReadByte();
                sd.special_shot.ulSpecialShot = _packet.ReadUInt32();
                sd.time_hole_sync = _packet.ReadUInt32();
                sd.mira = _packet.ReadSingle();

                sd.time_shot = _packet.ReadUInt32();
                sd.bar_point1 = _packet.ReadSingle();
                sd.club = _packet.ReadByte();

                sd.fUnknown[0] = _packet.ReadSingle();
                sd.fUnknown[1] = _packet.ReadSingle();

                sd.impact_zone_pixel = _packet.ReadSingle();

                sd.natural_wind[0] = _packet.ReadInt32();
                sd.natural_wind[1] = _packet.ReadInt32();

                #endregion

                m_state_vs.setStateWithLock(STATE_VERSUS.SHOTING);

#if _DEBUG
			// Log Shot Data Ex
			_smp.message_pool.getInstance().push(new message("Log Shot Data Ex:\n\r" + sd.toString(), type_msg.CL_FILE_LOG_AND_CONSOLE));
#endif // _DEBUG

                // Aqui n o manda resposta no TourneyBase ou Practice, mas outro modos(VS, MATCH) manda e outros tamb m n o(TOURNEY)
                p.init_plain(0x55);

                p.WriteInt32(_session.m_oid);

                p.WriteBytes(sd.ToArrayEx());

                packet_func.game_broadcast(this,
                    p, 1);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[PangBattle::requestInitShot][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }
        public void finish_pang_battle(int _option)
        {

            if (m_players.Count > 0 && m_game_init_state == 1)
            {

                foreach (var el in m_players)
                {

                    var pgi = INIT_PLAYER_INFO("finish_pang_battle",
                        "tentou terminar o versus", el);

                    pgi.sys_achieve.incrementCounter(0x6C400004u /*Normal game complete */);

                    requestCalculePang(el);

                    updatePlayerAssist(el);
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


                var p = (Player)(_quem);

                var pgi = INIT_PLAYER_INFO("timeIsOver",
                    "tentou acabar o tempo do turno no jogo",
                    p);

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
                _smp.message_pool.getInstance().push(new message("[PangBattle::timeIsOver][Warning] time is over executed without _quem, _quem is invalid(nullptr). Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
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

                m_pang_battle_state = true;
            }

            return true;
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

                // Verifica se ele fez giveup mesmo, porque Pang Battle   que nem Match se um player ganhar antes de o outro terminar o hole, 
                // passa para o pr ximo hole e esse fica sem fazer o hole
                if (!pgi.shot_sync.state_shot.display.acerto_hole && (m_pbd.m_hole <= 0 || m_pbd.v_player_win[m_pbd.m_hole - 1].player_win == -3))
                { // Terminou o Hole sem acerta ele, Give Up

                    var hole = m_course.findHole(pgi.hole);

                    if (hole == null)
                    {
                        throw new exception("[PangBattle::requestFinishHoleData][Error] PLAYER[UID=" + Convert.ToString(pgi.uid) + "] tentou finalizar os dados do hole no jogo, mas o hole[NUMERO=" + Convert.ToString(pgi.hole) + "] nao existe no course. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS,
                            400, 0));
                    }

                    // +1 que   giveup, s  add se n o passou o n mero de tacadas
                    if (pgi.data.tacada_num < hole.getPar().total_shot)
                    {
                        pgi.data.tacada_num++;
                    }

                    // Ainda n o colocara o give up, o outro pacote, coloca nesse(muito dif cil, n o colocar s  se estiver com bug)
                    if (!pgi.data._giveup)
                    {
                        pgi.data.giveup = 1;

                        // Incrementa o Bad Condute
                        pgi.data.bad_condute++;
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

                _smp.message_pool.getInstance().push(new message("[PangBattle::requestTranslateFinishHoleData][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }
        protected override bool checkEndGame(Player _session)
        {

            var pgi = INIT_PLAYER_INFO("checkEndGame",
                "tentou verificar se é o final do jogo",
                _session);

            return ((/*m_course.findHoleSeq(pgi.hole) == m_ri.qntd_hole && */m_pbd.m_count_finish_hole >= m_ri.qntd_hole && (m_pbd.m_hole <= 0 || m_pbd.v_player_win[m_pbd.m_hole - 1].player_win >= 0)) || m_players.Count == 1);
        }

        public override void requestSaveInfo(Player _session, int option)
        {

            var pgi = INIT_PLAYER_INFO("requestSaveInfo",
                "tentou salvar o info dele no jogo",
                _session);

            try
            {

                // Aqui dados do jogo ele passa o holein no lugar do mad_conduta <-> holein, agora quando ele passa o info user   invertido(Normal)
                // Inverte para salvar direito no banco de dados
                var tmp_holein = pgi.ui.hole_in;

#if _DEBUG
			_smp.message_pool.getInstance().push(new message("[PangBattle::requestSaveInfo][Log] Player[UID=" + Convert.ToString(_session.m_pi.uid) + "] UserInfo[" + pgi.ui.toString() + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));
#endif // _DEBUG

                pgi.ui.hole_in = pgi.ui.mad_conduta;
                pgi.ui.mad_conduta = tmp_holein;

                // Total score do player
                int score = 0;

                for (var it = 0; it < (ushort)m_pbd.m_hole && it < m_ri.qntd_hole /*9h ou 18h ele verifica */; ++it)
                {
                    score += pgi.progress.score[it];
                }
                // Fim de total score

                // Player saiu ou algu m saiu do Pang Battle
                pgi.ui.skin_run_hole = pgi.data.pang_battle_run_hole;

                // Pangs que o player ganhou ou perdeu no Pang Battle
                pgi.ui.skin_pang = pgi.data.pang_pang_battle;

                if (option == 0)
                { // Terminou VS

                    // Verifica se o Angel Event est  ativo de tira 1 quit do player que conclu  o jogo
                    if (m_ri.angel_event)
                    {

                        pgi.ui.quitado = -1;

                        _smp.message_pool.getInstance().push(new message("[PangBattle::requestSaveInfo][Log][AngelEvent] Player[UID=" + Convert.ToString(_session.m_pi.uid) + "] vai reduzir o quit em " + Convert.ToString(pgi.ui.quitado * -1) + " unidade(s).", type_msg.CL_FILE_LOG_AND_CONSOLE));
                    }

                    pgi.ui.exp = 0;
                    pgi.ui.combo = 1;
                    pgi.ui.jogado = 1;

                    // Pang Battle o score est  em progress struct
                    pgi.ui.media_score = score; //pgi->data.score;

                    // Os valores que eu n o colocava
                    pgi.ui.jogados_disconnect = 1; // Esse aqui   o contador de jogos que o player come ou   o mesmo do jogado, s  que esse aqui usa para o disconnect

                    var diff = UtilTime.GetLocalDateDiff(m_start_time);

                    pgi.ui.tempo = (int)diff;

                }
                else if (option == 1)
                { // Quitou ou tomou DC

                    // Quitou ou saiu n o ganha pangs
                    pgi.data.pang = 0;
                    pgi.data.bonus_pang = 0;

                    pgi.ui.exp = 0;
                    pgi.ui.combo = (int)(DECREASE_COMBO_VALUE * -1);
                    pgi.ui.jogado = 1;

                    // Verifica se tomou DC ou Quitou, ai soma o membro certo
                    if (!_session.m_connection_timeout)
                    {
                        pgi.ui.quitado = 1;
                    }
                    else
                    {
                        pgi.ui.disconnect = 1;
                    }

                    // Os valores que eu n o colocava
                    pgi.ui.jogados_disconnect = 1; // Esse aqui   o contador de jogos que o player come ou   o mesmo do jogado, s  que esse aqui usa para o disconnect

                    // Pang Battle o score est  em progress struct
                    pgi.ui.media_score = score; //pgi->data.score;

                    var diff = UtilTime.GetLocalDateDiff(m_start_time);

                    pgi.ui.tempo = (int)diff;

                }
                else if (option == 2)
                { // N o terminou o hole 1, alguem saiu ai volta para sala sem contar o combo, s  conta o jogo que come ou

                    pgi.data.pang = 0;
                    pgi.data.bonus_pang = 0;

                    pgi.ui.exp = 0;
                    pgi.ui.jogado = 1;

                    // Os valores que eu n o colocava
                    pgi.ui.jogados_disconnect = 1; // Esse aqui   o contador de jogos que o player come ou   o mesmo do jogado, s  que esse aqui usa para o disconnect

                    var diff = UtilTime.GetLocalDateDiff(m_start_time);


                    pgi.ui.tempo = (int)diff;

                }
                else if (option == 4)
                { // SSC

                    pgi.ui.clear();

                    // Verifica se o Angel Event est  ativo de tira 1 quit do player que conclu  o jogo
                    if (m_ri.angel_event)
                    {

                        pgi.ui.quitado = -1;

                        _smp.message_pool.getInstance().push(new message("[PangBattle::requestSaveInfo][Log][AngelEvent] Player[UID=" + Convert.ToString(_session.m_pi.uid) + "] vai reduzir o quit em " + Convert.ToString(pgi.ui.quitado * -1) + " unidade(s).", type_msg.CL_FILE_LOG_AND_CONSOLE));
                    }

                    pgi.ui.exp = 0;
                    pgi.ui.combo = 1;
                    pgi.ui.jogado = 1;
                    pgi.ui.media_score = 0;

                    // Os valores que eu n o colocava
                    pgi.ui.jogados_disconnect = 1; // Esse aqui   o contador de jogos que o player come ou   o mesmo do jogado, s  que esse aqui usa para o disconnect

                    var diff = UtilTime.GetLocalDateDiff(m_start_time);

                    pgi.ui.tempo = (int)diff;

                }
                else if (option == 5 /*N o conta quit */)
                {

                    // Quitou ou saiu n o ganha pangs
                    pgi.data.pang = 0;
                    pgi.data.bonus_pang = 0;

                    pgi.ui.exp = 0;
                    pgi.ui.jogado = 1;

                    // Pang Battle o score est  em progress struct
                    pgi.ui.media_score = score; //pgi->data.score;

                    // Os valores que eu n o colocava
                    pgi.ui.jogados_disconnect = 1; // Esse aqui   o contador de jogos que o player come ou   o mesmo do jogado, s  que esse aqui usa para o disconnect

                    var diff = UtilTime.GetLocalTimeDiff(m_start_time);

                    pgi.ui.tempo = (int)diff;
                }

                // Achievement Records
                records_player_achievement(_session);

                // Soma com os pangs que ele ganhou no Pang Battle, se o player ganhou o Pang Battle tira 5% dos pangs que ele ganhou
                long total_pang = Convert.ToInt64((long)pgi.data.pang + (long)pgi.data.bonus_pang + (m_pbd.m_player_win_pb == pgi.oid ? (long)(pgi.data.pang_pang_battle * 0.95f) : (long)pgi.data.pang_pang_battle));

                // UPDATE ON SERVER AND DB
                _session.m_pi.addUserInfo(pgi.ui, (ulong)total_pang); // add User Info

                if (total_pang > 0)
                {
                    _session.m_pi.addPang((ulong)total_pang); // add Pang
                }
                else if (total_pang < 0)
                {
                    _session.m_pi.consomePang((ulong)(total_pang * -1)); // Consome Pangs
                }

                // Game Combo
                if (_session.m_pi.ui.combo > 0)
                {
                    pgi.sys_achieve.incrementCounter(0x6C40004Bu /*Game Combo */, _session.m_pi.ui.combo);
                }

            }
            catch (exception e)
            {
                _smp.message_pool.getInstance().push(new message("[PangBattle::requestSaveInfo][Error] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }
        public void finish()
        {

            m_pang_battle_state = false; // Terminou o versus

            m_game_init_state = 2; // Terminou o jogo

            requestCalculeRankPlace();

            calculePlayerWinPangBattle(); // Calcula que ganhou o Pang Battle

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
        public void requestFinishData(Player _session)
        {

            // Finish Artefact Frozen Flame agora   direto no Finish Item Used Game
            requestFinishItemUsedGame(_session);

            requestSaveDrop(_session);

            rain_hole_consecutivos_count(_session); // conta os achievement de chuva em holes consecutivas

            score_consecutivos_count(_session); // conta os achievement de back-to-back(2 ou mais score iguais consecutivos) do player

            rain_count(_session); // Aqui achievement de rain count

            //var pgi = INIT_PLAYER_INFO("requestFinishData", "tentou finalizar dados do jogo", &_session);

            // Resposta Treasure Hunter Item Draw
            sendTreasureHunterItemDrawGUI(_session);

            // Resposta terminou game - Drop Itens
            sendDropItem(_session);

            // Resposta terminou game - Placar
            sendPlacar(_session);
        }
        public override void requestFinishHole(Player _session, int option)
        {

            var pgi = INIT_PLAYER_INFO("requestFinishHole",
                "tentou finalizar o dados do hole do player no jogo",
                _session);

            var hole = m_course.findHole(pgi.hole);

            if (hole == null)
            {
                throw new exception("[PangBattle::finishHole][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou finalizar hole[NUMERO=" + Convert.ToString((ushort)pgi.hole) + "] no jogo, mas o numero do hole is invalid. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANG_BATTLE,
                    20, 0));
            }

            int score_hole = 0;
            int tacada_hole = 0;

            // Finish Hole Dados
            if (option == 0)
            {

                pgi.data.total_tacada_num += pgi.data.tacada_num;

                // Score do hole
                score_hole = (pgi.data.tacada_num - hole.getPar().par);

                // Tacadas do hole
                tacada_hole = pgi.data.tacada_num;

                // Achievement Score
                var tmp_counter_typeid = AchievementSystem.getScoreCounterTypeId(tacada_hole, hole.getPar().par);

                if (tmp_counter_typeid > 0)
                {
                    pgi.sys_achieve.incrementCounter(tmp_counter_typeid);
                }

#if _DEBUG
			_smp.message_pool.getInstance().push(new message("[PangBattle::requestFinishHole][Log] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] terminou o hole[COURSE=" + Convert.ToString(hole.getCourse()) + ", NUMERO=" + Convert.ToString(hole.getNumero()) + ", PAR=" + Convert.ToString(hole.getPar().par) + ", SHOT=" + Convert.ToString(tacada_hole) + ", SCORE=" + Convert.ToString(score_hole) + ", TOTAL_SHOT=" + Convert.ToString(pgi.data.total_tacada_num) + ", TOTAL_SCORE=" + Convert.ToString(pgi.data.score) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));
#endif // _DEBUG


                // Zera dados
                pgi.data.time_out = 0;

                // Giveup Flag
                pgi.data.giveup = 0;

                // Zera as penalidades do hole
                pgi.data.penalidade = 0;

            }
            else if (option == 1)
            { // N o acabou o hole ent o faz os calculos para o jogo todo

                var pair = m_course.findRange(pgi.hole);

                foreach (var it in pair)
                {
                    if (it.Key > m_ri.qntd_hole)
                        break;

                    pgi.data.total_tacada_num += it.Value.getPar().total_shot;

                    pgi.data.score += it.Value.getPar().range_score[1]; // Max Score
                }

                // Zera dados
                pgi.data.time_out = 0;

                pgi.data.tacada_num = 0;

                // Giveup Flag
                pgi.data.giveup = 0;

                // Zera as penalidades do hole do player
                pgi.data.penalidade = 0;
            }

            // Aqui tem que atualiza o PGI direitinho com outros dados
            pgi.progress.hole = (short)m_course.findHoleSeq(pgi.hole);

            // Dados Game Progress do Player
            if (option == 0)
            {

                if (pgi.progress.hole > 0)
                {

                    if (pgi.shot_sync.state_shot.display.acerto_hole)
                    {
                        pgi.progress.finish_hole[pgi.progress.hole - 1] = 1; // Terminou o hole
                    }

                    pgi.progress.par_hole[pgi.progress.hole - 1] = hole.getPar().par;
                    pgi.progress.score[pgi.progress.hole - 1] = score_hole;
                    pgi.progress.tacada[pgi.progress.hole - 1] = tacada_hole;
                }

            }
            else
            {

                var range = m_course.findRange(pgi.hole);

                foreach (var kv in range)
                {
                    if (kv.Key > m_ri.qntd_hole)
                        break;

                    pgi.progress.finish_hole[kv.Key - 1] = 0;
                    pgi.progress.par_hole[kv.Key - 1] = kv.Value.getPar().par;
                    pgi.progress.score[kv.Key - 1] = kv.Value.getPar().range_score[1]; // Max Score
                    pgi.progress.tacada[kv.Key - 1] = kv.Value.getPar().total_shot;
                }
            }
        }
        public override bool checkNextStepGame(Player _session)
        {

            var ret = false;

            try
            {

                var pgi = INIT_PLAYER_INFO("checkNextStepGame",
                    "tentou verificar o proximo passo do jogo",
                    _session);

                var seq = m_course.findHoleSeq(pgi.hole);

                if (seq == 0 || seq == ushort.MaxValue)
                {
                    throw new exception("[PangBattle::checkNextStepGame][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou pegar sequencia do hole[NUMERO=" + Convert.ToString(pgi.hole) + ", SEQ=" + Convert.ToString(seq) + "], mas nao encontrou course. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANG_BATTLE,
                        500, 0));
                }

                if (m_players.Count == 2)
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
                else if (m_players.Count == 1)
                { // Player quitou mesmo sendo o ultimo no jogo

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
                        m_flag_next_step_game = 2;
                    }

                }
                else if (m_player_turn == null)
                {

                    // Player Turn ainda n o foi decidido, termina o jogo
                    m_state_vs.setStateWithLock(STATE_VERSUS.WAIT_END_GAME);

                    ret = true; // Termina o Game

                }
                else
                {
                    m_flag_next_step_game = 3; // Player quitou
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[PangBattle::checkNextStepGame][ErroSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            return ret;
        }
        public override bool checkAllClearHole()
        {

            bool draw = false;
            int best_shot = 100; // Melhor tacada de quem n o fez o hole ou deu giveup
            PlayerGameInfo p = null;

            // Verifica se esse hola j  tem um vencedor ou se empatou, que pode chamar esse fun  o mais de uma vez quando o player sai no primeiro hole
            if (m_pbd.m_hole <= (short)m_pbd.v_player_win.Count
                && m_pbd.v_player_win[m_pbd.m_hole - 1].player_win >= 0
                || (m_pbd.v_player_win[m_pbd.m_hole - 1].player_win == -1 /*Draw */ && !m_pbd.m_hole_extra_flag))
            {
                return true; // Esse hole j  tem um vencedor ou empatou
            }

            // Verifica se est  no hole extra, Approach Game
            if (m_pbd.m_hole == m_pbd.v_player_win.Count && m_pbd.m_hole_extra_flag)
            {

                // Approach Game

                // Initialize player order top shot approach
                var v_player_order = init_player_order_top_shot_approach();

                // N o conseguiu inicializar os players order top shot aprroach, termina o jogo
                if (!v_player_order.Any())
                {

                    _smp.message_pool.getInstance().push(new message("[PangBattle::checkAllClearHole][Error][Warning] nao conseguiu inicializar os players order top shot approach, terminando o jogo. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                    return true;
                }

                try
                {

                    // Quantos player tacaram uma ou mais vezes
                    uint count_player_shot = 0;
                    // Check
                    v_player_order.ForEach(_el =>
                    {
                        try
                        {
                            if (_el.pgi.data.tacada_num > 0)
                            {
                                count_player_shot++;
                            }
                            if (_el.pgi.shot_sync.isMakeHole() && p == null)
                            {
                                draw = true;
                            }
                            else if (!_el.pgi.shot_sync.isMakeHole() && p == null)
                            {
                                p = _el.pgi;
                            }
                        }
                        catch (exception e)
                        {
                            _smp.message_pool.getInstance().push(new message("[PangBattle::checkAllClearHole][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                        }
                    });

                    // Verifica se o hole est  ok
                    if (m_pbd.m_hole <= 0)
                    {
                        _smp.message_pool.getInstance().push(new message("[PangBattle::checkAllClearHole][Error][Warning] nao conseguiu atualizar os dados do pang battle por que o hole is invalid(" + Convert.ToString(m_pbd.m_hole) + "). Bug.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                    }
                    else
                    {

                        // Hole Pang Battle Data
                        var ph = m_pbd.v_player_win[m_pbd.m_hole - 1];

                        // D  os premios para quem ganhou ou se empatou s  passa para o pr ximo hole

                        // Verifica o Draw primeiro, que se ele estiver ativo, empatou
                        if (count_player_shot == v_player_order.Count && draw)
                        { // Draw

                            // Draw
                            ph.player_win = -1; // Draw

#if _DEBUG
						_smp.message_pool.getInstance().push(new message("[PangBattle::checkAllClearHole][Log] Hole[NUMERO=" + Convert.ToString(m_pbd.m_hole) + "][EXTRA] empatou.", type_msg.CL_FILE_LOG_AND_CONSOLE));
#endif // _DEBUG

                            if (m_pbd.m_hole < (short)m_pbd.v_player_win.Count && !m_pbd.m_hole_extra_flag)
                            {

                                // Passa o valor para o pr ximo hole que empatou esse hole
                                m_pbd.v_player_win[m_pbd.m_hole].pang += ph.pang;
                                m_pbd.v_player_win[m_pbd.m_hole].vezes = (byte)((ph.vezes < 8) ? ph.vezes * 2u : 8);
                                m_pbd.v_player_win[m_pbd.m_hole].pang_extra = ph.pang_extra;
                            }

                        }
                        else if (count_player_shot == v_player_order.Count && p != null || (m_players.Count == 1 && (p = getPlayerInfo(m_players[0])) != null))
                        { // Win

                            // Player Win
                            p.data.score++;
                            p.data.pang_pang_battle += ph.pang * ph.vezes * ((m_players.Count > 0) ? m_players.Count - 1 : 0) + ph.pang_extra;

                            // Player Win Holes
                            ph.player_win = p.oid;

#if _DEBUG
						_smp.message_pool.getInstance().push(new message("[PangBattle::checkAllClearHole][Log] Player[UID=" + Convert.ToString(p.uid) + "] Ganhou o hole[NUMERO=" + Convert.ToString(p.hole) + "][EXTRA]", type_msg.CL_FILE_LOG_AND_CONSOLE));
#endif // _DEBUG

                            // Tira os pangs dos outros players
                            foreach (var el in m_players)
                            {

                                if (el != null && el.m_pi.uid != p.uid)
                                {

                                    try
                                    {

                                        var pgi = INIT_PLAYER_INFO("checkAllClearHole",
                                            "tentou atualizar os dados do Pang Battle",
                                            el);

                                        if (pgi.flag != PlayerGameInfo.eFLAG_GAME.QUIT)
                                        {
                                            pgi.data.pang_pang_battle -= ph.pang * ph.vezes;
                                        }

                                    }
                                    catch (exception e)
                                    {

                                        _smp.message_pool.getInstance().push(new message("[PangBattle::checkAllClearHole][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                                    }
                                }
                            }

                        }
                        else if (count_player_shot != v_player_order.Count && p != null)
                        {
                            // N o terminou o jogo(Approach) ainda, falta player tacar
                            p = null;
                            draw = false;
                        }
                    }

#if _WIN32
				
#elif __linux__
				
#endif

                }
                catch (exception e)
                {

#if _WIN32
				
#elif __linux__
				
#endif

                    _smp.message_pool.getInstance().push(new message("[PangBattle::checkAllClearHole][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                }

            }
            else
            {

                // Normal Game

                // Initialize player order top shot
                init_player_order_top_shot();

                try
                {
                    // Check
                    m_player_order_pb.ForEach(_el =>
                    {
                        try
                        {
                            if (_el.shot_sync.isMakeHole() || _el.data._giveup)
                            {
                                if (p != null)
                                {
                                    if (p.data.tacada_num == _el.data.tacada_num && _el.data.tacada_num <= best_shot)
                                    {
                                        draw = true;
                                    }
                                }
                                else if (best_shot == 100u || _el.data.tacada_num <= best_shot)
                                {
                                    p = _el;
                                }
                            }
                            else
                            {
                                if (best_shot > _el.data.tacada_num)
                                {
                                    best_shot = _el.data.tacada_num;
                                }
                                if (p != null)
                                {
                                    if ((_el.data.tacada_num + 1) <= p.data.tacada_num)
                                    {
                                        p = null;
                                    }
                                }
                            }
                        }
                        catch (exception e)
                        {
                            _smp.message_pool.getInstance().push(new message("[PangBattle::checkAllClearHole][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                        }
                    });

                    // Verifica se o hole est  ok
                    if (m_pbd.m_hole <= 0)
                    {
                        _smp.message_pool.getInstance().push(new message("[PangBattle::checkAllClearHole][Error][Warning] nao conseguiu atualizar os dados do pang battle por que o hole is invalid(" + Convert.ToString(m_pbd.m_hole) + "). Bug.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                    }
                    else
                    {

                        // Hole Pang Battle Data
                        var ph = m_pbd.v_player_win[m_pbd.m_hole - 1];

                        // D  os premios para quem ganhou ou se empatou s  passa para o pr ximo hole

                        // Verifica o Draw primeiro, que se ele estiver ativo, empatou
                        if (draw)
                        { // Draw

                            // Draw
                            ph.player_win = -1; // Draw

#if _DEBUG
						_smp.message_pool.getInstance().push(new message("[PangBattle::checkAllClearHole][Log] Hole[NUMERO=" + Convert.ToString(m_pbd.m_hole) + "] empatou.", type_msg.CL_FILE_LOG_AND_CONSOLE));
#endif // _DEBUG

                            if (m_pbd.m_hole < (short)m_pbd.v_player_win.Count)
                            {

                                // Passa o valor para o pr ximo hole que empatou esse hole
                                m_pbd.v_player_win[m_pbd.m_hole].pang += ph.pang;
                                m_pbd.v_player_win[m_pbd.m_hole].vezes = (byte)((ph.vezes < 8) ? ph.vezes * 2u : 8);
                                m_pbd.v_player_win[m_pbd.m_hole].pang_extra = ph.pang_extra;
                            }

                        }
                        else if (p != null || (m_players.Count == 1 && (p = getPlayerInfo(m_players[0])) != null))
                        { // Win

                            // Player Win
                            p.data.score++;
                            p.data.pang_pang_battle += ph.pang * ph.vezes * ((m_players.Count > 0) ? m_players.Count - 1 : 0) + ph.pang_extra;

                            // Player Win Holes
                            ph.player_win = p.oid;

#if _DEBUG
						_smp.message_pool.getInstance().push(new message("[PangBattle::checkAllClearHole][Log] Player[UID=" + Convert.ToString(p.uid) + "] Ganhou o hole[NUMERO=" + Convert.ToString(p.hole) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));
#endif // _DEBUG

                            // Tira os pangs dos outros players
                            foreach (var el in m_players)
                            {

                                if (el != null && el.m_pi.uid != p.uid)
                                {

                                    try
                                    {

                                        var pgi = INIT_PLAYER_INFO("checkAllClearHole",
                                            "tentou atualizar os dados do Pang Battle",
                                            el);

                                        if (pgi.flag != PlayerGameInfo.eFLAG_GAME.QUIT)
                                        {
                                            pgi.data.pang_pang_battle -= ph.pang * ph.vezes;
                                        }

                                    }
                                    catch (exception e)
                                    {

                                        _smp.message_pool.getInstance().push(new message("[PangBattle::checkAllClearHole][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                                    }
                                }
                            }

                        }
                    }

#if _WIN32
				
#elif __linux__
				
#endif

                }
                catch (exception e)
                {

#if _WIN32
				
#elif __linux__
				
#endif

                    _smp.message_pool.getInstance().push(new message("[PangBattle::checkAllClearHole][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
            }

            return (draw || p != null);
        }
        public override void updateFinishHole()
        {

            var p = new PangyaBinaryWriter((ushort)0x65);

            // OID do player que ganhou ou -1 se empatou
            int player_win = (m_pbd.m_hole <= 0 || (short)m_pbd.v_player_win.Count < m_pbd.m_hole ? -1 : m_pbd.v_player_win[m_pbd.m_hole - 1].player_win);

            p.WriteInt32(player_win);

            packet_func.game_broadcast(this,
                p, 1);
        }
        public override void sendPlayerTurn()
        {

            if (m_player_turn == null)
            {
                throw new exception("[PangBattle::sendPlayerTurn][Error] PlayerGameInfo *m_player_turn is invalid(nullptr). Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                    100, 1));
            }

            var hole = m_course.findHole(m_player_turn.hole);

            if (hole == null)
            {
                throw new exception("[PangBattle::sendPlayerTurn][Error] PLAYER[UID=" + Convert.ToString(m_player_turn.uid) + "] tentou encontrar o hole[NUMERO=" + Convert.ToString(m_player_turn.hole) + "] do course no jogo, mas nao foi encontrado. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                    101, 0));
            }

            // Troca o vento
            var wind = m_course.shuffleWind((uint)new Random().Next() * 777u);

            // Toda hora o Pang Battle troca o vento e o angulo
            m_player_turn.degree = wind.degree.getShuffleDegree();

            var wind_flag = initCardWindPlayer(m_player_turn, wind.wind);

            // Resposta do vento do hole
            var p = new PangyaBinaryWriter((ushort)0x5B);

            p.WriteByte(wind.wind + wind_flag);
            p.WriteByte(1); // Flag de card de vento, aqui   a qnd diminui o vento, 1 Vento azul, no Pang Battle n o tem card de vento, mas toda hora ele troca o vento e o angulo
            p.WriteUInt16(m_player_turn.degree);
            p.WriteByte(0); // Flag do vento, 1 Reseta o Vento, 0 soma o vento que nem o comando gm \wind do pangya original, , Tamb m   type para trocar o vento no Pang Battle se mandar o valor 0

            packet_func.game_broadcast(this,
                p, 1);

            // Resposta passa o oid do player que vai come a o Hole
            p.init_plain(0x63);

            if (m_player_turn == null)
            {
                _smp.message_pool.getInstance().push(new message("[PangBattle::sendPlayerTurn][Error] player_turn is invalid(nullptr)", type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.WriteUInt32(0);
            }
            else
            {
                p.WriteInt32(m_player_turn.oid);
            }

            p.WriteUInt16((ushort)calcMsgToPlayerMakeHole(m_player_turn));

            packet_func.game_broadcast(this,
                p, 1);
        }
        public override void changeTurn()
        {

            if (m_player_turn == null)
            {
                throw new exception("[PangBattle::changeTurn][Error] PlayerGameInfo *m_player_turn is invalid(nullptr). Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                    100, 0));
            }

            // Para o tempo do player do turno
            stopTime();

            // Check Player Turn finish last hole
            if (m_player_turn.shot_sync.state_shot.display.acerto_hole || m_player_turn.data._giveup)
            {

                // Verifica se o player terminou jogo, fez o ultimo hole
                if (m_course.findHoleSeq(m_player_turn.hole) == m_ri.qntd_hole)
                {

                    // Resposta para o player que terminou o ultimo hole do Game
                    var p = new PangyaBinaryWriter((ushort)0x199);

                    packet_func.game_broadcast(this,
                        p, 1);

                    // Fez o Ultimo Hole, Calcula Clear Bonus para o player
                    if (m_player_turn.shot_sync.state_shot.display.clear_bonus)
                    {

                        if (!MapSystem.getInstance().isLoad())
                        {
                            MapSystem.getInstance().load();
                        }

                        var map = MapSystem.getInstance().getMap((byte)(m_ri.getMap() & 0x7F));

                        if (map == null)
                        {
                            _smp.message_pool.getInstance().push(new message("[PangBattle::changeTurn][Error][Warning] tentou pegar o Map dados estaticos do course[COURSE=" + Convert.ToString((ushort)(m_ri.getMap() & 0x7F)) + "], mas nao conseguiu encontra na classe do Server.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                        }
                        else
                        {
                            m_player_turn.data.bonus_pang += MapSystem.getInstance().calculeClearVS(map,
                                (uint)m_players.Count,
                                m_ri.qntd_hole);
                        }
                    }
                }
            }

            // Limpa dados que usa para cada tacada
            clearDataEndShot(m_player_turn);

            // Verifica se todos fizeram o hole
            if (checkAllClearHole())
            {

                clear_all_flag_sync();

                finishHole();

                // Utilizo ele antes no finish hole, limpo ele aqui depois
                clearAllClearHole();

                changeHole();

            }
            else if (m_players.Count == 1 /*&& m_course.findHoleSeq(m_player_turn.hole) < 4 */)
            { // Finaliza o game

                clear_all_flag_sync();

                finishHole();

                changeHole();

            }
            else
            { // Troca o Turno

                clear_all_flag_sync();

                // Recalcula Turno
                requestCalculePlayerTurn();

                // Cnvia para todos o vento e oid do player turn, o player que vai tacar nesse momento
                sendPlayerTurn();
            }
        }
        public override void sendPlacar(Player _session)
        {

            var p = new PangyaBinaryWriter((ushort)0x66);

            p.WriteByte((byte)m_players.Count);

            // Ultimo hole
            var last_hole_index = (m_pbd.m_hole < (short)m_pbd.v_player_win.Count ? m_pbd.m_hole - 1 : m_pbd.v_player_win.Count - 1);

            // Quem ganhou o ultimo hole
            if (m_pbd.v_player_win[last_hole_index].player_win != -1)
            {
                p.WriteInt32(m_pbd.v_player_win[last_hole_index].player_win);
            }
            else
            {
                p.WriteInt32(m_pbd.m_player_win_pb);
            }

            // Quem ganhou o Pang Battle
            p.WriteInt32(m_pbd.m_player_win_pb);

            foreach (var el in m_players)
            {

                var pgi = INIT_PLAYER_INFO("sendPlacar",
                    "tentou enviar o placar do jogo",
                    el);

                p.WriteInt32(el.m_oid);
                p.WriteByte((byte)getRankPlace(el) + 1);
                p.WriteByte((char)pgi.data.score);
                p.WriteByte((byte)pgi.data.total_tacada_num);

                p.WriteUInt16((ushort)pgi.data.exp);
                p.WriteUInt64(pgi.data.pang);
                p.WriteUInt64(pgi.data.bonus_pang);

                // Valor que usa no Pang Battle, valor de pang que ganhou ou perdeu
                p.WriteInt64(pgi.data.pang_pang_battle);
            }

            packet_func.session_send(p,
                _session, 1);
        }

        public override void requestCalculeRankPlace()
        {

            if (m_player_order.Any())
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

            m_player_order.Sort(sort_player_rank_place);
        }
        public eMSG_MAKE_HOLE calcMsgToPlayerMakeHole(PlayerGameInfo _pgi)
        {

            if (_pgi == null)
            {

                _smp.message_pool.getInstance().push(new message("[PangBattle::calcMsgToPlayerMakeHole][Error] PlayerGameInfo _pgi is invalid(nullptr). Bug.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                return eMSG_MAKE_HOLE.MMH_PERDEU;
            }

            eMSG_MAKE_HOLE ret = eMSG_MAKE_HOLE.MMH_PERDEU;

            try
            {

#if _WIN32
			
#elif __linux__
			
#endif

                if (m_player_info.Count > 0)
                {

                    uint c_perdeu = 0;
                    uint c_ganhou = 0;
                    uint c_empatou = 0;

                    foreach (var el in m_player_info)
                    {

                        if (el.Value != null)
                        {

                            if ((_pgi.data.tacada_num + 1) < el.Value.data.tacada_num || ((_pgi.data.tacada_num + 1) == el.Value.data.tacada_num && !el.Value.shot_sync.state_shot.display.acerto_hole))
                            {
                                c_ganhou++;
                            }
                            else if ((_pgi.data.tacada_num + 1) == el.Value.data.tacada_num && el.Value.shot_sync.state_shot.display.acerto_hole)
                            {
                                c_empatou++;
                            }
                            else
                            {
                                c_perdeu++;
                            }

                        }
                        else
                        {
                            _smp.message_pool.getInstance().push(new message("[PangBattle::calcMsgToPlayerMakeHole][Warning] PlayerGameInfo[UID=" + Convert.ToString(el.Value.uid) + "] _session is invalid(nullptr). Bug.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                        }
                    }

                    if (c_ganhou == (m_player_info.Count - 1))
                    {
                        ret = eMSG_MAKE_HOLE.MMH_GANHOU;
                    }
                    else if (c_perdeu == m_player_info.Count)
                    {
                        ret = eMSG_MAKE_HOLE.MMH_PERDEU;
                    }
                    else if (c_perdeu == 1 && (m_player_info.Count - c_ganhou - 1) == c_empatou)
                    {
                        ret = eMSG_MAKE_HOLE.MMH_EMPATOU;
                    }
                }

#if _WIN32
			
#elif __linux__
			
#endif

            }
            catch (exception e)
            {

#if _WIN32
			
#elif __linux__
			
#endif

                _smp.message_pool.getInstance().push(new message("[PangBattle::calcMsgToPlayerMakeHole][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            return ret; // Perdeu
        }
        public void init_pang_battle_data()
        {

            uint valor_pang = 10;

            for (int i = 0; i < m_ri.qntd_hole; i++)
            {

                if ((i + 1) == m_ri.qntd_hole)
                {
                    m_pbd.v_player_win.Add(new PangBattleHolePang(valor_pang * 2));
                }
                else
                {

                    if ((i % 3) == 0)
                    {

                        valor_pang += 10;

                        m_pbd.v_player_win.Add(new PangBattleHolePang(valor_pang /**2 */));

                    }
                    else
                    {
                        m_pbd.v_player_win.Add(new PangBattleHolePang(valor_pang /**2 */));
                    }
                }
            }
        }
        public void calculePlayerWinPangBattle()
        {

            m_pbd.m_player_win_pb = -1;

            if (m_players.Count == 0)
            {

                // Primeiro player ganhou, s  tem ele na sala
                m_pbd.m_player_win_pb = m_players[0].m_oid;

                return;
            }

            if (m_player_order.Any())
            {
                requestCalculeRankPlace();
            }

            long pang_win = -1;

            foreach (var el in m_player_order)
            {

                // !@
                // Se o player empatar nos pangs foi um empate
                if (pang_win == -1L || el.data.pang_pang_battle > pang_win)
                {

                    m_pbd.m_player_win_pb = el.oid;

                    pang_win = el.data.pang_pang_battle;
                }
            }
        }
        public void savePangBattleDados(Player _session)
        {

            if (!_session.getState())
            {

                _smp.message_pool.getInstance().push(new message("[PangBattle::savePangBattleDados][Error] Player[UID=" + Convert.ToString(_session.m_pi.uid) + "] is invalid session.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                return;
            }

            try
            {

                var pgi = INIT_PLAYER_INFO("savePangBattleDados",
                    "tentou salvar os dados do Pang Battle",
                    _session);

                if (m_pbd.m_player_win_pb == -1 || m_pbd.m_player_win_pb == _session.m_oid)
                {
                    pgi.ui.skin_win = 1;
                }
                else
                {
                    pgi.ui.skin_lose = 1;
                }

                pgi.ui.skin_all_in_count = 1;

                // Log
                _smp.message_pool.getInstance().push(new message("[PangBattle::savePangBattleDados][Log] Player[UID=" + Convert.ToString(_session.m_pi.uid) + "] " + (m_pbd.m_player_win_pb == _session.m_oid ? "Ganhou" : "Perdeu") + " Pang Battle, com Pang: " + Convert.ToString(pgi.data.pang_pang_battle), type_msg.CL_FILE_LOG_AND_CONSOLE));

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[PangBattle::savePangBattleDados][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }
        public int sort_player_top_shot(PlayerGameInfo p1, PlayerGameInfo p2)
        {
            bool p1HoleOrGiveup = p1.shot_sync.isMakeHole() || p1.data._giveup;
            bool p2HoleOrGiveup = p2.shot_sync.isMakeHole() || p2.data._giveup;
            bool p1GiveupOnly = !p1.shot_sync.isMakeHole() && p1.data._giveup;
            bool p1Playing = !p1HoleOrGiveup;
            bool p2Playing = !p2HoleOrGiveup;

            if (p1HoleOrGiveup && p1.data.tacada_num <= p2.data.tacada_num && p2Playing)
                return -1; // p1 vem antes

            if (p1HoleOrGiveup && p1.data.tacada_num < p2.data.tacada_num && p2HoleOrGiveup)
                return -1;

            if (p1GiveupOnly && (p1.data.tacada_num + 1) <= p2.data.tacada_num && p2HoleOrGiveup)
                return -1;

            if (p1Playing && p1.data.tacada_num < p2.data.tacada_num && p2Playing)
                return -1;

            return p1.data.tacada_num.CompareTo(p2.data.tacada_num);
        }

        public int sort_player_top_shot_approach(PlayerOrderTurnCtx c1, PlayerOrderTurnCtx c2)
        {
            if (c1?.pgi == null || c2?.pgi == null)
                return 0; // iguais se um for nulo

            bool p1InHole = c1.pgi.shot_sync.isMakeHole() || c1.pgi.shot_sync.state == ShotSyncData.SHOT_STATE.INTO_HOLE;
            bool p2InHole = c2.pgi.shot_sync.isMakeHole() || c2.pgi.shot_sync.state == ShotSyncData.SHOT_STATE.INTO_HOLE;

            var diff1 = c1.hole.getPinLocation().diffXZ(c1.pgi.location);
            var diff2 = c1.hole.getPinLocation().diffXZ(c2.pgi.location);

            if (!p1InHole && p2InHole) return -1;
            if (p1InHole && !p2InHole) return 1;

            if (!p1InHole && !p2InHole)
            {
                int cmp = diff1.CompareTo(diff2);
                if (cmp != 0) return cmp;

                cmp = c1.pgi.shot_data.time_shot.CompareTo(c2.pgi.shot_data.time_shot);
                if (cmp != 0) return cmp;

                return c1.pgi.data.tacada_num.CompareTo(c2.pgi.data.tacada_num);
            }

            return 0;
        }

        public int sort_player_rank_place(PlayerGameInfo p1, PlayerGameInfo p2)
        {
            int cmp = p2.data.score.CompareTo(p1.data.score); // ordem desc
            if (cmp != 0) return cmp;

            return p2.data.pang_pang_battle.CompareTo(p1.data.pang_pang_battle); // desc
        }

        public void init_player_order_top_shot()
        {

            if (m_player_order_pb != null && m_player_order_pb.Any())
            {
                m_player_order_pb.Clear();
            }

            foreach (var el in m_players)
            {

                if (el != null)
                {

                    try
                    {

                        var pgi = INIT_PLAYER_INFO("init_player_order_top_shot",
                            " tentou calcular o player que tem a melhor colocacao no jogo",
                            el);

                        if (pgi.flag != PlayerGameInfo.eFLAG_GAME.QUIT)
                        {
                            m_player_order_pb.Add(pgi);
                        }

                    }
                    catch (exception e)
                    {

                        _smp.message_pool.getInstance().push(new message("[PangBattle::init_player_order_top_shot][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                    }
                }
            }
            m_player_order_pb.Sort(
               sort_player_top_shot);
        }
        public List<PlayerOrderTurnCtx> init_player_order_top_shot_approach()
        {

            List<PlayerOrderTurnCtx> v_player_order_turn = new List<PlayerOrderTurnCtx>();

            var hole = m_course.findHoleBySeq((ushort)(m_pbd.m_hole_extra >= 0 ? m_pbd.m_hole_extra : m_pbd.m_hole));

            if (hole == null)
            {
                return new List<PlayerOrderTurnCtx>(v_player_order_turn);
            }

            foreach (var el in m_players)
            {

                if (el != null)
                {

                    try
                    {

                        var pgi = INIT_PLAYER_INFO("init_player_order_top_shot_approach",
                            " tentou calcular o player order top shot approach",
                            el);

                        if (pgi.flag != PlayerGameInfo.eFLAG_GAME.QUIT)
                        {
                            v_player_order_turn.Add(new PlayerOrderTurnCtx(pgi, hole));
                        }

                    }
                    catch (exception e)
                    {

                        _smp.message_pool.getInstance().push(new message("[PangBattle::init_player_order_top_shot_approach][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                    }
                }
            }

            if (v_player_order_turn.Count == 0)
            {
                return new List<PlayerOrderTurnCtx>(v_player_order_turn);
            }

            v_player_order_turn.Sort(sort_player_top_shot_approach);

            return new List<PlayerOrderTurnCtx>(v_player_order_turn);
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
                if (pgi.shot_sync.state_shot.display.acerto_hole || pgi.data._giveup)
                {

                    requestFinishHole(_session, 0);

                    requestUpdateItemUsedGame(_session);
                }

                pgi.finish_game = 1;

                if (PlayersCompleteGameAndClear() || option == 2 /*Termina o jogo */)
                {

                    var p = new PangyaBinaryWriter();

                    // Verifica se   o primeiro hole e se nem todos terminaram o hole
                    if (m_course.findHoleSeq(pgi.hole) == 1
                        && !checkAllClearHole()
                        && (pgi.progress.hole <= 0 || pgi.progress.finish_hole[pgi.progress.hole - 1] == 0 /*N o terminou o hole */))
                    {

                        foreach (var el in m_players)
                        {

                            pgi = INIT_PLAYER_INFO("finish_game",
                               "tentou finalizar o versus",
                               el);

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

                                setGameFlag(pgi, PlayerGameInfo.eFLAG_GAME.END_GAME);
                            }
                        }

                        m_game_init_state = 2; // Acabou o VS

                        return true;

                    }
                    else
                    {

                        if (m_pang_battle_state)
                        { // Deixa o cliente envia o pacote para finalizar o jogo, depois que ele mostrar os placares

                            // Chama o checkAllClearHole para atualizar o ultimo hole, por quem chamou para acabar o jogo foi externo
                            if (option == 2)
                            {
                                var lixo_ret = checkAllClearHole();
                            }

                            finish_pang_battle(1);

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

                                    requestSaveRecordCourse(el,
                                        0 /*Normal Game */,
                                        (m_ri.qntd_hole == 18 && m_course.findHoleSeq(pgi.hole) == 18) ? 1 : 0);

                                    // Salva (inicializa os dados do Pang Battle do player para o UserInfo) do PlayerGamInfo
                                    savePangBattleDados(el);

                                    // Salva info do player
                                    requestSaveInfo(el, 0);

                                    // D  Exp para o Caddie E Mascot Tamb m
                                    if (pgi.data.exp > 0)
                                    { // s  add exp se for maior que 0

                                        // Add Exp para o player
                                        el.addExp(pgi.data.exp, false /*N o precisa do pacote para trocar de level */);

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
                                        packet_func.session_send(packet_func.pacote06B(el.m_pi, 8),
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
    }
}