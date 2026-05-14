using System;
using System.Linq;
using System.Runtime.InteropServices;
using PangyaAPI.IFF.JP.Models.Flags;
using PangyaAPI.Utilities.Models;

namespace PangyaAPI.IFF.JP.Models.Data
{
    #region Struct MemorialRareItem.iff
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public class MemorialShopRareItem : ICloneable
    {
        public uint Active { get; set; }
        [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 8)]
        public class Gacha
        {
            public uint Number { get; set; }
            public uint Count { get; set; }
        }
        [field: MarshalAs(UnmanagedType.Struct, SizeConst = 8)]
        public Gacha gacha { get; set; }
        public uint ID { get; set; }
        public uint Probabilities { get; set; }
        public MemorialRareType RareType { get; set; }// Tipo Raro, EX: -1 - 0 normal, 1 - 2 raro, 3 - 4 Super raro

        [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
        public int[] filter { get; set; }
        [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 28)]
        public byte[] Null_Bytes { get; set; }
        public MemorialShopRareItem()
        { }
        public MemorialShopRareItem(ref PangyaBinaryReader reader)
        {
            Active = reader.ReadUInt32();
            gacha = reader.Read<Gacha>();
            ID = reader.ReadUInt32();
            Probabilities = reader.ReadUInt32();
            RareType = (MemorialRareType)reader.ReadInt32();
            filter = reader.ReadInt32Array(10).ToArray();
            // Lendo os bytes nulos
            Null_Bytes = reader.ReadBytes(24);
        }
        public object Clone()
        {
            return MemberwiseClone();
        }
    }
    #endregion
}
