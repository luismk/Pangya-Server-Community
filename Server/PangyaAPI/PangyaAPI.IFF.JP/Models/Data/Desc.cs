using System;
using System.Runtime.InteropServices;
using System.Text;
using PangyaAPI.Utilities.Models;

namespace PangyaAPI.IFF.JP.Models.Data
{

    #region Struct Desc.iff
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public class Desc : ICloneable
    {
        public uint ID { get; set; }
        [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 512)]
        byte[] DescriptionInBytes { get; set; }//4 start position
        public string Description
        {
            get
            {
                // Converta o array de bytes para uma string usando a codificação Shift_JIS
                string result = Encoding.GetEncoding("Shift_JIS").GetString(DescriptionInBytes);


                // Se necessário, substitua caracteres de quebra de linha específicos por um padrão desejado
                // Por exemplo, no Shift_JIS pode ser necessário garantir que as quebras de linha estejam no formato correto
                // Dependendo do seu contexto, você pode ajustar isso
                result = result.Replace("\r\n", "\n").Replace("\n", Environment.NewLine);

                return result;
            }
            set
            {
                // Adicionar quebras de linha para o exemplo
                string valueWithNewLines = value.Replace("\r\n", "\n").Replace("\n", "\r\n"); // Convert new lines to CRLF

                // Codificar a string com Shift_JIS e garantir que o comprimento seja 512 bytes
                DescriptionInBytes = Encoding.GetEncoding("Shift_JIS").GetBytes(valueWithNewLines.PadRight(512, '\0'));
            }
        }
        public Desc(PangyaBinaryReader reader)
        {
            ID = reader.ReadUInt32();
            DescriptionInBytes = reader.ReadBytes(512);
        }
        public Desc()
        {
        }

        public object Clone()
        {
            return MemberwiseClone();
        }

        public string getDesc()
        {
            var result = Description.Replace("'", "''");
            return result.Replace("\0", "");
        }
    }
    #endregion

}
