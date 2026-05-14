using System.Runtime.InteropServices;
using PangyaAPI.IFF.JP.Models.General;

namespace PangyaAPI.IFF.JP.Models.Data
{

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class CounterItem : IFFCommon
    {
        [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 88)]
        public byte[] Bytes { get; set; }//8 start position
    }
}