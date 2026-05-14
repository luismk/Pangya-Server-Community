using System;
using System.Runtime.InteropServices;
using System.Text;

namespace PangyaAPI.IFF.JP.Models.Data
{
    /// <summary>
    /// Is Struct file Match.iff
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public class Match : ICloneable
    {
        public uint Active { get; set; }
        public uint ID { get; set; }
        [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 80)]//is 64, 2 short unknown
        public byte[] NameInBytes { get; set; }
        public string Name { get => Encoding.GetEncoding("Shift_JIS").GetString(NameInBytes).Replace("\0", ""); set => NameInBytes = Encoding.GetEncoding("Shift_JIS").GetBytes(value.PadRight(80, '\0')); }

        public byte Level { get; set; }  //unsigned char ucUnknown;	// Não sei o que é, mas em todos é 10(0x0A)
        [field: MarshalAs(UnmanagedType.ByValTStr, SizeConst = 40)]
        public string Texture1 { get; set; }
        [field: MarshalAs(UnmanagedType.ByValTStr, SizeConst = 40)]
        public string Texture2 { get; set; }
        [field: MarshalAs(UnmanagedType.ByValTStr, SizeConst = 40)]
        public string Texture3 { get; set; }

        [field: MarshalAs(UnmanagedType.ByValTStr, SizeConst = 40)]
        public string Texture4 { get; set; }
        [field: MarshalAs(UnmanagedType.ByValTStr, SizeConst = 40)]
        public string Texture5 { get; set; }
        [field: MarshalAs(UnmanagedType.ByValTStr, SizeConst = 40)]
        public string Texture6 { get; set; }
        [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public byte[] Blank { get; set; }

        public object Clone()
        {
            return MemberwiseClone();
        }
        public void GenerateID(uint iffType, uint num, uint serial)
        {
            ID = GenerateNewTypeID(iffType, 0, num, 2, 0, serial);
        }
        uint GenerateNewTypeID(uint iffType, uint characterId, uint pos, uint group, uint type, uint serial)
        {
            if (group - 1 < 0)
            {
                group = 0;
            }
            return (uint)Convert.ToUInt64((iffType * Math.Pow(2.0, 26.0)) + (characterId * Math.Pow(2.0, 18.0)) + (pos * Math.Pow(2.0, 13.0)) + (group * Math.Pow(2.0, 11.0)) + (type * Math.Pow(2.0, 9.0)) + serial);
        }
        public Match()
        {
            Texture1 = "";
            Texture2 = "";
            Texture3 = "";
            Texture4 = "";
            Texture5 = "";
            Texture6 = "";
            NameInBytes = new byte[80];
            Blank = new byte[3];
        }
    }
}
