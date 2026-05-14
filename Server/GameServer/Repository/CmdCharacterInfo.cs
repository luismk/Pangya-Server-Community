using System;
using System.Data;
using System.Diagnostics;
using System.Linq;
using Pangya_GameServer.Game.Manager;
using PangyaAPI.IFF.JP.Extensions;
using PangyaAPI.Network.Models;
using PangyaAPI.SQL;

namespace Pangya_GameServer.Repository
{
    public class CmdCharacterInfo : Pangya_DB
    {
        uint m_uid = uint.MaxValue;
        public enum TYPE : int
        {
            ALL,
            ONE,
        }
        TYPE m_type;
        int m_item_id;
        CharacterManager v_ce;

        public CmdCharacterInfo(uint _uid, TYPE _type, int _item_id = -1)
        {
            m_uid = _uid;
            m_type = _type;
            m_item_id = _item_id;
            v_ce = new CharacterManager();
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {
            checkColumnNumber(81);
            try
            {

                CharacterInfo ce = new CharacterInfo();
                var i = 0;

                ce.id = Convert.ToInt32(_result.data[0]);
                ce._typeid = Convert.ToUInt32(_result.data[1]);
                for (i = 0; i < 24; i++)
                    ce.parts_id[i] = Convert.ToUInt32(_result.data[2 + i]);        // 2 + 24 
                for (i = 0; i < 24; i++)
                   ce.parts_typeid[i] = Convert.ToUInt32(_result.data[26 + i]);        // 2 + 24 
                ce.default_hair = (byte)Convert.ToUInt32(_result.data[50]);
                ce.default_shirts = (byte)Convert.ToUInt32(_result.data[51]);
                ce.gift_flag = (byte)Convert.ToUInt32(_result.data[52]);
                for (i = 0; i < 5; i++)
                    ce.pcl[i] = (byte)Convert.ToUInt32(_result.data[53 + i]); // 53 + 5
                ce.purchase = (byte)Convert.ToUInt32(_result.data[58]);
                for (i = 0; i < 5; i++)
                {
                    var aux_part = Convert.ToUInt32(_result.data[59 + i]);				// 59 + 5
                    if (aux_part != 0)
                    {
                        ce.auxparts[i] = aux_part;
                    }
                }

                for (i = 0; i < 4; i++)
                {
                    var cut_in = Convert.ToUInt32(_result.data[64 + i]);				// 59 + 5
                    if (cut_in != 0)
                    {
                        ce.cut_in[i] = cut_in;
                    }
                }

                ce.mastery = Convert.ToUInt32(_result.data[68]);
                for (i = 0; i < 4; i++)
                {
                    var card = Convert.ToUInt32(_result.data[69 + i]);				// 59 + 5
                    if (card != 0)
                    {
                        ce.Card_Character[i] = card;
                    }
                }

                for (i = 0; i < 4; i++)
                {
                    var card = Convert.ToUInt32(_result.data[73 + i]);				// 59 + 5
                    if (card != 0)
                    {
                        ce.Card_Caddie[i] = card;
                    }
                }

                for (i = 0; i < 4; i++)
                {
                    var card = Convert.ToUInt32(_result.data[77 + i]);				// 59 + 5
                    if (card != 0)
                    {
                        ce.Card_NPC[i] = card;
                    }
                }

                var it = v_ce.Where(c => c.Key == ce.id);
                if (!it.Any())
                    v_ce.Add(ce.id, ce);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);

            }
        }

        protected override Response prepareConsulta()
        {

            var (procName, parameters) = (m_type == TYPE.ALL)
         ? ("pangya.USP_CHAR_EQUIP_LOAD_S4", m_uid.ToString())
         : ("pangya.USP_CHAR_EQUIP_LOAD_S4_ONE", m_uid + "," + m_item_id);

            var r = procedure(procName, parameters);

            checkResponse(r, "nao conseguiu pegar o member info do player: " + (m_uid));
            return r;
        }


        public CharacterManager getAllInfo()
        {
            return v_ce;
        }

        public CharacterInfo getInfo()
        {
            return v_ce.First().Value;
        }


        public uint getUID()
        {
            return m_uid;
        }

        public void setUID(uint _uid)
        {
            m_uid = _uid;
        }

        public TYPE getType()
        {
            return m_type;
        }

        public void setType(TYPE _type)
        {
            m_type = _type;
        }

        public int getItemID()
        {
            return m_item_id;
        }

        public void setItemID(int _item_id)
        {
            m_item_id = _item_id;
        }
    }
}
