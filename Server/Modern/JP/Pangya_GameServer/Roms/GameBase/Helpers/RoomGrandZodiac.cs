using Pangya_GameServer.Channels;
using Pangya_GameServer.Engine;
using Pangya_GameServer.Feature;
using Pangya_GameServer.Flags;
using Pangya_GameServer.Manager;
using Pangya_GameServer.Models.Game;
using Pangya_GameServer.PacketFunc;
using Pangya_GameServer.Server;
using Pangya_GameServer.Session;
using PangyaAPI.IFF.Flags;
using PangyaAPI.IFF.Handle.JP;
using PangyaAPI.IFF.Regions.JP.Models.IFF;
using PangyaAPI.Network;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using System.Diagnostics;
using static Pangya_GameServer.Models.DefineConstants;
using static PangyaAPI.Utilities.Tools;
namespace Pangya_GameServer.Roms.GameBase.Helpers
{
    public class RoomGrandZodiac : Room
    {
        protected GrandZodiacState GameState;
        protected DateTime CreateTime; 
        protected PangyaSyncTimer TimerCountDown;// Timer de contagem regressiva para come a o do Bot GM Event
        protected IntPtr GameEventWaitStart;//
        protected IntPtr GameEventWaitStartPulse;//trocar, para IntPtr se possivel

        protected List<stReward> Rewards;
        private PangyaThread GameStartThread; // Thread que vai sincronizar o tempo de come a o do Bot GM Event

        // Singleton-like de instâncias
        protected static List<RoomGrandZodiacInstanciaCtx> GameList;
        protected static object m_cs_game = new object();
        public RoomGrandZodiac(Channel _channel_owner, GameRoomInfoModel _ri) : base(_channel_owner, _ri)
        {
            //room logs
            RoomInfoLog.roomId = Guid.Empty;//seta toda vez que inicia sala
            Rewards = [];
            GameState = new GrandZodiacState();
            GameList = [];
            AddRoom(this);

            CreateTime = DateTime.Now;


            // Cria evento que vai para a thread wait time start
            if ((GameEventWaitStart = CreateEvent(IntPtr.Zero,  true, false, null)) == IntPtr.Zero)
            {
                throw new exception("[RoomGrandZodiac::RoomGrandZodiac][Error] ao criar evento wait time start.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM_BOT_GM_EVENT, 1050, GetLastError()));
            }

            // Cria evento que vai pulsar a thread wait time start para ir mais r pido quando um player entrar o sair da sala
            if ((GameEventWaitStartPulse = CreateEvent(IntPtr.Zero,
            true, false, null)) == IntPtr.Zero)
            {
                throw new exception("[RoomGrandZodiac::RoomGrandZodiac][Error] ao criar evento wait time start pulse.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM_BOT_GM_EVENT, 1050, GetLastError()));
            }

            // Cria a thread que vai sincronizar o tempo de come a o Grand Zodiac
            GameStartThread = new PangyaThread(1063 /*Wait Time Start */, obj => WaitTimeStart(), this, ThreadPriority.AboveNormal);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                clear_timer_count_down();
                FinishGameRoom();
                RemoveGame(this);
            }
            base.Dispose(true);
        }

        public override bool IsAllReady()
        {
            return !HaveInvited();
        }

        public bool StartGame()
        {
            var p = new Packet();

            bool ret = true;

            try
            {

                // Verifica se j  tem um jogo inicializado e lan a error se tiver, para o cliente receber uma resposta
                if (CurrentGame != null)
                {
                    throw new exception("[RoomGrandZodiac::startGame][Error] Server tentou comecar o jogo na sala[NUMERO=" + Convert.ToString(RoomInfo.RoomID) + "], mas ja tem um jogo inicializado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM_BOT_GM_EVENT,
                        8, 0x5900202));
                }

                // Verifica se todos est o prontos se n o da erro
                if (!IsAllReady())
                {
                    throw new exception("[RoomGrandZodiac::startGame][Error] Server tentou comecar o jogo na sala[NUMERO=" + Convert.ToString(RoomInfo.RoomID) + ", MASTER=" + Convert.ToString(RoomInfo.OwnerUID) + "], mas nem todos jogadores estao prontos. Hacker ou Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM_BOT_GM_EVENT,
                    8, 0x5900202));
                }

                if (RoomInfo.CourseIndex >= RoomCourseFlags.UNK)
                {

                    // Special Shuffle Course
                    if (RoomInfo.GetRoomType() == RoomTypeFlags.SPECIAL_SHUFFLE_COURSE && RoomInfo.GetHoleType() == RoomHoleType.M_SHUFFLE_COURSE)
                    {

                        RoomInfo.CourseIndex = (RoomCourseFlags)(0x80 | (byte)RoomCourseFlags.CHRONICLE_1_CHAOS);

                    }
                    else
                    { // Random Normal

                        var lottery = new LotterySystem();

                        foreach (var el in sIff.Instance.getCourse())
                        {

                            var course_id = sIff.Instance.getItemIdentify(el.ID);

                            if (course_id != 17 && course_id != 0x40)
                            {
                                lottery.Add(100, course_id);
                            }
                        }

                        var lc = lottery.SpinRoleta();

                        if (lc != null)
                        {
                            SetCourse((byte)(0x80 | Convert.ToByte(lc.Value)));
                        }
                    }
                }

                if (!MakeGameRoom())
                    SendBroadCast(Handle_PACKET_RESPONSE.pacote049(null, TGAME_CREATE_RESULT.CREATE_GAME_CREATE_FAILED));

                // Update Room State
                RoomInfo.StateRoom = 0; // IN GAME

                p.init_plain(0x230);
                SendBroadCast(p);

                p.init_plain(0x231);
                SendBroadCast(p); 

                p.init_plain(0x77); 
                p.WriteInt32(GameServer.Instance.getInfo().Rate.Pang); // Rate Pang
                SendBroadCast(p);

                RoomInfoLog.roomId = Guid.Empty;//seta toda vez que inicia sala


                // Coloca para o thread que cria o tempo sspera o jogo acabar
                GameState.SetStateWithLock(GrandZodiacStateFlag.WAIT_END_GAME);

            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[RoomGrandZodiac::startGame][Error] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                ret = false; // Error ao inicializar o Jogo
            }

            return ret;
        } 

        protected void WaitTimeStart()
        {
            try
            {
                _smp.LogManager.Instance.push(new AppMessage($"[RoomGrandZodiac::waitTimeStart][Log] Sala [ID: {RoomInfo.RoomID}] waitTimeStart iniciado com sucesso!", type_msg.CL_FILE_LOG_AND_CONSOLE));

                uint retWait = WAIT_TIMEOUT;
                IntPtr[] wait_events = { GameEventWaitStart, GameEventWaitStartPulse };


                while ((retWait = WaitForMultipleObjects((uint)wait_events.Length, wait_events, false, 1000 /*1 segundo */)) == WAIT_TIMEOUT || retWait == (WAIT_OBJECT_0 + 1))
                {
                    try
                    {
                        GameState.Lock();

                        switch (GameState.GetState())
                        {
                            case GrandZodiacStateFlag.WAIT_TIME_START:
                                {
                                    // 1. CORREÇÃO: Use TotalMinutes para os 2 minutos de espera inicial
                                    double diffMin = (DateTime.Now - CreateTime).TotalMinutes;
                                    if (TimerCountDown == null && CurrentGame == null)
                                    {
                                        // Espera 2 minutos para começar se tiver pelo menos 1 player
                                        if (diffMin >= 2.0 && Players.Count > 0)
                                        {
                                            InitCountDown(10);
                                            GameState.SetState(GrandZodiacStateFlag.WAIT_10_SECONDS_START);
                                        }
                                        // Ou começa imediatamente se a sala lotar (MaxUsers)
                                        else if (GetCountPlayersWithoutInvited() == RoomInfo.MaxUsers)
                                        {
                                            using (var p = new Packet(0x40))
                                            {
                                                p.WriteByte(12); // Msg: Sala Cheia
                                                p.WriteUInt16(0); p.WriteUInt16(0);
                                                p.WriteUInt32(10); // 10 segundos para começar
                                                SendBroadCast(p);
                                            }

                                            InitCountDown(10);
                                            GameState.SetState(GrandZodiacStateFlag.WAIT_10_SECONDS_START);
                                        }
                                    }
                                }
                                break;

                            case GrandZodiacStateFlag.WAIT_10_SECONDS_START:
                                {
                                    // Se todos saíram da sala enquanto contava os 10 segundos, cancela e volta a esperar
                                    if (CurrentGame == null && Players.Count == 0)
                                    {
                                        GameState.SetState(GrandZodiacStateFlag.WAIT_TIME_START);

                                        _smp.LogManager.Instance.push(new AppMessage($"[GrandZodiac] Sala {RoomInfo.RoomID} ficou vazia. Cancelando contagem.", 1));
                                    }
                                }
                                break;
                            case GrandZodiacStateFlag.WAIT_END_GAME:
                                {

                                }
                                break;
                        }

                        GameState.Unlock();
                    }
                    catch (Exception e)
                    {
                        GameState.Unlock();
                        _smp.LogManager.Instance.push(new AppMessage("[RoomGrandZodiac::waitTimeStart][ErrorSystem] " + e.Message, type_msg.CL_FILE_LOG_AND_CONSOLE));
                    }
                }
            }
            catch (Exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[RoomGrandZodiac::waitTimeStart][ErrorSystem] " + e.Message, type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public int InitCountDown(long _sec_to_start)
        {
            var ret = 0;
            try
            {
                @lock();

                if (_sec_to_start <= 0)
                { // Come a o jogo

                    clear_timer_count_down();
                    if (Players.Count >= 1 && StartGame())
                        GameServer.Instance.sendUpdateRoomInfo(this, 3);
                    else if (Players.Count >= 1)
                        InitCountDown(10);
                    else
                        ret = 1; // Destrói a sala
                }
                else
                {

                    uint wait = 0;
                    uint interval = 0;
                    float diff = 0.0f;

                    int elapsed_sec = (TimerCountDown != null) ? (int)Math.Round(TimerCountDown.getElapsed() / 1000.0f) /*Mili para segundos */ : 0;

                    _sec_to_start -= elapsed_sec;

                    if ((diff = ((_sec_to_start - 10 /*10 segundos */) / 30.0f /* 30 segundos */)) >= 1.0f)
                    { // Intervalo de 30 segundos

                        if ((_sec_to_start % 30) == 0)
                        {

                            // Intervalo
                            interval = (uint)(30 * 1000); // 30 segundos

                            wait = interval * (uint)diff; // 30 * diff minutos em milisegundos

                        }
                        else
                        {

                            // Corrige o tempo para ficar no intervalo certo
                            wait = interval = (uint)((_sec_to_start % 30) * 1000);

                        }

                    }
                    else if ((diff = ((_sec_to_start - 1 /*1 segundo */) / 10.0f /*10 segundos */)) >= 1.0f)
                    { // Intervalo de 10 segundos

                        if ((_sec_to_start % 10) == 0)
                        {

                            // Intervalo
                            interval = (uint)(10 * 1000); // 10 segundos

                            wait = interval * (uint)diff; // 10 * diff segundos em milisegundos

                        }
                        else
                        {

                            // Corrige o tempo para ficar no intervalo certo
                            wait = interval = (uint)((_sec_to_start % 10) * 1000);
                        }

                    }
                    else
                    { // Intervalo de 1 segundo

                        diff = (float)Math.Round(_sec_to_start / 1.0f, MidpointRounding.AwayFromZero);

                        // Intervalo
                        interval = 1000; // 1 segundo

                        wait = interval * (uint)diff; // 1 * diff segundos em milesegundos

                    }

                    // UPDATE ON GAME
                    var p = new Packet(0x40);

                    p.WriteByte(11); // Temporizador Grand Prix e Grand Zodiac

                    p.WriteUInt16(0); // Nick
                    p.WriteUInt16(0); // Msg

                    p.WriteUInt32((uint)_sec_to_start); 
                    SendBroadCast(p);

                    clear_timer_count_down();

                    long next_sec = _sec_to_start - (interval / 1000);

                    TimerCountDown = GameServer.Instance.MakeTimer(
                        wait,
                        new List<long> { interval },
                        () => CountDownTime(this, next_sec)
                    );
                }
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[RoomGrandZodiac::count_down][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
            finally
            {
                unlock(); // Garante a liberação mesmo em caso de erro
            }
            return ret;
        }


        protected static void CountDownTime(object _arg1, object _arg2)
        {
            RoomGrandZodiac rgze = (RoomGrandZodiac)_arg1;
            long sec = (long)_arg2;
            if (rgze != null && InitAndValid(rgze))
                rgze.InitCountDown(sec);
        }

        public void FinishGameRoom()
        {

            try
            {

                if (GameStartThread != null)
                {

                    if (GameEventWaitStart != IntPtr.Zero)
                    {
                        CloseHandle(GameEventWaitStart);
                    }

                    if (GameEventWaitStartPulse != IntPtr.Zero)
                    {
                        CloseHandle(GameEventWaitStartPulse);
                    }

                    GameStartThread.waitThreadFinish(-1);
                }
            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[RoomGrandZodiac::finish_thread_sync_wait_time_start][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if (GameStartThread != null)
                {

                    GameStartThread.exit_thread();

                    GameStartThread = null;
                }
            }

            GameStartThread = null;
            GameEventWaitStart = IntPtr.Zero;
            GameEventWaitStartPulse = IntPtr.Zero;
        }

        public void clear_timer_count_down()
        {
            if (TimerCountDown != null)
            {
                TimerCountDown.Dispose();
                TimerCountDown = null;
            }
        }

        private void send_system_message_room(RoomGrandZodiac roomBotGMEvent, string msgAviso)
        {
            var p = new Packet();
            // Mensagem de erro pro GM
            p.init_plain(0x40);
            p.WriteByte(7);
            p.WriteString("@INI3");
            p.WriteString(msgAviso);
            SendBroadCast(p);
            //packet_func.room_broadcast(roomBotGMEvent, p, 1);
        }

        // --- Singleton/Instancia Helpers ---
        public static void AddRoom(RoomGrandZodiac _rgze)
        {
            lock (m_cs_game)
            {
                if (!GameList.Any(x => x.m_rgze.GetRoomId() == _rgze.GetRoomId()))
                {
                    _smp.LogManager.Instance.push(new AppMessage(
                        "[RoomGrandZodiac::Add][Log] Adicionou Room Grand Zodiac Event no list.",
                        type_msg.CL_FILE_LOG_AND_CONSOLE
                    ));
                    GameList.Add(new RoomGrandZodiacInstanciaCtx(_rgze, 1));
                }
            }
        }

        public static void RemoveGame(RoomGrandZodiac _rgze)
        {
            lock (m_cs_game) GameList.RemoveAll(x => x.m_rgze.GetRoomId() == _rgze.GetRoomId());
        }

        public static bool InitAndValid(RoomGrandZodiac _rgze)
        {
            lock (m_cs_game) return GameList.Any(x => x.m_rgze.GetRoomId() == _rgze.GetRoomId() && x.m_state == 1);
        }

        internal static void Init()
        {
            lock (m_cs_game)
            {
                if (GameList != null && GameList.Count == 0)
                {
                    _smp.LogManager.Instance.push(new AppMessage(
                        "[RoomGrandZodiac::initFirstInstance][Log] Criou primeira instance do Singleton da classe Room Grand Zodiac Event static vector.",
                        type_msg.CL_FILE_LOG_AND_CONSOLE
                    ));
                }
            }
        }
    }

    // Classes auxiliares para manter a semântica
    public class GrandZodiacState
    {
        private GrandZodiacStateFlag _state;
        private object _lock = new object();
        public void Lock() => Monitor.Enter(_lock);
        public void Unlock() => Monitor.Exit(_lock);
        public void SetState(GrandZodiacStateFlag s) => _state = s;
        public void SetStateWithLock(GrandZodiacStateFlag s) { Lock(); _state = s; Unlock(); }
        public GrandZodiacStateFlag GetState() => _state;
    }

    public enum GrandZodiacStateFlag : int
    {
        WAIT_TIME_START,
        WAIT_10_SECONDS_START,
        WAIT_END_GAME
    }

    public class RoomGrandZodiacInstanciaCtx
    {
        public RoomGrandZodiac m_rgze;
        public int m_state;
        public RoomGrandZodiacInstanciaCtx(RoomGrandZodiac r, int s) { m_rgze = r; m_state = s; }
    }
}
