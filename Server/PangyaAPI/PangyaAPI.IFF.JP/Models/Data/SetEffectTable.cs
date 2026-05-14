using System;
using System.Runtime.InteropServices;
using PangyaAPI.Utilities.Models;
namespace PangyaAPI.IFF.JP.Models.Data
{
    #region Struct SetEffectTable.iff
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class SetEffectTable : ICloneable
    {
        public SetEffectTable()
        {
        }
        public SetEffectTable(PangyaBinaryReader reader)
        {
            Index = reader.ReadUInt32();

            effect = new Effect();
            effect.effect = new uint[3];
            for (int i = 0; i < 3; i++)
            {
                effect.effect[i] = (uint)reader.ReadUInt32();
            }
            effect.Type = new uint[3];
            for (int i = 0; i < 3; i++)
            {
                effect.Type[i] = (uint)reader.ReadUInt32();
            }

            item = new Item();
            item.ID = new uint[5];
            for (int i = 0; i < 5; i++)
            {
                item.ID[i] = reader.ReadUInt32();
            }
            item.Active = new byte[5];
            for (int i = 0; i < 5; i++)
            {
                item.Active[i] = reader.ReadByte();
            }

            ucUnknown = new byte[11];
            for (int i = 0; i < 11; i++)
            {
                ucUnknown[i] = reader.ReadByte();
            }

            Slot = new short[5];
            for (int i = 0; i < 5; i++)
            {
                Slot[i] = reader.ReadInt16();
            }

            Effect_Add_Power = reader.ReadInt16();
        }

        public uint Index { get; set; }
        [field: MarshalAs(UnmanagedType.Struct)]
        public Effect effect { get; set; }
        [field: MarshalAs(UnmanagedType.Struct)]
        public Item item { get; set; }
        [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 11)]
        public byte[] ucUnknown { get; set; }
        [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
        public short[] Slot { get; set; }
        public short Effect_Add_Power { get; set; }   // Força sem penalidade

        public object Clone()
        {
            return MemberwiseClone();
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class Effect
        {
            [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public uint[] effect { get; set; } // eEFFECT = Effect[0~2] é o da descrição em cima
            [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public uint[] Type { get; set; }// eEFFECT_TYPE = Type[0~2], 2 Game, 4 Room e 8 Lounge
        }
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class Item
        {
            [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
            public uint[] ID { get; set; }
            [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
            public byte[] Active { get; set; }

        }


        public uint getID(int idx)
        {
            return item.ID[idx];
        }
        public bool IsActive(int idx)
        {
            return item.Active[idx] > 0;
        }
    }
    #endregion
}
