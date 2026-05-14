using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace PangyaAPI.Utilities
{
    public static class Tools
    {

        public static IntPtr INVALID_HANDLE_VALUE = IntPtr.Zero;
        public const UInt32 INFINITE = 0xFFFFFFFF;
        public const UInt32 WAIT_ABANDONED = 0x00000080;
        public const UInt32 WAIT_OBJECT_0 = 0x00000000;
        public const UInt32 WAIT_TIMEOUT = 0x00000102;
        public const uint CREATE_SUSPENDED = 0x00000004;

        [DllImport("kernel32.dll")]
        public static extern uint WaitForMultipleObjects(uint nCount, IntPtr[] lpHandles, bool bWaitAll, uint dwMilliseconds);
        // Source - https://stackoverflow.com/q
        // Posted by Pavel Durov, modified by community. See post 'Timeline' for change history
        // Retrieved 2025-12-29, License - CC BY-SA 3.0

        [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Auto)]
        public static extern IntPtr CreateEvent(IntPtr lpEventAttributes, [In, MarshalAs(UnmanagedType.Bool)] bool bManualReset, [In, MarshalAs(UnmanagedType.Bool)] bool bIntialState, [In, MarshalAs(UnmanagedType.BStr)] string lpName);
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern UInt32 WaitForSingleObject(IntPtr hHandle, UInt32 dwMilliseconds);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool SetEvent(IntPtr hHandle);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool ResetEvent(IntPtr hHandle);
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool CloseHandle(IntPtr hHandle);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern uint GetLastError();

        public delegate void ThreadRoutine(object lpThreadParameter);
        public static float Clamp(float value, float min, float max)
        {
            if (value < min)
                return min;

            if (value > max)
                return max;

            return value;
        }

        public static T IfCompare<T>(bool expression, T trueValue, T falseValue)
        {
            if (expression)
            {
                return trueValue;
            }
            else
            {
                return falseValue;
            }
        }

        public static bool Sanitize(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return true;

            if (input.Length > 256)
                return false;

            string[] blacklist =
            {
        "--",
        ";--",
        "/*",
        "*/",
        "@@",
        "char",
        "nchar",
        "varchar",
        "nvarchar",
        "alter",
        "begin",
        "cast",
        "create",
        "cursor",
        "declare",
        "delete",
        "drop",
        "exec",
        "execute",
        "fetch",
        "insert",
        "kill",
        "open",
        "select",
        "sys",
        "sysobjects",
        "syscolumns",
        "table",
        "update"
    };

            var lower = input.ToLower();

            foreach (var item in blacklist)
            {
                if (input.IndexOf(item, StringComparison.OrdinalIgnoreCase) >= 0)
                    return false;
            }

            return true;
        }

        public static KeyValuePair<TKey, TValue> insert<TKey, TValue>(this Dictionary<TKey, TValue> pairs, TKey key, TValue value)
        {
            if (pairs.ContainsKey(key))
                pairs[key] = value; // Atualiza o valor se a chave já existir
            else
                pairs.Add(key, value); // Adiciona a chave se não existir

            return new KeyValuePair<TKey, TValue>(key, value);
        }

        public static bool TryAdd<TKey, TValue>(this Dictionary<TKey, TValue> pairs, TKey key, TValue value)
        {
            if (pairs.ContainsKey(key))
                pairs[key] = value; // Atualiza o valor se a chave já existir
            else
                pairs.Add(key, value); // Adiciona a chave se não existir

            return pairs.ContainsKey(key);
        }

        public static KeyValuePair<TKey, TValue> insert<TKey, TValue>(this Dictionary<TKey, TValue> pairs, Tuple<TKey, TValue> tuple)
        {
            if (pairs.ContainsKey(tuple.Item1))
                pairs[tuple.Item1] = tuple.Item2; // Atualiza o valor se a chave já existir
            else
                pairs.Add(tuple.Item1, tuple.Item2); // Adiciona a chave se não existir

            return new KeyValuePair<TKey, TValue>(tuple.Item1, tuple.Item2);
        }

        public static bool empty<TKey, TValue>(this Dictionary<TKey, TValue> pairs)
        {
            return pairs == null || !pairs.Any(); // Retorna true se o dicionário estiver vazio
        }
        public static bool empty<TKey, TValue>(this Dictionary<TKey, List<TValue>> pairs)
        {
            return pairs == null || !pairs.Any(); // Retorna true se o dicionário estiver vazio
        }

        public static KeyValuePair<TKey, TValue> end<TKey, TValue>(this Dictionary<TKey, TValue> pairs)
        {
            try
            {
                if (pairs.Count > 0)
                    return pairs.LastOrDefault(); // Retorna true se o dicionário estiver vazio
                else
                    return new KeyValuePair<TKey, TValue>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(Environment.StackTrace);
                throw ex;
            }
        }

        public static KeyValuePair<TKey, TValue> find<TKey, TValue>(this Dictionary<TKey, TValue> pairs, object value)
        {
            if (value is TKey keyValue)
            {
                return pairs.FirstOrDefault(c => EqualityComparer<TKey>.Default.Equals(c.Key, keyValue));
            }
            if (value is TValue val)
            {
                return pairs.FirstOrDefault(c => EqualityComparer<TValue>.Default.Equals(c.Value, val));
            }

            return default; // Retorna o valor padrão (default) se não encontrar
        }

        public static KeyValuePair<TKey, TValue> begin<TKey, TValue>(this Dictionary<TKey, TValue> pairs)
        {
            return pairs.FirstOrDefault(); // Retorna true se o dicionário estiver vazio
        }


        public static T end<T>(this List<T> pairs)
        {
            return pairs.Count == 0 ? default : pairs.Last(); // Retorna true se o dicionário estiver vazio
        }

        public static T find<T>(this List<T> pairs, T value)
        {
            return pairs.FirstOrDefault(c => EqualityComparer<T>.Default.Equals(c, value));
        }


        public static T begin<T>(this List<T> pairs)
        {
            return pairs.FirstOrDefault(); // Retorna true se o dicionário estiver vazio
        }


        public static bool empty<T>(this List<T> pairs)
        {
            return pairs == null ? true : !pairs.Any(); // Retorna true se o dicionário estiver vazio
        }

        public static void ClearArray(this Array array)
        {
            if (array != null)
                Array.Clear(array, 0, array.Length);
        }

        public static T reinterpret_cast<T>(object obj) where T : class
        {
            return obj as T;
        }

        public static byte[] StructArrayToByteArray<T>(T[] array) where T : struct
        {
            int size = Marshal.SizeOf<T>() * array.Length;
            byte[] buffer = new byte[size];
            IntPtr ptr = Marshal.AllocHGlobal(size);

            try
            {
                for (int i = 0; i < array.Length; i++)
                {
                    Marshal.StructureToPtr(array[i], ptr + i * Marshal.SizeOf<T>(), false);
                }
                Marshal.Copy(ptr, buffer, 0, size);
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }

            return buffer;
        }

        public static T memcpy<T>(byte[] buffer, int offset = 0) where T : class
        {
            int size = Marshal.SizeOf<T>();

            if (buffer == null || buffer.Length < offset + size)
                throw new ArgumentException("Buffer inválido ou muito pequeno para o tipo especificado.");

            byte[] raw = new byte[size];
            Buffer.BlockCopy(buffer, offset, raw, 0, size);

            GCHandle handle = GCHandle.Alloc(raw, GCHandleType.Pinned);
            try
            {
                return Marshal.PtrToStructure<T>(handle.AddrOfPinnedObject());
            }
            finally
            {
                handle.Free();
            }
        }

        public static void Shuffle<T>(List<T> list)
        {
            Random rng = new Random((int)DateTime.Now.Ticks);
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        public static byte[] Slice(this byte[] source, uint startIndex)
        {
            byte[] result = new byte[source.Length - startIndex];
            Buffer.BlockCopy(source, (int)startIndex, result, 0, (int)(source.Length - startIndex));
            return result;
        }

        public static string GetString(this byte[] array)
        {
            return Encoding.GetEncoding("Shift_JIS").GetString(array).TrimEnd('\0');
        }

        public static void SetString(this byte[] array, string value)
        {
            ClearArray(array);
            var bytes = Encoding.GetEncoding("Shift_JIS").GetBytes(value ?? string.Empty);
            Array.Copy(bytes, array, Math.Min(bytes.Length, array.Length));
        }

        public static T ToObject<T>(this DataRow dataRow)
     where T : new()
        {
            T item = new T();
            foreach (DataColumn column in dataRow.Table.Columns)
            {
                if (dataRow[column] != DBNull.Value)
                {
                    PropertyInfo prop = item.GetType().GetProperty(column.ColumnName);
                    if (prop != null)
                    {
                        object result = Convert.ChangeType(dataRow[column], prop.PropertyType);
                        prop.SetValue(item, result, null);
                        continue;
                    }
                    else
                    {
                        FieldInfo fld = item.GetType().GetField(column.ColumnName);
                        if (fld != null)
                        {
                            object result = Convert.ChangeType(dataRow[column], fld.FieldType);
                            fld.SetValue(item, result);
                        }
                    }
                }
            }
            return item;
        }


        public static bool empty(this string a)
        {
            return string.IsNullOrEmpty(a);
        }

        public static bool IsTrue<T>(this T a)
        {
            return Convert.ToBoolean(a);
        }

        public static T[] InitializeWithDefaultInstances<T>(uint length) where T : class, new()
        {
            T[] array = new T[length];
            for (int i = 0; i < length; i++)
            {
                array[i] = new T();
            }
            return array;
        }

        public static string MD5Hash(this string text)
        {
            if (text.Length >= 32)
            {
                new Exception("input text is MD5 Hash");
            }
            MD5 md5 = new MD5CryptoServiceProvider();

            //compute hash from the bytes of text  
            md5.ComputeHash(Encoding.ASCII.GetBytes(text));

            //get hash result after compute it  
            byte[] result = md5.Hash;

            StringBuilder strBuilder = new StringBuilder();
            for (int i = 0; i < result.Length; i++)
            {
                //change it into 2 hexadecimal digits  
                //for each byte  
                strBuilder.Append(result[i].ToString("x2"));
            }
            var rstr = strBuilder.ToString();
            return rstr.ToUpper();
        }

        /// <summary>
        /// Divide uma lista em várias listas
        /// </summary>
        public static List<List<T>> Split<T>(this List<T> source, int tamanhoPorLista)
        {
            return source
                .Select((x, i) => new { Index = i, Value = x })
                .GroupBy(x => x.Index / tamanhoPorLista)
                .Select(x => x.Select(v => v.Value).ToList())
                .ToList();
        }

        public static string HexDumpOnlyHex(this byte[] bytes, int bytesPerLine = 16)
        {
            if (bytes == null) return "<null>";

            var hexChars = "0123456789ABCDEF".ToCharArray();
            var result = new StringBuilder();

            for (int i = 0; i < bytes.Length; i += bytesPerLine)
            {
                var line = new StringBuilder();

                for (int j = 0; j < bytesPerLine; j++)
                {
                    int index = i + j;
                    if (index < bytes.Length)
                    {
                        byte b = bytes[index];
                        line.Append(hexChars[(b >> 4) & 0xF]);
                        line.Append(hexChars[b & 0xF]);
                        line.Append(' ');
                    }
                    else
                    {
                        line.Append("   "); // espaço para alinhar linha incompleta
                    }

                    if ((j + 1) % 8 == 0)
                        line.Append(" ");
                }

                result.AppendLine(line.ToString().TrimEnd());
            }

            return result.ToString();
        }

        public static string HexDumpNoAddress(this byte[] bytes, int bytesPerLine = 16)
        {
            if (bytes == null) return "<null>";

            var hexChars = "0123456789ABCDEF".ToCharArray();
            var result = new StringBuilder();

            for (int i = 0; i < bytes.Length; i += bytesPerLine)
            {
                var hex = new StringBuilder();
                var ascii = new StringBuilder();

                for (int j = 0; j < bytesPerLine; j++)
                {
                    int index = i + j;
                    if (index < bytes.Length)
                    {
                        byte b = bytes[index];
                        hex.Append(hexChars[(b >> 4) & 0xF]);
                        hex.Append(hexChars[b & 0xF]);
                        hex.Append(' ');

                        ascii.Append(b < 32 || b > 126 ? '·' : (char)b);
                    }
                    else
                    {
                        hex.Append("   "); // espaço para byte ausente
                        ascii.Append(' ');
                    }

                    if ((j + 1) % 8 == 0)
                        hex.Append(" ");
                }

                result.Append(hex);
                result.Append(" ");
                result.AppendLine(ascii.ToString());
            }

            return result.ToString();
        }

        // this method from https://www.codeproject.com/Articles/36747/Quick-and-Dirty-HexDump-of-a-Byte-Array
        public static string HexDump(this byte[] bytes, int bytesPerLine = 16)
        {
            if (bytes == null) return "<null>";
            var bytesLength = bytes.Length;

            var HexChars = "0123456789ABCDEF".ToCharArray();

            var firstHexColumn =
                8 // 8 characters for the address
                + 3; // 3 spaces

            var firstCharColumn = firstHexColumn
                                  + bytesPerLine * 3 // - 2 digit for the hexadecimal value and 1 space
                                  + (bytesPerLine - 1) / 8 // - 1 extra space every 8 characters from the 9th
                                  + 2; // 2 spaces 

            var lineLength = firstCharColumn
                             + bytesPerLine // - characters to show the ascii value
                             + Environment.NewLine.Length; // Carriage return and line feed (should normally be 2)

            var line = (new string(' ', lineLength - Environment.NewLine.Length) + Environment.NewLine).ToCharArray();
            var expectedLines = (bytesLength + bytesPerLine - 1) / bytesPerLine;
            var result = new StringBuilder(expectedLines * lineLength);

            for (var i = 0; i < bytesLength; i += bytesPerLine)
            {
                line[0] = HexChars[(i >> 28) & 0xF];
                line[1] = HexChars[(i >> 24) & 0xF];
                line[2] = HexChars[(i >> 20) & 0xF];
                line[3] = HexChars[(i >> 16) & 0xF];
                line[4] = HexChars[(i >> 12) & 0xF];
                line[5] = HexChars[(i >> 8) & 0xF];
                line[6] = HexChars[(i >> 4) & 0xF];
                line[7] = HexChars[(i >> 0) & 0xF];

                var hexColumn = firstHexColumn;
                var charColumn = firstCharColumn;

                for (var j = 0; j < bytesPerLine; j++)
                {
                    if (j > 0 && (j & 7) == 0) hexColumn++;
                    if (i + j >= bytesLength)
                    {
                        line[hexColumn] = ' ';
                        line[hexColumn + 1] = ' ';
                        line[charColumn] = ' ';
                    }
                    else
                    {
                        var b = bytes[i + j];
                        line[hexColumn] = HexChars[(b >> 4) & 0xF];
                        line[hexColumn + 1] = HexChars[b & 0xF];
                        line[charColumn] = b < 32 ? '·' : (char)b;
                    }

                    hexColumn += 3;
                    charColumn++;
                }

                result.Append(line);
            }

            return result.ToString();
        }

        public static string Hex(this byte[] bytes, int code = 1)
        {
            switch (code)
            {
                case 1:
                    return HexDumpOnlyHex(bytes, 16);
                case 2:
                    return HexDumpNoAddress(bytes, 16);
                case 3:
                    return HexDump(bytes, 16);
                default:
                    return HexDump(bytes, 16);
            }
        }

        public static string getBetween(string strSource, string strStart, string strEnd)
        {
            int Start, End;
            if (strSource.Contains(strStart) && strSource.Contains(strEnd))
            {
                Start = strSource.IndexOf(strStart, 0) + strStart.Length;
                End = strSource.IndexOf(strEnd, Start);
                return strSource.Substring(Start, End - Start);
            }
            else
            {
                return "";
            }
        }

        public static string ByteToString(byte ba)
        {
            StringBuilder stringBuilder = new StringBuilder(2);
            stringBuilder.AppendFormat("{0:x2}", ba);
            return stringBuilder.ToString();
        }
    }
}
