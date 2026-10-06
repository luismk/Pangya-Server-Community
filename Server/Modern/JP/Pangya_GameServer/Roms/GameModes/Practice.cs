using Pangya_GameServer.Feature;
using Pangya_GameServer.Flags;
using Pangya_GameServer.Models.Game;
using Pangya_GameServer.PacketFunc;
using Pangya_GameServer.Roms.GameBase.Modes;
using Pangya_GameServer.Session;
using PangyaAPI.Network;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using System.Reflection.Emit;

namespace Pangya_GameServer.Roms.GameModes
{
    public class Practice : TourneyBase
    {
        private bool _initPracticeState;

        public Practice(List<Player> players, GameRoomInfoModel roomInfo, RateValue rateValue) : base(players, roomInfo, rateValue)
        { 
            // Aqui tem que inicializar os players info
            InitAllPlayerInfo();

            State = InitRoomGame();
        }

        #region REQUESTS
        public override void RequestInitHole(Player session, Packet packet)
        {
            try
            {

                base.RequestInitHole(session, packet);

                InitPlayerInfo("RequestInitHole",
                    "tentou inicializar o hole do jogo",
                    session, out PlayerGameInfo pgi);

                // Update Counter Hole do Achievement do player
                pgi.sys_achieve.incrementCounter(0x6C400005u/* / *Hole Count * /*/);

            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[GameModePractice::RequestInitHole][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void RequestChangeWindNextHoleRepeat(Player session, Packet packet)
        {
            try
            {

                InitPlayerInfo("RequestChangeWindNextHoleRepeat",
                    "tentou trocar o vento dos proximos holes repeat no jogo",
                    session, out PlayerGameInfo pgi);

                Course.shuffleWindNextHole(pgi.hole);

            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[GameModePractice::RequestChangeWindNextHoleRepeat][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void RequestReplySyncShotData(Player session)
        {
            try
            {

                // Resposta Sync Shot
                SendSyncShot(session);

            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[GameModePractice::RequestReplySyncShotData][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void RequestFinishData(Player session)
        {

            RequestFinishItemUsedGame(session);

            RequestSaveDrop(session);

            InitPlayerInfo("RequestFinishData",
                "tentou finalizar dados do jogo",
                session, out PlayerGameInfo pgi);

            // Resposta terminou game - Drop Itens
            SendDropItem(session);

            // Resposta terminou game - Placar
            SendPlacar(session);
        }

        public override void RequestCalculeShotSpinningCube(Player session, ShotSyncData ssd)
        {
            try
            {

#if DEBUG
                CalculeShotToSpinningCube(session, ssd);
#endif // DEBUG

            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[GameModePractice::RequestCalculeShotSpinningCube][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void RequestCalculeShotCoin(Player session, ShotSyncData ssd)
        {
            try
            {

#if DEBUG
                CalculeShotToCoin(session, ssd);
#endif // DEBUG

            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[GameModePractice::RequestCalculeShotCoin][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }


        public override void SendInitialData(Player session)
        { 
            base.SendInitialData(session);
        } 
        #endregion

        #region OVERRIDES METHODS

        public override bool InitRoomGame()
        {

            if (Players.Count > 0)
            {

                // Cria o timer do practice
                GameStartTime();//nao sei bem o que é, mas e um tempo!

                // variavel que salva a data local do sistema
                InitGameTime();

                GameInitState = 1; // Come ou Game

                _initPracticeState = true; // Come ou Practice
            }

            return true;
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
                    session.Send(new Packet(0x199));

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
                            _smp.LogManager.Instance.push(new AppMessage("[GameModePractice::checkEndShotOfHole][Error][Warning] tentou pegar o Map dados estaticos do CourseIndex[COURSE=" + Convert.ToString((ushort)((byte)(RoomInfo.CourseIndex & RoomCourseFlags.UNK))) + "], mas nao conseguiu encontra na classe do Server.", type_msg.CL_FILE_LOG_AND_CONSOLE));
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

        public void finish()
        {

            GameInitState = 2; // Acabou o Jogo

            CalculeRankPlace();

            FinishExpGame();

            // ToList() garante snapshot — RequestFinishData pode modificar Players
            foreach (var el in Players.ToList())
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

        public override void GameTimeIsOver()
        {

            try
            {

                // Block 

                if (GameInitState == 1 && Players.Count > 0)
                {

                    GameInitState = 2; // Acabou

                    var p = new Packet();

                    var s = Players.First();

                    InitPlayerInfo("end_time",
                        "tentou acabar o tempo no jogo",
                        s, out PlayerGameInfo pgi);

                    CalculePang(s);

                    SendFinishMessage(s);

                    // Resposta terminou o hole
                    UpdateFinishHole(s, 0);

                    RequestFinishHole(s, 1); // N o terminou o hole 

                    // Resposta para o termina Game por tempo
                    SendTimeIsOver(s);

                    // Termina jogo
                    finish();
                   //finaliza o tempo 
                    GameStop();
                }
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[GameModePractice::timeIsOver][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override bool FinishGame(Player session, int option)
        {
            if (session.getState()
            && session.Connected
            && Players.Count() > 0)
            {

                if (option == 6)
                {

                    var p = new Packet();

                    if (_initPracticeState)
                    {
                        FinishPractice(session, 1); // Termina sem ter acabado de jogar
                    }

                    InitPlayerInfo("finish_game",
                        "tentou terminar o jogo",
                        session, out PlayerGameInfo pgi);

                    // Practice n o salva info, s  Pang, Experience, e itens used and dropped. Ex: Active Item, Spinning Cube e Coin
                    SavePang(session);

                    // Practice ganha Experience, mas nao d  para o caddie, o caddie n o ganha Experience no practice
                    if (pgi.data.exp > 0) // s  add Experience se for maior que 0
                    {
                        session.addExp(pgi.data.exp, false);
                    }

                    // Update Info Map Statistics
                    SendUpdateInfoAndMapStatistics(session, 0);

                    // Resposta Treasure Hunter Item
                    RequestSendTreasureHunterItem(session);

                    // Achievement Aqui
                    pgi.sys_achieve.finish_and_update(session);

                    // Resposta Update Pang
                    p.init_plain(0xC8);
                    p.WriteUInt64(session.UserInfo.Statistics.pang);
                    p.WriteUInt64(0Ul);
                    session.Send(p);

                    // Colocar o finish_game Para 1 quer dizer que ele acabou o camp
                    pgi.finish_game = 1;

                    // ServerFlag do game que terminou
                    GameInitState = 2; // ACABOU
                }
            }

            return (PlayersCompleteGameAndClear() && _initPracticeState);
        }

        public void FinishExpGame()
        {

            if (Players.Count > 0)
            {

                // Practine n o conta estrela ele da 1 de Experience por hole jogados
                float stars = Course.getStar();

                var holeSeq = 0;

                foreach (var el in Players)
                {

                    InitPlayerInfo("RequestFinishExpGame",
                        "tentou finalizar Experience do jogo",
                        el, out PlayerGameInfo pgi);

                    holeSeq = Course.findHoleSeq(pgi.hole);

                    // Ele est  no primeiro hole e n o acertou ele, s  da experi ncia se ele tiver acertado o hole
                    if (holeSeq != RoomInfo.HoleCount && !pgi.shot_sync.state_shot.display.acerto_hole)
                    {
                        holeSeq = 0;
                    }

                    if (el.UserInfo.Level < 70)
                    {
                        pgi.data.exp = ((holeSeq > 0 ? holeSeq : 0) * 1);
                    }

                    _smp.LogManager.Instance.push(new AppMessage("[GameModePractice::FinishExpGame][Log] Normal[UID=" + Convert.ToString(el.UserInfo.UID) + "] ganhou " + Convert.ToString(pgi.data.exp) + " de experience.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                }
            }
        }

        public void SavePang(Player session)
        {

            InitPlayerInfo("SavePang",
                "tentou salvar os Pang ganho no jogo",
                session, out PlayerGameInfo pgi);

            if (pgi.data.pang > 0 || pgi.data.bonus_pang > 0) // S  add se for maior que 0
            {
                session.UserInfo.addPang(pgi.data.pang + pgi.data.bonus_pang);
            }
        }


        public override void CalculePang(Player session)
        {

            base.CalculePang(session);

            InitPlayerInfo("CalculePang",
                "tentou calcular o Pang do player no jogo",
                session, out PlayerGameInfo pgi);

            // Practice
            // Hole Repeat ganha 1/6 dos Pang(s) feito
            if (RoomInfo.HoleMode == (int)RoomHoleType.M_REPEAT)
            {
                float taxaDinamica = 1.0f / 6.0f; // Padrão

                // Se o cara fez MUITO Pang, a gente taxa mais para valorizar a moeda
                if (pgi.data.bonus_pang > 20000)
                {
                    taxaDinamica = 0.05f; // Apenas 5% (Taxa de Luxo)
                }
                else if (pgi.data.bonus_pang > 10000)
                {
                    taxaDinamica = 0.10f; // 10%
                }

                pgi.data.pang = (ulong)(pgi.data.pang * taxaDinamica);
                pgi.data.bonus_pang = (ulong)(pgi.data.bonus_pang * taxaDinamica);
            }
            else
            { // Course Practice

                pgi.data.pang = (ulong)(pgi.data.pang * (1.0f / 3.0f));
                pgi.data.bonus_pang = (ulong)(pgi.data.bonus_pang * (1.0f / 3.0f));
            }
        }

        public override void ChangeHole(Player session)
        {

            UpdateTreasureHunterPoint(session);

            if (CheckEndGame(session))
            {
                FinishPractice(session, 0);
            }
            else
            {
                // Resposta terminou o hole
                UpdateFinishHole(session, 1);
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

        private void FinishPractice(Player session, int option)
        {

            if (Players.Count > 0 && GameInitState == 1)
            {

                InitPlayerInfo("finish_practice",
                    "tentou terminar o practice no jogo",
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
                        pgi.sys_achieve.incrementCounter((RoomInfo.GetHoleType() == RoomHoleType.M_REPEAT) ? 0x6C40003Du /*/ *Hole Repeat * /*/ : 0x6C40003Eu /*/ *Course Practice * /*/);
                    }
                }

                SetGameFlag(pgi, (option == 0) ? PlayerGameInfo.eFLAG_GAME.FINISH : PlayerGameInfo.eFLAG_GAME.END_GAME);

                pgi.time_finish.CreateTime();

                if (AllCompleteGameAndClear() && GameInitState == 1)
                {
                    finish(); // Envia os pacotes que termina o jogo Ex: 0xCE, 0x79 e etc
                }
            }

        }


        #endregion

        #region DISPOSE

        public override void Dispose(bool disposing)
        {
            if (Disposed) return;

            if (disposing)
            {
                _initPracticeState = false;

                // Para o timer do jogo
                GameStop(); 

                LogDestruction();
            }

            base.Dispose(disposing);
        }

        ~Practice()
        {
            Dispose(false);
        }
        #endregion
    }
}
