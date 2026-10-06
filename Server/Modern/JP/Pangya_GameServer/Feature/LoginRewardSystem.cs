using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Pangya_GameServer.Repository;
using Pangya_GameServer.Models;
using Pangya_GameServer.Engine;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using Pangya_GameServer.Feature;
using PangyaAPI.IFF.Handle.JP;
using PangyaAPI.DataBase;
using PangyaAPI.Network;
using Pangya_GameServer.Flags;
using Pangya_GameServer.Manager;
using static Pangya_GameServer.Models.DefineConstants;
using Pangya_GameServer.Session;
using snmdb;
namespace Pangya_GameServer.Feature
{
    public class LoginRewardSystem
    {
        // Mutex para substituir a CriticalSection
        private readonly object m_cs = new object();

        private List<stLoginReward> m_events = new List<stLoginReward>();
        private bool m_load = false;

        public void initialize()
        {
            lock (m_cs)
            {
                try
                {
                    // Carrega a lista de eventos
                    CmdLoginRewardInfo cmd_lri = new CmdLoginRewardInfo(); // Waiter

                    snmdb.NormalManagerDB.Instance.add(0, cmd_lri, null, null);

                    if (cmd_lri.getException().getCodeError() != 0)
                        throw cmd_lri.getException();

                    m_events = cmd_lri.getInfo();

                    foreach (var el_e in m_events)
                    {
                        // Verifica se já venceu
                        if (!el_e.end_date.IsEmpty && UtilTime.GetLocalTimeDiff(el_e.end_date) > 0)
                        {
                            el_e.is_end = true;

                            // Atualiza aqui no banco de dados o evento
                            snmdb.NormalManagerDB.Instance.add(1, new CmdUpdateLoginReward(el_e.id, el_e.is_end), SQLDBResponse, this);
                        }
                    }

                    if (m_events.Count == 0)
                        _smp.LogManager.Instance.push(new AppMessage("[LoginRewardSystem::initialize][Log] Login Reward System not loaded!", type_msg.CL_FILE_LOG_AND_CONSOLE));

                    // Carregado com sucesso
                    m_load = true;
                }
                catch (Exception e)
                {
                    _smp.LogManager.Instance.push(new AppMessage("[LoginRewardSystem::initialize][ErrorSystem] " + e.Message, type_msg.CL_FILE_LOG_AND_CONSOLE));
                    throw; // Relança para o server tomar as providências
                }
            }
        }

        public void clear()
        {
            lock (m_cs)
            {
                try
                {
                    if (m_events.Count > 0)
                    {
                        m_events.Clear();
                    }

                    m_load = false;
                }
                catch (Exception e)
                {
                    _smp.LogManager.Instance.push(new AppMessage("[LoginRewardSystem::clear][ErrorSystem] " + e.Message, type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
            }
        }

        public void load()
        {
            if (isLoad())
                clear();

            initialize();
        }

        public bool isLoad()
        {
            lock (m_cs)
            {
                try
                {
                    return m_load;
                }
                catch (Exception e)
                {
                    _smp.LogManager.Instance.push(new AppMessage("[LoginRewardSystem::isLoad][ErrorSystem] " + e.Message, type_msg.CL_FILE_LOG_AND_CONSOLE));
                    return false;
                }
            }
        }

        public void CheckRewardLoginAndSend(Player _session)
        {
            // CHECK_SESSION
            if (!_session.getState())
                throw new Exception("[LoginRewardSystem::checkRewardLoginAndSend][Error] player nao esta connectado.");

            lock (m_cs)
            {
                try
                {
                    // Carrega lista de player do evento
                    CmdLoginRewardPlayerInfo cmd_lrpi = new CmdLoginRewardPlayerInfo(_session.UserInfo.UID); // Waiter

                    // Check All Event Enabled
                    for (int i = 0; i < m_events.Count; i++)
                    {
                        var el_e = m_events[i];

                        if (el_e.is_end)
                            continue;

                        // Se tiver data e passou dela, encerra esse evento
                        if (!el_e.end_date.IsEmpty && UtilTime.GetLocalTimeDiff(el_e.end_date) > 0)
                        {
                            el_e.is_end = true;
                            snmdb.NormalManagerDB.Instance.add(1, new CmdUpdateLoginReward(el_e.id, el_e.is_end), SQLDBResponse, this);
                            continue;
                        }

                        // Pega info do player no banco de dados
                        cmd_lrpi.setId(el_e.id);
                        snmdb.NormalManagerDB.Instance.add(0, cmd_lrpi, null, null);

                        if (cmd_lrpi.getException().getCodeError() != 0)
                        {
                            _smp.LogManager.Instance.push(new AppMessage("[LoginRewardSystem::checkRewardLoginAndSend][Error][WARNING] " + cmd_lrpi.getException().Message, type_msg.CL_FILE_LOG_AND_CONSOLE));
                            continue;
                        }

                        var p = cmd_lrpi.getInfo();

                        if (p.id == 0 && p.uid == 0) // Não tem, cria um novo
                        {
                            p = new stPlayerState { id = 0, uid = _session.UserInfo.UID, count_days = 1, count_seq = 0 };

                            if (p.update_date.IsEmpty)
                                p.update_date = new SystemTime(DateTime.Now);

                            // Add o player ao banco de dados aqui
                            CmdAddLoginRewardPlayer cmd_alrp = new CmdAddLoginRewardPlayer(el_e.id, p);

                            snmdb.NormalManagerDB.Instance.add(0, cmd_alrp, null, null);

                            if (cmd_alrp.getException().getCodeError() != 0 || !cmd_alrp.isGood())
                            {
                                _smp.LogManager.Instance.push(new AppMessage("[LoginRewardSystem::checkRewardLoginAndSend][Error] falha ao adicionar Player[UID=" + _session.UserInfo.UID + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));
                                continue;
                            }

                            p = cmd_alrp.getPlayerState();
                        }
                        else
                        {
                            if (p.is_clear)
                                continue;

                            if (UtilTime.GetLocalDateDiff(p.update_date) <= 0)
                                continue;

                            p.count_days++; // Update count
                            p.update_date = new SystemTime(DateTime.Now);

                            snmdb.NormalManagerDB.Instance.add(2, new CmdUpdateLoginRewardPlayer(p), SQLDBResponse, this);
                        }

                        // Verifica quantas vezes tem que logar para receber o prêmio
                        if (p.count_days < el_e.days_to_gift)
                            continue;

                        p.count_seq++;

                        if (el_e.type == stLoginReward.eTYPE.N_TIME)
                        {
                            if (p.count_seq < el_e.n_times_gift)
                                p.count_days = 0;
                            else
                                p.is_clear = true;
                        }
                        else if (el_e.type == stLoginReward.eTYPE.FOREVER)
                        {
                            p.count_days = 0;
                        }

                        // Atualiza aqui o StateRoom do player no banco de dados
                        snmdb.NormalManagerDB.Instance.add(2, new CmdUpdateLoginRewardPlayer(p), SQLDBResponse, this);

                        // Send Gift
                        SendGiftToPlayer(_session, el_e);
                    }
                }
                catch (Exception e)
                {
                    _smp.LogManager.Instance.push(new AppMessage("[LoginRewardSystem::checkRewardLoginAndSend][ErrorSystem] " + e.Message, type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
            }
        }

        public void UpdateLoginReward()
        {
            lock (m_cs)
            {
                try
                {
                    foreach (var el_e in m_events)
                    {
                        if (el_e.is_end)
                            continue;

                        if (!el_e.end_date.IsEmpty && UtilTime.GetLocalTimeDiff(el_e.end_date) > 0)
                        {
                            el_e.is_end = true;
                            snmdb.NormalManagerDB.Instance.add(1, new CmdUpdateLoginReward(el_e.id, el_e.is_end), SQLDBResponse, this);
                        }
                    }
                }
                catch (Exception e)
                {
                    _smp.LogManager.Instance.push(new AppMessage("[LoginRewardSystem::updateLoginReward][ErrorSystem] " + e.Message, type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
            }
        }

        private void SendGiftToPlayer(Player _session, stLoginReward _lr)
        {
            if (!_session.getState())
                throw new Exception("[LoginRewardSystem::sendGiftToPlayer][Error] player nao esta connectado.");

            try
            {
                stItem item = new stItem();
                BuyItem bi = new BuyItem();

                bi.id = -1;
                bi._typeid = _lr.item_reward._typeid;
                bi.qntd = _lr.item_reward.qntd;
                bi.time = (short)_lr.item_reward.qntd_time;

                ItemManager.initItemFromBuyItem(_session.UserInfo, item, bi, false, 0, 0, 1);

                if (item._typeid == 0)
                    _smp.LogManager.Instance.push(new AppMessage("[LoginRewardSystem::sendGiftToPlayer][Error] Bug inicializar item", type_msg.CL_FILE_LOG_AND_CONSOLE));

                var baseItem = sIff.Instance.findCommomItem(_lr.item_reward._typeid);
                string itemName = baseItem != null ? baseItem.Name : "";

                string msg = "Login Reward System - \"" + _lr.getName() + "\": item[ " + itemName + " ]";

                if (MailManager.SendMessageWithItem(0, _session.UserInfo.UID, msg, item) <= 0)
                    _smp.LogManager.Instance.push(new AppMessage("[LoginRewardSystem::sendGiftToPlayer][Error] Bug MailBox", type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
            catch (Exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[LoginRewardSystem::sendGiftToPlayer][ErrorSystem] " + e.Message, type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        private static void SQLDBResponse(int _msg_id, Pangya_DB _pangya_db, object _arg)
        {
            if (_arg == null) return;
            if (_pangya_db.getException().getCodeError() != 0) return;

            var gts = (LoginRewardSystem)_arg;

            switch (_msg_id)
            {
                case 1: // Update Login Reward
                    break;
                case 2: // Update Login Reward Player
                    break;
            }
        }
    }

    public class sLoginRewardSystem : Singleton<LoginRewardSystem>
    {
    }
}