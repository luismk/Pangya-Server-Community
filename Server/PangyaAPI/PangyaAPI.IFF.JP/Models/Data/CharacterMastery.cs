using System.Runtime.InteropServices;

namespace PangyaAPI.IFF.JP.Models.Data
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class CharacterMastery
    {
        public uint active;
        public uint _typeid;
        public uint seq;
        public uint stats;
        public uint level;
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class Condition
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
            public uint[] condition = new uint[5];
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
            public uint[] qntd = new uint[5];
        }
        [MarshalAs(UnmanagedType.Struct)]
        public Condition condition = new Condition();
    }
}