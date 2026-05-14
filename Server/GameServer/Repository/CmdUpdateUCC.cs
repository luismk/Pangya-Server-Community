using System;
using Pangya_GameServer.Models;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;
namespace Pangya_GameServer.Repository
{
    public class CmdUpdateUCC : Pangya_DB
    {
        public enum T_UPDATE : byte
        {
            TEMPORARY,
            FOREVER,
            COPY
        }


        public CmdUpdateUCC(uint _uid,
            WarehouseItemEx _wi,
            SYSTEMTIME _si, T_UPDATE _type
            )
        {
            this.m_uid = _uid;
            this.m_wi = _wi;
            this.m_dt_draw = _si;
            this.m_type = _type;
        }


        public uint getUID()
        {
            return m_uid;
        }

        public void setUID(uint _uid)
        {
            m_uid = _uid;
        }

        public SYSTEMTIME getDrawDate()
        {
            return m_dt_draw;
        }

        public void setDrawDate(SYSTEMTIME _si)
        {
            m_dt_draw = _si;
        }

        public CmdUpdateUCC.T_UPDATE getType()
        {
            return m_type;
        }

        public void setType(T_UPDATE _type)
        {
            m_type = _type;
        }

        public WarehouseItemEx getInfo()
        {
            return m_wi;
        }

        public void setInfo(WarehouseItemEx _wi)
        {
            m_wi = _wi;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {

            checkColumnNumber(1);

            if (m_type == T_UPDATE.COPY)
            {
                m_wi.ucc.seq = IFNULL<short>(_result.data[0]);
            }

            // Ignora o retorno dos outros tipos

            return;
        }

        protected override Response prepareConsulta()
        {

            if (m_uid == 0)
            {
                throw new exception("[CmdSaveUCC::prepareConsulta][Error] m_uid is invalid(zero)", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            if (m_wi._typeid == 0)
            {
                throw new exception("[CmdSaveUCC::prepareConsulta][Error] m_typeid is invalid(zero)", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            if (m_wi.ucc.idx[0] == '\0' || m_wi.ucc.name[0] == '\0')
            {
                throw new exception("[CmdSaveUCC::prepareConsulta][Error] UCC[IDX=" + m_wi.ucc.idx + ", NAME=" + m_wi.ucc.name + "] is invalid", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            var r = procedure(m_szConsulta, Convert.ToString(m_uid) + ", " + Convert.ToString(m_wi.id) + ", " + makeText(m_wi.ucc.idx) + ", " + makeText(m_wi.ucc.name)
            + ", " + ((m_dt_draw.Year == 0) ? "NULL" : makeText(_formatDate(m_dt_draw)))
            + ", " + ((m_wi.ucc.copier_nick[0] == '\0') ? "NULL" : makeText(m_wi.ucc.copier_nick))
            + ", " + Convert.ToString(m_wi.ucc.copier)
            + ", " + Convert.ToString((ushort)m_wi.ucc.status)
            + ", " + makeText(((m_type == T_UPDATE.TEMPORARY) ? "T" : "Y"))
            + ", " + Convert.ToString((int)m_type));

            checkResponse(r, "nao conseguiu salvar o UCC[TYPEID=" + Convert.ToString(m_wi._typeid) + ", ID=" + Convert.ToString(m_wi.id) + ", UCCIDX=" + m_wi.ucc.idx + ", NAME=" + m_wi.ucc.name + "] do PLAYER[UID=" + Convert.ToString(m_uid) + "]");

            return r;
        }


        private uint m_uid = new uint();
        private WarehouseItemEx m_wi = new WarehouseItemEx();
        private SYSTEMTIME m_dt_draw = new SYSTEMTIME();
        private T_UPDATE m_type;

        private const string m_szConsulta = "pangya.ProcUpdateUCC";
    }
}