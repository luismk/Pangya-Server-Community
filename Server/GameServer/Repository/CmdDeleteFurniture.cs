//Convertion By LuisMK
using System;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;

namespace Pangya_GameServer.Repository
{
    public class CmdDeleteFurniture : Pangya_DB
    {
        public CmdDeleteFurniture(bool _waiter = false) : base(_waiter)
        {
            this.m_uid = 0;
            this.m_id = 0;
        }

        public CmdDeleteFurniture(uint _uid,
            int _id,
            bool _waiter = false) : base(_waiter)
        {
            this.m_uid = _uid;
            this.m_id = _id;
        }

        public uint getUID()
        {
            return (m_uid);
        }

        public void setUID(uint _uid)
        {
            m_uid = _uid;
        }

        public int getID()
        {
            return (m_id);
        }

        public void setID(int _id)
        {
            m_id = _id;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {

            // N�o usa por que � um UPDATE
            return;
        }

        protected override Response prepareConsulta()
        {

            if (m_uid == 0)
            {
                throw new exception("[CmdDeleteFurniture::prepareConsulta][Error] m_uid is invalid(zero)", STDA_MAKE_ERROR(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            if (m_id <= 0)
            {
                throw new exception("[CmdDeleteFurniture::prepareConsulta][Error] m_id[value=" + Convert.ToString(m_id) + "] is invalid", STDA_MAKE_ERROR(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            var r = _update(m_szConsula[0] + Convert.ToString(m_uid) + m_szConsula[1] + Convert.ToString(m_id));

            checkResponse(r, "nao conseguiu deletat Furniture[ID=" + Convert.ToString(m_id) + "] do PLAYER[UID=" + Convert.ToString(m_uid) + "]");

            return r;
        }
        private uint m_uid = new uint();
        private int m_id = new int();

        private string[] m_szConsula = { "UPDATE pangya.td_room_data UPDATE SET valid = 0 WHERE UID = ", " AND MYROOM_ID = " };
    }
}