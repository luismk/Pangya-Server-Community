using System;
using System.Collections.Generic;
using System.Threading;
using Pangya_GameServer.Game.Base;
using Pangya_GameServer.Models;
using Pangya_GameServer.PacketFunc;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Models;
using PangyaAPI.Utilities.Log;
namespace Pangya_GameServer.Game.GameModes
{
    public class ChipInPractice : GrandZodiacBase
    {
        private bool m_chip_in_practice_state;

        public ChipInPractice(List<Player> _players, RoomInfoEx _ri, RateValue _rv, bool _channel_rookie) : base(_players, _ri, _rv, _channel_rookie)
        {
            this.m_chip_in_practice_state = false;

            foreach (var el in m_players)
            {

                var pgi = INIT_PLAYER_INFO("ChipInPractice",
                    "tentou inicializar o counter item do Chip-in Practice",
                    el);

                initAchievement(el);

                pgi.sys_achieve.incrementCounter(0x6C40003Fu);
            }

            m_state = init_game();
        }

        ~ChipInPractice()
        {
            Dispose(false);
        }

        public override void Dispose(bool disposing)
        {
            if (disposedValue) return;

            if (disposing)
            {
                m_chip_in_practice_state = false;

                if (m_game_init_state != 2)
                {
                    finish(2);
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
        public override void changeHole(Player _session)
        {

            try
            {

                nextHole(_session);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[ChipInPractice::changeHole][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }
        
		public override void finishHole(Player _session)
        {

            requestFinishHole(_session, 0);
        }
		
        public void finish_chip_in_practice(Player _session, int _option)
        {

            if (m_players.Count > 0 && m_game_init_state == 1)
            {

                var p = new PangyaBinaryWriter();

                var pgi = INIT_PLAYER_INFO("finish_chip_in_practice",
                     "tentou terminar o Chip-in Practice no jogo",
                     _session);

                if (pgi.flag == PlayerGameInfo.eFLAG_GAME.PLAYING)
                {

                    // Calcula os pangs que o player ganhou
                    //requestCalculePang(_session);//removido by LUIS

                    // Atualizar os pang do player se ele estiver com assist ligado, e for maior que beginner E
                    updatePlayerAssist(_session);

                    if (m_game_init_state == 1 && _option == 0)
                    {

                        // Achievement Counter, Chip-in Practice n o precisa do counter de game complete
                        //pgi->sys_achieve.incrementCounter(0x6C400004u/*Normal game complete*/);

                    } //else if (m_game_init_state == 1 && _option == 1)    // Acabou o Tempo
                }

                setGameFlag(pgi, (_option == 0) ? PlayerGameInfo.eFLAG_GAME.FINISH : PlayerGameInfo.eFLAG_GAME.END_GAME);

                pgi.time_finish.CreateTime();

                // Terminou o jogo no Grand Zodiac
                setEndGame(pgi);

                setFinishGameFlag(pgi, 1);

                // End Game
                p.init_plain(0x1F2);

                packet_func.session_send(p,
                    _session, 1);

                if (AllCompleteGameAndClear() && m_game_init_state == 1)
                {
                    finish(_option); // Envia os pacotes que termina o jogo Ex: 0xCE, 0x79 e etc
                }
            }
        }
		
        public override void timeIsOver()
        {

            if (m_game_init_state == 1 && m_players.Count > 0)
            {

                Player _session = null;

                foreach (var el in m_player_info)
                {

                    // S  os que n o acabaram
                    if (el.Value.flag == PlayerGameInfo.eFLAG_GAME.PLAYING && (_session = findSessionByUID(el.Value.uid)) != null)
                    {

                        // Send full time que o pr prio cliente termina
                        var p = new PangyaBinaryWriter((ushort)0x8D);

                        p.WriteUInt32(m_ri.time_30s);

                        packet_func.session_send(p,
                            _session, 1);
                    }
                } 
            }
        }

        public override bool init_game()
        {

            if (m_players.Count > 0)
            {

                // variavel que salva a data local do sistema
                initGameTime();

                m_game_init_state = 1; // Come ou

                m_chip_in_practice_state = true;
            }

            return true;
        }

        public void requestFinishExpGame()
        {

            int exp = 0;
            Player _session = null;

            foreach (var el in m_player_info)
            {

                if (el.Value != null)
                {

                    if (el.Value.flag == PlayerGameInfo.eFLAG_GAME.FINISH)
                    {

                        if ((_session = findSessionByUID(el.Value.uid)) != null)
                        {

                            exp = 15;
                            exp = (int)(exp * TRANSF_SERVER_RATE_VALUE(el.Value.used_item.rate.exp) * TRANSF_SERVER_RATE_VALUE(m_rv.exp));

                            if (el.Value.level < 70 /*/ *Ultimo level n o ganha exp * /*/)
                            {
                                el.Value.data.exp = 0;
                            }
                        }

                    }
                    else if (el.Value.flag == PlayerGameInfo.eFLAG_GAME.END_GAME)
                    {

                        exp = (int)(((PlayerGrandZodiacInfo)(el.Value)).m_gz.hole_in_one / 2);
                        exp = (int)(exp * TRANSF_SERVER_RATE_VALUE(el.Value.used_item.rate.exp) * TRANSF_SERVER_RATE_VALUE(m_rv.exp));

                        if (el.Value.level < 70)
                        {
                                el.Value.data.exp = 0;
                        }
                    }
                }
            }
        }

        public void finish(int option)
        {

            m_game_init_state = 2; // Acabou

            requestCalculeRankPlace();

            requestMakeTrofel();

            requestFinishExpGame();

            foreach (var el in m_players)
            {

                var pgi = INIT_PLAYER_INFO("finish",
                     "tentou finalizar os dados do jogador no jogo",
                     el);

                if (pgi.flag != PlayerGameInfo.eFLAG_GAME.QUIT)
                {
                    requestFinishData(el, option);
                }
            }
        }
        public override void drawDropItem(Player _session)
        {
            return; // N o tem drop de item no Chip-in Practice
        }
        public void requestFinishData(Player _session, int option)
        {

            var p = new PangyaBinaryWriter();

            try
            {

                var pgi = INIT_PLAYER_INFO("requestFinishData",
                     "tentou finalizar os dado do player no jogo",
                     _session);

                requestSaveInfo(_session, 0/* / *Terminou * /*/);

                // Atualiza itens usados no Grand Zodiac
                requestUpdateItemUsedGame(_session);

                // Finish Artefact Frozen Flame agora   direto no Finish Item Used Game
                requestFinishItemUsedGame(_session);

                requestSaveDrop(_session);

                sendTimeIsOver(_session);

                // Resposta terminou game - Placar
                sendPlacar(_session);

                // Resposta Update Pang
                p.init_plain(0xC8);

                p.WriteUInt64(_session.m_pi.ui.pang);

                p.WriteUInt64(0Ul);

                packet_func.session_send(p,
                    _session, 1);

                // A7
                p.init_plain(0xA7);

                p.WriteByte(0); // Count

                packet_func.session_send(p,
                    _session, 1);

                // AA
                p.init_plain(0xAA);

                p.WriteUInt16(0); // Count

                p.WriteUInt64(_session.m_pi.ui.pang);
                p.WriteUInt64(_session.m_pi.cookie);

                packet_func.session_send(p,
                    _session, 1);

                // Update Mascot Info ON GAME, se o player estiver com um mascot equipado
                if (_session.m_pi.ei.mascot_info != null)
                {
                    packet_func.session_send(packet_func.pacote06B(_session.m_pi, 8),
                        _session, 1);
                }

                // Achievement Aqui
                pgi.sys_achieve.finish_and_update(_session);

                // Esse   novo do JP, tem Tourney, VS, Grand Prix, HIO Event, n o vi talvez tenha nos outros tamb m
                p.init_plain(0x24F);

                p.WriteUInt32(0); // OK

                packet_func.session_send(p,
                    _session, 1);

                // Exp
                if (pgi.data.exp > 0) // s  add exp se for maior que 0
                {
                    _session.addExp(pgi.data.exp, true); // Update in Game
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[ChipInPractice::requestFinishData][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }
        public override void updateFinishHole(Player _session, int _option)
        {

            try
            {

                var pgi = INIT_PLAYER_INFO("updateFinishHole",
                      "tentou atualizar o finish hole do grand zodiac",
                      _session);

                // Passa a localiza  o do player, esse   a primeira, vez ent o passa os valores do init Hole sempre
                var p = new PangyaBinaryWriter((ushort)0x1EE);

                p.WriteInt32(_session.m_oid);
                p.WriteFloat(pgi.location.x);
                p.WriteFloat(pgi.location.z);

                packet_func.game_broadcast(this,
                    p, 1);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[ChipInPractice::updateFinishHole][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }
        public void requestMakeTrofel()
        {
            return; // N o tem isso no Chip-in Practice
        }
        public override void startGoldenBeam()
        {
            return; // N o tem isso no Chip-in Practice
        }
        public override void endGoldenBeam()
        {
            return; // N o tem isso no Chip-in Practice
        }
        public override bool finish_game(Player _session, int option)
        {

            if (m_players.Count > 0)
            {

                var p = new PangyaBinaryWriter();

                if (option == 0x12C || option == 2)
                {

                    bool is_hacker_or_bug = false;

                    if (m_timer != null)
                    {

                        is_hacker_or_bug = ((int)(m_ri.time_30s - m_timer.getElapsed()) / (60 * 1000 /*/ *Minuto * /*/)) >= 1 ? true : false;

                        if (is_hacker_or_bug && option == 0x12C)
                        {
                            _smp.message_pool.getInstance().push(new message("[ChipInPractice::finish_game][Warning] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] na sala[NUMERO=" + Convert.ToString(m_ri.numero) + "] TEMPO[FINISH=" + Convert.ToString(m_timer.getElapsed()) + ", FINISH_CORRETO=" + Convert.ToString(m_ri.time_30s) + "] pediu para terminar o Chip-in Practice com tempo menor que o do sala, pelo pacote normal, ele que ganhar exp, com menos tempo. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                        }
                    }

                    if (m_chip_in_practice_state)
                    {
                        finish_chip_in_practice(_session, (option == 0x12C && !is_hacker_or_bug) ? 0 : 1); // Termina sem ter acabado de jogar
                    }
                }
            }

            return (PlayersCompleteGameAndClear() && m_chip_in_practice_state);
        }
    }
}