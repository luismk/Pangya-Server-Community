using System.Runtime.InteropServices;
using PangyaAPI.IFF.JP.Models.General;

namespace PangyaAPI.IFF.JP.Models.Data
{
    #region Struct SetItem.iff
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class Packege
    {
        public uint Total { get; set; }
        [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
        public uint[] item_typeid { get; set; }
        [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
        public ushort[] item_qntd { get; set; }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public class SetItem : IFFCommon
    {

        [field: MarshalAs(UnmanagedType.Struct)]
        public Packege packege { get; set; }
        [field: MarshalAs(UnmanagedType.Struct)]
        public IFFStats Stats { get; set; } // aqui deve ser algum tempo
        public ushort Point { get; set; }
        public uint TypeSet => (uint)((ID & ~0xFC000000) >> 21);
    }
    #endregion     
}
