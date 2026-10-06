using Pangya_AuthServer.Manager;
using Pangya_AuthServer.Models;
using Pangya_AuthServer.Server;
using Pangya_AuthServer.Session;
using PangyaAPI.Network;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pangya_AuthServer.Feature
{
    public static class CommandSender
    {
        public static async Task SendToTarget(CommandInfo el, Packet p)
        {
            var s = AuthServer.Instance.FindSessionByUID(el.target);
            if (s != null)
            {
                s.SendAuth(p);
            }
            else
            {
                var servers = AuthServer.Instance.FindPlayersByType(el.target);
                if (servers.Count > 0)
                {
                    Broadcast(servers, p);
                }
            }
        }

        public static void Broadcast(List<Player> sessions, Packet p)
        {
            foreach (var _session in sessions)
            {
                _session.SendAuth(p);
            }
        }
    }
}
