using Pangya_GameServer.Manager;
using Pangya_GameServer.Models;
using Pangya_GameServer.PacketFunc;
using Pangya_GameServer.Repository;
using Pangya_GameServer.Session;
using PangyaAPI.IFF.Flags;
using PangyaAPI.IFF.Handle.JP;
using PangyaAPI.Network;
using PangyaAPI.Network.Core;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using snmdb;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Pangya_GameServer.Handles
{
    public class Handle_PLAYER_MAKE_PASS_DOLFINI_LOCKER : HandleBase<Player, Packet_EXAMPLE>
    {
        public override async Task Handle()
        {
            Packet p = new Packet();

            try
            {
                // 1. Leitura e Sanitização
                string pass = Packet.ReadString();

                if (string.IsNullOrEmpty(pass))
                {
                    throw new exception("[Handle_PLAYER_MAKE_PASS_DOLFINI_LOCKER][Error] Normal[UID=" + Player.UserInfo.UID + "] tentou contra o server[MESSAGE=" + pass + "], vazio. Hacker ou Bug",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 1, 1));
                }

                if (!Tools.Sanitize(pass))
                {
                    throw new exception("[Handle_PLAYER_MAKE_PASS_DOLFINI_LOCKER][Error] Normal[UID=" + Player.UserInfo.UID + "] tentou contra o server[MESSAGE=" + pass + "], tentativa de inject. Hacker ou Bug",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 1, 1));
                }

                // 2. Validação de Regras de Negócio (Tamanho da Password)
                if (pass.Length == 0)
                {
                    throw new exception("[Handle_PLAYER_MAKE_PASS_DOLFINI_LOCKER][Error] Normal [UID=" + Player.UserInfo.UID + "] tentrou trocar a Password do dolfini locker com Password vazia. Hacker ou Bug",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 200, 5100101));
                }

                if (pass.Length > 4)
                {
                    throw new exception("[Handle_PLAYER_MAKE_PASS_DOLFINI_LOCKER][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou trocar a Password do dolfini locker com uma Password maior do que o permitido. Hacker ou Bug",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 201, 5100102));
                }

                // 3. Atualização na Sessão
                Player.Inventory.DolfineLocker.pass = pass;

                // 4. Resposta de Sucesso (Pacote 0x176)
                p.init_plain(0x176);
                p.WriteUInt32(0);
                Player.Send(p);

                // 5. Persistência Assíncrona no DB
                NormalManagerDB.Instance.add(1,new CmdUpdateDolfiniLockerPass(Player.UserInfo.UID, pass));
            }
            catch (exception e)
            {
                // Log de Erro
                _smp.LogManager.Instance.push(new AppMessage("[Handle_PLAYER_MAKE_PASS_DOLFINI_LOCKER][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x176);

                // Tratamento de erro padrão
                uint errorCode = (ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) == STDA_ERROR_TYPE.CHANNEL)
                    ? ExceptionError.STDA_SYSTEM_ERROR_DECODE(e.getCodeError())
                    : 5100100;

                p.WriteUInt32(errorCode);
                Player.Send(p);
            }
        } 
    }
}