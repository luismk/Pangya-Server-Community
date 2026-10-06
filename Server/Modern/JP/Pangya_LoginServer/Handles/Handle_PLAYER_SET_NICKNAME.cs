using Pangya_LoginServer.DataBase;

using Pangya_LoginServer.PangyaEnums;
using Pangya_LoginServer.Session;
using PangyaAPI.Network;
using PangyaAPI.Network.Core;
using PangyaAPI.Network.Models;
using PangyaAPI.Utilities.Log;
using System;

namespace Pangya_LoginServer.Handles
{
    public class Handle_PLAYER_SET_NICKNAME : HandleBase<Player, Packet_EXAMPLE>
    {
        /// <summary>
        /// Handler para definir o Nickname do jogador (0x06)
        /// </summary>
        public override async Task Handle()
        {
            string wnick = "";

            try
            {
                wnick = Packet.ReadString();
                uint uid = Player.UserInfo.UID;

                // 1. Persistência no Banco (Async)
                // Salva o nick escolhido e marca como primeiro login realizado
                CommandDB.SaveNick(uid, wnick);
                CommandDB.AddFirstLogin(uid, 1);

                // Atualiza o NickName na sessão atual para evitar dessincronização
                Player.UserInfo.NickName = wnick;

                // 2. Fluxo de Direcionamento (Regra de Negócio Pangya)
                // Verifica se o jogador já possui o primeiro Set de Personagem (Hana/Nuri)
                var hasFirstSet = CommandDB.IsFirstSet(uid);

                if (!hasFirstSet)
                {
                    // Se não tem personagem, envia pacote 0xD9 (Seleção Inicial)
                    Player.Send(Handle_PACKET_RESPONSE.pacote001(Player, 0xD9));
                }
                else
                {
                    // Se já está tudo pronto, finaliza o processo de login com sucesso
                    // Aqui você chama seu método global de Login Success
                    await Handle_PLAYER_LOGIN.SUCCESS_LOGIN(Player);
                }
            }
            catch (Exception e)
            {
                // Em caso de erro grave, envia o pacote 0x0E com status de erro (geralmente 1 ou conforme seu Enum)
                Player.Send(Handle_PACKET_RESPONSE.pacote00E(Player, wnick, (int)NICK_CHECK.UNKNOWN_ERROR, 0));

                _smp.LogManager.Instance.push(new AppMessage(
                    $"[Handle_PLAYER_SET_NICKNAME] Erro ao definir nick '{wnick}' para UID {Player.UserInfo.UID}: {e.Message}",
                    type_msg.CL_ONLY_CONSOLE)
                );
            }
            await Task.CompletedTask;
        }
    }
}