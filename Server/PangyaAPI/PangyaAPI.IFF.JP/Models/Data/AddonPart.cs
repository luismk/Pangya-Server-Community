using System.Runtime.InteropServices;
using System.Text;
using PangyaAPI.Utilities.Models;
namespace PangyaAPI.IFF.JP.Models.Data
{
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public class AddonPart
    {
        public AddonPart()
        { }

        public AddonPart(PangyaBinaryReader Reader)
        {
            Active = Reader.ReadUInt32();
            ID = Reader.ReadUInt32();
            NameInBytes = Reader.ReadBytes(40);
            Texture = Reader.ReadPStr(40);
            Texture2 = Reader.ReadPStr(40);
            Texture3 = Reader.ReadPStr(40);
            Texture4 = Reader.ReadPStr(40);
            Texture5 = Reader.ReadPStr(40);
            Texture6 = Reader.ReadPStr(40);
        }

        public uint Active { get; set; }
        public uint ID { get; set; }
        [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 40)]
        byte[] NameInBytes { get; set; }//8 start position
        public string Name//correcao para não causar conflito ao escrever
        {
            get => Encoding.GetEncoding("Shift_JIS").GetString(NameInBytes).Replace("\0", "");
            set => NameInBytes = Encoding.GetEncoding("Shift_JIS").GetBytes(value.PadRight(40, '\0'));
        }
        [field: MarshalAs(UnmanagedType.ByValTStr, SizeConst = 40)]
        public string Texture { get; set; }
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
    }
}