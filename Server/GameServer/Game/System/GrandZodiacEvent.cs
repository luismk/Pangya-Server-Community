using System.Collections.Generic;
using System.Linq;
using Pangya_GameServer.Repository;
using Pangya_GameServer.Models;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using System;
namespace Pangya_GameServer.Game.System
{
    public class GrandZodiacEvent
    {
        List<range_time> m_rt;      // Times to make room event
        List<stReward> m_rewards;
        bool m_load;
        TimeSpan m_st;                            // Usando para n�o ficar criando direto na fun��o de check

        public GrandZodiacEvent()
        { 
            this.m_rt = new List<range_time>();
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
            CmdGrandZodiacEventInfo cmd_bgei = new CmdGrandZodiacEventInfo(); // Waiter

            snmdb.NormalManagerDB.getInstance().add(0,
                cmd_bgei, null, null);

            if (cmd_bgei.getException().getCodeError() != 0)
            {
                throw cmd_bgei.getException();
            }

            snmdb.NormalManagerDB.getInstance().add(0,
                cmd_bgei, null, null);

            if (cmd_bgei.getException().getCodeError() != 0)
            {
                throw cmd_bgei.getException();
            }

            m_rt = cmd_bgei.getInfo(); 
            // Log  
            if (m_rt.Count == 0)
                _smp.message_pool.getInstance().push(new message("[GrandZodiacEvent::initialize][Warning] Not Loaded!", type_msg.CL_FILE_LOG_AND_CONSOLE));

            m_load = true;

        }
		
        public bool checkTimeToMakeRoom()
        {
            if (!isLoad())
            {
                _smp.message_pool.getInstance().push(new message("[GrandZodiacEvent::checkTimeToMakeRoom][Error] GrandZodiac Event not have initialized, please call init function first.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                return false;
            }

            m_st = DateTime.Now.TimeOfDay;

            return m_rt.Any(_el => _el.isBetweenTime(m_st));
        }

        public void setSendedMessage()
        {

            if (!isLoad())
            {

                _smp.message_pool.getInstance().push(new message("[GrandZodiacEvent::setSendedMessage][Error] GrandZodiac Event not have initialized, please call init function first.", type_msg.CL_FILE_LOG_AND_CONSOLE));

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

        public void setSendedMessage(range_time rt)
        {

            if (!isLoad())
            {

                _smp.message_pool.getInstance().push(new message("[GrandZodiacEvent::setSendedMessage][Error] GrandZodiac Event not have initialized, please call init function first.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                return;
            }

            if (rt.m_sended_message)
                return;

            var i = m_rt.IndexOf(rt);

            m_st = DateTime.Now.TimeOfDay;

            rt.m_sended_message = true;
            m_rt[i] = rt;
        }

        public List<range_time> getInterval()
        { 
            if (!isLoad())
            {

                _smp.message_pool.getInstance().push(new message("[GrandZodiacEvent::getInterval][Error] GrandZodiac Event not have initialized, please call init function first.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                return null;
            }
             
            m_st = DateTime.Now.TimeOfDay;

            var rt = m_rt.Where(_el =>
            {
                return _el.isBetweenTime(m_st); // pega somente os que nao foram criados!
            }).ToList();

            return rt;
        } 
    }

    public class sGrandZodiacEvent : Singleton<GrandZodiacEvent>
    {
    }
}
