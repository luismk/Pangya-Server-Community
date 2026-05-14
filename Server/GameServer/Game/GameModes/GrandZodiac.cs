using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Pangya_GameServer.Game.Base;
using Pangya_GameServer.Game.Manager;
using Pangya_GameServer.Models;
using Pangya_GameServer.PacketFunc;

using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Models;
using PangyaAPI.Utilities.Log;
namespace Pangya_GameServer.Game.GameModes
{
    public class GrandZodiac : GrandZodiacBase
    {
        private bool m_grand_zodiac_state;

        public GrandZodiac(List<Player> _players, RoomInfoEx _ri, RateValue _rv, bool _channel_rookie) : base(_players, _ri, _rv, _channel_rookie)
        {
            foreach (var el in m_players)
            {

                var pgi = INIT_PLAYER_INFO("GrandZodiac",
                    "tentou inicializar o counter item do Grand Zodiac",
                    el);

                initAchievement(el);

                pgi.sys_achieve.incrementCounter(0x6C40003Cu /*/ *Grand Zodiac * /*/);
            }

            m_state = init_game();
        }

        ~GrandZodiac()
        {
            Dispose(false);
        }

        public override void Dispose(bool disposing)
        {
            if (disposedValue) return;

            if (disposing)
            {
                m_grand_zodiac_state = false;

                if (m_game_init_state != 2)
                {
                    finish(2);
                }

                while (!PlayersCompleteGameAndClear())
                {
                    Thread.Sleep(500);

                }

                stopTime();

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

                _smp.message_pool.getInstance().push(new message("[GrandZodiac::changeHole][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void finishHole(Player _session)
        {

            requestFinishHole(_session, 0);
        }

        public void finish_grand_zodiac(Player _session, int _option)
        {

            if (m_players.Count > 0 && m_game_init_state == 1)
            {

                var p = new PangyaBinaryWriter();

                var pgi = INIT_PLAYER_INFO("finish_grand_zodiac",
                    "tentou terminar o Grand Zodiac no jogo",
                    _session);

                if (pgi.flag == PlayerGameInfo.eFLAG_GAME.PLAYING)
                {

                    // Calcula os pangs que o player ganhou
                    requestCalculePang(_session);

                    // Atualizar os pang do player se ele estiver com assist ligado, e for maior que beginner E
                    updatePlayerAssist(_session);

                    if (m_game_init_state == 1 && _option == 0)
                    {

                        // Achievement Counter
                        pgi.sys_achieve.incrementCounter(0x6C400004u);

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

                m_grand_zodiac_state = true;
            }

            return true;
        }
        public void requestFinishExpGame()
        {

            int exp = 0;
            Player _session = null;

            PlayerGrandZodiacInfo pgzi = null;

            foreach (var el in m_player_info)
            {
                if ((pgzi = (PlayerGrandZodiacInfo)(el.Value)) != null)
                {

                    if (el.Value.flag == PlayerGameInfo.eFLAG_GAME.FINISH)
                    {

                        if ((_session = findSessionByUID(el.Value.uid)) != null)
                        {

                            exp = (int)(45.0f * ((121 - pgzi.m_gz.position) / 100.0f));
                            exp = (int)(exp * TRANSF_SERVER_RATE_VALUE(el.Value.used_item.rate.exp) * TRANSF_SERVER_RATE_VALUE(m_rv.exp));

                            if (el.Value.level < 70)
                            {
                                el.Value.data.exp = exp;
                            }
                        }

                    }
                    else if (el.Value.flag == PlayerGameInfo.eFLAG_GAME.END_GAME)
                    {

                        exp = (int)(pgzi.m_gz.hole_in_one / 2);
                        exp = (int)(exp * TRANSF_SERVER_RATE_VALUE(el.Value.used_item.rate.exp) * TRANSF_SERVER_RATE_VALUE(m_rv.exp));

                        if (el.Value.level < 70)
                        {
                            el.Value.data.exp = exp;
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

            requestCalculePontos();

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

            try
            {

                var pgi = INIT_PLAYER_INFO("drawDropItem",
                    "tentou sortear item drop para o jogador no jogo",
                    _session);

                var rnd = new Random();
                if (pgi.shot_sync.state_shot.display.acerto_hole)
                {

                    var seed = rnd.Next(1, 10000);

                    if (seed > 9000)
                    { // Dropou

                        // 0x1800002C - Silent Nerver Stabilizer
                        // 0x1800002D - Safe Silent
                        DropItem di = new DropItem();

                        di.numero_hole = 1;
                        di.course = (byte)(m_ri.getMap() & 0x7F);

                        seed = rnd.Next(0, 1);

                        di._typeid = 0x1800002C + (uint)seed;
                        di.qntd = (short)((seed == 0) ? 5 : 3);
                        di.type = DropItem.eTYPE.NORMAL_QNTD;

                        // Add Droped item to pgi player
                        pgi.drop_list.v_drop.Add(di);

                        // Update item game, show msg
                        var p = new PangyaBinaryWriter((ushort)0x40);

                        p.WriteByte(15); // Dropou item

                        p.WriteString(_session.m_pi.nickname);
                        p.WriteUInt16(0); // Message empty

                        p.WriteUInt32(di._typeid);
                        p.WriteUInt32((uint)di.qntd);

                        // Envia para todos
                        packet_func.game_broadcast(this,
                            p, 1);

                    }
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[GrandZodiac::drawDropItem][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }
        public void requestFinishData(Player _session, int option)
        {

            var p = new PangyaBinaryWriter();

            try
            {

                var pgi = INIT_PLAYER_INFO("requestFinishData",
                    "tentou finalizar os dado do player no jogo",
                    _session);

                requestSaveInfo(_session, 0);

                // Atualiza itens usados no Grand Zodiac
                requestUpdateItemUsedGame(_session);

                // Finish Artefact Frozen Flame agora   direto no Finish Item Used Game
                requestFinishItemUsedGame(_session);

                // Salva pontos do Grand Zodiac ganho
                if (pgi.m_gz.pontos > 0)
                {
                    _session.m_pi.addGrandZodiacPontos(pgi.m_gz.pontos);
                }

                // Salve trofe 
                sendTrofel(_session);

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

                _smp.message_pool.getInstance().push(new message("[GrandZodiac::requestFinishData][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
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

                if (m_ri.getTipo() == RoomInfo.ROOM_INFO_TYPE.GRAND_ZODIAC_INT)
                {
                    packet_func.game_broadcast(this,
                        p, 1);
                }
                else
                {
                    packet_func.session_send(p,
                        _session, 1);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[GrandZodiac::updateFinishHole][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }
        public void requestMakeTrofel()
        {

            // Trofe  Grand Zodiac
            uint trofeu_base = 0x2D0A6200;
            uint qntd = 0;

            var players_num = m_player_info.Count;

            if (players_num >= 20u && players_num < 30u) // Bronza
                qntd = 1u;
            else if (players_num >= 30 && players_num < 50) // Silver and Bronza
                qntd = 2u;
            else if (players_num >= 50) // Gold, Silver and Bronze
                qntd = 3u;


            trofeu_base = trofeu_base + ((3 - qntd) << 8);

            PlayerGrandZodiacInfo pgzi = null;

            foreach (var el in m_player_info)
            {


                if ((pgzi = (PlayerGrandZodiacInfo)(el.Value)) != null && pgzi.flag != PlayerGameInfo.eFLAG_GAME.QUIT)
                {

                    if (pgzi.m_gz.position <= qntd)
                    {
                        pgzi.m_gz.trofeu = trofeu_base + ((pgzi.m_gz.position - 1) << 8);
                    }
                }
            }
        }
        public override void startGoldenBeam()
        {
            Interlocked.Exchange(ref m_golden_beam_state, 1);
            // Golden Beam Start
            var p = new PangyaBinaryWriter((ushort)0x1F0);

            packet_func.game_broadcast(this,
                p, 1);
        }
        public override void endGoldenBeam()
        {

            try
            {

                // Acabou o tempo do golden beam time
                Interlocked.Exchange(ref m_golden_beam_state, 0);

                // Golden Beam End
                var p = new PangyaBinaryWriter((ushort)0x1F1);

                packet_func.game_broadcast(this,
                    p, 1);
                var rnd = new Random();
                if (!m_mp_golden_beam_player.empty())
                {

                    ulong jackpot = (ulong)(m_players.Count * (rnd.Next(1, 5) * 500)); // rand de 500 a 2500 por player

                    var seed = rnd.Next(0, 1);

                    if (m_mp_golden_beam_player.Count == 1 || seed == 0)
                    {

                        int index = rnd.Next(0, m_mp_golden_beam_player.Count); // até Count - 1 incluso

                        var it = m_mp_golden_beam_player.ElementAt(index);

                        var pgi = INIT_PLAYER_INFO("endGoldenBeam",
                            "tentou enviar o prensete do Golden Beam",
                            it._session);

                        if (pgi.flag != PlayerGameInfo.eFLAG_GAME.QUIT)
                        {

                            // Log
                            _smp.message_pool.getInstance().push(new message("[GrandZodiac::endGoldenBeam][Log] PLAYER[UID=" + Convert.ToString(it._session.m_pi.uid) + "] ganhou jackpot(" + Convert.ToString(jackpot) + ") sozinho.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            pgi.m_gz.jackpot = jackpot;

                            p.init_plain(0x40);

                            p.WriteByte(13); // 1 Ganhou sozinho o jackpot no Grand Zodiac

                            p.WriteString(it._session.m_pi.nickname);
                            p.WriteUInt16(0); // Msg empty

                            p.WriteUInt32(0x1A000010); // Jackpot Pangs Pouch
                            p.WriteUInt64(jackpot);

                            packet_func.game_broadcast(this,
                                p, 1);

                        }
                        else
                        {
                            _smp.message_pool.getInstance().push(new message("[GrandZodiac::endGoldenBeam][Log][Warning] PLAYER[UID=" + Convert.ToString(pgi.uid) + "] ganhou jackpot, mas ele nao esta mais no jogo, para receber o jackpot.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                        }

                    }
                    else if (seed == 1)
                    {

                        var equal_jackpot = Convert.ToUInt32(jackpot) / m_mp_golden_beam_player.Count;

                        foreach (var el in m_mp_golden_beam_player)
                        {

                            var pgi = INIT_PLAYER_INFO("endGoldenBeam",
                                "tentou enviar o presente do Golden Beam",
                                el._session);

                            if (pgi.flag != PlayerGameInfo.eFLAG_GAME.QUIT)
                            {

                                // Log
                                _smp.message_pool.getInstance().push(new message("[GrandZodiac::endGoldenBeam][Log] PLAYER[UID=" + Convert.ToString(el._session.m_pi.uid) + "] ganhou jackpot(" + Convert.ToString(jackpot) + ") igual ao de todo mundo.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                                pgi.m_gz.jackpot = (ulong)equal_jackpot;

                                p.init_plain(0x40);

                                p.WriteByte(14); // Todos que fizeram hio no golden beam garanham o jackpot

                                p.WriteString(el._session.m_pi.nickname);
                                p.WriteUInt16(0); // Msg Empty

                                p.WriteUInt32(0x1A000010); // Jackpot Pangs Pouch
                                p.WriteInt64(equal_jackpot);

                                packet_func.session_send(p,
                                    el._session, 1);

                            }
                            else
                            {
                                _smp.message_pool.getInstance().push(new message("[GrandZodiac::endGoldenBeam][Log][Warning] PLAYER[UID=" + Convert.ToString(pgi.uid) + "] ganhou jackpot, mas ele nao esta mais no jogo, para receber o jackpot.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                            }
                        }
                    }
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[GrandZodiac::endGoldenBeam][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }
        public void requestCalculePontos()
        {

            PlayerGrandZodiacInfo pgzi = null;

            float pontos_base = (m_ri.getTipo() == RoomInfo.ROOM_INFO_TYPE.GRAND_ZODIAC_INT ? 3.5f : 5.0f);

            foreach (var el in m_player_info)
            {


                if ((pgzi = (PlayerGrandZodiacInfo)(el.Value)) != null && pgzi.flag != PlayerGameInfo.eFLAG_GAME.QUIT)
                {

                    pgzi.m_gz.pontos = (uint)(pgzi.m_gz.total_score * pontos_base);
                }
            }
        }
        public void sendTrofel(Player _session)
        {

            var p = new PangyaBinaryWriter();

            try
            {

                var pgi = INIT_PLAYER_INFO("sendTrofel",
                    "tentou enviar o trofeu do player no jogo",
                    _session);

                if (pgi.m_gz.trofeu > 0)
                {

                    stItem item = new stItem();

                    // Inicializa o Trof u
                    item.type = 2;
                    item.id = -1;
                    item._typeid = pgi.m_gz.trofeu;
                    item.qntd = 1;
                    item.STDA_C_ITEM_QNTD = (short)item.qntd;

                    // Update on Server and Database
                    if (ItemManager.addItem(item,
                        _session, 0, 0) >= ItemManager.RetAddItem.T_SUCCESS)
                    {

                        // Adicionou o Trof u com sucesso para o player
                        _smp.message_pool.getInstance().push(new message("[GrandZodiac::sendTrofel][Log] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] ganhou Grand Zodiac Trofeu[TYPEID=" + Convert.ToString(pgi.m_gz.trofeu) + "] na Posicao[RANK=" + Convert.ToString(pgi.m_gz.position) + "].", type_msg.CL_FILE_LOG_AND_CONSOLE));

                        // Update Trof u on Game
                        p.init_plain(0x1FA);

                        p.WriteUInt32(item._typeid);

                        p.WriteInt32(item.id);

                        packet_func.session_send(p,
                            _session, 1);

                    }
                    else
                    {
                        _smp.message_pool.getInstance().push(new message("[GrandZodiac::sendTrofel][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou adicionar Grand Zodiac Trofeu[TYPEID=" + Convert.ToString(item._typeid) + "] na Posicao[RANK=" + Convert.ToString(pgi.m_gz.position) + "], mas nao conseguiu adicionar o item.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                    }
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[GrandZodiac::sendTrofel][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
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
                        is_hacker_or_bug = ((int)(m_ri.time_30s - m_timer.getElapsed()) / (60 * 1000)) >= 1 ? true : false;
                    }

                    if (m_grand_zodiac_state)
                    {
                        finish_grand_zodiac(_session, (option == 0x12C && !is_hacker_or_bug) ? 0 : 1); // Termina sem ter acabado de jogar
                    }
                }
            }

            return (PlayersCompleteGameAndClear() && m_grand_zodiac_state);
        }
    }
}
