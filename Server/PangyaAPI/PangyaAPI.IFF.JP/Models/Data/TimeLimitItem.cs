using System.Runtime.InteropServices;

namespace PangyaAPI.IFF.JP.Models.Data
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public class TimeLimitItem
    {
        public uint active;
        public uint _typeid;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 40)]
        public string name;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 40)]
        public string icon;

        public uint type;
        public uint percent; // Rate
        public uint time;

        public void clear()
        {
            active = 0;
            _typeid = 0;
            name = string.Empty;
            icon = string.Empty;
            type = 0;
            percent = 0;
            time = 0;
        }
    }

}