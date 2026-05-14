using Pangya_MessengerServer.PangyaEnums;
using PangyaAPI.Network.Models;
using PangyaAPI.Network.PangyaPacket;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Models;
using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Xml.Linq;
using static Pangya_MessengerServer.Models.ManyPacket;

namespace Pangya_MessengerServer.Models
{
    // PlayerInfo
    public class player_info
    {
        public player_info(uint _ul = 0u)
        {
            // O construtor apenas chama o Clear para garantir consistência
            this.clear();
            this.uid = _ul;
        }

        /// <summary>
        /// Reseta todos os dados da classe para o estado inicial (Zera a memória do objeto)
        /// </summary>
        public virtual void clear()
        {
            this.uid = 0;
            this.m_cap = 0;
            this.guild_uid = 0;
            this.server_uid = 0;
            this.level = 0;
            this.sex = 0; 
            // Strings e Objetos
            this.id = string.Empty;
            this.nickname = string.Empty;
            this.guild_name = string.Empty; 
            this.block_flag = new BlockFlag();
        }

        public void set_info(player_info info)
        {
            if (info == null)
            {
                this.clear(); // Se vier nulo, limpamos a instância atual por segurança
                return;
            }

            this.uid = info.uid;
            this.m_cap = info.m_cap;
            this.block_flag = info.block_flag ?? new BlockFlag();
            this.guild_uid = info.guild_uid;
            this.guild_name = info.guild_name ?? string.Empty;
            this.server_uid = info.server_uid;
            this.level = info.level;
            this.sex = info.sex;
            this.id = info.id ?? string.Empty;
            this.nickname = info.nickname ?? string.Empty;
        }

        // Propriedades
        public uint uid;
        public uint m_cap;
        public BlockFlag block_flag;
        public uint guild_uid;
        public string guild_name = string.Empty;
        public uint server_uid;
        public ushort level;
        public byte sex;
        public string id = string.Empty;
        public string nickname = string.Empty;
    }

    public enum GAMETYPE : int
    {
        STROKE,
        MATCH,
        LOUNGE,
        GAME_TYPE,
        TOURNEY,
        TOURNEY_TEAM,
        GUILD_BATTLE,
        PANG_BATTLE,
        GAME_TYPE_08,
        GAME_TYPE_09,//
        APPROCH,
        GRAND_ZODIAC_INT,// GM_EVENT = 0x0B,
        GAME_TYPE_12,
        GRAND_ZODIAC_ADV,
        GRAND_ZODIAC_PRACTICE,
        GAME_TYPE_15,
        GAME_TYPE_16,
        GAME_TYPE_17,
        SPECIAL_SHUFFLE_COURSE,
        PRACTICE,
        GRAND_PRIX,  
        DEFAULT = -1
    }
    // Canal Player Info
    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 75)]
    public class ChannelPlayerInfo
    {
        public ChannelPlayerInfo() => clear();

        public void clear()
        {
            room = new Room();
            server_uid = uint.MaxValue;
            id = byte.MaxValue;
            sname = new byte[64];
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class Room
        {
            public void clear()
            {
                number = ushort.MaxValue;
            }
            public ushort number;
            public int type;

            public byte[] ToArray()
            {
                using (var p = new PangyaBinaryWriter())
                {
                    p.Write(number);
                    p.Write(type); 
                    return p.GetBytes;
                }
            }
        }
        [MarshalAs(UnmanagedType.Struct)]
        public Room room; 
        public uint server_uid;
        public byte id; 
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public byte[] sname; 
        public string name { get => sname.GetString(); set => sname.SetString(value); }

        public byte[] ToArray()
        {
            using (var p = new PangyaBinaryWriter())
            {
                p.WriteBytes(room.ToArray());//info room 
                p.WriteUInt32(server_uid);//server conected
                p.WriteByte(id);//channel id
                p.WriteStr(name, 64);//channel name
                return p.GetBytes;
            }
        }

        public ChannelPlayerInfo ToRead(packet _packet)
        {
            try
            {
                room.number = _packet.ReadUInt16();
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

    // Friend Info
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class FriendInfo
    {
        public FriendInfo(uint _ul = 0u)
        {
            clear();
        }
        public void clear()
        {
            nickname = "";
            apelido = "";
            lUnknown = -1;
            lUnknown2 = 0;
            lUnknown3 = -1;
            lUnknown4 = 0;
            lUnknown5 = 0;
            lUnknown6 = 0;
            lUnknown7 = 0;
        }
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 22)]
        public string nickname = "";
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 11)]
        public string apelido = "";
        public uint uid;
        public int lUnknown;
        public int lUnknown2;
        public int lUnknown3;
        public int lUnknown4;
        public int lUnknown5;
        public int lUnknown6;
        public int lUnknown7; // Esse aqui s� tem no JP, esse valor a+, peguei ele sempre zero, das vezes que vi no pacote        

        public byte[] ToArray()
        {
            using (var p = new PangyaBinaryWriter())
            {
                p.WriteStr(nickname, 22);
                p.WriteStr(apelido, 11);
                p.Write(uid);
                p.Write(lUnknown);
                p.Write(lUnknown2);
                p.Write(lUnknown3);
                p.Write(lUnknown4);
                p.Write(lUnknown5);
                p.Write(lUnknown6);
                p.Write(lUnknown7);
                return p.GetBytes;
            }
        }
    }

    // Friend Info Ex
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class FriendInfoEx : FriendInfo
    {
        public FriendInfoEx(uint _ul = 0u) : base()
        {
            clear();
        }
        public new void clear()
        {

            base.clear();

            cUnknown_flag = 255;
            level = 0;
            flag = new uFlag(); // Flag se o player � amigo ou � membro guild
            state = new uState(); // Sex, online, friend, request, block e etc
            flag.clear();
            state.clear();
        }
        [StructLayout(LayoutKind.Explicit, Size = 1)]
        public struct uState
        {
            [FieldOffset(0)] public byte ucState;

            public void clear() => ucState = 0;

            public PlayerState State
            {
                get => (PlayerState)ucState;
                set => ucState = (byte)value;
            }

            public byte sex
            {
                get => (byte)(State.HasFlag(PlayerState.sex) ? 1 : 0);
                set => State = value != 0 ? State | PlayerState.sex : State & ~PlayerState.sex;
            }

            public byte online
            {
                get => (byte)(State.HasFlag(PlayerState.online) ? 1 : 0);
                set => State = value != 0 ? State | PlayerState.online : State & ~PlayerState.online;
            }

            public byte _friend
            {
                get => (byte)(State.HasFlag(PlayerState._friend) ? 1 : 0);
                set => State = value != 0 ? State | PlayerState._friend : State & ~PlayerState._friend;
            }

            public byte request_friend
            {
                get => (byte)(State.HasFlag(PlayerState.request_friend) ? 1 : 0);
                set => State = value != 0 ? State | PlayerState.request_friend : State & ~PlayerState.request_friend;
            }

            public byte block
            {
                get => (byte)(State.HasFlag(PlayerState.block) ? 1 : 0);
                set => State = value != 0 ? State | PlayerState.block : State & ~PlayerState.block;
            }

            public byte play
            {
                get => (byte)(State.HasFlag(PlayerState.play) ? 1 : 0);
                set => State = value != 0 ? State | PlayerState.play : State & ~PlayerState.play;
            }

            public byte AFK
            {
                get => (byte)(State.HasFlag(PlayerState.AFK) ? 1 : 0);
                set => State = value != 0 ? State | PlayerState.AFK : State & ~PlayerState.AFK;
            }

            public byte busy
            {
                get => (byte)(State.HasFlag(PlayerState.busy) ? 1 : 0);
                set => State = value != 0 ? State | PlayerState.busy : State & ~PlayerState.busy;
            }
        }
        [StructLayout(LayoutKind.Explicit, Size = 1)]
        public struct uFlag
        {
            [FieldOffset(0)] public byte ucFlag;

            public void clear() => ucFlag = 0;

            public byte _friend
            {
                get => (byte)((ucFlag & 0b0000_0001) != 0 ? 1 : 0);
                set => ucFlag = (byte)(value != 0 ? ucFlag | 0b0000_0001 : ucFlag & ~0b0000_0001);
            }

            public byte guild_member
            {
                get => (byte)((ucFlag & 0b0000_0010) != 0 ? 1 : 0);
                set => ucFlag = (byte)(value != 0 ? ucFlag | 0b0000_0010 : ucFlag & ~0b0000_0010);
            }
        }
        public byte cUnknown_flag;
        [field: MarshalAs(UnmanagedType.Struct)]
        public uFlag flag = new uFlag(); // Flag se o player � amigo ou � membro guild
        [field: MarshalAs(UnmanagedType.Struct)]
        public uState state = new uState(); // Sex, online, friend, request, block e etc
        public byte level;
    }

    // Many Packet
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public class ManyPacket
    {
        public ManyPacket(in ushort _size, in ushort _limit)
        {
            this.const_total = _size;
            this.const_limit = _limit;

            // Initialize data
            init();
        }
        public void clear()
        {

        }
        public void init()
        {
            // Calcula Initial data

            paginas = (ushort)(const_total / const_limit);

            if ((const_total % const_limit) != 0)
            {
                ++paginas;
            }

            pag.pagina = 1;
            pag.total = const_total;
            pag.current = (const_total <= const_limit) ? const_total : const_limit;

            // Calcule Index
            calcIndex();
        }
        public void increse()
        {


            try
            {
                if (pag.total > 0)
                {
                    pag.pagina++;

                    if (pag.total <= const_limit)
                    {
                        pag.current = pag.total = 0;
                    }
                    else
                    {
                        pag.total -= const_limit;
                        pag.current = (pag.total <= const_limit) ? (ushort)pag.total : const_limit;
                    }

                    // Cacule Index
                    calcIndex();
                }
            }
            catch (Exception)
            {

                throw;
            }
        }
        [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 5)]
        public class Pagina
        {
            public void clear()
            {

            }
            public byte pagina;
            public ushort total;
            public ushort current;

            public byte[] ToArray()
            {
                using (var p = new PangyaBinaryWriter())
                { 
                    p.Write(pagina);
                    p.Write(total);
                    p.Write(current); 
                    return p.GetBytes;
                }
            }
        }
        [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 4)]
        public class Index
        {
            public void clear()
            {
            }
            public ushort start;
            public ushort end;
            public byte[] ToArray()
            {
                using (var p = new PangyaBinaryWriter())
                {
                    p.Write(start);
                    p.Write(end);
                    return p.GetBytes;
                }
            }
        }
        protected void calcIndex()
        {
            try
            {
                // Calcule Index
                index.start = (ushort)((pag.pagina - 1) * const_limit);
                index.end = (ushort)(index.start + ((pag.total <= const_limit) ? pag.total : const_limit));
            }
            catch (Exception)
            {

                throw;
            }
        }
        protected readonly ushort const_total;
        public readonly ushort const_limit;
        public ushort paginas;
        [field: MarshalAs(UnmanagedType.Struct, SizeConst = 5)]
        public Pagina pag = new Pagina();
        [field: MarshalAs(UnmanagedType.Struct, SizeConst = 4)]
        public Index index = new Index();
    }
}
