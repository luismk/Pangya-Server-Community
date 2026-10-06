using Pangya_AuthServer.Feature;
using Pangya_AuthServer.Models;
using Pangya_AuthServer.Repository;
using Pangya_AuthServer.Server;
using PangyaAPI.Network;

namespace Pangya_AuthServer.Handles.Commands
{
    public class HandleAdmKick : ICmdHandler
    {
        public async Task Execute(CommandInfo el)
        { 
            var m_si = AuthServer.Instance.m_si;
             
            var p = new Packet(0x06);
            p.WriteUInt32(el.arg[0]); // Player UID
            p.WriteInt32(m_si.UID);   // Admin UID
            p.WriteByte(1);           // Force ServerFlag

            await CommandSender.SendToTarget(el, p);
        }
    }
}