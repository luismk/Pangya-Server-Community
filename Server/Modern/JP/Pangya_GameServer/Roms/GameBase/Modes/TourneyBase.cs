using Pangya_GameServer.Feature;
using Pangya_GameServer.Flags;
using Pangya_GameServer.Manager;
using Pangya_GameServer.Models.Game;
using Pangya_GameServer.Server;
using Pangya_GameServer.Session;
using PangyaAPI.IFF.Flags;
using PangyaAPI.IFF.Handle.JP;
using PangyaAPI.IFF.Regions.JP.Models.IFF;
using PangyaAPI.Network;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;

using static Pangya_GameServer.Models.DefineConstants;

namespace Pangya_GameServer.Roms.GameBase.Modes
{
    public abstract class TourneyBase : Game
    {
        public uint MaxPlayer = 255u;
        public int EntraDepoisFlag;
        private bool _initTourneyBaseState;
        public TicketReportInfo TicketReport = new TicketReportInfo();

        public Medal[] Medals = new Medal[12];
        public TourneyBase(List<Player> players, GameRoomInfoModel roomInfo, RateValue rateValue) : base(players, roomInfo, rateValue)
        {
            this.TicketReport = new TicketReportInfo();
            MaxPlayer = 255u;
            EntraDepoisFlag = -1;
            Medals = new Medal[12];

            for (var i = 0; i < 12; ++i)
                Medals[i] = new Medal();
        }


        public abstract void ChangeHole(Player session);
        public abstract void FinishHole(Player session);

        public virtual void GameTimeIsOver()
        {
        }

        public override void SendInitialData(Player session)
        {
            try
            {
                // Players.Count representa o tamanho da lista de jogadores
                if (Interlocked.Increment(ref SyncSendInitData) == Players.Count)
                {
                    // Zera a variável atômica
                    Interlocked.Exchange(ref SyncSendInitData, 0);

                    var p = new Packet();

                    // Game Data Init
                    p.init_plain(0x76); 
                    p.WriteByte(RoomInfo.RoomType);
                    p.WriteUInt32(1);

                    p.WriteTime(StartTime);//escreve um tempo
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
                _smp.LogManager.Instance.push(new AppMessage("[TourneyBase::sendInitialData][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void SendInitialDataAfter(Player session)
        {

            var p = new Packet();

            try
            {

                // Send Initial Data of Game
                p.init_plain(0x113);

                p.WriteByte(4);
                p.WriteUInt32(3);

                p.WriteTime(StartTime);
                session.Send(p);
                // Course
                p.init_plain(0x113);

                p.WriteByte(4);
                p.WriteByte(4);

                p.WriteByte((byte)RoomInfo.CourseIndex);
                p.WriteByte(RoomInfo.RoomType);
                p.WriteByte(RoomInfo.HoleMode);
                p.WriteByte(RoomInfo.HoleCount);
                p.WriteUInt32(RoomInfo.TrophyID);
                p.WriteUInt32(RoomInfo.TimeSec);
                p.WriteUInt32(RoomInfo.TimeMin);

                // Hole Info, Hole Spinning Cube, end Seed Random Course
                Course.makePacketHoleInfo(p, 1);
                session.Send(p); 
            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[TourneyBase::sendInitialDataBefore][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void RequestInitHole(Player session, Packet packet)
        { 
            var p = new Packet();

            try
            {

                #region Read Packet
                var ctx_hole = new stInitHole().ToRead(packet);
                #endregion

                var hole = Course.findHole(ctx_hole.numero);

                if (hole == null)
                {
                    throw new exception("[TourneyBase::RequestInitHole][Error] CourseIndex->findHole nao encontrou o hole retonou null, o server esta com erro no init CourseIndex do tourney_base.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY_BASE,
                        2555, 0));
                }

                hole.init(ctx_hole.tee, ctx_hole.pin);

                InitPlayerInfo("RequestInitHole",
                    "tentou inicializar o hole[NUMERO = " + Convert.ToString(hole.GetRoomId()) + "] no jogo",
                    session, out PlayerGameInfo pgi);

                // Update Location Player in Hole
                pgi.location.x = ctx_hole.tee.x;
                pgi.location.z = ctx_hole.tee.z;

                // Número do hole atual, que o player está jogandp
                pgi.hole = ctx_hole.numero;

                // ServerFlag que marca se o player já inicializou o primeiro hole do jogo
                if (!pgi.init_first_hole)
                {
                    pgi.init_first_hole = true;
                }

                // Gera degree para o player ou pega o degree sem gerar que é do HoleMode do hole repeat
                pgi.degree = (ushort)((RoomInfo.GetHoleType() == RoomHoleType.M_REPEAT) ? hole.getWind().degree.getDegree() : hole.getWind().degree.getShuffleDegree());

                // Resposta de tempo do hole
                p.init_plain(0x9E);

                p.WriteUInt16(hole.getWeather());
                p.WriteByte(0); // Option do tempo, sempre peguei zero aqui dos pacotes que vi
                session.Send(p);

                var wind_flag = InitCardWindPlayer(pgi, hole.getWind().wind);

                // Resposta do vento do hole
                p.init_plain(0x5B);

                p.WriteByte(hole.getWind().wind + wind_flag);
                p.WriteByte((wind_flag < 0) ? 1 : 0); // ServerFlag de card de vento, aqui é a qnd diminui o vento, 1 Vento Blue
                p.WriteUInt16(pgi.degree);
                p.WriteByte(1); // ServerFlag do vento, 1 Reseta o Vento, 0 soma o vento que nem o comando gm \wind do pangya original
                session.Send(p);

                // Resposta tempo percorrido do Tourney 
               SendRemainTime(session);//nao envia no Approach, buga o HoleMode todo...
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[TourneyBase::RequestInitHole][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override bool RequestFinishLoadHole(Player session, Packet packet)
        { 
            var p = new Packet();

            // Esse aqui é para Trocar Info da Sala
            // para colocar a sala no HoleMode que pode entrar depois de ter começado
            bool ret = false;

            try
            {

                InitPlayerInfo("RequestFinishLoadHole",
                    "tentou finalizar carregamento do hole no jogo",
                    session, out PlayerGameInfo pgi);

                pgi.finish_load_hole = 1;

                if (pgi.enter_after_started == 1)
                {
                    // Add Player Score
                    p.init_plain(0x113);

                    p.WriteByte(9);
                    p.WriteByte(0);

                    p.WriteInt32(session.ConnectionID);
                    p.WriteUInt32((uint)Players.Count());
                    SendBroadCast(p);
                }
                // Resposta passa o OID do player que vai começa o Hole
                p.init_plain(0x53);

                p.WriteInt32(session.ConnectionID);
                session.Send(p);
            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[TourneyBase::RequestFinishLoadHole][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            return ret;
        }

        public override void RequestFinishCharIntro(Player session, Packet packet)
        { 
            var p = new Packet();

            try
            {

                InitPlayerInfo("RequestFinishCharIntro",
                    "tentou finalizar intro do char no jogo",
                    session, out PlayerGameInfo pgi);

                pgi.finish_char_intro = 1;

                // Zera todas as tacada num dos players se for camp Normal se for short game coloca o n mero de tacadas inicial
                if (RoomInfo.SpecialModeRoom.IsShotMode)
                { // Short Game

                    var hole = Course.findHole(pgi.hole);

                    if (hole == null)
                    {
                        throw new exception("[TourneyBase::RequestFinishCharIntro][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou finalizar intro do char, mas nao conseguiu encontrar o hole[NUMERO=" + Convert.ToString(pgi.hole) + "] no CourseIndex do jogo. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY_BASE,
                            30, 0));
                    }

                    switch (hole.getPar().par)
                    {
                        case 5:
                            pgi.data.tacada_num = 2;
                            break;
                        case 4:
                            pgi.data.tacada_num = 1;
                            break;
                        case 3:
                        default:
                            pgi.data.tacada_num = 0;
                            break;
                    }
                }
                else
                {
                    pgi.data.tacada_num = 0;
                }

                // Giveup ServerFlag
                pgi.data.giveup = 0;
            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[TourneyBase::RequestFinishCharIntro][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void RequestFinishHoleData(Player session, Packet p)
        { 
            try
            {
                #region Read Packet
                var ui = new PlayerUserStatistics().ToRead(p);
                #endregion
                // aqui o cliente passa mad_conduta com o hole_in, trocados, mad_conduto <-> hole_in

                InitPlayerInfo("RequestFinishHoleData",
                    "tentou finalizar hole dados no jogo",
                    session, out PlayerGameInfo pgi);

                pgi.ui = ui;

                if (!(pgi.shot_sync.state_shot.display.acerto_hole))
                { // Terminou o Hole sem acerta ele, Give Up

                    // Ainda não colocara o give up, o outro pacote, coloca nesse(muito difícil, n o colocar só se estiver com bug)
                    if (!(pgi.data.giveup == 1))
                    {
                        pgi.data.giveup = 1;

                        // Incrementa o Bad Condute
                        pgi.data.bad_condute++;
                    }
                }

                // Aqui Salva os dados do Pgi, os best Chipin, Long putt e best drive(max distância)
                // Não sei se precisa de salvar aqui, já que estou salvando no pgi User Info
                pgi.progress.best_chipin = ui.best_chip_in;
                pgi.progress.best_long_puttin = ui.best_long_putt;
                pgi.progress.best_drive = ui.best_drive;

            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[TourneyBase::RequestFinishHoleData][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void RequestInitShot(Player session, Packet packet)
        {
            try
            {
                // Power Shot
                #region Read Shot Sync Data 
                var sd = new ShotDataEx();
                sd.ToRead(packet);
                #endregion

                InitPlayerInfo("RequestInitShot",
                    "tentou iniciar tacada no jogo",
                    session, out PlayerGameInfo pgi);

                pgi.shot_data = sd;

                pgi.alwaysDetect.Analyze(session, sd, pgi.effect_flag_shot);
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[TourneyBase::RequestInitShot][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void RequestSyncShot(Player session, Packet packet)
        {
            ShotSyncData ssd = new ShotSyncData();
            try
            {

                // game read Request sync shot
               RequestReadSyncShotData(session, packet, ref ssd);//usar ref, asssim, evito perder dados da memoria que foi lido.

                // Request Calcule Shot Spinning Cube
               RequestCalculeShotSpinningCube(session, ssd); // esse não precisa verificar o usuário, por que em Tourney só o próprio player que envia

                // Request Calcule Shot Coin
                RequestCalculeShotCoin(session, ssd); // esse não precisa verificar o usuário, por que em Tourney só o próprio player que envia

                RequestTranslateSyncShotData(session, ssd);

                RequestReplySyncShotData(session);

            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[TourneyBase::RequestSyncShot][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void RequestInitShotArrowSeq(Player session, Packet packet)
        { 
            var p = new Packet();

            try
            {

                byte countSeta = packet.ReadByte();

                if (countSeta == 0)
                {
                    throw new exception("[TourneyBase::RequestInitShotArrowSeq][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou inicializar as sequencia de setas, mas nao enviou nenhuma seta. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY_BASE,
                        5, 0));
                }

                List<uArrow> setas = new List<uArrow>();

                for (var i = 0; i < countSeta; ++i)
                {
                    setas.Add(new uArrow(packet.ReadUInt32()));
                }

            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[TourneyBase::RequestInitShotArrowSeq][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void RequestShotEndData(Player session, Packet packet)
        { 
            var p = new Packet();

            try
            {

                // ----------------- LEMBRETE --------------
                // Aqui vou usar para as tacadas do spinning cube que gera no CourseIndex
                // --- Já estou usando o pacote no sync, por que preciso verificar uns valores lá ---
                ShotEndLocationData seld = new ShotEndLocationData(packet);

                InitPlayerInfo("RequestShotEndData",
                    "tentou finalizar local da tacada no jogo",
                    session, out PlayerGameInfo pgi);

                pgi.shot_data_for_cube = seld;

                // Resposta para Shot End Data
                p.init_plain(0x1F7);

                p.WriteInt32(pgi.oid);
                p.WriteByte(pgi.hole);

                p.WriteBytes(seld.ToArray()); 
                SendBroadCast(p);
            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[TourneyBase::RequestShotEndData][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override RetFinishShot RequestFinishShot(Player session, Packet packet)
        { 
            RetFinishShot ret = new RetFinishShot();

            try
            {

                // Request Init Cube Coin
                var cube = RequestInitCubeCoin(session, packet);

                // Resposta para Finish Shot
                SendEndShot(session, cube);

                ret.ret = CheckEndShotOfHole(session);

                if (ret.ret == 2)
                    ret.p = session;

                InitPlayerInfo("RequestFinishShot",
                    "tentou finalizar a tacada",
                   session, out PlayerGameInfo pgi);

                // Limpa dados que usa para cada tacada
                ClearDataEndShot(pgi);


            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[TourneyBase::RequestFinishShot][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            return (ret);
        }

        public override void RequestChangeMira(Player session, Packet packet)
        { 
            var p = new Packet();

            try
            {

                float mira = packet.ReadFloat();

                InitPlayerInfo("RequestChangeMira",
                    "tentou mudar a mira[MIRA=" + Convert.ToString(mira) + "] no jogo",
                   session, out PlayerGameInfo pgi);

                pgi.location.r = mira;

                // Resposta para o Change mira
                p.init_plain(0x56);

                p.WriteInt32(pgi.oid);
                p.WriteFloat(pgi.location.r);

                session.Send(p);
            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[TourneyBase::RequestChangeMira][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void RequestChangeStateBarSpace(Player session, Packet packet)
        {
            try
            {
                byte state = packet.ReadByte();
                float clientPoint = packet.ReadFloat();

                InitPlayerInfo(
                    "RequestChangeStateBarSpace",
                    $"STATE={(ushort)state}, POINT={clientPoint}",
                    session,
                    out PlayerGameInfo pgi
                );

                // valida estado
                if (!pgi.bar_space.setState(state))
                {
                    throw new exception(
                        "[RequestChangeStateBarSpace::Error] Estado inválido ou fora de ordem",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY_BASE, 10, 0)
                    );
                }

                // guarda ponto enviado pelo client
                pgi.bar_space.setServerPoint(state, clientPoint);

                // soltou a barra (impact)
                if (state == 3) // soltou
                {
                    float serverPoint = pgi.bar_space.CalculateServerPoint();
                    float diff = Math.Abs(serverPoint - clientPoint);

                    // adiciona ao detector
                    pgi.bar_space_analize.Add(diff);

                    // só LOGA se realmente suspeito
                    if (pgi.bar_space_analize.IsSuspicious(out float avg))
                    {
                        _smp.LogManager.Instance.push(
                            new AppMessage(
                                $"[TourneyBase::RequestChangeStateBarSpace][Hacker] UID={session.UserInfo.UID} avgDiff={avg:0.000}",
                                type_msg.CL_FILE_LOG_AND_CONSOLE
                            )
                        );

                        // aqui futuramente:
                        // ServerFlag, Kick, Watchlist, etc
                    }

                    pgi.bar_space.clear();
                    pgi.tempo = 0;
                }
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(
                    new AppMessage(
                        "[TourneyBase::RequestChangeStateBarSpace][Error] " + e.getFullMessageError(),
                        type_msg.CL_FILE_LOG_AND_CONSOLE
                    )
                );
            }
        }

        public override void RequestActivePowerShot(Player session, Packet packet)
        {
            var p = new Packet();

            try
            {

                byte ps = packet.ReadByte();

                InitPlayerInfo("RequestActivePowerShot",
                    "tentou ativar power shot, no jogo",
                   session, out PlayerGameInfo pgi);

                pgi.power_shot = ps;

                if (ps == 1)//ps1
                {
                }
                else if (ps == 2)//ps2
                {
                }
                else
                    if (ps == 0)//desativado
                    {
                    }
                // Resposta para Active Power Shot
                p.init_plain(0x58);

                p.WriteInt32(session.ConnectionID);
                p.WriteByte(pgi.power_shot);
                session.Send(p);
            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[TourneyBase::RequestActivePowerShot][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void RequestChangeClub(Player session, Packet packet)
        {
            var p = new Packet();

            try
            {
                byte club = packet.ReadByte();


                InitPlayerInfo("RequestChangeClub",
                    "tentou trocar taco no jogo",
                   session, out PlayerGameInfo pgi);

                pgi.club = club;//otimo para o changeSpaceBar....

                // Resposta para Change Club
                p.init_plain(0x59);

                p.WriteInt32(session.ConnectionID);
                p.WriteByte(pgi.club);
                session.Send(p); 
            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[TourneyBase::RequestChangeClub][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void RequestUseActiveItem(Player session, Packet packet)
        { 
            var p = new Packet();

            try
            {

                uint item_typeid = packet.ReadUInt32();

                InitPlayerInfo("RequestUseActiveItem",
                    "tentou usar item ativo no jogo",
                   session, out PlayerGameInfo pgi);

                if (item_typeid == 0)
                {
                    throw new exception("[TourneyBase::RequestActiveItem][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou usar State item[TYPEID=" + Convert.ToString(item_typeid) + "] no jogo, mas o item__typeid é invalido(zero). Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY_BASE,
                        7, 0));
                }

                var iffItem = sIff.Instance.findCommomItem(item_typeid);

                if (iffItem == null)
                {
                    throw new exception("[TourneyBase::RequestActiveItem][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + " tentou usar State item[TYPEID=" + Convert.ToString(item_typeid) + "] no jogo, mas o item nao tem no IFF_STRUCT. Hacker ou Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY_BASE,
                        77, 0));
                }

                if (sIff.Instance.getItemGroupIdentify(item_typeid) != IFF_GROUP.ITEM || !sIff.Instance.IsItemEquipable(item_typeid))
                {
                    throw new exception("[TourneyBase::RequestActiveItem][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou usar State item[TYPEID=" + Convert.ToString(item_typeid) + "] no jogo, mas o item nao é equipavel(usar). Hacker ou Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY_BASE,
                        78, 0));
                }

                // Verifica se tem algum card de tempo equipado com efeito de mulligan rose
                if (item_typeid == MULLIGAN_ROSE_TYPEID)
                {

                    // Card Special - Efeito mulligan rose == 32
                    if (session.Inventory.CardEquipment.Count(el =>
                    {
                        return (el.parts_typeid == 0 && el.parts_typeid == 0 && sIff.Instance.getItemSubGroupIdentify22(el._typeid) == 2 && el.efeito == 32);
                    }) > 0)
                    {
                        var rand = new Random();
                        // Resposta para o Use Active Item
                        p.init_plain(0x5A);

                        p.WriteUInt32(item_typeid);
                        p.WriteInt32(rand.Next()); // Seed Rand Failure Active Item
                        p.WriteInt32(session.ConnectionID);
                        SendBroadCast(p);

                        // Sai
                        return;

                    }

                }

                // Verifica se o player tem o item para usar
                var pWi = session.Inventory.FindWarehouseItemByTypeid(item_typeid);

                if (pWi == null)
                {
                    throw new exception("[TourneyBase::RequestActiveItem][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou usar State item[TYPEID=" + Convert.ToString(item_typeid) + "] no jogo, mas ele nao tem esse item. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY_BASE,
                        8, 0));
                }

                var it = pgi.used_item.v_active.FirstOrDefault(c => c.Key == pWi._typeid);

                if (it.Value == null)
                {
                    throw new exception("[TourneyBase::RequestActiveItem][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou usar State item[TYPEID=" + Convert.ToString(item_typeid) + "] no jogo, mas ele nao equipou esse item. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY_BASE,
                        9, 0));
                }

                if (it.Value.count >= it.Value.v_slot.Count())
                {
                    throw new exception("[TourneyBase::RequestActiveItem][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou usar State item[TYPEID=" + Convert.ToString(item_typeid) + "] no jogo, mas ele ja usou todos os item desse que ele equipou. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY_BASE,
                        10, 0));
                }

                // Add +1 ou countador
                it.Value.count++;

                // item que foi usado na tacada
                pgi.item_active_used_shot = pWi._typeid;

                // Resposta para o Use Active Item
                p.init_plain(0x5A);

                p.WriteUInt32(pWi._typeid);
                p.WriteInt32(Random.Shared.Next()); // Seed Rand Failure Active Item
                p.WriteInt32(session.ConnectionID);
                SendBroadCast(p);
            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[TourneyBase::RequestUseActiveItem][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void RequestChangeStateTypeing(Player session, Packet packet)
        { 
            var p = new Packet();

            try
            {

                short typeing = packet.ReadInt16();

                InitPlayerInfo("RequestChangeStateTypeing",
                    "tentou mudar o estado de escrevendo no jogo",
                   session, out PlayerGameInfo pgi);

                pgi.typeing = typeing;

                // Resposta para Change State Typeing 
                p.init_plain(0x5D);

                p.WriteInt32(session.ConnectionID);
                p.WriteInt16(pgi.typeing);

                session.Send(p);
            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[TourneyBase::RequestChangeStateTypeing][Error] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void RequestMoveBall(Player session, Packet packet)
        { 
            var p = new Packet();

            try
            {

                float x = packet.ReadFloat();
                float y = packet.ReadFloat();
                float z = packet.ReadFloat();

                InitPlayerInfo("RequestMoveBall",
                    "tentou recolocar a bola no jogo",
                   session, out PlayerGameInfo pgi);

                pgi.location.x = x;
                pgi.location.y = y;
                pgi.location.z = z;

                // Add + 1 a tacada, já que ele recolocou em vez de tacar
                pgi.data.tacada_num++;

                // Resposta para Move Ball
                p.init_plain(0x60);

                p.WriteFloat(pgi.location.x);
                p.WriteFloat(pgi.location.y);
                p.WriteFloat(pgi.location.z);
                session.Send(p);
            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[TourneyBase::RequestMoveBall][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void RequestChangeStateChatBlock(Player session, Packet packet)
        { 
            var p = new Packet();

            try
            {

                byte chat_block = packet.ReadByte();

                InitPlayerInfo("RequestChangeStateChatBlock",
                    "tentou mudar estado do chat block no jogo",
                   session, out PlayerGameInfo pgi);

                pgi.chat_block = chat_block;

                // Resposta para Chat Block
                p.init_plain(0xAC);

                p.WriteInt32(session.ConnectionID);
                p.WriteByte(pgi.chat_block);
                session.Send(p);
            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[TourneyBase::RequestChangeStateChatBlock][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void RequestActiveBooster(Player session, Packet packet)
        { 
            var p = new Packet();

            try
            {

                float velocidade = packet.ReadFloat();

                InitPlayerInfo("RequestActiveBooster",
                    "tentou ativar Time Booster no jogo",
                   session, out PlayerGameInfo pgi);

                if (!(session.UserInfo.UserCapabilities.UserPremium))
                { // NÃO é premium user — precisa ter o item Time Booster

                    var pWi = session.Inventory.FindWarehouseItemByTypeid(TIME_BOOSTER_TYPEID);

                    if (pWi == null)
                    {
                        throw new exception("[TourneyBase::RequestActiveBooster][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou ativar time booster, mas ele nao tem o item passive. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY_BASE,
                            11, 0));
                    }

                    if (pWi.STDA_C_ITEM_QNTD <= 0)
                    {
                        throw new exception("[TourneyBase::RequestActiveBooster][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou ativar time booster, mas ele nao tem quantidade suficiente[VALUE=" + Convert.ToString(pWi.STDA_C_ITEM_QNTD) + ", REQUEST=1] do item de time booster.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY_BASE,
                            12, 0));
                    }

                    var it = pgi.used_item.v_passive.FirstOrDefault(c => c.Key == pWi._typeid);

                    if (it.Value == null)
                    {
                        throw new exception("[TourneyBase::RequestActiveBooster][Error] Normal[UID = " + Convert.ToString(session.UserInfo.UID) + "] tentou ativar time booster, mas ele nao tem ele no item passive usados do server. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY_BASE,
                            13, 0));
                    }

                    if ((short)it.Value.count >= pWi.STDA_C_ITEM_QNTD)
                    {
                        throw new exception("[TourneyBase::RequestActiveBooster][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou ativar time booster, mas ele ja usou todos os time booster. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY_BASE,
                            14, 0));
                    }

                    // Add +1 ao item passive usado
                    it.Value.count++;

                }
                else
                { // Soma +1 no contador de counter item do booster do player e passive item

                    pgi.sys_achieve.incrementCounter(0x6C400075u);

                    pgi.sys_achieve.incrementCounter(0x6C400050u);
                }

                // Resposta para Active Booster
                p.init_plain(0xC7);

                p.WriteFloat(velocidade);
                p.WriteInt32(session.ConnectionID);
                session.Send(p);
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[TourneyBase::RequestActiveBooster][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void RequestActiveReplay(Player session, Packet packet)
        {
            var p = new Packet();

            try
            {

                uint _typeid = packet.ReadUInt32();

                if (_typeid == 0)
                {
                    throw new exception("[TourneyBase::RequestActiveReplay][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou ativar Replay[TYPEID=" + Convert.ToString(_typeid) + "], mas o _typeid é invalido(zero). Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY_BASE,
                        200, 0));
                }

                var pWi = session.Inventory.FindWarehouseItemByTypeid(_typeid);

                if (pWi == null)
                {
                    throw new exception("[TourneyBase::RequestActiveReplay][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou ativar Replay[TYPEID=" + Convert.ToString(_typeid) + "], mas ele nao tem o item. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY_BASE,
                        201, 0));
                }

                if (pWi.STDA_C_ITEM_QNTD <= 0)
                {
                    throw new exception("[TourneyBase::RequestActiveReplay][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou ativar Replay[TYPEID=" + Convert.ToString(_typeid) + "], mas ele nao tem quantidade suficiente[VALUE=" + Convert.ToString(pWi.STDA_C_ITEM_QNTD) + ", REQUEST=1] do item. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY_BASE,
                        202, 0));
                }

                // UPDATE ON SERVER AND DB
                stItem item = new stItem();

                item.type = 2;
                item._typeid = pWi._typeid;
                item.id = (int)pWi.id;
                item.qntd = 1;
                item.STDA_C_ITEM_QNTD = (short)(item.qntd * -1);

                if (ItemManager.removeItem(item, session) <= 0)
                {
                    throw new exception("[TourneyBase::RequestActiveReplay][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou ativar Replay[TYPEID=" + Convert.ToString(_typeid) + "], nao conseguiu deletar ou atualizar qntd do item[TYPEID=" + Convert.ToString(item._typeid) + ", ID=" + Convert.ToString(item.id) + "]", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY_BASE,
                        203, 0));
                }

                // UPDATE ON GAME
                // Resposta para o Active Replay
                p.init_plain(0xA4);

                p.WriteUInt16((ushort)item.stat.qntd_dep);
                session.Send(p);
            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[TourneyBase::RequestActiveReplay][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void RequestActiveCutin(Player session, Packet packet)
        { 
            var p = new Packet();

            try
            {
                stActiveCutin ac = new stActiveCutin
                {
                    uid = packet.ReadUInt32(),
                    tipo = packet.ReadUInt32(),
                    opt = packet.ReadUInt16(),
                    char_typeid = packet.ReadUInt32(),
                    active = packet.ReadByte()
                };

                Player s = null;

                if (ac.uid != session.UserInfo.UID || (s = FindSessionByUID(ac.uid)) == null)
                {
                    throw new exception("[TourneyBase::RequestActiveCutin][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou activar cutin[CHAR_TYPEID=" + Convert.ToString(ac.char_typeid) + ", TIPO=" + Convert.ToString(ac.tipo) + ", OPT=" + Convert.ToString(ac.opt) + ",  ACTIVE=" + Convert.ToString(ac.active) + "] de um Normal[UID=" + Convert.ToString(ac.uid) + "], mas o jogador nao esta no jogo. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY_BASE,
                        1, 0x5200101));
                }

                if (s.Inventory.UserEquippedItem.CharacterEquiped == null)
                {
                    throw new exception("[TourneyBase::RequestActiveCutin][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou activar cutin[CHAR_TYPEID=" + Convert.ToString(ac.char_typeid) + ", TIPO=" + Convert.ToString(ac.tipo) + ", OPT=" + Convert.ToString(ac.opt) + ",  ACTIVE=" + Convert.ToString(ac.active) + "] de um Normal[UID=" + Convert.ToString(ac.uid) + "], mas o jogador nao tem um character equipado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY_BASE,
                        2, 0x5200102));
                }

                CutinInformation pCutin = null;

                // Cutin Padrão que o player equipa, quando o cliente envia o cutin type é que é efeito por roupas equipadas
                if (sIff.Instance.getItemGroupIdentify(ac.char_typeid) == IFF_GROUP.CHARACTER && ac.active == 1)
                {

                    if (s.Inventory.UserEquippedItem.CharacterEquiped._typeid != ac.char_typeid)
                    {
                        throw new exception("[TourneyBase::RequestActiveCutin][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou activar cutin[CHAR_TYPEID=" + Convert.ToString(ac.char_typeid) + ", TIPO=" + Convert.ToString(ac.tipo) + ", OPT=" + Convert.ToString(ac.opt) + ",  ACTIVE=" + Convert.ToString(ac.active) + "] de um Normal[UID=" + Convert.ToString(ac.uid) + "], mas o character _typeid passado nao é igual ao equipado do player. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY_BASE,
                            4, 0x5200104));
                    }

                    WarehouseItemEx pWi = null;

                    var end = (s.Inventory.UserEquippedItem.CharacterEquiped.cut_in.Length);

                    for (var i = 0; i < end; ++i)
                    {

                        if (s.Inventory.UserEquippedItem.CharacterEquiped.cut_in[i] > 0)
                        {

                            if ((pWi = session.Inventory.FindWarehouseItemById((int)s.Inventory.UserEquippedItem.CharacterEquiped.cut_in[i])) != null)
                            {

                                if ((pCutin = sIff.Instance.findCutinInfomation(pWi._typeid)) == null)
                                {
                                    throw new exception("[TourneyBase::RequestActiveCutin][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou activar cutin[CHAR_TYPEID=" + Convert.ToString(ac.char_typeid) + ", TIPO=" + Convert.ToString(ac.tipo) + ", OPT=" + Convert.ToString(ac.opt) + ", ACTIVE=" + Convert.ToString(ac.active) + "] de um Normal[UID=" + Convert.ToString(ac.uid) + "], mas o jogador nao tem esse cutin[TYPEID=" + Convert.ToString(pWi._typeid) + ", ID=" + Convert.ToString(pWi.id) + "]. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY_BASE,
                                        3, 0x5200103));
                                }

                                if (pCutin.tipo.ulCondition == ac.tipo)
                                {
                                    break;
                                }
                                else if ((i + 1) == end)
                                {
                                    throw new exception("[TourneyBase::RequestActiveCutin][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou activar cutin[CHAR_TYPEID=" + Convert.ToString(ac.char_typeid) + ", TIPO=" + Convert.ToString(ac.tipo) + ", OPT=" + Convert.ToString(ac.opt) + ",  ACTIVE=" + Convert.ToString(ac.active) + "] de um Normal[UID=" + Convert.ToString(ac.uid) + "], mas o jogador nao tem esse cutin[TYPEID=" + Convert.ToString(pWi._typeid) + ", ID=" + Convert.ToString(pWi.id) + "]. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY_BASE,
                                        3, 0x5200103));
                                }
                            }
                        }
                    }

                }
                else if (sIff.Instance.getItemGroupIdentify(ac.char_typeid) == IFF_GROUP.SKIN && ac.active == 0)
                {

                    // Verificar se ele tem os itens para ativar esse Cutin

                    if ((pCutin = sIff.Instance.findCutinInfomation(ac.char_typeid)) == null)
                    {
                        throw new exception("[TourneyBase::RequestActiveCutin][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou activar cutin[CHAR_TYPEID=" + Convert.ToString(ac.char_typeid) + ", TIPO=" + Convert.ToString(ac.tipo) + ", OPT=" + Convert.ToString(ac.opt) + ",  ACTIVE=" + Convert.ToString(ac.active) + "] de um Normal[UID=" + Convert.ToString(ac.uid) + "], mas o jogador nao tem esse cutin[TYPEID=" + Convert.ToString(ac.char_typeid) + "]. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY_BASE,
                            3, 0x5200103));
                    }

                    // Esses que passa o cutin _typeid, pode ativar com Type 1 e 2, 1 PS e 2 PS

                }

                if (pCutin == null)
                {
                    throw new exception("[TourneyBase::RequestActiveCutin][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou activar cutin[CHAR_TYPEID=" + Convert.ToString(ac.char_typeid) + ", TIPO=" + Convert.ToString(ac.tipo) + ", OPT=" + Convert.ToString(ac.opt) + ",  ACTIVE=" + Convert.ToString(ac.active) + "] de um Normal[UID=" + Convert.ToString(ac.uid) + "], mas o cution nao foi encontrado[TYPEID=" + Convert.ToString(ac.char_typeid) + "]. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY_BASE,
                        4, 0x5200104));
                }

                // Resposta para Active Cutin
                p.init_plain(0x18D);

                p.WriteByte(1); // OK

                p.WriteUInt32(pCutin._typeid);
                p.WriteUInt32(pCutin.sector);
                p.WriteUInt32(pCutin.tipo.ulCondition);

                p.WriteUInt32(pCutin.img[0].tipo);
                p.WriteUInt32(pCutin.img[1].tipo);
                p.WriteUInt32(pCutin.img[2].tipo);
                p.WriteUInt32(pCutin.img[3].tipo);

                p.WriteUInt32(pCutin.tempo);

                p.WriteString(pCutin.img[0].sprite, 40);
                p.WriteString(pCutin.img[1].sprite, 40);
                p.WriteString(pCutin.img[2].sprite, 40);
                p.WriteString(pCutin.img[3].sprite, 40);
                session.Send(p);

                // No Modo GrandZodic, não envia Cutin, então envia o pacote18D com option 0(Uint8), e valor 3(Uint16)

            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[TourneyBase::RequestActiveCutin][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x18D);

                p.WriteByte(0); // OPT

                p.WriteUInt16(1); // Error
                session.Send(p);
            }
        }

        public override void RequestActiveRing(Player session, Packet packet)
        { 
            var p = new Packet();

            try
            {

                stRing r = new stRing
                {
                    _typeid = packet.ReadUInt32(),
                    effect_value = packet.ReadUInt32(),
                    efeito = packet.ReadByte()
                };

                if (r._typeid == 0)
                {
                    throw new exception("[TourneyBase::RequestActiveRing][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou ativar Anel[TYPEID=" + Convert.ToString(r._typeid) + "], mas o _typeid é invalido(zero). Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY_BASE,
                        30, 0x330001));
                }

                var pWi = session.Inventory.FindWarehouseItemByTypeid(r._typeid);

                if (pWi == null)
                {
                    throw new exception("[TourneyBase::RequestActiveRing][Error] Normal[UID = " + Convert.ToString(session.UserInfo.UID) + "] tentou ativar Anel[TYPEID = " + Convert.ToString(r._typeid) + "], mas ele nao tem o anel. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY_BASE,
                        31, 0x330002));
                }

                if (session.Inventory.UserEquippedItem.CharacterEquiped == null)
                {
                    throw new exception("[TourneyBase::RequestActiveRing][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou ativar Anel[TYPEID=" + Convert.ToString(r._typeid) + "], mas ele nao esta com um Character equipado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY_BASE,
                        32, 0x330003));
                }

                if (!session.Inventory.UserEquippedItem.CharacterEquiped.auxparts.Any(c => c == r._typeid))
                {
                    throw new exception("[TourneyBase::RequestActiveRing][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou ativar Anel[TYPEID=" + Convert.ToString(r._typeid) + "], mas ele nao esta equipado com o anel. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY_BASE,
                        33, 0x330004));
                }

                // Adiciona o efeito que foi ativado
                CheckEffectItemAndSet(session, r._typeid);

                // Resposta para o cliente
                p.init_plain(0x237);

                p.WriteUInt32(0); // OK

                p.WriteUInt32(session.UserInfo.UID);

                p.WriteUInt32(r._typeid);
                p.WriteByte(r.efeito);
                session.Send(p);
            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[TourneyBase::RequestActiveRing][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Resposta Error
                p.init_plain(0x237);

                p.WriteUInt32((ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) == STDA_ERROR_TYPE.TOURNEY_BASE) ? ExceptionError.STDA_SYSTEM_ERROR_DECODE(e.getCodeError()) : 0x330000);
                session.Send(p);
            }
        }

        public override void RequestActiveRingGround(Player session, Packet packet)
        { 
            var p = new Packet();

            try
            {

                stRingGround rg = new stRingGround
                {
                    efeito = (AbilityEffect)packet.ReadUInt32()
                };
                rg.ring[0] = packet.ReadUInt32();
                rg.ring[1] = packet.ReadUInt32();
                rg.option = packet.ReadUInt32();

                // Log para saber qual é o efeito 31(0x1F)
                if ((AbilityEffect)(rg.efeito) == AbilityEffect.UNKNOWN_31)
                {
                    _smp.LogManager.Instance.push(new AppMessage("[TourneyBase::RequestActiveRingGround][Log] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] ativou o efeito 0x1F(31) com os itens[TYPEID_1=" + Convert.ToString(rg.ring[0]) + ", TYPEID_2=" + Convert.ToString(rg.ring[1]) + "] e OPTION=" + Convert.ToString(rg.option), type_msg.CL_FILE_LOG_AND_CONSOLE));
                }

                if (!rg.isValid())
                {
                    throw new exception("[TourneyBase::RequestActiveRingGround][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou ativar Anel de Terreno[TYPE=" + Convert.ToString(rg.efeito) + ", RING[0]=" + Convert.ToString(rg.ring[0]) + ", RING[1]=" + Convert.ToString(rg.ring[1]) + ", OPTION=" + Convert.ToString(rg.option) + "], mas os _typeid's nao sao validos. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY_BASE,
                        50, 0x340001));
                }

                if (session.Inventory.UserEquippedItem.CharacterEquiped == null)
                {
                    throw new exception("[TourneyBase::RequestActiveRingGround][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou ativar Anel de Terreno[TYPE=" + Convert.ToString(rg.efeito) + ", RING[0]=" + Convert.ToString(rg.ring[0]) + ", RING[1]=" + Convert.ToString(rg.ring[1]) + ", OPTION=" + Convert.ToString(rg.option) + "], mas ele nao esta com um Character equipado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY_BASE,
                        51, 0x340002));
                }

                if (sIff.Instance.getItemGroupIdentify(rg.ring[0]) == IFF_GROUP.AUX_PART)
                { // Anel

                    var pRing = session.Inventory.FindWarehouseItemByTypeid(rg.ring[0]);

                    if (pRing == null)
                    {
                        throw new exception("[TourneyBase::RequestActiveRingGround][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou ativar Anel de Terreno[TYPE=" + Convert.ToString(rg.efeito) + ", RING[0]=" + Convert.ToString(rg.ring[0]) + ", RING[1]=" + Convert.ToString(rg.ring[1]) + ", OPTION=" + Convert.ToString(rg.option) + "], mas ele nao tem o Anel[0]. hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY_BASE,
                            52, 0x340002));
                    }

                    if (!session.Inventory.UserEquippedItem.CharacterEquiped.auxparts.Any(c => c == rg.ring[0]))
                    {
                        throw new exception("[TourneyBase::RequestActiveRingGround][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou ativar Anel de Terreno[TYPE=" + Convert.ToString(rg.efeito) + ", RING[0]=" + Convert.ToString(rg.ring[0]) + ", RING[1]=" + Convert.ToString(rg.ring[1]) + ", OPTION=" + Convert.ToString(rg.option) + "], mas ele nao esta com o Anel[0] equipado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY_BASE,
                            53, 0x340003));
                    }

                    if (rg.ring[0] != rg.ring[1])
                    { // Ativou Habilidade em conjunto 2 aneis

                        var pRing2 = session.Inventory.FindWarehouseItemByTypeid(rg.ring[1]);

                        if (pRing2 == null)
                        {
                            throw new exception("[TourneyBase::RequestActiveRingGround][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou ativar Anel de Terreno[TYPE=" + Convert.ToString(rg.efeito) + ", RING[0]=" + Convert.ToString(rg.ring[0]) + ", RING[1]=" + Convert.ToString(rg.ring[1]) + ", OPTION=" + Convert.ToString(rg.option) + "], mas ele nao tem o Anel[1]. hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY_BASE,
                                52, 0x340002));
                        }

                        if (!session.Inventory.UserEquippedItem.CharacterEquiped.auxparts.Any(c => c == rg.ring[1]))
                        {
                            throw new exception("[TourneyBase::RequestActiveRingGround][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou ativar Anel de Terreno[TYPE=" + Convert.ToString(rg.efeito) + ", RING[0]=" + Convert.ToString(rg.ring[0]) + ", RING[1]=" + Convert.ToString(rg.ring[1]) + ", OPTION=" + Convert.ToString(rg.option) + "], mas ele nao esta com o Anel[1] equipado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY_BASE,
                                53, 0x340003));
                        }
                    }

                }
                else if (sIff.Instance.getItemGroupIdentify(rg.ring[0]) == IFF_GROUP.PART)
                { // Part

                    var pRing = session.Inventory.FindWarehouseItemByTypeid(rg.ring[0]);

                    if (pRing == null)
                    {
                        throw new exception("[TourneyBase::RequestActiveRingGround][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou ativar Anel de Terreno[TYPE=" + Convert.ToString(rg.efeito) + ", RING[0]=" + Convert.ToString(rg.ring[0]) + ", RING[1]=" + Convert.ToString(rg.ring[1]) + ", OPTION=" + Convert.ToString(rg.option) + "], mas ele nao tem o Part[0]. hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY_BASE,
                            52, 0x340002));
                    }

                    if (!session.Inventory.UserEquippedItem.CharacterEquiped.auxparts.Any(c => c == rg.ring[0]))
                    {
                        throw new exception("[TourneyBase::RequestActiveRingGround][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou ativar Anel de Terreno[TYPE=" + Convert.ToString(rg.efeito) + ", RING[0]=" + Convert.ToString(rg.ring[0]) + ", RING[1]=" + Convert.ToString(rg.ring[1]) + ", OPTION=" + Convert.ToString(rg.option) + "], mas ele nao esta com o Part[0] equipado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY_BASE,
                            53, 0x340003));
                    }

                    if (rg.ring[0] != rg.ring[1])
                    { // Ativou Habilidade em conjunto 2 aneis

                        var pRing2 = session.Inventory.FindWarehouseItemByTypeid(rg.ring[1]);

                        if (pRing2 == null)
                        {
                            throw new exception("[TourneyBase::RequestActiveRingGround][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou ativar Anel de Terreno[TYPE=" + Convert.ToString(rg.efeito) + ", RING[0]=" + Convert.ToString(rg.ring[0]) + ", RING[1]=" + Convert.ToString(rg.ring[1]) + ", OPTION=" + Convert.ToString(rg.option) + "], mas ele nao tem o Part[1]. hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY_BASE,
                                52, 0x340002));
                        }

                        if (!session.Inventory.UserEquippedItem.CharacterEquiped.auxparts.Any(c => c == rg.ring[1]))
                        {
                            throw new exception("[TourneyBase::RequestActiveRingGround][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou ativar Anel de Terreno[TYPE=" + Convert.ToString(rg.efeito) + ", RING[0]=" + Convert.ToString(rg.ring[0]) + ", RING[1]=" + Convert.ToString(rg.ring[1]) + ", OPTION=" + Convert.ToString(rg.option) + "], mas ele nao esta com o Part[1] equipado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY_BASE,
                                53, 0x340003));
                        }
                    }

                }
                else if (sIff.Instance.getItemGroupIdentify(rg.ring[0]) == IFF_GROUP.MASCOT)
                {

                    var pMascot = session.Inventory.FindMascotByTypeid(rg.ring[0]);

                    if (pMascot == null)
                    {
                        throw new exception("[TourneyBase::RequestActiveRingGround][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou ativar Anel de Terreno[TYPE=" + Convert.ToString(rg.efeito) + ", RING[0]=" + Convert.ToString(rg.ring[0]) + ", RING[1]=" + Convert.ToString(rg.ring[1]) + ", OPTION=" + Convert.ToString(rg.option) + "], mas ele nao tem o Mascot[0]. hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY_BASE,
                            52, 0x340002));
                    }

                    if (rg.ring[0] != rg.ring[1])
                    { // Ativou Habilidade em conjunto 2 aneis

                        var pPart2 = session.Inventory.FindWarehouseItemByTypeid(rg.ring[1]);

                        if (pPart2 == null)
                        {
                            throw new exception("[TourneyBase::RequestActiveRingGround][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou ativar Anel de Terreno[TYPE=" + Convert.ToString(rg.efeito) + ", RING[0]=" + Convert.ToString(rg.ring[0]) + ", RING[1]=" + Convert.ToString(rg.ring[1]) + ", OPTION=" + Convert.ToString(rg.option) + "], mas ele nao tem o Part[1]. hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY_BASE,
                                52, 0x340002));
                        }

                        if (!session.Inventory.UserEquippedItem.CharacterEquiped.auxparts.Any(c => c == rg.ring[1]))
                        {
                            throw new exception("[TourneyBase::RequestActiveRingGround][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou ativar Anel de Terreno[TYPE=" + Convert.ToString(rg.efeito) + ", RING[0]=" + Convert.ToString(rg.ring[0]) + ", RING[1]=" + Convert.ToString(rg.ring[1]) + ", OPTION=" + Convert.ToString(rg.option) + "], mas ele nao esta com o Part[1] equipado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY_BASE,
                                53, 0x340003));
                        }
                    }
                }

                // Adiciona o efeito que foi ativado
                //checkEffectItemAndSet(session, rg.ring[0]);
                SetEffectActiveInShot(session, enumToBitValue((AbilityEffect)rg.efeito));

                // Resposta para o Active Ring Terreno
                p.init_plain(0x266);

                p.WriteUInt32(0); // OK

                p.WriteBytes(rg.ToArray());

                p.WriteUInt32(session.UserInfo.UID);
                session.Send(p); 
            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[TourneyBase::RequestActiveRingGround][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Resposta Error
                p.init_plain(0x266);

                p.WriteUInt32((ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) == STDA_ERROR_TYPE.TOURNEY_BASE) ? ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) : 0x340000);
                session.Send(p);
            }
        }

        public override void RequestActiveRingPawsRainbowJP(Player session, Packet packet)
        { 
            var p = new Packet();

            try
            {

                // Efeito patinha não passa o TYPEID do item que ativou
                SetEffectActiveInShot(session, enumToBitValue(AbilityEffect.PAWS_ACCUMULATE));

                // Resposta para o Active Ring Paws Rainbow JP
                p.init_plain(0x27E);

                p.WriteUInt32(session.UserInfo.UID);
                session.Send(p);
            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[TourneyBase::RequestActiveRingPawsRainbowJP][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void RequestActiveRingPawsRingSetJP(Player session, Packet packet)
        { 
            var p = new Packet();

            try
            {

                // Efeito patinha não passa o TYPEID do item que ativou
                SetEffectActiveInShot(session, enumToBitValue(AbilityEffect.PAWS_NOT_ACCUMULATE));

                // Resposta para o Active Ring Paws Ring Set JP
                p.init_plain(0x281);

                p.WriteUInt32(session.UserInfo.UID);

                session.Send(p);
            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[TourneyBase::RequestActiveRingPawsRingSetJP][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void RequestActiveRingPowerGagueJP(Player session, Packet packet)
        { 
            var p = new Packet();

            try
            {

                stRingGround rpg = new stRingGround();
                rpg.efeito = (AbilityEffect)packet.ReadUInt32();
                rpg.ring[0] = packet.ReadUInt32();
                rpg.ring[1] = packet.ReadUInt32();
                rpg.option = packet.ReadUInt32();
                if (!rpg.isValid())
                {
                    throw new exception("[TourneyBase::RequestActiveRingPowerGagueJP][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou ativar Anel de Barra de PS [JP] [TYPE=" + Convert.ToString(rpg.efeito) + ", RING[0]=" + Convert.ToString(rpg.ring[0]) + ", RING[1]=" + Convert.ToString(rpg.ring[1]) + ", OPTION=" + Convert.ToString(rpg.option) + "], mas os _typeid's nao sao validos. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY_BASE,
                        150, 0x390001));
                }

                if (session.Inventory.UserEquippedItem.CharacterEquiped == null)
                {
                    throw new exception("[TourneyBase::RequestActiveRingPowerGagueJP][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou ativar Anel de Barra de PS [JP] [TYPE=" + Convert.ToString(rpg.efeito) + ", RING[0]=" + Convert.ToString(rpg.ring[0]) + ", RING[1]=" + Convert.ToString(rpg.ring[1]) + ", OPTION=" + Convert.ToString(rpg.option) + "], mas ele nao esta com um Character equipado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY_BASE,
                        151, 0x390002));
                }

                var pRing = session.Inventory.FindWarehouseItemByTypeid(rpg.ring[0]);

                if (pRing == null)
                {
                    throw new exception("[TourneyBase::RequestActiveRingPowerGagueJP][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou ativar Anel de Barra de PS [JP] [TYPE=" + Convert.ToString(rpg.efeito) + ", RING[0]=" + Convert.ToString(rpg.ring[0]) + ", RING[1]=" + Convert.ToString(rpg.ring[1]) + ", OPTION=" + Convert.ToString(rpg.option) + "], mas ele nao tem o Anel[0]. hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY_BASE,
                        152, 0x390002));
                }

                if (!session.Inventory.UserEquippedItem.CharacterEquiped.auxparts.Any(c => c == rpg.ring[0]))
                {
                    throw new exception("[TourneyBase::RequestActiveRingPowerGagueJP][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou ativar Anel de Barra de PS [JP] [TYPE=" + Convert.ToString(rpg.efeito) + ", RING[0]=" + Convert.ToString(rpg.ring[0]) + ", RING[1]=" + Convert.ToString(rpg.ring[1]) + ", OPTION=" + Convert.ToString(rpg.option) + "], mas ele nao esta com o Anel[0] equipado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY_BASE,
                        153, 0x390003));
                }

                if (rpg.ring[0] != rpg.ring[1])
                { // Ativou Habilidade em conjunto 2 aneis

                    var pRing2 = session.Inventory.FindWarehouseItemByTypeid(rpg.ring[1]);

                    if (pRing2 == null)
                    {
                        throw new exception("[TourneyBase::RequestActiveRingPowerGagueJP][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou ativar Anel de Barra de PS [JP] [TYPE=" + Convert.ToString(rpg.efeito) + ", RING[0]=" + Convert.ToString(rpg.ring[0]) + ", RING[1]=" + Convert.ToString(rpg.ring[1]) + ", OPTION=" + Convert.ToString(rpg.option) + "], mas ele nao tem o Anel[1]. hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY_BASE,
                            152, 0x390002));
                    }

                    if (!session.Inventory.UserEquippedItem.CharacterEquiped.auxparts.Any(c => c == rpg.ring[1]))
                    {
                        throw new exception("[TourneyBase::RequestActiveRingPowerGagueJP][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou ativar Anel de Barra de PS [JP] [TYPE=" + Convert.ToString(rpg.efeito) + ", RING[0]=" + Convert.ToString(rpg.ring[0]) + ", RING[1]=" + Convert.ToString(rpg.ring[1]) + ", OPTION=" + Convert.ToString(rpg.option) + "], mas ele nao esta com o Anel[1] equipado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY_BASE,
                            153, 0x390003));
                    }
                }

                // Effect
                SetEffectActiveInShot(session, enumToBitValue(AbilityEffect.POWER_GAUGE_FREE));

                // Resposta para o Active Ring Power Gague JP
                p.init_plain(0x27F);

                p.WriteUInt32(session.UserInfo.UID);
                session.Send(p);
            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[TourneyBase::RequestActiveRingPowerGagueJP][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void RequestActiveRingMiracleSignJP(Player session, Packet packet)
        { 
            var p = new Packet();

            try
            {

                uint _typeid = packet.ReadUInt32();

                if (_typeid == 0)
                {
                    throw new exception("[TourneyBase::RequestActiveRingMiracleSignJP][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou ativar 'Anel'[TYPEID=" + Convert.ToString(_typeid) + "] Olho Magico JP, mas o _typeid é invalido(zero)", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY_BASE,
                        70, 0x350001));
                }

                WarehouseItemEx pWi = session.Inventory.FindWarehouseItemByTypeid(_typeid);

                if (pWi == null)
                {
                    throw new exception("[TourneyBase::RequestActiveRingMiracleSignJP][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou ativar 'Anel'[TYPEID=" + Convert.ToString(_typeid) + "] Olho Magico JP, mas ele nao tem o 'Anel'. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY_BASE,
                        71, 0x350002));
                }

                if (session.Inventory.UserEquippedItem.CharacterEquiped == null)
                {
                    throw new exception("[TourneyBase::RequestActiveRingMiracleSignJP][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou ativar 'Anel'[TYPEID=" + Convert.ToString(_typeid) + "] Olho Magico JP, mas ele nao esta com um Character equipado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY_BASE,
                        72, 0x350003));
                }

                if (sIff.Instance.getItemGroupIdentify(_typeid) == IFF_GROUP.AUX_PART)
                { // Anel

                    if (!session.Inventory.UserEquippedItem.CharacterEquiped.auxparts.Any(c => c == _typeid))
                    {
                        throw new exception("[TourneyBase::RequestActiveRingMiracleSignJP][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou ativar 'Anel'[TYPEID=" + Convert.ToString(_typeid) + "] Olho Magico JP, mas ele nao esta com o Anel equipado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY_BASE,
                            0x73, 0x350004));
                    }

                }
                else if (sIff.Instance.getItemGroupIdentify(_typeid) == IFF_GROUP.PART)
                { // Part

                    // Corrigido: era Any() — lançava quando tinha equipado. Deve lançar quando NÃO tem.
                    if (!session.Inventory.UserEquippedItem.CharacterEquiped.parts_typeid.Any(c => c == _typeid))
                    {
                        throw new exception("[TourneyBase::RequestActiveRingMiracleSignJP][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou ativar 'Anel'[TYPEID=" + Convert.ToString(_typeid) + "] Olho Magico JP, mas ele nao esta com a Part equipada. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY_BASE,
                            74, 0x350005));
                    }

                } // else Item Passive

                // Effect
                SetEffectActiveInShot(session, enumToBitValue(AbilityEffect.MIRACLE_SIGN_RANDOM));

                // Resposta para o Active Ring Miracle Sign JP
                p.init_plain(0x280);

                p.WriteUInt32(0); // OK;

                p.WriteUInt32(_typeid);
                p.WriteUInt32(session.UserInfo.UID);
                session.Send(p);
            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[TourneyBase::RequestActiveRingMiracleSign][ErroSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Resposta Error
                p.init_plain(0x280);

                p.WriteUInt32((ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) == STDA_ERROR_TYPE.TOURNEY_BASE) ? ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) : 0x350000);
                session.Send(p);
            }
        }

        public override void RequestActiveWing(Player session, Packet packet)
        { 
            var p = new Packet();

            try
            {
                uint _typeid = packet.ReadUInt32();

                if (_typeid == 0)
                {
                    throw new exception("[TourneyBase::ActiveWing][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou ativar Asa[TYPEID=" + Convert.ToString(_typeid) + "], mas o _typeid é invalido(zero)", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY_BASE,
                        90, 0x360001));
                }

                var pWi = session.Inventory.FindWarehouseItemByTypeid(_typeid);

                if (pWi == null)
                {
                    throw new exception("[TourneyBase::ActiveWing][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou ativar Asa[TYPEID=" + Convert.ToString(_typeid) + "], mas ele nao tem esse item 'Asa', Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY_BASE,
                        91, 0x360002));
                }

                if (session.Inventory.UserEquippedItem.CharacterEquiped == null)
                {
                    throw new exception("[TourneyBase::ActiveWing][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou ativar Asa[TYPEID=" + Convert.ToString(_typeid) + "], mas ele nao esta com um Character equipado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY_BASE,
                        92, 0x360003));
                }

                if (!session.Inventory.UserEquippedItem.CharacterEquiped.parts_typeid.Any(c => c == _typeid))//tinha colocado verdadeiro antes, mas era false
                {
                    throw new exception("[TourneyBase::ActiveWing][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou ativar Asa[TYPEID=" + Convert.ToString(_typeid) + "], mas ele nao esta com o item 'Asa' equipado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY_BASE,
                        93, 0x360004));
                }

                // Adiciona o efeito que foi ativado
                CheckEffectItemAndSet(session, _typeid);

                // Resposta para o Active Wing
                p.init_plain(0x203);

                p.WriteUInt32(session.UserInfo.UID);

                p.WriteUInt32(_typeid);
                session.Send(p);
            }
            catch (exception e)
            { 
                _smp.LogManager.Instance.push(new AppMessage("[TourneyBase::ActiveWing][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void RequestActivePaws(Player session, Packet packet)
        { 
            var p = new Packet();

            try
            {

                // Efeito patinha não passa o TYPEID do item que ativou, Animal Ring(Anel) ou Patinha
                SetEffectActiveInShot(session, enumToBitValue(AbilityEffect.PAWS_NOT_ACCUMULATE));

                // Resposta para o Active Paws
                p.init_plain(0x236);

                p.WriteUInt32(session.UserInfo.UID);
                session.Send(p);
            }
            catch (exception e)
            { 
                _smp.LogManager.Instance.push(new AppMessage("[TourneyBase::RequestActivePaws][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void RequestActiveGlove(Player session, Packet packet)
        { 
            var p = new Packet();

            try
            {

                uint _typeid = packet.ReadUInt32();

                if (_typeid == 0)
                {
                    throw new exception("[TourneyBase::RequestActiveGlove][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou ativar Luva[TYPEID=" + Convert.ToString(_typeid) + "], mas o _typeid é invalido(zero). Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY_BASE,
                        110, 0x370001));
                }

                var pWi = session.Inventory.FindWarehouseItemByTypeid(_typeid);

                if (pWi == null)
                {
                    throw new exception("[TourneyBase::RequestActiveGlove][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou ativar Luva[TYPEID=" + Convert.ToString(_typeid) + "], mas ele nao tem esse item 'Luva'. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY_BASE,
                        111, 0x370002));
                }

                if (session.Inventory.UserEquippedItem.CharacterEquiped == null)
                {
                    throw new exception("[TourneyBase::RequestActiveGlove][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou ativar Luva[TYPEID=" + Convert.ToString(_typeid) + "], mas ele nao esta com um Character equipado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY_BASE,
                        112, 0x370003));
                }

                if (sIff.Instance.getItemGroupIdentify(_typeid) == IFF_GROUP.PART)
                { // Luva

                    if (!session.Inventory.UserEquippedItem.CharacterEquiped.parts_typeid.Any(c => c == _typeid))
                    {
                        throw new exception("[TourneyBase::RequestActiveGlove][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou ativar Luva[TYPEID=" + Convert.ToString(_typeid) + "], mas ele nao tem a Luva equipada. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY_BASE,
                            113, 0x370004));
                    }

                }
                else if (sIff.Instance.getItemGroupIdentify(_typeid) == IFF_GROUP.AUX_PART)
                { // Anel
                    if (!session.Inventory.UserEquippedItem.CharacterEquiped.auxparts.Any(c => c == _typeid))
                    {
                        throw new exception("[TourneyBase::RequestActiveGlove][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou ativar Luva[TYPEID=" + Convert.ToString(_typeid) + "], mas ele nao tem o Anel equipado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY_BASE,
                            114, 0x370005));
                    }
                }

                // Adiciona o efeito que foi ativado
                CheckEffectItemAndSet(session, _typeid);

                // Resposta para o Active Glove
                p.init_plain(0x265);

                p.WriteUInt32(0); // OK

                p.WriteUInt32(_typeid);

                p.WriteUInt32(session.UserInfo.UID);
                session.Send(p);
            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[TourneyBase::RequestActiveGlove][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Resposta Error
                p.init_plain(0x265);

                p.WriteUInt32((ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) == STDA_ERROR_TYPE.TOURNEY_BASE) ? ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) : 0x370000);
                session.Send(p);
            }
        }

        public override void RequestActiveEarcuff(Player session, Packet packet)
        { 
            var p = new Packet();

            try
            {

                stEarcuff ec = new stEarcuff();
                ec._typeid = packet.ReadUInt32();
                ec.angle = packet.ReadByte();
                ec.x_point_angle = packet.ReadFloat();

                if (ec._typeid == 0)
                {
                    throw new exception("[TourneyBase::ActiveEarcuff][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou ativar Earcuff'Mascot'[TYPEID=" + Convert.ToString(ec._typeid) + ", ANGLE_SENTIDO=" + Convert.ToString((ushort)ec.angle) + ", X_ANGLE=" + Convert.ToString(ec.x_point_angle) + "], mas o _typeid é invalido. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY_BASE,
                        130, 0x380001));
                }

                if (sIff.Instance.getItemGroupIdentify(ec._typeid) == IFF_GROUP.PART)
                { // Earcuff

                    if (session.Inventory.UserEquippedItem.CharacterEquiped == null)
                    {
                        throw new exception("[TourneyBase::ActiveEarcuff][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou ativar Earcuff[TYPEID=" + Convert.ToString(ec._typeid) + ", ANGLE_SENTIDO=" + Convert.ToString((ushort)ec.angle) + ", X_ANGLE=" + Convert.ToString(ec.x_point_angle) + "], mas ele nao esta com um Character equipado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY_BASE,
                            131, 0x380002));
                    }

                    var pWi = session.Inventory.FindWarehouseItemByTypeid(ec._typeid);

                    if (pWi == null)
                    {
                        throw new exception("[TourneyBase::ActiveEarcuff][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou ativar Earcuff[TYPEID=" + Convert.ToString(ec._typeid) + ", ANGLE_SENTIDO=" + Convert.ToString((ushort)ec.angle) + ", X_ANGLE=" + Convert.ToString(ec.x_point_angle) + "], mas ele nao tem o Part. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY_BASE,
                            132, 0x380003));
                    }
                    if (!session.Inventory.UserEquippedItem.CharacterEquiped.parts_typeid.Any(c => c == ec._typeid))
                    {
                        throw new exception("[TourneyBase::ActiveEarcuff][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou ativar Earcuff[TYPEID=" + Convert.ToString(ec._typeid) + ", ANGLE_SENTIDO=" + Convert.ToString((ushort)ec.angle) + ", X_ANGLE=" + Convert.ToString(ec.x_point_angle) + "], mas ele nao esta com o Part equipado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY_BASE,
                            133, 0x380004));
                    }

                }
                else if (sIff.Instance.getItemGroupIdentify(ec._typeid) == IFF_GROUP.MASCOT)
                { // Mascot Dragon

                    var pMi = session.Inventory.FindMascotByTypeid(ec._typeid);

                    if (pMi == null)
                    {
                        throw new exception("[TourneyBase::ActiveEarcuff][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou ativar Earcuff[TYPEID=" + Convert.ToString(ec._typeid) + ", ANGLE_SENTIDO=" + Convert.ToString((ushort)ec.angle) + ", X_ANGLE=" + Convert.ToString(ec.x_point_angle) + "], mas ele nao tem esse Mascot. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY_BASE,
                            134, 0x380005));
                    }

                    if (session.Inventory.UserEquippedItem.MascotEquiped == null)
                    {
                        throw new exception("[TourneyBase::ActiveEarcuff][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou ativar Earcuff'Mascot'[TYPEID=" + Convert.ToString(ec._typeid) + ", ANGLE_SENTIDO=" + Convert.ToString((ushort)ec.angle) + ", X_ANGLE=" + Convert.ToString(ec.x_point_angle) + "], mas ele nao esta com o Mascot equipado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY_BASE,
                            135, 0x380006));
                    }
                }

                InitPlayerInfo("RequestActiveEarcuff",
                    "tentou ativar o efeito earcuff de direcao de vento",
                   session, out PlayerGameInfo pgi);

                // Effect
                SetEffectActiveInShot(session, enumToBitValue(AbilityEffect.EARCUFF_DIRECTION_WIND));


                // Resposta para o Active Earcuff
                p.init_plain(0x24C);

                p.WriteUInt32(0); // OK

                p.WriteUInt32(ec._typeid);

                p.WriteUInt32(session.UserInfo.UID);

                p.WriteByte(ec.angle);

                p.WriteFloat(ec.x_point_angle);
                session.Send(p);
            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[TourneyBase::RequestActiveEarcuff][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Resposta Error
                p.init_plain(0x24C);

                p.WriteUInt32((ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) == STDA_ERROR_TYPE.TOURNEY_BASE) ? ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) : 0x380000);
                session.Send(p);
            }
        }

        public override void RequestUpdateTrofel()
        {

            uint soma = 0;

            PlayerInfo.ToList().ForEach(el =>
            {
                if (el.Key != null)
                {
                    soma += (el.Value.level > 60) ? 60 : (uint)(el.Value.level > 0 ? el.Value.level - 1 : 0);
                }
            });

            uint new_trofel = STDA_MAKE_TROFEL(soma, PlayerInfo.Count());

            // Check se o trofeu anterior era o GM e se o novo não é mais, aí tira a type de GM da sala
            if (RoomInfo.TrophyID == TROFEL_GM_EVENT_TYPEID && new_trofel != TROFEL_GM_EVENT_TYPEID)
            {
                RoomInfo.IsGameMaster = 0;
            }

            if (new_trofel > 0 && new_trofel != RoomInfo.TrophyID)
            {
                RoomInfo.TrophyID = new_trofel;
            }
        }

        public override void RequestSendTimeGame(Player session)
        { 
            var p = new Packet();

            try
            {

                if (IsGamingBefore(session.UserInfo.UID))
                {
                    throw new exception("[TourneyBase::RequestSendTimeGame][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou entrar na sala[NUMERO=" + Convert.ToString(RoomInfo.RoomID) + "] ja em jogo, mas o player ja tinha jogado nessa sala e saiu, e nao pode mais entrar.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY_BASE,
                        2703, 6));
                }

                p = new Packet(0x113);

                p.WriteByte(3); // Remain Time of Game
                p.WriteByte(0);

                p.WriteInt16(RoomInfo.RoomID);

                // old-> var remain_time = UtilTime.GetLocalDateDiff(StartTime);
                var elapsed = (DateTime.Now - StartTime).Ticks;

                long remain_time = 0;

                if (elapsed > 0)
                    remain_time = elapsed / TimeSpan.TicksPerMillisecond; // direto em milissegundos

                p.WriteUInt32((uint)remain_time);//tempo decorrido
                p.WriteUInt32(RoomInfo.TimeMin);

                p.WriteBytes(RoomInfo.ToArray());
                session.Send(p);
            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[TourneyBase::RequestSendTimeGame][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Resposta erro
                p.init_plain(0x113);

                p.WriteByte(6); // Option Error

                // Error Code
                p.WriteByte((byte)((ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) == STDA_ERROR_TYPE.TOURNEY_BASE) ? ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) : 1));
                session.Send(p);
            }
        }
      
        public override void RequestUpdateEnterAfterStartedInfo(Player session, EnterAfterStartInfo easi)
        { 
            var p = new Packet();

            try
            {

                if (session.ConnectionID != easi.owner_oid)
                {
                    throw new exception("[TourneyBase::RequestUpdateEnterAfterStartedInfo][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou atualizar info depois de entrar na sala[NUMERO=" + Convert.ToString(RoomInfo.RoomID) + "] que ja tinha comecado, mas os OID[owner=" + Convert.ToString(session.ConnectionID) + ", owner=" + Convert.ToString(easi.owner_oid) + "] nao bate. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY_BASE,
                        2708, 1));
                }

                var s = FindSessionByOID(easi.request_oid);

                if (s == null)
                {
                    throw new exception("[TourneyBase::RequestUpdateEnterAfterStartedInfo][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou atualizar info depois de entrar na sala[NUMERO=" + Convert.ToString(RoomInfo.RoomID) + "] que ja tinha comecado, mas o Normal[OID=" + Convert.ToString(easi.request_oid) + "] nao esta no jogo. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY_BASE,
                        2709, 1));
                }

                InitPlayerInfo("RequestUpdateEnterAfterStartedInfo",
                    "tentou atualizar info depois de entar na sala[NUMERO=" + Convert.ToString(RoomInfo.RoomID) + "] no jogo",
                   session, out PlayerGameInfo pgi);

                p = new Packet(0x113);

                p.WriteByte(10); // Update Info Scores
                p.WriteByte(0);

                p.WriteInt32(session.ConnectionID);
                p.WriteByte((byte)pgi.data.total_tacada_num);
                p.WriteByte(pgi.hole);
                p.WriteInt32(pgi.data.score);
                p.WriteUInt64(pgi.data.pang);

                p.WriteBytes(easi.ToArray());

                session.Send(p);

                s.Send(p); 

            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[TourneyBase::RequestUpdateEnterAfterStartedInfo][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Resposta erro
                p.init_plain(0x113);

                p.WriteByte(6); // Option Error

                // Error Code
                p.WriteByte((byte)((ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) == STDA_ERROR_TYPE.TOURNEY_BASE) ? ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) : 1));
                session.Send(p);
            }
        }
       
        public override bool RequestFinishGame(Player session, Packet p)
        {
            bool ret = false;

            try
            {

                #region Read Packet
                var ui = new PlayerUserStatistics().ToRead(p);
                #endregion
                // aqui o cliente passa mad_conduta com o hole_in, trocados, mad_conduto <-> hole_in

                InitPlayerInfo("RequestFinishGame",
                    "tentou terminar o jogo",
                   session, out PlayerGameInfo pgi);

                pgi.ui = ui;

                // Packet06
                ret = FinishGame(session, 6);
                //ver depois
              UpdateRoomLogSql(session);

            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[TourneyBase::RequestFinishGame][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            return ret;
        }

        public virtual void GameStartTime()
        {
            try
            {
                if (Timer != null)
                    GameStop();

                Timer = GameServer.Instance.MakeTimer(RoomInfo.TimeMin, () => OnEndTime(this, null), new List<long>(), PangyaSyncTimer.TIMER_TYPE.NORMAL);
            }
            catch (Exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage(
                    $"[TourneyBase::startTime][ErrorSystem] {e.Message}",
                    type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void RequestTranslateSyncShotData(Player session, ShotSyncData ssd)
        { 
            try
            {

                var s = FindSessionByOID(ssd.oid);

                if (s == null)
                {
                    throw new exception("[TourneyBase::RequestTranslateSyncShotData][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou sincronizar tacada do Normal[OID=" + Convert.ToString(ssd.oid) + "], mas o player nao existe nessa jogo. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                        200, 0));
                }

                // Update Sync Shot Player
                if (session.UserInfo.UID == s.UserInfo.UID)
                {

                    InitPlayerInfo("RequestTranslateSyncShotData",
                        "tentou sincronizar a tacada no jogo",
                       session, out PlayerGameInfo pgi);

                    pgi.shot_sync = ssd;

                    // Last Location Player
                    var last_location = pgi.location;

                    // Update Location Player
                    pgi.location.x = ssd.location.x;
                    pgi.location.z = ssd.location.z;

                    // Update Pang and Bonus Pang
                    pgi.data.pang = ssd.pang;
                    pgi.data.bonus_pang = ssd.bonus_pang;

                    // Já só na função que come a o tempo do player do turno
                    pgi.data.tacada_num++;

                    if (ssd.state == ShotSyncData.SHOT_STATE.OUT_OF_BOUNDS || ssd.state == ShotSyncData.SHOT_STATE.UNPLAYABLE_AREA)
                    {
                        pgi.data.tacada_num++;
                    }

                    var hole = Course.findHole(pgi.hole);

                    if (hole == null)
                    {
                        throw new exception("[TourneyBase::RequestTranslateSyncShotData][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou sincronizar tacada no hole[NUMERO=" + Convert.ToString((ushort)pgi.hole) + "], mas o RoomID do hole is invalid. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                            12, 0));
                    }

                    // Conta já a próxima tacada, no give up
                    if (!(ssd.state_shot.display.acerto_hole) && hole.getPar().total_shot <= (pgi.data.tacada_num + 1))
                    {

                        // +1 que é giveup, só add se n o passou o número de tacadas
                        if (pgi.data.tacada_num < hole.getPar().total_shot)
                        {
                            pgi.data.tacada_num++;
                        }

                        pgi.data.giveup = 1;

                        // Soma +1 no Bad Condute
                        pgi.data.bad_condute++;
                    }

                    // aqui os achievement de power shot int32_t putt beam impact e etc
                    UpdateSyncShotAchievement(session, last_location);
                }

            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[TourneyBase::RequestTranslateSyncShotData][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void RequestReplySyncShotData(Player session)
        { 
            try
            {

                DrawDropItem(session);

                // Resposta Sync Shot
               SendSyncShot(session);

            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[TourneyBase::RequestReplySyncShotData][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public virtual void SendRemainTime(Player session)
        {
            var elapsed = (DateTime.Now - StartTime).Ticks;

            long remain_time = 0;

            if (elapsed > 0)
                remain_time = elapsed / TimeSpan.TicksPerMillisecond; // direto em milissegundos

            var p = new Packet(0x8D);
            p.WriteUInt32((uint)remain_time);

            session.Send(p);
        }

        //send packet 6D
        public virtual void UpdateFinishHole(Player session, int option)
        {

            InitPlayerInfo("updateFinishHole",
                "tentou terminar o hole no jogo",
               session, out PlayerGameInfo pgi);

            var p = new Packet(0x6D);

            p.WriteInt32(session.ConnectionID);//4
            p.WriteByte(pgi.hole);//1
            p.Write((byte)pgi.data.total_tacada_num);//1
            p.WriteInt32(pgi.data.score);//4
            p.WriteUInt64(pgi.data.pang);//8-18
            p.WriteUInt64(pgi.data.bonus_pang);//8
            p.WriteByte((byte)option); // 1-27 Terminou o Hole, 0 - N o terminou o Hole
            SendBroadCast(p);
        }

        public void UpdateTreasureHunterPoint(Player session)
        {

            InitPlayerInfo("updateTreasureHunterPoint",
                "tentou atualizar os pontos do Treasure Hunter no jogo",
               session, out PlayerGameInfo pgi);


            if (!sTreasureHunterSystem.Instance.isLoad())
            {
                sTreasureHunterSystem.Instance.load();
            }

            var hole = Course.findHole(pgi.hole);

            if (hole == null)
            {
                throw new exception("[TourneyBase::updateTreasureHunterPoint][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou atualizar os pontos do Treasure Hunter no hole[NUMERO=" + Convert.ToString((ushort)pgi.hole) + "], mas o hole nao existe. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY_BASE,
                    30, 0));
            }

            // Calcule Treasure Pontos
            if (RoomInfo.GetRoomType() == RoomTypeFlags.SPECIAL_SHUFFLE_COURSE)
            {

                pgi.thi.treasure_point += (uint)(sTreasureHunterSystem.Instance.calcPointSSC(pgi.data.tacada_num, hole.getPar().par) + pgi.thi.getPoint(pgi.data.tacada_num, hole.getPar().par));
            }
            else
            {
                pgi.thi.treasure_point += (uint)(sTreasureHunterSystem.Instance.calcPointNormal(pgi.data.tacada_num, hole.getPar().par) + pgi.thi.getPoint(pgi.data.tacada_num, hole.getPar().par));
            }

            // Mostra score board
            var p = new Packet(0x132);

            p.WriteUInt32(pgi.thi.treasure_point);

            // No Modo Match passa outro valor tbm
            session.Send(p);
        }

        public virtual void RequestDrawTreasureHunterItem(Player session)
        {

            // Sorteia os itens ganho do Treasure ponto do player

            if (!sTreasureHunterSystem.Instance.isLoad())
            {
                sTreasureHunterSystem.Instance.load();
            }

            InitPlayerInfo("RequestDrawTreasureHunterItem",
                "tentou sortear os item(ns) do Treasure Hunter do jogo",
               session, out PlayerGameInfo pgi);

            // Guarda os item(ns) ganho no Treasure hunter system, no Player Game Info, para poder consultar ele depois

            if (!sTreasureHunterSystem.Instance.isLoad())
                sTreasureHunterSystem.Instance.load();

            var v_item = sTreasureHunterSystem.Instance.drawItem(pgi.thi.treasure_point, (byte)((int)RoomInfo.CourseIndex & 0x7F));

            if (!v_item.Any())
            {
                _smp.LogManager.Instance.push(new AppMessage(
                    "[TourneyBase::RequestDrawTreasureHunterItem][Warning] Nenhum item sorteado pelo sistema de Treasure Hunter.",
                    type_msg.CL_FILE_LOG_AND_CONSOLE));
                return;
            }




            if (PlayerOrder == null || PlayerOrder.Count == 0)
                return;

            int idx = 0;
            foreach (var item in v_item)
            {
                var player = PlayerOrder[idx % PlayerOrder.Count];
                // Treasure Hunter Item Player
                player.thi.v_item.Add(item);

                idx++;
            }
        }

        public virtual void SendSyncShot(Player session)
        {

            InitPlayerInfo("sendSyncShot",
                "tentou sincronizar a tacada do jogador no jogo",
               session, out PlayerGameInfo pgi);

            var p = new Packet(0x6E); 
            p.WriteInt32(pgi.shot_sync.oid); 
            p.WriteByte(pgi.hole); 
            p.WriteFloat(pgi.location.x);
            p.WriteFloat(pgi.location.z); 
            p.WriteUInt32(pgi.shot_sync.state_shot.shot.ulState);  
            p.WriteInt16(pgi.shot_sync.tempo_shot);
            SendBroadCast(p);
        }

        public void SendEndShot(Player session, DropItemRet cube)
        {

            var p = new Packet(0xCC);

            p.WriteInt32(session.ConnectionID);

            // Count, Coin/Cube "Drop"
            p.WriteByte(cube.v_drop.Count());

            if (cube.v_drop.Any())
            {
                foreach (var el in cube.v_drop)
                {
                    p.WriteBytes(el.ToArray());
                }

                // Aqui o server passa 128 itens de drop, os que dropou e o resto vazio
                if (cube.v_drop.Count() < 128)
                {
                    p.WriteZero((128 - cube.v_drop.Count()) * 16);
                }
            }
            session.Send(p);
        }

        public void SendUpdateState(Player session, int option)
        {

            var p = new Packet(0x6C);

            p.WriteInt32(session.ConnectionID);

            p.WriteByte((byte)option); // 2 Terminou, 3 Saiu

            SendBroadCast(p); 
        }

        public void SendDropItem(Player session)
        {

            InitPlayerInfo("sendDropItem",
                "tentou enviar os itens dropado do player no jogo",
               session, out PlayerGameInfo pgi);

            var p = new Packet(0xCE);

            p.WriteByte(0); // OK

            p.WriteUInt16((ushort)pgi.drop_list.v_drop.Count());

            foreach (var el in pgi.drop_list.v_drop)
            {
                p.WriteUInt32(el._typeid);
            }
            session.Send(p);
        }
        public virtual void SendPlacar(Player session)
        {

            InitPlayerInfo("sendPlacar",
                "tentou enviar o placar do jogo",
               session, out PlayerGameInfo pgi);

            var p = new Packet(0x79);

            p.WriteInt32(pgi.data.exp);

            p.WriteUInt32(RoomInfo.TrophyID);

            p.WriteByte(pgi.trofel); // Trofel Que o Player Ganhou
            p.WriteByte((byte)pgi.team); // Team Win, 0 - vermelho, 1 - Azul, 2 nenhum

            // Medalhas 
            for (var i = 0; i < (Medals.Length); ++i)
            {
                p.WriteBytes(Medals[i].ToArray());
            }

            // N o sei se   a geral ou se   s  a do Tourney, (DEIXEI A GERAL) todas as medalhas que ele tem
            p.WriteBytes(session.UserInfo.Statistics.medal.ToArray());
            session.Send(p);
        }

        public void SendTreasureHunterItemDrawGUI(Player session)
        {

            InitPlayerInfo("sendTreasureHunterItemDrawGUI",
                "tentou enviar os itens ganho no Treasure Hunter(so o Visual) do jogo",
               session, out PlayerGameInfo pgi);

            var p = new Packet(0x133);

            p.WriteByte((byte)pgi.thi.v_item.Count());

            // No VS aqui os itens s o dividido entres os players do versus
            foreach (var el in pgi.thi.v_item)
            {
                p.WriteUInt32(pgi.uid); // UID do player que ganhou o item
                p.WriteUInt32(el._typeid);
                p.WriteUInt16((ushort)el.qntd);
                p.WriteByte(0); // Acho que sej  op  o ou dizendo que acabou o struct de Treasure Hunter Item Draw
            }
            session.Send(p);
        }

        public void SendTimeIsOver(Player session)
        {

            var p = new Packet(0x8C);
            session.Send(p);
        }

        public virtual int CheckEndShotOfHole(Player session)
        {

            // Agora verifica o se ele acabou o hole e essas coisas
            InitPlayerInfo("checkEndShotOfHole",
                "tentou verificar a ultima tacada do hole no jogo",
               session, out PlayerGameInfo pgi);

            if (pgi.shot_sync.state_shot.display.acerto_hole || pgi.data.giveup == 1)
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
                    var p = new Packet(0x199);
                    session.Send(p);

                    // Fez o Ultimo Hole, Calcula Clear Bonus para o player
                    if (pgi.shot_sync.state_shot.display.clear_bonus)
                    {

                        if (!MapSystem.Instance.isLoad())
                        {
                            MapSystem.Instance.load();
                        }

                        var map = MapSystem.Instance.getMap((byte)((int)RoomInfo.CourseIndex & 0x7F));

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

        public virtual void DrawDropItem(Player session)
        {

            InitPlayerInfo("drawDropItem",
                "tentou sortear item drop para o jogador no jogo",
               session, out PlayerGameInfo pgi);

            if (pgi.shot_sync.state_shot.display.acerto_hole)
            {
                var drop = RequestInitDrop(session);

                if (drop.v_drop.Any())
                {
                    var p = new Packet(0xCC);

                    p.WriteInt32(session.ConnectionID);

                    // Count, Coin/Cube "Drop"
                    p.WriteByte((byte)drop.v_drop.Count());

                    if (drop.v_drop.Any())
                    {
                        foreach (var el in drop.v_drop)
                        {
                            p.WriteBytes(el.ToArray());
                        }

                        // Aqui o server passa 128 itens de drop, os que dropou e o resto vazio
                        if (drop.v_drop.Count() < 128)
                        {
                            p.WriteZero((128 - drop.v_drop.Count()) * 16);
                        }
                    }
                    session.Send(p);
                }
            }
        }

        public void AchievementTop3_1st(Player session)
        { 
            var rank = GetRankPlace(session);

            if (rank != -1)
            {

                if (rank < 3)
                {

                    InitPlayerInfo("achievement_top_3_1st",
                        "tentou atualizar achievement contador de top 3 RankPosition do player no jogo",
                       session, out PlayerGameInfo pgi);

                    pgi.sys_achieve.incrementCounter(0x6C4000B6u);

                    if (rank == 0u)
                    {
                        pgi.sys_achieve.incrementCounter(0x6C4000AFu);
                    }
                }
            }
        }

        public void CalculeShotToSpinningCube(Player session, ShotSyncData ssd)
        { 
            try
            {

                InitPlayerInfo("calcule_shot_to_spinning_cube",
                    "tentou calcular a tacada para o spinning cube",
                   session, out PlayerGameInfo pgi);

                var hole = Course.findHole(pgi.hole);

                if (hole == null)
                {
                    return;
                }

                if (ssd.state != ShotSyncData.SHOT_STATE.PLAYABLE_AREA && ssd.state != ShotSyncData.SHOT_STATE.INTO_HOLE)
                {
                    return; // Sai
                }

                // Bogey+ ou errou pangya ou bunker não calcula
                if (pgi.data.tacada_num > hole.getPar().par || pgi.shot_data.acerto_pangya_flag != 4 || ssd.bunker_flag != 0)
                {
                    return; // Sai, tacada bogey não calcula spinning cube
                }

                // Calcule Shot Cube
                sCoinCubeLocationUpdateSystem.Instance.pushOrderToCalcule(new CalculeCoinCubeUpdateOrder(CalculeCoinCubeUpdateOrder.eTYPE.CUBE, session.UserInfo.UID, pgi.location, hole.getPinLocation(), pgi.shot_data_for_cube, RoomInfo.GetMap(), (byte)(RoomInfo.HoleMode == (byte)RoomHoleType.M_REPEAT ? hole.getHoleRepeat() : hole.GetRoomId())));
            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[TourneyBase::calcule_shot_to_spinning_cube][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void CalculeShotToCoin(Player session, ShotSyncData ssd)
        { 
            try
            {

                const float MIN_DISTANCE_TO_HOLE_TO_SPAWN_COIN = 70.0f * SCALE_PANGYA; // 70y

                InitPlayerInfo("calcule_shot_to_coin",
                    "tentou verificar a tacada para a coin",
                   session, out PlayerGameInfo pgi);

                var hole = Course.findHole(pgi.hole);

                if (hole == null)
                {
                    return;
                }

                if (ssd.state != ShotSyncData.SHOT_STATE.PLAYABLE_AREA && ssd.state != ShotSyncData.SHOT_STATE.INTO_HOLE)
                {
                    return; // Sai
                }

                // Bogey+ ou errou pangya ou bunker não calcula
                if (pgi.data.tacada_num > hole.getPar().par || pgi.shot_data.acerto_pangya_flag != 4 || ssd.bunker_flag != 0)
                {
                    return; // Sai, tacada bogey não calcula coin
                }

                if (Math.Abs(hole.getPinLocation().diffXZ(ssd.location)) <= MIN_DISTANCE_TO_HOLE_TO_SPAWN_COIN)
                {
                    return; // Sai, muito perto do hole para spawnar uma coin
                }

                // Calcule Shot Coin
                sCoinCubeLocationUpdateSystem.Instance.pushOrderToCalcule(new CalculeCoinCubeUpdateOrder(CalculeCoinCubeUpdateOrder.eTYPE.COIN, session.UserInfo.UID, ssd.location, hole.getPinLocation(), pgi.shot_data_for_cube, RoomInfo.GetMap(), (byte)(RoomInfo.GetHoleType() == RoomHoleType.M_REPEAT ? hole.getHoleRepeat() : hole.GetRoomId())));

            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[TourneyBase::calcule_shot_to_coin][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }
        
        public virtual void RequestCalculeShotSpinningCube(Player session, ShotSyncData ssd)
        { 
        }

        public virtual void RequestCalculeShotCoin(Player session, ShotSyncData ssd)
        { 
        }

        public override void RequestExecCCGChangeWeather(Player session, Packet packet)
        {
            try
            {

                var weather = packet.ReadByte();

                // Log
                _smp.LogManager.Instance.push(new AppMessage("[TourneyBase::RequestExecCCGChangeWeather][Log] [GM] Normal[UID=" + (session.UserInfo.UID) + "] trocou o tempo(weather) da sala[NUMERO="
                         + (RoomInfo.RoomID) + ", WEATHER=" + (weather) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                // UPDATE ON GAME
                Packet p = new Packet(0x9E);

                p.WriteUInt16(weather);
                p.WriteByte(1); // Acho que seja type, não sei, vou deixar 1 por ser o GM que mudou
                SendBroadCast(p);
            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[TourneyBase::RequestExecCCGChangeWeather][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                throw;
            }
        }

        public virtual int OnEndTime(object arg1, object arg2)
        {
            // Cast seguro com 'as' — evita InvalidCastException se o callback
            // for disparado com um objeto de Type inesperado.
            var game = arg1 as TourneyBase;

            if (game == null)
            {
                _smp.LogManager.Instance.push(new AppMessage("[TourneyBase::OnEndTime][Error] arg1 não é uma instância de TourneyBase.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                return -1;
            }

            try
            {
                game.GameTimeIsOver();
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[TourneyBase::OnEndTime][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            return 0;
        }


        public override void Dispose(bool disposing)
        {
            if (Disposed) return; // Evita executar duas vezes

            if (disposing)
            {
                // Para o timer do jogo ao descartar
                GameStop();
            }

            base.Dispose(disposing);
        }
    }
}
