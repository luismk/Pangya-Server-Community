using System;
using PangyaAPI.Network.Models;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;

namespace Pangya_GameServer.Repository
{
    public class CmdUpdateChatMacroUser : Pangya_DB
    {
        public CmdUpdateChatMacroUser(uint _uid,
                chat_macro_user _cmu)
        {
            this.m_uid = _uid;
            this.m_cmu = _cmu;
        }


        public uint getUID()
        {
            return m_uid;
        }

        public void setUID(uint _uid)
        {
            m_uid = _uid;
        }

        public chat_macro_user getInfo()
        {
            return m_cmu;
        }

        public void setInfo(chat_macro_user _cmu)
        {
            m_cmu = _cmu;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {

            // N�o usa por que � um UPDATE
            return;
        }

        protected override Response prepareConsulta()
        {
            if (m_uid == 0)
            {
                throw new exception("[CmdUpdateChatMacroUser::prepareConsulta][Error] m_uid is invalid(zero)",
                    ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB, 4, 0));
            }

            // Função interna para limpar a sujeira da memória/buffer
            string Clean(string text)
            {
                if (string.IsNullOrEmpty(text)) return "";
                // Remove Nul (\0), substitui quebras de linha por espaço e remove espaços inúteis nas pontas
                return text.Replace("\0", "").Replace("\r", "").Replace("\n", "").Trim();
            }

            var m0 = Clean(m_cmu.macro[0].text);
            var m1 = Clean(m_cmu.macro[1].text);
            var m2 = Clean(m_cmu.macro[2].text);
            var m3 = Clean(m_cmu.macro[3].text);
            var m4 = Clean(m_cmu.macro[4].text);
            var m5 = Clean(m_cmu.macro[5].text);
            var m6 = Clean(m_cmu.macro[6].text);
            var m7 = Clean(m_cmu.macro[7].text);
            var m8 = Clean(m_cmu.macro[8].text);
             
            var r = procedure(m_szConsulta,
                Convert.ToString(m_uid) + ", " +
                makeText(m0) + ", " + makeText(m1) + ", " + makeText(m2) + ", " +
                makeText(m3) + ", " + makeText(m4) + ", " + makeText(m5) + ", " +
                makeText(m6) + ", " + makeText(m7) + ", " + makeText(m8)
            );

            checkResponse(r, "nao conseguiu atualizar Chat Macro...");

            return r;
        }

        private uint m_uid = new uint();
        private chat_macro_user m_cmu = new chat_macro_user();

        private const string m_szConsulta = "pangya.ProcUpdateChatMacroUser";
    }
}