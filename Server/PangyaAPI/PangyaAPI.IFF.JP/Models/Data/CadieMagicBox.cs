using System;
using System.Runtime.InteropServices;
using System.Text;
using PangyaAPI.Utilities;

namespace PangyaAPI.IFF.JP.Models.Data
{
    #region Struct CadieMagicBox.iff
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public class CadieMagicBox : ICloneable
    {
        public uint seq;
        public uint active;
        public uint setor;//category
        public uint character;
        public uint level;
        public uint ulUnknown;//uiOutput
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class ItemReceive
        {
            public uint ID;
            public uint Qty;//Total of recevied
        }
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class ItemTrade
        {
            [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public uint[] ID = new uint[4];
            [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public uint[] Qty = new uint[4];
        }
        public ItemReceive item_receive = new ItemReceive();
        public ItemTrade item_trade = new ItemTrade();
        public uint Box_Random_ID;
        [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 40)]
        byte[] NameInBytes { get; set; }
        [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 32)]
        public class _Date
        {
            [field: MarshalAs(UnmanagedType.Struct, SizeConst = 16)]
            public SYSTEMTIME Start { get; set; }
            [field: MarshalAs(UnmanagedType.Struct, SizeConst = 16)]
            public SYSTEMTIME End { get; set; }
            public _Date()
            {
                End = new SYSTEMTIME();
                Start = new SYSTEMTIME();
            }
            public bool Check()
            {
                return (DateTime.Compare(Start.Time, DateTime.Now) < 0) & (DateTime.Compare(End.Time, DateTime.Now) > 0);
            }
        }
        [field: MarshalAs(UnmanagedType.Struct)]
        public _Date date { get; set; } = new _Date();
        public string Name { get => Encoding.GetEncoding("Shift_JIS").GetString(NameInBytes).Replace("\0", ""); set => NameInBytes = Encoding.GetEncoding("Shift_JIS").GetBytes(value.PadRight(40, '\0')); }

        public object Clone()
        {
            return MemberwiseClone();
        }

    }
    #endregion
}
