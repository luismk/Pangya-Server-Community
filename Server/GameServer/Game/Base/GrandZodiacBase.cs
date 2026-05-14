using Pangya_GameServer.Game.Base;
using Pangya_GameServer.Game.System;
using Pangya_GameServer.Models;
using Pangya_GameServer.PacketFunc;
using PangyaAPI.IFF.JP.Extensions;
using PangyaAPI.IFF.JP.Models.Flags;
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
    /// <summary>
    /// class responsavel pelo grand zodiac
    /// </summary>
    public abstract class GrandZodiacBase : TourneyBase
    {
        public int m_golden_beam_state = 0;                  // Status do golden beam

        public List<(Player _session, bool HioTime)> m_mp_golden_beam_player = new List<(Player _session, bool HioTime)>();            // Map de golden beam player, os player que fizeram hio no tempo do golden beam

        public List<double> m_initial_values_seed = new List<double>();              // Valores que passa com o pacote1EC

        protected IntPtr m_hEvent_sync_hole;//
        protected IntPtr m_hEvent_sync_hole_pulse;//trocar, para IntPtr se possivel

        protected List<stReward> m_rewards = new List<stReward>();
        stStateGrandZodiacSync m_state_gz = new stStateGrandZodiacSync();
        protected PangyaThread m_thread_sync_first_hole;
        public GrandZodiacBase(List<Player> _players,
        RoomInfoEx _ri, RateValue _rv,
        bool _channel_rookie) : base(_players, _ri, _rv, _channel_rookie)
        {


            // Aqui tem que inicializar os players info
            initAllPlayerInfo();

            init_values_seed();

            // Cria evento que vai para a thRead sync hole
            if ((m_hEvent_sync_hole = CreateEvent(IntPtr.Zero,
        true, false, null)) == IntPtr.Zero)
            {
                throw new exception("[GrandZodiacBase::GrandZodiacBase][Error] ao criar evento sync hole.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.APPROACH,
                    1050, GetLastError()));
            }

            // Cria evento que vai pulsar a thRead sync hole para ir mais r pido quando um player tacar
            if ((m_hEvent_sync_hole_pulse = CreateEvent(IntPtr.Zero,
                        false, false, null)) == IntPtr.Zero)
            {
                throw new exception("[GrandZodiacBase::GrandZodiacBase][Error] ao criar evento sync hole pulse.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.APPROACH,
                    1050, GetLastError()));
            }

            // Cria a thRead que vai sincronizar os player no hole
            m_thread_sync_first_hole = new PangyaThread(1060, obj => syncFirstHole(), this, ThreadPriority.AboveNormal);

        }

        private void finish_thread_sync_first_hole()
        {
            try
            {
                if (m_thread_sync_first_hole != null)
                {
                    if (m_hEvent_sync_hole != INVALID_HANDLE_VALUE)
                        SetEvent(m_hEvent_sync_hole);
                   
                    // Espera a thread terminar
                    m_thread_sync_first_hole.waitThreadFinish(-1); 
                }
            }
            catch (exception ex)
            {
                Console.WriteLine($"[GrandZodiacBase::FinishThreadSyncFirstHole][ErrorSystem] {ex.getFullMessageError()}");  
            }


            m_thread_sync_first_hole = null;

            if (m_hEvent_sync_hole != INVALID_HANDLE_VALUE)
                CloseHandle(m_hEvent_sync_hole);

            if (m_hEvent_sync_hole_pulse != INVALID_HANDLE_VALUE)
                CloseHandle(m_hEvent_sync_hole_pulse);

            m_hEvent_sync_hole = IntPtr.Zero;
            m_hEvent_sync_hole_pulse = IntPtr.Zero;
        }

        public override bool deletePlayer(Player _session, int _option)
        {

            if (_session == null)
            {
                throw new exception("[GrandZodiacBase::deletePlayer][Error] tentou deletar um player, mas o seu endereco eh nullptr.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GRAND_ZODIAC_BASE,
                    50, 0));
            }

            bool ret = false;

            try
            {
                var it = m_players.Find(c => c == _session);

                if (it != null)
                {
                    // byte opt = 3; // Saiu Quitou

                    if (m_game_init_state == 1)
                    {

                        var p = new PangyaBinaryWriter();

                        var pgi = INIT_PLAYER_INFO("deletePlayer",
                            "tentou sair do jogo",
                            _session);

                        var sessions = getSessions(it);

                        requestSaveInfo((it), 1 /*/ *Saiu * /*/);

                        requestUpdateItemUsedGame((it)); // Atualiza primeiro, por que o Grand Zodiac n o atualiza, a cada hole, s  no final

                        requestFinishItemUsedGame((it)); // Salva itens usados no Grand Zodiac

                        setGameFlag(pgi, PlayerGameInfo.eFLAG_GAME.QUIT);

                        // Resposta Player saiu do jogo MSG
                        p.init_plain(0x40);

                        p.WriteByte(2); // Player Saiu Msg

                        p.WriteString(it.m_pi.nickname);

                        p.WriteUInt16(0); // size Msg, n o precisa de msg o pangya j  manda na opt 2

                        packet_func.vector_send(p,
                            sessions, 1);

                        if (AllCompleteGameAndClear())
                        {
                            ret = true; // Termina o Grand Zodiac
                        }
                    }

                    // Delete Player
                    m_players.Remove(it);

                }
                else
                {
                    _smp.message_pool.getInstance().push(new message("[GrandZodiacBase::deletePlayer][Warning] player ja foi excluido do game.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[GrandZodiacBase::deletePlayer][Error] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

            }

            return ret;
        }

        public void deleteAllPlayer()
        {
            // Percorre de trás para frente
            for (int i = m_players.Count - 1; i >= 0; i--)
            {
                var player = m_players[i];
                if (player != null)
                {
                    var pgi = getPlayerInfo(player);
                    if (pgi != null)
                    {
                        deletePlayer(player, 0);
                    }
                }
            }
        }

        public override void sendInitialData(Player _session)
        {
            //CHECK_SESSION_BEGIN("sendInitialData");

            try
            {

                // Envia aqui os valores do hole, size_cup
                var p = new PangyaBinaryWriter((ushort)0x1F9);

                var size_cup = _session.m_pi.getSizeCupGrandZodiac();

                p.WriteInt32(size_cup); // Start Hole Size Cup
                p.WriteInt32(size_cup); // Finish size cup (OU O tee acho que seja)
                p.WriteUInt32((uint)_session.m_pi.grand_zodiac_pontos); // Aqui acho que   os pangs que ele faz por cada hio, com rela  o ao size do cup (OU OS PONTOS DO GRAND ZODIAC)

                packet_func.session_send(p,
                    _session, 1);

                // Send Initial Data, por m ele tamb m   sincronizado, s  envia os dados quando todos fizer o mesmo pedido
                base.sendInitialData(_session);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[GrandZodiacBase::sendInitialData][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }


        public override void requestInitHole(Player _session, packet _packet)
        {
            ////REQUEST_BEGIN("InitHole");

            var p = new PangyaBinaryWriter();

            try
            {
                #region Read Packet
                stInitHole ctx_hole = new stInitHole().ToRead(_packet);
                #endregion

                var hole = m_course.findHole(ctx_hole.numero);

                if (hole == null)
                {
                    throw new exception("[GrandZodiacBase::requestInitHole][Error] course->findHole nao encontrou o hole retonou nullptr, o server esta com erro no init course do GrandZodiacBase.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GRAND_ZODIAC_BASE,
                        2555, 0));
                }

                hole.init(ctx_hole.tee, ctx_hole.pin);

                var pgi = INIT_PLAYER_INFO("requestInitHole",
                    "tentou inicializar o hole[NUMERO = " + (hole.getNumero()) + "] no jogo",
                    _session);

                // Update Location Player in Hole
                pgi.location.x = ctx_hole.tee.x;
                pgi.location.z = ctx_hole.tee.z;

                // N mero do hole atual, que o player est  jogandp
                pgi.hole = ctx_hole.numero;

                // Flag que marca se o player j  inicializou o primeiro hole do jogo
                if (!pgi.init_first_hole)
                {
                    pgi.init_first_hole = true;
                }

                // Gera degree para o player ou pega o degree sem gerar que   do modo do hole repeat
                pgi.degree = (m_ri.getModo() == RoomInfo.ROOM_INFO_MODO.M_REPEAT) ? hole.getWind().degree.getDegree() : hole.getWind().degree.getShuffleDegree();

                // Resposta de tempo do hole
                p.init_plain(0x9E);

                p.WriteUInt16(hole.getWeather());
                p.WriteByte(0); // Option do tempo, sempre peguei zero aqui dos pacotes que vi

                packet_func.session_send(p,
                    _session, 1);

                var wind_flag = initCardWindPlayer(pgi, hole.getWind().wind);

                // Resposta do vento do hole
                p.init_plain(0x5B);

                p.WriteByte(hole.getWind().wind + wind_flag);
                p.WriteByte((wind_flag < 0) ? 1 : 0); // Flag de card de vento, aqui   a qnd diminui o vento, 1 Vento azul
                p.WriteUInt16(pgi.degree);
                p.WriteByte(1); // Flag do vento, 1 Reseta o Vento, 0 soma o vento que nem o comando gm \wind do pangya original

                packet_func.session_send(p,
                    _session, 1);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[GrandZodiacBase::requestInitHole][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

            }
        }


        public override bool requestFinishLoadHole(Player _session, packet _packet)
        {
            ////REQUEST_BEGIN("FinishLoadHole");

            var p = new PangyaBinaryWriter();

            bool ret = false;

            try
            {

                var size_cup = _packet.ReadUInt32();
                 
                var pgi = INIT_PLAYER_INFO("requestFinishLoadHole",
                    "tentou finalizar carregamento do hole no jogo",
                    _session);

                pgi.finish_load_hole = 1;

                // Valores de double, aleat rio que passar, pode ser s  da rota  o da camera
                // Mas vou deixar o mesmo valor para todos, para fazer um teste
                foreach (var el in m_initial_values_seed)
                {

                    p.init_plain(0x1EC);

                    p.WriteDouble(el);

                    packet_func.session_send(p,
                        _session, 1);
                }

                // Primeiro pacote dizendo que terminou de carregar o course GZ
                p.init_plain(0x201);

                packet_func.session_send(p,
                    _session, 1);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[GrandZodiacBase::requestFinishLoadHole][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            return ret;
        }

        public override void requestFinishCharIntro(Player _session, packet _packet)
        {
            ////REQUEST_BEGIN("FinishCharIntro");

            var p = new PangyaBinaryWriter();

            try
            {

                var pgi = INIT_PLAYER_INFO("requestFinishCharIntro",
                    "tentou finalizar intro do char no jogo",
                    _session);

                pgi.finish_char_intro = 1;

                pgi.data.tacada_num = 0;

                // Giveup Flag
                pgi.data.giveup = 0;

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[GrandZodiacBase::requestFinishCharIntro][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void requestInitShot(Player _session, packet _packet)
        {
            try
            {
                ShotDataEx sd = new ShotDataEx();

                // Power Shot
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
                // sd.spend_time_game = _packet.ReadSingle();

                #endregion
                var pgi = INIT_PLAYER_INFO("requestInitShot",
                    "tentou iniciar tacada no jogo",
                    _session);

                pgi.shot_data = sd;

                // Aqui seta o state e verifica se   para mandar a resposta
                if (pgi.m_sync_shot_gz.setStateAndCheckAllAndClear(SyncShotGrandZodiac.eSYNC_SHOT_GRAND_ZODIAC_STATE.SSGZS_FIRST_SHOT_INIT))
                {
                    sendReplyInitShotAndSyncShot(_session);
                }

#if DEBUG
                // Log Shot Data Ex
                //_smp.message_pool.getInstance().push(new message("[GrandZodiacBase::requestInitShot][Log] Log Shot Data Ex:\n\r" + sd.ToString(), type_msg.CL_FILE_LOG_AND_CONSOLE));
#endif // DEBUG

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[GrandZodiacBase::requestInitShot][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }
        public override void requestActiveBooster(Player _session, packet _packet)
        {
            ////REQUEST_BEGIN("ActiveBooster");

            var p = new PangyaBinaryWriter();

            try
            {

                // Grand Zodiac o Booster   gr tis
                float velocidade = _packet.ReadFloat();

                // Resposta para Active Booster
                p.init_plain(0xC7);

                p.WriteFloat(velocidade);
                p.WriteInt32(_session.m_oid);

                packet_func.session_send(p,
                    _session, 1);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[GrandZodiacBase::requestActiveBooster][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void requestActiveCutin(Player _session, packet _packet)
        {
            ////REQUEST_BEGIN("ActiveCutin");

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

                // Resposta para Active Cutin
                p.init_plain(0x18D);

                p.WriteByte(0); // OK

                p.WriteUInt16(3); // Cuttin do Grand Zodiac, Cuttin desativado

                packet_func.session_send(p,
                    _session, 1);

                // No Modo GrandZodic, n o envia Cutin, ent o envia o pacote18D com option 0(Uint8), e valor 3(Uint16)

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[GrandZodiacBase::requestActiveCutin][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x18D);

                p.WriteByte(0); // OPT

                p.WriteUInt16(1); // Error

                packet_func.session_send(p,
                    _session, 1);
            }
        }

        public override void requestStartFirstHoleGrandZodiac(Player _session, packet _packet)
        {
            ////REQUEST_BEGIN("StartFirstHoleGrandZodiac");

            var p = new PangyaBinaryWriter();

            try
            {

                var pgi = INIT_PLAYER_INFO("requestStartFirstHoleGrandZodiac",
                    "tentou inicializar o primeiro hole do grand zodiac",
                    _session);

                // Aqui tem que sincronizar
                setInitFirstHole(pgi);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[GrandZodiacBase::requestStartFirstHoleGrandZodiac][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void requestReplyInitialValueGrandZodiac(Player _session, packet _packet)
        {
            ////REQUEST_BEGIN("ReplyInitialValueGrandZodiac");

            try
            {

                double value = _packet.ReadDouble();

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[GrandZodiacBase::requestReplyInitialValueGrandZodiac][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override bool requestFinishGame(Player _session, packet _packet)
        {
            ////REQUEST_BEGIN("FinishGame");

            bool ret = false;

            try
            {

#if DEBUG
                //_smp.message_pool.getInstance().push(new message("[GrandZodiacBase::requestFinishGame][Log] Packet Hex: " + _packet.Log(), type_msg.CL_FILE_LOG_AND_CONSOLE));
#endif // DEBUG

                // Packet0CB
                ret = finish_game(_session, 0x12C);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[GrandZodiacBase::requestFinishGame][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            return ret;
        }
        public override void sendRemainTime(Player _session)
        {

            try
            {
                // Resposta tempo percorrido do Tourney
                var p = new PangyaBinaryWriter((ushort)0x8D);

                p.WriteUInt32(0u); // Grand Zodiac, passa o tempo decorrido no pacote200

                packet_func.session_send(p,
                    _session, 1);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[GrandZodiacBase::sendRemainTime][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }
        public override void requestFinishHole(Player _session, int option)
        {

            var pgi = INIT_PLAYER_INFO("requestFinishHole",
                "tentou finalizar o dados do hole do player no jogo",
                _session);

            var hole = m_course.findHole(pgi.hole);

            if (hole == null)
            {
                throw new exception("[GrandZodiacBase::finishHole][Error] PLAYER[UID=" + (_session.m_pi.uid) + "] tentou finalizar hole[NUMERO=" + ((ushort)pgi.hole) + "] no jogo, mas o numero do hole is invalid. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GRAND_ZODIAC_BASE,
                    20, 0));
            }

            int score_hole = 0;

            // Finish Hole Dados
            if (option == 0)
            {
                // Score Player
                var p = new PangyaBinaryWriter((ushort)0x1EF);

                p.WriteInt32(_session.m_oid);

                p.WriteInt32(pgi.m_gz.total_score);

                p.WriteInt32(pgi.data.score);

                p.WriteInt32(pgi.m_gz.total_score + pgi.data.score);

                packet_func.game_broadcast(this,
                    p, 1);


                pgi.data.total_tacada_num += pgi.data.tacada_num;
                // Tacadas do hole
                var tacada_hole = pgi.data.tacada_num;
                // Score do hole
                score_hole = Convert.ToInt32(pgi.data.tacada_num);

                pgi.m_gz.total_score += score_hole;

                pgi.m_gz.hole_in_one++;

                // Zera dados
                pgi.data.time_out = 0;

                pgi.data.tacada_num = 0;

                // Zera o score, que o Grand Zodiac usa o total_score
                pgi.data.score = 0;

                // Giveup Flag
                pgi.data.giveup = 0;

                // Zera as penalidades do hole
                pgi.data.penalidade = 0;

            }
            else if (option == 1)
            { // N o acabou o hole ent o faz os calculos para o jogo todo

                // Zera dados
                pgi.data.time_out = 0;

                pgi.data.tacada_num = 0;

                // Zera o score, que o Grand Zodiac usa o total_score
                pgi.data.score = 0;

                // Giveup Flag
                pgi.data.giveup = 0;

                // Zera as penalidades do hole do player
                pgi.data.penalidade = 0;
            }

            // Aqui tem que atualiza o PGI direitinho com outros dados
            pgi.progress.hole = (short)m_course.findHoleSeq(pgi.hole);

            // Dados Game Progress do Player
            if (option == 0)
            {

                if (pgi.progress.hole > 0)
                {

                    if (pgi.shot_sync.state_shot.display.acerto_hole)
                    {
                        pgi.progress.finish_hole[pgi.progress.hole - 1] = 1; // Terminou o hole
                    }

                    pgi.progress.par_hole[pgi.progress.hole - 1] = hole.getPar().par;
                    pgi.progress.score[pgi.progress.hole - 1] = (sbyte)score_hole;
                    pgi.progress.tacada[pgi.progress.hole - 1] = pgi.data.tacada_num;
                }

            }
            else
            {

                var pair = m_course.findRange(pgi.hole);
                foreach (var it in pair)
                {
                    if (it.Key <= m_ri.qntd_hole)
                    {
                        continue;
                    }

                    pgi.progress.finish_hole[it.Key - 1] = 0; // n o terminou

                    pgi.progress.par_hole[it.Key - 1] = it.Value.getPar().par;

                    pgi.progress.score[it.Key - 1] = it.Value.getPar().range_score[1]; // Max Score

                    pgi.progress.tacada[it.Key - 1] = it.Value.getPar().total_shot;
                }
            }
        }

        public override void requestUpdateItemUsedGame(Player _session)
        {

            var pgi = INIT_PLAYER_INFO("requestUpdateItemUsedGame",
                "tentou atualizar itens usado no jogo",
                _session);

            var ui = pgi.used_item;

            // Passive Item exceto Time Booster e Auto Command, que soma o contador por uso, o cliente passa o pacote, dizendo que usou o item
            foreach (var el in ui.v_passive)
            {

                // Verica se   o ultimo hole, terminou o jogo, ai tira soma 1 ao count do pirulito que consome por jogo
                if (CHECK_PASSIVE_ITEM(el.Value._typeid)
                    && el.Value._typeid != TIME_BOOSTER_TYPEID /*/ *Time Booster * /*/

                    && el.Value._typeid != AUTO_COMMAND_TYPEID)
                {

                    // Item de Exp Boost que s  consome 1 Por Jogo, s  soma no requestFinishItemUsedGame
                    if (passive_item_exp_1perGame.Any(c => c == el.Value._typeid))
                    {
                        el.Value.count = (pgi.data.total_tacada_num / 4); // Gasta 1 a cada 4 tacadas
                    }

                }
                else if (sIff.getInstance().getItemGroupIdentify(el.Value._typeid) == PangyaAPI.IFF.JP.Models.Flags.IFF_GROUP.BALL/* / *Ball * /*/ || sIff.getInstance().getItemGroupIdentify(el.Value._typeid) == PangyaAPI.IFF.JP.Models.Flags.IFF_GROUP.AUX_PART)
                {
                    el.Value.count = (pgi.data.total_tacada_num / 4); // uma comet e um anel por 4 tacadas
                }
            }
        }

        public override void requestTranslateSyncShotData(Player _session, ShotSyncData _ssd)
        {
            //CHECK_SESSION_BEGIN("requestTranslateSyncShotData");

            try
            {

                var s = findSessionByOID(_ssd.oid);

                if (s == null)
                {
                    throw new exception("[GrandZodiacBase::requestTranslateSyncShotData][Error] PLAYER[UID=" + (_session.m_pi.uid) + "] tentou sincronizar tacada do PLAYER[OID=" + (_ssd.oid) + "], mas o player nao existe nessa jogo. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GRAND_ZODIAC_BASE,
                        200, 0));
                }

                // Update Sync Shot Player
                if (_session.m_pi.uid == s.m_pi.uid)
                {

                    var pgi = INIT_PLAYER_INFO("requestTranslateSyncShotData",
                        "tentou sincronizar a tacada no jogo",
                        _session);

                    pgi.shot_sync = _ssd;

                    // Last Location Player
                    var last_location = pgi.location;


                    // Update Pang and Bonus Pang
                    pgi.data.pang = _ssd.pang;
                    pgi.data.bonus_pang = _ssd.bonus_pang;

                    // J  s  na fun  o que come a o tempo do player do turno
                    pgi.data.tacada_num++;
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[GrandZodiacBase::requestTranslateSyncShotData][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void requestSaveInfo(Player _session, int option)
        {

            var pgi = INIT_PLAYER_INFO("requestSaveInfo",
                "tentou salvar o info dele no jogo",
                _session);

            try
            {
                if (option == 1)
                { // Saiu

                    // Zera os pangs ele saiu
                    pgi.data.pang = 0Ul;
                    pgi.data.bonus_pang = 0Ul;
                }

                // Limpa o User Info por que n o add nada, s  o tempo e os pangs ganhos
                pgi.ui.clear();

                var diff = UtilTime.GetLocalTimeDiff(m_start_time);

                if (diff > 0)
                {
                    diff /= STDA_10_MICRO_PER_SEC; // NanoSeconds To Seconds
                }

                pgi.ui.tempo = (int)diff;

                // Pode tirar pangs
                ulong total_pang = (pgi.data.pang + pgi.data.bonus_pang);

                // Adiciona o Jackpot, se ele ganhou
                if (option != 1 && pgi.m_gz.jackpot > 0)
                {
                    total_pang += pgi.m_gz.jackpot;
                }

                // UPDATE ON SERVER AND DB
                _session.m_pi.addUserInfo(pgi.ui, (ulong)total_pang); // add User Info

                if (total_pang > 0)
                {
                    _session.m_pi.addPang((ulong)total_pang); // add Pang
                }
                else if (total_pang < 0)
                {
                    _session.m_pi.consomePang((ulong)(Convert.ToInt64(total_pang) * -1)); // consome Pangs
                }
            }
            catch (exception e)
            {
                _smp.message_pool.getInstance().push(new message("[GrandZodiacBase::requestSaveInfo][Error] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override void requestCalculeRankPlace()
        {

            if (!m_player_order.empty())
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
            m_player_order.Sort(sort_grand_zodiac_rank_place);

            uint position = 1;
            int score = -1;

            PlayerGrandZodiacInfo pgzi = null;

            // Calcula posi  es, o Grand Zodiac tem player com a mesma posi  o se eles terminarem com o mesmo score
            foreach (var el in m_player_order)
            {


                if (el.flag != PlayerGameInfo.eFLAG_GAME.QUIT && (pgzi = (PlayerGrandZodiacInfo)(el)) != null)
                {

                    if (score == -1)
                    {

                        pgzi.m_gz.position = position;

                        score = pgzi.m_gz.total_score;

                    }
                    else if (score == pgzi.m_gz.total_score)
                    {
                        pgzi.m_gz.position = position;
                    }
                    else
                    {
                        pgzi.m_gz.position = ++position;
                    }
                }
            }
        }

        public static int sort_grand_zodiac_rank_place(PlayerGameInfo _pgi1, PlayerGameInfo _pgi2)
        {

            // All nullptr
            if (_pgi1 == null && _pgi2 == null)
            {
                return 0;
            }

            if (_pgi1 != null && _pgi2 == null)
            {
                return 1;
            }
            else if (_pgi1 == null && _pgi2 != null)
            {
                return 0;
            }


            return ((PlayerGrandZodiacInfo)_pgi1).m_gz.total_score > ((PlayerGrandZodiacInfo)(_pgi2)).m_gz.total_score ? 1 : 0;
        }

        public override void requestReplySyncShotData(Player _session)
        {
            //CHECK_SESSION_BEGIN("requestReplySyncShotData");

            try
            {

                var pgi = INIT_PLAYER_INFO("requestReplySyncShotData",
                    "tentou enviar a resposta do Sync Shot do jogo",
                    _session);

                // Resposta Sync Shot
                sendSyncShot(_session);

                // Deixai assim por que o Original manda a msg depois, pelo que estava no outro server
                drawDropItem(_session);

                // Aqui seta o state e verifica se   para mandar a resposta
                if (pgi.m_sync_shot_gz.setStateAndCheckAllAndClear(SyncShotGrandZodiac.eSYNC_SHOT_GRAND_ZODIAC_STATE.SSGZS_FIRST_SHOT_SYNC))
                {
                    sendReplyInitShotAndSyncShot(_session);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[GrandZodiacBase::requestReplySyncShotData][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }
        public override void sendPlacar(Player _session)
        {

            try
            {

                var pgi = INIT_PLAYER_INFO("sendPlacar",
                    "tentou enviar o placar do jogo",
                    _session);

                var p = new PangyaBinaryWriter((ushort)0x1F3);

                p.WriteUInt32((uint)((pgi.flag == PlayerGameInfo.eFLAG_GAME.FINISH) ? 1 : 2)); // 1 Terminou, 2 Saiu

                p.WriteUInt32(getCountPlayersGame());

                PlayerGrandZodiacInfo pgzi = null;

                foreach (var el in m_player_info)
                {


                    if (el.Value != null
                        && el.Value.flag != PlayerGameInfo.eFLAG_GAME.QUIT
                        && (pgzi = (PlayerGrandZodiacInfo)(el.Value)) != null)
                    {

                        p.WriteInt32(pgzi.oid);
                        p.WriteUInt32(pgzi.m_gz.position);
                        p.WriteInt32(pgzi.m_gz.total_score);
                        p.WriteUInt32(pgzi.m_gz.hole_in_one);
                        p.WriteInt32(pgzi.data.total_tacada_num);
                        p.WriteUInt32(pgzi.m_gz.pontos);
                        p.WriteInt32(pgzi.data.exp);
                        p.WriteUInt64(pgzi.data.pang);
                        p.WriteUInt64(pgzi.data.bonus_pang);
                        p.WriteUInt64(pgzi.m_gz.jackpot);
                        p.WriteUInt32(pgzi.m_gz.trofeu);

                        if (!pgzi.drop_list.v_drop.empty())
                        {

                            p.WriteUInt32((uint)pgzi.drop_list.v_drop.Count()); // Count

                            foreach (var el2 in pgzi.drop_list.v_drop)
                            {

                                p.WriteUInt32(el2._typeid);
                                p.WriteUInt32((uint)el2.qntd);
                            }

                        }
                        else
                        {
                            p.WriteUInt32(0u); // N o ganhou drop item
                        }
                    }
                }

                packet_func.session_send(p,
                    _session, 1);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[GrandZodiacBase::sendPlacar][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override int checkEndShotOfHole(Player _session)
        {

            try
            {

                // Agora verifica o se ele acabou o hole e essas coisas
                var pgi = INIT_PLAYER_INFO("checkEndShotOfHole",
                    "tentou verificar a ultima tacada do hole no jogo",
                    _session);

                if (pgi.shot_sync.state_shot.display.acerto_hole || pgi.data.giveup > 0)
                {

                    // Finish Hole and change
                    finishHole(_session);

                    changeHole(_session);

                }
                else // Update Shot
                {
                    updateFinishHole(_session, 0 /*/ *N o fez hio * /*/);
                }

                // Limpa, terminou a tacada
                pgi.m_gz.m_score_shot.Clear();

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[GrandZodiacBase::checkEndShotOfHole][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            return 0;
        }
        
public void init_values_seed()
        {

            if (!m_initial_values_seed.empty())
            {
                m_initial_values_seed.Clear();
            }

            var rnd = new Random();

            double var_d = ((rnd.Next() % 1000) / 100.0f) + 1.0f;

            for (var i = 0; i < 10u; ++i)
            {
                m_initial_values_seed.Add((var_d + i * ((rnd.Next() % 10) / 10.0f) + 1.0f));
            }
        }
        
public void nextHole(Player _session)
        {

            try
            {

                var pgi = INIT_PLAYER_INFO("nextHole",
                    "tentou trocar o hole no jogo",
                    _session);

                var p = new PangyaBinaryWriter((ushort)0x1F4);

                p.WriteUInt32(1u); // Inicializa o pr ximo hole

                packet_func.session_send(p,
                    _session, 1);

                // Wind
                var hole = m_course.findHole(pgi.hole);

                if (hole == null)
                {
                    throw new exception("[GrandZodiacBase::requestInitHole][Error] course->findHole nao encontrou o hole retonou nullptr, o server esta com erro no init course do Chip-in Practice.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHIP_IN_PRACTICE,
                        2555, 0));
                }

                var wind = m_course.shuffleWind((uint)new Random().Next());

                hole.setWind(wind);

                // Gera degree para o player ou pega o degree sem gerar que   do modo do hole repeat
                pgi.degree = hole.getWind().degree.getShuffleDegree();

                var wind_flag = initCardWindPlayer(pgi, hole.getWind().wind);

                // Resposta do vento do hole
                p.init_plain(0x5B);

                p.WriteByte(hole.getWind().wind + wind_flag);
                p.WriteByte((wind_flag < 0) ? 1 : 0); // Flag de card de vento, aqui   a qnd diminui o vento, 1 Vento azul
                p.WriteUInt16(pgi.degree);
                p.WriteByte(1/* / *Reseta * /*/); // Flag do vento, 1 Reseta o Vento, 0 soma o vento que nem o comando gm \wind do pangya original

                packet_func.session_send(p,
                    _session, 1);

                // Remain Time em segundos
                var remain_time = 0L;

                if (m_timer != null)
                {
                    remain_time = m_timer.getElapsed();
                }

                if (remain_time > 0)
                {
                    remain_time /= 1000/* / *Milli por segundos * /*/;
                }

                p.init_plain(0x200);

                p.WriteUInt32((uint)remain_time);

                packet_func.session_send(p,
                    _session, 1);

                // Update, finaliza o hole
                updateFinishHole(_session, 1/* / *Fez HIO * /*/);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[GrandZodiacBase::nextHole][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void setInitFirstHole(PlayerGrandZodiacInfo _pgi)
        {

            if (_pgi == null)
            {

                _smp.message_pool.getInstance().push(new message("[GrandZodiacBase::setInitFirstHole][Error] PlayerGrandZodiacInfo* _pgi is invalid(nullptr).", type_msg.CL_FILE_LOG_AND_CONSOLE));

                return;
            }


            // Set
            _pgi.init_first_hole_gz = 1;
            if (m_hEvent_sync_hole_pulse != INVALID_HANDLE_VALUE)
                SetEvent(m_hEvent_sync_hole_pulse);
        }
        public bool checkAllInitFirstHole()
        {

            uint count = 0;

            // Check
            m_players.ForEach(_el =>
            {
                try
                {
                    var pgi = INIT_PLAYER_INFO("checkAllInitFirstHole",
                        "tentou verificar se todos os player terminaram de inicializar o primeiro hole do Grand Zodiac no jogo",
                        _el);
                    if (pgi.init_first_hole_gz == 1)
                    {
                        count++;
                    }
                }
                catch (exception e)
                {
                    _smp.message_pool.getInstance().push(new message("[GrandZodiacBase::checkAllInitFirstHole][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
            });



            return (count == m_players.Count);
        }
        
public void clearInitFirstHole()
        {
            clear_all_init_first_hole();
        }
        public bool setInitFirstHoleAndCheckAllInitFirstHoleAndClear(PlayerGrandZodiacInfo _pgi)
        {

            if (_pgi == null)
            {

                _smp.message_pool.getInstance().push(new message("[GrandZodiacBase::setInitFirstHoleAndCheckAllInitFirstHoleAndClear][Error] PlayerGrandZodiacInfo* _pgi is invalid(nullptr).", type_msg.CL_FILE_LOG_AND_CONSOLE));

                return false;
            }

            uint count = 0;
            bool ret = false;
            // Set
            _pgi.init_first_hole_gz = 1;

            // Check
            m_players.ForEach(_el =>
            {
                try
                {
                    var pgi = INIT_PLAYER_INFO("setInitFirstHoleAndCheckAllInitFirstHoleAndClear",
                        "tentou verificar se todos os player terminaram de inicializar o primeiro hole do Grand Zodiac no jogo",
                        _el);
                    if (pgi.init_first_hole_gz == 1)
                    {
                        count++;
                    }
                }
                catch (exception e)
                {
                    _smp.message_pool.getInstance().push(new message("[GrandZodiacBase::setInitFirstHoleAndCheckAllInitFirstHoleAndClear][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
            });



            ret = (count == m_players.Count);

            // Clear
            if (ret)
            {
                clear_all_init_first_hole();
            }

            return ret;
        }
        
public void setEndGame(PlayerGrandZodiacInfo _pgi)
        {

            if (_pgi == null)
            {

                _smp.message_pool.getInstance().push(new message("[GrandZodiacBase::setEndGame][Error] PlayerGrandZodiacInfo* _pgi is invalid(nullptr).", type_msg.CL_FILE_LOG_AND_CONSOLE));

                return;
            }
            // Set
            _pgi.end_game = 1;

            if (m_hEvent_sync_hole_pulse != INVALID_HANDLE_VALUE)
                SetEvent(m_hEvent_sync_hole_pulse);
        }
        public bool checkAllEndGame()
        {

            uint count = 0;

            // Check
            m_players.ForEach(_el =>
            {
                try
                {
                    var pgi = INIT_PLAYER_INFO("checkAllEndGame",
                        "tentou verificar se todos os player terminaram o jogo no Grand Zodiac",
                        _el);
                    if (pgi.end_game > 0)
                    {
                        count++;
                    }
                }
                catch (exception e)
                {
                    _smp.message_pool.getInstance().push(new message("[GrandZodiacBase::checkAllEndGame][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
            });



            return (count == m_players.Count);
        }
        
public void clearEndGame()
        {
            clear_all_end_game();
        }
        public bool setEndGameAndCheckAllEndGameAndClear(PlayerGrandZodiacInfo _pgi)
        {

            if (_pgi == null)
            {

                _smp.message_pool.getInstance().push(new message("[GrandZodiacBase::setEndGameAndCheckAllEndGameAndClear][Error] PlayerGrandZodiacInfo* _pgi is invalid(nullptr).", type_msg.CL_FILE_LOG_AND_CONSOLE));

                return false;
            }

            uint count = 0;
            bool ret = false;
            // Set
            _pgi.end_game = 1;

            // Check
            m_players.ForEach(_el =>
            {
                try
                {
                    var pgi = INIT_PLAYER_INFO("setEndGameAndCheckAllEndGameAndClear",
                        "tentou verificar se todos os player terminaram o jogo no Grand Zodiac",
                        _el);
                    if (pgi.end_game > 0)
                    {
                        count++;
                    }
                }
                catch (exception e)
                {
                    _smp.message_pool.getInstance().push(new message("[GrandZodiacBase::setInitFirstHoleAndCheckAllInitFirstHoleAndClear][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
            });



            ret = (count == m_players.Count);

            // Clear
            if (ret)
            {
                clear_all_end_game();
            }

            return ret;
        }
        
public void clear_all_init_first_hole()
        {

            m_players.ForEach(_el =>
            {
                try
                {
                    var pgi = INIT_PLAYER_INFO("clear_all_init_first_hole",
                        " tentou limpar all init first hole do Grand Zodiac no jogo",
                        _el);
                    pgi.init_first_hole_gz = 0;
                }
                catch (exception e)
                {
                    _smp.message_pool.getInstance().push(new message("[GrandZodiacBase::clear_all_init_first_hole][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
            });


        }
        
public void clear_all_end_game()
        {

            m_players.ForEach(_el =>
            {
                try
                {
                    var pgi = INIT_PLAYER_INFO("clear_all_end_game",
                        " tentou limpar all end game do Grand Zodiac",
                        _el);
                    pgi.end_game = 0;
                }
                catch (exception e)
                {
                    _smp.message_pool.getInstance().push(new message("[GrandZodiacBase::clear_all_end_game][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
            });
        }
        
public void sendReplyInitShotAndSyncShot(Player _session)
        {

            try
            {

                var pgi = INIT_PLAYER_INFO("sendReplyInitShotAndSyncShot",
                    "tentou enviar a resposta da tacada do player no Grand Zodiac",
                    _session);

                if (pgi.shot_sync.state_shot.display.acerto_hole || pgi.data.giveup > 0)
                {

                    // Set Player no golden beam map se j  estiver no tempo do golden beam e se ele n o estiver no map
                    if (Interlocked.Exchange(ref m_golden_beam_state, 1) == 1)
                    {

                        int check_m = 1; // Compare
                        if (Interlocked.CompareExchange(ref m_golden_beam_state, check_m, 1) == 1)
                        {

                            setPlayerGoldenBeam(_session);
                        }
                    }

                    if (pgi.shot_sync.state_shot.shot.cobra > 0
                        || pgi.shot_sync.state_shot.shot.tomahawk > 0
                        || pgi.shot_sync.state_shot.shot.spike > 0)
                    {

                        pgi.m_gz.m_score_shot.Add(eGRAND_ZODIAC_TYPE_SHOT.GZTS_SPECIAL_SHOT);

                        pgi.data.score++;
                    }

                    if (pgi.data.tacada_num == 1)
                    {

                        pgi.m_gz.m_score_shot.Add(eGRAND_ZODIAC_TYPE_SHOT.GZTS_FIRST_SHOT);

                        pgi.data.score++;
                    }

                    // Sem setas, se ele n o mandou nenhum special shot ou spin ou curva ele n o apertou setas, s  as apagadas essas n o conta
                    if (pgi.shot_data.special_shot.ulSpecialShot == 0u)
                    {

                        pgi.m_gz.m_score_shot.Add(eGRAND_ZODIAC_TYPE_SHOT.GZTS_WITHOUT_COMMANDS);

                        pgi.data.score++;
                    }

                    if (pgi.shot_data.acerto_pangya_flag == 3)
                    {

                        pgi.m_gz.m_score_shot.Add(eGRAND_ZODIAC_TYPE_SHOT.GZTS_MISS_PANGYA);

                        pgi.data.score += 3;
                    }

                    // O score que o player fez hio
                    pgi.m_gz.m_score_shot.Add(eGRAND_ZODIAC_TYPE_SHOT.GZTS_HIO_SCORE);

                    pgi.data.score++; // Add +1 que   da tacada que ele deu, que ele fez hio

                    // S  envia se tiver mais que 1
                    if (pgi.m_gz.m_score_shot.Count > 1)
                    {

                        // Send Scores para o player
                        var p = new PangyaBinaryWriter((ushort)0x1F5);

                        p.WriteUInt32((uint)pgi.m_gz.m_score_shot.Count);

                        foreach (var el in pgi.m_gz.m_score_shot)
                        {
                            p.WriteUInt32(el);
                        }

                        packet_func.session_send(p,
                            _session, 1);
                    }
                }
            }
            catch (exception e)
            {
                _smp.message_pool.getInstance().push(new message("[GrandZodiacBase::SendReplyInitShotAndSyncShot][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public override PlayerGameInfo makePlayerInfoObject(Player _session)
        {

            var pgzi = new PlayerGrandZodiacInfo();

            try
            {
                // Aqui se eu precisar inicializar algum valor

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[GrandZodiacBase::makePlayerInfoObject][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            return pgzi;
        }

        public void setPlayerGoldenBeam(Player _session)
        {

            try
            {

                // N o tem o player no map add
                if (m_mp_golden_beam_player.FirstOrDefault(c => c._session == _session)._session != null)
                {
                    m_mp_golden_beam_player.Add((_session, true));
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[GrandZodiacBase::setPlayerGoldenBeam][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }


        public object syncFirstHole()
        {
            var datetime = Stopwatch.StartNew();
            TimeSpan ts = datetime.Elapsed;
            try
            {
                string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}", ts.Hours, ts.Minutes, ts.Seconds);

                _smp.message_pool.getInstance().push(new message($"[GrandZodiacBase::syncFirstHole][Log] Partida comecou: {elapsedTime}", type_msg.CL_FILE_LOG_AND_CONSOLE));

                uint retWait = WAIT_TIMEOUT;//
                IntPtr[] wait_events = { m_hEvent_sync_hole, m_hEvent_sync_hole_pulse };

                while ((retWait = WaitForMultipleObjects((uint)wait_events.Length, wait_events, false, 1000 /*1 segundo*/)) == WAIT_TIMEOUT || retWait == (WAIT_OBJECT_0 + 1))
                {
                    try
                    {

                        m_state_gz.@lock();

                        switch (m_state_gz.getState())
                        {
                            case eSTATE_GRAND_ZODIAC_SYNC.FIRST_HOLE:
                                {

                                    if (checkAllInitFirstHole())
                                    {

                                        clearInitFirstHole();

                                        // Come a o tempo do jogo
                                        startTime();

                                        foreach (var el in m_players)
                                        {

                                            if (el != null)
                                            {

                                                sendRemainTime(el);
                                                var p = new PangyaBinaryWriter();
                                                // Resposta passa o oid do player que vai come a o Hole
                                                p.init_plain(0x53);

                                                p.WriteInt32(el.m_oid);

                                                packet_func.session_send(p,
                                                    el, 1);

                                                // Passa a localiza  o do player, esse   a primeira, vez ent o passa os valores zerados
                                                updateFinishHole(el, 1/* / *Come ou o hole * /*/);

                                                // Come a o Grand Zodiac
                                                p.init_plain(value: (ushort)0x1F4);

                                                p.WriteInt32(1); // Start

                                                packet_func.session_send(p,
                                                    el, 1);
                                            }
                                        }

                                        // Verifica o tempo do start golden beam
                                        m_state_gz.setState(eSTATE_GRAND_ZODIAC_SYNC.START_GOLDEN_BEAM);
                                    }

                                    break;
                                }
                            case eSTATE_GRAND_ZODIAC_SYNC.START_GOLDEN_BEAM:
                                {

                                    if (m_timer != null)
                                    {

                                        var elapsed = m_timer.getElapsed();

                                        if (elapsed >= (m_ri.time_30s - 60000))
                                        {

                                            // Come a o tempo do golden beam time
                                            startGoldenBeam();

                                            // Verifica o tempo do end golden beam
                                            m_state_gz.setState(eSTATE_GRAND_ZODIAC_SYNC.END_GOLDEN_BEAM);
                                        }
                                    }

                                    break;
                                }
                            case eSTATE_GRAND_ZODIAC_SYNC.END_GOLDEN_BEAM:
                                {
                                    if (m_timer != null)
                                    {

                                        var elapsed = m_timer.getElapsed();

                                        if (elapsed >= (m_ri.time_30s - 30000))
                                        {

                                            // Terminar o golden beam
                                            endGoldenBeam();

                                            m_state_gz.setState(eSTATE_GRAND_ZODIAC_SYNC.WAIT_END_GAME);
                                        }
                                    }

                                    break;
                                }
                            case eSTATE_GRAND_ZODIAC_SYNC.LOAD_HOLE:
                                {
                                    break; // Faz nada por enquanto
                                }
                            case eSTATE_GRAND_ZODIAC_SYNC.LOAD_CHAR_INTRO:
                                {
                                    break; // Faz nada por enquanto
                                }
                            case eSTATE_GRAND_ZODIAC_SYNC.END_SHOT:
                                {
                                    break; // Faz nada por enquanto
                                }
                            case eSTATE_GRAND_ZODIAC_SYNC.WAIT_END_GAME:
                                {

                                    if (checkAllEndGame())
                                    { 
                                        clearEndGame();
                                    }

                                    break;
                                }
                        }

                        // Libera
                        m_state_gz.unlock();

                    }
                    catch (exception e)
                    {

                        m_state_gz.unlock();

                        _smp.message_pool.getInstance().push(new message("[GrandZodiacBase::syncFirstHole][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                    }
                }

                //para o tempo
                datetime.Stop();
                ts = datetime.Elapsed;
                elapsedTime = String.Format("{0:00}:{1:00}:{2:00}", ts.Hours, ts.Minutes, ts.Seconds);

                _smp.message_pool.getInstance().push(new message($"[GrandZodiacBase::syncFirstHole][Log] Partida Finalizada. Tempo total: {elapsedTime}", type_msg.CL_FILE_LOG_AND_CONSOLE));

            }
            catch (exception e)
            {
                _smp.message_pool.getInstance().push(new message("[GrandZodiacBase::syncFirstHole][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
            return null;
        }


        public abstract void startGoldenBeam();
        public abstract void endGoldenBeam();
        public new PlayerGrandZodiacInfo INIT_PLAYER_INFO(string _method, string _msg, Player __session)
        {
            var pgi = getPlayerInfo((__session));
            if (pgi == null)
                throw new exception($"[{GetType().Name}::" + _method + "][Error] PLAYER[UID=" + __session.m_pi.uid + "] " + _msg + ", mas o game nao tem o info dele guardado. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME, 1, 4));

            return (PlayerGrandZodiacInfo)pgi;
        }

        public override void Dispose(bool disposing)
        {

            if (disposedValue) return;

            if (disposing)
            {
                Interlocked.Exchange(ref m_golden_beam_state, 0);

                if (!m_mp_golden_beam_player.empty())
                    m_mp_golden_beam_player.Clear();

                if (!m_initial_values_seed.empty())
                    m_initial_values_seed.Clear();

                // Termina a thread sync first hole
                finish_thread_sync_first_hole();
                deleteAllPlayer();
            }
            base.Dispose(true);
        }

        ~GrandZodiacBase()
        {
            Dispose(false);
        }
    }
}
