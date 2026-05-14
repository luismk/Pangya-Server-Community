using System.Runtime.InteropServices;
using PangyaAPI.IFF.JP.Models.General;
using PangyaAPI.Utilities.Models;

namespace PangyaAPI.IFF.JP.Models.Data
{
    #region Struct CaddieItem.iff
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public class CaddieItem : IFFCommon
    {
        [field: MarshalAs(UnmanagedType.ByValTStr, SizeConst = 40)]
        public string MPet { get; set; }
        [field: MarshalAs(UnmanagedType.ByValTStr, SizeConst = 40)]
        public string TexTure { get; set; }
        [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public ushort[] price { get; set; }

        public uint unit_power_guage_start { get; set; }
        public CaddieItem(PangyaBinaryReader reader)
        {
            Load(ref reader, 40);
            MPet = reader.ReadPStr(40);
            TexTure = reader.ReadPStr(40);
            unit_power_guage_start = reader.ReadUInt32();
        }
        enum CaddieType : byte
        {
            COOKIE,     // CASH
            PANG,       // PANG
            ESPECIAL,   // ACHO, por que não tem nenhum item com esse, não vi pelo menos
            UPGRADE
        }
        public CaddieItem()
        { }
    }
    #endregion

}
