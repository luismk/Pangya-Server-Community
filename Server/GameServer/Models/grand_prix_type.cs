using System.Collections.Generic;
using System.Linq;
using System.Threading;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Models;

namespace Pangya_GameServer.Models
{

    // Polimorfirsmo da struct PlayerGameInfo
    public class PlayerGrandPrixInfo : PlayerGameInfo
    {
        public PlayerGrandPrixInfo(uint _ul = 0)
        {
            // Clear Base
            base.clear();
            _flag = 0;
        }
        public uint _flag;
    }

    public enum eRULE : uint
    {
        SPECIAL_SHOT = 0x1A000267u,
        TIME_10_SEC = 0x1A000268u,
        TIME_15_SEC = 0x1A00029Eu
    }
    public class Bot
    {
        public enum eTYPE_SCORE : byte
        {
            MIN_SCORE,
            MED_SCORE,
            MAX_SCORE
        }

        public class Hole
        {
            public Hole(uint _ul = 0u)
            {
                clear();
            }
            public Hole(uint _course,
                uint _hole, int _score,
                ulong _pang,
                ulong _bonus_pang)
            {
                this.m_course = _course;
                this.m_hole = _hole;
                this.m_score = _score;
                this.m_pang = _pang;
                this.m_bonus_pang = _bonus_pang;
                this.m_ulUnknown = 0;
                this.m_ullUnknown = 0Ul;
            }
            public void clear()
            {
                m_course = 0;
                m_hole = 0;
                m_score = 0;
                m_ulUnknown = 0;
                m_pang = 0;
                m_bonus_pang = 0;
                m_ullUnknown = 0;
            }
            public uint m_course = new uint();
            public uint m_hole = new uint();
            public int m_score = new int();
            public uint m_ulUnknown = new uint();
            public ulong m_pang = new ulong();
            public ulong m_bonus_pang = new ulong();
            public ulong m_ullUnknown = new ulong();

            public byte[] ToArray()
            {
                using (var p = new PangyaBinaryWriter())
                {
                    p.Write(m_course);
                    p.Write(m_hole);
                    p.Write(m_score);
                    p.Write(m_ulUnknown);
                    p.Write(m_pang);
                    p.Write(m_bonus_pang);
                    p.Write(m_ullUnknown);
                    return p.GetBytes;
                }
            }
        }

        public Bot(uint _ul = 0u)
        {
            clear();
        }
        public void Dispose()
        {
            clear();
        }
        public void clear()
        {
            id = 0;
            qntd_hole = 0;
            pang_total = 0;
            bonus_pang_total = 0;
            record = 0;
            max_record = 0;
            med_shot_per_hole = 0;
            type_score = eTYPE_SCORE.MIN_SCORE;

            pi.clear();

            if (!hole.empty())
            {
                hole.Clear();
            }
        }

        public uint id = new uint();
        public byte qntd_hole;
        public ulong pang_total = new ulong();
        public ulong bonus_pang_total = new ulong();
        public int record = new int();

        public int max_record = new int();
        public int med_shot_per_hole = new int();
        public eTYPE_SCORE type_score = new eTYPE_SCORE();

        // Player Game Info do Bot para usar na hora de classificação do rank
        public PlayerGameInfo pi = new PlayerGameInfo();

        public List<Hole> hole = new List<Hole>();
    }

    // Rank Player Display Character
    public class RankPlayerDisplayChracter
    {
        public RankPlayerDisplayChracter(uint _ul = 0u)
        {
            clear();
        }
        public void clear()
        {
            uid = new uint();
            rank = new uint();
            default_hair = 0;
            default_shirts = 0;
            parts_typeid = new uint[24];
            auxparts = new uint[5];
            parts_id = new uint[24];
        }
        public uint uid = new uint();
        public uint rank = new uint();
        public byte default_hair;
        public byte default_shirts;
        public uint[] parts_typeid = new uint[24];
        public uint[] auxparts = new uint[5];
        public uint[] parts_id = new uint[24];

        public byte[] ToArray()
        {
            using (var p = new PangyaBinaryWriter())
            {
                p.Write(uid);
                p.Write(rank);
                p.Write(default_hair);
                p.Write(default_shirts);

                for (int i = 0; i < 24; i++)
                    p.Write(parts_typeid[i]);

                for (int i = 0; i < 5; i++)
                    p.Write(auxparts[i]);

                for (int i = 0; i < 24; i++)
                    p.Write(parts_id[i]);

                return p.GetBytes;
            }
        }
    }


    // Estrutura que controle o tempo dos player no Grand Prix
    public class TimerManager
    {
        public class timer_ctx
        {
            public timer_ctx(uint _ul = 0u)
            {
                this.m_player = null;
                this.m_timer = null;
            }
            public timer_ctx(Player _player, PangyaSyncTimer _timer)
            {
                this.m_player = _player;
                this.m_timer = _timer;
            }
            public void clear()
            {

                if (m_player != null)
                {
                    m_player = null;
                }

                if (m_timer != null)
                {
                    m_timer = null;
                }
            }
            public Player m_player;
            public PangyaSyncTimer m_timer;
        }

        public TimerManager()
        {
            this.m_timers = new List<timer_ctx>();
            this.m_lock = false;
        }
        public void Dispose()
        {

            clear();

            // Verifica se está bloqueado e libera por que a classe vai ser destruída
            if (m_lock)
            {
                unlock();
            }
        }

        public void clear()
        {

            @lock();

            if (!m_timers.empty())
            {
                m_timers.Clear();
            }

            unlock();
        }

        public timer_ctx insertTimer(Player _player, PangyaSyncTimer _timer)
        {
            return insertTimer(new timer_ctx(_player, _timer));
        }

        public timer_ctx insertTimer(timer_ctx _tc)
        {
            @lock();
            m_timers.Add(_tc);
            unlock();
            return m_timers.Any(predicate: c => c.m_player.getUID() == _tc.m_player.getUID()) ? _tc : null;
        }

        public timer_ctx findTimer(Player _player)
        {

            if (_player == null)
            {
                return null;
            }

            timer_ctx tc = null;

            @lock(); 
            var it = m_timers.FirstOrDefault(_el =>
            {
                return _el.m_player == _player;
            });

            if (it != null)
            {
                tc = (it);
            }

            unlock();

            return tc;
        }

        // lock
        public void @lock()
        {
            //Monitor.Exit(m_cs);
            m_lock = true;
        }

        // unlock
        public void unlock()
        {

            // Não está bloqueado para poder desbloquear
            if (!m_lock)
            {
                return;
            }

            m_lock = false;
            //Monitor.Exit(m_cs);
        }

        public List<timer_ctx> getTimers()
        {
            return m_timers;
        }

        protected List<timer_ctx> m_timers = new List<timer_ctx>();

        protected bool m_lock;
         
    }

    // Player Lock Manager
    public class LockManager
    {
        public class lock_ctx
        {
            public lock_ctx()
            {
                this.m_player = null;
                this.m_lock = false;
            }
            public lock_ctx(Player _player)
            {
                this.m_player = _player;
                this.m_lock = false;
            }
            public void Dispose()
            {

                // Verifica se está bloqueado e libera por que a classe vai ser destruída
                if (m_lock)
                {
                    unlock();
                }
            }

            public void @lock()
            {

                //Monitor.Exit(m_cs);
                m_lock = true;
            }

            public void unlock()
            {

                // Não está bloqueado para desbloquear
                if (!m_lock)
                {
                    return;
                }

                m_lock = false;

                //Monitor.Exit(m_cs);
            }

            public Player m_player;

            protected bool m_lock; 
        }

        public LockManager()
        {
            this.m_lockers = new List<lock_ctx>();
            this.m_lock = false;
        }
        public void Dispose()
        {

            clear();

            // Verifica se está bloqueado e libera por que a classe vai ser destruída
            if (m_lock)
            {
                unlock();
            }
        }

        public void clear()
        {

            @lock();

            if (!m_lockers.empty())
            {
                m_lockers.Clear();
            }

            unlock();

        }

        public void @lock(Player _player)
        {

            @lock();

            var it = findLocker(_player);

            if (it != null)
            {
                it.@lock();
            }
            else
            {

                // O player ainda não tem um locker, cria uma para ele e bloquea
                var lc = insertLock(_player);

                if (lc != null)
                {
                    lc.@lock();
                }
            }

            unlock();
        }

        public void unlock(Player _player)
        {

            @lock();

            var it = findLocker(_player);

            if (it != null)
            {
                it.unlock();
            }
            // eslse não precisa criar um locker para desbloquear um locker que o player não tem

            unlock();
        }

        protected lock_ctx findLocker(Player _player)
        {

            if (_player == null)
            {
                return m_lockers.Last();
            }

            return m_lockers.FirstOrDefault(_el =>
            {
                return _el.m_player == _player;
            });
        }

        protected lock_ctx insertLock(Player _player)
        {

            var it = new lock_ctx(_player);

            m_lockers.Add(it);

            return (it != null ? (it) : null);
        }

        protected void @lock()
        {
            m_lock = true;
        }

        protected void unlock()
        {
            if (!m_lock)
            {
                return;
            }

            m_lock = false;
        }

        protected List<lock_ctx> m_lockers = new List<lock_ctx>();

        protected bool m_lock;
    }

    public enum STATE_TURN : byte
    {
        WAIT_HIT_SHOT,
        SHOTING,
        END_SHOT,
        LOAD_HOLE,
        WAIT_END_GAME
    }

    public class stStateTurn
    {
        public stStateTurn()
        {
            this.m_state = STATE_TURN.WAIT_HIT_SHOT;
            this.m_lock = false;
        }

        public void Dispose()
        {

            m_state = STATE_TURN.WAIT_HIT_SHOT;

            if (m_lock)
            {
                unlock();
            }

        }

        public void @lock()
        {
            //Monitor.Exit(m_cs);
            m_lock = true;
        }

        public void unlock()
        {

            // Não está bloqueado para poder liberar
            if (!m_lock)
            {
                return;
            }

            m_lock = false;

            //Monitor.Exit(m_cs);
        }

        public STATE_TURN getState()
        {
            return m_state;
        }

        public void setState(STATE_TURN _state)
        {
            m_state = _state;
        }

        public void setStateWithLock(STATE_TURN _state)
        {

            @lock();

            m_state = _state;

            unlock();
        }

        protected STATE_TURN m_state = new STATE_TURN();

        protected bool m_lock; 
    }
}
