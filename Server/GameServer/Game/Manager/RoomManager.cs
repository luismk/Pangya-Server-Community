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
        private readonly object _lock = new object(); // se ainda não tiver 
        ushort m_next_index;
        protected List<Room> v_rooms = new List<Room>();
        private object m_room_lock = new object();

        public RoomManager()
        { 
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


        public bool addRoom(Room r)
        {
            // Adiciona a sala no Vector
            v_rooms.Add(r);
            return v_rooms.Any(c => c.getNumero() == r.getNumero());
         }

        public void destroyRoom(Room _room)
        {
            if (_room == null) return;

            lock (m_room_lock) // CRITICAL: Você PRECISA de um lock aqui para não crashar o servidor
            {
                try
                {
                    // 1. Verificar se a sala ainda está na lista (evita duplicidade de destruição)
                    if (v_rooms.Any(c=> c.getNumero() == _room.getNumero()))
                    {

                        // 2. Log antes de limpar os dados
                        _smp.message_pool.getInstance().push(new message(
                            $"[RoomManager::destroyRoom][Log] Destruindo Sala [ID={_room.getNumero()}, Nome={_room.getInfo().name}]",
                            type_msg.CL_FILE_LOG_AND_CONSOLE));

                        // 3. Marcar como destruindo para as threads de rede pararem de processar pacotes nela
                        _room.setDestroying();

                        // 4. Se a sala tem um OID (Owner ID) ou Index no seu sistema de slots
                        // Limpa o índice no seu array de controle (se você usa um)
                        clearIndex(_room.getNumero());

                        // 5. REMOVE DA LISTA
                        v_rooms.Remove(_room);

                        // 6. LIBERA MEMÓRIA E RECURSOS
                        // IMPORTANTE: O Dispose deve ser a última coisa, pois ele limpa os dados da sala
                        _room.Dispose();
                    }
                    else
                    {
                        // 2. Log antes de limpar os dados
                        _smp.message_pool.getInstance().push(new message(
                            $"[RoomManager::destroyRoom][Log] nao encontrada",
                            type_msg.CL_FILE_LOG_AND_CONSOLE));

                        return;
                    }
                }
                catch (Exception e)
                {
                    _smp.message_pool.getInstance().push(new message(
                        "[RoomManager::destroy][ErrorSystem] " + e.Message,
                        type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
            }
        }

        // Make room Grand Zodiac Event
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

                r.trylock();

                if (_session != null)
                {
                    r.enter(_session);
                }
 
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
        public RoomGrandZodiacEvent makeRoomGrandZodiacEvent(byte _channel_owner, RoomInfoEx _ri, TimeSpan End)
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

                r.trylock();
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
                _smp.message_pool.getInstance().push(new message("[RoomManager::findRoom][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
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
                _smp.message_pool.getInstance().push(new message("[RoomManager::findRoom][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

            }

            return r;
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
                if (v_rooms[i] != null && (!_without_practice_room || (v_rooms[i].getInfo().getTipo() != RoomInfo.ROOM_INFO_TYPE.PRACTICE && v_rooms[i].getInfo().getTipo() != RoomInfo.ROOM_INFO_TYPE.GRAND_ZODIAC_PRACTICE)))
                {
                    v_ri.Add(v_rooms[i].getInfo());
                }
            }
            return v_ri;
        }

        public List<RoomGrandZodiacEvent> getAllRoomsGrandZodiacEvent()
        {

            List<RoomGrandZodiacEvent> v_r = new List<RoomGrandZodiacEvent>();

            foreach (var el in v_rooms)
            {
                if (el != null && (int)el.getMaster() == -2 && (el.getInfo().getTipo() == RoomInfo.ROOM_INFO_TYPE.GRAND_ZODIAC_ADV || el.getInfo().getTipo() == RoomInfo.ROOM_INFO_TYPE.GRAND_ZODIAC_INT))
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
                if (el != null && (RoomInfo.ROOM_INFO_TYPE)el.getInfo().tipo == RoomInfo.ROOM_INFO_TYPE.TOURNEY && el.getInfo().flag_gm == 1 && el.getInfo().state_flag == 0x100 && el.getInfo().trofel == TROFEL_GM_EVENT_TYPEID)
                {
                    v_r.Add((el) as RoomBotGMEvent);
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
            // Removi a trava do short.MaxValue, usamos o limite do ushort (65535)
            if (m_map_index.ContainsKey(_index))
            {
                m_map_index[_index] = false; // Agora o slot está livre para getNewIndex()
            }
        } 
    }
}
