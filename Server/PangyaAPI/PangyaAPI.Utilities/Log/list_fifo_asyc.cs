using System;
using System.Collections.Generic;
using System.Threading;

namespace PangyaAPI.Utilities.Log
{
    public class list_fifo_asyc<T> where T : class
    {
        private readonly LinkedList<T> m_deque = new LinkedList<T>();
        private readonly object cs = new object();
        private readonly AutoResetEvent cv = new AutoResetEvent(false);

        public list_fifo_asyc() => init();
        ~list_fifo_asyc() => destroy();

        public void init()
        {
            // Em C# não precisa inicializar mutex nem cv explicitamente
        }

        public void destroy()
        {
            // Em C# geralmente não precisa destruir
        }

        public virtual void push(T item) => push_back(item);

        public void push_front(T item)
        {
            lock (cs)
            {
                m_deque.AddFirst(item);
                cv.Set();
            }
        }

        public void push_back(T item)
        {
            lock (cs)
            {
                m_deque.AddLast(item);
                cv.Set();
            }
        }

        public void remove(T item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            lock (cs)
            {
                m_deque.Remove(item);
            }
        }

        public T get(int millisecondsTimeout = 1000) => getFirst(millisecondsTimeout);

        public T getFirst(int millisecondsTimeout = 1000)
        {
            T item = null;
            var wait = true;

            while (wait)
            {
                lock (cs)
                {
                    if (m_deque.Count > 0)
                    {
                        item = m_deque.First.Value;
                        m_deque.RemoveFirst();
                        wait = false;
                        return item;
                    }
                }
                if (!cv.WaitOne(millisecondsTimeout))
                    return null;
            }

            return item;
        }

        public T getLast(int millisecondsTimeout = 1000)
        {
            T item = null;
            var wait = true;

            while (wait)
            {
                lock (cs)
                {
                    if (m_deque.Count > 0)
                    {
                        item = m_deque.Last.Value;
                        m_deque.RemoveLast();
                        wait = false;
                        return item;
                    }
                }
                if (!cv.WaitOne(millisecondsTimeout))
                    throw new TimeoutException("Erro de timeout em getLast");
            }

            return item;
        }

        public T peek(int millisecondsTimeout = 1000) => peekFirst(millisecondsTimeout);

        public T peekFirst(int millisecondsTimeout = 1000)
        {
            T item = null;
            var wait = true;

            while (wait)
            {
                lock (cs)
                {
                    if (m_deque.Count > 0)
                    {
                        item = m_deque.First.Value;
                        wait = false;
                        return item;
                    }
                }
                if (!cv.WaitOne(millisecondsTimeout))
                    throw new TimeoutException("Erro de timeout em peekFirst");
            }

            return item;
        }

        public T peekLast(int millisecondsTimeout = 1000)
        {
            T item = null;
            var wait = true;

            while (wait)
            {
                lock (cs)
                {
                    if (m_deque.Count > 0)
                    {
                        item = m_deque.Last.Value;
                        wait = false;
                        return item;
                    }
                }
                if (!cv.WaitOne(millisecondsTimeout))
                    throw new TimeoutException("Erro de timeout em peekLast");
            }

            return item;
        }

        public int size()
        {
            lock (cs)
            {
                return m_deque.Count;
            }
        }

        public bool empty()
        {
            lock (cs)
            {
                return m_deque.Count == 0;
            }
        }

        public void clear()
        {
            lock (cs)
            {
                m_deque.Clear();
            }
        }
    }
}
