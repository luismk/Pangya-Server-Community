using Pangya_GameServer.Flags;
using Pangya_GameServer.PacketFunc;
using Pangya_GameServer.Server;
using Pangya_GameServer.Session;
using PangyaAPI.Network;
using PangyaAPI.Network.Core;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;

namespace Pangya_GameServer.Handles
{
    public class Handle_PLAYER_NOTIFY_NOT_DISPLAY_PRIVATE_MESSAGE_NOW : HandleBase<Player, Packet_EXAMPLE>
    {

        public override async Task Handle()
        {
            try
            { 
                string nicknameSender = Packet.ReadPStr();

                if (string.IsNullOrWhiteSpace(nicknameSender))
                {
                    throw new exception($"[WhisperRefuse] Player[UID={Player.UserInfo.UID}] enviou um NickName vazio.",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 0x750050, 0));
                }

                if (!Tools.Sanitize(nicknameSender))
                {
                    throw new exception($"[WhisperRefuse] Player[UID={Player.UserInfo.UID}] enviou NickName com caracteres suspeitos: {nicknameSender}",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 1, 1));
                }

                // 2. Localiza o remetente original da mensagem
                var senderSession = GameServer.Instance.FindSessionByNickname(nicknameSender);

                if (senderSession != null && senderSession.Connected)
                {
                    // Log do evento
                    _smp.LogManager.Instance.push(new AppMessage(
                        $"[WhisperRefuse] Player[{Player.UserInfo.NickName}] recusou automaticamente o Whisper de [{nicknameSender}].",
                        type_msg.CL_FILE_LOG_AND_CONSOLE)); 
                    var response = Handle_PACKET_RESPONSE.pacote040(nicknameSender, "", eChatMsg.CHAT_REFUSE_WHISPER); 
                    senderSession.Send(response);
                }
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage(
                    $"[Handle_NOTIFY_NOT_DISPLAY_PRIVATE_MESSAGE_NOW][Error] {e.getFullMessageError()}",
                    type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

        await Task.CompletedTask;
        }
    }
}