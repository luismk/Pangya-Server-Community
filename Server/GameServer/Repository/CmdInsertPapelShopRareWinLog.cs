using System;
using Pangya_GameServer.Models;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;
namespace Pangya_GameServer.Repository
{
    public class CmdInsertPapelShopRareWinLog : Pangya_DB
    {
        public CmdInsertPapelShopRareWinLog(uint _uid,
            ctx_papel_shop_ball _ctx_psb)
        {
            this.m_uid = _uid;
            this.m_ctx_psb = (_ctx_psb);
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

        public ctx_papel_shop_ball getInfo()
        {

            return m_ctx_psb;
        }

        public void setInfo(ctx_papel_shop_ball _ctx_psb)
        {

            m_ctx_psb = _ctx_psb;
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
                throw new exception("[CmdInsertPapelShopRareWinLog::prepareConsulta][Error] m_uid is invalid(zero)", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            if (m_ctx_psb.ctx_psi._typeid == 0)
            {
                throw new exception("[CmdInsertPapelShopRareWinLog::prepareConsulta][Error] m_ctx_psb.ctx_psi._typeid is invalid(zero)", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            var r = procedure(m_szConsulta,
                Convert.ToString(m_uid) + ", " + Convert.ToString(m_ctx_psb.ctx_psi._typeid) + ", " + Convert.ToString(m_ctx_psb.qntd) + ", " + Convert.ToString((int)m_ctx_psb.color) + ", " + Convert.ToString(m_ctx_psb.ctx_psi.probabilidade));

            checkResponse(r, "nao conseguiu adicionar o Log de Rare Win[TYPEID=" + Convert.ToString(m_ctx_psb.ctx_psi._typeid) + ", QNTD=" + Convert.ToString(m_ctx_psb.qntd) + ", COLOR=" + Convert.ToString(m_ctx_psb.color) + ", PROBABILIDADE=" + Convert.ToString(m_ctx_psb.ctx_psi.probabilidade) + "] do Papel Shop para o PLAYER[UID=" + Convert.ToString(m_uid) + "]");

            return r;
        }


        private uint m_uid = new uint();
        private ctx_papel_shop_ball m_ctx_psb = new ctx_papel_shop_ball();

        private const string m_szConsulta = "pangya.ProcInsertPapelShopRareWinLog";
    }
}
