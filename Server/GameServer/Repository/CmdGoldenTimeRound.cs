using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using Pangya_GameServer.Models.golden_time_type;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;

namespace Pangya_GameServer.Repository
{
    public class CmdGoldenTimeRound : Pangya_DB
    {
        public CmdGoldenTimeRound(uint _id, bool _waiter = false) : base(_waiter)
        {
            this.m_id = _id;
            this.m_round = new List<stRound>();
        }

        public CmdGoldenTimeRound(bool _waiter = false) : base(_waiter)
        {
            this.m_id = 0;
            this.m_round = new List<stRound>();
        }

        public virtual void Dispose()
        {

            if (m_round.Count > 0)
            {
                m_round.Clear();
            }
        }

        public uint getId()
        {
            return (m_id);
        }

        public void setId(uint _id)
        {
            m_id = _id;
        }
        public List<stRound> getInfo()
        {
            return new List<stRound>(m_round);
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {

            checkColumnNumber(1, (uint)_result.cols);

            stRound round = new stRound(0u);

            if (_result.data[0] != null)
            {
                var time_db = _translateDate(_result.data[0]).Value;
                var ts = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, time_db.Hour, time_db.Minute, time_db.Second, time_db.Millisecond);
                // Extrai só a hora, minuto, segundo
                round.time = new SYSTEMTIME
                {
                    Hour = (ushort)ts.Hour,
                    Minute = (ushort)ts.Minute,
                    Second = (ushort)ts.Second,
                    MilliSecond = (ushort)ts.Millisecond,
                    // Zera o resto
                    Year = (ushort)ts.Year,
                    Month = (ushort)ts.Month,
                    Day = (ushort)ts.Day,
                    DayOfWeek = (ushort)ts.DayOfWeek
                };
            }

            m_round.Add(round);
        }

        protected override Response prepareConsulta()
        {

            if (m_id == 0u)
            {
                throw new exception("[CmdGoldenTimeRound::prepareConsulta][Error] m_id is invalid(" + Convert.ToString(m_id) + ").", STDA_MAKE_ERROR(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            if (m_round.Count > 0)
            {
                m_round.Clear();
            }

            var r = consulta(m_szConsulta + Convert.ToString(m_id));

            checkResponse(r, "nao conseguiu pegar o round do Golden Time[ID=" + Convert.ToString(m_id) + "]");

            return r;
        }


        private uint m_id = new uint();

        private List<stRound> m_round = new List<stRound>();

        private const string m_szConsulta = "SELECT time FROM pangya.pangya_golden_time_round WHERE golden_time_id = ";

    }
}