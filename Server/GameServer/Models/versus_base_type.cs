using System.Threading;

namespace Pangya_GameServer.Models
{
    public enum STATE_VERSUS : byte
    {
        WAIT_HIT_SHOT,
        SHOTING,
        END_SHOT,
        LOAD_HOLE,
        WAIT_END_GAME,
    }

    public class stStateVersus
    {
        private STATE_VERSUS m_state;
        private readonly object m_syncRoot = new object(); // O objeto de sincronização

        public stStateVersus()
        {
            m_state = STATE_VERSUS.WAIT_HIT_SHOT;
        }
         
        public void @lock()
        {
            Monitor.Enter(m_syncRoot);
        }
         
        public void unlock()
        {
            Monitor.Exit(m_syncRoot);
        }

        public STATE_VERSUS getState()
        {
            // Opcional: garantir que a leitura seja thread-safe
            lock (m_syncRoot)
            {
                return m_state;
            }
        }

        public void setState(STATE_VERSUS state)
        {
            lock (m_syncRoot)
            {
                m_state = state;
            }
        }

        // Útil para mudar o estado quando você já tem o lock manual
        public void setStateWithLock(STATE_VERSUS _state)
        {
            m_state = _state;
        }
    }
}
