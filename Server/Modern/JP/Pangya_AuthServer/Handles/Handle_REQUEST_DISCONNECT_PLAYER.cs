using Pangya_AuthServer.Manager;
using Pangya_AuthServer.Server;
using Pangya_AuthServer.Session;
using PangyaAPI.Network;
using PangyaAPI.Network.Core;
using System;
using System.Threading.Tasks;

namespace Pangya_AuthServer.Handles
{
    public class Handle_REQUEST_DISCONNECT_PLAYER : HandleBase<Player, Packet_EXAMPLE>
    {
        public override async Task Handle()
        {
            uint playerUid = 0;
            uint serverUid = 0;

            try
            {
                // 1. Leitura dos dados: Quem deve ser kickado e em qual server ele está
                playerUid = Packet.ReadUInt32();
                serverUid = Packet.ReadUInt32();

                // 2. Busca a sessão do servidor onde o player está logado
                var targetServer = AuthServer.Instance.FindPlayer(serverUid);

                if (targetServer != null)
                {
                    Console.WriteLine($"[Kick] Server {Player.UserInfo.UID} solicitou a expulsão do Player {playerUid} no Server {serverUid}");

                    // 3. Envia o comando de expulsão (OpCode 0x06)
                    using (var p = new Packet(0x06))
                    {
                        p.WriteUInt32(playerUid);
                        p.WriteUInt32(Player.UserInfo.UID); // UID do servidor que solicitou o kick
                        p.WriteByte(0);                   // Force ServerFlag (0 = Normal, 1 = Force)

                        targetServer.SendAuth(p);
                    }
                }
                else
                {
                    // 4. Se o servidor alvo não existe mais, avisamos o solicitante imediatamente
                    // para ele liberar o estado do jogador.
                    Console.WriteLine($"[Kick Warning] Server {serverUid} não encontrado. Confirmando disconnect fantasma para Player {playerUid}.");
                    await SendInstantConfirm(playerUid);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Error] Handle_REQUEST_DISCONNECT_PLAYER: {ex.Message}");
                // Em caso de erro interno, enviamos a confirmação (0x07) para não travar o fluxo
                await SendInstantConfirm(playerUid);
            }
        }

        private async Task SendInstantConfirm(uint playerUid)
        {
            using (var p = new Packet(0x07))
            {
                p.WriteUInt32(playerUid);
                Player.SendAuth(p);
            }
        }
    }
}