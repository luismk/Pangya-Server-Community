using Pangya_AuthServer.Feature;
using Pangya_AuthServer.Manager;
using Pangya_AuthServer.Models;
using Pangya_AuthServer.Repository;
using Pangya_AuthServer.Server;
using PangyaAPI.Network;
using PangyaAPI.Utilities.Log;

namespace Pangya_AuthServer.Handles.Commands
{
    public class HandleShutdown : ICmdHandler
    {
        public async Task Execute(CommandInfo el)
        {
            CmdShutdownInfo cmd_si = new CmdShutdownInfo(el.idx);
            snmdb.NormalManagerDB.Instance.add(0, cmd_si);

            if (cmd_si.getException().getCodeError() != 0) return;

            int timeSec = cmd_si.getInfo();
            var p = new Packet(0x02);
            p.WriteInt32(timeSec);

            var m_si = AuthServer.Instance.m_si;

            // Se o target for o próprio Auth
            if (el.target == m_si.UID || el.target == m_si.Type)
            {
                CommandSender.Broadcast(AuthServer.Instance.getAllSessions(), p);
                _smp.LogManager.Instance.push(new AppMessage($"[Shutdown] Desligando Auth em {timeSec}s", type_msg.CL_FILE_LOG_AND_CONSOLE));

                if (timeSec <= 0) Thread.Sleep(5000);
                // shutdown_logic_here();
            }
            else
            {
                await CommandSender.SendToTarget(el, p);
            }
        }
    }
}