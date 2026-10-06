using Pangya_MessengerServer.Session;
using PangyaAPI.Network;
using PangyaAPI.Network.Core;
using PangyaAPI.Network.Handle;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;

namespace Pangya_MessengerServer.Handles
{
    public class Handle_DUMMY : HandleBase<Player, Packet_EXAMPLE>
    {
        public override async Task Handle()
        {
            int opcode = Packet.Type;
            uint uid = Player.UserInfo?.UID ?? 0;

            // Log detalhado para análise posterior no console/arquivo
            _smp.LogManager.Instance.push(new AppMessage(
                $"[Handle_DUMMY][Log] Pacote 0x{opcode:X2} recebido de Player[UID={uid}]. " +
                $"Tamanho: {Packet.GetBytesReader().HexDump()} bytes. Lógica de resposta ainda não implementada.",
                type_msg.CL_FILE_LOG_AND_CONSOLE));

            // Opcional: Se você quiser ver o conteúdo bruto (Hex) no log, 
            // pode implementar um helper Packet.ToHexString() aqui.

            await Task.CompletedTask;
        }
    }
}