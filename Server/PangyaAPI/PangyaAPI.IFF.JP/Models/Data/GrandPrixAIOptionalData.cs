using System.Runtime.InteropServices;
using System.Text;

namespace PangyaAPI.IFF.JP.Models.Data
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class GrandPrixAIOptionalData
    {
        public uint Active { get; set; }
        public int ID { get; set; }
        [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 36)]//is 64, 2 short unknown
        public byte[] NameInBytes { get; set; }
        public string Name { get => Encoding.GetEncoding("Shift_JIS").GetString(NameInBytes).Replace("\0", ""); set => NameInBytes = Encoding.GetEncoding("Shift_JIS").GetBytes(value.PadRight(36, '\0')); }
        public uint BetterOrNo { get; set; }
        public uint TypeID { get; set; }
        public uint Class { get; set; }
        [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 24)]
        public uint[] parts_typeid { get; set; } // Array de uints
        [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 24)]
        public uint[] parts_id { get; set; } // Array de uints
        /// <summary>
        ///Não sei bem direito o que é aqui
        /// </summary>
        [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 24 * 9)]
        public byte[] UccIndexList { get; set; }
        /// <summary>
        ///Auxiliar Parts 5, aqui fica anel
        /// </summary>
        [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
        public uint[] auxparts { get; set; }
        [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 120)]
        public byte[] ucUnknown { get; set; } // Array de bytes      
    }
}