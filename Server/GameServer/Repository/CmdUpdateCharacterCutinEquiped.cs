using System;
using PangyaAPI.Network.Models;
using PangyaAPI.SQL;

namespace Pangya_GameServer.Repository
{
    public class CmdUpdateCharacterCutinEquiped : Pangya_DB
    {
        public CmdUpdateCharacterCutinEquiped(uint _uid,
            CharacterInfo _ci)
        {

            this.m_uid = _uid;
            //this.

            this.m_ci = _ci;
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

        public CharacterInfo getInfo()
        {
            return (m_ci);
        }

        public void setInfo(CharacterInfo _ci)
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

            var r = procedure(m_szConsulta,
                Convert.ToString(m_uid) + ", " + Convert.ToString(m_ci.id) + ", " + Convert.ToString(m_ci.cut_in[0]) + ", " + Convert.ToString(m_ci.cut_in[1]) + ", " + Convert.ToString(m_ci.cut_in[2]) + ", " + Convert.ToString(m_ci.cut_in[3]));

            checkResponse(r, "nao conseguiu atualizar o character[ID=" + Convert.ToString(m_ci.id) + "] cutin equipado do player: " + Convert.ToString(m_uid));

            return r;
        }



        private uint m_uid = new uint();
        private CharacterInfo m_ci = new CharacterInfo();

        private const string m_szConsulta = "pangya.USP_FLUSHCHARACTERCUTIN";
    }
}
