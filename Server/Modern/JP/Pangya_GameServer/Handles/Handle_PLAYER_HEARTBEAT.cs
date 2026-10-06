using Pangya_GameServer.Server;
using Pangya_GameServer.Session;
using PangyaAPI.Network;
using PangyaAPI.Network.Core;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using System;
using System.Threading.Tasks;

namespace Pangya_GameServer.Handles
{
    public class Handle_PLAYER_HEARTBEAT : HandleBase<Player, Packet_EXAMPLE>
    {
        public override async Task Handle()
        {
            try
            {
                // Calcula o tempo decorrido desde o último tick (em milisegundos)
                long lastTick = Player.TicketBot;
                int currentTick = Packet.ReadInt32();
                long diff = currentTick - lastTick;

                //_smp.LogManager.Instance.push(new AppMessage($"[Handle_PLAYER_HEARTBEAT][Log] Normal[UID= {Player.PlayerUserStatistics.UID}, TIME OLD= {diff}ms, TIME NOW= {currentTick}ms", type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Se o tempo passado for maior que o TTL definido + 25%, loga um Warning
                long ttlLimit = GameServer.Instance.getBotTTL() + (GameServer.Instance.getBotTTL() / 4);
                if (diff >= ttlLimit)
                {
                     _smp.LogManager.Instance.push(new AppMessage($"[Handle_PLAYER_HEARTBEAT][Warning] Normal[UID= {Player.UserInfo.UID}, TIME OLD= {diff}ms (LIMIT= {ttlLimit}ms)] DELAY", type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
                // Atualiza o tick da sessão
                Player.TicketBot = Environment.TickCount;
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage($"[Handle_PLAYER_HEARTBEAT][ErrorSystem] {e.getFullMessageError()}", type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            await Task.CompletedTask;
        }
    }
}