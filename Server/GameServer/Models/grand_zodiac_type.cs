using System;
using System.Collections.Generic;
using System.Threading;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
namespace Pangya_GameServer.Models
{
    public enum eSTATE_GRAND_ZODIAC_SYNC : byte
    {
        LOAD_HOLE,
        LOAD_CHAR_INTRO,
        FIRST_HOLE,
        START_GOLDEN_BEAM,
        END_GOLDEN_BEAM,
        END_SHOT,
        WAIT_END_GAME,
    }
    public enum eGRAND_ZODIAC_TYPE_SHOT : byte
    {
        GZTS_HIO_SCORE = 1,
        GZTS_FIRST_SHOT,
        GZTS_SPECIAL_SHOT,
        GZTS_WITHOUT_COMMANDS,
        GZTS_MISS_PANGYA
    }

    public class stStateGrandZodiacSync
    {
        public stStateGrandZodiacSync()
        {
            this.m_state = eSTATE_GRAND_ZODIAC_SYNC.FIRST_HOLE;
        }


        public void @lock()
        {
            //Monitor.Exit(m_cs);
        }

        public void unlock()
        {
            //Monitor.Exit(m_cs);
        }

        public eSTATE_GRAND_ZODIAC_SYNC getState()
        {
            return m_state;
        }

        public void setState(eSTATE_GRAND_ZODIAC_SYNC _state)
        {

            m_state = _state;
        }

        public void setStateWithLock(eSTATE_GRAND_ZODIAC_SYNC _state)
        {

            @lock();

            m_state = _state;

            unlock();
        }


        protected eSTATE_GRAND_ZODIAC_SYNC m_state;

        protected object m_cs = new object();
    }
    public class grand_zodiac_dados
    {
        public grand_zodiac_dados(uint _ul = 0)
        {
            this.position = 0;
            this.pontos = 0;
            this.hole_in_one = 0;
            this.jackpot = 0Ul;
            this.total_score = 0;
            this.trofeu = 0;
            this.m_score_shot = new List<eGRAND_ZODIAC_TYPE_SHOT>();
        }

        public void clear()
        {

            position = 0;
            pontos = 0;
            hole_in_one = 0;
            jackpot = 0;
            trofeu = 0;
            total_score = 0;

            if (m_score_shot.Count > 0)
            {
                m_score_shot.Clear();
            }
        }

        public uint position { get; set; }
        public uint pontos { get; set; }
        public uint hole_in_one { get; set; }
        public ulong jackpot { get; set; }
        public uint trofeu { get; set; }
        public int total_score { get; set; }
        public List<eGRAND_ZODIAC_TYPE_SHOT> m_score_shot { get; set; } = new List<eGRAND_ZODIAC_TYPE_SHOT>();
    }

    public class SyncShotGrandZodiac
    {
        public enum eSYNC_SHOT_GRAND_ZODIAC_STATE : byte
        {
            SSGZS_FIRST_SHOT_INIT,
            SSGZS_FIRST_SHOT_SYNC
        }

        public SyncShotGrandZodiac()
        {
            this.first_shot_init = 0;
            this.first_shot_sync = 0;
        }
        public virtual void Dispose()
        {
            clearAllState();
        }

        public void setState(eSYNC_SHOT_GRAND_ZODIAC_STATE _state)
        { // Com Thread Safe

            try
            {
                set_state(_state);

            }
            catch (exception e)
            {
                _smp.message_pool.getInstance().push(new message("[SyncShotGrandZodiac:setState][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public bool checkAllState()
        { // Com Thread Safe

            bool ret = false;

            try
            {
                ret = check_all_state();

            }
            catch (exception e)
            {
                ret = false;

                _smp.message_pool.getInstance().push(new message("[SyncShotGrandZodiac::checkAllState][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            return ret;
        }

        public void clearAllState()
        { // Com Thread Safe

            try
            {
                clear_all_state();

            }
            catch (exception e)
            {
                _smp.message_pool.getInstance().push(new message("[SyncShotGrandZodiac::clearAllState][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public bool setStateAndCheckAllAndClear(eSYNC_SHOT_GRAND_ZODIAC_STATE _state)
        {

            bool ret = false;

            try
            {

                set_state(_state);

                ret = check_all_state();

                if (ret)
                {
                    clear_all_state();
                }
            }
            catch (exception e)
            {

                ret = false;
                _smp.message_pool.getInstance().push(new message("[SyncShotGrandZodiac::setStateAndCheckAllAndClear][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            return ret;
        }

        protected void clear_all_state()
        { // Sem Thread Safe

            first_shot_init = 0;
            first_shot_sync = 0;
        }

        protected void set_state(eSYNC_SHOT_GRAND_ZODIAC_STATE _state)
        { // Sem Thread Safe

            if (_state == eSYNC_SHOT_GRAND_ZODIAC_STATE.SSGZS_FIRST_SHOT_INIT)
            {
                first_shot_init = 1;
            }
            else if (_state == eSYNC_SHOT_GRAND_ZODIAC_STATE.SSGZS_FIRST_SHOT_SYNC)
            {
                first_shot_sync = 1;
            }
        }

        protected bool check_all_state()
        { // Sem Thread Safe
            return first_shot_init > 0 && first_shot_sync > 0;
        }

        protected byte first_shot_init = 1;
        protected byte first_shot_sync = 1;
    }

    // Polimorfirsmo da struct PlayerGameInfo
    public class PlayerGrandZodiacInfo : PlayerGameInfo
    {
        public PlayerGrandZodiacInfo(uint _ul = 0)
        {
            base.clear();
            this.m_gz = new grand_zodiac_dados(0);
            this.init_first_hole_gz = 0;
            this.end_game = 0;
            this.m_sync_shot_gz = new SyncShotGrandZodiac();
            // Clear Base
            base.clear();

            // Clear grand_zodiac_dados
            m_gz.clear();

            init_first_hole_gz = 0;
            end_game = 0;

            m_sync_shot_gz.clearAllState();
        }

        public grand_zodiac_dados m_gz { get; set; } = new grand_zodiac_dados();
        public byte init_first_hole_gz = 0; // inicializou o Primeiro hole do Grand Zodiac
        public byte end_game = 0; // terminou o jogo, enviou o packet12C

        public SyncShotGrandZodiac m_sync_shot_gz { get; set; } = new SyncShotGrandZodiac(); // Sincroniza os dois pacotes de inicializa  o  de tacada do player, j  que eu trato mais de um pacote do mesmo player ao mesmo tempo, a  pode da conflito
    }

    // Usado no Grand Zodiac Event class
    public class range_time : ICloneable
    {
        public enum eTYPE_MAKE_ROOM : byte
        {
            TMR_MAKE_ALL,
            TMR_MAKE_INTERMEDIARE,
            TMR_MAKE_ADVANCED
        }

        public range_time(uint _ul = 0)
        {
            this.m_start = new TimeSpan();
            this.m_end = new TimeSpan();
            this.m_type = eTYPE_MAKE_ROOM.TMR_MAKE_ALL;
            this.m_sended_message = false;
        }
        public range_time(ushort _hour_start,
            ushort _min_start,
            ushort _sec_start,
            ushort _hour_end,
            ushort _min_end,
            ushort _sec_end,
            eTYPE_MAKE_ROOM _type)
        {
            this.m_start = new TimeSpan(_hour_start,
                _min_start, _sec_start, 0);
            this.m_end = new TimeSpan(_hour_end,
                _min_end, _sec_end, 0);
            this.m_type = _type;
            this.m_sended_message = false;
        }
        public range_time(TimeSpan _start,
            TimeSpan _end,
            eTYPE_MAKE_ROOM _type)
        {
            this.m_start = _start;
            this.m_end = _end;
            this.m_type = (_type);
            this.m_sended_message = false;
        }
        public virtual void Dispose()
        {
            clear();
        }
        public void clear()
        {

            m_start = new TimeSpan();
            m_end = new TimeSpan();
            m_sended_message = false;
        }

        public bool isBetweenTime(TimeSpan _st)
        { 
            return intoStartTime(_st) && intoEndTime(_st);
        }

        public bool isBetweenTime(ushort _hour,
            ushort _min, ushort _sec,
            ushort _milli = 0)
        {

            TimeSpan st = new TimeSpan(_hour, _min, _sec,
                _milli);

            return isBetweenTime(st);
        }

        public uint getDiffInterval()
        {
            return timeToMilliseconds(m_end) - timeToMilliseconds(m_start);
        }

        protected bool intoStartTime(TimeSpan _st)
        {
            return timeToMilliseconds(m_start) <= timeToMilliseconds(_st);
        }

        protected bool intoEndTime(TimeSpan _st)
        {
            return timeToMilliseconds(_st) < timeToMilliseconds(m_end);
        }

        protected uint timeToMilliseconds(TimeSpan _st)
        {
            return (uint)((_st.Hours * 60 * 60 * 1000) + (_st.Minutes * 60 * 1000) + (_st.Seconds * 1000) + _st.Milliseconds);
        }

        public bool isPastEnd(TimeSpan _st)
        {  
            return timeToMilliseconds(_st) > timeToMilliseconds(m_end);
        }

        public object Clone()
        {
            return this.MemberwiseClone(); // Faz uma cópia superficial
        }

        public TimeSpan m_start { get; set; } = new TimeSpan();
        public TimeSpan m_end { get; set; } = new TimeSpan();
        public eTYPE_MAKE_ROOM m_type { get; set; } 
        public bool m_sended_message { get; set; } // Flag que guarda se o intervalo j  enviou a mensagem 
    }
}
