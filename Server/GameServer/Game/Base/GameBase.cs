// Arquivo Game.cs
using Pangya_GameServer.Game.Manager;
using Pangya_GameServer.Game.System;
using Pangya_GameServer.Models;
using Pangya_GameServer.PacketFunc;
using Pangya_GameServer.Repository;
using Pangya_GameServer.UTIL;
using PangyaAPI.IFF.JP.Extensions;
using PangyaAPI.IFF.JP.Models.Flags;
using PangyaAPI.Network.PangyaPacket;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Models;
using PangyaAPI.Utilities.Log;
using sgs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using static Pangya_GameServer.Models.DefineConstants;
using int32_t = System.Int32;
using int64_t = System.Int64;
using size_t = System.Int32;
using uint32_t = System.UInt32;
using uint64_t = System.UInt64;
namespace Pangya_GameServer.Game.Base
{
    public abstract class GameBase : IDisposable
    {
        protected List<Player> m_players;
        protected Dictionary<Player, PlayerGameInfo> m_player_info;
        protected List<PlayerGameInfo> m_player_order;
        protected Dictionary<uint, uint> m_player_report_game;
        protected RoomInfoEx m_ri;
        protected RateValue m_rv;
        /// <summary>
        /// 1 = iniciou, 2 acabou, -1 default
        /// </summary>
        public int m_game_init_state;
        protected bool m_state;
        protected DateTime m_start_time;
        protected PangyaSyncTimer m_timer;
        protected bool m_channel_rookie;
        protected volatile int m_sync_send_init_data;
        protected CourseManager m_course;
        public RoomInfoLog m_room_log;
        protected bool disposedValue = false; // Mudei para protected para as filhas verem
        // 
        #region Abstract Methods 
        // Game
        public abstract bool requestFinishGame(Player _session, packet _packet);

        // Inicializa Jogo e Finaliza Jogo
        public abstract bool init_game();

        // Trata Shot Sync Data
        public abstract void requestTranslateSyncShotData(Player _session, ShotSyncData _ssd);
        public abstract void requestReplySyncShotData(Player _session);

        // Metôdos do Game.Course.Hole
        public abstract void requestInitHole(Player _session, packet _packet);
        public abstract bool requestFinishLoadHole(Player _session, packet _packet);
        public abstract void requestFinishCharIntro(Player _session, packet _packet);
        public abstract void requestFinishHoleData(Player _session, packet _packet);

        // Server enviou a resposta do InitShot para o cliente
        // Esse aqui é exclusivo do VersusBase 
        public abstract void requestInitShot(Player _session, packet _packet);
        public abstract void requestSyncShot(Player _session, packet _packet);
        public abstract void requestInitShotArrowSeq(Player _session, packet _packet);
        public abstract void requestShotEndData(Player _session, packet _packet);
        public abstract RetFinishShot requestFinishShot(Player _session, packet _packet);

        public abstract void requestChangeMira(Player _session, packet _packet);
        public abstract void requestChangeStateBarSpace(Player _session, packet _packet);
        public abstract void requestActivePowerShot(Player _session, packet _packet);
        public abstract void requestChangeClub(Player _session, packet _packet);
        public abstract void requestUseActiveItem(Player _session, packet _packet);
        public abstract void requestChangeStateTypeing(Player _session, packet _packet); // Escrevendo
        public abstract void requestMoveBall(Player _session, packet _packet);
        public abstract void requestChangeStateChatBlock(Player _session, packet _packet);
        public abstract void requestActiveBooster(Player _session, packet _packet);
        public abstract void requestActiveReplay(Player _session, packet _packet);
        public abstract void requestActiveCutin(Player _session, packet _packet);

        // Hability Item
        public abstract void requestActiveRing(Player _session, packet _packet);
        public abstract void requestActiveRingGround(Player _session, packet _packet);
        public abstract void requestActiveRingPawsRainbowJP(Player _session, packet _packet);
        public abstract void requestActiveRingPawsRingSetJP(Player _session, packet _packet);
        public abstract void requestActiveRingPowerGagueJP(Player _session, packet _packet);
        public abstract void requestActiveRingMiracleSignJP(Player _session, packet _packet);
        public abstract void requestActiveWing(Player _session, packet _packet);
        public abstract void requestActivePaws(Player _session, packet _packet);
        public abstract void requestActiveGlove(Player _session, packet _packet);
        public abstract void requestActiveEarcuff(Player _session, packet _packet);

        #endregion

        #region Virtual 

        public virtual bool finish_game(Player _session, int option = 0) { return false; }

        /// <summary>
        /// metodo para decriptografar dados do shot(tiro) do player...
        /// </summary>
        /// <param name="_buffer">dados do tiro criptogrfado</param>
        /// <returns></returns>
        protected ShotSyncData DecryptShot(byte[] _buffer)
        {
            if (_buffer.Length < 38 || _buffer.Length > 38)
                return null;

            for (int i = 0; i < _buffer.Length; i++)
                _buffer[i] = (byte)(_buffer[i] ^ m_ri.key[i % 16]);

            //decrypt shot
            var reader = new PangyaBinaryReader(new MemoryStream(_buffer));
            var ssd = new ShotSyncData
            {
                oid = reader.ReadInt32(), //oid

                location = new ShotSyncData.Location()
                {
                    x = reader.ReadSingle(),
                    y = reader.ReadSingle(),
                    z = reader.ReadSingle(),
                },

                state = (ShotSyncData.SHOT_STATE)reader.ReadByte(),

                bunker_flag = reader.ReadByte(),
                ucUnknown = reader.ReadByte(),

                pang = reader.ReadUInt32(),

                bonus_pang = reader.ReadUInt32(),

                state_shot = new ShotSyncData.stStateShot()
                {
                    display = new ShotSyncData.stStateShot.uDisplayState()
                    {
                        ulState = reader.ReadUInt32(),
                    },
                    shot = new ShotSyncData.stStateShot.uShotState()
                    {
                        ulState = reader.ReadUInt32()
                    }
                },

                tempo_shot = reader.ReadInt16(),
                grand_prix_penalidade = reader.ReadByte()
            };
            return ssd;
        }

        public virtual PlayerGameInfo INIT_PLAYER_INFO(string _method, string _msg, Player __session)
        {
            var pgi = getPlayerInfo((__session));
            if (pgi == null)
                throw new exception($"[{GetType().Name}::" + _method + "][Error] PLAYER[UID=" + __session.m_pi.uid + "] " + _msg + ", mas o game nao tem o info dele guardado. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME, 1, 4));

            return pgi;
        }

        public virtual void INIT_PLAYER_INFO(string _method, string _msg, Player __session, out PlayerGameInfo pgi)
        {
            pgi = getPlayerInfo(__session);
            if (pgi == null)
                throw new exception($"[{GetType().Name}::" + _method + "][Error] PLAYER[UID=" + __session.m_pi.uid + "] " + _msg + ", mas o game nao tem o info dele guardado. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME, 1, 4));
        }

        #endregion

        public GameBase(List<Player> _players, RoomInfoEx _ri, RateValue _rv, bool _channel_rookie)
        {
            this.m_players = new List<Player>(_players);

            this.m_ri = _ri;
            this.m_rv = _rv;
            this.m_channel_rookie = _channel_rookie;
            this.m_start_time = DateTime.MinValue;
            this.m_player_info = new Dictionary<Player, PlayerGameInfo>();
            this.m_course = null;
            this.m_game_init_state = -1;
            this.m_state = false;
            this.m_player_order = new List<PlayerGameInfo>();
            this.m_timer = null;
            this.m_player_report_game = new Dictionary<uint, uint>();

            m_sync_send_init_data = 0;

            // Inicializar Artefact Info Of Game
            initArtefact();

            // Inicializar o rate chuva dos itens equipado dos players no jogo
            initPlayersItemRainRate();

            // Inicializa a flag persist rain next hole
            initPlayersItemRainPersistNextHole();

            // Map Dados Estáticos
            if (!MapSystem.getInstance().isLoad())
            {
                MapSystem.getInstance().load();
            }

            var map = MapSystem.getInstance().getMap((byte)((int)m_ri.course & 0x7F));

            if (map == null)
            {
                _smp.message_pool.getInstance().push(new message("[GameBase::Game][Error][Warning] tentou pegar o Map dados estaticos do course[COURSE=" + Convert.ToString((ushort)((int)m_ri.course & 0x7F)) + "], mas nao conseguiu encontra na classe do Server.", type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            // Cria Course
            m_course = new CourseManager(m_ri,
                _channel_rookie,
                (map == null) ? 1.0f : map.star,
                m_rv.rain, m_rv.persist_rain);
        }

        public virtual void clear_player_order()
        {
            m_player_order.Clear();
        }


        protected void clear_time()
        {
            // Garantir que qualquer exception derrube o server
            try
            {

                if (m_timer != null)
                    sgs.gs.getInstance().unMakeTime(m_timer);
            }
            catch (exception e)
            {
                _smp.message_pool.getInstance().push(new message("[GameBase::clear_time][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            m_timer = null;
        }

        public virtual void sendInitialData(Player _session)
        {

            PangyaBinaryWriter p = new PangyaBinaryWriter();

            try
            {

                // Course
                p.init_plain(0x52);

                p.WriteByte((byte)m_ri.course);
                p.WriteByte(m_ri.tipo_show);
                p.WriteByte(m_ri.modo);
                p.WriteByte(m_ri.qntd_hole);
                p.WriteUInt32(m_ri.trofel);
                p.WriteUInt32(m_ri.time_vs);
                p.WriteUInt32(m_ri.time_30s);
                // Hole Info, Hole Spinning Cube, end Seed Random Course
                m_course.makePacketHoleInfo(p);

                packet_func.session_send(p,
                    _session, 1);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[GameBase::sendInitialData][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

        }

        // Envia os dados iniciais para quem entra depois no Game
        public virtual void sendInitialDataAfter(Player _session)
        {
            // UNREFERENCED_PARAMETER(_session);
        }

        protected Player findSessionByOID(int32_t _oid)
        {
            return m_players.FirstOrDefault(el => el.m_oid == _oid);
        }

        protected Player findSessionByUID(uint32_t _uid)
        {
            return m_players.FirstOrDefault(el => el.m_pi.uid == _uid);
        }

        protected Player findSessionByNickname(string _nickname)
        {
            return m_players.FirstOrDefault(el =>
            {
                return (string.CompareOrdinal(_nickname, el.m_pi.nickname) == 0);
            });
        }

        protected Player findSessionByPlayerGameInfo(PlayerGameInfo _pgi)
        {

            if (_pgi == null)
            {
                _smp.message_pool.getInstance().push(new message("[GameBase::findSessionByPlayerGameInfo][Error] PlayerGameInfo* _pgi is invalid(null)", type_msg.CL_FILE_LOG_AND_CONSOLE));

                return null;
            }

            return m_player_info.FirstOrDefault(_el =>
            {
                return _el.Value == _pgi;
            }).Key;
        }

        public PlayerGameInfo getPlayerInfo(Player _session)
        {

            if (_session == null)
            {
                throw new exception("[GameBase::getPlayerInfo][Error] _session is null", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME,
                    1, 0));
            }

            return m_player_info.FirstOrDefault(_el =>
            {
                return _el.Key == _session;
            }).Value;
        }



        // Se _session for diferente de null retorna todas as session, menos a que foi passada no _session
        public List<Player> getSessions(Player _session = null)
        {

            List<Player> v_sessions = new List<Player>();
            // Se _session for diferente de null retorna todas as session, menos a que foi passada no _session
            foreach (var el in m_players)
            {
                if (el != null
                    && el.getState()
                    && el.m_pi.mi.sala_numero != ushort.MaxValue
                    && (_session == null || _session != el))
                {
                    v_sessions.Add(el);
                }
            }
            return v_sessions;
        }

        public virtual DateTime getTimeStart()
        {
            return m_start_time;
        }

        public virtual void addPlayer(Player _session)
        {
            m_players.Add(_session);

            makePlayerInfo(_session);
        }

        public virtual bool deletePlayer(Player _session, int _option)
        {
            if (_session == null)
            {
                throw new exception("[GameBase::deletePlayer][Error] tentou deletar um player, mas o seu endereco eh null.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME,
                    50, 0));
            }

            var it = m_players.Any(c => c == _session);

            if (it)
            {
                m_players.Remove(_session);//limpar ou deletar o jogador da lista
            }
            else
            {
                _smp.message_pool.getInstance().push(new message("[GameBase::deletePlayer][Warning] player ja foi excluido do game.", type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            return false;
        }

        public virtual void requestActiveAutoCommand(Player _session, packet _packet)
        {
            if (!_session.getState())
            {
                throw new exception("[GameBase::requestActiveAutoCommand][Error] player nao esta connectado.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME,
                    1, 0));
            }

            if (_packet == null)
            {
                throw new exception("[GameBase::requestActiveAutoCommand][Error] _packet is null", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME,
                    6, 0));
            }

            PangyaBinaryWriter p = new PangyaBinaryWriter();

            try
            {

                var pgi = getPlayerInfo((_session));
                if (pgi == null)
                {
                    throw new exception("[GameBase::" + "requestActiveAutoCommand][Error] PLAYER[UID=" + Convert.ToString((_session).m_pi.uid) + "] " + "tentou ativar var Command no jogo" + ", mas o game nao tem o info dele guardado. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME,
                        1, 4));
                }

                if (!pgi.premium_flag)
                { // (não é)!PREMIUM USER

                    var pWi = _session.m_pi.findWarehouseItemByTypeid(AUTO_COMMAND_TYPEID);

                    if (pWi == null)
                    {
                        throw new exception("[GameBase::requestActiveAutoCommand][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou ativar o var Command Item[TYPEID=" + Convert.ToString(AUTO_COMMAND_TYPEID) + "], mas ele nao tem o item. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME, 1, 0x550001));
                    }

                    if (pWi.STDA_C_ITEM_QNTD < 1)
                    {
                        throw new exception("[GameBase::requestActiveAutoCommand][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou ativar o var Command Item[TYPEID=" + Convert.ToString(AUTO_COMMAND_TYPEID) + "], mas ele nao tem quantidade suficiente do item[QNTD=" + Convert.ToString(pWi.STDA_C_ITEM_QNTD) + ", QNTD_REQ=1]. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME,
                            2, 0x550002));
                    }

                    var it = pgi.used_item.v_passive.find(pWi._typeid);

                    if (it.Key <= 0)
                    {
                        throw new exception("[GameBase::requestActiveAutoCommand][Error] PLAYER[UID = " + Convert.ToString(_session.m_pi.uid) + "] tentou ativar var Command, mas ele nao tem ele no item passive usados do server. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY_BASE,
                            13, 0));
                    }

                    if ((short)it.Value.count >= pWi.STDA_C_ITEM_QNTD)
                    {
                        throw new exception("[GameBase::requestActiveAutoCommand][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou ativar var Command, mas ele ja usou todos os var Command. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY_BASE,
                            14, 0));
                    }

                    // Add +1 ao item passive usado
                    it.Value.count++;
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[GameBase::requestActiveAutoCommand][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                // !@ Não sei o que esse pacote faz, não encontrei no meu antigo pangya
                // Resposta Error
                p.init_plain(0x22B);

                var errorCode = ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) == (uint)STDA_ERROR_TYPE.GAME ? ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) : 0x550001;
                p.WriteUInt32(errorCode);

                packet_func.session_send(p,
                    _session, 1);
            }
        }


        public virtual void requestActiveAssistGreen(Player _session, packet _packet)
        {
            if (!_session.getState())
            {
                throw new exception("[GameBase::requestActiveAssistGreen][Error] player nao esta connectado.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME,
                    1, 0));
            }
            ;
            if (_packet == null)
            {
                throw new exception("[GameBase::requestActiveAssistGreen][Error] _packet is null", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME,
                    6, 0));
            }

            PangyaBinaryWriter p = new PangyaBinaryWriter();

            try
            {

                uint32_t item_typeid = _packet.ReadUInt32();

                if (item_typeid == 0)
                    throw new exception("[GameBase::requestActiveAssistGreen][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou ativar Assist[TYPEID=" + Convert.ToString(item_typeid) + "] do Green, mas o item_typeid is invalid(zero). Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME,
                            1, 0x5200101));


                if (item_typeid != ASSIST_ITEM_TYPEID)
                    throw new exception("[GameBase::requestActiveAssistGreen][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou ativar Assist[TYPEID=" + Convert.ToString(item_typeid) + "] do Green, mas o item_typeid esta errado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME,
                               1, 0x5200101));

                var pWi = _session.m_pi.findWarehouseItemByTypeid(item_typeid);

                if (pWi == null)
                    throw new exception("[GameBase::requestActiveAssistGreen][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou ativar Assist[TYPEID=" + Convert.ToString(item_typeid) + "] do Green, mas o Assist Mode do player nao esta ligado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME,
                            2, 0x5200102));

                if (_session.m_pi.assist_flag || _session.m_pi.Assistent.id != 0)
                {

                    // Resposta para Active Assist Green
                    p.init_plain(0x26B);//get assist

                    p.WriteUInt32(0); // OK

                    p.WriteUInt32(pWi._typeid);
                    p.WriteUInt32(_session.m_pi.uid);

                    packet_func.session_send(p,
                        _session, 1);
                }
                else
                    throw new exception("[GameBase::requestActiveAssistGreen][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou ativar Assist[TYPEID=" + Convert.ToString(item_typeid) + "] do Green, mas o Assist Mode do player nao esta ligado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME,
                       2, 0x5200102));
            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[GameBase::requestActiveAssistGreen][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x26B);

                p.WriteUInt32((ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) == (uint)STDA_ERROR_TYPE.GAME) ? ExceptionError.STDA_SYSTEM_ERROR_DECODE(e.getCodeError()) : 0x5200100);

                packet_func.session_send(p,
                    _session, 1);
            }
        }
        // Esse Aqui só tem no VersusBase e derivados dele
        public virtual void requestMarkerOnCourse(Player _session, packet _packet)
        {
            // ignore : UNREFERENCED_PARAMETER(_session);
            // ignore : UNREFERENCED_PARAMETER(_packet);
        }

        public virtual void requestLoadGamePercent(Player _session, packet _packet)
        {
            // ignore : UNREFERENCED_PARAMETER(_session);
            // ignore : UNREFERENCED_PARAMETER(_packet);
        }

        public virtual void requestStartTurnTime(Player _session, packet _packet)
        {
            // ignore : UNREFERENCED_PARAMETER(_session);
            // ignore : UNREFERENCED_PARAMETER(_packet);
        }

        public virtual void requestUnOrPause(Player _session, packet _packet)
        {
            // ignore : UNREFERENCED_PARAMETER(_session);
            // ignore : UNREFERENCED_PARAMETER(_packet);
        }

        // Common Command GM Change Wind Versus
        public virtual void requestExecCCGChangeWind(Player _session, packet _packet)
        {
            // ignore : UNREFERENCED_PARAMETER(_session);
            // ignore : UNREFERENCED_PARAMETER(_packet);
        }

        public virtual void requestExecCCGChangeWeather(Player _session, packet _packet)
        {
            // ignore : UNREFERENCED_PARAMETER(_session);
            // ignore : UNREFERENCED_PARAMETER(_packet);
        }

        // Continua o versus depois que o player saiu no 3 hole pra cima e se for de 18h o game
        public virtual void requestReplyContinue()
        {
        }

        // Esse Aqui só tem no TourneyBase e derivados dele
        public virtual bool requestUseTicketReport(Player _session, packet _packet)
        {
            // ignore : UNREFERENCED_PARAMETER(_session);
            // ignore : UNREFERENCED_PARAMETER(_packet);

            return false;
        }

        // Apenas no Practice que ele é implementado
        public virtual void requestChangeWindNextHoleRepeat(Player _session, packet _packet)
        {
            // ignore : UNREFERENCED_PARAMETER(_session);
            // ignore : UNREFERENCED_PARAMETER(_packet);
        }

        // Exclusivo do Modo Tourney
        public virtual void requestStartAfterEnter(Action _job)
        {

            // ignore : UNREFERENCED_PARAMETER(_job);
        }

        public virtual void requestEndAfterEnter()
        {
        }

        public virtual void requestUpdateTrofel()
        {
        }

        // Excluviso do Modo Match
        public virtual void requestTeamFinishHole(Player _session, packet _packet)
        {
            // ignore : UNREFERENCED_PARAMETER(_session);
            // ignore : UNREFERENCED_PARAMETER(_packet);
        }
        // Pede o Hole que o player está, 
        // se eles estiver jogando ou 0 se ele não está jogando
        public virtual byte requestPlace(Player _session)
        {

            if (!_session.getState())
            {
                throw new exception("[GameBase::requestPlace][Error] player nao esta connectado.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY_BASE,
                    1, 0));
            }

            // Valor padrão
            ushort hole = 0;

            var pgi = getPlayerInfo((_session));
            if (pgi == null)
            {
                throw new exception("[GameBase::" + "requestPlace][Error] PLAYER[UID=" + Convert.ToString((_session).m_pi.uid) + "] " + "tentou pegar o lugar[Hole] do player no jogo" + ", mas o game nao tem o info dele guardado. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME,
                    1, 4));
            }

            if (pgi.hole != 255)
            {

                hole = m_course.findHoleSeq(pgi.hole);

                if (hole == ushort.MaxValue)
                {
                    // Valor padrão
                    hole = 0;

                    _smp.message_pool.getInstance().push(new message("[GameBase::requestPlace][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou pegar a sequencia do hole[NUMERO=" + Convert.ToString(pgi.hole) + "], mas ele nao encontrou no course do game na sala[NUMERO=" + Convert.ToString(m_ri.numero) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));
                }

            }
            else if (pgi.init_first_hole) // Só cria mensagem de log se o player já inicializou o primeiro hole do jogo e tem um valor inválido no pgi->hole (não é uma sequência de hole válida)
            {
                _smp.message_pool.getInstance().push(new message("[GameBase::requesPlace][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou pegar o hole[NUMERO=" + Convert.ToString(pgi.hole) + "] em que o player esta na sala[NUMERO=" + Convert.ToString(m_ri.numero) + "], mas ele esta carregando o course ou tem algum error.", type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            return (byte)hole;
        }

        // Verifica se o player já esteve na sala
        public virtual bool isGamingBefore(uint32_t _uid)
        {
            if (_uid == 0u)
                throw new exception("[GameBase::isGamingBefore][Error] _uid is invalid(zero)", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME,
                        1000, 0));

            return m_player_info.Any(_el =>
            {
                return _el.Value.uid == _uid;
            });
        }



        // Exclusivo do Modo Tourney
        public virtual void requestSendTimeGame(Player _session)
        {
            // ignore : UNREFERENCED_PARAMETER(_session);
        }

        public virtual void requestUpdateEnterAfterStartedInfo(Player _session, EnterAfterStartInfo _easi)
        {
            // ignore : UNREFERENCED_PARAMETER(_session);
            // ignore : UNREFERENCED_PARAMETER(_easi);
        }

        // Exclusivo do Grand Zodiac Modo
        public virtual void requestStartFirstHoleGrandZodiac(Player _session, packet _packet)
        {
            // ignore : UNREFERENCED_PARAMETER(_session);
            // ignore : UNREFERENCED_PARAMETER(_packet);
        }

        public virtual void requestReplyInitialValueGrandZodiac(Player _session, packet _packet)
        {
            // ignore : UNREFERENCED_PARAMETER(_session);
            // ignore : UNREFERENCED_PARAMETER(_packet);
        }

        public void requestReadSyncShotData(Player _session, packet _packet, ref ShotSyncData _ssd)
        {
            try
            {
                //check player connection
                if (!_session.getState())
                    throw new exception("[GameBase::requestReadSyncShotData][Error] player nao esta connectado.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME,
                           1, 0));

                //check packet
                if (_packet == null)
                    throw new exception("[GameBase::requestReadSyncShotData][Error] _packet is null", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME,
                            6, 0));

                //check size packet
                if (_packet.GetRemainingData.Length < 38 || _packet.GetRemainingData.Length > 38)
                    throw new exception($"[GameBase::requestReadSyncShotData][Error] DecryptShot" + (_packet.GetRemainingData.Length < 38 ? "is null" : _packet.GetRemainingData.Length > 38 ? "bad struct" : ""), ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME,
                       7, 0));

                //decript shot 
                _ssd = DecryptShot(_packet.GetRemainingData);

                if (_ssd == null)
                    throw new exception($"[GameBase::requestReadSyncShotData][Error] DecryptShot" + (_packet.GetRemainingData.Length < 38 ? "is null" : _packet.GetRemainingData.Length > 38 ? "bad struct" : ""), ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME,
                           8, 0));

                var oid = _ssd.oid;

                if (_ssd.oid == -1)
                    throw new exception($"[GameBase::requestReadSyncShotData][Error] Player no exist:" + oid, ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME,
                              9, 0));

                if (_ssd.pang > 37000u)
                    _smp.message_pool.getInstance().push(new message("[GameBase::requestReadSyncShotDate][Warning] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] pode esta usando hack, PANG[" + Convert.ToString(_ssd.pang) + "] maior que 40k. Hacker ou Bug.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                if (_ssd.bonus_pang > 10000u)
                    _smp.message_pool.getInstance().push(new message("[GameBase::requestReadSyncShotDate][Warning] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] pode esta usando hack, BONUS PANG[" + Convert.ToString(_ssd.bonus_pang) + "] maior que 10k. Hacker ou Bug.", type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
            catch (exception e)
            {
                _smp.message_pool.getInstance().push(new message("[GameBase::requestReadSyncShotData][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        // Usa o Padrão delas
        public bool stopTime()
        {
            clear_time();

            return true;
        }

        public bool pauseTime()
        {

            if (m_timer != null)
            {
                m_timer.Pause();


                _smp.message_pool.getInstance().push(new message("[GameBase::pauseTime][Log] pausou o Timer[Tempo=" + m_timer.getTimeLog() + "" + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));


                return true;
            }

            return false;
        }

        public bool finishTime()
        {
            if (m_timer != null)
                return m_timer.getState() == PangyaSyncTimer.TIMER_STATE.FINISH;

            return false;
        }

        public bool resumeTime()
        {
            if (m_timer != null)
            {
                m_timer.Resume();

                _smp.message_pool.getInstance().push(new message("[GameBase::resumerTime][Log] Retomou o Timer[Tempo=" + m_timer.getTimeLog() + "" + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                return true;
            }

            return false;
        }

        // Report Game
        public void requestPlayerReportChatGame(Player _session, packet _packet)
        {
            if (!_session.getState())
            {
                throw new exception("[GameBase::requestPlayerReportChatGame][Error] player nao esta connectado.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME,
                    1, 0));
            }
            ;
            if (_packet == null)
            {
                throw new exception("[GameBase::requestPlayerReportChatGame][Error] _packet is null", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME,
                    6, 0));
            }

            PangyaBinaryWriter p = new PangyaBinaryWriter();

            try
            {

                // Verifica se o player já reportou o jogo
                var it = m_player_report_game.find(_session.m_pi.uid);

                if (it.Key != 0)//(it.Key != m_player_report_game.end().Key)
                {

                    // Player já reportou o jogo
                    p.init_plain(0x94);

                    p.WriteByte(1); // Player já reportou o jogo

                    packet_func.session_send(p,
                        _session, 1);

                }
                else
                { // Primeira vez que o palyer report o jogo

                    // add ao mapa de uid de player que reportaram o jogo
                    m_player_report_game[_session.m_pi.uid] = _session.m_pi.uid;

                    // Faz Log de quem está na sala, quando pangya, o update enviar o chat log verifica o chat
                    // por que parece que o pangya não envia o chat, ele só cria um arquivo, acho que quem envia é o update
                    string log = "";

                    foreach (var el in m_players)
                    {
                        if (el != null)
                        {
                            log = log + "UID: " + Convert.ToString(_session.m_pi.uid) + "\tID: " + el.m_pi.id + "\tNICKNAME: " + el.m_pi.nickname + "\n";
                        }
                    }

                    // Log
                    _smp.message_pool.getInstance().push(new message("[GameBase::requestPlayerReportChatGame][Log] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] reportou o chat do jogo na sala[NUMERO=" + Convert.ToString(m_ri.numero) + "] Log{" + log + "}", type_msg.CL_FILE_LOG_AND_CONSOLE));

                    // Reposta para o cliente
                    p.init_plain(0x94);

                    p.WriteByte(0); // Sucesso

                    packet_func.session_send(p,
                        _session, 1);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[GameBase::requestPlayerReportChatGame][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x94);

                p.WriteByte(1); // 1 já foi feito report do jogo por esse player

                packet_func.session_send(p,
                    _session, 1);
            }
        }

        protected void initPlayersItemRainRate()
        {

            // Characters Equip
            foreach (var s in m_players)
            {
                if (s.getState() && s.isConnected())
                { // Check Player Connected

                    if (s.m_pi.ei.char_info == null)
                    { // Player não está com character equipado, kika dele do jogo
                        _smp.message_pool.getInstance().push(new message("[GameBase::initPlayersItemRainRate][Log] PLAYER[UID=" + Convert.ToString(s.m_pi.uid) + "] nao esta com Character equipado. kika ele do jogo. pode ser Bug.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                        continue;
                    }

                    // Devil Wings
                    if (s.m_pi.ei.char_info.parts_typeid.Any(_element =>
      devil_wings.Contains(_element)))
                    {
                        m_rv.rain += 10;
                    }

                    // Obsidian Wings
                    if (s.m_pi.ei.char_info.parts_typeid.Any(_element =>
      obsidian_wings.Contains(_element)))
                    {
                        m_rv.rain += 10;
                    }

                    // Corrupt Wings
                    if (s.m_pi.ei.char_info.parts_typeid.Any(_element =>
    corrupt_wings.Contains(_element)))
                    {
                        m_rv.rain += 15;
                    }

                    // Hasegawa Chirain
                    if (s.m_pi.ei.char_info.parts_typeid.Any(_element =>
      hasegawa_chirain.Contains(_element)))
                    {
                        m_rv.rain += 10;
                    }

                    // Hat Spooky Halloween -- Só funciona na época do Halloween (ex: outubro)
                    if (DateTime.Now.Month == 10 && s.m_pi.ei.char_info.parts_typeid.Any(_element => hat_spooky_halloween.Contains(_element)))
                    {
                        m_rv.rain += 10;
                    }


                    // Card Efeito 19 rate chuva
                    var it = s.m_pi.v_cei.FirstOrDefault(_el =>
                    {
                        return sIff.getInstance().getItemSubGroupIdentify22(_el._typeid) == 2 && _el.efeito == 19;
                    });

                    if (it != null)
                    {
                        if (it.efeito_qntd > 0)
                        {
                            m_rv.rain += it.efeito_qntd;
                        }
                    }

                    // Mascot Poltergeist -- Esse aqui "tenho que colocar a regra para funcionar só na epoca do halloween"
                    if (s.m_pi.ei.mascot_info != null && s.m_pi.ei.mascot_info._typeid == 0x40000029)
                    {
                        m_rv.rain += 10;
                    }

                    // Caddie Big Black Papel
                    if (s.m_pi.ei.cad_info != null && s.m_pi.ei.cad_info._typeid == 0x1C00000E)
                    {
                        m_rv.rain += 10;
                    }
                }
            }
        }

        public virtual void initPlayersItemRainPersistNextHole()
        {

            // Characters Equip
            foreach (var s in m_players)
            {
                if (s.getState() && s.isConnected())
                { // Check Player Connected

                    if (s.m_pi.ei.char_info == null)
                    { // Player não está com character equipado, kika dele do jogo
                        _smp.message_pool.getInstance().push(new message("[GameBase::initPlayersItemRainPersistNextHole][Log] PLAYER[UID=" + Convert.ToString(s.m_pi.uid) + "] nao esta com Character equipado. kika ele do jogo. pode ser Bug.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                        continue;
                    }

                    // Devil Wings
                    if (s.m_pi.ei.char_info.parts_typeid.Any(_element =>
      devil_wings.Contains(_element)))
                    {
                        // sai por que só precisa que 1 player tenha o item para valer para o game todo
                        m_rv.persist_rain = 1;
                        return;
                    }

                    // Obsidian Wings
                    if (s.m_pi.ei.char_info.parts_typeid.Any(_element =>
     obsidian_wings.Contains(_element)))
                    {
                        // sai por que só precisa que 1 player tenha o item para valer para o game todo
                        m_rv.persist_rain = 1;
                        return;
                    }

                    // Corrupt Wings
                    if (s.m_pi.ei.char_info.parts_typeid.Any(_element =>
     corrupt_wings.Contains(_element)))
                    {
                        // sai por que só precisa que 1 player tenha o item para valer para o game todo
                        m_rv.persist_rain = 1;
                        return;
                    }

                    // Hasegawa Chirain
                    if (s.m_pi.ei.char_info.parts_typeid.Any(_element =>
     hasegawa_chirain.Contains(_element)))
                    {
                        // sai por que só precisa que 1 player tenha o item para valer para o game todo
                        m_rv.persist_rain = 1;
                        return;
                    }

                    // Hat Spooky Halloween -- Esse aqui "tenho que colocar a regra para funcionar só na epoca do halloween"
                    if (DateTime.Now.Month == 10 && s.m_pi.ei.char_info.parts_typeid.Any(_element =>
     hat_spooky_halloween.Contains(_element)))
                    {
                        m_rv.persist_rain = 1;
                        return;
                    }


                    // Card Efeito 31 Persist chuva para o proximo hole

                    var it = s.m_pi.v_cei.FirstOrDefault(_el =>
                    {
                        return sIff.getInstance().getItemSubGroupIdentify22(_el._typeid) == 2 && _el.efeito == 31;
                    });

                    if (it != null)
                    {
                        // sai por que só precisa que 1 player tenha o item para valer para o game todo
                        m_rv.persist_rain = 1;
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// gerar o item do artefato, pode dar exp, pang, e etc...
        /// </summary>
        private void initArtefact()
        {

            switch (m_ri.typeid_artefatic)
            {
                // Artefact of EXP
                case ART_LUMINESCENT_CORAL:
                    m_rv.exp += 2;
                    break;
                case ART_TROPICAL_TREE:
                    m_rv.exp += 4;
                    break;
                case ART_TWIN_LUNAR_MIRROR:
                    m_rv.exp += 6;
                    break;
                case ART_MACHINA_WRENCH:
                    m_rv.exp += 8;
                    break;
                case ART_SILVIA_MANUAL:
                    m_rv.exp += 10;
                    break;
                // End
                // Artefact of Rain Rate
                case ART_SCROLL_OF_FOUR_GODS:
                    m_rv.rain += 5;
                    break;
                case ART_ZEPHYR_TOTEM:
                    m_rv.rain += 10;
                    break;
                case ART_DRAGON_ORB:
                    m_rv.rain += 20;
                    break;
                    // End
            }
        }

        private PlayerGameInfo.eCARD_WIND_FLAG getPlayerWindFlag(Player _session)
        {

            if (_session.m_pi.ei.char_info == null)
            { // Player n�o est� com character equipado, kika dele do jogo
                _smp.message_pool.getInstance().push(new message("[GameBase::getPlayerWindFlag][Log] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] nao esta com Character equipado. kika ele do jogo. pode ser Bug.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                return PlayerGameInfo.eCARD_WIND_FLAG.NONE;
            }

            // 3 R, 17 SR, 13 SC, 12 N

            var it = _session.m_pi.v_cei.FirstOrDefault(_el =>
            {
                return (_session.m_pi.ei.char_info.id == _el.parts_id && _session.m_pi.ei.char_info._typeid == _el.parts_typeid) && sIff.getInstance().getItemSubGroupIdentify22(_el._typeid) == 1 && (_el.efeito == 3 || _el.efeito == 17 || _el.efeito == 13 || _el.efeito == 12);
            });

            if (it != null)
            {
                switch (it.efeito)
                {
                    case 3:
                        return PlayerGameInfo.eCARD_WIND_FLAG.RARE;
                    case 12:
                        return PlayerGameInfo.eCARD_WIND_FLAG.NORMAL;
                    case 13:
                        return PlayerGameInfo.eCARD_WIND_FLAG.SECRET;
                    case 17:
                        return PlayerGameInfo.eCARD_WIND_FLAG.SUPER_RARE;
                }
            }

            return PlayerGameInfo.eCARD_WIND_FLAG.NONE;
        }

        public int initCardWindPlayer(PlayerGameInfo _pgi, byte _wind)
        {

            if (_pgi == null)
            {
                throw new exception("[GameBase::initCardWindPlayer][Error] PlayerGameInfo* _pgi is invalid(null). Ao tentar inicializar o card wind player no jogo. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME,
                    1, 4));
            }

            switch (_pgi.card_wind_flag)
            {
                case PlayerGameInfo.eCARD_WIND_FLAG.NORMAL:
                    if (_wind == 8) // 9m Wind
                    {
                        return -1;
                    }
                    break;
                case PlayerGameInfo.eCARD_WIND_FLAG.RARE:
                    if (_wind > 0) // All Wind
                    {
                        return -1;
                    }
                    break;
                case PlayerGameInfo.eCARD_WIND_FLAG.SUPER_RARE:
                    if (_wind >= 5) // High(strong) Wind
                    {
                        return -2;
                    }
                    break;
                case PlayerGameInfo.eCARD_WIND_FLAG.SECRET:
                    if (_wind >= 5) // High(strong) Wind
                    {
                        return -2;
                    }
                    else if (_wind > 0) // Low(weak) Wind, 1m não precisa diminuir
                    {
                        return -1;
                    }
                    break;
            }

            return 0;
        }

        private PlayerGameInfo.stTreasureHunterInfo getPlayerTreasureInfo(Player _session)
        {

            PlayerGameInfo.stTreasureHunterInfo pti = new PlayerGameInfo.stTreasureHunterInfo();

            if (_session.m_pi.ei.char_info == null)
            { // Player não está com character equipado, kika dele do jogo
                _smp.message_pool.getInstance().push(new message("[GameBase::getPlayerTreasureInfo][Log] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] nao esta com Character equipado. kika ele do jogo. pode ser Bug.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                return pti;
            }

            List<CardEquipInfoEx> v_cei = new List<CardEquipInfoEx>();

            // 9 N, 10 R, 14 SR por Score. 8 N, R, SR todos score
            _session.m_pi.v_cei.ToList().ForEach(_el =>
             {
                 if ((_session.m_pi.ei.char_info.id == _el.parts_id && _session.m_pi.ei.char_info._typeid == _el.parts_typeid)
                     && sIff.getInstance().getItemSubGroupIdentify22(_el._typeid) == 1
                     && (_el.efeito == 8 || _el.efeito == 9 || _el.efeito == 10 || _el.efeito == 14))
                 {
                     v_cei.Add(_el);
                 }
             });

            if (v_cei.Count > 0)
            {
                foreach (var el in v_cei)
                {
                    switch (el.efeito)
                    {
                        case 8: // Todos Score
                            pti.all_score = (byte)el.efeito_qntd;
                            break;
                        case 9: // Par
                            pti.par_score = (byte)el.efeito_qntd;
                            break;
                        case 10: // Birdie
                            pti.birdie_score = (byte)el.efeito_qntd;
                            break;
                        case 14: // Eagle
                            pti.eagle_score = (byte)el.efeito_qntd;
                            break;
                    }
                }
            }

            // Card Efeito 18 Aumenta o treasure point para qualquer score por 2 horas

            var it = _session.m_pi.v_cei.FirstOrDefault(_el =>
            {
                return sIff.getInstance().getItemSubGroupIdentify22(_el._typeid) == 2 && _el.efeito == 18;
            });

            if (it != null)
            {
                pti.all_score += (byte)it.efeito_qntd;
            }

            // Verifica se está com asa de anjo equipada (shop ou gacha), aumenta 30 treasure hunter point para todos scores
            if (_session.m_pi.ei.char_info.AngelEquiped() == 1 && _session.m_pi.ui.getQuitRate() < GOOD_PLAYER_ICON)
            {
                pti.all_score += 30; // +30 all score
            }

            return pti;
        }

        public virtual void updatePlayerAssist(Player _session)
        {

            var pgi = getPlayerInfo((_session));
            if (pgi == null)
            {
                throw new exception("[GameBase::" + "updatePlayerAssist][Error] PLAYER[UID=" + Convert.ToString((_session).m_pi.uid) + "] " + "tentou atualizar assist pang no jogo" + ", mas o game nao tem o info dele guardado. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME,
                    1, 4));
            }

            if (pgi.assist_flag == 1 && pgi.level > 10)
                pgi.data.pang = Convert.ToUInt64(pgi.data.pang * 0.7f); // - 30% dos pangs
        }

        public virtual void initGameTime()
        {
            m_start_time = DateTime.Now;
        }

        public virtual uint32_t getRankPlace(Player _session)
        {

            var pgi = getPlayerInfo((_session));
            if (pgi == null)
            {
                throw new exception("[GameBase::" + "getRankPlace][Error] PLAYER[UID=" + Convert.ToString((_session).m_pi.uid) + "] " + "tentou pegar o lugar no rank do jogo" + ", mas o game nao tem o info dele guardado. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME,
                    1, 4));
            }

            int index = m_player_order.IndexOf(pgi);

            return (index != -1) ? (uint)index : uint32_t.MaxValue;
        }

        public virtual DropItemRet requestInitDrop(Player _session)
        {

            if (!sDropSystem.getInstance().isLoad())
            {
                sDropSystem.getInstance().load();
            }

            DropItemRet dir = new DropItemRet();

            var pgi = getPlayerInfo((_session));
            if (pgi == null)
            {
                throw new exception("[GameBase::" + "requestInitDrop][Error] PLAYER[UID=" + Convert.ToString((_session).m_pi.uid) + "] " + "tentou inicializar drop do hole no jogo" + ", mas o game nao tem o info dele guardado. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME,
                    1, 4));
            }

            DropSystem.stCourseInfo ci = new DropSystem.stCourseInfo();

            var hole = m_course.findHole(pgi.hole);

            if (hole == null)
            {
                throw new exception("[GameBase::requestInitDrop][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou inicializar Drop System do hole[NUMERO=" + Convert.ToString(pgi.hole) + "] no jogo, mas nao encontrou o hole no course do game. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME,
                    200, 0));
            }

            // Init Course Info Drop System
            ci.artefact = m_ri.typeid_artefatic;
            ci.char_motion = pgi.char_motion_item;
            ci.course = (byte)(hole.getCourse() & 0x7F); // Course do Hole, Por que no SSC, cada hole é um course
            ci.hole = pgi.hole;
            ci.seq_hole = (byte)m_course.findHoleSeq(pgi.hole);
            ci.qntd_hole = m_ri.qntd_hole;
            ci.rate_drop = pgi.used_item.rate.drop;

            if (_session.m_pi.ei.char_info != null && _session.m_pi.ui.getQuitRate() < GOOD_PLAYER_ICON)
            {
                ci.angel_wings = _session.m_pi.ei.char_info.AngelEquiped();
            }
            else
            {
                ci.angel_wings = 0;
            }

            // Artefact Pang Drop
            if (m_ri.qntd_hole == ci.seq_hole && m_ri.qntd_hole == 18)
            { // Ultimo Hole, de 18h Game
                var art_pang = sDropSystem.getInstance().drawArtefactPang(ci, (uint32_t)m_players.Count());

                if (art_pang._typeid != 0)
                { // Dropou

                    dir.v_drop.Add(art_pang);

                    if (art_pang.qntd >= 30)
                    { // Envia notice que o player ganhou jackpot

                        PangyaBinaryWriter p = new PangyaBinaryWriter((ushort)0x40);

                        p.WriteByte(10); // JackPot

                        p.WriteString(_session.m_pi.nickname);

                        p.WriteUInt16(0); // size Msg

                        p.WriteInt32(art_pang.qntd * 500);

                        packet_func.game_broadcast(this,
                            p.GetBytes, 1);
                    }
                }
            }

            // Drop Event Course
            var course = sDropSystem.getInstance().findCourse((byte)(ci.course & 0x7F));

            if (course != null)
            { // tem Drop nesse Course
                var drop_event = sDropSystem.getInstance().drawCourse(course, ci);

                if (!drop_event.empty()) // Dropou
                {
                    dir.v_drop.AddRange(dir.v_drop);
                }
            }

            // Drop Mana Artefact
            var mana_drop = sDropSystem.getInstance().drawManaArtefact(ci);

            if (mana_drop._typeid != 0) // Dropou
            {
                dir.v_drop.Add(mana_drop);
            }

            // Drop Grand Prix Ticket, não drop no Grand Prix
            if (m_ri.qntd_hole == ci.seq_hole && m_ri.getTipo() != RoomInfo.ROOM_INFO_TYPE.GRAND_PRIX)
            {
                var gp_ticket = sDropSystem.getInstance().drawGrandPrixTicket(ci, _session);

                if (gp_ticket._typeid != 0) // Dropou
                {
                    dir.v_drop.Add(gp_ticket);
                }
            }

            // SSC Ticket
            var ssc = sDropSystem.getInstance().drawSSCTicket(ci);

            if (!ssc.empty())
            {
                dir.v_drop.AddRange(ssc);

                // SSC Ticket Achievement
                pgi.sys_achieve.incrementCounter(0x6C400053u, (int)ssc.Count);
            }

            // Adiciona para a lista de drop's do player
            if (!dir.v_drop.empty())
            {
                pgi.drop_list.v_drop.AddRange(dir.v_drop);

            }

            return (dir);
        }


        public void requestSaveDrop(Player _session)
        {

            var pgi = getPlayerInfo((_session));
            if (pgi == null)
            {
                throw new exception("[GameBase::" + "requestSaveDrop][Error] PLAYER[UID=" + Convert.ToString((_session).m_pi.uid) + "] " + "tentou salvar drop item no jogo" + ", mas o game nao tem o info dele guardado. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME,
                    1, 4));
            }
            pgi.drop_list.v_drop = pgi.drop_list.v_drop.Where(c => c._typeid != 0).ToList();
            if (pgi.drop_list.v_drop.Count > 0)
            {
                List<stItem> v_item = new List<stItem>();

                foreach (var el in pgi.drop_list.v_drop)
                {
                    stItem item = new stItem
                    {
                        type = 2,
                        _typeid = el._typeid,
                        qntd = (int)((el.type == DropItem.eTYPE.QNTD_MULTIPLE_500) ? el.qntd * 500 : el.qntd)
                    }; // <- cria um NOVO objeto a cada iteração 
                    item.STDA_C_ITEM_QNTD = (short)item.qntd;

                    var existente = v_item.FirstOrDefault(c => c._typeid == item._typeid);
                    if (existente == null)
                    {
                        // Novo item
                        v_item.Add(new stItem(item));
                    }
                    else
                    {
                        // Já existe, soma quantidade
                        existente.qntd += item.qntd;
                        existente.STDA_C_ITEM_QNTD = (short)existente.qntd;
                        v_item.Add(new stItem(_item: existente));
                    }
                }

                var rai = ItemManager.addItem(v_item, _session.getUID(), 0, 0);

                if (rai.fails.Any() && rai.type != ItemManager.RetAddItem.T_SUCCESS_PANG_AND_EXP_AND_CP_POUCH)
                {
                    _smp.message_pool.getInstance().push(
                        new message("[Game:requestSaveDrop][WARNIG] nao conseguiu adicionar os drop itens. Bug",
                        type_msg.CL_FILE_LOG_AND_CONSOLE));
                }

                PangyaBinaryWriter p = new PangyaBinaryWriter((ushort)0x216);

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

                packet_func.session_send(p, _session, 1);
            }
        }

        public DropItemRet requestInitCubeCoin(Player _session, packet _packet)
        {
            if (!_session.getState())
            {
                throw new exception("[GameBase::requestInitCubeCoin][Error] player nao esta connectado.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME,
                    1, 0));
            }
            ;
            if (_packet == null)
            {
                throw new exception("[GameBase::requestInitCubeCoin][Error] _packet is null", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME,
                    6, 0));
            }

            try
            {

                byte opt = _packet.ReadByte();
                byte count = _packet.ReadByte();

                // Player que tacou e tem drops (Coin ou Cube)
                if (opt == 1 && count > 0)
                {

                    DropItemRet dir = new DropItemRet();

                    var pgi = getPlayerInfo((_session));
                    if (pgi == null)
                    {
                        throw new exception("[GameBase::" + "initCubeCoin][Error] PLAYER[UID=" + Convert.ToString((_session).m_pi.uid) + "] " + "tentou terninar o hole no jogo" + ", mas o game nao tem o info dele guardado. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME,
                            1, 4));
                    }

                    var hole = m_course.findHole(pgi.hole);

                    if (hole == null)
                    {
                        throw new exception("[GameBase::requestInitCubeCoin][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou terminar hole[NUMERO=" + Convert.ToString((ushort)pgi.hole) + "], mas no course nao tem esse hole. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME,
                            250, 0));
                    }

                    uint32_t tipo = 0;
                    uint32_t id = 0;

                    CubeEx pCube = null;

                    for (var i = 0; i < count; ++i)
                    {

                        tipo = _packet.ReadByte();
                        id = _packet.ReadUInt32();

                        pCube = hole.findCubeCoin(id);

                        if (pCube == null)
                        {
                            throw new exception("[GameBase::requestInitCubeCoin][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou terminar hole[NUMERO=" + Convert.ToString((ushort)pgi.hole) + "], mas o cliente forneceu um cube/coin id[ID=" + Convert.ToString(id) + "] invalido. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME,
                                251, 0));
                        }
                        if (tipo == 0)
                        // Coin
                        {
                            // Tipo 3 Coin da borda do green ganha menos pangs ganha de 1 a 50, Tipo 4 Coin no chão qualquer lugar ganha mais pang de 1 a 200
                            dir.v_drop.Add(new DropItem(
                                COIN_TYPEID,
                                (byte)hole.getCourse(),
                                (byte)hole.getNumero(),
                                (short)((new Random().Next() % ((uint)(pCube.flag_location == 0 ? 50 : 200))) + 1),
                                (pCube.flag_location == 0) ? DropItem.eTYPE.COIN_EDGE_GREEN : DropItem.eTYPE.COIN_GROUND
                            ));

                            pgi.drop_list.v_drop.Add((new DropItem(
                                COIN_TYPEID,
                                (byte)hole.getCourse(),
                                (byte)hole.getNumero(),
                                (short)((new Random().Next() % ((uint)(pCube.flag_location == 0 ? 50 : 200))) + 1),
                                (pCube.flag_location == 0) ? DropItem.eTYPE.COIN_EDGE_GREEN : DropItem.eTYPE.COIN_GROUND
                            )));
                        }
                        if (tipo == 1) // Cube
                        {
                            // Add os Cube Coin para o player list drop
                            pgi.drop_list.v_drop.Add(new DropItem(SPINNING_CUBE_TYPEID, (byte)hole.getCourse(), (byte)hole.getNumero(), 1, DropItem.eTYPE.CUBE));
                            dir.v_drop.Add(new DropItem(SPINNING_CUBE_TYPEID, (byte)hole.getCourse(), (byte)hole.getNumero(), 1, DropItem.eTYPE.CUBE));
                        }
                    }
                    return (dir);
                }
            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[GameBase::requestInitCubeCoin][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            return new DropItemRet();
        }

        public virtual void requestCalculePang(Player _session)
        {
            var pgi = getPlayerInfo(_session);
            if (pgi == null)
            {
                throw new exception("[GameBase::requestCalculePang][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou calcular o pang, mas o info não existe.",
                    ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME, 1, 4));
            }

            // 1 e 2. Busca informações do Course (Campo)
            var course = sIff.getInstance().findCourse((uint)((int)m_ri.course & 0x7F) | 0x28000000u);
            float course_rate = (course != null && course.RatePang >= 1.0f) ? course.RatePang : 1.0f;

            // 3. Cálculo do Rate Total
            uint base_rate = (uint)(m_rv.pang + (m_ri.modo == (byte)RoomInfo.ROOM_INFO_MODO.M_SHUFFLE ? 10 : 0));

            // IMPORTANTE: Se o item_rate estiver multiplicando o base_rate, o valor explode.
            float item_rate = TRANSF_SERVER_RATE_VALUE(pgi.used_item.rate.pang);
            float server_rate = TRANSF_SERVER_RATE_VALUE(base_rate);

            float pang_rate = item_rate * server_rate * course_rate;

            // 1. Cálculo do Bônus Bruto
            // (Pang da partida * taxa) - Pang da partida = Apenas o bônus extra 
            uint64_t novo_bonus = (uint64_t)((pgi.data.pang * pang_rate) - pgi.data.pang) + pgi.data.bonus_pang;
            // 2. TAXA DE 10% (O servidor "come" 10%, sobra 90% para o player)
            novo_bonus = (uint64_t)(novo_bonus * 0.90f);

            // 6. TRAVA DE SEGURANÇA (Hard Cap)
            // Se após a taxa ainda for maior que 20.000, limitamos para proteger a economia
            if (novo_bonus > 20000)
            {
                novo_bonus = 20000 + (uint64_t)((novo_bonus - 20000) * 0.1f);
            }

            pgi.data.bonus_pang = novo_bonus;
        }

        public virtual void requestSaveInfo(Player _session, int option)
        {

            var pgi = getPlayerInfo((_session));
            if (pgi == null)
                return;

            try
            {

                // Aqui dados do jogo ele passa o holein no lugar do mad_conduta <-> holein, agora quando ele passa o info user é invertido(Normal)
                // Inverte para salvar direito no banco de dados
                var tmp_holein = pgi.ui.hole_in;

                pgi.ui.hole_in = pgi.ui.mad_conduta;
                pgi.ui.mad_conduta = tmp_holein;

                if (option == 0)
                { // Terminou VS

                    // Verifica se o Angel Event está ativo de tira 1 quit do player que concluí o jogo
                    if (m_ri.angel_event)
                    {
                        pgi.ui.quitado = -1;
                    }

                    pgi.ui.exp = 0;
                    pgi.ui.combo = 1;
                    pgi.ui.jogado = 1;
                    pgi.ui.media_score = pgi.data.score;

                    // Os valores que eu não colocava
                    pgi.ui.jogados_disconnect = 1; // Esse aqui é o contador de jogos que o player começou é o mesmo do jogado, só que esse aqui usa para o disconnect

                    var diff = UtilTime.GetLocalDateDiff(m_start_time);

                    if (diff > 0)
                    {
                        diff /= STDA_10_MICRO_PER_SEC; // NanoSeconds To Seconds
                    }

                    pgi.ui.tempo = (int32_t)diff;

                }
                else if (option == 1)
                { // Quitou ou tomou DC

                    // Quitou ou saiu não ganha pangs
                    pgi.data.pang = 0;
                    pgi.data.bonus_pang = 0;

                    pgi.ui.exp = 0;
                    pgi.ui.combo = (int)(DECREASE_COMBO_VALUE * -1);
                    pgi.ui.jogado = 1;

                    // Verifica se tomou DC ou Quitou, ai soma o membro certo
                    if (!_session.m_connection_timeout)
                    {
                        pgi.ui.quitado = 1;
                    }
                    else
                    {
                        pgi.ui.disconnect = 1;
                    }

                    // Os valores que eu não colocava
                    pgi.ui.jogados_disconnect = 1; // Esse aqui é o contador de jogos que o player começou é o mesmo do jogado, só que esse aqui usa para o disconnect

                    pgi.ui.media_score = pgi.data.score;

                    var diff = UtilTime.GetLocalDateDiff(m_start_time);

                    if (diff > 0)
                    {
                        diff /= STDA_10_MICRO_PER_SEC; // NanoSeconds To Seconds
                    }

                    pgi.ui.tempo = (int32_t)diff;

                }
                else if (option == 2)
                { // Não terminou o hole 1, alguem saiu ai volta para sala sem contar o combo, só conta o jogo que começou

                    pgi.data.pang = 0;
                    pgi.data.bonus_pang = 0;

                    pgi.ui.exp = 0;
                    pgi.ui.jogado = 1;

                    // Os valores que eu não colocava
                    pgi.ui.jogados_disconnect = 1; // Esse aqui é o contador de jogos que o player começou é o mesmo do jogado, só que esse aqui usa para o disconnect

                    var diff = UtilTime.GetLocalDateDiff(m_start_time);

                    if (diff > 0)
                    {
                        diff /= STDA_10_MICRO_PER_SEC; // NanoSeconds To Seconds
                    }

                    pgi.ui.tempo = (int32_t)diff;

                }
                else if (option == 4)
                { // SSC

                    pgi.ui.clear();

                    // Verifica se o Angel Event está ativo de tira 1 quit do player que concluí o jogo
                    if (m_ri.angel_event)
                    {

                        pgi.ui.quitado = -1;
                    }

                    pgi.ui.exp = 0;
                    pgi.ui.combo = 1;
                    pgi.ui.jogado = 1;
                    pgi.ui.media_score = 0;

                    // Os valores que eu não colocava
                    pgi.ui.jogados_disconnect = 1; // Esse aqui é o contador de jogos que o player começou é o mesmo do jogado, só que esse aqui usa para o disconnect

                    var diff = UtilTime.GetLocalDateDiff(m_start_time);

                    if (diff > 0)
                    {
                        diff /= STDA_10_MICRO_PER_SEC;
                    }

                    pgi.ui.tempo = (int32_t)diff;

                }
                else if (option == 5)
                {

                    // Quitou ou saiu não ganha pangs
                    pgi.data.pang = 0;
                    pgi.data.bonus_pang = 0;

                    pgi.ui.exp = 0;
                    pgi.ui.jogado = 1;
                    pgi.ui.media_score = pgi.data.score;

                    // Os valores que eu não colocava
                    pgi.ui.jogados_disconnect = 1; // Esse aqui é o contador de jogos que o player começou é o mesmo do jogado, só que esse aqui usa para o disconnect

                    var diff = UtilTime.GetLocalDateDiff(m_start_time);

                    if (diff > 0)
                    {
                        diff /= STDA_10_MICRO_PER_SEC; // NanoSeconds To Seconds
                    }

                    pgi.ui.tempo = (int32_t)diff;
                }

                // Achievement Records
                records_player_achievement(_session);

                // Pode tirar pangs
                int64_t total_pang = (long)(pgi.data.pang + pgi.data.bonus_pang);

                // UPDATE ON SERVER AND DB
                _session.m_pi.addUserInfo(pgi.ui, (ulong)total_pang); // add User Info

                if (total_pang > 0)
                {
                    _session.m_pi.addPang((ulong)total_pang); // add Pang
                }
                else if (total_pang < 0)
                {
                    _session.m_pi.consomePang((ulong)(total_pang * -1)); // consome Pangs
                }

                // Game Combo
                if (_session.m_pi.ui.combo > 0)
                {
                    pgi.sys_achieve.incrementCounter(0x6C40004Bu, _session.m_pi.ui.combo);
                }

            }
            catch (exception e)
            {
                _smp.message_pool.getInstance().push(new message("[GameBase::requestSaveInfo][Error] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public virtual void requestUpdateItemUsedGame(Player _session)
        {

            var pgi = getPlayerInfo((_session));
            if (pgi == null)
            {
                throw new exception("[GameBase::" + "requestUpdateItemUsedGame][Error] PLAYER[UID=" + Convert.ToString((_session).m_pi.uid) + "] " + "tentou atualizar itens usado no jogo" + ", mas o game nao tem o info dele guardado. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME,
                    1, 4));
            }

            var ui = pgi.used_item;

            // Club Mastery // ((int)((int)m_ri.course & 0x7F) == RoomInfo.ROOM_INFO_TYPE.SPECIAL_SHUFFLE_COURSE ? 1.5f : 1.f), SSSC sobrecarrega essa função para colocar os valores dele
            ui.club.count += (uint32_t)(1.0f * 10.0f * ui.club.rate * TRANSF_SERVER_RATE_VALUE(m_rv.clubset) * TRANSF_SERVER_RATE_VALUE(ui.rate.club));

            // Passive Item exceto Time Booster e var Command, que soma o contador por uso, o cliente passa o pacote, dizendo que usou o item
            foreach (var el in ui.v_passive)
            {

                // Verica se é o ultimo hole, terminou o jogo, ai tira soma 1 ao count do pirulito que consome por jogo
                if (CHECK_PASSIVE_ITEM(el.Value._typeid)
                    && el.Value._typeid != TIME_BOOSTER_TYPEID
                    && el.Value._typeid != AUTO_COMMAND_TYPEID)
                {

                    // Item de Exp Boost que só consome 1 Por Jogo, só soma no requestFinishItemUsedGame
                    if (passive_item_exp_1perGame.Contains(el.Value._typeid))
                    {
                        el.Value.count++;
                    }

                }
                else if (sIff.getInstance().getItemGroupIdentify(el.Value._typeid) == PangyaAPI.IFF.JP.Models.Flags.IFF_GROUP.BALL || sIff.getInstance().getItemGroupIdentify(el.Value._typeid) == PangyaAPI.IFF.JP.Models.Flags.IFF_GROUP.AUX_PART) //AuxPart(Anel)
                {
                    el.Value.count++;
                }
            }
        }

        public virtual void requestFinishItemUsedGame(Player _session)
        {

            List<stItemEx> v_item = new List<stItemEx>();

            var pgi = getPlayerInfo((_session));
            if (pgi == null)
            {
                throw new exception("[GameBase::" + "requestFinishItemUsedGame][Error] PLAYER[UID=" + Convert.ToString((_session).m_pi.uid) + "] " + "tentou finalizar itens usado no jogo" + ", mas o game nao tem o info dele guardado. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME,
                    1, 4));
            }

            // Player já finializou os itens usados, verifica para não finalizar dua vezes os itens do player
            if (pgi.finish_item_used == 1)
            {

                _smp.message_pool.getInstance().push(new message("[GameBase::requestFinishItemUsedGame][Warning] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] ja finalizou os itens. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                return;
            }

            var ui = pgi.used_item;

            // Add +1 ao itens que consome 1 só por jogo
            // Item de Exp Boost que só consome 1 Por Jogo
            foreach (var _el in ui.v_passive)
            {
                if (passive_item_exp_1perGame.Contains(_el.Value._typeid))
                {
                    _el.Value.count++;
                }
            }


            // Verifica se é premium 2 e se ele tem o var caliper para poder somar no Achievement
            if (_session.m_pi.m_cap.premium_user && sPremiumSystem.getInstance().isPremium(_session.m_pi.pt._typeid))
            {

                var it_ac = ui.v_passive.find(AUTO_CALIPER_TYPEID);

                if (it_ac.Key != 0)
                {

                    int qntd = m_course.findHoleSeq(pgi.hole);

                    if (qntd == ~0)
                    {
                        qntd = m_ri.qntd_hole;
                    }

                    // Adiciona var Caliper para ser contado no Achievement
                    var it_p_ac = ui.v_passive.insert(AUTO_CALIPER_TYPEID,
                        new UsedItem.Passive(typeid: AUTO_CALIPER_TYPEID, _count: qntd));

                    if (!(it_p_ac.Value != null) && it_p_ac.Key != 0)
                    {
                        // Log
                        _smp.message_pool.getInstance().push(new message("[GameBase::requestFinishItemUsedGame][Error][Warning] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] nao conseguiu adicionar o var Caliper passive item para adicionar no contador do Achievement, por que ele eh premium user 2", type_msg.CL_FILE_LOG_AND_CONSOLE));
                    }
                }
            }

            // Passive Item
            foreach (var el in ui.v_passive)
            {

                if (el.Value.count > 0)
                {

                    // Item Aqui tem o Achievemente de passive item
                    if (sIff.getInstance().getItemGroupIdentify(el.Value._typeid) == PangyaAPI.IFF.JP.Models.Flags.IFF_GROUP.ITEM && !sIff.getInstance().IsItemEquipable(el.Value._typeid))
                    {

                        pgi.sys_achieve.incrementCounter(0x6C400075u, (int)el.Value.count);

                        uint tmp_counter_typeid;
                        if ((tmp_counter_typeid = AchievementSystem.getPassiveItemCounterTypeId(el.Value._typeid)) > 0)
                        {
                            pgi.sys_achieve.incrementCounter(tmp_counter_typeid, (int)el.Value.count);
                        }
                    }

                    // Só atualiza o var Caliper se não for Premium 2
                    if (!_session.m_pi.m_cap.premium_user
                        || !sPremiumSystem.getInstance().isPremium(_session.m_pi.pt._typeid)
                        || el.Value._typeid != AUTO_CALIPER_TYPEID)
                    {

                        // Tira todos itens passivo, antes estava Item e AuxPart, não ia Ball por que eu fiz errado, só preciso verifica se é item e passivo para somar o achievement
                        // Para tirar os itens, tem que tirar(atualizar) todos.
                        var pWi = _session.m_pi.findWarehouseItemByTypeid(el.Value._typeid);

                        if (pWi != null)
                        {

                            // Init Item
                            var item = new stItemEx();

                            item.type = 2;
                            item._typeid = pWi._typeid;
                            item.id = pWi.id;
                            item.qntd = (int)el.Value.count;
                            item.STDA_C_ITEM_QNTD = (short)((short)item.qntd * -1);

                            // Add On Vector
                            v_item.Add(new stItemEx(item));


                        }
                        else
                        {
                            _smp.message_pool.getInstance().push(new message("[GameBase::requestFinishItemUsedGame][Warning] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou atualizar item[TYPEID=" + Convert.ToString(el.Value._typeid) + "] que ele nao possui. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                        }
                    }
                }
            }

            // Active Item
            foreach (var el in ui.v_active)
            {

                if (el.Value.count > 0)
                {

                    // Aqui tem achievement de Item Active
                    if (sIff.getInstance().getItemGroupIdentify(el.Value._typeid) == PangyaAPI.IFF.JP.Models.Flags.IFF_GROUP.ITEM && sIff.getInstance().IsItemEquipable(el.Value._typeid))
                    {

                        pgi.sys_achieve.incrementCounter(0x6C40004Fu, (int)el.Value.count);

                        uint tmp_counter_typeid;
                        if ((tmp_counter_typeid = AchievementSystem.getActiveItemCounterTypeId(el.Value._typeid)) > 0)
                        {
                            pgi.sys_achieve.incrementCounter(tmp_counter_typeid, (int)el.Value.count);
                        }
                    }

                    // Só tira os itens Active se a sala não estiver com o artefact Frozen Flame,
                    // se ele estiver com artefact Frozen Flame ele mantém os Itens Active, não consome e nem desequipa do inventório do player
                    if (m_ri.typeid_artefatic != ART_FROZEN_FLAME)
                    {

                        // Limpa o Item Slot do player, dos itens que foram usados(Ativados) no jogo
                        if (el.Value.count <= el.Value.v_slot.Count)
                        {

                            for (var i = 0; i < el.Value.count; ++i)
                            {
                                _session.m_pi.ue.item_slot[el.Value.v_slot[i]] = 0;
                            }
                        }

                        var pWi = _session.m_pi.findWarehouseItemByTypeid(el.Value._typeid);

                        if (pWi != null)
                        {
                            // Init Item
                            var item = new stItemEx();

                            item.type = 2;
                            item._typeid = pWi._typeid;
                            item.id = pWi.id;
                            item.qntd = (int)el.Value.count;
                            item.STDA_C_ITEM_QNTD = (short)(item.qntd * -1);

                            // Add On Vector
                            v_item.Add(new stItemEx(item));

                        }
                        else
                        {
                            _smp.message_pool.getInstance().push(new message("[GameBase::requestFinishItemUsedGame][Warning] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou atualizar item[TYPEID=" + Convert.ToString(el.Value._typeid) + "] que ele nao possui. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                        }
                    }
                }
            }

            // Update Item Equiped Slot ON DB
            snmdb.NormalManagerDB.getInstance().add(25,
                new CmdUpdateItemSlot(_session.m_pi.uid, _session.m_pi.ue.item_slot),
                SQLDBResponse, this);

            // Se for o Master da sala e ele estiver com artefato tira o mana dele
            // Antes tirava assim que começava o jogo, mas aí o cliente atualizava a sala tirando o artefact aí no final não tinha como ver se o frozen flame estava equipado
            // e as outras pessoas que estão na lobby não sabe qual artefect que está na sala, por que o master mesmo mando o pacote pra tirar da sala quando o server tira o mana dele no init game
            if (m_ri.typeid_artefatic != 0 && m_ri.master == _session.m_pi.uid)
            {

                // Tira Artefact Mana do master da sala
                var pWi = _session.m_pi.findWarehouseItemByTypeid(m_ri.typeid_artefatic + 1);

                if (pWi != null)
                {

                    var item = new stItemEx();

                    item.type = 2;
                    item.id = pWi.id;
                    item._typeid = pWi._typeid;
                    item.qntd = (int)((pWi.STDA_C_ITEM_QNTD <= 0) ? 1 : pWi.STDA_C_ITEM_QNTD);
                    item.STDA_C_ITEM_QNTD = (short)(item.qntd * -1);

                    // Add on Vector Update Itens
                    v_item.Add(new stItemEx(item));

                }
                else
                {
                    _smp.message_pool.getInstance().push(new message("[GameBase::requestFinishItemUsedGame][Warning] Master[UID=" + Convert.ToString(_session.m_pi.uid) + "] do jogo nao tem Mana do Artefect[TYPEID=" + Convert.ToString(m_ri.typeid_artefatic) + ", MANA=" + Convert.ToString(m_ri.typeid_artefatic + 1) + "] e criou e comecou um jogo com artefact sem mana. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
            }

            // Update Item ON Server AND DB
            if (v_item.Count > 0)
            {

                if (ItemManager.removeItem(v_item, _session) <= 0)
                {
                    _smp.message_pool.getInstance().push(new message("[GameBase::requestFinishItemUsedGame][Warning] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] nao conseguiu deletar os item do player. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
            }

            // Club Mastery
            if (ui.club.count != 0 && ui.club._typeid != 0)
            {

                var pClub = _session.m_pi.findWarehouseItemByTypeid(ui.club._typeid);

                if (pClub != null)
                {

                    pClub.clubset_workshop.mastery += ui.club.count;

                    var item = new stItemEx();

                    item.type = 0xCC;
                    item.id = (int)pClub.id;
                    item._typeid = pClub._typeid;

                    item.clubset_workshop.c = pClub.clubset_workshop.c;

                    item.clubset_workshop.level = (byte)pClub.clubset_workshop.level;
                    item.clubset_workshop.mastery = pClub.clubset_workshop.mastery;
                    item.clubset_workshop.rank = (uint)pClub.clubset_workshop.rank;
                    item.clubset_workshop.recovery = pClub.clubset_workshop.recovery_pts;

                    snmdb.NormalManagerDB.getInstance().add(12,
                        new CmdUpdateClubSetWorkshop(_session.m_pi.uid,
                            pClub,
                            CmdUpdateClubSetWorkshop.FLAG.F_TRANSFER_MASTERY_PTS),
                        SQLDBResponse, this);

                    v_item.Add(new stItemEx(item));
                }
                else
                {
                    _smp.message_pool.getInstance().push(new message("[GameBase::requestFinishItemUsedGame][Warning] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou salvar mastery do ClubSet[TYPEID=" + Convert.ToString(ui.club._typeid) + "] que ele nao tem. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
            }

            // Flag de que o palyer já finalizou os itens usados no jogo, para não finalizar duas vezes
            pgi.finish_item_used = 1;

            // Atualiza ON Jogo
            if (v_item.Count > 0)
            {
                PangyaBinaryWriter p = new PangyaBinaryWriter((ushort)0x216);

                p.WriteUInt32((uint32_t)UtilTime.GetSystemTimeAsUnix());
                p.WriteUInt32((uint32_t)v_item.Count);

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
            }
        }

        /// <summary>
        /// 100%, verificar o acerto o hole 100%
        /// </summary>
        /// <param name="_session"></param>
        /// <param name="option"></param>
        /// <exception cref="exception"></exception>
        public virtual void requestFinishHole(Player _session, int option)
        {
            var pgi = getPlayerInfo(_session);
            if (pgi == null)
            {
                throw new exception($"[GameBase::requestFinishHole][Error] PLAYER[UID={_session.m_pi.uid}] tentou finalizar dados do hole, mas info não guardada.",
                    ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME, 1, 4));
            }

            if (pgi.hole == 255)
                return;

            var hole = m_course.findHole(pgi.hole);

            if (hole == null)
            {
                throw new exception($"[GameBase::finishHole][Error] PLAYER[UID={_session.m_pi.uid}] hole[NUMERO={(ushort)pgi.hole}] inválido.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME, 20, 0));
            }

            // Variáveis locais para garantir a precisão do cálculo ANTES de manipular o objeto global
            int score_hole = 0;
            int tacada_hole_atual = pgi.data.tacada_num;
            int par_do_hole = hole.getPar().par;

            // --- BLOCO DE CÁLCULO ---
            if (option == 0) // Terminou o buraco normalmente
            {
                // 1. Atualiza totais da partida
                pgi.data.total_tacada_num += tacada_hole_atual;

                // 2. Calcula score (negativo ou positivo)
                score_hole = (int)(tacada_hole_atual - par_do_hole);
                pgi.data.score += score_hole;

                // 3. Sync Database (Log da sala)
                UpdateRoomLogSql(_session);

                // 4. Conquistas (Achievement)
                var tmp_counter_typeid = AchievementSystem.getScoreCounterTypeId((int)tacada_hole_atual, par_do_hole);
                if (tmp_counter_typeid > 0)
                    pgi.sys_achieve.incrementCounter(tmp_counter_typeid);

                // --- RESET DE DADOS DO BURACO ATUAL ---
                pgi.data.time_out = 0;
                pgi.data.giveup = 0;
                pgi.data.penalidade = 0;
            }
            else if (option == 1) // Quit/GiveUp (Calcula o resto do campo como Max Score)
            {
                var range = m_course.findRange(pgi.hole);
                foreach (var kv in range)
                {
                    if (kv.Key > m_ri.qntd_hole) break;

                    pgi.data.total_tacada_num += kv.Value.getPar().total_shot;
                    pgi.data.score += kv.Value.getPar().range_score[1]; // Geralmente +3 ou +4 por buraco
                }
                pgi.data.time_out = 0;
                pgi.data.tacada_num = 0;
                pgi.data.giveup = 0;
                pgi.data.penalidade = 0;
            }

            // --- ATUALIZAÇÃO DO PROGRESSO (CARD DE SCORE) ---
            pgi.progress.hole = (short)m_course.findHoleSeq(pgi.hole);

            if (option == 0)
            {
                int index = pgi.progress.hole - 1;
                if (index >= 0 && index < 18)
                {
                    if (pgi.shot_sync.state_shot.display.acerto_hole)
                        pgi.progress.finish_hole[index] = 1;

                    pgi.progress.par_hole[index] = (int)par_do_hole;
                    pgi.progress.score[index] = score_hole;
                    pgi.progress.tacada[index] = tacada_hole_atual; // Usa a local, pois a global já foi zerada
                }
            }
            else
            {
                var range = m_course.findRange(pgi.hole);
                foreach (var kv in range)
                {
                    int index = kv.Key - 1;
                    if (index >= 0 && index < 18)
                    {
                        pgi.progress.finish_hole[index] = 0;
                        pgi.progress.par_hole[index] = kv.Value.getPar().par;
                        pgi.progress.score[index] = kv.Value.getPar().range_score[1];
                        pgi.progress.tacada[index] = kv.Value.getPar().total_shot;
                    }
                }
            }
        }

        public virtual void requestSaveRecordCourse(Player _session,
            int game, int option)
        {

            var pgi = getPlayerInfo((_session));
            if (pgi == null)
            {
                throw new exception("[GameBase::" + "requestSaveRecordCourse][Error] PLAYER[UID=" + Convert.ToString((_session).m_pi.uid) + "] " + "tentou salvar record do course do player no jogo" + ", mas o game nao tem o info dele guardado. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME,
                    1, 4));
            }

            if (_session.m_pi.ei.char_info == null)
            { // Player não está com character equipado, kika dele do jogo
                _smp.message_pool.getInstance().push(new message("[GameBase::requestSaveRecordCourse][Log] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] nao esta com Character equipado. kika ele do jogo. pode ser Bug.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                return;
            }

            MapStatistics pMs = null;

            if (pgi.assist_flag == 1)
            { // Assist

                if (game == 52)
                {
                    pMs = _session.m_pi.a_msa_grand_prix[(int)((int)m_ri.course & 0x7F)];
                }
                else if (m_ri.special_flag_mod.natural)
                { // Natural
                    pMs = _session.m_pi.a_msa_natural[(int)((int)m_ri.course & 0x7F)];

                    game = 51; // Natural
                }
                else
                { // Normal
                    pMs = _session.m_pi.a_msa_normal[(int)((int)m_ri.course & 0x7F)];
                }

            }
            else
            { // Sem Assist

                if (game == 52)
                {
                    pMs = _session.m_pi.a_ms_grand_prix[(int)((int)m_ri.course & 0x7F)];
                }
                else if (m_ri.special_flag_mod.natural)
                { // Natural
                    pMs = _session.m_pi.a_ms_natural[(int)((int)m_ri.course & 0x7F)];

                    game = 51; // Natural
                }
                else
                { // Normal
                    pMs = _session.m_pi.a_ms_normal[(int)((int)m_ri.course & 0x7F)];
                }
            }

            bool make_record = false;

            // UPDATE ON SERVER
            if (option == 1)
            { // 18h pode contar record

                // Fez Record
                if (pMs.best_score == 127
                    || pgi.data.score < pMs.best_score
                    || pgi.data.pang > pMs.best_pang)
                {

                    // Update Best Score Record
                    if (pgi.data.score < pMs.best_score)
                    {
                        pMs.best_score = (sbyte)pgi.data.score;
                    }

                    // Update Best Pang Record
                    if (pgi.data.pang > pMs.best_pang)
                    {
                        pMs.best_pang = pgi.data.pang;
                    }

                    // Update Character Record
                    pMs.character_typeid = _session.m_pi.ei.char_info._typeid;

                    make_record = true;
                }
            }

            // Salva os dados normais
            pMs.tacada += (uint)pgi.ui.tacada;
            pMs.putt += (uint)pgi.ui.putt;
            pMs.hole += (uint)pgi.ui.hole;
            pMs.fairway += (uint)pgi.ui.fairway;
            pMs.hole_in += (uint)pgi.ui.hole_in;
            pMs.putt_in += (uint)pgi.ui.putt_in;
            pMs.total_score += pgi.data.score;
            pMs.event_score = 0;

            MapStatisticsEx ms = new MapStatisticsEx(pMs)
            {
                tipo = (byte)game
            };

            // UPDATE ON DB
            snmdb.NormalManagerDB.getInstance().add(5,
                new CmdUpdateMapStatistics(_session.m_pi.uid,
                    ms, pgi.assist_flag),
                SQLDBResponse, this);

            // UPDATE ON GAME, se ele fez record, e add 1000 para ele
            if (make_record)
            {
                // Add 1000 pang por ele ter quebrado o  record dele
                _session.m_pi.addPang(1000);

                // Resposta para make record
                PangyaBinaryWriter p = new PangyaBinaryWriter((ushort)0xB9);

                p.WriteByte(((int)m_ri.course) & 0x7F);

                packet_func.session_send(p,
                    _session, 1);
            }
        }

        public virtual void requestInitItemUsedGame(Player _session, PlayerGameInfo _pgi)
        {

            //INIT_PLAYER_INFO("requestInitItemUsedGame", "tentou inicializar itens usado no jogo", _session, out PlayerGameInfo pgi);

            // Characters Equip
            if (_session.getState() && _session.isConnected())
            { // Check Player Connected

                if (_session.m_pi.ei.char_info == null)
                { // Player não está com character equipado, kika dele do jogo
                    _smp.message_pool.getInstance().push(new message("[GameBase::requestInitItemUsedGame][Log] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] nao esta com Character equipado. kika ele do jogo. pode ser Bug.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                    return;
                }

                if (_session.m_pi.ei.comet == null)
                { // Player não está com Comet(Ball) equipado, kika dele do jogo
                    _smp.message_pool.getInstance().push(new message("[GameBase::requestInitItemUsedGame][Log] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] nao esta com Ball equipado. kika ele do jogo. pode ser Bug.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                    return;
                }

                var ui = _pgi.used_item;

                // Zera os Itens usados
                ui.clear();

                /// ********** Itens Usado **********

                // Passive Item Equipado
                _session.m_pi.mp_wi.ToList().ForEach(_el =>
                {
                    if (passive_item.Any(c => c == _el.Value._typeid))
                    {
                        ui.v_passive.insert(Tuple.Create(_el.Value._typeid, new UsedItem.Passive(_el.Value._typeid, 0)));
                    }
                });
                // Ball Equiped 
                if (_session.m_pi.ei.comet._typeid != DEFAULT_COMET_TYPEID && (!_session.m_pi.m_cap.premium_user || _session.m_pi.ei.comet._typeid != sPremiumSystem.getInstance().getPremiumBallByTicket(_session.m_pi.pt._typeid)))
                {
                    ui.v_passive.insert(Tuple.Create((uint32_t)_session.m_pi.ei.comet._typeid, new UsedItem.Passive(_session.m_pi.ei.comet._typeid, 0)));
                }

                // AuxParts
                for (var i = 0u; i < (_session.m_pi.ei.char_info.auxparts.Length); ++i)
                {
                    if (_session.m_pi.ei.char_info.auxparts[i] >= 0x70000000 && _session.m_pi.ei.char_info.auxparts[i] < 0x70010000)
                    {
                        ui.v_passive.insert(Tuple.Create((uint32_t)_session.m_pi.ei.char_info.auxparts[i], new UsedItem.Passive(_session.m_pi.ei.char_info.auxparts[i], 0)));
                    }
                }

                // Item Active Slot 
                for (var i = 0; i < (_session.m_pi.ue.item_slot.Length); ++i)
                {
                    // Diferente de 0 item está equipado
                    if (_session.m_pi.ue.item_slot[i] != 0)
                    {
                        if (!ui.v_active.ContainsKey(_session.m_pi.ue.item_slot[i])) // Não tem add o novo
                        {
                            ui.v_active.insert(Tuple.Create(_session.m_pi.ue.item_slot[i], new UsedItem.Active(_session.m_pi.ue.item_slot[i], 0u, new List<byte> { (byte)i })));
                        }

                        else // Já tem add só o slot
                        {
                            ui.v_active[(uint32_t)_session.m_pi.ue.item_slot[i]].v_slot.Add((byte)i); // Slot
                        }
                    }
                }

                // ClubSet For ClubMastery
                ui.club._typeid = _session.m_pi.ei.csi._typeid;
                ui.club.count = 0;
                ui.club.rate = 1.0f;

                var club = sIff.getInstance().findClubSet(ui.club._typeid);

                if (club != null)
                {
                    ui.club.rate = club.work_shop.rate;
                }
                else
                {
                    _smp.message_pool.getInstance().push(new message("[GameBase::requestIniItemUsedGame][Warning] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] esta equipado com um ClubSet[TYPEID=" + Convert.ToString(_session.m_pi.ei.csi._typeid) + ", ID=" + Convert.ToString(_session.m_pi.ei.csi.id) + "] que nao tem no IFF_STRUCT do Server. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                }

                /// ********** Itens Usado **********

                /// ********** Itens Exp/Pang Rate **********
                // Item Buff
                var time_limit_item = sIff.getInstance().getTimeLimitItem();

                _session.m_pi.v_ib.ForEach(_el =>
                {
                    var item_ = time_limit_item.FirstOrDefault(_el2 => _el2.Value._typeid == _el._typeid);

                    if (item_.Value != null)
                    {
                        switch ((ItemBuff.eTYPE)item_.Value.type)
                        {
                            case ItemBuff.eTYPE.YAM_AND_GOLD:
                                ui.rate.exp += item_.Value.percent;
                                break;

                            case ItemBuff.eTYPE.RAINBOW:
                            case ItemBuff.eTYPE.RED:
                                ui.rate.exp += (item_.Value.percent > 0) ? item_.Value.percent : 100;
                                ui.rate.pang += (item_.Value.percent > 0) ? item_.Value.percent : 100;
                                break;

                            case ItemBuff.eTYPE.GREEN:
                                ui.rate.exp += (item_.Value.percent > 0) ? item_.Value.percent : 100;
                                break;

                            case ItemBuff.eTYPE.YELLOW:
                                ui.rate.pang += (item_.Value.percent > 0) ? item_.Value.percent : 100;
                                break;
                        }
                    }
                });

                // Card Equipado, Special, NPC, e Caddie
                _session.m_pi.v_cei.ToList().ForEach(_el =>
{
    if (_el.parts_id == _session.m_pi.ei.char_info.id
        && _el.parts_typeid == _session.m_pi.ei.char_info._typeid
        && sIff.getInstance().getItemSubGroupIdentify22(_el._typeid) == 5)
    {
        if (_el.efeito == 2)
        {
            ui.rate.exp += _el.efeito_qntd;
        }
        else if (_el.efeito == 1)
        {
            ui.rate.pang += _el.efeito_qntd;
        }
    }
    else if (_el.parts_id == 0 && _el.parts_typeid == 0 && sIff.getInstance().getItemSubGroupIdentify22(_el._typeid) == 2)
    {
        if (_el.efeito == 3)
        {
            ui.rate.exp += _el.efeito_qntd;
        }
        else if (_el.efeito == 2)
        {
            ui.rate.pang += _el.efeito_qntd;
        }
        else if (_el.efeito == 34)
        {
            ui.rate.club += _el.efeito_qntd;
        }
    }
});

                // Item Passive Boost Exp, Pang and Club Mastery

                // Pang
                ui.v_passive.ToList().ForEach(_el =>
                {
                    if (Array.IndexOf(passive_item_pang_x2, _el.Value._typeid) != passive_item_pang_x2.Length - 1)
                    {
                        ui.rate.pang += 200;
                        _pgi.boost_item_flag.pang = 1;
                    }

                    if (Array.IndexOf(passive_item_pang_x4, _el.Value._typeid) != passive_item_pang_x4.Length - 1)
                    {
                        ui.rate.pang += 400;
                        _pgi.boost_item_flag.pang_nitro = 1;
                    }

                    if (Array.IndexOf(passive_item_pang_x1_5, _el.Value._typeid) != passive_item_pang_x1_5.Length - 1)
                    {
                        ui.rate.pang += 50;
                        _pgi.boost_item_flag.pang = 1;
                    }

                    if (Array.IndexOf(passive_item_pang_x1_4, _el.Value._typeid) != passive_item_pang_x1_4.Length - 1)
                    {
                        ui.rate.pang += 40;
                        _pgi.boost_item_flag.pang = 1;
                    }

                    if (Array.IndexOf(passive_item_pang_x1_2, _el.Value._typeid) != passive_item_pang_x1_2.Length - 1)
                    {
                        ui.rate.pang += 20;
                        _pgi.boost_item_flag.pang = 1;
                    }
                });


                // Exp
                ui.v_passive.ToList().ForEach(_el =>
                {
                    if (Array.IndexOf(passive_item_exp, _el.Value._typeid) != -1)
                    {
                        ui.rate.exp += 200;
                    }
                });

                // Club Mastery Boost
                ui.v_passive.ToList().ForEach(_el =>
                {
                    if (Array.IndexOf(passive_item_club_boost, _el.Value._typeid) != -1)
                    {
                        ui.rate.club += 200;
                    }
                });

                // Character Parts Equipado
                if (_session.m_pi.ei.char_info.parts_typeid.Any(_element =>
                    Array.IndexOf(hat_birthday, _element) != -1))
                {
                    _smp.message_pool.getInstance().push(new message("[GameBase::requestInitItemUsedGame][Log] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] esta equipado com Hat Birthday no Character[TYPEID=" + Convert.ToString(_session.m_pi.ei.char_info._typeid) + ", ID=" + Convert.ToString(_session.m_pi.ei.char_info.id) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                    ui.rate.exp += 20; // 20% Hat Birthday
                }

                // Hat Lua e Sol
                if (_session.m_pi.ei.char_info.parts_typeid.Any(_element =>
                    Array.IndexOf(hat_lua_sol, _element) != -1))
                {
                    _smp.message_pool.getInstance().push(new message("[GameBase::requestInitItemUsedGame][Log] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] esta equipado com Hat Lua e Sol no Character[TYPEID=" + Convert.ToString(_session.m_pi.ei.char_info._typeid) + ", ID=" + Convert.ToString(_session.m_pi.ei.char_info.id) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                    ui.rate.exp += 20;  // 20% Hat Lua e Sol
                    ui.rate.pang += 20; // 20% Hat Lua e Sol
                }

                // Kurafaito Ring Club Mastery
                if (Array.IndexOf(_session.m_pi.ei.char_info.auxparts, KURAFAITO_RING_CLUBMASTERY) != -1)
                {
                    _smp.message_pool.getInstance().push(new message("[GameBase::requestInitItemUsedGame][Log] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] esta equipado com Anel (Kurafaito) que da Club Mastery +1.1% no Character[TYPEID=" + Convert.ToString(_session.m_pi.ei.char_info._typeid) + ", ID=" + Convert.ToString(_session.m_pi.ei.char_info.id) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                    ui.rate.club += 10; // Kurafaito Ring da + 10% no Club Mastery
                }

                // Character AuxParts Equipado
                // Aux parts tem seus próprios valores de rate no iff
                foreach (var _el in _session.m_pi.ei.char_info.auxparts)
                {
                    if (_el != 0 && sIff.getInstance().getItemGroupIdentify(_el) == PangyaAPI.IFF.JP.Models.Flags.IFF_GROUP.AUX_PART)
                    {
                        var auxpart = sIff.getInstance().findAuxPart(_el);
                        if (auxpart != null)
                        {
                            if (auxpart.Pang_Rate > 100)
                            {
                                ui.rate.pang += (uint)(auxpart.Pang_Rate - 100);
                            }
                            else if (auxpart.Pang_Rate > 0)
                            {
                                ui.rate.pang += auxpart.Pang_Rate;
                            }

                            if (auxpart.Exp_Rate > 100)
                            {
                                ui.rate.exp += (uint)(auxpart.Exp_Rate - 100);
                            }
                            else if (auxpart.Exp_Rate > 0)
                            {
                                ui.rate.exp += auxpart.Exp_Rate;
                            }

                            if (auxpart.Drop_Rate > 100)
                            {
                                ui.rate.drop += (uint)(auxpart.Drop_Rate - 100);
                            }
                            else if (auxpart.Drop_Rate > 0)
                            {
                                ui.rate.drop += auxpart.Drop_Rate;
                            }

                            _pgi.thi.all_score += 15;
                        }
                    }
                }

                // Mascot Equipado Rate Exp And Pang, Drop item e Treasure Hunter rate
                if (_session.m_pi.ei.mascot_info != null && _session.m_pi.ei.mascot_info._typeid > 0)
                {

                    var mascot = sIff.getInstance().findMascot(_session.m_pi.ei.mascot_info._typeid);

                    if (mascot != null)
                    {
                        // Pang
                        if (mascot.efeito.pang_rate > 100)
                        {
                            ui.rate.pang += (uint)(mascot.efeito.pang_rate - 100);
                        }
                        else if (mascot.efeito.pang_rate > 0)
                        {
                            ui.rate.pang += (uint)mascot.efeito.pang_rate;
                        }

                        // Exp
                        if (mascot.efeito.exp_rate > 100)
                        {
                            ui.rate.exp += (uint)(mascot.efeito.exp_rate - 100);
                        }
                        else if (mascot.efeito.exp_rate > 0)
                        {
                            ui.rate.exp += (uint)mascot.efeito.exp_rate;
                        }

                        // Drop item, aqui ele add os 120% e no Drop System ele trata isso direito
                        // Todos itens que dá drop rate da treasure hunter point
                        if (mascot.efeito.drop_rate > 100)
                        {

                            if (mascot.efeito.drop_rate > 100)
                            {
                                ui.rate.drop += (uint)(mascot.efeito.drop_rate - 100);
                            }
                            else if (mascot.efeito.drop_rate > 0)
                            {
                                ui.rate.drop += (uint)mascot.efeito.drop_rate;
                            }

                            // Passaro gordo que usa isso aqui, mas pode adicionar mais mascot que dé drop rate e treasure hunter point
                            _pgi.thi.all_score += 15; // Add +15 ao all score
                        }
                    }
                    else
                    {
                        _smp.message_pool.getInstance().push(new message("[GameBase::requestInitItemUsedGame][Warning] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] esta equipado com um mascot[TYPEID=" + Convert.ToString(_session.m_pi.ei.mascot_info._typeid) + ", ID=" + Convert.ToString(_session.m_pi.ei.mascot_info.id) + "] que nao tem no IFF_STRUCT do Server. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                    }
                }

                /// ********** Premium User +10% EXP and PANG *********************

                //if (_pgi.premium_flag)
                //{
                //    var rate_premium = sPremiumSystem.getInstance().getExpPangRateByTicket(_session.m_pi.pt._typeid);
                //    ui.rate.exp += rate_premium;
                //    ui.rate.pang += rate_premium;
                //}

                /// ********** Itens Exp/Pang Rate **********
            }
        }

        public virtual void requestSendTreasureHunterItem(Player _session)
        {

            var pgi = getPlayerInfo((_session));

            if (pgi == null)
            {
                throw new exception("[GameBase::" + "requestSendTreasureHunterItem][Error] PLAYER[UID=" + Convert.ToString((_session).m_pi.uid) + "] " + "tentou enviar os itens ganho no Treasure Hunter do jogo" + ", mas o game nao tem o info dele guardado. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME,
                    1, 4));
            }

            List<stItem> v_item = new List<stItem>();

            if (!pgi.thi.v_item.empty())
            {
                foreach (var el in pgi.thi.v_item)
                {

                    var bi = new BuyItem();
                    var item = new stItem();

                    bi.id = -1;
                    bi._typeid = el._typeid;
                    bi.qntd = el.qntd;

                    ItemManager.initItemFromBuyItem(_session.m_pi,
                        item, bi, false, 0, 0, 1);

                    if (item._typeid == 0)
                    {
                        _smp.message_pool.getInstance().push(new message("[GameBase::requestSendTreasureHunterItem][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou inicializar item[TYPEID=" + Convert.ToString(bi._typeid) + "], mas nao consgeuiu. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                        continue;
                    }

                    v_item.Add(new stItem(item));
                }

                // Add Item, se tiver Item
                if (v_item.Count > 0)
                {

                    var rai = ItemManager.addItem(v_item,
                        _session.getUID(), 0, 0);

                    if (rai.fails.Count > 0 && rai.type != ItemManager.RetAddItem.T_SUCCESS_PANG_AND_EXP_AND_CP_POUCH)
                    {
                        _smp.message_pool.getInstance().push(new message("[GameBase::requestSendTreasureHunterItem][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] nao conseguiu adicionar os itens que ele ganhou no Treasure Hunter. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                    }
                }
            }

            // UPDATE ON GAME
            PangyaBinaryWriter p = new PangyaBinaryWriter((ushort)0x134);

            p.WriteByte((byte)v_item.Count);

            foreach (var el in v_item)
            {
                p.WriteUInt32(_session.m_pi.uid);

                p.WriteUInt32(el._typeid);
                p.WriteInt32(el.id);
                p.WriteInt32(el.qntd);
                p.WriteByte(0); // Opt Acho, mas nunca vi diferente de 0

                p.WriteUInt16((ushort)(el.stat.qntd_dep / 0x8000));
                p.WriteUInt16((ushort)(el.stat.qntd_dep % 0x8000));
            }

            packet_func.session_send(p,
                _session, 1);
        }

        public virtual byte checkCharMotionItem(Player _session)
        {

            // Characters Equip
            if (_session.getState() && _session.isConnected())
            { // Check Player Connected

                if (_session.m_pi.ei.char_info == null)
                { // Player não está com character equipado, kika dele do jogo
                    _smp.message_pool.getInstance().push(new message("[GameBase::checkCharMotionItem][Log] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] nao esta com Character equipado. kika ele do jogo. pode ser Bug.", type_msg.CL_FILE_LOG_AND_CONSOLE));


                    return 0;
                }

                // Motion Item
                if (_session.m_pi.ei.char_info.parts_typeid.Any(_element =>
    motion_item.Contains(_element)))
                {
                    return 1;
                }

            }

            return 0;
        }

        // Atualiza o Info do usuario, Info Trofel e Map Statistics do Course
        // Opt 0 Envia tudo, -1 não envia o map statistics
        public virtual void sendUpdateInfoAndMapStatistics(Player _session, int _option)
        {

            PangyaBinaryWriter p = new PangyaBinaryWriter((ushort)0x45);

            p.WriteBytes(_session.m_pi.ui.ToArray());

            p.WriteBytes(_session.m_pi.ti_current_season.ToArray());

            // Ainda tenho que ajeitar esses Map Statistics no Pacote Principal, No Banco de dados e no player_info class
            if (_option == -1)
            {

                // -1 12 Bytes, os 2 tipos de dados do Map Statistics
                p.WriteInt64(-1);
                p.WriteInt32(-1);

            }
            else
            {
                // Normal essa season
                if (_session.m_pi.a_ms_normal[(int)((int)m_ri.course & 0x7F)].course != ((int)m_ri.course & 0x7F))
                {
                    p.WriteSByte(-1); // Não tem
                }
                else
                {
                    p.WriteByte((char)m_ri.course & 0x7F);
                    p.WriteBytes(_session.m_pi.a_ms_normal[(int)((int)m_ri.course & 0x7F)].ToArray());
                }

                p.WriteSByte(-1); // Não tem

                // Natural essa season
                if (_session.m_pi.a_ms_natural[(int)((int)m_ri.course & 0x7F)].course != ((int)m_ri.course & 0x7F))
                {
                    p.WriteSByte(-1); // N�o tem
                }
                else
                {
                    p.WriteByte((char)m_ri.course & 0x7F);
                    p.WriteBytes(_session.m_pi.a_ms_natural[(int)((int)m_ri.course & 0x7F)].ToArray());
                }
                p.WriteSByte(-1); // Não tem

                // Normal Assist essa season
                if (_session.m_pi.a_msa_normal[(int)((int)m_ri.course & 0x7F)].course != ((int)m_ri.course & 0x7F))
                {
                    p.WriteSByte(-1); // Não tem
                }
                else
                {
                    p.WriteByte((char)m_ri.course & 0x7F);
                    p.WriteBytes(_session.m_pi.a_msa_normal[(int)((int)m_ri.course & 0x7F)].ToArray());
                }

                // Normal Assist rest season
                // tem que fazer o map statistics soma de todas season
                //p.WriteByte((char)m_ri.course & 0x7F);
                //p.WriteBuffer(_session, out PlayerGameInfo pgi.m_pi.aa_ms_normal_todas_season[0][(int)((int)m_ri.course & 0x7F)],Marshal.SizeOf(new MapStatistics()));
                p.WriteSByte(-1); // Não tem

                // Natural Assist essa season
                if (_session.m_pi.a_msa_natural[(int)((int)m_ri.course & 0x7F)].course != ((int)m_ri.course & 0x7F))
                {
                    p.WriteSByte(-1); // Não tem
                }
                else
                {
                    p.WriteByte((char)m_ri.course & 0x7F);
                    p.WriteBytes(_session.m_pi.a_msa_natural[(int)((int)m_ri.course & 0x7F)].ToArray());
                }

                // Natural Assist rest season
                // tem que fazer o map statistics soma de todas season
                //p.WriteByte((char)m_ri.course & 0x7F);
                //p.WriteBuffer(_session, out PlayerGameInfo pgi.m_pi.aa_ms_normal_todas_season[0][(int)((int)m_ri.course & 0x7F)],Marshal.SizeOf(new MapStatistics()));
                p.WriteSByte(-1); // Não tem

                // Grand Prix essa season
                if (_session.m_pi.a_ms_grand_prix[(int)((int)m_ri.course & 0x7F)].course != ((int)m_ri.course & 0x7F))
                {
                    p.WriteSByte(-1); // Não tem
                }
                else
                {
                    p.WriteByte((char)m_ri.course & 0x7F);
                    p.WriteBytes(_session.m_pi.a_ms_grand_prix[(int)((int)m_ri.course & 0x7F)].ToArray());
                }

                // Grand Prix rest season
                // tem que fazer o map statistics soma de todas season
                //p.WriteByte((char)m_ri.course & 0x7F);
                //p.WriteBuffer(_session, out PlayerGameInfo pgi.m_pi.aa_ms_normal_todas_season[0][(int)((int)m_ri.course & 0x7F)],Marshal.SizeOf(new MapStatistics()));
                p.WriteSByte(-1); // Não tem

                // Grand Prix Assist essa season
                if (_session.m_pi.a_msa_grand_prix[(int)((int)m_ri.course & 0x7F)].course != ((int)m_ri.course & 0x7F))
                {
                    p.WriteSByte(-1); // Não tem
                }
                else
                {
                    p.WriteByte((char)m_ri.course & 0x7F);
                    p.WriteBytes(_session.m_pi.a_msa_grand_prix[(int)((int)m_ri.course & 0x7F)].ToArray());
                }

                // Grand Prix Assist rest season
                // tem que fazer o map statistics soma de todas season
                //p.WriteByte((char)m_ri.course & 0x7F);
                //p.WriteBuffer(_session, out PlayerGameInfo pgi.m_pi.aa_ms_normal_todas_season[0][(int)((int)m_ri.course & 0x7F)],Marshal.SizeOf(new MapStatistics()));
                p.WriteSByte(-1); // Não tem
            }

            packet_func.session_send(p,
                _session, 1);
        }

        // Envia a message no char para todos player do Game que o player terminou o jogo
        protected virtual void sendFinishMessage(Player _session)
        {

            var pgi = getPlayerInfo((_session));
            if (pgi == null)
            {
                throw new exception("[GameBase::" + "sendFinishMessage][Error] PLAYER[UID=" + Convert.ToString((_session).m_pi.uid) + "] " + "tentou enviar message no chat que o player terminou o jogo" + ", mas o game nao tem o info dele guardado. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME,
                    1, 4));
            }

            PangyaBinaryWriter p = new PangyaBinaryWriter((ushort)0x40);

            p.WriteByte(16); // Msg que terminou o game

            p.WriteString(_session.m_pi.nickname);
            p.WriteUInt16(0); // Size Msg

            p.WriteInt32(pgi.data.score);
            p.WriteUInt64(pgi.data.pang);
            p.WriteByte(pgi.assist_flag);

            packet_func.game_broadcast(this,
                p.GetBytes, 1);
        }

        public virtual void requestCalculeRankPlace()
        {
            if (m_player_order.Count > 0)
            {
                m_player_order.Clear();
            }

            foreach (var el in m_player_info)
            {
                if (el.Value.flag != PlayerGameInfo.eFLAG_GAME.QUIT) // menos os que quitaram
                {
                    m_player_order.Add(el.Value);
                }
            }

            m_player_order.Sort(sort_player_rank);
        }

        public int sort_player_rank(PlayerGameInfo _pgi1, PlayerGameInfo _pgi2)
        {
            if (_pgi1.data.score == _pgi2.data.score)
                return _pgi2.data.pang.CompareTo(_pgi1.data.pang); // decrescente de pang (maior pang primeiro)

            return _pgi1.data.score.CompareTo(_pgi2.data.score); // crescente de score (menor score primeiro)
        }

        // Set Flag Game and finish_game flag
        public virtual void setGameFlag(PlayerGameInfo _pgi, PlayerGameInfo.eFLAG_GAME _fg)
        {

            if (_pgi == null)
            {

                _smp.message_pool.getInstance().push(new message("[GameBase::setGameFlag][Error] PlayerGameInfo* _pgi is invalid(null).", type_msg.CL_FILE_LOG_AND_CONSOLE));

                return;
            }

            _pgi.flag = _fg;
        }

        public virtual void setFinishGameFlag(PlayerGameInfo _pgi, byte _finish_game)
        {

            if (_pgi == null)
            {

                _smp.message_pool.getInstance().push(new message("[GameBase::setFinishGameFlag][Error] PlayerGameInfo* _pgi is invlaid(null).", type_msg.CL_FILE_LOG_AND_CONSOLE));

                return;
            }
            _pgi.finish_game = _finish_game;
        }

        // Check And Clear
        public virtual bool AllCompleteGameAndClear()
        {
            uint32_t count = 0;
            // Da error Aqui
            foreach (var el in m_players)
            {

                try
                {

                    var pgi = getPlayerInfo((el));
                    if (pgi == null)
                    {
                        throw new exception("[GameBase::" + "PlayersCompleteGameAndClear][Error] PLAYER[UID=" + Convert.ToString((el).m_pi.uid) + "] " + "tentou verificar se o player terminou o jogo" + ", mas o game nao tem o info dele guardado. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME,
                            1, 4));
                    }

                    if (pgi.flag != PlayerGameInfo.eFLAG_GAME.PLAYING)
                    {
                        count++;
                    }

                }
                catch (exception e)
                {

                    _smp.message_pool.getInstance().push(new message("[GameBase::AllCompleteGameAndClear][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
            }

            return count == m_players.Count;
        }

        public bool PlayersCompleteGameAndClear()
        {
            uint32_t count = 0;
            // Da error Aqui
            foreach (var el in m_players)
            {

                try
                {

                    var pgi = getPlayerInfo(el);
                    if (pgi == null)
                    {
                        throw new exception("[GameBase::" + "PlayersCompleteGameAndClear][Error] PLAYER[UID=" + Convert.ToString((el).m_pi.uid) + "] " + "tentou verificar se o player terminou o jogo" + ", mas o game nao tem o info dele guardado. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME,
                            1, 4));
                    }

                    if (pgi.finish_game == 1)
                    {
                        count++;
                    }

                }
                catch (exception e)
                {

                    _smp.message_pool.getInstance().push(new message("[GamePlayersCompleteGameAndClear][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
            }

            return count == m_players.Count;
        }

        // Verifica se é o ultimo hole feito
        protected virtual bool checkEndGame(Player _session)
        {

            var pgi = getPlayerInfo((_session));
            if (pgi == null)
            {
                throw new exception("[GameBase::" + "checkEndGame][Error] PLAYER[UID=" + Convert.ToString((_session).m_pi.uid) + "] " + "tentou verificar se eh o final do jogo" + ", mas o game nao tem o info dele guardado. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME,
                1, 4));
            }

            return (m_course.findHoleSeq(pgi.hole) == m_ri.qntd_hole);
        }

        // Retorna todos os player que entrou no jogo, exceto os que quitaram
        public virtual uint32_t getCountPlayersGame()
        {

            size_t count = 0;

            count = m_player_info.Count(_el =>
            {
                return _el.Value.flag != PlayerGameInfo.eFLAG_GAME.QUIT;
            });

            return (uint32_t)count;
        }
        public virtual void initAchievement(Player _session)
        {

            var pgi = getPlayerInfo((_session));
            if (pgi == null)
            {
                throw new exception("[GameBase::" + "initAchievement][Error] PLAYER[UID=" + Convert.ToString((_session).m_pi.uid) + "] " + "tentou inicializar o achievemento do player no jogo" + ", mas o game nao tem o info dele guardado. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME,
                    1, 4));
            }


            try
            {

                // Initialize Achievement Player
                pgi.sys_achieve.incrementCounter(0x6C400002u/*Normal Game*/);

                if (m_ri.special_flag_mod.short_game)
                    pgi.sys_achieve.incrementCounter(0x6C4000BBu/*Short Game*/);

                if (m_ri.master == _session.m_pi.uid)
                {
                    pgi.sys_achieve.incrementCounter(0x6C400098u/*Master da Sala*/);

                    if (m_ri.typeid_artefatic > 0)
                        pgi.sys_achieve.incrementCounter(0x6C400099u/*Master da Sala com Artefact*/);
                }

                if (_session.m_pi.ei.char_info != null)
                {

                    var ctc = AchievementSystem.getCharacterCounterTypeId(_session.m_pi.ei.char_info._typeid);

                    if (ctc > 0u)
                        pgi.sys_achieve.incrementCounter(ctc/*Character Counter Typeid*/);
                }

                if (_session.m_pi.ei.cad_info != null)
                {

                    var ctc = AchievementSystem.getCaddieCounterTypeId(_session.m_pi.ei.cad_info._typeid);

                    if (ctc > 0u)
                        pgi.sys_achieve.incrementCounter(ctc/*Caddie Counter Typeid*/);
                }

                if (_session.m_pi.ei.mascot_info != null)
                {

                    var ctm = AchievementSystem.getMascotCounterTypeId(_session.m_pi.ei.mascot_info._typeid);

                    if (ctm > 0u)
                        pgi.sys_achieve.incrementCounter(ctm/*Mascot Counter Typeid*/);
                }

                var ct = AchievementSystem.getCourseCounterTypeId((uint)(m_ri.getMap() & 0x7F));

                if (ct > 0u)
                    pgi.sys_achieve.incrementCounter(ct/*Course Counter Item*/);

                ct = AchievementSystem.getQntdHoleCounterTypeId(m_ri.qntd_hole);

                if (ct > 0u)
                    pgi.sys_achieve.incrementCounter(ct/*Qntd Hole Counter Item*/);

                // Fim do inicializa o Achievement

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[GameBase::initAchievement][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if (ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) != STDA_ERROR_TYPE.SYS_ACHIEVEMENT)
                    throw;  // relança exception
            }
        }
        public virtual void records_player_achievement(Player _session)
        {
            //CHECK_SESSION("records_players_achievement");

            INIT_PLAYER_INFO("records_player_achievement", "tentou atualizar os achievement de records do player no jogo", _session, out PlayerGameInfo pgi);

            try
            {

                if (pgi.ui.ob > 0)
                    pgi.sys_achieve.incrementCounter(0x6C40004Cu/*OB*/, pgi.ui.ob);

                if (pgi.ui.bunker > 0)
                    pgi.sys_achieve.incrementCounter(0x6C40004Eu/*Bunker*/, pgi.ui.bunker);

                if (pgi.ui.tacada > 0 || pgi.ui.putt > 0)
                    pgi.sys_achieve.incrementCounter(0x6C400055u/*Shots*/, pgi.ui.tacada + pgi.ui.putt);

                if (pgi.ui.hole > 0)
                    pgi.sys_achieve.incrementCounter(0x6C400005u/*Holes*/, pgi.ui.hole);

                if (pgi.ui.total_distancia > 0)
                    pgi.sys_achieve.incrementCounter(0x6C400056u/*Yards*/, pgi.ui.total_distancia);

                // Bug o valor é 0 por que (int)0.9f é 0 ele trunca não arredondo, e tem que truncar mesmo
                // Para fixa esse bug é só fazer >= 1.f sempre vai ser (int) >= 1(truncado)
                if (pgi.ui.best_drive >= 1.0f)
                    pgi.sys_achieve.incrementCounter(0x6C400057u/*Best Drive*/, (int)pgi.ui.best_drive);

                if (pgi.ui.best_chip_in >= 1.0f)
                    pgi.sys_achieve.incrementCounter(0x6C400058u/*Best Chip-in*/, (int)pgi.ui.best_chip_in);

                if (pgi.ui.best_long_putt >= 1.0f)
                    pgi.sys_achieve.incrementCounter(0x6C400077u/*Best Long-putt*/, (int)pgi.ui.best_long_putt);

                if (pgi.ui.acerto_pangya > 0)
                    pgi.sys_achieve.incrementCounter(0x6C40000Bu/*Acerto PangYa*/, pgi.ui.acerto_pangya);

                if (pgi.data.pang > 0)
                    pgi.sys_achieve.incrementCounter(0x6C40000Du/*Pangs Ganho em 1 jogo*/, (int)pgi.data.pang);

                if (pgi.data.score != 0)
                    pgi.sys_achieve.incrementCounter(0x6C40000Cu/*Score*/, pgi.data.score);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[GameBase::records_player_achievement][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if (ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) != STDA_ERROR_TYPE.SYS_ACHIEVEMENT)
                    throw;  // relança exception
            }
        }

        public virtual void update_sync_shot_achievement(Player _session, Location _last_location)
        {
            //CHECK_SESSION("update_sync_shot_achievement");

            INIT_PLAYER_INFO("update_sync_shot_achievement", "tentou atualizar o achievement de Desafios no jogo", _session, out PlayerGameInfo pgi);

            try
            {

                // Só conta se o player acertou o hole
                if (pgi.shot_sync.state_shot.display.acerto_hole)
                {

                    // Long-putt
                    if (pgi.shot_sync.state_shot.display.long_putt && pgi.shot_sync.state_shot.shot.club_putt == 1)
                    {
                        var diff = pgi.location.diffXZ(_last_location) * MEDIDA_PARA_YARDS;

                        if (diff >= 30.0f)
                            pgi.sys_achieve.incrementCounter(0x6C400035u/*Long Putt 30y+*/);

                        if (diff >= 25.0f)
                            pgi.sys_achieve.incrementCounter(0x6C400034u/*Long Putt 25y+*/);

                        if (diff >= 20.0f)
                            pgi.sys_achieve.incrementCounter(0x6C400033u/*Long Putt 20y+*/);

                        if (diff >= 17.0f)
                            pgi.sys_achieve.incrementCounter(0x6C400032u/*Long Putt 17y+*/);
                    }

                    //Fez o hole de Beam Impact
                    if (pgi.shot_sync.state_shot.display.beam_impact)
                        pgi.sys_achieve.incrementCounter(0x6C40006Fu/*Beam Impact*/);

                    // Fez o hole com
                    if (pgi.shot_sync.state_shot.shot.spin_front == 1)
                        pgi.sys_achieve.incrementCounter(0x6C400064u/*Spin Front*/);

                    if (pgi.shot_sync.state_shot.shot.spin_back == 1)
                        pgi.sys_achieve.incrementCounter(0x6C400065u/*Spin Back*/);

                    if (pgi.shot_sync.state_shot.shot.curve_left > 0 || pgi.shot_sync.state_shot.shot.curve_right > 0)
                        pgi.sys_achieve.incrementCounter(0x6C400066u/*Curve*/);

                    if (pgi.shot_sync.state_shot.shot.tomahawk > 0)
                        pgi.sys_achieve.incrementCounter(0x6C400067u/*Tomahawk*/);

                    if (pgi.shot_sync.state_shot.shot.spike > 0)
                        pgi.sys_achieve.incrementCounter(0x6C400068u/*Spike*/);

                    if (pgi.shot_sync.state_shot.shot.cobra > 0)
                        pgi.sys_achieve.incrementCounter(0x6C40006Eu/*Cobra*/);

                    ////Fez sem usar power shot
                    if (pgi.shot_sync.state_shot.display.chip_in_without_special_shot && !pgi.shot_sync.state_shot.display.special_shot/*Nega*/)
                        pgi.sys_achieve.incrementCounter(0x6C40005Bu/*Fez sem usar power shot*/);

                    // o pacote12 passa primeiro depois que o server response ele passa esse pacote1B, então esse valor sempre vai está certo
                    // Fez Errando pangya
                    if ((pgi.shot_data.acerto_pangya_flag & 2/*Errou pangya*/ ).IsTrue() && !pgi.shot_sync.state_shot.shot.club_putt.IsTrue()/*Nega*/)
                        pgi.sys_achieve.incrementCounter(0x6C400059u/*Fez errando pangya*/);
                }

                // Tacada Power Shot ou Double Power Shot
                if (pgi.shot_sync.state_shot.shot.power_shot > 0)
                    pgi.sys_achieve.incrementCounter(0x6C400051u/*Power Shot*/);

                if (pgi.shot_sync.state_shot.shot.double_power_shot > 0)
                    pgi.sys_achieve.incrementCounter(0x6C400052u/*Double Power Shot*/);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[GameBase::update_sync_shot_achievement][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if (ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) != STDA_ERROR_TYPE.SYS_ACHIEVEMENT)
                    throw;  // relança exception
            }
        }

        public virtual void rain_hole_consecutivos_count(Player _session)
        {

            var chr = m_course.getConsecutivesHolesRain();

            INIT_PLAYER_INFO("rain_hole_consecutivos_count", "tentou atualizar o achievement count de chuva em holes consecutivos do player no jogo", _session, out PlayerGameInfo pgi);

            try
            {
                var seq = m_course.findHoleSeq(pgi.hole);
                var count = 0;
                if (chr.isValid())
                {

                    // 2 Holes consecutivos
                    if ((count = chr._2_count.getCountHolesRainBySeq(seq)) > 0u)
                        pgi.sys_achieve.incrementCounter(0x6C40009Bu/*2 Holes consecutivos*/, count);

                    if ((count = chr._3_count.getCountHolesRainBySeq(seq)) > 0u)
                        pgi.sys_achieve.incrementCounter(0x6C40009Cu/*3 Holes consecutivos*/, count);

                    if ((count = chr._4_pluss_count.getCountHolesRainBySeq(seq)) > 0u)
                        pgi.sys_achieve.incrementCounter(0x6C40009Du/*4 ou mais Holes consecutivos*/, count);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[GameBase::rain_hole_consecutivos_count][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if (ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) != STDA_ERROR_TYPE.SYS_ACHIEVEMENT)
                    throw;  // relança exception
            }
        }

        public virtual void score_consecutivos_count(Player _session)
        {

            int32_t score = -2, last_score = -2;

            INIT_PLAYER_INFO("rain_score_consecutivos", "tentou atualizar o achievement contador de score consecutivos do player no jogo", _session, out PlayerGameInfo pgi);

            try
            {
                int count = 0;
                for (var i = 0; i < m_ri.qntd_hole; ++i)
                {
                    score = AchievementSystem.getScoreNum(pgi.progress.tacada[i], pgi.progress.par_hole[i]);

                    // Change Score, Soma o Count do Score
                    if ((score != last_score || i == (m_ri.qntd_hole - 1)/*Ultimo hole*/) && last_score != -2/*Primeiro Hole*/)
                    {

                        // 1 == 2, 2 ou mais Holes com o mesmo score
                        if (count >= 1u && last_score >= 0/*Scores que tem no achievement*/)
                        {

                            switch (last_score)
                            {
                                case 0: // HIO
                                    pgi.sys_achieve.incrementCounter(0x6C400063u/*HIO*/);
                                    break;
                                case 1: // Alba
                                    pgi.sys_achieve.incrementCounter(0x6C400062u/*Alba*/);
                                    break;
                                case 2: // Eagle
                                    pgi.sys_achieve.incrementCounter(0x6C400061u/*Eagle*/);
                                    break;
                                case 3: // Birdie
                                    pgi.sys_achieve.incrementCounter(0x6C40005Du/*Birdie*/);
                                    break;
                                case 4: // Par
                                    pgi.sys_achieve.incrementCounter(0x6C40005Eu/*Par*/);
                                    break;
                                case 5: // Bogey
                                    pgi.sys_achieve.incrementCounter(0x6C40005Fu/*Bogey*/);
                                    break;
                                case 6: // Double Bogey
                                    pgi.sys_achieve.incrementCounter(0x6C400060u/*Double Bogey*/);
                                    break;
                            }
                        }

                        // Reseta o count
                        count = 0;

                    }
                    else if (score == last_score)
                        count++;

                    // Update Last Score
                    last_score = score;
                }
            }

            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[GameBase::score_consecutivos_count][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if (ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) != STDA_ERROR_TYPE.SYS_ACHIEVEMENT)
                    throw;  // relança exception
            }
        }

        public virtual void rain_count(Player _session)
        {
            try
            {

                // Recovery, Chuva, Neve/*Tempo Ruim*/
                if (m_course.countHolesRain() > 0)
                {
                    INIT_PLAYER_INFO("rain_count_players", "tentou atualizar o achievement contador de chuva do player no jogo", _session, out PlayerGameInfo pgi);

                    // Pega pela quantidade de holes jogados
                    var seq = m_course.findHoleSeq(pgi.hole);

                    uint count;
                    if ((count = m_course.countHolesRainBySeq(seq)) > 0u)
                        pgi.sys_achieve.incrementCounter(0x6C40009Au/*Chuva*/, (int)count);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[GameBase::rain_count][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                if (ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) != STDA_ERROR_TYPE.SYS_ACHIEVEMENT)
                    throw;  // relança exception
            }
        }

        public virtual void setEffectActiveInShot(Player _session, uint64_t _effect)
        {
            try
            {

                INIT_PLAYER_INFO("setEffectActiveInShot", "tentou setar o efeito ativado na tacada", _session, out PlayerGameInfo pgi);

                pgi.effect_flag_shot.ullFlag |= _effect; // Ativa o efeito na tacada
            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[GameBase::setEffectActiveInShot][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        // Limpa os dados que são usados para cada tacada, reseta ele para usar na próxima tacada 
        public virtual void clearDataEndShot(PlayerGameInfo _pgi)
        {

            if (_pgi == null)
                throw new exception("[GameBase::clearDataEndShot][Error] PlayerGameInfo *_pgi is invalid(null). Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME, 100, 0));

            try
            {
                _pgi.effect_flag_shot.clear();
                _pgi.item_active_used_shot = 0;
                _pgi.earcuff_wind_angle_shot = 0.0f;
            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[GameBase::clearDataEndShot][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }
        public virtual void checkEffectItemAndSet(Player _session, uint32_t _typeid)
        {
            //CHECK_SESSION("checkEffectitemAndSet");

            try
            {

                var ability = sIff.getInstance().findAbility(_typeid);

                if (ability != null)
                {

                    for (var i = 0; i < ability.Efeito.Type.Length; ++i)
                    {

                        if (ability.Efeito.Type[i] == 0u)
                            continue;

                        if (ability.Efeito.Type[i] == (uint32_t)AbilityEffect.COMBINE_ITEM_EFFECT)
                        {

                            // find item setEffectTable
                            var effectTable = sIff.getInstance().findSetEffectTable((uint32_t)ability.Efeito.Rate[i]);

                            if (effectTable != null)
                            {

                                for (var j = 0; j < effectTable.effect.effect.Length; ++j)
                                {

                                    if (effectTable.effect.effect[j] == 0u || effectTable.effect.effect[j] < 4u)
                                        continue;

                                    switch ((eEFFECT)effectTable.effect.effect[j])
                                    {
                                        case eEFFECT.PIXEL:
                                            setEffectActiveInShot(_session, enumToBitValue(AbilityEffect.PIXEL));
                                            break;
                                        case eEFFECT.ONE_ALL_STATS:
                                            setEffectActiveInShot(_session, enumToBitValue(AbilityEffect.ONE_IN_ALL_STATS));
                                            break;
                                        case eEFFECT.WIND_DECREASE:
                                            setEffectActiveInShot(_session, enumToBitValue(AbilityEffect.DECREASE_1M_OF_WIND));
                                            break;
                                        case eEFFECT.PATINHA:
                                            setEffectActiveInShot(_session, enumToBitValue(AbilityEffect.PAWS_NOT_ACCUMULATE));
                                            break;
                                    }
                                }
                            }

                        }
                        else
                            setEffectActiveInShot(_session, enumToBitValue((AbilityEffect)(ability.Efeito.Type[i])));
                    }
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[GameBase::checkEffectitemAndSet][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        protected static void SQLDBResponse(int _msg_id, Pangya_DB _pangya_db, object _arg)
        {

            if (_arg == null)
            {
                _smp.message_pool.getInstance().push(new message("[GameBase::SQLDBResponse][Warning] _arg is null com msg_id = " + (_msg_id), type_msg.CL_FILE_LOG_AND_CONSOLE));
                return;
            }

            // Por Hora só sai, depois faço outro tipo de tratamento se precisar
            if (_pangya_db.getException().getCodeError() != 0)
            {
                _smp.message_pool.getInstance().push(new message("[GameBase::SQLDBResponse][Error] " + _pangya_db.getException().getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                return;
            }

            switch (_msg_id)
            {
                case 12:    // Update ClubSet Workshop
                    {
                        break;
                    }
                case 1: // Insert Ticket Report Dados
                    {
                        break;
                    }
                case 43:    // Insert Ticket Report Dados
                    {
                        break;
                    }
                case 44:    // Insert Ticket Report Dados
                    {
                        break;
                    }
                case 0:
                default:    // 25 é update item equipado slot
                    break;
            }
        }

        public void makePlayerInfo(Player _session)
        {
            try
            {
                PlayerGameInfo pgi = makePlayerInfoObject(_session);

                // Bloqueia o OID para ninguém pegar ele até o torneio acabar
                sgs.gs.getInstance().blockOID(_session.m_oid);

                // Update Place player
                _session.m_pi.place = 0;   // Jogando

                pgi.uid = _session.m_pi.uid;
                pgi.oid = _session.m_oid;
                pgi.level = _session.m_pi.mi.level;

                // Entrou no Jogo depois de ele ter começado
                if (m_state)
                    pgi.enter_after_started = 1;

                // Typeid do Mascot Equipado
                if (_session.m_pi.ei.mascot_info != null && _session.m_pi.ei.mascot_info._typeid > 0)
                    pgi.mascot_typeid = _session.m_pi.ei.mascot_info._typeid;

                // Premium User
                if (_session.m_pi.m_cap.premium_user)
                    pgi.premium_flag = true;

                // Card Wind Flag
                pgi.card_wind_flag = getPlayerWindFlag(_session);

                // Treasure Hunter Points Card Player Initialize Data
                // Não pode ser chamado depois do Init Item Used Game, por que ele vai add os pontos dos itens que dá Drop rate e treasure hunter point
                pgi.thi = getPlayerTreasureInfo(_session);

                // Flag Assist 
                if (_session.m_pi.assist_flag)
                    pgi.assist_flag = 1;

                // Verifica se o player está com o motion item equipado
                pgi.char_motion_item = checkCharMotionItem(_session);

                // Motion Item da Treasure Hunter Point também
                if (pgi.char_motion_item == 1)
                    pgi.thi.all_score += 20;    // +20 all score

                pgi.data.clear();
                pgi.location.clear();
                if (!m_player_info.ContainsKey(_session)) // ainda nao
                    m_player_info.Add(_session, pgi);
                else //ja tem ele 
                {
                    try
                    {

                        // pega o antigo PlayerGameInfo para usar no Log
                        var pgi_ant = m_player_info[_session];

                        // Novo PlayerGameInfo
                        m_player_info[_session] = pgi;

                        // Log de que trocou o PlayerGameInfo da session
                        _smp.message_pool.getInstance().push(new message("[GameBase::makePlayerInfo][Warning][Log] PLAYER[UID=" + (_session.m_pi.uid)
                                + "] esta trocando o PlayerGameInfo[UID=" + (pgi_ant.uid) + "] do player anterior que estava conectado com essa session, pelo o PlayerGameInfo[UID="
                                + (pgi.uid) + "] do player atual da session.", type_msg.CL_FILE_LOG_AND_CONSOLE));


                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        _smp.message_pool.getInstance().push(new message("[GameBase::makePlayerInfo][Error][Warning] PLAYER[UID=" + (_session.m_pi.uid)
                                + "], nao conseguiu atualizar o PlayerGameInfo da session para o novo PlayerGameInfo do player atual da session. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                    }
                }

                // Init Item Used Game(Dados)
                requestInitItemUsedGame(_session, pgi);
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public void clearAllPlayerInfo()
        {
            foreach (var el in m_player_info)
            {
                if (el.Value != null)
                    sgs.gs.getInstance().unblockOID(_oid: el.Value.oid);   // Desbloqueia o OID 
            }

            m_player_info.Clear();
        }

        public virtual void initAllPlayerInfo()
        {
            foreach (var el in m_players)
                makePlayerInfo(el);
        }

        // Make Object Player Info Polimofirsmo
        public virtual PlayerGameInfo makePlayerInfoObject(Player _session)
        {
            // ignore : UNREFERENCED_PARAMETER(_session); == ignore

            return new PlayerGameInfo();
        }
        /// <summary>
        /// metodo é ignorante
        /// </summary>
        /// <param name="_session"></param>
        /// <param name="_packet"></param>
        public virtual void requestInitShotSended(Player _session, packet _packet)
        {

        } 


        //somente atualiza no banco de dados
        // Atualiza as informações do jogador no log da sala
        public void UpdateRoomLogSql(Player _session)
        { 
        }

        bool isLoggableRoomType(byte tipo)
        {

            switch ((RoomInfo.ROOM_INFO_TYPE)tipo)
            {
                case RoomInfo.ROOM_INFO_TYPE.GUILD_BATTLE:
                case RoomInfo.ROOM_INFO_TYPE.TOURNEY_TEAM:
                case RoomInfo.ROOM_INFO_TYPE.STROKE:
                case RoomInfo.ROOM_INFO_TYPE.MATCH:
                case RoomInfo.ROOM_INFO_TYPE.PANG_BATTLE:
                case RoomInfo.ROOM_INFO_TYPE.APPROCH:
                case RoomInfo.ROOM_INFO_TYPE.TOURNEY:
                case RoomInfo.ROOM_INFO_TYPE.SPECIAL_SHUFFLE_COURSE:
                //aqui deve ser outro tipo de log, identificado por 1 ou 0
                case RoomInfo.ROOM_INFO_TYPE.GRAND_ZODIAC_INT:
                case RoomInfo.ROOM_INFO_TYPE.GRAND_ZODIAC_ADV:
                case RoomInfo.ROOM_INFO_TYPE.GRAND_PRIX:
                case RoomInfo.ROOM_INFO_TYPE.GRAND_ZODIAC_PRACTICE:
                case RoomInfo.ROOM_INFO_TYPE.PRACTICE:
                    return true;
                default:
                    return false;
            }
        }

        // Gera um GUID e formata como string no padrão "{xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx}"
        void generateRoomLogGuid()
        {
            Guid guid = Guid.NewGuid();
            m_room_log.roomId = guid;
        }


        string getNameMap(uint map)
        {
            switch ((RoomInfo.ROOM_INFO_COURSE)map)
            {
                case RoomInfo.ROOM_INFO_COURSE.BLUE_LAGOON:
                    return "Blue Lagoon";
                case RoomInfo.ROOM_INFO_COURSE.BLUE_WATER:
                    return "Blue Water";
                case RoomInfo.ROOM_INFO_COURSE.SEPIA_WIND:
                    return "Sepia Wind";
                case RoomInfo.ROOM_INFO_COURSE.WIND_HILL:
                    return "Wind Hill";
                case RoomInfo.ROOM_INFO_COURSE.WIZ_WIZ:
                    return "Wiz Wiz";
                case RoomInfo.ROOM_INFO_COURSE.WEST_WIZ:
                    return "West Wiz";
                case RoomInfo.ROOM_INFO_COURSE.BLUE_MOON:
                    return "Blue Moon";
                case RoomInfo.ROOM_INFO_COURSE.SILVIA_CANNON:
                    return "Silvia Cannon";
                case RoomInfo.ROOM_INFO_COURSE.ICE_CANNON:
                    return "Ice Cannon";
                case RoomInfo.ROOM_INFO_COURSE.WHITE_WIZ:
                    return "White Wiz";
                case RoomInfo.ROOM_INFO_COURSE.SHINNING_SAND:
                    return "Shinning Sand";
                case RoomInfo.ROOM_INFO_COURSE.PINK_WIND:
                    return "Pink Wind";
                case RoomInfo.ROOM_INFO_COURSE.DEEP_INFERNO:
                    return "Deep Inferno";
                case RoomInfo.ROOM_INFO_COURSE.ICE_SPA:
                    return "Ice Spa";
                case RoomInfo.ROOM_INFO_COURSE.LOST_SEAWAY:
                    return "Lost Seaway";
                case RoomInfo.ROOM_INFO_COURSE.EASTERN_VALLEY:
                    return "Eastern Valley";
                case RoomInfo.ROOM_INFO_COURSE.ICE_INFERNO:
                    return "Ice Inferno";
                case RoomInfo.ROOM_INFO_COURSE.WIZ_CITY:
                    return "Wiz City";
                case RoomInfo.ROOM_INFO_COURSE.ABBOT_MINE:
                    return "Abbot Mine";
                case RoomInfo.ROOM_INFO_COURSE.MYSTIC_RUINS:
                    return "Mystic Ruins";
                default:
                    return "Unknown";
            }
        }
        //retorna o tipo da tacada = 0(HIO), 1(ALBA), 2(EAGLE),3(BIRDIE), 4(PAR), -1(tacadas não feitas )
        public int getScore(int _tacada_num, int _par_hole)
        {
            int tipo = Convert.ToInt32(_tacada_num - _par_hole);
            if (_tacada_num == 1) // HIO
                return 0;
            else
            {
                switch (tipo)
                {
                    case -3:    // Alba
                        return 1;
                    case -2:    // Eagle
                        return 2;
                    case -1:    // Birdie
                        return 3;
                    case 0: // Par
                        return 4;
                    case 1: // bogey
                        return 5;
                    case 2: // Double bogey
                        return 6;
                    case 3: // Triple bogey
                        return 7;
                    default: // give up
                        return 8;

                }
            }
        }

        public int _getScore(uint _tacada_num, sbyte _par_hole)
        {
            int tipo = Convert.ToInt32(_tacada_num - _par_hole);
            if (_tacada_num == 1) // HIO
                return 0;//hio(tava -4)
            else
            {
                switch (tipo)
                {
                    case -3:    // Alba
                        return 1;
                    case -2:    // Eagle
                        return 2;//okay
                    case -1:    // Birdie
                        return 3;
                    case 0: // Par
                        return 4;//nao calcula, pq é zero
                    case 1: // bogey
                        return 5;
                    case 2: // Double bogey
                        return 6;
                    case 3: // Triple bogey
                        return 7;
                    default: // give up
                        return 8;

                }
            }
        }

        string getScoreStr(int _tacada_num, sbyte _par_hole)
        {

            var tipo = getScore(_tacada_num, _par_hole);
            switch (tipo)
            {
                case 0:
                    return ("HIO");
                case 1:
                    return ("ALBATROSS");
                case 2:
                    return ("EAGLE");
                case 3:
                    return ("BIRDIE");
                case 4:
                    return ("PAR");
                case 5:
                    return ("BOGEY");
                case 6:
                    return ("DOUBLE BOGEY");
                case 7:
                    return ("TRIPLE BOGEY");
                default:
                    return ("GIVE UP");
            }
        }

        public float TRANSF_SERVER_RATE_VALUE(uint rate)
        {
            return DefineConstants.TRANSF_SERVER_RATE_VALUE((int)rate);
        }

        public enum GAMESTATEFLAG : int
        {
            DEFAULT = -1,
            INIT = 1,
            GAME_FINISH = 2
        }
        public GAMESTATEFLAG getGameState()
        {
            switch (m_game_init_state)
            {
                case 1:
                    return GAMESTATEFLAG.INIT;
                case 2:
                    return GAMESTATEFLAG.GAME_FINISH;
                default:
                    return GAMESTATEFLAG.DEFAULT;
            }

        }

        protected void LogDestruction()
        {
            string className = this.GetType().Name;
            int roomNum = (m_ri != null) ? m_ri.numero : -1;

            string fullMsg = $"[{className}::Destruction][Warning] Destroyed on Room[Number={roomNum}]";

            _smp.message_pool.getInstance().push(new message(fullMsg, type_msg.CL_FILE_LOG_AND_CONSOLE));
        }

        /// <summary>
        /// GameBase.cs precisa ser chamado por ultimo(remove o conflito de dados)
        /// </summary>
        /// <param name="disposing">limpe agora = true</param>
        public virtual void Dispose(bool disposing)
        {
            if (disposedValue) return;

            if (disposing)
            {
                if (m_course != null)
                    m_course.Dispose();

                clear_player_order();

                clearAllPlayerInfo();

                clear_time();

                if (!m_player_report_game.empty())
                    m_player_report_game.Clear();
            }
            disposedValue = true;
        }

        public void Dispose()
        {
            // Não altere este código. Coloque o código de limpeza no método 'Dispose(bool disposing)'
            Dispose(true);
            GC.SuppressFinalize(this);
        } 
    }
}