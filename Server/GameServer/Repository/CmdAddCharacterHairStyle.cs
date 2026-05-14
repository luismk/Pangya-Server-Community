//Convertion By LuisMK
using System;
using PangyaAPI.Network.Repository;
using PangyaAPI.Network.Models;
using PangyaAPI.SQL;

namespace Pangya_GameServer.Repository
{
    public class CmdAddCharacterHairStyle : CmdAddItemBase
    {
        public CmdAddCharacterHairStyle(uint _uid,
            CharacterInfo _ci,
            byte _purchase,
            byte _gift_flag) : base(_uid,
                _purchase, _gift_flag)
        {
            this.m_ci = _ci;
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

            // N�o usa aqui por que � um UPDATE
            return;
        }

        protected override Response prepareConsulta()
        {

            var r = _update(m_szConsulta[0] + Convert.ToString((ushort)m_ci.default_hair) + m_szConsulta[1] + Convert.ToString(m_uid) + m_szConsulta[2] + Convert.ToString(m_ci.id));

            checkResponse(r, "nao consiguiu adicionar o hair style[" + Convert.ToString((ushort)m_ci.default_hair) + "] para o character[ID=" + Convert.ToString(m_ci.id) + "] do player: " + Convert.ToString(m_uid));

            return r;
        }

        private CharacterInfo m_ci = new CharacterInfo();

        private string[] m_szConsulta = { "UPDATE pangya.pangya_character_information SET default_hair = ", " WHERE UID = ", " AND item_id = " };
    }
}