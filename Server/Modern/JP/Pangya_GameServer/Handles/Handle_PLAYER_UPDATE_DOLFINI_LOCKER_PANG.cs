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
    public class Handle_PLAYER_UPDATE_DOLFINI_LOCKER_PANG : HandleBase<Player, Packet_EXAMPLE>
    {
        public override async Task Handle()
        {
            Packet p = new Packet();

            try
            {
                // 1. Leitura dos dados: Opção (0 = Retirar, 1 = Depositar) e Quantidade
                byte opt = Packet.ReadByte();
                ulong pang = Packet.ReadUInt64();

                if (opt == 1) // DEPÓSITO: Player -> Locker
                {
                    if (pang > Player.UserInfo.Statistics.pang)
                    {
                        throw new exception("[Handle_PLAYER_UPDATE_DOLFINI_LOCKER_PANG][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou depositar pangs[" + pang + "] que não possui.",
                            ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 451, 5100352));
                    }

                    Player.Inventory.DolfineLocker.pang += pang; // Adiciona no cofre
                   Player.UserInfo.consomePang(pang); // Remove do inventário
                }
                else if (opt == 0) // RETIRADA: Locker -> Player
                {
                    if (pang > Player.Inventory.DolfineLocker.pang)
                    {
                        throw new exception("[Handle_PLAYER_UPDATE_DOLFINI_LOCKER_PANG][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou retirar pangs[" + pang + "] que não estão no Dolfini Locker.",
                            ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 452, 5100353));
                    }

                    Player.Inventory.DolfineLocker.pang -= pang; // Remove do cofre
                   Player.UserInfo.addPang(pang); // Adiciona no inventário
                }
                else
                {
                    throw new exception("[Handle_PLAYER_UPDATE_DOLFINI_LOCKER_PANG][Error] Normal [UID=" + Player.UserInfo.UID + "] enviou opção inválida[" + opt + "].",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 450, 5100351));
                }

                _smp.LogManager.Instance.push(new AppMessage("[Dolfini Locker::Update Pang][Success] Normal [UID=" + Player.UserInfo.UID + "] Atualizou Pang[value=" + pang + ", OPT=" + opt + "].", type_msg.CL_FILE_LOG_AND_CONSOLE));

                // 2. Atualização persistente no Banco de Dados
                NormalManagerDB.Instance.add(3,
                     new CmdUpdateDolfiniLockerPang(Player.UserInfo.UID, Player.Inventory.DolfineLocker.pang));

                // 3. Sincronização de Pacotes com o Cliente

                // Pacote 0x171: Confirmação da operação
                p.init_plain(0x171);
                p.WriteUInt32(0);
                Player.Send(p);

                // Pacote 0xC8: Atualização visual dos Pangs no inventário principal
                p.init_plain(0xC8);
                p.WriteUInt64(Player.UserInfo.Statistics.pang);
                p.WriteUInt64(pang);
                Player.Send(p);

                // Pacote 0x172: Atualização visual dos Pangs dentro do Dolfini Locker
                p.init_plain(0x172);
                p.WriteUInt64(Player.Inventory.DolfineLocker.pang);
                Player.Send(p);
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[Handle_PLAYER_UPDATE_DOLFINI_LOCKER_PANG][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x171);
                uint errorCode = (ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) == STDA_ERROR_TYPE.CHANNEL)
                    ? ExceptionError.STDA_SYSTEM_ERROR_DECODE(e.getCodeError())
                    : 5100350;

                p.WriteUInt32(errorCode);
                Player.Send(p);
            }
        }
    }
}