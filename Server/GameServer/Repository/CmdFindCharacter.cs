//Convertion By LuisMK
using System;
using PangyaAPI.IFF.JP.Extensions;
using PangyaAPI.Network.Models;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;

namespace Pangya_GameServer.Repository
{
    public class CmdFindCharacter : Pangya_DB
    {
        public CmdFindCharacter(bool _waiter = false) : base(_waiter)
        {
            this.m_uid = 0;
            this.m_typeid = 0;
            this.m_ci = new CharacterInfo();
        }

        public CmdFindCharacter(uint _uid,
            uint _typeid,
            bool _waiter = false) : base(_waiter)
        {
            this.m_uid = _uid;
            this.m_typeid = _typeid;
            this.m_ci = new CharacterInfo();
        }

        public uint getUID()
        {
            return (m_uid);
        }

        public void setUID(uint _uid)
        {
            m_uid = _uid;
        }

        public uint getTypeid()
        {
            return (m_typeid);
        }

        public void setTypeid(uint _typeid)
        {
            m_typeid = _typeid;
        }

        public CharacterInfo getInfo()
        {
            return (m_ci);
        }

        public bool hasFound()
        {
            return m_ci.id > 0;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {

            checkColumnNumber(81, (uint)_result.cols);

            m_ci.id = IFNULL<int>(_result.data[0]);

            if (m_ci.id > 0)
            { // found
                var i = 0;

                m_ci._typeid = IFNULL(_result.data[1]);
                for (i = 0; i < 24; i++)
                {
                    m_ci.parts_id[i] = IFNULL(_result.data[2 + i]); // 2 + 24
                }
                for (i = 0; i < 24; i++)
                {
                    m_ci.parts_typeid[i] = IFNULL(_result.data[26 + i]); // 26 + 24
                }
                m_ci.default_hair = (byte)IFNULL(_result.data[50]);
                m_ci.default_shirts = (byte)IFNULL(_result.data[51]);
                m_ci.gift_flag = (byte)IFNULL(_result.data[52]);
                for (i = 0; i < 5; i++)
                {
                    m_ci.pcl[i] = (byte)IFNULL(_result.data[53 + i]); // 53 + 5
                }
                m_ci.purchase = (byte)IFNULL(_result.data[58]);
                for (i = 0; i < 5; i++)
                {
                    m_ci.auxparts[i] = IFNULL(_result.data[59 + i]); // 59 + 5
                }
                for (i = 0; i < 4; i++)
                {
                    m_ci.cut_in[i] = IFNULL(_result.data[64 + i]); // 64 + 4 Cut-in deveria guarda no db os outros 3 se for msm os 4 que penso q seja, � sim no JP USA os 4
                }
                m_ci.mastery = IFNULL(_result.data[68]);
                for (i = 0; i < 4; i++)
                {
                    m_ci.Card_Character[i] = IFNULL(_result.data[69 + i]); // 69 + 4
                }
                for (i = 0; i < 4; i++)
                {
                    m_ci.Card_Caddie[i] = IFNULL(_result.data[73 + i]); // 73 + 4
                }
                for (i = 0; i < 4; i++)
                {
                    m_ci.Card_NPC[i] = IFNULL(_result.data[77 + i]); // 77 + 4
                }
            }
        }

        protected override Response prepareConsulta()
        {

            if (m_typeid == 0 && sIff.getInstance().getItemGroupIdentify(m_typeid) != PangyaAPI.IFF.JP.Models.Flags.IFF_GROUP.CHARACTER)
            {
                throw new exception("[CmdFindCharacter::prepareConsulta][Error] typeid character invalid", STDA_MAKE_ERROR(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            m_ci.clear();
            m_ci.id = -1;

            var r = procedure(m_szConsulta,
                Convert.ToString(m_uid) + ", " + Convert.ToString(m_typeid));

            checkResponse(r, "erro ao encontrar o character[UID=" + Convert.ToString(m_typeid) + "] do PLAYER[UID=" + Convert.ToString(m_uid) + "]");

            return r;
        }


        private uint m_uid = new uint();
        private uint m_typeid = new uint();
        private CharacterInfo m_ci = new CharacterInfo();

        private const string m_szConsulta = "pangya.ProcFindCharacter";
    }
}