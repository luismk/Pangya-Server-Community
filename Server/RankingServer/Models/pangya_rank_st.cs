using Pangya_RankingServer.PangyaEnums;
using PangyaAPI.Network.Models;
using PangyaAPI.Network.PangyaPacket;
using PangyaAPI.Utilities.Models;
using System;
using System.Runtime.InteropServices;

namespace Pangya_RankingServer.Models
{
    public static class EnumOperator
    {
        public static void ENUM_OPERATOR_PLUS_PLUS<T>(ref T element) where T : struct, Enum
        {
            int value = Convert.ToInt32(element); // converte seguro
            value++;
            element = (T)Enum.ToObject(typeof(T), value);
        }

    }
    // PlayerInfo
    public class player_info
    {
        public player_info(uint _ul = 0u)
        {
            clear();
        }
        public void clear()
        {
            block_flag = new BlockFlag();
            id = "";
            nickname = "";
        }

        public void set_info(player_info info)
        {
            if (info == null)
                throw new ArgumentNullException(nameof(info), "O parâmetro 'info' não pode ser nulo.");

            uid = info.uid;
            m_cap = info.m_cap;
            block_flag = info.block_flag != null ? info.block_flag : new BlockFlag();
            level = info.level;
            id = info.id;
            server_uid = info.server_uid; 
            nickname = info.nickname;
        }
        public uint uid;
        public uint m_cap;
        public BlockFlag block_flag = new BlockFlag();
        public uint guild_uid { get; set; }
        public string guild_name { get; set; } = "";
        public uint server_uid { get; set; }
        public ushort level { get; set; }
        public byte sex;
        public string id { get; set; } = "";
        public string nickname { get; set; } = "";
    }
    // Rank Menu Index
    public enum eRANK_MENU : byte
    {
        RM_OVERALL,
        RM_COURSE_RECORDS,
        RM_RECORDS,
        RM_COURSE_RECORDS_NATURAL,
        RM_COURSE_RECORDS_GRAND_PRIX
    }

    // Rank Menu Item
    public enum eRANK_OVERALL : byte
    {
        RO_TOTAL_POINTS,
        RO_TOTAL_SCORE,
        RO_TROPHY_POINTS,
        RO_PANG_EARNED,
        RO_TOTAL_HOLES,
        RO_ACHIEVEMENT_POINTS
    }

    public enum eRANK_COURSE_RECORDS : byte
    {
        RCR_BLUE_LAGOON,
        RCR_BLUE_WATER,
        RCR_SEPIA_WIND,
        RCR_WIND_HILL,
        RCR_WIZ_WIZ,
        RCR_WEST_WIZ,
        RCR_BLUE_MOON,
        RCR_SILVIA_CANNON,
        RCR_ICE_CANNON,
        RCR_WHITE_WIZ,
        RCR_SHINNING_SAND,
        RCR_PINK_WIND,
        RCR_DEEP_INFERNO,
        RCR_ICE_SPA,
        RCR_LOST_SEAWAY,
        RCR_EASTERN_VALLEY,
        RCR_ICE_INFERNO,
        RCR_WIZ_CITY,
        RCR_ABBOT_MINE,
        RCR_MYSTIC_RUINS
    }

    public enum eRANK_RECORDS : byte
    {
        RR_ALBATROSS,
        RR_HOLE_IN_ONE,
        RR_LEVEL = 3,
        RR_TOTAL_DISTANCE
    }

    public enum ePLAYER_POSITION_RANK_TYPE : byte
    {
        PPRT_IN_TOP_RANK, // Tem registro e est� no top rank
        PPRT_NOT_RANK, // N�o tem registro
        PPRT_NOT_TOP_RANK // Tem registro mas n�o est� no top rank
    }

    // Rank Pesquisa dados
    public class search_dados : IDisposable
    {
        public search_dados(uint _ul = 0u)
        {
            this.rank_menu = eRANK_MENU.RM_OVERALL;
            this.rank_menu_item = (byte)eRANK_OVERALL.RO_TOTAL_POINTS;
            this.term_s5_type = 0;
            this.class_type = 0;
            this.page = 0u;
        }
        public void Dispose()
        {
            clear();
        }
        public void clear()
        {
        }
        public string toString()
        {
            return "RANK_MENU: " + Convert.ToString((ushort)rank_menu) + "\nRANK_MENU_ITEM: " + Convert.ToString((ushort)rank_menu_item) + "\nPAGE: " + Convert.ToString(page) + "\nTERM_S5_TYPE: " + Convert.ToString(term_s5_type) + "\nCLASS_TYPE: " + Convert.ToString(class_type);
        }

        public eRANK_MENU rank_menu { get; set; }
        public byte rank_menu_item { get; set; }
        public byte term_s5_type { get; set; } // Op��es descontinuadas no Fresh UP!, por�m ele ainda mant�m nos packet
        public byte class_type { get; set; } // Op��es descontinuadas no Fresh UP!, por�m ele ainda mant�m nos packet
        public uint page;

        public void ToRead(packet _packet)
        {
            rank_menu = (eRANK_MENU)_packet.ReadByte();
            rank_menu_item = _packet.ReadByte();
            term_s5_type = _packet.ReadByte();
            class_type = _packet.ReadByte();
            page = _packet.ReadUInt32();  
        }
    }

    // Rank Pesquisa dados Ex
    public class search_dados_ex : search_dados
    {
        public search_dados_ex(uint _ul = 0u) : base(_ul)
        {
            this.active = 0;
        }
        public new void Dispose()
        {
            clear();
            base.Dispose();
        }
        public new void clear()
        {
            active = 0;
        }
        public override string ToString()
        {
            return base.toString() + "\nACTIVE: " + Convert.ToString((ushort)active);
        }

        public byte active { get; set; }
    }

    public class key_menu : IEquatable<key_menu>, IComparable<key_menu>
    {
        public eRANK_MENU m_menu { get; set; }
        public byte m_item { get; set; }

        public key_menu(uint _ul = 0u)
        {
            m_menu = eRANK_MENU.RM_OVERALL;
            m_item = 0;
        }

        public key_menu(eRANK_MENU _menu, byte _item)
        {
            m_menu = _menu;
            m_item = _item;
        }

        public void clear()
        {
            m_menu = eRANK_MENU.RM_OVERALL;
            m_item = 0;
        }

        public bool Equals(key_menu other)
        {
            if (ReferenceEquals(other, null)) return false;
            if (ReferenceEquals(this, other)) return true;

            return m_menu == other.m_menu && m_item == other.m_item;
        }

        public override bool Equals(object obj) => Equals(obj as key_menu);

        public override int GetHashCode()
        {
            unchecked // permite overflow sem exception
            {
                int hash = 17;
                hash = hash * 31 + m_menu.GetHashCode();
                hash = hash * 31 + m_item.GetHashCode();
                return hash;
            }
        }


        public int CompareTo(key_menu other)
        {
            if (ReferenceEquals(other, null)) return 1;

            int menuComp = m_menu.CompareTo(other.m_menu);
            return menuComp != 0 ? menuComp : m_item.CompareTo(other.m_item);
        }

        public static bool operator ==(key_menu left, key_menu right)
        {
            if (ReferenceEquals(left, right)) return true;
            if (ReferenceEquals(left, null) || ReferenceEquals(right, null)) return false;

            return left.Equals(right);
        }

        public static bool operator !=(key_menu left, key_menu right) => !(left == right);

        public static bool operator <(key_menu left, key_menu right) => left.CompareTo(right) < 0;

        public static bool operator >(key_menu left, key_menu right) => left.CompareTo(right) > 0;
    }

    public class key_position : IEquatable<key_position>, IComparable<key_position>
    {
        public uint m_uid { get; set; }
        public uint m_position { get; set; }

        public key_position(uint _ul = 0u)
        {
            m_uid = 0u;
            m_position = 0u;
        }

        public key_position(uint _uid, uint _position)
        {
            m_uid = _uid;
            m_position = _position;
        }

        public void clear()
        {
            m_uid = 0u;
            m_position = 0u;
        }

        public bool Equals(key_position other)
        {
            if (ReferenceEquals(other, null)) return false;
            if (ReferenceEquals(this, other)) return true;

            return m_position == other.m_position && m_uid == other.m_uid;
        }

        public override bool Equals(object obj) => Equals(obj as key_position);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + m_uid.GetHashCode();
                hash = hash * 31 + m_position.GetHashCode();
                return hash;
            }
        }


        public int CompareTo(key_position other)
        {
            if (ReferenceEquals(other, null)) return 1;

            int posComp = m_position.CompareTo(other.m_position);
            return posComp != 0 ? posComp : m_uid.CompareTo(other.m_uid);
        }

        public static bool operator ==(key_position left, key_position right)
        {
            if (ReferenceEquals(left, right)) return true;
            if (ReferenceEquals(left, null) || ReferenceEquals(right, null)) return false;

            return left.Equals(right);
        }

        public static bool operator !=(key_position left, key_position right) => !(left == right);

        public static bool operator <(key_position left, key_position right) => left.CompareTo(right) < 0;

        public static bool operator >(key_position left, key_position right) => left.CompareTo(right) > 0;
    }

}
