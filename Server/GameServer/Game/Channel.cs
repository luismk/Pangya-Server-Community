using Pangya_GameServer.Game.Manager;
using Pangya_GameServer.Game.System;
using Pangya_GameServer.Models;
using Pangya_GameServer.Models.golden_time_type;
using Pangya_GameServer.PacketFunc;
using Pangya_GameServer.PangyaEnums;
using Pangya_GameServer.Repository;
using Pangya_GameServer.UTIL;
using PangyaAPI.IFF.JP.Extensions;
using PangyaAPI.IFF.JP.Models.Data;
using PangyaAPI.IFF.JP.Models.Flags;
using PangyaAPI.IFF.JP.Models.General;
using PangyaAPI.Network.Models;
using PangyaAPI.Network.PangyaPacket;
using PangyaAPI.Network.PangyaSession;
using PangyaAPI.Network.Repository;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using PangyaAPI.Utilities.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using static Pangya_GameServer.Models.ChangePlayerItemRoom;
using static Pangya_GameServer.Models.DefineConstants;
using static PangyaAPI.IFF.JP.Models.Data.GrandPrixData;
namespace Pangya_GameServer.Game
{
    public class Channel
    {
        protected enum ESTADO : byte
        {
            UNITIALIZED,
            INITIALIZED
        }

        public enum LEAVE_ROOM_STATE : int
        {
            DO_NOTHING = -1,        // Faz nada
            SEND_UPDATE_CLIENT = 0, // bug arm g++
            ROOM_DESTROYED,
        }

        protected ChannelInfo m_ci;
        RoomManager m_rm;
        public object m_cs = new object();
        protected uProperty m_type;           // Type GrandPrix, Natural, Normal

        protected int m_state;

        protected List<Player> v_sessions;
        protected Dictionary<Player, PlayerLobbyInfo> m_player_info;
        protected List<InviteChannelInfo> v_invite;
        private object m_cs_invite = new object();
        public PangyaSyncTimer m_time_convite;
        public Channel(ChannelInfo _ci, uProperty _type)
        {
            m_ci = _ci;
            m_rm = new RoomManager();
            m_type = (_type);
            m_state = (int)ESTADO.INITIALIZED;
            v_sessions = new List<Player>(_ci.max_user);
            m_player_info = new Dictionary<Player, PlayerLobbyInfo>(_ci.max_user);
            v_invite = new List<InviteChannelInfo>();
        }

        public void enterChannel(Player _session)
        {

            if (!_session.getState())
            {
                throw new exception("[Channel::enterChannel][Error] player nao esta conectado.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                    1, 1));
            }

            if (_session.m_pi.channel != DEFAULT_CHANNEL)
            {
                throw new exception("[Channel::enterChannel][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] ja esta conectado em outro canal.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                    2, 2));
            }

            addSession(_session);

            _smp.message_pool.getInstance().push(new message($"[Channel::EnterChannel][Sucess] CHANNEL[ID: {m_ci.id}, Users: {m_ci.curr_user}/{m_ci.max_user}, Rooms: {m_rm.getCount()}]", type_msg.CL_FILE_LOG_AND_CONSOLE));

            packet_func.session_send(packet_func.pacote095(0x102), // Não sei direito desse aqui mas passa antes de entrar no canal, talvez é o que faz o cliente pedir MSN server acho
                _session, 0);

            packet_func.session_send(packet_func.pacote04E(1),
                _session, 0);

            // Verifica se o tempo do ticket premium user acabou e manda a mensagem para o player, e exclui o ticket do player no SERVER, DB e GAME
            sPremiumSystem.getInstance().checkEndTimeTicket(_session);
        }


        public void leaveChannel(Player _session)
        {
            try
            {

                if (_session.m_pi.lobby != DEFAULT_CHANNEL)
                {
                    leaveLobby(_session); // Sai da Lobby
                }
                else // Sai da Sala Practice que não entra na lobby, [SINGLE PLAY]
                {
                    leaveRoom(_session, 0);
                }

                removeSession(_session);
                if (_session.m_pi.mi.sala_numero != ushort.MaxValue)
                    leaveRoom(_session, 0);

                _smp.message_pool.getInstance().push(new message($"[Channel::leaveChannel][Sucess] CHANNEL[ID: {m_ci.id}, Users: {m_ci.curr_user}/{m_ci.max_user}, Rooms: {m_rm.getCount()}]", type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
            catch (exception e)
            {
                removeSession(_session);

                _smp.message_pool.getInstance().push(new message("[Channel::leaveChannel][Error] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if (!ExceptionError.STDA_ERROR_CHECK_SOURCE_AND_ERROR_TYPE(e.getCodeError(), // Diferente do error do channel
                    STDA_ERROR_TYPE.CHANNEL, 1))
                {
                    throw;
                }
            }
        }


        public bool checkEnterChannel(Player _session)
        {
            // Não é GM verifica se o player pode entrar nesse canal
            if (!_session.m_pi.m_cap.game_master)
            {

                if (_session.m_pi.mi.level < m_ci.min_level_allow || _session.m_pi.mi.level > m_ci.max_level_allow)
                    return false;

                if (m_ci.type.only_rookie && _session.m_pi.mi.level > (short)enLEVEL.ROOKIE_A)
                    return false;

                if (m_ci.type.LowLevel && _session.m_pi.mi.level > (short)enLEVEL.JUNIOR_A)
                    return false;

                if (m_ci.type.HighLevel && _session.m_pi.mi.level < (short)enLEVEL.JUNIOR_E)
                    return false;

                if (m_ci.type.senior && (_session.m_pi.mi.level < (short)enLEVEL.JUNIOR_E || _session.m_pi.mi.level > (short)enLEVEL.SENIOR_A))
                    return false;

                if (m_ci.type.beginner && (_session.m_pi.mi.level < (short)enLEVEL.BEGINNER_E || _session.m_pi.mi.level > (short)enLEVEL.JUNIOR_A))
                    return false;

                return true;
            }
            return true;
        }

        public ChannelInfo getInfo()
        {
            return m_ci;
        }

        public byte getId()
        {
            return m_ci.id;
        }
        public PlayerLobbyInfo getPlayerInfo(Player _session)
        {

            if (_session == null)
            {
                throw new exception("[Channel::getPlayerInfo][Error] _session is null.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                    12, 1));
            }

            PlayerLobbyInfo pci = null;

            return m_player_info.Any(c => c.Key == _session) ? m_player_info.First(c => c.Key == _session).Value : pci;
        }

        public void startInviteTime()
        {
            if (m_time_convite != null && (m_time_convite.getState() == PangyaSyncTimer.TIMER_STATE.STOP || m_time_convite.getState() == PangyaSyncTimer.TIMER_STATE.FINISH))
                stopInviteTime();

            if (m_time_convite == null)//na primeira vez...
                m_time_convite = sgs.gs.getInstance().MakeTime(10 * 1000, () => checkInviteTime(), new List<long>(), PangyaSyncTimer.TIMER_TYPE.NORMAL);
        }

        public void stopInviteTime()
        {
            // Garantir que qualquer exception derrube o server
            try
            {

                if (m_time_convite != null)
                    sgs.gs.getInstance().unMakeTime(m_time_convite);
            }
            catch (exception e)
            {
                _smp.message_pool.getInstance().push(new message("[Channel::stopInviteTime][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            m_time_convite = null;
        }

        public void checkInviteTime()
        {
            // Protege o acesso à lista de convites
            lock (m_cs_invite)
            {
                if (v_invite.Count == 0)
                    return; // nada pra processar

                // Itera de trás pra frente para remover itens sem pular índices
                for (int i = v_invite.Count - 1; i >= 0; i--)
                {
                    var cii = v_invite[i];

                    try
                    {
                        // Se a função indicar que o convite expirou, remove da lista
                        if (send_time_out_invite(cii))
                        {
                            _smp.message_pool.getInstance().push(
                            new message($"[Channel::checkInviteTime][Warning] Remove UID={cii.invited_uid}",
                            type_msg.CL_FILE_LOG_AND_CONSOLE));

                            v_invite.RemoveAt(i);
                        }
                    }
                    catch (exception e)
                    {
                        _smp.message_pool.getInstance().push(
                            new message($"[Channel::checkInviteTime][ErrorSystem] Erro ao processar convite UID={cii.invited_uid}: {e.getFullMessageError()}",
                            type_msg.CL_FILE_LOG_AND_CONSOLE));
                        // Continua processando o resto da lista mesmo que dê erro
                    }
                }
            }
        }

        public bool isFull()
        {
            return m_ci.curr_user >= m_ci.max_user;
        }

        public void enterLobby(Player _session, byte _lobby)
        {

            try
            {
                if (!_session.getState())
                {
                    throw new exception("[Channel::enterLobby][Error] PLAYER [UID_TRASH=" + (_session.m_pi.uid) + "] nao esta conectado.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        1, 0));
                }

                if (_session.m_pi.lobby != DEFAULT_CHANNEL)
                {
                    throw new exception("[Channel::enterLobby][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] ja esta na lobby.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        5, 0));
                }

                _session.m_pi.lobby = (byte)((_lobby == DEFAULT_CHANNEL || _lobby == 0) ? 1 : _lobby);
                _session.m_pi.mi.sala_numero = ushort.MaxValue;//reseta por que pode esta bugado
                _session.m_pi.place = 0;

                updatePlayerInfo(_session);

                PangyaBinaryWriter p = new PangyaBinaryWriter();

                List<PlayerLobbyInfo> v_pci = new List<PlayerLobbyInfo>();
                PlayerLobbyInfo pci = null;

                List<RoomInfoEx> v_ri = m_rm.getRoomsInfo();

                List<Player> v_sessions = getSessions(_session.m_pi.lobby);

                for (var i = 0; i < v_sessions.Count; ++i)
                {
                    if ((pci = getPlayerInfo(v_sessions[i])) != null)
                    {
                        v_pci.Add(pci);
                    }
                }

                pci = getPlayerInfo(_session);
                if (v_pci.Count == 0)
                {
                    v_pci.Add(pci);
                }
                // Add o primeiro limpando a lobby
                packet_func.session_send(packet_func.pacote046(new List<PlayerLobbyInfo>() { v_pci[0] }, 4), _session, 0);

                if (v_pci.Count > 0)
                {
                    packet_func.session_send(packet_func.pacote046(v_pci, 5), _session, 0);
                }
                //precisa mandar pois pode causar bugs....
                packet_func.session_send(packet_func.pacote047(v_ri, 0), _session, 0);

                packet_func.channel_broadcast(this, packet_func.pacote046(new List<PlayerLobbyInfo>() { (pci == null) ? new PlayerLobbyInfo() : pci }, 1), 0);

                v_pci.Clear();
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public void leaveLobby(Player _session)
        {

            /// !@tem que tira isso aqui por que tem que enviar para os outros player da lobby que ele sai,
            /// mesmo que o sock dele não pode mais enviar
            //if (!_session.getState())
            //throw exception("[Channel::leaveLobby][Error] player nao esta conectado.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE::CHANNEL, 1, 0));

            // Sai da sala se estiver em uma sala
            try
            {
                leaveRoom(_session, 0);
            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::leaveLobby][Error] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            _session.m_pi.lobby = DEFAULT_CHANNEL;
            _session.m_pi.place = 0;

            updatePlayerInfo(_session);

            sendUpdatePlayerInfo(_session, 2);
        }

        public void enterLobbyMultiPlayer(Player _session)
        {

            try
            {

                // Enter Lobby
                enterLobby(_session, 1);

                packet_func.session_send(packet_func.pacote0F5(),
                                      _session, 0);
            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::enterLobbyMultiPlayer][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

        }

        public void leaveLobbyMultiPlayer(Player _session)
        {


            try
            {

                leaveLobby(_session);
                packet_func.session_send(packet_func.pacote0F6(),
                                      _session, 0);
            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::leaveLobbyMultiPlayer][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void enterLobbyGrandPrix(Player _session)
        {

            PangyaBinaryWriter p = new PangyaBinaryWriter();

            try
            {

                if (!sgs.gs.getInstance().getInfo().propriedade.grand_prix)
                {
                    throw new exception("[Channel::enterLobbyGrandPrix][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou entrar na lobby Grand Prix, mas ele esta desativo. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        1, 0x750001));
                }

                // Modo Grand Prix ainda não foi feito


                // Enter Lobby
                enterLobby(_session, 176);

                // Pacote Entra Lobby Grand Prix
                p.init_plain(0x250);

                p.WriteUInt32(0u); // OK

                // Count Type Grand Prix que está ativo
                // Tipo 0 é ativo por sem precisar desses valores
                p.WriteUInt32(sgs.gs.getInstance().getInfo().rate.countBitGrandPrixEvent());

                // Grand Prix Event: Types
                foreach (var el in sgs.gs.getInstance().getInfo().rate.getValueBitGrandPrixEvent())
                {
                    p.WriteUInt32(el);
                }

                // Count de	grand prix clear, (typeid, position)
                p.WriteUInt32((uint)_session.m_pi.v_gpc.Count);

                foreach (var el in _session.m_pi.v_gpc)
                {
                    p.Write(el._typeid);
                    p.Write(el.position);
                }

                // Avg. Score do player
                p.WriteFloat(_session.m_pi.ui.getMediaScore());

                packet_func.session_send(p,
                    _session, 1);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::enterLobbyGrandPrix][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x250);

                p.WriteUInt32((ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) == STDA_ERROR_TYPE.CHANNEL) ? ExceptionError.STDA_SYSTEM_ERROR_DECODE(e.getCodeError()) : 0x750000);

                packet_func.session_send(p,
                    _session, 1);
            }
        }

        public void leaveLobbyGrandPrix(Player _session)
        {

            try
            {

                leaveLobby(_session);

                if (_session.m_client != null)
                {
                    // Sai Lobby Grand Prix
                    PangyaBinaryWriter p = new PangyaBinaryWriter((ushort)0x251);

                    p.WriteUInt32(0u); // OK

                    packet_func.session_send(p,
                        _session, 0);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::leaveLobbyGrandPrix][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public List<stPlayerReward> getAllEligibleToGoldenTime()
        {
            List<stPlayerReward> players = new List<stPlayerReward>();


            // Channel verifica se o player está elegível a participar do Golden Time Event
            // Verifica se o player está em sala jogando ou no lounge, practice e Grand Prix Rookie não conta
            // [Lambda] get Room Info
            (bool isGaming, RoomInfoEx info) getRoomInfoLambda(Player _p)
            { 
                var r = m_rm.findRoom((short)_p.m_pi.mi.sala_numero);

                if (r != null)
                {
                    return (r.isGaming(), r.getInfo());
                }
                else
                {
                    _smp.message_pool.getInstance().push(new message(
                        $"[Channel::isGoldenTimeGood::lambda(getRoomInfo)][Error][WARNNING] PLAYER [UID={_p.m_pi.uid}] esta na sala[NUMERO={_p.m_pi.mi.sala_numero}], mas ela nao existe. Hacker ou Bug",
                        type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
                return (false, null);
            }
 
            foreach (var p in v_sessions)
            {
                // Invalid Player
                if (p?.m_pi == null) continue;

                // Não está no lobby (pode estar carregando ou trocando de canal)
                if (p.m_pi.lobby == 255) continue;

                // Não está em nenhuma sala
                if (p.m_pi.mi.sala_numero == ushort.MaxValue) continue;

                var (isPlaying, ri) = getRoomInfoLambda(p);

                // Não encontrou a sala ou RoomInfo inválido
                if (ri == null || ri.numero == ushort.MaxValue) continue;

                // 1. Filtro: Practice ou Grand Zodiac Practice não contam
                if (ri.getTipo() == RoomInfo.ROOM_INFO_TYPE.PRACTICE ||
                    ri.getTipo() == RoomInfo.ROOM_INFO_TYPE.GRAND_ZODIAC_PRACTICE)
                    continue;

                // 2. Filtro: Grand Prix Rookie (Tutorial) não conta
                if (ri.getTipo() == RoomInfo.ROOM_INFO_TYPE.GRAND_PRIX)
                {
                    var aba = sIff.getInstance().getGrandPrixAba(ri.grand_prix.dados_typeid);
                    bool isNormal = sIff.getInstance().isGrandPrixNormal(ri.grand_prix.dados_typeid);

                    if (aba == 0 && isNormal)
                        continue;
                }

                // 3. Regra do Lounge: Lounge conta sempre. Outros modos só se estiver "In-Game" (Playing)
                if (ri.getTipo() != RoomInfo.ROOM_INFO_TYPE.LOUNGE && !isPlaying)
                    continue;

                // Se passou em todos os filtros, adiciona à lista de recompensa
                players.Add(new stPlayerReward
                {
                    uid = p.m_pi.uid,
                    is_premium = true,
                    is_playing = isPlaying
                });
            } 
            return players;
        }

        public void sendFireWorksWinnerGoldenTime(List<stPlayerReward> _winners)
        {

            Player p = null;
            var pckt = new PangyaBinaryWriter();

            foreach (var el in _winners)
            {

                try
                {

                    if ((p = findSessionByUID((int)el.uid)) == null)
                    {
                        continue;
                    }

                    if (p.m_pi.mi.sala_numero == ushort.MaxValue || p.m_pi.lobby == 255)
                    {
                        continue;
                    }

                    var r = m_rm.findRoom((short)p.m_pi.mi.sala_numero);

                    if (r != null)
                    {

                        if (r.getInfo().getTipo() == RoomInfo.ROOM_INFO_TYPE.LOUNGE)
                        {

                            // send "chat" da sala fogos de artifícios em cima da cabela do player(*p)
                            packet_func.room_broadcast(r,
                                 packet_func.pacote04B(
                                p,
                                (byte)TYPE_CHANGE.TC_ITEM_EFFECT_LOUNGE,
                                0,
                                (int)stItemEffectLounge.TYPE_EFFECT.TE_TWILIGHT), 1);
                        }

                    }
                    else
                    {
                        _smp.message_pool.getInstance().push(new message("[Channel::sendFireWorksWinnerGoldenTime][Error][WARNNING] PLAYER [UID=" + (p.m_pi.uid) + "] esta na sala[NUMERO=" + (p.m_pi.mi.sala_numero) + "], mas ela nao existe. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                    }
                }
                catch (exception e)
                {

                    _smp.message_pool.getInstance().push(new message("[Channel::sendFireWorksWinnerGoldenTime][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
            }
        }

        public LEAVE_ROOM_STATE leaveRoom(Player _session, int _option)
        {
            LEAVE_ROOM_STATE state = LEAVE_ROOM_STATE.DO_NOTHING;

            // Simulação do BEGIN_FIND_ROOM (Busca da sala pelo número)
            var r = m_rm.findRoom((short)_session.m_pi.mi.sala_numero);

            if (r != null)
            {
                int opt = 0;

                try
                {
                    // Deleta Convidado
                    if (r.isInvited(_session))
                    {
                        var ici = r.deleteInvited(_session);
                        deleteInviteTimeRequest(ici);
                    }
                    else
                    {
                        opt = r.leave(_session, _option);
                    }

                    // Verifica se todos players da sala são convite
                    var all_invite = r.getAllInvite();

                    if (r.getNumPlayers() == all_invite.Count)
                    {
                        Player s = null;
                        InviteChannelInfo ici = new InviteChannelInfo();

                        // Usamos ToList para poder iterar enquanto a coleção original é modificada
                        var all_invite_copy = all_invite.ToList();

                        foreach (var invite_item in all_invite_copy)
                        {
                            s = null;
                            ici = new InviteChannelInfo();

                            s = sgs.gs.getInstance().findPlayer(invite_item.invited_uid);

                            if (s == null)
                            {
                                // Player não está online no server
                                ici = r.deleteInvited(invite_item.invited_uid);
                            }
                            else
                            {
                                // Player está online
                                ici = r.deleteInvited(s);
                            }

                            // Deleta invite
                            if (ici.room_number >= 0 && ici.invited_uid > 0 && ici.invite_uid > 0)
                                deleteInviteTimeRequest(ici);
                        }
                    }
                }
                catch (exception e)
                {
                    _smp.message_pool.getInstance().push(new message("[channel::leaveRoom][Error] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                }

                // Att PlayerCanalInfo
                updatePlayerInfo(_session);

                if (r.getNumPlayers() > 0 || opt == 0 /*Não exclui a sala*/)
                {
                    r.sendUpdate();

                    try
                    {
                        r.sendPlayerInfo(_session, 2);
                    }
                    catch (Exception e)
                    {
                        _smp.message_pool.getInstance().push(new message("[channel::leaveRoom][Error] " + e.Message, type_msg.CL_FILE_LOG_AND_CONSOLE));
                    }

                    sendUpdatePlayerInfo(_session, 3);
                    sendUpdateRoomInfo(r.getInfo(), 3);

                    try
                    {
                        // Deleta Todos da sala (Expulsão em massa)
                        if (opt == 0x801 && r.getNumPlayers() > 0)
                        {
                            while (r.getNumPlayers() > 0)
                            {
                                var first_session = r.getSessions().FirstOrDefault();
                                if (first_session == null || leaveRoom(first_session, 0x800) == LEAVE_ROOM_STATE.ROOM_DESTROYED)
                                    break; // Deletou a sala
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        _smp.message_pool.getInstance().push(new message("[channel::leaveRoom][ErrorSystem] " + e.Message, type_msg.CL_FILE_LOG_AND_CONSOLE));
                    }
                }
                else
                {
                    // Criamos uma cópia das informações antes de destruir
                    RoomInfoEx ri = r.getInfo();

                    // Destruíndo a sala
                    r.setDestroying();
                    m_rm.destroyRoom(r);

                    sendUpdatePlayerInfo(_session, 3);
                    sendUpdateRoomInfo(ri, 2);

                    state = LEAVE_ROOM_STATE.ROOM_DESTROYED;
                }

                // Send Packet Leave Room To client if necessary
                if (state < LEAVE_ROOM_STATE.ROOM_DESTROYED)
                    state = LEAVE_ROOM_STATE.SEND_UPDATE_CLIENT;
            }
            else if (_option == 1)
            {
                _smp.message_pool.getInstance().push(new message("[channel::leaveRoom][Error][WARNNING] player[UID=" + _session.m_pi.uid + "] tentou sair da sala[NUMERO="
                    + _session.m_pi.mi.sala_numero + "], mas ela nao existe. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            return state;
        }
        public LEAVE_ROOM_STATE leaveRoomMultiPlayer(Player _session, int _option)
        {

            var state = leaveRoom(_session, _option);

            if (state > LEAVE_ROOM_STATE.DO_NOTHING)
            {
                packet_func.session_send(packet_func.pacote04C(-1),
                    _session, 0);
            }

            return state;
        }
        public LEAVE_ROOM_STATE leaveRoomGrandPrix(Player _session, int _option)
        {

            var state = leaveRoom(_session, _option);

            if (state > LEAVE_ROOM_STATE.DO_NOTHING && true)
            {

                PangyaBinaryWriter p = new PangyaBinaryWriter((ushort)0x254);

                p.WriteUInt32(0u); // OK

                p.WriteInt16(-1); // Flag

                packet_func.session_send(p,
                    _session, 1);
            }

            return state;
        }
        public LEAVE_ROOM_STATE kickPlayerRoom(Player _session, byte force)
        {

            var state = leaveRoom(_session, (force == 1u) ? 3 : 0x800);

            if (state > LEAVE_ROOM_STATE.DO_NOTHING && true)
            {

                PangyaBinaryWriter p = new PangyaBinaryWriter((ushort)0x7E);

                p.WriteUInt32(0x800);

                packet_func.session_send(p,
                    _session, 1);
            }

            return state;
        }

        public List<Player> getSessions(int _lobby = 255)//default
        {
            List<Player> v_session = new List<Player>();
            //Monitor.Exit(m_cs);
            for (var i = 0; i < v_sessions.Count(); ++i)
            {
                if (v_sessions[i] != null && v_sessions[i].m_pi.channel != DEFAULT_CHANNEL
                    && (_lobby == DEFAULT_CHANNEL || v_sessions[i].m_pi.lobby != DEFAULT_CHANNEL))
                    v_session.Add(v_sessions[i]);

            }
            //Monitor.Exit(m_cs);
            return v_session;
        }

        public void makeGrandZodiacEventRoom(range_time _rt)
        {
            try
            {
                const string NAME_INT = "HIO Event (Intermediate)";
                const string NAME_ADV = "HIO Event (Advanced)";

                // 1. Cálculo de salas (Garantindo no mínimo 1 de cada tipo se o evento estiver ativo)
                int num_rooms = Math.Max(1, (int)Math.Ceiling(v_sessions.Count / 200.0));

                // 2. Limpeza de Salas Inválidas (Anti-Zumbi)
                // Antes de contar, limpamos salas que estão marcadas como destruídas mas ainda no Manager
                lock (m_rm)
                {
                    var allRooms = m_rm.getAllRoomsGrandZodiacEvent();
                    foreach (var r in allRooms)
                    {
                        // Se for uma sala de evento vazia ou marcada para destruição, removemos do Manager
                        if ((r is RoomGrandZodiacEvent) && (r.getNumPlayers() == 0 && r.geDestroying()))
                        {
                            m_rm.addRoom(r);
                        }
                    }
                }

                // Recuperamos a lista atualizada após a limpeza
                var currentRooms = m_rm.getAllRoomsGrandZodiacEvent();

                // 3. Função local de criação robusta
                void CreateNeededRooms(RoomInfo.ROOM_INFO_TYPE tipo, string nome, int goal)
                {
                    // Filtra apenas salas do tipo específico que ainda estão "vivas"
                    int currentCount = currentRooms.Count(el => el != null && el.getInfo().getTipo() == tipo && !el.geDestroying());

                    // LOG de Debug no Console para você monitorar o ciclo
                    _smp.message_pool.getInstance().push(new message(
                        $"[Zodiac Check] Tipo: {tipo} | Atuais: {currentCount} | Meta: {goal}",
                        type_msg.CL_ONLY_CONSOLE));

                    for (int i = currentCount; i < goal; i++)
                    {
                        try
                        {
                            // Criamos um RoomInfoEx limpo e específico
                            RoomInfoEx ri = new RoomInfoEx();
                            ri.time_30s = 7 * 60000; // 7 minutos de espera (exemplo)
                            ri.tipo = (byte)tipo;
                            ri.qntd_hole = 1;
                            ri.course = RoomInfo.ROOM_INFO_COURSE.GRAND_ZODIAC;
                            ri.max_player = 100;
                            ri.channel_rookie = true;
                            ri.name = nome;
                            ri.senha = ""; // Garante que a sala seja pública 

                            // m_rm.makeRoomGrandZodiacEvent deve retornar uma instância NOVA de RoomGrandZodiacEvent
                            var r = m_rm.makeRoomGrandZodiacEvent(m_ci.id, ri, _rt.m_end);

                            if (r != null)
                            {
                                // Adiciona ao Manager e avisa os players no Lobby (Pacote 0x48 / 0x49)
                                if (m_rm.addRoom(r))
                                {
                                    sendUpdateRoomInfo(r.getInfo(), 1); // 1 = Add/Update

                                    _smp.message_pool.getInstance().push(new message(
                                        $"[Zodiac] Sala {tipo} #{r.getNumero()} criada com sucesso no Canal {m_ci.id}.",
                                        type_msg.CL_FILE_LOG_AND_CONSOLE));
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _smp.message_pool.getInstance().push(new message(
                                $"[makeGrandZodiacEventRoom][CreateLoopError] {ex.Message}",
                                type_msg.CL_FILE_LOG_AND_CONSOLE));
                        }
                    }
                }

                // 4. Lógica de Disparo (Baseada no range_time)
                if (_rt.m_type == range_time.eTYPE_MAKE_ROOM.TMR_MAKE_ALL || _rt.m_type == range_time.eTYPE_MAKE_ROOM.TMR_MAKE_INTERMEDIARE)
                {
                    CreateNeededRooms(RoomInfo.ROOM_INFO_TYPE.GRAND_ZODIAC_INT, NAME_INT, num_rooms);
                }

                if (_rt.m_type == range_time.eTYPE_MAKE_ROOM.TMR_MAKE_ALL || _rt.m_type == range_time.eTYPE_MAKE_ROOM.TMR_MAKE_ADVANCED)
                {
                    CreateNeededRooms(RoomInfo.ROOM_INFO_TYPE.GRAND_ZODIAC_ADV, NAME_ADV, num_rooms);
                }
            }
            catch (Exception e)
            {
                _smp.message_pool.getInstance().push(new message(
                    $"[GameServer::makeGrandZodiacEventRoom][CriticalError] {e.Message}",
                    type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void makeBotGMEventRoom(stRangeTime _rt, List<stReward> _reward)
        {

            try
            {
                // Verifica se tem room grand Bot GM Event criado se não cria
                RoomInfoEx ri = new RoomInfoEx();
                RoomBotGMEvent r = null;

                var rooms = m_rm.getAllRoomsBotGMEvent();

                if (rooms.empty())
                {

                    ri.clear();

                    // Flag do canal, se for rookie passa para sala, que no jogo, essa type faz vir vento de 1m a 5m
                    ri.channel_rookie = true;

                    ri.time_30s = 35 * 60000; // 35 min
                    ri.tipo = (byte)RoomInfo.ROOM_INFO_TYPE.TOURNEY;
                    ri.qntd_hole = 18; // 18 Holes
                    ri.course = RoomInfo.ROOM_INFO_COURSE.RANDOM;
                    ri.max_player = 250;
                    ri.modo = (byte)RoomInfo.ROOM_INFO_MODO.M_SHUFFLE;
                    ri.name = BOT_GM_EVENT_NAME;

                    try
                    {

                        r = m_rm.makeRoomBotGMEvent(m_ci.id,
                            ri, _reward);

                        if (r == null)
                        {
                            throw new exception("[Channel::makeBotGMEventRoom][Error] Channel[ID=" + ((ushort)m_ci.id) + "] tentou criar a sala Bot GM Event, mas deu erro na criacao. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                8, 0));
                        }

                        _rt.m_room_created = true;//seta aqui agora

                        sendUpdateRoomInfo(r.getInfo(), 1);


                        // Libera a sala
                        if (r != null)
                        {
                            m_rm.addRoom(r);

                            m_rm.unlockRoom(r);
                        }


                    }
                    catch (exception e)
                    {

                        // Libera a sala
                        if (r != null)
                        {
                            m_rm.unlockRoom(r);
                        }

                        _smp.message_pool.getInstance().push(new message("[Channel::makeBotGMEventRoom][ErrorSystem][make] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                    }

                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::makeBotGMEventRoom][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestEnterLobby(Player _session, packet _packet)
        {
            //

            try
            {
                enterLobbyMultiPlayer(_session);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestEnterLobby][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestExitLobby(Player _session, packet _packet)
        {
            //

            try
            {
                leaveLobbyMultiPlayer(_session);
            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestExitLobby][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestEnterLobbyGrandPrix(Player _session, packet _packet)
        {
            try
            {
                enterLobbyGrandPrix(_session);
            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestEnterLobbyGrandPrix][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestExitLobbyGrandPrix(Player _session, packet _packet)
        {
            //

            try
            {





                leaveLobbyGrandPrix(_session);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestExitLobbyGrandPrix][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestEnterSpyRoom(Player _session, packet _packet)
        {
            try
            {
                var sala_numero = _packet.ReadUInt16();
                string senha = _packet.ReadString();

                var r = m_rm.findRoom((short)sala_numero);

                if (r != null && r.checkPass(senha))
                {
                    requestEnterRoom(_session, _packet);
                }
                else
                {
                    throw new exception("[Channel::requestEnterSpyRoom][Error] Channel[ID=" + ((ushort)m_ci.id) + "] enviou senha errada.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        7, 0));
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestEnterSpyRoom][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }


        public void requestCheckNick(Player _session, packet _packet)
        {
            NICK_CHECK nc = NICK_CHECK.SUCCESS;
            string nick = string.Empty;

            byte opt = 0;
            byte error = 2;

            MemberInfoEx mi = null;

            try
            {
                opt = _packet.ReadUInt8();

                if (opt != 0)
                {
                    _smp.message_pool.getInstance().push(new message(
                        $"[Channel::requestCheckNick][WARNING] Player[UID={_session.m_pi.uid}] Pediu para Check Nickname: {nick}, [OPT={opt}] diferente de 0.",
                        type_msg.CL_FILE_LOG_AND_CONSOLE));
                }

                nick = _packet.ReadPStr();

                _smp.message_pool.getInstance().push(new message($"[Channel::requestCheckNick][Log] Player[UID={_session.m_pi.uid}, IGN_CHECK={nick}]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                if (nc == NICK_CHECK.SUCCESS && Regex.IsMatch(nick, @".*[ ].*"))
                {
                    nc = NICK_CHECK.EMPETY_ERROR;

                    _smp.message_pool.getInstance().push(new message(
                        $"[Channel::requestCheckNick][Log] Player[UID={_session.m_pi.uid}] Pediu para verificar o nick contem espaco em branco: {nick}",
                        type_msg.CL_FILE_LOG_AND_CONSOLE));
                }

                if ((nc == NICK_CHECK.SUCCESS && nick.Length < 4) ||
                    Regex.IsMatch(nick, @".*[\^$&,\\?`´~\|""@#¨'%*!\\].*"))
                {
                    nc = NICK_CHECK.INCORRECT_NICK;

                    _smp.message_pool.getInstance().push(new message(
                        $"[Channel::requestCheckNick][Log] Player[UID={_session.m_pi.uid}] Pediu para verificar o nick é menor que 4 letras ou tem caracteres que nao pode: {nick}",
                        type_msg.CL_FILE_LOG_AND_CONSOLE));
                }

                if (nc == NICK_CHECK.SUCCESS)
                {
                    var cmd_vn = new CmdVerifyNick(nick); // Waiter
                    snmdb.NormalManagerDB.getInstance().add(0, cmd_vn, null, null);

                    if (cmd_vn.getException().getCodeError() != 0)
                        throw cmd_vn.getException();

                    if (cmd_vn.getLastCheck())
                    {
                        nc = NICK_CHECK.NICK_IN_USE;

                        error = (nc == NICK_CHECK.NICK_IN_USE && cmd_vn.getUID() != 0 ? (byte)0 : (byte)2);

                        var cmd_mi = new CmdMemberInfo(cmd_vn.getUID()); // Waiter
                        snmdb.NormalManagerDB.getInstance().add(0, cmd_mi, null, null);

                        if (cmd_mi.getException().getCodeError() != 0)
                            throw cmd_mi.getException();

                        mi = cmd_mi.getInfo();

                        _smp.message_pool.getInstance().push(new message(
                            $"[Channel::requestCheckNick][Log] Player[UID={_session.m_pi.uid}] Pediu para verificar o nick ja esta em uso: {nick}",
                            type_msg.CL_FILE_LOG_AND_CONSOLE));
                    }
                }
            }
            catch (exception e) // sua exception customizada
            {
                _smp.message_pool.getInstance().push(new message(
                    $"[Channel::requestCheckNick][ErrorSystem] {e.getFullMessageError()}",
                    type_msg.CL_FILE_LOG_AND_CONSOLE));

                if (ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) == STDA_ERROR_TYPE.PANGYA_DB)
                    nc = NICK_CHECK.ERROR_DB;
                else
                    nc = NICK_CHECK.UNKNOWN_ERROR;
            }
            catch (Exception e)
            {
                _smp.message_pool.getInstance().push(new message(
                    $"[Channel::requestCheckNick][ErrorSystem] {e.Message}",
                    type_msg.CL_FILE_LOG_AND_CONSOLE));

                nc = NICK_CHECK.UNKNOWN_ERROR;
            }

            try
            {
                PangyaBinaryWriter p = new PangyaBinaryWriter(0xA1);

                p.WriteByte(error);

                if (error == 0 && nc == NICK_CHECK.NICK_IN_USE)
                {
                    p.WriteUInt32(mi.uid);
                    p.WriteBytes(mi.ToArray());
                }

                packet_func.session_send(p, _session, 1);
            }
            catch (exception e)
            {
                _smp.message_pool.getInstance().push(new message(
                    $"[Channel::requestCheckNick][ErrorSystem] {e.getFullMessageError()}",
                    type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestMakeRoom(Player _session, packet _packet)
        {
            PangyaBinaryWriter p = new PangyaBinaryWriter();

            try
            {
                // 1. Validação de tamanho mínimo do pacote (Prevenção de Buffer Overflow/Crash)
                if (_packet.GetSize < 20)
                {
                    throw new exception($"[Channel::requestMakeRoom] Packet size ({_packet.GetSize}) too small. Hacker attempt.",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 7, 0));
                }

                int option;
                RoomInfoEx ri = new RoomInfoEx();
                string s_tmp = "";

                option = _packet.ReadUInt8();

                ri.time_vs = _packet.ReadUInt32();
                ri.time_30s = _packet.ReadUInt32();
                ri.max_player = _packet.ReadUInt8();
                ri.tipo = _packet.ReadUInt8();
                ri.qntd_hole = _packet.ReadUInt8();
                ri.course = (RoomInfo.ROOM_INFO_COURSE)(_packet.ReadUInt8());
                // 3. Verificação de Course Válido
                if (!Enum.IsDefined(typeof(RoomInfo.ROOM_INFO_COURSE), ri.course))
                {
                    throw new exception($"[Channel::requestMakeRoom] Course ID {(int)ri.course} inválido.",
                       ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 7, 0));
                }

                ri.modo = _packet.ReadUInt8();
                if (!Enum.IsDefined(typeof(RoomInfo.ROOM_INFO_MODO), ri.modo))
                {
                    throw new exception($"[Channel::requestMakeRoom] Modo ID {(int)ri.modo} inválido.",
                       ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 7, 0));
                }

                var len = _packet.GetSize;

                bool practice = false;
                //hole repeted = 68, chip-in = 63
                if (len == 68 && ri.tipo == 19) // Hole Repeated
                {
                    _packet.Skip(5);
                    ri.hole_repeat = 1;
                    ri.fixed_hole = 7;
                    practice = true;
                }
                else if (len == 63 && ri.tipo == 19) // Chip-in Practice
                {
                    ri.hole_repeat = 0;
                    ri.fixed_hole = 0;
                    practice = true;
                }

                if (!_session.m_pi.m_cap.game_master && ri.max_player > 30)//criar comparacao melhor
                {
                    throw new exception("[Channel::requestMakeRoom][Error] Channel[ID=" + ((ushort)m_ci.id) + "] limite atingido, Hacker, por que o cliente nao deixa criar uma sala maior que 30, pois o cliente nao e gm/adm.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        7, 0));
                }

                ri.special_flag_mod.ulNaturalAndShortGame = _packet.ReadUInt32();

                // CHECK DE SEGURANÇA: 
                // Natural (Bit 0) + Short Game (Bit 1) = Valor máximo 3
                if (ri.special_flag_mod.ulNaturalAndShortGame > 3)
                {
                    throw new exception("[Channel::requestMakeRoom][Error] Channel[ID=" + ((ushort)m_ci.id) + "] tentou criar sala com NaturalAndShortGame inválido.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                      7, 0));
                }
                s_tmp = _packet.ReadString();

                if (s_tmp.Length == 0)//criar comparacao melhor
                {
                    throw new exception("[Channel::requestMakeRoom][Error] Channel[ID=" + ((ushort)m_ci.id) + "] Nome da sala vazio, Hacker, por que o cliente nao deixa enviar esse pacote sem um nome da sala.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        7, 0));
                }

                if (s_tmp.Length > 32)//criar comparacao melhor
                {
                    throw new exception("[Channel::requestMakeRoom][Error] Channel[ID=" + ((ushort)m_ci.id) + "] Nome da sala vazio, Hacker, por que o cliente nao deixa enviar esse pacote sem um nome da sala.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        7, 0));
                }


                if (practice)
                {
                    s_tmp = "Single Player Practice Mode";
                    if (ri.max_player > 1)
                    {
                        throw new exception("[Channel::requestMakeRoom][Error] Channel[ID=" + (m_ci.id)
                            + "] Numero de jogadores errado, Hacker, por que o cliente nao deixa enviar esse pacote assim.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 7, 7));
                    }
                }

                ri.name = s_tmp;
                s_tmp = _packet.ReadString();

                if (s_tmp.Length > 8)//criar comparacao melhor
                {
                    throw new exception("[Channel::requestMakeRoom][Error] Channel[ID=" + ((ushort)m_ci.id) + "] tamanho da senha esta errado, Code[0].", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        7, 0));
                }

                if (practice)
                {
                    if (s_tmp.empty()) //senha vazia
                    {
                        throw new exception("[Channel::requestMakeRoom][Error] Channel[ID=" + (m_ci.id)
                            + "] senha da sala practice esta errada!.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 7, 0));
                    }

                    if (s_tmp.Length < 8)//criar comparacao melhor
                    {
                        throw new exception("[Channel::requestMakeRoom][Error] Channel[ID=" + ((ushort)m_ci.id) + "] tamanho da senha esta errado, Code[2].", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                            7, 0));
                    }
                }


                if (!s_tmp.empty())
                {
                    ri.senha_flag = 0;
                    ri.senha = s_tmp;
                }

                ri.typeid_artefatic = _packet.ReadUInt32();
                //::::::::::::::::::::::: Termina Leitura de dados do cliente ::::::::::::::::::::::::::::::://

                //Check Max Players, 30s, vs 
                FilterRoom(_session, ri);

                // Short game só pode em torneio, Special shuffle course e Grand Prix se estiver com o short game ativado, desativa
                if (ri.special_flag_mod.short_game && ri.getTipo() != RoomInfo.ROOM_INFO_TYPE.TOURNEY
                    && ri.getTipo() != RoomInfo.ROOM_INFO_TYPE.SPECIAL_SHUFFLE_COURSE
                    && ri.getTipo() != RoomInfo.ROOM_INFO_TYPE.GRAND_PRIX)
                {
                    ri.special_flag_mod.short_game = false;
                }

                // Se for natural Modo ativa o Modo natural na sala, para mostrar os detalhes na rosa dos ventos,
                // por que o cliente muda o vento, mas não mostra os detalhes
                if (m_type.natural)
                {
                    ri.special_flag_mod.natural = true;
                }

                // Flag Server
                uFlag flag = _session.m_pi.block_flag.m_flag;

                // Player não pode criar sala, exceto Lounge, se ele não estiver bloqueado
                if (flag.all_game && (ri.getTipo() != RoomInfo.ROOM_INFO_TYPE.LOUNGE || flag.lounge))
                {
                    throw new exception("[Channel::requestMakeRoom][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou criar um sala, mas ele nao pode criar nenhuma sala. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        1, 0x780001));
                }

                switch (ri.getTipo())
                {
                    case RoomInfo.ROOM_INFO_TYPE.STROKE:
                        if (flag.stroke)
                        {
                            throw new exception("[Channel::requestMakeRoom][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou criar sala[TIPO=" + ((ushort)ri.getTipo()) + "], mas ele nao pode criar Stroke. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                2, 0x770001));
                        }
                        break;
                    case RoomInfo.ROOM_INFO_TYPE.MATCH:
                        if (flag.match)
                        {
                            throw new exception("[Channel::requestMakeRoom][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou criar sala[TIPO=" + ((ushort)ri.getTipo()) + "], mas ele nao pode criar Match. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                3, 0x770001));
                        }
                        break;
                    case RoomInfo.ROOM_INFO_TYPE.TOURNEY:
                        if (flag.tourney)
                        {
                            throw new exception("[Channel::requestMakeRoom][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou criar sala[TIPO=" + ((ushort)ri.getTipo()) + "], mas ele nao pode criar Tourney. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                4, 0x770001));
                        }
                        break;
                    case RoomInfo.ROOM_INFO_TYPE.TOURNEY_TEAM:
                        if (flag.team_tourney)
                        {
                            throw new exception("[Channel::requestMakeRoom][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou criar sala[TIPO=" + ((ushort)ri.getTipo()) + "], mas ele nao pode criar Team Tourney. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                5, 0x770001));
                        }
                        break;
                    case RoomInfo.ROOM_INFO_TYPE.GUILD_BATTLE:
                        if (flag.guild_battle)
                        {
                            throw new exception("[Channel::requestMakeRoom][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou criar sala[TIPO=" + ((ushort)ri.getTipo()) + "], mas ele nao pode criar Guild Battle. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                6, 0x770001));
                        }
                        break;
                    case RoomInfo.ROOM_INFO_TYPE.PANG_BATTLE:
                        if (flag.pang_battle)
                        {
                            throw new exception("[Channel::requestMakeRoom][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou criar sala[TIPO=" + ((ushort)ri.getTipo()) + "], mas ele nao pode criar Pang Battle. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                7, 0x770001));
                        }
                        break;
                    case RoomInfo.ROOM_INFO_TYPE.APPROCH:
                        if (flag.approach)
                        {
                            throw new exception("[Channel::requestMakeRoom][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou criar sala[TIPO=" + ((ushort)ri.getTipo()) + "], mas ele nao pode criar Approach. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                8, 0x770001));
                        }
                        break;
                    case RoomInfo.ROOM_INFO_TYPE.LOUNGE:
                        if (flag.lounge)
                        {
                            throw new exception("[Channel::requestMakeRoom][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou criar sala[TIPO=" + ((ushort)ri.getTipo()) + "], mas ele nao pode criar Lounge. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                9, 0x770001));
                        }
                        break;
                    case RoomInfo.ROOM_INFO_TYPE.GRAND_ZODIAC_INT:
                    case RoomInfo.ROOM_INFO_TYPE.GRAND_ZODIAC_ADV:
                    case RoomInfo.ROOM_INFO_TYPE.GRAND_ZODIAC_PRACTICE:
                        if (flag.grand_zodiac)
                        {
                            throw new exception("[Channel::requestMakeRoom][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou criar sala[TIPO=" + ((ushort)ri.getTipo()) + "], mas ele nao pode criar Grand Zodiac. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                10, 0x770001));
                        }
                        break;
                    case RoomInfo.ROOM_INFO_TYPE.GRAND_PRIX:
                        if (flag.grand_prix)
                        {
                            throw new exception("[Channel::requestMakeRoom][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou criar sala[TIPO=" + ((ushort)ri.getTipo()) + "], mas ele nao pode criar Grand Prix. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                11, 0x770001));
                        }
                        break;
                    case RoomInfo.ROOM_INFO_TYPE.SPECIAL_SHUFFLE_COURSE:
                        if (flag.ssc)
                        {
                            throw new exception("[Channel::requestMakeRoom][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou criar sala[TIPO=" + ((ushort)ri.getTipo()) + "], mas ele nao pode criar Special Shuffle Course. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                12, 0x770001));
                        }
                        break;
                    case RoomInfo.ROOM_INFO_TYPE.PRACTICE:
                        if (flag.single_play)
                        {
                            throw new exception("[Channel::requestMakeRoom][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou criar sala[TIPO=" + ((ushort)ri.getTipo()) + "], mas ele nao pode criar Practice. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                13, 0x770001));
                        }
                        break;
                }

                if (ri.special_flag_mod.short_game && (flag.team_tourney || flag.short_game))
                {
                    throw new exception("[Channel::requestMakeRoom][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou criar a sala[TIPO=" + ((ushort)ri.getTipo()) + "], mas ele nao pode criar sala Short Game. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        1, 770001));
                }

                if (ri.getTipo() == RoomInfo.ROOM_INFO_TYPE.GRAND_ZODIAC_PRACTICE && ri.time_30s != (30 * 60000))
                {
                    throw new exception("[Channel::requestMakeRoom][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou criar a sala[TIPO=" + ((ushort)ri.getTipo()) + "], mas o tempo é diferente do tempo do Chip-in Practice[CERTO=" + (30 * 60000) + ", HACKER=" + (ri.time_30s) + "]. Hacker.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        1, 780002));
                }

                if ((ri.getTipo() >= RoomInfo.ROOM_INFO_TYPE.GRAND_ZODIAC_INT && ri.getTipo() <= RoomInfo.ROOM_INFO_TYPE.GRAND_ZODIAC_ADV) && !_session.m_pi.m_cap.game_master)
                {
                    throw new exception("[Channel::requestMakeRoom][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou criar a sala[TIPO=" + ((uint)ri.getTipo()) + "], mas ele nao é GM para poder criar sala de Grand Zodiac Event. Hacker.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        2, 760001));
                }

                if (ri.getTipo() == RoomInfo.ROOM_INFO_TYPE.GRAND_PRIX)
                {
                    throw new exception("[Channel::requestMakeRoom][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou criar a sala[TIPO=" + ((ushort)ri.getTipo()) + "], mas nao pode criar sala Grand Prix com esse pacote. Hacker.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        15, 0x770001));
                }

                // Flag do canal, se for rookie passa para sala, que no jogo, essa type faz vir vento de 1m a 5m
                ri.channel_rookie = true;

                if (ri.getTipo() == RoomInfo.ROOM_INFO_TYPE.SPECIAL_SHUFFLE_COURSE)
                {

                    var pWi = _session.m_pi.findWarehouseItemByTypeid(SPECIAL_SHUFFLE_COURSE_TICKET_TYPEID);

                    if (pWi == null)
                    {
                        throw new exception("[Channel::requestMakeRoom][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou criar a sala Special Shuffle Course, mas ele nao tem o Ticket[TYPEID=" + (SPECIAL_SHUFFLE_COURSE_TICKET_TYPEID) + "]. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                            9, 0));
                    }

                    if (pWi.STDA_C_ITEM_QNTD < 1)
                    {
                        throw new exception("[Channel::requestMakeRoom][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou criar a sala Special Shuffle Course, mas ele nao tem quantidade suficiente do Ticket[TYPEID=" + (SPECIAL_SHUFFLE_COURSE_TICKET_TYPEID) + ", QNTD=" + (pWi.STDA_C_ITEM_QNTD) + ", QNTD_REQ=1]. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                            10, 0));
                    }

                    stItem item = new stItem();

                    item.type = 2;
                    item.id = (int)pWi.id;
                    item._typeid = pWi._typeid;
                    item.qntd = 1;
                    item.STDA_C_ITEM_QNTD = (short)(item.qntd * -1);

                    // UPDATE ON SERVER AND DB
                    if (ItemManager.removeItem(item, _session) <= 0)
                    {
                        throw new exception("[Channel::requestMakeRoom][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou criar a sala Special Shuffle Course, mas nao conseguiu deletar o Ticket[TYPEID=" + (item._typeid) + ", ID=" + (item.id) + "]. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                            11, 0));
                    }

                    // UPDATE ON GAME
                    // O Proprio Cliente já tira 1 SSC Ticket, então só precisa atualizar no SERVER e NO DB

                    // Diminui o tempo do SSC se for Short Game
                    // Se for short game, coloca para o tempo ser de 20 minutos
                    if (ri.special_flag_mod.short_game)
                    {
                        ri.time_30s = 20 * 60000;
                    }
                }

                Room r = null;

                try
                {

                    // Verifica se o player foi convidado em outra sala
                    // e tira o convite dele
                    deleteInviteTimeResquestByInvited(_session);

                    r = m_rm.makeRoom(m_ci.id,
                        ri, _session);

                    if (r == null)
                    {
                        throw new exception("[Channel::requestMakeRoom][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou criar a sala, mas deu erro na criacao. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                            8, 0));
                    }

                    // Att PlayerCanalInfo
                    updatePlayerInfo(_session);

                    r.sendUpdate();

                    r.sendMake(_session);

                    r.sendPlayerInfo(_session, 0);

                    r.SendPlayerStateLounge(_session);

                    r.sendWeatherLounge(_session);

                    sendUpdateRoomInfo(r.getInfo(), 1);

                    // r.SendGalleryList(_session, 0);

                    if (r.getInfo().getTipo() != RoomInfo.ROOM_INFO_TYPE.PRACTICE && r.getInfo().getTipo() != RoomInfo.ROOM_INFO_TYPE.GRAND_ZODIAC_PRACTICE)
                    {
                        sendUpdatePlayerInfo(_session, 3);
                    }

                    // Guild Battle precisa enviar o sendCharacter opção 0 duas vezes.
                    // Uma na sua posição normal e outra depois de atualizar o info da sala na lobby
                    if (r.getInfo().getTipo() == RoomInfo.ROOM_INFO_TYPE.GUILD_BATTLE)
                    {
                        r.sendPlayerInfo(_session, 0);
                    }

                    // Verifica se é Tourney, Short Game, SSC e ver se tem senha e se a senha é "bot", para criar a sala com bot
                    // Verifica se tem o item para criar o bot se tiver cria se não só da a mensagem
                    if (!r.IsWithBot() && !r.IsRoomGM() && r.getInfo().getTipo() == RoomInfo.ROOM_INFO_TYPE.TOURNEY || r.getInfo().getTipo() == RoomInfo.ROOM_INFO_TYPE.SPECIAL_SHUFFLE_COURSE)
                    {

                        try
                        {

                            if (r.isLocked() && r.checkPass("bot"))
                            {
                                r.makeBot(_session);
                            }

                        }
                        catch (exception e)
                        {
                            throw e;
                        }
                    }

                    // Libera a sala
                    if (r != null)
                    {
                        m_rm.addRoom(r);

                        m_rm.unlockRoom(r);
                    }


                }
                catch (exception e)
                {
                    if (r != null)
                    {
                        m_rm.unlockRoom(r);
                    }

                    throw e;
                    // Relança a exception
                }
            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestMakeRoom][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Resposta Error
                p.init_plain(0x49);

                p.WriteUInt16(2); // Error

                packet_func.session_send(p,
                    _session, 1);
            }
        }

        public void requestEnterRoom(Player _session, packet _packet)
        {
            PangyaBinaryWriter p = new PangyaBinaryWriter();

            try
            {

                short sala_numero = _packet.ReadInt16();
                string senha = _packet.ReadString();

                //aqui
                var r = m_rm.findRoom((short)sala_numero);

                if (r == null)
                {
                    throw new exception("[Channel::requestEnterRoom][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou entrar na sala[NUMERO=" + (sala_numero) + "], mas ela nao existe.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        2, 0));
                }


                // Flag Server
                uFlag flag = _session.m_pi.block_flag.m_flag;

                // Player não pode criar sala, exceto Lounge, se ele não estiver bloqueado
                if (flag.all_game && (r.getInfo().getTipo() != RoomInfo.ROOM_INFO_TYPE.LOUNGE || flag.lounge))
                {
                    throw new exception("[Channel::requestEnterRoom][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou entrar um sala[NUMERO=" + (r.getNumero()) + "], mas ele nao pode entrar em nenhuma sala. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        1, 0x780001));
                }

                switch (r.getInfo().getTipo())
                {
                    case RoomInfo.ROOM_INFO_TYPE.STROKE:
                        if (flag.stroke)
                        {
                            throw new exception("[Channel::requestEnterRoom][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou entrar na sala[TIPO=" + ((ushort)r.getInfo().getTipo()) + ", NUMERO=" + (r.getNumero()) + "], mas ele nao pode entrar Stroke. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                2, 0x770001));
                        }
                        break;
                    case RoomInfo.ROOM_INFO_TYPE.MATCH:
                        if (flag.match)
                        {
                            throw new exception("[Channel::requestEnterRoom][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou entrar na sala[TIPO=" + ((ushort)r.getInfo().getTipo()) + ", NUMERO=" + (r.getNumero()) + "], mas ele nao pode entrar Match. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                3, 0x770001));
                        }
                        break;
                    case RoomInfo.ROOM_INFO_TYPE.TOURNEY:
                        if (flag.tourney)
                        {
                            throw new exception("[Channel::requestEnterRoom][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou entrar na sala[TIPO=" + ((ushort)r.getInfo().getTipo()) + ", NUMERO=" + (r.getNumero()) + "], mas ele nao pode entrar Tourney. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                4, 0x770001));
                        }
                        break;
                    case RoomInfo.ROOM_INFO_TYPE.TOURNEY_TEAM:
                        if (flag.team_tourney)
                        {
                            throw new exception("[Channel::requestEnterRoom][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou entrar na sala[TIPO=" + ((ushort)r.getInfo().getTipo()) + ", NUMERO=" + (r.getNumero()) + "], mas ele nao pode entrar Team Tourney. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                5, 0x770001));
                        }
                        break;
                    case RoomInfo.ROOM_INFO_TYPE.GUILD_BATTLE:
                        if (flag.guild_battle)
                        {
                            throw new exception("[Channel::requestEnterRoom][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou entrar na sala[TIPO=" + ((ushort)r.getInfo().getTipo()) + ", NUMERO=" + (r.getNumero()) + "], mas ele nao pode entrar Guild Battle. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                6, 0x770001));
                        }
                        break;
                    case RoomInfo.ROOM_INFO_TYPE.PANG_BATTLE:
                        if (flag.pang_battle)
                        {
                            throw new exception("[Channel::requestEnterRoom][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou entrar na sala[TIPO=" + ((ushort)r.getInfo().getTipo()) + ", NUMERO=" + (r.getNumero()) + "], mas ele nao pode entrar Pang Battle. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                7, 0x770001));
                        }
                        break;
                    case RoomInfo.ROOM_INFO_TYPE.APPROCH:
                        if (flag.approach)
                        {
                            throw new exception("[Channel::requestEnterRoom][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou entrar na sala[TIPO=" + ((ushort)r.getInfo().getTipo()) + ", NUMERO=" + (r.getNumero()) + "], mas ele nao pode entrar Approach. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                8, 0x770001));
                        }
                        break;
                    case RoomInfo.ROOM_INFO_TYPE.LOUNGE:
                        if (flag.lounge)
                        {
                            throw new exception("[Channel::requestEnterRoom][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou entrar na sala[TIPO=" + ((ushort)r.getInfo().getTipo()) + ", NUMERO=" + (r.getNumero()) + "], mas ele nao pode entrar Lounge. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                9, 0x770001));
                        }
                        break;
                    case RoomInfo.ROOM_INFO_TYPE.GRAND_ZODIAC_INT:
                    case RoomInfo.ROOM_INFO_TYPE.GRAND_ZODIAC_ADV:
                    case RoomInfo.ROOM_INFO_TYPE.GRAND_ZODIAC_PRACTICE:
                        if (flag.grand_zodiac)
                        {
                            throw new exception("[Channel::requestEnterRoom][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou entrar na sala[TIPO=" + ((ushort)r.getInfo().getTipo()) + ", NUMERO=" + (r.getNumero()) + "], mas ele nao pode entrar Grand Zodiac. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                10, 0x770001));
                        }
                        break;
                    case RoomInfo.ROOM_INFO_TYPE.GRAND_PRIX:
                        if (flag.grand_prix)
                        {
                            throw new exception("[Channel::requestEnterRoom][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou entrar na sala[TIPO=" + ((ushort)r.getInfo().getTipo()) + ", NUMERO=" + (r.getNumero()) + "], mas ele nao pode entrar Grand Prix. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                11, 0x770001));
                        }
                        break;
                    case RoomInfo.ROOM_INFO_TYPE.SPECIAL_SHUFFLE_COURSE:
                        if (flag.ssc)
                        {
                            throw new exception("[Channel::requestEnterRoom][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou entrar na sala[TIPO=" + ((ushort)r.getInfo().getTipo()) + ", NUMERO=" + (r.getNumero()) + "], mas ele nao pode entrar Special Shuffle Course. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                12, 0x770001));
                        }
                        break;
                    case RoomInfo.ROOM_INFO_TYPE.PRACTICE:
                        if (flag.single_play)
                        {
                            throw new exception("[Channel::requestEnterRoom][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou entrar na sala[TIPO=" + ((ushort)r.getInfo().getTipo()) + ", NUMERO=" + (r.getNumero()) + "], mas ele nao pode entrar Practice. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                13, 0x770001));
                        }
                        break;
                }

                if (r.getInfo().special_flag_mod.short_game && (flag.team_tourney || flag.short_game))
                {
                    throw new exception("[Channel::requestEnterRoom][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou entrar na sala[TIPO=" + ((ushort)r.getInfo().getTipo()) + ", NUMERO=" + (r.getNumero()) + "], mas ele nao pode entrar sala Short Game. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        1, 770001));
                }

                if (r.getInfo().getTipo() == RoomInfo.ROOM_INFO_TYPE.GRAND_PRIX)
                {
                    throw new exception("[Channel::requestEnterRoom][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou entrar na sala[TIPO=" + ((ushort)r.getInfo().getTipo()) + ", NUMERO=" + (r.getNumero()) + "], mas nao pode entrar na sala Grand Prix com esse pacote. Hacker.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        15, 0x770001));
                }

                if (r.IsStarted() && _session.m_pi.m_cap.game_master) // GM Entra na sala depois que o jogo começou
                {
                    r.requestSendTimeGame(_session);
                }

                else if (r.isGaming()) // não é GM envia error para o player que ele nao pode entrar na sala depois de ter começado
                {
                    throw new exception("[Channel::requestEnterRoom][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou entrar na sala[NUMERO=" + (sala_numero) + "], mas a sala ja comecou o jogo. Hacker ou Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        10, 0));
                }
                else
                {
                    if (!r.isLocked() || r.isInvited(_session) || (_session.m_pi.m_cap.game_master/* & 4/*GM*/) || (!senha.empty() && r.checkPass(senha)))
                    {
                        if (r.isInvited(_session))
                        {
                            // Deleta convite

                            // Add convidado a sala
                            if (!r.isFull() && r.getInvited(_session) != null)
                            {
                                var ici = r.deleteInvited(_session);//nao era pra deletar ?

                                r.enter(_session);

                                deleteInviteTimeRequest(ici);
                            }
                        }
                        else if (!r.isFull())
                        {
                            // Verifica se o player foi convidado em outra sala
                            // e tira o convite dele
                            deleteInviteTimeResquestByInvited(_session);

                            r.enter(_session);
                        }
                        else
                        {
                            throw new exception("[Channel::requestEnterRoom][Warning] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou entrar na sala[NUMERO=" + (sala_numero) + "], mas a sala esta cheia.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                3, 0));
                        }
                    }
                    else
                    {
                        throw new exception("[Channel::requestEnterRoom][Warning] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou entrar na sala[NUMERO=" + (sala_numero) + "], mas a senha nao é igual a da sala.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                            4, 0));
                    }

                    // Att PlayerCanalInfo
                    updatePlayerInfo(_session);

                    r.sendUpdate();

                    r.sendMake(_session);

                    r.sendPlayerInfo(_session, 0);//zero e a lista

                    r.sendPlayerInfo(_session, 1);//1 e o criador

                    r.SendPlayerStateLounge(_session);

                    r.sendWeatherLounge(_session);

                    sendUpdateRoomInfo(r.getInfo(), 3);

                    if (r.getInfo().getTipo() != RoomInfo.ROOM_INFO_TYPE.PRACTICE && r.getInfo().getTipo() != RoomInfo.ROOM_INFO_TYPE.GRAND_ZODIAC_PRACTICE)
                    {
                        sendUpdatePlayerInfo(_session, 3);
                    }

                    // Guild Battle precisa enviar o sendCharacter opção 0 duas vezes.
                    // Uma na sua posição normal e outra depois de atualizar o info da sala na lobby
                    if (r.getInfo().getTipo() == RoomInfo.ROOM_INFO_TYPE.GUILD_BATTLE)
                    {
                        r.sendPlayerInfo(_session, 0);
                    }
                }
            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestEnterRoom][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Resposta Error
                p.init_plain(0x49);

                p.WriteByte(1); // Error

                packet_func.session_send(p, _session, 1);
            }
        }

        public void requestChangeInfoRoom(Player _session, packet _packet)
        {
            try
            {
                var r = m_rm.findRoom((short)_session.m_pi.mi.sala_numero);

                if (r == null)
                {
                    throw new exception("[Channel::requestChangeInfoRoom][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou trocar info da sala[NUMERO=" + (_session.m_pi.mi.sala_numero) + "], mas a sala nao existe.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        10, 0));
                }

                if (r.requestChangeInfoRoom(_session, _packet))
                {
                    sendUpdateRoomInfo(r.getInfo(), 3);
                }



            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestChangeInfoRoom][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if (ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) != STDA_ERROR_TYPE.ROOM)
                {
                    throw;
                }
            }
        }

        public void requestExitRoom(Player _session, packet _packet)
        {
            // Recebe do cliente
            // 1 Byte Option, 0 não está em jogo, 1 está em jogo
            // 2 Byte -1, deve ser o número da sala ou outra coisa que não sei, tipo um valor constante
            // 16 Bytes acho que deve ser a senha da sala de encriptação do pacote1B da sala
            byte option;
            short room_Id;
            try
            {
                option = _packet.ReadUInt8();
                room_Id = _packet.ReadInt16();
                uint gamePang = _packet.ReadUInt32();   // v15
                uint gameBonus = _packet.ReadUInt32();  // v17
                _packet.ReadBytes(out byte[] senhaEncriptSala, 8);
                leaveRoomMultiPlayer(_session, 1);
            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestExitRoom][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestShowInfoRoom(Player _session, packet _packet)
        {
            try
            {
                ushort sala_numero = _packet.ReadUInt16();

                var r = m_rm.findRoom((short)sala_numero);

                // aqui tem que passar o pacote86 com resposta que a sala não existe
                if (r == null)
                {
                    throw new exception("[Channel::requestShowInfoRoom][Error] Channel[ID=" + ((ushort)m_ci.id) + "], PLAYER [UID=" + (_session.m_pi.uid) + "] pediu info da sala[NUMERO=" + (sala_numero) + "] nao existe.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        10, 0));
                }

                PangyaBinaryWriter p = new PangyaBinaryWriter((ushort)0x86);

                var ri = r.getInfo();

                p.WriteUInt32(ri.num_player);
                p.WriteByte(ri.qntd_hole);
                p.WriteUInt32((ri.getTipo() == RoomInfo.ROOM_INFO_TYPE.STROKE || ri.getTipo() == RoomInfo.ROOM_INFO_TYPE.MATCH || ri.getTipo() == RoomInfo.ROOM_INFO_TYPE.PANG_BATTLE) ? ri.time_vs : ((ri.getTipo() == RoomInfo.ROOM_INFO_TYPE.GUILD_BATTLE) ? 0 : ri.time_30s));
                p.WriteByte((byte)ri.course);
                p.WriteByte((byte)ri.getTipo());
                p.WriteByte(ri.modo);
                p.WriteUInt32(ri.trofel);

                List<Player> v_session = r.getSessions();
                PlayerLobbyInfo pci = null;

                for (var i = 0; i < v_session.Count; ++i)
                {
                    pci = getPlayerInfo(v_session[i]);

                    if (pci == null)
                    {
                        throw new exception("[Channel::requestShowInfoRoom][Error] Channel[ID=" + ((ushort)m_ci.id) + "], PLAYER [UID=" + (_session.m_pi.uid) + "] nao tem o info do player na sala[NUMERO=" + (sala_numero) + "].", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                            11, 0));
                    }

                    p.WriteInt32(pci.oid);
                    p.WriteByte(pci.level);
                    p.WriteByte(r.requestPlace(v_session[i])); // se estiver jogando, aqui fica o número do hole

                    // Cap do player, se for GM so mostra se estiver com a type visible
                    p.WriteInt32(pci.capability.ulCapability);
                    p.WriteUInt32(pci.title);
                    p.WriteUInt32(pci.ladder_point);
                }

                packet_func.session_send(p,
                    _session, 0);
            }
            catch (exception e)
            {
                _smp.message_pool.getInstance().push(new message("[Channel::requestShowInfoRoom][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestPlayerLocationRoom(Player _session, packet _packet)
        {
            try
            {
                var r = m_rm.findRoom((short)_session.m_pi.mi.sala_numero);

                if (r == null)
                    throw new exception("[Channel::requestPlayerLocationRoom][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou trocar localizacao na sala[NUMERO=" + (_session.m_pi.mi.sala_numero) + "], mas ela nao existe. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                           10, 0));

                TPLAYER_ACTION type = (TPLAYER_ACTION)_packet.ReadUInt8();

                PangyaBinaryWriter p = new PangyaBinaryWriter();

                switch (type)
                {
                    case TPLAYER_ACTION.PLAYER_ACTION_ROTATION: // R - Face
                        {
                            _session.m_pi.location.r = _packet.ReadFloat();

                            r.updatePlayerInfo(_session);

                            p.init_plain(0xC4);

                            p.WriteInt32(_session.m_oid);
                            p.WriteByte(type);

                            p.WriteFloat(_session.m_pi.location.r);

                            packet_func.room_broadcast(r,
                                p, 0);
                            //aqui eu envio as animacoes se caso o jogador esteja fazendo....
                            //r.SendPlayerAnimation(_session, type);
                            break;
                        }
                    case TPLAYER_ACTION.PLAYER_ACTION_MOTION_ROOM: // Motion In Room
                        {
                            _session.m_pi.animation = _packet.GetRemainingData;

                            r.updatePlayerInfo(_session);

                            // verifca algumas coisa se necessario e envia a resposta para o cliente
                            p.init_plain(0xC4);

                            p.WriteInt32(_session.m_oid);
                            p.WriteByte(type);

                            p.WriteBytes(_session.m_pi.animation);

                            packet_func.room_broadcast(r,
                                p, 0);
                            //aqui eu envio as animacoes se caso o jogador esteja fazendo....
                            //r.SendPlayerAnimation(_session,type);
                            break;
                        }
                    case TPLAYER_ACTION.PLAYER_ACTION_LOUNGER_LOC: // X Z R, coordenada inicial do player no lounge
                        {
                            PlayerRoomInfo.stLocation location_add = new PlayerRoomInfo.stLocation().ToRead(_packet);

                            _session.m_pi.location.x += location_add.x;
                            _session.m_pi.location.z += location_add.z;
                            _session.m_pi.location.r = location_add.r;

                            r.updatePlayerInfo(_session);

                            p.init_plain(0xC4);

                            p.WriteInt32(_session.m_oid);
                            p.WriteByte(type);

                            p.WriteBytes(location_add.ToArray());

                            packet_func.room_broadcast(r,
                                p, 0);

                            //aqui eu envio as animacoes se caso outros jogador esteja fazendo.... 
                            //r.SendPlayerAnimation(_session, TPLAYER_ACTION.PLAYER_ACTION_MOTION_LOUNGER);//envia essse e depois o o outro
                            //r.SendPlayerAnimation(_session, TPLAYER_ACTION.PLAYER_ANIMATION_WITH_EFFECTS);
                            //r.SendPlayerAnimation(_session, TPLAYER_ACTION.PLAYER_ACTION_LOUNGER_STATE);
                            //r.SendPlayerAnimation(_session, TPLAYER_ACTION.PLAYER_ACTION_ACK_PLAYER);
                            break;
                        }
                    case TPLAYER_ACTION.PLAYER_ACTION_LOUNGER_STATE: // Estado do player na sala, se o player esta sentado, deitado ou em pé
                        {
                            _session.m_pi.state = _packet.ReadUInt32();

                            r.updatePlayerInfo(_session);

                            p.init_plain(0xC4);

                            p.WriteInt32(_session.m_oid);
                            p.WriteByte(type);

                            p.WriteUInt32(_session.m_pi.state);

                            packet_func.room_broadcast(r,
                                p, 0);

                            //r.SendPlayerAnimation(_session, type);
                            break;
                        }
                    case TPLAYER_ACTION.PLAYER_ACTION_MOVE: // Player está andando no lounge, X, Z, R
                        {
                            PlayerRoomInfo.stLocation location_add = new PlayerRoomInfo.stLocation().ToRead(_packet);

                            _session.m_pi.location.x += location_add.x;
                            _session.m_pi.location.z += location_add.z;
                            _session.m_pi.location.r = location_add.r;

                            r.updatePlayerInfo(_session);

                            p.init_plain(0xC4);

                            p.WriteInt32(_session.m_oid);
                            p.WriteByte(type);

                            p.WriteBytes(location_add.ToArray());

                            packet_func.room_broadcast(r,
                                p, 0);
                            break;
                        }
                    case TPLAYER_ACTION.PLAYER_ACTION_MOTION_LOUNGER: // Motion no lounge
                        {
                            _session.m_pi.animation = _packet.GetRemainingData;

                            r.updatePlayerInfo(_session);

                            // verifca algumas coisa se necessario e envia a resposta para o cliente
                            p.init_plain(0xC4);

                            p.WriteInt32(_session.m_oid);
                            p.WriteByte(type);

                            p.WriteBytes(_session.m_pi.animation);//string bad

                            packet_func.room_broadcast(r,
                                p, 0);
                            break;
                        }
                    case TPLAYER_ACTION.PLAYER_ACTION_ACK_PLAYER: // Estado do player de icon no lounge
                        {
                            _session.m_pi.state_lounge = _packet.ReadUInt32();

                            r.updatePlayerInfo(_session);

                            p.init_plain(0xC4);

                            p.WriteInt32(_session.m_oid);
                            p.WriteByte(type);

                            p.WriteUInt32(_session.m_pi.state_lounge);

                            packet_func.room_broadcast(r,
                                p, 0);
                            break;
                        }
                    case TPLAYER_ACTION.PLAYER_ANIMATION_WITH_EFFECTS: // Motion no lounge de item especial
                        {
                            _session.m_pi.animation = _packet.GetRemainingData;

                            r.updatePlayerInfo(_session);

                            // verifca algumas coisa se necessario e envia a resposta para o cliente
                            p.init_plain(0xC4);

                            p.WriteInt32(_session.m_oid);
                            p.WriteByte(type);
                            p.WriteBytes(_session.m_pi.animation);
                            packet_func.room_broadcast(r,
                                p, 0);

                            break;
                        }
                    default:
                        throw new exception("[Channel::requestPlayerLocationRoom][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou trocar localizacao na sala[NUMERO=" + (_session.m_pi.mi.sala_numero) + "], mas o type desconhecido. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                            11, 0));
                }
            }
            catch (exception e)
            {
                _smp.message_pool.getInstance().push(new message("[Channel::requestPlayerLocationRoom][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestChangePlayerStateReadyRoom(Player _session, packet _packet)
        {
            try
            {
                var r = m_rm.findRoom((short)_session.m_pi.mi.sala_numero);

                if (r == null)
                {
                    throw new exception("[Channel::requestChangePlayerStateReadyRoom][Error] Channel[ID=" + ((ushort)m_ci.id) + "] sala[NUMERO=" + (_session.m_pi.mi.sala_numero) + "] nao existe.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        10, 0));
                }

                r.requestChangePlayerStateReadyRoom(_session, _packet);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestChangePlayerStateReadyRoom][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestKickPlayerOfRoom(Player _session, packet _packet)
        {
            try
            {
                uint uid = _packet.ReadUInt32();

                var r = m_rm.findRoom((short)_session.m_pi.mi.sala_numero);

                if (r == null)
                {
                    throw new exception("[Channel::requestKickPlayerOfRoom][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou chutar um PLAYER [UID=" + (uid) + "] da sala[NUMERO=" + (_session.m_pi.mi.sala_numero) + "], mas sala nao existe. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        10, 0));
                }

                if (r.getMaster() != _session.m_pi.uid)
                {
                    throw new exception("[Channel::requestKickPlayerOfRoom][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou chutar um PLAYER [UID=" + (uid) + "] da sala[NUMERO=" + (r.getNumero()) + "], mas o player nao é master da sala para poder chutar(kick) o player. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        11, 0));
                }

                // Se não for GM, não pode kikar o player da sala com jogo em andamento
                if (!_session.m_pi.m_cap.game_master && r.isGaming())
                {
                    throw new exception("[Channel::requestKickPlayerOfRoom][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou chutar um PLAYER [UID=" + (uid) + "] da sala[NUMERO=" + (r.getNumero()) + "], mas o player é GM para poder chutar o player da sala com o jogo em andamento.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        13, 0));
                }

                var kick = r.findSessionByUID(uid);

                if (kick == null)
                {
                    throw new exception("[Channel::requestKickPlayerOfRoom][Error] PLAYER [UID=" + (uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou chutar um PLAYER [UID=" + (uid) + "] da sala[NUMERO=" + (r.getNumero()) + "], mas o player nao existe na sala. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        12, 0));
                }

                // Player precisa do pacote para sair da sala
                // Não precisa verifica se é Grand Prix o multiplayer,
                // o pacote do multiplayer serve para kikar o player da sala. O pacote do GP no GP buga
                leaveRoomMultiPlayer(kick, 3);
            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[channel:requestKickPlayerOfRoom][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestChangePlayerTeamRoom(Player _session, packet _packet)
        {
            try
            {
                var r = m_rm.findRoom((short)_session.m_pi.mi.sala_numero);

                if (r == null)
                {
                    throw new exception("[Channel::requestChangePlayerTeamRoom][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou trocar de team(time) na sala[NUMERO=" + (_session.m_pi.mi.sala_numero) + "], mas a sala nao existe. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        10, 0));
                }

                r.requestChangeTeam(_session, _packet);
            }
            catch (exception e)
            {
                _smp.message_pool.getInstance().push(new message("[Channel::requestChangePlayerTeamRoom][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestChangePlayerStateAFKRoom(Player _session, packet _packet)
        {
            try
            {





                byte state = _packet.ReadUInt8();


                var r = m_rm.findRoom((short)_session.m_pi.mi.sala_numero);

                if (r == null)
                {
                    throw new exception("[Channel::requestChangePlayerStateAFKRoom][Error] Channel[ID=" + ((ushort)m_ci.id) + "] sala[NUMERO=" + (_session.m_pi.mi.sala_numero) + "] nao existe.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        10, 0));
                }

                PlayerRoomInfoEx pri = r.getPlayerInfo(_session);

                PlayerLobbyInfo pci = getPlayerInfo(_session);

                if (pri == null)
                {
                    throw new exception("[Channel::requestChangePlayerStateAFKRoom][Error] Channel[ID=" + ((ushort)m_ci.id) + "] nao tem o info do player na sala[NUMERO=" + (_session.m_pi.mi.sala_numero) + "].", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        11, 0));
                }

                if (pci == null)
                {
                    throw new exception("[Channel::requestChangePlayerStateAFKRoom][Error] Channel[ID=" + ((ushort)m_ci.id) + "] nao tem o info do player no canal.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        12, 0));
                }

                pci.state_flag.away = pri.state_flag.away = state;

                PangyaBinaryWriter p = new PangyaBinaryWriter((ushort)0x8E);

                p.WriteInt32(_session.m_oid);

                p.WriteByte(state);

                packet_func.room_broadcast(r,
                    p, 0);

                packet_func.channel_broadcast(this,
                              packet_func.pacote046(
                          new List<PlayerLobbyInfo>() { (pci == null) ? new PlayerLobbyInfo() : pci },
                              3), 0);
            }
            catch (exception e)
            {
                _smp.message_pool.getInstance().push(new message("[requestChangePlayerStateAFKRoom][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestPlayerStateCharacterLounge(Player _session, packet _packet)
        {
            try
            {
                var r = m_rm.findRoom((short)_session.m_pi.mi.sala_numero);

                if (r == null)
                {
                    throw new exception("[Channel::requestPlayerStateCharacterLounge][Error] Channel[ID=" + ((ushort)m_ci.id) + "] sala[NUMERO=" + (_session.m_pi.mi.sala_numero) + "] nao existe.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        10, 0));
                }

                if (r.getInfo().getTipo() != RoomInfo.ROOM_INFO_TYPE.LOUNGE)
                {
                    throw new exception("[Channel::requestPlayerStateCharacterLounge][Error] Channel[ID=" + ((ushort)m_ci.id) + "] sala[NUMERO=" + (_session.m_pi.mi.sala_numero) + "] nao é um lounge.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        12, 0));
                }

                var it = (_session.m_pi.ei.char_info == null) ? new KeyValuePair<int, StateCharacterLounge>() : _session.m_pi.mp_scl.FirstOrDefault(c => c.Key == _session.m_pi.ei.char_info.id);

                if (it.Value == null)
                {
                    throw new exception("[Channel::requestPlayerStateCharacterLounge][Error] Channel[ID=" + ((ushort)m_ci.id) + "] sala[NUMERO=" + (_session.m_pi.mi.sala_numero) + "] nao tem os estados do character na lounge.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        13, 0));
                }

                PangyaBinaryWriter p = new PangyaBinaryWriter(0x196);

                p.WriteInt32(_session.m_oid);

                p.WriteBytes(it.Value.ToArray());

                packet_func.room_broadcast(r,
                    p, 0);
            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestPlayerStateCharacterLounge][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestToggleAssist(Player _session, packet _packet)
        {
            //

            try
            {






                var r = m_rm.findRoom((short)_session.m_pi.mi.sala_numero);

                if (r == null)
                {
                    throw new exception("[Channel::requestToggleAssist][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou alterna Assist Modo na sala[NUMERO=" + (_session.m_pi.mi.sala_numero) + "], mas ele nao esta em nenhuma sala. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        1, 0x5800101));
                }

                r.requestToggleAssist(_session, _packet);
            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestToggleAssist][Error] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestInvite(Player _session, packet _packet)
        {
            //

            PangyaBinaryWriter p = new PangyaBinaryWriter();

            try
            {

                string nickname = _packet.ReadString();
                uint uid = _packet.ReadUInt32();





                var s = findSessionByNickname(nickname);

                if (s == null || s.m_pi.uid != uid)
                {
                    throw new exception("[Channel::requestInvite][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou convidar o PLAYER [UID=" + (uid) + ", NICKNAME=" + nickname + "] para Sala[NUMERO=" + (_session.m_pi.mi.sala_numero) + "], mas o player nao esta nesse canal. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        3000, 23));
                }

                if (s.m_pi.mi.sala_numero != ushort.MaxValue)
                {
                    throw new exception("[Channel::requestInvite][Warning] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou convidar o PLAYER [UID=" + (uid) + ", NICKNAME=" + nickname + "] para Sala[NUMERO=" + (_session.m_pi.mi.sala_numero) + "], mas o player ja esta em outra sala.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        3002, 23));
                }

                if (s.m_pi.place != 0)
                {
                    throw new exception("[Channel::requestInvite][Warning] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou convidar o PLAYER [UID=" + (uid) + ", NICKNAME=" + nickname + "] para Sala[NUMERO=" + (_session.m_pi.mi.sala_numero) + "], mas o player nao pode ser convidado no momento.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        3002, 23));
                }


                var r = m_rm.findRoom((short)_session.m_pi.mi.sala_numero);

                if (r == null)
                {
                    throw new exception("[Channel::requestInvite][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou convidar o PLAYER [UID=" + (uid) + ", NICKNAME=" + nickname + "] para Sala[NUMERO=" + (_session.m_pi.mi.sala_numero) + "], mas ele nao esta em nenhuma sala para poder convidar. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        3001, 23));
                }

                var ici = r.addInvited(_session.m_pi.uid, s);

                // Adiciona para o List ou Mapa do canal que monitora os convites
                addInviteTimeRequest(ici);

                sendUpdateRoomInfo(r.getInfo(), 3);

                // Resposta Invite Player
                p.init_plain(0x12F);

                p.WriteUInt16(0); // Ok

                p.WriteUInt32(sgs.gs.getInstance().getUID());

                p.WriteByte(m_ci.id);
                p.WriteUInt16(r.getNumero());

                p.WriteUInt32(_session.m_pi.uid);
                p.WriteString(_session.m_pi.nickname);

                p.WriteUInt32(s.m_pi.uid);

                packet_func.session_send(p,
                    _session, 1);

                // Envia o Convite para o player
                p.init_plain(0x83);

                p.WriteUInt16(0); // OK

                p.WriteUInt32(sgs.gs.getInstance().getUID());

                p.WriteByte(m_ci.id);
                p.WriteUInt16(r.getNumero());

                p.WriteUInt32(_session.m_pi.uid);
                p.WriteString(_session.m_pi.nickname);

                p.WriteUInt32(s.m_pi.uid);

                packet_func.session_send(p,
                    s, 1);
            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestInvite][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Resposta Error
                p.init_plain(0x12F);

                p.WriteUInt16((ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) == STDA_ERROR_TYPE.CHANNEL) ? (ushort)ExceptionError.STDA_SYSTEM_ERROR_DECODE(e.getCodeError()) : 23u);

                packet_func.session_send(p,
                    _session, 1);
            }
        }

        public void requestCheckInvite(Player _session, packet _packet)
        {
            //

            try
            {

                // Esse aqui o O Server Original nao retorna nada para o cliente, acho que é só um check
                uint uid = _packet.ReadUInt32();
                if (v_invite.Any(c => c.invited_uid == uid))
                {
                    _smp.message_pool.getInstance().push(new message("[Channel::requestCheckInvite][Sucess] PLAYER [UID=" + (_session.m_pi.uid) + "] enviou convite para o PLAYER [UID=" + (uid) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestCheckInvite][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestChatTeam(Player _session, packet _packet)
        {
            //

            try
            {






                var r = m_rm.findRoom((short)_session.m_pi.mi.sala_numero);

                if (r == null)
                {
                    throw new exception("[Channel::requestChatTeam][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou mandar message no chat do team na sala[NUMERO=" + (_session.m_pi.mi.sala_numero) + "], mas ele nao esta em nenhuma sala. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        1, 0x5900201));
                }

                r.requestChatTeam(_session, _packet);



            }
            catch (exception e)
            {
                _smp.message_pool.getInstance().push(new message("[Channel::requestChatTeam][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }


        public void requestExitedFromWebGuild(Player _session, packet _packet)
        {
            //

            try
            {





                // Verifica se tem alteração nos pangs
                var old_pang = _session.m_pi.ui.pang;

                // Update o pang do server com o valor que está no banco de dados
                _session.m_pi.updatePang();

                if (old_pang != _session.m_pi.ui.pang)
                {

                    // Atualiza o pangs do player no jogo que teve alteração dos pangs do player no banco de dados

                    // UPDATE ON GAME
                    PangyaBinaryWriter p = new PangyaBinaryWriter((ushort)0xC8);

                    p.WriteUInt64(_session.m_pi.ui.pang);
                    p.WriteUInt64(0); // Aqui é o pang que foi gasto, old_pang - new pang, mas o pangya original passa 0 por que pode da número negativo se o old for menor que o novo se adicionar mais pang no db

                    packet_func.session_send(p,
                        _session, 1);
                }

                // Verifica se tem alguma atualização da guild Web para atualizar o player no server e cliente

                // Só verifica se o player estiver em uma guild
                if (_session.m_pi.gi.uid > 0)
                {

                    CmdGuildUpdateActivityInfo cmd_guai = new CmdGuildUpdateActivityInfo(_session.m_pi.gi.uid, // Waiter
                        _session.m_pi.uid, true);

                    snmdb.NormalManagerDB.getInstance().add(0,
                        cmd_guai, null, null);

                    if (cmd_guai.getException().getCodeError() != 0)
                    {
                        throw cmd_guai.getException();
                    }

                    var v_info = cmd_guai.getInfo();

                    if (!v_info.empty())
                    {

                        PangyaBinaryWriter p = new PangyaBinaryWriter();

                        // Verifica todas as alterações que tem na Guild e trata elas
                        foreach (var el in v_info)
                        {

                            switch (el.type)
                            {
                                case GuildUpdateActivityInfo.TYPE_UPDATE.TU_ACCEPTED_MEMBER:
                                    {
                                        // Manda para o Message Server, para atulizar a lista de membros da guild dos membros online,
                                        // que o player foi adicionado na guild
                                        p.init_plain(0x01);

                                        p.WriteUInt32(el.club_uid);
                                        p.WriteUInt32(el.player_uid);

                                        sgs.gs.getInstance().sendCommandToOtherServerWithAuthServer(p, 3);

                                        // Verifica se o player está online e atualiza o info de guild dele no server
                                        var s = sgs.gs.getInstance().findPlayer(el.player_uid);

                                        // Player está online
                                        if (s != null)
                                        {

                                            // Member Info
                                            CmdMemberInfo cmd_mi = new CmdMemberInfo(s.m_pi.uid); // Waiter

                                            snmdb.NormalManagerDB.getInstance().add(0,
                                                cmd_mi, null, null);

                                            if (cmd_mi.getException().getCodeError() != 0)
                                            {
                                                throw cmd_mi.getException();
                                            }

                                            var mi = cmd_mi.getInfo();

                                            // Só atualiza o info da guild se ele estiver em uma guild
                                            if (mi.guild_uid > 0u)
                                            {

                                                // Atualiza os dados de Guild do player, ele foi aceito em uma guild
                                                s.m_pi.mi.guild_mark_img_no = mi.guild_mark_img_no;
                                                s.m_pi.mi.guild_uid = mi.guild_uid;
                                                s.m_pi.mi.guild_pang = mi.guild_pang;
                                                s.m_pi.mi.guild_point = mi.guild_point;

                                                s.m_pi.mi.guild_name =
                                                    mi.guild_name;
                                                s.m_pi.mi.guild_mark_img =
                                                    mi.guild_mark_img;

                                                // Guild info
                                                CmdGuildInfo cmd_gi = new CmdGuildInfo(s.m_pi.uid, // Waiter
                                                    0);

                                                snmdb.NormalManagerDB.getInstance().add(0,
                                                    cmd_gi, null, null);

                                                if (cmd_gi.getException().getCodeError() != 0)
                                                {
                                                    throw cmd_gi.getException();
                                                }

                                                // Atualiza guild info
                                                s.m_pi.gi = cmd_gi.getInfo();

                                                // Verifica se está na lobby e atualiza seu info
                                                if (s.m_pi.lobby != 255)
                                                {

                                                    var c = sgs.gs.getInstance().findChannel(s.m_pi.channel);

                                                    if (c != null)
                                                    {

                                                        c.updatePlayerInfo(s);

                                                        c.sendUpdatePlayerInfo(s, 3);
                                                    }
                                                }
                                            }
                                        }

                                        break;
                                    }
                                case GuildUpdateActivityInfo.TYPE_UPDATE.TU_EXITED_MEMBER:
                                    {
                                        // Manda para o Message Server, para atulizar a lista de membros da guild dos membros online,
                                        // que o player saiu da guild
                                        p.init_plain(0x02);

                                        p.WriteUInt32(el.club_uid);
                                        p.WriteUInt32(el.player_uid);

                                        sgs.gs.getInstance().sendCommandToOtherServerWithAuthServer(p, 3);

                                        // Atualiza o info do player no server que ele saiu da guild
                                        // Limpa os dados da guild do player
                                        // Ele não está mais em uma guild
                                        _session.m_pi.gi.clear();

                                        _session.m_pi.mi.guild_mark_img_no = 0;
                                        _session.m_pi.mi.guild_uid = 0;
                                        _session.m_pi.mi.guild_pang = 0;
                                        _session.m_pi.mi.guild_point = 0;
                                        // 
                                        _session.m_pi.mi.guild_name = "";
                                        // 
                                        _session.m_pi.mi.guild_mark_img = "";

                                        // Verifica se está na lobby e atualiza seu info
                                        if (_session.m_pi.lobby != 255)
                                        {

                                            var c = sgs.gs.getInstance().findChannel(_session.m_pi.channel);

                                            if (c != null)
                                            {

                                                c.updatePlayerInfo(_session);

                                                c.sendUpdatePlayerInfo(_session, 3);
                                            }
                                        }

                                        break;
                                    }
                                case GuildUpdateActivityInfo.TYPE_UPDATE.TU_KICKED_MEMBER:
                                    {
                                        // Manda para o Message Server, para atulizar a lista de membros da guild dos membros online,
                                        // que o player foi chutado da guild
                                        p.init_plain(0x03);

                                        p.WriteUInt32(el.club_uid);
                                        p.WriteUInt32(el.player_uid);

                                        sgs.gs.getInstance().sendCommandToOtherServerWithAuthServer(p, 3);

                                        // Verifica se o player está online e zera o info de guild dele no server
                                        var s = sgs.gs.getInstance().findPlayer(el.player_uid);

                                        // Player está online
                                        // Atualiza o info do player no server que ele saiu da guild
                                        if (s != null)
                                        {

                                            // Limpa os dados da guild do player
                                            // Ele não está mais em uma guild
                                            s.m_pi.gi.clear();

                                            s.m_pi.mi.guild_mark_img_no = 0;
                                            s.m_pi.mi.guild_uid = 0;
                                            s.m_pi.mi.guild_pang = 0;
                                            s.m_pi.mi.guild_point = 0;
                                            s.m_pi.mi.guild_name = "";
                                            s.m_pi.mi.guild_mark_img = "";
                                            // Verifica se está na lobby e atualiza seu info
                                            if (s.m_pi.lobby != 255)
                                            {

                                                var c = sgs.gs.getInstance().findChannel(s.m_pi.channel);

                                                if (c != null)
                                                {

                                                    c.updatePlayerInfo(s);

                                                    c.sendUpdatePlayerInfo(s, 3);
                                                }
                                            }
                                        }

                                        break;
                                    }
                            }

                            // Atualiza o STATE do guild update activity por que ela já foi tratada
                            snmdb.NormalManagerDB.getInstance().add(27,
                                new CmdUpdateGuildUpdateActiviy(el.index),
                                SQLDBResponse, this);
                        }
                    }
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestExitedFromWebGuild][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestStartGame(Player _session, packet _packet)
        {
            //

            try
            {






                var r = m_rm.findRoom((short)_session.m_pi.mi.sala_numero);

                if (r == null)
                {
                    throw new exception("[Channel::requestStartGame][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou comecar o jogo na sala[NUMERO=" + (_session.m_pi.mi.sala_numero) + "], mas ele nao esta em nenhuma sala. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        1, 0x5900201));
                }

                if (r.requestStartGame(_session, _packet))
                {

                    // Atualiza na lobby a sala, que acabou de começar o jogo
                    if (r.getInfo().getTipo() != RoomInfo.ROOM_INFO_TYPE.PRACTICE && r.getInfo().getTipo() != RoomInfo.ROOM_INFO_TYPE.GRAND_ZODIAC_PRACTICE)
                    {
                        PangyaBinaryWriter p = new PangyaBinaryWriter();

                        // Atualiza info da sala na lobby
                        sendUpdateRoomInfo(r.getInfo(), 3);
                    }
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestStartGame][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

        }

        public void requestInitHole(Player _session, packet _packet)
        {
            //

            try
            {






                var r = m_rm.findRoom((short)_session.m_pi.mi.sala_numero);

                if (r == null)
                {
                    throw new exception("[Channel::requestInitHole][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou inicializar o hole, no jogo na sala[NUMERO=" + (_session.m_pi.mi.sala_numero) + "], mas ele nao esta em nenhuma sala. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        1, 0x5900301));
                }

                r.requestInitHole(_session, _packet);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestInitHole][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestFinishLoadHole(Player _session, packet _packet)
        {
            //

            try
            {
                var r = m_rm.findRoom((short)_session.m_pi.mi.sala_numero);

                if (r == null)
                {
                    throw new exception("[Channel::requestFinishLoadHole][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou finalizar carregamento do hole do jogo na sala[NUMERO=" + (_session.m_pi.mi.sala_numero) + "], mas ele nao esta em nenhuma sala. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        1, 0x5900401));
                }

                // Timer do tempo que a sala fica aberta para entrar depois que o Tourney começa
                if (r.requestFinishLoadHole(_session, _packet))
                {
                    // Update State Room
                    r.setState(1);
                    r.setFlag(1);

                    r.requestStartAfterEnter(() =>
                    {
                        if (_session != null && _session.isConnected())
                        {
                            _enter_left_time_is_over(this, _session.m_pi.mi.sala_numero);
                        }
                    });
                    PangyaBinaryWriter p = new PangyaBinaryWriter();

                    // Update Room ON LOBBY
                    sendUpdateRoomInfo(r.getInfo(), 3);
                }



            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestFinishLoadHole][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestFinishCharIntro(Player _session, packet _packet)
        {
            //

            try
            {
                var r = m_rm.findRoom((short)_session.m_pi.mi.sala_numero);

                if (r == null)
                {
                    throw new exception("[Channel::requestFinishCharIntro][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou finalizar Char Intro do jogo na sala[NUMERO=" + (_session.m_pi.mi.sala_numero) + "], mas ele nao esta em nenhuma sala. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        1, 0x5900501));
                }

                r.requestFinishCharIntro(_session, _packet);
            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestFinishCharIntro][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestFinishHoleData(Player _session, packet _packet)
        {
            //

            try
            { 
                var r = m_rm.findRoom((short)_session.m_pi.mi.sala_numero);

                if (r == null)
                {
                    throw new exception("[Channel::requestFinishHoleData][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou finalizar dados do hole, no jogo na sala[NUMERO=" + (_session.m_pi.mi.sala_numero) + "], mas ele nao esta em nenhuma sala. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        1, 0x5902101));
                }

                r.requestFinishHoleData(_session, _packet);
            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestFinishHoleData][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestInitShotSended(Player _session, packet _packet)
        {
            //

            try
            {


                var r = m_rm.findRoom((short)_session.m_pi.mi.sala_numero);

                if (r == null)
                {
                    throw new exception("[Channel::requestInitShotSended][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] o server enviou o pacote de InitShot para o cliente, mas a sala[NUMERO=" + (_session.m_pi.mi.sala_numero) + "] nao existe mais. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        1, 0x5905001));
                }

                r.requestInitShotSended(_session, _packet);



            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestInitShotSended][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestInitShot(Player _session, packet _packet)
        {
            //

            try
            {






                var r = m_rm.findRoom((short)_session.m_pi.mi.sala_numero);

                if (r == null)
                {
                    throw new exception("[Channel::requestInitShot][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou inicializar shot no jogo na sala[NUMERO=" + (_session.m_pi.mi.sala_numero) + "], mas ele nao esta em nenhuma sala. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        1, 0x5901501));
                }

                r.requestInitShot(_session, _packet);



            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestInitShot][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

        }

        public void requestSyncShot(Player _session, packet _packet)
        {
            //

            try
            {

                var r = m_rm.findRoom((short)_session.m_pi.mi.sala_numero);

                if (r == null)
                {
                    throw new exception("[Channel::requestSyncShot][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou sincronizar tacada no jogo na sala[NUMERO=" + (_session.m_pi.mi.sala_numero) + "], mas ele nao esta em nenhuma sala. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        1, 0x5901601));
                }

                r.requestSyncShot(_session, _packet);



            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestSyncShot][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestInitShotArrowSeq(Player _session, packet _packet)
        {
            //

            try
            {
                var r = m_rm.findRoom((short)_session.m_pi.mi.sala_numero);

                if (r == null)
                {
                    throw new exception("[Channel::requestInitShotArrowSeq][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou inicializar a sequencia de setas no jogo na sala[NUMERO=" + (_session.m_pi.mi.sala_numero) + "], mas ele nao esta em nenhuma sala. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        1, 0x5901701));
                }

                r.requestInitShotArrowSeq(_session, _packet);



            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestInitShotArrowSeq][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestShotEndData(Player _session, packet _packet)
        {
            //

            try
            {
                var r = m_rm.findRoom((short)_session.m_pi.mi.sala_numero);

                if (r == null)
                {
                    throw new exception("[Channel::requestShotEndData][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou finalizar local da tacada no jogo na sala[NUMERO=" + (_session.m_pi.mi.sala_numero) + "], mas ele nao esta em nenhuma sala. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        1, 0x5901801));
                }

                r.requestShotEndData(_session, _packet);



            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestShotEndData][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestFinishShot(Player _session, packet _packet)
        {
            //

            try
            {

                var r = m_rm.findRoom((short)_session.m_pi.mi.sala_numero);

                if (r == null)
                {
                    throw new exception("[Channel::requestFinishShot][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou finalizar tacada no jogo na sala[NUMERO=" + (_session.m_pi.mi.sala_numero) + "], mas ele nao esta em nenhuma sala. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        1, 0x5901901));
                }

                var rfs = r.requestFinishShot(_session, _packet);

                if (rfs.ret > 0)
                {

                    if (rfs.ret == 2 && rfs.p != null)
                    {
                        leaveRoom(rfs.p, 2); // Time out ou Give Up não lembro mais direito
                    }


                }
            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestFinishShot][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestChangeMira(Player _session, packet _packet)
        {
            //

            try
            {
                var r = m_rm.findRoom((short)_session.m_pi.mi.sala_numero);

                if (r == null)
                {
                    throw new exception("[Channel::requestChangeMira][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou trocar a mira no jogo na sala[NUMERO=" + (_session.m_pi.mi.sala_numero) + "], mas ele nao esta em nenhuma sala. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        1, 0x5900601));
                }

                r.requestChangeMira(_session, _packet);



            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestChangeMira][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestChangeStateBarSpace(Player _session, packet _packet)
        {
            //

            try
            {
                var r = m_rm.findRoom((short)_session.m_pi.mi.sala_numero);

                if (r == null)
                {
                    throw new exception("[Channel::requestChangeStateBarSpace][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou trocar state da barra de espaco no jogo na sala[NUMERO=" + (_session.m_pi.mi.sala_numero) + "], nas ele nao esta em nenhuma sala. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        1, 0x5900701));
                }

                r.requestChangeStateBarSpace(_session, _packet);



            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestChangeStateBarSpace][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestActivePowerShot(Player _session, packet _packet)
        {
            //

            try
            {






                var r = m_rm.findRoom((short)_session.m_pi.mi.sala_numero);

                if (r == null)
                {
                    throw new exception("[Channel::requestActivePowerShot][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou ativat power shot no jogo na sala[NUMERO=" + (_session.m_pi.mi.sala_numero) + "], mas ele nao esta em nenhuma sala. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        1, 0x5900801));
                }

                r.requestActivePowerShot(_session, _packet);



            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestActivePowerShot][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestChangeClub(Player _session, packet _packet)
        {
            //

            try
            {
                var r = m_rm.findRoom((short)_session.m_pi.mi.sala_numero);

                if (r == null)
                {
                    throw new exception("[Channel::requestChangeClub][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou trocar taco no jogo na sala[NUMERO=" + (_session.m_pi.mi.sala_numero) + "], mas ele nao esta em nenhuma sala. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        1, 0x5900901));
                }

                r.requestChangeClub(_session, _packet);
            }
            catch (exception e)
            {
                _smp.message_pool.getInstance().push(new message("[Channel::requestChangeClub][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestUseActiveItem(Player _session, packet _packet)
        {
            //

            try
            {






                var r = m_rm.findRoom((short)_session.m_pi.mi.sala_numero);

                if (r == null)
                {
                    throw new exception("[Channel::requestUseActiveItem][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou usar active item no jogo na sala[NUMERO=" + (_session.m_pi.mi.sala_numero) + "], mas ele nao esta em nenhuma sala. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        1, 0x5901001));
                }

                r.requestUseActiveItem(_session, _packet);



            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestUseActiveItem][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestChangeStateTypeing(Player _session, packet _packet)
        {
            //

            try
            {






                var r = m_rm.findRoom((short)_session.m_pi.mi.sala_numero);

                if (r == null)
                {
                    throw new exception("[Channel::requestChangeStateTypeing][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou mudar estado de escrevendo icon no jogo na sala[NUMERO=" + (_session.m_pi.mi.sala_numero) + "], mas ele nao esta em nenhuma sala. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        1, 0x5901101));
                }

                r.requestChangeStateTypeing(_session, _packet);



            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestChangeStateTypeing][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestMoveBall(Player _session, packet _packet)
        {
            //

            try
            {






                var r = m_rm.findRoom((short)_session.m_pi.mi.sala_numero);

                if (r == null)
                {
                    throw new exception("[Channel::requestMoveBall][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou recolocar a bola no jogo na sala[NUMERO=" + (_session.m_pi.mi.sala_numero) + "], mas ele nao esta em nenhuma sala. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        1, 0x5901201));
                }

                r.requestMoveBall(_session, _packet);



            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestMoveBall][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestChangeStateChatBlock(Player _session, packet _packet)
        {
            //

            try
            {
                var r = m_rm.findRoom((short)_session.m_pi.mi.sala_numero);

                if (r == null)
                {
                    throw new exception("[Channel::requestChangeStateChatBlock][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou mudar estado so chat block no jogo na sala[NUMERO=" + (_session.m_pi.mi.sala_numero) + "], mas ele nao esta em nenhuma sala. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        1, 0x5901301));
                }

                r.requestChangeStateChatBlock(_session, _packet);
            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestChangeStateChatBlock][ErrorSysttem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestActiveBooster(Player _session, packet _packet)
        {
            //

            try
            {






                var r = m_rm.findRoom((short)_session.m_pi.mi.sala_numero);

                if (r == null)
                {
                    throw new exception("[Channel::requestActiveBooster][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou ativar time booster no jogo na sala[NUMERO=" + (_session.m_pi.mi.sala_numero) + "], mas ele nao esta em nenhuma sala. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        1, 0x5901401));
                }

                r.requestActiveBooster(_session, _packet);



            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestActiveBooster][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestActiveReplay(Player _session, packet _packet)
        {
            //

            try
            {






                var r = m_rm.findRoom((short)_session.m_pi.mi.sala_numero);

                if (r == null)
                {
                    throw new exception("[Channel::requestActiveReplay][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou ativar Replay no jogo na sala[NUMERO=" + (_session.m_pi.mi.sala_numero) + "], mas ele nao esta em nenhuma sala. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        1, 0x6001001));
                }

                r.requestActiveReplay(_session, _packet);



            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestActiveReplay][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestActiveCutin(Player _session, packet _packet)
        {
            //

            try
            {






                var r = m_rm.findRoom((short)_session.m_pi.mi.sala_numero);

                if (r == null)
                {
                    throw new exception("[Channel::requestActiveCutin][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou ativar cutin no jogo na sala[NUMERO=" + (_session.m_pi.mi.sala_numero) + "], mas ele nao esta em nenhuma sala. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        1, 0x5901801));
                }

                r.requestActiveCutin(_session, _packet);



            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestActiveCutin][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestActiveAutoCommand(Player _session, packet _packet)
        {
            //

            try
            {






                var r = m_rm.findRoom((short)_session.m_pi.mi.sala_numero);

                if (r == null)
                {
                    throw new exception("[Channel::requestActiveAutoCommand][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou ativar Auto Command no jogo na sala[NUMERO=" + (_session.m_pi.mi.sala_numero) + "], mas ele nao esta em nenhuma sala. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        1, 0x550001));
                }

                r.requestActiveAutoCommand(_session, _packet);



            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestActiveAutoCommand][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestActiveAssistGreen(Player _session, packet _packet)
        {
            //

            try
            {






                var r = m_rm.findRoom((short)_session.m_pi.mi.sala_numero);

                if (r == null)
                {
                    throw new exception("[Channel::requestActiveAssistGreen][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou ativar Assist Green no jogo na sala[NUMERO=" + (_session.m_pi.mi.sala_numero) + "], mas ele nao esta em nenhuma sala. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        1, 0x5901901));
                }

                r.requestActiveAssistGreen(_session, _packet);



            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestActiveAssistGreen][Error] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

            }
        }

        public void requestLoadGamePercent(Player _session, packet _packet)
        {
            //

            try
            {






                var r = m_rm.findRoom((short)_session.m_pi.mi.sala_numero);

                if (r == null)
                {
                    throw new exception("[Channel::requestLoadGamePercent][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou mandar a porcentagem do jogo carregado na sala[NUMEROR=" + (_session.m_pi.mi.sala_numero) + "], mas ele nao esta em nenhuma sala. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        1, 0x551001));
                }

                r.requestLoadGamePercent(_session, _packet);



            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestLoadGamePercent][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestMarkerOnCourse(Player _session, packet _packet)
        {
            //

            try
            {






                var r = m_rm.findRoom((short)_session.m_pi.mi.sala_numero);

                if (r == null)
                {
                    throw new exception("[Channel::requestMarkerOnCourse][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou marcar no course no jogo na sala[NUMERO=" + (_session.m_pi.mi.sala_numero) + "], mas ele nao esta em nenhuma sala. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        1, 0x552001));
                }

                r.requestMarkerOnCourse(_session, _packet);



            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestMarkerOnCourse][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestStartTurnTime(Player _session, packet _packet)
        {
            //

            try
            {






                var r = m_rm.findRoom((short)_session.m_pi.mi.sala_numero);

                if (r == null)
                {
                    throw new exception("[Channel::requestStartTurnTime][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou comecar o tempo do turno no jogo na sala[NUMERO=" + (_session.m_pi.mi.sala_numero) + "], mas ele nao esta em nenhuma sala. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        1, 0x553001));
                }

                r.requestStartTurnTime(_session, _packet);



            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestStartTurnTime][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestUnOrPauseGame(Player _session, packet _packet)
        {
            //

            try
            {






                var r = m_rm.findRoom((short)_session.m_pi.mi.sala_numero);

                if (r == null)
                {
                    throw new exception("[Channel::requestUnOrPauseGame][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou pausar ou despausar o jogo na sala[NUMERO=" + (_session.m_pi.mi.sala_numero) + "], mas ele nao esta em nenhuma sala. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        1, 0x554001));
                }

                r.requestUnOrPauseGame(_session, _packet);



            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestUnOrPauseGame][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestLastPlayerFinishVersus(Player _session, packet _packet)
        {
            //

            try
            {






                var r = m_rm.findRoom((short)_session.m_pi.mi.sala_numero);

                if (r == null)
                {
                    throw new exception("[Channel::requestLastPlayerFinishVersus][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou finalizar o Versus na sala[NUMERO=" + (_session.m_pi.mi.sala_numero) + "], mas ele nao esta em nenhuma sala. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        1, 0x555001));
                }

                if (r.requestLastPlayerFinishVersus(_session, _packet))
                {

                    if (r.getInfo().getTipo() != RoomInfo.ROOM_INFO_TYPE.PRACTICE && r.getInfo().getTipo() != RoomInfo.ROOM_INFO_TYPE.GRAND_ZODIAC_PRACTICE)
                    {
                        // Atualiza info da sala na lobby 
                        packet_func.channel_broadcast(this,
                             packet_func.pacote047(new List<RoomInfoEx>()
                        {
                            r.getInfo()
                        },
                                3), 1);
                    }
                }



            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestLastPlayerFinishVersus][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestReplyContinueVersus(Player _session, packet _packet)
        {
            //

            try
            {






                var r = m_rm.findRoom((short)_session.m_pi.mi.sala_numero);

                if (r == null)
                {
                    throw new exception("[Channel::requestReplyContinueVersus][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou responder se quer continuar o versus ou nao na sala[NUMERO=" + (_session.m_pi.mi.sala_numero) + "]. mas ele nao esta em nenhuma sala. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        1, 0x556001));
                }

                if (r.requestReplyContinueVersus(_session, _packet))
                {

                    if (r.getInfo().getTipo() != RoomInfo.ROOM_INFO_TYPE.PRACTICE && r.getInfo().getTipo() != RoomInfo.ROOM_INFO_TYPE.GRAND_ZODIAC_PRACTICE)
                    {
                        // Atualiza info da sala na lobby
                        packet_func.channel_broadcast(this,
                             packet_func.pacote047(new List<RoomInfoEx>()
                        {
                            r.getInfo()
                        },
                                3), 1);
                    }
                }



            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestReplyContinueVersus][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestTeamFinishHole(Player _session, packet _packet)
        {
            //

            try
            {






                var r = m_rm.findRoom((short)_session.m_pi.mi.sala_numero);

                if (r == null)
                {
                    throw new exception("[Channel::requestTeamFinishHole][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou finalizar o hole no Match na sala[NUMERO=" + (_session.m_pi.uid) + "], mas ele nao esta em nenhum sala. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        1, 0x562001));
                }

                r.requestTeamFinishHole(_session, _packet);



            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestTeamFinishHole][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestLeavePractice(Player _session, packet _packet)
        {
            //

            try
            {






                var r = m_rm.findRoom((short)_session.m_pi.mi.sala_numero);

                if (r == null)
                {
                    throw new exception("[Channel::requestLeavePractice][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou sair do practice na sala[NUMERO=" + (_session.m_pi.mi.sala_numero) + "], mas ele nao esta em nenhuma sala. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        1, 0x6202001));
                }

                if (r.getInfo().getTipo() != RoomInfo.ROOM_INFO_TYPE.PRACTICE)
                {
                    throw new exception("[Channel::requestLeavePratice][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou sair do practice na sala[NUMERO=" + (_session.m_pi.mi.sala_numero) + ", TIPO=" + (r.getInfo().getTipo()) + "], mas a sala nao é um tipo de sala do practice. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        2, 0x6202002));
                }

                r.requestLeavePractice(_session, _packet);



            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestLeavePractice][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestUseTicketReport(Player _session, packet _packet)
        {
            //

            try
            {






                var r = m_rm.findRoom((short)_session.m_pi.mi.sala_numero);

                if (r == null)
                {
                    throw new exception("[Channel::requestUseTicketReport][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou sair do Tourney com Ticket Report no jogo na sala[NUMERO=" + (_session.m_pi.mi.sala_numero) + "], mas ele nao esta em nenhuma sala. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        1, 0x6301001));
                }

                // Verifica se deu certo sair com Ticket Report do Tourney, se sim, sai da Sala
                if (r.requestUseTicketReport(_session, _packet))
                {
                    leaveRoom(_session, 10);
                }



            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestUseTicketReport][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestLeaveChipInPractice(Player _session, packet _packet)
        {
            //

            try
            {





                var r = m_rm.findRoom((short)_session.m_pi.mi.sala_numero);

                if (r == null)
                {
                    throw new exception("[Channel::requestLeaveChipInPractice][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou sair do Chip-in Practice na sala[NUMERO=" + (_session.m_pi.mi.sala_numero) + "], mas ele nao esta em nenhum sala. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        1, 0x6207701));
                }

                r.requestLeaveChipInPractice(_session, _packet);



            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestLeaveChipInPractice][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestStartFirstHoleGrandZodiac(Player _session, packet _packet)
        {
            //

            try
            {





                var r = m_rm.findRoom((short)_session.m_pi.mi.sala_numero);

                if (r == null)
                {
                    throw new exception("[Channel::requestStartFirstHoleGrandZodiac][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou comecar o primeiro hole do Grand Zodiac game na sala[NUMERO=" + (_session.m_pi.mi.sala_numero) + "], mas ele nao esta em nenhum sala. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        1, 0x6207801));
                }

                r.requestStartFirstHoleGrandZodiac(_session, _packet);



            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestStartFirstHoleGrandZodiac][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestReplyInitialValueGrandZodiac(Player _session, packet _packet)
        {
            //

            try
            {





                var r = m_rm.findRoom((short)_session.m_pi.mi.sala_numero);

                if (r == null)
                {
                    throw new exception("[Channel::ReplyInitialValueGrandZodiac][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou responder o valor inicial do Grand Zodiac game na sala[NUMERO=" + (_session.m_pi.mi.sala_numero) + "], mas ele nao esta em nenhum sala. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        1, 0x6207901));
                }

                r.requestReplyInitialValueGrandZodiac(_session, _packet);



            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestReplyInitialValueGrandZodiac][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestActiveRing(Player _session, packet _packet)
        {
            //

            try
            {






                var r = m_rm.findRoom((short)_session.m_pi.mi.sala_numero);

                if (r == null)
                {
                    throw new exception("[Channel::requestActiveRing][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou ativar Anel no jogo na sala[NUMERO=" + (_session.m_pi.mi.sala_numero) + "], mas ele nao esta em nenhum sala. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        1, 0x6201001));
                }

                r.requestActiveRing(_session, _packet);



            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestActiveRing][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestActiveRingGround(Player _session, packet _packet)
        {
            //

            try
            {
                 
                var r = m_rm.findRoom((short)_session.m_pi.mi.sala_numero);

                if (r == null)
                {
                    throw new exception("[Channel::requestActiveRingGround][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou ativar Anel de Terreno no jogo na sala[NUMERO=" + (_session.m_pi.mi.sala_numero) + "], mas ele nao esta em nenhuma sala. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        1, 0x6201101));
                }

                r.requestActiveRingGround(_session, _packet); 
            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestActiveRingGround][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestActiveRingPawsRainbowJP(Player _session, packet _packet)
        {
            //

            try
            {






                var r = m_rm.findRoom((short)_session.m_pi.mi.sala_numero);

                if (r == null)
                {
                    throw new exception("[Channel::requestActiveRingPawsRainbowJP][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou ativar Anel de Patinha Arco-iris JP no jogo na sala[NUMERO=" + (_session.m_pi.mi.sala_numero) + "], mas ele nao esta em nenhuma sala. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        1, 0x6201201));
                }

                r.requestActiveRingPawsRainbowJP(_session, _packet);



            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestActiveRingPawsRainbowJP][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestActiveRingPawsRingSetJP(Player _session, packet _packet)
        {
            //

            try
            {






                var r = m_rm.findRoom((short)_session.m_pi.mi.sala_numero);

                if (r == null)
                {
                    throw new exception("[Channel::requestActiveRingPawsRingSetJP][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou ativar Anel de Patinha de Conjunto de Aneis [JP] no jogo na sala[NUMERO=" + (_session.m_pi.mi.sala_numero) + "], mas ele nao esta em nenhuma sala. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        1, 0x6201301));
                }

                r.requestActiveRingPawsRingSetJP(_session, _packet);



            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestActiveRingPawsRingSetJP][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestActiveRingPowerGagueJP(Player _session, packet _packet)
        {
            //

            try
            {






                var r = m_rm.findRoom((short)_session.m_pi.mi.sala_numero);

                if (r == null)
                {
                    throw new exception("[Channel::requestActiveRingPowerGagueJP][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou ativar Anel Barra de PS [JP] no jogo na sala[NUMERO=" + (_session.m_pi.mi.sala_numero) + "], mas ele nao esta em nenhum sala. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        1, 0x6201401));
                }

                r.requestActiveRingPowerGagueJP(_session, _packet);



            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestActiveRingPowerGagueJP][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestActiveRingMiracleSignJP(Player _session, packet _packet)
        {
            //

            try
            {






                var r = m_rm.findRoom((short)_session.m_pi.mi.sala_numero);

                if (r == null)
                {
                    throw new exception("[Channel::requestActiveRingMiracleSignJP][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou ativar Anel Olho Magico [JP] no jogo na sala[NUMERO=" + (_session.m_pi.mi.sala_numero) + "], mas ele nao esta em nenhuma sala. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        1, 0x6201501));
                }

                r.requestActiveRingMiracleSignJP(_session, _packet);



            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestActiveRingMiracleSignJP][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestActiveWing(Player _session, packet _packet)
        {
            //

            try
            {






                var r = m_rm.findRoom((short)_session.m_pi.mi.sala_numero);

                if (r == null)
                {
                    throw new exception("[Channel::requestActiveWing][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou ativar Asa no jogo na sala[NUMERO=" + (_session.m_pi.mi.sala_numero) + "], mas ele nao esta em nenhuma sala. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        1, 0x6201601));
                }

                r.requestActiveWing(_session, _packet);
            }
            catch (exception e)
            {
                _smp.message_pool.getInstance().push(new message("[Channel::requestActiveWing][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestActivePaws(Player _session, packet _packet)
        {
            //

            try
            {






                var r = m_rm.findRoom((short)_session.m_pi.mi.sala_numero);

                if (r == null)
                {
                    throw new exception("[Channel::requestActivePaws][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou ativar Patinha no jogo na sala[NUMERO=" + (_session.m_pi.mi.sala_numero) + "], mas ele nao esta em nenhuma sala. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        1, 0x6201701));
                }

                r.requestActivePaws(_session, _packet);



            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestActivePaws][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestActiveGlove(Player _session, packet _packet)
        {
            //

            try
            {






                var r = m_rm.findRoom((short)_session.m_pi.mi.sala_numero);

                if (r == null)
                {
                    throw new exception("[Channel::requestActiveGlove][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou ativar Luva 1m no jogo na sala[NUMERO=" + (_session.m_pi.mi.sala_numero) + "], mas ele nao esta em nenhuma sala. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        1, 0x6201801));
                }

                r.requestActiveGlove(_session, _packet);



            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestActiveGlove][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestActiveEarcuff(Player _session, packet _packet)
        {
            //

            try
            {






                var r = m_rm.findRoom((short)_session.m_pi.mi.sala_numero);

                if (r == null)
                {
                    throw new exception("[Channel::requestActiveEarcuff][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou ativar Earcuff no jogo na sala[NUMERO=" + (_session.m_pi.mi.sala_numero) + "], mas ele nao esta em nenhuma sala. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        1, 0x6201901));
                }

                r.requestActiveEarcuff(_session, _packet);



            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestActiveEarcuff][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestEnterGameAfterStarted(Player _session, packet _packet)
        {
            //

            PangyaBinaryWriter p = new PangyaBinaryWriter();

            try
            {

                byte option = _packet.ReadUInt8();





                if (option == 0 || option == 1)
                {

                    ushort sala_numero = _packet.ReadUInt16();


                    var r = m_rm.findRoom((short)sala_numero);

                    if (r == null)
                    {
                        throw new exception("[Channel::requestEnterGameAfterStarted][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou entrar na sala[NUMERO=" + (sala_numero) + "] ja em jogo, mas ela nao existe. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                            2700, 1));
                    }

                    if (r.getInfo().getTipo() != RoomInfo.ROOM_INFO_TYPE.TOURNEY)
                    {
                        throw new exception("[Channel::requestEnterGameAfterStarted][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou entrar na sala[TIPO=" + ((ushort)r.getInfo().getTipo()) + ", NUMERO=" + (r.getNumero()) + "] ja em jogo, mas o tipo da sala nao é Tourney. Hacker.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                            15, 0x770001));
                    }

                    if (r.isLocked())
                    {
                        throw new exception("[Channel::requestEnterGameAfterStarted][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou entrar na sala[NUMERO=" + (sala_numero) + "] ja em jogo, mas a sala é privada. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                            2710, 1));
                    }

                    if (!r.isGaming())
                    {
                        throw new exception("[Channel::requestEnterGameAfterStarted][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou entrar na sala[NUMERO=" + (sala_numero) + "] ja em jogo, mas a sala nao esta em jogo ainda. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                            2701, 1));
                    }

                    if (r.isFull())
                    {
                        throw new exception("[Channel::requestEnterGameAfterStarted][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou entrar na sala[NUMERO=" + (sala_numero) + "] ja em jogo, mas a sala ja esta no seu limite de jogadores.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                            2702, 1));
                    }

                    if (option == 0)
                    {
                        r.requestSendTimeGame(_session);
                    }
                    else if (option == 1)
                    {

                        try
                        {

                            // Verifica se o player foi convidado em outra sala
                            // e tira o convite dele
                            deleteInviteTimeResquestByInvited(_session);

                            if (r.requestEnterGameAfterStarted(_session))
                            {

                                sendUpdateRoomInfo(r.getInfo(), 3);

                                // update info player no canal
                                updatePlayerInfo(_session);

                                if (r.getInfo().getTipo() != RoomInfo.ROOM_INFO_TYPE.PRACTICE && r.getInfo().getTipo() != RoomInfo.ROOM_INFO_TYPE.GRAND_ZODIAC_PRACTICE)
                                {
                                    sendUpdatePlayerInfo(_session, 3);
                                }

                            }

                        }
                        catch (exception e)
                        {
                            // UNREFERENCED_PARAMETER(e);

                            throw e;
                        }

                    }



                }
                else if (option == 2)
                {

                    EnterAfterStartInfo easi = new EnterAfterStartInfo();

                    // Le tacada (18 bytes)
                    for (int i = 0; i < 18; i++)
                        easi.tacada[i] = _packet.ReadByte();

                    // Le score (18 int32)
                    for (int i = 0; i < 18; i++)
                        easi.score[i] = _packet.ReadInt32();

                    // Le pang (18 uint64)
                    for (int i = 0; i < 18; i++)
                        easi.pang[i] = _packet.ReadUInt64();

                    // request_oid (int32)
                    easi.request_oid = _packet.ReadInt32();

                    // owner_oid (uint32)
                    easi.owner_oid = _packet.ReadUInt32();

                    var r = m_rm.findRoom((short)_session.m_pi.mi.sala_numero);

                    if (r == null)
                    {
                        throw new exception("[Channel::requestEnterGameAfterStarted][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou entrar na sala[NUMERO=" + (_session.m_pi.mi.sala_numero) + "] ja em jogo, mas ela nao existe. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                            2700, 1));
                    }

                    if (!r.isGaming())
                    {
                        throw new exception("[Channel::requestEnterGameAfterStarted][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou entrar na sala[NUMERO=" + (_session.m_pi.mi.sala_numero) + "] ja em jogo, mas a sala nao esta em jogo ainda. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                            2701, 1));
                    }

                    r.requestUpdateEnterAfterStartedInfo(_session, easi);


                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestEnterGameAfterStarted][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Resposta erro
                p.init_plain(0x113);

                p.WriteByte(6); // Option Error

                // Error Code
                p.WriteByte((byte)((ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) == STDA_ERROR_TYPE.CHANNEL) ? ExceptionError.STDA_SYSTEM_ERROR_DECODE(e.getCodeError()) : 1));

                packet_func.session_send(p,
                    _session, 1);
            }
        }

        public void requestFinishGame(Player _session, packet _packet)
        {
            //

            try
            {
                var r = m_rm.findRoom((short)_session.m_pi.mi.sala_numero);

                if (r == null)
                {
                    throw new exception("[Channel::requestFinishGame][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou finalizar o jogo na sala[NUMERO=" + (_session.m_pi.mi.sala_numero) + "], mas ele nao esta em nenhum sala. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        1, 0x5902201));
                }

                if (r.requestFinishGame(_session, _packet))
                { // Terminou o jogo

                    if (r.getInfo().getTipo() != RoomInfo.ROOM_INFO_TYPE.PRACTICE && r.getInfo().getTipo() != RoomInfo.ROOM_INFO_TYPE.GRAND_ZODIAC_PRACTICE)
                    {
                        // Atualiza info da sala na lobby
                        packet_func.channel_broadcast(this,
                            packet_func.pacote047(new List<RoomInfoEx>()
                       {
                            r.getInfo()
                       },
                               3), 1);
                    }
                }
            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestFinishGame][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestChangeWindNextHoleRepeat(Player _session, packet _packet)
        {
            //

            try
            {






                var r = m_rm.findRoom((short)_session.m_pi.mi.sala_numero);

                if (r == null)
                {
                    throw new exception("[Channel::requestChangeWindNextHoleRepeat][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou trocar vento dos proximos holes repeat no jogo na sala[NUMERO=" + (_session.m_pi.mi.sala_numero) + "], mas ele nao esta em nenhuma sala. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        1, 0x5902301));
                }

                r.requestChangeWindNextHoleRepeat(_session, _packet);



            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestChangeWindNextHoleRepeat][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestEnterRoomGrandPrix(Player _session, packet _packet)
        {
            //

            PangyaBinaryWriter p = new PangyaBinaryWriter();

            try
            {

                uint _typeid_gp = _packet.ReadUInt32();


                // Flag Server
                uFlag flag = _session.m_pi.block_flag.m_flag;

                // Player não pode criar ou entrar em sala Grand Prix
                if (flag.all_game)
                {
                    throw new exception("[Channel::requestEnterRoomGrandPrix][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou criar ou entrar sala Grand Prix[TYPEID=" + (_typeid_gp) + "], mas ele nao pode criar ou entrar nenhuma sala. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        0x6700003, 0x6700003));
                }

                if (flag.grand_prix)
                {
                    throw new exception("[Channel::requestEnterRoomGrandPrix][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou criar ou entrar sala Grand Prix[TYPEID=" + (_typeid_gp) + ", TIPO=" + (RoomInfo.ROOM_INFO_TYPE.GRAND_PRIX) + "], mas ele nao pode criar Grand Prix ou entrar(jogar). Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        0x6700004, 0x6700004));
                }

                // Verifica as regras do Grand Prix
                var gp = sIff.getInstance().findGrandPrixData(_typeid_gp);

                // Verifica se o Grand Prix existe no server e se ele está ativado
                if (gp == null || !gp.Active)
                {
                    throw new exception("[Channel::requestEnterRoomGrandPrix][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Canal[ID=" + ((ushort)m_ci.id) + "] tentou entrar na sala Grand Prix[TYPEID=" + (_typeid_gp) + "] mas nao existe esse grand prix no IFF_STRUCT do server. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        0x6700001, 0x6700001));
                }

                // Verifica se o Grand Prix está na hora em que pode entrar
                SYSTEMTIME local = new SYSTEMTIME();

                local.CreateTime();

                local.Day = local.DayOfWeek = local.Month = local.Year = 0;

                // Adiciona 1 dia para o start se a hora for >= 23 do open e <= 1 a hora do start
                if (gp.Open.Hour >= 23 && gp.Start.Hour <= 1)
                {
                    gp.Start.Day = 1;
                }

                if ((gp.Open.IsEmpty && UtilTime.GetHourDiff(local, gp.Open) < 0L) || (gp.Start.IsEmpty && UtilTime.GetHourDiff(local, gp.Start) > 0L))
                {
                    throw new exception("[Channel::requestEnterRoomGrandPrix][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Canal[ID=" + ((ushort)m_ci.id) + "] tentou entrar na sala Grand Prix[TYPEID=" + (gp.ID) + ", OPEN={HORA=" + (gp.Open.Hour) + ", MIN=" + (gp.Open.Minute) + "}, START={HORA=" + (gp.Start.Hour) + ", MIN=" + (gp.Start.Minute) + "}] mas ainda nao esta na hora[HORA=" + (local.Hour) + ", MIN=" + (local.Minute) + "] em que pode entrar na sala. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        0x670000C, 0x670000C));
                }

                // Verifica level
                if (_session.m_pi.mi.level < gp.MinLevel || (gp.MaxLevel > 0u && _session.m_pi.mi.level > gp.MaxLevel))
                {
                    throw new exception("[Channel::requestEnterRoomGrandPrix][Error] PLAYER [UID=" + (_session.m_pi.uid) + ", LEVEL=" + (_session.m_pi.mi.level) + "] Canal[ID=" + ((ushort)m_ci.id) + "] tentou entrar na sala Grand Prix[TYPEID=" + (gp.ID) + ", LVL_MIN=" + (gp.MinLevel) + ", LVL_MAX=" + (gp.MaxLevel) + "] mas ele nao tem o level necessario para entrar na sala. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        0x6700006, 0x6700006));
                }

                // Verifica condition equiped item
                var gp_condition = sIff.getInstance().findGrandPrixConditionEquip(gp.TypeID_Link);

                if (gp_condition != null && !_session.m_pi.checkEquipedItem(gp_condition.item_typeid))
                {
                    throw new exception("[Channel::requestEnterRoomGrandPrix][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Canal[ID=" + ((ushort)m_ci.id) + "] tentou entrar na sala Grand Prix[TYPEID=" + (gp.ID) + "] mas ele nao esta equipado com o item[TYPEID=" + (gp_condition.item_typeid) + "]. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        0x6700007, 0x6700007));
                }

                // Verifica Avg. Score
                if (_session.m_pi.ui.getMediaScore() < gp.condition[0] || (gp.condition[1] > 0u && _session.m_pi.ui.getMediaScore() > gp.condition[1]))
                {
                    throw new exception("[Channel::requestEnterRoomGrandPrix][Error] PLAYER [UID=" + (_session.m_pi.uid) + ", AVG_SCORE=" + (_session.m_pi.ui.getMediaScore()) + "] Canal[ID=" + ((ushort)m_ci.id) + "] tentou entrar na sala Grand Prix[TYPEID=" + (gp.ID) + ", AVG_MIN=" + (gp.condition[0]) + ", AVG_MAX=" + (gp.condition[1]) + "] mas ele nao tem o Avg. Score necessario para entrar na sala. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        0x6700008, 0x6700008));
                }

                // Verifica se ele tem o Grand Prix Ticket necessário
                if (gp.ticket.qntd > 0u && gp.ticket._typeid > 0)
                {

                    var pWi = _session.m_pi.findWarehouseItemByTypeid(gp.ticket._typeid);

                    if (pWi == null || (ushort)pWi.STDA_C_ITEM_QNTD < gp.ticket.qntd)
                    {
                        throw new exception("[Channel::requestEnterRoomGrandPrix][Error] PLAYER [UID=" + (_session.m_pi.uid) + ", TICKET_QNTD=" + ((pWi == null ? 0 : pWi.STDA_C_ITEM_QNTD)) + "] Canal[ID=" + ((ushort)m_ci.id) + "] tentou entrar na sala Grand Prix[TYPEID=" + (gp.ID) + "] mas ele nao tem ticket[TYPEID=" + (gp.ticket._typeid) + ", QNTD=" + (gp.ticket.qntd) + "] suficiente. Hacker ou Bug ", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                            0x6700009, 0x6700009));
                    }

                }

                // Verifica se ele já concluiu outro Grand Prix para poder jogar esse
                if (gp.Lock_YN > 0u
                    && gp.Clear_GP_TypeID != 0u
                    && _session.m_pi.findGrandPrixClear(gp.Clear_GP_TypeID) == null)
                {
                    throw new exception("[Channel::requestEnterRoomGrandPrix][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Canal[ID=" + ((ushort)m_ci.id) + "] tentou entrar na sala Grand Prix[TYPEID=" + (gp.ID) + "] mas nao concluiu o Grand Prix[TYPEID=" + (gp.Clear_GP_TypeID) + "]. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        0x670000A, 0x670000A));
                }

                // Verifica se o tipo do Grand Prix está ativo no server
                if (gp.TypeGP > 0u && !sgs.gs.getInstance().getInfo().rate.checkBitGrandPrixEvent((int)gp.TypeGP))
                {
                    throw new exception("[Channel::requestEnterRoomGrandPrix][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Canal[ID=" + ((ushort)m_ci.id) + "] tentou entrar na sala Grand Prix[TYPEID=" + (gp.ID) + ", TYPE=" + (gp.TypeGP) + "] mas esse type nao esta ativo no server[GP_TYPE=" + (sgs.gs.getInstance().getInfo().rate.grand_prix_event) + "]. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        0x670000B, 0x670000B));
                }

                // Room variable
                Room r = null;

                // Grand Prix Rookie é instância cria uma sala separada para cada 1, o resto do Grand Prix considera como uma sala normal
                // Cria uma nova sala se for GP Rookie ou não tiver nenhum sala criada do GP _typeid
                if (sIff.getInstance().isGrandPrixNormal(gp.ID)
                    && sIff.getInstance().getGrandPrixAbaType(gp.ID) == GrandPrixData.GP_ABA.ROOKIE
                    || (r = m_rm.findRoomGrandPrix(gp.ID)) == null)
                {
                    // Sala Beginner, Sempre cria uma nova ela é instancia
                    var ri = new RoomInfoEx
                    {
                        time_vs = 0,
                        time_30s = 0,
                        max_player = 30,
                        tipo = (byte)RoomInfo.ROOM_INFO_TYPE.GRAND_PRIX,
                        qntd_hole = gp.course_info.Qntd_hole,
                        course = (RoomInfo.ROOM_INFO_COURSE)gp.course_info.Course,
                        modo = (byte)gp.course_info.Modo
                    };
                    ri.special_flag_mod.natural = gp.flag.Natural_Mode;
                    ri.special_flag_mod.short_game = gp.flag.Shot_Mode; 
                    ri.typeid_artefatic = gp.rule; 
                    ri.grand_prix.active = 1;
                    ri.grand_prix.dados_typeid = gp.ID;
                    ri.grand_prix.rank_typeid = gp.TypeID_Link;
                    ri.grand_prix.tempo = (uint)(gp.TimeHole * 1000);
                    ri.name = gp.Name;
                    // Fim de init Grand Prix Room Dados

                    try
                    {

                        // Verifica se o player foi convidado em outra sala
                        // e tira o convite dele
                        deleteInviteTimeResquestByInvited(_session);

                        r = m_rm.makeRoomGrandPrix(m_ci.id,
                            ri, _session, gp, 1);

                        if (r == null)
                        {
                            throw new exception("[Channel::requestEnterRoomGrandPrix][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Canal[ID=" + ((ushort)m_ci.id) + "] tentou entrar na sala Grand Prix[TYPEID=" + (_typeid_gp) + "] mas nao conseguiu criar a sala. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                0x6700002, 0x6700002));
                        }

                        // Att PlayerCanalInfo
                        updatePlayerInfo(_session);

                        r.sendUpdate();

                        r.sendMake(_session);

                        r.sendPlayerInfo(_session, 0);

                        sendUpdateRoomInfo(r.getInfo(), 1);

                        if (r.getInfo().getTipo() != RoomInfo.ROOM_INFO_TYPE.PRACTICE && r.getInfo().getTipo() != RoomInfo.ROOM_INFO_TYPE.GRAND_ZODIAC_PRACTICE)
                        {
                            sendUpdatePlayerInfo(_session, 3);
                        }


                        // Libera a sala
                        if (r != null)
                            m_rm.addRoom(r);

                    }
                    catch
                    {
                        // UNREFERENCED_PARAMETER(e);

                        if (r != null)
                        {
                            m_rm.unlockRoom(r);
                        }

                        throw; // Relança a exception
                    }

                }
                else
                {

                    try
                    {

                        // Entra na sala
                        if (!r.isFull())
                        { 
                            // Verifica se o player foi convidado em outra sala
                            // e tira o convite dele
                            deleteInviteTimeResquestByInvited(_session);

                            // Entra na sala
                            r.enter(_session);

                        }
                        else
                        {
                            throw new exception("[Channel::requestEnterRoomGrandPrix][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou entrar na sala[NUMERO=" + (r.getNumero()) + "], mas a sala esta cheia.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                3, 0x6700005));
                        }

                        // Att PlayerCanalInfo
                        updatePlayerInfo(_session);

                        r.sendUpdate();

                        r.sendMake(_session);

                        r.sendPlayerInfo(_session, 0);

                        r.sendPlayerInfo(_session, 1);

                        sendUpdateRoomInfo(r.getInfo(), 3);

                        if (r.getInfo().getTipo() != RoomInfo.ROOM_INFO_TYPE.PRACTICE && r.getInfo().getTipo() != RoomInfo.ROOM_INFO_TYPE.GRAND_ZODIAC_PRACTICE)
                        {
                            sendUpdatePlayerInfo(_session, 3);
                        }


                        // Libera a sala
                        if (r != null)
                            m_rm.addRoom(r);
                    }
                    catch
                    {
                        if (r != null)
                        {
                            m_rm.unlockRoom(r);
                        }

                        throw; // Relança a exception
                    }
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestEnterRoomGrandPrix][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x253);

                p.WriteUInt32(ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) == STDA_ERROR_TYPE.CHANNEL ? ExceptionError.STDA_SYSTEM_ERROR_DECODE(e.getCodeError()) : 0x6700000);

                packet_func.session_send(p,
                    _session, 1);
            }
        }



        public bool hasGrandZodiacRoomAtivo(RoomInfo.ROOM_INFO_TYPE tipoDesejado)
        {
            lock (m_rm)
            {
                var roms = m_rm.getAllRoomsGrandZodiacEvent(); 

                return roms.Any(r=> r.getInfo().getTipo() == tipoDesejado);
            }
        }

        public void requestExitRoomGrandPrix(Player _session, packet _packet)
        {
            //

            PangyaBinaryWriter p = new PangyaBinaryWriter();

            try
            {
                // 0 sai da sala sem está em jogo, 1 sai da sala em jogo
                byte opt = _packet.ReadUInt8();

                short value = _packet.ReadInt16(); // aqui sempre peguei -1

                var key = _packet.ReadBytes(16);

                // Esse precisa do pacote para sair da sala
                leaveRoomGrandPrix(_session, 1);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestExitRoomGrandPrix][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestPlayerReportChatGame(Player _session, packet _packet)
        {
            //

            try
            {

                var r = m_rm.findRoom((short)_session.m_pi.mi.sala_numero);

                if (r == null)
                {
                    throw new exception("[Channel::requestPlayerReportChatGame][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] nao esta em nenhum sala[NUMERO=" + (_session.m_pi.mi.sala_numero) + "] para reportar o char da sala. Hacker ou Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        0x580200, 0));
                }

                r.requestPlayerReportChatGame(_session, _packet);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestPlayerReportChatGame][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if (ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) != STDA_ERROR_TYPE.ROOM)
                {
                    throw;
                }
            }
        }

        public void requestExecCCGVisible(Player _session, packet _packet)
        {
            //

            try
            {

                ushort visible = _packet.ReadUInt16();


                var r = m_rm.findRoom((short)_session.m_pi.mi.sala_numero);

                if (r == null && _session.m_pi.mi.sala_numero != ushort.MaxValue)
                    throw new exception("[Channel::requestExecCCGVisible][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou executar o comando visible, mas nao encontrou a sala[NUMERO=" + (_session.m_pi.mi.sala_numero) + "] que esta nos dados dele. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                           10, 0x5700100));

                _session.m_gi.visible = _session.m_pi.mi.state_flag.visible = (byte)(visible & 1);

                Debug.WriteLine(_session.m_pi.mi.state_flag.ToString());

                updatePlayerInfo(_session);

                if (r != null)
                    r.updatePlayerInfo(_session);

                // Log
                _smp.message_pool.getInstance().push(new message("[Channel::requestExecCCGVisible][Sucess] PLAYER [UID=" + (_session.m_pi.uid) + "] GM Visible[State: " + ((visible & 1) != 0 ? "ON" : "OFF") + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                // UPDATE ON GAME
                sendUpdatePlayerInfo(_session, 3);

                if (r != null)
                {
                    r.sendPlayerInfo(_session, 3);
                }
            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestExecCCGVisible][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                throw;
            }
        }

        public void requestExecCCGChangeWindVersus(Player _session, packet _packet)
        {
            //

            try
            {


                var r = m_rm.findRoom((short)_session.m_pi.mi.sala_numero);

                if (r == null)
                {
                    throw new exception("[Channel::requestExecCCGChangeWindVersus][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou executar o comando de troca de vento do versus, mas ele nao esta em nenhuma sala[NUMERO=" + (_session.m_pi.mi.sala_numero) + "]. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        1, 0x5700100));
                }

                r.requestExecCCGChangeWindVersus(_session, _packet);



            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestExecCCGChangeWindVersus][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                throw;
            }
        }

        public void requestExecCCGChangeWeather(Player _session, packet _packet)
        {
            //

            try
            {


                var r = m_rm.findRoom((short)_session.m_pi.mi.sala_numero);

                if (r == null)
                {
                    throw new exception("[Channel::requestExecCCGChangeWeather][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou executar o comando de troca de tempo(weather) da sala[NUMERO=" + (_session.m_pi.mi.sala_numero) + "], mas ele nao esta em nenhuma sala. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        2, 0x5700100));
                }

                r.requestExecCCGChangeWeather(_session, _packet);



            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestExecCCGChangeWeather][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                throw;
            }
        }

        public void requestExecCCGGoldenBell(Player _session, packet _packet)
        {
            //

            try
            {

                var r = m_rm.findRoom((short)_session.m_pi.mi.sala_numero);

                if (r == null)
                {
                    throw new exception("[Channel::requestExecCCGGoldenBell][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou executar o comando goldenbell na sala[NUMERO=" + (_session.m_pi.mi.sala_numero) + "], mas ele nao esta em nenhum sala. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        7, 0x5700100));
                }

                r.requestExecCCGGoldenBell(_session, _packet);



            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestExecCCGGoldenBell][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                throw;
            }
        }

        public void requestExecCCGIdentity(Player _session, packet _packet)
        {
            //

            try
            {

                uCapability cap = new uCapability(_packet.ReadInt32());
                string nick = _packet.ReadString();

                if (nick.Length == 0)
                {
                    throw new exception("[Channel::requestExecCCGIdentity][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou executar o comando identity, mas o nick is empty. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        11, 0x5700100));
                }

                if (string.CompareOrdinal(nick, _session.m_pi.nickname) != 0)
                {
                    throw new exception("[Channel::requestExecCCGIdentity][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou executar o comando identity, mas o nick[NICK=" + nick + "] nao bate com o do PLAYER [NICK=" + (_session.m_pi.nickname) + "]. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        12, 0x5700100));
                }

                if (_session.m_pi.m_cap.gm_normal == false && !_session.m_pi.m_cap.game_master)
                {
                    throw new exception("[Channel::requestExecCCGIdentity][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou executar o comando identity, mas ele nao é gm e nunca foi. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        13, 0x5700100));
                }


                var r = m_rm.findRoom((short)_session.m_pi.mi.sala_numero);

                if (r == null && _session.m_pi.mi.sala_numero != ushort.MaxValue)
                {
                    throw new exception("[Channel::requestExecCCGIdentity][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou executar o comando identity, mas nao encontrou a sala[NUMERO=" + (_session.m_pi.mi.sala_numero) + "] que esta nos dados dele. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        14, 0x5700100));
                }

                CmdVerifyCapability cmd_cap = new CmdVerifyCapability(_session.m_pi.uid);

                snmdb.NormalManagerDB.getInstance().add(0, cmd_cap, SQLDBResponse, this);

                if (cmd_cap.getException().getCodeError() != 0)
                {
                    throw cmd_cap.getException();
                }

                if (!cmd_cap.IsValid())
                {
                    throw new exception($"[Channel::requestExecCCGIdentity][Error] PLAYER [UID={_session.m_pi.uid}] tentou voltar pra GM/Admin mas não tem permissão no banco.",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 15, 0x5700100));
                }

                PangyaBinaryWriter p = new PangyaBinaryWriter();

                if (cap.ulCapability == -1)
                {
                    // player está tentando voltar a ser GM novamente

                    // Valta para o GM
                    if (_session.m_pi.m_cap.gm_normal)
                    {

                        _session.m_pi.m_cap.game_master = true;
                        _session.m_pi.m_cap.title_gm = true;

                        _session.m_pi.m_cap.gm_normal = false;

                        updatePlayerInfo(_session);

                        if (r != null)
                        {
                            r.updatePlayerInfo(_session);
                        }

                        // Log
                        _smp.message_pool.getInstance().push(new message("[Channel::requestExecCCGIdentity][Sucess] PLAYER [UID=" + (_session.m_pi.uid) + "] trocou a capacidade dele, para GM Total(Admin)", type_msg.CL_FILE_LOG_AND_CONSOLE));

                        // UPDATE ON GAME 
                        packet_func.session_send(packet_func.pacote09A(_session.m_pi.m_cap.ulCapability),
                            _session, 1);

                        sendUpdatePlayerInfo(_session, 3);

                        if (r != null)
                        {
                            r.sendPlayerInfo(_session, 3);
                        }
                    }

                }
                else
                {

                    // [GM] Player Normal
                    if (cap.gm_normal)
                    {

                        _session.m_pi.m_cap.game_master = false;
                        _session.m_pi.m_cap.title_gm = false;

                        _session.m_pi.m_cap.gm_normal = true;

                        updatePlayerInfo(_session);

                        if (r != null)
                        {
                            r.updatePlayerInfo(_session);
                        }

                        // Log
                        _smp.message_pool.getInstance().push(new message("[Channel::requestExecCCGIdentity][Sucess] PLAYER [UID=" + (_session.m_pi.uid) + "] trocou a capacidade dele, para GM Normal(user normal)", type_msg.CL_FILE_LOG_AND_CONSOLE));

                        packet_func.session_send(packet_func.pacote09A(_session.m_pi.m_cap.ulCapability),
                            _session, 1);

                        sendUpdatePlayerInfo(_session, 3);

                        if (r != null)
                        {
                            r.sendPlayerInfo(_session, 3);
                        }
                    }
                }



            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestExecCCGIdentity][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                throw;
            }
        }

        public void requestExecCCGKick(Player _session, packet _packet)
        {
            try
            {

                uint oid = _packet.ReadUInt32();
                byte force = _packet.ReadUInt8(); // Força o kick do player

                var s = findSessionByOID(oid);

                if (s == null)
                {
                    throw new exception("[Channel::requestExecCCGKick][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou executar o comando /kick mas nao encontrou o PLAYER [OID=" + (oid) + "] do oid fornecido. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER,
                        8, 0));
                }

                Room r = m_rm.findRoom((short)s.m_pi.mi.sala_numero);

                if (r == null)
                {
                    throw new exception("[Channel::requestExecCCGKick][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou chutar um PLAYER [UID=" + (s.m_pi.uid) + "] da sala[NUMERO=" + (s.m_pi.mi.sala_numero) + "], mas sala nao existe. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        10, 0));
                }

                // Log
                _smp.message_pool.getInstance().push(new message("[Channel::requestExecCCGKick][Sucess] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] kikou o PLAYER [UID=" + (s.m_pi.uid) + ", NICKNAME=" + (s.m_pi.nickname) + "] FORCE[QUIT=" + ((ushort)force) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Chuta da sala se o Player estiver em uma
                kickPlayerRoom(s, force);



            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestExecCCGKick][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                throw;
            }
        }

        public void requestExecCCGDestroy(Player _session, packet _packet)
        {
            //

            try
            {





                if (_session.m_pi.m_cap.game_master)
                {
                    short sala_numero = _packet.ReadInt16();

                    var r = m_rm.findRoom(sala_numero);

                    if (r == null)
                    {
                        throw new exception("[Channel::requestExecCCGDestroy][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou executar o comando destroy, para destruir a sala[NUMERO=" + (sala_numero) + "], mas a sala nao existe.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                            16, 0x5700100));
                    }

                    // Kick All of Room And Automatic Room Destroyed
                    var v_sessions = r.getSessions();

                    if (v_sessions.empty())
                    {

                        RoomInfoEx ri = r.getInfo();

                        m_rm.destroyRoom(r);

                        sendUpdateRoomInfo(ri, 2);

                    }
                    else
                    {

                        // Kick all player e destroi a sala
                        foreach (var el in v_sessions)
                        {
                            kickPlayerRoom(el, 0);
                        }
                    }

                    // Log
                    _smp.message_pool.getInstance().push(new message("[Channel::requestExecCCGDestroy][Sucess] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] destruiu a sala[NUMERO=" + (sala_numero) + "] no canal[NOME=" + (m_ci.name) + "].", type_msg.CL_FILE_LOG_AND_CONSOLE));

                }
                else
                {
                    throw new exception("[Channel::requestExecCCGDestroy][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] nao tem a capacidade de um GM. hacker ou bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        17, 0x5700101));
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestExecCCGDestroy][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                PangyaBinaryWriter p = new PangyaBinaryWriter((ushort)0x40); // Msg to Chat of player

                p.WriteByte(7); // Notice

                p.WriteString(_session.m_pi.nickname);
                p.WriteString("Nao conseguiu executar o comando.");

                packet_func.session_send(p,
                    _session, 1);
            }
        }

        public void requestEnterMyRoom(Player _session, packet _packet)
        {
            try
            {
                // @@!! Ajeitar para pegar a função estática da sala que initializa o Player Room Info
                PlayerRoomInfoEx pri = new PlayerRoomInfoEx
                {
                    // Player Room Info Init
                    oid = _session.m_oid,
                    nickname = _session.m_pi.nickname,
                    guild_name = _session.m_pi.gi.name,
                    position = 0,   // posição na sala
                    capability = _session.m_pi.m_cap,
                    title = _session.m_pi.ue.m_title
                };

                if (_session.m_pi.ei.char_info != null)
                    pri.char_typeid = _session.m_pi.ei.char_info._typeid;

                pri.skin = _session.m_pi.ue.skin_typeid;
                pri.skin[4] = 0;//4 = cutin, Aqui tem que ser zero, se for outro valor não mostra a imagem do character equipado

                pri.state_flag.master = 1;
                pri.state_flag.ready = 1;   // Sempre está pronto(ready) o master

                pri.state_flag.sexo = _session.m_pi.mi.sexo;

                // Só faz calculo de Quita rate depois que o player
                // estiver no level Beginner E e jogado 50 games
                if (_session.m_pi.mi.level >= 6 && _session.m_pi.ui.jogado >= 50)
                {
                    float rate = _session.m_pi.ui.getQuitRate();

                    if (rate < GOOD_PLAYER_ICON)
                        pri.state_flag.azinha = 1;
                    else if (rate >= QUITER_ICON_1 && rate < QUITER_ICON_2)
                        pri.state_flag.quiter_1 = 1;
                    else if (rate >= QUITER_ICON_2)
                        pri.state_flag.quiter_2 = 1;
                }

                pri.level = _session.m_pi.mi.level;

                if (_session.m_pi.ei.char_info != null && _session.m_pi.ui.getQuitRate() < GOOD_PLAYER_ICON)
                    pri.icon_angel = _session.m_pi.ei.char_info.AngelEquiped();
                else
                    pri.icon_angel = 0;

                pri.place = new PlayerPlace(0x0A); // 0x0A dec"10" _session.m_pi.place
                pri.guild_uid = _session.m_pi.gi.uid;
                pri.guild_mark_img = _session.m_pi.gi.mark_emblem;
                pri.guild_mark_index = _session.m_pi.gi.index_mark_emblem;
                pri.uid = _session.m_pi.uid;
                pri.state_action.state_lounge = _session.m_pi.state_lounge;
                pri.state_action.state = _session.m_pi.state;
                pri.location = new PlayerRoomInfo.stLocation() { x = _session.m_pi.location.x, z = _session.m_pi.location.z, r = _session.m_pi.location.r };
                pri.shop = new PlayerRoomInfo.PersonShop();

                if (_session.m_pi.ei.mascot_info != null)
                    pri.mascot_typeid = _session.m_pi.ei.mascot_info._typeid;

                pri.flag_item_boost = _session.m_pi.checkEquipedItemBoost();
                pri.channeling_flag = 0;
                //pri.id_NT não estou usando ainda
                //pri.ucUnknown106
                pri.convidado = 0;  // Flag Convidado, [Não sei bem por que os que entra na sala normal tem valor igual aqui, já que é type de convidado waiting], Valor constante da sala para os players(ACHO)
                pri.avg_score = _session.m_pi.ui.getMediaScore();
                //pri.ucUnknown3 


                var p = new PangyaBinaryWriter(); // Character Equipado

                p.init_plain(0x168);  // Character Equipado

                p.WriteBytes(pri.ToArrayEx());

                packet_func.session_send(p, _session);

                p.init_plain(0x12D); // Itens do Myroom, Mala, Email, sofa, teto chao, e poster, "NESSA SEASON, SÓ USA POSTER"

                p.WriteUInt32(1); // Option, tem outras opt

                p.WriteUInt16((ushort)_session.m_pi.v_mri.Count);

                for (var i = 0; i < _session.m_pi.v_mri.Count; ++i)
                    p.WriteBytes(_session.m_pi.v_mri[i].ToArray());

                packet_func.session_send(p, _session);
            }
            catch (exception e)
            {
                _smp.message_pool.getInstance().push(new message("[Channel::requestEnterMyRoom][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestChangePlayerItemMyRoom(Player session, packet packet)
        {
            byte type = 0;
            int error = 4;

            try
            {
                type = packet.ReadUInt8();

                switch (type)
                {
                    case 0:
                        error = HandleUpdateCharacterParts(session, packet);
                        break;

                    case 1:
                        error = HandleUpdateCaddie(session, packet);
                        break;

                    case 2:
                        error = HandleUpdateUseItems(session, packet);
                        break;

                    case 3:
                        error = HandleUpdateClubAndBall(session, packet);
                        break;

                    case 4:
                        error = HandleUpdateSkins(session, packet);
                        break;

                    case 5:
                        error = HandleUpdateCharacter(session, packet);
                        break;

                    case 8:
                        error = HandleUpdateMascot(session, packet);
                        break;

                    case 9:
                        error = HandleUpdateCutin(session, packet);
                        break;

                    case 10:
                        error = HandleUpdatePoster(session, packet);
                        break;

                    default:
                        error = 1;
                        break;
                }

                packet_func.session_send(
                    packet_func.pacote06B(session.m_pi, type, error),
                    session,
                    1);

                updatePlayerInfo(session);
            }
            catch (exception e)
            {
                packet_func.session_send(
                    packet_func.pacote06B(session.m_pi, type, 1),
                    session,
                    1);

                _smp.message_pool.getInstance().push(
                    new message(
                        "[Channel::requestChangePlayerItemMyRoom][ErrorSystem] " +
                        e.getFullMessageError(),
                        type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        #region Handle Update Item My Room
        private int HandleUpdateCharacterParts(Player session, packet packet)
        {
            int error = 4;

            CharacterInfo ci = new CharacterInfo();
           
            ci.ToRead(packet);

            var pCe = session.m_pi.findCharacterById(ci.id);

            if (ci.id == 0 || pCe == null)
                return (ci.id == 0) ? 1 : 2;

            // Checks Parts Equiped
           session.checkCharacterEquipedPart(ci);

            // Check AuxPart Equiped
            session.checkCharacterEquipedAuxPart(ci);  

            session.m_pi.ue.character_id = ci.id;
            session.m_pi.ei.char_info = ci; 
            snmdb.NormalManagerDB.getInstance().add(5, new CmdUpdateCharacterAllPartEquiped(session.getUID(), ci), SQLDBResponse, this);
            //salvar por ultimo
            session.m_pi.mp_ce[ci.id] = ci;
            return error;
        }

        private int HandleUpdateCharacter(Player session, packet packet)
        {
            int error = 4;
            int charId = packet.ReadInt32();

            var pCe = session.m_pi.findCharacterById(charId);

            if (charId == 0 || pCe == null)
                return (charId == 0) ? 1 : 2;

            session.m_pi.ei.char_info = pCe;
            session.m_pi.ue.character_id = charId;

            snmdb.NormalManagerDB.getInstance().add(
                0,
                new CmdUpdateCharacterEquiped(session.m_pi.uid, charId),
                SQLDBResponse,
                this);

            return error;
        }

        private int HandleUpdateCaddie(Player session, packet packet)
        {
            int error = 4;
            int itemId = packet.ReadInt32();

            if (itemId != 0)
            {
                var caddie = session.m_pi.findCaddieById(itemId);

                if (caddie == null)
                    return 2;

                session.m_pi.ei.cad_info = caddie;
                session.m_pi.ue.caddie_id = itemId;

                if (session.checkCaddieEquiped(session.m_pi.ue))
                    itemId = session.m_pi.ue.caddie_id;
            }
            else
            {
                session.m_pi.ue.caddie_id = 0;
            }

            snmdb.NormalManagerDB.getInstance().add(
                0,
                new CmdUpdateCaddieEquiped(session.m_pi.uid, itemId),
                SQLDBResponse,
                this);

            return error;
        }

        private int HandleUpdateClubAndBall(Player session, packet packet)
        {
            int error = 4;

            // BALL
            int ballTypeId = packet.ReadInt32();
            var ball = session.m_pi.findWarehouseItemByTypeid((uint)ballTypeId);

            if (ball != null)
            {
                session.m_pi.ei.comet = ball;
                session.m_pi.ue.ball_typeid = (uint)ballTypeId;

                if (session.checkBallEquiped(session.m_pi.ue))
                    ballTypeId = (int)session.m_pi.ue.ball_typeid;
            }
            else
            {
                session.m_pi.ue.ball_typeid = 0;
            }

            snmdb.NormalManagerDB.getInstance().add(
                0,
                new CmdUpdateBallEquiped(session.m_pi.uid, (uint)ballTypeId),
                SQLDBResponse,
                this);

            // CLUBSET
            int clubId = packet.ReadInt32();
            var club = session.m_pi.findWarehouseItemById(clubId);

            if (club == null)
                return 2;

            session.m_pi.ei.clubset = club;
            session.m_pi.ue.clubset_id = clubId;

            if (session.checkClubSetEquiped(session.m_pi.ue))
                clubId = session.m_pi.ue.clubset_id;

            snmdb.NormalManagerDB.getInstance().add(
                0,
                new CmdUpdateClubsetEquiped(session.m_pi.uid, clubId),
                SQLDBResponse,
                this);

            return error;
        }

        private int HandleUpdateUseItems(Player session, packet packet)
        {
            int error = 4;

            UserEquip ue = new UserEquip();
            ue.item_slot = packet.ReadUInt32((uint)session.m_pi.ue.item_slot.Length);

            session.m_pi.ue.item_slot = ue.item_slot;

            snmdb.NormalManagerDB.getInstance().add(
                25,
                new CmdUpdateItemSlot(session.m_pi.uid, ue.item_slot),
                SQLDBResponse,
                this);

            return error;
        }

        private int HandleUpdateSkins(Player session, packet packet)
        {
            int error = 4;

            for (int i = 0; i < session.m_pi.ue.skin_typeid.Length; i++)
            {
                int id = packet.ReadInt32();

                if (id == 0)
                {
                    session.m_pi.ue.skin_id[i] = 0;
                    session.m_pi.ue.skin_typeid[i] = 0;
                    continue;
                }

                var skin = session.m_pi.findWarehouseItemByTypeid((uint)id);

                if (skin == null)
                    return 2;

                session.m_pi.ue.skin_id[i] = (uint)skin.id;
                session.m_pi.ue.skin_typeid[i] = skin._typeid;
            }

            snmdb.NormalManagerDB.getInstance().add(
                0,
                new CmdUpdateSkinEquiped(session.m_pi.uid, session.m_pi.ue),
                SQLDBResponse,
                this);

            return error;
        }

        private int HandleUpdateMascot(Player session, packet packet)
        {
            int error = 4;
            int id = packet.ReadInt32();

            if (id != 0)
            {
                var mascot = session.m_pi.findMascotById(id);
                if (mascot == null)
                    return 2;

                session.m_pi.ei.mascot_info = mascot;
                session.m_pi.ue.mascot_id = id;
            }
            else
            {
                session.m_pi.ue.mascot_id = 0;
            }

            snmdb.NormalManagerDB.getInstance().add(
                0,
                new CmdUpdateMascotEquiped(session.m_pi.uid, id),
                SQLDBResponse,
                this);

            return error;
        }

        private int HandleUpdateCutin(Player session, packet packet)
        {
            int error = 4;

            int charId = packet.ReadInt32();
            var pCe = session.m_pi.findCharacterById(charId);

            if (charId == 0)
                return 1; // Invalid Item Id

            if (pCe == null)
                return 2; // Not Found

            if (session.m_pi.ei.char_info == null)
                return 4; // No character equipped

            if (session.m_pi.ei.char_info.id != pCe.id)
                return 5; // Not the equipped character

            int[] cutins = packet.ReadInt32(session.m_pi.ei.char_info.cut_in.Length);

            for (int i = 0; i < cutins.Length; i++)
            {
                int cutinId = cutins[i];

                if (cutinId == 0)
                {
                    pCe.cut_in[i] = 0;
                    continue;
                }

                var pWi = session.m_pi.findWarehouseItemById(cutinId);

                if (pWi == null ||
                    sIff.getInstance().getItemGroupIdentify(pWi._typeid) != IFF_GROUP.SKIN)
                    return 3; // Item Type Wrong

                pCe.cut_in[i] = (uint)pWi.id;
            }

            // Validação final
            session.checkCharacterEquipedCutin(pCe);


            session.m_pi.ue.character_id = pCe.id;
            session.m_pi.ei.char_info = pCe;
            // Update DB
            snmdb.NormalManagerDB.getInstance().add(
                0,
                new CmdUpdateCharacterCutinEquiped(session.m_pi.uid, pCe),
                SQLDBResponse,
                this);
            //salvar por ultimo
            session.m_pi.mp_ce[pCe.id] = pCe;
            return error;
        }

        private int HandleUpdatePoster(Player session, packet packet)
        {
            int error = 4;

            for (int i = 0; i < session.m_pi.ue.poster.Length; i++)
            {
                int posterTypeId = packet.ReadInt32();

                if (posterTypeId == 0)
                {
                    session.m_pi.ue.poster[i] = 0;
                    continue;
                }

                var pMri = session.m_pi.findMyRoomItemByTypeid((uint)posterTypeId);

                if (pMri == null ||
                    sIff.getInstance().getItemGroupIdentify(pMri._typeid) != IFF_GROUP.FURNITURE)
                    return 2;

                session.m_pi.ue.poster[i] = (uint)posterTypeId;
            }

            if (session.checkPosterEquiped(session.m_pi.ue) || error == 4)
            {
                snmdb.NormalManagerDB.getInstance().add(
                    0,
                    new CmdUpdatePosterEquiped(session.m_pi.uid, session.m_pi.ue),
                    SQLDBResponse,
                    this);
            }

            return error;
        }

        #endregion

        public void requestOpenTicketReportScroll(Player _session, packet _packet)
        {
            PangyaBinaryWriter p = new PangyaBinaryWriter();

            try
            {
                int ticket_scroll_item_id = _packet.ReadInt32();
                int ticket_scroll_id = _packet.ReadInt32();

                ItemManager.openTicketReportScroll(_session,
                    ticket_scroll_item_id,
                    ticket_scroll_id, true);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestOpenTicketReportScroll][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Reposta Error;
                p.init_plain(0x11A);

                p.WriteInt32(-1); // Error
                p.WriteZeroByte(16); // Date

                packet_func.session_send(p,
                    _session, 1);
            }
        }

        public void requestChangeMascotMessage(Player _session, packet _packet)
        {
            PangyaBinaryWriter p = new PangyaBinaryWriter();

            try
            {

                int mascot_id = _packet.ReadInt32();
                string msg = _packet.ReadString();

                if (msg.Length == 0)
                {
                    throw new exception("[Channel::requestChangeMascotMessage][Error] PLAYER [UID=" + (_session.m_pi.uid) + "], tentou trocar a message[" + msg + "] do Mascot[ID=" + (mascot_id) + "], mas a message esta vazia. Hacker ou Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        0x6200100, 0));
                }

                if (msg.Length > 30)
                {
                    throw new exception("[Channel::requestChangeMascotMessage][Error] PLAYER [UID=" + (_session.m_pi.uid) + "], tentou trocar a message[" + msg + "] do Mascot[ID=" + (mascot_id) + "], mas o comprimento da message ultrapassa os 30 caracteres permitido. Hacker ou Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        0x6200101, 0));
                }

                if (string.IsNullOrEmpty(msg))
                    throw new exception("[Channel::requestChangeMascotMessage][Error] PLAYER[UID=" + (_session.m_pi.uid) + "] tentou contra o server[MESSAGE="
                            + msg + "], vazio. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 1, 1/*UNKNOWN ERROR*/));

                if (!Tools.Sanitize(msg))
                    throw new exception("[Channel::requestChangeMascotMessage][Error] PLAYER[UID=" + (_session.m_pi.uid) + "] tentou contra o server[MESSAGE="
                            + msg + "], tentativa de inject. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 1, 1/*UNKNOWN ERROR*/));

                var pMi = _session.m_pi.findMascotById(mascot_id);

                if (pMi == null)
                {
                    throw new exception("[Channel::requestChangeMascotMessage][Error] PLAYER [UID=" + (_session.m_pi.uid) + "], tentou trocar a message[" + msg + "] do Mascot[ID=" + (mascot_id) + "], mas ele nao tem esse mascot. Hacker ou Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        0x6200102, 0));
                }

                if (!sIff.getInstance().isLoad())
                {
                    sIff.getInstance().initilation();
                }

                var mascot = sIff.getInstance().findMascot(pMi._typeid);

                if (mascot == null || !(mascot.msg.active))
                {
                    throw new exception("[Channel::requestChangeMascotMessage][Error] PLAYER [UID=" + (_session.m_pi.uid) + "], tentou trocar a message[" + msg + "] do Mascot[TYPEID=" + (pMi._typeid) + " ID=" + (pMi.id) + "], mas nao existe ou nao esta ativado esse mascot no IFF_STRUCT do server. Hacker ou Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        0x6200103, 0));
                }

                if (!(mascot.msg.active))
                {
                    throw new exception("[Channel::requestChangeMascotMessage][Error] PLAYER [UID=" + (_session.m_pi.uid) + "], tentou trocar a message[" + msg + "] do Mascot[TYPEID=" + (pMi._typeid) + " ID=" + (pMi.id) + "], mas a message do mascot nao esta ativado. Hacker ou Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        0x6200104, 0));
                }

                try
                {

                    if (mascot.msg.change_price > 0)
                    {
                        _session.m_pi.consomePang(mascot.msg.change_price);
                    }

                }
                catch (exception e)
                {

                    _smp.message_pool.getInstance().push(new message("[Channel::requestChangeMascotMessage][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                    throw new exception("[Channel::requestChangeMascotMessage][Error] PLAYER [UID=" + (_session.m_pi.uid) + "], tentou trocar a message[" + msg + "] do Mascot[TYPEID=" + (pMi._typeid) + " ID=" + (pMi.id) + "], mas o player nao tem Pang[HAVE=" + (_session.m_pi.ui.pang) + ", REQ=" + (mascot.msg.change_price) + "] suficiente para trocar a mensagem do mascot. Hacker ou Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        0x6200105, 0));
                }

                // limpa e move message para o Mascot Info do player no server 
                pMi.message = msg;
                // Update Mascot info no DB
                snmdb.NormalManagerDB.getInstance().add(26,
                    new CmdUpdateMascotInfo(_session.m_pi.uid, pMi),
                    SQLDBResponse, this);

                // Update on GAME
                p.init_plain(0xE2);

                p.WriteByte(4); // Update Mascot Message

                p.WriteInt32(pMi.id); // Mascot ID

                p.WriteString(pMi.message);

                p.WriteUInt64(_session.m_pi.ui.pang);

                packet_func.session_send(p,
                    _session, 1);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestChangeMascotMessage][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Error
                p.init_plain(0xE2);

                p.WriteSByte(-1); // Option [Error]

                p.WriteInt32(-1); // Mascot ID

                p.WriteUInt16(0); // Msg Length

                p.WriteUInt64(_session.m_pi.ui.pang);

                packet_func.session_send(p,
                    _session, 1);
            }
        }

        public void requestPayCaddieHolyDay(Player _session, packet _packet)
        {
            //

            PangyaBinaryWriter p = new PangyaBinaryWriter();

            try
            {

                int caddie_id = _packet.ReadInt32();





                if (caddie_id <= 0)
                {
                    throw new exception("[Channel::requestPayCaddieHolyDay][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou pagar as ferias do Caddie[ID=" + (caddie_id) + "], mas o caddie_id é invalido. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        1, 0x6100101));
                }

                var pCi = _session.m_pi.findCaddieById(caddie_id);

                if (pCi == null)
                {
                    throw new exception("[Channel::requestPayCaddieHolyDay][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou pagar as ferias do Caddie[ID=" + (caddie_id) + "], mas o ele nao possui esse Caddie. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        2, 0x6100102));
                }

                var caddie = sIff.getInstance().findCaddie(pCi._typeid);

                if (caddie == null)
                {
                    throw new exception("[Channel::requestPayCaddieHolyDay][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou pagar as ferias do Caddie[TYPEID=" + (pCi._typeid) + ", ID=" + (pCi.id) + "], mas nao tem esse caddie no IFF_STRUCT do Server. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        3, 0x6100103));
                }

                if ((!caddie.Shop.flag_shop.IsCash && caddie.valor_mensal <= 0) || pCi.rent_flag != 2)
                {
                    throw new exception("[Channel::requestPayCaddieHolyDay][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou pagar as ferias do Caddie[TYPEID=" + (pCi._typeid) + ", ID=" + (pCi.id) + "], mas nao é um caddie valido para pagar as verias. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        4, 0x6100104));
                }

                if (caddie.valor_mensal > _session.m_pi.ui.pang)
                {
                    throw new exception("[Channel::requestPayCaddieHolyDay][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou pagar as ferias do Caddie[TYPEID=" + (pCi._typeid) + ", ID=" + (pCi.id) + "], mas o ele nao tem pangs suficiente[value=" + (_session.m_pi.ui.pang) + ", request=" + (caddie.valor_mensal) + "] para pagar as ferias do caddie. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        5, 0x6100105));
                }

                // UPDATE ON SERVER

                // Date
                var end_date_unix = UtilTime.GetSystemTimeAsUnix() + (30 * 24 * 3600); // TO STRING DATE

                // Convert para System Time novamente
                pCi.end_date = (UtilTime.UnixToSystemTime(end_date_unix));

                // Update Caddie End Date Unix
                pCi.updateEndDate();

                var end_dt = UtilTime.FormatDate(pCi.end_date);

                _session.m_pi.consomePang(caddie.valor_mensal);

                // UPDATE ON DB
                snmdb.NormalManagerDB.getInstance().add(20,
                    new CmdPayCaddieHolyDay(_session.m_pi.uid,
                        pCi.id, end_dt),
                    SQLDBResponse, this);

                // Verifica se o Caddie já tem um item update, por que se tiver, 
                // ele vai desequipar o caddie por que o player não relogou quando acabou o tempo do caddie
                var v_it = _session.m_pi.findUpdateItemByTypeidAndId(pCi._typeid, pCi.id);

                if (!v_it.empty())
                {

                    foreach (var el in v_it)
                    {
                        if (el.Value.type == UpdateItem.UI_TYPE.CADDIE)
                        {
                            // Tira esse Update Item do map
                            _session.m_pi.mp_ui.Remove(el.Key);
                        }
                    }

                }
                // ---- fim do verifica se o caddie no update item ----

                // Log
                _smp.message_pool.getInstance().push(new message("[PayCaddieHolyDay][Sucess] PLAYER [UID=" + (_session.m_pi.uid) + "] pagou as ferias do Caddie[TYPEID=" + (pCi._typeid) + ", ID=" + (pCi.id) + ", PRICE=" + (caddie.valor_mensal) + "] ate " + end_dt, type_msg.CL_FILE_LOG_AND_CONSOLE));

                // UPDATE ON GAME

                // Resposta do Paga Ferias do Caddie
                p.init_plain(0x93);

                p.WriteByte(2); // OK

                p.WriteInt32(pCi.id);
                p.WriteUInt64(_session.m_pi.ui.pang);

                packet_func.session_send(p,
                    _session, 1);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestPayCaddieHolyDay][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x93);

                p.WriteByte(1); // Error

                packet_func.session_send(p,
                    _session, 1);
            }
        }

        public void requestSetNoticeBeginCaddieHolyDay(Player _session, packet _packet)
        {
            //

            try
            {

                int caddie_id = _packet.ReadInt32();
                byte check = _packet.ReadUInt8();

                if (caddie_id <= 0)
                {
                    throw new exception("[Channel::requestSetNoticeBeginCaddieHolyDay][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou setar ou desetar o Aviso de ferias do Caddie[ID=" + (caddie_id) + "], mas o caddie_id is invalid. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        1, 0x6200101));
                }

                var pCi = _session.m_pi.findCaddieById(caddie_id);

                if (pCi == null)
                {
                    throw new exception("[Channel::requestSetNoticeBeginCaddieHolyDay][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou setar ou desetar o Aviso de ferias do Caddie[ID=" + (caddie_id) + "], mas ele nao tem esse caddie. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        2, 0x6200102));
                }

                var caddie = sIff.getInstance().findCaddie(pCi._typeid);

                if (caddie == null)
                {
                    throw new exception("[Channel::requestSetNoticeBeginCaddieHolyDay][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou setar ou desetar o Aviso de ferias do Caddie[TYPEID=" + (pCi._typeid) + ", ID=" + (pCi.id) + "], mas nao tem esse caddie no IFF_STRUCT do Server. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        3, 0x6200103));
                }

                // Tem caddie que não precisa, checar o end, mas o cliente manda mesmo assim, ai aqui da erro se eu não ignorar
                if ((!caddie.Shop.flag_shop.IsCash && caddie.valor_mensal <= 0) || pCi.rent_flag != 2)
                {
                }

                // UPDATE ON SERVER

                // Só Att se for diferente do que está no Server
                if (pCi.check_end != check)
                {

                    pCi.check_end = check;

                    // UPDATE ON DB
                    snmdb.NormalManagerDB.getInstance().add(21,
                        new CmdSetNoticeCaddieHolyDay(_session.m_pi.uid,
                            pCi.id, (ushort)pCi.check_end),
                        SQLDBResponse, this);
                }
            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[requestSetNoticeBeginCaddieHolyDay][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Não envia nada de resposta para o cliente, pelo que eu vi até aqui
            }
        }

        public void requestEnterShop(Player _session, packet _packet)
        {
            try
            {
                if (_session.m_pi.block_flag.m_flag.buy_and_gift_shop)
                    throw new exception("[Channel::requestEnterShop][Error] PLAYER [UID=" + (_session.m_pi.uid)
                            + "] tentou jogar no Papel Shop, mas ele nao pode. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 3, 0x790002));


                var p = new PangyaBinaryWriter((ushort)0x20E);

                p.Write(0);
                p.Write(0); // Não sei pode ACHO "ser Value acho, ou erro, pode ser dizendo que o shop esta bloqueado"

                packet_func.session_send(p, _session);
            }
            catch (exception e)
            {
                throw e;
            }
        }

        public void requestBuyItemShop(Player _session, packet _packet)
        {
            var p = new PangyaBinaryWriter();
            try
            {

                if (_session.m_pi.block_flag.m_flag.buy_and_gift_shop)
                {
                    throw new exception("[Channel::requestBuyItemShop][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou comprar item no shop, mas ele nao pode. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        1, 0x790001));
                }

                // Log Gastos de CP
                CPLog cp_log = new CPLog();

                cp_log.setType(CPLog.TYPE.BUY_SHOP);

                BuyItem bi = new BuyItem();
                byte option = _packet.ReadUInt8();
                ushort qntd = _packet.ReadUInt16();

                // Coupon
                stItem coupon = new stItem();
                string coupon_msg = "";
                ulong pang = 0Ul;
                ulong cookie = 0Ul;

                if (qntd > 0)
                {

                    stItem item = new stItem();
                    List<stItem> v_item = new List<stItem>();

                    for (var i = 0; i < qntd; ++i)
                    {

                        bi = new BuyItem().ToRead(_packet);

                        // BuyItem é a type de visivel no pangya shop
                        // Verifica se o item só pode ser presenteado e da error, por que esse pacote é de comprar e o item só pode ser presenteado
                        // Verifica se o item pode ser comprado
                        if (sIff.getInstance().IsBuyItem(bi._typeid) && !sIff.getInstance().IsOnlyGift(bi._typeid))
                        {

                            // Inicializa o item que o player vai comprar
                            if (bi.pang > 0)
                            {
                                pang += bi.pang;
                            }

                            if (bi.cookie > 0)
                            {
                                cookie += bi.cookie;
                            }

                            item = new stItem();

                            ItemManager.initItemFromBuyItem(_session.m_pi,
                                item, bi, true, option);

                            if (item._typeid == 0)
                            {

                                _smp.message_pool.getInstance().push(new message("[Channel::requestBuyItemShop][Error] ao inicializar item from buyItem, item typeid: " + (bi._typeid) + " bug. para o PLAYER [UID=" + (_session.m_pi.uid) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                                p = new PangyaBinaryWriter((ushort)0x68);

                                p.WriteUInt32(1);

                                packet_func.session_send(p,
                                    _session, 0);

                                return;
                            }

                            if (item.is_cash == 1 ? (option != 1/*Rental*/ && item.desconto != 0 ? bi.cookie != (item.desconto * item.qntd) : bi.cookie != (item.price * item.qntd)) :
                              (option != 1/*Rental*/ && item.desconto != 0 ? bi.pang != (item.desconto * item.qntd) : bi.pang != (item.price * item.qntd)))
                            {

                                _smp.message_pool.getInstance().push(new message("[Channel::requestBuyItemShop][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou comprar um item com preco[server=" + ((item.desconto != 0 ? (item.desconto * item.qntd) : (item.price * item.qntd))) + ", cliente=" + ((item.is_cash.IsTrue() ? bi.cookie : bi.pang)) + "] diferente, item typeid: " + (bi._typeid) + ". Hacker ou bug.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                                p.init_plain(0x68);

                                p.WriteUInt32(2);

                                packet_func.session_send(p,
                                    _session, 0);

                                return;
                            }

                            if (!ItemManager.isTimeItem(item.date) || ItemManager.betweenTimeSystem(ref item.date))
                            {

                                // Verifica se já possui o item, o caddie item verifica se tem o caddie para depois verificar se tem o caddie item
                                if ((sIff.getInstance().IsCanOverlapped(item._typeid) && sIff.getInstance().getItemGroupIdentify(item._typeid) != IFF_GROUP.CAD_ITEM) || !_session.m_pi.ownerItem(item._typeid))
                                {

                                    if (ItemManager.isSetItem(item._typeid))
                                    {

                                        var v_stItem = ItemManager.getItemOfSetItem(_session,
                                            item._typeid, true, 1);

                                        // CP Log, Set Item
                                        if (item.is_cash.IsTrue() && bi.cookie > 0)
                                        {
                                            cp_log.putItem(item._typeid,
                                                (item.STDA_C_ITEM_TIME > 0 ? item.STDA_C_ITEM_TIME : item.STDA_C_ITEM_QNTD),
                                                bi.cookie);
                                        }

                                        if (!v_stItem.empty())
                                        {
                                            // Já verificou lá em cima se tem os item so set, então não precisa mais verificar aqui
                                            // Só add eles ao List de venda
                                            // Verifica se pode ter mais de 1 item e se não ver se não tem o item
                                            foreach (var el in v_stItem)
                                            {
                                                if ((sIff.getInstance().IsCanOverlapped(el._typeid) && sIff.getInstance().getItemGroupIdentify(el._typeid) != IFF_GROUP.CAD_ITEM) || !_session.m_pi.ownerItem(el._typeid))
                                                {
                                                    v_item.Add(new stItem(el));
                                                }
                                            }
                                            //v_item.insert(v_item.end(), v_stItem.begin(), v_stItem.end());

                                            //for (var ii = 0; ii < v_stItem.Count; ++ii) {
                                            //	var itt = VECTOR_FIND_ITEM(_session.m_pi.v_wi, _typeid, == , v_stItem[ii]._typeid);	// Aqui tem que ver mais tipo de item, aqui só está vendo Warehouse item. Ex:Character, Caddie, Skin e etc

                                            //	// verificar se tem character no set e se o player já tem o character para não colocar no List
                                            //	if (sIff::getInstance().IsCanOverlapped(v_stItem[ii]._typeid) || itt == _session.m_pi.v_wi.end())
                                            //		v_item.Add(v_stItem[ii]);
                                            //}
                                        }
                                        else
                                        {

                                            _smp.message_pool.getInstance().push(new message("[Channel::requestBuyItemShop][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou comprar um set item que nao tem item, item typeid: " + (bi._typeid) + ". Hacker ou bug.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                                            p.init_plain(0x68);

                                            p.WriteUInt32(3);

                                            packet_func.session_send(p,
                                                _session, 0);

                                            return;
                                        }

                                    }
                                    else
                                    {

                                        v_item.Add(new stItem(item));

                                        // CP Log, Item
                                        if (item.is_cash.IsTrue() && bi.cookie > 0)
                                        {
                                            cp_log.putItem(item._typeid,
                                                (item.STDA_C_ITEM_TIME > 0 ? item.STDA_C_ITEM_TIME : item.STDA_C_ITEM_QNTD),
                                                bi.cookie);
                                        }
                                    }

                                }
                                else if (sIff.getInstance().getItemGroupIdentify(item._typeid) == PangyaAPI.IFF.JP.Models.Flags.IFF_GROUP.CAD_ITEM)
                                {

                                    _smp.message_pool.getInstance().push(new message("[Channel::requestBuyItemShop][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou comprar um CaddieItem que ele nao tem o caddie, item typeid: " + (bi._typeid) + ". Hacker ou bug.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                                    p.init_plain(0x68);

                                    p.WriteUInt32(11);

                                    packet_func.session_send(p,
                                        _session, 0);

                                    return;
                                }
                                else
                                {

                                    _smp.message_pool.getInstance().push(new message("[Channel::requestBuyItemShop][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou comprar um item que ele ja tem, item typeid: " + (bi._typeid) + ". Hacker ou bug.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                                    p.init_plain(0x68);

                                    p.WriteUInt32(4);

                                    packet_func.session_send(p,
                                        _session, 0);

                                    return;
                                }

                            }
                            else
                            {

                                _smp.message_pool.getInstance().push(new message("[Channel::requestBuyItemShop][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou comprar um item que nao pode comprar, nao esta na data, item typeid: " + (bi._typeid) + ". Hacker ou bug.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                                p.init_plain(0x68);

                                p.WriteUInt32(5);

                                packet_func.session_send(p,
                                    _session, 0);

                                return;
                            }

                        }
                        else
                        {

                            _smp.message_pool.getInstance().push(new message("[Channel::requestBuyItemShop][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou comprar um item que nao pode ser comprado, item typeid: " + (bi._typeid) + ". Hacker ou bug.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            p = new PangyaBinaryWriter((ushort)0x68);

                            p.WriteUInt32(6);

                            packet_func.session_send(p,
                                _session, 0);

                            return;
                        }
                    }

                    // Coupon Id
                    coupon.id = _packet.ReadInt32();

                    if (coupon.id != 0)
                    {

                        // Verifica se o player tem o coupon mesmo
                        var wi_coupon = _session.m_pi.findWarehouseItemById(coupon.id);

                        if (wi_coupon == null)
                        {

                            _smp.message_pool.getInstance().push(new message("[Channel::requestBuyItemShop][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou comprar um item com coupon de descontou, mas ele nao tem o coupon[ID=" + (coupon.id) + "], item typeid: " + (bi._typeid) + ". Hacker ou bug.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            p.init_plain(0x68);

                            p.WriteUInt32(6);

                            packet_func.session_send(p,
                                _session, 0);

                            return;
                        }

                        // Inicializa o coupon para ser removido
                        coupon.type = 2;
                        coupon._typeid = wi_coupon._typeid;
                        coupon.qntd = 1; // Tira  1 coupon
                        coupon.STDA_C_ITEM_QNTD = (short)(coupon.qntd * -1);

                        // !@ Aqui tem que pegar os coupon que tem no banco de dados, e ver qual é o seu valor de descontou, 
                        // por hora está dando 5% desconta com qual quer coupon

                        ulong old_price = cookie;
                        string type_desconto = "5%"; // Padrão 

                        var cmd_guai = new CmdCouponShop(_session.m_pi.uid, coupon.id, true); // Waiter

                        snmdb.NormalManagerDB.getInstance().add(0, cmd_guai, null, null);

                        if (cmd_guai.getException().getCodeError() != 0)
                            throw cmd_guai.getException();

                        var desconto = cmd_guai.getCouponShop();


                        // !@ aqui tem que pegar o tipo do coupon e o seu valor que tem que tirar do banco de dados
                        // mas no cache do server
                        // Hard Coded
                        if (desconto > 0)
                        {

                            // 5 CP de desconto
                            if ((cookie - desconto) < 0)
                                cookie = 0;

                            else
                                cookie -= desconto;

                            type_desconto = desconto + "% CP";

                        }
                        else // Padrão para coupons desconhecido pelo server hardcoded
                        {
                            cookie = (ulong)(cookie * 0.95f);
                        }


                        // Log
                        coupon_msg = " e usou Coupon[TYPEID=" + (wi_coupon._typeid) + ", ID=" + (wi_coupon.id) + ", DESCONTO=" + type_desconto + ", TOTAL_CP=" + (old_price) + ", TOTAL_CP_COM_DESCONTO=" + (cookie) + "]";
                    }

                    if (_session.m_pi.cookie < cookie || _session.m_pi.ui.pang < pang)
                    {

                        // Aqui depois especifica cada um separado para manda mensagem
                        _smp.message_pool.getInstance().push(new message("[Channel::requestBuyItemShop][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou comprar um item, mas nao tem moedas(Pang ou Cookie) suficiente, item typeid: " + (bi._typeid) + ". Hacker ou bug.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                        p.init_plain(0x68);

                        p.WriteUInt32(7);

                        packet_func.session_send(p,
                            _session, 0);

                        return;
                    }

                    try
                    {

                        // Consome o cookie e pang, Antes de adicionar os itens
                        _session.m_pi.consomeMoeda(pang, cookie);

                    }
                    catch (exception e)
                    {

                        _smp.message_pool.getInstance().push(new message("[Channel::requestBuyItemShop][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                        if (ExceptionError.STDA_ERROR_CHECK_SOURCE_AND_ERROR_TYPE(e.getCodeError(),
                            STDA_ERROR_TYPE.PLAYER_INFO,
                            200))
                        {

                            p.init_plain(0x68);

                            p.WriteUInt32(2); // Tem alterações no Cookie do player no DB

                            packet_func.session_send(p,
                                _session, 0);

                            return;

                        }
                        else // Unknown Error
                        {
                            throw;
                        }
                    }


                    // Remove coupon se a compra foi feita com ele
                    if (coupon.id != 0 && coupon._typeid != 0u)
                    {

                        if (ItemManager.removeItem(coupon, _session) <= 0)
                        {

                            _smp.message_pool.getInstance().push(new message("[Channel::requestBuyItemShop][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou comprar um item com coupon de descontou, mas nao conseguiu remove o coupon[TYPEID=" + (coupon._typeid) + ", ID=" + (coupon.id) + "], item typeid: " + (bi._typeid) + ". Hacker ou bug.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            // Devolve as moedas gasta para o player, aqui tem que devolver o valor de cada item
                            _smp.message_pool.getInstance().push(new message("[Channel::requestBuyItemShop][Warning] devolve as moedas gasta deu erro no add itens no db para o player.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            _session.m_pi.addMoeda(pang, cookie);

                            p.init_plain(0x68);

                            p.WriteUInt32(8);

                            packet_func.session_send(p,
                                _session, 0);

                            return;
                        }

                        // Remove o coupon do player no jogo
                        p.init_plain(0x216);

                        p.WriteUInt32((uint)UtilTime.GetSystemTimeAsUnix());
                        p.WriteUInt32(1u); // Count

                        p.WriteByte(coupon.type);
                        p.WriteUInt32(coupon._typeid);
                        p.WriteInt32(coupon.id);
                        p.WriteInt32(coupon.flag_time); // Time Tipo(ou type)
                        p.WriteInt32(coupon.stat.qntd_ant); // qntd ant
                        p.WriteInt32(coupon.stat.qntd_dep); // qntd dep
                        p.WriteInt32((coupon.STDA_C_ITEM_TIME > 0 ? coupon.STDA_C_ITEM_TIME : coupon.STDA_C_ITEM_QNTD)); // qntd
                        p.WriteZeroByte(25);

                        packet_func.session_send(p,
                            _session, 1);
                    }

                    var rai = ItemManager.addItem(v_item,
                        _session.getUID(), 0, 1);

                    if (rai.fails.Count > 0 && rai.type != ItemManager.RetAddItem.T_SUCCESS_PANG_AND_EXP_AND_CP_POUCH)
                    {

                        string str = "";

                        for (var i = 0; i < rai.fails.Count; ++i)
                        {

                            if (i == 0)
                            {
                                str += "[TYPEID=" + (rai.fails[i]._typeid) + ", ID=" + (rai.fails[i].id) + ", QNTD=" + ((rai.fails[i].qntd > 0xFFu) ? rai.fails[i].qntd : rai.fails[i].STDA_C_ITEM_QNTD) + (rai.fails[i].STDA_C_ITEM_TIME > 0 ? ", TEMPO=" + (rai.fails[i].STDA_C_ITEM_TIME) : "") + "]";
                            }
                            else
                            {
                                str += ", [TYPEID=" + (rai.fails[i]._typeid) + ", ID=" + (rai.fails[i].id) + ", QNTD=" + ((rai.fails[i].qntd > 0xFFu) ? rai.fails[i].qntd : rai.fails[i].STDA_C_ITEM_QNTD) + (rai.fails[i].STDA_C_ITEM_TIME > 0 ? ", TEMPO=" + (rai.fails[i].STDA_C_ITEM_TIME) : "") + "]";
                            }
                        }

                        //Aqui depois especifica cada um separado para manda mensagem
                        _smp.message_pool.getInstance().push(new message("[Channel::requestBuyItemShop][Error] Itens que falhou ao add os itens que o PLAYER [UID=" + (_session.m_pi.uid) + "] comprou item(ns){" + str + "}. Hacker ou bug.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                        // Devolve as moedas gasta para o player, aqui tem que devolver o valor de cada item
                        _smp.message_pool.getInstance().push(new message("[Channel::requestBuyItemShop][Warning] devolve as moedas gasta deu erro no add itens no db para o player.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                        _session.m_pi.addMoeda(pang, cookie);

                        p.init_plain(0x68);

                        p.WriteUInt32(8);

                        packet_func.session_send(p,
                            _session, 0);

                        return;
                    }

                    var log_itens = new StringBuilder();

                    foreach (var el in v_item)
                    {
                        if (log_itens.Length > 0)
                            log_itens.Append("; ");

                        log_itens.Append($"[TYPEID={el._typeid}, ID={el.id}, FLAG_TIME={el.flag_time}, " +
                                         $"QNTD={(el.STDA_C_ITEM_TIME > 0 ? el.STDA_C_ITEM_TIME : el.STDA_C_ITEM_QNTD)}, " +
                                         $"QNTD_DEPOIS={el.stat.qntd_dep}]");
                    }

                    // Packet Send global of requestbuyitemshop
                    p = new PangyaBinaryWriter();

                    if (pang > 0)
                    {
                        p.init_plain(0xC8);

                        p.WriteUInt64(_session.m_pi.ui.pang);
                        p.WriteUInt64(pang);

                        packet_func.session_send(p,
                            _session, 1);
                    }

                    if (cookie > 0)
                    {

                        // Log de Gastos de CP
                        _session.saveCPLog(cp_log);

                        p.init_plain(0x96);

                        p.WriteUInt64(_session.m_pi.cookie);

                        packet_func.session_send(p,
                            _session, 1);
                    }


                    packet_func.session_send(packet_func.pacote0AA(_session, v_item),
                        _session, 1);

                    p.init_plain(0x68);

                    p.WriteUInt32(0);
                    p.WriteUInt64(_session.m_pi.ui.pang);
                    p.WriteUInt64(_session.m_pi.cookie);

                    packet_func.session_send(p,
                        _session, 0);

                    snmdb.NormalManagerDB.getInstance().add(0,
                        new CmdItemBuyShopLog(_session.m_pi.uid,
                            bi),
                        SQLDBResponse, this);



                }
                else
                { // quantidade de itens para comprar é 0

                    _smp.message_pool.getInstance().push(new message("[Channel::requestBuyItemShop][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou comprar um item, mas nao enviou nenhum item no request. Hacker ou bug.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                    p.init_plain(0x68);

                    p.WriteUInt32(9);

                    packet_func.session_send(p,
                        _session, 0);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestBuyItemShop][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] error desconhecido: " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x68);

                p.WriteUInt32(10);

                packet_func.session_send(p,
                    _session, 0);
            }
        }

        public void requestGiftItemShop(Player _session, packet _packet)
        {

            PangyaBinaryWriter p = new PangyaBinaryWriter();

            try
            {
                // Dados Log gasto de CP
                CPLog cp_log = new CPLog();

                cp_log.setType(CPLog.TYPE.GIFT_SHOP);

                BuyItem bi = new BuyItem();

                ushort option = _packet.ReadUInt16();
                uint uid_to_send = _packet.ReadUInt32();
                string msg = _packet.ReadString();
                byte opt2 = _packet.ReadUInt8();
                ushort qntd = _packet.ReadUInt16();

                ulong pang = 0Ul;
                ulong cookie = 0Ul;

                if (_session.m_pi.block_flag.m_flag.gift_shop || _session.m_pi.block_flag.m_flag.buy_and_gift_shop)
                {
                    throw new exception("[Channel::requestGiftItemShop][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou presentear PLAYER [UID=" + (uid_to_send) + "], mas ele nao pode. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        2, 0x790001));
                }

                // Verifica o level do player e bloquea se não tiver level Beginner E
                if (_session.m_pi.mi.level < (ushort)enLEVEL.BEGINNER_E)
                {
                    throw new exception("[Channel::requestGiftItemShop][Error] PLAYER [UID=" + (_session.m_pi.uid) + ", LEVEL=" + (_session.m_pi.mi.level) + "] tentou presentear o PLAYER [UID=" + (uid_to_send) + "], mas o level dele é menor que Beginner E.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        3500, 1));
                }

                if (qntd > 0)
                {

                    stItem item = new stItem();
                    List<stItem> v_item = new List<stItem>();

                    for (var i = 0; i < qntd; ++i)
                    {

                        bi = new BuyItem().ToRead(_packet);

                        // Verifica se o item pode ser presenteado
                        if (sIff.getInstance().IsGiftItem(bi._typeid))
                        {

                            // Inicializa o item que o player vai comprar
                            if (bi.pang > 0)
                            {
                                pang += bi.pang;
                            }

                            if (bi.cookie > 0)
                            {
                                cookie += bi.cookie;
                            }

                            item = new stItem();

                            ItemManager.initItemFromBuyItem(_session.m_pi,
                                item, bi, true, option, 1);

                            if (item._typeid == 0)
                            {

                                _smp.message_pool.getInstance().push(new message("[Channel::requestGiftItemShop][Error] ao inicializar item from buyItem, item typeid: " + (bi._typeid) + " bug. para o PLAYER [UID=" + (_session.m_pi.uid) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                                p.init_plain(0x6A);

                                p.WriteUInt32(1);

                                p.WriteUInt64(_session.m_pi.ui.pang);
                                p.WriteUInt64(_session.m_pi.cookie);

                                packet_func.session_send(p,
                                    _session, 0);

                                return;
                            }

                            if (item.is_cash.IsTrue() ? (item.desconto != 0 ? bi.cookie != (item.desconto * item.qntd) : bi.cookie != (item.price * item.qntd)) : (item.desconto != 0 ? bi.pang != (item.desconto * item.qntd) : bi.pang != (item.price * item.qntd)))
                            {

                                _smp.message_pool.getInstance().push(new message("[Channel::requestGiftItemShop][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou presentear para o PLAYER [UID=" + (uid_to_send) + "] um item com preco[server=" + ((item.desconto != 0 ? (item.desconto * item.qntd) : (item.price * item.qntd))) + ", cliente=" + ((item.is_cash.IsTrue() ? bi.cookie : bi.pang)) + "] diferente, item typeid: " + (bi._typeid) + ". Hacker ou bug.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                                p.init_plain(0x6A);

                                p.WriteUInt32(2);

                                p.WriteUInt64(_session.m_pi.ui.pang);
                                p.WriteUInt64(_session.m_pi.cookie);

                                packet_func.session_send(p,
                                    _session, 0);

                                return;
                            }

                            if (!ItemManager.isTimeItem(item.date) || ItemManager.betweenTimeSystem(ref item.date))
                            {

                                // para ele verificar se o player tem o caddie antes de enviar o part do caddie
                                if ((sIff.getInstance().IsCanOverlapped(item._typeid) && sIff.getInstance().getItemGroupIdentify(item._typeid) != IFF_GROUP.CAD_ITEM) || !ItemManager.ownerItem(uid_to_send, item._typeid))
                                {

                                    if (ItemManager.isSetItem(item._typeid))
                                    {

                                        // CP Log, Set Item
                                        if (item.is_cash.IsTrue() && bi.cookie > 0)
                                        {
                                            cp_log.putItem(item._typeid,
                                                (item.STDA_C_ITEM_TIME > 0 ? item.STDA_C_ITEM_TIME : item.STDA_C_ITEM_QNTD),
                                                bi.cookie);
                                        }

                                        var v_stItem = ItemManager.getItemOfSetItem(_session,
                                            item._typeid, true, 1);

                                        // No gift ele envia o set para o player, e não os itens que contém dentro do set
                                        if (!v_stItem.empty())
                                        {
                                            // No gift ele envia o set para o player, e não os itens que contém dentro do set
                                            v_item.Add(new stItem(item));

                                        }
                                        else
                                        {

                                            _smp.message_pool.getInstance().push(new message("[Channel::requestGiftItemShop][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou presentear para o PLAYER [UID=" + (uid_to_send) + "] um set item que nao tem item, item typeid: " + (bi._typeid) + ". Hacker ou bug.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                                            p.init_plain(0x6A);

                                            p.WriteUInt32(3);

                                            p.WriteUInt64(_session.m_pi.ui.pang);
                                            p.WriteUInt64(_session.m_pi.cookie);

                                            packet_func.session_send(p,
                                                _session, 0);

                                            return;
                                        }

                                    }
                                    else
                                    {

                                        v_item.Add(new stItem(item));

                                        // CP Log, Item
                                        if (item.is_cash.IsTrue() && bi.cookie > 0)
                                        {
                                            cp_log.putItem(item._typeid,
                                                (item.STDA_C_ITEM_TIME > 0 ? item.STDA_C_ITEM_TIME : item.STDA_C_ITEM_QNTD),
                                                bi.cookie);
                                        }
                                    }

                                }
                                else if (sIff.getInstance().getItemGroupIdentify(item._typeid) == PangyaAPI.IFF.JP.Models.Flags.IFF_GROUP.CAD_ITEM)
                                {

                                    _smp.message_pool.getInstance().push(new message("[Channel::requestGiftItemShop][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou presentear um CaddieItem que o PLAYER [UID=" + (uid_to_send) + "] nao tem o caddie, item typeid: " + (bi._typeid), type_msg.CL_FILE_LOG_AND_CONSOLE));

                                    p.init_plain(0x6A);

                                    p.WriteUInt32(11);

                                    p.WriteUInt64(_session.m_pi.ui.pang);
                                    p.WriteUInt64(_session.m_pi.cookie);

                                    packet_func.session_send(p,
                                        _session, 0);

                                    return;

                                }
                                else
                                {

                                    _smp.message_pool.getInstance().push(new message("[Channel::requestGiftItemShop][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou presentear um item que o PLAYER [UID=" + (uid_to_send) + "] ja tem, item typeid: " + (bi._typeid), type_msg.CL_FILE_LOG_AND_CONSOLE));

                                    p.init_plain(0x6A);

                                    p.WriteUInt32(4);

                                    p.WriteUInt64(_session.m_pi.ui.pang);
                                    p.WriteUInt64(_session.m_pi.cookie);

                                    packet_func.session_send(p,
                                        _session, 0);

                                    return;
                                }

                            }
                            else
                            {

                                _smp.message_pool.getInstance().push(new message("[Channel::requestGiftItemShop][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou presentear para o PLAYER [UID=" + (uid_to_send) + "] um item que nao esta na data para esta disponivel no shop, item typeid: " + (bi._typeid) + ". Hacker ou bug.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                                p.init_plain(0x6A);

                                p.WriteUInt32(5);

                                p.WriteUInt64(_session.m_pi.ui.pang);
                                p.WriteUInt64(_session.m_pi.cookie);

                                packet_func.session_send(p,
                                    _session, 0);

                                return;
                            }

                        }
                        else
                        {

                            _smp.message_pool.getInstance().push(new message("[Channel::requestGiftItemShop][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou presentear para o PLAYER [UID=" + (uid_to_send) + "] um item que nao pode ser comprado[indisponivel no shop], item typeid: " + (bi._typeid) + ". Hacker ou bug.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            p.init_plain(0x6A);

                            p.WriteUInt32(6);

                            p.WriteUInt64(_session.m_pi.ui.pang);
                            p.WriteUInt64(_session.m_pi.cookie);

                            packet_func.session_send(p,
                                _session, 0);

                            return;
                        }
                    }

                    if (_session.m_pi.cookie < cookie || _session.m_pi.ui.pang < pang)
                    {

                        // Aqui depois especifica cada um separado para manda mensagem
                        _smp.message_pool.getInstance().push(new message("[Channel::requestGiftItemShop][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou presentear para o PLAYER [UID=" + (uid_to_send) + "] um item, mas nao tem moedas(Pang ou Cookie) suficiente, item typeid: " + (bi._typeid) + ". Hacker ou bug.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                        p.init_plain(0x6A);

                        p.WriteUInt32(7);

                        p.WriteUInt64(_session.m_pi.ui.pang);
                        p.WriteUInt64(_session.m_pi.cookie);

                        packet_func.session_send(p,
                            _session, 0);

                        return;
                    }

                    try
                    {

                        // Consome o cookie e pang, Antes de adicionar os itens
                        _session.m_pi.consomeMoeda(pang, cookie);

                    }
                    catch (exception e)
                    {

                        _smp.message_pool.getInstance().push(new message("[Channel::requestGiftItemShop][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                        if (ExceptionError.STDA_ERROR_CHECK_SOURCE_AND_ERROR_TYPE(e.getCodeError(),
                            STDA_ERROR_TYPE.PLAYER_INFO,
                            200))
                        {

                            p.init_plain(0x6A);

                            p.WriteUInt32(2); // Tem alterações no Cookie do player no DB

                            p.WriteUInt64(_session.m_pi.ui.pang);
                            p.WriteUInt64(_session.m_pi.cookie);

                            packet_func.session_send(p,
                                _session, 0);

                            return;

                        }
                        else // Unknown Error
                        {
                            throw;
                        }
                    }

                    int mail_id = 0;

                    try
                    {

                        if ((mail_id = MailBoxManager.sendMessageWithItem(_session.m_pi.uid,
                            uid_to_send, msg, v_item)) <= 0)
                        {
                            throw new exception("[Channel::requestGiftItemShop][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou presentear um PLAYER [UID=" + (uid_to_send) + "] com o Item[TYPEID=" + (bi._typeid) + "], mas nao conseguiu colocar o item no mail box do player. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                1, 0x5800101));
                        }

                    }
                    catch (exception e)
                    {

                        _smp.message_pool.getInstance().push(new message("[Channel::requestGiftItemShop][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                        // Aqui depois especifica cada um separado para manda mensagem
                        _smp.message_pool.getInstance().push(new message("[Channel::requestGiftItemShop][Error] ao add os itens que o PLAYER [UID=" + (_session.m_pi.uid) + "] presenteou para o PLAYER [UID=" + (uid_to_send) + "]. Hacker ou bug.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                        _smp.message_pool.getInstance().push(new message("[Channel::requestGiftItemShop][Error] devolve as moedas gasta deu erro no add itens no db para o player.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                        // Devolve as moedas gasta para o player, aqui tem que devolver o valor de cada item
                        _session.m_pi.addMoeda(pang, cookie);

                        p.init_plain(0x6A);

                        p.WriteUInt32(8);

                        p.WriteUInt64(_session.m_pi.ui.pang);
                        p.WriteUInt64(_session.m_pi.cookie);

                        packet_func.session_send(p,
                            _session, 0);

                        return;

                    }

                    // Log
                    var log_itens = new StringBuilder();

                    foreach (var el in v_item)
                    {
                        if (log_itens.Length > 0)
                            log_itens.Append("; ");

                        log_itens.Append($"[TYPEID={el._typeid}, ID={el.id}, FLAG_TIME={el.flag_time}, " +
                                         $"QNTD={(el.STDA_C_ITEM_TIME > 0 ? el.STDA_C_ITEM_TIME : el.STDA_C_ITEM_QNTD)}, " +
                                         $"QNTD_DEPOIS={el.stat.qntd_dep}]");
                    }

                    var log_msg = $"[Channel::requestGiftItemShop][Sucess] PLAYER [UID=" + (_session.m_pi.uid) + "] MailBox[MAIL_ID=" + (mail_id) + "] mandou " + (v_item.Count) + " presente(s), Moedas(CP=" + (cookie) + ", PANG=" + (pang) + "), do Shop para o PLAYER [UID=" + (uid_to_send) + "]. Item(ns) { " + log_itens + " }";

                    _smp.message_pool.getInstance().push(new message(log_msg, type_msg.CL_ONLY_FILE_LOG));

                    if (pang > 0)
                    {
                        p.init_plain(0xC8);

                        p.WriteUInt64(_session.m_pi.ui.pang);
                        p.WriteUInt64(pang);

                        packet_func.session_send(p,
                            _session, 1);
                    }

                    if (cookie > 0)
                    {

                        // Log de Gastos de CP
                        cp_log.setMailId(mail_id);

                        _session.saveCPLog(cp_log);

                        p.init_plain(0x96);

                        p.WriteUInt64(_session.m_pi.cookie);

                        packet_func.session_send(p,
                            _session, 1);
                    }

                    p.init_plain(0x6A);

                    p.WriteUInt32(0);
                    p.WriteUInt64(_session.m_pi.ui.pang);
                    p.WriteUInt64(_session.m_pi.cookie);

                    packet_func.session_send(p,
                        _session, 0);

                    snmdb.NormalManagerDB.getInstance().add(0,
                        new CmdItemBuyShopLog(_session.m_pi.uid,
                            bi),
                        SQLDBResponse, this);

                }
                else
                { // quantidade de itens para comprar é 0

                    _smp.message_pool.getInstance().push(new message("[Channel::requestGiftItemShop][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou presentear para o PLAYER [UID=" + (uid_to_send) + "] um item, mas nao enviou nenhum item no request. Hacker ou bug.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                    p.init_plain(0x6A);

                    p.WriteUInt32(9);

                    p.WriteUInt64(_session.m_pi.ui.pang);
                    p.WriteUInt64(_session.m_pi.cookie);

                    packet_func.session_send(p,
                        _session, 0);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestGiftItemShop][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] error desconhecido: " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x6A);

                p.WriteUInt32(ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) == STDA_ERROR_TYPE.CHANNEL ? ExceptionError.STDA_SYSTEM_ERROR_DECODE(e.getCodeError()) : 10);

                p.WriteUInt64(_session.m_pi.ui.pang);
                p.WriteUInt64(_session.m_pi.cookie);

                packet_func.session_send(p,
                    _session, 0);
            }
        }

        public void requestExtendRental(Player _session, packet _packet)
        {
            //

            PangyaBinaryWriter p = new PangyaBinaryWriter();

            try
            {

                int item_id = _packet.ReadInt32();





                if (item_id <= 0)
                {
                    throw new exception("[Channel::requestExtendRental][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou extend rental, mas o item[ID=" + (item_id) + "] is invalid. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        350, 5200351));
                }

                var pWi = _session.m_pi.findWarehouseItemById(item_id);

                if (pWi == null)
                {
                    throw new exception("[Channel::requestExtendRental][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou extend rental, mas o player nao tem o item[ID=" + (item_id) + "]. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        351, 5200352));
                }

                if (sIff.getInstance().getItemGroupIdentify(pWi._typeid) != IFF_GROUP.PART)
                {
                    throw new exception("[Channel::requestExtendRental][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou extend rental, mas o item[TYPEID=" + (pWi._typeid) + ", ID=" + (pWi.id) + "] nao é um Part. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        352, 5200353));
                }

                var part = sIff.getInstance().findPart(pWi._typeid);

                if (part == null)
                {
                    throw new exception("[Channel::requestExtendRental][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou extender um rental Item[TYPEID=" + (pWi._typeid) + ", ID=" + (pWi.id) + "] que nao esta no IFF_STRUCT do server. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        353, 5200354));
                }

                if (part.valor_rental <= 0)
                {
                    throw new exception("[Channel::requestExtendRental][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou extender um rental Item[TYPEID=" + (pWi._typeid) + ", ID=" + (pWi.id) + "] que nao é um rental no IFF_STRUCT do server. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        354, 5200355));
                }

                pWi.end_date_unix_local = (uint)UtilTime.GetLocalTimeAsUnix() + (7 * 24 * 3600);

                // Convert to UTC to send to client
                pWi.end_date = UtilTime.UnixTimeConvert((long)pWi.end_date_unix_local);

                var end_date = UtilTime.FormatDateLocal(pWi.end_date_unix_local);

                // Cmd Extend Rental + 7 dias no DB
                snmdb.NormalManagerDB.getInstance().add(5,
                    new CmdExtendRental(_session.m_pi.uid,
                        pWi.id, end_date),
                    SQLDBResponse, this);

                // Tira os pangs do valor de renovar o Rental Item
                _session.m_pi.consomePang(part.valor_rental);

                // Verifica se o Parts já tem um item update do parts, por que se tiver, 
                // ele vai desequipar esse parts por que o player não relogou quando acabou o tempo do parts
                var v_it = _session.m_pi.findUpdateItemByTypeidAndId(pWi._typeid, pWi.id);

                if (!v_it.empty())
                {

                    foreach (var el in v_it)
                    {
                        if (el.Value.type == UpdateItem.UI_TYPE.WAREHOUSE)
                        {
                            // Tira esse Update Item do map
                            _session.m_pi.mp_ui.Remove(el.Key);
                        }
                    }

                }
                // ---- fim do verifica se tem o parts no update item ----

                // Log
                _smp.message_pool.getInstance().push(new message("[Rental::Extend][Sucess] PLAYER [UID=" + (_session.m_pi.uid) + "] extendeu o Rental Item[TYPEID=" + (pWi._typeid) + ", ID=" + (pWi.id) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Att pang no Jogo
                p.init_plain(0xC8);

                p.WriteUInt64(_session.m_pi.ui.pang);
                p.WriteUInt64(part.valor_rental);

                // Att Rental Item no Jogo
                packet_func.session_send(p,
                    _session, 1);

                p.init_plain(0x18F);

                p.WriteByte(0); // OK

                p.WriteUInt32(pWi._typeid);
                p.WriteInt32(pWi.id);

                packet_func.session_send(p,
                    _session, 1);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestExtendRental][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x18F);

                p.WriteByte(1); // Error

                packet_func.session_send(p,
                    _session, 1);
            }
        }

        public void requestDeleteRental(Player _session, packet _packet)
        {
            //

            PangyaBinaryWriter p = new PangyaBinaryWriter();

            try
            {

                int item_id = _packet.ReadInt32();





                if (item_id <= 0)
                {
                    throw new exception("[Channel::requestDeleteRental][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou deletar um Rental item[ID=" + (item_id) + "] invalid. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        400, 5200401));
                }

                var pWi = _session.m_pi.findWarehouseItemById(item_id);

                if (pWi == null)
                {
                    throw new exception("[Channel::requestDeleteRental][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou deletar um Rental item[ID=" + (item_id) + "] que ele nao tem. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        401, 5200402));
                }

                if (sIff.getInstance().getItemGroupIdentify(pWi._typeid) != IFF_GROUP.PART)
                {
                    throw new exception("[Channel::requestDeleteRental][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou deletar um Rental Item[TYPEID=" + (pWi._typeid) + ", ID=" + (pWi.id) + "] que nao é um Part. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        402, 5200403));
                }

                var part = sIff.getInstance().findPart(pWi._typeid);

                if (part == null)
                {
                    throw new exception("[Channel::requestDeleteRental][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou deletar um rental Item[TYPEID=" + (pWi._typeid) + ", ID=" + (pWi.id) + "] que nao esta no IFF_STRUCT do server. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        403, 5200404));
                }

                if (part.valor_rental <= 0)
                {
                    throw new exception("[Channel::requestDeleteRental][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou deletar um rental Item[TYPEID=" + (pWi._typeid) + ", ID=" + (pWi.id) + "] que nao é um rental no IFF_STRUCT do server. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        404, 5200404));
                }

                var tmp_wi = pWi;

                var it = _session.m_pi.findWarehouseItemById(pWi.id);

                if (it != null)
                {
                    _session.m_pi.mp_wi.Remove(it.id);
                }

                // Att no Banco de dados
                snmdb.NormalManagerDB.getInstance().add(6,
                    new CmdDeleteRental(_session.m_pi.uid, tmp_wi.id),
                    SQLDBResponse, this);

                _smp.message_pool.getInstance().push(new message("[Rental::Delete][Sucess] PLAYER [UID=" + (_session.m_pi.uid) + "] deletou Rental Item[TYPEID=" + (tmp_wi._typeid) + ", ID=" + (tmp_wi.id) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x190);

                p.WriteByte(0); // OK

                p.WriteUInt32(tmp_wi._typeid);
                p.WriteInt32(tmp_wi.id);

                packet_func.session_send(p,
                    _session, 1);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestDeleteRental][ErroSytem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x190);

                p.WriteByte(1); // Error

                packet_func.session_send(p,
                    _session, 1);
            }
        }

        public void requestCheckAttendanceReward(Player _session, packet _packet)
        {
            //

            try
            {





                // Attendance Reward System
                if (!sAttendanceRewardSystem.getInstance().isLoad())
                {
                    sAttendanceRewardSystem.getInstance().load();
                }

                sAttendanceRewardSystem.getInstance().requestCheckAttendance(_session, _packet);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestCheckAttendanceReward][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestAttendanceRewardLoginCount(Player _session, packet _packet)
        {
            //

            PangyaBinaryWriter p = new PangyaBinaryWriter();

            try
            {





                // Attendance Reward System
                if (!sAttendanceRewardSystem.getInstance().isLoad())
                {
                    sAttendanceRewardSystem.getInstance().load();
                }

                sAttendanceRewardSystem.getInstance().requestUpdateCountLogin(_session, _packet);
            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestAttendanceRewardLoginCount][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestDailyQuest(Player _session, packet _packet)
        {
            //

            try
            {





                DailyQuestManager.requestCheckAndSendDailyQuest(_session, _packet);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestDailyQuest][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestAcceptDailyQuest(Player _session, packet _packet)
        {
            //

            try
            {





                DailyQuestManager.requestAcceptQuest(_session, _packet);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestAcceptDailyQuest][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestTakeRewardDailyQuest(Player _session, packet _packet)
        {
            //

            try
            {





                DailyQuestManager.requestTakeRewardQuest(_session, _packet);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestTakeRewardDailyQuest][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestLeaveDailyQuest(Player _session, packet _packet)
        {
            //

            try
            {





                DailyQuestManager.requestLeaveQuest(_session, _packet);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestLeaveDailyQuest][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }


        public void requestCadieCauldronExchange(Player _session, packet _packet)
        {
            //

            PangyaBinaryWriter p = new PangyaBinaryWriter();
            try
            {

                if (_session.m_pi.block_flag.m_flag.cadie_recycle)
                    throw new exception(
                        $"[[Channel::requestCadieCauldronExchange][BLOCK] UID={_session.m_pi.uid}",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 8, 0x790001)
                    );

                ushort seq = _packet.ReadUInt16();
                uint clientRequested = _packet.ReadUInt32();
                byte count = _packet.ReadUInt8();

                if (count == 0 || count > 4)
                    throw new exception(
                        $"[[Channel::requestCadieCauldronExchange][CHEAT] UID={_session.m_pi.uid} count={count}",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 450, 5200451)
                    );

                if (_packet.BytesRemaining < count * 8)
                    throw new exception(
                        $"[[Channel::requestCadieCauldronExchange][CHEAT] pacote truncado UID={_session.m_pi.uid}",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 450, 5200452)
                    );

                CadieExchangeItem[] cei = new CadieExchangeItem[count];
                for (int i = 0; i < count; i++)
                    cei[i] = new CadieExchangeItem().ToRead(_packet);

                var cmb = sIff.getInstance().findCadieMagicBox((uint)(seq + 1));
                if (cmb == null || cmb.seq != seq + 1 || !cmb.active.IsTrue())
                    throw new exception(
                        $"[[Channel::requestCadieCauldronExchange][CHEAT] Seq inválida UID={_session.m_pi.uid}",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 451, 5200452)
                    );

                if (_session.m_pi.mi.level < cmb.level)
                    throw new exception(
                        $"[[Channel::requestCadieCauldronExchange][LEVEL] UID={_session.m_pi.uid}",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 454, 5200455)
                    );

                // 🔒 Validação dos itens exigidos
                for (int i = 0; i < count; i++)
                {
                    if (cmb.item_trade.ID[i] != 0 && cmb.item_trade.ID[i] != cei[i]._typeid)
                        throw new exception(
                            $"[[Channel::requestCadieCauldronExchange][CHEAT] item mismatch UID={_session.m_pi.uid}",
                            ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 453, 5200454)
                        );

                    cei[i].QtyPerExchange = cmb.item_trade.Qty[i];
                }


                if (ItemManager.isTimeItem(new stItem.stDate.stDateSys(cmb.date.Start, cmb.date.End)) && !ItemManager.betweenTimeSystem(new stItem.stDate.stDateSys(cmb.date.Start, cmb.date.End)))
                {
                    throw new exception("[Channel::requestCadieCauldronExchange][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou trocar item no CadieCauldron, mas o item[Seq=" + (seq + 1) + "] nao esta mais na data[temporario]. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        455, 5200456));
                }

                if (cmb.Box_Random_ID == 0
                    && !sIff.getInstance().IsCanOverlapped(cmb.item_receive.ID)
                    && _session.m_pi.ownerItem(cmb.item_receive.ID))
                {
                    throw new exception("[Channel::requestCadieCauldronExchange][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou trocar item[Seq=" + (seq + 1) + ", TYPEID_RCV=" + (cmb.item_receive.ID) + "] no Cauldron que ele ja possui e nao pode ter duplicata", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        458, 5200459));
                }

                // ============================
                // 🔒 CALCULA LIMITE GLOBAL
                // ============================
                uint safeExchangeCount = clientRequested;

                for (int i = 0; i < count; i++)
                {
                    uint itemLimit = ItemManager.CalculateSafeExchangeCount(
                        _session,
                        cei[i],
                        safeExchangeCount,
                        100
                    );

                    if (itemLimit < safeExchangeCount)
                        safeExchangeCount = itemLimit;
                }

                if (safeExchangeCount == 0)
                    throw new exception(
                        $"[[Channel::requestCadieCauldronExchange][CHEAT] safeExchangeCount=0 UID={_session.m_pi.uid}",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 902, 0xDEAD0003)
                    );


                var r = m_rm.findRoom((short)_session.m_pi.mi.sala_numero);

                // ============================
                // 🔒 EXECUTA TROCA (CHECKED)
                // ============================
                for (int i = 0; i < count; i++)
                {
                    ulong totalQty = (ulong)cmb.item_trade.Qty[i] * safeExchangeCount;
                    if (totalQty > uint.MaxValue)
                        throw new exception(
                            $"[[Channel::requestCadieCauldronExchange][OVERFLOW] UID={_session.m_pi.uid}",
                            ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 904, 0xDEAD0005)
                        );

                    if (ItemManager.exchangeCadieMagicBox(
                            _session,
                            cei[i]._typeid,
                            cei[i].id,
                            (uint)totalQty
                        ) <= 0)
                    {
                        throw new exception(
                            $"[[Channel::requestCadieCauldronExchange][Error][CT] troca inválida UID={_session.m_pi.uid}",
                            ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 457, 5200458)
                        );
                    }

                    if (r != null && r.checkPersonalShopItem(_session, cei[i].id))
                        throw new exception(
                            $"[[Channel::requestCadieCauldronExchange][Error] UID={_session.m_pi.uid}",
                            ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 1010, 0x5201010)
                        );
                }

                List<stItem> v_remove = new List<stItem>();
                List<stItem> v_item = new List<stItem>();
                stItem item = new stItem();
                BuyItem bi = new BuyItem();

                AchievementSystem sys_achieve = new AchievementSystem();

                // Remove os itens
                for (var i = 0; i < count; ++i)
                {
                    item = new stItem();

                    item.type = 2;
                    item.id = cei[i].id;
                    item._typeid = cei[i]._typeid;
                    item.qntd = (int)(cmb.item_trade.Qty[i] * safeExchangeCount);
                    item.STDA_C_ITEM_QNTD = (short)(item.qntd * -1); // Tira

                    v_remove.Add(new stItem(item));
                }

                // remove itens
                if (ItemManager.removeItem(v_remove, _session) <= 0)
                {
                    throw new exception("[Channel::requestCadieCauldronExchange][Error] problemas ao remover(s) item(ns) do PLAYER [UID=" + (_session.m_pi.uid) + "]", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        461, 5200462));
                }

                // Random Item
                if (cmb.Box_Random_ID > 0)
                { // Random Item

                    var cmbr_iff = sIff.getInstance().findCadieMagicBoxRandom(cmb.Box_Random_ID);

                    if (cmbr_iff.empty())
                    {
                        throw new exception("[Channel::requestCadieCauldronExchange][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] CadieMagicBoxRandom[ID=" + (cmb.Box_Random_ID) + "] empty", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                            456, 5200457));
                    }

                    // Sortea Item
                    Lottery lottery = new Lottery();

                    foreach (var el in cmbr_iff)
                    {
                        lottery.Push(el.Value.item_random.Rate, el);
                    }

                    var lc = lottery.spinRoleta();

                    if (lc == null)
                    {
                        throw new exception("[Channel::requestCadieCauldronExchange][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] nao conseguiu sortear um item do caddie magic box random[ID=" + (cmb.Box_Random_ID) + "]", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                            461, 5200462));
                    }

                    var cmbr = (CadieMagicBoxRandom)lc.Value;

                    if (cmbr == null)
                    {
                        throw new exception("[Channel::requestCadieCauldronExchange][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] valor retornado do sorteio is invalid(null)", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                            462, 5200463));
                    }

                    // Procura o item sorteado no IFF_STRUCT para ver se não foi colocado algum typeid errado na hora da criação desse item random do CadieCauldronExchange
                    var item_random = sIff.getInstance().findCommomItem(cmbr.item_random.ID);

                    if (item_random == null)
                    {
                        throw new exception("[Channel::requestCadieCauldronExchange][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] o item random[TYPEID=" + (cmbr.item_random.ID) + "] que esta no IFF_STRUCT do server nao existe no IFF do server, nao foi encontrado. Tem que colocar o item " + "no IFF ou foi colocado o TYPEID errado no IFF de random item CadieCauldronExchange. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                            463, 5200464));
                    }

                    bi.id = -1;
                    bi._typeid = cmbr.item_random.ID;
                    bi.qntd = cmbr.item_random.Qty;

                    if (item_random.Shop.flag_shop.time_shop.active)
                    {

                        if (item_random.Shop.flag_shop.time_shop.dia > 0)
                        {
                            bi.time = (short)item_random.Shop.flag_shop.time_shop.dia; // Quantidade de dias
                        }
                        else
                        {

                            // Verifica aqui por questão de segurança, mas tem que ter a type no IFF_STRUCT de tempo com a quantidade de dias
                            bi.time = (short)((cmb.Box_Random_ID == cadie_cauldron_Hermes_random_id || cmb.Box_Random_ID == cadie_cauldron_Jester_random_id || cmb.Box_Random_ID == cadie_cauldron_Twilight_random_id) ? 10 : 0); // Dias
                        }

                    }
                    else
                    {

                        // Verifica aqui por questão de segurança, mas tem que ter a type no IFF_STRUCT de tempo com a quantidade de dias
                        bi.time = (short)((cmb.Box_Random_ID == cadie_cauldron_Hermes_random_id || cmb.Box_Random_ID == cadie_cauldron_Jester_random_id || cmb.Box_Random_ID == cadie_cauldron_Twilight_random_id) ? 10 : 0);
                    }
                    // Fim de Sortea Item

                }
                else
                { // Normal Item

                    bi.id = -1;
                    bi._typeid = cmb.item_receive.ID;
                    bi.qntd = cmb.item_receive.Qty * safeExchangeCount;
                }

                // Limpa o item, para inicializar ele
                item = new stItem();

                ItemManager.initItemFromBuyItem(_session.m_pi,
                    item, bi, false, 0, 0, 1);

                // Verifica se é um setitem
                if (ItemManager.isSetItem(item._typeid))
                {
                    var v_stItem = ItemManager.getItemOfSetItem(_session,
                        item._typeid, false, 1);

                    if (!v_stItem.empty())
                    {
                        // Já verificou lá em cima se tem os item so set, então não precisa mais verificar aqui
                        // Só add eles ao List de venda
                        // Verifica se pode ter mais de 1 item e se não ver se não tem o item
                        foreach (var el in v_stItem)
                        {
                            if ((sIff.getInstance().IsCanOverlapped(el._typeid) && sIff.getInstance().getItemGroupIdentify(el._typeid) != IFF_GROUP.CAD_ITEM) || !_session.m_pi.ownerItem(el._typeid))
                            {
                                v_item.Add(new stItem(el));
                            }
                        }

                    }
                    else
                    {
                        throw new exception("[Channel::requestCadieCauldronExchange][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou trocar um set item que nao tem item, item typeid: " + (bi._typeid) + ". Hacker ou bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                            461, 0x5200062));
                    }
                }
                else
                {
                    v_item.Add(new stItem(item));
                }

                // Add item
                if (v_item.Count == 0)
                {
                    throw new exception("[Channel::requestCadieCauldronExchange][Error] problemas ao inicializar o item[TYPEID=" + (bi._typeid) + "] para o PLAYER [UID=" + (_session.m_pi.uid) + "]", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        459, 5200460));
                }

                var rai = ItemManager.addItem(v_item,
                    _session.getUID(), 0, 0);//aqui pode ter falhas criticas, depois ver o motivo

                if (rai.fails.Count > 0 && rai.type != ItemManager.RetAddItem.T_SUCCESS_PANG_AND_EXP_AND_CP_POUCH)
                {
                    throw new exception("[Channel::requestCadieCauldronExchange][Error] problemas ao adicionar o item[TYPEID=" + (bi._typeid) + "] para o PLAYER [UID=" + (_session.m_pi.uid) + "] ", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        460, 5200461));
                }

                // Verifica se é o Gacha Ticket Sub(Partial) e atualiza ele no server
                if (item._typeid == 0x1A000083)
                {
                    _session.m_pi.cg.partial_ticket += item.STDA_C_ITEM_QNTD;
                }

                //pega o que foi adicionado primeiro
                if (v_item.Count > 0) item = v_item[0];

                if (cmb.Box_Random_ID <= 0 && item._typeid != cmb.item_receive.ID)
                {
                    item._typeid = cmb.item_receive.ID;
                }

                // Add Item em Jogo
                if (rai.type != ItemManager.RetAddItem.T_SUCCESS_PANG_AND_EXP_AND_CP_POUCH)
                {
                    v_remove.AddRange(v_item);
                }

                // Att Item em Jogo
                p.init_plain(0x216);

                p.WriteUInt32((uint)UtilTime.GetSystemTimeAsUnix());
                p.WriteInt32(v_remove.Count); // Count

                foreach (var el in v_remove)
                {
                    p.WriteByte(el.type);
                    p.WriteUInt32(el._typeid);
                    p.WriteInt32(el.id);
                    p.WriteInt32(el.flag_time); // Time Tipo(ou type)
                    p.WriteBytes(el.stat.ToArray());
                    p.WriteInt32(el.STDA_C_ITEM_TIME > 0 ? el.STDA_C_ITEM_TIME : el.STDA_C_ITEM_QNTD); // qntd
                    p.WriteZeroByte(25);
                }

                packet_func.session_send(p,
                    _session, 1);

                // Resposta CadieMagicBox
                p.init_plain(0x22F);

                p.WriteUInt32(0); // OK

                p.WriteUInt32(seq);

                p.WriteUInt32(1); // Count receive item(ns)

                p.WriteUInt32(item._typeid);
                p.WriteInt32(item.id);
                p.WriteInt32(item.STDA_C_ITEM_QNTD);
                p.WriteInt32(item.stat.qntd_dep);
                p.WriteUInt32(item.flag_time);

                packet_func.session_send(p,
                    _session, 1);

                // UPDATE Achievement ON SERVER, DB and GAME 
                // Add +1 ao contador de troca de item no Cadie Cauldron
                sys_achieve.incrementCounter(0x6C400082u);

                sys_achieve.finish_and_update(_session);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestCadieCauldronExchange][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x22F);

                p.WriteUInt32((ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) == STDA_ERROR_TYPE.CHANNEL) ? ExceptionError.STDA_SYSTEM_ERROR_DECODE(e.getCodeError()) : 5200450); // Error

                packet_func.session_send(p,
                    _session, 0);
            }
        }

        public void requestCharacterStatsUp(Player _session, packet _packet)
        {
            //

            PangyaBinaryWriter p = new PangyaBinaryWriter();

            try
            {

                if (_session.m_pi.block_flag.m_flag.char_mastery)
                {
                    throw new exception("[Channel::requestCharacterStatsUp][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou upar Stats do character, mas ele nao pode. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        9, 0x790001));
                }

                uint stat = _packet.ReadUInt32();

                CharacterInfo ci = new CharacterInfo();

                ci.ToRead(_packet);

                var pCi = _session.m_pi.findCharacterById(ci.id);

                if (pCi == null || pCi._typeid != ci._typeid)
                {
                    throw new exception("[Channel::requestCharacterStatsUp][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou upar stat[value=" + (stat) + "] do Character[TYPEID=" + (ci._typeid) + ", ID=" + (ci.id) + "] que ele nao possui. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        500, 0x5200501));
                }

                var character = sIff.getInstance().findCharacter(pCi._typeid);

                if (character == null)
                {
                    throw new exception("[Channel::requestChracterStatsUp][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou upar stat[value=" + (stat) + "] do Character[TYPEID=" + (pCi._typeid) + ", ID=" + (pCi.id) + "], mas ele nao existe no IFF_STRUCT do server. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        504, 0x5200505));
                }

                sbyte value = 0;

                var value_part = ci.getSlotOfStatsFromCharEquipedPartItem((CharacterInfo.Stats)(stat));
                var value_auxpart = ci.getSlotOfStatsFromCharEquipedAuxPart((CharacterInfo.Stats)(stat));
                var value_set_effect_table = ci.getSlotOfStatsFromSetEffectTable((CharacterInfo.Stats)(stat));
                var value_card = ci.getSlotOfStatsFromCharEquipedCard((CharacterInfo.Stats)(stat));

                if (value_part == -1
                    || value_card == -1
                    || value_auxpart == -1
                    || value_set_effect_table == -1)
                {
                    throw new exception("[Channel::requestCharacterStatsUp][Error] PLAYER [UID=" + (_session.m_pi.uid) + "], stat[value=" + (stat) + "] is invalid. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        501, 0x5200502));
                }

                // Slot de Part Equiped
                value += (sbyte)value_part;

                // Slot de AuxPart Equiped
                value += (sbyte)value_auxpart;

                // Slot do Set Effect Table
                value += (sbyte)value_set_effect_table;

                // Slot de Card Equiped
                value += (sbyte)value_card;

                // Level + POWER, cada level da +1 de POWER
                if (stat == (int)CharacterInfo.Stats.S_POWER)
                {
                    value += (sbyte)((_session.m_pi.mi.level - 1) / 5);
                }

                var mastery = sIff.getInstance().findCharacterMastery(pCi._typeid);

                if (mastery.empty())
                {
                    throw new exception("[Channel::requestCharacterStatsUp][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou upar stat[value=" + (stat) + "] do Character[TYPEID=" + (pCi._typeid) + ", ID=" + (pCi.id) + "], mas nao tem o Character Mastery no IFF_STRUCT do server. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        505, 0x5200506));
                }

                if (mastery.Count < (uint)pCi.mastery)
                {
                    throw new exception("[Channel::requestCharacterStatsUp][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou upar stat[value=" + (stat) + "] do Character[TYPEID=" + (pCi._typeid) + ", ID=" + (pCi.id) + "], mas o CharacterMastery[value=" + (pCi.mastery) + ", List_size=" + (mastery.Count) + "] do player e invalido. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        506, 0x5200507));
                }

                // 1. Pegue o limite base do IFF para esse personagem (Quantos slots ele PODE ter no máximo)
                // Geralmente está no IFFCharacter.txt
                var bLimiteMaximoIFF = character.PCL[stat];

                // 2. Calcule o bônus adicional vindo de Mastery (se houver slots extras por mastery)
                sbyte slotsExtrasMastery = 0;
                for (var i = 0; i < pCi.mastery; ++i)
                {
                    if ((mastery[i].stats - 1) == stat)
                        slotsExtrasMastery++;
                }

                // O limite real de UPGRADE é o Base do IFF + o que ele ganhou de Mastery
                int limiteRealDeUpgrade = bLimiteMaximoIFF + slotsExtrasMastery + value;

                // 3. A validação correta: 
                if (pCi.pcl[stat] > limiteRealDeUpgrade)
                {
                    throw new exception("[Channel::requestCharacterStatsUp][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] atingiu o limite de slots para o stat " + stat,
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 502, 0x5200503));
                }

                uint enchant_typeid = ((Convert.ToUInt32(sIff.getInstance().ENCHANT) << 26) | (stat << 20)) + pCi.pcl[stat];

                var enchant = sIff.getInstance().findEnchant(enchant_typeid);

                if (enchant == null)
                {
                    throw new exception("[Channel::requestCharacterStatsUp][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou upar stats[stats=" + (stat) + "] do Character[ID=" + (ci.id) + "], mas nao encontrou o enchant[TYPEID=" + (enchant_typeid) + "]", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        503, 0x5200504));
                }

                _session.m_pi.consomePang((ulong)enchant.Pang);

                pCi.pcl[stat]++;

                // CmdUpdateCharacterPCL
                snmdb.NormalManagerDB.getInstance().add(7,
                    new CmdUpdateCharacterPCL(_session.m_pi.uid, pCi),
                    SQLDBResponse, this);


                // Atualiza Pang(s) no Jogo
                p.init_plain(0xC8);

                p.WriteUInt64(_session.m_pi.ui.pang);
                p.WriteInt64(enchant.Pang);

                packet_func.session_send(p,
                    _session, 1);

                // Atualiza Item no Jogo
                p.init_plain(0x216);

                p.WriteUInt32((uint)UtilTime.GetSystemTimeAsUnix());
                p.WriteUInt32(1); // Count

                p.WriteByte(0xC9);
                p.WriteUInt32(pCi._typeid);
                p.WriteInt32(pCi.id);
                p.WriteUInt32(0); // Flag Time
                p.WriteUInt32(0); // qntd ant
                p.WriteUInt32(0); // qntd dep
                p.WriteUInt32(0); // qntd
                p.WriteUInt16(pCi.pcl[(int)CharacterInfo.Stats.S_POWER]); // stats.PWR
                p.WriteUInt16(pCi.pcl[(int)CharacterInfo.Stats.S_CONTROL]); // stats.CTRL
                p.WriteUInt16(pCi.pcl[(int)CharacterInfo.Stats.S_ACCURACY]); // stats.ACCRY
                p.WriteUInt16(pCi.pcl[(int)CharacterInfo.Stats.S_SPIN]); // stats.SPIN
                p.WriteUInt16(pCi.pcl[(int)CharacterInfo.Stats.S_CURVE]); // stats.CURVE
                p.WriteZeroByte(15);

                packet_func.session_send(p,
                    _session, 1);

                // Resposta de Upar Stats Character
                p.init_plain(0x26F);

                p.WriteUInt32(0); // OK

                p.WriteUInt32(stat);

                packet_func.session_send(p,
                    _session, 1);

                // Update Achievement ON SERVER, DB and GAME
                AchievementSystem sys_achieve = new AchievementSystem();

                sys_achieve.incrementCounter(0x6C400084u);

                sys_achieve.finish_and_update(_session);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestCharacterStatsUp][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x26F);

                p.WriteUInt32((ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) == STDA_ERROR_TYPE.CHANNEL) ? ExceptionError.STDA_SYSTEM_ERROR_DECODE(e.getCodeError()) : 0x5200500);

                packet_func.session_send(p,
                    _session, 1);
            }
        }

        public void requestCharacterStatsDown(Player _session, packet _packet)
        {
            //

            PangyaBinaryWriter p = new PangyaBinaryWriter();

            try
            {





                if (_session.m_pi.block_flag.m_flag.char_mastery)
                {
                    throw new exception("[Channel::requestCharacterStatsDown][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou desupar Stats do character, mas ele nao pode. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        9, 0x790001));
                }

                uint stat = _packet.ReadUInt32();

                CharacterInfo ci = new CharacterInfo();

                ci.ToRead(_packet);

                var pCi = _session.m_pi.findCharacterById(ci.id);

                if (pCi == null || pCi._typeid != ci._typeid)
                {
                    throw new exception("[Channel::requestCharacterStatsDown][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou desupar o stat[value=" + (stat) + "] do Character[TYPEID=" + (ci._typeid) + ", ID=" + (ci.id) + "] que ele nao possui. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        550, 0x5200551));
                }

                var character = sIff.getInstance().findCharacter(pCi._typeid);

                if (character == null)
                {
                    throw new exception("[Channel::requestChracterStatsDown][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou desupar stat[value=" + (stat) + "] do Character[TYPEID=" + (pCi._typeid) + ", ID=" + (pCi.id) + "], mas ele nao existe no IFF_STRUCT do server. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        553, 0x5200554));
                }

                if (stat > (int)CharacterInfo.Stats.S_CURVE)
                {
                    throw new exception("[Channel::requestCharacterStatsDown][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou desupar um stat[value=" + (stat) + "] invalido do Character[ID=" + (pCi.id) + "]. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        551, 0x5200552));
                }

                if ((char)(pCi.pcl[stat] - 1) < 0)
                {
                    throw new exception("[Channel::requestCharacterStatsDown][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou desupar um stat[value=" + (stat) + "] do Character[ID=" + (pCi.id) + "] que ele nao tem mais valor upado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        552, 0x5200553));
                }

                pCi.pcl[stat]--;

                // Update on DB
                snmdb.NormalManagerDB.getInstance().add(7,
                    new CmdUpdateCharacterPCL(_session.m_pi.uid, pCi),
                    SQLDBResponse, this);

                // Atualiza item no Jogo
                p.init_plain(0x216);

                p.WriteUInt32((uint)UtilTime.GetSystemTimeAsUnix());
                p.WriteUInt32(1); // Count

                p.WriteByte(0xC9);
                p.WriteUInt32(pCi._typeid);
                p.WriteInt32(pCi.id);
                p.WriteUInt32(0); // Flag Time
                p.WriteUInt32(0); // qntd ant
                p.WriteUInt32(0); // qntd dep
                p.WriteUInt32(0); // qntd
                p.WriteUInt16(pCi.pcl[(int)CharacterInfo.Stats.S_POWER]); // stats.PWR
                p.WriteUInt16(pCi.pcl[(int)CharacterInfo.Stats.S_CONTROL]); // stats.CTRL
                p.WriteUInt16(pCi.pcl[(int)CharacterInfo.Stats.S_ACCURACY]); // stats.ACCRY
                p.WriteUInt16(pCi.pcl[(int)CharacterInfo.Stats.S_SPIN]); // stats.SPIN
                p.WriteUInt16(pCi.pcl[(int)CharacterInfo.Stats.S_CURVE]); // stats.CURVE
                p.WriteZeroByte(15);

                packet_func.session_send(p,
                    _session, 1);

                // Resposta de Downgrade Character Stats no Jogo
                p.init_plain(0x270);

                p.WriteUInt32(0); // OK
                p.WriteUInt32(stat);

                packet_func.session_send(p,
                    _session, 1);

                // Update Achievement ON SERVER, DB and GAME
                AchievementSystem sys_achieve = new AchievementSystem();

                sys_achieve.incrementCounter(0x6C400085u);

                sys_achieve.finish_and_update(_session);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestCharacterStatsDown][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x270);

                p.WriteUInt32((ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) == STDA_ERROR_TYPE.CHANNEL) ? ExceptionError.STDA_SYSTEM_ERROR_DECODE(e.getCodeError()) : 0x5200550);

                packet_func.session_send(p,
                    _session, 1);
            }
        }

        public void requestCharacterMasteryExpand(Player _session, packet _packet)
        {
            //

            PangyaBinaryWriter p = new PangyaBinaryWriter();

            try
            {





                if (_session.m_pi.block_flag.m_flag.char_mastery)
                {
                    throw new exception("[Channel::requestCharacterMasteryExpand][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou expandir o character mastery, mas ele nao pode. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        9, 0x790001));
                }

                uint char_typeid = _packet.ReadUInt32();
                int char_id = _packet.ReadInt32();

                var pCi = _session.m_pi.findCharacterById(char_id);

                if (pCi == null || pCi._typeid != char_typeid)
                {
                    throw new exception("[Channel::requestCharacterMasteryExpand][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou expandir Character[TYPEID=" + (char_typeid) + ", ID=" + (char_id) + "] mastery, mas ele nao possui o character. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        650, 0x5200651));
                }

                var mastery = sIff.getInstance().findCharacterMastery(char_typeid);

                if (mastery.empty())
                {
                    throw new exception("[Channel::requestCharacterMasteryExpand][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou expandir Character[TYPEID=" + (char_typeid) + ", ID=" + (char_id) + "] mastery, mas nao tem o character mastery no IFF_STRUCT do server. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        651, 0x5200652));
                }

                if ((uint)(pCi.mastery + 1) > mastery.Count)
                {
                    throw new exception("[Channel::requestCharacterMasteryExpand][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou expandir Character[TYPEID=" + (char_typeid) + ", ID=" + (char_id) + "] mastery, mas ele ja expandiu todos que é permitido. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        652, 0x5200653));
                }

                if (mastery[(int)pCi.mastery].seq != (pCi.mastery + 1))
                {
                    throw new exception("[Channel::requestCharacterMasteryExpand][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou expandir Character[TYPEID=" + (char_typeid) + ", ID=" + (char_id) + "] mastery, mas a sequencia do mastery no IFF_STRUCT é diferente. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        653, 0x5200654));
                }

                if ((char)mastery[(int)pCi.mastery].level > _session.m_pi.mi.level)
                {
                    throw new exception("[Channel::requestCharacterMasteryExpand][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou expandir Character[TYPEID=" + (char_typeid) + ", ID=" + (char_id) + "] mastery, mas nao tem level suficiente[have_lvl=" + (mastery[(int)pCi.mastery].level) + ", req_lvl=" + ((short)_session.m_pi.mi.level) + "]. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        654, 0x5200655));
                }

                List<stItem> v_item = new List<stItem>();
                stItem item = new stItem();

                var condition = mastery[(int)pCi.mastery].condition;

                for (var i = 0; i < 5; ++i)
                {

                    if (condition.condition[i] > 0)
                    {
                        switch (sIff.getInstance().getItemGroupIdentify(condition.condition[i]))
                        {
                            case IFF_GROUP.ITEM://case item
                                {
                                    var pWi = _session.m_pi.findWarehouseItemByTypeid(condition.condition[i]);

                                    if (pWi == null)
                                    {
                                        throw new exception("[Channel::requestCharacterMasteryExpand][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] nao tem o item da condicao.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                            656, 0x5200657));
                                    }

                                    if (pWi.STDA_C_ITEM_QNTD < (short)condition.qntd[i])
                                    {
                                        throw new exception("[Channel::requestCharacterMasteryExpand][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] o item nao tem quantidade suficiente para a condicao", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                            657, 0x5200658));
                                    }

                                    item = new stItem();

                                    item.type = 2;
                                    item._typeid = condition.condition[i];
                                    item.id = (int)pWi.id;
                                    item.qntd = (int)condition.qntd[i];
                                    item.STDA_C_ITEM_QNTD = (short)(item.qntd * -1);

                                    v_item.Add(new stItem(item));

                                    break;
                                }
                            case IFF_GROUP.QUEST_STUFF://case_quest_stuff
                                {
                                    var pQsi = _session.m_pi.mgr_achievement.findQuestStuffByTypeId(condition.condition[i]);

                                    if (pQsi == null)
                                    {
                                        throw new exception("[Channel::requestCharacterMasteryExpand][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] nao tem o QuestStuff da condicao", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                            658, 0x5200659));
                                    }

                                    if (!pQsi.isValid())
                                    {
                                        throw new exception("[Channel::requestCharacterMasteryExpand][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] o counter item da condicao esta inativo", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                            659, 0x5200660));
                                    }

                                    if (pQsi.counter_item_id == 0 || pQsi.clear_date_unix == 0)
                                    {
                                        throw new exception("[Channel::requestCharacterMasteryExpand][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] o QuestStuff[TYPEID=" + (pQsi._typeid) + "] nao foi concluido", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                            660, 0x5200661));
                                    }

                                    break;
                                }
                            default:
                                throw new exception("[Channel::requestCharacterMasteryExpand][Error] Unknown Condition[TYPEID=" + (condition.condition[i]) + ", QNTD=" + (condition.qntd[i]) + "]", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                    655, 0x5200656));
                        }
                    }
                }

                // Atualiza ON Server
                if (ItemManager.removeItem(v_item, _session) <= 0)
                {
                    throw new exception("[Channel::requestCharacterMasteryExpand][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] nao conseguiu excluir os item(ns) do player", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        661, 0x5200662));
                }

                // Atualiza mastery, add +1
                pCi.mastery++;

                item = new stItem();

                item._typeid = pCi._typeid;
                item.id = (int)pCi.id;
                item.type = 0xCD;
                item.flag = (byte)pCi.mastery;

                v_item.Add(new stItem(item));

                // Atualiza ON DB
                snmdb.NormalManagerDB.getInstance().add(9,
                    new CmdUpdateCharacterMastery(_session.m_pi.uid, pCi),
                    SQLDBResponse, this);

                // Atualiza ON Jogo
                p.init_plain(0x216);

                p.WriteUInt32((uint)UtilTime.GetSystemTimeAsUnix());
                p.WriteUInt32((uint)v_item.Count);

                foreach (var el in v_item)
                {
                    p.WriteByte(el.type);
                    p.WriteUInt32(el._typeid);
                    p.WriteInt32(el.id);
                    p.WriteUInt32(el.flag_time);
                    p.WriteInt32(el.stat.qntd_ant);
                    p.WriteInt32(el.stat.qntd_dep);
                    p.WriteInt32((el.STDA_C_ITEM_TIME > 0) ? el.STDA_C_ITEM_TIME : el.STDA_C_ITEM_QNTD);
                    p.WriteZeroByte(25); // 10 PCL[C0~C4] 2 Bytes cada, 15 bytes desconhecido
                    if (el.type == 0xCD)
                    {
                        p.WriteUInt32(el.flag); // Mastery
                    }
                }

                packet_func.session_send(p,
                    _session, 1);

                // Resposta do Character Mastery Expand
                p.init_plain(0x26E);

                p.WriteUInt32(0); // OK

                packet_func.session_send(p,
                    _session, 1);

                // UPDATE Achievement ON SERVER, DB and GAME
                AchievementSystem sys_achieve = new AchievementSystem();

                sys_achieve.incrementCounter(0x6C4000C3u);

                sys_achieve.finish_and_update(_session);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestCharacterMasteryExpand][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x26E);

                p.WriteUInt32(ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) == STDA_ERROR_TYPE.CHANNEL ? ExceptionError.STDA_SYSTEM_ERROR_DECODE(e.getCodeError()) : 0x5200650);

                packet_func.session_send(p,
                    _session, 1);
            }
        }

        public void requestCharacterCardEquip(Player _session, packet _packet)
        {
            //

            PangyaBinaryWriter p = new PangyaBinaryWriter();

            try
            {

                if (_session.m_pi.block_flag.m_flag.char_mastery)
                {
                    throw new exception("[Channel::requestCharacterCardEquip][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou equipar card no character, mas ele nao pode. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        9, 0x790001));
                }

                CardEquip ce = new CardEquip().ToRead(_packet);
                List<stItem> v_item = new List<stItem>();
                stItem item = new stItem();
                var card = sIff.getInstance().findCard(ce.card_typeid);

                if (card == null && card.ID == 0)
                {
                    throw new exception("[Channel::requestCharacterCardEquip][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou equipar card[TYPEID=" + (ce.card_typeid) + ", ID=" + (ce.card_id) + "] no Character[TYPEID=" + (ce.char_typeid) + ", ID=" + (ce.char_id) + "], mas o card nao existe no IFF_STRUCT do server. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        756, 0x5200757));
                }

                var pCi = _session.m_pi.findCharacterById(ce.char_id);

                if (pCi == null || pCi._typeid != ce.char_typeid)
                {
                    throw new exception("[Channel::requestCharacterCardEquip][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou equipar card[TYPEID=" + (ce.card_typeid) + ", ID=" + (ce.card_id) + "] no Character[TYPEID=" + (ce.char_typeid) + ", ID=" + (ce.char_id) + "], mas ele nao possui esse character. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        750, 0x5200751));
                }

                var pCardInfo = _session.m_pi.findCardById(ce.card_id);

                if (pCardInfo == null || pCardInfo._typeid != ce.card_typeid)
                {
                    throw new exception("[Channel::requestCharacterCardEquip][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou equipar card[TYPEID=" + (ce.card_typeid) + ", ID=" + (ce.card_id) + "] no Character[TYPEID=" + (ce.char_typeid) + ", ID=" + (ce.char_id) + "], mas ele nao possui esse card", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        751, 0x5200752));
                }

                // Verifica se o player está com shop aberto e se está vendendo o item no shop

                var r = m_rm.findRoom((short)_session.m_pi.mi.sala_numero);

                if (r != null && r.checkPersonalShopItem(_session, ce.card_id))
                {
                    throw new exception("[Channel::requestCharacterCardEquip][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou equipar o card[TYPEID=" + (ce.card_typeid) + ", ID=" + (ce.card_id) + "] no Character[TYPEID=" + (ce.char_typeid) + ", ID=" + (ce.char_id) + "], mas o card esta sendo vendido no Personal shop dele. Hacker ou Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        1010, 0x5201010));
                }



                item = new stItem();

                item.type = 2;
                item.id = pCardInfo.id;
                item._typeid = pCardInfo._typeid;
                item.qntd = 1;
                item.STDA_C_ITEM_QNTD = (short)(item.qntd * -1);

                v_item.Add(new stItem(item));

                if (ce.char_card_slot == 4 || ce.char_card_slot == 8) // Esse 2 só pode equipar com card patcher, mas o pacote é o 18B, e não esse
                {
                    throw new exception("[Channel::requestCharacterCardEquip][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou equipar card[TYPEID=" + (ce.card_typeid) + ", ID=" + (ce.card_id) + "] no Character[TYPEID=" + (ce.char_typeid) + ", ID=" + (ce.char_id) + "], mas o Slot do card requer um Club Patcher e um outro pacote do cliente. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        752, 0x5200753));
                }

                if (ce.char_card_slot == 7 && !pCi.isEquipedPartSlotThirdCaddieCardSlot()) // Esse aqui só pode equipar card se tiver o Part que libere esse Slot
                {
                    throw new exception("[Channel::requestCharacterCardEquip][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou equipar card[TYPEID=" + (ce.card_typeid) + ", ID=" + (ce.card_id) + "] no Character[TYPEID=" + (ce.char_typeid) + ", ID=" + (ce.char_id) + "], mas o Slot que ele tentou equipar precisa de um Part que libere ele, mas o player nao tem", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        753, 0x5200754));
                }

                switch (ce.char_card_slot)
                {
                    case 1:
                    case 2:
                    case 3:
                    case 4: // Character
                        if (sIff.getInstance().getItemSubGroupIdentify22(ce.card_typeid) != 0)
                        {
                            throw new exception("[Channel::requestCharacterCardEquip][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou equipar card[TYPEID=" + (ce.card_typeid) + ", ID=" + (ce.card_id) + "] no Character[TYPEID=" + (ce.char_typeid) + ", ID=" + (ce.char_id) + "] no Slot do Character[tipo=0], mas o card é tipo[value=" + (sIff.getInstance().getItemSubGroupIdentify22(ce.card_typeid)) + "]", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                755, 0x5200756));
                        }

                        if (pCi.Card_Character[(ce.char_card_slot - 1) % 4] != 0)
                        {
                            throw new exception("[Channel::requestCharacterCardEquipWithPatcher][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou equipar card[TYPEID=" + (ce.card_typeid) + ", ID=" + (ce.card_id) + "] no Character[TYPEID=" + (ce.char_typeid) + ", ID=" + (ce.char_id) + "] no Slot[value=" + (ce.char_card_slot) + "] mas ja tem card equipado[TYPEID=" + (pCi.Card_Character[(ce.char_card_slot - 1) % 4]) + "]", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                758, 0x5200759));
                        }

                        // Atualizar Card Character equipado
                        pCi.Card_Character[(ce.char_card_slot - 1) % 4] = ce.card_typeid;
                        break;
                    case 5:
                    case 6:
                    case 7:
                    case 8: // Caddie
                        if (sIff.getInstance().getItemSubGroupIdentify22(ce.card_typeid) != 1)
                        {
                            throw new exception("[Channel::requestCharacterCardEquip][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou equipar card[TYPEID=" + (ce.card_typeid) + ", ID=" + (ce.card_id) + "] no Character[TYPEID=" + (ce.char_typeid) + ", ID=" + (ce.char_id) + "] no Slot do Caddie[tipo=1], mas o card é tipo[value=" + (sIff.getInstance().getItemSubGroupIdentify22(ce.card_typeid)) + "]", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                755, 0x5200756));
                        }

                        if (pCi.Card_Caddie[(ce.char_card_slot - 1) % 4] != 0)
                        {
                            throw new exception("[Channel::requestCharacterCardEquipWithPatcher][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou equipar card[TYPEID=" + (ce.card_typeid) + ", ID=" + (ce.card_id) + "] no Character[TYPEID=" + (ce.char_typeid) + ", ID=" + (ce.char_id) + "] no Slot[value=" + (ce.char_card_slot) + "] mas ja tem card equipado[TYPEID=" + (pCi.Card_Caddie[(ce.char_card_slot - 1) % 4]) + "]", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                758, 0x5200759));
                        }

                        // Atualizar Card Caddie equipado
                        pCi.Card_Caddie[(ce.char_card_slot - 1) % 4] = ce.card_typeid;
                        break;
                    case 9:
                    case 10:
                    case 11:
                    case 12: // NPC
                        if (sIff.getInstance().getItemSubGroupIdentify22(ce.card_typeid) != 5)
                        {
                            throw new exception("[Channel::requestCharacterCardEquip][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou equipar card[TYPEID=" + (ce.card_typeid) + ", ID=" + (ce.card_id) + "] no Character[TYPEID=" + (ce.char_typeid) + ", ID=" + (ce.char_id) + "] no Slot do NPC[tipo=5], mas o card é tipo[value=" + (sIff.getInstance().getItemSubGroupIdentify22(ce.card_typeid)) + "]", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                755, 0x5200756));
                        }

                        if (pCi.Card_NPC[(ce.char_card_slot - 1) % 4] != 0)
                        {
                            throw new exception("[Channel::requestCharacterCardEquipWithPatcher][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou equipar card[TYPEID=" + (ce.card_typeid) + ", ID=" + (ce.card_id) + "] no Character[TYPEID=" + (ce.char_typeid) + ", ID=" + (ce.char_id) + "] no Slot[value=" + (ce.char_card_slot) + "] mas ja tem card equipado[TYPEID=" + (pCi.Card_NPC[(ce.char_card_slot - 1) % 4]) + "]", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                758, 0x5200759));
                        }

                        // Atualizar Card NPC equipado
                        pCi.Card_NPC[(ce.char_card_slot - 1) % 4] = ce.card_typeid;
                        break;
                    default:
                        throw new exception("[Channel::requestCharacterCardEquip][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou equipar card[TYPEID=" + (ce.card_typeid) + ", ID=" + (ce.card_id) + "] no Character[TYPEID=" + (ce.char_typeid) + ", ID=" + (ce.char_id) + "] em um Slot desconhecido. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                            754, 0x5200755));
                }

                if (ItemManager.removeItem(v_item, _session) <= 0)
                {
                    throw new exception("[Channel::requestCharacterCardEquip][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] nao conseguiu excluiu/(atualizar) item.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        757, 0x5200758));
                }

                // Update ON Server
                CardEquipInfoEx cei = new CardEquipInfoEx();

                cei.index = -1;
                cei._typeid = ce.card_typeid;
                cei.id = (uint)ce.card_id; // ce.card_id    // mas o pangya server original nao passa o id
                cei.efeito = card.Effect;
                cei.efeito_qntd = card.EffectValue;
                cei.slot = ce.char_card_slot;
                cei.tipo = sIff.getInstance().getItemSubGroupIdentify22(cei._typeid);
                cei.use_yn = 1;
                cei.parts_typeid = ce.char_typeid;
                cei.parts_id = (uint)ce.char_id;

                _session.m_pi.v_cei.Add(cei);

                item = new stItem();

                item.type = 0xCB;
                item.id = (int)pCi.id;
                item._typeid = pCi._typeid;
                item.price = cei._typeid;
                item.type_iff = (byte)cei.slot;

                v_item.Add(new stItem(item));

                // Update ON DB  
                snmdb.NormalManagerDB.getInstance().add(10, new CmdEquipCard(_session.m_pi.uid, cei, 0/*nao é card de tempo, é o normal*/), SQLDBResponse, this);

                _session.m_pi.UpdateCharacter(pCi.id, pCi);
                // Update ON Jogo
                p.init_plain(0x216);

                p.WriteUInt32((uint)UtilTime.GetSystemTimeAsUnix());
                p.WriteUInt32((uint)v_item.Count);

                foreach (var el in v_item)
                {
                    p.WriteByte(el.type);
                    p.WriteUInt32(el._typeid);
                    p.WriteInt32(el.id);
                    p.WriteUInt32(el.flag_time);
                    p.WriteInt32(el.stat.qntd_ant);
                    p.WriteInt32(el.stat.qntd_dep);
                    p.WriteInt32((el.STDA_C_ITEM_TIME > 0) ? el.STDA_C_ITEM_TIME : el.STDA_C_ITEM_QNTD);
                    p.WriteInt16(el.c);
                    p.WriteZeroByte(10);  // UCC IDX e outras coisas
                    p.WriteUInt32(el.price);      // Card typeid
                    p.WriteByte(el.type_iff);	// Card Slot

                }

                // Resposta para o Character Equip Card
                packet_func.session_send(p,
                    _session, 1);

                p.init_plain(0x271);

                p.WriteUInt32(0); // OK
                p.WriteUInt32(ce.card_typeid);

                packet_func.session_send(p,
                    _session, 1);

                // Update Achievement ON SERVER, DB and GAME
                AchievementSystem sys_achieve = new AchievementSystem();

                sys_achieve.incrementCounter(0x6C400087u);

                sys_achieve.finish_and_update(_session);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestCharacterCardEquip][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x271);

                p.WriteUInt32((ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) == STDA_ERROR_TYPE.CHANNEL) ? ExceptionError.STDA_SYSTEM_ERROR_DECODE(e.getCodeError()) : 0x5200750);

                packet_func.session_send(p,
                    _session, 1);
            }
        }

        public void requestCharacterCardEquipWithPatcher(Player _session, packet _packet)
        {
            //

            PangyaBinaryWriter p = new PangyaBinaryWriter();
            try
            {





                if (_session.m_pi.block_flag.m_flag.char_mastery)
                {
                    throw new exception("[Channel::requestCharacterCardEquipWithPatcher][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou equipar card no character com Patcher, mas ele nao pode. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        9, 0x790001));
                }

                CardEquip ce = new CardEquip().ToRead(_packet);
                List<stItem> v_item = new List<stItem>();
                stItem item = new stItem();

                var pWi = _session.m_pi.findWarehouseItemByTypeid(CLUB_PATCHER_TYPEID);

                if (pWi == null)
                {
                    throw new exception("[Channel::requestCharacterCardEquipWithPatcher][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou equipar card[TYPEID=" + (ce.card_typeid) + ", ID=" + (ce.card_id) + "] no Character[TYPEID=" + (ce.char_typeid) + ", ID=" + (ce.char_id) + "] com club Patcher mas ele nao tem o item. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        809, 0x5200810));
                }

                if (pWi.STDA_C_ITEM_QNTD < 1)
                {
                    throw new exception("[Channel::requestCharacterCardEquipWithPatcher][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou equipar card[TYPEID=" + (ce.card_typeid) + ", ID=" + (ce.card_id) + "] no Character[TYPEID=" + (ce.char_typeid) + ", ID=" + (ce.char_id) + "] com club Patcher mas ele nao tem quantidade suficiente do item. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        810, 0x5200811));
                }

                item = new stItem();

                item.type = 2;
                item.id = (int)pWi.id;
                item._typeid = pWi._typeid;
                item.qntd = 1;
                item.STDA_C_ITEM_QNTD = (short)(item.qntd * -1);

                v_item.Add(new stItem(item));

                var card = sIff.getInstance().findCard(ce.card_typeid);

                if (card == null)
                {
                    throw new exception("[Channel::requestCharacterCardEquipWithPatcher][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou equipar card[TYPEID=" + (ce.card_typeid) + ", ID=" + (ce.card_id) + "] no Character[TYPEID=" + (ce.char_typeid) + ", ID=" + (ce.char_id) + "], mas o card nao existe no IFF_STRUCT do server. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        806, 0x5200807));
                }

                var pCi = _session.m_pi.findCharacterById(ce.char_id);

                if (pCi == null || pCi._typeid != ce.char_typeid)
                {
                    throw new exception("[Channel::requestCharacterCardEquipWithPatcher][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou equipar card[TYPEID=" + (ce.card_typeid) + ", ID=" + (ce.card_id) + "] no Character[TYPEID=" + (ce.char_typeid) + ", ID=" + (ce.char_id) + "], mas ele nao possui esse character. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        800, 0x5200801));
                }

                var pCardInfo = _session.m_pi.findCardById(ce.card_id);

                if (pCardInfo == null || pCardInfo._typeid != ce.card_typeid)
                {
                    throw new exception("[Channel::requestCharacterCardEquipWithPatcher][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou equipar card[TYPEID=" + (ce.card_typeid) + ", ID=" + (ce.card_id) + "] no Character[TYPEID=" + (ce.char_typeid) + ", ID=" + (ce.char_id) + "], mas ele nao possui esse card", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        801, 0x5200802));
                }

                // Verifica se o player está com shop aberto e se está vendendo o item no shop

                var r = m_rm.findRoom((short)_session.m_pi.mi.sala_numero);

                if (r != null && r.checkPersonalShopItem(_session, ce.card_id))
                {
                    throw new exception("[Channel::requestCharacterCardEquipWithPatcher][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou equipar o card[TYPEID=" + (ce.card_typeid) + ", ID=" + (ce.card_id) + "] no Character[TYPEID=" + (ce.char_typeid) + ", ID=" + (ce.char_id) + "], mas o card esta sendo vendido no Personal shop dele. Hacker ou Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        1010, 0x5201010));
                }



                item = new stItem();

                item.type = 2;
                item.id = (int)pCardInfo.id;
                item._typeid = pCardInfo._typeid;
                item.qntd = 1;
                item.STDA_C_ITEM_QNTD = (short)(item.qntd * -1);

                v_item.Add(new stItem(item));

                if (ce.char_card_slot != 4 && ce.char_card_slot != 8) // Esse só pode equipar esses 2 Slot que usa 1 Club Patcher
                {
                    throw new exception("[Channel::requestCharacterCardEquipWithPatcher][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou equipar card[TYPEID=" + (ce.card_typeid) + ", ID=" + (ce.card_id) + "] no Character[TYPEID=" + (ce.char_typeid) + ", ID=" + (ce.char_id) + "], so pode equipar o card no slot 4 ou 8, reg_value=" + (ce.char_card_slot) + ". Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        802, 0x5200803));
                }

                switch (ce.char_card_slot)
                {
                    case 1:
                    case 2:
                    case 3:
                    case 4: // Character
                        if (sIff.getInstance().getItemSubGroupIdentify22(ce.card_typeid) != 0)
                        {
                            throw new exception("[Channel::requestCharacterCardEquipWithPatcher][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou equipar card[TYPEID=" + (ce.card_typeid) + ", ID=" + (ce.card_id) + "] no Character[TYPEID=" + (ce.char_typeid) + ", ID=" + (ce.char_id) + "] no Slot do Character[tipo=0], mas o card é tipo[value=" + (sIff.getInstance().getItemSubGroupIdentify22(ce.card_typeid)) + "]", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                805, 0x5200806));
                        }

                        if (pCi.Card_Character[(ce.char_card_slot - 1) % 4] != 0)
                        {
                            throw new exception("[Channel::requestCharacterCardEquipWithPatcher][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou equipar card[TYPEID=" + (ce.card_typeid) + ", ID=" + (ce.card_id) + "] no Character[TYPEID=" + (ce.char_typeid) + ", ID=" + (ce.char_id) + "] no Slot[value=" + (ce.char_card_slot) + "] mas ja tem card equipado[TYPEID=" + (pCi.Card_Character[(ce.char_card_slot - 1) % 4]) + "]", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                811, 0x5200812));
                        }

                        // Atualizar Card Character equipado
                        pCi.Card_Character[(ce.char_card_slot - 1) % 4] = ce.card_typeid;
                        break;
                    case 5:
                    case 6:
                    case 7:
                    case 8: // Caddie
                        if (sIff.getInstance().getItemSubGroupIdentify22(ce.card_typeid) != 1)
                        {
                            throw new exception("[Channel::requestCharacterCardEquipWithPatcher][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou equipar card[TYPEID=" + (ce.card_typeid) + ", ID=" + (ce.card_id) + "] no Character[TYPEID=" + (ce.char_typeid) + ", ID=" + (ce.char_id) + "] no Slot do Caddie[tipo=1], mas o card é tipo[value=" + (sIff.getInstance().getItemSubGroupIdentify22(ce.card_typeid)) + "]", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                805, 0x5200806));
                        }

                        if (pCi.Card_Caddie[(ce.char_card_slot - 1) % 4] != 0)
                        {
                            throw new exception("[Channel::requestCharacterCardEquipWithPatcher][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou equipar card[TYPEID=" + (ce.card_typeid) + ", ID=" + (ce.card_id) + "] no Character[TYPEID=" + (ce.char_typeid) + ", ID=" + (ce.char_id) + "] no Slot[value=" + (ce.char_card_slot) + "] mas ja tem card equipado[TYPEID=" + (pCi.Card_Caddie[(ce.char_card_slot - 1) % 4]) + "]", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                811, 0x5200812));
                        }

                        // Atualizar Card Caddie equipado
                        pCi.Card_Caddie[(ce.char_card_slot - 1) % 4] = ce.card_typeid;
                        break;
                    case 9:
                    case 10:
                    case 11:
                    case 12: // NPC
                        throw new exception("[Channel::requestCharacterCardEquipWithPatcher][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou equipar card[TYPEID=" + (ce.card_typeid) + ", ID=" + (ce.card_id) + "] no Character[TYPEID=" + (ce.char_typeid) + ", ID=" + (ce.char_id) + "] em no Slot NPC, mas nao pode, o slot do Club Patcher é so Character e Caddie. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                            803, 0x5200804));
                    default:
                        throw new exception("[Channel::requestCharacterCardEquipWithPatcher][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou equipar card[TYPEID=" + (ce.card_typeid) + ", ID=" + (ce.card_id) + "] no Character[TYPEID=" + (ce.char_typeid) + ", ID=" + (ce.char_id) + "] em um Slot desconhecido. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                            804, 0x5200805));
                }

                if (ItemManager.removeItem(v_item, _session) <= 0)
                {
                    throw new exception("[Channel::requestCharacterCardEquipWithPatcher][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] nao conseguiu excluiu/(atualizar) item.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        807, 0x5200808));
                }

                // Update ON Server
                CardEquipInfoEx cei = new CardEquipInfoEx
                {
                    index = -1,
                    _typeid = ce.card_typeid,
                    id = (uint)ce.card_id, // ce.card_id    // mas o pangya server original nao passa o id
                    efeito = card.Effect,
                    efeito_qntd = card.EffectValue,
                    slot = ce.char_card_slot
                };
                cei.tipo = sIff.getInstance().getItemSubGroupIdentify22(cei._typeid);
                cei.use_yn = 1;
                cei.parts_typeid = ce.char_typeid;
                cei.parts_id = (uint)ce.char_id;

                _session.m_pi.v_cei.Add(cei);

                item = new stItem();

                item.type = 0xCB;
                item.id = (int)pCi.id;
                item._typeid = pCi._typeid;
                item.price = cei._typeid;
                item.type_iff = (byte)cei.slot;

                v_item.Add(new stItem(item));

                // Update ON DB
                snmdb.NormalManagerDB.getInstance().add(10, new CmdEquipCard(_session.m_pi.uid, cei, 0), SQLDBResponse, this);

                _session.m_pi.UpdateCharacter(pCi.id, pCi);

                _session.m_pi.ei.char_info = pCi;
                // Update ON Jogo
                p.init_plain(0x216);

                p.WriteUInt32((uint)UtilTime.GetSystemTimeAsUnix());
                p.WriteUInt32((uint)v_item.Count);

                foreach (var el in v_item)
                {
                    p.WriteByte(el.type);
                    p.WriteUInt32(el._typeid);
                    p.WriteInt32(el.id);
                    p.WriteUInt32(el.flag_time);
                    p.WriteInt32(el.stat.qntd_ant);
                    p.WriteInt32(el.stat.qntd_dep);
                    p.WriteInt32((el.STDA_C_ITEM_TIME > 0) ? el.STDA_C_ITEM_TIME : el.STDA_C_ITEM_QNTD);
                    p.WriteInt16(el.c);
                    p.WriteZero(10);  // UCC IDX e outras coisas
                    p.WriteUInt32(el.price);      // Card typeid
                    p.WriteByte(el.type_iff);	// Card Slot
                }

                // Resposta para o Character Equip Card With Club Patcher
                packet_func.session_send(p,
                    _session, 1);

                p.init_plain(0x272);

                p.WriteUInt32(0); // OK
                p.WriteUInt32(ce.card_typeid);

                packet_func.session_send(p,
                    _session, 1);

                // Update Achievement ON SERVER, DB and GAME
                AchievementSystem sys_achieve = new AchievementSystem();

                sys_achieve.incrementCounter(0x6C400087u);

                sys_achieve.finish_and_update(_session);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestCharacterCardEquipWithPatcher][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x272);

                p.WriteUInt32((ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) == STDA_ERROR_TYPE.CHANNEL) ? ExceptionError.STDA_SYSTEM_ERROR_DECODE(e.getCodeError()) : 0x5200800);

                packet_func.session_send(p,
                    _session, 1);
            }
        }

        public void requestCharacterRemoveCard(Player _session, packet _packet)
        {
            //

            PangyaBinaryWriter p = new PangyaBinaryWriter();

            try
            {





                if (_session.m_pi.block_flag.m_flag.char_mastery)
                {
                    throw new exception("[Channel::requestCharacterRemoveCard][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou remover card do character, mas ele nao pode. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        9, 0x790001));
                }

                CardRemove cr = new CardRemove().ToRead(_packet);
                List<stItem> v_item = new List<stItem>();
                stItem item = new stItem();
                BuyItem bi = new BuyItem();

                var pCi = _session.m_pi.findCharacterById(cr.char_id);

                if (pCi == null || pCi._typeid != cr.char_typeid)
                {
                    throw new exception("[Channel::requestCharacterRemoveCard][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou remover card[Slot=" + (cr.card_slot) + "] do Character[TYPEID=" + (cr.char_typeid) + ", ID=" + (cr.char_id) + "], mas o ele nao possui esse character. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        850, 0x5200851));
                }

                var pWi = _session.m_pi.findWarehouseItemById(cr.removedor_id);

                if (pWi == null || pWi._typeid != cr.removedor_typeid)
                {
                    throw new exception("[Channel::requestCharacterRemoveCard][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou remover card[Slot=" + (cr.card_slot) + "] do Character[TYPEID=" + (cr.char_typeid) + ", ID=" + (cr.char_id) + "], mas ele nao possui o removedor[TYPEID=" + (cr.removedor_typeid) + ", ID=" + (cr.removedor_id) + "] de card. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        851, 0x5200852));
                }

                if (pWi.STDA_C_ITEM_QNTD < 1)
                {
                    throw new exception("[Channel::requestCharacterRemoveCard][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou remover card[Slot=" + (cr.card_slot) + "] do Character[TYPEID=" + (cr.char_typeid) + ", ID=" + (cr.char_id) + "], mas ele nao quantidade suficiente do removedor[TYPEID=" + (cr.removedor_typeid) + ", ID=" + (cr.removedor_id) + "] de card. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        854, 0x5200855));
                }

                switch (cr.card_slot)
                {
                    case 1:
                    case 2:
                    case 3:
                    case 4: // Character
                        if (pCi.Card_Character[(cr.card_slot - 1) % 4] == 0)
                        {
                            throw new exception("[Channel::requestCharacterRemoveCard][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou remover card[Slot=" + (cr.card_slot) + "] do Character[TYPEID=" + (cr.char_typeid) + ", ID=" + (cr.char_id) + "], mas nao tem nenhum card equipado nesse Slot. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                853, 0x5200854));
                        }

                        bi.id = -1;
                        bi.qntd = 1;
                        bi._typeid = pCi.Card_Character[(cr.card_slot - 1) % 4];

                        // Atualiza card equiped Slot
                        pCi.Card_Character[(cr.card_slot - 1) % 4] = 0;
                        break;
                    case 5:
                    case 6:
                    case 7:
                    case 8: // Caddie
                        if (pCi.Card_Caddie[(cr.card_slot - 1) % 4] == 0)
                        {
                            throw new exception("[Channel::requestCharacterRemoveCard][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou remover card[Slot=" + (cr.card_slot) + "] do Character[TYPEID=" + (cr.char_typeid) + ", ID=" + (cr.char_id) + "], mas nao tem nenhum card equipado nesse Slot. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                853, 0x5200854));
                        }

                        bi.id = -1;
                        bi.qntd = 1;
                        bi._typeid = pCi.Card_Caddie[(cr.card_slot - 1) % 4];

                        // Atualiza card equiped Slot
                        pCi.Card_Caddie[(cr.card_slot - 1) % 4] = 0;
                        break;
                    case 9:
                    case 10:
                    case 11:
                    case 12: // NPC
                        if (pCi.Card_NPC[(cr.card_slot - 1) % 4] == 0)
                        {
                            throw new exception("[Channel::requestCharacterRemoveCard][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou remover card[Slot=" + (cr.card_slot) + "] do Character[TYPEID=" + (cr.char_typeid) + ", ID=" + (cr.char_id) + "], mas nao tem nenhum card equipado nesse Slot. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                853, 0x5200854));
                        }

                        bi.id = -1;
                        bi.qntd = 1;
                        bi._typeid = pCi.Card_NPC[(cr.card_slot - 1) % 4];

                        // Atualiza card equiped Slot
                        pCi.Card_NPC[(cr.card_slot - 1) % 4] = 0;
                        break;
                    default:
                        throw new exception("[Channel::requestCharacterRemoveCard][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou remover card[Slot=" + (cr.card_slot) + "] do Character[TYPEID=" + (cr.char_typeid) + ", ID=" + (cr.char_id) + "], mas o slot é deconhecido. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                            852, 0x5200853));
                }

                // Update ON Server
                var pCei = _session.m_pi.findCardEquipedByTypeid(bi._typeid,
                    (int)cr.char_typeid, (int)cr.card_slot);

                if (pCei == null)
                {
                    throw new exception("[Channel::requestCharacterRemoveCard][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou remover card[Slot=" + (cr.card_slot) + "] do Character[TYPEID=" + (cr.char_typeid) + ", ID=" + (cr.char_id) + "], mas nao tem o card equipado no List de cards equipado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        0x857, 0x5200858));
                }

                item = new stItem();

                item.type = 2;
                item._typeid = pWi._typeid;
                item.id = (int)pWi.id;
                item.qntd = 1;
                item.STDA_C_ITEM_QNTD = (short)(item.qntd * -1);

                // Remove Card Removedor Item
                if (ItemManager.removeItem(item, _session) <= 0)
                {
                    throw new exception("[Channel::requestCharacterRemoveCard][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] nao conseguiu excluir/(atualizar qntd) item[TYPEID=" + (item._typeid) + ", ID=" + (item.id) + "]", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        858, 0x5200859));
                }

                v_item.Add(new stItem(item));

                item = new stItem();

                ItemManager.initItemFromBuyItem(_session.m_pi,
                    item, bi, false, 0, 0, 1);

                if (item._typeid == 0)
                {
                    throw new exception("[Channel::requestCharacterRemoveCard][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] nao conseguiu initializar item[TYPEID=" + (bi._typeid) + "]", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        855, 0x5200856));
                }

                // Add Card Desequipado
                var rt = ItemManager.RetAddItem.T_INIT_VALUE;

                if ((rt = ItemManager.addItem(item,
                    _session, 0, 0)) < 0)
                {
                    throw new exception("[Channel::requestCharacterRemoveCard][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] nao conseguiu adicionar item[TYPEID=" + (item._typeid) + "]", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        856, 0x5200857));
                }

                if (rt != ItemManager.RetAddItem.T_SUCCESS_PANG_AND_EXP_AND_CP_POUCH)
                {
                    v_item.Add(new stItem(item));
                }

                item = new stItem();

                item.type = 0xCB;
                item._typeid = pCi._typeid;
                item.id = (int)pCi.id;
                item.price = 0; // Card Typeid, 0 desequipa
                item.type_iff = (byte)cr.card_slot;

                v_item.Add(new stItem(item));

                // Update ON DB
                snmdb.NormalManagerDB.getInstance().add(11,
                    new CmdRemoveEquipedCard(_session.m_pi.uid, pCei),
                    SQLDBResponse, this);

                // Remove Equiped Card
                // 
                var it = _session.m_pi.v_cei.FirstOrDefault(_el =>
                {
                    return _el.id == bi._typeid && _el.parts_id == cr.char_id && _el.slot == cr.card_slot;
                });

                if (it != null)
                {
                    _session.m_pi.v_cei.Remove(it);
                }
                _session.m_pi.UpdateCharacter(pCi.id, pCi);

                // Update ON Jogo
                p.init_plain(0x216);

                p.WriteUInt32((uint)UtilTime.GetSystemTimeAsUnix());
                p.WriteUInt32((uint)v_item.Count);

                foreach (var el in v_item)
                {
                    p.WriteByte(el.type);
                    p.WriteUInt32(el._typeid);
                    p.WriteInt32(el.id);
                    p.WriteUInt32(el.flag_time);
                    p.WriteBytes(el.stat.ToArray());
                    p.WriteInt32((el.STDA_C_ITEM_TIME > 0) ? el.STDA_C_ITEM_TIME : el.STDA_C_ITEM_QNTD);
                    p.WriteInt16(el.c);
                    p.WriteZeroByte(10); // UCC IDX e outras coisas
                    p.WriteUInt32(el.price); // Card Typeid
                    p.WriteByte(el.type_iff); // Card Slot
                }

                packet_func.session_send(p,
                    _session, 1);

                // Reposta do Character Remove Card
                p.init_plain(0x273);

                p.WriteUInt32(0); // OK
                p.WriteUInt32(bi._typeid);

                packet_func.session_send(p,
                    _session, 1);

                // Update Achievement ON SERVER, DB and GAME
                AchievementSystem sys_achieve = new AchievementSystem();

                sys_achieve.incrementCounter(0x6C400088u);

                sys_achieve.finish_and_update(_session);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestCharacterRemoveCard][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x273);

                p.WriteUInt32((ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) == STDA_ERROR_TYPE.CHANNEL) ? ExceptionError.STDA_SYSTEM_ERROR_DECODE(e.getCodeError()) : 0x5200850);

                packet_func.session_send(p,
                    _session, 1);
            }
        }


        public void requestOpenClubWorkShopEvent(Player _session, packet _packet)
        {            
        }

        public void requestClubWorkShopEventCount(Player _session, packet _packet)
        { 
        }

        public void requestClubSetStatsUpdate(Player _session, packet _packet)
        {
            //

            PangyaBinaryWriter p = new PangyaBinaryWriter();

            try
            {

                byte opt = _packet.ReadUInt8();
                byte stat = _packet.ReadUInt8();
                int item_id = _packet.ReadInt32();





                if (opt == 1 || opt == 3)
                { // ClubSet Up/Downgrade

                    AchievementSystem sys_achieve = new AchievementSystem();

                    var pWi = _session.m_pi.findWarehouseItemById(item_id);

                    if (pWi == null)
                    {
                        throw new exception("[Channel::requestClubSetStatsUpdate][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou " + (opt == 1 ? "updar" : "desupar") + " stat[value=" + ((ushort)stat) + "] do ClubSet[ID=" + (item_id) + "] que ele nao possui. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                            600, 0x5200601));
                    }

                    if (stat > (int)CharacterInfo.Stats.S_CURVE)
                    {
                        throw new exception("[Channel::requestClubSetStatsUpdate][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou " + (opt == 1 ? "updar" : "desupar") + " um stat[value=" + ((ushort)stat) + "] que nao existe do ClubSet[ID=" + (item_id) + "]. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                            604, 0x5200605));
                    }

                    var clubset = sIff.getInstance().findClubSet(pWi._typeid);

                    if (clubset == null)
                    {
                        throw new exception("[Channel::requestClubSetStatsUpdate][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou " + (opt == 1 ? "updar" : "desupar") + " stat[value=" + ((ushort)stat) + "] do ClubSet[ID=" + (item_id) + "] que nao existe no IFF_STRUCT do server. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                            601, 0x5200602));
                    }

                    if (opt == 1)
                    { // UPGRADE

                        if (((clubset.SlotStats.getSlot[stat] - clubset.Stats.getSlot[stat]) + pWi.clubset_workshop.c[stat]) < (pWi.c[stat] + 1))
                        {
                            throw new exception("[Channel::requestClubSetStatsUpdate][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou upar stat[value=" + ((ushort)stat) + "] do ClubSet[ID=" + (item_id) + "], mas ele ja upou todos os slot's disponiveis. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                602, 0x5200603));
                        }

                        uint enchant_typeid = (uint)((sIff.getInstance().ENCHANT << 26) | (stat << 20) + pWi.c[stat]);

                        var enchant = sIff.getInstance().findEnchant(enchant_typeid);

                        if (enchant == null)
                        {
                            throw new exception("[Channel::requestClubSetStatsUpdate][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou upar stat[value=" + ((ushort)stat) + "] do ClubSet[ID=" + (item_id) + "], mas nao tem o enchant[TYPEID=" + (enchant_typeid) + "] no IFF_STRUCT do server. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                603, 0x5200604));
                        }

                        _session.m_pi.consomePang((ulong)enchant.Pang);

                        // Update ON Server
                        pWi.c[stat]++;

                        // Update ON DB
                        snmdb.NormalManagerDB.getInstance().add(8,
                            new CmdUpdateClubSetStats(_session.m_pi.uid,
                                pWi, (uint)enchant.Pang),
                            SQLDBResponse, this);

                        // Update Achievement ON SERVER, DB and GAME
                        sys_achieve.incrementCounter(0x6C400084u);

                        // Update ON Game
                        p.init_plain(0xA5);

                        p.WriteByte(opt / 2 + 1); // [0, 1] / 2 + 1 = 1, [2, 3] / 2 + 1 = 2    // UPA = 1, DESUPA = 2
                        p.WriteByte(opt % 2); // [0, 2] mod 2 = 0, [1, 3] mod 2 = 1        // Character = 0, ClubSet = 1
                        p.WriteByte(stat);
                        p.WriteInt32(item_id);
                        p.WriteInt64(enchant.Pang);

                        packet_func.session_send(p,
                            _session, 1);

                    }
                    else if (opt == 3)
                    { // DOWNGRADE

                        if ((pWi.c[stat] - 1) < 0)
                        {
                            throw new exception("[Channel::requestClubSetStatsUpdate][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou desupar stat[value=" + ((ushort)stat) + "] do ClubSet[ID=" + (item_id) + "], mas ele ja desupou tudo que podia. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                605, 0x5200606));
                        }

                        // Update ON Server
                        pWi.c[stat]--;

                        // Update ON DB
                        snmdb.NormalManagerDB.getInstance().add(8,
                            new CmdUpdateClubSetStats(_session.m_pi.uid,
                                pWi, 0),
                            SQLDBResponse, this);

                        // Update Achievement ON SERVER, DB and GAME
                        sys_achieve.incrementCounter(0x6C400085u);

                        // Update ON Game
                        p.init_plain(0xA5);

                        p.WriteByte(opt / 2 + 1); // [0, 1] / 2 + 1 = 1, [2, 3] / 2 + 1 = 2    // UPA = 1, DESUPA = 2
                        p.WriteByte(opt % 2); // [0, 2] mod 2 = 0, [1, 3] mod 2 = 1        // Character = 0, ClubSet = 1
                        p.WriteByte(stat);
                        p.WriteInt32(item_id);
                        p.WriteUInt64(0);

                        packet_func.session_send(p,
                            _session, 1);
                    }

                    // Update Achievement ON SERVER, DB and GAME
                    sys_achieve.finish_and_update(_session);
                } // OPT [0 OR 2] é Character Stats para season passada

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestCharacterStatsUpdate][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0xA5);

                p.WriteByte(0); // Error

                packet_func.session_send(p,
                    _session, 1);
            }
        }

        public void requestTikiShopExchangeItem(Player _session, packet _packet)
        {

            PangyaBinaryWriter p = new PangyaBinaryWriter();
            try
            {

                ulong pang = 0Ul;
                uint milage = 0;
                uint tiki_pts = 0;
                uint bonus = 0;
                uint bonus_prob = 0;
                uint[] bonus_minmax = new uint[2];

                // Log String Item
                string s_item = "";

                List<stItem> v_item = new List<stItem>();
                stItem item = new stItem();

                // Achievement System
                AchievementSystem sys_achieve = new AchievementSystem();

                // Milage Pts
                item.type = 2;
                item.id = -1;
                item._typeid = MILAGE_POINT_TYPEID;
                item.qntd = 0;
                item.STDA_C_ITEM_QNTD = 0;

                var pWi = _session.m_pi.findWarehouseItemByTypeid(MILAGE_POINT_TYPEID); // Milage pts

                if (pWi != null && pWi.id != int.MaxValue)
                {
                    item.id = pWi.id;
                    item.qntd = (item.STDA_C_ITEM_QNTD = pWi.STDA_C_ITEM_QNTD);
                }

                uint count = _packet.ReadUInt32();

                if (count == 0 || count > 5)
                    throw new exception(
                        $"[[Channel::requestTikiShopExchangeItem][CHEAT] UID={_session.m_pi.uid} count={count}",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 450, 5200451)
                    );

                if (_packet.BytesRemaining < count * 8)
                    throw new exception(
                        $"[[Channel::requestTikiShopExchangeItem][CHEAT] pacote truncado UID={_session.m_pi.uid}",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 450, 5200452)
                    );

                var r = m_rm.findRoom((short)_session.m_pi.mi.sala_numero);

                for (var i = 0; i < count; ++i)
                {

                    var tsei = new TikiShopExchangeItem().ToRead(_packet);

                    var _item = ItemManager.exchangeTikiShop(_session,
                          tsei._typeid, tsei.id,
                          tsei.qntd);

                    if (_item.Count == 0)
                    {
                        throw new exception("[Channel::requestTikiShopExchangeItem][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou trocar item[TYPEID=" + (tsei._typeid) + ", ID=" + (tsei.id) + ", QNTD=" + (tsei.qntd) + "] no Tiki's Shop, mas nao conseguiu inicializar o item. Hacker ou Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                            900, 0x52000901));
                    }

                    // Verifica se o player está com shop aberto e se está vendendo o item no shop 
                    if (r != null && r.checkPersonalShopItem(_session, tsei.id))
                    {
                        throw new exception("[Channel::requestTikiShopExchangeItem][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou trocar item[TYPEID=" + (tsei._typeid) + ", ID=" + (tsei.id) + ", QNTD=" + (tsei.qntd) + "] no Tiki's Shop, mas o item esta sendo vendido no Personal shop dele. Hacker ou Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                            1010, 0x5201010));
                    }

                    var @base = sIff.getInstance().findCommomItem(tsei._typeid);

                    if (@base == null || @base.ID == 0)
                    {
                        throw new exception("[Channel::requestTikiShopExchangeItem][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou trocar item[TYPEID=" + (tsei._typeid) + ", ID=" + (tsei.id) + "] no Tiki's Shop, mas o item nao existe no IFF_STRUCT do Server. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                            901, 0x5200902));
                    }

                    if (@base.ID != 0 && !@base.tiki.IsActived())
                    {
                        throw new exception("[Channel::requestTikiShopExchangeItem][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou trocar item[TYPEID=" + (tsei._typeid) + ", ID=" + (tsei.id) + "] no Tiki's Shop, mas o item nao é valido para ser trocado. Hacker ou Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                            904, 0x5200905));
                    }

                    // Soma dados de tiki dos itens
                    pang += @base.tiki.Tiki_Pang;

                    milage += @base.tiki.Mileage_Pts * tsei.qntd;

                    bonus_minmax[0] += (uint)@base.tiki.Bonus[0];
                    bonus_minmax[1] += (uint)@base.tiki.Bonus[1];
                    bonus_prob = @base.tiki.Bonus_Prob;

                    v_item.AddRange(_item);

                    // Achievement Add +1 ao contador de tipo de item que foi trocado no Tiki Shop Exchange
                    switch (@base.tiki.Type_TikiShop)
                    {
                        case 1: // Normal
                            sys_achieve.incrementCounter(0x6C4000BEu);
                            break;
                        case 2: // Cookie(CP)
                            sys_achieve.incrementCounter(0x6C4000BFu);
                            break;
                        case 3: // Rare
                            sys_achieve.incrementCounter(0x6C4000C0u);
                            break;
                    }
                    // Zera IDs para a log string do novo item
                    var s_ids = "";

                    for (int ii = 0; ii < v_item.Count; ++ii)
                    {
                        s_ids += (ii == 0 ? "" : ", ") + v_item[ii].id;//ta vindo negativo...
                    }

                    s_item += (i == 0 ? "" : ", ") + "[TYPEID=" + tsei._typeid + ", ID(s)={" + s_ids + "}, QNTD=" + tsei.qntd + ", TIPO(Normal, CP, Rare)=" + @base.tiki.Type_TikiShop + "]";
                }

                // Bonus
                uint index = (uint)new Random().Next() % (bonus_prob * 3 + 1);

                if (index < bonus_prob)
                {
                    bonus = (((uint)new Random().Next()) % (bonus_minmax[1] - bonus_minmax[0])) + bonus_minmax[0];

                    // Achievement Add +1 ao contador de Tiki Shop Exchange Bonus Milage
                    sys_achieve.incrementCounter(0x6C4000C1u);
                }
                // Fim Bonus

                if (ItemManager.removeItem(v_item, _session) <= 0)
                {
                    throw new exception("[Channel::requestTikiShopExchangeItem][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou trocar item(ns)(" + s_item + "), mas nao conseguiu deletar ele(s).", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        902, 0x5200903));
                }


                // Tiki Points
                if ((milage + item.qntd + bonus) > 1000)
                {
                    tiki_pts = (milage + (uint)item.qntd + bonus) / 1000;
                }

                // Att Qntd Milage
                item.STDA_C_ITEM_QNTD = (short)((int)((milage + item.qntd + bonus) % 1000) - (int)item.qntd);
                item.qntd = Math.Abs(item.STDA_C_ITEM_QNTD);

                // Só atualiza o Milage Points se for diferente de 0 a quantidade
                if (item.STDA_C_ITEM_QNTD != 0)
                {
                    var rt = ItemManager.RetAddItem.T_INIT_VALUE;

                    if ((rt = ItemManager.addItem(item,
                        _session, 0, 0)) < 0)
                    {
                        throw new exception("[Channel::requestTikiShopExchangeItem][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou adicionar item[TYPEID=" + (item._typeid) + ", ID=" + (item.id) + ", QNTD=" + (item.STDA_C_ITEM_QNTD) + "], mas nao conseguiu.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                            903, 0x5200904));
                    }

                    if (rt != ItemManager.RetAddItem.T_SUCCESS_PANG_AND_EXP_AND_CP_POUCH)
                    {
                        v_item.Add(item);
                    }
                }

                // Tiki Points
                if (tiki_pts > 0)
                {

                    item = new stItem();

                    item.type = 2;
                    item.id = -1;
                    item._typeid = TIKI_POINT_TYPEID;
                    item.qntd = 0;
                    item.STDA_C_ITEM_QNTD = 0;

                    pWi = _session.m_pi.findWarehouseItemByTypeid(TIKI_POINT_TYPEID); // Tiki Pts

                    if (pWi != null)
                        item.id = pWi.id;

                    item.qntd += (int)tiki_pts;
                    item.STDA_C_ITEM_QNTD = item.qntd;

                    var rt = ItemManager.RetAddItem.T_INIT_VALUE;

                    if ((rt = ItemManager.addItem(item,
                        _session, 0, 0)) < 0)
                    {
                        throw new exception("[Channel::requestTikiShopExchangeItem][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou adicionar item[TYPEID=" + (item._typeid) + ", ID=" + (item.id) + ", QNTD=" + (item.STDA_C_ITEM_QNTD) + "], mas nao conseguiu.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                            903, 0x5200904));
                    }

                    if (item.id == -1)
                        _smp.message_pool.getInstance().push(new message("[TikiShopExchangeItem][Bug] PLAYER [UID=" + (_session.m_pi.uid) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));


                    if (rt != ItemManager.RetAddItem.T_SUCCESS_PANG_AND_EXP_AND_CP_POUCH)
                    {
                        v_item.Add(item);
                    }


                    // Achievement Add + valor de Tiki Points Ganhos ao contador
                    sys_achieve.incrementCounter(0x6C4000C2u, (int)tiki_pts);
                }

                // Consome os Pangs
                _session.m_pi.consomePang(pang);

                p.init_plain(0xC8);

                p.WriteUInt64(_session.m_pi.ui.pang);
                p.WriteUInt64(pang);

                packet_func.session_send(p,
                    _session, 1);

                // Att Item ON Jogo
                p.init_plain(0x216);

                p.WriteUInt32((uint)UtilTime.GetSystemTimeAsUnix());
                p.WriteUInt32((uint)v_item.Count);

                foreach (var el in v_item)
                {
                    p.WriteByte(el.type);
                    p.WriteUInt32(el._typeid);
                    p.WriteInt32(el.id);//talvez por que o id esta negativo!
                    p.WriteUInt32(el.flag_time);
                    p.WriteInt32(el.stat.qntd_ant);
                    p.WriteInt32(el.stat.qntd_dep);
                    p.WriteInt32((el.STDA_C_ITEM_TIME > 0) ? el.STDA_C_ITEM_TIME : el.STDA_C_ITEM_QNTD);
                    p.WriteZeroByte(25);	// 10 PCL[C0~C4] 2 Bytes cada, 15 bytes desconhecido
                }

                packet_func.session_send(p,
                    _session, 1);

                p.init_plain(0x274);

                p.WriteUInt32(0); // OK
                p.WriteUInt32(milage);
                p.WriteUInt32(bonus);

                packet_func.session_send(p,
                    _session, 1);

                // UPDATE Achievement ON SERVER, DB and GAME
                sys_achieve.finish_and_update(_session);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestTikiShopExchangeItem][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x274);

                p.WriteUInt32((ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) == STDA_ERROR_TYPE.CHANNEL) ? ExceptionError.STDA_SYSTEM_ERROR_DECODE(e.getCodeError()) : 0x5200900);

                packet_func.session_send(p,
                    _session, 1);
            }
        }

        public void requestChangePlayerItemChannel(Player _session, packet _packet)
        {
            byte type = 255;

            PangyaBinaryWriter p = new PangyaBinaryWriter();

            try
            {
                type = _packet.ReadUInt8();
                int item_id;

                int error = 0;

                switch (type)
                {
                    case 1: // Caddie
                        {
                            CaddieInfoEx pCi = null;

                            // Caddie
                            if ((item_id = _packet.ReadInt32()) != 0
                                && (pCi = _session.m_pi.findCaddieById(item_id)) != null
                                && sIff.getInstance().getItemGroupIdentify(pCi._typeid) == IFF_GROUP.CADDIE)
                            {

                                // Check if item is in map of update item
                                var v_it = _session.m_pi.findUpdateItemByTypeidAndId(pCi._typeid, pCi.id);

                                if (!v_it.empty())
                                {

                                    foreach (var el in v_it)
                                    {

                                        if (el.Value.type == UpdateItem.UI_TYPE.CADDIE)
                                        {

                                            // Desequipa o caddie
                                            _session.m_pi.ei.cad_info = null;
                                            _session.m_pi.ue.caddie_id = 0;

                                            item_id = 0;

                                        }
                                        else if (el.Value.type == UpdateItem.UI_TYPE.CADDIE_PARTS)
                                        {

                                            // Limpa o caddie Parts
                                            pCi.parts_typeid = 0;
                                            pCi.parts_end_date_unix = 0;
                                            pCi.end_parts_date = new SYSTEMTIME();

                                            _session.m_pi.ei.cad_info = pCi;
                                            _session.m_pi.ue.caddie_id = item_id;
                                        }

                                        // Tira esse Update Item do map
                                        _session.m_pi.mp_ui.Remove(el.Key);
                                    }

                                }
                                else
                                {

                                    // Caddie is Good, Update caddie equiped ON SERVER AND DB
                                    _session.m_pi.ei.cad_info = pCi;
                                    _session.m_pi.ue.caddie_id = item_id;

                                    // Verifica se o caddie pode ser equipado
                                    if (_session.checkCaddieEquiped(_session.m_pi.ue))
                                    {
                                        item_id = _session.m_pi.ue.caddie_id;
                                    }

                                }

                                // Update ON DB
                                snmdb.NormalManagerDB.getInstance().add(0,
                                    new CmdUpdateCaddieEquiped(_session.m_pi.uid, (int)item_id),
                                    SQLDBResponse, this);

                            }
                            else if (_session.m_pi.ue.caddie_id > 0 && _session.m_pi.ei.cad_info != null)
                            { // Desequipa Caddie

                                error = (item_id == 0) ? 1 : (pCi == null ? 2 : 3);

                                if (error > 1)
                                {
                                    _smp.message_pool.getInstance().push(new message("[Channel::requestChangePlayerItemChannel][Sucess][Warning] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou trocar o Caddie[ID=" + (item_id) + "], mas deu Error[VALUE=" + (error) + "], desequipando o caddie. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                                }

                                // Check if item is in map of update item
                                var v_it = _session.m_pi.findUpdateItemByTypeidAndId(_session.m_pi.ei.cad_info._typeid, _session.m_pi.ei.cad_info.id);

                                if (!v_it.empty())
                                {

                                    foreach (var el in v_it)
                                    {

                                        // Caddie já vai se desequipar, só verifica o parts
                                        if (el.Value.type == UpdateItem.UI_TYPE.CADDIE_PARTS)
                                        {

                                            // Limpa o caddie Parts
                                            _session.m_pi.ei.cad_info.parts_typeid = 0;
                                            _session.m_pi.ei.cad_info.parts_end_date_unix = 0;
                                            _session.m_pi.ei.cad_info.end_parts_date = new SYSTEMTIME();
                                        }

                                        // Tira esse Update Item do map
                                        _session.m_pi.mp_ui.Remove(el.Key);
                                    }

                                }

                                _session.m_pi.ei.cad_info = null;
                                _session.m_pi.ue.caddie_id = 0;

                                item_id = 0;

                                // Zera o Error para o cliente desequipar o caddie que o server desequipou
                                error = 0;

                                // Att No DB
                                snmdb.NormalManagerDB.getInstance().add(0,
                                    new CmdUpdateCaddieEquiped(_session.m_pi.uid, (int)item_id),
                                    SQLDBResponse, this);

                            } // else Não tem nenhum caddie equipado, para desequipar, então o cliente só quis atualizar o estado

                            break;
                        }
                    case 2: // Ball
                        {
                            WarehouseItemEx pWi = null;

                            if ((item_id = _packet.ReadInt32()) != 0
                                && (pWi = _session.m_pi.findWarehouseItemByTypeid((uint)item_id)) != null
                                && sIff.getInstance().getItemGroupIdentify(pWi._typeid) == PangyaAPI.IFF.JP.Models.Flags.IFF_GROUP.BALL)
                            {

                                _session.m_pi.ei.comet = pWi;
                                _session.m_pi.ue.ball_typeid = (uint)item_id; // Ball(Comet) é o typeid que o cliente passa

                                // Verifica se a bola pode ser equipada
                                if (_session.checkBallEquiped(_session.m_pi.ue))
                                {
                                    item_id = (int)_session.m_pi.ue.ball_typeid;
                                }

                                // Update ON DB
                                snmdb.NormalManagerDB.getInstance().add(0,
                                    new CmdUpdateBallEquiped(_session.m_pi.uid, (uint)item_id),
                                    SQLDBResponse, this);

                            }
                            else if (item_id == 0)
                            { // Bola 0 coloca a bola padrão para ele, se for premium user coloca a bola de premium user

                                // Zera para equipar a bola padrão
                                _session.m_pi.ei.comet = null;
                                _session.m_pi.ue.ball_typeid = 0;

                                // Verifica se a Bola pode ser equipada (Coloca para equipar a bola padrão
                                if (_session.checkBallEquiped(_session.m_pi.ue))
                                {
                                    item_id = (int)_session.m_pi.ue.ball_typeid;
                                }

                                // Update ON DB
                                snmdb.NormalManagerDB.getInstance().add(0,
                                    new CmdUpdateBallEquiped(_session.m_pi.uid, (uint)item_id),
                                    SQLDBResponse, this);

                            }
                            else
                            {

                                error = (pWi == null ? 2 : 3);

                                _smp.message_pool.getInstance().push(new message("[Channel::requestChangePlayerItemChannel][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou trocar Ball[TYPEID=" + (item_id) + "], mas deu Error[VALUE=" + (error) + "], Equipando Ball Padrao. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                                pWi = _session.m_pi.findWarehouseItemByTypeid(DEFAULT_COMET_TYPEID);

                                if (pWi != null)
                                {

                                    _smp.message_pool.getInstance().push(new message("[Channel::requestChangePlayerItemChannel][Sucess][Warning] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou trocar a Ball[TYPEID=" + (item_id) + "], mas deu Error[VALUE=" + (error) + "], colocando a Ball Padrao do player. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                                    _session.m_pi.ei.comet = pWi;
                                    item_id = (int)(_session.m_pi.ue.ball_typeid = pWi._typeid);

                                    // Zera o Error para o cliente equipar a Ball Padrão que o server equipou
                                    error = 0;

                                    // Update ON DB
                                    snmdb.NormalManagerDB.getInstance().add(0,
                                        new CmdUpdateBallEquiped(_session.m_pi.uid, (uint)item_id),
                                        SQLDBResponse, this);

                                }
                                else
                                {

                                    _smp.message_pool.getInstance().push(new message("[Channel::requestChangePlayerItemChannel][Sucess][Warning] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou trocar a Ball[TYPEID=" + (item_id) + "], mas deu Error[VALUE=" + (error) + "], ele nao tem a Ball Padrao, adiciona a Ball pardrao para ele. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                                    BuyItem bi = new BuyItem();
                                    stItem item = new stItem();

                                    bi.id = -1;
                                    bi._typeid = DEFAULT_COMET_TYPEID;
                                    bi.qntd = 1;

                                    ItemManager.initItemFromBuyItem(_session.m_pi,
                                        item, bi, false, 0, 0, 1);

                                    if (item._typeid != 0)
                                    {

                                        int result = (int)ItemManager.addItem(item, _session, 2, 0);
                                        item_id = result;
                                        if (result != (int)ItemManager.RetAddItem.T_ERROR)
                                        {

                                            // Equipa a Ball padrao
                                            pWi = _session.m_pi.findWarehouseItemById(item_id);

                                            if (pWi != null)
                                            {

                                                _session.m_pi.ei.comet = pWi;
                                                _session.m_pi.ue.ball_typeid = pWi._typeid;

                                                // Zera o Error para o cliente equipar a Ball Padrão que o server equipou
                                                error = 0;

                                                // Update ON DB
                                                snmdb.NormalManagerDB.getInstance().add(0,
                                                    new CmdUpdateBallEquiped(_session.m_pi.uid, (uint)item_id),
                                                    SQLDBResponse, this);

                                                // Update ON GAME
                                                p.init_plain(0x216);

                                                p.WriteUInt32((uint)UtilTime.GetSystemTimeAsUnix());
                                                p.WriteUInt32(1); // Count

                                                p.WriteByte(item.type);
                                                p.WriteUInt32(item._typeid);
                                                p.WriteInt32(item.id);
                                                p.WriteUInt32(item.flag_time);
                                                p.WriteBytes(item.stat.ToArray());
                                                p.WriteInt32((item.STDA_C_ITEM_TIME > 0) ? item.STDA_C_ITEM_TIME : item.STDA_C_ITEM_QNTD);
                                                p.WriteZeroByte(25);

                                                packet_func.session_send(p,
                                                    _session, 1);

                                            }
                                            else
                                            {
                                                _smp.message_pool.getInstance().push(new message("[Channel::requestChangePlayerItemChannel][Sucess][Warning] PLAYER [UID=" + (_session.m_pi.uid) + "] nao conseguiu achar a Ball[ID=" + (item.id) + "] que acabou de adicionar para ele. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                                            }

                                        }
                                        else
                                        {
                                            _smp.message_pool.getInstance().push(new message("[Channel::requestChangePlayerItemChannel][Sucess][Warning] PLAYER [UID=" + (_session.m_pi.uid) + "] nao conseguiu adicionar a Ball[TYPEID=" + (item._typeid) + "] para ele. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                                        }

                                    }
                                    else
                                    {
                                        _smp.message_pool.getInstance().push(new message("[Channel::requestChangePlayerItemChannel][Sucess][Warning] PLAYER [UID=" + (_session.m_pi.uid) + "] nao conseguiu inicializar a Ball[TYPEID=" + (bi._typeid) + "] para ele. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                                    }
                                }
                            }

                            break;
                        }
                    case 3: // ClubSet
                        {
                            WarehouseItemEx pWi = null;

                            // ClubSet
                            if ((item_id = _packet.ReadInt32()) != 0
                                && (pWi = _session.m_pi.findWarehouseItemById(item_id)) != null
                                && sIff.getInstance().getItemGroupIdentify(pWi._typeid) == PangyaAPI.IFF.JP.Models.Flags.IFF_GROUP.CLUBSET)
                            {

                                var c_it = _session.m_pi.findUpdateItemByTypeidAndType((uint)item_id, UpdateItem.UI_TYPE.WAREHOUSE);

                                if (c_it.Count == 0 || c_it.Count > 0)
                                {

                                    _session.m_pi.ei.clubset = pWi;

                                    // Esse C do WarehouseItem, que pega do DB, não é o ja updado inicial da taqueira é o que fica tabela enchant, 
                                    // que no original fica no warehouse msm, eu só confundi quando fiz
                                    _session.m_pi.ei.csi.setValues(pWi.id, pWi._typeid, pWi.c);

                                    var cs = sIff.getInstance().findClubSet(pWi._typeid);

                                    if (cs != null)
                                    {

                                        for (var i = 0; i < (_session.m_pi.ei.csi.enchant_c.Length); ++i)
                                        {
                                            _session.m_pi.ei.csi.enchant_c[i] = (short)(cs.SlotStats.getSlot[i] + pWi.clubset_workshop.c[i]);
                                        }

                                        _session.m_pi.ue.clubset_id = item_id;

                                        // Verifica se o ClubSet pode ser equipado
                                        if (_session.checkClubSetEquiped(_session.m_pi.ue))
                                        {
                                            item_id = _session.m_pi.ue.clubset_id;
                                        }

                                        // Update ON DB
                                        snmdb.NormalManagerDB.getInstance().add(0,
                                            new CmdUpdateClubsetEquiped(_session.m_pi.uid, (int)item_id),
                                            SQLDBResponse, this);

                                    }
                                    else
                                    {

                                        error = 5;

                                        _smp.message_pool.getInstance().push(new message("[Channel::requestChangePlayerItemChannel][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou Atualizar Clubset[TYPEID=" + (pWi._typeid) + ", ID=" + (pWi.id) + "] equipado, mas ClubSet Not exists on IFF structure. Equipa o ClubSet padrao. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                                        // Coloca o ClubSet CV1 no lugar do ClubSet que acabou o tempo
                                        pWi = _session.m_pi.findWarehouseItemByTypeid(AIR_KNIGHT_SET);

                                        if (pWi != null)
                                        {

                                            _smp.message_pool.getInstance().push(new message("[Channel::requestChangePlayerItemChannel][Sucess][Warning] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou trocar o ClubSet[ID=" + (item_id) + "], mas acabou o tempo do ClubSet[ID=" + (item_id) + @"], colocando o ClubSet Padrao""CV1"" do player. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                                            _session.m_pi.ei.clubset = pWi;
                                            item_id = _session.m_pi.ue.clubset_id = pWi.id;

                                            // Atualiza o ClubSet Enchant no Equiped Item do Player
                                            _session.m_pi.ei.csi.setValues(pWi.id, pWi._typeid, pWi.c);

                                            cs = sIff.getInstance().findClubSet(pWi._typeid);

                                            if (cs != null)
                                            {
                                                for (var i = 0; i < (_session.m_pi.ei.csi.enchant_c.Length); ++i)
                                                {
                                                    _session.m_pi.ei.csi.enchant_c[i] = (short)(cs.SlotStats.getSlot[i] + pWi.clubset_workshop.c[i]);
                                                }
                                            }

                                            // Zera o Error para o cliente equipar a "CV1" que o server equipou
                                            error = 0;

                                            // Update ON DB
                                            snmdb.NormalManagerDB.getInstance().add(0,
                                                new CmdUpdateClubsetEquiped(_session.m_pi.uid, (int)item_id),
                                                SQLDBResponse, this);

                                        }
                                        else
                                        {

                                            _smp.message_pool.getInstance().push(new message("[Channel::requestChangePlayerItemChannel][Sucess][Warning] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou trocar oClubSet[ID=" + (item_id) + "], mas acabou o tempo do ClubSet[ID=" + (item_id) + @"], ele nao tem o ClubSet Padrao""CV1"", adiciona o ClubSet pardrao""CV1"" para ele. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                                            BuyItem bi = new BuyItem();
                                            stItem item = new stItem();

                                            bi.id = -1;
                                            bi._typeid = AIR_KNIGHT_SET;
                                            bi.qntd = 1;

                                            ItemManager.initItemFromBuyItem(_session.m_pi,
                                               item, bi, false, 0, 0, 1);

                                            if (item._typeid != 0)
                                            {

                                                int result = (int)ItemManager.addItem(item, _session, 2, 0);
                                                item_id = result;
                                                if (result != (int)ItemManager.RetAddItem.T_ERROR)
                                                {

                                                    // Equipa o ClubSet CV1
                                                    pWi = _session.m_pi.findWarehouseItemById(item_id);

                                                    if (pWi != null)
                                                    {

                                                        _session.m_pi.ei.clubset = pWi;
                                                        _session.m_pi.ue.clubset_id = pWi.id;

                                                        // Atualiza o ClubSet Enchant no Equiped Item do Player
                                                        _session.m_pi.ei.csi.setValues(pWi.id, pWi._typeid, pWi.c);

                                                        cs = sIff.getInstance().findClubSet(pWi._typeid);

                                                        if (cs != null)
                                                        {
                                                            for (var i = 0; i < (_session.m_pi.ei.csi.enchant_c.Length); ++i)
                                                            {
                                                                _session.m_pi.ei.csi.enchant_c[i] = (short)(cs.SlotStats.getSlot[i] + pWi.clubset_workshop.c[i]);
                                                            }
                                                        }

                                                        // Zera o Error para o cliente equipar a "CV1" que o server equipou
                                                        error = 0;

                                                        // Update ON DB
                                                        snmdb.NormalManagerDB.getInstance().add(0,
                                                            new CmdUpdateClubsetEquiped(_session.m_pi.uid, (int)item_id),
                                                            SQLDBResponse, this);

                                                        // Update ON GAME
                                                        p.init_plain(0x216);

                                                        p.WriteUInt32((uint)UtilTime.GetSystemTimeAsUnix());
                                                        p.WriteUInt32(1); // Count

                                                        p.WriteByte(item.type);
                                                        p.WriteUInt32(item._typeid);
                                                        p.WriteInt32(item.id);
                                                        p.WriteUInt32(item.flag_time);
                                                        p.WriteBytes(item.stat.ToArray());
                                                        p.WriteInt32((item.STDA_C_ITEM_TIME > 0) ? item.STDA_C_ITEM_TIME : item.STDA_C_ITEM_QNTD);
                                                        p.WriteZeroByte(25);

                                                        packet_func.session_send(p,
                                                            _session, 1);

                                                    }
                                                    else
                                                    {
                                                        _smp.message_pool.getInstance().push(new message("[Channel::requestChangePlayerItemChannel][Sucess][Warning] PLAYER [UID=" + (_session.m_pi.uid) + @"] nao conseguiu achar o ClubSet""CV1""[ID=" + (item.id) + "] que acabou de adicionar para ele. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                                                    }

                                                }
                                                else
                                                {
                                                    _smp.message_pool.getInstance().push(new message("[Channel::requestChangePlayerItemChannel][Sucess][Warning] PLAYER [UID=" + (_session.m_pi.uid) + "] nao conseguiu adicionar o ClubSet[TYPEID=" + (item._typeid) + "] para ele. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                                                }

                                            }
                                            else
                                            {
                                                _smp.message_pool.getInstance().push(new message("[Channel::requestChangePlayerItemChannel][Sucess][Warning] PLAYER [UID=" + (_session.m_pi.uid) + "] nao conseguiu inicializar o ClubSet[TYPEID=" + (bi._typeid) + "] para ele. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                                            }
                                        }
                                    }

                                }
                                else
                                {
                                    // ClubSet Acabou o tempo

                                    error = 6; // Acabou o tempo do item

                                    // Coloca o ClubSet CV1 no lugar do ClubSet que acabou o tempo
                                    pWi = _session.m_pi.findWarehouseItemByTypeid(AIR_KNIGHT_SET);

                                    if (pWi != null)
                                    {

                                        _smp.message_pool.getInstance().push(new message("[Channel::requestChangePlayerItemChannel][Sucess][Warning] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou trocar o ClubSet[ID=" + (item_id) + "], mas acabou o tempo do ClubSet[ID=" + (item_id) + @"], colocando o ClubSet Padrao ""CV1"" do player. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                                        _session.m_pi.ei.clubset = pWi;
                                        item_id = _session.m_pi.ue.clubset_id = pWi.id;

                                        // Atualiza o ClubSet Enchant no Equiped Item do Player
                                        _session.m_pi.ei.csi.setValues(pWi.id, pWi._typeid, pWi.c);

                                        var cs = sIff.getInstance().findClubSet(pWi._typeid);

                                        if (cs != null)
                                        {


                                            for (var i = 0; i < (_session.m_pi.ei.csi.enchant_c.Length); ++i)
                                            {
                                                _session.m_pi.ei.csi.enchant_c[i] = (short)(cs.SlotStats.getSlot[i] + pWi.clubset_workshop.c[i]);
                                            }
                                        }

                                        // Zera o Error para o cliente equipar a "CV1" que o server equipou
                                        error = 0;

                                        // Update ON DB
                                        snmdb.NormalManagerDB.getInstance().add(0,
                                            new CmdUpdateClubsetEquiped(_session.m_pi.uid, (int)item_id),
                                            SQLDBResponse, this);

                                    }
                                    else
                                    {

                                        _smp.message_pool.getInstance().push(new message("[Channel::requestChangePlayerItemChannel][Sucess][Warning] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou trocar oClubSet[ID=" + (item_id) + "], mas acabou o tempo do ClubSet[ID=" + (item_id) + @"], ele nao tem o ClubSet Padrao""CV1"", adiciona o ClubSet pardrao""CV1"" para ele. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                                        BuyItem bi = new BuyItem();
                                        stItem item = new stItem();

                                        bi.id = -1;
                                        bi._typeid = AIR_KNIGHT_SET;
                                        bi.qntd = 1;

                                        ItemManager.initItemFromBuyItem(_session.m_pi,
                                           item, bi, false, 0, 0, 1);

                                        if (item._typeid != 0)
                                        {

                                            int result = (int)ItemManager.addItem(item, _session, 2, 0);
                                            item_id = result;
                                            if (result != (int)ItemManager.RetAddItem.T_ERROR)
                                            {

                                                // Equipa o ClubSet CV1
                                                pWi = _session.m_pi.findWarehouseItemById(item_id);

                                                if (pWi != null)
                                                {

                                                    _session.m_pi.ei.clubset = pWi;
                                                    _session.m_pi.ue.clubset_id = pWi.id;

                                                    // Atualiza o ClubSet Enchant no Equiped Item do Player
                                                    _session.m_pi.ei.csi.setValues(pWi.id, pWi._typeid, pWi.c);

                                                    var cs = sIff.getInstance().findClubSet(pWi._typeid);

                                                    if (cs != null)
                                                    {


                                                        for (var i = 0; i < (_session.m_pi.ei.csi.enchant_c.Length); ++i)
                                                        {
                                                            _session.m_pi.ei.csi.enchant_c[i] = (short)(cs.SlotStats.getSlot[i] + pWi.clubset_workshop.c[i]);
                                                        }
                                                    }

                                                    // Zera o Error para o cliente equipar a "CV1" que o server equipou
                                                    error = 0;

                                                    // Update ON DB
                                                    snmdb.NormalManagerDB.getInstance().add(0,
                                                        new CmdUpdateClubsetEquiped(_session.m_pi.uid, (int)item_id),
                                                        SQLDBResponse, this);

                                                    // Update ON GAME
                                                    p.init_plain(0x216);

                                                    p.WriteUInt32((uint)UtilTime.GetSystemTimeAsUnix());
                                                    p.WriteUInt32(1); // Count

                                                    p.WriteByte(item.type);
                                                    p.WriteUInt32(item._typeid);
                                                    p.WriteInt32(item.id);
                                                    p.WriteUInt32(item.flag_time);
                                                    p.WriteBytes(item.stat.ToArray());
                                                    p.WriteInt32((item.STDA_C_ITEM_TIME > 0) ? item.STDA_C_ITEM_TIME : item.STDA_C_ITEM_QNTD);
                                                    p.WriteZeroByte(25);

                                                    packet_func.session_send(p,
                                                        _session, 1);

                                                }
                                                else
                                                {
                                                    _smp.message_pool.getInstance().push(new message("[Channel::requestChangePlayerItemChannel][Sucess][Warning] PLAYER [UID=" + (_session.m_pi.uid) + @"] nao conseguiu achar o ClubSet""CV1""[ID=" + (item.id) + "] que acabou de adicionar para ele. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                                                }

                                            }
                                            else
                                            {
                                                _smp.message_pool.getInstance().push(new message("[Channel::requestChangePlayerItemChannel][Sucess][Warning] PLAYER [UID=" + (_session.m_pi.uid) + "] nao conseguiu adicionar o ClubSet[TYPEID=" + (item._typeid) + "] para ele. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                                            }

                                        }
                                        else
                                        {
                                            _smp.message_pool.getInstance().push(new message("[Channel::requestChangePlayerItemChannel][Sucess][Warning] PLAYER [UID=" + (_session.m_pi.uid) + "] nao conseguiu inicializar o ClubSet[TYPEID=" + (bi._typeid) + "] para ele. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                                        }
                                    }
                                }

                            }
                            else
                            {

                                error = (item_id == 0) ? 1 : (pWi == null ? 2 : 3);

                                pWi = _session.m_pi.findWarehouseItemByTypeid(AIR_KNIGHT_SET);

                                if (pWi != null)
                                {

                                    _smp.message_pool.getInstance().push(new message("[Channel::requestChangePlayerItemChannel][Sucess][Warning] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou trocar o ClubSet[ID=" + (item_id) + "], mas deu Error[VALUE=" + (error) + @"], colocando o ClubSet Padrao""CV1"" do player. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                                    _session.m_pi.ei.clubset = pWi;
                                    item_id = _session.m_pi.ue.clubset_id = pWi.id;

                                    // Atualiza o ClubSet Enchant no Equiped Item do Player
                                    _session.m_pi.ei.csi.setValues(pWi.id, pWi._typeid, pWi.c);

                                    var cs = sIff.getInstance().findClubSet(pWi._typeid);

                                    if (cs != null)
                                    {
                                        for (var i = 0; i < (_session.m_pi.ei.csi.enchant_c.Length); ++i)
                                        {
                                            _session.m_pi.ei.csi.enchant_c[i] = (short)(cs.SlotStats.getSlot[i] + pWi.clubset_workshop.c[i]);
                                        }
                                    }

                                    // Zera o Error para o cliente equipar a "CV1" que o server equipou
                                    error = 0;

                                    // Update ON DB
                                    snmdb.NormalManagerDB.getInstance().add(0,
                                        new CmdUpdateClubsetEquiped(_session.m_pi.uid, (int)item_id),
                                        SQLDBResponse, this);

                                }
                                else
                                {

                                    _smp.message_pool.getInstance().push(new message("[Channel::requestChangePlayerItemChannel][Sucess][Warning] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou trocar o ClubSet[ID=" + (item_id) + "], mas deu Error[VALUE=" + (error) + @"], ele nao tem o ClubSet Padrao""CV1"", adiciona o ClubSet pardrao""CV1"" para ele. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                                    BuyItem bi = new BuyItem();
                                    stItem item = new stItem();

                                    bi.id = -1;
                                    bi._typeid = AIR_KNIGHT_SET;
                                    bi.qntd = 1;

                                    ItemManager.initItemFromBuyItem(_session.m_pi,
                                        item, bi, false, 0, 0, 1);

                                    if (item._typeid != 0)
                                    {

                                        int result = (int)ItemManager.addItem(item, _session, 2, 0);
                                        item_id = result;
                                        if (result != (int)ItemManager.RetAddItem.T_ERROR)
                                        {

                                            // Equipa o ClubSet CV1
                                            pWi = _session.m_pi.findWarehouseItemById(item_id);

                                            if (pWi != null)
                                            {

                                                _session.m_pi.ei.clubset = pWi;
                                                _session.m_pi.ue.clubset_id = pWi.id;

                                                // Atualiza o ClubSet Enchant no Equiped Item do Player
                                                _session.m_pi.ei.csi.setValues(pWi.id, pWi._typeid, pWi.c);

                                                var cs = sIff.getInstance().findClubSet(pWi._typeid);

                                                if (cs != null)
                                                {


                                                    for (var i = 0; i < (_session.m_pi.ei.csi.enchant_c.Length); ++i)
                                                    {
                                                        _session.m_pi.ei.csi.enchant_c[i] = (short)(cs.SlotStats.getSlot[i] + pWi.clubset_workshop.c[i]);
                                                    }
                                                }

                                                // Zera o Error para o cliente equipar a "CV1" que o server equipou
                                                error = 0;

                                                // Update ON DB
                                                snmdb.NormalManagerDB.getInstance().add(0,
                                                    new CmdUpdateClubsetEquiped(_session.m_pi.uid, (int)item_id),
                                                    SQLDBResponse, this);

                                                // Update ON GAME
                                                p.init_plain(0x216);

                                                p.WriteUInt32((uint)UtilTime.GetSystemTimeAsUnix());
                                                p.WriteUInt32(1); // Count

                                                p.WriteByte(item.type);
                                                p.WriteUInt32(item._typeid);
                                                p.WriteInt32(item.id);
                                                p.WriteUInt32(item.flag_time);
                                                p.WriteBytes(item.stat.ToArray());
                                                p.WriteInt32((item.STDA_C_ITEM_TIME > 0) ? item.STDA_C_ITEM_TIME : item.STDA_C_ITEM_QNTD);
                                                p.WriteZeroByte(25);

                                                packet_func.session_send(p,
                                                    _session, 1);

                                            }
                                            else
                                            {
                                                _smp.message_pool.getInstance().push(new message("[Channel::requestChangePlayerItemChannel][Sucess][Warning] PLAYER [UID=" + (_session.m_pi.uid) + @"] nao conseguiu achar o ClubSet""CV1""[ID=" + (item.id) + "] que acabou de adicionar para ele. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                                            }

                                        }
                                        else
                                        {
                                            _smp.message_pool.getInstance().push(new message("[Channel::requestChangePlayerItemChannel][Sucess][Warning] PLAYER [UID=" + (_session.m_pi.uid) + "] nao conseguiu adicionar o ClubSet[TYPEID=" + (item._typeid) + "] para ele. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                                        }

                                    }
                                    else
                                    {
                                        _smp.message_pool.getInstance().push(new message("[Channel::requestChangePlayerItemChannel][Sucess][Warning] PLAYER [UID=" + (_session.m_pi.uid) + "] nao conseguiu inicializar o ClubSet[TYPEID=" + (bi._typeid) + "] para ele. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                                    }
                                }
                            }

                            break;
                        }
                    case 4: // Character
                        {
                            CharacterInfo pCe = null;

                            if ((item_id = _packet.ReadInt32()) != 0
                                && (pCe = _session.m_pi.findCharacterById(item_id)) != null
                                && sIff.getInstance().getItemGroupIdentify(pCe._typeid) == IFF_GROUP.CHARACTER)
                            {
                                //atualiza nos 3, sicronizacao na memoria leak
                                _session.m_pi.ei.char_info = pCe;
                                _session.m_pi.ue.character_id = item_id; 
                                // Update ON DB
                                snmdb.NormalManagerDB.getInstance().add(0,
                                    new CmdUpdateCharacterEquiped(_session.m_pi.uid, item_id),
                                    SQLDBResponse, this);

                                // Update Player Info Channel and Room
                                updatePlayerInfo(_session);

                            }
                            else
                            {

                                error = (item_id == 0) ? 1 : (pCe == null ? 2 : 3);

                                if (_session.m_pi.mp_ce.Count > 0)
                                {

                                    _smp.message_pool.getInstance().push(new message("[Channel::requestChangePlayerItemChannel][Sucess][Warning] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou trocar o Character[ID=" + (item_id) + "], mas deu Error[VALUE=" + (error) + "], colocando o primeiro character do player. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                                    var _char = _session.m_pi.mp_ce.FirstOrDefault();
                                    if (_char.Key != 0 && _char.Value != null)
                                    {
                                        _session.m_pi.ei.char_info = _char.Value;
                                        item_id = _session.m_pi.ue.character_id = _session.m_pi.ei.char_info.id;

                                        // Zera o Error para o cliente equipar o Primeiro Character do map de character do player, que o server equipou
                                        error = 0;

                                        // Update ON DB
                                        snmdb.NormalManagerDB.getInstance().add(0,
                                            new CmdUpdateCharacterEquiped(_session.m_pi.uid, (int)item_id),
                                            SQLDBResponse, this);

                                        // Update Player Info Channel and Room
                                        updatePlayerInfo(_session);
                                    }

                                }
                                else
                                {

                                    _smp.message_pool.getInstance().push(new message("[Channel::requestChangePlayerItemChannel][Sucess][Warning] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou trocar o Character[ID=" + (item_id) + "], mas deu Error[VALUE=" + (error) + "], ele nao tem nenhum character, adiciona o Nuri para ele. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                                    BuyItem bi = new BuyItem();
                                    stItem item = new stItem();

                                    bi.id = -1;
                                    bi._typeid = (uint)(sIff.getInstance().CHARACTER << 26); // Nuri
                                    bi.qntd = 1;

                                    ItemManager.initItemFromBuyItem(_session.m_pi,
                                        item, bi, false, 0, 0, 1);

                                    if (item._typeid != 0)
                                    {

                                        // Add Item já atualiza o Character equipado
                                        int result = (int)ItemManager.addItem(item, _session, 2, 0);
                                        item_id = result;
                                        if (result != (int)ItemManager.RetAddItem.T_ERROR)
                                        {


                                            // Zera o Error para o cliente equipar o Nuri que o server equipou
                                            error = 0;

                                            // Update ON GAME
                                            p.init_plain(0x216);

                                            p.WriteUInt32((uint)UtilTime.GetSystemTimeAsUnix());
                                            p.WriteUInt32(1); // Count

                                            p.WriteByte(item.type);
                                            p.WriteUInt32(item._typeid);
                                            p.WriteInt32(item.id);
                                            p.WriteUInt32(item.flag_time);
                                            p.WriteBytes(item.stat.ToArray());
                                            p.WriteInt32((item.STDA_C_ITEM_TIME > 0) ? item.STDA_C_ITEM_TIME : item.STDA_C_ITEM_QNTD);
                                            p.WriteZeroByte(25);

                                            packet_func.session_send(p,
                                                _session, 1);

                                            // Update Player Info Channel and Room
                                            updatePlayerInfo(_session);

                                        }
                                        else
                                        {
                                            _smp.message_pool.getInstance().push(new message("[Channel::requestChangePlayerItemChannel][Sucess][Warning] PLAYER [UID=" + (_session.m_pi.uid) + "] nao conseguiu adicionar o Character[TYPEID=" + (item._typeid) + "] para ele. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                                        }

                                    }
                                    else
                                    {
                                        _smp.message_pool.getInstance().push(new message("[Channel::requestChangePlayerItemChannel][Sucess][Warning] PLAYER [UID=" + (_session.m_pi.uid) + "] nao conseguiu inicializar o Character[TYPEID=" + (bi._typeid) + "] para ele. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                                    }
                                }
                            }

                            break;
                        }
                    case 5: // Mascot
                        {
                            MascotInfoEx pMi = null;

                            if ((item_id = _packet.ReadInt32()) != 0)
                            {

                                if ((pMi = _session.m_pi.findMascotById(item_id)) != null && sIff.getInstance().getItemGroupIdentify(pMi._typeid) == PangyaAPI.IFF.JP.Models.Flags.IFF_GROUP.MASCOT)
                                {

                                    var m_it = _session.m_pi.findUpdateItemByTypeidAndType((uint)_session.m_pi.ue.mascot_id, UpdateItem.UI_TYPE.MASCOT);

                                    if (m_it.Count > 0)
                                    {

                                        // Desequipa o Mascot que acabou o tempo dele
                                        _session.m_pi.ei.mascot_info = null;
                                        _session.m_pi.ue.mascot_id = 0;

                                        item_id = 0;

                                    }
                                    else
                                    {

                                        // Mascot is Good, Update mascot equiped ON SERVER AND DB
                                        _session.m_pi.ei.mascot_info = pMi;
                                        _session.m_pi.ue.mascot_id = item_id;

                                        // Verifica se o Mascot pode ser equipado
                                        if (_session.checkMascotEquiped(_session.m_pi.ue))
                                        {
                                            item_id = _session.m_pi.ue.mascot_id;
                                        }

                                    }

                                    // Update ON DB
                                    snmdb.NormalManagerDB.getInstance().add(0,
                                        new CmdUpdateMascotEquiped(_session.m_pi.uid, (int)item_id),
                                        SQLDBResponse, this);

                                }
                                else
                                {

                                    error = (item_id == 0) ? 1 : (pMi == null ? 2 : 3);

                                    if (error > 1)
                                    {
                                        _smp.message_pool.getInstance().push(new message("[Channel::requestChangePlayerItemChannel][Sucess][Warning] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou trocar o Mascot[ID=" + (item_id) + "], mas deu Error[VALUE=" + (error) + "], desequipando o Mascot. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                                    }

                                    _session.m_pi.ei.mascot_info = null;
                                    _session.m_pi.ue.mascot_id = 0;

                                    item_id = 0;

                                    // Att No DB
                                    snmdb.NormalManagerDB.getInstance().add(0,
                                        new CmdUpdateMascotEquiped(_session.m_pi.uid, (int)item_id),
                                        SQLDBResponse, this);
                                }

                            }
                            else if (_session.m_pi.ue.mascot_id > 0 && _session.m_pi.ei.mascot_info != null)
                            { // Desequipa Mascot

                                _session.m_pi.ei.mascot_info = null;
                                _session.m_pi.ue.mascot_id = 0;

                                item_id = 0;

                                // Att No DB
                                snmdb.NormalManagerDB.getInstance().add(0,
                                    new CmdUpdateMascotEquiped(_session.m_pi.uid, (int)item_id),
                                    SQLDBResponse, this);

                            } // else Não tem nenhum mascot equipado, para desequipar, então o cliente só quis atualizar o estado

                            break;
                        }
                    default:
                        throw new exception("[Channel::requestChangePlayerItemChannel][Error] type desconhecido.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                            13, 1));
                }

                updatePlayerInfo(_session);


                packet_func.session_send(packet_func.pacote04B(
                    _session, type, error),
                    _session, 1);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestChangePlayerItemChannel][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                packet_func.session_send(packet_func.pacote04B(_session, type,
                    (int)(ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) == STDA_ERROR_TYPE.CHANNEL ? ExceptionError.STDA_SYSTEM_ERROR_DECODE(e.getCodeError()) : 1)),
                    _session, 1);
            }
        }

        public void requestChangePlayerItemRoom(Player _session, packet _packet)
        {
            try
            {

                var r = m_rm.findRoom((short)_session.m_pi.mi.sala_numero);

                // Error do Lounge que ele sai do lounge e pede para atualizar o character equipado
                if (r == null && _session.m_pi.lobby != DEFAULT_CHANNEL)
                {
                    return;
                }

                if (r == null)
                {
                    throw new exception("[Channel::requestChangePlayerItemRoom][Error] o PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] nao esta[NUMERO=" + (_session.m_pi.mi.sala_numero) + "] em nenhuma sala. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        17, 1));
                }

                ChangePlayerItemRoom cpir = new ChangePlayerItemRoom();

                cpir.type = (TYPE_CHANGE)(_packet.ReadUInt8());

                switch (cpir.type)
                {
                    case TYPE_CHANGE.TC_CADDIE:
                        cpir.caddie = _packet.ReadInt32();
                        break;
                    case TYPE_CHANGE.TC_BALL:
                        cpir.ball = _packet.ReadUInt32();
                        break;
                    case TYPE_CHANGE.TC_CLUBSET:
                        cpir.clubset = _packet.ReadInt32();
                        break;
                    case TYPE_CHANGE.TC_CHARACTER:
                        cpir.character = _packet.ReadInt32();
                        break;
                    case TYPE_CHANGE.TC_MASCOT:
                        cpir.mascot = _packet.ReadInt32();
                        break;
                    case TYPE_CHANGE.TC_ITEM_EFFECT_LOUNGE:
                        cpir.effect_lounge = new stItemEffectLounge().ToRead(_packet);
                        break;
                    case TYPE_CHANGE.TC_ALL:
                        cpir.character = _packet.ReadInt32();
                        cpir.caddie = _packet.ReadInt32();
                        cpir.clubset = _packet.ReadInt32();
                        cpir.ball = _packet.ReadUInt32();
                        break;
                }

                // Change Player Item Room
                r.requestChangePlayerItemRoom(_session, cpir);
            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestChangePlayerItemRoom][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                packet_func.session_send(packet_func.pacote04B(_session, 255,
                    (int)(ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) == STDA_ERROR_TYPE.CHANNEL ? ExceptionError.STDA_SYSTEM_ERROR_DECODE(e.getCodeError()) : 1))
             ,
                    _session, 0);
            }
        }

        public void requestDeleteActiveItem(Player _session, packet _packet)
        {
            PangyaBinaryWriter p = new PangyaBinaryWriter();

            try
            {
                uint _typeid = _packet.ReadUInt32();
                uint qntd = _packet.ReadUInt32();

                if (sIff.getInstance().getItemGroupIdentify(_typeid) != IFF_GROUP.ITEM)
                {
                    throw new exception("[Channel::requestDeleteActiveItem][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou excluir um item[TYPEID=" + (_typeid) + "] que nao pode ser excluido. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        703, 0x5200704));
                }

                var iff_item = sIff.getInstance().findItem(_typeid);

                if (iff_item == null)
                {
                    throw new exception("[Channel::requestDeleteActiveItem][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou excluir um item[TYPEID=" + (_typeid) + "] que nao pode ser excluido, por que ele nao tem no IFF_STRUCT do Server. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        704, 0x5200705));
                }

                if (sIff.getInstance().IsItemEquipable(_typeid) && iff_item.Shop.flag_shop.IsCash)
                {
                    throw new exception("[Channel::requestDeleteActiveItem][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou excluir um item[TYPEID=" + (_typeid) + "] que nao pode ser excluido, por que ele é um item equipavel de cash(cookie). Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        705, 0x5200706));
                }

                if (!sIff.getInstance().IsItemEquipable(_typeid) && !(iff_item.Shop.flag_shop.IsGift && iff_item.Stats.getSlot[0] > 0))
                {
                    throw new exception("[Channel::requestDeleteActiveItem][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou excluir um item[TYPEID=" + (_typeid) + "] que nao pode ser excluido, por que ele é um passive item que nao tem a condicao(giftable) e a quantidade no C[0] para deletar esse item. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        706, 0x5200707));
                }

                var pWi = _session.m_pi.findWarehouseItemByTypeid(_typeid);

                if (pWi == null)
                {
                    throw new exception("[Channel::requestDeleteActiveItem][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou excluir item[TYPEID=" + (_typeid) + "] que ele nao possui. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        700, 0x5200701));
                }

                if (pWi.STDA_C_ITEM_QNTD < (short)qntd)
                {
                    throw new exception("[Channel::requestDeleteActiveItem][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou excluir item[TYPEID=" + (_typeid) + "] mas ele nao tem quantidade suficiente[have_qntd=" + (pWi.STDA_C_ITEM_QNTD) + ", req_qntd=" + (qntd) + "]", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        701, 0x5200702));
                }

                stItem item = new stItem();

                item.type = 2;
                item.id = (int)pWi.id;
                item._typeid = pWi._typeid;
                item.qntd = (int)qntd;
                item.STDA_C_ITEM_QNTD = (short)((ushort)qntd * -1);

                // Atualiza ON Server AND Banco de dados
                if (ItemManager.removeItem(item, _session) <= 0)
                {
                    throw new exception("[Channel::requestDeleteActiveItem][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] nao conseguiu excluir item[TYPEID=" + (_typeid) + "]", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        702, 0x5200703));
                }

                _smp.message_pool.getInstance().push(new message("[DeleteActiveItem][Sucess] PLAYER [UID=" + (_session.m_pi.uid) + "] excluiu/(Atualizou qntd) item[TYPEID=" + (pWi._typeid) + ", QNTD=" + (qntd) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Atualiza ON Jogo
                p.init_plain(0xC5);

                p.WriteByte(1); // OK

                p.WriteUInt32(pWi._typeid);
                p.WriteUInt32(qntd);
                p.WriteInt32(pWi.id);

                packet_func.session_send(p,
                    _session, 1);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestDeleteActiveItem][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0xC5);

                p.WriteSByte(-1); // Error

                packet_func.session_send(p,
                    _session, 1);
            }
        }

        public void requestClubSetWorkShopTransferMasteryPts(Player _session, packet _packet)
        {
            //

            PangyaBinaryWriter p = new PangyaBinaryWriter();

            try
            {

                // 300 mastery pts transfere por cada UCIM chip
                ClubSetWorkShopTransferMasteryPts tmp = new ClubSetWorkShopTransferMasteryPts().ToRead(_packet);

                List<stItemEx> v_item = new List<stItemEx>();
                stItemEx item = new stItemEx();





                var pUCIM_chip = _session.m_pi.findWarehouseItemByTypeid(tmp.UCIM_chip_typeid);

                if (pUCIM_chip == null)
                {
                    throw new exception("[Channel::requestClubSetWorkShopTransferMasteryPts][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou transferir mastery pts do ClubSet[ID=" + (tmp.clubset[0]) + "] para ClubSet[ID=" + (tmp.clubset[1]) + "], mas ele nao tem UCIM Chip[TYPEID=" + (tmp.UCIM_chip_typeid) + "]. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        103, 0x5300104));
                }

                if (pUCIM_chip.STDA_C_ITEM_QNTD < (short)tmp.qntd)
                {
                    throw new exception("[Channel::requestClubSetWorkShopTransferMasteryPts][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou transferir mastery pts do ClubSet[ID=" + (tmp.clubset[0]) + "] para ClubSet[ID=" + (tmp.clubset[1]) + "], mas ele nao tem quantidade suficiente de UCIM Chip[TYPEID=" + (tmp.UCIM_chip_typeid) + ", QNTD=" + (pUCIM_chip.STDA_C_ITEM_QNTD) + ", request=" + (tmp.qntd) + "]", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        104, 0x5300105));
                }

                var pClub_src = _session.m_pi.findWarehouseItemById(tmp.clubset[0]);

                if (pClub_src == null)
                {
                    throw new exception("[Channel::requestClubSetWorkShopTransferMasteryPts][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou transferir mastery pts do ClubSet[ID=" + (tmp.clubset[0]) + "] mas o player nao tem esse ClubSet. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        100, 0x5300101));
                }

                var pClub_dst = _session.m_pi.findWarehouseItemById(tmp.clubset[1]);

                if (pClub_dst == null)
                {
                    throw new exception("[Channel::requestClubSetWorkShopTransferMasteryPts][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou transferir mastery pts para o ClubSet[ID=" + (tmp.clubset[1]) + "] mas o player nao tem esse ClubSet. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        100, 0x5300101));
                }

                if (sIff.getInstance().findClubSet(pClub_src._typeid) == null)
                {
                    throw new exception("[Channel::requestClubSetWorkShopTransferMasteryPts][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou transferir mastery pts do ClubSet[TYPEID=" + (pClub_src._typeid) + ", ID=" + (pClub_src.id) + "] mas o clubset nao existe no IFF_STRUCT do Server. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        101, 0x5300102));
                }

                var clubset = sIff.getInstance().findClubSet(pClub_dst._typeid);

                if (clubset == null)
                {
                    throw new exception("[Channel::requestClubSetWorkShopTransferMasteryPts][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou transferir mastery pts para o ClubSet[TYPEID=" + (pClub_src._typeid) + ", ID=" + (pClub_src.id) + "] mas o clubset nao existe no IFF_STRUCT do Server. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        101, 0x5300102));
                }

                if (clubset.work_shop.tipo == -1)
                {
                    throw new exception("[Channel::requestClubSetWorkShopTransferMasteryPts][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou transferir mastery pts para o ClubSet[TYPEID=" + (pClub_dst._typeid) + ", ID=" + (pClub_dst.id) + "] mas ele nao pode receber mastery de outros ClubSet. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        102, 0x5300103));
                }

                if (pClub_dst.clubset_workshop.calcRank(clubset.SlotStats.getSlot) == 5)
                {
                    throw new exception("[Channel::requestClubSetWorkShopTransferMasteryPts][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou transferir mastery pts para o ClubSet[TYPEID=" + (pClub_dst._typeid) + ", ID=" + (pClub_dst.id) + "] mas o ClubSet é Rank S nao pode transferir Mastery Pts mais para ele. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        107, 0x5300108));
                }

                if ((tmp.qntd * 300) > (uint)pClub_src.clubset_workshop.mastery && (uint)((pClub_src.clubset_workshop.mastery % 300 == 0) ? pClub_src.clubset_workshop.mastery / 300 : pClub_src.clubset_workshop.mastery / 300 + 1) > tmp.qntd)
                {
                    throw new exception("[Channel::requestClubSetWorkShopTransferMasteryPts][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou transferir mastery pts do ClubSet[ID=" + (tmp.clubset[0]) + "] para ClubSet[ID=" + (tmp.clubset[1]) + "], mas ele tentou usar UCIM chip mais que o necessario. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        105, 0x5300106));
                }

                uint mastery = ((tmp.qntd * 300) > (uint)pClub_src.clubset_workshop.mastery) ? pClub_src.clubset_workshop.mastery : tmp.qntd * 300;

                // Transferi os Mastery Points
                pClub_dst.clubset_workshop.mastery += mastery;
                pClub_src.clubset_workshop.mastery -= mastery;

                // UCIM Chip
                item = new stItemEx();

                item.type = 2;
                item.id = (int)pUCIM_chip.id;
                item._typeid = pUCIM_chip._typeid;
                item.qntd = (int)tmp.qntd;
                item.STDA_C_ITEM_QNTD = (short)(item.qntd * -1);

                if (ItemManager.removeItem(item, _session) <= 0)
                {
                    throw new exception("[Channel::requestClubSetWorkShopTransferMasteryPts][Error] PLAYER [UID=" + _session.m_pi.uid + "] tentou remover item[TYPEID=" + (item._typeid) + ", ID=" + (item.id) + "] mas nao conseguiu", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        106, 0x5300107));
                }

                v_item.Add(new stItemEx(item));

                // ClubSet Font
                item = new stItemEx
                {
                    type = 0xCC,
                    id = (int)pClub_src.id,
                    _typeid = pClub_src._typeid
                };
                item.clubset_workshop.c = pClub_src.clubset_workshop.c;
                item.clubset_workshop.level = (byte)pClub_src.clubset_workshop.level;
                item.clubset_workshop.mastery = pClub_src.clubset_workshop.mastery;
                item.clubset_workshop.rank = (uint)pClub_src.clubset_workshop.rank;
                item.clubset_workshop.recovery = pClub_src.clubset_workshop.recovery_pts;

                v_item.Add(new stItemEx(item));

                // ClubSet Destino
                item = new stItemEx
                {
                    type = 0xCC,
                    id = (int)pClub_dst.id,
                    _typeid = pClub_dst._typeid
                };
                item.clubset_workshop.c = pClub_dst.clubset_workshop.c;
                item.clubset_workshop.level = (byte)pClub_dst.clubset_workshop.level;
                item.clubset_workshop.mastery = pClub_dst.clubset_workshop.mastery;
                item.clubset_workshop.rank = (uint)pClub_dst.clubset_workshop.rank;
                item.clubset_workshop.recovery = pClub_dst.clubset_workshop.recovery_pts;

                v_item.Add(new stItemEx(item));

                // Atualiza ON DB
                snmdb.NormalManagerDB.getInstance().add(12,
                    new CmdUpdateClubSetWorkshop(_session.m_pi.uid,
                        pClub_src,
                        CmdUpdateClubSetWorkshop.FLAG.F_TRANSFER_MASTERY_PTS),
                    SQLDBResponse, this); 
                // Log
                _smp.message_pool.getInstance().push(new message("[ClubSet Workshop::TransferMasteryPts][Sucess] PLAYER [UID=" + (_session.m_pi.uid) + "] transferiu mastery pts[value=" + (mastery) + "] do ClubSet[TYPEID=" + (pClub_src._typeid) + ", ID=" + (pClub_src.id) + "] para o ClubSet[TYPEID=" + (pClub_dst._typeid) + ", ID=" + (pClub_dst.id) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Atualiza ON Jogo
                p.init_plain(0x216);

                p.WriteUInt32((uint)UtilTime.GetSystemTimeAsUnix());
                p.WriteUInt32((uint)v_item.Count);

                foreach (var el in v_item)
                {
                    p.WriteByte(el.type);
                    p.WriteUInt32(el._typeid);
                    p.WriteInt32(el.id);
                    p.WriteUInt32(el.flag_time);
                    p.WriteBytes(el.stat.ToArray());
                    p.WriteInt32((el.STDA_C_ITEM_TIME > 0) ? el.STDA_C_ITEM_TIME : el.STDA_C_ITEM_QNTD);
                    p.WriteZeroByte(25); // 10 PCL[C0~C4] 2 Bytes cada, 15 bytes desconhecido
                    if (el.type == 0xCC)
                    {
                        p.WriteBytes(el.clubset_workshop.ToArray());
                    }
                }

                packet_func.session_send(p,
                    _session, 1);

                // Resposta do transfer Mastery Pts
                p.init_plain(0x245);

                p.WriteUInt32(0); // OK

                packet_func.session_send(p,
                    _session, 1);

                // Update Achievement ON SERVER, DB and GAME
                AchievementSystem sys_achieve = new AchievementSystem();

                sys_achieve.incrementCounter(0x6C4000A5u);

                sys_achieve.finish_and_update(_session);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestClubSetWorkShopTransferMasteryPts][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x245);

                p.WriteUInt32((ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) == STDA_ERROR_TYPE.CHANNEL) ? ExceptionError.STDA_SYSTEM_ERROR_DECODE(e.getCodeError()) : 0x5300100);

                packet_func.session_send(p,
                    _session, 1);
            }
        }

        public void requestClubSetWorkShopRecoveryPts(Player _session, packet _packet)
        {
            //

            PangyaBinaryWriter p = new PangyaBinaryWriter();

            try
            {

                uint item_typeid = _packet.ReadUInt32();
                int clubset_id = _packet.ReadInt32();





                List<stItemEx> v_item = new List<stItemEx>();
                stItemEx item = new stItemEx();

                var pWi = _session.m_pi.findWarehouseItemByTypeid(item_typeid);

                if (pWi == null)
                {
                    throw new exception("[requestClubSetWorkShopRecoveryPts][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou recuperar os pontos de recuperacao do ClubSet[ID=" + (clubset_id) + "], mas ele nao tem o item[TYPEID=" + (item_typeid) + "] para isso. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        150, 0x5300151));
                }

                if (pWi.STDA_C_ITEM_QNTD < 1)
                {
                    throw new exception("[requestClubSetWorkShopRecoveryPts][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou recuperar os pontos de recuperacao do ClubSet[ID=" + (clubset_id) + "], mas ele nao tem quantidade do item[TYPEID=" + (pWi._typeid) + ", ID=" + (pWi.id) + ", QNTD=" + (pWi.STDA_C_ITEM_QNTD) + ", request=1]. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        151, 0x5300152));
                }

                var pClub = _session.m_pi.findWarehouseItemById(clubset_id);

                if (pClub == null)
                {
                    throw new exception("[requestClubSetWorkShopRecoveryPts][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou recuperar os pontos de recuperacao do ClubSet[ID=" + (clubset_id) + "], mas ele nao tem o ClubSet. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        152, 0x5300153));
                }

                var clubset = sIff.getInstance().findClubSet(pClub._typeid);

                if (clubset == null)
                {
                    throw new exception("[requestClubSetWorkShopRecoveryPts][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou recuperar os pontos de recuperacao do ClubSet[ID=" + (clubset_id) + "], mas nao tem esse ClubSet no IFF_STRUCT do Server. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        153, 0x5300154));
                }

                if (clubset.work_shop.tipo == -1)
                {
                    throw new exception("[requestClubSetWorkShopRecoveryPts][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou recuperar os pontos de recuperacao do ClubSet[ID=" + (clubset_id) + "], mas esse ClubSet nao pode Recuperar o Recovery Pts. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        154, 0x5300155));
                }

                if (pClub.clubset_workshop.recovery_pts == 0)
                {
                    throw new exception("[requestClubSetWorkShopRecoveryPts][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou recuperar os pontos de recuperacao do ClubSet[ID=" + (clubset_id) + "], mas o ClubSet do player ja foi recuperado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        156, 0x5300157));
                }

                // Corneta de recuperar recovery pts do ClubSet
                item = new stItemEx();

                item.type = 2;
                item.id = (int)pWi.id;
                item._typeid = pWi._typeid;
                item.qntd = 1;
                item.STDA_C_ITEM_QNTD = (short)(item.qntd * -1);

                if (ItemManager.removeItem(item, _session) <= 0)
                {
                    throw new exception("[requestClubSetWorkShopRecoveryPts][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou recuperar os pontos de recuperacao do ClubSet[ID=" + (clubset_id) + "], mas nao conseguiu remover item[TYPEID=" + (item._typeid) + ", ID=" + (item.id) + "]", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        155, 0x5300156));
                }

                v_item.Add(new stItemEx(item));

                pClub.clubset_workshop.recovery_pts = 0;

                // ClubSet
                item = new stItemEx();

                item.type = 0xCC;
                item.id = (int)pClub.id;
                item._typeid = pClub._typeid;
                item.clubset_workshop.c = pClub.clubset_workshop.c;
                item.clubset_workshop.level = (byte)pClub.clubset_workshop.level;
                item.clubset_workshop.mastery = pClub.clubset_workshop.mastery;
                item.clubset_workshop.rank = (uint)pClub.clubset_workshop.rank;
                item.clubset_workshop.recovery = pClub.clubset_workshop.recovery_pts;

                v_item.Add(new stItemEx(item));

                // UPDATE ON DB
                snmdb.NormalManagerDB.getInstance().add(12,
                    new CmdUpdateClubSetWorkshop(_session.m_pi.uid,
                        pClub,
                        CmdUpdateClubSetWorkshop.FLAG.F_R_RECOVERY_PTS),
                    SQLDBResponse, this);
                 
                // UPDATE ON Jogo
                p.init_plain(0x216);

                p.WriteUInt32((uint)UtilTime.GetSystemTimeAsUnix());
                p.WriteUInt32((uint)v_item.Count);

                foreach (var el in v_item)
                {
                    p.WriteByte(el.type);
                    p.WriteUInt32(el._typeid);
                    p.WriteInt32(el.id);
                    p.WriteUInt32(el.flag_time);
                    p.WriteBytes(el.stat.ToArray());
                    p.WriteInt32((el.STDA_C_ITEM_TIME > 0) ? el.STDA_C_ITEM_TIME : el.STDA_C_ITEM_QNTD);
                    p.WriteZeroByte(25);
                    if (el.type == 0xCC)
                    {
                        p.WriteBytes(el.clubset_workshop.ToArray());
                    }
                }

                packet_func.session_send(p,
                    _session, 1);

                // Resposta para o recovery ClubSet Pts
                p.init_plain(0x246);

                p.WriteUInt32(0); // OK

                packet_func.session_send(p,
                    _session, 1);

                // Update Achievement ON SERVER, DB and GAME
                AchievementSystem sys_achieve = new AchievementSystem();

                sys_achieve.incrementCounter(0x6C4000A6);

                sys_achieve.finish_and_update(_session);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestClubSetWorkShopRecoveryPts][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x246);

                p.WriteUInt32((ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) == STDA_ERROR_TYPE.CHANNEL) ? ExceptionError.STDA_SYSTEM_ERROR_DECODE(e.getCodeError()) : 0x5300150);

                packet_func.session_send(p,
                    _session, 1);
            }
        }

        public void requestClubSetWorkShopUpLevel(Player _session, packet _packet)
        {
            //

            PangyaBinaryWriter p = new PangyaBinaryWriter();


            try
            {

                CWUpLevel cwul = new CWUpLevel().ToRead(_packet);
                stItem item = new stItem();
                ProbCardExtra pce = new ProbCardExtra();

                uint stat = 0; // Stat que vai ser updado no ClubSet, Ex: PWR, CTRL, ACCY, SPIN, CURV





                switch (sIff.getInstance().getItemGroupIdentify(cwul.item_typeid))
                {
                    case IFF_GROUP.ITEM:
                        {
                            var pWi = _session.m_pi.findWarehouseItemByTypeid(cwul.item_typeid);

                            if (pWi == null)
                            {
                                throw new exception("[Channel::requestClubSetWorkShopUpLevel][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou upar ClubSet[ID=" + (cwul.clubset_id) + "] Level, mas ele nao tem o item[TYPEID=" + (cwul.item_typeid) + "]. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                    201, 0x5300202));
                            }

                            if (pWi.STDA_C_ITEM_QNTD < (short)cwul.qntd)
                            {
                                throw new exception("[Channel::requestClubSetWorkShopUpLevel][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou upar ClubSet[ID=" + (cwul.clubset_id) + "] Level, mas ele nao tem quantidade suficiente do item[TYPEID=" + (pWi._typeid) + ", ID=" + (pWi.id) + ", QNTD=" + (pWi.STDA_C_ITEM_QNTD) + ", request=" + (cwul.qntd) + "]. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                    202, 0x5300203));
                            }

                            if (sIff.getInstance().findItem(pWi._typeid) == null)
                            {
                                throw new exception("[Channel::requestClubSetWorkShopUpLevel][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou upar ClubSet[ID=" + (cwul.clubset_id) + "] Level, mas o Item nao existe no IFF_STRUCT do Server. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                    203, 0x5300204));
                            }

                            item = new stItem();

                            item.type = 2;
                            item.id = (int)pWi.id;
                            item._typeid = pWi._typeid;
                            item.qntd = cwul.qntd;
                            item.STDA_C_ITEM_QNTD = (short)(item.qntd * -1);

                            break;
                        }
                    case IFF_GROUP.CARD:
                        {
                            var pCi = _session.m_pi.findCardByTypeid(cwul.item_typeid);

                            if (pCi == null)
                            {
                                throw new exception("[Channel::requestClubSetWorkShopUpLevel][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou upar ClubSet[ID=" + (cwul.clubset_id) + "] Level, mas ele nao tem o item[TYPEID=" + (cwul.item_typeid) + "]. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                    201, 0x5300202));
                            }

                            if (pCi.qntd < (short)cwul.qntd)
                            {
                                throw new exception("[Channel::requestClubSetWorkShopUpLevel][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou upar ClubSet[ID=" + (cwul.clubset_id) + "] Level, mas ele nao tem quantidade suficiente do Card[TYPEID=" + (pCi._typeid) + ", ID=" + (pCi.id) + ", QNTD=" + (pCi.qntd) + ", request=" + (cwul.qntd) + "]. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                    202, 0x5300203));
                            }

                            if (sIff.getInstance().findCard(pCi._typeid) == null)
                            {
                                throw new exception("[Channel::requestClubSetWorkShopUpLevel][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou upar ClubSet[ID=" + (cwul.clubset_id) + "] Level, mas o Card nao existe no IFF_STRUCT do Server. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                    203, 0x5300204));
                            }

                            item = new stItem();

                            item.type = 2;
                            item.id = (int)pCi.id;
                            item._typeid = pCi._typeid;
                            item.qntd = cwul.qntd;
                            item.STDA_C_ITEM_QNTD = (short)(item.qntd * -1);

                            if (cwul.qntd > 0)
                            {
                                pce.active = 1;
                                pce.stat = (byte)(cwul.qntd == 1 ? 2 : (cwul.qntd == 2 ? 4 : (cwul.qntd == 3 ? 0 : (cwul.qntd == 4 ? 3 : (cwul.qntd == 5 ? 1 : 2)))));
                                pce.prob = (uint)(cwul.qntd * 200);
                            }

                            break;
                        }
                    default:
                        throw new exception("[Channel::requestClubSetWorkShopUpLevel][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou upar ClubSet[ID=" + (cwul.clubset_id) + "] Level, mas o item[TYPEID=" + (cwul.item_typeid) + "], usado para upar é desconhecido. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                            200, 0x5300201));
                }

                var pClub = _session.m_pi.findWarehouseItemById(cwul.clubset_id);

                if (pClub == null)
                {
                    throw new exception("[Channel::requestClubSetWorkShopUpLevel][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou upar ClubSet[ID=" + (cwul.clubset_id) + "] Level, mas o ele nao tem o ClubSet. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        204, 0x5300205));
                }

                if (pClub.clubset_workshop.rank == -1)
                {
                    throw new exception("[Channel::requestClubSetWorkShopUpLevel][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou upar ClubSet[ID=" + (cwul.clubset_id) + "] Level, mas ClubSet dele ja upou todos os levels permitidos. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        209, 0x5300210));
                }

                var clubset = sIff.getInstance().findClubSet(pClub._typeid);

                if (clubset == null)
                {
                    throw new exception("[Channel::requestClubSetWorkShopUpLevel][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou upar ClubSet[TYPEID=" + (pClub._typeid) + ", ID=" + (pClub.id) + "] Level, mas o ClubSet nao existe no IFF_STRUCT so Server. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        205, 0x5300206));
                }

                if (clubset.work_shop.tipo == -1)
                {
                    throw new exception("[Channel::requestClubSetWorkShopUpLevel][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou upar ClubSet[ID=" + (cwul.clubset_id) + "] Level, mas esse ClubSet nao pose upar Level. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        206, 0x5300207));
                }

                // Stat Up
                var level_up_limit = sIff.getInstance().findClubSetWorkShopLevelUpLimit(clubset.work_shop.tipo);
                var level_up_prob = sIff.getInstance().findClubSetWorkShopLevelUpProb(clubset.work_shop.tipo);

                if (level_up_limit.empty() || level_up_prob == null)
                {
                    throw new exception("[Channel::requestClubSetWorkShopUpLevel][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou upar ClubSet[ID=" + (cwul.clubset_id) + "] Level, IFF_STRUCT level_up_limit or level_up_prob not found. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        208, 0x5300209));
                }

                // 
                var limit = level_up_limit.FirstOrDefault(el =>
                {
                    return el.rank == pClub.clubset_workshop.calcRank(clubset.SlotStats.getSlot);
                });

                if (limit == null)
                {
                    throw new exception("[Channel::requestClubSetWorkShopUpLevel][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou upar ClubSet[ID=" + (cwul.clubset_id) + "] Level, nao encontrou o level para upar no IFF_STRUCT do Server. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        210, 0x5300211));
                }

                Lottery lottery = new Lottery();

                for (var ii = 0; ii < (limit.c.Length); ++ii)
                {
                    if (limit.c[ii] > (ushort)(pClub.clubset_workshop.c[ii] + clubset.SlotStats.getSlot[ii]))
                    {
                        lottery.Push(level_up_prob.c[ii] + (pce.active == 1 && ii == pce.stat ? pce.prob : 0), ii);
                    }
                }

                var lc = lottery.spinRoleta();

                if (lc != null)
                {
                    stat = Convert.ToUInt32(lc.Value);
                }

                if (ItemManager.removeItem(item, _session) <= 0)
                {
                    throw new exception("[Channel::requestClubSetWorkShopUpLevel][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou upar ClubSet[ID=" + (cwul.clubset_id) + "] Level, nao conseguiu remover item[TYPEID=" + (item._typeid) + ", ID=" + (item.id) + "]", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        207, 0x5300208));
                }

                _session.m_pi.cwlul.clubset_id = pClub.id;
                _session.m_pi.cwlul.stat = (uint)stat;

                pClub.clubset_workshop.c[stat]++;

                // UPDATE ON DB
                snmdb.NormalManagerDB.getInstance().add(12,
                    new CmdUpdateClubSetWorkshop(_session.m_pi.uid,
                        pClub,
                        CmdUpdateClubSetWorkshop.FLAG.F_UP_LEVEL),
                    SQLDBResponse, this);
                 
                // UPDATE ON JOGO
                p.init_plain(0x216);

                p.WriteUInt32((uint)UtilTime.GetSystemTimeAsUnix());
                p.WriteUInt32(1);

                p.WriteByte(item.type);
                p.WriteUInt32(item._typeid);
                p.WriteInt32(item.id);
                p.WriteUInt32(item.flag_time);
                p.WriteBytes(item.stat.ToArray());
                p.WriteInt32((item.STDA_C_ITEM_TIME > 0) ? item.STDA_C_ITEM_TIME : item.STDA_C_ITEM_QNTD);
                p.WriteZeroByte(25);

                packet_func.session_send(p,
                    _session, 1);

                // Resposta para o ClubSet Up Level
                p.init_plain(0x23D);

                p.WriteUInt32(0); // OK;
                p.WriteUInt32((uint)stat);

                packet_func.session_send(p,
                    _session, 1);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestClubSetWorkShopUpLevel][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x23D);

                p.WriteUInt32((ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) == STDA_ERROR_TYPE.CHANNEL) ? ExceptionError.STDA_SYSTEM_ERROR_DECODE(e.getCodeError()) : 0x5300200);

                packet_func.session_send(p,
                    _session, 1);
            }
        }

        public void requestClubSetWorkShopUpLevelConfirm(Player _session, packet _packet)
        {
            //

            PangyaBinaryWriter p = new PangyaBinaryWriter();

            try
            {





                stItemEx item = new stItemEx();

                var pClub = _session.m_pi.findWarehouseItemById(_session.m_pi.cwlul.clubset_id);

                if (pClub == null)
                {
                    throw new exception("[Channel::requestClubSetWorkShopUpLevelConfirm][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou confirma o Up Level[stat=" + (_session.m_pi.cwlul.stat) + "] do ClubSet[ID=" + (_session.m_pi.cwlul.clubset_id) + "], mas ele nao tem esse ClubSet. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        300, 0x5300301));
                }

                if (_session.m_pi.cwlul.stat > 4)
                {
                    throw new exception("[Channel::requestClubSetWorkShopUpLevelConfirm][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou confirma o Up Level[stat=" + (_session.m_pi.cwlul.stat) + "] do ClubSet[ID=" + (_session.m_pi.cwlul.clubset_id) + "], mas o stat é desconhecido. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        302, 0x5300303));
                }

                var clubset = sIff.getInstance().findClubSet(pClub._typeid);

                if (clubset == null)
                {
                    throw new exception("[Channel::requestClubSetWorkShopUpLevelConfirm][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou confirma o Up Level[stat=" + (_session.m_pi.cwlul.stat) + "] do ClubSet[ID=" + (_session.m_pi.cwlul.clubset_id) + "], mas nao existe esse ClubSet no IFF_STRUCT do Server. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        301, 0x5300302));
                }

                // UPDATE ON SERVER

                // ClubSet
                item = new stItemEx();

                item.type = 0xCC;
                item.id = (int)pClub.id;
                item._typeid = pClub._typeid;
                item.clubset_workshop.c = pClub.clubset_workshop.c;
                item.clubset_workshop.level = (byte)pClub.clubset_workshop.level;
                item.clubset_workshop.mastery = pClub.clubset_workshop.mastery;
                item.clubset_workshop.rank = (uint)pClub.clubset_workshop.rank;
                item.clubset_workshop.recovery = pClub.clubset_workshop.recovery_pts;
                 
                // UPDATE ON JOGO
                p.init_plain(0x216);

                p.WriteUInt32((uint)UtilTime.GetSystemTimeAsUnix());
                p.WriteUInt32(1); // Count

                p.WriteByte(item.type);
                p.WriteUInt32(item._typeid);
                p.WriteInt32(item.id);
                p.WriteUInt32(item.flag_time);
                p.WriteBytes(item.stat.ToArray());
                p.WriteInt32((item.STDA_C_ITEM_TIME > 0) ? item.STDA_C_ITEM_TIME : item.STDA_C_ITEM_QNTD);
                p.WriteZeroByte(25);
                if (item.type == 0xCC)
                {
                    p.WriteBytes(item.clubset_workshop.ToArray());
                }

                packet_func.session_send(p,
                    _session, 1);

                // Resposta para o ClubSet Wrokshop Up Level Confirm
                p.init_plain(0x23E);

                p.WriteUInt32(0); // OK
                p.WriteUInt32(_session.m_pi.cwlul.stat);
                p.WriteInt32(_session.m_pi.cwlul.clubset_id);

                packet_func.session_send(p,
                    _session, 1);

                // Update Achievement ON SERVER, DB and GAME
                AchievementSystem sys_achieve = new AchievementSystem();

                sys_achieve.incrementCounter(0x6C4000A2u);

                sys_achieve.finish_and_update(_session);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestClubSetWorkShopUpLevelConfirm][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x23E);

                p.WriteUInt32((ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) == STDA_ERROR_TYPE.CHANNEL) ? ExceptionError.STDA_SYSTEM_ERROR_DECODE(e.getCodeError()) : 0x5300300);

                packet_func.session_send(p,
                    _session, 1);
            }
        }


        public void requestClubSetWorkShopUpLevelCancel(Player _session, packet _packet)
        {
            PangyaBinaryWriter p = new PangyaBinaryWriter();

            try
            {
                stItemEx item = new stItemEx();

                var pClub = _session.m_pi.findWarehouseItemById(_session.m_pi.cwlul.clubset_id);

                if (pClub == null)
                {
                    throw new exception("[Channel::requestClubSetWorkShopUpLevelCancel][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou cancelar o up level[stat=" + (_session.m_pi.cwlul.stat) + "] do ClubSet[ID=" + (_session.m_pi.cwlul.clubset_id) + "], mas ele nao tem esse ClubSet. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        250, 0x5300251));
                }

                if (_session.m_pi.cwlul.stat > 4)
                {
                    throw new exception("[Channel::requestClubSetWorkShopUpLevelCancel][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou cancelar o up level[stat=" + (_session.m_pi.cwlul.stat) + "] do ClubSet[ID=" + (_session.m_pi.cwlul.clubset_id) + "], mas o stat é desconhecido. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        251, 0x5300252));
                }

                var clubset = sIff.getInstance().findClubSet(pClub._typeid);

                if (clubset == null)
                {
                    throw new exception("[Channel::requestClubSetWorkShopUpLevelCancel][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou cancelar o up level[stat=" + (_session.m_pi.cwlul.stat) + "] do ClubSet[ID=" + (_session.m_pi.cwlul.clubset_id) + "], mas o ClubSet nao existe no IFF_STRUCT do Server. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        252, 0x5300253));
                }

                if (clubset.work_shop.total_recovery <= (uint)pClub.clubset_workshop.recovery_pts)
                {
                    throw new exception("[Channel::requestClubSetWorkShopUpLevelCancel][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou cancelar o up level[stat=" + (_session.m_pi.cwlul.stat) + "] do ClubSet[ID=" + (_session.m_pi.cwlul.clubset_id) + "], mas o ele nao pode mais cancelar ja gastou todos os seus pts de recovery[ClubSet_IFF_recovery=" + (clubset.work_shop.total_recovery) + ", ClubSet_recovery=" + (pClub.clubset_workshop.recovery_pts) + "]", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        253, 0x5300254));
                }

                // UPDATE ON SERVER
                pClub.clubset_workshop.c[_session.m_pi.cwlul.stat]--;
                pClub.clubset_workshop.recovery_pts++;

                // ClubSet
                item = new stItemEx();

                item.type = 0xCC;
                item.id = (int)pClub.id;
                item._typeid = pClub._typeid;
                item.clubset_workshop.c = pClub.clubset_workshop.c;
                item.clubset_workshop.level = (byte)pClub.clubset_workshop.level;
                item.clubset_workshop.mastery = pClub.clubset_workshop.mastery;
                item.clubset_workshop.rank = (uint)pClub.clubset_workshop.rank;
                item.clubset_workshop.recovery = pClub.clubset_workshop.recovery_pts;

                // UPDATE ON DB
                snmdb.NormalManagerDB.getInstance().add(12,
                    new CmdUpdateClubSetWorkshop(_session.m_pi.uid,
                        pClub,
                        CmdUpdateClubSetWorkshop.FLAG.F_UP_LEVEL_CANCEL),
                    SQLDBResponse, this);

                 
                // UPDATE ON JOGO
                p.init_plain(0x216);

                p.WriteUInt32((uint)UtilTime.GetSystemTimeAsUnix());
                p.WriteUInt32(1); // Count

                p.WriteByte(item.type);
                p.WriteUInt32(item._typeid);
                p.WriteInt32(item.id);
                p.WriteUInt32(item.flag_time);
                p.WriteBytes(item.stat.ToArray());
                p.WriteInt32((item.STDA_C_ITEM_TIME > 0) ? item.STDA_C_ITEM_TIME : item.STDA_C_ITEM_QNTD);
                p.WriteZeroByte(25);
                if (item.type == 0xCC)
                {
                    p.WriteBytes(item.clubset_workshop.ToArray());
                }

                packet_func.session_send(p,
                    _session, 1);

                // Resposta para o ClubSet Wrokshop Up Level Cancel
                p.init_plain(0x23F);

                p.WriteUInt32(0); // OK
                p.WriteInt32(_session.m_pi.cwlul.clubset_id);

                packet_func.session_send(p,
                    _session, 1);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestClubSetWorkShopUpLevelCancel][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x23F);

                p.WriteUInt32((ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) == STDA_ERROR_TYPE.CHANNEL) ? ExceptionError.STDA_SYSTEM_ERROR_DECODE(e.getCodeError()) : 0x5300250);

                packet_func.session_send(p,
                    _session, 1);
            }
        }

        public void requestClubSetWorkShopUpRank(Player _session, packet _packet)
        {
            //

            PangyaBinaryWriter p = new PangyaBinaryWriter();

            try
            {

                CWUpRank cwup = new CWUpRank().ToRead(_packet);
                List<stItemEx> v_item = new List<stItemEx>();
                stItemEx item = new stItemEx();

                uint stat = 2; // PWR, CTRL, ACCRY, SPIN e CURVE





                if (cwup.qntd > 0)
                {
                    var pCi = _session.m_pi.findCardByTypeid(cwup.item_typeid);

                    if (pCi == null)
                    {
                        throw new exception("[Channel::requestClubSetWorkShopUpRank][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou upar rank do ClubSet[ID=" + (cwup.clubset_id) + "], mas ele nao tem o Card[TYPEID=" + (cwup.item_typeid) + "] para upar o rank. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                            350, 0x5300351));
                    }

                    if (pCi.qntd < (int)cwup.qntd)
                    {
                        throw new exception("[Channel::requestClubSetWorkShopUpRank][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou upar rank do ClubSet[ID=" + (cwup.clubset_id) + "], mas ele nao tem quantidade suficiente de Card[TYPEID=" + (pCi._typeid) + ", ID=" + (pCi.id) + ", QNTD=" + (pCi.qntd) + ", request=" + (cwup.qntd) + "] para upar o rank. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                            351, 0x5300532));
                    }

                    // Card
                    item = new stItemEx();

                    item.type = 2;
                    item.id = (int)pCi.id;
                    item._typeid = pCi._typeid;
                    item.qntd = cwup.qntd;
                    item.STDA_C_ITEM_QNTD = (short)(item.qntd * -1);
                }

                var pClub = _session.m_pi.findWarehouseItemById(cwup.clubset_id);

                if (pClub == null)
                {
                    throw new exception("[Channel::requestClubSetWorkShopUpRank][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou upar rank do ClubSet[ID=" + (cwup.clubset_id) + "], mas ele nao tem esse ClubSet. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        352, 0x5300353));
                }

                var clubset = sIff.getInstance().findClubSet(pClub._typeid);

                if (clubset == null)
                {
                    throw new exception("[Channel::requestClubSetWorkShopUpRank][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou upar rank do ClubSet[TYPEID=" + (pClub._typeid) + ", ID=" + (pClub.id) + "], mas esse ClubSet nao existe no IFF_STRUCT do Server. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        353, 0x5300354));
                }

                if (clubset.work_shop.tipo == -1)
                {
                    throw new exception("[Channel::requestClubSetWorkShopUpRank][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou upar rank do ClubSet[TYPEID=" + (pClub._typeid) + ", ID=" + (pClub.id) + "], mas esse ClubSet nao é permitido upar de rank. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        354, 0x5300355));
                }

                // Stat Up
                var level_up_limit = sIff.getInstance().findClubSetWorkShopLevelUpLimit(clubset.work_shop.tipo);

                if (level_up_limit.empty())
                {
                    throw new exception("[Channel::requestClubSetWorkShopUpRank][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou upar rank do ClubSet[TYPEID=" + (pClub._typeid) + ", ID=" + (pClub.id) + "], IFF_STRUCT level_up_limit not found. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        208, 0x5300209));
                }

                // 
                var limit = level_up_limit.FirstOrDefault(el =>
                {
                    return el.rank == (pClub.clubset_workshop.calcRank(clubset.SlotStats.getSlot) + 1);
                });

                if (limit == null)
                {
                    throw new exception("[Channel::requestClubSetWorkShopUpRank][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou upar rank do ClubSet[TYPEID=" + (pClub._typeid) + ", ID=" + (pClub.id) + "], nao encontrou o level para upar no IFF_STRUCT do Server. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        210, 0x5300211));
                }

                if (cwup.qntd > 4)
                {
                    throw new exception("[Channel::requestClubSetWorkShopUpRank][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou upar rank do ClubSet[TYPEID=" + (pClub._typeid) + ", ID=" + (pClub.id) + "], mas a quantidade de card[TYPEID=" + (cwup.item_typeid) + ", QNTD=" + (cwup.qntd) + "] é desconhecida", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        355, 0x5300356));
                }

                // Stat Up, quando upar o Rank do ClubSet
                stat = (uint)(cwup.qntd == 0 ? 2 : (cwup.qntd == 1 ? 4 : (cwup.qntd == 2 ? 0 : (cwup.qntd == 3 ? 3 : (cwup.qntd == 4 ? 1 : 2)))));

                if (limit.c[stat] <= (pClub.clubset_workshop.c[stat] + clubset.SlotStats.getSlot[stat]))
                {
                    throw new exception("[Channel::requestClubSetWorkShopUpRank][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou upar rank do ClubSet[TYPEID=" + (pClub._typeid) + ", ID=" + (pClub.id) + "], mas o player nao pode mais upar esse stat[value=" + (stat) + "]. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        357, 0x5300358));
                }

                var rank_up_exp = sIff.getInstance().findClubSetWorkShopRankExp(clubset.work_shop.tipo_rank_s);

                if (rank_up_exp == null)
                {
                    throw new exception("[Channel::requestClubSetWorkShopUpRank][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou upar rank do ClubSet[TYPEID=" + (pClub._typeid) + ", ID=" + (pClub.id) + "], mas nao encontrou o Rank Up Exp no IFF_STRUCT do Server. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        358, 0x5300359));
                }

                // Rank do ClubSet +1 que ele vai tornar-se
                int rank = pClub.clubset_workshop.calcRank(clubset.SlotStats.getSlot) + 1;

                if (rank == -1)
                {
                    throw new exception("[Channel::requestClubSetWorkShopUpRank][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou upar rank do ClubSet[TYPEID=" + (pClub._typeid) + ", ID=" + (pClub.id) + "], mas pegou um rank desconhecido, System Error", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        360, 0x5300361));
                }

                if ((uint)pClub.clubset_workshop.mastery < rank_up_exp.rank[(uint)rank])
                {
                    throw new exception("[Channel::requestClubSetWorkShopUpRank][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou upar rank[rank=" + (rank) + "] do ClubSet[TYPEID=" + (pClub._typeid) + ", ID=" + (pClub.id) + "], mas ele nao tem mastery[value=" + (pClub.clubset_workshop.mastery) + ", request=" + (rank_up_exp.rank[(uint)rank]) + "] suficiente para upar o rank. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        359, 0x5300360));
                }

                // Remove Card
                if (item._typeid != 0)
                {
                    if (ItemManager.removeItem(item, _session) <= 0)
                    {
                        throw new exception("[Channel::requestClubSetWorkShopUpRank][Error] PLAYER [UID = " + (_session.m_pi.uid) + "] tentou upar rank do ClubSet[TYPEID = " + (pClub._typeid) + ", ID = " + (pClub.id) + "], mas nao conseguiu remover Card[TYPEID=" + (item._typeid) + ", ID=" + (item.id) + ", QNTD=" + (item.qntd) + "]", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                            356, 0x5300357));
                    }

                    v_item.Add(new stItemEx(item));
                }

                // UPDATE ON SERVER

                // Upa Stat do rank S, que da 1 de bonus
                if (rank == 5)
                {
                    if (clubset.work_shop.rank_s_stat > 4)
                    {
                        throw new exception("[Channel::requestClubSetWorkShopUpRank][Error] PLAYER [UID = " + (_session.m_pi.uid) + "] tentou upar rank do ClubSet[TYPEID = " + (pClub._typeid) + ", ID = " + (pClub.id) + "], mas o ClubSet Stat[value=" + (clubset.work_shop.rank_s_stat) + "] Rank S do IFF_STRUCT do Server é invalido. System Error", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                            361, 0x5300362));
                    }

                    // Rank S Bonus Stat
                    pClub.clubset_workshop.c[clubset.work_shop.rank_s_stat]++;
                }

                // Up Stat
                pClub.clubset_workshop.c[stat]++;
                pClub.clubset_workshop.recovery_pts = 0;
                pClub.clubset_workshop.rank = rank;
                pClub.clubset_workshop.level = pClub.clubset_workshop.calcLevel(clubset.SlotStats.getSlot);
                pClub.clubset_workshop.mastery -= rank_up_exp.rank[(uint)rank];

                // ClubSet
                item = new stItemEx();

                item.type = 0xCC;
                item.id = (int)pClub.id;
                item._typeid = pClub._typeid;
                item.clubset_workshop.c = pClub.clubset_workshop.c;
                item.clubset_workshop.level = (byte)pClub.clubset_workshop.level;
                item.clubset_workshop.mastery = pClub.clubset_workshop.mastery;
                item.clubset_workshop.rank = (uint)pClub.clubset_workshop.rank;
                item.clubset_workshop.recovery = pClub.clubset_workshop.recovery_pts;

                v_item.Add(new stItemEx(item));

                // UPDATE ON DB
                snmdb.NormalManagerDB.getInstance().add(12,
                    new CmdUpdateClubSetWorkshop(_session.m_pi.uid,
                        pClub,
                        CmdUpdateClubSetWorkshop.FLAG.F_UP_RANK),
                    SQLDBResponse, this);

                // UPDATE ON JOGO
                p.init_plain(0x216);

                p.WriteUInt32((uint)UtilTime.GetSystemTimeAsUnix());
                p.WriteUInt32((uint)v_item.Count);

                foreach (var el in v_item)
                {
                    p.WriteByte(el.type);
                    p.WriteUInt32(el._typeid);
                    p.WriteInt32(el.id);
                    p.WriteUInt32(el.flag_time);
                    p.WriteBytes(el.stat.ToArray());
                    p.WriteInt32((el.STDA_C_ITEM_TIME > 0) ? el.STDA_C_ITEM_TIME : el.STDA_C_ITEM_QNTD);
                    p.WriteZeroByte(25);
                    if (el.type == 0xCC)
                    {
                        p.WriteBytes(el.clubset_workshop.ToArray());
                    }
                }

                packet_func.session_send(p,
                    _session, 1);

                // Check Se Ele Pode Transformar e se ele transformou
                if (clubset.work_shop.flag_transformar == 1)
                { // Esse Clubset pode transformar-se em um ClubSet Special
                    Lottery lottery = new Lottery();

                    lottery.Push(250, 0x1000005D); // Wingtross Evo-Knight Club Set
                    lottery.Push(250, 0x1000005E); // Giga Yard Totem Pole Club Set
                    lottery.Push(250, 0x1000005F); // Duostar Manapikal Club Set
                    lottery.Push(750 * 14, 0); // Não Transforma nada

                    var lc = lottery.spinRoleta();

                    if (lc != null && Convert.ToInt32(lc.Value) != 0)
                    { // Transformou
                        var clubset_original = sIff.getInstance().findClubSetOriginal((uint)lc.Value);

                        if (clubset_original.empty())
                        {
                            throw new exception("[Channel::requestClubSetWorkShopUpRank][Error] PLAYER [UID = " + (_session.m_pi.uid) + "] tentou upar rank do ClubSet[TYPEID = " + (pClub._typeid) + ", ID = " + (pClub.id) + "], nao encontrou o Special ClubSet Original no IFF_STRUCT do Server. System Error", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                362, 0x5300363));
                        }

                        if (clubset_original.Count <= (uint)(rank - 1))
                        {
                            throw new exception("[Channel::requestClubSetWorkShopUpRank][Error] PLAYER [UID = " + (_session.m_pi.uid) + "] tentou upar rank do ClubSet[TYPEID = " + (pClub._typeid) + ", ID = " + (pClub.id) + "], nao tem o Rank[value=" + (rank) + "] do Special ClubSet Original no IFF_STRUCT do Server. System Error", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                363, 0x5300364));
                        }

                        // 
                        var it = clubset_original.FirstOrDefault(el =>
                        {
                            return WarehouseItemEx.ClubsetWorkshop.s_calcRank(el.SlotStats.getSlot) == rank;
                        });

                        if (it == null)
                        {
                            throw new exception("[Channel::requestClubSetWorkShopUpRank][Error] PLAYER [UID = " + (_session.m_pi.uid) + "] tentou upar rank do ClubSet[TYPEID = " + (pClub._typeid) + ", ID = " + (pClub.id) + "], nao encontrou o Rank[value=" + (rank) + "] no IFF_STRUCT do Server. System Error", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                364, 0x5300365));
                        }

                        // Não tem o ClubSet Sorteado, Envia para cliente um dialog se ele quer transformar o ClubSet ou não
                        if (!_session.m_pi.ownerItem(it.ID))
                        {

                            // Att taqueira que ele pode transformar se ele confirmar depois
                            _session.m_pi.cwtc.clubset_id = pClub.id;
                            _session.m_pi.cwtc.stat = stat;
                            _session.m_pi.cwtc.transform_typeid = it.ID;

                            // Log
                            _smp.message_pool.getInstance().push(new message("[ClubSetWorkshop::UpRank][Sucess] PLAYER [UID=" + (_session.m_pi.uid) + "] transformou o ClubSet[TYPEID=" + (pClub._typeid) + ", ID=" + (pClub.id) + "] no ClubSet[TYPEID=" + (it.ID) + "] Special, aguardando confirmacao do cliente.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            // Dialog de Transformação do ClubSet
                            p.init_plain(0x241);

                            packet_func.session_send(p,
                                _session, 1);

                            return;
                        }
                    }
                }
                // Fim do Check Transform ClubSet

                // Log
                _smp.message_pool.getInstance().push(new message("[ClubSetWorkshop::UpRank][Sucess] PLAYER [UID=" + (_session.m_pi.uid) + "] upou Rank[value=" + (rank) + "] do ClubSet[TYPEID=" + (pClub._typeid) + ", ID=" + (pClub.id) + "] Stat[value=" + (stat) + "" + ((rank == 5) ? (", Rank S bonus=" + (clubset.work_shop.rank_s_stat) + "") : "") + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Resposta para o ClubSet Workshop Up Rank
                p.init_plain(0x240);

                p.WriteUInt32(0); // OK
                p.WriteUInt32(stat);
                p.WriteInt32(pClub.id);

                packet_func.session_send(p,
                    _session, 1);

                // Update Achievement ON SERVER, DB and GAME
                AchievementSystem sys_achieve = new AchievementSystem();

                // Add +1 ao contado do Up Rank S ClubSet
                if (rank == 5)
                {
                    sys_achieve.incrementCounter(0x6C4000A7u);
                }

                sys_achieve.incrementCounter(0x6C4000A3u);

                sys_achieve.finish_and_update(_session);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestClubSetWorkShopUpRank][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x240);

                p.WriteUInt32((ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) == STDA_ERROR_TYPE.CHANNEL) ? ExceptionError.STDA_SYSTEM_ERROR_DECODE(e.getCodeError()) : 0x5300350);

                packet_func.session_send(p,
                    _session, 1);
            }
        }

        public void requestClubSetWorkShopUpRankTransformConfirm(Player _session, packet _packet)
        {
            //

            PangyaBinaryWriter p = new PangyaBinaryWriter();

            try
            {





                List<stItem> v_item = new List<stItem>();
                stItem item = new stItem();

                var pClub = _session.m_pi.findWarehouseItemById(_session.m_pi.cwtc.clubset_id);

                if (pClub == null)
                {
                    throw new exception("[Channel::requestClubSetWorkShopUpRankTransformConfirm][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou transformar ClubSet[ID=" + (_session.m_pi.cwtc.clubset_id) + "] no ClubSet[TYPEID=" + (_session.m_pi.cwtc.transform_typeid) + "] Special, mas ele nao tem o ClubSet. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        450, 0x5300451));
                }

                var clubset = sIff.getInstance().findClubSet(pClub._typeid);

                if (clubset == null)
                {
                    throw new exception("[Channel::requestClubSetWorkShopUpRankTransformConfirm][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou transformar ClubSet[ID=" + (_session.m_pi.cwtc.clubset_id) + "] no ClubSet[TYPEID=" + (_session.m_pi.cwtc.transform_typeid) + "] Special, mas nao existe o ClubSet no IFF_STRUCT do Server. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        451, 0x5300452));
                }

                var clubset_transform = sIff.getInstance().findClubSet(_session.m_pi.cwtc.transform_typeid);

                if (clubset_transform == null)
                {
                    throw new exception("[Channel::requestClubSetWorkShopUpRankTransformConfirm][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou transformar ClubSet[ID=" + (_session.m_pi.cwtc.clubset_id) + "] no ClubSet[TYPEID=" + (_session.m_pi.cwtc.transform_typeid) + "] Special, mas o ClubSet Special nao existe no IFF_STRUCT do Server. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        452, 0x5300453));
                }

                // ClubSet que se Transformou
                item = new stItem();

                item.type = 2;
                item.id = (int)pClub.id;
                item._typeid = pClub._typeid;
                item.qntd = 1;
                item.STDA_C_ITEM_QNTD = (short)(item.qntd * -1);


                // Delete ClubSet que vai ser transformado no ClubSet Special
                if (ItemManager.removeItem(item, _session) <= 0)
                {
                    throw new exception("[Channel::requestClubSetWorkShopUpRankTransformConfirm][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou transformar ClubSet[ID=" + (_session.m_pi.cwtc.clubset_id) + "] no ClubSet[TYPEID=" + (_session.m_pi.cwtc.transform_typeid) + "] Special, nao conseguiu deletar o ClubSet[TYPEID=" + (item._typeid) + ", ID=" + (item.id) + "] que vai ser transformado no Special. System Error", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        453, 0x5300454));
                }

                v_item.Add(new stItem(item));

                // ClubSet Transformado
                item = new stItem();

                BuyItem bi = new BuyItem();

                bi.id = -1;
                bi._typeid = clubset_transform.ID;
                bi.qntd = 1;

                ItemManager.initItemFromBuyItem(_session.m_pi,
                    item, bi, false, 0, 0, 1);

                if (item._typeid == 0)
                {
                    throw new exception("[Channel::requestClubSetWorkShopUpRankTransformConfirm][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou transformar ClubSet[ID=" + (_session.m_pi.cwtc.clubset_id) + "] no ClubSet[TYPEID=" + (_session.m_pi.cwtc.transform_typeid) + "] Special, nao conseguiu inicializar o ClubSet[TYPEID=" + (bi._typeid) + "]. System Error", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        454, 0x5300455));
                }

                var rt = ItemManager.RetAddItem.T_INIT_VALUE;

                if ((rt = ItemManager.addItem(item,
                    _session, 0, 0)) < 0)
                {
                    throw new exception("[Channel::requestClubSetWorkShopUpRankTransformConfirm][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou transformar ClubSet[ID=" + (_session.m_pi.cwtc.clubset_id) + "] no ClubSet[TYPEID=" + (_session.m_pi.cwtc.transform_typeid) + "] Special, nao conseguiu adicionar o ClubSet[TYPEID=" + (item._typeid) + "]", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        455, 0x5300456));
                }

                if (rt != ItemManager.RetAddItem.T_SUCCESS_PANG_AND_EXP_AND_CP_POUCH)
                {
                    v_item.Add(new stItem(item));
                }

                // Log, // Usa o clubset._typeid e _session.m_pi.cwtc.clubset_id por que já excluiu esse ClubSet o "pClub"
                _smp.message_pool.getInstance().push(new message("[ClubSetWokShop::UpRankTransformConfirm][Sucess] PLAYER [UID=" + (_session.m_pi.uid) + "] confirmou a transformacao do ClubSet[TYPEID=" + (clubset.ID) + ", ID=" + (_session.m_pi.cwtc.clubset_id) + "] no ClubSet[TYPEID=" + (item._typeid) + ", ID=" + (item.id) + "] Special", type_msg.CL_FILE_LOG_AND_CONSOLE));

                // UPDATE ON JOGO
                p.init_plain(0x216);

                p.WriteUInt32((uint)UtilTime.GetSystemTimeAsUnix());
                p.WriteUInt32((uint)v_item.Count);

                foreach (var el in v_item)
                {
                    p.WriteByte(el.type);
                    p.WriteUInt32(el._typeid);
                    p.WriteInt32(el.id);
                    p.WriteUInt32(el.flag_time);
                    p.WriteBytes(el.stat.ToArray());
                    p.WriteInt32((el.STDA_C_ITEM_TIME > 0) ? el.STDA_C_ITEM_TIME : el.STDA_C_ITEM_QNTD);
                    p.WriteZeroByte(25);
                }

                packet_func.session_send(p,
                    _session, 1);

                // Resposta para o ClubSet Workshop Up Rank Transform Confirm
                p.init_plain(0x242);

                p.WriteUInt32(0); // OK;

                p.WriteUInt32(item._typeid);
                p.WriteInt32(item.id);

                packet_func.session_send(p,
                    _session, 1);

                // Update Achievement ON SERVER, DB and GAME
                AchievementSystem sys_achieve = new AchievementSystem();

                sys_achieve.incrementCounter(0x6C4000A4u);

                sys_achieve.finish_and_update(_session);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestClubSetWorkShopUpRankTransformConfirm][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x242);

                p.WriteUInt32((ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) == STDA_ERROR_TYPE.CHANNEL) ? ExceptionError.STDA_SYSTEM_ERROR_DECODE(e.getCodeError()) : 0x5300450);

                packet_func.session_send(p,
                    _session, 1);
            }
        }

        public void requestClubSetWorkShopUpRankTransformCancel(Player _session, packet _packet)
        {
            //

            PangyaBinaryWriter p = new PangyaBinaryWriter();

            try
            {





                var pClub = _session.m_pi.findWarehouseItemById(_session.m_pi.cwtc.clubset_id);

                if (pClub == null)
                {
                    throw new exception("[Channel::requestClubSetWorkShopUpRankTransformCancel][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou cancelar o transformacao do ClubSet[ID=" + (_session.m_pi.cwtc.clubset_id) + "] no ClubSet[TYPEID=" + (_session.m_pi.cwtc.transform_typeid) + "] Special, mas ele nao tem o ClubSet. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        400, 0x5300401));
                }

                var clubset = sIff.getInstance().findClubSet(pClub._typeid);

                if (clubset == null)
                {
                    throw new exception("[Channel::requestClubSetWorkShopUpRankTransformCancel][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou cancelar o transformacao do ClubSet[ID=" + (_session.m_pi.cwtc.clubset_id) + "] no ClubSet[TYPEID=" + (_session.m_pi.cwtc.transform_typeid) + "] Special, mas o ClubSet nao existe no IFF_STRUCT do Server. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        401, 0x5300402));
                }

                if (_session.m_pi.cwtc.stat > 4)
                {
                    throw new exception("[Channel::requestClubSetWorkShopUpRankTransformCancel][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou cancelar o transformacao do ClubSet[ID=" + (_session.m_pi.cwtc.clubset_id) + "] no ClubSet[TYPEID=" + (_session.m_pi.cwtc.transform_typeid) + "] Special, mas o Stat[value=" + (_session.m_pi.cwtc.stat) + "] é invalido. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        402, 0x5300403));
                }

                // Log
                _smp.message_pool.getInstance().push(new message("[ClubSetWorkshop::UpRankTransformCancel][Sucess] PLAYER [UID=" + (_session.m_pi.uid) + "] cancelou a transformacao do ClubSet[TYPEID=" + (pClub._typeid) + ", ID=" + (pClub.id) + "] no ClubSet[TYPEID=" + (_session.m_pi.cwtc.transform_typeid) + "] Special", type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x243);

                p.WriteUInt32(0); // OK

                p.WriteUInt32(_session.m_pi.cwtc.stat);
                p.WriteInt32(_session.m_pi.cwtc.clubset_id);

                packet_func.session_send(p,
                    _session, 1);

                // Update Achievement ON SERVER, DB and GAME
                AchievementSystem sys_achieve = new AchievementSystem();

                sys_achieve.incrementCounter(0x6C4000A3u);

                sys_achieve.finish_and_update(_session);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestClubSetWorkShopUpRankTransformCancel][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x243);

                p.WriteUInt32((ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) == STDA_ERROR_TYPE.CHANNEL) ? ExceptionError.STDA_SYSTEM_ERROR_DECODE(e.getCodeError()) : 0x5300400);

                packet_func.session_send(p,
                    _session, 1);
            }
        }

        public void requestClubSetReset(Player _session, packet _packet)
        {
            //

            PangyaBinaryWriter p = new PangyaBinaryWriter();

            try
            {

                List<stItemEx> v_item = new List<stItemEx>();
                stItemEx item = new stItemEx();

                uint item_typeid = _packet.ReadUInt32();
                int clubset_id = _packet.ReadInt32();





                if (item_typeid != 0x1A00024B && item_typeid != 0x1A000247)
                {
                    throw new exception("[Channel::requestClubSetReset][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou resetar ClubSet[ID=" + (clubset_id) + "], mas o item[TYPEID=" + (item_typeid) + "] é desconhecido. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        505, 0x5300506));
                }

                var pWi = _session.m_pi.findWarehouseItemByTypeid(item_typeid);

                if (pWi == null)
                {
                    throw new exception("[Channel::requestClubSetReset][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou resetar ClubSet[ID=" + (clubset_id) + "], mas ele nao tem o item[TYPEID=" + (item_typeid) + "]. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        500, 0x5300501));
                }

                if (pWi.STDA_C_ITEM_QNTD < 1)
                {
                    throw new exception("[Channel::requestClubSetReset][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou resetar ClubSet[ID=" + (clubset_id) + "], mas ele nao tem quantidade suficiente do item[TYPEID=" + (pWi._typeid) + ", ID=" + (pWi.id) + ", QNTD=" + (pWi.STDA_C_ITEM_QNTD) + ", request=1]. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        501, 0x5300502));
                }

                var pClub = _session.m_pi.findWarehouseItemById(clubset_id);

                if (pClub == null)
                {
                    throw new exception("[Channel::requestClubSetReset][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou resetar ClubSet[ID=" + (clubset_id) + "], mas ele nao tem o ClubSet. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        502, 0x5300503));
                }

                var clubset = sIff.getInstance().findClubSet(pClub._typeid);

                if (clubset == null)
                {
                    throw new exception("[Channel::requestClubSetReset][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou resetar ClubSet[ID=" + (clubset_id) + "], mas o ClubSet nao existe no IFF_STRUCT do Server. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        503, 0x5300504));
                }

                int rank_base = WarehouseItemEx.ClubsetWorkshop.s_calcRank(clubset.SlotStats.getSlot);
                int rank = pClub.clubset_workshop.calcRank(clubset.SlotStats.getSlot);

                if (rank_base == -1 || rank == -1)
                {
                    throw new exception("[Channel::requestClubSetReset][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou resetar ClubSet[ID=" + (clubset_id) + "], nao conseguiu pegar o Rank do ClubSet[TYPEID=" + (pClub._typeid) + ", ID=" + (pClub.id) + ", rank=" + (rank) + ", rank_base=" + (rank_base) + "]", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        505, 0x5300506));
                }

                var rank_up_exp = sIff.getInstance().findClubSetWorkShopRankExp(clubset.work_shop.tipo_rank_s);

                if (rank_up_exp == null)
                {
                    throw new exception("[Channel::requestClubSetReset][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou resetar ClubSet[ID=" + (clubset_id) + "], mas nao encontrou o Rank Up Exp[tipo=" + (clubset.work_shop.tipo_rank_s) + "] no IFF_STRUCT do Server. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        504, 0x5300505));
                }

                // Item reset ClubSet
                item = new stItemEx();

                item.type = 2;
                item.id = (int)pWi.id;
                item._typeid = pWi._typeid;
                item.qntd = 1;
                item.STDA_C_ITEM_QNTD = (short)(item.qntd * -1);

                if (ItemManager.removeItem(item, _session) <= 0)
                {
                    throw new exception("[Channel::requestClubSetReset][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou resetar ClubSet[ID=" + (clubset_id) + "], mas nao conseguiu remover o Item[TYPEID=" + (item._typeid) + ", ID=" + (item.id) + "]. ErrorSystem", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        506, 0x5300507));
                }

                v_item.Add(new stItemEx(item));

                uint mastery = 0;
                long pang = 0;

                if (item_typeid == 0x1A00024B)
                { // Hard Reset devolve 50% do Pang e Mastery gasto no ClubSet

                    Enchant enchant = null;

                    // Soma Todo Mastery Gasto no ClubSet
                    for (var i = rank_base + 1; i <= rank; ++i)
                    {
                        mastery += rank_up_exp.rank[i];
                    }

                    // Soma Todo Pang Gasto no ClubSet
                    for (var i = 0u; i < (pClub.c.Length); ++i)
                    {
                        for (var j = 0u; j < (uint)pClub.c[i]; ++j)
                        {
                            if ((enchant = sIff.getInstance().findEnchant(((Convert.ToUInt32(sIff.getInstance().ENCHANT) << 26) | (i << 20) + j))) != null)
                            {
                                pang += enchant.Pang;
                            }
                        }
                    }

                    // Metade
                    mastery = (uint)(mastery * 0.5f);
                    pang = (long)(pang * 0.5f);

                    pClub.clubset_workshop.mastery += mastery;

                    // Só atualiza os pangs se for maior que zero
                    if (pang > 0)
                    {
                        _session.m_pi.addPang((ulong)pang);
                    }

                    p.init_plain(0xC8);

                    p.WriteUInt64(_session.m_pi.ui.pang);
                    p.WriteInt64(pang);

                    packet_func.session_send(p,
                        _session, 1);

                }

                // UPDATE ON SERVER

                // Reseta ClubSet Workshop Stats 
                pClub.clubset_workshop.c = new short[5];

                pClub.clubset_workshop.level = 0;
                pClub.clubset_workshop.rank = 0;
                pClub.clubset_workshop.recovery_pts = 0;
                // Reseta ClubSet Stats 
                pClub.c = new short[5];

                // Atualiza o stats do ClubSet Workshop
                item = new stItemEx();

                item.type = 0xCC;
                item.id = (int)pClub.id;
                item._typeid = pClub._typeid;
                item.clubset_workshop.c = pClub.clubset_workshop.c;
                item.clubset_workshop.level = (byte)pClub.clubset_workshop.level;
                item.clubset_workshop.mastery = pClub.clubset_workshop.mastery;
                item.clubset_workshop.rank = (uint)pClub.clubset_workshop.rank;
                item.clubset_workshop.recovery = pClub.clubset_workshop.recovery_pts;

                v_item.Add(new stItemEx(item));

                // Atualiza os stats do ClubSet
                item.type = 0xC9;
                item.c = pClub.c;

                v_item.Add(new stItemEx(item));

                // UPDATE ON DB

                // Reset ON DB ClubSet Workshop
                snmdb.NormalManagerDB.getInstance().add(12,
                    new CmdUpdateClubSetWorkshop(_session.m_pi.uid,
                        pClub,
                        CmdUpdateClubSetWorkshop.FLAG.F_RESET),
                    SQLDBResponse, this);

                // Reset ON DB ClubSet Stats
                snmdb.NormalManagerDB.getInstance().add(8,
                    new CmdUpdateClubSetStats(_session.m_pi.uid,
                        pClub, 0),
                    SQLDBResponse, this);

                // Log
                _smp.message_pool.getInstance().push(new message("[ClubSet::Reset][Sucess] PLAYER [UID=" + (_session.m_pi.uid) + "] resetou o ClubSet[TYPEID=" + (pClub._typeid) + ", ID=" + (pClub.id) + "] " + (item_typeid == 0x1A00024B ? ("Hard[Pang=" + (pang) + ", Mastery=" + (mastery) + "] Item") : "Soft Item"), type_msg.CL_FILE_LOG_AND_CONSOLE));

                // UPDATE ON JOGO
                p.init_plain(0x216);

                p.WriteUInt32((uint)UtilTime.GetSystemTimeAsUnix());
                p.WriteUInt32((uint)v_item.Count);

                foreach (var el in v_item)
                {
                    p.WriteByte(el.type);
                    p.WriteUInt32(el._typeid);
                    p.WriteInt32(el.id);
                    p.WriteUInt32(el.flag_time);
                    p.WriteBytes(el.stat.ToArray());
                    p.WriteInt32((el.STDA_C_ITEM_TIME > 0) ? el.STDA_C_ITEM_TIME : el.STDA_C_ITEM_QNTD);
                    p.WriteInt16(el.c);
                    p.WriteZeroByte(15);
                    if (el.type == 0xCC)
                    {
                        p.WriteBytes(el.clubset_workshop.ToArray());
                    }
                }

                packet_func.session_send(p,
                    _session, 1);

                // Resposta para o ClubSet Reset
                p.init_plain(0x247);

                p.WriteUInt32(0); // OK

                p.WriteUInt32(pClub._typeid);
                p.WriteInt32(pClub.id);

                packet_func.session_send(p,
                    _session, 1);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestClubSetReset][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x247);

                p.WriteUInt32((ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) == STDA_ERROR_TYPE.CHANNEL) ? ExceptionError.STDA_SYSTEM_ERROR_DECODE(e.getCodeError()) : 0x5300500);

                packet_func.session_send(p,
                    _session, 1);
            }
        }

        public void requestMakeTutorial(Player _session, packet _packet)
        {
            //

            PangyaBinaryWriter p = new PangyaBinaryWriter();

            try
            {

                stItem item = new stItem();

                item.type = 2;
                item.id = -1;

                string msg = "";

                //eu poderia fazer melhor, alias sem outras class(structs internas)
                RequestMakeTutorial rmt = RequestMakeTutorial.Load(_packet.GetRemainingData);//acrisio que fez a strutura
                                                                                             //'.' eu só repliquei







                switch (rmt.uTipo.stTipo.tipo)
                {
                    case 0: // Rookie
                        {
                            if (rmt.uTipo.stTipo.tipo == 0 && (_session.m_pi.TutoInfo.rookie & rmt.uValor.stValor.rookie.ucbyte) != 0)
                            {
                                throw new exception("[Channel::requestMakeTutorial][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou fazer tutorial[tipo=" + (rmt.uTipo.stTipo.tipo) + ", value=" + (rmt.uValor.ulValor) + "], mas ele ja concluiu esse tutorial. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                    550, 0x5300551));
                            }

                            if (rmt.uValor.stValor.rookie.st8bit._bit2 || rmt.uValor.stValor.rookie.st8bit._bit3)
                            {
                                if (_session.m_pi.TutoInfo.rookie < 3) // Error não concluiu os outros tutoriais para liberar esse
                                {
                                    throw new exception("[Channel::requestMakeTutorial][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou fazer tutorial[tipo=" + (rmt.uTipo.stTipo.tipo) + ", value=" + (rmt.uValor.ulValor) + "], mas ele nao concluiu os outros tutoriais para poder completar o Rookie. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                        553, 0x5300554));
                                }
                            }
                            else if (rmt.uValor.stValor.rookie.st8bit._bit4)
                            {
                                if ((_session.m_pi.TutoInfo.rookie & 7) <= 3) // Error não concluiu os outros tutoriais para liberar esse
                                {
                                    throw new exception("[Channel::requestMakeTutorial][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou fazer tutorial[tipo=" + (rmt.uTipo.stTipo.tipo) + ", value=" + (rmt.uValor.ulValor) + "], mas ele nao concluiu os outros tutoriais para poder completar o Rookie. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                        553, 0x5300554));
                                }
                            }
                            else if (rmt.uValor.stValor.rookie.st8bit._bit6)
                            {
                                if ((_session.m_pi.TutoInfo.rookie & 11) <= 3) // Error não concluiu os outros tutoriais para liberar esse
                                {
                                    throw new exception("[Channel::requestMakeTutorial][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou fazer tutorial[tipo=" + (rmt.uTipo.stTipo.tipo) + ", value=" + (rmt.uValor.ulValor) + "], mas ele nao concluiu os outros tutoriais para poder completar o Rookie. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                        553, 0x5300554));
                                }
                            }
                            else if (rmt.uValor.stValor.rookie.st8bit._bit5)
                            {
                                if ((_session.m_pi.TutoInfo.rookie & 15) <= 3) // Error não concluiu os outros tutoriais para liberar esse
                                {
                                    throw new exception("[Channel::requestMakeTutorial][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou fazer tutorial[tipo=" + (rmt.uTipo.stTipo.tipo) + ", value=" + (rmt.uValor.ulValor) + "], mas ele nao concluiu os outros tutoriais para poder completar o Rookie. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                        553, 0x5300554));
                                }
                            }
                            else if (((rmt.uValor.stValor.rookie.ucbyte - 1) & _session.m_pi.TutoInfo.rookie) != (rmt.uValor.stValor.rookie.ucbyte - 1)) // Error não concluiu os outros tutoriais para liberar esse
                            {
                                throw new exception("[Channel::requestMakeTutorial][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou fazer tutorial[tipo=" + (rmt.uTipo.stTipo.tipo) + ", value=" + (rmt.uValor.ulValor) + "], mas ele nao concluiu os outros tutoriais para poder completar o Rookie. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                    553, 0x5300554));
                            }

                            _session.m_pi.TutoInfo.rookie |= rmt.uValor.ulValor;

                            // Send Item Reward Clear Tutorial
                            switch (rmt.uValor.stValor.rookie.st8bit.whatBit())
                            {
                                case 1: // Pang Mastery
                                    item._typeid = 0x1A000002;
                                    item.qntd = Convert.ToInt32(item.STDA_C_ITEM_QNTD = 3);//item.STDA_C_ITEM_QNTD = 10;
                                    break;
                                case 2: // Tranquilizande de Cookies
                                    item._typeid = 0x1800000B;
                                    item.qntd = Convert.ToInt32(item.STDA_C_ITEM_QNTD = 3);//item.STDA_C_ITEM_QNTD = 3;
                                    break;
                                case 3: // Power Milk
                                    item._typeid = 0x18000025;
                                    item.qntd = Convert.ToInt32(item.STDA_C_ITEM_QNTD = 3);//item.STDA_C_ITEM_QNTD = 3;
                                    break;
                                case 4: // Olho Magico
                                    item._typeid = 0x18000005;
                                    item.qntd = Convert.ToInt32(item.STDA_C_ITEM_QNTD = 3);//item.STDA_C_ITEM_QNTD = 3;
                                    break;
                                case 5: // Açai
                                    item._typeid = 0x18000004;
                                    item.qntd = Convert.ToInt32(item.STDA_C_ITEM_QNTD = 3);//item.STDA_C_ITEM_QNTD = 3;
                                    break;
                                case 6: // Duostar lucky pangya cookie
                                    item._typeid = 0x1800000A;
                                    item.qntd = Convert.ToInt32(item.STDA_C_ITEM_QNTD = 3);//item.STDA_C_ITEM_QNTD = 3;
                                    break;
                                case 7: // Spin Mastery(Guaraná)
                                    item._typeid = 0x18000000;
                                    item.qntd = Convert.ToInt32(item.STDA_C_ITEM_QNTD = 3);//item.STDA_C_ITEM_QNTD = 3;
                                    break;
                                case 8: // Pang Pouch
                                    item._typeid = PANG_POUCH_TYPEID;
                                    item.qntd = Convert.ToInt32(item.STDA_C_ITEM_QNTD = 1000);//item.STDA_C_ITEM_QNTD = 1000;
                                    break;
                                case 0:
                                default:
                                    throw new exception("[Channel::requestMakeTutorial][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou fazer tutorial[tipo=" + (rmt.uTipo.stTipo.tipo) + ", value=" + (rmt.uValor.ulValor) + "], o valor do tutorial é desconhecido. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                        555, 0x5300556));
                            }

                            msg = "NICE TUTORIAL ROOKIE CLEAR";

                            // Send Item para mailbox do player que concluiu o Tutorial
                            MailBoxManager.sendMessageWithItem(0,
                                _session.m_pi.uid, msg, item);

                            _smp.message_pool.getInstance().push(new message("[Tutorial][Sucess] PLAYER [UID=" + (_session.m_pi.uid) + "] Concluiu Tutorial Rookie", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            // Concluiu o Tutorial Rookie
                            if ((_session.m_pi.TutoInfo.rookie & 0xFF) != 0 && rmt.uTipo.stTipo.finish != 0)
                            {

                                List<stItem> v_item = new List<stItem>();

                                item = new stItem();

                                item.type = 2;
                                item.id = (int)-1;
                                item._typeid = 0x1C000000; // Papel
                                item.qntd = Convert.ToInt32(item.STDA_C_ITEM_QNTD = 1);//item.STDA_C_ITEM_QNTD = 1;

                                v_item.Add(new stItem(item));

                                item = new stItem();

                                item.type = 2;
                                item.id = (int)-1;
                                item._typeid = 0x10000014; // Air Knight Lucky Set
                                item.qntd = Convert.ToInt32(item.STDA_C_ITEM_QNTD = 1);//item.STDA_C_ITEM_QNTD = 1;

                                v_item.Add(new stItem(item));

                                msg = "NICE ALL TUTORIAL ROOKIE CLEAR";

                                // Send Item para mailbox do player que concluiu todos os Tutoriais Rookie
                                MailBoxManager.sendMessageWithItem(0,
                                    _session.m_pi.uid, msg, v_item);

                                // UPDATE ON DB
                                snmdb.NormalManagerDB.getInstance().add(14,
                                    new CmdTutoEventClear(_session.m_pi.uid, CmdTutoEventClear.T_ROOKIE),
                                    SQLDBResponse, this);

                                _smp.message_pool.getInstance().push(new message("[Tutorial][Sucess] PLAYER [UID=" + (_session.m_pi.uid) + "] Concluiu Todos Tutoriais Rookie", type_msg.CL_FILE_LOG_AND_CONSOLE)); // UPDATE ON DB
                            }
                            break;
                        }
                    case 1: // Beginner
                        {
                            if (rmt.uTipo.stTipo.tipo == 1 && (_session.m_pi.TutoInfo.beginner & rmt.uValor.stValor.beginner.ucbyte) != 0)
                            {
                                throw new exception("[Channel::requestMakeTutorial][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou fazer tutorial[tipo=" + (rmt.uTipo.stTipo.tipo) + ", value=" + (rmt.uValor.ulValor) + "], mas ele ja concluiu esse tutorial. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                    550, 0x5300551));
                            }

                            // Check Rookie Concluido
                            if (_session.m_pi.TutoInfo.rookie == 1 && 0xFF != 0xFF)
                            {
                                throw new exception("[Channel::requestMakeTutorial][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou fazer tutorial[tipo=" + (rmt.uTipo.stTipo.tipo) + ", value=" + (rmt.uValor.ulValor) + "], mas ele nao concluiu o tutorial rookie. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                    554, 0x5300555));
                            }

                            RequestMakeTutorial.u2 tutu = new RequestMakeTutorial.u2() { ulValor = _session.m_pi.TutoInfo.beginner };

                            if (rmt.uValor.stValor.beginner.st8bit._bit1 || rmt.uValor.stValor.beginner.st8bit._bit2)
                            {
                                if (tutu.stValor.beginner.ucbyte < 1)
                                {
                                    throw new exception("[Channel::requestMakeTutorial][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou fazer tutorial[tipo=" + (rmt.uTipo.stTipo.tipo) + ", value=" + (rmt.uValor.ulValor) + "], mas ele nao concluiu os outros tutoriais para poder completar o Beginner. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                        553, 0x5300554));
                                }
                            }
                            else if (rmt.uValor.stValor.beginner.st8bit._bit4 || rmt.uValor.stValor.beginner.st8bit._bit5)
                            {
                                if (tutu.stValor.beginner.ucbyte < 15)
                                {
                                    throw new exception("[Channel::requestMakeTutorial][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou fazer tutorial[tipo=" + (rmt.uTipo.stTipo.tipo) + ", value=" + (rmt.uValor.ulValor) + "], mas ele nao concluiu os outros tutoriais para poder completar o Beginner. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                        553, 0x5300554));
                                }
                            }
                            else if (((rmt.uValor.stValor.beginner.ucbyte - 1) & tutu.stValor.beginner.ucbyte) != (rmt.uValor.stValor.beginner.ucbyte - 1))
                            {
                                throw new exception("[Channel::requestMakeTutorial][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou fazer tutorial[tipo=" + (rmt.uTipo.stTipo.tipo) + ", value=" + (rmt.uValor.ulValor) + "], mas ele nao concluiu os outros tutoriais para poder completar o Beginner. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                    553, 0x5300554));
                            }

                            _session.m_pi.TutoInfo.beginner |= rmt.uValor.ulValor;

                            // Send Item Reward Clear Tutorial
                            switch (rmt.uValor.stValor.beginner.st8bit.whatBit())
                            {
                                case 1: // Pang Mastery
                                    item._typeid = 0x1A000002;
                                    item.qntd = Convert.ToInt32(item.STDA_C_ITEM_QNTD = 10);//item.STDA_C_ITEM_QNTD = 10;
                                    break;
                                case 2: // Safety
                                    item._typeid = 0x18000028;
                                    item.qntd = Convert.ToInt32(item.STDA_C_ITEM_QNTD = 1);//item.STDA_C_ITEM_QNTD = 1;
                                    break;
                                case 3: // Corta vento
                                    item._typeid = 0x18000006;
                                    item.qntd = Convert.ToInt32(item.STDA_C_ITEM_QNTD = 1);//item.STDA_C_ITEM_QNTD = 1;
                                    break;
                                case 4: // Duostar lucky pangya cookie
                                    item._typeid = 0x1800000A;
                                    item.qntd = Convert.ToInt32(item.STDA_C_ITEM_QNTD = 3);
                                    break;
                                case 5: // Spin Mastery(Guaraná)
                                    item._typeid = 0x18000000;
                                    item.qntd = Convert.ToInt32(item.STDA_C_ITEM_QNTD = 4);// item.STDA_C_ITEM_QNTD = 4;
                                    break;
                                case 6: // Banana
                                    item._typeid = 0x18000001;
                                    item.qntd = Convert.ToInt32(item.STDA_C_ITEM_QNTD = 3);// item.STDA_C_ITEM_QNTD = 3;
                                    break;
                                case 7:
                                case 8:
                                case 0:
                                default:
                                    throw new exception("[Channel::requestMakeTutorial][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou fazer tutorial[tipo=" + (rmt.uTipo.stTipo.tipo) + ", value=" + (rmt.uValor.ulValor) + "], o valor do tutorial é desconhecido. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                        555, 0x5300556));
                            }

                            msg = "NICE TUTORIAL BEGINNER CLEAR";

                            // Send Item para mailbox do player que concluiu o Tutorial
                            MailBoxManager.sendMessageWithItem(0,
                                _session.m_pi.uid, msg, item);

                            _smp.message_pool.getInstance().push(new message("[Tutorial][Sucess] PLAYER [UID=" + (_session.m_pi.uid) + "] Concluiu Tutorial Beginner", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            // Concluiu o Tutorial Beginner
                            if (_session.m_pi.TutoInfo.beginner == (0x3F << 8))
                            {

                                List<stItem> v_item = new List<stItem>();

                                item = new stItem();

                                item.type = 2;
                                item.id = (int)-1;
                                item._typeid = 0x18000027; // Power +15y Item
                                item.qntd = (int)(item.STDA_C_ITEM_QNTD = 10);

                                v_item.Add(new stItem(item));

                                item = new stItem();

                                item.type = 2;
                                item.id = (int)-1;
                                item._typeid = PANG_POUCH_TYPEID; // Pang Pouch 10k Pang
                                item.qntd = (int)(item.STDA_C_ITEM_QNTD = 10000);

                                v_item.Add(new stItem(item));

                                msg = "NICE ALL TUTORIAL BEGINNER CLEAR";

                                // Send Item para mailbox do player que concluiu todos os Tutoriais Beginner
                                MailBoxManager.sendMessageWithItem(0,
                                    _session.m_pi.uid, msg, v_item);

                                // UPDATE ON DB
                                snmdb.NormalManagerDB.getInstance().add(14,
                                    new CmdTutoEventClear(_session.m_pi.uid, CmdTutoEventClear.T_BEGINNER),
                                    SQLDBResponse, this);

                                _smp.message_pool.getInstance().push(new message("[Tutorial][Sucess] PLAYER [UID=" + (_session.m_pi.uid) + "] Concluiu Todos Tutoriais Beginner", type_msg.CL_FILE_LOG_AND_CONSOLE)); // UPDATE ON DB
                            }
                            break;
                        }
                    case 2: // Advancer(ACHO)
                        {
                            if (rmt.uTipo.stTipo.tipo == 2 && (_session.m_pi.TutoInfo.advancer & rmt.uValor.stValor.advancer.ucbyte) != 0)
                            {
                                throw new exception("[Channel::requestMakeTutorial][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou fazer tutorial[tipo=" + (rmt.uTipo.stTipo.tipo) + ", value=" + (rmt.uValor.ulValor) + "], mas ele ja concluiu esse tutorial. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                    550, 0x5300551));
                            }

                            // Check Rookie Concluido
                            if (_session.m_pi.TutoInfo.rookie == 1 && 0xFF != 0xFF)
                            {
                                throw new exception("[Channel::requestMakeTutorial][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou fazer tutorial[tipo=" + (rmt.uTipo.stTipo.tipo) + ", value=" + (rmt.uValor.ulValor) + "], mas ele nao concluiu o tutorial rookie. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                    554, 0x5300555));
                            }

                            // Check Beginner Concluido
                            if (_session.m_pi.TutoInfo.beginner == 1 && 0x3F != 0x3F)
                            {
                                throw new exception("[Channel::requestMakeTutorial][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou fazer tutorial[tipo=" + (rmt.uTipo.stTipo.tipo) + ", value=" + (rmt.uValor.ulValor) + "], mas ele nao concluiu o tutorial Beginner. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                    554, 0x5300555));
                            }

                            RequestMakeTutorial.u2 tutu = new RequestMakeTutorial.u2() { ulValor = _session.m_pi.TutoInfo.advancer };

                            if (((rmt.uValor.stValor.advancer.ucbyte - 1) & tutu.stValor.advancer.ucbyte) != (rmt.uValor.stValor.advancer.ucbyte - 1))
                            {
                                throw new exception("[Channel::requestMakeTutorial][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou fazer tutorial[tipo=" + (rmt.uTipo.stTipo.tipo) + ", value=" + (rmt.uValor.ulValor) + "], mas ele nao concluiu os outros tutoriais para poder completar o Advancer. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                    553, 0x5300554));
                            }

                            _session.m_pi.TutoInfo.advancer |= rmt.uValor.ulValor;

                            msg = "NICE TUTORIAL ADVANCER CLEAR";

                            // Send Item para mailbox do player que concluiu o Tutorial
                            MailBoxManager.sendMessageWithItem(0,
                                _session.m_pi.uid, msg, item);

                            _smp.message_pool.getInstance().push(new message("[Tutorial][Sucess] PLAYER [UID=" + (_session.m_pi.uid) + "] Concluiu Tutorial Advancer", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            // Concluiu o Tutorial Advancer (ACHO)
                            if (_session.m_pi.TutoInfo.advancer == (0x7 << 16) && rmt.uTipo.stTipo.finish != 0)
                            {

                                // Esse não tem estou deixando por questão de quando eu implementar ou só para ter mesmo
                                List<stItem> v_item = new List<stItem>();

                                item = new stItem();

                                item.type = 2;
                                item.id = (int)-1;
                                item._typeid = 0x18000000; // Spin Mastery(Guaraná)
                                item.qntd = (int)(item.STDA_C_ITEM_QNTD = 1);

                                v_item.Add(new stItem(item));

                                item = new stItem();

                                item.type = 2;
                                item.id = (int)-1;
                                item._typeid = PANG_POUCH_TYPEID; // Pang Pouch 30k Pang
                                item.qntd = (int)(item.STDA_C_ITEM_QNTD = 30000);

                                v_item.Add(new stItem(item));

                                msg = "NICE ALL TUTORIAL ADVANCER CLEAR";

                                // Send Item para mailbox do player que concluiu todos os Tutoriais Advancer
                                MailBoxManager.sendMessageWithItem(0,
                                    _session.m_pi.uid, msg, v_item);

                                // UPDATE ON DB
                                snmdb.NormalManagerDB.getInstance().add(14,
                                    new CmdTutoEventClear(_session.m_pi.uid, CmdTutoEventClear.T_ADVANCER),
                                    SQLDBResponse, this);

                                _smp.message_pool.getInstance().push(new message("[Tutorial][Sucess] PLAYER [UID=" + (_session.m_pi.uid) + "] Concluiu Todos Tutoriais Advancer", type_msg.CL_FILE_LOG_AND_CONSOLE)); // UPDATE ON DB
                            }
                            break;
                        }
                    default:
                        throw new exception("[Channel::requestMakeTutorial][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou fazer tutorial[tipo=" + (rmt.uTipo.stTipo.tipo) + ", value=" + (rmt.uValor.ulValor) + "], tipo desconhecido. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                            551, 0x5300552));
                }

                // UPDATE ON DB
                snmdb.NormalManagerDB.getInstance().add(13,
                    new CmdUpdateTutorial(_session.m_pi.uid, _session.m_pi.TutoInfo),
                    SQLDBResponse, this);

                // UPDATE ON JOGO
                // Resposta do Make Tutorial
                p.init_plain(0x11F);

                p.WriteByte(rmt.uTipo.stTipo.tipo); // 0 Rookie, 1 Beginner, 2 Advancer(ACHO), 3 Init Todos
                p.WriteByte(1); // Finish Tutorial Normal ou O Tipo

                if (rmt.uTipo.stTipo.tipo == 0)
                {
                    p.WriteUInt32(_session.m_pi.TutoInfo.rookie);
                }
                else if (rmt.uTipo.stTipo.tipo == 1)
                {
                    p.WriteUInt32(_session.m_pi.TutoInfo.beginner);
                }
                else if (rmt.uTipo.stTipo.tipo == 2)
                {
                    p.WriteUInt32(_session.m_pi.TutoInfo.advancer);
                }

                packet_func.session_send(p,
                    _session, 1);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestMakeTutorial][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Tenho que achar outro pacote que só envie erro para o cliente, esse pacote é de inicializar os info do player
                p.init_plain(0x44);

                p.WriteByte(0xE2); // Error
                p.WriteUInt32((ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) == STDA_ERROR_TYPE.CHANNEL) ? ExceptionError.STDA_SYSTEM_ERROR_DECODE(e.getCodeError()) : 0x5300550);

                packet_func.session_send(p,
                    _session, 1);
            }
        }

        public void requestEnterWebLinkState(Player _session, packet _packet)
        {
            //

            try
            {

                // Att Lugar que o player está, ele está vendo weblink
                _session.m_pi.place = _packet.ReadSByte();

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestEnterWebLinkState][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestCookie(Player _session, packet _packet)
        {
            //

            PangyaBinaryWriter p = new PangyaBinaryWriter();

            try
            {
                // Sempre atualiza o Cookie do server com o valor que está no banco de dados

                // Update cookie do server com o que está no banco de dados
                _session.m_pi.updateCookie();

                // Update ON GAME
                p.init_plain(0x96);

                p.WriteUInt64(_session.m_pi.cookie);

                packet_func.session_send(p,
                    _session, 1);

                // Vou colocar aqui para atualizar os Grand Zodiac Pontos por que quando eu fazer o evento o Grand Zodiac ele vai consumir os pontos na página web, 
                // aí vou atualizar aqui com o do banco de dados
                CmdGrandZodiacPontos cmd_gzp = new CmdGrandZodiacPontos(_session.m_pi.uid,
                    CmdGrandZodiacPontos.eCMD_GRAND_ZODIAC_TYPE.CGZT_GET);

                snmdb.NormalManagerDB.getInstance().add(0,
                    cmd_gzp, null, null);

                if (cmd_gzp.getException().getCodeError() != 0)
                {
                    throw cmd_gzp.getException();
                }

                _session.m_pi.grand_zodiac_pontos = cmd_gzp.getPontos();

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestCookie][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestUpdateGachaCoupon(Player _session, packet _packet)
        {
            //

            PangyaBinaryWriter p = new PangyaBinaryWriter();

            try
            {





                CmdCouponGacha cmd_cg = new CmdCouponGacha(_session.m_pi.uid); // Waiter

                snmdb.NormalManagerDB.getInstance().add(0,
                    cmd_cg, SQLDBResponse,
                    this);

                if (cmd_cg.getException().getCodeError() != 0)
                {
                    throw cmd_cg.getException();
                }

                _session.m_pi.cg = cmd_cg.getCouponGacha();

                // Update no Warehouse Item
                byte find_ticket_and_sub = 0;

                foreach (var el in _session.m_pi.mp_wi)
                {

                    switch (el.Value._typeid)
                    {
                        case 0x1A000080: // Gacha Ticket
                            el.Value.STDA_C_ITEM_QNTD = (short)_session.m_pi.cg.normal_ticket;
                            find_ticket_and_sub = 1;
                            break;
                        case 0x1A000083: // Gacha Sub Ticket
                            el.Value.STDA_C_ITEM_QNTD = (short)_session.m_pi.cg.partial_ticket;
                            find_ticket_and_sub |= 2;
                            break;
                    }

                    if (find_ticket_and_sub == 3)
                    {
                        break;
                    }
                }

                packet_func.session_send(packet_func.pacote102(_session.m_pi),
                    _session, 1);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestUpdateGachaCoupon][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Error envia o dizendo que deu erro no sistema
                p.init_plain(0x44);

                p.WriteByte(0xE2);

                p.WriteUInt32((ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) == STDA_ERROR_TYPE.CHANNEL) ? ExceptionError.STDA_SYSTEM_ERROR_DECODE(e.getCodeError()) : 0x5300600);

                packet_func.session_send(p,
                    _session, 1);
            }

        }

        public void requestOpenBoxMail(Player _session, packet _packet)
        {
            //

            PangyaBinaryWriter p = new PangyaBinaryWriter();

            try
            {

                if (!sBoxSystem.getInstance().isLoad())
                {
                    sBoxSystem.getInstance().load();
                }

                uint box_typeid = _packet.ReadUInt32();





                if (box_typeid == 0)
                {
                    throw new exception("[Channel::requestOpenBoxMail][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou abrir Box[TYPEID=" + (box_typeid) + "], mas o typeid é invalido(zero). Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        1, 0x6300101));
                }

                var pWi = _session.m_pi.findWarehouseItemByTypeid(box_typeid);

                if (pWi == null)
                {
                    throw new exception("[Channel::requestOpenBoxMail][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou abrir Box[TYPEID=" + (box_typeid) + "], mas ele nao tem essa Box. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        2, 0x6300102));
                }

                if (pWi.STDA_C_ITEM_QNTD < 1)
                {
                    throw new exception("[Channel::requestOpenBoxMail][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou abrir Box[TYPEID=" + (pWi._typeid) + ", ID=" + (pWi.id) + "], mas ele nao tem quantidade suficiente da Box[value=" + (pWi.STDA_C_ITEM_QNTD) + ", request=1]", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        3, 0x6300103));
                }

                if (sIff.getInstance().getItemGroupIdentify(pWi._typeid) != IFF_GROUP.ITEM)
                {
                    throw new exception("[Channel::requestOpenBoxMail][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou abrir Box[TYPEID=" + (pWi._typeid) + ", ID=" + (pWi.id) + "], mas nao é uma Box valida. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        4, 0x6300104));
                }

                var item_iff = sIff.getInstance().findItem(pWi._typeid);

                if (item_iff == null)
                {
                    throw new exception("[Channel::requestOpenBoxMail][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou abrir Box[TYPEID=" + (pWi._typeid) + ", ID=" + (pWi.id) + "], mas nao tem essa Box no IFF_STRUCT do Server. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        5, 0x6300105));
                }

                var box = sBoxSystem.getInstance().findBox(pWi._typeid);

                if (box == null)
                {
                    throw new exception("[Channel::requestOpenBoxMail][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou abrir Box[TYPEID=" + (pWi._typeid) + ", ID=" + (pWi.id) + "], mas nao tem essa Box no Box System do Server. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        6, 0x6300106));
                }

                List<stItem> v_item = new List<stItem>();
                stItem item = new stItem();

                ctx_box_item ctx_bi = null;
                Mascot mascot = null;

                string msg = box.msg;

                switch (pWi._typeid)
                {
                    case SPINNING_CUBE_TYPEID:
                        {
                            // Openned Spinning Cube, Ele ganha por abrir o spinning cube, e chave que gasta uma para abrir o spinning cube

                            // Key para abrir Spinning Cube
                            var key = _session.m_pi.findWarehouseItemByTypeid(KEY_OF_SPINNING_CUBE_TYPEID);

                            if (key == null)
                            {
                                throw new exception("[Channel::requestOpenBoxMail][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou abrir Box[TYPEID=" + (pWi._typeid) + ", ID=" + (pWi.id) + "], mas o ele nao tem a chave para abrir o spinning cube. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                    7, 0x6300107));
                            }

                            if (key.STDA_C_ITEM_QNTD < 1)
                            {
                                throw new exception("[Channel::requestOpenBoxMail][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou abrir Box[TYPEID=" + (pWi._typeid) + ", ID=" + (pWi.id) + "], mas ele nao tem quantidade suficiante[value=" + (key.STDA_C_ITEM_QNTD) + ", request=1] de chave para abrir Spinning Cube. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                    8, 0x6300108));
                            }

                            // Sortea
                            ctx_bi = sBoxSystem.getInstance().drawBox(_session, box);

                            if (ctx_bi == null)
                            {
                                throw new exception("[Channel::requestOpenBoxMail][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou abrir Box[TYPEID=" + (pWi._typeid) + ", ID=" + (pWi.id) + "], mas nao conseguiu sortear um Spinning Cube Item. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                    9, 0x6300109));
                            }

                            // Deleta Spinning Cube
                            item = new stItem();

                            item.type = 2;
                            item.id = (int)pWi.id;
                            item._typeid = box._typeid;
                            item.qntd = 1;
                            item.STDA_C_ITEM_QNTD = (short)(item.qntd * -1);

                            if (ItemManager.removeItem(item, _session) <= 0)
                            {
                                throw new exception("[Channel::requestOpenBoxMail][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou abrir Box[TYPEID=" + (pWi._typeid) + ", ID=" + (pWi.id) + "], mas nao conseguiu deletar o Spinning Cube. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                    10, 0x6300110));
                            }

                            v_item.Add(new stItem(item));

                            // [Key] tira uma chave
                            item = new stItem();

                            item.type = 2;
                            item.id = (int)key.id;
                            item._typeid = KEY_OF_SPINNING_CUBE_TYPEID;
                            item.qntd = 1;
                            item.STDA_C_ITEM_QNTD = (short)(item.qntd * -1);

                            if (ItemManager.removeItem(item, _session) <= 0)
                            {
                                throw new exception("[Channel::requestOpenBoxMail][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou abrir Box[TYPEID=" + (pWi._typeid) + ", ID=" + (pWi.id) + "], mas nao conseguiu deletar a Key[TYPEID=" + (KEY_OF_SPINNING_CUBE_TYPEID) + ", DESC=Spinning Cube]. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                    11, 0x6300111));
                            }

                            v_item.Add(new stItem(item));

                            // [Opened Spinning Cube] add um spinning cube aberto
                            if (box.opened_typeid > 0)
                            {

                                item = new stItem();

                                item.type = 2;
                                item._typeid = box.opened_typeid; //OPENNED_SPINNING_CUBE_TYPEID;
                                item.qntd = 1;
                                item.STDA_C_ITEM_QNTD = (short)item.qntd;

                                var rt = ItemManager.RetAddItem.T_INIT_VALUE;

                                if ((rt = ItemManager.addItem(item,
                                    _session, 0, 0)) < 0)
                                {
                                    throw new exception("[Channel::requestOpenBoxMail][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou abrir Box[TYPEID=" + (pWi._typeid) + ", ID=" + (pWi.id) + "], mas nao conseguiu adicionar um  Openned Spinning Cube. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                        12, 0x6300112));
                                }

                                if (rt != ItemManager.RetAddItem.T_SUCCESS_PANG_AND_EXP_AND_CP_POUCH)
                                {

                                    // UPDATE IN GAME
                                    p.init_plain(0x216);

                                    p.WriteUInt32((uint)UtilTime.GetLocalTimeAsUnix());
                                    p.WriteUInt32(1); // Count

                                    p.WriteByte(item.type);
                                    p.WriteUInt32(item._typeid);
                                    p.WriteInt32(item.id);
                                    p.WriteUInt32(item.flag_time);
                                    p.WriteBytes(item.stat.ToArray());
                                    p.WriteInt32((item.STDA_C_ITEM_TIME > 0) ? item.STDA_C_ITEM_TIME : item.STDA_C_ITEM_QNTD);
                                    p.WriteZeroByte(25);

                                    packet_func.session_send(p,
                                        _session, 1);
                                }

                            }

                            // Init Item Ganho
                            item = new stItem();

                            item.type = 2;
                            item.id = (int)-1;
                            item._typeid = ctx_bi._typeid;

                            // Check se é Mascot, para colocar por dia o tempo que é a quantidade
                            if (sIff.getInstance().getItemGroupIdentify(ctx_bi._typeid) == PangyaAPI.IFF.JP.Models.Flags.IFF_GROUP.MASCOT
                                && (mascot = sIff.getInstance().findMascot(ctx_bi._typeid)) != null
                                && mascot.Shop.flag_shop.time_shop.dia > 0
                                && mascot.Shop.flag_shop.time_shop.active)
                            {
                                item.qntd = 1;
                                item.flag_time = 4; // Flag Dias
                                item.STDA_C_ITEM_QNTD = 1; // qntd 1 por que é só 1 mascot com tempo
                                item.STDA_C_ITEM_TIME = (short)ctx_bi.qntd;
                            }
                            else
                            {
                                item.qntd = (int)ctx_bi.qntd;
                                item.STDA_C_ITEM_QNTD = (short)item.qntd;
                            }

                            // Coloca Item ganho no Mail do player
                            if (MailBoxManager.sendMessageWithItem(0,
                                _session.m_pi.uid, msg, item) <= 0)
                            {
                                throw new exception("[Channel::requestOpenBoxMail][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou abrir Box[TYPEID=" + (pWi._typeid) + ", ID=" + (pWi.id) + "], mas nao conseguiu colocar o item ganho no mailbox do player. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                    13, 0x6300113));
                            }

                            // Verifica se é um super raro para mandar broadcast que ganhou o item
                            if (ctx_bi.raridade == BOX_TYPE_RARETY.R_SUPER_RARE)
                            {
                                _smp.message_pool.getInstance().push(new message("[BoxSystem::SpinningCube][Sucess] PLAYER [UID=" + (_session.m_pi.uid) + "] Spinning Cube[TYPEID=" + (pWi._typeid) + "] ganhou super raro[TYPEID=" + (ctx_bi._typeid) + ", QNTD=" + (ctx_bi.qntd) + "] no spinning cube.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                                // DB envia comando de broadcast de Spinning Cube Win Super Rare
                                msg = "<PARAMS><BOX_TYPEID>" + (box._typeid) + "</BOX_TYPEID><NICKNAME>" + (_session.m_pi.nickname) + "</NICKNAME><TYPEID>" + (ctx_bi._typeid) + "</TYPEID><QTY>" + (ctx_bi.qntd) + "</QTY></PARAMS>";


                                byte opt = (byte)((ctx_bi._typeid == PANG_POUCH_TYPEID) ? 2 : 1);

                                snmdb.NormalManagerDB.getInstance().add(23,
                                    new CmdInsertSpinningCubeSuperRareWinBroadcast(msg, opt),
                                    SQLDBResponse, this);
                            }

                            // UPDATE Achievement ON SERVER, DB and GAME
                            AchievementSystem sys_achieve = new AchievementSystem();

                            sys_achieve.incrementCounter(0x6C400054u);

                            // Log
                            _smp.message_pool.getInstance().push(new message("[BoxSystem::SpinningCube][Sucess] PLAYER [UID=" + (_session.m_pi.uid) + "] abriu Spinning Cube[TYPEID=" + (pWi._typeid) + "] e ganhou o Item[TYPEID=" + (ctx_bi._typeid) + ", QNTD=" + (ctx_bi.qntd) + ", RARIDADE=" + ((short)ctx_bi.raridade) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            // UPDATE ON GAME
                            p.init_plain(0xA7);

                            p.WriteByte((byte)v_item.Count);

                            foreach (var el in v_item)
                            {
                                p.WriteUInt32(el._typeid);
                                p.WriteInt32(el.id);
                                p.WriteUInt16((ushort)el.stat.qntd_dep);
                            }

                            packet_func.session_send(p,
                                _session, 1);

                            // atualiza moedas em jogo
                            p.init_plain(0xAA);

                            p.WriteUInt16(0); // count;

                            p.WriteUInt64(_session.m_pi.ui.pang);
                            p.WriteUInt64(_session.m_pi.cookie);

                            packet_func.session_send(p,
                                _session, 1);

                            // Resposta do Abrir Cube
                            p.init_plain(0x19D);

                            p.WriteUInt32(0); // OK

                            p.WriteUInt32(box._typeid);
                            p.WriteUInt32(ctx_bi._typeid);
                            p.WriteInt32(ctx_bi.qntd);

                            packet_func.session_send(p,
                                _session, 1);

                            // UPDATE Achievement ON SERVER, DB and Game
                            sys_achieve.finish_and_update(_session);

                            break;
                        }
                    case PAPEL_BOX_TYPEID: // Esse add as 30 Key que ganha quando abre a papel box, no pacoteAA
                        {

                            // Sortea
                            ctx_bi = sBoxSystem.getInstance().drawBox(_session, box);

                            if (ctx_bi == null)
                            {
                                throw new exception("[Channel::requestOpenBoxMail][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou abrir Box[TYPEID=" + (pWi._typeid) + ", ID=" + (pWi.id) + "], mas nao conseguiu sortear um Papel Box Item. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                    9, 0x6300109));
                            }

                            // Delete Papel Box
                            item = new stItem();

                            item.type = 2;
                            item.id = (int)pWi.id;
                            item._typeid = box._typeid;
                            item.qntd = 1;
                            item.STDA_C_ITEM_QNTD = (short)(item.qntd * -1);

                            if (ItemManager.removeItem(item, _session) <= 0)
                            {
                                throw new exception("[Channel::requestOpenBoxMail][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou abrir Box[TYPEID=" + (pWi._typeid) + ", ID=" + (pWi.id) + "], mas nao conseguiu deletar Papel Box. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                    10, 0x6300110));
                            }

                            v_item.Add(new stItem(item)); // usando um construtor de cópia, que você deve implementar

                            // Add 30 Key
                            stItem key = new stItem();

                            key.clear();

                            key.type = 2;
                            key.id = -1;
                            key._typeid = KEY_OF_SPINNING_CUBE_TYPEID;
                            key.qntd = 30;
                            key.STDA_C_ITEM_QNTD = (short)key.qntd;

                            var rt = ItemManager.RetAddItem.T_INIT_VALUE;

                            if ((rt = ItemManager.addItem(key,
                                _session, 0, 0)) < 0)
                            {
                                throw new exception("[Channel::requestOpenBoxMail][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou abrir Box[TYPEID=" + (pWi._typeid) + ", ID=" + (pWi.id) + "], nao conseguiu adicionar Key[TYPEID=" + (KEY_OF_SPINNING_CUBE_TYPEID) + ", DESC=Spinning Cube]", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                    14, 0x6300114));
                            }

                            // [Opened Box] add um Papel Box aberto se tiver
                            if (box.opened_typeid > 0)
                            {

                                item = new stItem();

                                item.type = 2;
                                item._typeid = box.opened_typeid;
                                item.qntd = 1;
                                item.STDA_C_ITEM_QNTD = (short)item.qntd;

                                rt = ItemManager.RetAddItem.T_INIT_VALUE;

                                if ((rt = ItemManager.addItem(item,
                                    _session, 0, 0)) < 0)
                                {
                                    throw new exception("[Channel::requestOpenBoxMail][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou abrir Box[TYPEID=" + (pWi._typeid) + ", ID=" + (pWi.id) + "], mas nao conseguiu adicionar um  Openned Papel Box. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                        12, 0x6300112));
                                }

                                if (rt != ItemManager.RetAddItem.T_SUCCESS_PANG_AND_EXP_AND_CP_POUCH)
                                {

                                    // UPDATE IN GAME
                                    p.init_plain(0x216);

                                    p.WriteUInt32((uint)UtilTime.GetLocalTimeAsUnix());
                                    p.WriteUInt32(1); // Count

                                    p.WriteByte(item.type);
                                    p.WriteUInt32(item._typeid);
                                    p.WriteInt32(item.id);
                                    p.WriteUInt32(item.flag_time);
                                    p.WriteBytes(item.stat.ToArray());
                                    p.WriteInt32((item.STDA_C_ITEM_TIME > 0) ? item.STDA_C_ITEM_TIME : item.STDA_C_ITEM_QNTD);
                                    p.WriteZeroByte(25);

                                    packet_func.session_send(p,
                                        _session, 1);
                                }

                            }

                            // Init Item Ganho
                            item = new stItem();

                            item.type = 2;
                            item.id = (int)-1;
                            item._typeid = ctx_bi._typeid;

                            // Check se é Mascot, para colocar por dia o tempo que é a quantidade
                            if (sIff.getInstance().getItemGroupIdentify(ctx_bi._typeid) == PangyaAPI.IFF.JP.Models.Flags.IFF_GROUP.MASCOT
                                && (mascot = sIff.getInstance().findMascot(ctx_bi._typeid)) != null
                                && mascot.Shop.flag_shop.time_shop.dia > 0
                                && mascot.Shop.flag_shop.time_shop.active)
                            {
                                item.qntd = 1;
                                item.flag_time = 4; // Flag Dias
                                item.STDA_C_ITEM_QNTD = 1; // qntd 1 por que é só 1 mascot com tempo
                                item.STDA_C_ITEM_TIME = (short)ctx_bi.qntd;
                            }
                            else
                            {
                                item.qntd = (int)ctx_bi.qntd;
                                item.STDA_C_ITEM_QNTD = (short)item.qntd;
                            }

                            // Coloca Item ganho no Mail do player
                            if (MailBoxManager.sendMessageWithItem(0,
                                _session.m_pi.uid, msg, item) <= 0)
                            {
                                throw new exception("[Channel::requestOpenBoxMail][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou abrir Box[TYPEID=" + (pWi._typeid) + ", ID=" + (pWi.id) + "], mas nao conseguiu colocar o item ganho no mailbox do player. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                    13, 0x6300113));
                            }

                            // Log
                            _smp.message_pool.getInstance().push(new message("[BoxSystem::PapelBox][Sucess] PLAYER [UID=" + (_session.m_pi.uid) + "] abriu Papel Box[TYPEID=" + (pWi._typeid) + "] e ganhou o Item[TYPEID=" + (ctx_bi._typeid) + ", QNTD=" + (ctx_bi.qntd) + ", RARIDADE=" + ((short)ctx_bi.raridade) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            // UPDATE ON GAME
                            p.init_plain(0xA7);

                            p.WriteByte((byte)v_item.Count);

                            foreach (var el in v_item)
                            {
                                p.WriteUInt32(el._typeid);
                                p.WriteInt32(el.id);
                                p.WriteUInt16((ushort)el.stat.qntd_dep);
                            }

                            packet_func.session_send(p,
                                _session, 1);

                            // atualiza moedas em jogo e Key que ganha 30 quando abre papel box
                            p.init_plain(0xAA);

                            p.WriteUInt16(1); // count;

                            p.WriteUInt32(key._typeid);
                            p.WriteInt32(key.id);
                            p.WriteUInt16(key.STDA_C_ITEM_TIME);
                            p.WriteByte(key.flag_time);
                            p.WriteUInt16((ushort)key.stat.qntd_dep);
                            if (key.date != null && key.date.active.IsTrue())
                                p.WriteTime(key.date.date.sysDate[1].ConvertTime());
                            else
                                p.WriteZero(16);
                            p.WriteStr(key.ucc.IDX, 9);

                            p.WriteUInt64(_session.m_pi.ui.pang);
                            p.WriteUInt64(_session.m_pi.cookie);

                            packet_func.session_send(p,
                                _session, 1);

                            // Resposta do Abrir Papel Box
                            p.init_plain(0x19D);

                            p.WriteUInt32(0); // OK

                            p.WriteUInt32(box._typeid);
                            p.WriteUInt32(ctx_bi._typeid);
                            p.WriteInt32(ctx_bi.qntd);

                            packet_func.session_send(p,
                                _session, 1);

                            break;
                        }
                    default: // Todas as outras box
                        {
                            // Sortea
                            ctx_bi = sBoxSystem.getInstance().drawBox(_session, box);

                            if (ctx_bi == null)
                            {
                                throw new exception("[Channel::requestOpenBoxMail][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou abrir Box[TYPEID=" + (pWi._typeid) + ", ID=" + (pWi.id) + "], mas nao conseguiu sortear um Box Item. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                    9, 0x6300109));
                            }

                            // Delete Box
                            item = new stItem();

                            item.type = 2;
                            item.id = (int)pWi.id;
                            item._typeid = box._typeid;
                            item.qntd = 1;
                            item.STDA_C_ITEM_QNTD = (short)(item.qntd * -1);

                            if (ItemManager.removeItem(item, _session) <= 0)
                            {
                                throw new exception("[Channel::requestOpenBoxMail][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou abrir Box[TYPEID=" + (pWi._typeid) + ", ID=" + (pWi.id) + "], mas nao conseguiu deletar Box. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                    10, 0x6300110));
                            }

                            v_item.Add(new stItem(item));
                            // [Opened Box] add um Box aberto
                            if (box.opened_typeid > 0)
                            {

                                item = new stItem();

                                item.type = 2;
                                item._typeid = box.opened_typeid;
                                item.qntd = 1;
                                item.STDA_C_ITEM_QNTD = (short)item.qntd;

                                var rt = ItemManager.RetAddItem.T_INIT_VALUE;

                                if ((rt = ItemManager.addItem(item,
                                    _session, 0, 0)) < 0)
                                {
                                    throw new exception("[Channel::requestOpenBoxMail][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou abrir Box[TYPEID=" + (pWi._typeid) + ", ID=" + (pWi.id) + "], mas nao conseguiu adicionar um  Openned Box. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                        12, 0x6300112));
                                }

                                if (rt != ItemManager.RetAddItem.T_SUCCESS_PANG_AND_EXP_AND_CP_POUCH)
                                {

                                    // UPDATE IN GAME
                                    p.init_plain(0x216);

                                    p.WriteUInt32((uint)UtilTime.GetLocalTimeAsUnix());
                                    p.WriteUInt32(1); // Count

                                    p.WriteByte(item.type);
                                    p.WriteUInt32(item._typeid);
                                    p.WriteInt32(item.id);
                                    p.WriteUInt32(item.flag_time);
                                    p.WriteBytes(item.stat.ToArray());
                                    p.WriteInt32((item.STDA_C_ITEM_TIME > 0) ? item.STDA_C_ITEM_TIME : item.STDA_C_ITEM_QNTD);
                                    p.WriteZeroByte(25);

                                    packet_func.session_send(p,
                                        _session, 1);
                                }

                            }

                            // Init Item Ganho
                            item = new stItem();

                            item.type = 2;
                            item.id = (int)-1;
                            item._typeid = ctx_bi._typeid;

                            // Check se é Mascot, para colocar por dia o tempo que é a quantidade
                            if (sIff.getInstance().getItemGroupIdentify(ctx_bi._typeid) == PangyaAPI.IFF.JP.Models.Flags.IFF_GROUP.MASCOT
                                && (mascot = sIff.getInstance().findMascot(ctx_bi._typeid)) != null
                                && mascot.Shop.flag_shop.time_shop.dia > 0
                                && mascot.Shop.flag_shop.time_shop.active)
                            {
                                item.qntd = 1;
                                item.flag_time = 4; // Flag Dias
                                item.STDA_C_ITEM_QNTD = 1; // qntd 1 por que é só 1 mascot com tempo
                                item.STDA_C_ITEM_TIME = (short)ctx_bi.qntd;
                            }
                            else
                            {
                                item.qntd = (int)ctx_bi.qntd;
                                item.STDA_C_ITEM_QNTD = (short)item.qntd;
                            }

                            // Coloca Item ganho no Mail do player
                            if (MailBoxManager.sendMessageWithItem(0,
                                _session.m_pi.uid, msg, item) <= 0)
                            {
                                throw new exception("[Channel::requestOpenBoxMail][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou abrir Box[TYPEID=" + (pWi._typeid) + ", ID=" + (pWi.id) + "], mas nao conseguiu colocar o item ganho no mailbox do player. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                    13, 0x6300113));
                            }

                            // Log
                            _smp.message_pool.getInstance().push(new message("[BoxSystem::BoxMail][Sucess] PLAYER [UID=" + (_session.m_pi.uid) + "] abriu Box[TYPEID=" + (pWi._typeid) + "] e ganhou o Item[TYPEID=" + (ctx_bi._typeid) + ", QNTD=" + (ctx_bi.qntd) + ", RARIDADE=" + ((short)ctx_bi.raridade) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            // UPDATE ON GAME
                            p.init_plain(0xA7);

                            p.WriteByte((byte)v_item.Count);

                            foreach (var el in v_item)
                            {
                                p.WriteUInt32(el._typeid);
                                p.WriteInt32(el.id);
                                p.WriteUInt16((ushort)el.stat.qntd_dep);
                            }

                            packet_func.session_send(p,
                                _session, 1);

                            // atualiza moedas em jogo
                            p.init_plain(0xAA);

                            p.WriteUInt16(0); // count;

                            p.WriteUInt64(_session.m_pi.ui.pang);
                            p.WriteUInt64(_session.m_pi.cookie);

                            packet_func.session_send(p,
                                _session, 1);

                            // Resposta do Abrir Box Mail
                            p.init_plain(0x19D);

                            p.WriteUInt32(0); // OK

                            p.WriteUInt32(box._typeid);
                            p.WriteUInt32(ctx_bi._typeid);
                            p.WriteInt32(ctx_bi.qntd);

                            packet_func.session_send(p,
                                _session, 1);

                            break;
                        } // END DEFAULT CASE
                } // END SWITCH

                // DB Register Rare Win Log
                if (ctx_bi != null && ctx_bi.raridade > 0)
                {
                    snmdb.NormalManagerDB.getInstance().add(22,
                        new CmdInsertBoxRareWinLog(_session.m_pi.uid,
                            box._typeid, ctx_bi),
                        SQLDBResponse, this);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestOpenBoxMail][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x19D);

                p.WriteUInt32((ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) == STDA_ERROR_TYPE.CHANNEL) ? ExceptionError.STDA_SYSTEM_ERROR_DECODE(e.getCodeError()) : 0x6300100);

                packet_func.session_send(p,
                    _session, 1);
            }
        }

        public void requestOpenBoxMyRoom(Player _session, packet _packet)
        {
            //

            PangyaBinaryWriter p = new PangyaBinaryWriter();

            try
            {





                if (!sBoxSystem.getInstance().isLoad())
                {
                    sBoxSystem.getInstance().load();
                }

                uint box_typeid = _packet.ReadUInt32();

                if (box_typeid == 0)
                {
                    throw new exception("[Channel::requestOpenBoxMyRoom][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou abrir Box[TYPEID=" + (box_typeid) + "], mas o typeid é invalido(zero). Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        1, 0x6300201));
                }

                var pWi = _session.m_pi.findWarehouseItemByTypeid(box_typeid);

                if (pWi == null)
                {
                    throw new exception("[Channel::requestOpenBoxMyRoom][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou abrir Box[TYPEID=" + (box_typeid) + "], mas ele nao tem essa Box. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        2, 0x6300202));
                }

                if (pWi.STDA_C_ITEM_QNTD < 1)
                {
                    throw new exception("[Channel::requestOpenBoxMyRoom][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou abrir Box[TYPEID=" + (pWi._typeid) + ", ID=" + (pWi.id) + "], mas ele nao tem quantidade suficiente da Box[value=" + (pWi.STDA_C_ITEM_QNTD) + ", request=1]", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        3, 0x6300203));
                }

                if (sIff.getInstance().getItemGroupIdentify(pWi._typeid) != IFF_GROUP.ITEM)
                {
                    throw new exception("[Channel::requestOpenBoxMyRoom][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou abrir Box[TYPEID=" + (pWi._typeid) + ", ID=" + (pWi.id) + "], mas nao é uma Box valida. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        4, 0x6300204));
                }

                var item_iff = sIff.getInstance().findItem(pWi._typeid);

                if (item_iff == null)
                {
                    throw new exception("[Channel::requestOpenBoxMyRoom][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou abrir Box[TYPEID=" + (pWi._typeid) + ", ID=" + (pWi.id) + "], mas nao tem essa Box no IFF_STRUCT do Server. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        5, 0x6300205));
                }

                var box = sBoxSystem.getInstance().findBox(pWi._typeid);

                if (box == null)
                {
                    throw new exception("[Channel::requestOpenBoxMyRoom][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou abrir Box[TYPEID=" + (pWi._typeid) + ", ID=" + (pWi.id) + "], mas nao tem essa Box no Box System do Server. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        6, 0x6300206));
                }

                List<stItem> v_item = new List<stItem>();
                stItem item = new stItem();
                stItem stBox = new stItem();

                ctx_box_item ctx_bi = null;

                // ----------- Sortea ---------------
                ctx_bi = sBoxSystem.getInstance().drawBox(_session, box);

                if (ctx_bi == null)
                {
                    throw new exception("[Channel::requestOpenBoxMail][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou abrir Box[TYPEID=" + (pWi._typeid) + ", ID=" + (pWi.id) + "], mas nao conseguiu sortear um Box Item. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        9, 0x6300209));
                }

                // Init Item Ganho
                BuyItem bi = new BuyItem();
                Mascot mascot = null;

                item = new stItem();

                bi.id = -1;
                bi._typeid = ctx_bi._typeid;

                // Check se é Mascot, para colocar por dia o tempo que é a quantidade
                if (sIff.getInstance().getItemGroupIdentify(ctx_bi._typeid) == PangyaAPI.IFF.JP.Models.Flags.IFF_GROUP.MASCOT
                    && (mascot = sIff.getInstance().findMascot(ctx_bi._typeid)) != null
                    && mascot.Shop.flag_shop.time_shop.dia > 0
                    && mascot.Shop.flag_shop.time_shop.active)
                {
                    bi.qntd = 1;
                    bi.time = (short)ctx_bi.qntd;
                }
                else
                {
                    bi.qntd = (uint)ctx_bi.qntd;
                }

                ItemManager.initItemFromBuyItem(_session.m_pi,
                    item, bi, false, 0, 0, 1);

                if (item._typeid == 0)
                {
                    throw new exception("[Channel::requestOpenBoxMail][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou abrir Box[TYPEID=" + (pWi._typeid) + ", ID=" + (pWi.id) + "], mas nao conseguiu inicializar o Item[TYPEID=" + (bi._typeid) + "]", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        11, 0x6300211));
                }

                // Verifica se já possui o item, o caddie item verifica se tem o caddie para depois verificar se tem o caddie item
                if ((sIff.getInstance().IsCanOverlapped(item._typeid) && sIff.getInstance().getItemGroupIdentify(item._typeid) != IFF_GROUP.CAD_ITEM) || !_session.m_pi.ownerItem(item._typeid))
                {
                    if (ItemManager.isSetItem(item._typeid))
                    {
                        var v_stItem = ItemManager.getItemOfSetItem(_session,
                            item._typeid, false, 1);

                        if (!v_stItem.empty())
                        {
                            // Já verificou lá em cima se tem os item so set, então não precisa mais verificar aqui
                            // Só add eles ao List de venda
                            // Verifica se pode ter mais de 1 item e se não ver se não tem o item
                            foreach (var el in v_stItem)
                            {
                                if ((sIff.getInstance().IsCanOverlapped(el._typeid) && sIff.getInstance().getItemGroupIdentify(el._typeid) != IFF_GROUP.CAD_ITEM) || !_session.m_pi.ownerItem(el._typeid))
                                {
                                    v_item.Add(new stItem(el));
                                }
                            }
                        }
                        else
                        {
                            throw new exception("[Channel::requestOpenBoxMail][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou abrir Box[TYPEID=" + (pWi._typeid) + ", ID=" + (pWi.id) + "], mas SetItem que ele ganhou da box, nao tem Item[TYPEID=" + (bi._typeid) + "]. Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                12, 0x6300212));
                        }
                    }
                    else
                    {
                        v_item.Add(new stItem(item));
                    }

                }
                else if (sIff.getInstance().getItemGroupIdentify(item._typeid) == PangyaAPI.IFF.JP.Models.Flags.IFF_GROUP.CAD_ITEM)
                {
                    throw new exception("[Channel::requestOpenBoxMail][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou abrir Box[TYPEID=" + (pWi._typeid) + ", ID=" + (pWi.id) + "], mas o CaddieItem que ele ganhou, nao tem o caddie, Item[TYPEID=" + (bi._typeid) + "]. Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        13, 0x6300213));
                }
                else
                {
                    throw new exception("[Channel::requestOpenBoxMail][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou abrir Box[TYPEID=" + (pWi._typeid) + ", ID=" + (pWi.id) + "], mas ele ja tem o Item[TYPEID=" + (bi._typeid) + "]. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        14, 0x6300214));
                }

                // UPDATE ON SERVER AND DB

                // Delete Box
                stBox.clear();

                stBox.type = 2;
                stBox.id = pWi.id;
                stBox._typeid = box._typeid;
                stBox.qntd = 1;
                stBox.STDA_C_ITEM_QNTD = (short)(stBox.qntd * -1);

                if (ItemManager.removeItem(stBox, _session) <= 0)
                {
                    throw new exception("[Channel::requestOpenBoxMail][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou abrir Box[TYPEID=" + (pWi._typeid) + ", ID=" + (pWi.id) + "], mas nao conseguiu deletar Box. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        10, 0x6300210));
                }

                string str = "";

                // Coloca Item ganho no My Room do player
                var rai = ItemManager.addItem(v_item,
                    _session.getUID(), 0, 0);

                if (rai.fails.Count > 0 && rai.type != ItemManager.RetAddItem.T_SUCCESS_PANG_AND_EXP_AND_CP_POUCH)
                {

                    for (var i = 0; i < v_item.Count; ++i)
                    {
                        if (i == 0)
                        {
                            str += "[TYPEID=" + (v_item[i]._typeid) + ", ID=" + (v_item[i].id) + ", QNTD=" + ((v_item[i].qntd > 0xFFu) ? v_item[i].qntd : v_item[i].STDA_C_ITEM_QNTD) + (v_item[i].STDA_C_ITEM_TIME > 0 ? ", TEMPO=" + (v_item[i].STDA_C_ITEM_TIME) : "") + "]";
                        }
                        else
                        {
                            str += ", [TYPEID=" + (v_item[i]._typeid) + ", ID=" + (v_item[i].id) + ", QNTD=" + ((v_item[i].qntd > 0xFFu) ? v_item[i].qntd : v_item[i].STDA_C_ITEM_QNTD) + (v_item[i].STDA_C_ITEM_TIME > 0 ? ", TEMPO=" + (v_item[i].STDA_C_ITEM_TIME) : "") + "]";
                        }
                    }

                    throw new exception("[Channel::requestOpenBoxMail][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou abrir Box[TYPEID=" + (pWi._typeid) + ", ID=" + (pWi.id) + "], mas ele nao conseguiu adicionar os item(ns){" + str + "}. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        15, 0x6300215));
                }
                else
                {
                    // Init Item Add Log
                    for (var i = 0; i < v_item.Count; ++i)
                    {
                        if (i == 0)
                        {
                            str += "[TYPEID=" + (v_item[i]._typeid) + ", ID=" + (v_item[i].id) + ", QNTD=" + ((v_item[i].qntd > 0xFFu) ? v_item[i].qntd : v_item[i].STDA_C_ITEM_QNTD) + (v_item[i].STDA_C_ITEM_TIME > 0 ? ", TEMPO=" + (v_item[i].STDA_C_ITEM_TIME) : "") + "]";
                        }
                        else
                        {
                            str += ", [TYPEID=" + (v_item[i]._typeid) + ", ID=" + (v_item[i].id) + ", QNTD=" + ((v_item[i].qntd > 0xFFu) ? v_item[i].qntd : v_item[i].STDA_C_ITEM_QNTD) + (v_item[i].STDA_C_ITEM_TIME > 0 ? ", TEMPO=" + (v_item[i].STDA_C_ITEM_TIME) : "") + "]";
                        }
                    }
                }

                // DB Register Rare Win Log
                if (ctx_bi != null && ctx_bi.raridade > 0)
                {
                    snmdb.NormalManagerDB.getInstance().add(22,
                        new CmdInsertBoxRareWinLog(_session.m_pi.uid,
                            box._typeid, ctx_bi),
                        SQLDBResponse, this);
                }

                // UPDATE ON GAME

                // atualiza moedas e item(ns) em jogo
                foreach (var el in v_item)
                {
                    p.init_plain(0xAA);

                    p.WriteUInt16(1); // count;

                    p.WriteUInt32(el._typeid);
                    p.WriteInt32(el.id);
                    p.WriteUInt16(el.STDA_C_ITEM_TIME);
                    p.WriteByte(el.flag_time);
                    p.WriteUInt16((ushort)el.stat.qntd_dep);
                    if (el.date != null && el.date.active.IsTrue())
                        p.WriteTime(el.date.date.sysDate[1].ConvertTime());
                    else
                        p.WriteZero(16);
                    p.WriteStr(el.ucc.IDX, 9);

                    p.WriteUInt64(_session.m_pi.ui.pang);
                    p.WriteUInt64(_session.m_pi.cookie);

                    packet_func.session_send(p,
                        _session, 1);
                }

                // Resposta do Abrir Box My Room
                p.init_plain(0x129);

                p.WriteByte(0); // OK

                p.WriteUInt32(box._typeid);
                p.WriteInt32(stBox.stat.qntd_dep);

                p.WriteUInt32((uint)v_item.Count); // Count

                foreach (var el in v_item)
                {
                    p.WriteUInt32(el._typeid);
                    p.WriteInt32(el.id);
                    p.WriteInt32((el.STDA_C_ITEM_TIME > 0) ? el.STDA_C_ITEM_TIME : el.STDA_C_ITEM_QNTD);
                    p.WriteZeroByte(8); // Não sei o que é esses 8 Bytes ainda
                }

                packet_func.session_send(p,
                    _session, 1);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestOpenBoxMyRoom][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x129);

                p.WriteByte(1); // Error

                p.WriteZeroByte(12); // Box Typeid, Box Qntd e count de itens

                packet_func.session_send(p,
                    _session, 1);
            }
        }

        public void requestPlayMemorial(Player _session, packet _packet)
        {
            //

            PangyaBinaryWriter p = new PangyaBinaryWriter();

            try
            {
                if (_session.m_pi.block_flag.m_flag.memorial_shop)
                {
                    throw new exception("[Channel::requestPlayerMemorial][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou jogar no Memorial Shop, mas ele nao pode. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        6, 0x790001));
                }

                if (!sMemorialSystem.getInstance().isLoad())
                {
                    sMemorialSystem.getInstance().load();
                }

                uint coin_typeid = _packet.ReadUInt32();

                if (coin_typeid == 0)
                {
                    throw new exception("[Channel::requestPlayMemorial][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou jogar Memorial com a coin[TYPEID=" + (coin_typeid) + "], mas o coin_typeid is invalid(zero). Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        1, 0x6300301));
                }

                if (sIff.getInstance().getItemGroupIdentify(coin_typeid) != IFF_GROUP.ITEM)
                {
                    throw new exception("[Channel::requestPlayMemorial][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou jogar Memorial com a coin[TYPEID=" + (coin_typeid) + "], mas a coin is not Item Valid. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        2, 0x6300302));
                }

                var pWi = _session.m_pi.findWarehouseItemByTypeid(coin_typeid);

                if (pWi == null)
                {
                    throw new exception("[Channel::requestPlayMemorial][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou jogar Memorial com a coin[TYPEID=" + (coin_typeid) + "], mas o ele nao possui a coin. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        3, 0x6300303));
                }

                var coin = sIff.getInstance().findItem(pWi._typeid);

                if (coin == null || !coin.Active)
                {
                    throw new exception("[Channel::requestPlayMemorial][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou jogar Memorial com a coin[TYPEID=" + (coin_typeid) + "], mas nao tem a coin na IFF_STRUCT do Server. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        4, 0x6300304));
                }

                // Achievement System
                AchievementSystem sys_achieve = new AchievementSystem();

                // Memorial System
                var c = sMemorialSystem.getInstance().findCoin(coin.ID);

                if (c == null)
                {
                    throw new exception("[Channel::requestPlayMemorial][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou jogar Memorial com a coin[TYPEID=" + (coin_typeid) + "], mas nao tem essa coin no Memorial System do Server. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        5, 0x6300305));
                }

                // Achievement add + 1 ao contador de Play Coin no memorial shop
                if (c.tipo == MEMORIAL_COIN_TYPE.MCT_NORMAL)
                {
                    sys_achieve.incrementCounter(0x6C4000B2u);
                }
                else if (c.tipo == MEMORIAL_COIN_TYPE.MCT_SPECIAL)
                {
                    sys_achieve.incrementCounter(0x6C4000B3u);
                }

                var win_item = sMemorialSystem.getInstance().drawCoin(_session, c);

                if (win_item == null || win_item.Count == 0)
                {
                    throw new exception("[Channel::requestPlayMemorial][Error] win_item is null or empty. UID: " + _session.m_pi.uid,
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 6, 0x6300306));
                }

                List<stItem> v_item = new List<stItem>();
                stItem item = new stItem();

                // Init Item Ganho
                BuyItem bi = new BuyItem();
                Mascot mascot = null;

                foreach (var el in win_item)
                {
                    bi = new BuyItem();
                    item = new stItem();

                    bi.id = -1;
                    bi._typeid = el._typeid;

                    // Check se é Mascot, para colocar por dia o tempo que é a quantidade
                    if (sIff.getInstance().getItemGroupIdentify(el._typeid) == PangyaAPI.IFF.JP.Models.Flags.IFF_GROUP.MASCOT
                        && (mascot = sIff.getInstance().findMascot(el._typeid)) != null
                        && mascot.Shop.flag_shop.time_shop.dia > 0
                        && mascot.Shop.flag_shop.time_shop.active)
                    { // é Mascot por Tempo
                        bi.qntd = 1;
                        bi.time = (short)(ushort)el.qntd;
                    }
                    else
                    {
                        bi.qntd = el.qntd;
                    }

                    ItemManager.initItemFromBuyItem(_session.m_pi,
                        item, bi, false, 0, 0, 1);

                    if (item._typeid == 0)
                    {
                        throw new exception("[Channel::requestPlayMemorial][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou jogar Memorial com a coin[TYPEID=" + (coin_typeid) + "], mas nao conseguiu inicializar o Item[TYPEID=" + (bi._typeid) + "]", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                            7, 0x6300307));
                    }

                    // Verifica se já possui o item, o caddie item verifica se tem o caddie para depois verificar se tem o caddie item
                    if ((sIff.getInstance().IsCanOverlapped(item._typeid) && sIff.getInstance().getItemGroupIdentify(item._typeid) != IFF_GROUP.CAD_ITEM) || !_session.m_pi.ownerItem(item._typeid))
                    {
                        if (ItemManager.isSetItem(item._typeid))
                        {
                            var v_stItem = ItemManager.getItemOfSetItem(_session,
                                item._typeid, false, 1);

                            if (!v_stItem.empty())
                            {
                                // Já verificou lá em cima se tem os item so set, então não precisa mais verificar aqui
                                // Só add eles ao List de venda
                                // Verifica se pode ter mais de 1 item e se não ver se não tem o item
                                foreach (var _el in v_stItem)
                                {
                                    if ((sIff.getInstance().IsCanOverlapped(_el._typeid) && sIff.getInstance().getItemGroupIdentify(_el._typeid) != IFF_GROUP.CAD_ITEM) || !_session.m_pi.ownerItem(_el._typeid))
                                    {
                                        v_item.Add(new stItem(_el));
                                    }
                                }
                            }
                            else
                            {
                                throw new exception("[Channel::requestPlayMemorial][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou jogar Memorial com a coin[TYPEID=" + (coin_typeid) + "], mas SetItem que ele ganhou no Memorial Shop, nao tem Item[TYPEID=" + (bi._typeid) + "]. Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                    8, 0x6300308));
                            }
                        }
                        else
                        {
                            v_item.Add(new stItem(item));
                        }

                    }
                    else if (sIff.getInstance().getItemGroupIdentify(item._typeid) == PangyaAPI.IFF.JP.Models.Flags.IFF_GROUP.CAD_ITEM)
                    {
                        throw new exception("[Channel::requestPlayMemorial][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou jogar Memorial com a coin[TYPEID=" + (coin_typeid) + "], mas o CaddieItem que ele ganhou, nao tem o caddie, Item[TYPEID=" + (bi._typeid) + "]. Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                            9, 0x6300309));
                    }
                    else
                    {
                        throw new exception("[Channel::requestPlayMemorial][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou jogar Memorial com a coin[TYPEID=" + (coin_typeid) + "], mas ele ja tem o Item[TYPEID=" + (bi._typeid) + "]. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                            10, 0x6300310));
                    }

                    // Achievement add +1 ao contador de item raro que ganhou
                    if (el.tipo >= 0 && el.tipo < 3)
                    {
                        sys_achieve.incrementCounter(0x6C4000B5u);
                    }
                    else if (el.tipo >= 3)
                    {
                        sys_achieve.incrementCounter(0x6C4000B4u);
                    }
                }

                // UPDATE ON SERVER AND DB

                // Delete Coin
                item = new stItem();

                item.type = 2;
                item.id = (int)pWi.id;
                item._typeid = c._typeid;
                item.qntd = 1;
                item.STDA_C_ITEM_QNTD = (short)(item.qntd * -1);

                if (ItemManager.removeItem(item, _session) <= 0)
                {
                    throw new exception("[Channel::requestPlayMemorial][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou jogar Memorial com a coin[TYPEID=" + (coin_typeid) + "], mas nao conseguiu deletar Coin. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        11, 0x6300311));
                }

                // Add ao List depois que add os itens ganho no memorial

                string str = "";

                // Coloca Item ganho no My Room do player
                var rai = ItemManager.addItem(v_item,
                    _session.getUID(), 0, 0);

                if (rai.fails.Count > 0 && rai.type != ItemManager.RetAddItem.T_SUCCESS_PANG_AND_EXP_AND_CP_POUCH)
                {

                    for (var i = 0; i < v_item.Count; ++i)
                    {
                        if (i == 0)
                        {
                            str += "[TYPEID=" + (v_item[i]._typeid) + ", ID=" + (v_item[i].id) + ", QNTD=" + ((v_item[i].qntd > 0xFFu) ? v_item[i].qntd : v_item[i].STDA_C_ITEM_QNTD) + (v_item[i].STDA_C_ITEM_TIME > 0 ? ", TEMPO=" + (v_item[i].STDA_C_ITEM_TIME) : "") + "]";
                        }
                        else
                        {
                            str += ", [TYPEID=" + (v_item[i]._typeid) + ", ID=" + (v_item[i].id) + ", QNTD=" + ((v_item[i].qntd > 0xFFu) ? v_item[i].qntd : v_item[i].STDA_C_ITEM_QNTD) + (v_item[i].STDA_C_ITEM_TIME > 0 ? ", TEMPO=" + (v_item[i].STDA_C_ITEM_TIME) : "") + "]";
                        }
                    }

                    throw new exception("[Channel::requestPlayMemorial][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou jogar Memorial com a coin[TYPEID=" + (coin_typeid) + "], mas ele nao conseguiu adicionar os item(ns){" + str + "}. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        12, 0x6300312));
                }
                else
                {
                    // Init Item Add Log
                    for (var i = 0; i < v_item.Count; ++i)
                    {
                        if (i == 0)
                        {
                            str += "[TYPEID=" + (v_item[i]._typeid) + ", ID=" + (v_item[i].id) + ", QNTD=" + ((v_item[i].qntd > 0xFFu) ? v_item[i].qntd : v_item[i].STDA_C_ITEM_QNTD) + (v_item[i].STDA_C_ITEM_TIME > 0 ? ", TEMPO=" + (v_item[i].STDA_C_ITEM_TIME) : "") + "]";
                        }
                        else
                        {
                            str += $", [TYPEID=" + (v_item[i]._typeid) + ", ID=" + (v_item[i].id) + ", QNTD=" + ((v_item[i].qntd > 0xFFu) ? v_item[i].qntd : v_item[i].STDA_C_ITEM_QNTD) + (v_item[i].STDA_C_ITEM_TIME > 0 ? ", TEMPO=" + (v_item[i].STDA_C_ITEM_TIME) : "") + "]";
                        }
                    }
                }

                // Add a Coin agora no Vector de itens
                v_item.Add(new stItem(item));

                // DB Register Rare Win Log
                if (!win_item.empty()
                    && win_item[0].tipo > 0
                    && win_item.Count == 1)
                {
                    snmdb.NormalManagerDB.getInstance().add(24,
                        new CmdInsertMemorialRareWinLog(_session.m_pi.uid,
                            c._typeid, win_item.begin()),
                        SQLDBResponse, this);
                }

                // UPDATE ON GAME
                p.init_plain(0x216);

                p.WriteUInt32((uint)UtilTime.GetSystemTimeAsUnix());
                p.WriteUInt32((uint)v_item.Count); // Count;

                foreach (var el in v_item)
                {
                    p.WriteByte(el.type);
                    p.WriteUInt32(el._typeid);
                    p.WriteInt32(el.id);
                    p.WriteUInt32(el.flag_time);
                    p.WriteBytes(el.stat.ToArray());
                    p.WriteInt32((el.STDA_C_ITEM_TIME > 0) ? el.STDA_C_ITEM_TIME : el.STDA_C_ITEM_QNTD);
                    p.WriteZeroByte(25);
                }

                packet_func.session_send(p,
                    _session, 1);

                // Resposta ao Play Memorial
                p.init_plain(0x264);

                p.WriteUInt32(0); // OK

                p.WriteUInt32((uint)win_item.Count); // Count

                foreach (var el in win_item)
                {
                    p.WriteInt32(el.tipo);
                    p.WriteUInt32(el._typeid);
                    p.WriteUInt32(el.qntd);
                }

                packet_func.session_send(p,
                    _session, 1);

                // Update Achievement ON SERVER, DB and GAME
                sys_achieve.finish_and_update(_session);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestPlayMemorial][ErrorSystem]" + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x264);

                p.WriteUInt32((ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) == STDA_ERROR_TYPE.CHANNEL) ? ExceptionError.STDA_SYSTEM_ERROR_DECODE(e.getCodeError()) : 0x6300300);

                packet_func.session_send(p,
                    _session, 1);
            }
        }

        public void requestOpenCardPack(Player _session, packet _packet)
        {
            //

            PangyaBinaryWriter p = new PangyaBinaryWriter();

            try
            {

                AchievementSystem sys_achieve = new AchievementSystem();

                List<stItem> v_item_add = new List<stItem>();
                List<stItem> v_item = new List<stItem>();

                uint _typeid = _packet.ReadUInt32();
                int id = _packet.ReadInt32();
                var ids = "";

                if (!sCardSystem.getInstance().isLoad())
                    sCardSystem.getInstance().load();

                var pCi = _session.m_pi.findCardById(id);

                if (pCi == null)
                {
                    throw new exception("[Channel::requestOpenCardPack][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou abrir Card Pack[TYPEID=" + (_typeid) + ", ID=" + (id) + "], mas ele nao tem o Card Pack. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        102, 0x5400103));
                }

                if (pCi.qntd < 1)
                {
                    throw new exception("[Channel::requestOpenCardPack][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou abrir Card Pack[TYPEID=" + (_typeid) + ", ID=" + (id) + "], mas ele nao tem quantidade[value=" + (pCi.qntd) + ", request=1] suficiente para abrir Card Pack.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        103, 0x5400104));
                }

                CardPack cp = null;

                cp = (sIff.getInstance().getItemSubGroupIdentify22(_typeid) == 4 ? sCardSystem.getInstance().findBoxCardPack(_typeid) : sCardSystem.getInstance().findCardPack(_typeid));

                if (cp == null)
                {
                    throw new exception("[Channel::requestOpenCardPack][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou abrir Card Pack[TYPEID=" + (_typeid) + ", ID=" + (id) + "], mas nao tem esse Card Pack no Server. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        100, 0x5400101));
                }

                var cards = sCardSystem.getInstance().draws(cp);

                if (cards == null || cards.empty())
                {
                    throw new exception("[Channel::requestOpenCardPack][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou abrir Card Pack[TYPEID=" + (_typeid) + ", ID=" + (id) + "], mas nao conseguiu sortear os cards. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        101, 0x5400102));
                }

                stItem item_rm = new stItem();
                item_rm.type = 2;
                item_rm.id = pCi.id;
                item_rm._typeid = pCi._typeid;
                item_rm.qntd = 1;
                item_rm.STDA_C_ITEM_QNTD = (short)((short)item_rm.qntd * -1);

                if (ItemManager.removeItem(item_rm, _session) <= 0)
                {
                    throw new exception("[Channel::requestOpenCardPack][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou abrir Card Pack[TYPEID=" + (_typeid) + ", ID=" + (id) + "], mas nao conseguiu deletar o Card Pack[TYPEID=" + (pCi._typeid) + ", ID=" + (pCi.id) + "]", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        104, 0x5400105));
                }

                v_item.Add(item_rm);


                // Reserva o espaço na memória, por que se for alocar depois dinamicamente na hora do Add, ele pode realocar memória e mudar o endereço
                // e perder o endereço que eu utilizei no outro List para enviar o ganho de cards, para quando for add no db, nao add 2x o msm card, que pode da bug
                // no async e ele executar o sql do ultimo primeiro ai fica com 1 card a mais do que deveria
                v_item_add.AddRange(new List<stItem>() { new stItem(), new stItem(), new stItem() });

                for (int i = 0; i < cards.Count; i++)
                {
                    var el = cards[i];

                    var bi = new BuyItem();
                    var item = new stItem();

                    bi.id = -1;
                    bi._typeid = el._typeid;
                    bi.qntd = 1;

                    ItemManager.initItemFromBuyItem(_session.m_pi, item, bi, false, 0, 0, 1/*Não verifica o Level*/);

                    if (item._typeid == 0)
                    {
                        throw new exception("[Channel::requestOpenCardPack][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou abrir Card Pack[TYPEID=" + (_typeid) + ", ID=" + (id) + "], mas nao conseguiu inicializar Card[TYPEID=" + (bi._typeid) + "]", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                            105, 0x5400106));
                    }

                    var existing = v_item_add.FirstOrDefault(_el => _el._typeid == item._typeid);

                    if (existing != null)
                    {
                        existing.qntd += 1;
                        existing.STDA_C_ITEM_QNTD = (short)existing.qntd;

                        v_item.Add(new stItem(existing)); // adicionar referência para uso no pacote de retorno
                    }
                    else
                    {
                        v_item_add[i] = item;
                        v_item.Add(item); // mesma referência que vai para o banco e resposta
                    }


                    // Update Achievement Sys
                    switch ((CARD_SUB_TYPE)sIff.getInstance().getItemSubGroupIdentify22(el._typeid))
                    {
                        case CARD_SUB_TYPE.T_CHARACTER:
                            sys_achieve.incrementCounter(0x6C400079u);
                            break;
                        case CARD_SUB_TYPE.T_CADDIE:
                            sys_achieve.incrementCounter(0x6C40007Au);
                            break;
                        case CARD_SUB_TYPE.T_SPECIAL:
                            sys_achieve.incrementCounter(0x6C40007Bu);

                            if (el._typeid == CARD_ABBOT_ELEMENTAL_SHARD)
                            {
                                sys_achieve.incrementCounter(0x6C400080u);
                            }
                            break;
                        case CARD_SUB_TYPE.T_NPC:
                            sys_achieve.incrementCounter(0x6C4000A8u);
                            break;
                    }

                    // Card Tipo, 0x6C40007C Normal, +1 Rare, +2 Super Rare, +3 Secret
                    sys_achieve.incrementCounter(0x6C40007Cu + (uint)el.tipo);

                    if ((byte)el.tipo == 3) // Soma x2 para saber se ele concluí as duas quest com a modificação que eu fiz, de add x1 o contador
                        sys_achieve.incrementCounter(0x6C40007F);
                }
                v_item_add.RemoveAll(x => x._typeid == 0);

                var rai = ItemManager.addItem(v_item_add,
                    _session.getUID(), 0, 0);

                if (rai.fails.Count > 0 && rai.type != ItemManager.RetAddItem.T_SUCCESS_PANG_AND_EXP_AND_CP_POUCH)
                {
                    for (var i = 0; i < v_item_add.Count; ++i)
                    {
                        ids += (i == 0 ? "TYPEID=" : ", TYPEID=") + (v_item_add[i]._typeid);
                    }

                    throw new exception("[Channel::requestOpenCardPack][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou abrir Card Pack[TYPEID=" + (_typeid) + ", ID=" + (id) + "], mas nao conseguiu adicionar o Cards[TYPEID=" + ids + "]", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        106, 0x5400107));
                }

                foreach (stItem el in v_item)//atualizar os ids vazios ou somente os ids sem valor
                {
                    if (v_item_add.Any(c => el._typeid == c._typeid))
                    {
                        if (el.id == -1)
                            el.id = v_item_add.First(c => el._typeid == c._typeid).id;
                        el.stat = v_item_add.First(c => el._typeid == c._typeid).stat;
                    }
                }
                // Update Achievement
                sys_achieve.incrementCounter(0x6C400078u/*Card Pack*/);

                // Resposta para o Card System Open Card Pack
                p.init_plain(0x154);

                p.WriteUInt32(0); // OK

                foreach (stItem el in v_item)
                {

                    p.WriteInt32(el.id);
                    p.WriteUInt32(el._typeid);
                    p.WriteZeroByte(12);

                    var subGroup = sIff.getInstance().getItemSubGroupIdentify22(el._typeid);
                    p.WriteInt32((subGroup == 3/*CardPack*/ || subGroup == 4/*Box CardPack*/) ? 1 : el.stat.qntd_dep);//ta ficando 0 zero aqui

                    p.WriteZeroByte(32);
                    p.WriteUInt16(1);

                    if (subGroup == 3/*CardPack*/ || subGroup == 4/*Box CardPack*/)
                    {
                        p.WriteByte((byte)v_item.Count - 1);
                    }
                    else
                    {
                        p.WriteUInt32(1);
                    }
                }
                packet_func.session_send(p,
                    _session, 1);

                // UPDATE Achievement ON SERVER, DB and GAME
                sys_achieve.finish_and_update(_session);

                ids = "";

                for (var i = 0; i < cards.Count; ++i)
                {
                    ids += ((i == 0) ? "TYPEID=" : ", TYPEID=") + (cards[i]._typeid);
                }
            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestOpenCardPack][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x154);

                // Alguns valores o cliente não aceita como resposta de error
                //p.WriteUInt32((ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) == STDA_ERROR_TYPE::CHANNEL) ? ExceptionError.STDA_SYSTEM_ERROR_DECODE(e.getCodeError()) : 0x5400100);
                p.WriteUInt32(1); // Error

                packet_func.session_send(p,
                    _session, 1);
            }
        }

        public void requestLoloCardCompose(Player _session, packet _packet)
        {
            //

            PangyaBinaryWriter p = new PangyaBinaryWriter();

            try
            {





                if (_session.m_pi.block_flag.m_flag.lolo_copound_card)
                {
                    throw new exception("[Channel::requestLoloCardCompose][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou fundir card no Lolo Card Compose, mas ele nao pode. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        7, 0x790001));
                }

                LoloCardComposeEx lcc = new LoloCardComposeEx();
                List<stItem> v_item = new List<stItem>();
                stItem item = new stItem();

                AchievementSystem sys_achieve = new AchievementSystem();

                ulong pang = 0Ul;

                lcc = (LoloCardComposeEx)new LoloCardComposeEx().ToRead(_packet);

                var r = m_rm.findRoom((short)_session.m_pi.mi.sala_numero);

                for (var i = 0; i < (lcc._typeid.Length); ++i)
                {

                    item = new stItem();

                    item.type = 2;
                    item._typeid = lcc._typeid[i];

                    var card_iff = sIff.getInstance().findCard(item._typeid);

                    if (card_iff == null)
                    {
                        throw new exception("[Channel::requestLoloCardCompose][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou fundir card(s)[TYPEID=" + (lcc._typeid[0]) + ", TYPEID=" + (lcc._typeid[1]) + ", TYPEID=" + (lcc._typeid[2]) + "] no Lolo Card Compose, mas o card[TYPEID=" + (item._typeid) + "] nao existe no IFF_STRUCT do Server. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                            150, 0x5400151));
                    }

                    if (card_iff.Rarity == (byte)CARD_TYPE.T_SECRET)
                    {
                        throw new exception("[Channel::requestLoloCardCompose][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou fundir card(s)[TYPEID=" + (lcc._typeid[0]) + ", TYPEID=" + (lcc._typeid[1]) + ", TYPEID=" + (lcc._typeid[2]) + "] no Lolo Card Compose, mas nao pode fundir card secret. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                            151, 0x5400152));
                    }

                    var pCi = _session.m_pi.findCardByTypeid(card_iff.ID);

                    if (pCi == null)
                    {
                        throw new exception("[Channel::requestLoloCardCompose][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou fundir card(s)[TYPEID=" + (lcc._typeid[0]) + ", TYPEID=" + (lcc._typeid[1]) + ", TYPEID=" + (lcc._typeid[2]) + "] no Lolo Card Compose, mas ele nao tem esse card[TYPEID=" + (item._typeid) + "]. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                            152, 0x5400153));
                    }

                    if (pCi.qntd < 1)
                    {
                        throw new exception("[Channel::requestLoloCardCompose][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou fundir card(s)[TYPEID=" + (lcc._typeid[0]) + ", TYPEID=" + (lcc._typeid[1]) + ", TYPEID=" + (lcc._typeid[2]) + "] no Lolo Card Compose, mas ele nao tem quantidade suficiente do card[TYPEID=" + (pCi._typeid) + ", ID=" + (pCi.id) + ", QNTD=" + (pCi.qntd) + ", request=1]. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                            153, 0x5400154));
                    }

                    // Verifica se o player está com shop aberto e se está vendendo o item no shop


                    if (r != null && r.checkPersonalShopItem(_session, pCi.id))
                    {
                        throw new exception("[Channel::requestLoloCardCompose][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou fundir card(s)[TYPEID=" + (lcc._typeid[0]) + ", TYPEID=" + (lcc._typeid[1]) + ", TYPEID=" + (lcc._typeid[2]) + "], mas o card[TYPEID=" + (pCi._typeid) + ", ID=" + (pCi.id) + "] esta sendo vendido no Personal shop dele. Hacker ou Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                            1010, 0x5201010));
                    }

                    var it = v_item.FirstOrDefault(_el => _el.id == pCi.id);

                    if (it != null && it._typeid == pCi._typeid)
                    { // Update Qntd, já tem esse item no List
                        it.qntd += 1;
                        it.STDA_C_ITEM_QNTD = (short)(it.qntd * -1);
                    }
                    else
                    {
                        item.id = (int)pCi.id;
                        item.qntd = 1;
                        item.STDA_C_ITEM_QNTD = (short)(item.qntd * -1);

                        v_item.Add(new stItem(item));
                    }

                    lcc.tipo = card_iff.Rarity;
                    pang += (ulong)(lcc.tipo == (byte)CARD_TYPE.T_NORMAL ? 1000 : (lcc.tipo == (byte)CARD_TYPE.T_RARE ? 2000 : (lcc.tipo == (byte)CARD_TYPE.T_SUPER_RARE ? 5000 : 1000)));
                }


                if (pang != lcc.pang)
                {
                    throw new exception("[Channel::requestLoloCardCompose][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou fundir card(s)[TYPEID=" + (lcc._typeid[0]) + ", TYPEID=" + (lcc._typeid[1]) + ", TYPEID=" + (lcc._typeid[2]) + "] no Lolo Card Compose, mas os pang[value=" + (pang) + ", request=" + (lcc.pang) + "] é diferente. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        154, 0x5400155));
                }

                var card = sCardSystem.getInstance().drawsLoloCardCompose(lcc);

                if (card == null || card._typeid == 0)
                {
                    throw new exception("[Channel::requestLoloCardCompose][ErrorSystem] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou fundir card(s)[TYPEID=" + (lcc._typeid[0]) + ", TYPEID=" + (lcc._typeid[1]) + ", TYPEID=" + (lcc._typeid[2]) + "] no Lolo Card Compose, mas nao conseguiu sortear um card. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        155, 0x5400156));
                }

                // Remove Cards da fusão
                if (ItemManager.removeItem(v_item, _session) <= 0)
                {
                    throw new exception("[Channel::requestLoloCardCompose][ErrorSystem] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou fundir card(s)[TYPEID=" + (lcc._typeid[0]) + ", TYPEID=" + (lcc._typeid[1]) + ", TYPEID=" + (lcc._typeid[2]) + "] no Lolo Card Compose, nao conseguiu remover os cards[TYPEID=" + (lcc._typeid[0]) + ", TYPEID=" + (lcc._typeid[1]) + ", TYPEID=" + (lcc._typeid[2]) + "]. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        156, 0x5400157));
                }

                // Add o Card que foi sorteado
                BuyItem bi = new BuyItem();

                bi.id = -1;
                bi._typeid = card._typeid;
                bi.qntd = 1;

                item = new stItem();

                ItemManager.initItemFromBuyItem(_session.m_pi,
                    item, bi, false, 0, 0, 1);

                if (item._typeid == 0)
                {
                    throw new exception("[Channel::requestLoloCardCompose][ErrorSystem] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou fundir card(s)[TYPEID=" + (lcc._typeid[0]) + ", TYPEID=" + (lcc._typeid[1]) + ", TYPEID=" + (lcc._typeid[2]) + "] no Lolo Card Compose, mas nao conseguiu inicializar o card[TYPEID=" + (bi._typeid) + "]. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        157, 0x5400158));
                }

                var rt = ItemManager.RetAddItem.T_INIT_VALUE;

                if ((rt = ItemManager.addItem(item,
                    _session, 0, 0)) < 0)
                {
                    throw new exception("[Channel::requestLoloCardCompose][ErrorSystem] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou fundir card(s)[TYPEID=" + (lcc._typeid[0]) + ", TYPEID=" + (lcc._typeid[1]) + ", TYPEID=" + (lcc._typeid[2]) + "] no Lolo Card Compose, mas nao conseguiu adicionar o card[TYPEID=" + (item._typeid) + "]", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        158, 0x5400159));
                }

                if (rt != ItemManager.RetAddItem.T_SUCCESS_PANG_AND_EXP_AND_CP_POUCH)
                {
                    v_item.Add(new stItem(item));
                }

                // UPDATE pang ON Server
                _session.m_pi.consomePang(pang);

                // Update Achievement ON SERVER, DB and GAME

                // Add o tipo do card que ganho na fusão dos cards, Normal 0x6C40008A + tipo, 0 a 3, Normal = 0, Rare = 1, Super Rare = 2 e Secret = 3
                sys_achieve.incrementCounter(0x6C40008Au + (uint)card.tipo);

                // Add +1 ao contador de vezes que o player compose card no Lolo Card Compose
                sys_achieve.incrementCounter(0x6C400089u);

                // UPDATE pang ON GAME
                p.init_plain(0xC8);

                p.WriteUInt64(_session.m_pi.ui.pang);
                p.WriteUInt64(pang);

                packet_func.session_send(p,
                    _session, 1);

                // UPDATE ITEM ON GAME
                p.init_plain(0x216);

                p.WriteUInt32((uint)UtilTime.GetSystemTimeAsUnix());
                p.WriteUInt32((uint)v_item.Count);

                foreach (var el in v_item)
                {
                    p.WriteByte(el.type);
                    p.WriteUInt32(el._typeid);
                    p.WriteInt32(el.id);
                    p.WriteUInt32(el.flag_time);
                    p.WriteBytes(el.stat.ToArray());
                    p.WriteInt32((el.STDA_C_ITEM_TIME > 0) ? el.STDA_C_ITEM_TIME : el.STDA_C_ITEM_QNTD);
                    p.WriteZeroByte(25);
                }

                packet_func.session_send(p,
                    _session, 1);

                // Reposta do Lolo Card Compose
                p.init_plain(0x229);

                p.WriteUInt32(card.tipo); // Card Tipo

                packet_func.session_send(p,
                    _session, 1);

                p.init_plain(0x22A);

                p.WriteUInt32(0); // OK

                p.WriteUInt32(card._typeid); // Card Typeid

                packet_func.session_send(p,
                    _session, 1);

                // UPDATE Achievement ON SERVER, DB and GAME
                sys_achieve.finish_and_update(_session);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestLoloCardCompose][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x22A);

                p.WriteUInt32((ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) == STDA_ERROR_TYPE.CHANNEL) ? ExceptionError.STDA_SYSTEM_ERROR_DECODE(e.getCodeError()) : 0x5400150);

                packet_func.session_send(p,
                    _session, 1);
            }
        }

        public void requestUseCardSpecial(Player _session, packet _packet)
        {
            //

            PangyaBinaryWriter p = new PangyaBinaryWriter();

            try
            {

                uint card_typeid = _packet.ReadUInt32();





                AchievementSystem sys_achieve = new AchievementSystem();

                stItem item = new stItem();
                CardEquipInfoEx cei = new CardEquipInfoEx();

                if (card_typeid == 0)
                {
                    throw new exception("[Channel::requestUseCardSpecial][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou usar card special[TYPEID=" + (card_typeid) + "], mas o typeid é invalid.(zero). Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        350, 0x5500351));
                }

                var pCi = _session.m_pi.findCardByTypeid(card_typeid);

                if (pCi == null)
                {
                    throw new exception("[Channel::requestUseCardSpecial][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou usar card special[TYPEID=" + (card_typeid) + "], mas ele nao tem o card. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        351, 0x5500352));
                }

                if (pCi.qntd < 1)
                {
                    throw new exception("[Channel::requestUseCardSpecial][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou usar card special[TYPEID=" + (card_typeid) + "], nao tem quantidade suficiante[value=" + (pCi.qntd) + ", request=1] de card. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        357, 0x5500358));
                }

                var card = sIff.getInstance().findCard(pCi._typeid);

                if (card == null || card.ID == 0)
                {
                    throw new exception("[Channel::requestUseCardSpecial][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou usar card special[TYPEID=" + (card_typeid) + "], mas o card nao existe no IFF_STRUCT do Server. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        352, 0x5500353));
                }

                if (sIff.getInstance().getItemSubGroupIdentify22(card.ID) != (uint)CARD_SUB_TYPE.T_SPECIAL)
                {
                    throw new exception("[Channel::requestUseCardSpecial][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou usar card special[TYPEID=" + (card_typeid) + "], tentou usar um card que nao é espacial. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        353, 0x5500354));
                }

                // Verifica se o player está com shop aberto e se está vendendo o item no shop

                var r = m_rm.findRoom((short)_session.m_pi.mi.sala_numero);

                if (r != null && r.checkPersonalShopItem(_session, pCi.id))
                {
                    throw new exception("[Channel::requestUseCardSpecial][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou usar card special[TYPEID=" + (pCi._typeid) + ", ID=" + (pCi.id) + "], mas o card esta sendo vendido no Personal shop dele. Hacker ou Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        1010, 0x5201010));
                }



                item = new stItem();

                item.type = 2;
                item.id = (int)pCi.id;
                item._typeid = pCi._typeid;
                item.qntd = 1;
                item.STDA_C_ITEM_QNTD = (short)(item.qntd * -1);

                // Inicializa Card Equip Info
                cei.index = -1;
                cei.id = (uint)pCi.id;
                cei._typeid = pCi._typeid;
                cei.efeito = card.Effect;
                cei.efeito_qntd = card.EffectValue;
                cei.parts_typeid = 0; // Não usa por que é special card
                cei.parts_id = 0; // Não usa por que é special card
                cei.use_yn = 1;
                cei.tipo = sIff.getInstance().getItemSubGroupIdentify22(pCi._typeid);
                cei.slot = 0; // Não usa por que é special card

                switch (card.Effect)
                {
                    // Use Card Special Effect get here NOW
                    case 1: // Exp Value
                        {
                            if ((int)card.EffectValue <= 0)
                            {
                                throw new exception("[Channel::requestUseCardSpecial][ErrorSystem] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou usar card special[TYPEID=" + (pCi._typeid) + ", ID=" + (pCi.id) + "], mas a quantidade do efeito[TYPE=" + (card.Effect) + ", QNTD=" + (card.EffectValue) + "] é invalida. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                    356, 0x5500357));
                            }

                            // UPDATE ON SERVER
                            if (ItemManager.removeItem(item, _session) <= 0)
                            {
                                throw new exception("[Channel::requestUseCardSpecial][ErrorSystem] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou usar card special[TYPEID=" + (pCi._typeid) + ", ID=" + (pCi.id) + "], mas nao conseguiu deletar o card. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                    355, 0x5500356));
                            }

                            _session.addExp(card.EffectValue);
                            break;
                        }
                    case 4: // Pang Value
                        {
                            if ((int)card.EffectValue <= 0)
                            {
                                throw new exception("[Channel::requestUseCardSpecial][ErrorSystem] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou usar card special[TYPEID=" + (pCi._typeid) + ", ID=" + (pCi.id) + "], mas a quantidade do efeito[TYPE=" + (card.Effect) + ", QNTD=" + (card.EffectValue) + "] é invalida. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                    356, 0x5500357));
                            }

                            // UPDATE ON SERVER
                            if (ItemManager.removeItem(item, _session) <= 0)
                            {
                                throw new exception("[Channel::requestUseCardSpecial][ErrorSystem] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou usar card special[TYPEID=" + (pCi._typeid) + ", ID=" + (pCi.id) + "], mas nao conseguiu deletar o card. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                    355, 0x5500356));
                            }

                            _session.addPang(card.EffectValue);
                            break;
                        }
                    case 17: // Pang Value Sorteio
                        {
                            if ((int)card.EffectValue <= 0)
                            {
                                throw new exception("[Channel::requestUseCardSpecial][ErrorSystem] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou usar card special[TYPEID=" + (pCi._typeid) + ", ID=" + (pCi.id) + "], mas a quantidade do efeito[TYPE=" + (card.Effect) + ", QNTD=" + (card.EffectValue) + "] é invalida. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                    356, 0x5500357));
                            }

                            // UPDATE ON SERVER
                            if (ItemManager.removeItem(item, _session) <= 0)
                            {
                                throw new exception("[Channel::requestUseCardSpecial][ErrorSystem] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou usar card special[TYPEID=" + (pCi._typeid) + ", ID=" + (pCi.id) + "], mas nao conseguiu deletar o card. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                    355, 0x5500356));
                            }

                            ulong pang = (ulong)(100 + ((new Random().Next() % Convert.ToInt32(card.EffectValue - 100)) + 1));

                            _session.addPang(pang);
                            break;
                        }
                    // Use Card Special Effect get in Game or End Game, AND PER TIME
                    case 2: // Pang %
                    case 3: // Exp %
                    case 5: // PWR Stat
                    case 6: // CTRL Stat
                    case 7: // ACCURY Stat
                    case 8: // SPIN Stat
                    case 9: // CURVE Stat
                    case 10: // Stat Power Gague
                    case 11: // Item Slot +1
                    case 12: // Impact zone Increase
                    case 13: // Sepia Wind %
                    case 14: // Wind Hill %
                    case 15: // Pink Wind %
                    case 16: // Blue Moon %
                    case 18: // Treasure Hunter %
                    case 19: // Chuva %
                    case 20: // Blue Lagoon %
                    case 21: // Blue Water %
                    case 22: // Shinning Send %
                    case 23: // Deep Inferno %
                    case 24: // Silvia Cannon %
                    case 25: // Eastern Valley %
                    case 26: // Lost Seaway %
                    case 27: // Increase Yard(s) On Power Normal, Not Power Shot
                    case 28: // Increase Power Gague for Pangya shot
                    case 29: // Ice Inferno %
                    case 30: // Wiz City %
                    case 31: // Se chover, persistir no próximo hole a chuva
                    case 32: // Efeito de Flor do esquecimento(Mullegen Rose) infinito por tempo(alguns minutos)
                    case 33: // Uknown
                    case 34: // ClubSet Mastery %
                        {
                            // UPDATE ON SERVER
                            if (ItemManager.removeItem(item, _session) <= 0)
                            {
                                throw new exception("[Channel::requestUseCardSpecial][ErrorSystem] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou usar card special[TYPEID=" + (pCi._typeid) + ", ID=" + (pCi.id) + "], mas nao conseguiu deletar o card. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                    355, 0x5500356));
                            }

                            var pCei = _session.m_pi.findCardEquipedByTypeid(cei._typeid, 0, 0, (int)sIff.getInstance().getItemSubGroupIdentify22(cei._typeid), card.Effect);

                            if (pCei != null)
                            { // já tem um card equipado, aumenta o tempo dele

                                if (pCei._typeid != cei._typeid)
                                { // Mesmo Efeito mas typeid diferente, renova o tempo, e muda o efeito qntd e tempo

                                    pCei.id = cei.id;
                                    pCei._typeid = cei._typeid;
                                    pCei.efeito = card.Effect;
                                    pCei.efeito_qntd = card.EffectValue;
                                    pCei.tipo = sIff.getInstance().getItemSubGroupIdentify22(cei._typeid);
                                    pCei.use_date = new SYSTEMTIME(DateTime.Now); 
                                    pCei.end_date = UtilTime.UnixToSystemTime(UtilTime.SystemTimeToUnix(pCei.use_date) + (card.EffectTime * 60));
                                }
                                else
                                { 
                                    // É o mesmo só aumenta o tempo
                                    var new_end_date = (UtilTime.GetLocalTimeAsUnix() > UtilTime.SystemTimeToUnix(pCei.end_date)) ? UtilTime.GetLocalTimeAsUnix() : UtilTime.SystemTimeToUnix(pCei.end_date);

                                    pCei.end_date = UtilTime.UnixToSystemTime(new_end_date + (card.EffectTime * 60));
                                     
                                }

                                // UPDATE ON DB
                                snmdb.NormalManagerDB.getInstance().add(17, new CmdUpdateCardSpecialTime(_session.m_pi.uid, pCei), SQLDBResponse, this);

                                cei = pCei; 
                            }
                            else
                            { 
                                cei.use_date = new SYSTEMTIME(DateTime.Now);
                                cei.end_date = (UtilTime.UnixToSystemTime(UtilTime.SystemTimeToUnix(cei.use_date) + (card.EffectTime * 60)));

                                // UPDATE ON DB
                                CmdEquipCard cmd_ec = new CmdEquipCard(_session.m_pi.uid, cei, card.EffectTime);

                                snmdb.NormalManagerDB.getInstance().add(10,  cmd_ec, null, null);

                                if (cmd_ec.getException().getCodeError() != 0)
                                {
                                    throw cmd_ec.getException();
                                }

                                cei = cmd_ec.getInfo();

                                _session.m_pi.v_cei.Add(cei);

                                pCei = cei;
                            } 
                            break;
                        }
                    default:
                        throw new exception("[Channel::requestUseCardSpecial][ErrorSystem] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou usar card special[TYPEID=" + (cei._typeid) + ", ID=" + (cei.id) + "], mas card efeito[TYPE=" + (card.Effect) + ", QNTD=" + (card.EffectValue) + ", TEMPO=" + (card.EffectTime) + "min] no IFF_STRUCT do Server é desconhecido. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                            354, 0x5500355));
                }

                // UPDATE ON GAME
                sys_achieve.incrementCounter(0x6C40009E);

                // Resposta do Use Card Special
                p.init_plain(0x160); 
                p.WriteUInt32(0); // OK  
                p.WriteUInt32(cei.id);
                p.WriteUInt32(cei._typeid);
                p.WriteUInt32(cei.parts_typeid);
                p.WriteUInt32(cei.parts_id);
                p.WriteUInt32(cei.slot);
                p.WriteUInt32(1);       // Acho que seja o active date, como estava no meu antigo
                p.WriteTime(cei.use_date);
                p.WriteTime(cei.end_date);
                p.WriteUInt16(0);		// Não sei o que é ainda
                packet_func.session_send(p, _session, 1); 
                // UPDATE Achievement ON SERVER, DB and GAME
                sys_achieve.finish_and_update(_session);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestUseCardSpecial][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x160);

                p.WriteUInt32((ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) == STDA_ERROR_TYPE.CHANNEL) ? ExceptionError.STDA_SYSTEM_ERROR_DECODE(e.getCodeError()) : 0x5500350);

                packet_func.session_send(p,
                    _session, 1);
            }
        }

        public void requestUseItemBuff(Player _session, packet _packet)
        {
            //

            PangyaBinaryWriter p = new PangyaBinaryWriter();

            try
            {

                uint item_typeid = _packet.ReadUInt32(); // Item To Use





                stItem item = new stItem();
                ItemBuffEx ib = new ItemBuffEx();

                if (item_typeid == 0)
                {
                    throw new exception("[Channel::requestUseItemBuff][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou usar o item[TYPEID=" + (item_typeid) + "], mas typeid é invalido(zero). Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        400, 0x5500401));
                }

                var pWi = _session.m_pi.findWarehouseItemByTypeid(item_typeid);

                if (pWi == null)
                {
                    throw new exception("[Channel::requestUseItemBuff][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou usar o item[TYPEID=" + (item_typeid) + "], mas ele nao tem esse item. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        401, 0x5500402));
                }

                if (pWi.STDA_C_ITEM_QNTD < 1)
                {
                    throw new exception("[Channel::requestUseItemBuff][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou usar o item[TYPEID=" + (item_typeid) + "], mas nao tem quantidade suficiente[value=" + (pWi.STDA_C_ITEM_QNTD) + ", request=1] do item. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        404, 0x5500405));
                }

                var item_iff = sIff.getInstance().findItem(item_typeid);

                if (item_iff == null)
                {
                    throw new exception("[Channel::requestUseItemBuff][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou usar o item[TYPEID=" + (item_typeid) + "], mas nao tem esse item no IFF_STRUCT do Server. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        402, 0x5500403));
                }

                var tli = sIff.getInstance().findTimeLimitItem(item_typeid);

                if (tli == null)
                {
                    throw new exception("[Channel::requestUseItemBuff][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou usar o item[TYPEID=" + (item_typeid) + "], mas nao tem esse item na tabela de item buff no IFF_STRUCT do Server. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        403, 0x5500404));
                }

                // UPDATE ON SERVER
                item = new stItem();

                item.type = 2;
                item.id = (int)pWi.id;
                item._typeid = pWi._typeid;
                item.qntd = 1;
                item.STDA_C_ITEM_QNTD = (short)(item.qntd * -1);

                // Initializa Item Buff estrutura
                ib.index = -1;
                ib._typeid = pWi._typeid;
                ib.tipo = tli.type;
                ib.percent = tli.percent;
                ib.use_yn = 1;

                // Remove Item Buff usado
                if (ItemManager.removeItem(item, _session) <= 0)
                {
                    throw new exception("[Channel::requestUseItemBuff][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou usar o item[TYPEID=" + (item_typeid) + "], mas nao conseguiu deletar  o item. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        405, 0x5500406));
                }

                var pIb = _session.m_pi.findItemBuff(ib._typeid);

                if (pIb != null)
                { // já tem um equipado aumenta o tempo dele

                    if (pIb._typeid != ib._typeid)
                    { // Mesmo Efeito mas typeid diferente, renova o tempo, e muda o efeito qntd e tempo

                        pIb.tipo = tli.type;
                        pIb._typeid = ib._typeid;
                        pIb.percent = tli.percent;

                        // Date
                        pIb.use_date.CreateTime();

                        pIb.end_date = (UtilTime.UnixToSystemTime(UtilTime.SystemTimeToUnix(pIb.use_date.ConvertTime()) + (tli.time * 60)));

                        pIb.tempo.setTime((uint)(UtilTime.SystemTimeToUnix(pIb.end_date.ConvertTime()) - UtilTime.SystemTimeToUnix(pIb.use_date.ConvertTime()))); // Make Time in seconds

                    }
                    else
                    { // É o mesmo item só aumenta o tempo

                        var new_end_date = (UtilTime.GetLocalTimeAsUnix() > UtilTime.SystemTimeToUnix(pIb.end_date.ConvertTime())) ? UtilTime.GetLocalTimeAsUnix() : UtilTime.SystemTimeToUnix(pIb.end_date.ConvertTime());

                        pIb.end_date = (UtilTime.UnixToSystemTime((uint)(new_end_date + (tli.time * 60))));

                        pIb.tempo.setTime((uint)(UtilTime.SystemTimeToUnix(pIb.end_date.ConvertTime()) - UtilTime.SystemTimeToUnix(pIb.use_date.ConvertTime()))); // Make Time in seconds
                    }

                    // UPDATE ON DB
                    snmdb.NormalManagerDB.getInstance().add(16,
                        new CmdUpdateItemBuff(_session.m_pi.uid, pIb),
                        SQLDBResponse, this);

                    // Passa o Item buff já equipado(atualizado o tempo) para o novo que foi inicializado
                    ib = pIb;

                    // Log
                }
                else
                { // não tem equipado cria um novo

                    // Date
                    ib.use_date.CreateTime();
                    ib.end_date = (UtilTime.UnixToSystemTime(UtilTime.SystemTimeToUnix(ib.use_date.ConvertTime()) + (tli.time * 60/*FROM MINUTES TO SECONDS*/)));

                    ib.tempo.setTime((uint)(UtilTime.SystemTimeToUnix(st: ib.end_date.ConvertTime()) - UtilTime.SystemTimeToUnix(ib.use_date.ConvertTime())));  // Make Time in seconds

                    // UPDATE ON DB
                    CmdUseItemBuff cmd_uib = new CmdUseItemBuff(_session.m_pi.uid,
                        ib, tli.time);

                    snmdb.NormalManagerDB.getInstance().add(15,
                        cmd_uib, null, null);

                    if (cmd_uib.getException().getCodeError() != 0)
                    {
                        throw cmd_uib.getException();
                    }

                    ib = cmd_uib.getInfo();

                    _session.m_pi.v_ib.Add(ib);
                    var it = ib; // referência ao objeto adicionado

                    pIb = it;

                    // Log
                }

                // UPDATE ON GAME

                // Resposta para o Use Item Buff
                p.init_plain(0x181);

                p.WriteUInt32(2); // OK, add Item Buff

                p.WriteUInt32(1); // Qntd(Count)
                p.WriteUInt32(ib._typeid);
                p.WriteBytes(ib.ToArray());

                packet_func.session_send(p,
                    _session, 1);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestUseItemBuff][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x181);

                p.WriteUInt32((ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) == STDA_ERROR_TYPE.CHANNEL) ? ExceptionError.STDA_SYSTEM_ERROR_DECODE(e.getCodeError()) : 0x5500400);

                packet_func.session_send(p,
                    _session, 1);
            }
        }

        public void requestCometRefill(Player _session, packet _packet)
        {
            //

            PangyaBinaryWriter p = new PangyaBinaryWriter();

            try
            {
                // packet0EC: 0000 EC 00 05 01 00 1A 0A 00 00 14 -- -- -- -- -- -- 	................

                uint item_typeid = _packet.ReadUInt32();
                uint ball_typeid = _packet.ReadUInt32();





                // Carrega Comet Refill System se ele não estiver carregado
                if (!sCometRefillSystem.getInstance().isLoad())
                {
                    sCometRefillSystem.getInstance().load();
                }

                var pBall = _session.m_pi.findWarehouseItemByTypeid(ball_typeid);
                var pItem = _session.m_pi.findWarehouseItemByTypeid(item_typeid);

                if (pBall == null)
                {
                    throw new exception("[Channel::requestCometRefill][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou repreencher a Ball[TYPEID=" + (ball_typeid) + "] com o Item[TYPEID=" + (item_typeid) + "], mas ele nao tem a ball. Hacker ou Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        1, 0x5600101));
                }

                if (pItem == null)
                {
                    throw new exception("[Channel::requestCometRefill][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou repreencher a Ball[TYPEID=" + (ball_typeid) + ", ID=" + (pBall.id) + "] com o Item[TYPEID=" + (item_typeid) + "], mas ele nao tem o item. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        2, 0x5600102));
                }

                if (pItem.STDA_C_ITEM_QNTD < 1)
                {
                    throw new exception("[Channel::requestCometRefill][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou repreencher a Ball[TYPEID=" + (ball_typeid) + ", ID=" + (pBall.id) + "]] com o Item[TYPEID=" + (item_typeid) + ", ID=" + (pItem.id) + "], mas ele nao tem quantidade suficiente[value=" + (pItem.STDA_C_ITEM_QNTD) + ", request=1] do item. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        5, 0x5600105));
                }

                var item_iff = sIff.getInstance().findItem(item_typeid);

                if (item_iff == null)
                {
                    throw new exception("[Channel::requestCometRefill][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou repreencher a Ball[TYPEID=" + (ball_typeid) + ", ID=" + (pBall.id) + "] com o Item[TYPEID=" + (item_typeid) + ", ID=" + (pItem.id) + "], mas o item nao existe no IFF_STRUCT do Server. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        3, 0x5600103));
                }

                var ball_iff = sIff.getInstance().findBall(ball_typeid);

                if (ball_iff == null)
                {
                    throw new exception("[Channel::requestCometRefill][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou repreencher a Ball[TYPEID=" + (ball_typeid) + ", ID=" + (pBall.id) + "] com o Item[TYPEID=" + (item_typeid) + ", ID=" + (pItem.id) + "], mas o ball nao existe no IFF_STRUCT do Server. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        4, 0x5600104));
                }

                var ctx_cr = sCometRefillSystem.getInstance().findCometRefill(pItem._typeid);

                if (ctx_cr == null)
                {
                    throw new exception("[Channel::requestCometRefill][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou repreencher a Ball[TYPEID=" + (ball_typeid) + ", ID=" + (pBall.id) + "] com o Item[TYPEID=" + (item_typeid) + ", ID=" + (pItem.id) + "], mas nao tem o Comet Refill no sistema. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        8, 0x5600100));
                }

                // Sorteia a quantidade do comet refill
                var qntd = sCometRefillSystem.getInstance().drawsCometRefill(ctx_cr);
                if (qntd == 0)
                {
                    throw new exception("[Channel::requestCometRefill][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou repreencher a Ball[TYPEID=" + (ball_typeid) + ", ID=" + (pBall.id) + "] com o Item[TYPEID=" + (item_typeid) + ", ID=" + (pItem.id) + "], zero na quantidade, mas nao tem o Comet Refill no sistema. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                       8, 0x5600100));
                }
                // UPDATE ON SERVER

                stItem item = new stItem();

                item.type = 2;
                item.id = (int)pItem.id;
                item._typeid = pItem._typeid;
                item.qntd = 1;
                item.STDA_C_ITEM_QNTD = (short)(item.qntd * -1);

                if (ItemManager.removeItem(item, _session) <= 0)
                {
                    throw new exception("[Channel::requestCometRefill][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou repreencher a Ball[TYPEID=" + (ball_typeid) + ", ID=" + (pBall.id) + "] com o Item[TYPEID=" + (item_typeid) + ", ID=" + (pItem.id) + "], mas ele nao conseguiu deletar o item. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        6, 0x5600106));
                }

                // UPDATE QNTY BALL
                item = new stItem();

                item.type = 2;
                item.id = (int)pBall.id;
                item._typeid = pBall._typeid;
                item.qntd = (int)qntd;
                item.STDA_C_ITEM_QNTD = (short)item.qntd;

                var rt = ItemManager.RetAddItem.T_INIT_VALUE;

                if ((rt = ItemManager.addItem(item,
                    _session, 0, 0)) < 0)
                {
                    throw new exception("[Channel::requestCometRefill][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou repreencher a Ball[TYPEID=" + (ball_typeid) + ", ID=" + (pBall.id) + "] com o Item[TYPEID=" + (item_typeid) + ", ID=" + (pItem.id) + "], mas nao conseguiu atualizar quantidade da ball. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        7, 0x5600107));
                }

                // UPDATE ON GAME

                // Resposta para o Comet Refill
                p.init_plain(0x197);

                p.WriteByte(1); // OK

                p.WriteUInt32(pItem._typeid);
                p.WriteUInt32(pBall._typeid);
                p.WriteUInt16(pBall.STDA_C_ITEM_QNTD); // Pode ser esse também, item.stat.qntd_dep, por que a bola foi a ultima que eu att no server e db

                packet_func.session_send(p,
                    _session, 1);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestCometRefill][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x197);

                p.WriteByte(0);

                p.WriteZeroByte(10);

                packet_func.session_send(p,
                    _session, 1);
            }
        }

        public void requestOpenMailBox(Player _session, packet _packet)
        {
            //

            PangyaBinaryWriter p = new PangyaBinaryWriter();

            try
            {

                if (_session.m_pi.block_flag.m_flag.mail_box)
                {
                    throw new exception("[Channel::requestOpenMailBox][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou abrir Mail Box, mas ele nao pode. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        5, 0x790001));
                }

                int pagina = _packet.ReadInt32();

                if (pagina <= 0)
                {
                    throw new exception("[Channel::requestOpenMailBox][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou abrir Mail Box[Pagina=" + (pagina) + "], mas a pagina é invalida.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        6, 0x790002));
                }

                var mails = _session.m_pi.m_mail_box.GetPage((uint)pagina);

                if (mails.Any())
                {
                    // pagina existe, envia ela
                    packet_func.session_send(packet_func.pacote211(mails, pagina, (int)_session.m_pi.m_mail_box.getTotalPages()/*cmd_mbi.getTotalPage()*/), _session);

                }
                else
                { // MailBox Vazio                                                  
                    packet_func.session_send(packet_func.pacote211(new List<MailBox>(), pagina, 1), _session);
                }
            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestOpenMailBox][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x211);

                p.WriteUInt32((ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) == STDA_ERROR_TYPE.CHANNEL) ? ExceptionError.STDA_SYSTEM_ERROR_DECODE(e.getCodeError()) : 0x5500200);

                packet_func.session_send(p,
                    _session, 1);
            }
        }

        public void requestInfoMail(Player _session, packet _packet)
        {
            //

            PangyaBinaryWriter p = new PangyaBinaryWriter();

            try
            {

                int email_id = _packet.ReadInt32();

                var email = _session.m_pi.m_mail_box.getEmailInfo(email_id);

                if (email.id == 0)
                {
                    throw new exception("[Channel::requestInfoMail][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] pediu para ver o info do Mail[ID=" + (email_id) + "], mais ele nao existe no banco de dados. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        0x5500251, 1));
                }

                try
                {
                    ItemManager.checkSetItemOnEmail(_session, email);
                }
                catch (exception e)
                {
                    // Se não for item List vazio, relança a exception
                    if (!ExceptionError.STDA_ERROR_CHECK_SOURCE_AND_ERROR_TYPE(e.getCodeError(),
                        STDA_ERROR_TYPE._ITEM_MANAGER,
                        20))
                    {
                        throw;
                    }
                }
                packet_func.session_send(packet_func.pacote212(email),
                    _session, 1);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestInfoMail][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x212);

                p.WriteUInt32((ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) == STDA_ERROR_TYPE.CHANNEL) ? ExceptionError.STDA_SYSTEM_ERROR_DECODE(e.getCodeError()) : 0x5500250);

                packet_func.session_send(p,
                    _session, 1);
            }
        }

        public void requestSendMail(Player _session, packet _packet)
        {
            //

            PangyaBinaryWriter p = new PangyaBinaryWriter();

            try
            {

                uint from_uid = _packet.ReadUInt32();
                uint to_uid = _packet.ReadUInt32();
                string to_nick = _packet.ReadString();
                ushort unknown_opt = _packet.ReadUInt16();
                string to_msg = _packet.ReadString();
                ulong pang_price = _packet.ReadUInt64();
                byte count_item = _packet.ReadUInt8();

                if (string.IsNullOrEmpty(to_nick))
                    throw new exception("[Channel::requestSendMail][Error] PLAYER[UID=" + (_session.m_pi.uid) + "] tentou contra o server[MESSAGE="
                            + to_nick + "], vazio. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 1, 1/*UNKNOWN ERROR*/));

                if (!Tools.Sanitize(to_nick))
                    throw new exception("[Channel::requestSendMail][Error] PLAYER[UID=" + (_session.m_pi.uid) + "] tentou contra o server[MESSAGE="
                            + to_nick + "], tentativa de inject. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 1, 1/*UNKNOWN ERROR*/));

                if (string.IsNullOrEmpty(to_msg))
                    throw new exception("[Channel::requestSendMail][Error] PLAYER[UID=" + (_session.m_pi.uid) + "] tentou contra o server[MESSAGE="
                            + to_msg + "], vazio. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 1, 1/*UNKNOWN ERROR*/));

                if (!Tools.Sanitize(to_msg))
                    throw new exception("[Channel::requestSendMail][Error] PLAYER[UID=" + (_session.m_pi.uid) + "] tentou contra o server[MESSAGE="
                            + to_msg + "], tentativa de inject. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 1, 1/*UNKNOWN ERROR*/));

                if (count_item > 0)
                {

                    if (count_item > 4)
                    {
                        throw new exception("[Channel::requestSendMail][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou enviar um numero[value=" + (count_item) + "] de itens é maior que o permitido. Bug ou Hacker", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PACKET_FUNC_SV,
                            150, 5100081));
                    }

                    if (pang_price != (ulong)(count_item * 500))
                    {
                        throw new exception("[Channel::requestSendMail][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou usar pang price[value_client=" + (count_item) + ", value_srv=" + (count_item * 500) + "] send message is wrong. Bug ou Hacker", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PACKET_FUNC_SV,
                            153, 5100084));
                    }

                    EmailInfo.item[] aItem = Tools.InitializeWithDefaultInstances<EmailInfo.item>(count_item);
                    List<stItem> v_item = new List<stItem>();
                    stItem item = new stItem();

                    for (int i = 0; i < count_item; i++)
                    {
                        aItem[i] = new EmailInfo.item().ToRead(_packet);
                    }

                    IFFCommon pBase = null;

                    var r = m_rm.findRoom((short)_session.m_pi.mi.sala_numero);

                    for (var i = 0; i < count_item; ++i)
                    {

                        var group = sIff.getInstance().getItemGroupIdentify(aItem[i]._typeid);

                        if (group != IFF_GROUP.BALL
                            && group != IFF_GROUP.CLUBSET
                            && group != IFF_GROUP.ITEM
                            && group != IFF_GROUP.PART)
                        {
                            throw new exception("[Channel::requestSendMail][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou enviar um item[TYPEID=" + (aItem[i]._typeid) + ", ID=" + (aItem[i].id) + "] para o PLAYER [UID=" + (to_uid) + "], mas esse item nao pode ser enviado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PACKET_FUNC_SV,
                                154, 5100085));
                        }

                        pBase = sIff.getInstance().findCommomItem(aItem[i]._typeid);

                        if (pBase == null)
                        {
                            throw new exception("[Channel::requestSendMail][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou enviar um item[TYPEID=" + (aItem[i]._typeid) + ", ID=" + (aItem[i].id) + "] para o PLAYER [UID=" + (to_uid) + "], mas esse item nao tem no STRUCT IFF do server. Bug ou Hacker", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PACKET_FUNC_SV,
                                151, 5100082));
                        }

                        if (!pBase.Shop.flag_shop.can_send_mail_and_personal_shop)
                        {
                            throw new exception("[Channel::requestSendMail][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou enviar um item[TYPEID=" + (aItem[i]._typeid) + ", ID=" + (aItem[i].id) + "] para o PLAYER [UID=" + (to_uid) + "], mas esse item nao é permitido ser enviado por mail. Bug ou Hacker", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PACKET_FUNC_SV,
                                152, 5100083));
                        }

                        if (!sIff.getInstance().IsCanOverlapped(pBase.ID) && ItemManager.ownerItem(to_uid, pBase.ID))
                        {
                            throw new exception("[Channel::requestSendMail][Error][Sucess] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou enviar um item[TYPEID=" + (pBase.ID) + ", ID=" + (aItem[i].id) + "] que o outro PLAYER [UID=" + (to_uid) + "] ja tem esse item.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PACKET_FUNC_SV,
                                156, 5100087));
                        }

                        item = new stItem();

                        var pWi = _session.m_pi.findWarehouseItemByTypeid(aItem[i]._typeid);

                        if (pWi == null)
                        {
                            throw new exception("[Channel::requestSendMail][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou enviar um item[TYPEID=" + (aItem[i]._typeid) + ", ID=" + (aItem[i].id) + "] para o PLAYER [UID=" + (to_uid) + "], mas ele nao tem esse item. Bug ou Hacker", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PACKET_FUNC_SV,
                                157, 5100088));
                        }

                        // Verifica se o player está com shop aberto e se está vendendo o item no shop


                        if (r != null && r.checkPersonalShopItem(_session, (int)aItem[i].id))
                        {
                            throw new exception("[Channel::requestSendMail][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou enviar o item[TYPEID=" + (aItem[i]._typeid) + ", ID=" + (aItem[i].id) + "] para o PLAYER [UID=" + (to_uid) + "], mas o item esta sendo vendido no Personal shop dele. Hacker ou Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                1010, 0x5201010));
                        }

                        if (group == PangyaAPI.IFF.JP.Models.Flags.IFF_GROUP.ITEM)
                        {

                            if (aItem[i].qntd > 99)
                            {
                                throw new exception("[Channel::requestSendMail][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou enviar um item[TYPEID=" + (aItem[i]._typeid) + ", ID=" + (aItem[i].id) + "] para o PLAYER [UID=" + (to_uid) + "], mas a quantidade[value=" + (aItem[i].qntd) + "] maior que 99. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PACKET_FUNC_SV,
                                    155, 5100086));
                            }

                            if (pWi.STDA_C_ITEM_QNTD < aItem[i].qntd)
                            {
                                throw new exception("[Channel::requestSendMail][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou enviar um item[TYPEID=" + (aItem[i]._typeid) + ", ID=" + (aItem[i].id) + "] para o PLAYER [UID=" + (to_uid) + "], mas ele nao tem quantidade[value=" + (pWi.STDA_C_ITEM_QNTD) + ", req=" + (aItem[i].qntd) + "] suficiente. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PACKET_FUNC_SV,
                                    158, 5100089));
                            }
                        }

                        item.id = (int)aItem[i].id;
                        item._typeid = aItem[i]._typeid;
                        item.flag_time = aItem[i].flag_time;
                        item.STDA_C_ITEM_QNTD = (short)(item.qntd = (int)aItem[i].qntd);
                        item.STDA_C_ITEM_TIME = (short)(short)aItem[i].tempo_qntd;
                        item.ucc.IDX = aItem[i].ucc_img_mark;

                        item.type = 2;

                        v_item.Add(new stItem(item));
                    }



                    if (ItemManager.giveItem(v_item,
                        _session, 1) <= 0)
                    {
                        throw new exception("[Channel::requestSendMail][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] nao conseguiu presentear o PLAYER [UID=" + (to_uid) + "]", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PACKET_FUNC_SV,
                            159, 5100090));
                    }


                    packet_func.session_send(packet_func.pacote216(v_item),
                        _session, 1);

                    var msg_id = MailBoxManager.sendMessageWithItem(from_uid,
                        to_uid, to_msg, aItem,
                        count_item);

                    _session.m_pi.consomePang(pang_price);

                    // Log
                    string log_itens = "";

                    foreach (var el in v_item)
                    {

                        if (log_itens.empty())
                        {
                            log_itens += "";
                        }

                        log_itens += "[TYPEID=" + (el._typeid) + ", ID=" + (el.id) + ", FLAG_TIME=" + ((ushort)el.flag_time) + ", QNTD=" + ((el.STDA_C_ITEM_TIME > 0 ? el.STDA_C_ITEM_TIME : el.STDA_C_ITEM_QNTD)) + ", QNTD_DEPOIS=" + (el.stat.qntd_dep) + "]";
                    }

                    _smp.message_pool.getInstance().push(new message("[Channel::requestSendMail][Sucess] PLAYER [UID=" + (_session.m_pi.uid) + "] enviou presente para o PLAYER [UID=" + (to_uid) + "] MailBox[Email_ID=" + (msg_id) + ", Message=" + to_msg + "] item(ns)[QNTD=" + (v_item.Count) + "] Item(ns){" + log_itens + "}", type_msg.CL_ONLY_FILE_LOG));

                    // Update Pang
                    p.init_plain(0xC8);

                    p.WriteUInt64(_session.m_pi.ui.pang);
                    p.WriteUInt64(pang_price);

                    packet_func.session_send(p,
                        _session, 1);

                    // Good send Mail
                    p.init_plain(0x213);

                    p.WriteUInt32(0);

                    packet_func.session_send(p,
                        _session, 1);

                }
                else
                {

                    if (pang_price != 100)
                    {
                        throw new exception("[Channel::requestSendMail][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou usar pang price[value_client=" + (count_item) + ", value_srv=" + (100) + "] send message is wrong. Bug ou Hacker", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PACKET_FUNC_SV,
                            153, 5100084));
                    }

                    var msg_id = MailBoxManager.sendMessage(from_uid,
                        to_uid, to_msg);

                    _session.m_pi.consomePang(pang_price);

                    // Update Pang
                    p.init_plain(0xC8);

                    p.WriteUInt64(_session.m_pi.ui.pang);
                    p.WriteUInt64(pang_price);

                    packet_func.session_send(p,
                        _session, 1);

                    // Good send Mail
                    p.init_plain(0x213);

                    p.WriteUInt32(0);

                    packet_func.session_send(p,
                        _session, 1);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestSendMail][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x213);

                p.WriteUInt32((ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) == STDA_ERROR_TYPE.CHANNEL) ? ExceptionError.STDA_SYSTEM_ERROR_DECODE(e.getCodeError()) : 0x5500300);

                packet_func.session_send(p,
                    _session, 1);
            }
        }

        public void requestTakeItemFomMail(Player _session, packet _packet)
        {
            //

            PangyaBinaryWriter p = new PangyaBinaryWriter();

            try
            {

                int email_id = _packet.ReadInt32();
                // Level temporário do player para quando o player pegar Exp Pouch e 
                // subir de level atualizar o info dele e se ele estiver na lobby atualizar para todos da lobby o level dele
                ushort tmp_level = (ushort)_session.m_pi.mi.level;

                var ei = _session.m_pi.m_mail_box.getEmailInfo(email_id, false); // Não ler o email


                List<stItem> v_item = new List<stItem>();

                if (!ei.itens.empty())
                {
                    // trata os itens que pegou do banco de dados antes de add
                    // no warehouse item e depois no banco de dados com async
                    stItem item = new stItem();

                    for (var i = 0; i < ei.itens.Count; ++i)
                    {

                        item = new stItem();

                        ItemManager.initItemFromEmailItem(_session.m_pi,
                           item, ei.itens[i]);

                        if (item._typeid == 0)
                        {

                            _smp.message_pool.getInstance().push(new message("[Channel::requestTakeItemFrom][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou inicializar o item que pegou do mailbox[MAIL_ID=" + (email_id) + "].", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            // System Error 
                            packet_func.session_send(packet_func.pacote214(
                                3),
                                _session, 1);

                            return;
                        }

                        // Verifica se já possui o item, o caddie item verifica se tem o caddie para depois verificar se tem o caddie item
                        if ((sIff.getInstance().IsCanOverlapped(ei.itens[i]._typeid) && sIff.getInstance().getItemGroupIdentify(ei.itens[i]._typeid) != IFF_GROUP.CAD_ITEM) || !_session.m_pi.ownerItem(ei.itens[i]._typeid, 1))
                        {

                            // Verifica se o item é um SetItem
                            if (ItemManager.isSetItem(item._typeid))
                            {

                                var v_stItem = ItemManager.getItemOfSetItem(_session,
                                    ei.itens[i]._typeid, false, 1);

                                // No gift ele envia o set para o player, e não os itens que contém dentro do set
                                if (!v_stItem.empty())
                                {
                                    // Já verificou lá em cima se tem os item so set, então não precisa mais verificar aqui
                                    // Só add eles ao List de venda
                                    // Verifica se pode ter mais de 1 item e se não ver se não tem o item

                                    foreach (var el in v_stItem)
                                    {
                                        if ((sIff.getInstance().IsCanOverlapped(el._typeid) && sIff.getInstance().getItemGroupIdentify(el._typeid) != IFF_GROUP.CAD_ITEM) || !_session.m_pi.ownerItem(el._typeid, 1))
                                        {
                                            v_item.Add(new stItem(el));
                                        }
                                    }
                                }
                                else
                                {
                                    _smp.message_pool.getInstance().push(new message("[Channel::requestTakeItemFrom][Sucess] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou add set item sem item dentro, do MailBox[MAIL_ID=" + (email_id) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));
                                }
                            }
                            else
                            {
                                v_item.Add(new stItem(item));
                            }

                        }
                        else if (sIff.getInstance().getItemGroupIdentify(ei.itens[i]._typeid) == PangyaAPI.IFF.JP.Models.Flags.IFF_GROUP.CAD_ITEM)
                        {
                            throw new exception("[Channel::requestTakeItemFrom][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou pegar um CaddieItem[TYPEID=" + (ei.itens[i]._typeid) + "] do Mail[ID=" + (email_id) + "] de um caddie que ele nao possui", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PACKET_FUNC_SV,
                                201, 5100072));
                        }
                        else
                        {
                            throw new exception("[Channel::requestTakeItemFrom][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou pegar um item[TYPEID=" + (ei.itens[i]._typeid) + "] do Mail[ID=" + (email_id) + "] que ele ja possui, nao pode ter duplicatas", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PACKET_FUNC_SV,
                                201, 5100071));
                        }
                    }

                    // UPDATE ON DB
                    _session.m_pi.m_mail_box.leftItensFromEmail(email_id);

                    // Add Item
                    var rai = ItemManager.addItem(v_item,
                        _session.getUID(), 1, 0);

                    if (rai.fails.Count > 0 && rai.type != ItemManager.RetAddItem.T_SUCCESS_PANG_AND_EXP_AND_CP_POUCH)
                    {

                        foreach (var fail in rai.fails)
                        {
                            _smp.message_pool.getInstance().push(new message("[Channel::requestTakeItemFrom][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou mover o item[TYPEID=" + (fail._typeid) + ", ID=" + (fail.id) + "] do MailBox[EMAIL_ID=" + (email_id) + "] para o MyRoom, mas nao conseguiu. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                        }

                        packet_func.session_send(packet_func.pacote214(
                                2),
                            _session, 1);

                        return;
                    }

                    // Log
                    string log_itens = "";

                    foreach (var el in v_item)
                    {

                        if (log_itens.empty())
                        {
                            log_itens += "";
                        }

                        log_itens += "[TYPEID=" + (el._typeid) + ", ID=" + (el.id) + ", FLAG_TIME=" + ((ushort)el.flag_time) + ", QNTD=" + ((el.STDA_C_ITEM_TIME > 0 ? el.STDA_C_ITEM_TIME : el.STDA_C_ITEM_QNTD)) + ", QNTD_DEPOIS=" + (el.stat.qntd_dep) + "]";
                    }

                    // UPDATE ON GAME
                    packet_func.session_send(packet_func.pacote216(v_item),
                       _session, 1);

                    packet_func.session_send(packet_func.pacote214(),
                       _session, 1);

                    // att level no canal
                    if (tmp_level != _session.m_pi.mi.level)
                    {

                        updatePlayerInfo(_session);

                        if (_session.m_pi.lobby != DEFAULT_CHANNEL)
                        {

                            var pi = getPlayerInfo(_session);

                            if (pi != null)
                            {
                                packet_func.channel_broadcast(this,
                                     packet_func.pacote046(
                                new List<PlayerLobbyInfo>() { pi },
                                    3), 1);
                            }
                        }
                    }

                }
                else
                { // Não tem item do email, erro o cliente não poderia chamar esse pacote, por que esse email não tem item

                    packet_func.session_send(packet_func.pacote214(1),
                        _session, 1);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestTakeItemFromMail][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));


                packet_func.session_send(packet_func.pacote214(
                    (int)((ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) == STDA_ERROR_TYPE.CHANNEL) ? ExceptionError.STDA_SYSTEM_ERROR_DECODE(e.getCodeError()) : 0x5500100)),
                    _session, 1);
            }
        }

        public void requestDeleteMail(Player _session, packet _packet)
        {
            //

            PangyaBinaryWriter p = new PangyaBinaryWriter();
            uint[] a_email_id = new uint[0];

            try
            {





                uint num_email = _packet.ReadUInt32();
                uint pagina = 1;

                a_email_id = new uint[num_email];

                a_email_id = _packet.Read(num_email).ToArray();

                pagina = _packet.ReadUInt32();

                if ((int)pagina <= 0)
                {
                    throw new exception("[Channel::requestDeleteMail][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] pedeiu para deletar email(s)[COUNT=" + (num_email) + "] da pagina(" + ((int)pagina) + "), mas a pagina é invalida.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        6, 0x791002));
                }


                // UPDATE ON DB

                _session.m_pi.m_mail_box.deleteEmail(a_email_id, (uint)num_email);

                var mails = _session.m_pi.m_mail_box.GetPage((uint)pagina);

                // Ainda tem que ver se a pagina que ele solicita não tem mais depois que excluiu os emails, tem que checar isso tbm
                if (mails.Any())
                {
                    packet_func.session_send(packet_func.pacote215(mails, (int)pagina,
                        (int)_session.m_pi.m_mail_box.getTotalPages()),
                        _session, 1);

                }
                else
                { // MailBox Vazio

                    packet_func.session_send(packet_func.pacote215(
                        new List<MailBox>(),
                        (int)pagina, 1),
                        _session, 1);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestDeleteMail][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x215);

                p.WriteUInt32((ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) == STDA_ERROR_TYPE.CHANNEL) ? ExceptionError.STDA_SYSTEM_ERROR_DECODE(e.getCodeError()) : 0x5500150);

                packet_func.session_send(p,
                    _session, 1);
            }

            // Delete Array Email Id
            if (a_email_id != null)
            {
                a_email_id = null;
            }
        }

        public void requestMakePassDolfiniLocker(Player _session, packet _packet)
        {
            //

            PangyaBinaryWriter p = new PangyaBinaryWriter();

            try
            {

                string pass = _packet.ReadString();

                if (string.IsNullOrEmpty(pass))
                    throw new exception("[Channel::requestMakePassDolfiniLocker][Error] PLAYER[UID=" + (_session.m_pi.uid) + "] tentou contra o server[MESSAGE="
                            + pass + "], vazio. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 1, 1/*UNKNOWN ERROR*/));

                if (!Tools.Sanitize(pass))
                    throw new exception("[Channel::requestMakePassDolfiniLocker][Error] PLAYER[UID=" + (_session.m_pi.uid) + "] tentou contra o server[MESSAGE="
                            + pass + "], tentativa de inject. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 1, 1/*UNKNOWN ERROR*/));



                if (pass.Length == 0)
                {
                    throw new exception("[Channel::requestMakePassDolfiniLocker][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentrou trocar a senha do dolfini locker com senha vazia. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        200, 5100101));
                }

                if (pass.Length > 4)
                {
                    throw new exception("[Channel::requestMakePassDolfiniLocker][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou trocar a senha do dolfini locker com um senha maior do que o permitido. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        201, 5100102));
                }

                _session.m_pi.df.pass =
                        pass;

                p.init_plain(0x176);

                p.WriteUInt32(0);

                packet_func.session_send(p,
                    _session, 1);

                // Cmd Update Pass Dolfini Locker
                snmdb.NormalManagerDB.getInstance().add(1,
                    new CmdUpdateDolfiniLockerPass(_session.m_pi.uid, pass),
                    SQLDBResponse, this);

            }
            catch (exception e)
            {
                _smp.message_pool.getInstance().push(new message("[Channel::requestMakePassDolfiniLocker][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x176);

                p.WriteUInt32((ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) == STDA_ERROR_TYPE.CHANNEL) ? ExceptionError.STDA_SYSTEM_ERROR_DECODE(e.getCodeError()) : 5100100);

                packet_func.session_send(p,
                    _session, 1);
            }

        }

        public void requestCheckDolfiniLockerPass(Player _session, packet _packet)
        {
            //

            PangyaBinaryWriter p = new PangyaBinaryWriter();

            try
            {

                string pass = _packet.ReadString();

                if (string.IsNullOrEmpty(pass))
                    throw new exception("[Channel::requestCheckDolfiniLockerPass][Error] PLAYER[UID=" + (_session.m_pi.uid) + "] tentou contra o server[MESSAGE="
                            + pass + "], vazio. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 1, 1/*UNKNOWN ERROR*/));

                if (!Tools.Sanitize(pass))
                    throw new exception("[Channel::requestCheckDolfiniLockerPass][Error] PLAYER[UID=" + (_session.m_pi.uid) + "] tentou contra o server[MESSAGE="
                            + pass + "], tentativa de inject. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 1, 1/*UNKNOWN ERROR*/));



                if (pass.Length == 0)
                {
                    throw new exception("[Channel::requestCheckDolfiniLockerPass][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou entrar no Dolfini Locker com uma senha vazia. Hacker ou Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        250, 5100151));
                }

                if (pass.Length > 4)
                {
                    throw new exception("[Channel::requestCheckDolfiniLockerPass][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou entrar no Dolfini Locker com uma senha maior que a suportada. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        251, 5100152));
                }

                p.init_plain(0x16C);

                if (string.CompareOrdinal(pass, _session.m_pi.df.pass) != 0)
                {
                    _smp.message_pool.getInstance().push(new message("[Channel::requesCheckDolfiniLockerPass][Sucess] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou entrar no Dolfini Locker com senha[value=" + pass + "] errada", type_msg.CL_FILE_LOG_AND_CONSOLE));

                    p.WriteUInt32(0x75); // Senha errada
                }
                else
                {
                    _smp.message_pool.getInstance().push(new message("[Channel::requesCheckDolfiniLockerPass][Sucess] PLAYER [UID=" + (_session.m_pi.uid) + "] logou com sucesso no Dolfini Locker", type_msg.CL_FILE_LOG_AND_CONSOLE));

                    _session.m_pi.df.pass_check = true;

                    p.WriteUInt32(0); // Senha Correta
                }

                packet_func.session_send(p,
                    _session, 1);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestDolfiniLockerPass][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x16C);

                p.WriteUInt32((ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) == STDA_ERROR_TYPE.CHANNEL) ? ExceptionError.STDA_SYSTEM_ERROR_DECODE(e.getCodeError()) : 5100150);

                packet_func.session_send(p,
                    _session, 1);
            }
        }

        public void requestChangeDolfiniLockerPass(Player _session, packet _packet)
        {
            //

            PangyaBinaryWriter p = new PangyaBinaryWriter();

            try
            {

                string old_pass = _packet.ReadString();
                string new_pass = _packet.ReadString();


                if (string.IsNullOrEmpty(old_pass))
                    throw new exception("[Channel::requestChangeDolfiniLocker][Error] PLAYER[UID=" + (_session.m_pi.uid) + "] tentou contra o server[MESSAGE="
                            + old_pass + "], vazio. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 1, 1/*UNKNOWN ERROR*/));

                if (!Tools.Sanitize(old_pass))
                    throw new exception("[Channel::requestChangeDolfiniLocker][Error] PLAYER[UID=" + (_session.m_pi.uid) + "] tentou contra o server[MESSAGE="
                            + old_pass + "], tentativa de inject. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 1, 1/*UNKNOWN ERROR*/));

                if (string.IsNullOrEmpty(new_pass))
                    throw new exception("[Channel::requestChangeDolfiniLocker][Error] PLAYER[UID=" + (_session.m_pi.uid) + "] tentou contra o server[MESSAGE="
                            + new_pass + "], vazio. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 1, 1/*UNKNOWN ERROR*/));

                if (!Tools.Sanitize(new_pass))
                    throw new exception("[Channel::requestChangeDolfiniLocker][Error] PLAYER[UID=" + (_session.m_pi.uid) + "] tentou contra o server[MESSAGE="
                            + new_pass + "], tentativa de inject. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 1, 1/*UNKNOWN ERROR*/));


                if (old_pass.Length > 4 || new_pass.Length > 4)
                {
                    throw new exception("[Channel::requestChangeDolfiniLocker][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou trocar a senha, mas old_pass[value=" + old_pass + "] or new_pass[value=" + new_pass + "] length is hight of permited. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        301, 5100202));
                }

                p.init_plain(0x174);

                if (string.CompareOrdinal(old_pass, _session.m_pi.df.pass) != 0)
                {
                    _smp.message_pool.getInstance().push(new message("[Dolfini Locker::Change Pass][Sucess] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou trocar a senha mas a senha[value=" + old_pass + "] antiga esta incorreta", type_msg.CL_FILE_LOG_AND_CONSOLE));

                    p.WriteUInt32(1); // Não sei direito mas vou usar o 1
                }
                else
                {

                    _session.m_pi.df.pass =
                            new_pass;

                    p.WriteUInt32(0);

                    _smp.message_pool.getInstance().push(new message("[Dolfini Locker::Change Pass][Sucess] PLAYER [UID=" + (_session.m_pi.uid) + "] trocou a senha[old=" + old_pass + ", new=" + new_pass + "] com sucesso", type_msg.CL_FILE_LOG_AND_CONSOLE));

                    snmdb.NormalManagerDB.getInstance().add(1,
                        new CmdUpdateDolfiniLockerPass(_session.m_pi.uid, new_pass),
                        SQLDBResponse, this);
                }

                packet_func.session_send(p,
                    _session, 1);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::DolfiniLockerPass][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x174);

                p.WriteUInt32((ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) == STDA_ERROR_TYPE.CHANNEL) ? ExceptionError.STDA_SYSTEM_ERROR_DECODE(e.getCodeError()) : 5100200);

                packet_func.session_send(p,
                    _session, 1);
            }
        }

        public void requestChangeDolfiniLockerModeEnter(Player _session, packet _packet)
        {
            //

            PangyaBinaryWriter p = new PangyaBinaryWriter();

            try
            {

                byte locker = _packet.ReadUInt8();
                string pass = _packet.ReadString();





                if (pass.Length == 0)
                {
                    throw new exception("[Channel::requestChangeDolfiniLockerModeEnter][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou trocar o modo de entrar no dolfini locker, mas a senha fornecida esta vazia. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        350, 5100251));
                }

                if (pass.Length > 4)
                {
                    throw new exception("[Channel::requestChangeDolfiniLockerModeEnter][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou trocar o modo de entrar no dolfini locker, mas o tamanho da senha é maior que o permitido. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        351, 5100252));
                }

                p.init_plain(0x173);

                if (string.CompareOrdinal(pass, _session.m_pi.df.pass) != 0)
                {
                    _smp.message_pool.getInstance().push(new message("[Dolfini Locker::Change Mode Enter][Sucess] senha[value=" + pass + "] fornecida incorreta, nao combina com a do PLAYER [UID=" + (_session.m_pi.uid) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                    p.WriteUInt32(1); // não sei direito o valor de erro nesse pacote, mas vou usar 1
                }
                else
                {

                    _session.m_pi.df.locker = locker == 1;

                    p.WriteUInt32(0);

                    _smp.message_pool.getInstance().push(new message("[Dolfini Locker::Change Mode Enter][Sucess] ", type_msg.CL_FILE_LOG_AND_CONSOLE));

                    // Cmd update Mode Enter[locker]
                    snmdb.NormalManagerDB.getInstance().add(2,
                        new CmdUpdateDolfiniLockerMode(_session.m_pi.uid, locker),
                        SQLDBResponse, this);
                }

                packet_func.session_send(p,
                    _session, 1);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::ChangeDolfiniLockerModeEnter][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x173);

                p.WriteUInt32((ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) == STDA_ERROR_TYPE.CHANNEL) ? ExceptionError.STDA_SYSTEM_ERROR_DECODE(e.getCodeError()) : 5100250);

                packet_func.session_send(p,
                    _session, 1);
            }
        }

        public void requestDolfiniLockerItem(Player _session, packet _packet)
        {
            //

            PangyaBinaryWriter p = new PangyaBinaryWriter();

            try
            {

                // Dolfini Locker Limite de Item(ns) por página

                uint opt = _packet.ReadUInt32();
                ushort pagina = _packet.ReadUInt16();





                ushort paginas = 0;
                int index = 0;
                byte count = 0;

                //if (opt == 0x63)

                var num_item = _session.m_pi.df.v_item.Count;

                paginas = (ushort)((num_item % DL_LIMIT_ITEM_PER_PAGE == 0) ? (ushort)num_item / DL_LIMIT_ITEM_PER_PAGE : (ushort)num_item / DL_LIMIT_ITEM_PER_PAGE + 1);

                if (num_item > 0 && pagina > paginas)
                {
                    throw new exception("[Channel::requestDolfiniLockerItem][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou acessa a pagina[value=" + (pagina) + "] que nao existe. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        400, 5100300));
                }

                index = (pagina - 1) * DL_LIMIT_ITEM_PER_PAGE;

                count = (byte)(((index + DL_LIMIT_ITEM_PER_PAGE) > _session.m_pi.df.v_item.Count) ? (byte)(_session.m_pi.df.v_item.Count - index) : DL_LIMIT_ITEM_PER_PAGE);

                p.init_plain(0x16D);

                p.WriteUInt16(paginas);
                p.WriteUInt16((num_item > 0) ? pagina : 0); // Para não da erro no projectg por que não tem nenhum página, por que não tem nenhum item
                p.WriteByte(count);

                for (var i = index; i < (index + count); ++i)
                {
                    p.WriteUInt64(_session.m_pi.df.v_item[i].index);
                    p.WriteBytes(_session.m_pi.df.v_item[i].item.ToArray());
                }

                packet_func.session_send(p,
                    _session, 1);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestDolfiniLockerItem][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x16D);

                p.WriteZeroByte(5); // 2 páginas, 2 página, 1 count item(ns)

                packet_func.session_send(p,
                    _session, 1);
            }
        }

        public void requestDolfiniLockerPang(Player _session, packet _packet)
        {
            //

            PangyaBinaryWriter p = new PangyaBinaryWriter();

            try
            {





                p.init_plain(0x172);

                p.WriteUInt64(_session.m_pi.df.pang);

                packet_func.session_send(p,
                    _session, 1);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestDolfiniLockerPang][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x172);

                p.WriteUInt64(0); // Pangs

                packet_func.session_send(p,
                    _session, 1);
            }
        }

        public void requestUpdateDolfiniLockerPang(Player _session, packet _packet)
        {
            //

            PangyaBinaryWriter p = new PangyaBinaryWriter();

            try
            {

                byte opt = _packet.ReadUInt8();
                ulong pang = _packet.ReadUInt64();





                if (opt == 1)
                {

                    if (pang > _session.m_pi.ui.pang)
                    {
                        throw new exception("[Channel::requestUpdateDolfiniLockerPang][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou colocar pang(s)[value=" + (pang) + "] que ele nao tem no Dolfini Locker. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                            451, 5100352));
                    }

                    _session.m_pi.df.pang += pang;

                    _session.m_pi.consomePang(pang);

                }
                else if (opt == 0)
                {

                    if (pang > _session.m_pi.df.pang)
                    {
                        throw new exception("[Channel::requestUpdateDolfiniLockerPang][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou tirar pang(s)[value=" + (pang) + "] que ele nao tem no Dolfini Locker. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                            452, 5100353));
                    }

                    _session.m_pi.df.pang -= pang;

                    _session.m_pi.addPang(pang);

                }
                else
                {
                    throw new exception("[Channel::requestUpdateDolfiniLockerPang][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou colocar ou tirar pangs do Dolfini Locker com um opt[value=" + ((ushort)opt) + "] desconhecide. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        450, 5100351));
                }

                _smp.message_pool.getInstance().push(new message("[Dolfini Locker::Update pang][Sucess] PLAYER [UID=" + (_session.m_pi.uid) + "] Atualizou Pang[value=" + (pang) + ", OPTION=" + ((ushort)opt) + "] com sucesso.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Cmd update pang do dolfini locker do player no DB
                snmdb.NormalManagerDB.getInstance().add(3,
                    new CmdUpdateDolfiniLockerPang(_session.m_pi.uid, _session.m_pi.df.pang),
                    SQLDBResponse, this);

                p.init_plain(0x171);

                p.WriteUInt32(0);

                packet_func.session_send(p,
                    _session, 1);

                p.init_plain(0xC8);

                p.WriteUInt64(_session.m_pi.ui.pang);
                p.WriteUInt64(pang);

                packet_func.session_send(p,
                    _session, 1);

                p.init_plain(0x172);

                p.WriteUInt64(_session.m_pi.df.pang);

                packet_func.session_send(p,
                    _session, 1);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestUpdateDolfiniLockerPang][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x171);

                p.WriteUInt32((ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) == STDA_ERROR_TYPE.CHANNEL) ? ExceptionError.STDA_SYSTEM_ERROR_DECODE(e.getCodeError()) : 5100350);

                packet_func.session_send(p,
                    _session, 1);
            }
        }

        public void requestAddDolfiniLockerItem(Player _session, packet _packet)
        {
            //

            PangyaBinaryWriter p = new PangyaBinaryWriter();

            DolfiniLockerItem[] aTI = null;

            try
            {

#if RELEASE
        			_smp.message_pool.getInstance().push(new message("Packet[ID=0xCE] Hex.\n\r" + _packet.Log(), type_msg.CL_FILE_LOG_AND_CONSOLE));
#endif // RELEASE

                byte count = _packet.ReadUInt8();
                aTI = new DolfiniLockerItem[count];

                for (int index = 0; index < count; index++)
                    aTI[index] = new DolfiniLockerItem().ToRead(_packet);






                uint char_typeid = 0;
                uint i = 0;

                var r = m_rm.findRoom((short)_session.m_pi.mi.sala_numero);

                for (i = 0; i < count; ++i)
                {

                    // Verifica se o player está com shop aberto e se está vendendo o item no shop


                    if (r != null && r.checkPersonalShopItem(_session, (int)aTI[i].item.id))
                    {
                        throw new exception("[Channel::requestAddDolfiniLockerItem][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou colocar o item[TYPEID=" + (aTI[i].item._typeid) + ", ID=" + (aTI[i].item.id) + "] no Dolfini Locker, mas o item esta sendo vendido no Personal shop dele. Hacker ou Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                            1010, 0x5201010));
                    }

                    if (sIff.getInstance().getItemGroupIdentify(aTI[i].item._typeid) != IFF_GROUP.PART)
                    {
                        throw new exception("[Channel::requestAddDolfiniLockerItem][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou colocar um item[TYPEID=" + (aTI[i].item._typeid) + "] no Dolfini Locker que nao é um IFF::PART.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                            500, 109));
                    }

                    var part = sIff.getInstance().findPart(aTI[i].item._typeid);

                    if (part == null)
                    {
                        throw new exception("[Channel::requestAddDolfiniLockerItem][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou colocar um item[TYPEID=" + (aTI[i].item._typeid) + ", ID=" + (aTI[i].item.id) + "] no Dolfini Locker que nao tem no IFF_STRUCT do server. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                            504, 5100405));
                    }

                    if (part.type_item == PART_TYPE.UCC_DRAW_ONLY || part.type_item == PART_TYPE.UCC_COPY_ONLY) // Não pode colocar o part original[value=8] e nem cópia[value=9]
                    {
                        throw new exception("[Channel::requestAddDolfiniLockerItem][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou colocar um Self Design Original/Copy item[TYPEID=" + (aTI[i].item._typeid) + ", ID=" + (aTI[i].item.id) + "] no Dolfini Locker, mas nao é permitido", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                            505, 5100406));
                    }

                    char_typeid = ((Convert.ToUInt32(sIff.getInstance().CHARACTER) << 26) | sIff.getInstance().getItemCharIdentify(aTI[i].item._typeid));

                    var character = _session.m_pi.findCharacterByTypeid(char_typeid);

                    if (character != null)
                    {
                        var part_num = sIff.getInstance().getItemCharPartNumber(aTI[i].item._typeid);

                        // Aqui alguns Sub Def Part é um part número a+ do certo dele, mas acho que o item feito não tem isso
                        if (character.parts_id[part_num] == aTI[i].item.id && character.parts_typeid[part_num] == aTI[i].item._typeid)
                        {
                            throw new exception("[Channel::requestAddDolfiniLockerItem][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou colocar um item[TYPEID=" + (aTI[i].item._typeid) + ", ID=" + (aTI[i].item.id) + "] equipado Part[num=" + (part_num) + "] no Dolfini Locker. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                501, 5100402));
                        }
                    }

                    // Tira do v_wi, Warehouse Item
                    //var ii = VECTOR_FIND_ITEM(_session.m_pi.v_wi, id, ==, aTI[i].item.id);
                    var it = _session.m_pi.findWarehouseItemById(aTI[i].item.id);

                    if (it == _session.m_pi.mp_wi.end().Value)
                    {
                        throw new exception("[Channel::reuqestAddDolfiniLockerItem][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou colocar um item[TYPEID=" + (aTI[i].item._typeid) + ", ID=" + (aTI[i].item.id) + "] no Dolfini Locker que ele nao tem. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                            502, 5100403));
                    }

                    // cmd add Item no Dolfini Locker do player
                    CmdAddDolfiniLockerItem cmd_adli = new CmdAddDolfiniLockerItem(_session.m_pi.uid, // Waiter
                        aTI[i]);

                    snmdb.NormalManagerDB.getInstance().add(0,
                        cmd_adli, null, null);

                    if (cmd_adli.getException().getCodeError() != 0)
                    {
                        _smp.message_pool.getInstance().push(new message("[Channel::requestAddDolfiniLockerItem][Error] " + cmd_adli.getException().getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                        if (i < (count - 1u))
                        {
                            aTI[i] = aTI[i + 1];
                        }

                        i--;
                        count--;

                        continue;
                    }

                    aTI[i] = cmd_adli.getInfo();

                    if (aTI[i].index == ~0Ul)
                    {
                        _smp.message_pool.getInstance().push(new message("[Channel::requestAddDolfiniLockerItem][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] nao conseguiu add o item[TYPEID=" + (aTI[i].item._typeid) + ", ID=" + (aTI[i].item.id) + "] no Dolfini Locker no DB", type_msg.CL_FILE_LOG_AND_CONSOLE));

                        if (i < (count - 1u))
                        {
                            aTI[i] = aTI[i + 1];
                        }

                        i--;
                        count--;

                        continue;
                    }

                    // tira o item do warehouse item List do player
                    _session.m_pi.mp_wi.Remove(it.id);

                    _session.m_pi.df.v_item.Add(aTI[i]);

                    _smp.message_pool.getInstance().push(new message("[Dolfini Locker::AddItem][Sucess] PLAYER [UID=" + (_session.m_pi.uid) + "] Adicionou Item[TYPEID=" + (aTI[i].item._typeid) + ", ID=" + (aTI[i].item.id) + "] no Dolfini Locker com sucesso", type_msg.CL_FILE_LOG_AND_CONSOLE));
                }



                if (count == 0)
                {
                    throw new exception("[Channel::requestAddDolfiniLockerItem][Error] nenhum item passou nas verificacoes, PLAYER [UID=" + (_session.m_pi.uid) + "]", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        503, 5100404));
                }

                p.init_plain(0x139);

                p.WriteUInt16(0);

                packet_func.session_send(p,
                    _session, 1);

                p.init_plain(0xEC);

                p.WriteUInt32(count);

                p.WriteByte(1); // Add Item no Dolfini Locker

                p.WriteUInt64(0); // Pang add para o player

                p.WriteUInt32(0); // Unknown, ainda não sei que membro é esse da estrutura

                for (i = 0; i < count; ++i)
                {
                    p.WriteBytes(aTI[i].item.ToArray());
                }

                packet_func.session_send(p,
                    _session, 1);

                for (i = 0; i < count; ++i)
                {
                    p.init_plain(0x16E);

                    p.WriteUInt32(0); // opt[Error Code]

                    p.WriteUInt64(0);

                    p.WriteBytes(aTI[i].item.ToArray());

                    packet_func.session_send(p,
                        _session, 1);
                }

                if (aTI != null)
                {
                    aTI = null;
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestAddDolfiniLockerItem][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x16E);

                p.WriteUInt32((ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) == STDA_ERROR_TYPE.CHANNEL) ? ExceptionError.STDA_SYSTEM_ERROR_DECODE(e.getCodeError()) : 5100400);

                packet_func.session_send(p,
                    _session, 1);

                if (aTI != null)
                {
                    aTI = null;
                }
            }
        }

        public void requestRemoveDolfiniLockerItem(Player _session, packet _packet)
        {
            //

            PangyaBinaryWriter p = new PangyaBinaryWriter();

            DolfiniLockerItem[] aTI = null;
            WarehouseItemEx[] aWi = null;

            try
            {

#if RELEASE
        			_smp.message_pool.getInstance().push(new message("Packet[ID=0xCF] Hex.\n\r" + _packet.Log(), type_msg.CL_FILE_LOG_AND_CONSOLE));
#endif // RELEASE

                byte count = _packet.ReadUInt8();
                aTI = new DolfiniLockerItem[count];
                aWi = new WarehouseItemEx[count];

                for (int index = 0; index < count; index++)
                    aTI[index] = new DolfiniLockerItem().ToRead(_packet);

                uint i = 0;

                for (i = 0; i < count; ++i)
                {

                    // Encontra o índice do item com ID correspondente
                    var ii = _session.m_pi.df.v_item.FirstOrDefault(item => item.item.id == aTI[i].item.id);


                    if (ii == null && ii.index == 0)
                    {
                        throw new exception("[Channel::reuqestRemoveDolfiniLockerItem][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou tirar um item[TYPEID=" + (aTI[i].item._typeid) + ", ID=" + (aTI[i].item.id) + "] que ele nao tem. Do Dolfini Locker ", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                            550, 5100451));
                    }

                    // cmd remove Item no Dolfini Locker do player
                    snmdb.NormalManagerDB.getInstance().add(4,
                        new CmdDeleteDolfiniLockerItem(_session.m_pi.uid, aTI[i].index),
                        SQLDBResponse, this);

                    // tira o item do Dolfini Locker item List do player
                    _session.m_pi.df.v_item.Remove(ii);
                    aWi[i].clear();
                    aWi[i].id = aTI[i].item.id;
                    aWi[i]._typeid = aTI[i].item._typeid;
                    aWi[i].ano = -1;
                    aWi[i].STDA_C_ITEM_QNTD = 1; // Pode ser os stats da roupa msm, qntd de pwr, ctrl, spin e etc
                    aWi[i].purchase = 1;
                    aWi[i].type = 2;
                    aWi[i].clubset_workshop.level = -1;

                    // UCC
                    aWi[i].ucc.name = aTI[i].item.sd_name;
                    aWi[i].ucc.idx = aTI[i].item.sd_idx;
                    aWi[i].ucc.copier_nick = aTI[i].item.sd_copier_nick;

                    aWi[i].ucc.seq = aTI[i].item.sd_seq;
                    aWi[i].ucc.status = (byte)aTI[i].item.sd_status;

                    _session.m_pi.mp_wi.Add(aWi[i].id, aWi[i]);

                    _smp.message_pool.getInstance().push(new message("[Dolfini Locker::RemoveItem][Sucess] PLAYER [UID=" + (_session.m_pi.uid) + "] removeu o Item[TYPEID=" + (aWi[i]._typeid) + ", ID=" + (aWi[i].id) + "] do Dolfini Locker com sucesso", type_msg.CL_FILE_LOG_AND_CONSOLE));
                }

                if (count == 0)
                {
                    throw new exception("[Channel::requestRemoveDolfiniLockerItem][Error] nenhum item passou nas verificacoes, PLAYER [UID=" + (_session.m_pi.uid) + "]", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        503, 5100404));
                }

                p.init_plain(0xEC);

                p.WriteUInt32(count);

                p.WriteByte(0); // Remove Item no Dolfini Locker

                p.WriteUInt64(_session.m_pi.ui.pang); // Pang add para o player

                p.WriteUInt32(0); // Unknown, ainda não sei o que é esse membro na estrutura

                for (i = 0; i < count; ++i)
                {

                    p.WriteBytes(aTI[i].item.ToArray());//é 168 bytes

                    p.WriteByte(3);

                    p.WriteBytes(aWi[i].ToArray());
                }

                packet_func.session_send(p,
                    _session, 1);

                for (i = 0; i < count; ++i)
                {
                    p.init_plain(0x16F);

                    p.WriteUInt32(0); // opt[Error Code]

                    p.WriteUInt64(aTI[i].index);
                    p.WriteBytes(aTI[i].item.ToArray());

                    packet_func.session_send(p,
                        _session, 1);
                }

                if (aTI != null)
                {
                    aTI = null;
                }

                if (aWi != null)
                {
                    aWi = null;
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestRemoveDolfiniLockerItem][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x16F);

                p.WriteUInt32((ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) == STDA_ERROR_TYPE.CHANNEL) ? ExceptionError.STDA_SYSTEM_ERROR_DECODE(e.getCodeError()) : 5100450);

                packet_func.session_send(p,
                    _session, 1);

                if (aTI != null)
                {
                    aTI = null;
                }

                if (aWi != null)
                {
                    aWi = null;
                }
            }
        }

        public void requestOpenLegacyTikiShop(Player _session, packet _packet)
        {
            //

            PangyaBinaryWriter p = new PangyaBinaryWriter();

            try
            {

#if RELEASE
        			// Log
        			_smp.message_pool.getInstance().push(new message("[Channel::requestOpenLegacyTikiShop][Sucess] PLAYER [UID=" + (_session.m_pi.uid) + "] request open Point Shop(Tiki Shop antigo).", type_msg.CL_FILE_LOG_AND_CONSOLE));

        			_smp.message_pool.getInstance().push(new message("[Channel::requestOpenLegacyTikiShop][Sucess] PLAYER [UID=" + (_session.m_pi.uid) + "]. Packet raw: " + _packet.Log(), type_msg.CL_FILE_LOG_AND_CONSOLE));
#endif // RELEASE





                if (_session.m_pi.block_flag.m_flag.legacy_tiki_shop)
                {
                    throw new exception("[Channel::requestOpenLegacyTikiShop][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] esta bloqueado no Legacy Tiki Shop.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        4000, 1));
                }

                p.init_plain(0x1E7);

                p.WriteUInt32(0u); // OK

                packet_func.session_send(p,
                    _session, 1);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestOpenLegacyTikiShop][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x1E7);

                p.WriteUInt32((ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) == STDA_ERROR_TYPE.CHANNEL) ? ExceptionError.STDA_SYSTEM_ERROR_DECODE(e.getCodeError()) : 1u);

                packet_func.session_send(p,
                    _session, 1);
            }
        }

        public void requestPointLegacyTikiShop(Player _session, packet _packet)
        {
            //

            PangyaBinaryWriter p = new PangyaBinaryWriter();

            try
            {

#if RELEASE
        			// Log
        			_smp.message_pool.getInstance().push(new message("[Channel::requestPointLegacyTikiShop][Sucess] PLAYER [UID=" + (_session.m_pi.uid) + "] request TP from Point Shop(Tiki Shop antigo).", type_msg.CL_FILE_LOG_AND_CONSOLE));

        			_smp.message_pool.getInstance().push(new message("[Channel::requestPointLegacyTikiShop][Sucess] PLAYER [UID=" + (_session.m_pi.uid) + "]. Packet raw: " + _packet.Log(), type_msg.CL_FILE_LOG_AND_CONSOLE));
#endif // RELEASE





                if (_session.m_pi.block_flag.m_flag.legacy_tiki_shop)
                {
                    throw new exception("[Channel::requestOpenLerequestPointLegacyTikiShopgacyTikiShop][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] esta bloqueado no Legacy Tiki Shop.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        4000, 1));
                }

                p.init_plain(0x1E8);

                p.WriteUInt32(0u); // OK

                p.WriteUInt32((uint)_session.m_pi.m_legacy_tiki_pts);

                packet_func.session_send(p,
                    _session, 1);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestPointLegacyTikiShop][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x1E8);

                p.WriteUInt32((ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) == STDA_ERROR_TYPE.CHANNEL) ? ExceptionError.STDA_SYSTEM_ERROR_DECODE(e.getCodeError()) : 1u);

                packet_func.session_send(p,
                    _session, 1);
            }
        }

        public void requestExchangeTPByItemLegacyTikiShop(Player _session, packet _packet)
        {
            //


            PangyaBinaryWriter p = new PangyaBinaryWriter();

            try
            {

#if RELEASE
        			// Log
        			_smp.message_pool.getInstance().push(new message("[Channel::requestExchangeTPByItemLegacyTikiShop][Sucess] PLAYER [UID=" + (_session.m_pi.uid) + "] request exchange TP by Item in Point Shop(Tiki Shop antigo).", type_msg.CL_FILE_LOG_AND_CONSOLE));

        			_smp.message_pool.getInstance().push(new message("[Channel::requestExchangeTPByItemLegacyTikiShop][Sucess] PLAYER [UID=" + (_session.m_pi.uid) + "]. Packet raw: " + _packet.Log(), type_msg.CL_FILE_LOG_AND_CONSOLE));
#endif // RELEASE





                if (_session.m_pi.block_flag.m_flag.legacy_tiki_shop)
                {
                    throw new exception("[Channel::requestExchangeTPByItemLegacyTikiShop][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] esta bloqueado no Legacy Tiki Shop.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        4000, 1));
                }

                Func<IFFTikiShopData, (uint, uint)> getNumberItensPerTikiShopPts = (_tiki) =>
                {
                    uint itemCount = (_tiki.Tiki_Qnt_Pts == 0u) ? 1u : _tiki.Tiki_Qnt_Pts;
                    uint tikiPoints = (_tiki.Tiki_Pts == 0u) ? 1u : _tiki.Tiki_Pts;

                    return (itemCount, tikiPoints);
                };


                uint tiki_pts = 0;

                // Log String Item
                string s_item = "";

                stLegacyTikiShopExchangeItem tsei = new stLegacyTikiShopExchangeItem();

                List<stItem> v_item = new List<stItem>();
                stItem item = new stItem();

                // Achievement System
                AchievementSystem sys_achieve = new AchievementSystem();

                uint count = _packet.ReadUInt8();

                var r = m_rm.findRoom((short)_session.m_pi.mi.sala_numero);

                for (var i = 0; i < count; ++i)
                {
                    tsei = new stLegacyTikiShopExchangeItem().ToRead(_packet);

                    var @base = sIff.getInstance().findCommomItem(tsei._typeid);

                    if (@base == null)
                    {
                        throw new exception("[Channel::ExchangeTPByItemLegacyTikiShop][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou trocar item[TYPEID=" + (tsei._typeid) + ", ID=" + (tsei.id) + "] no Tiki's Shop, mas o item nao existe no IFF_STRUCT do Server. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                            901, 0x5200902));
                    }

                    if (!@base.tiki.IsActived())
                    {
                        throw new exception("[Channel::ExchangeTPByItemLegacyTikiShop][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou trocar item[TYPEID=" + (tsei._typeid) + ", ID=" + (tsei.id) + "] no Tiki's Shop, mas o item nao é valido para ser trocado. Hacker ou Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                            904, 0x5200905));
                    }

                    var dados_tiki = getNumberItensPerTikiShopPts(@base.tiki);

                    var _item = ItemManager.exchangeTikiShop(_session,
                        tsei._typeid, tsei.id,
                        (uint)(dados_tiki.Item1 * tsei.qntd));

                    if (_item.empty())
                    {
                        throw new exception("[Channel::ExchangeTPByItemLegacyTikiShop][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou trocar item[TYPEID=" + (tsei._typeid) + ", ID=" + (tsei.id) + ", QNTD=" + (tsei.qntd) + "] no Tiki's Shop, mas nao conseguiu inicializar o item. Hacker ou Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                            900, 0x52000901));
                    }

                    if (r != null && r.checkPersonalShopItem(_session, tsei.id))
                    {
                        throw new exception("[Channel::ExchangeTPByItemLegacyTikiShop][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou trocar item[TYPEID=" + (tsei._typeid) + ", ID=" + (tsei.id) + ", QNTD=" + (tsei.qntd) + "] no Tiki's Shop, mas o item esta sendo vendido no Personal shop dele. Hacker ou Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                            1010, 0x5201010));
                    }

                    // Soma dados de tiki dos itens
                    tiki_pts += (uint)(dados_tiki.Item2 * tsei.qntd);

                    v_item.AddRange(_item);
                }



                if (tiki_pts == 0u)
                {
                    throw new exception("[Channel::ExchangeTPByItemLegacyTikiShop][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou trocar item(ns)(" + s_item + "), mas ocorreu um erro na inicializacao do Tiki Points from IFF_STRUCT is invalid(" + (tiki_pts) + ").", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        905, 0x5200905));
                }

                // Remove Item(ns)
                if (ItemManager.removeItem(v_item, _session) <= 0)
                {
                    throw new exception("[Channel::ExchangeTPByItemLegacyTikiShop][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou trocar item(ns)(" + s_item + "), mas nao conseguiu deletar ele(s).", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        902, 0x5200903));
                }

                // Tiki Points
                _session.m_pi.m_legacy_tiki_pts += tiki_pts;

                snmdb.NormalManagerDB.getInstance().add(28,
                    new CmdUpdateLegacyTikiShopPoint(_session.m_pi.uid, _session.m_pi.m_legacy_tiki_pts),
                    SQLDBResponse, this);

                // Achievement Add 1 valor de Exchange Legacy Tiki Shop ao contador
                sys_achieve.incrementCounter(0x6C400086u, 1);

                // Log
                // _smp.message_pool.getInstance().push(new message("[ExchangeTPByItemLegacyTikiShop][Sucess] PLAYER [UID=" + (_session.m_pi.uid) + "] player trocou item(ns)(" + s_item + ") por Tiki Point[value=" + (tiki_pts) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Att Item ON Jogo
                p.init_plain(0x216);

                p.WriteUInt32((uint)UtilTime.GetSystemTimeAsUnix());
                p.WriteUInt32((uint)v_item.Count);

                foreach (var el in v_item)
                {
                    p.WriteByte(el.type);
                    p.WriteUInt32(el._typeid);
                    p.WriteInt32(el.id);
                    p.WriteUInt32(el.flag_time);
                    p.WriteBytes(el.stat.ToArray());
                    p.WriteInt32((el.STDA_C_ITEM_TIME > 0) ? el.STDA_C_ITEM_TIME : el.STDA_C_ITEM_QNTD);
                    p.WriteZeroByte(25); // 10 PCL[C0~C4] 2 Bytes cada, 15 bytes desconhecido
                }

                packet_func.session_send(p,
                    _session, 1);

                // Reply
                p.init_plain(0x1E9);

                p.WriteUInt32(0u); // OK
                p.WriteUInt32((uint)_session.m_pi.m_legacy_tiki_pts);

                packet_func.session_send(p,
                    _session, 1);

                // UPDATE Achievement ON SERVER, DB and GAME
                sys_achieve.finish_and_update(_session);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestExchangeTPByItemLegacyTikiShop][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x1E9);

                p.WriteUInt32((ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) == STDA_ERROR_TYPE.CHANNEL) ? ExceptionError.STDA_SYSTEM_ERROR_DECODE(e.getCodeError()) : 1u);

                packet_func.session_send(p,
                    _session, 1);
            }
        }

        public void requestExchangeItemByTPLegacyTikiShop(Player _session, packet _packet)
        {
            //


            PangyaBinaryWriter p = new PangyaBinaryWriter();

            try
            {

#if RELEASE
        			// Log
        			_smp.message_pool.getInstance().push(new message("[Channel::requestExchangeItemByTPLegacyTikiShop][Sucess] PLAYER [UID=" + (_session.m_pi.uid) + "] request exchange Item By TP in Point Shop(Tiki Shop antigo).", type_msg.CL_FILE_LOG_AND_CONSOLE));

        			_smp.message_pool.getInstance().push(new message("[Channel::requestExchangeItemByTPLegacyTikiShop][Sucess] PLAYER [UID=" + (_session.m_pi.uid) + "]. Packet raw: " + _packet.Log(), type_msg.CL_FILE_LOG_AND_CONSOLE));
#endif // RELEASE





                if (_session.m_pi.block_flag.m_flag.legacy_tiki_shop)
                {
                    throw new exception("[Channel::requestExchangeItemByTPLegacyTikiShop][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] esta bloqueado no Legacy Tiki Shop.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        4000, 1));
                }

                uint tiki_pts = 0;

                // Log String Item
                string s_item = "";

                stLegacyTikiShopExchangeTP tsetp = new stLegacyTikiShopExchangeTP();

                List<stItem> v_item = new List<stItem>();
                stItem item = new stItem();
                BuyItem bi = new BuyItem();

                // Achievement System
                AchievementSystem sys_achieve = new AchievementSystem();

                uint count = _packet.ReadUInt8();

                for (var i = 0; i < count; ++i)
                {

                    tsetp = new stLegacyTikiShopExchangeTP().ToRead(_packet);

                    var @base = sIff.getInstance().findCommomItem(tsetp._typeid);

                    if (@base == null)
                    {
                        throw new exception("[Channel::requestExchangeItemByTPLegacyTikiShop][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou trocar item[TYPEID=" + (tsetp._typeid) + "] no Tiki's Shop, mas o item nao existe no IFF_STRUCT do Server. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                            901, 0x5200902));
                    }

                    var point_shop = sIff.getInstance().findPointShop(tsetp._typeid);

                    if (point_shop == null)
                    {
                        throw new exception("[Channel::requestExchangeItemByTPLegacyTikiShop][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou trocar item[TYPEID=" + (tsetp._typeid) + "] no Tiki's Shop, mas o item nao existe no IFF_STRUCT(PointShop) do Server. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                            901, 0x5200902));
                    }

                    // Tiki Pts que vai ser gasto para trocar pelo item
                    tiki_pts += (uint)(point_shop.Points * tsetp.qntd);

                    bi.id = -1;
                    bi._typeid = tsetp._typeid;
                    bi.qntd = (uint)(point_shop.Quantity * tsetp.qntd);

                    ItemManager.initItemFromBuyItem(_session.m_pi,
                        item, bi, false, 0, 0, 1);

                    if (item._typeid == 0)
                    {
                        throw new exception("[Channel::requestExchangeItemByTPLegacyTikiShop][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou trocar Item[TYPEID=" + (bi._typeid) + ", QNTD=" + (bi.qntd) + "], mas nao conseguiu inicializar o item. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                            901, 0x5200902));
                    }

                    v_item.Add(new stItem(item));

                    // Log
                    // s_item += ((i == 0) ? "" : ", ") + "[TYPEID=" + (tsetp._typeid) + ", QNTD=" + (tsetp.qntd) + ", QNTD_REAL=" + (point_shop.Quantity * tsetp.qntd) + "]";
                }

                if (tiki_pts == 0u)
                {
                    throw new exception("[Channel::requestExchangeItemByTPLegacyTikiShop][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou trocar item(ns)(" + s_item + "), mas ocorreu um erro na inicializacao do Tiki Points from IFF_STRUCT is invalid(" + (tiki_pts) + ").", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        905, 0x5200905));
                }

                if (tiki_pts > _session.m_pi.m_legacy_tiki_pts)
                {
                    throw new exception("[Channel::requestExchangeItemByTPLegacyTikiShop][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou trocar item(ns)(" + s_item + "), mas o player nao tem tiki_pts suficiente para a troca[HAVE=" + (_session.m_pi.m_legacy_tiki_pts) + ", REQUEST=" + (tiki_pts) + "].", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        906, 0x5200906));
                }

                // Update tiki points no server
                _session.m_pi.m_legacy_tiki_pts -= tiki_pts;

                // Att no banco de dados
                snmdb.NormalManagerDB.getInstance().add(28,
                new CmdUpdateLegacyTikiShopPoint(_session.m_pi.uid, _session.m_pi.m_legacy_tiki_pts),
                    SQLDBResponse, this);

                // Add os itens
                var rai = ItemManager.addItem(v_item,
                    _session.getUID(), 0, 0);

                if (rai.fails.Count > 0 && rai.type != ItemManager.RetAddItem.T_SUCCESS_PANG_AND_EXP_AND_CP_POUCH)
                {


                    StringBuilder str = new StringBuilder();

                    for (int i = 0; i < rai.fails.Count; ++i)
                    {
                        var fail = rai.fails[i];

                        if (i > 0)
                            str.Append(", ");

                        str.Append("[TYPEID=")
                           .Append(fail._typeid)
                           .Append(", ID=")
                           .Append(fail.id)
                           .Append(", QNTD=")
                           .Append((fail.qntd > 0xFFu) ? fail.qntd : fail.STDA_C_ITEM_QNTD);

                        if (fail.STDA_C_ITEM_TIME > 0)
                            str.Append(", TEMPO=").Append(fail.STDA_C_ITEM_TIME);

                        str.Append("]");
                    }

                    // Aqui depois especifica cada um separado para manda mensagem
                    throw new exception("[Channel::requestExchangeItemByTPLegacyTikiShop][Error] Itens que falhou ao add os itens que o PLAYER [UID=" + (_session.m_pi.uid) + "] trocou item(ns){" + str.ToString() + "}. Hacker ou bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        907, 0x5200907));
                }

                // Achievement Add 1 valor de Exchange Legacy Tiki Shop ao contador
                sys_achieve.incrementCounter(0x6C400086u, 1);

                // Log
                // _smp.message_pool.getInstance().push(new message("[Channel::requestExchangeItemByTPLegacyTikiShop][Sucess] PLAYER [UID=" + (_session.m_pi.uid) + "] trocou Tiki Points[TP=" + (tiki_pts) + "] por Item(ns)(" + s_item + ").", type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Att Item ON Jogo
                p.init_plain(0x216);

                p.WriteUInt32((uint)UtilTime.GetSystemTimeAsUnix());
                p.WriteUInt32((uint)v_item.Count);

                foreach (var el in v_item)
                {
                    p.WriteByte(el.type);
                    p.WriteUInt32(el._typeid);
                    p.WriteInt32(el.id);
                    p.WriteUInt32(el.flag_time);
                    p.WriteBytes(el.stat.ToArray());
                    p.WriteInt32((el.STDA_C_ITEM_TIME > 0) ? el.STDA_C_ITEM_TIME : el.STDA_C_ITEM_QNTD);
                    p.WriteZeroByte(25); // 10 PCL[C0~C4] 2 Bytes cada, 15 bytes desconhecido
                }

                packet_func.session_send(p,
                    _session, 1);

                // Reply
                p.init_plain(0x1EA);

                p.WriteUInt32(0u); // OK
                p.WriteUInt32((uint)_session.m_pi.m_legacy_tiki_pts);

                packet_func.session_send(p,
                    _session, 1);

                // UPDATE Achievement ON SERVER, DB and GAME
                sys_achieve.finish_and_update(_session);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestExchangeItemByTPLegacyTikiShop][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x1EA);

                p.WriteUInt32((ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) == STDA_ERROR_TYPE.CHANNEL) ? ExceptionError.STDA_SYSTEM_ERROR_DECODE(e.getCodeError()) : 1u);

                packet_func.session_send(p,
                    _session, 1);
            }
        }

        public void requestOpenEditSaleShop(Player _session, packet _packet)
        {
            //

            try
            {






                var r = m_rm.findRoom((short)_session.m_pi.mi.sala_numero);

                if (r != null)
                {

                    // Aqui ou lá dentro verifica se o Personal Shop está bloqueado no shop ou para o player, para poder bloquear
                    r.requestOpenEditSaleShop(_session, _packet);

                }
                else
                {
                    // não aqui mas no else tem que retornar erro para o cliente, que ele esta tentando Fechar um Personal Shop, mas ele nao esta em nenhum sala
                    // Isso é Hacker ou Bug
                    _smp.message_pool.getInstance().push(new message("[Channel::requestOpenEditSaleShop][Error][WARNIG] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou abrir ou editar um/o personal shop para ele, mas nao esta em nenhum sala[numero=" + (_session.m_pi.mi.sala_numero) + "]. Hacker ou Bug [Tem que enviar a resposta para o cliente, por que ainda nao esta enviando]", type_msg.CL_FILE_LOG_AND_CONSOLE));
                }



            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestOpenEditSaleShop][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if (ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) != STDA_ERROR_TYPE.ROOM)
                {
                    throw;
                }
            }
        }

        public void requestCloseSaleShop(Player _session, packet _packet)
        {
            //

            try
            {






                var r = m_rm.findRoom((short)_session.m_pi.mi.sala_numero);

                if (r != null)
                {
                    r.requestCloseSaleShop(_session, _packet);
                }
                else
                {
                    // não aqui mas no else tem que retornar erro para o cliente, que ele esta tentando Fechar um Personal Shop, mas ele nao esta em nenhum sala
                    // Isso é Hacker ou Bug
                    _smp.message_pool.getInstance().push(new message("[Channel::requestCloseSaleShop][Error][WARNIG] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou deletar um personal shop dele, mas nao esta em nenhum sala[numero=" + (_session.m_pi.mi.sala_numero) + "]. Hacker ou Bug [Tem que enviar a resposta para o cliente, por que ainda nao esta enviando]", type_msg.CL_FILE_LOG_AND_CONSOLE));
                }



            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestCloseSaleShop][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if (ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) != STDA_ERROR_TYPE.ROOM)
                {
                    throw;
                }
            }
        }

        public void requestChangeNameSaleShop(Player _session, packet _packet)
        {
            //

            try
            {






                var r = m_rm.findRoom((short)_session.m_pi.mi.sala_numero);

                if (r != null)
                {
                    r.requestChangeNameSaleShop(_session, _packet);
                }
                else
                {
                    // não aqui mas no else tem que retornar erro para o cliente, que ele esta tentando Fechar um Personal Shop, mas ele nao esta em nenhum sala
                    // Isso é Hacker ou Bug
                    _smp.message_pool.getInstance().push(new message("[Channel::requestChangeNameSaleShop][Error][WARNIG] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou trocar o nome do personal shop dele. mas nao esta em nenhum sala[numero=" + (_session.m_pi.mi.sala_numero) + "]. Hacker ou Bug [Tem que enviar a resposta para o cliente, por que ainda nao esta enviando]", type_msg.CL_FILE_LOG_AND_CONSOLE));
                }



            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestChangeNameSaleShop][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if (ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) != STDA_ERROR_TYPE.ROOM)
                {
                    throw;
                }
            }
        }

        public void requestOpenSaleShop(Player _session, packet _packet)
        {
            //

            try
            {






                var r = m_rm.findRoom((short)_session.m_pi.mi.sala_numero);

                if (r != null)
                {
                    r.requestOpenSaleShop(_session, _packet);
                }
                else
                {
                    // não aqui mas no else tem que retornar erro para o cliente, que ele esta tentando Fechar um Personal Shop, mas ele nao esta em nenhum sala
                    // Isso é Hacker ou Bug
                    _smp.message_pool.getInstance().push(new message("[Channel::requestOpenSaleShop][Error][WARNIG] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou abrir o personal shop dele. mas nao esta em nenhum sala[numero=" + (_session.m_pi.mi.sala_numero) + "]. Hacker ou Bug [Tem que enviar a resposta para o cliente, por que ainda nao esta enviando]", type_msg.CL_FILE_LOG_AND_CONSOLE));
                }



            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestOpenSaleShop][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if (ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) != STDA_ERROR_TYPE.ROOM)
                {
                    throw;
                }
            }
        }

        public void requestVisitCountSaleShop(Player _session, packet _packet)
        {
            //

            try
            {






                var r = m_rm.findRoom((short)_session.m_pi.mi.sala_numero);

                if (r != null)
                {
                    r.requestVisitCountSaleShop(_session, _packet);
                }
                else
                {
                    // não aqui mas no else tem que retornar erro para o cliente, que ele esta tentando Fechar um Personal Shop, mas ele nao esta em nenhum sala
                    // Isso é Hacker ou Bug
                    _smp.message_pool.getInstance().push(new message("[Channel::requestVisitCountSaleShop][Error][WARNIG] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou pedir Visit Count do personal shop dele. mas nao esta em nenhum sala[numero=" + (_session.m_pi.mi.sala_numero) + "]. Hacker ou Bug [Tem que enviar a resposta para o cliente, por que ainda nao esta enviando]", type_msg.CL_FILE_LOG_AND_CONSOLE));
                }



            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestVisitCountSaleShop][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if (ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) != STDA_ERROR_TYPE.ROOM)
                {
                    throw;
                }
            }
        }

        public void requestPangSaleShop(Player _session, packet _packet)
        {
            //

            try
            {






                var r = m_rm.findRoom((short)_session.m_pi.mi.sala_numero);

                if (r != null)
                {
                    r.requestPangSaleShop(_session, _packet);
                }
                else
                {
                    // não aqui mas no else tem que retornar erro para o cliente, que ele esta tentando Fechar um Personal Shop, mas ele nao esta em nenhum sala
                    // Isso é Hacker ou Bug
                    _smp.message_pool.getInstance().push(new message("[Channel::requestPangSaleShop][Error][WARNIG] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou pedir Pang Sale do personal shop dele. mas nao esta em nenhum sala[numero=" + (_session.m_pi.mi.sala_numero) + "]. Hacker ou Bug [Tem que enviar a resposta para o cliente, por que ainda nao esta enviando]", type_msg.CL_FILE_LOG_AND_CONSOLE));
                }



            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestPangSaleShop][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if (ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) != STDA_ERROR_TYPE.ROOM)
                {
                    throw;
                }
            }
        }

        public void requestCancelEditSaleShop(Player _session, packet _packet)
        {
            //

            try
            {






                var r = m_rm.findRoom((short)_session.m_pi.mi.sala_numero);

                if (r != null)
                {
                    r.requestCancelEditSaleShop(_session, _packet);
                }
                else
                {
                    // não aqui mas no else tem que retornar erro para o cliente, que ele esta tentando Fechar um Personal Shop, mas ele nao esta em nenhum sala
                    // Isso é Hacker ou Bug
                    _smp.message_pool.getInstance().push(new message("[Channel::requestCancelEditSaleShop][Error][WARNIG] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou cancelar edit o personal shop dele. mas nao esta em nenhum sala[numero=" + (_session.m_pi.mi.sala_numero) + "]. Hacker ou Bug [Tem que enviar a resposta para o cliente, por que ainda nao esta enviando]", type_msg.CL_FILE_LOG_AND_CONSOLE));
                }



            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestCancelEditSaleShop][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if (ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) != STDA_ERROR_TYPE.ROOM)
                {
                    throw;
                }
            }
        }

        public void requestViewSaleShop(Player _session, packet _packet)
        {
            //

            try
            {






                var r = m_rm.findRoom((short)_session.m_pi.mi.sala_numero);

                if (r != null)
                {
                    r.requestViewSaleShop(_session, _packet);
                }
                else
                {
                    // não aqui mas no else tem que retornar erro para o cliente, que ele esta tentando Fechar um Personal Shop, mas ele nao esta em nenhum sala
                    // Isso é Hacker ou Bug
                    _smp.message_pool.getInstance().push(new message("[Channel::requestViewSaleShop][Error][WARNIG] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou ver o personal shop de outro player. mas nao esta em nenhum sala[numero=" + (_session.m_pi.mi.sala_numero) + "]. Hacker ou Bug [Tem que enviar a resposta para o cliente, por que ainda nao esta enviando]", type_msg.CL_FILE_LOG_AND_CONSOLE));
                }



            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestViewSaleShop][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if (ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) != STDA_ERROR_TYPE.ROOM)
                {
                    throw;
                }
            }
        }

        public void requestCloseViewSaleShop(Player _session, packet _packet)
        {
            //

            try
            {






                var r = m_rm.findRoom((short)_session.m_pi.mi.sala_numero);

                if (r != null)
                {
                    r.requestCloseViewSaleShop(_session, _packet);
                }
                else
                {
                    // não aqui mas no else tem que retornar erro para o cliente, que ele esta tentando Fechar um Personal Shop, mas ele nao esta em nenhum sala
                    // Isso é Hacker ou Bug
                    _smp.message_pool.getInstance().push(new message("[Channel::requestCloseViewSaleShop][Error][WARNIG] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou fechar o personal shop de outro player. mas nao esta em nenhum sala[numero=" + (_session.m_pi.mi.sala_numero) + "]. Hacker ou Bug [Tem que enviar a resposta para o cliente, por que ainda nao esta enviando]", type_msg.CL_FILE_LOG_AND_CONSOLE));
                }



            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestCloseViewSaleShop][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if (ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) != STDA_ERROR_TYPE.ROOM)
                {
                    throw;
                }
            }
        }

        public void requestBuyItemSaleShop(Player _session, packet _packet)
        {
            //

            try
            {






                var r = m_rm.findRoom((short)_session.m_pi.mi.sala_numero);

                if (r != null)
                {
                    r.requestBuyItemSaleShop(_session, _packet);
                }
                else
                {
                    // não aqui mas no else tem que retornar erro para o cliente, que ele esta tentando Fechar um Personal Shop, mas ele nao esta em nenhum sala
                    // Isso é Hacker ou Bug
                    _smp.message_pool.getInstance().push(new message("[Channel::requestBuyItemSaleShop][Error][WARNIG] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] tentou comprar no personal shop de outro player. mas nao esta em nenhum sala[numero=" + (_session.m_pi.mi.sala_numero) + "]. Hacker ou Bug [Tem que enviar a resposta para o cliente, por que ainda nao esta enviando]", type_msg.CL_FILE_LOG_AND_CONSOLE));
                }



            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestBuyItemSaleShop][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if (ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) != STDA_ERROR_TYPE.ROOM)
                {
                    throw;
                }
            }
        }

        public void requestOpenPapelShop(Player _session, packet _packet)
        {
            //

            PangyaBinaryWriter p = new PangyaBinaryWriter();

            try
            {





                // Verifica se ele pode entrar no papel shop
                // ------------- aqui o cliente não bloqueia mais por que o o memorial está junto dele, então só da erro quando vai jogar -------
                //if (_session.m_pi.block_flag.m_id_state & BLOCK_PAPEL_SHOP)
                //	throw exception("[Channel::requestOpenPapelShop][Error] PLAYER [UID=" + to_string(_session.m_pi.uid) + "] esta bloqueado para abrir o papel shop", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE::CHANNEL, 1, 0x5800101));

                p.init_plain(0x10B);

                p.WriteUInt32(0); // OK, !0 Error, aqui o cliente não bloqueia mais por que o o memorial está junto dele, então só da erro quando vai jogar

                p.WriteInt64(_session.m_pi.mi.papel_shop.limit_count); // Limite count(vezes) por dia

                packet_func.session_send(p,
                    _session, 1);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestOpenPapelShop][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x10B);

                p.WriteInt64(-1);

                p.WriteUInt32((ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) == STDA_ERROR_TYPE.CHANNEL) ? ExceptionError.STDA_SYSTEM_ERROR_DECODE(e.getCodeError()) : 0x5800100);

                packet_func.session_send(p,
                    _session, 1);
            }
        }

        public void requestPlayPapelShop(Player _session, packet _packet)
        {
            PangyaBinaryWriter p = new PangyaBinaryWriter();

            try
            {


                if (_session.m_pi.block_flag.m_flag.papel_shop)
                    throw new exception("[Channel::requestPlayPapelShop][Error] PLAYER [UID=" + (_session.m_pi.uid)
                            + "] tentou jogar no Papel Shop, mas ele nao pode. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 3, 0x790001));

                if (_session.m_pi.mi.level < 1)
                    throw new exception("[Channel::requestPlayPapelShop][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou jogar o Papel Shop Normal, mas nao tem o level necessario[level="
                            + (_session.m_pi.mi.level) + ", request=1]", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 8, 0x5900108));

                if (!sPapelShopSystem.getInstance().isLoad())
                    sPapelShopSystem.getInstance().load();

                if (sPapelShopSystem.getInstance().isLimittedPerDay() && _session.m_pi.mi.papel_shop.remain_count <= 0)
                    throw new exception("[Channel::requestPlayPapelShop][Warning] PLAYER [UID=" + (_session.m_pi.uid)
                        + "] tentou jogar o Papel Shop Normal, mas o limite por dia esta ativado, e ele nao tem mais vezes no dia ele ja chegou ao seu limite.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 1, 0x5900101));

                var coupon = sPapelShopSystem.getInstance().hasCoupon(_session);

                if ((coupon == null || coupon.STDA_C_ITEM_QNTD < 1) && _session.m_pi.ui.pang < sPapelShopSystem.getInstance().getPriceNormal())
                    throw new exception("[Channel::requestPlayPapelShop][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou jogar o Papel Shop Normal, ele nao tem Coupon e nem Pangs suficiente[value="
                            + (_session.m_pi.ui.pang) + ", request=" + (sPapelShopSystem.getInstance().getPriceNormal()) + "]. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 2, 0x5900102));

                var balls = sPapelShopSystem.getInstance().dropBalls(_session);

                if (!balls.Any())
                    throw new exception("[Channel::requestPlayPapelShop][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou jogar o Papel Shop Normal, mas nao conseguiu sortear as bolas. Bug",
                            ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 3, 0x5900103));

                List<stItem> v_item = new List<stItem>();
                stItem item = new stItem();
                BuyItem bi = new BuyItem();

                AchievementSystem sys_achieve = new AchievementSystem();

                // Reserva memória para o List, não realocar depois a cada push_back ou insert
                //   v_item.Re(balls.Count() + 1/*coupon*/);
                for (int i = 0; i < balls.Count; i++)
                {
                    var el = balls[i];
                    item = new stItem(); // Cria uma nova instância a cada iteração
                    bi = new BuyItem
                    {
                        id = -1,
                        _typeid = el.ctx_psi._typeid,
                        qntd = el.qntd
                    };

                    ItemManager.initItemFromBuyItem(_session.m_pi, item, bi, false, 0, 0, 1);

                    if (item._typeid == 0)
                    {
                        throw new exception("[Channel::requestPlayPapelShop][Error] PLAYER [UID=" + _session.m_pi.uid + "] tentou jogar o Papel Shop Normal, mas nao conseguiu inicializar o Item[TYPEID=" + bi._typeid + "]. Bug",
                            ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 4, 0x5900104));
                    }

                    var it = v_item.FirstOrDefault(el2 => el2._typeid == item._typeid);

                    if (it != null)
                    {
                        it.qntd += item.qntd;
                        it.STDA_C_ITEM_QNTD = (short)it.qntd;
                    }
                    else
                    {
                        v_item.Add(new stItem(item));
                    }
                }


                // UPDATE ON SERVER

                string ids = "";

                for (var i = 0; i < v_item.Count(); ++i)
                    ids += ((i == 0) ? ("") : (", ")) + "TYPEID=" + (v_item[i]._typeid) + ", ID=" + (v_item[i].id) + ", QNTD=" + (v_item[i].qntd);

                // Add ao Server e DB
                var rai = ItemManager.addItem(v_item, _session.getUID(), 0, 0);

                if (rai.fails.Count() > 0 && rai.type != ItemManager.RetAddItem.T_SUCCESS_PANG_AND_EXP_AND_CP_POUCH)
                    throw new exception("[Channel::requestPlayPapelShop][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou jogar o Papel Shop Normal, mas nao conseguiu adicionar o(s) Item(ns){"
                            + ids + "}", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 6, 0x5900106));

                // Delete Coupon e coloca no List de att item, se tiver coupon
                if (coupon != null)
                {
                    item = new stItem();

                    item.type = 2;
                    item.id = coupon.id;
                    item._typeid = coupon._typeid;
                    item.qntd = 1;
                    item.STDA_C_ITEM_QNTD = (short)((ushort)item.qntd * -1);

                    if (ItemManager.removeItem(item, _session) <= 0)
                        throw new exception("[Channel::requestPlayPapelShop][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou jogar o Papel Shop Normal, mas nao conseguiu deletar o Coupon[TYPEID="
                            + (coupon._typeid) + ", ID=" + (coupon.id) + "]. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 5, 0x5900105));

                    // Add ao List
                    v_item.Add(new stItem(item));

                }
                else    // Não tem Coupon Tira Pangs do player
                    _session.m_pi.consomePang(sPapelShopSystem.getInstance().getPriceNormal());

                // Update Papel Shop Count Player. Se o limite por dia estiver habilitado, decrementa 1 do player
                sPapelShopSystem.getInstance().updatePlayerCount(_session);

                // Verificar se ganhou item Raro, se sim, cria um log no banco de dados
                foreach (var el in balls)
                {
                    if (el.ctx_psi.tipo == PAPEL_SHOP_TYPE.PST_RARE)
                    {

                        sys_achieve.incrementCounter(0x6C400081u/*Rare Win*/);

                        snmdb.NormalManagerDB.getInstance().add(19, new CmdInsertPapelShopRareWinLog(_session.m_pi.uid, el), SQLDBResponse, this);
                    }
                }

                // UPDATE Achievement ON SERVER, DB and GAME

                // Add +1 ao contador de jogo ao play Palpel Shop
                sys_achieve.incrementCounter(0x6C40004Au/*Play Papel Shop*/);

                // UPDATE ON GAME
                p.init_plain(0x216);

                p.WriteUInt32((uint)UtilTime.GetSystemTimeAsUnix());
                p.WriteUInt32((uint)v_item.Count());

                foreach (var el in v_item)
                {
                    p.WriteByte(el.type);
                    p.WriteUInt32(el._typeid);
                    p.WriteInt32(el.id);
                    p.WriteUInt32(el.flag_time);
                    p.WriteBytes(el.stat.ToArray());
                    p.WriteInt32((el.STDA_C_ITEM_TIME > 0) ? el.STDA_C_ITEM_TIME : el.STDA_C_ITEM_QNTD);
                    p.WriteZero(25);  // C[0~4] 10 Bytes e mais outras coisas, que tem na struct stItem216 explicando
                }
                packet_func.session_send(p, _session);

                p.init_plain(0xFB);

                if (sPapelShopSystem.getInstance().isLimittedPerDay())
                {
                    p.WriteInt32(_session.m_pi.mi.papel_shop.remain_count);
                    p.WriteInt32(-2);                                             // Flag
                }
                else
                {
                    p.WriteInt32(-1);
                    p.WriteInt32(-3);                                             // Flag
                }

                packet_func.session_send(p, _session);

                // Resposta para o Play Papel Shop Normal
                p.init_plain(0x21B);

                p.WriteUInt32(0);     // OK

                p.WriteInt32((coupon != null) ? coupon.id : 0);

                p.WriteUInt32((uint)balls.Count());

                foreach (var el in balls)
                {
                    p.WriteUInt32((uint)el.color);
                    p.WriteUInt32(el.ctx_psi._typeid);
                    p.WriteUInt32((uint)((el.item is stItem item1) ? item1.id : 0));    // Precisa do ID, se não ele add 2 itens, o do pacote 216 e o desse
                    p.WriteUInt32(el.qntd);
                    p.WriteUInt32((uint)el.ctx_psi.tipo);
                }

                p.WriteUInt64(_session.m_pi.ui.pang);
                p.WriteUInt64(_session.m_pi.cookie);

                packet_func.session_send(p, _session);

                // UPDATE Achievement ON SERVER, DB and GAME
                sys_achieve.finish_and_update(_session);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestPlayPapelShop][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x21B);

                p.WriteUInt32((ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) == STDA_ERROR_TYPE.CHANNEL) ? ExceptionError.STDA_SYSTEM_ERROR_DECODE(e.getCodeError()) : 0x5900100);

                packet_func.session_send(p, _session);
            }
        }

        public void requestPlayBigPapelShop(Player _session, packet _packet)
        {
            //

            var p = new PangyaBinaryWriter();

            try
            {





                if (_session.m_pi.block_flag.m_flag.papel_shop)
                    throw new exception("[Channel::requestPlayBigPapelShop][Error] PLAYER [UID=" + (_session.m_pi.uid)
                            + "] tentou jogar no Papel Shop, mas ele nao pode. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 3, 0x790001));

                if (_session.m_pi.mi.level < 1)
                    throw new exception("[Channel::requestPlayBigPapelShop][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou jogar o Papel Shop Big, mas nao tem o level necessario[level="
                            + (_session.m_pi.mi.level) + ", request=1]", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 8, 0x5900108));

                if (!sPapelShopSystem.getInstance().isLoad())
                    sPapelShopSystem.getInstance().load();

                if (sPapelShopSystem.getInstance().isLimittedPerDay() && _session.m_pi.mi.papel_shop.remain_count <= 0)
                    throw new exception("[Channel::requestPlayBigPapelShop][Warning] PLAYER [UID=" + (_session.m_pi.uid)
                            + "] tentou jogar o Papel Shop Big, mas o limite por dia esta ativado, e ele nao tem mais vezes no dia ele ja chegou ao seu limite.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 1, 0x5900101));

                if (_session.m_pi.ui.pang < sPapelShopSystem.getInstance().getPriceBig())
                    throw new exception("[Channel::requestPlayBigPapelShop][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou jogar o Papel Shop Big, ele nao tem Pangs suficiente[value="
                            + (_session.m_pi.ui.pang) + ", request=" + (sPapelShopSystem.getInstance().getPriceBig()) + "]. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 2, 0x5900102));

                var balls = sPapelShopSystem.getInstance().dropBigBall(_session);

                if (balls.empty())
                    throw new exception("[Channel::requestPlayBigPapelShop][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou jogar o Papel Shop Big, mas nao conseguiu sortear as bolas. Bug",
                            ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 3, 0x5900103));

                List<stItem> v_item = new List<stItem>();
                stItem item = new stItem();
                BuyItem bi = new BuyItem();

                AchievementSystem sys_achieve = new AchievementSystem();

                // Reserva memória para o List, não realocar depois a cada push_back ou Add
                v_item.Capacity = (balls.Count());

                foreach (var el in balls)
                {
                    bi = new BuyItem();
                    item = new stItem();
                    bi.id = -1;
                    bi._typeid = el.ctx_psi._typeid;
                    bi.qntd = el.qntd;

                    ItemManager.initItemFromBuyItem(_session.m_pi, item, bi, false, 0, 0, 1);

                    if (item._typeid == 0)
                        throw new exception("[Channel::requestPlayBigPapelShop][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou jogar o Papel Shop Big, mas nao conseguiu inicializar o Item[TYPEID="
                                + (bi._typeid) + "]. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 4, 0x5900104));

                    var it = v_item.FirstOrDefault(el2 =>
                    {
                        return el2._typeid == item._typeid;
                    });

                    if (it != null)
                    {   // Já tem o item soma as quantidades
                        it.qntd += item.qntd;
                        it.STDA_C_ITEM_QNTD = (short)it.qntd;
                    }
                    else    // Não tem coloca ele no List
                    {
                        v_item.Add(new stItem(item));

                        it = item;
                    }
                    el.item = item;
                }

                // UPDATE ON SERVER

                string ids = "";

                for (var i = 0; i < v_item.Count(); ++i)
                    ids += ((i == 0) ? "" : ", " + "TYPEID=" + (v_item[i]._typeid) + ", ID=" + (v_item[i].id) + ", QNTD=" + (v_item[i].STDA_C_ITEM_QNTD));

                // Add ao Server e DB
                var rai = ItemManager.addItem(v_item, _session.getUID(), 0, 0);

                if (rai.fails.Count() > 0 && rai.type != ItemManager.RetAddItem.T_SUCCESS_PANG_AND_EXP_AND_CP_POUCH)
                    throw new exception("[Channel::requestPlayBigPapelShop][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou jogar o Papel Shop Big, mas nao conseguiu adicionar o(s) Item(ns){"
                            + ids + "}", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 6, 0x5900106));

                // Tira Pangs do player
                _session.m_pi.consomePang(sPapelShopSystem.getInstance().getPriceBig());

                // Update Papel Shop Count Player. Se o limite por dia estiver habilitado, decrementa 1 do player
                sPapelShopSystem.getInstance().updatePlayerCount(_session);

                // Verificar se ganhou item Raro, se sim, cria um log no banco de dados
                balls.ForEach(el =>
                {
                    if (el.ctx_psi.tipo == PAPEL_SHOP_TYPE.PST_RARE)
                    {
                        // Add +1 ao contador de item Rare Win no Papel Shop
                        sys_achieve.incrementCounter(0x6C400081u /*Rare Win*/);

                        snmdb.NormalManagerDB.getInstance().add(19, new CmdInsertPapelShopRareWinLog(_session.m_pi.uid, el), SQLDBResponse, this);
                    }
                });


                // UPDATE Achievement ON SERVER, DB and GAME

                // Add +1 ao contador de jogo ao play Palpel Shop
                sys_achieve.incrementCounter(0x6C40004Au/*Play Papel Shop*/);

                // UPDATE ON GAME

                // Update Pangs
                p.init_plain(0xC8);

                p.WriteUInt64(_session.m_pi.ui.pang);
                p.WriteUInt64(0);

                packet_func.session_send(p, _session, 1);

                // Update Itens

                // UPDATE ON GAME
                p.init_plain(0x216);

                p.WriteUInt32((uint)UtilTime.GetSystemTimeAsUnix());
                p.WriteUInt32((uint)v_item.Count());

                foreach (var el in v_item)
                {
                    p.WriteByte(el.type);
                    p.WriteUInt32(el._typeid);
                    p.WriteInt32(el.id);
                    p.WriteUInt32(el.flag_time);
                    p.WriteBytes(el.stat.ToArray());
                    p.WriteInt32((el.STDA_C_ITEM_TIME > 0) ? el.STDA_C_ITEM_TIME : el.STDA_C_ITEM_QNTD);
                    p.WriteZeroByte(25);  // C[0~4] 10 Bytes e mais outras coisas, que tem na struct stItem216 explicando
                }

                packet_func.session_send(p, _session, 1);

                // Update Count Play Per Day
                p.init_plain(0xFB);

                if (sPapelShopSystem.getInstance().isLimittedPerDay())
                {
                    p.WriteInt32(_session.m_pi.mi.papel_shop.remain_count);
                    p.WriteInt32(_session.m_pi.mi.papel_shop.current_count);
                }
                else
                {
                    p.WriteInt32(-1);
                    p.WriteInt32(-3);
                }

                packet_func.session_send(p, _session, 1);

                // Resposta para o Play Papel Shop Big
                p.init_plain(0x26C);

                p.WriteUInt32(0);       // OK

                p.WriteInt32(0);        // Big Papel Shop não tem coupon

                p.WriteUInt32((uint)balls.Count());

                foreach (var el in balls)
                {
                    p.WriteUInt32(el.color);
                    p.WriteUInt32(el.ctx_psi._typeid);
                    p.WriteInt32((el.item is stItem) ? ((stItem)el.item).id : 0);   // Precisa do ID, se não ele add 2 itens, o do pacote 216 e o desse
                    p.WriteUInt32(el.qntd);
                    p.WriteUInt32(el.ctx_psi.tipo);
                }

                p.WriteUInt64(_session.m_pi.ui.pang);
                p.WriteUInt64(_session.m_pi.cookie);

                packet_func.session_send(p, _session, 1);

                // UPDATE Achievement ON SERVER, DB and GAME
                sys_achieve.finish_and_update(_session);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestPlayBigPapelShop][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x26C);

                p.WriteUInt32((ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) == (uint)STDA_ERROR_TYPE.CHANNEL) ? ExceptionError.STDA_SYSTEM_ERROR_DECODE(e.getCodeError()) : 0x5900100);

                packet_func.session_send(p, _session, 1);
            }
        }


        public void requestSendMsgChatRoom(Player _session, string _msg)
        {

            if (!_session.getState())
            {
                throw new exception("[Channel::requestSendMsgChatRoom][Error] player nao esta connectado.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                    1, 0));
            }

            PangyaBinaryWriter p = new PangyaBinaryWriter();

            try
            {
                var r = m_rm.findRoom((short)_session.m_pi.mi.sala_numero);

                if (r == null)
                {
                    throw new exception("[Channel::requestSendMsgChatRoom][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] Channel[ID=" + ((ushort)m_ci.id) + "] nao esta em uma sala[NUMERO=" + (_session.m_pi.mi.sala_numero) + "]. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        18, 0));
                }

                packet_func.room_broadcast(r, packet_func.pacote040(_session.m_pi.nickname, _msg, ((_session.m_pi.m_cap.game_master) ? eChatMsg.CHAT_GM : 0)), 0);
            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::requestSendMsgChatRoom][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                // A função que chama ela tem que tratar as excpetion, relança elas
                throw;
            }
        }

        public void sendUpdateRoomInfo(RoomInfoEx _ri, int _option)
        {

            if (_ri.getTipo() != RoomInfo.ROOM_INFO_TYPE.PRACTICE && _ri.getTipo() != RoomInfo.ROOM_INFO_TYPE.GRAND_ZODIAC_PRACTICE)
            { // No modo practice não envia o pacote47, que é a criação de sala visual na lobby

                packet_func.channel_broadcast(this,
                        packet_func.pacote047(new List<RoomInfoEx>() { _ri },
                        _option), 0);
            }
        }

        public void sendUpdatePlayerInfo(Player _session, int _option)
        {
            PlayerLobbyInfo pci = getPlayerInfo(_session);

            var p = packet_func.pacote046(new List<PlayerLobbyInfo>() { (pci == null) ? new PlayerLobbyInfo() : pci }, _option);

            packet_func.channel_broadcast(this, p, 0);
        }

        public void destroyRoom(short sala_numero)
        {

            try
            {

                var r = m_rm.findRoom(sala_numero);

                if (r == null)
                {
                    throw new exception("[Channel::destroyRoom][Error] Channel[ID=" + ((ushort)m_ci.id) + "] tentou destruir a sala[NUMERO=" + (sala_numero) + "], mas a sala nao existe.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        16, 0x5700100));
                }

                // Kick All of Room And Automatic Room Destroyed
                var v_sessions = r.getSessions();

                if (v_sessions.Count == 0)
                {

                    RoomInfoEx ri = r.getInfo();

                    m_rm.destroyRoom(r);

                    sendUpdateRoomInfo(ri, 2);

                }
                else
                {

                    // Kick all player e destroi a sala
                    foreach (var el in v_sessions)
                    {
                        kickPlayerRoom(el, 0);
                    }
                }

                // Log
                _smp.message_pool.getInstance().push(new message("[Channel::destroyRoom][Sucess] Channel[ID=" + ((ushort)m_ci.id) + "] destruiu a sala[NUMERO=" + (sala_numero) + "] no canal[NOME=" + (m_ci.name) + "].", type_msg.CL_FILE_LOG_AND_CONSOLE));



            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::destroyRoom][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public static void _enter_left_time_is_over(object _arg1, object _arg2)
        {
            var c = (Channel)_arg1;
            short numero = (short)Convert.ToUInt16(_arg2);

            try
            {

                if (c == null)
                {
                    throw new exception("[Channel::_enter_left_time_is_over][Error] Channel[ID=-1] Sala[NUMERO=" + (numero) + "] channel ponteiro fornecido pelo argumento is invalid.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        1201, 0));
                }

                if (numero < 0)
                {
                    throw new exception("[Channel::_enter_left_time_is_over][Error] Channel[ID=" + ((ushort)c.getId()) + "] Sala[NUMERO=" + (numero) + "] numero da sala fornecido pelo argumento is invalid", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        1200, 0));
                }

                var r = c.m_rm.findRoom(numero);

                if (r == null)
                {
                    throw new exception("[Channel::_enter_left_time_is_over][Error] Channel[ID=" + ((ushort)c.getId()) + "] Sala[NUMERO=" + (numero) + "] nao encontrou a sala no canal", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        1202, 0));
                }

                r.setState(0);
                r.setFlag(0);

                // Limpa no Game o Timer
                r.requestEndAfterEnter();

                PangyaBinaryWriter p = new PangyaBinaryWriter();

                // Update Room ON LOBBY
                packet_func.channel_broadcast(c,
                        packet_func.pacote047(
                    new List<RoomInfoEx>() { r.getInfo() },
                        3), 1);
                 
            }
            catch (exception e)
            {
                _smp.message_pool.getInstance().push(new message("[Channel::_enter_left_time_is_over][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void addInviteTimeRequest(InviteChannelInfo _ici)
        {

            if (_ici.room_number < 0)
            {
                throw new exception("[Channel::addInviteTimeRequest][Error] Channel[ID=" + ((ushort)m_ci.id) + "] tentou adicionar Invite Time Request[INVITE=" + (_ici.invite_uid) + ", INVITED=" + (_ici.invited_uid) + "] para sala[NUMERO=" + (_ici.room_number) + "], mas o numero da sala é invalido. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                    3010, 0));
            }

            if (_ici.invite_uid == 0u)
            {
                throw new exception("[Channel::addInviteTimeRequest][Error] Channel[ID=" + ((ushort)m_ci.id) + "] tentou adicionar Invite Time Request[INVITE=" + (_ici.invite_uid) + ", INVITED=" + (_ici.invited_uid) + "] para sala[NUMERO=" + (_ici.room_number) + "], mas quem convidou o uid is invalid(zero)", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                    3010, 1));
            }

            if (_ici.invited_uid == 0u)
            {
                throw new exception("[Channel::addInviteTimeRequest][Error] Channel[ID=" + ((ushort)m_ci.id) + "] tentou adicionar Invite Time Request[INVITE=" + (_ici.invite_uid) + ", INVITED=" + (_ici.invited_uid) + "] para sala[NUMERO=" + (_ici.room_number) + "], mas o convidado uid is invalid(zero)", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                    3010, 2));
            }




            v_invite.Add(_ici);



        }

        public void deleteInviteTimeRequest(InviteChannelInfo _ici)
        {
            if (_ici.room_number < 0)
            {
                throw new exception("[Channel::deleteInviteTimeRequest][Error] Channel[ID=" + ((ushort)m_ci.id) +
                    "] tentou deletar Invite Time Request[INVITE=" + _ici.invite_uid + ", INVITED=" + _ici.invited_uid +
                    "] para sala[NUMERO=" + _ici.room_number + "], mas o numero da sala é invalido. Hacker ou Bug",
                    ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 3011, 0));
            }

            if (_ici.invite_uid == 0)
            {
                throw new exception("[Channel::deleteInviteTimeRequest][Error] Channel[ID=" + ((ushort)m_ci.id) +
                    "] tentou deletar Invite Time Request[INVITE=" + _ici.invite_uid + ", INVITED=" + _ici.invited_uid +
                    "] para sala[NUMERO=" + _ici.room_number + "], mas quem convidou o uid is invalid(zero)",
                    ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 3011, 1));
            }

            if (_ici.invited_uid == 0u)
            {
                throw new exception("[Channel::deleteInviteTimeRequest][Error] Channel[ID=" + ((ushort)m_ci.id) +
                    "] tentou deletar Invite Time Request[INVITE=" + _ici.invite_uid + ", INVITED=" + _ici.invited_uid +
                    "] para sala[NUMERO=" + _ici.room_number + "], mas o convidado uid is invalid(zero)",
                    ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 3011, 2));
            }



            try
            {
                int index = v_invite.FindIndex(_el =>
                    _el.room_number == _ici.room_number &&
                    _el.invite_uid == _ici.invite_uid &&
                    _el.invited_uid == _ici.invited_uid);

                if (index != int.MaxValue)
                {
                    v_invite.RemoveAt(index);
                }
                else
                {
                    _smp.message_pool.getInstance().push(new message(
                        "[Channel::deleteInviteTimeRequest][Sucess] Channel[ID=" + ((ushort)m_ci.id) +
                        "] tentou deletar Invite Time Request[INVITE=" + _ici.invite_uid + ", INVITED=" + _ici.invited_uid +
                        "] para sala[NUMERO=" + _ici.room_number + "], mas ele nao existe mais no List do canal.",
                        type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
            }
            finally
            {

            }
        }

        public void deleteInviteTimeResquestByInvited(Player _session)
        {

            try
            {



                for (var i = 0; i < v_invite.Count; ++i)
                {

                    if (v_invite[i].invited_uid == _session.m_pi.uid)
                    {

                        var r = m_rm.findRoom((short)v_invite[i].room_number);

                        if (r != null && r.isInvited(_session))
                        {

                            var ici = r.deleteInvited(_session);

                            v_invite.RemoveAt(i--); // Remove e ajusta índice como no C++

                            sendUpdateRoomInfo(r.getInfo(), 3);
                        }


                    }
                }




            }
            catch (exception e)
            {




                _smp.message_pool.getInstance().push(new message("[Channel::deleteInviteTimeRequestByInvited][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public bool send_time_out_invite(InviteChannelInfo _ici)
        {

            // Libera o Critical Section do invite, e bloqueia assim que pegar a sala



            var r = m_rm.findRoom((short)_ici.room_number);



            // InviteChannelInfo não é mais um invite válido, ele já foi excluido
            if (!v_invite.Contains(_ici))
            {
                return false;
            }

            try
            {

                if (r == null)
                {
                    _smp.message_pool.getInstance().push(new message("[Channel::send_time_out_invite][Sucess] Channel[ID=" + ((ushort)m_ci.id) + "] tentou deletar o convite[CONVIDOU=" + (_ici.invite_uid) + ", CONVIDADO=" + (_ici.invited_uid) + "] da Sala[NUMERO=" + (_ici.room_number) + "], mas a sala nao existe mais no canal.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                    // Deleta o invite, a sala não é válida mais, mas o invite ainda é válido
                    return true;
                }

                var s = findSessionByUID((int)_ici.invited_uid);

                if (s == null)
                {

                    _smp.message_pool.getInstance().push(new message("[Channel::send_time_out_invite][Sucess] Channel[ID=" + ((ushort)m_ci.id) + "] tentou deletar o convite[CONVIDOU=" + (_ici.invite_uid) + ", CONVIDADO=" + (_ici.invited_uid) + "] da Sala[NUMERO=" + (_ici.room_number) + "], mas o convidado nao esta mais no canal, tenta excluir o convite com uid.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                    r.deleteInvited(_ici.invited_uid);

                }
                else
                {
                    r.deleteInvited(s);
                }

                sendUpdateRoomInfo(r.getInfo(), 3);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[Channel::send_time_out_invite][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }



            // Deleta o Invite
            return true;
        }

        public void clear_invite_time()
        {

            if (!v_invite.empty())
            {

                // Envia o Time out dos invite do Canal
                foreach (var el in v_invite)
                {
                    send_time_out_invite(el);
                }

                v_invite.Clear();
            }
        }

        public void removeSession(Player _session)
        {

            if (_session == null)
            {
                throw new exception("[Channel::removeSession][Error] _session is null.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                    3, 0));
            }

            int index = -1;

            //Monitor.Exit(m_cs);

            if ((index = findIndexSession(_session)) == -1)
            {
                //Monitor.Exit(m_cs);
                return;
            }

            v_sessions.RemoveAt(index);

            m_ci.curr_user--;

            // reseta(default) o channel que o player está no player info
            _session.m_pi.channel = DEFAULT_CHANNEL;
            _session.m_pi.mi.sala_numero = DEFAULT_ROOM_ID;
            _session.m_pi.place = 0;

            deletePlayerInfo(_session);

            //Monitor.Exit(m_cs);
        }

        public void addSession(Player _session)
        {

            if (_session == null || !_session.getState())
            {
                throw new exception("[Channel::addSession][Error] _session is null or invalid.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                    3, 1));
            }

            //Monitor.Exit(m_cs);

            v_sessions.Add(_session);

            m_ci.curr_user++;

            // Channel id
            _session.m_pi.channel = m_ci.id;
            _session.m_pi.place = 0;

            // Calcula a condição do player e o sexo
            // Só faz calculo de Quita rate depois que o player
            // estiver no level Beginner E e jogado 50 games
            if (_session.m_pi.mi.level >= 6 && _session.m_pi.ui.jogado >= 50)
            {
                float rate = _session.m_pi.ui.getQuitRate();

                if (rate < GOOD_PLAYER_ICON)
                {
                    _session.m_pi.mi.state_flag.azinha = 1;
                }
                else if (rate >= QUITER_ICON_1 && rate < QUITER_ICON_2)
                {
                    _session.m_pi.mi.state_flag.quiter_1 = 1;
                }
                else if (rate >= QUITER_ICON_2)
                {
                    _session.m_pi.mi.state_flag.quiter_2 = 1;
                }
            }

            if (_session.m_pi.ei.char_info != null && _session.m_pi.ui.getQuitRate() < GOOD_PLAYER_ICON)
            {
                _session.m_pi.mi.state_flag.icon_angel = _session.m_pi.ei.char_info.AngelEquiped();
            }
            else
            {
                _session.m_pi.mi.state_flag.icon_angel = 0;
            }
            _session.m_pi.mi.sexo = (byte)(_session.m_pi.mi.state_flag.sexo);

            makePlayerInfo(_session);

            //Monitor.Exit(m_cs);
        }

        public Player findSessionByOID(uint _oid)
        {
            return m_player_info.Keys.FirstOrDefault(c => c.m_oid == _oid);
        }
        protected Player findSessionByUID(int _uid)
        {
            return m_player_info.Keys.FirstOrDefault(c => c.getUID() == _uid);
        }
        protected Player findSessionByNickname(string _nickname)
        {
            return m_player_info.Keys.FirstOrDefault(c => c.getNickname() == _nickname);
        }
        public int findIndexSession(Player _session)
        {

            if (_session == null)
            {
                throw new exception("[Channel::findIndexSession][Error] _session is null.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                    3, 0));
            }

            for (var i = 0; i < v_sessions.Count; ++i)
            {
                if (v_sessions[i] == _session)
                {
                    return i;
                }
            }

            return -1;
        }

        //        ////
        protected void makePlayerInfo(Player _session)
        {
            PlayerLobbyInfo pci = new PlayerLobbyInfo
            {
                // Player Canal Info clear
                uid = _session.m_pi.uid,
                oid = _session.m_oid,
                sala_numero = _session.m_pi.mi.sala_numero,
                level = (byte)_session.m_pi.mi.level,
                capability = _session.m_pi.m_cap,
                nickname = _session.m_pi.nickname,
                sDisplayID = "@NT_" + _session.m_pi.nickname,
                title = _session.m_pi.ue.m_title,
                ladder_point = 1000,
                guild_index_mark = _session.m_pi.gi.index_mark_emblem,
                guild_uid = _session.m_pi.gi.uid,
                guild_mark_img = _session.m_pi.gi.mark_emblem,
                flag_visible_gm = Convert.ToInt16(_session.m_pi.mi.state_flag.visible)
            };
            // Só faz calculo de Quita rate depois que o player
            // estiver no level Beginner E e jogado 50 games
            if (_session.m_pi.mi.level >= 6 && _session.m_pi.ui.jogado >= 50)
            {
                float rate = _session.m_pi.ui.getQuitRate();

                if (rate < GOOD_PLAYER_ICON)
                {
                    pci.state_flag.azinha = 0;
                }
                else if (rate >= QUITER_ICON_1 && rate < QUITER_ICON_2)
                    pci.state_flag.quiter_1 = 1;
                else if (rate >= QUITER_ICON_2)
                    pci.state_flag.quiter_2 = 1;
            }

            if (_session.m_pi.ei.char_info != null && _session.m_pi.ui.getQuitRate() < GOOD_PLAYER_ICON)
                pci.state_flag.icon_angel = 0;
            else
                pci.state_flag.icon_angel = 0;

            pci.state_flag.sexo = _session.m_pi.mi.sexo;

            pci.guild_uid = _session.m_pi.gi.uid;

            if (!m_player_info.ContainsKey(_session))
            {
                m_player_info.Add(_session, pci);
            }
            // Update Player Location
            _session.m_pi.updateLocationDB();
        }

        protected void updatePlayerInfo(Player _session)
        {
            PlayerLobbyInfo pci;

            if ((pci = getPlayerInfo(_session)) == null)
                return;//so retorna mesmo

            // Player Canal Info Update
            pci.nickname = _session.m_pi.nickname;
            pci.uid = _session.m_pi.uid;
            pci.oid = _session.m_oid;
            pci.sala_numero = _session.m_pi.mi.sala_numero;
            pci.level = (byte)_session.m_pi.mi.level;
            pci.ladder_point = 1000;
            pci.flag_visible_gm = Convert.ToInt16(_session.m_pi.mi.state_flag.visible);
            pci.capability = _session.m_pi.m_cap;
            pci.title = _session.m_pi.ue.m_title;
            pci.guild_index_mark = _session.m_pi.gi.index_mark_emblem;
            pci.guild_uid = _session.m_pi.gi.uid;
            pci.guild_mark_img = _session.m_pi.gi.mark_emblem;
            // Só faz calculo de Quita rate depois que o player
            // estiver no level Beginner E e jogado 50 games
            if (_session.m_pi.mi.level >= 6 && _session.m_pi.ui.jogado >= 50)
            {
                float rate = _session.m_pi.ui.getQuitRate();

                if (rate < GOOD_PLAYER_ICON)
                    pci.state_flag.azinha = 1;
                else if (rate >= QUITER_ICON_1 && rate < QUITER_ICON_2)
                    pci.state_flag.quiter_1 = 1;
                else if (rate >= QUITER_ICON_2)
                    pci.state_flag.quiter_2 = 1;
            }

            if (_session.m_pi.ei.char_info != null && _session.m_pi.ui.getQuitRate() < GOOD_PLAYER_ICON)
                pci.state_flag.icon_angel = 0;
            else
                pci.state_flag.icon_angel = 0;

            pci.state_flag.sexo = _session.m_pi.mi.sexo;

            // Update Location Player
            _session.m_pi.updateLocationDB();
        }

        public void deletePlayerInfo(Player _session)
        {
            // Update Location player
            _session.m_pi.updateLocationDB();

            // Delete Player Info of session(player)
            m_player_info.Remove(_session);
        }

        public bool CommandByChat(Player _session, Queue<string> _command)
        {

            string cmd = "command_chat";//wind
            Room r = null;
            cmd = _command.Dequeue();
            try
            {
                //comandos via chat ;)
                var p = new PangyaBinaryWriter();
                if (!string.IsNullOrEmpty(cmd))
                {
                    r = m_rm.findRoom((short)_session.m_pi.mi.sala_numero);

                    if (_session.m_pi.mi.capability.game_master)      //comandos [player gm/adm]
                    {
                        if (r != null) //comandos em sala
                        {
                            if (cmd == "@add")
                            {

                            }
                            if (cmd == ("@notice") || cmd == ("@noticia"))
                            {
                                string msg = "notice";
                                msg = msg = string.Join(separator: " ", _command.ToArray());

                                snmdb.NormalManagerDB.getInstance().add(0, new CmdInsertNotice(msg, 1, 1), null, null);

                                // Send Message
                                p.init_plain(0x40); // Msg to Chat of player

                                p.WriteByte(7); // Notice

                                p.WriteString("@INI3");
                                p.WriteString("\\c0xff00ff00\\cSend Notice-Broadcast");

                                packet_func.session_send(p, _session, 1);

                                return true;
                            }

                            if (cmd == ("@bot") && r.getNumPlayers() == 1 && !r.IsStarted())
                            {
                                if (!r.IsWithBot() && !r.IsRoomGM() && r.getInfo().getTipo() == RoomInfo.ROOM_INFO_TYPE.STROKE || r.getInfo().getTipo() == RoomInfo.ROOM_INFO_TYPE.TOURNEY || r.getInfo().getTipo() == RoomInfo.ROOM_INFO_TYPE.SPECIAL_SHUFFLE_COURSE)
                                {

                                    try
                                    {
                                        r.setSenha("bot");
                                        r.makeBot(_session);
                                        if (r.IsWithBot())
                                        {
                                            // Send Message
                                            p.init_plain(0x40); // Msg to Chat of player

                                            p.WriteByte(7); // Notice

                                            p.WriteString("@INI3");
                                            p.WriteString("\\c0xff00ff00\\cCall bot by chat.");

                                            packet_func.session_send(p, _session, 1);
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
                            if (cmd == ("@play") && r.getNumPlayers() > 1 && !r.isGaming() && (r.getInfo().getTipo() == RoomInfo.ROOM_INFO_TYPE.STROKE || r.getInfo().getTipo() == RoomInfo.ROOM_INFO_TYPE.MATCH || r.getInfo().getTipo() == RoomInfo.ROOM_INFO_TYPE.TOURNEY || r.getInfo().getTipo() == RoomInfo.ROOM_INFO_TYPE.SPECIAL_SHUFFLE_COURSE))
                            {
                                r.SetAllReady();//inicia tudo mundo aqui
                                                // Send Message
                                p.init_plain(0x40); // Msg to Chat of player

                                p.WriteByte(7); // Notice

                                p.WriteString("@INI3");
                                p.WriteString("\\c0xff00ff00\\cAuto Start Room.");

                                packet_func.session_send(p, _session, 1);

                                return true;
                            }
                            if (cmd == ("@big_char") && r.getInfo().getTipo() == RoomInfo.ROOM_INFO_TYPE.LOUNGE)
                            {
                                var it = (_session.m_pi.ei.char_info == null) ? _session.m_pi.mp_scl.Last() : _session.m_pi.mp_scl.find(_session.m_pi.ei.char_info.id);
                                if (it.Value.scale_head > 0)
                                {
                                    float scale_head = Convert.ToSingle(_command.Dequeue());


                                    it.Value.scale_head = scale_head;
                                    p.init_plain(0x196);
                                    p.WriteInt32(_session.m_oid);
                                    p.WriteBytes(it.Value.ToArray());
                                    packet_func.room_broadcast(r, p, 1);
                                    // Send Message
                                    p.init_plain(0x40); // Msg to Chat of player

                                    p.WriteByte(7); // Notice

                                    p.WriteString("@INI3");
                                    p.WriteString("\\c0xff00ff00\\cChange Character head");

                                    packet_func.session_send(p, _session, 1);

                                }
                            }
                            if (cmd == ("@speed_char") && r.getInfo().getTipo() == RoomInfo.ROOM_INFO_TYPE.LOUNGE)
                            {
                                var it = (_session.m_pi.ei.char_info == null) ? _session.m_pi.mp_scl.Last() : _session.m_pi.mp_scl.find(_session.m_pi.ei.char_info.id);
                                if (it.Value.scale_head > 0)
                                {
                                    float scale_head = Convert.ToSingle(_command.Dequeue());


                                    it.Value.walk_speed = scale_head;
                                    p.init_plain(0x196);
                                    p.WriteInt32(_session.m_oid);
                                    p.WriteBytes(it.Value.ToArray());
                                    packet_func.room_broadcast(r, p, 1);
                                    // Send Message
                                    p.init_plain(0x40); // Msg to Chat of player

                                    p.WriteByte(7); // Notice

                                    p.WriteString("@INI3");
                                    p.WriteString("\\c0xff00ff00\\cChange to Speed Character");

                                    packet_func.session_send(p, _session, 1);

                                }
                            }
                            if (cmd == ("@un_char") && r.getInfo().getTipo() == RoomInfo.ROOM_INFO_TYPE.LOUNGE)
                            {
                                var it = (_session.m_pi.ei.char_info == null) ? _session.m_pi.mp_scl.Last() : _session.m_pi.mp_scl.find(_session.m_pi.ei.char_info.id);
                                if (it.Value.scale_head > 0)
                                {
                                    float scale_head = Convert.ToSingle(_command.Dequeue());


                                    it.Value.fUnknown = scale_head;
                                    p.init_plain(0x196);
                                    p.WriteInt32(_session.m_oid);
                                    p.WriteBytes(it.Value.ToArray());
                                    packet_func.room_broadcast(r, p, 1);
                                    // Send Message
                                    p.init_plain(0x40); // Msg to Chat of player

                                    p.WriteByte(7); // Notice

                                    p.WriteString("@INI3");
                                    p.WriteString("\\c0xff00ff00\\cChange to un");

                                    packet_func.session_send(p, _session, 1);

                                }
                            }
                            if (cmd == ("@cam_char") && r.getInfo().getTipo() == RoomInfo.ROOM_INFO_TYPE.LOUNGE)
                            {
                                var it = (_session.m_pi.ei.char_info == null) ? _session.m_pi.mp_scl.Last() : _session.m_pi.mp_scl.find(_session.m_pi.ei.char_info.id);
                                if (it.Value.scale_head > 0)
                                {
                                    float scale_head = Convert.ToSingle(_command.Dequeue());

                                    it.Value.camera_zoom = scale_head;
                                    p.init_plain(0x196);
                                    p.WriteInt32(_session.m_oid);
                                    p.WriteBytes(it.Value.ToArray());
                                    packet_func.room_broadcast(r, p, 1);
                                    // Send Message
                                    p.init_plain(0x40); // Msg to Chat of player

                                    p.WriteByte(7); // Notice

                                    p.WriteString("@INI3");
                                    p.WriteString("\\c0xff00ff00\\cChange to camera by Character");

                                    packet_func.session_send(p, _session, 1);

                                }
                            }
                            if (cmd == ("@wind") && (r.getInfo().getTipo() == RoomInfo.ROOM_INFO_TYPE.GRAND_PRIX || r.getInfo().getTipo() == RoomInfo.ROOM_INFO_TYPE.MATCH || r.getInfo().getTipo() == RoomInfo.ROOM_INFO_TYPE.STROKE || r.getInfo().getTipo() == RoomInfo.ROOM_INFO_TYPE.TOURNEY || r.getInfo().getTipo() == RoomInfo.ROOM_INFO_TYPE.LOUNGE) || r.getInfo().getTipo() == RoomInfo.ROOM_INFO_TYPE.SPECIAL_SHUFFLE_COURSE || r.getInfo().getTipo() == RoomInfo.ROOM_INFO_TYPE.PRACTICE || r.getInfo().getTipo() == RoomInfo.ROOM_INFO_TYPE.GRAND_ZODIAC_PRACTICE)
                            {
                                if (_command.Count < 2)
                                    return false; // não tem argumentos suficientes

                                if (!ushort.TryParse(_command.Dequeue(), out ushort wind) ||
                                    !ushort.TryParse(_command.Dequeue(), out ushort degree))
                                {
                                    // Mensagem de erro pro GM
                                    p.init_plain(0x40);
                                    p.WriteByte(7);
                                    p.WriteString("@INI3");
                                    p.WriteString("\\c0xffff0000\\c Valores inválidos para vento.");
                                    packet_func.session_send(p, _session, 1);
                                    return true;
                                }

                                // Atualiza vento
                                p.init_plain(0x5B);
                                p.WriteUInt16(wind);
                                p.WriteUInt16(degree);
                                p.WriteByte(1);
                                packet_func.room_broadcast(r, p, 1);

                                // Mensagem de confirmação
                                p.init_plain(0x40);
                                p.WriteByte(7);
                                p.WriteString("@INI3");
                                p.WriteString("\\c0xff00ff00\\c GM alterou o vento.");
                                packet_func.session_send(p, _session, 1);

                                return true;
                            }
                            if (cmd == ("@weather") && (r.getInfo().getTipo() == RoomInfo.ROOM_INFO_TYPE.MATCH || r.getInfo().getTipo() == RoomInfo.ROOM_INFO_TYPE.STROKE || r.getInfo().getTipo() == RoomInfo.ROOM_INFO_TYPE.TOURNEY || r.getInfo().getTipo() == RoomInfo.ROOM_INFO_TYPE.LOUNGE) || r.getInfo().getTipo() == RoomInfo.ROOM_INFO_TYPE.SPECIAL_SHUFFLE_COURSE || r.getInfo().getTipo() == RoomInfo.ROOM_INFO_TYPE.PRACTICE || r.getInfo().getTipo() == RoomInfo.ROOM_INFO_TYPE.GRAND_ZODIAC_PRACTICE)
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

                                packet_func.room_broadcast(r, p, 1);

                                // Send Message
                                p.init_plain(0x40); // Msg to Chat of player

                                p.WriteByte(7); // Notice

                                p.WriteString("@INI3");
                                p.WriteString("\\c0xff00ff00\\c GM Change to Weather [" + clean.ToLowerInvariant() + "]");

                                packet_func.session_send(p, _session, 1);

                                return true;
                            }

                            if (cmd == ("@gift") || cmd == ("@presente"))
                            {
                                uint item_typeid = 0;
                                uint item_qntd = 0;
                                item_typeid = uint.Parse(_command.Dequeue());
                                item_qntd = uint.Parse(_command.Dequeue());

                                if (item_typeid == 0)
                                    throw new exception("[Channel::CommandByChat][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou enviar presente para todos o Item[TYPEID=" + (item_typeid) + "QNTD = "
                                        + (item_qntd) + "], mas item is invalid. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 3, 0x5700100));

                                if (item_qntd > 20000u)
                                    throw new exception("[Channel::CommandByChat][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou enviar presente para todos o Item[TYPEID=" + (item_typeid) + "QNTD = "
                                        + (item_qntd) + "], mas a quantidade passa de 20mil. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 4, 0x5700100));

                                var @base = sIff.getInstance().findCommomItem(item_typeid);

                                if (@base == null)
                                    throw new exception("[Channel::CommandByChat][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou enviar presente para todos o Item[TYPEID=" + (item_typeid) + "QNTD = "
                                        + (item_qntd) + "], mas o item nao existe no IFF_STRUCT do Server. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 6, 0));

                                stItem item = new stItem();
                                BuyItem bi = new BuyItem();

                                bi.id = -1;
                                bi._typeid = item_typeid;
                                bi.qntd = item_qntd;

                                var msg = ("GM Command Chat");

                                foreach (var el in v_sessions)
                                {
                                    if (el.m_pi.lobby != 255)
                                    {
                                        // Limpa item
                                        item = new stItem();

                                        ItemManager.initItemFromBuyItem(el.m_pi, item, bi, false, 0, 0, 1);

                                        if (item._typeid == 0)
                                            throw new exception("[Channel::CommandByChat][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou enviar presente para todos o Item[TYPEID=" + (item_typeid) + "QNTD = "
                                                + (item_qntd) + "], mas nao conseguiu inicializar o item. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 5, 0));

                                        if (MailBoxManager.sendMessageWithItem(0, el.m_pi.uid, msg, item) <= 0)
                                            throw new exception("[Channel::CommandByChat][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou enviar presente para o PLAYER [UID="
                                                + (el.m_pi.uid) + "] o Item[TYPEID=" + (item_typeid) + ", QNTD="
                                                + (item_qntd) + "], mas nao conseguiu colocar o item no mail box dele. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 7, 0));
                                    }
                                }

                                // Send Message
                                p.init_plain(0x40); // Msg to Chat of player

                                p.WriteByte(7); // Notice

                                p.WriteString("@INI3");
                                p.WriteString("\\c0xff00ff00\\cGM Send Gift");

                                packet_func.session_send(p, _session, 1);

                                return true;
                            }
                        }
                        else   //comandos sem esta na sala
                        {
                            if (cmd == ("@notice") || cmd == ("@noticia"))
                            {
                                string msg = "notice";
                                msg = string.Join(separator: " ", _command.ToArray());

                                snmdb.NormalManagerDB.getInstance().add(0, new CmdInsertNotice(msg, 1, 1), null, null);

                                // Send Message
                                p.init_plain(0x40); // Msg to Chat of player

                                p.WriteByte(7); // Notice

                                p.WriteString("@INI3");
                                p.WriteString("\\c0xff00ff00\\cSend Notice-Broadcast");

                                packet_func.session_send(p, _session, 1);

                                return true;
                            }

                            if (cmd == ("@gift") || cmd == ("@presente"))
                            {
                                uint item_typeid = 0;
                                uint item_qntd = 0;
                                item_typeid = uint.Parse(_command.Dequeue());
                                item_qntd = uint.Parse(_command.Dequeue());

                                if (item_typeid == 0)
                                    throw new exception("[Channel::CommandByChat][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou enviar presente para todos o Item[TYPEID=" + (item_typeid) + "QNTD = "
                                        + (item_qntd) + "], mas item is invalid. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 3, 0x5700100));

                                if (item_qntd > 20000u)
                                    throw new exception("[Channel::CommandByChat][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou enviar presente para todos o Item[TYPEID=" + (item_typeid) + "QNTD = "
                                        + (item_qntd) + "], mas a quantidade passa de 20mil. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 4, 0x5700100));

                                var @base = sIff.getInstance().findCommomItem(item_typeid);

                                if (@base == null)
                                    throw new exception("[Channel::CommandByChat][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou enviar presente para todos o Item[TYPEID=" + (item_typeid) + "QNTD = "
                                        + (item_qntd) + "], mas o item nao existe no IFF_STRUCT do Server. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 6, 0));

                                stItem item = new stItem();
                                BuyItem bi = new BuyItem();

                                bi.id = -1;
                                bi._typeid = item_typeid;
                                bi.qntd = item_qntd;

                                var msg = ("GM Command Chat");

                                foreach (var el in v_sessions)
                                {
                                    if (el.m_pi.lobby != 255)
                                    {
                                        // Limpa item
                                        item = new stItem();

                                        ItemManager.initItemFromBuyItem(el.m_pi, item, bi, false, 0, 0, 1);

                                        if (item._typeid == 0)
                                            throw new exception("[Channel::CommandByChat][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou enviar presente para todos o Item[TYPEID=" + (item_typeid) + "QNTD = "
                                                + (item_qntd) + "], mas nao conseguiu inicializar o item. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 5, 0));

                                        if (MailBoxManager.sendMessageWithItem(0, el.m_pi.uid, msg, item) <= 0)
                                            throw new exception("[Channel::CommandByChat][Error] PLAYER [UID=" + (_session.m_pi.uid) + "] tentou enviar presente para o PLAYER [UID="
                                                + (el.m_pi.uid) + "] o Item[TYPEID=" + (item_typeid) + ", QNTD="
                                                + (item_qntd) + "], mas nao conseguiu colocar o item no mail box dele. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 7, 0));
                                    }
                                }

                                // Send Message
                                p.init_plain(0x40); // Msg to Chat of player

                                p.WriteByte(7); // Notice

                                p.WriteString("@INI3");
                                p.WriteString("\\c0xff00ff00\\cGM Send Gift");

                                packet_func.session_send(p, _session, 1);

                                return true;
                            }
                            if (cmd == ("@notice") || cmd == ("@noticia"))
                            {

                                return true;
                            }
                        }
                    }
                    else   //comandos [player normal]
                    {
                        if (r != null)
                        {
                            if (cmd == ("@bot") && r.getNumPlayers() == 1 && !r.IsStarted())
                            {
                                if (!r.IsWithBot() && r.getInfo().getTipo() == RoomInfo.ROOM_INFO_TYPE.STROKE || r.getInfo().getTipo() == RoomInfo.ROOM_INFO_TYPE.TOURNEY || r.getInfo().getTipo() == RoomInfo.ROOM_INFO_TYPE.SPECIAL_SHUFFLE_COURSE)
                                {

                                    try
                                    {
                                        r.setSenha("by_luismk");
                                        r.sendUpdate();
                                        r.makeBot(_session);
                                        if (r.IsWithBot())
                                        {
                                            // Send Message
                                            p.init_plain(0x40); // Msg to Chat of player

                                            p.WriteByte(7); // Notice

                                            p.WriteString("@INI3");
                                            p.WriteString("\\c0xff00ff00\\cCall bot by chat.");

                                            packet_func.session_send(p, _session, 1);
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

        public void requestUpdatePCBangMascot(Player session, packet _packet)
        {
            //02 FF FF FF FF 07  00 50 61 6E 67 59 61 21

            //aqui é simples
            byte mode = _packet.ReadByte();
            int mascotTID = _packet.ReadInt32();
            string message = _packet.ReadPStr();

            if (session == null)
            {
                return;
            }

            if (message.Length > 16)
            {
                SendMascotMsgUpdateResult(session, 2, null); // Código 2 = mensagem longa
                return;
            }

            var mascotInfo = session.m_pi.mp_mi.FirstOrDefault(m => m.Key == mascotTID);
            var mascotData = sIff.getInstance().findMascot(mascotInfo.Value._typeid);

            if (mascotInfo.Key == 0 || mascotData == null || !mascotData.msg.active)
            {
                SendMascotMsgUpdateResult(session, 1, mascotInfo.Value); // Código 1 = não encontrado ou inválido
                return;
            }

            // Atualiza apenas localmente
            mascotInfo.Value.message = message;
            SendMascotMsgUpdateResult(session, mode, mascotInfo.Value); // Código 1 = não encontrado ou inválido
        }

        private void SendMascotMsgUpdateResult(Player _session, byte opt, MascotInfo pMi)
        {
            using (var p = new PangyaBinaryWriter(0xE2))
            {
                p.WriteByte(opt); // Update Mascot Message 
                if (opt == 4 || opt == 2)
                {

                    p.WriteInt32(pMi.id); // Mascot ID

                    p.WriteString(pMi.message);

                    p.WriteUInt64(_session.m_pi.ui.pang);
                }
                packet_func.session_send(p,
                    _session, 1);
            }
        }

        public void SQLDBResponse(int _msg_id,
                Pangya_DB _pangya_db,
                    object _arg)
        {

            if (_arg == null)
            {
                return;
            }

            // Por Hora só sai, depois faço outro tipo de tratamento se precisar
            if (_pangya_db.getException().getCodeError() != 0)
            {
                _smp.message_pool.getInstance().push(new message("[Channel::SQLDBResponse][Error] " + _pangya_db.getException().getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                return;
            }

            var _channel = Tools.reinterpret_cast<Channel>(_arg);

            switch (_msg_id)
            {
                case 1: // Update Dolfini Locker Pass
                    {
                        var cmd_udlp = Tools.reinterpret_cast<CmdUpdateDolfiniLockerPass>(_pangya_db);

                        // _smp.message_pool.getInstance().push(new message("[Channel::SQLDBResponse][Sucess] Atualizou a senha[value=" + cmd_udlp.getPass() + "] do Dolfini Locker do PLAYER [UID=" + (cmd_udlp.getUID()) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                        break;
                    }
                case 2: // Update Dolfini Locker Mode
                    {
                        var cmd_udlm = Tools.reinterpret_cast<CmdUpdateDolfiniLockerMode>(_pangya_db);

                        // _smp.message_pool.getInstance().push(new message("[Channel::SQLDBResponse][Sucess] Atualizou o Modo[locker=" + ((ushort)cmd_udlm.getLocker()) + "] do Dolfini Locker do PLAYER [UID=" + (cmd_udlm.getUID()) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                        break;
                    }
                case 3: // Update Dolfini Locker Pang
                    {
                        var cmd_udlp = Tools.reinterpret_cast<CmdUpdateDolfiniLockerPang>(_pangya_db);

                        // _smp.message_pool.getInstance().push(new message("[Channel::SQLDBResponse][Sucess] Atualizou o Pang[value=" + (cmd_udlp.getPang()) + "] do Dolfini Locker do PLAYER [UID=" + (cmd_udlp.getUID()) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                        break;
                    }
                case 4: // Delete Dolfini Locker Item
                    {
                        var cmd_ddli = Tools.reinterpret_cast<CmdDeleteDolfiniLockerItem>(_pangya_db);

                        // _smp.message_pool.getInstance().push(new message("[Channel::SQLDBResponse][Sucess] Deletou o Dolfini Locker Item[index=" + (cmd_ddli.getIndex()) + "] do PLAYER [UID=" + (cmd_ddli.getUID()) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                        break;
                    }
                case 5: // Extend Part Rental
                    {
                        var cmd_er = Tools.reinterpret_cast<CmdExtendRental>(_pangya_db);

                        // _smp.message_pool.getInstance().push(new message("[Channel::SQLDBResponse][Sucess] Extendeu Part Rental[ID=" + (cmd_er.getItemID()) + "] ate o a date[value=" + cmd_er.getDate() + "] para o PLAYER [UID=" + (cmd_er.getUID()) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));
                        break;
                    }
                case 6: // Delete Part Rental
                    {
                        var cmd_dr = Tools.reinterpret_cast<CmdDeleteRental>(_pangya_db);

                        // _smp.message_pool.getInstance().push(new message("[Channel::SQLDBResponse][Sucess] Deletou Part Rental[ID=" + (cmd_dr.getItemID()) + "] do PLAYER [UID=" + (cmd_dr.getUID()) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));
                        break;
                    }
                case 7: // Update Character PCL
                    {
                        var cmd_ucp = Tools.reinterpret_cast<CmdUpdateCharacterPCL>(_pangya_db);

                        // _smp.message_pool.getInstance().push(new message("[Channel::SQLDBResponse][Sucess] Atualizou Character[TYPEID=" + (cmd_ucp.getInfo()._typeid) + ", ID=" + (cmd_ucp.getInfo().id) + "] PCL[C0=" + ((ushort)cmd_ucp.getInfo().pcl[(int)CharacterInfo.Stats.S_POWER]) + ", C1=" + ((ushort)cmd_ucp.getInfo().pcl[(int)CharacterInfo.Stats.S_CONTROL]) + ", C2=" + ((ushort)cmd_ucp.getInfo().pcl[(int)CharacterInfo.Stats.S_ACCURACY]) + ", C3=" + ((ushort)cmd_ucp.getInfo().pcl[(int)CharacterInfo.Stats.S_SPIN]) + ", C4=" + ((ushort)cmd_ucp.getInfo().pcl[(int)CharacterInfo.Stats.S_CURVE]) + "] do PLAYER [UID=" + (cmd_ucp.getUID()) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));
                        break;
                    }
                case 8: // Update ClubSet Stats
                    {
                        var cmd_ucss = Tools.reinterpret_cast<CmdUpdateClubSetStats>(_pangya_db);

                        // _smp.message_pool.getInstance().push(new message("[Channel::SQLDBResponse][Sucess] Atualizou ClubSet[TYPEID=" + (cmd_ucss.getInfo()._typeid) + ", ID=" + (cmd_ucss.getInfo().id) + "] Stats[C0=" + ((ushort)cmd_ucss.getInfo().c[(int)CharacterInfo.Stats.S_POWER]) + ", C1=" + ((ushort)cmd_ucss.getInfo().c[(int)CharacterInfo.Stats.S_CONTROL]) + ", C2=" + ((ushort)cmd_ucss.getInfo().c[(int)CharacterInfo.Stats.S_ACCURACY]) + ", C3=" + ((ushort)cmd_ucss.getInfo().c[(int)CharacterInfo.Stats.S_SPIN]) + ", C4=" + ((ushort)cmd_ucss.getInfo().c[(int)CharacterInfo.Stats.S_CURVE]) + "] do PLAYER [UID=" + (cmd_ucss.getUID()) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));
                        break;
                    }
                case 9: // Update Character Mastery
                    {
                        var cmd_ucm = Tools.reinterpret_cast<CmdUpdateCharacterMastery>(_pangya_db);

                        // _smp.message_pool.getInstance().push(new message("[Channel::SQLDBResponse][Sucess] Atualizou Character[TYPEID=" + (cmd_ucm.getInfo()._typeid) + ", ID=" + (cmd_ucm.getInfo().id) + "] Mastery[value=" + (cmd_ucm.getInfo().mastery) + "] do PLAYER [UID=" + (cmd_ucm.getUID()) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));
                        break;
                    }
                case 10: // Equipa Card
                    {
                        var cmd_ec = Tools.reinterpret_cast<CmdEquipCard>(_pangya_db);

                        // _smp.message_pool.getInstance().push(new message("[Channel::SQLDBResponse][Sucess] Equipou Card[TYPEID=" + (cmd_ec.getInfo()._typeid) + "] no Character[TYPEID=" + (cmd_ec.getInfo().parts_typeid) + ", ID=" + (cmd_ec.getInfo().parts_id) + "] do PLAYER [UID=" + (cmd_ec.getUID()) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));
                        break;
                    }
                case 11: // Desequipa Card
                    {
                        var cmd_rec = Tools.reinterpret_cast<CmdRemoveEquipedCard>(_pangya_db);
                        break;
                    }
                case 12: // Update ClubSet Workshop
                    {
                        var cmd_ucw = Tools.reinterpret_cast<CmdUpdateClubSetWorkshop>(_pangya_db);
                        break;
                    }
                case 13: // Update Tutorial
                    {
                        var cmd_ut = Tools.reinterpret_cast<CmdUpdateTutorial>(_pangya_db);
                        break;
                    }
                case 14: // Tutorial Event Clear
                    {
                        var cmd_tec = Tools.reinterpret_cast<CmdTutoEventClear>(_pangya_db);
                        break;
                    }
                case 15: // Use Item Buff
                    {
                        var cmd_uib = Tools.reinterpret_cast<CmdUseItemBuff>(_pangya_db);
                        break;
                    }
                case 16: // Update Item Buff
                    {
                        var cmd_uib = Tools.reinterpret_cast<CmdUpdateItemBuff>(_pangya_db);

                        //// _smp.message_pool.getInstance().push(new message("[Channel::SQLDBResponse][Sucess] PLAYER [UID=" + (cmd_uib.getUID()) + "] Atualizou o tempo do Item Buff[INDEX=" + (cmd_uib.getInfo().index) + ", TYPEID=" + (cmd_uib.getInfo()._typeid) + ", TIPO=" + (cmd_uib.getInfo().tipo) + ", DATE{REG_DT: " + _formatDate(cmd_uib.getInfo().use_date) + ", END_DT: " + _formatDate(cmd_uib.getInfo().end_date) + "}]", type_msg.CL_FILE_LOG_AND_CONSOLE));
                        break;
                    }
                case 17: // Update Card Special Time
                    {
                        var cmd_ucst = Tools.reinterpret_cast<CmdUpdateCardSpecialTime>(_pangya_db);

                        // // _smp.message_pool.getInstance().push(new message("[Channel::SQLDBResponse][Sucess] PLAYER [UID=" + (cmd_ucst.getUID()) + "] Atualizou o tempo do Card Special[index=" + (cmd_ucst.getInfo().index) + ", TYPEID=" + (cmd_ucst.getInfo()._typeid) + ", EFEITO{TYPE: " + (cmd_ucst.getInfo().efeito) + ", QNTD: " + (cmd_ucst.getInfo().efeito_qntd) + "}, TIPO=" + (cmd_ucst.getInfo().tipo) + ", DATE{REG_DT: " + _formatDate(cmd_ucst.getInfo().use_date) + ", END_DT: " + _formatDate(cmd_ucst.getInfo().end_date) + "}]", type_msg.CL_FILE_LOG_AND_CONSOLE));
                        break;
                    }
                case 18: // Update Player Papel Shop Limit
                    {
                        var cmd_upsl = Tools.reinterpret_cast<CmdUpdatePapelShopInfo>(_pangya_db);

                        // _smp.message_pool.getInstance().push(new message("[Channel::SQLDBResponse][Sucess] PLAYER [UID=" + (cmd_upsl.getUID()) + "] Atualizou o Papel Shop Limit[current_cnt=" + (cmd_upsl.getInfo().current_count) + ", remain_cnt=" + (cmd_upsl.getInfo().remain_count) + ", limit_cnt=" + (cmd_upsl.getInfo().limit_count) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));
                        break;
                    }
                case 19: // Insert Papel Shop Rare Win Log
                    {
                        var cmd_ipsrwl = Tools.reinterpret_cast<CmdInsertPapelShopRareWinLog>(_pangya_db);

                        // _smp.message_pool.getInstance().push(new message("[Channel::SQLDBResponse][Sucess] PLAYER [UID=" + (cmd_ipsrwl.getUID()) + "] Adicionou Papel Shop Rare Win Log[TYPEID=" + (cmd_ipsrwl.getInfo().ctx_psi._typeid) + ", QNTD=" + (cmd_ipsrwl.getInfo().qntd) + ", COLOR=" + (cmd_ipsrwl.getInfo().color) + ", PROBABILIDADE=" + (cmd_ipsrwl.getInfo().ctx_psi.probabilidade) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));
                        break;
                    }
                case 20: // Pay Caddie Holy Day (Paga as ferias do Caddie)
                    {
                        var cmd_pchd = Tools.reinterpret_cast<CmdPayCaddieHolyDay>(_pangya_db);

                        // _smp.message_pool.getInstance().push(new message("[Channel::SQLDBResponse][Sucess] PLAYER [UID=" + (cmd_pchd.getUID()) + "] Pagou as ferias do Caddie[ID=" + (cmd_pchd.getId()) + "] ate " + cmd_pchd.getEndDate(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                        break;
                    }
                case 21: // Set Notice Caddie Holy Day (Seta Aviso de ferias do Caddie)
                    {
                        var cmd_snchd = Tools.reinterpret_cast<CmdSetNoticeCaddieHolyDay>(_pangya_db);

                        //_smp.message_pool.getInstance().push(new message("[Channel::SQLDBResponse][Sucess] PLAYER [UID=" + (cmd_snchd.getUID()) + "] setou Aviso[check=" + (cmd_snchd.getCheck() ? "ON" : "OFF") + "] de ferias do Caddie[ID=" + (cmd_snchd.getId()) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));
                        break;
                    }
                case 22: // Insert Box Rare Win Log
                    {
                        var cmd_ibrwl = Tools.reinterpret_cast<CmdInsertBoxRareWinLog>(_pangya_db);

                        // _smp.message_pool.getInstance().push(new message("[Channel::SQLDBResponse][Sucess] PLAYER [UID=" + (cmd_ibrwl.getUID()) + "] Inseriu Box[TYPEID=" + (cmd_ibrwl.getBoxTypeid()) + "] Rare[TYPEID=" + (cmd_ibrwl.getInfo()._typeid) + ", QNTD=" + (cmd_ibrwl.getInfo().qntd) + ", RARIDADE=" + ((ushort)cmd_ibrwl.getInfo().raridade) + "] Win Log", type_msg.CL_FILE_LOG_AND_CONSOLE));
                        break;
                    }
                case 23: // Insert Spinning Cube Super Rare Win Broadcast
                    {
                        var cmd_ispcsrwb = Tools.reinterpret_cast<CmdInsertSpinningCubeSuperRareWinBroadcast>(_pangya_db);

                        // _smp.message_pool.getInstance().push(new message("[Channel::SQLDBResponse][Sucess] Inseriu Spinning Cube Super Rare Win Broadcast[MSG=" + cmd_ispcsrwb.getMessage() + ", OPT=" + ((ushort)cmd_ispcsrwb.getOpt()) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));
                        break;
                    }
                case 24: // Insert Memorial Shop Rare Win Log
                    {
                        var cmd_imrwl = Tools.reinterpret_cast<CmdInsertMemorialRareWinLog>(_pangya_db);

                        // _smp.message_pool.getInstance().push(new message("[Channel::SQLDBResponse][Sucess] PLAYER [UID=" + (cmd_imrwl.getUID()) + "] Inseriu Memorial Shop[COIN=" + (cmd_imrwl.getCoinTypeid()) + "] Rare[TYPEID=" + (cmd_imrwl.getInfo()._typeid) + ", QNTD=" + (cmd_imrwl.getInfo().qntd) + ", RARIDADE=" + (cmd_imrwl.getInfo().tipo) + "] Win Log", type_msg.CL_FILE_LOG_AND_CONSOLE));
                        break;
                    }
                case 26: // Update Mascot Info
                    {

                        var cmd_umi = Tools.reinterpret_cast<CmdUpdateMascotInfo>(_pangya_db);

                        //// _smp.message_pool.getInstance().push(new message("[Channel::SQLDBResponse][Sucess] PLAYER [UID=" + (cmd_umi.getUID()) + "] Atualizar Mascot Info[TYPEID=" + (cmd_umi.getInfo()._typeid) + ", ID=" + (cmd_umi.getInfo().id) + ", LEVEL=" + ((ushort)cmd_umi.getInfo().level) + ", EXP=" + (cmd_umi.getInfo().exp) + ", FLAG=" + ((ushort)cmd_umi.getInfo().type) + ", TIPO=" + (cmd_umi.getInfo().tipo) + ", IS_CASH=" + ((ushort)cmd_umi.getInfo().is_cash) + ", PRICE=" + (cmd_umi.getInfo().price) + ", MESSAGE=" + (cmd_umi.getInfo().message) + ", END_DT=" + _formatDate(cmd_umi.getInfo().data) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                        break;
                    }
                case 27: // Atualizou Guild Update Activity
                    {
                        // var cmd_uguai = Tools.reinterpret_cast<CmdUpdateGuildUpdateActiviy>(_pangya_db);

                        //_smp.message_pool.getInstance().push(new message("[Channel::SQLDBResponse][Sucess] Atualizou Guild Update Activity[INDEX=" + (cmd_uguai.getIndex()) + "] com sucesso.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                        break;
                    }
                case 28: // Atualizou Legacy Tiki Shop Point
                    {
                        var cmd_ultp = Tools.reinterpret_cast<CmdUpdateLegacyTikiShopPoint>(_pangya_db);

                        // _smp.message_pool.getInstance().push(new message("[Channel::SQLDBResponse][Sucess] PLAYER [UID=" + (cmd_ultp.getUID()) + "] atualizou Legacy Tiki Shop Point(" + (cmd_ultp.getTikiShopPoint()) + ")", type_msg.CL_FILE_LOG_AND_CONSOLE));

                        break;
                    }
                case 0:
                default: // 25 é update item equipado slot
                    break;
            }

        }

        public void FilterRoom(Player _session, RoomInfoEx ri)
        {
            try
            {
                new FilterRoom(_session, ri, m_ci);
            }
            catch (exception e)
            {
                _smp.message_pool.getInstance().push(new message("[Channel::FilterRoom][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }
    }
}
