using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using Pangya_GameServer.Game;
using Pangya_GameServer.Game.System;
using Pangya_GameServer.UTIL; 
using PangyaAPI.Network.PangyaPacket;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Models;
using PangyaAPI.Utilities.Log;

namespace Pangya_GameServer.Models
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class uSpecialShot
    {
        public uSpecialShot()
        {
            clear();
        }
        void clear()
        {
            ulSpecialShot = 0;
        }
        public uint ulSpecialShot;
        public uint spin_front { get => (ulSpecialShot & (1 << 0)) != 0 ? 1U : 0U; set { if (value != 0) ulSpecialShot |= (1 << 0); else ulSpecialShot &= ~(1U << 0); } }
        public uint spin_back { get => (ulSpecialShot & (1 << 1)) != 0 ? 1U : 0U; set { if (value != 0) ulSpecialShot |= (1 << 1); else ulSpecialShot &= ~(1U << 1); } }
        public uint curve_left { get => (ulSpecialShot & (1 << 2)) != 0 ? 1U : 0U; set { if (value != 0) ulSpecialShot |= (1 << 2); else ulSpecialShot &= ~(1U << 2); } }
        public uint curve_right { get => (ulSpecialShot & (1 << 3)) != 0 ? 1U : 0U; set { if (value != 0) ulSpecialShot |= (1 << 3); else ulSpecialShot &= ~(1U << 3); } }
        public uint tomahawk { get => (ulSpecialShot & (1 << 4)) != 0 ? 1U : 0U; set { if (value != 0) ulSpecialShot |= (1 << 4); else ulSpecialShot &= ~(1U << 4); } }
        public uint cobra { get => (ulSpecialShot & (1 << 5)) != 0 ? 1U : 0U; set { if (value != 0) ulSpecialShot |= (1 << 5); else ulSpecialShot &= ~(1U << 5); } }
        public uint spike { get => (ulSpecialShot & (1 << 6)) != 0 ? 1U : 0U; set { if (value != 0) ulSpecialShot |= (1 << 6); else ulSpecialShot &= ~(1U << 6); } }
        public uint _unused = 25; // Não usa 
        public override string ToString()
        {
            return "Special Shot: " + Environment.NewLine + (ulSpecialShot) + " Spin Front: " + Environment.NewLine + (spin_front) + Environment.NewLine + " Spin Back: " + Environment.NewLine + (spin_back) + Environment.NewLine + " Curve Left: " + Environment.NewLine + (curve_left) + Environment.NewLine + " Curve Right: " + Environment.NewLine + (curve_right) + Environment.NewLine + " Tomahwak: " + Environment.NewLine + (tomahawk) + Environment.NewLine + " Cobra: " + Environment.NewLine + (cobra) + Environment.NewLine + " Spike: " + Environment.NewLine + (spike) + Environment.NewLine + " Unused: " + Environment.NewLine + (_unused);
        }
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class ShotDataBase
    {
        public ShotDataBase()
        {
            clear();
        }
        public void clear()
        {
            Array.Clear(bar_point, 0, 2);
            Array.Clear(ball_effect, 0, 2);
            acerto_pangya_flag = 0;
            special_shot = new uSpecialShot();
            time_hole_sync = 0;
            mira = 0;
            time_shot = 0;
            bar_point1 = 0;
            club = 0;
            Array.Clear(fUnknown, 0, 2);
            impact_zone_pixel = 0;
            Array.Clear(natural_wind, 0, 2);
        }
        public override string ToString()
        {
            return "Bar Point: Forca: " + bar_point[0] + " Hit PangYa: " + bar_point[1] + Environment.NewLine +
                   "Ball Effect: X: " + ball_effect[0] + " Y: " + ball_effect[1] + Environment.NewLine +
                   "Acerto PangYa Flag: " + acerto_pangya_flag + Environment.NewLine +
                   "Special Shot: " + special_shot.ToString() + Environment.NewLine +
                   "Time Hole SYNC: " + time_hole_sync + Environment.NewLine +
                   "Mira(shot): " + mira + Environment.NewLine +
                   "Time Shot: " + time_shot + Environment.NewLine +
                   "Bar Point: Start: " + bar_point1 + Environment.NewLine +
                   "Club: " + club + Environment.NewLine +
                   "fUnknown: [1]: " + fUnknown[0] + " [2]: " + fUnknown[1] + Environment.NewLine +
                   "Impact Zone Size Pixel: " + impact_zone_pixel + Environment.NewLine +
                   "Natural Wind: X: " + natural_wind[0] + " Y: " + natural_wind[1] + Environment.NewLine;
        }



        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public float[] bar_point = new float[2]; // [0] 2 Força, [1] 3 Impact Zone

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public float[] ball_effect = new float[2]; // [0] X Spin,  [1] Y Spin

        public byte acerto_pangya_flag;

        [MarshalAs(UnmanagedType.Struct)]
        public uSpecialShot special_shot = new uSpecialShot(); // Especial Shot, Tomahawk, Cobra e Spike

        public uint time_hole_sync = 0;

        public float mira; // Mira da tacada do player, seria o R do location[x,y,z,r]

        public uint time_shot = 0;

        public float bar_point1;

        public byte club;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public float[] fUnknown = new float[2]; // Float Unknown [0] 1 unknown, [1] 2 unknown

        public float impact_zone_pixel;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public int[] natural_wind = new int[2]; // Natural Wind Valor [0] X valor, [1] Y valor
        public byte[] ToArray()
        {
            using (var p = new PangyaBinaryWriter())
            {
                p.WriteFloat(bar_point);
                p.WriteFloat(ball_effect);
                p.Write(acerto_pangya_flag);
                p.Write(special_shot.ulSpecialShot);
                p.Write(time_hole_sync);
                p.Write(mira);
                p.Write(time_shot);
                p.Write(bar_point1);
                p.Write(club);
                p.WriteFloat(fUnknown);
                p.Write(impact_zone_pixel);
                p.WriteInt32(natural_wind);
                return p.GetBytes;
            }
        }

    }

    // Separei o spand time, que o pang battle não tem ele
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class ShotData : ShotDataBase
    {
        public ShotData()
        {
            clear();
        }
        public override string ToString()
        {
            return base.ToString() + "Spend Time Game: " + Convert.ToString(spend_time_game) + Environment.NewLine;
        }
        public float spend_time_game; // O Acumolo de tempo gasto no jogo, é o tempo decorrido geral

        public byte[] ToArrayEx()
        {
            using (var p = new PangyaBinaryWriter())
            {
                p.WriteBytes(ToArray()); //no versus tem 62,
                p.WriteFloat(spend_time_game);
                return p.GetBytes;
            }
        }
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class ShotDataEx : ShotData
    {
        public ShotDataEx()
        {
            clear();
        }
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class PowerShot
        {
            public PowerShot()
            {
                clear();
            }
            public override string ToString()
            {
                return "Option: " + Convert.ToString((ushort)option) + Environment.NewLine + "Decrease Power Shot: " + Convert.ToString(decrease_power_shot) + Environment.NewLine + "Increase Power Shot: " + Convert.ToString(increase_power_shot) + Environment.NewLine;
            }
            public void clear()
            { }
            public byte option;
            public int decrease_power_shot = 0;
            public int increase_power_shot = 0;
        }
        public override string ToString()
        {
            return option != 0 ? power_shot.ToString() + base.ToString() : base.ToString();
        }

        public void setShot(ShotData value)
        {
            if (value == null) return;

            // Campos de ShotData (herda ShotDataBase)
            this.spend_time_game = value.spend_time_game;

            // Campos de ShotDataBase
            Array.Copy(value.bar_point, this.bar_point, 2);
            Array.Copy(value.ball_effect, this.ball_effect, 2);
            this.acerto_pangya_flag = value.acerto_pangya_flag;
            this.special_shot.ulSpecialShot = value.special_shot.ulSpecialShot;
            this.time_hole_sync = value.time_hole_sync;
            this.mira = value.mira;
            this.time_shot = value.time_shot;
            this.bar_point1 = value.bar_point1;
            this.club = value.club;
            Array.Copy(value.fUnknown, this.fUnknown, 2);
            this.impact_zone_pixel = value.impact_zone_pixel;
            Array.Copy(value.natural_wind, this.natural_wind, 2);
        }

        public ShotDataEx ToRead(packet _packet)
        {
            option = _packet.ReadUInt16();

            if (option == 1)
            {
                power_shot.option = _packet.ReadByte();
                power_shot.decrease_power_shot = _packet.ReadInt32();
                power_shot.increase_power_shot = _packet.ReadInt32();
            }

            //READ SHOTDataBase primeiro, primeiro se lê a classe base, e depois a classe que herda.

            bar_point[0] = _packet.ReadSingle();
            bar_point[1] = _packet.ReadSingle();

            ball_effect[0] = _packet.ReadSingle();
            ball_effect[1] = _packet.ReadSingle();

            acerto_pangya_flag = _packet.ReadByte();
            special_shot.ulSpecialShot = _packet.ReadUInt32();
            time_hole_sync = _packet.ReadUInt32();
            mira = _packet.ReadSingle();

            time_shot = _packet.ReadUInt32();
            bar_point1 = _packet.ReadSingle();
            club = _packet.ReadByte();

            fUnknown[0] = _packet.ReadSingle();
            fUnknown[1] = _packet.ReadSingle();

            impact_zone_pixel = _packet.ReadSingle();

            natural_wind[0] = _packet.ReadInt32();
            natural_wind[1] = _packet.ReadInt32();
            return this;
        }

        public ShotDataEx ToReadEx(packet _packet)
        { 
            ToRead(_packet);//ler a base, e ler o ex
            //READ SHOTDATA extend
            spend_time_game = _packet.ReadSingle();
            return this;
        }

        public ushort option;
        [field: MarshalAs(UnmanagedType.Struct)]
        public PowerShot power_shot = new PowerShot();
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class ShotSyncData
    {
        public void clear()
        {
        }
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class Location
        {
            public void clear()
            { }
            public override string ToString()
            {
                return "X: " + Convert.ToString(x) + " Y: " + Convert.ToString(y) + " Z: " + Convert.ToString(z);
            }
            public float x;
            public float y;
            public float z;
        }
        public enum SHOT_STATE : byte
        {
            PLAYABLE_AREA = 2,//PODE TACAR
            OUT_OF_BOUNDS,//OB
            INTO_HOLE,//PROXIMO AO HOLE
            UNPLAYABLE_AREA//AREA DESCONHECIDA ou nao permitida
        }
        public override string ToString()
        {
            return "OID: " + Convert.ToString(oid) + Environment.NewLine + "Location: " + location.ToString() + Environment.NewLine + "STATE: " + Convert.ToString((ushort)state) + Environment.NewLine + "Bunker Flag: " + Convert.ToString((ushort)bunker_flag) + Environment.NewLine + "ucUnknown: " + Convert.ToString((ushort)ucUnknown) + Environment.NewLine + "Pang: " + Convert.ToString(pang) + Environment.NewLine + "Pang Bonus: " + Convert.ToString(bonus_pang) + Environment.NewLine + "State Shot: " + state_shot.ToString() + Environment.NewLine + "Tempo Shot: " + Convert.ToString(tempo_shot) + Environment.NewLine + "Grand Prix Penalidade: " + Convert.ToString((ushort)grand_prix_penalidade) + Environment.NewLine;
        }
        public int oid = -1;
        [field: MarshalAs(UnmanagedType.Struct)]
        public Location location = new Location();
        public SHOT_STATE state;
        public byte bunker_flag;
        public byte ucUnknown; // Deve ser relacionando ao bunker esses negocios
        public uint pang;
        public uint bonus_pang;
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class stStateShot
        {
            public stStateShot() => clear();
            public void clear()
            {
                shot = new uShotState();
                display = new uDisplayState();
            }

            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            public class uDisplayState
            {
                public uint ulState;
                public void clear()
                { ulState = 0; }
                public bool over_drive
                {
                    get => (ulState & (1u << 0)) != 0;
                    set
                    {
                        if (value)
                            ulState |= (1u << 0);
                        else
                            ulState &= ~(1u << 0);
                    }
                }

                public bool _bit2_unknown
                {
                    get => (ulState & (1u << 1)) != 0;
                    set
                    {
                        if (value)
                            ulState |= (1u << 1);
                        else
                            ulState &= ~(1u << 1);
                    }
                }

                public bool super_pangya
                {
                    get => (ulState & (1u << 2)) != 0;
                    set
                    {
                        if (value)
                            ulState |= (1u << 2);
                        else
                            ulState &= ~(1u << 2);
                    }
                }

                public bool special_shot
                {
                    get => (ulState & (1u << 3)) != 0;
                    set
                    {
                        if (value)
                            ulState |= (1u << 3);
                        else
                            ulState &= ~(1u << 3);
                    }
                }

                public bool beam_impact
                {
                    get => (ulState & (1u << 4)) != 0;
                    set
                    {
                        if (value)
                            ulState |= (1u << 4);
                        else
                            ulState &= ~(1u << 4);
                    }
                }

                public bool chip_in_17_a_199
                {
                    get => (ulState & (1u << 5)) != 0;
                    set
                    {
                        if (value)
                            ulState |= (1u << 5);
                        else
                            ulState &= ~(1u << 5);
                    }
                }

                public bool chip_in_200_plus
                {
                    get => (ulState & (1u << 6)) != 0;
                    set
                    {
                        if (value)
                            ulState |= (1u << 6);
                        else
                            ulState &= ~(1u << 6);
                    }
                }

                public bool long_putt
                {
                    get => (ulState & (1u << 7)) != 0;
                    set
                    {
                        if (value)
                            ulState |= (1u << 7);
                        else
                            ulState &= ~(1u << 7);
                    }
                }

                public bool acerto_hole
                {
                    get => (ulState & (1u << 8)) != 0;
                    set
                    {
                        if (value)
                            ulState |= (1u << 8);
                        else
                            ulState &= ~(1u << 8);
                    }
                }

                public bool approach_shot
                {
                    get => (ulState & (1u << 9)) != 0;
                    set
                    {
                        if (value)
                            ulState |= (1u << 9);
                        else
                            ulState &= ~(1u << 9);
                    }
                }

                public bool chip_in_with_special_shot
                {
                    get => (ulState & (1u << 10)) != 0;
                    set
                    {
                        if (value)
                            ulState |= (1u << 10);
                        else
                            ulState &= ~(1u << 10);
                    }
                }

                public bool _bit12_unknown
                {
                    get => (ulState & (1u << 11)) != 0;
                    set
                    {
                        if (value)
                            ulState |= (1u << 11);
                        else
                            ulState &= ~(1u << 11);
                    }
                }

                public bool happy_bonus
                {
                    get => (ulState & (1u << 12)) != 0;
                    set
                    {
                        if (value)
                            ulState |= (1u << 12);
                        else
                            ulState &= ~(1u << 12);
                    }
                }

                public bool clear_bonus
                {
                    get => (ulState & (1u << 13)) != 0;
                    set
                    {
                        if (value)
                            ulState |= (1u << 13);
                        else
                            ulState &= ~(1u << 13);
                    }
                }

                public bool aztec_bonus
                {
                    get => (ulState & (1u << 14)) != 0;
                    set
                    {
                        if (value)
                            ulState |= (1u << 14);
                        else
                            ulState &= ~(1u << 14);
                    }
                }

                public bool recovery_bonus
                {
                    get => (ulState & (1u << 15)) != 0;
                    set
                    {
                        if (value)
                            ulState |= (1u << 15);
                        else
                            ulState &= ~(1u << 15);
                    }
                }

                public bool chip_in_without_special_shot
                {
                    get => (ulState & (1u << 16)) != 0;
                    set
                    {
                        if (value)
                            ulState |= (1u << 16);
                        else
                            ulState &= ~(1u << 16);
                    }
                }

                public bool bound_bonus
                {
                    get => (ulState & (1u << 17)) != 0;
                    set
                    {
                        if (value)
                            ulState |= (1u << 17);
                        else
                            ulState &= ~(1u << 17);
                    }
                }

                public bool _bit19_unknown
                {
                    get => (ulState & (1u << 18)) != 0;
                    set
                    {
                        if (value)
                            ulState |= (1u << 18);
                        else
                            ulState &= ~(1u << 18);
                    }
                }

                public bool _bit20_unknown
                {
                    get => (ulState & (1u << 19)) != 0;
                    set
                    {
                        if (value)
                            ulState |= (1u << 19);
                        else
                            ulState &= ~(1u << 19);
                    }
                }

                public bool mascot_bonus_with_pangya
                {
                    get => (ulState & (1u << 20)) != 0;
                    set
                    {
                        if (value)
                            ulState |= (1u << 20);
                        else
                            ulState &= ~(1u << 20);
                    }
                }

                public bool mascot_bonus_without_pangya
                {
                    get => (ulState & (1u << 21)) != 0;
                    set
                    {
                        if (value)
                            ulState |= (1u << 21);
                        else
                            ulState &= ~(1u << 21);
                    }
                }

                public bool special_bonus_with_pangya
                {
                    get => (ulState & (1u << 22)) != 0;
                    set
                    {
                        if (value)
                            ulState |= (1u << 22);
                        else
                            ulState &= ~(1u << 22);
                    }
                }

                public bool special_bonus_without_pangya
                {
                    get => (ulState & (1u << 23)) != 0;
                    set
                    {
                        if (value)
                            ulState |= (1u << 23);
                        else
                            ulState &= ~(1u << 23);
                    }
                }

                public bool _bit25_unknown
                {
                    get => (ulState & (1u << 24)) != 0;
                    set
                    {
                        if (value)
                            ulState |= (1u << 24);
                        else
                            ulState &= ~(1u << 24);
                    }
                }

                public bool _bit26_unknown
                {
                    get => (ulState & (1u << 25)) != 0;
                    set
                    {
                        if (value)
                            ulState |= (1u << 25);
                        else
                            ulState &= ~(1u << 25);
                    }
                }

                public bool devil_bonus
                {
                    get => (ulState & (1u << 26)) != 0;
                    set
                    {
                        if (value)
                            ulState |= (1u << 26);
                        else
                            ulState &= ~(1u << 26);
                    }
                }

                public byte _bit28_a_32_unknown
                {
                    get => (byte)((ulState >> 27) & 0x1F); // 5 bits: 27 a 31
                    set
                    {
                        ulState = (ulState & ~(0x1F << 27)) | ((uint)(value & 0x1F) << 27);
                    }
                }
            }


            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            public class uShotState
            {
                public uint ulState;

                public uint _bit1_unknown { get => (ulState & (1 << 0)) != 0 ? 1U : 0U; set { if (value != 0) ulState |= (1 << 0); else ulState &= ~(1U << 0); } }
                public uint tomahawk { get => (ulState & (1 << 1)) != 0 ? 1U : 0U; set { if (value != 0) ulState |= (1 << 1); else ulState &= ~(1U << 1); } }
                public uint spike { get => (ulState & (1 << 2)) != 0 ? 1U : 0U; set { if (value != 0) ulState |= (1 << 2); else ulState &= ~(1U << 2); } }
                public uint cobra { get => (ulState & (1 << 3)) != 0 ? 1U : 0U; set { if (value != 0) ulState |= (1 << 3); else ulState &= ~(1U << 3); } }
                public uint spin_front { get => (ulState & (1 << 4)) != 0 ? 1U : 0U; set { if (value != 0) ulState |= (1 << 4); else ulState &= ~(1U << 4); } }
                public uint spin_back { get => (ulState & (1 << 5)) != 0 ? 1U : 0U; set { if (value != 0) ulState |= (1 << 5); else ulState &= ~(1U << 5); } }
                public uint curve_left { get => (ulState & (1 << 6)) != 0 ? 1U : 0U; set { if (value != 0) ulState |= (1 << 6); else ulState &= ~(1U << 6); } }
                public uint curve_right { get => (ulState & (1 << 7)) != 0 ? 1U : 0U; set { if (value != 0) ulState |= (1 << 7); else ulState &= ~(1U << 7); } }
                public uint _bit9_unknown { get => (ulState & (1 << 8)) != 0 ? 1U : 0U; set { if (value != 0) ulState |= (1 << 8); else ulState &= ~(1U << 8); } }
                public uint _bit10_unknown { get => (ulState & (1 << 9)) != 0 ? 1U : 0U; set { if (value != 0) ulState |= (1 << 9); else ulState &= ~(1U << 9); } }
                public uint _bit11_unknown { get => (ulState & (1 << 10)) != 0 ? 1U : 0U; set { if (value != 0) ulState |= (1 << 10); else ulState &= ~(1U << 10); } }
                public uint sem_setas { get => (ulState & (1 << 11)) != 0 ? 1U : 0U; set { if (value != 0) ulState |= (1 << 11); else ulState &= ~(1U << 11); } }
                public uint power_shot { get => (ulState & (1 << 12)) != 0 ? 1U : 0U; set { if (value != 0) ulState |= (1 << 12); else ulState &= ~(1U << 12); } }
                public uint double_power_shot { get => (ulState & (1 << 13)) != 0 ? 1U : 0U; set { if (value != 0) ulState |= (1 << 13); else ulState &= ~(1U << 13); } }
                public uint _bit15_unknown { get => (ulState & (1 << 14)) != 0 ? 1U : 0U; set { if (value != 0) ulState |= (1 << 14); else ulState &= ~(1U << 14); } }
                public uint _bit16_unknown { get => (ulState & (1 << 15)) != 0 ? 1U : 0U; set { if (value != 0) ulState |= (1 << 15); else ulState &= ~(1U << 15); } }
                public uint _bit17_unknown { get => (ulState & (1 << 16)) != 0 ? 1U : 0U; set { if (value != 0) ulState |= (1 << 16); else ulState &= ~(1U << 16); } }
                public uint _bit18_unknown { get => (ulState & (1 << 17)) != 0 ? 1U : 0U; set { if (value != 0) ulState |= (1 << 17); else ulState &= ~(1U << 17); } }
                public uint _bit19_unknown { get => (ulState & (1 << 18)) != 0 ? 1U : 0U; set { if (value != 0) ulState |= (1 << 18); else ulState &= ~(1U << 18); } }
                public uint _bit20_unknown { get => (ulState & (1 << 19)) != 0 ? 1U : 0U; set { if (value != 0) ulState |= (1 << 19); else ulState &= ~(1U << 19); } }
                public uint club_wood { get => (ulState & (1 << 20)) != 0 ? 1U : 0U; set { if (value != 0) ulState |= (1 << 20); else ulState &= ~(1U << 20); } }
                public uint club_iron { get => (ulState & (1 << 21)) != 0 ? 1U : 0U; set { if (value != 0) ulState |= (1 << 21); else ulState &= ~(1U << 21); } }
                public uint club_pw_sw { get => (ulState & (1 << 22)) != 0 ? 1U : 0U; set { if (value != 0) ulState |= (1 << 22); else ulState &= ~(1U << 22); } }
                public uint club_putt { get => (ulState & (1 << 23)) != 0 ? 1U : 0U; set { if (value != 0) ulState |= (1 << 23); else ulState &= ~(1U << 23); } }
                public uint _bit25_a_32_unknown { get => (ulState & (255U << 24)) >> 24; set { ulState = (ulState & ~(255U << 24)) | ((value & 255U) << 24); } }

                public void Clear() => ulState = 0;
            }

            [field: MarshalAs(UnmanagedType.Struct)]
            public uDisplayState display = new uDisplayState();
            [field: MarshalAs(UnmanagedType.Struct)]
            public uShotState shot = new uShotState();
            public override string ToString()
            {

                string s = "Display State.\n\r";

                s += "OverDrive: " + Convert.ToString(display.over_drive) + " SuperPangya: " + Convert.ToString(display.super_pangya);
                s += " SpecialShot: " + Convert.ToString(display.special_shot) + " BeamImpact: " + Convert.ToString(display.beam_impact);
                s += " ChipIn17a199: " + Convert.ToString(display.chip_in_17_a_199) + " ChipIn200+: " + Convert.ToString(display.chip_in_200_plus);
                s += " LongPutt: " + Convert.ToString(display.long_putt) + " AcertoHole: " + Convert.ToString(display.acerto_hole);
                s += " ApproachShot: " + Convert.ToString(display.approach_shot) + " ChipInWithSpecialShot(BS,FS): " + Convert.ToString(display.chip_in_with_special_shot);
                s += " HappyBonus: " + Convert.ToString(display.happy_bonus) + " ClearBonus: " + Convert.ToString(display.clear_bonus) + " AztecBonus: " + Convert.ToString(display.aztec_bonus);
                s += " RecoveryBonus: " + Convert.ToString(display.recovery_bonus) + " ChipInWithoutSpecialShot: " + Convert.ToString(display.chip_in_without_special_shot);
                s += " BoundBonus: " + Convert.ToString(display.bound_bonus);
                s += " MascotBonusWithPangya: " + Convert.ToString(display.mascot_bonus_with_pangya) + " MascotBonusWithoutPangya: " + Convert.ToString(display.mascot_bonus_without_pangya);
                s += " SpecialBonusWithPangya: " + Convert.ToString(display.special_bonus_with_pangya);
                s += " SpecialBonusWithouPangya: " + Convert.ToString(display.special_bonus_without_pangya);
                s += " DevilBonus: " + Convert.ToString(display.devil_bonus) + Environment.NewLine;

                s += "Shot State.\n\r";

                s += "Tomahawk: " + Convert.ToString(shot.tomahawk) + " Spike: " + Convert.ToString(shot.spike);
                s += " Cobra: " + Convert.ToString(shot.cobra) + " SpinFront: " + Convert.ToString(shot.spin_front);
                s += " SpinBack: " + Convert.ToString(shot.spin_back) + " CurveLeft: " + Convert.ToString(shot.curve_left);
                s += " CurveRight: " + Convert.ToString(shot.curve_right) + " SemSetas: " + Convert.ToString(shot.sem_setas);
                s += " PowerShot: " + Convert.ToString(shot.power_shot) + " DoublePowerShot: " + Convert.ToString(shot.double_power_shot);
                s += " ClubWood: " + Convert.ToString(shot.club_wood) + " ClubIron: " + Convert.ToString(shot.club_iron);
                s += " ClubPWeSW: " + Convert.ToString(shot.club_pw_sw) + " ClubPutt: " + Convert.ToString(shot.club_putt);

                return s;
            }
        }
        [field: MarshalAs(UnmanagedType.Struct)]
        public stStateShot state_shot = new stStateShot();
        public short tempo_shot; // Acho que seja o tempo da tacada
        public byte grand_prix_penalidade; // Flag(valor) de penalidade do Grand Prix quando tem regras com penalidades

        public byte[] ToArray()
        {
            using (var p = new PangyaBinaryWriter())
            {
                p.Write(oid);
                p.Write(location.x);
                p.Write(location.y);
                p.Write(location.z);
                p.Write((byte)state);//tinha mexido antes
                p.Write(bunker_flag);
                p.Write(ucUnknown);
                p.Write(pang);
                p.Write(bonus_pang);
                p.Write(state_shot.display.ulState);
                p.Write(state_shot.shot.ulState);
                p.Write(tempo_shot);
                p.Write(grand_prix_penalidade);
                return p.GetBytes;
            }
        }

        public bool isMakeHole()
        {
            return state_shot.display.acerto_hole;
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]//87
    public class ShotEndLocationData
    {
        public ShotEndLocationData()
        { }

        public ShotEndLocationData(packet r)
        {
            porcentagem = r.ReadSingle();

            ball_velocity.x = r.ReadSingle();
            ball_velocity.y = r.ReadSingle();
            ball_velocity.z = r.ReadSingle();

            option = r.ReadByte();

            location.x = r.ReadSingle();
            location.y = r.ReadSingle();
            location.z = r.ReadSingle();

            wind_influence.x = r.ReadSingle();
            wind_influence.y = r.ReadSingle();
            wind_influence.z = r.ReadSingle();

            ball_point.x = r.ReadSingle();
            ball_point.y = r.ReadSingle();

            special_shot.ulSpecialShot = r.ReadUInt32();

            ball_rotation_spin = r.ReadSingle();
            ball_rotation_curve = r.ReadSingle();

            stUnknown = r.ReadByte();
            taco = r.ReadByte();

            power_factor = r.ReadSingle();
            power_club = r.ReadSingle();
            rotation_spin_factor = r.ReadSingle();
            rotation_curve_factor = r.ReadSingle();
            power_factor_shot = r.ReadSingle();

            time_hole_sync = r.ReadUInt32();
        }
        public void clear()
        {
            porcentagem = 0.0f;

            ball_velocity.clear();
            option = 0;
            location.clear();
            wind_influence.clear();
            ball_point.clear();
            special_shot.ulSpecialShot = 0;
            ball_rotation_spin = 0.0f;
            ball_rotation_curve = 0.0f;
            stUnknown = 0;
            taco = 0;
            power_factor = 0.0f;
            power_club = 0.0f;
            rotation_spin_factor = 0.0f;
            rotation_curve_factor = 0.0f;
            power_factor_shot = 0.0f;
            time_hole_sync = 0;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]//348
        public class stLocation
        {
            public void clear()
            {
                x = 0.0f;
                y = 0.0f;
                z = 0.0f;
            }
            public float x;
            public float y;
            public float z;
            public override string ToString()
            {
                return "X: " + Convert.ToString(x) + " Y: " + Convert.ToString(y) + " Z: " + Convert.ToString(z);
            }
            public byte[] ToArray()
            {
                using (var p = new PangyaBinaryWriter())
                {
                    p.WriteFloat(x);
                    p.WriteFloat(y);
                    p.WriteFloat(z);
                    return p.GetBytes;
                }
            }
        }
        [StructLayout(LayoutKind.Sequential, Pack = 1)]//348
        public class BallPoint
        {
            public void clear()
            {
                x = 0.0f;
                y = 0.0f;
            }
            public override string ToString()
            {
                return "X: " + Convert.ToString(x) + " Y: " + Convert.ToString(y);
            }
            public float x;
            public float y;
            public byte[] ToArray()
            {
                using (var p = new PangyaBinaryWriter())
                {
                    p.WriteFloat(x);
                    p.WriteFloat(y);
                    return p.GetBytes;
                }
            }
        }
        public float porcentagem;
        [field: MarshalAs(UnmanagedType.Struct)]
        public stLocation ball_velocity = new stLocation();
        public byte option;
        [field: MarshalAs(UnmanagedType.Struct)]
        public stLocation location = new stLocation();
        [field: MarshalAs(UnmanagedType.Struct)]
        public stLocation wind_influence = new stLocation();
        [field: MarshalAs(UnmanagedType.Struct)]
        public BallPoint ball_point = new BallPoint();
        [field: MarshalAs(UnmanagedType.Struct)]
        public uSpecialShot special_shot = new uSpecialShot(); // Tipo da tacada
        public float ball_rotation_spin;
        public float ball_rotation_curve; // Esse é a quantidade do efeito final depois de todos os algorithmos do pangya
        public byte stUnknown;
        public byte taco; // esta em baixo=> Club(possivelmente, esse de cima é o debaixo)
        public float power_factor;
        public float power_club;
        public float rotation_spin_factor;
        public float rotation_curve_factor;
        public float power_factor_shot;
        public uint time_hole_sync = 0;
        public override string ToString()
        {
            return "Porcentagem: " + Convert.ToString(porcentagem) + Environment.NewLine + "Option: " + Convert.ToString((ushort)option) + Environment.NewLine + "Ball Velocity (Initial): " + ball_velocity.ToString() + Environment.NewLine + "Location (Begin Shot): " + location.ToString() + Environment.NewLine + "Wind Influence: " + wind_influence.ToString() + Environment.NewLine + "Ball Point: " + ball_point.ToString() + Environment.NewLine + "Special Shot(Tipo da tacada): " + special_shot.ToString() + Environment.NewLine + "Ball Rotation (Spin): " + Convert.ToString(ball_rotation_spin) + Environment.NewLine + "Ball Rotation (Curva): " + Convert.ToString(ball_rotation_curve) + Environment.NewLine + "ucUnknown: " + Convert.ToString(stUnknown) + Environment.NewLine + "Taco: " + Convert.ToString((ushort)taco) + Environment.NewLine + "Power Factor (Full): " + Convert.ToString(power_factor) + Environment.NewLine + "Power Club(Range): " + Convert.ToString(power_club) + Environment.NewLine + "Rotation Spin Factor: " + Convert.ToString(rotation_spin_factor) + Environment.NewLine + "Rotation Curve Factor: " + Convert.ToString(rotation_curve_factor) + Environment.NewLine + "Power Factor (Shot): " + Convert.ToString(power_factor_shot) + Environment.NewLine + "Time Hole SYNC: " + Convert.ToString(time_hole_sync) + Environment.NewLine;
        }

        internal byte[] ToArray()
        {
            using (var p = new PangyaBinaryWriter())
            {
                p.Write(porcentagem);
                p.WriteBytes(ball_velocity.ToArray());
                p.Write(option);
                p.WriteBytes(location.ToArray());
                p.WriteBytes(wind_influence.ToArray());
                p.WriteBytes(ball_point.ToArray());
                p.WriteUInt32(special_shot.ulSpecialShot);
                p.Write(ball_rotation_spin);
                p.Write(ball_rotation_curve);
                p.Write(stUnknown);
                p.Write(taco);
                p.Write(power_factor);
                p.Write(power_club);
                p.Write(rotation_spin_factor);
                p.Write(rotation_curve_factor);
                p.Write(power_factor_shot);
                p.Write(time_hole_sync);
                return p.GetBytes;
            }
        }
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class DropItem
    {
        public enum eTYPE : ulong
        {
            NONE,
            NORMAL_QNTD,
            QNTD_MULTIPLE_500,
            COIN_EDGE_GREEN,
            COIN_GROUND,
            CUBE
        }
        public void clear()
        {
        }

        public uint _typeid = 0;
        public byte course;
        public byte numero_hole;
        public short qntd;
        public eTYPE type = new eTYPE();

        public DropItem()
        {
        }

        public DropItem(uint typeid, byte map, byte nhole, short _qntd, eTYPE _eTYPE)
        {
            _typeid = typeid;
            course = map;
            numero_hole = nhole;
            qntd = _qntd;
            type = _eTYPE;
        }
        public byte[] ToArray()
        {
            using (var p = new PangyaBinaryWriter())
            {
                p.WriteUInt32(_typeid);
                p.WriteByte(course);
                p.WriteByte(numero_hole);
                p.WriteInt16(qntd);
                p.WriteUInt64((ulong)type);
                return p.GetBytes;
            }
        }
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class DropItemRet
    {
        public DropItemRet()
        {
            clear();
        }

        public void clear()
        {

            if (v_drop.Any())
            {
                v_drop.Clear();
            }
        }
        public List<DropItem> v_drop = new List<DropItem>();
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class GameData
    {
        public GameData() => clear();

        public void clear()
        {
            tacada_num = 0;
            total_tacada_num = 0;
            score = 0;
            giveup = 0;
            bad_condute = 0; // Má conduta, 3 give ups o jogo kika o player
            penalidade = 0; // Penalidade do Grand Prix Rule
            pang = 0;
            bonus_pang = 0;
            pang_pang_battle = 0; // Pang do Pang Battle que o player ganhou ou perdeu
            pang_battle_run_hole = 0; // Player saiu do pang battle(-1) ou alguém saiu(+1)
            time_out = 0; // Count de time outs do player, 3 time outs o jogo kika o player
            exp = 0; // Exp que o player, ganhou no jogo
        }
        public int tacada_num = 0;
        public int total_tacada_num = 0;
        public int score = 0;
        public byte giveup = 0;

        public bool _giveup => giveup > 0;
        public uint bad_condute = 0; // Má conduta, 3 give ups o jogo kika o player
        public uint penalidade = 0; // Penalidade do Grand Prix Rule
        public ulong pang = 0;
        public ulong bonus_pang = 0;
        public long pang_pang_battle = 0; // Pang do Pang Battle que o player ganhou ou perdeu
        public int pang_battle_run_hole = 0; // Player saiu do pang battle(-1) ou alguém saiu(+1)
        public uint time_out = 0; // Count de time outs do player, 3 time outs o jogo kika o player
        public int exp = 0; // Exp que o player, ganhou no jogo
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class BarSpace
    {
        public BarSpace()
        {
            clear();
        }

        public void clear()
        {
            state = 0;
            point = new float[4];
        }

        // CLIENT → apenas muda estado
        public bool setState(byte _state)
        {  
            if (_state == 1) // início
            {
                startTick = UtilTime.GetTickCount(); // server tick
            }

            state = _state;
            return true;
        }

        // SERVER → define o ponto real
        public void setServerPoint(byte _state, float _point)
        {
            if (_state > 4)
                return;

            point[_state] = _point;
        }

        // ❌ Método antigo NÃO deve mais ser usado para impacto
        public bool setStateAndPoint(byte _state, float _point)
        {
            // Apenas para estados não críticos (ex: início visual)
            if (_state > 4)
                return false;

            state = (byte)((_state == 4) ? 3 /*Impact Zone Update*/ : _state);

            // Tentou atualizar o State, mas os valores eram diferente, 
            // mas teria que ser o mesmo por que ele só está com lag, pedindo para mandar o pacote de initShot
            if (_state == 4 && point[state] != _point)
                return false; 

            point[_state] = _point;
            return true;
        }
        public float CalculateServerPoint()
        {
            if (startTick == 0)
                return 0.0f;

            long now = UtilTime.GetTickCount();
            float elapsed = (now - startTick) / 1000.0f; // segundos

            // Velocidade da barra (ajustável por modo)
            float speed = 1.35f;

            // Movimento da barra (0 → 1 → 0)
            float cycle = elapsed * speed;
            float pos = cycle % 2.0f;

            if (pos > 1.0f)
                pos = 2.0f - pos;

            return Tools.Clamp(pos, 0.0f, 1.0f);
        }

        public byte getState() => state;

        public float[] getPoint() => point;

        public override string ToString()
        {
            return $"Start={point[0]} Power={point[1]} ImpactZone={point[2]}, Hit PangYa={point[3]}";
        }

        protected long startTick; // server time (ms)
        protected byte state;
        protected float[] point = new float[4];
    }

    public class BarSpaceDetector
    {
        private const int WindowSize = 6;           // quantidade de tacadas analisadas
        private const float MaxAllowedDiff = 0.03f; // tolerância humana
        private const int MinPerfectHits = 4;       // mínimo suspeito

        private readonly Queue<float> diffs = new Queue<float>();

        public void Add(float diff)
        {
            diffs.Enqueue(diff);

            if (diffs.Count > WindowSize)
                diffs.Dequeue();
        }

        public bool IsSuspicious(out float avgDiff)
        {
            avgDiff = 0;

            if (diffs.Count < WindowSize)
                return false;

            int perfectHits = 0;
            float sum = 0;

            foreach (var d in diffs)
            {
                sum += d;
                if (d <= MaxAllowedDiff)
                    perfectHits++;
            }

            avgDiff = sum / diffs.Count;

            // Regra principal
            return perfectHits >= MinPerfectHits && avgDiff <= MaxAllowedDiff;
        }

        public void Reset()
        {
            diffs.Clear();
        }
    }



    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class UsedItem
    {
        public void Dispose()
        {
        }
        public void clear()
        {
            if (v_passive.Any())
            {
                v_passive.Clear();
            }

            if (v_active.Any())
            {
                v_active.Clear();
            }
        }
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class Passive
        { 
            public uint _typeid = 0;
            public int count = 0;
            public Passive() { }
            public Passive(uint typeid, int _count)
            {
                _typeid = typeid;
                this.count = _count;
            }
        }
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class Active
        { 
            public void clear()
            {

                _typeid = 0;
                count = 0;

                if (v_slot.Any())
                {
                    v_slot.Clear();
                }
            }
            public uint _typeid = 0;
            public uint count = 0;
            public List<byte> v_slot = new List<byte>();
            public Active()
            { clear(); }
            public Active(uint typeid, uint _count, List<byte> _slot)
            {
                _typeid = typeid;
                count = _count;
                v_slot = _slot;
            }
        }
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class Rate
        {
            public void clear()
            {
                // Default value
                pang = 100;
                exp = 100;
                club = 100;
                drop = 100;
            }
            public uint pang = 0;
            public uint exp = 0;
            public uint club = 0;
            public uint drop = 0;
        }
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class ClubMastery
        {
            public void clear()
            {
            }
            public uint _typeid = 0;
            public uint count = 0;
            public float rate;
        }
        public Dictionary<uint, Passive> v_passive = new Dictionary<uint, Passive>();
        public Dictionary<uint, Active> v_active = new Dictionary<uint, Active>();
        public Rate rate = new Rate();
        public ClubMastery club = new ClubMastery();
    }

    // Effect Item Flag
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class uEffectFlag
    {
        public uEffectFlag(ulong _ull = 0)
        {
            ullFlag = (_ull);
        }
        public void clear()
        {
            ullFlag = 0;
        }
        public ulong ullFlag;
        public ulong NONE { get => (ullFlag & (1U << 0)) != 0 ? 1U : 0U; set { if (value != 0) ullFlag |= (1U << 0); else ullFlag &= ~(1U << 0); } }
        public ulong PIXEL { get => (ullFlag & (1U << 1)) != 0 ? 1U : 0U; set { if (value != 0) ullFlag |= (1U << 1); else ullFlag &= ~(1U << 1); } }
        public ulong PIXEL_BY_WIND_NO_ITEM { get => (ullFlag & (1U << 2)) != 0 ? 1U : 0U; set { if (value != 0) ullFlag |= (1U << 2); else ullFlag &= ~(1U << 2); } }
        public ulong PIXEL_OVER_WIND_NO_ITEM { get => (ullFlag & (1U << 3)) != 0 ? 1U : 0U; set { if (value != 0) ullFlag |= (1U << 3); else ullFlag &= ~(1U << 3); } }
        public ulong PIXEL_BY_WIND { get => (ullFlag & (1U << 4)) != 0 ? 1U : 0U; set { if (value != 0) ullFlag |= (1U << 4); else ullFlag &= ~(1U << 4); } }
        public ulong PIXEL_2 { get => (ullFlag & (1U << 5)) != 0 ? 1U : 0U; set { if (value != 0) ullFlag |= (1U << 5); else ullFlag &= ~(1U << 5); } }
        public ulong PIXEL_WITH_WEAK_WIND { get => (ullFlag & (1U << 6)) != 0 ? 1U : 0U; set { if (value != 0) ullFlag |= (1U << 6); else ullFlag &= ~(1U << 6); } }
        public ulong POWER_GAUGE_TO_START_HOLE { get => (ullFlag & (1U << 7)) != 0 ? 1U : 0U; set { if (value != 0) ullFlag |= (1U << 7); else ullFlag &= ~(1U << 7); } }
        public ulong POWER_GAUGE_MORE_ONE { get => (ullFlag & (1U << 8)) != 0 ? 1U : 0U; set { if (value != 0) ullFlag |= (1U << 8); else ullFlag &= ~(1U << 8); } }
        public ulong POWER_GAUGE_TO_START_GAME { get => (ullFlag & (1U << 9)) != 0 ? 1U : 0U; set { if (value != 0) ullFlag |= (1U << 9); else ullFlag &= ~(1U << 9); } }
        public ulong PAWS_NOT_ACCUMULATE { get => (ullFlag & (1U << 10)) != 0 ? 1U : 0U; set { if (value != 0) ullFlag |= (1U << 10); else ullFlag &= ~(1U << 10); } }
        public ulong SWITCH_TWO_EFFECT { get => (ullFlag & (1U << 11)) != 0 ? 1U : 0U; set { if (value != 0) ullFlag |= (1U << 11); else ullFlag &= ~(1U << 11); } }
        public ulong EARCUFF_DIRECTION_WIND { get => (ullFlag & (1U << 12)) != 0 ? 1U : 0U; set { if (value != 0) ullFlag |= (1U << 12); else ullFlag &= ~(1U << 12); } }
        public ulong COMBINE_ITEM_EFFECT { get => (ullFlag & (1U << 13)) != 0 ? 1U : 0U; set { if (value != 0) ullFlag |= (1U << 13); else ullFlag &= ~(1U << 13); } }
        public ulong SAFETY_CLIENT_RANDOM { get => (ullFlag & (1U << 14)) != 0 ? 1U : 0U; set { if (value != 0) ullFlag |= (1U << 14); else ullFlag &= ~(1U << 14); } }
        public ulong PIXEL_RANDOM { get => (ullFlag & (1U << 15)) != 0 ? 1U : 0U; set { if (value != 0) ullFlag |= (1U << 15); else ullFlag &= ~(1U << 15); } }
        public ulong WIND_1M_RANDOM { get => (ullFlag & (1U << 16)) != 0 ? 1U : 0U; set { if (value != 0) ullFlag |= (1U << 16); else ullFlag &= ~(1U << 16); } }
        public ulong PIXEL_BY_WIND_MIDDLE_DOUBLE { get => (ullFlag & (1U << 17)) != 0 ? 1U : 0U; set { if (value != 0) ullFlag |= (1U << 17); else ullFlag &= ~(1U << 17); } }
        public ulong GROUND_100_PERCENT_RONDOM { get => (ullFlag & (1U << 18)) != 0 ? 1U : 0U; set { if (value != 0) ullFlag |= (1U << 18); else ullFlag &= ~(1U << 18); } }
        public ulong ASSIST_MIRACLE_SIGN { get => (ullFlag & (1U << 19)) != 0 ? 1U : 0U; set { if (value != 0) ullFlag |= (1U << 19); else ullFlag &= ~(1U << 19); } }
        public ulong VECTOR_SIGN { get => (ullFlag & (1U << 20)) != 0 ? 1U : 0U; set { if (value != 0) ullFlag |= (1U << 20); else ullFlag &= ~(1U << 20); } }
        public ulong ASSIST_TRAJECTORY_SHOT { get => (ullFlag & (1U << 21)) != 0 ? 1U : 0U; set { if (value != 0) ullFlag |= (1U << 21); else ullFlag &= ~(1U << 21); } }
        public ulong PAWS_ACCUMULATE { get => (ullFlag & (1U << 22)) != 0 ? 1U : 0U; set { if (value != 0) ullFlag |= (1U << 22); else ullFlag &= ~(1U << 22); } }
        public ulong POWER_GAUGE_FREE { get => (ullFlag & (1U << 23)) != 0 ? 1U : 0U; set { if (value != 0) ullFlag |= (1U << 23); else ullFlag &= ~(1U << 23); } }
        public ulong SAFETY_RANDOM { get => (ullFlag & (1U << 24)) != 0 ? 1U : 0U; set { if (value != 0) ullFlag |= (1U << 24); else ullFlag &= ~(1U << 24); } }
        public ulong ONE_IN_ALL_STATS { get => (ullFlag & (1U << 25)) != 0 ? 1U : 0U; set { if (value != 0) ullFlag |= (1U << 25); else ullFlag &= ~(1U << 25); } }
        public ulong POWER_GAUGE_BY_MISS_SHOT { get => (ullFlag & (1U << 26)) != 0 ? 1U : 0U; set { if (value != 0) ullFlag |= (1U << 26); else ullFlag &= ~(1U << 26); } }
        public ulong PIXEL_BY_WIND_2 { get => (ullFlag & (1U << 27)) != 0 ? 1U : 0U; set { if (value != 0) ullFlag |= (1U << 27); else ullFlag &= ~(1U << 27); } }
        public ulong PIXEL_WITH_RAIN { get => (ullFlag & (1U << 28)) != 0 ? 1U : 0U; set { if (value != 0) ullFlag |= (1U << 28); else ullFlag &= ~(1U << 28); } }
        public ulong NO_RAIN_EFFECT { get => (ullFlag & (1U << 29)) != 0 ? 1U : 0U; set { if (value != 0) ullFlag |= (1U << 29); else ullFlag &= ~(1U << 29); } }
        public ulong PUTT_MORE_10Y_RANDOM { get => (ullFlag & (1U << 30)) != 0 ? 1U : 0U; set { if (value != 0) ullFlag |= (1U << 30); else ullFlag &= ~(1U << 30); } }
        public ulong UNKNOWN_31 { get => (ullFlag & (1U << 31)) != 0 ? 1U : 0U; set { if (value != 0) ullFlag |= (1U << 31); else ullFlag &= ~(1U << 31); } }
        public ulong MIRACLE_SIGN_RANDOM { get => (ullFlag & (1U << 32)) != 0 ? 1U : 0U; set { if (value != 0) ullFlag |= (1U << 32); else ullFlag &= ~(1U << 32); } }
        public ulong UNKNOWN_33 { get => (ullFlag & (1U << 33)) != 0 ? 1U : 0U; set { if (value != 0) ullFlag |= (1U << 33); else ullFlag &= ~(1U << 33); } }
        public ulong DECREASE_1M_OF_WIND { get => (ullFlag & (1U << 34)) != 0 ? 1U : 0U; set { if (value != 0) ullFlag |= (1U << 34); else ullFlag &= ~(1U << 34); } }

        public static uint enumToBitValue<TEnum>(TEnum enumValue) where TEnum : Enum
        {
            // Desloca o valor 1 para a posição indicada pelo valor do enum
            return (uint)(1 << Convert.ToInt32(enumValue));
        }

        public bool PixelEffect()
        {
            if (PAWS_NOT_ACCUMULATE == 1 || PAWS_ACCUMULATE == 1 || PIXEL_WITH_WEAK_WIND == 1 || PIXEL_WITH_RAIN == 1 || PIXEL_OVER_WIND_NO_ITEM == 1 || PIXEL_BY_WIND_MIDDLE_DOUBLE == 1 || PIXEL_BY_WIND_2 == 1 || PIXEL_BY_WIND == 1 || PIXEL == 1 || PIXEL_RANDOM == 1)
                return true;
            else
                return false;
                    }
    }

    // Bit value                                                          
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class PlayerGameInfo
    {
        public enum eCARD_WIND_FLAG : byte
        {
            NONE, // Nenhum Efeito
            NORMAL, // Normal, diminui 1m do vento, quando é 9m
            RARE, // Rare, diminui 1m de todos os ventos exceto menos 1m
            SUPER_RARE, // Super Rare, diminui 2m do vento, quando é 6m a 9m
            SECRET // Secret, diminui 1m do vento, quando é 2m a 5m e diminui 2m do vento, quando é 6m a 9m
        }
        public enum eFLAG_GAME : byte
        {
            PLAYING, // Ainda esta jogando
            TICKET_REPORT, // Saiu com ticket report, "Terminou o jogo"
            FINISH, // Jogador terminou o jogo
            BOT, // É Bot do Grand Prix
            QUIT, // Saiu do jogo
            END_GAME // Terminou o jogo, antes do jogar acabar
        }
        public enum eTEAM : byte
        {
            T_RED,
            T_BLUE,
            T_NONE
        }
        public PlayerGameInfo()
        {
            clear();
        }
        public virtual void clear()
        {

            uid = 0;
            oid = 0;
            level = 0;
            finish_load_hole = 0;
            finish_char_intro = 0;
            init_shot = 0;
            finish_shot = 0;
            finish_shot2 = 0;
            sync_shot_flag = 0;
            sync_shot_flag2 = 0;
            finish_hole = 0;
            finish_hole2 = 0;
            finish_hole3 = 0;
            finish_game = 0;
            finish_item_used = 0;
            premium_flag = false;
            trofel = 0;
            char_motion_item = 0;
            assist_flag = 0;
            enter_after_started = 0;
            progress_bar = 0;
            tempo = 0;
            power_shot = 0;
            club = 0;
            chat_block = 0;
            degree = 0;
            mascot_typeid = 0;

            init_first_hole = false;

            tick_sync_shot.clear();
            tick_sync_end_shot.clear();

            card_wind_flag = eCARD_WIND_FLAG.NONE;
            flag = eFLAG_GAME.PLAYING;
            team = eTEAM.T_NONE; // Valor Padrão

            effect_flag_shot.clear();
            item_active_used_shot = 0;
            earcuff_wind_angle_shot = 0.0f;
            boost_item_flag.clear();

            thi.clear();
            bar_space.clear();
            location.clear();
            data.clear();
            shot_data.clear();
            shot_data_for_cube.clear();
            shot_sync.clear();
            ui.clear();
            drop_list.clear();
            used_item.clear();
            progress.clear();
            medal_win = new uMedalWin();

            typeing = -1;
            hole = 255;
        }
        [StructLayout(LayoutKind.Sequential, Pack = 1)]

        public class stProgress
        {
            public void clear()
            {
                hole = -1;
            }
            public bool isGoodScore()
            {

                for (var i = 0; i < 18; ++i)
                {
                    if (score[i] > 0)
                    {
                        return false;
                    }
                }

                return true;
            }
           public int getBestRecovery()
{
    int first = 0;
    int last = 0;

    for (int i = 0; i < 9; ++i) first += score[i];
    for (int i = 9; i < 18; ++i) last += score[i];

    // Se o objetivo é saber a diferença entre as duas metades:
    return first - last; 
}
            public short hole; // Hole Atual
            public float best_chipin;
            public float best_long_puttin;
            public float best_drive;
            [field: MarshalAs(UnmanagedType.ByValArray)]
            public int[] finish_hole = new int[18]; // Flag para verificar se o player terminou o hole
            [field: MarshalAs(UnmanagedType.ByValArray)]
            public int[] par_hole = new int[18]; // Par do hole, [18 Holes o máximo de um jogo]negativo
            [field: MarshalAs(UnmanagedType.ByValArray)] public int[] tacada = new int[18]; // Tacadas do hole, [18 Holes o máximo de um jogo](negativo)
            [field: MarshalAs(UnmanagedType.ByValArray)] public int[] score = new int[18]; // Score do hole, [18 Holes o máximo de um jogo](negativo)
        }
        [StructLayout(LayoutKind.Sequential, Pack = 1)]

        public class stTreasureHunterInfo
        {
            public stTreasureHunterInfo()
            {
                clear();
            }
            public void Dispose()
            {
            }
            public void clear()
            {

                all_score = 0;
                par_score = 0;
                birdie_score = 0;
                eagle_score = 0;

                treasure_point = 0;

                if (v_item.Any())
                {
                    v_item.Clear();
                }
            }
            public int getPoint(int _tacada, int _par_hole)
            {
                int point = all_score;

                if (_tacada == 1) // HIO
                {
                    return point;
                }

                var score = (_tacada - _par_hole);

                switch (score)
                {
                    case 0: // Par
                        point += par_score;
                        break;
                    case -1: // Birdie
                        point += birdie_score;
                        break;
                    case -2: // Eagle
                        point += eagle_score;
                        break;
                }

                return point;
            }
            public static stTreasureHunterInfo operator +(stTreasureHunterInfo lhs, stTreasureHunterInfo rhs)
            {
                if (lhs == null || rhs == null) return lhs;  // Verifica se algum objeto é null, e retorna o lhs

                lhs.all_score += rhs.all_score;
                lhs.par_score += rhs.par_score;
                lhs.birdie_score += rhs.birdie_score;
                lhs.eagle_score += rhs.eagle_score;
                lhs.treasure_point += rhs.treasure_point;
                return lhs; // Retorna a instância lhs após a soma
            }
            public uint treasure_point = 0; // Treasure Hunter point do player no game
            public List<TreasureHunterItem> v_item = new List<TreasureHunterItem>(); // Treasure Hunter Item
            public byte all_score;
            public byte par_score;
            public byte birdie_score;
            public byte eagle_score;
        }
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class TickTimeSync
        {
            public TickTimeSync()
            {
                clear();
            }

            public void clear()
            {
                count = 0;
                active = false;
                tick = 0;
            }

            public byte count;
            public bool active;
            public ulong tick;

            private double TicksPerSecond = Stopwatch.Frequency;

            public void Start()
            {
                tick = (ulong)Stopwatch.GetTimestamp();
                active = true;
                count = 0;
            }

            public void Stop()
            {
                active = false;
            }

            public double ElapsedSeconds
            {
                get
                {
                    if (tick == 0) return double.MaxValue;
                    return (Stopwatch.GetTimestamp() - (long)tick) / TicksPerSecond;
                }
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class uBoostItemFlag
        {
            public void clear()
            {
                ucFlag = 0;
            }
            public sbyte ucFlag;
            public uint pang { get => (ucFlag & (1 << 0)) != 0 ? 1U : 0U; set { if (value != 0) ucFlag |= (1 << 0); else ucFlag &= (sbyte)~(1 << 0); } }
            public uint pang_nitro { get => (ucFlag & (1 << 1)) != 0 ? 1U : 0U; set { if (value != 0) ucFlag |= (1 << 1); else ucFlag &= (sbyte)~(1 << 1); } }
            public uint exp { get => (ucFlag & (1 << 2)) != 0 ? 1U : 0U; set { if (value != 0) ucFlag |= (1 << 2); else ucFlag &= (sbyte)~(1 << 2); } }

        }
        public uint uid = 0;
        public int oid = -1;
        public byte level;
        public byte hole = 255; // Número do Hole que o player está

        public bool init_first_hole = true; // Flag que guarda quando o player inicializou o primeiro hole do jogo

        public byte finish_load_hole = 0;

        public byte finish_char_intro = 0;

        public byte init_shot = 0;

        public byte finish_shot = 0;

        public byte finish_shot2 = 0;

        public byte finish_hole = 0; // Usa no Grand Prix, flag de sincronização de hole conluído para trocar para o prox

        public byte finish_hole2 = 0; // Usa no Grand Prix, flag de sincronização do tempo do hole do player, para não dá time out depois que ele concluiu o hole

        public byte finish_hole3 = 0; // Usa no Grand Prix, flag de sincronização se o player já enviou o pacote de finalizar o hole antes

        public byte sync_shot_flag = 0;

        public byte sync_shot_flag2 = 0;

        public byte finish_game = 0; // Terminou o jogo

        public byte assist_flag = 0; // 0 não está com assist ligado, 1 está com assist ligado

        public byte char_motion_item = 0; // Está com intro de character Equipado

        public bool premium_flag = false; // 1 Player é um usuário premium, 0 player normal

        public byte enter_after_started = 0; // Entrou no Jogo depois de ele ter começado

        public byte finish_item_used = 0; // 1 Player já finalizou os itens usados no jogo, não finalizar de novo se ele já estiver finalizado
        public byte trofel; // Trofel que ele ganhou, 1 ouro, 2 prate, 3 bronze
        public ushort progress_bar;
        public uint tempo = 0;
        public byte power_shot;
        public byte club; // Taco
        public short typeing; // Escrevendo
        public byte chat_block; // Chat Block
        public ushort degree; // Degree(Graus) do player no Hole
        public uint mascot_typeid = 0; // Typeid do Mascot equipado
        public uint item_active_used_shot = 0; // O item Active usado na tacada
        public float earcuff_wind_angle_shot; // Ângulo que o efeito earcuff ativou na tacada para o player
        public uEffectFlag effect_flag_shot = new uEffectFlag(); // Effect Flag Shot(tacada), Wind 1m, Safety, Patinha e etc
        public eFLAG_GAME flag = new eFLAG_GAME(); // Flag se acabou o camp, ainda esta jogando, quitou, saiu, ou o jogo terminou pro ele
        public uBoostItemFlag boost_item_flag = new uBoostItemFlag(); // Flag que Exibe os icon de quais boost item o player está usando
        public eCARD_WIND_FLAG card_wind_flag = new eCARD_WIND_FLAG(); // Card Wind Flag
        public stTreasureHunterInfo thi = new stTreasureHunterInfo(); // Treasure Hunter Info do player, esse é que aumenta com card
        public eTEAM team = new eTEAM(); // Team(time) que o player está, antes usado no tourney de time, agora só usado no Match
        public TickTimeSync tick_sync_shot = new TickTimeSync(); // Tick de quando o player recebeu o pacote para ele enviar o pacote sync shot
        public TickTimeSync tick_sync_end_shot = new TickTimeSync(); // Tick de quando o player enviou o pacote de termino de tacada (FinishShot)
        public BarSpace bar_space = new BarSpace();
        public BarSpaceDetector bar_space_analize = new BarSpaceDetector();
        public Location location = new Location();
        public GameData data = new GameData();
        public ShotDataEx shot_data = new ShotDataEx();
        public ShotEndLocationData shot_data_for_cube = new ShotEndLocationData(); // Dados que vou usar para os locais de spaw do Spinning Cube
        public ShotSyncData shot_sync = new ShotSyncData();
        public UserInfoEx ui = new UserInfoEx();
        public DropItemRet drop_list = new DropItemRet(); // Drop List do player
        public UsedItem used_item = new UsedItem(); // Item usado no jogo
        public stProgress progress = new stProgress(); // Progresso do jogo, tacadas e score
        public SYSTEMTIME time_finish = new SYSTEMTIME(); // Tempo que acabou o game
        public uMedalWin medal_win = new uMedalWin(); // Medal que Ganhou no Jogo
        public AchievementSystem sys_achieve = new AchievementSystem(); // System of Achievement of Player
        public AlwaysPangyaDetector alwaysDetect = new AlwaysPangyaDetector();
    }
    public class AlwaysPangyaDetector
    {
        private const float PANGYA_MIN = 135f;
        private const float PANGYA_MAX = 142f;

        private const float PERFECT_CENTER_MIN = 138f;
        private const float PERFECT_CENTER_MAX = 140f;

        private const float MAX_HUMAN_VARIANCE = 1.0f;
        private const int MIN_SHOTS_FOR_ANALYSIS = 8;

        private class ShotStats
        {
            public int totalShots;
            public int pangyaHits;
            public int perfectCenterHits;

            public float minImpact = float.MaxValue;
            public float maxImpact = float.MinValue;
        }

        private readonly Dictionary<uint, ShotStats> _stats = new Dictionary<uint, ShotStats>();

        public void Analyze(Player session, ShotDataEx sd, uEffectFlag uEffect)
        {
            if (session == null || sd == null)
                return;

            uint uid = session.m_pi.uid;

            bool isclubValid = sd.club == 0x0E || sd.club == 0x0D || sd.club == 0x0C || sd.club == 0x0B || sd.club == 0x0A;

            if (!_stats.TryGetValue(uid, out ShotStats stats))
            {
                stats = new ShotStats();
                _stats[uid] = stats;
            }

            float impact = sd.bar_point[1];
            bool isPangya = sd.acerto_pangya_flag == 4;

            stats.totalShots++;

            // Atualiza min/max
            if (impact < stats.minImpact) stats.minImpact = impact;
            if (impact > stats.maxImpact) stats.maxImpact = impact;

            // -------------------------
            // CHECK 1: Pangya fora da zona física
            // -------------------------
            if (!uEffect.PixelEffect() && !isclubValid && isPangya && (impact < PANGYA_MIN || impact > PANGYA_MAX))
            {
                Flag(session,
                    "INVALID_PANGYA_RANGE",
                    $"Impact={impact}");
                return;
            }

            if (!isPangya)
                return;

            stats.pangyaHits++;

            // -------------------------
            // CHECK 2: Centro perfeito repetido
            // -------------------------
            if (impact >= PERFECT_CENTER_MIN && impact <= PERFECT_CENTER_MAX)
            {
                stats.perfectCenterHits++;
            }

            if (!uEffect.PixelEffect() && !isclubValid && stats.totalShots >= MIN_SHOTS_FOR_ANALYSIS &&
                stats.perfectCenterHits == stats.pangyaHits &&
                stats.pangyaHits >= MIN_SHOTS_FOR_ANALYSIS)
            {
                Flag(session,
                    "PERFECT_CENTER_ABUSE",
                    $"Hits={stats.pangyaHits} ImpactRange={stats.minImpact:F2}-{stats.maxImpact:F2}");
                return;
            }

            // -------------------------
            // CHECK 3: Variância impossível
            // -------------------------
            if (!uEffect.PixelEffect() && !isclubValid && stats.totalShots >= MIN_SHOTS_FOR_ANALYSIS)
            {
                float variance = stats.maxImpact - stats.minImpact;

                if (variance < MAX_HUMAN_VARIANCE)
                {
                    Flag(session,
                        "NO_HUMAN_VARIANCE",
                        $"Variance={variance:F3}");
                    return;
                }
            }

            // -------------------------
            // CHECK 4: Pangya rápido demais
            // -------------------------
            if (!uEffect.PixelEffect() && !isclubValid && sd.time_shot < 10000)
            {
                Flag(session,
                    "FAST_PERFECT_RELEASE",
                    $"TimeShot={sd.time_shot} Impact={impact}");
                return;
            }
        }

        private void Flag(Player session, string reason, string detail)
        {
            _smp.message_pool.getInstance().push(
                new message(
                    $"[AlwaysPangyaDetect][{reason}] UID={session.m_pi.uid} {detail}",
                    type_msg.CL_ONLY_CONSOLE_DEBUG
                )
            );

            // Aqui você decide:
            // - só logar
            // - marcar flag no player
            // - kickar
            // - enviar pro GameGuard
        }

        public void Clear(uint uid)
        {
            _stats.Remove(uid);
        }

        public void ClearAll()
        {
            _stats.Clear();
        }
    }
        // Ticket Report Info
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public class TicketReportInfo
    {
        public TicketReportInfo()
        {
            clear();
        }

        public void clear()
        {
            id = -1;
            v_dados.Clear();
        }
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public class stTicketReportDados
        {
            public void clear()
            {
            }
            public uint uid = 0;
            public int score = 0; 
            public uMedalWin medal = new uMedalWin();
            public byte trofel;
            public ulong pang = 0;
            public ulong bonus_pang = 0;
            public int exp = 0;
            public uint mascot_typeid = 0;
            public uint flag_item_pang = 0;
            public uint premium = 0;
            public uint state = 0; 
            public SYSTEMTIME finish_time = new SYSTEMTIME();
        }
        public int id = 0;
        public List<stTicketReportDados> v_dados = new List<stTicketReportDados>();
    }

    // Enter After Start Info
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public class EnterAfterStartInfo
    {
        public void clear()
        {
        }


        [field: MarshalAs(UnmanagedType.ByValArray)]
        public byte[] tacada = new byte[18]; // 18 Holes
        [field: MarshalAs(UnmanagedType.ByValArray)]
        public int[] score = new int[18]; // 18 Holes
        [field: MarshalAs(UnmanagedType.ByValArray)]
        public ulong[] pang = new ulong[18]; // 18 Holes
        public int request_oid = -1;
        public uint owner_oid = 0;

        public byte[] ToArray()
        {
            using (var p = new PangyaBinaryWriter())
            {
                p.WriteBytes(tacada);
                p.WriteInt32(score);
                p.WriteUInt64(pang);
                p.WriteInt32(request_oid);
                p.WriteUInt32(owner_oid);
                return p.GetBytes;
            }
        }
    }


    public class PlayerOrderTurnCtx
    {
        public PlayerOrderTurnCtx()
        {
            clear();
        }
        public PlayerOrderTurnCtx(PlayerGameInfo _pgi, HoleManager _hole)
        {
            this.pgi = _pgi;
            this.hole = _hole;
        }
        public void clear()
        {
            pgi = null;
        }
        public PlayerGameInfo pgi;
        public HoleManager hole;
    }

    // Table Rate Voice And Effect On Versus
    public class TableRateVoiceAndEffect
    {
        public enum eTYPE : byte
        {
            NONE,
            W_BIGBONGDARI,
            R_BIGBONGDARI,
            VOICE_CLUB
        }

        public string name;
        public eTYPE type;
        [field: MarshalAs(UnmanagedType.ByValArray)]
        public byte[] table = new byte[100];
        public TableRateVoiceAndEffect()
        {
            clear();
        }
        public TableRateVoiceAndEffect(string _name, eTYPE _type)
        {
            this.name = _name;
            this.type = _type;
            randomTable();
        }

        public void clear()
        {
            name = "";
            type = eTYPE.NONE;
        }
        public void randomTable()
        {
            ushort min_value = 0;

            if (type == eTYPE.VOICE_CLUB)
                min_value = 1;

            var rnd = new Random();
            for (var i = 0; i < 100; ++i)
            {
                var randValue = rnd.Next();
                table[i] = (byte)(min_value + (randValue % (4 - min_value)));
            }
        }
    }

    public class TreasureHunterVersusInfo
    {
        public TreasureHunterVersusInfo()
        {
            clear();
        }
        public void clear()
        {

            all_score = 0;
            par_score = 0;
            birdie_score = 0;
            eagle_score = 0;

            treasure_point = 0;

            if (v_item.Any())
            {
                v_item.Clear();
            }
        }
        public void increment(TreasureHunterVersusInfo other)
        {
            all_score += other.all_score;
            par_score += other.par_score;
            birdie_score += other.birdie_score;
            eagle_score += other.eagle_score;
        }
        
        public void increment(PlayerGameInfo.stTreasureHunterInfo _thi)
        {
            all_score += _thi.all_score;
            par_score += _thi.par_score;
            birdie_score += _thi.birdie_score;
            eagle_score += _thi.eagle_score;
        }

        public uint getPoint(int _tacada, int _par_hole)
        {
            byte point = all_score;

            if (_tacada == 1) // HIO
            {
                return point;
            }

            var score = (_tacada - _par_hole);

            switch (score)
            {
                case 0: // Par
                    point += par_score;
                    break;
                case (sbyte)-1: // Birdie
                    point += birdie_score;
                    break;
                case (sbyte)-2: // Eagle
                    point += eagle_score;
                    break;
            }

            return point;
        }

        public class _stTreasureHunterItem
        {
            public _stTreasureHunterItem()
            {
            }
            public _stTreasureHunterItem(uint _uid, TreasureHunterItem _thi)
            {

                AddItem(_uid, _thi);
            }

            public void AddItem(uint _uid, TreasureHunterItem item)
            {
                uid = _uid;
                this.thi = (item);

            }

            public void AddItem(TreasureHunterItem item)
            {
                this.thi = (item);
            }

            public uint uid { get; set; } = 0; // Player UID
            public TreasureHunterItem thi = new TreasureHunterItem();
        }
        public uint treasure_point = 0; // Treasure Hunter point do player no game
        public List<_stTreasureHunterItem> v_item { get; set; } = new List<_stTreasureHunterItem>(); // Treasure Hunter Item
        public byte all_score;
        public byte par_score;
        public byte birdie_score;
        public byte eagle_score;

    }

    // Ret Finish Shot
    public class RetFinishShot
    {
        public RetFinishShot()
        {
            clear();
        }
        public void clear()
        {
            p = new Player();
        }
        public int ret;
        public Player p;
    }

    // Holes rain count
    public class HolesRain
    {
        public HolesRain()
        {
            clear();
        }
        public void clear()
        {
        }
        public byte getCountHolesRainBySeq(uint _seq)
        {

            // Sequência de hole valor errado
            if (_seq < 1 || _seq > 18)
            {
                return 0;
            }

            byte sum = 0;
            for (uint i = 0; i < _seq; i++)
            {
                sum += rain[i];
            }

            return sum;
        }
        public byte getCountHolesRain()
        {
            byte sum = 0;
            for (uint i = 0; i < rain.Length; i++)
            {
                sum += rain[i];
            }

            return sum;
        }
        public void setRain(uint _index, byte _value)
        {

            // Index invalido
            if ((int)_index < 0 || _index >= 18)
            {
                return;
            }

            rain[_index] = _value;
        }
        [field: MarshalAs(UnmanagedType.ByValArray)]
        protected byte[] rain = new byte[18]; // Máximo número de holes de um jogo
    }

    // Consecutivos Holes Rain(Recovery) Tempo Ruim
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class ConsecutivosHolesRain
    {
        public ConsecutivosHolesRain()
        {
            clear();
        }
        public void clear()
        {
        }
        public bool isValid()
        {
            return _4_pluss_count.getCountHolesRain() > 0 || _3_count.getCountHolesRain() > 0 || _2_count.getCountHolesRain() > 0;
        }
        [field: MarshalAs(UnmanagedType.Struct)]
        public HolesRain _4_pluss_count = new HolesRain();
        [field: MarshalAs(UnmanagedType.Struct)]
        public HolesRain _3_count = new HolesRain();
        [field: MarshalAs(UnmanagedType.Struct)]
        public HolesRain _2_count = new HolesRain();
    }
}
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                    