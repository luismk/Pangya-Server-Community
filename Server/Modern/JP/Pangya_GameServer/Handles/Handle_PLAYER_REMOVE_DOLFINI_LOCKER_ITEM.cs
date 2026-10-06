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
    public class Handle_PLAYER_REMOVE_DOLFINI_LOCKER_ITEM : HandleBase<Player, Packet_EXAMPLE>
    {
        public override async Task Handle()
        {
            Packet p = new Packet();
            DolfiniLockerItem[] aTI = null;
            WarehouseItemEx[] aWi = null;

            try
            {
                // 1. Leitura do número de itens a retirar
                byte count = Packet.ReadByte();

                if (count == 0)
                {
                    throw new exception("[Handle_PLAYER_REMOVE_DOLFINI_LOCKER_ITEM][Error] Count é 0, Normal [UID=" + Player.UserInfo.UID + "]",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 503, 5100404));
                }

                aTI = new DolfiniLockerItem[count];
                aWi = new WarehouseItemEx[count];

                // 2. Processamento dos itens
                for (int index = 0; index < count; index++)
                {
                    aTI[index] = new DolfiniLockerItem().ToRead(Packet);

                    // Busca o item na memória do Dolfini Locker
                    var item_in_locker = Player.Inventory.DolfineLocker.v_item.FirstOrDefault(item => item.item.id == aTI[index].item.id);

                    if (item_in_locker == null)
                    {
                        throw new exception("[Handle_PLAYER_REMOVE_DOLFINI_LOCKER_ITEM][Error] Normal [UID=" + Player.UserInfo.UID + "] tentou tirar um item que não possui no Locker. [ID=" + aTI[index].item.id + "]",
                            ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 550, 5100451));
                    }

                    // Remove do Banco de Dados
                    NormalManagerDB.Instance.add(4,
                         new CmdDeleteDolfiniLockerItem(Player.UserInfo.UID, aTI[index].index));

                    // Remove da lista em memória do Locker
                    Player.Inventory.DolfineLocker.v_item.Remove(item_in_locker);

                    // Prepara o item para voltar ao Warehouse (Inventário)
                    aWi[index] = new WarehouseItemEx();
                    aWi[index].clear();
                    aWi[index].id = aTI[index].item.id;
                    aWi[index]._typeid = aTI[index].item._typeid;
                    aWi[index].ano = -1;
                    aWi[index].STDA_C_ITEM_QNTD = 1;
                    aWi[index].purchase = 1;
                    aWi[index].type = 2; // Default para itens retirados
                    aWi[index].clubset_workshop.level = -1;

                    // Mapeamento de UCC (caso o item seja customizado)
                    aWi[index].ucc.name = aTI[index].item.sd_name;
                    aWi[index].ucc.idx = aTI[index].item.sd_idx;
                    aWi[index].ucc.copier_nick = aTI[index].item.sd_copier_nick;
                    aWi[index].ucc.seq = aTI[index].item.sd_seq;
                    aWi[index].ucc.status = (byte)aTI[index].item.sd_status;

                    // Adiciona de volta ao inventário principal do Player
                    Player.Inventory.WarehouseItems.Add(aWi[index].id, aWi[index]);

                    _smp.LogManager.Instance.push(new AppMessage("[Dolfini Locker::RemoveItem][Success] Normal [UID=" + Player.UserInfo.UID + "] removeu o Item[TYPEID=" + aWi[index]._typeid + "] do Locker.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                }

                // 3. Sincronização: Pacote 0xEC (Atualização do Warehouse)
                p.init_plain(0xEC);
                p.WriteUInt32(count);
                p.WriteByte(0); // Sub-Type: Retirada de Locker
                p.WriteUInt64(Player.UserInfo.Statistics.pang);
                p.WriteUInt32(0); // Unknown padding

                for (int i = 0; i < count; ++i)
                {
                    p.WriteBytes(aTI[i].item.ToArray()); // 168 bytes do DolfiniItem
                    p.WriteByte(3); // ServerFlag de estado
                    p.WriteBytes(aWi[i].ToArray()); // Estrutura do Warehouse
                }
                Player.Send(p);

                // 4. Sincronização: Pacote 0x16F (Confirmação individual de retirada do Locker)
                for (int i = 0; i < count; ++i)
                {
                    p.init_plain(0x16F);
                    p.WriteUInt32(0); // Success Code
                    p.WriteInt64(aTI[i].index);
                    p.WriteBytes(aTI[i].item.ToArray());
                    Player.Send(p);
                }
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[Handle_PLAYER_REMOVE_DOLFINI_LOCKER_ITEM][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x16F);
                uint errorCode = (ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) == STDA_ERROR_TYPE.CHANNEL)
                    ? ExceptionError.STDA_SYSTEM_ERROR_DECODE(e.getCodeError())
                    : 5100450;

                p.WriteUInt32(errorCode);
                Player.Send(p);
            }
            finally
            {
                aTI = null;
                aWi = null;
            }
        } 
    }
}