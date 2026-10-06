using Pangya_GameServer.Feature;
using Pangya_GameServer.Manager;
using Pangya_GameServer.Models.Game;
using Pangya_GameServer.Server;
using Pangya_GameServer.Session;
using PangyaAPI.IFF.Flags;
using PangyaAPI.IFF.Handle.JP;
using PangyaAPI.IFF.Regions.JP.Models.IFF;
using System.Diagnostics;
using static Pangya_GameServer.Models.DefineConstants;
using static PangyaAPI.Utilities.Tools;
namespace Pangya_GameServer.Roms.GameBase.Modes
{
    /// <summary>
    /// Base class for Stroke-based game modes (Versus, Match, etc).
    /// Manages turn rotation, Stroke synchronization, and player StateRoom during State gameplay.
    /// </summary>
    public abstract class StrokeBase : Game
    {
        #region Fields

        private PangyaThread _checkTurnThread;
        private IntPtr _checkTurnEvent;
        private IntPtr _checkTurnPulseEvent;

        private readonly int _maxPlayers = 4;
        public PlayerGameInfo PlayerTurn { get; set; }
        private int _pauseCount;// Quantidade de vezes que o player pode pausar o tempo da tacada, para evitar que ele pause toda hora e nunca acabar a tacada
        private readonly uint _mascotEffectSeed;
        public uint NextStepGameFlag { get; set; }

        private TreasureHunterVersusInfo _treasureHunterInfo;
        public stStateVersus GameStateVersus { get; set; }

        private bool _initStrokeBaseState;
        #endregion

        #region Constructor

        public StrokeBase(List<Player> players, GameRoomInfoModel roomInfo, RateValue rateValue) 
            : base(players, roomInfo, rateValue)
        {
            PlayerTurn = new PlayerGameInfo();
            NextStepGameFlag = 0;
            _treasureHunterInfo = new TreasureHunterVersusInfo();
            _pauseCount = 0;//limite  é 3
            GameStateVersus = new stStateVersus();
            _mascotEffectSeed = (uint)(Random.Shared.Next()) & 0xFFFF;// Initialize mascot effect seed

            if (CheckLimitPlayers())
            {
                throw new exception("[StrokeBase::Constructor][Error] Maximum players exceeded");
            }

            // Create check turn event
            if ((_checkTurnEvent = CreateEvent(IntPtr.Zero, true, false, null)) == IntPtr.Zero)
            {
                throw new exception(
                    "[StrokeBase::Constructor][Error] Failed to create check turn event.",
                    ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE, 1050, GetLastError()));
            }

            // Create check turn pulse event
            if ((_checkTurnPulseEvent = CreateEvent(IntPtr.Zero, false, false, null)) == IntPtr.Zero)
            {
                throw new exception(
                    "[StrokeBase::Constructor][Error] Failed to create check turn pulse event.",
                    ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE, 1050, GetLastError()));
            }

            _checkTurnThread = new PangyaThread(1050, obj => SyncGameTurn(), this, ThreadPriority.AboveNormal);
        }

        #endregion

        #region Request Handlers - Hole Operations

        public override void RequestInitHole(Player session, Packet packet)
        {
            try
            {

                #region Read Packet
                stInitHole ctx_hole = new stInitHole().ToRead(packet);
                #endregion
                var hole = Course.findHole(ctx_hole.numero);

                hole.init(ctx_hole.tee, ctx_hole.pin);

                InitPlayerInfo("RequestInitHole",
                    "tentou inicializar o hole[NUMERO = " + Convert.ToString(hole.GetRoomId()) + "] no jogo",
                    session, out PlayerGameInfo pgi);

                // Update Location Player in Hole
                pgi.location.x = ctx_hole.tee.x;
                pgi.location.z = ctx_hole.tee.z;

                // Número do hole atual, que o player está jogando
                pgi.hole = ctx_hole.numero;

                // ServerFlag que marca se o player já inicializou o primeiro hole do jogo
                if (!pgi.init_first_hole)
                {
                    pgi.init_first_hole = true;
                }

                // Gera degree para o player ou pega o degree sem gerar que é do HoleMode do hole repeat
                pgi.degree = (RoomInfo.HoleMode == 4) ? hole.getWind().degree.getDegree() : hole.getWind().degree.getShuffleDegree();
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[StrokeBase::RequestInitHole][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override bool RequestFinishLoadHole(Player session, Packet packet)
        {

            // Esse aqui é para Trocar Info da Sala
            // para colocar a sala no HoleMode que pode entrar depois de ter começado
            bool ret = false;

            try
            {

                GameStateVersus.SetStateWithLock(STATE_VERSUS.LOAD_HOLE);

                InitPlayerInfo("RequestFinishLoadHole",
                    "tentou finalizar carregamento do hole no jogo",
                    session, out PlayerGameInfo pgi);

                SetLoadHole(pgi);


            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[StrokeBase::RequestFinishLoadHole][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            return ret;
        }

        public override void RequestFinishCharIntro(Player session, Packet packet)
        {

            try
            {

                InitPlayerInfo("RequestFinishCharIntro",
                    "tentou finalizar intro do char no jogo",
                    session, out PlayerGameInfo pgi);

                // Zera todas as tacada num dos players
                pgi.data.tacada_num = 0;

                // Giveup ServerFlag
                pgi.data.giveup = 0;

                if (SetFinishCharIntroAndCheckAllFinishCharIntroAndClear(pgi))
                {
                    SendReplyFinishCharIntro();
                }

            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[StrokeBase::RequestFinishCharIntro][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void RequestFinishHoleData(Player session, Packet packet)
        {

            try
            {

                PlayerUserStatistics ui = new PlayerUserStatistics();
                #region Read Packet
                ui.ToRead(packet);
                #endregion

                RequestTranslateFinishHoleData(session, ui);

            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[StrokeBase::RequestFinishHoleData][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }


        public override void RequestInitShotSended(Player session, Packet packet)
        {

            try
            {

                InitPlayerInfo("RequestInitShotSended",
                    "player recebeu o pacote de InitShot",
                    session, out PlayerGameInfo pgi);


                // Player recebeu o pacote55, agora o checkStrokeTurn pode verifica se ele enviou o pacote no tempo
                pgi.tick_sync_shot.Start();

            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[StrokeBase::RequestInitShotSended][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void RequestInitShot(Player session, Packet packet)
        {

            var p = new Packet();

            try
            {

                InitPlayerInfo("RequestInitShot", "tentou iniciar tacada no jogo", session, out PlayerGameInfo pgi);



                if (pgi.init_shot == 1)
                {
                    _smp.LogManager.Instance.push(new AppMessage("[StrokeBase::RequestInitShot][Log] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] o server ja recebeu o pacote12 Init Shot. ignora esse.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                    return;
                }
                else
                {
                    pgi.init_shot = 1;
                }

                // Stop time turn
                pgi.bar_space.setState(0); // Volta para 1 depois que taca, era esse meu comentário no antigo

                // para o tempo da tacada ele acabou de tacar
                GameStop();//cada jogador deve terminar o time...
                //pois o time, é diferente para cada jogador....

                pgi.tempo = 0; // ReSeta o tempo
                               // end

                ShotDataEx sd = new();
                #region Read Shot Sync Data
                sd.ToReadEx(packet); 
                #endregion
                pgi.shot_data = sd;

                GameStateVersus.SetStateWithLock(STATE_VERSUS.SHOTING);

                // Aqui não manda resposta no TourneyBase ou Practice, mas outro modos(VS, MATCH) manda e outros também não(TOURNEY)
                p.init_plain(0x55); 
                p.WriteInt32(session.ConnectionID);
                //talvez problema aqui
                p.WriteBytes(sd.ToArrayEx());//pela analize é 62

                SendBroadCast(p);

            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[StrokeBase::RequestInitShot][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void RequestSyncShot(Player session, Packet packet)
        {

            try
            {

                InitPlayerInfo("RequestSyncShot",
                    "tentou sincronizar a tacada no jogo",
                    session, out PlayerGameInfo pgi); 

                if (pgi.sync_shot_flag2 == 1)
                {

                    _smp.LogManager.Instance.push(new AppMessage("[StrokeBase::RequestSyncShot][Log] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] packet no read 0x1B.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                    PlayerTurn = pgi;
                    return;

                }
                else
                {
                    pgi.sync_shot_flag2 = 1;
                }

                ShotSyncData ssd = new ShotSyncData();

                base.RequestReadSyncShotData(session, packet, ref ssd);

                RequestTranslateSyncShotData(session, ssd);

                RequestReplySyncShotData(session);
            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[StrokeBase::RequestSyncShot][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void RequestInitShotArrowSeq(Player session, Packet packet)
        { 
            try
            {

                byte count_Seta = packet.ReadByte();

                if (count_Seta == 0)
                {
                    throw new exception("[StrokeBase::RequestInitShotArrowSeq][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou inicializar as sequencia de Setas, mas nao enviou nenhuma Seta. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                        5, 0));
                }

                List<uArrow> Setas = new List<uArrow>();

                for (var i = 0; i < count_Seta; ++i)
                {
                    Setas.Add(new uArrow(packet.ReadUInt32()));
                }

            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[StrokeBase::RequestInitShotArrowSeq][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }


        public override void RequestShotEndData(Player session, Packet packet)
        { 
            var p = new Packet();

            try
            {

                // ----------------- LEMBRETE --------------
                // Aqui vou usar para as tacadas do spinning cube que gera no CourseIndex 
                ShotEndLocationData seld = new ShotEndLocationData(packet);

                if (PlayerTurn == null)
                {
                    throw new exception("[StrokeBase::RequestShotEndData][Error] PlayerTurn is invalid(null)", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                        1500, 0));
                }

                if (PlayerTurn.uid == session.UserInfo.UID)
                {
                    PlayerTurn.shot_data_for_cube = seld;
                }

                // Resposta para Shot End Data
                p.init_plain(0x1F7);  
                p.WriteInt32(PlayerTurn.oid);
                p.WriteByte(PlayerTurn.hole);

                p.WriteBytes(seld.ToArray());

                SendBroadCast(p);

            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[StrokeBase::RequestShotEndData][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override RetFinishShot RequestFinishShot(Player session, Packet packet)
        { 
            var p = new Packet();

            RetFinishShot ret = new RetFinishShot();

            try
            {

                InitPlayerInfo("RequestFinishShot",
                    "tentou sincronizar o termino da tacada no jogo",
                    session, out PlayerGameInfo pgi);

                // Essa parte tem que vir antes, mas estou fazendo teste de dc 

                if (pgi.finish_shot2 == 1)
                {
                    _smp.LogManager.Instance.push(new AppMessage("[StrokeBase::RequestFinishShot][Log] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] o server ja recebeu o pacote1C sync end shot do player. ignora esse.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                    return ret;

                }
                else
                {
                    pgi.finish_shot2 = 1;
                }

                // Recebeu o primeiro finish shot libera a contagem, para verifica se o cliente recebeu a resposta em menos de 10 segundos
                // só entra se o timer do player no tick_sync_end_shot não estiver ativado, por que quando ativa 1 ativa todos
                if (!pgi.tick_sync_end_shot.active)
                {

                    Players.ForEach(el =>
                    {
                        try
                        {
                            InitPlayerInfo("RequestFinishShot",
                                " tentou ativar all tick sync end shot do jogo",
                                el, out pgi);

                            pgi.tick_sync_end_shot.active = true;
                        }
                        catch (exception e)
                        {
                            _smp.LogManager.Instance.push(new AppMessage("[StrokeBase::RequestFinishShot][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                        }
                    });
                }



                // Request Init Cube Coin
                var cube = RequestInitCubeCoin(session, packet);

                // Resposta para Finish Shot
                SendEndShot(session, cube);

                ret.ret = CheckEndShotOfHole(session);

                if (ret.ret == 2)
                {
                    ret.p = FindSessionByPlayerGameInfo(PlayerTurn);
                }
            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[StrokeBase::RequestFinishShot][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            return ret;
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

                // _smp.LogManager.Instance.push(new AppMessage("[StrokeBase::RequestChangeMira][Log] Normal[UID=" + Convert.ToString(session.PlayerUserStatistics.UID) + "] mira[VALUE=" + Convert.ToString(mira) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                pgi.location.r = mira;

                // Resposta para o Change mira
                p.init_plain(0x56);

                p.WriteInt32(pgi.oid);
                p.WriteFloat(pgi.location.r);

                SendBroadCast(p);

            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[StrokeBase::RequestChangeMira][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void RequestChangeStateBarSpace(Player session, Packet packet)
        { 
            var p = new Packet();

            try
            {

                byte state = packet.ReadByte();
                float point = packet.ReadFloat();

                InitPlayerInfo("RequestChangeStateBarSpace",
                    "tentou mudar o estado[STATE=" + Convert.ToString((ushort)state) + ", POINT=" + Convert.ToString(point) + "] da barra de espaco no jogo",
                    session, out PlayerGameInfo pgi);

                if (!pgi.bar_space.setStateAndPoint(state, point))
                {
                    throw new exception("[StrokeBase::RequestChangeStateBarSpace][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou mudar o estado da barra de espaco[STATE=" + Convert.ToString((ushort)state) + ", POINT=" + Convert.ToString(point) + "] no jogo, mas o estado eh desconhecido, Hacker ou Bug. packet: " + packet.Log(), ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                        5, 0));
                }

                if (state == 0 && pgi.tempo == 1)
                {

                    try
                    {

                        pgi.tempo = 0;

                        if (++pgi.data.time_out == 3)
                        {

                            var hole = Course.findHole(pgi.hole) ?? throw new exception("[StrokeBase::RequestChangeStateBarSpace][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou mudar o estado da barra de espaco[STATE=" + Convert.ToString((ushort)state) + ", POINT=" + Convert.ToString(point) + "] no jogo, mas tentou encontrar o hole[HOLE=" + Convert.ToString((short)pgi.hole) + "] no CourseIndex mas, nao encontrou. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                                    100, 0));
                            pgi.shot_sync.state_shot.display.acerto_hole = true;

                            pgi.data.tacada_num = hole.getPar().total_shot;

                            // Derruba player, tem que fazer isso no Channel ou na sala, com o retorno dessa função
                            pgi.data.bad_condute = 3;
                        }

                    }
                    catch (exception e)
                    {

                        _smp.LogManager.Instance.push(new AppMessage("[StrokeBase::RequestChangeStateBarSpace][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                    }
                    // Time Out
                    p.init_plain(0x5C);

                    p.WriteInt32(session.ConnectionID);

                    SendBroadCast(p);
                }


            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[StrokeBase::RequestChangeStateBarSpace][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
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

                // Resposta para Active Power Shot
                p.init_plain(0x58);

                p.WriteInt32(session.ConnectionID);
                p.WriteByte(pgi.power_shot);

                SendBroadCast(p);

            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[StrokeBase::RequestActivePowerShot][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
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

                pgi.club = club;

                // Resposta para Change Club
                p.init_plain(0x59);

                p.WriteInt32(session.ConnectionID);
                p.WriteByte(pgi.club);

                SendBroadCast(p);

            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[StrokeBase::RequestChangeClub][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
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
                    throw new exception("[StrokeBase::RequestActiveItem][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou usar State item[TYPEID=" + Convert.ToString(item_typeid) + "] no jogo, mas o item__typeid eh invalido(zero). Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                        7, 0));
                }

                var iffItem = sIff.Instance.findCommomItem(item_typeid) ?? throw new exception("[StrokeBase::RequestActiveItem][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + " tentou usar State item[TYPEID=" + Convert.ToString(item_typeid) + "] no jogo, mas o item nao tem no IFF_STRUCT. Hacker ou Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                        77, 0));


                if (sIff.Instance.getItemGroupIdentify(item_typeid) != IFF_GROUP.ITEM || !sIff.Instance.IsItemEquipable(item_typeid))
                {
                    throw new exception("[StrokeBase::RequestActiveItem][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou usar State item[TYPEID=" + Convert.ToString(item_typeid) + "] no jogo, mas o item nao eh equipavel(usar). Hacker ou Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                        78, 0));
                }

                if (item_typeid == MULLIGAN_ROSE_TYPEID)
                {
                    throw new exception("[StrokeBase::RequestActiveItem][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou usar State item[TYPEID=" + Convert.ToString(item_typeid) + "] no jogo, mas o item Mulligan Rose nao pode usar no StrokeBase, so em TourneyBase. Hacker ou Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                        79, 0));
                }

                var pWi = session.Inventory.FindWarehouseItemByTypeid(item_typeid);

                if (pWi == null)
                {
                    throw new exception("[StrokeBase::RequestActiveItem][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou usar State item[TYPEID=" + Convert.ToString(item_typeid) + "] no jogo, mas ele nao tem esse item. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                        8, 0));
                }

                var it = pgi.used_item.v_active.FirstOrDefault(c => c.Key == pWi._typeid);

                if (it.Key <= 0)
                {
                    throw new exception("[StrokeBase::RequestActiveItem][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou usar State item[TYPEID=" + Convert.ToString(item_typeid) + "] no jogo, mas ele nao equipou esse item. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                        9, 0));
                }

                if (it.Value.count >= it.Value.v_slot.Count)
                {
                    throw new exception("[StrokeBase::RequestActiveItem][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou usar State item[TYPEID=" + Convert.ToString(item_typeid) + "] no jogo, mas ele ja usou todos os item desse que ele equipou. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
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

                _smp.LogManager.Instance.push(new AppMessage("[StrokeBase::RequestUseActiveItem][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
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

                SendBroadCast(p);

            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[StrokeBase::RequestChangeStateTypeing][Error] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
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

                // para o tempo do da tacada do player, que ele vai recolocar e come a um novo tempo depois
                GameStop();

                // Resposta para Move Ball
                p.init_plain(0x60);

                p.WriteFloat(pgi.location.x);
                p.WriteFloat(pgi.location.y);
                p.WriteFloat(pgi.location.z);

                SendBroadCast(p);

            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[StrokeBase::RequestMoveBall][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
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

                SendBroadCast(p);

            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[StrokeBase::RequestChangeStateChatBlock][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
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

                if (!session.UserInfo.UserCapabilities.UserPremium)
                { // (não é)!PREMIUM USER

                    var pWi = session.Inventory.FindWarehouseItemByTypeid(TIME_BOOSTER_TYPEID);

                    if (pWi == null)
                    {
                        throw new exception("[StrokeBase::RequestActiveBooster][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou ativar time booster, mas ele nao tem o item passive. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                            11, 0));
                    }

                    if (pWi.STDA_C_ITEM_QNTD <= 0)
                    {
                        throw new exception("[StrokeBase::RequestActiveBooster][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou ativar time booster, mas ele nao tem quantidade suficiente[VALUE=" + Convert.ToString(pWi.STDA_C_ITEM_QNTD) + ", Request=1] do item de time booster.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                            12, 0));
                    }

                    var it = pgi.used_item.v_passive.FirstOrDefault(c=> c.Key == pWi._typeid);

                    if (it.Value == null)
                    {
                        throw new exception("[StrokeBase::RequestActiveBooster][Error] Normal[UID = " + Convert.ToString(session.UserInfo.UID) + "] tentou ativar time booster, mas ele nao tem ele no item passive usados do server. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                            13, 0));
                    }

                    if ((short)it.Value.count >= pWi.STDA_C_ITEM_QNTD)
                    {
                        throw new exception("[StrokeBase::RequestActiveBooster][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou ativar time booster, mas ele ja usou todos os time booster. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                            14, 0));
                    }

                    // Add +1 ao item passive usado
                    it.Value.count++;

                }
                else
                { // Soma +1 no contador de counter item do booster do player e passive item

                    pgi.sys_achieve.incrementCounter(0x6C400075u);

                    pgi.sys_achieve.incrementCounter(0x6C400050);
                }

                // Resposta para Active Booster
                p.init_plain(0xC7);

                p.WriteFloat(velocidade);
                p.WriteInt32(session.ConnectionID);

                SendBroadCast(p);

            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[StrokeBase::RequestActiveBooster][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
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
                    throw new exception("[StrokeBase::RequestActiveReplay][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou ativar Replay[TYPEID=" + Convert.ToString(_typeid) + "], mas o _typeid eh invalido(zero). Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                        200, 0));
                }

                var pWi = session.Inventory.FindWarehouseItemByTypeid(_typeid);

                if (pWi == null)
                {
                    throw new exception("[StrokeBase::RequestActiveReplay][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou ativar Replay[TYPEID=" + Convert.ToString(_typeid) + "], mas ele nao tem o item. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                        201, 0));
                }

                if (pWi.STDA_C_ITEM_QNTD <= 0)
                {
                    throw new exception("[StrokeBase::RequestActiveReplay][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou ativar Replay[TYPEID=" + Convert.ToString(_typeid) + "], mas ele nao tem quantidade suficiente[VALUE=" + Convert.ToString(pWi.STDA_C_ITEM_QNTD) + ", Request=1] do item. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                        202, 0));
                }

                // UPDATE ON SERVER AND DB
                stItem item = new stItem
                {
                    type = 2,
                    _typeid = pWi._typeid,
                    id = pWi.id,
                    qntd = 1
                };
                item.STDA_C_ITEM_QNTD = (short)(item.qntd * -1);

                if (ItemManager.removeItem(item, session) <= 0)
                {
                    throw new exception("[StrokeBase::RequestActiveReplay][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou ativar Replay[TYPEID=" + Convert.ToString(_typeid) + "], nao conseguiu deletar ou atualizar qntd do item[TYPEID=" + Convert.ToString(item._typeid) + ", ID=" + Convert.ToString(item.id) + "]", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                        203, 0));
                }


                // UPDATE ON GAME
                // Resposta para o Active Replay
                p.init_plain(0xA4);

                p.WriteUInt16((ushort)item.stat.qntd_dep);

                SendBroadCast(p);

            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[StrokeBase::RequestActiveReplay][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
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
                    throw new exception("[StrokeBase::RequestActiveCutin][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou activar cutin[CHAR_TYPEID=" + Convert.ToString(ac.char_typeid) + ", TIPO=" + Convert.ToString(ac.tipo) + ", OPT=" + Convert.ToString(ac.opt) + ",  ACTIVE=" + Convert.ToString(ac.active) + "] de um Normal[UID=" + Convert.ToString(ac.uid) + "], mas o jogador nao esta no jogo. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                        1, 0x5200101));
                }

                if (s.Inventory.UserEquippedItem.CharacterEquiped == null)
                {
                    throw new exception("[StrokeBase::RequestActiveCutin][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou activar cutin[CHAR_TYPEID=" + Convert.ToString(ac.char_typeid) + ", TIPO=" + Convert.ToString(ac.tipo) + ", OPT=" + Convert.ToString(ac.opt) + ",  ACTIVE=" + Convert.ToString(ac.active) + "] de um Normal[UID=" + Convert.ToString(ac.uid) + "], mas o jogador nao tem um character equipado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                        2, 0x5200102));
                }

                CutinInformation pCutin = null;

                // Cutin Padrão que o player equipa, quando o cliente envia o cutin type é que é efeito por roupas equipadas
                if (sIff.Instance.getItemGroupIdentify(ac.char_typeid) == IFF_GROUP.CHARACTER && ac.active == 1)
                {

                    if (s.Inventory.UserEquippedItem.CharacterEquiped._typeid != ac.char_typeid)
                    {
                        throw new exception("[StrokeBase::RequestActiveCutin][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou activar cutin[CHAR_TYPEID=" + Convert.ToString(ac.char_typeid) + ", TIPO=" + Convert.ToString(ac.tipo) + ", OPT=" + Convert.ToString(ac.opt) + ",  ACTIVE=" + Convert.ToString(ac.active) + "] de um Normal[UID=" + Convert.ToString(ac.uid) + "], mas o character _typeid passado nao eh igual ao equipado do player. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
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

                                if ((pCutin = sIff.Instance.findCutinInfomation(pWi._typeid)) == null || pCutin._typeid == 0)
                                {
                                    throw new exception("[StrokeBase::RequestActiveCutin][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou activar cutin[CHAR_TYPEID=" + Convert.ToString(ac.char_typeid) + ", TIPO=" + Convert.ToString(ac.tipo) + ", OPT=" + Convert.ToString(ac.opt) + ", ACTIVE=" + Convert.ToString(ac.active) + "] de um Normal[UID=" + Convert.ToString(ac.uid) + "], mas o jogador nao tem esse cutin[TYPEID=" + Convert.ToString(pWi._typeid) + ", ID=" + Convert.ToString(pWi.id) + "]. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                                        3, 0x5200103));
                                }

                                if (pCutin.tipo.ulCondition == ac.tipo)
                                {
                                    break;
                                }
                                else if ((i + 1) == end)
                                {
                                    throw new exception("[StrokeBase::RequestActiveCutin][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou activar cutin[CHAR_TYPEID=" + Convert.ToString(ac.char_typeid) + ", TIPO=" + Convert.ToString(ac.tipo) + ", OPT=" + Convert.ToString(ac.opt) + ",  ACTIVE=" + Convert.ToString(ac.active) + "] de um Normal[UID=" + Convert.ToString(ac.uid) + "], mas o jogador nao tem esse cutin[TYPEID=" + Convert.ToString(pWi._typeid) + ", ID=" + Convert.ToString(pWi.id) + "]. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                                        3, 0x5200103));
                                }
                            }
                        }
                    }

                }
                else if (sIff.Instance.getItemGroupIdentify(ac.char_typeid) == IFF_GROUP.SKIN && ac.active == 0)
                {
                    if ((pCutin = sIff.Instance.findCutinInfomation(ac.char_typeid)) == null)
                    {
                        throw new exception("[StrokeBase::RequestActiveCutin][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou activar cutin[CHAR_TYPEID=" + Convert.ToString(ac.char_typeid) + ", TIPO=" + Convert.ToString(ac.tipo) + ", OPT=" + Convert.ToString(ac.opt) + ",  ACTIVE=" + Convert.ToString(ac.active) + "] de um Normal[UID=" + Convert.ToString(ac.uid) + "], mas o jogador nao tem esse cutin[TYPEID=" + Convert.ToString(ac.char_typeid) + "]. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                            3, 0x5200103));
                    }
                }

                if (pCutin == null || pCutin._typeid == 0)
                {
                    throw new exception("[StrokeBase::RequestActiveCutin][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou activar cutin[CHAR_TYPEID=" + Convert.ToString(ac.char_typeid) + ", TIPO=" + Convert.ToString(ac.tipo) + ", OPT=" + Convert.ToString(ac.opt) + ",  ACTIVE=" + Convert.ToString(ac.active) + "] de um Normal[UID=" + Convert.ToString(ac.uid) + "], mas o cution nao foi encontrado[TYPEID=" + Convert.ToString(ac.char_typeid) + "]. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY_BASE,
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

                SendBroadCast(p);

                // No Modo GrandZodic, não envia Cutin, então envia o pacote18D com option 0(Byte), e valor 3(UInt16)

            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[StrokeBase::RequestActiveCutin][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

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
                    throw new exception("[StrokeBase::RequestActiveRing][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou ativar Anel[TYPEID=" + Convert.ToString(r._typeid) + "], mas o _typeid eh invalido(zero). Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                        30, 0x330001));
                }

                var pWi = session.Inventory.FindWarehouseItemByTypeid(r._typeid);

                if (pWi == null)
                {
                    throw new exception("[StrokeBase::RequestActiveRing][Error] Normal[UID = " + Convert.ToString(session.UserInfo.UID) + "] tentou ativar Anel[TYPEID = " + Convert.ToString(r._typeid) + "], mas ele nao tem o anel. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                        31, 0x330002));
                }

                if (session.Inventory.UserEquippedItem.CharacterEquiped == null)
                {
                    throw new exception("[StrokeBase::RequestActiveRing][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou ativar Anel[TYPEID=" + Convert.ToString(r._typeid) + "], mas ele nao esta com um Character equipado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                        32, 0x330003));
                }

                if (!session.Inventory.UserEquippedItem.CharacterEquiped.auxparts.Any(c => c == r._typeid))
                {
                    throw new exception("[StrokeBase::RequestActiveRing][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou ativar Anel[TYPEID=" + Convert.ToString(r._typeid) + "], mas ele nao esta equipado com o anel. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
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

                SendBroadCast(p);

            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[StrokeBase::RequestActiveRing][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Resposta Error
                p.init_plain(0x237);

                p.WriteUInt32((ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) == (uint)STDA_ERROR_TYPE.VERSUS_BASE) ? ExceptionError.STDA_SYSTEM_ERROR_DECODE(e.getCodeError()) : 0x330000);

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
                if (rg.efeito == AbilityEffect.UNKNOWN_31)//efeito 31, e o taco
                {
                    _smp.LogManager.Instance.push(new AppMessage("[StrokeBase::RequestActiveRingGround][Log] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] ativou o efeito 0x1F(31) com os itens[TYPEID_1=" + Convert.ToString(rg.ring[0]) + ", TYPEID_2=" + Convert.ToString(rg.ring[1]) + "] e OPTION=" + Convert.ToString(rg.option), type_msg.CL_FILE_LOG_AND_CONSOLE));
                }

                if (!rg.isValid())
                {
                    throw new exception("[StrokeBase::RequestActiveRingGround][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou ativar Anel de Terreno[TYPE=" + Convert.ToString(rg.efeito) + ", RING[0]=" + Convert.ToString(rg.ring[0]) + ", RING[1]=" + Convert.ToString(rg.ring[1]) + ", OPTION=" + Convert.ToString(rg.option) + "], mas os _typeid's nao sao validos. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY_BASE,
                        50, 0x340001));
                }

                if (session.Inventory.UserEquippedItem.CharacterEquiped == null)
                {
                    throw new exception("[StrokeBase::RequestActiveRingGround][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou ativar Anel de Terreno[TYPE=" + Convert.ToString(rg.efeito) + ", RING[0]=" + Convert.ToString(rg.ring[0]) + ", RING[1]=" + Convert.ToString(rg.ring[1]) + ", OPTION=" + Convert.ToString(rg.option) + "], mas ele nao esta com um Character equipado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY_BASE,
                        51, 0x340002));
                }

                if (sIff.Instance.getItemGroupIdentify(rg.ring[0]) == IFF_GROUP.AUX_PART)
                { // Anel

                    var pRing = session.Inventory.FindWarehouseItemByTypeid(rg.ring[0]);

                    if (pRing == null)
                    {
                        throw new exception("[StrokeBase::RequestActiveRingGround][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou ativar Anel de Terreno[TYPE=" + Convert.ToString(rg.efeito) + ", RING[0]=" + Convert.ToString(rg.ring[0]) + ", RING[1]=" + Convert.ToString(rg.ring[1]) + ", OPTION=" + Convert.ToString(rg.option) + "], mas ele nao tem o Anel[0]. hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY_BASE,
                            52, 0x340002));
                    }

                    if (!session.Inventory.UserEquippedItem.CharacterEquiped.auxparts.Any(c => c == rg.ring[0]))
                    {
                        throw new exception("[StrokeBase::RequestActiveRingGround][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou ativar Anel de Terreno[TYPE=" + Convert.ToString(rg.efeito) + ", RING[0]=" + Convert.ToString(rg.ring[0]) + ", RING[1]=" + Convert.ToString(rg.ring[1]) + ", OPTION=" + Convert.ToString(rg.option) + "], mas ele nao esta com o Anel[0] equipado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY_BASE,
                            53, 0x340003));
                    }

                    if (rg.ring[0] != rg.ring[1])
                    { // Ativou Habilidade em conjunto 2 aneis

                        var pRing2 = session.Inventory.FindWarehouseItemByTypeid(rg.ring[1]);

                        if (pRing2 == null)
                        {
                            throw new exception("[StrokeBase::RequestActiveRingGround][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou ativar Anel de Terreno[TYPE=" + Convert.ToString(rg.efeito) + ", RING[0]=" + Convert.ToString(rg.ring[0]) + ", RING[1]=" + Convert.ToString(rg.ring[1]) + ", OPTION=" + Convert.ToString(rg.option) + "], mas ele nao tem o Anel[1]. hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY_BASE,
                                52, 0x340002));
                        }

                        if (!session.Inventory.UserEquippedItem.CharacterEquiped.auxparts.Any(c => c == rg.ring[1]))
                        {
                            throw new exception("[StrokeBase::RequestActiveRingGround][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou ativar Anel de Terreno[TYPE=" + Convert.ToString(rg.efeito) + ", RING[0]=" + Convert.ToString(rg.ring[0]) + ", RING[1]=" + Convert.ToString(rg.ring[1]) + ", OPTION=" + Convert.ToString(rg.option) + "], mas ele nao esta com o Anel[1] equipado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY_BASE,
                                53, 0x340003));
                        }
                    }

                }
                else if (sIff.Instance.getItemGroupIdentify(rg.ring[0]) == IFF_GROUP.PART)
                { // Part

                    var pRing = session.Inventory.FindWarehouseItemByTypeid(rg.ring[0]);

                    if (pRing == null)
                    {
                        throw new exception("[StrokeBase::RequestActiveRingGround][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou ativar Anel de Terreno[TYPE=" + Convert.ToString(rg.efeito) + ", RING[0]=" + Convert.ToString(rg.ring[0]) + ", RING[1]=" + Convert.ToString(rg.ring[1]) + ", OPTION=" + Convert.ToString(rg.option) + "], mas ele nao tem o Part[0]. hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY_BASE,
                            52, 0x340002));
                    }
                    //etava como aux ring
                    if (!session.Inventory.UserEquippedItem.CharacterEquiped.parts_typeid.Any(c => c == rg.ring[0]))
                    {
                        throw new exception("[StrokeBase::RequestActiveRingGround][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou ativar Anel de Terreno[TYPE=" + Convert.ToString(rg.efeito) + ", RING[0]=" + Convert.ToString(rg.ring[0]) + ", RING[1]=" + Convert.ToString(rg.ring[1]) + ", OPTION=" + Convert.ToString(rg.option) + "], mas ele nao esta com o Part[0] equipado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY_BASE,
                            53, 0x340003));
                    }

                    if (rg.ring[0] != rg.ring[1])
                    { // Ativou Habilidade em conjunto 2 aneis

                        var pRing2 = session.Inventory.FindWarehouseItemByTypeid(rg.ring[1]);

                        if (pRing2 == null)
                        {
                            throw new exception("[StrokeBase::RequestActiveRingGround][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou ativar Anel de Terreno[TYPE=" + Convert.ToString(rg.efeito) + ", RING[0]=" + Convert.ToString(rg.ring[0]) + ", RING[1]=" + Convert.ToString(rg.ring[1]) + ", OPTION=" + Convert.ToString(rg.option) + "], mas ele nao tem o Part[1]. hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY_BASE,
                                52, 0x340002));
                        }

                        if (!session.Inventory.UserEquippedItem.CharacterEquiped.auxparts.Any(c => c == rg.ring[1]))
                        {
                            throw new exception("[StrokeBase::RequestActiveRingGround][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou ativar Anel de Terreno[TYPE=" + Convert.ToString(rg.efeito) + ", RING[0]=" + Convert.ToString(rg.ring[0]) + ", RING[1]=" + Convert.ToString(rg.ring[1]) + ", OPTION=" + Convert.ToString(rg.option) + "], mas ele nao esta com o Part[1] equipado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY_BASE,
                                53, 0x340003));
                        }
                    }

                }
                else if (sIff.Instance.getItemGroupIdentify(rg.ring[0]) == IFF_GROUP.MASCOT)
                {

                    var pMascot = session.Inventory.FindMascotByTypeid(rg.ring[0]);

                    if (pMascot == null)
                    {
                        throw new exception("[StrokeBase::RequestActiveRingGround][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou ativar Anel de Terreno[TYPE=" + Convert.ToString(rg.efeito) + ", RING[0]=" + Convert.ToString(rg.ring[0]) + ", RING[1]=" + Convert.ToString(rg.ring[1]) + ", OPTION=" + Convert.ToString(rg.option) + "], mas ele nao tem o Mascot[0]. hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY_BASE,
                            52, 0x340002));
                    }

                    if (rg.ring[0] != rg.ring[1])
                    { // Ativou Habilidade em conjunto 2 aneis

                        var pPart2 = session.Inventory.FindWarehouseItemByTypeid(rg.ring[1]);

                        if (pPart2 == null)
                        {
                            throw new exception("[StrokeBase::RequestActiveRingGround][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou ativar Anel de Terreno[TYPE=" + Convert.ToString(rg.efeito) + ", RING[0]=" + Convert.ToString(rg.ring[0]) + ", RING[1]=" + Convert.ToString(rg.ring[1]) + ", OPTION=" + Convert.ToString(rg.option) + "], mas ele nao tem o Part[1]. hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY_BASE,
                                52, 0x340002));
                        }

                        if (!session.Inventory.UserEquippedItem.CharacterEquiped.parts_typeid.Any(c => c == rg.ring[1]))
                        {
                            throw new exception("[StrokeBase::RequestActiveRingGround][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou ativar Anel de Terreno[TYPE=" + Convert.ToString(rg.efeito) + ", RING[0]=" + Convert.ToString(rg.ring[0]) + ", RING[1]=" + Convert.ToString(rg.ring[1]) + ", OPTION=" + Convert.ToString(rg.option) + "], mas ele nao esta com o Part[1] equipado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY_BASE,
                                53, 0x340003));
                        }
                    }
                }

                // Adiciona o efeito que foi ativado 
                SetEffectActiveInShot(session, enumToBitValue(rg.efeito));

                // Resposta para o Active Ring Terreno
                p.init_plain(0x266);

                p.WriteUInt32(0); // OK

                p.WriteBytes(rg.ToArray());

                p.WriteUInt32(session.UserInfo.UID);

                session.Send(p);

            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[StrokeBase::RequestActiveRingGround][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

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

                SendBroadCast(p);

            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[StrokeBase::RequestActiveRingPawsRainbowJP][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
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

                SendBroadCast(p);

            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[StrokeBase::RequestActiveRingPawsRingSetJP][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void RequestActiveRingPowerGagueJP(Player session, Packet packet)
        { 
            var p = new Packet();

            try
            {

                stRingPowerGagueJP rpg = new stRingPowerGagueJP();
                rpg.efeito = packet.ReadUInt32();
                rpg.ring[0] = packet.ReadUInt32();
                rpg.ring[1] = packet.ReadUInt32();
                rpg.option = packet.ReadUInt32();

                if (!rpg.isValid())
                {
                    throw new exception("[StrokeBase::RequestActiveRingPowerGagueJP][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou ativar Anel de Barra de PS [JP] [TYPE=" + Convert.ToString(rpg.efeito) + ", RING[0]=" + Convert.ToString(rpg.ring[0]) + ", RING[1]=" + Convert.ToString(rpg.ring[1]) + ", OPTION=" + Convert.ToString(rpg.option) + "], mas os _typeid's nao sao validos. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                        150, 0x390001));
                }

                if (session.Inventory.UserEquippedItem.CharacterEquiped == null)
                {
                    throw new exception("[StrokeBase::RequestActiveRingPowerGagueJP][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou ativar Anel de Barra de PS [JP] [TYPE=" + Convert.ToString(rpg.efeito) + ", RING[0]=" + Convert.ToString(rpg.ring[0]) + ", RING[1]=" + Convert.ToString(rpg.ring[1]) + ", OPTION=" + Convert.ToString(rpg.option) + "], mas ele nao esta com um Character equipado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                        151, 0x390002));
                }

                var pRing = session.Inventory.FindWarehouseItemByTypeid(rpg.ring[0]);

                if (pRing == null)
                {
                    throw new exception("[StrokeBase::RequestActiveRingPowerGagueJP][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou ativar Anel de Barra de PS [JP] [TYPE=" + Convert.ToString(rpg.efeito) + ", RING[0]=" + Convert.ToString(rpg.ring[0]) + ", RING[1]=" + Convert.ToString(rpg.ring[1]) + ", OPTION=" + Convert.ToString(rpg.option) + "], mas ele nao tem o Anel[0]. hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                        152, 0x390002));
                }

                if (!session.Inventory.UserEquippedItem.CharacterEquiped.auxparts.Any(c => c == rpg.ring[0]))
                {
                    throw new exception("[StrokeBase::RequestActiveRingPowerGagueJP][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou ativar Anel de Barra de PS [JP] [TYPE=" + Convert.ToString(rpg.efeito) + ", RING[0]=" + Convert.ToString(rpg.ring[0]) + ", RING[1]=" + Convert.ToString(rpg.ring[1]) + ", OPTION=" + Convert.ToString(rpg.option) + "], mas ele nao esta com o Anel[0] equipado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                        153, 0x390003));
                }

                if (rpg.ring[0] != rpg.ring[1])
                { // Ativou Habilidade em conjunto 2 aneis

                    var pRing2 = session.Inventory.FindWarehouseItemByTypeid(rpg.ring[1]);

                    if (pRing2 == null)
                    {
                        throw new exception("[StrokeBase::RequestActiveRingPowerGagueJP][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou ativar Anel de Barra de PS [JP] [TYPE=" + Convert.ToString(rpg.efeito) + ", RING[0]=" + Convert.ToString(rpg.ring[0]) + ", RING[1]=" + Convert.ToString(rpg.ring[1]) + ", OPTION=" + Convert.ToString(rpg.option) + "], mas ele nao tem o Anel[1]. hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                            152, 0x390002));
                    }

                    if (!session.Inventory.UserEquippedItem.CharacterEquiped.auxparts.Any(c => c == rpg.ring[1]))

                    {
                        throw new exception("[StrokeBase::RequestActiveRingPowerGagueJP][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou ativar Anel de Barra de PS [JP] [TYPE=" + Convert.ToString(rpg.efeito) + ", RING[0]=" + Convert.ToString(rpg.ring[0]) + ", RING[1]=" + Convert.ToString(rpg.ring[1]) + ", OPTION=" + Convert.ToString(rpg.option) + "], mas ele nao esta com o Anel[1] equipado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                            153, 0x390003));
                    }
                }

                // Effect
                SetEffectActiveInShot(session, enumToBitValue(AbilityEffect.POWER_GAUGE_FREE));

                // Resposta para o Active Ring Power Gague JP
                p.init_plain(0x27F);

                p.WriteUInt32(session.UserInfo.UID);

                SendBroadCast(p);

            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[StrokeBase::RequestActiveRingPowerGagueJP][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
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
                    throw new exception("[StrokeBase::RequestActiveRingMiracleSignJP][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou ativar 'Anel'[TYPEID=" + Convert.ToString(_typeid) + "] Olho Magico JP, mas o _typeid eh invalido(zero)", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                        70, 0x350001));
                }

                WarehouseItemEx pWi = session.Inventory.FindWarehouseItemByTypeid(_typeid);

                if (pWi == null)
                {
                    throw new exception("[StrokeBase::RequestActiveRingMiracleSignJP][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou ativar 'Anel'[TYPEID=" + Convert.ToString(_typeid) + "] Olho Magico JP, mas ele nao tem o 'Anel'. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                        71, 0x350002));
                }

                if (session.Inventory.UserEquippedItem.CharacterEquiped == null)
                {
                    throw new exception("[StrokeBase::RequestActiveRingMiracleSignJP][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou ativar 'Anel'[TYPEID=" + Convert.ToString(_typeid) + "] Olho Magico JP, mas ele nao esta com um Character equipado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                        72, 0x350003));
                }

                if (sIff.Instance.getItemGroupIdentify(_typeid) == IFF_GROUP.AUX_PART)
                { // Anel

                    if (!session.Inventory.UserEquippedItem.CharacterEquiped.auxparts.Any(c => c ==
                        _typeid))
                    {
                        throw new exception("[StrokeBase::RequestActiveRingMiracleSignJP][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou ativar 'Anel'[TYPEID=" + Convert.ToString(_typeid) + "] Olho Magico JP, mas ele nao esta com o Anel equipado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                            0x73, 0x350004));
                    }

                }
                else if (sIff.Instance.getItemGroupIdentify(_typeid) == IFF_GROUP.PART)
                { // Part

                    if (!session.Inventory.UserEquippedItem.CharacterEquiped.parts_typeid.Any(c => c ==
                        _typeid))
                    {
                        throw new exception("[StrokeBase::RequestActiveRingMiracleSignJP][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou ativar 'Anel'[TYPEID=" + Convert.ToString(_typeid) + "] Olho Magico JP, mas ele nao esta com a Part equipado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                            74, 0x350005));
                    }

                } // else Item Passive, o item do assist, mas acho que ele n o chame esse, ele chama o proprio pacote dele

                // Effect
                SetEffectActiveInShot(session, enumToBitValue(AbilityEffect.MIRACLE_SIGN_RANDOM));

                // Resposta para o Active Ring Miracle Sign JP
                p.init_plain(0x280);

                p.WriteUInt32(0); // OK;

                p.WriteUInt32(_typeid);
                p.WriteUInt32(session.UserInfo.UID);

                SendBroadCast(p);

            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[StrokeBase::RequestActiveRingMiracleSign][ErroSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Resposta Error
                p.init_plain(0x280);

                p.WriteUInt32((ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) == (uint)STDA_ERROR_TYPE.VERSUS_BASE) ? ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) : 0x350000);

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
                    throw new exception("[StrokeBase::ActiveWing][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou ativar Asa[TYPEID=" + Convert.ToString(_typeid) + "], mas o _typeid eh invalido(zero)", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                        90, 0x360001));
                }

                var pWi = session.Inventory.FindWarehouseItemByTypeid(_typeid);

                if (pWi == null)
                {
                    throw new exception("[StrokeBase::ActiveWing][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou ativar Asa[TYPEID=" + Convert.ToString(_typeid) + "], mas ele nao tem esse item 'Asa', Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                        91, 0x360002));
                }

                if (session.Inventory.UserEquippedItem.CharacterEquiped == null)
                {
                    throw new exception("[StrokeBase::ActiveWing][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou ativar Asa[TYPEID=" + Convert.ToString(_typeid) + "], mas ele nao esta com um Character equipado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                        92, 0x360003));
                }

                if (!session.Inventory.UserEquippedItem.CharacterEquiped.parts_typeid.Any(c => c == _typeid))
                {
                    throw new exception("[StrokeBase::ActiveWing][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou ativar Asa[TYPEID=" + Convert.ToString(_typeid) + "], mas ele nao esta com o item 'Asa' equipado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                        93, 0x360004));
                }

                // Adiciona o efeito que foi ativado
                CheckEffectItemAndSet(session, _typeid);

                // Resposta para o Active Wing
                p.init_plain(0x203);

                p.WriteUInt32(session.UserInfo.UID);

                p.WriteUInt32(_typeid);

                SendBroadCast(p);

            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[StrokeBase::ActiveWing][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
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

                SendBroadCast(p);

            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[StrokeBase::RequestActivePaws][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
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
                    throw new exception("[StrokeBase::RequestActiveGlove][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou ativar Luva[TYPEID=" + Convert.ToString(_typeid) + "], mas o _typeid eh invalido(zero). Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                        110, 0x370001));
                }

                var pWi = session.Inventory.FindWarehouseItemByTypeid(_typeid);

                if (pWi == null)
                {
                    throw new exception("[StrokeBase::RequestActiveGlove][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou ativar Luva[TYPEID=" + Convert.ToString(_typeid) + "], mas ele nao tem esse item 'Luva'. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                        111, 0x370002));
                }

                if (session.Inventory.UserEquippedItem.CharacterEquiped == null)
                {
                    throw new exception("[StrokeBase::RequestActiveGlove][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou ativar Luva[TYPEID=" + Convert.ToString(_typeid) + "], mas ele nao esta com um Character equipado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                        112, 0x370003));
                }

                if (sIff.Instance.getItemGroupIdentify(_typeid) == IFF_GROUP.PART)
                { // Luva

                    if (!session.Inventory.UserEquippedItem.CharacterEquiped.parts_typeid.Any(c => c ==
                        _typeid))
                    {
                        throw new exception("[StrokeBase::RequestActiveGlove][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou ativar Luva[TYPEID=" + Convert.ToString(_typeid) + "], mas ele nao tem a Luva equipada. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                            113, 0x370004));
                    }

                }
                else if (sIff.Instance.getItemGroupIdentify(_typeid) == IFF_GROUP.AUX_PART)
                { // Anel

                    if (!session.Inventory.UserEquippedItem.CharacterEquiped.auxparts.Any(c => c ==
                        _typeid))
                    {
                        throw new exception("[StrokeBase::RequestActiveGlove][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou ativar Luva[TYPEID=" + Convert.ToString(_typeid) + "], mas ele nao tem o Anel equipado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
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

                SendBroadCast(p);

            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[StrokeBase::RequestActiveGlove][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Resposta Error
                p.init_plain(0x265);

                p.WriteUInt32((ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) == (uint)STDA_ERROR_TYPE.VERSUS_BASE) ? ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) : 0x370000);

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
                ec.x_point_angle = packet.ReadSingle();

                if (ec._typeid == 0)
                {
                    throw new exception("[StrokeBase::ActiveEarcuff][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou ativar Earcuff'Mascot'[TYPEID=" + Convert.ToString(ec._typeid) + ", ANGLE_SENTIDO=" + Convert.ToString((ushort)ec.angle) + ", X_ANGLE=" + Convert.ToString(ec.x_point_angle) + "], mas o _typeid eh invalido. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                        130, 0x380001));
                }

                if (sIff.Instance.getItemGroupIdentify(ec._typeid) == IFF_GROUP.PART)
                { // Earcuff

                    if (session.Inventory.UserEquippedItem.CharacterEquiped == null)
                    {
                        throw new exception("[StrokeBase::ActiveEarcuff][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou ativar Earcuff[TYPEID=" + Convert.ToString(ec._typeid) + ", ANGLE_SENTIDO=" + Convert.ToString((ushort)ec.angle) + ", X_ANGLE=" + Convert.ToString(ec.x_point_angle) + "], mas ele nao esta com um Character equipado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                            131, 0x380002));
                    }

                    var pWi = session.Inventory.FindWarehouseItemByTypeid(ec._typeid);

                    if (pWi == null)
                    {
                        throw new exception("[StrokeBase::ActiveEarcuff][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou ativar Earcuff[TYPEID=" + Convert.ToString(ec._typeid) + ", ANGLE_SENTIDO=" + Convert.ToString((ushort)ec.angle) + ", X_ANGLE=" + Convert.ToString(ec.x_point_angle) + "], mas ele nao tem o Part. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                            132, 0x380003));
                    }

                    if (!session.Inventory.UserEquippedItem.CharacterEquiped.parts_typeid.Any(c => c ==
                        ec._typeid))
                    {
                        throw new exception("[StrokeBase::ActiveEarcuff][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou ativar Earcuff[TYPEID=" + Convert.ToString(ec._typeid) + ", ANGLE_SENTIDO=" + Convert.ToString((ushort)ec.angle) + ", X_ANGLE=" + Convert.ToString(ec.x_point_angle) + "], mas ele nao esta com o Part equipado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                            133, 0x380004));
                    }

                }
                else if (sIff.Instance.getItemGroupIdentify(ec._typeid) == IFF_GROUP.MASCOT)
                { // Mascot Dragon

                    var pMi = session.Inventory.FindMascotByTypeid(ec._typeid);

                    if (pMi == null)
                    {
                        throw new exception("[StrokeBase::ActiveEarcuff][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou ativar Earcuff[TYPEID=" + Convert.ToString(ec._typeid) + ", ANGLE_SENTIDO=" + Convert.ToString((ushort)ec.angle) + ", X_ANGLE=" + Convert.ToString(ec.x_point_angle) + "], mas ele nao tem esse Mascot. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                            134, 0x380005));
                    }

                    if (session.Inventory.UserEquippedItem.MascotEquiped == null)
                    {
                        throw new exception("[StrokeBase::ActiveEarcuff][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou ativar Earcuff'Mascot'[TYPEID=" + Convert.ToString(ec._typeid) + ", ANGLE_SENTIDO=" + Convert.ToString((ushort)ec.angle) + ", X_ANGLE=" + Convert.ToString(ec.x_point_angle) + "], mas ele nao esta com o Mascot equipado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                            135, 0x380006));
                    }
                }

                InitPlayerInfo("RequestActiveEarcuff",
                    "tentou ativar o efeito earcuff de direcao de vento",
                    session, out PlayerGameInfo pgi);

                // Effect
                SetEffectActiveInShot(session, enumToBitValue(AbilityEffect.EARCUFF_DIRECTION_WIND));

                // Radianos do angulo que foi trocado a direção
                pgi.earcuff_wind_angle_shot = (float)(ec.x_point_angle < 0.0f ? (2 * PI) + ec.x_point_angle : ec.x_point_angle);

                // Resposta para o Active Earcuff
                p.init_plain(0x24C);

                p.WriteUInt32(0); // OK

                p.WriteUInt32(ec._typeid);

                p.WriteUInt32(session.UserInfo.UID);

                p.WriteByte(ec.angle);

                p.WriteFloat(ec.x_point_angle);

                SendBroadCast(p);

            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[StrokeBase::RequestActiveEarcuff][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Resposta Error
                p.init_plain(0x24C);

                p.WriteUInt32((ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) == (uint)STDA_ERROR_TYPE.VERSUS_BASE) ? ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) : 0x380000);

                session.Send(p);
            }
        }


        public override void RequestMarkerOnCourse(Player session, Packet packet)
        { 
            var p = new Packet();

            try
            {

                var moc = new stMarkerOnCourse();
                moc.x = packet.ReadSingle();
                moc.y = packet.ReadSingle();
                moc.z = packet.ReadSingle();

                // Resposta para MarkerOnCourse
                p.init_plain(0x1F8);

                p.WriteInt32(session.ConnectionID);

                p.WriteFloat(moc.x);
                p.WriteFloat(moc.y);
                p.WriteFloat(moc.z);

                SendBroadCast(p);

            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[StrokeBase::RequestMarkerOnCourse][ErrorSystem] " + e.getFullMessageError(), 0));
            }
        }

        public override void RequestLoadGamePercent(Player session, Packet packet)
        { 
            var p = new Packet();

            try
            {

                byte percent = packet.ReadByte();

                p.init_plain(0xA3);

                p.WriteInt32(session.ConnectionID);

                p.WriteByte(percent);

                SendBroadCast(p);

            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[StrokeBase::RequestLoadGamePercent][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void RequestStartTurnTime(Player session, Packet packet)
        {
            try
            {

                // Começa a contar o tempo do turno do player no Jogo
                GameTime(session);

                GameStateVersus.SetStateWithLock(STATE_VERSUS.WAIT_HIT_SHOT);

            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[StrokeBase::RequestStartTurnTime][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void RequestUnOrPause(Player session, Packet packet)
        {

            try
            {

                byte opt = packet.ReadByte();

                if (Timer == null)
                {
                    throw new exception("[StrokeBase::RequestUnOrPause][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou pausar ou despausar[OPT=" + Convert.ToString((ushort)opt) + "] um StrokeBase, que nao tem timer inicializado. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                        300, 0));
                }

                if (opt == 0)
                {
                    // Despausa 
                    if (Timer.getState() != PangyaSyncTimer.TIMER_STATE.PAUSED)
                    {
                        throw new exception("[StrokeBase::RequestUnOrPause][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou pausar ou despausar[OPT=" + Convert.ToString((ushort)opt) + ", TYPE" + Timer.getState() + "] um StrokeBase, que o timer nao esta pausado, esta em outro estado[ESTADO=" + Convert.ToString(Timer.getState()) + "]", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                            301, 0));
                    }

                    GameResume();

                    var p = new Packet(0x8B);

                    p.WriteInt32(session.ConnectionID);

                    p.WriteByte(0);

                    SendBroadCast(p);
                    _smp.LogManager.Instance.push(new AppMessage("[StrokeBase::RequestUnOrPause][Log] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] pausou o tempo na sala[NUMERO=" + Convert.ToString(RoomInfo.RoomID) + ", TYPE" + Timer.getState() + "] com sucesso!", type_msg.CL_FILE_LOG_AND_CONSOLE));

                }
                else if (opt == 1)
                {
                    // Pausa 
                    if (Timer.getState() != PangyaSyncTimer.TIMER_STATE.RUNNING)
                    {
                        throw new exception("[StrokeBase::RequestUnOrPause][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou pausar ou despausar[OPT=" + Convert.ToString((ushort)opt) + ", TYPE" + Timer.getState() + "] um StrokeBase, que o timer nao esta rodando, esta em outro estado[ESTADO=" + Convert.ToString(Timer.getState()) + "]", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                            301, 0));
                    }
                    _pauseCount++;
                    if (_pauseCount >= 3)
                    {
                        throw new exception("[StrokeBase::RequestUnOrPause][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "], tentou pausar ou despausar[OPT=" + Convert.ToString((ushort)opt) + "] um StrokeBase, mas o Stroke Base ja foi pausado 3x. Hacker ou Bug");
                    }

                    GamePause();

                    var p = new Packet(0x8B);

                    p.WriteInt32(session.ConnectionID);

                    p.WriteByte(1);

                    SendBroadCast(p);

                    // Log

                    _smp.LogManager.Instance.push(new AppMessage("[StrokeBase::RequestUnOrPause][Log] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] pausou o tempo na sala[NUMERO=" + Convert.ToString(RoomInfo.RoomID) + ", TYPE" + Timer.getState() + "] com sucesso!", type_msg.CL_FILE_LOG_AND_CONSOLE));
                    // DEBUG
                }

            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[StrokeBase::RequestUnOrPause][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void RequestReplyContinue()
        {
            try
            {

                // Troca o Turno
                ChangeTurn();

            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[StrokeBase::RequestReplyContinue][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }
 
        public override void RequestExecCCGChangeWeather(Player session, Packet packet)
        { 
            try
            {

                if (PlayerTurn == null)
                {
                    throw new exception("[StrokeBase::RequestExecCCGChangeWeather][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou executar o comando de troca de tempo(weather) no Stroke na sala[NUMERO=" + Convert.ToString(RoomInfo.RoomID) + "], mas o player_turn do Stroke eh invalido. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                        1, 0x5700100));
                }

                var hole = Course.findHole(PlayerTurn.hole);

                if (hole == null)
                {
                    throw new exception("[StrokeBase::RequestExecCCGChangeWeather][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou executar o comando de troca de tempo(weather) no Stroke na sala[NUMERO=" + Convert.ToString(RoomInfo.RoomID) + "], mas o nao encontrou o hole[VALUE=" + Convert.ToString((short)PlayerTurn.hole) + "] no CourseIndex. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                        2, 0x5700100));
                }

                var weather = packet.ReadByte();

                // Change Weather of Hole
                hole.SetWeather(weather);

                // Log
                _smp.LogManager.Instance.push(new AppMessage("[StrokeBase::RequestExecCCGChangeWeather][Log] [GM] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] trocou o tempo(weather) da sala[NUMERO=" + Convert.ToString(RoomInfo.RoomID) + ", WEATHER=" + Convert.ToString((ushort)weather) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                // UPDATE ON GAME
                var p = new Packet(0x9E);

                p.WriteUInt16(hole.getWeather());
                p.WriteByte(1); // Acho que seja ServerFlag, não sei, vou deixar 1 por ser o GM que mudou

                SendBroadCast(p);

            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[StrokeBase::RequestExecCCGChangeWeather][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                throw;
            }
        }
        public override bool RequestFinishGame(Player session, Packet packet)
        { 
            bool ret = false;

            try
            {

                PlayerUserStatistics ui = new PlayerUserStatistics();
                #region Read Packet
                ui.ToRead(packet);
                #endregion
                 
                // aqui o cliente passa mad_conduta com o hole_in, trocados, mad_conduto <-> hole_in

                InitPlayerInfo("RequestFinishGame",
                    "tentou terminar o jogo",
                    session, out PlayerGameInfo pgi);

                pgi.ui = ui;

                // Packet06
                ret = FinishGame(session, 6);

                UpdateRoomLogSql(session);
            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[StrokeBase::RequestFinishGame][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            return ret;
        }


        public override void RequestExecCCGChangeWind(Player gm, Packet packet)
        {

            try
            {
                //ta errado...
                byte wind = packet.ReadByte();
                ushort degree = packet.ReadByte(); 

                if (PlayerTurn == null)
                {
                    throw new exception("Normal[UID=" + Convert.ToString(gm.UserInfo.UID) + "] tentou executar o comando de troca de vento no Stroke na sala[NUMERO=" + Convert.ToString(RoomInfo.RoomID) + "], mas o player_turn do Stroke eh invalido. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                        1, 0x5700100));
                }

                var hole = Course.findHole(PlayerTurn.hole);

                if (hole == null)
                {
                    throw new exception("Normal[UID=" + Convert.ToString(gm.UserInfo.UID) + "] tentou executar o comando de troca de vento no Stroke na sala[NUMERO=" + Convert.ToString(RoomInfo.RoomID) + "], mas o nao encontrou o hole[VALUE=" + Convert.ToString((short)PlayerTurn.hole) + "] no CourseIndex. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                        2, 0x5700100));
                }

                var old = hole.getWind();

                // Change Wind of Hole
                old.wind = wind;

                hole.SetWind(old);

                // Change Degree of player
                PlayerTurn.degree = (ushort)(degree % LIMIT_DEGREE);

                // Log
                _smp.LogManager.Instance.push(new AppMessage("[StrokeBase::CCGChangeWind][Log] [GM] Normal[UID=" + Convert.ToString(gm.UserInfo.UID) + "] trocou o vento e graus da sala[NUMERO=" + Convert.ToString(RoomInfo.RoomID) + ", VENTO=" + Convert.ToString((ushort)wind + 1) + ", GRAUS=" + Convert.ToString(degree) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                var wind_flag = InitCardWindPlayer(PlayerTurn, hole.getWind().wind);

                // UPDATE ON GAME
                var p = new Packet(0x5B); 
                p.WriteByte(hole.getWind().wind + wind_flag); // Wind
                p.WriteByte((wind_flag < 0) ? 1 : 0); // Card Wind ServerFlag, minus wind ServerFlag
                p.WriteUInt16(PlayerTurn.degree); // Degree
                p.WriteByte(1); // ServerFlag 1 = Reset Degree, 0 = Plus Degree, , Também é ServerFlag para trocar o vento no Pang Battle se mandar o valor 0 
                SendBroadCast(p);
            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[StrokeBase::CCGChangeWind][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                throw;
            }
        }

        public override void RequestTranslateSyncShotData(Player session, ShotSyncData ssd)
        {

            try
            {

                var s = FindSessionByOID(ssd.oid);

                if (s == null)
                    throw new exception("[StrokeBase::RequestTranslateSyncShotData][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou sincronizar tacada do Normal[OID=" + Convert.ToString(ssd.oid) + "], mas o player nao existe nessa jogo. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                           200, 0));

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

                    if (ssd.state == ShotSyncData.SHOT_STATE.OUT_OF_BOUNDS || ssd.state == ShotSyncData.SHOT_STATE.UNPLAYABLE_AREA)
                        pgi.data.tacada_num++;

                    var hole = Course.findHole(pgi.hole);

                    if (hole == null)
                        throw new exception("[StrokeBase::RequestTranslateSyncShotData][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou sincronizar tacada no hole[NUMERO=" + Convert.ToString((ushort)pgi.hole) + "], mas o RoomID do hole is invalid. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                                12, 0));

                    // Conta j  a pr xima tacada, no give up
                    if (!ssd.state_shot.display.acerto_hole && hole.getPar().total_shot <= (pgi.data.tacada_num + 1))
                    {

                        // +1 que   giveup, s  add se n o passou o n mero de tacadas
                        if (pgi.data.tacada_num < hole.getPar().total_shot)
                            pgi.data.tacada_num++;

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
                _smp.LogManager.Instance.push(new AppMessage("[StrokeBase::RequestTranslateSyncShotData][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void RequestReplySyncShotData(Player session)
        {
            //CHECK_SESSION_BEGIN("RequestReplySyncShotData");

            try
            {

                InitPlayerInfo("RequestReplySyncShotData",
                    "tentou sincronizar a tacada no jogo",
                    session, out PlayerGameInfo pgi);

                SetSyncShot(pgi);
            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[StrokeBase::RequestReplySyncShotData][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public virtual void RequestTranslateFinishHoleData(Player session, PlayerUserStatistics ui)
        {
            //CHECK_SESSION_BEGIN("RequestTranslateFinishHole");

            try
            {

                InitPlayerInfo("RequestTranslateFinishHoleData",
                    "tentou finalizar hole dados no jogo",
                    session, out PlayerGameInfo pgi);

                pgi.ui = ui;

                if (!pgi.shot_sync.state_shot.display.acerto_hole)
                { // Terminou o Hole sem acerta ele, Give Up

                    var hole = Course.findHole(pgi.hole);

                    if (hole == null)
                    {
                        throw new exception("[StrokeBase::RequestFinishHoleData][Error] Normal[UID=" + Convert.ToString(pgi.uid) + "] tentou finalizar os dados do hole no jogo, mas o hole[NUMERO=" + Convert.ToString(pgi.hole) + "] nao existe no CourseIndex. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS,
                            400, 0));
                    }

                    // +1 que é giveup, só add se não passou o número de tacadas
                    if (pgi.data.tacada_num < hole.getPar().total_shot)
                    {
                        pgi.data.tacada_num++;
                    }

                    // Ainda não colocara o give up, o outro pacote, coloca nesse(muito difícil, não colocar só se estiver com bug)
                    if (!pgi.data.giveup.IsTrue())
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

                _smp.LogManager.Instance.push(new AppMessage("[StrokeBase::RequestTranslateFinishHoleData][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }


        #endregion

        #region METHODS
        public override void InitPlayerInfo(string method, string message, Player session, out PlayerGameInfo pgi)
        {
            pgi = GetPlayerInfo(session);
            if (pgi == null)
                throw new exception($"[{GetType().Name}::" + method + "][Error] Normal[UID=" + session.UserInfo.UID + "] " + message + ", mas o game nao tem o info dele guardado. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME, 1, 4));
        }

        public void DrawTreasureHunterItem()
        {
            if (!sTreasureHunterSystem.Instance.isLoad())
                sTreasureHunterSystem.Instance.load();

            var v_item = sTreasureHunterSystem.Instance.drawItem(_treasureHunterInfo.treasure_point, RoomInfo.GetMap());

            if (!v_item.Any())
            {
                _smp.LogManager.Instance.push(new AppMessage(
                    "[StrokeBase::DrawTreasureHunterItem][Warning] Nenhum item sorteado pelo sistema de Treasure Hunter.",
                    type_msg.CL_FILE_LOG_AND_CONSOLE));
                return;
            }




            if (PlayerOrder == null || PlayerOrder.Count == 0)
                return;

            int idx = 0;
            foreach (var item in v_item)
            {
                var player = PlayerOrder[idx % PlayerOrder.Count];
                // Treasure Hunter Item Stroke Base
                _treasureHunterInfo.v_item.Add(new TreasureHunterVersusInfo._stTreasureHunterItem(player.uid, item));

                // Treasure Hunter Item Player
                player.thi.v_item.Add(item);

                idx++;
            }
        }


        public int CheckEndShotOfHole(Player session)
        {

            // Agora verifica o se ele acabou o hole e essas coisas
            InitPlayerInfo("CheckEndShotOfHole",
                "tentou verificar a ultima tacada do hole no jogo",
                session, out PlayerGameInfo pgi);

            if (pgi.data.bad_condute >= 3)
            {
                return 2; // Tira da sala
            }
            else
            {
                SetFinishShot(pgi);
            }

            return 0;
        }

        public void DrawDropItem(Player session)
        {

            InitPlayerInfo("DrawDropItem",
                "tentou sortear item drop para o jogador no jogo",
                session, out PlayerGameInfo pgi);

            if (pgi.shot_sync.state_shot.display.acerto_hole)
            {
                var drop = RequestInitDrop(session);

                if (drop != null && drop.v_drop.Count > 0)
                {
                    var p = new Packet(0xCC); 
                    p.WriteInt32(session.ConnectionID); 
                    p.WriteByte((byte)drop.v_drop.Count);
                    foreach (var el in drop.v_drop)
                    {
                        p.WriteBytes(el.ToArray());
                    }

                    // Aqui o server passa 128 itens de drop, os que dropou e o resto vazio
                    if (drop.v_drop.Count < 128)
                    {
                        p.WriteZero((128 - drop.v_drop.Count) * 16);
                    }
                    SendBroadCast(p);
                }
            }
        }



      

        public bool InitGame()
        {
            // Inicializar Treasure Hunter Info do Stroke Base
            foreach (var el in Players.ToArray())
            {

                InitPlayerInfo("ini_treasure_hunter_info",
                    "tentou inicializar o Treasure hunter info do Stroke base",
                   el, out PlayerGameInfo pgi);

                _treasureHunterInfo.Update(pgi.thi);
            }

            return true;
        }

        #endregion

        #region Game Timers


        public void GameTime(object quem)
        {
            try
            {
                // Pega info do jogador do turno
                InitPlayerInfo("startTime",
                    "tentou começar o tempo do player do turno no jogo",
                    (Player)quem, out PlayerGameInfo pgi);

                // Incrementa número de tacadas do jogador
                pgi.data.tacada_num++;

                // Para timer antigo, se existir
                GameStop();

                // Cria novo timer baseado no tempo do HoleMode 
                Timer = GameServer.Instance.MakeTimer(RoomInfo.TimeSec, () => GameEnd(this, quem));
            }
            catch (exception e) // <- Corrigido aqui!
            {
                _smp.LogManager.Instance.push(new AppMessage(
                    $"[StrokeBase::startTime][ErrorSystem] {e.Message}",
                    type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }


        public virtual void TimeIsOver(object quem)
        {

            if (quem == null)
            {
                _smp.LogManager.Instance.push(new AppMessage("[StrokeBase::timeIsOver][Warning] time is over executed without _quem, _quem is invalid(null). Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            GameStateVersus.SetStateWithLock(STATE_VERSUS.END_SHOT); 
        }

        public void GameEnd(object arg1, object arg2)
        {
            var game = (StrokeBase)arg1;

            try
            {
                if (game?.Timer == null)
                    return;

                // Se o tempo acabou
                game.TimeIsOver(arg2); // Aqui você implementa o que deve acontecer
            }
            catch (Exception e)
            {
                _smp.LogManager.Instance.push(
                    new AppMessage("[StrokeBase::end_time][ErrorSystem] " + e.Message, type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }
        #endregion

        #region Sort
        public int InitSortPlayerTurnHole(PlayerGameInfo pgi1, PlayerGameInfo pgi2)
        {
            if ((pgi1.progress.hole - 1) < 0)
                return 0;  // Considera empate se o índice for inválido

            var index = pgi1.progress.hole - 1;

            if (pgi1.progress.score[index] < pgi2.progress.score[index])
                return -1;

            if (pgi1.progress.score[index] > pgi2.progress.score[index])
                return 1;

            if (pgi1.data.pang > pgi2.data.pang)
                return -1;

            if (pgi1.data.pang < pgi2.data.pang)
                return 1;

            if (pgi1.data.score < pgi2.data.score)
                return -1;

            if (pgi1.data.score > pgi2.data.score)
                return 1;

            return 0;
        }

        public int SortPlayerTurn(PlayerOrderTurnCtx potc1, PlayerOrderTurnCtx potc2)
        {
            var diff1 = potc1.hole.getPinLocation().diffXZ(potc1.pgi.location);
            var diff2 = potc2.hole.getPinLocation().diffXZ(potc2.pgi.location);

            bool p1Acerto = potc1.pgi.shot_sync.state_shot.display.acerto_hole;
            bool p1Giveup = potc1.pgi.data.giveup == 1;
            bool p2Acerto = potc2.pgi.shot_sync.state_shot.display.acerto_hole;
            bool p2Giveup = potc2.pgi.data.giveup == 1;

            if (!p1Acerto && (p2Acerto || p2Giveup))
                return -1;

            if (p1Acerto && !(p2Acerto || p2Giveup))
                return 1;

            if (potc1.pgi.data.tacada_num == 0 && potc2.pgi.data.tacada_num > 0)
                return -1;

            if (potc1.pgi.data.tacada_num > 0 && potc2.pgi.data.tacada_num == 0)
                return 1;

            if (diff1 > diff2 && !p1Acerto && !p1Giveup)
                return -1;

            if (diff1 < diff2 && !(p2Acerto || p2Giveup))
                return 1;

            if (diff1 == diff2)
            {
                if (potc1.pgi.data.tacada_num < potc2.pgi.data.tacada_num && !p1Acerto && !p1Giveup)
                    return -1;

                if (potc1.pgi.data.tacada_num > potc2.pgi.data.tacada_num && !(p2Acerto || p2Giveup))
                    return 1;

                if (potc1.pgi.data.tacada_num == potc2.pgi.data.tacada_num)
                {
                    if (potc1.pgi.data.pang > potc2.pgi.data.pang && !p1Acerto && !p1Giveup)
                        return -1;

                    if (potc1.pgi.data.pang < potc2.pgi.data.pang && !(p2Acerto || p2Giveup))
                        return 1;
                }
            }

            return 0;
        }
        #endregion

        #region HANDLE TURN

        public void InitTurnHole()
        {

            if (PlayerOrder.Count > 0)
                PlayerOrder.Clear();

            foreach (var el in Players)
            {
                if (el != null)
                {
                    InitPlayerInfo("init_turn_hole_start", " tentou calcular o player do turno do comeco do hole no jogo", el, out PlayerGameInfo pgi);

                    if (pgi != null && pgi.flag != PlayerGameInfo.eFLAG_GAME.QUIT)
                    {
                        PlayerOrder.Add(pgi);
                    }
                }
            }


            PlayerOrder.Sort(InitSortPlayerTurnHole);
        }

        public bool CheckPlayerTurnExistOnGame()
        {

            foreach (var el in Players.ToArray())
            {

                InitPlayerInfo("checkPlayerTurnExistOnGame",
                    "verifica se o player turno existe no jogo",
                    el, out PlayerGameInfo pgi);

                // Existe
                if (PlayerTurn == pgi)
                {
                    return true;
                }
            }

            // Não existe
            return false;
        }

        public PlayerGameInfo UpdateNextPlayerTurnHole()
        {
            // Loop iterativo — evita StackOverflowException quando todos os players
            // estão com ServerFlag QUIT e a lista se esvazia durante a recursão original.
            while (PlayerOrder.Count > 0)
            {
                var pgi = PlayerOrder.First();
                PlayerOrder.Remove(pgi);

                if (pgi != null && pgi.flag != PlayerGameInfo.eFLAG_GAME.QUIT)
                    return pgi;
            }

            return null;
        }

        public PlayerGameInfo CalculePlayerTurn()
        {

            if ((PlayerTurn = UpdateNextPlayerTurnHole()) != null)
            {
                return PlayerTurn;
            }

            if (PlayerInfo.Count > 0)
            {

                var hole = Course.findHole(PlayerInfo.First().Value.hole);

                if (hole == null)
                {
                    _smp.LogManager.Instance.push(new AppMessage("[StrokeBase::RequestCalculePlayerTurn][Error] Normal[UID=" + Convert.ToString(PlayerInfo.First().Value.uid) + "] o hole[NUMERO=" + Convert.ToString(PlayerInfo.First().Value.hole) + "] nao foi encontrado no CourseIndex. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                    PlayerTurn = null;

                    return null;
                }

                List<PlayerOrderTurnCtx> v_player_order_turn = new List<PlayerOrderTurnCtx>();

                foreach (var el in Players)
                {

                    if (el != null)
                    {

                        InitPlayerInfo("RequestCalculePlayerTurn",
                            " tentou calcular o player do turno no jogo",
                            el, out PlayerGameInfo pgi);

                        if (pgi.flag != PlayerGameInfo.eFLAG_GAME.QUIT)
                        {
                            v_player_order_turn.Add(new PlayerOrderTurnCtx(pgi, hole));
                        }
                    }
                }

                if (v_player_order_turn.Count == 0)
                {
                    PlayerTurn = null;

                    _smp.LogManager.Instance.push(new AppMessage("[StrokeBase::RequestCalculePlayerTurn][Error] Ninguém foi selecionado como próximo turno. PlayerOrder pode estar vazio.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                    return null;
                }
                v_player_order_turn.Sort(SortPlayerTurn);

                // v_player_order_turn.Count > 0 é garantido pelo guard acima — acesso direto é seguro.
                PlayerTurn = v_player_order_turn[0].pgi;
            }

            return PlayerTurn;
        }
         
        public void ChangeTurn()
        {

            if (PlayerTurn == null)
            {
                throw new exception("PlayerGameInfo *PlayerTurn is invalid(null). Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                    100, 0));
            }
             
            // Check Player Turn finish last hole
            if (PlayerTurn.shot_sync.state_shot.display.acerto_hole || PlayerTurn.data.giveup == 1)
            {

                // Verifica se o player terminou jogo, fez o ultimo hole
                if (Course.findHoleSeq(PlayerTurn.hole) == RoomInfo.HoleCount)
                {

                    // Resposta para o player que terminou o ultimo hole do Game  
                    SendBroadCast(new Packet(0x199));

                    // Fez o Ultimo Hole, Calcula Clear Bonus para o player
                    if (PlayerTurn.shot_sync.state_shot.display.clear_bonus)
                    {

                        if (!MapSystem.Instance.isLoad())
                        {
                            MapSystem.Instance.load();
                        }

                        var map = MapSystem.Instance.getMap((RoomInfo.GetMap()));

                        if (map == null)
                        {
                            _smp.LogManager.Instance.push(new AppMessage("[StrokeBase::ChangeTurn][Error][Warning] tentou pegar o Map dados estaticos do CourseIndex[COURSE=" + Convert.ToString((ushort)(RoomInfo.GetMap())) + "], mas nao conseguiu encontra na classe do Server.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                        }
                        else
                        {
                            PlayerTurn.data.bonus_pang += MapSystem.Instance.calculeClearVS(map,
                            (uint)Players.Count(),
                            RoomInfo.HoleCount);
                        }
                    }
                }
            }

            // Limpa dados que usa para cada tacada
            ClearDataEndShot(PlayerTurn);

            // Verifica se todos fizeram o hole 
            if (CheckAllClearHole())
            {

                ClearAllFlagSync();

                FinishHole();

                // Utilizo ele antes no finish hole, limpo ele aqui depois
                ClearAllClearHole();

                ChangeHole();

            }
            else if (Players.Count() == 1 && Course.findHoleSeq(PlayerTurn.hole) < 4)
            { // Finaliza o game

                ClearAllFlagSync();

                FinishHole();

                ChangeHole();

            }
            else
            { // Troca o Turno

                ClearAllFlagSync();

                // Recalcula Turno
                CalculePlayerTurn();
                // Cnvia para todos o vento e OID do player turn, o player que vai tacar nesse momento
                SendPlayerTurn();
            }
        }
         
        public object SyncGameTurn()
        {
            var datetime = Stopwatch.StartNew();
            TimeSpan ts = datetime.Elapsed;
            try
            {
                string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}", ts.Hours, ts.Minutes, ts.Seconds);

                _smp.LogManager.Instance.push(new AppMessage($"[StrokeBase::CheckStrokeTurn] Partida comecou: {elapsedTime}", type_msg.CL_FILE_LOG_AND_CONSOLE));

                uint retWait = WAIT_TIMEOUT;//
                IntPtr[] wait_events = { _checkTurnEvent, _checkTurnPulseEvent };

                while ((retWait = WaitForMultipleObjects((uint)wait_events.Length, wait_events, false, 1000 /*1 segundo*/)) == WAIT_TIMEOUT || retWait == (WAIT_OBJECT_0 + 1))
                {
                    try
                    {
                        GameStateVersus.@lock();
                        switch (GameStateVersus.getState())
                        {
                            case STATE_VERSUS.WAIT_HIT_SHOT:
                                HandleWaitHitShot();
                                break;
                            case STATE_VERSUS.SHOTING:
                                HandleShoting();
                                break;
                            case STATE_VERSUS.END_SHOT:
                                HandleEndShot();
                                break;
                            case STATE_VERSUS.LOAD_HOLE:
                                HandleLoadHole();
                                break;
                            case STATE_VERSUS.WAIT_END_GAME:
                                break;
                            default:
                                break;
                        }
                        GameStateVersus.unlock();
                    }
                    catch (exception ex)
                    {
                        _smp.LogManager.Instance.push(new AppMessage("[StrokeBase::CheckStrokeTurn][ErrorSystem] " + ex.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                        GameStateVersus.unlock();
                    }
                    catch (Exception ex)
                    { 
                        _smp.LogManager.Instance.push(new AppMessage($"[StrokeBase::CheckStrokeTurn][UnhandledException] {ex.GetType().Name}: {ex.Message}", type_msg.CL_FILE_LOG_AND_CONSOLE));
                        GameStateVersus.unlock();
                    }
                }
                //para o tempo
                datetime.Stop();
                ts = datetime.Elapsed;
                elapsedTime = String.Format("{0:00}:{1:00}:{2:00}", ts.Hours, ts.Minutes, ts.Seconds);

                _smp.LogManager.Instance.push(new AppMessage($"[StrokeBase::CheckStrokeTurn] Partida Finalizada. Tempo total: {elapsedTime}", type_msg.CL_FILE_LOG_AND_CONSOLE));

            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[StrokeBase::CheckStrokeTurn][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
            return null;
        }
         
        private void HandleWaitHitShot()
        {
            if (NextStepGameFlag == 0)
                return;

            ValidatePlayerTurn();

            if (NextStepGameFlag == 1 && PlayerTurn.flag == PlayerGameInfo.eFLAG_GAME.QUIT)
            {
                var p = new Packet(0x92);
                SendBroadCast(p);
            }
            else if (NextStepGameFlag == 2)
            {
                _smp.LogManager.Instance.push(new AppMessage("[StrokeBase::HandleWaitHitShot][Log] Finaliza game.", type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
            else if (PlayerTurn.flag == PlayerGameInfo.eFLAG_GAME.QUIT)
            {
                ChangeTurn();
            }

            if (NextStepGameFlag != 1)
                NextStepGameFlag = 0;
        }

        private void HandleShoting()
        {
            if (CheckAllSyncShot())
            {
                ValidatePlayerTurn();

                var session = FindSessionByPlayerGameInfo(PlayerTurn);
                if (session != null)
                {
                    DrawDropItem(session);
                }
                else
                {
                    _smp.LogManager.Instance.push(new AppMessage($"[StrokeBase::CheckStroke][Error] Player UID={PlayerTurn.uid} não encontrado no mapa player_info.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                }

                SendSyncShot();
                ClearAllSyncShot();
                GameStateVersus.setState(STATE_VERSUS.END_SHOT);
                return;
            }

            if (NextStepGameFlag != 0)
            {
                HandleWaitHitShot(); // Reaproveita a lógica semelhante
                return;
            }
            // Verifica tempo dos players para reenvio do pacote 1B sync Shot
            CheckPlayersSyncShotTimeout(10, 3, 0x8A);
        }

        private void HandleEndShot()
        {
            if (Players.Count <= 0)
            {
                return;
            }

            if (CheckAllFinishShot())
            {
                ClearAllFinishShot();

                ValidatePlayerTurn();

                if (NextStepGameFlag != 0)
                {
                    if (NextStepGameFlag == 1)
                    {
                        var p = new Packet(0x92);
                        SendBroadCast(p);
                    }
                    else if (NextStepGameFlag == 2)
                    {
                        _smp.LogManager.Instance.push(new AppMessage("[StrokeBase::HandleEndShot][Log] Change Turn.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                        ChangeTurn();
                    }
                    else
                    {
                        ChangeTurn();
                    }
                    NextStepGameFlag = 0;
                }
                else
                {
                    ChangeTurn();
                }
                return;
            }

            if (NextStepGameFlag != 0)
            {
                HandleWaitHitShot(); // Reaproveita a lógica semelhante
                return;
            }

            // Verifica tempo dos players para desconectar após 10 segundos
            CheckPlayersSyncShotTimeout(10, 3, 0x8A, isFinishShot: true);
        }

        private void HandleLoadHole()
        {
            if (!CheckAllLoadHole())
                return;

            ClearLoadHole();

            SendReplyFinishLoadHole();
            SendRatesOfStrokeBase();
            GameStateVersus.setState(STATE_VERSUS.WAIT_HIT_SHOT);
        }
        private void ValidatePlayerTurn()
        {
            if (PlayerTurn == null)
                throw new exception("[StrokeBase::CheckStroke] PlayerGameInfo* PlayerTurn is invalid(null)", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS, 1201, 0));
        }

        public bool CheckNextStepGame(Player session)
        {

            var ret = false;

            try
            {

                InitPlayerInfo("checkNextStepGame",
                    "tentou verificar o proximo passo do jogo",
                    session, out PlayerGameInfo pgi);

                var seq = Course.findHoleSeq(pgi.hole);

                if (seq == 0 || seq == -1)
                {
                    throw new exception("[StrokeBase::checkNextStepGame][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou pegar sequencia do hole[NUMERO=" + Convert.ToString(pgi.hole) + ", SEQ=" + Convert.ToString(seq) + "], mas nao encontrou CourseIndex. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                        500, 0));
                }

                if (Players.Count == 2 && seq >= 4)
                {

                    if (RoomInfo.HoleCount == 18)
                    { // 18 Envia a pergunta se o player quer continuar o VS Sozinho

                        if (PlayerTurn == null)
                        {

                            // Player Turn ainda não foi decidido, termina o jogo
                            GameStateVersus.SetStateWithLock(STATE_VERSUS.WAIT_END_GAME);

                            ret = true; // Termina o Game

                        }
                        else if (PlayerTurn == pgi)
                        {

                            // só tem 2 na sala, então só retorna uma session
                            var sessions = GetSessions(session);

                            var p = new Packet(0x92);

                            if (sessions.Count > 1)
                            {
								sessions.SendBroadCast(p);
							}
                            else
                            {
                                if (sessions.Count > 0)
                                    sessions.First().Send(p); 
                            }

                        }
                        else if (!CheckPlayerTurnExistOnGame())
                        {

                            // Player Turn não está mais no jogo, termina o jogo
                            GameStateVersus.SetStateWithLock(STATE_VERSUS.WAIT_END_GAME);

                            ret = true; // Termina o Game

                        }
                        else
                        {
                            NextStepGameFlag = 1; // Pergunta se quer continuar
                        }

                    }
                    else if (PlayerTurn == null)
                    {

                        // Player Turn ainda não foi decidido, termina o jogo
                        GameStateVersus.SetStateWithLock(STATE_VERSUS.WAIT_END_GAME);

                        ret = true; // Termina o Game

                    }
                    else if (PlayerTurn == pgi)
                    {

                        GameStateVersus.SetStateWithLock(STATE_VERSUS.WAIT_END_GAME);

                        ret = true; // Termina o Game

                    }
                    else if (!CheckPlayerTurnExistOnGame())
                    {

                        // Player Turn não está mais no jogo, termina o jogo
                        GameStateVersus.SetStateWithLock(STATE_VERSUS.WAIT_END_GAME);

                        ret = true; // Termina o Game

                    }
                    else
                    {
                        NextStepGameFlag = 2; // Termina o game
                    }

                }
                else if (Players.Count == 2)
                {

                    if (PlayerTurn == null)
                    {

                        // Player Turn ainda não foi decidido, termina o jogo
                        GameStateVersus.SetStateWithLock(STATE_VERSUS.WAIT_END_GAME);

                        ret = true; // Termina o Game

                    }
                    else if (PlayerTurn == pgi)
                    {

                        GameStateVersus.SetStateWithLock(STATE_VERSUS.WAIT_END_GAME);

                        ret = true; // Termina o Game

                    }
                    else if (!CheckPlayerTurnExistOnGame())
                    {

                        // Player Turn não está mais no jogo, termina o jogo
                        GameStateVersus.SetStateWithLock(STATE_VERSUS.WAIT_END_GAME);

                        ret = true; // Termina o Game

                    }
                    else
                    {
                        NextStepGameFlag = 2; // Termina o game
                    }

                }
                else if (Players.Count == 1)
                { // Player quitou mesmo sendo o ultimo no jogo

                    if (PlayerTurn == null)
                    {

                        // Player Turn ainda não foi decidido, termina o jogo
                        GameStateVersus.SetStateWithLock(STATE_VERSUS.WAIT_END_GAME);

                        ret = true; // Termina o Game

                    }
                    else if (PlayerTurn == pgi)
                    {

                        GameStateVersus.SetStateWithLock(STATE_VERSUS.WAIT_END_GAME);

                        ret = true; // Termina o Game

                    }
                    else if (!CheckPlayerTurnExistOnGame())
                    {

                        // Player Turn não está mais no jogo, termina o jogo
                        GameStateVersus.SetStateWithLock(STATE_VERSUS.WAIT_END_GAME);

                        ret = true; // Termina o Game

                    }
                    else
                    {
                        NextStepGameFlag = 2;
                    }

                }
                else if (PlayerTurn == null)
                {

                    // Player Turn ainda não foi decidido, termina o jogo
                    GameStateVersus.SetStateWithLock(STATE_VERSUS.WAIT_END_GAME);

                    ret = true; // Termina o Game

                }
                else
                {
                    NextStepGameFlag = 3; // Player quitou
                }

            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[StrokeBase::checkNextStepGame][ErroSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            return ret;
        }
         
        public override bool CheckEndGame(Player session)
        {
            InitPlayerInfo("checkEndGame",
                "tentou verificar se eh o final do jogo",
                session, out PlayerGameInfo pgi);

            return (Course.findHoleSeq(pgi.hole) == RoomInfo.HoleCount || (Players.Count == 1 && Course.findHoleSeq(pgi.hole) < 4));
        }

        private void CheckPlayersSyncShotTimeout(int timeoutSeconds, int maxRetries, ushort packetId, bool isFinishShot = false)
        {
            foreach (var s in Players)
            {
                InitPlayerInfo("CheckStrokeTurn",
                    isFinishShot ? "Verifica tempo para pacote1C sync Finish Shot" : "Verifica tempo para pacote1B sync Shot",
                    s, out PlayerGameInfo pgi);

                try
                {


                    if (pgi.sync_shot_flag2 == 0 && pgi.tick_sync_shot.active)
                    {
                        double elapsed = pgi.tick_sync_shot.ElapsedSeconds;

                        if (elapsed > timeoutSeconds)
                        {
                            _smp.LogManager.Instance.push(new AppMessage($"[StrokeBase::CheckPlayersSyncShotTimeout][Log] Normal[UID={s.UserInfo.UID}] não enviou o pacote {(isFinishShot ? "1C" : "1B")} sync shot em {timeoutSeconds} segundos.", type_msg.CL_ONLY_FILE_LOG));

                            if (++pgi.tick_sync_shot.count >= maxRetries)
                            {
                                pgi.tick_sync_shot.Stop();

                                if (isFinishShot)
                                    pgi.finish_shot = 1; // sinaliza que terminou o shot (finalizou)
                                else
                                    pgi.sync_shot_flag2 = 1; // para o caso do sync shot Normal

                                s.Send(new Packet(packetId));
                                _smp.LogManager.Instance.push(new AppMessage($"[StrokeBase::CheckPlayersSyncShotTimeout][Log] Normal[UID={s.UserInfo.UID}] passou {(timeoutSeconds * maxRetries)} segundos, desconectando.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                            }
                            else
                            {
                                pgi.tick_sync_shot.tick = (ulong)Stopwatch.GetTimestamp(); // reinicia só o tempo

                                s.Send(new Packet(packetId));
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Apenas loga — NÃO chama unlock() aqui porque esse método é sempre
                    // chamado de dentro de um bloco que já possui o lock (SyncGameTurn).
                    // Um unlock() extra aqui corromperia o estado do mutex.
                    _smp.LogManager.Instance.push(new AppMessage($"[StrokeBase::CheckPlayersSyncShotTimeout][ErrorSystem] Normal[UID={s.UserInfo.UID}] {ex.Message}", type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
            }
        }

        #endregion

        #region SEND's 
        public override void SendInitialData(Player session)
        {

            var p = new Packet();

            try
            {
                // No Stroke tem o Update Last 5 players play
                if (Interlocked.Increment(ref SyncSendInitData) == Players.Count)
                {
                    Interlocked.Exchange(ref SyncSendInitData, 0);

                    int st_i = 0;

                    List<CardEquipInfoEx> v_card_equip_char_and_special = new List<CardEquipInfoEx>();

                    // Game Data Init
                    p.init_plain(0x76);

                    p.WriteByte(RoomInfo.RoomType);

                    p.WriteByte((byte)Players.Count);

                    foreach (var el in Players)
                    {
                        // Member Info 
                        p.WriteBytes(el.UserInfo.Member.ToArray(IncludeRoomID :true));
                        // User Info
                        p.WriteUInt32(el.UserInfo.UID);
                        p.WriteBytes(el.UserInfo.Statistics.ToArray());

                        // Trofel Info Current Season
                        p.WriteBytes(el.Inventory.CurrentTrophy.ToArray());

                        // User Equipped Item
                        p.WriteBytes(el.Inventory.UserEquipment.ToArray());

                        // Map Statistics Normal
                        for (st_i = 0; st_i < MS_NUM_MAPS; st_i++)
                            p.WriteBytes(el.UserInfo.NormalMapStatistics[index: st_i].ToArray());

                        // Map Statistics Natural
                        for (st_i = 0; st_i < MS_NUM_MAPS; st_i++)
                            p.WriteBytes(el.UserInfo.NaturalMapStatistics[st_i].ToArray());

                        // Map Statistics Grand Prix
                        for (st_i = 0; st_i < MS_NUM_MAPS; st_i++)
                            p.WriteBytes(el.UserInfo.GrandPrixMapStatistics[st_i].ToArray());

                        for (int j = 0; j < 9; j++)
                            for (st_i = 0; st_i < MS_NUM_MAPS; st_i++)
                                p.WriteBytes(el.UserInfo.AllSeasonsMapStatistics[j, st_i].ToArray());

                        // Character Info(CharEquip)
                        if (el.Inventory.UserEquippedItem.CharacterEquiped != null && el.Inventory.UserEquippedItem.CharacterEquiped.id != 0)
                        {

                            var tmp_char_info = el.Inventory.UserEquippedItem.CharacterEquiped;

                            int maxSlot = -1;

                            for (byte stats = 0; stats < 5; stats++)
                            { 
                                maxSlot = el.Inventory.getCharacterMaxSlot(stats, session.UserInfo.Member.GameLevel);//tenho que fazer algo melhor, ta sujo o codigo

                                // Não deixa passar do Slot em jogo
                                if (maxSlot != -1 && tmp_char_info.pcl[stats] > maxSlot)
                                {
                                    tmp_char_info.pcl[stats] = (byte)maxSlot;
                                }
                            }
                            p.WriteBytes(el.Inventory.UserEquippedItem.CharacterEquiped.ToArray());
                        }
                        else
                            p.WriteZero(513);

                        // Caddie Info
                        if (el.Inventory.UserEquippedItem.CaddieEquiped != null && el.Inventory.UserEquippedItem.CaddieEquiped.id != 0)
                            p.WriteBytes(el.Inventory.UserEquippedItem.CaddieEquiped.ToArray());
                        else
                            p.WriteZero(25);

                        // Club Set Info
                        if (el.Inventory.UserEquippedItem.ClubEquiped != null && el.Inventory.UserEquippedItem.ClubEquiped.id != 0)
                        { 
                            var tmp_csi = el.Inventory.UserEquippedItem.ClubEquiped; 
                            int value = -1; 
                            for (byte stats = 0; stats < 5; stats++)
                            { 
                                value = el.Inventory.getClubSetMaxSlot(stats);

                                // Não deixa passar do Slot em jogo
                                if (value != -1 && tmp_csi.slot_c[stats] > value)
                                {
                                    tmp_csi.slot_c[stats] = (short)value;
                                }
                            }

                            p.WriteBytes(tmp_csi.ToArray());
                        }
                        else
                            p.WriteZero(28);

                        // Mascot Info
                        if (el.Inventory.UserEquippedItem.MascotEquiped != null && el.Inventory.UserEquippedItem.MascotEquiped.id != 0)
                        {
                            p.WriteBytes(el.Inventory.UserEquippedItem.MascotEquiped.ToArray());
                        }
                        else
                            p.WriteZero(62);

                        // Time Start
                        p.WriteTime(StartTime);

                        // Card(s) Equipped, acho que aqui não vai os itens buff, por que ele só da buff de Experience e Pang, o outro player nao precisa saber
                        v_card_equip_char_and_special = new List<CardEquipInfoEx>();

                        foreach (var el2 in el.Inventory.CardEquipment)
                        {
                            if ((el2.parts_id == 0 && el2.parts_typeid == 0) || (el.Inventory.UserEquippedItem.CharacterEquiped != null && el2.parts_id == el.Inventory.UserEquippedItem.CharacterEquiped.id && el2.parts_typeid == el.Inventory.UserEquippedItem.CharacterEquiped._typeid))
                            {
                                v_card_equip_char_and_special.Add(el2);
                            }
                        }

                        p.WriteByte((byte)v_card_equip_char_and_special.Count);

                        foreach (var el2 in v_card_equip_char_and_special)
                        {
                            p.WriteBytes(el2.ToArray());
                        }
                    }

                    SendBroadCast(p);

                    // Send Individual Packet to all players in game
                    foreach (var el in Players)
                    {
                        // Send MapStatistics Info
                        SendUpdateInfoAndMapStatistics(el, -1);

                        // Course
                        base.SendInitialData(el);

                        // Send seed Mascot Effect
                        p.init_plain(0x16A);

                        p.WriteUInt32(_mascotEffectSeed);
                        el.Send(p);
                    }
                }
            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[StrokeBase::sendInitialData][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void SendRatesOfStrokeBase()
        {

            try
            {
                // Table Rate Voice And Effect
                TableRateVoiceAndEffect table = new TableRateVoiceAndEffect("W_BIGBONGDARI", TableRateVoiceAndEffect.eTYPE.W_BIGBONGDARI);

                // Rate Table Voice
                var p = new Packet(0x115);

                p.WriteString(table.name);

                p.WriteBytes(table.table, table.table.Length);

                SendBroadCast(p);

                // Table Rate Voice And Effect
                table = new TableRateVoiceAndEffect("R_BIGBONGDARI", TableRateVoiceAndEffect.eTYPE.R_BIGBONGDARI);

                p.init_plain(0x115);

                p.WriteString(table.name);

                p.WriteBytes(table.table, table.table.Length);

                SendBroadCast(p);

                // Table Rate Voice And Effect
                table = new TableRateVoiceAndEffect("VOICE_CLUB", TableRateVoiceAndEffect.eTYPE.VOICE_CLUB);
                p.init_plain(0x115);

                p.WriteString(table.name);

                p.WriteBytes(table.table, table.table.Length);

                SendBroadCast(p);
            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[StrokeBase::SendRatesOfStrokeBase][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }
         
        public void SendFinishHole()
        {
            SendBroadCast(new Packet((ushort)0x65));
        }
         
        public void SendTreasureHunterPoint()
        {

            if (!sTreasureHunterSystem.Instance.isLoad())
            {
                sTreasureHunterSystem.Instance.load();
            }

            // Calcule Treasure Pontos
            foreach (var el in Players)
            {

                InitPlayerInfo("updateTreasureHunterPoint",
                    "tentou atualizar os pontos do Treasure Hunter no jogo",
                    el, out PlayerGameInfo pgi);

                var hole = Course.findHole(pgi.hole);

                if (hole == null)
                {
                    throw new exception("[StrokeBase::updateTreasureHunterPoint][Error] Normal[UID=" + Convert.ToString(el.UserInfo.UID) + "] tentou atualizar os pontos do Treasure Hunter no hole[NUMERO=" + Convert.ToString((ushort)pgi.hole) + "], mas o hole nao existe. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY_BASE,
                        30, 0));
                }


                _treasureHunterInfo.treasure_point += sTreasureHunterSystem.Instance.calcPointNormal(pgi.data.tacada_num, hole.getPar().par) + _treasureHunterInfo.getPoint(pgi.data.tacada_num, (byte)hole.getPar().par);
            }

            // Mostra score board
            var p = new Packet((ushort)0x132);

            p.WriteUInt32(_treasureHunterInfo.treasure_point);

            // No Modo Match passa outro valor tbm
            SendBroadCast(p);
        }


        public void SendReplyFinishCharIntro()
        {
            // Resposta para Finish Char Intro 
            SendBroadCast(new Packet(0x90));
        }

        public void SendPlayerTurn()
        {
            if (PlayerTurn == null)
            {
                _smp.LogManager.Instance.push(new AppMessage("[StrokeBase::SendPlayerTurn][ERROR] PlayerTurn está null. Ninguém tem o turno!", type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            if (PlayerTurn == null)
            {
                throw new exception("[StrokeBase::SendPlayerTurn][Error] PlayerGameInfo *PlayerTurn is invalid(null). Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                    100, 1));
            }

            var hole = Course.findHole(PlayerTurn.hole) ?? throw new exception("[StrokeBase::SendPlayerTurn][Error] Normal[UID=" + Convert.ToString(PlayerTurn.uid) + "] tentou encontrar o hole[NUMERO=" + Convert.ToString(PlayerTurn.hole) + "] do CourseIndex no jogo, mas nao foi encontrado. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                    101, 0));

            var wind_flag = InitCardWindPlayer(PlayerTurn, hole.getWind().wind);

            // Resposta do vento do hole
            var p = new Packet(0x5B);

            p.WriteByte(hole.getWind().wind + wind_flag);
            p.WriteByte((wind_flag < 0) ? 1 : 0); // ServerFlag de card de vento, aqui é a qnd diminui o vento, 1 Vento Blue
            p.WriteUInt16(PlayerTurn.degree);
            p.WriteByte(1); // ServerFlag do vento, 1 Reseta o Vento, 0 soma o vento que nem o comando gm \wind do pangya original, , Também é ServerFlag para trocar o vento no Pang Battle se mandar o valor 0
            SendBroadCast(p);

            // Resposta passa o OID do player que vai começa o Hole
            p.init_plain(0x63);

            if (PlayerTurn == null)
            {
                _smp.LogManager.Instance.push(new AppMessage("[StrokeBase::SendPlayerTurn][Error] player_turn is invalid(null)", type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.WriteUInt32(0);
            }
            else
            {
                p.WriteInt32(PlayerTurn.oid);
            }
            SendBroadCast(p);
        }

        public void SendReplyFinishLoadHole()
        {

            try
            {

                InitTurnHole();

                PlayerGameInfo pgi = CalculePlayerTurn();

                var hole = Course.findHole(pgi.hole);

                if (hole == null)
                {
                    throw new exception("[StrokeBase::requestFinishLoadHole][Error] Normal[UID=" + Convert.ToString(pgi.uid) + "] tentou finalizar carregamento do hole[NUMERO=" + Convert.ToString(pgi.hole) + "], mas nao conseguiu encontrar o hole no CourseIndex. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                        201, 0));
                }

                // Resposta de tempo do hole
                var p = new Packet(0x9E); 
                p.WriteUInt16(hole.getWeather());
                p.WriteByte(0); // Option do tempo, sempre peguei zero aqui dos pacotes que vi
                SendBroadCast(p); 
                var wind_flag = InitCardWindPlayer(PlayerTurn, hole.getWind().wind);

                // Resposta do vento do hole
                p.init_plain(0x5B); 
                p.WriteByte(hole.getWind().wind + wind_flag);
                p.WriteByte((wind_flag < 0) ? 1 : 0); // ServerFlag de card de vento, aqui é a qnd diminui o vento, 1 Vento Blue
                p.WriteUInt16(PlayerTurn.degree);
                p.WriteByte(1); // ServerFlag do vento, 1 Reseta o Vento, 0 soma o vento que nem o comando gm \wind do pangya original, Também é ServerFlag para trocar o vento no Pang Battle se mandar o valor 0
                SendBroadCast(p);

                // Resposta passa o OID do player que vai começa o Hole
                p.init_plain(0x53); 
                if (PlayerTurn == null)
                {
                    _smp.LogManager.Instance.push(new AppMessage("[StrokeBase::requestFinishLoadHole][Error] player_turn is invalid(null)", type_msg.CL_FILE_LOG_AND_CONSOLE));

                    p.WriteUInt32(0);
                }
                else
                {
                    p.WriteInt32(PlayerTurn.oid);
                }
                SendBroadCast(p);
            }
            catch (exception e)
            { 
                _smp.LogManager.Instance.push(new AppMessage("[StrokeBase::sendReplyFinishLoadHole][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }


        public void SendSyncShot()
        {

            if (PlayerTurn == null)
            {
                throw new exception("[StrokeBase::SendSyncShot][Error] PlayerGameInfo* PlayerTurn is invalid(null)", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                    1200, 0));
            }

            var p = new Packet(0x64);

            p.WriteBytes(PlayerTurn.shot_sync.ToArray());

            SendBroadCast(p);
        }

        public void SendEndShot(Player session, DropItemRet cube)
        {

            var p = new Packet(0xCC);

            p.WriteInt32(session.ConnectionID);

            // Count, Coin/Cube "Drop"
            p.WriteByte((byte)cube.v_drop.Count);

            if (cube != null && cube.v_drop != null && cube.v_drop.Count > 0)
            {

                foreach (var el in cube.v_drop)
                {
                    p.WriteBytes(el.ToArray());
                }

                // Aqui o server passa 128 itens de drop, os que dropou e o resto vazio
                if (cube.v_drop.Count < 128)
                {
                    p.WriteZero((128 - cube.v_drop.Count) * 16);
                }
            }

            SendBroadCast(p);
        }

        public void SendDropItem(Player session)
        {

            var p = new Packet(0xFA);

            p.WriteUInt16((ushort)Players.Count);

            foreach (var el in Players)
            {

                InitPlayerInfo("SendDropItem",
                    "tentou enviar os itens dropado do player no jogo",
                    el, out PlayerGameInfo pgi);

                p.WriteInt32(el.ConnectionID);

                p.WriteByte(0); // OK

                p.WriteUInt16((ushort)pgi.drop_list.v_drop.Count);

                foreach (var els in pgi.drop_list.v_drop)
                {
                    p.WriteUInt32(els._typeid);
                }
            }
            session.Send(p);
        }

        public void SendPlacar(Player session)
        {

            var p = new Packet(0x66);

            p.WriteByte((byte)Players.Count);

            foreach (var el in Players)
            {

                InitPlayerInfo("SendPlacar",
                    "tentou enviar o placar do jogo",
                    el, out PlayerGameInfo pgi);

                p.WriteInt32(el.ConnectionID);
                p.WriteSByte(Convert.ToSByte(GetRankPlace(el)));
                p.WriteSByte(Convert.ToSByte(pgi.data.score));//tava char antes
                p.WriteSByte(Convert.ToSByte(pgi.data.total_tacada_num)); 
                p.WriteUInt16((ushort)pgi.data.exp);
                p.WriteUInt64(pgi.data.pang);
                p.WriteUInt64(pgi.data.bonus_pang);

                // Valor que usa no Pang Battle, valor de Pang que ganhou ou perdeu
                // Como aqui é vs Base deixa o valor 0
                p.WriteUInt64(0);
            }
            session.Send(p);
        }

        public void SendTreasureHunterItemDrawGUI(Player session)
        {

            InitPlayerInfo("SendTreasureHunterItemDrawGUI",
                "tentou enviar os itens ganho no Treasure Hunter(so o Visual) do jogo",
                session, out PlayerGameInfo pgi);

            var p = new Packet(0x133);

            p.WriteByte((byte)_treasureHunterInfo.v_item.Count);

            // No VS aqui os itens são dividido entres os players do Stroke
            foreach (var el in _treasureHunterInfo.v_item)
            {
                p.WriteUInt32(el.uid); // UID do player que ganhou o item
                p.WriteUInt32(el.thi._typeid);
                p.WriteUInt16((ushort)el.thi.qntd);
                p.WriteByte(0); // Acho que sejá opção ou dizendo que acabou o struct de Treasure Hunter Item Draw}
            }
            session.Send(p);
        }

        #endregion

        #region ABSTRACT  
        public abstract void ChangeHole();
        public abstract void FinishHole();
        #endregion

        #region CHECKS
        // 2. Verificação Robusta de Sincronismo
        public bool CheckAllSyncShot()
        {
            if (Players.Count == 0) return true;

            int count = 0;
            foreach (var el in Players)
            {
                if (el == null || !el.Connected)
                {
                    count++;
                    continue;
                }

                InitPlayerInfo("CheckAllSyncShot", "verificando sincronia", el, out PlayerGameInfo pgi);

                // Se o player confirmou OU se ele saiu do jogo, contamos como pronto
                if (pgi.sync_shot_flag == 1 || pgi.flag == PlayerGameInfo.eFLAG_GAME.QUIT)
                    count++;
            }
            return (count >= Players.Count);
        }


        public bool CheckAllClearHole()
        {

            uint count = 0;

            // Check
            Players.ForEach(el =>
            {
                try
                {
                    InitPlayerInfo("checkAllClearHole",
                        "tentou verificar se todos os player terminaram o hole no jogo",
                        el, out PlayerGameInfo pgi);
                    if (pgi.shot_sync.state_shot.display.acerto_hole || pgi.data.giveup.IsTrue())
                    {
                        count++;
                    }
                }
                catch (exception e)
                {
                    _smp.LogManager.Instance.push(new AppMessage("[StrokeBase::checkAllClearHole][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
            }); 
            return (count == Players.Count);
        }

        public void ClearAllClearHole()
        {
            ClearAllHole();
        }

        public void SetLoadHole(PlayerGameInfo pgi)
        {

            if (pgi == null)
            {

                _smp.LogManager.Instance.push(new AppMessage("[StrokeBase::setLoadHole][Error] PlayerGameInfo* _pgi is invalid(null).", type_msg.CL_FILE_LOG_AND_CONSOLE));

                return;
            }
            // Set
            pgi.finish_load_hole = 1;

            if (_checkTurnPulseEvent != INVALID_HANDLE_VALUE)
                SetEvent(_checkTurnPulseEvent);
        }

        public bool CheckAllLoadHole()
        {

            uint count = 0;

            // Check
            Players.ForEach(el =>
            {
                try
                {
                    InitPlayerInfo("CheckAllLoadHole",
                        "tentou verificar se todos os player terminaram de carregar o hole no jogo",
                        el, out PlayerGameInfo pgi);
                    if (pgi.finish_load_hole.IsTrue())
                    {
                        count++;
                    }
                }
                catch (exception e)
                {
                    _smp.LogManager.Instance.push(new AppMessage("[StrokeBase::CheckAllLoadHole][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
            });
            return (count == Players.Count);
        }

        public void ClearLoadHole()
        {
            ClearAllLoadHole();
        }

        public bool SetFinishCharIntroAndCheckAllFinishCharIntroAndClear(PlayerGameInfo pgi)
        {

            if (pgi == null)
            {

                _smp.LogManager.Instance.push(new AppMessage("[StrokeBase::setFinishCharIntroAndCheckAllFinishCharIntroAndClear][Error] PlayerGameInfo* _pgi is invalid(null).", type_msg.CL_FILE_LOG_AND_CONSOLE));

                return false;
            }

            uint count = 0;
            bool ret = false;

            // Set
            pgi.finish_char_intro = 1;

            // Check
            Players.ForEach(el =>
            {
                try
                {
                    InitPlayerInfo("setFinishCharIntroAndCheckAllFinishCharIntroAndClear",
                        "tentou verificar se todos os player terminaram a Intro do Character no jogo",
                        el, out PlayerGameInfo pgi);
                    if (pgi.finish_char_intro.IsTrue())
                    {
                        count++;
                    }
                }
                catch (exception e)
                {
                    _smp.LogManager.Instance.push(new AppMessage("[StrokeBase::setFinishCharIntroAndCheckAllFinishCharIntroAndClear][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
            });

            ret = (count == Players.Count);

            // Clear
            if (ret)
            {
                ClearAllFinishCharIntro();
            }
            return ret;
        }

        public void SetFinishShot(PlayerGameInfo pgi)
        {

            if (pgi == null)
            {

                _smp.LogManager.Instance.push(new AppMessage("[StrokeBase::setFinishShot][Error] PlayerGameInfo* _pgi is invalid(null).", type_msg.CL_FILE_LOG_AND_CONSOLE));

                return;
            }

            // Set
            pgi.finish_shot = 1;

            if (_checkTurnPulseEvent != INVALID_HANDLE_VALUE)
                SetEvent(_checkTurnPulseEvent);

        }

        public bool CheckAllFinishShot()
        {

            uint count = 0;

            // Check
            Players.ForEach(el =>
            {
                try
                {
                    InitPlayerInfo("CheckAllFinishShot",
                        "tentou verificar se todos os player terminaram a Tacada no jogo",
                        el, out PlayerGameInfo pgi);
                    if (pgi.finish_shot > 0)
                    {
                        count++;
                    }
                }
                catch (exception e)
                {
                    _smp.LogManager.Instance.push(new AppMessage("[StrokeBase::CheckAllFinishShot][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
            });

            return count == Players.Count;
        }
         
        public void SetSyncShot(PlayerGameInfo pgi)
        {

            if (pgi == null)
            {

                _smp.LogManager.Instance.push(new AppMessage("[StrokeBase::setSyncShot[Error] PlayerGameInfo *_pgi is invalid(null).", type_msg.CL_FILE_LOG_AND_CONSOLE));

                return;
            }

            // Set
            pgi.sync_shot_flag = 1;//possivelmente erro aqui

            if (_checkTurnPulseEvent != INVALID_HANDLE_VALUE)
                SetEvent(_checkTurnPulseEvent);
        }
          
        public void ClearAllHole()
        {

            Players.ForEach(el =>
            {
                try
                {
                    InitPlayerInfo("ClearAllclear_hole",
                        " tentou limpar all clear hole no jogo",
                        el, out PlayerGameInfo pgi);
                    pgi.shot_sync.state_shot.display.acerto_hole = false;
                    pgi.data.giveup = 0;
                }
                catch (exception e)
                {
                    _smp.LogManager.Instance.push(new AppMessage("[StrokeBase::ClearAllhole][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
            });
        }

        public void ClearAllLoadHole()
        {

            Players.ForEach(el =>
            {
                try
                {
                    InitPlayerInfo("ClearAllload_hole",
                        " tentou limpar all load hole no jogo",
                        el, out PlayerGameInfo pgi);
                    pgi.finish_load_hole = 0;
                }
                catch (exception e)
                {
                    _smp.LogManager.Instance.push(new AppMessage("[StrokeBase::ClearAllload_hole][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
            });
        }

        public void ClearAllFinishCharIntro()
        {

            Players.ForEach(el =>
            {
                try
                {
                    InitPlayerInfo("ClearAllfinish_char_intro",
                        " tentou limpar all finish char intro no jogo",
                        el, out PlayerGameInfo pgi);
                    pgi.finish_char_intro = 0;
                }
                catch (exception e)
                {
                    _smp.LogManager.Instance.push(new AppMessage("[StrokeBase::ClearAllfinish_char_intro][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
            });
        }

        public void ClearAllFinishShot()
        {

            Players.ForEach(el =>
            {
                try
                {
                    InitPlayerInfo("ClearAllfinish_shot",
                        " tentou limpar all finish tacada no jogo",
                        el, out PlayerGameInfo pgi);
                    pgi.finish_shot = 0;
                    pgi.tick_sync_end_shot.clear();
                }
                catch (exception e)
                {
                    _smp.LogManager.Instance.push(new AppMessage("[StrokeBase::ClearAllfinish_shot][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
            });
        }

        public void ClearAllFinishShot2()
        {

            Players.ForEach(el =>
            {
                try
                {
                    InitPlayerInfo("ClearAllFinishShot2",
                        " tentou limpar all finish tacada no jogo",
                        el, out PlayerGameInfo pgi);
                    pgi.finish_shot2 = 0; 
                }
                catch (exception e)
                {
                    _smp.LogManager.Instance.push(new AppMessage("[StrokeBase::ClearAllfinish_shot][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
            });
        }

        // 1. Limpeza total de flags para evitar que lixo da tacada anterior atropele a nova
        public void ClearAllSyncShot()
        {
            foreach (var el in Players)
            {
                try
                {
                    if (el == null) continue;

                    InitPlayerInfo("ClearAllsync_shot", "limpando sync shot", el, out PlayerGameInfo pgi);

                    pgi.sync_shot_flag = 0;
                    pgi.tick_sync_shot.clear();
                }
                catch { /* Log omitido para brevidade */ }
            }
        }

        public void ClearAllSyncShot2()
        {
            foreach (var el in Players)
            {
                try
                {
                    if (el == null) continue;

                    InitPlayerInfo("ClearAllsync_shot", "limpando sync shot", el, out PlayerGameInfo pgi);

                    pgi.sync_shot_flag2 = 0;
                }
                catch { /* Log omitido para brevidade */ }
            }
        }

        public void ClearAllInitShot()
        {

            Players.ForEach(el =>
            {
                try
                {
                    InitPlayerInfo("clear_all_init_shot",
                        " tentou limpar all init shot do jogo",
                        el, out PlayerGameInfo pgi);
                    pgi.init_shot = 0;
                }
                catch (exception e)
                {
                    _smp.LogManager.Instance.push(new AppMessage("[StrokeBase::clear_all_ini_shot][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
            });
        }


        public void ClearAllFlagSync()
        {
            ClearAllLoadHole();
            ClearAllFinishCharIntro();
            ClearAllFinishShot();
            ClearAllFinishShot2();
            ClearAllSyncShot();
            ClearAllSyncShot2();
            ClearAllInitShot();
        }

        public bool CheckLimitPlayers()
        {
            return Players.Count > _maxPlayers;
        }
        #endregion

        #region DISPOSE

        private void FinishGameTurn()
        {
            const int ShutdownTimeoutMs = 5000; // 5 segundos de timeout para encerramento gracioso

            try
            {
                if (_checkTurnThread != null)
                {
                    if (_checkTurnEvent != INVALID_HANDLE_VALUE)
                        SetEvent(_checkTurnEvent);

                    // Aguarda com timeout para evitar bloqueio infinito caso a thread
                    // esteja travada (ex: deadlock interno). Se expirar, força o encerramento.
                    if (!_checkTurnThread.waitThreadFinish(ShutdownTimeoutMs))
                    {
                        _smp.LogManager.Instance.push(new AppMessage(
                            $"[StrokeBase::FinishGameTurn][Warning] Thread não encerrou em {ShutdownTimeoutMs}ms — forçando exit.",
                            type_msg.CL_FILE_LOG_AND_CONSOLE));

                        _checkTurnThread.exit_thread();
                    }
                }
            }
            catch (exception ex)
            {
                Console.WriteLine($"[StrokeBase::FinishGameTurn][ErrorSystem] {ex.getFullMessageError()}");
            }

            _checkTurnThread = null;

            if (_checkTurnEvent != INVALID_HANDLE_VALUE)
                CloseHandle(_checkTurnEvent);

            if (_checkTurnPulseEvent != INVALID_HANDLE_VALUE)
                CloseHandle(_checkTurnPulseEvent);

            _checkTurnEvent = IntPtr.Zero;
            _checkTurnPulseEvent = IntPtr.Zero;
        }

        public override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    _treasureHunterInfo.clear();

                    FinishGameTurn();
                }
                catch (exception e)
                {
                    _smp.LogManager.Instance.push(new AppMessage("[StrokeBase::~StrokeBase][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                    if (_checkTurnThread != null)
                    {
                        _checkTurnThread.exit_thread();
                        _checkTurnThread = null;
                    }
                }
            }
            base.Dispose(disposing);
        }
        #endregion
    }
}
