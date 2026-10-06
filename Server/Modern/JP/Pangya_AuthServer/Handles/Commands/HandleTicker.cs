using Pangya_AuthServer.Feature;
using Pangya_AuthServer.Manager;
using Pangya_AuthServer.Models;
using Pangya_AuthServer.Repository;
using Pangya_AuthServer.Server;
using PangyaAPI.Network;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pangya_AuthServer.Handles.Commands
{
    public class HandleTicker : ICmdHandler
    {
        public async Task Execute(CommandInfo el)
        {
            CmdTickerInfo cmd_ti = new CmdTickerInfo(el.idx);
            snmdb.NormalManagerDB.Instance.add(0, cmd_ti);

            if (cmd_ti.getException().getCodeError() != 0) return;

            var ti = cmd_ti.getInfo();
            if (!ti.isValid()) return;

            using (var p = new Packet(0x04))
            {
                p.WriteString(ti.nick);
                p.WriteString(ti.msg);

                // Lógica específica de excluir o UID de origem
                var sessions = AuthServer.Instance.FindPlayerByTypeExcludeUID(el.target, el.arg[1]);
                CommandSender.Broadcast(sessions, p);
            }
        }
    }
}
