using Pangya_GameServer.Game.Manager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Pangya_GameServer.Models
{

    public class RoomGrandPrixInstanciaCtx
    {
        public enum eSTATE : byte
        {
            GOOD,
            DESTROYING,
            DESTROYED
        }

        public RoomGrandPrixInstanciaCtx(RoomGrandPrix _rgp, eSTATE _state)
        {
            this.m_rgp = _rgp;
            this.m_state = _state;
        }

        public RoomGrandPrix m_rgp;
        public eSTATE m_state = new eSTATE();
    }

    public enum eSTATE_ROOM_GRAND_ZODIAC_EVENT_SYNC : byte
    {
        WAIT_TIME_START,
        WAIT_10_SECONDS_START,
        WAIT_END_GAME
    }

    public class stStateRoomGrandZodiacEventSync
    {
        public object m_lock = new object();
        public stStateRoomGrandZodiacEventSync()
        {
            this.m_state = eSTATE_ROOM_GRAND_ZODIAC_EVENT_SYNC.WAIT_TIME_START;
        }

        public void @lock()
        {
            Monitor.Enter(m_lock);
        }

        public void unlock()
        {
            Monitor.Exit(m_lock);
        }

        public eSTATE_ROOM_GRAND_ZODIAC_EVENT_SYNC getState()
        {
            return m_state;
        }

        public void setState(eSTATE_ROOM_GRAND_ZODIAC_EVENT_SYNC _state)
        {

            m_state = _state;
        }

        public void setStateWithLock(eSTATE_ROOM_GRAND_ZODIAC_EVENT_SYNC _state)
        {
            m_state = _state;
        }


        protected eSTATE_ROOM_GRAND_ZODIAC_EVENT_SYNC m_state;
    }

    // Static Instance vector strunct
    public class RoomGrandZodiacEventCtx
    {
        public enum eSTATE : byte
        {
            GOOD,
            DESTROYING,
            DESTROYED
        }

        public RoomGrandZodiacEventCtx(RoomGrandZodiacEvent _rbge, eSTATE _state)
        {
            this.m_rbge = _rbge;
            this.m_state = _state;
        }

        public RoomGrandZodiacEvent m_rbge { get; set; }
        public eSTATE m_state { get; set; }
    }
     
    public enum eSTATE_ROOM_BOT_GM_EVENT_SYNC : byte
    {
        WAIT_TIME_START,
        WAIT_10_SECONDS_START,
        WAIT_END_GAME
    }

    public class stStateRoomBotGMEventSync
    {
        public stStateRoomBotGMEventSync()
        {
            this.m_state = eSTATE_ROOM_BOT_GM_EVENT_SYNC.WAIT_TIME_START;
        }

        public void @lock()
        {
            //Monitor.Exit(m_cs);
        }

        public void unlock()
        {
            //Monitor.Exit(m_cs);
        }

        public eSTATE_ROOM_BOT_GM_EVENT_SYNC getState()
        {
            return m_state;
        }

        public void setState(eSTATE_ROOM_BOT_GM_EVENT_SYNC _state)
        {

            m_state = _state;
        }

        public void setStateWithLock(eSTATE_ROOM_BOT_GM_EVENT_SYNC _state)
        {

            @lock();

            m_state = _state;

            unlock();
        }


        protected eSTATE_ROOM_BOT_GM_EVENT_SYNC m_state;

        protected object m_cs = new object();
    }

    // Static Instance vector strunct
    public class RoomBotGMEventInstanciaCtx
    {
        public enum eSTATE : byte
        {
            GOOD,
            DESTROYING,
            DESTROYED
        }

        public RoomBotGMEventInstanciaCtx(RoomBotGMEvent _rbge, eSTATE _state)
        {
            this.m_rbge = _rbge;
            this.m_state = _state;
        }

        public RoomBotGMEvent m_rbge { get; set; }
        public eSTATE m_state { get; set; }
    }

    public class CriticalSectionInstancia
    {
        public CriticalSectionInstancia()
        {
            this.m_state = false;
            this.m_lock = false;

            init();

        }

        public void init()
        {

            if (!m_state)
            {
            }

            m_state = true;
        }

        public void @lock()
        {

            if (!m_state)
            {
                init();
            }

            //Monitor.Exit(m_cs);
            // Est  bloqueado
            m_lock = true;

        }

        public void unlock()
        {

            if (!m_lock)
            {
                return; // N o est  bloqueado
            }

            // Desbloquea
            m_lock = false;

            //Monitor.Exit(m_cs);
        }

        public object m_cs { get; set; } = new object();
        public bool m_state { get; set; }
        public bool m_lock { get; set; }
    }
}
