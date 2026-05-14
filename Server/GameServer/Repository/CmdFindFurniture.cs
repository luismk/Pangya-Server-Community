//Convertion By LuisMK
using System;
using Pangya_GameServer.Models;
using PangyaAPI.IFF.JP.Extensions;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;

namespace Pangya_GameServer.Repository
{
    public class CmdFindFurniture : Pangya_DB
    {
        public CmdFindFurniture(bool _waiter = false) : base(_waiter)
        {
            this.m_uid = 0;
            this.m_typeid = 0;
            this.m_mri = new MyRoomItem();
        }

        public CmdFindFurniture(uint _uid,
            uint _typeid,
            bool _waiter = false) : base(_waiter)
        {
            this.m_uid = _uid;
            this.m_typeid = _typeid;
            this.m_mri = new MyRoomItem();
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
            return m_mri.id > 0;
        }

        public MyRoomItem getInfo()
        {
            return m_mri;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {

            checkColumnNumber(9, (uint)_result.cols);

            m_mri.id = IFNULL<int>(_result.data[0]);

            if (m_mri.id > 0)
            { // found
              //uid_req = IFNULL(_result->data[1]);	ignora o uid retornado
                m_mri._typeid = IFNULL(_result.data[2]);
                m_mri.number = (short)IFNULL(_result.data[3]);
                m_mri.location.x = (float)IFNULL<float>(_result.data[4]);
                m_mri.location.y = (float)IFNULL<float>(_result.data[5]);
                m_mri.location.z = (float)IFNULL<float>(_result.data[6]);
                m_mri.location.r = (float)IFNULL<float>(_result.data[7]);
                m_mri.equiped = (byte)IFNULL(_result.data[8]);
            }
        }

        protected override Response prepareConsulta()
        {

            if (m_typeid == 0 || sIff.getInstance().getItemGroupIdentify(m_typeid) != PangyaAPI.IFF.JP.Models.Flags.IFF_GROUP.FURNITURE)
            {
                throw new exception("[CmdFindFurniture::prepareConsulta][Error] _typeid furniture is invalid", STDA_MAKE_ERROR(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            m_mri.id = -1;

            var r = procedure(m_szConsulta,
                Convert.ToString(m_uid) + ", " + Convert.ToString(m_typeid));

            checkResponse(r, "nao conseguiu encontrar o furniture[TYPEID=" + Convert.ToString(m_typeid) + "] do PLAYER[TYPEID=" + Convert.ToString(m_uid) + "]");

            return r;
        }

        private uint m_uid = new uint();
        private uint m_typeid = new uint();
        private MyRoomItem m_mri = new MyRoomItem();

        private const string m_szConsulta = "pangya.ProcFindFurniture";
    }
}