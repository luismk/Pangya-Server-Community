using PangyaAPI.Network.Core;
using PangyaAPI.Utilities.Log;
namespace PangyaAPI.Network.Handle
{
    public class PacketDispatcher<TSession, TId>
        where TSession : class, IAppSession
        where TId : struct, Enum
    {
        // Guarda as funções assíncronas dos pacotes registrados
        private readonly Dictionary<TId, Func<TSession, Packet, Task>> _handlers = new();

        public Func<TSession, Packet, Task> DefaultHandler { get; set; }

        /// <summary>
        /// Registra um pacote mapeando o ID (Enum) diretamente para as classes Handle_XXXX.
        /// </summary>

        public void Register<TPacketResult>(TId id, HandleBase<TSession, TPacketResult> handler)
            where TPacketResult : PacketResult, new()
        {
            try
            {
                _handlers[id] = async (session, rawPacket) =>
                {
                    await handler.ExecuteAsync(session, rawPacket);
                };
            }
            catch (Exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage(
                     $"[Dispatcher][Error] Falha ao registrar {id}: {e.Message}",
                      type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void RegisterDefault<TPacketResult>(HandleBase<TSession, TPacketResult> handler) where TPacketResult : PacketResult, new()
        {
            DefaultHandler = async (session, rawPacket) =>
            {
                await handler.ExecuteAsync(session, rawPacket);
            };
        }

        /// <summary>
        /// Despacha o pacote de forma assíncrona para o Handler correto.
        /// </summary>
        public async Task Dispatch(TSession session, TId id, Packet packet)
        {
            // Busca a função de execução usando o ID (Enum) fornecido
            if (_handlers.TryGetValue(id, out var launchHandler))
            {
                try
                {
                    await launchHandler(session, packet);
                }
                catch (Exception ex)
                {
                    _smp.LogManager.Instance.push(new AppMessage(
                         $"[Dispatcher][CRITICAL] Erro no Handler {id} (0x{Convert.ToInt16(id):X2}): {ex.Message}",
                          type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
            }
            else
            {
                // Se o pacote não foi registrado, mas temos um DefaultHandler definido
                if (DefaultHandler != null)
                {
                    try
                    {
                        await DefaultHandler(session, packet);
                    }
                    catch (Exception ex)
                    {
                        _smp.LogManager.Instance.push(new AppMessage(
                             $"[Dispatcher][CRITICAL] Erro no DefaultHandler para o pacote {id}: {ex.Message}",
                              type_msg.CL_FILE_LOG_AND_CONSOLE));
                    }
                }
                else
                {
                    _smp.LogManager.Instance.push(new AppMessage(
                         $"[Dispatcher][Warning] PACKET[ID: {id} OPCODE: 0x{Convert.ToInt16(id):X2}].",
                          type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
            }
        }
    }
}