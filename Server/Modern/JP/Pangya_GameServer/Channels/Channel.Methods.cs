using Pangya_GameServer.Feature;
using Pangya_GameServer.Flags;
using Pangya_GameServer.Handles;
using Pangya_GameServer.Manager;
using Pangya_GameServer.Models.Game;
using Pangya_GameServer.Models;
using Pangya_GameServer.PacketFunc;
using Pangya_GameServer.Repository;
using Pangya_GameServer.Roms;
using Pangya_GameServer.Roms.GameBase.Helpers;
using Pangya_GameServer.Server;
using Pangya_GameServer.Session;

using PangyaAPI.IFF.Handle.JP;
using PangyaAPI.IFF.Regions.JP.Models.IFF;
using PangyaAPI.Network;
using PangyaAPI.Network.Models;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using snmdb;
using System.Threading.Channels;
using static Pangya_GameServer.Models.DefineConstants;
namespace Pangya_GameServer.Channels
{
    public partial class Channel
    {
        #region GET LIS-ROOM/ INFO CHANNEL
        public ChannelInfo getInfo() => m_ci;
        public sbyte getId() => m_ci.id;
        public string getName() => m_ci.name;
        public RoomManager getRoomsInfo() => m_rm;
        public ServerProperty getProperty() => Type;
        #endregion

        #region GET SESSION/LIST SESSION 
        public PlayerLobbyInfo GetPlayerInfo(Player session)
        {
            lock (m_cs)
            {
                return Players_Info.ContainsKey(session) ? Players_Info[session] : null;
            }
        }
        public List<Player> GetSessions(byte _lobby = 255)
        {
            lock (m_cs)
            {
                // Se for -1, retornamos todos do canal que não estão em estado "default/inválido"
                if (_lobby == 255)
                {
                    return Sessions
                        .Where(s => s != null && s.UserInfo.Channel != DEFAULT_CHANNEL)
                        .ToList();
                }

                // Se passar um ID de lobby, filtra apenas quem está naquela lobby específica
                return Sessions
                    .Where(s => s != null &&
                                s.UserInfo.Channel != DEFAULT_CHANNEL &&
                                s.UserInfo.Lobby == _lobby)
                    .ToList();
            }
        }
        #endregion

        #region ADD/REMOVE/UPDATE/MAKE SESSION/SEND

        public void SendListUpdateRooms(GameRoomInfoModel r)
        {
            SendBroadcast(Handle_PACKET_RESPONSE.MakeGameRoomList([r], 3));
        }


        public async void makePlayerInfo(Player _session)
        {
            lock (m_cs) // Importante para thread-safety
            {
                PlayerLobbyInfo pci = new()
                {
                    UID = _session.UserInfo.UID,
                    OID = _session.ConnectionID,
                    RoomID = _session.UserInfo.Member.RoomID,
                    GameLevel = (byte)_session.UserInfo.Member.GameLevel,
                    Capability = _session.UserInfo.UserCapabilities,
                    NickName = _session.UserInfo.NickName,
                    DisplayID = "@NT_" + _session.UserInfo.NickName,
                    TitleSkin = _session.Inventory.UserEquipment.m_title,
                    LadderPoints = 1000,
                    GuildMarkIndex = _session.UserInfo.Guild.index_mark_emblem,
                    GuildIndex = _session.UserInfo.Guild.uid,
                    GuildMarkImage = _session.UserInfo.Guild.mark_emblem,
                    IsGMVisible = Convert.ToInt16(_session.UserInfo.Member.State.Visible)
                };

                // Lógica de Quit Rate / Ícones
                if (_session.UserInfo.Member.GameLevel >= 6 && _session.UserInfo.Statistics.jogado >= 50)
                {
                    float rate = _session.UserInfo.Statistics.getQuitRate();
                    if (rate < GOOD_PLAYER_ICON) pci.State.NoQuiterWings = 0;
                    else if (rate >= QUITER_ICON_1 && rate < QUITER_ICON_2) pci.State.QuiterLow = 1;
                    else if (rate >= QUITER_ICON_2) pci.State.QuiterHight = 1;
                }

                pci.State.Gender = _session.UserInfo.Member.Gender;
                pci.GuildIndex = _session.UserInfo.Guild.uid;

                // Adiciona ou Atualiza no dicionário
                if (!Players_Info.ContainsKey(_session))
                    Players_Info.Add(_session, pci);
                else
                    Players_Info[_session] = pci;

                // Update DB
            }
            _session.UserInfo.updateLocationDB();
        }

        public async void UpdatePlayerInfo(Player _session)
        {
            PlayerLobbyInfo pci;

            if ((pci = GetPlayerInfo(_session)) == null)
                return;//so retorna mesmo

            // Player Canal Info Update
            pci.NickName = _session.UserInfo.NickName;
            pci.UID = _session.UserInfo.UID;
            pci.OID = _session.ConnectionID;
            pci.RoomID = _session.UserInfo.Member.RoomID;
            pci.GameLevel =  _session.UserInfo.Member.GameLevel;
            pci.LadderPoints = 1000;
            pci.IsGMVisible = _session.UserInfo.Member.State.Visible;
            pci.Capability = _session.UserInfo.UserCapabilities;
            pci.TitleSkin = _session.Inventory.UserEquipment.m_title;
            pci.GuildMarkIndex = _session.UserInfo.Guild.index_mark_emblem;
            pci.GuildIndex = _session.UserInfo.Guild.uid;
            pci.GuildMarkImage = _session.UserInfo.Guild.mark_emblem;
            // Só faz calculo de Quita Rate depois que o player
            // estiver no Level Beginner E e jogado 50 games
            if (_session.UserInfo.Member.GameLevel >= 6 && _session.UserInfo.Statistics.jogado >= 50)
            {
                float rate = _session.UserInfo.Statistics.getQuitRate();

                if (rate < GOOD_PLAYER_ICON)
                    pci.State.NoQuiterWings = 1;
                else if (rate >= QUITER_ICON_1 && rate < QUITER_ICON_2)
                    pci.State.QuiterLow = 1;
                else if (rate >= QUITER_ICON_2)
                    pci.State.QuiterHight = 1;
            }

            if (_session.Inventory.UserEquippedItem.CharacterEquiped != null && _session.UserInfo.Statistics.getQuitRate() < GOOD_PLAYER_ICON)
                pci.State.AngelWings = 0;
            else
                pci.State.AngelWings = 0;

            pci.State.Gender = _session.UserInfo.Member.Gender;

            _session.SetChannel(this);//Update Location.

            // Update Location Player
            _session.UserInfo.updateLocationDB();
        }

        public void AddSession(Player _session)
        {
            if (_session == null)
            {
                throw new exception("[Channel::addSession][Error] _session is null or invalid.",
                    ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 3, 1));
            }

            lock (m_cs) // Garante que a lista v_sessions não corrompa
            {
                Sessions.Add(_session);
                m_ci.curr_user++;

                // Channel Login e localização inicial
                _session.UserInfo.Channel = m_ci.id;
                _session.UserInfo.Place = 0; // 0 = Lobby/Channel

                // Lógica de Condição (Quit Rate / Angel Icon)
                if (_session.UserInfo.Member.GameLevel >= 6 && _session.UserInfo.Statistics.jogado >= 50)
                {
                    float rate = _session.UserInfo.Statistics.getQuitRate();
                    if (rate < GOOD_PLAYER_ICON) _session.UserInfo.Member.State.Wings = 1;
                    else if (rate >= QUITER_ICON_1 && rate < QUITER_ICON_2) _session.UserInfo.Member.State.Quit10Porcent = 1;
                    else if (rate >= QUITER_ICON_2) _session.UserInfo.Member.State.Quit20Porcent = 1;
                }

                if (_session.Inventory.UserEquippedItem.CharacterEquiped != null && _session.UserInfo.Statistics.getQuitRate() < GOOD_PLAYER_ICON)
                {
                    _session.UserInfo.Member.State.AngelWings = _session.Inventory.UserEquippedItem.CharacterEquiped.AngelEquiped();
                }
                else
                {
                    _session.UserInfo.Member.State.AngelWings = 0;
                }

                _session.UserInfo.Member.Gender = (byte)(_session.UserInfo.Member.State.Gender);

                // Gera o objeto PlayerLobbyInfo que a Lobby.cs vai usar
                makePlayerInfo(_session);
            }
        }

        public void RemoveSession(Player _session)
        {
            if (_session == null) return;

            lock (m_cs)
            {
                if (Sessions.Remove(_session))
                {
                    m_ci.curr_user--;
                }

                // Reseta localização
                _session.SetChannel(null);
                 
                RemovePlayerInfo(_session);
            }
        }

        public async void RemovePlayerInfo(Player _session)
        {
            lock (m_cs)
            {
                // Remove do dicionário de visualização da Lobby
                if (Players_Info.Any(c=> _session.UserInfo.UID == c.Value.UID))
                {
                    Players_Info.Remove(_session);
                }
            }

            // Update Location player no banco antes de remover da memória
            _session.UserInfo.updateLocationDB();

        }

        #endregion

        #region SEND BROADCAST 
        public void SendBroadcast(List<Packet> packets)
        {
            lock (m_cs)
            {
                var targets = GetSessions();

                foreach (var session in targets)
                {
                    if (session != null && session.Connected)
                    {
                        foreach (var packet in packets)
                        {
                            session.Send(packet);
                        }
                    }
                }
            }
        }

        public void SendBroadcast(Packet packet)
        {
            lock (m_cs)
            {
                var targets = GetSessions();

                foreach (var session in targets)
                {
                    if (session != null && session.Connected)
                    {
                        session.Send(packet);
                    }
                }
            }
        }

        public void SendBroadcast(Packet packet, byte slobbyId)
        {
            lock (m_cs)
            {
                var targets = GetSessions(slobbyId);

                foreach (var session in targets)
                {
                    if (session != null && session.Connected)
                    {
                        session.Send(packet);
                    }
                }
            }
        }

        public void SendBroadcast(List<Packet> packets, byte lobbyId)
        {
            if (packets == null || packets.Count == 0) return;

            lock (m_cs)
            {
                // Filtra apenas quem está na lobby e está com a conexão ativa
                var targets = GetSessions(lobbyId);

                foreach (var session in targets)
                {
                    if (session == null)
                        continue;

                    foreach (var packet in packets)
                    {
                        session.Send(packet);
                    }
                }
            }
        }
         
        #endregion

        #region FIND PLAYER/ROOM

        public void CheckRoom(Player _session, GameRoomInfoModel _ri)
        {
            Lobby._FilterHacker.HandleRoom(_session, _ri, getInfo());
        }

        public Player FindSessionByOID(uint _oid)
        {
            lock (m_cs)
            {
                return Sessions.FirstOrDefault(c => c != null && c.ConnectionID == _oid);
            }
        }

        public Player FindSessionByUID(int _uid)
        {
            lock (m_cs)
            {
                return Sessions.FirstOrDefault(c => c != null && c.UserInfo.UID == _uid);
            }
        }

        public Player FindSessionByNickname(string _nickname)
        {
            lock (m_cs)
            {
                return Sessions.FirstOrDefault(c => c != null && c.UserInfo.NickName == _nickname);
            }
        }

        public int findIndexSession(Player _session)
        {
            if (_session == null) return -1;

            lock (m_cs)
            {
                return Sessions.IndexOf(_session);
            }
        }

        #endregion

        #region TIMERS INVITED
        public void startInviteTime()
        {
            if (TimeInvite != null && (TimeInvite.getState() == PangyaSyncTimer.TIMER_STATE.STOP || TimeInvite.getState() == PangyaSyncTimer.TIMER_STATE.FINISH))
                stopInviteTime();

            if (TimeInvite == null)//na primeira vez...
                TimeInvite = GameServer.Instance.MakeTimer(10 * 1000, () => checkInviteTime(), new List<long>(), PangyaSyncTimer.TIMER_TYPE.NORMAL);
        }

        public void stopInviteTime()
        {
            // Garantir que qualquer exception derrube o server
            try
            {

                if (TimeInvite != null)
                    GameServer.Instance.DeleteTimer(TimeInvite);
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[Channel::stopInviteTime][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            TimeInvite = null;
        }

        public void checkInviteTime()
        {
            lock (m_cs_invite)
            {
                if (sInvites.Count == 0)
                {
                    stopInviteTime(); // Para o timer se não houver convites
                    return;
                }

                for (int i = sInvites.Count - 1; i >= 0; i--)
                {
                    var cii = sInvites[i];
                    if (SendTimeOutInvite(cii))
                    {
                        _smp.LogManager.Instance.push(
                          new AppMessage($"[Channel::checkInviteTime][Warning] Remove UID={cii.invited_uid}",
                          type_msg.CL_FILE_LOG_AND_CONSOLE));


                        sInvites.RemoveAt(i);
                    }
                }
            }
        }

        public void DeleteInviteTimeRequest(InviteChannelInfo _ici)
        {
            // Validações iniciais (Hacker check) com as Exceptions completas
            if (_ici.room_number == -1)
            {
                throw new exception("[Channel::deleteInviteTimeRequest][Error] Channel[ID=" + ((ushort)m_ci.id) +
                    "] tentou deletar Invite Time Request[INVITE=" + _ici.invite_uid + ", INVITED=" + _ici.invited_uid +
                    "] para sala[NUMERO=" + _ici.room_number + "], mas o RoomID da sala é invalido. Hacker ou Bug",
                    ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 3011, 0));
            }

            if (_ici.invite_uid == 0)
            {
                throw new exception("[Channel::deleteInviteTimeRequest][Error] Channel[ID=" + ((ushort)m_ci.id) +
                    "] tentou deletar Invite Time Request[INVITE=" + _ici.invite_uid + ", INVITED=" + _ici.invited_uid +
                    "] para sala[NUMERO=" + _ici.room_number + "], mas quem convidou o UID is invalid(zero)",
                    ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 3011, 1));
            }

            if (_ici.invited_uid == 0)
            {
                throw new exception("[Channel::deleteInviteTimeRequest][Error] Channel[ID=" + ((ushort)m_ci.id) +
                    "] tentou deletar Invite Time Request[INVITE=" + _ici.invite_uid + ", INVITED=" + _ici.invited_uid +
                    "] para sala[NUMERO=" + _ici.room_number + "], mas o Invite UID is invalid(zero)",
                    ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 3011, 2));
            }

            try
            {
                lock (m_cs_invite)
                {
                    int index = sInvites.FindIndex(_el =>
                        _el.room_number == _ici.room_number &&
                        _el.invite_uid == _ici.invite_uid &&
                        _el.invited_uid == _ici.invited_uid);

                    if (index != -1) // No C#, FindIndex retorna -1 se não achar
                    {
                        sInvites.RemoveAt(index);
                    }
                    else
                    {
                        _smp.LogManager.Instance.push(new AppMessage(
                            "[Channel::deleteInviteTimeRequest][Sucess] Channel[ID=" + ((ushort)m_ci.id) +
                            "] tentou deletar Invite Time Request[INVITE=" + _ici.invite_uid + ", INVITED=" + _ici.invited_uid +
                            "] para sala[NUMERO=" + _ici.room_number + "], mas ele nao existe mais no List do canal.",
                            type_msg.CL_FILE_LOG_AND_CONSOLE));
                    }
                }
            }
            catch (Exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[Channel::deleteInviteTimeRequest][ErrorSystem] " + e.Message, type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public bool SendTimeOutInvite(InviteChannelInfo _ici)
        {
            // 1. Busca a sala no RoomManager do canal
            var r = FindRoom(_ici.room_number);

            if (r == null)
            {
                // Sala não existe, convite pode ser removido
                return true;
            }

            try
            {
                // 2. Busca o player Invite no canal
                var s = FindSessionByUID((int)_ici.invited_uid);

                if (s == null)
                {
                    // Player deslogou ou mudou de canal, remove por UID
                    r.DeleteInvited(_ici.invited_uid);
                }
                else
                {
                    // Player encontrado, remove a ServerFlag de Invite dele na sala
                    r.DeleteInvited(s);

                    // Opcional: Enviar pacote de "Invite Timeout" para o player 's' se necessário
                }

                // 3. Notifica o canal que a sala mudou (ex: número de convidados alterado)
                SendUpdateRoomInfo(r.GetInfo(), 3);
            }
            catch (Exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[Channel::send_time_out_invite][Error] " + e.Message, type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            return true; // Indica que o processamento terminou e o item pode sair da lista
        }

        public void DeleteInviteTimeResquestByInvited(Player _session)
        {
            try
            {
                lock (m_cs_invite)
                {
                    // Itera de trás para frente para remover itens com segurança
                    for (int i = sInvites.Count - 1; i >= 0; i--)
                    {
                        if (sInvites[i].invited_uid == _session.UserInfo.UID)
                        {
                            var r = FindRoom(sInvites[i].room_number);

                            // Se a sala ainda existe, remove a ServerFlag de Invite dela
                            if (r != null && r.IsInvited(_session))
                            {
                                r.DeleteInvited(_session);
                                SendUpdateRoomInfo(r.GetInfo(), 3); // Atualiza o Lobby sobre a sala
                            }

                            // Remove o convite da lista do Canal
                            sInvites.RemoveAt(i);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[Channel::deleteInviteTimeRequestByInvited][Error] " + e.Message, type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void AddInviteTimeRequest(InviteChannelInfo _ici)
        {

            if (_ici.room_number < 0)
            {
                throw new exception("[Channel::addInviteTimeRequest][Error] Channel[ID=" + ((ushort)m_ci.id) + "] tentou adicionar Invite Time Request[INVITE=" + (_ici.invite_uid) + ", INVITED=" + (_ici.invited_uid) + "] para sala[NUMERO=" + (_ici.room_number) + "], mas o RoomID da sala é invalido. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                    3010, 0));
            }

            if (_ici.invite_uid == 0u)
            {
                throw new exception("[Channel::addInviteTimeRequest][Error] Channel[ID=" + ((ushort)m_ci.id) + "] tentou adicionar Invite Time Request[INVITE=" + (_ici.invite_uid) + ", INVITED=" + (_ici.invited_uid) + "] para sala[NUMERO=" + (_ici.room_number) + "], mas quem convidou o UID is invalid(zero)", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                    3010, 1));
            }

            if (_ici.invited_uid == 0u)
            {
                throw new exception("[Channel::addInviteTimeRequest][Error] Channel[ID=" + ((ushort)m_ci.id) + "] tentou adicionar Invite Time Request[INVITE=" + (_ici.invite_uid) + ", INVITED=" + (_ici.invited_uid) + "] para sala[NUMERO=" + (_ici.room_number) + "], mas o Invite UID is invalid(zero)", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                    3010, 2));
            }
            sInvites.Add(_ici);
        }
        #endregion

        #region SEND PACKET ROOM/ PACKET LOBBY 
        public void SendUpdateRoomInfo(GameRoomInfoModel _ri, int _option)
        {
            if (_ri != null && _ri.GetRoomType() != RoomTypeFlags.PRACTICE && _ri.GetRoomType() != RoomTypeFlags.GRAND_ZODIAC_PRACTICE)
            {
                SendBroadcast(Handle_PACKET_RESPONSE.MakeGameRoomList(new List<GameRoomInfoModel>() { _ri }, _option), 0);
            }
        }


        public void SendUpdateRoomInfo(short roomID, int _option)
        {
            var _ri = FindRoom(roomID)?.GetInfo();
            if (_ri != null && _ri.GetRoomType() != RoomTypeFlags.PRACTICE && _ri.GetRoomType() != RoomTypeFlags.GRAND_ZODIAC_PRACTICE)
            {
                SendBroadcast(Handle_PACKET_RESPONSE.MakeGameRoomList(new List<GameRoomInfoModel>() { _ri }, _option), 0);
            }
        }

        public void SendUpdatePlayerInfo(Player _session, int _option)
        {
            PlayerLobbyInfo pci = GetPlayerInfo(_session);

            SendBroadcast(Handle_PACKET_RESPONSE.MakePlayerLobby(new List<PlayerLobbyInfo>() { (pci == null) ? new PlayerLobbyInfo() : pci }, _option));
        }


        public bool EnterChannel(Player _session)
        {
            try
            {

                if (!_session.getState())
                {
                    throw new exception("[Channel::enterChannel][Error] player nao esta conectado.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        1, 1));
                }

                if (_session.UserInfo.Channel != -1)
                {
                    throw new exception("[Channel::enterChannel][Error] Normal [UID=" + (_session.UserInfo.UID) + "] ja esta conectado em outro canal.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        2, 2));
                }

                AddSession(_session);

                _smp.LogManager.Instance.push(new AppMessage($"[Channel::EnterChannel][Sucess] CHANNEL[ID: {m_ci.id}, Users: {m_ci.curr_user}/{m_ci.max_user}, Rooms: {m_rm.getRoomsInfo().Count}]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                _session.Send(Handle_PACKET_RESPONSE.pacote095(0x102));//update Channel

                _session.Send(Handle_PACKET_RESPONSE.pacote04E(1));//enter Channel 

                // Verifica se o tempo do ticket premium user acabou e manda a mensagem para o player, e exclui o ticket do player no SERVER, DB e GAME
                sPremiumSystem.Instance.CheckEndTimeTicket(_session);
                return true;
            }
            catch (exception e)
            {
                RemoveSession(_session);

                _smp.LogManager.Instance.push(new AppMessage("[Channel::EnterChannel][Error] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                return false;

            }
        }


        public void LeaveChannel(Player _session)
        {
            try
            {

                if (_session.UserInfo.Lobby != 255)
                {
                    this.Lobby.LeaveLobby(_session); // Sai da Lobby'
                }
                else // Sai da Sala Practice que não entra na lobby, [SINGLE PLAY]
                {
                    this.Lobby.LeaveRoom(_session, 0);
                }

                RemoveSession(_session);
                if (_session.GetRoom() != null)
                    Lobby.LeaveRoom(_session, 0);

                _smp.LogManager.Instance.push(new AppMessage($"[Channel::LeaveChannel][Warning] CHANNEL[ID: {m_ci.id}, Users: {m_ci.curr_user}/{m_ci.max_user}, Rooms: {m_rm.getRoomsInfo().Count()}]", type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
            catch (exception e)
            {
                RemoveSession(_session);

                _smp.LogManager.Instance.push(new AppMessage("[Channel::LeaveChannel][Error] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if (!ExceptionError.STDA_ERROR_CHECK_SOURCE_AND_ERROR_TYPE(e.getCodeError(), // Diferente do error do Channel
                    STDA_ERROR_TYPE.CHANNEL, 1))
                {
                    throw;
                }
            }
        }


        public bool CheckEnterChannel(Player _session)
        {
            // Não é GM verifica se o player pode entrar nesse canal
            if (!_session.UserInfo.UserCapabilities.IsGameMaster)
            {

                if (_session.UserInfo.Member.GameLevel < m_ci.min_level_allow || _session.UserInfo.Member.GameLevel > m_ci.max_level_allow)
                    throw new exception("[Channel::CheckEnterChannel][Error] Normal [UID=" + (_session.UserInfo.UID) + ", LEVEL=" + (_session.UserInfo.Member.GameLevel)
                        + "] nao tem o Level necessario para entrar no canal[ID=" + (m_ci.id) + ", MIN=" + m_ci.min_level_allow
                        + ", MAX=" + m_ci.max_level_allow + "].");

                if (m_ci.type.only_rookie && _session.UserInfo.Member.GameLevel > (short)enLEVEL.ROOKIE_A)
                    throw new exception("[Channel::CheckEnterChannel][Error] Normal [UID=" + (_session.UserInfo.UID) + ", LEVEL=" + (_session.UserInfo.Member.GameLevel)
                        + "] nao tem o Level necessario para entrar no canal[ID=" + (m_ci.id) + ", MIN=" + m_ci.min_level_allow
                        + ", MAX=" + m_ci.max_level_allow + "] com a type So Rookie.");

                if (m_ci.type.LowLevel && _session.UserInfo.Member.GameLevel > (short)enLEVEL.JUNIOR_A)
                    throw new exception("[Channel::CheckEnterChannel][Error] Normal [UID=" + (_session.UserInfo.UID) + ", LEVEL=" + (_session.UserInfo.Member.GameLevel)
                        + "] nao tem o Level necessario para entrar no canal[ID=" + (m_ci.id) + ", MIN=" + m_ci.min_level_allow
                        + ", MAX=" + m_ci.max_level_allow + "] com a type Junior A pra baixo.");

                if (m_ci.type.HighLevel && _session.UserInfo.Member.GameLevel < (short)enLEVEL.JUNIOR_E)
                    throw new exception("[Channel::CheckEnterChannel][Error] Normal [UID=" + (_session.UserInfo.UID) + ", LEVEL=" + (_session.UserInfo.Member.GameLevel)
                        + "] nao tem o Level necessario para entrar no canal[ID=" + (m_ci.id) + ", MIN=" + m_ci.min_level_allow
                        + ", MAX=" + m_ci.max_level_allow + "] com a type Junior E pra cima.");

                if (m_ci.type.senior && (_session.UserInfo.Member.GameLevel < (short)enLEVEL.JUNIOR_E || _session.UserInfo.Member.GameLevel > (short)enLEVEL.SENIOR_A))
                    throw new exception("[Channel::CheckEnterChannel][Error] Normal [UID=" + (_session.UserInfo.UID) + ", LEVEL=" + (_session.UserInfo.Member.GameLevel)
                        + "] nao tem o Level necessario para entrar no canal[ID=" + (m_ci.id) + ", MIN=" + m_ci.min_level_allow
                        + ", MAX=" + m_ci.max_level_allow + "] com a type junior E a Senior A.");

                if (m_ci.type.beginner && (_session.UserInfo.Member.GameLevel < (short)enLEVEL.BEGINNER_E || _session.UserInfo.Member.GameLevel > (short)enLEVEL.JUNIOR_A))
                    throw new exception("[Channel::CheckEnterChannel][Error] Normal [UID=" + (_session.UserInfo.UID) + ", LEVEL=" + (_session.UserInfo.Member.GameLevel)
                        + "] nao tem o Level necessario para entrar no canal[ID=" + (m_ci.id) + ", MIN=" + m_ci.min_level_allow
                        + ", MAX=" + m_ci.max_level_allow + "] com a type Beginner E a Junior A.");
                return true;
            }

            return true;

        }
        #endregion

        #region REMOVE-ROOM/ADD-ROOM/MAKE-ROOM/GET-ROOM
        public Room? FindRoom(short id)
        {
            return m_rm.FindRoom(id);
        }

        public Room? FindRoom(Room _r)
        {
            return m_rm.FindRoom(_r);
        }

        public RoomGrandPrix? FindRoomGrandPrix(uint _typeid)
        {
            return m_rm.FindRoomGrandPrix(_typeid);
        } 

        public bool ExistRoom(short id)
        {
            return FindRoom(id) != null;
        }

        public Room? MakeRoom(GameRoomInfoModel ri, Player _session)
        {
            return m_rm.MakeRoom(this, ri, _session);
        }

        public RoomGrandPrix? MakeRoomGrandPrix(GameRoomInfoModel _ri, Player _session, GrandPrixData _gp, int _option = 0)
        {
            return m_rm.MakeRoomGrandPrix(this, _ri, _session, _gp, _option);
        } 

        public void AddRoom(Room r)
        {
            m_rm.addRoom(r);
        }

        public LEAVE_ROOM_STATE LeaveRoom(Player _session, int _option)
        {
            LEAVE_ROOM_STATE state = LEAVE_ROOM_STATE.DO_NOTHING;

            // Busca a sala de forma segura
            var r = _session.GetRoom();

            if (r != null)
            {
                int opt = 0;
                try
                {
                    // 1. Gerenciamento de Convites
                    if (r.IsInvited(_session))
                    {
                        var ici = r.DeleteInvited(_session);
                        DeleteInviteTimeRequest(ici); // Método que está no Channel.cs
                    }
                    else
                    {
                        opt = r.Leave(_session, _option);
                    }

                    // 2. Limpeza de convites órfãos (se não sobrou ninguém "real" na sala)
                    var all_invite = r.getAllInvite();
                    if (r.GetNumPlayers() == all_invite.Count)
                    {
                        foreach (var invite_item in all_invite.ToList())
                        {
                            // Busca no Singleton global do GameServer (sgs)
                            Player s = GameServer.Instance.FindPlayer(invite_item.invited_uid);
                            var ici = (s == null) ? r.DeleteInvited(invite_item.invited_uid) : r.DeleteInvited(s);

                            if (ici.room_number >= 0 && ici.invited_uid > 0 && ici.invite_uid > 0) DeleteInviteTimeRequest(ici);
                        }
                    }
                }
                catch (Exception e)
                {
                    _smp.LogManager.Instance.push(new AppMessage("[Channel::leaveRoom][Error] " + e.Message, type_msg.CL_FILE_LOG_AND_CONSOLE));
                }

                // 3. Atualiza o estado do player no Canal/Lobby
                UpdatePlayerInfo(_session);

                // 4. Verificação de Destruição da Sala
                if (r.GetNumPlayers() > 0 || opt == 0/*Não exclui a sala*/)
                {
                    r.SendHeadRoom(); // Atualiza pacotes internos da sala
                    r.SendPlayerInfo(_session, 2); // Avisa a sala que o player saiu

                    // Sincroniza Lobby: Avisa que o player mudou de estado e a sala mudou de info
                    SendUpdatePlayerInfo(_session, 3);
                    SendUpdateRoomInfo(r.GetInfo(), 3);
                     
                    if (opt == 0x801 && r.GetNumPlayers() > 0)
                    {
                        try
                        {
                            var playersToKick = r.GetSessions().ToList();
                            foreach (var p in playersToKick)
                            {
                                if (LeaveRoom(p, 0x800) == LEAVE_ROOM_STATE.ROOM_DESTROYED)
                                {
                                    break;
                                }
                            }
                        }
                        catch (exception e)
                        {
                            _smp.LogManager.Instance.push(new AppMessage("[Channel::LeaveRoom][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                            throw;
                        }
                    }
                }
                else
                {
                    // Destruição física da sala
                    GameRoomInfoModel ri = r.GetInfo();
                    r.SetDestroying();
                    m_rm.DestroyRoom(r);
                    
                    SendUpdatePlayerInfo(_session, 3);
                    SendUpdateRoomInfo(ri, 2); // Avisa o Lobby que a sala sumiu (Type 2)
                    //manda a lista de novo.
                    _session.Send(Handle_PACKET_RESPONSE.MakeGameRoomList(m_rm.getRoomsInfo(), 0)); 
                    state = LEAVE_ROOM_STATE.ROOM_DESTROYED;
                }

                if (state < LEAVE_ROOM_STATE.ROOM_DESTROYED)
                    state = LEAVE_ROOM_STATE.SEND_UPDATE_CLIENT;
            }
            else if (_option == 1)
                _smp.LogManager.Instance.push(new AppMessage($"[Channel::LeaveRoom][Warning] Normal[UID:{_session.UserInfo}] Try Exit to Room[RID: {_session.UserInfo.Member.RoomID}], NOT EXIST. Hacker ou Bug", type_msg.CL_ONLY_CONSOLE));
             
            return state;
        }

        public LEAVE_ROOM_STATE LeaveRoomMultiPlayer(Player _session, int _option)
        {

            var state = LeaveRoom(_session, _option);

            if (state > LEAVE_ROOM_STATE.DO_NOTHING)
            {
                _session.Send(Handle_PACKET_RESPONSE.pacote04C(-1));
            }

            return state;
        }

        public LEAVE_ROOM_STATE LeaveRoomGrandPrix(Player _session, int _option)
        {

            var state = LeaveRoom(_session, _option);

            if (state > LEAVE_ROOM_STATE.DO_NOTHING)
            {

                Packet p = new(0x254);

                p.WriteUInt32(0u); // OK

                p.WriteInt16(-1); // ServerFlag

                _session.Send(p);
            }

            return state;
        }

        public LEAVE_ROOM_STATE KickPlayerRoom(Player _session, byte force)
        {

            var state = LeaveRoom(_session, (force == 1u) ? 3 : 0x800);

            if (state > LEAVE_ROOM_STATE.DO_NOTHING && true)
            {
                Packet p = new(0x7E);

                p.WriteUInt32(0x800);

                _session.Send(p);
            }

            return state;
        }
        #endregion

        #region CUSTOM METHODS
        public bool CommandByChat(Player _session, Queue<string> _command)
        {

            string cmd = "command_chat";//wind
            Room? r = null;
            cmd = _command.Dequeue();
            try
            {
                //comandos via chat ;)
                var p = new Packet();
                if (!string.IsNullOrEmpty(cmd))
                {
                    r = _session.GetRoom();

                    if (_session.UserInfo.Member.Capability.IsGameMaster)      //comandos [player gm/adm]
                    {
                        if (r != null) //comandos em sala
                        { 
                            if (cmd == ("@notice") || cmd == ("@noticia"))
                            {
                                string msg = "notice";
                                msg = msg = string.Join(separator: " ", _command.ToArray());

                                NormalManagerDB.Instance.add(0, new CmdInsertNotice(msg, 1, 1), null, null);

                                // Send Message
                                p.init_plain(0x40); // Msg to Chat of player

                                p.WriteByte(7); // Notice

                                p.WriteString("@INI3");
                                p.WriteString("\\c0xff00ff00\\cSend Notice-Broadcast");

                                _session.Send(p);

                                return true;
                            }

                            if (cmd == ("@bot") && r.GetNumPlayers() == 1 && !r.GameRun())
                            {
                                if (!r.IsWithBot() && !r.IsRoomGM() && r.GetTipo() == RoomTypeFlags.STROKE || r.GetTipo() == RoomTypeFlags.TOURNEY || r.GetTipo() == RoomTypeFlags.SPECIAL_SHUFFLE_COURSE)
                                {

                                    try
                                    {
                                        r.SetSenha("bot");
                                        r.MakeRoomBot(_session);
                                        if (r.IsWithBot())
                                        {
                                            // Send Message
                                            p.init_plain(0x40); // Msg to Chat of player

                                            p.WriteByte(7); // Notice

                                            p.WriteString("@INI3");
                                            p.WriteString("\\c0xff00ff00\\cCall bot by chat.");

                                            _session.Send(p);
                                        }

                                        return true;
                                    }
                                    catch (exception)
                                    {
                                        return false;
                                    }
                                }
                                return false;
                            }
                            if (cmd == ("@play") && r.GetNumPlayers() > 1 && !(r.CurrentGame != null) && (r.GetTipo() == RoomTypeFlags.STROKE || r.GetTipo() == RoomTypeFlags.MATCH || r.GetTipo() == RoomTypeFlags.TOURNEY || r.GetTipo() == RoomTypeFlags.SPECIAL_SHUFFLE_COURSE))
                            {
                                r.SetAllReady();//inicia tudo mundo aqui
                                                // Send Message
                                p.init_plain(0x40); // Msg to Chat of player

                                p.WriteByte(7); // Notice

                                p.WriteString("@INI3");
                                p.WriteString("\\c0xff00ff00\\cAuto Start Room.");

                                _session.Send(p);

                                return true;
                            }
                            if (cmd == ("@big_char") && r.GetTipo() == RoomTypeFlags.LOUNGE)
                            {
                                var it = _session.UserInfo.FindStateCharacterLounger(_session.Inventory.UserEquippedItem.CharacterEquiped.id);
                                if (it != null)
                                {
                                    float scale_head = Convert.ToSingle(_command.Dequeue());


                                    it.scale_head = scale_head;
                                    p.init_plain(0x196);
                                    p.WriteInt32(_session.ConnectionID);
                                    p.WriteBytes(it.ToArray());
                                    r.SendBroadCast(p);
                                    // Send Message
                                    p.init_plain(0x40); // Msg to Chat of player

                                    p.WriteByte(7); // Notice

                                    p.WriteString("@INI3");
                                    p.WriteString("\\c0xff00ff00\\cChange Character head");

                                    _session.Send(p);

                                }
                            }
                            if (cmd == ("@speed_char") && r.GetTipo() == RoomTypeFlags.LOUNGE)
                            {
                                var it = _session.UserInfo.FindStateCharacterLounger(_session.Inventory.UserEquippedItem.CharacterEquiped.id);
                                if (it != null)
                                {
                                    float scale_head = Convert.ToSingle(_command.Dequeue());


                                    it.walk_speed = scale_head;
                                    p.init_plain(0x196);
                                    p.WriteInt32(_session.ConnectionID);
                                    p.WriteBytes(it.ToArray());
                                    r.SendBroadCast(p);
                                    // Send Message
                                    p.init_plain(0x40); // Msg to Chat of player

                                    p.WriteByte(7); // Notice

                                    p.WriteString("@INI3");
                                    p.WriteString("\\c0xff00ff00\\cChange to Speed Character");

                                    _session.Send(p);

                                }
                            }
                            if (cmd == ("@un_char") && r.GetTipo() == RoomTypeFlags.LOUNGE)
                            {
                                var it = _session.UserInfo.FindStateCharacterLounger(_session.Inventory.UserEquippedItem.CharacterEquiped.id);
                                if (it != null)
                                {
                                    float scale_head = Convert.ToSingle(_command.Dequeue());


                                    it.fUnknown = scale_head;
                                    p.init_plain(0x196);
                                    p.WriteInt32(_session.ConnectionID);
                                    p.WriteBytes(it.ToArray());
                                    r.SendBroadCast(p);
                                    // Send Message
                                    p.init_plain(0x40); // Msg to Chat of player

                                    p.WriteByte(7); // Notice

                                    p.WriteString("@INI3");
                                    p.WriteString("\\c0xff00ff00\\cChange to un");

                                    _session.Send(p);

                                }
                            }
                            if (cmd == ("@cam_char") && r.GetTipo() == RoomTypeFlags.LOUNGE)
                            {
                                var it = _session.UserInfo.FindStateCharacterLounger(_session.Inventory.UserEquippedItem.CharacterEquiped.id);
                                if (it != null)
                                {
                                    float scale_head = Convert.ToSingle(_command.Dequeue());

                                    it.camera_zoom = scale_head;
                                    p.init_plain(0x196);
                                    p.WriteInt32(_session.ConnectionID);
                                    p.WriteBytes(it.ToArray());
                                    r.SendBroadCast(p);
                                    // Send Message
                                    p.init_plain(0x40); // Msg to Chat of player

                                    p.WriteByte(7); // Notice

                                    p.WriteString("@INI3");
                                    p.WriteString("\\c0xff00ff00\\cChange to camera by Character");

                                    _session.Send(p);

                                }
                            }
                            if (cmd == ("@wind") && (r.GetTipo() == RoomTypeFlags.GRAND_PRIX || r.GetTipo() == RoomTypeFlags.MATCH || r.GetTipo() == RoomTypeFlags.STROKE || r.GetTipo() == RoomTypeFlags.TOURNEY || r.GetTipo() == RoomTypeFlags.LOUNGE) || r.GetTipo() == RoomTypeFlags.SPECIAL_SHUFFLE_COURSE || r.GetTipo() == RoomTypeFlags.PRACTICE || r.GetTipo() == RoomTypeFlags.GRAND_ZODIAC_PRACTICE)
                            {
                                if (_command.Count < 2)
                                    return false; // não tem argumentos suficientes

                                if (!ushort.TryParse(_command.Dequeue(), out ushort wind) ||
                                    !ushort.TryParse(_command.Dequeue(), out ushort degree))
                                {
                                    // Mensagem de erro pro GM 
                                    _session.SendChatNotice("\\c0xff00ff00\\c Values Invalid to Wind.");
                                    return true;
                                }

                                // Atualiza vento
                                p.init_plain(0x5B);
                                p.WriteUInt16(wind);
                                p.WriteUInt16(degree);
                                p.WriteByte(1);
                                r.SendBroadCast(p);

                                // Mensagem de confirmação 
                                _session.SendChatNotice("\\c0xff00ff00\\c Change Wind by GM.");
                                return true;
                            }
                            if (cmd == ("@weather") && (r.GetTipo() == RoomTypeFlags.MATCH || r.GetTipo() == RoomTypeFlags.STROKE || r.GetTipo() == RoomTypeFlags.TOURNEY || r.GetTipo() == RoomTypeFlags.LOUNGE) || r.GetTipo() == RoomTypeFlags.SPECIAL_SHUFFLE_COURSE || r.GetTipo() == RoomTypeFlags.PRACTICE || r.GetTipo() == RoomTypeFlags.GRAND_ZODIAC_PRACTICE)
                            {
                                ushort m_weather_lounge = 0;

                                // Pega o próximo argumento
                                string input = _command.Dequeue()?.Trim() ?? "";

                                // Remove caracteres que não sejam letras ou números (opcional)
                                string clean = new string(input.Where(char.IsLetterOrDigit).ToArray());

                                // Tenta interpretar primeiro como palavra
                                switch (clean.ToLowerInvariant())
                                {
                                    case "default":
                                        m_weather_lounge = 1;
                                        break;
                                    case "night":
                                        m_weather_lounge = 1;
                                        break;
                                    case "rain":
                                        m_weather_lounge = 2;
                                        break;
                                    case "snow":
                                        m_weather_lounge = 3;
                                        break;
                                    default:
                                        // Se não for palavra conhecida, tenta número
                                        if (!ushort.TryParse(clean, out m_weather_lounge))
                                            m_weather_lounge = 0; // padrão
                                        break;
                                }
                                // UPDATE ON GAME
                                p.init_plain(0x9E);

                                p.WriteUInt16(m_weather_lounge);
                                p.WriteByte(1);  // type ou indicação de GM

                                r.SendBroadCast(p);

                                // Send Message
                                p.init_plain(0x40); // Msg to Chat of player

                                p.WriteByte(7); // Notice

                                p.WriteString("@INI3");
                                p.WriteString("\\c0xff00ff00\\c GM Change to Weather [" + clean.ToLowerInvariant() + "]");

                                _session.Send(p);

                                return true;
                            }

                            if (cmd == ("@gift") || cmd == ("@presente"))
                            {
                                uint item_typeid = 0;
                                uint item_qntd = 0;
                                item_typeid = uint.Parse(_command.Dequeue());
                                item_qntd = uint.Parse(_command.Dequeue());

                                if (item_typeid == 0)
                                    throw new exception("[Channel::CommandByChat][Error] Normal [UID=" + (_session.UserInfo.UID) + "] tentou enviar presente para todos o Item[TYPEID=" + (item_typeid) + "QNTD = "
                                        + (item_qntd) + "], mas item is invalid. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 3, 0x5700100));

                                if (item_qntd > 20000u)
                                    throw new exception("[Channel::CommandByChat][Error] Normal [UID=" + (_session.UserInfo.UID) + "] tentou enviar presente para todos o Item[TYPEID=" + (item_typeid) + "QNTD = "
                                        + (item_qntd) + "], mas a quantidade passa de 20mil. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 4, 0x5700100));

                                var @base = sIff.Instance.findCommomItem(item_typeid);

                                if (@base == null)
                                    throw new exception("[Channel::CommandByChat][Error] Normal [UID=" + (_session.UserInfo.UID) + "] tentou enviar presente para todos o Item[TYPEID=" + (item_typeid) + "QNTD = "
                                        + (item_qntd) + "], mas o item nao existe no IFF_STRUCT do Server. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 6, 0));

                                stItem item = new stItem();
                                BuyItem bi = new BuyItem();

                                bi.id = -1;
                                bi._typeid = item_typeid;
                                bi.qntd = item_qntd;

                                var msg = ("GM Command Chat");

                                foreach (var el in Sessions)
                                {
                                    if (el.UserInfo.Lobby != 255)
                                    {
                                        // Limpa item
                                        item = new stItem();

                                        ItemManager.initItemFromBuyItem(el.UserInfo, item, bi, false, 0, 0, 1);

                                        if (item._typeid == 0)
                                            throw new exception("[Channel::CommandByChat][Error] Normal [UID=" + (_session.UserInfo.UID) + "] tentou enviar presente para todos o Item[TYPEID=" + (item_typeid) + "QNTD = "
                                                + (item_qntd) + "], mas nao conseguiu inicializar o item. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 5, 0));

                                        if (MailManager.SendMessageWithItem(0, el.UserInfo.UID, msg, item) <= 0)
                                            throw new exception("[Channel::CommandByChat][Error] Normal [UID=" + (_session.UserInfo.UID) + "] tentou enviar presente para o Normal [UID="
                                                + (el.UserInfo.UID) + "] o Item[TYPEID=" + (item_typeid) + ", QNTD="
                                                + (item_qntd) + "], mas nao conseguiu colocar o item no mail box dele. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 7, 0));
                                    }
                                }

                                // Send Message
                                p.init_plain(0x40); // Msg to Chat of player

                                p.WriteByte(7); // Notice

                                p.WriteString("@INI3");
                                p.WriteString("\\c0xff00ff00\\cGM Send Gift");

                                _session.Send(p);

                                return true;
                            }
                        }
                        else   //comandos sem esta na sala
                        {
                            if (cmd == ("@notice") || cmd == ("@noticia"))
                            {
                                string msg = "notice";
                                msg = string.Join(separator: " ", _command.ToArray());

                                NormalManagerDB.Instance.add(0, new CmdInsertNotice(msg, 1, 1), null, null);

                                // Send Message
                                p.init_plain(0x40); // Msg to Chat of player

                                p.WriteByte(7); // Notice

                                p.WriteString("@INI3");
                                p.WriteString("\\c0xff00ff00\\cSend Notice-Broadcast");

                                _session.Send(p);

                                return true;
                            }

                            if (cmd == ("@gift") || cmd == ("@presente"))
                            {
                                uint item_typeid = 0;
                                uint item_qntd = 0;
                                item_typeid = uint.Parse(_command.Dequeue());
                                item_qntd = uint.Parse(_command.Dequeue());

                                if (item_typeid == 0)
                                    throw new exception("[Channel::CommandByChat][Error] Normal [UID=" + (_session.UserInfo.UID) + "] tentou enviar presente para todos o Item[TYPEID=" + (item_typeid) + "QNTD = "
                                        + (item_qntd) + "], mas item is invalid. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 3, 0x5700100));

                                if (item_qntd > 20000u)
                                    throw new exception("[Channel::CommandByChat][Error] Normal [UID=" + (_session.UserInfo.UID) + "] tentou enviar presente para todos o Item[TYPEID=" + (item_typeid) + "QNTD = "
                                        + (item_qntd) + "], mas a quantidade passa de 20mil. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 4, 0x5700100));

                                var @base = sIff.Instance.findCommomItem(item_typeid);

                                if (@base == null)
                                    throw new exception("[Channel::CommandByChat][Error] Normal [UID=" + (_session.UserInfo.UID) + "] tentou enviar presente para todos o Item[TYPEID=" + (item_typeid) + "QNTD = "
                                        + (item_qntd) + "], mas o item nao existe no IFF_STRUCT do Server. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 6, 0));

                                stItem item = new stItem();
                                BuyItem bi = new BuyItem();

                                bi.id = -1;
                                bi._typeid = item_typeid;
                                bi.qntd = item_qntd;

                                var msg = ("GM Command Chat");

                                foreach (var el in Sessions)
                                {
                                    if (el.UserInfo.Lobby != 255)
                                    {
                                        // Limpa item
                                        item = new stItem();

                                        ItemManager.initItemFromBuyItem(el.UserInfo, item, bi, false, 0, 0, 1);

                                        if (item._typeid == 0)
                                            throw new exception("[Channel::CommandByChat][Error] Normal [UID=" + (_session.UserInfo.UID) + "] tentou enviar presente para todos o Item[TYPEID=" + (item_typeid) + "QNTD = "
                                                + (item_qntd) + "], mas nao conseguiu inicializar o item. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 5, 0));

                                        if (MailManager.SendMessageWithItem(0, el.UserInfo.UID, msg, item) <= 0)
                                            throw new exception("[Channel::CommandByChat][Error] Normal [UID=" + (_session.UserInfo.UID) + "] tentou enviar presente para o Normal [UID="
                                                + (el.UserInfo.UID) + "] o Item[TYPEID=" + (item_typeid) + ", QNTD="
                                                + (item_qntd) + "], mas nao conseguiu colocar o item no mail box dele. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 7, 0));
                                    }
                                }

                                // Send Message
                                p.init_plain(0x40); // Msg to Chat of player

                                p.WriteByte(7); // Notice

                                p.WriteString("@INI3");
                                p.WriteString("\\c0xff00ff00\\cGM Send Gift");

                                _session.Send(p);

                                return true;
                            }
                            if (cmd == ("@notice") || cmd == ("@noticia"))
                            {

                                return true;
                            }
                        }
                    }
                    else   //comandos [player Normal]
                    {
                        if (r != null)
                        {
                            if (cmd == ("@bot") && (r.GetTipo() != RoomTypeFlags.PRACTICE || r.GetTipo() != RoomTypeFlags.GRAND_ZODIAC_PRACTICE) && r.GetNumPlayers() == 1 && !r.GameRun())
                            {
                                if (!r.IsWithBot() && r.GetTipo() == RoomTypeFlags.STROKE || r.GetTipo() == RoomTypeFlags.TOURNEY || r.GetTipo() == RoomTypeFlags.SPECIAL_SHUFFLE_COURSE)
                                {

                                    try
                                    {
                                        r.SetSenha("by_luismk");
                                        r.SendUpdateRoom();
                                        r.MakeRoomBot(_session);
                                        if (r.IsWithBot())
                                        {
                                            // Send Message
                                            p.init_plain(0x40); // Msg to Chat of player

                                            p.WriteByte(7); // Notice

                                            p.WriteString("@INI3");
                                            p.WriteString("\\c0xff00ff00\\cCall bot by chat.");

                                            _session.Send(p);
                                        }
                                        return true;
                                    }
                                    catch (exception)
                                    {
                                        return false;
                                    }
                                }
                                return false;
                            }
                            if (cmd == ("@wind") && (r.GetTipo() == RoomTypeFlags.PRACTICE || r.GetTipo() == RoomTypeFlags.GRAND_ZODIAC_PRACTICE))
                            {
                                if (_command.Count < 2)
                                    return false; // não tem argumentos suficientes

                                if (!ushort.TryParse(_command.Dequeue(), out ushort wind) ||
                                    !ushort.TryParse(_command.Dequeue(), out ushort degree))
                                {
                                    // Mensagem de erro pro GM 
                                    _session.SendChatNotice("\\c0xffff0000\\c Invalid To Wind."); 
                                    return true;
                                }

                                // Atualiza vento
                                p.init_plain(0x5B);
                                p.WriteUInt16(wind);
                                p.WriteUInt16(degree);
                                p.WriteByte(1);
                                r.SendBroadCast(p);
                                _session.SendChatNotice("\\c0xff00ff00\\c Change Wind.");

                                return true;
                            }
                            if (cmd == ("@weather") && (r.GetTipo() == RoomTypeFlags.PRACTICE || r.GetTipo() == RoomTypeFlags.GRAND_ZODIAC_PRACTICE))
                            {
                                ushort m_weather_lounge = 0;

                                // Pega o próximo argumento
                                string input = _command.Dequeue()?.Trim() ?? "";

                                // Remove caracteres que não sejam letras ou números (opcional)
                                string clean = new string(input.Where(char.IsLetterOrDigit).ToArray());

                                // Tenta interpretar primeiro como palavra
                                switch (clean.ToLowerInvariant())
                                {
                                    case "default":
                                        m_weather_lounge = 1;
                                        break;
                                    case "night":
                                        m_weather_lounge = 1;
                                        break;
                                    case "rain":
                                        m_weather_lounge = 2;
                                        break;
                                    case "snow":
                                        m_weather_lounge = 3;
                                        break;
                                    default:
                                        // Se não for palavra conhecida, tenta número
                                        if (!ushort.TryParse(clean, out m_weather_lounge))
                                            m_weather_lounge = 0; // padrão
                                        break;
                                }
                                // UPDATE ON GAME
                                p.init_plain(0x9E);

                                p.WriteUInt16(m_weather_lounge);
                                p.WriteByte(1);  // type ou indicação de GM

                                r.SendBroadCast(p);

                                // Send Message 
                                _session.SendChatNotice("\\c0xff00ff00\\c GM Change to Weather [" + clean.ToLowerInvariant() + "]");
                                return true;
                            }
                        }
                    }
                }
                else
                {
                    return false;
                }
                return false;
            }

            catch (exception)
            {
                return false;
            }
        }

        public bool IsFull()
        {
            return Sessions.Count >= m_ci.max_user;
        }

        public void DestroyRoom(Room r)
        {
            m_rm.DestroyRoom(r);
        }
        #endregion
    }
}