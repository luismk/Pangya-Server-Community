using Pangya_GameServer.Channels;
using Pangya_GameServer.Engine;
using Pangya_GameServer.Flags;
using Pangya_GameServer.Models.Game;
using Pangya_GameServer.PacketFunc;
using Pangya_GameServer.Server;
using Pangya_GameServer.Session;
using PangyaAPI.IFF.Handle.JP;
using PangyaAPI.IFF.Regions.JP.Models.IFF;
using PangyaAPI.Network;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;

namespace Pangya_GameServer.Roms.GameBase.Helpers
{
    public class RoomGrandPrix : Room
    {

        #region FIELDS 
        private SystemTime m_start;
        private SystemTime m_now;
        private GrandPrixData m_gp = new GrandPrixData();
        private PangyaSyncTimer m_count_down; 
        #endregion

        public class m_cs_instancia : Singleton<CriticalSectionInstancia>
        {
        }

        public class m_instancias : Singleton<List<RoomGrandPrixInstanciaCtx>>
        {
        }
        #region CONSTRUTOR
        public RoomGrandPrix(Channel _channel_owner, GameRoomInfoModel _ri, GrandPrixData _gp) : base(_channel_owner, _ri)
        {
            this.m_gp = _gp;
            this.m_count_down = null; 
            //room logs
            RoomInfoLog.roomId = Guid.Empty;//seta toda vez que inicia sala

            // Grand Prix tempo que falta para come�ar
            m_now = new SystemTime(DateTime.Now);
            // Coloca a inst�ncia da classe que acabou de criar no vector statico
            Add(this);

            // Verifica se � Grand Prix Normal e cria um temporizador para come�ar a sala
            // Rookie Grand Prix n�o tem tempo para come�ar o player come�a na hora que ele d� play por que � uma inst�ncia
            if (!(sIff.Instance.getGrandPrixAbaType(m_gp.ID) == GrandPrixData.GP_ABA.ROOKIE && sIff.Instance.isGrandPrixNormal(m_gp.ID)))
            {
                // "Zera" a data colocando valores válidos
                m_now.Year = 2000;
                m_now.Month = 1;
                m_now.Day = 1;
                m_now.DayOfWeek = 0; // pode deixar assim, não é usado em DateTime constructor

                // Adiciona 1 dia para o start se a hora for >= 23 do open e <= 1 a hora do start
                if (m_gp.Open.Hour >= 23 && m_gp.Start.Hour <= 1)
                {
                    m_gp.Start.Day = 1;
                }
                m_start = m_gp.Start;//gera o tempo que vai iniciar a sala

                var diff = (!m_gp.Start.IsEmpty ? UtilTime.GetHourDiff(m_gp.Start, m_now) : 0L);

                // mili to sec
                if (diff < 0)
                {
                    diff = 0;
                }

                CountDownStart(diff);
            }
        }
        #endregion
        // Checkers
        public override bool IsAllReady()
        {
            return !HaveInvited();
        }


        public override bool RequestStartGame(Player _session, Packet _packet)
        {
            if (!_session.getState())
            {
                throw new exception("[Room::RequestStartGame] [Error] player nao esta connectado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }
            if (_packet == null)
            {
                throw new exception("[Room::RequestStartGame] [Error] _packet is null", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }

            Packet p = new();

            bool ret = true;

            try
            {   
                if (sIff.Instance.getGrandPrixAbaType(m_gp.ID) == GrandPrixData.GP_ABA.ROOKIE && !sIff.Instance.isGrandPrixNormal(m_gp.ID))
                {
                    throw new exception("[RoomGrandPrix::requestStartGame][Error] Normal[UID=" + Convert.ToString(_session.UserInfo.UID) + "] tentou comecar o jogo na sala[NUMERO=" + Convert.ToString(RoomInfo.RoomID) + "], mas a sala nao é uma Grand Prix Rookie(Tuto). Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM_GRAND_PRIX,
                        1, 0x5900201));
                }

                // Diferente de Grand Prix ROOKIE(TUTO), manda para o requestStartGame da class room, para tratar esse requisi��o
                if (sIff.Instance.getGrandPrixAbaType(m_gp.ID) != GrandPrixData.GP_ABA.ROOKIE)
                {
                    ret = base.RequestStartGame(_session, _packet);
                }
                else
                {
                    // Verifica se todos est�o prontos se n�o da erro
                    if (!IsAllReady())
                    {
                        throw new exception("[RoomGrandPrix::requestStartGame][Error] Normal[UID=" + Convert.ToString(_session.UserInfo.UID) + "] tentou comecar o jogo na sala[NUMERO=" + Convert.ToString(RoomInfo.RoomID) + ", MASTER=" + Convert.ToString(RoomInfo.OwnerUID) + "], mas nem todos jogadores estao prontos. Hacker ou Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM_GRAND_PRIX,
                            8, 0x5900202));
                    }

                    // random CourseIndex if random CourseIndex
                    if (RoomInfo.GetMap() >= 0x7Fu)
                    {

                        // Special Shuffle Course
                        if (RoomInfo.GetRoomType() == RoomTypeFlags.SPECIAL_SHUFFLE_COURSE && RoomInfo.GetHoleType() == RoomHoleType.M_SHUFFLE_COURSE)
                        {

                            RoomInfo.CourseIndex = (RoomCourseFlags)(0x80 | 17);

                        }
                        else
                        { // Random Normal

                            LotterySystem lottery = new LotterySystem();

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
                                RoomInfo.CourseIndex = (RoomCourseFlags)(0x80u | Convert.ToByte(lc.Value));
                            }
                        }
                    }
                }

                if (!MakeGameRoom(m_gp))
                    SendBroadCast(Handle_PACKET_RESPONSE.pacote049(null, TGAME_CREATE_RESULT.CREATE_GAME_CREATE_FAILED));
                // Update Room State
                RoomInfo.StateRoom = 0; // IN GAME

                p.init_plain(0x230);

                SendBroadCast(p);

                p.init_plain(0x231);

                SendBroadCast(p);

                p.init_plain(0x77);

                p.WriteUInt32((uint)GameServer.Instance.getInfo().Rate.Pang); // Rate Pang

                SendBroadCast(p);

                RoomInfoLog.roomId = Guid.Empty;//seta toda vez que inicia sala

                //insert dados do player
                //foreach (var _sessions in v_sessions)
                //    CreateRoomLogSql(_sessions);//criar de todos
                  
            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[Room::RequestStartGame] [Error] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
				_session.Send(Handle_PACKET_RESPONSE.pacote049(this, TGAME_CREATE_RESULT.CREATE_GAME_CREATE_FAILED2));
				ret = false; // Error ao inicializar o Jogo
            }

            return ret;
        }

        public bool GameStart()
        {

            var p = new Packet();

            bool ret = true;

            try
            {

                if (sIff.Instance.getGrandPrixAbaType(m_gp.ID) == GrandPrixData.GP_ABA.ROOKIE && !sIff.Instance.isGrandPrixNormal(m_gp.ID))
                {
                    throw new exception("[RoomGrandPrix::startGame][Error] Server tentou comecar o jogo na sala[NUMERO=" + Convert.ToString(RoomInfo.RoomID) + "], mas a sala nao é uma Grand Prix Rookie(Tuto). Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM_GRAND_PRIX,
                        1, 0x5900201));
                }

                // Verifica se j� tem um jogo inicializado e lan�a error se tiver, para o cliente receber uma resposta
                if (CurrentGame != null)
                {
                    throw new exception("[RoomGrandPrix::startGame][Error] Server tentou comecar o jogo na sala[NUMERO=" + Convert.ToString(RoomInfo.RoomID) + "], mas ja tem um jogo inicializado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM_GRAND_PRIX,
                        7, 0x5900202));
                }

                // Verifica se todos est�o prontos se n�o da erro
                if (!IsAllReady())
                {
                    throw new exception("[RoomGrandPrix::startGame][Error] Server tentou comecar o jogo na sala[NUMERO=" + Convert.ToString(RoomInfo.RoomID) + ", MASTER=" + Convert.ToString(RoomInfo.OwnerUID) + "], mas nem todos jogadores estao prontos. Hacker ou Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM_GRAND_PRIX,
                        8, 0x5900202));
                }

                // random CourseIndex if random CourseIndex
                if (RoomInfo.GetMap() >= 0x7Fu)
                {

                    // Special Shuffle Course
                    if (RoomInfo.GetRoomType() == RoomTypeFlags.SPECIAL_SHUFFLE_COURSE && RoomInfo.GetHoleType() == RoomHoleType.M_SHUFFLE_COURSE)
                    {

                        RoomInfo.CourseIndex = (RoomCourseFlags)(0x80 | 17);

                    }
                    else
                    { // Random Normal

                        LotterySystem lottery = new LotterySystem();

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
                            RoomInfo.CourseIndex = (RoomCourseFlags)(0x80u | Convert.ToByte(lc.Value));
                        }
                    }
                }

                if (!MakeGameRoom(m_gp))
                    SendBroadCast(Handle_PACKET_RESPONSE.pacote049(null, TGAME_CREATE_RESULT.CREATE_GAME_CREATE_FAILED));


                // Update Room State
                RoomInfo.StateRoom = 0; // IN GAME

                // Mandar para ficar igual ao original
                p.init_plain(0x253); 
                p.WriteInt32(0);
                SendBroadCast(p);

                // Update on GAME
                p.init_plain(0x230);
                SendBroadCast(p);

                p.init_plain(0x231);
                SendBroadCast(p);

                var rate_pang = GameServer.Instance.getInfo().Rate.Pang;

                p.init_plain(0x77);

                p.WriteUInt32((uint)rate_pang); // Rate Pang
                SendBroadCast(p);

                RoomInfoLog.roomId = Guid.Empty;//seta toda vez que inicia sala 
            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[RoomGrandPrix::startGame][Error] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                ret = false; // Error ao inicializar o Jogo
            }

            return ret;
        }

        // Init Instance vector and lock, para não dá erro no destrutor por que vai destruir ele primeiro do que a instance da classe
        public static void initFirstInstance()
        { 
            if (m_cs_instancia.Instance.m_state && m_instancias.Instance.Count == 0)
            {
                 _smp.LogManager.Instance.push(new AppMessage("[RoomGrandPrix::initFirstInstance][Sucess] Created", type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        int CountDownStart(object _arg1, object _arg2)
        { 
            RoomGrandPrix _rgp = (RoomGrandPrix)_arg1;

            var sec_to_start = (long)_arg2;

            try
            {

                if (_rgp != null && InitAndValid(_rgp))
                {
                    m_now = new SystemTime(DateTime.Now);
                    sec_to_start = (!m_gp.Start.IsEmpty ? UtilTime.GetHourDiff(m_start, m_now) : 0L);

                    _rgp.CountDownStart(sec_to_start);
                }

            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[RoomGrandPrix::_count_down_to_start][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            return 0;
        }


        public int CountDownStart(long _sec_to_start)
        {
            int ret = 0;

            try
            {
                // Se tempo zerou ou negativo, para tudo e começa jogo ou destroi sala
                if (_sec_to_start <= 0)
                {
                    if (m_count_down != null)
                    {
                        GameServer.Instance.DeleteTimer(m_count_down);
                    }

                    if (Players.Count() >= 1 && GameStart())
                        GameServer.Instance.sendUpdateRoomInfo(this, 3); // Update Room Info
                    else
                        ret = 1; // Destroi a sala
                }
                else
                {

                    uint wait = 0;
                    long interval = 0;
                    float diff = 0.0f;
                    //pega o tempo decorrido, entre o inicio e final = resultado
                    int elapsed_sec = (m_count_down != null) ? (int)Math.Round(m_count_down.getElapsed() / 1000.0f)/*Mili para segundos*/ : 0;

                    _sec_to_start -= elapsed_sec;//tempo já vem atualizado lá em cima

                    if ((diff = ((_sec_to_start - 10/*10 segundos*/) / 30.0f/* 30 segundos*/)) >= 1.0f)
                    {   // Intervalo de 30 segundos

                        if ((_sec_to_start % 30) == 0)
                        {

                            // Intervalo
                            interval = 30 * 1000;   // 30 segundos

                            wait = (uint)(interval * (int)diff);    // 30 * diff minutos em milisegundos

                        }
                        else
                        {

                            // Corrige o tempo para ficar no intervalo certo
                            wait = (uint)(interval = (_sec_to_start % 30) * 1000);

                        }

                    }
                    else if ((diff = ((_sec_to_start - 1/*1 segundo*/) / 10.0f/*10 segundos*/)) >= 1.0f)
                    {           // Intervalo de 10 segundos

                        if ((_sec_to_start % 10) == 0)
                        {

                            // Intervalo
                            interval = 10 * 1000;   // 10 segundos

                            wait = (uint)(interval * (int)diff);    // 10 * diff segundos em milisegundos

                        }
                        else
                        {

                            // Corrige o tempo para ficar no intervalo certo
                            wait = (uint)(interval = (_sec_to_start % 10) * 1000);
                        }

                    }
                    else
                    {       // Intervalo de 1 segundo

                        diff = (float)Math.Round(_sec_to_start / 1.0f);

                        // Intervalo
                        interval = 1000;    // 1 segundo

                        wait = (uint)(interval * (int)diff);    // 1 * diff segundos em milesegundos

                    }

                    // Cria o pacote para broadcast
                    var p = new Packet(0x40);
                    p.WriteByte(11);     // msg
                    p.WriteUInt16(0);    // nick vazio
                    p.WriteUInt16(0);    // msg vazio

                    // Limita _sec_to_start entre 0 e UInt32.MaxValue para evitar erro
                    p.WriteUInt32((uint)_sec_to_start);

                    SendBroadCast(p);

                    // Cria ou reinicia timer caso não exista ou esteja parado/finalizado
                    if (m_count_down == null ||
                        m_count_down.getState() == PangyaSyncTimer.TIMER_STATE.STOP ||
                        m_count_down.getState() == PangyaSyncTimer.TIMER_STATE.FINISH)
                    {
                        if (m_count_down != null)
                            GameServer.Instance.DeleteTimer(m_count_down);

                        // Cria o timer com intervalo calculado e a lista de intervalos (pode ajustar aqui)
                        m_count_down = GameServer.Instance.MakeTimer(wait, new List<long> { interval }, () =>
                        {
                            CountDownStart(this, _sec_to_start);

                        });
                    }
                }
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[RoomGrandZodiacEvent::count_down_to_start][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            return ret;
        }

        // Static Help Check room is valid
        private static void Add(RoomGrandPrix _rgp)
        {

            m_cs_instancia.Instance.@lock();

            m_instancias.Instance.Add(new RoomGrandPrixInstanciaCtx(_rgp, RoomGrandPrixInstanciaCtx.eSTATE.GOOD));

            m_cs_instancia.Instance.unlock();
        }
         
        private static int getRoom(RoomGrandPrix _rgp)
        {

            int index = -1;

            for (var i = 0; i < m_instancias.Instance.Count(); ++i)
            {

                if (m_instancias.Instance[i].m_rgp == _rgp)
                {

                    index = (int)i;

                    break;
                }
            }

            return index;
        }

        private static bool InitAndValid(RoomGrandPrix _rgp)
        {

            bool valid = false;

            m_cs_instancia.Instance.@lock();

            var index = getRoom(_rgp);

            if (index >= 0)
            {
                valid = (m_instancias.Instance[index].m_state == RoomGrandPrixInstanciaCtx.eSTATE.GOOD);
            }

            m_cs_instancia.Instance.unlock();

            return valid;
        }

    } 
}
