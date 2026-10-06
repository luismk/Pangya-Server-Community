using Pangya_AuthServer.Feature;
using Pangya_AuthServer.Models;
using Pangya_AuthServer.Repository;
using Pangya_AuthServer.Server;
using PangyaAPI.Network;
using PangyaAPI.Utilities.Log;

namespace Pangya_AuthServer.Handles.Commands
{
    public class HandleNewItem : ICmdHandler
    {
        public async Task Execute(CommandInfo el)
        {
            try
            { 
                var p = new Packet(0x08);
                p.WriteUInt32(el.arg[0]); // Player UID
                p.WriteUInt32(el.arg[1]); // Msg Id

                // Tenta enviar
                await CommandSender.SendToTarget(el, p);
            }
            catch (Exception ex)
            {
                // Log de Erro: Fundamental para capturar falhas de null reference ou rede
                _smp.LogManager.Instance.push(new AppMessage(
                    $"[HandleNewItem][Error] Failed to execute for Player {el.arg[0]}. Exception: {ex.Message}",
                    type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }
    }
}