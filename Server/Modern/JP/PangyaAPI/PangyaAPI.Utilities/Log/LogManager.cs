using System;
using System.Collections.Generic;
using System.Threading;
namespace PangyaAPI.Utilities.Log
{
    /// <summary>
    /// nao uso mais
    /// </summary>
    public class message_pool
    {
        private readonly LinkedList<AppMessage> m_messages;
        private readonly object _lockMessages = new object();
        private readonly object _lockConsole = new object();
        private readonly AutoResetEvent _messageEvent;

        public message_pool()
        {
            m_messages = new LinkedList<AppMessage>();
            _messageEvent = new AutoResetEvent(false);
        }

        public void init()
        {
            // Inicialização caso precise (no C++ inicializa mutex/cond var)
        }

        public void destroy()
        {
            // Cleanup, liberando mensagens
            lock (_lockMessages)
            {
                foreach (var m in m_messages)
                    m.Dispose();
                m_messages.Clear();
            }
            _messageEvent.Dispose();
        }

        public void console_log()
        {
            lock (_lockConsole)
            {
                var m = getMessage();
                if (m != null)
                {
                    Console.WriteLine(m.get());
                }
            }
        }

        // Adiciona mensagem ao fim da fila
        public void push(AppMessage m)
        {
            if (m == null) return;

            lock (_lockMessages)
            {
                m_messages.AddLast(m);
                _messageEvent.Set(); // Sinaliza que nova mensagem chegou
            }
        }

        // Adiciona mensagem na frente da fila
        public void push_front(AppMessage m)
        {
            if (m == null) return;

            lock (_lockMessages)
            {
                m_messages.AddFirst(m);
                _messageEvent.Set();
            }
        }

        // Adiciona mensagem ao final (mesmo que push)
        public void push_back(AppMessage m)
        {
            push(m);
        }

        // Retira e retorna a primeira mensagem da fila (consumir)
        public AppMessage getMessage()
        {
            lock (_lockMessages)
            {
                if (m_messages.Count == 0)
                    return null;

                var first = m_messages.First.Value;
                m_messages.RemoveFirst();
                return first;
            }
        }

        // Retorna a primeira mensagem, removendo da fila
        public AppMessage getFirstMessage() => getMessage();

        // Retorna a última mensagem, removendo da fila
        public AppMessage getLastMessage()
        {
            lock (_lockMessages)
            {
                if (m_messages.Count == 0)
                    return null;

                var last = m_messages.Last.Value;
                m_messages.RemoveLast();
                return last;
            }
        }

        // Apenas espiar a primeira mensagem (não remove)
        public AppMessage peekMessage() => peekFirstMessage();

        public AppMessage peekFirstMessage()
        {
            lock (_lockMessages)
            {
                return m_messages.Count > 0 ? m_messages.First.Value : null;
            }
        }

        // Apenas espiar a última mensagem (não remove)
        public AppMessage peekLastMessage()
        {
            lock (_lockMessages)
            {
                return m_messages.Count > 0 ? m_messages.Last.Value : null;
            }
        }

        public void Dispose()
        {
            destroy();
        }
    }
}
////refiz
namespace _smp
{
    using PangyaAPI.Utilities;
    using PangyaAPI.Utilities.Log;
    /// <summary>
    /// ficou bem mais refatorado
    /// mais rapido e mais economico
    /// </summary>
    public class LogManager : Singleton<list_fifo_console_asyc<AppMessage>>
    {
    }
}
