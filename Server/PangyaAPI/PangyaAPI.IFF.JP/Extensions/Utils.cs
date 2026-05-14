using System.Text;
namespace PangyaAPI.IFF.JP.Extensions
{
    public class Utils
    {
        public Utils() { }
        public static bool IsShiftJIS(string input)
        {
            // Convertendo a string para bytes usando a codificação Shift-JIS
            Encoding sjisEnc = Encoding.GetEncoding("shift_jis");
            byte[] sjisBytes = sjisEnc.GetBytes(input);

            // Verifica se há bytes que correspondem a caracteres Shift-JIS japoneses
            for (int i = 0; i < sjisBytes.Length - 1; i++)
            {
                byte firstByte = sjisBytes[i];
                byte secondByte = sjisBytes[i + 1];

                // Verifica se os bytes correspondem a um caractere Shift-JIS japonês
                if ((firstByte >= 0x81 && firstByte <= 0x9F) ||
                    (firstByte >= 0xE0 && firstByte <= 0xEF && secondByte >= 0x40 && secondByte <= 0xFC))
                {
                    return true;
                }
            }

            return false;
        }

        public static uint GetItemGroup(uint _typeid)
        {
            return (uint)((_typeid & 0xFC000000) >> 26);
        }
    }
}
