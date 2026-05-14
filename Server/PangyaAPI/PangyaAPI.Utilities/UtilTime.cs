using System;
using System.Runtime.InteropServices;

namespace PangyaAPI.Utilities
{

    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 16)]
    public class SYSTEMTIME
    {
        /// <summary>
        /// Year
        /// </summary>
        public ushort Year { get; set; }

        /// <summary>
        /// Month
        /// </summary>
        public ushort Month { get; set; }

        /// <summary>
        /// Day of Week
        /// </summary>
        public ushort DayOfWeek { get; set; }

        /// <summary>
        /// Day
        /// </summary>
        public ushort Day { get; set; }

        /// <summary>
        /// Hour
        /// </summary>
        public ushort Hour { get; set; }

        /// <summary>
        /// Minute
        /// </summary>
        public ushort Minute { get; set; }

        /// <summary>
        /// Second
        /// </summary>
        public ushort Second { get; set; }

        /// <summary>
        /// Millisecond
        /// </summary>
        public ushort MilliSecond { get; set; }

        public bool TimeActive
        {
            get
            {
                return Year > 0 && Month > 0 && Day > 0;
            }
        }
        public bool IsEmpty
        {
            get
            {
                return Year == 0 && Month == 0 && DayOfWeek == 0 && Day == 0 && Hour == 0 && Minute == 0 && Second == 0 && MilliSecond == 0;
            }
        }

        public DateTime ConvertTime()
        {
            if (Month < 1 || Month > 12 || Day < 1 || Day > 31 || Hour > 23 || Minute > 59 || Second > 59 || MilliSecond > 999)
            {
                return DateTime.FromFileTimeUtc(0); // 1601-01-01 00:00:00, mínimo válido
            }

            return new DateTime(Year, Month, Day, Hour, Minute, Second, MilliSecond, DateTimeKind.Utc);
        }


        public void CreateTime(string format)
        {
            var date = DateTime.Parse(format);

            Year = (ushort)date.Year;
            Month = (ushort)date.Month;
            Minute = (ushort)date.Minute;
            Day = (ushort)date.Day;
            Hour = (ushort)date.Hour;
            Second = (ushort)date.Second;
            MilliSecond = (ushort)date.Millisecond;
        }

        public void CreateTime()
        {
            var date = DateTime.Now;

            Year = (ushort)date.Year;
            Month = (ushort)date.Month;
            Minute = (ushort)date.Minute;
            Day = (ushort)date.Day;
            Hour = (ushort)date.Hour;
            Second = (ushort)date.Second;
            MilliSecond = (ushort)date.Millisecond;
        }

        public void CreateTime(DateTime? date)
        {
            if (date != null && date != DateTime.MinValue)
            {
                Year = (ushort)date?.Year;
                Month = (ushort)date?.Month;
                Minute = (ushort)date?.Minute;
                Day = (ushort)date?.Day;
                Hour = (ushort)date?.Hour;
                Second = (ushort)date?.Second;
                MilliSecond = (ushort)date?.Millisecond;
            }
        }

        public void Clear()
        {
            Year = 0;
            Month = 0;
            Minute = 0;
            Day = 0;
            Hour = 0;
            Second = 0;
            MilliSecond = 0;
        }
        public SYSTEMTIME(int init = 0)
        {
            if (init == 1)
                CreateTime();
        }
        public SYSTEMTIME()
        {
        }

        public SYSTEMTIME(ushort _Year, ushort _Month = 0, ushort _Day = 0, ushort _Hour = 0, ushort _Minute = 0, ushort _Second = 0, ushort _Millisecond = 0)
        {
            Year = _Year;
            Month = _Month;
            Minute = _Minute;
            Day = _Day;
            Hour = _Hour;
            Second = _Second;
            MilliSecond = _Millisecond;
        }
        public SYSTEMTIME(ushort _Year, ushort _Month = 0, ushort _Day = 0, ushort _DayOfWeek = 0, ushort _Hour = 0, ushort _Minute = 0, ushort _Second = 0, ushort _Millisecond = 0)
        {
            Year = _Year;
            Month = _Month;
            Minute = _Minute;
            Day = _Day;
            DayOfWeek = _DayOfWeek;
            Hour = _Hour;
            Second = _Second;
            MilliSecond = _Millisecond;
        }
        public void SetInfo(SYSTEMTIME date)
        {
            Year = date.Year;
            Month = date.Month;
            Minute = date.Minute;
            Day = date.Day;
            DayOfWeek = date.DayOfWeek;
            Hour = date.Hour;
            Second = date.Second;
            MilliSecond = date.MilliSecond;
        }
        public DateTime Time
        {
            get
            {
                if (TimeActive)//normal item
                {
                    return new DateTime(Year, Month, Day, Hour, Minute, Second, MilliSecond);
                }
                //for grand prix :D
                else if (Hour > 0 || Minute > 0)
                {

                    var value = DateTime.Now; Year = (ushort)value.Year;
                    Month = (ushort)value.Month;
                    DayOfWeek = (ushort)value.DayOfWeek;
                    Day = (ushort)value.Day;
                    return new DateTime(value.Year, value.Month, value.Day, Hour, Minute, 0, 0);//aqui tem que setar, dia mes e ano
                }
                return DateTime.Now;
            }
            set
            {
                Year = (ushort)value.Year;
                Month = (ushort)value.Month;
                DayOfWeek = (ushort)value.DayOfWeek;
                Day = (ushort)value.Day;
                Hour = (ushort)value.Hour;
                Minute = (ushort)value.Minute;
                MilliSecond = (ushort)value.Millisecond == 0 ? (ushort)DateTime.Now.Millisecond : (ushort)value.Millisecond;
                Second = (ushort)value.Second;
            }
        }
        public DateTime TimeGP
        {
            get
            {
                if (Year == 0 && Month == 0 && Day == 0)
                    return new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, Hour, Minute, Second, MilliSecond);
                else
                    return new DateTime(Year, Month, Day, Hour, Minute, Second, MilliSecond);//aqui tem que setar, dia mes e ano
            }
            set
            {
                Year = 0;        // Ano
                Month = 0;         // Mês
                DayOfWeek = 0;      // Dia da semana (não utilizado aqui)
                Day = 0;           // Dia do mês     
                Hour = (ushort)value.Hour;
                Minute = (ushort)value.Minute;
                Second = (ushort)value.Second;
            }
        }

        public DateTime CheckAndReset()
        {
            Year = (ushort)DateTime.Now.Year;        // Ano
            Month = (ushort)DateTime.Now.Month;         // Mês
            DayOfWeek = (ushort)DateTime.Now.DayOfWeek;      // Dia da semana (não utilizado aqui)
            Day = (ushort)DateTime.Now.Day;           // Dia do mês

            // Criação de um novo DateTime com os valores decodificados
            var value = new DateTime(Year, Month, Day, Hour, Minute, Second, MilliSecond);

            // Retorna o novo DateTime
            return value;

        }

        public void ClearTime()
        {
            Year = 0;
            Month = 0;
            DayOfWeek = 0;
            Day = 0;
            Hour = 0;
            Minute = 0;
            Second = 0;
        }

        public string ToString(string format)
        {
            return Time.ToString(format);
        }
        public string GPToString()
        {
            return TimeGP.ToString();
        }

        public void CreateTime(DateTime date)
        {
            if (date != DateTime.MinValue)
            {

                Year = (ushort)date.Year;
                Month = (ushort)date.Month;
                Minute = (ushort)date.Minute;
                Day = (ushort)date.Day;
                Hour = (ushort)date.Hour;
                Second = (ushort)date.Second;
                MilliSecond = (ushort)date.Millisecond;

            }
        }

        public SYSTEMTIME
            (DateTime date)
        {
            Time = date;
        }
    }
    public class UtilTime
    {
        public static DateTime ToDateTime(SYSTEMTIME st)
        {
            try
            {
                return new DateTime(st.Year, st.Month, st.Day, st.Hour, st.Minute, st.Second, st.MilliSecond);
            }
            catch
            {
                return DateTime.MinValue; // Caso a data seja inválida no IFF
            }
        }

        /// <summary>
        /// Retorna o timestamp Unix (em segundos) da hora local atual.
        /// </summary>
        /// <returns>Timestamp Unix como uint.</returns>
        public static uint GetLocalTimeAsUnix()
        {
            DateTime localNow = DateTime.Now;
            DateTimeOffset localOffset = new DateTimeOffset(localNow);
            return (uint)localOffset.ToUnixTimeSeconds();
        }

        public static int TranslateDate(string dateSrc, ref DateTime dateDst)
        {
            if (dateSrc == null)
                throw new ArgumentNullException(nameof(dateSrc));

            if (dateDst == null)
                throw new ArgumentNullException(nameof(dateDst));

            if (string.IsNullOrEmpty(dateSrc))
            {
                dateDst = DateTime.MinValue;
                return 0;
            }

            if (DateTime.TryParseExact(dateSrc, "yyyy-MM-dd HH:mm:ss", null, System.Globalization.DateTimeStyles.None, out dateDst))
                return 0;

            return -1; // Return an appropriate error code if parsing fails
        }

        public static int TranslateTime(string dateSrc, ref DateTime dateDst)
        {
            if (dateSrc == null)
                throw new ArgumentNullException(nameof(dateSrc));

            if (dateDst == null)
                throw new ArgumentNullException(nameof(dateDst));

            if (string.IsNullOrEmpty(dateSrc))
            {
                dateDst = DateTime.MinValue;
                return 0;
            }

            if (DateTime.TryParseExact(dateSrc, "HH:mm:ss.fff", null, System.Globalization.DateTimeStyles.None, out dateDst))
                return 0;

            return -1; // Return an appropriate error code if parsing fails
        }

        // Implement the rest of the methods in a similar fashion
        // ...

        public static long GetTimeDiff(DateTime st1, DateTime st2)
        {
            return st1.ToFileTimeUtc() - st2.ToFileTimeUtc();
        }


        public static long GetTimeDiff(SYSTEMTIME st1, SYSTEMTIME st2)
        {
            return GetTimeDiff(st1.ConvertTime(), st2.ConvertTime());
        }


        public static long UnixTimeConvert(DateTime? unixtime)
        {
            if (unixtime.HasValue == false || unixtime?.Ticks == 0)
            { return 0; }
            TimeSpan timeSpan = (TimeSpan)(unixtime - new DateTime(1970, 1, 1, 0, 0, 0));
            return (long)timeSpan.TotalSeconds;
        }

        public static long UnixTimeConvert(long unixtime)
        {
            // Converte Unix timestamp (segundos) para DateTime em UTC
            return DateTimeOffset.FromUnixTimeSeconds(unixtime).ToUnixTimeMilliseconds();
        }
        public static long UnixTimeConvert(DateTime dateTime)
        {
            // Garante que o DateTime seja tratado como UTC
            DateTimeOffset offset = dateTime.Kind == DateTimeKind.Unspecified
                ? new DateTimeOffset(DateTime.SpecifyKind(dateTime, DateTimeKind.Local))
                : new DateTimeOffset(dateTime);

            return offset.ToUnixTimeMilliseconds();
        }


        public static long DateTimeToUnixTimestamp(DateTime dateTime)
        {
            return (long)(dateTime.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
        }

        public static DateTime UnixTimestampToDateTime(long unixTimestamp)
        {
            return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(unixTimestamp).ToLocalTime();
        }

        // The methods for date and time differences can be converted like this:

        public static long GetHourDiff(DateTime st1, DateTime st2)
        {
            TimeSpan diff = st1 - st2;
            return (long)diff.TotalMilliseconds;
        }

        public static long GetHourDiff(SYSTEMTIME st1, SYSTEMTIME st2)
        {
            // Usa data fixa para evitar erro com data inválida
            DateTime dt1 = new DateTime(2000, 1, 1, st1.Hour, st1.Minute, st1.Second, st1.MilliSecond);
            DateTime dt2 = new DateTime(2000, 1, 1, st2.Hour, st2.Minute, st2.Second, st2.MilliSecond);

            TimeSpan diff = dt1 - dt2;

            long minutes = (long)Math.Round(value: diff.TotalMilliseconds / 1000.0f, MidpointRounding.AwayFromZero);

            // Se quiser evitar negativo
            if (minutes < 0)
                minutes = 0;

            return minutes;
        }

        public static bool IsSameDay(DateTime st1, DateTime st2)
        {
            return st1.Date == st2.Date;
        }

        public static bool IsSameDay(SYSTEMTIME st1, SYSTEMTIME st2)
        {
            return st1.ConvertTime().Date == st2.ConvertTime().Date;
        }

        public static bool IsSameDayNow(DateTime st)
        {
            return st.Date == DateTime.Now.Date;
        }

        public static bool IsSameDay(SYSTEMTIME st)
        {
            return st.ConvertTime().Date == DateTime.Now.Date;
        }
        public static bool IsEmpty(DateTime st)
        {
            return st == DateTime.MinValue;
        }

        public static bool IsEmpty(SYSTEMTIME st)
        {
            return st.IsEmpty || st.ConvertTime() == DateTime.MinValue;
        }
        public static long GetLocalDateDiff(SYSTEMTIME st)
        {
            if (!st.IsEmpty)
            {
                DateTime local = DateTime.Now;
                TimeSpan diff = st.ConvertTime() - local.Date;
                return diff.Ticks / TimeSpan.TicksPerMillisecond;
            }
            return 0;
        }

        public static long GetLocalDateDiff(DateTime st)
        {
            DateTime local = DateTime.Now;
            TimeSpan diff = local - st;
            return diff.Ticks / TimeSpan.TicksPerMillisecond;
        }

        public static long GetLocalDateDiffDESC(SYSTEMTIME st)
        {
            DateTime local = DateTime.Now;

            // Se st for anterior a local, a diferença será negativa
            TimeSpan diff = st.ConvertTime() - local;

            // Retorna a diferença em milissegundos
            return diff.Ticks / TimeSpan.TicksPerMillisecond;
        }

        public static long GetSystemDateDiff(DateTime st)
        {
            DateTime system = DateTime.UtcNow;
            TimeSpan diff = st.Date - system.Date;
            return diff.Ticks / TimeSpan.TicksPerMillisecond;
        }

        public static long GetSystemDateDiffDESC(DateTime st)
        {
            DateTime system = DateTime.UtcNow;
            TimeSpan diff = system.Date - st.Date;
            return diff.Ticks / TimeSpan.TicksPerMillisecond;
        }

        public static long TzLocalUnixToUnixUTC(long localUnixTime)
        {
            DateTimeOffset localTime = DateTimeOffset.FromUnixTimeSeconds(localUnixTime);
            DateTimeOffset utcTime = localTime.ToUniversalTime();
            return utcTime.ToUnixTimeSeconds();
        }


        public static string FormatDate(DateTime t)
        {
            var str = string.Format(
                "{0:0000}-{1:00}-{2:00} {3:00}:{4:00}:{5:00}.{6:000}",
                t.Year, t.Month, t.Day,
                t.Hour, t.Minute, t.Second, t.Millisecond
            );
            return str;
        }

        public static string FormatTime(DateTime t)
        {
            var str = string.Format(
                "{0:00}:{1:00}:{2:00}.{3:000}",
                t.Hour, t.Minute, t.Second, t.Millisecond
            );

            return str;
        }

        public static string FormatDate(SYSTEMTIME _date)
        {
            var date = _date.ConvertTime();
            return $"{date:yyyy-MM-dd HH:mm:ss.fff}";
        }

        // Função para traduzir data de Unix para SYSTEMTIME (UTC)
        public static int TranslateDateSystem(long timeUnix, out DateTime dateDst)
        {
            if (timeUnix == 0)
            {
                dateDst = DateTime.UtcNow; // Data atual no UTC
            }
            else
            {
                dateDst = DateTimeOffset.FromUnixTimeSeconds(timeUnix).UtcDateTime;
            }

            return 0;
        }

        // Função para traduzir data de Unix para SYSTEMTIME (Local)
        public static int TranslateDateLocal(long timeUnix, out DateTime dateDst)
        {
            if (timeUnix == 0)
            {
                dateDst = DateTime.Now; // Data atual no local da máquina
            }
            else
            {
                dateDst = DateTimeOffset.FromUnixTimeSeconds(timeUnix).LocalDateTime;
            }

            return 0;
        }

        public static string _formatDate(DateTime date)
        {
            return FormatDate(date);
        }

        public static string _formatDate(SYSTEMTIME date)
        {
            return FormatDate(date);
        }

        // Função para formatar data do sistema para string (UTC)
        public static string FormatDateSystem(long timeUnix)
        {
            DateTime date;
            TranslateDateSystem(timeUnix, out date);
            return date.ToString("yyyy-MM-dd HH:mm:ss");
        }

        // Função para formatar data local para string
        public static string FormatDateLocal(long timeUnix)
        {
            DateTime date;
            TranslateDateLocal(timeUnix, out date);
            return date.ToString("yyyy-MM-dd HH:mm:ss");
        }
        public static string formatDateLocal(long timeUnix)
        {
            DateTime date;
            TranslateDateLocal(timeUnix, out date);
            return date.ToString("yyyy-MM-dd HH:mm:ss");
        }
        [DllImport("kernel32.dll", SetLastError = true)]
        [Obsolete]
        public static extern bool SystemTimeToFileTime(ref SYSTEMTIME lpSystemTime, ref FILETIME lpFileTime);

        // Função para converter System Time para Unix Timestamp
        public static long SystemTimeToUnix(DateTime st)
        {
            return (long)(st.ToUniversalTime() - new DateTime(1970, 1, 1)).TotalSeconds;
        }

        public static long SystemTimeToUnix(SYSTEMTIME st)
        {
            return (long)(st.ConvertTime().ToUniversalTime() - new DateTime(1970, 1, 1)).TotalSeconds;
        }
        // Função para obter o sistema como Unix Timestamp
        public static long GetSystemTimeAsUnix()
        {
            return SystemTimeToUnix(DateTime.UtcNow);
        }
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern void GetLocalTime(ref SYSTEMTIME lpSystemTime);

        public static void GetLocalTime(out DateTime time)
        {
            time = DateTime.Now;
        }

        public static SYSTEMTIME UnixToSystemTime(long unixTime)
        {
            return new SYSTEMTIME(DateTimeOffset.FromUnixTimeSeconds(unixTime).DateTime);
        }

        public static long GetLocalTimeDiffDESC(SYSTEMTIME time)
        {
            return _GetLocalTimeDiffDESC(time.ConvertTime()).Ticks;
        }

        public static long GetLocalTimeDiffDESC(DateTime time)
        {
            return DateTime.Now >= time ? 1 : 0;
        }

        public static TimeSpan _GetLocalTimeDiffDESC(DateTime time)
        {
            return DateTime.Now - time; // positivo se já passou, negativo se ainda não
        }

        public static bool IsExpired(DateTime time)
        {
            return DateTime.Now >= time;
        }

        public static bool IsExpired(SYSTEMTIME time)
        {
            var _time = time.ConvertTime();
            return DateTime.Now >= _time;
        }

        public static long GetLocalTimeDiff(SYSTEMTIME dateTime)
        {
            // Cada Tick = 100 nanosegundos = 0.1 microssegundo
            return DateTime.Now.Ticks - dateTime.ConvertTime().Ticks;
        }
        public static long GetLocalTimeDiff(DateTime dateTime)
        {
            // Cada Tick = 100 nanosegundos = 0.1 microssegundo
            return DateTime.Now.Ticks - dateTime.Ticks;
        }

        public static SYSTEMTIME UnixUTCToTzLocalTime(long timeUnix)
        {
            // Converte Unix timestamp para DateTime UTC
            DateTime utc = DateTimeOffset.FromUnixTimeSeconds(timeUnix).UtcDateTime;

            // Converte UTC para horário local
            DateTime tzLocal = TimeZoneInfo.ConvertTimeFromUtc(utc, TimeZoneInfo.Local);

            return new SYSTEMTIME(tzLocal);
        }

        public static long TzLocalTimeToUnixUTC(SYSTEMTIME dt)
        {
            return new DateTimeOffset(dt.ConvertTime()).ToUnixTimeSeconds();
        }

        public static int GetDateDiff(SYSTEMTIME _st1, SYSTEMTIME _st2)
        {
            // Apenas data a diferença
            _st1.Hour = _st1.Minute = _st1.Second = _st1.MilliSecond = 0;
            _st2.Hour = _st2.Minute = _st2.Second = _st2.MilliSecond = 0;

            return (int)GetTimeDiff(_st1, _st2);
        }

        public static long GetTickCount()
        {
            return (long)Environment.TickCount;
        }
    }
}
