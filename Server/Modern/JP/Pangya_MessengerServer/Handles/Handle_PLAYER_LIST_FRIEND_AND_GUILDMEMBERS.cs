using Pangya_MessengerServer.Models;
using Pangya_MessengerServer.Server;
using Pangya_MessengerServer.Session;
using PangyaAPI.Network;
using PangyaAPI.Network.Core;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using PangyaAPI.Utilities.Models;

namespace Pangya_MessengerServer.Handles
{
    public class Handle_FRIEND_GUILD_LIST : HandleBase<Player, Packet_EXAMPLE>
    {
        public override async Task Handle()
        {
            try
            {
                MessengerServer.Instance.SendUpdatedFriendList(Player);
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[Handle_FRIEND_GUILD_LIST][Error] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

            }

            await Task.CompletedTask;
        }
    }
}