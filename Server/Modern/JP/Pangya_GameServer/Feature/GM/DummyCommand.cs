using Pangya_GameServer.Feature.GM;
using Pangya_GameServer.Session;
using PangyaAPI.Network;
using PangyaAPI.Utilities.Log;

namespace Pangya_GameServer.Feature.GM
{
    public class DummyCommand : IGMCommand
    {
        public async Task Execute(Player s, Packet packet)
        {
            _smp.LogManager.Instance.push(new AppMessage(
             $"[GM-Action] Por: {s.UserInfo.NickName} (UID: {s.UserInfo.UID})",
             type_msg.CL_ONLY_CONSOLE));
        }
    }
}