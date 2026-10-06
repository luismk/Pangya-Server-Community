using Pangya_GameServer.Manager;
using Pangya_GameServer.Models;
using Pangya_GameServer.PacketFunc;
using Pangya_GameServer.Session;
using PangyaAPI.IFF.Flags;
using PangyaAPI.IFF.Handle.JP;
using PangyaAPI.Network;
using PangyaAPI.Network.Core;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Pangya_GameServer.Handles
{
    public class Handle_PLAYER_CHECK_DOLFINI_LOCKER_PASS : HandleBase<Player, Packet_EXAMPLE>
    {
        public override async Task Handle()
        {
            Packet p = new Packet();

            try
            {
                // 1. Leitura e Sanitização básica
                string pass = Packet.ReadString();

                if (string.IsNullOrEmpty(pass))
                {
                    throw new exception("[Handle_PLAYER_CHECK_DOLFINI_LOCKER_PASS][Error] Normal[UID=" + Player.UserInfo.UID + "] tentou contra o server[MESSAGE=" + pass + "], vazio. Hacker ou Bug",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 1, 1));
                }

                if (!Tools.Sanitize(pass))
                {
                    throw new exception("[Handle_PLAYER_CHECK_DOLFINI_LOCKER_PASS][Error] Normal[UID=" + Player.UserInfo.UID + "] tentou contra o server[MESSAGE=" + pass + "], tentativa de inject. Hacker ou Bug",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 1, 1));
                }

                // 2. Validações de comprimento (Business Logic)
                if (pass.Length == 0)
                {
                    throw new exception("[Handle_PLAYER_CHECK_DOLFINI_LOCKER_PASS][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou entrar no Dolfini Locker com uma Password vazia. Hacker ou Bug.",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 250, 5100151));
                }

                if (pass.Length > 4)
                {
                    throw new exception("[Handle_PLAYER_CHECK_DOLFINI_LOCKER_PASS][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou entrar no Dolfini Locker com uma Password maior que a suportada. Hacker ou Bug",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 251, 5100152));
                }

                // 3. Preparação da resposta (Pacote 0x16C)
                p.init_plain(0x16C);

                // Compara a Password enviada com a Password armazenada na sessão
                if (string.CompareOrdinal(pass, Player.Inventory.DolfineLocker.pass) != 0)
                {
                    _smp.LogManager.Instance.push(new AppMessage("[Handle_PLAYER_CHECK_DOLFINI_LOCKER_PASS][Success] Normal [UID=" + Player.UserInfo.UID + "] tentou entrar no Dolfini Locker com Password[value=" + pass + "] errada", type_msg.CL_FILE_LOG_AND_CONSOLE));

                    p.WriteUInt32(0x75); // Senha Incorreta
                }
                else
                {
                    _smp.LogManager.Instance.push(new AppMessage("[Handle_PLAYER_CHECK_DOLFINI_LOCKER_PASS][Success] Normal [UID=" + Player.UserInfo.UID + "] logou com sucesso no Dolfini Locker", type_msg.CL_FILE_LOG_AND_CONSOLE));

                    // Marca que o jogador passou na verificação de Password
                    Player.Inventory.DolfineLocker.pass_check = true;

                    p.WriteUInt32(0); // Senha Correta
                }

                Player.Send(p);
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[Handle_PLAYER_CHECK_DOLFINI_LOCKER_PASS][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x16C);

                uint errorCode = (ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) == STDA_ERROR_TYPE.CHANNEL)
                    ? ExceptionError.STDA_SYSTEM_ERROR_DECODE(e.getCodeError())
                    : 5100150;

                p.WriteUInt32(errorCode);
                Player.Send(p);
            }
        }
    }
}