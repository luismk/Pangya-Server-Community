using System;
using Pangya_GameServer.Models;
using PangyaAPI.SQL;
namespace Pangya_GameServer.Repository
{
    public class CmdUserEquip : Pangya_DB
    {
        readonly uint m_uid = uint.MaxValue;
        UserEquip m_ue = new UserEquip();
        public CmdUserEquip(uint _uid)
        {
            m_uid = _uid;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {
            checkColumnNumber(29);
            try
            {
                var i = 0;

                m_ue.caddie_id = Convert.ToInt32(_result.data[0]);
                m_ue.character_id = Convert.ToInt32(_result.data[1]);
                m_ue.clubset_id = Convert.ToInt32(_result.data[2]);
                m_ue.ball_typeid = Convert.ToUInt32(_result.data[3]);
                for (i = 0; i < 10; i++)
                    m_ue.item_slot[i] = Convert.ToUInt32(_result.data[4 + i]);     // 4 + 10
                for (i = 0; i < 6; i++)
                    m_ue.skin_id[i] = Convert.ToUInt32(_result.data[14 + i]);      // 14 + 6
                for (i = 0; i < 6; i++)
                    m_ue.skin_typeid[i] = Convert.ToUInt32(_result.data[20 + i]);  // 20 + 6
                m_ue.mascot_id = Convert.ToInt32(_result.data[26]);
                for (i = 0; i < 2; i++)
                    m_ue.poster[i] = Convert.ToUInt32(_result.data[27 + i]);		// 27 + 2
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        protected override Response prepareConsulta()
        {
            var r = procedure("pangya.USP_CHAR_USER_EQUIP", m_uid.ToString());
            checkResponse(r, "nao conseguiu pegar o member info do player: " + (m_uid));
            return r;
        }


        public UserEquip getInfo()
        {
            return m_ue;
        }

    }
}
