using Pangya_GameServer.Repository;
using Pangya_GameServer.Server;
using Pangya_GameServer.Session;
using PangyaAPI.Network;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static Pangya_GameServer.Manager.TradeShopManager;

namespace Pangya_GameServer.Feature.GM
{
    public class MatchHoleCommand : IGMCommand
    {
        // Lista de valores permitidos para facilitar a manutenção
        private readonly HashSet<int> _allowedHoleCounts = new() { 3, 6, 9, 18 };

        public async Task Execute(Player session, Packet pkt)
        {
            try
            {
                var holeCount = pkt.ReadInt32();
                var room = session.GetRoom();

                // 2. Log de auditoria (Console)
                _smp.LogManager.Instance.push(new AppMessage(
                    $"[GM-Action] Executor: {session.UserInfo.NickName} (UID: {session.UserInfo.UID}) | Command: MatchHole | Value: {holeCount}",
                    type_msg.CL_ONLY_CONSOLE));

                // 3. Validação de Estado
                if (room == null || room.GameRun())//jogo esta em percuso, nao pode editar.
                {
                    session.SendChatNotice("This needs to be in a room first.");
                    // Opcional: Enviar mensagem ao player dizendo que ele precisa estar em uma sala
                    return;
                }

                // 4. Lógica de Negócio
                if (_allowedHoleCounts.Contains(holeCount))
                {
                    room.SetQntdHole((byte)holeCount);
                    room.SendHeadRoom();
                    GameServer.Instance.sendUpdateRoomInfo(room, 3);
                    _smp.LogManager.Instance.push(new AppMessage($"[MatchHoleCommand][Sucess] ROOM[ID: {room.GetRoomId()}, UPDATE: {holeCount}, NICK: {session.UserInfo.NickName}]", type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
            }
            catch (Exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage(
                    $"[MatchHoleCommand][Error] {e.Message} | StackTrace: {e.StackTrace}",
                    type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            await Task.CompletedTask;
        }
    }
}