using Pangya_MessengerServer.Server;
using Pangya_MessengerServer.Session;
using PangyaAPI.Network;
using PangyaAPI.Network.Core;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;

namespace Pangya_MessengerServer.Handles
{
    public class Handle_PLAYER_LOGOUT : HandleBase<Player, Packet_EXAMPLE>
    {
        public override async Task Handle()
        {
            try
            {
                // No Pangya, o Logout no Messenger geralmente não envia dados no corpo do pacote,
                // pois a 'Player' já identifica quem está saindo.

                // 1. Notifica todos os amigos e membros da guilda que este player ficou offline
                MessengerServer.Instance.SendUpdatePlayerLogoutToFriends(Player);

                // Log opcional para monitorar o tráfego de saída
                _smp.LogManager.Instance.push(new AppMessage($"[Handle_PLAYER_LOGOUT] Player[{Player.UserInfo.UID}] deslogou do Messenger.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                MessengerServer.Instance.Disconnect(Player);
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[Handle_PLAYER_LOGOUT][Error] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            await Task.CompletedTask;
        }
         
    }
}