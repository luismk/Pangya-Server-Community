using System;
using System.Linq;
using System.Text;
using PangyaAPI.Utilities.Models;

namespace PangyaAPI.Utilities
{
    public static class Translation
    {
        private static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

        public static string Decode(byte[] buffer, int index, int count)
        {
            return Utf8NoBom.GetString(buffer, index, count);
        }

        public static string DecodeWithLength(byte[] buffer, ref int offset)
        {
            ushort len = BitConverter.ToUInt16(buffer, offset);
            offset += 2;
            string text = Utf8NoBom.GetString(buffer, offset, len);
            offset += len;
            return text;
        }
        public static byte[] Encode(string text)
        {
            if (text == null) return Array.Empty<byte>();
            return Utf8NoBom.GetBytes(text);
        }

        public static byte[] EncodeWithLength(string text)
        {
            var data = Encode(text);
            var len = BitConverter.GetBytes((ushort)data.Length); // 2 bytes de tamanho (ushort)
            return len.Concat(data).ToArray();
        }

        public static string ReadUtf8String(this PangyaBinaryReader reader)
        {
            ushort len = reader.ReadUInt16();
            if (len == 0)
                return string.Empty;

            byte[] data = reader.ReadBytes(len);
            return Utf8NoBom.GetString(data);
        }
        public static void WriteUtf8String(this PangyaBinaryWriter writer, string text)
        {
            if (text == null)
                return;

            byte[] data = Utf8NoBom.GetBytes(text);
            writer.Write((ushort)data.Length); // prefixo de tamanho (2 bytes)
            writer.Write(data);
        }
    }
}
