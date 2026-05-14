using System;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;

namespace Pangya_GameServer.Repository
{
    public class CmdDeleteDolfiniLockerItem : Pangya_DB
    {
        public CmdDeleteDolfiniLockerItem(bool _waiter = false) : base(_waiter)
        {
            this.m_uid = 0;
            this.m_index = 0Ul;
        }

        public CmdDeleteDolfiniLockerItem(uint _uid,
            ulong _index,
            bool _waiter = false) : base(_waiter)
        {
            this.m_uid = _uid;
            this.m_index = _index;
        }

        public uint getUID()
        {
            return (m_uid);
        }

        public void setUID(uint _uid)
        {
            m_uid = _uid;
        }

        public ulong getIndex()
        {
            return (m_index);
        }

        public void setIndex(ulong _index)
        {
            m_index = _index;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {

            // N�o usa por que � um UPDATE
            return;
        }

        protected override Response prepareConsulta()
        {

            if (m_index <= 0Ul)
            {
                throw new exception("[CmdDeleteDolfiniLockerItem][Error] Dolfini Locker Item[index=" + Convert.ToString(m_index) + "] is invalid", STDA_MAKE_ERROR(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            var r = procedure(m_szConsulta,
                Convert.ToString(m_uid) + ", " + Convert.ToString(m_index));

            checkResponse(r, "nao conseguiu deletar Dolfini Locker item[index=" + Convert.ToString(m_index) + "] do PLAYER[UID=" + Convert.ToString(m_uid) + "]");

            return r;
        }

        private uint m_uid = 0;
        private ulong m_index = 0;

        private const string m_szConsulta = "pangya.ProcMoveItemDolfiniLocker";
    }
}