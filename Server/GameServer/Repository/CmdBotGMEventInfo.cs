using System;
using System.Collections.Generic;
using Pangya_GameServer.Models;
using PangyaAPI.SQL;

namespace Pangya_GameServer.Repository
{
    public class CmdBotGMEventInfo : Pangya_DB
    {

        public CmdBotGMEventInfo(int _tipo = 0)
        {
            this.m_reward = new List<stReward>();
            this.m_time = new List<stRangeTime>();
            tipo = _tipo;
        }


        public List<stReward> getRewardInfo()
        {
            return new List<stReward>(m_reward);
        }

        public List<stRangeTime> getTimeInfo()
        {
            return new List<stRangeTime>(m_time);
        }

        public void setTipo(int _tipo = 0)
        {
            tipo = _tipo;
        }
        protected override void lineResult(ctx_res _result, uint _index_result)
        {

            try
            {
                if (_result.cols == 3)
                {

                    checkColumnNumber(3);

                    // Time
                    stRangeTime rt = new stRangeTime(0u);

                    if (!(_result.data[0] is DBNull))
                    {
                        rt.m_start = TimeSpan.Parse(_result.data[0].ToString());
                    }

                    if (!(_result.data[1] is DBNull))
                    {
                        rt.m_end = TimeSpan.Parse(_result.data[1].ToString());
                    }

                    rt.m_channel_id = (byte)IFNULL(_result.data[2]);

                    m_time.Add(rt);

                }
                else if (_result.cols == 4)
                {

                    checkColumnNumber(4);
                    var reward = new stReward
                    {
                        _typeid = IFNULL(_result.data[0]),
                        qntd = IFNULL(_result.data[1]),
                        qntd_time = IFNULL(_result.data[2]),
                        rate = IFNULL(_result.data[3])
                    };
                    // Reward
                    m_reward.Add(reward);
                }

            }
            catch (Exception)
            {
                throw;
            }
        }

        protected override Response prepareConsulta()
        {
            //0 = timers
            //1 = prizes
            var r = consulta(m_szConsulta[tipo]);

            checkResponse(r, "nao conseguiu pegar o info do Bot GM Event");

            return r;
        }

        private List<stReward> m_reward = new List<stReward>();
        private List<stRangeTime> m_time = new List<stRangeTime>();
        public int tipo;

        private string[] m_szConsulta = { "SELECT inicio_time, fim_time, channel_id FROM pangya.pangya_bot_gm_event_time WHERE valid = 1;", "SELECT typeid, qntd, qntd_time, rate FROM pangya.pangya_bot_gm_event_reward WHERE valid = 1" };
    }
}