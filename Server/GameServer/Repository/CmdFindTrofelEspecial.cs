using System;
using Pangya_GameServer.Models;
using PangyaAPI.IFF.JP.Extensions;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;

namespace Pangya_GameServer.Repository
{
    public class CmdFindTrofelEspecial : Pangya_DB
    {
        public enum eTYPE : byte
        {
            ESPECIAL,
            GRAND_PRIX
        }

        public CmdFindTrofelEspecial()
        {
            this.m_uid = 0;
            this.m_typeid = 0;
            this.m_type = eTYPE.ESPECIAL;
            this.m_tsi = new TrofelEspecialInfo();
        }

        public CmdFindTrofelEspecial(uint _uid,
            uint _typeid, eTYPE _type)
        {
            this.m_uid = _uid;
            this.m_typeid = _typeid;
            this.m_type = (_type);
            this.m_tsi = new TrofelEspecialInfo();
        }


        public uint getUID()
        {
            return (m_uid);
        }

        public void setUID(uint _uid)
        {
            m_uid = _uid;
        }

        public uint getTypeid()
        {
            return (m_typeid);
        }

        public void setTypeid(uint _typeid)
        {
            m_typeid = _typeid;
        }

        public CmdFindTrofelEspecial.eTYPE getType()
        {
            return m_type;
        }

        public void setType(eTYPE _type)
        {
            m_type = _type;
        }

        public bool hasFound()
        {
            return m_tsi.id > 0;
        }

        public TrofelEspecialInfo getInfo()
        {
            return m_tsi;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {

            checkColumnNumber(3);

            m_tsi.id = (int)IFNULL(_result.data[0]);

            if (m_tsi.id > 0)
            { // found
                m_tsi._typeid = (uint)IFNULL(_result.data[1]);
                m_tsi.qntd = (int)IFNULL(_result.data[2]);
            }

        }

        protected override Response prepareConsulta()
        {

            if (m_uid == 0u)
            {
                throw new exception("[CmdFindTrofelEspecial::prepareConsulta][Error] m_uid is invalid(zero)", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            if (m_typeid == 0u || sIff.getInstance().getItemGroupIdentify(m_typeid) != PangyaAPI.IFF.JP.Models.Flags.IFF_GROUP.MATCH)
            {
                throw new exception("[CmdFindTrofelEspecial::prepareConsulta][Error] TrofelEspecialInfo[TYPEID=" + Convert.ToString(m_typeid) + "] m_typeid is invalid", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            if (m_type > eTYPE.GRAND_PRIX)
            {
                throw new exception("[CmdFindTrofelEspecial::prepareConsulta][Error] m_type[VALUE=" + Convert.ToString(m_type) + "] is invalid(zero)", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            m_tsi = new TrofelEspecialInfo();
            m_tsi.id = -1;

            var r = procedure(m_szConsulta[(int)m_type],
                Convert.ToString(m_uid) + ", " + Convert.ToString(m_typeid));

            checkResponse(r, "nao conseguiu encontrar o TrofelEspecial(" + (m_type == eTYPE.GRAND_PRIX ? "Grand Prix" : "") + ")[TYPEID=" + Convert.ToString(m_typeid) + "] do PLAYER[UID=" + Convert.ToString(m_uid) + "]");

            return r;
        }

        private uint m_uid = new uint();
        private uint m_typeid = new uint();
        private TrofelEspecialInfo m_tsi = new TrofelEspecialInfo();
        private eTYPE m_type;

        private string[] m_szConsulta = { "pangya.ProcFindTrofelSpecial", "pangya.ProcFindTrofelGrandPrix" };
    }
}