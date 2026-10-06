using Pangya_GameServer.Engine;
using Pangya_GameServer.Feature;
using Pangya_GameServer.Flags;
using Pangya_GameServer.Manager;
using Pangya_GameServer.Models.Game;
using Pangya_GameServer.PacketFunc;
using Pangya_GameServer.Repository;
using Pangya_GameServer.Roms.GameBase.Modes;
using Pangya_GameServer.Server;
using Pangya_GameServer.Session;
using PangyaAPI.IFF.Flags;
using PangyaAPI.IFF.Handle.JP;
using PangyaAPI.Network;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using PangyaAPI.Utilities.Models;
using snmdb;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pangya_GameServer.Roms.GameModes
{
    /// <summary>
    /// SpecialShuffleCourse
    /// </summary>
    public class SSC : TourneyBase
    {
        private bool _SSCState;
        private uint m_coin_SSC;        // Que o Master da sala ganha se ficar at� o final
        private uint SPECIAL_SHUFFLE_COURSE_COIN_TYPEID = 0x1A0000F8;
        private uint ART_ROGER_K_STEERING_WHEEL = 0x1A0001BCu;	// de 500 a 501000 pangs no Ultimo Hole do game de 18H
        public SSC(List<Player> players, GameRoomInfoModel roomInfo, RateValue rateValue) : base(players, roomInfo, rateValue)
        {
            _SSCState = false; 

            UpdateTreasureHunterSystem();

            // Aqui tem que inicializar os players info
            InitAllPlayerInfo();

            InitAllAchievementPlayers(0x6C40001Fu); // Por que ele um Tourney

            State = InitRoomGame();
        }

        #region REQUEST
        public override DropItemRet RequestInitDrop(Player _session)
        {

            try
            {

                var dir = base.RequestInitDrop(_session);

                var pgi = InitPlayerInfo("requestInitDrop",
                    "tentou sortear o Drop do SSC no jogo",
                    _session);

                // Verifica se   o ultimo hole e sortea os pangs do final do SSC
                // Artefact Pang Drop
                if (RoomInfo.HoleCount == Course.findHoleSeq(pgi.hole) && RoomInfo.HoleCount == 18)
                { // Ultimo Hole, de 18h Game

                    DropSystem.stCourseInfo ci = new DropSystem.stCourseInfo();

                    // Init Course Info Drop System
                    ci.artefact = ART_ROGER_K_STEERING_WHEEL; // Para da os Pangs do SSC   o mesmo que o artefact
                    ci.char_motion = pgi.char_motion_item;
                    ci.course = (byte)(RoomInfo.GetMap() & 0x7F);
                    ci.hole = pgi.hole;
                    ci.qntd_hole = RoomInfo.HoleCount;

                    var art_pang = sDropSystem.Instance.drawArtefactPang(ci, (uint)Players.Count);

                    if (art_pang._typeid != 0)
                    { // Dropou

                        dir.v_drop.Add(art_pang);

                        // add para o drop list do player
                        pgi.drop_list.v_drop.Add(art_pang);

                        if (art_pang.qntd >= 30)
                        { // Envia notice que o player ganhou jackpot

                            var p = new Packet((ushort)0x40);

                            p.WriteByte(10); // JackPot

                            p.WriteString(_session.UserInfo.NickName);

                            p.WriteUInt16(0); // size Msg

                            p.WriteUInt32((uint)(art_pang.qntd * 500));
                            SendBroadCast(p); 
                        }
                    }
                }
                return dir;
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[SpecialShuffleCourse::RequestInitDrop][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            return new DropItemRet();
        }

        public override void RequestUpdateItemUsedGame(Player _session)
        {

            var pgi = InitPlayerInfo("requestUpdateItemUsedGame",
                "tentou atualizar itens usado no jogo",
                _session);

            var ui = pgi.used_item;

            ui.club.count += (uint)(1.5f * 10.0f * ui.club.rate * TRANSF_SERVER_RATE_VALUE(RateValue.clubset) * TRANSF_SERVER_RATE_VALUE(ui.rate.club));

            // Passive Item exceto Time Booster e Auto Command, que soma o contador por uso, o cliente passa o pacote, dizendo que usou o item
            foreach (var el in ui.v_passive)
            {
                // Passive Item no SSC só consome os item boost de Pang e o Club Mastery Boost,
                // Consome todos os outros menos os de Experiência
                if (DefineConstants.passive_item_exp.Any(c => c == el.Value._typeid))
                {
                    if (DefineConstants.CHECK_PASSIVE_ITEM(el.Value._typeid)
                    && el.Value._typeid != DefineConstants.TIME_BOOSTER_TYPEID/* / *Time Booster * /*/ && el.Value._typeid != DefineConstants.AUTO_COMMAND_TYPEID)
                        el.Value.count++;
                    else if (sIff.Instance.getItemGroupIdentify(el.Value._typeid) == IFF_GROUP.BALL /*/ *Ball * /*/ || sIff.Instance.getItemGroupIdentify(el.Value._typeid) == IFF_GROUP.AUX_PART)
                        el.Value.count++;
                }
            }
        }
        #endregion

        #region HELPER
        public override bool DeletePlayer(Player session, int option)
        {

            if (session == null)
            {
                throw new exception("[SCC::DeletePlayer][Error] tentou deletar um player, mas o seu endereco é null.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY,
                    50, 0));
            }

            bool ret = false;

            try
            {
                var it = Players.FirstOrDefault(c => c == session);

                if (it != null)
                {
                    if (GameInitState == 1)
                    {

                        var p = new Packet();

                        InitPlayerInfo("deletePlayer",
                            "tentou sair do jogo",
                            session, out PlayerGameInfo pgi);

                        var sessions = GetSessions(it);

                        RequestFinishItemUsedGame(it); // Salva itens usados no SCC

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
                        SendUpdateState(session, 3);

                        // Salva Achievement do player

                        if (AllCompleteGameAndClear())
                        {
                            ret = true; // Termina o SCC
                        }

                        // Delete Player
                        Players.Remove(it);
                    }
                }
                else
                {
                    _smp.LogManager.Instance.push(new AppMessage("[SCC::deletePlayer][Warning] player ja foi excluido do game.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                }

            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[SCC::deletePlayer][Error] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

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
                FinishSCC(session, 0);
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


        public void FinishSCC(Player session, int option)
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

                        // Resposta para acabou o tempo do SCC
                        SendTimeIsOver(session);
                    }
                }

                //pgi->type = (option == 0) ? PlayerGameInfo::eFLAG_GAME::FINISH : PlayerGameInfo::eFLAG_GAME::END_GAME;
                SetGameFlag(pgi, (option == 0) ? PlayerGameInfo.eFLAG_GAME.FINISH : PlayerGameInfo.eFLAG_GAME.END_GAME);

                pgi.time_finish = new SystemTime(DateTime.Now);

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

                // Cria o timer do SCC
                GameStartTime();

                // variavel que salva a data local do sistema
                InitGameTime();

                GameInitState = 1; // Come ou

                _SSCState = true;
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
                            _smp.LogManager.Instance.push(new AppMessage($"[SCC::FinishExpGame][Warning] Normal[UID={PlayerOrder[i].uid}] não encontrado na sessão — WebShopPoint não atualizado.", type_msg.CL_FILE_LOG_AND_CONSOLE));
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
                            _smp.LogManager.Instance.push(new AppMessage($"[SCC::FinishExpGame][Warning] Normal[UID={PlayerOrder[i].uid}] END_GAME não encontrado na sessão — WebShopPoint não atualizado.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                        }
                    }
                }
            }
        }

        public void Finish()
        {

            GameInitState = 2; // Acabou

            CalculeRankPlace();

            MakeMasterCoin();  

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

        public void MakeMasterCoin()
        {
            m_coin_SSC = (uint)(3 + ((PlayerInfo.Count == 0) ? 0 : Random.Shared.Next() % (((uint)PlayerInfo.Count * 4) - 3)));
        }

        public void SendMakeMasterCoin(Player _session)
        {

            if (RoomInfo.OwnerUID == _session.UserInfo.UID && m_coin_SSC > 0)
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

                var rt = 0;

                if ((rt = ItemManager.addItem(item, _session, 0, 0)) < 0)
                {
                    _smp.LogManager.Instance.push(new AppMessage("[SpecialShuffleCourse::requestSendMasterCoiin][Error] Normal[UID=" + Convert.ToString(_session.UserInfo.UID) + "] tentou adicionar SSC coin[TYPEID=" + Convert.ToString(SPECIAL_SHUFFLE_COURSE_COIN_TYPEID) + "] para o Master, mas deu erro no ItemManager::addItem. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                    return;
                }

                // Resposta para enviar SSC coin para o Master da sala
                var p = new Packet(0x198);

                p.WriteUInt32(SPECIAL_SHUFFLE_COURSE_COIN_TYPEID);
                p.WriteUInt32(m_coin_SSC);
                _session.Send(p);

                if (rt != RetAddItem.SUCCESS_PANG_AND_EXP_AND_CP_POUCH)
                {
                    // Update Item ON Game pacoteAA no JP n o att as moedas na hora
                    p.init_plain(0x216);

                    p.WriteUInt32((uint)UtilTime.GetSystemTimeAsUnix());
                    p.WriteUInt32(1); // Count 
                    p.WriteByte(item.type);
                    p.WriteUInt32(item._typeid);
                    p.WriteInt32(item.id);
                    p.WriteUInt32(item.flag);
                    p.WriteBytes(item.stat.ToArray());
                    p.WriteInt32((item.STDA_C_ITEM_TIME > 0) ? item.STDA_C_ITEM_TIME : item.STDA_C_ITEM_QNTD);
                    p.WriteZero(25);
                    _session.Send(p);
                }
            }
        }

        public override void RequestDrawTreasureHunterItem(Player _session)
        {

            if (!sTreasureHunterSystem.Instance.isLoad())
                sTreasureHunterSystem.Instance.load();

            var pgi = InitPlayerInfo("requestDrawTreasureHunterItem", "tentou sortear os item(ns) do Treasure Hunter do jogo", _session); 

            pgi.thi.v_item = sTreasureHunterSystem.Instance.drawItem(pgi.thi.treasure_point, (byte)(RoomInfo.GetMap() & 0x7F));

            if (pgi.thi.v_item.Count == 0)
                _smp.LogManager.Instance.push(deque: new AppMessage("[SpecialShuffleCourse::requestDrawTreasureHunterItem][Warning] Normal[UID=" + Convert.ToString(_session.UserInfo.UID) + "] tentou sortear os item(ns) do Treasure Hunter do jogo," + "mas o Treasure Hunter Item nao conseguiu sortear nenhum item", type_msg.CL_FILE_LOG_AND_CONSOLE));
        }

        public void FinishData(Player session)
        {

            // Finish Artefact Frozen Flame agora   direto no Finish Item Used Game
            RequestFinishItemUsedGame(session);

            RequestSaveDrop(session);

            RequestDrawTreasureHunterItem(session);

            RainHoleSeqCount(session); // conta os achievement de Rain em holes consecutivas

            ScoreSeqCount(session); // conta os achievement de back-to-back(2 ou mais score iguais consecutivos) do player

            RainCount(session); // Aqui achievement de rain count

            AchievementTop3_1st(session); // Se o Player ficou em Top 3 add +1 ao contador de top 3, e se ele ficou em primeiro add +1 ao do primeiro

            // Resposta terminou game - Drop Itens
            SendDropItem(session);

            // Resposta terminou game - Placar
            SendPlacar(session);

            // Resposta Treasure Hunter Item Draw
            SendTreasureHunterItemDrawGUI(session);
        }
         
        public override bool FinishGame(Player session, int option)
        {

            if (session.getState()
                && session.Connected
                && Players.Count() > 0)
            {
                if (option == 6)
                {

                    if (_SSCState)
                    {
                        FinishSCC(session, 1); // Termina sem ter acabado de jogar
                    }

                    InitPlayerInfo("finish_game", "tentou terminar o jogo", session, out PlayerGameInfo pgi);
                     
                    RequestSaveInfo(session, 4);

                    SendMakeMasterCoin(session);
                   
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
                    var p = new Packet();
                    // Resposta que tem sempre que acaba um jogo, n o sei o que   ainda, esse s  n o tem no HIO Event
                    p.init_plain(0x244);
                    p.WriteUInt32(0); // OK
                    session.Send(p);

                    // Esse   novo do JP, tem SCC, VS, Grand Prix, HIO Event, n o vi talvez tenha nos outros tamb m
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
            } 
            return PlayersCompleteGameAndClear() && _SSCState;
        }
        #endregion

        #region GAME TIMER
        public override void GameTimeIsOver()
        {

            if (GameInitState == 1 && Players.Count() > 0)
            {

                Player session = null;

                // ToList() garante snapshot: FinishSCC pode modificar PlayerInfo
                // internamente, causando InvalidOperationException no foreach original.
                foreach (var el in PlayerInfo.ToList())
                {

                    // Só os que não acabaram
                    if (el.Value.flag == PlayerGameInfo.eFLAG_GAME.PLAYING && (session = FindSessionByUID(el.Value.uid)) != null)
                    {
                        FinishSCC(session, 1);
                    }
                    else if (el.Value.flag == PlayerGameInfo.eFLAG_GAME.FINISH && (session = FindSessionByUID(el.Value.uid)) != null)
                    {
                        // Resposta para acabou o tempo do SCC
                        SendTimeIsOver(session);
                    }
                }
            }
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
                _SSCState = false;
            }
            base.Dispose(disposing);
        }

        ~SSC()
        {
            Dispose(false);
        }
        #endregion
    }
}
