using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pangya_RankingServer.Models;
using PangyaAPI.Network.PangyaPacket;
using PangyaAPI.Utilities.Models;

namespace Pangya_RankingServer.UTIL
{
    public class RankRegistry : IDisposable
    {
        // Passou de 10 mil o pangya zera no compact packet
        public const uint LIMIT_RANK_POSITION_TO_COMPACT_PACKET_SHOW = 10000u;

        public uint m_uid;
        protected uint m_current_position;
        protected uint m_last_position;
        protected int m_value; // Dados

        public RankRegistry()
        {
            m_uid = 0u;
            m_current_position = 0u;
            m_last_position = 0u;
            m_value = 0;
        }

        public RankRegistry(uint _uid, uint _current_position, uint _last_position, int _value)
        {
            m_uid = _uid;
            m_current_position = _current_position;
            m_last_position = _last_position;
            m_value = _value;
        }

        public void Dispose()
        {
            clear();
        }

        public void clear()
        {
            m_uid = 0u;
            m_current_position = 0u;
            m_last_position = 0u;
            m_value = 0;
        }

        // Fill Packet with data from Object
        public void toPacket(PangyaBinaryWriter _packet)
        {
            _packet.WriteUInt32(m_uid);
            _packet.WriteUInt32(m_current_position);
            _packet.WriteUInt32(m_last_position);
            _packet.WriteInt32(m_value);
        }

        public void toCompactPacket(PangyaBinaryWriter _packet)
        {
            // Se o rank atual passou de 10 mil, zera os 2
            if (m_current_position > LIMIT_RANK_POSITION_TO_COMPACT_PACKET_SHOW)
            {
                _packet.WriteZeroByte(8);
            }
            else
            {
                _packet.WriteUInt32(m_current_position);
                _packet.WriteUInt32(m_last_position);
            }

            _packet.WriteInt32(m_value);
        }

        // Get
        public uint getUID() => m_uid;
        public uint getCurrentPosition() => m_current_position;
        public uint getLastPosition() => m_last_position;
        public int getValue() => m_value;

        // Set
        public void setUID(uint _uid) => m_uid = _uid;
        public void setCurrentPosition(uint _current_position) => m_current_position = _current_position;
        public void setLastPosition(uint _last_position) => m_last_position = _last_position;
        public void setValue(int _value) => m_value = _value;
    }

    // Rank Character Entry 
    public class RankEntryValue : Dictionary<key_position, RankRegistry>
    {
        public RankEntryValue(key_position key, RankRegistry value)
        {
            Add(key, value);
        }

        public RankEntryValue()
        {
        } 
    }

    public class RankEntry : Dictionary<key_menu, RankEntryValue>
    {
        public RankEntry(key_menu key, RankEntryValue value)
        {
            Add(key, value);
        }

        public RankEntry()
        {
        } 
    }

    public class RankEntryValueRange : List<RankRegistry>
    {
        public RankEntryValueRange() { }

        public RankEntryValueRange(IEnumerable<RankRegistry> collection) : base(collection) { }
    }
}
