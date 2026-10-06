using Pangya_GameServer.PacketFunc;
using Pangya_GameServer.Session;
using PangyaAPI.Network;
using PangyaAPI.Network.Core;
using PangyaAPI.Network.Repository;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using PangyaAPI.Utilities.Models;
using System;
using System.Threading.Tasks;

namespace Pangya_GameServer.Handles
{
    public class Handle_PLAYER_CONNECT_MSN : HandleBase<Player, Packet_EXAMPLE>
    {
        public override async Task Handle()
        {
            try
            {
                // 2. Busca servidor no DB 
                var serverList = DBCommand.GetMsn();

                // 3. Verifica disponibilidade
                if (serverList == null || serverList.Count == 0)
                {
                    var p = new Packet(0xFC);
                    p.WriteByte((byte)0);
                    Player.Send(p);
                    return;
                }

                // 4. Envio de sucesso (conecta ao primeiro disponível)  
                Player.Send(Handle_PACKET_RESPONSE.pacote0FC(serverList));
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage($"[Handle_PLAYER_CONNECT_MSN][Error] {e.getFullMessageError()}", type_msg.CL_FILE_LOG_AND_CONSOLE));
                var p = new Packet(0xFC);
                p.WriteByte((byte)0); 
                Player.Send(p);
            }
        }
    }
}