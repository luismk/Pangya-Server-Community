using System;
using PangyaAPI.Utilities;
namespace Pangya_GameServer.Models
{
    public class stRangeTime : IDisposable
    {
        public stRangeTime(uint _ul = 0)
        {
            m_channel_id = 255;
            m_sended_message = false;
            m_room_created = false;
            m_room_closed = false;
        }

        public stRangeTime(ushort _hour_start, ushort _min_start, ushort _sec_start,
                           ushort _hour_end, ushort _min_end, ushort _sec_end,
                           byte _channel_id)
        {
            m_start = new TimeSpan(_hour_start, _min_start, _sec_start, 0);
            m_end = new TimeSpan(_hour_end, _min_end, _sec_end, 0);
            m_channel_id = _channel_id;
            m_sended_message = false;
            m_room_created = false;
            m_room_closed = false;
        }

        public stRangeTime(TimeSpan _start, TimeSpan _end, byte _channel_id)
        {
            m_start = _start;
            m_end = _end;
            m_channel_id = _channel_id;
            m_sended_message = false;
            m_room_created = false;
            m_room_closed = false;
        }

        public void Dispose() => clear();

        public void clear()
        {
            m_start = new TimeSpan();
            m_end = new TimeSpan();
            m_channel_id = 255;

            m_sended_message = false;
            m_room_created = false;
            m_room_closed = false;
        }

        public bool isBetweenTime(TimeSpan _st)
        { 
            return intoStartTime(_st) && intoEndTime(_st);
        }

        public bool isBetweenTime(ushort _hour, ushort _min, ushort _sec, ushort _milli = 0)
        {
            TimeSpan st = new TimeSpan(_hour, _min, _sec, _milli);
            return isBetweenTime(st);
        }

        public bool isPastEnd(TimeSpan _st)
        { 
            return timeToMilliseconds(_st) > timeToMilliseconds(m_end);
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

        public TimeSpan m_start = new TimeSpan();
        public TimeSpan m_end = new TimeSpan();
        public byte m_channel_id;

        public bool m_sended_message;
        public bool m_room_created;
        public bool m_room_closed;
    }

    // Reward
    public class stReward
    {
        public stReward(uint _ul = 0)
        {
            this._typeid = 0;
            this.qntd = 0;
            this.qntd_time = 0;
            this.rate = 100;
        }
        public stReward(uint __typeid,
            uint _qntd,
            uint _qntd_time,
            uint _rate = 100)
        {
            this._typeid = __typeid;
            this.qntd = _qntd;
            this.qntd_time = _qntd_time;
            this.rate = _rate;
        }
         
        public override string ToString()
        {
            return "TYPEID=" + Convert.ToString(_typeid) + ", QNTD=" + Convert.ToString(qntd) + ", QNTD_TIME=" + Convert.ToString(qntd_time) + ", RATE=" + Convert.ToString(rate);
        }

        public uint _typeid = new uint();
        public uint qntd = new uint();
        public uint qntd_time = new uint();
        public uint rate = new uint();
    }
}
