using Pangya_RankingServer.Manager;
using Pangya_RankingServer.Session;
using PangyaAPI.Network;
using PangyaAPI.Network.Core;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;

namespace Pangya_RankingServer.Handles
{
    public class Handle_REQUEST_PLAYER_INFO : HandleBase<Player, Packet_EXAMPLE>
    {
        public override async Task Handle()
        {
            try
            {
                uint uid = Packet.ReadUInt32();

                string id = Packet.ReadString();

                byte active = Packet.ReadByte();

                // Log de monitoramento com o Name da classe
                 
                _smp.LogManager.Instance.push(new AppMessage($"[{nameof(Handle_REQUEST_PLAYER_INFO)}][Log] PLAYER[UID: {uid}, ID: {id}] REQUEST INFO.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                // 1. Validação de Segurança Básica
                if (uid == 0)
                {
                    throw new Exception($"[{nameof(Handle_REQUEST_PLAYER_INFO)}] [PlayerInfo Error] Tentativa de request com UID zero.");
                }

                sRankRegistryManager.Instance.sendPlayerFullInfo(Player, uid);
            }
            catch (Exception e)
            {
                // Log de erro formatado
                string errorMsg = e is exception customEx ? customEx.getFullMessageError() : e.Message;

                _smp.LogManager.Instance.push(new AppMessage(
                    $"[{nameof(Handle_REQUEST_PLAYER_INFO)}] [Error] {errorMsg}",
                    type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }
    }
}