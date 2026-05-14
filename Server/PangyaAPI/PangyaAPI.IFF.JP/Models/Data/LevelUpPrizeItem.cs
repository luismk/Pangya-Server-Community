using System;
using System.Runtime.InteropServices;
using System.Text;

namespace PangyaAPI.IFF.JP.Models.Data
{
    #region Struct LevelUpItem.iff
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public class LevelUpPrizeItem : ICloneable
    {
        public byte Active { get; set; }
        [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 33)]
        byte[] NameInBytes { get; set; }//8 start position
        public ushort Level { get; set; }
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class Reward
        {
            [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            public uint[] TypeID { get; set; }
            [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            public uint[] Quantity { get; set; }
            [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            public uint[] Time { get; set; }
        }
        [field: MarshalAs(UnmanagedType.Struct)]
        public Reward reward { get; set; }
        [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 132)]
        byte[] DescriptionInBytes { get; set; }//8 start position
        public string Description//correcao para não causar conflito ao escrever
        {
            get => Encoding.GetEncoding("Shift_JIS").GetString(DescriptionInBytes).Replace("\0", "");
            set => DescriptionInBytes = Encoding.GetEncoding("Shift_JIS").GetBytes(value.PadRight(132, '\0'));
        }
        public string Name//correcao para não causar conflito ao escrever
        {
            get => Encoding.GetEncoding("Shift_JIS").GetString(NameInBytes).Replace("\0", "");
            set => NameInBytes = Encoding.GetEncoding("Shift_JIS").GetBytes(value.PadRight(33, '\0'));
        }

        public object Clone()
        {
            return MemberwiseClone();
        }
    }
    #endregion

}
