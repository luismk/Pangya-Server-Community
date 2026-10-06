using Pangya_GameServer.Session;
using PangyaAPI.Network;
using PangyaAPI.Network.Core;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using System;
using System.Threading.Tasks;

namespace Pangya_GameServer.Handles
{
    public class Handle_PLAYER_CHANGE_WHISPER_STATE : HandleBase<Player, Packet_EXAMPLE>
    {
        public override async Task Handle()
        {
            try
            { 
                // O pacote envia apenas 1 byte (0 para OFF, 1 para ON)
                byte whisperState = Packet.ReadByte();

                // Validação de segurança (Anti-Hacker/Bug)
                if (whisperState > 1)
                {
                    throw new exception(
                        $"[Handle_PLAYER_CHANGE_WHISPER_STATE][Error] Normal[UID={Player.UserInfo.UID}] enviou estado inválido: {whisperState}",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 1, 0x5300101));
                }

                // Atualiza o estado na estrutura de dados da sessão
                // mi.State.Whisper costuma ser usado para sincronização visual/IFF
                Player.UserInfo.Member.State.Whisper = whisperState;
                Player.UserInfo.WhisperState = whisperState;

                // Log formatado para o console e arquivo
                string stateText = whisperState == 1 ? "ON" : "OFF";
                _smp.LogManager.Instance.push(new AppMessage(
                    $"[Handle_PLAYER_CHANGE_WHISPER_STATE][Info] Normal[UID={Player.UserInfo.UID}] trocou o Whisper State para : {stateText}",
                    type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage(
                    $"[Handle_PLAYER_CHANGE_WHISPER_STATE][ErrorSystem] {e.getFullMessageError()}",
                    type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

        await Task.CompletedTask;
        }
    }
}