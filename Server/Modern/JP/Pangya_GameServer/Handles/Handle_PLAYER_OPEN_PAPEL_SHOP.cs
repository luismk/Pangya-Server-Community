using Pangya_GameServer.Session;
using PangyaAPI.Network;
using PangyaAPI.Network.Core;
using System;
using System.Threading.Tasks;

namespace Pangya_GameServer.Handles
{
    public class Handle_PLAYER_OPEN_PAPEL_SHOP : HandleBase<Player, Packet_EXAMPLE>
    {
        public override async Task Handle()
        {
            Packet p = new Packet(0x10B);

            try
            { 
                p.WriteUInt32(0);
                p.WriteInt64(Player.UserInfo.Member.PapelShop.LimitCount); 
                Player.Send(p);
            }
            catch (Exception e)
            {
                // Log de erro simplificado
                Console.WriteLine($"[Handle_PLAYER_OPEN_PAPEL_SHOP] Erro: {e.Message}");

                p.init_plain(0x10B);
                p.WriteInt64(-1);
                p.WriteUInt32(0x5800100); // Erro padrão do sistema

                Player.Send(p);
            }

        await Task.CompletedTask;
        }
    }
}