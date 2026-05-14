using System;

namespace PangyaAPI.Network.Cryptor
{

    public static class Cipher
    {

        public static byte[] EncryptClient(byte[] source, byte key, byte salt)
        {
            if (key >= 0x10) throw new ArgumentOutOfRangeException(nameof(key), $"Key too large ({key} >= 0x10)");

            var oracleIndex = (key << 8) + salt;
            var buffer = new byte[source.Length + 5];
            var pLen = buffer.Length - 4;

            buffer[0] = salt;
            buffer[1] = (byte)((pLen >> 0) & 0xFF);
            buffer[2] = (byte)((pLen >> 8) & 0xFF);
            buffer[4] = CryptoOracle.PUBLIC_KEY_TABLE[oracleIndex];

            Array.Copy(source, 0, buffer, 5, source.Length);

            for (var i = buffer.Length - 1; i >= 8; i--) buffer[i] ^= buffer[i - 4];

            buffer[4] ^= CryptoOracle.PRIVATE_KEY_TABLE[oracleIndex];
            return buffer;
        }

        /// <summary>
        ///     Decrypts data from client-side packets (sent from clients to servers.)
        /// </summary>
        /// <param name="source">The encrypted packet data.</param>
        /// <param name="key">Key to decrypt with.</param>
        /// <returns>The decrypted packet data.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the key is equal or superior to 0x10</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the source's length is inferior to 5</exception>
        public static byte[] DecryptClient(byte[] source, byte key)
        {
            if (key >= 0x10)
            {
                throw new ArgumentOutOfRangeException(nameof(key),
                    $"[{nameof(Cipher)}][{nameof(DecryptClient)}] The cryptography key is too big, the key generation should be changed.");
            }

            if (source.Length < 5)
            {
                throw new ArgumentOutOfRangeException(nameof(key),
                    $"[{nameof(Cipher)}][{nameof(DecryptClient)}] The packet is too small to get decrypted ({source.Length.ToString()} < 5)");
            }

            byte[] buffer = (byte[])source.Clone();

            buffer[4] = CryptoOracle.PRIVATE_KEY_TABLE[(key << 8) + source[0]];

            for (int i = 8; i < buffer.Length; i++)
            {
                buffer[i] ^= buffer[i - 4];
            }

            byte[] output = new byte[buffer.Length - 5];

            Array.Copy(buffer, 5, output, 0, buffer.Length - 5);

            return output;
        }

        /// <summary>
        ///     Decrypts data from server-side packets (sent from servers to clients.)
        /// </summary>
        /// <param name="source">The encrypted packet data.</param>
        /// <param name="serverCryptKey">Key to decrypt with.</param>
        /// <returns>The decrypted packet data.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     Thrown if the key is invalid or the packet data is too short.
        /// </exception>
        public static byte[] DecryptServer(byte[] source, byte serverCryptKey)
        {
            var size = source.Length;


            if (serverCryptKey >= 0x10) throw new ArgumentOutOfRangeException(nameof(serverCryptKey), $"Key too large ({serverCryptKey} >= 0x10)");

            if (source.Length < 8)
            {
                return source;
            }
            byte oracleByte = CryptoOracle.PRIVATE_KEY_TABLE[(serverCryptKey << 8) + source[0]];
            byte[] buffer = (byte[])source.Clone();

            buffer[7] ^= oracleByte;

            for (int i = 10; i < source.Length; i++) buffer[i] ^= buffer[i - 4];

            byte[] compressedData = new byte[source.Length - 8];
            Array.Copy(buffer, 8, compressedData, 0, source.Length - 8);
            try
            {
                return MiniLzo.Decompress(compressedData);
            }
            catch (Exception)
            {
                return source;
            }
        }

        public static byte[] _ServerEncrypt(this byte[] source, byte key, byte salt)
        {
            if (key >= 0x10)
                throw new ArgumentOutOfRangeException(nameof(key), $"Key too large ({key} >= 0x10)");

            var oracleIndex = (key << 8) + salt;

            // Sem compressão: dados brutos são usados diretamente
            var buffer = new byte[source.Length + 8];
            var pLen = buffer.Length - 3;

            var u = source.Length;
            var x = (u + u / 255) & 0xFF;
            var v = (u - x) / 255;
            var y = (v + v / 255) & 0xFF;
            var w = (v - y) / 255;
            var z = (w + w / 255) & 0xFF;

            // Cabeçalho do pacote
            buffer[0] = salt;
            buffer[1] = (byte)((pLen >> 0) & 0xFF);
            buffer[2] = (byte)((pLen >> 8) & 0xFF);
            buffer[3] = (byte)(CryptoOracle.PUBLIC_KEY_TABLE[oracleIndex] ^ CryptoOracle.PRIVATE_KEY_TABLE[oracleIndex]);
            buffer[5] = (byte)z;
            buffer[6] = (byte)y;
            buffer[7] = (byte)x;

            // Copia os dados originais (não comprimidos)
            Array.Copy(source, 0, buffer, 8, source.Length);

            // Criptografia com XOR em reverso
            for (var i = buffer.Length - 1; i >= 10; i--)
                buffer[i] ^= buffer[i - 4];

            buffer[7] ^= CryptoOracle.PRIVATE_KEY_TABLE[oracleIndex];

            return buffer;
        }

        public static byte[] ServerEncrypt(this byte[] source, byte key, byte salt)
        {
            if (key >= 0x10) throw new ArgumentOutOfRangeException(nameof(key), $"Key too large ({key} >= 0x10)");

            var oracleIndex = (key << 8) + salt;
            var compressedData = MiniLzo.Compress(source);
            var buffer = new byte[compressedData.Length + 8];
            var pLen = buffer.Length - 3;

            var u = source.Length;
            var x = (u + u / 255) & 0xff;
            var v = (u - x) / 255;
            var y = (v + v / 255) & 0xff;
            var w = (v - y) / 255;
            var z = (w + w / 255) & 0xff;
            //packet header
            buffer[0] = salt;
            buffer[1] = (byte)((pLen >> 0) & 0xFF);
            //low key
            buffer[2] = (byte)((pLen >> 8) & 0xFF);


            buffer[3] = (byte)(CryptoOracle.PUBLIC_KEY_TABLE[oracleIndex] ^ CryptoOracle.PRIVATE_KEY_TABLE[oracleIndex]);
            //outros
            buffer[5] = (byte)z;
            buffer[6] = (byte)y;
            buffer[7] = (byte)x;

            Array.Copy(compressedData, 0, buffer, 8, compressedData.Length);

            for (var i = buffer.Length - 1; i >= 10; i--) buffer[i] ^= buffer[i - 4];

            buffer[7] ^= CryptoOracle.PRIVATE_KEY_TABLE[oracleIndex];

            return buffer;
        }
    }
}
