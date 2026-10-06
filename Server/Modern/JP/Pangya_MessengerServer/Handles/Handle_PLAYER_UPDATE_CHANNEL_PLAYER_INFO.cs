using Pangya_MessengerServer.Models;
using Pangya_MessengerServer.Server;
using Pangya_MessengerServer.Session;
using PangyaAPI.Network;
using PangyaAPI.Network.Core;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using PangyaAPI.Utilities.Models;
using System.Data.Common;

namespace Pangya_MessengerServer.Handles
{
    public class Handle_UPDATE_CHANNEL_INFO : HandleBase<Player, Packet_EXAMPLE>
    {
        public override async Task Handle()
        {
            try
            {
                // 1. Lê e atualiza as informações de canal/sala da sessão
                Player.UserInfo.m_cpi.ToRead(Packet);

                var servers = PangyaAPI.Network.Repository.DBCommand.GetGame();

                // 3. Valida se o servidor escolhido existe e está online
                var selectedServer = servers.FirstOrDefault(c => c.UID == Player.UserInfo.m_cpi.server_uid);



                // Log detalhado para o console do Messenger
                LogChannelUpdate(Player);

                if (selectedServer != null)//verifica se realmente existe esse servidor..
                {  
                    // 2. Prepara o pacote de broadcast (0x30 -> 0x115)
                    using (var p = new Packet(0x30))
                    {
                        p.WriteUInt16(0x115); // Sub packet Id
                        p.WriteUInt32(Player.UserInfo.UID);
                        p.WriteUInt32((uint)Player.UserInfo.m_state);
                        p.WriteByte(1); // Status OK
                        p.WriteBytes(Player.UserInfo.m_cpi.ToArray());

                        // 3. Envia para o próprio jogador (Confirmação)
                        Player.Send(p);

                        // 4. Envia para todos os amigos/guilda (Broadcast)
                        var targets = MessengerServer.Instance.FindAllFriend(
                                Player.UserInfo.m_friend_manager.getAllFriendAndGuildMember(true)
                            );

                        if (targets != null && targets.Count > 0)
                        {
                            MessengerServer.Instance.FriendBroadcast(targets, Player, p);
                        }
                    }
                }
                else
                {
                    SendErrorResponse(Player);
                    MessengerServer.Instance.Disconnect(Player);
                }


            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[Handle_UPDATE_CHANNEL_INFO][Error] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Envia pacote de erro (Byte 0) para o cliente não ficar esperando
                SendErrorResponse(Player);
            }

            await Task.CompletedTask;
        }

        private async void LogChannelUpdate(Player Player)
        {
            var info = Player.UserInfo.m_cpi;
            var roomNum = info.room.number;

            var servers = PangyaAPI.Network.Repository.DBCommand.GetGame();

            // 3. Valida se o servidor escolhido existe e está online
            var selectedServer = servers.FirstOrDefault(c => c.UID == info.server_uid);

            if (selectedServer != null)
            {
                _smp.LogManager.Instance.push(new AppMessage(
       $"[Handle_UPDATE_CHANNEL_INFO][Log] Player[{Player.UserInfo.UID}] -> IN: {selectedServer.Name}, Room: {roomNum}, Name: {info.name}",
       type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
            else
            {
                // Servidor não encontrado na lista
                _smp.LogManager.Instance.push(new AppMessage(
                    $"[Handle_UPDATE_CHANNEL_INFO]][Error] Servidor UID {info.server_uid} não existe ou está offline.",
                    type_msg.CL_FILE_LOG_AND_CONSOLE));

            }

        }

        private void SendErrorResponse(Player Player)
        {
            using (var p = new Packet((ushort)0x30))
            {
                p.WriteUInt16(0x115);
                p.WriteUInt32(Player.UserInfo.UID);
                p.WriteUInt32((uint)Player.UserInfo.m_state);
                p.WriteByte(0); // Error status
                Player.Send(p);
            }
        }
    }
}