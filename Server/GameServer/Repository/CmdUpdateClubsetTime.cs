using System;
using Pangya_GameServer.Models;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;
namespace Pangya_GameServer.Repository
{

    public class CmdUpdateClubSetTime : Pangya_DB
    {

        public CmdUpdateClubSetTime(uint _uid,
            WarehouseItemEx _wi)
        {

            this.m_uid = _uid;
            //this.
            this.m_wi = (_wi);
        }

        public uint getUID()
        {
            return (m_uid);
        }

        public void setUID(uint _uid)
        {

            m_uid = _uid;

        }

        public WarehouseItemEx getClubSet()
        {
            return m_wi;
        }

        public void setClubSet(WarehouseItemEx _wi)
        {
            m_wi = _wi;
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
                throw new exception("[CmdUpdateClubSetTime::prepareConsulta][Error] m_uid is invalid(" + Convert.ToString(m_uid) + ")", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            if (m_wi.id <= 0)
            {
                throw new exception("[CmdUpdateClubSetTime::prepareConsulta][Error] m_wi.id is invalid(" + Convert.ToString(m_wi.id) + ")", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            if (m_wi._typeid == 0u)
            {
                throw new exception("[CmdUpdateClubSetTime::prepareConsulta][Error] m_wi._typeid is invalid(" + Convert.ToString(m_wi._typeid) + ")", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            var r = procedure(m_szConsulta,
                Convert.ToString(m_uid) + ", " + Convert.ToString(m_wi.id) + ", " + Convert.ToString(m_wi._typeid) + ", " + (formatDateLocal(m_wi.end_date_unix_local)));

            checkResponse(r, "nao conseguiu atualizar o tempo do ClubSet[ID=" + Convert.ToString(m_wi.id) + ", TYPEID=" + Convert.ToString(m_wi._typeid) + ", ENDDATE=" + formatDateLocal(m_wi.end_date_unix_local) + "] do PLAYER[UID=" + Convert.ToString(m_uid) + "]");

            return r;
        }


        private uint m_uid = new uint();
        private WarehouseItemEx m_wi = new WarehouseItemEx();

        private const string m_szConsulta = "pangya.ProcUpdateClubSetTime";
    }
}
