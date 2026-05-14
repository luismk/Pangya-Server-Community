using System.Runtime.InteropServices;
using System.Text;

namespace PangyaAPI.IFF.JP.Models.Data
{
    // GrandPrixConditionEquip IFF
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class GrandPrixConditionEquip
    {
        public uint active;
        public uint _typeid;
        public uint item_typeid;

        [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 516)]//is 64, 2 short unknown
        public byte[] infoInBytes { get; set; }
        public string info { get => Encoding.GetEncoding("Shift_JIS").GetString(bytes: infoInBytes).Replace("\0", ""); set => infoInBytes = Encoding.GetEncoding("Shift_JIS").GetBytes(value.PadRight(516, '\0')); }

    }
}