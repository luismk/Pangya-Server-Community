using Pangya_GameServer.Models;
using PangyaAPI.IFF.Handle.JP;
using PangyaAPI.IFF.Flags;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Models;
using System.Collections.Generic;
using System.Linq;
using PangyaAPI.Network;
using Pangya_GameServer.PacketFunc;
namespace Pangya_GameServer.Manager
{
    public class ItemWarehouseManager : Dictionary<int/*ID*/, WarehouseItemEx>
    {
        public ItemWarehouseManager()
        {
        }

        public List<Packet> Build()
        { 
            const int CHUNK = 100;

            var responses = new List<Packet>();
            var list = Values.ToList();

            ushort total = (ushort)list.Count;
            int index = 0;

            while (total > CHUNK)
            { 
                var packet = Handle_PACKET_RESPONSE.pacote073(list.Skip(index).Take(CHUNK).ToList(), total, CHUNK);

                responses.Add(packet);

                index += CHUNK;
                total -= CHUNK;
            }

            // Resto
            if (total > 0)
            {
                var packet = Handle_PACKET_RESPONSE.pacote073(list.Skip(index).Take(total).ToList(), total, total);

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
            if (sIff.Instance.getItemGroupIdentify((_typeid)) == IFF_GROUP.ITEM && sIff.Instance.getItemSubGroupIdentify24((_typeid)) > 1/*Passive Item*/)
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
            if (sIff.Instance.getItemGroupIdentify((_typeid)) == IFF_GROUP.ITEM && sIff.Instance.getItemSubGroupIdentify24((_typeid)) > 1/*Passive Item*/)
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
