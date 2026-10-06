using Pangya_GameServer.Server;
using Pangya_GameServer.Session;
using PangyaAPI.Network;
using PangyaAPI.Network.Core;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
namespace Pangya_GameServer.Handles
{
    public class Handle_PLAYER_ENTER_LOBBY_GRAND_PRIX : HandleBase<Player, Packet_EXAMPLE>
    {
        public override async Task Handle()
        {
            var _channel = Player.GetChannel();

            var srv = GameServer.Instance.getInfo();
            try
            {
                if (!srv.Property.GrandPrixMode)
                {
                    throw new exception(
                        $"[GrandPrix] Player[UID={Player.UserInfo.UID}] tentou entrar no Lobby GP desativado.",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 1, 0x750001)
                    );
                }

                _channel?.Lobby.EnterGrandPrix(Player);
                var p = new Packet(0x250);

                p.WriteUInt32(0u); // Status OK

                // Lista de Eventos Grand Prix Ativos (Baseado no bitmask/rates do servidor)
                uint countEvents = srv.Rate.CountBitGrandPrixEvent();
                p.WriteUInt32(countEvents);
                foreach (var eventType in srv.Rate.GetValueBitGrandPrixEvent())
                {
                    p.WriteUInt32(eventType);
                }
                p.WriteInt32(Player.UserInfo.GrandPrixHistory.Count);
                foreach (var entry in Player.UserInfo.GrandPrixHistory)
                {
                    p.WriteUInt32(entry._typeid);
                    p.WriteUInt32(entry.position); // Rank/Posição final obtida
                }

                p.WriteFloat(Player.UserInfo.Statistics.getMediaScore());
                Player.Send(p);
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage(
                    $"[Handle_PLAYER_ENTER_GRAND_PRIX_LOBBY][Error] {e.getFullMessageError()}",
                    type_msg.CL_FILE_LOG_AND_CONSOLE)
                );

                // Resposta de erro para o cliente não ficar em estado de "Loading"
                var errorPkt = new Packet(0x250);
                uint errorCode = (ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) == STDA_ERROR_TYPE.CHANNEL)
                    ? ExceptionError.STDA_SYSTEM_ERROR_DECODE(e.getCodeError())
                    : 0x750000;

                errorPkt.WriteUInt32(errorCode);
                Player.Send(errorPkt);
            }

            await Task.CompletedTask;
        }
    }
}
