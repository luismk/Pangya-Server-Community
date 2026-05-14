using System;
using System.Collections.Generic;
using System.Linq;
using Pangya_GameServer.Repository;
using Pangya_GameServer.Game.Manager;
using Pangya_GameServer.Models;
using Pangya_GameServer.Models.golden_time_type;
using Pangya_GameServer.UTIL;
using PangyaAPI.IFF.JP.Extensions;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using static Pangya_GameServer.Models.DefineConstants;
namespace Pangya_GameServer.Game.System
{
    public class GoldenTimeSystem
    {
        public GoldenTimeSystem()
        {
            this.m_events = new List<stGoldenTime>();
            this.m_current_golden_time = null;

            // Inicializa
            initialize();
        }

        public bool isLoad()
        {
            return m_load;
        }

        public void load()
        {
            if (isLoad())
            {
                clear();
            }

            initialize();
        }

        public bool checkRound()
        {

            // init function
            bool ret = false;
            var now = DateTime.Now;

            if (m_events.Count == 0 && m_current_golden_time == null)
            {
                return false;
            }

            if (m_current_golden_time == null)
            {

                m_current_golden_time = findNewGoldenTime();

                if (m_current_golden_time == null)
                {
                    return false;
                }

                m_current_golden_time.updateRound();

                if (m_current_golden_time.current_round == null)
                {

                    updateGoldenTimeEnd();

                    // golden time event, finished or past time
                    m_current_golden_time = null;

                    return false;
                }

                // Tira o Round, para inicializar no makeOfListOfPlayersToGoldenTime
                m_current_golden_time.current_round = null;

                initCurrentGoldenTime();

                ret = true;

            }
            else if (m_current_golden_time.current_round == null)
            {
                ret = true;
            }
            else if (!m_current_golden_time.current_round.executed && now.Hour == m_current_golden_time.current_round.time.Hour && now.Minute == m_current_golden_time.current_round.time.Minute && now.Millisecond == m_current_golden_time.current_round.time.MilliSecond)
            {
                ret = true;
            }
            return ret;
        }

        public stGoldenTimeReward calculeRoundReward(List<stPlayerReward> _player_reward)
        {

            stGoldenTimeReward reward = new stGoldenTimeReward();

            // Lambda[getNumberOfRate] 
            uint getNumberOfRate(bool _is_playing, bool _is_premium)
            {
                uint rate = 0;

                if (_is_playing)
                    rate++;

                if (_is_premium)
                    rate++;

                return rate * 100;
            }

            if (_player_reward.Count == 0)
            {
                throw new exception("[GoldenTimeSystem::calculeRoundReward][Error] not have player to reward(_player_reward is empty).", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GOLDEN_TIME_SYSTEM,
                    1, 0));
            }

            if (m_current_golden_time == null)
            {
                throw new exception("[GoldenTimeSystem::calculeRoundReward][Error] current goden time is invalid(nullptr)", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GOLDEN_TIME_SYSTEM,
                    2, 0));
            }

            if (m_current_golden_time.current_round == null)
            {
                throw new exception("[GoldenTimeSystem::calculeRoundReward][Error] current round is invalid(nullptr)", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GOLDEN_TIME_SYSTEM,
                    3, 0));
            }

            reward.round = m_current_golden_time.current_round;

            uint number_of_winners = ((uint)_player_reward.Count / NUMBER_OF_PLAYER_TO_WINNER) * m_current_golden_time.rate_of_players;

            if (number_of_winners == 0u)
            {
                number_of_winners = 1u;
            }

            if ((number_of_winners * m_current_golden_time.rate_of_players) > _player_reward.Count)
            {
                number_of_winners = (uint)_player_reward.Count;
            }

            // Log
            _smp.message_pool.getInstance().push(new message("[GoldenTimeSystem::calculeRoundReward][Log] Golden Time[Rate=" + Convert.ToString(m_current_golden_time.rate_of_players) + "] Round(" + (reward.round.time.ConvertTime()) + ") - Total de participantes(" + Convert.ToString(_player_reward.Count) + ") - Total de ganhadores(" + Convert.ToString(number_of_winners) + ").", type_msg.CL_FILE_LOG_AND_CONSOLE));

            Lottery lottery = new Lottery();

            foreach (var el in _player_reward)
            {
                lottery.Push(100 + getNumberOfRate(el.is_playing, el.is_premium), el);
            }

            Lottery.LotteryCtx ctx = null;
            number_of_winners = lottery.getCountItem() < number_of_winners ? (uint)lottery.getCountItem() : number_of_winners;

            while (number_of_winners > 0)
            {

                if ((ctx = lottery.spinRoleta(true)) != null && ctx.Value is stPlayerReward)
                {

                    reward.players.Add(ctx.Value as stPlayerReward);

                    number_of_winners--;

                }
                else
                {
                    _smp.message_pool.getInstance().push(new message("[GoldenTimeSystem::calculeRoundReward][Error][Warning] nao conseguiu sortear um player em lottery.spinRoleta(). Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
            }

            return reward;
        }

        public bool checkFirstMessage()
        { 

            if (m_current_golden_time.current_round != null)
            {
                return false;
            }

            m_current_golden_time.updateRound();

            // Para quem chama d� o error
            if (m_current_golden_time.current_round == null)
            {
                return true;
            }

            return true;
        }

        public stRound checkNextRound()
        {
            if (m_current_golden_time == null)
            {

                // Cria um novo se tiver eventos
                m_current_golden_time = findNewGoldenTime();

                if (m_current_golden_time == null)
                {
                    return (null);
                };

                initCurrentGoldenTime();

                m_current_golden_time.updateRound();

                return (m_current_golden_time.current_round);
            }

            if (m_current_golden_time.current_round == null || m_current_golden_time.current_round.executed)
            {

                while (m_current_golden_time != null && m_current_golden_time.updateRound() == null)
                {

                    updateGoldenTimeEnd();

                    m_current_golden_time = findNewGoldenTime();

                    if (m_current_golden_time == null)
                    {
                        return (null);
                    }
                }

                initCurrentGoldenTime();

                return (m_current_golden_time.current_round);
            }

            // Terminou turno
            if (DateTime.Now > m_current_golden_time.current_round.time.ConvertTime())
            {

                m_current_golden_time.current_round.executed = true;

                while (m_current_golden_time != null && m_current_golden_time.updateRound() == null)
                {

                    updateGoldenTimeEnd();

                    m_current_golden_time = findNewGoldenTime();

                    if (m_current_golden_time == null)
                    {
                        return (null);
                    };
                }

                initCurrentGoldenTime();

                {
                    return (m_current_golden_time.current_round);
                };
            }

            return null;
        }

        public stRound getCurrentRound()
        {
            return m_current_golden_time == null ? null : m_current_golden_time.current_round;
        }

        public void sendRewardToMailOfPlayers(stGoldenTimeReward _reward)
        {

            // Lambda[getItemName]

            string getItemName(uint _typeid)
            {

                string ret = "";

                var @base = sIff.getInstance().findCommomItem(_typeid);

                if (@base != null)
                {
                    ret = @base.Name;
                }

                return ret;
            }

            try
            {

                stItem item = new stItem();
                BuyItem bi = new BuyItem();

                Player p = null;

                foreach (var el in _reward.players)
                {

                    if ((p = sgs.gs.getInstance().findPlayer(el.uid)) == null)
                    {

                        // Log, Player que ganhou n�o est� mais online, vai ficar sem o item
                        _smp.message_pool.getInstance().push(new message("[GoldenTimeSystem::sendRewardToMailOfPlayers][Warning] Player[UID=" + Convert.ToString(el.uid) + "] ganhou o item[TYPEID=" + Convert.ToString(_reward.round.item._typeid) + ", QNTD=" + Convert.ToString(_reward.round.item.qntd) + ", QNTD_TIME=" + Convert.ToString(_reward.round.item.qntd_time) + "], mas saiu antes dos pr�mios ser entregues, vai ficar sem o pr�mio.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                        continue;
                    }

                    // Limpa
                    bi.clear();
                    item.clear();

                    // Initialize
                    bi.id = -1;
                    bi._typeid = _reward.round.item._typeid;
                    bi.qntd = _reward.round.item.qntd;
                    bi.time = (short)_reward.round.item.qntd_time;

                    ItemManager.initItemFromBuyItem(p.m_pi,
                        item, bi, false, 0, 0, 1);

                    if (item._typeid == 0)
                    {
                        _smp.message_pool.getInstance().push(new message("[GoldenTimeSystem::sendRewardToMailOfPlayers][Error][Warning] tentou enviar o reward para o PLAYER[UID=" + Convert.ToString(p.m_pi.uid) + "] o Item[TYPEID=" + Convert.ToString(_reward.round.item._typeid) + ", QNTD=" + Convert.ToString(_reward.round.item.qntd) + ", QNTD_TIME=" + Convert.ToString(_reward.round.item.qntd_time) + "], mas nao conseguiu inicializar o item. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                    }

                    var msg = ("Golden Time - Round(" + (_reward.round.time.ConvertTime()) + "): item[ " + getItemName(_reward.round.item._typeid) + " ]");

                    if (MailBoxManager.sendMessageWithItem(0,
                        p.m_pi.uid, msg, item) <= 0)
                    {
                        _smp.message_pool.getInstance().push(new message("[GoldenTimeSystem::sendRewardToMailOfPlayers][Error][Warning] tentou enviar reward para o PLAYER[UID=" + Convert.ToString(p.m_pi.uid) + "] o Item[TYPEID=" + Convert.ToString(_reward.round.item._typeid) + ", QNTD=" + Convert.ToString(_reward.round.item.qntd) + ", QNTD_TIME=" + Convert.ToString(_reward.round.item.qntd_time) + "], mas nao conseguiu colocar o item no mail box dele. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                    }
                }

            }
            catch (exception e)
            {

                _smp.message_pool.getInstance().push(new message("[GoldenTimeSystem::sendRewardToMailOfPlayers][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        protected void initialize()
        {
            // Carrega a lista de eventos
            CmdGoldenTimeInfo cmd_gti = new CmdGoldenTimeInfo(); // Waiter

            snmdb.NormalManagerDB.getInstance().add(0,
                cmd_gti, null, null);

            if (cmd_gti.getException().getCodeError() != 0)
            {
                throw cmd_gti.getException();
            }
            m_events = cmd_gti.getInfo();

            foreach (var el_gt in m_events)
            {

                CmdGoldenTimeItem cmd_gt_item = new CmdGoldenTimeItem(el_gt.id); // Waiter;
                CmdGoldenTimeRound cmd_gt_round = new CmdGoldenTimeRound(el_gt.id); // Waiter 

                snmdb.NormalManagerDB.getInstance().add(0,
                    cmd_gt_item, null, null);
                snmdb.NormalManagerDB.getInstance().add(0,
                    cmd_gt_round, null, null);

                try
                {

                    if (cmd_gt_item.getException().getCodeError() != 0)
                    {
                        throw cmd_gt_item.getException();
                    }

                    if (cmd_gt_round.getException().getCodeError() != 0)
                    {
                        throw cmd_gt_round.getException();
                    }

                    el_gt.item_rewards = cmd_gt_item.getInfo();

                    el_gt.rounds = cmd_gt_round.getInfo();

                    foreach (var el_r in el_gt.rounds)
                    {
                        el_r.time.Year = el_gt.date[0].Year;
                        el_r.time.Month = el_gt.date[0].Month;
                        el_r.time.Day = el_gt.date[0].Day;
                        el_r.time.DayOfWeek = el_gt.date[0].DayOfWeek;
                    }

                }
                catch (exception e)
                {

                    // Log
                    _smp.message_pool.getInstance().push(new message("[GoldenTimeSystem::initialize][Error] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
            }

            // Inicializa a ordem dos rounds e dos golden time, e atualiza as datas se for necess�rio

            // Sort Rounds
            foreach (var el in m_events)
            {
                el.rounds.Sort((stRound _el1, stRound _el2) =>
                {
                    long diff = UtilTime.GetTimeDiff(_el1.time, _el2.time);
                    return diff < 0 ? -1 : (diff > 0 ? 1 : 0);
                });
            }

            // Sort Events
            m_events.Sort((stGoldenTime _el1, stGoldenTime _el2) =>
            {
                long diffDate = UtilTime.GetTimeDiff(_el1.date[0], _el2.date[0]);

                if (diffDate == 0L)
                {
                    if (_el1.rounds.Count > 0 && _el2.rounds.Count == 0)
                        return 1;
                    else if (_el1.rounds.Count == 0 && _el2.rounds.Count > 0)
                        return -1;

                    if (_el1.rounds.Count > 0 && _el2.rounds.Count > 0)
                    {
                        long diffRound = UtilTime.GetTimeDiff(_el1.rounds.First().time, _el2.rounds.First().time);
                        return diffRound < 0 ? -1 : (diffRound > 0 ? 1 : 0);
                    }

                    return 0;
                }

                return diffDate < 0 ? -1 : 1;
            });


            // Verifica
            foreach (var el in m_events)
            {

                if (el.type != stGoldenTime.eTYPE.ONE_DAY)
                {

                    if (el.type == stGoldenTime.eTYPE.INTERVAL && (el.date[1].IsEmpty || UtilTime.GetLocalDateDiff(el.date[1]) > 0))
                    {
                        el.is_end = true;
                    }
                    else if (UtilTime.GetLocalDateDiff(el.date[0]) > 0)
                    {

                        el.date[0].CreateTime();

                        el.date[0].Hour = el.date[0].Minute = el.date[0].Second = el.date[0].MilliSecond = 0;

                        foreach (var el_round in el.rounds)
                        {
                            el_round.time.Year = el.date[0].Year;
                            el_round.time.Month = el.date[0].Month;
                            el_round.time.Day = el.date[0].Day;
                            el_round.time.DayOfWeek = el.date[0].DayOfWeek;
                        }
                    }

                }
                else if (UtilTime.GetLocalDateDiff(el.date[0]) > 0)
                {
                    el.is_end = true;
                }
            }

            if (m_events.Count == 0)
                _smp.message_pool.getInstance().push(new message("[GoldenTimeSystem::initialize][Warning] Not Loaded!", type_msg.CL_FILE_LOG_AND_CONSOLE));
            // Carregado com sucesso
            m_load = true;
        }

        protected void clear()
        {
            if (m_events.Count > 0)
            {
                m_events.Clear();
            }

            m_current_golden_time = null;

            m_load = false;
        }

        protected void initCurrentGoldenTime()
        {
            // Se não há prêmios, não faz sentido prosseguir
            if (m_current_golden_time.item_rewards.Count == 0)
            {
                _smp.message_pool.getInstance().push(
                    new message("[GoldenTimeSystem::initCurrentGoldenTime][ERROR] Nenhum item configurado para o Golden Time.",
                    type_msg.CL_FILE_LOG_AND_CONSOLE));
                return;
            }

            bool flag_repeat_item = !(m_current_golden_time.item_rewards.Count < m_current_golden_time.rounds.Count);

            Lottery lottery = new Lottery();
            foreach (var el in m_current_golden_time.item_rewards)
            {
                var rnd = new Random().Next(1, 100);
                lottery.Push(el.rate <= 1 ? (uint)rnd: el.rate, el);
            }

            foreach (var round in m_current_golden_time.rounds)
            {
                var ctx = lottery.spinRoleta(flag_repeat_item);
                var reward = ctx.Value;

                if (reward == null)
                {
                    _smp.message_pool.getInstance().push(
                        new message($"[GoldenTimeSystem::initCurrentGoldenTime][Warning] Não conseguiu sortear um item para o round ({round.time.ConvertTime()})",
                        type_msg.CL_FILE_LOG_AND_CONSOLE));
                    continue;
                }

                // Configura o round
                round.item = new stItemReward();
                round.executed = false;
                round.item = (stItemReward)reward;
                // Next Round
            }
        }

        protected stGoldenTime findNewGoldenTime()
        {
            if (m_events.Count == 0)
                return null;

            stGoldenTime gt = null;

            // Forever
            var evForever = m_events.FirstOrDefault(findForever);
            if (evForever != null)
                gt = evForever;

            // Interval
            var evInterval = m_events.FirstOrDefault(findInterval);
            if (evInterval != null && (gt == null || UtilTime.GetTimeDiff(evInterval.date[0], gt.date[0]) <= 0L))
                gt = evInterval;

            // OneDay
            var evOneDay = m_events.FirstOrDefault(findOneDay);
            if (evOneDay != null && (gt == null || UtilTime.GetTimeDiff(evOneDay.date[0], gt.date[0]) <= 0L))
                gt = evOneDay;

            return gt;
        }


        protected void updateGoldenTimeEnd()
        {

            // Atual golden time acabou, procura um novo
            if (m_current_golden_time.type != stGoldenTime.eTYPE.ONE_DAY)
            {
                var date_new = m_current_golden_time.date[0].ConvertTime();
                var now = DateTime.Now;

                if (date_new.Year < now.Year || date_new.Month < now.Month || date_new.Day < now.Day)//adiciona so se tiver um dia diferente
                    m_current_golden_time.date[0].CreateTime(date_new.AddDays(1)); // Add 1 dia

                // update all date of rounds
                foreach (var el in m_current_golden_time.rounds)
                {
                    el.time.Year = m_current_golden_time.date[0].Year;
                    el.time.Month = m_current_golden_time.date[0].Month;
                    el.time.Day = m_current_golden_time.date[0].Day;
                    el.time.DayOfWeek = m_current_golden_time.date[0].DayOfWeek;
                }

                if (m_current_golden_time.type == stGoldenTime.eTYPE.INTERVAL
                    && !(m_current_golden_time.date[1].IsEmpty)
                    && UtilTime.GetDateDiff(m_current_golden_time.date[0], m_current_golden_time.date[1]) > 0)
                {
                    m_current_golden_time.is_end = true;
                }

            }
            else
            {
                m_current_golden_time.is_end = true;
            }

            // Atualiza no banco de dados
            if (m_current_golden_time.is_end)
            {
                snmdb.NormalManagerDB.getInstance().add(1,
                    new CmdUpdateGoldenTime(m_current_golden_time.id, m_current_golden_time.is_end),
                    SQLDBResponse,
                    this);
            }
        }

        protected static bool findForever(stGoldenTime _el)
        {

            if ((_el.date[0]).IsEmpty)
            {
                return false;
            }

            if (!_el.is_end && _el.type == stGoldenTime.eTYPE.FOREVER)
            {
                return true;
            }

            return false;
        }

        protected static bool findInterval(stGoldenTime _el)
        {

            if ((_el.date[0]).IsEmpty)
            {
                return false;
            }

            if (!_el.is_end
                && !_el.date[1].IsEmpty
                && _el.type == stGoldenTime.eTYPE.INTERVAL
                && UtilTime.GetLocalDateDiff(_el.date[1]) <= 0)
            {
                return true;
            }

            return false;
        }

        protected static bool findOneDay(stGoldenTime _el)
        {

            if (_el.date[0].IsEmpty)
            {
                return false;
            }

            if (!_el.is_end
                && UtilTime.IsSameDay(_el.date[0])
                && _el.type == stGoldenTime.eTYPE.ONE_DAY)
            {
                return true;
            }

            return false;
        }

        protected static void SQLDBResponse(int _msg_id,
            Pangya_DB _pangya_db,
            object _arg)
        {

            if (_arg == null)
            {
                _smp.message_pool.getInstance().push(new message("[GoldenTimeSystem::SQLDBResponse][Warning] _arg is nullptr na msg_id = " + Convert.ToString(_msg_id), type_msg.CL_FILE_LOG_AND_CONSOLE));
                return;
            }

            // Por Hora s� sai, depois fa�o outro tipo de tratamento se precisar
            if (_pangya_db.getException().getCodeError() != 0)
            {
                _smp.message_pool.getInstance().push(new message("[GoldenTimeSystem::SQLDBResponse][Error] " + _pangya_db.getException().getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                return;
            }


            var gts = (GoldenTimeSystem)(_arg);

            switch (_msg_id)
            {
                case 1: // Update Golden Time is_end
                    {


                        var cmd_ugt = (CmdUpdateGoldenTime)(_pangya_db);

                        // Log
                        _smp.message_pool.getInstance().push(new message("[GoldenTimeSystem::SQLDBResponse][Debug] Atualizou o Golden Time[ID=" + Convert.ToString(cmd_ugt.getId()) + ", IS_END=" + (cmd_ugt.getIsEnd() ? "TRUE" : "FALSE") + "] com sucesso.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                        break;
                    }
                case 0:
                default:
                    break;
            }
        }

        private bool m_load;

        private List<stGoldenTime> m_events = new List<stGoldenTime>();

        private stGoldenTime m_current_golden_time;
    }

    public class sGoldenTimeSystem : Singleton<GoldenTimeSystem>
    { }
}
