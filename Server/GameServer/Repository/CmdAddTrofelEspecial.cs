using System;
using Pangya_GameServer.Models;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;

namespace Pangya_GameServer.Repository
{
    public class CmdAddTrofelEspecial : Pangya_DB
    {
        public enum eTYPE : byte
        {
            ESPECIAL,
            GRAND_PRIX
        }

        public CmdAddTrofelEspecial()
        {
            this.m_uid = 0;
            this.m_tsi = new TrofelEspecialInfo();
            this.m_type = eTYPE.ESPECIAL;
        }

        public CmdAddTrofelEspecial(uint _uid,
            TrofelEspecialInfo _tsi,
            eTYPE _type)
        {
            this.m_uid = _uid;
            this.m_tsi = (_tsi);
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

        public CmdAddTrofelEspecial.eTYPE getType()
        {
            return m_type;
        }

        public void setType(eTYPE _type)
        {
            m_type = _type;
        }

        public TrofelEspecialInfo getInfo()
        {
            return m_tsi;
        }

        public void setInfo(TrofelEspecialInfo _tsi)
        {
            m_tsi = _tsi;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {

            checkColumnNumber(1);

            m_tsi.id = IFNULL<int>(_result.data[0]);
        }

        protected override Response prepareConsulta()
        {

            if (m_uid == 0u)
            {
                throw new exception("[CmdAddTrofelEspecial::prepareConsulta][Error] m_uid is invalid(zero)", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            if (m_tsi._typeid == 0u)
            {
                throw new exception("[CmdAddTrofelEspecial::prepareConsulta][Error] TrofelEspecialInfo _typeid is invalid(zero)", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            if (m_type > eTYPE.GRAND_PRIX)
            {
                throw new exception("[CmdAddTrofelEspecial::prepareConsulta][Error] TYPE[VALUE=" + Convert.ToString((ushort)m_type) + "] is invalid", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            var r = procedure(
                m_szConsulta[(int)m_type],
                Convert.ToString(m_uid) + ", " + Convert.ToString(m_tsi._typeid) + ", " + Convert.ToString(m_tsi.qntd));

            checkResponse(r, "nao conseguiu Adicionar Trofel Especial(" + (m_type == eTYPE.GRAND_PRIX ? " Grand Prix" : "") + ")[TYPEID=" + Convert.ToString(m_tsi._typeid) + ", QNTD=" + Convert.ToString(m_tsi.qntd) + "] para o PLAYER[UID=" + Convert.ToString(m_uid) + "]");

            return r;
        }


        private uint m_uid = new uint();
        private TrofelEspecialInfo m_tsi = new TrofelEspecialInfo();
        private eTYPE m_type;

        private string[] m_szConsulta = { "pangya.ProcAddTrofelSpecial", "pangya.ProcAddTrofelGrandPrix" };
    }
}
