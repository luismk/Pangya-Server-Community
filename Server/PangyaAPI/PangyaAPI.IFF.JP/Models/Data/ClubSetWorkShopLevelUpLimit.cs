using System.Linq;
using System.Runtime.InteropServices;
using PangyaAPI.Utilities.Models;

namespace PangyaAPI.IFF.JP.Models.Data
{
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public class ClubSetWorkShopLevelUpLimit
    {
        public uint tipo;
        public uint rank;
        [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
        public ushort[] c;
        public ushort option;
        public ClubSetWorkShopLevelUpLimit()
        { }
        public ClubSetWorkShopLevelUpLimit(PangyaBinaryReader reader)
        {
            tipo = reader.ReadUInt32();
            rank = reader.ReadUInt32();
            c = reader.ReadUInt16Array(5).ToArray();
            option = reader.ReadUInt16();
        }
    }
}