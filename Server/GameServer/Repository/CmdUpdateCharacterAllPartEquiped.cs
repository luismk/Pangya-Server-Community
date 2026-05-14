using System;
using System.Data;
using PangyaAPI.Network.Models;
using PangyaAPI.SQL;

namespace Pangya_GameServer.Repository
{
    public class CmdUpdateCharacterAllPartEquiped : Pangya_DB
    {
        private uint m_uid;
        private CharacterInfo m_ci;

        public CmdUpdateCharacterAllPartEquiped(uint uid, CharacterInfo ci)
        {
            this.m_uid = uid;
            this.m_ci = ci;
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
        {

            // N�o usa aqui por que � um UPDATE
            return;
        }

        protected override Response prepareConsulta()
        {

            string q = ""; // "|" + (_ci._typeid) + "|" + (_ci.id);

            q += "|" + Convert.ToString((ushort)m_ci.default_hair) + "|" + Convert.ToString((ushort)m_ci.default_shirts);
            q += "|" + Convert.ToString((ushort)m_ci.gift_flag) + "|" + Convert.ToString((ushort)m_ci.purchase);

            uint @is;
            for (@is = 0; @is < (m_ci.parts_typeid.Length); ++@is)
            {
                q += "|" + Convert.ToString(m_ci.parts_typeid[@is]);
            }

            for (@is = 0; @is < (m_ci.parts_id.Length); ++@is)
            {
                q += "|" + Convert.ToString(m_ci.parts_id[@is]);
            }

            for (@is = 0; @is < (m_ci.auxparts.Length); ++@is)
            {
                q += "|" + Convert.ToString(m_ci.auxparts[@is]);
            }

            for (@is = 0; @is < (m_ci.cut_in.Length); ++@is)
            {
                q += "|" + Convert.ToString(m_ci.cut_in[@is]);
            }

            for (@is = 0; @is < (m_ci.pcl.Length); ++@is)
            {
                q += "|" + Convert.ToString(m_ci.pcl[@is]);
            }

            // Mastery Character
            q += "|" + Convert.ToString(m_ci.mastery);
            var r = procedure(m_szConsulta,
m_uid + ", " +                                // Ex: "retreev（ちょき）（むふ）"
 m_ci.id + ", " +           // Ex: "1"
   makeText(q));                       // Ex: "2025-06-20 14:32:00");
            checkResponse(r, "nao conseguiu atualizar o character[ID=" + Convert.ToString(m_ci.id) + "] parts equipado do player: " + Convert.ToString(m_uid));

            return r;
        }

        private const string m_szConsulta = "pangya.USP_CHAR_EQUIP_SAVE_S4";
    }
}