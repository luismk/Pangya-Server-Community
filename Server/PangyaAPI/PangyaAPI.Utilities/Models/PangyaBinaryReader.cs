using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace PangyaAPI.Utilities.Models
{
    public class PangyaBinaryReader : BinaryReader
    {
        public Encoding _Encoder { get; set; } = Encoding.GetEncoding("shift_jis");

        public PangyaBinaryReader(Stream input) : base(input)
        {
        }

        public PangyaBinaryReader(Stream input, Encoding encoding) : base(input, encoding)
        {
        }

        public PangyaBinaryReader(Stream input, Encoding encoding, bool leaveOpen) : base(input, encoding, leaveOpen)
        {
        }

        public void Skip(int count)
        {
            Seek(count, 1);
        }

        public void Seek(long offset, int origin)
        {
            BaseStream.Seek(offset, (SeekOrigin)origin);
        }
        public void Seek(uint offset, int origin)
        {
            BaseStream.Seek(offset, (SeekOrigin)origin);
        }

        public void Seek(int offset, int origin)
        {
            BaseStream.Seek(offset, (SeekOrigin)origin);
        }
        public uint Size => (uint)BaseStream.Length;


        public byte[] GetRemainingData(int Count)
        {
            int previousOffset;
            previousOffset = (int)BaseStream.Position;
            var array = ReadBytes(Count);
            BaseStream.Position = previousOffset;
            return array;
        }
        public byte[] GetRemainingData()
        {
            int previousOffset;
            previousOffset = (int)BaseStream.Position;
            var array = ReadBytes((int)Size);
            BaseStream.Position = previousOffset;
            return array;
        }


        public bool ReadPStr(out string value, uint Count)
        {
            try
            {
                var data = new byte[Count];
                //ler os dados
                BaseStream.Read(data, 0, (int)Count);

                value = _Encoder.GetString(data);
            }
            catch
            {
                value = null;
                return false;
            }
            return true;
        }

        public bool ReadPStr(out string[] value, uint Length, uint Count)
        {
            try
            {
                value = new string[Count / Length];
                for (int i = 0; i < Count / Length; i++)
                {
                    value[i] = ReadPStr(Length);
                }
            }
            catch
            {
                value = null;
                return false;
            }
            return true;
        }
        public bool ReadPStr(out string value)
        {
            try
            {
                var size = ReadUInt16();
                value = _Encoder.GetString(ReadBytes(size));
            }
            catch
            {
                value = null;
                return false;
            }
            return true;
        }

        public string ReadSJISString()
        {
            try
            {
                ushort length = ReadUInt16(); // Lê o tamanho da string (em bytes)
                byte[] data = ReadBytes(length); // Lê os bytes da string

                return _Encoder.GetString(data);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERRO] Falha ao ler string Shift_JIS: {ex.Message}");
                return string.Empty;
            }
        }
        public string ReadPStr()
        {
            try
            {
                var size = ReadUInt16();

                return _Encoder.GetString(ReadBytes(size));
            }
            catch
            {
                return "";
            }
        }

        public byte[] ReadByteStr()
        {
            try
            {
                var size = ReadUInt16();

                return ReadBytes(size);
            }
            catch
            {
                return new byte[0];
            }
        }
        public string ReadPStr(uint Count)
        {
            try
            {
                var data = new byte[Count];
                //ler os dados
                BaseStream.Read(data, 0, (int)Count);

                return _Encoder.GetString(data).Replace("\0", "");
            }
            catch
            {
                return "";
            }
        }

        public short[] ReadShorts(uint Count)
        {
            try
            {
                var data = new short[Count];
                for (int i = 0; i < Count; i++)
                {
                    data[i] = ReadInt16();
                }
                return data;
            }
            catch
            {
                return new short[0];
            }
        }

        public uint GetPosition()
        {
            return (uint)BaseStream.Position;
        }

        public bool ReadDouble(out Double value)
        {
            try
            {
                value = ReadDouble();
            }
            catch
            {
                value = 0;
                return false;
            }
            return true;
        }

        public bool ReadByte(out byte value)
        {
            try
            {
                value = ReadByte();
            }
            catch
            {
                value = 0;
                return false;
            }
            return true;
        }
        public bool ReadInt16(out short value)
        {
            try
            {
                value = ReadInt16();
            }
            catch
            {
                value = 0;
                return false;
            }
            return true;
        }

        public sbyte[] ReadSBytes(int size)
        {
            sbyte[] buffer = new sbyte[size];

            for (int i = 0; i < size; i++)
                buffer[i] = unchecked((sbyte)ReadByte());

            return buffer;
        }

        public bool ReadSBytes(out sbyte[] buffer, int size)
        {
            buffer = new sbyte[size];

            try
            {
                for (int i = 0; i < size; i++)
                    buffer[i] = unchecked((sbyte)ReadByte());
                return true;
            }
            catch
            {
                buffer = Array.Empty<sbyte>();
                return false;
            }
        }




        public bool ReadBytes(out byte[] value, int size)
        {
            try
            {
#pragma warning disable CS0652 // Comparação com constante integral é inútil; a constante está fora do intervalo do tipo "int"
                if (uint.MaxValue < size)
                {
                    value = new byte[0];
                    return false;
                }
#pragma warning restore CS0652 // Comparação com constante integral é inútil; a constante está fora do intervalo do tipo "int"
                value = ReadBytes(size);
            }
            catch
            {
                value = new byte[0];
                return false;
            }
            return true;
        }

        public bool ReadBytes(out byte[] value)
        {
            try
            {
                int size = ReadInt16();

                if (ushort.MaxValue < size)
                {
                    value = new byte[0];
                    return false;
                }
                value = ReadBytes(size);
            }
            catch
            {
                value = new byte[0];
                return false;
            }
            return true;
        }
        public bool ReadUInt16(out ushort value)
        {
            try
            {
                value = ReadUInt16();
            }
            catch
            {
                value = 0;
                return false;
            }
            return true;
        }

        public bool ReadUInt32(out uint value)
        {
            try
            {
                value = ReadUInt32();
            }
            catch
            {
                value = 0;
                return false;
            }
            return true;
        }

        public bool ReadInt32(out int value)
        {
            try
            {
                value = ReadInt32();
            }
            catch
            {
                value = 0;
                return false;
            }
            return true;
        }

        public bool ReadUInt64(out ulong value)
        {
            try
            {
                value = ReadUInt64();
            }
            catch
            {
                value = 0;
                return false;
            }
            return true;
        }

        public bool ReadInt64(out long value)
        {
            try
            {
                value = ReadInt64();
            }
            catch
            {
                value = 0;
                return false;
            }
            return true;
        }

        public bool ReadSingle(out float value)
        {
            try
            {
                value = ReadSingle();
            }
            catch
            {
                value = 0;
                return false;
            }
            return true;
        }

        public DateTime ReadDateTime()
        {
            try
            {
                var result = (SYSTEMTIME)Read(new SYSTEMTIME());
                return result.ConvertTime();
            }
            catch
            {
                var result = new SYSTEMTIME();
                return result.ConvertTime();
            }
        }

        public IEnumerable<uint> Read(uint count)
        {
            for (int i = 0; i < count; i++)
            {
                yield return ReadUInt32();
            }
        }

        public IEnumerable<int> Read(int count)
        {
            for (int i = 0; i < count; i++)
            {
                yield return ReadInt32();
            }
        }
        //não testado
        public bool Read(out object value, int Count)
        {
            try
            {
                var obj = new object();
                byte[] recordData = ReadBytes(Count);

                IntPtr ptr = Marshal.AllocHGlobal(Count);

                Marshal.Copy(recordData, 0, ptr, Count);

                value = Marshal.PtrToStructure(ptr, obj.GetType());
                Marshal.FreeHGlobal(ptr);
            }
            catch
            {
                value = 0;
                return false;
            }
            return true;
        }


        public bool ReadBuffer<T>(ref T value, int Count)
        {
            try
            {
                byte[] recordData = ReadBytes(Count);

                IntPtr ptr = Marshal.AllocHGlobal(Count);

                Marshal.Copy(recordData, 0, ptr, Count);

                value = (T)Marshal.PtrToStructure(ptr, value.GetType());
                Marshal.FreeHGlobal(ptr);
            }
            catch
            {
                value = default;
                return false;
            }
            return true;
        }


        public T Read<T>() where T : new()
        {
            T local;
            int count = (typeof(T) == typeof(bool)) ? 1 : Marshal.SizeOf(typeof(T));
            GCHandle handle = GCHandle.Alloc(this.ReadBytes(count), GCHandleType.Pinned);
            try
            {
                local = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
            }
            finally
            {
                handle.Free();
            }
            return local;
        }

        public object Read(object value)
        {
            var Count = Marshal.SizeOf(value);

            byte[] recordData = ReadBytes(Count);

            IntPtr ptr = Marshal.AllocHGlobal(Count);

            Marshal.Copy(recordData, 0, ptr, Count);

            value = Marshal.PtrToStructure(ptr, value.GetType());
            Marshal.FreeHGlobal(ptr);
            return value;
        }

        public object Read(object value, object value_ori)
        {
            var Count = Marshal.SizeOf(value_ori);

            byte[] recordData = ReadBytes(Count);

            if (recordData.Length != Count)
            {
                throw new Exception(
                    $"The record length ({recordData.Length}) mismatches the length of the passed structure ({Count})");
            }

            IntPtr ptr = Marshal.AllocHGlobal(Count);

            Marshal.Copy(recordData, 0, ptr, Count);

            value = Marshal.PtrToStructure(ptr, value.GetType());
            Marshal.FreeHGlobal(ptr);
            return value;
        }

        public object Read(object value, int Count)
        {
            byte[] recordData = ReadBytes(Count);

            IntPtr ptr = Marshal.AllocHGlobal(Count);

            Marshal.Copy(recordData, 0, ptr, Count);

            value = Marshal.PtrToStructure(ptr, value.GetType());
            Marshal.FreeHGlobal(ptr);
            return value;
        }
        public Object ReadObject(object obj)
        {
            foreach (var property in obj.GetType().GetProperties())
            {
                Type type = property.PropertyType;

                TypeCode typeCode = Type.GetTypeCode(type);
                switch (typeCode)
                {
                    case TypeCode.Empty:
                        break;
                    case TypeCode.Object:
                        {
                            if (type.Name == "Byte[]")
                            {
                                //  property.SetValue(obj, ReadBytes(obj));
                            }
                            if (type.Name == "Long[]")
                            {
                                //  property.SetValue(obj, ReadBytes(obj));
                                var obj_value = (long[])obj;
                                for (int i = 0; i < obj_value.Length; i++)
                                {

                                }
                            }
                        }
                        break;
                    case TypeCode.DBNull:
                        break;
                    case TypeCode.Boolean:
                        {
                            property.SetValue(obj, ReadBoolean());
                        }
                        break;
                    case TypeCode.Char:
                        {
                            property.SetValue(obj, ReadChar());
                        }
                        break;
                    case TypeCode.SByte:
                        {
                            property.SetValue(obj, ReadSByte());
                        }
                        break;
                    case TypeCode.Byte:
                        {
                            property.SetValue(obj, ReadByte());
                        }
                        break;
                    case TypeCode.Int16:
                        {
                            property.SetValue(obj, ReadInt16());
                        }
                        break;
                    case TypeCode.UInt16:
                        {
                            property.SetValue(obj, ReadUInt16());
                        }
                        break;
                    case TypeCode.Int32:
                        {
                            property.SetValue(obj, ReadInt32());
                        }
                        break;
                    case TypeCode.UInt32:
                        property.SetValue(obj, ReadUInt32());
                        break;
                    case TypeCode.Int64:
                        {
                            property.SetValue(obj, ReadInt64());
                        }
                        break;
                    case TypeCode.UInt64:
                        {
                            property.SetValue(obj, ReadUInt64());
                        }
                        break;
                    case TypeCode.Single:
                        {
                            property.SetValue(obj, ReadSingle());
                        }
                        break;
                    case TypeCode.Double:
                        {
                            property.SetValue(obj, ReadDouble());
                        }
                        break;
                    case TypeCode.Decimal:
                        {
                            property.SetValue(obj, ReadDecimal());
                        }
                        break;
                    case TypeCode.DateTime:
                        {
                            property.SetValue(obj, ReadDateTime());
                        }
                        break;
                    case TypeCode.String:
                        {
                            property.SetValue(obj, ReadPStr());
                        }
                        break;
                    default:
                        {
                            Console.WriteLine("Object Type Name: " + typeCode);
                        }
                        break;
                }
            }
            return obj;
        }
        public void ReadObject(out object obj)
        {
            obj = new object();
            foreach (var property in obj.GetType().GetProperties())
            {
                Type type = property.PropertyType;

                TypeCode typeCode = Type.GetTypeCode(type);
                switch (typeCode)
                {
                    case TypeCode.Empty:
                        break;
                    case TypeCode.Object:
                        break;
                    case TypeCode.DBNull:
                        break;
                    case TypeCode.Boolean:
                        {
                            property.SetValue(obj, ReadBoolean());
                        }
                        break;
                    case TypeCode.Char:
                        {
                            property.SetValue(obj, ReadChar());
                        }
                        break;
                    case TypeCode.SByte:
                        {
                            property.SetValue(obj, ReadSByte());
                        }
                        break;

                    case TypeCode.Byte:
                        {
                            property.SetValue(obj, ReadByte());
                        }
                        break;
                    case TypeCode.Int16:
                        {
                            property.SetValue(obj, ReadInt16());
                        }
                        break;
                    case TypeCode.UInt16:
                        {
                            property.SetValue(obj, ReadUInt16());
                        }
                        break;
                    case TypeCode.Int32:
                        {
                            property.SetValue(obj, ReadInt32());
                        }
                        break;
                    case TypeCode.UInt32:
                        property.SetValue(obj, ReadUInt32());
                        break;
                    case TypeCode.Int64:
                        {
                            property.SetValue(obj, ReadInt64());
                        }
                        break;
                    case TypeCode.UInt64:
                        {
                            property.SetValue(obj, ReadUInt64());
                        }
                        break;
                    case TypeCode.Single:
                        {
                            property.SetValue(obj, ReadSingle());
                        }
                        break;
                    case TypeCode.Double:
                        {
                            property.SetValue(obj, ReadDouble());
                        }
                        break;
                    case TypeCode.Decimal:
                        {
                            property.SetValue(obj, ReadDecimal());
                        }
                        break;
                    case TypeCode.DateTime:
                        {
                            property.SetValue(obj, ReadDateTime());
                        }
                        break;
                    case TypeCode.String:
                        {
                            property.SetValue(obj, ReadPStr());
                        }
                        break;
                    default:
                        {
                            Console.WriteLine("Object Type Name: " + typeCode);
                        }
                        break;
                }
            }
            // return obj;
        }

        public IEnumerable<uint> ReadUInt32Array(uint count)
        {
            for (int i = 0; i < count; i++)
            {
                yield return ReadUInt32();
            }
        }
        public IEnumerable<int> ReadInt32Array(uint count)
        {
            for (int i = 0; i < count; i++)
            {
                yield return ReadInt32();
            }
        }

        public IEnumerable<ushort> ReadUInt16Array(ushort count)
        {
            for (int i = 0; i < count; i++)
            {
                yield return ReadUInt16();
            }
        }

        public IEnumerable<short> ReadInt16Array(ushort count)
        {
            for (int i = 0; i < count; i++)
            {
                yield return ReadInt16();
            }
        }

        public T ReadStruct<T>()
        {
            var count = Marshal.SizeOf(typeof(T));

            byte[] recordData = ReadBytes(count);

            if (recordData.Length != count)
            {
                throw new Exception(
                    $"The record({typeof(T).GetType().Name}) length ({recordData.Length}) mismatches the length of the passed structure ({count})");
            }

            var handle = GCHandle.Alloc(recordData, GCHandleType.Pinned);

            try
            {
                var stt = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
                return stt;
            }
            catch { handle.Free(); return (T)Activator.CreateInstance(typeof(T)); }
            finally
            {
                handle.Free();
            }
        }

        protected T _Read<T>(long real_size)
        {
            var count = Marshal.SizeOf(typeof(T));

            byte[] recordData = new byte[real_size];
            if (count > real_size)
            {
                recordData = ReadBytes((int)real_size);
                count = (int)real_size;
            }
            else if (real_size > count)
            {
                recordData = ReadBytes((int)real_size);
                count = (int)real_size;
            }
            else
            {
                recordData = ReadBytes(count);
            }
            var handle = GCHandle.Alloc(recordData, GCHandleType.Pinned);

            try
            {
                var stt = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
                return stt;
            }
            catch { handle.Free(); return (T)Activator.CreateInstance(typeof(T)); }
            finally
            {
                handle.Free();
            }
        }

        public T[] ReadStruct<T>(int count)
        {
            var array = new T[count];

            for (int i = 0; i < count; i++)
            {
                array[i] = ReadStruct<T>();
            }

            return array;
        }

        public T[] ReadStruct<T>(int count, int size)
        {
            var array = new T[count];

            for (int i = 0; i < count; i++)
            {
                array[i] = _Read<T>(size);
            }

            return array;
        }

        public object Read(object value, long real_size)
        {
            try
            {
                var count = Marshal.SizeOf(value);

                byte[] recordData = new byte[real_size];
                if (count > real_size)
                {
                    recordData = ReadBytes((int)real_size);
                    count = (int)real_size;
                }
                else if (real_size > count)
                {
                    recordData = ReadBytes((int)real_size);
                    count = (int)real_size;
                }
                else
                {
                    recordData = ReadBytes(count);
                }
                IntPtr ptr = Marshal.AllocHGlobal(count);

                Marshal.Copy(recordData, 0, ptr, count);

                value = Marshal.PtrToStructure(ptr, value.GetType());
                Marshal.FreeHGlobal(ptr);
                return value;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
        public byte[] ReadBytes()
        {
            return ReadBytes((int)this.Size);
        }
    }
}
