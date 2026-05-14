using System;
namespace PangyaAPI.Utilities.Log
{
    public enum type_msg : int
    {
        CL_ONLY_CONSOLE,
        CL_FILE_TIME_LOG_AND_CONSOLE,
        CL_FILE_LOG_AND_CONSOLE,
        CL_ONLY_FILE_LOG,
        CL_ONLY_FILE_TIME_LOG,
        CL_ONLY_FILE_LOG_IO_DATA,
        CL_FILE_LOG_IO_DATA_AND_CONSOLE,
        CL_ONLY_FILE_LOG_TEST,
        CL_FILE_LOG_TEST_AND_CONSOLE,
        CL_ONLY_CONSOLE_DEBUG
    }

    public class message : IDisposable
    {
        private string m_message;
        private ConsoleColor m_console_color;
        private int m_tipo;
        private bool disposedValue;

        public message()
        {
            m_message = string.Empty;
            m_tipo = 0;
        }

        public message(string s, int _tipo = 0)
        {
            m_message = s;
            m_tipo = _tipo;
            var time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            m_message = "[" + time + "]" + " " + m_message;
            m_console_color = ConsoleColor.Gray;//padrao
        }

        public message(string s, type_msg _tipo = type_msg.CL_ONLY_CONSOLE)
        {
            m_message = s;
            m_tipo = (int)_tipo;
            var time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            m_message = "[" + time + "]" + " " + m_message;
            m_console_color = ConsoleColor.Gray;//padrao
        }

        public message(string s, type_msg _tipo = type_msg.CL_ONLY_CONSOLE, ConsoleColor consoleColor = ConsoleColor.Gray)
        {
            m_message = s;
            m_tipo = (int)_tipo;
            var time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            m_message = "[" + time + "]" + " " + m_message;
            m_console_color = consoleColor;//padrao
        }

        public void append(string s)
        {
            m_message += s;
        }

        public void set(string s)
        {
            m_message = s;
        }

        public string get()
        {
            return m_message;
        }

        public int getTipo()
        {
            return m_tipo;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    m_message = "";
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Não altere este código. Coloque o código de limpeza no método 'Dispose(bool disposing)'
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public ConsoleColor getConsoleColor()
        {
            return m_console_color;
        }
    }
}
