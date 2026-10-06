using Pangya_AuthServer.Manager;
using Pangya_AuthServer.Server;
using Pangya_AuthServer.Session;
using PangyaAPI.Network;
using PangyaAPI.Network.Core;
using System;
using System.Threading.Tasks;

namespace Pangya_AuthServer.Handles
{
    public class Handle_SEND_REPLY_TO_OTHER_SERVER : HandleBase<Player, Packet_EXAMPLE>
    { 

        public override async Task Handle()
        {
            try
            {
                // 1. Ler o cabeçalho do comando (Substituindo o Marshal antigo)
                // O Pangya costuma enviar: [TargetUID (4 bytes)][CommandID (2 bytes)]
                uint targetServerUid = Packet.ReadUInt32();
                ushort commandId = Packet.ReadUInt16();

                // 2. Calcular o tamanho do buffer restante
                // O que sobrar no pacote é o dado que deve ser repassado
                int remainingDataSize = Packet.Size;//pega o restante
                byte[] commandBuff = null;

                if (remainingDataSize > 0)
                {
                    commandBuff = Packet.ReadBytes(remainingDataSize);
                }

                // Log de operação
                Console.WriteLine($"[Relay] Server {Player.UserInfo.UID} enviando Resposta ID {commandId} para o Server {targetServerUid}");

                // 3. Localizar o servidor de destino
                // Usando o seu player_manager que agora deve estar acessível
                var targetSession = AuthServer.Instance.FindPlayer(targetServerUid);

                if (targetSession == null)
                {
                    Console.WriteLine($"[Relay Error] Destino {targetServerUid} não encontrado.");
                    return;
                }

                // 4. Montar o pacote de repasse (OpCode 0x0E)
                using (var response = new Packet(0x0E))
                {
                    // Quem enviou originalmente
                    response.WriteUInt32(Player.UserInfo.UID);

                    // ID do Comando
                    response.WriteUInt16(commandId);

                    // Dados do Comando
                    if (commandBuff != null && commandBuff.Length > 0)
                    {
                        response.WriteBytes(commandBuff);
                    }
                    else
                    {
                        // Se não houver dados, o Pangya costuma esperar um separador de 2 bytes (00 00)
                        response.WriteUInt16(0);
                    }

                    // 5. Enviar para o servidor destino (Plain pq é comunicação Server-to-Server)
                    targetSession.SendAuth(response);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Relay Critical Error] {ex.Message}");
            }
        }
    }
}