using Pangya_GameServer.Game.Manager;
using Pangya_GameServer.Game.System;
using Pangya_GameServer.Models;
using Pangya_GameServer.PacketFunc;
using PangyaAPI.IFF.JP.Extensions;
using PangyaAPI.IFF.JP.Models.Data;
using PangyaAPI.IFF.JP.Models.Flags;
using PangyaAPI.Network.Models;
using PangyaAPI.Network.PangyaPacket;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Models;
using PangyaAPI.Utilities.Log;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using static Pangya_GameServer.Models.DefineConstants;
using static PangyaAPI.Utilities.Tools;
namespace Pangya_GameServer.Game.Base
{
    public abstract class VersusBase : GameBase
    { 
        private PangyaThread m_thread_chk_turn;

        protected IntPtr m_hEvent_chk_turn;//

        private IntPtr m_hEvent_chk_turn_pulse;

        private readonly uint m_max_player = 4;                  // No m�ximo 4 jogadores

        protected PlayerGameInfo m_player_turn;                  // PlayerGameInfo do player que est� tacando ou vai tacar

        uint m_count_pause;                 // Count de pause no Versus Base, 3x � o m�ximo permitido

        readonly uint m_seed_mascot_effect;              // Seed Mascot Effect Random

        public uint m_flag_next_step_game;         // Flag que guarda a proxima passo que o game vai d� depois que um player sai

        protected TreasureHunterVersusInfo m_thi;

        protected stStateVersus m_state_vs;

        public VersusBase(List<Player> _players, RoomInfoEx _ri, RateValue _rv, bool _channel_rookie) : base(_players, _ri, _rv, _channel_rookie)
        {
            this.m_player_turn = new PlayerGameInfo();
            this.m_flag_next_step_game = 0;
            this.m_thi = new TreasureHunterVersusInfo();
            this.m_seed_mascot_effect = 0;
            this.m_count_pause = 0;
            this.m_state_vs = new stStateVersus();
            m_seed_mascot_effect = (uint)(new Random().Next()) & 0xFFFF;
            if (CheckLimitPlayers())
            {
                new exception("[VersusBase::init][Error] bug ou hackers, maximo de jogadores foi superior");
            }

            // Cria evento que vai para a thRead sync hole
            if ((m_hEvent_chk_turn = CreateEvent(IntPtr.Zero,
        true, false, null)) == IntPtr.Zero)
            {
                throw new exception("[VersusBase::init][Error] ao criar evento check versus.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.APPROACH,
                    1050, GetLastError()));
            }

            // Cria evento que vai pulsar a thRead sync hole para ir mais r pido quando um player tacar
            if ((m_hEvent_chk_turn_pulse = CreateEvent(IntPtr.Zero,
                        false, false, null)) == IntPtr.Zero)
            {
                throw new exception("[VersusBase::init][Error] ao criar evento check versus pulse.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.APPROACH,
                    1050, GetLastError()));
            }

            m_thread_chk_turn = new PangyaThread(1050, obj => checkVersusTurn(), this, ThreadPriority.AboveNormal);
        }

        private void finish_thread_check_versus_turn()
        {
            try
            {
                if (m_thread_chk_turn != null)
                {
                    if (m_hEvent_chk_turn != INVALID_HANDLE_VALUE)
                        SetEvent(m_hEvent_chk_turn);

                    // Espera a thread terminar
                    m_thread_chk_turn.waitThreadFinish(-1);
                }
            }
            catch (exception ex)
            {
                Console.WriteLine($"[VersusBase::finish_thread_check_versus_turn][ErrorSystem] {ex.getFullMessageError()}");
            }


            m_thread_chk_turn = null;

            if (m_hEvent_chk_turn != INVALID_HANDLE_VALUE)
                CloseHandle(m_hEvent_chk_turn);

            if (m_hEvent_chk_turn_pulse != INVALID_HANDLE_VALUE)
                CloseHandle(m_hEvent_chk_turn_pulse);

            m_hEvent_chk_turn = IntPtr.Zero;
            m_hEvent_chk_turn_pulse = IntPtr.Zero;
        }


        public override void Dispose(bool disposing) 
        {
            if (disposing)
            {                
                try
                {
                    clear_treasure_hunter();

                    finish_thread_check_versus_turn();
                }
                catch (exception e)
                {
                    _smp.message_pool.getInstance().push(new message("[VersusBase::~VersusBase][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                    if (m_thread_chk_turn != null)
                    {
                        m_thread_chk_turn.exit_thread();
                        m_thread_chk_turn = null;
                    }
                } 
            }
            base.Dispose(true);
        }

        public override void INIT_PLAYER_INFO(string _method, string _msg, Player __session, out PlayerGameInfo pgi)
        {
            pgi = getPlayerInfo(__session);
            if (pgi == null)
                throw new exception($"[{GetType().Name}::" + _method + "][Error] PLAYER[UID=" + __session.m_pi.uid + "] " + _msg + ", mas o game nao tem o info dele guardado. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME, 1, 4));
        }

        public abstract void changeHole();
        public abstract void finishHole();

        public virtual void sendRatesOfVersusBase()
        {

            try
            {
                // Table Rate Voice And Effect
                TableRateVoiceAndEffect table = new TableRateVoiceAndEffect("W_BIGBONGDARI", TableRateVoiceAndEffect.eTYPE.W_BIGBONGDARI);

                // Rate Table Voice
                var p = new PangyaBinaryWriter((ushort)0x115);

                p.WriteString(table.name);

                p.WriteBytes(table.table, table.table.Length);

                packet_func.game_broadcast(this,
                    p, 1);

                // Table Rate Voice And Effect
                table = new TableRateVoiceAndEffect("R_BIGBONGDARI", TableRateVoiceAndEffect.eTYPE.R_BIGBONGDARI);

                p.init_plain(0x115);

                p.WriteString(table.name);

                p.WriteBytes(table.table, table.table.Length);

                packet_func.game_broadcast(this,
                    p, 1);

                // Table Rate Voice And Effect
                table = new TableRateVoiceAndEffect("VOICE_CLUB", TableRateVoiceAndEffect.eTYPE.VOICE_CLUB);
                p.init_plain(0x115);

                p.WriteString(table.name);

                p.WriteBytes(table.table, table.table.Length);

                packet_func.game_broadcast(this,
                    p, 1);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[VersusBase::sendRatesOfVersusBase][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }
        public virtual void sendReplyFinishCharIntro()
        {

            // Resposta para Finish Char Intro
            var p = new PangyaBinaryWriter((ushort)0x90);

            packet_func.game_broadcast(this,
                p, 1);
        }

        public virtual void sendPlayerTurn()
        {
            if (m_player_turn == null)
            {
                _smp.message_pool.getInstance().push(new message("[VersusBase::sendPlayerTurn][ERROR] m_player_turn está null. Ninguém tem o turno!", type_msg.CL_FILE_LOG_AND_CONSOLE));
            }  

            if (m_player_turn == null)
            {
                throw new exception("[VersusBase::sendPlayerTurn][Error] PlayerGameInfo *m_player_turn is invalid(null). Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                    100, 1));
            }

            var hole = m_course.findHole(m_player_turn.hole) ?? throw new exception("[VersusBase::sendPlayerTurn][Error] PLAYER[UID=" + Convert.ToString(m_player_turn.uid) + "] tentou encontrar o hole[NUMERO=" + Convert.ToString(m_player_turn.hole) + "] do course no jogo, mas nao foi encontrado. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                    101, 0));

            var wind_flag = initCardWindPlayer(m_player_turn, hole.getWind().wind);

            // Resposta do vento do hole
            var p = new PangyaBinaryWriter((ushort)0x5B);

            p.WriteByte(hole.getWind().wind + wind_flag);
            p.WriteByte((wind_flag < 0) ? 1 : 0); // Flag de card de vento, aqui é a qnd diminui o vento, 1 Vento azul
            p.WriteUInt16(m_player_turn.degree);
            p.WriteByte(1); // Flag do vento, 1 Reseta o Vento, 0 soma o vento que nem o comando gm \wind do pangya original, , Também é flag para trocar o vento no Pang Battle se mandar o valor 0

            packet_func.game_broadcast(this,
                p, 1);

            // Resposta passa o oid do player que vai começa o Hole
            p.init_plain(0x63);

            if (m_player_turn == null)
            {
                _smp.message_pool.getInstance().push(new message("[VersusBase::sendPlayerTurn][Error] player_turn is invalid(null)", type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.WriteUInt32(0);
            }
            else
            {
                p.WriteInt32(m_player_turn.oid);
            }

            // Aqui tem 2 bytes a+, int16 de um valor, que acho que acontece de ver em quando, ou só no pang battle isso estava escrito no meu outro
            // acho que possa ser do pang battle, certaza, aqui acabei de ver na classe pang battle do antigo

            packet_func.game_broadcast(this,
                p, 1);
        }

        public virtual void changeTurn()
        {

            if (m_player_turn == null)
            {
                throw new exception("[VersusBase::changeTurn][Error] PlayerGameInfo *m_player_turn is invalid(null). Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                    100, 0));
            }

            // Para o tempo do player do turno
            //stopTime();

            // Check Player Turn finish last hole
            if (m_player_turn.shot_sync.state_shot.display.acerto_hole || m_player_turn.data.giveup == 1)
            {

                // Verifica se o player terminou jogo, fez o ultimo hole
                if (m_course.findHoleSeq(m_player_turn.hole) == m_ri.qntd_hole)
                {

                    // Resposta para o player que terminou o ultimo hole do Game
                    var p = new PangyaBinaryWriter((ushort)0x199);

                    packet_func.game_broadcast(this,
                        p, 1);

                    // Fez o Ultimo Hole, Calcula Clear Bonus para o player
                    if (m_player_turn.shot_sync.state_shot.display.clear_bonus)
                    {

                        if (!MapSystem.getInstance().isLoad())
                        {
                            MapSystem.getInstance().load();
                        }

                        var map = MapSystem.getInstance().getMap((m_ri.getMap()));

                        if (map == null)
                        {
                            _smp.message_pool.getInstance().push(new message("[VersusBase::changeTurn][Error][Warning] tentou pegar o Map dados estaticos do course[COURSE=" + Convert.ToString((ushort)(m_ri.getMap())) + "], mas nao conseguiu encontra na classe do Server.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                        }
                        else
                        {
                            m_player_turn.data.bonus_pang += MapSystem.getInstance().calculeClearVS(map,
                            (uint)m_players.Count(),
                            m_ri.qntd_hole);
                        }
                    }
                }
            }

            // Limpa dados que usa para cada tacada
            clearDataEndShot(m_player_turn);

            // Verifica se todos fizeram o hole 
            if (checkAllClearHole())
            {

                clear_all_flag_sync();

                finishHole();

                // Utilizo ele antes no finish hole, limpo ele aqui depois
                clearAllClearHole();

                changeHole();

            }
            else if (m_players.Count() == 1 && m_course.findHoleSeq(m_player_turn.hole) < 4)
            { // Finaliza o game

                clear_all_flag_sync();

                finishHole();

                changeHole();

            }
            else
            { // Troca o Turno

                clear_all_flag_sync();

                // Recalcula Turno
                requestCalculePlayerTurn();
                // Cnvia para todos o vento e oid do player turn, o player que vai tacar nesse momento
                sendPlayerTurn();
            }
        }

        public virtual void CCGChangeWind(Player _gm, byte _wind, ushort _degree)
        {

            try
            {

                if (m_player_turn == null)
                {
                    throw new exception("[VersusBase::CCGChangeWind][Error] PLAYER[UID=" + Convert.ToString(_gm.m_pi.uid) + "] tentou executar o comando de troca de vento no versus na sala[NUMERO=" + Convert.ToString(m_ri.numero) + "], mas o player_turn do versus eh invalido. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                        1, 0x5700100));
                }

                var hole = m_course.findHole(m_player_turn.hole);

                if (hole == null)
                {
                    throw new exception("[VersusBase::CCGChangeWind][Error] PLAYER[UID=" + Convert.ToString(_gm.m_pi.uid) + "] tentou executar o comando de troca de vento no versus na sala[NUMERO=" + Convert.ToString(m_ri.numero) + "], mas o nao encontrou o hole[VALUE=" + Convert.ToString((short)m_player_turn.hole) + "] no course. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                        2, 0x5700100));
                }

                var wind = hole.getWind();

                // Change Wind of Hole
                wind.wind = _wind;

                hole.setWind(wind);

                // Change Degree of player
                m_player_turn.degree = (ushort)(_degree % LIMIT_DEGREE);

                // Log
                _smp.message_pool.getInstance().push(new message("[VersusBase::CCGChangeWind][Log] [GM] PLAYER[UID=" + Convert.ToString(_gm.m_pi.uid) + "] trocou o vento e graus da sala[NUMERO=" + Convert.ToString(m_ri.numero) + ", VENTO=" + Convert.ToString((ushort)_wind + 1) + ", GRAUS=" + Convert.ToString(_degree) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                var wind_flag = initCardWindPlayer(m_player_turn, hole.getWind().wind);

                // UPDATE ON GAME
                var p = new PangyaBinaryWriter((ushort)0x5B);

                p.WriteByte(hole.getWind().wind + wind_flag); // Wind
                p.WriteByte((wind_flag < 0) ? 1 : 0); // Card Wind Flag, minus wind flag
                p.WriteUInt16(m_player_turn.degree); // Degree
                p.WriteByte(1); // Flag 1 = Reset Degree, 0 = Plus Degree, , Também é flag para trocar o vento no Pang Battle se mandar o valor 0

                packet_func.game_broadcast(this,
                    p, 1);
            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[VersusBase::CCGChangeWind][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                throw;
            }
        }

        public virtual int checkEndShotOfHole(Player _session)
        {

            // Agora verifica o se ele acabou o hole e essas coisas
            INIT_PLAYER_INFO("checkEndShotOfHole",
                "tentou verificar a ultima tacada do hole no jogo",
                _session, out PlayerGameInfo pgi);

            if (pgi.data.bad_condute >= 3)
            {
                return 2; // Tira da sala
            }
            else
            {
                setFinishShot(pgi);
            }

            return 0;
        }

        public virtual void drawDropItem(Player _session)
        {

            INIT_PLAYER_INFO("drawDropItem",
                "tentou sortear item drop para o jogador no jogo",
                _session, out PlayerGameInfo pgi);

            if (pgi.shot_sync.state_shot.display.acerto_hole)
            {
                var drop = requestInitDrop(_session);

                if (!drop.v_drop.empty())
                {
                    var p = new PangyaBinaryWriter((ushort)0xCC);

                    p.WriteInt32(_session.m_oid);

                    // Count, Coin/Cube "Drop"
                    p.WriteByte((byte)drop.v_drop.Count);

                    if (!drop.v_drop.empty())
                    {
                        foreach (var el in drop.v_drop)
                        {
                            p.WriteBytes(el.ToArray());
                        }

                        // Aqui o server passa 128 itens de drop, os que dropou e o resto vazio
                        if (drop.v_drop.Count < 128)
                        {
                            p.WriteZeroByte((128 - drop.v_drop.Count) * 16);
                        }
                    }

                    packet_func.game_broadcast(this,
                        p, 1);
                }
            }
        }

        public virtual void init_turn_hole_start()
        {

            if (!m_player_order.empty())
            {
                m_player_order.Clear();
            }

            foreach (var el in m_players)
            {

                if (el != null)
                {

                    INIT_PLAYER_INFO("init_turn_hole_start",
                        " tentou calcular o player do turno do comeco do hole no jogo",
                        el, out PlayerGameInfo pgi);

                    if (pgi.flag != PlayerGameInfo.eFLAG_GAME.QUIT)
                    {
                        m_player_order.Add(pgi);
                    }

                    if (pgi.flag == PlayerGameInfo.eFLAG_GAME.QUIT)
                    {
                    }
                    else
                    {
                    }

                }
            }


            m_player_order.Sort(sort_player_turn_hole_start);
        }

        public PlayerGameInfo getNextPlayerTurnHole()
        {

            PlayerGameInfo pgi = null;

            if (!m_player_order.empty())
            {

                pgi = m_player_order.First();

                m_player_order.Remove(m_player_order.First());

                if (pgi == null || pgi.flag == PlayerGameInfo.eFLAG_GAME.QUIT)
                {
                    return getNextPlayerTurnHole();
                }
            }

            return pgi;
        }

        public virtual PlayerGameInfo requestCalculePlayerTurn()
        {

            if ((m_player_turn = getNextPlayerTurnHole()) != null)
            {
                return m_player_turn;
            }

            if (!m_player_info.empty())
            {

                var hole = m_course.findHole(m_player_info.First().Value.hole);

                if (hole == null)
                {
                    _smp.message_pool.getInstance().push(new message("[VersusBase::requestCalculePlayerTurn][Error] PLAYER[UID=" + Convert.ToString(m_player_info.begin().Value.uid) + "] o hole[NUMERO=" + Convert.ToString(m_player_info.begin().Value.hole) + "] nao foi encontrado no course. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                    m_player_turn = null;

                    return null;
                }

                List<PlayerOrderTurnCtx> v_player_order_turn = new List<PlayerOrderTurnCtx>();

                foreach (var el in m_players)
                {

                    if (el != null)
                    {

                        INIT_PLAYER_INFO("requestCalculePlayerTurn",
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
                    m_player_turn = null;

                    _smp.message_pool.getInstance().push(new message("[VersusBase::requestCalculePlayerTurn][Error] Ninguém foi selecionado como próximo turno. m_player_order pode estar vazio.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                    return null;
                }
                v_player_order_turn.Sort(sort_player_turn);

                if (v_player_order_turn.Count > 0)
                {
                    m_player_turn = v_player_order_turn[0].pgi;
                }
                else
                {
                    _smp.message_pool.getInstance().push(new message("[VersusBase::requestCalculePlayerTurn][Error] Ninguém foi selecionado como próximo turno. m_player_order pode estar vazio.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                    m_player_turn = null; // ou lançar exceção, dependendo do fluxo
                }

            }

            return m_player_turn;
        }

        public virtual void clear_treasure_hunter()
        {
            m_thi.clear();
        }


        public int sort_player_turn_hole_start(PlayerGameInfo _pgi1, PlayerGameInfo _pgi2)
        {
            if ((_pgi1.progress.hole - 1) < 0)
                return 0;  // Considera empate se o índice for inválido

            var index = _pgi1.progress.hole - 1;

            if (_pgi1.progress.score[index] < _pgi2.progress.score[index])
                return -1;

            if (_pgi1.progress.score[index] > _pgi2.progress.score[index])
                return 1;

            if (_pgi1.data.pang > _pgi2.data.pang)
                return -1;

            if (_pgi1.data.pang < _pgi2.data.pang)
                return 1;

            if (_pgi1.data.score < _pgi2.data.score)
                return -1;

            if (_pgi1.data.score > _pgi2.data.score)
                return 1;

            return 0;
        }

        public int sort_player_turn(PlayerOrderTurnCtx _potc1, PlayerOrderTurnCtx _potc2)
        {
            var diff1 = _potc1.hole.getPinLocation().diffXZ(_potc1.pgi.location);
            var diff2 = _potc2.hole.getPinLocation().diffXZ(_potc2.pgi.location);

            bool p1Acerto = _potc1.pgi.shot_sync.state_shot.display.acerto_hole;
            bool p1Giveup = _potc1.pgi.data.giveup == 1;
            bool p2Acerto = _potc2.pgi.shot_sync.state_shot.display.acerto_hole;
            bool p2Giveup = _potc2.pgi.data.giveup == 1;

            if (!p1Acerto && (p2Acerto || p2Giveup))
                return -1;

            if (p1Acerto && !(p2Acerto || p2Giveup))
                return 1;

            if (_potc1.pgi.data.tacada_num == 0 && _potc2.pgi.data.tacada_num > 0)
                return -1;

            if (_potc1.pgi.data.tacada_num > 0 && _potc2.pgi.data.tacada_num == 0)
                return 1;

            if (diff1 > diff2 && !p1Acerto && !p1Giveup)
                return -1;

            if (diff1 < diff2 && !(p2Acerto || p2Giveup))
                return 1;

            if (diff1 == diff2)
            {
                if (_potc1.pgi.data.tacada_num < _potc2.pgi.data.tacada_num && !p1Acerto && !p1Giveup)
                    return -1;

                if (_potc1.pgi.data.tacada_num > _potc2.pgi.data.tacada_num && !(p2Acerto || p2Giveup))
                    return 1;

                if (_potc1.pgi.data.tacada_num == _potc2.pgi.data.tacada_num)
                {
                    if (_potc1.pgi.data.pang > _potc2.pgi.data.pang && !p1Acerto && !p1Giveup)
                        return -1;

                    if (_potc1.pgi.data.pang < _potc2.pgi.data.pang && !(p2Acerto || p2Giveup))
                        return 1;
                }
            }

            return 0;
        }


        public static void end_time(object _arg1, object _arg2)
        {
            var game = (VersusBase)_arg1;

            try
            {
                if (game?.m_timer == null)
                    return;

                // Se o tempo acabou
                game.timeIsOver(_arg2); // Aqui você implementa o que deve acontecer
            }
            catch (Exception e)
            {
                _smp.message_pool.getInstance().push(
                    new message("[VersusBase::end_time][ErrorSystem] " + e.Message, type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }


        public object checkVersusTurn()
        { 
            var datetime = Stopwatch.StartNew();
            TimeSpan ts = datetime.Elapsed;
            try
            {
                string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}", ts.Hours, ts.Minutes, ts.Seconds);

                _smp.message_pool.getInstance().push(new message($"[VersusBase::checkVersusTurn] Partida comecou: {elapsedTime}", type_msg.CL_FILE_LOG_AND_CONSOLE));

                uint retWait = WAIT_TIMEOUT;//
                IntPtr[] wait_events = { m_hEvent_chk_turn, m_hEvent_chk_turn_pulse };

                while ((retWait = WaitForMultipleObjects((uint)wait_events.Length, wait_events, false, 1000 /*1 segundo*/)) == WAIT_TIMEOUT || retWait == (WAIT_OBJECT_0 + 1))
                {
                    try
                    {
                        m_state_vs.@lock();
                        switch (m_state_vs.getState())
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
                        m_state_vs.unlock();
                    }
                    catch (exception ex)
                    {
                        _smp.message_pool.getInstance().push(new message("[VersusBase::checkVersusTurn][ErrorSystem] " + ex.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                        m_state_vs.unlock();
                    }
                }
                //para o tempo
                datetime.Stop();
                ts = datetime.Elapsed;
                elapsedTime = String.Format("{0:00}:{1:00}:{2:00}", ts.Hours, ts.Minutes, ts.Seconds);

                _smp.message_pool.getInstance().push(new message($"[VersusBase::checkVersusTurn] Partida Finalizada. Tempo total: {elapsedTime}", type_msg.CL_FILE_LOG_AND_CONSOLE));

            }
            catch (exception e)
            {
                _smp.message_pool.getInstance().push(new message("[VersusBase::checkVersusTurn][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
            return null;
        }

        // Métodos auxiliares para cada estado

        private void HandleWaitHitShot()
        {
            if (m_flag_next_step_game == 0)
                return;

            ValidatePlayerTurn();

            if (m_flag_next_step_game == 1 && m_player_turn.flag == PlayerGameInfo.eFLAG_GAME.QUIT)
            {
                var p = new PangyaBinaryWriter((ushort)0x92);
                packet_func.game_broadcast(this, p, 1);
            }
            else if (m_flag_next_step_game == 2)
            {
                _smp.message_pool.getInstance().push(new message("[VersusBase::HandleWaitHitShot][Log] Finaliza game.", type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
            else if (m_player_turn.flag == PlayerGameInfo.eFLAG_GAME.QUIT)
            {
                changeTurn();
            }

            if (m_flag_next_step_game != 1)
                m_flag_next_step_game = 0;
        }

        private void HandleShoting()
        {
            if (checkAllSyncShot())
            {
                ValidatePlayerTurn();

                var session = findSessionByPlayerGameInfo(m_player_turn);
                if (session != null)
                {
                    drawDropItem(session);
                }
                else
                {
                    _smp.message_pool.getInstance().push(new message($"[VersusBase::checkVersus][Error] Player UID={m_player_turn.uid} não encontrado no mapa player_info.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                }

                sendSyncShot();
                clearSyncShot();
                m_state_vs.setState(STATE_VERSUS.END_SHOT);
                return;
            }

            if (m_flag_next_step_game != 0)
            {
                HandleWaitHitShot(); // Reaproveita a lógica semelhante
                return;
            }
            // Verifica tempo dos players para reenvio do pacote 1B sync Shot
            CheckPlayersSyncShotTimeout(10, 3, 0x8A);
        }

        private void HandleEndShot()
        {
            if (m_players.Count <= 0)
            {
                return;
            }

            if (checkAllFinishShot())
            {
                clearFinishShot();
                ValidatePlayerTurn();

                if (m_flag_next_step_game != 0)
                {
                    if (m_flag_next_step_game == 1)
                    {
                        var p = new PangyaBinaryWriter((ushort)0x92);
                        packet_func.game_broadcast(this, p, 1);
                    }
                    else if (m_flag_next_step_game == 2)
                    {
                        _smp.message_pool.getInstance().push(new message("[VersusBase::HandleEndShot][Log] Change Turn.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                        changeTurn();
                    }
                    else
                    {
                        changeTurn();
                    }
                    m_flag_next_step_game = 0;
                }
                else
                {
                    changeTurn();
                }
                return;
            }

            if (m_flag_next_step_game != 0)
            {
                HandleWaitHitShot(); // Reaproveita a lógica semelhante
                return;
            }

            // Verifica tempo dos players para desconectar após 10 segundos
            CheckPlayersSyncShotTimeout(10, 3, 0x8A, isFinishShot: true);
        }

        private void HandleLoadHole()
        {
            if (!checkAllLoadHole())
                return;

            clearLoadHole();

            sendReplyFinishLoadHole();
            sendRatesOfVersusBase();
            m_state_vs.setState(STATE_VERSUS.WAIT_HIT_SHOT);
        }
        private void ValidatePlayerTurn()
        {
            if (m_player_turn == null)
                throw new exception("[VersusBase::checkVersus] PlayerGameInfo* m_player_turn is invalid(null)", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS, 1201, 0));
        }

        private void CheckPlayersSyncShotTimeout(int timeoutSeconds, int maxRetries, ushort packetId, bool isFinishShot = false)
        {
            foreach (var s in m_players)
            {
                INIT_PLAYER_INFO("checkVersusTurn",
                    isFinishShot ? "Verifica tempo para pacote1C sync Finish Shot" : "Verifica tempo para pacote1B sync Shot",
                    s, out PlayerGameInfo pgi);

                try
                {


                    if (pgi.sync_shot_flag2 == 0 && pgi.tick_sync_shot.active)
                    {
                        double elapsed = pgi.tick_sync_shot.ElapsedSeconds;

                        if (elapsed > timeoutSeconds)
                        {
                            _smp.message_pool.getInstance().push(new message($"[VersusBase::CheckPlayersSyncShotTimeout][Log] PLAYER[UID={s.m_pi.uid}] não enviou o pacote {(isFinishShot ? "1C" : "1B")} sync shot em {timeoutSeconds} segundos.", type_msg.CL_ONLY_FILE_LOG));

                            if (++pgi.tick_sync_shot.count >= maxRetries)
                            {
                                pgi.tick_sync_shot.Stop();

                                if (isFinishShot)
                                    pgi.finish_shot = 1; // sinaliza que terminou o shot (finalizou)
                                else
                                    pgi.sync_shot_flag2 = 1; // para o caso do sync shot normal

                                var p = new PangyaBinaryWriter(packetId);
                                packet_func.session_send(p, s, 1);

                                _smp.message_pool.getInstance().push(new message($"[VersusBase::CheckPlayersSyncShotTimeout][Log] PLAYER[UID={s.m_pi.uid}] passou {(timeoutSeconds * maxRetries)} segundos, desconectando.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                            }
                            else
                            {
                                pgi.tick_sync_shot.tick = (ulong)Stopwatch.GetTimestamp(); // reinicia só o tempo

                                var p = new PangyaBinaryWriter(packetId);
                                packet_func.session_send(p, s, 1);
                            }
                        }
                    }
                }
                catch
                {
                    m_state_vs.unlock();
                }
            }
        }


        public override void sendInitialData(Player _session)
        {

            var p = new PangyaBinaryWriter();

            try
            { 
                // No Versus tem o Update Last 5 players play
                if (Interlocked.Increment(ref m_sync_send_init_data) == m_players.Count)
                {
                    Interlocked.Exchange(ref m_sync_send_init_data, 0);

                    int st_i = 0;

                    List<CardEquipInfoEx> v_card_equip_char_and_special = new List<CardEquipInfoEx>();

                    // Game Data Init
                    p.init_plain(0x76);

                    p.WriteByte(m_ri.tipo_show);

                    p.WriteByte((byte)m_players.Count);

                    foreach (var el in m_players)
                    {
                        // Member Info 
                        p.WriteBytes(el.m_pi.mi.ToArrayEx());
                        // User Info
                        p.WriteUInt32(el.m_pi.uid);
                        p.WriteBytes(el.m_pi.ui.ToArray());

                        // Trofel Info Current Season
                        p.WriteBytes(el.m_pi.ti_current_season.ToArray());

                        // User Equipped Item
                        p.WriteBytes(el.m_pi.ue.ToArray());

                        // Map Statistics Normal
                        for (st_i = 0; st_i < MS_NUM_MAPS; st_i++)
                            p.WriteBytes(el.m_pi.a_ms_normal[index: st_i].ToArray());

                        // Map Statistics Natural
                        for (st_i = 0; st_i < MS_NUM_MAPS; st_i++)
                            p.WriteBytes(el.m_pi.a_ms_natural[st_i].ToArray());

                        // Map Statistics Grand Prix
                        for (st_i = 0; st_i < MS_NUM_MAPS; st_i++)
                            p.WriteBytes(el.m_pi.a_ms_grand_prix[st_i].ToArray());

                        for (int j = 0; j < 9; j++)
                            for (st_i = 0; st_i < MS_NUM_MAPS; st_i++)
                                p.WriteBytes(el.m_pi.aa_ms_normal_todas_season[j, st_i].ToArray());

                        // Character Info(CharEquip)
                        if (el.m_pi.ei.char_info != null && el.m_pi.ei.char_info.id != 0)
                        {

                            var tmp_char_info = el.m_pi.ei.char_info;

                            int _value = -1;

                            for (var stats = 0; stats < 5; stats++)
                            {

                                _value = el.m_pi.getCharacterMaxSlot((CharacterInfo.Stats)(stats));

                                // Não deixa passar do Slot em jogo
                                if (_value != -1 && tmp_char_info.pcl[stats] > _value)
                                {
                                    tmp_char_info.pcl[stats] = (byte)_value;
                                }
                            }

                            p.WriteBytes(el.m_pi.ei.char_info.ToArray());
                        }
                        else
                            p.WriteZeroByte(513);

                        // Caddie Info
                        if (el.m_pi.ei.cad_info != null && el.m_pi.ei.cad_info.id != 0)
                            p.WriteBytes(el.m_pi.ei.cad_info.getInfo().ToArray());
                        else
                            p.WriteZeroByte(25);

                        // Club Set Info
                        if (el.m_pi.ei.csi != null && el.m_pi.ei.csi.id != 0)
                        {

                            var tmp_csi = el.m_pi.ei.csi;

                            int _value = -1;

                            for (var stats = 0; stats < 5; stats++)
                            {

                                _value = el.m_pi.getClubSetMaxSlot((CharacterInfo.Stats)(stats));

                                // Não deixa passar do Slot em jogo
                                if (_value != -1 && tmp_csi.slot_c[stats] > _value)
                                {
                                    tmp_csi.slot_c[stats] = (short)_value;
                                }
                            }

                            p.WriteBytes(tmp_csi.ToArray()); 
                        }
                        else
                            p.WriteZeroByte(28);

                        // Mascot Info
                        if (el.m_pi.ei.mascot_info != null && el.m_pi.ei.mascot_info.id != 0)
                        {
                            p.WriteBytes(el.m_pi.ei.mascot_info.ToArray());
                        }
                        else
                            p.WriteZeroByte(62);

                        // Time Start
                        p.WriteTime(m_start_time);

                        // Card(s) Equipped, acho que aqui não vai os itens buff, por que ele só da buff de exp e pang, o outro player nao precisa saber
                        v_card_equip_char_and_special = new List<CardEquipInfoEx>();

                        foreach (var el2 in el.m_pi.v_cei)
                        {
                            if ((el2.parts_id == 0 && el2.parts_typeid == 0) || (el.m_pi.ei.char_info != null && el2.parts_id == el.m_pi.ei.char_info.id && el2.parts_typeid == el.m_pi.ei.char_info._typeid))
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

                    packet_func.game_broadcast(this, p, 1);

                    // Send Individual Packet to all players in game
                    foreach (var el in m_players)
                    {
                        // Send MapStatistics Info
                        sendUpdateInfoAndMapStatistics(el, -1);

                        // Course
                        base.sendInitialData(el);

                        // Send seed Mascot Effect
                        p.init_plain(0x16A);

                        p.WriteUInt32(m_seed_mascot_effect);

                        packet_func.session_send(p,
                            el, 1); 
                    }
                }
            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[VersusBase::sendInitialData][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void requestInitHole(Player _session, packet _packet)
        {
            //////REQUEST_BEGIN("InitHole");

            try
            {

                stInitHole ctx_hole = new stInitHole();
                #region Read Packet
                ctx_hole.numero = _packet.ReadByte();
                ctx_hole.option = _packet.ReadUInt32();
                ctx_hole.ulUnknown = _packet.ReadUInt32();
                ctx_hole.par = _packet.ReadByte();
                ctx_hole.tee = new stXZLocation
                {
                    x = _packet.ReadSingle(),
                    z = _packet.ReadSingle()
                };
                ctx_hole.pin = new stXZLocation
                {
                    x = _packet.ReadSingle(),
                    z = _packet.ReadSingle()
                };
                #endregion
                var hole = m_course.findHole(ctx_hole.numero);

                hole.init(ctx_hole.tee, ctx_hole.pin);

                INIT_PLAYER_INFO("requestInitHole",
                    "tentou inicializar o hole[NUMERO = " + Convert.ToString(hole.getNumero()) + "] no jogo",
                    _session, out PlayerGameInfo pgi);

                // Update Location Player in Hole
                pgi.location.x = ctx_hole.tee.x;
                pgi.location.z = ctx_hole.tee.z;

                // Número do hole atual, que o player está jogando
                pgi.hole = ctx_hole.numero;

                // Flag que marca se o player já inicializou o primeiro hole do jogo
                if (!pgi.init_first_hole)
                {
                    pgi.init_first_hole = true;
                }

                // Gera degree para o player ou pega o degree sem gerar que é do modo do hole repeat
                pgi.degree = (m_ri.modo == 4) ? hole.getWind().degree.getDegree() : hole.getWind().degree.getShuffleDegree();
            }
            catch (exception e)
            {
                _smp.message_pool.getInstance().push(new message("[VersusBase::requestInitHole][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override bool requestFinishLoadHole(Player _session, packet _packet)
        {

            // Esse aqui é para Trocar Info da Sala
            // para colocar a sala no modo que pode entrar depois de ter começado
            bool ret = false;

            try
            {

                m_state_vs.setStateWithLock(STATE_VERSUS.LOAD_HOLE);

                INIT_PLAYER_INFO("requestFinishLoadHole",
                    "tentou finalizar carregamento do hole no jogo",
                    _session, out PlayerGameInfo pgi);

                setLoadHole(pgi);


            }
            catch (exception e)
            {
                _smp.message_pool.getInstance().push(new message("[VersusBase::requestFinishLoadHole][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            return ret;
        }

        public override void requestFinishCharIntro(Player _session, packet _packet)
        {
            ////REQUEST_BEGIN("FinishCharIntro");


            try
            {

                INIT_PLAYER_INFO("requestFinishCharIntro",
                    "tentou finalizar intro do char no jogo",
                    _session, out PlayerGameInfo pgi);

                // Zera todas as tacada num dos players
                pgi.data.tacada_num = 0;

                // Giveup Flag
                pgi.data.giveup = 0;

                if (setFinishCharIntroAndCheckAllFinishCharIntroAndClear(pgi))
                {
                    sendReplyFinishCharIntro();
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[VersusBase::requestFinishCharIntro][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void requestFinishHoleData(Player _session, packet p)
        {
            ////REQUEST_BEGIN("FinishHoleData");

            try
            {

                UserInfoEx ui = new UserInfoEx();
                #region Read Packet
                ui.ToRead(p); 
                #endregion

                requestTranslateFinishHoleData(_session, ui);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[VersusBase::requestFinishHoleData][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void requestInitShotSended(Player _session, packet _packet)
        {
            ////REQUEST_BEGIN("InitShotSended");

            try
            {

                INIT_PLAYER_INFO("requestInitShotSended",
                    "player recebeu o pacote de InitShot",
                    _session, out PlayerGameInfo pgi);


                // Player recebeu o pacote55, agora o checkVersusTurn pode verifica se ele enviou o pacote no tempo
                pgi.tick_sync_shot.Start();

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[VersusBase::requestInitShotSended][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }


        public override void requestInitShot(Player _session, packet _packet)
        {
            ////REQUEST_BEGIN("InitShot");

            var p = new PangyaBinaryWriter();

            try
            {

                INIT_PLAYER_INFO("requestInitShot",
                    "tentou iniciar tacada no jogo",
                    _session, out PlayerGameInfo pgi);



                if (pgi.init_shot == 1)
                {
                    _smp.message_pool.getInstance().push(new message("[VersusBase::requestInitShot][Log] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] o server ja recebeu o pacote12 Init Shot. ignora esse.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                    return;
                }
                else
                {
                    pgi.init_shot = 1;
                }

                // Stop time turn
                pgi.bar_space.setState(0); // Volta para 1 depois que taca, era esse meu comentário no antigo

                // para o tempo da tacada ele acabou de tacar
                stopTime();//cada jogador deve terminar o time...
                //pois o time, é diferente para cada jogador....

                pgi.tempo = 0; // Reseta o tempo
                               // end

                ShotDataEx sd = new ShotDataEx();
                #region Read Shot Sync Data
                sd.option = _packet.ReadUInt16();

                if (sd.option == 1)
                {
                    sd.power_shot.option = _packet.ReadByte();
                    sd.power_shot.decrease_power_shot = _packet.ReadInt32();
                    sd.power_shot.increase_power_shot = _packet.ReadInt32();
                }

                //READ SHOTDataBase primeiro, primeiro se lê a classe base, e depois a classe que herda.

                sd.bar_point[0] = _packet.ReadSingle();
                sd.bar_point[1] = _packet.ReadSingle();

                sd.ball_effect[0] = _packet.ReadSingle();
                sd.ball_effect[1] = _packet.ReadSingle();

                sd.acerto_pangya_flag = _packet.ReadByte();
                sd.special_shot.ulSpecialShot = _packet.ReadUInt32();
                sd.time_hole_sync = _packet.ReadUInt32();
                sd.mira = _packet.ReadSingle();

                sd.time_shot = _packet.ReadUInt32();
                sd.bar_point1 = _packet.ReadSingle();
                sd.club = _packet.ReadByte();

                sd.fUnknown[0] = _packet.ReadSingle();
                sd.fUnknown[1] = _packet.ReadSingle();

                sd.impact_zone_pixel = _packet.ReadSingle();

                sd.natural_wind[0] = _packet.ReadInt32();
                sd.natural_wind[1] = _packet.ReadInt32();

                //READ SHOTDATA
                sd.spend_time_game = _packet.ReadSingle();


                //ShotType = (SHOT_STATE_Enum)_packet.ReadUInt16();

                #endregion
                pgi.shot_data = sd;

                m_state_vs.setStateWithLock(STATE_VERSUS.SHOTING);

                // Aqui não manda resposta no TourneyBase ou Practice, mas outro modos(VS, MATCH) manda e outros também não(TOURNEY)
                p.init_plain(0x55);

                p.WriteInt32(_session.m_oid);
                //talvez problema aqui
                p.WriteBytes(sd.ToArrayEx());//pela analize é 62

                packet_func.game_broadcast(this,
                    p, 1);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[VersusBase::requestInitShot][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void requestSyncShot(Player _session, packet _packet)
        {

            ////REQUEST_BEGIN("SyncShot");

            try
            {

                INIT_PLAYER_INFO("requestSyncShot",
                    "tentou sincronizar a tacada no jogo",
                    _session, out PlayerGameInfo pgi);



                if (pgi.sync_shot_flag2 == 1)
                {

                    _smp.message_pool.getInstance().push(new message("[VersusBase::requestSyncShot][Log] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] packet no read 0x1B.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                    m_player_turn = pgi;
                    return;

                }
                else
                {
                    pgi.sync_shot_flag2 = 1;
                }

                ShotSyncData ssd = new ShotSyncData();

                base.requestReadSyncShotData(_session,
                    _packet, ref ssd);

                requestTranslateSyncShotData(_session, ssd);

                requestReplySyncShotData(_session);
            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[VersusBase::requestSyncShot][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void requestInitShotArrowSeq(Player _session, packet _packet)
        {
            ////REQUEST_BEGIN("InitShotArrowSeq");

            try
            {

                byte count_seta = _packet.ReadByte();

                if (count_seta == 0)
                {
                    throw new exception("[VersusBase::requestInitShotArrowSeq][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou inicializar as sequencia de setas, mas nao enviou nenhuma seta. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                        5, 0));
                }

                List<uArrow> setas = new List<uArrow>();

                for (var i = 0; i < count_seta; ++i)
                {
                    setas.Add(new uArrow(_packet.ReadUInt32()));
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[VersusBase::requestInitShotArrowSeq][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }


        public override void requestShotEndData(Player _session, packet _packet)
        {
            ////REQUEST_BEGIN("requestShotEndData");

            var p = new PangyaBinaryWriter();

            try
            {

                // ----------------- LEMBRETE --------------
                // Aqui vou usar para as tacadas do spinning cube que gera no course

                ShotEndLocationData seld = new ShotEndLocationData(_packet);

                if (m_player_turn == null)
                {
                    throw new exception("[VersusBase::requestShotEndData][Error] m_player_turn is invalid(null)", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                        1500, 0));
                }

                if (m_player_turn.uid == _session.m_pi.uid)
                {
                    m_player_turn.shot_data_for_cube = seld;
                }

                // Resposta para Shot End Data
                p.init_plain(0x1F7);


                p.WriteInt32(m_player_turn.oid);
                p.WriteByte(m_player_turn.hole);

                p.WriteBytes(seld.ToArray());

                packet_func.game_broadcast(this,
                    p, 1);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[VersusBase::requestShotEndData][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override RetFinishShot requestFinishShot(Player _session, packet _packet)
        {
            ////REQUEST_BEGIN("FinishShot");

            var p = new PangyaBinaryWriter();

            RetFinishShot ret = new RetFinishShot();

            try
            {

                INIT_PLAYER_INFO("requestFinishShot",
                    "tentou sincronizar o termino da tacada no jogo",
                    _session, out PlayerGameInfo pgi);

                // Essa parte tem que vir antes, mas estou fazendo teste de dc 

                if (pgi.finish_shot2 == 1)
                {
                    _smp.message_pool.getInstance().push(new message("[VersusBase::requestFinishShot][Log] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] o server ja recebeu o pacote1C sync end shot do player. ignora esse.", type_msg.CL_FILE_LOG_AND_CONSOLE));

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

                    m_players.ForEach(_el =>
                    {
                        try
                        {
                            INIT_PLAYER_INFO("requestFinishShot",
                                " tentou ativar all tick sync end shot do jogo",
                                _el, out pgi);

                            pgi.tick_sync_end_shot.active = true;
                        }
                        catch (exception e)
                        {
                            _smp.message_pool.getInstance().push(new message("[VersusBase::requestFinishShot][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                        }
                    });
                }



                // Request Init Cube Coin
                var cube = requestInitCubeCoin(_session, _packet);

                // Resposta para Finish Shot
                sendEndShot(_session, cube);

                ret.ret = checkEndShotOfHole(_session);

                if (ret.ret == 2)
                {
                    ret.p = findSessionByPlayerGameInfo(m_player_turn);
                }
            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[VersusBase::requestFinishShot][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            return ret;
        }


        public override void requestChangeMira(Player _session, packet _packet)
        {
            ////REQUEST_BEGIN("ChangeMira");

            var p = new PangyaBinaryWriter();

            try
            {

                float mira = _packet.ReadFloat();

                INIT_PLAYER_INFO("requestChangeMira",
                    "tentou mudar a mira[MIRA=" + Convert.ToString(mira) + "] no jogo",
                    _session, out PlayerGameInfo pgi);

                // _smp.message_pool.getInstance().push(new message("[VersusBase::requestChangeMira][Log] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] mira[VALUE=" + Convert.ToString(mira) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                pgi.location.r = mira;

                // Resposta para o Change mira
                p.init_plain(0x56);

                p.WriteInt32(pgi.oid);
                p.WriteFloat(pgi.location.r);

                packet_func.game_broadcast(this,
                    p, 1);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[VersusBase::requestChangeMira][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void requestChangeStateBarSpace(Player _session, packet _packet)
        {
            ////REQUEST_BEGIN("ChangeStateBarSpace");

            var p = new PangyaBinaryWriter();

            try
            {

                byte state = _packet.ReadByte();
                float point = _packet.ReadFloat();

                INIT_PLAYER_INFO("requestChangeStateBarSpace",
                    "tentou mudar o estado[STATE=" + Convert.ToString((ushort)state) + ", POINT=" + Convert.ToString(point) + "] da barra de espaco no jogo",
                    _session, out PlayerGameInfo pgi);

                if (!pgi.bar_space.setStateAndPoint(state, point))
                {
                    throw new exception("[VersusBase::requestChangeStateBarSpace][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou mudar o estado da barra de espaco[STATE=" + Convert.ToString((ushort)state) + ", POINT=" + Convert.ToString(point) + "] no jogo, mas o estado eh desconhecido, Hacker ou Bug. packet: " + _packet.Log(), ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                        5, 0));
                }

                if (state == 0 && pgi.tempo == 1)
                {

                    try
                    {

                        pgi.tempo = 0;

                        if (++pgi.data.time_out == 3)
                        {

                            var hole = m_course.findHole(pgi.hole) ?? throw new exception("[VersusBase::requestChangeStateBarSpace][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou mudar o estado da barra de espaco[STATE=" + Convert.ToString((ushort)state) + ", POINT=" + Convert.ToString(point) + "] no jogo, mas tentou encontrar o hole[HOLE=" + Convert.ToString((short)pgi.hole) + "] no course mas, nao encontrou. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                                    100, 0));
                            pgi.shot_sync.state_shot.display.acerto_hole = true;

                            pgi.data.tacada_num = hole.getPar().total_shot;

                            // Derruba player, tem que fazer isso no channel ou na sala, com o retorno dessa função
                            pgi.data.bad_condute = 3;
                        }

                    }
                    catch (exception e)
                    {

                        _smp.message_pool.getInstance().push(new message("[VersusBase::requestChangeStateBarSpace][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                    }
                    // Time Out
                    p.init_plain(0x5C);

                    p.WriteInt32(_session.m_oid);

                    packet_func.game_broadcast(this,
                        p, 1);
                }


            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[VersusBase::requestChangeStateBarSpace][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void requestActivePowerShot(Player _session, packet _packet)
        {
            ////REQUEST_BEGIN("ActivePowerShot");

            var p = new PangyaBinaryWriter();

            try
            {

                byte ps = _packet.ReadByte();

                INIT_PLAYER_INFO("requestActivePowerShot",
                    "tentou ativar power shot, no jogo",
                    _session, out PlayerGameInfo pgi);

                pgi.power_shot = ps;

                // Resposta para Active Power Shot
                p.init_plain(0x58);

                p.WriteInt32(_session.m_oid);
                p.WriteByte(pgi.power_shot);

                packet_func.game_broadcast(this,
                    p, 1);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[VersusBase::requestActivePowerShot][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void requestChangeClub(Player _session, packet _packet)
        {
            ////REQUEST_BEGIN("ChangeClub");

            var p = new PangyaBinaryWriter();

            try
            {

                byte club = _packet.ReadByte();

                INIT_PLAYER_INFO("requestChangeClub",
                    "tentou trocar taco no jogo",
                    _session, out PlayerGameInfo pgi);

                pgi.club = club;

                // Resposta para Change Club
                p.init_plain(0x59);

                p.WriteInt32(_session.m_oid);
                p.WriteByte(pgi.club);

                packet_func.game_broadcast(this,
                    p, 1);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[VersusBase::requestChangeClub][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void requestUseActiveItem(Player _session, packet _packet)
        {
            ////REQUEST_BEGIN("UseActiveItem");

            var p = new PangyaBinaryWriter();

            try
            {

                uint item_typeid = _packet.ReadUInt32();

                INIT_PLAYER_INFO("requestUseActiveItem",
                    "tentou usar item ativo no jogo",
                    _session, out PlayerGameInfo pgi);

                if (item_typeid == 0)
                {
                    throw new exception("[VersusBase::requestActiveItem][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou usar active item[TYPEID=" + Convert.ToString(item_typeid) + "] no jogo, mas o item_typeid eh invalido(zero). Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                        7, 0));
                }

                var iffItem = sIff.getInstance().findCommomItem(item_typeid) ?? throw new exception("[VersusBase::requestActiveItem][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + " tentou usar active item[TYPEID=" + Convert.ToString(item_typeid) + "] no jogo, mas o item nao tem no IFF_STRUCT. Hacker ou Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                        77, 0));


                if (sIff.getInstance().getItemGroupIdentify(item_typeid) != IFF_GROUP.ITEM || !sIff.getInstance().IsItemEquipable(item_typeid))
                {
                    throw new exception("[VersusBase::requestActiveItem][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou usar active item[TYPEID=" + Convert.ToString(item_typeid) + "] no jogo, mas o item nao eh equipavel(usar). Hacker ou Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                        78, 0));
                }

                if (item_typeid == MULLIGAN_ROSE_TYPEID)
                {
                    throw new exception("[VersusBase::requestActiveItem][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou usar active item[TYPEID=" + Convert.ToString(item_typeid) + "] no jogo, mas o item Mulligan Rose nao pode usar no VersusBase, so em TourneyBase. Hacker ou Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                        79, 0));
                }

                var pWi = _session.m_pi.findWarehouseItemByTypeid(item_typeid);

                if (pWi == null)
                {
                    throw new exception("[VersusBase::requestActiveItem][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou usar active item[TYPEID=" + Convert.ToString(item_typeid) + "] no jogo, mas ele nao tem esse item. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                        8, 0));
                }

                var it = pgi.used_item.v_active.find(pWi._typeid);

                if (it.Key <= 0)
                {
                    throw new exception("[VersusBase::requestActiveItem][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou usar active item[TYPEID=" + Convert.ToString(item_typeid) + "] no jogo, mas ele nao equipou esse item. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                        9, 0));
                }

                if (it.Value.count >= it.Value.v_slot.Count)
                {
                    throw new exception("[VersusBase::requestActiveItem][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou usar active item[TYPEID=" + Convert.ToString(item_typeid) + "] no jogo, mas ele ja usou todos os item desse que ele equipou. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                        10, 0));
                }

                // Add +1 ou countador
                it.Value.count++;

                // item que foi usado na tacada
                pgi.item_active_used_shot = pWi._typeid;

                // Resposta para o Use Active Item
                p.init_plain(0x5A);

                p.WriteUInt32(pWi._typeid);
                p.WriteInt32(new Random().Next()); // Seed Rand Failure Active Item
                p.WriteInt32(_session.m_oid);

                packet_func.game_broadcast(this,
                    p, 1);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[VersusBase::requestUseActiveItem][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void requestChangeStateTypeing(Player _session, packet _packet)
        {
            ////REQUEST_BEGIN("ChangeStateTypeing");

            var p = new PangyaBinaryWriter();

            try
            {

                short typeing = _packet.ReadInt16();

                INIT_PLAYER_INFO("requestChangeStateTypeing",
                    "tentou mudar o estado de escrevendo no jogo",
                    _session, out PlayerGameInfo pgi);

                pgi.typeing = typeing;

                // Resposta para Change State Typeing 
                p.init_plain(0x5D);

                p.WriteInt32(_session.m_oid);
                p.WriteInt16(pgi.typeing);

                packet_func.game_broadcast(this,
                    p, 1);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[VersusBase::requestChangeStateTypeing][Error] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void requestMoveBall(Player _session, packet _packet)
        {
            ////REQUEST_BEGIN("MoveBall");

            var p = new PangyaBinaryWriter();

            try
            {

                float x = _packet.ReadFloat();
                float y = _packet.ReadFloat();
                float z = _packet.ReadFloat();

                INIT_PLAYER_INFO("requestMoveBall",
                    "tentou recolocar a bola no jogo",
                    _session, out PlayerGameInfo pgi);

                pgi.location.x = x;
                pgi.location.y = y;
                pgi.location.z = z;

                // para o tempo do da tacada do player, que ele vai recolocar e come a um novo tempo depois
                stopTime();

                // Resposta para Move Ball
                p.init_plain(0x60);

                p.WriteFloat(pgi.location.x);
                p.WriteFloat(pgi.location.y);
                p.WriteFloat(pgi.location.z);

                packet_func.game_broadcast(this,
                    p, 1);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[VersusBase::requestMoveBall][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void requestChangeStateChatBlock(Player _session, packet _packet)
        {
            ////REQUEST_BEGIN("ChangeStateChatBlock");

            var p = new PangyaBinaryWriter();

            try
            {

                byte chat_block = _packet.ReadByte();

                INIT_PLAYER_INFO("requestChangeStateChatBlock",
                    "tentou mudar estado do chat block no jogo",
                    _session, out PlayerGameInfo pgi);

                pgi.chat_block = chat_block;

                // Resposta para Chat Block
                p.init_plain(0xAC);

                p.WriteInt32(_session.m_oid);
                p.WriteByte(pgi.chat_block);

                packet_func.game_broadcast(this,
                    p, 1);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[VersusBase::requestChangeStateChatBlock][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void requestActiveBooster(Player _session, packet _packet)
        {
            ////REQUEST_BEGIN("ActiveBooster");

            var p = new PangyaBinaryWriter();

            try
            {

                float velocidade = _packet.ReadFloat();

                INIT_PLAYER_INFO("requestActiveBooster",
                    "tentou ativar Time Booster no jogo",
                    _session, out PlayerGameInfo pgi);

                if (!_session.m_pi.m_cap.premium_user)
                { // (não é)!PREMIUM USER

                    var pWi = _session.m_pi.findWarehouseItemByTypeid(TIME_BOOSTER_TYPEID);

                    if (pWi == null)
                    {
                        throw new exception("[VersusBase::requestActiveBooster][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou ativar time booster, mas ele nao tem o item passive. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                            11, 0));
                    }

                    if (pWi.STDA_C_ITEM_QNTD <= 0)
                    {
                        throw new exception("[VersusBase::requestActiveBooster][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou ativar time booster, mas ele nao tem quantidade suficiente[VALUE=" + Convert.ToString(pWi.STDA_C_ITEM_QNTD) + ", REQUEST=1] do item de time booster.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                            12, 0));
                    }

                    var it = pgi.used_item.v_passive.find(pWi._typeid);

                    if (it.Value == null)
                    {
                        throw new exception("[VersusBase::requestActiveBooster][Error] PLAYER[UID = " + Convert.ToString(_session.m_pi.uid) + "] tentou ativar time booster, mas ele nao tem ele no item passive usados do server. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                            13, 0));
                    }

                    if ((short)it.Value.count >= pWi.STDA_C_ITEM_QNTD)
                    {
                        throw new exception("[VersusBase::requestActiveBooster][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou ativar time booster, mas ele ja usou todos os time booster. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
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
                p.WriteInt32(_session.m_oid);

                packet_func.game_broadcast(this,
                    p, 1);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[VersusBase::requestActiveBooster][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void requestActiveReplay(Player _session, packet _packet)
        {
            ////REQUEST_BEGIN("ActiveReplay");

            var p = new PangyaBinaryWriter();

            try
            {

                uint _typeid = _packet.ReadUInt32();

                if (_typeid == 0)
                {
                    throw new exception("[VersusBase::requestActiveReplay][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou ativar Replay[TYPEID=" + Convert.ToString(_typeid) + "], mas o typeid eh invalido(zero). Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                        200, 0));
                }

                var pWi = _session.m_pi.findWarehouseItemByTypeid(_typeid);

                if (pWi == null)
                {
                    throw new exception("[VersusBase::requestActiveReplay][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou ativar Replay[TYPEID=" + Convert.ToString(_typeid) + "], mas ele nao tem o item. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                        201, 0));
                }

                if (pWi.STDA_C_ITEM_QNTD <= 0)
                {
                    throw new exception("[VersusBase::requestActiveReplay][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou ativar Replay[TYPEID=" + Convert.ToString(_typeid) + "], mas ele nao tem quantidade suficiente[VALUE=" + Convert.ToString(pWi.STDA_C_ITEM_QNTD) + ", REQUEST=1] do item. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
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

                if (ItemManager.removeItem(item, _session) <= 0)
                {
                    throw new exception("[VersusBase::requestActiveReplay][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou ativar Replay[TYPEID=" + Convert.ToString(_typeid) + "], nao conseguiu deletar ou atualizar qntd do item[TYPEID=" + Convert.ToString(item._typeid) + ", ID=" + Convert.ToString(item.id) + "]", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                        203, 0));
                }
                 

                // UPDATE ON GAME
                // Resposta para o Active Replay
                p.init_plain(0xA4);

                p.WriteUInt16((ushort)item.stat.qntd_dep);

                packet_func.game_broadcast(this,
                    p, 1);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[VersusBase::requestActiveReplay][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void requestActiveCutin(Player _session, packet _packet)
        { 
            var p = new PangyaBinaryWriter();

            try
            {

                stActiveCutin ac = new stActiveCutin
                {
                    uid = _packet.ReadUInt32(),
                    tipo = _packet.ReadUInt32(),
                    opt = _packet.ReadUInt16(),
                    char_typeid = _packet.ReadUInt32(),
                    active = _packet.ReadByte()
                };

                Player s = null;

                if (ac.uid != _session.m_pi.uid || (s = findSessionByUID(ac.uid)) == null)
                {
                    throw new exception("[VersusBase::requestActiveCutin][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou activar cutin[CHAR_TYPEID=" + Convert.ToString(ac.char_typeid) + ", TIPO=" + Convert.ToString(ac.tipo) + ", OPT=" + Convert.ToString(ac.opt) + ",  ACTIVE=" + Convert.ToString(ac.active) + "] de um PLAYER[UID=" + Convert.ToString(ac.uid) + "], mas o jogador nao esta no jogo. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                        1, 0x5200101));
                }

                if (s.m_pi.ei.char_info == null)
                {
                    throw new exception("[VersusBase::requestActiveCutin][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou activar cutin[CHAR_TYPEID=" + Convert.ToString(ac.char_typeid) + ", TIPO=" + Convert.ToString(ac.tipo) + ", OPT=" + Convert.ToString(ac.opt) + ",  ACTIVE=" + Convert.ToString(ac.active) + "] de um PLAYER[UID=" + Convert.ToString(ac.uid) + "], mas o jogador nao tem um character equipado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                        2, 0x5200102));
                }

                CutinInformation pCutin = null;

                // Cutin Padrão que o player equipa, quando o cliente envia o cutin type é que é efeito por roupas equipadas
                if (sIff.getInstance().getItemGroupIdentify(ac.char_typeid) == IFF_GROUP.CHARACTER && ac.active == 1)
                {

                    if (s.m_pi.ei.char_info._typeid != ac.char_typeid)
                    {
                        throw new exception("[VersusBase::requestActiveCutin][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou activar cutin[CHAR_TYPEID=" + Convert.ToString(ac.char_typeid) + ", TIPO=" + Convert.ToString(ac.tipo) + ", OPT=" + Convert.ToString(ac.opt) + ",  ACTIVE=" + Convert.ToString(ac.active) + "] de um PLAYER[UID=" + Convert.ToString(ac.uid) + "], mas o character typeid passado nao eh igual ao equipado do player. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                            4, 0x5200104));
                    }

                    WarehouseItemEx pWi = null;

                    var end = (s.m_pi.ei.char_info.cut_in.Length);

                    for (var i = 0; i < end; ++i)
                    {

                        if (s.m_pi.ei.char_info.cut_in[i] > 0)
                        {

                            if ((pWi = _session.m_pi.findWarehouseItemById((int)s.m_pi.ei.char_info.cut_in[i])) != null)
                            {

                                if ((pCutin = sIff.getInstance().findCutinInfomation(pWi._typeid)) == null || pCutin._typeid == 0)
                                {
                                    throw new exception("[VersusBase::requestActiveCutin][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou activar cutin[CHAR_TYPEID=" + Convert.ToString(ac.char_typeid) + ", TIPO=" + Convert.ToString(ac.tipo) + ", OPT=" + Convert.ToString(ac.opt) + ", ACTIVE=" + Convert.ToString(ac.active) + "] de um PLAYER[UID=" + Convert.ToString(ac.uid) + "], mas o jogador nao tem esse cutin[TYPEID=" + Convert.ToString(pWi._typeid) + ", ID=" + Convert.ToString(pWi.id) + "]. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                                        3, 0x5200103));
                                }

                                if (pCutin.tipo.ulCondition == ac.tipo)
                                {
                                    break;
                                }
                                else if ((i + 1) == end)
                                {
                                    throw new exception("[VersusBase::requestActiveCutin][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou activar cutin[CHAR_TYPEID=" + Convert.ToString(ac.char_typeid) + ", TIPO=" + Convert.ToString(ac.tipo) + ", OPT=" + Convert.ToString(ac.opt) + ",  ACTIVE=" + Convert.ToString(ac.active) + "] de um PLAYER[UID=" + Convert.ToString(ac.uid) + "], mas o jogador nao tem esse cutin[TYPEID=" + Convert.ToString(pWi._typeid) + ", ID=" + Convert.ToString(pWi.id) + "]. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                                        3, 0x5200103));
                                }
                            }
                        }
                    }

                }
                else if (sIff.getInstance().getItemGroupIdentify(ac.char_typeid) == IFF_GROUP.SKIN && ac.active == 0)
                { 
                    if ((pCutin = sIff.getInstance().findCutinInfomation(ac.char_typeid)) == null)
                    {
                        throw new exception("[VersusBase::requestActiveCutin][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou activar cutin[CHAR_TYPEID=" + Convert.ToString(ac.char_typeid) + ", TIPO=" + Convert.ToString(ac.tipo) + ", OPT=" + Convert.ToString(ac.opt) + ",  ACTIVE=" + Convert.ToString(ac.active) + "] de um PLAYER[UID=" + Convert.ToString(ac.uid) + "], mas o jogador nao tem esse cutin[TYPEID=" + Convert.ToString(ac.char_typeid) + "]. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                            3, 0x5200103));
                    }
                }

                if (pCutin == null || pCutin._typeid == 0)
                {
                    throw new exception("[VersusBase::requestActiveCutin][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou activar cutin[CHAR_TYPEID=" + Convert.ToString(ac.char_typeid) + ", TIPO=" + Convert.ToString(ac.tipo) + ", OPT=" + Convert.ToString(ac.opt) + ",  ACTIVE=" + Convert.ToString(ac.active) + "] de um PLAYER[UID=" + Convert.ToString(ac.uid) + "], mas o cution nao foi encontrado[TYPEID=" + Convert.ToString(ac.char_typeid) + "]. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY_BASE,
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

                packet_func.game_broadcast(this,
                    p, 1);

                // No Modo GrandZodic, não envia Cutin, então envia o pacote18D com option 0(Byte), e valor 3(UInt16)

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[VersusBase::requestActiveCutin][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x18D);

                p.WriteByte(0); // OPT

                p.WriteUInt16(1); // Error

                packet_func.session_send(p,
                    _session, 1);
            }
        }


        public override void requestActiveRing(Player _session, packet _packet)
        {
            ////REQUEST_BEGIN("ActiveRing");

            var p = new PangyaBinaryWriter();

            try
            {

                stRing r = new stRing
                {
                    _typeid = _packet.ReadUInt32(),
                    effect_value = _packet.ReadUInt32(),
                    efeito = _packet.ReadByte()
                };

                if (r._typeid == 0)
                {
                    throw new exception("[VersusBase::requestActiveRing][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou ativar Anel[TYPEID=" + Convert.ToString(r._typeid) + "], mas o typeid eh invalido(zero). Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                        30, 0x330001));
                }

                var pWi = _session.m_pi.findWarehouseItemByTypeid(r._typeid);

                if (pWi == null)
                {
                    throw new exception("[VersusBase::requestActiveRing][Error] PLAYER[UID = " + Convert.ToString(_session.m_pi.uid) + "] tentou ativar Anel[TYPEID = " + Convert.ToString(r._typeid) + "], mas ele nao tem o anel. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                        31, 0x330002));
                }

                if (_session.m_pi.ei.char_info == null)
                {
                    throw new exception("[VersusBase::requestActiveRing][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou ativar Anel[TYPEID=" + Convert.ToString(r._typeid) + "], mas ele nao esta com um Character equipado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                        32, 0x330003));
                }

                if (!_session.m_pi.ei.char_info.auxparts.Any(c => c == r._typeid))
                {
                    throw new exception("[VersusBase::requestActiveRing][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou ativar Anel[TYPEID=" + Convert.ToString(r._typeid) + "], mas ele nao esta equipado com o anel. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                        33, 0x330004));
                }

                // Adiciona o efeito que foi ativado
                checkEffectItemAndSet(_session, r._typeid);

                // Resposta para o cliente
                p.init_plain(0x237);

                p.WriteUInt32(0); // OK

                p.WriteUInt32(_session.m_pi.uid);

                p.WriteUInt32(r._typeid);
                p.WriteByte(r.efeito);

                packet_func.game_broadcast(this,
                    p, 1);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[VersusBase::requestActiveRing][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Resposta Error
                p.init_plain(0x237);

                p.WriteUInt32((ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) == (uint)STDA_ERROR_TYPE.VERSUS_BASE) ? ExceptionError.STDA_SYSTEM_ERROR_DECODE(e.getCodeError()) : 0x330000);

                packet_func.session_send(p,
                    _session, 1);
            }
        }


        public override void requestActiveRingGround(Player _session, packet _packet)
        {
            ////REQUEST_BEGIN("ActiveRingGround");

            var p = new PangyaBinaryWriter();

            try
            {

                stRingGround rg = new stRingGround
                {
                    efeito = (AbilityEffect)_packet.ReadUInt32()
                };
                rg.ring[0] = _packet.ReadUInt32();
                rg.ring[1] = _packet.ReadUInt32();
                rg.option = _packet.ReadUInt32();

                // Log para saber qual é o efeito 31(0x1F)
                if (rg.efeito == AbilityEffect.UNKNOWN_31)//efeito 31, e o taco
                {
                    _smp.message_pool.getInstance().push(new message("[VersusBase::requestActiveRingGround][Log] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] ativou o efeito 0x1F(31) com os itens[TYPEID_1=" + Convert.ToString(rg.ring[0]) + ", TYPEID_2=" + Convert.ToString(rg.ring[1]) + "] e OPTION=" + Convert.ToString(rg.option), type_msg.CL_FILE_LOG_AND_CONSOLE));
                }

                if (!rg.isValid())
                {
                    throw new exception("[VersusBase::requestActiveRingGround][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou ativar Anel de Terreno[TYPE=" + Convert.ToString(rg.efeito) + ", RING[0]=" + Convert.ToString(rg.ring[0]) + ", RING[1]=" + Convert.ToString(rg.ring[1]) + ", OPTION=" + Convert.ToString(rg.option) + "], mas os typeid's nao sao validos. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY_BASE,
                        50, 0x340001));
                }

                if (_session.m_pi.ei.char_info == null)
                {
                    throw new exception("[VersusBase::requestActiveRingGround][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou ativar Anel de Terreno[TYPE=" + Convert.ToString(rg.efeito) + ", RING[0]=" + Convert.ToString(rg.ring[0]) + ", RING[1]=" + Convert.ToString(rg.ring[1]) + ", OPTION=" + Convert.ToString(rg.option) + "], mas ele nao esta com um Character equipado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY_BASE,
                        51, 0x340002));
                }

                if (sIff.getInstance().getItemGroupIdentify(rg.ring[0]) == IFF_GROUP.AUX_PART)
                { // Anel

                    var pRing = _session.m_pi.findWarehouseItemByTypeid(rg.ring[0]);

                    if (pRing == null)
                    {
                        throw new exception("[VersusBase::requestActiveRingGround][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou ativar Anel de Terreno[TYPE=" + Convert.ToString(rg.efeito) + ", RING[0]=" + Convert.ToString(rg.ring[0]) + ", RING[1]=" + Convert.ToString(rg.ring[1]) + ", OPTION=" + Convert.ToString(rg.option) + "], mas ele nao tem o Anel[0]. hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY_BASE,
                            52, 0x340002));
                    }

                    if (!_session.m_pi.ei.char_info.auxparts.Any(c => c == rg.ring[0]))
                    {
                        throw new exception("[VersusBase::requestActiveRingGround][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou ativar Anel de Terreno[TYPE=" + Convert.ToString(rg.efeito) + ", RING[0]=" + Convert.ToString(rg.ring[0]) + ", RING[1]=" + Convert.ToString(rg.ring[1]) + ", OPTION=" + Convert.ToString(rg.option) + "], mas ele nao esta com o Anel[0] equipado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY_BASE,
                            53, 0x340003));
                    }

                    if (rg.ring[0] != rg.ring[1])
                    { // Ativou Habilidade em conjunto 2 aneis

                        var pRing2 = _session.m_pi.findWarehouseItemByTypeid(rg.ring[1]);

                        if (pRing2 == null)
                        {
                            throw new exception("[VersusBase::requestActiveRingGround][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou ativar Anel de Terreno[TYPE=" + Convert.ToString(rg.efeito) + ", RING[0]=" + Convert.ToString(rg.ring[0]) + ", RING[1]=" + Convert.ToString(rg.ring[1]) + ", OPTION=" + Convert.ToString(rg.option) + "], mas ele nao tem o Anel[1]. hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY_BASE,
                                52, 0x340002));
                        }

                        if (!_session.m_pi.ei.char_info.auxparts.Any(c => c == rg.ring[1]))
                        {
                            throw new exception("[VersusBase::requestActiveRingGround][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou ativar Anel de Terreno[TYPE=" + Convert.ToString(rg.efeito) + ", RING[0]=" + Convert.ToString(rg.ring[0]) + ", RING[1]=" + Convert.ToString(rg.ring[1]) + ", OPTION=" + Convert.ToString(rg.option) + "], mas ele nao esta com o Anel[1] equipado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY_BASE,
                                53, 0x340003));
                        }
                    }

                }
                else if (sIff.getInstance().getItemGroupIdentify(rg.ring[0]) == IFF_GROUP.PART)
                { // Part

                    var pRing = _session.m_pi.findWarehouseItemByTypeid(rg.ring[0]);

                    if (pRing == null)
                    {
                        throw new exception("[VersusBase::requestActiveRingGround][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou ativar Anel de Terreno[TYPE=" + Convert.ToString(rg.efeito) + ", RING[0]=" + Convert.ToString(rg.ring[0]) + ", RING[1]=" + Convert.ToString(rg.ring[1]) + ", OPTION=" + Convert.ToString(rg.option) + "], mas ele nao tem o Part[0]. hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY_BASE,
                            52, 0x340002));
                    }
                    //etava como aux ring
                    if (!_session.m_pi.ei.char_info.parts_typeid.Any(c => c == rg.ring[0]))
                    {
                        throw new exception("[VersusBase::requestActiveRingGround][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou ativar Anel de Terreno[TYPE=" + Convert.ToString(rg.efeito) + ", RING[0]=" + Convert.ToString(rg.ring[0]) + ", RING[1]=" + Convert.ToString(rg.ring[1]) + ", OPTION=" + Convert.ToString(rg.option) + "], mas ele nao esta com o Part[0] equipado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY_BASE,
                            53, 0x340003));
                    }

                    if (rg.ring[0] != rg.ring[1])
                    { // Ativou Habilidade em conjunto 2 aneis

                        var pRing2 = _session.m_pi.findWarehouseItemByTypeid(rg.ring[1]);

                        if (pRing2 == null)
                        {
                            throw new exception("[VersusBase::requestActiveRingGround][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou ativar Anel de Terreno[TYPE=" + Convert.ToString(rg.efeito) + ", RING[0]=" + Convert.ToString(rg.ring[0]) + ", RING[1]=" + Convert.ToString(rg.ring[1]) + ", OPTION=" + Convert.ToString(rg.option) + "], mas ele nao tem o Part[1]. hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY_BASE,
                                52, 0x340002));
                        }

                        if (!_session.m_pi.ei.char_info.auxparts.Any(c => c == rg.ring[1]))
                        {
                            throw new exception("[VersusBase::requestActiveRingGround][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou ativar Anel de Terreno[TYPE=" + Convert.ToString(rg.efeito) + ", RING[0]=" + Convert.ToString(rg.ring[0]) + ", RING[1]=" + Convert.ToString(rg.ring[1]) + ", OPTION=" + Convert.ToString(rg.option) + "], mas ele nao esta com o Part[1] equipado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY_BASE,
                                53, 0x340003));
                        }
                    }

                }
                else if (sIff.getInstance().getItemGroupIdentify(rg.ring[0]) == IFF_GROUP.MASCOT)
                {

                    var pMascot = _session.m_pi.findMascotByTypeid(rg.ring[0]);

                    if (pMascot == null)
                    {
                        throw new exception("[VersusBase::requestActiveRingGround][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou ativar Anel de Terreno[TYPE=" + Convert.ToString(rg.efeito) + ", RING[0]=" + Convert.ToString(rg.ring[0]) + ", RING[1]=" + Convert.ToString(rg.ring[1]) + ", OPTION=" + Convert.ToString(rg.option) + "], mas ele nao tem o Mascot[0]. hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY_BASE,
                            52, 0x340002));
                    }

                    if (rg.ring[0] != rg.ring[1])
                    { // Ativou Habilidade em conjunto 2 aneis

                        var pPart2 = _session.m_pi.findWarehouseItemByTypeid(rg.ring[1]);

                        if (pPart2 == null)
                        {
                            throw new exception("[VersusBase::requestActiveRingGround][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou ativar Anel de Terreno[TYPE=" + Convert.ToString(rg.efeito) + ", RING[0]=" + Convert.ToString(rg.ring[0]) + ", RING[1]=" + Convert.ToString(rg.ring[1]) + ", OPTION=" + Convert.ToString(rg.option) + "], mas ele nao tem o Part[1]. hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY_BASE,
                                52, 0x340002));
                        }

                        if (!_session.m_pi.ei.char_info.parts_typeid.Any(c => c == rg.ring[1]))
                        {
                            throw new exception("[VersusBase::requestActiveRingGround][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou ativar Anel de Terreno[TYPE=" + Convert.ToString(rg.efeito) + ", RING[0]=" + Convert.ToString(rg.ring[0]) + ", RING[1]=" + Convert.ToString(rg.ring[1]) + ", OPTION=" + Convert.ToString(rg.option) + "], mas ele nao esta com o Part[1] equipado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY_BASE,
                                53, 0x340003));
                        }
                    }
                }

                // Adiciona o efeito que foi ativado 
                setEffectActiveInShot(_session, enumToBitValue(rg.efeito));

                // Resposta para o Active Ring Terreno
                p.init_plain(0x266);

                p.WriteUInt32(0); // OK

                p.WriteBytes(rg.ToArray());

                p.WriteUInt32(_session.m_pi.uid);

                packet_func.session_send(p,
                    _session, 1);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[VersusBase::requestActiveRingGround][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Resposta Error
                p.init_plain(0x266);

                p.WriteUInt32((ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) == STDA_ERROR_TYPE.TOURNEY_BASE) ? ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) : 0x340000);

                packet_func.session_send(p,
                    _session, 1);
            }
        }

        public override void requestActiveRingPawsRainbowJP(Player _session, packet _packet)
        {
            ////REQUEST_BEGIN("ActiveRingPawsRainbowJP");

            var p = new PangyaBinaryWriter();

            try
            {

                // Efeito patinha não passa o TYPEID do item que ativou
                setEffectActiveInShot(_session, enumToBitValue(AbilityEffect.PAWS_ACCUMULATE));

                // Resposta para o Active Ring Paws Rainbow JP
                p.init_plain(0x27E);

                p.WriteUInt32(_session.m_pi.uid);

                packet_func.game_broadcast(this,
                    p, 1);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[VersusBase::requestActiveRingPawsRainbowJP][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void requestActiveRingPawsRingSetJP(Player _session, packet _packet)
        {
            ////REQUEST_BEGIN("ActiveRingPawsRingSetJP");

            var p = new PangyaBinaryWriter();

            try
            {

                // Efeito patinha não passa o TYPEID do item que ativou
                setEffectActiveInShot(_session, enumToBitValue(AbilityEffect.PAWS_NOT_ACCUMULATE));

                // Resposta para o Active Ring Paws Ring Set JP
                p.init_plain(0x281);

                p.WriteUInt32(_session.m_pi.uid);

                packet_func.game_broadcast(this,
                    p, 1);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[VersusBase::requestActiveRingPawsRingSetJP][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void requestActiveRingPowerGagueJP(Player _session, packet _packet)
        {
            ////REQUEST_BEGIN("ActiveRingPowerGagueJP");

            var p = new PangyaBinaryWriter();

            try
            {

                stRingPowerGagueJP rpg = new stRingPowerGagueJP();
                rpg.efeito = _packet.ReadUInt32();
                rpg.ring[0] = _packet.ReadUInt32();
                rpg.ring[1] = _packet.ReadUInt32();
                rpg.option = _packet.ReadUInt32();

                if (!rpg.isValid())
                {
                    throw new exception("[VersusBase::requestActiveRingPowerGagueJP][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou ativar Anel de Barra de PS [JP] [TYPE=" + Convert.ToString(rpg.efeito) + ", RING[0]=" + Convert.ToString(rpg.ring[0]) + ", RING[1]=" + Convert.ToString(rpg.ring[1]) + ", OPTION=" + Convert.ToString(rpg.option) + "], mas os typeid's nao sao validos. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                        150, 0x390001));
                }

                if (_session.m_pi.ei.char_info == null)
                {
                    throw new exception("[VersusBase::requestActiveRingPowerGagueJP][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou ativar Anel de Barra de PS [JP] [TYPE=" + Convert.ToString(rpg.efeito) + ", RING[0]=" + Convert.ToString(rpg.ring[0]) + ", RING[1]=" + Convert.ToString(rpg.ring[1]) + ", OPTION=" + Convert.ToString(rpg.option) + "], mas ele nao esta com um Character equipado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                        151, 0x390002));
                }

                var pRing = _session.m_pi.findWarehouseItemByTypeid(rpg.ring[0]);

                if (pRing == null)
                {
                    throw new exception("[VersusBase::requestActiveRingPowerGagueJP][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou ativar Anel de Barra de PS [JP] [TYPE=" + Convert.ToString(rpg.efeito) + ", RING[0]=" + Convert.ToString(rpg.ring[0]) + ", RING[1]=" + Convert.ToString(rpg.ring[1]) + ", OPTION=" + Convert.ToString(rpg.option) + "], mas ele nao tem o Anel[0]. hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                        152, 0x390002));
                }

                if (!_session.m_pi.ei.char_info.auxparts.Any(c => c == rpg.ring[0]))
                {
                    throw new exception("[VersusBase::requestActiveRingPowerGagueJP][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou ativar Anel de Barra de PS [JP] [TYPE=" + Convert.ToString(rpg.efeito) + ", RING[0]=" + Convert.ToString(rpg.ring[0]) + ", RING[1]=" + Convert.ToString(rpg.ring[1]) + ", OPTION=" + Convert.ToString(rpg.option) + "], mas ele nao esta com o Anel[0] equipado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                        153, 0x390003));
                }

                if (rpg.ring[0] != rpg.ring[1])
                { // Ativou Habilidade em conjunto 2 aneis

                    var pRing2 = _session.m_pi.findWarehouseItemByTypeid(rpg.ring[1]);

                    if (pRing2 == null)
                    {
                        throw new exception("[VersusBase::requestActiveRingPowerGagueJP][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou ativar Anel de Barra de PS [JP] [TYPE=" + Convert.ToString(rpg.efeito) + ", RING[0]=" + Convert.ToString(rpg.ring[0]) + ", RING[1]=" + Convert.ToString(rpg.ring[1]) + ", OPTION=" + Convert.ToString(rpg.option) + "], mas ele nao tem o Anel[1]. hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                            152, 0x390002));
                    }

                    if (!_session.m_pi.ei.char_info.auxparts.Any(c => c == rpg.ring[1]))

                    {
                        throw new exception("[VersusBase::requestActiveRingPowerGagueJP][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou ativar Anel de Barra de PS [JP] [TYPE=" + Convert.ToString(rpg.efeito) + ", RING[0]=" + Convert.ToString(rpg.ring[0]) + ", RING[1]=" + Convert.ToString(rpg.ring[1]) + ", OPTION=" + Convert.ToString(rpg.option) + "], mas ele nao esta com o Anel[1] equipado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                            153, 0x390003));
                    }
                }

                // Effect
                setEffectActiveInShot(_session, enumToBitValue(AbilityEffect.POWER_GAUGE_FREE));

                // Resposta para o Active Ring Power Gague JP
                p.init_plain(0x27F);

                p.WriteUInt32(_session.m_pi.uid);

                packet_func.game_broadcast(this,
                    p, 1);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[VersusBase::requestActiveRingPowerGagueJP][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        } 

        public override void requestActiveRingMiracleSignJP(Player _session, packet _packet)
        {
            ////REQUEST_BEGIN("ActiveRingMiracleSign");

            var p = new PangyaBinaryWriter();

            try
            {

                uint _typeid = _packet.ReadUInt32();

                if (_typeid == 0)
                {
                    throw new exception("[VersusBase::requestActiveRingMiracleSignJP][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou ativar 'Anel'[TYPEID=" + Convert.ToString(_typeid) + "] Olho Magico JP, mas o typeid eh invalido(zero)", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                        70, 0x350001));
                }

                WarehouseItemEx pWi = _session.m_pi.findWarehouseItemByTypeid(_typeid);

                if (pWi == null)
                {
                    throw new exception("[VersusBase::requestActiveRingMiracleSignJP][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou ativar 'Anel'[TYPEID=" + Convert.ToString(_typeid) + "] Olho Magico JP, mas ele nao tem o 'Anel'. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                        71, 0x350002));
                }

                if (_session.m_pi.ei.char_info == null)
                {
                    throw new exception("[VersusBase::requestActiveRingMiracleSignJP][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou ativar 'Anel'[TYPEID=" + Convert.ToString(_typeid) + "] Olho Magico JP, mas ele nao esta com um Character equipado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                        72, 0x350003));
                }

                if (sIff.getInstance().getItemGroupIdentify(_typeid) == IFF_GROUP.AUX_PART)
                { // Anel

                    if (!_session.m_pi.ei.char_info.auxparts.Any(c => c ==
                        _typeid))
                    {
                        throw new exception("[VersusBase::requestActiveRingMiracleSignJP][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou ativar 'Anel'[TYPEID=" + Convert.ToString(_typeid) + "] Olho Magico JP, mas ele nao esta com o Anel equipado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                            0x73, 0x350004));
                    }

                }
                else if (sIff.getInstance().getItemGroupIdentify(_typeid) == IFF_GROUP.PART)
                { // Part

                    if (!_session.m_pi.ei.char_info.parts_typeid.Any(c => c ==
                        _typeid))
                    {
                        throw new exception("[VersusBase::requestActiveRingMiracleSignJP][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou ativar 'Anel'[TYPEID=" + Convert.ToString(_typeid) + "] Olho Magico JP, mas ele nao esta com a Part equipado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                            74, 0x350005));
                    }

                } // else Item Passive, o item do assist, mas acho que ele n o chame esse, ele chama o proprio pacote dele

                // Effect
                setEffectActiveInShot(_session, enumToBitValue(AbilityEffect.MIRACLE_SIGN_RANDOM));

                // Resposta para o Active Ring Miracle Sign JP
                p.init_plain(0x280);

                p.WriteUInt32(0); // OK;

                p.WriteUInt32(_typeid);
                p.WriteUInt32(_session.m_pi.uid);

                packet_func.game_broadcast(this,
                    p, 1);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[VersusBase::requestActiveRingMiracleSign][ErroSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Resposta Error
                p.init_plain(0x280);

                p.WriteUInt32((ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) == (uint)STDA_ERROR_TYPE.VERSUS_BASE) ? ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) : 0x350000);

                packet_func.session_send(p,
                    _session, 1);
            }
        }

        public override void requestActiveWing(Player _session, packet _packet)
        {
            ////REQUEST_BEGIN("ActiveWing");

            var p = new PangyaBinaryWriter();

            try
            {

                uint _typeid = _packet.ReadUInt32();

                if (_typeid == 0)
                {
                    throw new exception("[VersusBase::ActiveWing][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou ativar Asa[TYPEID=" + Convert.ToString(_typeid) + "], mas o typeid eh invalido(zero)", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                        90, 0x360001));
                }

                var pWi = _session.m_pi.findWarehouseItemByTypeid(_typeid);

                if (pWi == null)
                {
                    throw new exception("[VersusBase::ActiveWing][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou ativar Asa[TYPEID=" + Convert.ToString(_typeid) + "], mas ele nao tem esse item 'Asa', Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                        91, 0x360002));
                }

                if (_session.m_pi.ei.char_info == null)
                {
                    throw new exception("[VersusBase::ActiveWing][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou ativar Asa[TYPEID=" + Convert.ToString(_typeid) + "], mas ele nao esta com um Character equipado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                        92, 0x360003));
                }

                if (!_session.m_pi.ei.char_info.parts_typeid.Any(c => c == _typeid))
                {
                    throw new exception("[VersusBase::ActiveWing][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou ativar Asa[TYPEID=" + Convert.ToString(_typeid) + "], mas ele nao esta com o item 'Asa' equipado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                        93, 0x360004));
                }

                // Adiciona o efeito que foi ativado
                checkEffectItemAndSet(_session, _typeid);

                // Resposta para o Active Wing
                p.init_plain(0x203);

                p.WriteUInt32(_session.m_pi.uid);

                p.WriteUInt32(_typeid);

                packet_func.game_broadcast(this,
                    p, 1);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[VersusBase::ActiveWing][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void requestActivePaws(Player _session, packet _packet)
        {
            ////REQUEST_BEGIN("ActivePaws");

            var p = new PangyaBinaryWriter();

            try
            {

                // Efeito patinha não passa o TYPEID do item que ativou, Animal Ring(Anel) ou Patinha
                setEffectActiveInShot(_session, enumToBitValue(AbilityEffect.PAWS_NOT_ACCUMULATE));

                // Resposta para o Active Paws
                p.init_plain(0x236);

                p.WriteUInt32(_session.m_pi.uid);

                packet_func.game_broadcast(this,
                    p, 1);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[VersusBase::requestActivePaws][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void requestActiveGlove(Player _session, packet _packet)
        {
            ////REQUEST_BEGIN("ActiveGlove");

            var p = new PangyaBinaryWriter();

            try
            {

                uint _typeid = _packet.ReadUInt32();

                if (_typeid == 0)
                {
                    throw new exception("[VersusBase::requestActiveGlove][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou ativar Luva[TYPEID=" + Convert.ToString(_typeid) + "], mas o typeid eh invalido(zero). Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                        110, 0x370001));
                }

                var pWi = _session.m_pi.findWarehouseItemByTypeid(_typeid);

                if (pWi == null)
                {
                    throw new exception("[VersusBase::requestActiveGlove][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou ativar Luva[TYPEID=" + Convert.ToString(_typeid) + "], mas ele nao tem esse item 'Luva'. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                        111, 0x370002));
                }

                if (_session.m_pi.ei.char_info == null)
                {
                    throw new exception("[VersusBase::requestActiveGlove][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou ativar Luva[TYPEID=" + Convert.ToString(_typeid) + "], mas ele nao esta com um Character equipado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                        112, 0x370003));
                }

                if (sIff.getInstance().getItemGroupIdentify(_typeid) == IFF_GROUP.PART)
                { // Luva

                    if (!_session.m_pi.ei.char_info.parts_typeid.Any(c => c ==
                        _typeid))
                    {
                        throw new exception("[VersusBase::requestActiveGlove][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou ativar Luva[TYPEID=" + Convert.ToString(_typeid) + "], mas ele nao tem a Luva equipada. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                            113, 0x370004));
                    }

                }
                else if (sIff.getInstance().getItemGroupIdentify(_typeid) == IFF_GROUP.AUX_PART)
                { // Anel

                    if (!_session.m_pi.ei.char_info.auxparts.Any(c => c ==
                        _typeid))
                    {
                        throw new exception("[VersusBase::requestActiveGlove][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou ativar Luva[TYPEID=" + Convert.ToString(_typeid) + "], mas ele nao tem o Anel equipado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                            114, 0x370005));
                    }
                }

                // Adiciona o efeito que foi ativado
                checkEffectItemAndSet(_session, _typeid);

                // Resposta para o Active Glove
                p.init_plain(0x265);

                p.WriteUInt32(0); // OK

                p.WriteUInt32(_typeid);

                p.WriteUInt32(_session.m_pi.uid);

                packet_func.game_broadcast(this,
                    p, 1);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[VersusBase::requestActiveGlove][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Resposta Error
                p.init_plain(0x265);

                p.WriteUInt32((ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) == (uint)STDA_ERROR_TYPE.VERSUS_BASE) ? ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) : 0x370000);

                packet_func.session_send(p,
                    _session, 1);
            }
        }

        public override void requestActiveEarcuff(Player _session, packet _packet)
        {
            var p = new PangyaBinaryWriter();

            try
            {

                stEarcuff ec = new stEarcuff();
                ec._typeid = _packet.ReadUInt32();
                ec.angle = _packet.ReadByte();
                ec.x_point_angle = _packet.ReadSingle();

                if (ec._typeid == 0)
                {
                    throw new exception("[VersusBase::ActiveEarcuff][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou ativar Earcuff'Mascot'[TYPEID=" + Convert.ToString(ec._typeid) + ", ANGLE_SENTIDO=" + Convert.ToString((ushort)ec.angle) + ", X_ANGLE=" + Convert.ToString(ec.x_point_angle) + "], mas o typeid eh invalido. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                        130, 0x380001));
                }

                if (sIff.getInstance().getItemGroupIdentify(ec._typeid) ==IFF_GROUP.PART)
                { // Earcuff

                    if (_session.m_pi.ei.char_info == null)
                    {
                        throw new exception("[VersusBase::ActiveEarcuff][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou ativar Earcuff[TYPEID=" + Convert.ToString(ec._typeid) + ", ANGLE_SENTIDO=" + Convert.ToString((ushort)ec.angle) + ", X_ANGLE=" + Convert.ToString(ec.x_point_angle) + "], mas ele nao esta com um Character equipado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                            131, 0x380002));
                    }

                    var pWi = _session.m_pi.findWarehouseItemByTypeid(ec._typeid);

                    if (pWi == null)
                    {
                        throw new exception("[VersusBase::ActiveEarcuff][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou ativar Earcuff[TYPEID=" + Convert.ToString(ec._typeid) + ", ANGLE_SENTIDO=" + Convert.ToString((ushort)ec.angle) + ", X_ANGLE=" + Convert.ToString(ec.x_point_angle) + "], mas ele nao tem o Part. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                            132, 0x380003));
                    }

                    if (!_session.m_pi.ei.char_info.parts_typeid.Any(c => c ==
                        ec._typeid))
                    {
                        throw new exception("[VersusBase::ActiveEarcuff][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou ativar Earcuff[TYPEID=" + Convert.ToString(ec._typeid) + ", ANGLE_SENTIDO=" + Convert.ToString((ushort)ec.angle) + ", X_ANGLE=" + Convert.ToString(ec.x_point_angle) + "], mas ele nao esta com o Part equipado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                            133, 0x380004));
                    }

                }
                else if (sIff.getInstance().getItemGroupIdentify(ec._typeid) == IFF_GROUP.MASCOT)
                { // Mascot Dragon

                    var pMi = _session.m_pi.findMascotByTypeid(ec._typeid);

                    if (pMi == null)
                    {
                        throw new exception("[VersusBase::ActiveEarcuff][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou ativar Earcuff[TYPEID=" + Convert.ToString(ec._typeid) + ", ANGLE_SENTIDO=" + Convert.ToString((ushort)ec.angle) + ", X_ANGLE=" + Convert.ToString(ec.x_point_angle) + "], mas ele nao tem esse Mascot. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                            134, 0x380005));
                    }

                    if (_session.m_pi.ei.mascot_info == null)
                    {
                        throw new exception("[VersusBase::ActiveEarcuff][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou ativar Earcuff'Mascot'[TYPEID=" + Convert.ToString(ec._typeid) + ", ANGLE_SENTIDO=" + Convert.ToString((ushort)ec.angle) + ", X_ANGLE=" + Convert.ToString(ec.x_point_angle) + "], mas ele nao esta com o Mascot equipado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                            135, 0x380006));
                    }
                }

                INIT_PLAYER_INFO("requestActiveEarcuff",
                    "tentou ativar o efeito earcuff de direcao de vento",
                    _session, out PlayerGameInfo pgi);

                // Effect
                setEffectActiveInShot(_session, enumToBitValue(AbilityEffect.EARCUFF_DIRECTION_WIND));

                // Radianos do angulo que foi trocado a direção
                pgi.earcuff_wind_angle_shot = (float)(ec.x_point_angle < 0.0f ? (2 * PI) + ec.x_point_angle : ec.x_point_angle);

                // Resposta para o Active Earcuff
                p.init_plain(0x24C);

                p.WriteUInt32(0); // OK

                p.WriteUInt32(ec._typeid);

                p.WriteUInt32(_session.m_pi.uid);

                p.WriteByte(ec.angle);

                p.WriteFloat(ec.x_point_angle);

                packet_func.game_broadcast(this,
                    p, 1);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[VersusBase::requestActiveEarcuff][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Resposta Error
                p.init_plain(0x24C);

                p.WriteUInt32((ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) == (uint)STDA_ERROR_TYPE.VERSUS_BASE) ? ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) : 0x380000);

                packet_func.session_send(p,
                    _session, 1);
            }
        }


        public override void requestMarkerOnCourse(Player _session, packet _packet)
        {
            ////REQUEST_BEGIN("MarkerOnCourse");

            var p = new PangyaBinaryWriter();

            try
            {

                var moc = new stMarkerOnCourse();
                moc.x = _packet.ReadSingle();
                moc.y = _packet.ReadSingle();
                moc.z = _packet.ReadSingle();

                // Resposta para MarkerOnCourse
                p.init_plain(0x1F8);

                p.WriteInt32(_session.m_oid);

                p.WriteFloat(moc.x);
                p.WriteFloat(moc.y);
                p.WriteFloat(moc.z);

                packet_func.game_broadcast(this,
                    p, 1);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[VersusBase::requestMarkerOnCourse][ErrorSystem] " + e.getFullMessageError(), 0));
            }
        }

        public override void requestLoadGamePercent(Player _session, packet _packet)
        {
            ////REQUEST_BEGIN("LoadGamePercent");

            var p = new PangyaBinaryWriter();

            try
            {

                byte percent = _packet.ReadByte(); 

                p.init_plain(0xA3);

                p.WriteInt32(_session.m_oid);

                p.WriteByte(percent);

                packet_func.game_broadcast(this,
                    p, 1);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[VersusBase::requestLoadGamePercent][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void requestStartTurnTime(Player _session, packet _packet)
        { 
            try
            {

                // Começa a contar o tempo do turno do player no Jogo
                startTime(_session);

                m_state_vs.setStateWithLock(STATE_VERSUS.WAIT_HIT_SHOT);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[VersusBase::requestStartTurnTime][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void requestUnOrPause(Player _session, packet _packet)
        {

            try
            {

                byte opt = _packet.ReadByte();

                if (m_timer == null)
                {
                    throw new exception("[VersusBase::requestUnOrPause][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou pausar ou despausar[OPT=" + Convert.ToString((ushort)opt) + "] um VersusBase, que nao tem timer inicializado. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                        300, 0));
                }

                if (opt == 0)
                { 
                    // Despausa 
                    if (m_timer.getState() != PangyaSyncTimer.TIMER_STATE.PAUSED)
                    {
                        throw new exception("[VersusBase::requestUnOrPause][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou pausar ou despausar[OPT=" + Convert.ToString((ushort)opt)  + ", TYPE" + m_timer.getState() + "] um VersusBase, que o timer nao esta pausado, esta em outro estado[ESTADO=" + Convert.ToString(m_timer.getState()) + "]", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                            301, 0));
                    }

                    resumeTime();

                    var p = new PangyaBinaryWriter((ushort)0x8B);

                    p.WriteInt32(_session.m_oid);

                    p.WriteByte(0);

                    packet_func.game_broadcast(this,
                        p, 1); 
                    _smp.message_pool.getInstance().push(new message("[VersusBase::requestUnOrPause][Log] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] pausou o tempo na sala[NUMERO=" + Convert.ToString(m_ri.numero) + ", TYPE" + m_timer.getState() + "] com sucesso!", type_msg.CL_FILE_LOG_AND_CONSOLE));

                }
                else if (opt == 1)
                { 
                    // Pausa 
                    if (m_timer.getState() != PangyaSyncTimer.TIMER_STATE.RUNNING)
                    {
                        throw new exception("[VersusBase::requestUnOrPause][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou pausar ou despausar[OPT=" + Convert.ToString((ushort)opt)  + ", TYPE" + m_timer.getState() + "] um VersusBase, que o timer nao esta rodando, esta em outro estado[ESTADO=" + Convert.ToString(m_timer.getState()) + "]", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                            301, 0));
                    }

                    if (m_count_pause++ >= 3)
                    {
                        throw new exception("[VersusBase::requestUnOrPause][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "], tentou pausar ou despausar[OPT=" + Convert.ToString((ushort)opt) + "] um VersusBase, mas o Versus Base ja foi pausado 3x. Hacker ou Bug");
                    }

                    pauseTime();

                    var p = new PangyaBinaryWriter((ushort)0x8B);

                    p.WriteInt32(_session.m_oid);

                    p.WriteByte(1);

                    packet_func.game_broadcast(this,
                        p, 1);

                    // Log

                    _smp.message_pool.getInstance().push(new message("[VersusBase::requestUnOrPause][Log] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] pausou o tempo na sala[NUMERO=" + Convert.ToString(m_ri.numero) + ", TYPE" + m_timer.getState() + "] com sucesso!", type_msg.CL_FILE_LOG_AND_CONSOLE));
                    // DEBUG
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[VersusBase::requestUnOrPause][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void requestReplyContinue()
        {
            try
            {

                // Troca o Turno
                changeTurn();

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[VersusBase::requestReplyContinue][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void requestExecCCGChangeWind(Player _session, packet _packet)
        {
            ////REQUEST_BEGIN("ExecCCGChangeWind");

            try
            {

                byte wind = _packet.ReadByte();
                ushort degree = _packet.ReadByte();

                CCGChangeWind(_session,
                    wind, degree);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[VersusBase::requestExecCCGChangeWind][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                throw;
            }
        }

        public override void requestExecCCGChangeWeather(Player _session, packet _packet)
        {
            ////REQUEST_BEGIN("ExecCCGChangeWeather");

            try
            {

                if (m_player_turn == null)
                {
                    throw new exception("[VersusBase::requestExecCCGChangeWeather][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou executar o comando de troca de tempo(weather) no versus na sala[NUMERO=" + Convert.ToString(m_ri.numero) + "], mas o player_turn do versus eh invalido. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                        1, 0x5700100));
                }

                var hole = m_course.findHole(m_player_turn.hole);

                if (hole == null)
                {
                    throw new exception("[VersusBase::requestExecCCGChangeWeather][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou executar o comando de troca de tempo(weather) no versus na sala[NUMERO=" + Convert.ToString(m_ri.numero) + "], mas o nao encontrou o hole[VALUE=" + Convert.ToString((short)m_player_turn.hole) + "] no course. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                        2, 0x5700100));
                }

                var weather = _packet.ReadByte();

                // Change Weather of Hole
                hole.setWeather(weather);

                // Log
                _smp.message_pool.getInstance().push(new message("[VersusBase::requestExecCCGChangeWeather][Log] [GM] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] trocou o tempo(weather) da sala[NUMERO=" + Convert.ToString(m_ri.numero) + ", WEATHER=" + Convert.ToString((ushort)weather) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                // UPDATE ON GAME
                var p = new PangyaBinaryWriter((ushort)0x9E);

                p.WriteUInt16(hole.getWeather());
                p.WriteByte(1); // Acho que seja flag, não sei, vou deixar 1 por ser o GM que mudou

                packet_func.game_broadcast(this,
                    p, 1);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[VersusBase::requestExecCCGChangeWeather][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                throw;
            }
        }
        public override bool requestFinishGame(Player _session, packet p)
        {
            ////REQUEST_BEGIN("FinishGame");

            bool ret = false;

            try
            {

                UserInfoEx ui = new UserInfoEx();
                #region Read Packet
                ui.ToRead(p);
                #endregion

                _smp.message_pool.getInstance().push(new message("[VersusBase::requestFinishGame][Log] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] UserInfo[" + ui.ToString() + "]", type_msg.CL_ONLY_CONSOLE_DEBUG));
                // DEBUG

                // aqui o cliente passa mad_conduta com o hole_in, trocados, mad_conduto <-> hole_in

                INIT_PLAYER_INFO("requestFinishGame",
                    "tentou terminar o jogo",
                    _session, out PlayerGameInfo pgi);

                pgi.ui = ui;

                // Packet06
                ret = finish_game(_session, 6);

                UpdateRoomLogSql(_session);
            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[VersusBase::requestFinishGame][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            return ret;
        }

        public virtual void startTime(object _quem)
        {
            try
            {
                // Pega info do jogador do turno
                INIT_PLAYER_INFO("startTime",
                    "tentou começar o tempo do player do turno no jogo",
                    (Player)_quem, out PlayerGameInfo pgi);

                // Incrementa número de tacadas do jogador
                pgi.data.tacada_num++;

                // Para timer antigo, se existir
                stopTime();

                // Cria novo timer baseado no tempo do modo 
                m_timer = sgs.gs.getInstance().MakeTime(m_ri.time_vs, () => end_time(this, _quem));
            }
            catch (exception e) // <- Corrigido aqui!
            {
                _smp.message_pool.getInstance().push(new message(
                    $"[VersusBase::startTime][ErrorSystem] {e.Message}",
                    type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }


        public virtual void timeIsOver(object _quem)
        {

            if (_quem == null)
            {
                _smp.message_pool.getInstance().push(new message("[VersusBase::timeIsOver][Warning] time is over executed without _quem, _quem is invalid(null). Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            m_state_vs.setStateWithLock(STATE_VERSUS.END_SHOT);

            // Time Out
            var p = new PangyaBinaryWriter();
            p.init_plain(0x5C);

            p.WriteInt32(((Player)_quem).m_oid);

            packet_func.game_broadcast(this,
                p, 1);
        }

        public override bool init_game()
        {
            // Inicializar Treasure Hunter Info do Versus Base
            init_treasure_hunter_info();

            return true;
        }

        public override void requestTranslateSyncShotData(Player _session, ShotSyncData _ssd)
        {
            //CHECK_SESSION_BEGIN("requestTranslateSyncShotData");

            try
            {

                var s = findSessionByOID(_ssd.oid);

                if (s == null)
                    throw new exception("[VersusBase::requestTranslateSyncShotData][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou sincronizar tacada do PLAYER[OID=" + Convert.ToString(_ssd.oid) + "], mas o player nao existe nessa jogo. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                           200, 0));

                // Update Sync Shot Player
                if (_session.m_pi.uid == s.m_pi.uid)
                {

                    INIT_PLAYER_INFO("requestTranslateSyncShotData",
                        "tentou sincronizar a tacada no jogo",
                        _session, out PlayerGameInfo pgi);

                    pgi.shot_sync = _ssd;

                    // Last Location Player
                    var last_location = pgi.location;

                    // Update Location Player
                    pgi.location.x = _ssd.location.x;
                    pgi.location.z = _ssd.location.z;

                    // Update Pang and Bonus Pang
                    pgi.data.pang = _ssd.pang;
                    pgi.data.bonus_pang = _ssd.bonus_pang;

                    if (_ssd.state == ShotSyncData.SHOT_STATE.OUT_OF_BOUNDS || _ssd.state == ShotSyncData.SHOT_STATE.UNPLAYABLE_AREA)
                        pgi.data.tacada_num++;

                    var hole = m_course.findHole(pgi.hole);

                    if (hole == null)
                        throw new exception("[VersusBase::requestTranslateSyncShotData][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou sincronizar tacada no hole[NUMERO=" + Convert.ToString((ushort)pgi.hole) + "], mas o numero do hole is invalid. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                                12, 0));

                    // Conta j  a pr xima tacada, no give up
                    if (!_ssd.state_shot.display.acerto_hole && hole.getPar().total_shot <= (pgi.data.tacada_num + 1))
                    {

                        // +1 que   giveup, s  add se n o passou o n mero de tacadas
                        if (pgi.data.tacada_num < hole.getPar().total_shot)
                            pgi.data.tacada_num++;

                        pgi.data.giveup = 1;

                        // Soma +1 no Bad Condute
                        pgi.data.bad_condute++;
                    }

                    // aqui os achievement de power shot int32_t putt beam impact e etc
                    update_sync_shot_achievement(_session, last_location);
                }
            }
            catch (exception e)
            {
                _smp.message_pool.getInstance().push(new message("[VersusBase::requestTranslateSyncShotData][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void requestReplySyncShotData(Player _session)
        {
            //CHECK_SESSION_BEGIN("requestReplySyncShotData");

            try
            {

                INIT_PLAYER_INFO("requestReplySyncShotData",
                    "tentou sincronizar a tacada no jogo",
                    _session, out PlayerGameInfo pgi);

                setSyncShot(pgi);
            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[VersusBase::requestReplySyncShotData][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public virtual void requestTranslateFinishHoleData(Player _session, UserInfoEx _ui)
        {
            //CHECK_SESSION_BEGIN("requestTranslateFinishHole");

            try
            {

                INIT_PLAYER_INFO("requestTranslateFinishHoleData",
                    "tentou finalizar hole dados no jogo",
                    _session, out PlayerGameInfo pgi);

                pgi.ui = _ui;

                if (!pgi.shot_sync.state_shot.display.acerto_hole)
                { // Terminou o Hole sem acerta ele, Give Up

                    var hole = m_course.findHole(pgi.hole);

                    if (hole == null)
                    {
                        throw new exception("[VersusBase::requestFinishHoleData][Error] PLAYER[UID=" + Convert.ToString(pgi.uid) + "] tentou finalizar os dados do hole no jogo, mas o hole[NUMERO=" + Convert.ToString(pgi.hole) + "] nao existe no course. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS,
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
                pgi.progress.best_chipin = _ui.best_chip_in;
                pgi.progress.best_long_puttin = _ui.best_long_putt;
                pgi.progress.best_drive = _ui.best_drive;
            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[VersusBase::requestTranslateFinishHoleData][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public virtual bool checkNextStepGame(Player _session)
        {

            var ret = false;

            try
            {

                INIT_PLAYER_INFO("checkNextStepGame",
                    "tentou verificar o proximo passo do jogo",
                    _session, out PlayerGameInfo pgi);

                var seq = m_course.findHoleSeq(pgi.hole);

                if (seq == 0 || seq == ushort.MaxValue)
                {
                    throw new exception("[VersusBase::checkNextStepGame][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou pegar sequencia do hole[NUMERO=" + Convert.ToString(pgi.hole) + ", SEQ=" + Convert.ToString(seq) + "], mas nao encontrou course. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                        500, 0));
                }

                if (m_players.Count == 2 && seq >= 4)
                {

                    if (m_ri.qntd_hole == 18)
                    { // 18 Envia a pergunta se o player quer continuar o VS Sozinho

                        if (m_player_turn == null)
                        {

                            // Player Turn ainda não foi decidido, termina o jogo
                            m_state_vs.setStateWithLock(STATE_VERSUS.WAIT_END_GAME);

                            ret = true; // Termina o Game

                        }
                        else if (m_player_turn == pgi)
                        {

                            // só tem 2 na sala, então só retorna uma session
                            var sessions = getSessions(_session);

                            var p = new PangyaBinaryWriter((ushort)0x92);

                            if (sessions.Count > 1)
                            {
                                packet_func.vector_send(p,
                                    sessions, 1);
                            }
                            else
                            {
                                packet_func.session_send(p,
                                    sessions.First(), 1);
                            }

                        }
                        else if (!checkPlayerTurnExistOnGame())
                        {

                            // Player Turn não está mais no jogo, termina o jogo
                            m_state_vs.setStateWithLock(STATE_VERSUS.WAIT_END_GAME);

                            ret = true; // Termina o Game

                        }
                        else
                        {
                            m_flag_next_step_game = 1; // Pergunta se quer continuar
                        }

                    }
                    else if (m_player_turn == null)
                    {

                        // Player Turn ainda não foi decidido, termina o jogo
                        m_state_vs.setStateWithLock(STATE_VERSUS.WAIT_END_GAME);

                        ret = true; // Termina o Game

                    }
                    else if (m_player_turn == pgi)
                    {

                        m_state_vs.setStateWithLock(STATE_VERSUS.WAIT_END_GAME);

                        ret = true; // Termina o Game

                    }
                    else if (!checkPlayerTurnExistOnGame())
                    {

                        // Player Turn não está mais no jogo, termina o jogo
                        m_state_vs.setStateWithLock(STATE_VERSUS.WAIT_END_GAME);

                        ret = true; // Termina o Game

                    }
                    else
                    {
                        m_flag_next_step_game = 2; // Termina o game
                    }

                }
                else if (m_players.Count == 2)
                {

                    if (m_player_turn == null)
                    {

                        // Player Turn ainda não foi decidido, termina o jogo
                        m_state_vs.setStateWithLock(STATE_VERSUS.WAIT_END_GAME);

                        ret = true; // Termina o Game

                    }
                    else if (m_player_turn == pgi)
                    {

                        m_state_vs.setStateWithLock(STATE_VERSUS.WAIT_END_GAME);

                        ret = true; // Termina o Game

                    }
                    else if (!checkPlayerTurnExistOnGame())
                    {

                        // Player Turn não está mais no jogo, termina o jogo
                        m_state_vs.setStateWithLock(STATE_VERSUS.WAIT_END_GAME);

                        ret = true; // Termina o Game

                    }
                    else
                    {
                        m_flag_next_step_game = 2; // Termina o game
                    }

                }
                else if (m_players.Count == 1)
                { // Player quitou mesmo sendo o ultimo no jogo

                    if (m_player_turn == null)
                    {

                        // Player Turn ainda não foi decidido, termina o jogo
                        m_state_vs.setStateWithLock(STATE_VERSUS.WAIT_END_GAME);

                        ret = true; // Termina o Game

                    }
                    else if (m_player_turn == pgi)
                    {

                        m_state_vs.setStateWithLock(STATE_VERSUS.WAIT_END_GAME);

                        ret = true; // Termina o Game

                    }
                    else if (!checkPlayerTurnExistOnGame())
                    {

                        // Player Turn não está mais no jogo, termina o jogo
                        m_state_vs.setStateWithLock(STATE_VERSUS.WAIT_END_GAME);

                        ret = true; // Termina o Game

                    }
                    else
                    {
                        m_flag_next_step_game = 2;
                    }

                }
                else if (m_player_turn == null)
                {

                    // Player Turn ainda não foi decidido, termina o jogo
                    m_state_vs.setStateWithLock(STATE_VERSUS.WAIT_END_GAME);

                    ret = true; // Termina o Game

                }
                else
                {
                    m_flag_next_step_game = 3; // Player quitou
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[VersusBase::checkNextStepGame][ErroSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            return ret;
        }

        protected override bool checkEndGame(Player _session)
        {
            INIT_PLAYER_INFO("checkEndGame",
                "tentou verificar se eh o final do jogo",
                _session, out PlayerGameInfo pgi);

            return (m_course.findHoleSeq(pgi.hole) == m_ri.qntd_hole || (m_players.Count == 1 && m_course.findHoleSeq(pgi.hole) < 4));
        }

        public virtual bool checkPlayerTurnExistOnGame()
        {

            foreach (var el in m_players)
            {

                INIT_PLAYER_INFO("checkPlayerTurnExistOnGame",
                    "verifica se o player turno existe no jogo",
                    el, out PlayerGameInfo pgi);

                // Existe
                if (m_player_turn == pgi)
                {
                    return true;
                }
            }

            // Não existe
            return false;
        }

        public virtual bool checkAllClearHole()
        {

            uint count = 0;

            // Check
            m_players.ForEach(_el =>
            {
                try
                {
                    INIT_PLAYER_INFO("checkAllClearHole",
                        "tentou verificar se todos os player terminaram o hole no jogo",
                        _el, out PlayerGameInfo pgi);
                    if (pgi.shot_sync.state_shot.display.acerto_hole || pgi.data.giveup.IsTrue())
                    {
                        count++;
                    }
                }
                catch (exception e)
                {
                    _smp.message_pool.getInstance().push(new message("[VersusBase::checkAllClearHole][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
            });




            return (count == m_players.Count);
        }

        public virtual void clearAllClearHole()
        {
            clear_all_clear_hole();
        }

        public virtual void setLoadHole(PlayerGameInfo _pgi)
        {

            if (_pgi == null)
            {

                _smp.message_pool.getInstance().push(new message("[VersusBase::setLoadHole][Error] PlayerGameInfo* _pgi is invalid(null).", type_msg.CL_FILE_LOG_AND_CONSOLE));

                return;
            }
            // Set
            _pgi.finish_load_hole = 1;

            if (m_hEvent_chk_turn_pulse != INVALID_HANDLE_VALUE)
                SetEvent(m_hEvent_chk_turn_pulse);
        }

        public virtual bool checkAllLoadHole()
        {

            uint count = 0;

            // Check
            m_players.ForEach(_el =>
            {
                try
                {
                    INIT_PLAYER_INFO("CheckAllLoadHole",
                        "tentou verificar se todos os player terminaram de carregar o hole no jogo",
                        _el, out PlayerGameInfo pgi);
                    if (pgi.finish_load_hole.IsTrue())
                    {
                        count++;
                    }
                }
                catch (exception e)
                {
                    _smp.message_pool.getInstance().push(new message("[VersusBase::CheckAllLoadHole][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
            });
            return (count == m_players.Count);
        }

        public virtual void clearLoadHole()
        {
            clear_all_load_hole();
        }

        public virtual bool setFinishCharIntroAndCheckAllFinishCharIntroAndClear(PlayerGameInfo _pgi)
        {

            if (_pgi == null)
            {

                _smp.message_pool.getInstance().push(new message("[VersusBase::setFinishCharIntroAndCheckAllFinishCharIntroAndClear][Error] PlayerGameInfo* _pgi is invalid(null).", type_msg.CL_FILE_LOG_AND_CONSOLE));

                return false;
            }

            uint count = 0;
            bool ret = false;

            // Set
            _pgi.finish_char_intro = 1;

            // Check
            m_players.ForEach(_el =>
            {
                try
                {
                    INIT_PLAYER_INFO("setFinishCharIntroAndCheckAllFinishCharIntroAndClear",
                        "tentou verificar se todos os player terminaram a Intro do Character no jogo",
                        _el, out PlayerGameInfo pgi);
                    if (pgi.finish_char_intro.IsTrue())
                    {
                        count++;
                    }
                }
                catch (exception e)
                {
                    _smp.message_pool.getInstance().push(new message("[VersusBase::setFinishCharIntroAndCheckAllFinishCharIntroAndClear][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
            });

            ret = (count == m_players.Count);

            // Clear
            if (ret)
            {
                clear_all_finish_char_intro();
            }
            return ret;
        }

        public virtual void setFinishShot(PlayerGameInfo _pgi)
        {

            if (_pgi == null)
            {

                _smp.message_pool.getInstance().push(new message("[VersusBase::setFinishShot][Error] PlayerGameInfo* _pgi is invalid(null).", type_msg.CL_FILE_LOG_AND_CONSOLE));

                return;
            }

            // Set
            _pgi.finish_shot = 1;

            if (m_hEvent_chk_turn_pulse != INVALID_HANDLE_VALUE)
                SetEvent(m_hEvent_chk_turn_pulse);

        }

        public virtual bool checkAllFinishShot()
        {

            uint count = 0;

            // Check
            m_players.ForEach(_el =>
            {
                try
                {
                    INIT_PLAYER_INFO("CheckAllFinishShot",
                        "tentou verificar se todos os player terminaram a Tacada no jogo",
                        _el, out PlayerGameInfo pgi);
                    if (pgi.finish_shot > 0)
                    {
                        count++;
                    }
                }
                catch (exception e)
                {
                    _smp.message_pool.getInstance().push(new message("[VersusBase::CheckAllFinishShot][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
            });

            return count == m_players.Count;
        }

        public virtual void clearFinishShot()
        {
            clear_all_finish_shot();
        }

        public virtual void setSyncShot(PlayerGameInfo _pgi)
        {

            if (_pgi == null)
            {

                _smp.message_pool.getInstance().push(new message("[VersusBase::setSyncShot[Error] PlayerGameInfo *_pgi is invalid(null).", type_msg.CL_FILE_LOG_AND_CONSOLE));

                return;
            }

            // Set
            _pgi.sync_shot_flag = 1;//possivelmente erro aqui

            if (m_hEvent_chk_turn_pulse != INVALID_HANDLE_VALUE)
                SetEvent(m_hEvent_chk_turn_pulse);
        }

        // 2. Verificação Robusta de Sincronismo
        public virtual bool checkAllSyncShot()
        {
            if (m_players.Count == 0) return true;

            int count = 0;
            foreach (var _el in m_players)
            {
                if (_el == null || !_el.isConnected())
                {
                    count++;
                    continue;
                }

                INIT_PLAYER_INFO("CheckAllSyncShot", "verificando sincronia", _el, out PlayerGameInfo pgi);

                // Se o player confirmou OU se ele saiu do jogo, contamos como pronto
                if (pgi.sync_shot_flag == 1 || pgi.flag == PlayerGameInfo.eFLAG_GAME.QUIT)
                    count++;
            }
            return (count >= m_players.Count);
        }


        public virtual void clearSyncShot()
        {

            clear_all_sync_shot();

        }

        public virtual void clear_all_clear_hole()
        {

            m_players.ForEach(_el =>
            {
                try
                {
                    INIT_PLAYER_INFO("clear_all_clear_hole",
                        " tentou limpar all clear hole no jogo",
                        _el, out PlayerGameInfo pgi);
                    pgi.shot_sync.state_shot.display.acerto_hole = false;
                    pgi.data.giveup = 0;
                }
                catch (exception e)
                {
                    _smp.message_pool.getInstance().push(new message("[VersusBase::clear_all_hole][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
            });
        }

        public virtual void clear_all_load_hole()
        {

            m_players.ForEach(_el =>
            {
                try
                {
                    INIT_PLAYER_INFO("clear_all_load_hole",
                        " tentou limpar all load hole no jogo",
                        _el, out PlayerGameInfo pgi);
                    pgi.finish_load_hole = 0;
                }
                catch (exception e)
                {
                    _smp.message_pool.getInstance().push(new message("[VersusBase::clear_all_load_hole][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
            });
        }

        public virtual void clear_all_finish_char_intro()
        {

            m_players.ForEach(_el =>
            {
                try
                {
                    INIT_PLAYER_INFO("clear_all_finish_char_intro",
                        " tentou limpar all finish char intro no jogo",
                        _el, out PlayerGameInfo pgi);
                    pgi.finish_char_intro = 0;
                }
                catch (exception e)
                {
                    _smp.message_pool.getInstance().push(new message("[VersusBase::clear_all_finish_char_intro][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
            });
        }

        public virtual void clear_all_finish_shot()
        {

            m_players.ForEach(_el =>
            {
                try
                {
                    INIT_PLAYER_INFO("clear_all_finish_shot",
                        " tentou limpar all finish tacada no jogo",
                        _el, out PlayerGameInfo pgi);
                    pgi.finish_shot = 0;
                    pgi.tick_sync_end_shot.clear();
                }
                catch (exception e)
                {
                    _smp.message_pool.getInstance().push(new message("[VersusBase::clear_all_finish_shot][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
            });
        }

        public virtual void clear_all_finish_shot2()
        {

            m_players.ForEach(_el =>
            {
                try
                {
                    INIT_PLAYER_INFO("clear_all_finish_shot2",
                        " tentou limpar all finish tacada no jogo",
                        _el, out PlayerGameInfo pgi);
                    pgi.finish_shot2 = 0;
                }
                catch (exception e)
                {
                    _smp.message_pool.getInstance().push(new message("[VersusBase::clear_all_finish_shot2][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
            });
        }

        // 1. Limpeza total de flags para evitar que lixo da tacada anterior atropele a nova
        public virtual void clear_all_sync_shot()
        {
            foreach (var _el in m_players)
            {
                try
                {
                    if (_el == null) continue;

                    INIT_PLAYER_INFO("clear_all_sync_shot", "limpando sync shot", _el, out PlayerGameInfo pgi);

                    pgi.sync_shot_flag = 0;
                    pgi.tick_sync_shot.clear();
                }
                catch { /* Log omitido para brevidade */ }
            }
        }

        public virtual void clear_all_sync_shot2()
        {

            m_players.ForEach(_el =>
            {
                try
                {
                    INIT_PLAYER_INFO("clear_all_sync_shot2",
                        " tentou limpar all sync shot2 do jogo",
                        _el, out PlayerGameInfo pgi);
                    pgi.sync_shot_flag2 = 0;
                }
                catch (exception e)
                {
                    _smp.message_pool.getInstance().push(new message("[VersusBase::clear_all_sync_shot2][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
            });
        }

        public virtual void clear_all_init_shot()
        {

            m_players.ForEach(_el =>
            {
                try
                {
                    INIT_PLAYER_INFO("clear_all_init_shot",
                        " tentou limpar all init shot do jogo",
                        _el, out PlayerGameInfo pgi);
                    pgi.init_shot = 0;
                }
                catch (exception e)
                {
                    _smp.message_pool.getInstance().push(new message("[VersusBase::clear_all_ini_shot][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
            });
        }

        public virtual void clear_all_flag_sync()
        {
            clear_all_load_hole();
            clear_all_finish_char_intro();
            clear_all_finish_shot();
            clear_all_finish_shot2();
            clear_all_sync_shot();
            clear_all_sync_shot2();
            clear_all_init_shot();
        }

        public virtual void init_treasure_hunter_info()
        {

            foreach (var el in m_players)
            {

                INIT_PLAYER_INFO("ini_treasure_hunter_info",
                    "tentou inicializar o treasure hunter info do versus base",
                   el, out PlayerGameInfo pgi);

                m_thi.increment(pgi.thi);//fazer operador
            }
        }

        public virtual void updateFinishHole()
        {

            var p = new PangyaBinaryWriter((ushort)0x65);

            packet_func.game_broadcast(this,
                p, 1);
        }

        public virtual void updateTreasureHunterPoint()
        {

            if (!sTreasureHunterSystem.getInstance().isLoad())
            {
                sTreasureHunterSystem.getInstance().load();
            }

            // Calcule Treasure Pontos
            foreach (var el in m_players)
            {

                INIT_PLAYER_INFO("updateTreasureHunterPoint",
                    "tentou atualizar os pontos do Treasure Hunter no jogo",
                    el, out PlayerGameInfo pgi);

                var hole = m_course.findHole(pgi.hole);

                if (hole == null)
                {
                    throw new exception("[VersusBase::updateTreasureHunterPoint][Error] PLAYER[UID=" + Convert.ToString(el.m_pi.uid) + "] tentou atualizar os pontos do Treasure Hunter no hole[NUMERO=" + Convert.ToString((ushort)pgi.hole) + "], mas o hole nao existe. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY_BASE,
                        30, 0));
                }


                m_thi.treasure_point += sTreasureHunterSystem.getInstance().calcPointNormal(pgi.data.tacada_num, hole.getPar().par) + m_thi.getPoint(pgi.data.tacada_num, (byte)hole.getPar().par);
            }

            // Mostra score board
            var p = new PangyaBinaryWriter((ushort)0x132);

            p.WriteUInt32(m_thi.treasure_point);

            // No Modo Match passa outro valor tbm

            packet_func.game_broadcast(this,
                p, 1);
        }
        public virtual void requestDrawTreasureHunterItem()
        {
            if (!sTreasureHunterSystem.getInstance().isLoad())
                sTreasureHunterSystem.getInstance().load();

            var v_item = sTreasureHunterSystem.getInstance().drawItem(m_thi.treasure_point, m_ri.getMap());

            if (!v_item.Any())
            {
                _smp.message_pool.getInstance().push(new message(
                    "[VersusBase::requestDrawTreasureHunterItem][Warning] Nenhum item sorteado pelo sistema de Treasure Hunter.",
                    type_msg.CL_FILE_LOG_AND_CONSOLE));
                return;
            }




            if (m_player_order == null || m_player_order.Count == 0)
                return;

            int idx = 0;
            foreach (var item in v_item)
            {
                var player = m_player_order[idx % m_player_order.Count];
                // Treasure Hunter Item Versus Base
                m_thi.v_item.Add(new TreasureHunterVersusInfo._stTreasureHunterItem(player.uid, item));

                // Treasure Hunter Item Player
                player.thi.v_item.Add(item);

                idx++;
            }
        }

        public virtual void sendSyncShot()
        {

            if (m_player_turn == null)
            {
                throw new exception("[VersusBase::sendSyncShot][Error] PlayerGameInfo* m_player_turn is invalid(null)", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                    1200, 0));
            }

            var p = new PangyaBinaryWriter((ushort)0x64);

            p.WriteBytes(m_player_turn.shot_sync.ToArray());

            packet_func.game_broadcast(this,
                p, 1);
        }

        public virtual void sendEndShot(Player _session, DropItemRet _cube)
        {

            var p = new PangyaBinaryWriter((ushort)0xCC);

            p.WriteInt32(_session.m_oid);

            // Count, Coin/Cube "Drop"
            p.WriteByte((byte)_cube.v_drop.Count);

            if (!_cube.v_drop.empty())
            {

                foreach (var el in _cube.v_drop)
                {
                    p.WriteBytes(el.ToArray());
                }

                // Aqui o server passa 128 itens de drop, os que dropou e o resto vazio
                if (_cube.v_drop.Count < 128)
                {
                    p.WriteZeroByte((128 - _cube.v_drop.Count) * 16);
                }
            }

            packet_func.game_broadcast(this,
                p, 1);
        }

        public virtual void sendDropItem(Player _session)
        {

            var p = new PangyaBinaryWriter((ushort)0xFA);

            p.WriteUInt16((ushort)m_players.Count);

            foreach (var el in m_players)
            {

                INIT_PLAYER_INFO("sendDropItem",
                    "tentou enviar os itens dropado do player no jogo",
                    el, out PlayerGameInfo pgi);

                p.WriteInt32(el.m_oid);

                p.WriteByte(0); // OK

                p.WriteUInt16((ushort)pgi.drop_list.v_drop.Count);

                foreach (var els in pgi.drop_list.v_drop)
                {
                    p.WriteUInt32(els._typeid);
                }
            }

            packet_func.session_send(p,
                _session, 1);
        }

        public virtual void sendPlacar(Player _session)
        {

            var p = new PangyaBinaryWriter((ushort)0x66);

            p.WriteByte((byte)m_players.Count);

            foreach (var el in m_players)
            {

                INIT_PLAYER_INFO("sendPlacar",
                    "tentou enviar o placar do jogo",
                    el, out PlayerGameInfo pgi);

                p.WriteInt32(el.m_oid);
                p.WriteSByte(Convert.ToSByte(getRankPlace(el)));
                p.WriteSByte(Convert.ToSByte(pgi.data.score));//tava char antes
                p.WriteSByte(Convert.ToSByte(pgi.data.total_tacada_num));

                p.WriteUInt16((ushort)pgi.data.exp);
                p.WriteUInt64(pgi.data.pang);
                p.WriteUInt64(pgi.data.bonus_pang);

                // Valor que usa no Pang Battle, valor de pang que ganhou ou perdeu
                // Como aqui é vs Base deixa o valor 0
                p.WriteUInt64(0Ul);
            }

            packet_func.session_send(p,
                _session, 1);
        }

        public virtual void sendTreasureHunterItemDrawGUI(Player _session)
        {

            INIT_PLAYER_INFO("sendTreasureHunterItemDrawGUI",
                "tentou enviar os itens ganho no Treasure Hunter(so o Visual) do jogo",
                _session, out PlayerGameInfo pgi);

            var p = new PangyaBinaryWriter((ushort)0x133);

            p.WriteByte((byte)m_thi.v_item.Count);

            // No VS aqui os itens são dividido entres os players do versus
            foreach (var _el in m_thi.v_item)
            {
                p.WriteUInt32(_el.uid); // UID do player que ganhou o item
                p.WriteUInt32(_el.thi._typeid);
                p.WriteUInt16((ushort)_el.thi.qntd);
                p.WriteByte(0); // Acho que sejá opção ou dizendo que acabou o struct de Treasure Hunter Item Draw}
            }
            packet_func.session_send(p,
                _session, 1);
        }

        public virtual void sendReplyFinishLoadHole()
        {

            try
            {

                init_turn_hole_start();

                PlayerGameInfo pgi = requestCalculePlayerTurn();

                var hole = m_course.findHole(pgi.hole);

                if (hole == null)
                {
                    throw new exception("[VersusBase::requestFinishLoadHole][Error] PLAYER[UID=" + Convert.ToString(pgi.uid) + "] tentou finalizar carregamento do hole[NUMERO=" + Convert.ToString(pgi.hole) + "], mas nao conseguiu encontrar o hole no course. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.VERSUS_BASE,
                        201, 0));
                }

                // Resposta de tempo do hole
                var p = new PangyaBinaryWriter((ushort)0x9E);

                p.WriteUInt16(hole.getWeather());
                p.WriteByte(0); // Option do tempo, sempre peguei zero aqui dos pacotes que vi

                packet_func.game_broadcast(this,
                    p, 1);

                var wind_flag = initCardWindPlayer(m_player_turn, hole.getWind().wind);

                // Resposta do vento do hole
                p.init_plain(0x5B);

                p.WriteByte(hole.getWind().wind + wind_flag);
                p.WriteByte((wind_flag < 0) ? 1 : 0); // Flag de card de vento, aqui é a qnd diminui o vento, 1 Vento azul
                p.WriteUInt16(m_player_turn.degree);
                p.WriteByte(1); // Flag do vento, 1 Reseta o Vento, 0 soma o vento que nem o comando gm \wind do pangya original, Também é flag para trocar o vento no Pang Battle se mandar o valor 0

                packet_func.game_broadcast(this,
                    p, 1);

                // Resposta passa o oid do player que vai começa o Hole
                p.init_plain(0x53);

                if (m_player_turn == null)
                {
                    _smp.message_pool.getInstance().push(new message("[VersusBase::requestFinishLoadHole][Error] player_turn is invalid(null)", type_msg.CL_FILE_LOG_AND_CONSOLE));

                    p.WriteUInt32(0);
                }
                else
                {
                    p.WriteInt32(m_player_turn.oid);
                }

                packet_func.game_broadcast(this,
                    p, 1);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[VersusBase::sendReplyFinishLoadHole][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public virtual bool CheckLimitPlayers()
        {
            return m_players.Count > m_max_player;
        }
    }
}