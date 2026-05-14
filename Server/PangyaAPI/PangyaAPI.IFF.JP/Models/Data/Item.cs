using System.Runtime.InteropServices;
using PangyaAPI.IFF.JP.Models.General;
using PangyaAPI.Utilities.Models;
namespace PangyaAPI.IFF.JP.Models.Data
{
    #region Struct Item.iff
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public class Item : IFFCommon
    {
        public uint ItemType { get; set; }
        [field: MarshalAs(UnmanagedType.ByValTStr, SizeConst = 40)]
        public string Model { get; set; }
        [field: MarshalAs(UnmanagedType.Struct, SizeConst = 10)]
        public IFFStats Stats { get; set; }
        public ushort Point { get; set; }
        public ushort Price1Day { get => Stats.Power; set => Stats.Power = value; }
        public ushort Price7Day { get => Stats.Control; set => Stats.Control = value; }
        public ushort Price15Day { get => Stats.Impact; set => Stats.Impact = value; }
        public ushort Price30Day { get => Stats.Spin; set => Stats.Spin = value; }
        public ushort Price365Day { get => Stats.Curve; set => Stats.Curve = value; }
        public Item(ref PangyaBinaryReader reader, uint strlen)
        {
            Load(ref reader, strlen);
            ItemType = reader.ReadUInt32();
            Model = reader.ReadPStr(40);
            Stats = reader.Read<IFFStats>();
            Point = reader.ReadUInt16();
        }
        public Item()
        {
        }
    }
    #endregion
}