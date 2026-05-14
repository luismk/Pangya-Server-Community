using System;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;

namespace Pangya_GameServer.Repository
{
    public class CmdUpdateTrofelEspecialQntd : Pangya_DB
    {
        public enum eTYPE : byte
        {
            ESPECIAL,
            GRAND_PRIX

        }

        public CmdUpdateTrofelEspecialQntd()
        {
            this.m_uid = 0;
            this.m_id = -1;
            this.m_qntd = 0;
            this.m_type = eTYPE.ESPECIAL;
        }

        public CmdUpdateTrofelEspecialQntd(uint _uid,
            int _id, int _qntd,
            eTYPE _type
            )
        {
            this.m_uid = _uid;
            this.m_id = _id;
            this.m_qntd = _qntd;
            this.m_type = (_type);
        }

        public uint getUID()
        {
            return (m_uid);
        }

        public void setUID(uint _uid)
        {
            m_uid = _uid;
        }

        public int getId()
        {
            return (m_id);
        }

        public void setId(int _id)
        {
            m_id = _id;
        }

        public int getQntd()
        {
            return (m_qntd);
        }

        public void setQntd(int _qntd)
        {
            m_qntd = _qntd;
        }

        public CmdUpdateTrofelEspecialQntd.eTYPE getType()
        {
            return m_type;
        }

        public void setType(eTYPE _type)
        {
            m_type = _type;
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
                throw new exception("[CmdUpdateTrofelEspecialQntd::prepareConsulta][Error] m_uid is invalid(zero)", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            if (m_id <= 0)
            {
                throw new exception("[CmdUpdateTrofelEspecialQntd::prepareConsulta][Error] m_id[VALUE=" + Convert.ToString(m_id) + "] is invalid", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            if (m_type > eTYPE.GRAND_PRIX)
            {
                throw new exception("[CmdUpdateTrofelEspecialQntd::prepareConsulta][Error] m_type[VALUE=" + Convert.ToString((ushort)m_type) + "] is invalid", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            var query = string.Format(m_szConsulta[(int)m_type], m_qntd, m_uid, m_id);

            var r = consulta(query);

            checkResponse(r, "nao conseguiu Atualizar quantidade do Trofel Especial(" + (m_type == eTYPE.GRAND_PRIX ? "Grand Prix" : "") + ")[ID=" + Convert.ToString(m_id) + ", QNTD=" + Convert.ToString(m_qntd) + "] do PLAYER[UID=" + Convert.ToString(m_uid) + "]");

            return r;
        }

        private uint m_uid = new uint();
        private int m_qntd = new int();
        private int m_id = new int();
        private eTYPE m_type;

        private string[] m_szConsulta = new string[] { "UPDATE pangya.pangya_trofel_especial SET qntd = {0} WHERE UID = {1} AND item_id = {2}", "UPDATE pangya.pangya_trofel_grandprix SET qntd = {0} WHERE UID = {1} AND item_id = {2}" };
    }
}
