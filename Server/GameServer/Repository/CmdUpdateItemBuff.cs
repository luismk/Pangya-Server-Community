using System;
using Pangya_GameServer.Models;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;

namespace Pangya_GameServer.Repository
{
    public class CmdUpdateItemBuff : Pangya_DB
    {
        public CmdUpdateItemBuff()
        {
            this.m_uid = 0;
        }

        public CmdUpdateItemBuff(uint _uid,
            ItemBuffEx _ib)
        {

            this.m_uid = _uid;
            //this.
            this.m_ib = (_ib);
        }

        public uint getUID()
        {
            return (m_uid);
        }

        public void setUID(uint _uid)
        {

            m_uid = _uid;

        }

        public ItemBuffEx getInfo()
        {
            // 
            return m_ib;
        }

        public void setInfo(ItemBuffEx _ib)
        {

            m_ib = _ib;
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
                throw new exception("[CmdUpdateItemBuff::prepareConsulta][Error] m_uid is invalid(zero)", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            if (m_ib.index <= 0 || m_ib._typeid == 0)
            {
                throw new exception("[CmdUpdateItemBuff::prepareConsulta][Error] m_ib[index=" + Convert.ToString(m_ib.index) + ", TYPEID=" + Convert.ToString(m_ib._typeid) + "] is invalid", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            var r = procedure(m_szConsulta,
                Convert.ToString(m_uid) + ", " + Convert.ToString(m_ib.index) + ", " + Convert.ToString(m_ib._typeid) + ", " + Convert.ToString(m_ib.tipo) + ", " + makeText(_formatDate(m_ib.end_date.ConvertTime())));

            checkResponse(r, "nao conseguiu atualizar o tempo do item buff[INDEX=" + Convert.ToString(m_ib.index) + ", TYPEID=" + Convert.ToString(m_ib._typeid) + ", TIPO=" + Convert.ToString(m_ib.tipo) + ", DATE{REG_DT: " + _formatDate(m_ib.use_date.ConvertTime()) + ", END_DT: " + _formatDate(m_ib.end_date.ConvertTime()) + "}] do PLAYER[UID=" + Convert.ToString(m_uid) + "]");

            return r;
        }

        private uint m_uid = new uint();
        private ItemBuffEx m_ib = new ItemBuffEx();

        private const string m_szConsulta = "pangya.ProcUpdateItemBuffTime";
    }
}
