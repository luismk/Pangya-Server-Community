using System.Runtime.InteropServices;

namespace PangyaAPI.IFF.JP.Models.Data
{
    #region Struct GrandPrixRankReward.iff
    [StructLayout(LayoutKind.Sequential, Pack = 4)] 
    public class GrandPrixRankReward
    {
        public uint Active { get; set; }
        public uint ID { get; set; }
        public uint Rank { get; set; }
        [field: MarshalAs(UnmanagedType.Struct)]
        public Reward reward { get; set; }
        public uint Trophy { get; set; }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class Reward
        {
            [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
            public uint[] _typeid { get; set; }
            [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
            public uint[] qntd { get; set; }
            [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
            public uint[] time { get; set; }
            public int GetQuantity()
            {
                int count = 0;

                for (int i = 0; i < qntd.Length; i++)
                {
                    if (qntd[i] > 0)
                    {
                        count++;
                    }
                }
                return count;
            }
            public bool SetQuantity(int idx, uint qtd)
            {
                qntd[idx] = qtd;

                return qntd[idx] > 0;
            }
        }
    }
    #endregion
}
