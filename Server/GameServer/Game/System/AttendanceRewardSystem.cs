using System;
using System.Collections.Generic;
using System.Linq;
using Pangya_GameServer.Repository;
using Pangya_GameServer.Game.Manager;
using Pangya_GameServer.Models;
using Pangya_GameServer.PacketFunc;
using Pangya_GameServer.UTIL;
using PangyaAPI.IFF.JP.Extensions;
using PangyaAPI.Network.PangyaPacket;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Models;
using PangyaAPI.Utilities.Log;
using static Pangya_GameServer.Models.DefineConstants;

namespace Pangya_GameServer.Game.System
{
    public class AttendanceRewardSystem
    {
        // Campos privados
        private List<AttendanceRewardItemCtx> v_item;
        private bool m_load;

        // Construtor
        public AttendanceRewardSystem()
        {
            v_item = new List<AttendanceRewardItemCtx>();
            m_load = false;
        }

        // Destrutor/Finalizador (caso necessário para liberação de recursos)
        ~AttendanceRewardSystem()
        {
            // Código de limpeza, se necessário.
            clear();
        }

        // Método para carregar o sistema de recompensa
        public void load()
        {
            if (isLoad())
                clear();

            initialize();
        }

        // Retorna se o sistema foi carregado
        public bool isLoad()
        {
            return m_load && v_item.Any();
        }

        // Solicita verificação de presença (attendance)
        public void requestCheckAttendance(Player _session, packet _packet)
        {
            try
            {
                var m_ari = _session.m_pi.ari;
                if (passedOneDay(_session))
                {
                    // Passou 1 dia depois que o player logou no pangya	  	
                    _session.m_pi.ari.login = 0;
                    //faz o proximo sorteio

                    _session.m_pi.ari.counter = _session.m_pi.ari.counter + 1;

                    var reward_item = drawReward((byte)(((_session.m_pi.ari.counter + 1) % 10 == 0) ? 2/*Tipo 2 Papel Box*/ : 1)/*Item Normal*/);

                    if (reward_item == null)
                        throw new exception("[AttendanceRewardSystem::requestCheckAttendance][Error] nao conseguiu sortear um item para o player. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ATTENDANCE_REWARD_SYSTEM, 7, 0));
                    if (sIff.getInstance().IsExist(reward_item._typeid) == false)
                    {
                        //gera o proximo se não existir dados la na db
                        reward_item = sAttendanceRewardSystem.getInstance().drawReward((byte)(((_session.m_pi.ari.counter + 1) % 10 == 0) ? 2/*Tipo 2 Papel Box*/ : 1)/*Item Normal*/);

                        if (reward_item == null)
                            throw new exception("[AttendanceRewardSystem::requestCheckAttendance][Error] nao conseguiu sortear um item para o player. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ATTENDANCE_REWARD_SYSTEM, 7, 0));

                    }
                    _session.m_pi.ari.now._typeid = reward_item._typeid;
                    _session.m_pi.ari.now.qntd = reward_item.qntd;

                    if (sIff.getInstance().IsExist(_session.m_pi.ari.now._typeid) == false)
                    {
                        //gera o proximo se não existir dados la na db
                        reward_item = sAttendanceRewardSystem.getInstance().drawReward((byte)(((_session.m_pi.ari.counter + 1) % 10 == 0) ? 2/*Tipo 2 Papel Box*/ : 1)/*Item Normal*/);

                        if (reward_item == null)
                            throw new exception("[AttendanceRewardSystem::requestCheckAttendance][Error] nao conseguiu sortear um item para o player. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ATTENDANCE_REWARD_SYSTEM, 7, 0));

                        _session.m_pi.ari.now._typeid = reward_item._typeid;
                        _session.m_pi.ari.now.qntd = reward_item.qntd;
                    }

                    _session.m_pi.ari.last_login.CreateTime();
                    // Zera as Horas deixa s� a date
                    _session.m_pi.ari.last_login.MilliSecond = _session.m_pi.ari.last_login.Second = _session.m_pi.ari.last_login.Minute = _session.m_pi.ari.last_login.Hour = 0;

                    stItem item = new stItem
                    {
                        type = 2,
                        id = -1,
                        _typeid = _session.m_pi.ari.now._typeid,
                        qntd = (int)_session.m_pi.ari.now.qntd
                    };
                    item.STDA_C_ITEM_QNTD = (short)item.qntd;
                }
                else
                {
                    _session.m_pi.ari.login = 1;	// Ainda n�o passou 1 dia desde que ele logou no pangya
                    _session.m_pi.ari.last_login.CreateTime();
                    // Zera as Horas deixa s� a date
                    _session.m_pi.ari.last_login.MilliSecond = _session.m_pi.ari.last_login.Second = _session.m_pi.ari.last_login.Minute = _session.m_pi.ari.last_login.Hour = 0;
                }

                packet_func.session_send(packet_func.pacote248(_session.m_pi.ari), _session);

                // Atualiza no banco de dados
                snmdb.NormalManagerDB.getInstance().add(1, new CmdUpdateAttendanceReward(_session.m_pi.uid, _session.m_pi.ari), SQLDBResponse, null);

            }
            catch (exception e)
            {
                _smp.message_pool.getInstance().push(new message("[AttendanceRewardSystem::checkAttendance][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                using (var p = new PangyaBinaryWriter())
                {
                    p.init_plain(0x248);

                    p.WriteUInt32(ExceptionError.STDA_SYSTEM_ERROR_DECODE_TYPE(e.getCodeError()) == (int)STDA_ERROR_TYPE.ATTENDANCE_REWARD_SYSTEM ? ExceptionError.STDA_SYSTEM_ERROR_DECODE_TYPE(e.getCodeError()) : ~0u);

                    packet_func.session_send(p, _session);
                }
                throw e;
            }

        }

        // Solicita atualização do contador de login
        public void requestUpdateCountLogin(Player _session, packet _packet)
        {

            try
            {
                AttendanceRewardItemCtx reward_item;

                _session.m_pi.ari.last_login.CreateTime();

                // Zera as Horas deixa s� a date
                _session.m_pi.ari.last_login.MilliSecond = _session.m_pi.ari.last_login.Second = _session.m_pi.ari.last_login.Minute = _session.m_pi.ari.last_login.Hour = 0;

                // Evento de Login do dia concluido
                _session.m_pi.ari.login = 1;

                // Sortea o Pr�ximo Item que ele vai ganhar
                reward_item = drawReward((byte)(((_session.m_pi.ari.counter + 1) % 10 == 0) ? 2/*Tipo 2 Papel Box*/ : 1)/*Item Normal*/);

                if (reward_item == null)
                    throw new exception("[AttendanceRewardSystem::requestUpdateCountLogin][Error] nao conseguiu sortear um item para o player. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ATTENDANCE_REWARD_SYSTEM, 7, 0));

                _session.m_pi.ari.after._typeid = reward_item._typeid;
                _session.m_pi.ari.after.qntd = reward_item.qntd;

                if (sIff.getInstance().IsExist(_session.m_pi.ari.now._typeid) == false)
                {
                    //gera o proximo se não existir dados la na db
                    reward_item = sAttendanceRewardSystem.getInstance().drawReward((byte)(((_session.m_pi.ari.counter + 1) % 10 == 0) ? 2/*Tipo 2 Papel Box*/ : 1)/*Item Normal*/);

                    if (reward_item == null)
                        throw new exception("[AttendanceRewardSystem::requestCheckAttendance][Error] nao conseguiu sortear um item para o player. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ATTENDANCE_REWARD_SYSTEM, 7, 0));

                    _session.m_pi.ari.now._typeid = reward_item._typeid;
                    _session.m_pi.ari.now.qntd = reward_item.qntd;
                }

                else if (sIff.getInstance().IsExist(_session.m_pi.ari.after._typeid) == false)
                {
                    //gera o proximo se não existir dados la na db
                    reward_item = sAttendanceRewardSystem.getInstance().drawReward((byte)(((_session.m_pi.ari.counter + 1) % 10 == 0) ? 2/*Tipo 2 Papel Box*/ : 1)/*Item Normal*/);

                    if (reward_item == null)
                        throw new exception("[AttendanceRewardSystem::requestCheckAttendance][Error] nao conseguiu sortear um item para o player. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ATTENDANCE_REWARD_SYSTEM, 7, 0));

                    _session.m_pi.ari.after._typeid = reward_item._typeid;
                    _session.m_pi.ari.after.qntd = reward_item.qntd;
                }

                packet_func.session_send(packet_func.pacote249(_session.m_pi.ari), _session);

                //// UPDATE Achievement ON SERVER, DB and GAME
                AchievementSystem sys_achieve = new AchievementSystem();

                sys_achieve.incrementCounter(0x6C4000A0u/*Login Count por dia, 1 por dia*/);

                // UPDATE Achievement ON SERVER, DB and GAME
                sys_achieve.finish_and_update(_session);

                // Atualiza no Banco de dados
                snmdb.NormalManagerDB.getInstance().add(1, new CmdUpdateAttendanceReward(_session.m_pi.uid, _session.m_pi.ari), SQLDBResponse, null);

                // D� 3 Grand Prix Ticket, por que � a primeira vez que o player loga no dia
                sendGrandPrixTicket(_session);

                sendBotTicket(_session);

                sendFortuneKey(_session);
            }
            catch (exception e)
            {
                _smp.message_pool.getInstance().push(new message("[AttendanceRewardSystem::requestUpdateCountLogin][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                using (var p = new PangyaBinaryWriter())
                {
                    p.init_plain(0x249);

                    p.WriteUInt32(ExceptionError.STDA_SYSTEM_ERROR_DECODE_TYPE(e.getCodeError()) == (int)STDA_ERROR_TYPE.ATTENDANCE_REWARD_SYSTEM ? ExceptionError.STDA_SYSTEM_ERROR_DECODE_TYPE(e.getCodeError()) : ~0u);

                    packet_func.session_send(p, _session);
                }
                throw e;
            }
        }

        // Métodos protegidos

        // Inicializa o sistema
        protected void initialize()
        {

            // Carrega os Itens do Attendance Reward
            var cmd_aric = new CmdAttendanceRewardItemInfo(); // Waiter

            snmdb.NormalManagerDB.getInstance().add(0, cmd_aric, null, null);

            if (cmd_aric.getException().getCodeError() != 0)
                throw cmd_aric.getException();

            v_item = cmd_aric.getInfo();

            if (v_item.Count == 0)
                _smp.message_pool.getInstance().push(new message("[AttendanceRewardSystem::initialize][Warning] Not Loaded!", type_msg.CL_FILE_LOG_AND_CONSOLE));

            // Carregou com sucesso
            m_load = true;

        }

        // Limpa os dados do sistema
        protected void clear()
        {
            // Implementação da limpeza dos dados

            if (v_item.Any())
            {
                v_item.Clear();
            }

            m_load = false;
        }

        /// <summary>
        /// Dá 3 Grand Prix Ticket para o Player por ele ter logado a primeira vez no dia,
        /// mas só dá se ele não atingiu o limite de grand prix ticket.
        /// </summary>
        public void sendGrandPrixTicket(Player _session)
        {
            try
            {

                var pWi = _session.m_pi.findWarehouseItemByTypeid(GRAND_PRIX_TICKET);

                // Envia os 3 Grand Prix para o player, ele n�o tem nenhum ticket ou n�o atingiu o limite
                if (pWi == null || pWi.STDA_C_ITEM_QNTD < LIMIT_GRAND_PRIX_TICKET)
                {

                    stItem item = new stItem();

                    item.type = 2;
                    item.id = -1;
                    item._typeid = GRAND_PRIX_TICKET;
                    item.qntd = (int)((pWi == null) ? 3 : ((LIMIT_GRAND_PRIX_TICKET - pWi.STDA_C_ITEM_QNTD) >= 3 ? 3 : LIMIT_GRAND_PRIX_TICKET - pWi.STDA_C_ITEM_QNTD));
                    item.STDA_C_ITEM_QNTD = (short)item.qntd;

                    // UPDATE ON SERVER AND DB
                    var rt = ItemManager.RetAddItem.T_ERROR;

                    if ((rt = ItemManager.addItem(item, _session, 0, 0)) < 0/*Error*/)
                        throw new exception("[AttendanceRewardSystem::sendGrandPrixTicket][Error] PLAYER[UID=" + (_session.m_pi.uid)
                            + "] tentou adicionar o Grand Prix Ticket do login, mas nao conseguiu. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ATTENDANCE_REWARD_SYSTEM, 9, 0));


                    // UPDATE ON GAME, s� envia se for diferente de Pang and Exp Pouch
                    if (rt != ItemManager.RetAddItem.T_SUCCESS_PANG_AND_EXP_AND_CP_POUCH)
                    {

                        var msg = "Your Attendance rewards have arrived!"; //envia no email, na proxima vez que ele logar, ele já ver o item

                        MailBoxManager.sendMessageWithItem(0, _session.m_pi.uid, msg, item);
                    }

                }

            }
            catch (Exception)
            {

                throw;
            }
        }


        public void sendBotTicket(Player _session)
        {
            try
            {

                var pWi = _session.m_pi.findWarehouseItemByTypeid(436207927);

                if (pWi == null || pWi.STDA_C_ITEM_QNTD < 5)
                {

                    stItem item = new stItem
                    {
                        type = 2,
                        id = -1,
                        _typeid = 436207927,
                        qntd = (int)((pWi == null) ? 5 : ((5 - pWi.STDA_C_ITEM_QNTD) >= 5 ? 5 : 5 - pWi.STDA_C_ITEM_QNTD))
                    };
                    item.STDA_C_ITEM_QNTD = (short)item.qntd;

                    // UPDATE ON SERVER AND DB
                    var rt = ItemManager.RetAddItem.T_ERROR;

                    if ((rt = ItemManager.addItem(item, _session, 0, 0)) < 0/*Error*/)
                        throw new exception("[AttendanceRewardSystem::sendBotTicket][Error] PLAYER[UID=" + (_session.m_pi.uid)
                            + "] tentou adicionar o Key of fortune do login, mas nao conseguiu. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ATTENDANCE_REWARD_SYSTEM, 9, 0));


                    // UPDATE ON GAME, s� envia se for diferente de Pang and Exp Pouch
                    if (rt != ItemManager.RetAddItem.T_SUCCESS_PANG_AND_EXP_AND_CP_POUCH)
                    {

                        var msg = "Special Daily Login Prize!"; //envia no email, na proxima vez que ele logar, ele já ver o item

                        MailBoxManager.sendMessageWithItem(0, _session.m_pi.uid, msg, item);
                    }
                }
            }
            catch (Exception)
            {

                throw;
            }
        }

        public void sendFortuneKey(Player _session)
        {
            try
            {

                var pWi = _session.m_pi.findWarehouseItemByTypeid(436207964);

                // Envia os 3 Grand Prix para o player, ele n�o tem nenhum ticket ou n�o atingiu o limite
                if (pWi == null || pWi.STDA_C_ITEM_QNTD < 5)
                {

                    stItem item = new stItem
                    {
                        type = 2,
                        id = -1,
                        _typeid = 436207964,
                        qntd = (int)((pWi == null) ? 5 : ((5 - pWi.STDA_C_ITEM_QNTD) >= 5 ? 5 : 5 - pWi.STDA_C_ITEM_QNTD))
                    };
                    item.STDA_C_ITEM_QNTD = (short)item.qntd;

                    // UPDATE ON SERVER AND DB
                    var rt = ItemManager.RetAddItem.T_ERROR;

                    if ((rt = ItemManager.addItem(item, _session, 0, 0)) < 0/*Error*/)
                        throw new exception("[AttendanceRewardSystem::sendFortuneKey][Error] PLAYER[UID=" + (_session.m_pi.uid)
                            + "] tentou adicionar o Key of fortune do login, mas nao conseguiu. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ATTENDANCE_REWARD_SYSTEM, 9, 0));


                    // UPDATE ON GAME, s� envia se for diferente de Pang and Exp Pouch
                    if (rt != ItemManager.RetAddItem.T_SUCCESS_PANG_AND_EXP_AND_CP_POUCH)
                    {

                        var msg = "Special Daily Login Prize!"; //envia no email, na proxima vez que ele logar, ele já ver o item

                        MailBoxManager.sendMessageWithItem(0, _session.m_pi.uid, msg, item);
                    }
                }
            }
            catch (Exception)
            {

                throw;
            }
        }

        // Retorna um objeto de recompensa com base no tipo informado
        public AttendanceRewardItemCtx drawReward(byte _tipo)
        {
            try
            {
                if (!isLoad())
                    throw new exception("[AttendanceRewardSystem::drawReward][Error] Attendance Reward not load, please call load method first.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ATTENDANCE_REWARD_SYSTEM, 4, 0));

                AttendanceRewardItemCtx aric = null;


                var lottery = new Lottery();

                if (v_item.Any(el => el.tipo == _tipo))
                {
                    var collection = v_item.Where(el => el.tipo == _tipo).ToList();
                    foreach (var item in collection)
                        lottery.Push(400, item);
                }
                else
                {
                    var collection = v_item.ToList();//nao tem o de cima, vou pegar do tipo '0'
                    foreach (var item in collection)
                        lottery.Push(400, item);
                }
                var lc = lottery.spinRoleta();

                if (lc == null)
                    throw new exception("[AttendanceRewardSystem::drawReward][Error] nao conseguiu rodar a roleta. falhou ao sortear o item. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ATTENDANCE_REWARD_SYSTEM, 5, 0));

                aric = (AttendanceRewardItemCtx)lc.Value;

                return aric;
            }
            catch (Exception e)
            {

                throw e;
            }
        }

        // Verifica se passou um dia após o Player ter logado no Pangya
        public bool passedOneDay(Player _session)
        {
            try
            {
                // Define o início do dia atual (0h, 0min, 0seg, 0ms)
                var st = DateTime.Now.Date; // Isso zera hora, minuto, segundo, milissegundo

                // Converte o horário do último login do player (supondo que ConvertTime retorne um DateTime)
                DateTime lastLogin = _session.m_pi.ari.last_login.ConvertTime().Date;

                // Calcula a diferença de tempo (em 100ns unidades)
                var diff = UtilTime.GetTimeDiff(st, lastLogin);
                // Passou um dia, depois que o player logou no PangYa 
                return (diff / STDA_10_MICRO_PER_DAY) >= 1;
            }
            catch (Exception e)
            {

                throw e;
            }
        }

        // Método estático para resposta de banco de dados
        protected static void SQLDBResponse(int _msg_id, Pangya_DB _pangya_db, object _arg)
        {
            if (_arg == null)
            {
                return;
            }

            // Por Hora s� sai, depois fa�o outro tipo de tratamento se precisar
            if (_pangya_db.getException().getCodeError() != 0)
            {
                _smp.message_pool.getInstance().push(new message("[AttendanceRewardSystem::SQLDBResponse][Error] " + _pangya_db.getException().getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                return;
            }

            switch (_msg_id)
            {
                case 1: // Update Attendance Reward Player
                    {
                        var cmd_uar = (CmdUpdateAttendanceReward)(_pangya_db);
                        _smp.message_pool.getInstance().push(new message("[AttendanceRewardSystem::SQLDBResponse][Debug] PLAYER[UID=" + (cmd_uar.getUID()) + "] Atualizou Attendance Reward com sucesso.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                        break;
                    }
                case 0:
                default:
                    break;
            }

        }
    }

    // Implementação do padrão Singleton
    public class sAttendanceRewardSystem : Singleton<AttendanceRewardSystem>
    {
    }
}
