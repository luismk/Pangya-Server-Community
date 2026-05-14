//Convertion By LuisMK
using System;
using Pangya_GameServer.Models;
using PangyaAPI.IFF.JP.Extensions;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;

namespace Pangya_GameServer.Repository
{
    public class CmdFindCaddie : Pangya_DB
    {

        public CmdFindCaddie(bool _waiter = false) : base(_waiter)
        {
            this.m_uid = 0;
            this.m_typeid = 0;
            this.m_ci = new CaddieInfoEx();
        }

        public CmdFindCaddie(uint _uid,
            uint _typeid,
            bool _waiter = false) : base(_waiter)
        {
            this.m_uid = _uid;
            this.m_typeid = _typeid;
            this.m_ci = new CaddieInfoEx();
        }

        public virtual void Dispose()
        {
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

        public bool hasFound()
        {
            return m_ci.id > 0;
        }

        public CaddieInfoEx getInfo()
        {
            return m_ci;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {

            checkColumnNumber(11, (uint)_result.cols);

            m_ci.id = IFNULL<int>(_result.data[0]);

            if (m_ci.id > 0)
            { // found

                // Begin of Initialization Caddie Info Structure
                m_ci._typeid = IFNULL(_result.data[2]);
                m_ci.parts_typeid = IFNULL(_result.data[3]);
                m_ci.level = (byte)IFNULL(_result.data[4]);
                m_ci.exp = IFNULL<int>(_result.data[5]);
                m_ci.rent_flag = (byte)IFNULL(_result.data[6]);

                //m_ci.end_date_unix = (short)IFNULL(_result->data[7]);  
                if (_result.IsNotNull(7))
                {
                    m_ci.end_date.CreateTime(_translateDate(_result.data[7]));
                }
                m_ci.purchase = (byte)IFNULL(_result.data[8]);


                if (_result.IsNotNull(9))
                {
                    m_ci.end_parts_date.CreateTime(_translateDate(_result.data[9]));
                }


                m_ci.check_end = (short)IFNULL(_result.data[10]);
                // End of Initialization Caddie Info Structure

            }
        }

        protected override Response prepareConsulta()
        {

            if (m_typeid == 0 || sIff.getInstance().getItemGroupIdentify(m_typeid) != PangyaAPI.IFF.JP.Models.Flags.IFF_GROUP.CADDIE)
            {
                throw new exception("[CmdFindCaddie::prepareConsulta][Error] _typeid caddie is invalid", STDA_MAKE_ERROR(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            m_ci.clear();
            m_ci.id = -1;

            var r = procedure(m_szConsulta,
                Convert.ToString(m_uid) + ", " + Convert.ToString(m_typeid));

            checkResponse(r, "nao conseguiu encontrar o caddie[TYPEID=" + Convert.ToString(m_typeid) + "] para do PLAYER[UID=" + Convert.ToString(m_uid) + "]");

            return r;
        }

        private uint m_uid = new uint();
        private uint m_typeid = new uint();
        private CaddieInfoEx m_ci = new CaddieInfoEx();

        private const string m_szConsulta = "pangya.ProcFindCaddie";
    }
}