using System;
using System.Collections.Generic;
using System.Linq;
using Pangya_GameServer.Models;
using PangyaAPI.Utilities.Models;

namespace Pangya_GameServer.Game.Manager
{
    public class MascotManager : Dictionary<int/*ID*/, MascotInfoEx>
    {
        public MascotManager()
        {

        }

        public MascotManager(Dictionary<int/*ID*/, MascotInfoEx> keys)
        {
            // this.(keys);    add array 
        }

        public byte[] Build()
        {
            var p = new PangyaBinaryWriter();
            try
            {
                p.WriteByte((byte)(Count & 0xFF));

                foreach (var item in Values)
                    if (item.PCBang == 0)//nao inclui ele de novo
                        p.WriteBytes(item.ToArray());

                return p.GetBytes;
            }
            catch (Exception)
            {
                return new byte[0];
            }
        }

        public MascotInfoEx findMascotById(int _id)
        {
            return this.Values.FirstOrDefault(c => c.id == _id);
        }

        public MascotInfoEx findMascotByTypeid(uint _typeid)
        {
            return this.Values.FirstOrDefault(c => c._typeid == _typeid);
        }

        public MascotInfoEx findMascotByTypeidAndId(uint _typeid, int _id)
        {
            return this.Values.FirstOrDefault(c => c.id == _id && c._typeid == _typeid);
        }
    }
}
