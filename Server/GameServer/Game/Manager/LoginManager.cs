using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Pangya_GameServer.Repository;
using Pangya_GameServer.Game.System;
using Pangya_GameServer.Models;
using Pangya_GameServer.PacketFunc;
using PangyaAPI.IFF.JP.Extensions;
using PangyaAPI.Network.Repository;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using static Pangya_GameServer.Models.PlayerInfo;
using static Pangya_GameServer.Models.DefineConstants;
namespace Pangya_GameServer.Game.Manager
{
    public class LoginManager
    {
        private readonly List<LoginTask> v_task = new List<LoginTask>();

        private Thread m_pThread;

        // Quando true, indica que o thread deve parar
        private volatile bool m_check_task_finish_shutdown;
        public LoginManager()
        {
            m_check_task_finish_shutdown = false;

            StartCheckTaskFinishThread();
        }

        ~LoginManager()
        {
            ShutdownCheckTaskFinishThread();
            clear();
        }

        public LoginTask createTask(Player _session)
        {
            var task = new LoginTask(_session);

            task.exec();

            v_task.Add(task);

            return task;
        }

        public void deleteTask(LoginTask _task)
        {
            if (v_task.Remove(_task))
            {
                _task.Dispose(); // Se LoginTask implementar IDisposable, ou qualquer cleanup necessário
            }
        }

        public static void SQLDBResponse(int _msg_id, Pangya_DB _pangya_db, object _arg)
        {

            if (_arg == null)
            {
                _smp.message_pool.getInstance().push(new message("[LoginSystem.SQLDBResponse][Error] _arg is null na msg_id = " + (_msg_id), type_msg.CL_FILE_LOG_AND_CONSOLE));
                return;
            }
            // if (_arg is LoginTask && (_session = (LoginTask)_arg) != null)

            var task = (LoginTask)_arg;

            try
            {
                // Verifica se a session ainda é valida, essas funções já é thread-safe
                if (task == null || !task.getSession.isConnected())
                {
                    _smp.message_pool.getInstance().push(
                        new message("[LoginManager::SQLDBResponse][Warn] session is invalid, ignorando resposta do pangya_db",
                        type_msg.CL_FILE_LOG_AND_CONSOLE)
                    );
                    return; // sai do método aqui
                }


                // Por Hora só sai, depois faço outro tipo de tratamento se precisar
                if (_pangya_db.getException().getCodeError() != 0)
                    throw new exception(_pangya_db.getException().getFullMessageError());

                switch (_msg_id)
                {
                    case 0: // Info Player
                        {

                            break;
                        }
                    case 1: // Key Login
                        {


                            break;
                        }
                    case 2: // Member Info - User Equip
                        {

                            task.getSession.m_pi.ue = ((CmdUserEquip)_pangya_db).getInfo();

                            // Verifica se tem o Pacote de verificação de bots ativado
                            int ttl = sgs.gs.getInstance().getBotTTL(); //10000÷1.000=10s

                            packet_func.session_send(packet_func.pacote1A9(ttl/*milliseconds*/), task.getSession); // Tempo para enviar um pacote, ant Bot

                            snmdb.NormalManagerDB.getInstance().add(5, new CmdTutorialInfo(task.getSession.m_pi.uid), SQLDBResponse, task);

                            snmdb.NormalManagerDB.getInstance().add(6, new CmdCouponGacha(task.getSession.m_pi.uid), SQLDBResponse, task);

                            snmdb.NormalManagerDB.getInstance().add(7, new CmdUserInfo(task.getSession.m_pi.uid), SQLDBResponse, task);

                            snmdb.NormalManagerDB.getInstance().add(8, new CmdGuildInfo(task.getSession.m_pi.uid, 0), SQLDBResponse, task);

                            snmdb.NormalManagerDB.getInstance().add(9, new CmdDolfiniLockerInfo(task.getSession.m_pi.uid), SQLDBResponse, task);

                            snmdb.NormalManagerDB.getInstance().add(10, new CmdCookie(task.getSession.m_pi.uid), SQLDBResponse, task);

                            snmdb.NormalManagerDB.getInstance().add(11, new CmdTrofelInfo(task.getSession.m_pi.uid, CmdTrofelInfo.TYPE_SEASON.CURRENT), SQLDBResponse, task);

                            // Esses que estavam aqui coloquei no resposta do CmdUserEquip, por que eles precisam da resposta do User Equip

                            snmdb.NormalManagerDB.getInstance().add(16, new CmdMyRoomConfig(task.getSession.m_pi.uid), SQLDBResponse, task);

                            snmdb.NormalManagerDB.getInstance().add(18, new CmdCheckAchievement(task.getSession.m_pi.uid), SQLDBResponse, task);

                            snmdb.NormalManagerDB.getInstance().add(20, new CmdDailyQuestInfoUser(task.getSession.m_pi.uid, CmdDailyQuestInfoUser.TYPE.GET), SQLDBResponse, task);

                            snmdb.NormalManagerDB.getInstance().add(21, new CmdCardInfo(task.getSession.m_pi.uid, CmdCardInfo.TYPE.ALL), SQLDBResponse, task);

                            snmdb.NormalManagerDB.getInstance().add(22, new CmdCardEquipInfo(task.getSession.m_pi.uid), SQLDBResponse, task);

                            snmdb.NormalManagerDB.getInstance().add(23, new CmdTrophySpecial(task.getSession.m_pi.uid, CmdTrophySpecial.TYPE_SEASON.CURRENT, CmdTrophySpecial.TYPE.NORMAL), SQLDBResponse, task);

                            snmdb.NormalManagerDB.getInstance().add(24, new CmdTrophySpecial(task.getSession.m_pi.uid, CmdTrophySpecial.TYPE_SEASON.CURRENT, CmdTrophySpecial.TYPE.GRAND_PRIX), SQLDBResponse, task);

                            break;
                        }
                    case 3: // User Equip - Desativa
                        {
                            break;
                        }
                    case 4: // Premium Ticket
                        {
                            task.getSession.m_pi.pt = ((CmdPremiumTicketInfo)(_pangya_db)).getInfo();

                            ///Att Capability do player
                            ///Verifica se tem premium ticket para mandar o pacote do premium user e a comet
                            if (sPremiumSystem.getInstance().isPremium(task.getSession.m_pi.pt._typeid) && task.getSession.m_pi.pt.id != 0 && task.getSession.m_pi.pt.unix_sec_date > 0)
                            {
                                sPremiumSystem.getInstance().updatePremiumUser(task.getSession);
                            }

                            break;
                        }
                    case 5: // Tutorial Info
                        {

                            task.getSession.m_pi.TutoInfo = ((CmdTutorialInfo)(_pangya_db)).getInfo();
                            // Manda pacote do tutorial aqui
                            packet_func.session_send(packet_func.pacote11F(task.getSession.m_pi, 3/*tutorial info, 3 add do zero init*/), task.getSession);

                            break;
                        }
                    case 6: // Coupon Gacha
                        {
                            task.getSession.m_pi.cg = ((CmdCouponGacha)(_pangya_db)).getCouponGacha();

                            // Não sei se o que é esse pacote, então não sei o que ele busca no banco de dados, mas depois descubro
                            // Deixar ele enviando aqui por enquanto

                            packet_func.session_send(packet_func.pacote101(), task.getSession);// pacote novo do JP

                            break;
                        }
                    case 7: // User Info
                        {

                            task.getSession.m_pi.ui = ((CmdUserInfo)(_pangya_db)).getInfo();    // cmd_ui.getInfo();

                            snmdb.NormalManagerDB.getInstance().add(26, new CmdMapStatistics(task.getSession.m_pi.uid, CmdMapStatistics.TYPE_SEASON.CURRENT, CmdMapStatistics.TYPE.NORMAL, CmdMapStatistics.TYPE_MODO.M_NORMAL), SQLDBResponse, task);

                            snmdb.NormalManagerDB.getInstance().add(27, new CmdMapStatistics(task.getSession.m_pi.uid, CmdMapStatistics.TYPE_SEASON.CURRENT, CmdMapStatistics.TYPE.ASSIST, CmdMapStatistics.TYPE_MODO.M_NORMAL), SQLDBResponse, task);

                            snmdb.NormalManagerDB.getInstance().add(28, new CmdMapStatistics(task.getSession.m_pi.uid, CmdMapStatistics.TYPE_SEASON.CURRENT, CmdMapStatistics.TYPE.NORMAL, CmdMapStatistics.TYPE_MODO.M_NATURAL), SQLDBResponse, task);

                            snmdb.NormalManagerDB.getInstance().add(29, new CmdMapStatistics(task.getSession.m_pi.uid, CmdMapStatistics.TYPE_SEASON.CURRENT, CmdMapStatistics.TYPE.ASSIST, CmdMapStatistics.TYPE_MODO.M_NATURAL), SQLDBResponse, task);

                            snmdb.NormalManagerDB.getInstance().add(30, new CmdMapStatistics(task.getSession.m_pi.uid, CmdMapStatistics.TYPE_SEASON.CURRENT, CmdMapStatistics.TYPE.NORMAL, CmdMapStatistics.TYPE_MODO.M_GRAND_PRIX), SQLDBResponse, task);

                            snmdb.NormalManagerDB.getInstance().add(31, new CmdMapStatistics(task.getSession.m_pi.uid, CmdMapStatistics.TYPE_SEASON.CURRENT, CmdMapStatistics.TYPE.ASSIST, CmdMapStatistics.TYPE_MODO.M_GRAND_PRIX), SQLDBResponse, task);

                            snmdb.NormalManagerDB.getInstance().add(36, new CmdChatMacroUser(task.getSession.m_pi.uid), SQLDBResponse, task);

                            snmdb.NormalManagerDB.getInstance().add(38, new CmdFriendInfo(task.getSession.m_pi.uid), SQLDBResponse, task);

                            break;
                        }
                    case 8: // Guild Info
                        {
                            task.getSession.m_pi.gi = ((CmdGuildInfo)(_pangya_db)).getInfo();   // cmd_gi.getInfo(); 
                            break;
                        }
                    case 9:     // Donfini Locker Info
                        {
                            var df = ((CmdDolfiniLockerInfo)(_pangya_db));

                            task.getSession.m_pi.df = df.getInfo();    
                            break;
                        }
                    case 10:    // Cookie
                        {
                            task.getSession.m_pi.cookie = ((CmdCookie)(_pangya_db)).getCookie();    // cmd_cookie.getCookie();

                            snmdb.NormalManagerDB.getInstance().add(32, new CmdMailBoxInfo2(task.getSession.m_pi.uid), SQLDBResponse, task);

                            snmdb.NormalManagerDB.getInstance().add(33, new CmdCaddieInfo(task.getSession.m_pi.uid, CmdCaddieInfo.TYPE.FERIAS), SQLDBResponse, task);

                            snmdb.NormalManagerDB.getInstance().add(34, new CmdMsgOffInfo(task.getSession.m_pi.uid), SQLDBResponse, task);

                            snmdb.NormalManagerDB.getInstance().add(35, new CmdItemBuffInfo(task.getSession.m_pi.uid), SQLDBResponse, task);

                            snmdb.NormalManagerDB.getInstance().add(37, new CmdLastPlayerGameInfo(task.getSession.m_pi.uid), SQLDBResponse, task);

                            snmdb.NormalManagerDB.getInstance().add(39, new CmdAttendanceRewardInfo(task.getSession.m_pi.uid), SQLDBResponse, task);

                            snmdb.NormalManagerDB.getInstance().add(42, new CmdGrandPrixClear(task.getSession.m_pi.uid), SQLDBResponse, task);

                            snmdb.NormalManagerDB.getInstance().add(43, new CmdGrandZodiacPontos(task.getSession.m_pi.uid, CmdGrandZodiacPontos.eCMD_GRAND_ZODIAC_TYPE.CGZT_GET), SQLDBResponse, task);

                            snmdb.NormalManagerDB.getInstance().add(44, new CmdLegacyTikiShopInfo(task.getSession.m_pi.uid), SQLDBResponse, task);
                            break;
                        }
                    case 11:    // Trofel Info atual
                        {
                            task.getSession.m_pi.ti_current_season = ((CmdTrofelInfo)(_pangya_db)).getInfo();   // cmd_ti.getInfo();

                            snmdb.NormalManagerDB.getInstance().add(12, new CmdCharacterInfo(task.getSession.m_pi.uid, CmdCharacterInfo.TYPE.ALL), SQLDBResponse, task);

                            snmdb.NormalManagerDB.getInstance().add(13, new CmdCaddieInfo(task.getSession.m_pi.uid, CmdCaddieInfo.TYPE.ALL), SQLDBResponse, task);

                            snmdb.NormalManagerDB.getInstance().add(14, new CmdMascotInfo(task.getSession.m_pi.uid, CmdMascotInfo.TYPE.ALL), SQLDBResponse, task);

                            snmdb.NormalManagerDB.getInstance().add(15, new CmdWarehouseItem(task.getSession.m_pi.uid, CmdWarehouseItem.TYPE.ALL), SQLDBResponse, task);

                            break;
                        }
                    case 12:    // Character Info
                        {

                            task.getSession.m_pi.mp_ce = ((CmdCharacterInfo)(_pangya_db)).getAllInfo(); // cmd_ci.getAllInfo();

                            task.getSession.m_pi.ei.char_info = null;

                            // Add Structure de estado do lounge para cada character do player
                            foreach (var el in task.getSession.m_pi.mp_ce)
                            {
                                if (!task.getSession.m_pi.mp_scl.ContainsKey(el.Value.id))
                                    task.getSession.m_pi.mp_scl.Add(el.Value.id, new StateCharacterLounge());
                            }

                            // Att Character Equipado que não tem nenhum character o player
                            if (task.getSession.m_pi.ue.character_id == 0 || task.getSession.m_pi.mp_ce.Count() <= 0)
                                task.getSession.m_pi.ue.character_id = 0;
                            else
                            { // Character Info(CharEquip)

                                // É um Map, então depois usa o find com a Key, que é mais rápido que rodar ele em um loop
                                var it = task.getSession.m_pi.mp_ce.Where(c => c.Key == task.getSession.m_pi.ue.character_id);

                                if (it.Any())
                                    task.getSession.m_pi.ei.char_info = it.First().Value;
                            }

                            // teste Calcula a condição do player e o sexo
                            // Só faz calculo de Quita rate depois que o player
                            // estiver no level Beginner E e jogado 50 games
                            if (task.getSession.m_pi.level >= 6 && task.getSession.m_pi.ui.jogado >= 50)
                            {
                                float rate = task.getSession.m_pi.ui.getQuitRate();

                                if (rate < GOOD_PLAYER_ICON)
                                    task.getSession.m_pi.mi.state_flag.azinha = 1;
                                else if (rate >= QUITER_ICON_1 && rate < QUITER_ICON_2)
                                    task.getSession.m_pi.mi.state_flag.quiter_1 = 1;
                                else if (rate >= QUITER_ICON_2)
                                    task.getSession.m_pi.mi.state_flag.quiter_2 = 1;
                            }

                            if (task.getSession.m_pi.ei.char_info != null && task.getSession.m_pi.ui.getQuitRate() < GOOD_PLAYER_ICON)
                                task.getSession.m_pi.mi.state_flag.icon_angel = task.getSession.m_pi.ei.char_info.AngelEquiped();
                            else
                                task.getSession.m_pi.mi.state_flag.icon_angel = 0;

                            task.getSession.m_pi.mi.state_flag.sexo = task.getSession.m_pi.mi.sexo;

                            break;
                        }
                    case 13:    // Caddie Info
                        {

                            task.getSession.m_pi.mp_ci = ((CmdCaddieInfo)(_pangya_db)).getInfo();   // cmd_cadi.getInfo();

                            // Check Caddie Times
                            PlayerManager.checkCaddie(task.getSession);

                            task.getSession.m_pi.ei.cad_info = null;

                            // Att Caddie Equipado que não tem nenhum caddie o player
                            if (task.getSession.m_pi.ue.caddie_id == 0 || task.getSession.m_pi.mp_ci.Count() <= 0)
                                task.getSession.m_pi.ue.caddie_id = 0;
                            else
                            { // Caddie Info

                                // É um Map, então depois usa o find com a Key, qui é mais rápido que rodar ele em um loop
                                var it = task.getSession.m_pi.mp_ci.Where(c => c.Key == task.getSession.m_pi.ue.caddie_id);

                                if (it.Any())
                                    task.getSession.m_pi.ei.cad_info = it.First().Value;
                            }
                            break;
                        }
                    case 14:    // Mascot Info
                        {
                            task.getSession.m_pi.mp_mi = ((CmdMascotInfo)(_pangya_db)).getInfo(); // cmd_mi.getInfo();

                            // Check Mascot Times
                            PlayerManager.checkMascot(task.getSession);

                            // Att Mascot Equipado que não tem nenhum mascot o player
                            if (task.getSession.m_pi.ue.mascot_id == 0 || task.getSession.m_pi.mp_mi.Count() <= 0)
                                task.getSession.m_pi.ue.mascot_id = 0;
                            else
                            { // Mascot Info

                                // É um Map, então depois usa o find com a Key, qui é mais rápido que rodar ele em um loop
                                var it = task.getSession.m_pi.mp_mi.Where(c => c.Key == task.getSession.m_pi.ue.mascot_id);

                                if (it.Any())
                                    task.getSession.m_pi.ei.mascot_info = it.First().Value;
                            }
                            break;
                        }
                    case 15:    // Warehouse Item
                        {

                            var cmd = ((CmdWarehouseItem)(_pangya_db));
                            task.getSession.m_pi.mp_wi = cmd.getInfo();
                            task.getSession.m_pi.ToTalClubsetCNT = cmd.getClubsetItemCount();
                            task.getSession.m_pi.ToTalPartsCNT = cmd.getPartsItemCount();

                            // Check Warehouse Item Times
                            PlayerManager.checkWarehouse(task.getSession);

                            // Iterator
                            Dictionary<stIdentifyKey, UpdateItem> ui_ticket_report_scroll;

                            //Verifica se tem Ticket Report Scroll no update item para abrir ele e excluir. Todos que estiver, não só 1
                            while ((ui_ticket_report_scroll = task.getSession.m_pi.findUpdateItemByTypeidAndType(TICKET_REPORT_SCROLL_TYPEID, UpdateItem.UI_TYPE.WAREHOUSE)).Count > 0)
                            {

                                try
                                {

                                    var pWi = task.getSession.m_pi.findWarehouseItemById(ui_ticket_report_scroll.FirstOrDefault().Value.id);

                                    if (pWi != null)
                                        ItemManager.openTicketReportScroll(task.getSession, pWi.id, _ticket_scroll_id: ((int)(pWi.c[1] * 0x800) | (int)(ushort)pWi.c[2]));

                                }
                                catch (exception e)
                                {

                                    if (e.getCodeError() == (int)STDA_ERROR_TYPE._ITEM_MANAGER)
                                        throw new exception("[SQLDBResponse][Error] " + e.getFullMessageError(), STDA_ERROR_TYPE.LOGIN_MANAGER);
                                    else
                                        throw;  // Relança
                                }
                            }


                            var it = task.getSession.m_pi.findWarehouseItemById(task.getSession.m_pi.ue.clubset_id);

                            // Att ClubSet Equipado que não tem nenhum clubset o player
                            if (task.getSession.m_pi.ue.clubset_id != 0 && it != null)
                            { // ClubSet Info

                                task.getSession.m_pi.ei.clubset = it;

                                // Esse C do WarehouseItem, que pega do DB, não é o ja updado inicial da taqueira é o que fica tabela enchant, 
                                // que no original fica no warehouse msm, eu só confundi quando fiz
                                // [AJEITEI JA] (tem que ajeitar na hora que coloca no DB e no DB isso)
                                task.getSession.m_pi.ei.csi.setValues(it.id, it._typeid, it.c);

                                var cs = sIff.getInstance().findClubSet(it._typeid);

                                if (cs != null)
                                {
                                    for (var i = 0; i < 5; ++i)
                                    {
                                        task.getSession.m_pi.ei.csi.enchant_c[i] = (short)(cs.SlotStats.getSlot[i] + it.clubset_workshop.c[i]);
                                    }
                                }
                                else
                                    _smp.message_pool.getInstance().push(new message("[SQLDBResponse][Erro] PLAYER[UID=" + (task.getSession.m_pi.uid) + "] tentou inicializar ClubSet[TYPEID="
                                             + (it._typeid) + ", ID=" + (it.id) + "] equipado, mas ClubSet Not exists on IFF_STRUCT do Server. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                            }
                            else
                            {

                                it = task.getSession.m_pi.findWarehouseItemByTypeid(AIR_KNIGHT_SET);

                                if (it != null)
                                {

                                    task.getSession.m_pi.ue.clubset_id = it.id;
                                    task.getSession.m_pi.ei.clubset = it;

                                    //// Esse C do WarehouseItem, que pega do DB, não é o ja updado inicial da taqueira é o que fica tabela enchant, 
                                    //// que no original fica no warehouse msm, eu só confundi quando fiz
                                    //// [AJEITEI JA] (tem que ajeitar na hora que coloca no DB e no DB isso)
                                    task.getSession.m_pi.ei.csi.setValues(it.id, it._typeid, it.c);

                                    var cs = sIff.getInstance().findClubSet(it._typeid);

                                    if (cs != null)
                                    {
                                        for (var i = 0; i < 5; ++i)
                                            task.getSession.m_pi.ei.csi.enchant_c[i] = (short)(cs.SlotStats.getSlot[i] + it.clubset_workshop.c[i]);

                                    }
                                    else
                                        _smp.message_pool.getInstance().push(new message("[SQLDBResponse][Erro] PLAYER[UID=" + (task.getSession.m_pi.uid) + "] tentou inicializar ClubSet[TYPEID="
                                                 + (it._typeid) + ", ID=" + (it.id) + "] equipado, mas ClubSet Not exists on IFF_STRUCT do Server. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));


                                }
                                else
                                {   // Não tem add o ClubSet padrão para ele(CV1)

                                    _smp.message_pool.getInstance().push(new message("[SQLDBResponse][Warning] PLAYER[UID=" + (task.getSession.m_pi.uid)
                                             + "] nao tem o ClubSet[TYPEID=" + (AIR_KNIGHT_SET) + "] padrao.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                                    BuyItem bi = new BuyItem();
                                    stItem item = new stItem();

                                    bi.id = -1;
                                    bi._typeid = AIR_KNIGHT_SET;
                                    bi.qntd = 1;

                                    ItemManager.initItemFromBuyItem(task.getSession.m_pi, @item, bi, false, 0, 0, 1/*Não verifica o Level*/);

                                    if (item._typeid != 0 && (item.id = ItemManager.addItem(item, task.getSession, 2/*Padrão item*/, 0)) != -4
                                        && (it = task.getSession.m_pi.findWarehouseItemById(item.id)) != null)
                                    {

                                        task.getSession.m_pi.ue.clubset_id = it.id;
                                        task.getSession.m_pi.ei.clubset = it;

                                        // Esse C do WarehouseItem, que pega do DB, não é o ja updado inicial da taqueira é o que fica tabela enchant, 
                                        // que no original fica no warehouse msm, eu só confundi quando fiz
                                        // [AJEITEI JA] (tem que ajeitar na hora que coloca no DB e no DB isso)
                                        task.getSession.m_pi.ei.csi.setValues(it.id, it._typeid, it.c);

                                        var cs = sIff.getInstance().findClubSet(it._typeid);

                                        if (cs != null)
                                        {

                                            for (var i = 0; i < 5; ++i)
                                                task.getSession.m_pi.ei.csi.enchant_c[i] = (short)(cs.SlotStats.getSlot[i] + it.clubset_workshop.c[i]);

                                        }
                                        else
                                            _smp.message_pool.getInstance().push(new message("[SQLDBResponse][Erro] PLAYER[UID=" + (task.getSession.m_pi.uid) + "] tentou inicializar ClubSet[TYPEID="
                                                 + (it._typeid) + ", ID=" + (it.id) + "] equipado, mas ClubSet Not exists on IFF_STRUCT do Server. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));


                                    }
                                    else
                                        throw new exception("[SQLDBResponse][Error] PLAYER[UID=" + (task.getSession.m_pi.uid)
                                                + "] nao conseguiu adicionar o ClubSet[TYPEID=" + (AIR_KNIGHT_SET) + "] padrao para ele. Bug");

                                }
                            }

                            // Atualiza Comet(Ball) Equipada
                            var it_ball = task.getSession.m_pi.findWarehouseItemByTypeid(task.getSession.m_pi.ue.ball_typeid);
                            if (task.getSession.m_pi.ue.ball_typeid != 0)
                            {
                                task.getSession.m_pi.ei.comet = it_ball;
                            }
                            else
                            { // Default Ball

                                task.getSession.m_pi.ue.ball_typeid = DEFAULT_COMET_TYPEID;

                                it = task.getSession.m_pi.findWarehouseItemByTypeid(DEFAULT_COMET_TYPEID);

                                if (it != task.getSession.m_pi.mp_wi.LastOrDefault().Value)
                                {
                                    task.getSession.m_pi.ei.comet = it;
                                }
                                else
                                {   // não tem add a bola padrão para ele

                                    _smp.message_pool.getInstance().push(new message("[SQLDBResponse][Warning] PLAYER[UID=" + (task.getSession.m_pi.uid)
                                             + "] nao tem a Comet(Ball)[TYPEID=" + (DEFAULT_COMET_TYPEID) + "] padrao.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                                    BuyItem bi = new BuyItem();
                                    stItem item = new stItem();

                                    bi.id = -1;
                                    bi._typeid = DEFAULT_COMET_TYPEID;
                                    bi.qntd = 1;

                                    ItemManager.initItemFromBuyItem(task.getSession.m_pi, item, bi, false, 0, 0, 1/*Não verifica o Level*/);

                                    if (true)
                                    {

                                        task.getSession.m_pi.ei.comet = it;

                                    }
                                    else
                                    {
                                        throw new exception("[SQLDBResponse][Error] PLAYER[UID=" + (task.getSession.m_pi.uid)
                                                + "] nao conseguiu adicionar a Comet(Ball)[TYPEID=" + (DEFAULT_COMET_TYPEID) + "] padrao para ele. Bug");
                                    }

                                }
                            }

                            if (task.getSession.m_pi.findWarehouseItemByTypeid(ASSIST_ITEM_TYPEID) != null)
                            {
                                var assist = task.getSession.m_pi.findWarehouseItemByTypeid(ASSIST_ITEM_TYPEID);
                                task.getSession.m_pi.Assistent.id = assist.id;
                            }

                            // Premium Ticket Tem que ser chamado depois que o Warehouse Item ja foi carregado
                            snmdb.NormalManagerDB.getInstance().add(4, new CmdPremiumTicketInfo(task.getSession.m_pi.uid), SQLDBResponse, task);

                            break;
                        }
                    case 16:    // Config MyRoom
                        {

                            task.getSession.m_pi.mrc = ((CmdMyRoomConfig)(_pangya_db)).getMyRoomConfig();   // cmd_mrc.getMyRoomConfig();

                            snmdb.NormalManagerDB.getInstance().add(17, new CmdMyRoomItem(task.getSession.m_pi.uid, CmdMyRoomItem.TYPE.ALL), SQLDBResponse, task);
                            break;
                        }
                    case 17:    // MyRoom Item Info
                        {
                            task.getSession.m_pi.v_mri = ((CmdMyRoomItem)(_pangya_db)).getMyRoomItem(); // cmd_mri.getMyRoomItem();

                            break;
                        }
                    case 18:    // Check if have Achievement
                        {
                            //// --------------------- AVISO ----------------------
                            //// esse aqui os outros tem que depender dele para, não ir sem ele
                            var cmd_cAchieve = (CmdCheckAchievement)(_pangya_db);

                            // Cria Achievements do player
                            if (!cmd_cAchieve.getLastState())
                            {
                                // Aqui pode lançar uma excession esse block dentro do if
                                var pi = task.getSession.m_pi;

                                pi.mgr_achievement.initAchievement(task.getSession.m_pi.uid, true/*Create sem verifica se o player tem achievement, por que aqui ele já verificou*/);

                                //    Add o Task + 1 por que não pede o achievement do db, porque criou ele aqui e salvo no DB
                                task.incremenetCount();

                            }
                            else
                            {
                                snmdb.NormalManagerDB.getInstance().add(19, new CmdAchievementInfo(task.getSession.m_pi.uid), SQLDBResponse, task);
                            }

                        }
                        break;
                    case 19:    // Achievement Info
                        {
                            var cmd_ai = ((CmdAchievementInfo)(_pangya_db));

                            // Inicializa o Achievement do player
                            task.getSession.m_pi.mgr_achievement.initAchievement(task.getSession.m_pi.uid, cmd_ai.GetInfo());

                            break;
                        }
                    case 20:    // Daily Quest User Info
                        {
                            var daily = ((CmdDailyQuestInfoUser)(_pangya_db)).GetInfo();
                            task.getSession.m_pi.dqiu = daily;    // cmd_dqiu.getInfo();
                                                                                                            //                                                              // fim daily quest info player

                            break;
                        }
                    case 21:    // Card Info
                        {
                            task.getSession.m_pi.v_card_info = ((CmdCardInfo)(_pangya_db)).getInfo();   // cmd_cardi.getInfo();

                            break;
                        }
                    case 22:    // Card Equipped Info
                        {
                            task.getSession.m_pi.v_cei = ((CmdCardEquipInfo)(_pangya_db)).getInfo();    // cmd_cei.getInfo();

                            // Check Card Special Times
                            PlayerManager.checkCardSpecial(task.getSession);

                            break;
                        }
                    case 23:    // Trofel especial normal atual
                        {
                            task.getSession.m_pi.v_tsi_current_season = ((CmdTrophySpecial)(_pangya_db)).getInfo();

                            break;
                        }
                    case 24:    // Trofel especial grand prix atual
                        {
                            task.getSession.m_pi.v_tgp_current_season = ((CmdTrophySpecial)_pangya_db).getInfo(); // cmd_tei.getInfo();

                            break;
                        }
                    case 26:    // MapStatistics normal, atual
                        {
                            var v_ms = ((CmdMapStatistics)(_pangya_db)).getMapStatistics(); // cmd_ms.getMapStatistics();

                            try
                            {
                                foreach (var i in v_ms)
                                {
                                    task.getSession.m_pi.a_ms_normal[i.course] = i;
                                }

                            }
                            catch (Exception ex)
                            {
                                throw ex;
                            }
                            break;
                        }
                    case 27:    // MapStatistics Normal, assist, atual
                        {
                            var v_ms = ((CmdMapStatistics)(_pangya_db)).getMapStatistics(); // cmd_ms.getMapStatistics();

                            foreach (var i in v_ms)
                            {
                                task.getSession.m_pi.a_msa_normal[i.course] = i;
                            }

                            break;
                        }
                    case 28:    // MapStatistics Natural, atual
                        {
                            var v_ms = ((CmdMapStatistics)(_pangya_db)).getMapStatistics(); // cmd_ms.getMapStatistics();

                            foreach (var i in v_ms)
                            {
                                task.getSession.m_pi.a_ms_natural[i.course] = i;
                            }

                            break;
                        }
                    case 29:    // MapStatistics Natural, assist, atual
                        {

                            var v_ms = ((CmdMapStatistics)(_pangya_db)).getMapStatistics(); // cmd_ms.getMapStatistics();

                            foreach (var i in v_ms)
                            {
                                task.getSession.m_pi.a_msa_natural[i.course] = i;
                            }

                            break;
                        }
                    case 30:    // MapStatistics GrandPrix, atual
                        {
                            var v_ms = ((CmdMapStatistics)(_pangya_db)).getMapStatistics(); // cmd_ms.getMapStatistics();


                            foreach (var i in v_ms)
                            {
                                task.getSession.m_pi.a_ms_grand_prix[i.course] = i;
                            }

                            break;
                        }
                    case 31:    // MapStatistics GrandPrix, Assist, atual
                        {
                            var v_ms = ((CmdMapStatistics)(_pangya_db)).getMapStatistics(); // cmd_ms.getMapStatistics();

                            foreach (var i in v_ms)
                            {
                                task.getSession.m_pi.a_msa_grand_prix[i.course] = i;
                            }
                            break;
                        }
                    case 32:    // [MailBox] New Email(s), Agora é a inicialização do Cache do Mail Box
                        {
                            var cmd_mbi2 = ((CmdMailBoxInfo2)(_pangya_db));

                            task.getSession.m_pi.m_mail_box.init(cmd_mbi2.getInfo(), task.getSession.m_pi.uid);

                            var v_mb = task.getSession.m_pi.m_mail_box.getAllUnreadEmail();

                            packet_func.session_send(packet_func.pacote210(v_mb), task.getSession);
                            break;
                        }
                    case 33:    // Aviso Caddie Ferias
                        {
                            var v_cif = ((CmdCaddieInfo)(_pangya_db)).getInfo();    // cmd_cadi.getInfo();

                            if (v_cif.Any())
                            {

                                packet_func.session_send(packet_func.pacote0D4(v_cif), task.getSession);
                            }
                            break;
                        }
                    case 34:    // Msg Off Info
                        {
                            var v_moi = ((CmdMsgOffInfo)(_pangya_db)).GetInfo();    // cmd_moi.getInfo();

                            if (!v_moi.Any())
                            {

                                packet_func.session_send(packet_func.pacote0B2(v_moi), task.getSession);

                            }

                            break;
                        }
                    case 35:    // YamEquipedInfo ItemBuff(item que da um efeito, por tempo)
                        {
                            task.getSession.m_pi.v_ib = ((CmdItemBuffInfo)(_pangya_db)).GetInfo();  // cmd_yei.getInfo();

                            //// Check Item Buff Times
                            PlayerManager.checkItemBuff(task.getSession);

                            break;
                        }
                    case 36:    // Chat Macro User
                        {
                            task.getSession.m_pi.cmu = ((CmdChatMacroUser)(_pangya_db)).getMacroUser();
                            break;
                        }
                    case 37:    // Last 5 Player Game Info
                        {
                            task.getSession.m_pi.l5pg = ((CmdLastPlayerGameInfo)(_pangya_db)).getInfo();
                            break;
                        }
                    case 38:    // Friend List
                        {
                            task.getSession.m_pi.mp_fi = ((CmdFriendInfo)(_pangya_db)).getInfo();
                            break;
                        }
                    case 39:    // Attendance Reward Info
                        {
                            task.getSession.m_pi.ari = ((CmdAttendanceRewardInfo)(_pangya_db)).getInfo();
                            break;
                        }
                    case 40:    // Register Player Logon ON DB
                        {
                            // Não usa por que é um UPDATE
                            break;
                        }
                    case 41:    // Register Logon of player on Server in DB
                        {
                            // Não usa por que é um UPDATE
                            break;
                        }
                    case 42:    // Grand Prix Clear
                        {
                            task.getSession.m_pi.v_gpc = ((CmdGrandPrixClear)(_pangya_db)).getInfo();

                            break;
                        }
                    case 43: // Grand Zodiac Pontos
                        {
                            task.getSession.m_pi.grand_zodiac_pontos = ((CmdGrandZodiacPontos)(_pangya_db)).getPontos();

                            break;
                        }
                    case 44: // Legacy Tiki Shop(PointShop)
                        {
                            task.getSession.m_pi.m_legacy_tiki_pts = ((CmdLegacyTikiShopInfo)(_pangya_db)).getInfo();

                            break;
                        } 
                    default:
                        break;
                }
                // Incrementa o contador
                task.incremenetCount();


                if (task.getCount() == 39) // 44 - 5 (38 deixei o 1, 2, 3, 40 e 41 para o game server)
                    task.sendCompleteData();
                else if (task.getCount() > 0)
                    task.sendReply(_msg_id + 1);

                // Devolve (deixa a session livre) ou desconnecta ela se foi requisitado
                if (task.getSession.devolve())
                {
                    _smp.message_pool.getInstance().push(new message("[LoginManager::LoginManager][Test1] ", type_msg.CL_FILE_LOG_AND_CONSOLE));
                    sgs.gs.getInstance().DisconnectSession(task.getSession);

                }

            }
            catch (exception ex)
            {
                _smp.message_pool.getInstance().push(new message(
                    $"[LoginSystem::SQLDBResponse][ErrorSystem] {ex}",
                    type_msg.CL_FILE_LOG_AND_CONSOLE));

                try
                {
                    if (task != null && task.getSession.isConnected())
                        sgs.gs.getInstance().DisconnectSession(task.getSession);
                }
                catch (Exception innerEx)
                {
                    _smp.message_pool.getInstance().push(new message(
                        $"[LoginSystem::SQLDBResponse][ErrorSystem] Falha ao desconectar sessão: {innerEx}",
                        type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
            }
        }

        private void clear()
        {
            foreach (var task in v_task)
            {
                task.Dispose(); // Se implementa IDisposable, limpar recursos
            }
            v_task.Clear();
        }


        private void StartCheckTaskFinishThread()
        {
            m_pThread = new Thread(CheckTaskFinish)
            {
                IsBackground = true,
                Name = "LoginManager_CheckTaskFinish"
            };
            m_pThread.Start();
        }

        private void CheckTaskFinish()
        {
            while (!m_check_task_finish_shutdown)
            {
                for (int i = 0; i < v_task.Count; i++)
                {
                    if (v_task[i].isFinished())
                    {
                        v_task[i].Dispose();
                        v_task.RemoveAt(i);
                        i--;
                    }
                }
                Thread.Sleep(1000); // 1 segundo para não consumir CPU excessivamente
            }
            _smp.message_pool.getInstance().push(new message("[LoginManager::checkTaskFinish][Info] saindo de check task finish.", type_msg.CL_FILE_LOG_AND_CONSOLE));
        }

        private void ShutdownCheckTaskFinishThread()
        {
            m_check_task_finish_shutdown = true;

            _smp.message_pool.getInstance().push(new message("[LoginManager::checkTaskFinish][Info] thread check task finish iniciada com sucesso!", type_msg.CL_FILE_LOG_AND_CONSOLE));

            m_pThread?.Join();
        }
    }
}