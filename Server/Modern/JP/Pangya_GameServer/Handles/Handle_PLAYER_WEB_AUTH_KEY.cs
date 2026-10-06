using Pangya_GameServer.Repository;
using Pangya_GameServer.Session;
using PangyaAPI.Network;
using PangyaAPI.Network.Core;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using PangyaAPI.Utilities.Models;

namespace Pangya_GameServer.Handles
{
    public class Handle_PLAYER_WEB_AUTH_KEY : HandleBase<Player, Packet_EXAMPLE>
    {
        public override async Task Handle()
        {
            try
            {
                // 2. Criação do Comando de Banco de Dados 
                string webKey = CommandDB.WEBKeyGeneration(Player.UserInfo.UID);

                // 4. Resposta ao Cliente (0x1AD)
                using (var p = new Packet())
                {
                    p.init_plain(0x1AD);
                    p.WriteString(webKey);
                    p.WriteByte(1);
                    Player.Send(p);
                }

                // Log de auditoria
                _smp.LogManager.Instance.push(new AppMessage(
                    $"[WebAuth] Chave gerada para UID={Player.UserInfo.UID}: {webKey}",
                    type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage(
                    $"[Handle_PLAYER_WEB_AUTH_KEY][ErrorSystem] {e.getFullMessageError()}",
                    type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Em caso de erro, podemos enviar o pacote com falha (0)
                SendError(Player);
            }
        }

        private void SendError(Player session)
        {
            using (var p = new Packet())
            {
                p.init_plain(0x1AD);
                p.WriteString(""); // Chave vazia
                p.WriteByte(0);  // Falha
                Player.Send(p);
            }
        }
    }
}