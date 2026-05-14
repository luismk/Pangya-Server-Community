using System.Linq;
using System.Runtime.InteropServices;
using PangyaAPI.Utilities.Models;

namespace PangyaAPI.IFF.JP.Models.Data
{
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public class ClubSetWorkShopRankUpExp
    {
        public uint tipo;
        [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        public uint[] rank; // F ~ A
        public ClubSetWorkShopRankUpExp()
        { }
        public ClubSetWorkShopRankUpExp(PangyaBinaryReader reader)
        {
            tipo = reader.ReadUInt32();
            rank = reader.ReadUInt32Array(5).ToArray();
        }
    }
}