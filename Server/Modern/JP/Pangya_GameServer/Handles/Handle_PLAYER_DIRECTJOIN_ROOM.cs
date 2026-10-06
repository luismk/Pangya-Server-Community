using Pangya_GameServer.Session;
using PangyaAPI.Network;
using PangyaAPI.Network.Core;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using System;
using System.Threading.Tasks;

namespace Pangya_GameServer.Handles
{
    public class Handle_PLAYER_DIRECT_JOIN_ROOM : HandleBase<Player, Packet_EXAMPLE>
    {
        public override async Task Handle()
        {
            var targetChannel = Player.GetChannel();
            try
            { 
                byte tarGetChannelId = Packet.ReadByte();      // btChannelUID
                short targetRoomId = Packet.ReadInt16();       // wRoomGUID
                string password = Packet.ReadString(7);      // Password da sala (7 chars no S4)

                var currentChannelId = targetChannel?.getId();

                //// 2. Se o canal alvo for diferente do canal atual
                //if (tarGetChannelId != currentChannelId)
                //{
                     
                //    if (targetChannel == null)
                //    {
                //        SendJoinError(Player, 4); // Canal Inválido
                //        return;
                //    }

                //    // Verifica se o Player pode entrar no novo canal (Level, Full, etc)
                //    bool enterCheck = targetChannel.CheckEnterChannel(Player);
                //    if (enterCheck)
                //    {
                //        SendJoinError(Player, 0);
                //        return;
                //    }

                //    // Sai do canal atual
                //    if (targetChannel != null)
                //    {
                //        targetChannel.LeaveChannel(Player); 
                //    }

                //    // Entra no novo canal e no Lobby dele
                //    if (!targetChannel.EnterChannel(Player))
                //    {
                //        SendJoinError(Player, 4);
                //        return;
                //    }

                //    targetChannel.Lobby.EnterLobby(Player, 0); // Entra no lobby padrão
                //}

                //// 3. Lógica de Join na Sala
                //// No S4, se o Player já estiver em uma sala diferente, ele precisa sair primeiro
                //if (Player.PlayerUserStatistics.mi.RoomID != -1 && Player.PlayerUserStatistics.mi.RoomID != targetRoomId)
                //{
                //    // Sai da sala atual antes de migrar
                //    Player.CurrentRoom?.RemovePlayer(Player);
                //}

                //// Tenta entrar na sala alvo
                //if (targetChannel != null)
                //{
                //    // Chama a lógica de join (DisJoinRoom no original redireciona para a sala)
                //    targetChannel.Lobby.RequestEnterRoom(Player, targetRoomId, password);
                //}
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage(
                    $"[DirectJoin][ErrorSystem] {e.getFullMessageError()}",
                    type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

        await Task.CompletedTask;
        }

        private void SendJoinError(Player session, byte errorType)
        {
            // Pacote 0x41 (65 decimal) - Erro de Join
            var p = new Packet(0x41);
            p.WriteByte(errorType);
            Player.Send(p);
        }
    }
}