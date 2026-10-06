using Pangya_GameServer.Session;
using PangyaAPI.Network;
using PangyaAPI.Network.Core;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;

namespace Pangya_GameServer.Handles
{
    public class Handle_PLAYER_CREATE_REALMYRROM : HandleBase<Player, Packet_EXAMPLE>
    {
        public override async Task Handle()
        {
            // Resposta padrão do My Room Info
            var p = new Packet(0x12B);

            try
            { 
                uint from_uid = Packet.ReadUInt32();
                uint to_uid = Packet.ReadUInt32();
                 
                bool isSelf = (from_uid == to_uid);
                bool canEnter = (Player.Inventory.MyRoomConfig.allow_enter == 1);

                if (isSelf && canEnter)
                {
                    // Status 1: Sucesso ao carregar dados do Quarto
                    p.WriteUInt32(1);
                    p.WriteUInt32(to_uid); 
                    p.WriteBytes(Player.Inventory.MyRoomConfig.ToArray());
                }
                else
                {
                    // Status 0: Falha ou Quarto Privado
                    p.WriteUInt32(0);
                    p.WriteUInt32(to_uid);
                }

                // 3. Envio direto via Session
                Player.Send(p);
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage(
                    $"[Handle_PLAYER_MY_ROOM_INFO_REQUEST][Error] {e.getFullMessageError()}",
                    type_msg.CL_FILE_LOG_AND_CONSOLE)
                );

                // Em caso de erro crítico, envia o pacote de falha para o cliente não travar
                var errorPkt = new Packet(0x12B);
                errorPkt.WriteUInt32(0);
                errorPkt.WriteUInt32(0); // UID zerado
                Player.Send(errorPkt);
            }

        await Task.CompletedTask;
        }
    }
}