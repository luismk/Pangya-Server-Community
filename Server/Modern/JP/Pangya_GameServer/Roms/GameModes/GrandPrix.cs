using Pangya_GameServer.Engine;
using Pangya_GameServer.Feature;
using Pangya_GameServer.Flags;
using Pangya_GameServer.Manager;
using Pangya_GameServer.Models.Game;
using Pangya_GameServer.PacketFunc;
using Pangya_GameServer.Roms.GameBase.Modes;
using Pangya_GameServer.Server;
using Pangya_GameServer.Session;
using PangyaAPI.IFF.Flags;
using PangyaAPI.IFF.Handle.JP;
using PangyaAPI.IFF.Regions.JP.Models.IFF;
using PangyaAPI.Network;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using static Pangya_GameServer.Models.DefineConstants;

namespace Pangya_GameServer.Roms.GameModes
{
    public class GrandPrix : TourneyBase
    {
        readonly float TimeBoosterValue = 3.0f;
        private GrandPrixData _grandPrixData;
        private List<GrandPrixRankReward> _reward;
        private List<Bot> _bots;
        private List<RankPlayerDisplayChracter> _characterRankDisplay;
        private TimerManager _timerManager;
        private TimerManager _timerManagerRules;
        private LockManager _grandPrixManager;
        private bool _initGrandPrixState; 
        public GrandPrix(List<Player> players, GameRoomInfoModel roomInfo, RateValue rateValue, GrandPrixData grandPrixData) : base(players, roomInfo, rateValue)
        { 
            if (grandPrixData == null)
                throw new exception("[GrandPrix::GrandPrix][Error] grandPrixData está NULL — não é possível inicializar o Grand Prix.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GRAND_PRIX, 1, 0));

            _grandPrixData = grandPrixData;
            _characterRankDisplay = new List<RankPlayerDisplayChracter>();
            _reward = new List<GrandPrixRankReward>();
            _bots = new List<Bot>();
            _initGrandPrixState = false;
            _timerManager = new TimerManager();
            _timerManagerRules = new TimerManager();
            _grandPrixManager = new LockManager();

            UpdateTreasureHunterSystem();

            // Aqui tem que inicializar os players info
            InitAllPlayerInfo();

            // Load Grand Prix Rank Reward from iff
            _reward = sIff.Instance.findGrandPrixRankReward(_grandPrixData.TypeID_Link);

            // Log para verificar o carregamento
            if (_reward != null && _reward.Count > 0)
            {
                _reward.Sort((a, b) => a.Rank.CompareTo(b.Rank));
            }
            else
            {
                if (_grandPrixData.reward._typeid[0] == 0)
                _smp.LogManager.Instance.push(new AppMessage("[GrandPrix::Error] Falha crítica: m_gp_reward ou m_gp retornou NULL ou nao tem premios para o GP TypeID: " + _grandPrixData.TypeID_Link, type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            // Init _bots
            Init_bots();

            // Class Grand Prix Counter Item Typeid
            uint class_gp_counter_typeid = 0;

            if (sIff.Instance.isGrandPrixEvent(_grandPrixData.ID))
            {

                class_gp_counter_typeid = 0x6C4000AEu;

            }
            else
            {

                switch (sIff.Instance.getGrandPrixAbaType(_grandPrixData.ID))
                {
                    case GrandPrixData.GP_ABA.ROOKIE:
                        class_gp_counter_typeid = 0x6C4000AAu;
                        break;
                    case GrandPrixData.GP_ABA.BEGINNER:
                        class_gp_counter_typeid = 0x6C4000ABu;
                        break;
                    case GrandPrixData.GP_ABA.JUNIOR:
                        class_gp_counter_typeid = 0x6C4000ACu;
                        break;
                    case GrandPrixData.GP_ABA.EVENT:
                        class_gp_counter_typeid = 0x6C4000ADu;
                        break;
                }

            }

            // Initialize achievement of players
            foreach (var el in Players)
            {

                var pgi = InitPlayerInfo("GrandPrix", "tentou inicializar o counter item do Grand Prix", el);

                InitAchievement(el);


                if (class_gp_counter_typeid > 0)
                {
                    pgi.sys_achieve.incrementCounter(class_gp_counter_typeid);
                }
            }

             // Consome os Tickets dos player que v o jogar o Grand Prix
            ConsumeTicket();

            // inicializa o jogo
            State = InitRoomGame();
        }

        #region REQUESTS
        public override void RequestFinishCharIntro(Player session, Packet packet)
        {

            var p = new Packet();

            try
            {

                // Chama a base para ela fazer a parte dela
                base.RequestFinishCharIntro(session, packet);

                var pgi = InitPlayerInfo("RequestFinishCharIntro",
                    "tentou finalizar o character intro do player",
                    session);

                _grandPrixManager.@lock(session);

                // Aqui zera finish hole2 do player
                pgi.finish_hole2 = 0;
                pgi.finish_hole3 = 0;

                _grandPrixManager.unlock(session);

                // Aqui come a o tempo do hole do player
                if (_grandPrixData.TimeHole > 0)
                {
                    GameStartTime(session);
                }

            }
            catch (exception e)
            {

                _grandPrixManager.unlock(session);

                _smp.LogManager.Instance.push(new AppMessage("[GrandPrix::RequestFinishCharIntro][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

        }

        public override void RequestActiveBooster(Player session, Packet packet)
        {

            var p = new Packet();

            try
            { 
                // 3.f Velociade 2x Padr o do Time Booster, o player est  gastando o dele ou ele   premium user
                // 2.f < 3.f Velocidade do Booster 1.5x do Grand Prix que ele d  de gra a. Porem no Pangya JP n o tem esse Booster no Grand Prix
                float velocidade = packet.ReadFloat();

                var pgi = InitPlayerInfo("RequestActiveBooster",
                    "tentou ativar Time Booster no jogo",
                    session);

                // Booster Normal
                if (velocidade >= TimeBoosterValue)
                {

                    if (!session.UserInfo.UserCapabilities.UserPremium)
                    { // NÃO é premium user — precisa ter o item Time Booster

                        var pWi = session.Inventory.FindWarehouseItemByTypeid(TIME_BOOSTER_TYPEID);

                        if (pWi == null)
                        {
                            throw new exception("[GrandPrix::RequestActiveBooster][Error] Normal[UID=" + session.UserInfo.UID + "] tentou ativar time booster, mas ele nao tem o item passive. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GRAND_PRIX,
                                11, 0));
                        }

                        if (pWi.STDA_C_ITEM_QNTD <= 0)
                        {
                            throw new exception("[GrandPrix::RequestActiveBooster][Error] Normal[UID=" + session.UserInfo.UID + "] tentou ativar time booster, mas ele nao tem quantidade suficiente[VALUE=" + (pWi.STDA_C_ITEM_QNTD) + ", REQUEST=1] do item de time booster.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY_BASE,
                                12, 0));
                        }

                        var it = pgi.used_item.v_passive.FirstOrDefault(c => c.Key == pWi._typeid);

                        if (it.Value == null)
                        {
                            throw new exception("[GrandPrix::RequestActiveBooster][Error] Normal[UID = " + session.UserInfo.UID + "] tentou ativar time booster, mas ele nao tem ele no item passive usados do server. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GRAND_PRIX,
                                13, 0));
                        }

                        if ((short)it.Value.count >= pWi.STDA_C_ITEM_QNTD)
                        {
                            throw new exception("[GrandPrix::RequestActiveBooster][Error] Normal[UID=" + session.UserInfo.UID + "] tentou ativar time booster, mas ele ja usou todos os time booster. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GRAND_PRIX,
                                14, 0));
                        }

                        // Add +1 ao item passive usado
                        it.Value.count++;

                    }
                    else
                    { // Soma +1 no contador de counter item do booster do player e passive item

                        pgi.sys_achieve.incrementCounter(0x6C400075u /*Passive Item*/);

                        pgi.sys_achieve.incrementCounter(0x6C400050u);
                    }

                }

                // Resposta para Active Booster
                p.init_plain(0xC7);

                p.WriteFloat(velocidade);
                p.WriteInt32(session.ConnectionID);

                session.Send(p);

            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[GrandPrix::RequestActiveBooster][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void RequestStartTurnTime(Player session, Packet packet)
        {
            try
            {

                var pgi = InitPlayerInfo("RequestStartTurnTime",
                    "tentou comecar o tempo de rule do player",
                    session);

                _grandPrixManager.@lock(session);

                // Limpa init shot
                pgi.init_shot = 0;

                _grandPrixManager.unlock(session);

                // Come a o tempo do Rule do Grand Prix
                if (_grandPrixData.rule > 0)
                {
                    GameStartTimeRule(session);
                }

            }
            catch (exception e)
            {

                _grandPrixManager.unlock(session);

                _smp.LogManager.Instance.push(new AppMessage("[GrandPrix::RequestStartTurnTime][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void RequestInitShot(Player session, Packet packet)
        {

            try
            {


                OncePerShot("RequestInitShot",
                    "tentou iniciar tacada no jogo",
                    "init_shot", session, out PlayerGameInfo pgi, () => { return; });

                // Para(Stop) o tempo rule dele que acabou de tacar
                GameStopTimeRule(session);

                // Chama o fun  o da classe pai
                base.RequestInitShot(session, packet);
            }
            catch (exception e)
            { 
                _grandPrixManager.unlock(session);

                _smp.LogManager.Instance.push(new AppMessage("[GrandPrix::RequestInitShot][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void RequestSyncShot(Player session, Packet packet)
        {

            try
            {
                OncePerShot("RequestInitShot",
                   "tentou iniciar tacada no jogo",
                   "sync_shot_flag", session, out PlayerGameInfo pgi, () => { return; });

                base.RequestSyncShot(session, packet);

                // Verifica se as tr s tacadas foram recebidas e para para o proximo turno outro troca o hole
                ChangeTurn(session);

            }
            catch (exception e)
            {

                _grandPrixManager.unlock(session);

                _smp.LogManager.Instance.push(new AppMessage("[GrandPrix::RequestSyncShot][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void RequestTranslateSyncShotData(Player session, ShotSyncData ssd)
        {
            try
            { 
                var s = FindSessionByOID(ssd.oid);

                if (s == null)
                {
                    throw new exception("[GrandPrix::requestTranslateSyncShotData][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou sincronizar tacada do Normal[OID=" + Convert.ToString(ssd.oid) + "], mas o player nao existe nessa jogo. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GRAND_PRIX,
                        200, 0));
                }

                // Bloquea o player o tempo
                _grandPrixManager.@lock(session);

                // Update Sync Shot Player
                if (session.UserInfo.UID == s.UserInfo.UID)
                {

                    var pgi = InitPlayerInfo("requestTranslateSyncShotData",
                        "tentou sincronizar a tacada no jogo",
                        session);

                    pgi.shot_sync = ssd;

                    // Last Location Player
                    var lastLocation = pgi.location;

                    // Update Location Player
                    pgi.location.x = ssd.location.x;
                    pgi.location.z = ssd.location.z;

                    // Update Pang and Bonus Pang
                    pgi.data.pang = ssd.pang;
                    pgi.data.bonus_pang = ssd.bonus_pang;

                    // J  s  na fun  o que come a o tempo do player do turno
                    pgi.data.tacada_num++;

                    if (ssd.state == ShotSyncData.SHOT_STATE.OUT_OF_BOUNDS || ssd.state == ShotSyncData.SHOT_STATE.UNPLAYABLE_AREA)
                    {
                        pgi.data.tacada_num++;
                    }

                    // Verifica se o Grand Prix tem regras especiais e se a regra   de n o poder fazer uma tacada especial
                    // Se sim a penalidade   +1 na tacada do player
                    if ((eRULE)_grandPrixData.rule == eRULE.SPECIAL_SHOT && ssd.state_shot.display.special_shot)
                    {
                        pgi.data.penalidade++;
                    }

                    // Hole find
                    var hole = Course.findHole(pgi.hole);

                    if (hole == null)
                    {
                        throw new exception("[GrandPrix::requestTranslateSyncShotData][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou sincronizar tacada no hole[NUMERO=" + Convert.ToString((ushort)pgi.hole) + "], mas o RoomID do hole is invalid. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GRAND_PRIX,
                            12, 0));
                    }

                    // Conta j  a pr xima tacada, no give up
                    if (!ssd.state_shot.display.acerto_hole && hole.getPar().total_shot <= (pgi.data.tacada_num + 1))
                    {

                        // +1 que   give up, s  add se n o passou o n mero de tacadas
                        if (pgi.data.tacada_num < hole.getPar().total_shot)
                        {
                            pgi.data.tacada_num++;
                        }

                        pgi.data.giveup = 1;

                        // Soma +1 no Bad Condute
                        pgi.data.bad_condute++;
                    }

                    // Acabou o hole para o tempo do hole do player
                    if (ssd.state_shot.display.acerto_hole || pgi.data.giveup > 0)
                    {

                        // seta PCBangMascot finish hole2 do player
                        pgi.finish_hole2 = 1;

                        // Para o tempo do player
                        GameStopTime(session);
                        GameStopTimeRule(session);
                    }

                    // aqui os achievement de power shot int32_t putt beam impact e etc
                    UpdateSyncShotAchievement(session, lastLocation);
                }

                // Libera
                _grandPrixManager.unlock(session);

            }
            catch (exception e)
            {

                // Libera
                _grandPrixManager.unlock(session);

                _smp.LogManager.Instance.push(new AppMessage("[GrandPrix::requestTranslateSyncShotData][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void RequestCalculeShotSpinningCube(Player session, ShotSyncData ssd)
        { 
            try
            {

                // S  calcula se n o for short game e n o for grand prix rookie
                if (!(RoomInfo.SpecialModeRoom.IsShotMode) && !(sIff.Instance.getGrandPrixAbaType(_grandPrixData.ID) == GrandPrixData.GP_ABA.ROOKIE && sIff.Instance.isGrandPrixNormal(_grandPrixData.ID)))
                {
                    CalculeShotToSpinningCube(session, ssd);
                }

            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[GrandPrix::requestCalculeShotSpinningCube][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }
         
        public override void RequestCalculeShotCoin(Player session, ShotSyncData ssd)
        { 
            try
            {

                // S  calcula se n o for short game e n o for grand prix rookier
                if (!(RoomInfo.SpecialModeRoom.IsShotMode) && !(sIff.Instance.getGrandPrixAbaType(_grandPrixData.ID) == GrandPrixData.GP_ABA.ROOKIE && sIff.Instance.isGrandPrixNormal(_grandPrixData.ID)))
                {
                    CalculeShotToCoin(session, ssd);
                }

            }
            catch (exception e)
            { 
                _smp.LogManager.Instance.push(new AppMessage("[GrandPrix::requestCalculeShotCoin][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }
         

        #endregion

        #region
        public override void ChangeHole(Player session)
        {

            UpdateTreasureHunterPoint(session);

            if (CheckEndGame(session))
            {
                FinishGrandPrix(session, 0);
            }
            else
            {

                // Resposta terminou o hole
                UpdateFinishHole(session, 1); // Terminou

                // Troquei o clear hole e giveup pelo a PCBangMascot finish hole. Agora est  OK
                if (CheckAllClearHole())
                { 
                    ClearAllHole();

                    // Change Hole All Finish Hole
                    SendAllToNextHole();
                }
            }
        }

        public override void FinishHole(Player session)
        {

            try
            {

                OncePerShot("finishHole", "tentou finalizar o hole", "finish_hole3", session, out PlayerGameInfo pgi, () => { return; });

                _grandPrixManager.@lock(session);

                // Para o tempo do player
                GameStopTime(session);
                GameStopTimeRule(session);

                // Se o player estiver feito give up ou dado time out, n o soma as penalidade que ele j  fez o score max mo
                if (pgi.data.time_out == 0u && pgi.data.giveup == 0u)
                {
                    // Adiciona as penalidade para as tacadas do player
                    pgi.data.tacada_num += (int)pgi.data.penalidade;
                }

                // finaliza os dados do hole Game::RequestfinishHole
                RequestFinishHole(session, 0);

                // update itens usados no jogo
                RequestUpdateItemUsedGame(session);

                // Limpa flags das tacadas
                pgi.init_shot = 0;
                pgi.sync_shot_flag = 0;
                pgi.finish_shot = 0;

                // Libera
                _grandPrixManager.unlock(session);

                pgi.finish_hole = 1; 

            }
            catch (exception e)
            {

                // Libera
                _grandPrixManager.unlock(session);

                _smp.LogManager.Instance.push(new AppMessage("[GrandPrix::finishHole][ErrrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

        }

        public void FinishGrandPrix(Player session, int option)
        {

            if (Players.Count > 0 && GameInitState == 1)
            {

                var pgi = InitPlayerInfo("finish_grand_prix",
                    "tentou terminar o grand prix no jogo",
                    session);

                if (pgi.flag == PlayerGameInfo.eFLAG_GAME.PLAYING)
                {

                    // Calcula os pangs que o player ganhou
                    CalculePang(session);

                    // Rookie Grand Prix s  da 1/3 dos pangs ganhos
                    if (sIff.Instance.getGrandPrixAbaType(_grandPrixData.ID) == GrandPrixData.GP_ABA.ROOKIE && sIff.Instance.isGrandPrixNormal(_grandPrixData.ID))
                    {
                        pgi.data.pang = (ulong)(pgi.data.pang * (1.0f / 3.0f));
                        pgi.data.bonus_pang = (ulong)(pgi.data.bonus_pang * (1.0f / 3.0f));
                    }

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
                        pgi.sys_achieve.incrementCounter(0x6C400004u /*Normal game complete*/);

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

                SetGameFlag(pgi, (option == 0) ? PlayerGameInfo.eFLAG_GAME.FINISH : PlayerGameInfo.eFLAG_GAME.END_GAME);

                pgi.time_finish.CreateTime();

                if (AllCompleteGameAndClear() && GameInitState == 1)
                {
                    Finish(); // Envia os pacotes que termina o jogo Ex: 0xCE, 0x79 e etc
                }
            }
        }

        public override void SendInitialData(Player session)
        {
            var p = new Packet();

            try
            {
                if (Interlocked.Increment(ref SyncSendInitData) == Players.Count())
                {
                    Interlocked.Exchange(ref SyncSendInitData, 0);

                    // Game Data Init
                    p.init_plain(0x76);

                    p.WriteByte(RoomInfo.RoomType);
                    p.WriteUInt32(1);

                    p.WriteTime(StartTime);

                    SendBroadCast(p);
                    // Aqui   os bots do GP
                    p.init_plain(0x256);

                    p.WriteUInt32(0); // OK [Option Error]

                    p.WriteUInt16((ushort)_bots.Count());

                    foreach (var bot in _bots)
                    {
                        p.WriteUInt32(bot.id);
                        p.WriteByte((byte)bot.hole.Count());
                        foreach (var hole in bot.hole)
                            p.WriteBytes(hole.ToArray());
                    }

                    SendBroadCast(p);
                    // Course
                    // Send Individual Packet to all players in game
                    foreach (var el in Players)
                    {
                        base.SendInitialData(el);
                    }
                }
            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[GrandPrix::sendInitialData][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }


        public override bool DeletePlayer(Player session, int option)
        {

            if (session == null)
            {
                throw new exception("[GrandPrix::deletePlayer][Error] tentou deletar um player, mas o seu endereco eh null.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY,
                    50, 0));
            }

            if (GetPlayerInfo((session)) == null)
                return true;

            bool ret = false;

            try
            {
                var it = Players.FirstOrDefault(c => c == session);

                if (it != null)
                {
                    byte opt = 3; // Saiu Quitou

                    var pgi = InitPlayerInfo("deletePlayer",
                        "tentou sair do jogo",
                        session);

                    // Para o tempo do hole do player
                    GameStopTime(session);
                    GameStopTimeRule(session);

                    var p = new Packet();

                    if (GameInitState == 1)
                    {

                        var sessions = GetSessions(it);

                        RequestFinishItemUsedGame(it); // Salva itens usados no Tourney

                        // Rookie Grand Prix n o altera o info do player s  achievement
                        if (!(sIff.Instance.getGrandPrixAbaType(_grandPrixData.ID) == GrandPrixData.GP_ABA.ROOKIE && sIff.Instance.isGrandPrixNormal(_grandPrixData.ID)))
                        {
                            RequestSaveInfo((it), (option == 0x800) ? 5 /*N o conta quit*/ : 1); // Quitou ou tomou DC
                        }

                        //pgi.PCBangMascot = PlayerGameInfo::eFLAG_GAME::QUIT;
                        SetGameFlag(pgi, PlayerGameInfo.eFLAG_GAME.QUIT);

                        // Resposta Player saiu do Jogo, tira ele do list de score
                        p.init_plain(0x61);

                        p.WriteInt32(it.ConnectionID);
                        session.Send(p);

                        // Resposta Player saiu do jogo
                        SendUpdateState(session, opt);

                        if (AllCompleteGameAndClear())
                        {
                            ret = true; // Termina o Tourney
                        }

                        SendUpdateInfoAndMapStatistics(session, -1);

                    }
                    else if (GameInitState == 2 && !(pgi.finish_game == 1))
                    {

                        // Acabou

                        // Rookie Grand Prix n o altera o info do player s  achievement
                        if (!(sIff.Instance.getGrandPrixAbaType(_grandPrixData.ID) == GrandPrixData.GP_ABA.ROOKIE && sIff.Instance.isGrandPrixNormal(_grandPrixData.ID)))
                        {
                            RequestSaveInfo((it), 0);
                        }
                    }

                    // Deleta o player por give up ou time out, ele conta os achievements dele, tem o counter item 0x6C400004u Normal Game Complete
                    // Envia os achievements para ele para ficar igual ao original
                    if (GameInitState == 1


                        && pgi.data.bad_condute >= 3
                        && (pgi.data.time_out > 0 || pgi.data.giveup > 0))
                    {

                        // Achievements
                        RainHoleSeqCount(session); // conta os achievement de Rain em holes consecutivas

                        ScoreSeqCount(session); // conta os achievement de back-to-back(2 ou mais score iguais consecutivos) do player

                        RainCount(session); // Aqui achievement de rain count

                        pgi.sys_achieve.incrementCounter(0x6C400004u /*Normal game complete*/);

                        // Achievement Aqui
                        pgi.sys_achieve.finish_and_update(session);

                        // Resposta que tem sempre que acaba um jogo, n o sei o que   ainda, esse s  n o tem no HIO Event
                        p = new Packet(0x244);

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
                    _smp.LogManager.Instance.push(new AppMessage("[GrandPrix::deletePlayer][Warning] player ja foi excluido do base.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                }

                // Aqui se n o for true tem que ver se todos terminaram o hole e enviar o pacote255
                if (!ret && CheckAllHoleAndClear())
                {
                    SendAllToNextHole();
                }
            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[GrandPrix::deletePlayer][Error] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Aqui se n o for true tem que ver se todos terminaram o hole e enviar o pacote255
                if (!ret && CheckAllHoleAndClear())
                {
                    SendAllToNextHole();
                }
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

        #endregion

        #region Game TImers
        public void GameStartTime(object quem)
        {

            try
            {

                if (quem  is not null && quem is Player)
                {
                    Player p = (Player)quem;
                    if (p != null && p.Connected)
                    {

                        // Para Tempo se j  estiver 1 timer
                        var timer = _timerManager.findTimer(p);

                        // N o tem um timer criado ainda, cria um para ele
                        if (timer == null || timer.m_timer == null)
                        {
                            if (timer == null && (timer = _timerManager.insertTimer(p, GameServer.Instance.MakeTimer((uint)(_grandPrixData.TimeHole * 1000), null, () => OnEndTime(this, quem), PangyaSyncTimer.TIMER_TYPE.NORMAL))) == null)
                            {
                                throw new exception("[GrandPrix::GameStartTime][Error] Normal[UID=" + Convert.ToString(p.UserInfo.UID) + "] nao conseguiu criar um timer_ctx para poder criar um timer para o player. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GRAND_PRIX,
                                    1050, 0));
                            }
                        }
                        else
                        {

                            // J  tem um timer, reseta ele e inicia novamente
                            if (timer.m_timer != null)
                            {
                                if (timer.m_timer.getState() != PangyaSyncTimer.TIMER_STATE.STOP || timer.m_timer.getState() != PangyaSyncTimer.TIMER_STATE.FINISH)
                                    timer.m_timer.Stop();

                                // inicia ele novamente, melhor desta forma
                                timer.m_timer = GameServer.Instance.MakeTimer((uint)(_grandPrixData.TimeHole * 1000), null, () => OnEndTime(this, quem), PangyaSyncTimer.TIMER_TYPE.NORMAL);
                            }
                        }
                    }

                }

            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[GrandPrix::GameStartTime][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public bool GameStopTime(object quem)
        {

            bool ret = true;

            try
            {

                if (quem != null && _grandPrixData.TimeHole > 0)
                {
                    // Cast seguro com 'as' — evita InvalidCastException
                    var p = quem as Player;
                    if (p != null)
                    {
                        var timer = _timerManager.findTimer(p);

                        // Corrigido: timer e timer.m_timer podem ser null se o player 
                        if (timer?.m_timer != null &&
                            (timer.m_timer.getState() != PangyaSyncTimer.TIMER_STATE.STOP ||
                             timer.m_timer.getState() != PangyaSyncTimer.TIMER_STATE.FINISH))
                        {
                            timer.m_timer.Stop();
                        }
                    }
                }

            }
            catch (exception e)
            {

                ret = false;

                _smp.LogManager.Instance.push(new AppMessage("[GrandPrix::GameStopTimer][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            return ret;
        }
         
        public void GameTimeIsOver(object quem)
        {

            try
            {

                if (quem is not null && quem is Player)
                { 
                    var s = (Player)(quem);

                    try
                    {

                        // Locker Player
                        _grandPrixManager.@lock(s);

                        var timer = _timerManager.findTimer(s);

                        if (timer != null && timer.m_timer != null)
                        {

                            // Para o tempo se ele n o estiver parado
                            if (timer.m_timer.getState() != PangyaSyncTimer.TIMER_STATE.STOP || timer.m_timer.getState() != PangyaSyncTimer.TIMER_STATE.FINISH)
                            {
                                timer.m_timer.Stop();
                            }

                            // Atualiza os dados do player que ele fez give up por que o tempo do hole  dele acabou
                            var pgi = InitPlayerInfo("timeIsOver",
                                "acabou o tempo do hole do player",
                                s);

                            // Player ainda n o terminou o hole
                            if (pgi.finish_hole2 == 0u && pgi.finish_hole3 == 0u)
                            {

                                var hole = Course.findHole(pgi.hole);

                                if (hole == null)
                                {
                                    throw new exception("[GrandPrix::timeIsOver][Error] Normal[UID=" + Convert.ToString(s.UserInfo.UID) + "] tentou pegar hole[NUMERO=" + Convert.ToString((ushort)pgi.hole) + "] no jogo, mas o RoomID do hole is invalid. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GRAND_PRIX,
                                        1020, 0));
                                }

                                pgi.data.tacada_num = hole.getPar().total_shot; // Give up

                                // Fez time out
                                pgi.data.time_out = 1;

                                // Envia para o player que o tempo do hole acabou
                                var p = new Packet(0x259);

                                p.WriteUInt32(0); // OK
								s.Send(p);
							}
                        }

                        // Libera
                        _grandPrixManager.unlock(s);

                    }
                    catch
                    {
                        // Libera
                        _grandPrixManager.unlock(s);

                        // Relan a para o outro try..catch exibir a mensagem no log
                        throw;
                    }

                }

            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[GrandPrix::timeIsOver][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }
         
        public void GameStartTimeRule(object quem)
        {

            try
            {

                if (quem != null
                    && _grandPrixData.rule > 0
                    && ((eRULE)_grandPrixData.rule == eRULE.TIME_10_SEC || (eRULE)_grandPrixData.rule == eRULE.TIME_15_SEC))
                {

                    uint time_milli = ((eRULE)_grandPrixData.rule == eRULE.TIME_10_SEC ? 10u : ((eRULE)_grandPrixData.rule == eRULE.TIME_15_SEC ? 15u : 0u));


                    Player p = (Player)(quem);

                    // Para Tempo se j  estiver 1 timer
                    var timer = _timerManagerRules.findTimer(p);

                    // N o tem um timer criado ainda, cria um para ele
                    if (timer == null || timer.m_timer == null)
                    {


                        // Cria o timer rule 
                        if (timer == null && (timer = _timerManagerRules.insertTimer(p, GameServer.Instance.MakeTimer(time_milli * 1000 /*milliseconds*/, null, () => end_time_rule(this, quem), PangyaSyncTimer.TIMER_TYPE.NORMAL))) == null)
                        {
                            throw new exception("[GrandPrix::GameStartTimeRule][Error] Normal[UID=" + Convert.ToString(p.UserInfo.UID) + "] nao conseguiu criar um timer_ctx para poder criar um timer rule para o player. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GRAND_PRIX,
                                1050, 0));
                        }
                    }
                    else
                    {

                        // J  tem um timer, reseta ele e inicia novamente
                        if (timer.m_timer != null)
                        {

                            if (timer.m_timer.getState() != PangyaSyncTimer.TIMER_STATE.STOP)
                            {
                                timer.m_timer.Stop();
                            }

                            // inicia ele novamente
                            timer.m_timer = GameServer.Instance.MakeTimer(time_milli * 1000 /*milliseconds*/, null, () => end_time_rule(this, quem), PangyaSyncTimer.TIMER_TYPE.NORMAL);
                        }
                    }

                }

            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[GrandPrix::GameStartTimeRule][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }
      
        public bool GameStopTimeRule(object quem)
        {


            bool ret = true;

            try
            {

                if (quem != null
                    && _grandPrixData.rule > 0
                    && ((eRULE)_grandPrixData.rule == eRULE.TIME_10_SEC || (eRULE)_grandPrixData.rule == eRULE.TIME_15_SEC))
                {


                    var p = (Player)(quem);

                    var timer = _timerManagerRules.findTimer(p);

                    if (timer != null
                        && timer.m_timer != null
                        && timer.m_timer.getState() != PangyaSyncTimer.TIMER_STATE.STOP)
                    {

                        timer.m_timer.Stop();

                        uint time_milli = ((eRULE)_grandPrixData.rule == eRULE.TIME_10_SEC ? 10u : ((eRULE)_grandPrixData.rule == eRULE.TIME_15_SEC ? 15u : 0u));

                    }
                }

            }
            catch (exception e)
            {

                ret = false;

                _smp.LogManager.Instance.push(new AppMessage("[GrandPrix::GameStopTimeRule][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            return ret;
        }

        public void GameTimeRuleIsOver(object quem)
        {

            try
            {

                if (quem != null
                    && _grandPrixData.rule > 0
                    && ((eRULE)_grandPrixData.rule == eRULE.TIME_10_SEC || (eRULE)_grandPrixData.rule == eRULE.TIME_15_SEC))
                {


                    var s = (Player)(quem);

                    try
                    {

                        _grandPrixManager.@lock(s);

                        var timer = _timerManagerRules.findTimer(s);

                        if (timer != null && timer.m_timer != null)
                        {

                            // Para o tempo se ele n o estiver parado
                            if (timer.m_timer.getState() != PangyaSyncTimer.TIMER_STATE.STOP)
                            {

                                timer.m_timer.Stop();
                            }
                            // Atualiza os dados do player que o tempo de Rule acabou
                            var pgi = InitPlayerInfo("timeRuleIsOver",
                            "acabou o tempo do hole do player",
                            s);

                            // Penalidade por que ele n o tacou antes de acabar o tempo Rule
                            if (GameInitState == 1 && pgi.init_shot == 0u)
                            {
                                pgi.data.penalidade++;
                            }

                            uint time_milli = ((eRULE)_grandPrixData.rule == eRULE.TIME_10_SEC ? 10u : ((eRULE)_grandPrixData.rule == eRULE.TIME_15_SEC ? 15u : 0u));
                        }

                        _grandPrixManager.unlock(s);

                    }
                    catch (exception)
                    {
                        //ignorar:  UNREFERENCED_PARAMETER(e);

                        _grandPrixManager.unlock(s);

                        // Relan a para o outro try..catch para mandar a msg no log
                        throw;
                    }

                }

            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[GrandPrix::timeRuleIsOver][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }
          
        public override int OnEndTime(object _arg1, object _arg2)
        { 
            var game = (GrandPrix)(_arg1);

            try
            {

                // Tempo hole acabou
                game.GameTimeIsOver(_arg2);

            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[GrandPrix::end_time][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            return 0;
        }
  
        public int end_time_rule(object _arg1, object _arg2)
        {
            var game = (GrandPrix)(_arg1);

            try
            {
                // Tempo rule acabou
                game.GameTimeRuleIsOver(_arg2);
            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[GrandPrix::end_time_rule][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            return 0;
        }

        public override void GameTimeIsOver()
        {
        }
        #endregion

        #region INIT'S
        public void Init_bots()
        {

            // Achievement Bosts
            uint bots_counter_typeid = 0;

            if (Players.Count == 30)
            {
                bots_counter_typeid = 0x6C4000B8u; // No AI, All player
            }
            else if (Players.Count == 1)
            {
                bots_counter_typeid = 0x6C4000B7u; // All AI, 1 player
            }

            if (bots_counter_typeid > 0u)
            {

                foreach (var el in Players)
                {

                    var pgi = InitPlayerInfo("GrandPrix",
                        "tentou inicializar o counter item do Grand Prix _bots",
                        el);

                    pgi.sys_achieve.incrementCounter(bots_counter_typeid);
                }
            }

            // Inicializa os bots
            if (Players.Count < 30)
            {

                // Média de score (Avg. Score) dos players da sala
                float mediaScoreAllPlayerRoom = Players
                    .Where(p => p != null)
                    .Select(p => p.UserInfo.Statistics.getMediaScore())
                    .DefaultIfEmpty(0.0f)
                    .Average();

                // Lambda que atualiza o bot score com base na média da sala
                Func<GrandPrixData, float, GrandPrixData.BOT> lambdaBotScoreByFactorAvgScoreRoom = (gp, roomAvgScore) =>
                {
                    var bot = new GrandPrixData.BOT();
                    byte qntdHole = (byte)(gp.course_info.Qntd_hole == 0 ? 18 : gp.course_info.Qntd_hole);
                    float mediaBot = ((gp.bot.ScoreBotMin + gp.bot.ScoreBotMed + gp.bot.ScoreBotMax) / 3.0f) * 1.7f;
                    float mediaScorePorHole = (((18.0f / qntdHole) * mediaBot + 72) - roomAvgScore + 180.0f) / 180.0f;

                    int bySign(int scores)
                    {
                        if (scores == 0)
                            return mediaScorePorHole <= 0.8f ? 1 : (mediaScorePorHole >= 1.4f ? -1 : 0);

                        if (scores < 0)
                            return (int)Math.Round(scores * mediaScorePorHole, MidpointRounding.AwayFromZero);

                        return (int)Math.Round(scores / (mediaScorePorHole == 0.0f ? 0.001f : mediaScorePorHole), MidpointRounding.AwayFromZero);
                    }

                    bot.ScoreBotMin = bySign(gp.bot.ScoreBotMin);
                    bot.ScoreBotMed = bySign(gp.bot.ScoreBotMed);
                    bot.ScoreBotMax = bySign(gp.bot.ScoreBotMax);

                    return bot;
                };

                var bot_score = lambdaBotScoreByFactorAvgScoreRoom(_grandPrixData, mediaScoreAllPlayerRoom);

                var qntd = 30u - Players.Count;

                var gp_ai = sIff.Instance.getGrandPrixAIOptionalData();

                LotterySystem lottery = new LotterySystem();

                foreach (var el in gp_ai)
                {

                    if (el.Active == 1 && el.Class == _grandPrixData._class)
                    {
                        lottery.Add(1000u, el.ID);
                    }

                }

                // Verifica se tem a quantidade necessária de bots para sortear
                if (Convert.ToInt64(lottery.getLimitProbilidade() / 1000u) < qntd)
                {

                    var rest_qntd = Convert.ToUInt64(qntd - (long)(lottery.getLimitProbilidade() / 1000));

                    foreach (var el in gp_ai)
                    {

                        if (el.Active == 1 && el.Class == _grandPrixData._class)
                        {
                            lottery.Add(1000u, el.ID);

                            if (--rest_qntd == 0)
                            {
                                break;
                            }
                        }
                    }
                }

                // Sortea os _bots e configura eles
                LotterySystem.LotteryCtx lc = null;
                LotterySystem lottery_score = new LotterySystem();

                PlayerGameInfo tmp_pi = new PlayerGameInfo();

                HoleManager hole = null;

                Bot bot = new Bot();

                int score = 0;
                int min_shot = 0;
                int diff_min_shot = 0;
                int diff_max_shot = 0;
                ulong pang = 0;
                ulong bonus_pang = 0;

                // Media do bot se ele fizer par em todos os holes
                float media_all_parhole = Course.getMediaAllParHolesBySeq(RoomInfo.HoleCount);

                Func<HoleManager, Bot.eTYPE_SCORE, bool, uint> lambdaWindFactor = (mhole, type, sameType) =>
                {
                    uint factor = 1;
                    int wind = mhole.getWind().wind;
                    int weather = mhole.getWeather(); // 2 = Rain ou neve

                    // Vento leve e pontuação máxima
                    if (wind >= 0 && wind < 3 && type == Bot.eTYPE_SCORE.MAX_SCORE)
                    {
                        factor = 2;
                    }
                    // Vento médio e pontuação média
                    else if (wind >= 3 && wind < 6 && type == Bot.eTYPE_SCORE.MED_SCORE)
                    {
                        factor = 4;
                    }
                    // Vento forte e pontuação mínima
                    else if (wind >= 6 && wind < 8 && type == Bot.eTYPE_SCORE.MIN_SCORE)
                    {
                        factor = 6;
                    }
                    // Vento muito forte e pontuação mínima
                    else if (wind >= 8 && type == Bot.eTYPE_SCORE.MIN_SCORE)
                    {
                        factor = 7;
                    }

                    // Se tiver Rain ou neve (weather == 2)
                    if (weather == 2 && (type == Bot.eTYPE_SCORE.MED_SCORE || type == Bot.eTYPE_SCORE.MIN_SCORE))
                    {
                        factor += 2;
                    }

                    if (sameType)
                    {
                        factor += 2;
                    }

                    return factor;
                };

                for (var i = 0; i < qntd; ++i)
                {

                    if ((lc = lottery.SpinRoleta(true)) != null)
                    {
                        bot.id = Convert.ToUInt32(lc.Value);// talvez aqui esteja errado, por causa do lottery

                        bot.type_score = (Random.Shared.Next() % 5 == 0 ? Bot.eTYPE_SCORE.MAX_SCORE : (Random.Shared.Next() % 3 == 0 ? Bot.eTYPE_SCORE.MED_SCORE : Bot.eTYPE_SCORE.MIN_SCORE));

                        bot.max_record = (bot.type_score == Bot.eTYPE_SCORE.MAX_SCORE ? bot_score.ScoreBotMax + (int)(Random.Shared.Next() % 3) : (bot.type_score == Bot.eTYPE_SCORE.MED_SCORE ? bot_score.ScoreBotMed + (int)(Random.Shared.Next(0, 6) - 3) : bot_score.ScoreBotMin + (int)(Random.Shared.Next(0, 5) - 3)));

                        bot.qntd_hole = RoomInfo.HoleCount;

                        for (var j = 0; j < RoomInfo.HoleCount; ++j)
                        {
                            hole = Course.findHoleBySeq((short)(j + 1));
                            if (hole != null)
                            {

                                // Score
                                bot.med_shot_per_hole = (int)Math.Round(((bot.qntd_hole - j + 1) * media_all_parhole + (bot.max_record - bot.record)) / (float)(bot.qntd_hole - j + 1));

                                min_shot = (hole.getPar().par + ((RoomInfo.SpecialModeRoom.IsShotMode) ? -2 /*Short Game*/ : hole.getPar().range_score[0]));

                                if (min_shot >= bot.med_shot_per_hole) // Limite de menor score do hole
                                {
                                    score = min_shot - hole.getPar().par;
                                }
                                else if (bot.med_shot_per_hole >= hole.getPar().total_shot) // Limite de maior score do hole
                                {
                                    score = hole.getPar().total_shot - hole.getPar().par;
                                }
                                else
                                {

                                    lottery_score.Clear();

                                    // Margem que tem para fazer um score melhor
                                    diff_min_shot = (bot.med_shot_per_hole - min_shot);

                                    // Margem que tem para fazer um score pior
                                    diff_max_shot = (hole.getPar().total_shot - bot.med_shot_per_hole);

                                    if (bot.med_shot_per_hole < hole.getPar().par)
                                    {

                                        // min shot, max score
                                        lottery_score.Add((uint)(1000u * diff_max_shot * lambdaWindFactor(hole,
                                            Bot.eTYPE_SCORE.MAX_SCORE,
                                            bot.type_score == Bot.eTYPE_SCORE.MAX_SCORE)), Bot.eTYPE_SCORE.MAX_SCORE);
                                    }

                                    // med
                                    lottery_score.Add((uint)(1000u * bot.med_shot_per_hole * lambdaWindFactor(hole,
                                        Bot.eTYPE_SCORE.MED_SCORE,
                                        bot.type_score == Bot.eTYPE_SCORE.MED_SCORE)), Bot.eTYPE_SCORE.MED_SCORE);

                                    // max shot, min score
                                    lottery_score.Add((uint)(1000u * diff_min_shot * lambdaWindFactor(hole,
                                        Bot.eTYPE_SCORE.MIN_SCORE,
                                        bot.type_score == Bot.eTYPE_SCORE.MIN_SCORE)), Bot.eTYPE_SCORE.MIN_SCORE);

                                    if ((lc = lottery_score.SpinRoleta(true)) == null)
                                    {

                                        _smp.LogManager.Instance.push(new AppMessage("[GrandPrix::init_bots][Warning] nao conseguiu rodar a roleta para o score do bot, usando o med_shot_per_hole.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                                        score = bot.med_shot_per_hole - hole.getPar().par;

                                    }
                                    else
                                    {

                                        if ((Bot.eTYPE_SCORE)lc.Value == Bot.eTYPE_SCORE.MAX_SCORE)
                                        {
                                            score = (bot.med_shot_per_hole - (int)Random.Shared.Next(0, diff_min_shot)) - hole.getPar().par;
                                        }
                                        else if ((Bot.eTYPE_SCORE)lc.Value == Bot.eTYPE_SCORE.MED_SCORE)
                                        {
                                            score = bot.med_shot_per_hole - hole.getPar().par;
                                        }
                                        else
                                        {
                                            score = (bot.med_shot_per_hole + (int)Random.Shared.Next(0, diff_max_shot)) - hole.getPar().par;
                                        }
                                    }
                                }

                                // Pang e Bonus Pang
                                pang = (ulong)(Random.Shared.Next() % (351 * (hole.getWeather() == 2 ? 2 : 1)));
                                bonus_pang = (ulong)Random.Shared.Next() % 200Ul;

                                // Insere o Hole (i) do Bot
                                bot.hole.Add(new Bot.Hole((byte)(RoomInfo.GetMap() & 0x7F),
                                    (uint)hole.GetRoomId(), score, pang,
                                    bonus_pang));

                                // Incrementa no total
                                bot.record += score;
                                bot.pang_total += pang;
                                bot.bonus_pang_total += bonus_pang;

                            } // If CourseIndex->findHole

                            else
                            {
                                _smp.LogManager.Instance.push(new AppMessage($"[GrandPrix::init_bots][ERROR] Hole não encontrado para seq {(j + 1)}", type_msg.CL_FILE_LOG_AND_CONSOLE));
                                break;
                            }
                        } // For Hole Bot

                        if (bot.qntd_hole != (byte)bot.hole.Count)
                        {
                            _smp.LogManager.Instance.push(new AppMessage("[GrandPrix::init_bots][WARNIG] Bot[ID=" + Convert.ToString(bot.id) + ", HOLE_QNTD_INIT=" + Convert.ToString(bot.hole.Count) + ", HOLE_QNTD_GP=" + Convert.ToString((ushort)bot.qntd_hole) + "] qntd de holes inicializado esta diferente da quantidade de holes da sala Grand Prix. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                        }

                        tmp_pi = new PlayerGameInfo();

                        tmp_pi.flag = PlayerGameInfo.eFLAG_GAME.BOT;
                        tmp_pi.data.score = bot.record;
                        tmp_pi.data.pang = bot.pang_total;
                        tmp_pi.data.bonus_pang = bot.bonus_pang_total;

                        bot.pi = tmp_pi;

                        // Add bot ao vector
                        _bots.Add(bot);
                        // Clear Bot para new data
                        bot = new Bot();

                    } // If lottery.spinRoleta

                } // For Rest of Bot

                _bots.Sort((bot1, bot2) =>
                {
                    if (bot1.record == bot2.record)
                        return bot2.pang_total.CompareTo(bot1.pang_total); // decrescente
                    return bot1.record.CompareTo(bot2.record); // crescente
                });

            } // If players.size < 30
        }


        #endregion

        #region PLAYER-RANK
        public override void CalculeRankPlace()
        {

            if (PlayerOrder.Count > 0)
            {
                PlayerOrder.Clear();
            }

            foreach (var el in PlayerInfo.ToArray())
            {
                if (el.Value.flag != PlayerGameInfo.eFLAG_GAME.QUIT) // menos os que quitaram
                {
                    PlayerOrder.Add(el.Value);
                }
            }

            // Add os _bots
            foreach (var el in _bots.ToArray())
            {
                PlayerOrder.Add(el.pi);
            }
            PlayerOrder.Sort(SortPlayerRank);
        }

        public void MakeRankPlayerDisplayCharacter()
        {

            if (PlayerOrder.Count <= 0)
            {
                CalculeRankPlace();
            }

            RankPlayerDisplayChracter rpdc = new RankPlayerDisplayChracter();

            Player p = null;

            // Top 3
            for (var i = 0; i < PlayerOrder.Count && i < 3u; ++i)
            {

                if (PlayerOrder[i].flag != PlayerGameInfo.eFLAG_GAME.BOT)
                {

                    if ((p = FindSessionByUID(PlayerOrder[i].uid)) != null)
                    {

                        rpdc = new RankPlayerDisplayChracter();

                        rpdc.uid = p.Inventory.uid;
                        rpdc.rank = (uint)(i + 1);

                        if (p.Inventory.UserEquippedItem.CharacterEquiped != null)//no meu antigo, esta null no packet, agora ta preenchendo
                        {

                            rpdc.default_hair = p.Inventory.UserEquippedItem.CharacterEquiped.default_hair;
                            rpdc.default_shirts = p.Inventory.UserEquippedItem.CharacterEquiped.default_shirts;
                            Array.Copy(
                                p.Inventory.UserEquippedItem.CharacterEquiped.parts_typeid, // origem
                                rpdc.parts_typeid,                // destino
                                rpdc.parts_typeid.Length          // tamanho (em elementos, não bytes)
                            );

                            Array.Copy(
                                p.Inventory.UserEquippedItem.CharacterEquiped.auxparts,
                                rpdc.auxparts,
                                rpdc.auxparts.Length
                            );

                            Array.Copy(
                                p.Inventory.UserEquippedItem.CharacterEquiped.parts_id,
                                rpdc.parts_id,
                                rpdc.parts_id.Length
                            );

                        }

                        // Add para o vector
                        _characterRankDisplay.Add(rpdc);
                    }
                }
            }

            _characterRankDisplay.Sort((a, b) => a.rank.CompareTo(b.rank));

        }
        #endregion

        #region OVERRIDES METHODS

        public override bool InitRoomGame()
        {

            if (Players.Count > 0)
            {
                // variavel que salva a data local do sistema
                InitGameTime();

                GameInitState = 1; // comecou

                _initGrandPrixState = true; //comecou o grandprix
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
                            _smp.LogManager.Instance.push(new AppMessage("[GameModeGrandPrix::checkEndShotOfHole][Error][Warning] tentou pegar o Map dados estaticos do CourseIndex[COURSE=" + Convert.ToString((ushort)((byte)(RoomInfo.CourseIndex & RoomCourseFlags.UNK))) + "], mas nao conseguiu encontra na classe do Server.", type_msg.CL_FILE_LOG_AND_CONSOLE));
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
            else
            {
                ClearAllShotPacket(session);
            }
            return 0;
        }

        public void Finish()
        {

            GameInitState = 2; // Acabou

            // J  est  com os bots incluso, fiz um overide dessa fun  o
            CalculeRankPlace();

            MakeRankPlayerDisplayCharacter();

            FinishExpGame();

            foreach (var el in Players)
            {

                var pgi = InitPlayerInfo("finish",
                    "tentou finalizar os dados do jogador no jogo",
                    el);

                if (pgi.flag != PlayerGameInfo.eFLAG_GAME.QUIT)
                {
                    FinishData(el);
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

            // GrandPrix
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
            { // Course GrandPrix

                pgi.data.pang = (ulong)(pgi.data.pang * (1.0f / 3.0f));
                pgi.data.bonus_pang = (ulong)(pgi.data.bonus_pang * (1.0f / 3.0f));
            }
        }


        #endregion

        #region HELP'S
        public void ConsumeTicket()
        {

            foreach (var session in Players.ToArray())
            {

                try
                {
                    if (session.Inventory == null)
                    {
                        throw new exception("[GrandPrix::consomeTicket][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou comecar o jogo na sala[NUMERO=" + Convert.ToString(RoomInfo.RoomID) + ", MASTER=" + Convert.ToString(RoomInfo.OwnerUID) + "], mas o player nao tem a quantidade de Ticket[TYPEID=" + Convert.ToString(_grandPrixData.ticket._typeid) + ", REQ_QNTD=" + Convert.ToString(_grandPrixData.ticket.qntd) + ", HAVE_QNTD=" + 0 + "] para jogar o Grand Prix. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GRAND_PRIX,
                            10, 0x5900203));
                    }

                    // Tira o ticket Grand Prix do player
                    var pWi = session.Inventory.FindWarehouseItemByTypeid(_grandPrixData.ticket._typeid) ?? throw new exception("[GrandPrix::consomeTicket][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou comecar o jogo na sala[NUMERO=" + Convert.ToString(RoomInfo.RoomID) + ", MASTER=" + Convert.ToString(RoomInfo.OwnerUID) + "], mas o player nao tem o Ticket[TYPEID=" + Convert.ToString(_grandPrixData.ticket._typeid) + "] para jogar o Grand Prix. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GRAND_PRIX,
                            9, 0x5900203));

                    if (pWi.STDA_C_ITEM_QNTD < (short)_grandPrixData.ticket.qntd)
                    {
                        throw new exception("[GrandPrix::consomeTicket][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou comecar o jogo na sala[NUMERO=" + Convert.ToString(RoomInfo.RoomID) + ", MASTER=" + Convert.ToString(RoomInfo.OwnerUID) + "], mas o player nao tem a quantidade de Ticket[TYPEID=" + Convert.ToString(_grandPrixData.ticket._typeid) + ", REQ_QNTD=" + Convert.ToString(_grandPrixData.ticket.qntd) + ", HAVE_QNTD=" + Convert.ToString(pWi.STDA_C_ITEM_QNTD) + "] para jogar o Grand Prix. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GRAND_PRIX,
                            10, 0x5900203));
                    }

                    stItem item = new stItem
                    {
                        type = 2,
                        id = pWi.id,
                        _typeid = pWi._typeid,
                        qntd = (int)_grandPrixData.ticket.qntd
                    };
                    item.STDA_C_ITEM_QNTD = (item.qntd * -1);

                    // Update On Server And Database
                    if (ItemManager.removeItem(item, session) <= 0)
                    {
                        throw new exception("[GrandPrix::consomeTicket][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou comecar o jogo na sala[NUMERO=" + Convert.ToString(RoomInfo.RoomID) + ", MASTER=" + Convert.ToString(RoomInfo.OwnerUID) + "], mas nao conseguiu excluir o Ticket[TYPEID=" + Convert.ToString(item._typeid) + "] do player. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GRAND_PRIX,
                            11, 0x5900203));
                    }


                    // Update Grand Prix Ticket do player no jogo
                    var p = new Packet(0x216);

                    p.WriteUInt32((uint)UtilTime.GetSystemTimeAsUnix());
                    p.WriteUInt32(1); // Count

                    p.WriteByte(item.type);
                    p.WriteUInt32(item._typeid);
                    p.WriteInt32(item.id);
                    p.WriteUInt32(item.flag_time);
                    p.WriteBytes(item.stat.ToArray());
                    p.WriteInt32((item.STDA_C_ITEM_TIME > 0) ? item.STDA_C_ITEM_TIME : item.STDA_C_ITEM_QNTD);
                    p.WriteZero(25);
                    session.Send(p);
                }
                catch (exception e)
                {

                    _smp.LogManager.Instance.push(new AppMessage("[GrandPrix::consomeTicket][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                    if (ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) != STDA_ERROR_TYPE.GRAND_PRIX)
                    {
                        throw;
                    }
                }

            }
        }

        public void FinishExpGame()
        {

            if (Players.Count > 0)
            {
                float stars = Course.getStar();
                int exp = 0;
                 
                // Exp padr�o de hole do Grand Prix
                switch (RoomInfo.HoleCount)
                {
                    case 3:
                        exp = 2;
                        break;
                    case 6:
                        exp = 4;
                        break;
                    case 9:
                        exp = 5;
                        break;
                    case 18:
                        exp = 7;
                        break;
                    default:
                        exp = 1;
                        break;
                }

                stars = (stars < 1.1f) ? 1.1f : stars;

                stars = ((stars - 1.1f) * 0.125f) + 1.0f;

                exp = (int)(exp * stars);
                // Grand Prix Rookie d  um pouco menos de experi ncia
                if (sIff.Instance.getGrandPrixAbaType(_grandPrixData.ID) == GrandPrixData.GP_ABA.ROOKIE && sIff.Instance.isGrandPrixNormal(_grandPrixData.ID))
                {
                    exp = (int)(exp * 0.12f);
                }

                for (var i = 0; i < PlayerOrder.Count; ++i)
                {
                    Player session = default;
                    if (PlayerOrder[i].flag != PlayerGameInfo.eFLAG_GAME.BOT
                        && PlayerOrder[i].flag == PlayerGameInfo.eFLAG_GAME.FINISH
                        && (session = FindSessionByUID(PlayerOrder[i].uid)) != null)
                    {

                        // Rate do player e do server
                        exp = (int)(exp * TRANSF_SERVER_RATE_VALUE(PlayerOrder[i].used_item.rate.exp) * TRANSF_SERVER_RATE_VALUE(RateValue.exp));

                        // Exp que o player ganhou
                        if (PlayerOrder[i].level < 70 /*Ultimo Level n o ganha Experience*/)
                        {
                            PlayerOrder[i].data.exp = exp;
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

            RainHoleSeqCount(session); // conta os achievement de Rain em holes consecutivas
            ScoreSeqCount(session); // conta os achievement de back-to-back(2 ou mais score iguais consecutivos) do player

            RainCount(session); // Aqui achievement de rain count

            AchievementTop3_1st(session); // Se o Player ficou em Top 3 add +1 ao contador de top 3, e se ele ficou em primeiro add +1 ao do primeiro

            var pgi = InitPlayerInfo("requestFinishData",
                "tentou finalizar dados do jogo",
                session);

            // Resposta terminou game - Drop Itens
            SendDropItem(session);

            // Resposta terminou game - Placar
            SendPlacar(session);

            // Aqui   os 3 player do podio, mas s  player bot n o vai n o
            SendRankPlayerDisplayCharacter(session);

            // Trof u que o player ganhou
            SendTrofel(session);

            // Envia os pr mios que o player ganhou no Grand Prix
            SendRewardRankAndGrandPrix(session);

            // Verifica se o player concluiu esse Grand Prix em um posi  o melhor, 
            // se sim atualiza no server, db e no jogo
            SaveGrandPrixClear(session);
        }


        public void SaveGrandPrixClear(Player session)
        {

            try
            {

                if (PlayerOrder.Count <= 0)
                {
                    CalculeRankPlace();
                }

                var it = PlayerOrder.FindIndex(el => el.uid == session.UserInfo.UID);
                var position = it != -1 ? it + 1 : 0; // Se não encontrar, posição 0 (equivalente a não encontrado)

                // Atualiza Grand Prix Clear do player
                if (session.UserInfo.updateGrandPrixClear(_grandPrixData.TypeID_Link, position))
                {

                    // Update On Game
                    var p = new Packet(0x25A);

                    p.WriteUInt32(0); // OK;

                    p.WriteUInt32(_grandPrixData.TypeID_Link);
                    p.WriteUInt32((uint)position);
					session.Send(p);
				}

            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[requestSaveGrandPrixClear][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }


        public void SendTrofel(Player session)
        {

            int allplayer = GetCountPlayersGame();

            if (PlayerOrder.Count <= 0)
            {
                CalculeRankPlace();
            }
            if (PlayerOrder.Count != (allplayer + _bots.Count))
            {

                _smp.LogManager.Instance.push(new AppMessage("[GrandPrix::sendTrofel][Error] VALUES[ORDER=" + Convert.ToString(PlayerOrder.Count) + ", INFO=" + Convert.ToString(PlayerInfo.Count) + ", BOT=" + Convert.ToString(_bots.Count) + "] nao conseguiu gerar os trofeus por que o vector de player RankPosition order nao bate com o dos players no jogo", type_msg.CL_FILE_LOG_AND_CONSOLE));

                return;
            }

            // D  os Trof us para os 3 primeiros Player, bots est o exclu dos
            if (_reward.Count > 0)
            {

                GrandPrixRankReward gprr = new GrandPrixRankReward();
                stItem item = new stItem();

                var p = new Packet();

                var it = PlayerOrder.FirstOrDefault(el => el.uid == session.UserInfo.UID);

                if (it != null)
                {

                    try
                    {
                        int index = PlayerOrder.FindIndex(el => el.uid == session.UserInfo.UID);
                        if (index >= 0 && index < _reward.Count && _reward.Count > 0)//verifica se tem premios
                        {
                            gprr = _reward[index];

                            // Inicializa o Trof u
                            item.type = 2;
                            item.id = -1;
                            item._typeid = gprr.Trophy;
                            item.qntd = 1;
                            item.STDA_C_ITEM_QNTD = (short)item.qntd;

                            // Update on Server and Database
                            if (ItemManager.addItem(item, session, 0, 0) >= RetAddItem.SUCCESS)
                            {
                                // Update Trof u on Game
                                p.init_plain(0x25C);

                                p.WriteUInt32(0); // OK;

                                p.WriteUInt32(gprr.Trophy);
								session.Send(p);

								// Update Item on Game (Trof u)
								p.init_plain(0x216);

                                p.WriteUInt32((uint)UtilTime.GetSystemTimeAsUnix());
                                p.WriteUInt32(1u); // Count

                                p.WriteByte(item.type);
                                p.WriteUInt32(item._typeid);
                                p.WriteInt32(item.id);
                                p.WriteUInt32(item.flag_time);
                                p.WriteBytes(item.stat.ToArray());
                                p.WriteInt32((item.STDA_C_ITEM_TIME > 0) ? item.STDA_C_ITEM_TIME : item.STDA_C_ITEM_QNTD);
                                p.WriteZero(25); 
                                session.Send(p);  
                            }
                        }
                        else
                        {
                            _smp.LogManager.Instance.push(new AppMessage("[GrandPrix::sendTrofel][Log] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] nao tem trofeu nessa room.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                        }
                    }
                    catch (IndexOutOfRangeException e)
                    {

                        _smp.LogManager.Instance.push(new AppMessage("[GrandPrix::sendTrofel][IndexOutOfRangeException] " + e.StackTrace, type_msg.CL_FILE_LOG_AND_CONSOLE));
                    }
                    catch (exception e)
                    {

                        _smp.LogManager.Instance.push(new AppMessage("[GrandPrix::sendTrofel][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                    }

                }
                else
                {
                    _smp.LogManager.Instance.push(new AppMessage("[GrandPrix::sendTrofel][Warning] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] nao esta no vector de player order.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
            }
        }

        public void SendRankPlayerDisplayCharacter(Player session)
        {

            // 3 Players do P dio
            var p = new Packet(0x258);

            p.WriteUInt32(0u); // OK

            p.WriteByte((byte)_characterRankDisplay.Count); // count

            foreach (var el in _characterRankDisplay.ToArray())
            {
                p.WriteBytes(el.ToArray());
            }

			session.Send(p);
		}

        public void SendRewardRankAndGrandPrix(Player session)
        {
            // 1. Garante que o RankPosition foi calculado
            if (PlayerOrder.Count <= 0)
            {
                CalculeRankPlace();
            }

            // 2. Encontra a info do player no ranking do jogo atual
            PlayerGameInfo it = PlayerOrder.Find(el => el.uid == session.UserInfo.UID);

            if (it != null && _grandPrixData != null && _grandPrixData.ID != 0)
            {
                List<stItem> v_item = new List<stItem>();

                // --- PARTE A: RECOMPENSA GERAL DO GRAND PRIX (PARTICIPAÇÃO) ---
                for (int i = 0; i < 5; i++)
                {
                    if (_grandPrixData.reward._typeid[i] == 0) continue;

                    // Instancia um novo objeto stItem para não sobrescrever referências
                    stItem itemGP = new stItem
                    {
                        type = 2,
                        id = -1,
                        _typeid = _grandPrixData.reward._typeid[i]
                    };

                    // Configuração de Tempo ou Quantidade
                    if (_grandPrixData.reward.time[i] > 0)
                    {
                        itemGP.qntd = 1;
                        itemGP.STDA_C_ITEM_QNTD = 1;
                        itemGP.STDA_C_ITEM_TIME = (short)_grandPrixData.reward.time[i];
                        itemGP.flag_time = 4; // Dias
                        itemGP.flag = 0x40;   // ServerFlag de tempo
                    }
                    else
                    {
                        itemGP.qntd = (int)_grandPrixData.reward.qntd[i];
                        itemGP.STDA_C_ITEM_QNTD = (short)itemGP.qntd;
                    }

                    // Adiciona se puder acumular ou se o player não possuir
                    if ((sIff.Instance.IsCanOverlapped(itemGP._typeid) && sIff.Instance.getItemGroupIdentify(itemGP._typeid) != IFF_GROUP.CAD_ITEM) || !session.Inventory.ownerItem(itemGP._typeid))
                    {
                        if (ItemManager.isSetItem(itemGP._typeid))
                        {
                            var v_stItem = ItemManager.GetItemOfSetItem(session, itemGP._typeid, false, 1);
                            foreach (var el in v_stItem) v_item.Add(new stItem(el));
                        }
                        else
                        {
                            v_item.Add(itemGP);
                        }
                    }
                }

                // --- PARTE B: RECOMPENSA DE RANK (POSIÇÃO) ---
                int index = PlayerOrder.FindIndex(el => el.uid == session.UserInfo.UID);

                // CORREÇÃO: index >= 0 permite que o 1º lugar (índice 0) receba o prêmio
                if (index >= 0 && index < _reward.Count && _reward.Count > 0)//verifica se tem premios
                {
                    GrandPrixRankReward gprr = _reward[index];

                    if (gprr != null && gprr.ID != 0)
                    {
                        for (int i = 0; i < 5; i++)
                        {
                            if (gprr.reward._typeid[i] == 0) continue;

                            stItem itemRank = new stItem
                            {
                                type = 2,
                                id = -1,
                                _typeid = gprr.reward._typeid[i]
                            };

                            if (gprr.reward.time[i] > 0)
                            {
                                itemRank.qntd = 1;
                                itemRank.STDA_C_ITEM_QNTD = 1;
                                itemRank.STDA_C_ITEM_TIME = (short)gprr.reward.time[i];
                                itemRank.flag_time = 4;
                                itemRank.flag = 0x40;
                            }
                            else
                            {
                                itemRank.qntd = (int)gprr.reward.qntd[i];
                                itemRank.STDA_C_ITEM_QNTD = (short)itemRank.qntd;
                            }

                            // Verifica posse do item de RankPosition
                            if ((sIff.Instance.IsCanOverlapped(itemRank._typeid) && sIff.Instance.getItemGroupIdentify(itemRank._typeid) != IFF_GROUP.CAD_ITEM) || !session.Inventory.ownerItem(itemRank._typeid))
                            {
                                if (ItemManager.isSetItem(itemRank._typeid))
                                {
                                    var v_stItemRank = ItemManager.GetItemOfSetItem(session, itemRank._typeid, false, 1);
                                    foreach (var el in v_stItemRank) v_item.Add(new stItem(el));
                                }
                                else
                                {
                                    v_item.Add(itemRank);
                                }
                            }
                        }
                    }
                }

                // --- PARTE C: PROCESSAMENTO E ENVIO ---
                if (v_item.Count > 0)
                {
                    // 1. Adiciona no Banco de Dados e Memória (O addItem que corrigimos o loop de remoção)
                    var rai = ItemManager.addItem(v_item, session.GetUID(), 0, 0);

                    if (rai.fails.Count > 0 && rai.type != RetAddItem.SUCCESS_PANG_AND_EXP_AND_CP_POUCH)
                    {
                        _smp.LogManager.Instance.push(new AppMessage($"[GP:Reward][WARNING] Falha ao adicionar {rai.fails.Count} itens para UID: {session.GetUID()}", type_msg.CL_FILE_LOG_AND_CONSOLE));
                    }

                    // 2. Monta e envia o pacote 0x216 (Update Item no Cliente)
                    var p = new Packet(0x216);
                    p.WriteUInt32((uint)UtilTime.GetSystemTimeAsUnix());
                    p.WriteUInt32((uint)v_item.Count);

                    foreach (var el in v_item)
                    {
                        p.WriteByte(el.type);
                        p.WriteUInt32(el._typeid);
                        p.WriteInt32(el.id); // Importante: O addItem deve ter atualizado esse Login com o do DB
                        p.WriteUInt32(el.flag_time);
                        p.WriteBytes(el.stat.ToArray());
                        // Se for tempo, envia o tempo, senão a quantidade
                        p.WriteInt32((el.STDA_C_ITEM_TIME > 0) ? el.STDA_C_ITEM_TIME : el.STDA_C_ITEM_QNTD);
                        p.WriteZero(25);
                    }

					session.Send(p);
				}
            }
            else
            {
                _smp.LogManager.Instance.push(new AppMessage($"[GrandPrix::sendReward] Erro: Player UID {session.UserInfo.UID} ou GP m_gp inválidos.", type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void SendAllToNextHole()
        { 
            SendBroadCast(new Packet(0x255));
        }

        public int ChangeTurn(Player session)
        {

            try
            {

                if (CheckAllShotPacket(session))
                {

                    var pgi = InitPlayerInfo("changeTurn",
                        "tentou trocar o turno do player",
                        session);

                    // Agora verifica o se ele acabou o hole e essas coisas
                    if (pgi.shot_sync.state_shot.display.acerto_hole
                        || pgi.data.giveup > 0
                        || pgi.data.time_out > 0)
                    {

                        if (pgi.data.bad_condute >= 3)
                        { // Kika player deu 3 give up

                            // !!@@@
                            // Tira o player da sala
                            return 2;
                        }

                        // Verifica se o player terminou jogo, fez o ultimo hole
                        if (Course.findHoleSeq(pgi.hole) == RoomInfo.HoleCount)
                        {

                            // Resposta para o player que terminou o ultimo hole do Game
                            var p = new Packet((ushort)0x199);

                            session.Send(p);

                            // Fez o Ultimo Hole, Calcula Clear Bonus para o player
                            if (pgi.shot_sync.state_shot.display.clear_bonus)
                            {

                                if (!MapSystem.Instance.isLoad())
                                {
                                    MapSystem.Instance.load();
                                }

                                var map = MapSystem.Instance.getMap((byte)(RoomInfo.GetMap() & 0x7F));

                                if (map == null)
                                {
                                    _smp.LogManager.Instance.push(new AppMessage("[TourneyBase::checkEndShotOfHole][Error][Warning] tentou pegar o Map dados estaticos do CourseIndex[COURSE=" + Convert.ToString((ushort)((byte)(RoomInfo.GetMap() & 0x7F))) + "], mas nao conseguiu encontra na classe do Server.", type_msg.CL_FILE_LOG_AND_CONSOLE));
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
                    else
                    {
                        ClearAllShotPacket(session);
                    }

                } // Wait ele ainda n o terminou de enviar todos os pacotes da tacada

            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[GrandPrix::changeTurn][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            return 0;
        }
         
        public override bool FinishGame(Player session, int option)
        {

            if (session.getState()
                && session.Connected
                && Players.Count > 0)
            {

                var p = new Packet();

                if (option == 6 /*packet06 pacote que termina o game*/)
                {

                    if (_initGrandPrixState)
                    {
                        FinishGrandPrix(session, 1); // Termina sem ter acabado de jogar
                    }

                    var pgi = InitPlayerInfo("finish_game",
                        "tentou terminar o jogo",
                        session);

                    // Rookie Grand Prix n o altera o info do player s  achievement
                    if (!(sIff.Instance.getGrandPrixAbaType(_grandPrixData.ID) == GrandPrixData.GP_ABA.ROOKIE && sIff.Instance.isGrandPrixNormal(_grandPrixData.ID)))
                    {

                        // Salve o record se o camp acabou e o player n o terminou todos os holes tbm tem que salvar o record [OK][Feito]
                        RequestSaveRecordCourse(session,
                            52 /*Grand Prix*/,
                            (RoomInfo.HoleCount == 18 && (Course.findHoleSeq(pgi.hole) == 18 || pgi.flag == PlayerGameInfo.eFLAG_GAME.END_GAME)) ? 1 : 0);

                        RequestSaveInfo(session, 0);
                    }

                    // D  Exp para o Caddie E Mascot Tamb m
                    if (pgi.data.exp > 0)
                    { // s  add Experience se for maior que 0

                        // Add Exp para o player
                        session.addExp(pgi.data.exp, false /*N o precisa do pacote para trocar de Level*/);

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

                    p.WriteUInt64(0Ul); 
                    session.Send(p);

                    // Colocar o finish_game Para 1 quer dizer que ele acabou o camp
                    pgi.finish_game = 1;

                    // ServerFlag do game que terminou
                    GameInitState = 2; // ACABOU

                }
            }

            return (PlayersCompleteGameAndClear() && _initGrandPrixState);
        }

        void OncePerShot(string method, string msg, string flagFieldName, Player session, out PlayerGameInfo pgi, Action action)
        {
            InitPlayerInfo(method, msg, session, out pgi);

            _grandPrixManager.@lock(session);

            try
            {
                var flagField = pgi.GetType().GetField(flagFieldName);
                if (flagField == null)
                {
                    throw new Exception($"[GrandPrix::{method}][Error] Campo '{flagFieldName}' não encontrado no PlayerGameInfo.");
                }

                var flagObj = flagField.GetValue(pgi);

                if (Convert.ToByte(flagObj) > 0)
                {
                    _smp.LogManager.Instance.push(new AppMessage(
                        $"[GrandPrix::{method}][Error] Normal[UID={session.UserInfo.UID}] já enviou esse pacote, ignorando.",
                        type_msg.CL_FILE_LOG_AND_CONSOLE));

                    action?.Invoke(); // Só chama se não for null
                    return;
                }

                // Marca como executado (usa o Type original pra evitar problemas de compatibilidade)
                if (flagObj is byte)
                    flagField.SetValue(pgi, (byte)1);
                else if (flagObj is uint)
                    flagField.SetValue(pgi, 1u);
            }
            finally
            {
                _grandPrixManager.unlock(session);
            }
        }
        #endregion

        #region CLEAR SHOT

        public bool CheckAllShotPacket(Player session)
        {

            bool ret = false;

            try
            {

                var pgi = InitPlayerInfo("checkAllShotPacket",
                    "tentou verificar as PCBangMascot de sincronizacao de tacada do player",
                    session);

                _grandPrixManager.@lock(session);

                ret = ((pgi.init_shot > 0 && pgi.sync_shot_flag > 0 || pgi.data.time_out > 0) && pgi.finish_shot > 0);

                _grandPrixManager.unlock(session);

            }
            catch (exception e)
            {

                _grandPrixManager.unlock(session);

                _smp.LogManager.Instance.push(new AppMessage("[GrandPrix::checkAllShotPacket][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            return ret;
        }

        public void ClearAllShotPacket(Player session)
        {

            try
            {

                var pgi = InitPlayerInfo("clearAllShotPacket",
                    "tentou limpar as PCBangMascot de sincronizacao de tacada do player",
                    session);

                // Limpa as veriaveis da tacada
                _grandPrixManager.@lock(session);

                pgi.init_shot = 0;
                pgi.sync_shot_flag = 0;
                pgi.finish_shot = 0;

                _grandPrixManager.unlock(session);

            }
            catch (exception e)
            {

                _grandPrixManager.unlock(session);

                _smp.LogManager.Instance.push(new AppMessage("[GrandPrix::clearAllShotPacket][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public bool CheckAllClearHole()
        { 
            uint count = 0;
            foreach (var el in Players.ToArray())
            {
                try
                {
                    var pgi = InitPlayerInfo("checkAllClearHole",
                        "tentou verificar se todos os player terminaram o hole no jogo",
                        el);
                    if (pgi.finish_hole > 0)
                    {
                        count++;
                    }
                }
                catch (exception e)
                {
                    _smp.LogManager.Instance.push(new AppMessage("[GrandPrix::checkAllClearHole][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
            }
            return (count == Players.Count);
        }
         
        public bool CheckAllHoleAndClear()
        { 
            uint count = 0; 
            // Check
            foreach (var el in Players.ToArray())
            {
                try
                {
                    var pgi = InitPlayerInfo("CheckAllHoleAndClear",
                        "tentou verificar se todos os player terminaram o hole no jogo",
                        el);
                    if (pgi.finish_hole > 0)
                    {
                        count++;
                    }
                }
                catch (exception e)
                {
                    _smp.LogManager.Instance.push(new AppMessage("[GrandPrix::CheckAllHoleAndClear][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
            } 
            // Clear
            if (count >= Players.Count)
            {
                ClearAllHole();
            }

            return count >= Players.Count;
        }

        public void ClearAllHole()
        { 
            foreach (var el in Players)
            {
                try
                {
                    var pgi = InitPlayerInfo("clear_all_clear_hole",
                        " tentou limpar all clear hole no jogo",
                        el);
                    pgi.finish_hole = 0;
                    pgi.shot_sync.state_shot.display.acerto_hole = false;
                    pgi.data.giveup = 0;
                }
                catch (exception e)
                {
                    _smp.LogManager.Instance.push(new AppMessage("[GrandPrix::clear_all_hole][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
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
                _initGrandPrixState = false;

                if (GameInitState != 2)
                {
                    Finish();
                }

                // Aguarda com timeout máximo para evitar bloqueio infinito no Dispose.
                int maxWaitMs = 5000;
                int waited = 0;
                while (!PlayersCompleteGameAndClear() && waited < maxWaitMs)
                {
                    Thread.Sleep(500);
                    waited += 500;
                }

                DeleteAllPlayer();

                if (_bots.Count > 0)
                {
                    _bots.Clear();
                }

                // Clear timers
                ClearGameTimers();
                LogDestruction();
            }
            base.Dispose(disposing);
        }

        public void ClearGameTimers()
        {
            // Begin Timer Hole
            _timerManager.@lock();
            try
            {
                foreach (var el in _timerManager.getTimers())
                {
                    if (el.m_timer != null)
                        GameServer.Instance.DeleteTimer(el.m_timer);
                }
            }
            finally
            {
                _timerManager.unlock();
            }

            // Begin Timer Rule
            _timerManagerRules.@lock();
            try
            {
                foreach (var el in _timerManagerRules.getTimers())
                {
                    if (el.m_timer != null)
                        GameServer.Instance.DeleteTimer(el.m_timer);
                }
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[GrandPrix::ClearGameTimers][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
            finally
            {
                _timerManagerRules.unlock();
            }

            // Reinicia os timers
            _timerManager = new TimerManager();
            _timerManagerRules = new TimerManager();
        }

        ~GrandPrix()
        {
            Dispose(false);
        }
        #endregion
    }
}
