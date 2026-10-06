using Pangya_MessengerServer.Session;
using PangyaAPI.Network;
using PangyaAPI.Network.Core;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;

namespace Pangya_MessengerServer.Handles
{
    public class Handle_PLAYER_INVITE_ROOM : HandleBase<Player, Packet_EXAMPLE>//<Packet_PLAYER_INVITE_TO_ROOM, MPlayer>
    {
        public override async Task Handle()
        {
            try
            {
                uint player_invited_uid = Packet.ReadUInt32();

                // Validação de Segurança: O UID enviado no pacote deve ser o do próprio player da sessão
                if (player_invited_uid != Player.UserInfo.UID)
                {
                    throw new exception($"[MessengerService::HandleNotifyInvited][Error] Player[UID={Player.UserInfo.UID}] informou um convite para UID={player_invited_uid} (divergente). Hacker ou Bug.",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.MESSAGE_SERVER, 3749, 0));
                }

                // Log de rastreio para o Pangya Fun
                _smp.LogManager.Instance.push(new AppMessage($"[MessengerService::HandleNotifyInvited][Log] Player[UID={Player.UserInfo.UID}] foi convidado para uma sala no jogo.",
                    type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Como este pacote é apenas uma notificação do Cliente -> Server (Notify), 
                // geralmente não há resposta (Packet Out) necessária, a menos que você queira sincronizar algo no DB.
                await Task.CompletedTask;
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[MessengerService::HandleNotifyInvited][ErrorSystem] " + e.getFullMessageError(),
                    type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }
    }
}
