using System;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;
namespace Pangya_GameServer.Repository
{  
    public class CmdPlayerTimeLogout : Pangya_DB
    {
        private uint m_uid;
        private uint m_game_server_id;

        // Ajustamos a Query para ser um UPDATE real com WHERE no UID
        private const string m_szConsulta = "UPDATE pangya.account SET [LastLogonTime] = GETDATE(), [Logon] = 1 WHERE [uid] = ";

        public CmdPlayerTimeLogout(uint _uid, uint _game_server_id = 20201)
        {
            this.m_uid = _uid;
            this.m_game_server_id = _game_server_id;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {
            // Update não costuma retornar linhas para processar aqui
        }

        protected override Response prepareConsulta()
        {
            if (m_uid == 0u)
            {
                throw new exception("[CmdPlayerTimeLogout::prepareConsulta][Error] m_uid is invalid(zero)",
                    ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB, 4, 0));
            }
 
            string sql = m_szConsulta + m_uid + " AND [game_server_id] = " + m_game_server_id;

            var r = consulta(sql);

            checkResponse(r, "Nao conseguiu atualizar o LastLogonTime do PLAYER[UID=" + m_uid + "]");

            return r;
        }
    }

    //desligar todo mundo.....
    public class CmdPlayerLogout : Pangya_DB
    {

        public CmdPlayerLogout(uint _uid)
        {
            this.m_uid = (_uid);
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {
        }

        protected override Response prepareConsulta()
        {

            if (m_uid == 0u)
            {
                throw new exception("[CmdPlayerLogout::prepareConsulta][Error] m_uid is invalid(zero)", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            var r = consulta(m_szConsulta + m_uid);

            checkResponse(r, "nao conseguiu pegar o pang do PLAYER[UID=" + Convert.ToString(m_uid) + "]");

            return r;
        }

        private uint m_uid = new uint();
        private const string m_szConsulta = "update pangya.account set [Logon] = 0 where game_server_id = ";
    }
}
