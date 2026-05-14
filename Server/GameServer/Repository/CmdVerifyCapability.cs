
using Pangya_GameServer.Game.System;
using Pangya_GameServer.Models;
using PangyaAPI.SQL;
using System;

namespace Pangya_GameServer.Repository
{
    public class CmdVerifyCapability : Pangya_DB
    {
        private uint m_uid;
        private uCapability m_cap; 

        public CmdVerifyCapability(uint uid)
        {
            m_uid = uid;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {
            checkColumnNumber(2);

            try
            {
                int db_uid = _result.GetInt32(0);
                m_cap = new uCapability(_result.GetInt32(1));

                if (db_uid != m_uid)
                    throw new Exception($"[CmdVerifyCapability][Error] UID não bate. Req: {m_uid}, DB: {db_uid}");

                if (4 != m_cap.ulCapability)
                    throw new Exception($"[CmdVerifyCapability][Error] Capacidade não bate. Req: {m_cap.ulCapability}, DB: {4}"); 
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message); 
            }
        }

        protected override Response prepareConsulta()
        {
            var r = consulta($"SELECT uid, capability FROM pangya.account WHERE uid = {m_uid}");
            checkResponse(r, $"Não conseguiu verificar capability do UID: {m_uid}");
            return r;
        }

        public bool IsValid()
        {
            return m_cap.game_master;
        } 
    }
}
