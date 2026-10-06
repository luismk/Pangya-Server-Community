using Pangya_GameServer.Engine;
using Pangya_GameServer.Feature;
using Pangya_GameServer.Flags;
using Pangya_GameServer.Manager;
using Pangya_GameServer.Models.Game;
using Pangya_GameServer.Models;
using Pangya_GameServer.PacketFunc;
using Pangya_GameServer.Repository;
using Pangya_GameServer.Roms.GameBase.Modes;
using Pangya_GameServer.Server;
using Pangya_GameServer.Session;
using PangyaAPI.IFF.Flags;
using PangyaAPI.IFF.Handle.JP;
using PangyaAPI.IFF.Regions.JP.Models.IFF;
using PangyaAPI.Network;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using snmdb;

using static Pangya_GameServer.Models.DefineConstants;

namespace Pangya_GameServer.Roms.GameModes
{
    public class Tourney : TourneyBase
    {
        private PangyaSyncTimer _timerAfterToEnter;        // Timer de entrar depois no Tourney

        private bool _tourneyState;
        public Tourney(List<Player> players, GameRoomInfoModel roomInfo, RateValue rateValue) : base(players, roomInfo, rateValue)
        {
            _tourneyState = false;
            _timerAfterToEnter = null;

            UpdateTreasureHunterSystem();

            // Aqui tem que inicializar os players info
            InitAllPlayerInfo();

            InitAllAchievementPlayers(0x6C40001Fu);

            State = InitRoomGame();
        }

        #region REQUEST
        public override bool RequestFinishLoadHole(Player session, Packet packet)
        { 
            // Esse aqui   para Trocar Info da Sala
            // para colocar a sala no HoleMode que pode entrar depois de ter come ado
            bool ret = false;

            try
            {

                // Chama a fun  o base para fazer a parte dela
                ret = base.RequestFinishLoadHole(session, packet);

                // Aqui come a o tempo que os outros player pode entrar se a sala n o for private
                // Come  o o tempo de 5 ou 10min para entra no camp se n o tiver Password
                if (EntraDepoisFlag != 1
                    && RoomInfo.IsPublicRoom == 1
                    && ((byte)(RoomInfo.CourseIndex & RoomCourseFlags.UNK)) != (byte)RoomTypeFlags.SPECIAL_SHUFFLE_COURSE)
                {

                    // S  libera se for Tourney Normal, se for GM Event N o libera
                    if (!(RoomInfo.TrophyID == TROFEL_GM_EVENT_TYPEID && RoomInfo.MaxUsers > 30 && RoomInfo.IsGameMaster == 1 && RoomInfo.SpecialFlag == 0x100))
                    {
                        // Libera Entrar, mesmo depois de ter come ado o Tourney
                        ret = true;
                    }

                    EntraDepoisFlag = 1;
                }

            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[Tourney::requestFinishLoadHole][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            return ret;
        }

        public override bool RequestUseTicketReport(Player session, Packet packet)
        {
            bool ret = false;

            try
            {
                PlayerUserStatistics ui = new PlayerUserStatistics();
                #region Read Packet
                ui.ToRead(packet);
                #endregion
                // aqui o cliente passa mad_conduta com o hole_in, trocados, mad_conduto <-> hole_in

                InitPlayerInfo("requestUseTicketReport",
                    "tentou sair do jogo com ticket report",
                    session, out PlayerGameInfo pgi);

                pgi.ui = ui;

                // Verifica se ele acabou todo o Tourney
                if (pgi.flag != PlayerGameInfo.eFLAG_GAME.FINISH)
                {
                    throw new exception("[Tourney::requestUseTicketReport][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou sair do jogo na sala[NUMERO=" + Convert.ToString(RoomInfo.RoomID) + "] com Ticket Report, mas ele ainda nao terminou o Tourney[FLAG=" + Convert.ToString((ushort)pgi.flag) + "]", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY,
                        403, 0));
                }

                // Verifica se o Level do player   maior ou igual a Beginner E
                if (session.UserInfo.Level < (byte)enLEVEL.BEGINNER_E)
                {
                    throw new exception("[Tourney::requestUseTicketReport][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + ", LEVEL=" + Convert.ToString(session.UserInfo.Level) + "] tentou sair do jogo na sala[NUMERO=" + Convert.ToString(RoomInfo.RoomID) + "] com Ticket Report, mas ele nao tem o Level necessario[6=BEGINNER E] para usar o Ticket Report.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY,
                        405, 0));
                }

                // Verifica se o player tem o ticket report
                var pWi = session.Inventory.FindWarehouseItemByTypeid(TICKET_REPORT_TYPEID);

                // N o tem o item ou n o tem a quantidade "  a mesma coisa, s  estou fazendo isso pra previnir bugs"
                if (pWi == null || pWi.STDA_C_ITEM_QNTD < 1)
                {
                    throw new exception("[Tourney::requestUseTicketReport][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou sair do jogo na sala[NUMERO=" + Convert.ToString(RoomInfo.RoomID) + "] com Ticket Report, mas ele nao tem o item[TYPEID=" + Convert.ToString(TICKET_REPORT_TYPEID) + "]", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY,
                        400, 0));
                }

                // Tira um Ticket Report dele
                stItem item = new stItem();

                item.type = 2;
                item.id = (int)pWi.id;
                item._typeid = pWi._typeid;
                item.qntd = 1;
                item.STDA_C_ITEM_QNTD = (short)((short)item.qntd * -1);

                if (ItemManager.removeItem(item, session) <= 0)
                {
                    throw new exception("[Tourney::requestUseTicketReport][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou sair do jogo na sala[NUMERO=" + Convert.ToString(RoomInfo.RoomID) + "] com Ticket Report, mas nao conseguiu deletar um Ticket Report Item do player.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY,
                        401, 0));
                }

                var v_item = new List<stItem>() { item };

                // Log
                _smp.LogManager.Instance.push(new AppMessage("[Tourney::requestUseTicketReport][Log] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] sai do Tourney na sala[NUMERO=" + Convert.ToString(RoomInfo.RoomID) + ", MASTER=" + Convert.ToString(RoomInfo.OwnerUID) + "] com ticket report.", type_msg.CL_FILE_LOG_AND_CONSOLE));

				// Respota para garantir que excluiu o ticket report mesmo do player 
				session.Send(Handle_PACKET_RESPONSE.pacote0AA(session, v_item));
				// Saiu com Ticket Report
				SetGameFlag(pgi, PlayerGameInfo.eFLAG_GAME.TICKET_REPORT);

                RainHoleSeqCount(session); // conta os achievement de Rain em holes consecutivas

                ScoreSeqCount(session); // conta os achievement de back-to-back(2 ou mais score iguais consecutivos) do player

                RainCount(session); // Aqui achievement de rain count

                FinishGame(session, 1);

                ret = true; // Confirma o sai da sala

            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[Tourney::requestUseTicketReport][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Reposta de erro aqui, tenho que arranjar um pacote para isso
            }

            return ret;
        }

        public override void RequestCalculeShotSpinningCube(Player session, ShotSyncData ssd)
        { 
            try
            {

                // S  calcula se n o for short game
                if (!(RoomInfo.SpecialModeRoom.IsShotMode))
                {
                    CalculeShotToSpinningCube(session, ssd);
                }

            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[Tourney::requestCalculeShotSpinningCube][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void RequestCalculeShotCoin(Player session, ShotSyncData ssd)
        {
            try
            {

                // S  calcula se n o for short game
                if (!(RoomInfo.SpecialModeRoom.IsShotMode))
                {
                    CalculeShotToCoin(session, ssd);
                }

            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[Tourney::requestCalculeShotCoin][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        #endregion

        #region HELPER
        public override bool DeletePlayer(Player session, int option)
        {

            if (session == null)
            {
                throw new exception("[Tourney::DeletePlayer][Error] tentou deletar um player, mas o seu endereco é null.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY,
                    50, 0));
            }

            bool ret = false;

            try
            {
                var it = Players.FirstOrDefault(c => c == session);

                if (it != null)
                {
                    byte opt = 3; // Saiu Quitou

                    if (GameInitState == 1)
                    {

                        var p = new Packet();

                        InitPlayerInfo("deletePlayer",
                            "tentou sair do jogo",
                            session, out PlayerGameInfo pgi);

                        var sessions = GetSessions(it);

                        if (pgi.flag != PlayerGameInfo.eFLAG_GAME.TICKET_REPORT)
                        {

                            RequestFinishItemUsedGame(it); // Salva itens usados no Tourney

                            RequestSaveInfo(it, (option == 0x800) ? 5 : 1); // Quitou ou tomou DC

                            //pgi->type = PlayerGameInfo::eFLAG_GAME::QUIT;
                            SetGameFlag(pgi, PlayerGameInfo.eFLAG_GAME.QUIT);

                            // Resposta Player saiu do Jogo, tira ele do list de score
                            p.init_plain(0x61);

                            p.WriteInt32(it.ConnectionID);

                            foreach (var s in sessions.ToArray())
                            {
                                s.Send(p);
                            }


                            // Resposta Player saiu do jogo
                            SendUpdateState(session, opt);

                            // Salva Achievement do player

                            if (AllCompleteGameAndClear())
                            {
                                ret = true; // Termina o Tourney
                            }

                        }
                        else if (option == 10)
                        { // Ticket Reporting
                            opt = 1; // Ticket Reporting

                            // Resposta Player saiu do Jogo, tira ele do list de score
                            p.init_plain(0x61);

                            p.WriteInt32(it.ConnectionID);
                            foreach (var s in sessions.ToArray())
                            {
                                s.Send(p);
                            }

                            // Resposta Player saiu com ticket reporting do jogo
                            p.init_plain(0x11B);

                            p.WriteInt32(it.ConnectionID);
                            foreach (var s in sessions.ToArray())
                            {
                                s.Send(p);
                            }
                        }

                        if (opt != 1) // !Ticket Report
                        {
                            SendUpdateInfoAndMapStatistics(session, -1);
                        }
                    }

                    // Delete Player
                    Players.Remove(it);
                }
                else
                {
                    _smp.LogManager.Instance.push(new AppMessage("[Tourney::deletePlayer][Warning] player ja foi excluido do game.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                }

            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[Tourney::deletePlayer][Error] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

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

        public override void ChangeHole(Player session)
        {

            UpdateTreasureHunterPoint(session);

            if (CheckEndGame(session))
            {
                FinishTourney(session, 0);
            }
            else
            {
                // Resposta terminou o hole
                UpdateFinishHole(session, 1); // Terminou
            }
        }

        public override void FinishHole(Player session)
        {
            InitPlayerInfo("finishHole",
                    "tentou finalizar o hole do jogo",
                    session, out PlayerGameInfo pgi);
            if (pgi.shot_sync.state_shot.display.acerto_hole || pgi.data.giveup == 1)
            {
                RequestFinishHole(session, 0);

                RequestUpdateItemUsedGame(session);
            }
        }


        public void FinishTourney(Player session, int option)
        {

            if (Players.Count() > 0 && GameInitState == 1)
            {

                InitPlayerInfo("finish_tourney",
                    "tentou terminar o Tourney no jogo",
                    session, out PlayerGameInfo pgi);

                if (pgi.flag == PlayerGameInfo.eFLAG_GAME.PLAYING)
                {

                    // Calcula os pangs que o player ganhou
                    CalculePang(session);

                    // Atualizar os Pang do player se ele estiver com assist ligado, e for maior que beginner E
                    UpdatePlayerAssist(session);

                    if (GameInitState == 1 && option == 0)
                    {

                        // Mostra msg que o player terminou o jogo
                        SendFinishMessage(session);

                        // Resposta terminou o hole
                        UpdateFinishHole(session, 1);

                        // Resposta Terminou o Jogo, ou Saiu
                        SendUpdateState(session, 2);

                        // Achievement Counter
                        pgi.sys_achieve.incrementCounter(0x6C400004u);

                    }
                    else if (GameInitState == 1 && option == 1)
                    { // Acabou o Tempo

                        RequestFinishHole(session, 1); // Acabou o Tempo 

                        // Mostra msg que o player terminou o jogo
                        SendFinishMessage(session);

                        // Resposta terminou o hole
                        UpdateFinishHole(session, 0);

                        // Resposta para acabou o tempo do Tourney
                        SendTimeIsOver(session);
                    }
                }

                //pgi->type = (option == 0) ? PlayerGameInfo::eFLAG_GAME::FINISH : PlayerGameInfo::eFLAG_GAME::END_GAME;
                SetGameFlag(pgi, (option == 0) ? PlayerGameInfo.eFLAG_GAME.FINISH : PlayerGameInfo.eFLAG_GAME.END_GAME);

                pgi.time_finish.CreateTime();

                if (AllCompleteGameAndClear() && GameInitState == 1)
                {
                    Finish(); // Envia os pacotes que termina o jogo Ex: 0xCE, 0x79 e etc
                }
            }
        }

        public override bool InitRoomGame()
        {

            if (Players.Count() > 0)
            {

                // Cria o timer do Tourney
                GameStartTime();

                // variavel que salva a data local do sistema
                InitGameTime();

                GameInitState = 1; // Come ou

                _tourneyState = true;
            }

            return true;
        }


        public void FinishExpGame()
        {

            // Bug Fix, ultimo player do camp sai e ou toma dc e n o fica ningu m na sala e calcula a Experience do camp
            if (GetCountPlayersGame() > 0)
            {

                Player session = null;
                float stars = Course.getStar();
                int exp = 0;
                int holeSeq = 0;


                for (var i = 0; i < PlayerOrder.Count(); ++i)
                {

                    // Exp padr�o de hole do Grand Prix
                    switch (RoomInfo.HoleCount)
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

                    holeSeq = (int)Course.findHoleSeq(PlayerOrder[i].hole);

                    // Ele est  no primeiro hole e n o acertou ele, s  da experi ncia se ele tiver acertado o hole
                    if (holeSeq == 1 && !(PlayerOrder[i].shot_sync.state_shot.display.acerto_hole))
                    {
                        holeSeq = 0;
                    }

                    if (PlayerOrder[i].flag == PlayerGameInfo.eFLAG_GAME.FINISH)
                    {

                        if ((session = FindSessionByUID(PlayerOrder[i].uid)) != null)
                        {

                            exp = (int)(1 * PlayerOrder.Count() * (holeSeq > 0 ? holeSeq : 0) * stars);
                            exp = (int)(exp * TRANSF_SERVER_RATE_VALUE(PlayerOrder[i].used_item.rate.exp) * TRANSF_SERVER_RATE_VALUE(RateValue.exp));
                            exp = (int)(exp * (1 - (i / PlayerInfo.Count())));

                            if (PlayerOrder[i].level < 70)
                            {
                                PlayerOrder[i].data.exp = exp;
                            }

                            // DB call movido para dentro do null-check: se session for null
                            // (player desconectou), session.PlayerUserStatistics.UID lança NullReferenceException.
                            NormalManagerDB.Instance.add(0, new CmdUpdateWebShopPoint(session.UserInfo.UID, 60), null, null);
                        }
                        else
                        {
                            _smp.LogManager.Instance.push(new AppMessage($"[Tourney::FinishExpGame][Warning] Normal[UID={PlayerOrder[i].uid}] não encontrado na sessão — WebShopPoint não atualizado.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                        }

                    }
                    else if (PlayerOrder[i].flag == PlayerGameInfo.eFLAG_GAME.TICKET_REPORT)
                    {
                        exp = (int)(1 * PlayerOrder.Count() * (holeSeq > 0 ? holeSeq : 0) * stars);
                        exp = (int)(exp * TRANSF_SERVER_RATE_VALUE(PlayerOrder[i].used_item.rate.exp) * TRANSF_SERVER_RATE_VALUE(RateValue.exp));
                        exp = (int)(exp * (1 - (i / PlayerInfo.Count())));

                        PlayerOrder[i].data.exp = exp;

                        // Ticket Report: não tem sessão ativa — usa UID direto se disponível
                    }
                    else if (PlayerOrder[i].flag == PlayerGameInfo.eFLAG_GAME.END_GAME)
                    {

                        if ((session = FindSessionByUID(PlayerOrder[i].uid)) != null)
                        {

                            exp = (int)(1 * PlayerOrder.Count() * (holeSeq > 0 ? holeSeq : 0) * stars);
                            exp = (int)(exp * TRANSF_SERVER_RATE_VALUE(PlayerOrder[i].used_item.rate.exp) * TRANSF_SERVER_RATE_VALUE(RateValue.exp));
                            exp = (int)(exp * (1 - (i / PlayerInfo.Count())));

                            if (PlayerOrder[i].level < 70)
                            {
                                PlayerOrder[i].data.exp = exp;
                            }

                            NormalManagerDB.Instance.add(0, new CmdUpdateWebShopPoint(session.UserInfo.UID, 60), null, null);
                        }
                        else
                        {
                            _smp.LogManager.Instance.push(new AppMessage($"[Tourney::FinishExpGame][Warning] Normal[UID={PlayerOrder[i].uid}] END_GAME não encontrado na sessão — WebShopPoint não atualizado.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                        }
                    }
                }
            }
        }

        public void Finish()
        {

            GameInitState = 2; // Acabou

            CalculeRankPlace();

            MakeMedal();

            MakeTrofel();

            FinishExpGame();

            SaveTicketReport();

            SendTicketReport();

            GiveMedalAndItens();

            // [O pangya original, quando o player sai com ticket report do Tourney,
            // mesmo que ele fique entre os 3 primeiros no short game n o conta o achievement de short game top 3 RankPosition]

            // ToList() garante snapshot: FinishData pode disparar callbacks que
            // modificam Players, o que causaria InvalidOperationException no foreach.
            foreach (var el in Players.ToList())
            {

                InitPlayerInfo("finish",
                    "tentou finalizar os dados do jogador no jogo",
                    el, out PlayerGameInfo pgi);

                if (pgi.flag != PlayerGameInfo.eFLAG_GAME.QUIT)
                {
                    FinishData(el);
                }
            }
        }


        public void MakeMedal()
        {

            int all_player = GetCountPlayersGame();

            // Medalhas s  s o liberadas apartir de 18 players no jogo
            if (all_player >= 18)
            {

                List<PlayerGameInfo> v_all_player = new List<PlayerGameInfo>();

                LotterySystem lottery = new LotterySystem();
                LotterySystem lot_active_item = new LotterySystem();

                LotterySystem.LotteryCtx ctx_lot = null;
                PlayerGameInfo pgi = null;

                // Active Item que o player ganha
                for (var i = 0; i < 15u; ++i)
                {
                    lot_active_item.Add(200, (sIff.Instance.ITEM << 26) + i);
                }

                // Preenche vector, e alimenta o lottery
                foreach (var el in PlayerInfo)
                {
                    if (el.Value.flag != PlayerGameInfo.eFLAG_GAME.QUIT)
                    {

                        v_all_player.Add(el.Value);

                        lottery.Add(200, el.Value);
                    }
                }

                // 1 Medalha da sorte
                var ctx = lottery.SpinRoleta();

                if (ctx == null)
                {
                    _smp.LogManager.Instance.push(new AppMessage("[Tourney::requestMakeMedal][Error] nao conseguiu sortear um player para ganha a medalha da sorte", type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
                else
                {
                    pgi = ((PlayerGameInfo)ctx.Value);

                    Medals[0].oid = (int)pgi.oid;

                    if ((ctx_lot = lot_active_item.SpinRoleta()) == null)
                    {
                        _smp.LogManager.Instance.push(new AppMessage("[Tourney::requestMakeMedal][Error] nao conseguiu sortear um State commun item da medalha da sorte", type_msg.CL_FILE_LOG_AND_CONSOLE));
                    }
                    else
                    {
                        Medals[0].item_typeid = (uint)ctx_lot.Value;
                    }

                    pgi.medal_win.stMedal.lucky = 1;
                }

                // 2 Medalha Mais r pido
                v_all_player.Sort(SpeedSort);

                pgi = v_all_player[0];

                Medals[1].oid = (int)pgi.oid;

                if ((ctx_lot = lot_active_item.SpinRoleta()) == null)
                {
                    _smp.LogManager.Instance.push(new AppMessage("[Tourney::requestMakeMedal][Error] nao conseguiu sortear um State commun item da medalha de speediest", type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
                else
                {
                    Medals[1].item_typeid = (uint)ctx_lot.Value;
                }

                pgi.medal_win.stMedal.speediest = 1;

                // 3 Medalha Melhor drive (Dist ncia tacada) 
                v_all_player.Sort(BestDriveSort);

                pgi = v_all_player[0];

                Medals[2].oid = (int)pgi.oid;

                if ((ctx_lot = lot_active_item.SpinRoleta()) == null)
                {
                    _smp.LogManager.Instance.push(new AppMessage("[Tourney::requestMakeMedal][Error] nao conseguiu sortear um State commun item da medalha de best drive", type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
                else
                {
                    Medals[2].item_typeid = (uint)ctx_lot.Value;
                }

                pgi.medal_win.stMedal.best_drive = 1;

                // 4 Melha Melhor Chip-in 
                v_all_player.Sort(BestChipInSort);

                pgi = v_all_player[0];

                Medals[3].oid = (int)pgi.oid;

                if ((ctx_lot = lot_active_item.SpinRoleta()) == null)
                {
                    _smp.LogManager.Instance.push(new AppMessage("[Tourney::requestMakeMedal][Error] nao conseguiu sortear um State commun item da medalha de best chipin", type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
                else
                {
                    Medals[3].item_typeid = (uint)ctx_lot.Value;
                }

                pgi.medal_win.stMedal.best_chipin = 1;

                // 5 Medalha Melhor Long Puttin 
                v_all_player.Sort(BestLongPuttinSort);

                pgi = v_all_player[0];

                Medals[4].oid = (int)pgi.oid;

                if ((ctx_lot = lot_active_item.SpinRoleta()) == null)
                {
                    _smp.LogManager.Instance.push(new AppMessage("[Tourney::requestMakeMedal][Error] nao conseguiu sortear um State commun item da medalha de best long puttin", type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
                else
                {
                    Medals[4].item_typeid = (uint)ctx_lot.Value;
                }

                pgi.medal_win.stMedal.best_long_puttin = 1;

                // 6 Medalha Melhor Recupera  o (S  da se for 18h)
                if (RoomInfo.HoleCount == 18)
                {
                    v_all_player.Sort(BestRecoverySort);

                    pgi = v_all_player[0];

                    Medals[5].oid = (int)pgi.oid;

                    if ((ctx_lot = lot_active_item.SpinRoleta()) == null)
                    {
                        _smp.LogManager.Instance.push(new AppMessage("[Tourney::requestMakeMedal][Error] nao conseguiu sortear um State commun item da medalha de best recovery", type_msg.CL_FILE_LOG_AND_CONSOLE));
                    }
                    else
                    {
                        Medals[5].item_typeid = (uint)ctx_lot.Value;
                    }

                    pgi.medal_win.stMedal.best_recovery = 1;
                }

            }
        }

        public void MakeTrofel()
        {

            int all_player = GetCountPlayersGame();

            int countTrofel = 0;
            int i = 0;

            if (PlayerOrder.Count() <= 0)
            {
                CalculeRankPlace();
            }

            if (PlayerOrder.Count() != all_player)
            {

                _smp.LogManager.Instance.push(new AppMessage("[Tourney::requestMakeTrofel][Error] nao conseguiu gerar os trofeus por que o vector de player RankPosition order nao bate com o dos players no jogo", type_msg.CL_FILE_LOG_AND_CONSOLE));

                return;
            }

            LotterySystem lottery = new LotterySystem();
            LotterySystem.LotteryCtx ctx = null;

            // Active Cummon Item
            for (i = 0; i < 15u; ++i)
            {
                lottery.Add(200, (sIff.Instance.ITEM << 26) + i);
            }

            if (RoomInfo.HoleCount == 18 && all_player >= 10)
            {
                // --- 18 Holes Tourney ----
                // 10-14 = 1 bronze
                // 15-18 = 1 silver e 1 bronze
                // 19-22 = 1 gold, 1 silver e 1 bronze
                // 23-26 = 1 gold, 1 silver e 2 bronze
                // 27-30 = 1 gold, 2 silver e 3 bronze

                if (all_player <= 14u)
                {
                    countTrofel = 1;
                }
                else if (all_player <= 18u)
                {
                    countTrofel = 2;
                }
                else if (all_player <= 22u)
                {
                    countTrofel = 3;
                }
                else if (all_player <= 26u)
                {
                    countTrofel = 4;
                }
                else if (all_player <= 30u)
                {
                    countTrofel = 6;
                }

            }
            else if (RoomInfo.HoleCount == 9 && all_player >= 15)
            {
                // --- 9 Holes Tourney ---
                // 15-18 = 1 bronze
                // 19-26 = 1 silver e 1 bronze
                // 27-30 = 1 gold, 1 silver e 1 bronze

                if (all_player <= 18u)
                {
                    countTrofel = 1;
                }
                else if (all_player <= 26u)
                {
                    countTrofel = 2;
                }
                else if (all_player <= 30u)
                {
                    countTrofel = 3;
                }
            }

            // Novo item commun e troféu
            List<PlayerGameInfo> trofeus = new List<PlayerGameInfo>();

            foreach (var el in PlayerInfo)
            {
                if (el.Value.flag != PlayerGameInfo.eFLAG_GAME.QUIT && (el.Key != null && el.Key.UserInfo.Statistics.getQuitRate() < QUITER_ICON_2)) // menos os que quitaram e os QUITER_ICON_2
                {
                    trofeus.Add(el.Value);
                }
            }

            // sort 
            trofeus.Sort(base.SortPlayerRank);

            // give trofeus
            if (trofeus.Count > 0)
            {

                if (trofeus.Count < countTrofel)
                {

                    for (i = 6; i < (countTrofel + 6u) && i < (trofeus.Count + 6u); ++i)
                    {

                        Medals[i].oid = (int)trofeus[i - 6].oid;

                        if ((ctx = lottery.SpinRoleta()) == null)
                        {
                            _smp.LogManager.Instance.push(new AppMessage("[Tourney::requestMakeTrofel][Error] nao conseguiu sortear um State commun item do TrophyID", type_msg.CL_FILE_LOG_AND_CONSOLE));
                        }
                        else
                        {
                            Medals[i].item_typeid = (uint)ctx.Value;
                        }
                    }

                    // S  da os trofeus se n o for Evento GM
                    if (!(RoomInfo.TrophyID == TROFEL_GM_EVENT_TYPEID && RoomInfo.IsGameMaster == 1 && RoomInfo.MaxUsers > 30 && RoomInfo.SpecialFlag == 0x100))
                    {

                        switch (countTrofel)
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

                    for (i = 6; i < (countTrofel + 6u); ++i)
                    {

                        Medals[i].oid = (int)trofeus[i - 6].oid;

                        if ((ctx = lottery.SpinRoleta()) == null)
                        {
                            _smp.LogManager.Instance.push(new AppMessage("[Tourney::requestMakeTrofel][Error] nao conseguiu sortear um State commun item do TrophyID", type_msg.CL_FILE_LOG_AND_CONSOLE));
                        }
                        else
                        {
                            Medals[i].item_typeid = (uint)ctx.Value;
                        }
                    }

                    // S  da os trofeus se n o for Evento GM
                    if (!(RoomInfo.TrophyID == TROFEL_GM_EVENT_TYPEID && RoomInfo.IsGameMaster == 1 && RoomInfo.MaxUsers > 30 && RoomInfo.SpecialFlag == 0x100))
                    {

                        switch (countTrofel)
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

        public void SaveTicketReport()
        {

            // Adiciona o Ticket Report do Tourney
            CmdInsertTicketReport cmd_itr = new CmdInsertTicketReport(RoomInfo.TrophyID, 4);

            snmdb.NormalManagerDB.Instance.add(0, cmd_itr, null, null);

            // Nota: não verificamos getException() imediatamente pois o DB é assíncrono.
            // A verificação de erro deve ser feita no callback, não aqui.

            TicketReport.clear();

            TicketReport.id = cmd_itr.getId();

            foreach (var el in PlayerInfo)
            { 
             var trd = new TicketReportInfo.stTicketReportDados();

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

                snmdb.NormalManagerDB.Instance.add(1, new CmdInsertTicketReportData(TicketReport.id, trd), Tourney.OnDatabaseResponse, this);

                TicketReport.v_dados.Add(trd);
            }
        }

        public void SendTicketReport()
        {

            if (TicketReport.id != -1)
            { // Tem Ticket Report o Tourney

                stItem item = new stItem();
                Player session = null;
                var p = new Packet();

                item.type = 2;
                item._typeid = TICKET_REPORT_SCROLL_TYPEID;
                item.qntd = 1;
                item.STDA_C_ITEM_QNTD = (short)item.qntd;

                // Ticket Report ID
                item.c[1] = (short)(TicketReport.id / 0x8000);
                item.c[2] = (short)(TicketReport.id % 0x8000);

                item.flag = 0x20;
                item.flag_time = 0x20; // Horas
                item.STDA_C_ITEM_TIME = 24; // 24 Horas

                foreach (var el in PlayerInfo)
                {

                    // S  para os que sairam com Ticket Report
                    if (el.Value.flag == PlayerGameInfo.eFLAG_GAME.TICKET_REPORT)
                    {

                        item.id = -1;

                        // Envia para os players online
                        //if (sgs::gs != null) {
                        if ((session = GameServer.Instance.FindPlayer(el.Value.uid)) != null)
                        {

                            // Add Ticket Report Item
                            var rt = RetAddItem.INIT_VALUE;

                            if ((rt = ItemManager.addItem(item,
                                session, 0, 0)) < 0)
                            {
                                throw new exception("[Tourney::requestSendTicketReport][Error] Normal[UID=" + Convert.ToString(el.Value.uid) + "], nao conseguiu adicionar o Ticket Report Item[TYPEID=" + Convert.ToString(item._typeid) + "] para o player.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY,
                                    1, 0));
                            }

                            // Reposta para o Ticket Report Treasure Hunter Item(ns)
                            p.init_plain(0x11C);
                            p.WriteByte(1); // OK 
                            session.Send(p);
                            // Update PlayerUserStatistics, TrofelInfo e MapStatistics
                            SendUpdateInfoAndMapStatistics(session, 0);

                            if (rt != RetAddItem.SUCCESS_PANG_AND_EXP_AND_CP_POUCH)
                            {

                                var v_item = new List<stItem>() { item };
                                // Add Ticket Report Item 
                                session.Send(Handle_PACKET_RESPONSE.pacote0AA(session, v_item));
                            }

                        }
                        else
                        { // Player Est  OFFLINE

                            var rt = RetAddItem.INIT_VALUE;

                            if ((rt = ItemManager.addItem(item,
                                el.Value.uid, 0, 0)) < 0)
                            {
                                _smp.LogManager.Instance.push(new AppMessage("[Tourney::requestSendTicketReport][Error] Normal[UID=" + Convert.ToString(el.Value.uid) + "] nao conseguiu adicionar o Ticket Report item[TYPEID=" + Convert.ToString(item._typeid) + "] para o player", type_msg.CL_FILE_LOG_AND_CONSOLE));
                            }

                        }
                    }
                }
            }
        }

        public void GiveMedalAndItens()
        {

            Dictionary<PlayerGameInfo, List<stItem>> map_item = new Dictionary<PlayerGameInfo, List<stItem>>();

            List<PlayerGameInfo> all_player = new List<PlayerGameInfo>();
            List<stItem> v_item = new List<stItem>();
            // Preenche vector, e alimenta o lottery
            foreach (var el in PlayerInfo)
            {
                if (el.Value.flag != PlayerGameInfo.eFLAG_GAME.QUIT)
                    all_player.Add(el.Value);
            }

            for (int i = 0; i < Medals.Length; ++i)
            {
                if (Medals[i].oid != -1)
                {
                    v_item.Clear();

                    // Item de premiação
                    var item = new stItem();
                    item.type = 2;
                    item.id = -1;
                    item._typeid = Medals[i].item_typeid;
                    item.qntd = 1;
                    item.STDA_C_ITEM_QNTD = (short)item.qntd;

                    v_item.Add(item);

                    // Medalha (índices 0 a 5)
                    if (i < 6)
                    {
                        item = new stItem();
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
                    PlayerGameInfo player = all_player.FirstOrDefault(pl => pl.oid == Medals[i].oid);

                    if (player == null)
                    {
                        _smp.LogManager.Instance.push(new AppMessage("[Tourney::requestGiveMedalAndItens][Error] player_info[OID=" + Convert.ToString(Medals[i].oid) + "] nao tem nos player_all que ficaram no camp ou saiu com ticket report.", type_msg.CL_FILE_LOG_AND_CONSOLE));
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
            Player session = null;

            var p = new Packet();

            foreach (var el in map_item)
            {
                // Itens
                if (el.Value.Count > 0)
                {

                    // Player Online
                    if ((session = GameServer.Instance.FindPlayer(el.Key.uid)) != null)
                    {

                        var rai = ItemManager.addItem(el.Value,
                            session.GetUID(), 0, 0);

                        if (rai.fails.Count() > 0 && rai.type != RetAddItem.SUCCESS_PANG_AND_EXP_AND_CP_POUCH)
                        {
                            _smp.LogManager.Instance.push(new AppMessage("[Tourney::requestGiveMedalAndItens][Error] Normal[UID=" + Convert.ToString(el.Key.uid) + "] nao conseguiu adicionar os itens que ele ganhou com medalhas e trofeus.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                        }

                        // Resposta Add Item para o player
                        if (el.Value.Count() > 0)
                        {

                            session.Send(Handle_PACKET_RESPONSE.pacote0AA(session, el.Value));
                        }

                    }
                    else
                    { // Player Offline
                        var rai = ItemManager.addItem(el.Value,
                            el.Key.uid, 0, 0);

                        if (rai.fails.Count() > 0 && rai.type != RetAddItem.SUCCESS_PANG_AND_EXP_AND_CP_POUCH)
                        {
                            _smp.LogManager.Instance.push(new AppMessage("[Tourney::requestGiveMedalAndItens][Error] Normal[UID=" + Convert.ToString(el.Key.uid) + "] nao conseguiu adicionar os itens que ele ganhou com medalhas e trofeus.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                        }
                    }
                }

                // Trofeus
                if (RoomInfo.TrophyID != 0 && el.Key.trofel != 0)
                {

                    // Player Online
                    if ((session = GameServer.Instance.FindPlayer(el.Key.uid)) != null)
                    {

                        session.Inventory.updateTrofelInfo(RoomInfo.TrophyID, el.Key.trofel);

                        // Update Tofel do player no jogo
                        SendUpdateInfoAndMapStatistics(session, 0);

                    }
                    else // Player Offline
                    {
                        InventoryInfo.updateTrofelInfo(el.Key.uid, RoomInfo.TrophyID, el.Key.trofel);
                    }
                }

                // Madelhas - Ganhou medalhas
                if (el.Key.medal_win.ucMedal != 0u)
                {

                    // Player Online
                    if ((session = GameServer.Instance.FindPlayer(el.Key.uid)) != null)
                    {

                        // Update Medal do player
                        session.UserInfo.updateMedal(el.Key.medal_win);

                        // Update Medal do player no jogo
                        SendUpdateInfoAndMapStatistics(session, 0);

                    }
                    else
                    { // Player Offline

                        // Update Medal do player - find the Player GameKey from PlayerInfo dictionary
                        var playerKey = PlayerInfo.Keys.FirstOrDefault(p => PlayerInfo[p].uid == el.Key.uid);
                        if (playerKey != null && PlayerInfo.TryGetValue(playerKey, out var playerGameInfo))
                        {
                            playerGameInfo.medal_win = el.Key.medal_win;
                        }

                    }
                }
            }
        }

        public void FinishData(Player session)
        {

            // Finish Artefact Frozen Flame agora   direto no Finish Item Used Game
            RequestFinishItemUsedGame(session);

            RequestSaveDrop(session);

            // Tourney GM n o tem Treasure Hunter Item
            if (!(RoomInfo.TrophyID == TROFEL_GM_EVENT_TYPEID && RoomInfo.MaxUsers > 30 && RoomInfo.IsGameMaster == 1 && RoomInfo.SpecialFlag == 0x100))
            {
                RequestDrawTreasureHunterItem(session);
            }

            RainHoleSeqCount(session); // conta os achievement de Rain em holes consecutivas

            ScoreSeqCount(session); // conta os achievement de back-to-back(2 ou mais score iguais consecutivos) do player

            RainCount(session); // Aqui achievement de rain count

            AchievementTop3_1st(session); // Se o Player ficou em Top 3 add +1 ao contador de top 3, e se ele ficou em primeiro add +1 ao do primeiro

            //InitPlayerInfo("requestFinishData", "tentou finalizar dados do jogo", &session);

            // Resposta terminou game - Drop Itens
            SendDropItem(session);

            // Resposta terminou game - Placar
            SendPlacar(session);

            // Resposta Treasure Hunter Item Draw
            SendTreasureHunterItemDrawGUI(session);
        }

        public override int CheckEndShotOfHole(Player session)
        {

            // Agora verifica o se ele acabou o hole e essas coisas
            InitPlayerInfo("checkEndShotOfHole",
                "tentou verificar a ultima tacada do hole no jogo",
                session, out PlayerGameInfo pgi);

            if (pgi.shot_sync.state_shot.display.acerto_hole || pgi.data.giveup == 1)
            {

                // Verifica se o player terminou jogo, fez o ultimo hole
                if (Course.findHoleSeq(pgi.hole) == RoomInfo.HoleCount)
                {

                    // Resposta para o player que terminou o ultimo hole do Game 
                    session.Send(new Packet((ushort)0x199));
                    // Fez o Ultimo Hole, Calcula Clear Bonus para o player
                    if (pgi.shot_sync.state_shot.display.clear_bonus)
                    {

                        if (!MapSystem.Instance.isLoad())
                        {
                            MapSystem.Instance.load();
                        }

                        var map = MapSystem.Instance.getMap((byte)(RoomInfo.CourseIndex & RoomCourseFlags.UNK));

                        if (map == null)
                        {
                            _smp.LogManager.Instance.push(new AppMessage("[TourneyBase::checkEndShotOfHole][Error][Warning] tentou pegar o Map dados estaticos do CourseIndex[COURSE=" + Convert.ToString(RoomInfo.GetMap()) + "], mas nao conseguiu encontra na classe do Server.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                        }
                        else
                        {
                            pgi.data.bonus_pang += MapSystem.Instance.calculeClear30s(map, RoomInfo.HoleCount);
                        }
                    }
                }

                FinishHole(session);

                ChangeHole(session);
            }

            return 0;
        }




        public override bool FinishGame(Player session, int option)
        {

            if (session.getState()
                && session.Connected
                && Players.Count() > 0)
            {

                var p = new Packet();

                if (option == 6)
                {

                    if (_tourneyState)
                    {
                        FinishTourney(session, 1); // Termina sem ter acabado de jogar
                    }

                    InitPlayerInfo("finish_game",
                        "tentou terminar o jogo",
                        session, out PlayerGameInfo pgi);

                    // Salve o record se o camp acabou e o player n o terminou todos os holes tbm tem que salvar o record [OK][Feito]
                    RequestSaveRecordCourse(session,
                        0,
                        (RoomInfo.HoleCount == 18 && (Course.findHoleSeq(pgi.hole) == 18 || pgi.flag == PlayerGameInfo.eFLAG_GAME.END_GAME)) ? 1 : 0);

                    RequestSaveInfo(session, 0);

                    // D  Exp para o Caddie E Mascot Tamb m
                    if (pgi.data.exp > 0)
                    { // s  add Experience se for maior que 0

                        // Add Exp para o player
                        session.addExp(pgi.data.exp, false);

                        // D  Exp para o Caddie Equipado
                        if (session.Inventory.UserEquippedItem.CaddieEquiped != null) // Tem um caddie equipado
                        {
                            session.addCaddieExp(pgi.data.exp);
                        }

                        // D  Exp para o Mascot Equipado
                        if (session.Inventory.UserEquippedItem.MascotEquiped != null)
                        {
                            session.addMascotExp(pgi.data.exp);
                        }
                    }

                    // Update Info Map Statistics
                    SendUpdateInfoAndMapStatistics(session, 0);

                    // Resposta Treasure Hunter Item
                    RequestSendTreasureHunterItem(session);

                    // Update Mascot Info ON GAME, se o player estiver com um mascot equipado
                    if (session.Inventory.UserEquippedItem.MascotEquiped != null)
                    {
                        session.Send(Handle_PACKET_RESPONSE.pacote06B(session.Inventory, 8));
                    }

                    // Achievement Aqui
                    pgi.sys_achieve.finish_and_update(session);

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
                    p.WriteUInt64(session.UserInfo.Statistics.pang);
                    p.WriteUInt64(0);
                    session.Send(p);

                    // Colocar o finish_game Para 1 quer dizer que ele acabou o camp
                    pgi.finish_game = 1;

                    // ServerFlag do game que terminou
                    GameInitState = 2; // ACABOU

                }
                else if (option == 1)
                {

                    InitPlayerInfo("finish_game",
                        "tentou terminar o jogo", session, out PlayerGameInfo pgi);

                    // Finish Artefact Frozen Flame agora   direto no Finish Item Used Game
                    RequestFinishItemUsedGame(session);

                    RequestSaveDrop(session);

                    RequestDrawTreasureHunterItem(session);

                    // Aqui que vem esse aqui, o Save record CourseIndex e o save info
                    RequestSaveRecordCourse(session,
                        0,
                        (RoomInfo.HoleCount == 18 && Course.findHoleSeq(pgi.hole) == 18) ? 1 : 0);

                    RequestSaveInfo(session, 0);

                    // Resposta Treasure Hunter Item Draw
                    SendTreasureHunterItemDrawGUI(session);

                    // Resposta de Sai com Ticket Report
                    p.init_plain(0x12A);

                    p.WriteUInt32(0); // OK 
					session.Send(p);
					// Update Info Map Statistics
					SendUpdateInfoAndMapStatistics(session, 0);

                    // Resposta terminou game - Drop Itens
                    SendDropItem(session);

                    // Pacote dizendo para sair da sala e voltar para a Lobby Normal por que Ticket report s  pode user em Tourney 
                    session.Send(Handle_PACKET_RESPONSE.pacote04C(-1)); 

                    // Resposta Envia os itens ganhos no Treasure Hunter
                    RequestSendTreasureHunterItem(session);

                    // Update Mascot Info ON GAME, se o player estiver com um mascot equipado
                    if (session.Inventory.UserEquippedItem.MascotEquiped != null)
                    {
                        session.Send(Handle_PACKET_RESPONSE.pacote06B(session.Inventory, 8));
                    }

                    // Achievement Aqui
                    pgi.sys_achieve.finish_and_update(session);

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
                    p.WriteUInt64(session.UserInfo.Statistics.pang);
                    p.WriteUInt64(0);
                    session.Send(p);

                }
            }

            return (PlayersCompleteGameAndClear() && _tourneyState);
        }
        #endregion

        #region GAME TIMER
        public override void GameTimeIsOver()
        {

            if (GameInitState == 1 && Players.Count() > 0)
            {

                Player session = null;

                // ToList() garante snapshot: FinishTourney pode modificar PlayerInfo
                // internamente, causando InvalidOperationException no foreach original.
                foreach (var el in PlayerInfo.ToList())
                {

                    // Só os que não acabaram
                    if (el.Value.flag == PlayerGameInfo.eFLAG_GAME.PLAYING && (session = FindSessionByUID(el.Value.uid)) != null)
                    {
                        FinishTourney(session, 1);
                    }
                    else if (el.Value.flag == PlayerGameInfo.eFLAG_GAME.FINISH && (session = FindSessionByUID(el.Value.uid)) != null)
                    {
                        // Resposta para acabou o tempo do Tourney
                        SendTimeIsOver(session);
                    }
                }
            }
        }

        public override void RequestStartAfterEnter(Action action)
        {
            uint milliseconds = 0;

            if (RoomInfo.HoleCount == 18)
                milliseconds = 10 * 60000; // 10min
            else if (RoomInfo.HoleCount == 9)
                milliseconds = 5 * 60000; // 5min 

            _timerAfterToEnter = GameServer.Instance.MakeTimer(milliseconds, () => action());
        }

        public override void RequestEndAfterEnter()
        {

            // Limpa timer After Enter
            ClearTimeAfterEnter();

            // Send Resposta para todos que acabou o tempo para entrar na sala
            var p = new Packet(0x113);
            p.WriteByte(8);
            p.WriteByte(0); // 0 = Acabou o tempo para entrar, 1 = Começou o tempo para entrar
            p.WriteByte((byte)PlayerInfo.Count());
            SendBroadCast(p);
        }


        private void ClearTimeAfterEnter()
        {

            // Garantir que qualquer exception derrube o server

            try
            {

                if (_timerAfterToEnter != null)
                    GameServer.Instance.DeleteTimer(_timerAfterToEnter);
            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[Tourney::clear_time_after_enter][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            _timerAfterToEnter = null;
        }

        #endregion

        #region Medal Sort
        public int SpeedSort(PlayerGameInfo pgi1, PlayerGameInfo pgi2)
        {
            if (pgi1.progress.isGoodScore() && !pgi2.progress.isGoodScore())
                return -1;

            if (!pgi1.progress.isGoodScore() && pgi2.progress.isGoodScore())
                return 1;

            TimeSpan diff = pgi1.time_finish.ConvertTime() - pgi2.time_finish.ConvertTime();

            return diff.CompareTo(TimeSpan.Zero);
        }

        public int BestDriveSort(PlayerGameInfo pgi1, PlayerGameInfo pgi2)
        {
            if (pgi1.progress.isGoodScore() && !pgi2.progress.isGoodScore())
                return -1;

            if (!pgi1.progress.isGoodScore() && pgi2.progress.isGoodScore())
                return 1;

            return pgi2.progress.best_drive.CompareTo(pgi1.progress.best_drive);
        }

        public int BestChipInSort(PlayerGameInfo pgi1, PlayerGameInfo pgi2)
        {
            if (pgi1.progress.isGoodScore() && !pgi2.progress.isGoodScore())
                return -1;

            if (!pgi1.progress.isGoodScore() && pgi2.progress.isGoodScore())
                return 1;

            return pgi2.progress.best_chipin.CompareTo(pgi1.progress.best_chipin);
        }

        public int BestLongPuttinSort(PlayerGameInfo pgi1, PlayerGameInfo pgi2)
        {
            if (pgi1.progress.isGoodScore() && !pgi2.progress.isGoodScore())
                return -1;

            if (!pgi1.progress.isGoodScore() && pgi2.progress.isGoodScore())
                return 1;

            return pgi2.progress.best_long_puttin.CompareTo(pgi1.progress.best_long_puttin);
        }

        public int BestRecoverySort(PlayerGameInfo p1, PlayerGameInfo p2)
        {
            if (p1.progress.isGoodScore() && !p2.progress.isGoodScore())
                return -1; // p1 vem antes

            if (!p1.progress.isGoodScore() && p2.progress.isGoodScore())
                return 1; // p2 vem antes

            // Menor recuperação é melhor
            return p1.progress.getBestRecovery().CompareTo(p2.progress.getBestRecovery());
        }
        #endregion



        #region DISPOSE
        public override void Dispose(bool disposing)
        {
            if (disposing)
            {
                LogDestruction();
                // Itera sobre snapshot para evitar InvalidOperationException
                // caso DeletePlayer modifique a lista durante a iteração.
                DeleteAllPlayer();
                //
                _tourneyState = false;
            }
            base.Dispose(disposing);
        }

        ~Tourney()
        {
            Dispose(false);
        }
        #endregion
    }
}
