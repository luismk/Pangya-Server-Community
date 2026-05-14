#region License

/* Based on the MiniLZO library by Markus Oberhumer and some code
   from a port to C# by Frank Razenberg.

   Copyright (C) 1996-2019 Markus Franz Xaver Johannes Oberhumer
   All Rights Reserved.

   The LZO library is free software; you can redistribute it and/or
   modify it under the terms of the GNU General Public License as
   published by the Free Software Foundation; either version 2 of
   the License, or (at your option) any later version.

   The LZO library is distributed in the hope that it will be useful,
   but WITHOUT ANY WARRANTY; without even the implied warranty of
   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
   GNU General Public License for more details.

   You should have received a copy of the GNU General Public License
   along with the LZO library; see the file COPYING.
   If not, write to the Free Software Foundation, Inc.,
   51 Franklin Street, Fifth Floor, Boston, MA 02110-1301, USA.

   Markus F.X.J. Oberhumer
   <markus@oberhumer.com>
   http://www.oberhumer.com/opensource/lzo/
 */

#endregion

using System;
using System.IO;
namespace PangyaAPI.Network.Cryptor
{
    public static class MiniLzo
    {
        #region Decompression

        private static byte[] Lzo1XDecompress(byte[] @in)
        {
            uint t;
            var @out = new byte[0];
            uint op = 0;
            uint ip = 0;
            var gtFirstLiteralRun = false;
            var gtMatchDone = false;

            if (@in[ip] > 17)
            {
                t = (uint)(@in[ip++] - 17);
                EnsureSpace(t, ref @out, op);
                if (t > 0)
                    do
                    {
                        @out[op++] = @in[ip++];
                    } while (--t > 0);

                if (t >= 4) gtFirstLiteralRun = true;
            }

            while (true)
            {
                uint mPos;
                if (gtFirstLiteralRun)
                {
                    gtFirstLiteralRun = false;
                    goto FirstLiteralRun;
                }

                t = @in[ip++];
                if (t >= 16) goto match;
                if (t == 0)
                {
                    while (@in[ip] == 0)
                    {
                        t += 255;
                        ip++;
                    }

                    t += (uint)(15 + @in[ip++]);
                }

                t += 3;
                EnsureSpace(t, ref @out, op);
                if (t > 0)
                    do
                    {
                        @out[op++] = @in[ip++];
                    } while (--t > 0);

                FirstLiteralRun:
                t = @in[ip++];
                if (t >= 16)
                    goto match;
                mPos = op - (1 + 0x0800);
                mPos -= t >> 2;
                mPos -= (uint)@in[ip++] << 2;

                EnsureSpace(3, ref @out, op);
                @out[op++] = @out[mPos++];
                @out[op++] = @out[mPos++];
                @out[op++] = @out[mPos];
                gtMatchDone = true;

            match:
                do
                {
                    if (gtMatchDone)
                    {
                        gtMatchDone = false;
                        goto MatchDone;
                    }

                    if (t >= 64)
                    {
                        mPos = op - 1;
                        mPos -= (t >> 2) & 7;
                        mPos -= (uint)@in[ip++] << 3;
                        t = (t >> 5) - 1;

                        t += 2;
                        EnsureSpace(t, ref @out, op);
                        do
                        {
                            @out[op++] = @out[mPos++];
                        } while (--t > 0);

                        goto MatchDone;
                    }

                    if (t >= 32)
                    {
                        t &= 31;
                        if (t == 0)
                        {
                            while (@in[ip] == 0)
                            {
                                t += 255;
                                ip++;
                            }

                            t += (uint)(31 + @in[ip++]);
                        }

                        mPos = op - 1;
                        mPos -= ReadU16(@in, ip) >> 2;
                        ip += 2;
                    }
                    else if (t >= 16)
                    {
                        mPos = op;
                        mPos -= (t & 8) << 11;
                        t &= 7;
                        if (t == 0)
                        {
                            while (@in[ip] == 0)
                            {
                                t += 255;
                                ip++;
                            }

                            t += (uint)(7 + @in[ip++]);
                        }

                        mPos -= ReadU16(@in, ip) >> 2;
                        ip += 2;
                        if (mPos == op)
                            goto EofFound;
                        mPos -= 0x4000;
                    }
                    else
                    {
                        mPos = op - 1;
                        mPos -= t >> 2;
                        mPos -= (uint)@in[ip++] << 2;
                        EnsureSpace(2, ref @out, op);
                        @out[op++] = @out[mPos++];
                        @out[op++] = @out[mPos];
                        goto MatchDone;
                    }

                    t += 2;
                    EnsureSpace(t, ref @out, op);
                    do
                    {
                        @out[op++] = @out[mPos++];
                    } while (--t > 0);

                MatchDone:
                    t = (uint)(@in[ip - 2] & 3);
                    if (t == 0) break;

                    EnsureSpace(t, ref @out, op);
                    if (t > 0)
                        do
                        {
                            @out[op++] = @in[ip++];
                        } while (--t > 0);

                    t = @in[ip++];
                } while (true);
            }

        EofFound:
            Array.Resize(ref @out, (int)op);
            return @out;
        }
        private static byte[] lzo1x_decompress(in byte[] _lzo_bytep, uint in_len,
                                        out byte[] lzo_bytep, in uint out_len,
                                        object wrkmem)
        {
            byte[] @in = _lzo_bytep;
            byte[] @out = new byte[4096]; // Tamanho inicial arbitrário
            uint op = 0;
            uint ip = 0;
            uint t;
            bool gtFirstLiteralRun = false;
            bool gtMatchDone = false;

            if (ip < in_len && @in[ip] > 17)
            {
                t = (uint)(@in[ip++] - 17);
                EnsureSpace(t, ref @out, op);
                if (t > 0)
                {
                    do
                    {
                        @out[op++] = @in[ip++];
                    } while (--t > 0);

                    if (t >= 4) gtFirstLiteralRun = true;
                }
            }

            while (ip < in_len)
            {
                uint mPos;

                if (gtFirstLiteralRun)
                {
                    gtFirstLiteralRun = false;
                    goto FirstLiteralRun;
                }

                t = @in[ip++];
                if (t >= 16) goto match;

                if (t == 0)
                {
                    while (ip < in_len && @in[ip] == 0)
                    {
                        t += 255;
                        ip++;
                    }

                    t += (uint)(15 + @in[ip++]);
                }

                t += 3;
                EnsureSpace(t, ref @out, op);
                if (t > 0)
                {
                    do
                    {
                        @out[op++] = @in[ip++];
                    } while (--t > 0);
                }

            FirstLiteralRun:
                t = @in[ip++];
                if (t >= 16) goto match;

                mPos = op - (1 + 0x0800);
                mPos -= t >> 2;
                mPos -= (uint)@in[ip++] << 2;

                EnsureSpace(3, ref @out, op);
                @out[op++] = @out[mPos++];
                @out[op++] = @out[mPos++];
                @out[op++] = @out[mPos];
                gtMatchDone = true;

            match:
                do
                {
                    if (gtMatchDone)
                    {
                        gtMatchDone = false;
                        goto MatchDone;
                    }

                    if (t >= 64)
                    {
                        mPos = op - 1;
                        mPos -= (t >> 2) & 7;
                        mPos -= (uint)@in[ip++] << 3;
                        t = (t >> 5) - 1;

                        t += 2;
                        EnsureSpace(t, ref @out, op);
                        do
                        {
                            @out[op++] = @out[mPos++];
                        } while (--t > 0);

                        goto MatchDone;
                    }

                    if (t >= 32)
                    {
                        t &= 31;
                        if (t == 0)
                        {
                            while (ip < in_len && @in[ip] == 0)
                            {
                                t += 255;
                                ip++;
                            }

                            t += (uint)(31 + @in[ip++]);
                        }

                        mPos = op - 1;
                        mPos -= ReadU16(@in, ip) >> 2;
                        ip += 2;
                    }
                    else if (t >= 16)
                    {
                        mPos = op;
                        mPos -= (t & 8) << 11;
                        t &= 7;
                        if (t == 0)
                        {
                            while (ip < in_len && @in[ip] == 0)
                            {
                                t += 255;
                                ip++;
                            }

                            t += (uint)(7 + @in[ip++]);
                        }

                        mPos -= ReadU16(@in, ip) >> 2;
                        ip += 2;

                        if (mPos == op)
                            goto EofFound;

                        mPos -= 0x4000;
                    }
                    else
                    {
                        mPos = op - 1;
                        mPos -= t >> 2;
                        mPos -= (uint)@in[ip++] << 2;
                        EnsureSpace(2, ref @out, op);
                        @out[op++] = @out[mPos++];
                        @out[op++] = @out[mPos];
                        goto MatchDone;
                    }

                    t += 2;
                    EnsureSpace(t, ref @out, op);
                    do
                    {
                        @out[op++] = @out[mPos++];
                    } while (--t > 0);

                MatchDone:
                    t = (uint)(@in[ip - 2] & 3);
                    if (t == 0) break;

                    EnsureSpace(t, ref @out, op);
                    if (t > 0)
                    {
                        do
                        {
                            @out[op++] = @in[ip++];
                        } while (--t > 0);
                    }

                    t = @in[ip++];
                } while (ip < in_len);
            }

        EofFound:
            Array.Resize(ref @out, (int)op);
            lzo_bytep = @out;
            return @out;
        }


        #endregion Decompression

        #region Helpers

        private static uint ReadU32(byte[] arr, uint i)
        {
            return (uint)(arr[i] | (arr[i + 1] << 8) | (arr[i + 2] << 16) | (arr[i + 3] << 24));
        }

        private static uint ReadU16(byte[] arr, uint i)
        {
            return (uint)(arr[i] | (arr[i + 1] << 8));
        }

        private static readonly int[] MultiplyDeBruijnBitPosition =
        {
            0, 1, 28, 2, 29, 14, 24, 3, 30, 22, 20, 15, 25, 17, 4, 8,
            31, 27, 13, 23, 21, 19, 16, 7, 26, 12, 18, 6, 11, 5, 10, 9
        };

        private static int LzoBitOpsCtz32(uint v)
        {
            return MultiplyDeBruijnBitPosition[(uint)((v & -v) * 0x077CB531U) >> 27];
        }

        private static uint NearestPowerOfTwo(uint n)
        {
            n--;
            n |= n >> 1;
            n |= n >> 2;
            n |= n >> 4;
            n |= n >> 8;
            n |= n >> 16;
            return n + 1;
        }

        private static void AllocSpace(ref byte[] array, uint minLength)
        {
            Array.Resize(ref array, (int)NearestPowerOfTwo(minLength));
        }

        private static void EnsureSpace(uint need, ref byte[] array, uint pos)
        {
            var needLen = pos + need;
            if (needLen > array.Length) AllocSpace(ref array, needLen);
        }

        #endregion

        #region Compression

        private static uint Lzo1X1CompressCore(byte[] @in, uint inIndex, uint inLen, byte[] @out, uint outIndex,
            out uint outLen, uint ti, ushort[] dict)
        {
            var inEnd = inIndex + inLen;
            var ipEnd = inIndex + inLen - 20;
            var op = outIndex;
            var ip = inIndex;
            var ii = ip;
            ip += ti < 4 ? 4 - ti : 0;

            for (; ; )
            {
            literal:
                ip += 1 + ((ip - ii) >> 5);
            next:
                if (ip >= ipEnd)
                    break;
                var dv = ReadU32(@in, ip);
                var dIndex = (((0x1824429d * dv) >> (32 - 14)) & (((1u << 14) - 1) >> 0)) << 0;
                var mPos = inIndex + dict[dIndex];
                dict[dIndex] = (ushort)(ip - inIndex);
                if (dv != ReadU32(@in, mPos))
                    goto literal;

                ii -= ti;
                ti = 0;
                {
                    var t = ip - ii;
                    if (t != 0)
                    {
                        if (t <= 3)
                        {
                            @out[op - 2] |= (byte)t;
                            Array.Copy(@in, ii, @out, op, t);
                            op += t;
                        }
                        else if (t <= 16)
                        {
                            @out[op++] = (byte)(t - 3);
                            Array.Copy(@in, ii, @out, op, t);
                            op += t;
                        }
                        else
                        {
                            if (t <= 18)
                            {
                                @out[op++] = (byte)(t - 3);
                            }
                            else
                            {
                                var tt = t - 18;
                                @out[op++] = 0;
                                while (tt > 255)
                                {
                                    tt -= 255;
                                    @out[op++] = 0;
                                }

                                @out[op++] = (byte)tt;
                            }

                            Array.Copy(@in, ii, @out, op, t);
                            op += t;
                        }
                    }
                }
                uint mLen = 4;
                {
                    var v = ReadU32(@in, ip + mLen) ^ ReadU32(@in, mPos + mLen);
                    while (v == 0)
                    {
                        mLen += 4;
                        v = ReadU32(@in, ip + mLen) ^ ReadU32(@in, mPos + mLen);
                        if (ip + mLen >= ipEnd)
                            goto m_len_done;
                    }

                    mLen += (uint)LzoBitOpsCtz32(v) / 8;
                }
            m_len_done:
                var mOff = ip - mPos;
                ip += mLen;
                ii = ip;
                if (mLen <= 8 && mOff <= 0x0800)
                {
                    mOff -= 1;
                    @out[op++] = (byte)(((mLen - 1) << 5) | ((mOff & 7) << 2));
                    @out[op++] = (byte)(mOff >> 3);
                }
                else if (mOff <= 0x4000)
                {
                    mOff -= 1;
                    if (mLen <= 33)
                    {
                        @out[op++] = (byte)(32 | (mLen - 2));
                    }
                    else
                    {
                        mLen -= 33;
                        @out[op++] = 32 | 0;
                        while (mLen > 255)
                        {
                            mLen -= 255;
                            @out[op++] = 0;
                        }

                        @out[op++] = (byte)mLen;
                    }

                    @out[op++] = (byte)(mOff << 2);
                    @out[op++] = (byte)(mOff >> 6);
                }
                else
                {
                    mOff -= 0x4000;
                    if (mLen <= 9)
                    {
                        @out[op++] = (byte)(16 | ((mOff >> 11) & 8) | (mLen - 2));
                    }
                    else
                    {
                        mLen -= 9;
                        @out[op++] = (byte)(16 | ((mOff >> 11) & 8));
                        while (mLen > 255)
                        {
                            mLen -= 255;
                            @out[op++] = 0;
                        }

                        @out[op++] = (byte)mLen;
                    }

                    @out[op++] = (byte)(mOff << 2);
                    @out[op++] = (byte)(mOff >> 6);
                }

                goto next;
            }

            outLen = op - outIndex;
            return inEnd - (ii - ti);
        }

        private static void Lzo1X1Compress(byte[] @in, uint inLen, byte[] @out, out uint outLen, ushort[] dict)
        {
            uint ip = 0;
            uint op = 0;
            var l = inLen;
            uint t = 0;
            while (l > 20)
            {
                var ll = l;
                ll = ll <= 49152 ? ll : 49152;
                var llEnd = ip + ll;
                if (llEnd + ((t + ll) >> 5) <= llEnd || llEnd + ((t + ll) >> 5) <= ip + ll)
                    break;

                for (var i = 0; i < (1 << 14) * sizeof(ushort); i++)
                    dict[i] = 0;
                t = Lzo1X1CompressCore(@in, ip, ll, @out, op, out outLen, t, dict);
                ip += ll;
                op += outLen;
                l -= ll;
            }

            t += l;
            if (t > 0)
            {
                ulong ii = inLen - t;
                if (op == 0 && t <= 238)
                {
                    @out[op++] = (byte)(17 + t);
                }
                else if (t <= 3)
                {
                    @out[op - 2] |= (byte)t;
                }
                else if (t <= 18)
                {
                    @out[op++] = (byte)(t - 3);
                }
                else
                {
                    var tt = t - 18;
                    @out[op++] = 0;
                    while (tt > 255)
                    {
                        tt -= 255;
                        @out[op++] = 0;
                    }

                    @out[op++] = (byte)tt;
                }

                do
                {
                    @out[op++] = @in[ii++];
                } while (--t > 0);
            }

            @out[op++] = 16 | 1;
            @out[op++] = 0;
            @out[op++] = 0;
            outLen = op;
        }

        #endregion

        #region API

        public static byte[] Decompress(byte[] input)
        {
            try
            {
                return Lzo1XDecompress(input);
            }
            catch (IndexOutOfRangeException e)
            {
                throw new IOException("Input buffer ends too early", e);
            }
        }

        public static byte[] Compress(byte[] input)
        {
            var @out = new byte[input.Length + input.Length / 16 + 64 + 3];
            Lzo1X1Compress(input, (uint)input.Length, @out, out var outLen, new ushort[32768]);
            Array.Resize(ref @out, (int)outLen);
            return @out;
        }
        public static void compress_data(byte[] @_uncompress,
                uint _size_uncompress,
                byte[] @_compress,
                uint @_size_compress)
        {
            Lzo1X1Compress(@_uncompress,
                       _size_uncompress, 
                       @_compress,
                      out @_size_compress,
                       new ushort[32768]);
            Array.Resize(ref @_uncompress, (int)@_size_compress);
        }
        public static void decompress_data(in byte[] @_compress,
                in uint @_size_compress,
             out byte[] @_uncompress,
                in uint @_size_uncompress,
                in uint @_size_decompress /*usado somente para check*/)
        {
            lzo1x_decompress(_compress,
                        _size_compress, out _uncompress,
                        _size_uncompress, null);
            Array.Resize(ref @_uncompress, (int)@_size_compress);
        }
        #endregion
    }
}
