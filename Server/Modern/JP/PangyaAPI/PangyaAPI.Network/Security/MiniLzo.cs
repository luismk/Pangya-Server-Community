using System;
using System.Collections.Generic;
using System.Text;

namespace PangyaAPI.Network.Security
{
    public class Lzo
    {

        public static uint Compress(
    byte[] sourceData,
    uint sourceSize,
    byte[] destinationBuffer,
    uint destinationSize,
    bool disableCompression = false,
    int destinationOffset = 0)
        {
            if (sourceData == null || destinationBuffer == null)
                throw new Exception("invalid buffer");

            if (sourceSize > sourceData.Length)
                throw new Exception("invalid uncompressed sourceSize");

            // Header (4 bytes)
            destinationBuffer[destinationOffset + 0] = (byte)(disableCompression ? 1 : 0);

            uint bytesWritten = 0;

            if (!disableCompression)
            {
                bytesWritten = Lzo1X1Compress(
                    sourceData,
                    sourceSize,
                    destinationBuffer,
                    destinationOffset + 4,
                    destinationSize - 4
                );
            }
            else
            {
                Buffer.BlockCopy(sourceData, 0, destinationBuffer, destinationOffset + 4, (int)sourceSize);
                bytesWritten = sourceSize;
            }

            uint dataLength = sourceSize;

            byte lengthByteLow = (byte)(dataLength % 0xFF);
            byte lengthByteMiddle = (byte)(((dataLength - lengthByteLow) / 0xFF) % 0xFF);
            byte lengthByteHigh = (byte)(((((dataLength - lengthByteLow) / 0xFF) - lengthByteMiddle) / 0xFF) % 0xFF);

            destinationBuffer[destinationOffset + 3] = lengthByteLow;
            destinationBuffer[destinationOffset + 2] = lengthByteMiddle;
            destinationBuffer[destinationOffset + 1] = lengthByteHigh;

            return bytesWritten + 4;
        }

        public static byte[] Compress(byte[] sourceData, bool disableCompression = false)
        {
            if (sourceData == null)
                throw new Exception("invalid data");

            byte[] header = new byte[4];
            int dataLength = sourceData.Length;
            byte[] compressedData;

            if (!disableCompression)
            {
                header[0] = 0; // ServerFlag de compressão ativa
                compressedData = Compress_Data(sourceData);
            }
            else
            {
                header[0] = 1; // ServerFlag de sem compressão
                compressedData = sourceData;
            }

            // Concatena Header + Body
            byte[] result = new byte[4 + compressedData.Length];
            Array.Copy(header, 0, result, 0, 4);
            Array.Copy(compressedData, 0, result, 4, compressedData.Length);

            result[3] = (byte)(dataLength % 0xFF);
            result[2] = (byte)(((dataLength - result[3]) / 0xFF) % 0xFF);
            result[1] = (byte)(((((dataLength - result[3]) / 0xFF) - result[2]) / 0xFF) % 0xFF);

            return result;
        }

        private static uint Lzo1X1Compress(
    byte[] sourceData,
    uint sourceSize,
    byte[] destinationBuffer,
    int destinationOffset,
    uint destinationSize)
        {
            byte[] compressedData = Compress_Data(sourceData);

            if (compressedData.Length > destinationSize)
                throw new Exception("destinationBuffer buffer too small");

            Buffer.BlockCopy(compressedData, 0, destinationBuffer, destinationOffset, compressedData.Length);

            return (uint)compressedData.Length;
        }

        public static byte[] Decompress(byte[] sourceData)
        {
            if (sourceData == null)
                throw new Exception("invalid data");

            if (sourceData.Length < 4)
                throw new Exception($"invalid dataLength({sourceData.Length})");

            int compressionFlag = sourceData[0];

            // Reconstrói o resultSize original usando a mesma aritmética do JS
            // 0xFE01 é 255 * 255
            int dataLength = sourceData[3] + (sourceData[2] * 0xFF) + (sourceData[1] * 0xFE01);

            if (compressionFlag > 0)
            {
                if (dataLength > (sourceData.Length - 4))
                    throw new Exception($"invalid original sourceSize: {dataLength}");

                byte[] matchType = new byte[dataLength];
                Array.Copy(sourceData, 4, matchType, 0, dataLength);
                return matchType;
            }

            // Pega os dados após o header de 4 bytes
            byte[] compressedBody = sourceData.Skip(4).ToArray();
            byte[] result = Decompress_Data(compressedBody, dataLength);

            if (result.Length != dataLength)
                throw new Exception($"decompress dataLength not Match. {dataLength} != {result.Length}");

            return result;
        }

        static byte[] Compress_Data(byte[] sourceBuffer)
        {
            int sourceSize = sourceBuffer.Length;

            int[] hashTable = Enumerable.Repeat(-1, 0x4000).ToArray();
            byte[] source = sourceBuffer;
            byte[] destination = new byte[sourceSize + 15];

            int remainingBytes = 0, destinationIndex = 0, sourceIndex = 0, literalStartIndex = 0, value = 0, distance = 0, repeatCount = 0, hashValue = 0;
            int matchIndex = 0, matchType = 0, matchDistance = 0, token = 0, literalLength = 0, extendedLiteralLength = 0, repeatValue = 0;

            if (sourceSize > 13)
            {
                sourceIndex += 4;

                do
                {
                    // Lógica de Hash do Pangya
                    hashValue = source[sourceIndex] << 6;
                    hashValue ^= source[sourceIndex + 1];
                    hashValue <<= 5;
                    hashValue ^= source[sourceIndex + 2];
                    hashValue <<= 5;
                    hashValue ^= source[sourceIndex + 3];
                    hashValue *= 0x21;
                    hashValue = (int)((uint)hashValue & 0xFFFFFFFF); // Garante 32-bit unsigned behavior
                    hashValue >>= 5;
                    hashValue &= 0x3FFF;

                    matchIndex = hashTable[hashValue];

                    if (matchIndex >= 0)
                    {
                        matchDistance = distance = sourceIndex - matchIndex;

                        if (distance != 0)
                        {
                            if (distance < 0xBFFF && distance > 0)
                            {
                                if (distance > 2048 && source[matchIndex + 3] != source[sourceIndex + 3])
                                {
                                    hashValue = (hashValue & 0x7FF) ^ 0x201F;
                                    matchIndex = hashTable[hashValue];

                                    if (matchIndex >= 0)
                                    {
                                        matchDistance = distance = sourceIndex - matchIndex;
                                        if (distance != 0)
                                        {
                                            if (distance < 0xBFFF && distance > 0)
                                            {
                                                if (distance > 2048 && source[matchIndex + 3] != source[sourceIndex + 3])
                                                    matchType = 1;
                                                else
                                                {
                                                    distance = matchDistance;
                                                    matchType = 2;
                                                }
                                            }
                                            else matchType = 1;
                                        }
                                        else matchType = 1;
                                    }
                                    else matchType = 1;
                                }
                                else matchType = 2;
                            }
                            else matchType = 1;
                        }
                        else matchType = 1;
                    }
                    else matchType = 1;

                    if (matchType == 2)
                    {
                        if (source[matchIndex] == source[sourceIndex]
                            && source[matchIndex + 1] == source[sourceIndex + 1]
                            && source[matchIndex + 2] == source[sourceIndex + 2])
                        {
                            token = sourceIndex - literalStartIndex;
                            hashTable[hashValue] = sourceIndex;

                            if (token != 0)
                            {
                                literalLength = token;
                                if (token > 3)
                                {
                                    if (token > 0x12)
                                    {
                                        value = token - 0x12;
                                        destination[destinationIndex++] = 0;
                                        extendedLiteralLength = value;

                                        if (value > 0xFF)
                                        {
                                            repeatCount = (int)Math.Floor((double)value / 0xFF);
                                            repeatValue = repeatCount;
                                            do { destination[destinationIndex++] = 0; } while (--repeatCount > 0);
                                            extendedLiteralLength = value % 0xFF;
                                            token = literalLength;
                                        }
                                        value = extendedLiteralLength;
                                    }
                                    else value = token - 3;

                                    destination[destinationIndex++] = (byte)(value & 0xFF);
                                }
                                else
                                {
                                    destination[destinationIndex - 2] |= (byte)token;
                                }

                                do
                                {
                                    destination[destinationIndex++] = source[literalStartIndex++];
                                } while (--token > 0);
                            }

                            distance = source[sourceIndex + 3];
                            sourceIndex += 4;

                            if (distance == source[matchIndex + 3])
                            {
                                distance = source[sourceIndex++];
                                if (distance == source[matchIndex + 4])
                                {
                                    distance = source[sourceIndex++];
                                    if (distance == source[matchIndex + 5])
                                    {
                                        distance = source[sourceIndex++];
                                        if (distance == source[matchIndex + 6])
                                        {
                                            distance = source[sourceIndex++];
                                            if (distance == source[matchIndex + 7])
                                            {
                                                distance = source[sourceIndex++];
                                                if (distance == source[matchIndex + 8])
                                                {
                                                    matchIndex += 9;
                                                    if (sourceIndex < sourceSize)
                                                    {
                                                        do
                                                        {
                                                            if (source[matchIndex] != source[sourceIndex]) break;
                                                            matchIndex++; sourceIndex++;
                                                        } while (sourceIndex < sourceSize);
                                                    }

                                                    distance = sourceIndex - literalStartIndex;
                                                    token = matchDistance;

                                                    if (token > 0x4000)
                                                    {
                                                        token -= 0x4000;
                                                        matchDistance = token;
                                                        token >>= 0x0B;
                                                        token &= 8;

                                                        if (distance > 9)
                                                        {
                                                            distance -= 9;
                                                            token |= 0x10;
                                                            destination[destinationIndex] = (byte)(token & 0xFF);
                                                            matchType = 3;
                                                        }
                                                        else
                                                        {
                                                            distance -= 2;
                                                            token |= distance;
                                                            token |= 0x10;
                                                            destination[destinationIndex] = (byte)(token & 0xFF);
                                                            value = matchDistance;
                                                            matchType = 4;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        matchDistance = --token;
                                                        if (distance > 0x21)
                                                        {
                                                            distance -= 0x21;
                                                            destination[destinationIndex] = 0x20;
                                                            matchType = 3;
                                                        }
                                                        else
                                                        {
                                                            distance -= 2;
                                                            distance |= 0x20;
                                                            destination[destinationIndex] = (byte)(distance & 0xFF);
                                                            value = token;
                                                            matchType = 4;
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }

                            if (matchType != 3 && matchType != 4)
                            {
                                sourceIndex--;
                                distance = sourceIndex - literalStartIndex;
                                value = matchDistance;

                                if (value <= 0x800)
                                {
                                    value--; distance--;
                                    distance <<= 3;
                                    token = value & 7;
                                    distance |= token;
                                    distance <<= 2;
                                    value >>= 3;
                                    destination[destinationIndex++] = (byte)(distance & 0xFF);
                                    destination[destinationIndex++] = (byte)(value & 0xFF);
                                    literalStartIndex = sourceIndex;
                                }
                                else
                                {
                                    distance -= 2;
                                    if (value > 0x4000)
                                    {
                                        value -= 0x4000;
                                        token = value >> 0x0B;
                                        token &= 8; token |= distance; token |= 0x10;
                                        destination[destinationIndex] = (byte)(token & 0xFF);
                                        matchType = 4;
                                    }
                                    else
                                    {
                                        value--;
                                        distance |= 0x20;
                                        destination[destinationIndex] = (byte)(distance & 0xFF);
                                        matchType = 4;
                                    }
                                }
                            }
                        }
                        else matchType = 1;
                    }

                    if (matchType == 3)
                    {
                        destinationIndex++;
                        if (distance > 0xFF)
                        {
                            repeatCount = (int)Math.Floor((double)distance / 0xFF);
                            do { destination[destinationIndex++] = 0; } while (--repeatCount > 0);
                            distance %= 0xFF;
                        }
                        value = matchDistance;
                        destination[destinationIndex] = (byte)(distance & 0xFF);
                        matchType = 4;
                    }

                    if (matchType == 4)
                    {
                        destinationIndex++;
                        token = value << 2;
                        value >>= 6;
                        destination[destinationIndex++] = (byte)(token & 0xFF);
                        destination[destinationIndex++] = (byte)(value & 0xFF);
                        literalStartIndex = sourceIndex;
                    }

                    if (matchType == 1)
                        hashTable[hashValue] = sourceIndex++;

                } while (sourceIndex < (sourceSize - 13));

                remainingBytes = sourceSize - literalStartIndex;
            }
            else remainingBytes = sourceSize;

            if (remainingBytes > 0)
            {
                sourceIndex = sourceSize - remainingBytes;
                if (destinationIndex == 0 && remainingBytes <= 0xEE)
                    destination[destinationIndex++] = (byte)(remainingBytes + 0x11);
                else
                {
                    if (remainingBytes <= 3)
                        destination[destinationIndex - 2] |= (byte)remainingBytes;
                    else
                    {
                        if (remainingBytes > 0x12)
                        {
                            distance = remainingBytes - 0x12;
                            destination[destinationIndex++] = 0;
                            value = distance;
                            if (distance > 0xFF)
                            {
                                repeatCount = (int)Math.Floor((double)distance / 0xFF);
                                do { destination[destinationIndex++] = 0; } while (--repeatCount > 0);
                                value = distance % 0xFF;
                            }
                        }
                        else value = remainingBytes - 3;
                        destination[destinationIndex++] = (byte)(value & 0xFF);
                    }
                }
                do { destination[destinationIndex++] = source[sourceIndex++]; } while (--remainingBytes > 0);
            }

            destination[destinationIndex++] = 0x11;
            destination[destinationIndex++] = 0;
            destination[destinationIndex++] = 0;

            byte[] finalResult = new byte[destinationIndex];
            Array.Copy(destination, 0, finalResult, 0, destinationIndex);
            return finalResult;
        }

        static byte[] Decompress_Data(byte[] sourceBuffer, int decompressedSize)
        {
            int sourceSize = sourceBuffer.Length;
            byte[] destinationBuffer = sourceBuffer;
            byte[] entrada_padrao = new byte[decompressedSize];

            int state = 0, hashValue = 0, destinationIndex = 0, matchIndex = 0, token = 0;
            int value = 0, distance = 0, resultSize = 0;

            hashValue = destinationBuffer[destinationIndex];

            if (hashValue > 0x11)
            {
                token = hashValue - 0x11;
                destinationIndex++;
                if (token >= 4)
                {
                    do { entrada_padrao[destinationIndex++] = destinationBuffer[destinationIndex++]; } while (--token > 0);
                    state = -1;
                }
                else state = -2;
            }
            else state = -10;

            do
            {
                if (state == -10)
                {
                    hashValue = destinationBuffer[destinationIndex++];
                    if (!(hashValue < 0x10)) state = -3;
                    else
                    {
                        if (hashValue == 0)
                        {
                            if (destinationBuffer[destinationIndex] == 0)
                            {
                                do
                                {
                                    distance = destinationBuffer[++destinationIndex];
                                    hashValue += 0xFF;
                                } while (distance == 0);
                            }
                            distance = destinationBuffer[destinationIndex++];
                            hashValue = hashValue + distance + 0xF;
                        }

                        entrada_padrao[destinationIndex++] = destinationBuffer[destinationIndex++];
                        entrada_padrao[destinationIndex++] = destinationBuffer[destinationIndex++];
                        entrada_padrao[destinationIndex++] = destinationBuffer[destinationIndex++];
                        entrada_padrao[destinationIndex++] = destinationBuffer[destinationIndex++];
                        hashValue--;

                        if (hashValue != 0)
                        {
                            if (hashValue < 4)
                            {
                                do { entrada_padrao[destinationIndex++] = destinationBuffer[destinationIndex++]; } while (--hashValue > 0);
                            }
                            else
                            {
                                do
                                {
                                    entrada_padrao[destinationIndex++] = destinationBuffer[destinationIndex++];
                                    entrada_padrao[destinationIndex++] = destinationBuffer[destinationIndex++];
                                    entrada_padrao[destinationIndex++] = destinationBuffer[destinationIndex++];
                                    entrada_padrao[destinationIndex++] = destinationBuffer[destinationIndex++];
                                    hashValue -= 4;
                                } while (!(hashValue < 4));

                                if (hashValue > 0)
                                {
                                    do { entrada_padrao[destinationIndex++] = destinationBuffer[destinationIndex++]; } while (--hashValue > 0);
                                }
                            }
                        }
                        state = -1;
                    }
                }

                if (state == -1)
                {
                    hashValue = destinationBuffer[destinationIndex++];
                    if (hashValue < 0x10)
                    {
                        distance = destinationBuffer[destinationIndex++];
                        hashValue >>= 2;
                        distance <<= 2;
                        matchIndex = destinationIndex - hashValue;
                        matchIndex -= distance;
                        matchIndex -= 0x801;
                        distance = entrada_padrao[matchIndex++];
                        entrada_padrao[destinationIndex++] = (byte)(distance & 0xFF);
                        state = -4;
                    }
                    else state = -3;
                }

                if (state == -2)
                {
                    entrada_padrao[destinationIndex++] = destinationBuffer[destinationIndex++];
                    if (token > 1)
                    {
                        entrada_padrao[destinationIndex++] = destinationBuffer[destinationIndex++];
                        if (token > 2) entrada_padrao[destinationIndex++] = destinationBuffer[destinationIndex++];
                    }
                    hashValue = destinationBuffer[destinationIndex++];
                    state = -3;
                }

                if (state == -3)
                {
                    if (hashValue < 0x40)
                    {
                        if (hashValue < 0x20)
                        {
                            matchIndex = destinationIndex;
                            if (hashValue < 0x10)
                            {
                                distance = destinationBuffer[destinationIndex++];
                                hashValue >>= 2;
                                matchIndex -= hashValue;
                                distance <<= 2;
                                matchIndex -= distance;
                                matchIndex--;
                                state = -4;
                            }
                            else
                            {
                                distance = hashValue & 8;
                                distance <<= 0x0B;
                                matchIndex -= distance;
                                hashValue &= 7;

                                if (hashValue == 0)
                                {
                                    if (destinationBuffer[destinationIndex] == 0)
                                    {
                                        do
                                        {
                                            distance = destinationBuffer[++destinationIndex];
                                            hashValue += 0xFF;
                                        } while (distance == 0);
                                    }
                                    distance = destinationBuffer[destinationIndex++];
                                    hashValue += distance + 7;
                                }

                                distance = destinationBuffer[destinationIndex + 1];
                                distance = (distance << 8) + destinationBuffer[destinationIndex];
                                distance >>= 2;
                                matchIndex -= distance;
                                destinationIndex += 2;

                                if (matchIndex == destinationIndex)
                                {
                                    resultSize = destinationIndex;
                                    if (destinationIndex == sourceSize) resultSize = destinationIndex;
                                    break;
                                }
                                else
                                {
                                    matchIndex -= 0x4000;
                                    state = -12;
                                }
                            }
                        }
                        else
                        {
                            hashValue &= 0x1F;
                            if (hashValue == 0)
                            {
                                if (destinationBuffer[destinationIndex] == 0)
                                {
                                    do
                                    {
                                        distance = destinationBuffer[++destinationIndex];
                                        hashValue += 0xFF;
                                    } while (distance == 0);
                                }
                                distance = destinationBuffer[destinationIndex++];
                                hashValue += distance + 0x1F;
                            }
                            distance = destinationBuffer[destinationIndex + 1];
                            distance = (distance << 8) + destinationBuffer[destinationIndex];
                            distance >>= 2;
                            matchIndex = destinationIndex - distance;
                            matchIndex--;
                            destinationIndex += 2;
                            state = -12;
                        }
                    }
                    else
                    {
                        distance = hashValue >> 2;
                        distance &= 7;
                        matchIndex = destinationIndex - distance;
                        distance = destinationBuffer[destinationIndex++];
                        distance <<= 3;
                        matchIndex -= distance;
                        matchIndex--;
                        hashValue >>= 5;
                        hashValue--;
                        state = -13;
                    }
                }

                if (state == -12)
                {
                    if (hashValue < 6) state = -13;
                    else
                    {
                        distance = destinationIndex - matchIndex;
                        if (distance < 4) state = -13;
                        else
                        {
                            entrada_padrao[destinationIndex++] = entrada_padrao[matchIndex++];
                            entrada_padrao[destinationIndex++] = entrada_padrao[matchIndex++];
                            entrada_padrao[destinationIndex++] = entrada_padrao[matchIndex++];
                            entrada_padrao[destinationIndex++] = entrada_padrao[matchIndex++];
                            hashValue -= 2;

                            do
                            {
                                entrada_padrao[destinationIndex++] = entrada_padrao[matchIndex++];
                                entrada_padrao[destinationIndex++] = entrada_padrao[matchIndex++];
                                entrada_padrao[destinationIndex++] = entrada_padrao[matchIndex++];
                                entrada_padrao[destinationIndex++] = entrada_padrao[matchIndex++];
                                hashValue -= 4;
                            } while (!(hashValue < 4));

                            if (hashValue > 0)
                            {
                                do { entrada_padrao[destinationIndex++] = entrada_padrao[matchIndex++]; } while (--hashValue > 0);
                            }
                            state = -5;
                        }
                    }
                }

                if (state == -13)
                {
                    entrada_padrao[destinationIndex++] = entrada_padrao[matchIndex++];
                    entrada_padrao[destinationIndex++] = entrada_padrao[matchIndex++];
                    do { entrada_padrao[destinationIndex++] = entrada_padrao[matchIndex++]; } while (--hashValue > 0);
                    state = -5;
                }

                if (state == -4)
                {
                    entrada_padrao[destinationIndex++] = entrada_padrao[matchIndex];
                    entrada_padrao[destinationIndex++] = entrada_padrao[matchIndex + 1];
                    state = -5;
                }

                if (state == -5)
                {
                    token = destinationBuffer[destinationIndex - 2];
                    token &= 3;
                    value = token;
                    if (token == 0) state = -10;
                    else state = -2;
                }

            } while (destinationIndex <= sourceSize);

            if (destinationIndex != sourceSize)
                resultSize = destinationIndex;

            byte[] finalResult = new byte[resultSize];
            Array.Copy(entrada_padrao, 0, finalResult, 0, resultSize);
            return finalResult;
        }
    }
}