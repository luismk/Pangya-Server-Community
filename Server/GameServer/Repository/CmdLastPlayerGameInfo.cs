using System;
using Pangya_GameServer.Models;

using PangyaAPI.SQL;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;

namespace Pangya_GameServer.Repository
{
    public class CmdLastPlayerGameInfo : Pangya_DB
    {

        public CmdLastPlayerGameInfo(uint _uid)
        {
            this.m_uid = _uid;
            this.m_l5pg = new Last5PlayersGame();
        }
        public Last5PlayersGame getInfo()
        {
            return m_l5pg;
        }

        public void setInfo(Last5PlayersGame _l5pg)
        {
            m_l5pg = _l5pg;
        }

        public uint getUID()
        {
            return m_uid;
        }

        public void setUID(uint _uid)
        {
            m_uid = _uid;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {

            checkColumnNumber(20);

            try
            {
                for (var i = 0; i < 5; i++)
                {
                    m_l5pg.players[i].sex = IFNULL(_result.data[i * 4]);

                    if (is_valid_c_string(_result.data[i * 4 + 1]))
                        m_l5pg.players[i].nick = _result.data[i * 4 + 1].ToString();
                    else
                        m_l5pg.players[i].nick = "";
                    if (is_valid_c_string(_result.data[i * 4 + 2]))
                        m_l5pg.players[i].id = _result.data[i * 4 + 2].ToString();
                    else
                        m_l5pg.players[i].id = "";

                    m_l5pg.players[i].uid = IFNULL(_result.data[i * 4 + 3]);
                }
            }
            catch (exception e)
            {
                _smp.message_pool.getInstance().push(new message($"{_getName}::[ErrorSt] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        protected override Response prepareConsulta()
        {

            m_l5pg = new Last5PlayersGame();

            var r = procedure(m_szConsulta,
                Convert.ToString(m_uid));

            checkResponse(r, "nao conseguiu pegar os ultimos players game info do player: " + Convert.ToString(m_uid));

            return r;
        }

        private uint m_uid;
        private Last5PlayersGame m_l5pg = new Last5PlayersGame();

        private const string m_szConsulta = "pangya.ProcGetLastPlayerGame";
    }
}