using PangyaAPI.IFF.JP.Models.Flags;
using PangyaAPI.Network.PangyaPacket;
using PangyaAPI.Utilities.Models;
using System;
using System.Runtime.InteropServices;

namespace Pangya_GameServer.Models
{

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class stInitHole
    {
        public void clear()
        {
        }

        public stInitHole ToRead(packet _packet)
        {
            numero = _packet.ReadByte();
            option = _packet.ReadUInt32();
            ulUnknown = _packet.ReadUInt32();
            par = _packet.ReadByte();
            tee = new stXZLocation
            {
                x = _packet.ReadSingle(),
                z = _packet.ReadSingle()
            };
            pin = new stXZLocation
            {
                x = _packet.ReadSingle(),
                z = _packet.ReadSingle()
            };

            return this;
        }

        public byte numero { get; set; }
        public uint option { get; set; }
        public uint ulUnknown { get; set; }
        public byte par { get; set; }
        [field: MarshalAs(UnmanagedType.Struct)]
        public stXZLocation tee { get; set; } = new stXZLocation();
        [field: MarshalAs(UnmanagedType.Struct)]
        public stXZLocation pin { get; set; } = new stXZLocation();
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4, Size = 4)]
    public class uArrow
    {
        public uint ulArrow;
        public byte bits0_4;    // bits: cima, baixo, esquerda, direita, azul_claro
        public byte bit6_a_13;
        public byte bit14_a_21;
        public byte bit22_a_29;
        public uArrow(uint ul = 0)
        {
            ulArrow = ul;
            bits0_4 = (byte)ul;
        }
        public void clear()
        {
            ulArrow = 0;
        }

        // Propriedades para acessar os bits individualmente
        public bool cima
        {
            get => (bits0_4 & (1 << 0)) != 0;
            set => bits0_4 = value ? (byte)(bits0_4 | (1 << 0)) : (byte)(bits0_4 & ~(1 << 0));
        }

        public bool baixo
        {
            get => (bits0_4 & (1 << 1)) != 0;
            set => bits0_4 = value ? (byte)(bits0_4 | (1 << 1)) : (byte)(bits0_4 & ~(1 << 1));
        }

        public bool esquerda
        {
            get => (bits0_4 & (1 << 2)) != 0;
            set => bits0_4 = value ? (byte)(bits0_4 | (1 << 2)) : (byte)(bits0_4 & ~(1 << 2));
        }

        public bool direita
        {
            get => (bits0_4 & (1 << 3)) != 0;
            set => bits0_4 = value ? (byte)(bits0_4 | (1 << 3)) : (byte)(bits0_4 & ~(1 << 3));
        }

        public bool azul_claro
        {
            get => (bits0_4 & (1 << 4)) != 0;
            set => bits0_4 = value ? (byte)(bits0_4 | (1 << 4)) : (byte)(bits0_4 & ~(1 << 4));
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public class stActiveCutin
    {
        public void clear()
        {
        }
        public uint uid { get; set; }
        public uint tipo { get; set; }
        public ushort opt { get; set; }
        public uint char_typeid { get; set; } // Aqui pode ter o typeid do cutin também
        public byte active { get; set; } // Active acho, sempre com valor 1 que peguei, 1 quando é id do character, 0 quando é o typeid do Cutin
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public class stRing
    {
        public void clear()
        {
        }
        public uint _typeid { get; set; }
        public uint effect_value { get; set; } // valor do efeito
        public byte efeito { get; set; }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class stRingGround
    {
        public void clear()
        {
        }
        public bool isValid()
        {
            return (ring[0] != 0 && ring[1] != 0);
        }
        public AbilityEffect efeito { get; set; }
        [MarshalAs(UnmanagedType.ByValArray)]
        public uint[] ring = new uint[2]; // 2 Anel, "Set"(Conjunto)
        public uint option { get; set; }
        public byte[] ToArray()
        {
            using (var p = new PangyaBinaryWriter())
            {
                p.WriteUInt32(efeito);
                p.WriteUInt32(ring);
                p.WriteUInt32(option);
                return p.GetBytes;
            }
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public class stRingPowerGagueJP
    {
        public void clear()
        {

        }
        public bool isValid()
        {
            return (ring[0] != 0 && ring[1] != 0);
        }
        public uint efeito { get; set; }
        [MarshalAs(UnmanagedType.ByValArray)]
        public uint[] ring = new uint[2]; // 2 Ring é Conjuntos de Aneis
        public uint option { get; set; }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public class stEarcuff
    {
        public void clear()
        {

        }
        public uint _typeid { get; set; }
        public byte angle { get; set; } // Sentido do angulo, back(Angel) ou front(Devil)
        public float x_point_angle { get; set; }
    }


    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class stMarkerOnCourse
    {

        public float x { get; set; }
        public float y { get; set; }
        public float z { get; set; }
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class Medal
    {
        public Medal(uint _ul = 0u)
        {
            clear();
        }
        public void clear()
        {
            oid = -1;
            item_typeid = 0;
        }
        public int oid = new int();
        public uint item_typeid = new uint();

        public byte[] ToArray()
        {
            using (var p = new PangyaBinaryWriter())
            {
                p.WriteInt32(oid);
                p.WriteUInt32(item_typeid);
                return p.GetBytes;
            }
        }
    }
}
