using Pangya_GameServer.Game.GameModes;
using Pangya_GameServer.Game.Manager;
using Pangya_GameServer.Models;
using Pangya_GameServer.PacketFunc;
using Pangya_GameServer.UTIL;
using PangyaAPI.IFF.JP.Extensions;
using PangyaAPI.Network.PangyaPacket;
using PangyaAPI.Network.PangyaSession;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Models;
using PangyaAPI.Utilities.Log;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using static Pangya_GameServer.Models.DefineConstants;
using static PangyaAPI.Utilities.Tools;
namespace Pangya_GameServer.Game.Manager
{
    public class RoomGrandZodiacEvent : Room
    {
        // Fields (Exatamente como no .cpp)
        protected m_state_rgze m_state_rgze = new m_state_rgze();
        protected DateTime m_create_room; // SYSTEMTIME m_create_room

        protected PangyaSyncTimer m_timer_count_down;// Timer de contagem regressiva para come a o do Bot GM Event
        protected IntPtr m_hEvent_wait_start;//
        protected IntPtr m_hEvent_wait_start_pulse;//trocar, para IntPtr se possivel

        protected List<stReward> m_rewards = new List<stReward>();
        private PangyaThread m_wait_time_start; // Thread que vai sincronizar o tempo de come a o do Bot GM Event

        // Singleton-like de instâncias
        protected static List<RoomGrandZodiacEventInstanciaCtx> m_instancias = new List<RoomGrandZodiacEventInstanciaCtx>();
        protected static object m_cs_instancia = new object();

        public RoomGrandZodiacEvent(byte _channel_owner, RoomInfoEx _ri) : base(_channel_owner, _ri)
        {
            //room logs
            m_room_log.roomId = Guid.Empty;//seta toda vez que inicia sala

            push_instancia(this);
             
            m_create_room = DateTime.Now;


            // Cria evento que vai para a thread wait time start
            if ((m_hEvent_wait_start = CreateEvent(IntPtr.Zero,
    true, false, null)) == IntPtr.Zero)
            {
                throw new exception("[RoomBotGMEvent::RoomBotGMEvent][Error] ao criar evento wait time start.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM_BOT_GM_EVENT, 1050, GetLastError()));
            }

            // Cria evento que vai pulsar a thread wait time start para ir mais r pido quando um player entrar o sair da sala
            if ((m_hEvent_wait_start_pulse = CreateEvent(IntPtr.Zero,
            true, false, null)) == IntPtr.Zero)
            {
                throw new exception("[RoomBotGMEvent::RoomBotGMEvent][Error] ao criar evento wait time start pulse.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM_BOT_GM_EVENT, 1050, GetLastError()));
            }

            // Cria a thread que vai sincronizar o tempo de come a o Grand Zodiac
            m_wait_time_start = new PangyaThread(1063 /*Wait Time Start */, obj => waitTimeStart(), this, ThreadPriority.AboveNormal);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                clear_timer_count_down();
                finish_thread_sync_wait_time_start();
                pop_instancia(this);
            }
            base.Dispose(true);
        }

        public override bool isAllReady()
        {
            return !_haveInvited();
        }

        public bool startGame()
        {
            var p = new PangyaBinaryWriter();

            bool ret = true;

            try
            {

                // Verifica se j  tem um jogo inicializado e lan a error se tiver, para o cliente receber uma resposta
                if (m_pGame != null)
                {
                    throw new exception("[RoomGrandZodiacEvent::startGame][Error] Server tentou comecar o jogo na sala[NUMERO=" + Convert.ToString(m_ri.numero) + "], mas ja tem um jogo inicializado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM_BOT_GM_EVENT,
                        8, 0x5900202));
                }

                // Verifica se todos est o prontos se n o da erro
                if (!isAllReady())
                {
                    throw new exception("[RoomGrandZodiacEvent::startGame][Error] Server tentou comecar o jogo na sala[NUMERO=" + Convert.ToString(m_ri.numero) + ", MASTER=" + Convert.ToString(m_ri.master) + "], mas nem todos jogadores estao prontos. Hacker ou Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM_BOT_GM_EVENT,
                    8, 0x5900202));
                }

                if (m_ri.course >= RoomInfo.ROOM_INFO_COURSE.UNK)
                {

                    // Special Shuffle Course
                    if (m_ri.getTipo() == RoomInfo.ROOM_INFO_TYPE.SPECIAL_SHUFFLE_COURSE && m_ri.getModo() == RoomInfo.ROOM_INFO_MODO.M_SHUFFLE_COURSE)
                    {

                        m_ri.course = (RoomInfo.ROOM_INFO_COURSE)(0x80 | (byte)RoomInfo.ROOM_INFO_COURSE.CHRONICLE_1_CHAOS);

                    }
                    else
                    { // Random normal

                        Lottery lottery = new Lottery();

                        foreach (var el in sIff.getInstance().getCourse())
                        {

                            var course_id = sIff.getInstance().getItemIdentify(el.ID);

                            if (course_id != 17 && course_id != 0x40)
                            {
                                lottery.Push(100, course_id);
                            }
                        }

                        var lc = lottery.spinRoleta();

                        if (lc != null)
                        {
                            setCourse((byte)(0x80 | Convert.ToByte(lc.Value)));
                        }
                    }
                }

                RateValue rv = new RateValue
                {
                    exp = m_ri.rate_exp = (uint)sgs.gs.getInstance().getInfo().rate.exp,
                    pang = m_ri.rate_pang = (uint)sgs.gs.getInstance().getInfo().rate.pang
                };

                // Angel Event
                m_ri.angel_event = sgs.gs.getInstance().getInfo().rate.angel_event == 1;

                rv.clubset = (uint)sgs.gs.getInstance().getInfo().rate.club_mastery;
                rv.rain = (uint)sgs.gs.getInstance().getInfo().rate.chuva;
                rv.treasure = (uint)sgs.gs.getInstance().getInfo().rate.treasure;

                rv.persist_rain = 0; // Persist rain type isso   feito na classe game

                switch (m_ri.getTipo())
                {
                    case RoomInfo.ROOM_INFO_TYPE.GRAND_ZODIAC_INT:
                    case RoomInfo.ROOM_INFO_TYPE.GRAND_ZODIAC_ADV:
                        m_pGame = new GrandZodiac(v_sessions,
                            m_ri, rv, m_ri.channel_rookie);
                        break;
                    default:
                        throw new exception("[RoomGrandZodiacEvent::startGame][Error] Server tentou comecar o jogo na sala[NUMERO=" + Convert.ToString(m_ri.numero) + ", MASTER=" + Convert.ToString(m_ri.master) + "], mas o tipo da sala nao é Tourney. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM_BOT_GM_EVENT,
                            9, 0x5900202));
                }

                // Update Room State
                m_ri.state = 0; // IN GAME

                p.init_plain(0x230);

                packet_func.room_broadcast(this,
                    p, 1);

                p.init_plain(0x231);

                packet_func.room_broadcast(this,
                    p, 1);

                uint rate_pang = (uint)sgs.gs.getInstance().getInfo().rate.pang;

                p.init_plain(0x77);

                p.WriteUInt32(rate_pang); // Rate Pang

                packet_func.room_broadcast(this,
                    p, 1);

                m_room_log.roomId = Guid.Empty;//seta toda vez que inicia sala
                                               //insert dados do player
                foreach (var _sessions in v_sessions)
                {
                    CreateRoomLogSql(_sessions);//criar de todos

                    _sessions.m_pGame = m_pGame;//gera a sala
                }

                // Coloca para o thread que cria o tempo sspera o jogo acabar
                m_state_rgze.setStateWithLock(eSTATE_ROOM_GRAND_ZODIAC_EVENT_SYNC.WAIT_END_GAME);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[RoomGrandZodiacEvent::startGame][Error] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                ret = false; // Error ao inicializar o Jogo
            }

            return ret;
        }

        protected static void _waitTimeStart(object lpParameter)
        {
            RoomGrandZodiacEvent pTP = (RoomGrandZodiacEvent)lpParameter;
            pTP.waitTimeStart();
        }

        protected void waitTimeStart()
        {
            try
            {
                _smp.message_pool.getInstance().push(new message($"[RoomGrandZodiacEvent::waitTimeStart][Log] Sala [ID: {m_ri.numero}] waitTimeStart iniciado com sucesso!", type_msg.CL_FILE_LOG_AND_CONSOLE));

                uint retWait = WAIT_TIMEOUT;
                IntPtr[] wait_events = { m_hEvent_wait_start, m_hEvent_wait_start_pulse };


                while ((retWait = WaitForMultipleObjects((uint)wait_events.Length, wait_events, false, 1000 /*1 segundo */)) == WAIT_TIMEOUT || retWait == (WAIT_OBJECT_0 + 1))
                {
                    try
                    {
                        m_state_rgze.lock_state();

                        switch (m_state_rgze.getState())
                        {
                            case eSTATE_ROOM_GRAND_ZODIAC_EVENT_SYNC.WAIT_TIME_START:
                                {
                                    // 1. CORREÇÃO: Use TotalMinutes para os 2 minutos de espera inicial
                                    double diffMin = (DateTime.Now - m_create_room).TotalMinutes;                                      
                                    if (m_timer_count_down == null && m_pGame == null)
                                    {
                                        // Espera 2 minutos para começar se tiver pelo menos 1 player
                                        if (diffMin >= 2.0 && v_sessions.Count > 0)
                                        {
                                            count_down(10);
                                            m_state_rgze.setState(eSTATE_ROOM_GRAND_ZODIAC_EVENT_SYNC.WAIT_10_SECONDS_START);
                                        }
                                        // Ou começa imediatamente se a sala lotar (max_player)
                                        else if (_getRealNumPlayersWithoutInvited() == m_ri.max_player)
                                        {
                                            using (var p = new PangyaBinaryWriter(0x40))
                                            {
                                                p.WriteByte(12); // Msg: Sala Cheia
                                                p.WriteUInt16(0); p.WriteUInt16(0);
                                                p.WriteUInt32(10); // 10 segundos para começar
                                                packet_func.room_broadcast(this, p, 1);
                                            }

                                            count_down(10);
                                            m_state_rgze.setState(eSTATE_ROOM_GRAND_ZODIAC_EVENT_SYNC.WAIT_10_SECONDS_START);
                                        }
                                    }
                                }
                                break;

                            case eSTATE_ROOM_GRAND_ZODIAC_EVENT_SYNC.WAIT_10_SECONDS_START:
                                {
                                    // Se todos saíram da sala enquanto contava os 10 segundos, cancela e volta a esperar
                                    if (m_pGame == null && v_sessions.Count == 0)
                                    { 
                                        m_state_rgze.setState(eSTATE_ROOM_GRAND_ZODIAC_EVENT_SYNC.WAIT_TIME_START);

                                        _smp.message_pool.getInstance().push(new message($"[GrandZodiac] Sala {m_ri.numero} ficou vazia. Cancelando contagem.", 1));
                                    }
                                }
                                break;
                            case eSTATE_ROOM_GRAND_ZODIAC_EVENT_SYNC.WAIT_END_GAME:
                                {
                                    
                                }
                                break;
                        }

                        m_state_rgze.unlock_state();
                    }
                    catch (Exception e)
                    {
                        m_state_rgze.unlock_state();
                        _smp.message_pool.getInstance().push(new message("[RoomGrandZodiacEvent::waitTimeStart][ErrorSystem] " + e.Message, type_msg.CL_FILE_LOG_AND_CONSOLE));
                    }
                }
            }
            catch (Exception e)
            {
                _smp.message_pool.getInstance().push(new message("[RoomGrandZodiacEvent::waitTimeStart][ErrorSystem] " + e.Message, type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public int count_down(long _sec_to_start)
        {
            var ret = 0;
            try
            {
                @lock();

                if (_sec_to_start <= 0)
                { // Come a o jogo

                    clear_timer_count_down();
                    if (v_sessions.Count >= 1 && startGame())
                        sgs.gs.getInstance().sendUpdateRoomInfo(this, 3);
                    else if (v_sessions.Count >= 1)
                        count_down(10);
                    else
                        ret = 1; // Destrói a sala
                }
                else
                {

                    uint wait = 0;
                    uint interval = 0;
                    float diff = 0.0f;

                    int elapsed_sec = (m_timer_count_down != null) ? (int)Math.Round(m_timer_count_down.getElapsed() / 1000.0f) /*Mili para segundos */ : 0;

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
                    var p = new PangyaBinaryWriter((ushort)0x40);

                    p.WriteByte(11); // Temporizador Grand Prix e Grand Zodiac

                    p.WriteUInt16(0u); // Nick
                    p.WriteUInt16(0u); // Msg

                    p.WriteUInt32((uint)_sec_to_start);

                    packet_func.room_broadcast(this, p, 1);

                    clear_timer_count_down();

                    long next_sec = _sec_to_start - (interval / 1000);

                    m_timer_count_down = sgs.gs.getInstance().MakeTime(
                        wait,
                        new List<long> { interval },
                        () => _count_down_time(this, next_sec)
                    );
                }
            }
            catch (exception e)
            {
                _smp.message_pool.getInstance().push(new message("[RoomGrandZodiacEvent::count_down][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
            finally
            {
                unlock(); // Garante a liberação mesmo em caso de erro
            }
            return ret;
        }


        protected static void _count_down_time(object _arg1, object _arg2)
        {
            RoomGrandZodiacEvent rgze = (RoomGrandZodiacEvent)_arg1;
            long sec = (long)_arg2;
            if (rgze != null && instancia_valid(rgze))
                rgze.count_down(sec);
        }

        public void finish_thread_sync_wait_time_start()
        {

            try
            {

                if (m_wait_time_start != null)
                {

                    if (m_hEvent_wait_start != IntPtr.Zero)
                    {
                        CloseHandle(m_hEvent_wait_start);
                    }

                    if (m_hEvent_wait_start_pulse != IntPtr.Zero)
                    {
                        CloseHandle(m_hEvent_wait_start_pulse);
                    }

                    m_wait_time_start.waitThreadFinish(-1);
                }
            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[RoomGrandZodiacEvent::finish_thread_sync_wait_time_start][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if (m_wait_time_start != null)
                {

                    m_wait_time_start.exit_thread();

                    m_wait_time_start = null;
                }
            }

            m_wait_time_start = null;
            m_hEvent_wait_start = IntPtr.Zero;
            m_hEvent_wait_start_pulse = IntPtr.Zero;
        }

        public void clear_timer_count_down()
        {
            if (m_timer_count_down != null)
            {
                m_timer_count_down.Dispose();
                m_timer_count_down = null;
            }
        }

        private void send_system_message_room(RoomGrandZodiacEvent roomBotGMEvent, string msgAviso)
        {
            var p = new PangyaBinaryWriter();
            // Mensagem de erro pro GM
            p.init_plain(0x40);
            p.WriteByte(7);
            p.WriteString("@INI3");
            p.WriteString(msgAviso);
            packet_func.room_broadcast(roomBotGMEvent, p, 1);
        }

        // --- Singleton/Instancia Helpers ---
        public static void push_instancia(RoomGrandZodiacEvent _rgze)
        {
            lock (m_cs_instancia)
            {
                if (!m_instancias.Any(x => x.m_rgze.getNumero() == _rgze.getNumero()))
                {
                    _smp.message_pool.getInstance().push(new message(
                        "[RoomGrandZodiacEvent::Add][Log] Adicionou Room Grand Zodiac Event no list.",
                        type_msg.CL_FILE_LOG_AND_CONSOLE
                    ));
                    m_instancias.Add(new RoomGrandZodiacEventInstanciaCtx(_rgze, 1));
                }
            }
        }

        public static void pop_instancia(RoomGrandZodiacEvent _rgze)
        {
            lock (m_cs_instancia) m_instancias.RemoveAll(x => x.m_rgze.getNumero() == _rgze.getNumero());
        }

        public static bool instancia_valid(RoomGrandZodiacEvent _rgze)
        {
            lock (m_cs_instancia) return m_instancias.Any(x => x.m_rgze.getNumero() == _rgze.getNumero() && x.m_state == 1);
        }

        internal static void initFirstInstance()
        {
            lock (m_cs_instancia)
            {
                if (m_instancias != null && m_instancias.Count == 0)
                {
                    _smp.message_pool.getInstance().push(new message(
                        "[RoomGrandZodiacEvent::initFirstInstance][Log] Criou primeira instance do Singleton da classe Room Grand Zodiac Event static vector.",
                        type_msg.CL_FILE_LOG_AND_CONSOLE
                    ));
                }
            }
        }
    }

    // Classes auxiliares para manter a semântica
    public class m_state_rgze
    {
        private eSTATE_ROOM_GRAND_ZODIAC_EVENT_SYNC _state;
        private object _lock = new object();
        public void lock_state() => Monitor.Enter(_lock);
        public void unlock_state() => Monitor.Exit(_lock);
        public void setState(eSTATE_ROOM_GRAND_ZODIAC_EVENT_SYNC s) => _state = s;
        public void setStateWithLock(eSTATE_ROOM_GRAND_ZODIAC_EVENT_SYNC s) { lock_state(); _state = s; unlock_state(); }
        public eSTATE_ROOM_GRAND_ZODIAC_EVENT_SYNC getState() => _state;
    }

    public enum eSTATE_ROOM_GRAND_ZODIAC_EVENT_SYNC : int
    {
        WAIT_TIME_START,
        WAIT_10_SECONDS_START,
        WAIT_END_GAME
    }

    public class RoomGrandZodiacEventInstanciaCtx
    {
        public RoomGrandZodiacEvent m_rgze;
        public int m_state;
        public RoomGrandZodiacEventInstanciaCtx(RoomGrandZodiacEvent r, int s) { m_rgze = r; m_state = s; }
    }
}
