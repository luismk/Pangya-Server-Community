using Pangya_GameServer.Server;
using Pangya_GameServer.Session;
using PangyaAPI.Network;
using PangyaAPI.Network.Core; 
namespace Pangya_GameServer.Handles
{
    public class Handle_PLAYER_SERVER_LIST : HandleBase<Player, Packet_EXAMPLE>
    {
        public override async Task Handle()
        {
			try
			{
				GameServer.Instance.SendUpdateServerList(Player);
			}
			catch (Exception)
			{

				throw;
			}
        }
    }
}