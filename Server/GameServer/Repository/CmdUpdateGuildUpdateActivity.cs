using System;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;
namespace Pangya_GameServer.Repository
{
    public class CmdUpdateGuildUpdateActiviy : Pangya_DB
    {
        public CmdUpdateGuildUpdateActiviy()
        {
            this.m_index = 0Ul;
        }

        public CmdUpdateGuildUpdateActiviy(ulong _index)
        {
            this.m_index = _index;
        }

        public ulong getIndex()
        {
            return (m_index);
        }

        public void setIndex(ulong _index)
        {
            m_index = _index;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {

            // N�o usa por que � um UPDATE
            return;
        }

        protected override Response prepareConsulta()
        {

            if (m_index == 0Ul)
            {
                throw new exception("[CmdUpdateGuildUpdateActivity::prepareConsulta][Error] m_index is invalid(zero).", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }
			
           string m_szConsulta = $"UPDATE pangya.pangya_guild_update_activity SET STATE = 1 WHERE {makeEscapeKeyword("index")} = {m_index}";

            var r = consulta(m_szConsulta);

            checkResponse(r, "nao conseguiu atualizar o guild update activity[INDEX=" + Convert.ToString(m_index) + "]");

            return r;
        }


        private ulong m_index = new ulong();

    }
}
