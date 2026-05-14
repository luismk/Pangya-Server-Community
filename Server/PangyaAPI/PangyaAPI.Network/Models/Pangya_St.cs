using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using PangyaAPI.IFF.JP.Extensions;
using PangyaAPI.IFF.JP.Models.Data;
using PangyaAPI.IFF.JP.Models.Flags;
using PangyaAPI.Network.PangyaPacket;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Models;
using PangyaAPI.Utilities.Log;
using Part = PangyaAPI.IFF.JP.Models.Data.Part;
namespace PangyaAPI.Network.Models
{
    public class Global
    {
        public static readonly uint[] angel_wings = { 134309888u, 134580224u, 134842368u, 135120896u, 135366656u, 135661568u, 135858176u, 136194048u, 136398848u, 136660992u, 137185294u, 137447424u, 138004480u };
        public static readonly uint[] gacha_angel_wings = { 134309903u, 134580239u, 134842383u, 135120911u, 135366671u, 135661583u, 135858191u, 136194063u, 136398863u, 136661007u, 136923153u, 137185284u, 137447436u, 138004492u };
    }
    public class IPBan
    {
        public enum _TYPE : byte
        {
            IP_BLOCK_NORMAL,
            IP_BLOCK_RANGE
        }
        public _TYPE type;
        public uint ip;
        public uint mask;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4, Size = 4)]
    public class uProperty
    {
        public uProperty(uint _ul = 0u)
        {
            ulProperty = _ul;
        }

        [field: MarshalAs(UnmanagedType.U4, SizeConst = 4)]
        public uint ulProperty { get; set; }

        // NORMAL (0) = nenhum bit ativado
        public bool normal => ulProperty == 0;

        public bool special // 1
        {
            get => (ulProperty & (1 << 0)) != 0;
            set => ulProperty = value ? (ulProperty | (1u << 0)) : (ulProperty & ~(1u << 0));
        }

        public bool small_play // 2
        {
            get => (ulProperty & (1 << 1)) != 0;
            set => ulProperty = value ? (ulProperty | (1u << 1)) : (ulProperty & ~(1u << 1));
        }

        public bool ladder // 4
        {
            get => (ulProperty & (1 << 2)) != 0;
            set => ulProperty = value ? (ulProperty | (1u << 2)) : (ulProperty & ~(1u << 2));
        }

        public bool adult // 8
        {
            get => (ulProperty & (1 << 3)) != 0;
            set => ulProperty = value ? (ulProperty | (1u << 3)) : (ulProperty & ~(1u << 3));
        }

        public bool mantle // 16 (também foi chamado de mantle antes)
        {
            get => (ulProperty & (1 << 4)) != 0;
            set => ulProperty = value ? (ulProperty | (1u << 4)) : (ulProperty & ~(1u << 4));
        }

        public bool skins // 32
        {
            get => (ulProperty & (1 << 5)) != 0;
            set => ulProperty = value ? (ulProperty | (1u << 5)) : (ulProperty & ~(1u << 5));
        }

        public bool only_rookie // 64
        {
            get => (ulProperty & (1 << 6)) != 0;
            set => ulProperty = value ? (ulProperty | (1u << 6)) : (ulProperty & ~(1u << 6));
        }

        public bool natural // 128
        {
            get => (ulProperty & (1 << 7)) != 0;
            set => ulProperty = value ? (ulProperty | (1u << 7)) : (ulProperty & ~(1u << 7));
        }

        public bool championship // 256
        {
            get => (ulProperty & (1 << 8)) != 0;
            set => ulProperty = value ? (ulProperty | (1u << 8)) : (ulProperty & ~(1u << 8));
        }

        public bool azul // 512
        {
            get => (ulProperty & (1 << 9)) != 0;
            set => ulProperty = value ? (ulProperty | (1u << 9)) : (ulProperty & ~(1u << 9));
        }

        public bool verde // 1024
        {
            get => (ulProperty & (1 << 10)) != 0;
            set => ulProperty = value ? (ulProperty | (1u << 10)) : (ulProperty & ~(1u << 10));
        }

        public bool grand_prix // 2048
        {
            get => (ulProperty & (1 << 11)) != 0;
            set => ulProperty = value ? (ulProperty | (1u << 11)) : (ulProperty & ~(1u << 11));
        }

        public bool relay // 4096
        {
            get => (ulProperty & (1 << 12)) != 0;
            set => ulProperty = value ? (ulProperty | (1u << 12)) : (ulProperty & ~(1u << 12));
        }

        public bool rookie_beginner_only // 2147483648 (bit 31)
        {
            get => (ulProperty & (1u << 31)) != 0;
            set => ulProperty = value ? (ulProperty | (1u << 31)) : (ulProperty & ~(1u << 31));
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4, Size = 2)]
    public class uEventFlag
    {
        public uEventFlag(ushort ul = 0)
        {
            usEventFlag = ul;
        }

        [field: MarshalAs(UnmanagedType.U2, SizeConst = 2)]
        public ushort usEventFlag { get; set; }
        public bool pang_x_plus
        {
            get => (usEventFlag & (1 << 1)) != 0;
            set => usEventFlag = (ushort)(value ? (usEventFlag | (1 << 1)) : (usEventFlag & ~(1 << 1)));
        }

        public bool exp_x2
        {
            get => (usEventFlag & (1 << 2)) != 0;
            set => usEventFlag = (ushort)(value ? (usEventFlag | (1 << 2)) : (usEventFlag & ~(1 << 2)));
        }

        public bool angel_wing
        {
            get => (usEventFlag & (1 << 3)) != 0;
            set => usEventFlag = (ushort)(value ? (usEventFlag | (1 << 3)) : (usEventFlag & ~(1 << 3)));
        }

        /// <summary>
        /// 3x
        /// </summary>
        public bool exp_x_plus
        {
            get => (usEventFlag & (1 << 4)) != 0;
            set => usEventFlag = (ushort)(value ? (usEventFlag | (1 << 4)) : (usEventFlag & ~(1 << 4)));
        }

        public bool unknown_0
        {
            get => (usEventFlag & (1 << 5)) != 0;
            set => usEventFlag = (ushort)(value ? (usEventFlag | (1 << 5)) : (usEventFlag & ~(1 << 5)));
        }

        public bool unknown_1
        {
            get => (usEventFlag & (1 << 6)) != 0;
            set => usEventFlag = (ushort)(value ? (usEventFlag | (1 << 6)) : (usEventFlag & ~(1 << 6)));
        }

        public bool unknown_2
        {
            get => (usEventFlag & (1 << 8)) != 0;
            set => usEventFlag = (ushort)(value ? (usEventFlag | (1 << 8)) : (usEventFlag & ~(1 << 8)));
        }

        public bool club_mastery_x_plus
        {
            get => (usEventFlag & (1 << 7)) != 0;
            set => usEventFlag = (ushort)(value ? (usEventFlag | (1 << 7)) : (usEventFlag & ~(1 << 7)));
        }


        public bool unknown_3
        {
            get => (usEventFlag & (1 << 9)) != 0;
            set => usEventFlag = (ushort)(value ? (usEventFlag | (1 << 9)) : (usEventFlag & ~(1 << 9)));
        }

        public bool unknown_4
        {
            get => (usEventFlag & (1 << 10)) != 0;
            set => usEventFlag = (ushort)(value ? (usEventFlag | (1 << 10)) : (usEventFlag & ~(1 << 10)));
        }

        public bool unknown_5
        {
            get => (usEventFlag & (1 << 11)) != 0;
            set => usEventFlag = (ushort)(value ? (usEventFlag | (1 << 11)) : (usEventFlag & ~(1 << 11)));
        }

        public bool unknown_6
        {
            get => (usEventFlag & (1 << 12)) != 0;
            set => usEventFlag = (ushort)(value ? (usEventFlag | (1 << 12)) : (usEventFlag & ~(1 << 12)));
        }

        public bool unknown_7
        {
            get => (usEventFlag & (1 << 13)) != 0;
            set => usEventFlag = (ushort)(value ? (usEventFlag | (1 << 13)) : (usEventFlag & ~(1 << 13)));
        }

        public bool unknown_8
        {
            get => (usEventFlag & (1 << 14)) != 0;
            set => usEventFlag = (ushort)(value ? (usEventFlag | (1 << 14)) : (usEventFlag & ~(1 << 14)));
        }

        public bool unknown_9
        {
            get => (usEventFlag & (1 << 15)) != 0;
            set => usEventFlag = (ushort)(value ? (usEventFlag | (1 << 15)) : (usEventFlag & ~(1 << 15)));
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4, Size = 8)]
    public class uFlag
    {
        public uFlag(ulong _ull = 0)
        {
            ullFlag = _ull;
        }

        public ulong ullFlag { get; set; }
        /// <summary>
        /// Flag desconhecida. Representa o valor do bit 0.
        /// </summary>
        public bool Unknown0
        {
            get => (ullFlag & (1UL << 0)) != 0;
            set => ullFlag = value ? (ullFlag | (1UL << 0)) : (ullFlag & ~(1UL << 0));
        }

        /// <summary>
        /// Desabilita a capacidade de jogar qualquer modo de jogo. Representa o valor do bit 1.
        /// </summary>
        public bool all_game
        {
            get => (ullFlag & (1UL << 1)) != 0;
            set => ullFlag = value ? (ullFlag | (1UL << 1)) : (ullFlag & ~(1UL << 1));
        }

        /// <summary>
        /// Impede compras na loja e envio de presentes. Representa o valor do bit 2.
        /// </summary>
        public bool buy_and_gift_shop
        {
            get => (ullFlag & (1UL << 2)) != 0;
            set => ullFlag = value ? (ullFlag | (1UL << 2)) : (ullFlag & ~(1UL << 2));
        }

        /// <summary>
        /// Impede o envio de presentes. Representa o valor do bit 3.
        /// </summary>
        public bool gift_shop
        {
            get => (ullFlag & (1UL << 3)) != 0;
            set => ullFlag = value ? (ullFlag | (1UL << 3)) : (ullFlag & ~(1UL << 3));
        }

        /// <summary>
        /// Impede o acesso ao Papel Shop. Representa o valor do bit 4.
        /// </summary>
        public bool papel_shop
        {
            get => (ullFlag & (1UL << 4)) != 0;
            set => ullFlag = value ? (ullFlag | (1UL << 4)) : (ullFlag & ~(1UL << 4));
        }

        /// <summary>
        /// Impede o acesso ao Personal Shop. Representa o valor do bit 5.
        /// </summary>
        public bool personal_shop
        {
            get => (ullFlag & (1UL << 5)) != 0;
            set => ullFlag = value ? (ullFlag | (1UL << 5)) : (ullFlag & ~(1UL << 5));
        }

        /// <summary>
        /// Impede a participação no modo Stroke. Representa o valor do bit 6.
        /// </summary>
        public bool stroke
        {
            get => (ullFlag & (1UL << 6)) != 0;
            set => ullFlag = value ? (ullFlag | (1UL << 6)) : (ullFlag & ~(1UL << 6));
        }

        /// <summary>
        /// Impede a participação em Match. Representa o valor do bit 7.
        /// </summary>
        public bool match
        {
            get => (ullFlag & (1UL << 7)) != 0;
            set => ullFlag = value ? (ullFlag | (1UL << 7)) : (ullFlag & ~(1UL << 7));
        }

        /// <summary>
        /// Impede a participação em Tourney. Representa o valor do bit 8.
        /// </summary>
        public bool tourney
        {
            get => (ullFlag & (1UL << 8)) != 0;
            set => ullFlag = value ? (ullFlag | (1UL << 8)) : (ullFlag & ~(1UL << 8));
        }

        /// <summary>
        /// Impede a participação em Team Tourney (agora é Short Game). Representa o valor do bit 9.
        /// </summary>
        public bool team_tourney
        {
            get => (ullFlag & (1UL << 9)) != 0;
            set => ullFlag = value ? (ullFlag | (1UL << 9)) : (ullFlag & ~(1UL << 9));
        }

        /// <summary>
        /// Impede a participação em Guild Battle. Representa o valor do bit 10.
        /// </summary>
        public bool guild_battle
        {
            get => (ullFlag & (1UL << 10)) != 0;
            set => ullFlag = value ? (ullFlag | (1UL << 10)) : (ullFlag & ~(1UL << 10));
        }

        /// <summary>
        /// Impede a participação em Pang Battle. Representa o valor do bit 11.
        /// </summary>
        public bool pang_battle
        {
            get => (ullFlag & (1UL << 11)) != 0;
            set => ullFlag = value ? (ullFlag | (1UL << 11)) : (ullFlag & ~(1UL << 11));
        }

        /// <summary>
        /// Impede a participação no modo Approach. Representa o valor do bit 12.
        /// </summary>
        public bool approach
        {
            get => (ullFlag & (1UL << 12)) != 0;
            set => ullFlag = value ? (ullFlag | (1UL << 12)) : (ullFlag & ~(1UL << 12));
        }

        /// <summary>
        /// Impede criar ou entrar em salas de lounge. Representa o valor do bit 13.
        /// </summary>
        public bool lounge
        {
            get => (ullFlag & (1UL << 13)) != 0;
            set => ullFlag = value ? (ullFlag | (1UL << 13)) : (ullFlag & ~(1UL << 13));
        }

        /// <summary>
        /// Impede a participação no Scratchy System. Representa o valor do bit 14.
        /// </summary>
        public bool scratchy
        {
            get => (ullFlag & (1UL << 14)) != 0;
            set => ullFlag = value ? (ullFlag | (1UL << 14)) : (ullFlag & ~(1UL << 14));
        }

        /// <summary>
        /// Flag desconhecida. Representa o valor do bit 15.
        /// </summary>
        public bool Unknown1
        {
            get => (ullFlag & (1UL << 15)) != 0;
            set => ullFlag = value ? (ullFlag | (1UL << 15)) : (ullFlag & ~(1UL << 15));
        }

        /// <summary>
        /// Impede a visualização do rank server. Representa o valor do bit 16.
        /// </summary>
        public bool rank_server
        {
            get => (ullFlag & (1UL << 16)) != 0;
            set => ullFlag = value ? (ullFlag | (1UL << 16)) : (ullFlag & ~(1UL << 16));
        }

        /// <summary>
        /// Impede o envio de ticker. Representa o valor do bit 17.
        /// </summary>
        public bool ticker
        {
            get => (ullFlag & (1UL << 17)) != 0;
            set => ullFlag = value ? (ullFlag | (1UL << 17)) : (ullFlag & ~(1UL << 17));
        }

        /// <summary>
        /// Desabilita a funcionalidade de Mail Box. Representa o valor do bit 18.
        /// </summary>
        public bool mail_box
        {
            get => (ullFlag & (1UL << 18)) != 0;
            set => ullFlag = value ? (ullFlag | (1UL << 18)) : (ullFlag & ~(1UL << 18));
        }

        /// <summary>
        /// Impede o acesso ao Grand Zodiac (provável). Representa o valor do bit 19.
        /// </summary>
        public bool grand_zodiac
        {
            get => (ullFlag & (1UL << 19)) != 0;
            set => ullFlag = value ? (ullFlag | (1UL << 19)) : (ullFlag & ~(1UL << 19));
        }

        /// <summary>
        /// Impede o modo Single Play. Representa o valor do bit 20.
        /// </summary>
        public bool single_play
        {
            get => (ullFlag & (1UL << 20)) != 0;
            set => ullFlag = value ? (ullFlag | (1UL << 20)) : (ullFlag & ~(1UL << 20));
        }

        /// <summary>
        /// Impede o acesso ao Grand Prix. Representa o valor do bit 21.
        /// </summary>
        public bool grand_prix
        {
            get => (ullFlag & (1UL << 21)) != 0;
            set => ullFlag = value ? (ullFlag | (1UL << 21)) : (ullFlag & ~(1UL << 21));
        }

        /// <summary>
        /// Flag desconhecida. Representa os bits 22-23.
        /// </summary>
        public bool Unknown2
        {
            get => (ullFlag & (3UL << 22)) != 0;
            set => ullFlag = value ? (ullFlag | (3UL << 22)) : (ullFlag & ~(3UL << 22));
        }

        /// <summary>
        /// Impede o acesso a Guild. Representa o valor do bit 24.
        /// </summary>
        public bool guild
        {
            get => (ullFlag & (1UL << 24)) != 0;
            set => ullFlag = value ? (ullFlag | (1UL << 24)) : (ullFlag & ~(1UL << 24));
        }

        /// <summary>
        /// Impede a participação no Special Shuffle Course. Representa o valor do bit 25.
        /// </summary>
        public bool ssc
        {
            get => (ullFlag & (1UL << 25)) != 0;
            set => ullFlag = value ? (ullFlag | (1UL << 25)) : (ullFlag & ~(1UL << 25));
        }

        /// <summary>
        /// Flag desconhecida. Representa os bits 26-27.
        /// </summary>
        public bool Unknown3
        {
            get => (ullFlag & (3UL << 26)) != 0;
            set => ullFlag = value ? (ullFlag | (3UL << 26)) : (ullFlag & ~(3UL << 26));
        }

        /// <summary>
        /// Impede o uso do Memorial Shop. Representa o valor do bit 28.
        /// </summary>
        public bool memorial_shop
        {
            get => (ullFlag & (1UL << 28)) != 0;
            set => ullFlag = value ? (ullFlag | (1UL << 28)) : (ullFlag & ~(1UL << 28));
        }

        /// <summary>
        /// Impede a participação no Short Game. Representa o valor do bit 29.
        /// </summary>
        public bool short_game
        {
            get => (ullFlag & (1UL << 29)) != 0;
            set => ullFlag = value ? (ullFlag | (1UL << 29)) : (ullFlag & ~(1UL << 29));
        }

        /// <summary>
        /// Impede o acesso ao Character Mastery System. Representa o valor do bit 30.
        /// </summary>
        public bool char_mastery
        {
            get => (ullFlag & (1UL << 30)) != 0;
            set => ullFlag = value ? (ullFlag | (1UL << 30)) : (ullFlag & ~(1UL << 30));
        }

        /// <summary>
        /// Flag desconhecida. Representa o valor do bit 31.
        /// </summary>
        public bool Unknown4
        {
            get => (ullFlag & (1UL << 31)) != 0;
            set => ullFlag = value ? (ullFlag | (1UL << 31)) : (ullFlag & ~(1UL << 31));
        }

        /// <summary>
        /// Impede o uso do Lolo Compound Card System. Representa o valor do bit 32.
        /// </summary>
        public bool lolo_copound_card
        {
            get => (ullFlag & (1UL << 32)) != 0;
            set => ullFlag = value ? (ullFlag | (1UL << 32)) : (ullFlag & ~(1UL << 32));
        }

        /// <summary>
        /// Impede o uso do Caddie Recycle Item System. Representa o valor do bit 33.
        /// </summary>
        public bool cadie_recycle
        {
            get => (ullFlag & (1UL << 33)) != 0;
            set => ullFlag = value ? (ullFlag | (1UL << 33)) : (ullFlag & ~(1UL << 33));
        }

        /// <summary>
        /// Impede o uso do Legacy Tiki Shop System. Representa o valor do bit 34.
        /// </summary>
        public bool legacy_tiki_shop
        {
            get => (ullFlag & (1UL << 34)) != 0;
            set => ullFlag = value ? (ullFlag | (1UL << 34)) : (ullFlag & ~(1UL << 34));
        }

    }



    [StructLayout(LayoutKind.Sequential, Pack = 4, Size = 92)]
    public class ServerInfo
    {
        [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 40)]
        private byte[] name_bytes;
        public string nome { get => name_bytes.GetString(); set => name_bytes.SetString(value); }
        public int uid { get; set; }
        public int max_user { get; set; }
        public int curr_user { get; set; }
        [field: MarshalAs(UnmanagedType.ByValTStr, SizeConst = 18)]
        public string ip { get; set; } = "";
        public int port { get; set; }
        [field: MarshalAs(UnmanagedType.Struct, SizeConst = 4)]
        public uProperty propriedade = new uProperty();
        public int angelic_wings_num { get; set; }
        [field: MarshalAs(UnmanagedType.Struct, SizeConst = 4)]
        public uEventFlag event_flag = new uEventFlag();
        public short event_map { get; set; }
        public short app_rate { get; set; }
        public short scratch_rate { get; set; } // pode ser scratchy rate ou não
        public short img_no { get; set; }
        public ServerInfo()
        {
            name_bytes = new byte[40];
        }

        public byte[] ToArray()
        {
            using (var p = new PangyaBinaryWriter())
            {
                p.WriteStr(nome, 40);
                p.WriteInt32(uid);
                p.WriteInt32(max_user);
                p.WriteInt32(curr_user);
                p.WriteStr(ip, 18);
                p.WriteInt32(port);
                p.WriteUInt32(propriedade.ulProperty);
                p.WriteInt32(angelic_wings_num);
                p.WriteUInt16(event_flag.usEventFlag);
                p.WriteInt16(event_map);
                p.WriteInt16(app_rate);
                p.WriteInt16(scratch_rate); // pode ser scratchy rate ou não
                p.WriteInt16(img_no);
                return p.GetBytes;
            }
        }

        public ServerInfo ToRead(packet p)
        {
            this.nome = p.ReadPStr(40);
            this.uid = p.ReadInt32();
            this.max_user = p.ReadInt32();
            this.curr_user = p.ReadInt32();
            this.ip = p.ReadPStr(18);
            this.port = p.ReadInt32();
            this.propriedade.ulProperty = p.ReadUInt32();
            this.angelic_wings_num = p.ReadInt32();
            this.event_flag.usEventFlag = p.ReadUInt16();
            this.event_map = p.ReadInt16();
            this.app_rate = p.ReadInt16();
            this.scratch_rate = p.ReadInt16();
            this.img_no = p.ReadInt16();
            return this;
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public class ServerInfoEx : ServerInfo
    {
        public sbyte tipo { get; set; }
        [field: MarshalAs(UnmanagedType.ByValTStr, SizeConst = 40)]
        public string version { get; set; } = new string(new char[40]);
        [field: MarshalAs(UnmanagedType.ByValTStr, SizeConst = 40)]
        public string version_client { get; set; } = new string(new char[40]);
        [field: MarshalAs(UnmanagedType.Struct, SizeConst = 2)]
        public uint packet_version { get; set; }
        public RateConfigInfo rate { get; set; } = new RateConfigInfo();
        [field: MarshalAs(UnmanagedType.Struct, SizeConst = 4)]
        public uFlag flag { get; set; } = new uFlag();
    }
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public class RateConfigInfo
    {

        public short scratchy { get; set; }
        public short papel_shop_rare_item { get; set; }
        public short papel_shop_cookie_item { get; set; }
        public short treasure { get; set; }
        public short pang { get; set; }
        public short exp { get; set; }
        public short club_mastery { get; set; }
        public short chuva { get; set; }
        public short memorial_shop { get; set; }
        public short grand_zodiac_event_time { get; set; }          // Verifica se o evento do grand zodiac está ativado no server
        public short angel_event { get; set; }                      // Verifica se o Angel Event Quit Reduce está ativo no server
        public short grand_prix_event { get; set; }             // Verifica se o Grand Prix evento está ativado no server
        public short golden_time_event { get; set; }                // Verifica se o Golden Time está ativado no server
        public short login_reward_event { get; set; }               // Verifica se o Login Reward está ativado no server
        public short bot_gm_event { get; set; }                 // Verifica se o Bot GM Event está ativado no server
        public short smart_calculator { get; set; }             // Verifica se o Smart Calculator está ativado no server

        public short world_tour_event { get; set; }

        public short hole_event { get; set; }

        public short mission_event { get; set; }
        public short point_event_shop { get; set; }
        public uint countBitGrandPrixEvent()
        {

            uint count = 0;
            // 16 Bit public short
            for (var i = 0; i < 16u; ++i)
            {
                var check = (grand_prix_event >> i);
                if ((check & 1) == 1)
                    count++;
            }
            return count;
        }
        public List<uint> getValueBitGrandPrixEvent()
        {

            List<uint> v_value = new List<uint>();

            // 16 Bit unisgned short
            for (var i = 0; i < 16; ++i)
            {
                var check = (grand_prix_event >> i);
                if ((check & 1) == 1)
                    v_value.Add((uint)i + 1);
            }
            return v_value;
        }

        public bool checkBitGrandPrixEvent(int _type)
        {
            if (_type == 0)
                return false;

            var check = Convert.ToUInt32(grand_prix_event);

            return ((check >> (_type - 1)) & 1) == 1;
        }


        public override string ToString()
        {
            return $"GRAND_ZODIAC_EVENT_TIME={grand_zodiac_event_time}, " +
                   $"GOLDEN_TIME_EVENT={golden_time_event}, " +
                   $"ANGEL_EVENT={angel_event}, " +
                   $"GRAND_PRIX_EVENT={grand_prix_event}, " +
                   $"LOGIN_REWARD_EVENT={login_reward_event}, " +
                   $"BOT_GM_EVENT={bot_gm_event}, " +
                   $"SMART_CALCULATOR_SYSTEM={smart_calculator}, " +
                   $"SCRATCHY={scratchy}, " +
                   $"PAPEL_SHOP_RARE_ITEM={papel_shop_rare_item}, " +
                   $"PAPEL_SHOP_COOKIE_ITEM={papel_shop_cookie_item}, " +
                   $"TREASURE={treasure}, " +
                   $"PANG={pang}, " +
                   $"EXP={exp}, " +
                   $"CLUB_MASTERY={club_mastery}, " +
                   $"CHUVA={chuva}, " +
                   $"MEMORIAL_SHOP={memorial_shop}";
        }
    }

    public partial class TableMac
    {
        public string Mac_Adress { get; set; }
        public DateTime Date { get; set; }

        public TableMac(string adress, DateTime insert_time)
        {
            Mac_Adress = adress;
            Date = insert_time;
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class chat_macro_user
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 9)]
        public chat_macro[] macro;

        public chat_macro_user()
        {
            macro = new chat_macro[9];
            clear();
        }

        public void setMacro(int index, string macros)
        {
            macro[index].text = macros;
        }

        public void clear()
        {
            for (int i = 0; i < macro.Length; i++)
            {
                macro[i] = new chat_macro();
            }
        }
        public byte[] ToArray()
        {
            using (var p = new PangyaBinaryWriter())
            {
                for (int i = 0; i < 9; i++)
                {
                    p.WriteString(macro[i].text, 64);
                }
                return p.GetBytes;
            }
        }
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class chat_macro
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
            public string text;
        }
    }


    // Auth Server m_key Struture
    public class AuthServerKey
    {
        public AuthServerKey()
        {
        }
        public bool isValid()
        {
            return (valid == 1 && !string.IsNullOrEmpty(key));
        }
        public bool checkKey(string _str)
        {
            return (isValid() && string.Compare(_str, key) == 0);
        }
        public int server_uid;
        [field: MarshalAs(UnmanagedType.ByValTStr, SizeConst = 17)]
        public string key;               // 16 + null termineted string
        public byte valid = 1;
    }


    // Keys Of Login
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public class KeysOfLogin
    {
        public KeysOfLogin()
        {
            keys = new string[2];
        }
        public byte valid;
        [field: MarshalAs(UnmanagedType.ByValTStr, SizeConst = 10)]
        public string[] keys { get; set; } = new string[2];
    }

    // Keys Of Login
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public class AuthKeyInfo
    {
        public byte valid;
        [field: MarshalAs(UnmanagedType.ByValTStr, SizeConst = 10)]
        public string key { get; set; }
    }


    // Auth m_key Login Info
    // Keys Of Login
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public class AuthKeyLoginInfo : AuthKeyInfo
    {
    }
    // Auth m_key Game Info
    public class AuthKeyGameInfo : AuthKeyInfo
    {
        public int server_uid;
    }


    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 513)]
    public class CharacterInfo
    {
        public CharacterInfo()
        {
            clear();
        }

        public enum Stats : int
        {
            S_POWER,
            S_CONTROL,
            S_ACCURACY,
            S_SPIN,
            S_CURVE,
        }
        public uint _typeid { get; set; }
        public int id { get; set; }
        public byte default_hair { get; set; }
        public byte default_shirts { get; set; }
        public byte gift_flag { get; set; }
        public byte purchase { get; set; }
        /// <summary>
        /// Parts typeid, do 1 ao 24
        /// </summary>
        [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 24)]
        public uint[] parts_typeid { get; set; }
        /// <summary>
        /// Parts id, do 1 ao 24
        /// </summary>
        [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 24)]
        public uint[] parts_id { get; set; }
        /// <summary>
        ///Não sei bem direito o que é aqui
        /// </summary>
        [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 216)]
        public byte[] UccIndexList { get; set; }
        /// <summary>
        ///Auxiliar Parts 5, aqui fica anel
        /// </summary>
        [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
        public uint[] auxparts { get; set; }
        /// <summary>
        ///Cut-in, no primeiro mas acho que pode ser cut-in no resto
        /// </summary>
        [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public uint[] cut_in { get; set; }
        /// <summary>
        ///Aqui é o character stats, como controle, força, spin e etc
        /// </summary>
        [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
        public byte[] pcl { get; set; }
        /// <summary>
        /// Mastery, que aumenta os slot do stats do character
        /// </summary>
        public uint mastery { get; set; }
        [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public uint[] Card_Character { get; set; }				// 4 Slot de card Character
        [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public uint[] Card_Caddie { get; set; }             // 4 Slot de card Caddie
        [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public uint[] Card_NPC { get; set; }

        public void clear()
        {
            Card_NPC = new uint[4];
            Card_Character = new uint[4];
            Card_Caddie = new uint[4];
            parts_id = new uint[24];
            parts_typeid = new uint[24];
            auxparts = new uint[5];
            UccIndexList = new byte[216];
            cut_in = new uint[4];
            pcl = new byte[5];
        }

        public byte AngelEquiped()
        {
            uint typeId = (_typeid & 0x000000FF);
            uint partNum;

            var angel = Global.gacha_angel_wings.FirstOrDefault(el => sIff.getInstance().getItemCharIdentify(el) == typeId);
            if (angel != 0 && (partNum = sIff.getInstance().getItemCharPartNumber(angel)) >= 0u && parts_typeid[partNum] == angel)
                return 1; // 3% icon rosa e drop chance A+ e treasure point A+

            // Verifica se o item está na lista de Gacha Angel Wings
            var gachaAngel = Global.gacha_angel_wings.FirstOrDefault(el => sIff.getInstance().getItemCharIdentify(el) == typeId);
            if (gachaAngel != 0 && (partNum = sIff.getInstance().getItemCharPartNumber(gachaAngel)) >= 0u && parts_typeid[partNum] == gachaAngel)
                return 2; // Drop chance A+ e treasure point A+

            return 0; // Nenhuma Angel Wings equipada                
        }

        public bool isEquipedPartSlotThirdCaddieCardSlot()
        {
            for (var i = 0; i < (parts_typeid.Length); ++i)
            {
                Part part;
                if (parts_id[i] != 0 && (part = sIff.getInstance().findPart(parts_typeid[i])) != null)
                {
                    if (part._CardSlot.CaddieSlot != 0) // Tem um Part que Libera o terceiro Caddie Card Slot
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public bool isPartEquiped(uint _part_typeid, int _id)
        {

            if (_part_typeid == 0)
                return false;

            if (sIff.getInstance().getItemCharIdentify(_part_typeid) != (_typeid & 0x000000FF))
                return false;

            var part_num = sIff.getInstance().getItemCharPartNumber(_part_typeid);

            if (parts_typeid[part_num] != _part_typeid || parts_id[part_num] != _id)
                return false;

            return true;
        }
        public bool isPartEquiped(uint _part_typeid)
        {

            if (_part_typeid == 0)
                return false;

            if (sIff.getInstance().getItemCharIdentify(_part_typeid) != (_typeid & 0x000000FF))
                return false;

            var part_num = sIff.getInstance().getItemCharPartNumber(_part_typeid);

            if (parts_typeid[part_num] != _part_typeid)
                return false;

            return true;
        }


        public bool isAuxPartEquiped(uint _auxPart_typeid)
        {

            if (_auxPart_typeid == 0)
                return false;

            for (var i = 0; i < auxparts.Length; ++i)
            {
                if (auxparts[i] == _auxPart_typeid)
                {
                    return true;
                }
            }

            return false;
        }

        public void unequipPart(Part _part)
        { // Deseequipa o Part do character e coloca os Parts Default do Character no lugar

            if (_part == null)
            {

                Singleton<list_fifo_console_asyc<message>>.getInstance().push(new message("[CharacterInfo::unequipPart][Error] IFF::Part* _part is invalid(null).", type_msg.CL_FILE_LOG_AND_CONSOLE));

                return;
            }
            for (uint i = 0; i < (parts_typeid.Length); ++i)
            {

                if (_part.position_mask.getSlot((int)i))
                { // Coloca Def Parts

                    uint def_part = (uint)(((i | (uint)(_typeid << 5)) << 13) | 0x8000400);

                    var part_find = sIff.getInstance().findPart(def_part);

                    parts_typeid[i] = (part_find != null && part_find.ID != 0) ? (uint)def_part : 0;
                    parts_id[i] = 0;
                }
            }
        }


        public void unequipPart(uint _typeid)
        {

            // Invalid Typeid
            if (_typeid == 0u)
                return;

            var part = sIff.getInstance().findPart(_typeid);

            if (part != null && part.ID != 0)
            {
                unequipPart(part);
            }
            else
            {

                Singleton<list_fifo_console_asyc<message>>.getInstance().push(new message("[CharacterInfo::unequipPart][Error][WARNIG] Part[TYPEID=" + Convert.ToString(_typeid) + "], mas ele nao existe no IFF_STRUCT do server, desequipa sem usar a funcao do character. Hacker ou Bug.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Não vai pegar todos os Slots que o Part ocupava para desequipar, desequipa o só onde tem o typeid   
                for (uint i = 0; i < (parts_typeid.Length); ++i)
                {

                    // Não vai pergar todos os 
                    if (parts_typeid[i] == _typeid)
                    { // Coloca Def Parts

                        uint def_part = (uint)(((i | (uint)(_typeid << 5)) << 13) | 0x8000400);

                        var part_find = sIff.getInstance().findPart(def_part);

                        parts_typeid[i] = (part_find != null && part_find.ID != 0) ? (uint)def_part : 0;
                        parts_id[i] = 0;

                        break;
                    }
                }
            }

        }

        public void unequipAuxPart(uint _typeid)
        {

            // Invalid Typeid
            if (_typeid == 0u)
                return;

            for (var i = 0; i < (auxparts.Length); ++i)
            {

                if (auxparts[i] == _typeid)
                {

                    auxparts[i] = 0;

                    // Já desequipou sai
                    break;
                }
            }
        }


        public sbyte getSlotOfStatsFromsbyteEquipedPartItem(Stats __stat)
        {   // Get Slot of stats from Character equiped item

            sbyte value = 0;

            // Invalid Stats type, Unknown type Stats
            if (__stat > Stats.S_CURVE)
                return -1;

            for (var i = 0; i < (Marshal.SizeOf(parts_typeid) / Marshal.SizeOf(parts_typeid[0])); ++i)
            {
                Part part;
                if (parts_id[i] != 0 && (part = sIff.getInstance().findPart(parts_typeid[i])) != null)
                    value += (sbyte)part.SlotStats.getSlot[(int)__stat];
            }

            return value;
        }

        public sbyte getSlotOfStatsFromCharEquipedPartItem(Stats __stat)
        {
            int totalValue = 0; // Use int para evitar problemas de cast durante a soma

            if (__stat > Stats.S_CURVE)
                return -1;

            for (var i = 0; i < parts_typeid.Length; ++i)
            {
                // Verifica se o slot de equipamento não está vazio
                if (parts_id[i] != 0)
                {
                    var part = sIff.getInstance().findPart(parts_typeid[i]);
                    if (part != null)
                    {
                        short slotAmount = (short)part.SlotStats.getSlot[(ushort)__stat];
                        totalValue += slotAmount;

                        // Console.WriteLine($"Item {parts_typeid[i]} adicionou {slotAmount} slots de {__stat}");
                    }
                }
            }

            return (sbyte)totalValue;
        }

        public sbyte getSlotOfStatsFromCharEquipedAuxPart(Stats __stat)
        { 
            sbyte value = 0;
            AuxPart aux_part = null;

            // Invalid Stats type, Unknown type Stats
            if (__stat > Stats.S_CURVE)
            {
                return -1;
            }

            for (var i = 0; i < auxparts.Length; ++i)
            {
                
                if (auxparts[i] != 0)
                  {
                    aux_part = sIff.getInstance().findAuxPart(auxparts[i]);
                    if (aux_part != null)
                        value += (sbyte)aux_part.slot[(int)__stat];
                }

            }

            return value;
        }

        public sbyte getSlotOfStatsFromSetEffectTable(Stats __stat)
        {

            sbyte value = 0;
            int ret = 0;

            // Set Effect Table
            SetEffectTable iff_SET = null;

            // Ids que já foram
            List<uint> check_id = new List<uint>();

            // Invalid Stats type, Unknown type Stats
            if (__stat > Stats.S_CURVE)
            {
                return -1;
            }

            // Part Item                                                                                     
            for (var i = 0; i < (parts_typeid.Length); ++i)
            {

                if (parts_typeid[i] != 0)
                {

                    iff_SET = sIff.getInstance().findFirstItemInSetEffectTable(parts_typeid[i]);

                    // O Item no Set Effect Table
                    if (iff_SET != null)
                    {
                        if (check_id.Count == 0 || !check_id.Contains(iff_SET.Index))
                        {

                            // add id para o check
                            check_id.Add(iff_SET.Index);

                            // Verifica sem tem todos os itens da tabela de efeito equipados
                            ret = 1;
                            for (var j = 0; j < (iff_SET.item.ID.Length); ++j)
                            {

                                if (iff_SET.item.ID[j] != 0u)
                                {

                                    if (sIff.getInstance().getItemGroupIdentify(iff_SET.item.ID[j]) == PangyaAPI.IFF.JP.Models.Flags.IFF_GROUP.PART)
                                    {

                                        if (!isPartEquiped(iff_SET.item.ID[j]))
                                        {
                                            // Não tem o outro item equipado
                                            ret = 0;

                                            break;
                                        }

                                    }
                                    else if (sIff.getInstance().getItemGroupIdentify(iff_SET.item.ID[j]) == IFF_GROUP.AUX_PART)
                                    {

                                        if (!isAuxPartEquiped(iff_SET.item.ID[j]))
                                        {

                                            // Não tem o outro item equipado
                                            ret = 0;

                                            break;
                                        }

                                    }
                                }
                            }

                            // Não tem todos os itens equipados
                            if (ret == 0)
                            {
                                continue;
                            }

                            // Effect 6 ONE_ALL_STATS
                            foreach (var _el in iff_SET.effect.effect)
                            {

                                if (_el == (byte)AbilityEffect.ONE_IN_ALL_STATS)
                                    value++;
                            }

                            // Slot
                            value += (sbyte)iff_SET.Slot[(int)__stat];
                        }
                    }
                }
            }

            //AUX PART ITEM
            for (var i = 0; i < (auxparts.Length); ++i)
            {

                if (auxparts[i] != 0)
                {

                    iff_SET = sIff.getInstance().findFirstItemInSetEffectTable(auxparts[i]);

                    // O Item no Set Effect Table
                    if (iff_SET != null)
                    {

                        if (check_id.Count == 0 || !check_id.Contains(iff_SET.Index))
                        {

                            // add id para o check
                            check_id.Add(iff_SET.Index);

                            // Verifica sem tem todos os itens da tabela de efeito equipados
                            ret = 1;
                             
                            for (var j = 0u; j < (iff_SET.item.ID.Length); ++j)
                            {

                                if (iff_SET.item.ID[j] != 0u)
                                {

                                    if (sIff.getInstance().getItemGroupIdentify(iff_SET.item.ID[j]) == IFF_GROUP.PART)
                                    {

                                        if (!isPartEquiped(iff_SET.item.ID[j]))
                                        {

                                            // Não tem o outro item equipado
                                            ret = 0;

                                            break;
                                        }

                                    }
                                    else if (sIff.getInstance().getItemGroupIdentify(iff_SET.item.ID[j]) == IFF_GROUP.AUX_PART)
                                    {

                                        if (!isAuxPartEquiped(iff_SET.item.ID[j]))
                                        {

                                            // Não tem o outro item equipado
                                            ret = 0;

                                            break;
                                        }

                                    }
                                }
                            }

                            // Não tem todos os itens equipados
                            if (ret == 0)
                            {
                                continue;
                            }

                            // Effect 6 ONE_ALL_STATS
                            foreach (var _el in iff_SET.effect.effect)
                            {

                                if (_el == (byte)AbilityEffect.ONE_IN_ALL_STATS)
                                    value++;
                            }

                            // Slot
                            value += (sbyte)iff_SET.Slot[(int)__stat];
                        }
                    }
                }
            }
             
            return value;
        }


        public sbyte getSlotOfStatsFromCharEquipedCard(Stats __stat)
        {

            sbyte value = 0;
            Card card = null;

            // Invalid Stats type, Unknown type Stats
            if (__stat > Stats.S_CURVE)
                return -1;

            for (uint i = 0; i < Card_Character.Length; ++i)
            {

                if (Card_Character[i] != 0 && (card = sIff.getInstance().findCard(Card_Character[i])) != null)
                    value += (sbyte)card.c[(int)__stat];
            }

            return value;
        }

        public void initComboDef()
        {
            if (_typeid == 0)
                return;

            // Limpa
            Array.Clear(parts_typeid, 0, parts_typeid.Length); // array com 24 uints
            Array.Clear(parts_id, 0, parts_id.Length);         // array com 24 uints

            for (uint i = 0; i < parts_typeid.Length; ++i)
            {
                uint part_typeid = (((_typeid << 5 /*CharIdentify*/) | i) << 13 /*PartNum*/) | 0x8000400;
                var part_find = sIff.getInstance().findPart(part_typeid);
                if (part_find != null && part_find.ID == part_typeid) // <-- aqui estava errado
                    parts_typeid[i] = part_typeid;
            }
        }


        /// <summary>
        /// size = 513 bytes
        /// </summary>
        /// <returns></returns>
        public byte[] ToArray()
        {
            using (var p = new PangyaBinaryWriter())
            {
                p.Write(_typeid);
                p.Write(id);
                p.Write(default_hair);
                p.Write(default_shirts);
                p.Write(gift_flag);
                p.Write(purchase);
                p.WriteUInt32(parts_typeid);
                p.WriteUInt32(parts_id);
                for (int i = 0; i < 216; i++)
                    p.WriteByte(0);
                p.WriteUInt32(auxparts);
                p.WriteUInt32(cut_in);
                p.WriteBytes(pcl);
                p.WriteUInt32(mastery);
                p.WriteUInt32(Card_Character);
                p.WriteUInt32(Card_Caddie);
                p.WriteUInt32(Card_NPC);
                return p.GetBytes;
            }
        }

        public void ToRead(packet r)
        {
            _typeid = r.ReadUInt32();
            id = r.ReadInt32();
            default_hair = r.ReadByte();
            default_shirts = r.ReadByte();
            gift_flag = r.ReadByte();
            purchase = r.ReadByte();
            parts_typeid = r.ReadUInt32(24);
            parts_id = r.ReadUInt32(24);
            UccIndexList = r.ReadBytes(216);
            auxparts = r.ReadUInt32(5); 
            cut_in = r.ReadUInt32(4);
            pcl = r.ReadBytes(5);
            mastery = r.ReadUInt32();
            Card_Character = r.ReadUInt32(4);
            Card_Caddie = r.ReadUInt32(4);
            Card_NPC = r.ReadUInt32(4); 
        }

    }
    // Auth Server - Player Info
    public struct AuthServerPlayerInfo
    {
        public uint uid;
        public string id;
        public string ip;
        public int option;

        public AuthServerPlayerInfo(uint _uid = 0)
        {
            uid = _uid;
            id = string.Empty;
            ip = string.Empty;
            option = -1;
        }

        public AuthServerPlayerInfo(uint _uid, string _id, string _ip)
        {
            uid = _uid;
            id = _id;
            ip = _ip;
            option = 1;
        }

        public void Clear()
        {
            this.uid = 0;
            this.option = -1; // -1 Geralmente indica "Não Inicializado" ou "Erro"
            this.id = string.Empty;
            this.ip = "0.0.0.0";
        }
    }

    // Auth Server - Server Send command to Other Server Header
    public class CommandOtherServerHeader
    {
        public uint send_server_uid_or_type { get; set; } // Envia o comando para esse server (UID/TYPE)
        public short command_id { get; set; }         // Comando ID

        public CommandOtherServerHeader(uint ul = 0)
        {
            send_server_uid_or_type = ul;
            command_id = 0;
        }

        public virtual void Clear()
        {
            send_server_uid_or_type = 0;
            command_id = 0;
        }

        public byte[] ToArray()
        {
            using (var p = new PangyaBinaryWriter())
            {
                p.Write(send_server_uid_or_type);
                p.Write(command_id);
                return p.GetBytes;
            }
        }
    }

    // Auth Server - Server Send command to Other Server Header Ex
    public class CommandOtherServerHeaderEx : CommandOtherServerHeader
    {
        public class StCommand
        {
            public byte[] buff { get; set; }
            public ushort size { get; set; }
            private bool state { get; set; }

            public StCommand(ushort size = 0)
            {
                buff = new byte[0];
                this.size = 0;
                state = false;

                init(size);
            }

            public void Destroy()
            {
                buff = new byte[0];
                state = false;
            }

            public void init(ushort size)
            {
                if (size > 0)
                {
                    this.size = size;

                    if (buff != null)
                        Destroy();

                    buff = new byte[size];
                    state = true;
                }
            }

            public bool is_good() => state;
        }

        public StCommand command { get; set; }

        public CommandOtherServerHeaderEx(uint ul = 0) : base(ul)
        {
            command = new StCommand(0);
            Clear();
        }
         
        public override void Clear()
        {
            base.Clear();
            command.Destroy();
        }

        public new byte[] ToArray()
        {
            using (var p = new PangyaBinaryWriter())
            {
                // 1. Escreve os dados da classe base (send_server_uid_or_type e command_id)
                p.Write(base.ToArray());

                // 2. Escreve os dados do StCommand
                if (command != null && command.is_good() && command.buff != null)
                {
                    p.WriteUInt16(command.size); // Escreve o tamanho do buffer (ushort)
                    p.WriteBytes(command.buff); // Escreve o conteúdo binário
                }
                else
                {
                    p.WriteUInt16(0);    // Se não houver comando, envia tamanho 0
                } 
                return p.GetBytes;
            }
        }
    }
    #region User Info


    public class BlockFlag
    {
        public BlockFlag()
        {
            if (m_flag == null || (m_flag.ullFlag == 0))
            {
                m_flag = new uFlag(0);
            }

            m_id_state = new IDStateBlockFlag(0);
        }
        public void setIDState(ulong _id_state)
        {
            m_id_state = new IDStateBlockFlag(_id_state);

            // Block Recursos do player
            if ((m_id_state.L_BLOCK_LOUNGE/* & 4*/)) // Block Lounge
                m_flag.lounge = true; // Block Lounge
            if ((m_id_state.L_BLOCK_SHOP_LOUNGE/* & 8*/)) // Block Shop Lounge
                m_flag.personal_shop = true; // Block Shop Lounge
            if ((m_id_state.L_BLOCK_GIFT_SHOP/* & 16*/)) // Block Gift Shop
                m_flag.gift_shop = true; // Block Gift Shop
            if ((m_id_state.L_BLOCK_PAPEL_SHOP/* & 32*/)) // Block Papel Shop
                m_flag.papel_shop = true; // Block Papel Shop
            if ((m_id_state.L_BLOCK_SCRATCHY/* & 64*/)) // Block Scratchy
                m_flag.scratchy = true; // Block Scratchy
            if ((m_id_state.L_BLOCK_TICKER/* & 128*/)) // Block Ticker
                m_flag.ticker = true; // Block Ticker
            if ((m_id_state.L_BLOCK_MEMORIAL_SHOP/* & 256*/)) // Block Memorial Shop
                m_flag.memorial_shop = true; // Block Memorial Shop
        }

        public IDStateBlockFlag m_id_state;
        public uFlag m_flag;
    }

    // ------------------ Player Account Basic ---------------- //
    // Struct ID state Block Flag
    public class IDStateBlockFlag
    {
        public IDStateBlockFlag(ulong _ul)
        {
            _ull_IDState = _ul;
        }
        private ulong _ull_IDState;

        /// <summary>
        /// Todo:  0 player normal status, 1 block por tempo, 2 block permanente, 4 block lounge, 8 block shop lounge,  10 nao lembro,  16 block gift shop, 20 paran end,  32 block papel shop, FLAGBLOCK_WRONG_CARD_BLOCK  = 40h,  64 block scratchy, 128 Ticker, 256 block memorial shop
        /// </summary>
        public ulong ull_IDState
        {
            get { return _ull_IDState; }
            set
            {
                _ull_IDState = value;
            }
        }

        public int block_time;

        public bool L_BLOCK_TEMPORARY //bloqueio temporario
        {
            get => (_ull_IDState & 1) == 1;
            set => _ull_IDState = value ? (_ull_IDState | 1) : (_ull_IDState & ~(1ul));
        }

        public bool L_BLOCK_FOREVER // bloqueio infinito
        {
            get => (_ull_IDState & 2) == 2;
            set => _ull_IDState = value ? (_ull_IDState | 2) : (_ull_IDState & ~(2ul));
        }

        public bool L_BLOCK_LOUNGE
        {
            get => (_ull_IDState & 4) == 4;
            set => _ull_IDState = value ? (_ull_IDState | 4) : (_ull_IDState & ~(4ul));
        }

        public bool L_BLOCK_SHOP_LOUNGE
        {
            get => (_ull_IDState & 8) == 8;
            set => _ull_IDState = value ? (_ull_IDState | 8) : (_ull_IDState & ~(8ul));
        }

        public bool L_BLOCK_GIFT_SHOP
        {
            get => (_ull_IDState & 16) == 16;
            set => _ull_IDState = value ? (_ull_IDState | 16) : (_ull_IDState & ~(16ul));
        }

        public bool L_BLOCK_PAPEL_SHOP
        {
            get => (_ull_IDState & 32) == 32;
            set => _ull_IDState = value ? (_ull_IDState | 32) : (_ull_IDState & ~(32ul));
        }

        public bool L_BLOCK_SCRATCHY
        {
            get => (_ull_IDState & 64) == 64;
            set => _ull_IDState = value ? (_ull_IDState | 64) : (_ull_IDState & ~(64ul));
        }

        public bool L_BLOCK_TICKER
        {
            get => (_ull_IDState & 128) == 128;
            set => _ull_IDState = value ? (_ull_IDState | 128) : (_ull_IDState & ~(128ul));
        }

        public bool L_BLOCK_MEMORIAL_SHOP
        {
            get => (_ull_IDState & 256) == 256;
            set => _ull_IDState = value ? (_ull_IDState | 256) : (_ull_IDState & ~(256ul));
        }

        public bool L_BLOCK_ALL_IP //nao sei qual bit flag
        {
            get => (_ull_IDState & 512) == 512;
            set => _ull_IDState = value ? (_ull_IDState | 512) : (_ull_IDState & ~(512ul));
        }

        public bool L_BLOCK_MAC_ADDRESS //nao sei qual bit flag
        {
            get => (_ull_IDState & 1024) == 1024;
            set => _ull_IDState = value ? (_ull_IDState | 1024) : (_ull_IDState & ~(1024ul));
        }
    }

    #endregion
}
