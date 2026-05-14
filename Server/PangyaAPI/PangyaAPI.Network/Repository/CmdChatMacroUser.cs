using System;
using PangyaAPI.Network.Models;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;

namespace PangyaAPI.Network.Repository
{
    public class CmdChatMacroUser : Pangya_DB
    {
        uint m_uid = 0;
        chat_macro_user m_macro_user;
        public CmdChatMacroUser(uint _uid)
        {
            m_macro_user = new chat_macro_user();
            m_uid = _uid;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {
            checkColumnNumber(9);
            try
            {
                for (int i = (int)0u; i < 9u; i++)
                {
                    string _chat = "";
                    if (is_valid_c_string(_result.data[i]) && !(_chat = _result.data[i].ToString()).empty())
                    {
                        try
                        {
                            m_macro_user.setMacro(i, _result.data[i].ToString());
                        }
                        catch (Exception)
                        {

                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);

            }
        }

        protected override Response prepareConsulta()
        {
            var r = procedure("pangya.ProcGetMacrosUser", m_uid.ToString());
            checkResponse(r, "nao conseguiu pegar o macro do player: " + (m_uid));
            return r;
        }


        public chat_macro_user getMacroUser()
        {
            return m_macro_user;
        }

    }
}
