using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace PangyaAPI.Utilities
{
    public enum STDA_ERROR_TYPE : uint
    {
        WSA,
        _SOCKET,
        SOCKETSERVER,
        IOCP,
        THREAD,
        THREADPOOL,
        THREADPL_SERVER,
        THREADPL_CLIENT,
        _MYSQL,
        _MSSQL,
        _POSTGRESQL,
        MANAGER,
        EXEC_QUERY,
        PANGYA_DB,
        BUFFER,
        PACKET,
        PACKET_POOL,
        PACKET_FUNC,
        PACKET_FUNC_SV,
        PACKET_FUNC_LS,
        PACKET_FUNC_AS,
        PACKET_FUNC_RS,
        PACKET_FUNC_GG_AS,
        PACKET_FUNC_CLIENT,
        FUNC_ARR,
        SESSION,
        SESSION_POOL,
        JOB,
        JOB_POOL,
        UTIL_TIME,
        MESSAGE,
        MESSAGE_POOL,
        LIST_FIFO,
        LIST_FIFO_CONSOLE,
        CRYPT,
        COMPRESS,
        PANGYA_ST,
        PANGYA_GAME_ST,
        PANGYA_LOGIN_ST,
        PANGYA_MESSAGE_ST,
        PANGYA_RANK_ST,
        _IFF,
        CLIENTVERSION,
        SERVER,
        GAME_SERVER,
        LOGIN_SERVER,
        MESSAGE_SERVER,
        AUTH_SERVER,
        RANK_SERVER,
        GG_AUTH_SERVER,
        CLIENT,
        MULTI_CLIENT,
        CLIENTE,
        TIMER,
        TIMER_MANAGER,
        CHANNEL,
        LOBBY,
        ROOM,
        ROOM_GRAND_PRIX,
        ROOM_GRAND_ZODIAC_EVENT,
        ROOM_BOT_GM_EVENT,
        ROOM_MANAGER,
        LIST_ASYNC,
        _RESULT_SET,
        _RESPONSE,
        _ITEM,
        _ITEM_MANAGER,
        GM_INFO,
        PLAYER_INFO,
        PLAYER,
        PLAYER_MANAGER,
        SESSION_MANAGER,
        CLIENTE_MANAGER,
        READER_INI,
        MGR_ACHIEVEMENT,
        MGR_DAILY_QUEST,
        SYS_ACHIEVEMENT,
        NORMAL_DB,
        LOGIN_MANAGER,
        LOGIN_TASK,
        MAIL_BOX_MANAGER,
        PERSONAL_SHOP,
        PERSONAL_SHOP_MANAGER,
        LOTTERY,
        CARD_SYSTEM,
        COMET_REFILL_SYSTEM,
        PAPEL_SHOP_SYSTEM,
        BOX_SYSTEM,
        MEMORIAL_SYSTEM,
        PACKET_FUNC_MS,
        FRIEND_MANAGER,
        HOLE,
        GAME,
        TOURNEY_BASE,
        VERSUS_BASE,
        PRACTICE,
        CUBE_COIN_SYSTEM,
        COIN_CUBE_LOCATION_SYSTEM,
        TREASURE_HUNTER_SYSTEM,
        DROP_SYSTEM,
        TOURNEY,
        VERSUS,
        COURSE,
        MATCH,
        TEAM,
        GRAND_PRIX,
        GUILD_BATTLE,
        PANG_BATTLE,
        APPROACH,
        GRAND_ZODIAC_BASE,
        GRAND_ZODIAC,
        CHIP_IN_PRACTICE,
        ATTENDANCE_REWARD_SYSTEM,
        UNIT,
        UNIT_CONNECT,
        UNIT_AUTH_SERVER_CONNECT,
        UNIT_GG_AUTH_SERVER_CONNECT,
        MD5,
        RANDOM_GEN,
        DUPLA,
        DUPLA_MANAGER,
        GUILD,
        GUILD_ROOM_MANAGER,
        APPROACH_MISSION_SYSTEM,
        RANK_REGISTRY_MANAGER,
        GOLDEN_TIME_SYSTEM,
        LOGIN_REWARD_SYSTEM,
        PREMIUM_SYSTEM,
        SMART_CALCULATOR,
        PLAYER_MAIL_BOX,
        QUEUETIMER
    }
    public static class ExceptionError
    {
        public static uint STDA_SOURCE_ERROR_ENCODE(uint source_error) => (uint)(((source_error) << 24) & 0xFF000000);
        public static uint STDA_SOURCE_ERROR_DECODE(uint err_code) => (uint)(((err_code) >> 24) & 0x000000FF);
        public static STDA_ERROR_TYPE STDA_SOURCE_ERROR_DECODE_TYPE(uint err_code) => (STDA_ERROR_TYPE)(((err_code) >> 24) & 0x000000FF);
        public static uint STDA_ERROR_ENCODE(uint err) => (uint)(((err) << 16) & 0x00FF0000);
        public static uint STDA_ERROR_DECODE(uint err_code) => (uint)(((err_code) >> 16) & 0x000000FF);
        public static uint STDA_SYSTEM_ERROR_ENCODE(uint _err_sys) => (uint)((_err_sys) & 0x0000FFFF);
        public static uint STDA_SYSTEM_ERROR_DECODE(uint err_code) => (uint)((err_code) & 0x0000FFFF);
        public static uint STDA_SYSTEM_ERROR_DECODE_TYPE(uint err_code) => (uint)((err_code) & 0x0000FFFF);
        public static uint STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE source_error, uint err_code, uint _err_sys) => (uint)(STDA_SOURCE_ERROR_ENCODE(Convert.ToUInt32(source_error)) | STDA_ERROR_ENCODE((err_code)) | STDA_SYSTEM_ERROR_ENCODE((_err_sys)));
        public static uint STDA_MAKE_ERROR(uint source_error, uint err_code, uint _err_sys) => (uint)(STDA_SOURCE_ERROR_ENCODE((source_error)) | STDA_ERROR_ENCODE((err_code)) | STDA_SYSTEM_ERROR_ENCODE((_err_sys)));
        public static bool STDA_ERROR_CHECK_SOURCE_AND_ERROR(uint err_code, uint source_error, uint error) => (bool)(STDA_SOURCE_ERROR_DECODE((err_code)) == (source_error) && STDA_ERROR_DECODE((err_code)) == (error));
        public static bool STDA_ERROR_CHECK_SOURCE_AND_ERROR_TYPE(uint err_code, STDA_ERROR_TYPE source_error, uint error) => (bool)(STDA_SOURCE_ERROR_DECODE((err_code)) == (uint)(source_error) && STDA_ERROR_DECODE((err_code)) == (error));

    }

    public class exception : Exception
    {
        protected string m_message_error = "";
        protected string m_message_error_full = "";
        protected uint m_code_error = 0;
        public exception(string message) : base(message)
        {
            if (!string.IsNullOrEmpty(message))
                HandleException(this);
        }

        public exception(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected exception(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public exception(string message_error, uint code_error) : this(message_error + "\t Error Code: " + code_error)
        {
            m_message_error = message_error;
            m_code_error = code_error;

            m_message_error_full = m_message_error + "\t Error Code: " + code_error;

            Debug.WriteLine(m_message_error_full);

            if (!string.IsNullOrEmpty(message_error))
                HandleException(this);
        }

        public exception(string message_error, STDA_ERROR_TYPE code_error) : this(message_error + "\t Error Code: " + code_error)
        {
            m_message_error = message_error;
            m_code_error = (uint)code_error;

            m_message_error_full = m_message_error + "\t Error Code: " + code_error;

            Debug.WriteLine(m_message_error_full);

        }

        public Exception GetException()
        {
            return this;
        }
        public string getMessageError()
        {
            return m_message_error;
        }

        public uint getCodeError()
        {
            return m_code_error;
        }

        public string getFullMessageError()
        {
            return m_message_error_full + ", [Strace]: " + getStackTrace();
        }

        public string getStackTrace()
        {
            return this.StackTrace;
        }
        private void HandleException(exception ex, [CallerMemberName] string source = "")
        {
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string dumpPath = $"crash_{timestamp}.dmp";
            string logPath = $"crash_{timestamp}.log";

            try
            {
                if (!File.Exists(dumpPath))
                    DumpGenerator.CreateDump(dumpPath, DumpGenerator.MINIDUMP_TYPE.MiniDumpNormal); // Cria dump

                // Cria log de erro
                File.WriteAllText(logPath,
                    $"Origem: {source}\n" +
                    $"Data: {DateTime.Now}\n" +
                    $"Mensagem: {ex?.Message}\n" +
                    $"StackTrace:\n{ex?.StackTrace}");

                Console.WriteLine($"Dump e log gerados: {dumpPath}, {logPath}");
            }
            catch (Exception innerEx)
            {
                Console.WriteLine($"Falha ao gerar dump/log: {innerEx.Message}");
            }

        }

        private void HandleException(Exception ex, [CallerMemberName] string source = "")
        {
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string dumpPath = $"crash_{timestamp}.dmp";
            string logPath = $"crash_{timestamp}.log";

            try
            {
                // Cria dump
                DumpGenerator.CreateDump(dumpPath, DumpGenerator.MINIDUMP_TYPE.MiniDumpNormal);

                // Cria log de erro
                File.WriteAllText(logPath,
                    $"Origem: {source}\n" +
                    $"Data: {DateTime.Now}\n" +
                    $"Mensagem: {ex?.Message}\n" +
                    $"StackTrace:\n{ex?.StackTrace}");

                Console.WriteLine($"Dump e log gerados: {dumpPath}, {logPath}");
            }
            catch (Exception innerEx)
            {
                Console.WriteLine($"Falha ao gerar dump/log: {innerEx.Message}");
            }

        }
    }

    class DumpGenerator
    {
        [Flags]
        public enum MINIDUMP_TYPE : uint
        {
            MiniDumpNormal = 0x00000000,
            MiniDumpWithDataSegs = 0x00000001,
            MiniDumpWithFullMemory = 0x00000002,
            MiniDumpWithHandleData = 0x00000004,
            MiniDumpWithThreadInfo = 0x00001000,
            // Você pode combinar com '|' os flags desejados
        }

        [DllImport("dbghelp.dll", SetLastError = true)]
        static extern bool MiniDumpWriteDump(
            IntPtr hProcess,
            uint processId,
            SafeHandle hFile,
            MINIDUMP_TYPE dumpType,
            IntPtr exceptionParam,
            IntPtr userStreamParam,
            IntPtr callbackParam);

        public static void CreateDump(string dumpFilePath, MINIDUMP_TYPE dumpType = MINIDUMP_TYPE.MiniDumpWithThreadInfo)
        {
            using (var fs = new FileStream(dumpFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                var process = Process.GetCurrentProcess();

                bool result = MiniDumpWriteDump(
                    process.Handle,
                    (uint)process.Id,
                    fs.SafeFileHandle,
                    dumpType,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    IntPtr.Zero);

                if (!result)
                    throw new InvalidOperationException($"Erro ao gerar dump: {Marshal.GetLastWin32Error()}");
            }
        }
    }
}
