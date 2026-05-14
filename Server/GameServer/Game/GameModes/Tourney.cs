using Pangya_GameServer.Game.Base;
using Pangya_GameServer.Game.Manager;
using Pangya_GameServer.Game.System;
using Pangya_GameServer.Models;
using Pangya_GameServer.PacketFunc;
using Pangya_GameServer.PangyaEnums;
using Pangya_GameServer.Repository;
using Pangya_GameServer.UTIL;
using PangyaAPI.IFF.JP.Extensions;
using PangyaAPI.Network.PangyaPacket;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Models;
using PangyaAPI.Utilities.Log;
using snmdb;
using System;
using System.Collections.Generic;
using System.Linq;
using static Pangya_GameServer.Models.DefineConstants;
namespace Pangya_GameServer.Game.GameModes
{
    public class Tourney : TourneyBase, IDisposable
    {
        PangyaSyncTimer m_pTimer_after_enter;        // Timer de entrar depois no Tourney

        bool m_tourney_state;
        public Tourney(List<Player> _players, RoomInfoEx _ri, RateValue _rv, bool _channel_rookie) : base(_players, _ri, _rv, _channel_rookie)
        {

            this.m_tourney_state = false;
            this.m_pTimer_after_enter = null;

            // Atualiza Treasure Hunter System Course

            if (!sTreasureHunterSystem.getInstance().isLoad())
            {
                sTreasureHunterSystem.getInstance().load();
            }


            var course = sTreasureHunterSystem.getInstance().findCourse((byte)(m_ri.course & RoomInfo.ROOM_INFO_COURSE.UNK));

            if (course == null)
            {
                _smp.message_pool.getInstance().push(new message("[Tourney::Tourney][Error] tentou pegar o course do Treasure Hunter System, mas o course[COURSE=" + Convert.ToString((ushort)((byte)(m_ri.course & RoomInfo.ROOM_INFO_COURSE.UNK))) + "] nao existe no sistema", type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
            else
            {
                //TreasureHunterSystem::updateCoursePoint(*course, -1);	// -1 ponto a cada jogo iniciado
                sTreasureHunterSystem.getInstance().updateCoursePoint(course, -1); // -1 ponto a cada jogo iniciado
            }

            // Aqui tem que inicializar os players info
            initAllPlayerInfo();

            foreach (var el in m_players)
            {

                INIT_PLAYER_INFO("Tourney",
                    "tentou inicializar o counter item do Tourney",
                    el, out PlayerGameInfo pgi);

                initAchievement(el);

                pgi.sys_achieve.incrementCounter(0x6C40001Fu);
            }

            m_state = init_game();
        }

        public override void Dispose(bool disposing)
        {
            if (disposing)
            {
                LogDestruction();
                deleteAllPlayer();
            }
            base.Dispose(true);
        }

        ~Tourney()
        {
            Dispose(false);
        }

        public override bool deletePlayer(Player _session, int _option)
        {

            if (_session == null)
            {
                throw new exception("[Tourney::deletePlayer][Error] tentou deletar um player, mas o seu endereco é null.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY,
                    50, 0));
            }

            bool ret = false;

            try
            { 
                var it = m_players.FirstOrDefault(c => c == _session);

                if (it != null)
                {
                    byte opt = 3; // Saiu Quitou

                    if (m_game_init_state == 1)
                    {

                        var p = new PangyaBinaryWriter();

                        INIT_PLAYER_INFO("deletePlayer",
                            "tentou sair do jogo",
                            _session, out PlayerGameInfo pgi);

                        var sessions = getSessions(it);

                        if (pgi.flag != PlayerGameInfo.eFLAG_GAME.TICKET_REPORT)
                        {

                            requestFinishItemUsedGame(it); // Salva itens usados no Tourney

                            requestSaveInfo(it, (_option == 0x800) ? 5 : 1); // Quitou ou tomou DC

                            //pgi->type = PlayerGameInfo::eFLAG_GAME::QUIT;
                            setGameFlag(pgi, PlayerGameInfo.eFLAG_GAME.QUIT);

                            // Resposta Player saiu do Jogo, tira ele do list de score
                            p.init_plain(0x61);

                            p.WriteInt32(it.m_oid);

                            packet_func.vector_send(p,
                                sessions, 1);

                            // Resposta Player saiu do jogo
                            sendUpdateState(_session, opt);

                            // Salva Achievement do player

                            if (AllCompleteGameAndClear())
                            {
                                ret = true; // Termina o Tourney
                            }

                        }
                        else if (_option == 10)
                        { // Ticket Reporting
                            opt = 1; // Ticket Reporting

                            // Resposta Player saiu do Jogo, tira ele do list de score
                            p.init_plain(0x61);

                            p.WriteInt32(it.m_oid);

                            packet_func.vector_send(p,
                                sessions, 1);

                            // Resposta Player saiu com ticket reporting do jogo
                            p.init_plain(0x11B);

                            p.WriteInt32(it.m_oid);

                            packet_func.vector_send(p,
                                sessions, 1);
                        }

                        if (opt != 1) // !Ticket Report
                        {
                            sendUpdateInfoAndMapStatistics(_session, -1);
                        }
                    }

                    // Delete Player
                    m_players.Remove(it);
                }
                else
                {
                    _smp.message_pool.getInstance().push(new message("[Tourney::deletePlayer][Warning] player ja foi excluido do game.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                } 

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Tourney::deletePlayer][Error] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                 
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
        public override bool requestFinishLoadHole(Player _session, packet _packet)
        {
            ////REQUEST_BEGIN("FinishLoadHole");

            var p = new PangyaBinaryWriter();

            // Esse aqui   para Trocar Info da Sala
            // para colocar a sala no modo que pode entrar depois de ter come ado
            bool ret = false;

            try
            {

                // Chama a fun  o base para fazer a parte dela
                ret = base.requestFinishLoadHole(_session, _packet);

                // Aqui come a o tempo que os outros player pode entrar se a sala n o for private
                // Come  o o tempo de 5 ou 10min para entra no camp se n o tiver senha
                if (m_entra_depois_flag != 1
                    && m_ri.senha_flag == 1
                    && ((byte)(m_ri.course & RoomInfo.ROOM_INFO_COURSE.UNK)) != (byte)RoomInfo.ROOM_INFO_TYPE.SPECIAL_SHUFFLE_COURSE)
                {

                    // S  libera se for Tourney Normal, se for GM Event N o libera
                    if (!(m_ri.trofel == TROFEL_GM_EVENT_TYPEID && m_ri.max_player > 30 && m_ri.flag_gm == 1 && m_ri.state_flag == 0x100))
                    {
                        // Libera Entrar, mesmo depois de ter come ado o Tourney
                        ret = true;
                    }

                    m_entra_depois_flag = 1;
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Tourney::requestFinishLoadHole][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            return ret;
        }
        public override void changeHole(Player _session)
        {

            updateTreasureHunterPoint(_session);

            if (checkEndGame(_session))
            {
                finish_tourney(_session, 0);
            }
            else
            {
                // Resposta terminou o hole
                updateFinishHole(_session, 1); // Terminou
            }
        }

        public override void finishHole(Player _session)
        {
            INIT_PLAYER_INFO("finishHole",
                    "tentou finalizar o hole do jogo",
                    _session, out PlayerGameInfo pgi);
            if (pgi.shot_sync.state_shot.display.acerto_hole || pgi.data.giveup == 1)
            {
                requestFinishHole(_session, 0);

                requestUpdateItemUsedGame(_session);
            }
        }
        
public void finish_tourney(Player _session, int _option)
        {

            if (m_players.Count() > 0 && m_game_init_state == 1)
            {

                INIT_PLAYER_INFO("finish_tourney",
                    "tentou terminar o tourney no jogo",
                    _session, out PlayerGameInfo pgi);

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

                        requestFinishHole(_session, 1); // Acabou o Tempo 

                        // Mostra msg que o player terminou o jogo
                        sendFinishMessage(_session);

                        // Resposta terminou o hole
                        updateFinishHole(_session, 0);

                        // Resposta para acabou o tempo do Tourney
                        sendTimeIsOver(_session);
                    }
                }

                //pgi->type = (_option == 0) ? PlayerGameInfo::eFLAG_GAME::FINISH : PlayerGameInfo::eFLAG_GAME::END_GAME;
                setGameFlag(pgi, (_option == 0) ? PlayerGameInfo.eFLAG_GAME.FINISH : PlayerGameInfo.eFLAG_GAME.END_GAME);

                pgi.time_finish.CreateTime();

                if (AllCompleteGameAndClear() && m_game_init_state == 1)
                {
                    finish(); // Envia os pacotes que termina o jogo Ex: 0xCE, 0x79 e etc
                }
            }
        }
        public override bool requestUseTicketReport(Player _session, packet p)
        {
            ////REQUEST_BEGIN("UseTicketReport");

            bool ret = false;

            try
            {
                #region Read Packet
                UserInfoEx ui = new UserInfoEx().ToRead(p);
                #endregion
                // aqui o cliente passa mad_conduta com o hole_in, trocados, mad_conduto <-> hole_in

                INIT_PLAYER_INFO("requestUseTicketReport",
                    "tentou sair do jogo com ticket report",
                    _session, out PlayerGameInfo pgi);

                pgi.ui = ui;

                // Verifica se ele acabou todo o Tourney
                if (pgi.flag != PlayerGameInfo.eFLAG_GAME.FINISH)
                {
                    throw new exception("[Tourney::requestUseTicketReport][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou sair do jogo na sala[NUMERO=" + Convert.ToString(m_ri.numero) + "] com Ticket Report, mas ele ainda nao terminou o Tourney[FLAG=" + Convert.ToString((ushort)pgi.flag) + "]", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY,
                        403, 0));
                }

                // Verifica se o Level do player   maior ou igual a Beginner E
                if (_session.m_pi.level < (byte)enLEVEL.BEGINNER_E)
                {
                    throw new exception("[Tourney::requestUseTicketReport][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + ", LEVEL=" + Convert.ToString(_session.m_pi.level) + "] tentou sair do jogo na sala[NUMERO=" + Convert.ToString(m_ri.numero) + "] com Ticket Report, mas ele nao tem o level necessario[6=BEGINNER E] para usar o Ticket Report.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY,
                        405, 0));
                }

                // Verifica se o player tem o ticket report
                var pWi = _session.m_pi.findWarehouseItemByTypeid(TICKET_REPORT_TYPEID);

                // N o tem o item ou n o tem a quantidade "  a mesma coisa, s  estou fazendo isso pra previnir bugs"
                if (pWi == null || pWi.STDA_C_ITEM_QNTD < 1)
                {
                    throw new exception("[Tourney::requestUseTicketReport][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou sair do jogo na sala[NUMERO=" + Convert.ToString(m_ri.numero) + "] com Ticket Report, mas ele nao tem o item[TYPEID=" + Convert.ToString(TICKET_REPORT_TYPEID) + "]", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY,
                        400, 0));
                }

                // Tira um Ticket Report dele
                stItem item = new stItem();

                item.type = 2;
                item.id = (int)pWi.id;
                item._typeid = pWi._typeid;
                item.qntd = 1;
                item.STDA_C_ITEM_QNTD = (short)((short)item.qntd * -1);

                if (ItemManager.removeItem(item, _session) <= 0)
                {
                    throw new exception("[Tourney::requestUseTicketReport][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou sair do jogo na sala[NUMERO=" + Convert.ToString(m_ri.numero) + "] com Ticket Report, mas nao conseguiu deletar um Ticket Report Item do player.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY,
                        401, 0));
                }

                var v_item = new List<stItem>() { item };

                // Log
                _smp.message_pool.getInstance().push(new message("[Tourney::requestUseTicketReport][Log] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] sai do tourney na sala[NUMERO=" + Convert.ToString(m_ri.numero) + ", MASTER=" + Convert.ToString(m_ri.master) + "] com ticket report.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Respota para garantir que excluiu o ticket report mesmo do player


                packet_func.session_send(packet_func.pacote0AA(_session, v_item),
                    _session, 1);

                // Saiu com Ticket Report
                setGameFlag(pgi, PlayerGameInfo.eFLAG_GAME.TICKET_REPORT);

                rain_hole_consecutivos_count(_session); // conta os achievement de chuva em holes consecutivas

                score_consecutivos_count(_session); // conta os achievement de back-to-back(2 ou mais score iguais consecutivos) do player

                rain_count(_session); // Aqui achievement de rain count

                finish_game(_session, 1);

                ret = true; // Confirma o sai da sala

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Tourney::requestUseTicketReport][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Reposta de erro aqui, tenho que arranjar um pacote para isso
            }

            return ret;
        }

        public override void requestStartAfterEnter(Action action)
        { 
            uint milliseconds = 0;

            if (m_ri.qntd_hole == 18)
                milliseconds = 10 * 60000; // 10min
            else if (m_ri.qntd_hole == 9)
                milliseconds = 5 * 60000; // 5min 

            m_pTimer_after_enter = sgs.gs.getInstance().MakeTime(milliseconds, () => action()); 
        }

        public override void requestEndAfterEnter()
        {

            // Limpa timer After Enter
            clear_time_after_enter();

            // Send Resposta para todos que acabou o tempo para entrar na sala
            var p = new PangyaBinaryWriter((ushort)0x113);

            p.WriteByte(8);
            p.WriteByte(0);

            p.WriteByte((byte)m_player_info.Count());

            packet_func.game_broadcast(this,
                p, 1); 
        }

        public override void timeIsOver()
        {

            if (m_game_init_state == 1 && m_players.Count() > 0)
            {

                Player _session = null;

                foreach (var el in m_player_info)
                {

                    // S  os que n o acabaram
                    if (el.Value.flag == PlayerGameInfo.eFLAG_GAME.PLAYING && (_session = findSessionByUID(el.Value.uid)) != null)
                    {
                        finish_tourney(_session, 1);
                    }
                    else if (el.Value.flag == PlayerGameInfo.eFLAG_GAME.FINISH && (_session = findSessionByUID(el.Value.uid)) != null)
                    {
                        // Resposta para acabou o tempo do Tourney
                        sendTimeIsOver(_session);
                    }
                }
            }
        }

        public override bool init_game()
        {

            if (m_players.Count() > 0)
            {

                // Cria o timer do Tourney
                startTime();

                // variavel que salva a data local do sistema
                initGameTime();

                m_game_init_state = 1; // Come ou

                m_tourney_state = true;
            }

            return true;
        }

        public void clear_time_after_enter()
        {

            // Garantir que qualquer exception derrube o server

            try
            {

                if (m_pTimer_after_enter != null)
                    sgs.gs.getInstance().unMakeTime(m_pTimer_after_enter);
            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Tourney::clear_time_after_enter][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            m_pTimer_after_enter = null;
        }

        public virtual void requestFinishExpGame()
        {

            // Bug Fix, ultimo player do camp sai e ou toma dc e n o fica ningu m na sala e calcula a exp do camp
            if (getCountPlayersGame() > 0)
            {

                Player _session = null;
                float stars = m_course.getStar();
                int exp = 0;
                int hole_seq = 0;


                for (var i = 0; i < m_player_order.Count(); ++i)
                {

// Exp padr�o de hole do Grand Prix
				switch (m_ri.qntd_hole)
				{
				case 9:
					exp = 4;
					break;
				case 18:
					exp = 6;
					break;
				default:
					exp = 1;
					break;
				}
				
								exp = (int)(exp * stars);

                    hole_seq = (int)m_course.findHoleSeq(m_player_order[i].hole);

                    // Ele est  no primeiro hole e n o acertou ele, s  da experi ncia se ele tiver acertado o hole
                    if (hole_seq == 1 && !(m_player_order[i].shot_sync.state_shot.display.acerto_hole))
                    {
                        hole_seq = 0;
                    }

                    if (m_player_order[i].flag == PlayerGameInfo.eFLAG_GAME.FINISH)
                    {

                        if ((_session = findSessionByUID(m_player_order[i].uid)) != null)
                        {

                            exp = (int)(1 * m_player_order.Count() * (hole_seq > 0 ? hole_seq : 0) * stars);
                            exp = (int)(exp * TRANSF_SERVER_RATE_VALUE(m_player_order[i].used_item.rate.exp) * TRANSF_SERVER_RATE_VALUE(m_rv.exp));
                            exp = (int)(exp * (1 - (i / m_player_info.Count())));

                            if (m_player_order[i].level < 70)
                            {
                                m_player_order[i].data.exp = exp;
                            }
                        }

                    }
                    else if (m_player_order[i].flag == PlayerGameInfo.eFLAG_GAME.TICKET_REPORT)
                    {
                        exp = (int)(1 * m_player_order.Count() * (hole_seq > 0 ? hole_seq : 0) * stars);
                        exp = (int)(exp * TRANSF_SERVER_RATE_VALUE(m_player_order[i].used_item.rate.exp) * TRANSF_SERVER_RATE_VALUE(m_rv.exp));
                        exp = (int)(exp * (1 - (i / m_player_info.Count())));

                        m_player_order[i].data.exp = exp;
                    }
                    else if (m_player_order[i].flag == PlayerGameInfo.eFLAG_GAME.END_GAME)
                    {

                        if ((_session = findSessionByUID(m_player_order[i].uid)) != null)
                        {

                            exp = (int)(1 * m_player_order.Count() * (hole_seq > 0 ? hole_seq : 0) * stars);
                            exp = (int)(exp * TRANSF_SERVER_RATE_VALUE(m_player_order[i].used_item.rate.exp) * TRANSF_SERVER_RATE_VALUE(m_rv.exp));
                            exp = (int)(exp * (1 - (i / m_player_info.Count())));

                            if (m_player_order[i].level < 70)
                            {
                                m_player_order[i].data.exp = exp;
                            }
                        }
                    }
                }
            }
        }

        public virtual void finish()
        {

            m_game_init_state = 2; // Acabou

            requestCalculeRankPlace();

            requestMakeMedal();

            requestMakeTrofel();

            requestFinishExpGame();

            requestSaveTicketReport();

            requestSendTicketReport();

            requestGiveMedalAndItens();

            // [O pangya original, quando o player sai com ticket report do tourney,
            // mesmo que ele fique entre os 3 primeiros no short game n o conta o achievement de short game top 3 rank]

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

        public void requestMakeMedal()
        {

            uint all_player = getCountPlayersGame();

            // Medalhas s  s o liberadas apartir de 18 players no jogo
            if (all_player >= 18)
            {

                List<PlayerGameInfo> v_all_player = new List<PlayerGameInfo>();

                Lottery lottery = new Lottery();
                Lottery lot_active_item = new Lottery();

                Lottery.LotteryCtx ctx_lot = null;
                PlayerGameInfo pgi = null;

                // Active Item que o player ganha
                for (var i = 0; i < 15u; ++i)
                {
                    lot_active_item.Push(200, (sIff.getInstance().ITEM << 26) + i);
                }

                // Preenche vector, e alimenta o lottery
                foreach (var el in m_player_info)
                {
                    if (el.Value.flag != PlayerGameInfo.eFLAG_GAME.QUIT)
                    {

                        v_all_player.Add(el.Value);

                        lottery.Push(200, el.Value);
                    }
                }

                // 1 Medalha da sorte
                var ctx = lottery.spinRoleta();

                if (ctx == null)
                {
                    _smp.message_pool.getInstance().push(new message("[Tourney::requestMakeMedal][Error] nao conseguiu sortear um player para ganha a medalha da sorte", type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
                else
                {
                    pgi = ((PlayerGameInfo)ctx.Value);

                    m_medal[0].oid = (int)pgi.oid;

                    if ((ctx_lot = lot_active_item.spinRoleta()) == null)
                    {
                        _smp.message_pool.getInstance().push(new message("[Tourney::requestMakeMedal][Error] nao conseguiu sortear um active commun item da medalha da sorte", type_msg.CL_FILE_LOG_AND_CONSOLE));
                    }
                    else
                    {
                        m_medal[0].item_typeid = (uint)ctx_lot.Value;
                    }

                    pgi.medal_win.stMedal.lucky = 1;
                }

                // 2 Medalha Mais r pido
                v_all_player.Sort(speediest_sort);

                pgi = v_all_player.GetEnumerator().Current;

                m_medal[1].oid = (int)pgi.oid;

                if ((ctx_lot = lot_active_item.spinRoleta()) == null)
                {
                    _smp.message_pool.getInstance().push(new message("[Tourney::requestMakeMedal][Error] nao conseguiu sortear um active commun item da medalha de speediest", type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
                else
                {
                    m_medal[1].item_typeid = (uint)ctx_lot.Value;
                }

                pgi.medal_win.stMedal.speediest = 1;

                // 3 Medalha Melhor drive (Dist ncia tacada) 
                v_all_player.Sort(best_drive_sort);

                pgi = v_all_player.GetEnumerator().Current;

                m_medal[2].oid = (int)pgi.oid;

                if ((ctx_lot = lot_active_item.spinRoleta()) == null)
                {
                    _smp.message_pool.getInstance().push(new message("[Tourney::requestMakeMedal][Error] nao conseguiu sortear um active commun item da medalha de best drive", type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
                else
                {
                    m_medal[2].item_typeid = (uint)ctx_lot.Value;
                }

                pgi.medal_win.stMedal.best_drive = 1;

                // 4 Melha Melhor Chip-in 
                v_all_player.Sort(best_chipin_sort);

                pgi = v_all_player.GetEnumerator().Current;

                m_medal[3].oid = (int)pgi.oid;

                if ((ctx_lot = lot_active_item.spinRoleta()) == null)
                {
                    _smp.message_pool.getInstance().push(new message("[Tourney::requestMakeMedal][Error] nao conseguiu sortear um active commun item da medalha de best chipin", type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
                else
                {
                    m_medal[3].item_typeid = (uint)ctx_lot.Value;
                }

                pgi.medal_win.stMedal.best_chipin = 1;

                // 5 Medalha Melhor Long Puttin 
                v_all_player.Sort(best_long_puttin_sort);

                pgi = v_all_player.GetEnumerator().Current;

                m_medal[4].oid = (int)pgi.oid;

                if ((ctx_lot = lot_active_item.spinRoleta()) == null)
                {
                    _smp.message_pool.getInstance().push(new message("[Tourney::requestMakeMedal][Error] nao conseguiu sortear um active commun item da medalha de best long puttin", type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
                else
                {
                    m_medal[4].item_typeid = (uint)ctx_lot.Value;
                }

                pgi.medal_win.stMedal.best_long_puttin = 1;

                // 6 Medalha Melhor Recupera  o (S  da se for 18h)
                if (m_ri.qntd_hole == 18)
                {
                    v_all_player.Sort(best_recovery);

                    pgi = v_all_player.GetEnumerator().Current;

                    m_medal[5].oid = (int)pgi.oid;

                    if ((ctx_lot = lot_active_item.spinRoleta()) == null)
                    {
                        _smp.message_pool.getInstance().push(new message("[Tourney::requestMakeMedal][Error] nao conseguiu sortear um active commun item da medalha de best recovery", type_msg.CL_FILE_LOG_AND_CONSOLE));
                    }
                    else
                    {
                        m_medal[5].item_typeid = (uint)ctx_lot.Value;
                    }

                    pgi.medal_win.stMedal.best_recovery = 1;
                }

            }
        }

        public void requestMakeTrofel()
        {

            uint all_player = getCountPlayersGame();

            int count_trofel = 0;
            int i = 0;

            if (m_player_order.Count() <= 0)
            {
                requestCalculeRankPlace();
            }

            if (m_player_order.Count() != all_player)
            {

                _smp.message_pool.getInstance().push(new message("[Tourney::requestMakeTrofel][Error] nao conseguiu gerar os trofeus por que o vector de player rank order nao bate com o dos players no jogo", type_msg.CL_FILE_LOG_AND_CONSOLE));

                return;
            }

            Lottery lottery = new Lottery();
            Lottery.LotteryCtx ctx = null;

            // Active Cummon Item
            for (i = 0; i < 15u; ++i)
            {
                lottery.Push(200, (sIff.getInstance().ITEM << 26) + i);
            }

            if (m_ri.qntd_hole == 18 && all_player >= 10)
            {
                // --- 18 Holes Tourney ----
                // 10-14 = 1 bronze
                // 15-18 = 1 silver e 1 bronze
                // 19-22 = 1 gold, 1 silver e 1 bronze
                // 23-26 = 1 gold, 1 silver e 2 bronze
                // 27-30 = 1 gold, 2 silver e 3 bronze

                if (all_player <= 14u)
                {
                    count_trofel = 1;
                }
                else if (all_player <= 18u)
                {
                    count_trofel = 2;
                }
                else if (all_player <= 22u)
                {
                    count_trofel = 3;
                }
                else if (all_player <= 26u)
                {
                    count_trofel = 4;
                }
                else if (all_player <= 30u)
                {
                    count_trofel = 6;
                }

            }
            else if (m_ri.qntd_hole == 9 && all_player >= 15)
            {
                // --- 9 Holes Tourney ---
                // 15-18 = 1 bronze
                // 19-26 = 1 silver e 1 bronze
                // 27-30 = 1 gold, 1 silver e 1 bronze

                if (all_player <= 18u)
                {
                    count_trofel = 1;
                }
                else if (all_player <= 26u)
                {
                    count_trofel = 2;
                }
                else if (all_player <= 30u)
                {
                    count_trofel = 3;
                }
            }

            // Novo item commun e trof u
            List<PlayerGameInfo> trofeus = new List<PlayerGameInfo>();

            if (trofeus.Count > 0)
            {
                trofeus.Clear();
            }

            foreach (var el in m_player_info)
            {
                if (el.Value.flag != PlayerGameInfo.eFLAG_GAME.QUIT && (el.Key != null && el.Key.m_pi.ui.getQuitRate() < QUITER_ICON_2)) // menos os que quitaram e os QUITER_ICON_2
                {
                    trofeus.Add(el.Value);
                }
            }

            // sort 
            trofeus.Sort(base.sort_player_rank);

            // give trofeus
            if (trofeus.Count > 0)
            {

                if (trofeus.Count < count_trofel)
                {

                    for (i = 6; i < (count_trofel + 6u) && i < (trofeus.Count + 6u); ++i)
                    {

                        m_medal[i].oid = (int)trofeus[i - 6].oid;

                        if ((ctx = lottery.spinRoleta()) == null)
                        {
                            _smp.message_pool.getInstance().push(new message("[Tourney::requestMakeTrofel][Error] nao conseguiu sortear um active commun item do trofel", type_msg.CL_FILE_LOG_AND_CONSOLE));
                        }
                        else
                        {
                            m_medal[i].item_typeid = (uint)ctx.Value;
                        }
                    }

                    // S  da os trofeus se n o for Evento GM
                    if (!(m_ri.trofel == TROFEL_GM_EVENT_TYPEID && m_ri.flag_gm == 1 && m_ri.max_player > 30 && m_ri.state_flag == 0x100))
                    {

                        switch (count_trofel)
                        {
                            case 1:
                                trofeus[0].trofel = 3;
                                break;
                            case 2:
                                for (i = 0; i < 2u && i < trofeus.Count; ++i)
                                {
                                    trofeus[i].trofel = (byte)((byte)i + 2);
                                }
                                break;
                            case 3:
                                for (i = 0; i < 3u && i < trofeus.Count; ++i)
                                {
                                    trofeus[i].trofel = (byte)((byte)i + 1);
                                }
                                break;
                            case 4:
                                for (i = 0; i < 4u && i < trofeus.Count; ++i)
                                {
                                    trofeus[i].trofel = (byte)((i < 3u) ? i + 1 : 3);
                                }
                                break;
                            case 6:
                                for (i = 0; i < 6u && i < trofeus.Count; ++i)
                                {
                                    if (i == 0u)
                                    {
                                        trofeus[i].trofel = (byte)1;
                                    }
                                    else if (i < 3u)
                                    {
                                        trofeus[i].trofel = (byte)2;
                                    }
                                    else if (i < 6u)
                                    {
                                        trofeus[i].trofel = (byte)3;
                                    }
                                }
                                break;
                        }
                    }

                }
                else
                {

                    for (i = 6; i < (count_trofel + 6u); ++i)
                    {

                        m_medal[i].oid = (int)trofeus[i - 6].oid;

                        if ((ctx = lottery.spinRoleta()) == null)
                        {
                            _smp.message_pool.getInstance().push(new message("[Tourney::requestMakeTrofel][Error] nao conseguiu sortear um active commun item do trofel", type_msg.CL_FILE_LOG_AND_CONSOLE));
                        }
                        else
                        {
                            m_medal[i].item_typeid = (uint)ctx.Value;
                        }
                    }

                    // S  da os trofeus se n o for Evento GM
                    if (!(m_ri.trofel == TROFEL_GM_EVENT_TYPEID && m_ri.flag_gm == 1 && m_ri.max_player > 30 && m_ri.state_flag == 0x100))
                    {

                        switch (count_trofel)
                        {
                            case 1:
                                trofeus[0].trofel = 3;
                                break;
                            case 2:
                                for (i = 0; i < 2u; ++i)
                                {
                                    trofeus[i].trofel = (byte)((byte)i + 2);
                                }
                                break;
                            case 3:
                                for (i = 0; i < 3u; ++i)
                                {
                                    trofeus[i].trofel = (byte)((byte)i + 1);
                                }
                                break;
                            case 4:
                                for (i = 0; i < 4u; ++i)
                                {
                                    trofeus[i].trofel = (byte)((i < 3u) ? i + 1 : 3);
                                }
                                break;
                            case 6:
                                for (i = 0; i < 6u; ++i)
                                {
                                    if (i == 0u)
                                    {
                                        trofeus[i].trofel = (byte)1;
                                    }
                                    else if (i < 3u)
                                    {
                                        trofeus[i].trofel = (byte)2;
                                    }
                                    else if (i < 6u)
                                    {
                                        trofeus[i].trofel = (byte)3;
                                    }
                                }
                                break;
                        }
                    }
                }
            }


        }

        public void requestSaveTicketReport()
        {

            //// Adiciona o Ticket Report do Tourney
            CmdInsertTicketReport cmd_itr = new CmdInsertTicketReport(m_ri.trofel, // Waiter
                4, true);

            snmdb.NormalManagerDB.getInstance().add(0,
                cmd_itr, null, null);

            if (cmd_itr.getException().getCodeError() != 0)
            {
                throw cmd_itr.getException();
            }

            m_tri.clear();
            TicketReportInfo.stTicketReportDados trd = new TicketReportInfo.stTicketReportDados();

            m_tri.id = cmd_itr.getId();

            foreach (var el in m_player_info)
            {
                trd.clear();

                trd.uid = el.Value.uid;
                trd.exp = el.Value.data.exp;
                trd.pang = el.Value.data.pang;
                trd.bonus_pang = el.Value.data.bonus_pang;
                trd.mascot_typeid = el.Value.mascot_typeid;
                trd.flag_item_pang = (uint)el.Value.boost_item_flag.ucFlag;
                trd.medal = el.Value.medal_win;
                trd.premium = (uint)(el.Value.premium_flag ? 1 : 0);
                trd.score = el.Value.data.score;
                trd.state = (uint)((el.Value.flag == PlayerGameInfo.eFLAG_GAME.QUIT ? 4 : 0) | el.Value.enter_after_started);
                trd.trofel = el.Value.trofel; // Rank, Ouro, Prata e Bronze
                trd.finish_time = el.Value.time_finish;

                snmdb.NormalManagerDB.getInstance().add(1,
                    new CmdInsertTicketReportData(m_tri.id, trd),
                    Tourney.SQLDBResponse, this);

                m_tri.v_dados.Add(trd);
            }
        }

        public void requestSendTicketReport()
        {

            if (m_tri.id != -1)
            { // Tem Ticket Report o Tourney

                stItem item = new stItem();
                Player _session = null;
                var p = new PangyaBinaryWriter();

                item.type = 2;
                item._typeid = TICKET_REPORT_SCROLL_TYPEID;
                item.qntd = 1;
                item.STDA_C_ITEM_QNTD = (short)item.qntd;

                // Ticket Report ID
                item.c[1] = (short)(m_tri.id / 0x8000);
                item.c[2] = (short)(m_tri.id % 0x8000);

                item.flag = 0x20;
                item.flag_time = 0x20; // Horas
                item.STDA_C_ITEM_TIME = 24; // 24 Horas

                foreach (var el in m_player_info)
                {

                    // S  para os que sairam com Ticket Report
                    if (el.Value.flag == PlayerGameInfo.eFLAG_GAME.TICKET_REPORT)
                    {

                        item.id = -1;

                        // Envia para os players online
                        //if (sgs::gs != null) {
                        if ((_session = sgs.gs.getInstance().findPlayer(el.Value.uid)) != null)
                        {

                            // Add Ticket Report Item
                            var rt = ItemManager.RetAddItem.T_INIT_VALUE;

                            if ((rt = ItemManager.addItem(item,
                                _session, 0, 0)) < 0)
                            {
                                throw new exception("[Tourney::requestSendTicketReport][Error] PLAYER[UID=" + Convert.ToString(el.Value.uid) + "], nao conseguiu adicionar o Ticket Report Item[TYPEID=" + Convert.ToString(item._typeid) + "] para o player.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY,
                                    1, 0));
                            }

                            // Reposta para o Ticket Report Treasure Hunter Item(ns)
                            p.init_plain(0x11C);

                            p.WriteByte(1); // OK

                            packet_func.session_send(p,
                                _session, 1);

                            // Update UserInfo, TrofelInfo e MapStatistics
                            sendUpdateInfoAndMapStatistics(_session, 0);

                            if (rt != ItemManager.RetAddItem.T_SUCCESS_PANG_AND_EXP_AND_CP_POUCH)
                            {

                                var v_item = new List<stItem>() { item };

                                // Add Ticket Report Item 
                                packet_func.session_send(packet_func.pacote0AA(_session, v_item),
                                    _session, 1);
                            }

                        }
                        else
                        { // Player Est  OFFLINE

                            var rt = ItemManager.RetAddItem.T_INIT_VALUE;

                            if ((rt = ItemManager.addItem(item,
                                el.Value.uid, 0, 0)) < 0)
                            {
                                _smp.message_pool.getInstance().push(new message("[Tourney::requestSendTicketReport][Error] PLAYER[UID=" + Convert.ToString(el.Value.uid) + "] nao conseguiu adicionar o Ticket Report item[TYPEID=" + Convert.ToString(item._typeid) + "] para o player", type_msg.CL_FILE_LOG_AND_CONSOLE));
                            }

                        }
                    }
                }
            }
        }

        public void requestGiveMedalAndItens()
        {

            Dictionary<PlayerGameInfo, List<stItem>> map_item = new Dictionary<PlayerGameInfo, List<stItem>>();

            List<PlayerGameInfo> all_player = new List<PlayerGameInfo>();
            List<stItem> v_item = new List<stItem>();
            stItem item = new stItem();

            // Preenche vector, e alimenta o lottery
            foreach (var el in m_player_info)
            {
                if (el.Value.flag != PlayerGameInfo.eFLAG_GAME.QUIT)
                    all_player.Add(el.Value);
            }

            for (int i = 0; i < m_medal.Length; ++i)
            {
                if (m_medal[i].oid != -1)
                {
                    v_item.Clear();

                    // Item de premiação
                    item.clear();
                    item.type = 2;
                    item.id = -1;
                    item._typeid = m_medal[i].item_typeid;
                    item.qntd = 1;
                    item.STDA_C_ITEM_QNTD = (short)item.qntd;

                    v_item.Add(item);

                    // Medalha (índices 0 a 5)
                    if (i < 6)
                    {
                        item.clear();
                        item.type = 2;
                        item.id = -1;
                        item._typeid = (uint)(i == 0 ? 0x1A0000F5u : 0x1A0000F0u + (i - 1));
                        item.qntd = 1;
                        item.STDA_C_ITEM_QNTD = (short)item.qntd;

                        v_item.Add(item);
                    }

                    if (v_item.Count == 0)
                        continue;

                    // Busca o jogador pelo OID
                    PlayerGameInfo player = all_player.FirstOrDefault(pl => pl.oid == m_medal[i].oid);

                    if (player == null)
                    {
                        _smp.message_pool.getInstance().push(new message("[Tourney::requestGiveMedalAndItens][Error] player_info[OID=" + Convert.ToString(m_medal[i].oid) + "] nao tem nos player_all que ficaram no camp ou saiu com ticket report.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                        continue;
                    }

                    // Se já existe, adiciona os itens à lista existente
                    if (map_item.TryGetValue(player, out List<stItem> item_list))
                    {
                        item_list.AddRange(v_item);
                    }
                    else
                    {
                        map_item[player] = new List<stItem>(v_item);
                    }
                }
            }


            /// Send Itens e Trofel
            Player _session = null;

            var p = new PangyaBinaryWriter();

            foreach (var el in map_item)
            {
                // Itens
                if (!el.Value.empty())
                {

                    // Player Online
                    if ((_session = sgs.gs.getInstance().findPlayer(el.Key.uid)) != null)
                    {

                        var rai = ItemManager.addItem(el.Value,
                            _session.getUID(), 0, 0);

                        if (rai.fails.Count() > 0 && rai.type != ItemManager.RetAddItem.T_SUCCESS_PANG_AND_EXP_AND_CP_POUCH)
                        {
                            _smp.message_pool.getInstance().push(new message("[Tourney::requestGiveMedalAndItens][Error] PLAYER[UID=" + Convert.ToString(el.Key.uid) + "] nao conseguiu adicionar os itens que ele ganhou com medalhas e trofeus.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                        }

                        // Resposta Add Item para o player
                        if (el.Value.Count() > 0)
                        {

                            packet_func.session_send(packet_func.pacote0AA(_session, el.Value),
                                _session, 1);
                        }

                    }
                    else
                    { // Player Offline
                        var rai = ItemManager.addItem(el.Value,
                            el.Key.uid, 0, 0);

                        if (rai.fails.Count() > 0 && rai.type != ItemManager.RetAddItem.T_SUCCESS_PANG_AND_EXP_AND_CP_POUCH)
                        {
                            _smp.message_pool.getInstance().push(new message("[Tourney::requestGiveMedalAndItens][Error] PLAYER[UID=" + Convert.ToString(el.Key.uid) + "] nao conseguiu adicionar os itens que ele ganhou com medalhas e trofeus.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                        }
                    }
                }

                // Trofeus
                if (m_ri.trofel != 0 && el.Key.trofel != 0)
                {

                    // Player Online
                    if ((_session = sgs.gs.getInstance().findPlayer(el.Key.uid)) != null)
                    {

                        _session.m_pi.updateTrofelInfo(m_ri.trofel, el.Key.trofel);

                        // Update Tofel do player no jogo
                        sendUpdateInfoAndMapStatistics(_session, 0);

                    }
                    else // Player Offline
                    {
                        PlayerInfo.updateTrofelInfo(el.Key.uid,
                            m_ri.trofel, el.Key.trofel);
                    }
                }

                // Madelhas - Ganhou medalhas
                if (el.Key.medal_win.ucMedal != 0u)
                {

                    // Player Online
                    if ((_session = sgs.gs.getInstance().findPlayer(el.Key.uid)) != null)
                    {

                        // Update Medal do player
                        _session.m_pi.updateMedal(el.Key.medal_win);

                        // Update Medal do player no jogo
                        sendUpdateInfoAndMapStatistics(_session, 0);

                    }
                    else
                    { // Player Offline

                        // Update Medal do player
                        PlayerInfo.updateMedal(el.Key.uid, el.Key.medal_win);

                    }
                }
            }
        }

        public virtual void requestFinishData(Player _session)
        {

            // Finish Artefact Frozen Flame agora   direto no Finish Item Used Game
            requestFinishItemUsedGame(_session);

            requestSaveDrop(_session);

            // Tourney GM n o tem treasure Hunter Item
            if (!(m_ri.trofel == TROFEL_GM_EVENT_TYPEID && m_ri.max_player > 30 && m_ri.flag_gm == 1 && m_ri.state_flag == 0x100))
            {
                requestDrawTreasureHunterItem(_session);
            }

            rain_hole_consecutivos_count(_session); // conta os achievement de chuva em holes consecutivas

            score_consecutivos_count(_session); // conta os achievement de back-to-back(2 ou mais score iguais consecutivos) do player

            rain_count(_session); // Aqui achievement de rain count

            achievement_top_3_1st(_session); // Se o Player ficou em Top 3 add +1 ao contador de top 3, e se ele ficou em primeiro add +1 ao do primeiro

            //INIT_PLAYER_INFO("requestFinishData", "tentou finalizar dados do jogo", &_session);

            // Resposta terminou game - Drop Itens
            sendDropItem(_session);

            // Resposta terminou game - Placar
            sendPlacar(_session);

            // Resposta Treasure Hunter Item Draw
            sendTreasureHunterItemDrawGUI(_session);
        }
        public override int checkEndShotOfHole(Player _session)
        {

            // Agora verifica o se ele acabou o hole e essas coisas
            INIT_PLAYER_INFO("checkEndShotOfHole",
                "tentou verificar a ultima tacada do hole no jogo",
                _session, out PlayerGameInfo pgi);

            if (pgi.shot_sync.state_shot.display.acerto_hole || pgi.data.giveup == 1)
            {

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

                        var map = MapSystem.getInstance().getMap((byte)(m_ri.course & RoomInfo.ROOM_INFO_COURSE.UNK));

                        if (map == null)
                        {
                            _smp.message_pool.getInstance().push(new message("[TourneyBase::checkEndShotOfHole][Error][Warning] tentou pegar o Map dados estaticos do course[COURSE=" + Convert.ToString((ushort)((byte)(m_ri.course & RoomInfo.ROOM_INFO_COURSE.UNK))) + "], mas nao conseguiu encontra na classe do Server.", type_msg.CL_FILE_LOG_AND_CONSOLE));
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

            return 0;
        }

        public override void requestCalculeShotSpinningCube(Player _session, ShotSyncData _ssd)
        {
            //CHECK_SESSION_BEGIN("requestCalculeShotSpinningCube");

            try
            {

                // S  calcula se n o for short game
                if (!(m_ri.special_flag_mod.short_game))
                {
                    calcule_shot_to_spinning_cube(_session, _ssd);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Tourney::requestCalculeShotSpinningCube][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void requestCalculeShotCoin(Player _session, ShotSyncData _ssd)
        {
            //CHECK_SESSION_BEGIN("requestCalculeShotCoin");

            try
            {

                // S  calcula se n o for short game
                if (!(m_ri.special_flag_mod.short_game))
                {
                    calcule_shot_to_coin(_session, _ssd);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Tourney::requestCalculeShotCoin][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public int speediest_sort(PlayerGameInfo _pgi1, PlayerGameInfo _pgi2)
        {
            if (_pgi1.progress.isGoodScore() && !_pgi2.progress.isGoodScore())
                return -1;

            if (!_pgi1.progress.isGoodScore() && _pgi2.progress.isGoodScore())
                return 1;

            TimeSpan diff = _pgi1.time_finish.ConvertTime() - _pgi2.time_finish.ConvertTime();

            return diff.CompareTo(TimeSpan.Zero);
        }

        public int best_drive_sort(PlayerGameInfo _pgi1, PlayerGameInfo _pgi2)
        {
            if (_pgi1.progress.isGoodScore() && !_pgi2.progress.isGoodScore())
                return -1;

            if (!_pgi1.progress.isGoodScore() && _pgi2.progress.isGoodScore())
                return 1;

            return _pgi2.progress.best_drive.CompareTo(_pgi1.progress.best_drive);
        }

        public int best_chipin_sort(PlayerGameInfo _pgi1, PlayerGameInfo _pgi2)
        {
            if (_pgi1.progress.isGoodScore() && !_pgi2.progress.isGoodScore())
                return -1;

            if (!_pgi1.progress.isGoodScore() && _pgi2.progress.isGoodScore())
                return 1;

            return _pgi2.progress.best_chipin.CompareTo(_pgi1.progress.best_chipin);
        }

        public int best_long_puttin_sort(PlayerGameInfo _pgi1, PlayerGameInfo _pgi2)
        {
            if (_pgi1.progress.isGoodScore() && !_pgi2.progress.isGoodScore())
                return -1;

            if (!_pgi1.progress.isGoodScore() && _pgi2.progress.isGoodScore())
                return 1;

            return _pgi2.progress.best_long_puttin.CompareTo(_pgi1.progress.best_long_puttin);
        }

        public int best_recovery(PlayerGameInfo p1, PlayerGameInfo p2)
        {
            if (p1.progress.isGoodScore() && !p2.progress.isGoodScore())
                return -1; // p1 vem antes

            if (!p1.progress.isGoodScore() && p2.progress.isGoodScore())
                return 1; // p2 vem antes

            // Menor recuperação é melhor
            return p1.progress.getBestRecovery().CompareTo(p2.progress.getBestRecovery());
        }


        public override bool finish_game(Player _session, int option)
        {

            if (_session.getState()
                && _session.isConnected()
                && m_players.Count() > 0)
            {

                var p = new PangyaBinaryWriter();

                if (option == 6)
                {

                    if (m_tourney_state)
                    {
                        finish_tourney(_session, 1); // Termina sem ter acabado de jogar
                    }

                    INIT_PLAYER_INFO("finish_game",
                        "tentou terminar o jogo",
                        _session, out PlayerGameInfo pgi);

                    // Salve o record se o camp acabou e o player n o terminou todos os holes tbm tem que salvar o record [OK][Feito]
                    requestSaveRecordCourse(_session,
                        0,
                        (m_ri.qntd_hole == 18 && (m_course.findHoleSeq(pgi.hole) == 18 || pgi.flag == PlayerGameInfo.eFLAG_GAME.END_GAME)) ? 1 : 0);

                    requestSaveInfo(_session, 0);

                    // D  Exp para o Caddie E Mascot Tamb m
                    if (pgi.data.exp > 0)
                    { // s  add exp se for maior que 0

                        // Add Exp para o player
                        _session.addExp(pgi.data.exp, false);

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
                else if (option == 1)
                {

                    INIT_PLAYER_INFO("finish_game",
                        "tentou terminar o jogo", _session, out PlayerGameInfo pgi);

                    // Finish Artefact Frozen Flame agora   direto no Finish Item Used Game
                    requestFinishItemUsedGame(_session);

                    requestSaveDrop(_session);

                    requestDrawTreasureHunterItem(_session);

                    // Aqui que vem esse aqui, o Save record course e o save info
                    requestSaveRecordCourse(_session,
                        0,
                        (m_ri.qntd_hole == 18 && m_course.findHoleSeq(pgi.hole) == 18) ? 1 : 0);

                    requestSaveInfo(_session, 0);

                    // Resposta Treasure Hunter Item Draw
                    sendTreasureHunterItemDrawGUI(_session);

                    // Resposta de Sai com Ticket Report
                    p.init_plain(0x12A);

                    p.WriteUInt32(0); // OK

                    packet_func.session_send(p,
                        _session, 1);

                    // Update Info Map Statistics
                    sendUpdateInfoAndMapStatistics(_session, 0);

                    // Resposta terminou game - Drop Itens
                    sendDropItem(_session);

                    // Pacote dizendo para sair da sala e voltar para a Lobby normal por que Ticket report s  pode user em Tourney
                    // Sa  da sala, visual

                    packet_func.session_send(packet_func.pacote04C(-1),
                        _session, 1);

                    // Resposta Envia os itens ganhos no Treasure Hunter
                    requestSendTreasureHunterItem(_session);

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

                }
            }

            return (PlayersCompleteGameAndClear() && m_tourney_state);
        }
    }
}