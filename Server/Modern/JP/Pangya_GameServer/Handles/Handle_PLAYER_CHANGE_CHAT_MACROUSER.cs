using Pangya_GameServer.Repository;
using Pangya_GameServer.Server;
using Pangya_GameServer.Session;
using PangyaAPI.Network;
using PangyaAPI.Network.Core;
using PangyaAPI.Network.Models;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using System;
using System.Threading.Tasks;

namespace Pangya_GameServer.Handles
{
    public class Handle_PLAYER_CHANGE_CHAT_MACRO : HandleBase<Player, Packet_EXAMPLE>
    {
        public override async Task Handle()
        {
            try
            {
 
                if (Packet.Size != 576)
                {
                    throw new exception(
                        $"[Handle_PLAYER_CHANGE_CHAT_MACRO][Error] Normal[UID={Player.UserInfo.UID}] Tamanho de pacote inválido: {Packet.Size}.",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 1, 1));
                }

                var cmu = new ChatMacroUser();
                bool detectedInjection = false;

                // Processamento das Macros (F1 até F9)
                for (int i = 0; i < 9; i++)
                {
                    // Lê a string com tamanho fixo de 64 bytes
                    var macro = Packet.ReadPStr(64);

                    if (macro == null)
                        throw new exception($"[Handle_PLAYER_CHANGE_CHAT_MACRO][Error] Macro {i} é nula.");

                    // Validação de Segurança (Prevenção de SQL Injection e caracteres ilegais)
                    if (!Tools.Sanitize(macro))
                    {
                        detectedInjection = true;
                        macro = "Pangya!"; // Fallback seguro
                    }

                    cmu.setMacro(i, macro);
                }

                // 4. Sincronização em Memória
                Player.UserInfo.ChatMacro = cmu;

                // 5. Persistência no Banco de Dados (Async) 
                CommandDB.UpdateMacroUser(Player.UserInfo.UID, Player.UserInfo.ChatMacro);

                // 6. Resposta de Segurança
                if (detectedInjection)
                {
                    _smp.LogManager.Instance.push(new AppMessage(
                        $"[Handle_PLAYER_CHANGE_CHAT_MACRO][Security] UID={Player.UserInfo.UID} tentou injeção de código. Desconectando.",
                        type_msg.CL_FILE_LOG_AND_CONSOLE));

                    GameServer.Instance.Disconnect(Player);
                }
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage(
                    $"[Handle_PLAYER_CHANGE_CHAT_MACRO][ErrorSystem] {e.getFullMessageError()}",
                    type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

        await Task.CompletedTask;
        }
    }
}