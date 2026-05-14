using System;
using Pangya_GameServer.Models;
using PangyaAPI.IFF.JP.Extensions;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;

namespace Pangya_GameServer.Repository
{
    internal class CmdAddDolfiniLockerItem : Pangya_DB
    {
        public CmdAddDolfiniLockerItem()
        {
            this.m_uid = 0;
            this.m_dli = new DolfiniLockerItem();
        }

        public CmdAddDolfiniLockerItem(uint _uid,
            DolfiniLockerItem _dli)
        {
            this.m_uid = _uid;
            this.m_dli = (_dli);
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

        public DolfiniLockerItem getInfo()
        {
            return m_dli;
        }

        public void setInfo(DolfiniLockerItem _dli)
        {
            m_dli = _dli;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {

            checkColumnNumber(1);

            m_dli.index = IFNULL(_result.data[0]);
        }

        protected override Response prepareConsulta()
        {

            if (m_dli.item.id <= 0
            || m_dli.item._typeid == 0
                || sIff.getInstance().getItemGroupIdentify(m_dli.item._typeid) != PangyaAPI.IFF.JP.Models.Flags.IFF_GROUP.PART)
            {
                throw new exception("[CmdAddDolfiniLockerItem][Error] PLAYER[UID=" + Convert.ToString(m_uid) + "] -> Item[TYPEID=" + Convert.ToString(m_dli.item._typeid) + ", ID=" + Convert.ToString(m_dli.item.id) + "] invalid for put in Dolfini Locker", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            m_dli.index = ~0Ul;

            var r = procedure(m_szConsulta,
                Convert.ToString(m_uid) + ", " + Convert.ToString(m_dli.item.id));

            checkResponse(r, "nao conseguiu colocar o item[TYPEID=" + Convert.ToString(m_dli.item._typeid) + ", ID=" + Convert.ToString(m_dli.item.id) + "] no Dolfini Locker do PLAYER[UID=" + Convert.ToString(m_uid) + "]");

            return r;
        }


        private uint m_uid = new uint();
        private DolfiniLockerItem m_dli = new DolfiniLockerItem();

        private const string m_szConsulta = "pangya.ProcAddItemDolfiniLocker";
    }
}