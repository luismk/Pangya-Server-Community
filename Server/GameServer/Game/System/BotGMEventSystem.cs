using System;
using System.Collections.Generic;
using System.Linq;
using Pangya_GameServer.Repository;
using Pangya_GameServer.Models;
using Pangya_GameServer.UTIL;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
namespace Pangya_GameServer.Game.System
{
    public class BotGMEvent
    {
        List<stRangeTime> m_rt;      // Times to make room event
        List<stReward> m_rewards;
        bool m_load;
        TimeSpan m_st;                            // Usando para n�o ficar criando direto na fun��o de check
        Random rnd;
        public BotGMEvent()
        {
			rnd = new Random();
            this.m_rt = new List<stRangeTime>();
            this.m_rewards = new List<stReward>();
            this.m_load = false;
            this.m_st = new TimeSpan();
            // Inicializa
            initialize();
        }

        public void clear()
        {

            if (!m_rt.empty())
                m_rt.Clear();

            if (!m_rewards.empty())
                m_rewards.Clear();

            m_load = false;
        }

        public void load()
        {
            if (isLoad())
                clear();

            initialize();
        }

        public bool isLoad()
        {
            return m_load;
        }

        public void initialize()
        {
            CmdBotGMEventInfo cmd_bgei = new CmdBotGMEventInfo(0); // Waiter

            snmdb.NormalManagerDB.getInstance().add(0,
                cmd_bgei, null, null);

            if (cmd_bgei.getException().getCodeError() != 0)
            {
                throw cmd_bgei.getException();
            }
            m_rt = cmd_bgei.getTimeInfo();

            cmd_bgei = new CmdBotGMEventInfo(1); // Waiter

            snmdb.NormalManagerDB.getInstance().add(0,
                cmd_bgei, null, null);

            if (cmd_bgei.getException().getCodeError() != 0)
            {
                throw cmd_bgei.getException();
            }

            m_rewards = cmd_bgei.getRewardInfo();
            // Log  
            if (m_rt.Count == 0 || m_rewards.Count == 0)
                _smp.message_pool.getInstance().push(new message("[BotGMEvent::initialize][Warning] Not Loaded!", type_msg.CL_FILE_LOG_AND_CONSOLE));

            m_load = true;

        }

        public bool checkTimeToMakeRoom()
        {
            if (!isLoad())
            {
                _smp.message_pool.getInstance().push(new message("[BotGMEvent::checkTimeToMakeRoom][Error] Bot GM Event not have initialized, please call init function first.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                return false;
            }

            m_st = DateTime.Now.TimeOfDay;

            return m_rt.Any(_el => _el.isBetweenTime(m_st));
        }
         
        public void setSendedMessage()
        {

            if (!isLoad())
            {

                _smp.message_pool.getInstance().push(new message("[BotGMEvent::setSendedMessage][Error] Bot GM Event not have initialized, please call init function first.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                return;
            }



            m_st = DateTime.Now.TimeOfDay;

            for (int i = 0; i < m_rt.Count; i++)
            {
                var _el = m_rt[i];
                if (_el.isBetweenTime(m_st))
                {
                    _el.m_sended_message = true;
                }
                else
                {
                    _el.m_sended_message = false;
                }

                m_rt[i] = _el;
            }
        }
        public stRangeTime getInterval()
        {

            if (!isLoad())
            {

                _smp.message_pool.getInstance().push(new message("[BotGMEvent::getInterval][Error] Bot GM Event not have initialized, please call init function first.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                return null;
            }

            m_st = DateTime.Now.TimeOfDay;

            var rt = m_rt.FirstOrDefault(_el =>
            {
                return _el.isBetweenTime(m_st); // pega somente os que nao foram criados!
            });

            return rt;
        } 
        public List<stReward> calculeReward()
        {

            List<stReward> v_reward = new List<stReward>();
 
            // No m ximo 3 pr mios
            uint num_r = (uint)rnd.Next(1, 3);

            Lottery lottery = new Lottery();
            Lottery.LotteryCtx ctx = null;

            foreach (var el in m_rewards)
            {
                lottery.Push(el.rate, el);
            }

            bool remove_to_roleta = num_r < lottery.getCountItem();

            // Not loop infinite
            num_r = num_r > lottery.getCountItem() ? lottery.getCountItem() : num_r;

            while (num_r > 0)
            {

                if ((ctx = lottery.spinRoleta(remove_to_roleta)) == null)
                {

                    // Log
                    _smp.message_pool.getInstance().push(new message("[BotGMEvent::calculeReward][Error][Warning] nao conseguiu sortear um reward na lottery.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                    // Continua
                    continue;
                }

                v_reward.Add((stReward)ctx.Value);

                // decrease num_r(reward)
                num_r--;
            }

            return v_reward;
        }
    }

    public class sBotGMEvent : Singleton<BotGMEvent>
    {
    }
}
