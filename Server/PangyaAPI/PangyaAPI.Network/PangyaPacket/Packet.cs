using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Runtime.Remoting.Messaging;
using System.Text;
using PangyaAPI.Network.Cryptor;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Models;
using uint8_t = System.Byte;

namespace PangyaAPI.Network.PangyaPacket
{

    // Structs auxiliares convertidas de packet.hpp

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class packet_head
    {
        public byte low_key;
        public ushort size;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class packet_head_client : packet_head
    {
        public byte seq;
    }

    public class offset_index
    {
        public byte[] m_buf;
        public ulong m_index_r;
        public ulong m_index_w;
        public ulong m_size;
        public ulong m_size_alloced;

        public void clear() { if (m_buf != null && m_buf.Length > 0) { Array.Clear(m_buf, 0, m_buf.Length); } }
        public void reset_read() => m_index_r = 0;
        public void reset_write()
        {
            m_index_w = 0;
            m_size = 0;
        }
        public void reset()
        {
            reset_read();
            reset_write();
        }
    }

    public class conversionByte
    {
        public const byte CB_BASE_256 = 10;
        public const byte CB_BASE_255 = 20;
        public const byte CB_SEQ_NORMAL = 1;
        public const byte CB_SEQ_INVERTIDA = 2;
        public const byte CB_PARAM_DEFAULT = 0;

        public unionConvertidoStruct unionConvertido;
        private byte m_flag;
        private uint ulNumber_temp;

        public conversionByte()
        {
            unionConvertido = new unionConvertidoStruct();
        }

        public conversionByte(uint _dwConvertido, byte _flag = CB_PARAM_DEFAULT)
        {
            unionConvertido = new unionConvertidoStruct { dwConvertido = _dwConvertido };
            m_flag = _flag;
            if (m_flag != CB_PARAM_DEFAULT) invert();
        }

        public conversionByte(byte[] _ucpConvertido, byte _flag = CB_PARAM_DEFAULT)
        {
            unionConvertido = new unionConvertidoStruct();
            m_flag = _flag;

            if (_ucpConvertido != null && _ucpConvertido.Length >= 4)
                unionConvertido.dwConvertido = BitConverter.ToUInt32(_ucpConvertido, 0);

            if (m_flag != CB_PARAM_DEFAULT)
                invert();
        }

        private void invert()
        {
            if ((m_flag & CB_BASE_255) != 0)
            {
                unionConvertido.dwConvertido = getNumberIS();
                unionConvertido.dwConvertido = getNumberBase256();
            }
            else
            {
                unionConvertido.dwConvertido = getNumberBase255();
                unionConvertido.dwConvertido = getNumberIS();
            }
        }

        public uint getNumberNS() => unionConvertido.dwConvertido;

        public uint getNumberIS()
        {
            var number = (uint)(unionConvertido.a << 24 | unionConvertido.b << 16 | unionConvertido.c << 8 | unionConvertido.d);
            return number;
        }

        public byte[] getLPUCNS()
        {
            ulNumber_temp = getNumberNS();
            return BitConverter.GetBytes(ulNumber_temp);
        }

        public byte[] getLPUCIS()
        {
            ulNumber_temp = getNumberIS();
            return BitConverter.GetBytes(ulNumber_temp);
        }

        public uint getNumberBase256() => getNumberNS() * 255 / 256 + 1;
        public uint getNumberBase255() => ((unionConvertido.dwConvertido / 255) << 8) | unionConvertido.dwConvertido % 255;

        public uint getISNumberBase256() => getNumberIS() * 255 / 256 + 1;
        public uint getISNumberBase255() => ((getNumberIS() / 255) << 8) | getNumberIS() % 255;

        public int putNumberBuffer(byte[] buffer)
        {
            if (buffer == null || buffer.Length < 4)
                return -1;

            var bytes = BitConverter.GetBytes(unionConvertido.dwConvertido);
            Array.Copy(bytes, 0, buffer, 0, 4);
            return 4;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct unionConvertidoStruct
        {
            public uint dwConvertido;
            public byte a => (byte)((dwConvertido >> 24) & 0xFF);
            public byte b => (byte)((dwConvertido >> 16) & 0xFF);
            public byte c => (byte)((dwConvertido >> 8) & 0xFF);
            public byte d => (byte)(dwConvertido & 0xFF);
        }
    }

    public partial class packet : IDisposable
    {
        private MemoryStream _stream;
        /// <summary>
        /// Leitor do packet
        /// </summary>
        private PangyaBinaryReader _reader;
        /// <summary>
        /// Leitor do packet
        /// </summary>
        private PangyaBinaryWriter _writer;
        private Random Random = new Random(); 

        /// <summary>
        /// Mensagem do Packet
        /// </summary>
        public byte[] Decrypt_Msg { get; set; }
        public byte[] Encrypt_Msg { get; set; }

        /// <summary>
        /// Id do Packet
        /// </summary>
        public short Id { get; private set; } = -1;
        public bool IsRaw { get; private set; } = true;
        public uint PublicKey { get; private set; }
        public uint PrivateKey { get; private set; }       
        public packet(ushort _id)
        {
            _writer = new PangyaBinaryWriter();

            _writer.init_plain(_id);
        }
        public packet()
        { clear(); }

        public packet(byte[] rawPacket)
        {
            Decrypt_Msg = rawPacket;
            SetBuffer(rawPacket);
            Id = ReadInt16();
        }

        public packet(byte[] rawPacket, bool auth)
        {
            packet_head ph = Tools.memcpy<packet_head>(rawPacket);

            rawPacket = rawPacket.Slice(4); //3size, -1 byte aleatory

            Decrypt_Msg = rawPacket;
            SetBuffer(rawPacket);
             Id = ReadInt16();//auth envia bit
        }


        private void SetBuffer(byte[] data)
        {
            _writer?.Dispose();
            _reader?.Dispose();
            _stream?.Dispose();

            _stream = new MemoryStream(data, 0, data.Length, writable: true, publiclyVisible: true);
            _writer = new PangyaBinaryWriter(_stream, Encoding.GetEncoding("shift_jis"), leaveOpen: true);
            _reader = new PangyaBinaryReader(_stream, Encoding.GetEncoding("shift_jis"), leaveOpen: true);
        }
        public uint GetSize
        {
            get => _reader.Size;
        }
        public byte[] ToArray() => _stream.ToArray();
        public long Position { get => _stream.Position; set => _stream.Position = value; }
        public long Length => _stream.Length;

        public byte[] getBuffer()
        {
            Encrypt_Msg = _writer.GetBytes;

            return (Encrypt_Msg);
        }

        public short getTipo() => Id;
        public uint GetPos
        {
            get => _reader.GetPosition();
        }

        public double ReadDouble()
        {
            return _reader.ReadDouble();
        }

        public byte ReadUInt8()
        {
            return _reader.ReadByte();
        }

        public short ReadInt16()
        {
            return _reader.ReadInt16();
        }
        public ushort ReadUInt16()
        {
            return _reader.ReadUInt16();
        }
        public uint[] ReadUInt32(uint size)
        {
            return _reader.Read(size).ToArray();
        }
        public uint ReadUInt32()
        {
            return _reader.ReadUInt32();
        }
        public int[] ReadInt32(int size)
        {
            return _reader.Read(size).ToArray();
        }
        public int ReadInt32()
        {
            return _reader.ReadInt32();
        }

        public ulong ReadUInt64()
        {
            return _reader.ReadUInt64();
        }

        public long ReadInt64()
        {
            return _reader.ReadInt64();
        }

        public float ReadSingle()
        {
            return _reader.ReadSingle();
        }

        public string ReadString()
        {
            return _reader.ReadPStr();
        }

        public string ReadSJISString() => _reader.ReadSJISString();
        public void Skip(int count)
        {
            _reader.Skip(count);
        }
        /// <summary>
        /// seta uma nova ou retorna uma posicao/dados anteriores
        /// </summary>
        /// <param name="offset">posicao da onde vai comecar ou onde ira pular</param>
        /// <param name="origin">0 = inicio do packet, 1 da onde esta(usado para pular dados), 2 final(pode dar exception)</param>
        public void Seek(int offset, int origin)
        {
            _reader.Seek(offset, origin);
        }

        public T ReadStruct<T>()
        {
            return _reader.ReadStruct<T>();
        }

        public T Read<T>() where T : new()
        {
            return _reader.Read<T>();
        }

        public IEnumerable<uint> Read(uint count)
        {
            return _reader.Read(count);
        }

        public object Read(object value, int Count)
        {
            return _reader.Read(value, Count);
        }

        public object Read(object value)
        {
            return _reader.Read(value);
        }

        public string ReadPStr(uint Count)
        {
            var data = new byte[Count];
            //ler os dados
            _reader.BaseStream.Read(data, 0, (int)Count);
            var value = Encoding.ASCII.GetString(data);
            return value;
        }

        public bool ReadPStr(out string value, uint Count)
        {
            return _reader.ReadPStr(out value, Count);
        }
        public bool ReadPStr(out string value)
        {
            return _reader.ReadPStr(out value);
        }
        public string ReadPStr()
        {
            return _reader.ReadPStr();
        }
        public bool ReadDouble(out Double value)
        {
            return _reader.ReadDouble(out value);
        }

        public sbyte[] ReadSBytes(int size)
        {
            return _reader.ReadSBytes(size);
        }

        public bool ReadBytes(out byte[] value)
        {
            return _reader.ReadBytes(out value);
        }

        public bool ReadBytes(out byte[] value, int len)
        {
            return _reader.ReadBytes(out value, len);
        }

        public bool ReadBytes(ref byte[] value, uint len)
        {
            _reader.ReadBytes(out value, (int)len);
            return true;
        }
        public bool ReadByte(out byte value)
        {
            return _reader.ReadByte(out value);
        }
        public byte ReadByte()
        {
            return _reader.ReadByte();
        }
        public bool ReadInt16(out short value)
        {
            return _reader.ReadInt16(out value);
        }
        public bool ReadUInt16(out ushort value)
        {
            return _reader.ReadUInt16(out value);
        }

        public bool ReadUInt32(out uint value)
        {
            return _reader.ReadUInt32(out value);
        }

        public bool ReadInt32(out int value)
        {
            return _reader.ReadInt32(out value);
        }

        public bool ReadUInt64(out ulong value)
        {
            return _reader.ReadUInt64(out value);
        }

        public bool ReadInt64(out long value)
        {
            return _reader.ReadInt64(out value);
        }

        public bool ReadSingle(out float value)
        {
            return _reader.ReadSingle(out value);
        }

        public sbyte ReadSByte()
        {
            return _reader.ReadSByte();
        }

        public void ReadBuffer<T>(ref T value, int size)
        {
            _reader.ReadBuffer(ref value, size);
        }

        public float ReadFloat()
        {
            return _reader.ReadSingle();
        }


        public byte[] GetRemainingData
        {
            get => _reader.GetRemainingData();
        }

        public int BytesRemaining
        {
            get => _reader.GetRemainingData().Count();
        }

        public byte[] ReadBytes(int count)
        {
            return _reader.ReadBytes(count);
        }


        public void SetReader(PangyaBinaryReader read)
        {
            _reader = read;
        }     

        public string Log()
        {
            return Decrypt_Msg.HexDump();
        }
                 
        public void clear()
        {
            _writer = new PangyaBinaryWriter();
            _stream = new MemoryStream();
            _reader = new PangyaBinaryReader(_stream);
            Decrypt_Msg = new byte[0];
            Encrypt_Msg = new uint8_t[0];
        }


        public void Dispose()
        {
            _reader?.Dispose();
            _writer?.Dispose();
            _stream?.Dispose();
        }

        public void AddByte(byte value)
        {
            _writer.WriteByte(value);
        }

        public void AddUInt32(uint value)
        {
            _writer.Write(value);
        }

        public void AddInt32(int value)
        {
            _writer.Write(value);
        }

        public void AddString(string value)
        {
            _writer.WriteString(value);
        }

        public byte[] makeRaw()
        {
            packet_head ph = new packet_head();

            if (_writer.GetBytes == null)
            {
                throw new exception("Error buf is null em packet::makeRaw()", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PACKET,
                    15, 0));
            }

            ph.low_key = 0; // low part of key random - 0 nesse pacote porque ele é o primiero que passa a chave
            ph.size = (ushort)(_writer.GetBytes.Length + 1);

            var m_maked = _writer.GetBytes;
            // Maked Reset
            _writer.Clear();

            _writer.WriteBuffer(ph, 3);
            _writer.WriteByte(0);// byte com valor 0 para dizer que é um pacote raw
            _writer.WriteBytes(m_maked);

            return getBuffer();
        } 
    }
}
