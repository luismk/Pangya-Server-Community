using System.Collections.Generic;
using System.Linq;
using PangyaAPI.Network.Models;

namespace Pangya_GameServer.Game.Manager
{
    public class CharacterManager : Dictionary<int/*ID*/, CharacterInfo>
    {
        public CharacterManager()
        {

        }

        public CharacterManager(Dictionary<int/*ID*/, CharacterInfo> keys)
        {
            // this.(keys);    add array 
        }

        public CharacterInfo findCharacterById(int _id)
        {
            return this.Values.FirstOrDefault(c => c.id == _id);
        }

        public CharacterInfo findCharacterByTypeid(uint _typeid)
        {
            return this.Values.FirstOrDefault(c => c._typeid == _typeid);
        }

        public CharacterInfo findCharacterByTypeidAndId(uint _typeid, int _id)
        {
            return this.Values.FirstOrDefault(c => c.id == _id && c._typeid == _typeid);
        }
    }
}
