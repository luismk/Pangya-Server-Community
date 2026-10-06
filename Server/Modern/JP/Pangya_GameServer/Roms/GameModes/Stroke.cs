using Pangya_GameServer.Feature;
using Pangya_GameServer.Models.Game;
using Pangya_GameServer.PacketFunc;
using Pangya_GameServer.Repository;
using Pangya_GameServer.Roms.GameBase.Modes;
using Pangya_GameServer.Session;
using PangyaAPI.DataBase;
using PangyaAPI.Network;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using snmdb;

namespace Pangya_GameServer.Roms.GameModes
{
    public class Stroke : StrokeBase
    {
        private bool _initStrokeState;

        public Stroke(List<Player> players, GameRoomInfoModel roomInfo, RateValue rateValue) : base(players, roomInfo, rateValue)
        {
           
            // Aqui tem que inicializar os players info
            UpdateTreasureHunterSystem();

            // Aqui tem que inicializar os players info
            InitAllPlayerInfo();

            InitAllAchievementPlayers(0x6C40001Du);

            // Last 5 Players Play, tem que salvar no server e no DB, and Achievement
            foreach (var el in Players)
            { 
                foreach (var el2 in Players)
                {
                    if (el.Inventory.uid != el2.Inventory.uid)
                    {
                        el.UserInfo.GameHistory.Add(el2.UserInfo, el.UserInfo.Member.Gender);
                    }
                }

                // Update ON DB
                NormalManagerDB.Instance.add(1, new CmdUpdateLastPlayerGame(el.Inventory.uid, el.UserInfo.GameHistory), DBResponse);
            }

            _initStrokeState = InitRoomGame(); 
        }

        public override void ChangeHole()
        {
            SendTreasureHunterPoint();

            if (Players.Count() <= 0 || CheckEndGame(Players.FirstOrDefault()))
                FinishStroke(0);
            else if (Players.Count() > 0)
                // Resposta terminou o hole
                SendFinishHole(); // Terminou
        }

        public override void FinishHole()
        {
            foreach (var el in Players.ToArray())
            {
                RequestFinishHole(el, 0);

                RequestUpdateItemUsedGame(el);
            }
        }
         
        public override bool InitRoomGame()
        {

            try
            {
                base.InitGame();

                if (Players.Count > 0)
                {

                    // variavel que salva a data local do sistema
                    InitGameTime();

                    GameInitState = 1; // Come ou 
                }

                return true;
            }
            catch (Exception ex)
            {
                _smp.LogManager.Instance.push(new AppMessage("[Versus::Versus][Error]: " + ex.StackTrace, type_msg.CL_ONLY_CONSOLE_DEBUG));

                return false;
            }
        }

        public void FinishExpGame()
        {

            if (Players.Count > 0)
            {

                Player session = null;
                float stars = Course.getStar();
                int exp = 0;
                int holeSeq = 0;

                for (var i = 0; i < PlayerOrder.Count; ++i)
                {
                    switch (RoomInfo.HoleCount)
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

                    holeSeq = (int)Course.findHoleSeq(PlayerOrder[i].hole);

                    // Ele est  no primeiro hole e n o acertou ele, s  da experi ncia se ele tiver acertado o hole
                    if (holeSeq == 1 && !PlayerOrder[i].shot_sync.state_shot.display.acerto_hole)
                    {
                        holeSeq = 0;
                    }

                    if ((session = FindSessionByUID(PlayerOrder[i].uid)) != null)
                    {

                        exp = (int)(1 * PlayerOrder.Count * (holeSeq > 0 ? holeSeq : 0) * stars);
                        exp = (int)(exp * TRANSF_SERVER_RATE_VALUE(PlayerOrder[i].used_item.rate.exp) * TRANSF_SERVER_RATE_VALUE(RateValue.exp));
                        exp = (int)((float)exp * (float)(1.0f - (i * 0.1f)));

                        if (PlayerOrder[i].level < 70)
                        {
                            PlayerOrder[i].data.exp = exp;
                        }

                        // Movido para dentro do null-check: session pode ser null se o player
                        // desconectou antes do fim do jogo, causando NullReferenceException.
                        NormalManagerDB.Instance.add(0, new CmdUpdateWebShopPoint(session.Inventory.uid, 150), null, null);
                    }
                    else
                    {
                        _smp.LogManager.Instance.push(new AppMessage($"[GameModeStroke::FinishExpGame][Warning] Normal[UID={PlayerOrder[i].uid}] não encontrado na sessão.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                    }
                }
            }
        }


        public void Finish()
        {

            _initStrokeState = false; // Terminou o Stroke

            GameInitState = 2; // Terminou o jogo

            CalculeRankPlace();

            FinishExpGame();

            DrawTreasureHunterItem();

            foreach (var el in Players)
            {

                InitPlayerInfo("finish",
                    "tentou finalizar os dados do jogador no jogo",
                    el, out PlayerGameInfo pgi);

                if (pgi.flag != PlayerGameInfo.eFLAG_GAME.QUIT)
                {
                    RequestFinishData(el);
                }
            }

        }
        public void RequestFinishData(Player session)
        {

            // Finish Artefact Frozen Flame agora   direto no Finish Item Used Game
            RequestFinishItemUsedGame(session);

            RequestSaveDrop(session);

            RainHoleSeqCount(session); // conta os achievement de Rain em holes consecutivas

            ScoreSeqCount(session); // conta os achievement de back-to-back(2 ou mais score iguais consecutivos) do player

            RainCount(session); // Aqui achievement de rain count


            // Resposta Treasure Hunter Item Draw
            SendTreasureHunterItemDrawGUI(session);

            // Resposta terminou game - Drop Itens
            SendDropItem(session);

            // Resposta terminou game - Placar
            SendPlacar(session);
        }

        public override bool FinishGame(Player session, int option = 0)
        {

            if (session.getState()
                && session.Connected
                && Players.Count > 0)
            {

                InitPlayerInfo("finish_game",
                    "tentou finalizar o jogo",
                    session, out PlayerGameInfo pgi);

                // Terminou o hole, finalizar o hole por ele
                if (pgi.shot_sync.state_shot.display.acerto_hole || pgi.data.giveup.IsTrue())
                {

                    RequestFinishHole(session, 0);

                    RequestUpdateItemUsedGame(session);
                }

                pgi.finish_game = 1;

                if (PlayersCompleteGameAndClear() || option == 2)
                {

                    var p = new Packet();

                    // Verifica se   o primeiro hole e se nem todos terminaram o hole
                    if (Course.findHoleSeq(pgi.hole) == 1
                        && !CheckAllClearHole()
                        && (pgi.progress.hole <= 0 || pgi.progress.finish_hole[pgi.progress.hole - 1] == 0))
                    {

                        foreach (var el in Players)
                        {

                            InitPlayerInfo("finish_game",
                                "tentou finalizar o Stroke",
                                el, out pgi);

                            if (pgi.flag == PlayerGameInfo.eFLAG_GAME.PLAYING)
                            {

                                RequestSaveInfo(el, 2);

                                if (pgi.finish_item_used == 0u)
                                {
                                    RequestFinishItemUsedGame(el);
                                }

                                p.init_plain(0x67);
                                el.Send(p);

                                //pgi.ServerFlag = PlayerGameInfo::eFLAG_GAME::END_GAME;
                                SetGameFlag(pgi, PlayerGameInfo.eFLAG_GAME.END_GAME);
                            }
                        }

                        GameInitState = 2; // Acabou o VS

                        return true;

                    }
                    else
                    {

                        if (_initStrokeState) // Deixa o cliente envia o pacote para finalizar o jogo, depois que ele mostrar os placares
                        {
                            FinishStroke(1);
                        }
                        else
                        {

                            foreach (var el in Players)
                            {

                                InitPlayerInfo("finish_game",
                                    "tentou finalizar o Stroke",
                                    el, out pgi);

                                if (pgi.flag == PlayerGameInfo.eFLAG_GAME.PLAYING)
                                {

                                    RequestSaveRecordCourse(el,
                                        0,
                                        (RoomInfo.HoleCount == 18 && Course.findHoleSeq(pgi.hole) == 18) ? 1 : 0);

                                    RequestSaveInfo(el, 0);

                                    // D  Exp para o Caddie E Mascot Tamb m
                                    if (pgi.data.exp > 0)
                                    { // s  add Experience se for maior que 0

                                        // Add Exp para o player
                                        el.addExp(pgi.data.exp, false);

                                        // D  Exp para o Caddie Equipado
                                        if (el.Inventory.UserEquippedItem.CaddieEquiped != null) // Tem um caddie equipado
                                        {
                                            el.addCaddieExp(pgi.data.exp);
                                        }

                                        // D  Exp para o Mascot Equipado
                                        if (el.Inventory.UserEquippedItem.MascotEquiped != null)
                                        {
                                            el.addMascotExp(pgi.data.exp);
                                        }
                                    }

                                    SendUpdateInfoAndMapStatistics(el, 0);

                                    // Resposta Treasure Hunter Item
                                    RequestSendTreasureHunterItem(el);

                                    // Update Mascot Info ON GAME, se o player estiver com um mascot equipado
                                    if (el.Inventory.UserEquippedItem.MascotEquiped != null)
                                    {

                                        el.Send(Handle_PACKET_RESPONSE.pacote06B(el.Inventory, 8)); 
                                    }

                                    // Achievement Aqui
                                    pgi.sys_achieve.finish_and_update(el);

                                    // Resposta que tem sempre que acaba um jogo, n o sei o que   ainda, esse s  n o tem no HIO Event
                                    p.init_plain(0x244);

                                    p.WriteUInt32(0); // OK
                                    session.Send(p); 
                                    // Esse   novo do JP, tem Tourney, VS, Grand Prix, HIO Event, n o vi talvez tenha nos outros tamb m
                                    p.init_plain(0x24F);

                                    p.WriteUInt32(0); // OK
                                    session.Send(p);


                                    // Resposta Update Pang
                                    p.init_plain(0xC8);

                                    p.WriteUInt64(el.UserInfo.Statistics.pang);

                                    p.WriteUInt64(0Ul);
                                    el.Send(p); 

                                    //pgi.ServerFlag = PlayerGameInfo::eFLAG_GAME::FINISH;
                                    SetGameFlag(pgi, PlayerGameInfo.eFLAG_GAME.FINISH);
                                }
                            }

                            GameInitState = 2; // Acabou o VS

                            return true;
                        }
                    }
                }
            }

            return Players.Count == 0;
        }



        public override void TimeIsOver(object quem)
        {

            // Chama o timeIsOver da classe pai
            base.TimeIsOver(quem);

            if (quem != null)
            {
                // Cast seguro com 'as' — evita InvalidCastException se o objeto
                var pl = quem as Player;
                if (pl != null && pl.Connected)
                {
                    InitPlayerInfo("timeIsOver",
                        "tentou acabar o tempo do turno no jogo",
                        pl, out PlayerGameInfo pgi);

                    pgi.tempo = 1u;

                    if (pgi.bar_space.getState() == 0 && pgi == PlayerTurn)
                    {

                        pgi.tempo = 0;
                        pgi.data.time_out++;
                        if (pgi.data.time_out >= 3)
                        {
                            // 3 Time outs kika o jogado da sala
                            pgi.data.bad_condute = 3; // Kika Player
                        }

                        // Time Out
                        var p = new Packet(0x5C);

                        p.WriteInt32(pgi.oid);
                        SendBroadCast(p);
                        _smp.LogManager.Instance.push(new AppMessage($"[Versus::timeIsOver][Log] Normal[UID={pgi.uid}] Time Out", type_msg.CL_FILE_LOG_AND_CONSOLE));

                    }
                }

            }
            else
            {
                _smp.LogManager.Instance.push(new AppMessage("[Versus::timeIsOver][Warning] time is over executed without _quem, _quem is invalid(null). Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }


        public override bool DeletePlayer(Player session, int option)
        {

            if (session == null)
            {
                throw new exception("[Versus::deletePlayer][Error] tentou deletar um player, mas o seu endereco eh null.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS,
                    50, 0));
            }

            bool ret = false;

            // Evitar deadlock com a thread checkVersusTurn - Bloqueia
            GameStateVersus.@lock();
            try
            {
                var it = Players.FirstOrDefault(c => c == session);

                if (it != null)
                {
                    InitPlayerInfo("deletePlayer",
                        "tentou sair do jogo",
                        session, out PlayerGameInfo pgi);

                    if (GameInitState == 1)
                    {

                        var p = new Packet();

                        // Player Turn Para o tempo dele
                        if (PlayerTurn == pgi)
                        {
                            GameStop();
                        }

                        var sessions = GetSessions(it);

                        RequestFinishItemUsedGame((it)); // Salva itens usados no Tourney

                        RequestSaveInfo(it, (option == 0x800) ? 5 : 1); // Quitou ou tomou DC

                        //pgi.ServerFlag = PlayerGameInfo::eFLAG_GAME::QUIT;
                        SetGameFlag(pgi, PlayerGameInfo.eFLAG_GAME.QUIT);

                        // Resposta Player saiu do Jogo, tira ele do list de score
                        p.init_plain(0x61); 
                        p.WriteInt32(it.ConnectionID);
						sessions.SendBroadCast(p);

						// Resposta Player saiu do jogo MSG
						p.init_plain(0x40); 
                        p.WriteByte(2); // Player Saiu Msg 
                        p.WriteString(it.UserInfo.NickName); 
                        p.WriteUInt16(0); // size Msg, n o precisa de msg o pangya j  manda na opt 2
						sessions.SendBroadCast(p);

						SendUpdateInfoAndMapStatistics(session, -1);

                        ret = CheckNextStepGame(session);

                    }
                    else if (GameInitState == 2 && !pgi.finish_game.IsTrue())
                    {

                        // Acabou
                        RequestSaveInfo((it), 0);
                    }

                    // Deleta o player por give up ou time out, ele conta os achievements dele, tem o counter item 0x6C400004u Normal Game Complete
                    // Envia os achievements para ele para ficar igual ao original
                    if (GameInitState == 1
                        && pgi.data.bad_condute >= 3
                        && (pgi.data.time_out >= 3 || pgi.data.giveup >= 3))
                    {

                        // Achievements
                        RainHoleSeqCount(session); // conta os achievement de Rain em holes consecutivas

                        ScoreSeqCount(session); // conta os achievement de back-to-back(2 ou mais score iguais consecutivos) do player

                        RainCount(session); // Aqui achievement de rain count

                        pgi.sys_achieve.incrementCounter(0x6C400004u);

                        //Achievement Aqui
                        pgi.sys_achieve.finish_and_update(session);

                        // Resposta que tem sempre que acaba um jogo, n o sei o que   ainda, esse s  n o tem no HIO Event
                        var p = new Packet(0x244); 
                        p.WriteUInt32(0); // OK
                        session.Send(p); 

                        // Esse   novo do JP, tem Tourney, VS, Grand Prix, HIO Event, n o vi talvez tenha nos outros tamb m
                        p.init_plain(0x24F); 
                        p.WriteUInt32(0); // OK
                        session.Send(p);
                    }

                    // Delete Player
                    Players.Remove(it);
                }
                else
                {
                    _smp.LogManager.Instance.push(new AppMessage("[Versus::deletePlayer][Warning] player ja foi excluido do game.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[Versus::deletePlayer][Error] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
            finally
            {
                // Garante que o lock é sempre liberado, mesmo em caso de exception
                GameStateVersus.unlock();
            }
            return ret;
        }

        public void DeleteAllPlayer()
        {
            // Percorre de trás para frente
            for (int i = Players.Count - 1; i >= 0; i--)
            {
                var player = Players[i];
                if (player != null)
                {
                    var pgi = GetPlayerInfo(player);
                    if (pgi != null)
                    {
                        DeletePlayer(player, 0);
                    }
                }
            }
        }

        public void FinishStroke(int option)
        {

            if (Players.Count > 0 && GameInitState == 1)
            {

                foreach (var el in Players)
                {

                    InitPlayerInfo("FinishStroke",
                        "tentou terminar o Stroke", el, out PlayerGameInfo pgi);

                    pgi.sys_achieve.incrementCounter(0x6C400004u);

                    CalculePang(el);

                    UpdatePlayerAssist(el);

                    SendFinishMessage(el);
                } 
                Finish();
            }
        }

        private static void DBResponse(int msgId, Pangya_DB pangyaDb, object arg)
        {

            if (arg == null)
            {
                return;
            }

            // Por Hora s  sai, depois fa o outro Type de tratamento se precisar
            if (pangyaDb.getException().getCodeError() != 0)
            {
                _smp.LogManager.Instance.push(new AppMessage("[Versus::SQLDBResponse][Error] " + pangyaDb.getException().getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                return;
            }
            switch (msgId)
            {
                case 1: // Update Last 5 Player Game
                    {
                        var cmd_l5pg = (CmdUpdateLastPlayerGame)(pangyaDb);

                        _smp.LogManager.Instance.push(new AppMessage("[Versus::SQLDBResponse][Log] player[UID=" + cmd_l5pg.getUID() + "] atualizou o Last 5 Player Game dele com sucesso!", type_msg.CL_FILE_LOG_AND_CONSOLE));

                        break;
                    }
                case 0:
                default:
                    break;
            }
        }


        #region DISPOSE 
        public override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _initStrokeState = false;
                // Para o tempo do player Turn
                GameStop();

                // Itera sobre snapshot para evitar InvalidOperationException caso
                // FinishGame dispare callbacks que modifiquem a lista Players.
                foreach (var el in Players.ToList())
                {
                    FinishGame(el);
                }

                DeleteAllPlayer();

                LogDestruction();

            }
            //chamar por ultimo
            base.Dispose(disposing);
        }
        #endregion
    }
}
