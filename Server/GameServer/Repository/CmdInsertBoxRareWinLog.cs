using System;
using Pangya_GameServer.Models;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;

namespace Pangya_GameServer.Repository
{
    public class CmdInsertBoxRareWinLog : Pangya_DB
    {
        public CmdInsertBoxRareWinLog(bool _waiter = false) : base(_waiter)
        {
            this.m_uid = 0;
            this.m_box_typeid = 0;
            this.m_ctx_bi = new ctx_box_item();
        }

        public CmdInsertBoxRareWinLog(uint _uid,
            uint _box_typeid,
            ctx_box_item _ctx_bi,
            bool _waiter = false) : base(_waiter)
        {
            this.m_uid = _uid;
            this.m_box_typeid = _box_typeid;
            this.m_ctx_bi = (_ctx_bi);
        }
        public uint getUID()
        {
            return (m_uid);
        }

        public void setUID(uint _uid)
        {
            m_uid = _uid;
        }

        public uint getBoxTypeid()
        {
            return (m_box_typeid);
        }

        public void setBoxTypeid(uint _box_typeid)
        {
            m_box_typeid = _box_typeid;

        }

        public ctx_box_item getInfo()
        {
            return m_ctx_bi;
        }

        public void setInfo(ctx_box_item _ctx_bi)
        {
            m_ctx_bi = _ctx_bi;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {


            // N�o usa por que � um INSERT
            return;
        }

        protected override Response prepareConsulta()
        {

            if (m_uid == 0)
            {
                throw new exception("[CmdInsertBoxRareWinLog::prepareConsulta][Error] m_uid is invalid(zero)", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            if (m_box_typeid == 0)
            {
                throw new exception("[CmdInsertBoxRareWinLog::prepareConsulta][Error] m_box_typeid is invalid(zero)", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            if (m_ctx_bi._typeid == 0)
            {
                throw new exception("[CmdInsertBoxRareWinLog::prepareConsulta][Error] m_ctx_bi._typeid is invalid(zero)", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            var r = procedure(m_szConsulta,
                Convert.ToString(m_uid) + ", " + Convert.ToString(m_box_typeid) + ", " + Convert.ToString(m_ctx_bi._typeid) + ", " + Convert.ToString(m_ctx_bi.qntd) + ", " + Convert.ToString((ushort)m_ctx_bi.raridade));

            checkResponse(r, "nao conseguiu inserir o box[TYPEID=" + Convert.ToString(m_box_typeid) + "] rare[TYPEID=" + Convert.ToString(m_ctx_bi._typeid) + ", QNTD=" + Convert.ToString(m_ctx_bi.qntd) + ", RARIDADE=" + Convert.ToString((ushort)m_ctx_bi.raridade) + "] win log para o PLAYER[UID=" + Convert.ToString(m_uid) + "]");

            return r;
        }

        private uint m_uid = new uint();
        private uint m_box_typeid = new uint();
        private ctx_box_item m_ctx_bi = new ctx_box_item();

        private const string m_szConsulta = "pangya.ProcInsertBoxRareWinLog";
    }
}