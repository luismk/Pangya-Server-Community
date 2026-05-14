//Convertion By LuisMK
using System;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;
namespace Pangya_GameServer.Repository
{
    public class CmdFindMailBoxItem : Pangya_DB
    {
        public CmdFindMailBoxItem()
        {
            this.m_uid = 0;
            this.m_typeid = 0;
            this.m_has_found = false;
        }

        public CmdFindMailBoxItem(uint _uid,
            uint _typeid)
        {
            this.m_uid = _uid;
            this.m_typeid = _typeid;
            this.m_has_found = false;
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

        public bool hasFound()
        {
            return m_has_found;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {

            checkColumnNumber(1);

            int _typeid = IFNULL<int>(_result.data[0]);

            if (_typeid != -1 && (uint)_typeid != m_typeid)
            {
                throw new exception("[CmdFindMailBoxItem::lineResult][Error] typeid que retornou é diferento do requisitado. [REQUEST=" + Convert.ToString(m_uid) + ", RETURN=" + Convert.ToString(_typeid) + "]", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                    3, 0));
            }
            else
            {
                m_has_found = _typeid > 0;
            }
        }

        protected override Response prepareConsulta()
        {

            if (m_uid == 0u)
            {
                throw new exception("[CmdFindMailBoxItem::prepareConsulta][Error] m_uid is invalid(zero).", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            if (m_typeid == 0u)
            {
                throw new exception("[CmdFindMailBoxItem::prepareConsulta][Error] m_typeid is invalid(zero).", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                    0, 4));
            }

            m_has_found = false;

            var r = procedure(m_consulta,
                Convert.ToString(m_uid) + ", " + Convert.ToString(m_typeid));

            checkResponse(r, "nao conseguiu encontrar o item[TYPEID=" + Convert.ToString(m_typeid) + "] no Mail Box do PLAYER[UID=" + Convert.ToString(m_uid) + "].");

            return r;
        }

        private uint m_uid = new uint();
        private uint m_typeid = new uint();

        private bool m_has_found; // Encontrou o item

        private string m_consulta = "pangya.ProcFindMailBoxItem";
    }
}