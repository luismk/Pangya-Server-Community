using System;
using PangyaAPI.SQL;

namespace PangyaAPI.Network.Repository
{
    public class CmdSaveNick : Pangya_DB
    {
        uint m_uid = 0;
        string m_nick = "";

        public CmdSaveNick(uint _uid, string _nick)
        {
            m_uid = _uid;
            m_nick = _nick;
        }
        protected override void lineResult(ctx_res _result, uint _index_result)
        {

        }

        protected override Response prepareConsulta()
        {
            if (string.IsNullOrEmpty(m_nick))
                throw new Exception("[CmdSaveNick::prepareConsulta][Error] Nick invalid");

            var r = procedure("pangya.ProcSaveNickname", m_uid + ", " + makeText(m_nick));

            checkResponse(r, "nao conseguiu atualizar o nick: " + m_nick + ", do player: " + m_uid);
            return r;
        }

    }
}
