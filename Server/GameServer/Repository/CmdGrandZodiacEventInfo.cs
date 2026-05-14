using System;
using System.Collections.Generic;
using Pangya_GameServer.Models;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;

namespace Pangya_GameServer.Repository
{
    public class CmdGrandZodiacEventInfo : Pangya_DB
    {
        public CmdGrandZodiacEventInfo(bool _waiter = false) : base(_waiter)
        {
            this.m_rt = new List<range_time>();
        }

        public virtual void Dispose()
        {

            if (!m_rt.empty())
            {
                m_rt.Clear();
            }
        }

        public List<range_time> getInfo()
        {
            return m_rt;
        }

        protected override void lineResult(ctx_res _result, uint _index_reuslt)
        {

            checkColumnNumber(3);

            range_time rt = new range_time(0u);

            if (!(_result.data[0] is DBNull))
                rt.m_start = TimeSpan.Parse(_result.data[0].ToString());

            if (!(_result.data[1] is DBNull))
                rt.m_end = TimeSpan.Parse(_result.data[1].ToString());

            rt.m_type = (range_time.eTYPE_MAKE_ROOM)((byte)IFNULL(_result.data[2]));

            m_rt.Add(rt);
        }

        protected override Response prepareConsulta()
        {
            m_rt.Clear();

            var r = consulta(m_szConsulta);

            checkResponse(r, "Nao conseguiu pegar os tempo do Grand Zodiac Event");

            return r;
        }

        private List<range_time> m_rt = new List<range_time>();

        private const string m_szConsulta = "SELECT inicio_time, fim_time, type FROM pangya.pangya_grand_zodiac_times WHERE valid = 1";

    }
}
