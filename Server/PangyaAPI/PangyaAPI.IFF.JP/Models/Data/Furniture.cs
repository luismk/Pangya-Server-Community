using System.Runtime.InteropServices;
using PangyaAPI.IFF.JP.Models.General;
namespace PangyaAPI.IFF.JP.Models.Data
{
    /// <summary>
    /// Is Struct file Furniture.iff
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class Furniture : IFFCommon
    {
        [field: MarshalAs(UnmanagedType.ByValTStr, SizeConst = 40)]
        public string mpet { get; set; }
        public ushort num;             // By TH S4 - (Num)
        public ushort is_own;          // By TH S4 - (IsOwn)
        public ushort is_move;         // By TH S4 - (IsMove) 5 Poster
        public ushort is_function;     // By TH S4 - (IsFunction)  0x62 Poster B, 0x63 Poster A
        public int etc;               // By TH S4 - (Etc)
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class Location
        {
            public float x;
            public float y;
            public float z;
            public float r;
        };
        [field: MarshalAs(UnmanagedType.Struct)]
        public Location location;
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct Textura
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 40)]
            public string Value;
        }
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct Textura_Org
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 40)]
            public string Value;
        }
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public Textura[] textura;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public Textura_Org[] textura_org;
        [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
        public ushort[] c;            // By TH S4 - (COM[5])
        public ushort use_time;		// By TH S4 - (UseTime)
    }
}
