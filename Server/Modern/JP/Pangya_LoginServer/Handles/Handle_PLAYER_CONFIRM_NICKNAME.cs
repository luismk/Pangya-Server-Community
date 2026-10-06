using Pangya_LoginServer.DataBase;

using Pangya_LoginServer.PangyaEnums;
using Pangya_LoginServer.Session;
using PangyaAPI.Network;
using PangyaAPI.Network.Core;
using PangyaAPI.Network.Models;
using PangyaAPI.Utilities.Log;
using System;
using System.Text.RegularExpressions;

namespace Pangya_LoginServer.Handles
{
    public class Handle_PLAYER_CONFIRM_NICKNAME : HandleBase<Player, Packet_EXAMPLE>
    {
        /// <summary>
        /// Handler para verificar a disponibilidade de um Nickname (0x07)
        /// </summary>
        public override async Task Handle()
        {
            string wnick = "";
            NICK_CHECK status = NICK_CHECK.SUCCESS;

            try
            {
                wnick = Packet.ReadString();

                // 1. Validações de Regra de Negócio
                status = ValidateNickname(wnick);

                // 2. Se as regras passaram, verifica no Banco de Dados
                if (status == NICK_CHECK.SUCCESS)
                {
                    var check = CommandDB.VerifyNick(wnick);
                    // Aqui usamos o CommandDB que você já tem
                    if (check)
                    {
                        status = NICK_CHECK.NICK_IN_USE;
                    }
                }
            }
            catch (Exception e)
            {
                status = NICK_CHECK.UNKNOWN_ERROR;
                _smp.LogManager.Instance.push(new AppMessage($"[Handle_PLAYER_CONFIRM_NICKNAME] Erro ao validar nick '{wnick}': {e.Message}", type_msg.CL_ONLY_CONSOLE));
            }
             
            Player.Send(Handle_PACKET_RESPONSE.pacote00E(Player, wnick, (int)status, 0));

            await Task.CompletedTask;
        }

        private NICK_CHECK ValidateNickname(string nick)
        {
            // Nick igual ao ID de login? Não pode.
            if (nick.Equals(Player.UserInfo.Login, StringComparison.OrdinalIgnoreCase))
                return NICK_CHECK.SAME_NICK_USED;

            // Contém espaços?
            if (nick.Contains(" "))
                return NICK_CHECK.EMPETY_ERROR;

            // Tamanho mínimo (Pangya padrão é 4)
            if (nick.Length < 4 || nick.Length > 16)
                return NICK_CHECK.INCORRECT_NICK;

            // Caracteres especiais proibidos
            if (Regex.IsMatch(nick, @"[\^\$\?,`´~|""@#¨'%*!\\\]]"))
                return NICK_CHECK.INCORRECT_NICK;

            // Proteção contra nomes de Staff (Se não for GM/ADM)
            if (Player.UserInfo.Capability < 4) // Capability < 4 geralmente não é Staff no seu sistema
            {
                if (Regex.IsMatch(nick, "(GM|ADM|MOD|ADMIN|STAFF)", RegexOptions.IgnoreCase))
                    return NICK_CHECK.HAVE_BAD_WORD;
            }

            return NICK_CHECK.SUCCESS;
        }
    }
}