//Convertion By LuisMK
using System;
using Pangya_GameServer.Models;
using PangyaAPI.IFF.JP.Extensions;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;

namespace Pangya_GameServer.Repository
{
    public class CmdFindMascot : Pangya_DB
    {
        public CmdFindMascot(bool _waiter = false) : base(_waiter)
        {
            this.m_uid = 0;
            this.m_typeid = 0;
            this.m_mi = new MascotInfoEx();
        }

        public CmdFindMascot(uint _uid,
            uint _typeid,
            bool _waiter = false) : base(_waiter)
        {
            this.m_uid = _uid;
            this.m_typeid = _typeid;
            this.m_mi = new MascotInfoEx();
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
            return m_mi.id > 0;
        }

        public MascotInfoEx getInfo()
        {
            return m_mi;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {

            checkColumnNumber(10, (uint)_result.cols);

            m_mi.id = IFNULL<int>(_result.data[0]);

            if (m_mi.id > 0)
            { // found
                m_mi._typeid = IFNULL(_result.data[2]);
                m_mi.level = (byte)IFNULL(_result.data[3]);
                m_mi.exp = IFNULL<int>(_result.data[4]);
                m_mi.flag = (byte)IFNULL(_result.data[5]);
                if (is_valid_c_string(_result.data[6]))
                {
                    m_mi.message = _result.GetString(6);
                }
                m_mi.tipo = (short)IFNULL(_result.data[7]);
                m_mi.is_cash = (byte)IFNULL(_result.data[8]);
                m_mi.data.CreateTime(_translateDate(_result.data[9]));
            }
        }

        protected override Response prepareConsulta()
        {

            if (m_typeid == 0 || sIff.getInstance().getItemGroupIdentify(m_typeid) != PangyaAPI.IFF.JP.Models.Flags.IFF_GROUP.MASCOT)
            {
                throw new exception("[CmdFindMascot::prepareConsulta][Error] _typeid mascot is invalid", STDA_MAKE_ERROR(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            m_mi.id = -1;

            var r = procedure(m_szConsulta,
                Convert.ToString(m_uid) + ", " + Convert.ToString(m_typeid));

            checkResponse(r, "nao conseguiu encontrar o mascot[TYPEID=" + Convert.ToString(m_typeid) + "] do PLAYER[UID=" + Convert.ToString(m_uid) + "]");

            return r;
        }
        private uint m_uid = new uint();
        private uint m_typeid = new uint();
        private MascotInfoEx m_mi = new MascotInfoEx();

        private const string m_szConsulta = "pangya.ProcFindMascot";
    }
}