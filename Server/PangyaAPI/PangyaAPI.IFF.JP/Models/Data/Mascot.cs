using System.Runtime.InteropServices;
using PangyaAPI.IFF.JP.Models.General;
using PangyaAPI.Utilities.Models;
namespace PangyaAPI.IFF.JP.Models.Data
{
    #region class Mascot.iff
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public class Mascot : IFFCommon
    {
        public Mascot() { }
        public Mascot(ref PangyaBinaryReader reader, uint strLen)
        {
            LoadFile(ref reader, strLen);
        }

        public void LoadFile(ref PangyaBinaryReader reader, uint strLen)
        {
        }


        [field: MarshalAs(UnmanagedType.ByValTStr, SizeConst = 40)]
        public string MPet { get; set; }
        [field: MarshalAs(UnmanagedType.ByValTStr, SizeConst = 40)]
        public string Texture1 { get; set; }
        [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
        public byte[] price { get; set; }
        public byte Power { get; set; }
        public byte Control { get; set; }
        public byte Impact { get; set; }
        public byte Spin { get; set; }
        public byte Curve { get; set; }
        [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 11)]
        public class Efeito
        {
            public short power_drive { get; set; }
            public short drop_rate { get; set; }
            public short power_gague { get; set; }
            public short pang_rate { get; set; }
            public short exp_rate { get; set; }
            public byte item_slot { get; set; }
        }
        [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 7)]
        public class Mensagem
        {
            [field: MarshalAs(UnmanagedType.U1)]
            public bool active { get; set; }
            public short flag { get; set; }
            public uint change_price { get; set; }
        }
        [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 4)]
        public class BonusPangya
        {
            public ushort pang { get; set; }
            public ushort flag { get; set; }
        }
        [field: MarshalAs(UnmanagedType.Struct)]
        public Efeito efeito { get; set; }
        [field: MarshalAs(UnmanagedType.Struct)]
        public Mensagem msg { get; set; }
        [field: MarshalAs(UnmanagedType.Struct)]
        public BonusPangya bonus_pangya { get; set; }
    }
    #endregion

}
