using System.Runtime.InteropServices;

namespace PangyaAPI.IFF.JP.Models.Data
{
    #region Struct MemorialShopCoinItem.sff
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public class MemorialShopCoinItem
    {
        public uint Active { get; set; }
        public uint ID { get; set; }
        public uint type { get; set; }//0 normal
        public uint Probabilities { get; set; }
        [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 8)]
        public class GachaRange
        {
            public uint Number_Min { get; set; }
            public uint Number_Max { get; set; }
            public bool empty()
            {
                return Number_Min == 0 && Number_Max == 0;
            }
            public bool isBetweenGacha(uint _number)
            {
                return Number_Min <= _number && _number <= Number_Max;
            }
        }
        [field: MarshalAs(UnmanagedType.Struct, SizeConst = 8)]
        public GachaRange gacha_range { get; set; }

        [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
        public int[] filter { get; set; }

        public bool hasFilter(int _filter)
        {
            if (_filter == 0)
                return false;

            for (int i = 0; i < 10; ++i)
                if (filter[i] == _filter)
                    return true;

            return false;
        }

        public bool emptyFilter()
        {
            int count = 0;

            for (var i = 0; i <  10; ++i)
                count += filter[i];

            return count == 0;
        }
    }
    #endregion

}
