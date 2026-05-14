//Convertion By LuisMK
using System;
using Pangya_GameServer.Models;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;

namespace Pangya_GameServer.Repository
{
    internal class CmdFindDolfiniLockerItem : Pangya_DB
    {
        public CmdFindDolfiniLockerItem(bool _waiter = false) : base(_waiter)
        {
            this.m_uid = 0;
            this.m_typeid = 0;
            this.m_dli = new DolfiniLockerItem();
        }

        public CmdFindDolfiniLockerItem(uint _uid,
            uint _typeid,
            bool _waiter = false) : base(_waiter)
        {

            this.m_uid = _uid;
            this.m_typeid = _typeid;
            this.m_dli = new DolfiniLockerItem();
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

        public DolfiniLockerItem getInfo()
        {
            return m_dli;
        }

        public bool hasFound()
        {
            return m_dli.index != ~0Ul && m_dli.item.id > 0;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {

            checkColumnNumber(10, (uint)_result.cols);

            m_dli.item.id = IFNULL<int>(_result.data[0]);

            if (m_dli.item.id <= -1)
            {
                m_dli.index = ~0Ul; // not found
            }
            else
            {

                int uid_req = 0;

                uid_req = IFNULL<int>(_result.data[1]);
                m_dli.item._typeid = IFNULL(_result.data[2]);
                if (is_valid_c_string(_result.data[3]))
                {
                    m_dli.item.sd_name = _result.GetString(3);
                }
                //strcpy_s(dli.item.sd_name, _result->data[3]);
                if (is_valid_c_string(_result.data[4]))
                {
                    m_dli.item.sd_idx = _result.GetString(4);
                }
                //strcpy_s(dli.item.sd_idx, _result->data[4]);
                m_dli.item.sd_seq = (short)((short)IFNULL(_result.data[5]));
                if (is_valid_c_string(_result.data[6]))
                {
                    m_dli.item.sd_copier_nick = _result.GetString(6);
                }
                //strcpy_s(dli.item.sd_copier_nick, _result->data[6]);
                m_dli.item.sd_status = (byte)IFNULL(_result.data[7]);
                m_dli.index = IFNULL(_result.data[8]);
                m_dli.item.qntd = IFNULL<int>(_result.data[9]); // DOLFINI_LOCKER_FLAG, mas é quantidade

                if (uid_req != m_uid)
                {
                    throw new exception("[CmdFindDolfiniLockerItem::lineResult][Error] O dolfini info requerido retornou um uid diferente. UID_req: " + Convert.ToString(m_uid) + " != " + Convert.ToString(uid_req), STDA_MAKE_ERROR(STDA_ERROR_TYPE.PANGYA_DB,
                        3, 0));
                }
            }
        }

        protected override Response prepareConsulta()
        {

            if (m_uid == 0u || m_typeid == 0u)
            {
                throw new exception("[CmdFindDolfiniLockerItem::prepareConsulta][Error] m_uid(" + Convert.ToString(m_uid) + ") ou o m_typeid(" + Convert.ToString(m_typeid) + ") is invalid.", STDA_MAKE_ERROR(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            m_dli.index = ~0Ul;
            m_dli.item.id = -1;

            var r = procedure(m_szConsulta,
                Convert.ToString(m_uid) + ", " + Convert.ToString(m_typeid));

            checkResponse(r, "nao conseguiu encontrar o DolfiniLockerItem[TYPEID=" + Convert.ToString(m_typeid) + "] do PLAYER[UID=" + Convert.ToString(m_uid) + "]");

            return r;
        }


        private uint m_uid = new uint();
        private uint m_typeid = new uint();
        private DolfiniLockerItem m_dli = new DolfiniLockerItem();

        private const string m_szConsulta = "pangya.ProcFindDolfiniLockerItem";
    }
}