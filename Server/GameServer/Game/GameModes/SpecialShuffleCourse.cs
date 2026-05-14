using System;
using System.Collections.Generic;
using System.Linq;
using Pangya_GameServer.Game.Base;
using Pangya_GameServer.Game.Manager;
using Pangya_GameServer.Game.System;
using Pangya_GameServer.Models;
using Pangya_GameServer.PacketFunc;
using PangyaAPI.IFF.JP.Extensions;
using PangyaAPI.IFF.JP.Models.Flags;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Models;
using PangyaAPI.Utilities.Log;

namespace Pangya_GameServer.Game.GameModes
{
    public class SpecialShuffleCourse : TourneyBase, IDisposable
    {
        bool m_SSC_state;
        uint m_coin_SSC;        // Que o master da sala ganha se ficar at� o final
        public uint SPECIAL_SHUFFLE_COURSE_COIN_TYPEID = 0x1A0000F8;
        public uint ART_ROGER_K_STEERING_WHEEL = 0x1A0001BCu;	// de 500 a 501000 pangs no Ultimo Hole do game de 18H

        public SpecialShuffleCourse(List<Player> _players, RoomInfoEx _ri, RateValue _rv, bool _channel_rookie) : base(_players, _ri, _rv, _channel_rookie)
        {
            this.m_SSC_state = false;
            this.m_coin_SSC = 0;

            // Atualiza Treasure Hunter System Course

            if (!sTreasureHunterSystem.getInstance().isLoad())
            {
                sTreasureHunterSystem.getInstance().load();
            }

            var course = sTreasureHunterSystem.getInstance().findCourse((byte)(m_ri.getMap() & 0x7F));

            if (course == null)
            {
                _smp.message_pool.getInstance().push(new message("[SpecialShuffleCourse::SpecialShuffleCourse][Error] tentou pegar o course do Treasure Hunter System, mas o course[COURSE=" + Convert.ToString((ushort)(m_ri.getMap() & 0x7F)) + "] nao existe no sistema", type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
            else
            {
                sTreasureHunterSystem.getInstance().updateCoursePoint(course, -1); // -1 ponto a cada jogo iniciado
            }

            // Aqui tem que inicializar os players info
            initAllPlayerInfo();

            foreach (var el in m_players)
            {

                INIT_PLAYER_INFO("SpecialShuffleCourse",
                    "tentou inicializar o counter item do Special Shuffle Course",
                    el, out PlayerGameInfo pgi);

                initAchievement(el);

                pgi.sys_achieve.incrementCounter(0x6C40001Fu); // Por que ele   um Tourney
            }

            m_state = init_game();

        }

        public override void Dispose(bool disposing)
        {
            if (disposing)
            {
                deleteAllPlayer();

                LogDestruction();

                m_SSC_state = false;
            }
            base.Dispose(true);
        }

        ~SpecialShuffleCourse()
        {
            Dispose(false);
        }

        public override bool deletePlayer(Player _session, int _option)
        {
            if (_session == null)
            {
                throw new exception("[SpecialShuffleCourse::deletePlayer][Error] tentou deletar um player, mas o seu endereco é nullptr.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME,
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

                        var pgi = INIT_PLAYER_INFO("deletePlayer",
                            "tentou sair do jogo",
                            _session);

                        var sessions = getSessions(it);

                        requestFinishItemUsedGame((it)); // Salva itens usados no Tourney 


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
                            ret = true; // Termina o Tourney

                        sendUpdateInfoAndMapStatistics(_session, -1);
                    }

                    // Delete Player
                    m_players.Remove(it);
                }
                else
                {
                    _smp.message_pool.getInstance().push(new message("[SpecialShuffleCourse::deletePlayer][Warning] player ja foi excluido do game.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[SpecialShuffleCourse::deletePlayer][Error] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Libera Critical Section

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

        public override void changeHole(Player _session)
        {

            updateTreasureHunterPoint(_session);

            if (checkEndGame(_session))
            {
                finish_SSC(_session, 0);
            }
            else
            {
                // Resposta terminou o hole
                updateFinishHole(_session, 1); // Terminou
            }
        }

        public override void finishHole(Player _session)
        {

            requestFinishHole(_session, 0);

            requestUpdateItemUsedGame(_session);
        }

        public void finish_SSC(Player _session, int _option)
        {

            if (m_players.Count > 0 && m_game_init_state == 1)
            {

                var pgi = INIT_PLAYER_INFO("finish_SSC",
                    "tentou terminar o Special Shuffle Course no jogo",
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
                    {
                        // Acabou o Tempo 
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
        public override void timeIsOver()
        {

            if (m_game_init_state == 1 && m_players.Count > 0)
            {

                Player _session = null;

                foreach (var el in m_player_info)
                {

                    // Só os que n o acabaram
                    if (el.Value.flag == PlayerGameInfo.eFLAG_GAME.PLAYING && (_session = findSessionByUID(el.Value.uid)) != null)
                    {
                        finish_SSC(_session, 1);
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

            if (m_players.Count > 0)
            {

                // Cria o timer do Tourney
                startTime();

                // variavel que salva a data local do sistema
                initGameTime();

                // Aqui achievement de rain count
                // Esse aqui tem que ser na hora que finaliza o jogo por que depende de quantos holes o player completou
                //rain_count_players();

                m_game_init_state = 1; // Come ou

                m_SSC_state = true;
            }

            return true;
        }

        public void finish()
        {

            m_game_init_state = 2; // Acabou

            requestCalculeRankPlace();

            requestMakeMasterCoin();

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

        public override DropItemRet requestInitDrop(Player _session)
        {

            try
            {

                var dir = base.requestInitDrop(_session);

                var pgi = INIT_PLAYER_INFO("requestInitDrop",
                    "tentou sortear o Drop do SSC no jogo",
                    _session);

                // Verifica se   o ultimo hole e sortea os pangs do final do SSC
                // Artefact Pang Drop
                if (m_ri.qntd_hole == m_course.findHoleSeq(pgi.hole) && m_ri.qntd_hole == 18)
                { // Ultimo Hole, de 18h Game

                    DropSystem.stCourseInfo ci = new DropSystem.stCourseInfo();

                    // Init Course Info Drop System
                    ci.artefact = ART_ROGER_K_STEERING_WHEEL; // Para da os Pangs do SSC   o mesmo que o artefact
                    ci.char_motion = pgi.char_motion_item;
                    ci.course = (byte)(m_ri.getMap() & 0x7F);
                    ci.hole = pgi.hole;
                    ci.qntd_hole = m_ri.qntd_hole;

                    var art_pang = sDropSystem.getInstance().drawArtefactPang(ci, (uint)m_players.Count);

                    if (art_pang._typeid != 0)
                    { // Dropou

                        dir.v_drop.Add(art_pang);

                        // add para o drop list do player
                        pgi.drop_list.v_drop.Add(art_pang);

                        if (art_pang.qntd >= 30)
                        { // Envia notice que o player ganhou jackpot

                            var p = new PangyaBinaryWriter((ushort)0x40);

                            p.WriteByte(10); // JackPot

                            p.WriteString(_session.m_pi.nickname);

                            p.WriteUInt16(0); // size Msg

                            p.WriteUInt32((uint)(art_pang.qntd * 500));

                            packet_func.game_broadcast(this,
                                p, 1);
                        }
                    }
                }
                return dir;
            }
            catch (exception e)
            {
                _smp.message_pool.getInstance().push(new message("[SpecialShuffleCourse::RequestInitDrop][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            return new DropItemRet();
        }
        public override void requestUpdateItemUsedGame(Player _session)
        {

            var pgi = INIT_PLAYER_INFO("requestUpdateItemUsedGame",
                "tentou atualizar itens usado no jogo",
                _session);

            var ui = pgi.used_item;

            ui.club.count += (uint)(1.5f * 10.0f * ui.club.rate * TRANSF_SERVER_RATE_VALUE(m_rv.clubset) * TRANSF_SERVER_RATE_VALUE(ui.rate.club));

            // Passive Item exceto Time Booster e Auto Command, que soma o contador por uso, o cliente passa o pacote, dizendo que usou o item
            foreach (var el in ui.v_passive)
            {
                // Passive Item no SSC só consome os item boost de pang e o Club Mastery Boost,
                // Consome todos os outros menos os de Experiência
                if (DefineConstants.passive_item_exp.Any(c => c == el.Value._typeid))
                {
                    if (DefineConstants.CHECK_PASSIVE_ITEM(el.Value._typeid)
                    && el.Value._typeid != DefineConstants.TIME_BOOSTER_TYPEID/* / *Time Booster * /*/ && el.Value._typeid != DefineConstants.AUTO_COMMAND_TYPEID)
                        el.Value.count++;
                    else if (sIff.getInstance().getItemGroupIdentify(el.Value._typeid) == IFF_GROUP.BALL /*/ *Ball * /*/ || sIff.getInstance().getItemGroupIdentify(el.Value._typeid) == IFF_GROUP.AUX_PART)
                        el.Value.count++;
                }
            }
        }

        public void requestFinishData(Player _session)
        {

            // Finish Artefact Frozen Flame agora é direto no Finish Item Used Game
            requestFinishItemUsedGame(_session);

            requestSaveDrop(_session);

            requestDrawTreasureHunterItem(_session);

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

            // Resposta Treasure Hunter Item Draw
            sendTreasureHunterItemDrawGUI(_session);
        }

        public override void requestDrawTreasureHunterItem(Player _session)
        {

            if (!sTreasureHunterSystem.getInstance().isLoad()) sTreasureHunterSystem.getInstance().load();

            var pgi = INIT_PLAYER_INFO("requestDrawTreasureHunterItem",
                "tentou sortear os item(ns) do Treasure Hunter do jogo",
                _session);


            pgi.thi.v_item = sTreasureHunterSystem.getInstance().drawItem(pgi.thi.treasure_point, (byte)(m_ri.getMap() & 0x7F));

            if (pgi.thi.v_item.empty())
                _smp.message_pool.getInstance().push(deque: new message("[SpecialShuffleCourse::requestDrawTreasureHunterItem][Warning] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou sortear os item(ns) do Treasure Hunter do jogo," + "mas o Treasure Hunter Item nao conseguiu sortear nenhum item", type_msg.CL_FILE_LOG_AND_CONSOLE));
        }

        public void requestMakeMasterCoin()
        {
            m_coin_SSC = (uint)(3 + ((m_player_info.Count == 0) ? 0 : new Random().Next() % (((uint)m_player_info.Count * 4) - 3)));
        }
        public void requestSendMasterCoin(Player _session)
        {

            if (m_ri.master == _session.m_pi.uid && m_coin_SSC > 0)
            {

                // Send Coin to Master
                stItem item = new stItem
                {
                    type = 2,
                    id = -1,
                    _typeid = SPECIAL_SHUFFLE_COURSE_COIN_TYPEID,
                    qntd = (int)m_coin_SSC
                };
                item.STDA_C_ITEM_QNTD = (short)item.qntd;

                var rt = ItemManager.RetAddItem.T_INIT_VALUE;

                if ((rt = ItemManager.addItem(item,
                    _session, 0, 0)) < 0)
                {
                    _smp.message_pool.getInstance().push(new message("[SpecialShuffleCourse::requestSendMasterCoiin][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou adicionar SSC coin[TYPEID=" + Convert.ToString(SPECIAL_SHUFFLE_COURSE_COIN_TYPEID) + "] para o master, mas deu erro no ItemManager::addItem. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                    return;
                }

                // Resposta para enviar SSC coin para o master da sala
                var p = new PangyaBinaryWriter((ushort)0x198);

                p.WriteUInt32(SPECIAL_SHUFFLE_COURSE_COIN_TYPEID);
                p.WriteUInt32(m_coin_SSC);

                packet_func.session_send(p,
                    _session, 1);

                if (rt != ItemManager.RetAddItem.T_SUCCESS_PANG_AND_EXP_AND_CP_POUCH)
                {
                    // Update Item ON Game pacoteAA no JP n o att as moedas na hora
                    p.init_plain(0x216);

                    p.WriteUInt32((uint)UtilTime.GetSystemTimeAsUnix());
                    p.WriteUInt32(1u); // Count

                    p.WriteByte(item.type);
                    p.WriteUInt32(item._typeid);
                    p.WriteInt32(item.id);
                    p.WriteUInt32(item.flag);
                    p.WriteBytes(item.stat.ToArray());
                    p.WriteInt32((item.STDA_C_ITEM_TIME > 0) ? item.STDA_C_ITEM_TIME : item.STDA_C_ITEM_QNTD);
                    p.WriteZeroByte(25);
                    packet_func.session_send(p,
                        _session, 1);
                }
            }
        }

        public override bool finish_game(Player _session, int option)
        {
            if (_session.getState()
            && _session.isConnected()
                && m_players.Count > 0)
            {

                var p = new PangyaBinaryWriter();

                if (option == 6)
                {

                    if (m_SSC_state)
                    {
                        finish_SSC(_session, 1); // Termina sem ter acabado de jogar
                    }

                    var pgi = INIT_PLAYER_INFO("finish_game",
                        "tentou terminar o jogo",
                        _session);

                    // N o terminou o Jogo a tempo, add as tacadas dos outros holes que ele nao conseguiu terminar
                    // ------ O Original n o soma as tacadas do resto dos holes que o player n o jogou, quando o tempo acaba -------
                    //if (pgi->type == PlayerGameInfo::eFLAG_GAME::END_GAME)
                    //pgi->ui.tacada = pgi->data.total_tacada_num;

                    requestSaveInfo(_session, 4);

                    requestSendMasterCoin(_session);

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

                    // Resposta que tem sempre que acaba um jogo, não sei o que é ainda, esse só não tem no HIO Event
                    p.init_plain(0x244);

                    p.WriteUInt32(0); // OK

                    packet_func.session_send(p,
                        _session, 1);

                    // Esse é novo do JP, tem Tourney, VS, Grand Prix, HIO Event, não vi talvez tenha nos outros também
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

            return (PlayersCompleteGameAndClear() && m_SSC_state);
        }
    }
}