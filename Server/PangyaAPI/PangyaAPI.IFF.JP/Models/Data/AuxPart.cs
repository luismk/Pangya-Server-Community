using System.Runtime.InteropServices;
using PangyaAPI.IFF.JP.Models.General;
namespace PangyaAPI.IFF.JP.Models.Data
{
    #region Struct AuxPart.iff
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class AuxPart : IFFCommon
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
        public ushort[] cc = new ushort[5];  // [0] qntd, (Acho) usa para tempo e essas coisas, 1day, 7, 15, 30, 365 e assim vai
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
        public byte[] c = new byte[5];
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
        public byte[] slot = new byte[5];
        public ushort Power_Drive { get; set; }
        public ushort Drop_Rate { get; set; }
        public ushort Power_Gauge { get; set; }
        public ushort Pang_Rate { get; set; }
        public ushort Exp_Rate { get; set; }
        public ushort ItemSlot { get; set; }
        public ushort Bonus_Pang { get; set; }
        public ushort Bonus_Flag { get; set; }

        public bool getQnt()
        {
            return cc[0]/*Qntd*/ > 0;
        }

        // Método para identificar o tipo de um typeid
        public string IdentifyType()
        {
            byte result;

            result = (byte)((ID & ~0xFC000000) >> 21);

            return result.ToString();
        }
        public int IdentifyRing()
        {
            byte result;

            result = (byte)((ID & ~0xFC000000) >> 21);

            return result;
        }

        // Método auxiliar para verificar se o typeId corresponde a um padrão específico
        private bool IsPatternMatch(uint pattern)
        {
            // Extrai os bits relevantes e compara com o padrão
            return (ID & pattern) == pattern;
        }
    }
    #endregion
}
