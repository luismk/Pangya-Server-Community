using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PangyaAPI.Utilities;

namespace Pangya_RankingServer.UTIL
{
    public struct FILETIME
    {
        public uint dwLowDateTime;
        public uint dwHighDateTime;
    }

    public class RankRefreshTime : IDisposable
    {
        protected uint m_interval_refresh;          // Intervalo em horas
        protected SYSTEMTIME m_st_last_date;        // Última data (SYSTEMTIME)
        protected DateTime m_ft_last_date;          // Última data (FILETIME)

        public RankRefreshTime()
        {
            m_interval_refresh = 0u;
            m_st_last_date = default;
            m_ft_last_date = default;
        }

        public RankRefreshTime(uint _interval, string _date)
        {
            m_interval_refresh = _interval;
            m_st_last_date = default;
            m_ft_last_date = default;

            setLastRefreshDate(_date);
        }

        public RankRefreshTime(uint _interval, SYSTEMTIME _st)
        {
            m_interval_refresh = _interval;
            m_st_last_date = default;
            m_ft_last_date = default;

            setLastRefreshDate(_st);
        }

        public RankRefreshTime(uint _interval, DateTime _ft)
        {
            m_interval_refresh = _interval;
            m_st_last_date = default;
            m_ft_last_date = default;

            setLastRefreshDate(_ft);
        }

        public void Dispose()
        {
            clear();
        }

        public void clear()
        {
            m_interval_refresh = 0u;
            m_st_last_date = default;
            m_ft_last_date = default;
        }

        // Passou da data, venceu, pode atualizar os registros do Rank
        public bool isOutDated()
        {
            if (m_interval_refresh == 0u)
                return false;

            // Usa FILETIME como referência (UTC) e compara com agora
            var lastUtc = m_ft_last_date;
            if (lastUtc == DateTime.MinValue)
                return false;

            var nextUtc = lastUtc.AddHours(m_interval_refresh);
            return DateTime.Now > nextUtc;
        }

        public string toString()
        {
            return $"Interval_Hora={m_interval_refresh}, Last_Refresh={_formatDate(m_st_last_date)}";
        }

        // Get
        public uint getIntervalRefresh() => m_interval_refresh;

        public SYSTEMTIME getLastRefreshDateSystemTime() => m_st_last_date;

        public DateTime getLastRefreshDateFileTime() => m_ft_last_date;

        // Set
        public void setIntervalRefresh(uint _interval) => m_interval_refresh = _interval;

        public void setLastRefreshDate(string _date)
        {
            _translateDate(_date, ref m_st_last_date);
            setLastRefreshDate(m_st_last_date);
        }

        public void setLastRefreshDate(SYSTEMTIME _st)
        {
            m_st_last_date = _st;

            var local = _fromSystemTime(_st);            // interpreta SYSTEMTIME como horário local
            m_ft_last_date = local;
        }

        public void setLastRefreshDate(DateTime _ft)
        {
            m_ft_last_date = _ft;
            m_st_last_date = new SYSTEMTIME(_ft);
        }

        // ----------------- Helpers privados -----------------

        private static void _translateDate(string text, ref SYSTEMTIME st)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                st = default;
                return;
            }

            // Tenta parsear em vários formatos comuns
            var styles = DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeLocal;
            if (!DateTime.TryParse(text, CultureInfo.InvariantCulture, styles, out var dt))
                if (!DateTime.TryParse(text, CultureInfo.CurrentCulture, styles, out dt))
                {
                    st = default;
                    return;
                }

            st = _toSystemTime(dt);
        }

        private static string _formatDate(SYSTEMTIME st)
        {
            if (st.Year == 0) return "0000-00-00 00:00:00";
            try
            {
                var dt = _fromSystemTime(st);
                return dt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            }
            catch
            {
                return "0000-00-00 00:00:00";
            }
        }

        private static SYSTEMTIME _toSystemTime(DateTime local)
        {
            return new SYSTEMTIME
            {
                Year = (ushort)local.Year,
                Month = (ushort)local.Month,
                Day = (ushort)local.Day,
                DayOfWeek = (ushort)local.DayOfWeek,
                Hour = (ushort)local.Hour,
                Minute = (ushort)local.Minute,
                Second = (ushort)local.Second,
                MilliSecond = (ushort)local.Millisecond
            };
        }

        private static DateTime _fromSystemTime(SYSTEMTIME st)
        {
            if (st.Year == 0 || st.Month == 0 || st.Day == 0)
                return DateTime.MinValue;

            return new DateTime(
                st.Year, st.Month, st.Day,
                st.Hour, st.Minute, st.Second, st.MilliSecond,
                DateTimeKind.Local);
        }
         
    }
}
