using Pangya_AuthServer.Session;
using PangyaAPI.Network;
using PangyaAPI.Network.Core;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using System;
using System.Threading.Tasks;

namespace Pangya_AuthServer.Handles
{
    public class Handle_HEARTBEAT : HandleBase<Player, Packet_EXAMPLE>
    {
        public override async Task Handle()
        {
            try
            {
                // Atualiza o timestamp para o monitor de conexões
                Player.last_activity = new SystemTime(DateTime.Now);

                // OpCode 0xFE - Mantém o link entre Auth e Sub-Servers
                using (var response = new Packet(0xFE))
                {
                    // Envia o tempo atual para sincronia
                    response.WriteTime(Player.last_activity); 
                    Player.SendAuth(response);
                }

                //_smp.LogManager.Instance.push(new AppMessage(
                //   $"[Handle_HEART][Sucess] UPDATE SERVER {Player.m_pi.UID} ON",
                //   type_msg.CL_FILE_LOG_AND_CONSOLE)); 
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Critical] Falha no Handle_HEART para {Player.UserInfo.UID}: {ex.Message}");
            }
        }
    }
}