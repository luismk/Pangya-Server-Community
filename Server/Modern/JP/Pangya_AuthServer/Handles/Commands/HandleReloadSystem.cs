using Pangya_AuthServer.Feature;
using Pangya_AuthServer.Models;
using Pangya_AuthServer.Repository;
using Pangya_AuthServer.Server;
using PangyaAPI.Network;
using PangyaAPI.Utilities.Log;

namespace Pangya_AuthServer.Handles.Commands
{
    public class HandleReloadSystem : ICmdHandler
    {
        public async Task Execute(CommandInfo el)
        {  
            uint typeReload = el.arg[0];

            _smp.LogManager.Instance.push(new AppMessage(
                $"[HandleReloadSystem] Reloading System Type: {typeReload}",
                type_msg.CL_FILE_LOG_AND_CONSOLE));
             
            // 3. Notifica outros servidores (se o target for um GameServer, por exemplo)
            var p = new Packet(0x0A); // Exemplo de OpCode para Reload
            p.WriteUInt32(typeReload);

            await CommandSender.SendToTarget(el, p);
        }
    }
}