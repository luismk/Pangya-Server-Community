using System;
using Pangya_GameServer.Models;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;
namespace Pangya_GameServer.Repository
{
    public class CmdUpdateMascotInfo : Pangya_DB
    {
        public CmdUpdateMascotInfo()
        {
            this.m_uid = 0;
        }

        public CmdUpdateMascotInfo(uint _uid,
            MascotInfoEx _mi)
        {

            this.m_uid = _uid;
            //this.
            this.m_mi = (_mi);
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

        public MascotInfoEx getInfo()
        {
            // 
            return m_mi;
        }

        public void setInfo(MascotInfoEx _mi)
        {

            m_mi = _mi;
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
                throw new exception("[CmdUpdateMascotInfo::prepareConsulta][Error] m_uid is invalid(zero)", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            if (m_mi.id < 0 || m_mi._typeid == 0u)
            {
                throw new exception("[CmdUpdateMascotInfo::prepareConsulta][Error] MascotInfoEx m_mi[TYPEID=" + Convert.ToString(m_mi._typeid) + ", ID=" + Convert.ToString(m_mi.id) + "] is invalid", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 1));
            }

            var end_dt = makeText(_formatDate(m_mi.data.ConvertTime()));

            var r = procedure(m_szConsulta,
                Convert.ToString(m_uid) + ", " + Convert.ToString(m_mi.id) + ", " + Convert.ToString(m_mi._typeid) + ", " + Convert.ToString((ushort)m_mi.level) + ", " + Convert.ToString(m_mi.exp) + ", " + Convert.ToString((ushort)m_mi.flag) + ", " + Convert.ToString(m_mi.tipo) + ", " + Convert.ToString((ushort)m_mi.is_cash) + ", " + Convert.ToString(m_mi.price) + ", " + makeText(m_mi.message) + ", " + makeText(end_dt));

            checkResponse(r, "PLAYER[UID=" + Convert.ToString(m_uid) + "] nao conseguiu Atualizar Mascot Info[TYPEID=" + Convert.ToString(m_mi._typeid) + ", ID=" + Convert.ToString(m_mi.id) + ", LEVEL=" + Convert.ToString((ushort)m_mi.level) + ", EXP=" + Convert.ToString(m_mi.exp) + ", FLAG=" + Convert.ToString((ushort)m_mi.flag) + ", TIPO=" + Convert.ToString(m_mi.tipo) + ", IS_CASH=" + Convert.ToString((ushort)m_mi.is_cash) + ", PRICE=" + Convert.ToString(m_mi.price) + ", MESSAGE=" + (m_mi.message) + ", END_DT=" + end_dt + "]");

            return r;
        }


        private uint m_uid = new uint();
        private MascotInfoEx m_mi = new MascotInfoEx();

        private const string m_szConsulta = "pangya.ProcUpdateMascotInfo";
    }
}
