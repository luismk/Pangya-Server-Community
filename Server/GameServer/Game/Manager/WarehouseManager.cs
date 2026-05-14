using Pangya_GameServer.Models;
using Pangya_GameServer.PacketFunc;
using PangyaAPI.IFF.JP.Extensions;
using PangyaAPI.IFF.JP.Models.Flags;
using PangyaAPI.Network.PangyaSession;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Models;
using System.Collections.Generic;
using System.Linq;
namespace Pangya_GameServer.Game.Manager
{
    public class WarehouseManager : Dictionary<int/*ID*/, WarehouseItemEx>
    {
        public WarehouseManager()
        {
        }

        public List<PangyaBinaryWriter> Build()
        { 
            const int CHUNK = 100;

            var responses = new List<PangyaBinaryWriter>();
            var list = Values.ToList();

            ushort total = (ushort)list.Count;
            int index = 0;

            while (total > CHUNK)
            {
                var packet = packet_func.pacote073(
                    list.Skip(index).Take(CHUNK).ToList(),
                    total,          // total geral
                    CHUNK           // amount neste packet
                );

                responses.Add(packet);

                index += CHUNK;
                total -= CHUNK;
            }

            // Resto
            if (total > 0)
            {
                var packet = packet_func.pacote073(
                    list.Skip(index).Take(total).ToList(),
                    total,
                    total
                );

                responses.Add(packet);
            }

            return responses;
        }



        public WarehouseItemEx findWarehouseItemById(int _id)
        {
            TryGetValue(_id, out WarehouseItemEx item);
            if (item == null)
            {
                return this.Values.FirstOrDefault(c => c.id == _id);
            }

            return item;
        }

        public WarehouseItemEx findWarehouseItemByTypeid(uint _typeid)
        {
            if (sIff.getInstance().getItemGroupIdentify((_typeid)) == IFF_GROUP.ITEM && sIff.getInstance().getItemSubGroupIdentify24((_typeid)) > 1/*Passive Item*/)
            {
                return Values.Where(c => c._typeid == _typeid)
                                             .OrderByDescending(c => c.STDA_C_ITEM_QNTD)
                                             .FirstOrDefault();//pega sempre o que tem mais quantidade
            }
            else//
                return this.Values.FirstOrDefault(c => c._typeid == _typeid);
        }


        public WarehouseItemEx findWarehouseItemByTypeidAndId(uint _typeid, int _id)
        {
            if (sIff.getInstance().getItemGroupIdentify((_typeid)) == IFF_GROUP.ITEM && sIff.getInstance().getItemSubGroupIdentify24((_typeid)) > 1/*Passive Item*/)
            {
                return Values.Where(c => c.id == _id && c._typeid == _typeid)
                                             .OrderByDescending(c => c.STDA_C_ITEM_QNTD)
                                             .FirstOrDefault();//pega sempre o que tem mais quantidade
            }
            else//
                return this.Values.FirstOrDefault(c => c.id == _id && c._typeid == _typeid);
        }
    }
}
