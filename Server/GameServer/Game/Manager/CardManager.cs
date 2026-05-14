using System;
using System.Collections.Generic;
using System.Linq;
using Pangya_GameServer.Models;
using PangyaAPI.Utilities.Models;

namespace Pangya_GameServer.Game.Manager
{
    public class CardManager : Dictionary<int, CardInfo>
    {
        public byte[] Build()
        {
            var p = new PangyaBinaryWriter();
            try
            {
                p.WriteUInt16((short)Count);
                p.WriteUInt16((short)Count);
                foreach (var item in Values)
                {
                    p.WriteBytes(item.ToArray());
                }
                return p.GetBytes;
            }
            catch (Exception)
            {
                return new byte[0];
            }
        }

        public byte[] GetInfo(int _id)
        {
            var char_info = findCardById(_id);
            if (char_info == null)
                return new byte[0x19];
            else
            {
                var p = new PangyaBinaryWriter();
                p.WriteBytes(char_info.ToArray());
                return p.GetBytes;
            }
        }
        public CardInfo findCardById(int _id)
        {
            return this.Values.FirstOrDefault(c => c.id == _id);
        }

        public CardInfo findCardByTypeid(uint _typeid)
        {
            return this.Values.FirstOrDefault(c => c._typeid == _typeid);
        }

        public CardInfo findCardByTypeidAndId(uint _typeid, int _id)
        {
            return this.Values.FirstOrDefault(c => c.id == _id && c._typeid == _typeid);
        }
    }
}
