using System;
using Pangya_GameServer.Models;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;

namespace Pangya_GameServer.Repository
{
    public class CmdUpdateLastPlayerGame : Pangya_DB
    {
        public CmdUpdateLastPlayerGame()
        {
            this.m_uid = 0;
            this.m_l5pg = new Last5PlayersGame();
        }

        public CmdUpdateLastPlayerGame(uint _uid,
            Last5PlayersGame _l5pg)
        {

            this.m_uid = _uid;
            //this.
            this.m_l5pg = _l5pg;
        }

        public uint getUID()
        {
            return (m_uid);
        }

        public void setUID(uint _uid)
        {

            m_uid = _uid;

        }

        public Last5PlayersGame getInfo()
        {
            // 
            return m_l5pg;
        }

        public void setInfo(Last5PlayersGame _l5pg)
        {
            m_l5pg = _l5pg;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {

            // N�o usa por que � um UPDATE
            return;
        }

        protected override Response prepareConsulta()
        {

            if (m_uid == 0u)
            {
                throw new exception("[CmdUpdateLastPlayerGame::prepareConsulta][Error] uid is invalid(0)", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            string param = "";

            for (var i = 0; i < m_l5pg.players.Count; ++i)
            {

                if (m_l5pg.players[i].uid == 0u) // n�o tem Player nesse passa null pra o DB
                {
                    param += ", null, null, null, null";
                }
                else
                {
                    param += ", " + Convert.ToString(m_l5pg.players[i].uid) + ", " + Convert.ToString(m_l5pg.players[i].sex);
                    param += (string.IsNullOrEmpty(m_l5pg.players[i].id) ? ", null" : ", " + makeText(m_l5pg.players[i].id));
                    param += (string.IsNullOrEmpty(m_l5pg.players[i].nick) ? ", null" : ", " + makeText(m_l5pg.players[i].nick));
                }
            }

            var r = procedure(m_szConsulta,
                Convert.ToString(m_uid) + param);

            checkResponse(r, "nao conseguiu atualizar o Last 5 Player Game do PLAYER[UID=" + Convert.ToString(m_uid) + "]");

            return r;
        }

        private uint m_uid = new uint();
        private Last5PlayersGame m_l5pg = new Last5PlayersGame();

        private const string m_szConsulta = "pangya.ProcUpdateLast5PlayerGame";
    }
}
