using Pangya_GameServer.Server;
using Pangya_GameServer.Session;
using PangyaAPI.Network;
using PangyaAPI.Network.Core;
using PangyaAPI.Network.Session;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using System;
using System.Threading.Tasks;

namespace Pangya_GameServer.Handles
{
    public class Handle_PLAYER_EXIT_ROOM : HandleBase<Player, Packet_EXAMPLE>
    {
        public override async Task Handle()
        {
            try
            {
            var _channel = Player.GetChannel();
                byte option = Packet.ReadByte();
                short roomId = Packet.ReadInt16();
                uint gamePang = Packet.ReadUInt32();
                uint gameBonus = Packet.ReadUInt32();
                byte[] roomKey = Packet.ReadBytes(8);

                var code = _channel?.LeaveRoomMultiPlayer(Player, 1);
                if (code > Channels.Channel.LEAVE_ROOM_STATE.DO_NOTHING)
                {
                    // Log de depuração
                    _smp.LogManager.Instance.push(new AppMessage(
                        $"[Handle_PLAYER_EXIT_ROOM][Sucess] Normal[UID: {Player.UserInfo.UID}, RID: {Player.UserInfo.Member.RoomID}] EXIT TO ROOM. Option: {option}, Pang: {gamePang}",
                        type_msg.CL_FILE_LOG_AND_CONSOLE)); 
					//atualiza.
					_channel.UpdatePlayerInfo(Player);
                    _channel.SendUpdatePlayerInfo(Player, 3);
                } 
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage(
                    $"[Handle_PLAYER_EXIT_ROOM][ErrorSystem] {e.getFullMessageError()}",
                    type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            await Task.CompletedTask;
        }
    }
}