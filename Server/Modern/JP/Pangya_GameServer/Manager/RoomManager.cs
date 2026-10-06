using Pangya_GameServer.Channels;
using Pangya_GameServer.Flags;
using Pangya_GameServer.Models.Game;
using Pangya_GameServer.Roms;
using Pangya_GameServer.Roms.GameBase.Helpers;
using Pangya_GameServer.Session;
using PangyaAPI.IFF.Regions.JP.Models.IFF;
using PangyaAPI.Network.Session;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using static Pangya_GameServer.Models.DefineConstants;

namespace Pangya_GameServer.Manager
{
    public class RoomManager
    {
        // Member 
        private Dictionary<short, bool> m_map_index = new Dictionary<short, bool>(short.MaxValue);
        private readonly object _lock = new object(); // se ainda não tiver 
        private short m_next_index;
        private List<Room> v_rooms = new List<Room>();
        private object m_room_lock = new object();
        public int Count => v_rooms.Count;
        public RoomManager()
        {
            if (m_map_index.Count == 0)
            {
                for (short i = 0; i < short.MaxValue; i++)
                {
                    m_map_index.Add(i, false);
                }
            }
        }

       
        public Room? MakeRoom(Channel _channel_owner, GameRoomInfoModel _ri, Player _session, int _option = 0)
        {
            Room? r = null;

            try
            {

                if (_session != null && _session.UserInfo.Member.RoomID != -1)
                {
                    throw new exception("[RoomManager::makeRoom][Error] Normal[UID=" + Convert.ToString(_session.UserInfo.UID) + "] sala[NUMERO=" + Convert.ToString(_session.UserInfo.Member.RoomID) + "], ja esta em outra sala, nao pode criar outra. Hacker ou Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM_MANAGER,
                        120, 0));
                }

                _ri.RoomID = getNewIndex();
                _ri.roomId = Guid.NewGuid();
                if (_option == 0 && _session != null)
                {
                    _ri.OwnerUID = (int)_session.UserInfo.UID;
                }
                else if (_option == 1) // Room Sem Master Grand Prix ou Grand Zodiac Event Time
                {
                    _ri.OwnerUID = -2;
                }
                else // Room sem Master
                {
                    _ri.OwnerUID = -1;
                }

                r = new Room(_channel_owner, _ri);

                if (r == null)
                {
                    throw new exception("[RoomManager::makeRoom][Error] Normal[UID=" + Convert.ToString(_session.UserInfo.UID) + "] tentou criar a sala[TIPO=" + Convert.ToString((ushort)_ri.RealRoomType) + "], mas nao conseguiu criar o objeto da classe room. Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM_MANAGER,
                        130, 0));
                }

                // Verifica se é um room válida e bloquea ela 
                r.trylock();

                if (_session != null)
                    r.EnterToRoom(_session);

                r.unlock(); 
            }
            catch (exception e)
            {
                if (r != null)
                {

                    // Destruindo a sala, não conseguiu
                    r.SetDestroying();

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

                _smp.LogManager.Instance.push(new AppMessage("[RoomManager::makeRoom][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            return r;
        }
         
        public RoomGrandPrix? MakeRoomGrandPrix(Channel _channel_owner, GameRoomInfoModel _ri, Player _session, GrandPrixData _gp, int _option = 0)
        {
            RoomGrandPrix? r = null;

            try
            {

                if (_session != null && _session.UserInfo.Member.RoomID != -1)
                {
                    throw new exception("[RoomManager::makeRoom][Error] Normal[UID=" + Convert.ToString(_session.UserInfo.UID) + "] sala[NUMERO=" + Convert.ToString(_session.UserInfo.Member.RoomID) + "], ja esta em outra sala, nao pode criar outra. Hacker ou Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM_MANAGER,
                        120, 0));
                }

                _ri.RoomID = getNewIndex();
                _ri.roomId = Guid.NewGuid();
                if (_option == 0 && _session != null)
                {
                    _ri.OwnerUID = (int)_session.UserInfo.UID;
                }
                else if (_option == 1) // Room Sem Master Grand Prix ou Grand Zodiac Event Time
                {
                    _ri.OwnerUID = -2;
                }
                else // Room sem Master
                {
                    _ri.OwnerUID = -1;
                }

                r = new RoomGrandPrix(_channel_owner, _ri, _gp);

                if (r == null)
                {
                    throw new exception("[RoomManager::makeRoom][Error] Normal[UID=" + Convert.ToString(_session.UserInfo.UID) + "] tentou criar a sala[TIPO=" + Convert.ToString((ushort)_ri.RealRoomType) + "], mas nao conseguiu criar o objeto da classe room. Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM_MANAGER,
                        130, 0));
                }

                // Verifica se é um room válida e bloquea ela 
                r.trylock();

                if (_session != null)
                    r.EnterToRoom(_session);

                r.unlock(); 
            }
            catch (exception e)
            {
                if (r != null)
                {

                    // Destruindo a sala, não conseguiu
                    r.SetDestroying();

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

                _smp.LogManager.Instance.push(new AppMessage("[RoomManager::makeRoom][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            return r;
        }
         
        public bool addRoom(Room r)
        {
            // Adiciona a sala no Vector
            v_rooms.Add(r);
            return v_rooms.Any(c => c.GetRoomId() == r.GetRoomId());
        }

        public void DestroyRoom(Room _room)
        {
            if (_room == null) return;

            lock (m_room_lock) // CRITICAL: Você PRECISA de um lock aqui para não crashar o servidor
            {
                try
                {
                    // 1. Verificar se a sala ainda está na lista (evita duplicidade de destruição)
                    if (v_rooms.Any(c => c.GetRoomId() == _room.GetRoomId()))
                    {

                        // 2. Log antes de limpar os dados
                        _smp.LogManager.Instance.push(new AppMessage(
                            $"[RoomManager::DestroyRoom][Sucess] DESTROYD[RID: {_room.GetRoomId()}, NAME: {_room.GetInfo().Name}]",
                            type_msg.CL_FILE_LOG_AND_CONSOLE));

                        // 3. Marcar como destruindo para as threads de rede pararem de processar pacotes nela
                        _room.SetDestroying();

                        // 4. Se a sala tem um OID (Owner ID) ou Index no seu sistema de slots
                        // Limpa o índice no seu array de controle (se você usa um)
                        clearIndex(_room.GetRoomId());

                        // 5. REMOVE DA LISTA
                        v_rooms.Remove(_room);

                        // 6. LIBERA MEMÓRIA E RECURSOS
                        // IMPORTANTE: O Dispose deve ser a última coisa, pois ele limpa os dados da sala
                        _room.Dispose(); 
                    }
                    else
                    { 
                        return;
                    }
                }
                catch (Exception e)
                {
                    _smp.LogManager.Instance.push(new AppMessage(
                        "[RoomManager::destroyRoom][ErrorSystem] " + e.Message,
                        type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
            }
        }

        // Opt sem sala practice, se não todas as salas
        public List<GameRoomInfoModel> getRoomsInfo(bool _without_practice_room = true)
        {

            List<GameRoomInfoModel> v_ri = new List<GameRoomInfoModel>();

            for (var i = 0; i < v_rooms.Count; ++i)
            {
                if (v_rooms[i] != null && (!_without_practice_room || (v_rooms[i].GetTipo() != RoomTypeFlags.PRACTICE && v_rooms[i].GetTipo() != RoomTypeFlags.GRAND_ZODIAC_PRACTICE)))
                {
                    v_ri.Add(v_rooms[i].GetInfo());
                }
            }
            return v_ri;
        }

        // Unlock Room
        public void UnlockRoom(Room _r)
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
                _smp.LogManager.Instance.push(new AppMessage("[RoomManager::unlockRoom][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public Room? FindRoom(Room? _r)
        {

            if (_r == null)
            {
                return null;
            }
             
            try
            {

                for (var i = 0; i < v_rooms.Count; ++i)
                {
                    if (v_rooms[i].GetRoomId() == _r.GetRoomId())
                    {
                        return v_rooms[i]; 
                    }
                }
            }
            catch (exception e)
            { 
                _smp.LogManager.Instance.push(new AppMessage("[RoomManager::findRoom][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            return null;
        }

        public Room? FindRoom(short _numero)
        {

            if (_numero == -1)
            {
                return null;
            }

            Room? r = null;

            try
            {

                for (var i = 0; i < v_rooms.Count; ++i)
                {
                    if (v_rooms[i].GetRoomId() == _numero)
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
                _smp.LogManager.Instance.push(new AppMessage("[RoomManager::findRoom][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

            }

            return r;
        }

        public RoomGrandPrix? FindRoomGrandPrix(uint _typeid)
        {

            if (_typeid == 0u)
            {
                return null;
            }

            RoomGrandPrix? r = null;

            try
            {

                foreach (var el in v_rooms.ToArray())
                {

                    if (el.GetInfo().grand_prix.active > 0
                        && el.GetInfo().grand_prix.dados_typeid != 0U
                        && el.GetInfo().grand_prix.dados_typeid == _typeid)
                    {

                        r = (RoomGrandPrix)el;

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
                _smp.LogManager.Instance.push(new AppMessage("[RoomManager::findRoomGrandPrix][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

            }

            return r;
        }

        private short getNewIndex()
        {
            short index = 0;

            lock (_lock) // substitui o CriticalSection
            {
                for (short i = 0; i < short.MaxValue; ++i)
                {
                    short candidate_index = (short)((m_next_index + i) % short.MaxValue);

                    if (!m_map_index[candidate_index])
                    {
                        index = candidate_index;
                        m_map_index[index] = true; // marca como ocupado
                        m_next_index = (short)((index + 1) % short.MaxValue);
                        break;
                    }
                }

                if (m_next_index >= short.MaxValue)
                    m_next_index = 0;
            }

            return index;
        }

        private void clearIndex(short _index)
        {
            // Removi a trava do short.MaxValue, usamos o limite do ushort (65535)
            if (m_map_index.ContainsKey(_index))
            {
                m_map_index[_index] = false; // Agora o slot está livre para getNewIndex()
            }
        }
    }
}
