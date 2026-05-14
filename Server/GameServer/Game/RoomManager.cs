using System;
using System.Collections.Generic;
using System.Linq;
using Pangya_GameServer.Models;
using PangyaAPI.IFF.JP.Models.Data;
using PangyaAPI.Network.PangyaSession;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using static System.Collections.Specialized.BitVector32;
using static Pangya_GameServer.Models.DefineConstants;

namespace Pangya_GameServer.Game.Manager
{
    public class RoomManager
    {
        // Member 
        private Dictionary<ushort, bool> m_map_index = new Dictionary<ushort, bool>(ushort.MaxValue);
        private byte m_channel_id;
        private readonly object _lock = new object(); // se ainda não tiver 
        ushort m_next_index;
        protected List<Room> v_rooms = new List<Room>();
        public RoomManager(byte _channel_id)
        {
            this.m_channel_id = _channel_id;
            if (m_map_index.Count == 0)
            {
                for (ushort i = 0; i < ushort.MaxValue; i++)
                {
                    m_map_index.Add(i, false);
                }
            }
        }

        public void destroy()
        {
            foreach (var el in v_rooms)
            {

                if (el != null)
                {

                    // Sala está destruindo
                    el.setDestroying();

                    // Libera a sala se ela estiver bloqueada
                    el.unlock();
                }
            }

            v_rooms.Clear();
            m_channel_id = 255;
        }

        public Room makeRoom(byte _channel_owner,
            RoomInfoEx _ri,
            Player _session,
            int _option = 0)
        {
            Room r = null;

            try
            {

                if (_session != null && _session.m_pi.mi.sala_numero != ushort.MaxValue)
                {
                    throw new exception("[RoomManager::makeRoom][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] sala[NUMERO=" + Convert.ToString(_session.m_pi.mi.sala_numero) + "], ja esta em outra sala, nao pode criar outra. Hacker ou Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM_MANAGER,
                        120, 0));
                }

                _ri.numero = getNewIndex();
                _ri.roomId = Guid.NewGuid();
                if (_option == 0 && _session != null)
                {
                    _ri.master = (int)_session.m_pi.uid;
                }
                else if (_option == 1) // Room Sem Master Grand Prix ou Grand Zodiac Event Time
                {
                    _ri.master = -2;
                }
                else // Room sem master
                {
                    _ri.master = -1;
                }

                r = new Room(_channel_owner, _ri);

                if (r == null)
                {
                    throw new exception("[RoomManager::makeRoom][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou criar a sala[TIPO=" + Convert.ToString((ushort)_ri.tipo) + "], mas nao conseguiu criar o objeto da classe room. Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM_MANAGER,
                        130, 0));
                }

                // Verifica se é um room válida e bloquea ela 
                r.trylock();

                if (_session != null)
                    r.enter(_session);

                r.unlock();
            }
            catch (exception e)
            {
                if (r != null)
                {

                    // Destruindo a sala, não conseguiu
                    r.setDestroying();

                    // Desbloqueia para
                    if (!ExceptionError.STDA_ERROR_CHECK_SOURCE_AND_ERROR_TYPE(e.getCodeError(),
                        STDA_ERROR_TYPE.ROOM, 150))
                    {
                        r.unlock();
                    }

                    // Deletando o Objeto
                    r = null;

                    // Limpa o ponteiro
                    r = null;
                }

                _smp.message_pool.getInstance().push(new message("[RoomManager::makeRoom][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            return r;
        }


        public void addRoom(Room r)
        {
            // Adiciona a sala no Vector
            v_rooms.Add(r);

            // Log
            _smp.message_pool.getInstance().push(new message("[RoomManager::addRoom][Sucess] Channel[ID=" + Convert.ToString((ushort)m_channel_id) + "] Maked Room[TIPO=" + Convert.ToString((ushort)r.getInfo().tipo) + ", NUMERO=" + Convert.ToString(r.getNumero()) + ", MASTER=" + Convert.ToString((int)r.getMaster()) + ", NOME=" + r.getInfo().nome + ", SENHA=" + r.getInfo().senha + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));
        }

        public void destroyRoom(Room _room)
        {

            if (_room == null)
            {
                throw new exception("[RoomManager::destroyRoom][Error] _room is nullptr.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM_MANAGER,
                    4, 0));
            }

            int index = findIndexRoom(_room);

            if (index == -1)
            {
                throw new exception("[RoomManager::destroyRoom][Error] room nao existe no vector de salas.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM_MANAGER,
                    5, 0));
            }

            try
            {
                // Sala vai ser deletada
                _room.setDestroying();
                // Vai destruir(excluir) a sala, libera a sala
                _room.unlock();
                //limpa tudo
                _room.Dispose();

                _smp.message_pool.getInstance().push(new message($"[DEBUG][RoomManager] destroyRoom chamado para sala {(_room?.getNumero())}.", type_msg.CL_ONLY_CONSOLE_DEBUG));

                clearIndex((ushort)index);

                v_rooms.RemoveAt(index);
            }
            catch (exception e)
            {
                if (_room != null)
                {

                    _room.setDestroying();

                    _room.unlock();

                    _room = null;

                    _room = null;
                }

                _smp.message_pool.getInstance().push(new message("[RoomManager::destroy][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        // Make room Grand Zodiac Event
        // Make room Grand Prix
        public RoomGrandPrix makeRoomGrandPrix(byte _channel_owner,
            RoomInfoEx _ri,
        Player _session,
            GrandPrixData _gp,
            int _option = 0)
        {
            RoomGrandPrix r = null;

            try
            {

                if (_session != null && _session.m_pi.mi.sala_numero != ushort.MaxValue)
                {
                    throw new exception("[RoomManager::makeRoomGrandPrix][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] sala[NUMERO=" + Convert.ToString(_session.m_pi.mi.sala_numero) + "], ja esta em outra sala, nao pode criar outra. Hacker ou Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM_MANAGER,
                        120, 0));
                }

                _ri.numero = getNewIndex();
                _ri.roomId = Guid.NewGuid();
                if (_option == 0 && _session != null)
                {
                    _ri.master = (int)_session.m_pi.uid;
                }
                else if (_option == 1) // Room Sem Master Grand Prix ou Grand Zodiac Event Time
                {
                    _ri.master = -2;
                }
                else // Room sem master
                {
                    _ri.master = -1;
                }

                r = new RoomGrandPrix(_channel_owner,
                    _ri, _gp);

                if (r == null)
                {
                    throw new exception("[RoomManager::makeRoomGrandPrix][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou criar a sala[TIPO=" + Convert.ToString((ushort)_ri.tipo) + "], mas nao conseguiu criar o objeto da classe RoomGrandPrix. Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM_MANAGER,
                        130, 0));
                }

                if (_session != null)
                {
                    r.enter(_session);
                }

                // Log
                _smp.message_pool.getInstance().push(new message("[RoomManager::makeRoomGrandPrix][Info] Channel[ID=" + Convert.ToString((ushort)m_channel_id) + "] Maked Room[TIPO=" + Convert.ToString((ushort)r.getInfo().tipo) + ", NUMERO=" + Convert.ToString(r.getNumero()) + ", MASTER=" + Convert.ToString((int)r.getMaster()) + ", PLAYER_REQUEST_CREATE=" + (_session != null ? Convert.ToString(_session.m_pi.uid) : "NENHUMA-SYSTEM") + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

            }
            catch (exception e)
            {

                // Libera Crictical Session do Room Manager


                if (r != null)
                {

                    // Destruindo a sala, não conseguiu
                    r.setDestroying();

                    // Desbloqueia para
                    if (!ExceptionError.STDA_ERROR_CHECK_SOURCE_AND_ERROR_TYPE(e.getCodeError(),
                        STDA_ERROR_TYPE.ROOM, 150))
                    {
                        r.unlock();
                    }

                    // Deletando o Objeto
                    r = null;

                    // Limpa o ponteiro
                    r = null;
                }

                _smp.message_pool.getInstance().push(new message("[RoomManager::makeRoomGrandZodiacEvent][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            return r;
        }


        // Make room Grand Zodiac Event
        public RoomGrandZodiacEvent makeRoomGrandZodiacEvent(byte _channel_owner, RoomInfoEx _ri)
        {

            RoomGrandZodiacEvent r = null;

            try
            {



                _ri.numero = getNewIndex();
                _ri.roomId = Guid.NewGuid();
                // Room Sem Master Grand Prix ou Grand Zodiac Event Time
                _ri.master = -2;

                r = new RoomGrandZodiacEvent(_channel_owner, _ri);

                if (r == null)
                {
                    throw new exception("[RoomManager::makeRoomGrandZodiacEvent][Error] tentou criar a sala[TIPO=" + Convert.ToString((ushort)_ri.tipo) + "] Grand Zodiac Event, mas nao conseguiu criar o objeto da classe room. Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM_MANAGER,
                        130, 0));
                }
                // Log
                _smp.message_pool.getInstance().push(new message("[RoomManager::makeRoomGrandZodiacEvent][Info] Channel[ID=" + Convert.ToString((ushort)m_channel_id) + "] Maked Room[TIPO=" + Convert.ToString((ushort)r.getInfo().tipo) + ", NUMERO=" + Convert.ToString(r.getNumero()) + ", MASTER=" + Convert.ToString((int)r.getMaster()) + "] Grand Zodiac Event.", type_msg.CL_FILE_LOG_AND_CONSOLE));

            }
            catch (exception e)
            {

                // Libera Crictical Session do Room Manager


                if (r != null)
                {

                    // Destruindo a sala, não conseguiu
                    r.setDestroying();

                    // Desbloqueia para
                    if (!ExceptionError.STDA_ERROR_CHECK_SOURCE_AND_ERROR_TYPE(e.getCodeError(),
                        STDA_ERROR_TYPE.ROOM, 150))
                    {
                        r.unlock();
                    }

                    // Deletando o Objeto
                    r = null;

                    // Limpa o ponteiro
                    r = null;
                }

                _smp.message_pool.getInstance().push(new message("[RoomManager::makeRoomGrandZodiacEvent][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            return r;
        }

        //// Make room Bot GM Event
        public RoomBotGMEvent makeRoomBotGMEvent(byte _channel_owner,
            RoomInfoEx _ri,
            List<stReward> _rewards)
        {

            RoomBotGMEvent r = null;

            try
            {



                _ri.numero = getNewIndex();
                _ri.roomId = Guid.NewGuid();
                // Room Sem Master Grand Prix ou Grand Zodiac Event Time ou Bot GM Event
                _ri.master = -2;

                r = new RoomBotGMEvent(_channel_owner,
                    _ri, _rewards);

                if (r == null)
                {
                    throw new exception("[RoomManager::makeRoomBotGMEvent][Error] tentou criar a sala[TIPO=" + Convert.ToString((ushort)_ri.tipo) + "] Bot GM Event, mas nao conseguiu criar o objeto da classe room. Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM_MANAGER,
                        130, 0));
                }
                // Log
                _smp.message_pool.getInstance().push(new message("[RoomManager::makeRoomBotGMEvent][Info] Channel[ID=" + Convert.ToString((ushort)m_channel_id) + "] Maked Room[TIPO=" + Convert.ToString((ushort)r.getInfo().tipo) + ", NUMERO=" + Convert.ToString(r.getNumero()) + ", MASTER=" + Convert.ToString((int)r.getMaster()) + "] Bot GM Event.", type_msg.CL_FILE_LOG_AND_CONSOLE));

            }
            catch (exception e)
            {

                // Libera Crictical Session do Room Manager                

                if (r != null)
                {

                    // Destruindo a sala, não conseguiu
                    r.setDestroying();

                    // Desbloqueia para
                    if (!ExceptionError.STDA_ERROR_CHECK_SOURCE_AND_ERROR_TYPE(e.getCodeError(),
                        STDA_ERROR_TYPE.ROOM, 150))
                    {
                        r.unlock();
                    }

                    // Deletando o Objeto
                    r = null;

                    // Limpa o ponteiro
                    r = null;
                }

                _smp.message_pool.getInstance().push(new message("[RoomManager::makeRoomBotGMEvent][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            return r;
        }

        public int getCount()
        {

            try
            {
                return v_rooms.Count;
            }
            catch (exception e)
            {
                _smp.message_pool.getInstance().push(new message("[RoomManager::findRoom][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_ONLY_CONSOLE));
            }
            return 0;
        }

        public Room findRoom(short _numero)
        {

            if (_numero == -1)
            {
                return null;
            }

            Room r = null;

            try
            {

                for (var i = 0; i < v_rooms.Count; ++i)
                {
                    if (v_rooms[i].getNumero() == _numero)
                    {
                        r = v_rooms[i];
                        break;
                    }
                }
                WAIT_ROOM_UNLOCK(r);
            }
            catch (exception e)
            {

                // Libera Crictical Session do Room Manager


                if (r != null)
                {

                    if (!ExceptionError.STDA_ERROR_CHECK_SOURCE_AND_ERROR_TYPE(e.getCodeError(),
                        STDA_ERROR_TYPE.ROOM, 150))
                    {
                        r.unlock();
                    }

                    r = null;
                }
                _smp.message_pool.getInstance().push(new message("[RoomManager::findRoom][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_ONLY_CONSOLE));

            }

            return r;
        }

       
        public void WAIT_ROOM_UNLOCK(Room _r)
        {

            if (_r == null)
            {
                return;
            }
            try
            { 
                _r.trylock();
            }
            catch (exception e)
            {
            }
        }


        public RoomGrandPrix findRoomGrandPrix(uint _typeid)
        {

            if (_typeid == 0u)
            {
                return null;
            }

            RoomGrandPrix r = null;

            try
            {
                foreach (var el in v_rooms)
                {

                    if (el.getInfo().grand_prix.active > 0
                        && el.getInfo().grand_prix.dados_typeid != 0U
                        && el.getInfo().grand_prix.dados_typeid == _typeid)
                    {

                        r = (RoomGrandPrix)el;

                        break;
                    }
                }
            }
            catch (exception e)
            {
                if (r != null)
                {

                    if (!ExceptionError.STDA_ERROR_CHECK_SOURCE_AND_ERROR_TYPE(e.getCodeError(),
                         STDA_ERROR_TYPE.ROOM, 150))
                    {
                        r.unlock();
                    }

                    r = null;
                }

                _smp.message_pool.getInstance().push(new message("[RoomManager::findRoomGrandPrix][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            return r;
        }

        // Opt sem sala practice, se não todas as salas
        public List<RoomInfoEx> getRoomsInfo(bool _without_practice_room = true)
        {

            List<RoomInfoEx> v_ri = new List<RoomInfoEx>();

            for (var i = 0; i < v_rooms.Count; ++i)
            {
                if (v_rooms[i] != null && (!_without_practice_room || (v_rooms[i].getInfo().getTipo() != RoomInfo.TIPO.PRACTICE && v_rooms[i].getInfo().getTipo() != RoomInfo.TIPO.GRAND_ZODIAC_PRACTICE)))
                {
                    v_ri.Add((RoomInfoEx)v_rooms[i].getInfo());
                }
            }
            return v_ri;
        }

        public List<RoomGrandZodiacEvent> getAllRoomsGrandZodiacEvent()
        {

            List<RoomGrandZodiacEvent> v_r = new List<RoomGrandZodiacEvent>();

            foreach (var el in v_rooms)
            {
                if (el != null
                    && (int)el.getMaster() == -2
                    && (el.getInfo().getTipo() == RoomInfo.TIPO.GRAND_ZODIAC_ADV || el.getInfo().getTipo() == RoomInfo.TIPO.GRAND_ZODIAC_INT))
                {
                    v_r.Add((RoomGrandZodiacEvent)(el));
                }
            }
            return v_r;
        }

        public List<RoomBotGMEvent> getAllRoomsBotGMEvent()
        {

            List<RoomBotGMEvent> v_r = new List<RoomBotGMEvent>();

            foreach (var el in v_rooms)
            {
                if (el != null
                    && (int)el.getMaster() == -2
                    && (RoomInfo.TIPO)el.getInfo().tipo == RoomInfo.TIPO.TOURNEY
                    && el.getInfo().flag_gm == 1
                    && el.getInfo().trofel == TROFEL_GM_EVENT_TYPEID)
                {
                    v_r.Add((RoomBotGMEvent)(el));
                }
            }
            return v_r;
        }

        // Unlock Room
        public void unlockRoom(Room _r)
        {
            // _r is invalid
            if (_r == null)
                return;

            try
            {
                foreach (var el in v_rooms)
                {

                    if (el != null && el == _r)
                    {

                        // Libera a sala
                        el.unlock();

                        // Acorda as outras threads que estão esperando                 
                        break;
                    }
                }
            }
            catch (exception e)
            {
                _smp.message_pool.getInstance().push(new message("[RoomManager::unlockRoom][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        protected int findIndexRoom(Room _room)
        {

            if (_room == null)
            {
                throw new exception("[RoomManager::findIndexRoom][Error] _room is nullptr.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM_MANAGER,
                    4, 0));
            }

            int index = ~0;

            for (var i = 0; i < v_rooms.Count; ++i)
            {
                if (v_rooms[i] == _room)
                {
                    index = i;
                    break;
                }
            }
            return index;
        }

        private ushort getNewIndex()
        {
            ushort index = 0;

            lock (_lock) // substitui o CriticalSection
            {
                for (ushort i = 0; i < ushort.MaxValue; ++i)
                {
                    ushort candidate_index = (ushort)((m_next_index + i) % ushort.MaxValue);

                    if (!m_map_index[candidate_index])
                    {
                        index = candidate_index;
                        m_map_index[index] = true; // marca como ocupado
                        m_next_index = (ushort)((index + 1) % ushort.MaxValue);
                        break;
                    }
                }

                if (m_next_index >= ushort.MaxValue)
                    m_next_index = 0;
            }

            return index;
        }

        private void clearIndex(ushort _index)
        {

            if (_index >= short.MaxValue)
            {
                throw new exception("[RoomManager::clearIndex][Error] _index maior que o limite do mapa de indexes.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM_MANAGER,
                    3, 0));
            }

            m_map_index[_index] = false; // Livre 
        }

        public void FilterHackRoom(Player session, RoomInfoEx ri, ChannelInfo m_ci)
        {
            if (session == null || !session.m_connected)
                ThrowHackException(session, ri, m_ci, "Sessão inexistente ou desconectada");

            if (session.m_pi.m_cap.game_master)
                return;

            ValidateRoomName(session, ri, m_ci);
            ValidateRoomPass(session, ri, m_ci);
            ValidateRoomCreate(session, ri, m_ci);
            ValidateMaxPlayers(session, ri, m_ci);
            ValidateRoomTime(session, ri, m_ci);
            ValidateHoleCount(session, ri, m_ci);
            ValidateForbiddenModes(session, ri, m_ci);

            switch (ri.getTipo())
            {
                case RoomInfo.TIPO.STROKE:
                    ValidateStrokeSpecific(session, ri, m_ci);
                    break;
                case RoomInfo.TIPO.PANG_BATTLE:
                    ValidatePangBattleSpecific(session, ri, m_ci);
                    break;
                case RoomInfo.TIPO.MATCH: // se MATCH corresponde ao VS/Approach no seu enum
                case RoomInfo.TIPO.APPROCH:
                    ValidateVsApproach(session, ri, m_ci);
                    break;
                case RoomInfo.TIPO.SPECIAL_SHUFFLE_COURSE:
                    ValidateShuffleSpecific(session, ri, m_ci);
                    break;
                default:
                    break;
            }
        }

        // --------- ValidateRoomTime (ajustado para também checar shotTimeLimits do C++) ----------
        private void ValidateRoomTime(Player session, RoomInfoEx ri, ChannelInfo m_ci)
        {
            switch (ri.getTipo())
            {
                case RoomInfo.TIPO.STROKE:
                    if (ri.qntd_hole == 3 || ri.qntd_hole == 6 || ri.qntd_hole == 9 || ri.qntd_hole == 18)
                    {
                        ValidateTimeVs(session, ri, m_ci, 40, 60, 120, 300);
                    }
                    else
                        ThrowHackException(session, ri, m_ci, $"time_vs inválido: {ri.time_vs}");
                    break;

                case RoomInfo.TIPO.MATCH:
                case RoomInfo.TIPO.PANG_BATTLE:
                    if (ri.qntd_hole == 6 || ri.qntd_hole == 9 || ri.qntd_hole == 18)
                    {
                        ValidateTimeVs(session, ri, m_ci, 40, 60, 120, 300);
                    }
                    else
                        ThrowHackException(session, ri, m_ci, $"time_vs inválido: {ri.time_vs}");
                    break;

                case RoomInfo.TIPO.TOURNEY:
                    // Tournament: pode ter short_game / natural branches; time_30s used (ms)
                    if (ri.natural != null && (ri.natural.short_game == 1 || ri.natural.natural == 1))
                    {
                        if (ri.qntd_hole == 9)
                            ValidateTime30s(session, ri, m_ci, 15, 20, 25, 30);
                        else if (ri.qntd_hole == 18)
                            ValidateTime30s(session, ri, m_ci, 30, 35, 40, 45, 50);
                        else
                            ThrowHackException(session, ri, m_ci, $"time_30s inválido: {ri.time_30s / 60000}");
                    }
                    else
                    {
                        if (ri.qntd_hole == 9)
                            ValidateTime30s(session, ri, m_ci, 15, 20, 25, 30);
                        else if (ri.qntd_hole == 18)
                            ValidateTime30s(session, ri, m_ci, 30, 35, 40, 45, 50);
                        else
                            ThrowHackException(session, ri, m_ci, $"time_30s inválido: {ri.time_30s / 60000}");
                    }
                    break;

                case RoomInfo.TIPO.GUILD_BATTLE:
                    if (ri.qntd_hole == 9)
                        ValidateTime30s(session, ri, m_ci, 15, 20, 25, 30);
                    else if (ri.qntd_hole == 18)
                        ValidateTime30s(session, ri, m_ci, 35, 40, 45, 50, 55);
                    else
                        ThrowHackException(session, ri, m_ci, $"time_30s inválido: {ri.time_30s / 60000}");
                    break;

                case RoomInfo.TIPO.PRACTICE:
                    // practice pode ter regras próprias - ignorado aqui (seguindo seu código)
                    break;

                case RoomInfo.TIPO.APPROCH:
                    if (ri.qntd_hole == 3 || ri.qntd_hole == 6 || ri.qntd_hole == 9)
                        ValidateTime30s(session, ri, m_ci, 40);
                    else
                        ThrowHackException(session, ri, m_ci, $"time_30s inválido: {ri.time_30s / 1000}");
                    break;

                case RoomInfo.TIPO.SPECIAL_SHUFFLE_COURSE:
                    if (ri.qntd_hole == 18)
                        ValidateTime30s(session, ri, m_ci, 40);
                    else
                        ThrowHackException(session, ri, m_ci, $"time_30s inválido: {ri.time_30s / 60000}");
                    break;

                default:
                    break;
            }
        }

        // --------- Stroke specific checks (shotTime, Modo for 18H, holes set) ----------
        private void ValidateStrokeSpecific(Player session, RoomInfoEx ri, ChannelInfo m_ci)
        {
            // hole count already validado em ValidateHoleCount; validar shotTime (time_vs) em segundos permitidos
            uint[] allowedShotSeconds = { 40, 60, 120, 300 };
            if (!allowedShotSeconds.Contains(ri.time_vs / 1000))
                ThrowHackException(session, ri, m_ci, "ShotTime inválido (Stroke)");

            // Se 18 holes então Modo deve ser 0 ou 3
            if (ri.qntd_hole == 18)
            {
                if (ri.modo != 0 && ri.modo != 3)
                    ThrowHackException(session, ri, m_ci, "Modo inválido no Stroke 18H");
            }
        }

        // --------- Pang Battle specific ----------
        private void ValidatePangBattleSpecific(Player session, RoomInfoEx ri, ChannelInfo m_ci)
        {
            // Modo valid
            if (ri.modo != 0 && ri.modo != 3)
                ThrowHackException(session, ri, m_ci, "Modo inválido no Pang Battle");

            // shotTime valid (seconds)
            uint[] allowedShotSeconds = { 40, 60, 120, 300 };
            if (!allowedShotSeconds.Contains(ri.time_vs / 1000))
                ThrowHackException(session, ri, m_ci, "ShotTime inválido no Pang Battle");
        }

        // --------- VS / APPROACH (game type 4 / 5) ----------
        private void ValidateVsApproach(Player session, RoomInfoEx ri, ChannelInfo m_ci)
        {
            if(ri.getTipo() == RoomInfo.TIPO.MATCH)
			{
				 // gameTimeLimit checks (valores em milissegundos como no C++)
            if (ri.qntd_hole == 9)
            {
                uint[] allowed = { 900000, 1200000, 1500000, 1800000 };
                if (!allowed.Contains(ri.time_vs))
                    ThrowHackException(session, ri, m_ci, "gameTimeLimit inválido para 9H");
            }
            else if (ri.qntd_hole == 18)
            {
                uint[] allowed = { 1800000, 2100000, 2400000, 2700000, 3000000 };
                if (!allowed.Contains(ri.time_vs))
                    ThrowHackException(session, ri, m_ci, "gameTimeLimit inválido para 18H");

                if (ri.modo != 0 && ri.modo != 3)//nao tenho ideia do que seja 'Modo', deve ser o 'mode/modo'
                    ThrowHackException(session, ri, m_ci, "Modo inválido em 18H");
            }
            else
                ThrowHackException(session, ri, m_ci, "HoleNum inválido para Match");

            // UserLimit válido? (4,10,20,30) — GMs podem usar 100 ou 200
            int[] allowedPlayers = { 4, 10, 20, 30 };
            if (!allowedPlayers.Contains(ri.max_player) && !session.m_pi.m_cap.game_master)
                ThrowHackException(session, ri, m_ci, "UserLimit inválido no Match");
			}
			else
			{ 
			 // gameTimeLimit checks (valores em milissegundos como no C++)
            if (ri.qntd_hole == 3 || ri.qntd_hole == 6 || ri.qntd_hole == 9)
            {
                uint[] allowed = { 40000 };
                if (!allowed.Contains(ri.time_30s))
                    ThrowHackException(session, ri, m_ci, "gameTimeLimit inválido para 9H");
            } 
            else
                ThrowHackException(session, ri, m_ci, "HoleNum inválido para Approach");

            // UserLimit válido? (4,20,30) — GMs podem usar 100 ou 200
            int[] allowedPlayers = { 6, 20, 30 };
            if (!allowedPlayers.Contains(ri.max_player) && !session.m_pi.m_cap.game_master)
                ThrowHackException(session, ri, m_ci, "UserLimit inválido Approach");
			}

            // Canal normal não permite Modo aleatório (random) — apenas Modo == 3 é permitido para "random"
            if (m_ci != null && m_ci.type.all && ri.modo != 3)
                ThrowHackException(session, ri, m_ci, "Random Modo proibido no canal normal");
        }

        // --------- Shuffle (tipo 6) ----------
        private void ValidateShuffleSpecific(Player session, RoomInfoEx ri, ChannelInfo m_ci)
        {
            int[] allowedHoles = { 18 };
            if (!allowedHoles.Contains(ri.qntd_hole))
                ThrowHackException(session, ri, m_ci, "HoleNum inválido no Shuffle");

            int[] allowedPlayers = { 30 };
            if (!allowedPlayers.Contains(ri.max_player))
                ThrowHackException(session, ri, m_ci, "UserLimit inválido no Shuffle");

            if (ri.modo != 0 && ri.modo != 3)
                ThrowHackException(session, ri, m_ci, "Modo inválido no Shuffle");
        }

        // --------- ValidateRoomName ----------
        private void ValidateRoomName(Player session, RoomInfoEx ri, ChannelInfo m_ci)
        {
            bool _check = true;
            switch (ri.getTipo())
            {
                case RoomInfo.TIPO.STROKE:
                case RoomInfo.TIPO.MATCH:
                case RoomInfo.TIPO.TOURNEY:
                case RoomInfo.TIPO.TOURNEY_TEAM:
                case RoomInfo.TIPO.GUILD_BATTLE:
                case RoomInfo.TIPO.APPROCH:
                case RoomInfo.TIPO.PANG_BATTLE:
                case RoomInfo.TIPO.GRAND_ZODIAC_PRACTICE:
                case RoomInfo.TIPO.SPECIAL_SHUFFLE_COURSE:
                case RoomInfo.TIPO.LOUNGE:
                    if (string.IsNullOrEmpty(ri.nome))
                        _check = false;
                    break;
                case RoomInfo.TIPO.PRACTICE:
                    if (!string.IsNullOrEmpty(ri.nome) && ri.nome.CompareTo("Single Player Practice Mode") != 0)
                        _check = false;
                    break;
                default:
                    _check = false;
                    break;
            }

            if (!_check)
                ThrowHackException(session, ri, m_ci, "Nome da sala inválido: " + ri.getTipo());
        }

        // --------- ValidateRoomPass ----------
        private void ValidateRoomPass(Player session, RoomInfoEx ri, ChannelInfo m_ci)
        {
            if (ri.getTipo() == RoomInfo.TIPO.PRACTICE || ri.getTipo() == RoomInfo.TIPO.GRAND_ZODIAC_PRACTICE)
            {
                if (!string.IsNullOrEmpty(ri.senha) && ri.senha.Length < 8 && !ri.senha.Contains("MDA"))
                    ThrowHackException(session, ri, m_ci, "tamanho da str da senha na sala inválida: " + ri.getTipo());
            }
            else
            {
                if (!string.IsNullOrEmpty(ri.senha) && ri.senha.Length > 14)
                    ThrowHackException(session, ri, m_ci, "tamanho da str da senha na sala inválida: " + ri.getTipo());
            }
        }

        // --------- ValidateRoomCreate ----------
        private void ValidateRoomCreate(Player session, RoomInfoEx ri, ChannelInfo m_ci)
        {
            bool _check;
            switch (ri.getTipo())
            {
                case RoomInfo.TIPO.STROKE:
                case RoomInfo.TIPO.MATCH:
                case RoomInfo.TIPO.TOURNEY:
                case RoomInfo.TIPO.TOURNEY_TEAM:
                case RoomInfo.TIPO.GUILD_BATTLE:
                case RoomInfo.TIPO.APPROCH:
                case RoomInfo.TIPO.PANG_BATTLE:
                case RoomInfo.TIPO.GRAND_PRIX:
                case RoomInfo.TIPO.GRAND_ZODIAC_PRACTICE:
                case RoomInfo.TIPO.PRACTICE:
                case RoomInfo.TIPO.SPECIAL_SHUFFLE_COURSE:
                case RoomInfo.TIPO.LOUNGE:
                    _check = true;
                    break;
                default:
                    _check = false;
                    break;
            }

            if (!_check)
                ThrowHackException(session, ri, m_ci, "Tipo de jogo inválido: " + ri.getTipo());
        }

        // --------- ValidateMaxPlayers (ajustado para incluir regras do C++ e GM special) ----------
        private void ValidateMaxPlayers(Player session, RoomInfoEx ri, ChannelInfo m_ci)
        {
            int[] allowedPlayers;

            switch (ri.getTipo())
            {
                case RoomInfo.TIPO.STROKE:
                    allowedPlayers = new[] { 2, 3, 4 };
                    break;
                case RoomInfo.TIPO.MATCH:
                    allowedPlayers = new[] { 2, 4 };
                    break;
                case RoomInfo.TIPO.TOURNEY:
                case RoomInfo.TIPO.TOURNEY_TEAM:
                case RoomInfo.TIPO.GUILD_BATTLE:
                    allowedPlayers = new[] { 10, 20, 30 };
                    break;
                case RoomInfo.TIPO.APPROCH:
                    allowedPlayers = new[] { 6, 20, 30 };
                    break;
                case RoomInfo.TIPO.PANG_BATTLE:
                    allowedPlayers = new[] { 2, 4 };
                    break;
                case RoomInfo.TIPO.GRAND_ZODIAC_PRACTICE:
                case RoomInfo.TIPO.GRAND_ZODIAC_ADV:
                case RoomInfo.TIPO.GRAND_ZODIAC_INT:
                case RoomInfo.TIPO.PRACTICE:
                    allowedPlayers = new[] { 1 };
                    break;
                case RoomInfo.TIPO.SPECIAL_SHUFFLE_COURSE:
                    allowedPlayers = new[] { 30 };
                    break;
                case RoomInfo.TIPO.LOUNGE:
                    allowedPlayers = new[] { 10, 20, 30 };
                    break;
                default:
                    allowedPlayers = new int[0];
                    break;
            }

            if (allowedPlayers.Length > 0 && Array.IndexOf(allowedPlayers, ri.max_player) == -1)
                ThrowHackException(session, ri, m_ci, "max_player inválido: " + ri.max_player);
        }

        // --------- ValidateHoleCount (ajustado para suportar Shuffle e outros) ----------
        private void ValidateHoleCount(Player session, RoomInfoEx ri, ChannelInfo m_ci)
        {
            int[] allowedHoles;
            switch (ri.getTipo())
            {
                case RoomInfo.TIPO.STROKE:
                    allowedHoles = new[] { 3, 6, 9, 18 };
                    break;
                case RoomInfo.TIPO.MATCH:
                    allowedHoles = new[] { 6, 9, 18 };
                    break;
                case RoomInfo.TIPO.TOURNEY:
                case RoomInfo.TIPO.TOURNEY_TEAM:
                case RoomInfo.TIPO.GUILD_BATTLE:
                    allowedHoles = new[] { 9, 18 };
                    break;
                case RoomInfo.TIPO.APPROCH:
                    allowedHoles = new[] { 3, 6, 9 };
                    break;
                case RoomInfo.TIPO.PANG_BATTLE:
                    allowedHoles = new[] { 6, 9, 18 };
                    break;
                case RoomInfo.TIPO.SPECIAL_SHUFFLE_COURSE:
                    allowedHoles = new[] { 18 };
                    break;
                case RoomInfo.TIPO.GRAND_ZODIAC_PRACTICE:
                case RoomInfo.TIPO.GRAND_ZODIAC_ADV:
                case RoomInfo.TIPO.GRAND_ZODIAC_INT:
                    allowedHoles = new[] { 1 };
                    break;
                case RoomInfo.TIPO.PRACTICE:
                    allowedHoles = new[] { 1, 9, 18 };
                    break;
                case RoomInfo.TIPO.LOUNGE:
                    allowedHoles = new[] { 1 };
                    break;
                default:
                    allowedHoles = new int[0];
                    break;
            }

            if (allowedHoles.Length > 0 && Array.IndexOf(allowedHoles, ri.qntd_hole) == -1)
                ThrowHackException(session, ri, m_ci, "qntd_hole inválido: " + ri.qntd_hole);
        }

        // --------- ValidateForbiddenModes (mantive sua checagem) ----------
        private void ValidateForbiddenModes(Player session, RoomInfoEx ri, ChannelInfo m_ci)
        {
            if (ri.getTipo() == RoomInfo.TIPO.GRAND_ZODIAC_INT || ri.getTipo() == RoomInfo.TIPO.GRAND_ZODIAC_ADV)
            {
                ThrowHackException(session, ri, m_ci, "tentou criar modo proibido");
            }
        }

        // --------- Helpers de tempo (mantive seu comportamento, com checagens em segundos/minutos) ----------
        private void ValidateTime30s(Player session, RoomInfoEx ri, ChannelInfo m_ci, params uint[] allowedMinutes)
        {
            if (ri.getTipo() == RoomInfo.TIPO.APPROCH)//unico com time minute em segundos
            {
                if (ri.time_30s < (40 * 1000))
                    ThrowHackException(session, ri, m_ci, $"time_30s inválido para o approach: {ri.time_30s}");

                if (!allowedMinutes.Contains(ri.time_30s / 1000))
                    ThrowHackException(session, ri, m_ci, $"time_30s inválido para o approach: {ri.time_30s / 1000}");
            }
            else
            {
                if (ri.time_30s < (15 * 60000))
                    ThrowHackException(session, ri, m_ci, $"time_30s inválido: {ri.time_30s / 60000}");

                if (!allowedMinutes.Contains(ri.time_30s / 60000))
                    ThrowHackException(session, ri, m_ci, $"time_30s inválido: {ri.time_30s / 60000}");
            }
        }

        private void ValidateTimeVs(Player session, RoomInfoEx ri, ChannelInfo m_ci, params uint[] allowedSeconds)
        {
            if ((ri.getTipo() == RoomInfo.TIPO.STROKE || ri.getTipo() == RoomInfo.TIPO.MATCH)
               && ri.time_vs < (40 * 1000))
            {
                ThrowHackException(session, ri, m_ci, $"time_vs inválido: {ri.time_vs}");
            }

            if (!allowedSeconds.Contains(ri.time_vs / 1000))
            {
                ThrowHackException(session, ri, m_ci, $"time_vs inválido: {ri.time_vs}");
            }
        }

        // --------- ThrowHackException ----------
        private void ThrowHackException(Player session, RoomInfoEx ri, ChannelInfo m_ci, string motivo)
        {
            string msg = $"[channel::requestMakeRoom][Error] PLAYER [UID={(session != null ? session.m_pi.uid.ToString() : "NULL")}] " +
                         $"Channel[ID={(m_ci != null ? m_ci.id.ToString() : "NULL")}] tentou criar sala [Nome={ri.nome}, PWD={ri.senha}, TIPO={ri.getTipo()}], {motivo}. Hacker ou Bug";

            throw new exception(msg, ExceptionError.STDA_MAKE_ERROR_TYPE(
                STDA_ERROR_TYPE.CHANNEL, 10, 0x770001));
        }
    }
}
