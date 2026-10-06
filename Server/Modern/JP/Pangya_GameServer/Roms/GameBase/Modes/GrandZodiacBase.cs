using Pangya_GameServer.Flags;
using Pangya_GameServer.Models.Game;
using Pangya_GameServer.Session;
using PangyaAPI.IFF.Flags;
using PangyaAPI.IFF.Handle.JP;
using PangyaAPI.Network;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using System.Diagnostics;

using static Pangya_GameServer.Models.DefineConstants;
using static PangyaAPI.Utilities.Tools;

namespace Pangya_GameServer.Roms.GameBase.Modes
{
    public abstract class GrandZodiacBase : TourneyBase
    {
        public int StateGoldenBeam;
        public List<(Player session, bool HioTime)> PlayersGoldenBeam;
        public List<double> SeedValues;
        private IntPtr _checkSyncHoleEvent;
        private IntPtr _checkSyncHolePulseEvent; 
        private List<stReward> Rewards;
        private stStateGrandZodiacSync GrandZodiacState;
        private PangyaThread _checkSyncThread;
        private bool _initGrandZodiacState;

        public GrandZodiacBase(List<Player> players, GameRoomInfoModel roomInfo, RateValue rateValue) : base(players, roomInfo, rateValue)
        {
            PlayersGoldenBeam = new List<(Player session, bool HioTime)>();
            Rewards = new List<stReward>();
            GrandZodiacState = new stStateGrandZodiacSync();
            SeedValues = new List<double>();
            // Aqui tem que inicializar os players info
            InitAllPlayerInfo();

            InitValuesSeed();

            // Cria evento que vai para a thRead sync hole
            if ((_checkSyncHoleEvent = CreateEvent(IntPtr.Zero,  true, false, null)) == IntPtr.Zero)
            {
                throw new exception("[GrandZodiacBase::GrandZodiacBase][Error] ao criar evento sync hole.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.APPROACH,
                    1050, GetLastError()));
            }

            // Cria evento que vai pulsar a thRead sync hole para ir mais r pido quando um player tacar
            if ((_checkSyncHolePulseEvent = CreateEvent(IntPtr.Zero, false, false, null)) == IntPtr.Zero)
            {
                throw new exception("[GrandZodiacBase::GrandZodiacBase][Error] ao criar evento sync hole pulse.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.APPROACH,
                    1050, GetLastError()));
            }

            // Cria a thRead que vai sincronizar os player no hole
            _checkSyncThread = new PangyaThread(1060, obj => SyncFirstHole(), this, ThreadPriority.AboveNormal);
        }

        public override bool DeletePlayer(Player session, int option)
        {

            if (session == null)
            {
                throw new exception("[GrandZodiacBase::DeletePlayer][Error] tentou deletar um player, mas o seu endereco eh nullptr.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GRAND_ZODIAC_BASE,
                    50, 0));
            }

            bool ret = false;

            try
            {
                var it = Players.Find(c => c == session);

                if (it != null)
                {
                    // byte opt = 3; // Saiu Quitou

                    if (GameInitState == 1)
                    {

                        var p = new Packet();

                        var pgi = InitPlayerInfo("deletePlayer",
                            "tentou sair do jogo",
                            session);

                        var sessions = GetSessions(it);

                        RequestSaveInfo((it), 1 /*/ *Saiu * /*/);

                        RequestUpdateItemUsedGame((it)); // Atualiza primeiro, por que o Grand Zodiac n o atualiza, a cada hole, s  no final

                        RequestFinishItemUsedGame((it)); // Salva itens usados no Grand Zodiac

                        SetGameFlag(pgi, PlayerGameInfo.eFLAG_GAME.QUIT);

                        // Resposta Player saiu do jogo MSG
                        p.init_plain(0x40); 
                        p.WriteByte(2); // Player Saiu Msg 
                        p.WriteString(it.UserInfo.NickName); 
                        p.WriteUInt16(0); // size Msg, n o precisa de msg o pangya j  manda na opt 2
                        sessions.SendBroadCast(p);    

                        if (AllCompleteGameAndClear())
                        {
                            ret = true; // Termina o Grand Zodiac
                        }
                    }

                    // Delete Player
                    Players.Remove(it);

                }
                else
                {
                    _smp.LogManager.Instance.push(new AppMessage("[GrandZodiacBase::deletePlayer][Warning] player ja foi excluido do game.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[GrandZodiacBase::deletePlayer][Error] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

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

        public override void SendInitialData(Player session)
        { 
            try
            {

                // Envia aqui os valores do hole, size_cup
                var p = new Packet(0x1F9); 
                p.WriteInt32(session.UserInfo.getSizeCupGrandZodiac()); // Start Hole Size Cup
                p.WriteInt32(session.UserInfo.getSizeCupGrandZodiac()); // Finish size cup (OU O tee acho que seja)
                p.WriteUInt32((uint)session.UserInfo.GrandZodiacPoints); // Aqui acho que   os pangs que ele faz por cada hio, com rela  o ao size do cup (OU OS PONTOS DO GRAND ZODIAC)
                session.Send(p);
                //sync hole
                base.SendInitialData(session);

            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[GrandZodiacBase::SendInitialData][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }


        public override void RequestInitHole(Player session, Packet packet)
        { 
            var p = new Packet();

            try
            {
                #region Read Packet
                stInitHole ctxhole = new stInitHole().ToRead(packet);
                #endregion

                var hole = Course.findHole(ctxhole.numero);

                if (hole == null)
                {
                    throw new exception("[GrandZodiacBase::requestInitHole][Error] CourseIndex->findHole nao encontrou o hole retonou nullptr, o server esta com erro no init CourseIndex do GrandZodiacBase.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GRAND_ZODIAC_BASE,
                        2555, 0));
                }

                hole.init(ctxhole.tee, ctxhole.pin);

                var pgi = InitPlayerInfo("requestInitHole",
                    "tentou inicializar o hole[NUMERO = " + (hole.GetRoomId()) + "] no jogo",
                    session);

                // Update Location Player in Hole
                pgi.location.x = ctxhole.tee.x;
                pgi.location.z = ctxhole.tee.z;

                // N mero do hole atual, que o player est  jogandp
                pgi.hole = ctxhole.numero;

                // ServerFlag que marca se o player j  inicializou o primeiro hole do jogo
                if (!pgi.init_first_hole)
                {
                    pgi.init_first_hole = true;
                }

                // Gera degree para o player ou pega o degree sem gerar que   do HoleMode do hole repeat
                pgi.degree = (RoomInfo.GetHoleType() == RoomHoleType.M_REPEAT) ? hole.getWind().degree.getDegree() : hole.getWind().degree.getShuffleDegree();

                // Resposta de tempo do hole
                p.init_plain(0x9E);

                p.WriteUInt16(hole.getWeather());
                p.WriteByte(0); // Option do tempo, sempre peguei zero aqui dos pacotes que vi
                session.Send(p); 

                var windFlag = InitCardWindPlayer(pgi, hole.getWind().wind);

                // Resposta do vento do hole
                p.init_plain(0x5B);

                p.WriteByte(hole.getWind().wind + windFlag);
                p.WriteByte((windFlag < 0) ? 1 : 0); // ServerFlag de card de vento, aqui   a qnd diminui o vento, 1 Vento Blue
                p.WriteUInt16(pgi.degree);
                p.WriteByte(1); // ServerFlag do vento, 1 Reseta o Vento, 0 soma o vento que nem o comando gm \wind do pangya original
                session.Send(p);

            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[GrandZodiacBase::requestInitHole][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

            }
        }


        public override bool RequestFinishLoadHole(Player session, Packet packet)
        {
            var p = new Packet();

            bool ret = false;

            try
            {

                var size_cup = packet.ReadUInt32();

                var pgi = InitPlayerInfo("requestFinishLoadHole",
                    "tentou finalizar carregamento do hole no jogo",
                    session);

                pgi.finish_load_hole = 1;

                // Valores de double, aleat rio que passar, pode ser s  da rota  o da camera
                // Mas vou deixar o mesmo valor para todos, para fazer um teste
                foreach (var el in SeedValues)
                {
                    p.init_plain(0x1EC);
                    p.WriteDouble(el);
                    session.Send(p);
                }

                // Primeiro pacote dizendo que terminou de carregar o CourseIndex GZ
                p.init_plain(0x201);

                session.Send(p);
            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[GrandZodiacBase::requestFinishLoadHole][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            return ret;
        }

        public override void RequestFinishCharIntro(Player session, Packet packet)
        { 
            var p = new Packet();

            try
            {

                var pgi = InitPlayerInfo("requestFinishCharIntro",
                    "tentou finalizar intro do char no jogo",
                    session);

                pgi.finish_char_intro = 1;

                pgi.data.tacada_num = 0;

                // Giveup ServerFlag
                pgi.data.giveup = 0;

            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[GrandZodiacBase::requestFinishCharIntro][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void RequestInitShot(Player session, Packet packet)
        {
            try
            {
                ShotDataEx sd = new();

                // Power Shot
                #region Read Shot Sync Data
                sd.ToRead(packet); 
                #endregion
                var pgi = InitPlayerInfo("requestInitShot",
                    "tentou iniciar tacada no jogo",
                    session);

                pgi.shot_data = sd;

                // Aqui seta o StateRoom e verifica se   para mandar a resposta
                if (pgi.m_sync_shot_gz.setStateAndCheckAllAndClear(SyncShotGrandZodiac.eSYNC_SHOT_GRAND_ZODIAC_STATE.SSGZS_FIRST_SHOT_INIT))
                {
                    SendReplyInitShotAndSyncShot(session);
                }

            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[GrandZodiacBase::requestInitShot][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void RequestActiveBooster(Player session, Packet packet)
        {
            var p = new Packet();

            try
            {
                float velocidade = packet.ReadFloat();

                // Resposta para Active Booster
                p.init_plain(0xC7);

                p.WriteFloat(velocidade);
                p.WriteInt32(session.ConnectionID);
                session.Send(p);

            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[GrandZodiacBase::requestActiveBooster][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void RequestActiveCutin(Player session, Packet packet)
        {
            var p = new Packet();

            try
            {
                // Resposta para Active Cutin
                p.init_plain(0x18D);

                p.WriteByte(0); // OK

                p.WriteUInt16(3); // Cuttin do Grand Zodiac, Cuttin desativado
                session.Send(p);
            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[GrandZodiacBase::requestActiveCutin][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x18D);
                p.WriteByte(0); // OPT 
                p.WriteUInt16(1); // Error
                session.Send(p);
            }
        }

        public override void RequestStartFirstHoleGrandZodiac(Player session, Packet packet)
        { 
            try
            {

                var pgi = InitPlayerInfo("requestStartFirstHoleGrandZodiac",
                    "tentou inicializar o primeiro hole do grand zodiac",
                    session);

                // Aqui tem que sincronizar
                SetInitFirstHole(pgi);

            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[GrandZodiacBase::requestStartFirstHoleGrandZodiac][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void RequestReplyInitialValueGrandZodiac(Player session, Packet packet)
        {
            try
            {

                double value = packet.ReadDouble();

            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[GrandZodiacBase::requestReplyInitialValueGrandZodiac][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override bool RequestFinishGame(Player session, Packet packet)
        {
            bool ret = false;

            try
            {

#if DEBUG
                //_smp.message_pool.getInstance.push(new message("[GrandZodiacBase::requestFinishGame][Log] Packet Hex: " + packet.Log(), type_msg.CL_FILE_LOG_AND_CONSOLE));
#endif // DEBUG

                // Packet0CB
                ret = FinishGame(session, 0x12C);

            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[GrandZodiacBase::requestFinishGame][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            return ret;
        }

        public override void SendRemainTime(Player session)
        {

            try
            {
                // Resposta tempo percorrido do Tourney
                var p = new Packet(0x8D); 
                p.WriteUInt32(0u); // Grand Zodiac, passa o tempo decorrido no pacote200
                session.Send(p);
            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[GrandZodiacBase::SendRemainTime][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void RequestFinishHole(Player session, int option)
        {

            var pgi = InitPlayerInfo("requestFinishHole",
                "tentou finalizar o dados do hole do player no jogo",
                session);

            var hole = Course.findHole(pgi.hole);

            if (hole == null)
            {
                throw new exception("[GrandZodiacBase::finishHole][Error] Normal[UID=" + (session.UserInfo.UID) + "] tentou finalizar hole[NUMERO=" + ((ushort)pgi.hole) + "] no jogo, mas o RoomID do hole is invalid. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GRAND_ZODIAC_BASE,
                    20, 0));
            }

            int scorehole = 0;

            // Finish Hole Dados
            if (option == 0)
            {
                // Score Player
                var p = new Packet(0x1EF);

                p.WriteInt32(session.ConnectionID);

                p.WriteInt32(pgi.m_gz.total_score);

                p.WriteInt32(pgi.data.score);

                p.WriteInt32(pgi.m_gz.total_score + pgi.data.score);
                SendBroadCast(p);


                pgi.data.total_tacada_num += pgi.data.tacada_num;
                // Tacadas do hole
                var tacadahole = pgi.data.tacada_num;
                // Score do hole
                scorehole = Convert.ToInt32(pgi.data.tacada_num);

                pgi.m_gz.total_score += scorehole;

                pgi.m_gz.hole_in_one++;

                // Zera dados
                pgi.data.time_out = 0;

                pgi.data.tacada_num = 0;

                // Zera o score, que o Grand Zodiac usa o total_score
                pgi.data.score = 0;

                // Giveup ServerFlag
                pgi.data.giveup = 0;

                // Zera as penalidades do hole
                pgi.data.penalidade = 0;

            }
            else if (option == 1)
            { // N o acabou o hole ent o faz os calculos para o jogo todo

                // Zera dados
                pgi.data.time_out = 0;

                pgi.data.tacada_num = 0;

                // Zera o score, que o Grand Zodiac usa o total_score
                pgi.data.score = 0;

                // Giveup ServerFlag
                pgi.data.giveup = 0;

                // Zera as penalidades do hole do player
                pgi.data.penalidade = 0;
            }

            // Aqui tem que atualiza o PGI direitinho com outros dados
            pgi.progress.hole = (short)Course.findHoleSeq(pgi.hole);

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
                    pgi.progress.score[pgi.progress.hole - 1] = (sbyte)scorehole;
                    pgi.progress.tacada[pgi.progress.hole - 1] = pgi.data.tacada_num;
                }

            }
            else
            {

                var pair = Course.findRange(pgi.hole);
                foreach (var it in pair)
                {
                    if (it.Key <= RoomInfo.HoleCount)
                    {
                        continue;
                    }

                    pgi.progress.finish_hole[it.Key - 1] = 0; // n o terminou

                    pgi.progress.par_hole[it.Key - 1] = it.Value.getPar().par;

                    pgi.progress.score[it.Key - 1] = it.Value.getPar().range_score[1]; // Max Score

                    pgi.progress.tacada[it.Key - 1] = it.Value.getPar().total_shot;
                }
            }
        }

        public override void RequestUpdateItemUsedGame(Player session)
        {

            var pgi = InitPlayerInfo("requestUpdateItemUsedGame",
                "tentou atualizar itens usado no jogo",
                session);

            var ui = pgi.used_item;

            // Passive Item exceto Time Booster e Auto Command, que soma o contador por uso, o cliente passa o pacote, dizendo que usou o item
            foreach (var el in ui.v_passive)
            {

                // Verica se   o ultimo hole, terminou o jogo, ai tira soma 1 ao count do pirulito que consome por jogo
                if (CHECK_PASSIVE_ITEM(el.Value._typeid)
                    && el.Value._typeid != TIME_BOOSTER_TYPEID /*/ *Time Booster * /*/

                    && el.Value._typeid != AUTO_COMMAND_TYPEID)
                {

                    // Item de Exp Boost que s  consome 1 Por Jogo, s  soma no RequestFinishItemUsedGame
                    if (passive_item_exp_1perGame.Any(c => c == el.Value._typeid))
                    {
                        el.Value.count = (pgi.data.total_tacada_num / 4); // Gasta 1 a cada 4 tacadas
                    }

                }
                else if (sIff.Instance.getItemGroupIdentify(el.Value._typeid) == IFF_GROUP.BALL/* / *Ball * /*/ || sIff.Instance.getItemGroupIdentify(el.Value._typeid) == IFF_GROUP.AUX_PART)
                {
                    el.Value.count = (pgi.data.total_tacada_num / 4); // uma comet e um anel por 4 tacadas
                }
            }
        }

        public override void RequestTranslateSyncShotData(Player session, ShotSyncData ssd)
        { 
            try
            {

                var s = FindSessionByOID(ssd.oid);

                if (s == null)
                {
                    throw new exception("[GrandZodiacBase::requestTranslateSyncShotData][Error] Normal[UID=" + (session.UserInfo.UID) + "] tentou sincronizar tacada do Normal[OID=" + (ssd.oid) + "], mas o player nao existe nessa jogo. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GRAND_ZODIAC_BASE,
                        200, 0));
                }

                // Update Sync Shot Player
                if (session.UserInfo.UID == s.UserInfo.UID)
                {

                    var pgi = InitPlayerInfo("requestTranslateSyncShotData",
                        "tentou sincronizar a tacada no jogo",
                        session);

                    pgi.shot_sync = ssd;

                    // Last Location Player
                    var lastLocation = pgi.location;


                    // Update Pang and Bonus Pang
                    pgi.data.pang = ssd.pang;
                    pgi.data.bonus_pang = ssd.bonus_pang;

                    // J  s  na fun  o que come a o tempo do player do turno
                    pgi.data.tacada_num++;
                }

            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[GrandZodiacBase::requestTranslateSyncShotData][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void RequestSaveInfo(Player session, int option)
        {

            var pgi = InitPlayerInfo("requestSaveInfo",
                "tentou salvar o info dele no jogo",
                session);

            try
            {
                if (option == 1)
                { // Saiu

                    // Zera os pangs ele saiu
                    pgi.data.pang = 0Ul;
                    pgi.data.bonus_pang = 0Ul;
                }

                // Limpa o User Info por que n o add nada, s  o tempo e os pangs ganhos
                pgi.ui.clear();

                var diff = UtilTime.GetLocalTimeDiff(StartTime);

                if (diff > 0)
                {
                    diff /= STDA_10_MICRO_PER_SEC; // NanoSeconds To Seconds
                }

                pgi.ui.tempo = (int)diff;

                // Pode tirar pangs
                ulong total_pang = (pgi.data.pang + pgi.data.bonus_pang);

                // Adiciona o Jackpot, se ele ganhou
                if (option != 1 && pgi.m_gz.jackpot > 0)
                {
                    total_pang += pgi.m_gz.jackpot;
                }

                // UPDATE ON SERVER AND DB
                session.UserInfo.addUserInfo(pgi.ui, (ulong)total_pang); // add User Info

                if (total_pang > 0)
                {
                    session.UserInfo.addPang((ulong)total_pang); // add Pang
                }
                else if (total_pang < 0)
                {
                    session.UserInfo.consomePang((ulong)(Convert.ToInt64(total_pang) * -1)); // consome Pangs
                }
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[GrandZodiacBase::requestSaveInfo][Error] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void CalculeRankPlace()
        {

            if (PlayerOrder.Any())
            {
                PlayerOrder.Clear();
            }

            foreach (var el in PlayerInfo)
            {
                if (el.Value.flag != PlayerGameInfo.eFLAG_GAME.QUIT) // menos os que quitaram
                {
                    PlayerOrder.Add(el.Value);
                }
            }
            PlayerOrder.Sort(SortRankPlace);

            uint position = 1;
            int score = -1;

            PlayerGrandZodiacInfo pgzi = null;

            // Calcula posi  es, o Grand Zodiac tem player com a mesma posi  o se eles terminarem com o mesmo score
            foreach (var el in PlayerOrder)
            {


                if (el.flag != PlayerGameInfo.eFLAG_GAME.QUIT && (pgzi = (PlayerGrandZodiacInfo)(el)) != null)
                {

                    if (score == -1)
                    {

                        pgzi.m_gz.position = position;

                        score = pgzi.m_gz.total_score;

                    }
                    else if (score == pgzi.m_gz.total_score)
                    {
                        pgzi.m_gz.position = position;
                    }
                    else
                    {
                        pgzi.m_gz.position = ++position;
                    }
                }
            }
        }

        public static int SortRankPlace(PlayerGameInfo pgi1, PlayerGameInfo pgi2)
        {
            // Ambos null — considerados iguais
            if (pgi1 == null && pgi2 == null) return 0;

            // Corrigido: era 0 quando pgi1==null e pgi2!=null — null deve ir para o fim (-1 = pgi2 vem antes)
            if (pgi1 != null && pgi2 == null) return 1;
            if (pgi1 == null && pgi2 != null) return -1;

            var gz1 = (PlayerGrandZodiacInfo)pgi1;
            var gz2 = (PlayerGrandZodiacInfo)pgi2;

            // Maior score vem primeiro (ordem decrescente)
            if (gz1.m_gz.total_score > gz2.m_gz.total_score) return 1;
            if (gz1.m_gz.total_score < gz2.m_gz.total_score) return -1;
            return 0;
        }

        public override void RequestReplySyncShotData(Player session)
        { 
            try
            {

                var pgi = InitPlayerInfo("requestReplySyncShotData",
                    "tentou enviar a resposta do Sync Shot do jogo",
                    session);

                // Resposta Sync Shot
                SendSyncShot(session);

                // Deixai assim por que o Original manda a msg depois, pelo que estava no outro server
                DrawDropItem(session);

                // Aqui seta o StateRoom e verifica se   para mandar a resposta
                if (pgi.m_sync_shot_gz.setStateAndCheckAllAndClear(SyncShotGrandZodiac.eSYNC_SHOT_GRAND_ZODIAC_STATE.SSGZS_FIRST_SHOT_SYNC))
                {
                    SendReplyInitShotAndSyncShot(session);
                }

            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[GrandZodiacBase::requestReplySyncShotData][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void SendPlacar(Player session)
        {

            try
            {

                var pgi = InitPlayerInfo("SendPlacar",
                    "tentou enviar o placar do jogo",
                    session);

                var p = new Packet(0x1F3);

                p.WriteUInt32((uint)((pgi.flag == PlayerGameInfo.eFLAG_GAME.FINISH) ? 1 : 2)); // 1 Terminou, 2 Saiu

                p.WriteInt32(GetCountPlayersGame());

                PlayerGrandZodiacInfo pgzi = null;

                foreach (var el in PlayerInfo)
                { 
                    if (el.Value != null && el.Value.flag != PlayerGameInfo.eFLAG_GAME.QUIT && (pgzi = (PlayerGrandZodiacInfo)el.Value) != null)
                    {

                        p.WriteInt32(pgzi.oid);
                        p.WriteUInt32(pgzi.m_gz.position);
                        p.WriteInt32(pgzi.m_gz.total_score);
                        p.WriteUInt32(pgzi.m_gz.hole_in_one);
                        p.WriteInt32(pgzi.data.total_tacada_num);
                        p.WriteUInt32(pgzi.m_gz.pontos);
                        p.WriteInt32(pgzi.data.exp);
                        p.WriteUInt64(pgzi.data.pang);
                        p.WriteUInt64(pgzi.data.bonus_pang);
                        p.WriteUInt64(pgzi.m_gz.jackpot);
                        p.WriteUInt32(pgzi.m_gz.trofeu);

                        if (pgzi.drop_list.v_drop.Any())
                        {

                            p.WriteUInt32((uint)pgzi.drop_list.v_drop.Count()); // Count

                            foreach (var el2 in pgzi.drop_list.v_drop)
                            {

                                p.WriteUInt32(el2._typeid);
                                p.WriteUInt32((uint)el2.qntd);
                            }

                        }
                        else
                        {
                            p.WriteUInt32(0u); // N o ganhou drop item
                        }
                    }
                }
                session.Send(p);
            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[GrandZodiacBase::SendPlacar][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override int CheckEndShotOfHole(Player session)
        {

            try
            {

                // Agora verifica o se ele acabou o hole e essas coisas
                var pgi = InitPlayerInfo("checkEndShotOfHole",
                    "tentou verificar a ultima tacada do hole no jogo",
                    session);

                if (pgi.shot_sync.state_shot.display.acerto_hole || pgi.data.giveup > 0)
                {

                    // Finish Hole and change
                    FinishHole(session);

                    ChangeHole(session);

                }
                else // Update Shot
                {
                    UpdateFinishHole(session, 0 /*/ *N o fez hio * /*/);
                }

                // Limpa, terminou a tacada
                pgi.m_gz.m_score_shot.Clear();

            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[GrandZodiacBase::checkEndShotOfHole][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            return 0;
        }

        public object SyncFirstHole()
        {
            var datetime = Stopwatch.StartNew();
            TimeSpan ts = datetime.Elapsed;
            try
            { 
                _smp.LogManager.Instance.push(new AppMessage($"[GrandZodiacBase::syncFirstHole][Log] Partida comecou: {String.Format("{0:00}:{1:00}:{2:00}", ts.Hours, ts.Minutes, ts.Seconds)}", type_msg.CL_FILE_LOG_AND_CONSOLE));

                uint retWait = WAIT_TIMEOUT;

                IntPtr[] wait_events = { _checkSyncHoleEvent, _checkSyncHolePulseEvent };

                while ((retWait = WaitForMultipleObjects((uint)wait_events.Length, wait_events, false, 1000 /*1 segundo*/)) == WAIT_TIMEOUT || retWait == (WAIT_OBJECT_0 + 1))
                {
                    try
                    {

                        GrandZodiacState.@lock();

                        switch (GrandZodiacState.getState())
                        {
                            case eSTATE_GRAND_ZODIAC_SYNC.FIRST_HOLE:
                                {

                                    if (CheckAllInitFirstHole())
                                    {

                                        ClearInitFirstHole();

                                        // Come a o tempo do jogo
                                        GameStartTime();

                                        foreach (var el in Players)
                                        {

                                            if (el != null)
                                            {

                                                SendRemainTime(el);
                                                var p = new Packet();
                                                // Resposta passa o OID do player que vai come a o Hole
                                                p.init_plain(0x53);

                                                p.WriteInt32(el.ConnectionID);
                                                el.Send(p);

                                                // Passa a localiza  o do player, esse   a primeira, vez ent o passa os valores zerados
                                                UpdateFinishHole(el, 1/* / *Come ou o hole * /*/);

                                                // Come a o Grand Zodiac
                                                p.init_plain(0x1F4);

                                                p.WriteInt32(1); // Start
                                                el.Send(p);
                                            }
                                        }

                                        // Verifica o tempo do start golden beam
                                        GrandZodiacState.setState(eSTATE_GRAND_ZODIAC_SYNC.START_GOLDEN_BEAM);
                                    }

                                    break;
                                }
                            case eSTATE_GRAND_ZODIAC_SYNC.START_GOLDEN_BEAM:
                                {

                                    if (Timer != null)
                                    {

                                        var elapsed = Timer.getElapsed();

                                        if (elapsed >= (RoomInfo.TimeMin - 60000))
                                        {

                                            // Come a o tempo do golden beam time
                                            StartGoldenBeam();

                                            // Verifica o tempo do end golden beam
                                            GrandZodiacState.setState(eSTATE_GRAND_ZODIAC_SYNC.END_GOLDEN_BEAM);
                                        }
                                    }

                                    break;
                                }
                            case eSTATE_GRAND_ZODIAC_SYNC.END_GOLDEN_BEAM:
                                {
                                    if (Timer != null)
                                    {

                                        var elapsed = Timer.getElapsed();

                                        if (elapsed >= (RoomInfo.TimeMin - 30000))
                                        {

                                            // Terminar o golden beam
                                            EndGoldenBeam();

                                            GrandZodiacState.setState(eSTATE_GRAND_ZODIAC_SYNC.WAIT_END_GAME);
                                        }
                                    }

                                    break;
                                }
                            case eSTATE_GRAND_ZODIAC_SYNC.LOAD_HOLE:
                                {
                                    break; // Faz nada por enquanto
                                }
                            case eSTATE_GRAND_ZODIAC_SYNC.LOAD_CHAR_INTRO:
                                {
                                    break; // Faz nada por enquanto
                                }
                            case eSTATE_GRAND_ZODIAC_SYNC.END_SHOT:
                                {
                                    break; // Faz nada por enquanto
                                }
                            case eSTATE_GRAND_ZODIAC_SYNC.WAIT_END_GAME:
                                {

                                    if (CheckAllEndGame())
                                    {
                                        ClearEndGame();
                                    }

                                    break;
                                }
                        }

                        // Libera
                        GrandZodiacState.unlock();

                    }
                    catch (exception e)
                    {
                        GrandZodiacState.unlock();
                        _smp.LogManager.Instance.push(new AppMessage("[GrandZodiacBase::syncFirstHole][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                    }
                    catch (Exception e)
                    {
                        // Captura exceções nativas do .NET (NullReferenceException, etc.)
                        // que de outra forma matariam esta thread silenciosamente.
                        GrandZodiacState.unlock();
                        _smp.LogManager.Instance.push(new AppMessage($"[GrandZodiacBase::syncFirstHole][UnhandledException] {e.GetType().Name}: {e.Message}", type_msg.CL_FILE_LOG_AND_CONSOLE));
                    }
                } 
               
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[GrandZodiacBase::syncFirstHole][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
            //para o tempo
            datetime.Stop();
            ts = datetime.Elapsed;
     
            _smp.LogManager.Instance.push(new AppMessage($"[GrandZodiacBase::syncFirstHole][Log] Partida Finalizada. Tempo total: {String.Format("{0:00}:{1:00}:{2:00}", ts.Hours, ts.Minutes, ts.Seconds)}", type_msg.CL_FILE_LOG_AND_CONSOLE));
            return null;
        }


        #region ABSTRACT
        public abstract void StartGoldenBeam();
        public abstract void EndGoldenBeam(); 
        #endregion

        #region HELPERS
        public void InitValuesSeed()
        {
            SeedValues.Clear();
            double varD = ((Random.Shared.Next() % 1000) / 100.0f) + 1.0f;
            for (var i = 0; i < 10u; ++i)
            {
                SeedValues.Add((varD + i * ((Random.Shared.Next() % 10) / 10.0f) + 1.0f));
            }
        }

        public void NextHole(Player session)
        {

            try
            {

                var pgi = InitPlayerInfo("nextHole",
                    "tentou trocar o hole no jogo",
                    session);

                var p = new Packet(0x1F4);
                p.WriteUInt32(1u); // Inicializa o pr ximo hole
                session.Send(p);
                // Wind
                var hole = Course.findHole(pgi.hole);

                if (hole == null)
                {
                    throw new exception("[GrandZodiacBase::requestInitHole][Error] CourseIndex->findHole nao encontrou o hole retonou nullptr, o server esta com erro no init CourseIndex do Chip-in Practice.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHIP_IN_PRACTICE,
                        2555, 0));
                }

                var wind = Course.shuffleWind(Random.Shared.Next());

                hole.SetWind(wind);

                // Gera degree para o player ou pega o degree sem gerar que   do HoleMode do hole repeat
                pgi.degree = hole.getWind().degree.getShuffleDegree();

                var windFlag = InitCardWindPlayer(pgi, hole.getWind().wind);

                // Resposta do vento do hole
                p.init_plain(0x5B);

                p.WriteByte(hole.getWind().wind + windFlag);
                p.WriteByte((windFlag < 0) ? 1 : 0); // ServerFlag de card de vento, aqui   a qnd diminui o vento, 1 Vento Blue
                p.WriteUInt16(pgi.degree);
                p.WriteByte(1/* / *Reseta * /*/); // ServerFlag do vento, 1 Reseta o Vento, 0 soma o vento que nem o comando gm \wind do pangya original
                session.Send(p);


                // Remain Time em segundos
                var remainTime = 0L;

                if (Timer != null)
                {
                    remainTime = Timer.getElapsed();
                }

                if (remainTime > 0)
                {
                    remainTime /= 1000/* / *Milli por segundos * /*/;
                }

                p.init_plain(0x200);
                p.WriteUInt32((uint)remainTime);
                session.Send(p);


                // Update, finaliza o hole
                UpdateFinishHole(session, 1/* / *Fez HIO * /*/);

            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[GrandZodiacBase::nextHole][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void SetInitFirstHole(PlayerGrandZodiacInfo pgi)
        {

            if (pgi == null)
            {

                _smp.LogManager.Instance.push(new AppMessage("[GrandZodiacBase::setInitFirstHole][Error] PlayerGrandZodiacInfo* _pgi is invalid(nullptr).", type_msg.CL_FILE_LOG_AND_CONSOLE));

                return;
            }
             
            // Set
            pgi.init_first_hole_gz = 1;
            if (_checkSyncHolePulseEvent != INVALID_HANDLE_VALUE)
                SetEvent(_checkSyncHolePulseEvent);
        }
      
        public bool CheckAllInitFirstHole()
        {

            uint count = 0;

            // Check
            Players.ForEach(el =>
            {
                try
                {
                    var pgi = InitPlayerInfo("checkAllInitFirstHole",
                        "tentou verificar se todos os player terminaram de inicializar o primeiro hole do Grand Zodiac no jogo",
                        el);
                    if (pgi.init_first_hole_gz == 1)
                    {
                        count++;
                    }
                }
                catch (exception e)
                {
                    _smp.LogManager.Instance.push(new AppMessage("[GrandZodiacBase::checkAllInitFirstHole][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
            });



            return (count == Players.Count);
        }

        public void ClearInitFirstHole()
        {
            ClearAllInitFirstHole();
        }
        
        public bool SetInitFirstHoleAndCheckAllInitFirstHoleAndClear(PlayerGrandZodiacInfo pgi)
        {

            if (pgi == null)
            {

                _smp.LogManager.Instance.push(new AppMessage("[GrandZodiacBase::setInitFirstHoleAndCheckAllInitFirstHoleAndClear][Error] PlayerGrandZodiacInfo* _pgi is invalid(nullptr).", type_msg.CL_FILE_LOG_AND_CONSOLE));

                return false;
            }

            uint count = 0;
            bool ret = false;
            // Set
            pgi.init_first_hole_gz = 1;

            // Check
            Players.ForEach(el =>
            {
                try
                {
                    var pgi = InitPlayerInfo("setInitFirstHoleAndCheckAllInitFirstHoleAndClear",
                        "tentou verificar se todos os player terminaram de inicializar o primeiro hole do Grand Zodiac no jogo",
                        el);
                    if (pgi.init_first_hole_gz == 1)
                    {
                        count++;
                    }
                }
                catch (exception e)
                {
                    _smp.LogManager.Instance.push(new AppMessage("[GrandZodiacBase::setInitFirstHoleAndCheckAllInitFirstHoleAndClear][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
            });



            ret = (count == Players.Count);

            // Clear
            if (ret)
            {
                ClearAllInitFirstHole();
            }

            return ret;
        }

        public void SetEndGame(PlayerGrandZodiacInfo pgi)
        {

            if (pgi == null)
            {

                _smp.LogManager.Instance.push(new AppMessage("[GrandZodiacBase::setEndGame][Error] PlayerGrandZodiacInfo* _pgi is invalid(nullptr).", type_msg.CL_FILE_LOG_AND_CONSOLE));

                return;
            }
            // Set
            pgi.end_game = 1;

            if (_checkSyncHolePulseEvent != INVALID_HANDLE_VALUE)
                SetEvent(_checkSyncHolePulseEvent);
        }

        public bool CheckAllEndGame()
        {

            uint count = 0;

            // Check
            Players.ForEach(el =>
            {
                try
                {
                    var pgi = InitPlayerInfo("checkAllEndGame",
                        "tentou verificar se todos os player terminaram o jogo no Grand Zodiac",
                        el);
                    if (pgi.end_game > 0)
                    {
                        count++;
                    }
                }
                catch (exception e)
                {
                    _smp.LogManager.Instance.push(new AppMessage("[GrandZodiacBase::checkAllEndGame][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
            });



            return (count == Players.Count);
        }

        public void ClearEndGame()
        {
            ClearAllEndGame();
        }

        public bool SetEndGameAndCheckAllEndGameAndClear(PlayerGrandZodiacInfo pgi)
        {

            if (pgi == null)
            {

                _smp.LogManager.Instance.push(new AppMessage("[GrandZodiacBase::setEndGameAndCheckAllEndGameAndClear][Error] PlayerGrandZodiacInfo* _pgi is invalid(nullptr).", type_msg.CL_FILE_LOG_AND_CONSOLE));

                return false;
            }

            uint count = 0;
            bool ret = false;
            // Set
            pgi.end_game = 1;

            // Check
            Players.ForEach(el =>
            {
                try
                {
                    var pgi = InitPlayerInfo("setEndGameAndCheckAllEndGameAndClear",
                        "tentou verificar se todos os player terminaram o jogo no Grand Zodiac",
                        el);
                    if (pgi.end_game > 0)
                    {
                        count++;
                    }
                }
                catch (exception e)
                {
                    _smp.LogManager.Instance.push(new AppMessage("[GrandZodiacBase::setInitFirstHoleAndCheckAllInitFirstHoleAndClear][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
            });



            ret = (count == Players.Count);

            // Clear
            if (ret)
            {
                ClearAllEndGame();
            }

            return ret;
        }

        public void ClearAllInitFirstHole()
        {

            Players.ForEach(el =>
            {
                try
                {
                    var pgi = InitPlayerInfo("clear_all_init_first_hole",
                        " tentou limpar all init first hole do Grand Zodiac no jogo",
                        el);
                    pgi.init_first_hole_gz = 0;
                }
                catch (exception e)
                {
                    _smp.LogManager.Instance.push(new AppMessage("[GrandZodiacBase::ClearAllInitFirstHole][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
            });


        }

        public void ClearAllEndGame()
        {

            Players.ForEach(el =>
            {
                try
                {
                    var pgi = InitPlayerInfo("clear_all_end_game",
                        " tentou limpar all end game do Grand Zodiac",
                        el);
                    pgi.end_game = 0;
                }
                catch (exception e)
                {
                    _smp.LogManager.Instance.push(new AppMessage("[GrandZodiacBase::clear_all_end_game][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
            });
        }

        public void SendReplyInitShotAndSyncShot(Player session)
        {

            try
            {

                var pgi = InitPlayerInfo("SendReplyInitShotAndSyncShot",
                    "tentou enviar a resposta da tacada do player no Grand Zodiac",
                    session);

                if (pgi.shot_sync.state_shot.display.acerto_hole || pgi.data.giveup > 0)
                {

                    // Set Player no golden beam map se j  estiver no tempo do golden beam e se ele n o estiver no map
                    if (Interlocked.Exchange(ref StateGoldenBeam, 1) == 1)
                    {

                        int check_m = 1; // Compare
                        if (Interlocked.CompareExchange(ref StateGoldenBeam, check_m, 1) == 1)
                        { 
                            SetPlayerGoldenBeam(session);
                        }
                    }

                    if (pgi.shot_sync.state_shot.shot.cobra > 0
                        || pgi.shot_sync.state_shot.shot.tomahawk > 0
                        || pgi.shot_sync.state_shot.shot.spike > 0)
                    {

                        pgi.m_gz.m_score_shot.Add(eGRAND_ZODIAC_TYPE_SHOT.GZTS_SPECIAL_SHOT);

                        pgi.data.score++;
                    }

                    if (pgi.data.tacada_num == 1)
                    {

                        pgi.m_gz.m_score_shot.Add(eGRAND_ZODIAC_TYPE_SHOT.GZTS_FIRST_SHOT);

                        pgi.data.score++;
                    }

                    // Sem setas, se ele n o mandou nenhum Special shot ou spin ou curva ele n o apertou setas, s  as apagadas essas n o conta
                    if (pgi.shot_data.special_shot.ulSpecialShot == 0u)
                    {

                        pgi.m_gz.m_score_shot.Add(eGRAND_ZODIAC_TYPE_SHOT.GZTS_WITHOUT_COMMANDS);

                        pgi.data.score++;
                    }

                    if (pgi.shot_data.acerto_pangya_flag == 3)
                    {

                        pgi.m_gz.m_score_shot.Add(eGRAND_ZODIAC_TYPE_SHOT.GZTS_MISS_PANGYA);

                        pgi.data.score += 3;
                    }

                    // O score que o player fez hio
                    pgi.m_gz.m_score_shot.Add(eGRAND_ZODIAC_TYPE_SHOT.GZTS_HIO_SCORE);

                    pgi.data.score++; // Add +1 que   da tacada que ele deu, que ele fez hio

                    // S  envia se tiver mais que 1
                    if (pgi.m_gz.m_score_shot.Count > 1)
                    {

                        // Send Scores para o player
                        var p = new Packet(0x1F5);

                        p.WriteUInt32((uint)pgi.m_gz.m_score_shot.Count);

                        foreach (var el in pgi.m_gz.m_score_shot)
                        {
                            p.WriteUInt32((uint)el);
                        }

                        session.Send(p);
                    }
                }
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[GrandZodiacBase::SendReplyInitShotAndSyncShot][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override PlayerGameInfo MakePlayerInfoObject(Player session)
        {

            var pgzi = new PlayerGrandZodiacInfo();

            try
            {
                // Aqui se eu precisar inicializar algum valor

            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[GrandZodiacBase::makePlayerInfoObject][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            return pgzi;
        }

        public void SetPlayerGoldenBeam(Player session)
        {
            try
            {
                // Corrigido: era != null — adicionava quando já existia.
                if (PlayersGoldenBeam.FirstOrDefault(c => c.session == session).session == null)
                {
                    PlayersGoldenBeam.Add((session, true));
                }
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[GrandZodiacBase::SetPlayerGoldenBeam][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override PlayerGrandZodiacInfo InitPlayerInfo(string method, string message, Player session)
        {
            var pgi = GetPlayerInfo(session);
            if (pgi == null)
                throw new exception($"[{GetType().Name}::{method}][Error] Normal[UID={session.UserInfo.UID}] {message}, mas o game nao tem o info dele guardado. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME, 1, 4));

            return (PlayerGrandZodiacInfo)pgi;
        }



        private void FinishThreadSyncFirstHole()
        {
            try
            {
                if (_checkSyncThread != null)
                {
                    if (_checkSyncHoleEvent != INVALID_HANDLE_VALUE)
                        SetEvent(_checkSyncHoleEvent);

                    // Aguarda com timeout para evitar bloqueio infinito.
                    // Se expirar, força o encerramento da thread.
                    const int ShutdownTimeoutMs = 5000;
                    if (!_checkSyncThread.waitThreadFinish(ShutdownTimeoutMs))
                    {
                        _smp.LogManager.Instance.push(new AppMessage(
                            $"[GrandZodiacBase::FinishThreadSyncFirstHole][Warning] Thread não encerrou em {ShutdownTimeoutMs}ms — forçando exit.",
                            type_msg.CL_FILE_LOG_AND_CONSOLE));
                        _checkSyncThread.exit_thread();
                    }
                }
            }
            catch (exception ex)
            {
                Console.WriteLine($"[GrandZodiacBase::FinishThreadSyncFirstHole][ErrorSystem] {ex.getFullMessageError()}");
            }


            _checkSyncThread = null;

            if (_checkSyncHoleEvent != INVALID_HANDLE_VALUE)
                CloseHandle(_checkSyncHoleEvent);

            if (_checkSyncHolePulseEvent != INVALID_HANDLE_VALUE)
                CloseHandle(_checkSyncHolePulseEvent);

            _checkSyncHoleEvent = IntPtr.Zero;
            _checkSyncHolePulseEvent = IntPtr.Zero;
        }

        #endregion

        public override void Dispose(bool disposing)
        {

            if (Disposed) return;

            if (disposing)
            {
                Interlocked.Exchange(ref StateGoldenBeam, 0);

                if (PlayersGoldenBeam.Any())
                    PlayersGoldenBeam.Clear();

                if (SeedValues.Any())
                    SeedValues.Clear();

                // Termina a thread sync first hole
                FinishThreadSyncFirstHole();
                DeleteAllPlayer();
            }
            base.Dispose(disposing);
        }

        ~GrandZodiacBase()
        {
            Dispose(false);
        }

    }
}
