using System;
using System.Collections.Generic;
using Pangya_GameServer.Models.golden_time_type;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;

namespace Pangya_GameServer.Repository
{
    public class CmdGoldenTimeInfo : Pangya_DB
    {
        public CmdGoldenTimeInfo(bool _waiter = false) : base(_waiter)
        {
            this.m_gt = new List<stGoldenTime>();
        }

        public List<stGoldenTime> getInfo()
        {
            return new List<stGoldenTime>(m_gt);
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {

            checkColumnNumber(6, _result.cols);

            stGoldenTime gt = new stGoldenTime();
            gt.id = IFNULL(_result.data[0]);
            gt.type = (stGoldenTime.eTYPE)IFNULL(_result.data[1]);

            if (_result.data[2] != DBNull.Value)
            {
                var time_db = _translateDate(_result.data[2]).Value;
                var time_now = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, time_db.Hour, time_db.Minute, time_db.Second, time_db.Millisecond);
                gt.date[0] = new SYSTEMTIME(time_now);
            }

            if (_result.data[3] != DBNull.Value)
            {
                gt.date[1] = new SYSTEMTIME((DateTime)_translateDate(_result.data[3]));
            }

            gt.rate_of_players = IFNULL(_result.data[4]);
            gt.is_end = IFNULL(_result.data[5]) == 1 ? true : false;

            m_gt.Add(gt);
        }

        protected override Response prepareConsulta()
        {

            if (m_gt.Count > 0)
            {
                m_gt.Clear();
            }

            var r = procedure(m_szConsulta, "");

            checkResponse(r, "nao conseguiu pegar o Golden Time Info");

            return r;
        }


        private List<stGoldenTime> m_gt = new List<stGoldenTime>();

        private const string m_szConsulta = "pangya.ProcGetGoldenTimeInfo";
    }
}