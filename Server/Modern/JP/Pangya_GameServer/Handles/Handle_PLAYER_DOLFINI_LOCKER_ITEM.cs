using System;
using System.Threading.Tasks;
using Pangya_GameServer.Feature;
using Pangya_GameServer.Flags;
using Pangya_GameServer.Manager;
using Pangya_GameServer.Models;
using Pangya_GameServer.Repository;
using Pangya_GameServer.Server;
using Pangya_GameServer.Session;
using PangyaAPI.IFF.Handle.JP;
using PangyaAPI.IFF.Regions.JP.Models;
using PangyaAPI.Network;
using PangyaAPI.Network.Core;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using snmdb;
using System;
using System.Threading.Tasks;
using static Pangya_GameServer.Models.DefineConstants;
namespace Pangya_GameServer.Handles
{
    public class Handle_PLAYER_DOLFINI_LOCKER_ITEM : HandleBase<Player, Packet_EXAMPLE>
    { 
        public override async Task Handle()
        {
            // Criamos o pacote de resposta (0x16D)
            var p = new Packet(0x16D);

            try
            { 
                uint opt = Packet.ReadUInt32();
                ushort paginaSolicitada = Packet.ReadUInt16();

                var itemList = Player.Inventory.DolfineLocker.v_item;
                int totalItems = itemList.Count;
                 
                ushort totalPaginas = (ushort)((totalItems % DL_LIMIT_ITEM_PER_PAGE == 0) ? (ushort)totalItems / DL_LIMIT_ITEM_PER_PAGE : (ushort)totalItems / DL_LIMIT_ITEM_PER_PAGE + 1);

                if (totalItems > 0 && paginaSolicitada > totalPaginas)
                {
                    throw new exception($"[Handle_PLAYER_DOLFINI_LOCKER_ITEM] Pagina invalida: {paginaSolicitada}",
                        ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL, 400, 5100300));
                }

                int startIndex = (paginaSolicitada > 0) ? (paginaSolicitada - 1) * DL_LIMIT_ITEM_PER_PAGE : 0;
                byte countParaEnviar = (byte)Math.Min(totalItems - startIndex, DL_LIMIT_ITEM_PER_PAGE);

                // Escrita no Packet
                p.WriteUInt16(totalPaginas);
                p.WriteUInt16((totalItems > 0) ? paginaSolicitada : (ushort)0);
                p.WriteByte(countParaEnviar);

                for (int i = 0; i < countParaEnviar; i++)
                {
                    var itemLocker = itemList[startIndex + i];
                    p.WriteInt64(itemLocker.index);
                    p.WriteBytes(itemLocker.item.ToArray());
                }

                Player.Send(p);
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[Handle_PLAYER_DOLFINI_LOCKER_ITEM][Error] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Resposta de erro (vazia)
                var errorPkt = new Packet(0x16D);
                errorPkt.WriteZero(5);
                Player.Send(errorPkt);
            }

        await Task.CompletedTask;
        }
    }
}
