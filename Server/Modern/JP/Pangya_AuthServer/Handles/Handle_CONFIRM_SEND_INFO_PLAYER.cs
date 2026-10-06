using Pangya_AuthServer.Manager;
using Pangya_AuthServer.Server;
using Pangya_AuthServer.Session;
using PangyaAPI.Network;
using PangyaAPI.Network.Core;
using PangyaAPI.Utilities.Log;
using System;
using System.Threading.Tasks;

namespace Pangya_AuthServer.Handles
{
    public class Handle_CONFIRM_SEND_INFO_PLAYER : HandleBase<Player, Packet_EXAMPLE>
    {
        public override async Task Handle()
        {
            try
            {
                // 1. Read packet data
                uint reqServerUid = Packet.ReadUInt32();
                int option = Packet.ReadInt32();
                uint playerUid = Packet.ReadUInt32();

                string playerId = string.Empty;
                string playerIp = string.Empty;

                if (option == 1)
                {
                    playerId = Packet.ReadString();
                    playerIp = Packet.ReadString();
                }

                // 2. Check if the target is the AuthServer itself
                if (reqServerUid == AuthServer.Instance.m_si.UID)
                {
                    _smp.LogManager.Instance.push(new AppMessage(
                        $"[Handle_CONFIRM_SEND_INFO_PLAYER][Self-Target] Confirmation ignored: Server UID {reqServerUid} is the AuthServer. Player: {playerUid}",
                        type_msg.CL_FILE_LOG_AND_CONSOLE));
                    return;
                }

                // 3. Find the target server Player
                var targetServer = AuthServer.Instance.FindPlayer(reqServerUid);

                if (targetServer != null)
                {
                    // Success Log
                    _smp.LogManager.Instance.push(new AppMessage(
                        $"[Handle_CONFIRM_SEND_INFO_PLAYER][Sucess] Routing confirmation: SOURCE[Server: {Player.UserInfo.UID}] -> DEST[Server: {reqServerUid}] PLAYER: {playerUid}",
                        type_msg.CL_FILE_LOG_AND_CONSOLE));

                    // 4. Build response (OpCode 0x0C)
                    using (var p = new Packet(0x0C))
                    {
                        p.WriteUInt32(Player.UserInfo.UID); // Sender UID
                        p.WriteInt32(option);
                        p.WriteUInt32(playerUid);

                        if (option == 1)
                        {
                            p.WriteString(playerId);
                            p.WriteString(playerIp);
                        }

                        // 5. Send packet to target server
                        targetServer.SendAuth(p);
                    }
                }
                else
                {
                    // Warning Log
                    _smp.LogManager.Instance.push(new AppMessage(
                        $"[Handle_CONFIRM_SEND_INFO_PLAYER][Warning] Target offline: Server {reqServerUid} not found for Player {playerUid}.",
                        type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
            }
            catch (Exception ex)
            {
                // Error Log
                _smp.LogManager.Instance.push(new AppMessage(
                    $"[Handle_CONFIRM_SEND_INFO_PLAYER][Exception] {ex.Message}{Environment.NewLine}{ex.StackTrace}",
                    type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }
    }
}