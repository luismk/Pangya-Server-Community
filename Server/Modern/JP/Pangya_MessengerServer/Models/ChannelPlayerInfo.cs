using PangyaAPI.Network;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Models;
using System.Runtime.InteropServices;
namespace Pangya_MessengerServer.Models
{
    public class ChannelPlayerInfo
    {
        public ChannelPlayerInfo()
        {
            clear();
        }

        public void clear()
        {
            room = new Room();
            server_uid = uint.MaxValue;
            id = byte.MaxValue;
            sname = new byte[64];
        }

        public class Room
        {
            public Room()
            { number = -1; }
            public short number;
            public int type;

            public byte[] ToArray()
            {
                using (var p = new Packet())
                {
                    p.WriteInt16(number);
                    p.WriteInt32(type);
                    return p.GetBytes;
                }
            }
        }

        public Room room;
        public uint server_uid;
        public byte id;
        public byte[] sname;
        public string name { get => sname.GetString(); set => sname.SetString(value); }

        public byte[] ToArray()
        {
            using (var p = new Packet())
            {
                p.WriteBytes(room.ToArray());//info room 
                p.WriteUInt32(server_uid);//server conected
                p.WriteByte(id);//channel Login
                p.WriteString(name, 64);//channel name
                return p.GetBytes;
            }
        }

        public ChannelPlayerInfo ToRead(Packet _packet)
        {
            try
            {
                room.number = _packet.ReadInt16();
                room.type = _packet.ReadInt32();
                server_uid = _packet.ReadUInt32();
                id = _packet.ReadByte();
                sname = _packet.ReadBytes(64);
                return this;
            }
            catch (Exception e)
            {
                throw e;
            }
        }
    }

}
