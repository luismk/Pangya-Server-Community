using System;
using PangyaAPI.SQL;

namespace PangyaAPI.Network.Repository
{
    public class CmdUpdateCharacterEquiped : Pangya_DB
    {
        public CmdUpdateCharacterEquiped(uint _uid,
                int _character_id)
        {
            this.m_uid = _uid;
            this.m_character_id = _character_id;
        }


        public uint getUID()
        {
            return (m_uid);
        }

        public void setUID(uint _uid)
        {
            m_uid = _uid;
        }

        public int getCharacterID()
        {
            return (m_character_id);
        }

        public void setCharacterID(int _character_id)
        {
            m_character_id = _character_id;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {

            // N�o usa por que � um UPDATE
            return;
        }

        protected override Response prepareConsulta()
        {

            var r = procedure(
                m_szConsulta,
                Convert.ToString(m_uid) + ", " + Convert.ToString(m_character_id));

            checkResponse(r, "nao conseguiu atualizar o character[ID=" + Convert.ToString(m_character_id) + "] equipado do player: " + Convert.ToString(m_uid));

            return r;
        }

        private uint m_uid = new uint();
        private int m_character_id = new int();

        private const string m_szConsulta = "pangya.USP_FLUSH_CHARACTER";
    }
}