using System;
using System.Collections.Generic;
using Pangya_GameServer.Models;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;

namespace Pangya_GameServer.Repository
{
    internal class CmdMyRoomItem : Pangya_DB
    {
        public enum TYPE : byte
        {
            ALL,
            ONE
        }

        public CmdMyRoomItem(uint _uid,
            TYPE _type,
            int _item_id = -1)
        {
            this.m_uid = _uid;
            this.m_type = (_type);
            this.m_item_id = _item_id;
            this.v_mri = new List<MyRoomItem>();
        }

        public List<MyRoomItem> getMyRoomItem()
        {
            return new List<MyRoomItem>(v_mri);
        }

        public uint getUID()
        {
            return m_uid;
        }

        public void setUID(uint _uid)
        {
            m_uid = _uid;
        }

        public int getItemID()
        {
            return m_item_id;
        }

        public void setItemID(int _item_id)
        {
            m_item_id = _item_id;
        }

        public CmdMyRoomItem.TYPE getType()
        {
            return m_type;
        }

        public void setType(TYPE _type)
        {
            m_type = _type;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {

            checkColumnNumber(9);

            try
            {
                MyRoomItem mri = new MyRoomItem();
                uint uid_req = 0;

                mri.id = IFNULL<int>(_result.data[0]);
                uid_req = IFNULL<uint>(_result.data[1]);
                mri._typeid = IFNULL<uint>(_result.data[2]);
                mri.number = IFNULL<short>(_result.data[3]);
                mri.location.x = IFNULL<float>(_result.data[4]);
                mri.location.y = IFNULL<float>(_result.data[5]);
                mri.location.z = IFNULL<float>(_result.data[6]);
                mri.location.r = IFNULL<float>(_result.data[7]);
                mri.equiped = IFNULL<byte>(_result.data[8]);

                v_mri.Add(mri);

                if (uid_req != m_uid)
                {
                    throw new exception("[CmdMyRoomItem::lineResult][Error] o m_uid do my room item requisitado do player e diferente. UID_req: " + Convert.ToString(uid_req) + " != " + Convert.ToString(m_uid));
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        protected override Response prepareConsulta()
        {

            v_mri.Clear();

            var r = procedure((m_type == (CmdMyRoomItem.TYPE)TYPE.ALL) ? m_szConsulta[0] : m_szConsulta[1],
                Convert.ToString(m_uid) + (m_type == TYPE.ONE ? ", " + Convert.ToString(m_item_id) : ""));

            checkResponse(r, "nao conseguiu pegar o(s) item(ns) do my room do player: " + Convert.ToString(m_uid));

            return r;
        }

        private uint m_uid = new uint();
        private int m_item_id = -1;
        private TYPE m_type;
        private List<MyRoomItem> v_mri = new List<MyRoomItem>();

        private string[] m_szConsulta = { "pangya.ProcGetRoom", "pangya.ProcGetMyRoom_One" };
    }
}