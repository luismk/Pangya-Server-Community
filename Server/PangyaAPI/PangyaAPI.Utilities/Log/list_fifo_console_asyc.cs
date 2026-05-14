using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace PangyaAPI.Utilities.Log
{
    public class list_fifo_console_asyc<T> : list_fifo_asyc<T> where T : class
    {
        private readonly object cs_console = new object();

        private StreamWriter log_time;
        private StreamWriter log;
        private StreamWriter log_io_data;

#if DEBUG
        private StreamWriter log_test;
#endif

        private DateTime day_time;

        private string prex = "";
        private string dir = "Log";

        public list_fifo_console_asyc() : base()
        {
            // não inicia log ainda, inicia depois
        }

        ~list_fifo_console_asyc()
        {
            close_log_files();
        }

        public override void push(T deque)
        {
            base.push(deque);
            console_log();//aqui é bem mais rapido
            //sempre que for chamar o push, ja chamo console log
            //assim nao travo a thread primaria
        }

        public void push(string deque, type_msg type, ConsoleColor consoleColor = default)
        {
            base.push(new message(deque, type, consoleColor) as T);
            console_log();//aqui é bem mais rapido
            //sempre que for chamar o push, ja chamo console log
            //assim nao travo a thread primaria
        }

        public void push(string deque, type_msg type)
        {
            base.push(new message(deque, type) as T);
            console_log();//aqui é bem mais rapido
            //sempre que for chamar o push, ja chamo console log
            //assim nao travo a thread primaria
        }

        public void console_log(int millisecondsTimeout = 1000)
        {
            var m = base.get(millisecondsTimeout);

            if (m == null)
                return;

            try
            {
                lock (cs_console)
                {
                    Console.ResetColor();

                    int tipo = get_tipo(m);

                    if (tipo == CL_ONLY_FILE_TIME_LOG || tipo == CL_FILE_TIME_LOG_AND_CONSOLE)
                    {
                        if (log_time == null || log_time.BaseStream == null)
                            init_log_files();

                        if (log_time != null)
                            log_time.WriteLine(get(m));
                    }

                    if (tipo == CL_ONLY_FILE_LOG_IO_DATA || tipo == CL_FILE_LOG_IO_DATA_AND_CONSOLE)
                    {
                        if (log_io_data == null || log_io_data.BaseStream == null)
                            init_log_files();

                        if (log_io_data != null)
                            log_io_data.WriteLine(get(m));
                    }

                    if (tipo == CL_FILE_LOG_AND_CONSOLE || tipo == CL_ONLY_FILE_LOG)
                    {
                        if (log == null || log.BaseStream == null)
                            init_log_files();

                        if (log != null)
                            log.WriteLine(get(m));
                    }

#if DEBUG
                    if (tipo == CL_FILE_LOG_TEST_AND_CONSOLE || tipo == CL_ONLY_FILE_LOG_TEST)
                    {
                        if (log_test == null || log_test.BaseStream == null)
                            init_log_files();

                        if (log_test != null)
                            log_test.WriteLine(get(m));
                    }
#endif

                    if (tipo == CL_ONLY_CONSOLE || tipo == CL_FILE_LOG_AND_CONSOLE || tipo == CL_FILE_TIME_LOG_AND_CONSOLE
                        || tipo == CL_FILE_LOG_IO_DATA_AND_CONSOLE || tipo == CL_FILE_LOG_TEST_AND_CONSOLE)
                    {
                      
                        var msg = get(m);
                       

                        Console.ForegroundColor = getConsoleColor(msg);


                        Console.WriteLine(msg);
                    }
                    else if (tipo == CL_ONLY_CONSOLE_DEBUG)
                        Debug.WriteLine(get(m));
                    Console.ResetColor();
                }

                if (m is IDisposable disp)
                    disp.Dispose();

            }
            catch (Exception e)
            {
                try
                {
                    lock (cs_console)
                    {
                        Console.WriteLine(format_date_local() + $"\t[{nameof(list_fifo_console_asyc<T>)}::console_log][ErrorSystem] {e.Message}");
                    }
                }
                catch { }
            }
        }

        private void init_log_files()
        {
            lock (cs_console)
            {
                try
                {
                    string iniDir = read_ini_log_dir();
                    if (!(iniDir.Length == 0)) 
                    {
                        if (!Directory.Exists(iniDir))
                        {
                            try
                            {
                                Directory.CreateDirectory(iniDir);
                            }
                            catch (Exception)
                            {
                                throw new exception($"[{nameof(list_fifo_console_asyc<T>)}::init_log_files] Cannot create directory '{iniDir}'");
                            }
                        }
                        dir = iniDir;
                    }
                    else
                    {
                        dir = "Log";
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(format_date_local() + $"\t[{nameof(list_fifo_console_asyc<T>)}::init_log_files][ErrorSystem] {e.Message}");
                    Console.WriteLine(format_date_local() + $"\t[{nameof(list_fifo_console_asyc<T>)}::init_log_files][Log] Using default directory 'Log'.");
                    dir = "Log";

                    try
                    {
                        if (!Directory.Exists(dir))
                            Directory.CreateDirectory(dir);
                    }
                    catch
                    {
                        Console.WriteLine(format_date_local() + $"\t[{nameof(list_fifo_console_asyc<T>)}::init_log_files][Error] Cannot create default log directory '{dir}'.");
                    }
                }

                close_log_files();

                day_time = DateTime.Now;

                string datetime = day_time.ToString("ddMMyyyyHHmmss");

                if (!string.IsNullOrEmpty(prex))
                    datetime += " " + prex;

                try
                {
                    string fullPath;

                    fullPath = Path.Combine(dir, "log_time " + datetime + ".log");
                    if (log_time == null)
                        log_time = new StreamWriter(new FileStream(fullPath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite));

                    fullPath = Path.Combine(dir, "log " + datetime + ".log");
                    if (log == null)
                        log = new StreamWriter(new FileStream(fullPath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite));

                    fullPath = Path.Combine(dir, "log_io_data " + datetime + ".log");
                    if (log_io_data == null)
                        log_io_data = new StreamWriter(new FileStream(fullPath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite));

#if DEBUG
                    fullPath = Path.Combine(dir, "log_test " + datetime + ".log");
                    if (log_test == null)
                        log_test = new StreamWriter(new FileStream(fullPath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite));
                    log_test.AutoFlush = true;
#endif
                    log_time.AutoFlush = true;
                    log_io_data.AutoFlush = true;
                    log.AutoFlush = true;

                }
                catch (Exception e)
                {
                    Console.WriteLine(format_date_local() + $"\t[{nameof(list_fifo_console_asyc<T>)}::init_log_files][Error] Cannot open log file. Error: {e.Message}");
                }
            }
        }

        public void close_log_files()
        {
            lock (cs_console)
            {
                try
                {
                    log_time?.Flush();
                    log_time?.Close();
                    log_time = null;

                    log?.Flush();
                    log?.Close();
                    log = null;

                    log_io_data?.Flush();
                    log_io_data?.Close();
                    log_io_data = null;

#if DEBUG
                    log_test?.Flush();
                    log_test?.Close();
                    log_test = null;
#endif
                }
                catch (Exception e)
                {
                    Console.WriteLine(format_date_local() + $"\t[{nameof(list_fifo_console_asyc<T>)}::close_log_files][Error] {e.Message}");
                }
            }
        }

        public bool check_update_day_log()
        {
            var now = DateTime.Now;

            if (day_time.Year < now.Year || day_time.Month < now.Month || day_time.Day < now.Day)
            {
                reload_log_files();
                return true;
            }

            return false;
        }

        public void reload_log_files()
        {
            init_log_files();
        }

        public void set_prex(string prefix)
        {
            prex = prefix;
        }

        #region Helpers and placeholders

        private string format_date_local()
        {
            return DateTime.Now.ToString("[yyyy-MM-dd HH:mm:ss]");
        }

        private string read_ini_log_dir()
        {

            try
            {
                var reader_ini = new IniHandle("Server.ini");

                return reader_ini.ReadString("LOG", "DIR", "Log");
            }
            catch
            {
                return "Log";   
            }
        }

        private int get_tipo(T m)
        {
            dynamic dyn = m;
            return dyn.getTipo();
        }

        private string get(T m)
        {
            dynamic dyn = m;
            return dyn.get();
        }

        private ConsoleColor getConsoleColor(string msg)
        {
            var colorMap = new Dictionary<string, ConsoleColor>(StringComparer.OrdinalIgnoreCase)
            {
                ["[Error]"] = ConsoleColor.Red,
                ["[ErrorSystem]"] = ConsoleColor.Red,
                ["[Warning]"] = ConsoleColor.Yellow,
                ["[Log]"] = ConsoleColor.Gray,
                ["[Sucess]"] = ConsoleColor.Green,
                ["[Info]"] = ConsoleColor.Gray,
                ["[Debug]"] = ConsoleColor.Cyan
            }; 

            foreach (var kvp in colorMap)
            {
                if (msg.Contains(kvp.Key))
                    return kvp.Value;
            }

            return ConsoleColor.Gray;
        }

        public void FlushAll()
        {
            lock (cs_console)
            {
                // Força a escrita de tudo que está nos buffers para o disco
                log_time?.Flush();
                log?.Flush();
                log_io_data?.Flush();
#if DEBUG
                log_test?.Flush();
#endif
            }
        }

        public void LogEmergency(string message, string context = "CRASH")
        {
            try
            {
                // Define o caminho (usa o diretório configurado ou o padrão)
                string emergencyPath = Path.Combine(dir ?? "Log", $"EMERGENCY_{DateTime.Now:ddMMyyyy}.log");

                string fullMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{context}] {message}{Environment.NewLine}";

                // O File.AppendAllText abre, escreve e fecha o arquivo imediatamente.
                // Isso é lento para o dia a dia, mas INFALÍVEL para erros fatais.
                File.AppendAllText(emergencyPath, fullMessage);

                // Também tenta mandar para o console caso ele ainda responda
                Console.ForegroundColor = ConsoleColor.White;
                Console.BackgroundColor = ConsoleColor.Red;
                Console.WriteLine("\n!!! FATAL ERROR WRITTEN TO EMERGENCY LOG !!!");
                Console.ResetColor();
            }
            catch
            {
                // Se até isso falhar (ex: falta de permissão de disco), 
                // não há mais o que fazer a nível de software.
            }
        }

        #endregion

        #region Constantes do tipo (substitua pelos valores reais)
        private const int CL_ONLY_FILE_TIME_LOG = (int)type_msg.CL_ONLY_FILE_TIME_LOG;
        private const int CL_FILE_TIME_LOG_AND_CONSOLE = (int)type_msg.CL_FILE_TIME_LOG_AND_CONSOLE;
        private const int CL_ONLY_FILE_LOG_IO_DATA = (int)type_msg.CL_ONLY_FILE_LOG_IO_DATA;
        private const int CL_FILE_LOG_IO_DATA_AND_CONSOLE = (int)type_msg.CL_FILE_LOG_IO_DATA_AND_CONSOLE;
        private const int CL_FILE_LOG_AND_CONSOLE = (int)type_msg.CL_FILE_LOG_AND_CONSOLE;
        private const int CL_ONLY_FILE_LOG = (int)type_msg.CL_ONLY_FILE_LOG;
        private const int CL_FILE_LOG_TEST_AND_CONSOLE = (int)type_msg.CL_FILE_LOG_TEST_AND_CONSOLE;
        private const int CL_ONLY_FILE_LOG_TEST = (int)type_msg.CL_ONLY_FILE_LOG_TEST;
        private const int CL_ONLY_CONSOLE = (int)type_msg.CL_ONLY_CONSOLE;
        private const int CL_ONLY_CONSOLE_DEBUG = (int)type_msg.CL_ONLY_CONSOLE_DEBUG;
        #endregion
    }

}
