using Pangya_GameServer.Models;
using Pangya_GameServer.PacketFunc;
using Pangya_GameServer.Server;
using Pangya_GameServer.Session;
using PangyaAPI.Network;
using PangyaAPI.Network.Core;
using PangyaAPI.Network.Session;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using PangyaAPI.Utilities.Models;

namespace Pangya_GameServer.Handles
{
    public class Handle_PLAYER_CHANGE_AFK_STATE : HandleBase<Player, Packet_EXAMPLE>
    {
        public override async Task Handle()
        {
            var _channel = Player.GetChannel();
            try
            {
                byte state = Packet.ReadByte();

                var room = Player.GetRoom();

                if (room == null)
                {
                    throw new exception(
                        $"[AFK][Error] Player[UID={Player.UserInfo.UID}] tentou mudar estado AFK mas não está em uma sala.",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 10, 0));
                }

                var pri = room.GetPlayerInfo(Player);

                // PlayerLobbyInfo (Info que o canal enxerga no Lobby) 
                var pci = _channel?.GetPlayerInfo(Player);

                if (pri == null || pci == null)
                {
                    throw new exception(
                        $"[AFK][Error] Falha ao localizar Info de Sala ou Lobby para UID={Player.UserInfo.UID}.",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 11, 0));
                }

                //Atualização dos Flags
                pci.State.Sleep = pri.State.Sleep = state;

                _channel?.UpdatePlayerInfo(Player);
                room?.UpdatePlayerInfo(Player);

                using (var response = new Packet())
                {
                    response.init_plain(0x8E);
                    response.WriteInt32(Player.ConnectionID); // OID do jogador
                    response.WriteByte(state);          // Novo estado 
                    room?.SendBroadCast(response);
                }

                if (_channel != null)
                {
                    _channel.Lobby.SendUpdatePlayerInfo(Player, 3);
                }

            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage(
                    $"[Handle_PLAYER_CHANGE_AFK_STATE][ErrorSystem] {e.getFullMessageError()}",
                    type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

        await Task.CompletedTask;
        }
    }
}