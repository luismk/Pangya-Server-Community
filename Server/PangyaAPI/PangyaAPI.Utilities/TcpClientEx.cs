using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
namespace PangyaAPI.Utilities
{
    public static class TcpClientEx
    {
        public static bool IsClientConnected(this TcpClient client)
        {
            try
            {
                var socket = client.Client;

                // Se o socket diz que não está conectado, nem perdemos tempo
                if (socket == null) return false;

                // Se o socket diz que não está conectado, nem perdemos tempo
                if (!socket.Connected) return false;

                // Poll retorna true se:
                // 1. Há dados esperando para serem lidos (conexão OK)
                // 2. A conexão foi fechada/resetada (conexão RUIM)
                if (socket.Poll(0, SelectMode.SelectRead))
                {
                    byte[] buffer = new byte[1];
                    // Se tentarmos ler (Peek) e retornar 0, a conexão caiu definitivamente
                    if (socket.Receive(buffer, SocketFlags.Peek) == 0)
                    {
                        return false;
                    }
                }
                return true;
            }
            catch (SocketException)
            {
                return false;
            }
        }
        public static void SafeClose(this TcpClient client)
        {
            if (client == null)
                return;

            try
            {
                var socket = client.Client;

                if (socket != null && socket.Connected)
                {
                    try
                    {
                        socket.Shutdown(SocketShutdown.Both);
                    }
                    catch
                    {
                        // normal: socket já morto
                    }
                }
            }
            catch { }

            try
            {
                client.Close(); // Fecha stream + socket
            }
            catch { }
        }


         

        public static bool Send(this TcpClient client, byte[] buffer, int len = 0)
        {
            try
            {
                return client.GetState() != TcpState.Unknown && client.GetStream().Send(buffer, 0, len);
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public static bool Send(this NetworkStream stream, byte[] buffer, int offset, int len)
        {
            try
            {
                if (stream.CanWrite)
                {
                    stream.Write(buffer, offset, len);
                    return true;
                }

                return false;
            }
            catch (IOException ioEx)
            {
                Debug.WriteLine($"[Send] Erro de leitura: {ioEx.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Send] Erro inesperado: {ex.Message}");
                return false;
            }
        }

        public static TcpState GetState(this TcpClient tcpClient)
        {
            var foo = IPGlobalProperties.GetIPGlobalProperties()
              .GetActiveTcpConnections()
              .SingleOrDefault(x => x.LocalEndPoint.Equals(tcpClient.Client.LocalEndPoint)
                                 && x.RemoteEndPoint.Equals(tcpClient.Client.RemoteEndPoint)
              );

            return foo != null ? foo.State : TcpState.Unknown;
        }

        public static bool Shutdown(this TcpClient _sock, SocketShutdown how)
        {
            Thread.Sleep(3000);
            Shutdown(_sock.Client, how); return true; }

        public static bool Shutdown(this Socket _sock, SocketShutdown how)
        { _sock.Shutdown(how); return true; }
        public static bool SocketConnected(Socket s)
        {
            bool part1 = s.Poll(1000, SelectMode.SelectRead);
            bool part2 = (s.Available == 0);
            if (part1 & part2)
            {//connection is closed
                return false;
            }
            return true;
        }

        public static async Task<(bool check, byte[] _buffer, int len)> ReadAsync(this TcpClient client)
        {
            if (client != null && client.Connected)
                return await client.Client.ReadAsync();

            return (false, Array.Empty<byte>(), 0);
        }

        public static async Task<(bool Success, byte[] Buffer, int Length)> ReadAsync(this Socket socket)
        {
            if (socket == null || !socket.Connected)
                return (false, Array.Empty<byte>(), 0);

            // 8 KB é um bom tamanho para Pangya (pacotes raramente passam disso)
            byte[] buffer = new byte[8192];

            try
            {
                // Usa a versão nativa assíncrona do .NET que não bloqueia threads
                int bytesRead = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), SocketFlags.None);

                if (bytesRead == 0)
                {
                    // Conexão encerrada graciosamente pelo outro lado
                    return (false, Array.Empty<byte>(), 0);
                }

                // Criamos o array de retorno apenas com o tamanho lido
                byte[] result = new byte[bytesRead];
                Buffer.BlockCopy(buffer, 0, result, 0, bytesRead);

                return (true, result, bytesRead);
            }
            catch (ObjectDisposedException)
            {
                // Ocorre se o socket for fechado por outra thread enquanto lia
                Debug.WriteLine("[ReadAsync] Socket foi descartado.");
                return (false, Array.Empty<byte>(), 0);
            }
            catch (SocketException ex)
            {
                Debug.WriteLine($"[ReadAsync] Erro de rede: {ex.SocketErrorCode}");
                return (false, Array.Empty<byte>(), 0);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ReadAsync] Erro: {ex.Message}");
                return (false, Array.Empty<byte>(), 0);
            }
        }

        public static (bool Success, byte[] Buffer, int Length) Read(this NetworkStream stream)
        {
            if (stream == null || !stream.CanRead)
            {
                Debug.WriteLine("[Read] Stream nula ou não pode ser lida.");
                return (false, Array.Empty<byte>(), 0);
            }

            byte[] buffer = new byte[8192]; // 8 KB é suficiente na maioria dos casos
            try
            {
                int bytesRead = stream.Read(buffer, 0, buffer.Length);

                if (bytesRead == 0)
                {
                    // Cliente fechou a conexão (EOF)
                    Debug.WriteLine("[Read] Cliente desconectou durante a leitura.");
                    return (false, Array.Empty<byte>(), 0);
                }

                byte[] result = new byte[bytesRead];
                Array.Copy(buffer, result, bytesRead);

                return (true, result, bytesRead);
            }
            catch (IOException ioEx) when (ioEx.InnerException is SocketException sockEx)
            {
                Debug.WriteLine($"[Read] Socket encerrado pelo cliente: {sockEx.SocketErrorCode} - {sockEx.Message}");
                return (false, Array.Empty<byte>(), 0);
            }
            catch (IOException ioEx)
            {
                Debug.WriteLine($"[Read] Erro de leitura de stream: {ioEx.Message}");
                return (false, Array.Empty<byte>(), 0);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Read] Erro inesperado: {ex.Message}");
                return (false, Array.Empty<byte>(), 0);
            }
        }
    }
}
