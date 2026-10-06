using Pangya_GameServer.Engine;
using Pangya_GameServer.Manager;
using Pangya_GameServer.Models;
using Pangya_GameServer.PacketFunc;
using Pangya_GameServer.Repository;
using Pangya_GameServer.Session;
using PangyaAPI.DataBase;
using PangyaAPI.IFF.Handle.JP;
using PangyaAPI.Network;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using PangyaAPI.Utilities.Models;
using snmdb;
using static Pangya_GameServer.Models.DefineConstants;

namespace Pangya_GameServer.Feature
{
    public class AttendanceRewardSystem
    {
        // Campos privados
        private List<AttendanceRewardItemCtx> v_item;
        private bool m_load;
        protected readonly object m_cs = new();
        // Construtor
        public AttendanceRewardSystem()
        {
            v_item = new List<AttendanceRewardItemCtx>();
            m_load = false;
        }
         
        ~AttendanceRewardSystem()
        { 
            clear();
        }
         
        public void load()
        {
            if (isLoad())
                clear();

            initialize();
        } 

        public bool isLoad()
        {
            return m_load && v_item.Any();
        }
         
        public async void requestCheckAttendance(Player _session, Packet _packet)
        {
            try
            {
                if (passedOneDay(_session))
                {
                    _session.UserInfo.Attendance.login = 0;
                    _session.UserInfo.Attendance.now = _session.UserInfo.Attendance.after;
                    _session.UserInfo.Attendance.after.clear();

                   snmdb.NormalManagerDB.Instance.add(1, new CmdUpdateAttendanceReward(_session.UserInfo.UID, _session.UserInfo.Attendance), SQLDBResponse, null);
                }
                else
                {
                    _session.UserInfo.Attendance.login = 1;
                }

                _session.Send(Handle_PACKET_RESPONSE.pacote248(_session.UserInfo.Attendance));
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[AttendanceRewardSystem::checkAttendance][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                using (var p = new Packet())
                {
                    p.init_plain(0x248);
                    uint errorCode = (ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) == (uint)STDA_ERROR_TYPE.ATTENDANCE_REWARD_SYSTEM)
                                     ? ExceptionError.STDA_SYSTEM_ERROR_DECODE(e.getCodeError())
                                     : 0xFFFFFFFF;
                    p.WriteUInt32(errorCode);
                    _session.Send(p);
                }
            }

        }

        public async void requestUpdateCountLogin(Player _session, Packet _packet)
        {
            try
            {
                if (_session.UserInfo.Attendance.login == 1)
                    throw new exception($"[AttendanceRewardSystem::requestUpdateCountLogin][Error] UID={_session.UserInfo.UID} já pegou prêmio.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ATTENDANCE_REWARD_SYSTEM, 8, 0));

                if (!passedOneDay(_session))
                    throw new exception($"[AttendanceRewardSystem::requestUpdateCountLogin][Error] UID={_session.UserInfo.UID} tempo insuficiente.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ATTENDANCE_REWARD_SYSTEM, 8, 1));

                if (_session.UserInfo.Attendance.counter++ == 0 || _session.UserInfo.Attendance.now._typeid == 0)
                {
                    var reward = drawReward(1);
                    _session.UserInfo.Attendance.now._typeid = reward._typeid;
                    _session.UserInfo.Attendance.now.qntd = reward.qntd;
                }

                // Atualiza data de login (Zera horas)
                _session.UserInfo.Attendance.last_login = new SystemTime(DateTime.Now.Date);
                _session.UserInfo.Attendance.login = 1;

                // Envia Item via Mail
                stItem item = new stItem { type = 2, id = -1, _typeid = _session.UserInfo.Attendance.now._typeid, qntd = (int)_session.UserInfo.Attendance.now.qntd };
                item.STDA_C_ITEM_QNTD = (short)item.qntd;

                MailManager.SendMessageWithItem(0, _session.UserInfo.UID, "Your Attendance rewards have arrived", item);

                // Sorteia próximo (Tipo 2 se for múltiplo de 10)
                byte nextType = (byte)(((_session.UserInfo.Attendance.counter + 1) % 10 == 0) ? 2 : 1);
                var nextReward = drawReward(nextType);
                _session.UserInfo.Attendance.after._typeid = nextReward._typeid;
                _session.UserInfo.Attendance.after.qntd = nextReward.qntd;

               snmdb.NormalManagerDB.Instance.add(1, new CmdUpdateAttendanceReward(_session.UserInfo.UID, _session.UserInfo.Attendance));

                sendGrandPrixTicket(_session);

                // Achievements
                AchievementSystem sys_achieve = new AchievementSystem();
                sys_achieve.incrementCounter(0x6C4000A0);

                _session.Send(Handle_PACKET_RESPONSE.pacote249(_session.UserInfo.Attendance));

                sys_achieve.finish_and_update(_session);
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[AttendanceRewardSystem::requestUpdateCountLogin][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                using (var p = new Packet())
                {
                    p.init_plain(0x249);
                    uint errorCode = (ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) == (uint)STDA_ERROR_TYPE.ATTENDANCE_REWARD_SYSTEM)
                                     ? ExceptionError.STDA_SYSTEM_ERROR_DECODE(e.getCodeError())
                                     : 0xFFFFFFFF;
                    p.WriteUInt32(errorCode);
                    _session.Send(p);
                }
            }
        }
        // Métodos protegidos

        // Inicializa o sistema
        private void initialize()
        {

            // Carrega os Itens do Attendance Reward
            var cmd_aric = new CmdAttendanceRewardItemInfo(); // Waiter

            NormalManagerDB.Instance.add(0, cmd_aric, null, null);

            if (cmd_aric.getException().getCodeError() != 0)
                throw cmd_aric.getException();

            v_item = cmd_aric.getInfo();

            if (v_item.Count == 0)
                _smp.LogManager.Instance.push(new AppMessage("[AttendanceRewardSystem::initialize][Warning] Not Loaded!", type_msg.CL_FILE_LOG_AND_CONSOLE));

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

                var pWi = _session.Inventory.FindWarehouseItemByTypeid(GRAND_PRIX_TICKET);

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
                    var rt = RetAddItem.ERROR;

                    if ((rt = ItemManager.addItem(item, _session, 0, 0)) < 0/*Error*/)
                        throw new exception("[AttendanceRewardSystem::sendGrandPrixTicket][Error] Normal[UID=" + (_session.UserInfo.UID)
                            + "] tentou adicionar o Grand Prix Ticket do login, mas nao conseguiu. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ATTENDANCE_REWARD_SYSTEM, 9, 0));


                    // UPDATE ON GAME, s� envia se for diferente de Pang and Exp Pouch
                    if (rt != RetAddItem.SUCCESS_PANG_AND_EXP_AND_CP_POUCH)
                    {

                        var msg = "Your Attendance rewards have arrived!"; //envia no email, na proxima vez que ele logar, ele já ver o item

                        MailManager.SendMessageWithItem(0, _session.UserInfo.UID, msg, item);
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

                var pWi = _session.Inventory.FindWarehouseItemByTypeid(436207927);

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
                    var rt = RetAddItem.ERROR;

                    if ((rt = ItemManager.addItem(item, _session, 0, 0)) < 0/*Error*/)
                        throw new exception("[AttendanceRewardSystem::sendBotTicket][Error] Normal[UID=" + (_session.UserInfo.UID)
                            + "] tentou adicionar o Key of fortune do login, mas nao conseguiu. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ATTENDANCE_REWARD_SYSTEM, 9, 0));


                    // UPDATE ON GAME, s� envia se for diferente de Pang and Exp Pouch
                    if (rt != RetAddItem.SUCCESS_PANG_AND_EXP_AND_CP_POUCH)
                    {

                        var msg = "Special Daily Login Prize!"; //envia no email, na proxima vez que ele logar, ele já ver o item

                        MailManager.SendMessageWithItem(0, _session.UserInfo.UID, msg, item);
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

                var pWi = _session.Inventory.FindWarehouseItemByTypeid(436207964);

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
                    var rt = RetAddItem.ERROR;

                    if ((rt = ItemManager.addItem(item, _session, 0, 0)) < 0/*Error*/)
                        throw new exception("[AttendanceRewardSystem::sendFortuneKey][Error] Normal[UID=" + (_session.UserInfo.UID)
                            + "] tentou adicionar o Key of fortune do login, mas nao conseguiu. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ATTENDANCE_REWARD_SYSTEM, 9, 0));


                    // UPDATE ON GAME, s� envia se for diferente de Pang and Exp Pouch
                    if (rt != RetAddItem.SUCCESS_PANG_AND_EXP_AND_CP_POUCH)
                    {

                        var msg = "Special Daily Login Prize!"; //envia no email, na proxima vez que ele logar, ele já ver o item

                        MailManager.SendMessageWithItem(0, _session.UserInfo.UID, msg, item);
                    }
                }
            }
            catch (Exception)
            {

                throw;
            }
        }
 
        public AttendanceRewardItemCtx drawReward(byte _tipo)
        {
            try
            {
                if (!isLoad())
                    throw new exception("[AttendanceRewardSystem::drawReward][Error] Attendance Reward not load, please call load method first.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ATTENDANCE_REWARD_SYSTEM, 4, 0));

                AttendanceRewardItemCtx aric = null;


                var lottery = new LotterySystem();

                if (v_item.Any(el => el.tipo == _tipo))
                {
                    var collection = v_item.Where(el => el.tipo == _tipo).ToList();
                    foreach (var item in collection)
                        lottery.Add(400, item);
                }
                else
                {
                    var collection = v_item.ToList();//nao tem o de cima, vou pegar do Type '0'
                    foreach (var item in collection)
                        lottery.Add(400, item);
                }
                var lc = lottery.SpinRoleta();

                if (lc == null)
                    throw new exception("[AttendanceRewardSystem::drawReward][Error] nao conseguiu rodar a roleta. falhou ao sortear o item. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.ATTENDANCE_REWARD_SYSTEM, 5, 0));

                aric = (AttendanceRewardItemCtx)lc.Value;

                return aric;
            }
            catch (Exception)
            { 
                throw;
            }
        }

        // Verifica se passou um dia após o Player ter logado no Pangya
        public bool passedOneDay(Player _session)
        {
            try
            { 
                var st = DateTime.Now.Date; // Isso zera hora, minuto, segundo, milissegundo
                 
                DateTime lastLogin = _session.UserInfo.Attendance.last_login.ConvertTime().Date;
                 
                var diff = UtilTime.GetTimeDiff(st, lastLogin);
                // Passou um dia, depois que o player logou no PangYa 
                return (diff / STDA_10_MICRO_PER_DAY) >= 1;
            }
            catch (Exception)
            { 
                throw;
            }
        } 

        protected static void SQLDBResponse(int _msg_id, Pangya_DB _pangya_db, object _arg)
        {
            if (_arg == null)
            {
                return;
            }

            // Por Hora s� sai, depois fa�o outro Type de tratamento se precisar
            if (_pangya_db.getException().getCodeError() != 0)
            {
                _smp.LogManager.Instance.push(new AppMessage("[AttendanceRewardSystem::SQLDBResponse][Error] " + _pangya_db.getException().getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                return;
            }

            switch (_msg_id)
            {
                case 1: // Update Attendance Reward Player
                    {
                        var cmd_uar = (CmdUpdateAttendanceReward)(_pangya_db);
                        _smp.LogManager.Instance.push(new AppMessage("[AttendanceRewardSystem::SQLDBResponse][Debug] Normal[UID=" + (cmd_uar.getUID()) + "] Atualizou Attendance Reward com sucesso.", type_msg.CL_FILE_LOG_AND_CONSOLE));

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
