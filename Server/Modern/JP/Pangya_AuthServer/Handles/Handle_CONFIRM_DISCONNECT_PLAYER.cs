using Pangya_AuthServer.Manager;
using Pangya_AuthServer.Server;
using Pangya_AuthServer.Session;
using PangyaAPI.Network;
using PangyaAPI.Network.Core;
using System;
using System.Threading.Tasks;

namespace Pangya_AuthServer.Handles
{
    public class Handle_CONFIRM_DISCONNECT_PLAYER : HandleBase<Player, Packet_EXAMPLE>
    {
        public override async Task Handle()
        {
            try
            {
                // 1. Leitura dos dados do pacote
                uint playerUid = Packet.ReadUInt32();
                uint targetServerUid = Packet.ReadUInt32();

                // 2. Verifica se o destinatário da confirmação não é o próprio Auth Server
                // Substitua 'm_si.UID' pela sua constante de UID do servidor atual
                if (targetServerUid == AuthServer.Instance.m_si.UID)
                {
                    Console.WriteLine($"[Auth] Confirmação de Disconnect: Player {playerUid} desconectado com sucesso (Solicitado pelo Auth).");
                    return;
                }

                // 3. Busca o servidor que deve receber a confirmação
                var targetServer = AuthServer.Instance.FindPlayer(targetServerUid);

                if (targetServer != null)
                {
                    Console.WriteLine($"[Relay 0x07] Confirmando disconnect do Player {playerUid} para o Server {targetServerUid}.");

                    // 4. Monta o pacote de repasse (OpCode 0x07)
                    using (var response = new Packet(0x07))
                    {
                        response.WriteUInt32(playerUid);

                        // 5. Envia o pacote
                        targetServer.SendAuth(response);
                    }
                }
                else
                {
                    Console.WriteLine($"[Warning] Server {targetServerUid} não encontrado para receber confirmação de disconnect do Player {playerUid}.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Error] Handle_CONFIRM_DISCONNECT_PLAYER: {ex.Message}");
            }
        }
    }
}