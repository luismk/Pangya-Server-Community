using Pangya_GameServer.Game.GameModes;
using Pangya_GameServer.Models;
using Pangya_GameServer.PacketFunc;
using Pangya_GameServer.UTIL;
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
using System.Linq;
using System.Threading;

namespace Pangya_GameServer.Game.Manager
{ 
    public class RoomGrandPrix : Room
    { 
        public class m_cs_instancia : Singleton<CriticalSectionInstancia>
        {
        }

        public class m_instancias : Singleton<List<RoomGrandPrixInstanciaCtx>>
        {
        }

        SYSTEMTIME m_start;
        SYSTEMTIME m_now;
        public RoomGrandPrix(byte _channel_owner,
            RoomInfoEx _ri,
            GrandPrixData _gp) : base(_channel_owner, _ri)
        {
            this.m_gp = _gp;
            this.m_count_down = null;


            //room logs
            m_room_log.roomId = Guid.Empty;//seta toda vez que inicia sala

            // Grand Prix tempo que falta para come�ar
            m_now = new SYSTEMTIME(DateTime.Now);
            // Coloca a inst�ncia da classe que acabou de criar no vector statico
            push_instancia(this);

            // Verifica se � Grand Prix Normal e cria um temporizador para come�ar a sala
            // Rookie Grand Prix n�o tem tempo para come�ar o player come�a na hora que ele d� play por que � uma inst�ncia
            if (!(sIff.getInstance().getGrandPrixAbaType(m_gp.ID) == GrandPrixData.GP_ABA.ROOKIE && sIff.getInstance().isGrandPrixNormal(m_gp.ID)))
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

                count_down_to_start(diff);
            }
        }
         

        // Checkers
        public override bool isAllReady()
        { 
            return !_haveInvited();
        }

        // Change Item Equiped of player
        public override void requestChangePlayerItemRoom(Player _session, ChangePlayerItemRoom _cpir)
        {
            if (!_session.getState())
            {
                throw new exception("[RoomGrandPrix::" + "ChangePlayerItemRoom" + "][Error] player nao esta connectado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM_GRAND_PRIX,
                    12, 0));
            }

            var p = new PangyaBinaryWriter();

            try
            {

                var gp_condition = sIff.getInstance().findGrandPrixConditionEquip(m_gp.TypeID_Link);

                if (gp_condition != null)
                {

                    var grup_type = sIff.getInstance().getItemGroupIdentify(gp_condition.item_typeid);

                    switch (_cpir.type)
                    {
                        case ChangePlayerItemRoom.TYPE_CHANGE.TC_CADDIE:
                            if (grup_type == IFF_GROUP.CADDIE)
                            {
                                CaddieInfoEx pCi = null;

                                // Caddie
                                if (_cpir.caddie != 0
                                    && (pCi = _session.m_pi.findCaddieById(_cpir.caddie)) != null
                                    && sIff.getInstance().getItemGroupIdentify(pCi._typeid) == IFF_GROUP.CADDIE)
                                {

                                    if (gp_condition.item_typeid != pCi._typeid)
                                    {

                                        // Procura o caddie que o player tem que ter para jogar esse Grand Prix
                                        if ((pCi = _session.m_pi.findCaddieByTypeid(gp_condition.item_typeid)) != null && sIff.getInstance().getItemGroupIdentify(pCi._typeid) == IFF_GROUP.CADDIE)
                                        {

                                            // Atualiza o caddie equipado do player
                                            _cpir.caddie = pCi.id;

                                            _session.m_pi.ei.cad_info = pCi;
                                            _session.m_pi.ue.caddie_id = pCi.id;

                                            // Update IN GAME 
                                            packet_func.room_broadcast(this,
                                                packet_func.pacote04B(_session,
                                                (byte)ChangePlayerItemRoom.TYPE_CHANGE.TC_CADDIE,
                                                0), 1);

                                        }
                                        else
                                        {

                                            // Player n�o tem o caddie necess�rio para jogar esse Grand Prix, kick ele d� sala
                                            _smp.message_pool.getInstance().push(new message("[RoomGrandPrix::requestChangePlayerItemRoom][Warning] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou trocar item[ID=" + Convert.ToString(_cpir.caddie) + "] equipado na sala[NUMERO=" + Convert.ToString(m_ri.numero) + "] Grand Prix, mas a sala tem uma condicao que nao pode trocar o caddie equipada. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                                        }
                                    }
                                }
                            }
                            break;
                        case ChangePlayerItemRoom.TYPE_CHANGE.TC_BALL:
                            if (grup_type == IFF_GROUP.BALL)
                            {
                                WarehouseItemEx pWi = null;

                                if (_cpir.ball != 0
                                    && (pWi = _session.m_pi.findWarehouseItemByTypeid(_cpir.ball)) != null
                                    && sIff.getInstance().getItemGroupIdentify(pWi._typeid) == IFF_GROUP.BALL)
                                {

                                    if (gp_condition.item_typeid != pWi._typeid)
                                    {

                                        // Procura a Ball que o player tem que ter para jogar esse Grand Prix
                                        if ((pWi = _session.m_pi.findWarehouseItemByTypeid(gp_condition.item_typeid)) != null && sIff.getInstance().getItemGroupIdentify(pWi._typeid) == IFF_GROUP.BALL)
                                        {

                                            // Atualiza a bola equipado do player
                                            _cpir.ball = pWi._typeid;

                                            _session.m_pi.ei.comet = pWi;
                                            _session.m_pi.ue.ball_typeid = pWi._typeid;

                                            // Update IN GAME 
                                            packet_func.room_broadcast(this,
                                                packet_func.pacote04B(_session,
                                                (byte)ChangePlayerItemRoom.TYPE_CHANGE.TC_BALL,
                                                0), 1);

                                        }
                                        else
                                        {

                                            // Player n�o tem a bola necess�rio para jogar esse Grand Prix, kick ele d� sala
                                            _smp.message_pool.getInstance().push(new message("[RoomGrandPrix::requestChangePlayerItemRoom][Warning] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou trocar item[TYPEID=" + Convert.ToString(_cpir.ball) + "] equipado na sala[NUMERO=" + Convert.ToString(m_ri.numero) + "] Grand Prix, mas a sala tem uma condicao que nao pode trocar a bola equipada. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                                        }
                                    }
                                }
                            }
                            break;
                        case ChangePlayerItemRoom.TYPE_CHANGE.TC_CLUBSET:
                            if (grup_type == IFF_GROUP.CLUBSET)
                            {
                                WarehouseItemEx pWi = null;

                                // ClubSet
                                if (_cpir.clubset != 0
                                    && (pWi = _session.m_pi.findWarehouseItemById(_cpir.clubset)) != null
                                    && sIff.getInstance().getItemGroupIdentify(pWi._typeid) == IFF_GROUP.CLUBSET)
                                {

                                    if (gp_condition.item_typeid != pWi._typeid)
                                    {

                                        // Procura a ClubSet que o player tem que ter para jogar esse Grand Prix
                                        if ((pWi = _session.m_pi.findWarehouseItemByTypeid(gp_condition.item_typeid)) != null && sIff.getInstance().getItemGroupIdentify(pWi._typeid) == IFF_GROUP.CLUBSET)
                                        {

                                            // Atualiza o clubset equipado do player
                                            _cpir.clubset = pWi.id;

                                            _session.m_pi.ei.clubset = pWi;

                                            // Esse C do WarehouseItem, que pega do DB, n�o � o ja updado inicial da taqueira � o que fica tabela enchant,
                                            // que no original fica no warehouse msm, eu s� confundi quando fiz
                                            _session.m_pi.ei.csi.setValues(pWi.id, pWi._typeid, pWi.c);

                                            ClubSet cs = sIff.getInstance().findClubSet(pWi._typeid);

                                            if (cs != null)
                                            {

                                                // C++ TO C# CONVERTER WARNING: This 'sizeof' ratio was replaced with a direct reference to the array length:
                                                // ORIGINAL LINE: for (auto j = 0; j < (sizeof(_session.m_pi.ei.csi.enchant_c) / sizeof(short)); ++j)
                                                for (var j = 0; j < (_session.m_pi.ei.csi.enchant_c.Length); ++j)
                                                {
                                                    _session.m_pi.ei.csi.enchant_c[j] = (short)(cs.SlotStats.getSlot[j] + pWi.clubset_workshop.c[j]);
                                                }

                                                _session.m_pi.ue.clubset_id = pWi.id;
                                            }

                                            // Update IN GAME 
                                            packet_func.room_broadcast(this,
                                                packet_func.pacote04B(_session,
                                                (byte)ChangePlayerItemRoom.TYPE_CHANGE.TC_CLUBSET,
                                                0), 1);

                                        }
                                        else
                                        {

                                            // Player n�o tem o clubset necess�rio para jogar esse Grand Prix, kick ele d� sala
                                            _smp.message_pool.getInstance().push(new message("[RoomGrandPrix::requestChangePlayerItemRoom][Warning] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou trocar item[ID=" + Convert.ToString(_cpir.clubset) + "] equipado na sala[NUMERO=" + Convert.ToString(m_ri.numero) + "] Grand Prix, mas a sala tem uma condicao que nao pode trocar o ClubSet equipada. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                                        }
                                    }
                                }
                            }
                            break;
                        case ChangePlayerItemRoom.TYPE_CHANGE.TC_CHARACTER:
                            if (grup_type == IFF_GROUP.CHARACTER)
                            {
                                CharacterInfo pCe = null;

                                if (_cpir.character != 0
                                    && (pCe = _session.m_pi.findCharacterById(_cpir.character)) != null
                                    && sIff.getInstance().getItemGroupIdentify(pCe._typeid) == IFF_GROUP.CHARACTER)
                                {

                                    if (gp_condition.item_typeid != pCe._typeid)
                                    {

                                        // Procura o character que o player tem que ter para jogar esse Grand Prix
                                        if ((pCe = _session.m_pi.findCharacterByTypeid(gp_condition.item_typeid)) != null && sIff.getInstance().getItemGroupIdentify(pCe._typeid) == IFF_GROUP.CHARACTER)
                                        {

                                            // Atualiza o character equipado do player
                                            _cpir.character = pCe.id;

                                            _session.m_pi.ei.char_info = pCe;
                                            _session.m_pi.ue.character_id = pCe.id;

                                            // Update IN GAME

                                            packet_func.session_send(packet_func.pacote06B(_session.m_pi, 5, 4),
                                                _session, 1);

                                        }
                                        else
                                        {

                                            // Player n�o tem o character necess�rio para jogar esse Grand Prix, kick ele d� sala
                                            _smp.message_pool.getInstance().push(new message("[RoomGrandPrix::requestChangePlayerItemRoom][Warning] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou trocar item[ID=" + Convert.ToString(_cpir.character) + "] equipado na sala[NUMERO=" + Convert.ToString(m_ri.numero) + "] Grand Prix, mas a sala tem uma condicao que nao pode trocar o character equipada. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                                        }
                                    }
                                }
                            }
                            break;
                        case ChangePlayerItemRoom.TYPE_CHANGE.TC_MASCOT:
                            if (grup_type == IFF_GROUP.MASCOT)
                            {
                                MascotInfoEx pMi = null;

                                if (_cpir.mascot != 0
                                    && (pMi = _session.m_pi.findMascotById(_cpir.mascot)) != null
                                    && sIff.getInstance().getItemGroupIdentify(pMi._typeid) == IFF_GROUP.MASCOT)
                                {

                                    if (gp_condition.item_typeid != pMi._typeid)
                                    {

                                        // Procura o mascot que o player tem que ter para jogar esse Grand Prix
                                        if ((pMi = _session.m_pi.findMascotByTypeid(gp_condition.item_typeid)) != null && sIff.getInstance().getItemGroupIdentify(pMi._typeid) == IFF_GROUP.MASCOT)
                                        {

                                            // Atualiza o mascot equipado do player
                                            _cpir.mascot = pMi.id;

                                            _session.m_pi.ei.mascot_info = pMi;
                                            _session.m_pi.ue.mascot_id = pMi.id;

                                            // Update IN GAME 
                                            packet_func.room_broadcast(this,
                                                packet_func.pacote04B(_session,
                                                (byte)ChangePlayerItemRoom.TYPE_CHANGE.TC_MASCOT,
                                                0), 1);

                                        }
                                        else
                                        {

                                            // Player n�o tem o mascot necess�rio para jogar esse Grand Prix, kick ele d� sala
                                            _smp.message_pool.getInstance().push(new message("[RoomGrandPrix::requestChangePlayerItemRoom][Warning] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou trocar item[ID=" + Convert.ToString(_cpir.mascot) + "] equipado na sala[NUMERO=" + Convert.ToString(m_ri.numero) + "] Grand Prix, mas a sala tem uma condicao que nao pode trocar o mascot equipada. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                                        }
                                    }
                                }
                            }
                            break;
                        case ChangePlayerItemRoom.TYPE_CHANGE.TC_ITEM_EFFECT_LOUNGE:
                            if (grup_type == IFF_GROUP.PART)
                            {
                                // Esse n�o usa por que � s� no lounge que troca esse item ou ativa o efeito
                            }
                            break;
                        case ChangePlayerItemRoom.TYPE_CHANGE.TC_ALL:
                            {
                                if (grup_type == IFF_GROUP.CHARACTER)
                                {
                                    CharacterInfo pCe = null;

                                    if (_cpir.character != 0
                                        && (pCe = _session.m_pi.findCharacterById(_cpir.character)) != null
                                        && sIff.getInstance().getItemGroupIdentify(pCe._typeid) == IFF_GROUP.CHARACTER)
                                    {

                                        if (gp_condition.item_typeid != pCe._typeid)
                                        {

                                            // Procura o character que o player tem que ter para jogar esse Grand Prix
                                            if ((pCe = _session.m_pi.findCharacterByTypeid(gp_condition.item_typeid)) != null && sIff.getInstance().getItemGroupIdentify(pCe._typeid) == IFF_GROUP.CHARACTER)
                                            {

                                                // Atualiza o character equipado do player
                                                _cpir.character = pCe.id;

                                                _session.m_pi.ei.char_info = pCe;
                                                _session.m_pi.ue.character_id = pCe.id;

                                                // Update IN GAME

                                                packet_func.session_send(packet_func.pacote06B(_session.m_pi, 5, 4),
                                                    _session, 1);

                                            }
                                            else
                                            {

                                                // Player n�o tem o character necess�rio para jogar esse Grand Prix, kick ele d� sala
                                                _smp.message_pool.getInstance().push(new message("[RoomGrandPrix::requestChangePlayerItemRoom][Warning] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou trocar item[ID=" + Convert.ToString(_cpir.character) + "] equipado na sala[NUMERO=" + Convert.ToString(m_ri.numero) + "] Grand Prix, mas a sala tem uma condicao que nao pode trocar o character equipada. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                                            }
                                        }
                                    }

                                }
                                else if (grup_type == IFF_GROUP.CADDIE)
                                {
                                    CaddieInfoEx pCi = null;

                                    // Caddie
                                    if (_cpir.caddie != 0
                                        && (pCi = _session.m_pi.findCaddieById(_cpir.caddie)) != null
                                        && sIff.getInstance().getItemGroupIdentify(pCi._typeid) == IFF_GROUP.CADDIE)
                                    {

                                        if (gp_condition.item_typeid != pCi._typeid)
                                        {

                                            // Procura o caddie que o player tem que ter para jogar esse Grand Prix
                                            if ((pCi = _session.m_pi.findCaddieByTypeid(gp_condition.item_typeid)) != null && sIff.getInstance().getItemGroupIdentify(pCi._typeid) == IFF_GROUP.CADDIE)
                                            {

                                                // Atualiza o caddie equipado do player
                                                _cpir.caddie = pCi.id;

                                                _session.m_pi.ei.cad_info = pCi;
                                                _session.m_pi.ue.caddie_id = pCi.id;

                                                // Update IN GAME

                                                packet_func.room_broadcast(this,
                                                     packet_func.pacote04B(_session,
                                                    (byte)ChangePlayerItemRoom.TYPE_CHANGE.TC_CADDIE,
                                                    0), 1);

                                            }
                                            else
                                            {

                                                // Player n�o tem o caddie necess�rio para jogar esse Grand Prix, kick ele d� sala
                                                _smp.message_pool.getInstance().push(new message("[RoomGrandPrix::requestChangePlayerItemRoom][Warning] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou trocar item[ID=" + Convert.ToString(_cpir.caddie) + "] equipado na sala[NUMERO=" + Convert.ToString(m_ri.numero) + "] Grand Prix, mas a sala tem uma condicao que nao pode trocar o caddie equipada. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                                            }
                                        }
                                    }

                                }
                                else if (grup_type == IFF_GROUP.CLUBSET)
                                {
                                    WarehouseItemEx pWi = null;

                                    // ClubSet
                                    if (_cpir.clubset != 0
                                        && (pWi = _session.m_pi.findWarehouseItemById(_cpir.clubset)) != null
                                        && sIff.getInstance().getItemGroupIdentify(pWi._typeid) == IFF_GROUP.CLUBSET)
                                    {

                                        if (gp_condition.item_typeid != pWi._typeid)
                                        {

                                            // Procura a ClubSet que o player tem que ter para jogar esse Grand Prix
                                            if ((pWi = _session.m_pi.findWarehouseItemByTypeid(gp_condition.item_typeid)) != null && sIff.getInstance().getItemGroupIdentify(pWi._typeid) == IFF_GROUP.CLUBSET)
                                            {

                                                // Atualiza o clubset equipado do player
                                                _cpir.clubset = pWi.id;

                                                _session.m_pi.ei.clubset = pWi;

                                                // Esse C do WarehouseItem, que pega do DB, n�o � o ja updado inicial da taqueira � o que fica tabela enchant,
                                                // que no original fica no warehouse msm, eu s� confundi quando fiz
                                                _session.m_pi.ei.csi.setValues(pWi.id, pWi._typeid, pWi.c);

                                                ClubSet cs = sIff.getInstance().findClubSet(pWi._typeid);

                                                if (cs != null)
                                                {
                                                    for (var j = 0; j < (_session.m_pi.ei.csi.enchant_c.Length); ++j)
                                                    {
                                                        _session.m_pi.ei.csi.enchant_c[j] = (short)(cs.SlotStats.getSlot[j] + pWi.clubset_workshop.c[j]);
                                                    }

                                                    _session.m_pi.ue.clubset_id = pWi.id;
                                                }

                                                // Update IN GAME 
                                                packet_func.room_broadcast(this,
                                                    packet_func.pacote04B(_session,
                                                    (byte)ChangePlayerItemRoom.TYPE_CHANGE.TC_CLUBSET,
                                                    0), 1);

                                            }
                                            else
                                            {

                                                // Player n�o tem o clubset necess�rio para jogar esse Grand Prix, kick ele d� sala
                                                _smp.message_pool.getInstance().push(new message("[RoomGrandPrix::requestChangePlayerItemRoom][Warning] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou trocar item[ID=" + Convert.ToString(_cpir.clubset) + "] equipado na sala[NUMERO=" + Convert.ToString(m_ri.numero) + "] Grand Prix, mas a sala tem uma condicao que nao pode trocar o ClubSet equipada. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                                            }
                                        }
                                    }

                                }
                                else if (grup_type == IFF_GROUP.BALL)
                                {
                                    WarehouseItemEx pWi = null;

                                    if (_cpir.ball != 0
                                        && (pWi = _session.m_pi.findWarehouseItemByTypeid(_cpir.ball)) != null
                                        && sIff.getInstance().getItemGroupIdentify(pWi._typeid) == IFF_GROUP.BALL)
                                    {

                                        if (gp_condition.item_typeid != pWi._typeid)
                                        {

                                            // Procura a Ball que o player tem que ter para jogar esse Grand Prix
                                            if ((pWi = _session.m_pi.findWarehouseItemByTypeid(gp_condition.item_typeid)) != null && sIff.getInstance().getItemGroupIdentify(pWi._typeid) == IFF_GROUP.BALL)
                                            {

                                                // Atualiza a bola equipado do player
                                                _cpir.ball = pWi._typeid;

                                                _session.m_pi.ei.comet = pWi;
                                                _session.m_pi.ue.ball_typeid = pWi._typeid;

                                                // Update IN GAME 
                                                packet_func.room_broadcast(this,
                                                     packet_func.pacote04B(_session,
                                                    (byte)ChangePlayerItemRoom.TYPE_CHANGE.TC_BALL,
                                                    0), 1);

                                            }
                                            else
                                            {

                                                // Player n�o tem a bola necess�rio para jogar esse Grand Prix, kick ele d� sala
                                                _smp.message_pool.getInstance().push(new message("[RoomGrandPrix::requestChangePlayerItemRoom][Warning] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou trocar item[TYPEID=" + Convert.ToString(_cpir.ball) + "] equipado na sala[NUMERO=" + Convert.ToString(m_ri.numero) + "] Grand Prix, mas a sala tem uma condicao que nao pode trocar a bola equipada. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                                            }
                                        }
                                    }

                                }
                                break;
                            }
                    }

                } // fim do iff que verifica se o gp_condition � v�lido

                // Chama o changePlayerItemRoom d� sala padr�o para fazer as altera��es
                base.requestChangePlayerItemRoom(_session, _cpir);

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[RoomGrandPrix::requestChangePlayerItemRoom][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                packet_func.session_send(packet_func.pacote04B(_session, (byte)_cpir.type,
                    (int)(ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) == (uint)STDA_ERROR_TYPE.ROOM ? ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) : 1)),
                    _session, 0);
            }
        }

        // Game, esse aqui é só para o Grand Prix ROOKIE(TUTO) 
        public override bool requestStartGame(Player _session, packet _packet)
        {
            if (!_session.getState())
            {
                throw new exception("[RoomGrandPrix::" + (("request" + "StartGame")) + "][Error] player nao esta connectado", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM_GRAND_PRIX,
                    12, 0));
            }
            if (_packet == null)
            {
                throw new exception("[RoomGrandPrix::request" + "StartGame" + "][Error] _packet is nullptr", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM_GRAND_PRIX,
                    12, 0));
            }

            var p = new PangyaBinaryWriter();

            bool ret = true;

            try
            {

                if (sIff.getInstance().getGrandPrixAbaType(m_gp.ID) == GrandPrixData.GP_ABA.ROOKIE && !sIff.getInstance().isGrandPrixNormal(m_gp.ID))
                {
                    throw new exception("[RoomGrandPrix::requestStartGame][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou comecar o jogo na sala[NUMERO=" + Convert.ToString(m_ri.numero) + "], mas a sala nao é uma Grand Prix Rookie(Tuto). Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM_GRAND_PRIX,
                        1, 0x5900201));
                }

                // Diferente de Grand Prix ROOKIE(TUTO), manda para o requestStartGame da class room, para tratar esse requisi��o
                if (sIff.getInstance().getGrandPrixAbaType(m_gp.ID) != GrandPrixData.GP_ABA.ROOKIE)
                {
                    ret = base.requestStartGame(_session, _packet);
                }
                else
                {

                    // Verifica se j� tem um jogo inicializado e lan�a error se tiver, para o cliente receber uma resposta
                    if (m_pGame != null)
                    {
                        throw new exception("[RoomGrandPrix::requestStartGame][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou comecar o jogo na sala[NUMERO=" + Convert.ToString(m_ri.numero) + "], mas ja tem um jogo inicializado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM_GRAND_PRIX,
                            7, 0x5900202));
                    }

                    // Verifica se todos est�o prontos se n�o da erro
                    if (!isAllReady())
                    {
                        throw new exception("[RoomGrandPrix::requestStartGame][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou comecar o jogo na sala[NUMERO=" + Convert.ToString(m_ri.numero) + ", MASTER=" + Convert.ToString(m_ri.master) + "], mas nem todos jogadores estao prontos. Hacker ou Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM_GRAND_PRIX,
                            8, 0x5900202));
                    }

                    // random course if random course
                    if (m_ri.getMap() >= 0x7Fu)
                    {

                        // Special Shuffle Course
                        if (m_ri.getTipo() == RoomInfo.ROOM_INFO_TYPE.SPECIAL_SHUFFLE_COURSE && m_ri.getModo() == RoomInfo.ROOM_INFO_MODO.M_SHUFFLE_COURSE)
                        {

                            m_ri.course = (RoomInfo.ROOM_INFO_COURSE)(0x80 | 17);

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
                                m_ri.course = (RoomInfo.ROOM_INFO_COURSE)(0x80u | Convert.ToByte(lc.Value));
                            }
                        }
                    }

                    RateValue rv = new RateValue();

                    // Att Exp rate, e Pang rate, que come�ou o jogo
                    rv.exp = m_ri.rate_exp = (uint)sgs.gs.getInstance().getInfo().rate.exp;
                    rv.pang = m_ri.rate_pang = (uint)sgs.gs.getInstance().getInfo().rate.pang;

                    // Angel Event
                    m_ri.angel_event = sgs.gs.getInstance().getInfo().rate.angel_event > 1 ? true : false;

                    rv.clubset = (uint)sgs.gs.getInstance().getInfo().rate.club_mastery;
                    rv.rain = (uint)sgs.gs.getInstance().getInfo().rate.chuva;
                    rv.treasure = (uint)sgs.gs.getInstance().getInfo().rate.treasure;

                    rv.persist_rain = 0; // Persist rain type isso � feito na classe game

                    switch (m_ri.getTipo())
                    {
                        case RoomInfo.ROOM_INFO_TYPE.GRAND_PRIX:
                            m_pGame = new GrandPrix(v_sessions,
                                m_ri, rv, m_ri.channel_rookie,
                                m_gp);
                            break;
                        default:
                            throw new exception("[RoomGrandPrix::requestStartGame][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou comecar o jogo na sala[NUMERO=" + Convert.ToString(m_ri.numero) + ", MASTER=" + Convert.ToString(m_ri.master) + "], mas o tipo da sala nao é Grand Prix. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM_GRAND_PRIX,
                                9, 0x5900202));
                    }

                    // Update Room State
                    m_ri.state = 0; // IN GAME

                    // Mandar para ficar igual ao original
                    p.init_plain(0x253);

                    p.WriteInt32(0);

                    packet_func.room_broadcast(this,
                        p, 1);

                    // Update on GAME
                    p.init_plain(0x230);

                    packet_func.room_broadcast(this,
                        p, 1);

                    p.init_plain(0x231);

                    packet_func.room_broadcast(this,
                        p, 1);

                    var rate_pang = sgs.gs.getInstance().getInfo().rate.pang;

                    p.init_plain(0x77);

                    p.WriteUInt32((uint)rate_pang); // Rate Pang

                    packet_func.room_broadcast(this,
                        p, 1);

                    //room logs
                    m_room_log.roomId = Guid.Empty;//seta toda vez que inicia sala

                    //insert dados do player
                    foreach (var _sessions in v_sessions)
                        CreateRoomLogSql(_sessions);//criar de todos

                    _session.m_pGame = m_pGame;

                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[RoomGrandPrix::requestStartGame][Error] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Error
                p.init_plain(0x253);

                p.WriteUInt32(0x5900200
                );

                packet_func.session_send(p,
                    _session, 1);

                ret = false; // Error ao inicializar o Jogo
            }

            return ret;
        }

        // O Grand Prix de tempo tem o seu próprio startGame já que quem começa é o server
        public bool startGame()
        {

            var p = new PangyaBinaryWriter();

            bool ret = true;

            try
            {

                if (sIff.getInstance().getGrandPrixAbaType(m_gp.ID) == GrandPrixData.GP_ABA.ROOKIE && !sIff.getInstance().isGrandPrixNormal(m_gp.ID))
                {
                    throw new exception("[RoomGrandPrix::startGame][Error] Server tentou comecar o jogo na sala[NUMERO=" + Convert.ToString(m_ri.numero) + "], mas a sala nao é uma Grand Prix Rookie(Tuto). Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM_GRAND_PRIX,
                        1, 0x5900201));
                }

                // Verifica se j� tem um jogo inicializado e lan�a error se tiver, para o cliente receber uma resposta
                if (m_pGame != null)
                {
                    throw new exception("[RoomGrandPrix::startGame][Error] Server tentou comecar o jogo na sala[NUMERO=" + Convert.ToString(m_ri.numero) + "], mas ja tem um jogo inicializado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM_GRAND_PRIX,
                        7, 0x5900202));
                }

                // Verifica se todos est�o prontos se n�o da erro
                if (!isAllReady())
                {
                    throw new exception("[RoomGrandPrix::startGame][Error] Server tentou comecar o jogo na sala[NUMERO=" + Convert.ToString(m_ri.numero) + ", MASTER=" + Convert.ToString(m_ri.master) + "], mas nem todos jogadores estao prontos. Hacker ou Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ROOM_GRAND_PRIX,
                        8, 0x5900202));
                }

                // random course if random course
                if (m_ri.getMap() >= 0x7Fu)
                {

                    // Special Shuffle Course
                    if (m_ri.getTipo() == RoomInfo.ROOM_INFO_TYPE.SPECIAL_SHUFFLE_COURSE && m_ri.getModo() == RoomInfo.ROOM_INFO_MODO.M_SHUFFLE_COURSE)
                    {

                        m_ri.course = (RoomInfo.ROOM_INFO_COURSE)(0x80 | 17);

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
                            m_ri.course = (RoomInfo.ROOM_INFO_COURSE)(0x80u | Convert.ToByte(lc.Value));
                        }
                    }
                }


                RateValue rv = new RateValue();

                // Att Exp rate, e Pang rate, que come�ou o jogo
                rv.exp = m_ri.rate_exp = (uint)sgs.gs.getInstance().getInfo().rate.exp;
                rv.pang = m_ri.rate_pang = (uint)sgs.gs.getInstance().getInfo().rate.pang;

                // Angel Event
                m_ri.angel_event = sgs.gs.getInstance().getInfo().rate.angel_event > 1 ? true : false;

                rv.clubset = (uint)sgs.gs.getInstance().getInfo().rate.club_mastery;
                rv.rain = (uint)sgs.gs.getInstance().getInfo().rate.chuva;
                rv.treasure = (uint)sgs.gs.getInstance().getInfo().rate.treasure;

                rv.persist_rain = 0; // Persist rain type isso � feito na classe game

                switch (m_ri.getTipo())
                {
                    case RoomInfo.ROOM_INFO_TYPE.GRAND_PRIX:
                        m_pGame = new GrandPrix(v_sessions,
                            m_ri, rv, m_ri.channel_rookie,
                            m_gp);
                        break;
                    default:
                        break;
                }

                // Update Room State
                m_ri.state = 0; // IN GAME

                // Mandar para ficar igual ao original
                p.init_plain(0x253);

                p.WriteInt32(0);

                packet_func.room_broadcast(this,
                    p, 1);

                // Update on GAME
                p.init_plain(0x230);

                packet_func.room_broadcast(this,
                    p, 1);

                p.init_plain(0x231);

                packet_func.room_broadcast(this,
                    p, 1);

                var rate_pang = sgs.gs.getInstance().getInfo().rate.pang;

                p.init_plain(0x77);

                p.WriteUInt32((uint)rate_pang); // Rate Pang

                packet_func.room_broadcast(this,
                    p, 1);

                m_room_log.roomId = Guid.Empty;//seta toda vez que inicia sala
                //insert dados do player
                foreach (var _sessions in v_sessions)
                {
                    CreateRoomLogSql(_sessions);//criar de todos

                    _sessions.m_pGame = m_pGame;//gera a sala
                }
            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[RoomGrandPrix::startGame][Error] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                ret = false; // Error ao inicializar o Jogo
            }

            return ret;
        }

        // Init Instance vector and lock, para não dá erro no destrutor por que vai destruir ele primeiro do que a instance da classe
        public static void initFirstInstance()
        {

            if (m_cs_instancia.getInstance().m_state && m_instancias.getInstance().empty())
            {
                //  _smp.message_pool.getInstance().push(new message("[RoomGrandPrix::initFirstInstance][Log] Criou primeira instance do Singleton da classe Room Grand Prix static vector.", type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        int _count_down_to_start(object _arg1, object _arg2)
        {


            RoomGrandPrix _rgp = (RoomGrandPrix)(_arg1);

            var sec_to_start = (long)_arg2;

            try
            {

                if (_rgp != null && instancia_valid(_rgp))
                {
                    m_now = new SYSTEMTIME(DateTime.Now);
                    sec_to_start = (!m_gp.Start.IsEmpty ? UtilTime.GetHourDiff(m_start, m_now) : 0L);

                    _rgp.count_down_to_start(sec_to_start);
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[RoomGrandPrix::_count_down_to_start][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            return 0;
        }


        public int count_down_to_start(long _sec_to_start)
        {
            int ret = 0;

            try
            {
                // Se tempo zerou ou negativo, para tudo e começa jogo ou destroi sala
                if (_sec_to_start <= 0)
                {
                    if (m_count_down != null)
                    {
                        sgs.gs.getInstance().unMakeTime(m_count_down);
                    }

                    if (v_sessions.Count() >= 1 && startGame())
                        sgs.gs.getInstance().sendUpdateRoomInfo(this, 3); // Update Room Info
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
                    var p = new PangyaBinaryWriter(0x40);
                    p.WriteByte(11);     // msg
                    p.WriteUInt16(0);    // nick vazio
                    p.WriteUInt16(0);    // msg vazio

                    // Limita _sec_to_start entre 0 e UInt32.MaxValue para evitar erro
                    p.WriteUInt32((uint)_sec_to_start);

                    packet_func.room_broadcast(this, p, 1);

                    // Cria ou reinicia timer caso não exista ou esteja parado/finalizado
                    if (m_count_down == null ||
                        m_count_down.getState() == PangyaSyncTimer.TIMER_STATE.STOP ||
                        m_count_down.getState() == PangyaSyncTimer.TIMER_STATE.FINISH)
                    {
                        if (m_count_down != null)
                            sgs.gs.getInstance().unMakeTime(m_count_down);

                        // Cria o timer com intervalo calculado e a lista de intervalos (pode ajustar aqui)
                        m_count_down = sgs.gs.getInstance().MakeTime(wait, new List<long> { interval }, () =>
                        {
                            _count_down_to_start(this, _sec_to_start);

                        });
                    }
                }
            }
            catch (exception e)
            {
                _smp.message_pool.getInstance().push(new message("[RoomGrandZodiacEvent::count_down_to_start][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            return ret;
        }

        protected List<GrandPrixRankReward> reward = new List<GrandPrixRankReward>();

        protected GrandPrixData m_gp = new GrandPrixData();

        protected PangyaSyncTimer m_count_down;

        // Static funções e variaveis para garantir mexer com o ponteiro das sala Grand Prix que usar o time com um função callback

        // Static Help Check room is valid
        private static void push_instancia(RoomGrandPrix _rgp)
        {

            m_cs_instancia.getInstance().@lock();

            m_instancias.getInstance().Add(new RoomGrandPrixInstanciaCtx(_rgp, RoomGrandPrixInstanciaCtx.eSTATE.GOOD));

            m_cs_instancia.getInstance().unlock();
        }

        private static void pop_instancia(RoomGrandPrix _rgp)
        {

            m_cs_instancia.getInstance().@lock();

            var index = get_instancia_index(_rgp);

            if (index >= 0)
            {
                m_instancias.getInstance().RemoveAt(index);
            }

            m_cs_instancia.getInstance().unlock();
        }

        private static int get_instancia_index(RoomGrandPrix _rgp)
        {

            int index = -1;

            for (var i = 0; i < m_instancias.getInstance().Count(); ++i)
            {

                if (m_instancias.getInstance()[i].m_rgp == _rgp)
                {

                    index = (int)i;

                    break;
                }
            }

            return index;
        }

        private static bool instancia_valid(RoomGrandPrix _rgp)
        {

            bool valid = false;

            m_cs_instancia.getInstance().@lock();

            var index = get_instancia_index(_rgp);

            if (index >= 0)
            {
                valid = (m_instancias.getInstance()[index].m_state == RoomGrandPrixInstanciaCtx.eSTATE.GOOD);
            }

            m_cs_instancia.getInstance().unlock();

            return valid;
        }

    }
}
