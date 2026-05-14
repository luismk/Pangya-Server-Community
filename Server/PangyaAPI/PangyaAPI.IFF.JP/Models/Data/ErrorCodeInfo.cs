using System;
using System.Runtime.InteropServices;
using System.Text;
using PangyaAPI.Utilities.Models;

namespace PangyaAPI.IFF.JP.Models.Data
{
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public class ErrorCodeInfo : ICloneable
    {
        public uint Active { get; set; }
        public uint Code { get; set; }
        public uint Type { get; set; }
        [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 260)]
        byte[] InfoInBytes { get; set; }//8 start position
        public string Info
        {
            get
            {
                // Converta o array de bytes para uma string usando a codificação Shift_JIS
                string result = Encoding.GetEncoding("Shift_JIS").GetString(InfoInBytes);

                result = result.Replace("\r\n", "\n").Replace("\n", Environment.NewLine);

                return result;
            }
            set
            {
                // Adicionar quebras de linha para o exemplo
                string valueWithNewLines = value.Replace("\r\n", "\n").Replace("\n", "\r\n"); // Convert new lines to CRLF

                // Codificar a string com Shift_JIS e garantir que o comprimento seja 512 bytes
                InfoInBytes = Encoding.GetEncoding("Shift_JIS").GetBytes(valueWithNewLines.PadRight(260, '\0'));
            }
        }


        public ErrorCodeInfo()
        {
        }
        public ErrorCodeInfo(PangyaBinaryReader reader)
        {
            // Leitura do campo Active
            Active = reader.ReadUInt32();

            // Leitura do campo Code
            Code = reader.ReadUInt32();

            // Leitura do campo Type
            Type = reader.ReadUInt32();

            // Leitura do campo InfoInBytes
            InfoInBytes = reader.ReadBytes(260);
        }

        public object Clone()
        {
            return MemberwiseClone();
        }
    }
}