using System.Runtime.InteropServices;
using PangyaAPI.IFF.JP.Models.General;
namespace PangyaAPI.IFF.JP.Models.Data
{


    #region Struct QuestItem.iff
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public class QuestItem : IFFCommon
    {
        public uint ulUnknown;
        public uint type;
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class Quest
        {
            public uint qntd;
            [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
            public uint[] _typeid = new uint[10];
        }
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class Reward
        {
            [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            public uint[] _typeid = new uint[2];
            [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            public uint[] qntd = new uint[2];
            [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            public uint[] time = new uint[2]; // !@[ACHO] que é isso aqui também
        }
        [field: MarshalAs(UnmanagedType.Struct)]
        public Quest quest = new Quest();
        [field: MarshalAs(UnmanagedType.Struct)]
        public Reward reward = new Reward();
        public uint ulUnknown2;

    }
    #endregion    
}
