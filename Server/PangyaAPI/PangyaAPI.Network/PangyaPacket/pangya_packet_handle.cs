using PangyaAPI.Network.Cryptor;
using PangyaAPI.Network.PangyaSession;
using PangyaAPI.Network.PangyaUnit;
using PangyaAPI.Utilities;
using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace PangyaAPI.Network.PangyaPacket
{
    public abstract class pangya_packet_handle : IUnitAuthServer
    {

        public PangyaSyncTimerManager m_timer_mgr = new PangyaSyncTimerManager();

        public PacketBuffer ToServerBuffer = new PacketBuffer();
        public ToClientBuffer ToClientBuffer = new ToClientBuffer();

        //decript packet client->server
        protected abstract void dispach_packet_same_thread(Session _session, packet _packet);
        //decript packet server->client
        public abstract void dispach_packet_sv_same_thread(Session _session, packet _packet);
        //implement desconnect
        public abstract bool DisconnectSession(Session _session);


        protected async Task<bool> recv_client_new(Session _session)
        {
            try
            {
                // 1. Verificação de segurança da sessão
                if (_session?.m_client == null || !_session.m_client.Connected)
                    return false;

                // 2. Leitura Assíncrona
                // Nota: Se o seu método Read() ainda for síncrono, o 'await Task.Run' 
                // ajuda a não travar a thread de IO principal.
                var result = await _session.m_client.ReadAsync();

                if (result.check)
                {
                    // 3. Validação do Header do Pacote Pangya
                    if (_session.isCreated() && result.len > 0)
                    {
                        _session.m_client.ReceiveTimeout = 0;

                        // 4. Descriptografia e extração da lista de pacotes
                        var decryptedPackets = ToServerBuffer.getPackets(result._buffer, _session.m_key);

                        if (decryptedPackets != null && decryptedPackets.Count > 0)
                        {
                            foreach (var _packet in decryptedPackets)
                            {
                                // Processa o pacote (mantendo a ordem na thread da sessão)
                                dispach_packet_same_thread(_session, _packet);
                            }

                            // 5. Limpeza do buffer residual
                            ToClientBuffer = new ToClientBuffer();
                            return true;
                        }
                        else
                        {
                            // Pacote lido mas vazio ou incompleto (aguardando mais dados)
                            ToClientBuffer = new ToClientBuffer();
                            return false;
                        }
                    }
                    else
                    {
                        Debug.WriteLine($"[recv_client_new] Falha na integridade: {result.len} bytes.");
                        return false;
                    }
                }
                else
                {
                    // Se o check falhar ou len <= 0, o socket encerrou
                    DisconnectSession(_session);
                    return false;
                }
            }
            catch (Exception ex) when (ex is SocketException || ex is ObjectDisposedException)
            {
                Debug.WriteLine($"[recv_client_new] Conexão encerrada: {ex.Message}");
                DisconnectSession(_session);
                return false;
            }
            catch (Exception e)
            {
                Debug.WriteLine($"[recv_client_new] Erro Crítico: {e.Message}");
                DisconnectSession(_session);
                return false;
            }
        }

        protected async Task<bool> recv_server_new(Session _session)
        {
            try
            {
                // 1. Verificação de segurança da sessão
                if (_session?.m_client == null || !_session.m_client.Connected)
                    return false;

                // 2. Leitura Assíncrona
                // Nota: Se o seu método Read() ainda for síncrono, o 'await Task.Run' 
                // ajuda a não travar a thread de IO principal.
                var result = await _session.m_client.ReadAsync();

                if (result.check)
                {
                    // 3. Validação do Header do Pacote Pangya
                    if (_session.isCreated() && ToServerBuffer.check_packet(result._buffer))
                    {
                        _session.m_client.ReceiveTimeout = 0;

                        // 4. Descriptografia e extração da lista de pacotes
                        var decryptedPackets = ToServerBuffer.getPackets(result._buffer, _session.m_key);

                        if (decryptedPackets != null && decryptedPackets.Count > 0)
                        {
                            foreach (var _packet in decryptedPackets)
                            {
                                // Processa o pacote (mantendo a ordem na thread da sessão)
                                dispach_packet_same_thread(_session, _packet);
                            }

                            // 5. Limpeza do buffer residual
                            ToServerBuffer.clear();
                            return true;
                        }
                        else
                        {
                            // Pacote lido mas vazio ou incompleto (aguardando mais dados)
                            ToServerBuffer.clear();
                            return false;
                        }
                    }
                    else
                    {
                        Debug.WriteLine($"[recv_server_new] Falha na integridade: {result.len} bytes.");
                        return false;
                    }
                }
                else
                {
                    // Se o check falhar ou len <= 0, o socket encerrou
                    DisconnectSession(_session);
                    return false;
                }
            }
            catch (Exception ex) when (ex is SocketException || ex is ObjectDisposedException)
            {
                Debug.WriteLine($"[recv_server_new] Conexão encerrada: {ex.Message}");
                DisconnectSession(_session);
                return false;
            }
            catch (Exception e)
            {
                Debug.WriteLine($"[recv_server_new] Erro Crítico: {e.Message}");
                DisconnectSession(_session);
                return false;
            }
        }

        protected async Task<bool> recv_client_new(Session session, bool raw)
        {
            try
            {
                // 1. Verificação de Sanidade
                if (session?.m_client == null || !session.m_client.Connected)
                    return false;

                // 2. Leitura Assíncrona (Aproveitando o async/await)
                // Se o seu método Read() for síncrono, considere criar um ReadAsync()
                var result = await session.m_client.ReadAsync();

                if (result.check)
                {
                    if (session.isCreated() && result.len > 0) // result.len 0 geralmente é um "Keep-Alive" vazio
                    {
                        if (raw)
                        {
                            // Usa o buffer diretamente
                            dispach_packet_same_thread(session, new packet(result._buffer, raw));
                        }
                        else
                        {
                            // Descriptografia Pangya (m_key)
                            var decryptedPackets = ToServerBuffer.getPackets(result._buffer, session.m_key);

                            if (decryptedPackets != null)
                            {
                                foreach (var p in decryptedPackets)
                                    dispach_packet_same_thread(session, p);
                            }
                        }
                        return true;
                    }

                    // Se len for 0 e check for true, pode ser apenas um sinal de socket vazio, não necessariamente erro
                    return true;
                }
                else
                {
                    // Se check for false, algo falhou na leitura do socket
                    Debug.WriteLine($"[recv_new] Falha no check: {result.len}");
                    DisconnectSession(session);
                    return false;
                }
            }
            catch (Exception e) when (e is SocketException || e is ObjectDisposedException)
            {
                Debug.WriteLine($"[recv_new] Conexão perdida: {e.Message}");
                DisconnectSession(session);
                return false;
            }
            catch (Exception e)
            {
                Debug.WriteLine($"[recv_new] Erro Crítico: {e.Message}");
                DisconnectSession(session);
                return false;
            }
        }
         
    }
}