
using PangyaAPI.Network.Models;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;

namespace Pangya_GameServer.Repository
{
    public class CmdUpdateCharacterMastery : Pangya_DB
    {
        public CmdUpdateCharacterMastery(uint _uid, CharacterInfo _ci)

        {
            m_uid = _uid;
            setInfo(_ci);
        }
        public uint getUID()
        {
            return m_uid;
        }
        public void setUID(uint _uid)
        {
            m_uid = _uid;
        }

        public CharacterInfo getInfo()
        {
            return m_ci;
        }
        public void setInfo(CharacterInfo _ci)
        {
            m_ci = _ci;
        }
        protected override void lineResult(ctx_res _result, uint _index_result)
        { // N�o usa por que � um UPDATE
            return;
        }
        protected override Response prepareConsulta()
        {
            if (m_uid == 0)
                throw new exception("[CmdUpdateCharacterMastery::prepareConsulta][Error] m_uid is invalid(zero)", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB, 4, 0));

            if (m_ci.id <= 0 || m_ci._typeid == 0)
                throw new exception("[CmdUpdateCharacterMastery::prepareConsulta][Error] CharacterInfo[TYPEID=" + (m_ci._typeid) + ", ID=" + (m_ci.id) + "] is invalid", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB, 4, 0));

            var r = _update(m_szConsulta[0] + (m_ci.mastery) + m_szConsulta[1] + (m_uid) + m_szConsulta[2] + (m_ci.id));

            checkResponse(r, "nao conseguiu atualizar Character[TYPEID=" + (m_ci._typeid) + ", ID=" + (m_ci.id) + "] Mastery[value=" + (m_ci.mastery) + "] do PLAYER[UID=" + (m_uid) + "]");

            return r;
        }

        private uint m_uid = new uint();
        private CharacterInfo m_ci = new CharacterInfo();

        private string[] m_szConsulta = { "UPDATE pangya.pangya_character_information SET mastery = ", " WHERE UID = ", " AND item_id = " };
    }
}