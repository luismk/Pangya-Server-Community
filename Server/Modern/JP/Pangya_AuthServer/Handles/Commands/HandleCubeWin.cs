using Pangya_AuthServer.Feature;
using Pangya_AuthServer.Models;
using Pangya_AuthServer.Repository;
using Pangya_AuthServer.Server;
using Pangya_AuthServer.Session;
using PangyaAPI.Network;
using PangyaAPI.Utilities.Log;

namespace Pangya_AuthServer.Handles.Commands
{
    public class HandleCubeWin : ICmdHandler
    {
        public async Task Execute(CommandInfo el)
        {
            try
            {
                // 1. Busca informação no Banco de Dados
                CmdNoticeInfo cmd_ni = new CmdNoticeInfo(el.idx);
                snmdb.NormalManagerDB.Instance.add(0, cmd_ni);

                // 2. Validações iniciais
                if (cmd_ni.getException().getCodeError() != 0)
                {
                    _smp.LogManager.Instance.push(new AppMessage(
                        $"[HandleNotice][Error] {cmd_ni.getException().getFullMessageError()}",
                        type_msg.CL_ONLY_CONSOLE));
                    return;
                }

                var msg = cmd_ni.getInfo();
                if (string.IsNullOrEmpty(msg))
                {
                    _smp.LogManager.Instance.push(new AppMessage(
                        $"[HandleNotice][Error] msg is empty. Comando[{el.toString()}]",
                        type_msg.CL_FILE_LOG_AND_CONSOLE));
                    return;
                }

                // 3. Montagem do Pacote (OpCode 0x03)
                var p = new Packet(0x05);
                p.WriteString(msg);

                // 4. Lógica de Envio (Target ou Broadcast por Tipo)
                var s = (Player)AuthServer.Instance.FindSessionByUID(el.target);

                if (s == null)
                {
                    var _sessions = AuthServer.Instance.FindPlayersByType(el.target);
                    if (_sessions.Count > 0)
                    {
                        _smp.LogManager.Instance.push(new AppMessage(
                            $"[HandleNotice][Log] Send Broadcast Notice[MESSAGE={msg}] For Server[UID={el.target}]",
                            type_msg.CL_FILE_LOG_AND_CONSOLE));

                        // Chame seu método de Broadcast global
                        CommandSender.Broadcast(_sessions, p);
                    }
                    else
                    {
                        _smp.LogManager.Instance.push(new AppMessage(
                            $"[HandleNotice][Error] Nao encontrou o SERVER[UID/TIPO={el.target}] para enviar Notice.",
                            type_msg.CL_FILE_LOG_AND_CONSOLE));
                    }
                }
                else
                {
                    _smp.LogManager.Instance.push(new AppMessage(
                        $"[HandleNotice][Log] Send Broadcast Notice[MESSAGE={msg}] For Server[UID={el.target}]",
                        type_msg.CL_FILE_LOG_AND_CONSOLE));

                    s.SendAuth(p);
                }
            }
            catch (Exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage(
                    $"[HandleNotice][Critical] {e.Message}",
                    type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            await Task.CompletedTask; // Mantém a compatibilidade async
        }
    }
}