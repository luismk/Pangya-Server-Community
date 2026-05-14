using Pangya_GameServer.Game.GameModes;
using Pangya_GameServer.Models;
using Pangya_GameServer.PacketFunc;
using Pangya_GameServer.UTIL;
using PangyaAPI.IFF.JP.Extensions;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Models;
using PangyaAPI.Utilities.Log;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using static Pangya_GameServer.Models.DefineConstants;
using static PangyaAPI.Utilities.Tools;
namespace Pangya_GameServer.Game.Manager
{
    public class RoomBotGMEvent : Room
    {
        // Fields originais
        protected m_state_rbge m_state_rbge = new m_state_rbge();
        protected DateTime m_create_room;
        protected List<stReward> m_rewards;
        private PangyaThread m_wait_time_start; // Thread que vai sincronizar o tempo de come a o do Bot GM Event
        protected PangyaSyncTimer m_timer_count_down;// Timer de contagem regressiva para come a o do Bot GM Event
        protected IntPtr m_hEvent_wait_start;//
        protected IntPtr m_hEvent_wait_start_pulse;//trocar, para IntPtr se possivel

        protected static List<RoomBotGMEventInstanciaCtx> m_instancias = new List<RoomBotGMEventInstanciaCtx>();
        protected static object m_cs_instancia = new object(); 
        public RoomBotGMEvent(byte _channel_owner, RoomInfoEx _ri, List<stReward> _rewards) : base(_channel_owner, _ri)
        {
            m_rewards = _rewards;

            // Coloca a instância no "vector" estático
            push_instancia(this);

            // Data que a sala foi criada
            m_create_room = DateTime.Now;


            // Coloca o troféu e flags GM
            m_ri.flag_gm = 1;
            m_ri.state_flag = 0x100;
            m_ri.trofel = TROFEL_GM_EVENT_TYPEID;

            // Cria evento que vai para a thread wait time start 
            if ((m_hEvent_wait_start = CreateEvent(IntPtr.Zero, true, false, null)) == IntPtr.Zero)
            {
                throw new exception("[RoomBotGMEvent::RoomBotGMEvent][Error] ao criar evento wait time start.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM_BOT_GM_EVENT, 1050, GetLastError()));
            }

            // Cria evento que vai pulsar a thread wait time start para ir mais r pido quando um player entrar o sair da sala
            if ((m_hEvent_wait_start_pulse = CreateEvent(IntPtr.Zero, true, false, null)) == IntPtr.Zero)
            {
                throw new exception("[RoomBotGMEvent::RoomBotGMEvent][Error] ao criar evento wait time start pulse.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM_BOT_GM_EVENT, 1050, GetLastError()));
            }

            // Cria a thread que vai sincronizar o tempo de come a o Bot GM Event
            m_wait_time_start = new PangyaThread(1063 /*Wait Time Start */, obj => waitTimeStart(), this, ThreadPriority.AboveNormal);

        }

        protected override void Dispose(bool actived)
        {
            if (actived)
            {
                m_rewards?.Clear();

                clear_timer_count_down();

                // Finish Thread Sync wait time start
                finish_thread_sync_wait_time_start();

                // Tira a instância da lista
                pop_instancia(this);
            }
            base.Dispose(true);
        }

        public override bool isAllReady()
        {
            // Verifica se não tem nenhum convidado na sala
            return !_haveInvited();
        }

        public bool startGame()
        {
           var p = new PangyaBinaryWriter();
            bool ret = true;

            try
            {
                if (m_pGame != null)
                    throw new Exception($"[RoomBotGMEvent::startGame][Error] Server tentou comecar o jogo na sala[NUMERO={m_ri.numero}], mas ja tem um jogo inicializado.");

                if (!isAllReady())
                    throw new Exception($"[RoomBotGMEvent::startGame][Error] Server tentou comecar o jogo na sala[NUMERO={m_ri.numero}], mas nem todos jogadores estao prontos.");

                // Lógica de Course
                if (m_ri.getMap() >= 0x7F)
                {
                    if (m_ri.getTipo() == RoomInfo.ROOM_INFO_TYPE.SPECIAL_SHUFFLE_COURSE && m_ri.getModo() == RoomInfo.ROOM_INFO_MODO.M_SHUFFLE_COURSE)
                    {
                        m_ri.course = (RoomInfo.ROOM_INFO_COURSE)(byte)(0x80 | (byte)RoomInfo.ROOM_INFO_COURSE.CHRONICLE_1_CHAOS);
                    }
                    else
                    {
                        Lottery lottery = new Lottery((ulong)this.GetHashCode());
                        foreach (var el in sIff.getInstance().getCourse())
                        {
                            var course_id = sIff.getInstance().getItemIdentify(el.ID);
                            if (course_id != 17 && course_id != 0x40)
                                lottery.push(100, course_id);
                        }

                        var lc = lottery.spinRoleta();
                        if (lc != null)
                            setCourse((byte)(0x80 | Convert.ToByte(lc.Value)));
                    }
                }

                RateValue rv = new RateValue();
                m_ri.angel_event = sgs.gs.getInstance().getInfo().rate.angel_event == 1;

                rv.clubset = (uint)sgs.gs.getInstance().getInfo().rate.club_mastery;
                rv.rain = (uint)sgs.gs.getInstance().getInfo().rate.chuva;
                rv.treasure = (uint)sgs.gs.getInstance().getInfo().rate.treasure;

                rv.persist_rain = 0; // Persist rain type isso   feito na classe game


                switch (m_ri.getTipo())
                {
                    case RoomInfo.ROOM_INFO_TYPE.TOURNEY:
                        m_pGame = new Tourney(v_sessions, m_ri, rv, m_ri.channel_rookie);
                        break;
                    default:
                        throw new Exception($"[RoomBotGMEvent::startGame][Error] Tipo da sala nao eh Tourney na sala {m_ri.numero}");
                }

                // Se tiver apenas 1 player, adiciona o Bot Visual
                if (v_sessions.Count == 1)
                    addBotVisual(v_sessions.First());

                m_ri.state = 0; // IN GAME 

                p.init_plain((ushort)0x230);

                packet_func.room_broadcast(this, p, 1);

                p.init_plain((ushort)0x231);

                packet_func.room_broadcast(this, p, 1);

                uint rate_pang = (uint)sgs.gs.getInstance().getInfo().rate.pang;

                p.init_plain((ushort)0x77);

                p.WriteUInt32(rate_pang); // Rate Pang

                packet_func.room_broadcast(this, p, 1);


                m_room_log.roomId = Guid.Empty;//seta toda vez que inicia sala

                //insert dados do player
                foreach (var _sessions in v_sessions)
                {
                    CreateRoomLogSql(_sessions);//criar de todos 
                    //set room 
                    _sessions.m_room = this;
                    //set game 
                    _sessions.m_pGame = m_pGame;
                }
                // Coloca para o thread que cria o tempo sspera o jogo acabar
                m_state_rbge.setStateWithLock(eSTATE_ROOM_BOT_GM_EVENT_SYNC.WAIT_END_GAME); 
            }
            catch (Exception e)
            {
                _smp.message_pool.getInstance().push(new message("[RoomBotGMEvent::startGame][Error] " + e.Message, type_msg.CL_FILE_LOG_AND_CONSOLE));
                ret = false;
            }

            return ret;
        }

        protected static void _waitTimeStart(object lpParameter)
        {
            RoomBotGMEvent pTP = (RoomBotGMEvent)lpParameter;
            pTP.waitTimeStart();
        }

        protected void waitTimeStart()
        {
            try
            {
                _smp.message_pool.getInstance().push(new message($"[RoomBotGMEvent::waitTimeStart][Log] Sala [ID: {m_ri.numero}] waitTimeStart iniciado com sucesso!", type_msg.CL_FILE_LOG_AND_CONSOLE));
                uint retWait = WAIT_TIMEOUT;
                IntPtr[] wait_events = { m_hEvent_wait_start, m_hEvent_wait_start_pulse };

                while ((retWait = WaitForMultipleObjects((uint)wait_events.Length, wait_events, false, 1000 /*1 segundo*/)) == WAIT_TIMEOUT || retWait == (WAIT_OBJECT_0 + 1))
                { 
                    try
                    {
                        m_state_rbge.lock_state();

                        switch (m_state_rbge.getState())
                        {
                            case eSTATE_ROOM_BOT_GM_EVENT_SYNC.WAIT_TIME_START:
                                {
                                    double tempoCriadaMin = (DateTime.Now - m_create_room).TotalMinutes;
                                       
                                    if (m_timer_count_down == null)
                                    {
                                        // 2. GATILHO DE INÍCIO (2 MINUTOS)
                                        if (tempoCriadaMin >= 2)
                                        {
                                            count_down(10);
                                            m_state_rbge.setState(eSTATE_ROOM_BOT_GM_EVENT_SYNC.WAIT_10_SECONDS_START);
                                        }
                                        // 3. GATILHO DE INÍCIO (SALA CHEIA)
                                        else if (_getRealNumPlayersWithoutInvited() == m_ri.max_player)
                                        {
                                            var p = new PangyaBinaryWriter(0x40);
                                            p.WriteByte(12); // Msg: Sala cheia
                                            p.WriteUInt16(0); p.WriteUInt16(0);
                                            p.WriteUInt32(10); // 10 segundos
                                            packet_func.room_broadcast(this, p, 1);

                                            count_down(10);
                                            m_state_rbge.setState(eSTATE_ROOM_BOT_GM_EVENT_SYNC.WAIT_10_SECONDS_START);
                                        }
                                    }
                                }
                                break;

                            case eSTATE_ROOM_BOT_GM_EVENT_SYNC.WAIT_END_GAME:
                                { 
                                }
                                break;
                        }

                        m_state_rbge.unlock_state();
                    }
                    catch (Exception e)
                    {
                        m_state_rbge.unlock_state();
                        _smp.message_pool.getInstance().push(new message("[RoomBotGMEvent::waitTimeStart][ErrorSystem] " + e.Message, type_msg.CL_FILE_LOG_AND_CONSOLE));
                    }
                }
            }
            catch (Exception e)
            {
                _smp.message_pool.getInstance().push(new message("[RoomBotGMEvent::waitTimeStart][ErrorSystem] " + e.Message, type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
            _smp.message_pool.getInstance().push(new message("[RoomBotGMEvent::waitTimeStart][Log] Saindo de waitTimeStart()...", type_msg.CL_FILE_LOG_AND_CONSOLE));
        }

        private void send_system_message_room(RoomBotGMEvent roomBotGMEvent, string msgAviso)
        {
            var p = new PangyaBinaryWriter();
            // Mensagem de erro pro GM
            p.init_plain(0x40);
            p.WriteByte(7);
            p.WriteString("@INI3");
            p.WriteString(msgAviso);
            packet_func.room_broadcast(roomBotGMEvent, p, 1);
        }

        protected static void _count_down_time(object _arg1, object _arg2)
        {
            RoomBotGMEvent rbge = (RoomBotGMEvent)_arg1;
            long sec_to_start = (long)_arg2;

            try
            {
                if (rbge != null && instancia_valid(rbge))
                {
                    if (rbge.count_down(sec_to_start) == 1)
                        sgs.gs.getInstance().destroyRoom(rbge.m_channel_owner, (short)rbge.m_ri.numero);
                }
            }
            catch (Exception e)
            {
                _smp.message_pool.getInstance().push(new message("[RoomBotGMEvent::_count_down_time][ErrorSystem] " + e.Message, type_msg.CL_FILE_LOG_AND_CONSOLE));
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
                _smp.message_pool.getInstance().push(new message("[RoomBotGMEvent::count_down][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
            finally
            {
                unlock(); // Garante a liberação mesmo em caso de erro
            }
            return ret;
        }

        public void finish_thread_sync_wait_time_start()
        {
            try
            {

                if (m_wait_time_start != null)
                {
                    m_wait_time_start.waitThreadFinish(-1);

                    if (m_hEvent_wait_start != IntPtr.Zero)
                    {
                        CloseHandle(m_hEvent_wait_start);
                    }

                    if (m_hEvent_wait_start_pulse != IntPtr.Zero)
                    {
                        CloseHandle(m_hEvent_wait_start_pulse);
                    }
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[RoomBotGMEvent::finish_thread_sync_wait_time_start][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

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
            m_timer_count_down?.Dispose();
            m_timer_count_down = null;
        }

        public override void finish_game()
        {

            if (m_pGame != null)
            { 
                // Envia presentes aqui, mas tem que ter jogadores no vector do game
                if (m_pGame.getSessions().Count == 0 || m_rewards.Count == 0)
                { 
                    base.finish_game(); 
                    return;
                }
                 

                // Envia os presentes aqui
                Func<uint, string> getItemName = (_typeid) =>
                {
                    string ret = "";

                    var @base = sIff.getInstance().findCommomItem(_typeid);

                    if (@base != null && !string.IsNullOrEmpty(@base.Name))
                    {
                        ret = @base.Name;
                    }

                    return ret;
                };


                var reward_str = new StringBuilder("{");

                for (int i = 0; i < m_rewards.Count; i++)
                {
                    reward_str.Append("[");
                    reward_str.Append(m_rewards[i].ToString());
                    reward_str.Append("]");

                    // Adiciona a vírgula apenas se NÃO for o último item
                    if (i < m_rewards.Count - 1)
                    {
                        reward_str.Append(", ");
                    }
                }

                reward_str.Append("}"); 

                try
                {

                    List<stItem> v_item = new List<stItem>();
                    stItem item = new stItem();
                    BuyItem bi = new BuyItem();

                    Player p = null;

                    foreach (var el_p in v_sessions)
                    {

                        // Limpa, por que   por jogador
                        v_item.Clear();

                        if (el_p == null || (p = sgs.gs.getInstance().findPlayer(el_p.m_pi.uid)) == null)
                        {

                            // Log, Player que ganhou n o est  mais online, vai ficar sem o item
                            _smp.message_pool.getInstance().push(new message("[RoomBotGMEvent::finish_game][WARNING] Player[UID=" + (el_p == null ? "UNKNOWN" : Convert.ToString(el_p.m_pi.uid)) + "] ganhou o item(ns)" + reward_str + ", mas saiu antes dos pr mios ser entregues, vai ficar sem o pr mio.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            continue;
                        }

                        // Initialize itens
                        foreach (var el_r in m_rewards)
                        {

                            // Limpa
                            bi = new BuyItem();
                            item = new stItem();

                            // Initialize
                            bi.id = -1;
                            bi._typeid = el_r._typeid;
                            bi.qntd = el_r.qntd;
                            if (el_r.qntd_time > 0)
                            {
                                bi.time = (short)el_r.qntd_time;
                            }

                            ItemManager.initItemFromBuyItem(p.m_pi, item, bi, false, 0, 0, 1 /*~nao Check Level */);

                            if (item._typeid == 0)
                            {
                                _smp.message_pool.getInstance().push(new message("[RoomBotGMEvent::finish_game][Error][WARNING] tentou enviar o reward para o player[UID=" + Convert.ToString(p.m_pi.uid) + "] o Item[" + el_r.ToString() + "], mas nao conseguiu inicializar o item. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                                return;
                            }

                            v_item.Add(item);
                        }

                        var msg = ("Bot GM Event(" + m_create_room + "): item[ " + (v_item.Count == 0 ? "NONE" : getItemName(v_item[0]._typeid)) + " ]");

                        if (v_item.Count == 0 || MailBoxManager.sendMessageWithItem(0, p.m_pi.uid, msg, v_item) <= 0)
                        {
                            _smp.message_pool.getInstance().push(new message("[RoomBotGMEvent::finish_game][Error][WARNING] tentou enviar reward para o player[UID=" + Convert.ToString(p.m_pi.uid) + "] o Item(ns)" + reward_str + ", mas nao conseguiu colocar o item no mail box dele. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                        }

                        // Log
                        _smp.message_pool.getInstance().push(new message("[RoomBotGMEvent::finish_game][Log] Player[UID=" + Convert.ToString(p.m_pi.uid) + "] ganhou no Bot GM Event(" + UtilTime._formatDate(m_create_room) + "): Item(" + Convert.ToString(m_rewards.Count) + ")" + reward_str, type_msg.CL_FILE_LOG_AND_CONSOLE));
                    }

                }
                catch (exception e)
                { 
                    _smp.message_pool.getInstance().push(new message("[RoomBotGMEvent::finish_game][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                }

                // 2min para destruir a sala 
                Action<object, object> destroyRoomTimer = (object _arg1, object _arg2) =>
                {
                    RoomBotGMEvent rbge = (RoomBotGMEvent)(_arg1);
                    long sec_to_start = (long)(_arg2);

                    try
                    { 
                        if (rbge != null && instancia_valid(rbge))
                        {
                            sgs.gs.getInstance().destroyRoom(rbge.m_channel_owner, (short)rbge.m_ri.numero); // Destroi a sala, se n o tem players, ou n o conseguiu inicializar
                        }
                    }
                    catch (exception e)
                    {

                        _smp.message_pool.getInstance().push(new message("[RoomBotGMEvent::lambda(destroyRoom)][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                    }
                };

                // Se o Shutdown Timer estiver criado descria e cria um novo
                clear_timer_count_down();
                // Cria novo timer baseado no tempo do modo 
                m_timer_count_down = sgs.gs.getInstance().MakeTime(2 * 60000, () => destroyRoomTimer(this, 0));
            }
        }

        protected override bool isDropRoom() => false;

        // --- Singleton/List Management ---
        public static void push_instancia(RoomBotGMEvent _rgze)
        {
            lock (m_cs_instancia)
            {
                if (!m_instancias.Any(x => x.m_rbge.getNumero() == _rgze.getNumero()))
                {
                    _smp.message_pool.getInstance().push(new message(
                        "[RoomGrandZodiacEvent::Add][Log] Adicionou Room Grand Zodiac Event no list.",
                        type_msg.CL_FILE_LOG_AND_CONSOLE
                    ));
                    m_instancias.Add(new RoomBotGMEventInstanciaCtx(_rgze, 1));
                }
            }
        }

        public static void pop_instancia(RoomBotGMEvent _rgze)
        {
            lock (m_cs_instancia) m_instancias.RemoveAll(x => x.m_rbge.getNumero() == _rgze.getNumero());
        }

        public static bool instancia_valid(RoomBotGMEvent _rgze)
        {
            lock (m_cs_instancia) return m_instancias.Any(x => x.m_rbge.getNumero() == _rgze.getNumero() && x.m_state == 1);
        }
         
        public static void initFirstInstance()
        {
            lock (m_cs_instancia)
            {
                if (m_instancias != null && m_instancias.Count == 0)
                {
                    _smp.message_pool.getInstance().push(new message("[RoomBotGMEvent::initFirstInstance][Log] Criou primeira instance do Singleton da classe Room Bot GM Event static vector.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
            }
        }
    }

    // --- Sub-classes e Enums Helpers ---
    public class m_state_rbge
    {
        private eSTATE_ROOM_BOT_GM_EVENT_SYNC _state;
        private object _lock = new object();
        public void lock_state() => Monitor.Enter(_lock);
        public void unlock_state() => Monitor.Exit(_lock);
        public void setState(eSTATE_ROOM_BOT_GM_EVENT_SYNC s) => _state = s;
        public void setStateWithLock(eSTATE_ROOM_BOT_GM_EVENT_SYNC s) { lock_state(); _state = s; unlock_state(); }
        public eSTATE_ROOM_BOT_GM_EVENT_SYNC getState() => _state;
    }

    public enum eSTATE_ROOM_BOT_GM_EVENT_SYNC : int
    {
        WAIT_TIME_START,
        WAIT_10_SECONDS_START,
        WAIT_END_GAME
    }

    public class RoomBotGMEventInstanciaCtx
    {
        public RoomBotGMEvent m_rbge;
        public int m_state;
        public RoomBotGMEventInstanciaCtx(RoomBotGMEvent r, int s) { m_rbge = r; m_state = s; }
    }
}