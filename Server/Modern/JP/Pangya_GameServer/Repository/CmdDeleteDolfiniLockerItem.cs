using System;
using PangyaAPI.DataBase;
using PangyaAPI.Utilities;

namespace Pangya_GameServer.Repository
{
    public class CmdDeleteDolfiniLockerItem : Pangya_DB
    { 
        public CmdDeleteDolfiniLockerItem(uint _uid,
            long _index)
        {
            this.m_uid = _uid;
            this.m_index = _index;
        }
         

        protected override void lineResult(ctx_res _result, uint _index_result)
        {

            // N�o usa por que � um UPDATE
            return;
        }

        protected override Response prepareConsulta()
        {

            if (m_index <= 0)
            {
                throw new exception("[CmdDeleteDolfiniLockerItem][Error] Dolfini Locker Item[index=" + Convert.ToString(m_index) + "] is invalid", STDA_MAKE_ERROR(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            var r = procedure(m_szConsulta,
                Convert.ToString(m_uid) + ", " + Convert.ToString(m_index));

            checkResponse(r, "nao conseguiu deletar Dolfini Locker item[index=" + Convert.ToString(m_index) + "] do Normal[UID=" + Convert.ToString(m_uid) + "]");

            return r;
        }

        private uint m_uid = 0;
        private long m_index = 0;

        private const string m_szConsulta = "pangya.ProcMoveItemDolfiniLocker";
    }
}