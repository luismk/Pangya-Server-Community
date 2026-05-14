using System.Runtime.InteropServices;
namespace PangyaAPI.IFF.JP.Models.General
{
    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 1)]
    public class IFFLevel
    {
        [field: MarshalAs(UnmanagedType.U1, SizeConst = 1)]
        public sbyte level { get; set; }//ler somente esse ;D

        public bool GoodLevel(byte my_level)
        {
            if (is_max && my_level <= level)
                return true;
            else if (!(is_max) && my_level >= level)
                return true;

            return false;
        }
        /// <summary>
        /// set value in level max, true = 70, false = other value
        /// </summary>
        public bool is_max { get => level >= 70; }
    }
}
