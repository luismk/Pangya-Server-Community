using System.Runtime.InteropServices;
using PangyaAPI.Utilities;
namespace PangyaAPI.IFF.JP.Models.General
{
    [StructLayout(LayoutKind.Sequential, Pack = 4, Size = 36)]
    public class IFFDate
    {
        public IFFDate()
        {
            Start = new SYSTEMTIME();
            End = new SYSTEMTIME();
        }
        //-------------------- TIME IFF--------------\\
        [field: MarshalAs(UnmanagedType.Bool, SizeConst = 4)]
        public bool active { get; set; }//156 start position
        [field: MarshalAs(UnmanagedType.Struct, SizeConst = 16)]
        public SYSTEMTIME Start { get; set; }// 160 start position
        [field: MarshalAs(UnmanagedType.Struct, SizeConst = 16)]
        public SYSTEMTIME End { get; set; }// 176 start position
        //--------------------------------------------------\\
        public bool Check()
        {
            if (active)
            {
                return true;
            }
            return false;
        }
        public void Clear()
        {
            Start = new SYSTEMTIME();
            End = new SYSTEMTIME();
        }
    }
}
