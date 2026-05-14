
using System;
using Pangya_GameServer.Models;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;

namespace Pangya_GameServer.Repository
{
    public class CmdUpdateCaddieInfo : Pangya_DB
    {
        public CmdUpdateCaddieInfo(uint _uid,
            CaddieInfoEx _ci)
        {
            this.m_uid = _uid;
            this.m_ci = (_ci);
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

        public CaddieInfoEx getInfo()
        {
            return m_ci;
        }

        public void setInfo(CaddieInfoEx _ci)
        {
            m_ci = _ci;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {

            // N�o usa por que � um UPDATE
            return;
        }

        protected override Response prepareConsulta()
        {

            if (m_uid == 0u)
            {
                throw new exception("[CmdUpdateCaddieInfo::prepareConsulta][Error] m_uid is invalid(zero)", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            if (m_ci.id < 0 || m_ci._typeid == 0u)
            {
                throw new exception("[CmdUpdateCaddieInfo::prepareConsulta][Error] CaddieInfo m_ci[TYPEID=" + Convert.ToString(m_ci._typeid) + ", ID=" + Convert.ToString(m_ci.id) + "] is invalid", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 1));
            }

            string end_dt = "null";
            string parts_end_dt = "null";

            if (!m_ci.end_date.IsEmpty)
            {
                end_dt = makeText(_formatDate(m_ci.end_date.ConvertTime()));
            }

            if (!m_ci.end_parts_date.IsEmpty)
            {
                parts_end_dt = makeText(_formatDate(m_ci.end_parts_date.ConvertTime()));
            }

            var r = procedure(m_szConsulta,
                Convert.ToString(m_uid) + ", " + Convert.ToString(m_ci.id) + ", " + Convert.ToString(m_ci._typeid) + ", " + Convert.ToString(m_ci.parts_typeid) + ", " + Convert.ToString((ushort)m_ci.level) + ", " + Convert.ToString(m_ci.exp) + ", " + Convert.ToString((ushort)m_ci.rent_flag) + ", " + Convert.ToString((ushort)m_ci.purchase) + ", " + Convert.ToString(m_ci.check_end) + ", " + end_dt + ", " + parts_end_dt);

            checkResponse(r, "PLAYER[UID=" + Convert.ToString(m_uid) + "] nao conseguiu Atualizar o Caddie Info[TYPEID=" + Convert.ToString(m_ci._typeid) + ", ID=" + Convert.ToString(m_ci.id) + ", PARTS_TYPEID=" + Convert.ToString(m_ci.parts_typeid) + ", LEVEL=" + Convert.ToString((ushort)m_ci.level) + ", EXP=" + Convert.ToString(m_ci.exp) + ", RENT_FLAG=" + Convert.ToString((ushort)m_ci.rent_flag) + ", PURCHASE=" + Convert.ToString((ushort)m_ci.purchase) + ", CHECK_END=" + Convert.ToString(m_ci.check_end) + ", END_DT=" + end_dt + ", PARTS_END_DT=" + parts_end_dt + "]");

            return r;
        }

        private uint m_uid = new uint();
        private CaddieInfoEx m_ci = new CaddieInfoEx();

        private const string m_szConsulta = "pangya.ProcUpdateCaddieInfo";
    }
}