using PangyaAPI.Network.Core;
using PangyaAPI.Utilities.Log;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace PangyaAPI.Network.Service
{
    public class AcceptLoop : IAppServerAccept
    {
        private readonly TcpListener _listener;
        private readonly Func<TcpClient, Task> _onClient;

        private CancellationTokenSource _internalCts = new();
        private readonly SemaphoreSlim _semaphore = new(500); // max 500 simultâneos
        public int AvailableSlots => _semaphore.CurrentCount;
        public AcceptLoop(TcpListener listener, Func<TcpClient, Task> onClient, int maxConcurrent = 500)
        {
            _listener = listener;
            _onClient = onClient;
            _semaphore = new SemaphoreSlim(maxConcurrent, maxConcurrent);
        }

        public async Task StartAsync(CancellationToken token)
        {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(token, _internalCts.Token);

            while (!linked.Token.IsCancellationRequested)
            {
                try
                {
                    var client = await _listener.AcceptTcpClientAsync(linked.Token);
                    // Aguarda slot disponível antes de criar a Task
                    await _semaphore.WaitAsync(linked.Token);
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await _onClient(client);
                        }
                        catch (Exception ex)
                        {
                            _smp.LogManager.Instance.push(new AppMessage($"[AcceptLoop::StartAsync][Error] Exception[Message: {ex.Message}, StackTrace: {ex.StackTrace}]", type_msg.CL_FILE_LOG_AND_CONSOLE));
                        }
                        finally
                        {
                            // Libera o slot ANTES de fechar o socket
                            // para que o próximo accept possa ser processado imediatamente
                            _semaphore.Release();

                            try
                            {
                                if (client.Connected)
                                    client.Close();
                            }
                            catch { }
                        }
                    }, linked.Token);
                }
                catch (OperationCanceledException e)
                {
                    _smp.LogManager.Instance.push(new AppMessage($"[AcceptLoop::StartAsync][Error] OperationCanceledException[Message: {e.Message}, StackTrace: {e.StackTrace}]", type_msg.CL_FILE_LOG_AND_CONSOLE));
                    break;
                }
                catch (Exception ex)
                {
                    _smp.LogManager.Instance.push(new AppMessage($"[AcceptLoop::StartAsync][Error] Exception[Message: {ex.Message}, StackTrace: {ex.StackTrace}]", type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
            }
        }

        public void Stop()
        {
            _internalCts.Cancel();
        }
    }
}
