using Pangya_GameServer.Game.Base;
using Pangya_GameServer.Game.GameModes;
using Pangya_GameServer.Game.Manager;
using Pangya_GameServer.Models;
using Pangya_GameServer.PacketFunc;
using Pangya_GameServer.Repository;
using Pangya_GameServer.UTIL;
using PangyaAPI.IFF.JP.Extensions;
using PangyaAPI.IFF.JP.Models.Flags;
using PangyaAPI.Network.Models;
using PangyaAPI.Network.PangyaPacket;
using PangyaAPI.Network.Repository;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Models;
using PangyaAPI.Utilities.Log;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;
using static Pangya_GameServer.Models.DefineConstants;
using int64_t = System.Int64;
using uint32_t = System.UInt32;
namespace Pangya_GameServer.Game
{
    /// <summary>
    /// class room, finalization 5/7/25
    /// </summary> 
    public class Room : IDisposable
    {
        protected List<Player> v_sessions { get; set; } = new List<Player>();
        protected List<Player> m_listGallery = new List<Player>();
        protected Dictionary<Player, PlayerRoomInfoEx> m_player_info = new Dictionary<Player, PlayerRoomInfoEx>();
        protected Dictionary<uint, bool> m_player_kicked = new Dictionary<uint, bool>();
        public object m_lock_cs = new object();        // Bloquea a sala 
        public object m_cs = new object();       // Bloquea a sala 
        protected PersonalShopManager m_personal_shop;

        protected List<Team> m_teans = new List<Team>();

        protected GuildRoomManager m_guild_manager = new GuildRoomManager();

        protected List<InviteChannelInfo> v_invite = new List<InviteChannelInfo>();

        protected RoomInfoEx m_ri = new RoomInfoEx();

        protected byte m_channel_owner; // Id do Canal dono da sala

        protected bool m_bot_tourney; // Bot para começa o Modo tourney só com 1 jogador
        private int m_lock_spin_state;
        protected bool m_destroying;

        protected GameBase m_pGame;
        // Room Tipo Lounge
        protected byte m_weather_lounge;
        public RoomInfoLog m_room_log;
        private bool disposedValue;

        public Room(byte _channel_owner, RoomInfoEx _ri)
        {
            this.m_ri = _ri;
            this.m_pGame = null;
            this.m_channel_owner = _channel_owner;
            this.m_teans = new List<Team>();
            this.m_weather_lounge = 0;
            this.m_destroying = false;
            this.m_bot_tourney = false;
            this.m_lock_spin_state = 01;
            this.m_personal_shop = new PersonalShopManager(m_ri);
            this.m_room_log = new RoomInfoLog(m_ri);

            geraSecurityKey();

            // Calcula chuva(weather) se o tipo da sala for lounge
            calcRainLounge();

            // Atualiza tipo da sala
            setTipo(m_ri.tipo);

            // Att Exp rate, e Pang rate, que criou a sala, att ele também quando começa o jogo

            m_ri.rate_exp = (uint)sgs.gs.getInstance().getInfo().rate.exp;
            m_ri.rate_pang = (uint)sgs.gs.getInstance().getInfo().rate.pang;
            m_ri.angel_event = sgs.gs.getInstance().getInfo().rate.angel_event == 1 ? true : false;
        }

        private void geraSecurityKey()
        {
            new Random().NextBytes(m_ri.key);
        }

        private void clear_Player_kicked()
        {
            if (m_player_kicked.Any())
                m_player_kicked.Clear();
        }

        public void enter(Player _session)
        {

            if (!_session.getState())
            {
                throw new exception("[room::enter] [Error] Player nao esta connectado.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    4, 0));
            }



            if (isFull())
            {
                throw new exception("[room::enter] [Error] PLAYER[UID=" + _session.m_pi.uid + "] tentou entrar na a sala[NUMERO=" + m_ri.numero + "], mas a sala ja esta cheia.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    2, 0));
            }

            if (_session.m_pi.mi.sala_numero != ushort.MaxValue)
            {
                throw new exception("[room::enter] [Error] PLAYER[UID=" + _session.m_pi.uid + "] sala[NUMERO=" + m_ri.numero + "], ja esta em outra sala[NUMERO=" + Convert.ToString(_session.m_pi.mi.sala_numero) + "], nao pode entrar em outra. Hacker ou Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    120, 0));
            }

            if (m_ri.getTipo() == RoomInfo.ROOM_INFO_TYPE.GUILD_BATTLE
                && m_ri.guilds.guild_1_uid != 0
                && m_ri.guilds.guild_2_uid != 0
                && m_ri.guilds.guild_1_uid != _session.m_pi.gi.uid
                && m_ri.guilds.guild_2_uid != _session.m_pi.gi.uid)
            {
                throw new exception("[room::enter] [Error] PLAYER[UID=" + _session.m_pi.uid + "] sala[NUMERO=" + m_ri.numero + "], ja tem duas guild e o Player que quer entrar nao é de nenhum delas. Hacker ou Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    11000, 0));
            }

            try
            {

                _session.m_pi.mi.sala_numero = m_ri.numero;

                // Update Place Player
                if (m_ri.getTipo() == RoomInfo.ROOM_INFO_TYPE.PRACTICE || m_ri.getTipo() == RoomInfo.ROOM_INFO_TYPE.GRAND_ZODIAC_PRACTICE)
                {
                    _session.m_pi.place = 2;
                }
                else
                {
                    _session.m_pi.place = 0;
                }

                v_sessions.Add(_session);

                m_ri.num_player = (byte)v_sessions.Count;

                // Update Trofel
                if (m_ri.trofel > 0)
                {
                    updateTrofel();
                }

                // Acabou de criar a sala
                if (m_ri.master == _session.m_pi.uid && m_ri.tipo != (byte)RoomInfo.ROOM_INFO_TYPE.GRAND_PRIX)
                {
                    // Update Trofel
                    if (_session.m_pi.m_cap.game_master)
                    { // GM

                        if ((m_ri.max_player > 30 && m_ri.getTipo() == RoomInfo.ROOM_INFO_TYPE.TOURNEY) || (m_ri.tipo >= (byte)RoomInfo.ROOM_INFO_TYPE.GRAND_ZODIAC_INT && m_ri.tipo <= (byte)RoomInfo.ROOM_INFO_TYPE.GRAND_ZODIAC_ADV))
                        {

                            m_ri.flag_gm = 1;

                            m_ri.state_flag = 0x100;

                            m_ri.trofel = TROFEL_GM_EVENT_TYPEID;

                        }
                        else if (m_ri.getTipo() == RoomInfo.ROOM_INFO_TYPE.TOURNEY || m_ri.tipo >= (byte)RoomInfo.ROOM_INFO_TYPE.GRAND_ZODIAC_INT)
                        {
                            updateTrofel();
                        }

                    }
                    else if (m_ri.getTipo() == RoomInfo.ROOM_INFO_TYPE.TOURNEY || m_ri.tipo >= (byte)RoomInfo.ROOM_INFO_TYPE.GRAND_ZODIAC_INT)
                    {
                        updateTrofel();
                    }

                }
                else if (m_ri.getTipo() == RoomInfo.ROOM_INFO_TYPE.GRAND_PRIX)
                {
                    updateTrofel();
                }

                // Update Master
                // Só trocar o master da sala se não tiver nenhum jogo inicializado
                if (m_pGame == null
                    && v_sessions.Count > 0
                    && _session.m_pi.m_cap.game_master
                    && m_ri.state_flag != 0x100
                    && m_ri.tipo != (byte)RoomInfo.ROOM_INFO_TYPE.SPECIAL_SHUFFLE_COURSE
                    && m_ri.tipo != (byte)RoomInfo.ROOM_INFO_TYPE.GRAND_PRIX)
                {
                    updateMaster(_session);
                }

                // Add o Player ao jogo
                if (m_pGame != null)
                {
                    m_pGame.addPlayer(_session);

                    if (m_ri.trofel > 0)
                    {
                        updateTrofel();
                    }
                }

                try
                {
                    // Make Info Room Player
                    makePlayerInfo(_session);

                }
                catch (exception e)
                {
                    _smp.message_pool.getInstance().push(new message("[room::enter][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                }

                if (m_ri.getTipo() == RoomInfo.ROOM_INFO_TYPE.GUILD_BATTLE)
                {
                    updateGuild(_session);
                }

                //room is player
                _session.m_room = this;
            }
            catch (exception e)
            {
                _smp.message_pool.getInstance().push(new message("[room::enter][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        protected virtual PlayerRoomInfoEx makePlayerInfo(Player _session)
        {
            PlayerRoomInfoEx pri = new PlayerRoomInfoEx();

            // Player Room Info Init
            pri.oid = _session.m_oid;
            pri.nickname = _session.m_pi.nickname;
            pri.guild_name = _session.m_pi.gi.name;
            pri.position = (byte)(getPosition(_session) + 1);
            pri.capability = _session.m_pi.m_cap;
            pri.title = _session.m_pi.ue.m_title;
            pri.sDisplayID = "@NT_" + _session.m_pi.nickname;
            if (_session.m_pi.ei.char_info != null)
                pri.char_typeid = _session.m_pi.ei.char_info._typeid;

            pri.skin = _session.m_pi.ue.skin_typeid;

            pri.skin[4] = 0;

            if (getMaster() == _session.m_pi.uid)
            {
                pri.state_flag.master = 1;
                pri.state_flag.ready = 1;// Sempre está pronto(ready) o master
            }
            pri.state_flag.sexo = _session.m_pi.mi.sexo;

            if (m_ri.getTipo() == RoomInfo.ROOM_INFO_TYPE.MATCH)
            {
                if (v_sessions.Count > 1)
                {
                    if (m_teans[0].getCount() >= 2 && m_teans[1].getCount() >= 2)
                    {
                        throw new exception("[room::makePlayerInfo] [Error] PLAYER[UID=" + _session.m_pi.uid + "] tentou entrar em time para todos os times da sala estao cheios. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM, 1500, 0));
                    }
                    else if (m_teans[0].getCount() >= 2)
                    {
                        pri.state_flag.team = 1;
                    }
                    else if (m_teans[1].getCount() >= 2)
                    {
                        pri.state_flag.team = 0;
                    }
                    else
                    {
                        var targetSession = (v_sessions.Count == 2) ? v_sessions[0] : (v_sessions.Count > 2 ? v_sessions[1] : null);
                        var pPri = getPlayerInfo(targetSession);

                        if (pPri == null)
                        {
                            throw new exception("[room::makePlayerInfo] [Error] PLAYER[UID=" + _session.m_pi.uid + "] tentou entrar em um time, mas o ultimo player da sala, nao tem um info no sala. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM, 1501, 0));
                        }

                        pri.state_flag.team = (byte)~pPri.state_flag.team;
                    }
                }
                else
                {
                    pri.state_flag.team = 0;
                }

                m_teans[pri.state_flag.team].addPlayer(_session);
            }
            else if (m_ri.getTipo() != RoomInfo.ROOM_INFO_TYPE.GUILD_BATTLE)
            {
                pri.state_flag.team = (byte)((pri.position - 1) % 2);
            }

            if (_session.m_pi.level >= 6 && _session.m_pi.ui.jogado >= 50)
            {
                float rate = _session.m_pi.ui.getQuitRate();

                if (rate < GOOD_PLAYER_ICON)
                {
                    pri.state_flag.azinha = 1;
                }
                else if (rate >= QUITER_ICON_1 && rate < QUITER_ICON_2)
                {
                    pri.state_flag.quiter_1 = 1;
                }
                else if (rate >= QUITER_ICON_2)
                {
                    pri.state_flag.quiter_2 = 1;
                }
            }

            pri.level = _session.m_pi.mi.level;

            if (_session.m_pi.ei.char_info != null && _session.m_pi.ui.getQuitRate() < GOOD_PLAYER_ICON)
                pri.icon_angel = _session.m_pi.ei.char_info.AngelEquiped();
            else
                pri.icon_angel = 0;

            pri.place.ulPlace = 10;
            pri.guild_uid = _session.m_pi.gi.uid;
            pri.guild_mark_img = _session.m_pi.gi.mark_emblem;
            pri.guild_mark_index = _session.m_pi.gi.index_mark_emblem;
            pri.uid = _session.m_pi.uid;
            pri.state_action.state_lounge = _session.m_pi.state_lounge;
            pri.state_action.usUnknown_flg = 0;
            pri.state_action.state = _session.m_pi.state;

            pri.location = new PlayerRoomInfo.stLocation
            {
                x = _session.m_pi.location.x,
                z = _session.m_pi.location.z,
                r = _session.m_pi.location.r
            };

            pri.shop = m_personal_shop.getPersonShop(_session);

            if (_session.m_pi.ei.mascot_info != null)
                pri.mascot_typeid = _session.m_pi.ei.mascot_info._typeid;

            pri.flag_item_boost = _session.m_pi.checkEquipedItemBoost();
            pri.channeling_flag = 0;
            pri.convidado = 0;
            pri.avg_score = _session.m_pi.ui.getMediaScore();

            if (_session.m_pi.ei.char_info != null)
                pri.ci = _session.m_pi.ei.char_info;
            if (!m_player_info.TryAdd(_session, pri))
            {
                if (m_player_info.TryGetValue(_session, out var existingPri))
                {
                    if (existingPri.uid != _session.m_pi.uid)
                    {
                        try
                        {
                            var pri_ant = m_player_info[_session];
                            m_player_info[_session] = pri;
                        }
                        catch (IndexOutOfRangeException e)
                        {
                            _smp.message_pool.getInstance().push(new message($"[room::makePlayerInfo] [Error][Warning] PLAYER[UID={_session.m_pi.uid}], nao conseguiu atualizar o PlayerRoomInfo da session para o novo PlayerRoomInfo do player atual da session. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                            throw e;
                        }
                    }
                    else
                    {
                        _smp.message_pool.getInstance().push(new message($"[room::makePlayerInfo][Info] PLAYER[UID={_session.m_pi.uid}] nao conseguiu adicionar o PlayerRoomInfo da session, por que ja tem o mesmo PlayerRoomInfo no map.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                    }
                }
                else
                {
                    _smp.message_pool.getInstance().push(new message($"[room::makePlayerInfo] [Error] nao conseguiu inserir o pair de PlayerInfo do PLAYER[UID={_session.m_pi.uid}] no map de player info do room. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
            }

            return pri;
        }

        protected PlayerRoomInfoEx makePlayerInvitedInfo(Player _session)
        {

            PlayerRoomInfoEx pri = new PlayerRoomInfoEx();

            // Player Room Info Init
            pri.oid = _session.m_oid;
            pri.position = (byte)(getPosition(_session) + 1); // posição na sala

            pri.place.ulPlace = 10; // 0x0A dec"10" _session.m_pi.place, pode ser lugar[place]

            pri.uid = _session.m_pi.uid;

            pri.convidado = 1; // Flag Convidado, [Não sei bem por que os que entra na sala normal tem valor igual aqui, já que é type de convidado waiting], Valor constante da sala para os players(ACHO)

            // Check inset pair in map of room player info
            if (m_player_info.ContainsKey(_session))
            {
                try
                {

                    // pega o antigo PlayerRoomInfo para usar no Log
                    var pri_ant = m_player_info[_session];

                    // Novo PlayerRoomInfo
                    m_player_info[_session] = pri;

                }
                catch (IndexOutOfRangeException e)
                {
                    _smp.message_pool.getInstance().push(new message("[room::makePlayerInfo] [Error][Warning] PLAYER[UID=" + _session.m_pi.uid + "], nao conseguiu atualizar o PlayerRoomInfo da session para o novo PlayerRoomInfo do player atual da session. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                    throw e;
                }
            }
            else
                m_player_info.Add(_session, pri);

            return pri;
        }


        protected void updatePosition()
        {
            for (int i = 0; i < v_sessions.Count; ++i)//255 e o limite
            {
                m_player_info[v_sessions[i]].position = (byte)Math.Min(i + 1, byte.MaxValue);
            }
        }


        private void updateTrofel()
        {

            if (v_sessions.Count() > 0 && (m_ri.trofel != TROFEL_GM_EVENT_TYPEID || m_ri.max_player <= 30) && (m_ri.time_30s > 0 && m_ri.tipo != (byte)RoomInfo.ROOM_INFO_TYPE.GUILD_BATTLE)
                && m_ri.master != -2 || (m_ri.getTipo() == RoomInfo.ROOM_INFO_TYPE.GRAND_PRIX && m_ri.grand_prix.dados_typeid > 0))
            {

                if (m_pGame != null)
                    m_pGame.requestUpdateTrofel();
                else
                {

                    uint32_t soma = 0;

                    foreach (var _el in v_sessions)
                    {
                        if (_el != null)
                            soma += (uint)((_el.m_pi.level > 60) ? 60 : (_el.m_pi.level > 0 ? _el.m_pi.level - 1 : 0));

                    }
                    uint32_t new_trofel = STDA_MAKE_TROFEL(soma, v_sessions.Count());

                    if (new_trofel > 0 && new_trofel != m_ri.trofel)
                    {

                        // Check se o trofeu anterior era o GM e se o novo não é mais, aí tira a type de GM da sala
                        if (m_ri.trofel == TROFEL_GM_EVENT_TYPEID && new_trofel != TROFEL_GM_EVENT_TYPEID)
                            m_ri.flag_gm = 0;

                        if (m_ri.trofel > 0)
                        {

                            m_ri.trofel = new_trofel;

                            var p = new PangyaBinaryWriter(0x97);

                            p.WriteUInt32(m_ri.trofel);

                            packet_func.room_broadcast(this, p, 1);

                        }
                        else
                            m_ri.trofel = new_trofel;
                    }
                }
            }
        }

        public int leave(Player _session, int _option)
        {
            try
            {
                // 1. Localiza o jogador na lista interna
                int index = findIndexSession(_session);

                if (index == -1)
                {
                    // Se o jogador não existe, logamos o erro mas verificamos se a sala deve fechar
                    // Isso evita que uma sala "bugada" fique aberta para sempre
                    _smp.message_pool.getInstance().push(new message("[room::leave][Warning] Sessão não encontrada na sala " + m_ri.numero, type_msg.CL_FILE_LOG_AND_CONSOLE));
                    return (v_sessions.Count > 0) ? 0 : 1;
                }

                // 2. Lógica de Kick (Opções que não são saída voluntária)
                if (_option != 0 && _option != 1 && _option != 0x800 && _option != 10)
                {
                    addPlayerKicked(_session.m_pi.uid);
                }

                // 3. Verifica se ele está em jogo e remove da partida
                try
                {
                    if (m_pGame != null)
                    {
                        if (m_pGame.deletePlayer(_session, _option) && m_pGame.finish_game(_session, 2))
                        {
                            finish_game();
                        }
                    }
                }
                catch (exception e)
                {
                    _smp.message_pool.getInstance().push(new message("[room::leave][Error Game] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                }

                // 4. REMOÇÃO REAL DA LISTA
                if (v_sessions != null && index >= 0 && index < v_sessions.Count)
                {
                    v_sessions.RemoveAt(index);
                }

                // 5. ATUALIZAÇÃO DO CONTADOR (Aqui estava o bug)
                // Se a lista está vazia, o contador TEM que ser 0.
                if (v_sessions.Count == 0)
                {
                    m_ri.num_player = 0;
                }
                else if (m_ri.num_player > 0)
                {
                    --m_ri.num_player;
                }

                // 6. Lógica de Times (Match e Guild Battle)
                var pPri = getPlayerInfo(_session);
                if (pPri != null)
                {
                    if (m_ri.getTipo() == RoomInfo.ROOM_INFO_TYPE.MATCH)
                    {
                        if (m_teans.Count >= 2 && pPri.state_flag.team < m_teans.Count)
                        {
                            m_teans[pPri.state_flag.team].deletePlayer(_session, _option);
                        }
                    }
                    else if (m_ri.getTipo() == RoomInfo.ROOM_INFO_TYPE.GUILD_BATTLE)
                    {
                        var guild = m_guild_manager.findGuildByPlayer(_session);
                        if (guild != null)
                        {
                            guild.deletePlayer(_session);
                            if (pPri.state_flag.team < m_teans.Count)
                                m_teans[pPri.state_flag.team].deletePlayer(_session, _option);

                            if (guild.numPlayers() == 0)
                            {
                                if (guild.getTeam() == Guild.eTEAM.RED) { m_ri.guilds.guild_1_uid = 0; m_ri.guilds.guild_1_index_mark = 0; }
                                else { m_ri.guilds.guild_2_uid = 0; m_ri.guilds.guild_2_index_mark = 0; }
                                m_guild_manager.deleteGuild(guild);
                            }
                        }
                    }
                    // Limpa Info do Player na sala
                    m_player_info.Remove(_session);
                }

                // 7. Reset de status do Player
                _session.m_pi.mi.sala_numero = ushort.MaxValue;
                _session.m_pi.place = 0;
                m_personal_shop.destroyShop(_session);

                if (m_ri.getTipo() == RoomInfo.ROOM_INFO_TYPE.LOUNGE)
                {
                    _session.m_pi.state = 0;
                    _session.m_pi.state_lounge = 0;
                }

                // 8. Notificações para os que ficaram
                if (v_sessions.Count > 0)
                {
                    updatePosition();
                    updateTrofel();
                    sendUpdate();
                    sendPlayerInfo(_session, 2);

                    if (m_pGame == null)
                    {
                        updateMaster(null); // Eleger novo mestre se necessário
                    }
                }

                // 9. Envio de pacote de Kick/Saída para o cliente que saiu
                if (_option == 0x800 || (_option != 0 && _option != 1 && _option != 3))
                {
                    uint opt_kick = (_option == 1) ? 4u : (_option == 2) ? 2u : (uint)_option;
                    var p = new PangyaBinaryWriter((ushort)0x7E);
                    p.WriteUInt32(opt_kick);
                    packet_func.session_send(p, _session, 1);
                }

                // 10. CONDIÇÃO DE FECHAMENTO (O veredito final)

                // Regra de Master GM ou Shuffle Course (Fecha tudo imediatamente)
                if ((m_pGame == null && m_ri.getTipo() == RoomInfo.ROOM_INFO_TYPE.SPECIAL_SHUFFLE_COURSE && _session.m_pi.uid == m_ri.master) ||
                    (_session.m_pi.m_cap.game_master && m_ri.master == _session.m_pi.uid && m_ri.tipo != (byte)RoomInfo.ROOM_INFO_TYPE.LOUNGE && m_ri.trofel == TROFEL_GM_EVENT_TYPEID))
                {
                    return 0x801;
                }

                // Se a sala ficou vazia, retorna 1 para o ServerManager destruir o objeto da sala
                if (v_sessions.Count == 0)
                {
                    // Verifica se não é uma sala persistente (ex: Grand Zodiac)
                    bool isPersistent = (m_ri.master == -2 && (!isDropRoom() || (m_ri.tipo >= (byte)RoomInfo.ROOM_INFO_TYPE.GRAND_ZODIAC_INT && m_ri.tipo <= (byte)RoomInfo.ROOM_INFO_TYPE.GRAND_ZODIAC_ADV)));

                    return isPersistent ? 0 : 1;
                }

            }
            catch (exception e)
            {
                _smp.message_pool.getInstance().push(new message("[room::leave][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            // Se chegou aqui, ainda há jogadores na sala
            return 0;
        }

        public void sendMake(Player _session)
        {
            var p = packet_func.pacote049(this, 0);
            packet_func.session_send(p, _session, 0);
        }

        public void sendUpdate()
        {
            var p = packet_func.pacote04A(m_ri, -1/*valor constante*/);
            packet_func.room_broadcast(this, p, 0);
        }

        private void addPlayerKicked(uint _uid)
        {
            if (isKickedPlayer(_uid))
                _smp.message_pool.getInstance().push(new message("[room::addPlayerKicked] [Error][Warning] PLAYER[UID=" + (_uid) + "] ja foi chutado da sala[NUMERO="
                    + (m_ri.numero) + "]", type_msg.CL_FILE_TIME_LOG_AND_CONSOLE));
            else
                m_player_kicked[_uid] = true;
        }

        public static void SQLDBResponse(int _msg_id,
                Pangya_DB _pangya_db,
                object _arg)
        {

            if (_arg == null)
            {
                _smp.message_pool.getInstance().push(new message("[room::SQLDBResponse][Warning] _arg is null com msg_id = " + Convert.ToString(_msg_id), type_msg.CL_FILE_LOG_AND_CONSOLE));
                return;
            }

            // Por Hora só sai, depois faço outro tipo de tratamento se precisar
            if (_pangya_db.getException().getCodeError() != 0)
            {
                _smp.message_pool.getInstance().push(new message("[room::SQLDBResponse] [Error] " + _pangya_db.getException().getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                return;
            }
            Channel _channel = null;
            Room _room = null;

            if (_arg is Channel ch)
                _channel = ch;

            if (_arg is Room r)
                _room = r;

            switch (_msg_id)
            {
                case 7: // Update Character PCL
                    {
                        var cmd_ucp = (CmdUpdateCharacterPCL)(_pangya_db);
                        break;
                    }
                case 8: // Update ClubSet Stats
                    {
                        var cmd_ucss = (CmdUpdateClubSetStats)(_pangya_db);

                        break;
                    }
                case 9: // Update Character Mastery
                    {
                        var cmd_ucm = (CmdUpdateCharacterMastery)(_pangya_db);

                        _smp.message_pool.getInstance().push(new message("[room::SQLDBResponse][Info] Atualizou Character[TYPEID=" + Convert.ToString(cmd_ucm.getInfo()._typeid) + ", ID=" + Convert.ToString(cmd_ucm.getInfo().id) + "] Mastery[value=" + Convert.ToString(cmd_ucm.getInfo().mastery) + "] do PLAYER[UID=" + Convert.ToString(cmd_ucm.getUID()) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));
                        break;
                    }
                case 12: // Update ClubSet Workshop
                    {
                        var cmd_ucw = (CmdUpdateClubSetWorkshop)(_pangya_db);

                        _smp.message_pool.getInstance().push(new message("[room::SQLDBResponse][Info] PLAYER[UID=" + Convert.ToString(cmd_ucw.getUID()) + "] Atualizou ClubSet[TYPEID=" + Convert.ToString(cmd_ucw.getInfo()._typeid) + ", ID=" + Convert.ToString(cmd_ucw.getInfo().id) + "] Workshop[C0=" + Convert.ToString(cmd_ucw.getInfo().clubset_workshop.c[0]) + ", C1=" + Convert.ToString(cmd_ucw.getInfo().clubset_workshop.c[1]) + ", C2=" + Convert.ToString(cmd_ucw.getInfo().clubset_workshop.c[2]) + ", C3=" + Convert.ToString(cmd_ucw.getInfo().clubset_workshop.c[3]) + ", C4=" + Convert.ToString(cmd_ucw.getInfo().clubset_workshop.c[4]) + ", Level=" + Convert.ToString(cmd_ucw.getInfo().clubset_workshop.level) + ", Mastery=" + Convert.ToString(cmd_ucw.getInfo().clubset_workshop.mastery) + ", Rank=" + Convert.ToString(cmd_ucw.getInfo().clubset_workshop.rank) + ", Recovery=" + Convert.ToString(cmd_ucw.getInfo().clubset_workshop.recovery_pts) + "] Flag=" + Convert.ToString(cmd_ucw.getFlag()) + "", type_msg.CL_FILE_LOG_AND_CONSOLE));
                        break;
                    }
                case 26: // Update Mascot Info
                    {

                        var cmd_umi = (CmdUpdateMascotInfo)(_pangya_db);

                        _smp.message_pool.getInstance().push(new message("[room::SQLDBResponse][Info] PLAYER[UID=" + Convert.ToString(cmd_umi.getUID()) + "] Atualizar Mascot Info[TYPEID=" + Convert.ToString(cmd_umi.getInfo()._typeid) + ", ID=" + Convert.ToString(cmd_umi.getInfo().id) + ", LEVEL=" + Convert.ToString((ushort)cmd_umi.getInfo().level) + ", EXP=" + Convert.ToString(cmd_umi.getInfo().exp) + ", FLAG=" + Convert.ToString((ushort)cmd_umi.getInfo().flag) + ", TIPO=" + Convert.ToString(cmd_umi.getInfo().tipo) + ", IS_CASH=" + Convert.ToString((ushort)cmd_umi.getInfo().is_cash) + ", PRICE=" + Convert.ToString(cmd_umi.getInfo().price) + ", MESSAGE=" + cmd_umi.getInfo().message + ", END_DT=" + UtilTime.FormatDate(cmd_umi.getInfo().data.ConvertTime()) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                        break;
                    }
                case 0:
                default: // 25 é update item equipado slot
                    break;
            }
        }


        protected void calcRainLounge()
        {

            // Só calcRainLounge se for lounge
            if (m_ri.getTipo() == RoomInfo.ROOM_INFO_TYPE.LOUNGE)
            {

                m_weather_lounge = 0; // Good Weather

                short rate_rain = sgs.gs.getInstance().getInfo().rate.chuva;

                Lottery loterry = new Lottery();

                uint rate_good_weather = (uint)((rate_rain <= 0) ? 1000 : ((rate_rain < 1000) ? 1000 - rate_rain : 1));

                loterry.Push(rate_good_weather, 0);
                loterry.Push(rate_good_weather, 0);
                loterry.Push(rate_good_weather, 0);
                loterry.Push((uint)rate_rain, 2);

                var lc = loterry.spinRoleta();

                if (lc != null && Convert.ToInt32(lc.Value) > 0)
                {
                    m_weather_lounge = (byte)Convert.ToInt32(lc.Value);
                }
            }
        }


        protected void clear_teans()
        {
            if (m_teans.Any())
            {
                m_teans.Clear();
            }
        }

        // Add Bot Tourney Visual to Room
        protected void addBotVisual(Player _session)
        {
            if (!_session.getState())
            {
                throw new exception("[room::addBotVisual] [Error] Player nao esta connectado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }

            // Add Bot
            List<PlayerRoomInfoEx> v_element = new List<PlayerRoomInfoEx>();
            PlayerRoomInfoEx pri = new PlayerRoomInfoEx();

            try
            {
                v_sessions.ForEach(_el =>
                {
                    var tmp_pri = getPlayerInfo(_el);
                    if (tmp_pri != null)
                    {
                        v_element.Add(tmp_pri);
                    }
                });


                if (v_element.Count == 0)
                {
                    throw new exception("[room::makeBot] [Error] PLAYER[UID=" + _session.m_pi.uid + "] tentou criar Bot na sala[NUMERO=" + m_ri.numero + ", MASTER=" + Convert.ToString(m_ri.master) + "], mas nao nenhum Player na sala. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                        1, 5000));
                }

                // Inicializa os dados do Bot
                pri.uid = _session.m_pi.uid;
                pri.oid = _session.m_oid;
                pri.state_flag.ready = 1;
                pri.position = 0;
                pri.char_typeid = 0x4000000;
                pri.title = 0x39800013; // Title Helper
                pri.nickname = "\\1Bot";
                pri.sDisplayID = "@NT_" + pri.nickname;
                // if (m_ri.getTipo() == RoomInfo.ROOM_INFO_TYPE.STROKE)
                // {
                // pri.position = 1;
                // pri.ci.id = 1;
                // pri.ci._typeid = pri.char_typeid;
                // pri.ci.initComboDef(); 
                // pri.skin[4] = 0;
                // pri.state_flag.sexo = 0; 
                // pri.level = 0;
                // pri.icon_angel = 0;
                // pri.place.ulPlace = 10;
                // pri.guild_uid = 0;
                // pri.guild_mark_img = "guildmark";
                // pri.guild_mark_index = 0; 
                // pri.state_action.state_lounge = 0;
                // pri.state_action.usUnknown_flg = 0;
                // pri.state_action.state = 0;
                // pri.location = new PlayerRoomInfo.stLocation(); 
                // pri.mascot_typeid = 0; 
                // pri.flag_item_boost.ulItemBoost = 0;
                // pri.channeling_flag = 0;
                // pri.convidado = 0;
                // pri.avg_score = 0;
                // }
                // Add o Bot a sala, só no visual
                v_element.Add(pri);

                // Packet
                var p = new PangyaBinaryWriter();

                if (m_ri.getTipo() != RoomInfo.ROOM_INFO_TYPE.STROKE)
                {
                    // Option 0, passa todos que estão na sala
                    if (packet_func.pacote048(ref p, _session, v_element, 0x100))
                        packet_func.room_broadcast(this, p, 1);

                    // Option 1, passa só o Player que entrou na sala, nesse caso foi o Bot
                    if (packet_func.pacote048(ref p, _session, new List<PlayerRoomInfoEx> { pri }, 0x101))
                        packet_func.room_broadcast(this, p, 1);
                }
                // else
                // {
                //Option 0, passa todos que estão na sala
                // if (packet_func.pacote048(ref p, _session, v_element, 0))
                // packet_func.room_broadcast(this, p, 1);

                //Option 1, passa só o Player que entrou na sala, nesse caso foi o Bot
                // if (packet_func.pacote048(ref p, _session, new List<PlayerRoomInfoEx> { pri }, 1))
                // packet_func.room_broadcast(this, p, 1);
                // }

                // Criou Bot com sucesso
                m_bot_tourney = true;
            }
            catch (exception e)
            {

                // Relança
                throw e;
            }
        }

        // Para as classes filhas, empedir que exclua a sala depLastLasto do se tem Player ou não na sala
        protected virtual bool isDropRoom()
        {
            return true; // class room normal é sempre true
        }

        // protected porque é um método inseguro (sem thread safety)
        protected uint _getRealNumPlayersWithoutInvited()
        {
            return (uint)v_sessions.Count(_el =>
            {
                if (_el == null)
                    return false;

                return m_player_info.TryGetValue(_el, out var playerInfo) && !(playerInfo.convidado == 1);
            });
        }


        // protected por que é o método unsave(inseguro), sem thread safe
        protected bool _haveInvited()
        {
            return v_sessions.Any(_el =>
            {
                if (_el == null)
                    return false;

                return m_player_info.TryGetValue(_el, out var playerInfo) && playerInfo.convidado == 1;
            });
        }


        // Game
        public virtual void finish_game()
        {

            if (m_pGame != null)
            {

                var toAdd = new List<(Player player, PlayerRoomInfoEx info)>();
                // Zera Player Flags
                var player_info = m_player_info.ToList();
                foreach (var el in player_info)
                {
                    // Update Place Player
                    if (m_ri.getTipo() == RoomInfo.ROOM_INFO_TYPE.PRACTICE || m_ri.getTipo() == RoomInfo.ROOM_INFO_TYPE.GRAND_ZODIAC_PRACTICE)
                    {
                        el.Value.place.ulPlace = 2;
                    }
                    else
                    {
                        el.Value.place.ulPlace = 0;
                    }

                    el.Value.state_flag.away = 0;

                    // Aqui só zera quem não é Master da sala, o master deixa sempre ready
                    if (m_ri.master == el.Key.m_pi.uid)
                    {
                        el.Value.state_flag.ready = 1;
                    }
                    else
                    {
                        el.Value.state_flag.ready = 0;
                    }

                    // Update Player info
                    updatePlayerInfo(el.Key);

                    // SLast update on room
                    sendPlayerInfo(el.Key, 3);
                }

                // Atualiza type da sala, só não atualiza se for GM evento ou GZ Event e SSC
                if (!(m_ri.trofel == TROFEL_GM_EVENT_TYPEID || m_ri.getTipo() == RoomInfo.ROOM_INFO_TYPE.SPECIAL_SHUFFLE_COURSE || m_ri.master == -2))
                    m_ri.state = 1; //em espera

                // Att Exp rate, e Pang rate, que criou a sala, att ele também quando começa o jogo

                m_ri.rate_exp = (uint)sgs.gs.getInstance().getInfo().rate.exp;
                m_ri.rate_pang = (uint)sgs.gs.getInstance().getInfo().rate.pang;
                m_ri.angel_event = sgs.gs.getInstance().getInfo().rate.angel_event.IsTrue();


                // Update Course of Hole
                if (m_ri.getMap() >= 0x7F) // Random Course With Course already draw
                    m_ri.course = RoomInfo.ROOM_INFO_COURSE.UNK; // Random Course standard


                // Update Master da sala
                updateMaster(null);

                if (m_ri.master == -2)
                    m_ri.master = -1; // pode deletar a sala quando sair todos


                if (v_sessions.Count > 0)
                {
                    // Atualiza info da sala para quem está na sala 
                    packet_func.room_broadcast(this,
                         packet_func.pacote04A(
                        m_ri, -1), 1);
                }

                // limpa lista de Player kikados
                clear_Player_kicked();

                // Verifica se o Bot Tourney está ativo, kika bot e limpa a type
                if (m_bot_tourney)
                {

                    var pMaster = findMaster();

                    if (pMaster != null)
                    {

                        try
                        {
                            // Kick Bot
                            // Atualiza os Player que estão na sala que o Bot sai por que ele é só visual
                            sendPlayerInfo(pMaster, 0);

                        }
                        catch (exception e)
                        {

                            _smp.message_pool.getInstance().push(new message("[room::finish_game::KickBotTourney][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                        }
                    }

                    m_bot_tourney = false;
                }
                if (m_pGame != null)
                {
                    m_pGame.stopTime();//desliga o relogio...

                    m_pGame.Dispose();      // elimina a referência
                }
                m_pGame = null;      // elimina a referência
            }
        }

        // Invite
        protected void clear_invite()
        {
            if (v_invite.Any())
            {
                v_invite.Clear();
            }
        }

        // Team
        protected void init_teans()
        {

            // Limpa teans, se tiver teans inicilizados já
            clear_teans();

            // Init Teans
            m_teans.Add(new Team(0));
            m_teans.Add(new Team(1));

            PlayerRoomInfo pPri = null;

            // Add Players All Seus Respectivos teans
            foreach (var el in v_sessions)
            {

                if ((pPri = getPlayerInfo(el)) == null)
                {
                    throw new exception("[room::init_teans] [Error] nao encontrou o info do PLAYER[UID=" + Convert.ToString(el.m_pi.uid) + "] na sala. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                        1504, 0));
                }

                m_teans[pPri.state_flag.team].addPlayer(el);
            }

        }


        public int leaveAll(int _option)
        {
            // Percorre de trás para frente
            for (int i = v_sessions.Count - 1; i >= 0; i--)
            {
                var player = v_sessions[i];
                if (player != null)
                {
                    try
                    {
                        leave(player, _option);
                    }
                    catch (exception e)
                    {

                        _smp.message_pool.getInstance().push(new message("[room::leaveAll] [Error] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                    }
                }
            }
            return 0;
        }

        public bool isInvited(Player _session)
        {

            var it = m_player_info.FirstOrDefault(c => c.Key.getUID() == _session.getUID());

            return (it.Value != null && it.Value.convidado == 1);
        }

        public InviteChannelInfo addInvited(uint _uid_has_invite, Player _session)
        {

            if (!_session.getState())
            {
                throw new exception("[room::addInvited] [Error] Player nao esta connectado.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    4, 0));
            }

            if (isFull())
            {
                throw new exception("[room::addInvited] [Error] PLAYER[UID=" + _session.m_pi.uid + "] tentou entrar na a sala[NUMERO=" + m_ri.numero + "], mas a sala ja esta cheia.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    2, 0));
            }

            if (findIndexSession(_uid_has_invite) == (int)~0)
            {
                throw new exception("[room::addInvited] [Error] quem convidou[UID=" + Convert.ToString(_uid_has_invite) + "] o PLAYER[UID=" + _session.m_pi.uid + "] para a sala nao esta na sala. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    2010, 0));
            }

            var s = findSessionByUID(_session.m_pi.uid);

            if (s != null)
            {
                throw new exception("[room::addInvited] [Error] PLAYER[UID=" + Convert.ToString(_uid_has_invite) + "] tentou adicionar o convidado[UID=" + _session.m_pi.uid + "] a sala, mas ele ja esta na sala. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    2001, 0));
            }



            _session.m_pi.mi.sala_numero = m_ri.numero;

            _session.m_pi.place = 70; // Está sendo convidado

            v_sessions.Add(_session);

            ++m_ri.num_player;

            PlayerRoomInfoEx pri = null;

            try
            {

                // Make Info Room Player Invited
                pri = makePlayerInvitedInfo(_session);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[room::addInvited][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            if (pri == null)
            {

                // Pop_back
                v_sessions.Remove(v_sessions.Last());



                throw new exception("[[room::addInvited] [Error] PLAYER[UID=" + Convert.ToString(_uid_has_invite) + "] tentou adicionar o convidado[UID=" + _session.m_pi.uid + "] a sala, nao conseguiu criar o Player Room Info Invited do Player. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    2002, 0));
            }

            // Add Invite Channel Info
            InviteChannelInfo ici = new InviteChannelInfo
            {
                room_number = m_ri.numero,
                invite_uid = _uid_has_invite,
                invited_uid = _session.m_pi.uid,
                time = new SYSTEMTIME(DateTime.Now)
            };

            v_invite.Add(ici);
            // End Add Invite Channel Info

            // Update Char Invited ON ROOM
            var p = new PangyaBinaryWriter((ushort)0x48);

            p.WriteByte(1);
            p.WriteInt16(-1);

            p.WriteBytes(pri.ToArrayEx());

            p.WriteByte(0); // Final Packet

            packet_func.room_broadcast(this,
                p, 1);



            return ici;
        }
        public InviteChannelInfo getInvited(Player _session)
        {
            return v_invite.FirstOrDefault(_el =>
            {
                return (_el.room_number == m_ri.numero && _el.invited_uid == _session.m_pi.uid);
            });
        }

        public InviteChannelInfo getInvited(uint _uid)
        {
            return v_invite.FirstOrDefault(_el => _el.room_number == m_ri.numero && _el.invited_uid == _uid); ;
        }

        public InviteChannelInfo deleteInvited(Player _session)
        {

            var it = m_player_info.FirstOrDefault(c => c.Key.m_pi.uid == _session.m_pi.uid);

            if (it.Key == null && getInvited(_session) == null)
                throw new exception("[room::deleteInvited] [Error] PLAYER[UID=" + _session.m_pi.uid + "] tentou deletar convidado,"
                    + " mas nao tem o info do convidado na sala. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM, 2003, 0));


            int index = findIndexSession(_session);

            if (index == -1 && getInvited(_session) == null)
            {
                throw new exception("[room::deleteInvited] [Error] session[UID=" + _session.m_pi.uid + "] nao existe no vector de sessions da sala.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    5, 0));
            }

            _session.m_pi.mi.sala_numero = ushort.MaxValue;

            _session.m_pi.place = 0; // Limpa Está sendo convidado

            v_sessions.RemoveAt(index);

            --m_ri.num_player;

            m_player_info.Remove(_session);

            // Update Position all Players
            updatePosition();

            // Delete Invite Channel Info
            InviteChannelInfo ici = new InviteChannelInfo();


            var itt = getInvited(_session);

            if (itt != null)
            {

                ici = itt;

                v_invite.Remove(itt);

            }
            else
            {
                _smp.message_pool.getInstance().push(new message("[room::deleteInvited][Warning] PLAYER[UID=" + _session.m_pi.uid + "] nao tem um convite.", type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            // End Delete Invite Channel Info

            // Resposta Delete Convidado
            var p = new PangyaBinaryWriter((ushort)0x130);

            p.WriteUInt32(_session.m_pi.uid);

            packet_func.room_broadcast(this,
                p, 1);

            _smp.message_pool.getInstance().push(new message("[room::deleteInvited][Info] Deleteou um convite[Convidado=" + _session.m_pi.uid + "] na Sala[NUMERO=" + m_ri.numero + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));


            return ici;
        }


        public InviteChannelInfo _deleteInvited(Player _session)
        {


            // Delete Invite Channel Info
            InviteChannelInfo ici = new InviteChannelInfo();


            var itt = getInvited(_session);

            if (itt != null)
            {

                ici = itt;

                v_invite.Remove(itt);

            }
            else
            {
                _smp.message_pool.getInstance().push(new message("[room::deleteInvited][Warning] PLAYER[UID=" + _session.m_pi.uid + "] nao tem um convite.", type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            // End Delete Invite Channel Info

            // Resposta Delete Convidado
            var p = new PangyaBinaryWriter((ushort)0x130);

            p.WriteUInt32(_session.m_pi.uid);

            packet_func.room_broadcast(this,
                p, 1);

            _smp.message_pool.getInstance().push(new message("[room::deleteInvited][Info] Deleteou um convite[Convidado=" + _session.m_pi.uid + "] na Sala[NUMERO=" + m_ri.numero + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));


            return ici;
        }
        public InviteChannelInfo deleteInvited(uint _uid)
        {

            if (_uid == 0)
            {
                throw new exception("[room::deleteInvited] [Error] _uid is invalid(zero). Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    2005, 0));
            }

            var it = m_player_info.FirstOrDefault(_el =>
            {
                return (_el.Value.convidado == 1 && _el.Value.uid == _uid);
            });

            if (it.Key == m_player_info.Last().Key)
            {
                throw new exception("[room::deleteInvited] [Error] PLAYER[UID=" + Convert.ToString(_uid) + "] tentou deletar convidado," + " mas nao tem o info do convidado na sala. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    2003, 0));
            }



            int index = findIndexSession(_uid);

            if (index == (int)~0)
            {


                throw new exception("[room::deleteInvited] [Error] session[UID=" + Convert.ToString(_uid) + "] nao existe no vector de sessions da sala.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    5, 0));
            }

            v_sessions.RemoveAt(index);

            --m_ri.num_player;

            m_player_info.Remove(it.Key);

            // Update Position all Players
            updatePosition();

            // Delete Invite Channel Info
            InviteChannelInfo ici = new InviteChannelInfo();

            var itt = v_invite.FirstOrDefault(_el => _el.room_number == m_ri.numero && _el.invited_uid == _uid);


            if (itt != null)
            {

                ici = itt;

                v_invite.Remove(itt);

            }
            else
            {
                _smp.message_pool.getInstance().push(new message("[room::deleteInvited][Warning] PLAYER[UID=" + Convert.ToString(_uid) + "] nao tem um convite.", type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            // End Delete Invite Channel Info

            // Resposta Delete Convidado
            var p = new PangyaBinaryWriter((ushort)0x130);

            p.WriteUInt32(_uid);

            packet_func.room_broadcast(this,
                p, 1);

            _smp.message_pool.getInstance().push(new message("[room::deleteInvited][Info] Deleteou um convite[Convidado=" + Convert.ToString(_uid) + "] na Sala[NUMERO=" + m_ri.numero + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

            return ici;
        }

        public RoomInfoEx getInfo()
        {
            return m_ri;
        }

        public byte[] ToArray()
        {
            return m_ri.ToArray();
        }


        public bool IsRoomGM()
        {
            return m_ri.flag_gm == 1 &&
             m_ri.state_flag == 0x100;
        }


        // Gets
        public byte getChannelOwenerId()
        {
            return m_channel_owner;
        }

        public ushort getNumero()
        {
            return m_ri.numero;
        }

        public uint getMaster()
        {
            return (uint)m_ri.master;
        }

        public uint getNumPlayers()
        {
            return (uint)m_player_info.Count;
        }

        public byte getPosition(Player _session)
        {
            byte position = 255;

            for (byte i = 0; i < v_sessions.Count; ++i)
            {
                if (v_sessions[i] == _session)
                {
                    position = i;
                    break;
                }
            }
            return position;
        }

        public PlayerRoomInfoEx getPlayerInfo(Player _session)
        {

            if (_session == null)
            {
                throw new exception("Error _session is null. Em room::getPlayerInfo()", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    7, 0));
            }

            PlayerRoomInfoEx pri = m_player_info.FirstOrDefault(c => c.Key == _session).Value;

            if (pri == null)
                return null;

            return pri;
        }

        public List<Player> getSessions(Player _session = null, bool _with_invited = true)
        {
            List<Player> v_session = new List<Player>();
            HashSet<uint> addedUids = new HashSet<uint>();  // evita duplicatas por UID
            foreach (var el in v_sessions)
            {
                if (el != null
                    && el.getState()
                    && el.m_pi.mi.sala_numero != ushort.MaxValue
                    && (_session == null || _session != el)
                    && (_with_invited || !isInvited(el))
                    && addedUids.Add(el.m_pi.uid)) // só adiciona se UID ainda não estiver no HashSet
                {
                    v_session.Add(el);
                }
            }
            return v_session;
        }


        public uint getRealNumPlayersWithoutInvited()
        {

            uint num = 0;



            try
            {

                num = _getRealNumPlayersWithoutInvited();

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[room::getRealNumPlayerWithoutInvited][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }



            return (num);
        }

        public bool haveInvited()
        {

            bool question = false;
            try
            {
                question = _haveInvited();
            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[room::haveInvited][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
            return question;
        }

        // Sets
        public void setNome(string _nome)
        {

            if (_nome.Length == 0)
            {
                throw new exception("Error _nome esta vazio. Em room::setNome()", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    6, 0));
            }
            m_ri.name = _nome;
        }

        public void setSenha(string _senha)
        {

            if (_senha.Length == 0)
            {
                if (!(m_ri.senha_flag == 1))
                {
                    m_ri.senha = "";
                    m_ri.senha_flag = 1;
                }
            }
            else
            {
                m_ri.senha = _senha;
                m_ri.senha_flag = 0;
            }
        }

        public void setTipo(byte _tipo)
        {

            if (_tipo == (byte)RoomInfo.ROOM_INFO_TYPE.MATCH || _tipo == (byte)RoomInfo.ROOM_INFO_TYPE.GUILD_BATTLE)
                init_teans();
            else if (_tipo != (byte)RoomInfo.ROOM_INFO_TYPE.MATCH && m_ri.getTipo() == RoomInfo.ROOM_INFO_TYPE.MATCH)
                clear_teans();

            m_ri.tipo = _tipo;

            // Atualizar tipo da sala
            if (m_ri.tipo > (byte)RoomInfo.ROOM_INFO_TYPE.GRAND_ZODIAC_PRACTICE)
                m_ri.tipo_show = 4;
            else if (m_ri.getTipo() == RoomInfo.ROOM_INFO_TYPE.GRAND_ZODIAC_ADV || m_ri.getTipo() == RoomInfo.ROOM_INFO_TYPE.GRAND_ZODIAC_PRACTICE)
                m_ri.tipo_show = (byte)RoomInfo.ROOM_INFO_TYPE.GRAND_ZODIAC_INT;
            else
                m_ri.tipo_show = m_ri.tipo;


            if (m_ri.tipo >= (byte)RoomInfo.ROOM_INFO_TYPE.GRAND_ZODIAC_INT)
                m_ri.type_extend = m_ri.tipo;
            else
                m_ri.type_extend = 255;

            // Atualiza Trofel se for Tourney
            if (m_ri.getTipo() == RoomInfo.ROOM_INFO_TYPE.TOURNEY || (m_ri.master != -2 && m_ri.tipo >= (byte)RoomInfo.ROOM_INFO_TYPE.GRAND_ZODIAC_INT && m_ri.tipo <= (byte)RoomInfo.ROOM_INFO_TYPE.GRAND_ZODIAC_ADV))
            {

                if ((m_ri.max_player > 30 && m_ri.getTipo() == RoomInfo.ROOM_INFO_TYPE.TOURNEY) || (m_ri.tipo >= (byte)RoomInfo.ROOM_INFO_TYPE.GRAND_ZODIAC_INT && m_ri.tipo <= (byte)RoomInfo.ROOM_INFO_TYPE.GRAND_ZODIAC_ADV))
                {

                    m_ri.flag_gm = 1;

                    m_ri.state_flag = 0x100;

                    m_ri.trofel = TROFEL_GM_EVENT_TYPEID;

                }
                else if (m_ri.getTipo() == RoomInfo.ROOM_INFO_TYPE.TOURNEY || m_ri.tipo >= (byte)RoomInfo.ROOM_INFO_TYPE.GRAND_ZODIAC_INT)
                {
                    updateTrofel();
                }

            }
            else
            {
                m_ri.trofel = 0;
            }
        }

        public void setCourse(byte _course)
        {
            m_ri.course = (RoomInfo.ROOM_INFO_COURSE)_course;
        }

        public void setQntdHole(byte _qntd_hole)
        {
            m_ri.qntd_hole = _qntd_hole;
        }

        public void setModo(byte _modo)
        {
            m_ri.modo = _modo;
        }

        public void setTempoVS(uint _tempo)
        {
            m_ri.time_vs = _tempo;
        }

        public void setMaxPlayer(byte _max_Player)
        {

            if (v_sessions.Count > _max_Player)
            {
                throw new exception("[room::setMaxPlayer] [Error] MASTER[UID=" + Convert.ToString(m_ri.master) + "] _max_PLAYER[VALUE=" + Convert.ToString(_max_Player) + "] é menor que o numero de jogadores[VALUE=" + Convert.ToString(v_sessions.Count) + "] na sala.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    250, 0x588000));
            }

            // New Max Player room
            m_ri.max_player = _max_Player;

            // Atualiza Trofeu se for Tourney
            if (m_ri.getTipo() == RoomInfo.ROOM_INFO_TYPE.TOURNEY || (m_ri.tipo >= (byte)RoomInfo.ROOM_INFO_TYPE.GRAND_ZODIAC_INT && m_ri.tipo <= (byte)RoomInfo.ROOM_INFO_TYPE.GRAND_ZODIAC_ADV))
            {

                if ((m_ri.max_player > 30 && m_ri.getTipo() == RoomInfo.ROOM_INFO_TYPE.TOURNEY) || (m_ri.tipo >= (byte)RoomInfo.ROOM_INFO_TYPE.GRAND_ZODIAC_INT && m_ri.tipo <= (byte)RoomInfo.ROOM_INFO_TYPE.GRAND_ZODIAC_ADV))
                {

                    m_ri.flag_gm = 1;

                    m_ri.trofel = TROFEL_GM_EVENT_TYPEID;

                }
                else if (m_ri.getTipo() == RoomInfo.ROOM_INFO_TYPE.TOURNEY || m_ri.tipo >= (byte)RoomInfo.ROOM_INFO_TYPE.GRAND_ZODIAC_INT)
                {
                    updateTrofel();
                }
            }
        }

        public void setTempo30S(uint _tempo)
        {
            m_ri.time_30s = _tempo;
        }

        public void setHoleRepeat(byte _hole_repeat)
        {
            m_ri.hole_repeat = _hole_repeat;
        }

        public void setFixedHole(uint _fixed_hole)
        {
            m_ri.fixed_hole = _fixed_hole;
        }

        public void setArtefato(uint _artefato)
        {
            m_ri.typeid_artefatic = _artefato;
        }

        public void setNatural(uint _natural)
        {
            m_ri.special_flag_mod.ulNaturalAndShortGame = _natural;
        }

        public void setState(byte _state)
        {
            m_ri.state = _state;
        }

        public void setFlag(byte _flag)
        {
            m_ri.flag = _flag;
        }

        public void setStateAFK(byte _state_afk)
        {
            m_ri.state_afk = _state_afk;
        }

        ///new methods! 
        public void SetAllReady()
        {
            byte ready = 0;
            foreach (var _el in v_sessions)
            {
                var pri = getPlayerInfo(_el);
                pri.state_flag.ready = (byte)(ready == 0 ? 1 : 0);//invertido
                if (pri.state_flag.ready == 1 && !pri.capability.game_master)
                {
                    updatePlayerInfo(_el);
                    var p = new PangyaBinaryWriter();
                    p.init_plain(0x78);
                    p.WriteInt32(pri.oid);
                    p.WriteByte(ready);
                    packet_func.room_broadcast(this, p, 1);
                }
            }
        }

        public bool IsStarted()
        {
            return m_pGame != null && m_pGame.m_game_init_state == 1;
        }

        public bool IsWithBot()
        {
            return m_bot_tourney;
        }

        public void setHioHoleCupScale(uint size_cup)
        {
            if (m_ri.state == 1 && (int)getMaster() == -2 && (m_ri.getTipo() == RoomInfo.ROOM_INFO_TYPE.GRAND_ZODIAC_ADV || m_ri.getTipo() == RoomInfo.ROOM_INFO_TYPE.GRAND_ZODIAC_INT))
            {
                // ((GrandZodiac)m_pGame).setHoleSizeCup(size_cup);
            }
        }

        public void setMission(uint mission_id)
        {
            if (m_ri.state == 1 && (m_ri.getTipo() == RoomInfo.ROOM_INFO_TYPE.APPROCH))
            {
                // ((Approach)m_pGame).setMission(mission_id);
            }
        }

        public void setMatchHole(uint _hole)
        {
            if (m_ri.state == 1 && (m_ri.getTipo() == RoomInfo.ROOM_INFO_TYPE.MATCH))
            {
                m_ri.qntd_hole = (byte)_hole;
            }
        }

        public void setMatchMap(uint _map)
        {
            if (m_ri.state == 1 && (m_ri.getTipo() == RoomInfo.ROOM_INFO_TYPE.MATCH))
            {
                m_ri.course = (RoomInfo.ROOM_INFO_COURSE)_map;
            }
        }

        public bool CheckSecurityKey(byte[] senhaEncriptSala)
        {
            if (senhaEncriptSala == null || senhaEncriptSala.Length == 0)
                return false;

            byte[] storedKey = m_ri.key;

            if (storedKey == null || storedKey.Length != senhaEncriptSala.Length)
                return false;

            // Comparação segura (evita timing attack)
            bool equals = true;
            for (int i = 0; i < storedKey.Length; i++)
                equals &= storedKey[i] == senhaEncriptSala[i];

            return equals;
        }


        // Checks
        public bool checkPass(string _pass)
        {

            if (!isLocked())
            {
                throw new exception("[Room::checkPass] [Error] sala nao tem senha", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    1, 0));
            }

            return string.Compare(m_ri.senha, _pass) == 0;
        }

        // Verifica se o Player tem um loja aberta no lounge e se o item está à vLasta nela
        public bool checkPersonalShopItem(Player _session, int _item_id)
        {
            return m_personal_shop.isItemForSale(_session, _item_id);
        }

        // States
        public bool isLocked()
        {
            return !(m_ri.senha_flag == 1);
        }

        public bool isFull()
        {
            m_ri.num_player = (byte)v_sessions.Count;
            return m_ri.num_player >= m_ri.max_player;
        }

        public bool isGaming()
        {
            if (m_pGame != null)
            {
                return true;
            }
            return false;
        }

        public bool isGamingBefore(uint _uid)
        {

            if (_uid == 0)
            {
                throw new exception("[room::isGamingBefore] [Error] _uid is invalid(zero)", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    1000, 0));
            }

            if (m_pGame == null)
            {
                throw new exception("[room::isGamingBefore] [Error] a sala[NUMERO=" + m_ri.numero + "] nao tem um jogo inicializado. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    1001, 0));
            }

            return m_pGame.isGamingBefore(_uid);
        }

        public bool isKickedPlayer(uint _uid)
        {
            return m_player_kicked.Any(el => el.Key == _uid);
        }

        public virtual bool isAllReady()
        {

            var master = findMaster();

            if (master == null)
            {
                return false;
            }

            // Bot Tourney, Short Game and Special Shuffle Course
            if (m_bot_tourney
                && v_sessions.Count == 1
                && m_ri.getTipo() == RoomInfo.ROOM_INFO_TYPE.TOURNEY || m_ri.getTipo() == RoomInfo.ROOM_INFO_TYPE.SPECIAL_SHUFFLE_COURSE)
            {
                return true;
            }

            // se a sala for Practice, CHIP-IN Practice, e GRAND_PRIX_NOVICE não precisa o Player está pronto
            if (m_ri.getTipo() == RoomInfo.ROOM_INFO_TYPE.PRACTICE
                || m_ri.getTipo() == RoomInfo.ROOM_INFO_TYPE.GRAND_ZODIAC_PRACTICE
                || m_ri.getTipo() == RoomInfo.ROOM_INFO_TYPE.GRAND_PRIX)
            {
                return true;
            }

            // Se o master for GM então não precisar todos está ready(prontos)
            if (master.m_pi.m_cap.game_master && !_haveInvited())
            {
                return true;
            }


            var count = v_sessions.Count(_el =>
            {
                var pri = getPlayerInfo(_el);
                return (pri != null && pri.state_flag.ready == 1);
            });

            // Conta com o master por que o master sempre está pronto(ready)
            return count == v_sessions.Count;
        }

        public void CreateRoomLogSql(Player _session)
        {
            try
            { 
            }
            catch (Exception e)
            {
                _smp.message_pool.getInstance().push(new message("[Game.CreateRoomLogSql][ErrorSystem] Exceção capturada: " + (e.Message), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }


        void generateRoomLogGuid()
        {
            m_room_log.roomId = Guid.NewGuid();
        }

        // Updates
        public void updatePlayerInfo(Player _session)
        {
            PlayerRoomInfoEx pri = new PlayerRoomInfoEx();
            PlayerRoomInfoEx _pri = null;
            try
            {

                if ((_pri = getPlayerInfo(_session)) == null)
                {
                    throw new exception("[room::updatePlayerInfo] [Error] nao tem o PLAYER[UID=" + _session.m_pi.uid + "] info dessa session na sala.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                        8, 0));
                }

                // Copia do que esta no map
                pri = _pri;

                // Player Room Info Update
                pri.oid = _session.m_oid;

                pri.position = (byte)(getPosition(_session) + 1); // posição na sala
                pri.capability = _session.m_pi.m_cap;
                pri.title = _session.m_pi.ue.m_title;

                if (_session.m_pi.ei.char_info != null)
                    pri.char_typeid = _session.m_pi.ei.char_info._typeid;


                pri.skin[4] = 0; // Aqui tem que ser zero, se for outro valor não mostra a imagem do character equipado

                if (getMaster() == _session.m_pi.uid)
                {
                    pri.state_flag.master = 1;
                    pri.state_flag.ready = 1; // Sempre está pronto(ready) o master
                }
                else
                {

                    // Só troca o estado de pronto dele na sala, se anterior mente ele era Master da sala ou não estiver pronto
                    if (pri.state_flag.master == 1 || !(pri.state_flag.ready == 1))
                    {
                        pri.state_flag.ready = 0;
                    }

                    pri.state_flag.master = 0;
                }

                pri.state_flag.sexo = _session.m_pi.mi.sexo;

                // Update Team se for Match
                if (m_ri.getTipo() == RoomInfo.ROOM_INFO_TYPE.MATCH)
                {

                    // Verifica se o Player está em algum team para atualizar o team dele se ele não estiver em nenhum
                    var Player_team = pri.state_flag.team;
                    Player p_seg_team = null;

                    // atualizar o team do Player a type de team dele não bate com o team dele
                    if (m_teans[Player_team].findPlayerByUID(pri.uid) == null && (p_seg_team = m_teans[~Player_team].findPlayerByUID(pri.uid)) == null)
                    {

                        // Player não está em nenhum team
                        if (v_sessions.Count > 1)
                        {

                            if (m_teans[0].getCount() >= 2 && m_teans[1].getCount() >= 2)
                            {
                                throw new exception("[room::updatePlayerInfo] [Error] PLAYER[UID=" + _session.m_pi.uid + "] tentou entrar em time para todos os times da sala estao cheios. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                                    1500, 0));
                            }
                            else if (m_teans[0].getCount() >= 2)
                            {
                                pri.state_flag.team = 1; // Blue
                            }
                            else if (m_teans[1].getCount() >= 2)
                            {
                                pri.state_flag.team = 0; // Red
                            }
                            else
                            {

                                var pPri = getPlayerInfo((v_sessions.Count == 2) ? v_sessions.FirstOrDefault() : (v_sessions.Count > 2 ? (v_sessions.Skip(1).FirstOrDefault()) : null));

                                if (pPri == null)
                                {
                                    throw new exception("[room::updatePlayerInfo] [Error] PLAYER[UID=" + _session.m_pi.uid + "] tentou entrar em um time, mas o ultimo Player da sala, nao tem um info no sala. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                                        1501, 0));
                                }

                                pri.state_flag.team = (byte)~pPri.state_flag.team;
                            }

                        }
                        else
                        {
                            pri.state_flag.team = 0;
                        }

                        m_teans[pri.state_flag.team].addPlayer(_session);

                    }
                    else if (p_seg_team != null)
                    {
                        // a type de team do Player está errada, ele está no outro team, ajeita
                        pri.state_flag.team = (byte)~Player_team;
                    }
                }
                else if (m_ri.tipo != (byte)RoomInfo.ROOM_INFO_TYPE.GUILD_BATTLE) // O Guild Battle tem sua própria função para inicializar e atualizar o team e os dados da guild
                {
                    pri.state_flag.team = Convert.ToByte(((pri.position > 0 ? pri.position : 1) - 1) % 2);
                }

                // Só faz calculo de Quita rate depois que o Player
                // estiver no level Beginner E e jogado 50 games
                if (_session.m_pi.level >= 6 && _session.m_pi.ui.jogado >= 50)
                {
                    float rate = _session.m_pi.ui.getQuitRate();

                    if (rate < GOOD_PLAYER_ICON)
                    {
                        pri.state_flag.azinha = 1;
                    }
                    else if (rate >= QUITER_ICON_1 && rate < QUITER_ICON_2)
                    {
                        pri.state_flag.quiter_1 = 1;
                    }
                    else if (rate >= QUITER_ICON_2)
                    {
                        pri.state_flag.quiter_2 = 1;
                    }
                }

                pri.level = _session.m_pi.mi.level;

                if (_session.m_pi.ei.char_info != null && _session.m_pi.ui.getQuitRate() < GOOD_PLAYER_ICON)
                    pri.icon_angel = _session.m_pi.ei.char_info.AngelEquiped();
                else
                    pri.icon_angel = 0;

                pri.place.ulPlace = 10; // 0x0A dec"10" _session.m_pi.place
                pri.guild_uid = _session.m_pi.gi.uid;

                pri.uid = _session.m_pi.uid;
                pri.state_action.state_lounge = _session.m_pi.state_lounge;
                pri.state_action.usUnknown_flg = 0; // Ví Players com valores 2 e 4 e 0
                pri.state_action.state = _session.m_pi.state;
                pri.location = new PlayerRoomInfo.stLocation() { x = _session.m_pi.location.x, z = _session.m_pi.location.z, r = _session.m_pi.location.r };

                // Personal Shop
                pri.shop = m_personal_shop.getPersonShop(_session);

                if (_session.m_pi.ei.mascot_info != null)
                    pri.mascot_typeid = _session.m_pi.ei.mascot_info._typeid;

                pri.flag_item_boost = _session.m_pi.checkEquipedItemBoost();
                pri.channeling_flag = 0;

                // Só atualiza a type de convidado se for diferente de 1, por que 1 ele é convidado
                if (pri.convidado != 1)
                    pri.convidado = 0; // Flag Convidado, [Não sei bem por que os que entra na sala normal tem valor igual aqui, já que é type de convidado waiting], Valor constante da sala para os Players(ACHO)

                pri.avg_score = _session.m_pi.ui.getMediaScore();

                if (_session.m_pi.ei.char_info != null)
                    pri.ci = _session.m_pi.ei.char_info;

                // Salva novamente
                m_player_info[_session] = pri;
            }
            catch (Exception e)
            {

                throw e;
            }
        }

        // Finds
        public Player findSessionByOID(uint _oid)
        {
            var i = v_sessions.FirstOrDefault(_el =>
                _el.m_oid == _oid);



            if (i != v_sessions.Last())
            {
                return i;
            }

            return null;
        }

        public Player findSessionByUID(uint _uid)
        {

            var i = v_sessions.FirstOrDefault(_el =>
                _el.m_pi.uid == _uid);



            if (i != null)
            {
                return i;
            }
            return null;
        }

        public Player findMaster()
        {
            Player master = null;

            var pMaster = v_sessions.Find(_el => _el.m_pi.uid == m_ri.master);

            if (pMaster != null)
            {
                master = pMaster;
            }

            return master;
        }

        // Bot Tourney, Short Game and Special Shuffle Course
        public void makeBot(Player _session)
        {
            if (!_session.getState())
            {
                throw new exception("[room::makeBot] [Error] Player nao esta connectado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }

            var p = new PangyaBinaryWriter();

            try
            {
                if (IsRoomGM())
                {
                    // SLast Message
                    p.init_plain(0x40); // Msg to Chat of Player

                    p.WriteByte(7); // Notice

                    p.WritePStr("@NOTICE");
                    p.WritePStr("[ \\2Premium ] \\c0xff00ff00\\cNot Need add bot, auto start.");

                    packet_func.session_send(p,
                        _session, 1);
                    return;
                }

                else
                {
                    // Bot Ticket TypeId

                    // Premium User Não precisa de ticket não
                    if (_session.m_pi.m_cap.premium_user || _session.m_pi.m_cap.game_master)
                    {


                        // Add Bot Tourney Visual para a sala
                        addBotVisual(_session);

                        // SLast Message
                        p.init_plain(0x40); // Msg to Chat of Player

                        p.WriteByte(7); // Notice

                        p.WritePStr("@SuperSS");
                        p.WritePStr("[ \\2Premium ] \\c0xff00ff00\\cBot was created.");

                        packet_func.session_send(p,
                            _session, 1);
                    }
                    else
                    {

                        // Verifica se ele tem o ticket para criar o Bot se não manda mensagem dizenho que ele não tem ticket para criar o bot
                        var pWi = _session.m_pi.findWarehouseItemByTypeid(TICKET_BOT_TYPEID) != null ? _session.m_pi.findWarehouseItemByTypeid(TICKET_BOT_TYPEID) : _session.m_pi.findWarehouseItemByTypeid(TICKET_BOT_TYPEID2);

                        if (pWi == null)
                        {

                            // Não tem ticket bot suficiente, manda mensagem
                            // SLast Message
                            p.init_plain(0x40); // Msg to Chat of Player

                            p.WriteByte(7); // Notice

                            p.WritePStr("@SuperSS");
                            p.WritePStr("\\c0xffff0000\\cYou do not have enough ticket to create the Bot.");

                            packet_func.session_send(p,
                                _session, 1);
                        }
                        else
                        {

                            // Add Bot Tourney Visual para a sala
                            addBotVisual(_session);

                            // SLast Message
                            p.init_plain(0x40); // Msg to Chat of Player

                            p.WriteByte(7); // Notice

                            p.WritePStr("@SuperSS");
                            p.WritePStr("[ \\2Premium ] \\c0xff00ff00\\cBot was created.");

                            packet_func.session_send(p,
                                _session, 1);
                        }
                    }
                }
            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[room::makeBot][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                // SLast Message
                p.init_plain(0x40); // Msg to Chat of Player

                p.WriteByte(7); // Notice

                p.WritePStr("@SuperSS");
                p.WritePStr("\\c0xffff0000\\cError creating Bot.");

                packet_func.session_send(p,
                    _session, 1);
            }
        }

        // Info Room
        public bool requestChangeInfoRoom(Player _session, packet _packet)
        {
            if (!_session.getState())
            {
                throw new exception("[room::requestChangeInfoRoom] [Error] Player nao esta connectado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }
            if (_packet == null)
            {
                throw new exception("[room::requestChangeInfoRoom] [Error] _packet is null", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }

            bool ret = false;

            try
            {

                byte num_info;
                short roomId;

                if (m_ri.master != _session.m_pi.uid)
                {
                    if (!_session.m_pi.m_cap.game_master)
                        throw new exception("[room::requestChangeInfoRoom] [Error] PLAYER[UID=" + _session.m_pi.uid + "] tentou trocar o info da sala[NUMERO=" + m_ri.numero + ", MASTER=" + Convert.ToString(m_ri.master) + "], mas nao pode trocar o info da sala sem ser master.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                        11, 0));
                }

                roomId = _packet.ReadInt16();

                num_info = _packet.ReadByte();

                if (num_info <= 0)
                {
                    throw new exception("[room::requestChangeInfoRoom] [Error] PLAYER[UID=" + _session.m_pi.uid + "] tentou trocar o info da sala[NUMERO=" + m_ri.numero + ", MASTER=" + Convert.ToString(m_ri.master) + "], mas nao tem nenhum info para trocar do buffer do cliente.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                        8, 0));
                }

                for (var i = 0; i < num_info; ++i)
                {
                    var type = (RoomInfo.ROOM_INFO_CHANGE)_packet.ReadByte();

                    switch (type)
                    {
                        case RoomInfo.ROOM_INFO_CHANGE.NAME:
                            {
                                var title = _packet.ReadPStr();

                                if ((m_ri.getTipo() == RoomInfo.ROOM_INFO_TYPE.PRACTICE || m_ri.getTipo() == RoomInfo.ROOM_INFO_TYPE.GRAND_ZODIAC_PRACTICE) && title.CompareTo("Single Player Practice Mode") != 0)
                                    setNome("Single Player Practice Mode");
                                else
                                    setNome(title);
                            }
                            break;
                        case RoomInfo.ROOM_INFO_CHANGE.SENHA:
                            {
                                var pwd = _packet.ReadPStr();

                                if (!string.IsNullOrEmpty(pwd) && pwd.Length > 8 && (m_ri.getTipo() == RoomInfo.ROOM_INFO_TYPE.PRACTICE || m_ri.getTipo() == RoomInfo.ROOM_INFO_TYPE.GRAND_ZODIAC_PRACTICE))
                                    ThrowHackException(_session, "tamanho da str da senha na sala inválida: " + getInfo().getTipo());

                                setSenha(pwd);
                            }
                            break;
                        case RoomInfo.ROOM_INFO_CHANGE.TIPO:
                            {
                                var T8 = _packet.ReadByte();
                                if (Enum.IsDefined(typeof(RoomInfo.ROOM_INFO_TYPE), T8))
                                {
                                    setTipo(T8);
                                }
                                else
                                    ThrowHackException(_session, "falha ao setar o tipo: " + getInfo().getTipo());
                            }
                            break;
                        case RoomInfo.ROOM_INFO_CHANGE.COURSE:
                            {
                                var T8 = _packet.ReadByte();
                                if (Enum.IsDefined(typeof(RoomInfo.ROOM_INFO_COURSE), T8))
                                {
                                    setCourse(T8);
                                }
                                else
                                    ThrowHackException(_session, "falha ao setar o course: " + getInfo().getTipo());
                            }
                            break;
                        case RoomInfo.ROOM_INFO_CHANGE.QNTD_HOLE:
                            setQntdHole(_packet.ReadByte());
                            break;
                        case RoomInfo.ROOM_INFO_CHANGE.MODO:
                            {
                                var T8 = _packet.ReadByte();
                                if (Enum.IsDefined(typeof(RoomInfo.ROOM_INFO_MODO), T8))
                                {
                                    setModo(T8);
                                }
                                else
                                    ThrowHackException(_session, "falha ao setar o Modo: " + getInfo().getTipo());
                            }
                            break;
                        case RoomInfo.ROOM_INFO_CHANGE.TEMPO_VS: // Passa em Segundos
                            {
                                var timevs = (uint)_packet.ReadUInt16();
                                if (timevs > 0)
                                    setTempoVS(timevs * 1000);
                                else
                                    ThrowHackException(_session, "falha ao setar o tempo vs: " + getInfo().getTipo());
                            }
                            break;
                        case RoomInfo.ROOM_INFO_CHANGE.MAX_PLAYER:
                            {
                                var T8 = _packet.ReadByte();
                                if (!(T8 <= this.v_sessions.Count))
                                    setMaxPlayer(T8);
                                else
                                    ThrowHackException(_session, "falha ao setar o numero de maximo de players: " + getInfo().getTipo());
                            }
                            break;
                        case RoomInfo.ROOM_INFO_CHANGE.TEMPO_30S: // Passa em Minutos
                            {
                                var time30s = (uint)_packet.ReadByte();
                                if (time30s > 0)
                                    setTempo30S(time30s * 60000);
                                else
                                    ThrowHackException(_session, "falha ao setar o tempo minutos: " + getInfo().getTipo());
                            }
                            break;
                        case RoomInfo.ROOM_INFO_CHANGE.STATE_FLAG:
                            setStateAFK(_packet.ReadByte());
                            break;
                        case RoomInfo.ROOM_INFO_CHANGE.GALLERY_LIMIT:
                            m_ri.gallery_limit = _packet.ReadByte();
                            break;
                        case RoomInfo.ROOM_INFO_CHANGE.HOLE_REPEAT:
                            setHoleRepeat(_packet.ReadByte());
                            break;
                        case RoomInfo.ROOM_INFO_CHANGE.FIXED_HOLE:
                            setFixedHole(_packet.ReadUInt32());
                            break;
                        case RoomInfo.ROOM_INFO_CHANGE.ARTEFATO:
                            setArtefato(_packet.ReadUInt32());
                            break;
                        case RoomInfo.ROOM_INFO_CHANGE.NATURAL:
                            {
                                var value = _packet.ReadUInt32();
                                var natural = new SpecialModeFlag(value);

                                if (!natural.natural && sgs.gs.getInstance().getInfo().propriedade.natural) // Natural não deixa desabilitar o Natural da sala, por que o server é natural
                                {
                                    natural.natural = true;
                                }
                                setNatural(natural.ulNaturalAndShortGame);

                                break;
                            }
                        default:
                            throw new exception("[room::requestChangeInfoRoom] [Error] PLAYER[UID=" + _session.m_pi.uid + "] tentou trocar info da sala[NUMERO=" + m_ri.numero + ", MASTER=" + Convert.ToString(m_ri.master) + "], mas info change é desconhecido.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                                9, 0));
                    }
                }
                new FilterRoom(_session, m_ri, _session.m_channel.getInfo());
                // send to clients update room info
                SendHeadRoom();

                ret = true; // Trocou o info da sala com sucesso

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[room::requestChangeInfoRoom][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Resposta para o cliente 
                packet_func.session_send(packet_func.pacote04A(
                    m_ri, 25),
                    _session, 1);
            }

            return ret;
        }


        private void SendHeadRoom()
        {
            var p = packet_func.pacote04A(m_ri, -1/*valor constante*/);
            packet_func.room_broadcast(this, p, 0);
        }

        public void sendPlayerInfo(Player _session, int _option)
        {

            int option = !(m_ri.getTipo() == RoomInfo.ROOM_INFO_TYPE.STROKE ||
                           m_ri.getTipo() == RoomInfo.ROOM_INFO_TYPE.MATCH ||
                           m_ri.getTipo() == RoomInfo.ROOM_INFO_TYPE.LOUNGE ||
                           m_ri.getTipo() == RoomInfo.ROOM_INFO_TYPE.PANG_BATTLE) ? 0x100 : 0;

            option += _option;

            if (option == 0 && m_ri.getTipo() == RoomInfo.ROOM_INFO_TYPE.LOUNGE)
                option = 7;

            List<PlayerRoomInfoEx> v_element = new List<PlayerRoomInfoEx>();
            PlayerRoomInfoEx pri = null;

            try
            {

                foreach (var sess in v_sessions)
                {
                    pri = getPlayerInfo(sess);
                    if (pri != null)
                        v_element.Add(pri);
                }

                pri = getPlayerInfo(_session);

                if (pri == null && _option != 2)
                    return;

                var p = new PangyaBinaryWriter();

                if (packet_func.pacote048(ref p, _session, ((_option == 1 || _option == 4 || _option == 0x103) ? new List<PlayerRoomInfoEx>() { pri } : v_element), option))
                    packet_func.room_broadcast(this, p, 1);
            }
            catch (exception e)
            {
                throw e;
            }
        }

        public void SendPlayerStateLounge(Player _session)
        {

            if (m_ri.getTipo() == RoomInfo.ROOM_INFO_TYPE.LOUNGE)
            {
                var it = (_session.m_pi.ei.char_info == null) ? _session.m_pi.mp_scl.LastOrDefault() : _session.m_pi.mp_scl.find(_session.m_pi.ei.char_info.id);

                if (it.Value == null)
                {
                    throw new exception("[room::sendCharacterStateLounge] [Error] PLAYER[UID=" + _session.m_pi.uid + "] nao tem os estados do character na lounge.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        13, 0));
                }

                var p = packet_func.pacote196(_session, it.Value);

                packet_func.room_broadcast(this,
                    p, 0);
            }
        }

        public void sendWeatherLounge(Player _session)
        {

            if (m_ri.getTipo() == RoomInfo.ROOM_INFO_TYPE.LOUNGE)
            {

                // Envia o tempo(weather) do lounge só se ele for diferente de tempo bom
                if (m_weather_lounge != 0)
                {

                    var p = new PangyaBinaryWriter((ushort)0x9E);

                    p.WriteUInt16(m_weather_lounge);
                    p.WriteByte(0); // Flag (acho), vou colocar 0 o padrão, colocou 1 aqui só quando eu mudou com o comando GM

                    packet_func.session_send(p,
                        _session, 1);
                }
            }
        }
        // Chat Team
        public void requestChatTeam(Player _session, packet _packet)
        {
            if (!_session.getState())
            {
                throw new exception("[room::requestChatTeam] [Error] Player nao esta connectado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }
            if (_packet == null)
            {
                throw new exception("[room::requestChatTeam] [Error] _packet is null", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }

            var p = new PangyaBinaryWriter();

            try
            {

                var msg = _packet.ReadPStr();

                // Verifica a mensagem com palavras proibida e manda para o log e bloquea o chat dele
                _smp.message_pool.getInstance().push(new message("[room::requestChatTeam][Info] PLAYER[UID=" + _session.m_pi.uid + ", MESSAGE=" + msg + "]", type_msg.CL_ONLY_FILE_LOG));

                if (string.IsNullOrEmpty(msg))
                {
                    throw new exception("[room::requestChatTeam] [Error] PLAYER[UID=" + _session.m_pi.uid + "] tentou mandar messsage[MSG=" + msg + "] no chat do team na sala[NUMERO=" + m_ri.numero + "], mas a msg esta vazia. Hacker ou Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                        2000, 0));
                }

                if (m_ri.tipo != (byte)RoomInfo.ROOM_INFO_TYPE.MATCH && m_ri.tipo != (byte)RoomInfo.ROOM_INFO_TYPE.GUILD_BATTLE)
                {
                    throw new exception("[room::requestChatTeam] [Error] PLAYER[UID=" + _session.m_pi.uid + "] tentou mandar messsage[MSG=" + msg + "] no chat do team na sala[NUMERO=" + m_ri.numero + "], mas a sala nao é MATCH ou GUILD_BATTLE. Hacker ou Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                        2001, 0));
                }

                if (m_teans.Count == 0)
                {
                    throw new exception("[room::requestChatTeam] [Error] PLAYER[UID=" + _session.m_pi.uid + "] tentou mandar messsage[MSG=" + msg + "] no chat do team na sala[NUMERO=" + m_ri.numero + "], mas a sala nao tem nenhum team. Hacker ou Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                        2002, 0));
                }

                var pri = getPlayerInfo(_session);

                if (pri == null)
                {
                    throw new exception("[room::requetChatTeam] [Error] PLAYER[UID=" + _session.m_pi.uid + "] tentou mandar messsage[MSG=" + msg + "] no chat do team na sala[NUMERO=" + m_ri.numero + "], mas a sala nao tem o info dele. Hacker ou Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                        2003, 0));
                }

                var team = m_teans[pri.state_flag.team];

                if (team.findPlayerByUID(_session.m_pi.uid) == null)
                {
                    throw new exception("[room::requestChatTeam] [Error] PLAYER[UID=" + _session.m_pi.uid + "] tentou mandar messsage[MSG=" + msg + "] no chat do team na sala[NUMERO=" + m_ri.numero + "], mas ele nao esta no team que a type de team dele diz. Hacker ou Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                        2004, 0));
                }

                // LOG GM
                // Envia para todo os GM do server essa message
                var c = sgs.gs.getInstance().findChannel(_session.m_pi.channel);

                if (c != null)
                {

                    var gm = sgs.gs.getInstance().FindAllGM();

                    if (gm.Any())
                    {

                        string msg_gm = "\\5" + (_session.m_pi.nickname) + ": '" + msg + "'";
                        string from = "\\1[Channel=" + (c.getInfo().name) + ", \\1ROOM=" + Convert.ToString(_session.m_pi.mi.sala_numero) + "][Team" + (!(pri.state_flag.team == 1) ? "R" : "B") + "]";

                        var index = from.IndexOf(' ');

                        if (index != -1)
                        {
                            from = from.Remove(index, 1).Insert(index, " \\1");
                        }

                        foreach (Player el in gm)
                        {
                            if (((el.m_gi.channel > 0 && el.m_pi.channel == c.getInfo().id) || el.m_gi.whisper > 0 || el.m_gi.isOpenPlayerWhisper(_session.m_pi.uid)) && (el.m_pi.channel != _session.m_pi.channel || el.m_pi.mi.sala_numero != _session.m_pi.mi.sala_numero || team.findPlayerByUID(el.m_pi.uid) == null))
                            {

                                // Responde no chat do Player
                                p.init_plain(0x40);

                                p.WriteByte(0);

                                p.WritePStr(from); // Nickname

                                p.WritePStr(msg_gm); // Message

                                packet_func.session_send(p,
                                    el, 1);
                            }
                        }
                    }
                }
                else
                {

                }
                {
                    _smp.message_pool.getInstance().push(new message("[room::requestChatTeam][Warning] Log GM nao encontrou o Channel[ID=" + Convert.ToString((ushort)_session.m_pi.channel) + "] no server. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                }

                // Manda message para o team da sala
                p.init_plain(0xB0);

                p.WritePStr(_session.m_pi.nickname);
                p.WritePStr(msg);

                foreach (var el in team.getPlayers())
                {
                    packet_func.session_send(p, _session);
                }
            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[room::requestChatTeam][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        // Change Item Equiped of Player 
          public virtual void requestChangePlayerItemRoom(Player _session, ChangePlayerItemRoom _cpir)
        {
            if (!_session.getState())
            {
                throw new exception("[room::" + "ChangePlayerItemRoom] [Error] Player nao esta connectado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }

            var p = new PangyaBinaryWriter();
            var result = new byte[10];
            try
            {

                int error = 0/*SUCCESS*/;
                switch (_cpir.type)
                {
                    case ChangePlayerItemRoom.TYPE_CHANGE.TC_CADDIE:
                        {
                            CaddieInfoEx pCi = null;

                            // Caddie
                            if (_cpir.caddie != 0 && (pCi = _session.m_pi.findCaddieById(_cpir.caddie)) != null
                                    && sIff.getInstance().getItemGroupIdentify(pCi._typeid) == IFF_GROUP.CADDIE)
                            {

                                // Check if item is in map of update item
                                var v_it = _session.m_pi.findUpdateItemByTypeidAndId(pCi._typeid, pCi.id);

                                if (v_it.Count == 0)
                                {

                                    foreach (var el in v_it)
                                    {

                                        if (el.Value.type == UpdateItem.UI_TYPE.CADDIE)
                                        {

                                            // Desequipa o caddie
                                            _session.m_pi.ei.cad_info = null;
                                            _session.m_pi.ue.caddie_id = 0;

                                            _cpir.caddie = 0;

                                        }
                                        else if (el.Value.type == UpdateItem.UI_TYPE.CADDIE_PARTS)
                                        {

                                            // Limpa o caddie Parts
                                            pCi.parts_typeid = 0u;
                                            pCi.parts_end_date_unix = 0;
                                            pCi.end_parts_date = new SYSTEMTIME();

                                            _session.m_pi.ei.cad_info = pCi;
                                            _session.m_pi.ue.caddie_id = _cpir.caddie;
                                        }

                                        // Tira esse Update Item do map
                                        _session.m_pi.mp_ui.Remove(el.Key);
                                    }

                                }
                                else
                                {

                                    // Caddie is Good, Update caddie equiped ON SERVER AND DB
                                    _session.m_pi.ei.cad_info = pCi;
                                    _session.m_pi.ue.caddie_id = _cpir.caddie;

                                    // Verifica se o Caddie pode ser equipado
                                    if (_session.checkCaddieEquiped(_session.m_pi.ue))
                                        _cpir.caddie = _session.m_pi.ue.caddie_id;

                                }

                                // Update ON DB
                                snmdb.NormalManagerDB.getInstance().add(0, new CmdUpdateCaddieEquiped(_session.m_pi.uid, _cpir.caddie), SQLDBResponse, this);

                            }
                            else if (_session.m_pi.ue.caddie_id > 0 && _session.m_pi.ei.cad_info != null)
                            {   // Desequipa Caddie

                                error = (_cpir.caddie == 0) ? 1/*client give invalid item id*/ : (pCi == null ? 2/*Item Not Found*/ : 3/*Erro item typeid invalid*/);

                                if (error > 1)
                                {
                                    _smp.message_pool.getInstance().push(new message("[room::requestChangePlayerItemRoom][Info][Warning] PLAYER[UID=" + (_session.m_pi.uid)
                                            + "] tentou trocar o Caddie[ID=" + (_cpir.caddie) + "] para comecar o jogo, mas deu Error[VALUE="
                                            + (error) + "], desequipando o caddie. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                                }

                                // Check if item is in map of update item
                                var v_it = _session.m_pi.findUpdateItemByTypeidAndId(_session.m_pi.ei.cad_info._typeid, _session.m_pi.ei.cad_info.id);

                                if (v_it.Count == 0)
                                {

                                    foreach (var el in v_it)
                                    {

                                        // Caddie já vai se desequipar, só verifica o parts
                                        if (el.Value.type == UpdateItem.UI_TYPE.CADDIE_PARTS)
                                        {

                                            // Limpa o caddie Parts
                                            _session.m_pi.ei.cad_info.parts_typeid = 0u;
                                            _session.m_pi.ei.cad_info.parts_end_date_unix = 0;
                                            _session.m_pi.ei.cad_info.end_parts_date = new SYSTEMTIME();
                                        }

                                        // Tira esse Update Item do map
                                        _session.m_pi.mp_ui.Remove(el.Key);
                                    }

                                }

                                _session.m_pi.ei.cad_info = null;
                                _session.m_pi.ue.caddie_id = 0;

                                _cpir.caddie = 0;

                                // Zera o Error para o cliente desequipar o caddie que o server desequipou
                                error = 0;

                                // Att No DB
                                snmdb.NormalManagerDB.getInstance().add(0, new CmdUpdateCaddieEquiped(_session.m_pi.uid, _cpir.caddie), SQLDBResponse, this);
                            }


                            packet_func.room_broadcast(this, packet_func.pacote04B(_session, (byte)_cpir.type, error));
                        }
                        break;
                    case ChangePlayerItemRoom.TYPE_CHANGE.TC_BALL:
                        {
                            WarehouseItemEx pWi = null;

                            if (_cpir.ball != 0 && (pWi = _session.m_pi.findWarehouseItemByTypeid(_cpir.ball)) != null
                                    && sIff.getInstance().getItemGroupIdentify(pWi._typeid) == IFF_GROUP.BALL)
                            {

                                _session.m_pi.ei.comet = pWi;
                                _session.m_pi.ue.ball_typeid = _cpir.ball;      // Ball(Comet) é o typeid que o cliente passa

                                // Verifica se a bola pode ser equipada
                                if (_session.checkBallEquiped(_session.m_pi.ue))
                                    _cpir.ball = _session.m_pi.ue.ball_typeid;

                                // Update ON DB
                                snmdb.NormalManagerDB.getInstance().add(0, new CmdUpdateBallEquiped(_session.m_pi.uid, _cpir.ball), SQLDBResponse, this);

                            }
                            else if (_cpir.ball == 0)
                            { // Bola 0 coloca a bola padrão para ele, se for premium user coloca a bola de premium user

                                // Zera para equipar a bola padrão
                                _session.m_pi.ei.comet = null;
                                _session.m_pi.ue.ball_typeid = 0;

                                // Verifica se a Bola pode ser equipada (Coloca para equipar a bola padrão
                                if (_session.checkBallEquiped(_session.m_pi.ue))
                                    _cpir.ball = _session.m_pi.ue.ball_typeid;

                                // Update ON DB
                                snmdb.NormalManagerDB.getInstance().add(0, new CmdUpdateBallEquiped(_session.m_pi.uid, _cpir.ball), SQLDBResponse, this);

                            }
                            else
                            {

                                error = (pWi == null ? 2 : 3);

                                pWi = _session.m_pi.findWarehouseItemByTypeid(DEFAULT_COMET_TYPEID);

                                if (pWi != null)
                                {

                                    _smp.message_pool.getInstance().push(new message("[room::requestChangePlayerItemRoom][Info][Warning] PLAYER[UID=" + (_session.m_pi.uid)
                                            + "] tentou trocar a Ball[TYPEID=" + (_cpir.ball) + "] para comecar o jogo, mas deu Error[VALUE="
                                            + (error) + "], colocando a Ball Padrao do player. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                                    _session.m_pi.ei.comet = pWi;
                                    _cpir.ball = _session.m_pi.ue.ball_typeid = pWi._typeid;

                                    // Zera o Error para o cliente equipar a Ball Padrão que o server equipou
                                    error = 0;

                                    // Update ON DB
                                    snmdb.NormalManagerDB.getInstance().add(0, new CmdUpdateBallEquiped(_session.m_pi.uid, _cpir.ball), SQLDBResponse, this);

                                }
                                else
                                {

                                    _smp.message_pool.getInstance().push(new message("[room::requestChangePlayerItemRoom][Info][Warning] PLAYER[UID=" + (_session.m_pi.uid)
                                            + "] tentou trocar a Ball[TYPEID=" + (_cpir.ball) + "] para comecar o jogo, mas deu Error[VALUE="
                                            + (error) + "], ele nao tem a Ball Padrao, adiciona a Ball pardrao para ele. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                                    BuyItem bi = new BuyItem();
                                    stItem item = new stItem();

                                    bi.id = -1;
                                    bi._typeid = DEFAULT_COMET_TYPEID;
                                    bi.qntd = 1;

                                    ItemManager.initItemFromBuyItem(_session.m_pi, item, bi, false, 0, 0, 1);

                                    if (item._typeid != 0)
                                    {

                                        if ((_cpir.ball = (uint)ItemManager.addItem(item, _session, 2/*Padrão Item*/, 0)) != Convert.ToUInt32(ItemManager.RetAddItem.T_ERROR))
                                        {

                                            // Equipa a Ball padrao
                                            pWi = _session.m_pi.findWarehouseItemById((int)_cpir.ball);

                                            if (pWi != null)
                                            {

                                                _session.m_pi.ei.comet = pWi;
                                                _session.m_pi.ue.ball_typeid = pWi._typeid;

                                                // Zera o Error para o cliente equipar a Ball Padrão que o server equipou
                                                error = 0;

                                                // Update ON DB
                                                snmdb.NormalManagerDB.getInstance().add(0, new CmdUpdateBallEquiped(_session.m_pi.uid, _cpir.ball), SQLDBResponse, this);

                                                // Update ON GAME
                                                p.init_plain(0x216);

                                                p.WriteUInt32((uint)UtilTime.GetSystemTimeAsUnix());
                                                p.WriteUInt32(1);   // Count

                                                p.WriteByte(item.type);
                                                p.WriteUInt32(item._typeid);
                                                p.WriteInt32(item.id);
                                                p.WriteUInt32(item.flag_time);
                                                p.WriteBytes(item.stat.ToArray());
                                                p.WriteInt32((item.STDA_C_ITEM_TIME > 0) ? item.STDA_C_ITEM_TIME : item.STDA_C_ITEM_QNTD);
                                                p.WriteZero(25);

                                                packet_func.session_send(p, _session, 1);

                                            }
                                            else
                                                _smp.message_pool.getInstance().push(new message("[room::requestChangePlayerItemRoom][Info][Warning] PLAYER[UID=" + (_session.m_pi.uid)
                                                        + "] nao conseguiu achar a Ball[ID="
                                                        + (item.id) + "] que acabou de adicionar para ele. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                                        }
                                        else
                                            _smp.message_pool.getInstance().push(new message("[room::requestChangePlayerItemRoom][Info][Warning] PLAYER[UID=" + (_session.m_pi.uid)
                                                    + "] nao conseguiu adicionar a Ball[TYPEID="
                                                    + (item._typeid) + "] para ele. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                                    }
                                    else
                                        _smp.message_pool.getInstance().push(new message("[room::requestChangePlayerItemRoom][Info][Warning] PLAYER[UID=" + (_session.m_pi.uid)
                                                + "] nao conseguiu inicializar a Ball[TYPEID="
                                                + (bi._typeid) + "] para ele. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                                }
                            }
                            p = packet_func.pacote04B(_session,
                               (byte)_cpir.type, error);
                            packet_func.room_broadcast(this,
                                p, 1);
                        }
                        break;
                    case ChangePlayerItemRoom.TYPE_CHANGE.TC_CLUBSET: // ClubSet
                        {
                            WarehouseItemEx pWi = null;

                            // ClubSet
                            if (_cpir.clubset != 0
                                && (pWi = _session.m_pi.findWarehouseItemById(_cpir.clubset)) != null
                                && sIff.getInstance().getItemGroupIdentify(pWi._typeid) == IFF_GROUP.CLUBSET)
                            {

                                var c_it = _session.m_pi.findUpdateItemByTypeidAndType((uint)_cpir.clubset, UpdateItem.UI_TYPE.WAREHOUSE);

                                if (c_it.Count > 0)
                                {

                                    _session.m_pi.ei.clubset = pWi;

                                    // Esse C do WarehouseItem, que pega do DB, não é o ja updado inicial da taqueira é o que fica tabela enchant,
                                    // que no original fica no warehouse msm, eu só confundi quando fiz
                                    _session.m_pi.ei.csi.setValues(pWi.id, pWi._typeid, pWi.c);

                                    var cs = sIff.getInstance().findClubSet(pWi._typeid);

                                    if (cs != null)
                                    {

                                        for (var j = 0u; j < (_session.m_pi.ei.csi.enchant_c.Length); ++j)
                                        {
                                            _session.m_pi.ei.csi.enchant_c[j] = (short)(cs.SlotStats.getSlot[j] + pWi.clubset_workshop.c[j]);
                                        }

                                        _session.m_pi.ue.clubset_id = _cpir.clubset;

                                        // Verifica se o ClubSet pode ser equipado
                                        if (_session.checkClubSetEquiped(_session.m_pi.ue))
                                        {
                                            _cpir.clubset = _session.m_pi.ue.clubset_id;
                                        }

                                        // Update ON DB
                                        snmdb.NormalManagerDB.getInstance().add(0,
                                            new CmdUpdateClubsetEquiped(_session.m_pi.uid, (int)_cpir.clubset),
                                            Room.SQLDBResponse, this);

                                    }
                                    else
                                    {

                                        error = 5;

                                        _smp.message_pool.getInstance().push(new message("[room::requestChangePlayerItemRoom] [Error] PLAYER[UID=" + _session.m_pi.uid + "] tentou Atualizar Clubset[TYPEID=" + Convert.ToString(pWi._typeid) + ", ID=" + Convert.ToString(pWi.id) + "] equipado, mas ClubSet Not exists on IFF structure. Equipa o ClubSet padrao. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                                        // Coloca o ClubSet CV1 no lugar do ClubSet que acabou o tempo
                                        pWi = _session.m_pi.findWarehouseItemByTypeid(AIR_KNIGHT_SET);

                                        if (pWi != null)
                                        {

                                            _smp.message_pool.getInstance().push(new message("[room::requestChangePlayerItemRoom][Info][Warning] PLAYER[UID=" + _session.m_pi.uid + "] tentou trocar o ClubSet[ID=" + Convert.ToString(_cpir.clubset) + "] para comecar o jogo, mas acabou o tempo do ClubSet[ID=" + Convert.ToString(_cpir.clubset) + "], colocando o ClubSet Padrao\"CV1\" do player. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                                            // Esse C do WarehouseItem, que pega do DB, não é o ja updado inicial da taqueira é o que fica tabela enchant,
                                            // que no original fica no warehouse msm, eu só confundi quando fiz
                                            _session.m_pi.ei.csi.setValues(pWi.id, pWi._typeid, pWi.c);

                                            cs = sIff.getInstance().findClubSet(pWi._typeid);

                                            if (cs != null)
                                            {
                                                for (var j = 0u; j < (_session.m_pi.ei.csi.enchant_c.Length); ++j)
                                                {
                                                    _session.m_pi.ei.csi.enchant_c[j] = (short)(cs.SlotStats.getSlot[j] + pWi.clubset_workshop.c[j]);
                                                }
                                            }

                                            _session.m_pi.ei.clubset = pWi;
                                            _cpir.clubset = _session.m_pi.ue.clubset_id = pWi.id;

                                            // Zera o Error para o cliente equipar a "CV1" que o server equipou
                                            error = 0;

                                            // Update ON DB
                                            snmdb.NormalManagerDB.getInstance().add(0,
                                                new CmdUpdateClubsetEquiped(_session.m_pi.uid, (int)_cpir.clubset),
                                                Room.SQLDBResponse, this);

                                        }
                                        else
                                        {

                                            _smp.message_pool.getInstance().push(new message("[room::requestChangePlayerItemRoom][Info][Warning] PLAYER[UID=" + _session.m_pi.uid + "] tentou trocar o ClubSet[ID=" + Convert.ToString(_cpir.clubset) + "] para comecar o jogo, mas acabou o tempo do ClubSet[ID=" + Convert.ToString(_cpir.clubset) + "], ele nao tem o ClubSet Padrao\"CV1\", adiciona o ClubSet pardrao\"CV1\" para ele. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                                            BuyItem bi = new BuyItem();
                                            stItem item = new stItem();

                                            bi.id = -1;
                                            bi._typeid = AIR_KNIGHT_SET;
                                            bi.qntd = 1;

                                            ItemManager.initItemFromBuyItem(_session.m_pi,
                                                item, bi, false, 0, 0, 1);

                                            if (item._typeid != 0)
                                            {
                                                _cpir.clubset = ItemManager.addItem(item,
                                                    _session, 2, 0);
                                                if (_cpir.clubset != ItemManager.RetAddItem.T_ERROR)
                                                {

                                                    // Equipa o ClubSet CV1
                                                    pWi = _session.m_pi.findWarehouseItemById(_cpir.clubset);

                                                    if (pWi != null)
                                                    {

                                                        // Esse C do WarehouseItem, que pega do DB, não é o ja updado inicial da taqueira é o que fica tabela enchant,
                                                        // que no original fica no warehouse msm, eu só confundi quando fiz
                                                        _session.m_pi.ei.csi.setValues(pWi.id, pWi._typeid, pWi.c);

                                                        cs = sIff.getInstance().findClubSet(pWi._typeid);

                                                        if (cs != null)
                                                        {
                                                            for (var j = 0u; j < (_session.m_pi.ei.csi.enchant_c.Length); ++j)
                                                            {
                                                                _session.m_pi.ei.csi.enchant_c[j] = (short)(cs.SlotStats.getSlot[j] + pWi.clubset_workshop.c[j]);
                                                            }
                                                        }

                                                        _session.m_pi.ei.clubset = pWi;
                                                        _session.m_pi.ue.clubset_id = pWi.id;

                                                        // Zera o Error para o cliente equipar a "CV1" que o server equipou
                                                        error = 0;

                                                        // Update ON DB
                                                        snmdb.NormalManagerDB.getInstance().add(0,
                                                            new CmdUpdateClubsetEquiped(_session.m_pi.uid, (int)_cpir.clubset),
                                                            Room.SQLDBResponse, this);

                                                        // Update ON GAME
                                                        p.init_plain(0x216);

                                                        p.WriteUInt32((uint32_t)UtilTime.GetSystemTimeAsUnix());
                                                        p.WriteUInt32(1); // Count

                                                        p.WriteByte(item.type);
                                                        p.WriteUInt32(item._typeid);
                                                        p.WriteInt32(item.id);
                                                        p.WriteUInt32(item.flag_time);
                                                        p.WriteBytes(item.stat.ToArray());
                                                        p.WriteUInt32((item.c[3] > 0) ? item.c[3] : item.c[0]);
                                                        p.WriteZeroByte(25);

                                                        packet_func.session_send(p,
                                                            _session, 1);

                                                    }
                                                    else
                                                    {
                                                        _smp.message_pool.getInstance().push(new message("[room::requestChangePlayerItemRoom][Info][Warning] PLAYER[UID=" + _session.m_pi.uid + "] nao conseguiu achar o ClubSet\"CV1\"[ID=" + Convert.ToString(item.id) + "] que acabou de adicionar para ele. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                                                    }

                                                }
                                                else
                                                {
                                                    _smp.message_pool.getInstance().push(new message("[room::requestChangePlayerItemRoom][Info][Warning] PLAYER[UID=" + _session.m_pi.uid + "] nao conseguiu adicionar o ClubSet[TYPEID=" + Convert.ToString(item._typeid) + "] para ele. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                                                }

                                            }
                                            else
                                            {
                                                _smp.message_pool.getInstance().push(new message("[room::requestChangePlayerItemRoom][Info][Warning] PLAYER[UID=" + _session.m_pi.uid + "] nao conseguiu inicializar o ClubSet[TYPEID=" + Convert.ToString(bi._typeid) + "] para ele. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                                            }
                                        }
                                    }

                                }
                                else
                                { // ClubSet Acabou o tempo

                                    error = 6; // Acabou o tempo do item

                                    // Coloca o ClubSet CV1 no lugar do ClubSet que acabou o tempo
                                    pWi = _session.m_pi.findWarehouseItemByTypeid(AIR_KNIGHT_SET);

                                    if (pWi != null)
                                    {

                                        _smp.message_pool.getInstance().push(new message("[room::requestChangePlayerItemRoom][Info][Warning] PLAYER[UID=" + _session.m_pi.uid + "] tentou trocar o ClubSet[ID=" + Convert.ToString(_cpir.clubset) + "] para comecar o jogo, mas acabou o tempo do ClubSet[ID=" + Convert.ToString(_cpir.clubset) + "], colocando o ClubSet Padrao\"CV1\" do player. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                                        // Esse C do WarehouseItem, que pega do DB, não é o ja updado inicial da taqueira é o que fica tabela enchant,
                                        // que no original fica no warehouse msm, eu só confundi quando fiz
                                        _session.m_pi.ei.csi.setValues(pWi.id, pWi._typeid, pWi.c);

                                        var cs = sIff.getInstance().findClubSet(pWi._typeid);

                                        if (cs != null)
                                        {
                                            for (var j = 0u; j < (_session.m_pi.ei.csi.enchant_c.Length); ++j)
                                            {
                                                _session.m_pi.ei.csi.enchant_c[j] = (short)(cs.SlotStats.getSlot[j] + pWi.clubset_workshop.c[j]);
                                            }
                                        }

                                        _session.m_pi.ei.clubset = pWi;
                                        _cpir.clubset = _session.m_pi.ue.clubset_id = pWi.id;

                                        // Zera o Error para o cliente equipar a "CV1" que o server equipou
                                        error = 0;

                                        // Update ON DB
                                        snmdb.NormalManagerDB.getInstance().add(0,
                                            new CmdUpdateClubsetEquiped(_session.m_pi.uid, (int)_cpir.clubset),
                                            Room.SQLDBResponse, this);

                                    }
                                    else
                                    {

                                        _smp.message_pool.getInstance().push(new message("[room::requestChangePlayerItemRoom][Info][Warning] PLAYER[UID=" + _session.m_pi.uid + "] tentou trocar o ClubSet[ID=" + Convert.ToString(_cpir.clubset) + "] para comecar o jogo, mas acabou o tempo do ClubSet[ID=" + Convert.ToString(_cpir.clubset) + "], ele nao tem o ClubSet Padrao\"CV1\", adiciona o ClubSet pardrao\"CV1\" para ele. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                                        BuyItem bi = new BuyItem();
                                        stItem item = new stItem();

                                        bi.id = -1;
                                        bi._typeid = AIR_KNIGHT_SET;
                                        bi.qntd = 1;

                                        ItemManager.initItemFromBuyItem(_session.m_pi,
                                          item, bi, false, 0, 0, 1);

                                        if (item._typeid != 0)
                                        {

                                            if ((_cpir.clubset = (int)ItemManager.addItem(item,
                                                        _session, 2, 0)) != Convert.ToUInt32(ItemManager.RetAddItem.T_ERROR))
                                            {

                                                // Equipa o ClubSet CV1
                                                pWi = _session.m_pi.findWarehouseItemById(_cpir.clubset);

                                                if (pWi != null)
                                                {

                                                    // Esse C do WarehouseItem, que pega do DB, não é o ja updado inicial da taqueira é o que fica tabela enchant,
                                                    // que no original fica no warehouse msm, eu só confundi quando fiz
                                                    var cs = sIff.getInstance().findClubSet(pWi._typeid);

                                                    if (cs != null)
                                                    {
                                                        for (var j = 0u; j < (_session.m_pi.ei.csi.enchant_c.Length); ++j)
                                                        {
                                                            _session.m_pi.ei.csi.enchant_c[j] = (short)(cs.SlotStats.getSlot[j] + pWi.clubset_workshop.c[j]);
                                                        }
                                                    }
                                                    _session.m_pi.ei.clubset = pWi;
                                                    _session.m_pi.ue.clubset_id = pWi.id;

                                                    // Zera o Error para o cliente equipar a "CV1" que o server equipou
                                                    error = 0;

                                                    // Update ON DB
                                                    snmdb.NormalManagerDB.getInstance().add(0,
                                                        new CmdUpdateClubsetEquiped(_session.m_pi.uid, (int)_cpir.clubset),
                                                        Room.SQLDBResponse, this);

                                                    // Update ON GAME
                                                    p.init_plain(0x216);

                                                    p.WriteUInt32((uint32_t)UtilTime.GetSystemTimeAsUnix());
                                                    p.WriteUInt32(1); // Count

                                                    p.WriteByte(item.type);
                                                    p.WriteUInt32(item._typeid);
                                                    p.WriteInt32(item.id);
                                                    p.WriteUInt32(item.flag_time);
                                                    p.WriteBytes(item.stat.ToArray());
                                                    p.WriteUInt32((item.c[3] > 0) ? item.c[3] : item.c[0]);
                                                    p.WriteZeroByte(25);

                                                    packet_func.session_send(p,
                                                        _session, 1);

                                                }
                                                else
                                                {
                                                    _smp.message_pool.getInstance().push(new message("[room::requestChangePlayerItemRoom][Info][Warning] PLAYER[UID=" + _session.m_pi.uid + "] nao conseguiu achar o ClubSet\"CV1\"[ID=" + Convert.ToString(item.id) + "] que acabou de adicionar para ele. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                                                }

                                            }
                                            else
                                            {
                                                _smp.message_pool.getInstance().push(new message("[room::requestChangePlayerItemRoom][Info][Warning] PLAYER[UID=" + _session.m_pi.uid + "] nao conseguiu adicionar o ClubSet[TYPEID=" + Convert.ToString(item._typeid) + "] para ele. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                                            }

                                        }
                                        else
                                        {
                                            _smp.message_pool.getInstance().push(new message("[room::requestChangePlayerItemRoom][Info][Warning] PLAYER[UID=" + _session.m_pi.uid + "] nao conseguiu inicializar o ClubSet[TYPEID=" + Convert.ToString(bi._typeid) + "] para ele. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                                        }
                                    }
                                }

                            }
                            else
                            {

                                error = (_cpir.clubset == 0) ? 1 : (pWi == null ? 2 : 3);

                                pWi = _session.m_pi.findWarehouseItemByTypeid(AIR_KNIGHT_SET);

                                if (pWi != null)
                                {

                                    _smp.message_pool.getInstance().push(new message("[room::requestChangePlayerItemRoom][Info][Warning] PLAYER[UID=" + _session.m_pi.uid + "] tentou trocar o ClubSet[ID=" + Convert.ToString(_cpir.clubset) + "] para comecar o jogo, mas deu Error[VALUE=" + Convert.ToString(error) + "], colocando o ClubSet Padrao\"CV1\" do player. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                                    // Esse C do WarehouseItem, que pega do DB, não é o ja updado inicial da taqueira é o que fica tabela enchant,
                                    // que no original fica no warehouse msm, eu só confundi quando fiz
                                    var cs = sIff.getInstance().findClubSet(pWi._typeid);

                                    if (cs != null)
                                    {
                                        for (var j = 0u; j < (_session.m_pi.ei.csi.enchant_c.Length); ++j)
                                        {
                                            _session.m_pi.ei.csi.enchant_c[j] = (short)(cs.SlotStats.getSlot[j] + pWi.clubset_workshop.c[j]);
                                        }
                                    }

                                    _session.m_pi.ei.clubset = pWi;
                                    _cpir.clubset = _session.m_pi.ue.clubset_id = pWi.id;

                                    // Zera o Error para o cliente equipar a "CV1" que o server equipou
                                    error = 0;

                                    // Update ON DB
                                    snmdb.NormalManagerDB.getInstance().add(0,
                                        new CmdUpdateClubsetEquiped(_session.m_pi.uid, (int)_cpir.clubset),
                                        Room.SQLDBResponse, this);

                                }
                                else
                                {

                                    _smp.message_pool.getInstance().push(new message("[room::requestChangePlayerItemRoom][Info][Warning] PLAYER[UID=" + _session.m_pi.uid + "] tentou trocar o ClubSet[ID=" + Convert.ToString(_cpir.clubset) + "] para comecar o jogo, mas deu Error[VALUE=" + Convert.ToString(error) + "], ele nao tem o ClubSet Padrao\"CV1\", adiciona o ClubSet pardrao\"CV1\" para ele. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                                    BuyItem bi = new BuyItem();
                                    stItem item = new stItem();

                                    bi.id = -1;
                                    bi._typeid = AIR_KNIGHT_SET;
                                    bi.qntd = 1;

                                    ItemManager.initItemFromBuyItem(_session.m_pi,
                                       item, bi, false, 0, 0, 1);

                                    if (item._typeid != 0)
                                    {

                                        if ((_cpir.clubset = (int)ItemManager.addItem(item,
                                                         _session, 2, 0)) != Convert.ToUInt32(ItemManager.RetAddItem.T_ERROR))
                                        {

                                            // Equipa o ClubSet CV1
                                            pWi = _session.m_pi.findWarehouseItemById(_cpir.clubset);

                                            if (pWi != null)
                                            {

                                                // Esse C do WarehouseItem, que pega do DB, não é o ja updado inicial da taqueira é o que fica tabela enchant,
                                                // que no original fica no warehouse msm, eu só confundi quando fiz
                                                _session.m_pi.ei.csi.setValues(pWi.id, pWi._typeid, pWi.c);

                                                var cs = sIff.getInstance().findClubSet(pWi._typeid);

                                                if (cs != null)
                                                {
                                                    for (var j = 0u; j < (_session.m_pi.ei.csi.enchant_c.Length); ++j)
                                                    {
                                                        _session.m_pi.ei.csi.enchant_c[j] = (short)(cs.SlotStats.getSlot[j] + pWi.clubset_workshop.c[j]);
                                                    }
                                                }

                                                _session.m_pi.ei.clubset = pWi;
                                                _session.m_pi.ue.clubset_id = pWi.id;

                                                // Zera o Error para o cliente equipar a "CV1" que o server equipou
                                                error = 0;

                                                // Update ON DB
                                                snmdb.NormalManagerDB.getInstance().add(0,
                                                    new CmdUpdateClubsetEquiped(_session.m_pi.uid, (int)_cpir.clubset),
                                                    Room.SQLDBResponse, this);

                                                // Update ON GAME
                                                p.init_plain(0x216);

                                                p.WriteUInt32((uint32_t)UtilTime.GetSystemTimeAsUnix());
                                                p.WriteUInt32(1); // Count

                                                p.WriteByte(item.type);
                                                p.WriteUInt32(item._typeid);
                                                p.WriteInt32(item.id);
                                                p.WriteUInt32(item.flag_time);
                                                p.WriteBytes(item.stat.ToArray());
                                                p.WriteUInt32((item.c[3] > 0) ? item.c[3] : item.c[0]);
                                                p.WriteZeroByte(25);

                                                packet_func.session_send(p,
                                                    _session, 1);

                                            }
                                            else
                                            {
                                                _smp.message_pool.getInstance().push(new message("[room::requestChangePlayerItemRoom][Info][Warning] PLAYER[UID=" + _session.m_pi.uid + "] nao conseguiu achar o ClubSet\"CV1\"[ID=" + Convert.ToString(item.id) + "] que acabou de adicionar para ele. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                                            }

                                        }
                                        else
                                        {
                                            _smp.message_pool.getInstance().push(new message("[room::requestChangePlayerItemRoom][Info][Warning] PLAYER[UID=" + _session.m_pi.uid + "] nao conseguiu adicionar o ClubSet[TYPEID=" + Convert.ToString(item._typeid) + "] para ele. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                                        }

                                    }
                                    else
                                    {
                                        _smp.message_pool.getInstance().push(new message("[room::requestChangePlayerItemRoom][Info][Warning] PLAYER[UID=" + _session.m_pi.uid + "] nao conseguiu inicializar o ClubSet[TYPEID=" + Convert.ToString(bi._typeid) + "] para ele. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                                    }
                                }
                            }

                            p = packet_func.pacote04B(_session,
                               (byte)_cpir.type, error);
                            packet_func.room_broadcast(this,
                                p, 1);
                            break;
                        }
                    case ChangePlayerItemRoom.TYPE_CHANGE.TC_CHARACTER:
                        {
                            CharacterInfo pCe = null;

                            if (_cpir.character != 0
                                && (pCe = _session.m_pi.findCharacterById(_cpir.character)) != null
                                && sIff.getInstance().getItemGroupIdentify(pCe._typeid) == IFF_GROUP.CHARACTER)
                            {

                                _session.m_pi.ei.char_info = pCe;
                                _session.m_pi.ue.character_id = _cpir.character;

                                // Update ON DB
                                snmdb.NormalManagerDB.getInstance().add(0,
                                    new CmdUpdateCharacterEquiped(_session.m_pi.uid, (int)_cpir.character),
                                    Room.SQLDBResponse, this);

                                // Update Player Info Channel and Room
                                updatePlayerInfo(_session);

                                PlayerRoomInfoEx pri = getPlayerInfo(_session);

                                if (getInfo().getTipo() != RoomInfo.ROOM_INFO_TYPE.PRACTICE && getInfo().getTipo() != RoomInfo.ROOM_INFO_TYPE.GRAND_ZODIAC_PRACTICE)
                                {
                                    if (packet_func.pacote048(ref p,
                                    _session,
                                    new List<PlayerRoomInfoEx>() { pri == null ? new PlayerRoomInfoEx() : pri },
        0x103))
                                    {
                                        packet_func.room_broadcast(this,
                                            p, 0);

                                        //                if (getInfo().getTipo() == RoomInfo.ROOM_INFO_TYPE.LOUNGE)
                                        //                {
                                        //                    var v_element = m_player_info.Where(c => c.Key.m_pi.mi.sala_numero == _session.m_pi.mi.sala_numero).

                                        //                    packet_func.pacote048(ref p, _session, v_element.ke, 0))
                                        //packet_func.room_broadcast(this, p, 1);
                                        //                }
                                    }
                                }

                                if (getInfo().getTipo() == RoomInfo.ROOM_INFO_TYPE.LOUNGE)
                                {

                                    var it = (_session.m_pi.ei.char_info == null) ? _session.m_pi.mp_scl.LastOrDefault() : _session.m_pi.mp_scl.find(_session.m_pi.ei.char_info.id);

                                    if (it.Value == null)
                                    {

                                        _smp.message_pool.getInstance().push(new message("[room::requestChangePlayerItemRoom] [Error] PLAYER[UID=" + _session.m_pi.uid + "] nao tem os estados do character na lounge. Criando um novo para ele. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                                        // Add New State Character Lounge
                                        var pair = _session.m_pi.mp_scl.insert(Tuple.Create(_session.m_pi.ei.char_info.id, new StateCharacterLounge()));

                                        it = pair;
                                    }
                                    p = packet_func.pacote196(_session, it.Value);
                                    packet_func.room_broadcast(this,
                                        p, 0);
                                }

                            }
                            else
                            {

                                error = (_cpir.character == 0) ? 1 : (pCe == null ? 2 : 3);

                                if (_session.m_pi.mp_ce.Count() > 0)
                                {

                                    _smp.message_pool.getInstance().push(new message("[room::requestChangePlayerItemRoom][Info][Warning] PLAYER[UID=" + _session.m_pi.uid + "] tentou trocar o Character[ID=" + Convert.ToString(_cpir.character) + "] para comecar o jogo, mas deu Error[VALUE=" + Convert.ToString(error) + "], colocando o primeiro character do player. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                                    _session.m_pi.ei.char_info = _session.m_pi.mp_ce.FirstOrDefault().Value;
                                    _cpir.character = _session.m_pi.ue.character_id = _session.m_pi.ei.char_info.id;

                                    // Zera o Error para o cliente equipar o Primeiro Character do map de character do player, que o server equipou
                                    error = 0;

                                    // Update ON DB
                                    snmdb.NormalManagerDB.getInstance().add(0,
                                        new CmdUpdateCharacterEquiped(_session.m_pi.uid, (int)_cpir.character),
                                        Room.SQLDBResponse, this);

                                    // Update Player Info Channel and Room
                                    updatePlayerInfo(_session);

                                    PlayerRoomInfoEx pri = getPlayerInfo(_session);

                                    if (getInfo().getTipo() != RoomInfo.ROOM_INFO_TYPE.PRACTICE && getInfo().getTipo() != RoomInfo.ROOM_INFO_TYPE.GRAND_ZODIAC_PRACTICE)
                                    {
                                        if (packet_func.pacote048(ref p,
                                              _session,
                                              new List<PlayerRoomInfoEx>() { (pri == null) ? new PlayerRoomInfoEx() : pri },
               0x103))
                                        {
                                            packet_func.room_broadcast(this,
                                                p, 0);
                                        }
                                    }

                                    if (getInfo().getTipo() == RoomInfo.ROOM_INFO_TYPE.LOUNGE)
                                    {

                                        var it = (_session.m_pi.ei.char_info == null) ? _session.m_pi.mp_scl.LastOrDefault() : _session.m_pi.mp_scl.FirstOrDefault(c => c.Key == _session.m_pi.ei.char_info.id);

                                        if (it.Value == null)
                                        {

                                            _smp.message_pool.getInstance().push(new message("[room::requestChangePlayerItemRoom] [Error] PLAYER[UID=" + _session.m_pi.uid + "] nao tem os estados do character na lounge. Criando um novo para ele. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                                            // Add New State Character Lounge
                                            it = _session.m_pi.mp_scl.insert(Tuple.Create(_session.m_pi.ei.char_info.id, new StateCharacterLounge()));
                                        }

                                        p = packet_func.pacote196(_session, it.Value);

                                        packet_func.room_broadcast(this,
                                            p, 0);
                                    }

                                }
                                else
                                {

                                    _smp.message_pool.getInstance().push(new message("[room::requestChangePlayerItemRoom][Info][Warning] PLAYER[UID=" + _session.m_pi.uid + "] tentou trocar o Character[ID=" + Convert.ToString(_cpir.character) + "] para comecar o jogo, mas deu Error[VALUE=" + Convert.ToString(error) + "], ele nao tem nenhum character, adiciona o Nuri para ele. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                                    BuyItem bi = new BuyItem();
                                    stItem item = new stItem();

                                    bi.id = -1;
                                    bi._typeid = (uint)(sIff.getInstance().CHARACTER << 26); // Nuri
                                    bi.qntd = 1;

                                    ItemManager.initItemFromBuyItem(_session.m_pi,
                                       item, bi, false, 0, 0, 1);

                                    if (item._typeid != 0)
                                    {
                                        _cpir.character = (int)ItemManager.addItem(item,
                                            _session, 2, 0);
                                        // Add Item já atualiza o Character equipado
                                        if ((int)_cpir.character != (int)ItemManager.RetAddItem.T_ERROR)
                                        {

                                            // Zera o Error para o cliente equipar o Nuri que o server equipou
                                            error = 0;

                                            // Update ON GAME
                                            p.init_plain(0x216);

                                            p.WriteUInt32((uint32_t)UtilTime.GetSystemTimeAsUnix());
                                            p.WriteUInt32(1); // Count

                                            p.WriteByte(item.type);
                                            p.WriteUInt32(item._typeid);
                                            p.WriteInt32(item.id);
                                            p.WriteUInt32(item.flag_time);
                                            p.WriteBytes(item.stat.ToArray());
                                            p.WriteUInt32((item.c[3] > 0) ? item.c[3] : item.c[0]);
                                            p.WriteZeroByte(25);

                                            packet_func.session_send(p,
                                                _session, 1);

                                            // Update Player Info Channel and Room
                                            updatePlayerInfo(_session);

                                            PlayerRoomInfoEx pri = getPlayerInfo(_session);

                                            if (getInfo().getTipo() != RoomInfo.ROOM_INFO_TYPE.PRACTICE && getInfo().getTipo() != RoomInfo.ROOM_INFO_TYPE.GRAND_ZODIAC_PRACTICE)
                                            {
                                                if (packet_func.pacote048(ref p,
                                                    _session,
                                                    new List<PlayerRoomInfoEx>() { pri == null ? new PlayerRoomInfoEx() : pri },
               0x103))
                                                {
                                                    packet_func.room_broadcast(this,
                                                        p, 0);
                                                }
                                            }

                                            if (getInfo().getTipo() == RoomInfo.ROOM_INFO_TYPE.LOUNGE)
                                            {

                                                var it = (_session.m_pi.ei.char_info == null) ? _session.m_pi.mp_scl.LastOrDefault() : _session.m_pi.mp_scl.find(_session.m_pi.ei.char_info.id);

                                                if (it.Value == null)
                                                {

                                                    _smp.message_pool.getInstance().push(new message("[room::requestChangePlayerItemRoom] [Error] PLAYER[UID=" + _session.m_pi.uid + "] nao tem os estados do character na lounge. Criando um novo para ele. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                                                    // Add New State Character Lounge
                                                    var pair = _session.m_pi.mp_scl.insert(Tuple.Create(_session.m_pi.ei.char_info.id, new StateCharacterLounge()));

                                                    it = pair;
                                                }


                                                p = packet_func.pacote196(_session, it.Value);

                                                packet_func.room_broadcast(this,
                                                    p, 0);
                                            }

                                        }
                                        else
                                        {
                                            _smp.message_pool.getInstance().push(new message("[room::requestChangePlayerItemRoom][Info][Warning] PLAYER[UID=" + _session.m_pi.uid + "] nao conseguiu adicionar o Character[TYPEID=" + Convert.ToString(item._typeid) + "] para ele. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                                        }

                                    }
                                    else
                                    {
                                        _smp.message_pool.getInstance().push(new message("[room::requestChangePlayerItemRoom][Info][Warning] PLAYER[UID=" + _session.m_pi.uid + "] nao conseguiu inicializar o Character[TYPEID=" + Convert.ToString(bi._typeid) + "] para ele. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                                    }
                                }
                            }
                            p = packet_func.pacote04B(
                            _session, (byte)_cpir.type, error);
                            packet_func.room_broadcast(this, p);

                            break;
                        }
                    case ChangePlayerItemRoom.TYPE_CHANGE.TC_MASCOT:
                        MascotInfoEx pMi = null;

                        if (_cpir.mascot != 0)
                        {

                            if ((pMi = _session.m_pi.findMascotById(_cpir.mascot)) != null && sIff.getInstance().getItemGroupIdentify(pMi._typeid) == IFF_GROUP.MASCOT)
                            {

                                var m_it = _session.m_pi.findUpdateItemByTypeidAndType((uint)_session.m_pi.ue.mascot_id, UpdateItem.UI_TYPE.MASCOT);

                                if (m_it.Keys.Count > 0)
                                {

                                    // Desequipa o Mascot que acabou o tempo dele
                                    _session.m_pi.ei.mascot_info = null;
                                    _session.m_pi.ue.mascot_id = 0;

                                    _cpir.mascot = 0;

                                }
                                else
                                {

                                    // Mascot is good, update on server, DB and game
                                    _session.m_pi.ei.mascot_info = pMi;
                                    _session.m_pi.ue.mascot_id = _cpir.mascot;

                                    // Verifica se o mascot pode equipar
                                    if (_session.checkMascotEquiped(_session.m_pi.ue))
                                        _cpir.mascot = _session.m_pi.ue.mascot_id;

                                }

                                // Update ON DB
                                snmdb.NormalManagerDB.getInstance().add(0, new CmdUpdateMascotEquiped(_session.m_pi.uid, _cpir.mascot), SQLDBResponse, this);

                            }
                            else
                            {

                                error = (_cpir.mascot == 0) ? 1/*client give invalid item id*/ : (pMi == null ? 2/*Item Not Found*/ : 3/*Erro item typeid invalid*/);

                                if (error > 1)
                                {
                                    _smp.message_pool.getInstance().push(new message("[room::requestChangePlayerItemRoom][Info][Warning] PLAYER[UID=" + (_session.m_pi.uid)
                                            + "] tentou trocar o Mascot[ID=" + (_cpir.mascot) + "] para comecar o jogo, mas deu Error[VALUE="
                                            + (error) + "], desequipando o Mascot. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                                }

                                _session.m_pi.ei.mascot_info = null;
                                _session.m_pi.ue.mascot_id = 0;

                                _cpir.mascot = 0;

                                // Att No DB
                                snmdb.NormalManagerDB.getInstance().add(0, new CmdUpdateMascotEquiped(_session.m_pi.uid, _cpir.mascot), SQLDBResponse, this);
                            }

                        }
                        else if (_session.m_pi.ue.mascot_id > 0 && _session.m_pi.ei.mascot_info != null)
                        {   // Desequipa Mascot

                            _session.m_pi.ei.mascot_info = null;
                            _session.m_pi.ue.mascot_id = 0;

                            _cpir.mascot = 0;

                            // Att No DB
                            snmdb.NormalManagerDB.getInstance().add(0, new CmdUpdateMascotEquiped(_session.m_pi.uid, _cpir.mascot), SQLDBResponse, this);

                        } // else Não tem nenhum mascot equipado, para desequipar, então o cliente só quis atualizar o estado


                        p = packet_func.pacote04B(_session,
               (byte)_cpir.type, error);
                        packet_func.room_broadcast(this,
                            p, 1);
                        break;
                    case ChangePlayerItemRoom.TYPE_CHANGE.TC_ITEM_EFFECT_LOUNGE:  // Itens Active, Jester x2 velocidade no lounge, e Harmes tamanho da cabeça
                        {
                            // ignora o item_id por que ele envia 0

                            // Valor 1 Cabeca
                            // Valor 2 Velocidade
                            // Valor 3 Twilight

                            if (!sIff.getInstance().isLoad())
                            {
                                sIff.getInstance().initilation();
                            }

                            if (_session.m_pi.ei.char_info == null)
                            {
                                throw new exception("[room::requestChangePlayerItemRoom] [Error] PLAYER[UID=" + _session.m_pi.uid + "] nao tem nenhum character equipado. Hacker ou Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                    1000, 0x57007));
                            }

                            if (_cpir.effect_lounge.effect != ChangePlayerItemRoom.stItemEffectLounge.TYPE_EFFECT.TE_TWILIGHT)
                            {

                                var it = (_session.m_pi.ei.char_info == null) ? _session.m_pi.mp_scl.Last() : _session.m_pi.mp_scl.FirstOrDefault(c => c.Key == _session.m_pi.ei.char_info.id);

                                if (it.Key == _session.m_pi.mp_scl.Last().Key)
                                {

                                    _smp.message_pool.getInstance().push(new message("[room::requestChangePlayerItemRoom] [Error] PLAYER[UID=" + _session.m_pi.uid + "] nao tem os estados do character na lounge. Criando um novo para ele. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                                    // Add New State Character Lounge
                                    _session.m_pi.mp_scl.Add(_session.m_pi.ei.char_info.id, new StateCharacterLounge());
                                    var pair = _session.m_pi.mp_scl.FirstOrDefault(c => c.Key == _session.m_pi.ei.char_info.id);
                                    it = pair;
                                }

                                switch (_cpir.effect_lounge.effect)
                                {
                                    case ChangePlayerItemRoom.stItemEffectLounge.TYPE_EFFECT.TE_BIG_HEAD: // Jester (Big head)
                                        {

                                            var ccj = cadie_cauldron_Jester_item_typeid.FirstOrDefault(el =>
                                            {
                                                return sIff.getInstance().getItemCharIdentify(el) == (_session.m_pi.ei.char_info._typeid & 0x000000FF);
                                            });

                                            if (ccj <= 0)
                                            {
                                                throw new exception("[room::requestChangePlayerItemRoom] [Error] PLAYER[UID=" + _session.m_pi.uid + "] o Character[TYPEID=" + Convert.ToString(_session.m_pi.ei.char_info._typeid) + "] equipado nao tem o item Jester no server. Hacker ou Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 1001, 0x57008));
                                            }

                                            if (!_session.m_pi.ei.char_info.isPartEquiped(ccj))
                                            {
                                                throw new exception("[room::requestChangePlayerItemRoom] [Error] PLAYER[UID=" + _session.m_pi.uid + "] o Character[TYPEID=" + Convert.ToString(_session.m_pi.ei.char_info._typeid) + "] nao esta com o item[TYPEID=" + Convert.ToString(ccj) + "] Jester equipado. Hacker ou Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 1002, 0x57009));
                                            }

                                            it.Value.scale_head = (it.Value.scale_head > 1.0f) ? 1.0f : 2.0f;

                                            break;
                                        }
                                    case ChangePlayerItemRoom.stItemEffectLounge.TYPE_EFFECT.TE_FAST_WALK: // Hermes (Velocidade x2)
                                        {
                                            var cch = cadie_cauldron_Hermes_item_typeid.FirstOrDefault(el =>
                                            {
                                                return sIff.getInstance().getItemCharIdentify(el) == (_session.m_pi.ei.char_info._typeid & 0x000000FF);
                                            });

                                            if (cch <= 0)
                                            {
                                                throw new exception("[room::requestChangePlayerItemRoom] [Error] PLAYER[UID=" + _session.m_pi.uid + "] o Character[TYPEID=" + Convert.ToString(_session.m_pi.ei.char_info._typeid) + "] equipado nao tem o item Hermes no server. Hacker ou Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                                    1001, 0x57008));
                                            }

                                            if (!_session.m_pi.ei.char_info.isPartEquiped(cch))
                                            {
                                                throw new exception("[room::requestChangePlayerItemRoom] [Error] PLAYER[UID=" + _session.m_pi.uid + "] o Character[TYPEID=" + Convert.ToString(_session.m_pi.ei.char_info._typeid) + "] nao esta com o item[TYPEID=" + Convert.ToString(cch) + "] Hermes equipado. Hacker ou Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 1002, 0x57009));
                                            }

                                            it.Value.walk_speed = (it.Value.walk_speed > 1.0f) ? 1.0f : 2.0f;

                                            break;
                                        }
                                } // End Switch

                            }
                            else
                            {
                                // else == 3 // Twilight (Fogos de artifícios em cima da cabeça do Player)
                                // Valor 1 pass para fazer o fogos                                              
                                var cct = cadie_cauldron_Twilight_item_typeid.FirstOrDefault(el =>
                                {
                                    return sIff.getInstance().getItemCharIdentify(el) == (_session.m_pi.ei.char_info._typeid & 0x000000FF);
                                });

                                if (cct <= 0)
                                {
                                    throw new exception("[room::requestChangePlayerItemRoom] [Error] PLAYER[UID=" + _session.m_pi.uid + "] o Character[TYPEID=" + Convert.ToString(_session.m_pi.ei.char_info._typeid) + "] equipado nao tem o item Twilight no server. Hacker ou Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                        1001, 0x57008));
                                }

                                if (!_session.m_pi.ei.char_info.isPartEquiped(cct))
                                {
                                    throw new exception("[room::requestChangePlayerItemRoom] [Error] PLAYER[UID=" + _session.m_pi.uid + "] o Character[TYPEID=" + Convert.ToString(_session.m_pi.ei.char_info._typeid) + "] nao esta com o item[TYPEID=" + Convert.ToString(cct) + "] Twilight equipado. Hacker ou Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                        1002, 0x57009));
                                }
                            }

                            packet_func.room_broadcast(this,
                                packet_func.pacote04B(
                                _session, (byte)_cpir.type, error,
                                (int)_cpir.effect_lounge.effect), 1);
                        }
                        break;
                    case ChangePlayerItemRoom.TYPE_CHANGE.TC_ALL://envia isso quando é pra inicia a sala
                        {

                            // Aqui se não tiver os itens, algum hacker, gera Log, e coloca item padrão ou nenhum
                            CharacterInfo pCe = null;
                            CaddieInfoEx pCi = null;
                            WarehouseItemEx pWi = null;
                            error = 0;

                            // Character
                            if (_cpir.character != 0 && (pCe = _session.m_pi.findCharacterById(_cpir.character)) != null
                                    && sIff.getInstance().getItemGroupIdentify(pCe._typeid) == IFF_GROUP.CHARACTER)
                            {

                                _session.m_pi.ei.char_info = pCe;
                                _session.m_pi.ue.character_id = _cpir.character;

                                // Update ON DB
                                snmdb.NormalManagerDB.getInstance().add(0, new CmdUpdateCharacterEquiped(_session.m_pi.uid, (int)_cpir.character), SQLDBResponse, this);

                            }
                            else
                            {

                                error = (_cpir.character == 0) ? 1/*client give invalid item id*/ : (pCe == null ? 2/*Item Not Found*/ : 3/*Erro item typeid invalid*/);

                                if (_session.m_pi.mp_ce.Count() > 0)
                                {

                                    _smp.message_pool.getInstance().push(new message("[room::requestChangePlayerItemRoom][Info][Warning] PLAYER[UID=" + (_session.m_pi.uid)
                                            + "] tentou trocar o Character[ID=" + (_cpir.character) + "] para comecar o jogo, mas deu Error[VALUE="
                                            + (error) + "], colocando o primeiro character do player. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                                    _session.m_pi.ei.char_info = _session.m_pi.mp_ce.First().Value;
                                    _cpir.character = _session.m_pi.ue.character_id = _session.m_pi.ei.char_info.id;

                                    // Update ON DB
                                    snmdb.NormalManagerDB.getInstance().add(0, new CmdUpdateCharacterEquiped(_session.m_pi.uid, (int)_cpir.character), SQLDBResponse, this);

                                }
                                else
                                {

                                    _smp.message_pool.getInstance().push(new message("[room::requestChangePlayerItemRoom][Info][Warning] PLAYER[UID=" + (_session.m_pi.uid)
                                            + "] tentou trocar o Character[ID=" + (_cpir.character) + "] para comecar o jogo, mas deu Error[VALUE="
                                            + (error) + "], ele nao tem nenhum character, adiciona o Nuri para ele. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                                    BuyItem bi = new BuyItem();
                                    stItem item = new stItem(); ;

                                    bi.id = -1;
                                    bi._typeid = (uint)(sIff.getInstance().CHARACTER << 26);    // Nuri
                                    bi.qntd = 1;

                                    ItemManager.initItemFromBuyItem(_session.m_pi, item, bi, false, 0, 0, 1/*Não verifica o Level*/);

                                    if (item._typeid != 0)
                                    {

                                        // Add Item já atualiza o Character equipado
                                        if ((_cpir.character = (int)ItemManager.addItem(item, _session, 2/*Padrão Item*/, 0)) != Convert.ToUInt32(ItemManager.RetAddItem.T_ERROR))
                                        {

                                            // Update ON GAME
                                            p.init_plain(0x216);

                                            p.WriteUInt32((uint)UtilTime.GetSystemTimeAsUnix());
                                            p.WriteUInt32(1);   // Count

                                            p.WriteByte(item.type);
                                            p.WriteUInt32(item._typeid);
                                            p.WriteInt32(item.id);
                                            p.WriteUInt32(item.flag_time);
                                            p.WriteBytes(item.stat.ToArray());
                                            p.WriteInt32((item.STDA_C_ITEM_TIME > 0) ? item.STDA_C_ITEM_TIME : item.STDA_C_ITEM_QNTD);
                                            p.WriteZeroByte(25);

                                            packet_func.session_send(p, _session, 1);

                                        }
                                        else
                                            _smp.message_pool.getInstance().push(new message("[room::requestChangePlayerItemRoom][Info][Warning] PLAYER[UID=" + (_session.m_pi.uid)
                                                    + "] nao conseguiu adicionar o Character[TYPEID="
                                                    + (item._typeid) + "] para ele. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                                    }
                                    else
                                        _smp.message_pool.getInstance().push(new message("[room::requestChangePlayerItemRoom][Info][Warning] PLAYER[UID=" + (_session.m_pi.uid)
                                                + "] nao conseguiu inicializar o Character[TYPEID="
                                                + (bi._typeid) + "] para ele. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                                }
                            }

                            // Caddie
                            if (_cpir.caddie != 0 && (pCi = _session.m_pi.findCaddieById(_cpir.caddie)) != null
                                    && sIff.getInstance().getItemGroupIdentify(pCi._typeid) == IFF_GROUP.CADDIE)
                            {

                                // Check if item is in map of update item
                                var v_it = _session.m_pi.findUpdateItemByTypeidAndId(pCi._typeid, pCi.id);

                                if (v_it.Count == 0)
                                {

                                    foreach (var el in v_it)
                                    {

                                        if (el.Value.type == UpdateItem.UI_TYPE.CADDIE)
                                        {

                                            // Desequipa o caddie
                                            _session.m_pi.ei.cad_info = null;
                                            _session.m_pi.ue.caddie_id = 0;

                                            _cpir.caddie = 0;

                                        }
                                        else if (el.Value.type == UpdateItem.UI_TYPE.CADDIE_PARTS)
                                        {

                                            // Limpa o caddie Parts
                                            pCi.parts_typeid = 0u;
                                            pCi.parts_end_date_unix = 0;
                                            pCi.end_parts_date = new SYSTEMTIME();

                                            _session.m_pi.ei.cad_info = pCi;
                                            _session.m_pi.ue.caddie_id = _cpir.caddie;
                                        }

                                        // Tira esse Update Item do map
                                        _session.m_pi.mp_ui.Remove(el.Key);
                                    }

                                }
                                else
                                {

                                    // Caddie is Good, Update caddie equiped ON SERVER AND DB
                                    _session.m_pi.ei.cad_info = pCi;
                                    _session.m_pi.ue.caddie_id = _cpir.caddie;

                                    // Verifica se o Caddie pode equipar
                                    if (_session.checkCaddieEquiped(_session.m_pi.ue))
                                        _cpir.caddie = _session.m_pi.ue.caddie_id;

                                }

                                // Update ON DB
                                snmdb.NormalManagerDB.getInstance().add(0, new CmdUpdateCaddieEquiped(_session.m_pi.uid, (int)_cpir.caddie), SQLDBResponse, this);

                            }
                            else if (_session.m_pi.ue.caddie_id > 0 && _session.m_pi.ei.cad_info != null)
                            {   // Desequipa Caddie

                                error = (_cpir.caddie == 0) ? 1/*client give invalid item id*/ : (pCi == null ? 2/*Item Not Found*/ : 3/*Erro item typeid invalid*/);

                                if (error > 1)
                                {
                                    _smp.message_pool.getInstance().push(new message("[room::requestChangePlayerItemRoom][Info][Warning] PLAYER[UID=" + (_session.m_pi.uid)
                                            + "] tentou trocar o Caddie[ID=" + (_cpir.caddie)
                                            + "] para comecar o jogo, mas deu Error[VALUE=" + (error) + "], desequipando o caddie. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                                }

                                // Check if item is in map of update item
                                var v_it = _session.m_pi.findUpdateItemByTypeidAndId(_session.m_pi.ei.cad_info._typeid, _session.m_pi.ei.cad_info.id);

                                if (v_it.Count == 0)
                                {

                                    foreach (var el in v_it)
                                    {

                                        // Caddie já vai se desequipar, só verifica o parts
                                        if (el.Value.type == UpdateItem.UI_TYPE.CADDIE_PARTS)
                                        {

                                            // Limpa o caddie Parts
                                            _session.m_pi.ei.cad_info.parts_typeid = 0u;
                                            _session.m_pi.ei.cad_info.parts_end_date_unix = 0;
                                            _session.m_pi.ei.cad_info.end_parts_date = new SYSTEMTIME();
                                        }

                                        // Tira esse Update Item do map
                                        _session.m_pi.mp_ui.Remove(el.Key);
                                    }

                                }

                                _session.m_pi.ei.cad_info = null;
                                _session.m_pi.ue.caddie_id = 0;

                                _cpir.caddie = 0;

                                // Att No DB
                                snmdb.NormalManagerDB.getInstance().add(0, new CmdUpdateCaddieEquiped(_session.m_pi.uid, (int)_cpir.caddie), SQLDBResponse, this);
                            }

                            // ClubSet
                            if (_cpir.clubset != 0 && (pWi = _session.m_pi.findWarehouseItemById(_cpir.clubset)) != null
                                    && sIff.getInstance().getItemGroupIdentify(pWi._typeid) == IFF_GROUP.CLUBSET)
                            {

                                var c_it = _session.m_pi.findUpdateItemByTypeidAndType((uint)_cpir.clubset, UpdateItem.UI_TYPE.WAREHOUSE);

                                if (c_it.Count <= 0)
                                {

                                    _session.m_pi.ei.clubset = pWi;

                                    // Esse C do WarehouseItem, que pega do DB, não é o ja updado inicial da taqueira é o que fica tabela enchant, 
                                    // que no original fica no warehouse msm, eu só confundi quando fiz
                                    _session.m_pi.ei.csi.setValues(pWi.id, pWi._typeid, pWi.c);

                                    var cs = sIff.getInstance().findClubSet(pWi._typeid);

                                    if (cs != null)
                                    {

                                        for (var j = 0; j < 5; ++j)
                                            _session.m_pi.ei.csi.enchant_c[j] = (short)(cs.SlotStats.getSlot[j] + pWi.clubset_workshop.c[j]);

                                        _session.m_pi.ue.clubset_id = _cpir.clubset;

                                        // Verifica se o ClubSet pode equipar
                                        if (_session.checkClubSetEquiped(_session.m_pi.ue))
                                            _cpir.clubset = _session.m_pi.ue.clubset_id;

                                        // Update ON DB
                                        snmdb.NormalManagerDB.getInstance().add(0, new CmdUpdateClubsetEquiped(_session.m_pi.uid, (int)_cpir.clubset), SQLDBResponse, this);

                                    }
                                    else
                                    {

                                        error = 5/*Item Not Found ON IFF_STRUCT SERVER*/;

                                        _smp.message_pool.getInstance().push(new message("[room::requestChangePlayerItemRoom] [Error] PLAYER[UID=" + (_session.m_pi.uid) + "] tentou Atualizar Clubset[TYPEID="
                                                + (pWi._typeid) + ", ID=" + (pWi.id)
                                                + "] equipado, mas ClubSet Not exists on IFF structure. Equipa o ClubSet padrao. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                                        // Coloca o ClubSet CV1 no lugar do ClubSet que acabou o tempo
                                        pWi = _session.m_pi.findWarehouseItemByTypeid(AIR_KNIGHT_SET);

                                        if (pWi != null)
                                        {

                                            _smp.message_pool.getInstance().push(new message("[room::requestChangePlayerItemRoom][Info][Warning] PLAYER[UID=" + (_session.m_pi.uid)
                                                    + "] tentou trocar o ClubSet[ID=" + (_cpir.clubset) + "] para comecar o jogo, mas acabou o tempo do ClubSet[ID="
                                                    + (_cpir.clubset) + "], colocando o ClubSet Padrao\"CV1\" do player. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                                            // Esse C do WarehouseItem, que pega do DB, não é o ja updado inicial da taqueira é o que fica tabela enchant, 
                                            // que no original fica no warehouse msm, eu só confundi quando fiz
                                            _session.m_pi.ei.csi.setValues(pWi.id, pWi._typeid, pWi.c);

                                            cs = sIff.getInstance().findClubSet(pWi._typeid);

                                            if (cs != null)
                                                for (var j = 0; j < 5; ++j)
                                                    _session.m_pi.ei.csi.enchant_c[j] = (short)(cs.SlotStats.getSlot[j] + pWi.clubset_workshop.c[j]);

                                            _session.m_pi.ei.clubset = pWi;
                                            _cpir.clubset = _session.m_pi.ue.clubset_id = pWi.id;

                                            // Update ON DB
                                            snmdb.NormalManagerDB.getInstance().add(0, new CmdUpdateClubsetEquiped(_session.m_pi.uid, (int)_cpir.clubset), SQLDBResponse, this);

                                        }
                                        else
                                        {

                                            _smp.message_pool.getInstance().push(new message("[channel::requestChangePlayerItemRoom][Info][Warning] PLAYER[UID=" + (_session.m_pi.uid)
                                                    + "] tentou trocar o ClubSet[ID=" + (_cpir.clubset) + "] para comecar o jogo, mas acabou o tempo do ClubSet[ID="
                                                    + (_cpir.clubset) + "], ele nao tem o ClubSet Padrao\"CV1\", adiciona o ClubSet pardrao\"CV1\" para ele. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                                            BuyItem bi = new BuyItem();
                                            stItem item = new stItem(); ;

                                            bi.id = -1;
                                            bi._typeid = AIR_KNIGHT_SET;
                                            bi.qntd = 1;

                                            ItemManager.initItemFromBuyItem(_session.m_pi, item, bi, false, 0, 0, 1/*Não verifica o Level*/);

                                            if (item._typeid != 0)
                                            {

                                                if ((_cpir.clubset = (int)ItemManager.addItem(item, _session, 2/*Padrão Item*/, 0)) != Convert.ToUInt32(ItemManager.RetAddItem.T_ERROR))
                                                {

                                                    // Equipa o ClubSet CV1
                                                    pWi = _session.m_pi.findWarehouseItemById(_cpir.clubset);

                                                    if (pWi != null)
                                                    {

                                                        // Esse C do WarehouseItem, que pega do DB, não é o ja updado inicial da taqueira é o que fica tabela enchant, 
                                                        // que no original fica no warehouse msm, eu só confundi quando fiz
                                                        _session.m_pi.ei.csi.setValues(pWi.id, pWi._typeid, pWi.c);

                                                        cs = sIff.getInstance().findClubSet(pWi._typeid);

                                                        if (cs != null)
                                                            for (var j = 0; j < 5; ++j)
                                                                _session.m_pi.ei.csi.enchant_c[j] = (short)(cs.SlotStats.getSlot[j] + pWi.clubset_workshop.c[j]);

                                                        _session.m_pi.ei.clubset = pWi;
                                                        _session.m_pi.ue.clubset_id = pWi.id;

                                                        // Update ON DB
                                                        snmdb.NormalManagerDB.getInstance().add(0, new CmdUpdateClubsetEquiped(_session.m_pi.uid, (int)_cpir.clubset), SQLDBResponse, this);

                                                        // Update ON GAME
                                                        p.init_plain(0x216);

                                                        p.WriteUInt32((uint)UtilTime.GetSystemTimeAsUnix());
                                                        p.WriteUInt32(1);   // Count

                                                        p.WriteByte(item.type);
                                                        p.WriteUInt32(item._typeid);
                                                        p.WriteInt32(item.id);
                                                        p.WriteUInt32(item.flag_time);
                                                        p.WriteBytes(item.stat.ToArray());
                                                        p.WriteInt32((item.STDA_C_ITEM_TIME > 0) ? item.STDA_C_ITEM_TIME : item.STDA_C_ITEM_QNTD);
                                                        p.WriteZeroByte(25);

                                                        packet_func.session_send(p, _session, 1);

                                                    }
                                                    else
                                                        _smp.message_pool.getInstance().push(new message("[room::requestChangePlayerItemRoom][Info][Warning] PLAYER[UID=" + (_session.m_pi.uid)
                                                                + "] nao conseguiu achar o ClubSet\"CV1\"[ID="
                                                                + (item.id) + "] que acabou de adicionar para ele. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                                                }
                                                else
                                                    _smp.message_pool.getInstance().push(new message("[room::requestChangePlayerItemRoom][Info][Warning] PLAYER[UID=" + (_session.m_pi.uid)
                                                            + "] nao conseguiu adicionar o ClubSet[TYPEID="
                                                            + (item._typeid) + "] para ele. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                                            }
                                            else
                                                _smp.message_pool.getInstance().push(new message("[room::requestChangePlayerItemRoom][Info][Warning] PLAYER[UID=" + (_session.m_pi.uid)
                                                        + "] nao conseguiu inicializar o ClubSet[TYPEID="
                                                        + (bi._typeid) + "] para ele. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                                        }
                                    }

                                }
                                else
                                {   // ClubSet Acabou o tempo

                                    // Coloca o ClubSet CV1 no lugar do ClubSet que acabou o tempo
                                    pWi = _session.m_pi.findWarehouseItemByTypeid(AIR_KNIGHT_SET);

                                    if (pWi != null)
                                    {

                                        _smp.message_pool.getInstance().push(new message("[room::requestChangePlayerItemRoom][Info][Warning] PLAYER[UID=" + (_session.m_pi.uid)
                                                + "] tentou trocar o ClubSet[ID=" + (_cpir.clubset) + "] para comecar o jogo, mas acabou o tempo do ClubSet[ID="
                                                + (_cpir.clubset) + "], colocando o ClubSet Padrao\"CV1\" do player. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                                        // Esse C do WarehouseItem, que pega do DB, não é o ja updado inicial da taqueira é o que fica tabela enchant, 
                                        // que no original fica no warehouse msm, eu só confundi quando fiz
                                        _session.m_pi.ei.csi.setValues(pWi.id, pWi._typeid, pWi.c);

                                        var cs = sIff.getInstance().findClubSet(pWi._typeid);

                                        if (cs != null)
                                            for (var j = 0; j < 5; ++j)
                                                _session.m_pi.ei.csi.enchant_c[j] = (short)(cs.SlotStats.getSlot[j] + pWi.clubset_workshop.c[j]);

                                        _session.m_pi.ei.clubset = pWi;
                                        _cpir.clubset = _session.m_pi.ue.clubset_id = pWi.id;

                                        // Update ON DB
                                        snmdb.NormalManagerDB.getInstance().add(0, new CmdUpdateClubsetEquiped(_session.m_pi.uid, (int)_cpir.clubset), SQLDBResponse, this);

                                    }
                                    else
                                    {

                                        _smp.message_pool.getInstance().push(new message("[room::requestChangePlayerItemRoom][Info][Warning] PLAYER[UID=" + (_session.m_pi.uid)
                                                + "] tentou trocar o ClubSet[ID=" + (_cpir.clubset) + "] para comecar o jogo, mas acabou o tempo do ClubSet[ID="
                                                + (_cpir.clubset) + "], ele nao tem o ClubSet Padrao\"CV1\", adiciona o ClubSet pardrao\"CV1\" para ele. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                                        BuyItem bi = new BuyItem();
                                        stItem item = new stItem(); ;

                                        bi.id = -1;
                                        bi._typeid = AIR_KNIGHT_SET;
                                        bi.qntd = 1;

                                        ItemManager.initItemFromBuyItem(_session.m_pi, item, bi, false, 0, 0, 1/*Não verifica o Level*/);

                                        if (item._typeid != 0)
                                        {

                                            if ((_cpir.clubset = (int)ItemManager.addItem(item, _session, 2/*Padrão Item*/, 0)) != Convert.ToUInt32(ItemManager.RetAddItem.T_ERROR))
                                            {

                                                // Equipa o ClubSet CV1
                                                pWi = _session.m_pi.findWarehouseItemById(_cpir.clubset);

                                                if (pWi != null)
                                                {

                                                    // Esse C do WarehouseItem, que pega do DB, não é o ja updado inicial da taqueira é o que fica tabela enchant, 
                                                    // que no original fica no warehouse msm, eu só confundi quando fiz
                                                    _session.m_pi.ei.csi.setValues(pWi.id, pWi._typeid, pWi.c);

                                                    var cs = sIff.getInstance().findClubSet(pWi._typeid);

                                                    if (cs != null)
                                                        for (var j = 0; j < 5; ++j)
                                                            _session.m_pi.ei.csi.enchant_c[j] = (short)(cs.SlotStats.getSlot[j] + pWi.clubset_workshop.c[j]);

                                                    _session.m_pi.ei.clubset = pWi;
                                                    _session.m_pi.ue.clubset_id = pWi.id;

                                                    // Update ON DB
                                                    snmdb.NormalManagerDB.getInstance().add(0, new CmdUpdateClubsetEquiped(_session.m_pi.uid, (int)_cpir.clubset), SQLDBResponse, this);

                                                    // Update ON GAME
                                                    p.init_plain(0x216);

                                                    p.WriteUInt32((uint)UtilTime.GetSystemTimeAsUnix());
                                                    p.WriteUInt32(1);   // Count

                                                    p.WriteByte(item.type);
                                                    p.WriteUInt32(item._typeid);
                                                    p.WriteInt32(item.id);
                                                    p.WriteUInt32(item.flag_time);
                                                    p.WriteBytes(item.stat.ToArray());
                                                    p.WriteInt32((item.STDA_C_ITEM_TIME > 0) ? item.STDA_C_ITEM_TIME : item.STDA_C_ITEM_QNTD);
                                                    p.WriteZeroByte(25);

                                                    packet_func.session_send(p, _session, 1);

                                                }
                                                else
                                                    _smp.message_pool.getInstance().push(new message("[room::requestChangePlayerItemRoom][Info][Warning] PLAYER[UID=" + (_session.m_pi.uid)
                                                            + "] nao conseguiu achar o ClubSet\"CV1\"[ID="
                                                            + (item.id) + "] que acabou de adicionar para ele. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                                            }
                                            else
                                                _smp.message_pool.getInstance().push(new message("[room::requestChangePlayerItemRoom][Info][Warning] PLAYER[UID=" + (_session.m_pi.uid)
                                                        + "] nao conseguiu adicionar o ClubSet[TYPEID="
                                                        + (item._typeid) + "] para ele. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                                        }
                                        else
                                            _smp.message_pool.getInstance().push(new message("[room::requestChangePlayerItemRoom][Info][Warning] PLAYER[UID=" + (_session.m_pi.uid)
                                                    + "] nao conseguiu inicializar o ClubSet[TYPEID="
                                                    + (bi._typeid) + "] para ele. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                                    }
                                }

                            }
                            else
                            {

                                error = (_cpir.clubset == 0) ? 1/*client give invalid item id*/ : (pWi == null ? 2/*Item Not Found*/ : 3/*Erro item typeid invalid*/);

                                pWi = _session.m_pi.findWarehouseItemByTypeid(AIR_KNIGHT_SET);

                                if (pWi != null)
                                {

                                    _smp.message_pool.getInstance().push(new message("[room::requestChangePlayerItemRoom][Info][Warning] PLAYER[UID=" + (_session.m_pi.uid)
                                            + "] tentou trocar o ClubSet[ID=" + (_cpir.clubset) + "] para comecar o jogo, mas deu Error[VALUE="
                                            + (error) + "], colocando o ClubSet Padrao\"CV1\" do player. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                                    // Esse C do WarehouseItem, que pega do DB, não é o ja updado inicial da taqueira é o que fica tabela enchant, 
                                    // que no original fica no warehouse msm, eu só confundi quando fiz
                                    _session.m_pi.ei.csi.setValues(pWi.id, pWi._typeid, pWi.c);

                                    var cs = sIff.getInstance().findClubSet(pWi._typeid);

                                    if (cs != null)
                                        for (var j = 0; j < 5; ++j)
                                            _session.m_pi.ei.csi.enchant_c[j] = (short)(cs.SlotStats.getSlot[j] + pWi.clubset_workshop.c[j]);

                                    _session.m_pi.ei.clubset = pWi;
                                    _cpir.clubset = _session.m_pi.ue.clubset_id = pWi.id;

                                    // Update ON DB
                                    snmdb.NormalManagerDB.getInstance().add(0, new CmdUpdateClubsetEquiped(_session.m_pi.uid, (int)_cpir.clubset), SQLDBResponse, this);

                                }
                                else
                                {

                                    _smp.message_pool.getInstance().push(new message("[room::requestChangePlayerItemRoom][Info][Warning] PLAYER[UID=" + (_session.m_pi.uid)
                                            + "] tentou trocar o ClubSet[ID=" + (_cpir.clubset) + "] para comecar o jogo, mas deu Error[VALUE="
                                            + (error) + "], ele nao tem o ClubSet Padrao\"CV1\", adiciona o ClubSet pardrao\"CV1\" para ele. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                                    BuyItem bi = new BuyItem();
                                    stItem item = new stItem(); ;

                                    bi.id = -1;
                                    bi._typeid = AIR_KNIGHT_SET;
                                    bi.qntd = 1;

                                    ItemManager.initItemFromBuyItem(_session.m_pi, item, bi, false, 0, 0, 1/*Não verifica o Level*/);

                                    if (item._typeid != 0)
                                    {

                                        if ((_cpir.clubset = (int)ItemManager.addItem(item, _session, 2/*Padrão Item*/, 0)) != Convert.ToUInt32(ItemManager.RetAddItem.T_ERROR))
                                        {

                                            // Equipa o ClubSet CV1
                                            pWi = _session.m_pi.findWarehouseItemById(_cpir.clubset);

                                            if (pWi != null)
                                            {

                                                // Esse C do WarehouseItem, que pega do DB, não é o ja updado inicial da taqueira é o que fica tabela enchant, 
                                                // que no original fica no warehouse msm, eu só confundi quando fiz
                                                _session.m_pi.ei.csi.setValues(pWi.id, pWi._typeid, pWi.c);

                                                var cs = sIff.getInstance().findClubSet(pWi._typeid);

                                                if (cs != null)
                                                    for (var j = 0; j < 5; ++j)
                                                        _session.m_pi.ei.csi.enchant_c[j] = (short)(cs.SlotStats.getSlot[j] + pWi.clubset_workshop.c[j]);

                                                _session.m_pi.ei.clubset = pWi;
                                                _session.m_pi.ue.clubset_id = pWi.id;

                                                // Update ON DB
                                                snmdb.NormalManagerDB.getInstance().add(0, new CmdUpdateClubsetEquiped(_session.m_pi.uid, (int)_cpir.clubset), SQLDBResponse, this);

                                                // Update ON GAME
                                                p.init_plain(0x216);

                                                p.WriteUInt32((uint)UtilTime.GetSystemTimeAsUnix());
                                                p.WriteUInt32(1);   // Count

                                                p.WriteByte(item.type);
                                                p.WriteUInt32(item._typeid);
                                                p.WriteInt32(item.id);
                                                p.WriteUInt32(item.flag_time);
                                                p.WriteBytes(item.stat.ToArray());
                                                p.WriteInt32((item.STDA_C_ITEM_TIME > 0) ? item.STDA_C_ITEM_TIME : item.STDA_C_ITEM_QNTD);
                                                p.WriteZeroByte(25);

                                                packet_func.session_send(p, _session, 1);

                                            }
                                            else
                                                _smp.message_pool.getInstance().push(new message("[room::requestChangePlayerItemRoom][Info][Warning] PLAYER[UID=" + (_session.m_pi.uid)
                                                        + "] nao conseguiu achar o ClubSet\"CV1\"[ID="
                                                        + (item.id) + "] que acabou de adicionar para ele. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                                        }
                                        else
                                            _smp.message_pool.getInstance().push(new message("[room::requestChangePlayerItemRoom][Info][Warning] PLAYER[UID=" + (_session.m_pi.uid)
                                                    + "] nao conseguiu adicionar o ClubSet[TYPEID="
                                                    + (item._typeid) + "] para ele. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                                    }
                                    else
                                        _smp.message_pool.getInstance().push(new message("[room::requestChangePlayerItemRoom][Info][Warning] PLAYER[UID=" + (_session.m_pi.uid)
                                                + "] nao conseguiu inicializar o ClubSet[TYPEID="
                                                + (bi._typeid) + "] para ele. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                                }
                            }

                            // Ball(Comet)
                            if (_cpir.ball != 0 && (pWi = _session.m_pi.findWarehouseItemByTypeid(_cpir.ball)) != null
                                    && sIff.getInstance().getItemGroupIdentify(pWi._typeid) == IFF_GROUP.BALL)
                            {

                                _session.m_pi.ei.comet = pWi;
                                _session.m_pi.ue.ball_typeid = _cpir.ball;      // Ball(Comet) é o typeid que o cliente passa

                                // Verifica se a Bola pode ser equipada
                                if (_session.checkBallEquiped(_session.m_pi.ue))
                                    _cpir.ball = _session.m_pi.ue.ball_typeid;

                                // Update ON DB
                                snmdb.NormalManagerDB.getInstance().add(0, new CmdUpdateBallEquiped(_session.m_pi.uid, _cpir.ball), SQLDBResponse, this);

                            }
                            else if (_cpir.ball == 0)
                            { // Bola 0 coloca a bola padrão para ele, se for premium user coloca a bola de premium user

                                // Zera para equipar a bola padrão
                                _session.m_pi.ei.comet = null;
                                _session.m_pi.ue.ball_typeid = 0;

                                // Verifica se a Bola pode ser equipada (Coloca para equipar a bola padrão
                                if (_session.checkBallEquiped(_session.m_pi.ue))
                                    _cpir.ball = _session.m_pi.ue.ball_typeid;

                                // Update ON DB
                                snmdb.NormalManagerDB.getInstance().add(0, new CmdUpdateBallEquiped(_session.m_pi.uid, _cpir.ball), SQLDBResponse, this);

                            }
                            else
                            {

                                error = (pWi == null ? 2/*Item Not Found*/ : 3/*Erro item typeid invalid*/);

                                pWi = _session.m_pi.findWarehouseItemByTypeid(DEFAULT_COMET_TYPEID);

                                if (pWi != null)
                                {

                                    _smp.message_pool.getInstance().push(new message("[room::requestChangePlayerItemRoom][Info][Warning] PLAYER[UID=" + (_session.m_pi.uid)
                                            + "] tentou trocar a Ball[TYPEID=" + (_cpir.ball) + "] para comecar o jogo, mas deu Error[VALUE="
                                            + (error) + "], colocando a Ball Padrao do player. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                                    _session.m_pi.ei.comet = pWi;
                                    _cpir.ball = _session.m_pi.ue.ball_typeid = pWi._typeid;

                                    // Update ON DB
                                    snmdb.NormalManagerDB.getInstance().add(0, new CmdUpdateBallEquiped(_session.m_pi.uid, _cpir.ball), SQLDBResponse, this);

                                }
                                else
                                {

                                    _smp.message_pool.getInstance().push(new message("[room::requestChangePlayerItemRoom][Info][Warning] PLAYER[UID=" + (_session.m_pi.uid)
                                            + "] tentou trocar a Ball[TYPEID=" + (_cpir.ball) + "] para comecar o jogo, mas deu Error[VALUE="
                                            + (error) + "], ele nao tem a Ball Padrao, adiciona a Ball pardrao para ele. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                                    BuyItem bi = new BuyItem();
                                    stItem item = new stItem(); ;

                                    bi.id = -1;
                                    bi._typeid = DEFAULT_COMET_TYPEID;
                                    bi.qntd = 1;

                                    ItemManager.initItemFromBuyItem(_session.m_pi, item, bi, false, 0, 0, 1/*Não verifica o Level*/);

                                    if (item._typeid != 0)
                                    {

                                        if ((_cpir.ball = (uint)ItemManager.addItem(item, _session, 2/*Padrão Item*/, 0)) != Convert.ToUInt32(ItemManager.RetAddItem.T_ERROR))
                                        {

                                            // Equipa a Ball padrao
                                            pWi = _session.m_pi.findWarehouseItemById((int)_cpir.ball);

                                            if (pWi != null)
                                            {

                                                _session.m_pi.ei.comet = pWi;
                                                _session.m_pi.ue.ball_typeid = pWi._typeid;

                                                // Update ON DB
                                                snmdb.NormalManagerDB.getInstance().add(0, new CmdUpdateBallEquiped(_session.m_pi.uid, _cpir.ball), SQLDBResponse, this);

                                                // Update ON GAME
                                                p.init_plain(0x216);

                                                p.WriteUInt32((uint)UtilTime.GetSystemTimeAsUnix());
                                                p.WriteUInt32(1);   // Count

                                                p.WriteByte(item.type);
                                                p.WriteUInt32(item._typeid);
                                                p.WriteInt32(item.id);
                                                p.WriteUInt32(item.flag_time);
                                                p.WriteBytes(item.stat.ToArray());
                                                p.WriteInt32((item.STDA_C_ITEM_TIME > 0) ? item.STDA_C_ITEM_TIME : item.STDA_C_ITEM_QNTD);
                                                p.WriteZeroByte(25);

                                                packet_func.session_send(p, _session, 1);

                                            }
                                            else
                                                _smp.message_pool.getInstance().push(new message("[room::requestChangePlayerItemRoom][Info][Warning] PLAYER[UID=" + (_session.m_pi.uid)
                                                        + "] nao conseguiu achar a Ball[ID="
                                                        + (item.id) + "] que acabou de adicionar para ele. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                                        }
                                        else
                                            _smp.message_pool.getInstance().push(new message("[room::requestChangePlayerItemRoom][Info][Warning] PLAYER[UID=" + (_session.m_pi.uid)
                                                    + "] nao conseguiu adicionar a Ball[TYPEID="
                                                    + (item._typeid) + "] para ele. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                                    }
                                    else
                                        _smp.message_pool.getInstance().push(new message("[channel::requestChangePlayerItemRoom][Info][Warning] PLAYER[UID=" + (_session.m_pi.uid)
                                                + "] nao conseguiu inicializar a Ball[TYPEID="
                                                + (bi._typeid) + "] para ele. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                                }
                            }

                            // Verifica se o Mascot Equipado acabou o tempo
                            if (_session.m_pi.ue.mascot_id != 0 && _session.m_pi.ei.mascot_info != null)
                            {
                                //FindUpdateItemByIdAndType
                                var m_it = _session.m_pi.findUpdateItemByTypeidAndType((uint)_session.m_pi.ue.mascot_id, UpdateItem.UI_TYPE.MASCOT);

                                if (m_it.Count > 0)
                                {

                                    // Desequipa o Mascot que acabou o tempo dele
                                    _session.m_pi.ei.mascot_info = null;
                                    _session.m_pi.ue.mascot_id = 0;

                                    snmdb.NormalManagerDB.getInstance().add(0, new CmdUpdateMascotEquiped(_session.m_pi.uid, 0/*Mascot_id == 0 not equiped*/), SQLDBResponse, this);

                                    // Update on GAME se não o cliente continua com o mascot equipado
                                    packet_func.pacote04B(_session, (byte)ChangePlayerItemRoom.TYPE_CHANGE.TC_MASCOT, 0);
                                    packet_func.session_send(p, _session, 0);

                                }
                            }

                            // Começa jogo
                            startGame(_session);
                        }
                        break;
                    default:
                        throw new exception("[room::requestChangePlayerItemRoom] [Error] sala[NUMERO=" + Convert.ToString(getNumero()) + "] type desconhecido.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                           13, 1));
                }
                updatePlayerInfo(_session);
            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[room::requestChangePlayerItemRoom][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                packet_func.pacote04B(_session, (byte)_cpir.type,
                    (int)(ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) == (uint)STDA_ERROR_TYPE.ROOM ? ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) : 1));
                packet_func.session_send(p,
                    _session, 0);
            }
        }
		
		private int HandleChangeCaddie(Player _session, int caddie)
        {
            var p = new PangyaBinaryWriter();
            int error = 0/*SUCCESS*/;
            CaddieInfoEx pCi = null;

            // Caddie
            if (caddie != 0 && (pCi = _session.m_pi.findCaddieById(caddie)) != null
                    && sIff.getInstance().getItemGroupIdentify(pCi._typeid) == IFF_GROUP.CADDIE)
            {

                // Check if item is in map of update item
                var v_it = _session.m_pi.findUpdateItemByTypeidAndId(pCi._typeid, pCi.id);

                if (v_it.Any())
                {

                    foreach (var el in v_it)
                    {

                        if (el.Value.type == UpdateItem.UI_TYPE.CADDIE)
                        {

                            // Desequipa o caddie
                            _session.m_pi.ei.cad_info = null;
                            _session.m_pi.ue.caddie_id = 0;

                            caddie = 0;

                        }
                        else if (el.Value.type == UpdateItem.UI_TYPE.CADDIE_PARTS)
                        {

                            // Limpa o caddie Parts
                            pCi.parts_typeid = 0;
                            pCi.parts_end_date_unix = 0;
                            pCi.end_parts_date = new SYSTEMTIME();

                            _session.m_pi.ei.cad_info = pCi;
                            _session.m_pi.ue.caddie_id = caddie;
                        }

                        // Tira esse Update Item do map
                        _session.m_pi.mp_ui.Remove(el.Key);
                    }

                }
                else
                {

                    // Caddie is Good, Update caddie equiped ON SERVER AND DB
                    _session.m_pi.ei.cad_info = pCi;
                    _session.m_pi.ue.caddie_id = caddie;

                    // Verifica se o Caddie pode ser equipado
                    if (_session.checkCaddieEquiped(_session.m_pi.ue))
                        caddie = _session.m_pi.ue.caddie_id;

                }

                // Update ON DB
                snmdb.NormalManagerDB.getInstance().add(0, new CmdUpdateCaddieEquiped(_session.m_pi.uid, caddie), SQLDBResponse, this);

            }
            else if (_session.m_pi.ue.caddie_id > 0 && _session.m_pi.ei.cad_info != null)
            {   // Desequipa Caddie

                error = (caddie == 0) ? 1/*client give invalid item id*/ : (pCi == null ? 2/*Item Not Found*/ : 3/*Erro item typeid invalid*/);

                if (error > 1)
                {
                    _smp.message_pool.getInstance().push(new message("[room::requestChangePlayerItemRoom][Info][Warning] PLAYER[UID=" + (_session.m_pi.uid)
                            + "] tentou trocar o Caddie[ID=" + (caddie) + "] para comecar o jogo, mas deu Error[VALUE="
                            + (error) + "], desequipando o caddie. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                }

                // Check if item is in map of update item
                var v_it = _session.m_pi.findUpdateItemByTypeidAndId(_session.m_pi.ei.cad_info._typeid, _session.m_pi.ei.cad_info.id);

                if (v_it.Any())
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

                caddie = 0;

                // Zera o Error para o cliente desequipar o caddie que o server desequipou
                error = 0;

                // Att No DB
                snmdb.NormalManagerDB.getInstance().add(0, new CmdUpdateCaddieEquiped(_session.m_pi.uid, caddie), SQLDBResponse, this);
            }

            return error;
        }

        private int HandleChangeBall(Player _session, uint ball)
        {
            var p = new PangyaBinaryWriter();

            int error = 0/*SUCCESS*/;
            WarehouseItemEx pWi = null;

            if (ball != 0 && (pWi = _session.m_pi.findWarehouseItemByTypeid(ball)) != null
                    && sIff.getInstance().getItemGroupIdentify(pWi._typeid) == IFF_GROUP.BALL)
            {

                _session.m_pi.ei.comet = pWi;
                _session.m_pi.ue.ball_typeid = ball;      // Ball(Comet) é o typeid que o cliente passa

                // Verifica se a bola pode ser equipada
                if (_session.checkBallEquiped(_session.m_pi.ue))
                    ball = _session.m_pi.ue.ball_typeid;

                // Update ON DB
                snmdb.NormalManagerDB.getInstance().add(0, new CmdUpdateBallEquiped(_session.m_pi.uid, ball), SQLDBResponse, this);

            }
            else if (ball == 0)
            { // Bola 0 coloca a bola padrão para ele, se for premium user coloca a bola de premium user

                // Zera para equipar a bola padrão
                _session.m_pi.ei.comet = null;
                _session.m_pi.ue.ball_typeid = 0;

                // Verifica se a Bola pode ser equipada (Coloca para equipar a bola padrão
                if (_session.checkBallEquiped(_session.m_pi.ue))
                    ball = _session.m_pi.ue.ball_typeid;

                // Update ON DB
                snmdb.NormalManagerDB.getInstance().add(0, new CmdUpdateBallEquiped(_session.m_pi.uid, ball), SQLDBResponse, this);

            }
            else
            {

                error = (pWi == null ? 2 : 3);

                pWi = _session.m_pi.findWarehouseItemByTypeid(DEFAULT_COMET_TYPEID);

                if (pWi != null)
                {

                    _smp.message_pool.getInstance().push(new message("[room::requestChangePlayerItemRoom][Info][Warning] PLAYER[UID=" + (_session.m_pi.uid)
                            + "] tentou trocar a Ball[TYPEID=" + (ball) + "] para comecar o jogo, mas deu Error[VALUE="
                            + (error) + "], colocando a Ball Padrao do player. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                    _session.m_pi.ei.comet = pWi;
                    ball = _session.m_pi.ue.ball_typeid = pWi._typeid;

                    // Zera o Error para o cliente equipar a Ball Padrão que o server equipou
                    error = 0;

                    // Update ON DB
                    snmdb.NormalManagerDB.getInstance().add(0, new CmdUpdateBallEquiped(_session.m_pi.uid, ball), SQLDBResponse, this);

                }
                else
                {

                    _smp.message_pool.getInstance().push(new message("[room::requestChangePlayerItemRoom][Info][Warning] PLAYER[UID=" + (_session.m_pi.uid)
                            + "] tentou trocar a Ball[TYPEID=" + (ball) + "] para comecar o jogo, mas deu Error[VALUE="
                            + (error) + "], ele nao tem a Ball Padrao, adiciona a Ball pardrao para ele. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                    BuyItem bi = new BuyItem();
                    stItem item = new stItem();

                    bi.id = -1;
                    bi._typeid = DEFAULT_COMET_TYPEID;
                    bi.qntd = 1;

                    ItemManager.initItemFromBuyItem(_session.m_pi, item, bi, false, 0, 0, 1);

                    if (item._typeid != 0)
                    {

                        if ((ball = (uint)ItemManager.addItem(item, _session, 2/*Padrão Item*/, 0)) != Convert.ToUInt32(ItemManager.RetAddItem.T_ERROR))
                        {

                            // Equipa a Ball padrao
                            pWi = _session.m_pi.findWarehouseItemById((int)ball);

                            if (pWi != null)
                            {

                                _session.m_pi.ei.comet = pWi;
                                _session.m_pi.ue.ball_typeid = pWi._typeid;

                                // Zera o Error para o cliente equipar a Ball Padrão que o server equipou
                                error = 0;

                                // Update ON DB
                                snmdb.NormalManagerDB.getInstance().add(0, new CmdUpdateBallEquiped(_session.m_pi.uid, ball), SQLDBResponse, this);

                                // Update ON GAME
                                p.init_plain(0x216);

                                p.WriteUInt32((uint)UtilTime.GetSystemTimeAsUnix());
                                p.WriteUInt32(1);   // Count

                                p.WriteByte(item.type);
                                p.WriteUInt32(item._typeid);
                                p.WriteInt32(item.id);
                                p.WriteUInt32(item.flag_time);
                                p.WriteBytes(item.stat.ToArray());
                                p.WriteInt32((item.STDA_C_ITEM_TIME > 0) ? item.STDA_C_ITEM_TIME : item.STDA_C_ITEM_QNTD);
                                p.WriteZero(25);

                                packet_func.session_send(p, _session, 1);

                            }
                            else
                                _smp.message_pool.getInstance().push(new message("[room::requestChangePlayerItemRoom][Info][Warning] PLAYER[UID=" + (_session.m_pi.uid)
                                        + "] nao conseguiu achar a Ball[ID="
                                        + (item.id) + "] que acabou de adicionar para ele. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                        }
                        else
                            _smp.message_pool.getInstance().push(new message("[room::requestChangePlayerItemRoom][Info][Warning] PLAYER[UID=" + (_session.m_pi.uid)
                                    + "] nao conseguiu adicionar a Ball[TYPEID="
                                    + (item._typeid) + "] para ele. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                    }
                    else
                        _smp.message_pool.getInstance().push(new message("[room::requestChangePlayerItemRoom][Info][Warning] PLAYER[UID=" + (_session.m_pi.uid)
                                + "] nao conseguiu inicializar a Ball[TYPEID="
                                + (bi._typeid) + "] para ele. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
            }

            return error;
        }

        private int HandleChangeClubSet(Player _session, int clubset)
        {
            int error = 0/*SUCCESS*/;
            WarehouseItemEx pWi = null;
            var p = new PangyaBinaryWriter();

            // ClubSet
            if (clubset != 0
                && (pWi = _session.m_pi.findWarehouseItemById(clubset)) != null
                && sIff.getInstance().getItemGroupIdentify(pWi._typeid) == IFF_GROUP.CLUBSET)
            {

                var c_it = _session.m_pi.findUpdateItemByTypeidAndType((uint)clubset, UpdateItem.UI_TYPE.WAREHOUSE);

                if (c_it.FirstOrDefault().Key == _session.m_pi.mp_ui.LastOrDefault().Key)
                {

                    _session.m_pi.ei.clubset = pWi;

                    // Esse C do WarehouseItem, que pega do DB, não é o ja updado inicial da taqueira é o que fica tabela enchant,
                    // que no original fica no warehouse msm, eu só confundi quando fiz
                    _session.m_pi.ei.csi.setValues(pWi.id, pWi._typeid, pWi.c);

                    var cs = sIff.getInstance().findClubSet(pWi._typeid);

                    if (cs != null)
                    {

                        for (var j = 0; j < (_session.m_pi.ei.csi.enchant_c.Length); ++j)
                        {
                            _session.m_pi.ei.csi.enchant_c[j] = (short)(cs.SlotStats.getSlot[j] + pWi.clubset_workshop.c[j]);
                        }

                        _session.m_pi.ue.clubset_id = clubset;

                        // Verifica se o ClubSet pode ser equipado
                        if (_session.checkClubSetEquiped(_session.m_pi.ue))
                        {
                            clubset = _session.m_pi.ue.clubset_id;
                        }

                        // Update ON DB
                        snmdb.NormalManagerDB.getInstance().add(0,
                            new CmdUpdateClubsetEquiped(_session.m_pi.uid, (int)clubset),
                            Room.SQLDBResponse, this);

                    }
                    else
                    {

                        error = 5;

                        _smp.message_pool.getInstance().push(new message("[room::requestChangePlayerItemRoom] [Error] PLAYER[UID=" + _session.m_pi.uid + "] tentou Atualizar Clubset[TYPEID=" + Convert.ToString(pWi._typeid) + ", ID=" + Convert.ToString(pWi.id) + "] equipado, mas ClubSet Not exists on IFF structure. Equipa o ClubSet padrao. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                        // Coloca o ClubSet CV1 no lugar do ClubSet que acabou o tempo
                        pWi = _session.m_pi.findWarehouseItemByTypeid(AIR_KNIGHT_SET);

                        if (pWi != null)
                        {

                            _smp.message_pool.getInstance().push(new message("[room::requestChangePlayerItemRoom][Info][Warning] PLAYER[UID=" + _session.m_pi.uid + "] tentou trocar o ClubSet[ID=" + Convert.ToString(clubset) + "] para comecar o jogo, mas acabou o tempo do ClubSet[ID=" + Convert.ToString(clubset) + "], colocando o ClubSet Padrao\"CV1\" do player. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            // Esse C do WarehouseItem, que pega do DB, não é o ja updado inicial da taqueira é o que fica tabela enchant,
                            // que no original fica no warehouse msm, eu só confundi quando fiz
                            _session.m_pi.ei.csi.setValues(pWi.id, pWi._typeid, pWi.c);

                            cs = sIff.getInstance().findClubSet(pWi._typeid);

                            if (cs != null)
                            {
                                for (var j = 0; j < (_session.m_pi.ei.csi.enchant_c.Length); ++j)
                                {
                                    _session.m_pi.ei.csi.enchant_c[j] = (short)(cs.SlotStats.getSlot[j] + pWi.clubset_workshop.c[j]);
                                }
                            }

                            _session.m_pi.ei.clubset = pWi;
                            clubset = _session.m_pi.ue.clubset_id = pWi.id;

                            // Zera o Error para o cliente equipar a "CV1" que o server equipou
                            error = 0;

                            // Update ON DB
                            snmdb.NormalManagerDB.getInstance().add(0,
                                new CmdUpdateClubsetEquiped(_session.m_pi.uid, (int)clubset),
                                Room.SQLDBResponse, this);

                        }
                        else
                        {

                            _smp.message_pool.getInstance().push(new message("[room::requestChangePlayerItemRoom][Info][Warning] PLAYER[UID=" + _session.m_pi.uid + "] tentou trocar o ClubSet[ID=" + Convert.ToString(clubset) + "] para comecar o jogo, mas acabou o tempo do ClubSet[ID=" + Convert.ToString(clubset) + "], ele nao tem o ClubSet Padrao\"CV1\", adiciona o ClubSet pardrao\"CV1\" para ele. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            BuyItem bi = new BuyItem();
                            stItem item = new stItem();

                            bi.id = -1;
                            bi._typeid = AIR_KNIGHT_SET;
                            bi.qntd = 1;

                            ItemManager.initItemFromBuyItem(_session.m_pi,
                                item, bi, false, 0, 0, 1);

                            if (item._typeid != 0)
                            {
                                clubset = ItemManager.addItem(item,
                                    _session, 2, 0);
                                if (clubset != ItemManager.RetAddItem.T_ERROR)
                                {

                                    // Equipa o ClubSet CV1
                                    pWi = _session.m_pi.findWarehouseItemById(clubset);

                                    if (pWi != null)
                                    {

                                        // Esse C do WarehouseItem, que pega do DB, não é o ja updado inicial da taqueira é o que fica tabela enchant,
                                        // que no original fica no warehouse msm, eu só confundi quando fiz
                                        _session.m_pi.ei.csi.setValues(pWi.id, pWi._typeid, pWi.c);

                                        cs = sIff.getInstance().findClubSet(pWi._typeid);

                                        if (cs != null)
                                        {
                                            for (var j = 0; j < (_session.m_pi.ei.csi.enchant_c.Length); ++j)
                                            {
                                                _session.m_pi.ei.csi.enchant_c[j] = (short)(cs.SlotStats.getSlot[j] + pWi.clubset_workshop.c[j]);
                                            }
                                        }

                                        _session.m_pi.ei.clubset = pWi;
                                        _session.m_pi.ue.clubset_id = pWi.id;

                                        // Zera o Error para o cliente equipar a "CV1" que o server equipou
                                        error = 0;

                                        // Update ON DB
                                        snmdb.NormalManagerDB.getInstance().add(0,
                                            new CmdUpdateClubsetEquiped(_session.m_pi.uid, (int)clubset),
                                            Room.SQLDBResponse, this);

                                        // Update ON GAME
                                        p.init_plain(0x216);

                                        p.WriteUInt32((uint32_t)UtilTime.GetSystemTimeAsUnix());
                                        p.WriteUInt32(1); // Count

                                        p.WriteByte(item.type);
                                        p.WriteUInt32(item._typeid);
                                        p.WriteInt32(item.id);
                                        p.WriteUInt32(item.flag_time);
                                        p.WriteBytes(item.stat.ToArray());
                                        p.WriteUInt32((item.c[3] > 0) ? item.c[3] : item.c[0]);
                                        p.WriteZeroByte(25);

                                        packet_func.session_send(p,
                                            _session, 1);

                                    }
                                    else
                                    {
                                        _smp.message_pool.getInstance().push(new message("[room::requestChangePlayerItemRoom][Info][Warning] PLAYER[UID=" + _session.m_pi.uid + "] nao conseguiu achar o ClubSet\"CV1\"[ID=" + Convert.ToString(item.id) + "] que acabou de adicionar para ele. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                                    }

                                }
                                else
                                {
                                    _smp.message_pool.getInstance().push(new message("[room::requestChangePlayerItemRoom][Info][Warning] PLAYER[UID=" + _session.m_pi.uid + "] nao conseguiu adicionar o ClubSet[TYPEID=" + Convert.ToString(item._typeid) + "] para ele. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                                }

                            }
                            else
                            {
                                _smp.message_pool.getInstance().push(new message("[room::requestChangePlayerItemRoom][Info][Warning] PLAYER[UID=" + _session.m_pi.uid + "] nao conseguiu inicializar o ClubSet[TYPEID=" + Convert.ToString(bi._typeid) + "] para ele. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                            }
                        }
                    }

                }
                else
                { // ClubSet Acabou o tempo

                    error = 6; // Acabou o tempo do item

                    // Coloca o ClubSet CV1 no lugar do ClubSet que acabou o tempo
                    pWi = _session.m_pi.findWarehouseItemByTypeid(AIR_KNIGHT_SET);

                    if (pWi != null)
                    {

                        _smp.message_pool.getInstance().push(new message("[room::requestChangePlayerItemRoom][Info][Warning] PLAYER[UID=" + _session.m_pi.uid + "] tentou trocar o ClubSet[ID=" + Convert.ToString(clubset) + "] para comecar o jogo, mas acabou o tempo do ClubSet[ID=" + Convert.ToString(clubset) + "], colocando o ClubSet Padrao\"CV1\" do player. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                        // Esse C do WarehouseItem, que pega do DB, não é o ja updado inicial da taqueira é o que fica tabela enchant,
                        // que no original fica no warehouse msm, eu só confundi quando fiz
                        _session.m_pi.ei.csi.setValues(pWi.id, pWi._typeid, pWi.c);

                        var cs = sIff.getInstance().findClubSet(pWi._typeid);

                        if (cs != null)
                        {
                            for (var j = 0; j < (_session.m_pi.ei.csi.enchant_c.Length); ++j)
                            {
                                _session.m_pi.ei.csi.enchant_c[j] = (short)(cs.SlotStats.getSlot[j] + pWi.clubset_workshop.c[j]);
                            }
                        }

                        _session.m_pi.ei.clubset = pWi;
                        clubset = _session.m_pi.ue.clubset_id = pWi.id;

                        // Zera o Error para o cliente equipar a "CV1" que o server equipou
                        error = 0;

                        // Update ON DB
                        snmdb.NormalManagerDB.getInstance().add(0,
                            new CmdUpdateClubsetEquiped(_session.m_pi.uid, (int)clubset),
                            Room.SQLDBResponse, this);

                    }
                    else
                    {

                        _smp.message_pool.getInstance().push(new message("[room::requestChangePlayerItemRoom][Info][Warning] PLAYER[UID=" + _session.m_pi.uid + "] tentou trocar o ClubSet[ID=" + Convert.ToString(clubset) + "] para comecar o jogo, mas acabou o tempo do ClubSet[ID=" + Convert.ToString(clubset) + "], ele nao tem o ClubSet Padrao\"CV1\", adiciona o ClubSet pardrao\"CV1\" para ele. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                        BuyItem bi = new BuyItem();
                        stItem item = new stItem();

                        bi.id = -1;
                        bi._typeid = AIR_KNIGHT_SET;
                        bi.qntd = 1;

                        ItemManager.initItemFromBuyItem(_session.m_pi,
                          item, bi, false, 0, 0, 1);

                        if (item._typeid != 0)
                        {

                            if ((clubset = (int)ItemManager.addItem(item,
                                        _session, 2, 0)) != Convert.ToUInt32(ItemManager.RetAddItem.T_ERROR))
                            {

                                // Equipa o ClubSet CV1
                                pWi = _session.m_pi.findWarehouseItemById(clubset);

                                if (pWi != null)
                                {

                                    // Esse C do WarehouseItem, que pega do DB, não é o ja updado inicial da taqueira é o que fica tabela enchant,
                                    // que no original fica no warehouse msm, eu só confundi quando fiz
                                    var cs = sIff.getInstance().findClubSet(pWi._typeid);

                                    if (cs != null)
                                    {
                                        for (var j = 0; j < (_session.m_pi.ei.csi.enchant_c.Length); ++j)
                                        {
                                            _session.m_pi.ei.csi.enchant_c[j] = (short)(cs.SlotStats.getSlot[j] + pWi.clubset_workshop.c[j]);
                                        }
                                    }
                                    _session.m_pi.ei.clubset = pWi;
                                    _session.m_pi.ue.clubset_id = pWi.id;

                                    // Zera o Error para o cliente equipar a "CV1" que o server equipou
                                    error = 0;

                                    // Update ON DB
                                    snmdb.NormalManagerDB.getInstance().add(0,
                                        new CmdUpdateClubsetEquiped(_session.m_pi.uid, (int)clubset),
                                        Room.SQLDBResponse, this);

                                    // Update ON GAME
                                    p.init_plain(0x216);

                                    p.WriteUInt32((uint32_t)UtilTime.GetSystemTimeAsUnix());
                                    p.WriteUInt32(1); // Count

                                    p.WriteByte(item.type);
                                    p.WriteUInt32(item._typeid);
                                    p.WriteInt32(item.id);
                                    p.WriteUInt32(item.flag_time);
                                    p.WriteBytes(item.stat.ToArray());
                                    p.WriteUInt32((item.c[3] > 0) ? item.c[3] : item.c[0]);
                                    p.WriteZeroByte(25);

                                    packet_func.session_send(p,
                                        _session, 1);

                                }
                                else
                                {
                                    _smp.message_pool.getInstance().push(new message("[room::requestChangePlayerItemRoom][Info][Warning] PLAYER[UID=" + _session.m_pi.uid + "] nao conseguiu achar o ClubSet\"CV1\"[ID=" + Convert.ToString(item.id) + "] que acabou de adicionar para ele. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                                }

                            }
                            else
                            {
                                _smp.message_pool.getInstance().push(new message("[room::requestChangePlayerItemRoom][Info][Warning] PLAYER[UID=" + _session.m_pi.uid + "] nao conseguiu adicionar o ClubSet[TYPEID=" + Convert.ToString(item._typeid) + "] para ele. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                            }

                        }
                        else
                        {
                            _smp.message_pool.getInstance().push(new message("[room::requestChangePlayerItemRoom][Info][Warning] PLAYER[UID=" + _session.m_pi.uid + "] nao conseguiu inicializar o ClubSet[TYPEID=" + Convert.ToString(bi._typeid) + "] para ele. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                        }
                    }
                }

            }
            else
            {

                error = (clubset == 0) ? 1 : (pWi == null ? 2 : 3);

                pWi = _session.m_pi.findWarehouseItemByTypeid(AIR_KNIGHT_SET);

                if (pWi != null)
                {

                    _smp.message_pool.getInstance().push(new message("[room::requestChangePlayerItemRoom][Info][Warning] PLAYER[UID=" + _session.m_pi.uid + "] tentou trocar o ClubSet[ID=" + Convert.ToString(clubset) + "] para comecar o jogo, mas deu Error[VALUE=" + Convert.ToString(error) + "], colocando o ClubSet Padrao\"CV1\" do player. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                    // Esse C do WarehouseItem, que pega do DB, não é o ja updado inicial da taqueira é o que fica tabela enchant,
                    // que no original fica no warehouse msm, eu só confundi quando fiz
                    var cs = sIff.getInstance().findClubSet(pWi._typeid);

                    if (cs != null)
                    {
                        for (var j = 0; j < (_session.m_pi.ei.csi.enchant_c.Length); ++j)
                        {
                            _session.m_pi.ei.csi.enchant_c[j] = (short)(cs.SlotStats.getSlot[j] + pWi.clubset_workshop.c[j]);
                        }
                    }

                    _session.m_pi.ei.clubset = pWi;
                    clubset = _session.m_pi.ue.clubset_id = pWi.id;

                    // Zera o Error para o cliente equipar a "CV1" que o server equipou
                    error = 0;

                    // Update ON DB
                    snmdb.NormalManagerDB.getInstance().add(0,
                        new CmdUpdateClubsetEquiped(_session.m_pi.uid, (int)clubset),
                        Room.SQLDBResponse, this);

                }
                else
                {

                    _smp.message_pool.getInstance().push(new message("[room::requestChangePlayerItemRoom][Info][Warning] PLAYER[UID=" + _session.m_pi.uid + "] tentou trocar o ClubSet[ID=" + Convert.ToString(clubset) + "] para comecar o jogo, mas deu Error[VALUE=" + Convert.ToString(error) + "], ele nao tem o ClubSet Padrao\"CV1\", adiciona o ClubSet pardrao\"CV1\" para ele. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                    BuyItem bi = new BuyItem();
                    stItem item = new stItem();

                    bi.id = -1;
                    bi._typeid = AIR_KNIGHT_SET;
                    bi.qntd = 1;

                    ItemManager.initItemFromBuyItem(_session.m_pi,
                       item, bi, false, 0, 0, 1);

                    if (item._typeid != 0)
                    {

                        if ((clubset = (int)ItemManager.addItem(item,
                                         _session, 2, 0)) != Convert.ToUInt32(ItemManager.RetAddItem.T_ERROR))
                        {

                            // Equipa o ClubSet CV1
                            pWi = _session.m_pi.findWarehouseItemById(clubset);

                            if (pWi != null)
                            {

                                // Esse C do WarehouseItem, que pega do DB, não é o ja updado inicial da taqueira é o que fica tabela enchant,
                                // que no original fica no warehouse msm, eu só confundi quando fiz
                                _session.m_pi.ei.csi.setValues(pWi.id, pWi._typeid, pWi.c);

                                var cs = sIff.getInstance().findClubSet(pWi._typeid);

                                if (cs != null)
                                {
                                    for (var j = 0; j < (_session.m_pi.ei.csi.enchant_c.Length); ++j)
                                    {
                                        _session.m_pi.ei.csi.enchant_c[j] = (short)(cs.SlotStats.getSlot[j] + pWi.clubset_workshop.c[j]);
                                    }
                                }

                                _session.m_pi.ei.clubset = pWi;
                                _session.m_pi.ue.clubset_id = pWi.id;

                                // Zera o Error para o cliente equipar a "CV1" que o server equipou
                                error = 0;

                                // Update ON DB
                                snmdb.NormalManagerDB.getInstance().add(0,
                                    new CmdUpdateClubsetEquiped(_session.m_pi.uid, (int)clubset),
                                    Room.SQLDBResponse, this);

                                // Update ON GAME
                                p.init_plain(0x216);

                                p.WriteUInt32((uint32_t)UtilTime.GetSystemTimeAsUnix());
                                p.WriteUInt32(1); // Count

                                p.WriteByte(item.type);
                                p.WriteUInt32(item._typeid);
                                p.WriteInt32(item.id);
                                p.WriteUInt32(item.flag_time);
                                p.WriteBytes(item.stat.ToArray());
                                p.WriteUInt32((item.c[3] > 0) ? item.c[3] : item.c[0]);
                                p.WriteZeroByte(25);

                                packet_func.session_send(p,
                                    _session, 1);

                            }
                            else
                            {
                                _smp.message_pool.getInstance().push(new message("[room::requestChangePlayerItemRoom][Info][Warning] PLAYER[UID=" + _session.m_pi.uid + "] nao conseguiu achar o ClubSet\"CV1\"[ID=" + Convert.ToString(item.id) + "] que acabou de adicionar para ele. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                            }

                        }
                        else
                        {
                            _smp.message_pool.getInstance().push(new message("[room::requestChangePlayerItemRoom][Info][Warning] PLAYER[UID=" + _session.m_pi.uid + "] nao conseguiu adicionar o ClubSet[TYPEID=" + Convert.ToString(item._typeid) + "] para ele. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                        }

                    }
                    else
                    {
                        _smp.message_pool.getInstance().push(new message("[room::requestChangePlayerItemRoom][Info][Warning] PLAYER[UID=" + _session.m_pi.uid + "] nao conseguiu inicializar o ClubSet[TYPEID=" + Convert.ToString(bi._typeid) + "] para ele. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                    }
                }
            }
            return error;
        }

        private int HandleChangeCharacter(Player _session, int character)
        {
            var p = new PangyaBinaryWriter();
            int error = 0/*SUCCESS*/;

            CharacterInfo pCe = null;

            if (character != 0
                && (pCe = _session.m_pi.findCharacterById(character)) != null
                && sIff.getInstance().getItemGroupIdentify(pCe._typeid) == IFF_GROUP.CHARACTER)
            {

                _session.m_pi.ei.char_info = pCe;
                _session.m_pi.ue.character_id = character;

                // Update ON DB
                snmdb.NormalManagerDB.getInstance().add(0,
                    new CmdUpdateCharacterEquiped(_session.m_pi.uid, (int)character),
                    Room.SQLDBResponse, this);

                // Update Player Info Channel and Room
                updatePlayerInfo(_session);

                PlayerRoomInfoEx pri = getPlayerInfo(_session);

                if (getInfo().getTipo() != RoomInfo.ROOM_INFO_TYPE.PRACTICE && getInfo().getTipo() != RoomInfo.ROOM_INFO_TYPE.GRAND_ZODIAC_PRACTICE)
                {
                    if (packet_func.pacote048(ref p,
                    _session,
                    new List<PlayerRoomInfoEx>() { pri == null ? new PlayerRoomInfoEx() : pri },
0x103))
                    {
                        packet_func.room_broadcast(this, p, 0);
                    }
                }

                if (getInfo().getTipo() == RoomInfo.ROOM_INFO_TYPE.LOUNGE)
                {

                    var it = (_session.m_pi.ei.char_info == null) ? _session.m_pi.mp_scl.LastOrDefault() : _session.m_pi.mp_scl.find(_session.m_pi.ei.char_info.id);

                    if (it.Value == null)
                    {

                        _smp.message_pool.getInstance().push(new message("[room::requestChangePlayerItemRoom] [Error] PLAYER[UID=" + _session.m_pi.uid + "] nao tem os estados do character na lounge. Criando um novo para ele. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                        // Add New State Character Lounge
                        var pair = _session.m_pi.mp_scl.insert(Tuple.Create(_session.m_pi.ei.char_info.id, new StateCharacterLounge()));

                        it = pair;
                    }
                    p = packet_func.pacote196(_session, it.Value);
                    packet_func.room_broadcast(this,
                        p, 0);
                }

            }
            else
            {

                error = (character == 0) ? 1 : (pCe == null ? 2 : 3);

                if (_session.m_pi.mp_ce.Count() > 0)
                {

                    _smp.message_pool.getInstance().push(new message("[room::requestChangePlayerItemRoom][Info][Warning] PLAYER[UID=" + _session.m_pi.uid + "] tentou trocar o Character[ID=" + Convert.ToString(character) + "] para comecar o jogo, mas deu Error[VALUE=" + Convert.ToString(error) + "], colocando o primeiro character do player. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                    _session.m_pi.ei.char_info = _session.m_pi.mp_ce.FirstOrDefault().Value;
                    character = _session.m_pi.ue.character_id = _session.m_pi.ei.char_info.id;

                    // Zera o Error para o cliente equipar o Primeiro Character do map de character do player, que o server equipou
                    error = 0;

                    // Update ON DB
                    snmdb.NormalManagerDB.getInstance().add(0,
                        new CmdUpdateCharacterEquiped(_session.m_pi.uid, (int)character),
                        Room.SQLDBResponse, this);

                    // Update Player Info Channel and Room
                    updatePlayerInfo(_session);

                    PlayerRoomInfoEx pri = getPlayerInfo(_session);

                    if (getInfo().getTipo() != RoomInfo.ROOM_INFO_TYPE.PRACTICE && getInfo().getTipo() != RoomInfo.ROOM_INFO_TYPE.GRAND_ZODIAC_PRACTICE)
                    {
                        if (packet_func.pacote048(ref p,
                              _session,
                              new List<PlayerRoomInfoEx>() { (pri == null) ? new PlayerRoomInfoEx() : pri },
0x103))
                        {
                            packet_func.room_broadcast(this,
                                p, 0);
                        }
                    }

                    if (getInfo().getTipo() == RoomInfo.ROOM_INFO_TYPE.LOUNGE)
                    {

                        var it = (_session.m_pi.ei.char_info == null) ? _session.m_pi.mp_scl.LastOrDefault() : _session.m_pi.mp_scl.FirstOrDefault(c => c.Key == _session.m_pi.ei.char_info.id);

                        if (it.Value == null)
                        {

                            _smp.message_pool.getInstance().push(new message("[room::requestChangePlayerItemRoom] [Error] PLAYER[UID=" + _session.m_pi.uid + "] nao tem os estados do character na lounge. Criando um novo para ele. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            // Add New State Character Lounge
                            it = _session.m_pi.mp_scl.insert(Tuple.Create(_session.m_pi.ei.char_info.id, new StateCharacterLounge()));
                        }

                        p = packet_func.pacote196(_session, it.Value);

                        packet_func.room_broadcast(this,
                            p, 0);
                    }

                }
                else
                {

                    _smp.message_pool.getInstance().push(new message("[room::requestChangePlayerItemRoom][Info][Warning] PLAYER[UID=" + _session.m_pi.uid + "] tentou trocar o Character[ID=" + Convert.ToString(character) + "] para comecar o jogo, mas deu Error[VALUE=" + Convert.ToString(error) + "], ele nao tem nenhum character, adiciona o Nuri para ele. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                    BuyItem bi = new BuyItem();
                    stItem item = new stItem();

                    bi.id = -1;
                    bi._typeid = (uint)(sIff.getInstance().CHARACTER << 26); // Nuri
                    bi.qntd = 1;

                    ItemManager.initItemFromBuyItem(_session.m_pi,
                       item, bi, false, 0, 0, 1);

                    if (item._typeid != 0)
                    {
                        character = (int)ItemManager.addItem(item,
                            _session, 2, 0);
                        // Add Item já atualiza o Character equipado
                        if ((int)character != (int)ItemManager.RetAddItem.T_ERROR)
                        {

                            // Zera o Error para o cliente equipar o Nuri que o server equipou
                            error = 0;

                            // Update ON GAME
                            p.init_plain(0x216);

                            p.WriteUInt32((uint32_t)UtilTime.GetSystemTimeAsUnix());
                            p.WriteUInt32(1); // Count

                            p.WriteByte(item.type);
                            p.WriteUInt32(item._typeid);
                            p.WriteInt32(item.id);
                            p.WriteUInt32(item.flag_time);
                            p.WriteBytes(item.stat.ToArray());
                            p.WriteUInt32((item.c[3] > 0) ? item.c[3] : item.c[0]);
                            p.WriteZeroByte(25);

                            packet_func.session_send(p,
                                _session, 1);

                            // Update Player Info Channel and Room
                            updatePlayerInfo(_session);

                            PlayerRoomInfoEx pri = getPlayerInfo(_session);

                            if (getInfo().getTipo() != RoomInfo.ROOM_INFO_TYPE.PRACTICE && getInfo().getTipo() != RoomInfo.ROOM_INFO_TYPE.GRAND_ZODIAC_PRACTICE)
                            {
                                if (packet_func.pacote048(ref p,
                                    _session,
                                    new List<PlayerRoomInfoEx>() { pri == null ? new PlayerRoomInfoEx() : pri },
0x103))
                                {
                                    packet_func.room_broadcast(this,
                                        p, 0);
                                }
                            }

                            if (getInfo().getTipo() == RoomInfo.ROOM_INFO_TYPE.LOUNGE)
                            {

                                var it = (_session.m_pi.ei.char_info == null) ? _session.m_pi.mp_scl.LastOrDefault() : _session.m_pi.mp_scl.find(_session.m_pi.ei.char_info.id);

                                if (it.Value == null)
                                {

                                    _smp.message_pool.getInstance().push(new message("[room::requestChangePlayerItemRoom] [Error] PLAYER[UID=" + _session.m_pi.uid + "] nao tem os estados do character na lounge. Criando um novo para ele. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                                    // Add New State Character Lounge
                                    var pair = _session.m_pi.mp_scl.insert(Tuple.Create(_session.m_pi.ei.char_info.id, new StateCharacterLounge()));

                                    it = pair;
                                }


                                p = packet_func.pacote196(_session, it.Value);

                                packet_func.room_broadcast(this,
                                    p, 0);
                            }

                        }
                        else
                        {
                            _smp.message_pool.getInstance().push(new message("[room::requestChangePlayerItemRoom][Info][Warning] PLAYER[UID=" + _session.m_pi.uid + "] nao conseguiu adicionar o Character[TYPEID=" + Convert.ToString(item._typeid) + "] para ele. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                        }

                    }
                    else
                    {
                        _smp.message_pool.getInstance().push(new message("[room::requestChangePlayerItemRoom][Info][Warning] PLAYER[UID=" + _session.m_pi.uid + "] nao conseguiu inicializar o Character[TYPEID=" + Convert.ToString(bi._typeid) + "] para ele. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                    }
                }
            }
            return error;
        }

        private int HandleChangeMascot(Player _session, int mascot)
        {
            var p = new PangyaBinaryWriter();
            int error = 0/*SUCCESS*/;
            MascotInfoEx pMi = null;

            if (mascot != 0)
            {

                if ((pMi = _session.m_pi.findMascotById(mascot)) != null && sIff.getInstance().getItemGroupIdentify(pMi._typeid) == IFF_GROUP.MASCOT)
                {

                    var m_it = _session.m_pi.findUpdateItemByTypeidAndType((uint)_session.m_pi.ue.mascot_id, UpdateItem.UI_TYPE.MASCOT);

                    if (m_it.Keys.Count > 0)
                    {

                        // Desequipa o Mascot que acabou o tempo dele
                        _session.m_pi.ei.mascot_info = null;
                        _session.m_pi.ue.mascot_id = 0;

                        mascot = 0;

                    }
                    else
                    {

                        // Mascot is good, update on server, DB and game
                        _session.m_pi.ei.mascot_info = pMi;
                        _session.m_pi.ue.mascot_id = mascot;

                        // Verifica se o mascot pode equipar
                        if (_session.checkMascotEquiped(_session.m_pi.ue))
                            mascot = _session.m_pi.ue.mascot_id;

                    }

                    // Update ON DB
                    snmdb.NormalManagerDB.getInstance().add(0, new CmdUpdateMascotEquiped(_session.m_pi.uid, mascot), SQLDBResponse, this);

                }
                else
                {

                    error = (mascot == 0) ? 1/*client give invalid item id*/ : (pMi == null ? 2/*Item Not Found*/ : 3/*Erro item typeid invalid*/);

                    if (error > 1)
                    {
                        _smp.message_pool.getInstance().push(new message("[room::requestChangePlayerItemRoom][Info][Warning] PLAYER[UID=" + (_session.m_pi.uid)
                                + "] tentou trocar o Mascot[ID=" + (mascot) + "] para comecar o jogo, mas deu Error[VALUE="
                                + (error) + "], desequipando o Mascot. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                    }

                    _session.m_pi.ei.mascot_info = null;
                    _session.m_pi.ue.mascot_id = 0;

                    mascot = 0;

                    // Att No DB
                    snmdb.NormalManagerDB.getInstance().add(0, new CmdUpdateMascotEquiped(_session.m_pi.uid, mascot), SQLDBResponse, this);
                }

            }
            else if (_session.m_pi.ue.mascot_id > 0 && _session.m_pi.ei.mascot_info != null)
            {   // Desequipa Mascot

                _session.m_pi.ei.mascot_info = null;
                _session.m_pi.ue.mascot_id = 0;

                mascot = 0;

                // Att No DB
                snmdb.NormalManagerDB.getInstance().add(0, new CmdUpdateMascotEquiped(_session.m_pi.uid, mascot), SQLDBResponse, this);

            } // else Não tem nenhum mascot equipado, para desequipar, então o cliente só quis atualizar o estado


            return error;
        }

        private int HandleChangeItemSpecial(Player _session, ChangePlayerItemRoom.stItemEffectLounge effect_lounge)
        {
            var p = new PangyaBinaryWriter();

            int error = 0/*SUCCESS*/;
            // ignora o item_id por que ele envia 0

            // Valor 1 Cabeca
            // Valor 2 Velocidade
            // Valor 3 Twilight

            if (!sIff.getInstance().isLoad())
            {
                sIff.getInstance().initilation();
            }

            if (_session.m_pi.ei.char_info == null)
            {
                throw new exception("[room::requestChangePlayerItemRoom] [Error] PLAYER[UID=" + _session.m_pi.uid + "] nao tem nenhum character equipado. Hacker ou Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                    1000, 0x57007));
            }

            if (effect_lounge.effect != ChangePlayerItemRoom.stItemEffectLounge.TYPE_EFFECT.TE_TWILIGHT)
            {

                var it = (_session.m_pi.ei.char_info == null) ? _session.m_pi.mp_scl.Last() : _session.m_pi.mp_scl.FirstOrDefault(c => c.Key == _session.m_pi.ei.char_info.id);

                if (it.Key == _session.m_pi.mp_scl.Last().Key)
                {

                    _smp.message_pool.getInstance().push(new message("[room::requestChangePlayerItemRoom] [Error] PLAYER[UID=" + _session.m_pi.uid + "] nao tem os estados do character na lounge. Criando um novo para ele. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                    // Add New State Character Lounge
                    _session.m_pi.mp_scl.Add(_session.m_pi.ei.char_info.id, new StateCharacterLounge());
                    var pair = _session.m_pi.mp_scl.FirstOrDefault(c => c.Key == _session.m_pi.ei.char_info.id);
                    it = pair;
                }

                switch (effect_lounge.effect)
                {
                    case ChangePlayerItemRoom.stItemEffectLounge.TYPE_EFFECT.TE_BIG_HEAD: // Jester (Big head)
                        {

                            var ccj = cadie_cauldron_Jester_item_typeid.FirstOrDefault(el =>
                            {
                                return sIff.getInstance().getItemCharIdentify(el) == (_session.m_pi.ei.char_info._typeid & 0x000000FF);
                            });

                            if (ccj <= 0)
                            {
                                throw new exception("[room::requestChangePlayerItemRoom] [Error] PLAYER[UID=" + _session.m_pi.uid + "] o Character[TYPEID=" + Convert.ToString(_session.m_pi.ei.char_info._typeid) + "] equipado nao tem o item Jester no server. Hacker ou Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 1001, 0x57008));
                            }

                            if (!_session.m_pi.ei.char_info.isPartEquiped(ccj))
                            {
                                throw new exception("[room::requestChangePlayerItemRoom] [Error] PLAYER[UID=" + _session.m_pi.uid + "] o Character[TYPEID=" + Convert.ToString(_session.m_pi.ei.char_info._typeid) + "] nao esta com o item[TYPEID=" + Convert.ToString(ccj) + "] Jester equipado. Hacker ou Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 1002, 0x57009));
                            }

                            it.Value.scale_head = (it.Value.scale_head > 1.0f) ? 1.0f : 2.0f;

                            break;
                        }
                    case ChangePlayerItemRoom.stItemEffectLounge.TYPE_EFFECT.TE_FAST_WALK: // Hermes (Velocidade x2)
                        {
                            var cch = cadie_cauldron_Hermes_item_typeid.FirstOrDefault(el =>
                            {
                                return sIff.getInstance().getItemCharIdentify(el) == (_session.m_pi.ei.char_info._typeid & 0x000000FF);
                            });

                            if (cch <= 0)
                            {
                                throw new exception("[room::requestChangePlayerItemRoom] [Error] PLAYER[UID=" + _session.m_pi.uid + "] o Character[TYPEID=" + Convert.ToString(_session.m_pi.ei.char_info._typeid) + "] equipado nao tem o item Hermes no server. Hacker ou Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                    1001, 0x57008));
                            }

                            if (!_session.m_pi.ei.char_info.isPartEquiped(cch))
                            {
                                throw new exception("[room::requestChangePlayerItemRoom] [Error] PLAYER[UID=" + _session.m_pi.uid + "] o Character[TYPEID=" + Convert.ToString(_session.m_pi.ei.char_info._typeid) + "] nao esta com o item[TYPEID=" + Convert.ToString(cch) + "] Hermes equipado. Hacker ou Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 1002, 0x57009));
                            }

                            it.Value.walk_speed = (it.Value.walk_speed > 1.0f) ? 1.0f : 2.0f;

                            break;
                        }
                } // End Switch

            }
            else
            {
                // else == 3 // Twilight (Fogos de artifícios em cima da cabeça do Player)
                // Valor 1 pass para fazer o fogos                                              
                var cct = cadie_cauldron_Twilight_item_typeid.FirstOrDefault(el =>
                {
                    return sIff.getInstance().getItemCharIdentify(el) == (_session.m_pi.ei.char_info._typeid & 0x000000FF);
                });

                if (cct <= 0)
                {
                    throw new exception("[room::requestChangePlayerItemRoom] [Error] PLAYER[UID=" + _session.m_pi.uid + "] o Character[TYPEID=" + Convert.ToString(_session.m_pi.ei.char_info._typeid) + "] equipado nao tem o item Twilight no server. Hacker ou Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        1001, 0x57008));
                }

                if (!_session.m_pi.ei.char_info.isPartEquiped(cct))
                {
                    throw new exception("[room::requestChangePlayerItemRoom] [Error] PLAYER[UID=" + _session.m_pi.uid + "] o Character[TYPEID=" + Convert.ToString(_session.m_pi.ei.char_info._typeid) + "] nao esta com o item[TYPEID=" + Convert.ToString(cct) + "] Twilight equipado. Hacker ou Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                        1002, 0x57009));
                }
            }

            return error;
        }

        private int HandleChangeAll(Player _session, ChangePlayerItemRoom _cpir)
        {
            int error = 0/*SUCCESS*/;
            var p = new PangyaBinaryWriter();
            // Aqui se não tiver os itens, algum hacker, gera Log, e coloca item padrão ou nenhum
            CharacterInfo pCe = null;
            CaddieInfoEx pCi = null;
            WarehouseItemEx pWi = null;
            error = 0;
            var character = _cpir.character;
            var caddie = _cpir.caddie;
            var clubset = _cpir.clubset;
            var mascot = _cpir.mascot;
            var ball = _cpir.ball;
            // Character
            if (character != 0 && (pCe = _session.m_pi.findCharacterById(character)) != null && sIff.getInstance().getItemGroupIdentify(pCe._typeid) == IFF_GROUP.CHARACTER)
            {

                _session.m_pi.ei.char_info = pCe;
                _session.m_pi.ue.character_id = character;

                // Update ON DB
                snmdb.NormalManagerDB.getInstance().add(0, new CmdUpdateCharacterEquiped(_session.m_pi.uid, (int)character), SQLDBResponse, this);

            }
            else
            {

                error = (character == 0) ? 1/*client give invalid item id*/ : (pCe == null ? 2/*Item Not Found*/ : 3/*Erro item typeid invalid*/);

                if (_session.m_pi.mp_ce.Count() > 0)
                {

                    _smp.message_pool.getInstance().push(new message("[room::requestChangePlayerItemRoom][Info][Warning] PLAYER[UID=" + (_session.m_pi.uid)
                            + "] tentou trocar o Character[ID=" + (character) + "] para comecar o jogo, mas deu Error[VALUE="
                            + (error) + "], colocando o primeiro character do player. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                    _session.m_pi.ei.char_info = _session.m_pi.mp_ce.First().Value;
                    character = _session.m_pi.ue.character_id = _session.m_pi.ei.char_info.id;

                    // Update ON DB
                    snmdb.NormalManagerDB.getInstance().add(0, new CmdUpdateCharacterEquiped(_session.m_pi.uid, (int)character), SQLDBResponse, this);

                }
                else
                {

                    _smp.message_pool.getInstance().push(new message("[room::requestChangePlayerItemRoom][Info][Warning] PLAYER[UID=" + (_session.m_pi.uid)
                            + "] tentou trocar o Character[ID=" + (character) + "] para comecar o jogo, mas deu Error[VALUE="
                            + (error) + "], ele nao tem nenhum character, adiciona o Nuri para ele. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                    BuyItem bi = new BuyItem();
                    stItem item = new stItem();

                    bi.id = -1;
                    bi._typeid = (uint)(sIff.getInstance().CHARACTER << 26);    // Nuri
                    bi.qntd = 1;

                    ItemManager.initItemFromBuyItem(_session.m_pi, item, bi, false, 0, 0, 1/*Não verifica o Level*/);

                    if (item._typeid != 0)
                    {

                        // Add Item já atualiza o Character equipado
                        if ((character = (int)ItemManager.addItem(item, _session, 2/*Padrão Item*/, 0)) != Convert.ToUInt32(ItemManager.RetAddItem.T_ERROR))
                        {

                            // Update ON GAME
                            p.init_plain(0x216);

                            p.WriteUInt32((uint)UtilTime.GetSystemTimeAsUnix());
                            p.WriteUInt32(1);   // Count

                            p.WriteByte(item.type);
                            p.WriteUInt32(item._typeid);
                            p.WriteInt32(item.id);
                            p.WriteUInt32(item.flag_time);
                            p.WriteBytes(item.stat.ToArray());
                            p.WriteInt32((item.STDA_C_ITEM_TIME > 0) ? item.STDA_C_ITEM_TIME : item.STDA_C_ITEM_QNTD);
                            p.WriteZeroByte(25);

                            packet_func.session_send(p, _session, 1);

                        }
                        else
                            _smp.message_pool.getInstance().push(new message("[room::requestChangePlayerItemRoom][Info][Warning] PLAYER[UID=" + (_session.m_pi.uid)
                                    + "] nao conseguiu adicionar o Character[TYPEID="
                                    + (item._typeid) + "] para ele. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                    }
                    else
                        _smp.message_pool.getInstance().push(new message("[room::requestChangePlayerItemRoom][Info][Warning] PLAYER[UID=" + (_session.m_pi.uid)
                                + "] nao conseguiu inicializar o Character[TYPEID="
                                + (bi._typeid) + "] para ele. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
            }

            // Caddie
            if (caddie != 0 && (pCi = _session.m_pi.findCaddieById(caddie)) != null
                    && sIff.getInstance().getItemGroupIdentify(pCi._typeid) == IFF_GROUP.CADDIE)
            {

                // Check if item is in map of update item
                var v_it = _session.m_pi.findUpdateItemByTypeidAndId(pCi._typeid, pCi.id);

                if (v_it.Any())
                {

                    foreach (var el in v_it)
                    {

                        if (el.Value.type == UpdateItem.UI_TYPE.CADDIE)
                        {

                            // Desequipa o caddie
                            _session.m_pi.ei.cad_info = null;
                            _session.m_pi.ue.caddie_id = 0;

                            caddie = 0;

                        }
                        else if (el.Value.type == UpdateItem.UI_TYPE.CADDIE_PARTS)
                        {

                            // Limpa o caddie Parts
                            pCi.parts_typeid = 0;
                            pCi.parts_end_date_unix = 0;
                            pCi.end_parts_date = new SYSTEMTIME();

                            _session.m_pi.ei.cad_info = pCi;
                            _session.m_pi.ue.caddie_id = caddie;
                        }

                        // Tira esse Update Item do map
                        _session.m_pi.mp_ui.Remove(el.Key);
                    }

                }
                else
                {

                    // Caddie is Good, Update caddie equiped ON SERVER AND DB
                    _session.m_pi.ei.cad_info = pCi;
                    _session.m_pi.ue.caddie_id = caddie;

                    // Verifica se o Caddie pode equipar
                    if (_session.checkCaddieEquiped(_session.m_pi.ue))
                        caddie = _session.m_pi.ue.caddie_id;

                }

                // Update ON DB
                snmdb.NormalManagerDB.getInstance().add(0, new CmdUpdateCaddieEquiped(_session.m_pi.uid, (int)caddie), SQLDBResponse, this);

            }
            else if (_session.m_pi.ue.caddie_id > 0 && _session.m_pi.ei.cad_info != null)
            {   // Desequipa Caddie

                error = (caddie == 0) ? 1/*client give invalid item id*/ : (pCi == null ? 2/*Item Not Found*/ : 3/*Erro item typeid invalid*/);

                if (error > 1)
                {
                    _smp.message_pool.getInstance().push(new message("[room::requestChangePlayerItemRoom][Info][Warning] PLAYER[UID=" + (_session.m_pi.uid)
                            + "] tentou trocar o Caddie[ID=" + (caddie)
                            + "] para comecar o jogo, mas deu Error[VALUE=" + (error) + "], desequipando o caddie. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                }

                // Check if item is in map of update item
                var v_it = _session.m_pi.findUpdateItemByTypeidAndId(_session.m_pi.ei.cad_info._typeid, _session.m_pi.ei.cad_info.id);

                if (v_it.Any())
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

                caddie = 0;

                // Att No DB
                snmdb.NormalManagerDB.getInstance().add(0, new CmdUpdateCaddieEquiped(_session.m_pi.uid, (int)caddie), SQLDBResponse, this);
            }

            // ClubSet
            if (clubset != 0 && (pWi = _session.m_pi.findWarehouseItemById(clubset)) != null
                    && sIff.getInstance().getItemGroupIdentify(pWi._typeid) == IFF_GROUP.CLUBSET)
            {

                var c_it = _session.m_pi.findUpdateItemByTypeidAndType((uint)clubset, UpdateItem.UI_TYPE.WAREHOUSE);

                if (c_it.Count <= 0)
                {

                    _session.m_pi.ei.clubset = pWi;

                    // Esse C do WarehouseItem, que pega do DB, não é o ja updado inicial da taqueira é o que fica tabela enchant, 
                    // que no original fica no warehouse msm, eu só confundi quando fiz
                    _session.m_pi.ei.csi.setValues(pWi.id, pWi._typeid, pWi.c);

                    var cs = sIff.getInstance().findClubSet(pWi._typeid);

                    if (cs != null)
                    {

                        for (var j = 0; j < 5; ++j)
                            _session.m_pi.ei.csi.enchant_c[j] = (short)(cs.SlotStats.getSlot[j] + pWi.clubset_workshop.c[j]);

                        _session.m_pi.ue.clubset_id = clubset;

                        // Verifica se o ClubSet pode equipar
                        if (_session.checkClubSetEquiped(_session.m_pi.ue))
                            clubset = _session.m_pi.ue.clubset_id;

                        // Update ON DB
                        snmdb.NormalManagerDB.getInstance().add(0, new CmdUpdateClubsetEquiped(_session.m_pi.uid, (int)clubset), SQLDBResponse, this);

                    }
                    else
                    {

                        error = 5/*Item Not Found ON IFF_STRUCT SERVER*/;

                        _smp.message_pool.getInstance().push(new message("[room::requestChangePlayerItemRoom] [Error] PLAYER[UID=" + (_session.m_pi.uid) + "] tentou Atualizar Clubset[TYPEID="
                                + (pWi._typeid) + ", ID=" + (pWi.id)
                                + "] equipado, mas ClubSet Not exists on IFF structure. Equipa o ClubSet padrao. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                        // Coloca o ClubSet CV1 no lugar do ClubSet que acabou o tempo
                        pWi = _session.m_pi.findWarehouseItemByTypeid(AIR_KNIGHT_SET);

                        if (pWi != null)
                        {

                            _smp.message_pool.getInstance().push(new message("[room::requestChangePlayerItemRoom][Info][Warning] PLAYER[UID=" + (_session.m_pi.uid)
                                    + "] tentou trocar o ClubSet[ID=" + (clubset) + "] para comecar o jogo, mas acabou o tempo do ClubSet[ID="
                                    + (clubset) + "], colocando o ClubSet Padrao\"CV1\" do player. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            // Esse C do WarehouseItem, que pega do DB, não é o ja updado inicial da taqueira é o que fica tabela enchant, 
                            // que no original fica no warehouse msm, eu só confundi quando fiz
                            _session.m_pi.ei.csi.setValues(pWi.id, pWi._typeid, pWi.c);

                            cs = sIff.getInstance().findClubSet(pWi._typeid);

                            if (cs != null)
                                for (var j = 0; j < 5; ++j)
                                    _session.m_pi.ei.csi.enchant_c[j] = (short)(cs.SlotStats.getSlot[j] + pWi.clubset_workshop.c[j]);

                            _session.m_pi.ei.clubset = pWi;
                            clubset = _session.m_pi.ue.clubset_id = pWi.id;

                            // Update ON DB
                            snmdb.NormalManagerDB.getInstance().add(0, new CmdUpdateClubsetEquiped(_session.m_pi.uid, (int)clubset), SQLDBResponse, this);

                        }
                        else
                        {

                            _smp.message_pool.getInstance().push(new message("[channel::requestChangePlayerItemRoom][Info][Warning] PLAYER[UID=" + (_session.m_pi.uid)
                                    + "] tentou trocar o ClubSet[ID=" + (clubset) + "] para comecar o jogo, mas acabou o tempo do ClubSet[ID="
                                    + (clubset) + "], ele nao tem o ClubSet Padrao\"CV1\", adiciona o ClubSet pardrao\"CV1\" para ele. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            BuyItem bi = new BuyItem();
                            stItem item = new stItem(); ;

                            bi.id = -1;
                            bi._typeid = AIR_KNIGHT_SET;
                            bi.qntd = 1;

                            ItemManager.initItemFromBuyItem(_session.m_pi, item, bi, false, 0, 0, 1/*Não verifica o Level*/);

                            if (item._typeid != 0)
                            {

                                if ((clubset = (int)ItemManager.addItem(item, _session, 2/*Padrão Item*/, 0)) != Convert.ToUInt32(ItemManager.RetAddItem.T_ERROR))
                                {

                                    // Equipa o ClubSet CV1
                                    pWi = _session.m_pi.findWarehouseItemById(clubset);

                                    if (pWi != null)
                                    {

                                        // Esse C do WarehouseItem, que pega do DB, não é o ja updado inicial da taqueira é o que fica tabela enchant, 
                                        // que no original fica no warehouse msm, eu só confundi quando fiz
                                        _session.m_pi.ei.csi.setValues(pWi.id, pWi._typeid, pWi.c);

                                        cs = sIff.getInstance().findClubSet(pWi._typeid);

                                        if (cs != null)
                                            for (var j = 0; j < 5; ++j)
                                                _session.m_pi.ei.csi.enchant_c[j] = (short)(cs.SlotStats.getSlot[j] + pWi.clubset_workshop.c[j]);

                                        _session.m_pi.ei.clubset = pWi;
                                        _session.m_pi.ue.clubset_id = pWi.id;

                                        // Update ON DB
                                        snmdb.NormalManagerDB.getInstance().add(0, new CmdUpdateClubsetEquiped(_session.m_pi.uid, (int)clubset), SQLDBResponse, this);

                                        // Update ON GAME
                                        p.init_plain(0x216);

                                        p.WriteUInt32((uint)UtilTime.GetSystemTimeAsUnix());
                                        p.WriteUInt32(1);   // Count

                                        p.WriteByte(item.type);
                                        p.WriteUInt32(item._typeid);
                                        p.WriteInt32(item.id);
                                        p.WriteUInt32(item.flag_time);
                                        p.WriteBytes(item.stat.ToArray());
                                        p.WriteInt32((item.STDA_C_ITEM_TIME > 0) ? item.STDA_C_ITEM_TIME : item.STDA_C_ITEM_QNTD);
                                        p.WriteZeroByte(25);

                                        packet_func.session_send(p, _session, 1);

                                    }
                                    else
                                        _smp.message_pool.getInstance().push(new message("[room::requestChangePlayerItemRoom][Info][Warning] PLAYER[UID=" + (_session.m_pi.uid)
                                                + "] nao conseguiu achar o ClubSet\"CV1\"[ID="
                                                + (item.id) + "] que acabou de adicionar para ele. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                                }
                                else
                                    _smp.message_pool.getInstance().push(new message("[room::requestChangePlayerItemRoom][Info][Warning] PLAYER[UID=" + (_session.m_pi.uid)
                                            + "] nao conseguiu adicionar o ClubSet[TYPEID="
                                            + (item._typeid) + "] para ele. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            }
                            else
                                _smp.message_pool.getInstance().push(new message("[room::requestChangePlayerItemRoom][Info][Warning] PLAYER[UID=" + (_session.m_pi.uid)
                                        + "] nao conseguiu inicializar o ClubSet[TYPEID="
                                        + (bi._typeid) + "] para ele. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                        }
                    }

                }
                else
                {   // ClubSet Acabou o tempo

                    // Coloca o ClubSet CV1 no lugar do ClubSet que acabou o tempo
                    pWi = _session.m_pi.findWarehouseItemByTypeid(AIR_KNIGHT_SET);

                    if (pWi != null)
                    {

                        _smp.message_pool.getInstance().push(new message("[room::requestChangePlayerItemRoom][Info][Warning] PLAYER[UID=" + (_session.m_pi.uid)
                                + "] tentou trocar o ClubSet[ID=" + (clubset) + "] para comecar o jogo, mas acabou o tempo do ClubSet[ID="
                                + (clubset) + "], colocando o ClubSet Padrao\"CV1\" do player. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                        // Esse C do WarehouseItem, que pega do DB, não é o ja updado inicial da taqueira é o que fica tabela enchant, 
                        // que no original fica no warehouse msm, eu só confundi quando fiz
                        _session.m_pi.ei.csi.setValues(pWi.id, pWi._typeid, pWi.c);

                        var cs = sIff.getInstance().findClubSet(pWi._typeid);

                        if (cs != null)
                            for (var j = 0; j < 5; ++j)
                                _session.m_pi.ei.csi.enchant_c[j] = (short)(cs.SlotStats.getSlot[j] + pWi.clubset_workshop.c[j]);

                        _session.m_pi.ei.clubset = pWi;
                        clubset = _session.m_pi.ue.clubset_id = pWi.id;

                        // Update ON DB
                        snmdb.NormalManagerDB.getInstance().add(0, new CmdUpdateClubsetEquiped(_session.m_pi.uid, (int)clubset), SQLDBResponse, this);

                    }
                    else
                    {

                        _smp.message_pool.getInstance().push(new message("[room::requestChangePlayerItemRoom][Info][Warning] PLAYER[UID=" + (_session.m_pi.uid)
                                + "] tentou trocar o ClubSet[ID=" + (clubset) + "] para comecar o jogo, mas acabou o tempo do ClubSet[ID="
                                + (clubset) + "], ele nao tem o ClubSet Padrao\"CV1\", adiciona o ClubSet pardrao\"CV1\" para ele. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                        BuyItem bi = new BuyItem();
                        stItem item = new stItem(); ;

                        bi.id = -1;
                        bi._typeid = AIR_KNIGHT_SET;
                        bi.qntd = 1;

                        ItemManager.initItemFromBuyItem(_session.m_pi, item, bi, false, 0, 0, 1/*Não verifica o Level*/);

                        if (item._typeid != 0)
                        {

                            if ((clubset = (int)ItemManager.addItem(item, _session, 2/*Padrão Item*/, 0)) != Convert.ToUInt32(ItemManager.RetAddItem.T_ERROR))
                            {

                                // Equipa o ClubSet CV1
                                pWi = _session.m_pi.findWarehouseItemById(clubset);

                                if (pWi != null)
                                {

                                    // Esse C do WarehouseItem, que pega do DB, não é o ja updado inicial da taqueira é o que fica tabela enchant, 
                                    // que no original fica no warehouse msm, eu só confundi quando fiz
                                    _session.m_pi.ei.csi.setValues(pWi.id, pWi._typeid, pWi.c);

                                    var cs = sIff.getInstance().findClubSet(pWi._typeid);

                                    if (cs != null)
                                        for (var j = 0; j < 5; ++j)
                                            _session.m_pi.ei.csi.enchant_c[j] = (short)(cs.SlotStats.getSlot[j] + pWi.clubset_workshop.c[j]);

                                    _session.m_pi.ei.clubset = pWi;
                                    _session.m_pi.ue.clubset_id = pWi.id;

                                    // Update ON DB
                                    snmdb.NormalManagerDB.getInstance().add(0, new CmdUpdateClubsetEquiped(_session.m_pi.uid, (int)clubset), SQLDBResponse, this);

                                    // Update ON GAME
                                    p.init_plain(0x216);

                                    p.WriteUInt32((uint)UtilTime.GetSystemTimeAsUnix());
                                    p.WriteUInt32(1);   // Count

                                    p.WriteByte(item.type);
                                    p.WriteUInt32(item._typeid);
                                    p.WriteInt32(item.id);
                                    p.WriteUInt32(item.flag_time);
                                    p.WriteBytes(item.stat.ToArray());
                                    p.WriteInt32((item.STDA_C_ITEM_TIME > 0) ? item.STDA_C_ITEM_TIME : item.STDA_C_ITEM_QNTD);
                                    p.WriteZeroByte(25);

                                    packet_func.session_send(p, _session, 1);

                                }
                                else
                                    _smp.message_pool.getInstance().push(new message("[room::requestChangePlayerItemRoom][Info][Warning] PLAYER[UID=" + (_session.m_pi.uid)
                                            + "] nao conseguiu achar o ClubSet\"CV1\"[ID="
                                            + (item.id) + "] que acabou de adicionar para ele. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            }
                            else
                                _smp.message_pool.getInstance().push(new message("[room::requestChangePlayerItemRoom][Info][Warning] PLAYER[UID=" + (_session.m_pi.uid)
                                        + "] nao conseguiu adicionar o ClubSet[TYPEID="
                                        + (item._typeid) + "] para ele. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                        }
                        else
                            _smp.message_pool.getInstance().push(new message("[room::requestChangePlayerItemRoom][Info][Warning] PLAYER[UID=" + (_session.m_pi.uid)
                                    + "] nao conseguiu inicializar o ClubSet[TYPEID="
                                    + (bi._typeid) + "] para ele. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                    }
                }

            }
            else
            {

                error = (clubset == 0) ? 1/*client give invalid item id*/ : (pWi == null ? 2/*Item Not Found*/ : 3/*Erro item typeid invalid*/);

                pWi = _session.m_pi.findWarehouseItemByTypeid(AIR_KNIGHT_SET);

                if (pWi != null)
                {

                    _smp.message_pool.getInstance().push(new message("[room::requestChangePlayerItemRoom][Info][Warning] PLAYER[UID=" + (_session.m_pi.uid)
                            + "] tentou trocar o ClubSet[ID=" + (clubset) + "] para comecar o jogo, mas deu Error[VALUE="
                            + (error) + "], colocando o ClubSet Padrao\"CV1\" do player. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                    // Esse C do WarehouseItem, que pega do DB, não é o ja updado inicial da taqueira é o que fica tabela enchant, 
                    // que no original fica no warehouse msm, eu só confundi quando fiz
                    _session.m_pi.ei.csi.setValues(pWi.id, pWi._typeid, pWi.c);

                    var cs = sIff.getInstance().findClubSet(pWi._typeid);

                    if (cs != null)
                        for (var j = 0; j < 5; ++j)
                            _session.m_pi.ei.csi.enchant_c[j] = (short)(cs.SlotStats.getSlot[j] + pWi.clubset_workshop.c[j]);

                    _session.m_pi.ei.clubset = pWi;
                    clubset = _session.m_pi.ue.clubset_id = pWi.id;

                    // Update ON DB
                    snmdb.NormalManagerDB.getInstance().add(0, new CmdUpdateClubsetEquiped(_session.m_pi.uid, (int)clubset), SQLDBResponse, this);

                }
                else
                {

                    _smp.message_pool.getInstance().push(new message("[room::requestChangePlayerItemRoom][Info][Warning] PLAYER[UID=" + (_session.m_pi.uid)
                            + "] tentou trocar o ClubSet[ID=" + (clubset) + "] para comecar o jogo, mas deu Error[VALUE="
                            + (error) + "], ele nao tem o ClubSet Padrao\"CV1\", adiciona o ClubSet pardrao\"CV1\" para ele. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                    BuyItem bi = new BuyItem();
                    stItem item = new stItem(); ;

                    bi.id = -1;
                    bi._typeid = AIR_KNIGHT_SET;
                    bi.qntd = 1;

                    ItemManager.initItemFromBuyItem(_session.m_pi, item, bi, false, 0, 0, 1/*Não verifica o Level*/);

                    if (item._typeid != 0)
                    {

                        if ((clubset = (int)ItemManager.addItem(item, _session, 2/*Padrão Item*/, 0)) != Convert.ToUInt32(ItemManager.RetAddItem.T_ERROR))
                        {

                            // Equipa o ClubSet CV1
                            pWi = _session.m_pi.findWarehouseItemById(clubset);

                            if (pWi != null)
                            {

                                // Esse C do WarehouseItem, que pega do DB, não é o ja updado inicial da taqueira é o que fica tabela enchant, 
                                // que no original fica no warehouse msm, eu só confundi quando fiz
                                _session.m_pi.ei.csi.setValues(pWi.id, pWi._typeid, pWi.c);

                                var cs = sIff.getInstance().findClubSet(pWi._typeid);

                                if (cs != null)
                                    for (var j = 0; j < 5; ++j)
                                        _session.m_pi.ei.csi.enchant_c[j] = (short)(cs.SlotStats.getSlot[j] + pWi.clubset_workshop.c[j]);

                                _session.m_pi.ei.clubset = pWi;
                                _session.m_pi.ue.clubset_id = pWi.id;

                                // Update ON DB
                                snmdb.NormalManagerDB.getInstance().add(0, new CmdUpdateClubsetEquiped(_session.m_pi.uid, (int)clubset), SQLDBResponse, this);

                                // Update ON GAME
                                p.init_plain(0x216);

                                p.WriteUInt32((uint)UtilTime.GetSystemTimeAsUnix());
                                p.WriteUInt32(1);   // Count

                                p.WriteByte(item.type);
                                p.WriteUInt32(item._typeid);
                                p.WriteInt32(item.id);
                                p.WriteUInt32(item.flag_time);
                                p.WriteBytes(item.stat.ToArray());
                                p.WriteInt32((item.STDA_C_ITEM_TIME > 0) ? item.STDA_C_ITEM_TIME : item.STDA_C_ITEM_QNTD);
                                p.WriteZeroByte(25);

                                packet_func.session_send(p, _session, 1);

                            }
                            else
                                _smp.message_pool.getInstance().push(new message("[room::requestChangePlayerItemRoom][Info][Warning] PLAYER[UID=" + (_session.m_pi.uid)
                                        + "] nao conseguiu achar o ClubSet\"CV1\"[ID="
                                        + (item.id) + "] que acabou de adicionar para ele. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                        }
                        else
                            _smp.message_pool.getInstance().push(new message("[room::requestChangePlayerItemRoom][Info][Warning] PLAYER[UID=" + (_session.m_pi.uid)
                                    + "] nao conseguiu adicionar o ClubSet[TYPEID="
                                    + (item._typeid) + "] para ele. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                    }
                    else
                        _smp.message_pool.getInstance().push(new message("[room::requestChangePlayerItemRoom][Info][Warning] PLAYER[UID=" + (_session.m_pi.uid)
                                + "] nao conseguiu inicializar o ClubSet[TYPEID="
                                + (bi._typeid) + "] para ele. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
            }

            // Ball(Comet)
            if (ball != 0 && (pWi = _session.m_pi.findWarehouseItemByTypeid(ball)) != null
                    && sIff.getInstance().getItemGroupIdentify(pWi._typeid) == IFF_GROUP.BALL)
            {

                _session.m_pi.ei.comet = pWi;
                _session.m_pi.ue.ball_typeid = ball;      // Ball(Comet) é o typeid que o cliente passa

                // Verifica se a Bola pode ser equipada
                if (_session.checkBallEquiped(_session.m_pi.ue))
                    ball = _session.m_pi.ue.ball_typeid;

                // Update ON DB
                snmdb.NormalManagerDB.getInstance().add(0, new CmdUpdateBallEquiped(_session.m_pi.uid, ball), SQLDBResponse, this);

            }
            else if (ball == 0)
            { // Bola 0 coloca a bola padrão para ele, se for premium user coloca a bola de premium user

                // Zera para equipar a bola padrão
                _session.m_pi.ei.comet = null;
                _session.m_pi.ue.ball_typeid = 0;

                // Verifica se a Bola pode ser equipada (Coloca para equipar a bola padrão
                if (_session.checkBallEquiped(_session.m_pi.ue))
                    ball = _session.m_pi.ue.ball_typeid;

                // Update ON DB
                snmdb.NormalManagerDB.getInstance().add(0, new CmdUpdateBallEquiped(_session.m_pi.uid, ball), SQLDBResponse, this);

            }
            else
            {

                error = (pWi == null ? 2/*Item Not Found*/ : 3/*Erro item typeid invalid*/);

                pWi = _session.m_pi.findWarehouseItemByTypeid(DEFAULT_COMET_TYPEID);

                if (pWi != null)
                {

                    _smp.message_pool.getInstance().push(new message("[room::requestChangePlayerItemRoom][Info][Warning] PLAYER[UID=" + (_session.m_pi.uid)
                            + "] tentou trocar a Ball[TYPEID=" + (ball) + "] para comecar o jogo, mas deu Error[VALUE="
                            + (error) + "], colocando a Ball Padrao do player. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                    _session.m_pi.ei.comet = pWi;
                    ball = _session.m_pi.ue.ball_typeid = pWi._typeid;

                    // Update ON DB
                    snmdb.NormalManagerDB.getInstance().add(0, new CmdUpdateBallEquiped(_session.m_pi.uid, ball), SQLDBResponse, this);

                }
                else
                {

                    _smp.message_pool.getInstance().push(new message("[room::requestChangePlayerItemRoom][Info][Warning] PLAYER[UID=" + (_session.m_pi.uid)
                            + "] tentou trocar a Ball[TYPEID=" + (ball) + "] para comecar o jogo, mas deu Error[VALUE="
                            + (error) + "], ele nao tem a Ball Padrao, adiciona a Ball pardrao para ele. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                    BuyItem bi = new BuyItem();
                    stItem item = new stItem(); ;

                    bi.id = -1;
                    bi._typeid = DEFAULT_COMET_TYPEID;
                    bi.qntd = 1;

                    ItemManager.initItemFromBuyItem(_session.m_pi, item, bi, false, 0, 0, 1/*Não verifica o Level*/);

                    if (item._typeid != 0)
                    {

                        if ((ball = (uint)ItemManager.addItem(item, _session, 2/*Padrão Item*/, 0)) != Convert.ToUInt32(ItemManager.RetAddItem.T_ERROR))
                        {

                            // Equipa a Ball padrao
                            pWi = _session.m_pi.findWarehouseItemById((int)ball);

                            if (pWi != null)
                            {

                                _session.m_pi.ei.comet = pWi;
                                _session.m_pi.ue.ball_typeid = pWi._typeid;

                                // Update ON DB
                                snmdb.NormalManagerDB.getInstance().add(0, new CmdUpdateBallEquiped(_session.m_pi.uid, ball), SQLDBResponse, this);

                                // Update ON GAME
                                p.init_plain(0x216);

                                p.WriteUInt32((uint)UtilTime.GetSystemTimeAsUnix());
                                p.WriteUInt32(1);   // Count

                                p.WriteByte(item.type);
                                p.WriteUInt32(item._typeid);
                                p.WriteInt32(item.id);
                                p.WriteUInt32(item.flag_time);
                                p.WriteBytes(item.stat.ToArray());
                                p.WriteInt32((item.STDA_C_ITEM_TIME > 0) ? item.STDA_C_ITEM_TIME : item.STDA_C_ITEM_QNTD);
                                p.WriteZeroByte(25);

                                packet_func.session_send(p, _session, 1);

                            }
                            else
                                _smp.message_pool.getInstance().push(new message("[room::requestChangePlayerItemRoom][Info][Warning] PLAYER[UID=" + (_session.m_pi.uid)
                                        + "] nao conseguiu achar a Ball[ID="
                                        + (item.id) + "] que acabou de adicionar para ele. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                        }
                        else
                            _smp.message_pool.getInstance().push(new message("[room::requestChangePlayerItemRoom][Info][Warning] PLAYER[UID=" + (_session.m_pi.uid)
                                    + "] nao conseguiu adicionar a Ball[TYPEID="
                                    + (item._typeid) + "] para ele. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                    }
                    else
                        _smp.message_pool.getInstance().push(new message("[channel::requestChangePlayerItemRoom][Info][Warning] PLAYER[UID=" + (_session.m_pi.uid)
                                + "] nao conseguiu inicializar a Ball[TYPEID="
                                + (bi._typeid) + "] para ele. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
            }

            // Verifica se o Mascot Equipado acabou o tempo
            if (_session.m_pi.ue.mascot_id != 0 && _session.m_pi.ei.mascot_info != null)
            {
                //FindUpdateItemByIdAndType
                var m_it = _session.m_pi.findUpdateItemByTypeidAndType((uint)_session.m_pi.ue.mascot_id, UpdateItem.UI_TYPE.MASCOT);

                if (m_it.Count > 0)
                {

                    // Desequipa o Mascot que acabou o tempo dele
                    _session.m_pi.ei.mascot_info = null;
                    _session.m_pi.ue.mascot_id = 0;

                    snmdb.NormalManagerDB.getInstance().add(0, new CmdUpdateMascotEquiped(_session.m_pi.uid, 0/*Mascot_id == 0 not equiped*/), SQLDBResponse, this);

                    // Update on GAME se não o cliente continua com o mascot equipado
                    packet_func.pacote04B(_session, (byte)ChangePlayerItemRoom.TYPE_CHANGE.TC_MASCOT, 0);
                    packet_func.session_send(p, _session, 0);

                }
            }

            return error;
        }
		
        void startGame(Player _session)
        {

            try
            {

                if (m_pGame == null)
                    throw new exception("[room::startGame] [Error] PLAYER[UID=" + (_session.m_pi.uid) + "] tentou comecar o jogo na sala[NUMERO="
                        + (m_ri.numero) + "], mas a sala nao tem nenhum jogo iniciado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM, 1, 0x5200101));

                if (m_ri.flag == 0)
                {

                    m_pGame.sendInitialData(_session);//aqui

                    if (m_ri.getTipo() == RoomInfo.ROOM_INFO_TYPE.STROKE || m_ri.getTipo() == RoomInfo.ROOM_INFO_TYPE.MATCH)
                        sendPlayerInfo(_session, 0x103);
                }
                else
                {   // Entra depois

                    try
                    {
                        List<PlayerRoomInfoEx> v_element = new List<PlayerRoomInfoEx>();
                        PlayerRoomInfoEx pri = null;

                        foreach (var _el in v_sessions)
                        {
                            if ((pri = getPlayerInfo(_el)) != null)
                                v_element.Add(pri);
                        }


                        // Send Make Room
                        var p = new PangyaBinaryWriter(0x113);

                        p.WriteByte(4);  // Cria sala
                        p.WriteByte(0);

                        p.WriteBytes(m_ri.ToArray());

                        packet_func.session_send(p, _session, 1);

                        // Send All Player Of Room
                        p.init_plain(0x113);

                        p.WriteByte(4);
                        p.WriteByte(1);

                        p.WriteByte((int)v_element.Count);

                        for (var i = 0; i < v_element.Count; i++)
                            p.WriteBytes(v_element[i].ToArray());

                        packet_func.session_send(p, _session, 1);

                        // Rate Pang
                        p.init_plain(0x113);

                        p.WriteByte(4);
                        p.WriteByte(2);

                        p.WriteUInt32(m_ri.rate_pang);

                        packet_func.session_send(p, _session, 1);

                        // Send Initial of Game
                        m_pGame.sendInitialDataAfter(_session);

                        // Add Player ON GAME to ALL players
                        p.init_plain(0x113);

                        p.WriteByte(7);
                        p.WriteByte(0);

                        p.WriteString(_session.m_pi.nickname);

                        p.WriteBytes(m_ri.ToArray());

                        p.WriteByte(v_element.Count);

                        p.WriteUInt32((uint32_t)v_sessions.Count);

                        for (var i = 0; i < v_element.Count; i++)
                            p.WriteBytes(v_element[i].ToArray());

                        // Send ALL players of room exceto ele
                        packet_func.vector_send(p, getSessions(_session), 1);

                    }
                    catch (exception e)
                    {
                        throw e;
                    }
                }

            }

            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[room::startGame][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }


        public void updateMaster(Player _session)
        {
            var p = new PangyaBinaryWriter();
            try
            {
                Player master = findSessionByUID((uint)m_ri.master);

                if (_session != null && _session.m_pi.m_cap.game_master && m_ri.master != -2)
                {
                    // Só troca o master se ele saiu da sala ou se ele não for GM
                    if (master == null || !(master.m_pi.m_cap.game_master/* & 4*/))
                    {
                        m_ri.master = (int)_session.m_pi.uid;
                        m_ri.state_flag = 0x100; // GM

                        if (master != null)
                        {
                            updatePlayerInfo(master);
                            p.init_plain(0x78);
                            p.WriteInt32(master.m_oid);
                            p.WriteByte((byte)~getPlayerInfo(master).state_flag.ready);

                            packet_func.room_broadcast(this, p, 1);
                        }

                        p = new PangyaBinaryWriter();
                        p.init_plain(0x7C);
                        p.WriteInt32(_session.m_oid);
                        p.WriteInt16(0);

                        packet_func.room_broadcast(this, p, 0);
                    }
                }
                else if (master == null && v_sessions.Count > 0 && m_ri.master != -2)
                {
                    if (m_ri.getTipo() != RoomInfo.ROOM_INFO_TYPE.SPECIAL_SHUFFLE_COURSE && m_ri.getTipo() != RoomInfo.ROOM_INFO_TYPE.GRAND_PRIX)
                    {
                        // Find GM 
                        var i = v_sessions.FirstOrDefault(pl => pl.m_pi.m_cap.game_master);

                        if (i != null)
                            master = i;
                        else
                            master = v_sessions[0];

                        m_ri.master = (int)master.m_pi.uid;
                        m_ri.state_flag = (short)(master.m_pi.m_cap.game_master ? 0x100 : 0);

                        updatePlayerInfo(master);

                        p = new PangyaBinaryWriter(0x7C);
                        p.WriteInt32(master.m_oid);
                        p.WriteInt16(0);

                        packet_func.room_broadcast(this, p, 0);
                    }
                }
            }
            catch (Exception e)
            {

                throw e;
            }
        }
        public void updateGuild(Player _session)
        {
            if (_session.m_pi.gi.uid == -1)
                throw new Exception($"[channel::UpdateGuild] [Error] PLAYER[UID={_session.m_pi.uid}] player nao esta em uma guild.");

            PlayerRoomInfoEx pri = getPlayerInfo(_session);

            if (pri == null)
                throw new Exception($"[channel::UpdateGuild] [Error] PLAYER[UID={_session.m_pi.uid}] nao tem o info do player na sala[NUMERO={m_ri.numero}]. Hacker ou Bug.");

            Guild guild = null;

            if (m_ri.guilds.guild_1_uid == 0 && m_ri.guilds.guild_2_uid != _session.m_pi.gi.uid)
            {
                m_ri.guilds.guild_1_uid = _session.m_pi.gi.uid;
                m_ri.guilds.guild_1_nome = _session.m_pi.gi.name;
                m_ri.guilds.guild_1_mark = _session.m_pi.gi.mark_emblem;
                m_ri.guilds.guild_1_index_mark = (ushort)_session.m_pi.gi.index_mark_emblem;

                pri.state_flag.team = 0;
                guild = m_guild_manager.addGuild(Guild.eTEAM.RED, m_ri.guilds.guild_1_uid);
            }
            else if (m_ri.guilds.guild_1_uid == _session.m_pi.gi.uid)
            {
                pri.state_flag.team = 0;
                guild = m_guild_manager.findGuildByTeam(Guild.eTEAM.RED);
            }
            else if (m_ri.guilds.guild_2_uid == 0)
            {
                m_ri.guilds.guild_2_uid = _session.m_pi.gi.uid;
                m_ri.guilds.guild_2_nome = _session.m_pi.gi.name;
                m_ri.guilds.guild_2_mark = _session.m_pi.gi.mark_emblem;
                m_ri.guilds.guild_2_index_mark = (ushort)_session.m_pi.gi.index_mark_emblem;

                pri.state_flag.team = 1;
                guild = m_guild_manager.addGuild(Guild.eTEAM.BLUE, m_ri.guilds.guild_2_uid);
            }
            else
            {
                pri.state_flag.team = 1;
                guild = m_guild_manager.findGuildByTeam(Guild.eTEAM.BLUE);
            }

            if (guild != null)
            {
                guild.addPlayer(_session);
            }
            else
            {
                _smp.message_pool.getInstance().push(new message(
                    $"[room::updateGuild][Warning] PLAYER[UID={_session.m_pi.uid}] tentou entrar em uma guild da sala[NUMERO={m_ri.numero}], mas nao conseguiu criar ou achar nenhum guild na sala. Bug.",
                   type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            m_teans[pri.state_flag.team].addPlayer(_session);
        }

        public int findIndexSession(Player _session)
        {
            if (_session == null)
                throw new Exception("[room::findIndexSession] [Error] _session is null.");

            int index = -1;
            for (int i = 0; i < v_sessions.Count; ++i)
            {
                if (v_sessions[i].getUID() == _session.getUID())
                {
                    index = i;
                    break;
                }
            }
            return index;
        }

        public int findIndexSession(uint _uid)
        {
            if (_uid == 0)
                throw new Exception("[room::findIndexSession] [Error] _uid is invalid(zero).");

            int index = -1;

            for (int i = 0; i < v_sessions.Count; ++i)
            {
                if (v_sessions[i].m_pi.uid == _uid)
                {
                    index = i;
                    break;
                }
            }

            return index;
        }
        public void @lock()
        {
            //Monitor.Exit(m_lock_cs);

            if (m_destroying)
            {

                //Monitor.Exit(m_lock_cs);

                throw new exception("[room::lock] [Error] room esta no estado para ser destruida, nao pode bloquear ela.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM, 150, 0));
            }

            m_lock_spin_state++;	// Bloqueado
        }

        public bool trylock()
        {
            bool ret = false;
            ret = true;

            if (m_destroying)
            {
                //Monitor.Exit(m_lock_cs);
                throw new exception("[room::tryLock] [Error] room esta no estado para ser destruida, nao pode bloquear ela.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM, 150, 0));
            }

            m_lock_spin_state++; // Bloqueado

            return ret;
        }

        public void unlock()
        {
            int state = Interlocked.Decrement(ref m_lock_spin_state);

            if (state < 0)
            {
                _smp.message_pool.getInstance().push(
                    new message(
                        "[room::unlock][Warning] a sala[NUMERO=" + m_ri.numero + "] ja esta desbloqueada.",
                        type_msg.CL_ONLY_FILE_LOG
                    ));
            }

            //Monitor.Exit(m_lock_cs);
        }

        public void setDestroying()
        {
            m_destroying = true;
        }

        public bool geDestroying()
        {
            return m_destroying;
        }


        public List<InviteChannelInfo> getAllInvite()
        {
            return v_invite;
        }

        public void requestEndAfterEnter()
        {
            try
            {

                if (m_pGame == null)
                    return;

                m_pGame.requestEndAfterEnter();

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[room::requestEndAfterEnter][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }
        // Personal Shop
        public void requestOpenEditSaleShop(Player _session, packet _packet)
        {
            if (!_session.getState())
            {
                throw new exception("[room::requestOpenEditSaleShop] [Error] player nao esta connectado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }
            if (_packet == null)
            {
                throw new exception("[room::requestOpenEditSaleShop] [Error] _packet is null", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }

            var p = new PangyaBinaryWriter();

            try
            {

                if (m_personal_shop.openShopToEdit(_session, p))
                {
                    packet_func.room_broadcast(this,
                        p, 1);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[room::requestOpenEditSaleShop][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestCloseSaleShop(Player _session, packet _packet)
        {
            if (!_session.getState())
            {
                throw new exception("[room::requestCloseSaleShop] [Error] player nao esta connectado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }
            if (_packet == null)
            {
                throw new exception("[room::requestCloseSaleShop] [Error] _packet is null", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }

            var p = new PangyaBinaryWriter();

            try
            {

                if (m_personal_shop.closeShop(_session, p))
                {
                    packet_func.room_broadcast(this,
                       p, 1);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[room::requestCloseSaleShop][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestChangeNameSaleShop(Player _session, packet _packet)
        {
            if (!_session.getState())
            {
                throw new exception("[room::requestChangeNameSaleShop] [Error] player nao esta connectado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }
            if (_packet == null)
            {
                throw new exception("[room::requestChangeNameSaleShop] [Error] _packet is null", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }

            var p = new PangyaBinaryWriter();

            try
            {

                if (m_personal_shop.changeShopName(_session,
                    _packet.ReadString(), p))
                {
                    packet_func.room_broadcast(this,
                        p, 1);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[room::requestChangeNameSaleShop][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestOpenSaleShop(Player _session, packet _packet)
        {
            if (!_session.getState())
            {
                throw new exception("[room::requestOpenSaleShop] [Error] player nao esta connectado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }
            if (_packet == null)
            {
                throw new exception("[room::requestOpenSaleShop] [Error] _packet is null", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }

            try
            {

                m_personal_shop.openShop(_session, _packet);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[room::requestOpenSaleShop][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestBuyItemSaleShop(Player _session, packet _packet)
        {
            if (!_session.getState())
            {
                throw new exception("[room::requestBuyItemSaleShop] [Error] player nao esta connectado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }
            if (_packet == null)
            {
                throw new exception("[room::requestBuyItemSaleShop] [Error] _packet is null", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }

            try
            {

                m_personal_shop.buyInShop(_session, _packet);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[room::requestBuyItemSaleShop][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }


        public void requestVisitCountSaleShop(Player _session, packet _packet)
        {
            if (!_session.getState())
            {
                throw new exception("[room::requestVisitCountSaleShop] [Error] player nao esta connectado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }
            if (_packet == null)
            {
                throw new exception("[room::requestVisitCountSaleShop] [Error] _packet is null", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }

            try
            {

                m_personal_shop.visitCountShop(_session);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[room::requestVisitCount][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestPangSaleShop(Player _session, packet _packet)
        {
            if (!_session.getState())
            {
                throw new exception("[room::requestPangSaleShop] [Error] player nao esta connectado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }
            if (_packet == null)
            {
                throw new exception("[room::requestPangSaleShop] [Error] _packet is null", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }

            try
            {

                m_personal_shop.pangShop(_session);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[room::requestPangSaleShop][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestCancelEditSaleShop(Player _session, packet _packet)
        {
            if (!_session.getState())
            {
                throw new exception("[room::requestCancelEditSaleShop] [Error] player nao esta connectado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }
            if (_packet == null)
            {
                throw new exception("[room::requestCancelEditSaleShop] [Error] _packet is null", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }

            var p = new PangyaBinaryWriter();

            try
            {

                if (m_personal_shop.cancelEditShop(_session, p))
                {
                    packet_func.room_broadcast(this,
                        p, 1);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[room::reuqestCancelEditSaleShop][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestViewSaleShop(Player _session, packet _packet)
        {
            if (!_session.getState())
            {
                throw new exception("[room::requestViewSaleShop] [Error] player nao esta connectado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }
            if (_packet == null)
            {
                throw new exception("[room::requestViewSaleShop] [Error] _packet is null", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }

            try
            {

                m_personal_shop.viewShop(_session, _packet.ReadUInt32());

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[room::requestViewSaleShop][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestCloseViewSaleShop(Player _session, packet _packet)
        {
            if (!_session.getState())
            {
                throw new exception("[room::requestCloseViewSaleShop] [Error] player nao esta connectado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }
            if (_packet == null)
            {
                throw new exception("[room::requestCloseViewSaleShop] [Error] _packet is null", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }

            try
            {

                m_personal_shop.closeViewShop(_session, _packet.ReadUInt32());

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[room::requestCloseViewSaleShop][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        // Room Wait
        // Room Wait
        public void requestToggleAssist(Player _session, packet _packet)
        {
            if (!_session.getState())
            {
                throw new exception("[room::requestToggleAssist] [Error] player nao esta connectado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }
            if (_packet == null)
            {
                throw new exception("[room::requestToggleAssist] [Error] _packet is null", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }

            PangyaBinaryWriter p = new PangyaBinaryWriter();

            try
            {
                if (m_pGame == null)
                {
                    stItem item = new stItem();

                    var rt = ItemManager.RetAddItem.T_INIT_VALUE;

                    item.type = 2;
                    item.id = -1;
                    item._typeid = ASSIST_ITEM_TYPEID;

                    var pWi = _session.m_pi.findWarehouseItemByTypeid(ASSIST_ITEM_TYPEID);
                    if (pWi == null)
                    { // Não tem ativa o Assist
                        item.qntd = 1;
                        item.STDA_C_ITEM_QNTD = 1;

                        if ((rt = ItemManager.addItem(item, _session, 0, 0)) < 0)
                        {
                            throw new exception("[room::requestToggleAssist] [Error] PLAYER[UID=" + _session.m_pi.uid + "] tentou ativar o Assist[TYPEID=" + Convert.ToString(ASSIST_ITEM_TYPEID) + "], mas nao conseguiu adicionar o item. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                                1, 0x5200801));
                        }
                        _session.m_pi.assist_flag = true;
                        _smp.message_pool.getInstance().push(new message("[room::requestToggleAssist][Info] PLAYER[UID=" + _session.m_pi.uid + "] Ligou o Assist Modo", type_msg.CL_FILE_LOG_AND_CONSOLE));
                        _session.m_pi.Assistent.id = item.id;
                    }
                    else
                    { // Já tem, Desativa
                        _session.m_pi.assist_flag = false;
                        item.id = pWi.id;
                        item.qntd = (int)((pWi.STDA_C_ITEM_QNTD <= 0) ? 1 : pWi.STDA_C_ITEM_QNTD);
                        item.STDA_C_ITEM_QNTD = (short)(item.qntd * -1);
                        if (ItemManager.removeItem(item, _session) <= 0)
                        {
                            throw new exception("[room::requestToggleAssist] [Error] PLAYER[UID=" + _session.m_pi.uid + "] tentou desativar o Assist[TYPEID=" + Convert.ToString(ASSIST_ITEM_TYPEID) + "], mas nao conseguiu remover o item. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                                2, 0x5200802));
                        }

                        _smp.message_pool.getInstance().push(new message("[room::requestToggleAssist][Info] PLAYER[UID=" + _session.m_pi.uid + "] Desligou o Assist Modo", type_msg.CL_FILE_LOG_AND_CONSOLE));
                        _session.m_pi.Assistent.id = item.id;
                    }

                    // UPDATE ON GAME
                    if (rt != ItemManager.RetAddItem.T_SUCCESS_PANG_AND_EXP_AND_CP_POUCH)
                    {

                        p.init_plain(0x216);

                        p.WriteUInt32((uint32_t)UtilTime.GetSystemTimeAsUnix());
                        p.WriteUInt32(1); // Count

                        p.WriteByte(item.type);
                        p.WriteUInt32(item._typeid);
                        p.WriteInt32(item.id);
                        p.WriteUInt32(item.flag_time);
                        p.WriteBytes(item.stat.ToArray());
                        p.WriteInt32(item.STDA_C_ITEM_TIME > 0 ? item.STDA_C_ITEM_TIME : item.STDA_C_ITEM_QNTD);
                        p.WriteZeroByte(25);
                        packet_func.session_send(p,
                            _session, 1);
                    }

                    // Resposta ao Toggle Assist
                    p.init_plain(0x26A);

                    p.WriteUInt32(0); // OK
                    p.WriteUInt32(ASSIST_ITEM_TYPEID);
                    p.WriteUInt32(_session.m_pi.uid);
                    packet_func.session_send(p,
                        _session, 1);
                }
                else
                {
                    _smp.message_pool.getInstance().push(new message($"[room::requestToggleAssist][ErrorSystem] é hacker de packet: {_session.m_pi.uid} ", type_msg.CL_FILE_LOG_AND_CONSOLE));
                    // Resposta ao Toggle Assist
                    p.init_plain(0x16A);//set 
                    p.WriteUInt32(0);
                    packet_func.session_send(p,
                        _session, 1);
                }
            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[room::requestToggleAssist][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x26A);

                p.WriteUInt32((ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) == STDA_ERROR_TYPE.ROOM) ? ExceptionError.STDA_SYSTEM_ERROR_DECODE(e.getCodeError()) : 0x5200800);

                packet_func.session_send(p,
                    _session, 1);
            }
        }

        public void requestChangeTeam(Player _session, packet _packet)
        {
            if (!_session.getState())
            {
                throw new exception("[room::requestChangeTeam] [Error] player nao esta connectado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }
            if (_packet == null)
            {
                throw new exception("[room::requestChangeTeam] [Error] _packet is null", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }

            PangyaBinaryWriter p = new PangyaBinaryWriter();

            try
            {

                byte team = _packet.ReadByte();

                PlayerRoomInfoEx pPri = getPlayerInfo(_session);

                if (pPri == null)
                {
                    throw new exception("[room::requestChangeTeam] [Error] PLAYER[UID=" + _session.m_pi.uid + "] tentou trocar o team(time) na sala[NUMERO=" + m_ri.numero + "], mas a sala nao tem o info do player. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                        1505, 0));
                }

                if (m_teans.Count() < 2)
                {
                    throw new exception("[room::requestChangeTeam] [Error] PLAYER[UID=" + _session.m_pi.uid + "] tentou trocar o team(time) na sala[NUMERO=" + m_ri.numero + "], mas a sala nao tem teans(times) suficiente. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                        1506, 0));
                }

                // Sai do outro team(time) se ele estiver
                try
                {

                    m_teans[pPri.state_flag.team].deletePlayer(_session, 3);

                }
                catch (exception e)
                {

                    _smp.message_pool.getInstance().push(new message("[room::requestChangeTeam][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                }

                // Add o Player ao (team)time
                m_teans[team].addPlayer(_session);

                pPri.state_flag.team = team; // ~pri.state_flag.uFlag.stFlagBit.team;


                p = new PangyaBinaryWriter((ushort)0x7D);

                p.WriteInt32(_session.m_oid);

                p.WriteByte(team);

                packet_func.room_broadcast(this,
                    p, 0);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[room::requestChangeTeam][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        // Request Game
        public virtual bool requestStartGame(Player _session, packet _packet)
        {
            if (!_session.getState())
            {
                throw new exception("[room::requestStartGame] [Error] player nao esta connectado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }
            if (_packet == null)
            {
                throw new exception("[room::requestStartGame] [Error] _packet is null", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }

            PangyaBinaryWriter p = new PangyaBinaryWriter();

            bool ret = true;

            try
            {

                if (m_ri.master != _session.m_pi.uid)
                {
                    if (!_session.m_pi.mi.capability.game_master)
                        throw new exception("[room::requestStartGame] [Error] PLAYER[UID=" + _session.m_pi.uid + "] tentou comecar o jogo na sala[NUMERO=" + m_ri.numero + "], mas ele nao é o master da sala. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                            1, 0x5900201));
                }

                // Verifica se já tem um jogo inicializado e lança error se tiver, para o cliente receber uma resposta
                if (m_pGame != null)
                {
                    throw new exception("[room::requestStartGame] [Error] PLAYER[UID=" + _session.m_pi.uid + "] tentou comecar o jogo na sala[NUMERO=" + m_ri.numero + "], mas ja tem um jogo inicializado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                        8, 0x5900202));
                }

                // Verifica se todos estão prontos se não da erro
                if (!isAllReady())
                {
                    throw new exception("[room::requestStartGame] [Error] PLAYER[UID=" + _session.m_pi.uid + "] tentou comecar o jogo na sala[NUMERO=" + m_ri.numero + ", MASTER=" + Convert.ToString(m_ri.master) + "], mas nem todos jogadores estao prontos. Hacker ou Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    8, 0x5900202));
                }

                // Coloquei para verificar se a type de Bot tourney não está ativo verifica o resto das condições
                if (!m_bot_tourney
                    && v_sessions.Count == 1
                    && m_ri.getTipo() != RoomInfo.ROOM_INFO_TYPE.PRACTICE
                    && m_ri.getTipo() != RoomInfo.ROOM_INFO_TYPE.GRAND_PRIX
                    && m_ri.getTipo() != RoomInfo.ROOM_INFO_TYPE.GRAND_ZODIAC_INT
                    && m_ri.getTipo() != RoomInfo.ROOM_INFO_TYPE.GRAND_ZODIAC_ADV
                    && m_ri.getTipo() != RoomInfo.ROOM_INFO_TYPE.GRAND_ZODIAC_PRACTICE)
                {
                    packet_func.session_send(packet_func.pacote049(this, PangyaEnums.TGAME_CREATE_RESULT.CREATE_GAME_CREATE_FAILED2), _session, 0);
                    return false;
                }

                // Match
                if (m_ri.getTipo() == RoomInfo.ROOM_INFO_TYPE.MATCH)
                {

                    if (m_teans.Count == 0)
                    {
                        packet_func.session_send(packet_func.pacote049(this, PangyaEnums.TGAME_CREATE_RESULT.CREATE_GAME_CREATE_FAILED2), _session, 0);
                        return false;
                    }

                    if (m_teans.Count() == 1)
                    {
                        packet_func.session_send(packet_func.pacote049(this, PangyaEnums.TGAME_CREATE_RESULT.CREATE_GAME_CREATE_FAILED2), _session, 0);
                        return false;
                    }

                    if (v_sessions.Count() % 2 == 1)
                    {
                        packet_func.session_send(packet_func.pacote049(this, PangyaEnums.TGAME_CREATE_RESULT.CREATE_GAME_CREATE_FAILED2), _session, 0);
                        return false;
                    }

                    if (v_sessions.Count() == 2 && (m_teans[0].getNumPlayers() == 0 || m_teans[1].getNumPlayers() == 0))
                    {
                        packet_func.session_send(packet_func.pacote049(this, PangyaEnums.TGAME_CREATE_RESULT.CREATE_GAME_CREATE_FAILED2), _session, 0);
                        return false;
                    }

                    if (v_sessions.Count() == 4 && (m_teans[0].getNumPlayers() < 2 || m_teans[1].getNumPlayers() < 2))
                    {
                        packet_func.session_send(packet_func.pacote049(this, PangyaEnums.TGAME_CREATE_RESULT.CREATE_GAME_CREATE_FAILED2), _session, 0);
                        return false;
                    }

                    if (m_ri.max_player == 4 && v_sessions.Count() < 4)
                    {
                        packet_func.session_send(packet_func.pacote049(this, PangyaEnums.TGAME_CREATE_RESULT.CREATE_GAME_CREATE_FAILED2), _session, 0);
                        return false;
                    }
                }

                // Guild Battle
                if (m_ri.getTipo() == RoomInfo.ROOM_INFO_TYPE.GUILD_BATTLE)
                {

                    if (v_sessions.Count() % 2 == 1)
                    {
                        packet_func.session_send(packet_func.pacote049(this, PangyaEnums.TGAME_CREATE_RESULT.CREATE_GAME_CREATE_FAILED2), _session, 0);
                        return false;
                    }

                    var error_check = m_guild_manager.isGoodToStart();

                    if (error_check <= 0)
                    {

                        switch (error_check)
                        {
                            case 0: // Não tem duas guilds na sala
                                throw new exception("[room::requestStartGame] [Error] PLAYER[UID=" + _session.m_pi.uid + "] tentou comecar o Guild Battle na sala[NUMERO=" + m_ri.numero + "], mas nao tem guilds suficientes para comecar o jogo. Hacker ou Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                                    10, 0x5900202));
                            case -1: // Não tem o mesmo número de jogadores na sala as duas guilds
                                throw new exception("[room::requestStartGame] [Error] PLAYER[UID=" + _session.m_pi.uid + "] tentou comecar o Guild Battle na sala[NUMERO=" + m_ri.numero + "], mas as duas guilds nao tem o mesmo numero de jogadores na sala. Hacker ou Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                                    11, 0x5900202));
                            case -2: // Uma das Guilds ou as duas não tem 2 jogadores
                                throw new exception("[room::requestStartGame] [Error] PLAYER[UID=" + _session.m_pi.uid + "] tentou comecar o Guild Battle na sala[NUMERO=" + m_ri.numero + "], mas uma ou as duas guilds tem menos que 2 jogadores na sala. Hacker ou Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                                    12, 0x5900202));
                        }
                    }
                }

                // Chip-in Practice
                if (m_ri.getTipo() == RoomInfo.ROOM_INFO_TYPE.GRAND_ZODIAC_PRACTICE)
                {

                    var pTicket = _session.m_pi.findWarehouseItemByTypeid(CHIP_IN_PRACTICE_TICKET_TYPEID);

                    if (pTicket == null)
                    {
                        packet_func.session_send(packet_func.pacote049(this, PangyaEnums.TGAME_CREATE_RESULT.CREATE_GAME_CREATE_FAILED2), _session, 0);
                        return false;
                    }

                    if (pTicket.c[0] < 1)
                    {
                        packet_func.session_send(packet_func.pacote049(this, PangyaEnums.TGAME_CREATE_RESULT.CREATE_GAME_CREATE_FAILED2), _session, 0);
                        return false;
                    }

                    stItem item = new stItem
                    {
                        id = pTicket.id,
                        type = 2,
                        _typeid = pTicket._typeid,
                        qntd = 1
                    };
                    item.c[0] = (short)((short)item.qntd * -1);

                    // UPDATE ON SERVER AND DB
                    if (ItemManager.removeItem(item, _session) <= 0)
                    {
                        packet_func.session_send(packet_func.pacote049(this, PangyaEnums.TGAME_CREATE_RESULT.CREATE_GAME_CREATE_FAILED2), _session, 0);
                        return false;
                    }

                    // UPDATE ON GAME
                    p.init_plain(0x216);

                    p.WriteUInt32((uint32_t)UtilTime.GetSystemTimeAsUnix());
                    p.WriteUInt32(1); // Count;

                    p.WriteByte(item.type);
                    p.WriteUInt32(item._typeid);
                    p.WriteInt32(item.id);
                    p.WriteUInt32(item.flag_time);
                    p.WriteBytes(item.stat.ToArray());
                    p.WriteUInt32((item.c[3] > 0) ? item.c[3] : item.c[0]);
                    p.WriteZeroByte(25);

                    packet_func.session_send(p,
                        _session, 1);
                }

                if (m_ri.getMap() >= 0x7Fu)
                {

                    // Special Shuffle Course
                    if (m_ri.getTipo() == RoomInfo.ROOM_INFO_TYPE.SPECIAL_SHUFFLE_COURSE && m_ri.getModo() == RoomInfo.ROOM_INFO_MODO.M_SHUFFLE_COURSE)
                    {

                        m_ri.course = (RoomInfo.ROOM_INFO_COURSE)(0x80 | (int)RoomInfo.ROOM_INFO_COURSE.CHRONICLE_1_CHAOS);
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
                            m_ri.course = (RoomInfo.ROOM_INFO_COURSE)(0x80 | Convert.ToByte(lc.Value));
                        }
                    }
                }

                if (!_session.m_pi.m_cap.premium_user || !_session.m_pi.m_cap.game_master)
                {
                    // Verifica se ele tem o ticket para criar o Bot se não manda mensagem dizenho que ele não tem ticket para criar o bot
                    var pWi = _session.m_pi.findWarehouseItemByTypeid(TICKET_BOT_TYPEID) != null ? _session.m_pi.findWarehouseItemByTypeid(TICKET_BOT_TYPEID) : _session.m_pi.findWarehouseItemByTypeid(TICKET_BOT_TYPEID2);

                    if (pWi != null && pWi.STDA_C_ITEM_QNTD > 0)
                    {

                        stItem item = new stItem();

                        item.type = 2;
                        item.id = pWi.id;
                        item._typeid = pWi._typeid;
                        item.qntd = 1;
                        item.c[0] = (short)(item.qntd * -1);

                        if (ItemManager.removeItem(item, _session) > 0)
                        {

                            // Atualiza o item no Jogo e Add o Bot e manda a mensagem que o bot foi add
                            p.init_plain(0x216);

                            p.WriteUInt32((uint)UtilTime.GetSystemTimeAsUnix());
                            p.WriteUInt32(1); // Count;

                            p.WriteByte(item.type);
                            p.WriteUInt32(item._typeid);
                            p.WriteInt32(item.id);
                            p.WriteUInt32(item.flag_time);
                            p.WriteBytes(item.stat.ToArray());
                            p.WriteUInt32((item.c[3] > 0) ? item.c[3] : item.c[0]);
                            p.WriteZero(25);

                            packet_func.session_send(p,
                                _session, 1);
                        }
                        else
                        {

                            _smp.message_pool.getInstance().push(new message("[room::makeBot] [Error] PLAYER[UID=" + _session.m_pi.uid + "] nao conseguiu deletar o TICKET_BOT[TYPEID=" + Convert.ToString(TICKET_BOT_TYPEID) + ", ID=" + Convert.ToString(item.id) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                        }

                    }

                }
                RateValue rv = new RateValue
                {
                    // Att Exp rate, e Pang rate, que começou o jogo


                    exp = m_ri.rate_exp = (uint)sgs.gs.getInstance().getInfo().rate.exp,
                    pang = m_ri.rate_pang = (uint)sgs.gs.getInstance().getInfo().rate.pang
                };

                // Angel Event
                m_ri.angel_event = sgs.gs.getInstance().getInfo().rate.angel_event == 1;

                rv.clubset = (uint)sgs.gs.getInstance().getInfo().rate.club_mastery;
                rv.rain = (uint)sgs.gs.getInstance().getInfo().rate.chuva;
                rv.treasure = (uint)sgs.gs.getInstance().getInfo().rate.treasure;

                rv.persist_rain = 0; // Persist rain type isso é feito na classe game 

                switch (m_ri.getTipo())
                {
                    case RoomInfo.ROOM_INFO_TYPE.STROKE:
                        m_pGame = new Versus(v_sessions, m_ri, rv, m_ri.channel_rookie);
                        break;
                    case RoomInfo.ROOM_INFO_TYPE.MATCH:
                        m_pGame = new Match(v_sessions, m_ri, rv, m_ri.channel_rookie, m_teans);
                        break;
                    case RoomInfo.ROOM_INFO_TYPE.PANG_BATTLE: // Ainda não está feio, usa o  Versus Normal
                        m_pGame = new PangBattle(v_sessions, m_ri, rv, m_ri.channel_rookie);
                        break;
                    case RoomInfo.ROOM_INFO_TYPE.APPROCH:
                        m_pGame = new Approach(v_sessions, m_ri, rv, m_ri.channel_rookie);
                        break;
                    case RoomInfo.ROOM_INFO_TYPE.PRACTICE:
                        m_pGame = new Practice(v_sessions, m_ri, rv, m_ri.channel_rookie);
                        break;
                    case RoomInfo.ROOM_INFO_TYPE.TOURNEY:
                        m_pGame = new Tourney(v_sessions, m_ri, rv, m_ri.channel_rookie);
                        break;
                    case RoomInfo.ROOM_INFO_TYPE.SPECIAL_SHUFFLE_COURSE:
                        m_pGame = new SpecialShuffleCourse(v_sessions, m_ri, rv, m_ri.channel_rookie);
                        break;
                    case RoomInfo.ROOM_INFO_TYPE.GUILD_BATTLE:
                        m_pGame = new GuildBattle(v_sessions, m_ri, rv, m_ri.channel_rookie, m_guild_manager);
                        break;
                    case RoomInfo.ROOM_INFO_TYPE.GRAND_ZODIAC_PRACTICE:
                        m_pGame = new ChipInPractice(v_sessions, m_ri, rv, m_ri.channel_rookie);
                        break;
                    case RoomInfo.ROOM_INFO_TYPE.GRAND_ZODIAC_INT:
                    case RoomInfo.ROOM_INFO_TYPE.GRAND_ZODIAC_ADV:
                        m_pGame = new GrandZodiac(v_sessions, m_ri, rv, m_ri.channel_rookie);
                        break;
                    default:
                        m_pGame = new Practice(v_sessions, m_ri, rv, m_ri.channel_rookie);
                        break;
                }

                // Update Room State
                m_ri.state = 0; // IN GAME

                p.init_plain(0x230);

                packet_func.room_broadcast(this,
                    p, 1);

                p.init_plain(0x231);

                packet_func.room_broadcast(this,
                    p, 1);

                p.init_plain(0x77);

                p.WriteUInt32((uint)sgs.gs.getInstance().getInfo().rate.pang); // Rate Pang

                packet_func.room_broadcast(this,
                    p, 1);

                m_room_log.roomId = Guid.Empty;//seta toda vez que inicia sala

                //insert dados do player
                foreach (var _sessions in v_sessions)
                    CreateRoomLogSql(_sessions);//criar de todos

                //set room 
                _session.m_room = this;
                //set game 
                _session.m_pGame = m_pGame;
            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[room::requestStartGame] [Error] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Error
                p.init_plain(0x253);

                p.WriteUInt32((ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) == STDA_ERROR_TYPE.ROOM) ? ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) : 0x5900200);
                packet_func.session_send(p,
                    _session, 1);

                ret = false; // Error ao inicializar o Jogo
            }

            return ret;
        }

        public void requestInitHole(Player _session, packet _packet)
        {
            if (!_session.getState())
            {
                throw new exception("[room::requestInitHole] [Error] player nao esta connectado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }
            if (_packet == null)
            {
                throw new exception("[room::requestInitHole] [Error] _packet is null", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }

            try
            {

                if (m_pGame == null)
                {
                    throw new exception("[room::requestInitHole] [Error] PLAYER[UID=" + _session.m_pi.uid + "] tentou inicializer o hole do jogo na sala[NUMERO=" + m_ri.numero + "], mas a sala nao tem nenhum jogo inicializado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                        1, 0x5200201));
                }

                m_pGame.requestInitHole(_session, _packet);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[room::requestInitHole][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public bool requestFinishLoadHole(Player _session, packet _packet)
        {
            if (!_session.getState())
            {
                throw new exception("[room::requestFinishLoadHole] [Error] player nao esta connectado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }
            if (_packet == null)
            {
                throw new exception("[room::requestFinishLoadHole] [Error] _packet is null", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }

            bool ret = false;

            try
            {

                if (m_pGame == null)
                {
                    throw new exception("[room::requestFinishLoadHole] [Error] PLAYER[UID=" + _session.m_pi.uid + "] tentou finalizar carregamento do hole do jogo na sala[NUMERO=" + m_ri.numero + "], mas a sala nao tem nenhum jogo inicializado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                        1, 0x5200301));
                }

                ret = m_pGame.requestFinishLoadHole(_session, _packet);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[room::requestFinishLoadHole][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            return ret;
        }

        public void requestFinishCharIntro(Player _session, packet _packet)
        {
            if (!_session.getState())
            {
                throw new exception("[room::requestFinishCharIntro] [Error] player nao esta connectado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }
            if (_packet == null)
            {
                throw new exception("[room::requestFinishCharIntro] [Error] _packet is null", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }

            try
            {

                if (m_pGame == null)
                {
                    throw new exception("[room::requestFinishCharIntro] [Error] PLAYER[UID=" + _session.m_pi.uid + "] tentou finalizar Char Intro do jogo na sala[NUMEROR=" + m_ri.numero + "], mas a sala nao tem nenhum jogo inicializado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                        1, 0x5200401));
                }

                m_pGame.requestFinishCharIntro(_session, _packet);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[room::requestFinishCharIntro][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestFinishHoleData(Player _session, packet _packet)
        {
            if (!_session.getState())
            {
                throw new exception("[room::requestFinishHoleData] [Error] player nao esta connectado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }
            if (_packet == null)
            {
                throw new exception("[room::requestFinishHoleData] [Error] _packet is null", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }

            try
            {

                if (m_pGame == null)
                {
                    throw new exception("[room::requestFinishHoleData] [Error] PLAYER[UID=" + _session.m_pi.uid + "] tentou finalizar dados do hole, no jogo na sala[NUMERO=" + m_ri.numero + "], mas a sala nao tem nenhum jogo inicializado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                        1, 0x5201901));
                }

                m_pGame.requestFinishHoleData(_session, _packet);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[room::requestFinishHoleData][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        // Server enviou a resposta do InitShot para o cliente
        public void requestInitShotSended(Player _session, packet _packet)
        {
            if (!_session.getState())
            {
                throw new exception("[room::requestInitShotSended] [Error] player nao esta connectado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }
            if (_packet == null)
            {
                throw new exception("[room::requestInitShotSended] [Error] _packet is null", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }

            try
            {

                if (m_pGame == null)
                {
                    throw new exception("[room::requestInitShotSended] [Error] PLAYER[UID=" + _session.m_pi.uid + "] o server enviou o pacote de InitShot para o cliente, mas na sala[NUMERO=" + m_ri.numero + "] nao tem mais nenhum jogo inicializado. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                        1, 0x5905001));
                }

                m_pGame.requestInitShotSended(_session, _packet);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[room::requestInitShotSended][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestInitShot(Player _session, packet _packet)
        {
            if (!_session.getState())
            {
                throw new exception("[room::requestInitShot] [Error] player nao esta connectado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }
            if (_packet == null)
            {
                throw new exception("[room::requestInitShot] [Error] _packet is null", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }

            try
            {

                if (m_pGame == null)
                {
                    throw new exception("[room::requestInitShot] [Error] PLAYER[UID=" + _session.m_pi.uid + "] tentou inicializar o shot no jogo na sala[NUMERO=" + m_ri.numero + "], mas a sala nao tem nenhum jogo inicializado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                        1, 0x5201401));
                }

                m_pGame.requestInitShot(_session, _packet);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[room::requestInitShot][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestSyncShot(Player _session, packet _packet)
        {
            if (!_session.getState())
            {
                throw new exception("[room::requestSyncShot] [Error] player nao esta connectado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }
            if (_packet == null)
            {
                throw new exception("[room::requestSyncShot] [Error] _packet is null", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }

            try
            {

                if (m_pGame == null)
                {
                    throw new exception("[room::requestSyncShot] [Error] PLAYER[UID=" + _session.m_pi.uid + "] tentou sincronizar tacada no jogo na sala[NUMERO=" + m_ri.numero + "], mas a sala nao tem nenhum jogo inicializado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                        1, 0x5201501));
                }

                m_pGame.requestSyncShot(_session, _packet);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[room::requestSyncShot][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestInitShotArrowSeq(Player _session, packet _packet)
        {
            if (!_session.getState())
            {
                throw new exception("[room::requestInitShotArrowSeq] [Error] player nao esta connectado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }
            if (_packet == null)
            {
                throw new exception("[room::requestInitShotArrowSeq] [Error] _packet is null", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }

            try
            {

                if (m_pGame == null)
                {
                    throw new exception("[room::requestInitShotArrowSeq] [Error] PLAYER[UID=" + _session.m_pi.uid + "] tentou inicializar a sequencia de setas no jogo na sala[NUMERO=" + m_ri.numero + "], mas a sala nao tem nenhum jogo inicializado, Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                        1, 0x5201601));
                }

                m_pGame.requestInitShotArrowSeq(_session, _packet);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[room::requestInitShotArrowSeq][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestShotEndData(Player _session, packet _packet)
        {
            if (!_session.getState())
            {
                throw new exception("[room::requestShotEndData] [Error] player nao esta connectado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }
            if (_packet == null)
            {
                throw new exception("[room::requestShotEndData] [Error] _packet is null", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }

            try
            {

                if (m_pGame == null)
                {
                    throw new exception("[room::requestShotEndData] [Error] PLAYER[UID=" + _session.m_pi.uid + "] tentou finalizar local da tacada no jogo na sala[NUMERO=" + m_ri.numero + "], mas a sala nao tem nenhum jogo inicializado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                        1, 0x5201701));
                }

                m_pGame.requestShotEndData(_session, _packet);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[room::requestShotEndData][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public RetFinishShot requestFinishShot(Player _session, packet _packet)
        {
            if (!_session.getState())
            {
                throw new exception("[room::requestFinishShot] [Error] player nao esta connectado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }
            if (_packet == null)
            {
                throw new exception("[room::requestFinishShot] [Error] _packet is null", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }

            RetFinishShot rfs = new RetFinishShot();

            try
            {

                if (m_pGame == null)
                {
                    throw new exception("[room::requestFinishShot] [Error] PLAYER[UID=" + _session.m_pi.uid + "] tentou finalizar tacada no jogo na sala[NUMERO=" + m_ri.numero + "], mas a sala nao tem nenhum jogo inicializado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                        1, 0x5201801));
                }

                rfs = m_pGame.requestFinishShot(_session, _packet);

                if (rfs.ret > 0)
                {

                    // Acho que não usa mais isso então vou deixar ai, e o ret == 2 vou deixar no channel,
                    // por que se ele for o ultimo da sala tem que excluir ela
                    if (rfs.ret == 1)
                    {
                        finish_game();
                    }
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[room::requestFinishShot][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            return (rfs);
        }

        public void requestChangeMira(Player _session, packet _packet)
        {
            if (!_session.getState())
            {
                throw new exception("[room::requestChangeMira] [Error] player nao esta connectado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }
            if (_packet == null)
            {
                throw new exception("[room::requestChangeMira] [Error] _packet is null", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }

            try
            {

                if (m_pGame == null)
                {
                    throw new exception("[room::requestChangeMira] [Error] PLAYER[UID=" + _session.m_pi.uid + "] tentou trocar a mira no jogo na sala[NUMERO=" + m_ri.numero + "], mas a sala nao tem nenhum jogo inicializado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                        1, 0x5200501));
                }

                m_pGame.requestChangeMira(_session, _packet);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[room::requestChangeMira][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestChangeStateBarSpace(Player _session, packet _packet)
        {
            if (!_session.getState())
            {
                throw new exception("[room::requestChangeStateBarSpace] [Error] player nao esta connectado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }
            if (_packet == null)
            {
                throw new exception("[room::requestChangeStateBarSpace] [Error] _packet is null", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }

            try
            {

                if (m_pGame == null)
                {
                    throw new exception("[room::requestChangeStateBarSpace] [Error] PLAYER[UID=" + _session.m_pi.uid + "] tentou trocar estado da barra de espaco no jogo na sala[NUMERO=" + m_ri.numero + "], mas a sala nao tem nenhum jogo inicializado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                        1, 0x5200601));
                }

                m_pGame.requestChangeStateBarSpace(_session, _packet);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[room::requestChangeStateBarSpace][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestActivePowerShot(Player _session, packet _packet)
        {
            if (!_session.getState())
            {
                throw new exception("[room::requestActivePowerShot] [Error] player nao esta connectado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }
            if (_packet == null)
            {
                throw new exception("[room::requestActivePowerShot] [Error] _packet is null", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }

            try
            {

                if (m_pGame == null)
                {
                    throw new exception("[room::requestActivePowerShot] [Error] PLAYER[UID=" + _session.m_pi.uid + "] tentou ativar power shot no jogo na sala[NUMEROR=" + m_ri.numero + "], mas a sala nao tem nenhum jogo inicializado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                        1, 0x5200701));
                }

                m_pGame.requestActivePowerShot(_session, _packet);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[room::requestActivePowerShot][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestChangeClub(Player _session, packet _packet)
        {
            if (!_session.getState())
            {
                throw new exception("[room::requestChangeClub] [Error] player nao esta connectado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }
            if (_packet == null)
            {
                throw new exception("[room::requestChangeClub] [Error] _packet is null", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }

            try
            {

                if (m_pGame == null)
                {
                    throw new exception("[room::requestChangeClub] [Error] PLAYER[UID=" + _session.m_pi.uid + "] tentou trocar taco no jogo na sala[NUMERO=" + m_ri.numero + "], mas a sala nao tem nenhum jogo inicializado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                        1, 0x5200801));
                }

                m_pGame.requestChangeClub(_session, _packet);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[room::requestChangeClub][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestUseActiveItem(Player _session, packet _packet)
        {
            if (!_session.getState())
            {
                throw new exception("[room::requestUseActiveItem] [Error] player nao esta connectado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }
            if (_packet == null)
            {
                throw new exception("[room::requestUseActiveItem] [Error] _packet is null", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }

            try
            {

                if (m_pGame == null)
                {
                    throw new exception("[room::requestUseActiveItem] [Error] PLAYER[UID=" + _session.m_pi.uid + "] tentou usar active item no jogo na sala[NUMERO=" + m_ri.numero + "], mas a sala nao tem nenhum jogo inicializado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                        1, 0x5200901));
                }

                m_pGame.requestUseActiveItem(_session, _packet);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[room::requestUseActiveItem][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestChangeStateTypeing(Player _session, packet _packet)
        {
            if (!_session.getState())
            {
                throw new exception("[room::requestChangeStateTypeing] [Error] player nao esta connectado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }
            if (_packet == null)
            {
                throw new exception("[room::requestChangeStateTypeing] [Error] _packet is null", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }

            try
            {

                if (m_pGame == null)
                    return;

                m_pGame.requestChangeStateTypeing(_session, _packet);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[room::requestChangeStateTypeing][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestMoveBall(Player _session, packet _packet)
        {
            if (!_session.getState())
            {
                throw new exception("[room::requestMoveBall] [Error] player nao esta connectado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }
            if (_packet == null)
            {
                throw new exception("[room::requestMoveBall] [Error] _packet is null", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }

            try
            {

                if (m_pGame == null)
                {
                    throw new exception("[room::requestMoveBall] [Error] PLAYER[UID=" + _session.m_pi.uid + "] tentou recolocar a bola no jogo na sala[NUMERO=" + m_ri.numero + "], mas a sala nao tem nenhum jogo inicializado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                        1, 0x5201101));
                }

                m_pGame.requestMoveBall(_session, _packet);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[room::requestMoveBall][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestChangeStateChatBlock(Player _session, packet _packet)
        {
            if (!_session.getState())
            {
                throw new exception("[room::requestChangeStateChatBlock] [Error] player nao esta connectado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }
            if (_packet == null)
            {
                throw new exception("[room::requestChangeStateChatBlock] [Error] _packet is null", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }

            try
            {

                if (m_pGame == null)
                {
                    throw new exception("[room::requestChangeStateChatBlock] [Error] PLAYER[UID=" + _session.m_pi.uid + "] tentou mudar estado do chat block no jogo na sala[NUMERO=" + m_ri.numero + "], mas a sala nao tem nenhum jogo inicializado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                        1, 0x5201201));
                }

                m_pGame.requestChangeStateChatBlock(_session, _packet);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[room::requestChangeStateChatBlock][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestActiveBooster(Player _session, packet _packet)
        {
            if (!_session.getState())
            {
                throw new exception("[room::requestActiveBooster] [Error] player nao esta connectado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }
            if (_packet == null)
            {
                throw new exception("[room::requestActiveBooster] [Error] _packet is null", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }

            try
            {

                if (m_pGame == null)
                {
                    throw new exception("[room::requestActiveBooster] [Error] PLAYER[UID=" + _session.m_pi.uid + "] tentou ativar time booster no jogo na sala[NUMERO=" + m_ri.numero + "], mas a sala nao tem nenhum jogo inicializado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                        1, 0x5201301));
                }

                m_pGame.requestActiveBooster(_session, _packet);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[room::requestActiveBooster][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestActiveReplay(Player _session, packet _packet)
        {
            if (!_session.getState())
            {
                throw new exception("[room::requestActiveReplay] [Error] player nao esta connectado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }
            if (_packet == null)
            {
                throw new exception("[room::requestActiveReplay] [Error] _packet is null", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }

            try
            {

                if (m_pGame == null)
                {
                    throw new exception("[room::requestActiveReplay] [Error] PLAYER[UID=" + _session.m_pi.uid + "] tentou ativar Replay no jogo na sala[NUMERO=" + m_ri.numero + "], mas a sala nao tem nenhum jogo inicializado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                        1, 0x5301001));
                }

                m_pGame.requestActiveReplay(_session, _packet);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[room::requestActiveReplay][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestActiveCutin(Player _session, packet _packet)
        {
            if (!_session.getState())
            {
                throw new exception("[room::requestActiveCutin] [Error] player nao esta connectado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }
            if (_packet == null)
            {
                throw new exception("[room::requestActiveCutin] [Error] _packet is null", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }

            try
            {

                if (m_pGame == null)
                {
                    throw new exception("[room::requestActiveCutin] [Error] PLAYER[UID=" + _session.m_pi.uid + "] tentou ativar cutin no jogo na sala[NUMERO=" + m_ri.numero + "], mas a sala nao tem nenhum jogo inicializado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                        1, 0x5201701));
                }

                m_pGame.requestActiveCutin(_session, _packet);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[room::requestActiveCutin][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestActiveAutoCommand(Player _session, packet _packet)
        {
            if (!_session.getState())
            {
                throw new exception("[room::requestActiveAutoCommand] [Error] player nao esta connectado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }
            if (_packet == null)
            {
                throw new exception("[room::requestActiveAutoCommand] [Error] _packet is null", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }

            try
            {

                if (m_pGame == null)
                {
                    throw new exception("[room::requestActiveAutoCommand] [Error] PLAYER[UID=" + _session.m_pi.uid + "] tentou ativar var Command no jogo na sala[NUMEROR=" + m_ri.numero + "], mas a sala nao tem nenhum jogo inicializado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                        1, 0x550001));
                }

                m_pGame.requestActiveAutoCommand(_session, _packet);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[room::requestActiveAutoCommand][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestActiveAssistGreen(Player _session, packet _packet)
        {
            if (!_session.getState())
            {
                throw new exception("[room::requestActiveAssistGreen] [Error] player nao esta connectado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }
            if (_packet == null)
            {
                throw new exception("[room::requestActiveAssistGreen] [Error] _packet is null", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }

            try
            {

                if (m_pGame == null)
                {
                    throw new exception("[room::requestActiveAssistGreen] [Error] PLAYER[UID=" + _session.m_pi.uid + "] tentou ativar Assist Green no jogo na sala[NUMERO=" + m_ri.numero + "], mas a sala nao tem nenhum jogo inicializado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                        1, 0x5201801));
                }

                m_pGame.requestActiveAssistGreen(_session, _packet);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[room::requestActiveAssistGreen][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        // VersusBase
        public void requestLoadGamePercent(Player _session, packet _packet)
        {
            if (!_session.getState())
            {
                throw new exception("[room::requestLoadGamePercent] [Error] player nao esta connectado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }
            if (_packet == null)
            {
                throw new exception("[room::requestLoadGamePercent] [Error] _packet is null", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }

            try
            {

                if (m_pGame == null)
                {
                    throw new exception("[room::requestLoadGamePercent] [Error] PLAYER[UID=" + _session.m_pi.uid + "] tentou mandar a porcentagem carregada do jogo na sala[NUMERO=" + m_ri.numero + "], mas a sala nao tem nenhum jogo inicializado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                        1, 0x551001));
                }

                m_pGame.requestLoadGamePercent(_session, _packet);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[room::requestLoadGamePercent][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestMarkerOnCourse(Player _session, packet _packet)
        {
            if (!_session.getState())
            {
                throw new exception("[room::requestMarkerOnCourse] [Error] player nao esta connectado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }
            if (_packet == null)
            {
                throw new exception("[room::requestMarkerOnCourse] [Error] _packet is null", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }

            try
            {

                if (m_pGame == null)
                {
                    throw new exception("[room::requestMarkerOnCourse] [Error] PLAYER[UID=" + _session.m_pi.uid + "] tentou marcar no course no jogo na sala[NUMERO=" + m_ri.numero + "], mas a sala nao tem nenhum jogo inicializado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                        1, 0x552001));
                }

                m_pGame.requestMarkerOnCourse(_session, _packet);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[room::requestMarkerOnCourse][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestStartTurnTime(Player _session, packet _packet)
        {
            if (!_session.getState())
            {
                throw new exception("[room::requestStartTurnTime] [Error] player nao esta connectado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }
            if (_packet == null)
            {
                throw new exception("[room::requestStartTurnTime] [Error] _packet is null", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }

            try
            {

                if (m_pGame == null)
                {
                    throw new exception("[room::requestStartTurnTime] [Error] PLAYER[UID=" + _session.m_pi.uid + "] tentou comecar o tempo do turno no jogo na sala[NUMERO=" + m_ri.numero + "], mas a sala nao tem nenhum jogo inicializado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                        1, 0x553001));
                }

                m_pGame.requestStartTurnTime(_session, _packet);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[room::requestStartTurnTime][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestUnOrPauseGame(Player _session, packet _packet)
        {
            if (!_session.getState())
            {
                throw new exception("[room::requestUnOrPauseGame] [Error] player nao esta connectado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }
            if (_packet == null)
            {
                throw new exception("[room::requestUnOrPauseGame] [Error] _packet is null", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }

            try
            {

                if (m_pGame == null)
                {
                    throw new exception("[room::requestUnOrPauseGame] [Error] PLAYER[UID=" + _session.m_pi.uid + "] tentou pausar ou despausar o jogo na sala[NUMERO=" + m_ri.numero + "], mas a sala nao tem nenhum jogo inicializado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                        1, 0x554001));
                }

                m_pGame.requestUnOrPause(_session, _packet);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[room::requestUnOrPauseGame][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public bool requestLastPlayerFinishVersus(Player _session, packet _packet)
        {
            if (!_session.getState())
            {
                throw new exception("[room::requestLastPlayerFinishVersus] [Error] player nao esta connectado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }
            if (_packet == null)
            {
                throw new exception("[room::requestLastPlayerFinishVersus] [Error] _packet is null", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }

            bool ret = false;

            try
            {

                if (m_pGame == null)
                {
                    throw new exception("[room::requestLastPlayerFinishVersus] [Error] PLAYER[UID=" + _session.m_pi.uid + "] tentou finalizar Versus na sala[NUMERO=" + m_ri.numero + "], mas a sala nao tem nenhum jogo inicializado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                        1, 0x555001));
                }

                // Finaliza o Versus
                if (m_pGame.getSessions().Count() > 0)
                {

                    if (m_pGame.finish_game(m_pGame.getSessions().FirstOrDefault(), 2))
                    {
                        finish_game();
                    }

                }
                else
                {
                    finish_game();
                }

                ret = true;

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[room::requestLastPlayerFinishVersus][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            return ret;
        }

        public bool requestReplyContinueVersus(Player _session, packet _packet)
        {
            if (!_session.getState())
            {
                throw new exception("[room::requestReplyContinueVersus] [Error] player nao esta connectado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }
            if (_packet == null)
            {
                throw new exception("[room::requestReplyContinueVersus] [Error] _packet is null", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }

            bool ret = false;

            try
            {

                byte opt = _packet.ReadByte();

                if (m_pGame == null)
                {
                    throw new exception("[room::requestReplyContinueVersus] [Error] PLAYER[UID=" + _session.m_pi.uid + "] tentou responder se quer continuar o versus ou nao na sala[NUMERO=" + m_ri.numero + "], mas a sala nao tem nenhum jogo inicializado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                        1, 0x556001));
                }

                if (opt == 0)
                {

                    if (m_pGame.getSessions().Count() > 0)
                    {

                        if (m_pGame.finish_game(m_pGame.getSessions().First(), 2))
                        {
                            finish_game();
                        }

                    }
                    else
                    {
                        finish_game();
                    }

                    ret = true;

                }
                else if (opt == 1)
                {
                    m_pGame.requestReplyContinue();
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[room::requestReplyContinueVersus][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            return ret;
        }

        // Match
        public void requestTeamFinishHole(Player _session, packet _packet)
        {
            if (!_session.getState())
            {
                throw new exception("[room::requestTeamFinishHole] [Error] player nao esta connectado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }
            if (_packet == null)
            {
                throw new exception("[room::requestTeamFinishHole] [Error] _packet is null", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }

            try
            {

                if (m_pGame == null)
                {
                    throw new exception("[room::requestTeamFinishHole] [Error] PLAYER[UID=" + _session.m_pi.uid + "] tentou finalizar hole do Match na sala[NUMERO=" + m_ri.numero + "], mas a sala nao tem nenhum jogo inicializado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                        1, 0x562001));
                }

                m_pGame.requestTeamFinishHole(_session, _packet);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[room::requestTeamFinishHole][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void testeDegree()
        {

            try
            {

                if (m_pGame == null)
                {
                    throw new exception("[room::testeDegree] [Error] sala[NUMERO=" + m_ri.numero + "] nao tem um jogo inicializado. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                        2000, 0));
                }

                var pgi = m_pGame.getPlayerInfo(v_sessions.First());

                if (pgi == null)
                {
                    throw new exception("[room::testeDegree] [Error] o jogo na sala[NUMERO=" + m_ri.numero + "], nao tem o PLAYER[UID=" + Convert.ToString((v_sessions.First()).m_pi.uid) + "]. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                        2001, 0));
                }

                pgi.degree += 1;

                pgi.degree %= LIMIT_DEGREE;

#if DEBUG
                // Log
                _smp.message_pool.getInstance().push(new message("[room::testeDegree][Info] PLAYER[UID=" + Convert.ToString(pgi.uid) + "] Vento[Graus=" + Convert.ToString(pgi.degree) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));
#endif // _DEBUG

                PangyaBinaryWriter p = new PangyaBinaryWriter((ushort)0x5B);

                p.WriteByte(1);
                p.WriteByte(0); // Flag de card de vento, aqui é a qnd diminui o vento, 1 Vento azul
                p.WriteUInt16(pgi.degree);
                p.WriteByte(1); // Flag do vento, 1 Reseta o Vento, 0 soma o vento que nem o comando gm \wind do pangya original

                packet_func.room_broadcast(this,
                    p, 1);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[room::testeDegree][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        // Practice
        public void requestLeavePractice(Player _session, packet _packet)
        {
            if (!_session.getState())
            {
                throw new exception("[room::requestLeavePractice] [Error] player nao esta connectado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }
            if (_packet == null)
            {
                throw new exception("[room::requestLeavePractice] [Error] _packet is null", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }

            try
            {

                if (m_pGame == null)
                {
                    throw new exception("[room::requestLeavePractice] [Error] PLAYER[UID=" + _session.m_pi.uid + "] tentou sair do Practice na sala[NUMERO=" + m_ri.numero + "], mas a sala nao tem nenhum jogo inicializado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                        1, 0x6202001));
                }

                if (m_ri.getTipo() != RoomInfo.ROOM_INFO_TYPE.PRACTICE)
                {
                    throw new exception("[room::requestLeavePractice] [Error] PLAYER[UID=" + _session.m_pi.uid + "] tentou sair do Practice na sala[NUMERO=" + m_ri.numero + "], mas TIPO=" + Convert.ToString((ushort)m_ri.tipo) + " de jogo da sala nao é Practice", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                        2, 0x6202002));
                }

                // Acabou o tempo /*Sai do Practice*/
                ((TourneyBase)(m_pGame)).timeIsOver();
            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[room::requestLeavePractice][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        // Tourney
        public bool requestUseTicketReport(Player _session, packet _packet)
        {
            if (!_session.getState())
            {
                throw new exception("[room::requestUseTicketReport] [Error] player nao esta connectado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }
            if (_packet == null)
            {
                throw new exception("[room::requestUseTicketReport] [Error] _packet is null", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }

            bool ret = false;

            try
            {

                if (m_pGame == null)
                {
                    throw new exception("[room::requestUseTicketReport] [Error] PLAYER[UID=" + _session.m_pi.uid + "] tentou usar Ticket Report no Tourney no jogo na sala[NUMERO=" + m_ri.numero + "], mas a sala nao tem nenhum jogo inicializado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                        1, 0x6301001));
                }

                ret = m_pGame.requestUseTicketReport(_session, _packet);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[room::requestUseTicketReport][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            return ret;
        }

        // Grand Zodiac
        public void requestLeaveChipInPractice(Player _session, packet _packet)
        {
            if (!_session.getState())
            {
                throw new exception("[room::requestLeaveChipInPractice] [Error] player nao esta connectado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }
            if (_packet == null)
            {
                throw new exception("[room::requestLeaveChipInPractice] [Error] _packet is null", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }

            try
            {

                if (m_pGame == null)
                {
                    throw new exception("[room::requestLeaveChipInPractice] [Error] PLAYER[UID=" + _session.m_pi.uid + "] tentou sair do Chip-in Practice na sala[NUMERO=" + m_ri.numero + "], mas a sala nao tem nenhum jogo inicializado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                        1, 0x6701001));
                }

                if (m_ri.getTipo() != RoomInfo.ROOM_INFO_TYPE.GRAND_ZODIAC_PRACTICE)
                {
                    throw new exception("[room::requestLeaveChipInPractice] [Error] PLAYER[UID=" + _session.m_pi.uid + "] tentou sair do Chip-in Practice na sala[NUMERO=" + m_ri.numero + "], mas TIPO=" + Convert.ToString((ushort)m_ri.tipo) + " de jogo da sala nao é Chip-in Practice", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                        2, 0x6701002));
                }

                // Acabou o tempo /*Sai do Chip-in Practice*/
                if (m_pGame.finish_game(_session, 2))
                {
                    finish_game();
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[room::requestLeaveChipInPractice][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestStartFirstHoleGrandZodiac(Player _session, packet _packet)
        {
            if (!_session.getState())
            {
                throw new exception("[room::requestStartFirstHoleGrandZodiac] [Error] player nao esta connectado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }
            if (_packet == null)
            {
                throw new exception("[room::requestStartFirstHoleGrandZodiac] [Error] _packet is null", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }

            try
            {

                if (m_pGame == null)
                {
                    throw new exception("[room::requestStartFisrtHoleGrandZodiac] [Error] PLAYER[UID=" + _session.m_pi.uid + "] tentou comecar o primeiro hole do Grand Zodiac game na sala[NUMERO=" + m_ri.numero + "], mas a sala nao tem nenhum jogo inicializado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                        1, 0x6702001));
                }

                m_pGame.requestStartFirstHoleGrandZodiac(_session, _packet);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[room::requestStartFirstHoleGrandZodiac][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestReplyInitialValueGrandZodiac(Player _session, packet _packet)
        {
            if (!_session.getState())
            {
                throw new exception("[room::requestReplyInitialValueGrandZodiac] [Error] player nao esta connectado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }
            if (_packet == null)
            {
                throw new exception("[room::requestReplyInitialValueGrandZodiac] [Error] _packet is null", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }

            try
            {

                if (m_pGame == null)
                {
                    throw new exception("[room::requestReplyInitialValueGrandZodiac] [Error] PLAYER[UID=" + _session.m_pi.uid + "] tentou reponder o valor inicial do Grand Zodiac game na sala[NUMERO=" + m_ri.numero + "], mas a sala nao tem nenhum jogo inicializado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                        1, 0x6703001));
                }

                m_pGame.requestReplyInitialValueGrandZodiac(_session, _packet);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[room::requestReplyInitialValueGrandZodiac][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        // Ability Item
        public void requestActiveRing(Player _session, packet _packet)
        {
            if (!_session.getState())
            {
                throw new exception("[room::requestActiveRing] [Error] player nao esta connectado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }
            if (_packet == null)
            {
                throw new exception("[room::requestActiveRing] [Error] _packet is null", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }

            try
            {

                if (m_pGame == null)
                {
                    throw new exception("[room::requestActiveRing] [Error] PLAYER[UID=" + _session.m_pi.uid + "] tentou ativar Anel no jogo na sala[NUMERO=" + m_ri.numero + "], mas a sala nao tem nenhum jogo inicializado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                        1, 0x6201001));
                }

                m_pGame.requestActiveRing(_session, _packet);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[room::requestActiveRing][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestActiveRingGround(Player _session, packet _packet)
        {
            if (!_session.getState())
            {
                throw new exception("[room::requestActiveRingGround] [Error] player nao esta connectado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }
            if (_packet == null)
            {
                throw new exception("[room::requestActiveRingGround] [Error] _packet is null", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }

            try
            {

                if (m_pGame == null)
                {
                    throw new exception("[room::requestActiveRingGround] [Error] PLAYER[UID=" + _session.m_pi.uid + "] tentou ativar Anel de Terreno no jogo na sala[NUMERO=" + m_ri.numero + "], mas a sala nao tem nenhum jogo inicializado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                        1, 0x6201101));
                }

                m_pGame.requestActiveRingGround(_session, _packet);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[room::requestActiveRingGround][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestActiveRingPawsRainbowJP(Player _session, packet _packet)
        {
            if (!_session.getState())
            {
                throw new exception("[room::requestActiveRingPawsRainbowJP] [Error] player nao esta connectado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }
            if (_packet == null)
            {
                throw new exception("[room::requestActiveRingPawsRainbowJP] [Error] _packet is null", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }

            try
            {

                if (m_pGame == null)
                {
                    throw new exception("[room::requestActiveRingPawsRainbowJP] [Error] PLAYER[UID=" + _session.m_pi.uid + "] tentou ativar Anel Patinha Arco-iris no jogo na sala[NUMERO=" + m_ri.numero + "], mas a sala nao tem nenhum jogo inicializado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                        1, 0x6201201));
                }

                m_pGame.requestActiveRingPawsRainbowJP(_session, _packet);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[room::requestActiveRingPawsRainbowJP][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestActiveRingPawsRingSetJP(Player _session, packet _packet)
        {
            if (!_session.getState())
            {
                throw new exception("[room::requestActiveRingPawsRingSetJP] [Error] player nao esta connectado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }
            if (_packet == null)
            {
                throw new exception("[room::requestActiveRingPawsRingSetJP] [Error] _packet is null", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }

            try
            {

                if (m_pGame == null)
                {
                    throw new exception("[room::requestActiveRingPawsRingSetJP] [Error] PLAYER[UID=" + _session.m_pi.uid + "] tentou ativar Anel de Patinha de conjunto de Aneis [JP] no jogo na sala[NUMERO=" + m_ri.numero + "], mas a sala n ao tem nenhum jogo inicializado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                        1, 0x6201301));
                }

                m_pGame.requestActiveRingPawsRingSetJP(_session, _packet);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[room::requestActiveRingPawsRingSetJP][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestActiveRingPowerGagueJP(Player _session, packet _packet)
        {
            if (!_session.getState())
            {
                throw new exception("[room::requestActiveRingPowerGagueJP] [Error] player nao esta connectado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }
            if (_packet == null)
            {
                throw new exception("[room::requestActiveRingPowerGagueJP] [Error] _packet is null", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }

            try
            {

                if (m_pGame == null)
                {
                    throw new exception("[room::requestActiveRingPowerGagueJP] [Error] PLAYER[UID=" + _session.m_pi.uid + "] tentou ativar Anel Barra de PS [JP] no jogo na sala[NUMERO=" + m_ri.numero + "], mas a sala nao tem nenhum jogo inicializado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                        1, 0x6201401));
                }

                m_pGame.requestActiveRingPowerGagueJP(_session, _packet);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[room::requestActiveRingPowerGagueJP][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestActiveRingMiracleSignJP(Player _session, packet _packet)
        {
            if (!_session.getState())
            {
                throw new exception("[room::requestActiveRingMiracleSignJP] [Error] player nao esta connectado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }
            if (_packet == null)
            {
                throw new exception("[room::requestActiveRingMiracleSignJP] [Error] _packet is null", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }

            try
            {

                if (m_pGame == null)
                {
                    throw new exception("[room::requestActiveRingMiracleSignJP] [Error] PLAYER[UID=" + _session.m_pi.uid + "] tentou ativar Anel Olho Magico [JP] no jogo na sala[NUMERO=" + m_ri.numero + "], mas a sala nao tem nenhum jogo inicializado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                        1, 0x6201501));
                }

                m_pGame.requestActiveRingMiracleSignJP(_session, _packet);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[room::requestActiveRingMiracleSignJP][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestActiveWing(Player _session, packet _packet)
        {
            if (!_session.getState())
            {
                throw new exception("[room::requestActiveWing] [Error] player nao esta connectado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }
            if (_packet == null)
            {
                throw new exception("[room::requestActiveWing] [Error] _packet is null", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }

            try
            {

                if (m_pGame == null)
                {
                    throw new exception("[room::requestActiveWing] [Error] PLAYER[UID=" + _session.m_pi.uid + "] tentou ativar Asa no joga na sala[NUMERO=" + m_ri.numero + "], mas a sala nao tem nenhum jogo inicializado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                        1, 0x6201601));
                }

                m_pGame.requestActiveWing(_session, _packet);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[room::requestActiveWing][ErrorSystem] " + e.getMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestActivePaws(Player _session, packet _packet)
        {
            if (!_session.getState())
            {
                throw new exception("[room::requestActivePaws] [Error] player nao esta connectado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }
            if (_packet == null)
            {
                throw new exception("[room::requestActivePaws] [Error] _packet is null", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }

            try
            {

                if (m_pGame == null)
                {
                    throw new exception("[room::requestActivePaws] [Error] PLAYER[UID=" + _session.m_pi.uid + "] tentou ativar Patinha no jogo na sala[NUMERO=" + m_ri.numero + "], mas a sala nao tem nenhum jogo inicializado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                        1, 0x6201701));
                }

                m_pGame.requestActivePaws(_session, _packet);

            }
            catch (exception e)
            {
                _smp.message_pool.getInstance().push(new message("[room::requestActivePaws][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestActiveGlove(Player _session, packet _packet)
        {
            if (!_session.getState())
            {
                throw new exception("[room::requestActiveGlove] [Error] player nao esta connectado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }
            if (_packet == null)
            {
                throw new exception("[room::requestActiveGlove] [Error] _packet is null", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }

            try
            {

                if (m_pGame == null)
                {
                    throw new exception("[room::requestActiveGlove] [Error] PLAYER[UID=" + _session.m_pi.uid + "] tentou ativat Luva 1m no jogo na sala[NUMERO=" + m_ri.numero + "], mas a sala nao tem nenhum jogo inicializado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                        1, 0x6201801));
                }

                m_pGame.requestActiveGlove(_session, _packet);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[room::requestActiveGlove][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestActiveEarcuff(Player _session, packet _packet)
        {
            if (!_session.getState())
            {
                throw new exception("[room::requestActiveEarcuff] [Error] player nao esta connectado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }
            if (_packet == null)
            {
                throw new exception("[room::requestActiveEarcuff] [Error] _packet is null", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }

            try
            {

                if (m_pGame == null)
                {
                    throw new exception("[room::requestActiveEarcuff] [Error] PLAYER[UID=" + _session.m_pi.uid + "] tentou ativar Earcuff no jogo na sala[NUMERO=" + m_ri.numero + "], mas a sala nao tem nenhum jogo inicializado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                        1, 0x6201901));
                }

                m_pGame.requestActiveEarcuff(_session, _packet);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[room::requestActiveEarcuff][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestSendTimeGame(Player _session)
        {
            if (!_session.getState())
            {
                throw new exception("[room::requestSendTimeGame] [Error] player nao esta connectado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }

            PangyaBinaryWriter p = new PangyaBinaryWriter();

            try
            {
                if (isKickedPlayer(_session.m_pi.uid))
                {
                    throw new exception("[room::requestSendTimeGame] [Error] PLAYER[UID=" + _session.m_pi.uid + "] tentou entrar na sala[NUMERO=" + m_ri.numero + "] ja em jogo, mas o player foi chutado da sala antes de comecar o jogo.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                        2704, 7));
                }

                if (m_pGame == null)
                {
                    throw new exception("[room::requestSendTimeGame] [Error] PLAYER[UID=" + _session.m_pi.uid + "] tentou pegar o tempo do tourney que comecou na sala[NUMERO=" + m_ri.numero + "], mas a sala nao tem nenhum jogo inicializado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                        2705, 1));
                }

                m_pGame.requestSendTimeGame(_session);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[room::requestSendTimeGame][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Resposta erro
                p.init_plain(0x113);

                p.WriteByte(6); // Option Error

                // Error Code
                p.WriteByte((byte)((ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) == STDA_ERROR_TYPE.ROOM) ? ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) : 1));

                packet_func.session_send(p,
                    _session, 1);
            }
        }

        public bool requestEnterGameAfterStarted(Player _session)
        {
            if (!_session.getState())
            {
                throw new exception("[room::requestEnterGameAfterStarted] [Error] player nao esta connectado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }

            PangyaBinaryWriter p = new PangyaBinaryWriter();

            bool ret = false;

            try
            {

                if (isKickedPlayer(_session.m_pi.uid))
                {
                    throw new exception("[room::requestEnterGameAfterStarted][Warning] PLAYER[UID=" + _session.m_pi.uid + "] tentou entrar na sala[NUMERO=" + m_ri.numero + "] ja em jogo, mas o player foi chutado da sala antes de comecar o jogo.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                        2704, 7));
                }

                if (m_pGame == null)
                {
                    throw new exception("[room::requestEnterGameAfterStarted] [Error] PLAYER[UID=" + _session.m_pi.uid + "] tentou entrar na sala[NUMERO=" + m_ri.numero + "] ja em jogo, mas a sala nao tem nenhum jogo inicializado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                        2705, 1));
                }

                if (isGamingBefore(_session.m_pi.uid))
                {
                    throw new exception("[room::requestEnterGameAfterStarted][Warning] PLAYER[UID=" + _session.m_pi.uid + "] tentou entrar na sala[NUMERO=" + m_ri.numero + "] ja em jogo, mas o player ja tinha jogado nessa sala e saiu, e nao pode mais entrar.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                        2703, 6));
                }

                int64_t tempo = (m_ri.qntd_hole == 18) ? 10 * 60000 : 5 * 60000;

                var remain = UtilTime.GetLocalDateDiff(m_pGame.getTimeStart());

                if (remain > 0)
                {
                    remain /= STDA_10_MICRO_PER_MILLI; // miliseconds
                }

                if (remain >= tempo)
                {
                    throw new exception("[room::requestEnterGameAfrerStarted][Warning] PLAYER[UID=" + _session.m_pi.uid + "] tentou entrar na sala[NUMERO=" + m_ri.numero + "] ja em jogo, mas o tempo de entrar no tourney acabou.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM, // Acabou o tempo de entrar na sala
                        2706, 2));
                }

                // Add Player a sala
                enter(_session);

                ret = true;

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[room::requestEnterGameAfterStarted][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Excluí player da sala se adicionou ele antes
                if (findSessionByUID(_session.m_pi.uid) != null)
                {
                    leave(_session, 0);
                }

                // Resposta erro
                p.init_plain(0x113);

                p.WriteByte(6); // Option Error

                // Error Code
                p.WriteByte((byte)((ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) == STDA_ERROR_TYPE.ROOM) ? ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) : 1));

                packet_func.session_send(p,
                    _session, 1);
            }

            return ret;
        }

        public void requestUpdateEnterAfterStartedInfo(Player _session, EnterAfterStartInfo _easi)
        {
            if (!_session.getState())
            {
                throw new exception("[room::requestUpdateEnterAfterStartedInfo] [Error] player nao esta connectado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }

            PangyaBinaryWriter p = new PangyaBinaryWriter();

            try
            {

                if (m_pGame == null)
                {
                    throw new exception("[room::requestUpdateEnterAfterStartedInfo] [Error] PLAYER[UID=" + _session.m_pi.uid + "] tentou atualizar info do player que entrou depois na sala[NUMERO=" + m_ri.numero + "] ja em jogo, mas a sala nao tem nenhum jogo inicializado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                        2705, 1));
                }

                m_pGame.requestUpdateEnterAfterStartedInfo(_session, _easi);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[room::requestUpdateEnterAfterStartedInfo][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Resposta erro
                p.init_plain(0x113);

                p.WriteByte(6); // Option Error

                // Error Code
                p.WriteByte((byte)((ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) == STDA_ERROR_TYPE.ROOM) ? ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) : 1));

                packet_func.session_send(p,
                    _session, 1);
            }
        }

        public bool requestFinishGame(Player _session, packet _packet)
        {
            if (!_session.getState())
            {
                throw new exception("[room::requestFinishGame] [Error] player nao esta connectado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }
            if (_packet == null)
            {
                throw new exception("[room::requestFinishGame] [Error] _packet is null", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }

            bool ret = false;

            try
            {

                if (m_pGame == null)
                {
                    throw new exception("[room::requestFinishGame] [Error] PLAYER[UID=" + _session.m_pi.uid + "] tentou finalizar jogo na sala[NUMERO=" + m_ri.numero + "], mas a sala nao tem nenhum jogo inicializado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                        1, 0x5202101));
                }

                if (m_pGame.requestFinishGame(_session, _packet))
                { // Terminou o Jogo

                    finish_game();
                    m_ri.roomId = Guid.NewGuid();//gera a proxima roomId para evitar problemas com players que tentarem reconectar no jogo antigo
                    ret = true;
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[room::requestFinishGame][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            return ret;
        }

        public void requestChangeWindNextHoleRepeat(Player _session, packet _packet)
        {
            if (!_session.getState())
            {
                throw new exception("[room::requestChangeWindNextHoleRepeat] [Error] player nao esta connectado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }
            if (_packet == null)
            {
                throw new exception("[room::requestChangeWindNextHoleRepeat] [Error] _packet is null", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }

            try
            {

                if (m_pGame == null)
                {
                    throw new exception("[room::requestChangeWindNextHoleRepeat] [Error] PLAYER[UID=" + _session.m_pi.uid + "] tentou trocar vento dos proximos holes repeat no jogo na sala[NUMERO=" + m_ri.numero + "], mas a sala nao tem nenhum jogo inicializado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                        1, 0x5202201));
                }

                m_pGame.requestChangeWindNextHoleRepeat(_session, _packet);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[room::requestChangeWindNextHoleRepeat][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void requestPlayerReportChatGame(Player _session, packet _packet)
        {
            if (!_session.getState())
            {
                throw new exception("[room::requestPlayerReportChatGame] [Error] player nao esta connectado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }
            if (_packet == null)
            {
                throw new exception("[room::requestPlayerReportChatGame] [Error] _packet is null", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }

            try
            {

                if (m_pGame == null)
                {
                    throw new exception("[room::requestPlayerReportChatGame] [Error] PLAYER[UID=" + _session.m_pi.uid + "] tentou reporta o chat do jogo na sala[NUMERO=" + m_ri.numero + "], mas nao tem nenhum jogo inicializado na sala. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                        0x580200, 0));
                }

                // Report Chat Game
                m_pGame.requestPlayerReportChatGame(_session, _packet);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[room::requestPlayerReportChatGame][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if (ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) != STDA_ERROR_TYPE.GAME)
                {
                    throw;
                }
            }
        }

        // Common Command GM
        public void requestExecCCGChangeWindVersus(Player _session, packet _packet)
        {
            if (!_session.getState())
            {
                throw new exception("[room::requestExecCCGChangeWindVersus] [Error] player nao esta connectado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }
            if (_packet == null)
            {
                throw new exception("[room::requestExecCCGChangeWindVersus] [Error] _packet is null", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }

            try
            {

                if (m_ri.getTipo() != RoomInfo.ROOM_INFO_TYPE.PANG_BATTLE || m_ri.getTipo() != RoomInfo.ROOM_INFO_TYPE.STROKE || m_ri.getTipo() != RoomInfo.ROOM_INFO_TYPE.MATCH)
                {
                    throw new exception("[room::requestExecCCGChangeWindVersus] [Error] PLAYER[UID=" + _session.m_pi.uid + "] tentou executar o comando de troca de vento na sala[NUMERO=" + m_ri.numero + ", TIPO=" + Convert.ToString(m_ri.tipo) + "], mas o tipo da sala nao é Stroke ou Match modo. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                        1, 0x5700100));
                }

                if (m_pGame == null)
                {
                    throw new exception("[room::requestExecCCGChangeWindVersus] [Error] PLAYER[UID=" + _session.m_pi.uid + "] tentou executar o comando de troca de vento na sala[NUMERO=" + m_ri.numero + "], mas a sala nao tem nenhum jogo inicializado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                        2, 0x5700100));
                }

                m_pGame.requestExecCCGChangeWind(_session, _packet);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[room::requestExecCCGChangeWindVersus][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                throw;
            }
        }

        public void requestExecCCGChangeWeather(Player _session, packet _packet)
        {
            if (!_session.getState())
            {
                throw new exception("[room::requestExecCCGChangeWeather] [Error] player nao esta connectado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }
            if (_packet == null)
            {
                throw new exception("[room::requestExecCCGChangeWeather] [Error] _packet is null", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }

            try
            {

                // Update on Flag Lounge or Game Course.hole.weather
                if (m_ri.getTipo() == RoomInfo.ROOM_INFO_TYPE.LOUNGE)
                {

                    m_weather_lounge = _packet.ReadByte();

                    // UPDATE ON GAME
                    PangyaBinaryWriter p = new PangyaBinaryWriter((ushort)0x9E);

                    p.WriteUInt16(m_weather_lounge);
                    p.WriteByte(1); // Acho que seja type, não sei, vou deixar 1 por ser o GM que mudou

                    packet_func.room_broadcast(this,
                        p, 1);

                }
                else if (m_pGame != null)
                {

                    m_pGame.requestExecCCGChangeWeather(_session, _packet);

                }
                else
                {
                    throw new exception("[room::requestExecCCGChangeWeather] [Error] PLAYER[UID=" + _session.m_pi.uid + "] tentou executar o comando de troca de tempo(weather) na sala[NUMERO=" + m_ri.numero + "], mas a sala nao é lounge ou nao tem um jogo iniclializado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                        3, 0x5700100));
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[room::requestExecCCGChangeWeather][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                throw;
            }
        }

        public void requestExecCCGGoldenBell(Player _session, packet _packet)
        {
            if (!_session.getState())
            {
                throw new exception("[room::requestExecCCGGoldenBell] [Error] player nao esta connectado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }
            if (_packet == null)
            {
                throw new exception("[room::requestExecCCGGoldenBell] [Error] _packet is null", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM,
                    12, 0));
            }

            try
            {

                uint32_t item_typeid = _packet.ReadUInt32();
                uint32_t item_qntd = _packet.ReadUInt32();

                if (item_typeid == 0)
                {
                    throw new exception("[room::requestExecCCGGoldenBell] [Error] PLAYER[UID=" + _session.m_pi.uid + "] tentou enviar presente para todos da sala[NUMERO=" + m_ri.numero + "] o Item[TYPEID=" + Convert.ToString(item_typeid) + "QNTD = " + Convert.ToString(item_qntd) + "], mas item is invalid. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER,
                        3, 0x5700100));
                }

                if (item_qntd > 20000)
                {
                    throw new exception("[room::requestExecCCGGoldenBell] [Error] PLAYER[UID=" + _session.m_pi.uid + "] tentou enviar presente para todos da sala[NUMERO=" + m_ri.numero + "] o Item[TYPEID=" + Convert.ToString(item_typeid) + "QNTD = " + Convert.ToString(item_qntd) + "], mas a quantidade passa de 20mil. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER,
                        4, 0x5700100));
                }

                var @base = sIff.getInstance().findCommomItem(item_typeid);

                if (@base == null || @base.ID == 0)
                {
                    throw new exception("[room::requestExecCCGGoldenBell] [Error] PLAYER[UID=" + _session.m_pi.uid + "] tentou enviar presente para todos da sala[NUMERO=" + m_ri.numero + "] o Item[TYPEID=" + Convert.ToString(item_typeid) + "QNTD = " + Convert.ToString(item_qntd) + "], mas o item nao existe no IFF_STRUCT do Server. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER,
                        6, 0));
                }

                BuyItem bi = new BuyItem();

                bi.id = -1;
                bi._typeid = item_typeid;
                bi.qntd = item_qntd;

                var msg = ("GM enviou um item para voce: item[ " + (@base.Name) + " ]");

                foreach (var el in v_sessions)
                {

                    // Limpa item
                    stItem item = new stItem();

                    ItemManager.initItemFromBuyItem(el.m_pi,
                        item, bi, false, 0, 0, 1);

                    if (item._typeid == 0)
                    {
                        throw new exception("[room::requestExecCCGGoldenBell] [Error] PLAYER[UID=" + _session.m_pi.uid + "] tentou enviar presente para todos da sala[NUMERO=" + m_ri.numero + "] o Item[TYPEID=" + Convert.ToString(item_typeid) + "QNTD = " + Convert.ToString(item_qntd) + "], mas nao conseguiu inicializar o item. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER,
                            5, 0));
                    }

                    if (MailBoxManager.sendMessageWithItem(0,
                        el.m_pi.uid, msg, item) <= 0)
                    {
                        throw new exception("[room::requestExecCCGGoldenBell] [Error] PLAYER[UID=" + _session.m_pi.uid + "] tentou enviar presente para o PLAYER[UID=" + Convert.ToString(el.m_pi.uid) + "] o Item[TYPEID=" + Convert.ToString(item_typeid) + ", QNTD=" + Convert.ToString(item_qntd) + "], mas nao conseguiu colocar o item no mail box dele. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER,
                            7, 0));
                    }

                    // Log
                    _smp.message_pool.getInstance().push(new message("[room::requestExecCCGGoldenBell][Info] PLAYER[UID=" + _session.m_pi.uid + "] enviou um Item[TYPEID=" + Convert.ToString(item_typeid) + ", QNTD=" + Convert.ToString(item_qntd) + "] para o PLAYER[UID=" + Convert.ToString(el.m_pi.uid) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[room::requestExecCCGGoldenBell][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                throw;
            }
        }

        // Smart Calculator Command

        // Pede o Hole que o player está, 
        // se eles estiver jogando ou 0 se ele não está jogando
        public byte requestPlace(Player _session)
        {

            try
            {

                if (m_pGame != null)
                {
                    return m_pGame.requestPlace(_session);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[room::requestPlacePlayer][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            return 0;
        }

        // Time Tourney After Enter IN GAME
        public void requestStartAfterEnter(Action _job)
        {

            try
            {

                if (m_pGame == null)
                    return;

                m_pGame.requestStartAfterEnter(_job);

            }
            catch (exception e)
            {
                _smp.message_pool.getInstance().push(new message("[room::requestStartAfterEnter][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void AddListGallery(Player pSession)
        {
            if (pSession == null)
            {
                Console.WriteLine("AddListGallery : session null");
                return;
            }

            // Procura o primeiro slot vazio na lista
            m_listGallery.Add(pSession);

            // Se não achou espaço, loga o ID do usuário
            string userId = pSession.getID();
            Console.WriteLine($"AddListGallery : {userId}");
        }

        public int GetSizeGalleryList()
        {
            int count = 0;

            // m_listGallery é assumido como uma lista ou array de sessões
            foreach (var session in m_listGallery)
            {
                if (session != null)
                    count++;
            }

            return count;
        }

        public void SendGalleryList(Player _session, byte code)
        {
            var galleryList = this.m_listGallery;
            int validSessions = galleryList.Count(s => s != null);

            if (validSessions == 0)
                return;

            var packet = new PangyaBinaryWriter(0xAE);
            packet.WriteByte(code);

            switch (code)
            {
                case 0:
                    byte size = (byte)this.GetSizeGalleryList();
                    packet.WriteByte(size);

                    foreach (var session in galleryList)
                    {
                        if (session != null)
                        {
                            packet.Write(getPlayerInfo(session).ToArray());
                        }
                    }

                    packet_func.session_send(packet, _session);
                    break;

                case 1:
                    if (_session != null)
                    {
                        PlayerRoomInfoEx src = getPlayerInfo(_session);
                        packet.Write(src.ToArray());
                        goto Broadcast;
                    }
                    break;

                case 2:
                case 3:
                    packet.Write(_session.getUID());
                    goto Broadcast;

                default:
                Broadcast:
                    packet_func.room_broadcast(this, packet);
                    break;
            }

            packet.Dispose(); // ou packet = null se não tiver IDisposable
        }

        public static void SQLDBResponse(uint32_t _msg_id,
            Pangya_DB _pangya_db,
            object _arg)
        {

            if (_arg == null)
            {
                _smp.message_pool.getInstance().push(new message("[room::SQLDBResponse][Warning] _arg is null com msg_id = " + Convert.ToString(_msg_id), type_msg.CL_FILE_LOG_AND_CONSOLE));
                return;
            }

            // Por Hora só sai, depois faço outro tipo de tratamento se precisar
            if (_pangya_db.getException().getCodeError() != 0)
            {
                _smp.message_pool.getInstance().push(new message("[room::SQLDBResponse] [Error] " + _pangya_db.getException().getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                return;
            }

            var _channel = Tools.reinterpret_cast<Channel>(_arg);

            switch (_msg_id)
            {
                case 7: // Update Character PCL
                    {
                        var cmd_ucp = Tools.reinterpret_cast<CmdUpdateCharacterPCL>(_pangya_db);

                        _smp.message_pool.getInstance().push(new message("[room::SQLDBResponse][Info] Atualizou Character[TYPEID=" + Convert.ToString(cmd_ucp.getInfo()._typeid) + ", ID=" + Convert.ToString(cmd_ucp.getInfo().id) + "] PCL[C0=" + Convert.ToString((ushort)cmd_ucp.getInfo().pcl[(int)CharacterInfo.Stats.S_POWER]) + ", C1=" + Convert.ToString((ushort)cmd_ucp.getInfo().pcl[(int)CharacterInfo.Stats.S_CONTROL]) + ", C2=" + Convert.ToString((ushort)cmd_ucp.getInfo().pcl[(int)CharacterInfo.Stats.S_ACCURACY]) + ", C3=" + Convert.ToString((ushort)cmd_ucp.getInfo().pcl[(int)CharacterInfo.Stats.S_SPIN]) + ", C4=" + Convert.ToString((ushort)cmd_ucp.getInfo().pcl[(int)CharacterInfo.Stats.S_CURVE]) + "] do PLAYER[UID=" + Convert.ToString(cmd_ucp.getUID()) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));
                        break;
                    }
                case 8: // Update ClubSet Stats
                    {
                        var cmd_ucss = Tools.reinterpret_cast<CmdUpdateClubSetStats>(_pangya_db);

                        _smp.message_pool.getInstance().push(new message("[room::SQLDBResponse][Info] Atualizou ClubSet[TYPEID=" + Convert.ToString(cmd_ucss.getInfo()._typeid) + ", ID=" + Convert.ToString(cmd_ucss.getInfo().id) + "] Stats[C0=" + Convert.ToString((ushort)cmd_ucss.getInfo().c[(int)CharacterInfo.Stats.S_POWER]) + ", C1=" + Convert.ToString((ushort)cmd_ucss.getInfo().c[(int)CharacterInfo.Stats.S_CONTROL]) + ", C2=" + Convert.ToString((ushort)cmd_ucss.getInfo().c[(int)CharacterInfo.Stats.S_ACCURACY]) + ", C3=" + Convert.ToString((ushort)cmd_ucss.getInfo().c[(int)CharacterInfo.Stats.S_SPIN]) + ", C4=" + Convert.ToString((ushort)cmd_ucss.getInfo().c[(int)CharacterInfo.Stats.S_CURVE]) + "] do PLAYER[UID=" + Convert.ToString(cmd_ucss.getUID()) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));
                        break;
                    }
                case 9: // Update Character Mastery
                    {
                        var cmd_ucm = Tools.reinterpret_cast<CmdUpdateCharacterMastery>(_pangya_db);

                        _smp.message_pool.getInstance().push(new message("[room::SQLDBResponse][Info] Atualizou Character[TYPEID=" + Convert.ToString(cmd_ucm.getInfo()._typeid) + ", ID=" + Convert.ToString(cmd_ucm.getInfo().id) + "] Mastery[value=" + Convert.ToString(cmd_ucm.getInfo().mastery) + "] do PLAYER[UID=" + Convert.ToString(cmd_ucm.getUID()) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));
                        break;
                    }
                case 12: // Update ClubSet Workshop
                    {
                        var cmd_ucw = Tools.reinterpret_cast<CmdUpdateClubSetWorkshop>(_pangya_db);

                        _smp.message_pool.getInstance().push(new message("[room::SQLDBResponse][Info] PLAYER[UID=" + Convert.ToString(cmd_ucw.getUID()) + "] Atualizou ClubSet[TYPEID=" + Convert.ToString(cmd_ucw.getInfo()._typeid) + ", ID=" + Convert.ToString(cmd_ucw.getInfo().id) + "] Workshop[C0=" + Convert.ToString(cmd_ucw.getInfo().clubset_workshop.c[0]) + ", C1=" + Convert.ToString(cmd_ucw.getInfo().clubset_workshop.c[1]) + ", C2=" + Convert.ToString(cmd_ucw.getInfo().clubset_workshop.c[2]) + ", C3=" + Convert.ToString(cmd_ucw.getInfo().clubset_workshop.c[3]) + ", C4=" + Convert.ToString(cmd_ucw.getInfo().clubset_workshop.c[4]) + ", Level=" + Convert.ToString(cmd_ucw.getInfo().clubset_workshop.level) + ", Mastery=" + Convert.ToString(cmd_ucw.getInfo().clubset_workshop.mastery) + ", Rank=" + Convert.ToString(cmd_ucw.getInfo().clubset_workshop.rank) + ", Recovery=" + Convert.ToString(cmd_ucw.getInfo().clubset_workshop.recovery_pts) + "] Flag=" + Convert.ToString(cmd_ucw.getFlag()) + "", type_msg.CL_FILE_LOG_AND_CONSOLE));
                        break;
                    }
                case 26: // Update Mascot Info
                    {

                        var cmd_umi = Tools.reinterpret_cast<CmdUpdateMascotInfo>(_pangya_db);

                        _smp.message_pool.getInstance().push(new message("[room::SQLDBResponse][Info] PLAYER[UID=" + Convert.ToString(cmd_umi.getUID()) + "] Atualizar Mascot Info[TYPEID=" + Convert.ToString(cmd_umi.getInfo()._typeid) + ", ID=" + Convert.ToString(cmd_umi.getInfo().id) + ", LEVEL=" + Convert.ToString((ushort)cmd_umi.getInfo().level) + ", EXP=" + Convert.ToString(cmd_umi.getInfo().exp) + ", FLAG=" + Convert.ToString((ushort)cmd_umi.getInfo().flag) + ", TIPO=" + Convert.ToString(cmd_umi.getInfo().tipo) + ", IS_CASH=" + Convert.ToString((ushort)cmd_umi.getInfo().is_cash) + ", PRICE=" + Convert.ToString(cmd_umi.getInfo().price) + ", MESSAGE=" + (cmd_umi.getInfo().message) + ", END_DT=" + (cmd_umi.getInfo().data.ConvertTime()) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                        break;
                    }
                case 0:
                default: // 25 é update item equipado slot
                    break;
            }
        }

        ~Room()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposedValue)
                return;

            if (disposing)
            {

                // Leave All Players
                leaveAll(0);

                if (m_pGame != null)
                {
                    m_pGame.stopTime();//desliga o relogio...
                    m_pGame.Dispose();      // elimina a referência
                }
                m_pGame = null;

                m_channel_owner = 255;

                m_weather_lounge = 0;

                if (v_sessions.Any())
                {
                    v_sessions.Clear();
                }

                if (m_player_info.Any())
                {
                    m_player_info.Clear();
                }

                clear_invite();

                clear_Player_kicked();

                clear_teans();

                m_bot_tourney = false;

                m_personal_shop.destroy();

                m_destroying = false;      // Destruindo a sala
                try
                {

                    @lock();

                    m_destroying = true;

                    unlock();

                    disposedValue = true;
                }
                catch (exception e)
                {

                    if (!ExceptionError.STDA_ERROR_CHECK_SOURCE_AND_ERROR_TYPE(e.getCodeError(),
                        STDA_ERROR_TYPE.ROOM, 150))
                    {

                        unlock();

                        _smp.message_pool.getInstance().push(new message("[room::destroy][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                    }
                }
            }
        }

        public void Dispose()
        {
            // Não altere este código. Coloque o código de limpeza no método 'Dispose(bool disposing)'
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public void requestChangePlayerStateReadyRoom(Player _session, packet _packet)
        {
            byte ready = _packet.ReadUInt8();

            PlayerRoomInfoEx pri = getPlayerInfo(_session);
            // Update state of ready
            pri.state_flag.ready = (byte)(ready == 0 ? 1 : 0);//invertido

            PangyaBinaryWriter p = new PangyaBinaryWriter(0x78); // Estado de Ready do player na sala

            p.WriteInt32(_session.m_oid);
            p.WriteByte(ready);

            packet_func.room_broadcast(this,
                p, 1);

            m_player_info[_session] = pri;
        }

        private void ThrowHackException(Player session, string motivo)
        {
            var ri = getInfo();

            string msg = $"[Room::ThrowHackException] [Error] PLAYER [UID={session.m_pi.uid}] " +
                         $"Channel[ID={this.m_channel_owner}] tentou criar sala [Nome={ri.name}, PWD={ri.senha}, TIPO={ri.tipo}], {motivo}. Hacker ou Bug";

            throw new exception(msg, ExceptionError.STDA_MAKE_ERROR_TYPE(
                STDA_ERROR_TYPE.ROOM, 10, 0x770001));
        }
    }
}